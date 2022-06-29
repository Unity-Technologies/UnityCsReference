// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEditor.Modules;
using System.Globalization;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditorInternal.VR;
using UnityEngine.XR;
using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;
using UnityEditor.ShortcutManagement;

/*
The main GameView can be in the following states when entering playmode.
These states should be tested when changing the behavior when entering playmode.


 GameView docked and visible
 GameView docked and visible and maximizeOnPlay
 GameView docked and hidden
 GameView docked and hidden and maximizeOnPlay
 GameView docked and hidden and resized
 Maximized GameView (maximized from start)
 No GameView but a SceneView exists
 No GameView and no SceneView exists
 Floating GameView in separate window
*/

namespace UnityEditor
{
    [EditorWindowTitle(title = "Game", useTypeNameAsIconName = true)]
    internal class GameView : PlayModeView, IHasCustomMenu, IGameViewSizeMenuUser, IGameViewOnPlayMenuUser
    {
        const int kScaleSliderMinWidth = 30;
        const int kScaleSliderMaxWidth = 150;
        const int kScaleSliderSnapThreshold = 4;
        const int kScaleLabelWidth = 40;
        readonly Vector2 kWarningSize = new Vector2(400f, 140f);
        readonly Color kClearBlack = new Color(0, 0 , 0, 0);
        const float kMinScale = 1f;
        const float kMaxScale = 5f;
        const float kScrollZoomSnapDelay = 0.2f;
        // This will save the previous state which will be useful in case of platform switch
        int prevSizeGroupType;

        float minScale
        {
            get
            {
                var clampedMinScale = Mathf.Min(kMinScale, ScaleThatFitsTargetInView(targetRenderSize, viewInWindow.size));
                if (m_LowResolutionForAspectRatios[(int)currentSizeGroupType] && currentGameViewSize.sizeType == GameViewSizeType.AspectRatio)
                    clampedMinScale = Mathf.Max(clampedMinScale, Mathf.Floor(EditorGUIUtility.pixelsPerPoint));
                return clampedMinScale;
            }
        }
        float maxScale
        {
            get { return Mathf.Max(kMaxScale * EditorGUIUtility.pixelsPerPoint, ScaleThatFitsTargetInView(targetRenderSize, viewInWindow.size)); }
        }

        [SerializeField] bool m_VSyncEnabled;
        [SerializeField] bool m_PlayFocused;
        [SerializeField] bool m_Gizmos;
        [SerializeField] bool m_Stats;
        [SerializeField] int[] m_SelectedSizes = new int[0]; // We have a selection for each game view size group (e.g standalone, android etc)

        [SerializeField] ZoomableArea m_ZoomArea;
        [SerializeField] float m_defaultScale = -1f;
        bool m_TargetClamped;

        [SerializeField] Vector2 m_LastWindowPixelSize;

        [SerializeField] bool m_ClearInEditMode = true;
        [SerializeField] bool m_NoCameraWarning = true;
        [SerializeField] bool[] m_LowResolutionForAspectRatios = new bool[0];
        [SerializeField] int m_XRRenderMode = 0;
        [SerializeField] RenderTexture m_RenderTexture;
        [SerializeField] bool m_showToolbar = true;
        [SerializeField] bool m_showToolbarOnFullscreen = false;

        int m_SizeChangeID = int.MinValue;

        List<XRDisplaySubsystem> m_DisplaySubsystems = new List<XRDisplaySubsystem>();
        PlayModeStateChange latestState = PlayModeStateChange.EnteredEditMode;

        internal override bool liveReloadPreferenceDefault => true;

        internal static class Styles
        {
            public static GUIContent gizmosContent = EditorGUIUtility.TrTextContent("Gizmos");
            public static GUIContent zoomSliderContent = EditorGUIUtility.TrTextContent("Scale", "Size of the game view on the screen.");
            public static GUIContent vsyncContent = EditorGUIUtility.TrTextContent("VSync");
            public static GUIContent muteContent = EditorGUIUtility.TrTextContent("Mute Audio");
            public static GUIContent statsContent = EditorGUIUtility.TrTextContent("Stats");
            public static GUIContent frameDebuggerOnContent = EditorGUIUtility.TrTextContent("Frame Debugger On");
            public static GUIContent loadRenderDocContent = EditorGUIUtility.TrTextContent(UnityEditor.RenderDocUtil.loadRenderDocLabel);
            public static GUIContent noCameraWarningContextMenuContent = EditorGUIUtility.TrTextContent("Warn if No Cameras Rendering");
            public static GUIContent clearEveryFrameContextMenuContent = EditorGUIUtility.TrTextContent("Clear Every Frame in Edit Mode");
            public static GUIContent lowResAspectRatiosContextMenuContent = EditorGUIUtility.TrTextContent("Low Resolution Aspect Ratios");
            public static GUIContent metalFrameCaptureContent = EditorGUIUtility.TrIconContent("FrameCapture", "Capture the current view and open in Xcode frame debugger");
            public static GUIContent suppressMessage = EditorGUIUtility.TrTextContent("This GameView is suppressed from rendering during Fullscreen.");
            public static GUIContent disableFullscreenMainDisplayFormatContent = EditorGUIUtility.TrTextContent("Press {0} to exit fullscreen.");

            public static GUIContent renderdocContent;
            public static GUIStyle gameViewBackgroundStyle;

            // The ordering here must correspond with ordering in UnityEngine.XR.GameViewRenderMode
            public static GUIContent[] xrRenderingModes = { EditorGUIUtility.TextContent("Left Eye|Left eye is displayed in play mode."), EditorGUIUtility.TextContent("Right Eye|Right eye is displayed in play mode."), EditorGUIUtility.TextContent("Both Eyes|Both eyes are displayed in play mode."), EditorGUIUtility.TextContent("Occlusion Mesh|Both eyes are displayed in play mode along with the occlusion mesh.") };

