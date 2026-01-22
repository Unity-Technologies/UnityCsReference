// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class StageContextMenuUtility
{
    static readonly string k_EditFolderName = L10n.Tr("Edit");
    static readonly string k_Cut = L10n.Tr("Cut");
    static readonly string k_Copy = L10n.Tr("Copy");
    static readonly string k_Paste = L10n.Tr("Paste");
    static readonly string k_Rename = L10n.Tr("Rename");
    static readonly string k_Duplicate = L10n.Tr("Duplicate");
    static readonly string k_Delete = L10n.Tr("Delete");
    static readonly string k_SelectAll = L10n.Tr("Select All");
    static readonly string k_DeselectAll = L10n.Tr("Deselect All");
    static readonly string k_InvertSelection = L10n.Tr("Invert Selection");
    static readonly string k_SelectChildren = L10n.Tr("Select Children");

    public static void PopulateMenu(DropdownMenu menu)
    {
        PopulateEditOperations(menu);
        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    static void PopulateEditOperations(DropdownMenu menu)
    {
        const bool disabled = false;

        AppendAction(menu, k_Cut, Menu.GetHotkey($"{k_EditFolderName}/{k_Cut}"), () => {}, disabled);
        AppendAction(menu, k_Copy, Menu.GetHotkey($"{k_EditFolderName}/{k_Copy}"), () => {}, disabled);
        AppendAction(menu, k_Paste, Menu.GetHotkey($"{k_EditFolderName}/{k_Paste}"), () => {}, disabled);

        AppendAction(menu, k_Rename, () => {}, disabled);
        AppendAction(menu, k_Duplicate, Menu.GetHotkey($"{k_EditFolderName}/{k_Duplicate}"), () => {}, disabled);
        AppendAction(menu, k_Delete, Menu.GetHotkey($"{k_EditFolderName}/{k_Delete}"), () => {}, disabled);

        menu.AppendSeparator();

        AppendAction(menu, k_SelectAll, Menu.GetHotkey($"{k_EditFolderName}/{k_SelectAll}"), () => {}, disabled);
        AppendAction(menu, k_DeselectAll, Menu.GetHotkey($"{k_EditFolderName}/{k_DeselectAll}"), () => {}, disabled);
        AppendAction(menu, k_InvertSelection, Menu.GetHotkey($"{k_EditFolderName}/{k_InvertSelection}"), () => {}, disabled);
        AppendAction(menu, k_SelectChildren, Menu.GetHotkey($"{k_EditFolderName}/{k_SelectChildren}"), () => {}, disabled);
    }

    static void PopulateElementOperations(DropdownMenu menu)
    {
        var itemPath = $"{MenuItemGenerator.k_MenuPrefix}/{ControlTypeInfo.k_ContextMenuPrefix}/Standard Elements";
        var menuItems = Menu.GetMenuItems(itemPath, includeSeparators: false, localized: true);
        var previousPriority = MenuItemGenerator.k_DefaultPriority;

        foreach (var menuItem in menuItems)
        {
            // Trim out the whole prefix string and append a new one. The +1 for the substring is for the end "/".
            var contextMenuPath = $"{ControlTypeInfo.k_ContextMenuPrefix}/{menuItem.path.Substring(itemPath.Length + 1)}";
            var hotkey = Menu.GetHotkey(menuItem.path);
            if (!string.IsNullOrEmpty(hotkey))
                contextMenuPath += " " + hotkey;

            // Append a separator based on the menu item's separator
            if (menuItem.priority - previousPriority >= 10)
                menu.AppendSeparator(ControlTypeInfo.k_ContextMenuPrefix + "/");

            previousPriority = menuItem.priority;

            menu.AppendAction(
                contextMenuPath,
                _ => EditorApplication.ExecuteMenuItem(menuItem.path),
                _ => DropdownMenuAction.Status.Normal
            );
        }
    }

    static void AppendAction(DropdownMenu menu, string name, Action action, bool enabled = true)
    {
        AppendAction(menu, name, hotkey: null, action, enabled);
    }

    static void AppendAction(DropdownMenu menu, string name, string hotkey, Action action, bool enabled = true)
    {
        var status = enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        if (string.IsNullOrEmpty(hotkey))
            menu.AppendAction(name, _ => action?.Invoke(), status);
        else
            menu.AppendAction($"{name} {hotkey}", _ => action?.Invoke(), status);
    }
}
