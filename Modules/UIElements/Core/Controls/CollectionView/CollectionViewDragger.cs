// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal class CollectionViewDragger : DragEventsProcessor
    {
        internal struct DragPosition : IEquatable<DragPosition>
        {
            public int insertAtIndex;
            public RecycledItem recycledItem;
            public DragAndDropPosition dropPosition;

            public bool Equals(DragPosition other)
            {
                return insertAtIndex == other.insertAtIndex
                    && Equals(recycledItem, other.recycledItem)
                    && dropPosition == other.dropPosition;
            }

            public override bool Equals(object obj) => obj is DragPosition position && Equals(position);

            public override int GetHashCode() => HashCode.Combine(insertAtIndex, recycledItem, dropPosition);
        }

        const int k_AutoScrollAreaSize = 10;
        const int k_PanSpeed = 20;
        const int k_BetweenElementsAreaSize = 5;
        const int k_DragHoverBarHeight = 2;
        // Matches the MouseCursor.MoveArrow, but since it's Editor Only, we're creating this constant here.
        const int k_DefaultCursorId = 8;

        DragPosition m_LastDragPosition;
        VisualElement m_DragHoverBar;
        CollectionView targetView => m_Target as CollectionView;
        ScrollContainer targetScrollView => targetView.scrollView;
        // Some settings can disable reordering temporarily, like multi column sorting
        bool enabled { get; set; } = true;

        public ICollectionDragAndDropController dragAndDropController { get; set; }

        public CollectionViewDragger(CollectionView listView) : base(listView) {}

        protected override bool CanStartDrag(Vector3 pointerPosition, EventModifiers modifiers)
        {
            if (dragAndDropController == null)
                return false;

            if (!targetScrollView.contentContainer.worldBound.Contains(pointerPosition))
                return false;

            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem != null && targetView.HasCanStartDrag())
            {
                var ids = targetView.hasSelection ? GetCollectionViewSelectedIndices() : new[] { recycledItem.index };
                return targetView.RaiseCanStartDrag(recycledItem, ids, modifiers);
            }

            if (targetView.hasSelection)
            {
                return dragAndDropController.CanStartDrag(GetCollectionViewSelectedIndices());
            }

            return recycledItem != null && dragAndDropController.CanStartDrag(new[] { recycledItem.index });
        }

        protected internal override StartDragArgs StartDrag(Vector3 pointerPosition, EventModifiers modifiers)
        {
            var recycledItem = GetRecycledItem(pointerPosition);
            IEnumerable<int> indices;
            if (recycledItem != null)
            {
                if (targetView.selectionType == SelectionType.None)
                {
                    indices = new[] { recycledItem.index };
                }
                else
                {
                    if (!targetView.IsSelected(recycledItem.index))
                    {
                        targetView.SetSelection(recycledItem.index);
                    }

                    indices = GetCollectionViewSelectedIndices();
                }
            }
            else
            {
                indices = GetCollectionViewSelectedIndices();
            }

            var startDragArgs = dragAndDropController.SetupDragAndDrop(indices);
            startDragArgs.modifiers = modifiers;

            // User defined handling, if any.
            if (recycledItem != null)
                startDragArgs = targetView.RaiseSetupDragAndDrop(recycledItem, dragAndDropController.GetSortedSelectedIds(), startDragArgs);

            startDragArgs.SetGenericData(DragAndDropData.dragSourceKey, targetView);
            return startDragArgs;
        }

        protected internal override void UpdateDrag(Vector3 pointerPosition, EventModifiers modifiers)
        {
            var dragPosition = new DragPosition();
            var visualMode = GetVisualMode(pointerPosition, modifiers, ref dragPosition);

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

        protected internal override void OnDrop(Vector3 pointerPosition, EventModifiers modifiers)
        {
            var dragPosition = new DragPosition();
            if (!TryGetDragPosition(pointerPosition, ref dragPosition))
                return;

            var args = MakeDragAndDropArgs(dragPosition, modifiers);

            // User defined drop handling.
            var mode = targetView.RaiseDrop(pointerPosition, args);
            if (mode != DragVisualMode.None)
            {
                if (mode != DragVisualMode.Rejected)
                    dragAndDrop.AcceptDrag();
                else
                    dragAndDropController.DragCleanup();

                return;
            }

            if (IsDraggingDisabled())
                return;

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

        protected override void ClearDragAndDropUI(bool dragCancelled)
        {
            if (dragCancelled)
            {
                dragAndDropController.DragCleanup();
            }

            targetView.elementPanel.cursorManager.ResetCursor();

            m_LastDragPosition = new DragPosition();
            foreach (var recycledItem in targetView.m_IndexToItemDictionary.Values)
            {
                recycledItem.element.RemoveFromClassList(BaseVerticalCollectionView.itemDragHoverUssClassNameUnique);
            }

            if (m_DragHoverBar != null)
                m_DragHoverBar.style.visibility = Visibility.Hidden;
        }

        // Made internal for testing
        internal void HandleDragAndScroll(Vector2 pointerPosition)
        {
            double offset = 0;
            // Scroll up
            if (pointerPosition.y < targetScrollView.worldBound.yMin + k_AutoScrollAreaSize)
            {
                offset = targetScrollView.verticalScroller.value + -k_PanSpeed;
                if (offset <= targetScrollView.worldBound.yMin)
                    offset = 0;
            }
            // Scroll down
            else if (pointerPosition.y > targetScrollView.worldBound.yMax - k_AutoScrollAreaSize)
            {
                var rangeEstimate = targetView.fixedItemHeight * targetView.itemsSource.Count;
                var containerHeight = targetScrollView.contentContainer.resolvedStyle.height;
                if (rangeEstimate > containerHeight)
                {
                    var oldOffset = targetScrollView.verticalScroller.value;
                    offset = oldOffset + k_PanSpeed > rangeEstimate - containerHeight ? oldOffset : oldOffset + k_PanSpeed;
                }
            }
            else
            {
                return;
            }

            targetView.UpdateVerticalScrollValue(offset);
        }

        DragVisualMode GetVisualMode(Vector3 pointerPosition, EventModifiers modifiers, ref DragPosition dragPosition)
        {
            if (dragAndDropController == null || !dragAndDropController.CanDrop())
            {
                return DragVisualMode.Rejected;
            }

            var foundPosition = TryGetDragPosition(pointerPosition, ref dragPosition);
            var args = MakeDragAndDropArgs(dragPosition, modifiers);

            // User defined handling, if any.
            var mode = targetView.RaiseHandleDragAndDrop(pointerPosition, args);
            if (mode != DragVisualMode.None)
                return mode;

            return foundPosition ? dragAndDropController.HandleDragAndDrop(args) : DragVisualMode.Rejected;
        }

        VisualElement CreateDragHoverBar()
        {
            var dragHoverBar = new VisualElement { pickingMode = PickingMode.Ignore, style = { width = targetView.localBound.width, visibility = Visibility.Hidden } };
            dragHoverBar.AddToClassList(BaseVerticalCollectionView.dragHoverBarUssClassNameUnique);
            targetView.RegisterCallback<GeometryChangedEvent>(_ => m_DragHoverBar.style.width = targetView.localBound.width);
            return dragHoverBar;
        }

        void ApplyDragAndDropUI(DragPosition dragPosition)
        {
            if (m_LastDragPosition.Equals(dragPosition) || IsDraggingDisabled())
                return;

            m_DragHoverBar ??= CreateDragHoverBar();
            targetScrollView.viewport.Add(m_DragHoverBar);

            ClearDragAndDropUI(false);
            m_LastDragPosition = dragPosition;
            switch (dragPosition.dropPosition)
            {
                case DragAndDropPosition.OverItem:
                    dragPosition.recycledItem.element.AddToClassList(BaseVerticalCollectionView.itemDragHoverUssClassNameUnique);
                    break;
                case DragAndDropPosition.BetweenItems:
                    if (dragPosition.insertAtIndex == 0)
                    {
                        PlaceHoverBarAt(0);
                    }
                    else
                    {
                        var beforeItem = targetView.GetRootElementForIndex(dragPosition.insertAtIndex - 1);
                        PlaceHoverBarAtElement(beforeItem ?? targetView.GetRootElementForIndex(dragPosition.insertAtIndex));
                    }

                    break;
                case DragAndDropPosition.OutsideItems:
                    var recycledItem = targetView.GetRootElementForIndex(targetView.itemsSource.Count - 1);
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
                targetView.elementPanel.cursorManager.SetCursor(new UIElements.Cursor { defaultCursorId = k_DefaultCursorId });
            }
        }

        bool TryGetDragPosition(Vector2 pointerPosition, ref DragPosition dragPosition)
        {
            var recycledItem = GetRecycledItem(pointerPosition);
            if (recycledItem == null)
            {
                if (!targetView.worldBound.Contains(pointerPosition))
                    return false;

                dragPosition.dropPosition = DragAndDropPosition.OutsideItems;
                if (pointerPosition.y <= targetScrollView.contentContainer.worldBound.yMin)
                {
                    // When a sticky row is active, "above everything" means before the stuck item, not index 0
                    if (targetView.hasActiveStickyRow)
                        dragPosition.insertAtIndex = targetView.currentStickyRow.index;
                    else
                        dragPosition.insertAtIndex = 0;
                }
                else
                    dragPosition.insertAtIndex = targetView.itemsSource.Count;

                return true;
            }

            // Below an item
            if (recycledItem.element.worldBound.yMax - pointerPosition.y < k_BetweenElementsAreaSize)
            {
                dragPosition.dropPosition = DragAndDropPosition.BetweenItems;

                if (targetView.hasActiveStickyRow && ReferenceEquals(recycledItem, targetView.currentStickyRow))
                    dragPosition.insertAtIndex = (int)(targetView.scrollValue / targetView.fixedItemHeight);
                else
                    dragPosition.insertAtIndex = recycledItem.index + 1;
            }
            // Upon an item
            else if (pointerPosition.y - recycledItem.element.worldBound.yMin > k_BetweenElementsAreaSize)
            {
                if (!ReferenceEquals(recycledItem, targetView.currentStickyRow))
                {
                    var scrollOffset = targetScrollView.containerOffset;
                    targetView.ScrollToItem(recycledItem.index);
                    if (!Mathf.Approximately(scrollOffset.x, targetScrollView.containerOffset.x) || !Mathf.Approximately(scrollOffset.y, targetScrollView.containerOffset.y))
                    {
                        return TryGetDragPosition(pointerPosition, ref dragPosition);
                    }
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

            return true;
        }

        DragAndDropArgs MakeDragAndDropArgs(DragPosition dragPosition, EventModifiers modifiers)
        {
            object target = null;
            var recycledItem = dragPosition.recycledItem;
            if (recycledItem != null)
                target = targetView.itemsSource[recycledItem.index];

            return new DragAndDropArgs
            {
                target = target,
                insertAtIndex = dragPosition.insertAtIndex,
                dragAndDropPosition = dragPosition.dropPosition,
                dragAndDropData = DragAndDropUtility.GetDragAndDrop(m_Target.panel).data,
                modifiers = modifiers
            };
        }

        float GetHoverBarTopPosition(VisualElement item)
        {
            var contentViewport = targetScrollView.viewport;
            var elementBounds = contentViewport.WorldToLocal(item.worldBound);
            var top = Mathf.Min(elementBounds.yMax, contentViewport.localBound.yMax - k_DragHoverBarHeight);
            return top;
        }

        void PlaceHoverBarAtElement(VisualElement item)
        {
            // If this item is partially or fully behind the sticky row, clamp to the sticky row's bottom edge
            if (targetView.hasActiveStickyRow)
            {
                var stickyBounds = targetScrollView.viewport.WorldToLocal(targetView.currentStickyRow.element.worldBound);
                var itemBounds = targetScrollView.viewport.WorldToLocal(item.worldBound);
                if (itemBounds.yMax <= stickyBounds.yMax)
                {
                    PlaceHoverBarAt(stickyBounds.yMax);
                    return;
                }
            }

            PlaceHoverBarAt(GetHoverBarTopPosition(item));
        }

        void PlaceHoverBarAt(float top)
        {
            m_DragHoverBar.style.top = top;
            m_DragHoverBar.style.visibility = Visibility.Visible;
            m_DragHoverBar.style.marginLeft = 0;
            m_DragHoverBar.style.width = targetView.localBound.width;
        }

        RecycledItem GetRecycledItem(Vector3 pointerPosition)
        {
            if (targetView.hasActiveStickyRow && targetView.currentStickyRow.element.worldBound.Contains(pointerPosition))
                return targetView.currentStickyRow;

            foreach (var recycledItem in targetView.m_IndexToItemDictionary.Values)
            {
                if (recycledItem.element.worldBound.Contains(pointerPosition))
                    return recycledItem;
            }

            return null;
        }

        bool IsDraggingDisabled()
        {
            // If dragging within the same view and reordering is not enabled, otherwise allow drag and drop between views
            return targetView == dragAndDrop.data.source && !enabled;
        }

        int[] GetCollectionViewSelectedIndices()
        {
            if (targetView.selection is CollectionViewSelection collectionViewSelection)
            {
                return collectionViewSelection.indices.ToArray();
            }

            // This works for HierarchyViewSelection, because we have our own implementation for drag and drop workflow.
            return Array.Empty<int>();
        }
    }
}