            static Styles()
            {
                gameViewBackgroundStyle = "GameViewBackground";
                renderdocContent = EditorGUIUtility.TrIconContent("FrameCapture", UnityEditor.RenderDocUtil.openInRenderDocLabel);
            }

            public static readonly GUIStyle largeCenteredText = new GUIStyle(EditorStyles.label)
            {
                name = "large-centered-text",
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(4, 4, 1, 4),
                padding = new RectOffset(4, 4, 4, 4),
                fontSize = 42
            };
            public static readonly GUIStyle smallCenteredText = new GUIStyle(EditorStyles.label)
            {
                name = "small-centered-text",
                richText = true,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(4, 4, 1, 4),
                padding = new RectOffset(4, 4, 4, 4),
                fontSize = 12
            };
        }


        static double s_LastScrollTime;

        public GameView()
        {
            autoRepaintOnSceneChange = true;
            InitializeZoomArea();
            playModeViewName = "GameView";
            clearColor = kClearBlack;
            showGizmos = m_Gizmos;
            targetDisplay = 0;
            targetSize = new Vector2(640f, 480f);
            textureFilterMode = FilterMode.Point;
            textureHideFlags = HideFlags.HideAndDontSave;
        }

        internal override void ApplyEditorDisplayFullscreenSetting(IPlayModeViewFullscreenSettings settings)
        {
            var gameviewSetting = settings as GameViewFullscreenSettings;
            if (gameviewSetting == null)
            {
                return;
            }

            isFullscreen = true;

            var changed = false;

            if (ModuleManager.ShouldShowMultiDisplayOption())
            {
                if (targetDisplay != gameviewSetting.DisplayNumber)
                {
                    targetDisplay = gameviewSetting.DisplayNumber;
                    changed = true;
                }
            }

            if (m_showToolbarOnFullscreen != gameviewSetting.ShowToolbar)
            {
                m_showToolbarOnFullscreen = gameviewSetting.ShowToolbar;
                changed = true;
            }

            if (m_Stats != gameviewSetting.ShowStats)
            {
                m_Stats = gameviewSetting.ShowStats;
                changed = true;
            }

            if (m_Gizmos != gameviewSetting.ShowGizmos)
            {
                m_Gizmos = gameviewSetting.ShowGizmos;
                changed = true;
            }

            if (canVSync)
            {
                if (m_VSyncEnabled != gameviewSetting.VsyncEnabled)
                {
                    m_VSyncEnabled = gameviewSetting.VsyncEnabled;
                    SetVSync(m_VSyncEnabled);
                    changed = true;
                }
            }

            if (selectedSizeIndex != gameviewSetting.SelectedSizeIndex)
            {
                selectedSizeIndex = gameviewSetting.SelectedSizeIndex;
                changed = true;
            }

            if (changed)
            {
                UpdateZoomAreaAndParent();
            }
        }

        private bool canVSync
        {
            get
            {
                var gfxDeviceType = SystemInfo.graphicsDeviceType;
                return
                    gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal ||
                    gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan ||
                    gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 ||
                    gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D12 ||
                    gfxDeviceType == UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore;
            }
        }

        public bool lowResolutionForAspectRatios
        {
            get
            {
                EnsureSelectedSizeAreValid();
                return m_LowResolutionForAspectRatios[(int)currentSizeGroupType];
            }
            set
            {
                EnsureSelectedSizeAreValid();
                if (value != m_LowResolutionForAspectRatios[(int)currentSizeGroupType])
                {
                    m_LowResolutionForAspectRatios[(int)currentSizeGroupType] = value;
                    UpdateZoomAreaAndParent();
                }
            }
        }

        public bool forceLowResolutionAspectRatios => EditorGUIUtility.pixelsPerPoint == 1f;

        public bool vSyncEnabled
        {
            get { return m_VSyncEnabled; }
            set
            {
                if (value == m_VSyncEnabled)
                    return;

                SetVSync(value);
                m_VSyncEnabled = value;
            }
        }

        public bool playFocused
        {
            get { return m_PlayFocused; }
            set { m_PlayFocused = value; }
        }

        public int selectedSizeIndex
        {
            get
            {
                EnsureSelectedSizeAreValid();
                return m_SelectedSizes[(int)currentSizeGroupType];
            }
            set
            {
                EnsureSelectedSizeAreValid();
                m_SelectedSizes[(int)currentSizeGroupType] = value;
            }
        }

        //This has been added to accomodate the test in PR for case 991291.
        internal void SetTargetDisplay(int id)
        {
            if (id < 0 || id > 7)
                return;
            targetDisplay = id;
            SetDisplayViewSize(id, targetSize);
        }

        static GameViewSizeGroupType currentSizeGroupType => GameViewSizes.instance.currentGroupType;

        GameViewSize currentGameViewSize => GameViewSizes.instance.currentGroup.GetGameViewSize(selectedSizeIndex);

        Rect GetViewInWindow(Rect pos)
        {
            if (showToolbar)
                return new Rect(0, EditorGUI.kWindowToolbarHeight, pos.width, pos.height - EditorGUI.kWindowToolbarHeight);
            return new Rect(0, 0, pos.width, pos.height);
        }

        Rect GetViewPixelRect(Rect viewRectInWindow)
        {
            return lowResolutionForAspectRatios? viewRectInWindow : EditorGUIUtility.PointsToPixels(viewRectInWindow);
        }

        // The area of the window that the rendered game view is limited to
        Rect viewInWindow
        {
            get
            {
                return GetViewInWindow(position);
            }
        }

        internal Vector2 targetRenderSize // Size of render target in pixels
        {
            get
            {
                var viewPixelRect = GetViewPixelRect(viewInWindow);
                return GameViewSizes.GetRenderTargetSize(viewPixelRect, currentSizeGroupType, selectedSizeIndex, out m_TargetClamped);
            }
        }

        // Area of the render target in zoom content space (it is centered in content space)
        Rect targetInContent
        {
            get
            {
                var targetSizeCached = targetRenderSize;
                return EditorGUIUtility.PixelsToPoints(new Rect(-0.5f * targetSizeCached, targetSizeCached));
            }
        }

