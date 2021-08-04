// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to change the drag and drop mode for items in the ListView.
    /// </summary>
    /// <remarks>
    /// Using <c>Animated</c> will affect the layout of the ListView, by adding drag handles before every item.
    /// Multiple item drag is only supported in the <c>Simple</c> mode.
    /// </remarks>
    public enum ListViewReorderMode
    {
        /// <summary>
        /// ListView will display the standard blue line dragger on reorder.
        /// </summary>
        Simple,
        /// <summary>
        /// ListView will add drag handles before every item, that can be used to drag a single item with animated visual feedback.
        /// </summary>
        Animated,
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
    ///- <see cref="BaseVerticalCollectionView.fixedItemHeight"/>
    ///
    /// It is also recommended to supply the following for more complex items:
    ///
    ///- <see cref="ListView.makeItem"/>
    ///- <see cref="ListView.bindItem"/>
    ///- <see cref="BaseVerticalCollectionView.fixedItemHeight"/>, in the case of <c>FixedHeight</c> ListView
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
    public class ListView : BaseVerticalCollectionView
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
            private readonly UxmlIntAttributeDescription m_FixedItemHeight = new UxmlIntAttributeDescription { name = "fixed-item-height", obsoleteNames = new[] { "itemHeight, item-height" }, defaultValue = s_DefaultItemHeight };
            private readonly UxmlEnumAttributeDescription<CollectionVirtualizationMethod> m_VirtualizationMethod = new UxmlEnumAttributeDescription<CollectionVirtualizationMethod> { name = "virtualization-method", defaultValue = CollectionVirtualizationMethod.FixedHeight };
            private readonly UxmlBoolAttributeDescription m_ShowBorder = new UxmlBoolAttributeDescription { name = "show-border", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<SelectionType> m_SelectionType = new UxmlEnumAttributeDescription<SelectionType> { name = "selection-type", defaultValue = SelectionType.Single };
            private readonly UxmlEnumAttributeDescription<AlternatingRowBackground> m_ShowAlternatingRowBackgrounds = new UxmlEnumAttributeDescription<AlternatingRowBackground> { name = "show-alternating-row-backgrounds", defaultValue = AlternatingRowBackground.None };
            private readonly UxmlBoolAttributeDescription m_ShowFoldoutHeader = new UxmlBoolAttributeDescription { name = "show-foldout-header", defaultValue = false };
            private readonly UxmlStringAttributeDescription m_HeaderTitle = new UxmlStringAttributeDescription() { name = "header-title", defaultValue = string.Empty };
            private readonly UxmlBoolAttributeDescription m_ShowAddRemoveFooter = new UxmlBoolAttributeDescription { name = "show-add-remove-footer", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_Reorderable = new UxmlBoolAttributeDescription { name = "reorderable", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<ListViewReorderMode> m_ReorderMode = new UxmlEnumAttributeDescription<ListViewReorderMode>() { name = "reorder-mode", defaultValue = ListViewReorderMode.Simple };
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
                if (m_FixedItemHeight.TryGetValueFromBag(bag, cc, ref itemHeight))
                {
                    listView.fixedItemHeight = itemHeight;
                }

                listView.reorderMode = m_ReorderMode.GetValueFromBag(bag, cc);
                listView.virtualizationMethod = m_VirtualizationMethod.GetValueFromBag(bag, cc);
                listView.showBorder = m_ShowBorder.GetValueFromBag(bag, cc);
                listView.selectionType = m_SelectionType.GetValueFromBag(bag, cc);
                listView.showAlternatingRowBackgrounds = m_ShowAlternatingRowBackgrounds.GetValueFromBag(bag, cc);
                listView.showFoldoutHeader = m_ShowFoldoutHeader.GetValueFromBag(bag, cc);
                listView.headerTitle = m_HeaderTitle.GetValueFromBag(bag, cc);
                listView.showAddRemoveFooter = m_ShowAddRemoveFooter.GetValueFromBag(bag, cc);
                listView.showBoundCollectionSize = m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
                listView.horizontalScrollingEnabled = m_HorizontalScrollingEnabled.GetValueFromBag(bag, cc);
            }
        }

        bool m_ShowBoundCollectionSize = true;

        /// <summary>
        /// This property controls whether the list view displays the collection size (number of items).
        /// </summary>
        /// <remarks>
        /// The default values if <c>true</c>.
        /// When this property is set to <c>true</c>, Unity displays the collection size as the first item in the list, but does
        /// not make it an actual list item that is part of the list index. If you query for list index 0,
        /// Unity returns the first real list item, and not the collection size.
        /// If <see cref="showFoldoutHeader"/> is set to <c>true</c>, the collection size field will be included in the header instead.
        /// This property is usually used to debug a ListView, because it indicates whether the data source is
        /// linked correctly. In production, the collection size is rarely displayed as a line item in a ListView.
        /// </remarks>>
        /// <seealso cref="UnityEditor.UIElements.BindingExtensions.Bind"/>
        public bool showBoundCollectionSize
        {
            get => m_ShowBoundCollectionSize;
            set
            {
                if (m_ShowBoundCollectionSize == value)
                    return;

                m_ShowBoundCollectionSize = value;

                SetupArraySizeField();
            }
        }

        internal override bool sourceIncludesArraySize => showBoundCollectionSize && binding != null && !showFoldoutHeader;

        bool m_ShowFoldoutHeader;

        /// <summary>
        /// This property controls whether the list view will display a header, in the form of a foldout that can be expanded or collapsed.
        /// </summary>
        /// <remarks>
        /// The default values if <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a foldout in the hierarchy of the list view and moves
        /// the scroll view inside that newly created foldout. The text of this foldout can be changed with <see cref="headerTitle"/>
        /// property on the ListView.
        /// If <see cref="showBoundCollectionSize"/> is set to <c>true</c>, the header will include a TextField to control
        /// the array size, instead of using the field as part of the list.
        /// </remarks>>
        public bool showFoldoutHeader
        {
            get => m_ShowFoldoutHeader;
            set
            {
                if (m_ShowFoldoutHeader == value)
                    return;

                m_ShowFoldoutHeader = value;

                EnableInClassList(listViewWithHeaderUssClassName, value);

                if (m_ShowFoldoutHeader)
                {
                    if (m_Foldout != null)
                        return;

                    m_Foldout = new Foldout() { name = foldoutHeaderUssClassName, text = m_HeaderTitle };
                    m_Foldout.AddToClassList(foldoutHeaderUssClassName);
                    m_Foldout.tabIndex = 1;
                    hierarchy.Add(m_Foldout);
                    m_Foldout.Add(scrollView);
                }
                else if (m_Foldout != null)
                {
                    m_Foldout?.RemoveFromHierarchy();
                    m_Foldout = null;
                    hierarchy.Add(scrollView);
                }

                SetupArraySizeField();
                UpdateEmpty();

                if (showAddRemoveFooter)
                {
                    EnableFooter(true);
                }
            }
        }

        void SetupArraySizeField()
        {
            if (sourceIncludesArraySize || !showFoldoutHeader || !showBoundCollectionSize)
            {
                m_ArraySizeField?.RemoveFromHierarchy();
                m_ArraySizeField = null;
                return;
            }

            m_ArraySizeField = new TextField() { name = arraySizeFieldUssClassName };
            m_ArraySizeField.AddToClassList(arraySizeFieldUssClassName);
            m_ArraySizeField.RegisterValueChangedCallback(OnArraySizeFieldChanged);
            m_ArraySizeField.isDelayed = true;
            m_ArraySizeField.focusable = true;
            hierarchy.Add(m_ArraySizeField);

            //m_ArraySizeField.tabIndex = 1;
            //m_Foldout.contentContainer.tabIndex = 2;

            UpdateArraySizeField();
        }

        string m_HeaderTitle;

        /// <summary>
        /// This property controls the text of the foldout header when using <see cref="showFoldoutHeader"/>.
        /// </summary>
        public string headerTitle
        {
            get => m_HeaderTitle;
            set
            {
                m_HeaderTitle = value;

                if (m_Foldout != null)
                    m_Foldout.text = m_HeaderTitle;
            }
        }

        /// <summary>
        /// This property controls whether a footer will be added to the list view.
        /// </summary>
        /// <remarks>
        /// The default values if <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a footer under the scroll view.
        /// This footer contains two buttons:
        /// A "+" button. When clicked, adds a single item at the end of the list view.
        /// A "-" button. When clicked, removes all selected items, or the last item if none are selected.
        /// </remarks>
        public bool showAddRemoveFooter
        {
            get => m_Footer != null;
            set => EnableFooter(value);
        }

        void EnableFooter(bool enabled)
        {
            EnableInClassList(listViewWithFooterUssClassName, enabled);
            scrollView.EnableInClassList(scrollViewWithFooterUssClassName, enabled);

            if (enabled)
            {
                if (m_Footer == null)
                {
                    m_Footer = new VisualElement() { name = footerUssClassName };
                    m_Footer.AddToClassList(footerUssClassName);

                    m_RemoveButton = new Button(OnRemoveClicked) { name = footerRemoveButtonName, text = "-" };
                    m_Footer.Add(m_RemoveButton);

                    m_AddButton = new Button(OnAddClicked) { name = footerAddButtonName, text = "+" };
                    m_Footer.Add(m_AddButton);
                }

                if (m_Foldout != null)
                    m_Foldout.contentContainer.Add(m_Footer);
                else
                    hierarchy.Add(m_Footer);
            }
            else
            {
                m_RemoveButton?.RemoveFromHierarchy();
                m_AddButton?.RemoveFromHierarchy();
                m_Footer?.RemoveFromHierarchy();
                m_RemoveButton = null;
                m_AddButton = null;
                m_Footer = null;
            }
        }

        /// <summary>
        /// This event is called for every item added to the itemsSource. Includes the item index.
        /// </summary>
        public event Action<IEnumerable<int>> itemsAdded;

        /// <summary>
        /// This event is called for every item added to the itemsSource. Includes the item index.
        /// </summary>
        public event Action<IEnumerable<int>> itemsRemoved;

        private void AddItems(int itemCount)
        {
            viewController.AddItems(itemCount);
        }

        private void RemoveItems(List<int> indices)
        {
            viewController.RemoveItems(indices);
        }

        void OnArraySizeFieldChanged(ChangeEvent<string> evt)
        {
            if (!int.TryParse(evt.newValue, out var value) || value < 0)
            {
                m_ArraySizeField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            var count = viewController.GetItemCount();
            if (value > count)
            {
                viewController.AddItems(value - count);
            }
            else if (value < count)
            {
                var previousCount = count;
                for (var i = previousCount - 1; i >= value; i--)
                {
                    viewController.RemoveItem(i);
                }
            }
        }

        void UpdateArraySizeField()
        {
            if (!HasValidDataAndBindings())
                return;

            m_ArraySizeField?.SetValueWithoutNotify(viewController.GetItemCount().ToString());
        }

        Label m_EmptyListLabel;

        void UpdateEmpty()
        {
            if (!HasValidDataAndBindings())
                return;

            if (itemsSource.Count == 0 && !sourceIncludesArraySize)
            {
                if (m_EmptyListLabel != null)
                    return;

                m_EmptyListLabel = new Label("List is Empty"); // TODO localize
                m_EmptyListLabel.AddToClassList(emptyLabelUssClassName);
                scrollView.contentViewport.Add(m_EmptyListLabel);
            }
            else
            {
                m_EmptyListLabel?.RemoveFromHierarchy();
                m_EmptyListLabel = null;
            }
        }

        void OnAddClicked()
        {
            AddItems(1);
            if (binding == null)
            {
                SetSelection(itemsSource.Count - 1);
                ScrollToItem(-1);
            }
            else
            {
                schedule.Execute(() =>
                {
                    SetSelection(itemsSource.Count - 1);
                    ScrollToItem(-1);
                }).ExecuteLater(100);
            }
        }

        void OnRemoveClicked()
        {
            if (selectedIndices.Any())
            {
                viewController.RemoveItems(selectedIndices.ToList());
                ClearSelection();
            }
            else if (itemsSource.Count > 0)
            {
                var index = itemsSource.Count - 1;
                viewController.RemoveItem(index);
            }
        }

        // Foldout Header
        Foldout m_Foldout;
        TextField m_ArraySizeField;

        // Add/Remove Buttons Footer
        VisualElement m_Footer;
        Button m_AddButton;
        Button m_RemoveButton;

        // View Controller callbacks
        Action<IEnumerable<int>> m_ItemAddedCallback;
        Action<IEnumerable<int>> m_ItemRemovedCallback;
        Action m_ItemsSourceSizeChangedCallback;

        ListViewController m_ListViewController;
        internal new ListViewController viewController => m_ListViewController;

        private protected override void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableListViewItem>();
        }

        private protected override void CreateViewController()
        {
            SetViewController(new ListViewController());
        }

        internal void SetViewController(ListViewController controller)
        {
            // Lazily init the callbacks because SetViewController is called before the ListView constructor is fully called.
            // *begin-nonstandard-formatting*
            m_ItemAddedCallback ??= OnItemAdded;
            m_ItemRemovedCallback ??= OnItemsRemoved;
            m_ItemsSourceSizeChangedCallback ??= OnItemsSourceSizeChanged;
            // *end-nonstandard-formatting*

            if (m_ListViewController != null)
            {
                m_ListViewController.itemsAdded -= m_ItemAddedCallback;
                m_ListViewController.itemsRemoved -= m_ItemRemovedCallback;
                m_ListViewController.itemsSourceSizeChanged -= m_ItemsSourceSizeChangedCallback;
            }

            base.SetViewController(controller);
            m_ListViewController = controller;

            if (m_ListViewController != null)
            {
                m_ListViewController.itemsAdded += m_ItemAddedCallback;
                m_ListViewController.itemsRemoved += m_ItemRemovedCallback;
                m_ListViewController.itemsSourceSizeChanged += m_ItemsSourceSizeChangedCallback;
            }
        }

        void OnItemAdded(IEnumerable<int> indices)
        {
            itemsAdded?.Invoke(indices);
        }

        void OnItemsRemoved(IEnumerable<int> indices)
        {
            itemsRemoved?.Invoke(indices);
        }

        void OnItemsSourceSizeChanged()
        {
            RefreshItems();
        }

        ListViewReorderMode m_ReorderMode;

        /// <summary>
        /// This property controls the drag and drop mode for the list view.
        /// </summary>
        /// <remarks>
        /// The default values if <c>Simple</c>.
        /// When this property is set to <c>Animated</c>, Unity adds drag handles in front of every item and the drag and
        /// drop manipulation will push items with an animation as the reordering happens.
        /// Multiple item reordering is only supported with the <c>Simple</c> drag mode.
        /// </remarks>
        public ListViewReorderMode reorderMode
        {
            get => m_ReorderMode;
            set
            {
                if (value != m_ReorderMode)
                {
                    m_ReorderMode = value;
                    InitializeDragAndDropController();
                    Rebuild();
                }
            }
        }

        internal override ListViewDragger CreateDragger()
        {
            if (m_ReorderMode == ListViewReorderMode.Simple)
                return new ListViewDragger(this);

            return new ListViewDraggerAnimated(this);
        }

        internal override ICollectionDragAndDropController CreateDragAndDropController() => new ListViewReorderableDragAndDropController(this);

        /// <summary>
        /// The USS class name for ListView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the ListView element. Any styling applied to
        /// this class affects every ListView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-list-view";
        /// <summary>
        /// The USS class name of item elements in ListView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item element the ListView contains. Any styling applied to
        /// this class affects every item element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string itemUssClassName = ussClassName + "__item";
        /// <summary>
        /// The USS class name for label displayed when ListView is empty.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the label displayed if the ListView is empty. Any styling applied to
        /// this class affects every empty label located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string emptyLabelUssClassName = ussClassName + "__empty-label";

        /// <summary>
        /// The USS class name for reorderable animated ListView elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the ListView element when <see cref="reorderMode"/> is set to <c>Animated</c>.
        /// Any styling applied to this class affects every ListView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string reorderableUssClassName = ussClassName + "__reorderable";
        /// <summary>
        /// The USS class name for item elements in reorderable animated ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every element in the ListView when <see cref="reorderMode"/> is set to <c>Animated</c>.
        /// Any styling applied to this class affects every element located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string reorderableItemUssClassName = reorderableUssClassName + "-item";
        /// <summary>
        /// The USS class name for item container in reorderable animated ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every item container in the ListView when <see cref="reorderMode"/> is set to <c>Animated</c>.
        /// Any styling applied to this class affects every item container located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string reorderableItemContainerUssClassName = reorderableItemUssClassName + "__container";
        /// <summary>
        /// The USS class name for drag handle in reorderable animated ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to drag handles in the ListView when <see cref="reorderMode"/> is set to <c>Animated</c>.
        /// Any styling applied to this class affects every drag handle located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string reorderableItemHandleUssClassName = reorderableUssClassName + "-handle";
        /// <summary>
        /// The USS class name for drag handle bar in reorderable animated ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every drag handle bar in the ListView when <see cref="reorderMode"/> is set to <c>Animated</c>.
        /// Any styling applied to this class affects every drag handle bar located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string reorderableItemHandleBarUssClassName = reorderableItemHandleUssClassName + "-bar";
        /// <summary>
        /// The USS class name for the footer of the ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the footer element in the ListView. Any styling applied to this class
        /// affects every ListView located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string footerUssClassName = ussClassName + "__footer";
        /// <summary>
        /// The USS class name for the foldout header of the ListView.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the foldout element in the ListView. Any styling applied to this class
        /// affects every foldout located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string foldoutHeaderUssClassName = ussClassName + "__foldout-header";
        /// <summary>
        /// The USS class name for the size field of the ListView when foldout header is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the size field element in the ListView when <see cref="showFoldoutHeader"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every size field located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string arraySizeFieldUssClassName = ussClassName + "__size-field";
        /// <summary>
        /// The USS class name for ListView when foldout header is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to ListView when <see cref="showFoldoutHeader"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every list located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string listViewWithHeaderUssClassName = ussClassName + "--with-header";
        /// <summary>
        /// The USS class name for ListView when add/remove footer is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to ListView when <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every list located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string listViewWithFooterUssClassName = ussClassName + "--with-footer";
        /// <summary>
        /// The USS class name for scroll view when add/remove footer is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to ListView's scroll view when <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every list located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string scrollViewWithFooterUssClassName = ussClassName + "__scroll-view--with-footer";

        internal static readonly string footerAddButtonName = ussClassName + "__add-button";
        internal static readonly string footerRemoveButtonName = ussClassName + "__remove-button";

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
            : base(itemsSource, itemHeight, makeItem, bindItem)
        {
            AddToClassList(ussClassName);
        }

        private protected override void PostRefresh()
        {
            UpdateArraySizeField();
            UpdateEmpty();
            base.PostRefresh();
        }
    }
}
