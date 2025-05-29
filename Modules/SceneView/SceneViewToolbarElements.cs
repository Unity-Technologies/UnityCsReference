// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;
using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("SceneView/Common Camera Mode", typeof(SceneView))]
    sealed class CommonCameraModeElement : VisualElement, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        static readonly string s_UssClassName_Wireframe = "cameramode-wireframe";
        static readonly string s_UssClassName_ShadedWireframe = "cameramode-shadedwireframe";
        static readonly string s_UssClassName_Unlit = "cameramode-unlit";
        static readonly string s_UssClassName_Shaded = "cameramode-shaded";

        readonly EditorToolbarToggle m_WireframeButton;
        readonly EditorToolbarToggle m_ShadedWireframeButton;
        readonly EditorToolbarToggle m_UnlitButton;
        readonly EditorToolbarToggle m_ShadedButton;
        readonly VisualElement m_UIElementsRoot;

        public CommonCameraModeElement()
        {
            name = "CommonCameraModes";
            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
            SceneViewToolbarStyles.AddStyleSheets(this);

            Add(m_UIElementsRoot = new VisualElement());
            m_UIElementsRoot.AddToClassList("toolbar-contents");

            m_UIElementsRoot.Add(m_WireframeButton = new EditorToolbarToggle
            {
                name = "Wireframe",
                tooltip = "Wireframe Draw Mode",
            });
            m_WireframeButton.AddToClassList(s_UssClassName_Wireframe);
            m_WireframeButton.RegisterValueChangedCallback((evt) =>
            {
                sceneView.SwitchToRenderMode(DrawCameraMode.Wireframe);
            });

            m_UIElementsRoot.Add(m_ShadedWireframeButton = new EditorToolbarToggle
            {
                name = "Shaded Wireframe",
                tooltip = "Shaded Wireframe Draw Mode",
            });
            m_ShadedWireframeButton.AddToClassList(s_UssClassName_ShadedWireframe);
            m_ShadedWireframeButton.RegisterValueChangedCallback((evt) =>
            {
                sceneView.SwitchToRenderMode(DrawCameraMode.TexturedWire);
            });

            m_UIElementsRoot.Add(m_UnlitButton = new EditorToolbarToggle
            {
                name = "Unlit",
                tooltip = "Unlit Draw Mode",
            });
            m_UnlitButton.AddToClassList(s_UssClassName_Unlit);
            m_UnlitButton.RegisterValueChangedCallback((evt) =>
            {
                sceneView.SwitchToUnlit();
            });

            m_UIElementsRoot.Add(m_ShadedButton = new EditorToolbarToggle
            {
                name = "Shaded",
                tooltip = "Shaded Draw Mode",
            });
            m_ShadedButton.AddToClassList(s_UssClassName_Shaded);
            m_ShadedButton.RegisterValueChangedCallback((evt) =>
            {
                sceneView.SwitchToRenderMode(DrawCameraMode.Textured);
            });

            EditorToolbarUtility.SetupChildrenAsButtonStrip(m_UIElementsRoot);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.onCameraModeChanged += OnCameraModeChanged;
            sceneView.debugDrawModesUseInteractiveLightBakingDataChanged += OnUseInteractiveLightBakingDataChanged;
            sceneView.sceneLightingChanged += OnSceneLightingChanged;

            ValidateShadingMode(sceneView.cameraMode.drawMode);
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.onCameraModeChanged -= OnCameraModeChanged;
            sceneView.debugDrawModesUseInteractiveLightBakingDataChanged -= OnUseInteractiveLightBakingDataChanged;
            sceneView.sceneLightingChanged -= OnSceneLightingChanged;
        }

        void OnCameraModeChanged(SceneView.CameraMode mode) => ValidateShadingMode(mode.drawMode);

        void OnSceneLightingChanged(bool lit)
        {
            m_UnlitButton.SetValueWithoutNotify(!lit);
            ValidateShadingMode(sceneView.cameraMode.drawMode);
        }

        void OnUseInteractiveLightBakingDataChanged(bool useInteractiveLightBakingData)
        {
            ValidateShadingMode(sceneView.cameraMode.drawMode);
        }

        // Given the current DrawCameraMode, make sure the state of this toolbar is correct.
        void ValidateShadingMode(DrawCameraMode mode)
        {
            m_WireframeButton.SetValueWithoutNotify(false);
            m_ShadedWireframeButton.SetValueWithoutNotify(false);
            m_UnlitButton.SetValueWithoutNotify(false);
            m_ShadedButton.SetValueWithoutNotify(false);

            switch (mode)
            {
                case DrawCameraMode.Wireframe:
                    m_WireframeButton.SetValueWithoutNotify(true);
                    break;
                case DrawCameraMode.TexturedWire:
                    m_ShadedWireframeButton.SetValueWithoutNotify(true);
                    break;
                case DrawCameraMode.Textured when !sceneView.sceneLighting:
                    m_UnlitButton.SetValueWithoutNotify(true);
                    break;
                case DrawCameraMode.Textured:
                    m_ShadedButton.SetValueWithoutNotify(true);
                    break;
                default:
                    break;
            }
        }
    }

    [EditorToolbarElement("SceneView/Camera Mode", typeof(SceneView))]
    sealed class CameraModeElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        static readonly string s_UssClassName_Debug = "cameramode-debug";

        public CameraModeElement()
        {
            name = "CameraModeDropDown";
            tooltip = L10n.Tr("Debug Draw Mode");

            dropdownClicked += () => PopupWindow.Show(worldBound, new SceneRenderModeWindow(sceneView));

            this.RegisterValueChangedCallback((evt) => sceneView.ToggleLastDebugDrawMode());

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            AddToClassList(s_UssClassName_Debug);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.onCameraModeChanged += OnCameraModeChanged;
            OnCameraModeChanged(sceneView.cameraMode);

            //Settings the icon display explicitly as this is set to DisplayStyle.Flex when icon = null
            //Here the icon is set using USS so on the C# side icon = null
            var iconElement = this.Q<Image>();
            iconElement.style.display = DisplayStyle.Flex;
        }

        void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.onCameraModeChanged -= OnCameraModeChanged;
        }

        void OnCameraModeChanged(SceneView.CameraMode mode)
        {
            // These modes are handled in CommonCameraModeElement
            if (mode.drawMode == DrawCameraMode.Textured || mode.drawMode == DrawCameraMode.Wireframe || mode.drawMode == DrawCameraMode.TexturedWire)
            {
                SetValueWithoutNotify(false);
            }
            else
            {
                SetValueWithoutNotify(true);
            }
        }
    }

    [EditorToolbarElement("SceneView/2D", typeof(SceneView))]
    sealed class In2DModeElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public In2DModeElement()
        {
            name = "SceneView2D";
            tooltip = L10n.Tr("When toggled on, the Scene is in 2D view. When toggled off, the Scene is in 3D view.");
            this.RegisterValueChangedCallback(evt => sceneView.in2DMode = evt.newValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.in2DMode;
            sceneView.modeChanged2D += OnModeChanged;
            sceneView.viewpoint.cameraLookThroughStateChanged += OnViewpointChanged;
            OnViewpointChanged(sceneView.viewpoint.hasActiveViewpoint);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.viewpoint.cameraLookThroughStateChanged -= OnViewpointChanged;
            sceneView.modeChanged2D -= OnModeChanged;
        }

        void OnViewpointChanged(bool active)
        {
            SetEnabled(!active);
        }

        void OnModeChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Audio", typeof(SceneView))]
    sealed class SceneAudioElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneAudioElement()
        {
            name = "SceneviewAudio";
            tooltip = "Toggle audio on or off.";

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => sceneView.audioPlay = evt.newValue);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.audioPlay;
            sceneView.sceneAudioChanged += SceneViewOnsceneAudioChanged;
            EditorApplication.update += CheckAvailability;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneAudioChanged -= SceneViewOnsceneAudioChanged;
            EditorApplication.update -= CheckAvailability;
        }

        void SceneViewOnsceneAudioChanged(bool audio)
        {
            value = audio;
        }

        void CheckAvailability()
        {
            SetEnabled(!EditorApplication.isPlaying);
        }
    }

    [EditorToolbarElement("SceneView/Fx", typeof(SceneView))]
    sealed class SceneFxElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneFxElement()
        {
            name = "SceneviewFx";
            tooltip = L10n.Tr("Toggle skybox, fog, and various other effects.");

            dropdownClicked += () => PopupWindow.Show(worldBound, new SceneFXWindow(sceneView));

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.sceneViewState.fxEnabled = evt.newValue;
            });

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.sceneViewState.fxEnableChanged += OnSceneFxChanged;
            OnSceneFxChanged(sceneView.sceneViewState.fxEnabled);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneViewState.fxEnableChanged -= OnSceneFxChanged;
        }

        void OnSceneFxChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Scene Visibility", typeof(SceneView))]
    sealed class SceneVisElement : EditorToolbarToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneVisElement()
        {
            name = "SceneViewVisibility";
            tooltip = "Number of hidden objects, click to toggle scene visibility";

            this.RegisterValueChangedCallback(evt => sceneView.sceneVisActive = evt.newValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.sceneVisActiveChanged += SceneViewOnsceneVisActiveChanged;
            value = sceneView.sceneVisActive;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneVisActiveChanged -= SceneViewOnsceneVisActiveChanged;
        }

        void SceneViewOnsceneVisActiveChanged(bool active)
        {
            value = active;
        }
    }

    [EditorToolbarElement("SceneView/Grids", typeof(SceneView))]
    sealed class SceneViewGridSettingsElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneViewGridSettingsElement()
        {
            name = "SceneviewGrids";
            tooltip = L10n.Tr("Toggle the visibility of the grid");

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.sceneViewGrids.showGrid = evt.newValue;
            });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnDropdownClicked()
        {
            if (!(containerWindow is SceneView view))
                return;

            var w = PopupWindowBase.Show<GridSettingsWindow>(this, new Vector2(300, 88));

            if(w != null)
                w.Init(view);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.sceneViewGrids.showGrid;
            sceneView.gridVisibilityChanged += SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged += OnSceneViewOngridRenderAxisChanged;
            OnSceneViewOngridRenderAxisChanged(sceneView.sceneViewGrids.gridAxis);
            dropdownClicked += OnDropdownClicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.gridVisibilityChanged -= SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged -= OnSceneViewOngridRenderAxisChanged;
            dropdownClicked -= OnDropdownClicked;
        }

        void OnSceneViewOngridRenderAxisChanged(SceneViewGrid.GridRenderAxis axis)
        {
            EnableInClassList("unity-sceneview-grid-axis--x", axis == SceneViewGrid.GridRenderAxis.X);
            EnableInClassList("unity-sceneview-grid-axis--y", axis == SceneViewGrid.GridRenderAxis.Y);
            EnableInClassList("unity-sceneview-grid-axis--z", axis == SceneViewGrid.GridRenderAxis.Z);
        }

        void SceneViewOngridVisibilityChanged(bool visibility)
        {
            value = visibility;
        }
    }

    [EditorToolbarElement("SceneView/Render Doc", typeof(SceneView))]
    sealed class RenderDocElement : EditorToolbarButton, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public RenderDocElement()
        {
            name = "FrameCapture";
            tooltip = L10n.Tr(RenderDocUtil.openInRenderDocTooltip);
            icon = EditorGUIUtility.FindTexture("FrameCapture");
            UpdateState();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void UpdateState()
        {
            style.display = RenderDoc.IsLoaded() && RenderDoc.IsSupported() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnAction;
            EditorApplication.update += OnUpdate;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnAction;
            EditorApplication.update -= OnUpdate;
        }

        void OnAction()
        {
            sceneView.m_Parent.CaptureRenderDocScene();
        }

        void OnUpdate()
        {
            UpdateState();
        }
    }

    [EditorToolbarElement("SceneView/Metal Capture", typeof(SceneView))]
    sealed class MetalCaptureElement : EditorToolbarButton, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public MetalCaptureElement()
        {
            name = "MetalCapture";
            tooltip = L10n.Tr("Capture the current view and open in Xcode frame debugger");
            icon = EditorGUIUtility.FindTexture("FrameCapture");
            UpdateState();

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void UpdateState()
        {
            style.display = FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools)
                || FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnAction;
            EditorApplication.update += OnUpdate;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnAction;
            EditorApplication.update -= OnUpdate;
        }

        void OnAction()
        {
            sceneView.m_Parent.CaptureMetalScene();
        }

        void OnUpdate()
        {
            UpdateState();
        }
    }

    [EditorToolbarElement("SceneView/Scene Camera", typeof(SceneView))]
    sealed class SceneCameraElement : EditorToolbarDropdown, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public SceneCameraElement()
        {
            name = "SceneViewCamera";
            tooltip = "Settings for the Scene view camera.";

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            //Settings the icon display explicitly as this is set to DisplayStyle.Flex when icon = null
            //Here the icon is set using USS so on the C# side icon = null
            var iconElement = this.Q<Image>();
            iconElement.style.display = DisplayStyle.Flex;
            clickable.clickedWithEventInfo += OnClickableOnclickedWithEventInfo;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clickable.clickedWithEventInfo -= OnClickableOnclickedWithEventInfo;
        }

        void OnClickableOnclickedWithEventInfo(EventBase eventBase)
        {
            if (eventBase.eventTypeId == ContextClickEvent.TypeId())
                SceneViewCameraWindow.ShowContextMenu(sceneView);
            else
                PopupWindow.Show(worldBound, new SceneViewCameraWindow(sceneView));
        }
    }

    [EditorToolbarElement("SceneView/Gizmos", typeof(SceneView))]
    sealed class GizmosElement : EditorToolbarDropdownToggle, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }
        SceneView sceneView => containerWindow as SceneView;

        public GizmosElement()
        {
            name = "Gizmos";
            tooltip = L10n.Tr("Toggle visibility of all Gizmos in the Scene view");

            dropdownClicked += () => AnnotationWindow.ShowAtPosition(worldBound, false);

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.drawGizmos = evt.newValue;
            });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarStyles.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.drawGizmos;
            sceneView.drawGizmosChanged += SceneViewOndrawGizmosChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.drawGizmosChanged -= SceneViewOndrawGizmosChanged;
        }

        void SceneViewOndrawGizmosChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Search", typeof(SceneView))]
    sealed class SceneViewSearchElement : VisualElement, IAccessContainerWindow
    {
        public EditorWindow containerWindow { get; set; }

        public SceneViewSearchElement()
        {
            name = "Search";
            tooltip = "Search the Hierarchy / Scene View";
            SceneViewToolbarStyles.AddStyleSheets(this);
            Add(new IMGUIContainer { onGUIHandler = OnGUI });
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (containerWindow is SceneView sceneView)
                sceneView.ToolbarSearchFieldGUI();
            EditorGUILayout.EndHorizontal();
        }
    }
}
