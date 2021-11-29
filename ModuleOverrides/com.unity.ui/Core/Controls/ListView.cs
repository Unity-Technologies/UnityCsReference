// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A ListView is a vertically scrollable area that links to, and displays, a list of items.
    /// </summary>
    /// <remarks>
    /// A <see cref="ListView"/> is a <see cref="ScrollView"/> with additional logic to display a list of vertically-arranged
    /// VisualElements. Each VisualElement in the list is bound to a corresponding element in a data-source list. The
    /// data-source list can contain elements of any type.
    ///
    /// The logic required to create VisualElements, and to bind them to or unbind them from the data source, varies depending
    /// on the intended result. For the ListView to function correctly, you must supply at least a value for
    /// <see cref="BaseVerticalCollectionView.itemsSource">itemsSource</see>.
    ///
    /// It's also recommended to supply the following properties for more complex items:\\
    /// - <see cref="BaseVerticalCollectionView.makeItem">makeItem</see> \\
    /// - <see cref="BaseVerticalCollectionView.bindItem">bindItem</see> \\
    /// - <see cref="BaseVerticalCollectionView.fixedItemHeight">fixedItemHeight</see> when using <see cref="CollectionVirtualizationMethod.FixedHeight"/>\\
    ///
    /// The <c>ListView</c> creates multiple <see cref="VisualElement"/> objects for the visible items. As the user scrolls, the ListView
    /// recycles these objects and re-binds them to new data items.
    ///
    /// To set the height of a single item in pixels, set the <c>item-height</c> property in UXML or the
    ///     <see cref="ListView.itemHeight"/> property in C# to the desired value. \\
    /// \\
    /// To display a border around the scrollable area, set the <c>show-border</c> property in UXML or the
    ///     <see cref="ListView.showBorder"/> property in C# to <c>true</c>.\\
    ///     \\
    /// By default, the user can select one element in the list at a time. To change the default selection
    /// use the <c>selection-type</c> property in UXML or the<see cref="ListView.selectionType"/> property in C#.
    ///      To allow the user to select more than one element simultaneously, set the property to <c>Selection.Multiple</c>.
    ///     To prevent the user from selecting items, set the property to <c>Selection.None</c>.\\
    /// \\
    ///     By default, all rows in the ListView have same background color. To make the row background colors
    ///     alternate, set the <c>show-alternating-row-backgrounds</c> property in UXML or the
    ///     <see cref="ListView.showAlternatingRowBackgrounds"/> property in C# to
    ///     <see cref="AlternatingRowBackground.ContentOnly"/> or
    ///     <see cref="AlternatingRowBackground.All"/>. For details, see <see cref="AlternatingRowBackground"/>. \\
    /// \\
    ///     By default, the user can't reorder the list's elements. To allow the user to drag the elements
    ///     to reorder them, set the <c>reorderable</c> property in UXML or the <see cref="ListView.reorderable"/>
    ///     property in C# to <c>true</c>.\\
    /// \\
    /// To make the first item in the ListView display the number of items in the list, set the
    ///     <c>show-bound-collection-size</c> property in UXML or the <see cref="ListView.showBoundCollectionSize"/>
    ///     to true. This is useful for debugging. By default, the ListView's scroller element only scrolls vertically.\\
    /// \\
    /// To enable horizontal scrolling when the displayed element is wider than the visible area, set the
    ///     <c>horizontal-scrolling-enabled</c> property in UXML or the <see cref="ListView.horizontalScrollingEnabled"/>
    ///     to <c>true</c>.
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
    public class ListView : BaseListView
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
        public new class UxmlTraits : BaseListView.UxmlTraits {}

        Func<VisualElement> m_MakeItem;

        /// <summary>
        /// Callback for constructing the VisualElement that is the template for each recycled and re-bound element in the list.
        /// </summary>
        /// <remarks>
        /// This callback needs to call a function that constructs a blank <see cref="VisualElement"/> that is
        /// bound to an element from the list.
        ///
        /// The BaseVerticalCollectionView automatically creates enough elements to fill the visible area, and adds more if the area
        /// is expanded. As the user scrolls, the BaseVerticalCollectionView cycles elements in and out as they appear or disappear.
        ///
        /// If this property and <see cref="bindItem"/> are not set, Unity will either create a PropertyField if bound
        /// to a SerializedProperty, or create an empty label for any other case.
        /// </remarks>
        public new Func<VisualElement> makeItem
        {
            get => m_MakeItem;
            set
            {
                m_MakeItem = value;
                Rebuild();
            }
        }

        private Action<VisualElement, int> m_BindItem;

        /// <summary>
        /// Callback for binding a data item to the visual element.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to bind, and the index of the
        /// element to bind it to.
        ///
        /// If this property and <see cref="makeItem"/> are not set, Unity will try to bind to a SerializedProperty if
        /// bound, or simply set text in the created Label.
        /// </remarks>
        public new Action<VisualElement, int> bindItem
        {
            get => m_BindItem;
            set
            {
                m_BindItem = value;
                RefreshItems();
            }
        }

        /// <summary>
        /// Callback for unbinding a data item from the VisualElement.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement to unbind, and the index of the
        /// element to unbind it from.
        /// </remarks>
        public new Action<VisualElement, int> unbindItem { get; set; }

        /// <summary>
        /// Callback invoked when a <see cref="VisualElement"/> created via <see cref="makeItem"/> is no longer needed and will be destroyed.
        /// </summary>
        /// <remarks>
        /// The method called by this callback receives the VisualElement that will be destroyed from the pool.
        /// </remarks>
        public new Action<VisualElement> destroyItem { get; set; }

        internal override bool HasValidDataAndBindings()
        {
            return base.HasValidDataAndBindings() && !(makeItem != null ^ bindItem != null);
        }

        protected override CollectionViewController CreateViewController() => new ListViewController();

        /// <summary>
        /// Creates a <see cref="ListView"/> with all default properties. The <see cref="ListView.itemSource"/>
        /// must all be set for the ListView to function properly.
        /// </summary>
        public ListView()
        {
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Constructs a <see cref="ListView"/>, with all important properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels.</param>
        /// <param name="makeItem">The factory method to call to create a display item. The method should return a
        /// VisualElement that can be bound to a data item.</param>
        /// <param name="bindItem">The method to call to bind a data item to a display item. The method
        /// receives as parameters the display item to bind, and the index of the data item to bind it to.</param>
        public ListView(IList itemsSource, float itemHeight = ItemHeightUnset, Func<VisualElement> makeItem = null, Action<VisualElement, int> bindItem = null)
            : base(itemsSource, itemHeight)
        {
            AddToClassList(ussClassName);

            this.makeItem = makeItem;
            this.bindItem = bindItem;
        }
    }
}
