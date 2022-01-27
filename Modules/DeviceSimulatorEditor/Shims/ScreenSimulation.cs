// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal class ScreenSimulation : ScreenShimBase
    {
        // Reasonable maximum resolution, tested on an Android device and game crashes when set beyond
        private const int k_MaxResolution = 8192;

        private int m_RequestedWidth;
        private int m_RequestedHeight;
        private ScreenOrientation m_RequestedOrientation;
        private bool m_RequestedFullScreen;
        private bool m_RequestDefaultResolution;
        private bool m_RequestInsetUpdate;

        private SimulationPlayerSettings m_PlayerSettings;
        private DeviceInfo m_DeviceInfo;
        private ScreenData m_Screen;

        private bool m_AutoRotation;
        public bool AutoRotation => m_AutoRotation;

        private ScreenOrientation m_RenderedOrientation = ScreenOrientation.Portrait;
        private Dictionary<ScreenOrientation, bool> m_AllowedAutoRotation;
        private Dictionary<ScreenOrientation, OrientationData> m_SupportedOrientations;

        private Rect m_CurrentSafeArea;
        private Rect[] m_CurrentCutouts;

        private bool m_WasResolutionSet;
        private int m_CurrentWidth;
        private int m_CurrentHeight;

        private bool m_IsFullScreen;
        public Vector4 Insets { get; private set; }
        public Vector4 InsetsInCurrentOrientation
        {
            get
            {
                switch (m_RenderedOrientation)
                {
                    case ScreenOrientation.Portrait:
                        return Insets;
                    case ScreenOrientation.PortraitUpsideDown:
                        return new Vector4(Insets.z, Insets.w, Insets.x, Insets.y);
                    case ScreenOrientation.LandscapeLeft:
                        return new Vector4(Insets.y, Insets.z, Insets.w, Insets.x);
                    case ScreenOrientation.LandscapeRight:
                        return new Vector4(Insets.w, Insets.x, Insets.y, Insets.z);
                    default:
                        return Insets;
                }
            }
        }

        private bool m_IsRenderingOutsideSafeArea;
        public Rect ScreenSpaceSafeArea { get; private set; }

        private ScreenOrientation m_DeviceOrientation;

        public int DeviceRotation
        {
            set
            {
                m_DeviceOrientation = SimulatorUtilities.RotationToScreenOrientation(value);
                ApplyAutoRotation();
            }
        }

        public bool IsRenderingLandscape => SimulatorUtilities.IsLandscape(m_RenderedOrientation);

        public event Action OnOrientationChanged;
        public event Action OnAllowedOrientationChanged;
        public event Action<int, int> OnResolutionChanged;
        public event Action<bool> OnFullScreenChanged;
        public event Action<Vector4> OnInsetsChanged;
        public event Action<Rect> OnScreenSpaceSafeAreaChanged;

        public ScreenSimulation(DeviceInfo device, SimulationPlayerSettings playerSettings, int screenIndex)
        {
            m_PlayerSettings = playerSettings;
            m_DeviceInfo = device;
            m_Screen = device.screens[screenIndex];

            FindSupportedOrientations();

            m_AllowedAutoRotation = new Dictionary<ScreenOrientation, bool>();
            m_AllowedAutoRotation.Add(ScreenOrientation.Portrait, m_PlayerSettings.allowedPortrait);
            m_AllowedAutoRotation.Add(ScreenOrientation.PortraitUpsideDown, m_PlayerSettings.allowedPortraitUpsideDown);
            m_AllowedAutoRotation.Add(ScreenOrientation.LandscapeLeft, m_PlayerSettings.allowedLandscapeLeft);
            m_AllowedAutoRotation.Add(ScreenOrientation.LandscapeRight, m_PlayerSettings.allowedLandscapeRight);

            // Set the full screen mode.
            m_IsFullScreen = !m_DeviceInfo.IsAndroidDevice() || m_PlayerSettings.androidStartInFullscreen;
            m_RequestedFullScreen = m_IsFullScreen;
            m_IsRenderingOutsideSafeArea = !m_DeviceInfo.IsAndroidDevice() || m_PlayerSettings.androidRenderOutsideSafeArea;

            // Calculate the right orientation.
            var settingOrientation = SimulatorUtilities.ToScreenOrientation(m_PlayerSettings.defaultOrientation);
            if (settingOrientation == ScreenOrientation.AutoRotation)
            {
                m_AutoRotation = true;
                SetFirstAvailableAutoOrientation();
            }
            else if (m_SupportedOrientations.ContainsKey(settingOrientation))
            {
                m_AutoRotation = false;
                RequestOrientation(settingOrientation);
            }
            else
            {
                // The real iPhone X responds to this absolute corner case by crashing, we will not do that.
                m_AutoRotation = false;
                RequestOrientation(m_SupportedOrientations.Keys.ToArray()[0]);
            }

            m_RequestInsetUpdate = true;
            m_RequestDefaultResolution = true;

            ShimManager.UseShim(this);
        }

        public void ChangeScreen(int screenIndex)
        {
            m_Screen = m_DeviceInfo.screens[screenIndex];
            FindSupportedOrientations();

            if (!m_WasResolutionSet)
            {
                m_RequestDefaultResolution = true;
            }
        }

        private void FindSupportedOrientations()
        {
            m_SupportedOrientations = new Dictionary<ScreenOrientation, OrientationData>();
            foreach (var o in m_Screen.orientations)
            {
                m_SupportedOrientations.Add(o.orientation, o);
            }
        }

        private void ApplyAutoRotation()
        {
            if (!m_AutoRotation) return;

            if (m_DeviceOrientation != m_RequestedOrientation && m_SupportedOrientations.ContainsKey(m_DeviceOrientation) && m_AllowedAutoRotation[m_DeviceOrientation])
            {
                RequestOrientation(m_DeviceOrientation);
            }
        }

        private void RequestOrientation(ScreenOrientation orientation)
        {
            m_RequestedOrientation = orientation;
        }

        private void SetAutoRotationOrientation(ScreenOrientation orientation, bool value)
        {
            m_AllowedAutoRotation[orientation] = value;

            if (!m_AutoRotation)
            {
                OnAllowedOrientationChanged?.Invoke();
                return;
            }

            // If the current auto rotation is disabled we need to rotate to another allowed orientation
            if (!value && orientation == m_RequestedOrientation)
            {
                SetFirstAvailableAutoOrientation();
            }
            else if (value)
            {
                ApplyAutoRotation();
            }

            OnAllowedOrientationChanged?.Invoke();
        }

        private void SetFirstAvailableAutoOrientation()
        {
            foreach (var newOrientation in m_SupportedOrientations.Keys)
            {
                if (m_AllowedAutoRotation[newOrientation])
                {
                    RequestOrientation(newOrientation);
                    return;
                }
            }
        }

        public void ApplyChanges()
        {
            var updateSafeArea = false;

            var orientationEvent = false;
            var resolutionEvent = false;
            var fullScreenEvent = false;
            var screenSpaceSafeAreaEvent = false;
            var insetsEvent = false;

            if (m_RequestedOrientation != m_RenderedOrientation)
            {
                if (m_RequestedOrientation.IsLandscape() != m_RenderedOrientation.IsLandscape())
                {
                    // Swap resolution Width and Height if changing from Portrait to Landscape or vice versa
                    if(m_WasResolutionSet)
                        (m_RequestedHeight, m_RequestedWidth) = (m_RequestedWidth, m_RequestedHeight);
                    else
                        m_RequestDefaultResolution = true;
                }

                m_RenderedOrientation = m_RequestedOrientation;
                orientationEvent = true;
                m_RequestInsetUpdate = true;
                updateSafeArea = true;
            }

            if(m_RequestedFullScreen != m_IsFullScreen)
            {
                m_IsFullScreen = m_RequestedFullScreen;
                m_RequestInsetUpdate = true;

                // We only change the resolution if we never set the resolution by calling Screen.SetResolution().
                if (!m_WasResolutionSet)
                {
                    m_RequestDefaultResolution = true;
                }

                updateSafeArea = true;
                fullScreenEvent = true;
            }

            if (m_RequestInsetUpdate)
            {
                CalculateInsets();
                insetsEvent = true;
            }

            if((m_RequestedWidth != m_CurrentWidth || m_RequestedHeight != m_CurrentHeight) && m_WasResolutionSet)
            {
                m_CurrentWidth = m_RequestedWidth;
                m_CurrentHeight = m_RequestedHeight;
                updateSafeArea = true;
                resolutionEvent = true;
            }
            else if (m_RequestDefaultResolution)
            {
                CalculateResolutionWithInsets();
                updateSafeArea = true;
                resolutionEvent = true;
            }

            if (updateSafeArea)
            {
                CalculateSafeAreaAndCutouts();
                screenSpaceSafeAreaEvent = true;
            }

            if(orientationEvent)
                OnOrientationChanged?.Invoke();
            if(resolutionEvent)
                OnResolutionChanged?.Invoke(m_CurrentWidth, m_CurrentHeight);
            if(fullScreenEvent)
                OnFullScreenChanged?.Invoke(m_IsFullScreen);
            if(screenSpaceSafeAreaEvent)
                OnScreenSpaceSafeAreaChanged?.Invoke(ScreenSpaceSafeArea);
            if(insetsEvent)
                OnInsetsChanged?.Invoke(Insets);

            m_RequestDefaultResolution = false;
            m_RequestedOrientation = m_RenderedOrientation;
            m_RequestedHeight = m_CurrentHeight;
            m_RequestedWidth = m_CurrentWidth;
            m_RequestInsetUpdate = false;
        }

        private void CalculateSafeAreaAndCutouts()
        {
            var orientationData = m_SupportedOrientations[m_RenderedOrientation];
            var safeArea = orientationData.safeArea;
            Rect onScreenSafeArea = new Rect();

            // Calculating where on the screen to draw safe area
            onScreenSafeArea = safeArea;
            switch (m_RenderedOrientation)
            {
                case ScreenOrientation.Portrait:
                    onScreenSafeArea.y = m_Screen.height - safeArea.height - safeArea.y;
                    break;
                case ScreenOrientation.PortraitUpsideDown:
                    break;
                case ScreenOrientation.LandscapeLeft:
                    onScreenSafeArea.y = safeArea.x;
                    onScreenSafeArea.x = safeArea.y;
                    onScreenSafeArea.height = safeArea.width;
                    onScreenSafeArea.width = safeArea.height;
                    break;
                case ScreenOrientation.LandscapeRight:
                    onScreenSafeArea.y = m_Screen.height - safeArea.width - safeArea.x;
                    onScreenSafeArea.x = m_Screen.width - safeArea.height - safeArea.y;
                    onScreenSafeArea.width = safeArea.height;
                    onScreenSafeArea.height = safeArea.width;
                    break;
            }

            if (!m_IsFullScreen)
            {
                switch (m_RenderedOrientation)
                {
                    case ScreenOrientation.PortraitUpsideDown:
                        onScreenSafeArea.yMin += m_Screen.navigationBarHeight;
                        break;
                    case ScreenOrientation.LandscapeLeft:
                    case ScreenOrientation.LandscapeRight:
                    case ScreenOrientation.Portrait:
                        onScreenSafeArea.yMax -= m_Screen.navigationBarHeight;
                        break;
                }
            }

            ScreenSpaceSafeArea = onScreenSafeArea;

            var screenWidthInOrientation = IsRenderingLandscape ? m_Screen.height : m_Screen.width;
            var screenHeightInOrientation = IsRenderingLandscape ? m_Screen.width : m_Screen.height;

            // The inverse of safe area, that is the size of borders excluded from the safe area.
            // It is more convenient to scale these borders to correct resolution than to scale safe area rect.
            var unsafeBorders = new Vector4()
            {
                x = safeArea.x,
                y = screenHeightInOrientation - safeArea.height - safeArea.y,
                z = screenWidthInOrientation - safeArea.width - safeArea.x,
                w = safeArea.y
            };

            // Need to exclude unsafe area hidden by insets. It is obscured by the inset and therefore disappears.
            var insetsInOrientation = InsetsInCurrentOrientation;
            unsafeBorders -= insetsInOrientation;

            // A negative unsafe border can happen because inset encroaches on the safe area. In other words, unsafe border is smaller than the inset.
            // In these cases the safe area will border the inset directly on that side of the screen, so unsafe border is clamped to 0.
            unsafeBorders.x = Mathf.Clamp(unsafeBorders.x, 0, float.MaxValue);
            unsafeBorders.y = Mathf.Clamp(unsafeBorders.y, 0, float.MaxValue);
            unsafeBorders.z = Mathf.Clamp(unsafeBorders.z, 0, float.MaxValue);
            unsafeBorders.w = Mathf.Clamp(unsafeBorders.w, 0, float.MaxValue);

            // This is the resolution of the part of the screen where game rendering is occuring, it might be different than the actual rendering resolution.
            var renderAreaWidth = screenWidthInOrientation - insetsInOrientation.x - insetsInOrientation.z;
            var renderAreaHeight = screenHeightInOrientation - insetsInOrientation.y - insetsInOrientation.w;

            var resolutionWidthScale = m_CurrentWidth / renderAreaWidth;
            var resolutionHeightScale = m_CurrentHeight / renderAreaHeight;

            unsafeBorders.x *= resolutionWidthScale;
            unsafeBorders.y *= resolutionHeightScale;
            unsafeBorders.z *= resolutionWidthScale;
            unsafeBorders.w *= resolutionHeightScale;

            m_CurrentSafeArea = new Rect(Mathf.Round(unsafeBorders.x), Mathf.Round(unsafeBorders.w), Mathf.Round(m_CurrentWidth - unsafeBorders.x - unsafeBorders.z), Mathf.Round(m_CurrentHeight - unsafeBorders.y - unsafeBorders.w));

            if (orientationData.cutouts == null || orientationData.cutouts.Length == 0)
            {
                m_CurrentCutouts = new Rect[0];
            }
            else
            {
                // Calculating the cutouts and adding the ones that are inside the rendering area.
                List<Rect> currentCutouts = new List<Rect>();

                for (int i = 0; i < orientationData.cutouts.Length; ++i)
                {
                    var cutout = orientationData.cutouts[i];
                    var currentCutout = new Rect(Mathf.Round((cutout.x - insetsInOrientation.x) * resolutionWidthScale), Mathf.Round((cutout.y - insetsInOrientation.w) * resolutionHeightScale), Mathf.Round(cutout.width * resolutionWidthScale), Mathf.Round(cutout.height * resolutionHeightScale));

                    // Negative coordinates can happen if the cutout overlaps the inset. In other words, if the cutout is outside the rendering area.
                    if (currentCutout.x >= 0 && currentCutout.y >= 0 && currentCutout.xMax <= m_CurrentWidth && currentCutout.yMax <= m_CurrentHeight)
                    {
                        currentCutouts.Add(currentCutout);
                    }
                }

                m_CurrentCutouts = currentCutouts.ToArray();
            }
        }

        // Insets are parts of the screen that are outside of unity rendering area, like navigation bar in windowed mode. Insets are only possible on Android at the moment.
        private void CalculateInsets()
        {
            if (!m_DeviceInfo.IsAndroidDevice())
                return;

            var safeArea = m_SupportedOrientations[ScreenOrientation.Portrait].safeArea;
            var inset = Vector4.zero;

            if (!m_IsRenderingOutsideSafeArea)
            {
                inset = new Vector4
                {
                    x = safeArea.x,
                    y = m_Screen.height - safeArea.height - safeArea.y,
                    z = m_Screen.width - safeArea.width - safeArea.x,
                    w = safeArea.y
                };
            }

            if (!m_IsFullScreen)
            {
                switch (m_RenderedOrientation)
                {
                    case ScreenOrientation.Portrait:
                    case ScreenOrientation.LandscapeLeft:
                    case ScreenOrientation.LandscapeRight:
                        inset.w += m_Screen.navigationBarHeight;
                        break;
                    case ScreenOrientation.PortraitUpsideDown:
                        if (m_IsRenderingOutsideSafeArea)
                        {
                            inset.y = m_Screen.height - safeArea.height - safeArea.y;
                        }

                        inset.y += m_Screen.navigationBarHeight;

                        break;
                }
            }
            Insets = inset;
        }

        private void CalculateResolutionWithInsets()
        {
            var screenWidthInOrientation = IsRenderingLandscape ? m_Screen.height : m_Screen.width;
            var screenHeightInOrientation = IsRenderingLandscape ? m_Screen.width : m_Screen.height;

            var insetsInOrientation = InsetsInCurrentOrientation;

            var widthInOrientation = screenWidthInOrientation - insetsInOrientation.x - insetsInOrientation.z;
            var heightInOrientation = screenHeightInOrientation - insetsInOrientation.y - insetsInOrientation.w;

            var dpiRatio = 1f;
            if (m_PlayerSettings.resolutionScalingMode == ResolutionScalingMode.FixedDpi && m_PlayerSettings.targetDpi < m_Screen.dpi)
                dpiRatio = m_PlayerSettings.targetDpi / m_Screen.dpi;

            m_CurrentWidth = Mathf.RoundToInt(widthInOrientation * dpiRatio);
            m_CurrentHeight = Mathf.RoundToInt(heightInOrientation * dpiRatio);
        }

        public void Enable()
        {
            ShimManager.UseShim(this);
        }

        public void Disable()
        {
            ShimManager.RemoveShim(this);
        }

        public new void Dispose()
        {
            Disable();
        }

        #region ShimBase Overrides

        public override int width => m_CurrentWidth;

        public override int height => m_CurrentHeight;

        public override Rect safeArea => m_CurrentSafeArea;

        public override Rect[] cutouts => m_CurrentCutouts;

        public override float dpi => m_Screen.dpi;

        public override Resolution currentResolution => new Resolution() { width = m_CurrentWidth, height = m_CurrentHeight };

        public override Resolution[] resolutions => new[] { currentResolution };

        public override ScreenOrientation orientation
        {
            get => m_RenderedOrientation;
            set
            {
                if (value == ScreenOrientation.AutoRotation)
                {
                    m_AutoRotation = true;
                    ApplyAutoRotation();
                }
                else if (m_SupportedOrientations.ContainsKey(value))
                {
                    m_AutoRotation = false;
                    RequestOrientation(value);
                }
            }
        }

        public override bool autorotateToPortrait
        {
            get => m_AllowedAutoRotation[ScreenOrientation.Portrait];
            set => SetAutoRotationOrientation(ScreenOrientation.Portrait, value);
        }

        public override bool autorotateToPortraitUpsideDown
        {
            get => m_AllowedAutoRotation[ScreenOrientation.PortraitUpsideDown];
            set => SetAutoRotationOrientation(ScreenOrientation.PortraitUpsideDown, value);
        }

        public override bool autorotateToLandscapeLeft
        {
            get => m_AllowedAutoRotation[ScreenOrientation.LandscapeLeft];
            set => SetAutoRotationOrientation(ScreenOrientation.LandscapeLeft, value);
        }

        public override bool autorotateToLandscapeRight
        {
            get => m_AllowedAutoRotation[ScreenOrientation.LandscapeRight];
            set => SetAutoRotationOrientation(ScreenOrientation.LandscapeRight, value);
        }

        public override void SetResolution(int width, int height, FullScreenMode fullScreenMode, int refreshRate)
        {
            m_WasResolutionSet = true;

            if (width > k_MaxResolution || height > k_MaxResolution || width < 0 || height < 0)
            {
                Debug.LogError($"Failed to change resolution. Make sure that both width and height are at least 0 and less than {k_MaxResolution}.");
                return;
            }

            if (width == 0 && height == 0)
            {
                m_WasResolutionSet = false;
                m_RequestDefaultResolution = true;
                return;
            }

            if (width == 0)
                width = 1;
            else if (height == 0)
                height = 1;

            m_RequestedWidth = width;
            m_RequestedHeight = height;

            fullScreen = (fullScreenMode != FullScreenMode.Windowed); // Tested on Pixel 2 that all other three types go into full screen mode.
        }

        public override bool fullScreen
        {
            get => m_IsFullScreen;
            set
            {
                if (!m_DeviceInfo.IsAndroidDevice() || m_IsFullScreen == value)
                    return;

                m_RequestedFullScreen = value;
            }
        }

        public override FullScreenMode fullScreenMode
        {
            get => fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            set => fullScreen = (value != FullScreenMode.Windowed);
        }

        #endregion
    }
}
