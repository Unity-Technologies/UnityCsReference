// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using System.Buffers;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements.HierarchyV2
{
    [VisibleToOtherModules("UnityEngine.HierarchyModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class CollectionView : VisualElement
    {
        internal static readonly BindingId itemsSourceProperty = nameof(itemsSource);
        internal static readonly BindingId selectionTypeProperty = nameof(selectionType);
        internal static readonly BindingId reorderableProperty = nameof(reorderable);
        internal static readonly BindingId reorderModeProperty = nameof(reorderMode);
        internal static readonly BindingId showBorderProperty = nameof(showBorder);
        internal static readonly BindingId showAlternatingRowBackgroundsProperty = nameof(showAlternatingRowBackgrounds);
        internal static readonly BindingId fixedItemHeightProperty = nameof(fixedItemHeight);

        VisualElement m_Container;
        VisualElement m_ContainerClip;
        VisualElement m_StickyRowContainer;
        VisualElement m_BackgroundFill;
        RecycledItem m_StickyRow;
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
        IVisualElementScheduledItem m_RefreshScheduled;
        IVisualElementScheduledItem m_ScrollScheduledItem;
        ICollectionDragAndDropController CreateDragAndDropController() => new ReorderableDragAndDropController(this);
        IStickyRowController m_StickyRowController;
        List<int> m_LastFocusedElementTreeChildIndexes = new();

        // The list of unused items - these items will be repurposed during the BindVisibleItems.
        readonly LinkedList<RecycledItem> m_FreeList = new();
        readonly ICollectionViewSelectionContainer m_Selection;
        public ICollectionViewSelectionContainer selection => m_Selection;

        bool m_IsChangingScrollingParameters;
        double m_DelayedScrolledVerticalValue = 0;
        double m_ScrollValue;
        float m_FixedItemHeight = k_DefaultItemHeight;
        float m_LastHeight = -1;
        float m_StickyHeight;
        int m_FirstVisibleItemIndex;
        int m_LastFocusedElementIndex = -1;
        Vector3 m_TouchDownPosition;

        AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;
        RangeSelectionDirection m_RangeSelectionDirection = RangeSelectionDirection.None;
        SelectionType m_SelectionType = SelectionType.Single;
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

        internal double scrollValue => m_ScrollValue;
        internal VisualElement itemContainer => m_Container;
        internal VisualElement backgroundFill => m_BackgroundFill;
        internal RecycledItem currentStickyRow => m_StickyRow;
        internal bool hasActiveStickyRow => m_StickyRow != null && m_StickyRow.index != -1;
        internal CollectionViewDragger dragger => m_Dragger;
        internal event Action reorderModeChanged;
        // Holds the item position along with the item. It is currently purposed to retain a collection of visible items
        // including some items out of bound (ghost items).
        internal readonly Dictionary<int, RecycledItem> m_IndexToItemDictionary = new();
        internal bool isRebuildScheduled => m_RebuildScheduled?.isActive == true;
        // The list of items being displayed in the CollectionView. Note, this list does not contain ghost items.
        internal LinkedList<RecycledItem> m_DisplayedList = new();

        ICollectionViewAnimation m_Animation;
        ItemAnimationInfo? m_ActiveAnimationBatch;
        // Extra items bound below the viewport during animation; cleared by the strategy.
        int m_ExtraBindCount;

        /// <summary>
        /// True when <paramref name="index"/> is the currently-pinned sticky row (its data
        /// position is scrolled past the top of the viewport).
        /// </summary>
        public bool IsStickyPinned(int index) => m_StickyRow != null && m_StickyRow.index == index;

        /// <summary>
        /// Approximate number of items that fit in the viewport. Used to cap animation-related
        /// force-binding so it does not scale with batch size on large hierarchies.
        /// </summary>
        public int visibleViewportCount => (m_LastHeight > 0 && fixedItemHeight > 0)
            ? Math.Max(1, (int)Mathf.Ceil(m_LastHeight / fixedItemHeight))
            : 1;

        /// <summary>Fired on every terminal state of the active animation (completion, skip, reversal end).</summary>
        public event Action animationCompleted;

        /// <summary>
        /// Strategy that drives appear/disappear animations. <c>null</c> = instant.
        /// </summary>
        public ICollectionViewAnimation animation
        {
            get => m_Animation;
            set
            {
                if (m_Animation == value)
                    return;

                m_Animation?.SkipAnimation();
                m_Animation = value;
                if (m_Animation == null)
                    return;

                var context = new CollectionViewAnimationContext
                {
                    recycledItemForIndex = GetRecycledItemForIndex,
                    itemContainer = m_Container,
                    clipParent = m_ScrollView.contentContainer,
                    scheduleRefresh = ScheduleRefreshItems,
                    clearBindWindow = ClearAnimationBindWindow,
                    onAnimationCompleted = RaiseAnimationCompleted,
                    getVisibleViewportCount = () => visibleViewportCount,
                };
                m_Animation.Initialize(in context);
            }
        }

        void RaiseAnimationCompleted() => animationCompleted?.Invoke();

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
        /// Gets or sets the sticky row controller that manages which rows should stick to the top during scrolling.
        /// </summary>
        internal IStickyRowController stickyRowController
        {
            get => m_StickyRowController;
            set
            {
                if (ReferenceEquals(m_StickyRowController, value))
                    return;

                if (m_StickyRowController != null)
                    m_StickyRowController.onStickyStateChanged -= OnStickyRowStateChanged;

                m_StickyRowController = value;

                if (m_StickyRowController != null)
                    m_StickyRowController.onStickyStateChanged += OnStickyRowStateChanged;
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

                if (MultiColumnLayout != null)
                {
                    Insert(0, MultiColumnLayout.CreateMultiColumnHeader());
                    scrollView.applyOffset = (offset) =>
                    {
                        m_Container.style.translate = new Vector3(0, -offset.y, 0);
                        MultiColumnLayout.ScrollHorizontally(offset.x);
                    };
                }

                UnbindAllItems();
                RefreshItems();
            }
        }

        MultiColumnLayoutConfiguration MultiColumnLayout => layoutConfiguration as MultiColumnLayoutConfiguration;

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
                        SetSelection(m_Selection.selectedIndex);
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
            get => m_ScrollView.contentContainer.ClassListContains(BaseVerticalCollectionView.borderUssClassNameUnique);
            set
            {
                var previous = showBorder;
                m_ScrollView.contentContainer.EnableInClassList(BaseVerticalCollectionView.borderUssClassNameUnique, value);

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

        RecycledItem GetOrCreateStickyRow()
        {
            if (m_StickyRow == null)
            {
                var itemElement = layoutConfiguration.makeCell?.Invoke();
                m_StickyRow = RecycledItem.AllocateItem(itemElement, this);
            }
            return m_StickyRow;
        }

        /// <summary>
        /// The USS class name of an item element that is marked as sticky.
        /// </summary>
        internal static readonly UniqueStyleString stickyUssClassName = new(BaseVerticalCollectionView.itemUssClassName + "--sticky");

        /// <summary>
        /// The USS class name of an item element that is marked as sticky and currently stuck at the top.
        /// </summary>
        internal static readonly UniqueStyleString stuckUssClassName = new(BaseVerticalCollectionView.itemUssClassName + "--stuck");

        /// <summary>
        /// Constructs a CollectionView.
        /// </summary>
        public CollectionView(ICollectionViewSelectionContainer selection = null)
        {
            m_Selection = selection ?? new CollectionViewSelection(this);

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;

            AddToClassList(BaseVerticalCollectionView.ussClassNameUnique);

            m_ScrollView = new ScrollContainer { focusable = true };
            m_Container = new VisualElement { name = "container", pickingMode = PickingMode.Ignore, style = { flexGrow = 1 } };
            m_ContainerClip = new VisualElement { name = "container-clip", pickingMode = PickingMode.Ignore, style = { overflow = Overflow.Hidden, flexGrow = 1 } };
            m_VerticalScroller = m_ScrollView.verticalScroller;
            m_VerticalScroller.RegisterValueChangedCallback(OnVerticalScrollingChangeEvent);
            m_HorizontalScroller = m_ScrollView.horizontalScroller;
            m_HorizontalScroller.RegisterValueChangedCallback(OnHorizontalScrollerChangeEvent);

            m_ContainerClip.Add(m_Container);
            m_StickyRowContainer = new VisualElement
            {
                name = "sticky-row-container",
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, top = 0, left = 0, right = 0 }
            };
            m_ScrollView.contentContainer.Add(m_ContainerClip);
            m_ScrollView.contentContainer.Add(m_StickyRowContainer);
            stickyRowController = new StickyRowController();

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
            m_ScrollView.viewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
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
            m_ScrollView.viewport.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);

            if (m_ScrollScheduledItem?.isActive == true)
            {
                m_ScrollScheduledItem.Pause();
                m_ScrollScheduledItem = null;
            }
        }

        void SetStickyHeight(float height)
        {
            m_StickyHeight = height;
            m_StickyRowContainer.style.height = height;
            m_Container.style.marginTop = height;
        }

        void OnStickyRowStateChanged(int index, bool enabled)
        {
            ScheduleRefreshItems();
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
            MultiColumnLayout?.ScrollHorizontally((float)evt.newValue);
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

            // Update the header's maxWidth to account for the scrollbar
            if (MultiColumnLayout != null)
                UpdateMultiColumnHeaderWidth();
        }

        void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            // Only update if width changed for multicolumn layout
            if (Mathf.Approximately(evt.oldRect.width, evt.newRect.width)
                || MultiColumnLayout == null)
                return;

            UpdateMultiColumnHeaderWidth();
        }

        void UpdateMultiColumnHeaderWidth()
        {
            var header = MultiColumnLayout.header;
            var viewportWidth = m_ScrollView.viewport.layout.width;
            var headerPadding = header.resolvedStyle.paddingLeft + header.resolvedStyle.paddingRight;
            header.style.maxWidth = viewportWidth - headerPadding;

            // Update all row widths to match the new header width
            // Schedule to ensure header layout is complete
            schedule.Execute(() =>
            {
                foreach (var displayItem in m_DisplayedList)
                {
                    MultiColumnLayout.UpdateRowCellsWidth(displayItem.element);
                }

                UpdateStickyRowWidth();
            });
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
                m_VerticalScroller.highValue = maxScrollRange;
                m_VerticalScroller.value = currentScrollOffset;
                m_ScrollValue = m_VerticalScroller.value;
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
            var containerHeight = m_Container.resolvedStyle.height;
            if (float.IsNaN(containerHeight))
                containerHeight = height;

            if (!Mathf.Approximately(m_LastHeight, containerHeight))
            {
                m_LastHeight = containerHeight;
                RefreshItems();
            }
        }

        void UnbindItem(RecycledItem item)
        {
            if (item == null)
                return;

            var index = item.index;
            item.index = -1;

            // Only remove from dictionary if this item is the one mapped to this index.
            if (m_IndexToItemDictionary.TryGetValue(index, out var existing) && ReferenceEquals(existing, item))
                m_IndexToItemDictionary.Remove(index);

            layoutConfiguration?.unbindCell?.Invoke(item.element, index);
        }

        internal void OnDestroyItem(RecycledItem item)
        {
            layoutConfiguration?.destroyCell?.Invoke(item.element);
        }

        void BindItem(RecycledItem item, int index)
        {
            var previousIndex = item.index;

            if (m_IndexToItemDictionary.ContainsKey(item.index))
                UnbindItem(item);

            var useAlternateUss = showAlternatingRowBackgrounds != AlternatingRowBackground.None && index % 2 == 1;
            item.element.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassNameUnique, useAlternateUss);
            item.isLastItem = index == itemsSource.Count - 1;
            item.SetSelected(m_Selection.ContainsIndex(index));
            item.ClearHoverState();
            item.element.style.height = fixedItemHeight;
            item.index = index;
            item.SetSticky(stickyRowController != null && stickyRowController.IsSticky(index));

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

            m_RefreshScheduled?.Pause();
        }

        /// <summary>
        /// Schedules a call to <see cref="RefreshItems"/>.
        /// Good for when you want to make multiple changes that will each require a refresh.
        /// Calling this method multiple times will only schedule one rebuild.
        /// </summary>
        public void ScheduleRefreshItems()
        {
            if (m_RebuildScheduled?.isActive == true)
            {
                // No need as we are doing a full rebuild.
                return;
            }

            if (m_RefreshScheduled == null)
                m_RefreshScheduled = schedule.Execute(RefreshItems);
            else if (!m_RefreshScheduled.isActive)
                m_RefreshScheduled.Resume();
        }

        /// <summary>
        /// Notifies the strategy that a batch is about to appear. Call BEFORE the data update.
        /// The visual reveal happens in <see cref="NotifyItemsAppeared"/>, after items are bound.
        /// </summary>
        public void NotifyItemsAppearing(ItemAnimationInfo info)
        {
            if (m_Animation == null)
                return;

            var preservedCap = m_ExtraBindCount;
            var hadPriorAnimation = m_Animation.isAnimating;
            m_Animation.SkipAnimation();
            m_ActiveAnimationBatch = info;
            var newCap = Math.Min(info.count, visibleViewportCount);
            m_ExtraBindCount = newCap;

            if (hadPriorAnimation || preservedCap < newCap)
                RefreshItems();
            m_Animation.OnItemsAppearing(info);
        }

        /// <summary>
        /// Notifies the strategy that the appearing items are bound and visible. Call AFTER refresh.
        /// </summary>
        public void NotifyItemsAppeared(ItemAnimationInfo info)
        {
            m_Animation?.OnItemsAppeared(info);
        }

        /// <summary>
        /// Notifies the strategy that a batch is about to disappear. The strategy MUST call
        /// <paramref name="onComplete"/> on completion (or immediately if not animating).
        /// <paramref name="prepareBatch"/> runs after the internal <see cref="RefreshItems"/>
        /// but before the strategy starts — use it to force-bind items that the refresh would
        /// otherwise trim to the FreeList.
        /// </summary>
        public void NotifyItemsDisappearing(ItemAnimationInfo info, Action onComplete, Action prepareBatch = null)
        {
            if (m_Animation == null)
            {
                onComplete();
                return;
            }

            m_Animation.SkipAnimation();
            m_ExtraBindCount = Math.Min(info.count, visibleViewportCount);
            RefreshItems();
            prepareBatch?.Invoke();
            m_Animation.OnItemsDisappearing(info, onComplete);
        }

        /// <summary>
        /// Reverses the in-flight animation when <paramref name="info"/> matches the active batch
        /// with the opposite direction. Avoids the SkipAnimation+RefreshItems teardown path.
        /// </summary>
        public bool TryReverseAnimation(ItemAnimationInfo info, Action onComplete)
        {
            return m_Animation?.TryReverseAnimation(info, onComplete) ?? false;
        }

        /// <summary>
        /// Clears the extra-bind window opened by NotifyItemsAppearing/Disappearing. Strategies
        /// MUST call this on completion so the virtualizer trims back to the viewport.
        /// Callers using <see cref="PrepareBindWindowForAnimation"/> should call this in
        /// early-return paths so the widened window doesn't leak into subsequent operations.
        /// </summary>
        public void ClearAnimationBindWindow()
        {
            m_ExtraBindCount = 0;
            m_ActiveAnimationBatch = null;
        }

        /// <summary>
        /// Widens the bind window so the next <see cref="RefreshItems"/> covers the animation range
        /// in one pass and invalidates stale FreeList bindings up front. Pair with
        /// <see cref="ClearAnimationBindWindow"/> on early-return paths.
        /// </summary>
        public void PrepareBindWindowForAnimation()
        {
            foreach (var item in m_FreeList)
            {
                if (item.index >= 0)
                    UnbindItem(item);
            }
            m_ExtraBindCount = visibleViewportCount;
        }

        /// <summary>
        /// Force-binds a contiguous range so the items exist in m_DisplayedList /
        /// m_IndexToItemDictionary before an animation starts.
        /// </summary>
        public void EnsureItemsBound(int firstIndex, int count)
        {
            if (itemsSource == null || count <= 0)
                return;

            for (var i = 0; i < count; i++)
            {
                var index = firstIndex + i;
                if (index < 0 || index >= itemsSource.Count)
                    continue;

                // Skip only when in m_DisplayedList — dict entries in m_FreeList (display:None)
                // need to be re-promoted, not skipped.
                if (m_IndexToItemDictionary.TryGetValue(index, out var existing)
                    && ReferenceEquals(existing.node.List, m_DisplayedList))
                    continue;

                AddElementFromIndex(index);

                // Position arithmetically; write through verticalOffset to keep the cache in sync.
                if (m_IndexToItemDictionary.TryGetValue(index, out var bound))
                    bound.verticalOffset = index * fixedItemHeight - (float)m_ScrollValue;
            }
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
            // Defer mid-animation refreshes — items are reparented in the clip container.
            if (m_Animation is { isAnimating: true })
            {
                ScheduleRefreshItems();
                return;
            }

            BeforeRefreshingItems?.Invoke();
            m_RefreshScheduled?.Pause();

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

            m_LastHeight = height + m_StickyHeight;

            var numberOfVisibleItems = (int)(m_Container.layoutSize.y / fixedItemHeight);
            if (itemsSource.Count - 1 < numberOfVisibleItems)
                m_ScrollValue = 0;

            var rangeEstimate = (double)fixedItemHeight * itemsSource.Count;
            var maxScrollRange = rangeEstimate > m_LastHeight ? Math.Abs(rangeEstimate - m_LastHeight) : 0;
            m_VerticalScroller.style.display = rangeEstimate > height - m_HorizontalScroller.worldBound.height ? DisplayStyle.Flex : DisplayStyle.None;
            SetScrollingParameters(m_ScrollValue, maxScrollRange);
            BindVisibleItems(true);
            UpdateBackgroundFill();
            ApplyHoverStateFromPointerPosition();
        }

        void ApplyHoverStateFromPointerPosition()
        {
            if (panel is not BaseVisualElementPanel basePanel)
                return;

            var elementUnderPointer = basePanel.GetTopElementUnderPointer(PointerId.mousePointerId);
            if (elementUnderPointer == null)
                return;

            var element = elementUnderPointer.GetFirstAncestorWhere(p => p.ClassListContains(MultiColumnController.rowContainerUssClassNameUnique));
            if (element == null)
                return;

            element.pseudoStates |= PseudoStates.Hover;
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

            if (m_StickyRow != null)
            {
                UnbindItem(m_StickyRow);
                RecycledItem.Recycle(m_StickyRow);
                m_StickyRow = null;
            }

            RecycledItem.ClearItemPool();
            m_BackgroundFill?.RemoveFromHierarchy();
        }

        // Fills empty space below the last item with alternating bands for AlternatingRowBackground.All.
        void UpdateBackgroundFill()
        {
            var itemHeight = fixedItemHeight;
            var count = itemsSource?.Count ?? 0;
            var availableHeight = m_ContainerClip.resolvedStyle.height;
            var contentBottom = m_StickyHeight + count * itemHeight;
            var fillHeight = availableHeight - contentBottom;

            if (showAlternatingRowBackgrounds != AlternatingRowBackground.All || count == 0 || float.IsNaN(itemHeight) || itemHeight <= 0f || float.IsNaN(availableHeight) || fillHeight <= 0f)
            {
                m_BackgroundFill?.RemoveFromHierarchy();
                return;
            }

            m_BackgroundFill ??= new VisualElement
            {
                name = "unity-collection-view__background-fill",
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, right = 0, overflow = Overflow.Hidden }
            };

            if (m_BackgroundFill.parent != m_ContainerClip)
                m_ContainerClip.Add(m_BackgroundFill);

            m_BackgroundFill.style.top = contentBottom;
            m_BackgroundFill.style.height = fillHeight;

            var rowCount = Mathf.FloorToInt(fillHeight / itemHeight) + 1;
            while (m_BackgroundFill.hierarchy.childCount < rowCount)
                m_BackgroundFill.Add(new VisualElement { pickingMode = PickingMode.Ignore, style = { flexShrink = 0 } });

            var childCount = m_BackgroundFill.hierarchy.childCount;
            for (var i = 0; i < childCount; i++)
            {
                var row = m_BackgroundFill.hierarchy[i];
                if (i >= rowCount)
                {
                    row.style.display = DisplayStyle.None;
                    continue;
                }

                row.style.display = DisplayStyle.Flex;
                row.style.height = itemHeight;
                row.EnableInClassList(BaseVerticalCollectionView.itemAlternativeBackgroundUssClassNameUnique, (count + i) % 2 == 1);
            }
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

            if (m_Container.FindElementInTree(leafTarget, m_LastFocusedElementTreeChildIndexes))
            {
                var recycledElement = m_Container[m_LastFocusedElementTreeChildIndexes[0]];
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
            if (m_DisplayedList.Count == 0 || itemsSource == null)
                return;

            m_VerticalScroller.scrollSize = k_DefaultScrollSize;

            var firstVisibleIndex = (int)(m_ScrollValue / fixedItemHeight);
            var maxOffset = m_LastHeight - m_ScrollView.containerOffset.y;
            var visibleCount = (int)Mathf.Ceil(maxOffset / fixedItemHeight);
            var lastVisibleIndex = firstVisibleIndex + visibleCount - 1;
            lastVisibleIndex = Mathf.Min(lastVisibleIndex, itemsSource.Count - 1);

            var visibleRange = lastVisibleIndex - firstVisibleIndex + 1;
            if (visibleRange <= 0)
                return;

            var ratio = (double)visibleRange / itemsSource.Count;
            var rangeEstimate = (double)fixedItemHeight * itemsSource.Count;
            m_VerticalScroller.style.display = rangeEstimate > m_Container.worldBound.height ? DisplayStyle.Flex : DisplayStyle.None;
            m_VerticalScroller.factor = (float)(Math.Abs(ratio - 1) < UIRUtility.k_Epsilon ? m_ScrollView.viewport.layout.height / m_Container.boundingBox.height : ratio);
        }

        void UpdateHorizontalScrollRange()
        {
            var maxWidth = 0f;
            var containerWidth = m_Container.boundingBox.width;
            var verticalScrollerWidth = m_VerticalScroller.worldBound.width;

            if (MultiColumnLayout != null)
            {
                maxWidth = MultiColumnLayout.header.maxScrollableWidth;
                containerWidth = MultiColumnLayout.header.scrollableWidth;
            }
            else
            {
                foreach (var displayItem in m_DisplayedList)
                {
                    maxWidth = Mathf.Max(maxWidth, displayItem.element.worldBoundingBox.width - verticalScrollerWidth);
                }
            }

            m_HorizontalScroller.style.display = maxWidth > containerWidth && containerWidth > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            m_HorizontalScroller.lowValue = 0;
            m_HorizontalScroller.highValue = Mathf.Max(0, maxWidth - containerWidth);
            m_HorizontalScroller.scrollSize = k_DefaultScrollSize * containerWidth;
            m_HorizontalScroller.factor = maxWidth > UIRUtility.k_Epsilon ? containerWidth / maxWidth : 1f;
        }

        void BindStickyRow(int index, bool forceRebind = false)
        {
            var stickyRowItem = GetOrCreateStickyRow();
            if (!forceRebind && stickyRowItem.index == index)
                return;

            if (stickyRowItem.index != -1)
                UnbindItem(stickyRowItem);

            if (stickyRowItem.element.parent == null)
                m_StickyRowContainer.Add(stickyRowItem.element);

            SetStickyHeight(fixedItemHeight);
            BindItem(stickyRowItem, index);
            stickyRowItem.SetSticky(false, true);
        }

        void UnbindStickyRow()
        {
            if (m_StickyRow == null || m_StickyRow.index == -1)
                return;

            UnbindItem(m_StickyRow);
            m_StickyRow.element.RemoveFromHierarchy();
            SetStickyHeight(0);
        }

        void UpdateStickyRowWidth()
        {
            if (!hasActiveStickyRow || MultiColumnLayout == null)
                return;

            var headerWidth = MultiColumnLayout.header.worldBoundingBox.width;
            if (!Mathf.Approximately(m_StickyRow.element.resolvedStyle.width, headerWidth))
                m_StickyRow.element.style.width = headerWidth;
        }

        void BindVisibleItems(bool forceBindItem = false)
        {
            // Defer mid-animation rebinds — see RefreshItems above.
            if (m_Animation is { isAnimating: true })
            {
                ScheduleRefreshItems();
                return;
            }

            var height = m_LastHeight;
            // m_ExtraBindCount widens the range during an animation so below-viewport items exist.
            var visibleCount = (int)Mathf.Ceil(height / fixedItemHeight) + 3 + m_ExtraBindCount;
            var animatedStickyRow = -1;

            m_FirstVisibleItemIndex = (int)(m_ScrollValue / fixedItemHeight);

            if (stickyRowController != null)
            {
                var currentStickyItem = stickyRowController.GetPreviousStickyIndex(m_FirstVisibleItemIndex);
                var nextItemIsSticky = stickyRowController.IsSticky(m_FirstVisibleItemIndex + 1);
                var stickyIsScrolledUnder = currentStickyItem != -1 && m_ScrollValue > currentStickyItem * fixedItemHeight;

                if (stickyIsScrolledUnder && !nextItemIsSticky)
                {
                    BindStickyRow(currentStickyItem, forceBindItem);
                }
                else if (stickyIsScrolledUnder)
                {
                    UnbindStickyRow();
                    animatedStickyRow = currentStickyItem;
                    m_FirstVisibleItemIndex++;
                    visibleCount--;
                }
                else
                {
                    UnbindStickyRow();
                }

                UpdateStickyRowWidth();
            }

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

            // Add the animated sticky item to the top
            if (animatedStickyRow != -1)
            {
                AddElementFromIndex(animatedStickyRow);

                // Switch to stuck class
                if (m_IndexToItemDictionary.TryGetValue(animatedStickyRow, out var row))
                {
                    row.SetSticky(false, true);
                    var offVertical = (float)(m_ScrollValue % fixedItemHeight);
                    row.verticalOffset = -offVertical;
                }
            }

            AddElementsFromIndex(m_FirstVisibleItemIndex, visibleCount, forceBindItem);

            foreach (var item in m_FreeList)
            {
                item.element.style.display = DisplayStyle.None;
                // Unbind potentially-stale entries so the next promotion rebinds against current data.
                if (forceBindItem && m_ExtraBindCount == 0 && item.index >= 0)
                    UnbindItem(item);
            }

            m_Animation?.OnRefreshCompleted();
        }

        void AddElementsFromIndex(int firstIndex, int itemCount, bool forceBindItem = false)
        {
            var lastIndex = firstIndex + itemCount;

            // We now fill the Display List
            for (var index = firstIndex; index < lastIndex; ++index)
            {
                if (index < 0 || index > itemsSource.Count - 1)
                    continue;

                AddElementFromIndex(index, forceBindItem);
            }

            UpdateContainerOffset();

            if (m_DisplayedList.Count > 0)
            {
                RecycledItem.UpdatePositions(m_DisplayedList.First.Value);
            }
        }

        void AddElementFromIndex(int index, bool forceBindItem = false)
        {
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
                m_IndexToItemDictionary.Add(index, itemWrapper);
            }
            else
            {
                // Item is reused for the same index - update width to match current column widths
                MultiColumnLayout?.UpdateRowCellsWidth(itemWrapper.element);
            }

            // Since we are hiding the reusable items instead of removing it, we need to make them visible again.
            itemWrapper.element.style.display = DisplayStyle.Flex;

            m_DisplayedList.AddLast(itemWrapper.node);

            if (m_Animation != null)
            {
                ItemAnimationContext? context = null;
                if (m_ActiveAnimationBatch is { } batch
                    && index >= batch.firstIndex
                    && index < batch.firstIndex + batch.count)
                {
                    context = new ItemAnimationContext
                    {
                        batchInfo = batch,
                        indexInBatch = index - batch.firstIndex,
                    };
                }
                m_Animation.OnItemBound(itemWrapper, index, context);
            }
        }

        void UpdateContainerOffset()
        {
            var offset = m_ScrollView.containerOffset;
            offset.y = m_StickyHeight;
            offset.x = m_HorizontalScroller.highValue > 0 ? offset.x : 0;
            m_ScrollView.containerOffset = offset;
        }

        internal void UpdateScrollingRangeAfterLayout()
        {
            UpdateHorizontalScrollRange();

            var item = m_DisplayedList.Last;
            if (item == null)
                return;

            var itemValue = item.Value;
            var offset = -m_Container.resolvedStyle.translate.y - m_StickyHeight;
            var bottom = itemValue.verticalOffset + fixedItemHeight;
            var height = m_LastHeight;

            if (itemValue.isLastItem)
            {
                var visibleHeight = m_ScrollView.viewport.layout.height;
                var totalRange = Math.Max(0, itemsSource.Count * fixedItemHeight - visibleHeight);
                SetScrollingParameters(Math.Min(m_ScrollValue, totalRange), totalRange);
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

        /// <summary>
        /// Returns the item's index from based on the provided position.
        /// </summary>
        /// <param name="position">The position of the item.</param>
        /// <returns>The index of the item.</returns>
        public int GetIndexFromPosition(Vector2 position)
        {
            var itemHeight = AlignmentUtils.RoundToPixelGrid(fixedItemHeight, scaledPixelsPerPoint);

            if (hasActiveStickyRow && position.y < itemHeight)
                return m_StickyRow.index;

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
                if (hasActiveStickyRow && stickyRowController.GetPreviousStickyIndex(index - 1) != -1)
                {
                    // When scrolling the item to the top, it can appear behind the stuck row.
                    // So scroll to the 1 before so the index item is visible.
                    index = Mathf.Max(0, index - 1);
                }

                UpdateVerticalScrollValue(fixedItemHeight * index);
                return;
            }

            // Initial frame-to-selection can fire before RefreshItems configures the scroller's high value.
            if (m_LastHeight <= 0)
            {
                schedule.Execute(() => ScrollToItem(index));
                return;
            }

            var containerHeight = m_Container.layoutSize.y + m_StickyHeight;
            var numberOfVisibleItems = (int)(containerHeight / fixedItemHeight);
            if (index < m_FirstVisibleItemIndex + numberOfVisibleItems)
                return;

            var visibleOffset = fixedItemHeight - (containerHeight - numberOfVisibleItems * fixedItemHeight);
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
            if (m_StickyRow != null && m_StickyRow.index == index)
                return m_StickyRow.element;

            foreach (var item in m_DisplayedList)
            {
                if (item.index == index)
                    return item.element;
            }
            return null;
        }

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal RecycledItem GetRecycledItemForIndex(int index)
        {
            if (m_StickyRow != null && m_StickyRow.index == index)
                return m_StickyRow;

            return m_IndexToItemDictionary.TryGetValue(index, out var item) ? item : null;
        }

        public bool hasSelection => m_Selection.indexCount > 0;

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

                SetSelection(newSelection.AsSpan(0, count));
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

            AddToSelectionWithoutValidation(indices);

            NotifyOfSelectionChange();
            SaveViewData();
        }

        void AddToSelectionWithoutValidation(ReadOnlySpan<int> indices)
        {
            foreach (var index in indices)
            {
                if (m_IndexToItemDictionary.TryGetValue(index, out var recycleItem))
                    recycleItem.SetSelected(true);
            }

            m_Selection.AddRange(indices);
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
            if (!m_Selection.ContainsIndex(index))
                return;

            if (m_IndexToItemDictionary.TryGetValue(index, out var recycleItem))
                recycleItem.SetSelected(false);

            if (m_StickyRow != null && m_StickyRow.index == index)
                m_StickyRow.SetSelected(false);

            m_Selection.Remove(index);
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
                Span<int> selectedIndices = stackalloc int[count];
                for (var i = 0; i < indices.Count; i++)
                {
                    selectedIndices[i] = indices[i];
                }

                SetSelectionInternal(selectedIndices, sendNotification);
            }
            else
            {
                using var selectedIndices = new RentSpanUnmanaged<int>(count);
                for (var i = 0; i < indices.Count; i++)
                {
                    selectedIndices.Span[i] = indices[i];
                }

                SetSelectionInternal(selectedIndices, sendNotification);
            }

            SaveViewData();
        }

        void SetSelectionInternal(ReadOnlySpan<int> indices, bool sendNotification)
        {
            if (m_Selection.MatchesExistingSelection(indices))
                return;

            m_Selection.Select(indices);

            // Update the visual state of visible items
            foreach (var (_, recycledItem) in m_IndexToItemDictionary)
                recycledItem.SetSelected(false);

            m_StickyRow?.SetSelected(false);

            foreach (var index in indices)
            {
                if (m_IndexToItemDictionary.TryGetValue(index, out var recycleItem))
                    recycleItem.SetSelected(true);
                if (m_StickyRow != null && m_StickyRow.index == index)
                    m_StickyRow.SetSelected(true);
            }

            if (sendNotification)
                NotifyOfSelectionChange();

            SaveViewData();
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            if (m_Selection?.indexCount == 0)
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        void ClearSelectionWithoutValidation()
        {
            foreach (var (_, recycledItem) in m_IndexToItemDictionary)
                recycledItem.SetSelected(false);

            m_StickyRow?.SetSelected(false);
            m_Selection.Clear();
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

        internal StartDragArgs RaiseSetupDragAndDrop(RecycledItem item, IReadOnlyList<int> indices, StartDragArgs args)
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

            m_Selection.SelectAll();

            foreach (var recycledItem in m_IndexToItemDictionary.Values)
            {
                recycledItem.SetSelected(true);
            }

            if (m_StickyRow != null && m_StickyRow.index >= 0)
                m_StickyRow.SetSelected(true);

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
                    m_Selection.selectedIndex = index;
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
                    ScrollToItem(m_Selection.selectedIndex);
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
                {
                    var lastIndex = itemsSource.Count - 1;
                    if (lastIndex < 0)
                        return true;

                    // Sync selection (listeners read it immediately); defer scroll so the sticky row can settle.
                    if (selectionType == SelectionType.Multiple && shiftKey && m_Selection.indexCount != 0)
                        DoRangeSelection(lastIndex);
                    else
                    {
                        m_RangeSelectionDirection = RangeSelectionDirection.None;
                        m_Selection.selectedIndex = lastIndex;
                    }

                    if (m_StickyRowController != null)
                        schedule.Execute(() => ScrollToItem(lastIndex));
                    else
                        ScrollToItem(lastIndex);

                    return true;
                }
                case KeyboardNavigationOperation.PageDown:
                {
                    if (m_DisplayedList.Count == 0)
                        return true;

                    if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                        m_RangeSelectionDirection = RangeSelectionDirection.Down;

                    var selectionDown = m_RangeSelectionDirection == RangeSelectionDirection.Up ? m_Selection.minIndex : m_Selection.maxIndex;
                    var itemHeight = fixedItemHeight;
                    var containerHeight = m_Container.layoutSize.y + m_StickyHeight;
                    var containerBottom = m_VerticalScroller.value + containerHeight;
                    var maxIndex = itemsSource.Count - 1;
                    var lastVisibleIndex = (int)(containerBottom / itemHeight);
                    var pageSize = (int)(containerHeight / itemHeight);

                    lastVisibleIndex = lastVisibleIndex > maxIndex ? maxIndex : lastVisibleIndex;

                    // Determine target: if no selection or after first visible, select first visible; else jump page
                    var targetIndex = (m_Selection.indexCount == 0 || selectionDown < lastVisibleIndex - 1) ? lastVisibleIndex : (selectionDown + pageSize > maxIndex ? maxIndex : selectionDown + pageSize);

                    HandleSelectionAndScroll(targetIndex);

                    if (m_StickyRowController != null)
                        schedule.Execute(() => ScrollToItem(targetIndex));

                    return true;
                }
                case KeyboardNavigationOperation.PageUp:
                {
                    if (m_DisplayedList.Count == 0)
                        return true;

                    if (m_RangeSelectionDirection == RangeSelectionDirection.None)
                        m_RangeSelectionDirection = RangeSelectionDirection.Up;

                    var selectionUp = m_RangeSelectionDirection == RangeSelectionDirection.Up ? m_Selection.minIndex : m_Selection.maxIndex;
                    var scrollOffset = m_VerticalScroller.value;
                    var itemHeight = fixedItemHeight;
                    var containerHeight = m_Container.layoutSize.y + m_StickyHeight;
                    var firstVisibleIndex = (int)(scrollOffset / itemHeight);
                    var pageSize = (int)(containerHeight / itemHeight);

                    firstVisibleIndex = firstVisibleIndex < 0 ? 0 : firstVisibleIndex;

                    // Determine target: if no selection or after first visible, select first visible; else jump page
                    var targetIndex = (m_Selection.indexCount == 0 || selectionUp > firstVisibleIndex + 1) ? firstVisibleIndex : selectionUp - pageSize;

                    HandleSelectionAndScroll(targetIndex < 0 ? 0 : targetIndex);
                    return true;
                }
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
