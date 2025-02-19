// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.Rendering;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.CameraPreviewUtils;

namespace UnityEditor
{
    class ViewpointNameComparer : IComparer<IViewpoint>
    {
        public int Compare(IViewpoint x, IViewpoint y)
        {
            return x.TargetObject.name.CompareTo(y.TargetObject.name);
        }
    }

    sealed class CameraLookThrough : EditorToolbarDropdown
    {
        const string k_DropdownButtonUSSClass = "unity-cameras-overlay-selector";

        static readonly string k_NoCameraFound = L10n.Tr("No camera found");
        static readonly string k_Tooltip = L10n.Tr("Select a camera in the Scene.");

        [SerializeField]
        CamerasOverlay m_Overlay;

        public CameraLookThrough(CamerasOverlay overlay) : base()
        {
            tooltip = k_Tooltip;
            text = k_NoCameraFound;

            // The button will always reflect the Viewpoint's gameobject name.
            var textElement = this.Query<TextElement>().Children<TextElement>().First();
            textElement.bindingPath = "m_Name";

            m_Overlay = overlay;
            m_Overlay.onViewpointSelected += OnViewpointSelected;

            AddToClassList(k_DropdownButtonUSSClass);

            RegisterCallback<ClickEvent>(ShowSelectionDropdown);
            Initialize();
        }

        void Initialize()
        {
            SetCameraName();
        }

        void SetCameraName()
        {
            this.Unbind();

            if (m_Overlay.viewpoint != null)
            {
                icon = ViewpointProxyTypeCache.GetIcon(m_Overlay.viewpoint);
                this.Bind(new SerializedObject((m_Overlay.viewpoint.TargetObject as Component).gameObject));
            }
            else
            {
                icon = null;
                text = k_NoCameraFound;
            }
        }

        void ShowSelectionDropdown(ClickEvent evt)
        {
            var menu = new DropdownMenu();
            menu.allowDuplicateNames = true;
            foreach (var vp in m_Overlay.availableViewpoints)
            {
                var status = vp.TargetObject.Equals(m_Overlay.viewpoint.TargetObject) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                menu.AppendAction(vp.TargetObject.name, (_) => { m_Overlay.viewpoint = vp; }, status);
            }
            menu.DoDisplayEditorMenu(worldBound);
        }

        void OnViewpointSelected(IViewpoint vp) => SetCameraName();
    }

    sealed class CameraInspectProperties : EditorToolbarButton
    {
        [SerializeField]
        CamerasOverlay m_Overlay;

        [SerializeField]
        PropertyEditor m_ActiveEditor;

        public CameraInspectProperties(CamerasOverlay overlay) : base()
        {
            icon = EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow");
            tooltip = L10n.Tr("Open camera component properties.");

            m_Overlay = overlay;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnClicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnClicked;
        }

        void OnClicked()
        {
            if (m_Overlay.viewpoint == null)
                return;

            if (m_ActiveEditor != null)
            {
                m_ActiveEditor.Focus();
                return;
            }

            m_ActiveEditor = PropertyEditor.OpenPropertyEditor(m_Overlay.viewpoint.TargetObject, true);
        }
    }

    sealed class CameraOverscanSettingsWindow : OverlayPopupWindow
    {
        readonly string k_OverscanScaleTooltip = L10n.Tr("Configure size of overscan view guides.");
        readonly string k_OverscanOpacityTooltip = L10n.Tr("Configure overscan opacity.");

        protected override void OnEnable()
        {
            base.OnEnable();

            var sceneView = SceneView.lastActiveSceneView;
            var settings = sceneView.viewpoint.cameraOverscanSettings;

            var scale = new Slider(L10n.Tr("Overscan"), SceneViewViewpoint.ViewpointSettings.minScale, SceneViewViewpoint.ViewpointSettings.maxScale, SliderDirection.Horizontal, 1f);
            scale.tooltip = k_OverscanScaleTooltip;
            scale.SetValueWithoutNotify(settings.scale);
            scale.showInputField = true;
            scale.RegisterValueChangedCallback(evt =>
            {
                settings.scale = evt.newValue;
                sceneView.Repaint();
            });
            rootVisualElement.Add(scale);

            var opacity = new SliderInt(L10n.Tr("Overscan Opacity"), SceneViewViewpoint.ViewpointSettings.minOpacity, SceneViewViewpoint.ViewpointSettings.maxOpacity, SliderDirection.Horizontal, 1);
            opacity.tooltip = k_OverscanOpacityTooltip;
            opacity.SetValueWithoutNotify(settings.opacity);
            opacity.showInputField = true;
            opacity.RegisterValueChangedCallback(evt =>
            {
                settings.opacity = evt.newValue;
                sceneView.Repaint();
            });
            rootVisualElement.Add(opacity);
        }
    }

