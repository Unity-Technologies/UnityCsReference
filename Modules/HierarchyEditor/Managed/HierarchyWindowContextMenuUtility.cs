// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Context menu handler for hierarchy node's common menu items.
    /// </summary>
    internal static class HierarchyWindowContextMenuUtility
    {
        static readonly string k_EditFolderName = L10n.Tr("Edit");
        static readonly string k_Cut = L10n.Tr("Cut");
        static readonly string k_Copy = L10n.Tr("Copy");
        static readonly string k_Paste = L10n.Tr("Paste");
        static readonly string k_PasteSpecialFolderName = L10n.Tr("Paste Special");
        static readonly string k_PasteAsChildKeepLocalTransform = L10n.Tr("Paste as Child (Keep Local Transform)");
        static readonly string k_PasteAsChildKeepWorldTransform = L10n.Tr("Paste as Child (Keep World Transform)");
        static readonly string k_Rename = L10n.Tr("Rename");
        static readonly string k_Duplicate = L10n.Tr("Duplicate");
        static readonly string k_Delete = L10n.Tr("Delete");
        static readonly string k_SelectAll = L10n.Tr("Select All");
        static readonly string k_DeselectAll = L10n.Tr("Deselect All");
        static readonly string k_InvertSelection = L10n.Tr("Invert Selection");
        static readonly string k_SelectChildren = L10n.Tr("Select Children");
        static readonly string k_FindReferenceInScene = L10n.Tr("Find References In Scene");

        /// <summary>
        /// Populate common context menu items for the given node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <param name="handler">The <see cref="HierarchyNodeTypeHandler"/>.</param>
        /// <param name="menu">The <see cref="DropdownMenu"/> to populate with.</param>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        public static void PopulateCommonContextMenuItems(HierarchyView view, HierarchyNode node, HierarchyNodeTypeHandler handler, DropdownMenu menu)
        {
            var selectionCount = view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            bool hasSelection = selectionCount > 0;

            if (handler is IHierarchyEditorNodeTypeHandler editorHandler)
            {
                AppendAction(menu, k_Cut, Menu.GetHotkey($"{k_EditFolderName}/{k_Cut}"), () => editorHandler.OnCut(view), hasSelection && editorHandler.CanCut(view));
                AppendAction(menu, k_Copy, Menu.GetHotkey($"{k_EditFolderName}/{k_Copy}"), () => editorHandler.OnCopy(view), hasSelection && editorHandler.CanCopy(view));
                AppendAction(menu, k_Paste, Menu.GetHotkey($"{k_EditFolderName}/{k_Paste}"), () => editorHandler.OnPaste(view), editorHandler.CanPaste(view));

                AppendAction(menu, $"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepLocalTransform}", Menu.GetHotkey($"{k_EditFolderName}/{k_PasteSpecialFolderName}/{k_PasteAsChildKeepLocalTransform}"), () => editorHandler.OnPasteAsChild(view, false), editorHandler.CanPasteAsChild(view));
                AppendAction(menu, $"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepWorldTransform}", () => editorHandler.OnPasteAsChild(view, true), hasSelection && editorHandler.CanPasteAsChild(view));

                AppendAction(menu, k_Rename, () => view.OnSetName(node), selectionCount == 1 && editorHandler.CanSetName(view, in node));
                AppendAction(menu, k_Duplicate, Menu.GetHotkey($"{k_EditFolderName}/{k_Duplicate}"), () => editorHandler.OnDuplicate(view), hasSelection && editorHandler.CanDuplicate(view));
                AppendAction(menu, k_Delete, Menu.GetHotkey($"{k_EditFolderName}/{k_Delete}"), () => editorHandler.OnDelete(view), hasSelection && editorHandler.CanDelete(view));

                menu.AppendSeparator();

                AppendAction(menu, k_SelectAll, Menu.GetHotkey($"{k_EditFolderName}/{k_SelectAll}"), () => view.SelectAll(exposedOnly: true));
                AppendAction(menu, k_DeselectAll, Menu.GetHotkey($"{k_EditFolderName}/{k_DeselectAll}"), view.DeselectAll, hasSelection);
                AppendAction(menu, k_InvertSelection, Menu.GetHotkey($"{k_EditFolderName}/{k_InvertSelection}"), view.ToggleSelection, hasSelection);
                AppendAction(menu, k_SelectChildren, Menu.GetHotkey($"{k_EditFolderName}/{k_SelectChildren}"), () => view.SelectChildrenAndExpandRecursive(), view.DoesSelectedNodesHaveChildren());

                menu.AppendSeparator();

                AppendAction(menu, k_FindReferenceInScene, () => editorHandler.OnFindReferences(view), hasSelection && editorHandler.CanFindReferences(view));

                menu.AppendSeparator();
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
}
