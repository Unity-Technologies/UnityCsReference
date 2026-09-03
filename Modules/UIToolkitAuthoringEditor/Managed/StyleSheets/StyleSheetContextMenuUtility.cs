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
    internal static readonly string k_EditFolderName = L10n.Tr("Edit", null);
    internal static readonly string k_Copy = L10n.Tr("Copy", null);
    internal static readonly string k_Paste = L10n.Tr("Paste", null);
    internal static readonly string k_Rename = L10n.Tr("Rename", null);
    internal static readonly string k_Duplicate = L10n.Tr("Duplicate", null);
    internal static readonly string k_Delete = L10n.Tr("Delete", null);
    internal static readonly string k_CreateNewUss = L10n.Tr("Create new USS", null);
    internal static readonly string k_AddExistingUss = L10n.Tr("Add Existing USS", null);
    internal static readonly string k_RemoveUss = L10n.Tr("Remove USS", null);
    internal static readonly string k_SetActiveUss = L10n.Tr("Set as Active USS", null);

    public static void PopulateMenu(HierarchyView view, in HierarchyNode node, DropdownMenu menu, IHierarchyEditorNodeTypeHandler handler)
    {
        if (handler is not StyleSheetEditingNodeTypeHandler styleSheetHandler)
            return;

        if (styleSheetHandler.IsGroup(node))
            return;

        var isStyleSheet = styleSheetHandler.IsStyleSheet(node);
        var isReadOnly = styleSheetHandler.IsReadOnly(node);
        // Document-level authoring needs an editable document: a UI Stage, or a scene document while Main
        // Stage authoring is enabled.
        var canEdit = styleSheetHandler.Window is { IsReadOnly: false };

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

        AppendAction(menu, k_CreateNewUss, Menu.GetHotkey(k_CreateNewUss), styleSheetHandler.Window.CreateStyleSheet, canEdit);
        AppendAction(menu, k_AddExistingUss, Menu.GetHotkey(k_AddExistingUss), styleSheetHandler.Window.AddStyleSheet, canEdit);
        AppendAction(menu, k_RemoveUss, Menu.GetHotkey(k_RemoveUss), () => styleSheetHandler.Window.RemoveStyleSheet(n), isStyleSheet && !isReadOnly);

        menu.AppendSeparator();
        AppendAction(menu, k_SetActiveUss, Menu.GetHotkey(k_SetActiveUss),  () => styleSheetHandler.Window.SetActiveStyleSheet(n), isStyleSheet && !isReadOnly);
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
