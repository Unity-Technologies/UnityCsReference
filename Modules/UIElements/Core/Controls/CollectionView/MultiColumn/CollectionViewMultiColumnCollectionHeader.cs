// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements.HierarchyV2;

/// <summary>
/// The multi-column header. It handles resize, sorting, visibility events and sends them to the listening controller.
/// </summary>
[VisibleToOtherModules("UnityEngine.HierarchyModule", "UnityEditor.UIToolkitAuthoringModule")]
internal class CollectionViewMultiColumnCollectionHeader : VisualElement, IDisposable
{
    const int kMaxStableLayoutPassCount = 2; // Beyond this threshold, DoLayout must be performed in the next frame; otherwise, this may lead to Layout instabilities. This is caused by the dependencies between the geometries of the header, the viewport and the content.

    [Serializable]
    class ViewState
    {
        [SerializeField]
        bool m_HasPersistedData;

        /// <summary>
        /// State of columns.
        /// </summary>
        [Serializable]
        struct ColumnState
        {
            public int index;
            public string name;
            public float actualWidth;
            public Length width;
            public bool visible;
        }

        [SerializeField]
        List<SortColumnDescription> m_SortDescriptions = new();

        [SerializeField]
        List<ColumnState> m_OrderedColumnStates = new();

        /// <summary>
        /// Saves the state of the specified header control.
        /// </summary>
        /// <param name="header">The header control of which state to save.</param>
        internal void Save(CollectionViewMultiColumnCollectionHeader header)
        {
            m_SortDescriptions.Clear();
            m_OrderedColumnStates.Clear();

            foreach (var sortDesc in header.sortDescriptions)
            {
                m_SortDescriptions.Add(sortDesc);
            }

            foreach (var column in header.columns.displayList)
            {
                var columnState = new ColumnState() { index = column.index, name = column.name, actualWidth = column.desiredWidth, width = column.width, visible = column.visible };

                m_OrderedColumnStates.Add(columnState);
            }

            m_HasPersistedData = true;
        }

        /// <summary>
        /// Applies the state of the specified header control.
        /// </summary>
        internal void Apply(CollectionViewMultiColumnCollectionHeader header)
        {
            if (!m_HasPersistedData)
                return;

            var minCount = Math.Min(m_OrderedColumnStates.Count, header.columns.Count);
            var nextValidOrderedIndex = 0;

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
    }

    internal class ColumnData
    {
        public CollectionViewMultiColumnHeaderColumn control { get; set; }
        public MultiColumnHeaderColumnResizeHandle resizeHandle { get; set; }
    }

    /// <summary>
    /// Used to determine whether the actual sorting has changed.
    /// </summary>
    struct SortedColumnState
    {
        public SortedColumnState(SortColumnDescription desc, SortDirection dir)
        {
            columnDesc = desc;
            direction = dir;
        }

        public SortColumnDescription columnDesc;
        public SortDirection direction;
    }

    /// <summary>
    /// The USS class name for MultiColumnCollectionHeader elements.
    /// </summary>
    public static readonly UniqueStyleString ussClassName = new("unity-multi-column-header");

    /// <summary>
    /// The USS class name for column container elements of multi column headers.
    /// </summary>
    public static readonly UniqueStyleString columnContainerUssClassName = new("unity-multi-column-header__column-container");

    /// <summary>
    /// The USS class name for handle container elements of multi column headers.
    /// </summary>
    public static readonly UniqueStyleString handleContainerUssClassName = new("unity-multi-column-header__resize-handle-container");

    /// <summary>
    /// The USS class name for frozen-left column elements.
    /// </summary>
    public static readonly UniqueStyleString frozenColumnUssClassName = new("unity-multi-column-header__column--frozen");

    /// <summary>
    /// The USS class name for the scrollable columns clipping area.
    /// </summary>
    public static readonly UniqueStyleString scrollableColumnsClippingContainerUssClassName = new("unity-multi-column-header__scrollable-columns-clipping-container");

    /// <summary>
    /// The USS class name for the scrollable columns container.
    /// </summary>
    public static readonly UniqueStyleString scrollableColumnsContainerUssClassName = new("unity-multi-column-header__scrollable-columns-container");

