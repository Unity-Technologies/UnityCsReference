// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
     // See also
     // - UIServiceEditor/SceneView/SceneViewToolbarElements.cs
     // - UIServiceEditor/EditorToolbar/ToolbarElements/BuiltinTools.cs

    [Overlay(typeof(SceneView), k_Id, "Tools", true, priority = (int)OverlayPriority.Tools)]
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
            if(!Tools.viewToolActive)
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

    [Overlay(typeof(SceneView), k_Id, "View Options", true, priority = (int)OverlayPriority.ViewOptions)]
    [Icon("Icons/Overlays/ViewOptions.png")]
    class SceneViewToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-scene-view-toolbar";
        public SceneViewToolBar() : base(
            "SceneView/2D",
            "SceneView/Audio",
            "SceneView/Fx",
            "SceneView/Scene Visibility",
            "SceneView/Render Doc",
            "SceneView/Metal Capture",
            "SceneView/Scene Camera",
            "SceneView/Gizmos") {}
    }

    [Overlay(typeof(SceneView), k_Id, "Draw Modes", true, priority = (int)OverlayPriority.DrawModes, defaultDockIndex = 1, defaultDockPosition = DockPosition.Bottom, defaultLayout = Layout.HorizontalToolbar, defaultDockZone = DockZone.TopToolbar)]
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

    [Overlay(typeof(SceneView), k_Id, "Search", true, priority = (int)OverlayPriority.Search)]
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

    [Overlay(typeof(SceneView), k_Id, "Grid and Snap", true, priority = (int)OverlayPriority.GridAndSnap)]
    [Icon("Icons/Overlays/GridAndSnap.png")]
    class GridAndSnapToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-grid-and-snap-toolbar";

        public GridAndSnapToolBar() : base(
            "Tools/Snap Size",
            "SceneView/Grids",
            "Tools/Snap Settings",
            "SceneView/Snap Increment") {}
    }
}
// namespace
