// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A tree view with multi column support.
    /// </summary>
    public class MultiColumnTreeView : BaseTreeView
    {
        static readonly BindingId columnsProperty = nameof(columns);
        static readonly BindingId sortColumnDescriptionsProperty = nameof(sortColumnDescriptions);
        static readonly BindingId sortingModeProperty = nameof(sortingMode);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseTreeView.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(sortingEnabled), "sorting-enabled"),
                    new (nameof(sortingMode), "sorting-mode"),
                    new (nameof(columns), "columns"),
                    new (nameof(sortColumnDescriptions), "sort-column-descriptions"),
                });
            }

            #pragma warning disable 649
            [SerializeField, HideInInspector] bool sortingEnabled;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sortingEnabled_UxmlAttributeFlags;
            [SerializeField] ColumnSortingMode sortingMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sortingMode_UxmlAttributeFlags;
            [SerializeReference, UxmlObjectReference] Columns.UxmlSerializedData columns;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags columns_UxmlAttributeFlags;
            [SerializeReference, UxmlObjectReference] SortColumnDescriptions.UxmlSerializedData sortColumnDescriptions;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags sortColumnDescriptions_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new MultiColumnTreeView();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (MultiColumnTreeView)obj;

                if (ShouldWriteAttributeValue(sortingMode_UxmlAttributeFlags))
                    e.sortingMode = sortingMode;
                else if (ShouldWriteAttributeValue(sortingEnabled_UxmlAttributeFlags))
                {
                    #pragma warning disable CS0618 // Type or member is obsolete
                    e.sortingEnabled = sortingEnabled;
                    #pragma warning restore CS0618
                }

                if (ShouldWriteAttributeValue(sortColumnDescriptions_UxmlAttributeFlags) && sortColumnDescriptions != null)
                {
                    var c = new SortColumnDescriptions();
                    sortColumnDescriptions.Deserialize(c);
                    e.sortColumnDescriptions = c;
                }

                if (ShouldWriteAttributeValue(columns_UxmlAttributeFlags) && columns != null)
                {
                    var c = new Columns();
                    columns.Deserialize(c);
                    e.columns = c;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="MultiColumnTreeView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<MultiColumnTreeView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MultiColumnTreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the MultiColumnTreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseTreeView.UxmlTraits
        {
            readonly UxmlEnumAttributeDescription<ColumnSortingMode> m_SortingMode = new() { name = "sorting-mode", obsoleteNames = new[] { "sorting-enabled" } };
            readonly UxmlObjectAttributeDescription<Columns> m_Columns = new ();
            readonly UxmlObjectAttributeDescription<SortColumnDescriptions> m_SortColumnDescriptions = new ();

            /// <summary>
            /// Initializes <see cref="MultiColumnTreeView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var treeView = (MultiColumnTreeView)ve;

                // Convert potential sorting-enabled boolean attribute to a sorting mode enum value.
                if (m_SortingMode.TryGetValueFromBagAsString(bag, cc, out var stringSortingMode))
                {
                    if (bool.TryParse(stringSortingMode, out var boolSortingMode))
                    {
                        treeView.sortingMode = boolSortingMode ? ColumnSortingMode.Custom : ColumnSortingMode.None;
                    }
                    else
                    {
                        treeView.sortingMode = m_SortingMode.GetValueFromBag(bag, cc);
                    }
                }

                treeView.sortColumnDescriptions = m_SortColumnDescriptions.GetValueFromBag(bag, cc);
                treeView.columns = m_Columns.GetValueFromBag(bag, cc);
            }
        }

        Columns m_Columns;
        ColumnSortingMode m_SortingMode;
        SortColumnDescriptions m_SortColumnDescriptions = new SortColumnDescriptions();
        List<SortColumnDescription> m_SortedColumns = new List<SortColumnDescription>();

        /// <summary>
        /// The view controller for this view, cast as a <see cref="MultiColumnTreeViewController"/>.
        /// </summary>
        public new MultiColumnTreeViewController viewController => base.viewController as MultiColumnTreeViewController;

        /// <summary>
        /// If a column is clicked to change sorting, this event is raised to allow users to sort the tree view items.
        /// For a default implementation, set <see cref="sortingMode"/> to <see cref="ColumnSortingMode.Default"/>.
        /// </summary>
        public event Action columnSortingChanged;

        /// <summary>
        /// If a column is right-clicked to show context menu options, this event is raised to allow users to change the available options.
        /// </summary>
        public event Action<ContextualMenuPopulateEvent, Column> headerContextMenuPopulateEvent;

        /// <summary>
        /// Contains information about which columns are currently being sorted.
        /// </summary>
        /// <remarks>Use along with the <see cref="columnSortingChanged"/> event.</remarks>
        public IEnumerable<SortColumnDescription> sortedColumns => m_SortedColumns;

        /// <summary>
        /// The collection of columns for the multi-column header.
        /// </summary>
        [CreateProperty]
        public Columns columns
        {
            get => m_Columns;
            private set
            {
                if (m_Columns != null)
                    m_Columns.propertyChanged -= ColumnsChanged;

                if (value == null)
                {
                    m_Columns.Clear();
                    return;
                }

                m_Columns = value;
                m_Columns.propertyChanged += ColumnsChanged;

                if (m_Columns.Count > 0)
                {
                    GetOrCreateViewController();
                }

                NotifyPropertyChanged(columnsProperty);
            }
        }

        /// <summary>
        /// The collection of sorted columns by default.
        /// </summary>
        [CreateProperty]
        public SortColumnDescriptions sortColumnDescriptions
        {
            get => m_SortColumnDescriptions;
            private set
            {
                if (value == null)
                {
                    m_SortColumnDescriptions.Clear();
                    return;
                }

                m_SortColumnDescriptions = value;

                if (viewController != null)
                {
                    viewController.columnController.header.sortDescriptions = value;
                    RaiseColumnSortingChanged();
                }

                NotifyPropertyChanged(sortColumnDescriptionsProperty);
            }
        }

        /// <summary>
        /// Whether or not sorting is enabled in the multi-column header. This is deprecated. Use <see cref="sortingMode"/> instead.
        /// </summary>
        [Obsolete("sortingEnabled has been deprecated. Use sortingMode instead.", false)]
        public bool sortingEnabled
        {
            get => sortingMode == ColumnSortingMode.Custom;
            set => sortingMode = value ? ColumnSortingMode.Custom : ColumnSortingMode.None;
        }

        /// <summary>
        /// Indicates how to sort columns. To enable sorting, set it to <see cref="ColumnSortingMode.Default"/> or <see cref="ColumnSortingMode.Custom"/>.
        /// The <c>Default</c> mode uses the sorting algorithm provided by <see cref="MultiColumnController"/>, acting on indices. You can also implement your
        /// own sorting with the <c>Custom</c> mode, by responding to the <see cref="columnSortingChanged"/> event.
        /// </summary>
        /// <remarks>
        /// __Note__: If there is at least one sorted column, reordering is temporarily disabled.
        /// </remarks>
        [CreateProperty]
        public ColumnSortingMode sortingMode
        {
            get => m_SortingMode;
            set
            {
                if (sortingMode == value)
                    return;

                m_SortingMode = value;
                if (viewController != null)
                {
                    viewController.columnController.sortingMode = value;
                }

                NotifyPropertyChanged(sortingModeProperty);
            }
        }

        /// <summary>
        /// Initializes a MultiColumnTreeView with an empty header.
        /// </summary>
        public MultiColumnTreeView()
            : this(new Columns()) {}

        /// <summary>
        /// Initializes a MultiColumnTreeView with a list of column definitions.
        /// </summary>
        /// <param name="columns">Column definitions used to initialize the header.</param>
        public MultiColumnTreeView(Columns columns)
        {
            // Keep the old key to support upgrading projects (UUM-62717)
            scrollView.viewDataKey = "unity-multi-column-scroll-view";
            this.columns = columns ?? new Columns();
        }

        internal override void SetRootItemsInternal<T>(IList<TreeViewItemData<T>> rootItems)
        {
            TreeViewHelpers<T, DefaultMultiColumnTreeViewController<T>>.SetRootItems(this, rootItems,
                () => new DefaultMultiColumnTreeViewController<T>(columns, m_SortColumnDescriptions, m_SortedColumns));
        }

        private protected override IEnumerable<TreeViewItemData<T>> GetSelectedItemsInternal<T>()
        {
            return TreeViewHelpers<T, DefaultMultiColumnTreeViewController<T>>.GetSelectedItems(this);
        }

        private protected override T GetItemDataForIndexInternal<T>(int index)
        {
            return TreeViewHelpers<T, DefaultMultiColumnTreeViewController<T>>.GetItemDataForIndex(this, index);
        }

        private protected override T GetItemDataForIdInternal<T>(int id)
        {
            return TreeViewHelpers<T, DefaultMultiColumnTreeViewController<T>>.GetItemDataForId(this, id);
        }

        private protected override void AddItemInternal<T>(TreeViewItemData<T> item, int parentId, int childIndex, bool rebuildTree)
        {
            TreeViewHelpers<T, DefaultMultiColumnTreeViewController<T>>.AddItem(this, item, parentId, childIndex, rebuildTree);
        }

        protected override CollectionViewController CreateViewController() => new DefaultMultiColumnTreeViewController<object>(columns, sortColumnDescriptions, m_SortedColumns);

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        /// <remarks>The controller should implement <see cref="MultiColumnTreeViewController"/>.</remarks>
        public override void SetViewController(CollectionViewController controller)
        {
            if (viewController != null)
            {
                viewController.columnController.columnSortingChanged -= RaiseColumnSortingChanged;
                viewController.columnController.headerContextMenuPopulateEvent -= RaiseHeaderContextMenuPopulate;
            }

            base.SetViewController(controller);

            if (viewController != null)
            {
                viewController.columnController.sortingMode = m_SortingMode;
                viewController.columnController.columnSortingChanged += RaiseColumnSortingChanged;
                viewController.columnController.headerContextMenuPopulateEvent += RaiseHeaderContextMenuPopulate;
            }
        }

        private protected override void CreateVirtualizationController()
        {
            CreateVirtualizationController<ReusableMultiColumnTreeViewItem>();
        }

        void RaiseColumnSortingChanged()
        {
            columnSortingChanged?.Invoke();
        }

        void ColumnsChanged(object sender, BindablePropertyChangedEventArgs args)
        {
            // Forward the event
            NotifyPropertyChanged(args.propertyName);
        }

        void RaiseHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column column)
        {
            headerContextMenuPopulateEvent?.Invoke(evt, column);
        }
    }
}