    /// <summary>
    /// The USS class name for the scrollable columns resize handle container.
    /// </summary>
    public static readonly UniqueStyleString scrollableColumnsResizeHandlerContainerUssClassName = new("unity-multi-column-header__scrollable-columns-resize-handler-container");

    internal static readonly float k_MinScrollableWidth = 0f;

    bool m_ApplyingViewState;
    bool m_DoLayoutScheduled;
    bool m_SortingEnabled;
    bool m_SortingUpdatesTemporarilyDisabled;

    float m_CachedMaxScrollableWidth = -1;

    List<SortColumnDescription> m_SortedColumns;
    List<SortedColumnState> m_OldSortedColumnStates = new();

    SortColumnDescriptions m_SortDescriptions;
    ViewState m_ViewState;

    // Freeze state is stored per-header instance to allow different views to freeze the same columns differently.
    readonly Dictionary<Column, FreezeState> m_ColumnFreezeStates = new();
    readonly VisualElement m_ScrollableColumnsClipArea;
    readonly VisualElement m_ScrollableColumnsContainer;
    readonly VisualElement m_ScrollableColumnsResize;

    internal bool isApplyingViewState => m_ApplyingViewState;

    /// <summary>
    /// Per-Column data.
    /// </summary>
    public Dictionary<Column, ColumnData> columnDataMap { get; } = new();

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
    VisualElement resizeHandleContainer { get; }

    /// <summary>
    /// The effective list of sorted columns.
    /// </summary>
    public IEnumerable<SortColumnDescription> sortedColumns => m_SortedColumns;

    internal IReadOnlyList<SortColumnDescription> sortedColumnReadonly => m_SortedColumns;

    public float maxScrollableWidth
    {
        get
        {
            if (m_CachedMaxScrollableWidth < 0)
            {
                m_CachedMaxScrollableWidth = 0;
                foreach (var child in m_ScrollableColumnsContainer.Children())
                {
                    if (child is CollectionViewMultiColumnHeaderColumn mchc)
                        m_CachedMaxScrollableWidth += mchc.layout.width;
                }
            }
            return m_CachedMaxScrollableWidth;
        }
    }

    public float scrollableWidth => m_ScrollableColumnsClipArea.resolvedStyle.width;