        Rect targetInView // Area of the render target in zoom view space
        {
            get
            {
                var targetInContentCached = targetInContent;
                return new Rect(
                    m_ZoomArea.DrawingToViewTransformPoint(targetInContentCached.position),
                    m_ZoomArea.DrawingToViewTransformVector(targetInContentCached.size)
                );
            }
        }

        // The final image needs to be flipped upside-down if we use coordinates with zero at the top
        Rect deviceFlippedTargetInView
        {
            get
            {
                if (!SystemInfo.graphicsUVStartsAtTop)
                    return targetInView;
                else
                {
                    var flippedTarget = targetInView;
                    flippedTarget.y += flippedTarget.height;
                    flippedTarget.height = -flippedTarget.height;
                    return flippedTarget;
                }
            }
        }

        Rect viewInParent // The view area in parent view space
        {
            get
            {
                var viewInParent = viewInWindow;
                var parentBorder = m_Parent.borderSize;
                viewInParent.x += parentBorder.left;
                // Note: DockArea has extra space on top of the tab area which is accounted for strangely.
                // It is stored in borderSize.bottom but it should be added to the top instead
                // @TODO: Make DockArea.borderSize more sensible (requires fixing all usages)
                viewInParent.y += parentBorder.top + parentBorder.bottom;
                return viewInParent;
            }
        }

        Rect targetInParent // Area of the render target in parent view space
        {
            get
            {
                var targetInViewCached = targetInView;
                return new Rect(targetInViewCached.position + viewInParent.position, targetInViewCached.size);
            }
        }

        // Area for warnings such as no cameras rendering
        Rect warningPosition { get { return new Rect((viewInWindow.size - kWarningSize) * 0.5f, kWarningSize); } }

        Vector2 gameMouseOffset { get { return -viewInWindow.position - targetInView.position; } }

        float gameMouseScale { get { return EditorGUIUtility.pixelsPerPoint / m_ZoomArea.scale.y; } }

        private bool showToolbar
        {
            get => isFullscreen ? m_showToolbarOnFullscreen : m_showToolbar;
            set => m_showToolbar = value;
        }

        internal bool drawGizmos
        {
            get => m_Gizmos;
            set => m_Gizmos = value;
        }

        void InitializeZoomArea()
        {
            m_ZoomArea = new ZoomableArea(true, false) {uniformScale = true, upDirection = ZoomableArea.YDirection.Negative};
        }

        private void UndoRedoPerformed()
        {
            Repaint();
        }

        public void OnEnable()
        {
            wantsLessLayoutEvents = true;
            prevSizeGroupType = (int)currentSizeGroupType;
            titleContent = GetLocalizedTitleContent();
            UpdateZoomAreaAndParent();
            showToolbar = ModeService.HasCapability(ModeCapability.GameViewToolbar, true);

            ModeService.modeChanged += OnEditorModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            targetSize = targetRenderSize;

            PlayModeAnalytics.GameViewEnableEvent();
        }

        public void OnDisable()
        {
            ModeService.modeChanged -= OnEditorModeChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            if (m_RenderTexture)
            {
                DestroyImmediate(m_RenderTexture);
            }

            PlayModeAnalytics.GameViewDisableEvent();
        }

        [UsedImplicitly] // This is here because NGUI uses it via reflection (noted in https://confluence.hq.unity3d.com/display/DEV/Game+View+Bucket)
        internal static Vector2 GetSizeOfMainGameView()
        {
            return GetMainPlayModeViewTargetSize();
        }

        private void UpdateZoomAreaAndParent()
        {
            // Configure ZoomableArea for new resolution so that old resolution doesn't restrict scale
            bool oldScaleWasDefault = Mathf.Approximately(m_ZoomArea.scale.y, m_defaultScale);
            ConfigureZoomArea();
            m_defaultScale = DefaultScaleForTargetInView(targetRenderSize, viewInWindow.size);
            if (oldScaleWasDefault)
            {
                m_ZoomArea.SetTransform(Vector2.zero, Vector2.one * m_defaultScale);
                EnforceZoomAreaConstraints();
            }

            m_LastWindowPixelSize = position.size * EditorGUIUtility.pixelsPerPoint;
            EditorApplication.SetSceneRepaintDirty();

            // update the scale according to new resolution
            m_ZoomArea.UpdateZoomScale(maxScale, minScale);
        }

        protected void AllowCursorLockAndHide(bool enable)
        {
            Unsupported.SetAllowCursorLock(enable, Unsupported.DisallowCursorLockReasons.Other);
            Unsupported.SetAllowCursorHide(enable);
        }

        private void OnFocus()
        {
            SetFocus(true);
            targetSize = targetRenderSize;
        }

        private void OnLostFocus()
        {
            // We unlock the cursor when the game view loses focus to allow the user to regain cursor control.
            // Fix for 389362: Ensure that we do not unlock cursor during init of play mode. Because we could
            // be maximizing game view, which causes a lostfocus on the original game view, in these situations we do
            // not unlock cursor.
            if (!EditorApplicationLayout.IsInitializingPlaymodeLayout())
            {
                AllowCursorLockAndHide(false);
            }
            SetFocus(false);
        }

        internal override void OnResized()
        {
            targetSize = targetRenderSize;
        }

        internal override void OnBackgroundViewResized(Rect pos)
        {
            // Should only update the game view size if it's in Aspect Ratio mode, otherwise
            // we keep the static size

            Rect viewInWindow = GetViewInWindow(pos);
            Rect viewPixelRect = GetViewPixelRect(viewInWindow);
            var newTargetSize =
                GameViewSizes.GetRenderTargetSize(viewPixelRect, currentSizeGroupType, selectedSizeIndex, out m_TargetClamped);

            if (newTargetSize == GetDisplayViewSize(targetDisplay) && currentGameViewSize.sizeType != GameViewSizeType.AspectRatio)
                return;
            SetDisplayViewSize(targetDisplay, new Vector2(newTargetSize.x, newTargetSize.y));
            UpdateZoomAreaAndParent();
        }

