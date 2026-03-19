// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [EditorToolbarElement("Tools/Builtin Tools")]
    sealed class BuiltinToolsStrip : VisualElement, IAccessContainerWindow
    {
        internal const string k_ToolGroupContainerClassName = "tool-group-container";
        internal const string k_ToolGroupOutlineClassName = "tool-group-outline";
        internal const string k_ToolGroupHeaderContainerClassName = "header-container";
        
        // builtin, global, grouped, component
        const int k_ToolbarSections = 4;
        
        VisualElement[] m_Toolbars;
        List<ToolEntry> m_AvailableTools = new();
        VisualElement m_Contexts;

        VisualElement defaultToolButtons => m_Toolbars[0];
        VisualElement customGlobalToolButtons => m_Toolbars[1];
        VisualElement groupedToolButtons => m_Toolbars[2];
        VisualElement componentToolButtons => m_Toolbars[3];

        private Type toolOwner => containerWindow?.GetType() ?? typeof(SceneView);

        EditorWindow m_ContainerWindow;
        public EditorWindow containerWindow
        {
            get => m_ContainerWindow;
            set
            {
                m_ContainerWindow = value;
                if (m_Contexts != null)
                    m_Contexts.userData = toolOwner;
            }
        }
        
        public BuiltinToolsStrip()
        {
            name = "BuiltinTools";
            SceneViewToolbarStyles.AddStyleSheets(this);
            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            m_Contexts = new ToolContextButton();
            m_Contexts.AddToClassList(EditorToolbarUtility.aloneStripElementClassName);
            Add(m_Contexts);

            m_Toolbars = new VisualElement[k_ToolbarSections]
            {
                new VisualElement() { name = "Builtin View and Transform Tools" },
                new VisualElement() { name = "Custom Global Tools" },
                new VisualElement(),
                new VisualElement()
            };

            for (int i = 0; i < k_ToolbarSections; ++i)
            {
                m_Toolbars[i].AddToClassList("toolbar-contents");
                Add(m_Toolbars[i]);
            }
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            EditorToolManager.availableToolsChangedForOwner += OnAvailableToolsChangedForOwner;
            EditorTool.stateChanged += OnEditorToolStateChanged;
            
            RebuildAvailableTools();
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorToolManager.availableToolsChangedForOwner -= OnAvailableToolsChangedForOwner;
            EditorTool.stateChanged -= OnEditorToolStateChanged;
        }

        void OnAvailableToolsChangedForOwner(Type changeToolOwner)
        {
            if (changeToolOwner == toolOwner)
                RebuildAvailableTools();
        }
        
        void RebuildAvailableTools()
        {
            EditorToolManager.GetAvailableTools(m_AvailableTools, toolOwner);
            
            foreach (var toolbar in m_Toolbars)
                toolbar.Clear();

            EditorToolUtility.OrderAvailableTools(m_AvailableTools);
            VisualElement curGroupContent = null;
            Type curGroupType = null;
            
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
                        defaultToolButtons.Add(new ToolButton((Tool)entry.scope, entry.tools, toolOwner));
                        break;
                    case ToolEntry.Scope.BuiltinAdditional:
                        defaultToolButtons.Add(new ToolButton(entry.tools, toolOwner));
                        break;
                    case ToolEntry.Scope.CustomGlobal:
                        customGlobalToolButtons.Add(new ToolButton(entry.tools, toolOwner));
                        break;
                    case ToolEntry.Scope.Grouped:
                        ProcessGroupedToolEntry(entry, entry.group, groupedToolButtons, "Grouped Tools", ref curGroupType, ref curGroupContent);
                        break;
                    case ToolEntry.Scope.Component:
                        ProcessGroupedToolEntry(entry, entry.targetBehaviour, componentToolButtons, "Component Tools", ref curGroupType, ref curGroupContent);
                        break;
                }
            }

            if (curGroupContent != null)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(curGroupContent);

            for (var i = 0; i < k_ToolbarSections - 2; i++)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(m_Toolbars[i]);
        }
        
        void OnEditorToolStateChanged(EditorTool tool)
        {
            RebuildAvailableTools();
        }
        
        void ProcessGroupedToolEntry(ToolEntry entry, Type groupType, VisualElement groupContainer, string groupContentName, 
            ref Type curGroupType, ref VisualElement curGroupContent)
        {
            foreach (var tool in entry.tools)
            {
                // Keep postponing building current group until a non-hidden tool is found.
                if (tool.isHidden)
                    return;
            }
            
            if (curGroupType == null || curGroupType != groupType)
            {
                curGroupType = groupType;
                if (curGroupType == null)
                    return;
                
                groupContainer.AddToClassList(k_ToolGroupContainerClassName);
                var outline = new VisualElement();
                outline.AddToClassList(k_ToolGroupOutlineClassName);
                outline.pickingMode = PickingMode.Ignore;
                
                if (curGroupContent != null)
                    EditorToolbarUtility.SetupChildrenAsButtonStrip(curGroupContent);

                curGroupContent = new VisualElement() { name = groupContentName };
                curGroupContent.AddToClassList("toolbar-contents");
                curGroupContent.AddToClassList(k_ToolGroupHeaderContainerClassName);
                curGroupContent.Add(outline);
                
                var iconTexture = EditorToolUtility.GetIcon(curGroupType)?.image as Texture2D;
                var headerToolbarIcon = new EditorToolbarIcon(curGroupType.Name, iconTexture);
                var groupHeader = new EditorToolbarHeader(headerToolbarIcon);
                
                groupHeader.tooltip = ObjectNames.NicifyVariableName(curGroupType.Name);
                if (curGroupType == typeof(CreationToolsGroup))
                    groupHeader.tooltip = CreationToolsGroup.k_Tooltip;
                groupHeader.userData = curGroupType;
                groupHeader.collapsed = EditorToolsSettings.IsGroupCollapsed(groupType, toolOwner);
                
                groupHeader.clicked += OnGroupHeaderClicked;
                curGroupContent.Add(groupHeader);
                groupContainer.Add(curGroupContent);
            }
            
            if (curGroupType == null || !EditorToolsSettings.IsGroupCollapsed(curGroupType, toolOwner))
                curGroupContent.Add(new ToolButton(entry.tools, toolOwner));
        }

        void OnGroupHeaderClicked(VisualElement headerVE)
        {
            var groupType = headerVE.userData as Type;
            var collapsed = !EditorToolsSettings.IsGroupCollapsed(groupType, toolOwner);
            EditorToolsSettings.SetGroupCollapsed(groupType, collapsed, toolOwner);
            RebuildAvailableTools();
        }
    }
}
