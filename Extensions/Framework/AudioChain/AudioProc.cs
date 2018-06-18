﻿// This file is a part of MPDN Extensions.
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

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cudafy;
using Cudafy.Host;
using Mpdn.OpenCl;
using Mpdn.RenderScript;

namespace Mpdn.Extensions.Framework.AudioChain
{
    public static class AudioProc
    {
        private static GPGPU s_Gpu;

        public const int THREAD_COUNT = 512;

        public static GPGPU Gpu
        {
            get { return s_Gpu; }
        }

        private static Lazy<bool> s_Init = new Lazy<bool>(Init);
        private static Lazy<Task<bool>> s_InitAsync = new Lazy<Task<bool>>(() => Task.Run(() => s_Init.Value));

        public static Task<bool> AsyncInitialize()
        {
            return s_InitAsync.Value;
        }

        public static bool Initialize()
        {
            return s_Init.Value;
        }

        private static bool Init()
        {
            if (s_Gpu != null)
                return true;

            try
            {
                var devices = CudafyHost.GetDeviceProperties(eGPUType.OpenCL).ToArray();
                var device = devices.FirstOrDefault(d => d.Integrated && d.PlatformName.Contains("Intel(R)")); // use Intel iGPU if possible
                if (device == null || IsInUseForVideoRendering(device))
                {
                    // Fallback to CPU (prefer AMD Accelerated Parallel Processing first as it is faster)
                    const string cpuId = " CPU ";
                    device = devices.FirstOrDefault(d => d.Name.Contains(cpuId) && d.PlatformName.Contains("AMD"));
                    if (device == null)
                    {
                        // Settle for any CPU OpenCL device (most likely Intel OpenCL) as the last resort
                        device = devices.FirstOrDefault(d => d.Name.Contains(cpuId));
                        if (device == null)
                        {
                            throw new OpenClException("Error: CPU OpenCL drivers not installed");
                        }
                    }
                }
                var gpu = CudafyHost.GetDevice(eGPUType.OpenCL, device.DeviceId);
                gpu.LoadAudioKernel(typeof(AudioKernels));

                Player.Closed += PlayerOnClosed;

                s_Gpu = gpu;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                return false;
            }

            return true;
        }

        private static void Destroy()
        {
            try
            {
                if (s_Gpu == null)
                    return;

                s_Gpu.FreeAll();
                s_Gpu.HostFreeAll();
                s_Gpu.DestroyStreams();
                s_Gpu.UnloadModules();
                DisposeHelper.Dispose(ref s_Gpu);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            Player.Closed -= PlayerOnClosed;
        }

        private static bool IsInUseForVideoRendering(GPGPUProperties device)
        {
            var name1 = device.Name.Trim();
            var name2 = Renderer.Dx9GpuInfo.Details.Description.Trim();
            return name1.Contains(name2) || name2.Contains(name1);
        }

        private static void PlayerOnClosed(object sender, EventArgs eventArgs)
        {
            Destroy();
        }
    }
}
