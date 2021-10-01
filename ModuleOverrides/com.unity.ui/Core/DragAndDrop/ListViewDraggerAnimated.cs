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
        public bool isDragging => m_Item != null;
        public ReusableCollectionItem draggedItem => m_Item;

        internal override bool supportsDragEvents => false;

        public ListViewDraggerAnimated(BaseVerticalCollectionView listView)
            : base(listView) {}

        protected override StartDragArgs StartDrag(Vector3 pointerPosition)
        {
            targetListView.ClearSelection();

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
                return default;

            targetListView.SetSelection(recycledItem.index);

            m_Item = recycledItem;
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

        protected override DragVisualMode UpdateDrag(Vector3 pointerPosition)
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
                if (item.rootElement.style.display == DisplayStyle.None)
                    continue;

                var child = item;
                if (child == m_Item)
                {
                    continue;
                }

                var trueHeight = child.rootElement.layout.height - child.rootElement.resolvedStyle.paddingTop;

                var shouldSkip = targetListView.sourceIncludesArraySize && child.index == 0;
                if (!shouldSkip && m_CurrentIndex == -1 && itemLayout.y <= y + trueHeight * 0.5f)
                {
                    m_CurrentIndex = child.index;

                    if (m_OffsetItem == child)
                        break;

                    Animate(m_OffsetItem, 0);
                    Animate(child, m_SelectionHeight);
                    m_OffsetItem = child;
                    break;
                }

                y += trueHeight;
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

        protected override void OnDrop(Vector3 pointerPosition)
        {
            if (m_Item != null && targetListView.binding == null)
            {
                targetListView.virtualizationController.ReplaceActiveItem(m_Item.index);
            }

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

            if (m_Item != null && targetListView.binding != null)
            {
                targetListView.virtualizationController.ReplaceActiveItem(m_Item.index);
            }

            m_OffsetItem = null;
            m_Item = null;
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
