// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEditor.Snap;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using FrameCapture = UnityEngine.Apple.FrameCapture;
using FrameCaptureDestination = UnityEngine.Apple.FrameCaptureDestination;

namespace UnityEditor.Toolbars
{
    static class SceneViewToolbarElements
    {
        const string k_StyleSheet = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElements.uss";
        const string k_StyleLight = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsLight.uss";
        const string k_StyleDark = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsDark.uss";

        static StyleSheet s_Style;
        static StyleSheet s_Skin;
        static internal void AddStyleSheets(VisualElement ve)
        {
            if (s_Skin == null)
            {
                if (EditorGUIUtility.isProSkin)
                    s_Skin = EditorGUIUtility.Load(k_StyleDark) as StyleSheet;
                else
                    s_Skin = EditorGUIUtility.Load(k_StyleLight) as StyleSheet;
            }
            if (s_Style == null)
            {
                s_Style = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
            }
            ve.styleSheets.Add(s_Style);
            ve.styleSheets.Add(s_Skin);
        }
    }

    [EditorToolbarElement("SceneView/Camera Mode", typeof(SceneView))]
    sealed class CameraModeElement : ToolbarButton, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        VisualElement m_Icon;

        public CameraModeElement()
        {
            name = "CameraModeDropDown";
            tooltip = L10n.Tr("The Draw Mode used to display the Scene.");

            m_Icon = EditorToolbarUtility.AddIconElement(this);
            EditorToolbarUtility.AddArrowElement(this);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += () => PopupWindow.Show(worldBound, new SceneRenderModeWindow(sceneView));
            SceneViewOnCameraModeChanged(sceneView.cameraMode);
            sceneView.onCameraModeChanged += SceneViewOnCameraModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.onCameraModeChanged -= SceneViewOnCameraModeChanged;
        }

