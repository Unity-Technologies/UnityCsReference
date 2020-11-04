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
        private DeviceInfo m_DeviceInfo;
        private ScreenData m_Screen;

        private bool m_AutoRotation;
        public bool AutoRotation => m_AutoRotation;

        private ScreenOrientation m_RenderedOrientation = ScreenOrientation.Portrait;
        private Dictionary<ScreenOrientation, bool> m_AllowedAutoRotation;
        private Dictionary<ScreenOrientation, OrientationData> m_SupportedOrientations;

        private float m_DpiRatio = 1;

        private Rect m_CurrentSafeArea;
        private Rect[] m_CurrentCutouts;

        private bool m_WasResolutionSet = false;
        private int m_CurrentWidth;
        private int m_CurrentHeight;

        public int Width => m_CurrentWidth;
        public int Height => m_CurrentHeight;

        private bool m_IsFullScreen;
        public Vector4 Insets { get; private set; }

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

        public event Action<bool> OnOrientationChanged;
        public event Action OnAllowedOrientationChanged;
        public event Action<int, int> OnResolutionChanged;
        public event Action<bool> OnFullScreenChanged;
        public event Action<Vector4> OnInsetsChanged;
        public event Action<Rect> OnScreenSpaceSafeAreaChanged;

        public ScreenSimulation(DeviceInfo device, SimulationPlayerSettings playerSettings)
        {
            m_DeviceInfo = device;
            m_Screen = device.screens[0];

            m_SupportedOrientations = new Dictionary<ScreenOrientation, OrientationData>();
            foreach (var o in m_Screen.orientations)
            {
                m_SupportedOrientations.Add(o.orientation, o);
            }

            m_AllowedAutoRotation = new Dictionary<ScreenOrientation, bool>();
            m_AllowedAutoRotation.Add(ScreenOrientation.Portrait, playerSettings.allowedPortrait);
            m_AllowedAutoRotation.Add(ScreenOrientation.PortraitUpsideDown, playerSettings.allowedPortraitUpsideDown);
            m_AllowedAutoRotation.Add(ScreenOrientation.LandscapeLeft, playerSettings.allowedLandscapeLeft);
            m_AllowedAutoRotation.Add(ScreenOrientation.LandscapeRight, playerSettings.allowedLandscapeRight);

            // Set the full screen mode.
            m_IsFullScreen = !m_DeviceInfo.IsAndroidDevice() || playerSettings.androidStartInFullscreen;

            // Calculate the right orientation.
            var settingOrientation = SimulatorUtilities.ToScreenOrientation(playerSettings.defaultOrientation);
            if (settingOrientation == ScreenOrientation.AutoRotation)
            {
                m_AutoRotation = true;
                SetFirstAvailableAutoOrientation();
            }
            else if (m_SupportedOrientations.ContainsKey(settingOrientation))
            {
                m_AutoRotation = false;
                ForceNewOrientation(settingOrientation);
            }
            else
            {
                // The real iPhone X responds to this absolute corner case by crashing, we will not do that.
                m_AutoRotation = false;
                ForceNewOrientation(m_SupportedOrientations.Keys.ToArray()[0]);
            }

            // Calculate the right resolution.
            var initWidth = m_Screen.width;
            var initHeight = m_Screen.height;
            if (playerSettings.resolutionScalingMode == ResolutionScalingMode.FixedDpi && playerSettings.targetDpi < m_Screen.dpi)
            {
                m_DpiRatio = playerSettings.targetDpi / m_Screen.dpi;
                initWidth = (int)(initWidth * m_DpiRatio);
                initHeight = (int)(initHeight * m_DpiRatio);
            }
            m_CurrentWidth = IsRenderingLandscape ? initHeight : initWidth;
            m_CurrentHeight = IsRenderingLandscape ? initWidth : initHeight;

            if (!m_IsFullScreen)
            {
                CalculateScreenResolutionForScreenMode(out m_CurrentWidth, out m_CurrentHeight);
                CalculateInsets();
            }
            CalculateSafeAreaAndCutouts();

            ShimManager.UseShim(this);
        }

        private void ApplyAutoRotation()
        {
            if (!m_AutoRotation) return;

            if (m_DeviceOrientation != m_RenderedOrientation && m_SupportedOrientations.ContainsKey(m_DeviceOrientation) && m_AllowedAutoRotation[m_DeviceOrientation])
            {
                ForceNewOrientation(m_DeviceOrientation);
            }
            else
            {
                OnOrientationChanged?.Invoke(m_AutoRotation);
            }
        }

        private void ForceNewOrientation(ScreenOrientation orientation)
        {
            // Swap resolution Width and Height if changing from Portrait to Landscape or vice versa
            if ((orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.PortraitUpsideDown) && IsRenderingLandscape ||
                (orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.LandscapeRight) && !IsRenderingLandscape)
            {
                var temp = m_CurrentHeight;
                m_CurrentHeight = m_CurrentWidth;
                m_CurrentWidth = temp;
                OnResolutionChanged?.Invoke(m_CurrentWidth, m_CurrentHeight);
            }
            m_RenderedOrientation = orientation;
            OnOrientationChanged?.Invoke(m_AutoRotation);
            CalculateInsets();
            CalculateSafeAreaAndCutouts();
        }

        private void CalculateSafeAreaAndCutouts()
        {
            var safeArea = m_SupportedOrientations[m_RenderedOrientation].safeArea;
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
            OnScreenSpaceSafeAreaChanged?.Invoke(ScreenSpaceSafeArea);

            int scaledHeight = 0;
            var scaledNavigationBarHeight = Mathf.RoundToInt(m_DpiRatio * m_Screen.navigationBarHeight);
            if (!m_WasResolutionSet)
            {
                scaledHeight = scaledNavigationBarHeight;
                if (m_SupportedOrientations.ContainsKey(ScreenOrientation.Portrait))
                    scaledHeight += Mathf.RoundToInt(m_DpiRatio * (m_Screen.height - m_SupportedOrientations[ScreenOrientation.Portrait].safeArea.height));
            }

            // Always consider the full screen mode width/height to scale the safe area & cutouts.
            float xScale, yScale;
            if (IsRenderingLandscape)
            {
                xScale = (float)(m_CurrentWidth + (m_IsFullScreen ? 0 : scaledHeight)) / m_Screen.height;
                yScale = (float)m_CurrentHeight / m_Screen.width;
            }
            else
            {
                xScale = (float)m_CurrentWidth / m_Screen.width;
                yScale = (float)(m_CurrentHeight + (m_IsFullScreen ? 0 : scaledHeight)) / m_Screen.height;
            }

            // Scale safe area.
            var odd = m_SupportedOrientations[m_RenderedOrientation];
            var sa = odd.safeArea;
            if (m_IsFullScreen)
            {
                m_CurrentSafeArea = new Rect(Mathf.Round(sa.x * xScale), Mathf.Round(sa.y * yScale), Mathf.Round(sa.width * xScale), Mathf.Round(sa.height * yScale));
            }
            else
            {
                if (m_WasResolutionSet)
                    m_CurrentSafeArea = new Rect(0, 0, m_CurrentWidth, m_CurrentHeight); // Set the safe area to current resolution in windowed mode with resolution set.
                else
                    m_CurrentSafeArea = new Rect(0, 0, Mathf.Round(sa.width * xScale), Mathf.Round(sa.height * yScale));
            }

            // Consider the navigation bar height if it's windowed mode without resolution set.
            if (!m_IsFullScreen && !m_WasResolutionSet)
            {
                switch (m_RenderedOrientation)
                {
                    case ScreenOrientation.Portrait:
                    case ScreenOrientation.PortraitUpsideDown:
                        m_CurrentSafeArea.height -= scaledNavigationBarHeight;
                        break;
                    case ScreenOrientation.LandscapeLeft:
                    case ScreenOrientation.LandscapeRight:
                        m_CurrentSafeArea.width -= scaledNavigationBarHeight;
                        break;
                }
            }

            // For windowed mode, let's return empty cutouts for now.
            if (!m_IsFullScreen)
            {
                m_CurrentCutouts = new Rect[0];
                return;
            }

            // Scale cutouts.
            if (odd.cutouts?.Length > 0)
            {
                m_CurrentCutouts = new Rect[odd.cutouts.Length];
                for (int i = 0; i < odd.cutouts.Length; ++i)
                {
                    var cutout = odd.cutouts[i];
                    m_CurrentCutouts[i] = new Rect(Mathf.Round(cutout.x * xScale), Mathf.Round(cutout.y * yScale), Mathf.Round(cutout.width * xScale), Mathf.Round(cutout.height * yScale));
                }
            }
            else
                m_CurrentCutouts = new Rect[0];
        }

        // Insets are parts of the screen that are outside of unity rendering area, like navigation bar in windowed mode. Insets are only possible on Android at the moment.
        private void CalculateInsets()
        {
            if (!m_DeviceInfo.IsAndroidDevice())
                return;

            var inset = Vector4.zero;
            if (!m_IsFullScreen)
            {
                switch (m_RenderedOrientation)
                {
                    case ScreenOrientation.Portrait:
                    case ScreenOrientation.LandscapeLeft:
                    case ScreenOrientation.LandscapeRight:
                        inset = new Vector4(0, m_Screen.height - m_SupportedOrientations[ScreenOrientation.Portrait].safeArea.height, 0, m_Screen.navigationBarHeight);
                        break;
                    case ScreenOrientation.PortraitUpsideDown:
                        var topInset = m_Screen.height - m_SupportedOrientations[ScreenOrientation.Portrait].safeArea.height + m_Screen.navigationBarHeight;
                        inset = new Vector4(0, topInset, 0, 0);
                        break;
                }
            }
            Insets = inset;
            OnInsetsChanged?.Invoke(inset);
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
            if (!value && orientation == m_RenderedOrientation)
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
                    ForceNewOrientation(newOrientation);
                }
            }
        }

        private void SetResolution(int width, int height)
        {
            // For now limit width & height from 1 to 9999.
            if (width < 1 || width > 9999 || height < 1 || height > 9999)
            {
                Debug.LogError("Failed to change resolution. Make sure that both width and height are between 1 and 9999.");
                return;
            }

            m_CurrentWidth = width;
            m_CurrentHeight = height;
            CalculateSafeAreaAndCutouts();

            OnResolutionChanged?.Invoke(m_CurrentWidth, m_CurrentHeight);
        }

        private void CalculateScreenResolutionForScreenMode(out int width, out int height)
        {
            width = m_CurrentWidth;
            height = m_CurrentHeight;


            var portraitSafeArea = m_SupportedOrientations[ScreenOrientation.Portrait].safeArea;
            var noFullScreenHeight = portraitSafeArea.height + portraitSafeArea.y - m_Screen.navigationBarHeight;
            var scale = m_IsFullScreen ? m_Screen.height / noFullScreenHeight : noFullScreenHeight / m_Screen.height;
            switch (m_RenderedOrientation)
            {
                case ScreenOrientation.Portrait:
                case ScreenOrientation.PortraitUpsideDown:
                    height = Convert.ToInt32(Math.Round(height * scale));
                    break;
                case ScreenOrientation.LandscapeLeft:
                case ScreenOrientation.LandscapeRight:
                    width = Convert.ToInt32(Math.Round(width * scale));
                    break;
            }
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
                    ForceNewOrientation(value);
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
            SetResolution(width, height);
            fullScreen = (fullScreenMode != FullScreenMode.Windowed); // Tested on Pixel 2 that all other three types go into full screen mode.
        }

        public override bool fullScreen
        {
            get => m_IsFullScreen;
            set
            {
                if (!m_DeviceInfo.IsAndroidDevice() || m_IsFullScreen == value)
                    return;

                m_IsFullScreen = value;
                CalculateInsets();

                // We only change the resolution if we never set the resolution by calling Screen.SetResolution().
                if (!m_WasResolutionSet)
                {
                    CalculateScreenResolutionForScreenMode(out int tempWidth, out int tempHeight);
                    SetResolution(tempWidth, tempHeight);
                }
                else
                {
                    CalculateSafeAreaAndCutouts();
                }

                OnFullScreenChanged?.Invoke(m_IsFullScreen);
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
