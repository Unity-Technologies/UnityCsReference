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
        VisualElement content
        {
            get
            {
                if (m_Content == null)
                    m_Content = new VisualElement() { name = "Tool Settings Container" };
                return m_Content;
            }
        }

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

        public EditorToolSettingsOverlay()
        {
            ToolManager.activeToolChanged += OnToolChanged;
            CreateEditor();
        }

        void CreateEditor()
        {
            UnityObject.DestroyImmediate(m_Editor);
            m_Editor = Editor.CreateEditor(EditorToolManager.activeTool);
        }

        void OnToolChanged()
        {
            CreateEditor();
            RebuildContent();
        }

        public override VisualElement CreatePanelContent()
        {
            CreateEditorContent();
            return content;
        }

        public VisualElement CreateHorizontalToolbarContent()
        {
            CreateEditorContent();
            return content;
        }

        public VisualElement CreateVerticalToolbarContent()
        {
            CreateEditorContent();
            return content;
        }

        void CreateEditorContent()
        {
            m_Content?.RemoveFromHierarchy();
            m_Content = null;

            if(m_Editor == null)
                return;

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
