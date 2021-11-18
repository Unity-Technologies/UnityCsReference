// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Builtin Tools")]
    sealed class BuiltinToolsStrip : VisualElement
    {
        VisualElement m_DefaultTools;

        public BuiltinToolsStrip()
        {
            name = "BuiltinTools";

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            // Only show the contexts dropdown if there are non-builtin contexts available
            if (EditorToolUtility.toolContextsInProject > 1)
            {
                var contexts = new ToolContextButton();
                contexts.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
                Add(contexts);
            }

            // View and Builtin Transform Tools
            m_DefaultTools = new VisualElement() { name = "Builtin View and Transform Tools" };
            m_DefaultTools.AddToClassList("toolbar-contents");
            Add(m_DefaultTools);

            this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            this.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            CreateDefaultTools();

            // Custom global tools button (only shown if custom global tools exist in project)
            // A single "overflow" toggle+dropdown for additional global tools
            var customToolButton = new LastCustomToolButton();
            customToolButton.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
            Add(customToolButton);

            // Component tools are last
            Add(new ComponentToolsStrip());
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeContextChanged += CreateDefaultTools;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeContextChanged -= CreateDefaultTools;
        }

        void CreateDefaultTools()
        {
            m_DefaultTools.Clear();

            for (int i = 0; i < (int)Tool.Custom; i++)
            {
                var button = new ToolButton((Tool) i);
                button.displayChanged += () => EditorToolbarUtility.SetupChildrenAsButtonStrip(m_DefaultTools);
                m_DefaultTools.Add(button);
            }

            foreach (var type in EditorToolManager.activeToolContext.GetAdditionalToolTypes())
            {
                if (!typeof(EditorTool).IsAssignableFrom(type))
                {
                    Debug.LogError($"{type} must be assignable to EditorTool, and not abstract.");
                    continue;
                }

                var addl = EditorToolManager.GetSingleton(type) as EditorTool;
                var button = new ComponentToolButton<EditorTool>(addl);
                m_DefaultTools.Add(button);
            }

            EditorToolbarUtility.SetupChildrenAsButtonStrip(m_DefaultTools);
        }
    }
}
