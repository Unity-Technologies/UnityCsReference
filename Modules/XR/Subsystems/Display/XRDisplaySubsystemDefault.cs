// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine.XR
{
    internal class XRDisplaySubsystemDefault : XRDisplaySubsystem
    {
        private static XRDisplaySubsystemDefault s_Instance = null;

        // Singleton instance
        public static XRDisplaySubsystemDefault instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new XRDisplaySubsystemDefault();
                }

                return s_Instance;
            }
        }

        // Default safe values for all relevant APIs
        public override float displayRefreshRate => 0.0f;
        public override float fovZoomFactor { get => 1.0f; set { } }

        public override bool TryGetAppGPUTimeLastFrame(out float gpuTimeLastFrame)
        {
            gpuTimeLastFrame = 0.0f;
            return false;
        }

        public override bool TryGetDroppedFrameCount(out int droppedFrameCount)
        {
            droppedFrameCount = 0;
            return false;
        }

        public override bool TryGetFramePresentCount(out int framePresentCount)
        {
            framePresentCount = 0;
            return false;
        }

    }
}
