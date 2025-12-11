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
        MultiColumnCollectionHeader m_MultiColumnHeader;
        VisualElement m_HeaderContainer;
        const string k_HeaderViewDataKey = "Header";
        const string k_HeaderContainerViewDataKey = "unity-multi-column-header-container";
        readonly PropertyName k_BoundColumnVePropertyName = "__unity-multi-column-bound-column";
        readonly PropertyName bindableElementPropertyName = "__unity-multi-column-bindable-element";

        internal MultiColumnCollectionHeader header => m_MultiColumnHeader;

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
            label.AddToClassList(MultiColumnController.cellUssClassName);
            return label;
        }

        VisualElement MakeCell()
        {
            if (m_MultiColumnHeader == null)
            {
                return new Label();
            }

            var container = new VisualElement() { name = MultiColumnController.rowContainerUssClassName };
            container.AddToClassList(MultiColumnController.rowContainerUssClassName);

            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                var cellContainer = new VisualElement();
                cellContainer.AddToClassList(MultiColumnController.cellUssClassName);

                var cellItem = column.makeCell?.Invoke() ?? DefaultMakeCellItem();
                cellContainer.SetProperty(bindableElementPropertyName, cellItem);

                cellContainer.Add(cellItem);
                container.Add(cellContainer);
            }

            return container;
        }

        void BindCell(VisualElement element, int index)
        {
            var i = 0;
            element.style.width = header.columnContainer.layout.size.x;
            foreach (var column in m_MultiColumnHeader.columns.visibleList)
            {
                if (!m_MultiColumnHeader.columnDataMap.TryGetValue(column, out var columnData))
                    continue;

                var cellContainer = element[i++];
                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;

                if (column.bindCell != null)
                {
                    column.bindCell.Invoke(cellItem, index);
                }
                cellContainer.style.width = columnData.control.resolvedStyle.width;
                cellContainer.SetProperty(k_BoundColumnVePropertyName, column);
            }
        }

        void UnbindCell(VisualElement element, int index)
        {
            foreach (var cellContainer in element.Children())
            {
                if (cellContainer.GetProperty(k_BoundColumnVePropertyName) is not Column column)
                    continue;

                var cellItem = cellContainer.GetProperty(bindableElementPropertyName) as VisualElement;
                column.unbindCell?.Invoke(cellItem, index);
            }
        }

        void DestroyCell(VisualElement element)
        {
            foreach (var cellContainer in element.Children())
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

            m_MultiColumnHeader = new MultiColumnCollectionHeader(columns, new SortColumnDescriptions(), new List<SortColumnDescription>())
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

            // Create the header to the multi column view.
            m_HeaderContainer = new VisualElement { name = MultiColumnController.headerContainerUssClassName };
            m_HeaderContainer.AddToClassList(MultiColumnController.headerContainerUssClassName);
            m_HeaderContainer.viewDataKey = k_HeaderContainerViewDataKey;
            m_HeaderContainer.Add(m_MultiColumnHeader);
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
            m_MultiColumnHeader.RemoveFromHierarchy();
            m_MultiColumnHeader.Dispose();
            m_MultiColumnHeader = null;

            m_HeaderContainer.RemoveFromHierarchy();
            m_HeaderContainer = null;
        }

        void OnContextMenuPopulateEvent(ContextualMenuPopulateEvent evt, Column column) => headerContextMenuPopulateEvent?.Invoke(evt, column);

        void OnColumnResized(int index, float width)
        {
            if (m_View.isRebuildScheduled)
            {
                // We are waiting on a rebuild, so our elements are most likely not the right ones.
                // We'll let the rebuild handle the new width.
                return;
            }

            foreach (var displayItem in m_View.m_DisplayedList)
            {
                // We need to make sure that the item's width is updated as well.
                displayItem.element.style.width = header.columnContainer.layout.size.x;
                // Update the cell's width
                displayItem.element[index].style.width = width;
            }

            // Update the scroller's sizes
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

            m_View.Rebuild();
        }

        void OnColumnsChanged(Column column, ColumnDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            if (type == ColumnDataType.Visibility)
                m_View.ScheduleRebuild();
        }

        void OnColumnChanged(ColumnsDataType type)
        {
            if (m_MultiColumnHeader.isApplyingViewState)
                return;

            if (type == ColumnsDataType.PrimaryColumn)
                m_View.ScheduleRebuild();
        }

        void OnViewDataRestored()
        {
            m_View.Rebuild();
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
