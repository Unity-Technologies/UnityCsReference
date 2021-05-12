// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [Overlay(typeof(EditorWindow), k_Id, "Tools", true)]
    [Icon("Icons/Overlays/StandardTools.png")]
    class TransformToolsOverlayToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-transform-toolbar";

        protected override void PopulateToolbar(EditorToolbar toolbar)
        {
            toolbar.AddElement("Tools/Builtin Tools");
        }
    }

    [Overlay(typeof(SceneView), k_Id, "SceneView toolbar", true)]
    [Icon("Icons/Overlays/ViewOptions.png")]
    class SceneViewToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-scene-view-toolbar";
        protected override void PopulateToolbar(EditorToolbar toolbar)
        {
            toolbar.AddElement("SceneView/Camera Mode");
            toolbar.AddElement("SceneView/2D");
            toolbar.AddElement("SceneView/Lighting");
            toolbar.AddElement("SceneView/Audio");
            toolbar.AddElement("SceneView/Fx");
            toolbar.AddElement("SceneView/Scene Visibility");
            toolbar.AddElement("SceneView/Render Doc");
            toolbar.AddElement("SceneView/Metal Capture");
            toolbar.AddElement("SceneView/Scene Camera");
            toolbar.AddElement("SceneView/Gizmos");
        }
    }

    [Overlay(typeof(EditorWindow), k_Id, "Search", true)]
    [Icon("Icons/Overlays/SearchOverlay.png")]
    class SearchToolBar : Overlay, ICreateHorizontalToolbar
    {
        const string k_Id = "unity-search-toolbar";

        public override VisualElement CreatePanelContent()
        {
            return new IMGUIContainer { onGUIHandler = OnGUI };
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (containerWindow is SceneView sceneView)
                sceneView.ToolbarSearchFieldGUI();
            EditorGUILayout.EndHorizontal();
        }

        public VisualElement CreateHorizontalToolbarContent() => CreatePanelContent();
    }

    [Overlay(typeof(SceneView), k_Id, "Grid and Snap toolbar", true)]
    [Icon("Icons/Overlays/GridAndSnap.png")]
    class GridAndSnapToolBar : ToolbarOverlay
    {
        const string k_Id = "unity-grid-and-snap-toolbar";
        protected override void PopulateToolbar(EditorToolbar toolbar)
        {
            toolbar.AddElement("SceneView/Grids");
            toolbar.AddElement("Tools/Snap Settings");
            toolbar.AddElement("SceneView/Snap Increment");
        }
    }
}
// namespace
