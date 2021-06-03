// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    class ToolContextButton : EditorToolbarDropdown
    {
        public ToolContextButton()
        {
            RefreshActiveContextIcon();
            ToolManager.activeContextChanged += RefreshActiveContextIcon;
            clicked += ShowContextMenu;
        }

        ~ToolContextButton()
        {
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

        void RefreshActiveContextIcon()
        {
            var content = EditorToolUtility.GetIcon(ToolManager.activeContextType, true);
            icon = content.image as Texture2D;
            tooltip = content.tooltip;
        }
    }
}
