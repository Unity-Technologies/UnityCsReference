// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

    [Overlay(typeof(SceneView), k_Id, "Tools", true)]
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

    [Overlay(typeof(SceneView), k_Id, "View Options", true)]
    [Icon("Icons/Overlays/ViewOptions.png")]
    class SceneViewToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-scene-view-toolbar";
        public SceneViewToolBar() : base(
            "SceneView/Camera Mode",
            "SceneView/2D",
            "SceneView/Lighting",
            "SceneView/Audio",
            "SceneView/Fx",
            "SceneView/Scene Visibility",
            "SceneView/Render Doc",
            "SceneView/Metal Capture",
            "SceneView/Scene Camera",
            "SceneView/Gizmos") {}
    }

    [Overlay(typeof(SceneView), k_Id, "Search", true)]
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

    [Overlay(typeof(SceneView), k_Id, "Grid and Snap", true)]
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
