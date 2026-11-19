// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    static class MenuItemExtension
    {
        public static bool RemoveMenuItemFromShortcut<T>(this DropdownMenu self, GraphTool tool) where T : ShortcutEventBase<T>, new()
        {
            var name = ShortcutEventBase<T>.GetMenuItemName(tool);
            if (name == null)
                return false;

            int index = self.MenuItems().FindIndex(t => t is DropdownMenuAction a && a.name == name);
            if (index >= 0)
            {
                self.RemoveItemAt(index);
                return true;
            }
            return false;
        }

        public static bool InsertMenuItemFromShortcut<T>(this DropdownMenu self, int atIndex, GraphTool tool, Action<DropdownMenuAction> action, DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal) where T : ShortcutEventBase<T>, new()
        {
            if (tool == null)
                return false;
            var name = ShortcutEventBase<T>.GetMenuItemName(tool);
            if (name == null)
                return false;
            self.InsertAction(atIndex, name, action, status);

            return true;
        }

        public static bool AppendMenuItemFromShortcut<T>(this DropdownMenu self, GraphTool tool, Action<DropdownMenuAction> action, DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal) where T : ShortcutEventBase<T>, new()
        {
            if (tool == null)
                return false;
            var name = ShortcutEventBase<T>.GetMenuItemName(tool);
            if (name == null)
                return false;
            self.AppendAction(name, action, status);

            return true;
        }

        public static void AppendMenuItemFromShortcutWithName<T>(this DropdownMenu self, GraphTool tool, string customName, Action<DropdownMenuAction> action, DropdownMenuAction.Status status = DropdownMenuAction.Status.Normal) where T : ShortcutEventBase<T>, new()
        {
            var shortcutString = tool != null ? ShortcutEventBase<T>.GetMenuItemShortcutString(tool) : null;

            var fullItemName = string.IsNullOrEmpty(shortcutString) ? customName : $"{customName} {shortcutString}";

            self.AppendAction(fullItemName, action, status);
        }
    }
}
