// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Device
{
    public static class Screen
    {
        public static bool autorotateToLandscapeLeft
        {
            get => ShimManager.screenShim.autorotateToLandscapeLeft;
            set => ShimManager.screenShim.autorotateToLandscapeLeft = value;
        }

        public static bool autorotateToLandscapeRight
        {
            get => ShimManager.screenShim.autorotateToLandscapeRight;
            set => ShimManager.screenShim.autorotateToLandscapeRight = value;
        }

        public static bool autorotateToPortrait
        {
            get => ShimManager.screenShim.autorotateToPortrait;
            set => ShimManager.screenShim.autorotateToPortrait = value;
        }

        public static bool autorotateToPortraitUpsideDown
        {
            get => ShimManager.screenShim.autorotateToPortraitUpsideDown;
            set => ShimManager.screenShim.autorotateToPortraitUpsideDown = value;
        }

        public static Resolution currentResolution => ShimManager.screenShim.currentResolution;

        public static Rect[] cutouts => ShimManager.screenShim.cutouts;

        public static float dpi => ShimManager.screenShim.dpi;

        public static bool fullScreen
        {
            get => ShimManager.screenShim.fullScreen;
            set => ShimManager.screenShim.fullScreen = value;
        }

        public static FullScreenMode fullScreenMode
        {
            get => ShimManager.screenShim.fullScreenMode;
            set => ShimManager.screenShim.fullScreenMode = value;
        }

        public static int height => ShimManager.screenShim.height;

        public static int width => ShimManager.screenShim.width;

        public static ScreenOrientation orientation
        {
            get => ShimManager.screenShim.orientation;
            set => ShimManager.screenShim.orientation = value;
        }

        public static Resolution[] resolutions => ShimManager.screenShim.resolutions;

        public static Rect safeArea => ShimManager.screenShim.safeArea;

        public static int sleepTimeout
        {
            get => ShimManager.screenShim.sleepTimeout;
            set => ShimManager.screenShim.sleepTimeout = value;
        }

        public static float brightness
        {
            get => ShimManager.screenShim.brightness;
            set => ShimManager.screenShim.brightness = value;
        }

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [Internal.DefaultValue("0")] int preferredRefreshRate)
        {
            ShimManager.screenShim.SetResolution(width, height, fullscreenMode, preferredRefreshRate);
        }

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode)
        {
            SetResolution(width, height, fullscreenMode, 0);
        }

        public static void SetResolution(int width, int height, bool fullscreen, [Internal.DefaultValue("0")] int preferredRefreshRate)
        {
            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, preferredRefreshRate);
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            SetResolution(width, height, fullscreen, 0);
        }

        public static Vector2Int mainWindowPosition => ShimManager.screenShim.mainWindowPosition;
        public static DisplayInfo mainWindowDisplayInfo => ShimManager.screenShim.mainWindowDisplayInfo;
        public static void GetDisplayLayout(List<DisplayInfo> displayLayout) => ShimManager.screenShim.GetDisplayLayout(displayLayout);
        public static AsyncOperation MoveMainWindowTo(in DisplayInfo display, Vector2Int position) => ShimManager.screenShim.MoveMainWindowTo(display, position);
    }
}
