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
    /// Using @@Animated@@ will affect the layout of the ListView, by adding drag handles before every item.
    /// Multiple item drag is only supported in the @@Simple@@ mode.
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
    /// Base class for a list view, a vertically scrollable area that links to, and displays, a list of items.
    /// </summary>
    public abstract class BaseListView : BaseVerticalCollectionView
    {
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the list view element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BaseVerticalCollectionView.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription m_ShowFoldoutHeader = new UxmlBoolAttributeDescription { name = "show-foldout-header", defaultValue = false };
            private readonly UxmlStringAttributeDescription m_HeaderTitle = new UxmlStringAttributeDescription() { name = "header-title", defaultValue = string.Empty };
            private readonly UxmlBoolAttributeDescription m_ShowAddRemoveFooter = new UxmlBoolAttributeDescription { name = "show-add-remove-footer", defaultValue = false };
            private readonly UxmlEnumAttributeDescription<ListViewReorderMode> m_ReorderMode = new UxmlEnumAttributeDescription<ListViewReorderMode>() { name = "reorder-mode", defaultValue = ListViewReorderMode.Simple };
            private readonly UxmlBoolAttributeDescription m_ShowBoundCollectionSize = new UxmlBoolAttributeDescription { name = "show-bound-collection-size", defaultValue = true };

            /// <summary>
            /// Returns an empty enumerable, because list views usually do not have child elements.
            /// </summary>
            /// <returns>An empty enumerable.</returns>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            /// <summary>
            /// Initializes <see cref="BaseListView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var view = (BaseListView)ve;
                view.reorderMode = m_ReorderMode.GetValueFromBag(bag, cc);
                view.showFoldoutHeader = m_ShowFoldoutHeader.GetValueFromBag(bag, cc);
                view.headerTitle = m_HeaderTitle.GetValueFromBag(bag, cc);
                view.showAddRemoveFooter = m_ShowAddRemoveFooter.GetValueFromBag(bag, cc);
                view.showBoundCollectionSize = m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
            }
        }

        bool m_ShowBoundCollectionSize = true;

        /// <summary>
        /// This property controls whether the list view displays the collection size (number of items).
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// When this property is set to <c>true</c>, Unity displays the collection size as the first item in the list, but does
        /// not make it an actual list item that is part of the list index. If you query for list index 0,
        /// Unity returns the first real list item, and not the collection size.
        /// If <see cref="showFoldoutHeader"/> is set to <c>true</c>, the collection size field is included in the header instead.
        /// You can use this property to debug a ListView because the property indicates whether the data source is
        /// linked correctly. In production, the collection size rarely displays as a line item in a ListView.
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
        /// This property controls whether the list view displays a header, in the form of a foldout that can be expanded or collapsed.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a foldout in the hierarchy of the list view and moves
        /// the scroll view inside that newly created foldout. You can change the text of this foldout with <see cref="headerTitle"/>
        /// property on the ListView.
        /// If <see cref="showBoundCollectionSize"/> is set to <c>true</c>, the header includes a TextField to control
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

                    m_Foldout = new Foldout() {name = foldoutHeaderUssClassName, text = m_HeaderTitle};

                    var foldoutToggle = m_Foldout.Q<Toggle>(className: Foldout.toggleUssClassName);
                    foldoutToggle.m_Clickable.acceptClicksIfDisabled = true;

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
                UpdateListViewLabel();

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
        /// The default value is <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a footer under the scroll view.
        /// This footer contains two buttons:
        /// A "+" button. When clicked, it adds a single item at the end of the list view.
        /// A "-" button. When clicked, it removes all selected items, or the last item if none are selected.
        /// </remarks>
        public bool showAddRemoveFooter
        {
            get => m_Footer != null;
            set => EnableFooter(value);
        }

        internal Foldout headerFoldout => m_Foldout;

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
            if (m_ArraySizeField.showMixedValue && TextInputBaseField<string>.mixedValueString == evt.newValue)
                return;

            if (!int.TryParse(evt.newValue, out var value) || value < 0)
            {
                m_ArraySizeField.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            var count = viewController.GetItemsCount();

            if (count == 0 && value == viewController.GetItemsMinCount())
                return;

            if (value > count)
            {
                viewController.AddItems(value - count);
            }
            else if (value < count)
            {
                viewController.RemoveItems(count - value);
            }
            else if (value == 0)
            {
                // Special case: list view cannot show arrays with more than n elements (see m_IsOverMultiEditLimit)
                // when multiple objects are selected (so count is already 0 even though array size field shows
                // a different value) and user wants to reset the number of items for that selection
                viewController.ClearItems();
                m_IsOverMultiEditLimit = false;
            }

            UpdateListViewLabel();
        }

        internal void UpdateArraySizeField()
        {
            if (!HasValidDataAndBindings() || m_ArraySizeField == null)
                return;
            if (!m_ArraySizeField.showMixedValue)
                m_ArraySizeField.SetValueWithoutNotify(viewController.GetItemsMinCount().ToString());
            footer?.SetEnabled(!m_IsOverMultiEditLimit);
        }
        Label m_ListViewLabel;

        internal void UpdateListViewLabel()
        {
            if (!HasValidDataAndBindings())
                return;

            var noItemsCount = itemsSource.Count == 0 && !sourceIncludesArraySize;

            if (m_IsOverMultiEditLimit)
            {
                m_ListViewLabel ??= new Label();
                m_ListViewLabel.text = m_MaxMultiEditStr;
                scrollView.contentViewport.Add(m_ListViewLabel);
            }
            else if (noItemsCount)
            {
                m_ListViewLabel ??= new Label();
                m_ListViewLabel.text = k_EmptyListStr;
                scrollView.contentViewport.Add(m_ListViewLabel);
            }
            else
            {
                m_ListViewLabel?.RemoveFromHierarchy();
                m_ListViewLabel = null;
            }

            m_ListViewLabel?.EnableInClassList(emptyLabelUssClassName, noItemsCount);
            m_ListViewLabel?.EnableInClassList(overMaxMultiEditLimitClassName, m_IsOverMultiEditLimit);
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

            if (HasValidDataAndBindings() && m_ArraySizeField != null)
                m_ArraySizeField.showMixedValue = false;
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

            if (HasValidDataAndBindings() && m_ArraySizeField != null)
                m_ArraySizeField.showMixedValue = false;
        }

        // Foldout Header
        Foldout m_Foldout;
        TextField m_ArraySizeField;
        internal TextField arraySizeField => m_ArraySizeField;
        bool m_IsOverMultiEditLimit;
        int m_MaxMultiEditCount;
        internal void SetOverMaxMultiEditLimit(bool isOverLimit, int maxMultiEditCount)
        {
            m_IsOverMultiEditLimit = isOverLimit;
            m_MaxMultiEditCount = maxMultiEditCount;
            m_MaxMultiEditStr = $"This field cannot display arrays with more than {m_MaxMultiEditCount} elements when multiple objects are selected.";
        }

        // Add/Remove Buttons Footer
        VisualElement m_Footer;
        Button m_AddButton;
        Button m_RemoveButton;

        internal VisualElement footer => m_Footer;

        // View Controller callbacks
        Action<IEnumerable<int>> m_ItemAddedCallback;
        Action<IEnumerable<int>> m_ItemRemovedCallback;
        Action m_ItemsSourceSizeChangedCallback;

        /// <summary>
        /// The view controller for this view, cast as a <see cref="BaseListViewController"/>.
        /// </summary>
        public new BaseListViewController viewController => base.viewController as BaseListViewController;

        private protected override void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableListViewItem>();
        }

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        /// <remarks>The controller should implement <see cref="BaseListViewController"/>.</remarks>
        public override void SetViewController(CollectionViewController controller)
        {
            // Lazily init the callbacks because SetViewController is called before the ListView constructor is fully called.
            m_ItemAddedCallback ??= OnItemAdded;
            m_ItemRemovedCallback ??= OnItemsRemoved;
            m_ItemsSourceSizeChangedCallback ??= OnItemsSourceSizeChanged;

            if (viewController != null)
            {
                viewController.itemsAdded -= m_ItemAddedCallback;
                viewController.itemsRemoved -= m_ItemRemovedCallback;
                viewController.itemsSourceSizeChanged -= m_ItemsSourceSizeChangedCallback;
            }

            base.SetViewController(controller);

            if (viewController != null)
            {
                viewController.itemsAdded += m_ItemAddedCallback;
                viewController.itemsRemoved += m_ItemRemovedCallback;
                viewController.itemsSourceSizeChanged += m_ItemsSourceSizeChangedCallback;
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
            // When bound, the ListViewBinding class takes care of refreshing when the array size is updated.
            if (GetProperty(internalBindingKey) == null)
            {
                RefreshItems();
            }
        }

        ListViewReorderMode m_ReorderMode;
        internal event Action reorderModeChanged;

        /// <summary>
        /// This property controls the drag and drop mode for the list view.
        /// </summary>
        /// <remarks>
        /// The default value is <c>Simple</c>.
        /// When this property is set to <c>Animated</c>, Unity adds drag handles in front of every item and the drag and
        /// drop manipulation pushes items with an animation when the reordering happens.
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
                    InitializeDragAndDropController(reorderable);
                    reorderModeChanged?.Invoke();
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
        /// /// The USS class name for label displayed when ListView is trying to edit too many items.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the label displayed if the ListView is trying to edit too many items at once.
        /// </remarks>
        public static readonly string overMaxMultiEditLimitClassName = ussClassName + "__over-max-multi-edit-limit-label";

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

        string m_MaxMultiEditStr;
        static readonly string k_EmptyListStr = "List is empty";

        /// <summary>
        /// Creates a <see cref="BaseListView"/> with all default properties. The <see cref="BaseVerticalCollectionView.itemsSource"/>
        /// must all be set for the BaseListView to function properly.
        /// </summary>
        public BaseListView()
        {
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Constructs a <see cref="BaseListView"/>, with all important properties provided.
        /// </summary>
        /// <param name="itemsSource">The list of items to use as a data source.</param>
        /// <param name="itemHeight">The height of each item, in pixels.</param>
        public BaseListView(IList itemsSource, float itemHeight = ItemHeightUnset)
            : base(itemsSource, itemHeight)
        {
            AddToClassList(ussClassName);
        }

        private protected override void PostRefresh()
        {
            UpdateArraySizeField();
            UpdateListViewLabel();
            base.PostRefresh();
        }
    }
}
