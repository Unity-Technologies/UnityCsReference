// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    internal class DynamicHeightVirtualizationController<T> : VerticalVirtualizationController<T> where T : ReusableCollectionItem, new()
    {
        readonly struct ContentHeightCacheInfo
        {
            public readonly float sum;
            public readonly int count;

            public ContentHeightCacheInfo(float sum, int count)
            {
                this.sum = sum;
                this.count = count;
            }
        }

        const int k_AdditionalItems = 5;
        int m_HighestCachedIndex = -1;
        readonly Dictionary<int, float> m_ItemHeightCache = new Dictionary<int, float>(32);
        readonly Dictionary<int, ContentHeightCacheInfo> m_ContentHeightCache = new Dictionary<int, ContentHeightCacheInfo>(32);
        readonly HashSet<int> m_WaitingCache = new HashSet<int>(32);

        // Internal for tests.
        internal IReadOnlyDictionary<int, float> itemHeightCache => m_ItemHeightCache;

        int m_ForcedFirstVisibleItem = -1;
        int m_ForcedLastVisibleItem = -1;
        bool m_StickToBottom;

        float m_AverageHeight = BaseVerticalCollectionView.s_DefaultItemHeight;
        float m_AccumulatedHeight;

        float storedPadding
        {
            get => m_CollectionView.serializedVirtualizationData.storedPadding;
            set
            {
                m_CollectionView.serializedVirtualizationData.storedPadding = value;
                m_CollectionView.SaveViewData();
            }
        }

        Action m_FillCallback;
        Action<ReusableCollectionItem> m_GeometryChangedCallback;
        IVisualElementScheduledItem m_ScheduledItem;
        IVisualElementScheduledItem m_ScrollbarsScheduledItem;
        Action m_UpdateScrollbarsCallback;
        Predicate<int> m_IndexOutOfBoundsPredicate;

        public DynamicHeightVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView)
        {
            m_FillCallback = Fill;
            m_GeometryChangedCallback = OnRecycledItemGeometryChanged;
            m_IndexOutOfBoundsPredicate = IsIndexOutOfBounds;
            m_UpdateScrollbarsCallback = UpdateScrollViewScrollers;
        }

        public override void Refresh(bool rebuild)
        {
            CleanItemHeightCache();

            if (rebuild)
            {
                m_WaitingCache.Clear();
            }
            else
            {
                m_WaitingCache.RemoveWhere(m_IndexOutOfBoundsPredicate);

                // Update item height of active items to support index changes (reordering).
                foreach (var item in m_ActiveItems)
                {
                    if (IsIndexOutOfBounds(item.index) || item.rootElement.style.display == DisplayStyle.None)
                        continue;

                    UpdateRegisteredHeight(item);
                }
            }

            base.Refresh(rebuild);

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
            else if (firstVisibleIndex >= index)
            {
                m_ForcedFirstVisibleItem = index;
                m_ScrollView.scrollOffset = new Vector2(0, GetContentHeightForIndex(index - 1));
            }
            else // index > first
            {
                var itemOffset = GetContentHeightForIndex(index);
                if (itemOffset < storedPadding + viewportHeight)
                    return;

                m_ForcedLastVisibleItem = index;

                var yScrollOffset = itemOffset - viewportHeight + BaseVerticalCollectionView.s_DefaultItemHeight;

                m_ScrollView.scrollOffset = new Vector2(0, yScrollOffset);
            }
        }

        public override void Resize(Vector2 size, int layoutPass)
        {
            var contentHeight = GetExpectedContentHeight();
            m_ScrollView.contentContainer.style.height = contentHeight;

            // Recalculate offset
            var firstItemPadding = GetContentHeightForIndex(firstVisibleIndex - 1);

            var previousOffset = m_CollectionView.serializedVirtualizationData.scrollOffset.y;
            var previousPadding = storedPadding;
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
            m_CollectionView.serializedVirtualizationData.scrollOffset.y = m_ScrollView.verticalScroller.value;
            m_ScrollView.contentContainer.style.paddingTop = firstItemPadding;
            storedPadding = firstItemPadding;

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
            if (float.IsNaN(lastHeight) || float.IsNaN(scrollOffset.y))
                return;

            var offset = scrollOffset.y;
            var contentHeight = GetExpectedContentHeight();
            var maxOffset = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            var scrollableHeight = m_ScrollView.contentContainer.boundingBox.height - m_ScrollView.contentViewport.layout.height;
            m_CollectionView.serializedVirtualizationData.scrollOffset.y = Mathf.Min(offset, maxOffset);

            if (scrollOffset.y == 0)
            {
                m_ForcedFirstVisibleItem = 0;
            }
            else
            {
                m_StickToBottom = scrollableHeight > 0 && Math.Abs(scrollOffset.y - m_ScrollView.verticalScroller.highValue) < float.Epsilon;
            }

            var firstIndex = m_ForcedFirstVisibleItem != -1 ? m_ForcedFirstVisibleItem : GetFirstVisibleItem(m_CollectionView.serializedVirtualizationData.scrollOffset.y);
            var firstVisiblePadding = GetContentHeightForIndex(firstIndex - 1);
            m_ForcedFirstVisibleItem = -1;

            if (firstIndex != firstVisibleIndex)
            {
                firstVisibleIndex = firstIndex;

                if (m_ActiveItems.Count > 0)
                {
                    var currentFirstVisibleItem = firstVisibleItem;
                    if (m_StickToBottom || currentFirstVisibleItem == null)
                    {
                        // Skip the item swapping.
                    }
                    else if (firstVisibleIndex < currentFirstVisibleItem.index) //we're scrolling up
                    {
                        //How many do we have to swap back
                        var count = currentFirstVisibleItem.index - firstVisibleIndex;

                        var inserting = m_ScrollInsertionList;

                        for (var i = 0; i < count && m_ActiveItems.Count > 0; ++i)
                        {
                            var last = m_ActiveItems[^1];
                            if (last.rootElement.layout.y < m_CollectionView.serializedVirtualizationData.scrollOffset.y + m_ScrollView.contentViewport.layout.height)
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
                        if (firstVisibleIndex < currentLastVisibleItem.index && !IsIndexOutOfBounds(currentLastVisibleItem.index))
                        {
                            var inserting = m_ScrollInsertionList;

                            var checkIndex = 0;
                            while (firstVisibleIndex > m_ActiveItems[checkIndex].index)
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
                        var recycledItem = m_ActiveItems[i];
                        var index = firstVisibleIndex + i;
                        var previousIndex = recycledItem.index;
                        m_WaitingCache.Remove(previousIndex);

                        if (IsIndexOutOfBounds(index))
                        {
                            if (recycledItem.rootElement.style.display == DisplayStyle.Flex)
                            {
                                m_StickToBottom = true;
                                m_ForcedLastVisibleItem = -1;
                                HideItem(i);
                            }

                            m_WaitingCache.Remove(index);
                            continue;
                        }

                        var isItemInViewport = itemContentOffset - m_CollectionView.serializedVirtualizationData.scrollOffset.y <= m_ScrollView.contentViewport.layout.height;

                        Setup(recycledItem, index);

                        if (isItemInViewport)
                        {
                            if (index != previousIndex)
                            {
                                m_WaitingCache.Add(index);
                            }
                        }
                        else
                        {
                            HideItem(i);
                        }

                        itemContentOffset += GetExpectedItemHeight(index);
                    }
                }
            }

            storedPadding = firstVisiblePadding;
            m_ScrollView.contentContainer.style.paddingTop = firstVisiblePadding;
            m_ScheduledItem ??= m_CollectionView.schedule.Execute(m_FillCallback);
        }

        bool NeedsFill()
        {
            var lastItemIndex = lastVisibleItem?.index ?? -1;
            var padding = storedPadding;

            if (padding > m_CollectionView.serializedVirtualizationData.scrollOffset.y)
                return true;

            for (var i = firstVisibleIndex; i < itemsCount; i++)
            {
                if (padding - m_CollectionView.serializedVirtualizationData.scrollOffset.y > m_ScrollView.contentViewport.layout.height)
                    break;

                padding += GetExpectedItemHeight(i);

                if (i > lastItemIndex)
                    return true;
            }

            return false;
        }

        void Fill()
        {
            if (!m_CollectionView.HasValidDataAndBindings())
                return;

            // Wait for view to scroll first.
            var contentHeight = GetExpectedContentHeight();
            if (storedPadding > contentHeight)
                return;

            var itemsToAdd = 0;
            var lastItemIndex = lastVisibleItem?.index ?? -1;
            var firstVisiblePadding = storedPadding;
            var padding = firstVisiblePadding;

            if (m_CollectionView.dragger is ListViewDraggerAnimated dragger && dragger.draggedItem != null)
            {
                padding -= dragger.draggedItem.rootElement.style.height.value.value;
            }

            // Find how many items we should add to fill the viewport.
            for (var i = firstVisibleIndex; i < itemsCount; i++)
            {
                if (padding - m_CollectionView.serializedVirtualizationData.scrollOffset.y > m_ScrollView.contentViewport.layout.height)
                    break;

                padding += GetExpectedItemHeight(i);

                if (i > lastItemIndex)
                    itemsToAdd++;
            }

            // Give some leverage to the layout system to avoid coming back here too often.
            if (itemsToAdd > 0 && !m_StickToBottom && m_ActiveItems.Count - visibleItemCount < itemsToAdd)
                itemsToAdd += k_AdditionalItems;

            // Grow down
            var initialVisibleCount = visibleItemCount;
            for (var i = 0; i < itemsToAdd; i++)
            {
                var index = i + firstVisibleIndex + initialVisibleCount;
                if (index >= itemsCount)
                    break;

                var recycledItem = GetOrMakeItemAtIndex();
                m_WaitingCache.Add(index);
                Setup(recycledItem, index);
            }

            // Grow upwards
            while (firstVisiblePadding > m_CollectionView.serializedVirtualizationData.scrollOffset.y)
            {
                var index = firstVisibleIndex - 1;

                if (index < 0)
                    break;

                var recycledItem = GetOrMakeItemAtIndex(0, 0);
                m_WaitingCache.Add(index);
                Setup(recycledItem, index);

                firstVisiblePadding -= GetExpectedItemHeight(index);
                firstVisibleIndex = index;
            }

            m_ScrollView.contentContainer.style.paddingTop = firstVisiblePadding;
            storedPadding = firstVisiblePadding;
            m_ScheduledItem = null;
        }

        public override int GetIndexFromPosition(Vector2 position)
        {
            var index = 0;
            var traversedHeight = 0f;
            while (traversedHeight < position.y)
            {
                traversedHeight += GetExpectedItemHeight(index++);
            }

            return index - 1;
        }

        float defaultExpectedHeight => m_CollectionView.m_ItemHeightIsInline ? m_CollectionView.fixedItemHeight : m_AverageHeight;

        public override float GetExpectedItemHeight(int index)
        {
            return m_ItemHeightCache.TryGetValue(index, out var height) ? height : defaultExpectedHeight;
        }

        int GetFirstVisibleItem(float offset)
        {
            if (offset <= 0)
                return 0;

            var index = -1;
            while (offset > 0)
            {
                index++;
                var height = GetExpectedItemHeight(index);
                offset -= height;
            }

            return index;
        }

        void UpdateScrollViewContainer(int index, float previousHeight, float newHeight)
        {
            var previousOffset = m_CollectionView.serializedVirtualizationData.scrollOffset.y;
            var previousPadding = storedPadding;

            storedPadding = GetContentHeightForIndex(firstVisibleIndex - 1);

            if (m_StickToBottom)
                return;

            if (newHeight < previousHeight)
            {
                foreach (var item in m_ActiveItems)
                {
                    if (item.index == itemsCount - 1 && item.rootElement.style.display == DisplayStyle.Flex)
                    {
                        m_StickToBottom = true;
                        return;
                    }
                }
            }

            if (m_ForcedLastVisibleItem >= 0)
            {
                var lastItemHeight = GetContentHeightForIndex(m_ForcedLastVisibleItem);
                m_CollectionView.serializedVirtualizationData.scrollOffset.y = lastItemHeight + BaseVerticalCollectionView.s_DefaultItemHeight - m_ScrollView.contentViewport.layout.height;
            }
            else
            {
                var offset = previousOffset - previousPadding;

                // We need to adjust the scroll offset relative to the new item height.
                if (index == firstVisibleIndex && offset != 0)
                {
                    offset += newHeight - previousHeight;
                }

                m_CollectionView.serializedVirtualizationData.scrollOffset.y = storedPadding + offset;
            }
        }

        void ApplyScrollViewUpdate()
        {
            var contentHeight = GetExpectedContentHeight();
            storedPadding = GetContentHeightForIndex(firstVisibleIndex - 1);

            m_ScrollView.contentContainer.style.paddingTop = storedPadding;
            m_ScrollView.contentContainer.style.height = contentHeight;

            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);

            if (m_StickToBottom)
            {
                m_CollectionView.serializedVirtualizationData.scrollOffset.y = scrollableHeight;
            }

            m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(m_CollectionView.serializedVirtualizationData.scrollOffset.y);
            m_CollectionView.serializedVirtualizationData.scrollOffset.y = m_ScrollView.verticalScroller.slider.value;

            if (!NeedsFill())
            {
                // Clear extra items.
                var itemContentOffset = storedPadding;
                var previousFirstVisibleIndex = firstVisibleIndex;

                for (var i = 0; i < m_ActiveItems.Count; i++)
                {
                    var index = firstVisibleIndex + i;
                    var isItemInViewport = itemContentOffset - m_CollectionView.serializedVirtualizationData.scrollOffset.y < m_ScrollView.contentViewport.layout.height;

                    if (!isItemInViewport && m_ActiveItems[i].rootElement.style.display == DisplayStyle.Flex)
                    {
                        HideItem(i);

                        if (firstVisibleIndex == index)
                            firstVisibleIndex = index + 1;
                    }

                    itemContentOffset += GetExpectedItemHeight(index);
                }

                if (firstVisibleIndex != previousFirstVisibleIndex)
                {
                    storedPadding = GetContentHeightForIndex(firstVisibleIndex - 1);
                }

                m_StickToBottom = false;
                m_ForcedLastVisibleItem = -1;
                m_ScheduledItem?.Pause();
                m_ScheduledItem = null;
                m_CollectionView.SaveViewData();
            }
            else
            {
                Fill();
            }
        }

        void UpdateScrollViewScrollers()
        {
            m_ScrollView.UpdateScrollers(m_ScrollView.needsHorizontal, m_ScrollView.needsVertical);
            m_ScrollbarsScheduledItem = null;
        }

        public override float GetExpectedContentHeight()
        {
            return m_AccumulatedHeight + (itemsCount - m_ItemHeightCache.Count) * defaultExpectedHeight;
        }

        float GetContentHeightForIndex(int lastIndex)
        {
            if (lastIndex < 0)
                return 0;

            if (m_ContentHeightCache.TryGetValue(lastIndex, out var contentHeight))
            {
                return contentHeight.sum + (lastIndex - contentHeight.count + 1) * defaultExpectedHeight;
            }

            return GetContentHeightForIndex(lastIndex - 1) + GetExpectedItemHeight(lastIndex);
        }

        ContentHeightCacheInfo GetCachedContentHeight(int index)
        {
            if (index < 0)
                return default;

            if (m_ContentHeightCache.TryGetValue(index, out var content))
                return content;

            return GetCachedContentHeight(index - 1);
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

            if (index > m_HighestCachedIndex)
            {
                m_HighestCachedIndex = index;
            }

            var isNew = value == 0;
            var cached = GetCachedContentHeight(index - 1);
            m_ContentHeightCache[index] = new ContentHeightCacheInfo(cached.sum + resolvedHeight, cached.count + 1);

            foreach (var kvp in m_ItemHeightCache)
            {
                if (kvp.Key > index)
                {
                    var content = m_ContentHeightCache[kvp.Key];
                    m_ContentHeightCache[kvp.Key] = new ContentHeightCacheInfo(content.sum - value + resolvedHeight, isNew ? content.count + 1 : content.count);
                }
            }
        }

        void UnregisterItemHeight(int index)
        {
            if (!m_ItemHeightCache.TryGetValue(index, out var value))
                return;

            m_AccumulatedHeight -= value;
            m_ItemHeightCache.Remove(index);
            m_ContentHeightCache.Remove(index);

            var highestIndex = -1;
            foreach (var kvp in m_ItemHeightCache)
            {
                if (kvp.Key > index)
                {
                    var content = m_ContentHeightCache[kvp.Key];
                    m_ContentHeightCache[kvp.Key] = new ContentHeightCacheInfo(content.sum - value, content.count - 1);
                }

                if (kvp.Key > highestIndex)
                {
                    highestIndex = kvp.Key;
                }
            }

            m_HighestCachedIndex = highestIndex;

            var count = m_ItemHeightCache.Count;
            if (m_AccumulatedHeight <= 0)
            {
                m_AverageHeight = BaseVerticalCollectionView.s_DefaultItemHeight;
            }
            else
            {
                m_AverageHeight = m_CollectionView.ResolveItemHeight(count > 0 ? m_AccumulatedHeight / count : m_AccumulatedHeight);
            }
        }

        void CleanItemHeightCache()
        {
            // Indices are valid if they are within the array size.
            if (!IsIndexOutOfBounds(m_HighestCachedIndex))
                return;

            var unregisterList = ListPool<int>.Get();
            try
            {
                foreach (var index in m_ItemHeightCache.Keys)
                {
                    if (IsIndexOutOfBounds(index))
                    {
                        unregisterList.Add(index);
                    }
                }

                foreach (var index in unregisterList)
                {
                    UnregisterItemHeight(index);
                }
            }
            finally
            {
                ListPool<int>.Release(unregisterList);
            }
        }

        void OnRecycledItemGeometryChanged(ReusableCollectionItem item)
        {
            if (item.index == ReusableCollectionItem.UndefinedIndex || float.IsNaN(item.rootElement.layout.height) || item.rootElement.layout.height == 0)
                return;

            m_ScrollbarsScheduledItem ??= m_ScrollView.contentContainer.schedule.Execute(m_UpdateScrollbarsCallback);

            if (UpdateRegisteredHeight(item))
            {
                ApplyScrollViewUpdate();
            }
        }

        bool UpdateRegisteredHeight(ReusableCollectionItem item)
        {
            if (item.index == ReusableCollectionItem.UndefinedIndex || float.IsNaN(item.rootElement.layout.height) || item.rootElement.layout.height == 0)
                return false;

            var targetHeight = item.rootElement.layout.height - item.rootElement.style.paddingTop.value.value;
            var wasCached = m_ItemHeightCache.TryGetValue(item.index, out var height);
            var previousHeight = wasCached ? GetExpectedItemHeight(item.index) : targetHeight;

            if (!wasCached || !targetHeight.Equals(height))
            {
                RegisterItemHeight(item.index, targetHeight);
                UpdateScrollViewContainer(item.index, previousHeight, targetHeight);

                if (m_WaitingCache.Count == 0)
                {
                    return true;
                }
            }

            return m_WaitingCache.Remove(item.index) && m_WaitingCache.Count == 0;
        }

        internal override T GetOrMakeItemAtIndex(int activeItemIndex = -1, int scrollViewIndex = -1)
        {
            // Reuse hidden items first.
            foreach (var i in m_ActiveItems)
            {
                if (i.rootElement.style.display == DisplayStyle.None)
                {
                    if (activeItemIndex != -1)
                    {
                        m_ActiveItems.Remove(i);
                        m_ActiveItems.Insert(activeItemIndex, i);
                    }

                    if (scrollViewIndex != -1)
                    {
                        m_ScrollView.Insert(scrollViewIndex, i.rootElement);
                    }

                    return i;
                }
            }

            var item = base.GetOrMakeItemAtIndex(activeItemIndex, scrollViewIndex);
            item.onGeometryChanged += m_GeometryChangedCallback;
            return item;
        }

        public override void ReplaceActiveItem(int index)
        {
            base.ReplaceActiveItem(index);
            m_WaitingCache.Remove(index);
        }

        internal override void ReleaseItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            item.onGeometryChanged -= m_GeometryChangedCallback;
            var index = item.index;
            UnregisterItemHeight(index);
            base.ReleaseItem(activeItemsIndex);
            m_WaitingCache.Remove(index);
        }

        void HideItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            item.rootElement.style.display = DisplayStyle.None;
            m_WaitingCache.Remove(item.index);
        }

        bool IsIndexOutOfBounds(int i)
        {
            return m_CollectionView.itemsSource == null || i >= itemsCount;
        }
    }
}