        // Call when number of available aspects can have changed (after deserialization or gui change)
        private void EnsureSelectedSizeAreValid()
        {
            // Early out if no change was recorded
            if (GameViewSizes.instance.GetChangeID() == m_SizeChangeID)
                return;

            m_SizeChangeID = GameViewSizes.instance.GetChangeID();

            var sizeGroupTypes = System.Enum.GetValues(typeof(GameViewSizeGroupType));
            // Ensure deserialized array is resized if needed
            if (m_SelectedSizes.Length != sizeGroupTypes.Length)
                System.Array.Resize(ref m_SelectedSizes, sizeGroupTypes.Length);

            // Ensure deserialized selection index for each group is within valid range
            foreach (GameViewSizeGroupType groupType in sizeGroupTypes)
            {
                var gvsg = GameViewSizes.instance.GetGroup(groupType);
                var index = (int)groupType;
                m_SelectedSizes[index] = Mathf.Clamp(m_SelectedSizes[index], 0, gvsg.GetTotalCount() - 1);
            }

            // Resize low resolution array as necessary, and fill in new indices with defaults
            var lowResolutionSettingsLength = m_LowResolutionForAspectRatios.Length;
            if (m_LowResolutionForAspectRatios.Length != sizeGroupTypes.Length)
                System.Array.Resize(ref m_LowResolutionForAspectRatios, sizeGroupTypes.Length);
            for (var groupIndex = lowResolutionSettingsLength; groupIndex < sizeGroupTypes.Length; ++groupIndex)
                m_LowResolutionForAspectRatios[groupIndex] = GameViewSizes.DefaultLowResolutionSettingForSizeGroupType((GameViewSizeGroupType)sizeGroupTypes.GetValue(groupIndex));
        }

        private void OnSelectionChange()
        {
            if (m_Gizmos)
                Repaint();
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                ShaderUtil.RequestLoadRenderDoc();
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(Styles.loadRenderDocContent, false, LoadRenderDoc);
            }
            menu.AddItem(Styles.noCameraWarningContextMenuContent, m_NoCameraWarning, ToggleNoCameraWarning);
            menu.AddItem(Styles.clearEveryFrameContextMenuContent, m_ClearInEditMode, ToggleClearInEditMode);
        }

        private void ToggleNoCameraWarning()
        {
            m_NoCameraWarning = !m_NoCameraWarning;
        }

        private void ToggleClearInEditMode()
        {
            m_ClearInEditMode = !m_ClearInEditMode;
        }

        public void SizeSelectionCallback(int indexClicked, object objectSelected)
        {
            if (indexClicked != selectedSizeIndex)
            {
                selectedSizeIndex = indexClicked;
                UpdateZoomAreaAndParent();
                targetSize = targetRenderSize;
                SceneView.RepaintAll();
            }
        }

        void SnapZoomDelayed()
        {
            if (EditorApplication.timeSinceStartup > s_LastScrollTime + kScrollZoomSnapDelay)
            {
                EditorApplication.update -= SnapZoomDelayed;
                SnapZoom(m_ZoomArea.scale.y);
                Repaint();
            }
        }

        void SnapZoom(float newZoom)
        {
            var logScale = Mathf.Log10(newZoom);
            var logMin = Mathf.Log10(minScale);
            var logMax = Mathf.Log10(maxScale);
            var shortestSliderDistance = System.Single.MaxValue;
            if (logScale > logMin && logScale < logMax)
            {
                for (var i = 1; i <= maxScale; ++i)
                {
                    // Snap distance is defined in points, so convert difference back to UI space
                    var sliderDistanceToI = kScaleSliderMaxWidth * Mathf.Abs(logScale - Mathf.Log10(i)) / (logMax - logMin);
                    if (sliderDistanceToI < kScaleSliderSnapThreshold && sliderDistanceToI < shortestSliderDistance)
                    {
                        newZoom = i;
                        shortestSliderDistance = sliderDistanceToI;
                    }
                }
            }

            var areaInMargins = m_ZoomArea.shownAreaInsideMargins;
            var focalPoint = areaInMargins.position + areaInMargins.size * 0.5f;
            m_ZoomArea.SetScaleFocused(focalPoint, Vector2.one * newZoom);
        }

        void DoZoomSlider()
        {
            GUILayout.Label(Styles.zoomSliderContent);
            EditorGUI.BeginChangeCheck();
            // Zooming feels more natural on a log scale
            var logScale = Mathf.Log10(m_ZoomArea.scale.y);
            var logMin = Mathf.Log10(minScale);
            var logMax = Mathf.Log10(maxScale);
            logScale = GUILayout.HorizontalSlider(logScale, logMin, logMax, GUILayout.MaxWidth(kScaleSliderMaxWidth), GUILayout.MinWidth(kScaleSliderMinWidth));
            if (EditorGUI.EndChangeCheck())
            {
                var newZoom = Mathf.Pow(10f, logScale);
                SnapZoom(newZoom);
            }
            var scaleContent = EditorGUIUtility.TempContent(UnityString.Format("{0}x", (m_ZoomArea.scale.y).ToString("G2", CultureInfo.InvariantCulture.NumberFormat)));
            scaleContent.tooltip = Styles.zoomSliderContent.tooltip;
            GUILayout.Label(scaleContent, GUILayout.Width(kScaleLabelWidth));
            scaleContent.tooltip = string.Empty;
        }

