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

        int m_HighestCachedIndex = -1;
        readonly Dictionary<int, float> m_ItemHeightCache = new (32);
        readonly Dictionary<int, ContentHeightCacheInfo> m_ContentHeightCache = new (32);
        readonly HashSet<int> m_WaitingCache = new (32);
        int? m_ScrolledToItemIndex;

        // Internal for tests.
        internal IReadOnlyDictionary<int, float> itemHeightCache => m_ItemHeightCache;

        int m_ForcedFirstVisibleItem = -1;
        int m_ForcedLastVisibleItem = -1;
        bool m_StickToBottom;
        VirtualizationChange m_LastChange;
        ScrollDirection m_ScrollDirection;
        Vector2 m_DelayedScrollOffset = Vector2.negativeInfinity;

        enum VirtualizationChange
        {
            /// <summary>
            /// We're back in an idle state.
            /// </summary>
            None,
            /// <summary>
            /// Viewport was resized.
            /// </summary>
            Resize,
            /// <summary>
            /// Regular scroll changes, through mouse wheel or scroller changes.
            /// </summary>
            Scroll,
            /// <summary>
            /// ScrollToItem was called.
            /// </summary>
            ForcedScroll,
        }

        enum ScrollDirection
        {
            Idle,
            Up,
            Down,
        }

        float m_AccumulatedHeight;
        float m_MinimumItemHeight = -1;

        float defaultExpectedHeight
        {
            get
            {
                if (m_MinimumItemHeight > 0)
                    return m_MinimumItemHeight;

                if (m_CollectionView.m_ItemHeightIsInline && m_CollectionView.fixedItemHeight > 0)
                    return m_CollectionView.fixedItemHeight;

                return BaseVerticalCollectionView.s_DefaultItemHeight;
            }
        }

        float contentPadding
        {
            get => serializedData.contentPadding;
            set
            {
                m_CollectionView.scrollView.contentContainer.style.paddingTop = value;
                serializedData.contentPadding = value;
                m_CollectionView.SaveViewData();
            }
        }

        float contentHeight
        {
            get => serializedData.contentHeight;
            set
            {
                m_CollectionView.scrollView.contentContainer.style.height = value;
                serializedData.contentHeight = value;
                m_CollectionView.SaveViewData();
            }
        }

        int anchoredIndex
        {
            get => serializedData.anchoredItemIndex;
            set
            {
                serializedData.anchoredItemIndex = value;
                m_CollectionView.SaveViewData();
            }
        }

        float anchorOffset
        {
            get => serializedData.anchorOffset;
            set
            {
                serializedData.anchorOffset = value;
                m_CollectionView.SaveViewData();
            }
        }

        float viewportMaxOffset => serializedData.scrollOffset.y + m_ScrollView.contentViewport.layout.height;

        Action m_FillCallback;
        Action m_ScrollCallback;
        Action m_ScrollResetCallback;
        Action<ReusableCollectionItem> m_GeometryChangedCallback;
        IVisualElementScheduledItem m_ScheduledItem;
        IVisualElementScheduledItem m_ScrollScheduledItem;
        IVisualElementScheduledItem m_ScrollResetScheduledItem;
        Predicate<int> m_IndexOutOfBoundsPredicate;

        bool m_FillExecuted;
        long m_TimeSinceFillScheduledMs;

        // Dynamic height virtualization handles the refresh binding with the scheduled Fill call.
        protected override bool alwaysRebindOnRefresh => false;

        const float k_ForceRefreshIntervalInMilliseconds = 100;

        public DynamicHeightVirtualizationController(BaseVerticalCollectionView collectionView)
            : base(collectionView)
        {
            m_FillCallback = Fill;
            m_ScrollCallback = OnScrollUpdate;
            m_GeometryChangedCallback = OnRecycledItemGeometryChanged;
            m_IndexOutOfBoundsPredicate = IsIndexOutOfBounds;
            m_ScrollResetCallback = ResetScroll;

            collectionView.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
            collectionView.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        private void OnGeometryChangedEvent(GeometryChangedEvent _)
        {
            if (m_ScrolledToItemIndex != null)
            {
                if (ShouldDeferScrollToItem(m_ScrolledToItemIndex ?? ReusableCollectionItem.UndefinedIndex))
                    ScheduleDeferredScrollToItem();

                m_ScrolledToItemIndex = null;
            }
        }

        public override void Refresh(bool rebuild)
        {
            CleanItemHeightCache();

            var previousActiveItemsCount = m_ActiveItems.Count;
            var needsApply = false;

            if (rebuild)
            {
                m_WaitingCache.Clear();
            }
            else
            {
                needsApply |= m_WaitingCache.RemoveWhere(m_IndexOutOfBoundsPredicate) > 0;
            }

            base.Refresh(rebuild);

            m_ScrollDirection = ScrollDirection.Idle;
            m_LastChange = VirtualizationChange.None;

            if (m_CollectionView.HasValidDataAndBindings())
            {
                if (needsApply || previousActiveItemsCount != m_ActiveItems.Count)
                {
                    contentHeight = GetExpectedContentHeight();
                    var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
                    m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
                    m_ScrollView.verticalScroller.value = serializedData.scrollOffset.y;
                    serializedData.scrollOffset.y = m_ScrollView.verticalScroller.value;
                }

                ScheduleFill();
            }
        }

        public override void ScrollToItem(int index)
        {
            if (index < ReusableCollectionItem.UndefinedIndex)
                return;

            if (visibleItemCount == 0)
            {
                m_ScrolledToItemIndex = index;
                return;
            }

            ShouldDeferScrollToItem(index);

            var currentContentHeight = m_ScrollView.contentContainer.layout.height;
            var viewportHeight = m_ScrollView.contentViewport.layout.height;

            if (index == -1)
            {
                // Scroll to last item
                m_ForcedLastVisibleItem = itemsCount - 1;
                m_ForcedFirstVisibleItem = ReusableCollectionItem.UndefinedIndex;
                m_StickToBottom = true;
                m_ScrollView.scrollOffset = new Vector2(0, viewportHeight >= currentContentHeight ? 0 : currentContentHeight);
            }
            else if (firstVisibleIndex >= index)
            {
                m_ForcedFirstVisibleItem = index;
                m_ForcedLastVisibleItem = ReusableCollectionItem.UndefinedIndex;
                m_ScrollView.scrollOffset = new Vector2(0, GetContentHeightForIndex(index - 1));
            }
            else // index > first
            {
                var itemOffset = GetContentHeightForIndex(index);
                if (float.IsNaN(viewportHeight) || itemOffset < contentPadding + viewportHeight)
                    return;

                var yScrollOffset = itemOffset - viewportHeight + BaseVerticalCollectionView.s_DefaultItemHeight;
                m_ForcedLastVisibleItem = index;
                m_ForcedFirstVisibleItem = ReusableCollectionItem.UndefinedIndex;
                m_ScrollView.scrollOffset = new Vector2(0, yScrollOffset);
            }
        }

        public override void Resize(Vector2 size)
        {
            var expectedContentHeight = GetExpectedContentHeight();
            contentHeight = Mathf.Max(expectedContentHeight, contentHeight);

            // Restore scroll offset and preemptively update the highValue
            // in case this is the initial restore from persistent data and
            // the ScrollView's OnGeometryChanged() didn't update the low
            // and highValues.
            var viewportHeight = m_ScrollView.contentViewport.layout.height;
            var scrollableHeight = Mathf.Max(0, contentHeight - viewportHeight);
            var scrollOffset = Mathf.Min(serializedData.scrollOffset.y, scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(scrollOffset);
            serializedData.scrollOffset.y = m_ScrollView.verticalScroller.value;

            // We virtualize a number of items based on the smallest expected item height.
            var resolvedViewportHeight = m_CollectionView.ResolveItemHeight(size.y);
            var itemCountFromHeight = Mathf.CeilToInt(resolvedViewportHeight / defaultExpectedHeight);
            var expectedItemCount = itemCountFromHeight;
            if (expectedItemCount <= 0)
                return;

            expectedItemCount += k_ExtraVisibleItems;

            var itemCount = Mathf.Min(expectedItemCount, itemsCount);

            if (m_ActiveItems.Count != itemCount)
            {
                var initialItemCount = m_ActiveItems.Count;
                if (initialItemCount > itemCount)
                {
                    // Shrink
                    var removeCount = initialItemCount - itemCount;
                    for (var i = 0; i < removeCount; i++)
                    {
                        var lastIndex = m_ActiveItems.Count - 1;
                        ReleaseItem(lastIndex);
                    }
                }
                else
                {
                    // Grow
                    var addCount = itemCount - m_ActiveItems.Count;
                    var firstItem = firstVisibleIndex < 0 ? 0 : firstVisibleIndex;
                    for (var i = 0; i < addCount; i++)
                    {
                        var index = i + firstItem + initialItemCount;
                        var recycledItem = GetOrMakeItemAtIndex();

                        if (IsIndexOutOfBounds(index))
                        {
                            HideItem(m_ActiveItems.Count - 1);
                            continue;
                        }

                        Setup(recycledItem, index);
                        MarkWaitingForLayout(recycledItem);
                    }
                }
            }

            var currentTimeMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (currentTimeMs - m_TimeSinceFillScheduledMs > k_ForceRefreshIntervalInMilliseconds &&
                m_TimeSinceFillScheduledMs != 0 && !m_FillExecuted)
            {
                Fill();
                ResetScroll();
                m_TimeSinceFillScheduledMs = 0;
            }
            else
            {
                if (m_TimeSinceFillScheduledMs == 0)
                    m_TimeSinceFillScheduledMs = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

                ScheduleFill();
                ScheduleScrollDirectionReset();
                m_FillExecuted = false;
            }

            m_LastChange = VirtualizationChange.Resize;
        }

        public override void OnScroll(Vector2 scrollOffset)
        {
            if (m_DelayedScrollOffset == scrollOffset)
                return;

            m_DelayedScrollOffset = scrollOffset;

            // ScrollToItem forced a new offset, let's scroll right away, it will be our new saved state.
            if (m_ForcedFirstVisibleItem != -1 || m_ForcedLastVisibleItem != -1)
            {
                OnScrollUpdate();
                m_LastChange = VirtualizationChange.ForcedScroll;
                return;
            }

            // As we resize, the geometry changed event of the ScrollView tries to change the scroll offset as it
            // updates the scrollers. We want to keep our cached values, so we reassign them and early out until we're
            // in an idle state where we receive true user input.
            if (m_LastChange is VirtualizationChange.Resize or VirtualizationChange.ForcedScroll)
            {
                var viewportHeight = m_ScrollView.contentViewport.layout.height;
                var scrollableHeight = Mathf.Max(0, contentHeight - viewportHeight);
                var offset = Mathf.Min(serializedData.scrollOffset.y, scrollableHeight);
                m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
                m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(offset);
                serializedData.scrollOffset.y = m_ScrollView.verticalScroller.value;
                return;
            }

            // Schedule later to allow receiving multiple scroll events in one frame and potentially avoid a few expensive rebind.
            ScheduleScroll();
        }

        void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            if (m_ScheduledItem?.isActive == true)
            {
                m_ScheduledItem.Pause();
                m_ScheduledItem = null;
            }

            if (m_ScrollScheduledItem?.isActive == true)
            {
                m_ScrollScheduledItem.Pause();
                m_ScrollScheduledItem = null;
            }

            if (m_ScrollResetScheduledItem?.isActive == true)
            {
                m_ScrollResetScheduledItem.Pause();
                m_ScrollResetScheduledItem = null;
            }
        }

        void OnScrollUpdate()
        {
            var scrollOffset = float.IsNegativeInfinity(m_DelayedScrollOffset.y) ? serializedData.scrollOffset : m_DelayedScrollOffset;
            if (float.IsNaN(m_ScrollView.contentViewport.layout.height) || float.IsNaN(scrollOffset.y))
                return;

            m_LastChange = VirtualizationChange.Scroll;
            var expectedContentHeight = GetExpectedContentHeight();

            // Keep the serialized value if new content is smaller. Adjustment will come later in Fill or Apply.
            contentHeight = Mathf.Max(expectedContentHeight, contentHeight);
            m_ScrollDirection = scrollOffset.y < serializedData.scrollOffset.y ? ScrollDirection.Up : ScrollDirection.Down;
            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);

            if (scrollOffset.y <= 0)
            {
                m_ForcedFirstVisibleItem = 0;
            }

            m_StickToBottom = scrollableHeight > 0 && Math.Abs(scrollOffset.y - m_ScrollView.verticalScroller.highValue) < float.Epsilon;
            serializedData.scrollOffset = scrollOffset;
            m_CollectionView.SaveViewData();

            var firstIndex = m_ForcedFirstVisibleItem != -1 ? m_ForcedFirstVisibleItem : GetFirstVisibleItem(serializedData.scrollOffset.y);
            var firstVisiblePadding = GetContentHeightForIndex(firstIndex - 1);
            contentPadding = firstVisiblePadding;
            m_ForcedFirstVisibleItem = -1;

            if (firstIndex != firstVisibleIndex)
            {
                CycleItems(firstIndex);
            }
            else
            {
                Fill();
            }

            ScheduleScrollDirectionReset();

            m_DelayedScrollOffset = Vector2.negativeInfinity;
        }

        void CycleItems(int firstIndex)
        {
            if (firstIndex == firstVisibleIndex)
                return;

            var currentFirstVisibleItem = firstVisibleItem;
            contentPadding = GetContentHeightForIndex(firstIndex - 1);
            firstVisibleIndex = firstIndex;

            if (m_ActiveItems.Count > 0)
            {
                if (currentFirstVisibleItem == null || m_ActiveItems.Count <= Mathf.Abs(firstVisibleIndex - currentFirstVisibleItem.index))
                {
                    // We're scrolling for more items than the number of visible items, let's just rebind everything
                    // without cycling them in the scroll view / active items.
                }
                else if (firstVisibleIndex < currentFirstVisibleItem.index) // we're scrolling up.
                {
                    //How many do we have to swap back.
                    var count = currentFirstVisibleItem.index - firstVisibleIndex;
                    var inserting = m_ScrollInsertionList;

                    for (var i = 0; i < count; ++i)
                    {
                        var last = m_ActiveItems[^1];
                        inserting.Insert(0, last);
                        m_ActiveItems.RemoveAt(m_ActiveItems.Count - 1); // we remove from the end.

                        last.rootElement.SendToBack(); //We send the element to the top of the list (back in z-order)
                    }

                    m_ActiveItems.InsertRange(0, inserting);
                    m_ScrollInsertionList.Clear();
                }
                else // we're scrolling down
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

                var itemContentOffset = contentPadding;

                //Let's rebind everything
                for (var i = 0; i < m_ActiveItems.Count; i++)
                {
                    var recycledItem = m_ActiveItems[i];
                    var index = firstVisibleIndex + i;
                    var previousIndex = recycledItem.index;
                    var wasVisible = recycledItem.rootElement.style.display == DisplayStyle.Flex;
                    m_WaitingCache.Remove(previousIndex);

                    if (IsIndexOutOfBounds(index))
                    {
                        HideItem(i);
                        continue;
                    }

                    Setup(recycledItem, index);

                    var isItemOutsideViewport = itemContentOffset > viewportMaxOffset;
                    if (isItemOutsideViewport)
                    {
                        HideItem(i);
                    }
                    else if (index != previousIndex || !wasVisible)
                    {
                        MarkWaitingForLayout(recycledItem);
                    }

                    itemContentOffset += GetExpectedItemHeight(index);
                }
            }

            // Save anchored item.
            if (m_LastChange != VirtualizationChange.Resize)
            {
                UpdateAnchor();
            }

            ScheduleFill();
        }

        bool NeedsFill()
        {
            // No need to fill until we get to a stable state.
            if (m_LastChange != VirtualizationChange.None || anchoredIndex < 0)
                return false;

            var lastItemIndex = lastVisibleItem?.index ?? -1;
            var contentOffset = contentPadding;

            if (contentOffset > serializedData.scrollOffset.y)
                return true;

            for (var i = firstVisibleIndex; i < itemsCount; i++)
            {
                if (contentOffset > viewportMaxOffset || (contentOffset == viewportMaxOffset && !m_StickToBottom))
                    break;

                contentOffset += GetExpectedItemHeight(i);

                if (i > lastItemIndex)
                    return true;
            }

            return false;
        }

        void Fill()
        {
            if (!m_CollectionView.HasValidDataAndBindings())
                return;

            m_FillExecuted = true;

            if (m_ActiveItems.Count == 0)
            {
                // Reset the saved content height.
                contentHeight = 0;
                contentPadding = 0;
                return;
            }

            // Let the UI stabilize.
            if (anchoredIndex < 0)
                return;

            // Wait for view to scroll first.
            if (contentPadding > contentHeight)
            {
                OnScrollUpdate();
                return;
            }

            var firstVisiblePadding = contentPadding;
            var contentOffset = contentPadding;
            var activeIndex = 0;

            // Change the visibility of items under the current content to fill the viewport below.
            for (var i = firstVisibleIndex; i < itemsCount; i++)
            {
                if (contentOffset > viewportMaxOffset || (contentOffset == viewportMaxOffset && !m_StickToBottom))
                    break;

                contentOffset += GetExpectedItemHeight(i);

                var item = m_ActiveItems[activeIndex++];
                if (item.index != i || item.rootElement.style.display == DisplayStyle.None)
                {
                    Setup(item, i);
                    MarkWaitingForLayout(item);
                }

                if (activeIndex >= m_ActiveItems.Count)
                {
                    break;
                }
            }

            // Bring back items in front of the current content to fill the viewport above.
            if (firstVisibleIndex > 0 && contentPadding > serializedData.scrollOffset.y)
            {
                var inserting = m_ScrollInsertionList;

                for (var i = m_ActiveItems.Count - 1; i >= activeIndex; --i)
                {
                    if (firstVisibleIndex == 0)
                        break;

                    var last = m_ActiveItems[i];

                    inserting.Insert(0, last);
                    m_ActiveItems.RemoveAt(m_ActiveItems.Count - 1); // we remove from the end
                    last.rootElement.SendToBack();

                    var newIndex = --firstVisibleIndex;
                    Setup(last, newIndex);
                    MarkWaitingForLayout(last);

                    firstVisiblePadding -= GetExpectedItemHeight(newIndex);
                    if (firstVisiblePadding < serializedData.scrollOffset.y)
                        break;
                }

                m_ActiveItems.InsertRange(0, inserting);
                m_ScrollInsertionList.Clear();
            }

            contentPadding = firstVisiblePadding;
            contentHeight = GetExpectedContentHeight();

            // During resize, we must not update the anchored item, as it may take a while for the UI to stabilize.
            if (m_LastChange != VirtualizationChange.Resize)
            {
                UpdateAnchor();
            }

            // After a fill, we want to reapply the dimensions correctly if anything changed.
            if (m_WaitingCache.Count == 0)
            {
                ResetScroll();
                ApplyScrollViewUpdate(true);
            }
        }

        void UpdateScrollViewContainer(float previousHeight, float newHeight)
        {
            if (m_StickToBottom)
                return;

            if (m_ForcedLastVisibleItem >= 0)
            {
                var lastItemHeight = GetContentHeightForIndex(m_ForcedLastVisibleItem);
                serializedData.scrollOffset.y = lastItemHeight + BaseVerticalCollectionView.s_DefaultItemHeight - m_ScrollView.contentViewport.layout.height;
            }
            else
            {
                if (m_ScrollDirection == ScrollDirection.Up)
                {
                    serializedData.scrollOffset.y += newHeight - previousHeight;
                }
            }
        }

        void ApplyScrollViewUpdate(bool dimensionsOnly = false)
        {
            var previousPadding = contentPadding;
            var previousScrollOffset = serializedData.scrollOffset.y;
            var itemOffset = previousScrollOffset - previousPadding;

            if (anchoredIndex >= 0)
            {
                // Force the anchored item to the top.
                if (firstVisibleIndex != anchoredIndex)
                {
                    CycleItems(anchoredIndex);
                    ScheduleFill();
                }

                firstVisibleIndex = anchoredIndex;
                itemOffset = anchorOffset;
            }

            var expectedContentHeight = GetExpectedContentHeight();
            contentHeight = expectedContentHeight;
            contentPadding = GetContentHeightForIndex(firstVisibleIndex - 1);

            // RoundToPixelGrid to avoid imprecision (UUM-69616)
            var scrollableHeight = Mathf.Max(0, m_ScrollView.RoundToPanelPixelSize(expectedContentHeight - m_ScrollView.contentViewport.layout.height));
            var scrollOffset = Mathf.Min(contentPadding + itemOffset, scrollableHeight);

            // Stick to the end of the viewport.
            if (m_StickToBottom && scrollableHeight > 0)
            {
                scrollOffset = scrollableHeight;
            }
            else if (m_ForcedLastVisibleItem != -1)
            {
                var lastItemHeight = GetContentHeightForIndex(m_ForcedLastVisibleItem);
                var lastItemViewportOffset = lastItemHeight + BaseVerticalCollectionView.s_DefaultItemHeight - m_ScrollView.contentViewport.layout.height;
                scrollOffset = Mathf.Clamp(lastItemViewportOffset, 0, scrollableHeight);
                m_ForcedLastVisibleItem = -1;
            }

            // Don't notify to avoid coming back in the scroll update for no reason.
            m_ScrollView.verticalScroller.slider.SetHighValueWithoutNotify(scrollableHeight);
            m_ScrollView.verticalScroller.slider.SetValueWithoutNotify(scrollOffset);
            serializedData.scrollOffset.y = m_ScrollView.verticalScroller.slider.value;

            if (dimensionsOnly || m_LastChange == VirtualizationChange.Resize)
            {
                ScheduleScrollDirectionReset();
                return;
            }

            if (NeedsFill())
            {
                Fill();
            }
            else
            {
                // Clear extra items.
                var itemContentOffset = contentPadding;
                var previousFirstVisibleIndex = firstVisibleIndex;

                var inserting = m_ScrollInsertionList;
                var bumpCount = 0;
                for (var i = 0; i < m_ActiveItems.Count; i++)
                {
                    var item = m_ActiveItems[i];
                    var index = item.index;

                    if (index < 0)
                        break;

                    var itemHeight = GetExpectedItemHeight(index);

                    // Hide items outside viewport's top/bottom.
                    if (m_ActiveItems[i].rootElement.style.display == DisplayStyle.Flex)
                    {
                        // Items above the viewport bounds need to be sent back to the back of active items.
                        if (itemContentOffset + itemHeight < serializedData.scrollOffset.y)
                        {
                            item.rootElement.BringToFront(); // We send the element to the bottom of the list (front in z-order)
                            HideItem(i);
                            inserting.Add(item);
                            bumpCount++;
                            firstVisibleIndex++;
                        }
                        else if (itemContentOffset > viewportMaxOffset)
                        {
                            // We can just hide items below the bounds of the viewport.
                            HideItem(i);
                        }
                    }

                    itemContentOffset += GetExpectedItemHeight(index);
                }

                m_ActiveItems.RemoveRange(0, bumpCount); //we remove them all at once
                m_ActiveItems.AddRange(inserting); // add them back to the end
                m_ScrollInsertionList.Clear();

                if (firstVisibleIndex != previousFirstVisibleIndex)
                {
                    contentPadding = GetContentHeightForIndex(firstVisibleIndex - 1);
                    UpdateAnchor();
                }

                ScheduleScrollDirectionReset();
                m_CollectionView.SaveViewData();
            }

            ScheduleDeferredScrollToItem();
        }

        void UpdateAnchor()
        {
            anchoredIndex = firstVisibleIndex;
            anchorOffset = serializedData.scrollOffset.y - contentPadding;
        }

        void ScheduleFill()
        {
            if (m_ScheduledItem == null)
            {
                m_ScheduledItem = m_CollectionView.schedule.Execute(m_FillCallback);
                return;
            }

            m_ScheduledItem.Pause();
            m_ScheduledItem.Resume();
        }

        void ScheduleScroll()
        {
            if (m_ScrollScheduledItem == null)
            {
                m_ScrollScheduledItem = m_CollectionView.schedule.Execute(m_ScrollCallback);
                return;
            }

            m_ScrollScheduledItem.Pause();
            m_ScrollScheduledItem.Resume();
        }

        void ScheduleScrollDirectionReset()
        {
            if (m_ScrollResetScheduledItem == null)
            {
                m_ScrollResetScheduledItem = m_CollectionView.schedule.Execute(m_ScrollResetCallback);
                return;
            }

            m_ScrollResetScheduledItem.Pause();
            m_ScrollResetScheduledItem.Resume();
        }

        void ResetScroll()
        {
            m_ScrollDirection = ScrollDirection.Idle;
            m_LastChange = VirtualizationChange.None;
            m_ScrollView.UpdateContentViewTransform();
            UpdateAnchor();
            m_CollectionView.SaveViewData();
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

        public override float GetExpectedItemHeight(int index)
        {
            var draggedIndex = GetDraggedIndex();
            if (draggedIndex >= 0 && index == draggedIndex)
            {
                return 0;
            }

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

        public override float GetExpectedContentHeight()
        {
            return m_AccumulatedHeight + (itemsCount - m_ItemHeightCache.Count) * defaultExpectedHeight;
        }

        float GetContentHeightForIndex(int lastIndex)
        {
            if (lastIndex < 0)
                return 0;

            var draggedIndex = GetDraggedIndex();

            float GetContentHeightFromCachedHeight(int index, in ContentHeightCacheInfo heightInfo)
            {
                // Make sure we don't include the dragged item height.
                if (draggedIndex >= 0 && index >= draggedIndex)
                {
                    return heightInfo.sum + (index - heightInfo.count + 1) * defaultExpectedHeight - m_DraggedItem.rootElement.layout.height;
                }

                return heightInfo.sum + (index - heightInfo.count + 1) * defaultExpectedHeight;
            }

            // We can skip a lot of work when there is a big jump past the last known cached index. We can use the default
            // expected height for anything past that, without the need to iterate through the indices.
            if (m_HighestCachedIndex <= lastIndex && m_ContentHeightCache.TryGetValue(m_HighestCachedIndex, out var highestHeightInfo))
            {
                return GetContentHeightFromCachedHeight(lastIndex, highestHeightInfo);
            }

            // Accumulate height down the indices until we find a node that has been cached.
            var totalHeight = 0f;
            for (var i = lastIndex; i >= 0; i--)
            {
                if (m_ContentHeightCache.TryGetValue(i, out var heightInfo))
                {
                    return totalHeight + GetContentHeightFromCachedHeight(i, heightInfo);
                }

                totalHeight += draggedIndex == i ? 0 : defaultExpectedHeight;
            }

            return totalHeight;
        }

        ContentHeightCacheInfo GetCachedContentHeight(int index)
        {
            while (index >= 0)
            {
                if (m_ContentHeightCache.TryGetValue(index, out var content))
                    return content;

                index -= 1;
            }

            return default;
        }

        void RegisterItemHeight(int index, float height)
        {
            if (height <= 0)
                return;

            var resolvedHeight = m_CollectionView.ResolveItemHeight(height);

            if (m_ItemHeightCache.TryGetValue(index, out var value))
                m_AccumulatedHeight -= value;

            m_AccumulatedHeight += resolvedHeight;
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

            m_MinimumItemHeight = -1;
        }

        void OnRecycledItemGeometryChanged(ReusableCollectionItem item)
        {
            if (item.index == ReusableCollectionItem.UndefinedIndex || item.isDragGhost || float.IsNaN(item.rootElement.layout.height) || item.rootElement.layout.height == 0)
                return;

            if (UpdateRegisteredHeight(item))
            {
                ApplyScrollViewUpdate();
            }
        }

        bool UpdateRegisteredHeight(ReusableCollectionItem item)
        {
            if (item.index == ReusableCollectionItem.UndefinedIndex || item.isDragGhost || float.IsNaN(item.rootElement.layout.height) || item.rootElement.layout.height == 0)
                return false;

            if (item.rootElement.layout.height < defaultExpectedHeight)
            {
                m_MinimumItemHeight = item.rootElement.layout.height;
                Resize(m_ScrollView.layout.size);
            }

            var targetHeight = item.rootElement.layout.height - item.rootElement.resolvedStyle.paddingTop;
            var wasCached = m_ItemHeightCache.TryGetValue(item.index, out var height);
            var previousHeight = wasCached ? GetExpectedItemHeight(item.index) : defaultExpectedHeight;

            // Update the m_StickToBottom variable.
            if (m_WaitingCache.Count == 0)
            {
                // When the size increases on an item that wasn't waiting for layout, we are certainly not sticking
                // to the bottom. Otherwise, we should check if we should.
                if (targetHeight > previousHeight)
                {
                    m_StickToBottom = false;
                }
                else
                {
                    var deltaHeight = targetHeight - previousHeight;
                    var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
                    m_StickToBottom = scrollableHeight > 0 && serializedData.scrollOffset.y >= m_ScrollView.verticalScroller.highValue + deltaHeight;
                }
            }

            if (!wasCached || !Mathf.Approximately(targetHeight, height))
            {
                RegisterItemHeight(item.index, targetHeight);
                UpdateScrollViewContainer(previousHeight, targetHeight);

                if (m_WaitingCache.Count == 0)
                {
                    return true;
                }
            }

            return m_WaitingCache.Remove(item.index) && m_WaitingCache.Count == 0;
        }

        internal override T GetOrMakeItemAtIndex(int activeItemIndex = -1, int scrollViewIndex = -1)
        {
            var item = base.GetOrMakeItemAtIndex(activeItemIndex, scrollViewIndex);
            item.onGeometryChanged += m_GeometryChangedCallback;
            return item;
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

        internal override void StartDragItem(ReusableCollectionItem item)
        {
            m_WaitingCache.Remove(item.index);
            base.StartDragItem(item);

            // We don't need to track geometry changes for the dragged item
            m_DraggedItem.onGeometryChanged -= m_GeometryChangedCallback;
        }

        internal override void EndDrag(int dropIndex)
        {
            // Update item height of active items to support index changes (reordering).
            var draggingDown = m_DraggedItem.index < dropIndex;
            var startIndex = m_DraggedItem.index;
            var increment = draggingDown ? 1 : -1;
            var startItemHeight = GetExpectedItemHeight(startIndex);
            for (var i = startIndex; i != dropIndex; i += increment)
            {
                var height = GetExpectedItemHeight(i);
                var nextHeight = GetExpectedItemHeight(i + increment);
                if (Mathf.Approximately(height, nextHeight))
                    continue;

                RegisterItemHeight(i, nextHeight);
            }

            RegisterItemHeight(draggingDown ? dropIndex - 1 : dropIndex, startItemHeight);

            // With the new updated height, we need to grab anchors on the new first element if needed.
            if (firstVisibleIndex > m_DraggedItem.index)
            {
                firstVisibleIndex = GetFirstVisibleItem(serializedData.scrollOffset.y);
                UpdateAnchor();
            }

            // Restore the geometry changed event.
            m_DraggedItem.onGeometryChanged += m_GeometryChangedCallback;

            // Clear the dragged item reference.
            base.EndDrag(dropIndex);
        }

        void HideItem(int activeItemsIndex)
        {
            var item = m_ActiveItems[activeItemsIndex];
            item.rootElement.style.display = DisplayStyle.None;
            m_WaitingCache.Remove(item.index);
        }

        void MarkWaitingForLayout(T item)
        {
            // We don't wait for the layout of the dragged item, it will stay hidden.
            if (item.isDragGhost)
                return;

            m_WaitingCache.Add(item.index);

            // This will ensure the GeometryChangedEvent is triggered no matter what.
            // We depend on it to know when all items are laid out to update the contentContainer size,
            // so we need to make sure it is going to be called when we track it.
            item.rootElement.lastLayout = Rect.zero;
            item.rootElement.MarkDirtyRepaint();
        }

        bool IsIndexOutOfBounds(int i)
        {
            return m_CollectionView.itemsSource == null || i >= itemsCount;
        }
    }
}
