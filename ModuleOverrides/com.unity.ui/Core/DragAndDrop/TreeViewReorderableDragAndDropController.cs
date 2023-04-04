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
            public int[] expandedIdsBeforeDrag;
            public int[] draggedIds;
            public int lastItemId = -1;
            public float expandItemBeginTimerMs;
            public Vector2 expandItemBeginPosition;
        }

        const long k_ExpandUpdateIntervalMs = 10;
        const float k_DropExpandTimeoutMs = 700f;
        const float k_DropDeltaPosition = 100f;
        const float k_HalfDropBetweenHeight = 4f; // Value taken from TreeViewGUI -> k_HalfDropBetweenHeight.

        protected DropData m_DropData = new ();
        protected readonly BaseTreeView m_TreeView;

        public TreeViewReorderableDragAndDropController(BaseTreeView view)
            : base(view)
        {
            m_TreeView = view;
            m_ExpandDropItemCallback = ExpandDropItem;
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

            return args.dragAndDropData.source == m_TreeView ? DragVisualMode.Move : DragVisualMode.Rejected;
        }

        public override void OnDrop(IListDragAndDropArgs args)
        {
            var insertAtParentId = args.parentId;
            var insertAtChildIndex = args.childIndex;
            var insertIndexShift = 0;

            const int undef = ReusableCollectionItem.UndefinedIndex;
            var insertLast = args.dragAndDropPosition == DragAndDropPosition.OverItem || (insertAtParentId == undef && insertAtChildIndex == undef);
            using var _ = ListPool<(int parentId, int childIndex)>.Get(out var previousStates);

            foreach (var id in m_DropData.draggedIds)
            {
                var parentId = m_TreeView.viewController.GetParentId(id);
                var childIndex = m_TreeView.viewController.GetChildIndexForId(id);
                previousStates.Add((parentId, childIndex));

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

            m_ExpandDropItemScheduledItem?.Pause();
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
                if (m_DropData.expandedIdsBeforeDrag != null)
                {
                    RestoreExpanded(new List<int>(m_DropData.expandedIdsBeforeDrag));
                }

                m_DropData = new DropData();
            }

            m_ExpandDropItemScheduledItem?.Pause();
        }

        void RestoreExpanded(List<int> ids)
        {
            // We assume that we can only have expanded items during dragging
            foreach (var itemId in m_TreeView.viewController.GetAllItemIds())
            {
                if (!ids.Contains(itemId))
                {
                    m_TreeView.CollapseItem(itemId);
                }
            }
        }

        public override void HandleAutoExpand(ReusableCollectionItem item, Vector2 pointerPosition)
        {
            var itemId = item.id;
            var targetItemRect = item.bindableElement.worldBound; // Use bindableElement to directly get the indented rect.

            // Handle auto expansion
            var indentedContentRect = new Rect(targetItemRect.x, targetItemRect.y + k_HalfDropBetweenHeight, targetItemRect.width, targetItemRect.height - k_HalfDropBetweenHeight * 2);
            var hoveringOverIndentedContent = indentedContentRect.Contains(pointerPosition);
            var deltaPosition = m_DropData.expandItemBeginPosition - pointerPosition;

            if (itemId != m_DropData.lastItemId || !hoveringOverIndentedContent || deltaPosition.sqrMagnitude >= k_DropDeltaPosition)
            {
                m_DropData.lastItemId = itemId;
                m_DropData.expandItemBeginTimerMs = Panel.TimeSinceStartupMs();
                m_DropData.expandItemBeginPosition = pointerPosition;
                DelayExpandDropItem();
            }
        }

        IVisualElementScheduledItem m_ExpandDropItemScheduledItem;
        Action m_ExpandDropItemCallback;

        void DelayExpandDropItem()
        {
            if (m_ExpandDropItemScheduledItem == null)
            {
                m_ExpandDropItemScheduledItem = m_TreeView.schedule.Execute(m_ExpandDropItemCallback).Every(k_ExpandUpdateIntervalMs);
            }
            else
            {
                m_ExpandDropItemScheduledItem.Pause();
                m_ExpandDropItemScheduledItem.Resume();
            }
        }

        void ExpandDropItem()
        {
            var expandTimerExpired = Panel.TimeSinceStartupMs() - m_DropData.expandItemBeginTimerMs > k_DropExpandTimeoutMs;
            var mayExpand = expandTimerExpired;
            var itemId = m_DropData.lastItemId;

            // Auto open folders we are about to drag into
            if (m_TreeView.viewController.Exists(itemId) && mayExpand)
            {
                var hasChildren = m_TreeView.viewController.HasChildren(itemId);
                var isExpanded = m_TreeView.IsExpanded(itemId);
    
                if (!hasChildren || isExpanded)
                    return;

                // Store the expanded array prior to drag so we can revert it with a delay later
                m_DropData.expandedIdsBeforeDrag ??= m_TreeView.expandedItemIds.ToArray();
                m_DropData.expandItemBeginTimerMs = Panel.TimeSinceStartupMs();
                m_DropData.lastItemId = 0;

                m_TreeView.ExpandItem(itemId);
            }
        }
    }
}
