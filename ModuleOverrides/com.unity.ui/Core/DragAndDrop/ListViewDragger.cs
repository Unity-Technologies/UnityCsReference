// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEngine.UIElements
{
    internal class ListViewDragger : DragEventsProcessor
    {
        internal struct DragPosition : IEquatable<DragPosition>
        {
            public int insertAtIndex;
            public ReusableCollectionItem recycledItem;
            public DragAndDropPosition dragAndDropPosition;

            public bool Equals(DragPosition other)
            {
                return insertAtIndex == other.insertAtIndex
                    && Equals(recycledItem, other.recycledItem)
                    && dragAndDropPosition == other.dragAndDropPosition;
            }

            public override bool Equals(object obj)
            {
                return obj is DragPosition && Equals((DragPosition)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = insertAtIndex;
                    hashCode = (hashCode * 397) ^ (recycledItem != null ? recycledItem.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (int)dragAndDropPosition;
                    return hashCode;
                }
            }
        }

        private DragPosition m_LastDragPosition;

        private VisualElement m_DragHoverBar;

        private const int k_AutoScrollAreaSize = 5;
        private const int k_BetweenElementsAreaSize = 5;
        private const int k_PanSpeed = 20;
        private const int k_DragHoverBarHeight = 2;

        protected BaseVerticalCollectionView targetListView
        {
            get { return m_Target as BaseVerticalCollectionView; }
        }

        protected ScrollView targetScrollView
        {
            get { return targetListView.scrollView; }
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

            if (targetListView.selectedItems.Any())
                return dragAndDropController.CanStartDrag(targetListView.selectedIndices);

            var recycledItem = GetRecycledItem(pointerPosition);
            return recycledItem != null && dragAndDropController.CanStartDrag(new[] { recycledItem.index });
        }

        protected override StartDragArgs StartDrag(Vector3 pointerPosition)
        {
            if (targetListView.selectedItems.Any())
                return dragAndDropController.SetupDragAndDrop(targetListView.selectedIndices);

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
                return default;

            return dragAndDropController.SetupDragAndDrop(new[] { recycledItem.index });
        }

        protected override DragVisualMode UpdateDrag(Vector3 pointerPosition)
        {
            var dragPosition = new DragPosition();
            var visualMode = GetVisualMode(pointerPosition, ref dragPosition);
            if (visualMode == DragVisualMode.Rejected)
                ClearDragAndDropUI();
            else
                ApplyDragAndDropUI(dragPosition);

            return visualMode;
        }

        private DragVisualMode GetVisualMode(Vector3 pointerPosition, ref DragPosition dragPosition)
        {
            if (dragAndDropController == null)
                return DragVisualMode.Rejected;

            HandleDragAndScroll(pointerPosition);
            if (!TryGetDragPosition(pointerPosition, ref dragPosition))
                return DragVisualMode.Rejected;

            var args = MakeDragAndDropArgs(dragPosition);
            return dragAndDropController.HandleDragAndDrop(args);
        }

        protected override void OnDrop(Vector3 pointerPosition)
        {
            var dragPosition = new DragPosition();
            if (!TryGetDragPosition(pointerPosition, ref dragPosition))
                return;

            var args = MakeDragAndDropArgs(dragPosition);
            if (dragAndDropController.HandleDragAndDrop(args) != DragVisualMode.Rejected)
                dragAndDropController.OnDrop(args);
        }

        protected void HandleDragAndScroll(Vector2 pointerPosition)
        {
            var scrollUp = pointerPosition.y < targetScrollView.worldBound.yMin + k_AutoScrollAreaSize;
            var scrollDown = pointerPosition.y > targetScrollView.worldBound.yMax - k_AutoScrollAreaSize;
            if (scrollUp || scrollDown)
            {
                targetScrollView.scrollOffset += (scrollUp ? Vector2.down : Vector2.up) * k_PanSpeed;
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
                m_DragHoverBar.style.width = targetListView.localBound.width;
                m_DragHoverBar.style.visibility = Visibility.Hidden;
                m_DragHoverBar.pickingMode = PickingMode.Ignore;

                targetListView.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    m_DragHoverBar.style.width = targetListView.localBound.width;
                });
                targetScrollView.contentViewport.Add(m_DragHoverBar);
            }

            ClearDragAndDropUI();
            m_LastDragPosition = dragPosition;
            switch (dragPosition.dragAndDropPosition)
            {
                case DragAndDropPosition.OverItem:
                    dragPosition.recycledItem.rootElement.AddToClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
                    break;
                case DragAndDropPosition.BetweenItems:
                    if (dragPosition.insertAtIndex == 0)
                        PlaceHoverBarAt(0);
                    else
                    {
                        var item = targetListView.GetRecycledItemFromIndex(dragPosition.insertAtIndex - 1);
                        // *begin-nonstandard-formatting*
                        item ??= targetListView.GetRecycledItemFromIndex(dragPosition.insertAtIndex);
                        // *end-nonstandard-formatting*
                        PlaceHoverBarAtElement(item.rootElement);
                    }

                    break;
                case DragAndDropPosition.OutsideItems:
                    var recycledItem = targetListView.GetRecycledItemFromIndex(targetListView.itemsSource.Count - 1);
                    if (recycledItem != null)
                        PlaceHoverBarAtElement(recycledItem.rootElement);
                    else if (targetListView.sourceIncludesArraySize && targetListView.itemsSource.Count > 0)
                        PlaceHoverBarAtElement(targetListView.GetRecycledItemFromIndex(0).rootElement);
                    else
                        PlaceHoverBarAt(0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dragPosition.dragAndDropPosition),
                        dragPosition.dragAndDropPosition,
                        $"Unsupported {nameof(dragPosition.dragAndDropPosition)} value");
            }
        }

        protected virtual bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem != null)
            {
                // Skip array size item
                if (targetListView.sourceIncludesArraySize && recycledItem.index == 0)
                {
                    dragPosition.insertAtIndex = recycledItem.index + 1;
                    dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                    return true;
                }

                //Below an item
                if (recycledItem.rootElement.worldBound.yMax - pointerPosition.y < k_BetweenElementsAreaSize)
                {
                    dragPosition.insertAtIndex = recycledItem.index + 1;
                    dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                    return true;
                }

                //Upon an item
                if (pointerPosition.y - recycledItem.rootElement.worldBound.yMin > k_BetweenElementsAreaSize)
                {
                    var scrollOffset = targetScrollView.scrollOffset;
                    targetScrollView.ScrollTo(recycledItem.rootElement);
                    if (scrollOffset != targetScrollView.scrollOffset)
                    {
                        return TryGetDragPosition(pointerPosition, ref dragPosition);
                    }

                    dragPosition.recycledItem = recycledItem;
                    dragPosition.insertAtIndex = recycledItem.index;
                    dragPosition.dragAndDropPosition = DragAndDropPosition.OverItem;
                    return true;
                }

                dragPosition.insertAtIndex = recycledItem.index;
                dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
                return true;
            }

            if (!targetListView.worldBound.Contains(pointerPosition))
                return false;

            dragPosition.dragAndDropPosition = DragAndDropPosition.OutsideItems;
            if (pointerPosition.y >= targetScrollView.contentContainer.worldBound.yMax)
                dragPosition.insertAtIndex = targetListView.itemsSource.Count;
            else
                dragPosition.insertAtIndex = 0;

            return true;
        }

        private ListDragAndDropArgs MakeDragAndDropArgs(DragPosition dragPosition)
        {
            object target = null;
            var recycledItem = dragPosition.recycledItem;
            if (recycledItem != null)
                target = targetListView.viewController.GetItemForIndex(recycledItem.index);

            return new ListDragAndDropArgs
            {
                target = target,
                insertAtIndex = dragPosition.insertAtIndex,
                dragAndDropPosition = dragPosition.dragAndDropPosition,
                dragAndDropData = useDragEvents ? DragAndDropUtility.dragAndDrop.data : dragAndDropClient.data,
            };
        }

        private void PlaceHoverBarAtElement(VisualElement element)
        {
            var contentViewport = targetScrollView.contentViewport;
            var elementBounds = contentViewport.WorldToLocal(element.worldBound);
            PlaceHoverBarAt(Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - k_DragHoverBarHeight));
        }

        private void PlaceHoverBarAt(float top)
        {
            m_DragHoverBar.style.top = top;
            m_DragHoverBar.style.visibility = Visibility.Visible;
        }

        protected override void ClearDragAndDropUI()
        {
            m_LastDragPosition = new DragPosition();
            foreach (var item in targetListView.activeItems)
            {
                item.rootElement.RemoveFromClassList(BaseVerticalCollectionView.itemDragHoverUssClassName);
            }

            if (m_DragHoverBar != null)
                m_DragHoverBar.style.visibility = Visibility.Hidden;
        }

        protected ReusableCollectionItem GetRecycledItem(Vector3 pointerPosition)
        {
            foreach (var recycledItem in targetListView.activeItems)
            {
                if (recycledItem.rootElement.worldBound.Contains(pointerPosition))
                    return recycledItem;
            }

            return null;
        }
    }

    internal static class ListViewDraggerExtension
    {
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
