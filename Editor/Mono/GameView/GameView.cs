// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using UnityEditor.Modules;
using UnityEngine.Scripting;

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
    internal class GameView : EditorWindow, IHasCustomMenu, IGameViewSizeMenuUser
    {
        const int kBorderSize = 5;
        const int kScaleSliderMinWidth = 30;
        const int kScaleSliderMaxWidth = 150;
        const int kScaleSliderSnapThreshold = 4;
        const int kScaleLabelWidth = 30;
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
                var clampedMinScale = Mathf.Min(kMinScale, ScaleThatFitsTargetInView(targetSize, viewInWindow.size));
                if (m_LowResolutionForAspectRatios[(int)currentSizeGroupType] && currentGameViewSize.sizeType == GameViewSizeType.AspectRatio)
                    clampedMinScale = Mathf.Max(clampedMinScale, EditorGUIUtility.pixelsPerPoint);
                return clampedMinScale;
            }
        }
        float maxScale
        {
            get { return Mathf.Max(kMaxScale * EditorGUIUtility.pixelsPerPoint, ScaleThatFitsTargetInView(targetSize, viewInWindow.size)); }
        }

        [SerializeField] bool m_MaximizeOnPlay;
        [SerializeField] bool m_Gizmos;
        [SerializeField] bool m_Stats;
        [SerializeField] int[] m_SelectedSizes = new int[0]; // We have a selection for each game view size group (e.g standalone, android etc)
        [SerializeField] int m_TargetDisplay;

        [SerializeField] ZoomableArea m_ZoomArea;
        [SerializeField] float m_defaultScale = -1f;

        [SerializeField] RenderTexture m_TargetTexture;
        bool m_TargetClamped;
        [SerializeField] ColorSpace m_CurrentColorSpace = ColorSpace.Uninitialized;

        [SerializeField] Vector2 m_LastWindowPixelSize;

        [SerializeField] bool m_ClearInEditMode = true;
        [SerializeField] bool m_NoCameraWarning = true;
        [SerializeField] bool[] m_LowResolutionForAspectRatios = new bool[0];

        int m_SizeChangeID = int.MinValue;


        internal static class Styles
        {
            public static GUIContent gizmosContent = EditorGUIUtility.TextContent("Gizmos");
            public static GUIContent zoomSliderContent = EditorGUIUtility.TextContent("Scale|Size of the game view on the screen.");
            public static GUIContent maximizeOnPlayContent = EditorGUIUtility.TextContent("Maximize On Play");
            public static GUIContent muteContent = EditorGUIUtility.TextContent("Mute Audio");
            public static GUIContent statsContent = EditorGUIUtility.TextContent("Stats");
            public static GUIContent frameDebuggerOnContent = EditorGUIUtility.TextContent("Frame Debugger On");
            public static GUIContent loadRenderDocContent = EditorGUIUtility.TextContent("Load RenderDoc");
            public static GUIContent noCameraWarningContextMenuContent = EditorGUIUtility.TextContent("Warn if No Cameras Rendering");
            public static GUIContent clearEveryFrameContextMenuContent = EditorGUIUtility.TextContent("Clear Every Frame in Edit Mode");
            public static GUIContent lowResAspectRatiosContextMenuContent = EditorGUIUtility.TextContent("Low Resolution Aspect Ratios");
            public static GUIContent renderdocContent;
            public static GUIStyle gizmoButtonStyle;
            public static GUIStyle gameViewBackgroundStyle;

            static Styles()
            {
                gameViewBackgroundStyle = (GUIStyle)"GameViewBackground";
                gizmoButtonStyle = (GUIStyle)"GV Gizmo DropDown";
                renderdocContent = EditorGUIUtility.IconContent("renderdoc", "Capture|Capture the current view and open in RenderDoc.");
            }
        };

        static List<GameView> s_GameViews = new List<GameView>();
        static GameView s_LastFocusedGameView = null;
        static double s_LastScrollTime;

        public GameView()
        {
            autoRepaintOnSceneChange = true;
            m_TargetDisplay = 0;
            InitializeZoomArea();
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

        public bool forceLowResolutionAspectRatios { get { return EditorGUIUtility.pixelsPerPoint == 1f; } }
        public bool showLowResolutionToggle { get { return EditorApplication.supportsHiDPI; } }

        public bool maximizeOnPlay
        {
            get { return m_MaximizeOnPlay; }
            set { m_MaximizeOnPlay = value; }
        }

        int selectedSizeIndex
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

        static GameViewSizeGroupType currentSizeGroupType
        {
            get { return GameViewSizes.instance.currentGroupType; }
        }

        GameViewSize currentGameViewSize
        {
            get { return GameViewSizes.instance.currentGroup.GetGameViewSize(selectedSizeIndex); }
        }

        // The area of the window that the rendered game view is limited to
        Rect viewInWindow { get { return new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - EditorGUI.kWindowToolbarHeight); } }

        internal Vector2 targetSize // Size of render target in pixels
        {
            get
            {
                var viewPixelRect = lowResolutionForAspectRatios ? viewInWindow : EditorGUIUtility.PointsToPixels(viewInWindow);
                return GameViewSizes.GetRenderTargetSize(viewPixelRect, currentSizeGroupType, selectedSizeIndex, out m_TargetClamped);
            }
        }

        // Area of the render target in zoom content space (it is centered in content space)
        Rect targetInContent
        {
            get
            {
                var targetSizeCached = targetSize;
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

        Rect clippedTargetInParent // targetInParent, but clipped to viewInParent to discard outside mouse events
        {
            get
            {
                var targetInParentCached = targetInParent;
                var viewInParentCached = viewInParent;
                var clippedTargetInParent = Rect.MinMaxRect(
                        Mathf.Max(targetInParentCached.xMin, viewInParentCached.xMin),
                        Mathf.Max(targetInParentCached.yMin, viewInParentCached.yMin),
                        Mathf.Min(targetInParentCached.xMax, viewInParentCached.xMax),
                        Mathf.Min(targetInParentCached.yMax, viewInParentCached.yMax)
                        );
                return clippedTargetInParent;
            }
        }

        // Area for warnings such as no cameras rendering
        Rect warningPosition { get { return new Rect((viewInWindow.size - kWarningSize) * 0.5f, kWarningSize); } }

        Vector2 gameMouseOffset { get { return -viewInWindow.position - targetInView.position; } }

        float gameMouseScale { get { return EditorGUIUtility.pixelsPerPoint / m_ZoomArea.scale.y; } }

        Vector2 WindowToGameMousePosition(Vector2 windowMousePosition)
        {
            return (windowMousePosition + gameMouseOffset) * gameMouseScale;
        }

        void InitializeZoomArea()
        {
            m_ZoomArea = new ZoomableArea(true, false);
            m_ZoomArea.uniformScale = true;
            m_ZoomArea.upDirection = ZoomableArea.YDirection.Negative;
        }

        public void OnEnable()
        {
            prevSizeGroupType = (int)currentSizeGroupType;
            titleContent = GetLocalizedTitleContent();
            UpdateZoomAreaAndParent();
            dontClearBackground = true;
            s_GameViews.Add(this);
        }

        public void OnDisable()
        {
            s_GameViews.Remove(this);
            if (m_TargetTexture)
            {
                DestroyImmediate(m_TargetTexture);
            }
        }

        internal static GameView GetMainGameView()
        {
            if (s_LastFocusedGameView == null && s_GameViews != null && s_GameViews.Count > 0)
                s_LastFocusedGameView = s_GameViews[0];

            return s_LastFocusedGameView;
        }

        public static void RepaintAll()
        {
            if (s_GameViews == null)
                return;

            foreach (GameView gv in s_GameViews)
                gv.Repaint();
        }

        // This is here because NGUI uses it via reflection (noted in https://confluence.hq.unity3d.com/display/DEV/Game+View+Bucket)
        internal static Vector2 GetSizeOfMainGameView()
        {
            return GetMainGameViewTargetSize();
        }

        internal static Vector2 GetMainGameViewTargetSize()
        {
            var gameView = GetMainGameView();
            // It's possible with a corrupted layout that a GameView doesn't have a parent view.
            if (gameView != null && gameView.m_Parent)
                return gameView.targetSize;
            else
                return new Vector2(640f, 480f);
        }

        [RequiredByNativeCode]
        private static void GetMainGameViewTargetSizeNoBox(out Vector2 result)
        {
            result = GetMainGameViewTargetSize();
        }

        private void UpdateZoomAreaAndParent()
        {
            // Configure ZoomableArea for new resolution so that old resolution doesn't restrict scale
            bool oldScaleWasDefault = Mathf.Approximately(m_ZoomArea.scale.y, m_defaultScale);
            ConfigureZoomArea();
            m_defaultScale = DefaultScaleForTargetInView(targetSize, viewInWindow.size);
            if (oldScaleWasDefault)
            {
                m_ZoomArea.SetTransform(Vector2.zero, Vector2.one * m_defaultScale);
                EnforceZoomAreaConstraints();
            }

            CopyDimensionsToParentView();
            m_LastWindowPixelSize = position.size * EditorGUIUtility.pixelsPerPoint;
            EditorApplication.SetSceneRepaintDirty();
        }

        void AllowCursorLockAndHide(bool enable)
        {
            Unsupported.SetAllowCursorLock(enable);
            Unsupported.SetAllowCursorHide(enable);
        }

        private void OnFocus()
        {
            AllowCursorLockAndHide(true);
            s_LastFocusedGameView = this;
            InternalEditorUtility.OnGameViewFocus(true);
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

            InternalEditorUtility.OnGameViewFocus(false);
        }

        internal void CopyDimensionsToParentView()
        {
            if (m_Parent)
                SetParentGameViewDimensions(targetInParent, clippedTargetInParent, targetSize);
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

        public bool IsShowingGizmos()
        {
            return m_Gizmos;
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
                RenderDoc.Load();
                ShaderUtil.RecreateGfxDevice();
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
                dontClearBackground = true; // will cause re-clear
                UpdateZoomAreaAndParent();
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
            GUILayout.Label(Styles.zoomSliderContent, EditorStyles.miniLabel);
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
            var scaleContent = EditorGUIUtility.TempContent(string.Format("{0}x", (m_ZoomArea.scale.y).ToString("G2")));
            scaleContent.tooltip = Styles.zoomSliderContent.tooltip;
            GUILayout.Label(scaleContent, EditorStyles.miniLabel, GUILayout.Width(kScaleLabelWidth));
            scaleContent.tooltip = string.Empty;
        }

        private void DoToolbarGUI()
        {
            GameViewSizes.instance.RefreshStandaloneAndRemoteDefaultSizes();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    int display = EditorGUILayout.Popup(m_TargetDisplay, DisplayUtility.GetDisplayNames(), EditorStyles.toolbarPopup, GUILayout.Width(80));
                    EditorGUILayout.Space();
                    if (display != m_TargetDisplay)
                    {
                        m_TargetDisplay = display;
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

                if (FrameDebuggerUtility.IsLocalEnabled())
                {
                    GUILayout.FlexibleSpace();
                    Color oldCol = GUI.color;
                    // This has nothing to do with animation recording.  Can we replace this color with something else?
                    GUI.color *= AnimationMode.recordedPropertyColor;
                    GUILayout.Label(Styles.frameDebuggerOnContent, EditorStyles.miniLabel);
                    GUI.color = oldCol;
                    // Make frame debugger windows repaint after each time game view repaints.
                    // We want them to always display the latest & greatest game view
                    // rendering state.
                    if (Event.current.type == EventType.Repaint)
                        FrameDebuggerWindow.RepaintAll();
                }

                GUILayout.FlexibleSpace();

                if (RenderDoc.IsLoaded())
                {
                    using (new EditorGUI.DisabledScope(!RenderDoc.IsSupported()))
                    {
                        if (GUILayout.Button(Styles.renderdocContent, EditorStyles.toolbarButton))
                        {
                            m_Parent.CaptureRenderDoc();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                m_MaximizeOnPlay = GUILayout.Toggle(m_MaximizeOnPlay, Styles.maximizeOnPlayContent, EditorStyles.toolbarButton);
                EditorUtility.audioMasterMute = GUILayout.Toggle(EditorUtility.audioMasterMute, Styles.muteContent, EditorStyles.toolbarButton);
                m_Stats = GUILayout.Toggle(m_Stats, Styles.statsContent, EditorStyles.toolbarButton);

                Rect r = GUILayoutUtility.GetRect(Styles.gizmosContent, Styles.gizmoButtonStyle);
                Rect rightRect = new Rect(r.xMax - Styles.gizmoButtonStyle.border.right, r.y, Styles.gizmoButtonStyle.border.right, r.height);
                if (EditorGUI.DropdownButton(rightRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    if (AnnotationWindow.ShowAtPosition(rect, true))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
                m_Gizmos = GUI.Toggle(r, m_Gizmos, Styles.gizmosContent, Styles.gizmoButtonStyle);
            }
            GUILayout.EndHorizontal();
        }

        private void ClearTargetTexture()
        {
            if (m_TargetTexture.IsCreated())
            {
                var previousTarget = RenderTexture.active;
                RenderTexture.active = m_TargetTexture;
                GL.Clear(true, true, kClearBlack);
                RenderTexture.active = previousTarget;
            }
        }

        private void ConfigureTargetTexture(int width, int height)
        {
            var clearTexture = false;
            // Changing color space requires destroying the entire RT object and recreating it
            if (m_TargetTexture && m_CurrentColorSpace != QualitySettings.activeColorSpace)
            {
                DestroyImmediate(m_TargetTexture);
            }
            if (!m_TargetTexture)
            {
                m_CurrentColorSpace = QualitySettings.activeColorSpace;
                m_TargetTexture = new RenderTexture(0, 0, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                m_TargetTexture.name = "GameView RT";
                m_TargetTexture.filterMode = FilterMode.Point;
                m_TargetTexture.hideFlags = HideFlags.HideAndDontSave;
                EditorGUIUtility.SetGUITextureBlitColorspaceSettings(EditorGUIUtility.GUITextureBlitColorspaceMaterial);
            }

            // Changes to these attributes require a release of the texture
            if (m_TargetTexture.width != width || m_TargetTexture.height != height)
            {
                m_TargetTexture.Release();
                m_TargetTexture.width = width;
                m_TargetTexture.height = height;
                m_TargetTexture.antiAliasing = 1;
                clearTexture = true;
                if (m_TargetClamped)
                    Debug.LogWarningFormat("GameView reduced to a reasonable size for this system ({0}x{1})", width, height);
            }

            m_TargetTexture.Create();

            if (clearTexture)
            {
                ClearTargetTexture();
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
            ConfigureTargetTexture((int)targetSize.x, (int)targetSize.y);

            if (m_TargetTexture.IsCreated())
            {
                var gizmos = false;
                var targetDisplay = 0;
                var sendInput = false;

                EditorGUIUtility.RenderGameViewCamerasInternal(
                    m_TargetTexture,
                    targetDisplay,
                    GUIClip.Unclip(viewInWindow),
                    Vector2.zero,
                    gizmos,
                    sendInput);
            }
        }

        private void OnGUI()
        {
            if (position.size * EditorGUIUtility.pixelsPerPoint != m_LastWindowPixelSize) // pixelsPerPoint only reliable in OnGUI()
            {
                UpdateZoomAreaAndParent();
            }

            DoToolbarGUI();

            // Setup game view dimensions, so that player loop can use it for input
            CopyDimensionsToParentView();

            // This isn't ideal. Custom Cursors set by editor extensions for other windows can leak into the game view.
            // To fix this we should probably stop using the global custom cursor (intended for runtime) for custom editor cursors.
            // This has been noted for Cursors tech debt.
            EditorGUIUtility.AddCursorRect(viewInWindow, MouseCursor.CustomCursor);

            EventType type = Event.current.type;

            // Gain mouse lock when clicking on game view content
            if (type == EventType.MouseDown && viewInWindow.Contains(Event.current.mousePosition))
            {
                AllowCursorLockAndHide(true);
            }
            // Lose mouse lock when pressing escape
            else if (type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Unsupported.SetAllowCursorLock(false);

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

            var editorMousePosition = Event.current.mousePosition;
            var gameMousePosition = WindowToGameMousePosition(editorMousePosition);

            GUI.color = Color.white; // Get rid of play mode tint

            var originalEventType = Event.current.type;

            m_ZoomArea.BeginViewGUI();

            if (type == EventType.Repaint)
            {
                GUI.Box(m_ZoomArea.drawRect, GUIContent.none, Styles.gameViewBackgroundStyle);

                Vector2 oldOffset = GUIUtility.s_EditorScreenPointOffset;
                GUIUtility.s_EditorScreenPointOffset = Vector2.zero;
                SavedGUIState oldState = SavedGUIState.Create();

                ConfigureTargetTexture((int)targetSize.x, (int)targetSize.y);

                if (m_ClearInEditMode && !EditorApplication.isPlaying)
                    ClearTargetTexture();

                var currentTargetDisplay = 0;
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    // Display Targets can have valid targets from 0 to 7.
                    System.Diagnostics.Debug.Assert(m_TargetDisplay < 8, "Display Target is Out of Range");
                    currentTargetDisplay = m_TargetDisplay;
                }
                if (m_TargetTexture.IsCreated())
                {
                    var sendInput = true;
                    EditorGUIUtility.RenderGameViewCamerasInternal(m_TargetTexture, currentTargetDisplay, GUIClip.Unclip(viewInWindow), gameMousePosition, m_Gizmos, sendInput);
                    oldState.ApplyAndForget();
                    GUIUtility.s_EditorScreenPointOffset = oldOffset;

                    GUI.BeginGroup(m_ZoomArea.drawRect);
                    GL.sRGBWrite = m_CurrentColorSpace == ColorSpace.Linear;
                    // Actually draw the game view to the screen, without alpha blending
                    Graphics.DrawTexture(deviceFlippedTargetInView, m_TargetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlitColorspaceMaterial);
                    GL.sRGBWrite = false;
                    GUI.EndGroup();
                }
            }
            else if (type != EventType.Layout && type != EventType.Used)
            {
                if (WindowLayout.s_MaximizeKey.activated)
                {
                    if (!EditorApplication.isPlaying || EditorApplication.isPaused)
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
                Event.current.displayIndex = m_TargetDisplay;

                EditorGUIUtility.QueueGameViewInputEvent(Event.current);

                bool useEvent = true;

                // Do not use mouse UP event if mousepos is outside game view rect (fix for case 380995: Gameview tab's context menu is not appearing on right click)
                // Placed after event queueing above to ensure scripts can react on mouse up events.
                if (Event.current.rawType == EventType.MouseUp && !mousePosInGameViewRect)
                    useEvent = false;

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

            if (m_TargetTexture)
            {
                if (m_ZoomArea.scale.y < 1f)
                {
                    m_TargetTexture.filterMode = FilterMode.Bilinear;
                }
                else
                {
                    m_TargetTexture.filterMode = FilterMode.Point;
                }
            }

            if (m_NoCameraWarning && !EditorGUIUtility.IsDisplayReferencedByCameras(m_TargetDisplay))
            {
                GUI.Label(warningPosition, GUIContent.none, EditorStyles.notificationBackground);
                var displayName = ModuleManager.ShouldShowMultiDisplayOption() ? DisplayUtility.GetDisplayNames()[m_TargetDisplay].text : string.Empty;
                var cameraWarning = string.Format("{0}\nNo cameras rendering", displayName);
                EditorGUI.DoDropShadowLabel(warningPosition, EditorGUIUtility.TempContent(cameraWarning), EditorStyles.notificationText, .3f);
            }

            if (m_Stats)
                GameViewGUI.GameViewStatsGUI();
        }
    }
}
