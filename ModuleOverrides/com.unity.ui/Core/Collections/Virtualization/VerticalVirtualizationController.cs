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
        protected BaseVerticalCollectionView m_ListView;
        protected const int k_ExtraVisibleItems = 2;

        protected readonly UnityEngine.Pool.ObjectPool<T> m_Pool = new UnityEngine.Pool.ObjectPool<T>(() => new T(), null, i => i.DetachElement());
        protected List<T> m_ActiveItems;

        public override IEnumerable<ReusableCollectionItem> activeItems => m_ActiveItems as IEnumerable<ReusableCollectionItem>;

        protected int m_FirstVisibleIndex;

        Func<T, bool> m_VisibleItemPredicateDelegate;

        bool VisibleItemPredicate(T i)
        {
            var isBeingDragged = false;
            if (m_ListView.dragger is ListViewDraggerAnimated dragger)
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

        protected float lastHeight => m_ListView.lastHeight;

        protected VerticalVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView.scrollView)
        {
            m_ListView = collectionView;
            m_ActiveItems = new List<T>();
            m_VisibleItemPredicateDelegate = VisibleItemPredicate;

            k_EmptyRows = new VisualElement();
            k_EmptyRows.AddToClassList(BaseVerticalCollectionView.backgroundFillUssClassName);
        }

        public override void Refresh(bool rebuild)
        {
            var hasValidBindings = m_ListView.HasValidDataAndBindings();

            for (var i = 0; i < m_ActiveItems.Count; i++)
            {
                var index = m_FirstVisibleIndex + i;
                var recycledItem = m_ActiveItems[i];
                var isVisible = recycledItem.rootElement.style.display == DisplayStyle.Flex;

                if (rebuild)
                {
                    if (hasValidBindings && isVisible)
                    {
                        m_ListView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                        m_ListView.viewController.InvokeDestroyItem(recycledItem);
                    }

                    m_Pool.Release(recycledItem);
                    continue;
                }

                if (index >= 0 && index < m_ListView.itemsSource.Count)
                {
                    if (hasValidBindings && isVisible)
                    {
                        recycledItem.index = ReusableCollectionItem.UndefinedIndex;
                        Setup(recycledItem, index);
                    }
                }
                else if (isVisible)
                {
                    m_Pool.Release(recycledItem);
                    m_ActiveItems.RemoveAt(i--);
                }
            }

            if (rebuild)
            {
                m_Pool.Clear();
                m_ActiveItems.Clear();
                m_ScrollView.Clear();
            }
        }

        protected void Setup(T recycledItem, int newIndex, bool forceHide = false)
        {
            // We want to skip the item that is being reordered with the animated dragger.
            if (m_ListView.dragger is ListViewDraggerAnimated dragger)
                if (dragger.isDragging && (dragger.draggedItem.index == newIndex || dragger.draggedItem == recycledItem))
                    return;

            if (newIndex >= m_ListView.itemsSource.Count || forceHide)
            {
                recycledItem.rootElement.style.display = DisplayStyle.None;
                if (recycledItem.index >= 0 && recycledItem.index < m_ListView.itemsSource.Count)
                {
                    m_ListView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);
                }
                return;
            }

            var newId = m_ListView.viewController.GetIdForIndex(newIndex);
            recycledItem.rootElement.style.display = DisplayStyle.Flex;
            if (recycledItem.index == newIndex) return;

            var useAlternateUss = m_ListView.showAlternatingRowBackgrounds != AlternatingRowBackground.None && newIndex % 2 == 1;
            recycledItem.rootElement.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassName, useAlternateUss);

            if (recycledItem.index != ReusableCollectionItem.UndefinedIndex)
                m_ListView.viewController.InvokeUnbindItem(recycledItem, recycledItem.index);

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

            m_ListView.viewController.InvokeBindItem(recycledItem, newIndex);

            // Handle focus cycling
            m_ListView.HandleFocus(recycledItem);
        }

        public override void UpdateBackground()
        {
            var currentFillHeight = k_EmptyRows.layout.size.y;
            var backgroundFillHeight = m_ScrollView.contentViewport.layout.size.y - m_ScrollView.contentContainer.layout.size.y - currentFillHeight;
            if (m_ListView.showAlternatingRowBackgrounds != AlternatingRowBackground.All || backgroundFillHeight <= 0)
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

            var index = lastVisibleItem.index;

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
                m_ListView.viewController.InvokeMakeItem(item);
            }

            item.PreAttachElement();

            return item;
        }
    }
}
