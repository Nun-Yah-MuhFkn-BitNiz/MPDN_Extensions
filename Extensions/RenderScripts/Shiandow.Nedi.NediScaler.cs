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
    namespace Shiandow.Nedi
    {
        public class Nedi : RenderChain
        {
            #region Settings

            public Nedi()
            {
                AlwaysDoubleImage = false;
                Centered = true;
            }

            public bool AlwaysDoubleImage { get; set; }
            public bool Centered { get; set; }

            #endregion

            public float[] LumaConstants = {0.2126f, 0.7152f, 0.0722f};

            private bool UseNedi(IFilter sourceFilter)
            {
                var size = sourceFilter.OutputSize;
                if (size.IsEmpty)
                    return false;

                if (AlwaysDoubleImage)
                    return true;

                return Renderer.TargetSize.Width > size.Width ||
                       Renderer.TargetSize.Height > size.Height;
            }

            public override IFilter CreateFilter(IResizeableFilter sourceFilter)
            {
                Func<TextureSize, TextureSize> transformWidth;
                Func<TextureSize, TextureSize> transformHeight;
                if (Centered)
                {
                    transformWidth = s => new TextureSize(2 * s.Width - 1, s.Height);
                    transformHeight = s => new TextureSize(s.Width, 2 * s.Height - 1);
                } else {
                    transformWidth = s => new TextureSize(2 * s.Width, s.Height);
                    transformHeight = s => new TextureSize(s.Width, 2 * s.Height);
                }

                var nedi1Shader = CompileShader("NEDI-I.hlsl").Configure(arguments: LumaConstants);
                var nedi2Shader = CompileShader("NEDI-II.hlsl").Configure(arguments: LumaConstants);
                var nediHInterleaveShader = CompileShader("NEDI-HInterleave.hlsl").Configure(transform: transformWidth);
                var nediVInterleaveShader = CompileShader("NEDI-VInterleave.hlsl").Configure(transform: transformHeight);

                if (!UseNedi(sourceFilter))
                    return sourceFilter;

                var nedi1 = new ShaderFilter(nedi1Shader, sourceFilter);
                var nediH = new ShaderFilter(nediHInterleaveShader, sourceFilter, nedi1);
                var nedi2 = new ShaderFilter(nedi2Shader, nediH);
                var nediV = new ShaderFilter(nediVInterleaveShader, nediH, nedi2);

                return nediV;
            }
        }

        public class NediScaler : RenderChainUi<Nedi, NediConfigDialog>
        {
            protected override string ConfigFileName
            {
                get { return "Shiandow.Nedi"; }
            }

            public override ExtensionUiDescriptor Descriptor
            {
                get
                {
                    return new ExtensionUiDescriptor
                    {
                        Guid = new Guid("B8E439B7-7DC2-4FC1-94E2-608A39756FB0"),
                        Name = "NEDI",
                        Description = GetDescription(),
                        Copyright = "NEDI by Shiandow",
                    };
                }
            }

            private string GetDescription()
            {
                var options = string.Format("{0}", Settings.AlwaysDoubleImage ? " (forced)" : string.Empty);
                return string.Format("NEDI image doubler{0}", options);
            }
        }
    }
}
