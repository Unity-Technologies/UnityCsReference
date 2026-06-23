// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    [VisibleToOtherModules]
    internal interface IHierarchyEditorNodeTypeHandler
    {
        bool CanCut(HierarchyView view);
        bool OnCut(HierarchyView view);
        bool CanCopy(HierarchyView view);
        bool OnCopy(HierarchyView view);
        bool CanPaste(HierarchyView view);
        bool OnPaste(HierarchyView view);
        bool CanPasteAsChild(HierarchyView view);
        bool OnPasteAsChild(HierarchyView view, bool keepWorldPos);
        bool CanSetName(HierarchyView view, in HierarchyNode node);
        bool OnSetName(HierarchyView view, in HierarchyNode node, string name);
        string GetDisplayName(HierarchyView view, in HierarchyNode node);
        bool CanDuplicate(HierarchyView view);
        bool OnDuplicate(HierarchyView view);
        bool CanDelete(HierarchyView view);
        bool OnDelete(HierarchyView view);
        bool CanFindReferences(HierarchyView view);
        bool OnFindReferences(HierarchyView view);
        bool CanDoubleClick(HierarchyView view, in HierarchyNode node);
        bool OnDoubleClick(HierarchyView view, in HierarchyNode node);
        void GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip);
        void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu);
        bool AcceptParent(HierarchyView view, in HierarchyNode parent);
        bool AcceptChild(HierarchyView view, in HierarchyNode child);

        /// <summary>
        /// Determines whether the dragged nodes of this handler's type can begin a drag operation.
        /// Only called when at least one node of this handler's type is in the dragged set; the
        /// <paramref name="nodes"/> span contains only this handler's nodes.
        /// Return <see langword="false"/> to veto the drag.
        /// </summary>
        bool CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes);

        /// <summary>
        /// Populates the drag and drop data with this handler's contribution (entity ids, paths, generic data).
        /// Only called when at least one node of this handler's type is in the dragged nodes.
        /// <see cref="HierarchyViewDragAndDropSetupData.Nodes"/> contains only this handler's nodes.
        /// </summary>
        void OnStartDrag(in HierarchyViewDragAndDropSetupData data);

        string GetDragTitle(HierarchyView view, in HierarchyNode node) => null;

        /// <summary>
        /// Determines whether this handler allows the selected nodes to be reordered/reparented within the hierarchy.
        /// Called before <see cref="HierarchyViewModel.SetParentOfSelection"/> moves any nodes.
        /// Only called when at least one node of this handler's type is in the dragged nodes.
        /// May be called multiple times per drag and drop operation; it should be free of side effects.
        /// Return <see cref="DragVisualMode.None"/> to indicate this handler has no opinion (allow by default).
        /// Return <see cref="DragVisualMode.Rejected"/> to block the operation.
        /// </summary>
        DragVisualMode CanReorder(in HierarchyViewDragAndDropHandlingData data);

        /// <summary>
        /// Applies the reorder/reparent operation to the underlying objects (e.g. Transform, Scene) after
        /// <see cref="HierarchyViewModel.SetParentOfSelection"/> has already moved the hierarchy nodes.
        /// Only called when at least one node of this handler's type is in the dragged nodes and no handler rejected the reorder.
        /// </summary>
        void OnReorder(in HierarchyViewDragAndDropHandlingData data);

        /// <summary>
        /// Determines whether this handler can accept an external drop (e.g. assets dragged from the Project window).
        /// Called on every registered handler so each can claim its own slice of the drag and drop data.
        /// Return <see cref="DragVisualMode.None"/> to abstain. If every handler returns this, the drop is rejected.
        /// Return <see cref="DragVisualMode.Rejected"/> to block the operation.
        /// </summary>
        DragVisualMode CanAcceptDrop(in HierarchyViewDragAndDropHandlingData data);

        /// <summary>
        /// Handles an external drop (e.g. assets dragged from the Project window). The handler owns the full
        /// operation including instantiation, positioning, and undo registration.
        /// Called on every registered handler so each can claim its own slice of drag and drop data.
        /// Return <see cref="DragVisualMode.None"/> to abstain. If every handler returns this, the drop is rejected.
        /// Return <see cref="DragVisualMode.Rejected"/> to block the operation.
        /// </summary>
        DragVisualMode OnAcceptDrop(in HierarchyViewDragAndDropHandlingData data);
    }
}
