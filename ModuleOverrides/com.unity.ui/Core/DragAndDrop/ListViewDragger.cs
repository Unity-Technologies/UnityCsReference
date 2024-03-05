// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal class ListViewDragger : DragEventsProcessor
    {
        internal struct DragPosition : IEquatable<DragPosition>
        {
            public int insertAtIndex;
            public int parentId;
            public int childIndex;
            public ReusableCollectionItem recycledItem;
            public DragAndDropPosition dropPosition;

            public bool Equals(DragPosition other)
            {
                return insertAtIndex == other.insertAtIndex
                    && parentId == other.parentId
                    && childIndex == other.childIndex
                    && Equals(recycledItem, other.recycledItem)
                    && dropPosition == other.dropPosition;
            }

            public override bool Equals(object obj)
            {
                return obj is DragPosition position && Equals(position);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = insertAtIndex;
                    hashCode = (hashCode * 397) ^ parentId;
                    hashCode = (hashCode * 397) ^ childIndex;
                    hashCode = (hashCode * 397) ^ (recycledItem?.GetHashCode() ?? 0);
                    hashCode = (hashCode * 397) ^ (int)dropPosition;
                    return hashCode;
                }
            }
        }


        DragPosition m_LastDragPosition;

        VisualElement m_DragHoverBar;
        VisualElement m_DragHoverItemMarker;
        VisualElement m_DragHoverSiblingMarker;

        float m_LeftIndentation = -1f;
        float m_SiblingBottom = -1f;

        const int k_AutoScrollAreaSize = 5;
        const int k_BetweenElementsAreaSize = 5;
        const int k_PanSpeed = 20;
        const int k_DragHoverBarHeight = 2;

        protected BaseVerticalCollectionView targetView
        {
            get { return m_Target as BaseVerticalCollectionView; }
        }

        protected ScrollView targetScrollView
        {
            get { return targetView.scrollView; }
        }

        public ICollectionDragAndDropController dragAndDropController { get; set; }

        public ListViewDragger(BaseVerticalCollectionView listView)
            : base(listView) {}

        protected override bool CanStartDrag(Vector3 pointerPosition)
        {
            if (dragAndDropController == null)
                return false;

            if (!targetScrollView.contentContainer.worldBound.Contains(pointerPosition))
                return false;

            var recycledItem = GetRecycledItem(pointerPosition);

            if (targetView.selectedIds.Any())
            {
                return dragAndDropController.CanStartDrag(targetView.selectedIds);
            }

            return recycledItem != null && dragAndDropController.CanStartDrag(new[] { recycledItem.id });
        }

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition)
        {
            var recycledItem = GetRecycledItem(pointerPosition);
            IEnumerable<int> ids;
            if (recycledItem != null)
            {
                if (!targetView.selectedIndices.Contains(recycledItem.index))
                {
                    targetView.SetSelection(recycledItem.index);
                }

                ids = targetView.selectedIds;
            }
            else
            {
                ids = targetView.selectedIds.Any() ? targetView.selectedIds : Enumerable.Empty<int>();
            }

            var startDragArgs = dragAndDropController.SetupDragAndDrop(ids);
            startDragArgs.SetGenericData(DragAndDropData.dragSourceKey, targetView);
            return startDragArgs;
        }

        protected internal override void UpdateDrag(Vector3 pointerPosition)
        {
            var dragPosition = new DragPosition();
            var visualMode = GetVisualMode(pointerPosition, ref dragPosition);

            if (visualMode == DragVisualMode.Rejected)
            {
                ClearDragAndDropUI(false);
            }
            else
            {
                HandleDragAndScroll(pointerPosition);
                ApplyDragAndDropUI(dragPosition);
            }

            dragAndDrop.SetVisualMode(visualMode);
            dragAndDrop.UpdateDrag(pointerPosition);
        }

        DragVisualMode GetVisualMode(Vector3 pointerPosition, ref DragPosition dragPosition)
        {
            if (dragAndDropController == null)
            {
                return DragVisualMode.Rejected;
            }

            var foundPosition = TryGetDragPosition(pointerPosition, ref dragPosition);
            var args = MakeDragAndDropArgs(dragPosition);

            return foundPosition ? dragAndDropController.HandleDragAndDrop(args) : DragVisualMode.Rejected;
        }

        protected internal override void OnDrop(Vector3 pointerPosition)
        {
            var dragPosition = new DragPosition();
            if (!TryGetDragPosition(pointerPosition, ref dragPosition))
                return;

            var args = MakeDragAndDropArgs(dragPosition);
            if (dragAndDropController.HandleDragAndDrop(args) != DragVisualMode.Rejected)
            {
                dragAndDropController.OnDrop(args);
                dragAndDrop.AcceptDrag();
            }
            else
            {
                dragAndDropController.DragCleanup();
            }
        }

        // Internal for tests.
        internal void HandleDragAndScroll(Vector2 pointerPosition)
        {
            var scrollUp = pointerPosition.y < targetScrollView.worldBound.yMin + k_AutoScrollAreaSize;
            var scrollDown = pointerPosition.y > targetScrollView.worldBound.yMax - k_AutoScrollAreaSize;
            if (scrollUp || scrollDown)
            {
                var offset = targetScrollView.scrollOffset + (scrollUp ? Vector2.down : Vector2.up) * k_PanSpeed;
                offset.y = Mathf.Clamp(offset.y, 0f, Mathf.Max(0, targetScrollView.contentContainer.worldBound.height - targetScrollView.contentViewport.worldBound.height));
                targetScrollView.scrollOffset = offset;
            }
        }

        protected void ApplyDragAndDropUI(DragPosition dragPosition)
        {
            if (m_LastDragPosition.Equals(dragPosition))
                return;

            if (m_DragHoverBar == null)
            {
                m_DragHoverBar = new VisualElement();
                m_DragHoverBar.AddToClassList(BaseVerticalCollectionView.dragHoverBarUssClassName);
                m_DragHoverBar.style.width = targetView.localBound.width;
                m_DragHoverBar.style.visibility = Visibility.Hidden;
                m_DragHoverBar.pickingMode = PickingMode.Ignore;

                void GeometryChangedCallback(GeometryChangedEvent e)
                {
                    m_DragHoverBar.style.width = targetView.localBound.width;
                }

                targetView.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
                targetScrollView.contentViewport.Add(m_DragHoverBar);
            }

            if (m_DragHoverItemMarker == null && targetView is Experimental.TreeView)
            {
                m_DragHoverItemMarker = new VisualElement();
                m_DragHoverItemMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassName);
                m_DragHoverItemMarker.style.visibility = Visibility.Hidden;
                m_DragHoverItemMarker.pickingMode = PickingMode.Ignore;
                m_DragHoverBar.Add(m_DragHoverItemMarker);

                m_DragHoverSiblingMarker = new VisualElement();
                m_DragHoverSiblingMarker.AddToClassList(BaseVerticalCollectionView.dragHoverMarkerUssClassName);
                m_DragHoverSiblingMarker.style.visibility = Visibility.Hidden;
                m_DragHoverSiblingMarker.pickingMode = PickingMode.Ignore;
                targetScrollView.contentViewport.Add(m_DragHoverSiblingMarker);
            }

            ClearDragAndDropUI(false);
            m_LastDragPosition = dragPosition;
            switch (dragPosition.dropPosition)
            {
                case DragAndDropPosition.OverItem:
                    dragPosition.recycledItem.rootElement.AddToClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
                    break;
                case DragAndDropPosition.BetweenItems:
                    if (dragPosition.insertAtIndex == 0)
                    {
                        PlaceHoverBarAt(0);
                    }
                    else
                    {
                        var beforeItem = targetView.GetRecycledItemFromIndex(dragPosition.insertAtIndex - 1);
                        var afterItem = targetView.GetRecycledItemFromIndex(dragPosition.insertAtIndex);
                        PlaceHoverBarAtElement(beforeItem ?? afterItem);
                    }

                    break;
                case DragAndDropPosition.OutsideItems:
                    var recycledItem = targetView.GetRecycledItemFromIndex(targetView.itemsSource.Count - 1);
                    if (recycledItem != null)
                        PlaceHoverBarAtElement(recycledItem);
                    else
                        PlaceHoverBarAt(0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dragPosition.dropPosition),
                        dragPosition.dropPosition,
                        $"Unsupported {nameof(dragPosition.dropPosition)} value");
            }

            // For some reason, the move cursor is missing when using visualMode, so we force it in editor.
            if (dragAndDrop.data.visualMode == DragVisualMode.Move && targetView.elementPanel.contextType == ContextType.Editor)
            {
                targetView.elementPanel.cursorManager.SetCursor(new Cursor { defaultCursorId = 8 });
            }
        }

        protected virtual bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
            {
                if (!targetView.worldBound.Contains(pointerPosition))
                    return false;

                dragPosition.dropPosition = DragAndDropPosition.OutsideItems;
                if (pointerPosition.y >= targetScrollView.contentContainer.worldBound.yMax)
                    dragPosition.insertAtIndex = targetView.itemsSource.Count;
                else
                    dragPosition.insertAtIndex = 0;

                HandleTreePosition(pointerPosition, ref dragPosition);
                return true;
            }

            // Below an item
            if (recycledItem.rootElement.worldBound.yMax - pointerPosition.y < k_BetweenElementsAreaSize)
            {
                dragPosition.insertAtIndex = recycledItem.index + 1;
                dragPosition.dropPosition = DragAndDropPosition.BetweenItems;
            }
            // Upon an item
            else if (pointerPosition.y - recycledItem.rootElement.worldBound.yMin > k_BetweenElementsAreaSize)
            {
                var scrollOffset = targetScrollView.scrollOffset;
                targetScrollView.ScrollTo(recycledItem.rootElement);
                if (scrollOffset != targetScrollView.scrollOffset)
                {
                    return TryGetDragPosition(pointerPosition, ref dragPosition);
                }

                dragPosition.recycledItem = recycledItem;
                dragPosition.insertAtIndex = recycledItem.index;
                dragPosition.dropPosition = DragAndDropPosition.OverItem;
            }
            // Above an item
            else
            {
                dragPosition.insertAtIndex = recycledItem.index;
                dragPosition.dropPosition = DragAndDropPosition.BetweenItems;
            }

            HandleTreePosition(pointerPosition, ref dragPosition);
            return true;
        }

        void HandleTreePosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            // Reset values.
            dragPosition.parentId = -1;
            dragPosition.childIndex = -1;
            m_LeftIndentation = -1f;
            m_SiblingBottom = -1f;

            if (targetView is not Experimental.TreeView treeView)
                return;

            if (dragPosition.insertAtIndex < 0)
                return; // Already handled.

            // Insert inside an item, as the last child.
            var treeController = treeView.viewController;
            if (dragPosition.dropPosition == DragAndDropPosition.OverItem)
            {
                dragPosition.parentId = treeController.GetIdForIndex(dragPosition.insertAtIndex);
                dragPosition.childIndex = -1;
                return;
            }

            // Above first row.
            if (dragPosition.insertAtIndex <= 0)
            {
                dragPosition.childIndex = 0;
                return;
            }

            HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(ref dragPosition, pointerPosition);
        }

        void HandleSiblingInsertionAtAvailableDepthsAndChangeTargetIfNeeded(ref DragPosition dragPosition, Vector2 pointerPosition)
        {
            if (targetView is not Experimental.TreeView treeView)
                return;

            var treeController = treeView.viewController;
            var targetIndex = dragPosition.insertAtIndex;
            var initialTargetId = treeController.GetIdForIndex(targetIndex);
            // var targetId = initialTargetId;

            GetPreviousAndNextItemsIgnoringDraggedItems(dragPosition.insertAtIndex, out var previousItemId, out var nextItemId);

            if (previousItemId == TreeItem.invalidId)
                return; // Above first row so keep targetItem

            var hoveringBetweenExpandedParentAndFirstChild = treeController.HasChildren(previousItemId) && treeView.IsExpanded(previousItemId);
            var previousItemDepth = treeController.GetIndentationDepth(previousItemId);
            var nextItemDepth = treeController.GetIndentationDepth(nextItemId);
            var minDepth = nextItemId != TreeItem.invalidId ? nextItemDepth : 0;
            var maxDepth = treeController.GetIndentationDepth(previousItemId) + (hoveringBetweenExpandedParentAndFirstChild ? 1 : 0);

            // Change target item.
            var targetId = previousItemId;

            // Get the indent width
            var toggleWidth = 15f;
            var indentWidth = 15f;
            if (previousItemDepth > 0)
            {
                var rootElement = treeView.GetRootElementForId(previousItemId);
                var indentElement = rootElement.Q(Experimental.TreeView.itemIndentUssClassName);
                var toggle = rootElement.Q(Experimental.TreeView.itemToggleUssClassName);
                toggleWidth = toggle.layout.width;
                indentWidth = indentElement.layout.width / previousItemDepth;
            }
            else
            {
                var initialItemDepth = treeView.viewController.GetIndentationDepth(initialTargetId);
                if (initialItemDepth > 0)
                {
                    var rootElement = treeView.GetRootElementForId(initialTargetId);
                    var indentElement = rootElement.Q(Experimental.TreeView.itemIndentUssClassName);
                    var toggle = rootElement.Q(Experimental.TreeView.itemToggleUssClassName);
                    toggleWidth = toggle.layout.width;
                    indentWidth = indentElement.layout.width / initialItemDepth;
                }
            }

            if (maxDepth <= minDepth)
            {
                m_LeftIndentation = toggleWidth + indentWidth * minDepth;
                if (hoveringBetweenExpandedParentAndFirstChild)
                {
                    dragPosition.parentId = previousItemId;
                    dragPosition.childIndex = 0;
                }
                else
                {
                    dragPosition.parentId = treeController.GetParentId(previousItemId);
                    dragPosition.childIndex = treeController.GetChildIndexForId(nextItemId);
                }
                return; // The nextItem is a descendant of previous item so keep targetItem
            }

            var localMousePosition = treeView.scrollView.contentContainer.WorldToLocal(pointerPosition);
            var cursorDepth = Mathf.FloorToInt((localMousePosition.x - toggleWidth) / indentWidth);
            if (cursorDepth >= maxDepth)
            {
                m_LeftIndentation = toggleWidth + indentWidth * maxDepth;
                if (hoveringBetweenExpandedParentAndFirstChild)
                {
                    dragPosition.parentId = previousItemId;
                    dragPosition.childIndex = 0;
                }
                else
                {
                    dragPosition.parentId = treeController.GetParentId(previousItemId);
                    dragPosition.childIndex = treeController.GetChildIndexForId(previousItemId) + 1;
                }
                return; // No need to change targetItem if same or higher depth
            }

            // Search through parents for a new target that matches the cursor
            var targetDepth = treeController.GetIndentationDepth(targetId);
            while (targetDepth > minDepth)
            {
                if (targetDepth == cursorDepth)
                    break;

                targetId = treeController.GetParentId(targetId);
                targetDepth--;
            }

            var didChangeTargetToAncestor = targetId != initialTargetId;
            if (didChangeTargetToAncestor)
            {
                var siblingRoot = treeView.GetRootElementForId(targetId);
                if (siblingRoot != null)
                {
                    var contentViewport = targetScrollView.contentViewport;
                    var elementBounds = contentViewport.WorldToLocal(siblingRoot.worldBound);
                    if (contentViewport.localBound.yMin < elementBounds.yMax && elementBounds.yMax < contentViewport.localBound.yMax)
                    {
                        m_SiblingBottom = elementBounds.yMax;
                    }
                }
            }

            // Change to new target item
            dragPosition.parentId = treeController.GetParentId(targetId);
            dragPosition.childIndex = treeController.GetChildIndexForId(targetId) + 1;
            m_LeftIndentation = toggleWidth + indentWidth * targetDepth;
        }

        void GetPreviousAndNextItemsIgnoringDraggedItems(int insertAtIndex, out int previousItemId, out int nextItemId)
        {
            previousItemId = nextItemId = -1;
            var previousItemIndex = insertAtIndex - 1;
            var nextItemIndex = insertAtIndex;

            while (previousItemIndex >= 0)
            {
                var id = targetView.viewController.GetIdForIndex(previousItemIndex);
                if (!dragAndDropController.GetSortedSelectedIds().Contains(id))
                {
                    previousItemId = id;
                    break;
                }

                previousItemIndex--;
            }

            while (nextItemIndex < targetView.itemsSource.Count)
            {
                var id = targetView.viewController.GetIdForIndex(nextItemIndex);
                if (!dragAndDropController.GetSortedSelectedIds().Contains(id))
                {
                    nextItemId = id;
                    break;
                }

                nextItemIndex++;
            }
        }

        protected DragAndDropArgs MakeDragAndDropArgs(DragPosition dragPosition)
        {
            object target = null;
            var recycledItem = dragPosition.recycledItem;
            if (recycledItem != null)
                target = targetView.viewController.GetItemForIndex(recycledItem.index);

            return new DragAndDropArgs
            {
                target = target,
                insertAtIndex = dragPosition.insertAtIndex,
                parentId = dragPosition.parentId,
                childIndex = dragPosition.childIndex,
                dragAndDropPosition = dragPosition.dropPosition,
                dragAndDropData = dragAndDrop.data,
            };
        }

        float GetHoverBarTopPosition(ReusableCollectionItem item)
        {
            var contentViewport = targetScrollView.contentViewport;
            var elementBounds = contentViewport.WorldToLocal(item.rootElement.worldBound);
            var top = Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - k_DragHoverBarHeight);
            return top;
        }

        void PlaceHoverBarAtElement(ReusableCollectionItem item)
        {
            PlaceHoverBarAt(GetHoverBarTopPosition(item), m_LeftIndentation, m_SiblingBottom);
        }

        void PlaceHoverBarAt(float top, float indentationPadding = -1f, float siblingBottom = -1f)
        {
            m_DragHoverBar.style.top = top;
            m_DragHoverBar.style.visibility = Visibility.Visible;
            if (m_DragHoverItemMarker != null)
            {
                m_DragHoverItemMarker.style.visibility = Visibility.Visible;
            }

            if (indentationPadding >= 0)
            {
                m_DragHoverBar.style.marginLeft = indentationPadding;
                m_DragHoverBar.style.width = targetView.localBound.width - indentationPadding;

                if (siblingBottom > 0 && m_DragHoverSiblingMarker != null)
                {
                    m_DragHoverSiblingMarker.style.top = siblingBottom;
                    m_DragHoverSiblingMarker.style.visibility = Visibility.Visible;
                    m_DragHoverSiblingMarker.style.marginLeft = indentationPadding;
                }
            }
            else
            {
                m_DragHoverBar.style.marginLeft = 0;
                m_DragHoverBar.style.width = targetView.localBound.width;
            }
        }

        protected override void ClearDragAndDropUI(bool dragCancelled)
        {
            if (dragCancelled)
            {
                dragAndDropController.DragCleanup();
            }

            targetView.elementPanel.cursorManager.ResetCursor();

            m_LastDragPosition = new DragPosition();
            foreach (var item in targetView.activeItems)
            {
                item.rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
            }

            if (m_DragHoverBar != null)
                m_DragHoverBar.style.visibility = Visibility.Hidden;
            if (m_DragHoverItemMarker != null)
                m_DragHoverItemMarker.style.visibility = Visibility.Hidden;
            if (m_DragHoverSiblingMarker != null)
                m_DragHoverSiblingMarker.style.visibility = Visibility.Hidden;
        }

        protected ReusableCollectionItem GetRecycledItem(Vector3 pointerPosition)
        {
            foreach (var recycledItem in targetView.activeItems)
            {
                if (recycledItem.rootElement.worldBound.Contains(pointerPosition))
                    return recycledItem;
            }

            return null;
        }
    }

    internal static class ListViewDraggerExtension
    {
        public static ReusableCollectionItem GetRecycledItemFromId(this BaseVerticalCollectionView listView, int id)
        {
            foreach (var recycledItem in listView.activeItems)
            {
                if (recycledItem.id.Equals(id))
                    return recycledItem;
            }

            return null;
        }

        public static ReusableCollectionItem GetRecycledItemFromIndex(this BaseVerticalCollectionView listView, int index)
        {
            foreach (var recycledItem in listView.activeItems)
            {
                if (recycledItem.index.Equals(index))
                    return recycledItem;
            }

            return null;
        }
    }
}
