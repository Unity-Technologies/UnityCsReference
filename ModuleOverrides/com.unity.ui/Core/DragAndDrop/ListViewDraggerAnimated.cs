// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    internal class ListViewDraggerAnimated : ListViewDragger
    {
        int m_DragStartIndex;
        int m_CurrentIndex;
        float m_SelectionHeight;
        float m_LocalOffsetOnStart;
        Vector3 m_CurrentPointerPosition;
        ReusableCollectionItem m_Item;
        ReusableCollectionItem m_OffsetItem;
        public bool isDragging { get; private set; }
        public ReusableCollectionItem draggedItem => m_Item;

        internal override bool supportsDragEvents => false;

        public ListViewDraggerAnimated(BaseVerticalCollectionView listView)
            : base(listView) {}

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition)
        {
            targetListView.ClearSelection();

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
                return default;

            targetListView.SetSelection(recycledItem.index);

            isDragging = true;
            m_Item = recycledItem;
            targetListView.virtualizationController.StartDragItem(m_Item);

            var y = m_Item.rootElement.layout.y;
            m_SelectionHeight = m_Item.rootElement.layout.height;

            m_Item.rootElement.style.position = Position.Absolute;
            m_Item.rootElement.style.height = m_Item.rootElement.layout.height;
            m_Item.rootElement.style.width = m_Item.rootElement.layout.width;
            m_Item.rootElement.style.top = y;

            m_DragStartIndex = m_Item.index;
            m_CurrentIndex = m_DragStartIndex;
            m_CurrentPointerPosition = pointerPosition;
            m_LocalOffsetOnStart = targetScrollView.contentContainer.WorldToLocal(pointerPosition).y - y;

            var item = targetListView.GetRecycledItemFromIndex(m_CurrentIndex + 1);
            if (item != null)
            {
                m_OffsetItem = item;

                // We must set the animator even if we set the values directly afterwards, so that it is treated the same way in other callbacks.
                Animate(m_OffsetItem, m_SelectionHeight);

                m_OffsetItem.rootElement.style.paddingTop = m_SelectionHeight;
                if (targetListView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight)
                    m_OffsetItem.rootElement.style.height = targetListView.fixedItemHeight + m_SelectionHeight;
            }

            return dragAndDropController.SetupDragAndDrop(new[] { m_Item.index }, true);
        }

        protected internal override DragVisualMode UpdateDrag(Vector3 pointerPosition)
        {
            if (m_Item == null)
                return DragVisualMode.Rejected;

            HandleDragAndScroll(pointerPosition);

            m_CurrentPointerPosition = pointerPosition;
            var positionInContainer = targetScrollView.contentContainer.WorldToLocal(m_CurrentPointerPosition);

            var itemLayout = m_Item.rootElement.layout;
            var contentHeight = targetScrollView.contentContainer.layout.height;
            itemLayout.y = Mathf.Clamp(positionInContainer.y - m_LocalOffsetOnStart, 0, contentHeight - m_SelectionHeight);

            var y = targetScrollView.contentContainer.resolvedStyle.paddingTop;
            m_CurrentIndex = -1;
            foreach (var item in targetListView.activeItems)
            {
                if (item.index < 0 || (item.rootElement.style.display == DisplayStyle.None && !item.isDragGhost))
                    continue;

                if (item.index == m_Item.index)
                {
                    continue;
                }

                var expectedHeight = targetListView.virtualizationController.GetExpectedItemHeight(item.index);
                var shouldSkip = targetListView.sourceIncludesArraySize && item.index == 0;
                if (!shouldSkip && m_CurrentIndex == -1 && itemLayout.y <= y + expectedHeight * 0.5f)
                {
                    m_CurrentIndex = item.index;

                    if (m_OffsetItem == item)
                        break;

                    Animate(m_OffsetItem, 0);
                    Animate(item, m_SelectionHeight);
                    m_OffsetItem = item;
                    break;
                }

                y += expectedHeight;
            }

            if (m_CurrentIndex == -1)
            {
                m_CurrentIndex = targetListView.itemsSource.Count;
                Animate(m_OffsetItem, 0);
                m_OffsetItem = null;
            }

            m_Item.rootElement.layout = itemLayout;
            m_Item.rootElement.BringToFront();

            return DragVisualMode.Move;
        }

        void Animate(ReusableCollectionItem element, float paddingTop)
        {
            if (element == null)
                return;

            if (element.animator != null)
            {
                if ((element.animator.isRunning && element.animator.to.paddingTop == paddingTop) ||
                    (!element.animator.isRunning && element.rootElement.style.paddingTop == paddingTop))
                    return;
            }

            element.animator?.Stop();
            element.animator?.Recycle();
            var targetStyle = targetListView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight ? new StyleValues { paddingTop = paddingTop, height = targetListView.ResolveItemHeight() + paddingTop } : new StyleValues { paddingTop = paddingTop };
            element.animator = element.rootElement.experimental.animation.Start(targetStyle, 500);
            element.animator.KeepAlive();
        }

        protected internal override void OnDrop(Vector3 pointerPosition)
        {
            // Stop dragging first, to allow the list to refresh properly dragged items.
            isDragging = false;
            m_Item.rootElement.ClearManualLayout();
            targetListView.virtualizationController.EndDrag(m_CurrentIndex);

            if (m_OffsetItem != null)
            {
                m_OffsetItem.animator?.Stop();
                m_OffsetItem.animator?.Recycle();
                m_OffsetItem.animator = null;
                m_OffsetItem.rootElement.style.paddingTop = 0;
                if (targetListView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight)
                    m_OffsetItem.rootElement.style.height = targetListView.ResolveItemHeight();
            }

            base.OnDrop(pointerPosition);

            m_Item = null;
            m_OffsetItem = null;
        }

        protected override void ClearDragAndDropUI()
        {
            // Nothing to clear.
        }

        protected override bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            dragPosition.recycledItem = m_Item;
            dragPosition.insertAtIndex = m_CurrentIndex;
            dragPosition.dragAndDropPosition = DragAndDropPosition.BetweenItems;
            return true;
        }
    }
}
