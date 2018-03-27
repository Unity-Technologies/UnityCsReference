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
            CaptureScreenshot(filename, 1, StereoScreenCaptureMode.LeftEye);
        }

        public static void CaptureScreenshot(string filename, int superSize)
        {
            CaptureScreenshot(filename, superSize, StereoScreenCaptureMode.LeftEye);
        }

        public static void CaptureScreenshot(string filename, StereoScreenCaptureMode stereoCaptureMode)
        {
            CaptureScreenshot(filename, 1, stereoCaptureMode);
        }

        public static Texture2D CaptureScreenshotAsTexture()
        {
            return CaptureScreenshotAsTexture(1, StereoScreenCaptureMode.LeftEye);
        }

        public static Texture2D CaptureScreenshotAsTexture(int superSize)
        {
            return CaptureScreenshotAsTexture(superSize, StereoScreenCaptureMode.LeftEye);
        }

        public static Texture2D CaptureScreenshotAsTexture(StereoScreenCaptureMode stereoCaptureMode)
        {
            return CaptureScreenshotAsTexture(1, stereoCaptureMode);
        }

        private static extern void CaptureScreenshot(string filename, [UnityEngine.Internal.DefaultValue("1")] int superSize, [UnityEngine.Internal.DefaultValue("1")]  StereoScreenCaptureMode CaptureMode);
        private static extern Texture2D CaptureScreenshotAsTexture(int superSize, StereoScreenCaptureMode stereoScreenCaptureMode);


        // Offsets must match UnityVRBlitMode in IUnityVR.h
        public enum StereoScreenCaptureMode
        {
            LeftEye = 1,
            RightEye = 2,
            BothEyes = 3,
        }
    }
}
