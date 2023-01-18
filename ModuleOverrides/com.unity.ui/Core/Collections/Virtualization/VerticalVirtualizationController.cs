// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    // TODO [GR] Could move some of that stuff to a base CollectionVirtualizationController<T> class (pool, active items, visible items, etc.)
    abstract class VerticalVirtualizationController<T> : CollectionVirtualizationController where T : ReusableCollectionItem, new()
    {
        readonly UnityEngine.Pool.ObjectPool<T> m_Pool = new (() => new T(), null, i => i.DetachElement());

        protected BaseVerticalCollectionView m_CollectionView;
        protected const int k_ExtraVisibleItems = 2;
        protected List<T> m_ActiveItems;
        protected T m_DraggedItem;

        public override IEnumerable<ReusableCollectionItem> activeItems => m_ActiveItems;

        int m_LastFocusedElementIndex = -1;
        List<int> m_LastFocusedElementTreeChildIndexes = new List<int>();

        protected readonly Func<T, bool> m_VisibleItemPredicateDelegate;

        internal int itemsCount =>
            m_CollectionView.sourceIncludesArraySize
                ? m_CollectionView.itemsSource.Count - 1
                : m_CollectionView.itemsSource.Count;

        protected virtual bool VisibleItemPredicate(T i)=> i.rootElement.style.display == DisplayStyle.Flex;

        internal T firstVisibleItem => m_ActiveItems.FirstOrDefault(m_VisibleItemPredicateDelegate);
        internal T lastVisibleItem => m_ActiveItems.LastOrDefault(m_VisibleItemPredicateDelegate);

        public override int visibleItemCount => m_ActiveItems.Count(m_VisibleItemPredicateDelegate);

        protected SerializedVirtualizationData serializedData => m_CollectionView.serializedVirtualizationData;

        public override int firstVisibleIndex
        {
            get => Mathf.Min(serializedData.firstVisibleIndex, m_CollectionView.viewController.GetItemsCount() - 1);
            protected set => serializedData.firstVisibleIndex = value;
        }

        // we keep this list in order to minimize temporary gc allocs
        protected List<T> m_ScrollInsertionList = new ();

        VisualElement m_EmptyRows;

        protected float lastHeight => m_CollectionView.lastHeight;

        protected VerticalVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView.scrollView)
        {
            m_CollectionView = collectionView;
            m_ActiveItems = new List<T>();
            m_VisibleItemPredicateDelegate = VisibleItemPredicate;

            // ScrollView sets this to true to support Absolute position. It causes issues with the scrollbars with animated reordering.
            // In the case of a collection view, we know our ReusableCollectionItems need to be in Relative anyway, so no need for it.
            m_ScrollView.contentContainer.disableClipping = false;
        }

        public override void Refresh(bool rebuild)
        {
            var hasValidBindings = m_CollectionView.HasValidDataAndBindings();

            for (var i = 0; i < m_ActiveItems.Count; i++)
            {
                var index = firstVisibleIndex + i;
                var recycledItem = m_ActiveItems[i];
                var isVisible = recycledItem.rootElement.style.display == DisplayStyle.Flex;

                if (rebuild)
                {
                    if (hasValidBindings)
                    {
                        m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                    }

                    m_CollectionView.viewController.InvokeDestroyItem(recycledItem);
                    m_Pool.Release(recycledItem);
                    continue;
                }

                if (m_CollectionView.itemsSource != null && index >= 0 && index < itemsCount)
                {
                    if (!hasValidBindings)
                        continue;

                    // Rebind visible items.
                    if (isVisible)
                    {
                        if (recycledItem.index != ReusableCollectionItem.UndefinedIndex)
                        {
                            m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                        }

                        recycledItem.index = ReusableCollectionItem.UndefinedIndex;
                        Setup(recycledItem, index);
                    }
                }
                else if (isVisible)
                {
                    ReleaseItem(i--);
                }
            }

            if (rebuild)
            {
                m_Pool.Clear();
                m_ActiveItems.Clear();
                m_ScrollView.Clear();
            }
        }

        protected void Setup(T recycledItem, int newIndex)
        {
            // We want to skip the item that is being reordered with the animated dragger.
            var wasGhostItem = recycledItem.isDragGhost;
            if (GetDraggedIndex() == newIndex)
            {
                if (recycledItem.index != ReusableCollectionItem.UndefinedIndex)
                    m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);

                recycledItem.isDragGhost = true;
                recycledItem.index = m_DraggedItem.index;
                recycledItem.rootElement.style.maxHeight = 0;
                recycledItem.rootElement.style.display = DisplayStyle.Flex;
                recycledItem.bindableElement.style.display = DisplayStyle.None;
                return;
            }

            // Restore the state of the item if it was hidden by a drag.
            if (wasGhostItem)
            {
                recycledItem.isDragGhost = false;
                recycledItem.rootElement.style.maxHeight = StyleKeyword.Null;
                recycledItem.bindableElement.style.display = DisplayStyle.Flex;
            }

            if (newIndex >= itemsCount)
            {
                recycledItem.rootElement.style.display = DisplayStyle.None;
                if (recycledItem.index >= 0 && recycledItem.index < itemsCount)
                {
                    m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                    recycledItem.index = ReusableCollectionItem.UndefinedIndex;
                }
                return;
            }

            recycledItem.rootElement.style.display = DisplayStyle.Flex;
            if (recycledItem.index == newIndex) return;

            var useAlternateUss = m_CollectionView.showAlternatingRowBackgrounds != AlternatingRowBackground.None && newIndex % 2 == 1;
            recycledItem.rootElement.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassName, useAlternateUss);

            var previousIndex = recycledItem.index;
            var newId = m_CollectionView.viewController.GetIdForIndex(newIndex);

            if (recycledItem.index != ReusableCollectionItem.UndefinedIndex)
                m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);

            recycledItem.index = newIndex;
            recycledItem.id = newId;

            var indexInParent = newIndex - firstVisibleIndex;
            if (indexInParent >= m_ScrollView.contentContainer.childCount)
            {
                recycledItem.rootElement.BringToFront();
            }
            else if (indexInParent >= 0)
            {
                recycledItem.rootElement.PlaceBehind(m_ScrollView.contentContainer[indexInParent]);
            }
            else
            {
                recycledItem.rootElement.SendToBack();
            }

            m_CollectionView.viewController.InvokeBindItem(recycledItem, newIndex);

            // Handle focus cycling
            HandleFocus(recycledItem, previousIndex);
        }

        public override void OnFocus(VisualElement leafTarget)
        {
            if (leafTarget == m_ScrollView.contentContainer)
                return;

            m_LastFocusedElementTreeChildIndexes.Clear();

            if (m_ScrollView.contentContainer.FindElementInTree(leafTarget, m_LastFocusedElementTreeChildIndexes))
            {
                var recycledElement = m_ScrollView.contentContainer[m_LastFocusedElementTreeChildIndexes[0]];
                foreach (var recycledItem in activeItems)
                {
                    if (recycledItem.rootElement == recycledElement)
                    {
                        m_LastFocusedElementIndex = recycledItem.index;
                        break;
                    }
                }

                m_LastFocusedElementTreeChildIndexes.RemoveAt(0);
            }
            else
            {
                m_LastFocusedElementIndex = -1;
            }
        }

        public override void OnBlur(VisualElement willFocus)
        {
            // Focus lost and the about-to-be-focused VisualElement is not part of the VerticalVirtualizationController.
            if (willFocus == null || willFocus != m_ScrollView.contentContainer)
            {
                m_LastFocusedElementTreeChildIndexes.Clear();
                m_LastFocusedElementIndex = -1;
            }
        }

        void HandleFocus(ReusableCollectionItem recycledItem, int previousIndex)
        {
            if (m_LastFocusedElementIndex == -1)
                return;

            if (m_LastFocusedElementIndex == recycledItem.index)
                recycledItem.rootElement.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Focus();
            else if (m_LastFocusedElementIndex != previousIndex)
                recycledItem.rootElement.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Blur();
            else
                m_ScrollView.contentContainer.Focus();
        }

        public override void UpdateBackground()
        {
            float backgroundFillHeight;
            if (m_CollectionView.showAlternatingRowBackgrounds != AlternatingRowBackground.All ||
                (backgroundFillHeight = m_ScrollView.contentViewport.resolvedStyle.height - GetExpectedContentHeight()) <= 0)
            {
                m_EmptyRows?.RemoveFromHierarchy();
                return;
            }

            if (lastVisibleItem == null)
                return;

            if (m_EmptyRows == null)
            {
                m_EmptyRows = new VisualElement()
                {
                    classList = {BaseVerticalCollectionView.backgroundFillUssClassName}
                };
            }

            if (m_EmptyRows.parent == null)
                m_ScrollView.contentViewport.Add(m_EmptyRows);

            var pixelAlignedItemHeight = GetExpectedItemHeight(-1);
            var itemCount = Mathf.FloorToInt(backgroundFillHeight / pixelAlignedItemHeight) + 1;

            if (itemCount > m_EmptyRows.childCount)
            {
                var itemsToAdd = itemCount - m_EmptyRows.childCount;
                for (var i = 0; i < itemsToAdd; i++)
                {
                    var row = new VisualElement();

                    //Inline style is used to prevent a user from changing an item flexShrink property.
                    row.style.flexShrink = 0;
                    m_EmptyRows.Add(row);
                }
            }

            var index = lastVisibleItem?.index ?? -1;

            var emptyRowCount = m_EmptyRows.hierarchy.childCount;
            for (var i = 0; i < emptyRowCount; ++i)
            {
                var child = m_EmptyRows.hierarchy[i];
                index++;
                child.style.height = pixelAlignedItemHeight;
                child.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassName, index % 2 == 1);
            }
        }

        internal override void StartDragItem(ReusableCollectionItem item)
        {
            m_DraggedItem = item as T;

            // Remove the active item from the list to prevent recycling it.
            var activeIndex = m_ActiveItems.IndexOf(m_DraggedItem);
            m_ActiveItems.RemoveAt(activeIndex);

            // Create a replacement item. Flag it as being dragged, so that we know it needs to stay hidden during item cycling.
            var replacementItem = GetOrMakeItemAtIndex(activeIndex, activeIndex);
            Setup(replacementItem, m_DraggedItem.index);
        }

        internal override void EndDrag(int dropIndex)
        {
            // Reinsert the dragged item.
            var item = m_CollectionView.GetRecycledItemFromIndex(dropIndex);
            var activeItemIndex = item != null ? m_ScrollView.IndexOf(item.rootElement) : m_ActiveItems.Count;
            m_ScrollView.Insert(activeItemIndex, m_DraggedItem.rootElement);
            m_ActiveItems.Insert(activeItemIndex, m_DraggedItem);

            // Release the ghost items.
            for (var i = 0; i < m_ActiveItems.Count; i++)
            {
                var activeItem = m_ActiveItems[i];
                if (activeItem.isDragGhost)
                {
                    // Clear index so that it doesn't get unbound since it was never bound, then restore and release it.
                    activeItem.index = ReusableCollectionItem.UndefinedIndex;
                    activeItem.bindableElement.style.display = DisplayStyle.Flex;
                    ReleaseItem(i);
                    i--;
                }
            }

            // We want to avoid releasing items in Refresh, that happens when an item is out of bounds and visible,
            // so we set the last one invisible and let the virtualization display it if necessary.
            if (dropIndex != m_DraggedItem.index)
            {
                if (lastVisibleItem != null)
                    lastVisibleItem.rootElement.style.display = DisplayStyle.None;

                // We unbind in order.
                if (m_DraggedItem.index < dropIndex)
                {
                    m_CollectionView.viewController.InvokeUnbindItem(m_DraggedItem, m_DraggedItem.index);
                    m_DraggedItem.index = ReusableCollectionItem.UndefinedIndex;
                }
                else if (item != null)
                {
                    m_CollectionView.viewController.InvokeUnbindItem(item, item.index);
                    item.index = ReusableCollectionItem.UndefinedIndex;
                }
            }

            m_DraggedItem = null;
        }

        internal virtual T GetOrMakeItemAtIndex(int activeItemIndex = -1, int scrollViewIndex = -1)
        {
            var item = m_Pool.Get();

            if (item.rootElement == null)
            {
                m_CollectionView.viewController.InvokeMakeItem(item);
            }

            item.PreAttachElement();

            if (activeItemIndex == -1)
            {
                m_ActiveItems.Add(item);
            }
            else
            {
                m_ActiveItems.Insert(activeItemIndex, item);
            }

            if (scrollViewIndex == -1)
            {
                m_ScrollView.Add(item.rootElement);
            }
            else
            {
                m_ScrollView.Insert(scrollViewIndex, item.rootElement);
            }

            return item;
        }

        internal virtual void ReleaseItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            if (item.index != ReusableCollectionItem.UndefinedIndex)
            {
                m_CollectionView.viewController.InvokeUnbindItem(item, item.index);
            }

            m_Pool.Release(item);
            m_ActiveItems.Remove(item);
        }

        protected int GetDraggedIndex()
        {
            if (m_CollectionView.dragger is ListViewDraggerAnimated { isDragging: true } dragger)
                return dragger.draggedItem.index;

            return ReusableCollectionItem.UndefinedIndex;
        }
    }
}
