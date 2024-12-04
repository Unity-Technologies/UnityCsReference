// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options to change the drag-and-drop mode for items in the ListView.
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
        private static readonly string k_SizeFieldLabel = "Size";

        internal static readonly BindingId showBoundCollectionSizeProperty = nameof(showBoundCollectionSize);
        internal static readonly BindingId showFoldoutHeaderProperty = nameof(showFoldoutHeader);
        internal static readonly BindingId headerTitleProperty = nameof(headerTitle);
        internal static readonly BindingId makeHeaderProperty = nameof(makeHeader);
        internal static readonly BindingId makeFooterProperty = nameof(makeFooter);
        internal static readonly BindingId showAddRemoveFooterProperty = nameof(showAddRemoveFooter);
        internal static readonly BindingId bindingSourceSelectionModeProperty = nameof(bindingSourceSelectionMode);
        internal static readonly BindingId reorderModeProperty = nameof(reorderMode);
        internal static readonly BindingId makeNoneElementProperty = nameof(makeNoneElement);
        internal static readonly BindingId allowAddProperty = nameof(allowAdd);
        internal static readonly BindingId overridingAddButtonBehaviorProperty = nameof(overridingAddButtonBehavior);
        internal static readonly BindingId onAddProperty = nameof(onAdd);
        internal static readonly BindingId allowRemoveProperty = nameof(allowRemove);
        internal static readonly BindingId onRemoveProperty = nameof(onRemove);

        [ExcludeFromDocs, Serializable]
        public new abstract class UxmlSerializedData : BaseVerticalCollectionView.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(showFoldoutHeader), "show-foldout-header"),
                    new (nameof(headerTitle), "header-title"),
                    new (nameof(showAddRemoveFooter), "show-add-remove-footer"),
                    new (nameof(allowAdd), "allow-add"),
                    new (nameof(allowRemove), "allow-remove"),
                    new (nameof(reorderMode), "reorder-mode"),
                    new (nameof(showBoundCollectionSize), "show-bound-collection-size"),
                    new (nameof(bindingSourceSelectionMode), "binding-source-selection-mode"),
                });
            }

            #pragma warning disable 649
            [SerializeField] bool showFoldoutHeader;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showFoldoutHeader_UxmlAttributeFlags;
            [SerializeField] string headerTitle;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags headerTitle_UxmlAttributeFlags;
            [SerializeField] bool showAddRemoveFooter;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showAddRemoveFooter_UxmlAttributeFlags;
            [SerializeField] bool allowAdd;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags allowAdd_UxmlAttributeFlags;
            [SerializeField] bool allowRemove;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags allowRemove_UxmlAttributeFlags;
            [SerializeField] ListViewReorderMode reorderMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags reorderMode_UxmlAttributeFlags;
            [SerializeField] bool showBoundCollectionSize;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags showBoundCollectionSize_UxmlAttributeFlags;
            [SerializeField] BindingSourceSelectionMode bindingSourceSelectionMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingSourceSelectionMode_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (BaseListView)obj;
                if (ShouldWriteAttributeValue(showFoldoutHeader_UxmlAttributeFlags))
                    e.showFoldoutHeader = showFoldoutHeader;
                if (ShouldWriteAttributeValue(headerTitle_UxmlAttributeFlags))
                    e.headerTitle = headerTitle;
                if (ShouldWriteAttributeValue(showAddRemoveFooter_UxmlAttributeFlags))
                    e.showAddRemoveFooter = showAddRemoveFooter;
                if (ShouldWriteAttributeValue(allowAdd_UxmlAttributeFlags))
                    e.allowAdd = allowAdd;
                if (ShouldWriteAttributeValue(allowRemove_UxmlAttributeFlags))
                    e.allowRemove = allowRemove;
                if (ShouldWriteAttributeValue(reorderMode_UxmlAttributeFlags))
                    e.reorderMode = reorderMode;
                if (ShouldWriteAttributeValue(showBoundCollectionSize_UxmlAttributeFlags))
                    e.showBoundCollectionSize = showBoundCollectionSize;
                if (ShouldWriteAttributeValue(bindingSourceSelectionMode_UxmlAttributeFlags))
                    e.bindingSourceSelectionMode = bindingSourceSelectionMode;
            }
        }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BaseListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the list view element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseVerticalCollectionView.UxmlTraits
        {
            private readonly UxmlBoolAttributeDescription m_ShowFoldoutHeader = new UxmlBoolAttributeDescription { name = "show-foldout-header", defaultValue = false };
            private readonly UxmlStringAttributeDescription m_HeaderTitle = new UxmlStringAttributeDescription() { name = "header-title", defaultValue = string.Empty };
            private readonly UxmlBoolAttributeDescription m_ShowAddRemoveFooter = new UxmlBoolAttributeDescription { name = "show-add-remove-footer", defaultValue = false };
            private readonly UxmlBoolAttributeDescription m_AllowAdd = new UxmlBoolAttributeDescription{ name = "allow-add", defaultValue = true };
            private readonly UxmlBoolAttributeDescription m_AllowRemove = new UxmlBoolAttributeDescription{ name = "allow-remove", defaultValue = true };
            private readonly UxmlEnumAttributeDescription<ListViewReorderMode> m_ReorderMode = new UxmlEnumAttributeDescription<ListViewReorderMode>() { name = "reorder-mode", defaultValue = ListViewReorderMode.Simple };
            private readonly UxmlBoolAttributeDescription m_ShowBoundCollectionSize = new UxmlBoolAttributeDescription { name = "show-bound-collection-size", defaultValue = true };
            private readonly UxmlEnumAttributeDescription<BindingSourceSelectionMode> m_BindingSourceSelectionMode = new () { name = "binding-source-selection-mode", defaultValue = BindingSourceSelectionMode.Manual };

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
                view.allowAdd = m_AllowAdd.GetValueFromBag(bag, cc);
                view.allowRemove = m_AllowRemove.GetValueFromBag(bag, cc);
                view.showBoundCollectionSize = m_ShowBoundCollectionSize.GetValueFromBag(bag, cc);
                view.bindingSourceSelectionMode = m_BindingSourceSelectionMode.GetValueFromBag(bag, cc);
            }

            protected UxmlTraits()
            {
                // Ignore by default, because the ListView content can have empty space when using footer for example.
                // PointerEvents are registered on the ScrollView, which is pickingMode = PickingMode.Position.
                m_PickingMode.defaultValue = PickingMode.Ignore;
            }
        }

        // Custom index to ensure the fields are selected in the order they are displayed in the inspector. (UUM-32041)
        const int k_FoldoutTabIndex = 10;
        const int k_ArraySizeFieldTabIndex = 20;

        bool m_ShowBoundCollectionSize = true;

        /// <summary>
        /// This property controls whether the list view displays the collection size (number of items).
        /// </summary>
        /// <remarks>
        /// The default value is <c>true</c>.
        /// When this property is set to to <c>true</c>, the ListView includes a TextField to control the array size.
        /// </remarks>
        /// <seealso cref="UnityEditor.UIElements.BindingExtensions.Bind"/>
        [CreateProperty]
        public bool showBoundCollectionSize
        {
            get => m_ShowBoundCollectionSize;
            set
            {
                if (m_ShowBoundCollectionSize == value)
                    return;

                m_ShowBoundCollectionSize = value;

                SetupArraySizeField();
                NotifyPropertyChanged(showBoundCollectionSizeProperty);
            }
        }

        bool m_ShowFoldoutHeader;

        /// <summary>
        /// This property controls whether the list view displays a header, in the form of a foldout that can be expanded or collapsed.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a foldout in the hierarchy of the list view and moves
        /// the scroll view inside that newly created foldout. You can change the text of this foldout with <see cref="headerTitle"/>
        /// property on the ListView.
        /// If <see cref="showBoundCollectionSize"/> is set to <c>true</c>, the ListView includes a TextField to control
        /// the array size.
        /// If the <see cref="makeHeader"/> callback is set, no Foldout is shown.
        /// </remarks>
        [CreateProperty]
        public bool showFoldoutHeader
        {
            get => m_ShowFoldoutHeader;
            set
            {
                var previous = m_ShowFoldoutHeader;
                m_ShowFoldoutHeader = value;

                try
                {
                    if (makeHeader != null)
                        return;

                    EnableInClassList(listViewWithHeaderUssClassName, value);
                    if (m_ShowFoldoutHeader)
                    {
                        AddFoldout();
                    }
                    else if (m_Foldout != null)
                    {
                        // If present, drawnFooter would be discarded. Avoid recreating it later
                        drawnFooter?.RemoveFromHierarchy();
                        RemoveFoldout();
                    }

                    SetupArraySizeField();
                    UpdateListViewLabel();

                    if (makeFooter == null)
                    {
                        if (showAddRemoveFooter)
                        {
                            EnableFooter(true);
                        }
                    }
                    else
                    {
                        if (m_ShowFoldoutHeader)
                        {
                            drawnFooter?.RemoveFromHierarchy();
                            m_Foldout?.contentContainer.Add(drawnFooter);
                        }
                        else
                        {
                            hierarchy.Add(drawnFooter);
                            hierarchy.BringToFront(drawnFooter);
                        }
                    }
                }
                finally
                {
                    if (previous != m_ShowFoldoutHeader)
                        NotifyPropertyChanged(showFoldoutHeaderProperty);
                }
            }
        }

        void AddFoldout()
        {
            if (m_Foldout != null)
                return;

            m_Foldout = new Foldout() {name = foldoutHeaderUssClassName, text = m_HeaderTitle};
            m_Foldout.toggle.tabIndex = k_FoldoutTabIndex;
            m_Foldout.toggle.acceptClicksIfDisabled = true;

            m_Foldout.AddToClassList(foldoutHeaderUssClassName);
            hierarchy.Add(m_Foldout);
            m_Foldout.Add(scrollView);
        }

        void RemoveFoldout()
        {
            m_Foldout?.RemoveFromHierarchy();
            m_Foldout = null;
            hierarchy.Add(scrollView);
        }

        internal void SetupArraySizeField()
        {
            if (!showBoundCollectionSize ||
                (!showFoldoutHeader && GetProperty(internalBindingKey) == null) ||
                drawnHeader != null)
            {
                m_ArraySizeField?.RemoveFromHierarchy();
                return;
            }

            if (m_ArraySizeField == null)
            {
                m_ArraySizeField = new TextField() { name = arraySizeFieldUssClassName, tabIndex = k_ArraySizeFieldTabIndex };
                m_ArraySizeField.AddToClassList(arraySizeFieldUssClassName);
                m_ArraySizeField.RegisterValueChangedCallback(OnArraySizeFieldChanged);
                m_ArraySizeField.isDelayed = true;
                m_ArraySizeField.focusable = true;
            }

            m_ArraySizeField.EnableInClassList(arraySizeFieldWithFooterUssClassName, showAddRemoveFooter);
            m_ArraySizeField.EnableInClassList(arraySizeFieldWithHeaderUssClassName, showFoldoutHeader);

            if (showFoldoutHeader)
            {
                m_ArraySizeField.label = string.Empty;
                // This is needed since z-index is not supported yet.
                hierarchy.Add(m_ArraySizeField);
            }
            else
            {
                m_ArraySizeField.label = k_SizeFieldLabel;
                hierarchy.Insert(0, m_ArraySizeField);
            }

            UpdateArraySizeField();
        }

        string m_HeaderTitle;

        /// <summary>
        /// This property controls the text of the foldout header when using <see cref="showFoldoutHeader"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="makeHeader"/> callback is set, this property gets overridden and the title is not shown.
        /// </remarks>
        [CreateProperty]
        public string headerTitle
        {
            get => m_HeaderTitle;
            set
            {
                var previous = m_HeaderTitle;

                m_HeaderTitle = value;

                if (m_Foldout != null)
                    m_Foldout.text = m_HeaderTitle;

                if (string.CompareOrdinal(previous, m_HeaderTitle) != 0)
                    NotifyPropertyChanged(headerTitleProperty);
            }
        }

        private VisualElement drawnHeader;
        Func<VisualElement> m_MakeHeader;

        /// <summary>
        /// This callback allows the user to make their own header for this control.
        /// </summary>
        /// <remarks>
        /// Setting this callback will override the <see cref="showFoldoutHeader"/> and the <see cref="headerTitle"/> properties.
        /// </remarks>
        [CreateProperty]
        public Func<VisualElement> makeHeader
        {
            get => m_MakeHeader;
            set
            {
                if (value == m_MakeHeader)
                    return;

                RemoveFoldout();

                m_MakeHeader = value;
                if (m_MakeHeader != null)
                {
                    SetupArraySizeField();
                    drawnHeader = m_MakeHeader.Invoke();
                    drawnHeader.tabIndex = 1;
                    hierarchy.Add(drawnHeader);
                    hierarchy.SendToBack(drawnHeader);
                }
                else
                {
                    drawnHeader?.RemoveFromHierarchy();
                    drawnHeader = null;

                    // Force the foldout header state to be reset
                    if (showFoldoutHeader)
                    {
                        AddFoldout();
                        SetupArraySizeField();
                        UpdateListViewLabel();
                    }
                }

                // Because the presence/absence of foldout, footer (custom or not) might be affected.
                // Ensure it is sent to the bottom of the ListView.
                if (drawnFooter != null)
                {
                    if (m_Foldout != null)
                    {
                        drawnFooter.RemoveFromHierarchy();
                        m_Foldout.contentContainer.hierarchy.Add(drawnFooter);
                    }
                    else
                    {
                        hierarchy.Add(drawnFooter);
                        drawnFooter?.BringToFront();
                    }
                }
                else
                {
                    EnableFooter(showAddRemoveFooter);
                }
                NotifyPropertyChanged(makeHeaderProperty);
            }
        }


        private VisualElement drawnFooter;
        Func<VisualElement> m_MakeFooter;

        /// <summary>
        /// This callback allows the user to make their own footer for this control.
        /// </summary>
        /// <remarks>
        /// Setting this callback will override the <see cref="showAddRemoveFooter"/> property.
        /// </remarks>
        [CreateProperty]
        public Func<VisualElement> makeFooter
        {
            get => m_MakeFooter;
            set
            {
                if (value == m_MakeFooter)
                    return;

                m_MakeFooter = value;
                if (m_MakeFooter != null)
                {
                    m_Footer?.RemoveFromHierarchy();
                    m_Footer = null;

                    drawnFooter = m_MakeFooter.Invoke();
                    if (m_Foldout != null)
                    {
                        m_Foldout.contentContainer.Add(drawnFooter);
                    }
                    else
                    {
                        hierarchy.Add(drawnFooter);
                        hierarchy.BringToFront(drawnFooter);
                    }
                    EnableInClassList(listViewWithFooterUssClassName,true);
                    scrollView.EnableInClassList(scrollViewWithFooterUssClassName, true);
                }
                else
                {
                    drawnFooter?.RemoveFromHierarchy();
                    drawnFooter = null;

                    // Force the footer state to be reset
                    EnableFooter(m_ShowAddRemoveFooter);
                }

                NotifyPropertyChanged(makeFooterProperty);
            }
        }

        private bool m_ShowAddRemoveFooter;
        /// <summary>
        /// This property controls whether a footer will be added to the list view.
        /// </summary>
        /// <remarks>
        /// The default value is <c>false</c>.
        /// When this property is set to <c>true</c>, Unity adds a footer under the scroll view.
        /// This footer contains two buttons:
        /// A "+" button. When clicked, it adds a single item at the end of the list view.
        /// A "-" button. When clicked, it removes all selected items, or the last item if none are selected.
        /// If the <see cref="makeFooter"/> callback is set, it will override this property.
        /// </remarks>
        [CreateProperty]
        public bool showAddRemoveFooter
        {
            get => m_Footer != null;
            set
            {
                var previous = showAddRemoveFooter;
                m_ShowAddRemoveFooter = value;

                if (makeFooter == null)
                    EnableFooter(value);

                if (value && m_ArraySizeField != null)
                    m_ArraySizeField.AddToClassList(arraySizeFieldWithFooterUssClassName);

                if (previous != showFoldoutHeader)
                    NotifyPropertyChanged(showAddRemoveFooterProperty);
            }
        }

        internal Foldout headerFoldout
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => m_Foldout;
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

                    m_AddButton = new Button(OnAddClicked) { name = footerAddButtonName, text = "+" };
                    m_AddButton.SetEnabled(allowAdd);
                    m_Footer.Add(m_AddButton);

                    m_RemoveButton = new Button(OnRemoveClicked) { name = footerRemoveButtonName, text = "-" };
                    m_RemoveButton.SetEnabled(allowRemove);
                    m_Footer.Add(m_RemoveButton);
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
        /// This event is called for every item added to the ::ref::itemsSource. Includes the item index.
        /// </summary>
        /// <remarks>
        /// Note: This event is only called when items are added through the ::ref::viewController, such as when calling ::ref::AddItems.
        /// Adding items directly to the ::ref::itemsSource will not trigger this event.
        /// </remarks>
        public event Action<IEnumerable<int>> itemsAdded;

        /// <summary>
        /// This event is called for every item removed from the ::ref::itemsSource. Includes the item index.
        /// </summary>
        /// <remarks>
        /// **Note**: This event is only called when items are removed through the ::ref::viewController, such as when
        /// calling <see cref="BaseListViewController.RemoveItems"/> or <see cref="BaseListViewController.ClearItems"/>.
        /// </remarks>
        public event Action<IEnumerable<int>> itemsRemoved;

        internal event Action itemsSourceSizeChanged;

        private IVisualElementScheduledItem m_TrackedItem;

        private IVisualElementScheduledItem trackItemCount
        {
            get
            {
                if (null != m_TrackedItem)
                    return m_TrackedItem;

                m_TrackedItem = schedule
                    .Execute(trackCount)
                    .Until(untilManualBindingSourceSelectionMode);
                return m_TrackedItem;
            }
        }

        private Action m_TrackCount;
        private Action trackCount => m_TrackCount ??= () =>
        {
            if (itemsSource?.Count != m_PreviousRefreshedCount)
                RefreshItems();
        };

        private Func<bool> m_WhileAutoAssign;
        private Func<bool> untilManualBindingSourceSelectionMode => m_WhileAutoAssign ??= () => !autoAssignSource;

        BindingSourceSelectionMode m_BindingSourceSelectionMode = BindingSourceSelectionMode.Manual;

        /// <summary>
        /// This property controls whether every element in the list will get its data source setup automatically to the
        /// correct item in the collection's source.
        /// </summary>
        /// <remarks>
        /// When set to <c>AutoAssign</c>, the bind callbacks don't need to be specified, since bindings can be used
        /// to fill the elements.
        /// </remarks>
        [CreateProperty]
        public BindingSourceSelectionMode bindingSourceSelectionMode
        {
            get => m_BindingSourceSelectionMode;
            set
            {
                if (m_BindingSourceSelectionMode == value)
                    return;

                m_BindingSourceSelectionMode = value;
                Rebuild();
                NotifyPropertyChanged(bindingSourceSelectionModeProperty);

                if (autoAssignSource)
                    trackItemCount.Resume();
            }
        }

        internal bool autoAssignSource => bindingSourceSelectionMode == BindingSourceSelectionMode.AutoAssign;

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

            var noItemsCount = itemsSource.Count == 0;

            if (m_IsOverMultiEditLimit)
            {
                m_ListViewLabel ??= new Label();
                m_ListViewLabel.text = m_MaxMultiEditStr;
                scrollView.contentViewport.Add(m_ListViewLabel);
            }
            else if (noItemsCount)
            {
                if (m_MakeNoneElement != null)
                {
                    m_NoneElement ??= m_MakeNoneElement.Invoke();
                    scrollView.contentViewport.Add(m_NoneElement);
                    m_ListViewLabel?.RemoveFromHierarchy();
                    m_ListViewLabel = null;
                }
                else
                {
                    m_ListViewLabel ??= new Label();
                    m_ListViewLabel.text = k_EmptyListStr;
                    scrollView.contentViewport.Add(m_ListViewLabel);
                    m_NoneElement?.RemoveFromHierarchy();
                    m_NoneElement = null;
                }
            }
            else
            {
                m_NoneElement?.RemoveFromHierarchy();
                m_NoneElement = null;

                m_ListViewLabel?.RemoveFromHierarchy();
                m_ListViewLabel = null;
            }

            m_ListViewLabel?.EnableInClassList(emptyLabelUssClassName, noItemsCount);
            m_ListViewLabel?.EnableInClassList(overMaxMultiEditLimitClassName, m_IsOverMultiEditLimit);
        }

        /// <summary>
        /// Adds item to underlying list using callback if available
        /// </summary>
        /// <remarks>
        /// Callback is used in precedence:
        /// 1. `overridingAddButtonBehavior` if present to provide DropDownMenu customisation
        /// 2. falling back to `onAdd` callback
        /// 3. finally default `AddItems(1)` is used
        ///
        /// After a successful insertion `OnItemsSourceSizeChanged()` is called and selection+scroll is updated
        /// </remarks>
        void OnAddClicked()
        {
            var itemsCountPreCallback = itemsSource?.Count ?? 0;

            if (overridingAddButtonBehavior != null)
            {
                overridingAddButtonBehavior(this, m_AddButton);
            }
            else
            if (onAdd != null)
            {
                onAdd.Invoke(this);
            }
            else
            {
                AddItems(1);
            }

            if ( itemsSource != null
                && itemsCountPreCallback != itemsSource.Count)
            {
                OnItemsSourceSizeChanged();

                Action fnSetSelection = () =>
                    {
                        SetSelection(itemsSource.Count - 1);
                        ScrollToItem(-1);
                    };

                if (GetProperty(internalBindingKey) == null)
                {
                    fnSetSelection();
                }
                else
                {
                    schedule.Execute(fnSetSelection).ExecuteLater(100);
                }
            }

            if (HasValidDataAndBindings() && m_ArraySizeField != null)
                m_ArraySizeField.showMixedValue = false;
        }

        void OnRemoveClicked()
        {
            if (onRemove != null)
            {
                onRemove.Invoke(this);
            }
            else if (selectedIndices.Any())
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

            itemsSourceSizeChanged?.Invoke();
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

        private VisualElement m_NoneElement;
        Func<VisualElement> m_MakeNoneElement;

        /// <summary>
        /// This callback allows the user to set a Visual Element to replace the "List is empty" Label shown when the ListView is empty.
        /// </summary>
        /// <remarks>
        /// Setting this callback to anything other than <c>null</c> will remove the "List is empty" Label.
        /// </remarks>
        [CreateProperty]
        public Func<VisualElement> makeNoneElement
        {
            get => m_MakeNoneElement;
            set
            {
                if (value == m_MakeNoneElement)
                    return;

                m_MakeNoneElement = value;
                m_NoneElement?.RemoveFromHierarchy();
                m_NoneElement = null;
                RefreshItems();
                NotifyPropertyChanged(makeNoneElementProperty);
            }
        }

        bool m_AllowAdd = true;

        /// <summary>
        /// This property allows the user to allow or block the addition of an item when clicking on the Add Button.
        /// It must return <c>true</c> or <c>false</c>.
        /// </summary>
        /// <remarks>
        /// If the callback is not set to <c>false</c>, any Add operation will be allowed.
        /// </remarks>
        [CreateProperty]
        public bool allowAdd
        {
            get => m_AllowAdd;
            set
            {
                if (value == m_AllowAdd)
                    return;

                m_AllowAdd = value;
                m_AddButton?.SetEnabled(m_AllowAdd);
                NotifyPropertyChanged(allowAddProperty);
            }
        }

        Action<BaseListView, Button> m_OverridingAddButtonBehavior;

        /// <summary>
        /// This callback allows the user to implement a <see cref="DropdownMenu"/> when the Add Button is clicked.
        /// </summary>
        /// <remarks>
        /// This callback will only be called if <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Setting this callback to anything other than <c>null</c> will override the basic add behavior.
        /// This callback will override the <see cref="onAdd"/> callback.
        /// </remarks>
        [CreateProperty]
        public Action<BaseListView, Button> overridingAddButtonBehavior
        {
            get => m_OverridingAddButtonBehavior;
            set
            {
                if (value == m_OverridingAddButtonBehavior)
                    return;

                m_OverridingAddButtonBehavior = value;
                RefreshItems();
                NotifyPropertyChanged(overridingAddButtonBehaviorProperty);
            }
        }

        Action<BaseListView> m_OnAdd;

        /// <summary>
        /// This callback allows the user to implement their own code to be executed when the Add Button is clicked.
        /// </summary>
        /// <remarks>
        /// This callback will only be called if <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Setting this callback to anything other than <c>null</c> will override the basic add behavior.
        /// This callback will be overriden if the <see cref="overridingAddButtonBehavior"/> callback is set.
        /// </remarks>
        [CreateProperty]
        public Action<BaseListView> onAdd
        {
            get => m_OnAdd;
            set
            {
                if (value == m_OnAdd)
                    return;

                m_OnAdd = value;
                RefreshItems();
                NotifyPropertyChanged(onAddProperty);
            }
        }

        bool m_AllowRemove = true;

        /// <summary>
        /// This property allows the user to allow or block the removal of an item when clicking on the Remove Button.
        /// It must return <c>true</c> or <c>false</c>.
        /// </summary>
        /// /// <remarks>
        /// If the property is not set to <c>false</c>, any Remove operation will be allowed.
        /// </remarks>
        [CreateProperty]
        public bool allowRemove
        {
            get => m_AllowRemove;
            set
            {
                if (value == m_AllowRemove)
                    return;

                m_AllowRemove = value;
                m_RemoveButton?.SetEnabled(allowRemove);
                NotifyPropertyChanged(allowRemoveProperty);
            }
        }

        Action<BaseListView> m_OnRemove;

        /// <summary>
        /// This callback allows the user to implement their own code to be executed when the Remove Button is clicked.
        /// </summary>
        /// <remarks>
        /// This callback will only be called if <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Setting this callback to anything other than <c>null</c> will override the basic remove behavior.
        /// </remarks>
        [CreateProperty]
        public Action<BaseListView> onRemove
        {
            get => m_OnRemove;
            set
            {
                if (value == m_OnRemove)
                    return;

                m_OnRemove = value;
                RefreshItems();
                NotifyPropertyChanged(onRemoveProperty);
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
        /// The USS class name for label displayed when ListView is trying to edit too many items.
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
        /// The USS class name for the size field of the ListView when show bound collection size is enabled
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the size field element in the ListView when <see cref="showBoundCollectionSize"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every size field located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string arraySizeFieldUssClassName = ussClassName + "__size-field";
        /// <summary>
        /// The USS class name for the size field of the ListView when foldout header is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the size field element in the ListView when <see cref="showBoundCollectionSize"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every size field located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string arraySizeFieldWithHeaderUssClassName = arraySizeFieldUssClassName + "--with-header";
        /// <summary>
        /// The USS class name for the size field of the ListView when the footer is enabled.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the size field element in the ListView when <see cref="showBoundCollectionSize"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every size field located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string arraySizeFieldWithFooterUssClassName = arraySizeFieldUssClassName + "--with-footer";
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
        /// Unity adds this USS class  scroll view of <see cref="BaseListView"/>when <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// Any styling applied to this class affects every list located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public static readonly string scrollViewWithFooterUssClassName = ussClassName + "__scroll-view--with-footer";
        /// <summary>
        /// The name of the add button element in the footer.
        /// </summary>
        /// <remarks>
        /// Unity uses this name of <see cref="BaseListView"/> add button when <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// </remarks>
        public static readonly string footerAddButtonName = ussClassName + "__add-button";
        /// <summary>
        /// The name of the remove button element in the footer.
        /// </summary>
        /// <remarks>
        /// Unity uses this name of <see cref="BaseListView"/> remove button when <see cref="showAddRemoveFooter"/> is set to <c>true</c>.
        /// </remarks>
        public static readonly string footerRemoveButtonName = ussClassName + "__remove-button";

        string m_MaxMultiEditStr;
        static readonly string k_EmptyListStr = "List is empty";

        /// <summary>
        /// Creates a <see cref="BaseListView"/> with all default properties. The <see cref="BaseVerticalCollectionView.itemsSource"/>
        /// must all be set for the BaseListView to function properly.
        /// </summary>
        public BaseListView()
            :this(null)
        {
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
            pickingMode = PickingMode.Ignore;
            allowAdd = true;
            allowRemove = true;
        }

        private protected override void PostRefresh()
        {
            UpdateArraySizeField();
            UpdateListViewLabel();
            base.PostRefresh();
        }

        private protected override bool HandleItemNavigation(bool moveIn, bool altPressed)
        {
            var hasChanges = false;

            foreach (var index in selectedIndices)
            {
                foreach (var reusableCollectionItem in activeItems)
                {
                    if (reusableCollectionItem.index == index && GetProperty(internalBindingKey) != null)
                    {
                        var foldout = reusableCollectionItem.bindableElement.Q<Foldout>();
                        if (foldout != null)
                        {
                            foldout.value = moveIn;
                            hasChanges = true;
                        }
                    }
                }
            }

            return hasChanges;
        }
    }
}
