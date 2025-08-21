// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
            bool hasSelection = view.ViewModel.HasAllFlags(HierarchyNodeFlags.Selected);

            menu.AppendAction($"{k_Cut} " + Menu.GetHotkey($"{k_EditFolderName}/{k_Cut}"), _ => view.OnCut(), hasSelection && handler.CanCut(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_Copy} " + Menu.GetHotkey($"{k_EditFolderName}/{k_Copy}"), _ => view.OnCopy(), hasSelection && handler.CanCopy(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_Paste} " + Menu.GetHotkey($"{k_EditFolderName}/{k_Paste}"), _ => view.OnPaste(), handler.CanPaste(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            if (handler is HierarchyGameObjectHandler gameObjectHandler)
            {
                menu.AppendAction($"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepLocalTransform} " + Menu.GetHotkey($"{k_EditFolderName}/{k_PasteSpecialFolderName}/{k_PasteAsChildKeepLocalTransform}"), _ => ClipboardUtility.PasteGOAsChild(), hasSelection && gameObjectHandler.CanPasteAsChild(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                menu.AppendAction($"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepWorldTransform}", _ => ClipboardUtility.PasteGOAsChild(true), hasSelection && gameObjectHandler.CanPasteAsChild(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            else
            {
                menu.AppendAction($"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepLocalTransform} " + Menu.GetHotkey($"{k_EditFolderName}/{k_PasteSpecialFolderName}/{k_PasteAsChildKeepWorldTransform}"), null, DropdownMenuAction.Status.Disabled);
                menu.AppendAction($"{k_PasteSpecialFolderName}/{k_PasteAsChildKeepWorldTransform}", null, DropdownMenuAction.Status.Disabled);
            }

            menu.AppendAction($"{k_Rename}", _ => view.OnSetName(node), view.ViewModel.HasAllFlagsCount(HierarchyNodeFlags.Selected) == 1 && handler.CanSetName(view, in node) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_Duplicate} " + Menu.GetHotkey($"{k_EditFolderName}/{k_Duplicate}"), _ => view.OnDuplicate(), hasSelection && handler.CanDuplicate(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_Delete} " + Menu.GetHotkey($"{k_EditFolderName}/{k_Delete}"), _ => handler.Delete(view), hasSelection && handler.CanDelete(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendSeparator();

            menu.AppendAction($"{k_SelectAll} " + Menu.GetHotkey($"{k_EditFolderName}/{k_SelectAll}"), _ => view.SelectAll());
            menu.AppendAction($"{k_DeselectAll} " + Menu.GetHotkey($"{k_EditFolderName}/{k_DeselectAll}"), _ => view.ClearSelection(), hasSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_InvertSelection} " + Menu.GetHotkey($"{k_EditFolderName}/{k_InvertSelection}"), _ => view.InvertSelection(), hasSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            menu.AppendAction($"{k_SelectChildren} " + Menu.GetHotkey($"{k_EditFolderName}/{k_SelectChildren}"), _ => view.SelectChildrenForSelectedNodes(), view.DoesSelectedNodesHaveChildren() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendSeparator();

            menu.AppendAction($"{k_FindReferenceInScene}", _ => handler.FindReferences(view), hasSelection && handler.CanFindReferences(view) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            menu.AppendSeparator();
        }
    }
}
