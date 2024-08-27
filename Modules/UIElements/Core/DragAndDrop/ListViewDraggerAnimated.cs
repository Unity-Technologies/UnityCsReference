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

        protected override bool supportsDragEvents => false;

        public ListViewDraggerAnimated(BaseVerticalCollectionView listView)
            : base(listView) {}

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition)
        {
            if (!enabled)
                return base.StartDrag(pointerPosition);

            targetView.ClearSelection();

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
            {
                return new StartDragArgs(string.Empty, DragVisualMode.Rejected);
            }

            targetView.SetSelection(recycledItem.index);

            isDragging = true;
            m_Item = recycledItem;

            targetView.virtualizationController.StartDragItem(m_Item);

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

            var item = targetView.GetRecycledItemFromIndex(m_CurrentIndex + 1);
            if (item != null)
            {
                m_OffsetItem = item;

                // We must set the animator even if we set the values directly afterwards, so that it is treated the same way in other callbacks.
                Animate(m_OffsetItem, m_SelectionHeight);

                m_OffsetItem.rootElement.style.paddingTop = m_SelectionHeight;
                if (targetView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight)
                    m_OffsetItem.rootElement.style.height = targetView.fixedItemHeight + m_SelectionHeight;
            }

            return dragAndDropController.SetupDragAndDrop(new[] { m_Item.index }, true);
        }

        protected internal override void UpdateDrag(Vector3 pointerPosition)
        {
            if (!enabled)
            {
                base.UpdateDrag(pointerPosition);
                return;
            }

            if (m_Item == null)
                return;

            HandleDragAndScroll(pointerPosition);

            m_CurrentPointerPosition = pointerPosition;
            var positionInContainer = targetScrollView.contentContainer.WorldToLocal(m_CurrentPointerPosition);

            var itemLayout = m_Item.rootElement.layout;
            var contentHeight = targetScrollView.contentContainer.layout.height;
            itemLayout.y = Mathf.Clamp(positionInContainer.y - m_LocalOffsetOnStart, 0, contentHeight - m_SelectionHeight);

            var y = targetScrollView.contentContainer.resolvedStyle.paddingTop;
            m_CurrentIndex = -1;
            foreach (var item in targetView.activeItems)
            {
                if (item.index < 0 || (item.rootElement.style.display == DisplayStyle.None && !item.isDragGhost))
                    continue;

                if (item.index == m_Item.index && item.index < targetView.itemsSource.Count - 1)
                {
                    var nextExpectedHeight = targetView.virtualizationController.GetExpectedItemHeight(item.index + 1);
                    if (itemLayout.y <= y + nextExpectedHeight * 0.5f)
                    {
                        m_CurrentIndex = item.index;
                    }

                    continue;
                }

                var expectedHeight = targetView.virtualizationController.GetExpectedItemHeight(item.index);
                if (itemLayout.y <= y + expectedHeight * 0.5f)
                {
                    if (m_CurrentIndex == -1)
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
                m_CurrentIndex = targetView.itemsSource.Count;
                Animate(m_OffsetItem, 0);
                m_OffsetItem = null;
            }

            m_Item.rootElement.layout = itemLayout;
            m_Item.rootElement.BringToFront();
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
            var targetStyle = targetView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight ? new StyleValues { paddingTop = paddingTop, height = targetView.ResolveItemHeight() + paddingTop } : new StyleValues { paddingTop = paddingTop };
            element.animator = element.rootElement.experimental.animation.Start(targetStyle, 500);
            element.animator.KeepAlive();
        }

        protected internal override void OnDrop(Vector3 pointerPosition)
        {
            if (!enabled)
            {
                base.OnDrop(pointerPosition);
                return;
            }

            if (m_Item == null)
                return;

            // Stop dragging first, to allow the list to refresh properly dragged items.
            isDragging = false;

            m_Item.rootElement.ClearManualLayout();

            targetView.virtualizationController.EndDrag(m_CurrentIndex);

            if (m_OffsetItem != null)
            {
                m_OffsetItem.animator?.Stop();
                m_OffsetItem.animator?.Recycle();
                m_OffsetItem.animator = null;
                m_OffsetItem.rootElement.style.paddingTop = 0;
                if (targetView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight)
                    m_OffsetItem.rootElement.style.height = targetView.ResolveItemHeight();
            }

            var dragPosition = new DragPosition
            {
                recycledItem = m_Item,
                insertAtIndex = m_CurrentIndex,
                dropPosition = DragAndDropPosition.BetweenItems
            };

            var args = MakeDragAndDropArgs(dragPosition);
            dragAndDropController.OnDrop(args);
            dragAndDrop.AcceptDrag();

            m_Item = null;
            m_OffsetItem = null;
        }

        protected override void ClearDragAndDropUI(bool dragCancelled)
        {
            // Nothing to clear.
        }

        protected override bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            dragPosition.recycledItem = m_Item;
            dragPosition.insertAtIndex = m_CurrentIndex;
            dragPosition.dropPosition = DragAndDropPosition.BetweenItems;
            return true;
        }
    }
}
