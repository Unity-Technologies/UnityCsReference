// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Pool;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to display alternating background colors for collection view rows.
    /// </summary>
    /// <remarks>
    /// If there are more rows with content than there are visible rows, you will not see a difference between
    /// the <c>All</c> option and the <c>ContentOnly</c> option.
    /// </remarks>
    public enum AlternatingRowBackground
    {
        /// <summary>
        /// Do not alternate background colors for rows.
        /// </summary>
        None,
        /// <summary>
        /// Alternate background colors only for rows that have content. The background color does not alternate for empty lines.
        /// </summary>
        ContentOnly,
        /// <summary>
        /// Alternate background colors for all rows, regardless of whether they have content. The background color continues to alternate for empty lines.
        /// </summary>
        All
    }

    /// <summary>
    /// Options to change the virtualization method used by the collection view to display its content.
    /// </summary>
    public enum CollectionVirtualizationMethod
    {
        /// <summary>
        /// Collection view won't wait for the layout to update items, as the all have the same height. <c>fixedItemHeight</c> Needs to be set. More performant but less flexible.
        /// </summary>
        FixedHeight,
        /// <summary>
        /// Collection view will use the actual height of every item when geometry changes. More flexible but less performant.
        /// </summary>
        DynamicHeight,
    }

    /// <summary>
    /// Option to change the data source assignation when using Data Binding in collection views.
    /// </summary>
    public enum BindingSourceSelectionMode
    {
        /// <summary>
        /// Data source assignation will be handled by user code when binding each item.
        /// </summary>
        Manual,
        /// <summary>
        /// The items source and indexed path are automatically assigned to each item's data source.
        /// </summary>
        AutoAssign,
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class SerializedVirtualizationData
    {
        public Vector2 scrollOffset;
        public int firstVisibleIndex;
        public float contentPadding;
        public float contentHeight;
        public int anchoredItemIndex;
        public float anchorOffset;
    }

    /// <summary>
    /// Base class for controls that display virtualized vertical content inside a scroll view.
    /// </summary>
    /// <remarks>
    /// In BaseCollectionListView, the <c>id</c> represents a unique and stable identifier for each item.
    /// It is essential for operations like saving and restoring the state, such as expansions and selections.
    /// In contrast, the <c>index</c> indicates an item's position within the current view order,
    /// which can change based on user actions like sorting and filtering.
    /// You can use <c>id</c> to maintain distinct references, and use the <c>index</c> to handle rendering
    /// and layout tasks based on the visible order of items.
    /// </remarks>
    public abstract class BaseVerticalCollectionView : BindableElement, ISerializationCallbackReceiver
    {
        internal static readonly BindingId itemsSourceProperty = nameof(itemsSource);
        internal static readonly BindingId selectionTypeProperty = nameof(selectionType);
        internal static readonly BindingId selectedItemProperty = nameof(selectedItem);
        internal static readonly BindingId selectedItemsProperty = nameof(selectedItems);
        internal static readonly BindingId selectedIndexProperty = nameof(selectedIndex);
        internal static readonly BindingId selectedIndicesProperty = nameof(selectedIndices);
        internal static readonly BindingId showBorderProperty = nameof(showBorder);
        internal static readonly BindingId reorderableProperty = nameof(reorderable);
        internal static readonly BindingId horizontalScrollingEnabledProperty = nameof(horizontalScrollingEnabled);
        internal static readonly BindingId showAlternatingRowBackgroundsProperty = nameof(showAlternatingRowBackgrounds);
        internal static readonly BindingId virtualizationMethodProperty = nameof(virtualizationMethod);
        internal static readonly BindingId fixedItemHeightProperty = nameof(fixedItemHeight);

        [ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BindableElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(fixedItemHeight), "fixed-item-height", null, "itemHeight", "item-height"),
                    new (nameof(virtualizationMethod), "virtualization-method"),
                    new (nameof(showBorder), "show-border"),
                    new (nameof(selectionType), "selection-type"),
                    new (nameof(showAlternatingRowBackgrounds), "show-alternating-row-backgrounds"),
                    new (nameof(reorderable), "reorderable"),
                    new (nameof(horizontalScrollingEnabled), "horizontal-scrolling"),
                });
            }

            #pragma warning disable 649
            [UxmlAttribute(obsoleteNames = new[] { "itemHeight", "item-height" })]
            [SerializeField, FixedItemHeightDecorator] float fixedItemHeight;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags fixedItemHeight_UxmlAttributeFlags;
            [SerializeField] CollectionVirtualizationMethod virtualizationMethod;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags virtualizationMethod_UxmlAttributeFlags;
            [SerializeField] bool showBorder;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showBorder_UxmlAttributeFlags;
            [SerializeField] SelectionType selectionType;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags selectionType_UxmlAttributeFlags;
            [SerializeField] AlternatingRowBackground showAlternatingRowBackgrounds;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showAlternatingRowBackgrounds_UxmlAttributeFlags;
            [SerializeField] bool reorderable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags reorderable_UxmlAttributeFlags;
            [UxmlAttribute("horizontal-scrolling")]
            [SerializeField] bool horizontalScrollingEnabled;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags horizontalScrollingEnabled_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (BaseVerticalCollectionView)obj;

                // Clear the old controller so that the list refreshes
                e.SetViewController(null);

                if (ShouldWriteAttributeValue(fixedItemHeight_UxmlAttributeFlags))
                    e.fixedItemHeight = fixedItemHeight;
                if (ShouldWriteAttributeValue(virtualizationMethod_UxmlAttributeFlags))
                    e.virtualizationMethod = virtualizationMethod;
                if (ShouldWriteAttributeValue(showBorder_UxmlAttributeFlags))
                    e.showBorder = showBorder;
                if (ShouldWriteAttributeValue(selectionType_UxmlAttributeFlags))
                    e.selectionType = selectionType;
                if (ShouldWriteAttributeValue(showAlternatingRowBackgrounds_UxmlAttributeFlags))
                    e.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;
                if (ShouldWriteAttributeValue(reorderable_UxmlAttributeFlags))
                    e.reorderable = reorderable;
                if (ShouldWriteAttributeValue(horizontalScrollingEnabled_UxmlAttributeFlags))
                    e.horizontalScrollingEnabled = horizontalScrollingEnabled;
            }
        }

        internal const string internalBindingKey = "__unity-collection-view-internal-binding";
        static readonly ProfilerMarker k_RefreshMarker = new ("BaseVerticalCollectionView.RefreshItems");
        static readonly ProfilerMarker k_RebuildMarker = new ("BaseVerticalCollectionView.Rebuild");

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseVerticalCollectionView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the BaseVerticalCollectionView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlEnumAttributeDescription<CollectionVirtualizationMethod> m_VirtualizationMethod = new UxmlEnumAttributeDescription<CollectionVirtualizationMethod> { name = "virtualization-method", defaultValue = CollectionVirtualizationMethod.FixedHeight };
            private readonly UxmlIntAttributeDescription m_FixedItemHeight = new UxmlIntAttributeDescription { name = "fixed-item-height", obsoleteNames = new[] { "itemHeight", "item-height" }, defaultValue = s_DefaultItemHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_HorizontalScrollingEnabled = new UxmlBoolAttributeDescription { name = "horizontal-scrolling", defaultValue = false };

            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
            }

            /// <summary>
            /// Initializes <see cref="BaseVerticalCollectionView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var view = (BaseVerticalCollectionView)ve;
                view.reorderable = m_Reorderable.GetValueFromBag(bag, cc);

                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (m_FixedItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    view.fixedItemHeight = itemHeight;
                }

                view.virtualizationMethod = m_VirtualizationMethod.GetValueFromBag(bag, cc);
                view.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                view.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                view.showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
                view.horizontalScrollingEnabled = m_HorizontalScrollingEnabled.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.itemsChosen"/> instead.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// </remarks>
        [Obsolete("onItemsChosen is deprecated, use itemsChosen instead", false)]
        public event Action<IEnumerable<object>> onItemsChosen
        {
            add => itemsChosen += value;
            remove => itemsChosen -= value;
        }

        /// <summary>
        /// Callback triggered when the user acts on a selection of one or more items, for example by double-clicking or pressing Enter.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// <para>__Note__: A single-click only changes the selection. Use a double-click to submit the selection.</para>
        /// </remarks>
        public event Action<IEnumerable<object>> itemsChosen;

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.selectionChanged"/> instead.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        [Obsolete("onSelectionChange is deprecated, use selectionChanged instead", false)]
        public event Action<IEnumerable<object>> onSelectionChange
        {
            add => selectionChanged += value;
            remove => selectionChanged -= value;
        }

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        public event Action<IEnumerable<object>> selectionChanged;

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.selectedIndicesChanged"/> instead.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item index or item indices selected.
        /// </remarks>
        [Obsolete("onSelectedIndicesChange is deprecated, use selectedIndicesChanged instead", false)]
        public event Action<IEnumerable<int>> onSelectedIndicesChange
        {
            add => selectedIndicesChanged += value;
            remove => selectedIndicesChanged -= value;
        }

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item index or item indices selected.
        /// </remarks>
        public event Action<IEnumerable<int>> selectedIndicesChanged;

        /// <summary>
        /// Called when an item is moved in the itemsSource.
        /// </summary>
        /// <remarks>
        /// This callback receives two IDs, the first being the ID being moved, the second being the destination ID.
        /// In the case of a tree, the destination is the parent ID.
        /// </remarks>
        public event Action<int, int> itemIndexChanged;

        /// <summary>
        /// Raised when the data source of a vertical collection view is assigned a new reference or new type.
        /// </summary>
        /// <remarks>
        /// Use this event to handle changes to the vertical collection view's data source, ensuring the UI appropriately
        /// reflects the new data. For example, if the data source changes from a list of characters to a list of items, you can
        /// use this event to update the binding events so the UI fits the new type.
        ///\\
        ///\\
        /// This event isn't raised if the selection or the size of the data source changes. For size changes, such as adding
        /// or removing an item from a list view, listen to the [[BaseListViewController.itemsSourceSizeChanged]] event.
        /// For selection changes, listen to the [[BaseVerticalCollectionView.selectionChanged]] event.
        /// </remarks>
        /// <example>
        /// The following example illustrates that the @@itemsSourceChanged@@ event is only triggered when the [[BaseVerticalCollectionView.itemsSource|itemsSource]] property is changed,
        /// not when the contents of the data source are modified.
        /// <code lang="cs">
        /// <![CDATA[
        /// var changedCount = 0;
        /// var source = new List<string>();
        /// var listView = new ListView();
        ///
        /// listView.itemsSourceChanged += () => changedCount++;
        ///
        /// // Changing the data source of the list view triggers the event.
        /// listView.itemsSource = source;
        ///
        /// // Adding an item to the source doesn't trigger itemsSourceChanged
        /// // because the data source reference remains the same.
        /// source.Add("Hello World!");
        ///
        /// // Adding an item to the ListView directly doesn't trigger itemsSourceChanged
        /// // because the data source reference remains the same.
        /// listView.viewController.AddItems(1);
        ///
        /// Debug.Log(changedCount); // Outputs 1.
        /// ]]>
        /// </code>
        /// </example>
        /// <remarks>
        /// SA: [[BaseListViewController.itemsAdded]], [[BaseListViewController.itemsRemoved]]
        /// </remarks>
        public event Action itemsSourceChanged;

        private event Action m_SelectionNotChanged = () => { };

        internal event Action selectionNotChanged
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            add => m_SelectionNotChanged += value;
            remove => m_SelectionNotChanged -= value;
        }

        /// <summary>
        /// Called when a drag operation wants to start in this collection view.
        /// </summary>
        public event Func<CanStartDragArgs, bool> canStartDrag;

        internal bool HasCanStartDrag() => canStartDrag != null;

        internal bool RaiseCanStartDrag(ReusableCollectionItem item, IEnumerable<int> ids)
        {
            return canStartDrag?.Invoke(new CanStartDragArgs(item?.rootElement, item?.id ?? BaseTreeView.invalidId, ids)) ?? true;
        }

        /// <summary>
        /// Called when a drag operation starts in this collection view.
        /// </summary>
        public event Func<SetupDragAndDropArgs, StartDragArgs> setupDragAndDrop;

        internal StartDragArgs RaiseSetupDragAndDrop(ReusableCollectionItem item, IEnumerable<int> ids, StartDragArgs args)
        {
            return setupDragAndDrop?.Invoke(new SetupDragAndDropArgs(item?.rootElement, ids, args)) ?? args;
        }

        /// <summary>
        /// Called when a drag operation updates in this collection view.
        /// </summary>
        public event Func<HandleDragAndDropArgs, DragVisualMode> dragAndDropUpdate;

        internal DragVisualMode RaiseHandleDragAndDrop(Vector2 pointerPosition, DragAndDropArgs dragAndDropArgs)
        {
            return dragAndDropUpdate?.Invoke(new HandleDragAndDropArgs(pointerPosition, dragAndDropArgs)) ?? DragVisualMode.None;
        }

        /// <summary>
        /// Called when a drag operation is released in this collection view.
        /// </summary>
        public event Func<HandleDragAndDropArgs, DragVisualMode> handleDrop;

        internal DragVisualMode RaiseDrop(Vector2 pointerPosition, DragAndDropArgs dragAndDropArgs)
        {
            return handleDrop?.Invoke(new HandleDragAndDropArgs(pointerPosition, dragAndDropArgs)) ?? DragVisualMode.None;
        }

        /// <summary>
        /// The data source for collection items.
        /// </summary>
        /// <remarks>
        /// This list contains the items that the <see cref="BaseVerticalCollectionView"/> displays.
        /// </remarks>
        [CreateProperty]
        public IList itemsSource
        {
            get => viewController?.itemsSource;
            set
            {
                var previous = itemsSource;
                GetOrCreateViewController().itemsSource = value;
                if (previous != itemsSource)
                {
                    NotifyPropertyChanged(itemsSourceProperty);
                }
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="ListView.makeItem"/> or <see cref="TreeView.makeItem"/> instead.
        /// </summary>
        [Obsolete("makeItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Func<VisualElement> makeItem
        {
            get => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("makeItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Obsolete. Use <see cref="ListView.bindItem"/> or <see cref="TreeView.bindItem"/> instead.
        /// </summary>
        [Obsolete("bindItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement, int> bindItem
        {
            get => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("bindItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Obsolete. Use <see cref="ListView.unbindItem"/> or <see cref="TreeView.unbindItem"/> instead.
        /// </summary>
        [Obsolete("unbindItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement, int> unbindItem
        {
            get => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("unbindItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Obsolete. Use <see cref="ListView.destroyItem"/> or <see cref="TreeView.destroyItem"/> instead.
        /// </summary>
        [Obsolete("destroyItem has been moved to ListView and TreeView. Use these ones instead.")]
        public Action<VisualElement> destroyItem
        {
            get => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
            set => throw new UnityException("destroyItem has been moved to ListView and TreeView. Use these ones instead.");
        }

        /// <summary>
        /// Returns the content container for the <see cref="BaseVerticalCollectionView"/>. Because the BaseVerticalCollectionView
        /// control automatically manages its content, this always returns null.
        /// </summary>
        public override VisualElement contentContainer => null;

        private SelectionType m_SelectionType;

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
            get { return m_SelectionType; }
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
                    {
                        SetSelection(m_Selection.FirstIndex());
                    }
                }

                if (previous != m_SelectionType)
                    NotifyPropertyChanged(selectionTypeProperty);
            }
        }

        /// <summary>
        /// Returns the selected item from the data source. If multiple items are selected, returns the first selected item.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public object selectedItem => m_Selection.FirstObject();

        /// <summary>
        /// Returns the selected items from the data source. Always returns an enumerable, even if no item is selected, or a single
        /// item is selected.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public IEnumerable<object> selectedItems
        {
            get
            {
                // Match the order of the selection
                foreach (var index in m_Selection.indices)
                {
                    if (m_Selection.items.TryGetValue(index, out var item))
                        yield return item;
                    else
                        yield return null;
                }
            }
        }

        /// <summary>
        /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
        /// first selected item's index. If multiple items are provided, sets them all as selected. If no item is selected, returns -1.
        /// </summary>
        [CreateProperty]
        public int selectedIndex
        {
            get { return m_Selection.indexCount == 0 ? -1 : m_Selection.FirstIndex(); }
            set
            {
                var previous = selectedIndex;
                SetSelection(value);
                if (previous != selectedIndex)
                    NotifyPropertyChanged(selectedIndexProperty);
            }
        }

        /// <summary>
        /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        /// <remarks>
        /// In a tree, if a child item is collapsed, its index is not included in the selection. To get selected items regardless of whether they are collapsed or not, use <see cref="selectedIds"/> instead.
        /// </remarks>
        [CreateProperty(ReadOnly = true)]
        public IEnumerable<int> selectedIndices => m_Selection.indices;

        /// <summary>
        /// Returns the persistent IDs of selected items in the data source, regardless of whether they are collapsed or not. Always returns an enumerable, even if no item is selected, or a
        /// single item is selected.
        /// </summary>
        /// <remarks>
        /// In a tree, if a child item is collapsed, its ID is included in the persistent selection.
        /// </remarks>
        public IEnumerable<int> selectedIds => m_Selection.selectedIds;

        static readonly List<ReusableCollectionItem> k_EmptyItems = new();
        internal IEnumerable<ReusableCollectionItem> activeItems => m_VirtualizationController?.activeItems ?? k_EmptyItems;

        internal ScrollView scrollView
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_ScrollView;
        }

        internal ListViewDragger dragger => m_Dragger;

        internal CollectionVirtualizationController virtualizationController
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => GetOrCreateVirtualizationController();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool allowSingleClickChoice = false;

        /// <summary>
        /// The view controller for this view.
        /// </summary>
        public CollectionViewController viewController => m_ViewController;

        /// <summary>
        /// Obsolete, will be removed from the API.
        /// </summary>
        /// <remarks>
        /// This value changes depending on the current panel's DPI scaling.
        /// </remarks>
        /// <seealso cref="fixedItemHeight"/>
        [Obsolete("resolvedItemHeight is deprecated and will be removed from the API.", false)]
        public float resolvedItemHeight => ResolveItemHeight();

        internal float ResolveItemHeight(float height = -1)
        {
            height = height < 0 ? fixedItemHeight : height;

            if (elementPanel == null)
            {
                return height;
            }

            return AlignmentUtils.RoundToPixelGrid(height, scaledPixelsPerPoint);
        }

        /// <summary>
        /// Enable this property to display a border around the collection view.
        /// </summary>
        /// <remarks>
        /// If set to true, a border appears around the ScrollView that the collection view uses internally.
        /// </remarks>
        [CreateProperty]
        public bool showBorder
        {
            get => m_ScrollView.ClassListContains(borderUssClassName);
            set
            {
                var previous = showBorder;
                m_ScrollView.EnableInClassList(borderUssClassName, value);

                if (previous != showBorder)
                    NotifyPropertyChanged(showBorderProperty);
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
                var previous = reorderable;

                try
                {
                    var controller = m_Dragger.dragAndDropController;
                    if (controller != null && controller.enableReordering != value)
                    {
                        controller.enableReordering = value;
                        Rebuild();
                    }
                }
                finally
                {
                    if (previous != reorderable)
                        NotifyPropertyChanged(reorderableProperty);
                }
            }
        }

        private bool m_HorizontalScrollingEnabled;

        /// <summary>
        /// This property controls whether the collection view shows a horizontal scroll bar when its content
        /// does not fit in the visible area.
        /// </summary>
        [CreateProperty]
        public bool horizontalScrollingEnabled
        {
            get => m_HorizontalScrollingEnabled;
            set
            {
                if (m_HorizontalScrollingEnabled == value)
                    return;

                m_HorizontalScrollingEnabled = value;
                m_ScrollView.horizontalScrollerVisibility = value ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
                m_ScrollView.mode = value ? ScrollViewMode.VerticalAndHorizontal : ScrollViewMode.Vertical;
                NotifyPropertyChanged(horizontalScrollingEnabledProperty);
            }
        }

        [SerializeField, DontCreateProperty]
        private AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;

        /// <summary>
        /// This property controls whether the background colors of collection view rows alternate.
        /// Takes a value from the <see cref="AlternatingRowBackground"/> enum.
        /// </summary>
        [CreateProperty]
        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get { return m_ShowAlternatingRowBackgrounds; }
            set
            {
                if (m_ShowAlternatingRowBackgrounds == value)
                    return;

                m_ShowAlternatingRowBackgrounds = value;
                RefreshItems();
                NotifyPropertyChanged(showAlternatingRowBackgroundsProperty);
            }
        }

        internal static readonly string k_InvalidTemplateError = "Template Not Found";
        // If we ever change the default item height, we should consider changing the default max height of the view when
        // used in property fields. The rule to look for is ".unity-property-field > .unity-collection-view"
        internal const int s_DefaultItemHeight = 22;
        internal float m_FixedItemHeight = s_DefaultItemHeight;
        internal bool m_ItemHeightIsInline;
        CollectionVirtualizationMethod m_VirtualizationMethod;

        /// <summary>
        /// The virtualization method to use for this collection when a scroll bar is visible.
        /// Takes a value from the <see cref="CollectionVirtualizationMethod"/> enum.
        /// </summary>
        /// <remarks>
        /// The default value is <c>FixedHeight</c>.
        /// When using fixed height, specify the <see cref="fixedItemHeight"/> property.
        /// Fixed height is more performant but offers less flexibility on content.
        /// When using <c>DynamicHeight</c>, the collection will wait for the actual height to be computed.
        /// Dynamic height is more flexible but less performant.
        /// </remarks>
        [CreateProperty]
        public CollectionVirtualizationMethod virtualizationMethod
        {
            get => m_VirtualizationMethod;
            set
            {
                if (m_VirtualizationMethod == value)
                    return;
                m_VirtualizationMethod = value;
                CreateVirtualizationController();
                Rebuild();
                NotifyPropertyChanged(virtualizationMethodProperty);
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.fixedItemHeight"/> instead.
        /// </summary>
        /// <remarks>
        /// This property must be set when using the <see cref="virtualizationMethod"/> is set to <c>FixedHeight</c>, for the collection view to function.
        /// </remarks>
        [Obsolete("itemHeight is deprecated, use fixedItemHeight instead.", false)]
        public int itemHeight
        {
            get => (int)fixedItemHeight;
            set => fixedItemHeight = value;
        }

        /// <summary>
        /// The height of a single item in the list, in pixels.
        /// </summary>
        /// <remarks>
        /// This property must be set when using the <see cref="virtualizationMethod"/> is set to <c>FixedHeight</c>, for the collection view to function.
        /// If set when <see cref="virtualizationMethod"/> is <c>DynamicHeight</c>, it serves as the default height to help calculate the
        /// number of items necessary and the scrollable area, before items are laid out. It should be set to the minimum expected height of an item.
        /// </remarks>
        [CreateProperty]
        public float fixedItemHeight
        {
            get => m_FixedItemHeight;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(fixedItemHeight), "Value needs to be positive for virtualization.");

                m_ItemHeightIsInline = true;
                if (Math.Abs(m_FixedItemHeight - value) > float.Epsilon)
                {
                    m_FixedItemHeight = value;
                    RefreshItems();
                    NotifyPropertyChanged(fixedItemHeightProperty);
                }
            }
        }

        readonly ScrollView m_ScrollView;
        CollectionViewController m_ViewController;
        CollectionVirtualizationController m_VirtualizationController;
        KeyboardNavigationManipulator m_NavigationManipulator;

        [SerializeField, DontCreateProperty]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal SerializedVirtualizationData serializedVirtualizationData = new SerializedVirtualizationData();

        // Persisted. It's why this can't be a HashSet(). :(
        // This field is used for view data persistence and must be serializable. (UUM-29291)
        [SerializeField, DontCreateProperty]
        List<int> m_SelectedIds = new List<int>();

        // Not persisted! Just used for fast lookups of selected indices and object references.
        // This is to avoid also having a mapping from index/object ref to index for the entire
        // items source.
        class Selection
        {
            readonly HashSet<int> m_IndexLookup = new();
            readonly HashSet<int> m_IdLookup = new();

            // We cache the min/max index
            int m_MinIndex = -1;
            int m_MaxIndex = -1;

            // Reference to m_SelectedIds
            public List<int> selectedIds { get; set; }
            public readonly List<int> indices = new();
            public readonly Dictionary<int, object> items = new();
            public int indexCount => indices.Count;
            public int idCount => selectedIds.Count;

            public int minIndex
            {
                get
                {
                    if (m_MinIndex == -1)
                        m_MinIndex = indices.Min();
                    return m_MinIndex;
                }
            }

            public int maxIndex
            {
                get
                {
                    if (m_MaxIndex == -1)
                        m_MaxIndex = indices.Max();
                    return m_MaxIndex;
                }
            }

            public int capacity
            {
                get => indices.Capacity;
                set
                {
                    indices.Capacity = value;

                    if (selectedIds.Capacity < value)
                        selectedIds.Capacity = value;
                }
            }

            public int FirstIndex() => indices.Count > 0 ? indices[0] : -1;
            public object FirstObject() => items.TryGetValue(FirstIndex(), out var obj) ? obj : null;

            public bool ContainsIndex(int index) => m_IndexLookup.Contains(index);
            public bool ContainsId(int id) => m_IdLookup.Contains(id);

            public void AddId(int id)
            {
                selectedIds.Add(id);
                m_IdLookup.Add(id);
            }

            public void AddIndex(int index, object obj)
            {
                m_IndexLookup.Add(index);
                indices.Add(index);
                items[index] = obj;

                if (index < m_MinIndex)
                    m_MinIndex = index;
                if (index > m_MaxIndex)
                    m_MaxIndex = index;
            }

            public bool TryRemove(int index)
            {
                if (!m_IndexLookup.Remove(index))
                    return false;

                var i = indices.IndexOf(index);
                if (i >= 0)
                {
                    indices.RemoveAt(i);
                    items.Remove(index);

                    if (index == m_MinIndex)
                        m_MinIndex = -1;
                    if (index == m_MaxIndex)
                        m_MaxIndex = -1;
                }
                return true;
            }

            public void RemoveId(int id)
            {
                selectedIds.Remove(id);
                m_IdLookup.Remove(id);
            }

            public void ClearItems()
            {
                items.Clear();
            }

            public void ClearIds()
            {
                m_IdLookup.Clear();
                selectedIds.Clear();
            }

            public void ClearIndices()
            {
                m_IndexLookup.Clear();
                indices.Clear();
                m_MinIndex = -1;
                m_MaxIndex = -1;
            }

            public void Clear()
            {
                ClearItems();
                ClearIds();
                ClearIndices();
            }
        }

        readonly Selection m_Selection;
        private float m_LastHeight;
        internal float lastHeight => m_LastHeight;

        private bool m_IsRangeSelectionDirectionUp;
        private ListViewDragger m_Dragger;

        internal const float ItemHeightUnset = -1;
        internal static CustomStyleProperty<int> s_ItemHeightProperty = new CustomStyleProperty<int>("--unity-item-height");

        // View controller callbacks
        Action<int, int> m_ItemIndexChangedCallback;
        Action m_ItemsSourceChangedCallback;

        internal IVisualElementScheduledItem m_RebuildScheduled;

        private protected virtual void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableCollectionItem>();
        }

        internal CollectionVirtualizationController GetOrCreateVirtualizationController()
        {
            if (m_VirtualizationController == null)
                CreateVirtualizationController();

            return m_VirtualizationController;
        }

        internal void CreateVirtualizationController<T>() where T : ReusableCollectionItem, new()
        {
            switch (virtualizationMethod)
            {
                case CollectionVirtualizationMethod.FixedHeight:
                    m_VirtualizationController = new FixedHeightVirtualizationController<T>(this);
                    break;
                case CollectionVirtualizationMethod.DynamicHeight:
                    m_VirtualizationController = new DynamicHeightVirtualizationController<T>(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(virtualizationMethod), virtualizationMethod, $"Unsupported {nameof(virtualizationMethod)} virtualization");
            }
        }

        internal CollectionViewController GetOrCreateViewController()
        {
            if (m_ViewController == null)
            {
                SetViewController(CreateViewController());
            }

            return m_ViewController;
        }

        /// <summary>
        /// Creates the view controller for this view.
        /// Override this method in inheritors to change the controller type.
        /// </summary>
        /// <returns>The view controller.</returns>
        protected abstract CollectionViewController CreateViewController();

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        public virtual void SetViewController(CollectionViewController controller)
        {
            if (m_ViewController != null)
            {
                m_ViewController.itemIndexChanged -= m_ItemIndexChangedCallback;
                m_ViewController.itemsSourceChanged -= m_ItemsSourceChangedCallback;
                m_ViewController.Dispose();
                m_ViewController = null;
            }

            m_ViewController = controller;

            if (m_ViewController != null)
            {
                m_ViewController.SetView(this);
                m_ViewController.itemIndexChanged += m_ItemIndexChangedCallback;
                m_ViewController.itemsSourceChanged += m_ItemsSourceChangedCallback;
            }
        }

        internal virtual ListViewDragger CreateDragger()
        {
            return new ListViewDragger(this);
        }

        internal void InitializeDragAndDropController(bool enableReordering)
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

        internal abstract ICollectionDragAndDropController CreateDragAndDropController();

        internal void SetDragAndDropController(ICollectionDragAndDropController dragAndDropController)
        {
            m_Dragger ??= CreateDragger();
            m_Dragger.dragAndDropController = dragAndDropController;
        }

        /// <summary>
        /// The USS class name for BaseVerticalCollectionView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the BaseVerticalCollectionView element. Any styling applied to
        /// this class affects every BaseVerticalCollectionView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-collection-view";
        /// <summary>
        /// The USS class name for BaseVerticalCollectionView elements with a border.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the BaseVerticalCollectionView element if the instance's
        /// <see cref="BaseVerticalCollectionView.showBorder"/> property is set to true. Any styling applied to this class
        /// affects every such BaseVerticalCollectionView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string borderUssClassName = ussClassName + "--with-border";
        /// <summary>
        /// The USS class name of item elements in BaseVerticalCollectionView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element the BaseVerticalCollectionView contains. Any styling applied to
        /// this class affects every item element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name of the drag hover bar.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the bar that appears when the user drags an item in the list.
        /// Any styling applied to this class affects every BaseVerticalCollectionView located beside, or below the stylesheet in the
        /// visual tree.
        /// </remarks>
        public static readonly string dragHoverBarUssClassName = ussClassName + "__drag-hover-bar";
        /// <summary>
        /// The USS class name of the drag hover circular marker used to indicate depth.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the bar that appears when the user drags an item in the list. Any styling applied to this class affects
        /// every BaseVerticalCollectionView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string dragHoverMarkerUssClassName = ussClassName + "__drag-hover-marker";
        /// <summary>
        /// The USS class name applied to an item element on drag hover.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the item element when it's being dragged. Any styling applied to this class affects
        /// every BaseVerticalCollectionView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemDragHoverUssClassName = itemUssClassName + "--drag-hover";
        /// <summary>
        /// The USS class name of selected item elements in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every selected element in the BaseVerticalCollectionView. The <see cref="BaseVerticalCollectionView.selectionType"/>
        /// property decides if zero, one, or more elements can be selected. Any styling applied to
        /// this class affects every BaseVerticalCollectionView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";
        /// <summary>
        /// The USS class name for odd rows in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every odd-numbered item in the BaseVerticalCollectionView when the
        /// <see cref="BaseVerticalCollectionView.showAlternatingRowBackgrounds"/> property is set to <c>ContentOnly</c> or <c>All</c>.
        /// When the <c>showAlternatingRowBackgrounds</c> property is set to either of those values, odd-numbered items
        /// are displayed with a different background color than even-numbered items. This USS class is used to differentiate
        /// odd-numbered items from even-numbered items. When the <c>showAlternatingRowBackgrounds</c> property is set to
        /// <c>None</c>, the USS class is not added, and any styling or behavior that relies on it's invalidated.
        /// </remarks>
        public static readonly string itemAlternativeBackgroundUssClassName = itemUssClassName + "--alternative-background";
        /// <summary>
        /// The USS class name of the scroll view in the BaseVerticalCollectionView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the BaseVerticalCollectionView's scroll view. Any styling applied to
        /// this class affects every BaseVerticalCollectionView scroll view located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string listScrollViewUssClassName = ussClassName + "__scroll-view";

        internal static readonly string backgroundFillUssClassName = ussClassName + "__background-fill";

        /// <summary>
        /// Creates a <see cref="BaseVerticalCollectionView"/> with all default properties.
        /// The <see cref="BaseVerticalCollectionView.itemsSource"/> must all be set for the BaseVerticalCollectionView to function properly.
        /// </summary>
        public BaseVerticalCollectionView()
        {
            AddToClassList(ussClassName);

            m_Selection = new Selection { selectedIds = m_SelectedIds };

            selectionType = SelectionType.Single;

            m_ScrollView = new ScrollView();
            m_ScrollView.AddToClassList(listScrollViewUssClassName);
            m_ScrollView.verticalScroller.valueChanged += v => OnScroll(new Vector2(0, v));

            m_ScrollView.RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ScrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ScrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(m_ScrollView);

            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            // Setting the view data key on the ScrollView to get view data persistence on the contents. (UUM-62717)
            // Disabling view data persistence on the vertical and horizontal scrollers to make sure we keep
            // the previous behavior on the scrollOffset.
            m_ScrollView.viewDataKey = "unity-vertical-collection-scroll-view";
            m_ScrollView.verticalScroller.viewDataKey = null;
            m_ScrollView.horizontalScroller.viewDataKey = null;

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;

            m_ItemIndexChangedCallback = OnItemIndexChanged;
            m_ItemsSourceChangedCallback = OnItemsSourceChanged;

            InitializeDragAndDropController(false);
        }

        /// <summary>
        /// Constructs a <see cref="BaseVerticalCollectionView"/>, with all required properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels. For <c>FixedHeight</c> virtualization only.</param>
        public BaseVerticalCollectionView(IList itemsSource, float itemHeight = ItemHeightUnset)
            : this()
        {
            if (Math.Abs(itemHeight - ItemHeightUnset) > float.Epsilon)
            {
                m_FixedItemHeight = itemHeight;
                m_ItemHeightIsInline = true;
            }

            if (itemsSource != null)
            {
                this.itemsSource = itemsSource;
            }
        }

        /// <summary>
        /// Obsolete.  Use <see cref="ListView"/> or <see cref="TreeView"/> constructor directly.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels. For <c>FixedHeight</c> virtualization only.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        [Obsolete("makeItem and bindItem are now in ListView and TreeView directly, please use a constructor without these parameters.")]
        public BaseVerticalCollectionView(IList itemsSource, float itemHeight = ItemHeightUnset, Func<VisualElement> makeItem = null, Action<VisualElement, int> bindItem = null)
            : this()
        {
            if (Math.Abs(itemHeight - ItemHeightUnset) > float.Epsilon)
            {
                m_FixedItemHeight = itemHeight;
                m_ItemHeightIsInline = true;
            }

            this.itemsSource = itemsSource;
        }

        /// <summary>
        /// Gets the root element of the specified collection view item.
        /// </summary>
        /// <param name="id">The item identifier.</param>
        /// <returns>The item's root element.</returns>
        /// <remarks>
        /// This method provides an entry point to re-style elements added by Unity over the user-driven content.
        /// Ex. the drag handle in a ListView, or the Toggle in a TreeView.
        /// </remarks>
        public VisualElement GetRootElementForId(int id)
        {
            return activeItems.FirstOrDefault(t => t.id == id)?.rootElement;
        }

        /// <summary>
        /// Gets the root element of the specified collection view item.
        /// </summary>
        /// <param name="index">The item index.</param>
        /// <returns>The item's root element.</returns>
        /// <remarks>
        /// This method provides an entry point to re-style elements added by Unity over the user-driven content.
        /// Ex. the drag handle in a ListView, or the Toggle in a TreeView.
        /// </remarks>
        public VisualElement GetRootElementForIndex(int index)
        {
            return GetRootElementForId(viewController.GetIdForIndex(index));
        }

        internal virtual bool HasValidDataAndBindings()
        {
            return m_ViewController != null && itemsSource != null;
        }

        void OnItemIndexChanged(int srcIndex, int dstIndex)
        {
            itemIndexChanged?.Invoke(srcIndex, dstIndex);
            RefreshItems();
        }

        void OnItemsSourceChanged()
        {
            itemsSourceChanged?.Invoke();
            NotifyPropertyChanged(nameof(itemsSource));
        }

        /// <summary>
        /// Rebinds a single item if it is currently visible in the collection view.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RefreshItem(int index)
        {
            foreach (var recycledItem in activeItems)
            {
                var recycledItemIndex = recycledItem.index;
                if (recycledItemIndex == index)
                {
                    viewController.InvokeUnbindItem(recycledItem, recycledItemIndex);
                    viewController.InvokeBindItem(recycledItem, recycledItemIndex);
                    break;
                }
            }
        }

        internal int m_PreviousRefreshedCount;

        /// <summary>
        /// Rebinds all items currently visible.
        /// </summary>
        /// <remarks>
        /// Call this method whenever the data source changes.
        /// </remarks>
        public void RefreshItems()
        {
            using (k_RefreshMarker.Auto())
            {
                if (m_ViewController == null)
                    return;

                // If a Rebuild is scheduled then let it handle the refresh.
                if (m_RebuildScheduled?.isActive == true)
                {
                    Rebuild();
                    return;
                }

                m_ViewController.PreRefresh();
                RefreshSelection();
                virtualizationController.Refresh(false);
                PostRefresh();
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.Rebuild"/> instead.
        /// </summary>
        [Obsolete("Refresh() has been deprecated. Use Rebuild() instead. (UnityUpgradable) -> Rebuild()", false)]
        public void Refresh()
        {
            Rebuild();
        }

        /// <summary>
        /// Clears the collection view, recreates all visible visual elements, and rebinds all items.
        /// </summary>
        /// <remarks>
        /// Call this method whenever a structural change is made to the view.
        /// </remarks>
        public void Rebuild()
        {
            using (k_RebuildMarker.Auto())
            {
                if (m_ViewController == null)
                    return;

                m_ViewController.PreRefresh();
                RefreshSelection();
                virtualizationController.Refresh(true);
                PostRefresh();

                m_RebuildScheduled?.Pause();
            }
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

        private void RefreshSelection()
        {
            var selectedIndicesChanged = false;
            var previousSelectionCount = m_Selection.indexCount;
            m_Selection.items.Clear();

            if (viewController?.itemsSource == null)
            {
                m_Selection.ClearIndices();
                NotifyIfChanged();
                return;
            }

            // O(m) where `m` is m_SelectedIds.Count now, instead of itemsSource.Count.
            if (m_Selection.idCount > 0)
            {
                // Add selected objects to working lists.
                using var pool = ListPool<int>.Get(out var list);
                foreach (var id in m_Selection.selectedIds)
                {
                    var index = viewController.GetIndexForId(id);
                    if (index < 0)
                    {
                        selectedIndicesChanged = true; // Item is not there anymore.
                        continue;
                    }

                    if (!m_Selection.ContainsIndex(index))
                    {
                        selectedIndicesChanged = true;  // Index of a selection changed.
                    }

                    list.Add(index);
                }

                // Rebuild selected indices/items lists.
                m_Selection.ClearIndices();
                foreach (var index in list)
                {
                    m_Selection.AddIndex(index, viewController.GetItemForIndex(index));
                }
            }

            NotifyIfChanged();
            return;

            void NotifyIfChanged()
            {
                // Compare selection to raise the event if it changed.
                if (selectedIndicesChanged || m_Selection.indexCount != previousSelectionCount)
                {
                    NotifyOfSelectionChange();
                }
            }
        }

        private protected virtual void PostRefresh()
        {
            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = m_ScrollView.layout.height;

            if (panel== null || float.IsNaN(m_ScrollView.layout.height))
                return;

            Resize(m_ScrollView.layout.size);
        }

        /// <summary>
        /// Scrolls to a specific VisualElement.
        /// </summary>
        /// <param name="visualElement">The element to scroll to.</param>
        public void ScrollTo(VisualElement visualElement)
        {
            m_ScrollView.ScrollTo(visualElement);
        }

        /// <summary>
        /// Scrolls to a specific item index and makes it visible.
        /// </summary>
        /// <param name="index">Item index to scroll to. Specify -1 to make the last item visible.</param>
        public void ScrollToItem(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            virtualizationController.ScrollToItem(index);
        }

        /// <summary>
        /// Obsolete. Use <see cref="BaseVerticalCollectionView.ScrollToItemById"/> instead.
        /// </summary>
        /// <param name="id">Item id to scroll to.</param>
        [Obsolete("ScrollToId() has been deprecated. Use ScrollToItemById() instead. (UnityUpgradable) -> ScrollToItemById(*)", false)]
        public void ScrollToId(int id)
        {
            ScrollToItemById(id);
        }

        /// <summary>
        /// Scrolls to a specific item id and makes it visible.
        /// </summary>
        /// <param name="id">Item id to scroll to.</param>
        public void ScrollToItemById(int id)
        {
            if (!HasValidDataAndBindings())
                return;

            var index = viewController.GetIndexForId(id);
            virtualizationController.ScrollToItem(index);
        }

        private void OnScroll(Vector2 offset)
        {
            if (!HasValidDataAndBindings())
                return;

            virtualizationController.OnScroll(offset);
        }

        private void Resize(Vector2 size)
        {
            virtualizationController.Resize(size);
            m_LastHeight = size.y;
            virtualizationController.UpdateBackground();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            m_ScrollView.contentContainer.AddManipulator(m_NavigationManipulator = new KeyboardNavigationManipulator(Apply));
            m_ScrollView.contentContainer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.contentContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.contentContainer.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.contentContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.originPanel == null)
                return;

            m_ScrollView.contentContainer.RemoveManipulator(m_NavigationManipulator);
            m_ScrollView.contentContainer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            m_ScrollView.contentContainer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            m_ScrollView.contentContainer.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            m_ScrollView.contentContainer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        }

        // TODO: make private. This doesn't need to be in the public API. Unit tests can be implemented with SendEvent.
        // Obsoleted as error for 2022.2. We can remove completely in the next version.
        [Obsolete("OnKeyDown is obsolete and will be removed from ListView. Use the event system instead, i.e. SendEvent(EventBase e).", true)]
        public void OnKeyDown(KeyDownEvent evt)
        {
            m_NavigationManipulator.OnKeyDown(evt);
        }

        private bool Apply(KeyboardNavigationOperation op, bool shiftKey, bool altKey)
        {
            if (selectionType == SelectionType.None || !HasValidDataAndBindings())
            {
                return false;
            }

            void HandleSelectionAndScroll(int index)
            {
                if (index < 0 || index >= m_ViewController.itemsSource.Count)
                    return;

                if (selectionType == SelectionType.Multiple && shiftKey && m_Selection.indexCount != 0)
                {
                    DoRangeSelection(index);
                }
                else
                {
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
                    itemsChosen?.Invoke(selectedItems);
                    ScrollToItem(selectedIndex);
                    return true;
                case KeyboardNavigationOperation.Previous:
                    if (selectedIndex > 0)
                    {
                        HandleSelectionAndScroll(selectedIndex - 1);
                        return true;
                    }
                    break; // Allow focus to move outside the ListView
                case KeyboardNavigationOperation.Next:
                    if (selectedIndex + 1 < m_ViewController.itemsSource.Count)
                    {
                        HandleSelectionAndScroll(selectedIndex + 1);
                        return true;
                    }
                    break; // Allow focus to move outside the ListView
                case KeyboardNavigationOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case KeyboardNavigationOperation.End:
                    HandleSelectionAndScroll(m_ViewController.itemsSource.Count - 1);
                    return true;
                case KeyboardNavigationOperation.PageDown:
                    if (m_Selection.indexCount > 0)
                    {
                        var selectionDown = m_IsRangeSelectionDirectionUp ? m_Selection.minIndex : m_Selection.maxIndex;
                        HandleSelectionAndScroll(Mathf.Min(viewController.itemsSource.Count - 1, selectionDown + (virtualizationController.visibleItemCount - 1)));
                    }
                    return true;
                case KeyboardNavigationOperation.PageUp:
                    if (m_Selection.indexCount > 0)
                    {
                        var selectionUp = m_IsRangeSelectionDirectionUp ? m_Selection.minIndex : m_Selection.maxIndex;
                        HandleSelectionAndScroll(Mathf.Max(0, selectionUp - (virtualizationController.visibleItemCount - 1)));
                    }
                    return true;
                case KeyboardNavigationOperation.MoveRight:
                    if (m_Selection.indexCount > 0)
                    {
                        return HandleItemNavigation(true, altKey);
                    }
                    break;
                case KeyboardNavigationOperation.MoveLeft:
                    if (m_Selection.indexCount > 0)
                    {
                        return HandleItemNavigation(false, altKey);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            return false;
        }

        private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            var shiftKey = sourceEvent is KeyDownEvent { shiftKey: true } or INavigationEvent { shiftKey: true };
            var altKey = sourceEvent is KeyDownEvent { altKey: true } or INavigationEvent { altKey: true };
            if (Apply(op, shiftKey, altKey))
            {
                sourceEvent.StopPropagation();
            }

            focusController?.IgnoreEvent(sourceEvent);
        }

        private protected virtual bool HandleItemNavigation(bool moveIn, bool altKey)
        {
            // We could possibly want to expand child items here.
            return false;
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            // Support cases where PointerMove corresponds to a MouseDown or MouseUp event with multiple buttons.
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) == 0)
                {
                    ProcessPointerUp(evt);
                }
                else
                {
                    ProcessPointerDown(evt);
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            ProcessPointerDown(evt);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            ClearSelection();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            ProcessPointerUp(evt);
        }

        private Vector3 m_TouchDownPosition;
        private long m_LastPointerDownTimeStamp;
        private int m_PointerDownCount;

        private void ProcessPointerDown(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button is not ((int)MouseButton.LeftMouse or (int)MouseButton.RightMouse))
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                m_TouchDownPosition = evt.position;
                var pointerDownTimeStamp = (evt as PointerDownEvent)?.timestamp ?? 0;
                m_PointerDownCount = pointerDownTimeStamp - m_LastPointerDownTimeStamp < Event.GetDoubleClickTime() ? m_PointerDownCount + 1 : 1;
                m_LastPointerDownTimeStamp = pointerDownTimeStamp;
                return;
            }
            else
            {
                m_PointerDownCount = evt.clickCount;
            }

            DoSelect(evt.localPosition, evt.button, m_PointerDownCount, evt.actionKey, evt.shiftKey);
        }

        private void ProcessPointerUp(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button is not ((int)MouseButton.LeftMouse or (int)MouseButton.RightMouse))
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                var delta = evt.position - m_TouchDownPosition;
                if (delta.sqrMagnitude <= ScrollView.ScrollThresholdSquared)
                {
                    DoSelect(evt.localPosition, evt.button, m_PointerDownCount, evt.actionKey, evt.shiftKey);
                }

                // Reset the pointer down counter if it's passed the double click time.
                var pointerUpTimeStamp = (evt as PointerUpEvent)?.timestamp ?? 0;
                m_PointerDownCount = pointerUpTimeStamp - m_LastPointerDownTimeStamp < Event.GetDoubleClickTime() ? m_PointerDownCount : 0;
            }
            else
            {
                var clickedIndex = virtualizationController.GetIndexFromPosition(evt.localPosition);
                if (selectionType == SelectionType.Multiple
                    && evt.button == (int)MouseButton.LeftMouse
                    && !evt.shiftKey
                    && !evt.actionKey
                    && m_Selection.indexCount > 1
                    && m_Selection.ContainsIndex(clickedIndex))
                {
                    ProcessSingleClick(clickedIndex);
                }
            }
        }

        private void DoSelect(Vector2 localPosition, int mouseButton, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = virtualizationController.GetIndexFromPosition(localPosition);
            var effectiveClickCount = (m_Selection.indexCount > 0 && m_Selection.FirstIndex() != clickedIndex) ? 1 : (clickCount > 2) ? 2 : clickCount;
            if (clickedIndex > viewController.itemsSource.Count - 1)
                return;

            if (selectionType == SelectionType.None)
                return;

            var clickedItemId = viewController.GetIdForIndex(clickedIndex);

            switch (effectiveClickCount)
            {
                case 1:
                    if (selectionType == SelectionType.Multiple && actionKey)
                    {
                        // Add/remove single clicked element
                        if (m_Selection.ContainsId(clickedItemId))
                            RemoveFromSelection(clickedIndex);
                        else
                            AddToSelection(clickedIndex);
                    }
                    else if (selectionType == SelectionType.Multiple && shiftKey)
                    {
                        if (m_Selection.indexCount == 0)
                        {
                            SetSelection(clickedIndex);
                        }
                        else
                        {
                            DoRangeSelection(clickedIndex);
                        }
                    }
                    else if (selectionType == SelectionType.Multiple && m_Selection.ContainsIndex(clickedIndex))
                    {
                        // Do nothing, selection will be processed OnPointerUp.
                        // If drag and drop will be started ListViewDragger will capture the mouse and ListView will not receive the mouse up event.
                        m_SelectionNotChanged?.Invoke();
                    }
                    else // single
                    {
                        if (selectionType == SelectionType.Single && m_Selection.ContainsIndex(clickedIndex))
                        {
                            m_SelectionNotChanged?.Invoke();
                        }
                        else
                        {
                            SetSelection(clickedIndex);
                        }

                        // Only choose on left mouse button
                        if (allowSingleClickChoice && mouseButton == (int)MouseButton.LeftMouse)
                            itemsChosen?.Invoke(selectedItems);
                    }

                    break;
                case 2:
                    if (itemsChosen == null)
                        return;

                    var wasClickedIndexInSelection = false;
                    foreach (var index in selectedIndices)
                    {
                        if (clickedIndex == index)
                        {
                            wasClickedIndexInSelection = true;
                            break;
                        }
                    }

                    ProcessSingleClick(clickedIndex);

                    // Only invoke itemsChosen if we're clicking on the same entry. Case UUM-42450.
                    if (!wasClickedIndexInSelection)
                        return;

                    if (!allowSingleClickChoice && mouseButton == (int)MouseButton.LeftMouse)
                        itemsChosen?.Invoke(selectedItems);
                    break;
            }
        }

        internal void DoRangeSelection(int rangeSelectionFinalIndex)
        {
            var selectionOrigin = m_IsRangeSelectionDirectionUp ? m_Selection.maxIndex : m_Selection.minIndex;

            ClearSelectionWithoutValidation();

            // Add range
            var range = new List<int>();
            m_IsRangeSelectionDirectionUp = rangeSelectionFinalIndex < selectionOrigin;
            if (m_IsRangeSelectionDirectionUp)
            {
                for (var i = rangeSelectionFinalIndex; i <= selectionOrigin; i++)
                    range.Add(i);
            }
            else
            {
                for (var i = rangeSelectionFinalIndex; i >= selectionOrigin; i--)
                    range.Add(i);
            }

            AddToSelection(range);
        }

        private void ProcessSingleClick(int clickedIndex)
        {
            SetSelection(clickedIndex);
        }

        internal void SelectAll()
        {
            if (!HasValidDataAndBindings())
                return;

            if (selectionType != SelectionType.Multiple)
            {
                return;
            }

            for (var index = 0; index < m_ViewController.itemsSource.Count; index++)
            {
                var id = viewController.GetIdForIndex(index);
                var item = viewController.GetItemForIndex(index);

                foreach (var recycledItem in activeItems)
                    if (recycledItem.id == id)
                        recycledItem.SetSelected(true);

                if (!m_Selection.ContainsId(id))
                {
                    m_Selection.AddId(id);
                    m_Selection.AddIndex(index, item);
                }
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        /// <summary>
        /// Adds an item to the collection of selected items.
        /// </summary>
        /// <param name="index">Item index.</param>
        public void AddToSelection(int index)
        {
            AddToSelection(new[] { index });
        }

        internal void AddToSelection(IList<int> indexes)
        {
            if (!HasValidDataAndBindings() || indexes == null || indexes.Count == 0)
                return;

            foreach (var index in indexes)
            {
                AddToSelectionWithoutValidation(index);
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void AddToSelectionWithoutValidation(int index)
        {
            if (m_Selection.ContainsIndex(index))
                return;

            var id = viewController.GetIdForIndex(index);
            var item = viewController.GetItemForIndex(index);

            foreach (var recycledItem in activeItems)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(true);

            m_Selection.AddId(id);
            m_Selection.AddIndex(index, item);
        }

        /// <summary>
        /// Removes an item from the collection of selected items.
        /// </summary>
        /// <param name="index">The item index.</param>
        public void RemoveFromSelection(int index)
        {
            if (!HasValidDataAndBindings())
                return;

            RemoveFromSelectionWithoutValidation(index);
            NotifyOfSelectionChange();
            SaveViewData();
        }

        private void RemoveFromSelectionWithoutValidation(int index)
        {
            if (!m_Selection.TryRemove(index))
                return;

            var id = viewController.GetIdForIndex(index);

            foreach (var recycledItem in activeItems)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(false);

            m_Selection.RemoveId(id);
        }

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

            SetSelection(new[] { index });
        }

        /// <summary>
        /// Sets a collection of selected items.
        /// </summary>
        /// <param name="indices">The collection of the indices of the items to be selected.</param>
        public void SetSelection(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, true);
        }

        /// <summary>
        /// Sets a collection of selected items without triggering a selection change callback.
        /// </summary>
        /// <param name="indices">The collection of items to be selected.</param>
        public void SetSelectionWithoutNotify(IEnumerable<int> indices)
        {
            SetSelectionInternal(indices, false);
        }

        internal void SetSelectionInternal(IEnumerable<int> indices, bool sendNotification)
        {
            if (!HasValidDataAndBindings() || indices == null)
                return;

            if (MatchesExistingSelection(indices))
                return;

            ClearSelectionWithoutValidation();

            // If possible resize indices so we can better handle large selections. (UUM-74996)
            if (indices is ICollection collection && m_Selection.capacity < collection.Count)
            {
                m_Selection.capacity = collection.Count;
            }

            foreach (var index in indices)
                AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();

            SaveViewData();
        }

        private bool MatchesExistingSelection(IEnumerable<int> indices)
        {
            var indicesCollection = indices as IList<int>;
            List<int> pooled = null;
            try
            {
                if (indicesCollection == null)
                {
                    pooled = ListPool<int>.Get();
                    pooled.AddRange(indices);
                    indicesCollection = pooled;
                }

                if (indicesCollection.Count != m_Selection.indexCount)
                    return false;

                for (var i = 0; i < indicesCollection.Count; ++i)
                {
                    // The order of the indices is important.
                    if (indicesCollection[i] != m_Selection.indices[i])
                        return false;
                }

                return true;
            }
            finally
            {
                if (pooled != null)
                    ListPool<int>.Release(pooled);
            }
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            selectionChanged?.Invoke(selectedItems);
            selectedIndicesChanged?.Invoke(m_Selection.indices);
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            if (!HasValidDataAndBindings() || m_Selection.idCount == 0)
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        private void ClearSelectionWithoutValidation()
        {
            foreach (var recycledItem in activeItems)
                recycledItem.SetSelected(false);

            m_Selection.Clear();
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();
            OverwriteFromViewData(this, key);
            m_ScrollView.UpdateContentViewTransform();
        }

        [EventInterest(typeof(PointerUpEvent), typeof(FocusInEvent), typeof(FocusOutEvent),
            typeof(NavigationSubmitEvent))]
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
                m_VirtualizationController?.OnFocusIn(evt.elementTarget);
            }
            else if (evt.eventTypeId == FocusOutEvent.TypeId())
            {
                m_VirtualizationController?.OnFocusOut(((FocusOutEvent)evt).relatedTarget as VisualElement);
            }
            else if (evt.eventTypeId == NavigationSubmitEvent.TypeId())
            {
                if (evt.target == this)
                {
                    m_ScrollView.contentContainer.Focus();
                }
            }
        }

        [EventInterest(EventInterestOptions.Inherit)]
        [Obsolete("ExecuteDefaultAction override has been removed because default event handling was migrated to HandleEventBubbleUp. Please use HandleEventBubbleUp.", false)]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.width, evt.oldRect.width) &&
                Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            Resize(evt.newRect.size);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out var height))
            {
                if (Math.Abs(m_FixedItemHeight - height) > float.Epsilon)
                {
                    m_FixedItemHeight = height;
                    RefreshItems();
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_Selection.selectedIds = m_SelectedIds;
            RefreshItems();
        }
    }
}
