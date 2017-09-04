// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/ScreenCapture/Public/CaptureScreenshot.h")]
    public static class ScreenCapture
    {
        public static void CaptureScreenshot(string filename)
        {
            CaptureScreenshot(filename, 1);
        }

        public static extern void CaptureScreenshot(string filename, [UnityEngine.Internal.DefaultValue("1")] int superSize);
        public static extern Texture2D CaptureScreenshotAsTexture(int superSize = 1);
    }
}