        private bool ShouldShowMetalFrameCaptureGUI()
        {
            return FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools)
                || FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument);
        }

        private void MirrorFullscreenToolbarSettings()
        {
            if (enterPlayModeBehavior != EnterPlayModeBehavior.PlayFullscreen)
                return;

            // Even though there is a 1:1 relationship between a fullscreen game view, and the "suppressed" game view that
            // spawned it, it's not easy to distingush which game view controls which sub game view. Rather than brutally
            // enforce the 1:1 relationship allow any game view with the selected fullscreen idx, to control the settings
            // on that fullscreen container window.
            int displayIdx = fullscreenMonitorIdx;
            List<ContainerWindow> fullscreenContainers = EditorFullscreenController.GetFullscreenContainersForDisplayIndex(displayIdx);
            foreach (var cw in fullscreenContainers)
            {
                var fullscreenGameView = (cw.rootView as HostView).actualView as GameView;

                fullscreenGameView.targetDisplay = this.targetDisplay;
                fullscreenGameView.m_ZoomArea = this.m_ZoomArea;
                fullscreenGameView.UpdateZoomAreaAndParent();
                fullscreenGameView.m_Stats = this.m_Stats;
                fullscreenGameView.m_Gizmos = this.m_Gizmos;

            }

        }

        private void DoToolbarGUI()
        {
            if (Event.current.isKey || Event.current.type == EventType.Used)
                return;

            GameViewSizes.instance.RefreshStandaloneAndRemoteDefaultSizes();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (!isFullscreen)
                {
                    EditorGUI.BeginDisabled(enterPlayModeBehavior == EnterPlayModeBehavior.PlayFullscreen);
                    var availableTypes = GetAvailableWindowTypes();
                    if (availableTypes.Count > 1)
                    {
                        var typeNames = availableTypes.Values.ToList();
                        var types = availableTypes.Keys.ToList();
                        int viewIndex = EditorGUILayout.Popup(typeNames.IndexOf(titleContent.text), typeNames.ToArray(),
                            EditorStyles.toolbarPopup,
                            GUILayout.Width(90));
                        EditorGUILayout.Space();
                        if (types[viewIndex] != typeof(GameView))
                        {
                            SwapMainWindow(types[viewIndex]);
                        }
                    }
                    EditorGUI.EndDisabled();
                }

                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    int display = EditorGUILayout.Popup(targetDisplay, DisplayUtility.GetDisplayNames(), EditorStyles.toolbarPopupLeft, GUILayout.Width(80));
                    if (display != targetDisplay)
                    {
                        targetDisplay = display;
                        Display.activeEditorGameViewTarget  = targetDisplay;
                        UpdateZoomAreaAndParent();
                    }
                }
                EditorGUILayout.GameViewSizePopup(currentSizeGroupType, selectedSizeIndex, this, EditorStyles.toolbarPopup, GUILayout.Width(160f));

                DoZoomSlider();
                // If the previous platform and current does not match, update the scale
                if ((int)currentSizeGroupType != prevSizeGroupType)
                {
                    UpdateZoomAreaAndParent();
                    // Update the platform to the recent one
                    prevSizeGroupType = (int)currentSizeGroupType;
                }

                if (FrameDebugger.IsLocalEnabled())
                {
                    GUILayout.FlexibleSpace();
                    Color oldCol = GUI.color;
                    // This has nothing to do with animation recording.  Can we replace this color with something else?
                    GUI.color *= AnimationMode.recordedPropertyColor;
                    GUILayout.Label(Styles.frameDebuggerOnContent, EditorStyles.toolbarLabel);
                    GUI.color = oldCol;
                    // Make frame debugger windows repaint after each time game view repaints.
                    // We want them to always display the latest & greatest game view
                    // rendering state.
                    if (Event.current.type == EventType.Repaint)
                        FrameDebuggerWindow.RepaintAll();
                }

                GUILayout.FlexibleSpace();

                if (ShouldShowMetalFrameCaptureGUI())
                {
                    if (GUILayout.Button(Styles.metalFrameCaptureContent, EditorStyles.toolbarButton))
                        m_Parent.CaptureMetalScene();
                }

                if (RenderDoc.IsLoaded())
                {
                    using (new EditorGUI.DisabledScope(!RenderDoc.IsSupported()))
                    {
                        if (GUILayout.Button(Styles.renderdocContent, EditorStyles.toolbarButton))
                        {
                            m_Parent.CaptureRenderDocScene();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                SubsystemManager.GetSubsystems(m_DisplaySubsystems);
                // Allow the user to select how the XR device will be rendered during "Play In Editor"
                if (VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)) || (m_DisplaySubsystems.Count != 0 && !m_DisplaySubsystems[0].disableLegacyRenderer))
                {
                    EditorGUI.BeginChangeCheck();
                    GameViewRenderMode currentGameViewRenderMode = UnityEngine.XR.XRSettings.gameViewRenderMode;
                    int selectedRenderMode = EditorGUILayout.Popup(Mathf.Clamp(((int)currentGameViewRenderMode) - 1, 0, Styles.xrRenderingModes.Length - 1), Styles.xrRenderingModes, EditorStyles.toolbarPopup, GUILayout.Width(80));
                    if (EditorGUI.EndChangeCheck() && currentGameViewRenderMode != GameViewRenderMode.None)
                    {
                        SetXRRenderMode(selectedRenderMode);
                    }
                }
                // Handles the case where XRSDK is being used without the shim layer
                else if (m_DisplaySubsystems.Count != 0 && m_DisplaySubsystems[0].disableLegacyRenderer)
                {
                    EditorGUI.BeginChangeCheck();
                    int currentMirrorViewBlitMode = m_DisplaySubsystems[0].GetPreferredMirrorBlitMode();
                    int currentRenderMode = XRTranslateMirrorViewBlitModeToRenderMode(currentMirrorViewBlitMode);
                    int selectedRenderMode = EditorGUILayout.Popup(Mathf.Clamp(currentRenderMode , 0, Styles.xrRenderingModes.Length - 1), Styles.xrRenderingModes, EditorStyles.toolbarPopup, GUILayout.Width(80));
                    int selectedMirrorViewBlitMode = XRTranslateRenderModeToMirrorViewBlitMode(selectedRenderMode);
                    if (EditorGUI.EndChangeCheck() || currentMirrorViewBlitMode == 0)
                    {
                        m_DisplaySubsystems[0].SetPreferredMirrorBlitMode(selectedMirrorViewBlitMode);
                        if (selectedMirrorViewBlitMode != m_XRRenderMode)
                            ClearTargetTexture();

                        m_XRRenderMode = selectedMirrorViewBlitMode;
                    }
                }
                GUILayout.FlexibleSpace();

                // Don't display the fullscreen selection dropdown on gameview toolbars that are already fullscreen.
                if (!isFullscreen)
                {
                    EditorGUI.BeginDisabled(Application.isPlaying);
                    EditorGUILayout.GameViewOnPlayPopup(playModeBehaviorIdx, this, EditorStyles.toolbarDropDown);
                    EditorGUI.EndDisabled();
                }

                EditorUtility.audioMasterMute = GUILayout.Toggle(EditorUtility.audioMasterMute, Styles.muteContent, EditorStyles.toolbarButton);
                m_Stats = GUILayout.Toggle(m_Stats, Styles.statsContent, EditorStyles.toolbarButton);

                if (EditorGUILayout.DropDownToggle(ref m_Gizmos, Styles.gizmosContent, EditorStyles.toolbarDropDownToggleRight))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    if (AnnotationWindow.ShowAtPosition(rect, true))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
            }
            GUILayout.EndHorizontal();
            MirrorFullscreenToolbarSettings();
        }

        private int XRTranslateMirrorViewBlitModeToRenderMode(int mirrorViewBlitMode)
        {
            switch (mirrorViewBlitMode)
            {
                default:
                    return 0;
                case XRMirrorViewBlitMode.RightEye:
                    return 1;
                case XRMirrorViewBlitMode.SideBySide:
                    return 2;
                case XRMirrorViewBlitMode.SideBySideOcclusionMesh:
                    return 3;
            }
        }

        private int XRTranslateRenderModeToMirrorViewBlitMode(int renderMode)
        {
            switch (renderMode)
            {
                default: // or 0
                    return XRMirrorViewBlitMode.LeftEye;
                case 1:
                    return XRMirrorViewBlitMode.RightEye;
                case 2:
                    return XRMirrorViewBlitMode.SideBySide;
                case 3:
                    return XRMirrorViewBlitMode.SideBySideOcclusionMesh;
            }
        }

        private void SetXRRenderMode(int mode)
        {
            switch (mode)
            {
                default: // or 0
                    UnityEngine.XR.XRSettings.gameViewRenderMode = UnityEngine.XR.GameViewRenderMode.LeftEye;
                    break;
                case 1:
                    UnityEngine.XR.XRSettings.gameViewRenderMode = UnityEngine.XR.GameViewRenderMode.RightEye;
                    break;
                case 2:
                    UnityEngine.XR.XRSettings.gameViewRenderMode = UnityEngine.XR.GameViewRenderMode.BothEyes;
                    break;
                case 3:
                    UnityEngine.XR.XRSettings.gameViewRenderMode = UnityEngine.XR.GameViewRenderMode.OcclusionMesh;
                    break;
            }

            if (mode != m_XRRenderMode)
                ClearTargetTexture();

            m_XRRenderMode = mode;
        }

        private void ClearTargetTexture()
        {
            if (m_RenderTexture && m_RenderTexture.IsCreated())
            {
                var previousTarget = RenderTexture.active;
                RenderTexture.active = m_RenderTexture;
                GL.Clear(true, true, kClearBlack);
                RenderTexture.active = previousTarget;
            }
        }

        private float ScaleThatFitsTargetInView(Vector2 targetInPixels, Vector2 viewInPoints)
        {
            var targetInPoints = EditorGUIUtility.PixelsToPoints(targetInPixels);
            var viewToTargetRatio = new Vector2(viewInPoints.x / targetInPoints.x, viewInPoints.y / targetInPoints.y);
            return Mathf.Min(viewToTargetRatio.x, viewToTargetRatio.y);
        }

        private float DefaultScaleForTargetInView(Vector2 targetToFit, Vector2 viewSize)
        {
            var scale = ScaleThatFitsTargetInView(targetToFit, viewSize);
            if (scale > 1f)
            {
                scale = Mathf.Min(maxScale * EditorGUIUtility.pixelsPerPoint, Mathf.FloorToInt(scale));
            }
            return scale;
        }

        private void ConfigureZoomArea()
        {
            m_ZoomArea.rect = viewInWindow;
            // Sliders are sized with respect to canvas
            var targetInContentCached = targetInContent;
            m_ZoomArea.hBaseRangeMin = targetInContentCached.xMin;
            m_ZoomArea.vBaseRangeMin = targetInContentCached.yMin;
            m_ZoomArea.hBaseRangeMax = targetInContentCached.xMax;
            m_ZoomArea.vBaseRangeMax = targetInContentCached.yMax;
            // Restrict zooming
            m_ZoomArea.hScaleMin = m_ZoomArea.vScaleMin = minScale;
            m_ZoomArea.hScaleMax = m_ZoomArea.vScaleMax = maxScale;
        }

        private void EnforceZoomAreaConstraints()
        {
            var shownArea = m_ZoomArea.shownArea;
            var targetInContentCached = targetInContent;

            // When zoomed out, we disallow panning by automatically centering the view
            if (shownArea.width > targetInContentCached.width)
            {
                shownArea.x = -0.5f * shownArea.width;
            }
            else
            // When zoomed in, we prevent panning outside the render area
            {
                shownArea.x = Mathf.Clamp(shownArea.x, targetInContentCached.xMin, targetInContentCached.xMax - shownArea.width);
            }
            // Horizontal and vertical are separated because otherwise we get weird behaviour when only one is zoomed out
            if (shownArea.height > targetInContent.height)
            {
                shownArea.y = -0.5f * shownArea.height;
            }
            else
            {
                shownArea.y = Mathf.Clamp(shownArea.y, targetInContentCached.yMin, targetInContentCached.yMax - shownArea.height);
            }

            m_ZoomArea.shownArea = shownArea;
        }

        public void RenderToHMDOnly()
        {
            var mousePos = Vector2.zero;
            targetDisplay = 0;
            targetSize = targetRenderSize;
            showGizmos = false;
            renderIMGUI = false;

            m_RenderTexture = RenderView(mousePos, clearTexture: false);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            latestState = state;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // Enable vsync in play mode to get as much as possible frame rate consistency
                m_Parent.EnableVSync(m_VSyncEnabled);
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                m_Parent.EnableVSync(false);
            }
        }

        void OnBecameVisible()
        {
            EditorApplication.update += RepaintIfNeeded;
        }

        void OnBecameInvisible()
        {
            EditorApplication.update -= RepaintIfNeeded;
        }

        void RepaintIfNeeded()
        {
            if (LODUtility.IsLODAnimatingOnDisplay(targetDisplay))
                Repaint();
        }

        private void OnEditorModeChanged(ModeService.ModeChangedArgs args)
        {
            showToolbar = ModeService.HasCapability(ModeCapability.GameViewToolbar, true);

            Repaint();
        }

        private bool HandleCommand(Event evt)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            if (evt.type != EventType.ValidateCommand && evt.type != EventType.ExecuteCommand)
                return false;

            // Disable certain commands when in play mode with GameView focused. Executing commands has a noticeable
            // impact on performance, especially when the default global shortcut is a single alphanumeric key.
            switch (evt.commandName)
            {
                case EventCommandNames.FrameSelected:
                case EventCommandNames.FrameSelectedWithLock:
                    evt.Use();
                    return true;

                default:
                    return false;
            }
        }

        private void OnGUI()
        {
            Event evt = Event.current;
            EventType type = evt.type;

            if (HandleCommand(evt))
                return;

            if (suppressRenderingForFullscreen && latestState == PlayModeStateChange.EnteredPlayMode)
            {
                DrawDuringFullscreenBackground();
                return;
            }

            if (position.size * EditorGUIUtility.pixelsPerPoint != m_LastWindowPixelSize) // pixelsPerPoint only reliable in OnGUI()
            {
                UpdateZoomAreaAndParent();
            }

            if (showToolbar)
                DoToolbarGUI();

            // This isn't ideal. Custom Cursors set by editor extensions for other windows can leak into the game view.
            // To fix this we should probably stop using the global custom cursor (intended for runtime) for custom editor cursors.
            // This has been noted for Cursors tech debt.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                EditorGUIUtility.AddCursorRect(viewInWindow, MouseCursor.CustomCursor);

            // Gain mouse lock when clicking on game view content, unless game is paused
            if (!EditorApplication.isPaused && type == EventType.MouseDown && viewInWindow.Contains(Event.current.mousePosition))
            {
                AllowCursorLockAndHide(true);
            }
            // Lose mouse lock when pressing escape
            else if (type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                AllowCursorLockAndHide(false);
            }

            // We hide sliders when playing, and also when we are zoomed out beyond canvas edges
            var playing = EditorApplication.isPlaying && !EditorApplication.isPaused;
            var targetInContentCached = targetInContent;
            m_ZoomArea.hSlider = !playing && m_ZoomArea.shownArea.width < targetInContentCached.width;
            m_ZoomArea.vSlider = !playing && m_ZoomArea.shownArea.height < targetInContentCached.height;
            m_ZoomArea.enableMouseInput = !playing;
            ConfigureZoomArea();

            // We don't want controls inside the GameView (e.g. the toolbar) to have keyboard focus while playing.
            // The game should get the keyboard events.
            if (playing)
                EditorGUIUtility.keyboardControl = 0;

            GUI.color = Color.white; // Get rid of play mode tint

            var originalEventType = Event.current.type;

            m_ZoomArea.BeginViewGUI();

            // Window size might change on Layout event
            if (type == EventType.Layout)
                targetSize = targetRenderSize;

            // Setup game view dimensions, so that player loop can use it for input
            var gameViewTarget = GUIClip.UnclipToWindow(m_ZoomArea.drawRect);
            if (m_Parent)
            {
                var zoomedTarget = new Rect(targetInView.position + gameViewTarget.position, targetInView.size);
                if (!isFullscreen)
                {
                    SetParentGameViewDimensions(zoomedTarget, gameViewTarget, targetRenderSize);
                }
                else
                {
                    SetParentGameViewDimensions(zoomedTarget, gameViewTarget, EditorFullscreenController.fullscreenDisplayRenderSize);
                }
            }

            var editorMousePosition = Event.current.mousePosition;
            var gameMousePosition = (editorMousePosition + gameMouseOffset) * gameMouseScale;

            if (type == EventType.Repaint)
            {
                GUI.Box(m_ZoomArea.drawRect, GUIContent.none, Styles.gameViewBackgroundStyle);

                Vector2 oldOffset = GUIUtility.s_EditorScreenPointOffset;
                GUIUtility.s_EditorScreenPointOffset = Vector2.zero;
                SavedGUIState oldState = SavedGUIState.Create();


                var clearTexture = m_ClearInEditMode && !EditorApplication.isPlaying;

                var currentTargetDisplay = 0;
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    // Display Targets can have valid targets from 0 to 7.
                    System.Diagnostics.Debug.Assert(targetDisplay < 8, "Display Target is Out of Range");
                    currentTargetDisplay = targetDisplay;
                }

                targetDisplay = currentTargetDisplay;
                targetSize = targetRenderSize;
                showGizmos = m_Gizmos;
                clearColor = kClearBlack;
                renderIMGUI = true;

                if (!EditorApplication.isPlaying ||
                    (EditorApplication.isPlaying && (Time.frameCount % OnDemandRendering.renderFrameInterval == 0 && !Unsupported.IsEditorPlayerLoopWaiting())))
                    m_RenderTexture = RenderView(gameMousePosition, clearTexture);

                if (m_TargetClamped)
                    Debug.LogWarningFormat("GameView reduced to a reasonable size for this system ({0}x{1})", targetSize.x, targetSize.y);
                EditorGUIUtility.SetupWindowSpaceAndVSyncInternal(GUIClip.Unclip(viewInWindow));

                if (m_RenderTexture != null && m_RenderTexture.IsCreated())
                {
                    oldState.ApplyAndForget();
                    GUIUtility.s_EditorScreenPointOffset = oldOffset;

                    GUI.BeginGroup(m_ZoomArea.drawRect);
                    // Actually draw the game view to the screen, without alpha blending
                    Rect drawRect = deviceFlippedTargetInView;
                    drawRect.x = Mathf.Round(drawRect.x);
                    drawRect.y = Mathf.Round(drawRect.y);
                    Graphics.DrawTexture(drawRect, m_RenderTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, GUI.blitMaterial);
                    GUI.EndGroup();
                }
            }
            else if (type != EventType.Layout && type != EventType.Used)
            {
                if (Event.current.isKey && (!EditorApplication.isPlaying || EditorApplication.isPaused))
                    return;

                if (isFullscreen)
                {
                    // Let the global eventhandler handle toggling fullscreen after this event (do not use the current event)
                    // We support fullscreen in both edit mode and play mode
                    return;
                }

                bool mousePosInGameViewRect = viewInWindow.Contains(Event.current.mousePosition);

                // MouseDown events outside game view rect are not send to scripts but MouseUp events are (see below)
                if (Event.current.rawType == EventType.MouseDown && !mousePosInGameViewRect)
                    return;

                var originalDisplayIndex = Event.current.displayIndex;

                // Transform events into local space, so the mouse position is correct
                // Then queue it up for playback during playerloop
                Event.current.mousePosition = gameMousePosition;
                Event.current.displayIndex = targetDisplay;

                EditorGUIUtility.QueueGameViewInputEvent(Event.current);

                // Do not use mouse or touch events if mousepos is outside game view rect (fix for case 380995: Gameview tab's context menu is not appearing on right click)
                // Placed after event queueing above to ensure scripts can react on mouse up events.
                bool isKey = Event.current.rawType == EventType.KeyDown || Event.current.rawType == EventType.KeyUp;
                bool useEvent = mousePosInGameViewRect || isKey;

                // Don't use command events, or they won't be sent to other views.
                if (type == EventType.ExecuteCommand || type == EventType.ValidateCommand)
                    useEvent = false;

                if (useEvent)
                    Event.current.Use();
                else
                    Event.current.mousePosition = editorMousePosition;

                // Reset display index
                Event.current.displayIndex = originalDisplayIndex;
            }

            m_ZoomArea.EndViewGUI();

            if (originalEventType == EventType.ScrollWheel && Event.current.type == EventType.Used)
            {
                EditorApplication.update -= SnapZoomDelayed;
                EditorApplication.update += SnapZoomDelayed;
                s_LastScrollTime = EditorApplication.timeSinceStartup;
            }

            EnforceZoomAreaConstraints();

            if (m_RenderTexture)
            {
                if (m_ZoomArea.scale.y < 1f)
                {
                    m_RenderTexture.filterMode = FilterMode.Bilinear;
                }
                else
                {
                    m_RenderTexture.filterMode = FilterMode.Point;
                }
            }

            if (m_NoCameraWarning && !EditorGUIUtility.IsDisplayReferencedByCameras(targetDisplay))
            {
                GUI.Label(warningPosition, GUIContent.none, EditorStyles.notificationBackground);
                var displayName = ModuleManager.ShouldShowMultiDisplayOption() ? DisplayUtility.GetDisplayNames()[targetDisplay].text : string.Empty;
                var cameraWarning = string.Format("{0}\nNo cameras rendering", displayName);
                EditorGUI.DoDropShadowLabel(warningPosition, EditorGUIUtility.TempContent(cameraWarning), EditorStyles.notificationText, .3f);
            }

            if (m_Stats)
                GameViewGUI.GameViewStatsGUI();
        }

        private void DrawDuringFullscreenBackground()
        {
            DoToolbarGUI();
            EditorGUILayout.LabelField(Styles.suppressMessage, Styles.largeCenteredText);
            var binding = ShortcutManager.instance.GetShortcutBinding(EditorFullscreenController.kFullscreenToggle);
            string content = string.Format(Styles.disableFullscreenMainDisplayFormatContent.text, binding);
            EditorGUILayout.LabelField(content, Styles.smallCenteredText);
        }

        void IGameViewOnPlayMenuUser.OnPlayPopupSelection(int indexClicked, object objectSelected)
        {
            playModeBehaviorIdx = indexClicked;
            if (playModeBehaviorIdx == 0)
            {
                if (playFocused)
                    enterPlayModeBehavior = EnterPlayModeBehavior.PlayFocused;
                else
                    enterPlayModeBehavior = EnterPlayModeBehavior.PlayUnfocused;
                fullscreenMonitorIdx = PlayModeView.kFullscreenNone;
            }
            else if (playModeBehaviorIdx == 1)
            {
                enterPlayModeBehavior = EnterPlayModeBehavior.PlayMaximized;
                fullscreenMonitorIdx = PlayModeView.kFullscreenNone;
            }
            else
            {
                enterPlayModeBehavior = EnterPlayModeBehavior.PlayFullscreen;
                fullscreenMonitorIdx = GameViewOnPlayMenu.SelectedIndexToDisplayIndex(indexClicked);
                if (fullscreenMonitorIdx == PlayModeView.kFullscreenInvalidIdx)
                {
                    Debug.LogError("Invalid display index(" + fullscreenMonitorIdx + ") for selected index(" + indexClicked + ")");
                    fullscreenMonitorIdx = PlayModeView.kFullscreenNone;
                }
            }
        }
    }
}