    /// <summary>
    /// The descriptions of sorted columns.
    /// </summary>
    public SortColumnDescriptions sortDescriptions
    {
        get => m_SortDescriptions;
        protected internal set
        {
            m_SortDescriptions = value;
            m_SortDescriptions.changed += UpdateSortedColumns;
            UpdateSortedColumns();
        }
    }

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
    ///  Sent whenever a ContextMenuPopulate event sent allowing user code to add its own actions to the context menu.
    /// </summary>
    internal event Action viewDataRestored;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CollectionViewMultiColumnCollectionHeader() : this(new Columns(), new SortColumnDescriptions(), new List<SortColumnDescription>()) { }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="columns"></param>
    /// <param name="sortDescriptions"></param>
    /// <param name="sortedColumns"></param>
    public CollectionViewMultiColumnCollectionHeader(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
    {
        AddToClassList(ussClassName);

        this.columns = columns;
        m_SortedColumns = sortedColumns;
        this.sortDescriptions = sortDescriptions;

        columnContainer = new VisualElement()
        {
            pickingMode = PickingMode.Ignore
        };
        columnContainer.AddToClassList(columnContainerUssClassName);
        Add(columnContainer);

        m_ScrollableColumnsClipArea = new VisualElement { name = CollectionViewFrozenColumnUtility.ScrollableColumnsClippingContainerName };
        m_ScrollableColumnsClipArea.AddToClassList(scrollableColumnsClippingContainerUssClassName);

        m_ScrollableColumnsContainer = new VisualElement { name = CollectionViewFrozenColumnUtility.ScrollableColumnsContainerName };
        m_ScrollableColumnsContainer.AddToClassList(scrollableColumnsContainerUssClassName);

        columnContainer.Add(m_ScrollableColumnsClipArea);
        m_ScrollableColumnsClipArea.Add(m_ScrollableColumnsContainer);

        // Ensure that the frozen columns' resize handles are on top of the columns.
        resizeHandleContainer = new VisualElement()
        {
            pickingMode = PickingMode.Ignore
        };
        resizeHandleContainer.AddToClassList(handleContainerUssClassName);
        resizeHandleContainer.StretchToParentSize();
        Add(resizeHandleContainer);

        m_ScrollableColumnsResize = new VisualElement {
            name = CollectionViewFrozenColumnUtility.ScrollableColumnsResizeHandlerContainerName,
            pickingMode = PickingMode.Ignore
        };
        m_ScrollableColumnsResize.AddToClassList(scrollableColumnsResizeHandlerContainerUssClassName);
        m_ScrollableColumnsResize.StretchToParentSize();
        m_ScrollableColumnsClipArea.Add(m_ScrollableColumnsResize);

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

    public void SetColumnFreezeState(Column column, FreezeState freezeState)
    {
        m_ColumnFreezeStates[column] = freezeState;

        ReorderColumnVisuals();
        ScheduleDoLayout();
    }

    public FreezeState GetColumnFreezeState(Column column)
    {
        if (m_ColumnFreezeStates.TryGetValue(column, out var freezeState))
            return freezeState;
        return FreezeState.None;
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
    public void ResizeToFit()
    {
        columnLayout.ResizeToFit(layout.width);
    }

    /// <summary>
    /// Updates the effective list of sorted columns.
    /// </summary>
    void UpdateSortedColumns()
    {
        if (m_SortingUpdatesTemporarilyDisabled)
            return;

        using (ListPool<SortedColumnState>.Get(out var sortedColumnStates))
        {
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
                        sortedColumnStates.Add(new SortedColumnState(desc, desc.direction));
                    }
                    else
                    {
                        desc.column = null;
                    }
                }
            }

            if (m_OldSortedColumnStates.Count == sortedColumnStates.Count)
            {
                var areEqual = true;
                for (var i = 0; i < m_OldSortedColumnStates.Count; i++)
                {
                    var old = m_OldSortedColumnStates[i];
                    var current = sortedColumnStates[i];

                    if (old.columnDesc != current.columnDesc || old.direction != current.direction)
                    {
                        areEqual = false;
                        break;
                    }
                }

                if (areEqual)
                    return;
            }

            m_SortedColumns.Clear();
            foreach (var state in sortedColumnStates)
            {
                m_SortedColumns.Add(state.columnDesc);
            }
            m_OldSortedColumnStates.CopyFrom(sortedColumnStates);
        }

        SaveViewState();
        RaiseColumnSortingChanged();
    }

    /// <summary>
    /// Update the column controls and resize handles from the columns.
    /// </summary>
    void UpdateColumnControls()
    {
        var hasStretch = false;
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
            // BUT: don't hide if the last column is frozen
            if (columns.stretchMode == Columns.StretchMode.GrowAndFill && columnDataMap.TryGetValue(lastVisibleColumn, out var columnData) && GetColumnFreezeState(lastVisibleColumn) == FreezeState.None)
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
        if (columnDataMap.ContainsKey(column))
            return;

        var columnElement = new CollectionViewMultiColumnHeaderColumn(column);
        var resizeHandle = new MultiColumnHeaderColumnResizeHandle();

        columnElement.RegisterCallback<GeometryChangedEvent>(OnColumnControlGeometryChanged);
        columnElement.clickable.clickedWithEventInfo += OnColumnClicked;
        columnElement.mover.activeChanged += OnMoveManipulatorActivated;

        resizeHandle.dragArea.AddManipulator(new CollectionViewColumnResizer(column));

        columnDataMap[column] = new ColumnData
        {
            control = columnElement,
            resizeHandle = resizeHandle
        };

        if (column.visible)
        {
            // Use existing ReorderColumnVisuals to handle placement
            ReorderColumnVisuals();
        }
        else
        {
            OnColumnRemoved(column);
        }

        InvalidateScrollableWidthCache();
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
        if (!columnDataMap.TryGetValue(column, out var data))
            return;

        CleanupColumnData(data);
        columnDataMap.Remove(column);

        InvalidateScrollableWidthCache();
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

            ApplyColumnSorting();
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
        if (!column.visible || from == to)
            return;

        ReorderColumnVisuals();
        UpdateColumnControls();
        SaveViewState();
    }

