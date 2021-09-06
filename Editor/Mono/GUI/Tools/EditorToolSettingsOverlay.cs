// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    [Overlay(typeof(SceneView), "Tool Settings", true)]
    [Icon("Icons/Overlays/ToolSettings.png")]
    sealed class EditorToolSettingsOverlay : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        Editor m_Editor;
        VisualElement m_Content;
        // When this Overlay is requested to provide content in an unsupported layout, it can either collapse (toolbar)
        // or force itself to Panel layout. When that happens, m_CollapsedByInvalidLayout preserves the user requested
        // state so that if the tool contents change, we can attempt to restore the requested layout.
        bool m_CollapsedByInvalidLayout;

        VisualElement content
        {
            get
            {
                if (m_Content == null)
                    m_Content = new VisualElement() { name = "Tool Settings Container" };
                return m_Content;
            }
        }

        EditorToolSettingsOverlay()
        {
            ToolManager.activeToolChanged += OnToolChanged;
        }

        public override VisualElement CreatePanelContent() { CreateEditorContent(); return content; }

        public VisualElement CreateHorizontalToolbarContent() { CreateEditorContent(); return content; }

        public VisualElement CreateVerticalToolbarContent() { CreateEditorContent(); return content; }

        protected internal override Layout supportedLayouts
        {
            get
            {
                var ret = Layout.Panel;
                if (m_Editor == null)
                    return ret;
                if (m_Editor is ICreateHorizontalToolbar)
                    ret |= Layout.HorizontalToolbar;
                if (m_Editor is ICreateVerticalToolbar)
                    ret |= Layout.VerticalToolbar;
                return ret;
            }
        }

        void OnToolChanged()
        {
            // When collapsed an Overlay won't rebuild it's contents. However we need to create a new editor to find
            // eligible layouts, so if the Overlay is collapsed we'll force a rebuild of the editor content.
            if (collapsed)
            {
                CreateEditorContent();
                if(m_CollapsedByInvalidLayout)
                    collapsed = false;
                m_CollapsedByInvalidLayout = false;
            }
            else
            {
                // If m_DesiredLayout is not matching the requested layout, that means that this Overlay was forced
                // to default to Panel layout. Try to restore the originally requested layout.
                RebuildContent(layout);

                // When in a toolbar, if the content does not support vertical/horizontal toolbar layout, collapse it.
                m_CollapsedByInvalidLayout = isInToolbar && !collapsed && !container.IsOverlayLayoutSupported(supportedLayouts);

                // If not in a toolbar, but still requesting an unsupported layout, also need to collapse.
                m_CollapsedByInvalidLayout |= !isInToolbar && (supportedLayouts & layout) != layout;

                collapsed = m_CollapsedByInvalidLayout;
            }
        }

        void CreateEditorContent()
        {
            m_Content?.RemoveFromHierarchy();
            m_Content = null;

            if (m_Editor != null)
                UnityObject.DestroyImmediate(m_Editor);

            m_Editor = Editor.CreateEditor(EditorToolManager.activeTool);

            switch (layout)
            {
                case Layout.HorizontalToolbar:
                    if (m_Editor is ICreateHorizontalToolbar horizontal)
                    {
                        var toolbar = horizontal.CreateHorizontalToolbarContent();

                        if (toolbar != null)
                        {
                            content.Add(toolbar);
                            break;
                        }
                    }
                    goto default;

                case Layout.VerticalToolbar:
                    if (m_Editor is ICreateVerticalToolbar vertical)
                    {
                        var toolbar = vertical.CreateVerticalToolbarContent();

                        if (toolbar != null)
                        {
                            content.Add(toolbar);
                            break;
                        }
                    }
                    goto default;

                default:
                    content.Add(m_Editor.CreateInspectorGUI() ?? new IMGUIContainer(m_Editor.OnInspectorGUI));
                    break;
            }
        }
    }
}
