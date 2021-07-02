// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    class ToolContextButton : EditorToolbarDropdown
    {
        const string k_Tooltip = "The tool context determines what the Move, Rotate, Scale, Rect, and Transform tools" +
            " select and modify. GameObject is the default, and allows you to work with " +
            "GameObjects. Additional contexts allow you to edit different objects.";

        public ToolContextButton()
        {
            RefreshActiveContextIcon();
            ToolManager.activeContextChanged += RefreshActiveContextIcon;
            EditorToolManager.availableComponentToolsChanged += RefreshActiveContextIcon;
            clicked += ShowContextMenu;
        }

        ~ToolContextButton()
        {
            EditorToolManager.availableComponentToolsChanged -= RefreshActiveContextIcon;
            ToolManager.activeContextChanged -= RefreshActiveContextIcon;
            clicked -= ShowContextMenu;
        }

        void ShowContextMenu()
        {
            var menu = new GenericMenu();

            foreach (var ctx in EditorToolUtility.availableGlobalToolContexts)
            {
                menu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolName(ctx.editor)),
                    ToolManager.activeContextType == ctx.editor,
                    () => { ToolManager.SetActiveContext(ctx.editor); });
            }

            foreach (var ctx in EditorToolManager.componentContexts)
            {
                menu.AddItem(
                    new GUIContent(EditorToolUtility.GetToolName(ctx.editorType)),
                    EditorToolManager.activeToolContext == ctx.editor,
                    () => { ToolManager.SetActiveContext(ctx.editorType); });
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

        void RefreshActiveContextIcon()
        {
            SetEnabled(availableContextCount > 1);
            var content = EditorToolUtility.GetIcon(ToolManager.activeContextType, true);
            icon = content.image as Texture2D;
            tooltip = $"{k_Tooltip}\n\nActive Context: {EditorToolUtility.GetToolName(ToolManager.activeContextType)}";
        }
    }
}
