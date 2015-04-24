// This file is a part of MPDN Extensions.
// https://github.com/zachsaw/MPDN_Extensions
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3.0 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library.
// 
using System;
using System.Drawing;

namespace Mpdn.RenderScript
{
    namespace Shiandow.SuperRes
    {
        public class SuperRes : RenderChain
        {
            #region Settings

            public enum SuperResDoubler
            {
                None,
                NEDI,
                NNEDI3
            };

            public int Passes { get; set; }

            public float Strength { get; set; }
            public float Sharpness { get; set; }
            public float AntiAliasing { get; set; }
            public float AntiRinging { get; set; }
            public bool FastMethod { get; set; }

            public SuperResDoubler ImageDoubler { get; set; }
            public bool NoIntermediates { get; set; }

            public bool FirstPassOnly;

            #endregion

            public Func<TextureSize> TargetSize; // Not saved
            private IScaler downscaler, upscaler;

            public SuperRes()
            {
                TargetSize = () => Renderer.TargetSize;

                Passes = 3;

                Strength = 0.75f;
                Sharpness = 0.5f;
                AntiAliasing = 0.5f;
                AntiRinging = 0.75f;

                ImageDoubler = SuperResDoubler.None;
                NoIntermediates = false;

                FirstPassOnly = false;
                upscaler = new Scaler.Jinc(ScalerTaps.Four, false);
                downscaler = new Scaler.Bilinear();
            }

            public override IFilter CreateFilter(IResizeableFilter sourceFilter)
            {
                return CreateFilter(sourceFilter, sourceFilter);
            }

            public IFilter CreateFilter(IFilter original, IFilter initial)
            {
                IFilter lab, linear, result = initial;

                var inputSize = original.OutputSize;
                var currentSize = original.OutputSize;
                var targetSize = TargetSize();

                RenderChain Doubler = null;
                switch (ImageDoubler)
                {
                    case SuperResDoubler.NEDI:
                        Doubler = new Shiandow.Nedi.Nedi
                        {
                            AlwaysDoubleImage = false,
                            ForceCentered = true,
                            LumaConstants = new[] { 1.0f, 0.0f, 0.0f }
                        };
                        break;
                    case SuperResDoubler.NNEDI3:
                        Doubler = new Shiandow.NNedi3.NNedi3
                        {
                            ForceCentered = true
                        };
                        break;
                }

                var Diff = CompileShader("Diff.hlsl").Configure(format: TextureFormat.Float16);
                var SuperRes = CompileShader(FastMethod ? "SuperResFast.hlsl" : "SuperRes.hlsl");

                var GammaToLab = CompileShader("../Common/GammaToLab.hlsl");
                var LabToGamma = CompileShader("../Common/LabToGamma.hlsl");
                var LinearToGamma = CompileShader("../Common/LinearToGamma.hlsl");
                var GammaToLinear = CompileShader("../Common/GammaToLinear.hlsl");
                var LabToLinear = CompileShader("../Common/LabToLinear.hlsl");
                var LinearToLab = CompileShader("../Common/LinearToLab.hlsl");

                // Skip if downscaling
                if (targetSize.Width <= inputSize.Width && targetSize.Height <= inputSize.Height)
                    return initial;

                // Initial scaling
                lab = new ShaderFilter(GammaToLab, initial);
                original = new ShaderFilter(GammaToLab, original);

                for (int i = 1; i <= Passes; i++)
                {
                    IFilter res, diff;
                    bool useBilinear = (upscaler is Scaler.Bilinear) || (FirstPassOnly && !(i == 1));

                    // Calculate size
                    if (i == Passes || NoIntermediates) currentSize = targetSize;
                    else currentSize = CalculateSize(currentSize, targetSize, i);
                                        
                    // Resize
                    if (i == 1 && Doubler != null)
                    {
                        var nedi = lab + Doubler;
                        nedi.SetSize(currentSize);
                        lab = nedi;
                    }
                    else
                        lab = new ResizeFilter(lab, currentSize, upscaler, downscaler);

                    // Downscale and Subtract
                    linear = new ShaderFilter(LabToLinear, lab);
                    res = new ResizeFilter(linear, inputSize, upscaler, downscaler); // Downscale result
                    diff = new ShaderFilter(Diff, res, original);                    // Compare with original

                    // Scale difference back
                    if (!useBilinear)
                        diff = new ResizeFilter(diff, currentSize, upscaler, downscaler);
                    
                    // Update result
                    var Consts = new[] { Strength, Sharpness, AntiAliasing, AntiRinging };
                    lab = new ShaderFilter(SuperRes.Configure(useBilinear, arguments: Consts), lab, diff, original);
                    result = new ShaderFilter(LabToGamma, lab);
                }

                return result;
            }

            private TextureSize CalculateSize(TextureSize sizeA, TextureSize sizeB, int k)
            {
                double w, h;
                var MaxScale = 2.0;
                var MinScale = Math.Sqrt(MaxScale);
                
                int minW = sizeA.Width; int minH = sizeA.Height;
                int maxW = sizeB.Width; int maxH = sizeB.Height;

                int maxSteps = (int)Math.Floor  (Math.Log((double)(maxH * maxW) / (double)(minH * minW)) / (2 * Math.Log(MinScale)));
                int minSteps = (int)Math.Ceiling(Math.Log((double)(maxH * maxW) / (double)(minH * minW)) / (2 * Math.Log(MaxScale)));
                int steps = Math.Max(Math.Max(1,minSteps), Math.Min(maxSteps, Passes - (k - 1)));
                
                w = minW * Math.Pow((double)maxW / (double)minW, (double)Math.Min(k, steps) / (double)steps);
                h = minW * Math.Pow((double)maxH / (double)minH, (double)Math.Min(k, steps) / (double)steps);

                return new TextureSize(Math.Max(minW, Math.Min(maxW, (int)Math.Round(w))),
                                Math.Max(minH, Math.Min(maxH, (int)Math.Round(h))));
            }
        }

        public class SuperResUi : RenderChainUi<SuperRes, SuperResConfigDialog>
        {
            protected override string ConfigFileName
            {
                get { return "Shiandow.SuperRes"; }
            }

            public override ExtensionUiDescriptor Descriptor
            {
                get
                {
                    return new ExtensionUiDescriptor
                    {
                        Guid = new Guid("3E7C670C-EFFB-41EB-AC19-207E650DEBD0"),
                        Name = "SuperRes",
                        Description = "SuperRes image scaling",
                        Copyright = "SuperRes by Shiandow",
                    };
                }
            }
        }
    }
}
