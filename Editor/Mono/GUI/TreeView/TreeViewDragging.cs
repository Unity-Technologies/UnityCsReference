// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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


    internal abstract class TreeViewDragging : ITreeViewDragging
    {
        protected TreeViewController m_TreeView;

        protected class DropData
        {
            public int[]    expandedArrayBeforeDrag;
            public int      lastControlID = -1;
            public int      dropTargetControlID = -1;
            public int      rowMarkerControlID = -1;
            public double   expandItemBeginTimer;
            public Vector2  expandItemBeginPosition;
        }

        public enum DropPosition
        {
            Upon = 0,
            Below = 1,
            Above = 2
        }

        protected DropData m_DropData = new DropData();
        const double k_DropExpandTimeout = 0.7;

        public TreeViewDragging(TreeViewController treeView)
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

        public bool drawRowMarkerAbove { get; set; }

        public virtual bool CanStartDrag(TreeViewItem targetItem, List<int> draggedItemIDs, Vector2 mouseDownPosition)
        {
            return true;
        }

        // This method is called from TreeView when a drag is started
        // Client should setup the drag data
        public abstract void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs);

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
        public abstract DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPosition);


        // This method is called from TreeView and handles:
        // - Where the dragged items are dropped (above, below or upon)
        // - Auto expansion of collapsed items when hovering over them
        // - Setting up the render markers for drop location (horizontal lines)
        // 'targetItem' is null when not hovering over any target Item, if so the rest if the arguments are invalid
        public virtual bool DragElement(TreeViewItem targetItem, Rect targetItemRect, bool firstItem)
        {
            bool perform = Event.current.type == EventType.DragPerform;

            // Are we dragging outside any items
            if (targetItem == null)
            {
                // If so clear any drop markers
                if (m_DropData != null)
                {
                    m_DropData.dropTargetControlID = 0;
                    m_DropData.rowMarkerControlID = 0;
                }

                // And let client decide what happens when dragging outside items

                DragAndDrop.visualMode = DoDrag(null, null, perform, DropPosition.Below);
                if (DragAndDrop.visualMode != DragAndDropVisualMode.None && perform)
                    FinalizeDragPerformed(true);

                return false;
            }

            Vector2 currentMousePos = Event.current.mousePosition;
            bool targetCanBeParent = m_TreeView.data.CanBeParent(targetItem);

            // We create a rect that overlaps downwards to detect dropUpon or dropBetween
            Rect dropRect = targetItemRect;
            float betweenHalfHeight = targetCanBeParent ? m_TreeView.gui.halfDropBetweenHeight : targetItemRect.height * 0.5f;
            if (firstItem)
                dropRect.yMin -= betweenHalfHeight;
            dropRect.yMax += betweenHalfHeight;
            if (!dropRect.Contains(currentMousePos))
                return false;

            // Ok we are inside our offset rect
            DropPosition dropPosition;

            if (currentMousePos.y >= targetItemRect.yMax - betweenHalfHeight)
                dropPosition = DropPosition.Below;
            else if (firstItem && currentMousePos.y <= targetItemRect.yMin + betweenHalfHeight)
                dropPosition = DropPosition.Above;
            else
                dropPosition = targetCanBeParent ? DropPosition.Upon : DropPosition.Above;

            TreeViewItem parentItem = null;
            switch (dropPosition)
            {
                case DropPosition.Upon:
                {
                    // Client must decide what happens when dropping upon: e.g: insert last or first in child list
                    parentItem = targetItem;
                }
                break;
                case DropPosition.Below:
                {
                    // When hovering between an expanded parent and its first child then make sure we change state to match that
                    if (m_TreeView.data.IsExpanded(targetItem) && targetItem.hasChildren)
                    {
                        parentItem = targetItem;
                        targetItem = targetItem.children[0];
                        dropPosition = DropPosition.Above;
                    }
                    else
                    {
                        // Drop as next sibling to target
                        parentItem = targetItem.parent;
                    }
                }
                break;
                case DropPosition.Above:
                {
                    parentItem = targetItem.parent;
                }
                break;
                default:
                    Assert.IsTrue(false, "Unhandled enum");
                    break;
            }


            DragAndDropVisualMode mode = DragAndDropVisualMode.None;
            if (perform)
            {
                // Try Drop on top of element
                if (dropPosition == DropPosition.Upon)
                    mode = DoDrag(targetItem, targetItem, true, dropPosition);

                // Fall back to dropping on parent  (drop between elements)
                if (mode == DragAndDropVisualMode.None && parentItem != null)
                {
                    mode = DoDrag(parentItem, targetItem, true, dropPosition);
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
                m_DropData.dropTargetControlID = 0;
                m_DropData.rowMarkerControlID = 0;

                int itemControlID = TreeViewController.GetItemControlID(targetItem);
                HandleAutoExpansion(itemControlID, targetItem, targetItemRect, betweenHalfHeight, currentMousePos);

                // Try drop on top of element
                if (dropPosition == DropPosition.Upon)
                    mode = DoDrag(targetItem, targetItem, false, dropPosition);

                if (mode != DragAndDropVisualMode.None)
                {
                    m_DropData.dropTargetControlID = itemControlID;
                    DragAndDrop.visualMode = mode;
                }
                // Fall back to dropping on parent (drop between elements)
                else if (targetItem != null && parentItem != null)
                {
                    mode = DoDrag(parentItem, targetItem, false, dropPosition);

                    if (mode != DragAndDropVisualMode.None)
                    {
                        drawRowMarkerAbove = dropPosition == DropPosition.Above;
                        m_DropData.rowMarkerControlID = itemControlID;
                        DragAndDrop.visualMode = mode;
                    }
                }
            }

            Event.current.Use();
            return true;
        }

        void FinalizeDragPerformed(bool revertExpanded)
        {
            DragCleanup(revertExpanded);
            DragAndDrop.AcceptDrag();

            List<Object> objs = new List<Object>(DragAndDrop.objectReferences); // TODO, what about when dragging non objects...

            bool draggedItemsFromOwnTreeView = true;
            if (objs.Count > 0 && objs[0] != null && TreeViewUtility.FindItemInList(objs[0].GetInstanceID(), m_TreeView.data.GetRows()) == null)
                draggedItemsFromOwnTreeView = false;

            int[] newSelection = new int[objs.Count];
            for (int i = 0; i < objs.Count; ++i)
            {
                if (objs[i] == null)
                    continue;

                newSelection[i] = (objs[i].GetInstanceID());
            }
            m_TreeView.NotifyListenersThatDragEnded(newSelection, draggedItemsFromOwnTreeView);
        }

        protected virtual void HandleAutoExpansion(int itemControlID, TreeViewItem targetItem, Rect targetItemRect, float betweenHalfHeight, Vector2 currentMousePos)
        {
            // Handle auto expansion
            float targetItemIndent = m_TreeView.gui.GetContentIndent(targetItem);
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
                    List<int> expandedIDs = GetCurrentExpanded();
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
                    RestoreExpanded(new List<int>(m_DropData.expandedArrayBeforeDrag));
                }
                m_DropData = new DropData();
            }
        }

        public List<int> GetCurrentExpanded()
        {
            var visibleItems = m_TreeView.data.GetRows();
            List<int> expandedIDs = (from item in visibleItems
                                     where m_TreeView.data.IsExpanded(item)
                                     select item.id).ToList();
            return expandedIDs;
        }

        // We assume that we can only have expanded items during dragging
        public void RestoreExpanded(List<int> ids)
        {
            var visibleItems = m_TreeView.data.GetRows();
            foreach (TreeViewItem item in visibleItems)
                m_TreeView.data.SetExpanded(item, ids.Contains(item.id));
        }

        internal static int GetInsertionIndex(TreeViewItem parentItem, TreeViewItem targetItem, DropPosition dropPosition)
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
                    Debug.LogError("Did not find targetItem,; should be a child of parentItem");
                    insertionIndex = -1;
                }
            }
            return insertionIndex;
        }
    }
} // namespace UnityEditor
