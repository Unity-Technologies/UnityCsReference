// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Builtin Tools")]
    sealed class BuiltinToolsStrip : VisualElement
    {
        // builtin, global, component
        const int k_ToolbarSections = 3;

        VisualElement[] m_Toolbars;
        List<ToolEntry> m_AvailableTools = new List<ToolEntry>();

        VisualElement defaultToolButtons => m_Toolbars[0];
        VisualElement customGlobalToolButtons => m_Toolbars[1];
        VisualElement componentToolButtons => m_Toolbars[2];

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

            m_Toolbars = new VisualElement[k_ToolbarSections]
            {
                new VisualElement() { name = "Builtin View and Transform Tools" },
                new VisualElement() { name = "Custom Global Tools" },
                new VisualElement() { name = "Component Tools" }
            };

            for (int i = 0; i < k_ToolbarSections; ++i)
            {
                m_Toolbars[i].AddToClassList("toolbar-contents");
                Add(m_Toolbars[i]);
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            RebuildAvailableTools();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            EditorToolManager.availableToolsChanged += RebuildAvailableTools;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorToolManager.availableToolsChanged -= RebuildAvailableTools;
        }

        void RebuildAvailableTools()
        {
            EditorToolManager.GetAvailableTools(m_AvailableTools);

            foreach (var toolbar in m_Toolbars)
                toolbar.Clear();

            // Builtin tools are handled as a special case because order needs to be consistent
            for (int i = (int)Tool.View; i < (int)Tool.Custom; ++i)
            {
                var tool = m_AvailableTools.FindIndex(x=> (Tool) x.scope == (Tool) i);
                if(tool > -1)
                    defaultToolButtons.Add(new ToolButton((Tool) i, m_AvailableTools[tool].tools));
            }

            m_AvailableTools.Sort((a, b) =>
            {
                if (a.priority == ToolAttribute.defaultPriority && b.priority == ToolAttribute.defaultPriority)
                    return a.GetHashCode().CompareTo(b.GetHashCode());
                return a.priority.CompareTo(b.priority);
            });

            foreach (var entry in m_AvailableTools)
            {
                switch (entry.scope)
                {
                    case ToolEntry.Scope.BuiltinAdditional:
                        defaultToolButtons.Add(new ToolButton(entry.tools));
                        break;
                    case ToolEntry.Scope.CustomGlobal:
                        customGlobalToolButtons.Add(new ToolButton(entry.tools));
                        break;
                    case ToolEntry.Scope.Component:
                        componentToolButtons.Add(new ToolButton(entry.tools));
                        break;
                }
            }

            foreach(var toolbar in m_Toolbars)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(toolbar);
        }
    }
}