    void ReorderColumnVisuals()
    {
        ClearColumnContainers();

        using (ListPool<Column>.Get(out var frozenLeftColumns))
        using (ListPool<Column>.Get(out var freeColumns))
        using (ListPool<Column>.Get(out var frozenRightColumns))
        {
            SeparateColumnsByFreezeState(frozenLeftColumns, freeColumns, frozenRightColumns);
            AddColumnsToContainers(frozenLeftColumns, freeColumns, frozenRightColumns);
        }
    }

    void ClearColumnContainers()
    {
        m_ScrollableColumnsContainer.Clear();
        m_ScrollableColumnsResize.Clear();

        // Remove all frozen columns from hierarchy
        foreach (var kvp in columnDataMap)
        {
            var freezeState = GetColumnFreezeState(kvp.Key);
            if (freezeState != FreezeState.None)
            {
                kvp.Value.control.RemoveFromHierarchy();
                kvp.Value.resizeHandle.RemoveFromHierarchy();
            }
        }
    }

    void SeparateColumnsByFreezeState(List<Column> frozenLeft, List<Column> free, List<Column> frozenRight)
    {
        foreach (var col in columns.visibleList)
        {
            var freezeState = GetColumnFreezeState(col);

            if (freezeState == FreezeState.FreezeLeft)
                frozenLeft.Add(col);
            else if (freezeState == FreezeState.None)
                free.Add(col);
            else
                frozenRight.Add(col);
        }
    }

