// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
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

    public static void PopulateMenu(HierarchyView view, in HierarchyNode node, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        PopulateEditOperations(view, in node, menu, handler);
        menu.AppendSeparator();
        PopulateElementOperations(menu);
    }

    static void PopulateEditOperations(HierarchyView view, in HierarchyNode node, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        var cutMenu = k_EditFolderName + "/" + k_Cut;
        AppendAction(menu, k_Cut, Menu.GetHotkey(cutMenu), view.OnCut, handler.CanCut(view));
        var copyMenu = k_EditFolderName + "/" + k_Copy;
        AppendAction(menu, k_Copy, Menu.GetHotkey(copyMenu), view.OnCopy, handler.CanCopy(view));
        var pasteMenu = k_EditFolderName + "/" + k_Paste;
        AppendAction(menu, k_Paste, Menu.GetHotkey(pasteMenu), view.OnPaste, handler.CanPaste(view));

        var renameMenu = k_EditFolderName + "/" + k_Rename;
        var n = node;
        AppendAction(menu, k_Rename, Menu.GetHotkey(renameMenu), () => { view.OnSetName(n); }, handler.CanSetName(view, node));

        var duplicateMenu = k_EditFolderName + "/" + k_Duplicate;
        AppendAction(menu, k_Duplicate, Menu.GetHotkey(duplicateMenu), view.OnDuplicate, handler.CanDuplicate(view));

        var deleteMenu = k_EditFolderName + "/" + k_Delete;
        AppendAction(menu, k_Delete, Menu.GetHotkey(deleteMenu), view.OnDelete, handler.CanDelete(view));

        menu.AppendSeparator();
        var selectAllMenu = k_EditFolderName + "/" + k_SelectAll;
        AppendAction(menu, k_SelectAll, Menu.GetHotkey(selectAllMenu), () => { view.SelectAll(exposedOnly:true); });
        var deselectAllMenu = k_EditFolderName + "/" + k_DeselectAll;
        AppendAction(menu, k_DeselectAll, Menu.GetHotkey(deselectAllMenu), view.DeselectAll);
        var invertMenu = k_EditFolderName + "/" + k_InvertSelection;
        AppendAction(menu, k_InvertSelection, Menu.GetHotkey(invertMenu), view.ToggleSelection);
        var selectChildrenMenu = k_EditFolderName + "/" + k_SelectChildren;
        AppendAction(menu, k_SelectChildren, Menu.GetHotkey(selectChildrenMenu), view.SelectChildrenAndExpandRecursive);
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
