// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    class ToolContextButton : EditorToolbarDropdownToggle
    {
        const string k_Tooltip = "The tool context determines what the Move, Rotate, Scale, Rect, and Transform tools" +
            " select and modify. GameObject is the default, and allows you to work with " +
            "GameObjects. Additional contexts allow you to edit different objects.";

        public ToolContextButton()
        {
            RefreshActiveContext();
            ToolManager.activeContextChanged += RefreshActiveContext;
            EditorToolManager.availableComponentToolsChanged += RefreshActiveContext;
            dropdownClicked += ShowContextMenu;

            this.RegisterValueChangedCallback(OnValueChanged);
        }

        ~ToolContextButton()
        {
            EditorToolManager.availableComponentToolsChanged -= RefreshActiveContext;
            ToolManager.activeContextChanged -= RefreshActiveContext;
            dropdownClicked -= ShowContextMenu;
        }

        void ShowContextMenu()
        {
            var menu = new GenericMenu();

            foreach (var ctx in EditorToolUtility.availableGlobalToolContexts)
            {
                if (ctx.editor != typeof(GameObjectToolContext))
                    menu.AddItem(
                        new GUIContent(EditorToolUtility.GetToolName(ctx.editor)),
                        ToolManager.activeContextType == ctx.editor,
                        () =>
                        {
                            if(ToolManager.activeContextType == ctx.editor)
                                ToolManager.ExitToolContext();
                            else
                                ToolManager.SetActiveContext(ctx.editor);
                        });
            }

            foreach (var ctx in EditorToolManager.componentContexts)
            {
                menu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolName(ctx.editorType)),
                    EditorToolManager.activeToolContext == ctx.editor,
                    () =>
                    {
                        if(EditorToolManager.activeToolContext == ctx.editor)
                            ToolManager.ExitToolContext();
                        else
                            ToolManager.SetActiveContext(ctx.editorType);
                    });
            }

            menu.DropDown(worldBound);
        }

        static int availableContextCount
        {
            get
            {
                return EditorToolUtility.availableGlobalToolContexts.Count() +
                    EditorToolManager.availableComponentContextCount;
            }
        }

        void OnValueChanged(ChangeEvent<bool> evt)
        {
            var isGOToolContext = ToolManager.activeContextType == typeof(GameObjectToolContext);
            if (evt.newValue && isGOToolContext)
                ToolManager.CycleToolContexts();
            else if (!evt.newValue && !isGOToolContext)
                ToolManager.ExitToolContext();
        }

        void RefreshActiveContext()
        {
            var isGOToolContext = ToolManager.activeContextType == typeof(GameObjectToolContext);
            if (value && isGOToolContext)
                value = false;
            else if(!value && !isGOToolContext)
                value = true;

            // Enable button only if at least one other context beside GameObjectToolContext is available
            SetEnabled(availableContextCount > 1);
            // Enable toggle only if at least 2 other contexts are available in addition to GameObjectToolContext
            ShowDropDown(availableContextCount > 2);

            var activeContextType = typeof(GameObjectToolContext);

            if (availableContextCount > 1)
            {
                if (isGOToolContext)
                {
                    var lastContextType = ToolManager.GetLastContextType();
                    // JIRA: UUM-16237. Use the content of the last context only if the current selection is associated with the same type of context.
                    if (ToolManager.allContextsExceptGameObject.Contains(lastContextType))
                        activeContextType = lastContextType;
                    else
                        activeContextType = ToolManager.allContextsExceptGameObject.FirstOrDefault();
                }
                else
                    activeContextType = ToolManager.activeContextType;
            }

            var content = EditorToolUtility.GetIcon(activeContextType, true);
            icon = content.image as Texture2D;
            var activeContextName = EditorToolUtility.GetToolName(ToolManager.activeContextType)
                                    + (isGOToolContext ? " (Default)" : "");
            tooltip = $"{k_Tooltip}\n\nActive Context: {activeContextName}";
        }
    }
}
