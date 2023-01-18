// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A tree view with multi column support.
    /// </summary>
    public class MultiColumnTreeView : BaseTreeView
    {
        /// <summary>
        /// Instantiates a <see cref="MultiColumnTreeView"/> using data from a UXML file.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<MultiColumnTreeView, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="MultiColumnTreeView"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the MultiColumnTreeView element properties that you can use in a UI document asset (UXML file).
        /// </remarks>
        public new class UxmlTraits : BaseTreeView.UxmlTraits
        {
            readonly UxmlBoolAttributeDescription m_SortingEnabled = new UxmlBoolAttributeDescription { name = "sorting-enabled" };
            readonly UxmlObjectAttributeDescription<Columns> m_Columns = new UxmlObjectAttributeDescription<Columns>();
            readonly UxmlObjectAttributeDescription<SortColumnDescriptions> m_SortColumnDescriptions = new UxmlObjectAttributeDescription<SortColumnDescriptions>();

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
                treeView.sortingEnabled = m_SortingEnabled.GetValueFromBag(bag, cc);
                treeView.sortColumnDescriptions = m_SortColumnDescriptions.GetValueFromBag(bag, cc);
                treeView.columns = m_Columns.GetValueFromBag(bag, cc);
            }
        }

        Columns m_Columns;

        bool m_SortingEnabled;
        SortColumnDescriptions m_SortColumnDescriptions = new SortColumnDescriptions();
        List<SortColumnDescription> m_SortedColumns = new List<SortColumnDescription>();

        /// <summary>
        /// The view controller for this view, cast as a <see cref="MultiColumnTreeViewController"/>.
        /// </summary>
        public new MultiColumnTreeViewController viewController => base.viewController as MultiColumnTreeViewController;

        /// <summary>
        /// If a column is clicked to change sorting, this event is raised to allow users to sort the tree view items.
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
            }
        }

        /// <summary>
        /// Whether or not sorting is enabled in the multi-column header.
        /// </summary>
        public bool sortingEnabled
        {
            get => m_SortingEnabled;
            set
            {
                m_SortingEnabled = value;
                if (viewController != null)
                {
                    viewController.columnController.header.sortingEnabled = value;
                }
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
                viewController.header.sortingEnabled = m_SortingEnabled;
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

        void RaiseHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column column)
        {
            headerContextMenuPopulateEvent?.Invoke(evt, column);
        }
    }
}
