// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal class CellRow : VisualElement
    {
        public VisualElement scrollableClip;
        public VisualElement scrollableContainer;
        public List<VisualElement> cells = new();

        public CellRow()
        {
            scrollableClip = new VisualElement { name = "scrollable-clip", style = { overflow = Overflow.Hidden, minWidth = 0 } };
            scrollableContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            scrollableClip.Add(scrollableContainer);
            Add(scrollableClip);
        }

        public void SetScrollableContainerOffset(float value) => scrollableContainer.style.translate = new Vector2(value, 0);

        public void AddCell(VisualElement cellContainer, FreezeState freezeState)
        {
            cells.Add(cellContainer);
            switch (freezeState)
            {
                case FreezeState.FreezeLeft:
                    var freeIndex = IndexOf(scrollableClip);
                    Insert(freeIndex, cellContainer);
                    break;
                case FreezeState.FreezeRight:
                    Add(cellContainer);
                    break;
                case FreezeState.None:
                    scrollableContainer.Add(cellContainer);
                    break;
            }
        }
    }

    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal abstract class CollectionViewLayoutConfiguration
    {
        internal CollectionView m_View;
        public Func<VisualElement> makeCell { get; set; }
        public Action<VisualElement, int> bindCell { get; set; }
        public Action<VisualElement, int> unbindCell { get; set; }
        public Action<VisualElement> destroyCell { get; set; }
    }

    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class MultiColumnLayoutConfiguration : CollectionViewLayoutConfiguration
    {
        Columns m_Columns;
        CollectionViewMultiColumnCollectionHeader m_MultiColumnHeader;
        VisualElement m_HeaderContainer;
        const string k_HeaderViewDataKey = "Header";
        const string k_HeaderContainerViewDataKey = "unity-multi-column-header-container";
        readonly PropertyName k_BoundColumnVePropertyName = "__unity-multi-column-bound-column";
        readonly PropertyName bindableElementPropertyName = "__unity-multi-column-bindable-element";

        static readonly UniqueStyleString k_HierarchyLastColumnHeader = new(MultiColumnHeaderColumn.ussClassName+"__last");

        float m_HorizontalScroll;

        public CollectionViewMultiColumnCollectionHeader header => m_MultiColumnHeader;

        public VisualElement headerContainer => m_HeaderContainer;
        public event Action<ContextualMenuPopulateEvent, Column> headerContextMenuPopulateEvent;

        public MultiColumnLayoutConfiguration()
        {
            columns = new Columns();

            makeCell += MakeCell;
            bindCell += BindCell;
            unbindCell += UnbindCell;
            destroyCell += DestroyCell;
        }

        /// <summary>
        /// The collection of columns for the multi-column header.
        /// </summary>
        [CreateProperty]
        public Columns columns
        {
            get => m_Columns;
            set
            {
                if (value == null)
                {
                    m_Columns.Clear();
                    return;
                }

                m_Columns = value;

                if (m_Columns.Count > 0)
                {
                    CreateMultiColumnHeader();
                }
            }
        }

        VisualElement DefaultMakeCellItem()
        {
            var label = new Label();
            label.AddToClassList(MultiColumnController.cellUssClassNameUnique);
            return label;
        }

        VisualElement MakeCell()
        {
            if (m_MultiColumnHeader == null)
            {
                return new Label();
            }

            var container = new CellRow { name = MultiColumnController.rowContainerUssClassName};
            container.SetScrollableContainerOffset(m_HorizontalScroll);
            container.AddToClassList(MultiColumnController.rowContainerUssClassNameUnique);

            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                var cellContainer = new VisualElement();
                cellContainer.AddToClassList(MultiColumnController.cellUssClassNameUnique);

                var cellItem = column.makeCell?.Invoke() ?? DefaultMakeCellItem();
                cellContainer.SetProperty(bindableElementPropertyName, cellItem);

                cellContainer.Add(cellItem);
                container.AddCell(cellContainer, header.GetColumnFreezeState(column));
            }

            return container;
        }

        void BindCell(VisualElement element, int index)
        {
            var row = element as CellRow;
            var i = 0;
            row.SetScrollableContainerOffset(m_HorizontalScroll);

            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                if (!m_MultiColumnHeader.columnDataMap.TryGetValue(column, out var columnData))
                    continue;

                var cellContainer = row.cells[i++];
                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;

                if (column.bindCell != null)
                {
                    column.bindCell.Invoke(cellItem, index);
                }

                var width = columnData.control.resolvedStyle.width;
                cellContainer.style.width = width;
                cellContainer.SetProperty(k_BoundColumnVePropertyName, column);
            }

            var frozenWidth = CalculateTotalFrozenWidth();
            var totalWidth = header.columnContainer.layoutSize.x;

            element.style.width = totalWidth;

            var scrollableWidth = Mathf.Max(CollectionViewMultiColumnCollectionHeader.k_MinScrollableWidth, totalWidth - frozenWidth);
            row.scrollableClip.style.maxWidth = scrollableWidth;
            row.scrollableClip.style.width = scrollableWidth;
        }

        public void ScrollHorizontally(float horizontalOffset)
        {
            m_HorizontalScroll = -horizontalOffset;
            header.ScrollHorizontally(m_HorizontalScroll);

            foreach (var displayItem in m_View.m_DisplayedList)
            {
                if (displayItem.element is CellRow row)
                    row.SetScrollableContainerOffset(m_HorizontalScroll);
            }
        }

        /// <summary>
        /// Updates width of the cells of the given row based on the column header width.
        /// </summary>
        internal void UpdateRowCellsWidth(VisualElement element)
        {
            if (element is not CellRow row)
                return;

            element.style.width = header.columnContainer.layoutSize.x;

            var columnIndex = 0;

            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                if (columnIndex >= row.cells.Count)
                    break;

                var columnData = m_MultiColumnHeader.columnDataMap[column];
                var width = columnData.control.resolvedStyle.width;
                row.cells[columnIndex].style.width = width;
                columnIndex++;
            }

            var frozenWidth = CalculateTotalFrozenWidth();
            var scrollableWidth = Mathf.Max(CollectionViewMultiColumnCollectionHeader.k_MinScrollableWidth, header.columnContainer.layoutSize.x - frozenWidth);

            row.scrollableClip.style.maxWidth = scrollableWidth;
            row.scrollableClip.style.width = scrollableWidth;
        }

        void UnbindCell(VisualElement element, int index)
        {
            if (element is not CellRow row)
                return;

            // Use the cells list instead of Children()
            foreach (var cellContainer in row.cells)
            {
                if (cellContainer.GetProperty(k_BoundColumnVePropertyName) is not Column column)
                    continue;

                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                column.unbindCell?.Invoke(cellItem, index);
            }
        }

        void DestroyCell(VisualElement element)
        {
            if (element is not CellRow row)
                return;

            foreach (var cellContainer in row.cells)
            {
                var column = cellContainer.GetProperty(k_BoundColumnVePropertyName) as Column;
                if (column == null)
                    continue;

                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                column.destroyCell?.Invoke(cellItem);
                cellContainer.ClearProperty(k_BoundColumnVePropertyName);
            }
        }

        public VisualElement CreateMultiColumnHeader()
        {
            if (m_MultiColumnHeader != null)
                Dispose();

            m_MultiColumnHeader = new CollectionViewMultiColumnCollectionHeader(columns, new SortColumnDescriptions(), new List<SortColumnDescription>())
            {
                viewDataKey = k_HeaderViewDataKey
            };
            m_MultiColumnHeader.contextMenuPopulateEvent += OnContextMenuPopulateEvent;
            m_MultiColumnHeader.columnResized += OnColumnResized;
            m_MultiColumnHeader.viewDataRestored += OnViewDataRestored;

            m_MultiColumnHeader.columns.columnAdded += OnColumnAdded;
            m_MultiColumnHeader.columns.columnRemoved += OnColumnRemoved;
            m_MultiColumnHeader.columns.columnReordered += OnColumnReordered;
            m_MultiColumnHeader.columns.columnChanged += OnColumnsChanged;
            m_MultiColumnHeader.columns.changed += OnColumnChanged;
            m_MultiColumnHeader.RegisterCallback<GeometryChangedEvent>(OnHeaderGeometryChanged);

            // Create the header to the multi column view.
            m_HeaderContainer = new VisualElement { name = MultiColumnController.headerContainerUssClassName };
            m_HeaderContainer.AddToClassList(MultiColumnController.headerContainerUssClassNameUnique);
            m_HeaderContainer.viewDataKey = k_HeaderContainerViewDataKey;
            m_HeaderContainer.Add(m_MultiColumnHeader);

            m_MultiColumnHeader.RegisterCallback<GeometryChangedEvent>(ResizeToFitCallback);

            return m_HeaderContainer;
        }

        void Dispose()
        {
            m_MultiColumnHeader.contextMenuPopulateEvent -= OnContextMenuPopulateEvent;
            m_MultiColumnHeader.columnResized -= OnColumnResized;
            m_MultiColumnHeader.viewDataRestored -= OnViewDataRestored;
            m_MultiColumnHeader.columns.columnAdded -= OnColumnAdded;
            m_MultiColumnHeader.columns.columnRemoved -= OnColumnRemoved;
            m_MultiColumnHeader.columns.columnReordered -= OnColumnReordered;
            m_MultiColumnHeader.columns.columnChanged -= OnColumnsChanged;
            m_MultiColumnHeader.columns.changed -= OnColumnChanged;
            m_MultiColumnHeader.UnregisterCallback<GeometryChangedEvent>(OnHeaderGeometryChanged);
            m_MultiColumnHeader.RemoveFromHierarchy();
            m_MultiColumnHeader.Dispose();
            m_MultiColumnHeader = null;

            m_HeaderContainer.RemoveFromHierarchy();
            m_HeaderContainer = null;
        }

        void OnHeaderGeometryChanged(GeometryChangedEvent evt)
        {
            // When header layout stabilizes, ensure scrollbar is updated
            if (!Mathf.Approximately(evt.oldRect.width, evt.newRect.width))
            {
                m_View.schedule.Execute(() => m_View.UpdateScrollingRangeAfterLayout());
            }
        }

        void OnContextMenuPopulateEvent(ContextualMenuPopulateEvent evt, Column column) => headerContextMenuPopulateEvent?.Invoke(evt, column);

        void OnColumnResized(int index, float width)
        {
            if (m_MultiColumnHeader == null || index < 0 || index >= m_MultiColumnHeader.columns.Count)
                return;

            var header = m_MultiColumnHeader;
            var column = header.columns[index];
            var visibleIndex = -1;

            for (var i = 0; i < header.columns.visibleList.Count; i++)
            {
                if (header.columns.visibleList[i] == column)
                {
                    visibleIndex = i;
                    break;
                }
            }

            if (visibleIndex == -1)
                return;

            var frozenWidth = CalculateTotalFrozenWidth();
            var totalRowWidth = header.columnContainer.layoutSize.x;
            var scrollableWidth = Mathf.Max(CollectionViewMultiColumnCollectionHeader.k_MinScrollableWidth, totalRowWidth - frozenWidth);

            foreach (var displayItem in m_View.m_DisplayedList)
            {
                displayItem.element.style.width = totalRowWidth;

                if (displayItem.element is not CellRow row)
                    continue;

                if (visibleIndex < row.cells.Count)
                {
                    row.cells[visibleIndex].style.width = width;
                }

                row.scrollableClip.style.maxWidth = scrollableWidth;
                row.scrollableClip.style.width = scrollableWidth;
            }

            m_View.UpdateScrollingRangeAfterLayout();
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

            UpdateColumnsStyles();
            m_View.Rebuild();
        }

        void OnColumnsChanged(Column column, ColumnDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            UpdateColumnsStyles();
            if (type == ColumnDataType.Visibility)
                m_View.ScheduleRebuild();
        }

        void OnColumnChanged(ColumnsDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            UpdateColumnsStyles();
            if (type == ColumnsDataType.PrimaryColumn)
                m_View.ScheduleRebuild();
        }

        void OnViewDataRestored()
        {
            m_View.Rebuild();
        }

        void ResizeToFitCallback(GeometryChangedEvent _)
        {
            m_MultiColumnHeader.UnregisterCallback<GeometryChangedEvent>(ResizeToFitCallback);
            m_MultiColumnHeader.ResizeToFit();
        }

        void UpdateColumnsStyles()
        {
            var scrollableContainer = m_MultiColumnHeader.Q(className: CollectionViewMultiColumnCollectionHeader.scrollableColumnsContainerUssClassName.value);
            var headers = scrollableContainer?.Query<VisualElement>(className: "unity-multi-column-header__column").ToList();

            if (headers == null)
                return;

            foreach (var column in headers)
                column.RemoveFromClassList(k_HierarchyLastColumnHeader);

            var horizontalScroller = m_View.scrollView.horizontalScroller;
            // Expand only the last element, but only when the window content is smaller than the window
            // (which means when the horizontal scroller is not shown)
            if (!horizontalScroller.enabledSelf)
            {
                // Applying style.flexGrow = 1 to the last column
                headers[^1].AddToClassList(k_HierarchyLastColumnHeader);
            }
        }

        float CalculateTotalFrozenWidth()
        {
            return CollectionViewFrozenColumnUtility.CalculateTotalFrozenWidth(m_MultiColumnHeader, m_MultiColumnHeader.columns);
        }
    }

    [VisibleToOtherModules("UnityEngine.HierarchyModule")]
    internal class LayoutConfiguration : CollectionViewLayoutConfiguration
    {
        public LayoutConfiguration()
        {
            makeCell += MakeCell;
            bindCell += BindCell;
            unbindCell += UnbindCell;
            destroyCell += DestroyCell;
        }

        VisualElement MakeCell() => new Label();
        void BindCell(VisualElement element, int index) { }
        void UnbindCell(VisualElement element, int index) { }
        void DestroyCell(VisualElement element) { }
    }
}
