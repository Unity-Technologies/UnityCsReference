// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
     // See also
     // - UIServiceEditor/SceneView/SceneViewToolbarElements.cs
     // - UIServiceEditor/EditorToolbar/ToolbarElements/BuiltinTools.cs
     
    [Overlay(typeof(ISupportsEditorTools), k_Id, "Tools", true, priority = (int)OverlayPriority.Tools, defaultDockZone = DockZone.LeftColumn, defaultDockPosition = DockPosition.Top, defaultLayout = Layout.VerticalToolbar, defaultDockIndex = 0, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/ToolsToggle.png")]
    class TransformToolsOverlayToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-transform-toolbar";
        public TransformToolsOverlayToolBar() : base("Tools/Builtin Tools") {}

        public override void OnCreated()
        {
            base.OnCreated();
            ToolManager.activeToolChanged += UpdateActiveToolIcon;
            SceneViewMotion.viewToolActiveChanged += UpdateActiveToolIcon;
            Tools.viewToolChanged += UpdateViewToolIcon;
            UpdateActiveToolIcon();
        }

        public override void OnWillBeDestroyed()
        {
            ToolManager.activeToolChanged -= UpdateActiveToolIcon;
            SceneViewMotion.viewToolActiveChanged -= UpdateActiveToolIcon;
            Tools.viewToolChanged -= UpdateViewToolIcon;
            base.OnWillBeDestroyed();
        }

        void UpdateActiveToolIcon()
        {
            if(Tools.viewToolActive)
                UpdateViewToolIcon();
            else
                collapsedIcon = EditorToolUtility.IsManipulationTool(Tools.current) ?
                                EditorToolUtility.GetToolbarIcon(EditorToolManager.activeTool)?.image as Texture2D :
                                null;
        }

        void UpdateViewToolIcon()
        {
            if (!Tools.viewToolActive)
                return;

            switch (Tools.viewTool)
            {
                case ViewTool.Orbit:
                case ViewTool.FPS:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("ViewToolOrbit");
                    break;
                case ViewTool.Pan:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("ViewToolMove");
                    break;
                case ViewTool.Zoom:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("ViewToolZoom");
                    break;
            }
        }
    }

    [Overlay(typeof(SceneView), k_Id, "View Options", true, priority = (int)OverlayPriority.ViewOptions, defaultDockIndex = 0, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/ViewOptions.png")]
    class SceneViewToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-scene-view-toolbar";
        
        Editor m_ToolEditor, m_ContextEditor;
        
        static IReadOnlyList<string> builtinToolbarElements = new[] 
        {
            "SceneView/2D",
            "SceneView/Audio",
            "SceneView/Fx",
            "SceneView/Scene Visibility",
            "SceneView/Render Doc",
            "SceneView/Metal Capture",
            "SceneView/Layers",
            "SceneView/Scene Camera",
            "SceneView/Gizmos"
        };
        
        public override IEnumerable<string> toolbarElements
        {
            get
            {
                var elements = contextElements != null ? new List<string>(contextElements) : new List<string>();
                elements.AddRange(toolElements);
                return elements;
            }
        }
        
        IReadOnlyList<string> contextElements
        {
            get
            {
                if (m_ContextEditor is IOverrideToolbar)
                {
                    return EditorToolbarUtility.GetToolbarOverrideElementIds(m_ContextEditor, (IEnumerable<string>)null,
                        OverridableToolbar.ViewOptions);
                }

                return null;
            }
        }

        IReadOnlyList<string> toolElements
        {
            get
            {
                if (m_ToolEditor is IOverrideToolbar)
                {
                    return EditorToolbarUtility.GetToolbarOverrideElementIds(m_ToolEditor, builtinToolbarElements,
                        OverridableToolbar.ViewOptions);
                }
                
                return builtinToolbarElements;
            }
        }
        
        public SceneViewToolBar()
        {
            ToolManager.activeToolChanged += OnToolChanged;
            ToolManager.activeContextChanged += OnToolChanged;
            CreateEditor();
        }

        public override void OnWillBeDestroyed()
        {
            UnityObject.DestroyImmediate(m_ToolEditor);
            UnityObject.DestroyImmediate(m_ContextEditor);
        }

        void CreateEditor()
        {
            UnityObject.DestroyImmediate(m_ContextEditor);
            UnityObject.DestroyImmediate(m_ToolEditor);

            m_ContextEditor = Editor.CreateEditor(EditorToolManager.activeToolContext);
            m_ToolEditor = Editor.CreateEditor(EditorToolManager.activeTool);
        }

        void OnToolChanged()
        {
            CreateEditor();
            RebuildContent();
        }
    }

    [Overlay(typeof(SceneView), k_Id, "Draw Modes", true, priority = (int)OverlayPriority.DrawModes, defaultDockIndex = 1, defaultDockPosition = DockPosition.Bottom, defaultDockZone = DockZone.TopToolbar, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/ViewOptions.png")]
    class SceneViewCameraModeToolbar : ToolbarOverlay
    {
        const string k_Id = "unity-scene-view-camera-mode-toolbar";
        public SceneViewCameraModeToolbar() : base(
            "SceneView/Common Camera Mode",
            "SceneView/Camera Mode") {}

        private SceneView m_SceneView;

        public override void OnCreated()
        {
            m_SceneView = containerWindow as SceneView;
            m_SceneView.onCameraModeChanged += UpdateIcon;
            UpdateIcon(m_SceneView.cameraMode);
        }

        public override void OnWillBeDestroyed()
        {
            m_SceneView.onCameraModeChanged -= UpdateIcon;
        }

        internal void UpdateIcon(SceneView.CameraMode cameraMode)
        {
            switch (cameraMode.drawMode)
            {
                case DrawCameraMode.Wireframe:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("Toolbars/wireframe");
                    break;
                case DrawCameraMode.TexturedWire:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("Toolbars/ShadedWireframe");
                    break;
                case DrawCameraMode.Textured when !m_SceneView.sceneLighting:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("Toolbars/UnlitMode");
                    break;
                case DrawCameraMode.Textured:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("Toolbars/Shaded");
                    break;
                default:
                    collapsedIcon = EditorGUIUtility.LoadIconRequired("Toolbars/debug");
                    break;
            }
        }
    }

    [Overlay(typeof(SceneView), k_Id, "Search", false, priority = (int)OverlayPriority.Search, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Bottom, defaultDockIndex = 2, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/SearchOverlay.png")]
    class SearchToolBar : Overlay, ICreateHorizontalToolbar
    {
        const string k_Id = "unity-search-toolbar";

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            return EditorToolbar.CreateOverlay(toolbarElements, containerWindow);
        }

        public override VisualElement CreatePanelContent()
        {
            return CreateHorizontalToolbarContent();
        }

        public IEnumerable<string> toolbarElements
        {
            get { yield return "SceneView/Search"; }
        }
    }

    [Overlay(typeof(SceneView), k_Id, "Grid and Snap", true, priority = (int)OverlayPriority.GridAndSnap, defaultDockZone = DockZone.TopToolbar, defaultDockPosition = DockPosition.Top, defaultDockIndex = 1, group = OverlayAttribute.unityGroup)]
    [Icon("Icons/Overlays/GridAndSnap.png")]
    class GridAndSnapToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-grid-and-snap-toolbar";
        const string k_GridAndSnapUSSClass = "grid-and-snap-toolbar";

        public GridAndSnapToolBar() : base(
            "SceneView/GridVisibility",
            "SceneView/Separator",
            "Tools/Snap Settings",
            "SceneView/Separator",
            "Tools/Angle Snap Settings",
            "Tools/Scale Snap Settings")
        {
            rootVisualElement.AddToClassList(k_GridAndSnapUSSClass);
        }
    }
}
// namespace
