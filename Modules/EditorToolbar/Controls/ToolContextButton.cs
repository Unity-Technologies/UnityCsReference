// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class ToolContextButton : EditorToolbarDropdown
    {
        static readonly string k_ToolContextsScriptingPage =
            $"https://docs.unity3d.com/{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/ScriptReference/EditorTools.EditorToolContext.html";
        
        static readonly string k_Tooltip = L10n.Tr("The tool context determines what the Move, Rotate, Scale, Rect, and Transform tools" +
                                          " select and modify. GameObject is the default, and allows you to work with " +
                                          "GameObjects. Additional contexts allow you to edit different objects.");
        
        static readonly string k_DropdownUssClassName = "editor-tools-dropdown";
        static readonly string k_DropdownItemIConUssClassName = k_DropdownUssClassName + "__item-icon";
        static readonly string k_MenuInfoContainerUssClassName = k_DropdownUssClassName + "__info-container";
        static readonly string k_MenuInfoLabelUssClassName = k_DropdownUssClassName + "__info-label";
        static readonly string k_MenuHelpButtonUssClassName = k_DropdownUssClassName + "__info-help-button";
        
        static readonly string k_BuiltInHelpIconName = "_Help";
        static readonly string k_HelpButtonTooltip =  L10n.Tr("Open Editor Tool Contexts documentation page.");
        
        static readonly string k_NoContextsAvailableInfo = L10n.Tr("No other Tool Contexts available");
        static readonly string k_NoContextsAvailableTooltip = L10n.Tr("No scripts were found in the project that implement a custom Editor Tool Context.");
        
        static readonly string k_NotMatchingContextsInfo = L10n.Tr("Tool Contexts not matching selection");
        static readonly string k_NotMatchingContextsTooltip = L10n.Tr("Custom Editor Tool Contexts targeting types incompatible with the current selection cannot be activated.");

        public ToolContextButton()
        {
            RefreshActiveContext();
            ToolManager.activeContextChanged += RefreshActiveContext;
            EditorToolManager.availableToolsChanged += RefreshActiveContext;
            clicked += ShowContextMenu;
        }

        ~ToolContextButton()
        {
            EditorToolManager.availableToolsChanged -= RefreshActiveContext;
            ToolManager.activeContextChanged -= RefreshActiveContext;
            clicked -= ShowContextMenu;
        }

        void ShowContextMenu()
        {
            var dropdownMenu = new GenericDropdownMenu();
            var sortedCtxsDta = EditorToolUtility.sortedContextsDataCache;
            var globalCtxs = sortedCtxsDta.globalContextAssociations;
            var availableCompCtxs = sortedCtxsDta.availableCompContextAssociations;
            var unavailableCompCtxs = sortedCtxsDta.unavailableCompContextAssociations;
         
            // Populate global contexts section
            var totalCtxCount = globalCtxs.Count + availableCompCtxs.Count + unavailableCompCtxs.Count;
            for (int i = 0; i < globalCtxs.Count; ++i)
            {   // Separate GO context from rest of globals if there's more than one
                if (i == 1)
                    dropdownMenu.AddSeparator(String.Empty);
                
                AddItem(dropdownMenu, enabled: true, globalCtxs[i].editor);
                
                // Show helper info if no other contexts available
                if (totalCtxCount == 1)
                    AddInfoForMenuItem(k_NoContextsAvailableInfo, k_NoContextsAvailableTooltip, dropdownMenu, k_ToolContextsScriptingPage, k_HelpButtonTooltip);
            }

            // Populate available component contexts section
            for (int i = 0; i < availableCompCtxs.Count; ++i)
            {
                var ctx = availableCompCtxs[i];
                if (i == 0)
                    dropdownMenu.AddSeparator(String.Empty);
                
                AddItem(dropdownMenu, enabled: true, ctx.editor);
            }
            
            // Populate unavailable component contexts section
            for (int i = 0; i < unavailableCompCtxs.Count; ++i)
            {
                var ctx = unavailableCompCtxs[i];
                if (i == 0)
                {
                    dropdownMenu.AddSeparator(String.Empty);
                    AddInfoForMenuItem(k_NotMatchingContextsInfo, k_NotMatchingContextsTooltip, dropdownMenu, k_ToolContextsScriptingPage, k_HelpButtonTooltip);
                }

                AddItem(dropdownMenu, enabled: false, ctx.editor);
            }

            EditorToolbarUtility.LoadStyleSheets("EditorToolbar", dropdownMenu.outerContainer);
            OverlayUtilities.AddStyleSheets(dropdownMenu.outerContainer);
            dropdownMenu.outerContainer.AddToClassList(k_DropdownUssClassName);

            var bounds = worldBound;
            dropdownMenu.DropDown(bounds, this, DropdownMenuSizeMode.Content);
        }

        void AddItem(GenericDropdownMenu menu, bool enabled, Type contextType)
        {
            var on = ToolManager.activeContextType == contextType;
            if (enabled)
                menu.AddItem(EditorToolUtility.GetToolName(contextType), on, () =>
                {
                    if (ToolManager.activeContextType == contextType)
                        ToolManager.ExitToolContext();
                    else
                        ToolManager.SetActiveContext(contextType);
                });
            else
                menu.AddDisabledItem(EditorToolUtility.GetToolName(contextType), on);

            var menuItem = menu.items[^1];
            var itemIconVE = new VisualElement();
            itemIconVE.AddToClassList(k_DropdownItemIConUssClassName);
            itemIconVE.pickingMode = PickingMode.Ignore;
            var ctxIconTex = EditorToolUtility.GetContextIcon(contextType, out _).image as Texture2D;
            itemIconVE.style.backgroundImage = new StyleBackground(ctxIconTex);
 
            var itemVE = menuItem.element;
            var itemLabelVE = itemVE.Q<VisualElement>(className: GenericDropdownMenu.labelUssClassName);
            var itemContent = itemVE.Q<VisualElement>(className: GenericDropdownMenu.itemContentUssClassName);

            itemContent.Insert(itemContent.IndexOf(itemLabelVE), itemIconVE);
        }

        void AddInfoForMenuItem(string infoText, string infoTooltip, GenericDropdownMenu menu, string helpLink, string helpButtonTooltip)
        {
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList(k_MenuInfoContainerUssClassName);
 
            var infoLabel = new Label(infoText);
            infoLabel.AddToClassList(k_MenuInfoLabelUssClassName);
            infoLabel.SetEnabled(false);
            infoLabel.tooltip = infoTooltip;
            infoContainer.Add(infoLabel);

            if (!string.IsNullOrEmpty(helpLink))
            {
                var helpButton = new Button();
                var helpIcon = EditorGUIUtility.IconContent(k_BuiltInHelpIconName).image as Texture2D;
                helpButton.style.backgroundImage = new StyleBackground(helpIcon);
                helpButton.AddToClassList(k_MenuHelpButtonUssClassName);
                helpButton.tooltip = helpButtonTooltip;
                helpButton.clicked += () => { Help.ShowHelpPage(helpLink); };
                infoContainer.Add(helpButton);
            }

            menu.scrollView.Add(infoContainer);
        }

        void RefreshActiveContext()
        {
            Type activeContextType = ToolManager.activeContextType != null ? ToolManager.activeContextType : typeof(GameObjectToolContext);
            
            icon = EditorToolUtility.GetContextIcon(activeContextType, out _).image as Texture2D;
            text = EditorToolUtility.GetToolName(activeContextType);
            tooltip = $"Active Context: {EditorToolUtility.GetContextName(activeContextType, forTooltip:true)}\n\n{k_Tooltip}";
        }
    }
}
