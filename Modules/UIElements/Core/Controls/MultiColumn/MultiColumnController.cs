// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines the sorting mode of a <see cref="MultiColumnListView"/> or <see cref="MultiColumnTreeView"/>.
    /// </summary>
    public enum ColumnSortingMode
    {
        /// <summary>
        /// Sorting is disabled.
        /// </summary>
        None,
        /// <summary>
        /// The default Unity sorting will be used. Define how to compare items in a column with <see cref="Column.comparison"/>.
        /// </summary>
        Default,
        /// <summary>
        /// Sorting is left to the user in the <see cref="MultiColumnListView.columnSortingChanged"/> or <see cref="MultiColumnTreeView.columnSortingChanged"/>.
        /// </summary>
        Custom,
    }

    /// <summary>
    /// The default controller for a multi column view. Takes care of adding the MultiColumnCollectionHeader and
    /// reacting to the various callbacks.
    /// </summary>
    public class MultiColumnController : IDisposable
    {
        private static readonly PropertyName k_BoundColumnVePropertyName = "__unity-multi-column-bound-column";
        internal static readonly PropertyName bindableElementPropertyName = "__unity-multi-column-bindable-element";

        internal static readonly string baseUssClassName = "unity-multi-column-view";
        static readonly string k_HeaderContainerViewDataKey = "unity-multi-column-header-container";

        /// <summary>
        /// The USS class name for the header container inside a multi column view.
        /// </summary>
        public static readonly string headerContainerUssClassName = baseUssClassName + "__header-container";
        /// <summary>
        /// The USS class name for all row containers inside a multi column view.
        /// </summary>
        public static readonly string rowContainerUssClassName = baseUssClassName + "__row-container";
        /// <summary>
        /// The USS class name for all cells inside a multi column view.
        /// </summary>
        public static readonly string cellUssClassName = baseUssClassName + "__cell";
        /// <summary>
        /// The USS class name for default labels cells inside a multi column view.
        /// </summary>
        public static readonly string cellLabelUssClassName = cellUssClassName + "__label";
        private static readonly string k_HeaderViewDataKey = "Header";

        /// <summary>
        /// Raised when sorting changes for a column.
        /// </summary>
        public event Action columnSortingChanged;

        /// <summary>
        /// Raised when a column is right-clicked to bring context menu options.
        /// </summary>
        public event Action<ContextualMenuPopulateEvent, Column> headerContextMenuPopulateEvent;

        List<int> m_SortedIndices;
        ColumnSortingMode m_SortingMode;

        BaseVerticalCollectionView m_View;
        VisualElement m_HeaderContainer;
        MultiColumnCollectionHeader m_MultiColumnHeader;

        internal MultiColumnCollectionHeader header => m_MultiColumnHeader;

        internal ColumnSortingMode sortingMode
        {
            get => m_SortingMode;
            set
            {
                m_SortingMode = value;
                header.sortingEnabled = m_SortingMode != ColumnSortingMode.None;
            }
        }

        /// <summary>
        /// Constructor. It will create the <see cref="MultiColumnCollectionHeader"/> to use for the view.
        /// </summary>
        /// <param name="columns">The columns data used to initialize the header.</param>
        /// <param name="sortDescriptions">The sort data used to initialize the header.</param>
        /// <param name="sortedColumns">The sorted columns for the view.</param>
        /// <remarks>The header will be added to the view in the <see cref="PrepareView"/> phase.</remarks>
        public MultiColumnController(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
        {
            m_MultiColumnHeader = new MultiColumnCollectionHeader(columns, sortDescriptions, sortedColumns) { viewDataKey = k_HeaderViewDataKey };
            m_MultiColumnHeader.columnSortingChanged += OnColumnSortingChanged;
            m_MultiColumnHeader.contextMenuPopulateEvent += OnContextMenuPopulateEvent;
            m_MultiColumnHeader.columnResized += OnColumnResized;
            m_MultiColumnHeader.viewDataRestored += OnViewDataRestored;

            m_MultiColumnHeader.columns.columnAdded += OnColumnAdded;
            m_MultiColumnHeader.columns.columnRemoved += OnColumnRemoved;
            m_MultiColumnHeader.columns.columnReordered += OnColumnReordered;

            m_MultiColumnHeader.columns.columnChanged += OnColumnsChanged;
            m_MultiColumnHeader.columns.changed += OnColumnChanged;
        }

        static void BindCellItem<T>(VisualElement ve, int rowIndex, Column column, T item)
        {
            if (column.bindCell != null)
            {
                column.bindCell.Invoke(ve, rowIndex);
            }
            else
            {
                DefaultBindCellItem(ve, item);
            }
        }

        static void UnbindCellItem(VisualElement ve, int rowIndex, Column column)
        {
            column.unbindCell?.Invoke(ve, rowIndex);
        }

        static VisualElement DefaultMakeCellItem()
        {
            var label = new Label();
            label.AddToClassList(cellLabelUssClassName);
            return label;
        }

        static void DefaultBindCellItem<T>(VisualElement ve, T item)
        {
            if (ve is Label label)
            {
                label.text = item.ToString();
            }
        }

        /// <summary>
        /// Creates a VisualElement to use in the virtualization of the collection view.
        /// It will create a cell for every visible column.
        /// </summary>
        /// <returns>A VisualElement for the row.</returns>
        public VisualElement MakeItem()
        {
            var container = new VisualElement() { name = rowContainerUssClassName };
            container.AddToClassList(rowContainerUssClassName);

            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                var cellContainer = new VisualElement();
                cellContainer.AddToClassList(cellUssClassName);

                var cellItem = column.makeCell?.Invoke() ?? DefaultMakeCellItem();
                cellContainer.SetProperty(bindableElementPropertyName, cellItem);

                cellContainer.Add(cellItem);
                container.Add(cellContainer);
            }

            return container;
        }

        /// <summary>
        /// Binds a row of multiple cells to an item index.
        /// </summary>
        /// <param name="element">The element from that row, created by MakeItem().</param>
        /// <param name="index">The item index.</param>
        /// <param name="item">The item to bind.</param>
        public void BindItem<T>(VisualElement element, int index, T item)
        {
            var i = 0;
            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                if (!m_MultiColumnHeader.columnDataMap.TryGetValue(column, out var columnData))
                    continue;

                var cellContainer = element[i++];
                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                BindCellItem(cellItem, index, column, item);
                cellContainer.style.width = columnData.control.resolvedStyle.width;
                cellContainer.SetProperty(k_BoundColumnVePropertyName, column);
            }
        }

        /// <summary>
        /// Unbinds the row at the item index.
        /// </summary>
        /// <param name="element">The element from that row, created by MakeItem().</param>
        /// <param name="index">The item index.</param>
        public void UnbindItem(VisualElement element, int index)
        {
            foreach (var cellContainer in element.Children())
            {
                var column = cellContainer.GetProperty(k_BoundColumnVePropertyName) as Column;
                if (column == null)
                    continue;

                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                UnbindCellItem(cellItem, index, column);
                cellContainer.SetProperty(k_BoundColumnVePropertyName, null);
            }
        }

        /// <summary>
        /// Destroys a VisualElement when the view is rebuilt or cleared.
        /// </summary>
        /// <param name="element">The element being destroyed.</param>
        public void DestroyItem(VisualElement element)
        {
            foreach (var cellContainer in element.Children())
            {
                var column = cellContainer.GetProperty(k_BoundColumnVePropertyName) as Column;
                if (column == null)
                    continue;

                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                column.destroyCell?.Invoke(cellItem);
            }
        }

        /// <summary>
        /// Initialization step once the view is set.
        /// It will insert the multi column header in the hierarchy and register to important callbacks.
        /// </summary>
        /// <param name="collectionView">The view to register to.</param>
        public void PrepareView(BaseVerticalCollectionView collectionView)
        {
            if (m_View != null)
            {
                Debug.LogWarning("Trying to initialize multi column view more than once. This shouldn't happen.");
                return;
            }

            m_View = collectionView;

            // Insert header to the multi column view.
            m_HeaderContainer = new VisualElement { name = headerContainerUssClassName };
            m_HeaderContainer.AddToClassList(headerContainerUssClassName);
            m_HeaderContainer.viewDataKey = k_HeaderContainerViewDataKey;
            collectionView.scrollView.hierarchy.Insert(0, m_HeaderContainer);
            m_HeaderContainer.Add(m_MultiColumnHeader);

            // Handle horizontal scrolling
            m_View.scrollView.horizontalScroller.valueChanged += OnHorizontalScrollerValueChanged;
            m_View.scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            m_MultiColumnHeader.columnContainer.RegisterCallback<GeometryChangedEvent>(OnColumnContainerGeometryChanged);
        }

        /// <summary>
        /// Unregisters events and removes the header from the hierarchy.
        /// </summary>
        public void Dispose()
        {
            if (m_View != null)
            {
                m_View.scrollView.horizontalScroller.valueChanged -= OnHorizontalScrollerValueChanged;
                m_View.scrollView.contentViewport.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
                m_View = null;
            }

            m_MultiColumnHeader.columnContainer.UnregisterCallback<GeometryChangedEvent>(OnColumnContainerGeometryChanged);
            m_MultiColumnHeader.columnSortingChanged -= OnColumnSortingChanged;
            m_MultiColumnHeader.contextMenuPopulateEvent -= OnContextMenuPopulateEvent;
            m_MultiColumnHeader.columnResized -= OnColumnResized;
            m_MultiColumnHeader.viewDataRestored -= OnViewDataRestored;
            m_MultiColumnHeader.columns.columnAdded -= OnColumnAdded;
            m_MultiColumnHeader.columns.columnRemoved -= OnColumnRemoved;
            m_MultiColumnHeader.columns.columnReordered -= OnColumnReordered;
            m_MultiColumnHeader.columns.columnChanged -= OnColumnsChanged;
            m_MultiColumnHeader.columns.changed -= OnColumnChanged;
            m_MultiColumnHeader.RemoveFromHierarchy();
            m_MultiColumnHeader.Dispose();
            m_MultiColumnHeader = null;

            m_HeaderContainer.RemoveFromHierarchy();
            m_HeaderContainer = null;
        }

        void OnHorizontalScrollerValueChanged(float v)
        {
            m_MultiColumnHeader.ScrollHorizontally(v);
        }

        void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            var headerPadding = m_MultiColumnHeader.resolvedStyle.paddingLeft + m_MultiColumnHeader.resolvedStyle.paddingRight;
            m_MultiColumnHeader.style.maxWidth = evt.newRect.width - headerPadding;
            m_MultiColumnHeader.style.maxWidth = evt.newRect.width - headerPadding;

            UpdateContentContainer(m_View);
        }

        void OnColumnContainerGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateContentContainer(m_View);
        }

        void UpdateContentContainer(BaseVerticalCollectionView collectionView)
        {
            var headerTotalWidth = m_MultiColumnHeader.columnContainer.layout.width;
            var targetWidth = Mathf.Max(headerTotalWidth, collectionView.scrollView.contentViewport.resolvedStyle.width);
            collectionView.scrollView.contentContainer.style.width = targetWidth;
        }

        void OnColumnSortingChanged()
        {
            if (SortIfNeeded())
            {
                m_View.RefreshItems();
            }

            columnSortingChanged?.Invoke();
        }

        internal bool SortIfNeeded()
        {
            if (sortingMode == ColumnSortingMode.None)
            {
                m_View.dragger.enabled = true;
                return false;
            }

            m_View.dragger.enabled = header.sortedColumnReadonly.Count == 0;

            if (sortingMode != ColumnSortingMode.Default || m_View.itemsSource == null)
            {
                return false;
            }

            var wasSorted = m_SortedIndices?.Count > 0;
            if (wasSorted)
            {
                // We need to unbind before the indices are sorted, so that the sorted index matches the previous one.
                m_View.virtualizationController.UnbindAll();
            }

            m_SortedIndices?.Clear();

            if (header.sortedColumnReadonly.Count == 0)
            {
                return wasSorted;
            }

            using var pool = ListPool<int>.Get(out var sortedList);
            for (var i = 0; i < m_View.itemsSource.Count; i++)
            {
                sortedList.Add(i);
            }

            sortedList.Sort(CombinedComparison);

            m_SortedIndices ??= new List<int>(capacity: m_View.itemsSource.Count);
            m_SortedIndices.AddRange(sortedList);

            return true;
        }

        int CombinedComparison(int a, int b)
        {
            if (m_View.viewController is BaseTreeViewController treeViewController)
            {
                var idA = treeViewController.GetIdForIndex(a);
                var idB = treeViewController.GetIdForIndex(b);
                var parentIdA = treeViewController.GetParentId(idA);
                var parentIdB = treeViewController.GetParentId(idB);

                // Only sort items within the same parent.
                if (parentIdA != parentIdB)
                {
                    var depthA = treeViewController.GetIndentationDepth(idA);
                    var depthB = treeViewController.GetIndentationDepth(idB);
                    var originalDepthA = depthA;
                    var originalDepthB = depthB;

                    // We walk up until both sides are at the same depth
                    while (depthA > depthB)
                    {
                        depthA--;
                        idA = parentIdA;
                        parentIdA = treeViewController.GetParentId(parentIdA);
                    }

                    while (depthB > depthA)
                    {
                        depthB--;
                        idB = parentIdB;
                        parentIdB = treeViewController.GetParentId(parentIdB);
                    }

                    // Now both are at the same depth, we then walk up the tree until we hit the same element
                    while (parentIdA != parentIdB)
                    {
                        idA = parentIdA;
                        idB = parentIdB;
                        parentIdA = treeViewController.GetParentId(parentIdA);
                        parentIdB = treeViewController.GetParentId(parentIdB);
                    }

                    // We were looking at a node and one of its parent, so compare the original depths.
                    if (idA == idB)
                    {
                        return originalDepthA.CompareTo(originalDepthB);
                    }

                    // Compare the indices now that we're at the same depth.
                    a = treeViewController.GetIndexForId(idA);
                    b = treeViewController.GetIndexForId(idB);
                }
            }

            var result = 0;
            foreach (var sortedColumn in header.sortedColumns)
            {
                result = sortedColumn.column.comparison?.Invoke(a, b) ?? 0;
                if (result != 0)
                {
                    if (sortedColumn.direction == SortDirection.Descending)
                    {
                        result = -result;
                    }
                    break;
                }
            }

            // When equal, we keep the current order.
            return result == 0 ? a.CompareTo(b) : result;
        }

        internal int GetSortedIndex(int index)
        {
            if (m_SortedIndices == null)
                return index;

            if (index < 0 || index >= m_SortedIndices.Count)
                return index;

            return m_SortedIndices.Count > 0 ? m_SortedIndices[index] : index;
        }

        void OnContextMenuPopulateEvent(ContextualMenuPopulateEvent evt, Column column) => headerContextMenuPopulateEvent?.Invoke(evt, column);

        void OnColumnResized(int index, float width)
        {
            foreach (var item in m_View.activeItems)
            {
                item.bindableElement.ElementAt(index).style.width = width;
            }
        }

        void OnColumnAdded(Column column, int index)
        {
            m_View.Rebuild();
        }

        void OnColumnRemoved(Column column)
        {
            m_View.Rebuild();
        }

        void OnColumnReordered(Column column, int from, int to)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            m_View.Rebuild();
        }

        void OnColumnsChanged(Column column, ColumnDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            if (type == ColumnDataType.Visibility) m_View.Rebuild();
        }

        void OnColumnChanged(ColumnsDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            if (type == ColumnsDataType.PrimaryColumn) m_View.Rebuild();
        }

        void OnViewDataRestored()
        {
            m_View.Rebuild();
        }
    }
}
