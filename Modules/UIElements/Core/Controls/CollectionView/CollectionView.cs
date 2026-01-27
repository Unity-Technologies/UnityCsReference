// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using System.Buffers;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements.HierarchyV2
{
    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class CollectionView : VisualElement
    {
        internal static readonly BindingId itemsSourceProperty = nameof(itemsSource);
        internal static readonly BindingId selectionTypeProperty = nameof(selectionType);
        internal static readonly BindingId selectedIndexProperty = nameof(selectedIndex);
        internal static readonly BindingId reorderableProperty = nameof(reorderable);
        internal static readonly BindingId reorderModeProperty = nameof(reorderMode);
        internal static readonly BindingId showBorderProperty = nameof(showBorder);
        internal static readonly BindingId showAlternatingRowBackgroundsProperty = nameof(showAlternatingRowBackgrounds);
        internal static readonly BindingId fixedItemHeightProperty = nameof(fixedItemHeight);
        internal static readonly BindingId selectedIndicesProperty = nameof(selectedIndices);

        VisualElement m_Container;
        ScrollContainer m_ScrollView;
        CollectionViewScroller m_VerticalScroller;
        CollectionViewScroller m_HorizontalScroller;
        CollectionViewDragger m_Dragger;
        CollectionViewLayoutConfiguration m_Configuration;
        IList m_ItemsSource;
        // This is used for filtering the unused items in the DisplayedList during the BindVisibleItems.
        LinkedList<RecycledItem> m_RefreshList = new();
        KeyboardNavigationManipulator m_NavigationManipulator;
        IVisualElementScheduledItem m_RebuildScheduled;
        IVisualElementScheduledItem m_ScrollScheduledItem;
        ICollectionDragAndDropController CreateDragAndDropController() => new ReorderableDragAndDropController(this);
        List<int> m_LastFocusedElementTreeChildIndexes = new();

        // The list of unused items - these items will be repurposed during the BindVisibleItems.
        readonly LinkedList<RecycledItem> m_FreeList = new();
        readonly CollectionViewSelection m_Selection = new();

        bool m_IsChangingScrollingParameters;
        double m_DelayedScrolledVerticalValue = 0;
        double m_ScrollValue;
        float m_FixedItemHeight = k_DefaultItemHeight;
        float m_LastHeight = -1;
        int m_FirstVisibleItemIndex;
        int m_LastFocusedElementIndex = -1;
        Vector3 m_TouchDownPosition;

        AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;
        RangeSelectionDirection m_RangeSelectionDirection = RangeSelectionDirection.None;
        SelectionType m_SelectionType;
        ListViewReorderMode m_ReorderMode;
        enum RangeSelectionDirection
        {
            Up = -1,
            None,
            Down
        }

        const float k_DefaultItemHeight = 22.0f;
        const float k_ScrollThresholdSquared = 100;
        const float k_DefaultScrollSize = 10.0f;
        const float k_Buffer = 1f;

        internal CollectionViewDragger dragger => m_Dragger;
        internal event Action reorderModeChanged;
        // Holds the item position along with the item. It is currently purposed to retain a collection of visible items
        // including some items out of bound (ghost items).
        internal readonly Dictionary<int, RecycledItem> m_IndexToItemDictionary = new();
        internal bool isRebuildScheduled => m_RebuildScheduled?.isActive == true;
        // The list of items being displayed in the CollectionView. Note, this list does not contain ghost items.
        internal LinkedList<RecycledItem> m_DisplayedList = new();
        /// <summary>
        /// Enum for current pointer processing state.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal enum pointerProcessingStateEnum
        {
            None,
            PointerDown
        }

        /// <summary>
        /// Determine what pointer state we are currently processing.
        /// </summary>
        internal pointerProcessingStateEnum pointerProcessingState
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")] get;
            private set;
        }

        /// <summary>
        /// Determine mouse button for currently processed pointer event.
        /// See <see cref="MouseButton"/> for details.
        /// </summary>
        internal int currentPointerButton
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")] get;
            private set;
        }

        /// <summary>
        /// Called when a drag operation wants to start in this collection view.
        /// </summary>
        public event Func<CanStartDragArgs, bool> canStartDrag;

        /// <summary>
        /// Called when a drag operation starts in this collection view.
        /// </summary>
        public event Func<SetupDragAndDropArgs, StartDragArgs> setupDragAndDrop;

        /// <summary>
        /// Called when a drag operation updates in this collection view.
        /// </summary>
        public event Func<HandleDragAndDropArgs, DragVisualMode> dragAndDropUpdate;

        /// <summary>
        /// Called when a drag operation is released in this collection view.
        /// </summary>
        public event Func<HandleDragAndDropArgs, DragVisualMode> handleDrop;

        /// <summary>
        /// The items data source. This property must be set for the list view to function.
        /// </summary>
        [CreateProperty]
        public IList itemsSource
        {
            get => m_ItemsSource;
            set
            {
                if (value == itemsSource)
                    return;

                m_ItemsSource = value;
                RefreshItems();
                NotifyPropertyChanged(itemsSourceProperty);
            }
        }

        /// <summary>
        /// Set the <see cref="itemsSource"/> without performing a refresh.
        /// </summary>
        /// <param name="source">The new source.</param>
        public void SetItemsSourceWithoutNotify(IList source)
        {
            m_ItemsSource = source;
        }

        /// <summary>
        /// Specifies which layout, standard or multi-column, the collection view will take shape.
        /// </summary>
        public CollectionViewLayoutConfiguration layoutConfiguration
        {
            get => m_Configuration;
            set
            {
                if (value == null || value == m_Configuration)
                    return;

                m_Configuration = value;
                m_Configuration.m_View = this;

                if (value is MultiColumnLayoutConfiguration multiColumnLayoutConfiguration)
                {
                    Insert(0, multiColumnLayoutConfiguration.CreateMultiColumnHeader());
                }

                UnbindAllItems();
                RefreshItems();
            }
        }

        /// <summary>
        /// The scroll container for the CollectionView.
        /// </summary>
        public ScrollContainer scrollView
        {
            get => m_ScrollView;
            private set => m_ScrollView = value;
        }

        /// <summary>
        /// The height of the CollectionView item.
        /// </summary>
        [CreateProperty]
        public float fixedItemHeight
        {
            get => m_FixedItemHeight;
            set
            {
                if (Math.Abs(m_FixedItemHeight - value) > float.Epsilon)
                {
                    m_FixedItemHeight = value;
                    NotifyPropertyChanged(fixedItemHeightProperty);
                }
            }
        }

        /// <summary>
        /// Controls the selection type.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="SelectionType.Single"/>.
        /// When you set the collection view to disable selections, any current selection is cleared.
        /// </remarks>
        [CreateProperty]
        public SelectionType selectionType
        {
            get => m_SelectionType;
            set
            {
                var previous = m_SelectionType;
                m_SelectionType = value;

                if (m_SelectionType == SelectionType.None)
                {
                    ClearSelection();
                }
                else if (m_SelectionType == SelectionType.Single)
                {
                    if (m_Selection.indexCount > 1)
                        SetSelection(m_Selection.FirstIndex());
                }

                if (previous != m_SelectionType)
                    NotifyPropertyChanged(selectionTypeProperty);
            }
        }

        /// <summary>
        /// Enable this property to display a border around the collection view.
        /// </summary>
        /// <remarks>
        /// If set to true, a border appears around the scroll container that the collection view uses internally.
        /// </remarks>
        [CreateProperty]
        public bool showBorder
        {
            get => m_ScrollView.contentContainer.ClassListContains(BaseVerticalCollectionView.borderUssClassName);
            set
            {
                var previous = showBorder;
                m_ScrollView.contentContainer.EnableInClassList(BaseVerticalCollectionView.borderUssClassName, value);

                if (previous != showBorder)
                    NotifyPropertyChanged(showBorderProperty);
            }
        }

        /// <summary>
        /// This property controls whether the background colors of collection view rows alternate.
        /// Takes a value from the <see cref="AlternatingRowBackground"/> enum.
        /// </summary>
        [CreateProperty]
        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get => m_ShowAlternatingRowBackgrounds;
            set
            {
                if (m_ShowAlternatingRowBackgrounds == value)
                    return;

                m_ShowAlternatingRowBackgrounds = value;
                RefreshItems();
                NotifyPropertyChanged(showAlternatingRowBackgroundsProperty);
            }
        }

         /// <summary>
        /// Gets or sets a value that indicates whether the user can drag list items to reorder them.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c> which allows the user to drag items to and from other views
        /// when you implement <see cref="canStartDrag"/>, <see cref="setupDragAndDrop"/>, <see cref="dragAndDropUpdate"/>, and <see cref="handleDrop"/>.
        /// Set this value to <c>true</c> to allow the user to reorder items in the list.
        /// </remarks>
        [CreateProperty]
        public bool reorderable
        {
            get => m_Dragger?.dragAndDropController?.enableReordering ?? false;
            set
            {
                if (value == reorderable)
                    return;

                var previous = reorderable;
                var controller = m_Dragger.dragAndDropController;

                if (controller != null && controller.enableReordering != value)
                {
                    controller.enableReordering = value;
                    Rebuild();
                }

                if (previous != reorderable)
                    NotifyPropertyChanged(reorderableProperty);
            }
        }

        /// <summary>
        /// This property controls the drag and drop mode for the list view.
        /// </summary>
        /// <remarks>
        /// The default value is <c>Simple</c>.
        /// When this property is set to <c>Animated</c>, Unity adds drag handles in front of every item and the drag and
        /// drop manipulation pushes items with an animation when the reordering happens.
        /// Multiple item reordering is only supported with the <c>Simple</c> drag mode.
        /// </remarks>
        [CreateProperty]
        public ListViewReorderMode reorderMode
        {
            get => m_ReorderMode;
            set
            {
                if (value == m_ReorderMode)
                    return;

                m_ReorderMode = value;
                InitializeDragAndDropController(reorderable);
                reorderModeChanged?.Invoke();
                Rebuild();
                NotifyPropertyChanged(reorderModeProperty);
            }
        }

        /// <summary>
        /// Constructs a CollectionView.
        /// </summary>
        public CollectionView()
        {
            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;
            selectionType = SelectionType.Single;

            AddToClassList(BaseVerticalCollectionView.ussClassName);

            m_ScrollView = new ScrollContainer { focusable = true };
            m_Container = m_ScrollView.contentContainer;
            m_VerticalScroller = m_ScrollView.verticalScroller;
            m_VerticalScroller.RegisterValueChangedCallback(OnVerticalScrollingChangeEvent);
            m_HorizontalScroller = m_ScrollView.horizontalScroller;
            m_HorizontalScroller.RegisterValueChangedCallback(OnHorizontalScrollerChangeEvent);

            Add(m_ScrollView);

            InitializeDragAndDropController(reorderable);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
            RegisterCallback<GeometryChangedEvent>(evt => ContainerSizeChanged(evt.newRect.height, evt.newRect.width));
        }

        void OnAttachToPanelEvent(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            this.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            m_ScrollView.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.RegisterCallback<PointerUpEvent>(OnPointerUp);
            // Triggered the scroller(s) appears/disappears
            m_HorizontalScroller.RegisterCallback<GeometryChangedEvent>(OnHorizontalScrollerGeometryChange);
            m_VerticalScroller.RegisterCallback<GeometryChangedEvent>(OnVerticalScrollerGeometryChange);
        }

        void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            this.RemoveManipulator(m_NavigationManipulator);
            m_ScrollView.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            m_HorizontalScroller.UnregisterCallback<GeometryChangedEvent>(OnHorizontalScrollerGeometryChange);
            m_VerticalScroller.UnregisterCallback<GeometryChangedEvent>(OnVerticalScrollerGeometryChange);

            if (m_ScrollScheduledItem?.isActive == true)
            {
                m_ScrollScheduledItem.Pause();
                m_ScrollScheduledItem = null;
            }
        }

        void OnVerticalScrollingChangeEvent(ChangeEvent<double> evt)
        {
            if (m_IsChangingScrollingParameters)
                return;

            m_DelayedScrolledVerticalValue = m_VerticalScroller.value;
            ScheduleScroll();
            evt.StopImmediatePropagation();
        }

        void OnHorizontalScrollerChangeEvent(ChangeEvent<double> evt)
        {
            if (layoutConfiguration is MultiColumnLayoutConfiguration columnLayout)
            {
                columnLayout.header.ScrollHorizontally((float)evt.newValue);
            }
        }

        void OnHorizontalScrollerGeometryChange(GeometryChangedEvent evt)
        {
            // Only proceed if the scrollbar's height changed
            if (Mathf.Approximately(evt.oldRect.size.y, evt.newRect.size.y) || itemsSource == null)
            {
                return;
            }

            var rangeEstimate = (double)fixedItemHeight * itemsSource.Count;
            var containerHeight = m_Container.layout.height;
            var maxScrollRange = rangeEstimate > containerHeight ? Math.Abs(rangeEstimate - containerHeight) : 0;

            // Update the high value and scroll value of the vertical scroller
            SetScrollingParameters(m_ScrollValue, maxScrollRange);
            // We need to reset the offset that is added by the horizontal scrollbar's appearance
            if (evt.newRect.size == new Vector2())
            {
                m_ScrollView.containerOffset = new Vector2(0, 0);
            }
            // Update the visibility and factor of the vertical scroller when the horizontal one appears
            UpdateVerticalScrollRange();
        }

        void OnVerticalScrollerGeometryChange(GeometryChangedEvent evt)
        {
            // Only proceed if the scrollbar's width changed
            if (Mathf.Approximately(evt.oldRect.size.x, evt.newRect.size.x) || itemsSource == null)
            {
                return;
            }

            // Refresh the vertical scroll range (resolves the annoying issue of, we need to wait for the next frame to get the right dimension)
            UpdateVerticalScrollRange();
        }

        internal void UpdateVerticalScrollValue(double value)
        {
            if (!m_VerticalScroller.Approximately(value, m_ScrollValue))
            {
                m_VerticalScroller.value = value;
                m_ScrollValue = m_VerticalScroller.value;
                BindVisibleItems();
            }
        }

        void SetScrollingParameters(double currentScrollOffset, double maxScrollRange)
        {
            var wasChangingScrollingParameters = m_IsChangingScrollingParameters;
            m_IsChangingScrollingParameters = true;

            currentScrollOffset = Math.Min(currentScrollOffset, maxScrollRange);

            try
            {
                // here we don`t want to react to further value changes in order not to trigger an
                // infinite loop
                using (new EventDispatcherGate(panel.dispatcher))
                {
                    m_VerticalScroller.highValue = maxScrollRange;
                    m_VerticalScroller.value = currentScrollOffset;
                    m_ScrollValue = m_VerticalScroller.value;
                }
            }
            finally
            {
                m_IsChangingScrollingParameters = wasChangingScrollingParameters;
            }
        }

        void ScheduleScroll()
        {
            if (m_ScrollScheduledItem == null)
                m_ScrollScheduledItem = schedule.Execute(OnDelayedScroll);
            else if (!m_ScrollScheduledItem.isActive)
                m_ScrollScheduledItem.Resume();
        }

        void OnDelayedScroll()
        {
            UpdateVerticalScrollValue(m_DelayedScrolledVerticalValue);
            // Reset the delayed value
            m_DelayedScrolledVerticalValue = 0;
        }

        void ContainerSizeChanged(float height, float width)
        {
            if (!Mathf.Approximately(m_LastHeight, height))
            {
                m_LastHeight = height;
                RefreshItems();
            }
        }

        void UnbindItem(RecycledItem item)
        {
            if (item == null)
                return;

            var index = item.index;
            item.index = -1;

            m_IndexToItemDictionary.Remove(index);
            layoutConfiguration.unbindCell?.Invoke(item.element, index);
        }

        internal void OnDestroyItem(RecycledItem item)
        {
            layoutConfiguration.destroyCell?.Invoke(item.element);
        }

        void BindItem(RecycledItem item, int index)
        {
            var previousIndex = item.index;

            if (m_IndexToItemDictionary.ContainsKey(item.index))
                UnbindItem(item);

            var useAlternateUss = showAlternatingRowBackgrounds != AlternatingRowBackground.None && index % 2 == 1;
            item.element.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassName, useAlternateUss);
            item.isLastItem = index == itemsSource.Count - 1;
            item.SetSelected(m_Selection.ContainsIndex(index));
            item.ClearHoverState();
            item.element.style.height = fixedItemHeight;
            item.index = index;
            m_IndexToItemDictionary.Add(index, item);

            if (index >= 0 && index < itemsSource.Count)
                layoutConfiguration.bindCell?.Invoke(item.element, index);

            HandleFocus(item, previousIndex);
        }

        /// <summary>
        /// Clears the collection view, recreates all visible visual elements, and rebinds all items.
        /// </summary>
        public void Rebuild()
        {
            m_RebuildScheduled?.Pause();

            ClearAllItems();
            RefreshItems();
        }

        /// <summary>
        /// Schedules a call to <see cref="Rebuild"/>.
        /// Calling this method multiple times will only schedule one rebuild.
        /// </summary>
        internal void ScheduleRebuild()
        {
            if (m_RebuildScheduled == null)
                m_RebuildScheduled = schedule.Execute(Rebuild);
            else if (!m_RebuildScheduled.isActive)
                m_RebuildScheduled.Resume();
        }

        /// <summary>
        /// Event fired at the beginning of <see cref="RefreshItems"/> to allow users to make sure the underlying data is ready to be iterated on.
        /// </summary>
        public event Action BeforeRefreshingItems;

        /// <summary>
        /// Rebinds all items currently visible.
        /// </summary>
        public void RefreshItems()
        {
            BeforeRefreshingItems?.Invoke();

            // Clean up all items if itemsSource is null or empty, or makeCell is null
            if (itemsSource == null || layoutConfiguration?.makeCell == null || itemsSource.Count == 0)
            {
                m_VerticalScroller.style.display = DisplayStyle.None;
                ClearAllItems();
                return;
            }

            // If a Rebuild is scheduled then let it handle the refresh.
            if (m_RebuildScheduled?.isActive == true)
            {
                Rebuild();
                return;
            }

            var height = m_Container.resolvedStyle.height;
            if (float.IsNaN(height))
                return;

            m_LastHeight = height;

            var numberOfVisibleItems = (int)(m_Container.layout.height / fixedItemHeight);
            if (itemsSource.Count - 1 < numberOfVisibleItems)
                m_ScrollValue = 0;

            var rangeEstimate = (double)fixedItemHeight * itemsSource.Count;
            var maxScrollRange = rangeEstimate > height ? Math.Abs(rangeEstimate - height) : 0;
            m_VerticalScroller.style.display = rangeEstimate > height - m_HorizontalScroller.worldBound.height ? DisplayStyle.Flex : DisplayStyle.None;
            SetScrollingParameters(m_ScrollValue, maxScrollRange);
            BindVisibleItems(true);
        }

        void ClearAllItems()
        {
            while (m_DisplayedList.Count > 0)
            {
                ClearItem(m_DisplayedList.First);
            }

            while (m_FreeList.Count > 0)
            {
                ClearItem(m_FreeList.First);
            }

            RecycledItem.ClearItemPool();
        }

        [EventInterest(typeof(PointerUpEvent), typeof(FocusInEvent), typeof(FocusOutEvent), typeof(NavigationSubmitEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            // We always need to know when pointer up event occurred to reset DragEventsProcessor flags.
            if (evt.eventTypeId == PointerUpEvent.TypeId())
            {
                m_Dragger?.OnPointerUpEvent((PointerUpEvent)evt);
            }
            // We need to store the focused item in order to be able to scroll out and back to it, without
            // seeing the focus affected. To do so, we store the path to the tree element that is focused,
            // and set it back in Setup().
            else if (evt.eventTypeId == FocusInEvent.TypeId())
            {
                OnFocusIn(evt.elementTarget);
            }
            else if (evt.eventTypeId == FocusOutEvent.TypeId())
            {
                OnFocusOut(((FocusOutEvent)evt).relatedTarget as VisualElement);
            }
            else if (evt.eventTypeId == NavigationSubmitEvent.TypeId())
            {
                if (evt.target == this)
                {
                    m_ScrollView.Focus();
                }
            }
        }

        void OnFocusIn(VisualElement leafTarget)
        {
            if (leafTarget == m_ScrollView)
                return;

            m_LastFocusedElementTreeChildIndexes.Clear();

            if (m_ScrollView.contentContainer.FindElementInTree(leafTarget, m_LastFocusedElementTreeChildIndexes))
            {
                var recycledElement = m_ScrollView.contentContainer[m_LastFocusedElementTreeChildIndexes[0]];
                foreach (var recycledItem in m_IndexToItemDictionary.Values)
                {
                    if (recycledItem.element == recycledElement)
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

        void OnFocusOut(VisualElement willFocus)
        {
            // Focus lost and the about-to-be-focused VisualElement is not part of the VerticalVirtualizationController.
            if (willFocus == null || willFocus != m_ScrollView)
            {
                m_LastFocusedElementTreeChildIndexes.Clear();
                m_LastFocusedElementIndex = -1;
            }
        }

        void HandleFocus(RecycledItem recycledItem, int previousIndex)
        {
            if (m_LastFocusedElementIndex == -1)
                return;

            if (m_LastFocusedElementIndex == recycledItem.index)
                recycledItem.element.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Focus();
            else if (m_LastFocusedElementIndex != previousIndex)
                recycledItem.element.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Blur();
            else
                m_ScrollView.Focus();
        }

        void ClearItem(LinkedListNode<RecycledItem> item)
        {
            item.List.Remove(item);
            UnbindItem(item.Value);
            RecycledItem.Recycle(item.Value);
        }

        void UnbindAllItems()
        {
            using var _ = ListPool<int>.Get(out var indices);
            foreach (var key in m_IndexToItemDictionary.Keys)
                indices.Add(key);

            foreach (var index in indices)
                UnbindItem(m_IndexToItemDictionary[index]);
        }

        void UpdateVerticalScrollRange()
        {
            var firstVisibleIndex = 0;
            var lastVisibleIndex = -1;

            if (m_DisplayedList.Count > 0)
            {
                firstVisibleIndex = m_DisplayedList.First.Value.index;
                var current = m_DisplayedList.First;

                // We update the scrolling speed so we scroll down by 1/2 of a standard item per click
                m_VerticalScroller.scrollSize = k_DefaultScrollSize * fixedItemHeight / fixedItemHeight;

                var lastVisibleItem = m_DisplayedList.First;
                var maxOffset = m_LastHeight - m_ScrollView.containerOffset.y;

                while (current != null && current.Value.verticalOffset + fixedItemHeight < maxOffset)
                {
                    lastVisibleItem = current;
                    current = current.Next;
                }

                if (lastVisibleItem != null)
                    lastVisibleIndex = lastVisibleItem.Value.index;
            }

            var visibleRange = lastVisibleIndex - firstVisibleIndex + 1;
            if (visibleRange > 0 && itemsSource != null)
            {
                // This cast is required for the floating precision which causes the incorrect drag element's height
                var ratio = (double)visibleRange / itemsSource.Count;
                var rangeEstimate = (double)fixedItemHeight * itemsSource.Count;
                // In the events that we go through this code path outside the refresh
                m_VerticalScroller.style.display = rangeEstimate > m_Container.worldBound.height ? DisplayStyle.Flex : DisplayStyle.None;
                m_VerticalScroller.factor = (float) (Math.Abs(ratio - 1) < UIRUtility.k_Epsilon ? m_ScrollView.viewport.layout.height / m_Container.boundingBox.height : ratio);
            }
        }

        void UpdateHorizontalScrollRange()
        {
            var maxWidth = 0f;
            var verticalScrollerWidth = m_VerticalScroller.worldBound.width;

            if (layoutConfiguration is MultiColumnLayoutConfiguration columnLayout)
            {
                maxWidth = columnLayout.header.columnContainer.layout.size.x - verticalScrollerWidth;
            }
            else
            {
                foreach (var displayItem in m_DisplayedList)
                {
                    maxWidth = Mathf.Max(maxWidth, displayItem.element.worldBoundingBox.width - verticalScrollerWidth);
                }
            }

            m_HorizontalScroller.style.display = maxWidth > m_Container.rect.width ? DisplayStyle.Flex : DisplayStyle.None;
            m_HorizontalScroller.lowValue = 0;
            m_HorizontalScroller.highValue = maxWidth - m_Container.rect.width;
            m_HorizontalScroller.scrollSize = k_DefaultScrollSize * m_Container.rect.width;
            m_HorizontalScroller.factor = maxWidth > UIRUtility.k_Epsilon ? m_Container.worldBound.width / maxWidth : 1f;
        }

        void BindVisibleItems(bool forceBindItem = false)
        {
            var height = m_LastHeight;
            var visibleCount = (int)Mathf.Ceil(height / fixedItemHeight) + 3;
            m_FirstVisibleItemIndex = (int)(m_ScrollValue / fixedItemHeight);

            // The items should be in the map, we swap the refresh and the displayed list
            (m_DisplayedList, m_RefreshList) = (m_RefreshList, m_DisplayedList);

            // We do a first pass to remove in-use elements from  m_RefreshList and add them to the FreeList
            for (var i = 0; i < visibleCount; ++i)
            {
                var index = m_FirstVisibleItemIndex + i;

                if (index < 0 || index > itemsSource.Count - 1)
                    continue;

                if (m_IndexToItemDictionary.TryGetValue(index, out var value))
                {
                    // We remove it from the RefreshList
                    if (ReferenceEquals(value.node.List, m_RefreshList))
                    {
                        value.node.List.Remove(value.node);
                    }
                }
            }

            // We add unused items to the FreeList
            while (m_RefreshList.Count > 0)
            {
                var item = m_RefreshList.First;
                if (item != null)
                {
                    m_RefreshList.RemoveFirst();
                    m_FreeList.AddLast(item);
                }
            }

            AddElementsFromIndex(m_FirstVisibleItemIndex, visibleCount, forceBindItem);

            foreach (var item in m_FreeList)
            {
                item.element.style.display = DisplayStyle.None;
            }
        }

        void AddElementsFromIndex(int firstIndex, int itemCount, bool forceBindItem = false)
        {
            var lastIndex = firstIndex + itemCount;

            // We now fill the Display List
            for (var index = firstIndex; index < lastIndex; ++index)
            {
                if (index < 0 || index > itemsSource.Count - 1)
                    continue;

                if (m_IndexToItemDictionary.TryGetValue(index, out var itemWrapper))
                {
                    // We already have this somewhere therefore we remove it from the Free/RefreshLists
                    itemWrapper.node.List?.Remove(itemWrapper.node);
                }
                else
                {
                    if (m_FreeList.Count > 0)
                    {
                        itemWrapper = m_FreeList.First.Value;
                        m_FreeList.RemoveFirst();
                    }
                    else
                    {
                        var itemElement = layoutConfiguration.makeCell?.Invoke();
                        itemWrapper = RecycledItem.AllocateItem(itemElement, this);
                        m_Container.Add(itemElement);
                    }
                }

                if (forceBindItem || itemWrapper.index != index)
                {
                    BindItem(itemWrapper, index);
                }

                // Since we are hiding the reusable items instead of removing it, we need to make them visible again.
                itemWrapper.element.style.display = DisplayStyle.Flex;

                m_DisplayedList.AddLast(itemWrapper.node);
            }

            UpdateContainerOffset();

            if (m_DisplayedList.Count > 0)
            {
                RecycledItem.UpdatePositions(m_DisplayedList.First.Value);
            }
        }

        void UpdateContainerOffset()
        {
            var offset = m_ScrollView.containerOffset;
            var offVertical = (float)(m_ScrollValue % fixedItemHeight);
            offset.y = offVertical;
            // If there's a scrollable space, we retain the old offset, otherwise we reset to 0.
            offset.x = m_HorizontalScroller.highValue > 0 ? offset.x : 0;
            m_ScrollView.containerOffset = offset;
        }

        internal void UpdateScrollingRangeAfterLayout()
        {
            UpdateHorizontalScrollRange();

            var item = m_DisplayedList.Last;
            if (item != null)
            {
                var itemValue = item.Value;
                var offset = m_ScrollView.containerOffset.y;
                var bottom = itemValue.verticalOffset + fixedItemHeight;
                var height = m_Container.resolvedStyle.height;

                if (itemValue.isLastItem)
                {
                    var firstItemCandidate = item;
                    var currentSpace = height;
                    var currentTop = currentSpace - fixedItemHeight;

                    while (currentTop > 0 && firstItemCandidate.Previous != null)
                    {
                        firstItemCandidate = firstItemCandidate.Previous;
                        currentTop -= fixedItemHeight;
                    }

                    if (currentTop <= 0)
                    {
                        // We get the ratio of the first visible item
                        var ratio = -currentTop / fixedItemHeight;
                        var firstID = firstItemCandidate.Value.index;
                        var totalRange = (firstID + ratio) * fixedItemHeight;
                        SetScrollingParameters(Math.Min(m_ScrollValue, totalRange), totalRange);
                    }
                }
                else
                {
                    // Here we used to set the average item height but turns out we don't really need it
                    // we make sure that there is no empty space left without content
                    if (bottom + offset < (height - k_Buffer))
                    {
                        var emptySpace = height - (bottom - offset);
                        var toAdd = Mathf.CeilToInt(emptySpace / fixedItemHeight);
                        toAdd = Math.Clamp(toAdd, 0, itemsSource.Count - itemValue.index - 1);

                        if (toAdd > 0)
                        {
                            AddElementsFromIndex(itemValue.index + 1, toAdd);
                            return;
                        }
                    }
                }

                UpdateVerticalScrollRange();
            }
        }

        /// <summary>
        /// Returns the item's index from based on the provided position.
        /// </summary>
        /// <param name="position">The position of the item.</param>
        /// <returns>The index of the item.</returns>
        public int GetIndexFromPosition(Vector2 position)
        {
            var itemHeight = AlignmentUtils.RoundToPixelGrid(fixedItemHeight, scaledPixelsPerPoint);
            var positionY = m_ScrollValue + position.y;
            return (int)(positionY / itemHeight);
        }

        /// <summary>
        /// Scroll the CollectionView to the item at the provided index.
        /// </summary>
        /// <remarks>
        /// If -1 is passed, it will scroll to the last item of the list.
        /// </remarks>
        /// <param name="index">The item index.</param>
        public void ScrollToItem(int index)
        {
            if (index < RecycledItem.k_UndefinedIndex || index > itemsSource.Count)
                return;

            if (index == -1)
                index = itemsSource.Count - 1;

            if (m_FirstVisibleItemIndex >= index)
            {
                UpdateVerticalScrollValue(fixedItemHeight * index);
                return;
            }

            var numberOfVisibleItems = (int)(m_Container.layout.height / fixedItemHeight);
            if (index < m_FirstVisibleItemIndex + numberOfVisibleItems)
                return;

            var visibleOffset = fixedItemHeight - (m_Container.layout.height - numberOfVisibleItems * fixedItemHeight);
            var yScrollOffset = fixedItemHeight * (index - numberOfVisibleItems) + visibleOffset;
            UpdateVerticalScrollValue(yScrollOffset);
        }

        /// <summary>
        /// Gets the root element of the specified collection view item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The item's root element.</returns>
        public VisualElement GetRootElementForIndex(int index)
        {
            foreach (var item in m_DisplayedList)
            {
                if (item.index == index)
                    return item.element;
            }
            return null;
        }

        /// <summary>
        /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
        /// first selected item's index. If multiple items are provided, sets them all as selected.
        /// </summary>
        [CreateProperty]
        public int selectedIndex
        {
            get => m_Selection.FirstIndex();
            set => SetSelection(value);
        }

        /// <summary>
        /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public IEnumerable<int> selectedIndices => m_Selection.indices;

        public bool hasSelection => m_Selection.indices.Count > 0;

        public bool IsSelected(int index) => m_Selection.ContainsIndex(index);

        void NotifyOfSelectionChange()
        {
            selectedIndicesChanged?.Invoke();
        }

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item index or item indices selected.
        /// </remarks>
        public event Action selectedIndicesChanged;

        void OnPointerUp(IPointerEvent evt)
        {
            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse && evt.button != (int)MouseButton.RightMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                var delta = evt.position - m_TouchDownPosition;
                if (delta.sqrMagnitude <= k_ScrollThresholdSquared)
                {
                    DoSelect(evt.localPosition, evt.actionKey, evt.shiftKey);
                }
            }
            else
            {
                var clickedIndex = GetIndexFromPosition(evt.localPosition);
                if (selectionType == SelectionType.Multiple
                    && evt.button == (int)MouseButton.LeftMouse
                    && !evt.shiftKey
                    && !evt.actionKey
                    && m_Selection.indexCount > 1
                    && m_Selection.ContainsIndex(clickedIndex))
                {
                    SetSelection(clickedIndex);
                }
            }
        }

        void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!evt.isPrimary)
                return;

            ClearSelection();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) == 0)
                {
                    OnPointerDown(evt);
                }
                else
                {
                    OnPointerUp(evt);
                }
            }
        }

        void OnPointerDown(IPointerEvent evt)
        {
            pointerProcessingState = pointerProcessingStateEnum.PointerDown;
            try
            {
                if (!evt.isPrimary)
                    return;

                if (evt.button != (int)MouseButton.LeftMouse && evt.button != (int)MouseButton.RightMouse)
                    return;

                currentPointerButton = evt.button;

                if (evt.pointerType != PointerType.mouse)
                {
                    m_TouchDownPosition = evt.position;
                    return;
                }

                DoSelect(evt.localPosition, evt.actionKey, evt.shiftKey);
            }
            finally
            {
                pointerProcessingState = pointerProcessingStateEnum.None;
                currentPointerButton = -1;
            }
        }

        void DoSelect(Vector2 localPosition, bool actionKey, bool shiftKey)
        {
            var clickedIndex = GetIndexFromPosition(localPosition);
            if (clickedIndex > itemsSource.Count - 1)
                return;

            m_RangeSelectionDirection = RangeSelectionDirection.None;

            switch (selectionType)
            {
                case SelectionType.None:
                    return;
                case SelectionType.Multiple when actionKey:
                {
                    // Add/remove single clicked element
                    if (m_Selection.ContainsIndex(clickedIndex))
                        RemoveFromSelection(clickedIndex);
                    else
                        AddToSelection(clickedIndex);
                    break;
                }
                case SelectionType.Multiple when shiftKey:
                {
                    if (m_Selection.indexCount == 0)
                        SetSelection(clickedIndex);
                    else
                        DoRangeSelection(clickedIndex);

                    break;
                }
                case SelectionType.Multiple when m_Selection.ContainsIndex(clickedIndex):
                case SelectionType.Single when m_Selection.ContainsIndex(clickedIndex):
                    break;
                default:
                    SetSelection(clickedIndex);
                    break;
            }
        }

        void DoRangeSelection(int rangeSelectionFinalIndex)
        {
            if (rangeSelectionFinalIndex < 0 || rangeSelectionFinalIndex >= itemsSource.Count)
                return;

            var min = m_Selection.minIndex;
            var max = m_Selection.maxIndex;
            switch (m_RangeSelectionDirection)
            {
                case RangeSelectionDirection.Up:
                    min = rangeSelectionFinalIndex;
                    break;

                case RangeSelectionDirection.Down:
                    max = rangeSelectionFinalIndex;
                    break;

                default:
                    min = Mathf.Min(min, rangeSelectionFinalIndex);
                    max = Mathf.Max(max, rangeSelectionFinalIndex);
                    break;
            }

            // Reset direction if we're back to a single selection
            if (min == max)
                m_RangeSelectionDirection = RangeSelectionDirection.None;

            var count = max - min + 1;
            if (count <= 0)
                return;

            var newSelection = ArrayPool<int>.Shared.Rent(count);
            try
            {
                for (var i = 0; i < count; ++i)
                    newSelection[i] = min + i;

                ClearSelectionWithoutValidation();
                AddToSelection(newSelection.AsSpan(0, count));
            }
            finally
            {
                ArrayPool<int>.Shared.Return(newSelection);
            }
        }

        void AddToSelection(ReadOnlySpan<int> indices)
        {
            if (indices.Length == 0)
                return;

            foreach (var index in indices)
            {
                AddToSelectionWithoutValidation(index);
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        void AddToSelectionWithoutValidation(int index)
        {
            if (m_Selection.ContainsIndex(index))
                return;

            if (m_IndexToItemDictionary.TryGetValue(index, out var recycleItem))
                recycleItem.SetSelected(true);

            m_Selection.AddIndex(index);
        }

        /// <summary>
        /// Adds an item to the collection of selected items.
        /// </summary>
        /// <param name="index">Item index.</param>
        public void AddToSelection(int index)
        {
            AddToSelection(stackalloc int[] { index });
        }

        /// <summary>
        /// Removes an item from the collection of selected items.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RemoveFromSelection(int index)
        {
            if (!m_Selection.TryRemove(index))
                return;

            if (m_IndexToItemDictionary.TryGetValue(index, out var recycleItem))
                recycleItem.SetSelected(false);

            m_Selection.TryRemove(index);
            NotifyOfSelectionChange();
            SaveViewData();
        }

        /// <summary>
        /// Sets a collection of selected items.
        /// </summary>
        /// <param name="indices">The collection of the indices of the items to be selected.</param>
        public void SetSelection(IReadOnlyList<int> indices)
        {
            SetSelectionInternal(indices, true);
        }

        public void SetSelection(ReadOnlySpan<int> indices) => SetSelectionInternal(indices, true);

        /// <summary>
        /// Sets the currently selected item.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void SetSelection(int index)
        {
            if (index < 0)
            {
                ClearSelection();
                return;
            }

            SetSelection(stackalloc int[] { index });
        }

        /// <summary>
        /// Sets a collection of selected items without triggering a selection change callback.
        /// </summary>
        /// <param name="indices">The collection of items to be selected.</param>
        public void SetSelectionWithoutNotify(IReadOnlyList<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        public void SetSelectionWithoutNotify(ReadOnlySpan<int> indices) => SetSelectionInternal(indices, false);

        void SetSelectionInternal(IReadOnlyList<int> indices, bool sendNotification)
        {
            if (indices == null)
                return;

            var count = indices.Count;
            if (count == 0)
            {
                SetSelectionInternal(ReadOnlySpan<int>.Empty, sendNotification);
            }
            else if (count < 16)
            {
                Span<int> ints = stackalloc int[count];
                var i = 0;
                foreach (var index in indices)
                    ints[i++] = index;

                SetSelectionInternal(ints, sendNotification);
            }
            else
            {
                // if indices collection is bigger than what can be safely stackalloc-ed, get a pooled array of the correct size
                // using ArrayPool<byte> instead of <int> to allow arrays to be reused for any non blittable type
                var buffer = ArrayPool<byte>.Shared.Rent(count * sizeof(int));
                try
                {
                    var span = MemoryMarshal.Cast<byte, int>(buffer);
                    var spanLength = 0;
                    foreach (var index in indices)
                    {
                        span[spanLength++] = index;
                    }
                    span = span[..spanLength];

                    SetSelectionInternal(span, sendNotification);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            SaveViewData();
        }

        void SetSelectionInternal(ReadOnlySpan<int> indices, bool sendNotification)
        {
            if (MatchesExistingSelection(indices))
                return;

            var previousSelectedIndex = selectedIndex;
            ClearSelectionWithoutValidation();

            // If possible resize indices so we can better handle large selections. (UUM-74996)
            if (m_Selection.capacity < indices.Length)
            {
                m_Selection.capacity = indices.Length;
            }

            foreach (var index in indices)
            {
                AddToSelectionWithoutValidation(index);
            }

            if (sendNotification)
            {
                if (previousSelectedIndex != selectedIndex)
                    NotifyPropertyChanged(selectedIndexProperty);

                NotifyOfSelectionChange();
            }

            SaveViewData();
        }

        bool MatchesExistingSelection(ReadOnlySpan<int> indices)
        {
            if (indices.Length != m_Selection.indexCount)
                return false;

            var existingSelection = NoAllocHelpers.CreateReadOnlySpan(m_Selection.indices);
            return existingSelection.SequenceEqual(indices);
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            if (m_Selection.indices.Count == 0)
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        void ClearSelectionWithoutValidation()
        {
            foreach (var (_, recycledItem) in m_IndexToItemDictionary)
                recycledItem.SetSelected(false);

            m_Selection.ClearIndices();
        }

        internal virtual CollectionViewDragger CreateDragger()
        {
            return new CollectionViewDragger(this);
        }

        void InitializeDragAndDropController(bool enableReordering)
        {
            if (m_Dragger != null)
            {
                m_Dragger.UnregisterCallbacksFromTarget(true);
                m_Dragger.dragAndDropController = null;
                m_Dragger = null;
            }

            m_Dragger = CreateDragger();
            m_Dragger.dragAndDropController = CreateDragAndDropController();
            if (m_Dragger.dragAndDropController == null)
                return;

            m_Dragger.dragAndDropController.enableReordering = enableReordering;
        }

        internal void SetDragAndDropController(ICollectionDragAndDropController dragAndDropController)
        {
            m_Dragger ??= CreateDragger();
            m_Dragger.dragAndDropController = dragAndDropController;
        }

        internal bool HasCanStartDrag() => canStartDrag != null;

        internal bool RaiseCanStartDrag(RecycledItem item, IEnumerable<int> indices, EventModifiers modifiers)
        {
            return canStartDrag?.Invoke(new CanStartDragArgs(item.element, item.index, indices, modifiers)) ?? true;
        }

        internal StartDragArgs RaiseSetupDragAndDrop(RecycledItem item, IEnumerable<int> indices, StartDragArgs args)
        {
            return setupDragAndDrop?.Invoke(new SetupDragAndDropArgs(item.element, indices, args)) ?? args;
        }

        internal DragVisualMode RaiseHandleDragAndDrop(Vector2 pointerPosition, DragAndDropArgs dragAndDropArgs)
        {
            return dragAndDropUpdate?.Invoke(new HandleDragAndDropArgs(pointerPosition, dragAndDropArgs)) ?? DragVisualMode.None;
        }

        internal DragVisualMode RaiseDrop(Vector2 pointerPosition, DragAndDropArgs dragAndDropArgs)
        {
            return handleDrop?.Invoke(new HandleDragAndDropArgs(pointerPosition, dragAndDropArgs)) ?? DragVisualMode.None;
        }

        /// <summary>
        /// Moves an item in the source.
        /// </summary>
        /// <param name="index">The source index.</param>
        /// <param name="newIndex">The destination index.</param>
        internal void Move(int index, int newIndex)
        {
            if (itemsSource == null)
                return;

            if (index == newIndex)
                return;

            var minIndex = Mathf.Min(index, newIndex);
            var maxIndex = Mathf.Max(index, newIndex);

            if (minIndex < 0 || maxIndex >= itemsSource.Count)
                return;

            var direction = newIndex < index ? 1 : -1;

            while (minIndex < maxIndex)
            {
                Swap(index, newIndex);
                newIndex += direction;

                if (index < newIndex)
                {
                    minIndex = index;
                    maxIndex = newIndex;
                }
                else
                {
                    maxIndex = index;
                    minIndex = newIndex;
                }
            }
        }

        void Swap(int lhs, int rhs)
        {
            (itemsSource[lhs], itemsSource[rhs]) = (itemsSource[rhs], itemsSource[lhs]);
        }

        void SelectAll()
        {
            if (selectionType != SelectionType.Multiple)
                return;

            for (var index = 0; index < itemsSource.Count; index++)
            {
                m_Selection.AddIndex(index);
            }

            foreach (var recycledItem in m_IndexToItemDictionary.Values)
            {
                recycledItem.SetSelected(true);
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        bool Apply(KeyboardNavigationOperation op, bool shiftKey)
        {
            if (selectionType == SelectionType.None)
            {
                return false;
            }

            void HandleSelectionAndScroll(int index)
            {
                if (index < 0 || index >= itemsSource.Count)
                    return;

                if (selectionType == SelectionType.Multiple && shiftKey && m_Selection.indexCount != 0)
                {
                    DoRangeSelection(index);
                }
                else
                {
                    m_RangeSelectionDirection = RangeSelectionDirection.None;
                    selectedIndex = index;
                }

                ScrollToItem(index);
            }

            switch (op)
            {
                case KeyboardNavigationOperation.SelectAll:
                    SelectAll();
                    return true;
                case KeyboardNavigationOperation.Cancel:
                    ClearSelection();
                    return true;
                case KeyboardNavigationOperation.Submit:
                    ScrollToItem(selectedIndex);
                    return true;
                case KeyboardNavigationOperation.Previous:
                {
                    var index = (m_Selection.indexCount == 0 ? -1 :
                        m_RangeSelectionDirection != RangeSelectionDirection.Down ? m_Selection.minIndex :
                        m_Selection.maxIndex) - 1;
                    if (index >= 0)
                    {
                        if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                            m_RangeSelectionDirection = RangeSelectionDirection.Up;

                        HandleSelectionAndScroll(index);
                        return true;
                    }
                    break; // Allow focus to move outside the CollectionView
                }
                case KeyboardNavigationOperation.Next:
                {
                    var index = (m_Selection.indexCount == 0 ? -1 : m_RangeSelectionDirection != RangeSelectionDirection.Up ? m_Selection.maxIndex : m_Selection.minIndex) + 1;
                    if (index < itemsSource.Count)
                    {
                        if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                            m_RangeSelectionDirection = RangeSelectionDirection.Down;

                        HandleSelectionAndScroll(index);
                        return true;
                    }
                    break; // Allow focus to move outside the CollectionView
                }
                case KeyboardNavigationOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case KeyboardNavigationOperation.End:
                    HandleSelectionAndScroll(itemsSource.Count - 1);
                    return true;
                case KeyboardNavigationOperation.PageDown:
                    if (m_Selection.indexCount > 0)
                    {
                        if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                            m_RangeSelectionDirection = RangeSelectionDirection.Down;

                        var selectionDown = m_RangeSelectionDirection == RangeSelectionDirection.Up ? m_Selection.minIndex : m_Selection.maxIndex;
                        HandleSelectionAndScroll(Mathf.Min(itemsSource.Count - 1, selectionDown + (m_DisplayedList.Count - 1)));
                    }
                    return true;
                case KeyboardNavigationOperation.PageUp:
                    if (m_Selection.indexCount > 0)
                    {
                        if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                            m_RangeSelectionDirection = RangeSelectionDirection.Up;

                        var selectionUp = m_RangeSelectionDirection == RangeSelectionDirection.Up ? m_Selection.minIndex : m_Selection.maxIndex;
                        HandleSelectionAndScroll(Mathf.Max(0, selectionUp - (m_DisplayedList.Count - 1)));
                    }
                    return true;
                case KeyboardNavigationOperation.MoveRight:
                case KeyboardNavigationOperation.MoveLeft:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            return false;
        }

        void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            var shiftKey = sourceEvent is KeyDownEvent { shiftKey: true } or INavigationEvent { shiftKey: true };
            if (Apply(op, shiftKey))
            {
                sourceEvent.StopPropagation();
            }

            focusController?.IgnoreEvent(sourceEvent);
        }
    }
}

