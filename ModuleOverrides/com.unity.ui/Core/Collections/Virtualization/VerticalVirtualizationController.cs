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
        protected BaseVerticalCollectionView m_CollectionView;
        protected const int k_ExtraVisibleItems = 2;

        protected readonly UnityEngine.Pool.ObjectPool<T> m_Pool = new UnityEngine.Pool.ObjectPool<T>(() => new T(), null, i => i.DetachElement());
        protected List<T> m_ActiveItems;

        public override IEnumerable<ReusableCollectionItem> activeItems => m_ActiveItems as IEnumerable<ReusableCollectionItem>;

        int m_LastFocusedElementIndex = -1;
        List<int> m_LastFocusedElementTreeChildIndexes = new List<int>();

        protected int m_FirstVisibleIndex;

        Func<T, bool> m_VisibleItemPredicateDelegate;

        protected virtual bool VisibleItemPredicate(T i)
        {
            var isBeingDragged = false;
            if (m_CollectionView.dragger is ListViewDraggerAnimated dragger)
                isBeingDragged = dragger.isDragging && i.index == dragger.draggedItem.index;

            return i.rootElement.style.display == DisplayStyle.Flex && !isBeingDragged;
        }

        internal T firstVisibleItem => m_ActiveItems.FirstOrDefault(m_VisibleItemPredicateDelegate);
        internal T lastVisibleItem => m_ActiveItems.LastOrDefault(m_VisibleItemPredicateDelegate);

        public override int visibleItemCount => m_ActiveItems.Count(m_VisibleItemPredicateDelegate);
        public override int firstVisibleIndex => m_FirstVisibleIndex;
        public override int lastVisibleIndex => lastVisibleItem?.index ?? -1;

        // we keep this list in order to minimize temporary gc allocs
        protected List<T> m_ScrollInsertionList = new List<T>();

        readonly VisualElement k_EmptyRows;

        protected float lastHeight => m_CollectionView.lastHeight;

        protected VerticalVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView.scrollView)
        {
            m_CollectionView = collectionView;
            m_ActiveItems = new List<T>();
            m_VisibleItemPredicateDelegate = VisibleItemPredicate;

            k_EmptyRows = new VisualElement();
            k_EmptyRows.AddToClassList(BaseVerticalCollectionView.backgroundFillUssClassName);
        }

        public override void Refresh(bool rebuild)
        {
            var hasValidBindings = m_CollectionView.HasValidDataAndBindings();

            for (var i = 0; i < m_ActiveItems.Count; i++)
            {
                var index = m_FirstVisibleIndex + i;
                var recycledItem = m_ActiveItems[i];
                var isVisible = recycledItem.rootElement.style.display == DisplayStyle.Flex;

                if (rebuild)
                {
                    if (hasValidBindings && isVisible)
                    {
                        m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                        m_CollectionView.viewController.InvokeDestroyItem(recycledItem);
                    }

                    m_Pool.Release(recycledItem);
                    continue;
                }

                if (m_CollectionView.itemsSource != null && index >= 0 && index < m_CollectionView.itemsSource.Count)
                {
                    if (hasValidBindings && isVisible)
                    {
                        m_CollectionView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
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
            if (m_CollectionView.dragger is ListViewDraggerAnimated dragger)
                if (dragger.isDragging && (dragger.draggedItem.index == newIndex || dragger.draggedItem == recycledItem))
                    return;

            if (newIndex >= m_CollectionView.itemsSource.Count)
            {
                recycledItem.rootElement.style.display = DisplayStyle.None;
                if (recycledItem.index >= 0 && recycledItem.index < m_CollectionView.itemsSource.Count)
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

            var indexInParent = newIndex - m_FirstVisibleIndex;
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
            var currentFillHeight = float.IsNaN(k_EmptyRows.layout.size.y) ? 0 : k_EmptyRows.layout.size.y;
            var backgroundFillHeight = m_ScrollView.contentViewport.layout.size.y - m_ScrollView.contentContainer.layout.size.y - currentFillHeight;
            if (m_CollectionView.showAlternatingRowBackgrounds != AlternatingRowBackground.All || backgroundFillHeight <= 0)
            {
                k_EmptyRows.RemoveFromHierarchy();
                return;
            }

            if (lastVisibleItem == null)
                return;

            if (k_EmptyRows.parent == null)
                m_ScrollView.contentViewport.Add(k_EmptyRows);

            var pixelAlignedItemHeight = GetItemHeight(-1);
            var itemsCount = Mathf.FloorToInt(backgroundFillHeight / pixelAlignedItemHeight) + 1;
            if (itemsCount > k_EmptyRows.childCount)
            {
                var itemsToAdd = itemsCount - k_EmptyRows.childCount;
                for (var i = 0; i < itemsToAdd; i++)
                {
                    var row = new VisualElement();

                    //Inline style is used to prevent a user from changing an item flexShrink property.
                    row.style.flexShrink = 0;
                    k_EmptyRows.Add(row);
                }
            }

            var index = lastVisibleIndex;

            int emptyRowCount = k_EmptyRows.hierarchy.childCount;
            for (int i = 0; i < emptyRowCount; ++i)
            {
                var child = k_EmptyRows.hierarchy[i];
                index++;
                child.style.height = pixelAlignedItemHeight;
                child.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassName, index % 2 == 1);
            }
        }

        public override void ReplaceActiveItem(int index)
        {
            var i = 0;
            foreach (var item in m_ActiveItems)
            {
                if (item.index == index)
                {
                    var recycledItem = GetOrMakeItem();

                    // Detach the old one
                    item.DetachElement();
                    m_ActiveItems.Remove(item);
                    m_CollectionView.viewController.InvokeUnbindItem(item, index);
                    m_CollectionView.viewController.InvokeDestroyItem(item);

                    // Attach and setup new one.
                    m_ActiveItems.Insert(i, recycledItem);
                    m_ScrollView.Add(recycledItem.rootElement);
                    Setup(recycledItem, index);
                    break;
                }

                i++;
            }
        }

        internal virtual T GetOrMakeItem()
        {
            var item = m_Pool.Get();

            if (item.rootElement == null)
            {
                m_CollectionView.viewController.InvokeMakeItem(item);
            }

            item.PreAttachElement();

            return item;
        }

        internal virtual void ReleaseItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            var index = item.index;

            if (index >= 0 && index < m_CollectionView.itemsSource.Count)
            {
                m_CollectionView.viewController.InvokeUnbindItem(item, index);
            }

            m_Pool.Release(item);
            m_ActiveItems.RemoveAt(activeItemsIndex);
        }
    }
}
