using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to display alternating background colors for ListView rows.
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
    /// A ListView is a vertically scrollable area that links to, and displays, a list of items.
    /// </summary>
    /// <remarks>
    /// <p>A <see cref="ListView"/> is a <see cref="ScrollView"/> with additional logic to display a list of vertically-arranged
    /// VisualElements. Each VisualElement in the list is bound to a corresponding element in a data-source list. The
    /// data-source list can contain elements of any type.</p>
    ///
    /// <p>The logic required to create VisualElements, and to bind them to or unbind them from the data source, varies depending
    /// on the intended result. It's up to you to implement logic that is appropriate to your use case. For the ListView to function
    /// correctly, you must supply at least the following:</p>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <description><see cref="ListView.itemHeight"/></description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="ListView.makeItem"/></description>
    ///   </item>
    ///   <item>
    ///     <description><see cref="ListView.bindItem"/></description>
    ///   </item>
    /// </list>
    ///
    /// <p>The ListView creates enough VisualElements for the visible items, and supports binding many more. As the user scrolls, the ListView
    /// recycles VisualElements and re-binds them to new data items.</p>
    ///
    /// <list type="bullet">
    ///   <item>
    ///     <description>To set the height of a single item in pixels, set the <c>item-height</c> property in UXML or the
    ///     <see cref="ListView.itemHeight"/> property in C# to the desired value.</description>
    ///   </item>
    ///   <item>
    ///     <description>To show a border around the scrollable area, set the <c>show-border</c> property in UXML or the
    ///     <see cref="ListView.showBorder"/> property in C# to <c>true</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description>By default, the user can select one element in the list at a time. To change the default selection
    ///     use the <c>selection-type</c> property in UXML or the<see cref="ListView.selectionType"/> property in C#.
    ///        <list type="bullet">
    ///          <item>
    ///            <description>To allow the user to select more than one element simultaneously, set the property to
    ///            <c>Selection.Multiple</c>.</description>
    ///          </item>
    ///          <item>
    ///            <description>To prevent the user from selecting items, set the property to <c>Selection.None</c>.</description>
    ///          </item>
    ///        </list>
    ///      </description>
    ///   </item>
    ///   <item>
    ///     <description>By default, all rows in the ListView have same background color. To make the row background colors
    ///     alternate, set the <c>show-alternating-row-backgrounds</c> property in UXML or the
    ///     <see cref="ListView.showAlternatingRowBackgrounds"/> property in C# to
    ///     <see cref="AlternatingRowBackground.ContentOnly"/> or
    ///     <see cref="AlternatingRowBackground.All"/>. For details, see <see cref="AlternatingRowBackground"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>By default, the user can't reorder the list's elements. To allow the user to drag the elements
    ///     to reorder them, set the <c>reorderable</c> property in UXML or the <see cref="ListView.reorderable"/>
    ///     property in C# to to true.</description>
    ///   </item>
    ///   <item>
    ///     <description>To make the first item in the ListView display the number of items in the list, set the
    ///     <c>show-bound-collection-size</c> property in UXML or the <see cref="ListView.showBoundCollectionSize"/>
    ///     to true. This is useful for debugging.</description>
    ///   </item>
    ///   <item>
    ///     <description>By default, the ListView's scroller element only scrolls vertically.
    ///     To enable horizontal scrolling when the displayed element is wider than the visible area, set the
    ///     <c>horizontal-scrolling-enabled</c> property in UXML or the <see cref="ListView.horizontalScrollingEnabled"/>
    ///     to true.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// public class ListViewExampleWindow : EditorWindow
    /// {
    ///     [MenuItem("Window/ListViewExampleWindow")]
    ///     public static void OpenDemoManual()
    ///     {
    ///         GetWindow<ListViewExampleWindow>().Show();
    ///     }
    ///
    ///     public void OnEnable()
    ///     {
    ///         // Create a list of data. In this case, numbers from 1 to 1000.
    ///         const int itemCount = 1000;
    ///         var items = new List<string>(itemCount);
    ///         for (int i = 1; i <= itemCount; i++)
    ///             items.Add(i.ToString());
    ///
    ///         // The "makeItem" function is called when the
    ///         // ListView needs more items to render.
    ///         Func<VisualElement> makeItem = () => new Label();
    ///
    ///         // As the user scrolls through the list, the ListView object
    ///         // recycles elements created by the "makeItem" function,
    ///         // and invoke the "bindItem" callback to associate
    ///         // the element with the matching data item (specified as an index in the list).
    ///         Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = items[i];
    ///
    ///         // Provide the list view with an explict height for every row
    ///         // so it can calculate how many items to actually display
    ///         const int itemHeight = 16;
    ///
    ///         var listView = new ListView(items, itemHeight, makeItem, bindItem);
    ///
    ///         listView.selectionType = SelectionType.Multiple;
    ///
    ///         listView.onItemsChosen += objects => Debug.Log(objects);
    ///         listView.onSelectionChange += objects => Debug.Log(objects);
    ///
    ///         listView.style.flexGrow = 1.0f;
    ///
    ///         rootVisualElement.Add(listView);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class ListView : BindableElement, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Instantiates a <see cref="ListView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<ListView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the ListView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            private readonly UxmlIntAttributeDescription m_ItemHeight = new UxmlIntAttributeDescription { name = "item-height", obsoleteNames = new[] { "itemHeight" }, defaultValue = s_DefaultItemHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_ShowBoundCollectionSize = new UxmlBoolAttributeDescription { name = "show-bound-collection-size", defaultValue = true };
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
            /// Initializes <see cref="ListView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var itemHeight = 0;
                var listView = (ListView)ve;
                listView.reorderable = m_Reorderable.GetValueFromBag(bag, cc);

                // Avoid setting itemHeight unless it's explicitly defined.
                // Setting itemHeight property will activate inline property mode.
                if (m_ItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    listView.itemHeight = itemHeight;
                }

                listView.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                listView.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                listView.showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
                listView.showBoundCollectionSize = m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
                listView.horizontalScrollingEnabled = m_HorizontalScrollingEnabled.GetValueFromBag(bag, cc);
            }
        }

        internal class RecycledItem
        {
            public const int kUndefinedIndex = -1;
            public VisualElement element { get; private set; }
            public int index;
            public int id;

            public RecycledItem(VisualElement element)
            {
                this.element = element;
                index = id = kUndefinedIndex;
                element.AddToClassList(itemUssClassName);
            }

            public void DetachElement()
            {
                element.RemoveFromClassList(itemUssClassName);
                element = null;
            }

            public void SetSelected(bool selected)
            {
                if (element != null)
                {
                    if (selected)
                    {
                        element.AddToClassList(itemSelectedVariantUssClassName);
                        element.pseudoStates |= PseudoStates.Checked;
                    }
                    else
                    {
                        element.RemoveFromClassList(itemSelectedVariantUssClassName);
                        element.pseudoStates &= ~PseudoStates.Checked;
                    }
                }
            }
        }

        /// <summary>
        /// Callback triggered when a user double-clicks an item to activate it. This is different from selecting the item.
        /// </summary>
        [Obsolete("onItemChosen is obsolete, use onItemsChosen instead")]
        public event Action<object> onItemChosen;
        /// <summary>
        /// Callback triggered when the user acts on a selection of one or more items, for example by double-clicking or pressing Enter.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items chosen.
        /// </remarks>
        public event Action<IEnumerable<object>> onItemsChosen;

        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        [Obsolete("onSelectionChanged is obsolete, use onSelectionChange instead")]
        public event Action<List<object>> onSelectionChanged;
        /// <summary>
        /// Callback triggered when the selection changes.
        /// </summary>
        /// <remarks>
        /// This callback receives an enumerable that contains the item or items selected.
        /// </remarks>
        public event Action<IEnumerable<object>> onSelectionChange;


        private IList m_ItemsSource;
        /// <summary>
        /// The data source for list items.
        /// </summary>
        /// <remarks>
        /// This list contains the items that the <see cref="ListView"/> displays.
        ///
        /// This property must be set for the list view to function.
        /// </remarks>
        public IList itemsSource
        {
            get { return m_ItemsSource; }
            set
            {
                m_ItemsSource = value;
                Refresh();
            }
        }

        Func<VisualElement> m_MakeItem;
        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        /// <remarks>
        /// This callback needs to call a function that constructs a blank <see cref="VisualElement"/> that is
        /// bound to an element from the list.
        ///
        /// The ListView automatically creates enough elements to fill the visible area, and adds more if the area
        /// is expanded. As the user scrolls, the ListView cycles elements in and out as they appear or disappear.
        ///
        ///  This property must be set for the list view to function.
        /// </remarks>
        public Func<VisualElement> makeItem
        {
            get { return m_MakeItem; }
            set
            {
                if (m_MakeItem == value)
                    return;
                m_MakeItem = value;
                Refresh();
            }
        }

        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to unbind, and the index of the
        /// element to unbind it from.
        /// </remarks>
        public Action<VisualElement, int> unbindItem { get; set; }

        private Action<VisualElement, int> m_BindItem;
        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to bind, and the index of the
        /// element to bind it to.
        /// </remarks>
        public Action<VisualElement, int> bindItem
        {
            get { return m_BindItem; }
            set
            {
                m_BindItem = value;
                Refresh();
            }
        }

        private Func<int, int> m_GetItemId;
        internal Func<int, int> getItemId
        {
            get { return m_GetItemId; }
            set
            {
                m_GetItemId = value;
                Refresh();
            }
        }

        /// <summary>
        /// The computed pixel-aligned height for the list elements.
        /// </summary>
        /// <remarks>
        /// This value changes depending on the current panel's DPI scaling.
        /// </remarks>
        /// <seealso cref="ListView.itemHeight"/>
        public float resolvedItemHeight
        {
            get
            {
                var dpiScaling = scaledPixelsPerPoint;
                return Mathf.Round(itemHeight * dpiScaling) / dpiScaling;
            }
        }

        internal List<RecycledItem> Pool
        {
            get { return m_Pool; }
        }

        internal int m_ItemHeight = s_DefaultItemHeight;
        internal bool m_ItemHeightIsInline;

        /// <summary>
        /// The height of a single item in the list, in pixels.
        /// </summary>
        /// <remarks>
        /// ListView requires that all visual elements have the same height so that it can calculate the
        /// scroller size.
        ///
        /// This property must be set for the list view to function.
        /// </remarks>
        public int itemHeight
        {
            get { return m_ItemHeight; }
            set
            {
                m_ItemHeightIsInline = true;
                if (m_ItemHeight != value)
                {
                    m_ItemHeight = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Enable this property to display a border around the ListView.
        /// </summary>
        /// <remarks>
        /// If set to true, a border appears around the ScrollView.
        /// </remarks>
        public bool showBorder
        {
            get { return ClassListContains(borderUssClassName); }
            set { EnableInClassList(borderUssClassName, value); }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the user can drag list items to reorder them.
        /// </summary>
        /// <remarks>
        /// Set this value to true to allow the user to drag and drop the items in the list. The ListView
        /// provides a default controller to allow standard behavior. It also automatically handles reordering
        /// the items in the data source.
        /// </remarks>
        public bool reorderable
        {
            get
            {
                var controller = m_Dragger?.dragAndDropController;
                return controller != null && controller.enableReordering;
            }
            set
            {
                if (m_Dragger?.dragAndDropController == null)
                {
                    if (value)
                        SetDragAndDropController(new ListViewReorderableDragAndDropController(this));

                    return;
                }

                var controller = m_Dragger.dragAndDropController;
                if (controller != null)
                    controller.enableReordering = value;
            }
        }


        // Persisted.
        [SerializeField]
        private float m_ScrollOffset;

        // Persisted. It's why this can't be a HashSet(). :(
        [SerializeField]
        private readonly List<int> m_SelectedIds = new List<int>();

        internal List<int> currentSelectionIds => m_SelectedIds;

        // Not persisted! Just used for fast lookups of selected indices and object references.
        // This is to avoid also having a mapping from index/object ref to index for the entire
        // items source.
        private readonly List<int> m_SelectedIndices = new List<int>();
        private readonly List<object> m_SelectedItems = new List<object>();

        // Used to store the focused element to enable scrolling without losing it.
        private int m_LastFocusedElementIndex = -1;
        private List<int> m_LastFocusedElementTreeChildIndexes = new List<int>();

        private bool m_IsRangeSelectionDirectionUp;
        private ListViewDragger m_Dragger;

        /// <summary>
        /// Returns or sets the selected item's index in the data source. If multiple items are selected, returns the
        /// first selected item's index. If multiple items are provided, sets them all as selected.
        /// </summary>
        public int selectedIndex
        {
            get { return m_SelectedIndices.Count == 0 ? -1 : m_SelectedIndices.First(); }
            set { SetSelection(value); }
        }

        /// <summary>
        /// Returns the indices of selected items in the data source. Always returns an enumerable, even if no item  is selected, or a
        /// single item is selected.
        /// </summary>
        public IEnumerable<int> selectedIndices => m_SelectedIndices;

        /// <summary>
        /// Returns the selected item from the data source. If multiple items are selected, returns the first selected item.
        /// </summary>
        public object selectedItem => m_SelectedItems.Count == 0 ? null : m_SelectedItems.First();

        /// <summary>
        /// Returns the selected items from the data source. Always returns an enumerable, even if no item is selected, or a single
        /// item is selected.
        /// </summary>
        public IEnumerable<object> selectedItems => m_SelectedItems;

        /// <summary>
        /// Returns the content container for the <see cref="ListView"/>. Because the ListView control automatically manages
        /// its content, this always returns null.
        /// </summary>
        public override VisualElement contentContainer => null;

        private SelectionType m_SelectionType;
        /// <summary>
        /// Controls the selection type.
        /// </summary>
        /// <remarks>
        /// You can set the ListView to make one item selectable at a time, make multiple items selectable, or disable selections completely.
        ///
        /// When you set the ListView to disable selections, any current selection is cleared.
        /// </remarks>
        public SelectionType selectionType
        {
            get { return m_SelectionType; }
            set
            {
                m_SelectionType = value;
                if (m_SelectionType == SelectionType.None)
                {
                    ClearSelection();
                }
            }
        }

        [SerializeField] private AlternatingRowBackground m_ShowAlternatingRowBackgrounds = AlternatingRowBackground.None;

        /// <summary>
        /// This property controls whether the background colors of ListView rows alternate. Takes a value from <see cref="AlternatingRowBackground"/>.
        /// </summary>
        public AlternatingRowBackground showAlternatingRowBackgrounds
        {
            get { return m_ShowAlternatingRowBackgrounds; }
            set
            {
                if (m_ShowAlternatingRowBackgrounds == value)
                    return;

                m_ShowAlternatingRowBackgrounds = value;
                Refresh();
            }
        }

        /// <summary>
        /// This property controls whether the list view displays the collection size (number of items).
        /// Set to true to display the collection size, false to omit it. Default is true.
        /// </summary>
        /// <remarks>
        /// When this property is set to true, Unity displays the collection size as the first item in the list, but does
        /// not make it an actual list item that is part of the list index. If you query for list index 0,
        /// Unity returns the first real list item, and not the collection size.
        ///
        /// This property is usually used to debug a ListView, because it indicates whether the data source is
        /// linked correctly. In production, the collection size is rarely displayed as a line item in a ListView.
        /// </remarks>>
        /// <seealso cref="UnityEditor.UIElements.BindingExtensions.Bind"/>
        public bool showBoundCollectionSize { get; set; } = true;

        private bool m_HorizontalScrollingEnabled;

        /// <summary>
        /// This property controls whether the ListView shows a horizontal scroll bar when its content
        /// does not fit in the visible area. Set this property to true to display a horizontal scroll bar,
        /// false to omit the horizontal scroll bar. The default value is False.
        /// </summary>
        public bool horizontalScrollingEnabled
        {
            get { return m_HorizontalScrollingEnabled; }
            set
            {
                if (m_HorizontalScrollingEnabled == value)
                    return;

                m_HorizontalScrollingEnabled = value;
                m_ScrollView.SetScrollViewMode(value ? ScrollViewMode.VerticalAndHorizontal : ScrollViewMode.Vertical);
            }
        }

        internal static readonly int s_DefaultItemHeight = 30;
        internal static CustomStyleProperty<int> s_ItemHeightProperty = new CustomStyleProperty<int>("--unity-item-height");

        private int m_FirstVisibleIndex;
        private float m_LastHeight;
        private List<RecycledItem> m_Pool = new List<RecycledItem>();
        internal readonly ScrollView m_ScrollView;
        KeyboardNavigationManipulator m_NavigationManipulator;

        private readonly VisualElement m_EmptyRows;
        private int m_LastItemIndex;

        // we keep this list in order to minimize temporary gc allocs
        private List<RecycledItem> m_ScrollInsertionList = new List<RecycledItem>();

        private const int k_ExtraVisibleItems = 2;
        private int m_VisibleItemCount;

        /// <summary>
        /// The USS class name for ListView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the ListView element. Any styling applied to
        /// this class affects every ListView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string ussClassName = "unity-list-view";
        /// <summary>
        /// The USS class name for ListView elements with a border.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to an instance of the ListView element if the instance's
        /// <see cref="ListView.showBorder"/> property is set to true. Any styling applied to this class
        /// affects every such ListView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string borderUssClassName = ussClassName + "--with-border";
        /// <summary>
        /// The USS class name of item elements in ListView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element the ListView contains. Any styling applied to
        /// this class affects every item element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name of the drag hover bar.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the bar that appears when an item element is dragged. The
        /// <see cref="ListView.reorderable"/> property must be true in order for items to be dragged.
        /// Any styling applied to this class affects every ListView located beside, or below the stylesheet in the
        /// visual tree.
        /// </remarks>
        public static readonly string dragHoverBarUssClassName = ussClassName + "__drag-hover-bar";
        /// <summary>
        /// The USS class name applied to an item element on drag hover.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the list element that is dragged. The <see cref="ListView.reorderable"/>
        /// property must be set to true for items to be draggable. Any styling applied to this class affects
        /// every ListView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemDragHoverUssClassName = itemUssClassName + "--drag-hover";
        /// <summary>
        /// The USS class name of selected item elements in the ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every selected element in the ListView. The <see cref="ListView.selectionType"/>
        /// property decides if zero, one, or more elements can be selected. Any styling applied to
        /// this class affects every ListView item located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string itemSelectedVariantUssClassName = itemUssClassName + "--selected";
        /// <summary>
        /// The USS class name for odd rows in the ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every odd-numbered item in the ListView when the
        /// <see cref="ListView.showAlternatingRowBackground"/> property is set to <c>ContentOnly</c> or <c>All</c>.
        /// When the <c>showAlternatingRowBackground</c> property is set to either of those values, odd-numbered items
        /// are displayed with a different background color than even-numbered items. This USS class is used to differentiate
        /// odd-numbered items from even-numbered items. When the <c>showAlternatingRowBackground</c> property is set to
        /// <c>None</c>, the USS class is not added, and any styling or behavior that relies on it is invalidated.
        /// </remarks>
        public static readonly string itemAlternativeBackgroundUssClassName = itemUssClassName + "--alternative-background";

        internal static readonly string s_BackgroundFillUssClassName = ussClassName + "__background";

        /// <summary>
        /// Creates a <see cref="ListView"/> with all default properties. The <see cref="ListView.itemSource"/>,
        /// <see cref="ListView.itemHeight"/>, <see cref="ListView.makeItem"/> and <see cref="ListView.bindItem"/> properties
        /// must all be set for the ListView to function properly.
        /// </summary>
        public ListView()
        {
            AddToClassList(ussClassName);

            selectionType = SelectionType.Single;
            m_ScrollOffset = 0.0f;

            m_ScrollView = new ScrollView();
            m_ScrollView.viewDataKey = "list-view__scroll-view";
            m_ScrollView.StretchToParentSize();
            m_ScrollView.verticalScroller.valueChanged += OnScroll;

            RegisterCallback<GeometryChangedEvent>(OnSizeChanged);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            m_ScrollView.contentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ScrollView.contentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            hierarchy.Add(m_ScrollView);

            m_ScrollView.contentContainer.focusable = true;
            m_ScrollView.contentContainer.usageHints &= ~UsageHints.GroupTransform; // Scroll views with virtualized content shouldn't have the "view transform" optimization

            m_EmptyRows = new VisualElement();
            m_EmptyRows.AddToClassList(s_BackgroundFillUssClassName);

            focusable = true;
            isCompositeRoot = true;
            delegatesFocus = true;
        }

        /// <summary>
        /// Constructs a <see cref="ListView"/>, with all required properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        public ListView(IList itemsSource, int itemHeight, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem) : this()
        {
            m_ItemsSource = itemsSource;
            m_ItemHeight = itemHeight;
            m_ItemHeightIsInline = true;

            m_MakeItem = makeItem;
            m_BindItem = bindItem;
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
        // Obsoleted for 2021.2. We can obsolete completely in the next version.
        [Obsolete("OnKeyDown is obsolete and will be removed from ListView. Use the event system instead, i.e. SendEvent(EventBase e).", false)]
        public void OnKeyDown(KeyDownEvent evt)
        {
            m_NavigationManipulator.OnKeyDown(evt);
        }

        private bool Apply(KeyboardNavigationOperation op, bool shiftKey)
        {
            if (!HasValidDataAndBindings())
            {
                return false;
            }

            void HandleSelectionAndScroll(int index)
            {
                if (selectionType == SelectionType.Multiple && shiftKey && m_SelectedIndices.Count != 0)
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
#pragma warning disable 618
                    if (selectedIndex >= 0 && selectedIndex < m_ItemsSource.Count)
                        onItemChosen?.Invoke(m_ItemsSource[selectedIndex]);
#pragma warning restore 618
                    onItemsChosen?.Invoke(m_SelectedItems);
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
                    if (selectedIndex + 1 < itemsSource.Count)
                    {
                        HandleSelectionAndScroll(selectedIndex + 1);
                        return true;
                    }
                    break; // Allow focus to move outside the ListView
                case KeyboardNavigationOperation.Begin:
                    HandleSelectionAndScroll(0);
                    return true;
                case KeyboardNavigationOperation.End:
                    HandleSelectionAndScroll(itemsSource.Count - 1);
                    return true;
                case KeyboardNavigationOperation.PageDown:
                    HandleSelectionAndScroll(Math.Min(itemsSource.Count - 1, selectedIndex + (int)(m_LastHeight / resolvedItemHeight)));
                    return true;
                case KeyboardNavigationOperation.PageUp:
                    HandleSelectionAndScroll(Math.Max(0, selectedIndex - (int)(m_LastHeight / resolvedItemHeight)));
                    return true;
            }

            return false;
        }

        private void Apply(KeyboardNavigationOperation op, EventBase sourceEvent)
        {
            var shiftKey = (sourceEvent as KeyDownEvent)?.shiftKey ?? false;
            if (Apply(op, shiftKey))
            {
                sourceEvent.StopPropagation();
                sourceEvent.PreventDefault();
            }
        }

        /// <summary>
        /// Scrolls to a specific item index and makes it visible.
        /// </summary>
        /// <param name="index">Item index to scroll to. Specify -1 to make the last item visible.</param>
        public void ScrollToItem(int index)
        {
            if (!HasValidDataAndBindings())
                throw new InvalidOperationException("Can't scroll without valid source, bind method, or factory method.");

            if (m_VisibleItemCount == 0 || index < -1)
                return;

            var pixelAlignedItemHeight = resolvedItemHeight;
            if (index == -1)
            {
                // Scroll to last item
                int actualCount = (int)(m_LastHeight / pixelAlignedItemHeight);
                if (itemsSource.Count < actualCount)
                    m_ScrollView.scrollOffset = new Vector2(0, 0);
                else
                    m_ScrollView.scrollOffset = new Vector2(0, (itemsSource.Count + 1) * pixelAlignedItemHeight);
            }
            else if (m_FirstVisibleIndex >= index)
            {
                m_ScrollView.scrollOffset = Vector2.up * (pixelAlignedItemHeight * index);
            }
            else // index > first
            {
                var actualCount = (int)(m_LastHeight / pixelAlignedItemHeight);
                if (index < m_FirstVisibleIndex + actualCount)
                    return;

                var d = index - actualCount + 1;    // +1 ensures targeted element is fully visible
                var visibleOffset = pixelAlignedItemHeight - (m_LastHeight - actualCount * pixelAlignedItemHeight);
                var yScrollOffset = pixelAlignedItemHeight * d + visibleOffset;

                m_ScrollView.scrollOffset =  new Vector2(m_ScrollView.scrollOffset.x, yScrollOffset);
            }
        }

        private long m_TouchDownTime = 0;
        private Vector3 m_TouchDownPosition;

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

        private void ProcessPointerDown(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                m_TouchDownTime = ((EventBase)evt).timestamp;
                m_TouchDownPosition = evt.position;
                return;
            }

            DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
        }

        private void ProcessPointerUp(IPointerEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (!evt.isPrimary)
                return;

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (evt.pointerType != PointerType.mouse)
            {
                var delay = ((EventBase)evt).timestamp - m_TouchDownTime;
                var delta = evt.position - m_TouchDownPosition;
                if (delay < 500 && delta.sqrMagnitude <= 100)
                {
                    DoSelect(evt.localPosition, evt.clickCount, evt.actionKey, evt.shiftKey);
                }
            }
            else
            {
                var clickedIndex = (int)(evt.localPosition.y / itemHeight);
                if (selectionType == SelectionType.Multiple
                    && !evt.shiftKey
                    && !evt.actionKey
                    && m_SelectedIndices.Count > 1
                    && m_SelectedIndices.Contains(clickedIndex))
                {
                    ProcessSingleClick(clickedIndex);
                }
            }
        }

        private void DoSelect(Vector2 localPosition, int clickCount, bool actionKey, bool shiftKey)
        {
            var clickedIndex = (int)(localPosition.y / resolvedItemHeight);
            if (clickedIndex > m_ItemsSource.Count - 1)
                return;

            var clickedItemId = GetIdFromIndex(clickedIndex);
            switch (clickCount)
            {
                case 1:
                    if (selectionType == SelectionType.None)
                        return;

                    if (selectionType == SelectionType.Multiple && actionKey)
                    {
                        // Add/remove single clicked element
                        if (m_SelectedIds.Contains(clickedItemId))
                            RemoveFromSelection(clickedIndex);
                        else
                            AddToSelection(clickedIndex);
                    }
                    else if (selectionType == SelectionType.Multiple && shiftKey)
                    {
                        if (m_SelectedIndices.Count == 0)
                        {
                            SetSelection(clickedIndex);
                        }
                        else
                        {
                            DoRangeSelection(clickedIndex);
                        }
                    }
                    else if (selectionType == SelectionType.Multiple && m_SelectedIndices.Contains(clickedIndex))
                    {
                        // Do noting, selection will be processed OnPointerUp.
                        // If drag and drop will be started ListViewDragger will capture the mouse and ListView will not receive the mouse up event.
                    }
                    else // single
                    {
                        SetSelection(clickedIndex);
                    }
                    break;
                case 2:
                    if (onItemsChosen != null)
                    {
                        ProcessSingleClick(clickedIndex);
                    }

                    onItemsChosen?.Invoke(m_SelectedItems);
                    break;
            }
        }

        private void DoRangeSelection(int rangeSelectionFinalIndex)
        {
            var selectionOrigin = m_IsRangeSelectionDirectionUp ? m_SelectedIndices.Max() : m_SelectedIndices.Min();

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

            for (var index = 0; index < itemsSource.Count; index++)
            {
                var id = GetIdFromIndex(index);
                var item = m_ItemsSource[index];

                foreach (var recycledItem in m_Pool)
                    if (recycledItem.id == id)
                        recycledItem.SetSelected(true);

                if (!m_SelectedIds.Contains(id))
                {
                    m_SelectedIds.Add(id);
                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(item);
                }
            }

            NotifyOfSelectionChange();
            SaveViewData();
        }

        private int GetIdFromIndex(int index)
        {
            if (m_GetItemId == null)
                return index;
            else
                return m_GetItemId(index);
        }

        /// <summary>
        /// Adds an item to the collection of selected items.
        /// </summary>
        /// <param name="index">Item index.</param>
        public void AddToSelection(int index)
        {
            AddToSelection(new[] {index});
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
            if (m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(true);

            m_SelectedIds.Add(id);
            m_SelectedIndices.Add(index);
            m_SelectedItems.Add(item);
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
            if (!m_SelectedIndices.Contains(index))
                return;

            var id = GetIdFromIndex(index);
            var item = m_ItemsSource[index];

            foreach (var recycledItem in m_Pool)
                if (recycledItem.id == id)
                    recycledItem.SetSelected(false);

            m_SelectedIds.Remove(id);
            m_SelectedIndices.Remove(index);
            m_SelectedItems.Remove(item);
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

            ClearSelectionWithoutValidation();
            foreach (var index in indices)
                AddToSelectionWithoutValidation(index);

            if (sendNotification)
                NotifyOfSelectionChange();

            SaveViewData();
        }

        private void NotifyOfSelectionChange()
        {
            if (!HasValidDataAndBindings())
                return;

            onSelectionChange?.Invoke(m_SelectedItems);
#pragma warning disable 618
            onSelectionChanged?.Invoke(m_SelectedItems);
#pragma warning restore 618
        }

        /// <summary>
        /// Deselects any selected items.
        /// </summary>
        public void ClearSelection()
        {
            if (!HasValidDataAndBindings() || m_SelectedIds.Count == 0)
                return;

            ClearSelectionWithoutValidation();
            NotifyOfSelectionChange();
        }

        private void ClearSelectionWithoutValidation()
        {
            foreach (var recycledItem in m_Pool)
                recycledItem.SetSelected(false);
            m_SelectedIds.Clear();
            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();
        }

        /// <summary>
        /// Scrolls to a specific VisualElement.
        /// </summary>
        /// <param name="visualElement">The element to scroll to.</param>
        public void ScrollTo(VisualElement visualElement)
        {
            m_ScrollView.ScrollTo(visualElement);
        }

        internal void SetDragAndDropController(IListViewDragAndDropController dragAndDropController)
        {
            if (m_Dragger == null)
                m_Dragger = new ListViewDragger(this);

            m_Dragger.dragAndDropController = dragAndDropController;
        }

        //Used for unit testing
        internal IListViewDragAndDropController GetDragAndDropController()
        {
            return m_Dragger?.dragAndDropController;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();
            OverwriteFromViewData(this, key);
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // We always need to know when pointer up event occurred to reset DragEventsProcessor flags.
            // Some controls may capture the mouse, but the ListView is a composite root (isCompositeRoot),
            // and will always receive ExecuteDefaultAction despite what the actual event target is.
            if (evt.eventTypeId == PointerUpEvent.TypeId())
            {
                m_Dragger?.OnPointerUp();
            }
            // We need to store the focused item in order to be able to scroll out and back to it, without
            // seeing the focus affected. To do so, we store the path to the tree element that is focused,
            // and set it back in Setup().
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                m_LastFocusedElementTreeChildIndexes.Clear();

                if (m_ScrollView.contentContainer.FindElementInTree(evt.leafTarget as VisualElement, m_LastFocusedElementTreeChildIndexes))
                {
                    var recycledElement = m_ScrollView.contentContainer[m_LastFocusedElementTreeChildIndexes[0]];
                    foreach (var recycledItem in m_Pool)
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
        }

        private void OnScroll(float offset)
        {
            if (!HasValidDataAndBindings())
                return;

            m_ScrollOffset = offset;
            var pixelAlignedItemHeight = resolvedItemHeight;
            int fistVisibleItem = (int)(offset / pixelAlignedItemHeight);

            m_ScrollView.contentContainer.style.paddingTop = fistVisibleItem * pixelAlignedItemHeight;
            m_ScrollView.contentContainer.style.height = itemsSource.Count * pixelAlignedItemHeight;

            if (fistVisibleItem != m_FirstVisibleIndex)
            {
                m_FirstVisibleIndex = fistVisibleItem;

                if (m_Pool.Count > 0)
                {
                    // we try to avoid rebinding a few items
                    if (m_FirstVisibleIndex < m_Pool[0].index) //we're scrolling up
                    {
                        //How many do we have to swap back
                        int count = m_Pool[0].index - m_FirstVisibleIndex;

                        var inserting = m_ScrollInsertionList;

                        for (int i = 0; i < count && m_Pool.Count > 0; ++i)
                        {
                            var last = m_Pool[m_Pool.Count - 1];
                            inserting.Add(last);
                            m_Pool.RemoveAt(m_Pool.Count - 1); //we remove from the end

                            last.element.SendToBack(); //We send the element to the top of the list (back in z-order)
                        }

                        m_ScrollInsertionList = m_Pool;
                        m_Pool = inserting;
                        m_Pool.AddRange(m_ScrollInsertionList);
                        m_ScrollInsertionList.Clear();
                    }
                    else //down
                    {
                        if (m_FirstVisibleIndex < m_Pool[m_Pool.Count - 1].index)
                        {
                            var inserting = m_ScrollInsertionList;

                            int checkIndex = 0;
                            while (m_FirstVisibleIndex > m_Pool[checkIndex].index)
                            {
                                var first = m_Pool[checkIndex];
                                inserting.Add(first);
                                checkIndex++;

                                first.element.BringToFront();  //We send the element to the bottom of the list (front in z-order)
                            }

                            m_Pool.RemoveRange(0, checkIndex); //we remove them all at once
                            m_Pool.AddRange(inserting); // add them back to the end
                            inserting.Clear();
                        }
                    }

                    //Let's rebind everything
                    for (var i = 0; i < m_Pool.Count; i++)
                    {
                        int index = i + m_FirstVisibleIndex;

                        if (index < itemsSource.Count)
                            Setup(m_Pool[i], index);
                        else
                            m_Pool[i].element.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private bool HasValidDataAndBindings()
        {
            return itemsSource != null && makeItem != null && bindItem != null;
        }

        /// <summary>
        /// Clears the ListView, recreates all visible visual elements, and rebinds all items.
        /// </summary>
        /// <remarks>
        /// Call this method whenever the data source changes.
        /// </remarks>
        public void Refresh()
        {
            foreach (var recycledItem in m_Pool)
                recycledItem.DetachElement();

            m_Pool.Clear();
            m_ScrollView.Clear();
            m_VisibleItemCount = 0;

            m_SelectedIndices.Clear();
            m_SelectedItems.Clear();

            // O(n)
            if (m_SelectedIds.Count > 0)
            {
                // Add selected objects to working lists.
                for (var index = 0; index < m_ItemsSource.Count; ++index)
                {
                    if (!m_SelectedIds.Contains(GetIdFromIndex(index))) continue;

                    m_SelectedIndices.Add(index);
                    m_SelectedItems.Add(m_ItemsSource[index]);
                }
            }

            if (!HasValidDataAndBindings())
                return;

            m_LastHeight = m_ScrollView.layout.height;

            if (float.IsNaN(m_LastHeight))
                return;

            m_FirstVisibleIndex = (int)(m_ScrollOffset / resolvedItemHeight);
            ResizeHeight(m_LastHeight);
        }

        private void ResizeHeight(float height)
        {
            var pixelAlignedItemHeight = resolvedItemHeight;
            var contentHeight = itemsSource.Count * pixelAlignedItemHeight;
            m_ScrollView.contentContainer.style.height = contentHeight;

            // Restore scroll offset and preemptively update the highValue
            // in case this is the initial restore from persistent data and
            // the ScrollView's OnGeometryChanged() didn't update the low
            // and highValues.
            var scrollableHeight = Mathf.Max(0, contentHeight - m_ScrollView.contentViewport.layout.height);
            m_ScrollView.verticalScroller.highValue = Mathf.Min(Mathf.Max(m_ScrollOffset, m_ScrollView.verticalScroller.highValue), scrollableHeight);
            m_ScrollView.verticalScroller.value = Mathf.Min(m_ScrollOffset, m_ScrollView.verticalScroller.highValue);

            int itemCount = Math.Min((int)(height / pixelAlignedItemHeight) + k_ExtraVisibleItems, itemsSource.Count);

            if (m_VisibleItemCount != itemCount)
            {
                if (m_VisibleItemCount > itemCount)
                {
                    // Shrink
                    int removeCount = m_VisibleItemCount - itemCount;
                    for (int i = 0; i < removeCount; i++)
                    {
                        int lastIndex = m_Pool.Count - 1;

                        var poolItem = m_Pool[lastIndex];
                        poolItem.element.RemoveFromHierarchy();
                        poolItem.DetachElement();

                        m_Pool.RemoveAt(lastIndex);
                    }
                }
                else
                {
                    // Grow
                    int addCount = itemCount - m_VisibleItemCount;
                    for (int i = 0; i < addCount; i++)
                    {
                        int index = i + m_FirstVisibleIndex + m_VisibleItemCount;
                        var item = makeItem();
                        var recycledItem = new RecycledItem(item);
                        m_Pool.Add(recycledItem);

                        item.AddToClassList("unity-listview-item");
                        item.style.position = Position.Relative;
                        item.style.flexBasis = StyleKeyword.Initial;
                        item.style.marginTop = 0f;
                        item.style.marginBottom = 0f;
                        item.style.flexGrow = 0f;
                        item.style.flexShrink = 0f;
                        item.style.height = pixelAlignedItemHeight;
                        if (index < itemsSource.Count)
                        {
                            Setup(recycledItem, index);
                        }
                        else
                        {
                            item.style.display = DisplayStyle.None;
                        }

                        m_ScrollView.Add(item);
                    }
                }

                m_VisibleItemCount = itemCount;
            }

            m_LastHeight = height;
            UpdateBackground();
        }

        private void Setup(RecycledItem recycledItem, int newIndex)
        {
            var newId = GetIdFromIndex(newIndex);
            recycledItem.element.style.display = DisplayStyle.Flex;
            if (recycledItem.index == newIndex) return;

            m_LastItemIndex = newIndex;
            if (showAlternatingRowBackgrounds != AlternatingRowBackground.None && newIndex % 2 == 1)
                recycledItem.element.AddToClassList(itemAlternativeBackgroundUssClassName);
            else
                recycledItem.element.RemoveFromClassList(itemAlternativeBackgroundUssClassName);

            if (recycledItem.index != RecycledItem.kUndefinedIndex)
                unbindItem?.Invoke(recycledItem.element, recycledItem.index);

            recycledItem.index = newIndex;
            recycledItem.id = newId;
            int indexInParent = newIndex - m_FirstVisibleIndex;
            if (indexInParent == m_ScrollView.contentContainer.childCount)
            {
                recycledItem.element.BringToFront();
            }
            else
            {
                recycledItem.element.PlaceBehind(m_ScrollView.contentContainer[indexInParent]);
            }

            bindItem(recycledItem.element, recycledItem.index);
            recycledItem.SetSelected(m_SelectedIds.Contains(newId));

            // Handle focus cycling
            if (m_LastFocusedElementIndex != -1)
            {
                if (m_LastFocusedElementIndex == newIndex)
                    recycledItem.element.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Focus();
                else
                    recycledItem.element.ElementAtTreePath(m_LastFocusedElementTreeChildIndexes)?.Blur();
            }
        }

        private void UpdateBackground()
        {
            var backgroundFillHeight = m_ScrollView.contentViewport.layout.size.y - m_ScrollView.contentContainer.layout.size.y;
            if (showAlternatingRowBackgrounds != AlternatingRowBackground.All || backgroundFillHeight <= 0)
            {
                m_EmptyRows.RemoveFromHierarchy();
                return;
            }

            if (m_EmptyRows.parent == null)
                m_ScrollView.contentViewport.Add(m_EmptyRows);

            var pixelAlignedItemHeight = resolvedItemHeight;
            var itemsCount = Mathf.FloorToInt(backgroundFillHeight / pixelAlignedItemHeight) + 1;
            if (itemsCount > m_EmptyRows.childCount)
            {
                var itemsToAdd = itemsCount - m_EmptyRows.childCount;
                for (var i = 0; i < itemsToAdd; i++)
                {
                    var row = new VisualElement();
                    //Inline style is used to prevent a user from changing an item flexShrink property.
                    row.style.flexShrink = 0;
                    m_EmptyRows.Add(row);
                }
            }

            var index = m_LastItemIndex;

            int emptyRowCount = m_EmptyRows.hierarchy.childCount;
            for (int i = 0; i < emptyRowCount; ++i)
            {
                var child = m_EmptyRows.hierarchy[i];
                index++;
                child.style.height = pixelAlignedItemHeight;
                child.EnableInClassList(itemAlternativeBackgroundUssClassName, index % 2 == 1);
            }
        }

        private void OnSizeChanged(GeometryChangedEvent evt)
        {
            if (!HasValidDataAndBindings())
                return;

            if (Mathf.Approximately(evt.newRect.height, evt.oldRect.height))
                return;

            ResizeHeight(evt.newRect.height);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int height;
            if (!m_ItemHeightIsInline && e.customStyle.TryGetValue(s_ItemHeightProperty, out height))
            {
                if (m_ItemHeight != height)
                {
                    m_ItemHeight = height;
                    Refresh();
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Refresh();
        }
    }
}
