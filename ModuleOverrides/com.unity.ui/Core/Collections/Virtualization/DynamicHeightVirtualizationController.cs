// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class DynamicHeightVirtualizationController<T> : VerticalVirtualizationController<T> where T : ReusableCollectionItem, new()
    {
        internal static readonly int InitialAverageHeight = 20;

        Dictionary<int, float> m_ItemHeightCache = new Dictionary<int, float>();
        HashSet<int> m_WaitingCache = new HashSet<int>();

        int m_ForcedFirstVisibleItem = -1;
        int m_ForcedLastVisibleItem = -1;
        bool m_StickToBottom;

        float m_AverageHeight = InitialAverageHeight;
        float m_AccumulatedHeight;
        float m_StoredPadding;

        Action m_FillCallback;
        Action<ReusableCollectionItem> m_GeometryChangedCallback;
        IVisualElementScheduledItem m_ScheduledItem;
        Predicate<int> m_IndexOutOfBoundsPredicate;

        public DynamicHeightVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView)
        {
            m_FillCallback = Fill;
            m_GeometryChangedCallback = OnRecycledItemGeometryChanged;
            m_IndexOutOfBoundsPredicate = IsIndexOutOfBounds;
            m_ScrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        public override void Refresh(bool rebuild)
        {
            base.Refresh(rebuild);

            if (rebuild)
                m_WaitingCache.Clear();
            else
                m_WaitingCache.RemoveWhere(m_IndexOutOfBoundsPredicate);

            if (m_CollectionView.HasValidDataAndBindings())
            {
                m_ScheduledItem ??= m_CollectionView.schedule.Execute(m_FillCallback);
            }
        }

        public override void ScrollToItem(int index)
        {
            if (visibleItemCount == 0 || index < -1)
                return;

            var currentContentHeight = m_ScrollView.contentContainer.layout.height;
            var viewportHeight = m_ScrollView.contentViewport.layout.height;

            if (index == -1)
            {
                // Scroll to last item
                m_ForcedLastVisibleItem = itemsCount - 1;
                m_StickToBottom = true;
                m_ScrollView.scrollOffset = new Vector2(0, viewportHeight >= currentContentHeight ? 0 : currentContentHeight);
            }
            else if (m_FirstVisibleIndex >= index)
            {
                m_ForcedFirstVisibleItem = index;
                m_ScrollView.scrollOffset = new Vector2(0, GetContentHeightForIndex(index - 1));
            }
            else // index > first
            {
                var itemOffset = GetContentHeightForIndex(index);
                if (itemOffset < m_StoredPadding + viewportHeight)
                    return;

                m_ForcedLastVisibleItem = index;

                var yScrollOffset = itemOffset - viewportHeight + InitialAverageHeight;

                m_ScrollView.scrollOffset = new Vector2(0, yScrollOffset);
            }
        }

        public override void Resize(Vector2 size, int layoutPass)
        {
            var contentHeight = GetContentHeight();
            m_ScrollView.contentContainer.style.height = contentHeight;

            // Recalculate offset
            var firstItemPadding = GetContentHeightForIndex(m_FirstVisibleIndex - 1);

            var previousOffset = m_CollectionView.m_ScrollOffset.y;
            var previousPadding = m_StoredPadding;
            var deltaOffset = previousOffset - previousPadding;
            var offset = firstItemPadding + deltaOffset;

            // Restore scroll offset and preemptively update the highValue
            // in case this is the initial restore from persistent data and
            // the ScrollView's OnGeometryChanged() didn't update the low
            // and highValues.
            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            var scrollOffset = Mathf.Min(offset, scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
            m_ScrollView.verticalScroller.value = scrollOffset;
            m_CollectionView.m_ScrollOffset.y = m_ScrollView.verticalScroller.value;
            m_ScrollView.contentContainer.style.paddingTop = firstItemPadding;
            m_StoredPadding = firstItemPadding;

            if (layoutPass == 0)
            {
                Fill();
                OnScroll(new Vector2(0, scrollOffset));
            }
            else if (m_ScheduledItem == null)
            {
                m_ScheduledItem = m_CollectionView.schedule.Execute(m_FillCallback);
            }
        }

        public override void OnScroll(Vector2 scrollOffset)
        {
            if (float.IsNaN(lastHeight))
                return;

            var offset = scrollOffset.y;
            var contentHeight = GetContentHeight();
            var maxOffset = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            var scrollableHeight = m_ScrollView.contentContainer.boundingBox.height - m_ScrollView.contentViewport.layout.height;
            m_CollectionView.m_ScrollOffset.y = Mathf.Min(offset, maxOffset);

            if (scrollOffset.y == 0)
            {
                m_ForcedFirstVisibleItem = 0;
            }
            else
            {
                m_StickToBottom = scrollableHeight > 0 && Math.Abs(scrollOffset.y - m_ScrollView.verticalScroller.highValue) < float.Epsilon;
            }

            var firstIndex = m_ForcedFirstVisibleItem != -1 ? m_ForcedFirstVisibleItem : GetFirstVisibleItem(m_CollectionView.m_ScrollOffset.y);
            var firstVisiblePadding = GetContentHeightForIndex(firstIndex - 1);
            m_ForcedFirstVisibleItem = -1;

            if (firstIndex != m_FirstVisibleIndex)
            {
                m_FirstVisibleIndex = firstIndex;

                if (m_ActiveItems.Count > 0)
                {
                    var currentFirstVisibleItem = firstVisibleItem;
                    if (m_StickToBottom)
                    {
                        // Skip the item swapping.
                    }
                    else if (m_FirstVisibleIndex < currentFirstVisibleItem.index) //we're scrolling up
                    {
                        //How many do we have to swap back
                        var count = currentFirstVisibleItem.index - m_FirstVisibleIndex;

                        var inserting = m_ScrollInsertionList;

                        for (var i = 0; i < count && m_ActiveItems.Count > 0; ++i)
                        {
                            var last = m_ActiveItems[m_ActiveItems.Count - 1];
                            if (last.rootElement.layout.y < m_CollectionView.m_ScrollOffset.y + m_ScrollView.contentViewport.layout.height)
                                break;

                            inserting.Add(last);
                            m_ActiveItems.RemoveAt(m_ActiveItems.Count - 1); //we remove from the end

                            last.rootElement.SendToBack(); //We send the element to the top of the list (back in z-order)
                        }

                        m_ActiveItems.InsertRange(0, inserting);
                        m_ScrollInsertionList.Clear();
                    }
                    else // down
                    {
                        var currentLastVisibleItem = lastVisibleItem;
                        if (m_FirstVisibleIndex < currentLastVisibleItem.index && currentLastVisibleItem.index < itemsCount - 1)
                        {
                            var inserting = m_ScrollInsertionList;

                            var checkIndex = 0;
                            while (m_FirstVisibleIndex > m_ActiveItems[checkIndex].index)
                            {
                                var first = m_ActiveItems[checkIndex];
                                inserting.Add(first);
                                checkIndex++;

                                first.rootElement.BringToFront(); //We send the element to the bottom of the list (front in z-order)
                            }

                            m_ActiveItems.RemoveRange(0, checkIndex); //we remove them all at once
                            m_ActiveItems.AddRange(inserting); // add them back to the end
                            m_ScrollInsertionList.Clear();
                        }
                    }

                    var itemContentOffset = firstVisiblePadding;

                    //Let's rebind everything
                    for (var i = 0; i < m_ActiveItems.Count; i++)
                    {
                        var index = m_FirstVisibleIndex + i;

                        if (index >= itemsCount)
                        {
                            m_StickToBottom = true;
                            m_ForcedLastVisibleItem = -1;
                            ReleaseItem(i--);
                            continue;
                        }

                        var isItemInViewport = itemContentOffset - m_CollectionView.m_ScrollOffset.y <= m_ScrollView.contentViewport.layout.height;
                        var previousIndex = m_ActiveItems[i].index;
                        m_WaitingCache.Remove(previousIndex);

                        Setup(m_ActiveItems[i], index);

                        if (isItemInViewport)
                        {
                            if (index != previousIndex)
                            {
                                m_WaitingCache.Add(index);
                            }
                        }
                        else
                        {
                            ReleaseItem(i--);
                        }

                        itemContentOffset += GetItemHeight(index);
                    }
                }
            }

            m_StoredPadding = firstVisiblePadding;
            m_ScrollView.contentContainer.style.paddingTop = firstVisiblePadding;
            m_ScheduledItem ??= m_CollectionView.schedule.Execute(m_FillCallback);
        }

        bool NeedsFill()
        {
            var lastItemIndex = lastVisibleItem?.index ?? -1;
            var padding = m_StoredPadding;

            if (padding > m_CollectionView.m_ScrollOffset.y)
                return true;

            for (var i = m_FirstVisibleIndex; i < itemsCount; i++)
            {
                if (padding - m_CollectionView.m_ScrollOffset.y > m_ScrollView.contentViewport.layout.height)
                    break;

                padding += GetItemHeight(i);

                if (i > lastItemIndex)
                    return true;
            }

            return false;
        }

        void Fill()
        {
            if (!m_CollectionView.HasValidDataAndBindings())
                return;

            var itemsToAdd = 0;
            var lastItem = lastVisibleItem;
            var lastItemIndex = lastItem?.index ?? -1;
            var firstVisiblePadding = m_StoredPadding;
            var padding = firstVisiblePadding;

            if (m_CollectionView.dragger is ListViewDraggerAnimated dragger && dragger.draggedItem != null)
            {
                padding -= dragger.draggedItem.rootElement.style.height.value.value;
            }

            for (var i = m_FirstVisibleIndex; i < itemsCount; i++)
            {
                if (padding - m_CollectionView.m_ScrollOffset.y > m_ScrollView.contentViewport.layout.height)
                    break;

                padding += GetItemHeight(i);

                if (i > lastItemIndex)
                    itemsToAdd++;
            }

            // Grow down
            var initialVisibleCount = visibleItemCount;
            for (var i = 0; i < itemsToAdd; i++)
            {
                var index = i + m_FirstVisibleIndex + initialVisibleCount;
                var recycledItem = GetOrMakeItem();
                m_ActiveItems.Add(recycledItem);
                m_ScrollView.Add(recycledItem.rootElement);
                m_WaitingCache.Add(index);

                Setup(recycledItem, index);
            }

            // Grow upwards
            while (firstVisiblePadding > m_CollectionView.m_ScrollOffset.y)
            {
                var index = m_FirstVisibleIndex - 1;

                if (index < 0)
                    break;

                var recycledItem = GetOrMakeItem();
                m_ActiveItems.Insert(0, recycledItem);
                m_ScrollView.Insert(0, recycledItem.rootElement);
                m_WaitingCache.Add(index);

                Setup(recycledItem, index);

                firstVisiblePadding -= GetItemHeight(index);
                m_FirstVisibleIndex = index;
            }

            m_ScrollView.contentContainer.style.paddingTop = firstVisiblePadding;
            m_StoredPadding = firstVisiblePadding;
            m_ScheduledItem = null;
        }

        public override int GetIndexFromPosition(Vector2 position)
        {
            var index = 0;
            var traversedHeight = 0f;
            while (traversedHeight < position.y)
            {
                traversedHeight += GetItemHeight(index++);
            }

            return index - 1;
        }

        public override float GetItemHeight(int index)
        {
            return m_ItemHeightCache.TryGetValue(index, out var height) ? height : m_AverageHeight;
        }

        int GetFirstVisibleItem(float offset)
        {
            if (offset <= 0)
                return 0;

            var index = -1;
            while (offset > 0)
            {
                index++;
                var height = GetItemHeight(index);
                offset -= height;
            }

            return index;
        }

        void UpdateScrollViewContainer(int index, float previousHeight, float newHeight)
        {
            var previousOffset = m_CollectionView.m_ScrollOffset.y;
            var previousPadding = m_StoredPadding;

            m_StoredPadding = GetContentHeightForIndex(m_FirstVisibleIndex - 1);

            if (m_StickToBottom)
                return;

            if (m_ForcedLastVisibleItem >= 0)
            {
                var lastItemHeight = GetContentHeightForIndex(m_ForcedLastVisibleItem);
                m_CollectionView.m_ScrollOffset.y = lastItemHeight + InitialAverageHeight - m_ScrollView.contentViewport.layout.height;
            }
            else
            {
                var offset = previousOffset - previousPadding;

                // We need to adjust the scroll offset relative to the new item height.
                if (index == m_FirstVisibleIndex && offset != 0)
                {
                    offset += newHeight - previousHeight;
                }

                m_CollectionView.m_ScrollOffset.y = m_StoredPadding + offset;
            }
        }

        void ApplyScrollViewUpdate()
        {
            var contentHeight = GetContentHeight();
            m_StoredPadding = GetContentHeightForIndex(m_FirstVisibleIndex - 1);

            m_ScrollView.contentContainer.style.paddingTop = m_StoredPadding;
            m_ScrollView.contentContainer.style.height = contentHeight;

            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);

            if (m_StickToBottom)
            {
                m_CollectionView.m_ScrollOffset.y = scrollableHeight;
            }

            m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(m_CollectionView.m_ScrollOffset.y);
            m_CollectionView.m_ScrollOffset.y = m_ScrollView.verticalScroller.slider.value;

            if (!NeedsFill())
            {
                // Clear extra items.
                var itemContentOffset = m_StoredPadding;
                var previousFirstVisibleIndex = m_FirstVisibleIndex;

                for (var i = 0; i < m_ActiveItems.Count; i++)
                {
                    var index = m_FirstVisibleIndex + i;
                    var isItemInViewport = itemContentOffset - m_CollectionView.m_ScrollOffset.y < m_ScrollView.contentViewport.layout.height;

                    if (!isItemInViewport)
                    {
                        if (m_FirstVisibleIndex == index)
                            m_FirstVisibleIndex = index + 1;
                    }

                    itemContentOffset += GetItemHeight(index);
                }

                if (m_FirstVisibleIndex != previousFirstVisibleIndex)
                {
                    m_StoredPadding = GetContentHeightForIndex(m_FirstVisibleIndex - 1);
                }

                m_StickToBottom = false;
                m_ForcedLastVisibleItem = -1;
                m_ScheduledItem?.Pause();
                m_ScheduledItem = null;
            }
            else
            {
                m_ScheduledItem ??= m_CollectionView.schedule.Execute(m_FillCallback);
            }
        }

        void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.oldRect.size == evt.newRect.size)
                return;

            m_ScrollView.UpdateScrollers(m_ScrollView.needsHorizontal, m_ScrollView.needsVertical);
        }

        float GetContentHeight()
        {
            return GetContentHeightForIndex(itemsCount - 1);
        }

        // TODO [GR] optim this.
        float GetContentHeightForIndex(int lastIndex)
        {
            if (lastIndex < 0)
                return 0;

            var height = 0f;
            for (var index = 0; index <= lastIndex; index++)
            {
                height += GetItemHeight(index);
            }

            return height;
        }

        void RegisterItemHeight(int index, float height)
        {
            if (height <= 0)
                return;

            var resolvedHeight = m_CollectionView.ResolveItemHeight(height);

            if (m_ItemHeightCache.TryGetValue(index, out var value))
                m_AccumulatedHeight -= value;

            m_AccumulatedHeight += resolvedHeight;
            var count = m_ItemHeightCache.Count;
            m_AverageHeight = m_CollectionView.ResolveItemHeight(count > 0 ? m_AccumulatedHeight / count : m_AccumulatedHeight);

            m_ItemHeightCache[index] = resolvedHeight;
        }

        void UnregisterItemHeight(int index, float height)
        {
            if (height <= 0)
                return;

            if (!m_ItemHeightCache.TryGetValue(index, out var value))
                return;

            m_AccumulatedHeight -= value;
            m_ItemHeightCache.Remove(index);

            var count = m_ItemHeightCache.Count;
            m_AverageHeight = m_CollectionView.ResolveItemHeight(count > 0 ? m_AccumulatedHeight / count : m_AccumulatedHeight);
        }

        void OnRecycledItemGeometryChanged(ReusableCollectionItem item)
        {
            var rItem = item;
            if (rItem.index == ReusableCollectionItem.UndefinedIndex || float.IsNaN(rItem.rootElement.layout.height) || rItem.rootElement.layout.height == 0)
                return;

            if (rItem.animator != null && rItem.animator.isRunning)
                return;

            var previousHeight = GetItemHeight(rItem.index);

            if (!m_ItemHeightCache.TryGetValue(rItem.index, out var height) || !rItem.rootElement.layout.height.Equals(height))
            {
                RegisterItemHeight(rItem.index, rItem.rootElement.layout.height);
                UpdateScrollViewContainer(rItem.index, previousHeight, rItem.rootElement.layout.height);

                if (m_WaitingCache.Count == 0)
                {
                    ApplyScrollViewUpdate();
                }
            }

            if (m_WaitingCache.Remove(rItem.index) && m_WaitingCache.Count == 0)
            {
                ApplyScrollViewUpdate();
            }
        }

        internal override T GetOrMakeItem()
        {
            var item = base.GetOrMakeItem();
            item.onGeometryChanged += m_GeometryChangedCallback;
            return item;
        }

        public override void ReplaceActiveItem(int index)
        {
            base.ReplaceActiveItem(index);
            m_WaitingCache.Remove(index);

            foreach (var item in m_CollectionView.activeItems)
            {
                if (item.index == index)
                {
                    UnregisterItemHeight(item.index, item.rootElement.layout.height);
                    break;
                }
            }
        }

        internal override void ReleaseItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            item.onGeometryChanged -= m_GeometryChangedCallback;
            var index = item.index;
            base.ReleaseItem(activeItemsIndex);
            m_WaitingCache.Remove(index);
        }

        bool IsIndexOutOfBounds(int i)
        {
            return m_CollectionView.itemsSource == null || i >= itemsCount;
        }
    }
}
