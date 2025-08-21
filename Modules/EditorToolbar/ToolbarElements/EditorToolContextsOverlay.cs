// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEditor.EditorTools;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [Overlay(typeof(SceneView), k_Id, k_OverlayName,
        priority = (int)OverlayPriority.ToolContexts,
        group = OverlayAttribute.unityGroup,
        defaultDisplay = false,
        defaultDockZone = DockZone.TopToolbar,
        defaultDockPosition = DockPosition.Top,
        defaultDockIndex = 0)]
    [Icon("Icons/Overlays/ToolContext.png")]
    class EditorToolContextsOverlay : Overlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        internal const string k_Id = "unity-tool-contexts-toolbar";
        const string k_OverlayName = "Tool Contexts";
        const string k_ToolbarContentsUssClassName = "toolbar-contents";

        OverlayToolbar m_Root;
        readonly Dictionary<Type, Toggle> m_CtxTypeToToggle = new();

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            return CreatePanelContent() as OverlayToolbar;
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            return CreatePanelContent() as OverlayToolbar;
        }

        public override VisualElement CreatePanelContent()
        {
            m_Root = new OverlayToolbar();
            RebuildContextButtons();
            return m_Root;
        }

        public override void OnCreated()
        {
            EditorToolManager.availableToolsChanged += RebuildContextButtons;
            ToolManager.activeContextChanged += RefreshToggleStates;
        }

        public override void OnWillBeDestroyed()
        {
            EditorToolManager.availableToolsChanged -= RebuildContextButtons;
            ToolManager.activeContextChanged -= RefreshToggleStates;
        }

        void RefreshToggleStates()
        {
            var activeContextType = ToolManager.activeContextType;
            foreach (var kvp in m_CtxTypeToToggle)
            {
                var toggle = kvp.Value;
                toggle.SetValueWithoutNotify(kvp.Key == activeContextType);
            }
        }

        void RebuildContextButtons()
        {
            if (!displayed)
                return;

            m_Root.Clear();
            m_CtxTypeToToggle.Clear();

            var containerVE = new VisualElement();
            SceneViewToolbarStyles.AddStyleSheets(containerVE);
            containerVE.AddToClassList(EditorToolbar.elementClassName);
            containerVE.AddToClassList("toolbar-contents");

            m_Root.Add(containerVE);

            VisualElement goGroupVE = new VisualElement();
            var sortedCtxs = EditorToolUtility.sortedContextsDataCache.allAvailableContextAssociations;

            AddContextToggle(sortedCtxs[0].editor, goGroupVE);
            EditorToolbarUtility.SetupChildrenAsButtonStrip(goGroupVE);
            containerVE.Add(goGroupVE);

            VisualElement globalGroupVE = null;
            VisualElement componentGroupVE = null;
            for (int i = 1; i < sortedCtxs.Count; ++i)
            {
                var ctx = sortedCtxs[i];
                if (ctx.targetBehaviour == typeof(NullTargetKey)) // Global context
                {
                    if (globalGroupVE == null)
                    {
                        globalGroupVE = new VisualElement();
                        globalGroupVE.AddToClassList(k_ToolbarContentsUssClassName);
                        containerVE.Add(globalGroupVE);
                    }

                    AddContextToggle(ctx.editor, globalGroupVE);
                }
                else // Component context
                {
                    if (componentGroupVE == null)
                    {
                        componentGroupVE = new VisualElement();
                        componentGroupVE.AddToClassList(k_ToolbarContentsUssClassName);
                        containerVE.Add(componentGroupVE);
                    }

                    AddContextToggle(ctx.editor, componentGroupVE);
                }
            }

            if (globalGroupVE != null)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(globalGroupVE);

            if (componentGroupVE != null)
                EditorToolbarUtility.SetupChildrenAsButtonStrip(componentGroupVE);

            RefreshToggleStates();
        }

        void AddContextToggle(Type contextType, VisualElement parent)
        {
            Texture2D icon = null;
            var iconContent = EditorToolUtility.GetContextIcon(contextType, out var isFallbackIcon);
            if (!isFallbackIcon)
                icon = iconContent.image as Texture2D;

            var text = icon == null ?
                OverlayUtilities.GetSignificantLettersForIcon(contextType.Name) :
                string.Empty;

            var toggle = new EditorToolbarToggle()
            {
                icon = icon,
                text = text,
                tooltip = EditorToolUtility.GetContextName(contextType, true),
                userData = contextType
            };

            toggle.RegisterValueChangedCallback((_) =>
            {
                toggle.SetValueWithoutNotify(true);
                ToolManager.SetActiveContext(contextType);
            });

            parent.Add(toggle);
            m_CtxTypeToToggle.Add(contextType, toggle);
        }
    }
}
