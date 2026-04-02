// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Hierarchy;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal static class StyleSheetContextMenuUtility
{
    internal static readonly string k_EditFolderName = L10n.Tr("Edit");
    internal static readonly string k_Copy = L10n.Tr("Copy");
    internal static readonly string k_Paste = L10n.Tr("Paste");
    internal static readonly string k_Rename = L10n.Tr("Rename");
    internal static readonly string k_Duplicate = L10n.Tr("Duplicate");
    internal static readonly string k_Delete = L10n.Tr("Delete");
    internal static readonly string k_CreateNewUss = L10n.Tr("Create new USS");
    internal static readonly string k_AddExistingUss = L10n.Tr("Add Existing USS");
    internal static readonly string k_RemoveUss = L10n.Tr("Remove USS");
    internal static readonly string k_SetActiveUss = L10n.Tr("Set as Active USS");

    public static void PopulateMenu(HierarchyView view, in HierarchyNode node, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        if (handler is not StyleSheetEditingNodeTypeHandler styleSheetHandler)
            return;

        var isStyleSheet = styleSheetHandler.IsStyleSheet(node);

        var copyMenu = k_EditFolderName + "/" + k_Copy;
        AppendAction(menu, k_Copy, Menu.GetHotkey(copyMenu), view.OnCopy, handler.CanCopy(view));
        var pasteMenu = k_EditFolderName + "/" + k_Paste;
        AppendAction(menu, k_Paste, Menu.GetHotkey(pasteMenu), view.OnPaste, handler.CanPaste(view));

        menu.AppendSeparator();

        var renameMenu = k_EditFolderName + "/" + k_Rename;
        var n = node;
        AppendAction(menu, k_Rename, Menu.GetHotkey(renameMenu), () => view.OnSetName(n), handler.CanSetName(view, node));
        var duplicateMenu = k_EditFolderName + "/" + k_Duplicate;
        AppendAction(menu, k_Duplicate, Menu.GetHotkey(duplicateMenu), view.OnDuplicate, handler.CanDuplicate(view));
        var deleteMenu = k_EditFolderName + "/" + k_Delete;
        AppendAction(menu, k_Delete, Menu.GetHotkey(deleteMenu), view.OnDelete, handler.CanDelete(view));

        menu.AppendSeparator();

        var canCreateNewUss = Menu.GetHotkey(k_CreateNewUss);
        AppendAction(menu, k_CreateNewUss, Menu.GetHotkey(canCreateNewUss), styleSheetHandler.Window.CreateStyleSheet);
        var canAddExistingUss = Menu.GetHotkey(k_AddExistingUss);
        AppendAction(menu, k_AddExistingUss, Menu.GetHotkey(canAddExistingUss), styleSheetHandler.Window.AddStyleSheet);
        var canRemoveUss = Menu.GetHotkey(k_RemoveUss);
        AppendAction(menu, k_RemoveUss, Menu.GetHotkey(canRemoveUss), () => styleSheetHandler.Window.RemoveStyleSheet(n), isStyleSheet);

        menu.AppendSeparator();
        var canSetActiveUss = Menu.GetHotkey(k_SetActiveUss);
        AppendAction(menu, k_SetActiveUss, Menu.GetHotkey(canSetActiveUss),  () => styleSheetHandler.Window.SetActiveStyleSheet(n), isStyleSheet);
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
