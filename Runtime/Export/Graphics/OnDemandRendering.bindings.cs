// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;
using System;

namespace UnityEngine.Rendering
{
    [RequiredByNativeCode]
    public class OnDemandRendering
    {
        // Default to 1. Render every frame.
        private static int m_RenderFrameInterval = 1;

        public static bool willCurrentFrameRender
        {
            get
            {
                return Time.frameCount % renderFrameInterval == 0;
            }
        }

        public static int renderFrameInterval
        {
            get { return m_RenderFrameInterval; }

            set { m_RenderFrameInterval = Math.Max(1, value); }
        }

        [RequiredByNativeCode]
        internal static void GetRenderFrameInterval(out int frameInterval) { frameInterval = renderFrameInterval; }

        public static int effectiveRenderFrameRate
        {
            get
            {
                if (QualitySettings.vSyncCount > 0)
                {
                    return Screen.currentResolution.refreshRate / QualitySettings.vSyncCount / renderFrameInterval;
                }
                else
                {
                    if (Application.targetFrameRate <= 0)
                        return Application.targetFrameRate;
                    return Application.targetFrameRate / renderFrameInterval;
                }
            }
        }
    }
}
