// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A list view with multi column support.
    /// </summary>
    public class MultiColumnListView : BaseListView
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseListView.UxmlSerializedData
        {
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

            public override object CreateInstance() => new MultiColumnListView();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (MultiColumnListView)obj;

                if (ShouldWriteAttributeValue(sortingEnabled_UxmlAttributeFlags))
                {
                    var mode = sortingEnabled ? ColumnSortingMode.Custom : ColumnSortingMode.None;
                    if (ShouldWriteAttributeValue(sortingMode_UxmlAttributeFlags) && sortingMode != ColumnSortingMode.None)
                        mode = sortingMode;
                    e.sortingMode = mode;
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
        /// Instantiates a <see cref="MultiColumnListView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<MultiColumnListView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MultiColumnListView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the MultiColumnListView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BaseListView.UxmlTraits
        {
            readonly UxmlEnumAttributeDescription<ColumnSortingMode> m_SortingMode = new() { name = "sorting-mode", obsoleteNames = new[] { "sorting-enabled" } };
            readonly UxmlObjectAttributeDescription<Columns> m_Columns = new UxmlObjectAttributeDescription<Columns>();
            readonly UxmlObjectAttributeDescription<SortColumnDescriptions> m_SortColumnDescriptions = new UxmlObjectAttributeDescription<SortColumnDescriptions>();

            /// <summary>
            /// Initializes <see cref="MultiColumnListView"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var listView = (MultiColumnListView)ve;

                // Convert potential sorting-enabled boolean attribute to a sorting mode enum value.
                if (m_SortingMode.TryGetValueFromBagAsString(bag, cc, out var stringSortingMode))
                {
                    if (bool.TryParse(stringSortingMode, out var boolSortingMode))
                    {
                        listView.sortingMode = boolSortingMode ? ColumnSortingMode.Custom : ColumnSortingMode.None;
                    }
                    else
                    {
                        listView.sortingMode = m_SortingMode.GetValueFromBag(bag, cc);
                    }
                }

                listView.sortColumnDescriptions = m_SortColumnDescriptions.GetValueFromBag(bag, cc);
                listView.columns = m_Columns.GetValueFromBag(bag, cc);
            }
        }

        Columns m_Columns;
        ColumnSortingMode m_SortingMode;
        SortColumnDescriptions m_SortColumnDescriptions = new SortColumnDescriptions();
        List<SortColumnDescription> m_SortedColumns = new List<SortColumnDescription>();

        /// <summary>
        /// The view controller for this view, cast as a <see cref="MultiColumnListViewController"/>.
        /// </summary>
        public new MultiColumnListViewController viewController => base.viewController as MultiColumnListViewController;

        /// <summary>
        /// If a column is clicked to change sorting, this event is raised to allow users to sort the list view items.
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
        public Columns columns
        {
            get => m_Columns;
            private set
            {
                if (value == null)
                {
                    m_Columns.Clear();
                    return;
                }

                m_Columns = value;

                if (m_Columns.Count > 0)
                {
                    GetOrCreateViewController();
                }
            }
        }

        /// <summary>
        /// The collection of sorted columns by default.
        /// </summary>
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
        public ColumnSortingMode sortingMode
        {
            get => m_SortingMode;
            set
            {
                m_SortingMode = value;
                if (viewController != null)
                {
                    viewController.columnController.sortingMode = value;
                }
            }
        }

        /// <summary>
        /// Initializes a MultiColumnListView with an empty header.
        /// </summary>
        public MultiColumnListView()
            : this(new Columns()) {}

        /// <summary>
        /// Initializes a MultiColumnListView with a list of column definitions.
        /// </summary>
        /// <param name="columns">Column definitions used to initialize the header.</param>
        public MultiColumnListView(Columns columns)
        {
            // Setting the view data key on the ScrollView to get view data persistence on the header which
            // is inside the contentViewport.
            // Disabling view data persistence on the vertical and horizontal scrollers to make sure we keep
            // the previous behavior on the scrollOffset.
            scrollView.viewDataKey = "unity-multi-column-scroll-view";
            scrollView.verticalScroller.viewDataKey = null;
            scrollView.horizontalScroller.viewDataKey = null;

            this.columns = columns ?? new Columns();
        }

        protected override CollectionViewController CreateViewController() => new MultiColumnListViewController(columns, sortColumnDescriptions, m_SortedColumns);

        /// <summary>
        /// Assigns the view controller for this view and registers all events required for it to function properly.
        /// </summary>
        /// <param name="controller">The controller to use with this view.</param>
        /// <remarks>The controller should implement <see cref="MultiColumnListViewController"/>.</remarks>
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
            CreateVirtualizationController<ReusableMultiColumnListViewItem>();
        }

        void RaiseColumnSortingChanged()
        {
            columnSortingChanged?.Invoke();
        }

        void RaiseHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column column)
        {
            headerContextMenuPopulateEvent?.Invoke(evt, column);
        }
    }
}