    void AddColumnsToContainers(List<Column> frozenLeftColumns, List<Column> freeColumns, List<Column> frozenRightColumns)
    {
        var freezeLeftIndex = 0;

        // Add frozen left columns first
        foreach (var col in frozenLeftColumns)
        {
            if (columnDataMap.TryGetValue(col, out var colData))
            {
                var insertIndex = columnContainer.IndexOf(m_ScrollableColumnsClipArea);
                columnContainer.Insert(insertIndex + freezeLeftIndex, colData.control);
                resizeHandleContainer.Insert(insertIndex + freezeLeftIndex, colData.resizeHandle);
                freezeLeftIndex++;
            }
        }

        // Add scrollable columns
        foreach (var col in freeColumns)
        {
            if (columnDataMap.TryGetValue(col, out var colData))
            {
                m_ScrollableColumnsContainer.Add(colData.control);
                m_ScrollableColumnsResize.Add(colData.resizeHandle);
            }
        }

        // Add frozen right columns
        foreach (var col in frozenRightColumns)
        {
            if (columnDataMap.TryGetValue(col, out var colData))
            {
                columnContainer.Add(colData.control);
                resizeHandleContainer.Add(colData.resizeHandle);
            }
        }
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
        var canResizeToFit = columns.visibleList.Count > 0;

        foreach (var column in columns.visibleList)
        {
            if (columns.stretchMode == Columns.StretchMode.GrowAndFill && canResizeToFit && column.stretchable)
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

        foreach (var column in columns)
        {
            var title = column.title ?? column.name;
            if (string.IsNullOrEmpty(title))
                continue;

            evt.menu.AppendAction(title,
                (_) =>
                {
                    column.visible = !column.visible;
                }
                ,
                (_) =>
                {
                    if (!string.IsNullOrEmpty(column.name) && columns.primaryColumnName == column.name)
                        return DropdownMenuAction.Status.Disabled;
                    if (!column.optional)
                        return DropdownMenuAction.Status.Disabled;

                    return column.visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                });
        }

        contextMenuPopulateEvent?.Invoke(evt, columnUnderMouse);
    }

    /// <summary>
    /// Called whenever the move manipulator is activated.
    /// </summary>
    /// <param name="mover"></param>
    void OnMoveManipulatorActivated(CollectionViewColumnMover mover)
    {
        var display = mover.active ? DisplayStyle.None : DisplayStyle.Flex;
        resizeHandleContainer.style.display = display;
        m_ScrollableColumnsResize.style.display = display;

        // When mover deactivates, refresh all resize handle positions
        if (!mover.active)
        {
            RefreshResizeHandlePositions();
        }
    }

    void RefreshResizeHandlePositions()
    {
        foreach (var kvp in columnDataMap)
        {
            var handlePosition = CalculateResizeHandlePosition(kvp.Key, kvp.Value);
            kvp.Value.resizeHandle.style.left = handlePosition;
        }
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
        columnLayout.DoLayout(resolvedStyle.width);

        var frozenWidth = 0f;

        foreach (var col in columns.visibleList)
        {
            var freezeState = GetColumnFreezeState(col);

            if (freezeState != FreezeState.None)
            {
                var width = columnLayout.GetDesiredWidth(col);
                frozenWidth += width;

                // Set explicit width and prevent shrinking/growing
                if (columnDataMap.TryGetValue(col, out var colData))
                {
                    colData.control.AddToClassList(frozenColumnUssClassName);
                    colData.control.style.width = width;
                }
            }
            else
            {
                if (columnDataMap.TryGetValue(col, out var colData))
                {
                    colData.control.RemoveFromClassList(frozenColumnUssClassName);
                }
            }
        }

        m_ScrollableColumnsClipArea.style.maxWidth = Mathf.Max(k_MinScrollableWidth, resolvedStyle.width - frozenWidth);
        m_DoLayoutScheduled = false;

        if (sortingEnabled)
            UpdateSortedColumns();

        InvalidateScrollableWidthCache();
    }

    /// <summary>
    /// Called when the geometry of a column control has changed.
    /// </summary>
    /// <param name="evt"></param>
    void OnColumnControlGeometryChanged(GeometryChangedEvent evt)
    {
        if (evt.target is not CollectionViewMultiColumnHeaderColumn columnControl)
            return;

        if (columnControl.panel == null)
            return;

        if (!columnDataMap.TryGetValue(columnControl.column, out var controlData))
            return;

        var freezeState = GetColumnFreezeState(columnControl.column);
        var handlePosition = CalculateResizeHandlePosition(columnControl.column, controlData);
        controlData.resizeHandle.style.left = handlePosition;

        if (Math.Abs(evt.newRect.width - evt.oldRect.width) < float.Epsilon)
            return;

        InvalidateScrollableWidthCache();

        // For frozen columns, just schedule a layout instead of partial updates
        if (freezeState != FreezeState.None)
        {
            ScheduleDoLayout();

            // When frozen-left columns resize, update scrollable column handle positions
            // because they are positioned relative to the scrollable container which has shifted
            if (freezeState == FreezeState.FreezeLeft)
            {
                CollectionViewFrozenColumnUtility.UpdateScrollableHandlePositions(this, m_ScrollableColumnsResize);
            }
        }

        RaiseColumnResized(columns.IndexOf(columnControl.column));
    }

    /// <summary>
    /// Called whenever a column is clicked.
    /// </summary>
    /// <param name="evt"></param>
    void OnColumnClicked(EventBase evt)
    {
        if (!sortingEnabled)
            return;

        var columnControl = evt.currentTarget as CollectionViewMultiColumnHeaderColumn;

        if (columnControl == null || !columnControl.column.sortable)
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

        m_SortingUpdatesTemporarilyDisabled = true;

        try
        {
            UpdateSortColumnDescriptionsOnClick(columnControl.column, modifiers);
        }
        finally
        {
            m_SortingUpdatesTemporarilyDisabled = false;
        }
        UpdateSortedColumns();
    }

    /// <summary>
    /// Updates the list of sort column descriptions upon click on a column.
    /// </summary>
    /// <param name="column">The clicked column</param>
    /// <param name="modifiers">The modifiers of the pointer event</param>
    void UpdateSortColumnDescriptionsOnClick(Column column, EventModifiers modifiers)
    {
        SortColumnDescription desc = null;
        foreach (var d in sortDescriptions)
        {
            if (d.column == column || (!string.IsNullOrEmpty(column.name) && d.columnName == column.name) || d.columnIndex == column.index)
            {
                desc = d;
                break;
            }
        }

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
        EventModifiers multiSortingModifier = EventModifiers.Control;

        if (Application.platform is RuntimePlatform.OSXEditor or RuntimePlatform.OSXPlayer)
        {
            multiSortingModifier = EventModifiers.Command;
        }

        if (modifiers != multiSortingModifier)
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
        m_ScrollableColumnsContainer.style.translate = new Vector3(horizontalOffset, 0, 0);
        m_ScrollableColumnsResize.style.translate = new Vector3(horizontalOffset, 0, 0);
    }

    void RaiseColumnResized(int columnIndex)
    {
        if (columnIndex != -1 && columnDataMap.TryGetValue(columns[columnIndex], out var colData))
        {
            columnResized?.Invoke(columnIndex, colData.control.resolvedStyle.width);
        }
    }

    void RaiseColumnSortingChanged()
    {
        ApplyColumnSorting();
        if (!m_ApplyingViewState)
            columnSortingChanged?.Invoke();
    }

    void ApplyColumnSorting()
    {
        foreach (var column in columns.visibleList)
        {
            if (!columnDataMap.TryGetValue(column, out var columnData))
                continue;

            columnData.control.sortOrderLabel = "";
            columnData.control.RemoveFromClassList(MultiColumnHeaderColumn.sortedAscendingUssClassNameUnique);
            columnData.control.RemoveFromClassList(MultiColumnHeaderColumn.sortedDescendingUssClassNameUnique);
        }

        using (ListPool<ColumnData>.Get(out var sortedColumnDataList))
        {
            foreach (var sortedColumn in sortedColumns)
            {
                if (columnDataMap.TryGetValue(sortedColumn.column, out var columnData))
                {
                    sortedColumnDataList.Add(columnData);
                    if (sortedColumn.direction == SortDirection.Ascending)
                        columnData.control.AddToClassList(CollectionViewMultiColumnHeaderColumn.sortedAscendingUssClassNameUnique);
                    else
                        columnData.control.AddToClassList(CollectionViewMultiColumnHeaderColumn.sortedDescendingUssClassNameUnique);
                }
            }

            if (sortedColumnDataList.Count > 1)
            {
                for (var i = 0; i < sortedColumnDataList.Count; ++i)
                {
                    sortedColumnDataList[i].control.sortOrderLabel = (i + 1).ToString();
                }
            }
        }
    }

    void UpdateSortingStatus()
    {
        var hasSortableColumns = false;

        // Perform a first pass to determine if any column is sortable
        if (sortingEnabled)
        {
            foreach (var column in columns.visibleList)
            {
                if (!column.sortable)
                    continue;

                hasSortableColumns = true;
                break;
            }
        }

        // On second pass, update all columns based on result
        foreach (var column in columns.visibleList)
        {
            if (!columnDataMap.TryGetValue(column, out var columnData))
                continue;

            if (hasSortableColumns)
                columnData.control.AddToClassList(CollectionViewMultiColumnHeaderColumn.sortableUssClassNameUnique);
            else
                columnData.control.RemoveFromClassList(CollectionViewMultiColumnHeaderColumn.sortableUssClassNameUnique);
        }
    }

    void InvalidateScrollableWidthCache()
    {
        m_CachedMaxScrollableWidth = -1;
    }

    float CalculateResizeHandlePosition(Column column, ColumnData colData)
    {
        return CollectionViewFrozenColumnUtility.CalculateResizeHandlePosition(this, column, colData, m_ScrollableColumnsResize);
    }

    internal override void OnViewDataReady()
    {
        try
        {
            m_ApplyingViewState = true;

            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();

            m_ViewState = GetOrCreateViewData<ViewState>(m_ViewState, key);
            m_ViewState.Apply(this);

            viewDataRestored?.Invoke();
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

        m_ViewState?.Save(this);
        SaveViewData();
    }

    void CleanupColumnData(ColumnData data)
    {
        data.control.UnregisterCallback<GeometryChangedEvent>(OnColumnControlGeometryChanged);
        data.control.clickable.clickedWithEventInfo -= OnColumnClicked;
        data.control.mover.activeChanged -= OnMoveManipulatorActivated;
        data.control.RemoveFromHierarchy();
        data.control.Dispose();
        data.resizeHandle.RemoveFromHierarchy();
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
            CleanupColumnData(data);
        }

        columnDataMap.Clear();
    }
}
