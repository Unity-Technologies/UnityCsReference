// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.IMGUI.Controls
{
    // Abstract base class for common dragging behavior
    // Usage:
    //   - Override StartDrag
    //   - Override DoDrag
    // Features:
    //   - Expands items with children on hover (after ca 0.7 seconds)
    //


    internal abstract class TreeViewDragging<TIdentifier> : ITreeViewDragging<TIdentifier> where TIdentifier : unmanaged, IEquatable<TIdentifier>
    {
        protected TreeViewController<TIdentifier> m_TreeView;

        protected class DropData
        {
            public TIdentifier[] expandedArrayBeforeDrag;
            public int lastControlID = -1;
            public int dropTargetControlID = -1;
            public int rowMarkerControlID = -1;
            public int ancestorControlID;
            public double expandItemBeginTimer;
            public Vector2 expandItemBeginPosition;
            public float insertionMarkerYPosition;
            public TreeViewItem<TIdentifier> insertRelativeToSibling;

            public void ClearPerEventState()
            {
                dropTargetControlID = -1;
                rowMarkerControlID = -1;
                ancestorControlID = -1;
                insertionMarkerYPosition = -1f;
                insertRelativeToSibling = null;
            }
        }

        public enum DropPosition
        {
            Upon = 0,
            Below = 1,
            Above = 2
        }

        protected DropData m_DropData = new DropData();
        const double k_DropExpandTimeout = 0.7;

        static class Constants
        {
            public const string GetInsertionIndexNotFound = "Did not find targetItem,; should be a child of parentItem";
        }

        public TreeViewDragging(TreeViewController<TIdentifier> treeView)
        {
            m_TreeView = treeView;
        }

        virtual public void OnInitialize()
        {
        }

        public int GetDropTargetControlID()
        {
            return m_DropData.dropTargetControlID;
        }

        public int GetRowMarkerControlID()
        {
            return m_DropData.rowMarkerControlID;
        }

        public int GetAncestorControlID()
        {
            return m_DropData.ancestorControlID;
        }

        public bool drawRowMarkerAbove { get; set; }
        public float insertionMarkerYPosition { get { return m_DropData.insertionMarkerYPosition; } }
        public TreeViewItem<TIdentifier> insertRelativeToSibling { get { return m_DropData.insertRelativeToSibling; } }

        public Func<int> getIndentLevelForMouseCursor;

        public virtual bool CanStartDrag(TreeViewItem<TIdentifier> targetItem, List<TIdentifier> draggedItemIDs, Vector2 mouseDownPosition) => CanStartDragInternal(targetItem, draggedItemIDs, mouseDownPosition);
        public virtual bool CanStartDragInternal(TreeViewItem<TIdentifier> targetItem, List<TIdentifier> draggedItemIDs, Vector2 mouseDownPosition)
        {
            return true;
        }

        // This method is called from TreeView when a drag is started
        // Client should setup the drag data
        public virtual void StartDragInternal(TreeViewItem<TIdentifier> draggedItem, List<TIdentifier> draggedItemIDs){}
        public virtual void StartDrag(TreeViewItem<TIdentifier> draggedItem, List<TIdentifier> draggedItemIDs) => StartDragInternal(draggedItem, draggedItemIDs);

        // This method is called from within DragElement when it has determined what is the parent item of the current targetItem
        // (This depends on if dropPosition is above, below or upon)
        // Implemented by client code to decide what should happen when the drag is e.g performed (e.g change the backend state of the tree view)
        // Notes on arguments:
        // When hovering outside any items: target and parent is null, dropPos is invalid
        // If parentItem and targetItem is the same then insert as first child of parent, dropPos is invalid
        // If parentItem and targetItem is different then use dropPos to insert dragged items relative to targetItem
        // parentItem can be null when root is visible and hovering above or below the root

        // if targetItem is null then parent can be null if root is visible
        // if targetitem is null then parent might be valid if root is hidden
        public virtual DragAndDropVisualMode DoDrag(TreeViewItem<TIdentifier> parentItem, TreeViewItem<TIdentifier> targetItem, bool perform, DropPosition dropPosition) => DoDragInternal(parentItem, targetItem, perform, dropPosition);
        public virtual DragAndDropVisualMode DoDragInternal(TreeViewItem<TIdentifier> parentItem, TreeViewItem<TIdentifier> targetItem, bool perform, DropPosition dropPosition) => throw new NotImplementedException();

        protected float GetDropBetweenHalfHeight(TreeViewItem<TIdentifier> item, Rect itemRect)
        {
            return m_TreeView.data.CanBeParent(item) ? m_TreeView.gui.halfDropBetweenHeight : itemRect.height * 0.5f;
        }

        void GetPreviousAndNextItemsIgnoringDraggedItems(int targetRow, DropPosition dropPosition, out TreeViewItem<TIdentifier> previousItem, out TreeViewItem<TIdentifier> nextItem)
        {
            if (dropPosition != DropPosition.Above && dropPosition != DropPosition.Below)
                throw new ArgumentException("Invalid argument: " + dropPosition);

            previousItem = nextItem = null;
            int curPrevRow = (dropPosition == DropPosition.Above) ? targetRow - 1 : targetRow;
            int curNextRow = (dropPosition == DropPosition.Above) ? targetRow : targetRow + 1;

            while (curPrevRow >= 0)
            {
                var curPreviousItem = m_TreeView.data.GetItem(curPrevRow);
                if (!m_TreeView.IsDraggingItem(curPreviousItem))
                {
                    previousItem = curPreviousItem;
                    break;
                }
                curPrevRow--;
            }

            while (curNextRow < m_TreeView.data.rowCount)
            {
                var curNextItem = m_TreeView.data.GetItem(curNextRow);
                if (!m_TreeView.IsDraggingItem(curNextItem))
                {
                    nextItem = curNextItem;
                    break;
                }
                curNextRow++;
            }
        }

        internal void HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(ref TreeViewItem<TIdentifier> targetItem, int targetItemRow, ref DropPosition dropPosition, int cursorDepth, out bool didChangeTargetToAncestor)
        {
            if (dropPosition != DropPosition.Above && dropPosition != DropPosition.Below)
                throw new ArgumentException("Invalid argument: " + dropPosition);

            didChangeTargetToAncestor = false;

            TreeViewItem<TIdentifier> prevItem, nextItem;
            GetPreviousAndNextItemsIgnoringDraggedItems(targetItemRow, dropPosition, out prevItem, out nextItem);

            if (prevItem == null)
                return; // Above first row so keep targetItem

            bool hoveringBetweenExpandedParentAndFirstChild = prevItem.hasChildren && m_TreeView.data.IsExpanded(prevItem.id);
            int minDepth = nextItem != null ? nextItem.depth : 0;
            int maxDepth = prevItem.depth + (hoveringBetweenExpandedParentAndFirstChild ? 1 : 0);

            // Change targetItem and dropPosition
            targetItem = prevItem;
            dropPosition = DropPosition.Below;

            if (maxDepth <= minDepth)
            {
                if (hoveringBetweenExpandedParentAndFirstChild)
                {
                    targetItem = prevItem.children[0];
                    dropPosition = DropPosition.Above;
                }
                return; // The nextItem is a descendant of previous item so keep targetItem
            }

            if (cursorDepth >= maxDepth)
            {
                if (hoveringBetweenExpandedParentAndFirstChild)
                {
                    targetItem = prevItem.children[0];
                    dropPosition = DropPosition.Above;
                }
                return; // No need to change targetItem if same or higher depth
            }

            // Search through parents for a new target that matches the cursor
            var target = targetItem;
            while (target.depth > minDepth)
            {
                if (target.depth == cursorDepth)
                    break;
                target = target.parent;
            }

            didChangeTargetToAncestor = target != targetItem;

            // Change to new targetItem
            targetItem = target;
        }

        protected bool TryGetDropPosition(TreeViewItem<TIdentifier> item, Rect itemRect, int row, out DropPosition dropPosition)
        {
            Vector2 currentMousePos = Event.current.mousePosition;

            if (itemRect.Contains(currentMousePos))
            {
                float dropBetweenHalfHeight = GetDropBetweenHalfHeight(item, itemRect);
                if (currentMousePos.y >= itemRect.yMax - dropBetweenHalfHeight)
                    dropPosition = DropPosition.Below;
                else if (currentMousePos.y <= itemRect.yMin + dropBetweenHalfHeight)
                    dropPosition = DropPosition.Above;
                else
                    dropPosition = DropPosition.Upon;
                return true;
            }
            else
            {
                // Check overlap with next item (if any)
                float nextOverlap = m_TreeView.gui.halfDropBetweenHeight;
                int nextRow = row + 1;
                if (nextRow < m_TreeView.data.rowCount)
                {
                    Rect nextRect = m_TreeView.gui.GetRowRect(nextRow, itemRect.width);
                    bool nextCanBeParent = m_TreeView.data.CanBeParent(m_TreeView.data.GetItem(nextRow));
                    if (nextCanBeParent)
                        nextOverlap = m_TreeView.gui.halfDropBetweenHeight;
                    else
                        nextOverlap = nextRect.height * 0.5f;
                }
                Rect nextOverlapRect = itemRect;
                nextOverlapRect.y = itemRect.yMax;
                nextOverlapRect.height = nextOverlap;
                if (nextOverlapRect.Contains(currentMousePos))
                {
                    dropPosition = DropPosition.Below;
                    return true;
                }

                // Check overlap above first item
                if (row == 0)
                {
                    Rect overlapUpwards = itemRect;
                    overlapUpwards.yMin -= m_TreeView.gui.halfDropBetweenHeight;
                    overlapUpwards.height = m_TreeView.gui.halfDropBetweenHeight;
                    if (overlapUpwards.Contains(currentMousePos))
                    {
                        dropPosition = DropPosition.Above;
                        return true;
                    }
                }
            }

            dropPosition = DropPosition.Below;
            return false;
        }

        // This method is called from TreeView and handles:
        // - Where the dragged items are dropped (above, below or upon)
        // - Auto expansion of collapsed items when hovering over them
        // - Setting up the render markers for drop location (horizontal lines)
        // 'targetItem' is null when not hovering over any target Item, if so the rest of the arguments are invalid
        public virtual bool DragElement(TreeViewItem<TIdentifier> targetItem, Rect targetItemRect, int row) => DragElementInternal(targetItem, targetItemRect, row);
        public virtual bool DragElementInternal(TreeViewItem<TIdentifier> targetItem, Rect targetItemRect, int row)
        {
            bool perform = Event.current.type == EventType.DragPerform;

            // Are we dragging outside any items
            if (targetItem == null)
            {
                // If so clear any drop markers
                if (m_DropData != null)
                {
                    m_DropData.ClearPerEventState();
                }

                // And let client decide what happens when dragging outside items
                DragAndDrop.visualMode = DoDrag(null, null, perform, DropPosition.Below);
                if (DragAndDrop.visualMode != DragAndDropVisualMode.None && perform)
                    FinalizeDragPerformed(true);

                return false;
            }

            DropPosition dropPosition;
            if (!TryGetDropPosition(targetItem, targetItemRect, row, out dropPosition))
                return false;

            TreeViewItem<TIdentifier> parentItem = null;
            TreeViewItem<TIdentifier> dropRelativeToItem = targetItem;
            bool didChangeTargetToAncestor = false;
            DropPosition originalDropPosition = dropPosition;
            switch (dropPosition)
            {
                case DropPosition.Upon:
                    // Parent change: Client must decide what happens when dropping upon: e.g: insert last or first in child list
                    parentItem = dropRelativeToItem;
                    break;

                case DropPosition.Below:
                case DropPosition.Above:
                    // Sibling change
                    if (getIndentLevelForMouseCursor != null)
                    {
                        int cursorDepth = getIndentLevelForMouseCursor();
                        HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(ref dropRelativeToItem, row, ref dropPosition, cursorDepth, out didChangeTargetToAncestor);
                    }
                    else
                    {
                        if (dropPosition == DropPosition.Below && m_TreeView.data.IsExpanded(dropRelativeToItem) && dropRelativeToItem.hasChildren)
                        {
                            // When hovering between an expanded parent and its first child then make sure we change state to match that
                            dropPosition = DropPosition.Above;
                            dropRelativeToItem = dropRelativeToItem.children[0];
                        }
                    }
                    parentItem = dropRelativeToItem.parent;
                    break;

                default:
                    Debug.LogError("Unhandled enum. Report a bug.");
                    break;
            }

            if (perform)
            {
                DragAndDropVisualMode mode = DragAndDropVisualMode.None;
                // Try Drop upon target item
                if (dropPosition == DropPosition.Upon)
                    mode = DoDrag(dropRelativeToItem, dropRelativeToItem, true, dropPosition);

                // Drop between items
                if (mode == DragAndDropVisualMode.None && parentItem != null)
                {
                    mode = DoDrag(parentItem, dropRelativeToItem, true, dropPosition);
                }

                // Finalize drop
                if (mode != DragAndDropVisualMode.None)
                {
                    FinalizeDragPerformed(false);
                }
                else
                {
                    DragCleanup(true);
                    m_TreeView.NotifyListenersThatDragEnded(null, false);
                }
            }
            else // DragUpdate
            {
                if (m_DropData == null)
                    m_DropData = new DropData();
                m_DropData.ClearPerEventState();

                // Try drop on top of items
                if (dropPosition == DropPosition.Upon)
                {
                    int itemControlID = TreeViewController<TIdentifier>.GetItemControlID(dropRelativeToItem);
                    HandleAutoExpansion(itemControlID, dropRelativeToItem, targetItemRect);

                    var mode = DoDrag(dropRelativeToItem, dropRelativeToItem, false, dropPosition);
                    if (mode != DragAndDropVisualMode.None)
                    {
                        m_DropData.dropTargetControlID = itemControlID;
                        DragAndDrop.visualMode = mode;
                    }
                }
                // Drop between items
                else if (dropRelativeToItem != null && parentItem != null)
                {
                    var mode = DoDrag(parentItem, dropRelativeToItem, false, dropPosition);
                    if (mode != DragAndDropVisualMode.None)
                    {
                        drawRowMarkerAbove = dropPosition == DropPosition.Above;
                        m_DropData.rowMarkerControlID = TreeViewController<TIdentifier>.GetItemControlID(dropRelativeToItem);
                        m_DropData.insertionMarkerYPosition = originalDropPosition == DropPosition.Above ? targetItemRect.y : targetItemRect.yMax;
                        m_DropData.insertRelativeToSibling = dropRelativeToItem;
                        if (didChangeTargetToAncestor)
                        {
                            m_DropData.ancestorControlID = TreeViewController<TIdentifier>.GetItemControlID(dropRelativeToItem);
                        }

                        DragAndDrop.visualMode = mode;
                    }
                }
            }

            Event.current.Use();
            return true;
        }

        void FinalizeDragPerformed(bool revertExpanded)
        {
            string undoActionName = "Drag and Drop Multiple Objects";

            DragCleanup(revertExpanded);
            DragAndDrop.AcceptDrag();

            if (m_TreeView is TreeViewController<int> instanceIDTreeView) // todo: change to EntityId
            {
                List<UnityEngine.Object> objs = new List<UnityEngine.Object>(DragAndDrop.objectReferences); // TODO, what about when dragging non objects...

                bool draggedItemsFromOwnTreeView = true;
                if (objs.Count > 0 && objs[0] != null && TreeViewUtility<int>.FindItemInList(objs[0].GetInstanceID(), instanceIDTreeView.data.GetRows()) == null)
                    draggedItemsFromOwnTreeView = false;

                int[] newSelection = new int[objs.Count];
                for (int i = 0; i < objs.Count; ++i)
                {
                    if (objs[i] == null)
                        continue;

                    newSelection[i] = (objs[i].GetInstanceID());
                }

                instanceIDTreeView.NotifyListenersThatDragEnded(newSelection, draggedItemsFromOwnTreeView);
                if (objs.Count == 1)
                    undoActionName = "Drag and Drop " + objs[0].name;
            }

            Undo.SetCurrentGroupName(undoActionName);
        }

        protected virtual void HandleAutoExpansion(int itemControlID, TreeViewItem<TIdentifier> targetItem, Rect targetItemRect)
        {
            Vector2 currentMousePos = Event.current.mousePosition;

            // Handle auto expansion
            float targetItemIndent = m_TreeView.gui.GetContentIndent(targetItem);
            float betweenHalfHeight = GetDropBetweenHalfHeight(targetItem, targetItemRect);
            Rect indentedContentRect = new Rect(targetItemRect.x + targetItemIndent, targetItemRect.y + betweenHalfHeight, targetItemRect.width - targetItemIndent, targetItemRect.height - betweenHalfHeight * 2);
            bool hoveringOverIndentedContent = indentedContentRect.Contains(currentMousePos);

            if (itemControlID != m_DropData.lastControlID || !hoveringOverIndentedContent || m_DropData.expandItemBeginPosition != currentMousePos)
            {
                m_DropData.lastControlID = itemControlID;
                m_DropData.expandItemBeginTimer = Time.realtimeSinceStartup;
                m_DropData.expandItemBeginPosition = currentMousePos;
            }

            bool expandTimerExpired = Time.realtimeSinceStartup - m_DropData.expandItemBeginTimer > k_DropExpandTimeout;
            bool mayExpand = hoveringOverIndentedContent && expandTimerExpired;

            // Auto open folders we are about to drag into
            if (targetItem != null && mayExpand && targetItem.hasChildren && !m_TreeView.data.IsExpanded(targetItem))
            {
                // Store the expanded array prior to drag so we can revert it with a delay later
                if (m_DropData.expandedArrayBeforeDrag == null)
                {
                    List<TIdentifier> expandedIDs = GetCurrentExpanded();
                    m_DropData.expandedArrayBeforeDrag = expandedIDs.ToArray();
                }

                m_TreeView.data.SetExpanded(targetItem, true);
                m_DropData.expandItemBeginTimer = Time.realtimeSinceStartup;
                m_DropData.lastControlID = 0;
            }
        }

        public virtual void DragCleanup(bool revertExpanded)
        {
            if (m_DropData != null)
            {
                if (m_DropData.expandedArrayBeforeDrag != null && revertExpanded)
                {
                    RestoreExpanded(new List<TIdentifier>(m_DropData.expandedArrayBeforeDrag));
                }
                m_DropData = new DropData();
            }
        }

        public List<TIdentifier> GetCurrentExpanded()
        {
            var visibleItems = m_TreeView.data.GetRows();
            List<TIdentifier> expandedIDs = (from item in visibleItems
                where m_TreeView.data.IsExpanded(item)
                select item.id).ToList();
            return expandedIDs;
        }

        // We assume that we can only have expanded items during dragging
        public void RestoreExpanded(List<TIdentifier> ids)
        {
            var visibleItems = m_TreeView.data.GetRows();
            foreach (TreeViewItem<TIdentifier> item in visibleItems)
                m_TreeView.data.SetExpanded(item, ids.Contains(item.id));
        }

        internal static int GetInsertionIndex(TreeViewItem<TIdentifier> parentItem, TreeViewItem<TIdentifier> targetItem, DropPosition dropPosition)
        {
            if (parentItem == null)
                return -1;

            int insertionIndex;
            if (parentItem == targetItem)
            {
                // Let user decide what index item should be added to when dropping upon
                insertionIndex = -1;
                Assert.AreEqual(DropPosition.Upon, dropPosition);
            }
            else
            {
                int index = parentItem.children.IndexOf(targetItem);
                if (index >= 0)
                {
                    if (dropPosition == DropPosition.Below)
                        insertionIndex = index + 1;
                    else
                        insertionIndex = index;
                }
                else
                {
                    Debug.LogError(Constants.GetInsertionIndexNotFound);
                    insertionIndex = -1;
                }
            }
            return insertionIndex;
        }
    }
} // namespace UnityEditor