        void SceneViewOnCameraModeChanged(SceneView.CameraMode mode)
        {
            var shaded = mode.name == "Shaded";
            var wireframe =  mode.name == "Wireframe";
            var shadedWireframe = mode.name == "Shaded Wireframe";
            m_Icon.EnableInClassList("shaded", shaded);
            m_Icon.EnableInClassList("wireframe", wireframe);
            m_Icon.EnableInClassList("shaded-wireframe", shadedWireframe);
            m_Icon.EnableInClassList("debug", !(shaded || wireframe || shadedWireframe));
        }
    }

    [EditorToolbarElement("SceneView/2D", typeof(SceneView))]
    sealed class In2DModeElement : EditorToolbarToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public In2DModeElement()
        {
            name = "SceneView2D";
            tooltip = L10n.Tr("When toggled on, the Scene is in 2D view. When toggled off, the Scene is in 3D view.");
            this.RegisterValueChangedCallback(evt => sceneView.in2DMode = evt.newValue);
            text = "2D";
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.in2DMode;
            sceneView.modeChanged2D += OnModeChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.modeChanged2D -= OnModeChanged;
        }

        void OnModeChanged(bool enabled)
        {
            value = enabled;
        }
    }

    [EditorToolbarElement("SceneView/Lighting", typeof(SceneView))]
    sealed class SceneLightingElement : EditorToolbarToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SceneLightingElement()
        {
            name = "SceneviewLighting";
            tooltip = L10n.Tr("When toggled on, the Scene lighting is used. When toggled off, a light attached to the Scene view camera is used.");
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => sceneView.sceneLighting = evt.newValue);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.sceneLighting;
            sceneView.sceneLightingChanged += SceneViewOnsceneLightingChanged;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.sceneLightingChanged -= SceneViewOnsceneLightingChanged;
        }

        void SceneViewOnsceneLightingChanged(bool lightingOn)
        {
            value = lightingOn;
        }
    }

    [EditorToolbarElement("SceneView/Audio", typeof(SceneView))]
    sealed class SceneAudioElement : EditorToolbarToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SceneAudioElement()
        {
            name = "SceneviewAudio";
            tooltip = "Toggle audio on or off.";
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            this.RegisterValueChangedCallback(evt => sceneView.audioPlay = evt.newValue);
            SceneViewToolbarElements.AddStyleSheets(this);
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
    sealed class SceneFxElement : DropdownToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SceneFxElement()
        {
            name = "SceneviewFx";
            tooltip = L10n.Tr("Toggle skybox, fog, and various other effects.");
            dropdownButton.clicked += () => PopupWindow.Show(worldBound, new SceneFXWindow(sceneView));
            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.sceneViewState.fxEnabled = evt.newValue;
            });

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            sceneView.sceneViewState.fxEnableChanged += OnSceneFxChanged;
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
    sealed class SceneVisElement : EditorToolbarToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SceneVisElement()
        {
            name = "SceneViewVisibility";
            tooltip = "Number of hidden objects, click to toggle scene visibility";
            this.RegisterValueChangedCallback(evt => sceneView.sceneVisActive = evt.newValue);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
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
    sealed class SceneViewGridSettingsElement : DropdownToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

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
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnClickableOnclicked()
        {
            GridSettingsWindow.ShowDropDownAtTrigger(this, context as SceneView);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            value = sceneView.sceneViewGrids.showGrid;
            sceneView.gridVisibilityChanged += SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged += OnSceneViewOngridRenderAxisChanged;
            OnSceneViewOngridRenderAxisChanged(sceneView.sceneViewGrids.gridAxis);
            dropdownButton.clicked += OnClickableOnclicked;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            sceneView.gridVisibilityChanged -= SceneViewOngridVisibilityChanged;
            sceneView.sceneViewGrids.gridRenderAxisChanged -= OnSceneViewOngridRenderAxisChanged;
            dropdownButton.clicked -= OnClickableOnclicked;
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
    sealed class RenderDocElement : ToolbarButton, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public RenderDocElement()
        {
            name = "FrameCapture";
            tooltip = L10n.Tr(RenderDocUtil.openInRenderDocLabel);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);

            EditorToolbarUtility.AddIconElement(this);
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
            style.display = RenderDoc.IsLoaded() && RenderDoc.IsSupported() ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    [EditorToolbarElement("SceneView/Metal Capture", typeof(SceneView))]
    sealed class MetalCaptureElement : ToolbarButton, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public MetalCaptureElement()
        {
            name = "MetalCapture";
            tooltip = L10n.Tr("Capture the current view and open in Xcode frame debugger");
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
            EditorToolbarUtility.AddIconElement(this);
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
            style.display = FrameCapture.IsDestinationSupported(FrameCaptureDestination.DevTools)
                || FrameCapture.IsDestinationSupported(FrameCaptureDestination.GPUTraceDocument) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    [EditorToolbarElement("SceneView/Scene Camera", typeof(SceneView))]
    sealed class SceneCameraElement : ToolbarButton, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SceneCameraElement()
        {
            name = "SceneViewCamera";
            tooltip = "Settings for the Scene view camera.";

            EditorToolbarUtility.AddIconElement(this);
            EditorToolbarUtility.AddArrowElement(this);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
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
    sealed class GizmosElement : DropdownToggle, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;
        // ToolbarToggle m_Toggle;

        public GizmosElement()
        {
            name = "Gizmos";
            tooltip = L10n.Tr("Toggle visibility of all Gizmos in the Scene view");

            dropdownButton.clicked += () => AnnotationWindow.ShowAtPosition(worldBound, false);

            this.RegisterValueChangedCallback(delegate(ChangeEvent<bool> evt)
            {
                sceneView.drawGizmos = evt.newValue;
            });
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
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

    [EditorToolbarElement("SceneView/Snap Increment", typeof(SceneView))]
    sealed class SnapIncrementSettingsElement : ToolbarButton, IEditorToolbarContext
    {
        public object context { get; set; }
        SceneView sceneView => context as SceneView;

        public SnapIncrementSettingsElement()
        {
            name = "SnapIncrement";
            tooltip = "Snap Increment";

            EditorToolbarUtility.AddIconElement(this);
            EditorToolbarUtility.AddArrowElement(this);
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            SceneViewToolbarElements.AddStyleSheets(this);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            clicked += OnOnclicked;
        }

        void OnOnclicked()
        {
            OverlayPopupWindow.ShowOverlayPopup<SnapIncrementSettingsWindow>(this, new Vector2(300, 88));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            clicked -= OnOnclicked;
        }
    }
}
// namespace