    sealed class CameraViewToggle : EditorToolbarDropdownToggle
    {
        const string k_ShortcutIdPrefx = "Scene View/Camera View/";
        const string k_IconPathNormal = "Overlays/Fullscreen";
        const string k_IconPathActive = "Overlays/FullscreenOn";
        readonly string k_TooltipNormal = L10n.Tr("Control the selected camera in first person.");
        readonly string k_TooltipActive = L10n.Tr("Return to Scene Camera.");

        [Shortcut(k_ShortcutIdPrefx + "Toggle Between Scene Camera and Last Controlled Camera", typeof(SceneView))]
        static void ToggleViewWithLastViewpoint(ShortcutArguments args)
        {
            SceneView sv = SceneView.focusedWindow as SceneView;

            if (sv.TryGetOverlay(CamerasOverlay.overlayId, out Overlay match))
            {
                var toggleInstance = match.contentRoot.Q<CameraViewToggle>();
                if (toggleInstance == null)
                    return;

                if (toggleInstance.canActivate)
                    toggleInstance.value = !toggleInstance.value;
            }
        }

        [SerializeField]
        CamerasOverlay m_Overlay;

        SceneView sceneView => m_Overlay.containerWindow as SceneView;

        bool canActivate => m_Overlay.viewpoint != null;

        public CameraViewToggle(CamerasOverlay overlay) : base()
        {
            m_Overlay = overlay;
            m_Overlay.onWillBeDestroyed += OnWillBeDestroyed;
            m_Overlay.displayedChanged += OnDisplayChanged;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            Initialize();
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            this.RegisterValueChangedCallback(ValueChanged);

            m_Overlay.onViewpointSelected += ViewpointChanged;
            dropdownClicked += OnDropdownClicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            this.UnregisterValueChangedCallback(ValueChanged);

            m_Overlay.onViewpointSelected -= ViewpointChanged;
            dropdownClicked -= OnDropdownClicked;
        }

        void OnDisplayChanged(bool display)
        {
            if (!display && value)
                DisableCameraViewTool();
        }

        void OnWillBeDestroyed()
        {
            if (value)
                DisableCameraViewTool();

            m_Overlay.onWillBeDestroyed -= OnWillBeDestroyed;
            m_Overlay.displayedChanged -= OnDisplayChanged;
        }

