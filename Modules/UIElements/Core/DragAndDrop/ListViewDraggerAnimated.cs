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
        StyleLength m_WidthBackup;
        StyleLength m_HeightBackup;

        ReusableCollectionItem m_Item;
        ReusableCollectionItem m_OffsetItem;
        public bool isDragging { get; private set; }
        public ReusableCollectionItem draggedItem => m_Item;

        protected override bool supportsDragEvents => false;

        public ListViewDraggerAnimated(BaseVerticalCollectionView listView)
            : base(listView) {}

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition, EventModifiers modifiers)
        {
            if (!enabled)
                return base.StartDrag(pointerPosition, modifiers);

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
            {
                return new StartDragArgs(string.Empty, DragVisualMode.Rejected, modifiers);
            }

            // Validate before mutating: a rejected drag (e.g. reorderable is false) must not create the
            // ghost placeholder or pull the row out of layout, or it would be left invisible (UUM-135762).
            var startDragArgs = dragAndDropController.SetupDragAndDrop(new[] { recycledItem.index }, true);
            startDragArgs.modifiers = modifiers;
            if (startDragArgs.visualMode is DragVisualMode.Rejected)
                return startDragArgs;

            targetView.ClearSelection();

            if (targetView.selectionType != SelectionType.None)
            {
                targetView.SetSelection(recycledItem.index);
            }

            isDragging = true;
            m_Item = recycledItem;

            targetView.virtualizationController.StartDragItem(m_Item);

            var y = m_Item.rootElement.layout.y;
            m_SelectionHeight = m_Item.rootElement.layout.height;

            m_WidthBackup = m_Item.rootElement.style.width;
            m_HeightBackup = m_Item.rootElement.style.height;
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

            return startDragArgs;
        }

        protected internal override void UpdateDrag(Vector3 pointerPosition, EventModifiers modifiers)
        {
            if (!enabled)
            {
                base.UpdateDrag(pointerPosition, modifiers);
                return;
            }

            if (m_Item == null)
                return;

            HandleDragAndScroll(pointerPosition);

            m_CurrentPointerPosition = pointerPosition;
            var positionInContainer = targetScrollView.contentContainer.WorldToLocal(m_CurrentPointerPosition);

            var contentHeight = targetScrollView.contentContainer.layout.height;

            var itemYPosition = Mathf.Clamp(positionInContainer.y - m_LocalOffsetOnStart, 0, contentHeight - m_SelectionHeight);

            var y = targetScrollView.contentContainer.resolvedStyle.paddingTop;
            m_CurrentIndex = -1;
            foreach (var item in targetView.activeItems)
            {
                if (item.index < 0 || (item.rootElement.style.display == DisplayStyle.None && !item.isDragGhost))
                    continue;

                if (item.index == m_Item.index && item.index < targetView.itemsSource.Count - 1)
                {
                    var nextExpectedHeight = targetView.virtualizationController.GetExpectedItemHeight(item.index + 1);
                    // If the drop position is at the end, we will let the conditional check that is outside the loop to handle the animating.
                    if (Mathf.Approximately(itemYPosition + nextExpectedHeight, contentHeight))
                        continue;

                    if (itemYPosition <= y + nextExpectedHeight * 0.5f)
                    {
                        m_CurrentIndex = item.index;
                    }

                    continue;
                }

                var expectedHeight = targetView.virtualizationController.GetExpectedItemHeight(item.index);
                if (itemYPosition <= y + expectedHeight * 0.5f)
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

            m_Item.rootElement.style.translate = new Translate(0, itemYPosition - m_Item.rootElement.layout.y);
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

        protected internal override void OnDrop(Vector3 pointerPosition, EventModifiers modifiers)
        {
            if (!enabled)
            {
                base.OnDrop(pointerPosition, modifiers);
                return;
            }

            if (m_Item == null)
                return;

            var droppedItem = m_Item;
            var dropIndex = m_CurrentIndex;

            RestoreDraggedItem(dropIndex);

            var dragPosition = new DragPosition
            {
                recycledItem = droppedItem,
                insertAtIndex = dropIndex,
                dropPosition = DragAndDropPosition.BetweenItems
            };

            var args = MakeDragAndDropArgs(dragPosition, modifiers);
            dragAndDropController.OnDrop(args);
            dragAndDrop.AcceptDrag();
        }

        // Pairs with StartDrag: restores the row's layout and releases the ghost placeholder. Must run
        // for every started drag, including rejected/cancelled ones, or the row stays invisible (UUM-135762).
        void RestoreDraggedItem(int dropIndex)
        {
            // Stop dragging first, to allow the list to refresh properly dragged items.
            isDragging = false;
            m_Item.rootElement.style.translate = StyleKeyword.Initial;
            m_Item.rootElement.style.paddingTop = 0;
            m_Item.rootElement.style.position = Position.Relative;
            m_Item.rootElement.style.top = 0;
            m_Item.rootElement.style.height = m_HeightBackup;
            m_Item.rootElement.style.width = m_WidthBackup;

            targetView.virtualizationController.EndDrag(dropIndex);

            if (m_OffsetItem != null)
            {
                m_OffsetItem.animator?.Stop();
                m_OffsetItem.animator?.Recycle();
                m_OffsetItem.animator = null;
                m_OffsetItem.rootElement.style.paddingTop = 0;
                if (targetView.virtualizationMethod == CollectionVirtualizationMethod.FixedHeight)
                    m_OffsetItem.rootElement.style.height = targetView.ResolveItemHeight();
            }

            m_Item = null;
            m_OffsetItem = null;
        }

        protected override void ClearDragAndDropUI(bool dragCancelled)
        {
            // A drag that started but never dropped must still release the ghost; OnDrop nulls m_Item, so a
            // normal drop won't re-enter here. Snap back to the start index since no reorder took place.
            if (dragCancelled && m_Item != null)
                RestoreDraggedItem(m_DragStartIndex);
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
