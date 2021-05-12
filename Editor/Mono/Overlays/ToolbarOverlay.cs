// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    abstract class ToolbarOverlay : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        EditorToolbar m_Toolbar;

        public VisualElement CreateHorizontalToolbarContent() => CreateToolbarContent();

        public VisualElement CreateVerticalToolbarContent() => CreateToolbarContent();

        public override VisualElement CreatePanelContent() => CreateToolbarContent();

        VisualElement CreateToolbarContent()
        {
            rootVisualElement.AddToClassList("unity-toolbar-overlay");
            var toolbarRoot = new VisualElement { name = "toolbar-overlay" };
            m_Toolbar = new EditorToolbar(canvas.containerWindow, toolbarRoot);
            PopulateToolbar(m_Toolbar);
            return toolbarRoot;
        }

        protected virtual void PopulateToolbar(EditorToolbar toolbar)
        {
        }
    }
}
