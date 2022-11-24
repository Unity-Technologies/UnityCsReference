// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
                new VisualElement()
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

            m_AvailableTools.OrderBy(x => x.scope)
                .ThenBy(x => x.GetHashCode())
                .ThenBy(x => x.priority);

            Type currentComponentHeaderType = null;
            VisualElement componentTools = null;
            foreach (var entry in m_AvailableTools)
            {
                switch (entry.scope)
                {
                    case ToolEntry.Scope.BuiltinView:
                    case ToolEntry.Scope.BuiltinMove:
                    case ToolEntry.Scope.BuiltinRotate:
                    case ToolEntry.Scope.BuiltinScale:
                    case ToolEntry.Scope.BuiltinRect:
                    case ToolEntry.Scope.BuiltinTransform:
                        defaultToolButtons.Add(new ToolButton((Tool)entry.scope, entry.tools));
                        break;
                    case ToolEntry.Scope.BuiltinAdditional:
                        defaultToolButtons.Add(new ToolButton(entry.tools));
                        break;
                    case ToolEntry.Scope.CustomGlobal:
                        customGlobalToolButtons.Add(new ToolButton(entry.tools));
                        break;
                    case ToolEntry.Scope.Component:
                        if (currentComponentHeaderType == null || currentComponentHeaderType != entry.tools[0].target?.GetType())
                        {
                            currentComponentHeaderType = entry.tools[0].target?.GetType();
                            if (currentComponentHeaderType == null)
                                break;

                            if (componentTools != null)
                                EditorToolbarUtility.SetupChildrenAsButtonStrip(componentTools);

                            componentTools = new VisualElement() { name = "Component Tools" };
                            componentTools.AddToClassList("toolbar-contents");

                            var header = new EditorToolbarIcon();
                            if ((header.icon = EditorGUIUtility.FindTexture(currentComponentHeaderType)) == null)
                                header.textIcon = currentComponentHeaderType.Name;

                            componentToolButtons.Add(header);
                            componentToolButtons.Add(componentTools);
                        }
                        componentTools.Add(new ToolButton(entry.tools));
                        break;
                }
            }

            if (componentTools != null)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(componentTools);

            for (var i = 0; i < k_ToolbarSections - 1; i++)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(m_Toolbars[i]);
        }
    }
}