        void ValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if (m_Overlay.viewpoint == null)
                {
                    return;
                }
                EnableCameraViewTool();
            }
            else
            {
                DisableCameraViewTool();
            }
        }

        void OnDropdownClicked()
        {
            OverlayPopupWindow.Show<CameraOverscanSettingsWindow>(this, new Vector2(300, 88));
        }

        void EnableCameraViewTool()
        {
            sceneView.viewpoint.SetViewpoint(m_Overlay.viewpoint);
            UpdateStyling();
        }

        void DisableCameraViewTool()
        {
            sceneView.viewpoint.ClearViewpoint();
            UpdateStyling();
        }

        void SwitchCamera()
        {
            EnableCameraViewTool();
        }

        void ViewpointChanged(IViewpoint viewpoint)
        {
            UpdateEnableState();
            // If toggle is on, it means the SceneView is looking though a camera.
            // In that case, switch camera.
            if (viewpoint != null && value)
                SwitchCamera();
        }

        void UpdateEnableState()
        {
            SetEnabled(m_Overlay.viewpoint != null);
        }

        void UpdateToggleValue()
        {
            SetValueWithoutNotify(sceneView.viewpoint.hasActiveViewpoint);
        }

        void UpdateStyling()
        {
            icon = EditorGUIUtility.FindTexture(value? k_IconPathActive : k_IconPathNormal);
            tooltip = value? k_TooltipActive : k_TooltipNormal;
        }

        void Initialize()
        {
            UpdateEnableState();
            UpdateToggleValue();
            UpdateStyling();
        }
    }

    sealed class CameraPreview : IMGUIContainer
    {
        readonly string k_NoCameraDisplayLabel = L10n.Tr("No camera selected");

        CamerasOverlay m_Overlay;

        public CameraPreview(CamerasOverlay overlay)
        {
            m_Overlay = overlay;
            onGUIHandler += OnGUI;
        }

        void OnGUI()
        {
            if ((m_Overlay.containerWindow as SceneView).viewpoint.hasActiveViewpoint)
                return;

            if (m_Overlay.viewpoint == null || !m_Overlay.viewpoint.TargetObject || !(m_Overlay.viewpoint is ICameraLensData))
            {
                GUILayout.Label(k_NoCameraDisplayLabel, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            //In some context, some camera cannot render preview (e.g.: overlay camera from URP's camera stacking)
            if (m_Overlay.viewpoint.TargetObject is Camera camera && !RenderPipelineEditorUtility.SupportPreview(camera, out string reason))
            {
                GUILayout.Label(reason, EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var sceneView = m_Overlay.containerWindow as SceneView;

            var cameraRect = rect;
            cameraRect.width = Mathf.Floor(cameraRect.width);

            if (cameraRect.width < 1 || cameraRect.height < 1 || float.IsNaN(cameraRect.width) || float.IsNaN(cameraRect.height))
                return;

            if (Event.current.type == EventType.Repaint)
            {
                Vector2 previewSize = PlayModeView.GetMainPlayModeViewTargetSize();

                if (previewSize.x < 0f)
                {
                    // Fallback to Scene View if not a valid game view size
                    previewSize.x = sceneView.position.width;
                    previewSize.y = sceneView.position.height;
                }

                float rectAspect = cameraRect.width / cameraRect.height;
                float previewAspect = previewSize.x / previewSize.y;
                Rect previewRect = cameraRect;
                if (rectAspect > previewAspect)
                {
                    float stretch = previewAspect / rectAspect;
                    previewRect = new Rect(cameraRect.xMin + cameraRect.width * (1.0f - stretch) * .5f, cameraRect.yMin, stretch * cameraRect.width, cameraRect.height);
                }
                else
                {
                    float stretch = rectAspect / previewAspect;
                    previewRect = new Rect(cameraRect.xMin, cameraRect.yMin + cameraRect.height * (1.0f - stretch) * .5f, cameraRect.width, stretch * cameraRect.height);
                }

                var settings = new PreviewSettings(previewSize);
                settings.overrideSceneCullingMask = sceneView.overrideSceneCullingMask;
                settings.scene = sceneView.customScene;
                settings.useHDR = (m_Overlay.containerWindow as SceneView).SceneViewIsRenderingHDR();

                var previewTexture = CameraPreviewUtils.GetPreview(m_Overlay.viewpoint, settings);

                Graphics.DrawTexture(previewRect, previewTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlit2SRGBMaterial);
            }
        }
    }

    sealed class ViewpointUserData : VisualElement
    {
        const string k_USSClass = "unity-user-data";

        CamerasOverlay m_Overlay;

        VisualElement m_CachedVisualElement;

        internal ViewpointUserData(CamerasOverlay overlay)
        {
            m_Overlay = overlay;

            AddToClassList(k_USSClass);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            if ((m_Overlay.containerWindow as SceneView).viewpoint.hasActiveViewpoint)
                ViewpointChanged((m_Overlay.containerWindow as SceneView).viewpoint.activeViewpoint);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            m_Overlay.onViewpointSelected += ViewpointChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Overlay.onViewpointSelected -= ViewpointChanged;
        }

        void ViewpointChanged(IViewpoint viewpoint)
        {
            if (m_CachedVisualElement != null && Contains(m_CachedVisualElement))
                Remove(m_CachedVisualElement);

            if (viewpoint == null)
                return;

            m_CachedVisualElement = viewpoint.CreateVisualElement();
            Add(m_CachedVisualElement);
        }
    }

    [Overlay(typeof(SceneView), overlayId, k_DisplayName, priority = (int)OverlayPriority.Cameras)]
    [Icon("Icons/Overlays/CameraPreview.png")]
    class CamerasOverlay : Overlay
    {
        const string k_USSFilePath = "StyleSheets/SceneView/CamerasOverlay/CamerasOverlay.uss";
        const string k_CamerasOverlayUSSClass = "unity-cameras-overlay";
        const string k_ReducedCamerasOverlayUSSClass = "unity-cameras-overlay--reduced";
        const string k_DisplayName = "Cameras";

        static readonly Vector2 k_DefaultOverlaySize = new (250, 180);
        static readonly Vector2 k_DefaultMaxSize = new (4000, 4000);

        internal const string overlayId = "SceneView/CamerasOverlay";

        struct OverlaySavedState
        {
            public bool sizeWasOverriden;
            public float previousHeight;

            public OverlaySavedState()
            {
                sizeWasOverriden = false;
                previousHeight = 0;
            }

            internal OverlaySavedState(bool sizeIsOverriden, float height)
            {
                sizeWasOverriden = sizeIsOverriden;
                previousHeight = height;
            }
        }

        OverlaySavedState m_SavedStateBeforeReduceMode = default;
        float m_CurrentBaseHeight;

        CameraPreview m_CameraPreview;
        ViewpointUserData m_UserData;

        List<IViewpoint> m_AvailableViewpoints = new List<IViewpoint>();
        IViewpoint m_SelectedViewpoint;

        VisualElement m_Root;
        internal VisualElement root => m_Root;

        internal IViewpoint viewpoint
        {
            get => m_SelectedViewpoint;
            set
            {
                if (value == m_SelectedViewpoint)
                    return;

                m_SelectedViewpoint = value;
                onViewpointSelected?.Invoke(m_SelectedViewpoint);
            }
        }

        internal IReadOnlyCollection<IViewpoint> availableViewpoints
        {
            get
            {
                UpdateViewpointInternalList();
                return m_AvailableViewpoints;
            }
        }

        internal Action<IViewpoint> onViewpointSelected;

        internal event Action onWillBeDestroyed;

        public CamerasOverlay()
        {
            minSize = defaultSize = k_DefaultOverlaySize;
            maxSize = k_DefaultMaxSize;
        }

        public override VisualElement CreatePanelContent()
        {
            var styleSheet = EditorGUIUtility.Load(k_USSFilePath) as StyleSheet;

            m_Root = new VisualElement();
            m_Root.name = "Cameras Preview Overlay";
            m_Root.styleSheets.Add(styleSheet);
            m_Root.AddToClassList(k_CamerasOverlayUSSClass);
            m_Root.Add(new CameraControlsToolbar(this));

            m_CameraPreview = new CameraPreview(this);
            m_Root.Add(m_CameraPreview);

            m_UserData = new ViewpointUserData(this);
            m_UserData.RegisterCallback<GeometryChangedEvent>(UpdateSizeBasedOnUserData);
            m_Root.Add(m_UserData);

            return m_Root;
        }

        public override void OnCreated()
        {
            EditorApplication.update += Update;

            (containerWindow as SceneView).viewpoint.cameraLookThroughStateChanged += CameraViewStateChanged;

            CameraViewStateChanged((containerWindow as SceneView).viewpoint.hasActiveViewpoint);
        }

        public override void OnWillBeDestroyed()
        {
            EditorApplication.update -= Update;

            if (m_UserData != null)
                m_UserData.UnregisterCallback<GeometryChangedEvent>(UpdateSizeBasedOnUserData);

            (containerWindow as SceneView).viewpoint.cameraLookThroughStateChanged -= CameraViewStateChanged;

            onWillBeDestroyed?.Invoke();
        }

        // When the camera view is enabled in the SceneView, we reduce the overlay to hide the preview
        // render. User can still manually resize the width of the overlay.
        // When the user exits camera view, the overlay expands back to its original height.
        void CameraViewStateChanged(bool viewpointIsActive)
        {
            if (viewpointIsActive)
            {
                if (m_SavedStateBeforeReduceMode.previousHeight > 0)
                    return;

                m_SavedStateBeforeReduceMode = new OverlaySavedState(sizeOverridden, size.y);

                float delta = m_CameraPreview != null? m_CameraPreview.resolvedStyle.height : 0;
                m_CurrentBaseHeight = resizeTarget.resolvedStyle.height - delta + 5f;

                // Lock the height. User can't resize the Overlay vertically in reduce mode.
                maxSize = new Vector2(maxSize.x, m_CurrentBaseHeight);
                minSize = new Vector2(minSize.x, m_CurrentBaseHeight);

                UpdateOverlayStyling(isInReducedView: true);
            }
            else
            {
                maxSize = k_DefaultMaxSize;
                minSize = defaultSize;
                size = defaultSize;

                ResetSize();

                // Restore the size used before this overlay got reduced.
                if (m_SavedStateBeforeReduceMode.sizeWasOverriden || size.x != k_DefaultMaxSize.x)
                    size = new Vector2(size.x, m_SavedStateBeforeReduceMode.previousHeight);

                m_SavedStateBeforeReduceMode = default;
                UpdateOverlayStyling(isInReducedView: false);
            }
        }

        void UpdateOverlayStyling(bool isInReducedView)
        {
            contentRoot.EnableInClassList(k_CamerasOverlayUSSClass, !isInReducedView);
            contentRoot.EnableInClassList(k_ReducedCamerasOverlayUSSClass, isInReducedView);
        }

        // Adjust the size of the Overlay based on the size of the ViewpointUserData.
        void UpdateSizeBasedOnUserData(GeometryChangedEvent evt)
        {
            if (!(containerWindow as SceneView).viewpoint.hasActiveViewpoint)
                return;

            var size = m_UserData.resolvedStyle.height;
            float fixedHeight = m_CurrentBaseHeight + size;

            // Lock the height. User can't resize the Overlay vertically in reduced mode.
            maxSize = new Vector2(maxSize.x, fixedHeight);
            minSize = new Vector2(minSize.x, fixedHeight);
        }

        void UpdateViewpointInternalList()
        {
            m_AvailableViewpoints.Clear();

            var caches = ViewpointProxyTypeCache.caches;

            foreach (var componentType in ViewpointProxyTypeCache.GetSupportedCameraComponents())
            {
                foreach (var cam in GameObject.FindObjectsByType(componentType.viewpointType, FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    Type proxyType = ViewpointProxyTypeCache.GetTranslatorTypeForType(cam.GetType());
                    var proxyTypeCtor = proxyType.GetConstructor(new[] { cam.GetType() });
                    IViewpoint instance = proxyTypeCtor.Invoke(new object[] { cam }) as IViewpoint;

                    m_AvailableViewpoints.Add(instance);
                }
            }

            m_AvailableViewpoints.Sort(new ViewpointNameComparer());
        }

        void Update()
        {
            if (viewpoint == null)
            {
                UpdateViewpointInternalList();

                if (m_SelectedViewpoint == null && m_AvailableViewpoints.Count > 0)
                    m_SelectedViewpoint = m_AvailableViewpoints[0];

                if (m_SelectedViewpoint != null)
                    onViewpointSelected?.Invoke(m_SelectedViewpoint);
            }
            else if (!viewpoint.TargetObject)
            {
                m_SelectedViewpoint = null;

                onViewpointSelected?.Invoke(m_SelectedViewpoint);
            }

            // Enable the toolbar only when there is at least one camera in the scene.
            contentRoot.SetEnabled(viewpoint != null);
        }

        // used in tests
        internal void SelectViewpoint(Component targetObject)
        {
            var viewpointToSelect = GetViewpoint(targetObject);

            if (viewpointToSelect != null)
                viewpoint = viewpointToSelect;
        }

        // used in tests
        internal IViewpoint GetViewpoint(Component targetObject)
        {
            return m_AvailableViewpoints.Find(vp => vp.TargetObject == targetObject);
        }
    }

    class CameraControlsToolbar : OverlayToolbar
    {
        public CameraControlsToolbar(CamerasOverlay overlay)
        {
            Add(new CameraLookThrough(overlay));
            Add(new CameraInspectProperties(overlay));
            Add(new CameraViewToggle(overlay));
        }
    }
}
