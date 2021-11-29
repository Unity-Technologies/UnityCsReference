// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements.Internal
{
    /// <summary>
    /// The multi-column header. It handles resize, sorting, visibility events and sends them to the listening controller.
    /// </summary>
    class MultiColumnCollectionHeader : VisualElement, IDisposable
    {
        const int kMaxStableLayoutPassCount = 2; // Beyond this threshold, DoLayout must be performed in the next frame; otherwise, this may lead to Layout instabilities. This is caused by the dependencies between the geometries of the header, the viewport and the content.

        [Serializable]
        class ViewState : ISerializationCallbackReceiver
        {
            bool m_HasPersistedData;

            public MultiColumnCollectionHeader header { get; set; }

            /// <summary>
            /// State of columns.
            /// </summary>
            [Serializable]
            struct ColumnState
            {
                public int index;
                public string name;
                public float actualWidth;
                public float width;
                public bool visible;
            }

            [SerializeField]
            List<SortColumnDescription> m_SortDescriptions = new List<SortColumnDescription>();

            [SerializeField]
            List<ColumnState> m_OrderedColumnStates = new List<ColumnState>();

            /// <summary>
            /// Saves the state of the specified header control.
            /// </summary>
            /// <param name="header">The header control of which state will be saved.</param>
            void Save(MultiColumnCollectionHeader header)
            {
                m_SortDescriptions.Clear();
                m_OrderedColumnStates.Clear();

                foreach (var sortDesc in header.sortDescriptions)
                {
                    m_SortDescriptions.Add(sortDesc);
                }

                foreach (var column in header.columns.displayList)
                {
                    var columnState = new ColumnState() { index = column.index, name = column.name, actualWidth = column.desiredWidth, width = column.width.value, visible = column.visible };

                    m_OrderedColumnStates.Add(columnState);
                }
            }

            /// <summary>
            /// Applies the state of the specified header control.
            /// </summary>
            public void Apply()
            {
                if (m_HasPersistedData)
                {
                    Apply(header);
                }
            }

            void Apply(MultiColumnCollectionHeader header)
            {
                int minCount = Math.Min(m_OrderedColumnStates.Count, header.columns.Count);
                int nextValidOrderedIndex = 0;

                for (var orderedIndex = 0; (orderedIndex < m_OrderedColumnStates.Count) && (nextValidOrderedIndex < minCount); orderedIndex++)
                {
                    var columnState = m_OrderedColumnStates[orderedIndex];

                    Column column = null;

                    // Find column by name
                    if (!string.IsNullOrEmpty(columnState.name))
                    {
                        if (header.columns.Contains(columnState.name))
                        {
                            column = header.columns[columnState.name];
                        }
                    }
                    else
                    {
                        if (columnState.index > header.columns.Count - 1)
                            continue;

                        column = header.columns[columnState.index];
                        // If the column has a name then we assume it is not the same column anymore
                        if (!string.IsNullOrEmpty(column.name))
                        {
                            column = null;
                        }
                    }

                    if (column == null)
                        continue;

                    header.columns.ReorderDisplay(column.displayIndex, nextValidOrderedIndex++);
                    column.visible = columnState.visible;
                    column.width = columnState.width;
                    column.desiredWidth = columnState.actualWidth;
                }

                header.sortDescriptions.Clear();
                foreach (var sortDesc in m_SortDescriptions)
                {
                    header.sortDescriptions.Add(sortDesc);
                }
            }

            public void OnBeforeSerialize()
            {
                if (header != null)
                {
                    m_HasPersistedData = true;
                    Save(header);
                }
            }

            public void OnAfterDeserialize()
            {
                m_HasPersistedData = true;
            }
        }

        internal class ColumnData
        {
            public MultiColumnHeaderColumn control { get; set; }
            public MultiColumnHeaderColumnResizeHandle resizeHandle { get; set; }
        }

        /// <summary>
        /// The USS class name for MultiColumnCollectionHeader elements.
        /// </summary>
        public static readonly string ussClassName = "unity-multi-column-header";
        /// <summary>
        /// The USS class name for column container elements of multi column headers.
        /// </summary>
        public static readonly string columnContainerUssClassName = ussClassName + "__column-container";
        /// <summary>
        /// The USS class name for handle container elements of multi column headers.
        /// </summary>
        public static readonly string handleContainerUssClassName = ussClassName + "__resize-handle-container";
        /// <summary>
        /// The USS class name for MultiColumnCollectionHeader elements that are in animated reorder mode.
        /// </summary>
        public static readonly string reorderableUssClassName = ussClassName + "__header";

        bool m_SortingEnabled;
        List<SortColumnDescription> m_SortedColumns;

        ViewState m_ViewState;
        bool m_ApplyingViewState;

        bool m_DoLayoutScheduled;

        /// <summary>
        /// Per-Column data.
        /// </summary>
        public Dictionary<Column, ColumnData> columnDataMap { get; } = new Dictionary<Column, ColumnData>();

        /// <summary>
        /// The layout manager to lay columns.
        /// </summary>
        public ColumnLayout columnLayout { get; }

        /// <summary>
        /// Container for column elements.
        /// </summary>
        public VisualElement columnContainer { get; }

        /// <summary>
        /// Container for resize handles.
        /// </summary>
        public VisualElement resizeHandleContainer { get; }

        /// <summary>
        /// The effective list of sorted columns.
        /// </summary>
        public IEnumerable<SortColumnDescription> sortedColumns => m_SortedColumns;

        /// <summary>
        /// The descriptions of sorted columns.
        /// </summary>
        public SortColumnDescriptions sortDescriptions { get; }

        /// <summary>
        ///  The list of columns.
        /// </summary>
        public Columns columns { get; }

        /// <summary>
        /// Gets or sets the value that indicates whether sorting is enabled.
        /// </summary>
        public bool sortingEnabled
        {
            get => m_SortingEnabled;
            set
            {
                if (m_SortingEnabled == value)
                    return;
                m_SortingEnabled = value;
                UpdateSortingStatus();
                UpdateSortedColumns();
            }
        }

        /// <summary>
        /// Sent whenever the column at the specified visual index is resized to the specified size.
        /// </summary>
        public event Action<int, float> columnResized;

        /// <summary>
        /// Sent whenever the column sorting status has changed.
        /// </summary>
        public event Action columnSortingChanged;

        /// <summary>
        ///  Sent whenever a ContextMenuPopulate event sent allowing user code to add its own actions to the context menu.
        /// </summary>
        public event Action<ContextualMenuPopulateEvent, Column> contextMenuPopulateEvent;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MultiColumnCollectionHeader()
            : this(new Columns(), new SortColumnDescriptions(), new List<SortColumnDescription>()) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="sortDescriptions"></param>
        /// <param name="sortedColumns"></param>
        public MultiColumnCollectionHeader(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
        {
            AddToClassList(ussClassName);

            this.columns = columns;
            m_SortedColumns = sortedColumns;
            this.sortDescriptions = sortDescriptions;
            this.sortDescriptions.changed += UpdateSortedColumns;

            columnContainer = new VisualElement()
            {
                pickingMode = PickingMode.Ignore
            };
            columnContainer.AddToClassList(columnContainerUssClassName);
            Add(columnContainer);

            //Ensure that the resize handlers are on top of the columns.
            resizeHandleContainer = new VisualElement()
            {
                pickingMode = PickingMode.Ignore
            };
            resizeHandleContainer.AddToClassList(handleContainerUssClassName);
            resizeHandleContainer.StretchToParentSize();
            Add(resizeHandleContainer);

            columnLayout = new ColumnLayout(columns);
            columnLayout.layoutRequested += ScheduleDoLayout;

            foreach (var column in columns.visibleList)
            {
                OnColumnAdded(column);
            }

            this.columns.columnAdded += OnColumnAdded;
            this.columns.columnRemoved += OnColumnRemoved;
            this.columns.columnChanged += OnColumnChanged;
            this.columns.columnReordered += OnColumnReordered;
            this.columns.columnResized += OnColumnResized;

            this.AddManipulator(new ContextualMenuManipulator(OnContextualMenuManipulator));
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void ScheduleDoLayout()
        {
            if (m_DoLayoutScheduled)
                return;
            schedule.Execute(DoLayout);
            m_DoLayoutScheduled = true;
        }

        /// <summary>
        /// Resizes all columns to fit the width of the header.
        /// </summary>
        void ResizeToFit()
        {
            columnLayout.ResizeToFit(layout.width);
        }

        /// <summary>
        /// Updates the effective list of sorted columns.
        /// </summary>
        void UpdateSortedColumns()
        {
            m_SortedColumns.Clear();

            if (sortingEnabled)
            {
                foreach (var desc in sortDescriptions)
                {
                    Column column = null;

                    if (desc.columnIndex != -1)
                        column = columns[desc.columnIndex];
                    else if (!string.IsNullOrEmpty(desc.columnName))
                        column = columns[desc.columnName];

                    if (column != null && column.sortable)
                    {
                        desc.column = column;
                        m_SortedColumns.Add(desc);
                    }
                    else
                    {
                        desc.column = null;
                    }
                }
            }

            SaveViewState();
            RaiseColumnSortingChanged();
        }

        /// <summary>
        /// Update the column controls and resize handles from the columns.
        /// </summary>
        void UpdateColumnControls()
        {
            bool hasStretch = false;
            Column lastVisibleColumn = null;

            foreach (var col in columns.visibleList)
            {
                hasStretch |= col.stretchable;

                ColumnData columnData = null;

                if (columnDataMap.TryGetValue(col, out columnData))
                {
                    columnData.control.style.minWidth = col.minWidth;
                    columnData.control.style.maxWidth = col.maxWidth;
                    columnData.resizeHandle.style.display = (columns.resizable && col.resizable) ? DisplayStyle.Flex : DisplayStyle.None;
                }
                lastVisibleColumn = col;
            }

            if (hasStretch)
            {
                columnContainer.style.flexGrow = 1;

                // If there is at least one stretchable column then hide the last resizer
                if (columns.stretchMode == Columns.StretchMode.GrowAndFill && columnDataMap.TryGetValue(lastVisibleColumn, out var columnData))
                {
                    columnData.resizeHandle.style.display = DisplayStyle.None;
                }
            }
            else
            {
                columnContainer.style.flexGrow = 0;
            }

            UpdateSortingStatus();
        }

        void OnColumnAdded(Column column, int index) => OnColumnAdded(column);

        /// <summary>
        /// Called whenever a column is added to the header.
        /// </summary>
        /// <param name="column"></param>
        void OnColumnAdded(Column column)
        {
            // If the column was already added then ignore it
            if (columnDataMap.ContainsKey(column))
                return;

            var columnElement = new MultiColumnHeaderColumn(column);

            columnElement.RegisterCallback<GeometryChangedEvent>(OnColumnControlGeometryChanged);
            columnElement.clickable.clickedWithEventInfo += OnColumnClicked;
            // Prevent cursor change when hovering handles while drag reordering columns
            columnElement.mover.activeChanged += OnMoveManipulatorActivated;

            columnContainer.Insert(column.visibleIndex, columnElement);

            var resHandle = new MultiColumnHeaderColumnResizeHandle();

            resizeHandleContainer.Insert(column.visibleIndex, resHandle);
            resHandle.dragArea.AddManipulator(new ColumnResizer(column));

            columnDataMap[column] = new ColumnData()
            {
                control = columnElement,
                resizeHandle = resHandle
            };

            UpdateColumnControls();
            SaveViewState();
        }

        /// <summary>
        /// Called whenever a column is removed from the header.
        /// </summary>
        /// <param name="column"></param>
        void OnColumnRemoved(Column column)
        {
            // If the column was not already added then ignore it
            if (!columnDataMap.ContainsKey(column))
                return;

            var data = columnDataMap[column];

            data.control.UnregisterCallback<GeometryChangedEvent>(OnColumnControlGeometryChanged);
            data.control.clickable.clickedWithEventInfo -= OnColumnClicked;
            data.control.mover.activeChanged -= OnMoveManipulatorActivated;
            data.control.RemoveFromHierarchy();
            data.resizeHandle.RemoveFromHierarchy();

            columnDataMap.Remove(column);

            UpdateColumnControls();
            SaveViewState();
        }

        /// <summary>
        /// Called whenever the data of a column has changed specifing the associated data role.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="type"></param>
        void OnColumnChanged(Column column, ColumnDataType type)
        {
            if (type == ColumnDataType.Visibility)
            {
                if (column.visible)
                    OnColumnAdded(column);
                else
                    OnColumnRemoved(column);
            }

            UpdateColumnControls();

            if (type == ColumnDataType.Visibility)
                SaveViewState();
        }

        /// <summary>
        /// Called whenever a column is reordered.
        /// </summary>
        /// <param name="column"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        void OnColumnReordered(Column column, int from, int to)
        {
            if (!column.visible)
                return;

            if (columnDataMap.TryGetValue(column, out var columnData))
            {
                var index = column.visibleIndex;

                if (index == columns.visibleList.Count() - 1)
                {
                    columnData.control.BringToFront();
                }
                else
                {
                    if (to > from)
                        ++index;
                    columnData.control.PlaceBehind(columnContainer[index]);
                    columnData.resizeHandle.PlaceBehind(resizeHandleContainer[index]);
                }
            }

            UpdateColumnControls();
            SaveViewState();
        }

        /// <summary>
        /// Called whenever a column is resized.
        /// </summary>
        void OnColumnResized(Column column)
        {
            SaveViewState();
        }

        /// <summary>
        /// Called whenever a ContextualMenuPopulateEvent event is received.
        /// </summary>
        /// <param name="evt"></param>
        void OnContextualMenuManipulator(ContextualMenuPopulateEvent evt)
        {
            Column columnUnderMouse = null;
            bool canResizeToFit = this.columns.visibleList.Count() > 0;

            foreach (var column in this.columns.visibleList)
            {
                if (this.columns.stretchMode == Columns.StretchMode.GrowAndFill && canResizeToFit && column.stretchable)
                    canResizeToFit = false;

                if (columnUnderMouse == null)
                {
                    if (columnDataMap.TryGetValue(column, out var data))
                    {
                        if (data.control.layout.Contains(evt.localMousePosition))
                        {
                            columnUnderMouse = column;
                        }
                    }
                }
            }

            evt.menu.AppendAction("Resize To Fit", (a) => ResizeToFit(), canResizeToFit ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();

            foreach (var column in this.columns)
            {
                var title = column.title;

                if (string.IsNullOrEmpty(title))
                    title = column.name;

                if (string.IsNullOrEmpty(title))
                    title = "Unnamed Column_" + column.index;

                evt.menu.AppendAction(title,
                    (a) =>
                    {
                        column.visible = !column.visible;
                    }
                    ,
                    (a) =>
                    {
                        if (!string.IsNullOrEmpty(column.name) && columns.primaryColumnName == column.name)
                            return DropdownMenuAction.Status.Disabled;
                        else if (!column.optional)
                            return DropdownMenuAction.Status.Disabled;
                        else if (column.visible)
                            return DropdownMenuAction.Status.Checked;
                        else
                            return DropdownMenuAction.Status.Normal;
                    });
            }

            contextMenuPopulateEvent?.Invoke(evt, columnUnderMouse);
        }

        /// <summary>
        /// Called whenever the move manipulator is activated.
        /// </summary>
        /// <param name="mover"></param>
        void OnMoveManipulatorActivated(ColumnMover mover)
        {
            resizeHandleContainer.style.display = mover.active ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Called whenever the geometry of the header has changed.
        /// </summary>
        /// <param name="e"></param>
        void OnGeometryChanged(GeometryChangedEvent e)
        {
            if (float.IsNaN(e.newRect.width) || float.IsNaN(e.newRect.height))
                return;

            columnLayout.Dirty();

            if (e.layoutPass > kMaxStableLayoutPassCount)
            {
                ScheduleDoLayout();
            }
            else
            {
                // Force the layout to be computed right away
                DoLayout();
            }
        }

        /// <summary>
        /// Performs layout of columns.
        /// </summary>
        void DoLayout()
        {
            columnLayout.DoLayout(layout.width);
            m_DoLayoutScheduled = false;
        }

        /// <summary>
        /// Called when the geometry of a column control has changed.
        /// </summary>
        /// <param name="evt"></param>
        void OnColumnControlGeometryChanged(GeometryChangedEvent evt)
        {
            if (!(evt.target is MultiColumnHeaderColumn columnControl))
                return;

            var controlData = columnDataMap[columnControl.column];

            controlData.resizeHandle.style.left = columnControl.layout.xMax;

            if (Math.Abs(evt.newRect.width - evt.oldRect.width) < float.Epsilon)
                return;
            RaiseColumnResized(columnContainer.IndexOf(evt.target as VisualElement));
        }

        /// <summary>
        /// Called whenever a column is clicked.
        /// </summary>
        /// <param name="evt"></param>
        void OnColumnClicked(EventBase evt)
        {
            if (!sortingEnabled)
                return;

            var columnControl = evt.currentTarget as MultiColumnHeaderColumn;

            if (columnControl == null)
            {
                return;
            }

            EventModifiers modifiers;

            if (evt is IPointerEvent ptEvt)
                modifiers = ptEvt.modifiers;
            else if (evt is IMouseEvent msEvt)
                modifiers = msEvt.modifiers;
            else
                return;

            var column = columnControl.column;

            if (!column.sortable)
                return;

            var desc = sortDescriptions.FirstOrDefault((d) => (d.column == column || (!string.IsNullOrEmpty(column.name) && d.columnName == column.name) || d.columnIndex == column.index));

            // If a sort description matching the column is found then ...
            if (desc != null)
            {
                // If Shift is pressed then unsort the column
                if (modifiers == EventModifiers.Shift)
                {
                    sortDescriptions.Remove(desc);
                    return;
                }
                // otherwise, flip the sort direction
                desc.direction = desc.direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
            }
            // otherwise, create a new sort description in ascending order
            else
            {
                desc = string.IsNullOrEmpty(column.name) ? new SortColumnDescription(column.index, SortDirection.Ascending) :
                    new SortColumnDescription(column.name, SortDirection.Ascending);
            }

            // If multi sort is not active then clear
            if (modifiers != EventModifiers.Control)
            {
                sortDescriptions.Clear();
            }

            if (!sortDescriptions.Contains(desc))
            {
                sortDescriptions.Add(desc);
            }
        }

        public void ScrollHorizontally(float horizontalOffset)
        {
            transform.position = new Vector3(-horizontalOffset, transform.position.y, transform.position.z);
        }

        void RaiseColumnResized(int columnIndex)
        {
            columnResized?.Invoke(columnIndex, columnContainer[columnIndex].resolvedStyle.width);
        }

        void RaiseColumnSortingChanged()
        {
            ApplyColumnSorting();
            columnSortingChanged?.Invoke();
        }

        void ApplyColumnSorting()
        {
            foreach (var column in columns.visibleList)
            {
                if (!columnDataMap.TryGetValue(column, out var columnData))
                {
                    continue;
                }

                columnData.control.sortOrderLabel = "";
                columnData.control.RemoveFromClassList(MultiColumnHeaderColumn.sortedAscendingUssClassName);
                columnData.control.RemoveFromClassList(MultiColumnHeaderColumn.sortedDescendingUssClassName);
            }

            var sortedColumnDataList = new List<ColumnData>();

            foreach (var sortedColumn in sortedColumns)
            {
                if (columnDataMap.TryGetValue(sortedColumn.column, out var columnData))
                {
                    sortedColumnDataList.Add(columnData);
                    if (sortedColumn.direction == SortDirection.Ascending)
                        columnData.control.AddToClassList(MultiColumnHeaderColumn.sortedAscendingUssClassName);
                    else
                        columnData.control.AddToClassList(MultiColumnHeaderColumn.sortedDescendingUssClassName);
                }
            }

            if (sortedColumnDataList.Count > 1)
            {
                for (int i = 0; i < sortedColumnDataList.Count; ++i)
                {
                    sortedColumnDataList[i].control.sortOrderLabel = (i + 1).ToString();
                }
            }
        }

        void UpdateSortingStatus()
        {
            bool hasSortableColumns = false;

            foreach (var column in columns.visibleList)
            {
                if (!columnDataMap.TryGetValue(column, out var columnData))
                {
                    continue;
                }

                if (sortingEnabled && column.sortable)
                    hasSortableColumns = true;
            }

            foreach (var column in columns.visibleList)
            {
                if (!columnDataMap.TryGetValue(column, out var columnData))
                {
                    continue;
                }

                if (hasSortableColumns)
                {
                    columnData.control.AddToClassList(MultiColumnHeaderColumn.sortableUssClassName);
                }
                else
                {
                    columnData.control.RemoveFromClassList(MultiColumnHeaderColumn.sortableUssClassName);
                }
            }
        }

        internal override void OnViewDataReady()
        {
            try
            {
                m_ApplyingViewState = true;

                base.OnViewDataReady();

                var key = GetFullHierarchicalViewDataKey();

                m_ViewState = GetOrCreateViewData<ViewState>(m_ViewState, key);
                m_ViewState.header = this;
                m_ViewState.Apply();
            }
            finally
            {
                m_ApplyingViewState = false;
            }
        }

        void SaveViewState()
        {
            if (m_ApplyingViewState)
                return;
            SaveViewData();
        }

        public void Dispose()
        {
            sortDescriptions.changed -= UpdateSortedColumns;
            columnLayout.layoutRequested -= ScheduleDoLayout;
            columns.columnAdded -= OnColumnAdded;
            columns.columnRemoved -= OnColumnRemoved;
            columns.columnChanged -= OnColumnChanged;
            columns.columnReordered -= OnColumnReordered;
            columns.columnResized -= OnColumnResized;

            foreach (var data in columnDataMap.Values)
            {
                data.control.clickable.clickedWithEventInfo -= OnColumnClicked;
                data.control.mover.activeChanged -= OnMoveManipulatorActivated;
            }
        }
    }
}
