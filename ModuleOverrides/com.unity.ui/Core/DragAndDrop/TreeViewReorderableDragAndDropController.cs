// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal class TreeViewReorderableDragAndDropController : BaseReorderableDragAndDropController
    {
        protected class DropData
        {
            public int[] draggedIds;
        }

        protected DropData m_DropData = new ();
        protected readonly Experimental.TreeView m_TreeView;

        public TreeViewReorderableDragAndDropController(Experimental.TreeView view)
            : base(view)
        {
            m_TreeView = view;
            enableReordering = true;
        }

        protected override int CompareId(int id1, int id2)
        {
            if (id1 == id2)
                return id1.CompareTo(id2);

            // Find common parent and compare child indices to know which on comes first.
            var parentId1 = id1;
            var parentId2 = id2;

            // Get parents of id1.
            using var pool1 = ListPool<int>.Get(out var parentList1);
            while (parentId1 != TreeItem.invalidId)
            {
                parentList1.Add(parentId1);
                parentId1 = m_TreeView.viewController.GetParentId(parentId1);
            }

            // Get parents of id2.
            using var pool2 = ListPool<int>.Get(out var parentList2);
            while (parentId2 != TreeItem.invalidId)
            {
                parentList2.Add(parentId2);
                parentId2 = m_TreeView.viewController.GetParentId(parentId2);
            }

            // Add the root.
            parentList1.Add(TreeItem.invalidId);
            parentList2.Add(TreeItem.invalidId);

            // Look for the first common parent.
            for (var i = 0; i < parentList1.Count; i++)
            {
                var parentId = parentList1[i];
                var index2 = parentList2.IndexOf(parentId);
                if (index2 >= 0)
                {
                    // id1 is the parent of id2. Leave early with the result -1, because id1 < id2.
                    if (i == 0)
                        return -1;

                    var previousId1 = i > 0 ? parentList1[i - 1] : id1;
                    var previousId2 = index2 > 0 ? parentList2[index2 - 1] : id2;
                    var childIndex1 = m_TreeView.viewController.GetChildIndexForId(previousId1);
                    var childIndex2 = m_TreeView.viewController.GetChildIndexForId(previousId2);
                    return childIndex1.CompareTo(childIndex2);
                }
            }

            throw new ArgumentOutOfRangeException($"[UI Toolkit] Trying to reorder ids that are not in the same tree.");
        }

        public override StartDragArgs SetupDragAndDrop(IEnumerable<int> itemIds, bool skipText = false)
        {
            var startDragArgs = base.SetupDragAndDrop(itemIds, skipText);
            m_DropData.draggedIds = GetSortedSelectedIds().ToArray();
            return startDragArgs;
        }

        public override DragVisualMode HandleDragAndDrop(IListDragAndDropArgs args)
        {
            if (!enableReordering)
                return DragVisualMode.Rejected;

            return args.dragAndDropData.userData == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
        }

        struct TreeItemState
        {
            public int parentId;
            public int childIndex;

            public TreeItemState(int parentId, int childIndex)
            {
                this.parentId = parentId;
                this.childIndex = childIndex;
            }
        }

        public override void OnDrop(IListDragAndDropArgs args)
        {
            var insertAtParentId = args.parentId;
            var insertAtChildIndex = args.childIndex;
            var insertIndexShift = 0;

            const int undef = ReusableCollectionItem.UndefinedIndex;
            var insertLast = args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtParentId == undef && insertAtChildIndex == undef);
            using var _ = ListPool<TreeItemState>.Get(out var previousStates);

            foreach (var id in m_DropData.draggedIds)
            {
                var parentId = m_TreeView.viewController.GetParentId(id);
                var childIndex = m_TreeView.viewController.GetChildIndexForId(id);
                previousStates.Add(new TreeItemState(parentId, childIndex));

                if (insertLast)
                {
                    m_TreeView.viewController.Move(id, insertAtParentId, -1, false);
                    continue;
                }

                var newChildIndex = insertAtChildIndex + insertIndexShift;

                if (parentId != insertAtParentId || childIndex >= insertAtChildIndex)
                {
                    insertIndexShift++;
                }

                m_TreeView.viewController.Move(id, insertAtParentId, newChildIndex, false);
            }

            if (args.dragAndDropPosition == DragAndDropPosition.OverItem)
            {
                // Expand parent after insertion.
                m_TreeView.viewController.ExpandItem(insertAtParentId, false, false);
            }

            m_TreeView.viewController.RebuildTree();
            m_TreeView.RefreshItems();

            for (var i = 0; i < m_DropData.draggedIds.Length; i++)
            {
                var id = m_DropData.draggedIds[i];
                var previous = previousStates[i];
                var newParentId = m_TreeView.viewController.GetParentId(id);
                var newChildIndex = m_TreeView.viewController.GetChildIndexForId(id);
                if (previous.parentId == newParentId && previous.childIndex == newChildIndex)
                    continue;

                m_TreeView.viewController.RaiseItemParentChanged(id, insertAtParentId);
            }
        }

        public override void DragCleanup()
        {
            if (m_DropData != null)
            {
                m_DropData = new DropData();
            }
        }
    }
}
