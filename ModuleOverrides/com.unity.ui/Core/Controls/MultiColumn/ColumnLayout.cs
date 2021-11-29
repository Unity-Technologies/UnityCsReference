// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Lays out columns within their containers.
    /// </summary>
    class ColumnLayout
    {
        List<Column> m_StretchableColumns = new List<Column>();
        List<Column> m_FixedColumns = new List<Column>();
        Columns m_Columns;
        float m_ColumnsWidth = 0;
        bool m_ColumnsWidthDirty = true;
        float m_MaxColumnsWidth = 0;
        float m_MinColumnsWidth = 0;
        bool m_IsDirty = false;
        float m_PreviousWidth = float.NaN;

        // Drag info
        bool m_DragResizeInPreviewMode;
        bool m_DragResizing = false;
        float m_DragStartPos;
        float m_DragLastPos;
        float m_DragInitialColumnWidth;
        List<Column> m_DragStretchableColumns = new List<Column>();
        List<Column> m_DragFixedColumns = new List<Column>();
        Dictionary<Column, float> m_PreviewDesiredWidths;

        /// <summary>
        /// The column collection managed by this layout.
        /// </summary>
        public Columns columns => m_Columns;

        /// <summary>
        /// Indicates whether the layout is dirty and needs to be updated.
        ///</summary>
        public bool isDirty => m_IsDirty;

        /// <summary>
        /// The total width of all columns.
        /// </summary>
        public float columnsWidth
        {
            get
            {
                if (m_ColumnsWidthDirty)
                {
                    m_ColumnsWidth = 0;
                    foreach (var column in m_Columns.visibleList)
                    {
                        m_ColumnsWidth += column.desiredWidth;
                    }
                    m_ColumnsWidthDirty = false;
                }
                return m_ColumnsWidth;
            }
        }

        /// <summary>
        /// The total minimum width of all columns.
        /// </summary>
        public float minColumnsWidth => m_MinColumnsWidth;

        /// <summary>
        /// The total maximum width of all columns.
        /// </summary>
        public float maxColumnsWidth => m_MaxColumnsWidth;

        /// <summary>
        /// Indicates whether the layout contains stretchable columns.
        /// </summary>
        public bool hasStretchableColumns => m_StretchableColumns.Count > 0;

        /// <summary>
        /// Called whenever the layout needs to be performed.
        /// </summary>
        public event Action layoutRequested;

        /// <summary>
        /// Constructs with a collection of columns.
        /// </summary>
        /// <param name="columns">The collection of columns managed by the layout.</param>
        public ColumnLayout(Columns columns)
        {
            m_Columns = columns;
            for (var i = 0; i < columns.Count; ++i)
                OnColumnAdded(columns[i], i);
            columns.columnAdded += OnColumnAdded;
            columns.columnRemoved += OnColumnRemoved;
            columns.columnReordered += OnColumnReordered;
        }

        /// <summary>
        /// Invalidates the current layout and requests a new layout calculation.
        /// </summary>
        public void Dirty()
        {
            if (m_IsDirty)
                return;
            m_IsDirty = true;
            ClearCache();
            layoutRequested?.Invoke();
        }

        /// <summary>
        /// Called when a column is added to the layout.
        /// </summary>
        /// <param name="column">The added column.</param>
        /// <param name="index">The index where the column was inserted.</param>
        void OnColumnAdded(Column column, int index)
        {
            column.changed += OnColumnChanged;
            column.resized += OnColumnResized;
            Dirty();
        }

        /// <summary>
        /// Called when a column is removed from the layout.
        /// </summary>
        /// <param name="column">The removed column.</param>
        void OnColumnRemoved(Column column)
        {
            column.changed -= OnColumnChanged;
            column.resized -= OnColumnResized;
            Dirty();
        }

        /// <summary>
        /// Called when columns get ordered.
        /// </summary>
        /// <param name="column">The reordered column.</param>
        /// <param name="from">The previous display index of the column.</param>
        /// <param name="to">The new display index of the column.</param>
        void OnColumnReordered(Column column, int from, int to)
        {
            Dirty();
        }

        bool RequiresLayoutUpdate(ColumnDataType type)
        {
            switch (type)
            {
                case ColumnDataType.CellTemplate:
                case ColumnDataType.HeaderTemplate:
                case ColumnDataType.MaxWidth:
                case ColumnDataType.MinWidth:
                case ColumnDataType.Width:
                case ColumnDataType.Stretchable:
                case ColumnDataType.Visibility:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Called whenever a column has changed.
        /// </summary>
        /// <param name="column">The column that has changed.</param>
        /// <param name="type">The data that has changed.</param>
        void OnColumnChanged(Column column, ColumnDataType type)
        {
            // Do not dirty the layout if the user is resizing columns interactively
            if (m_DragResizing || !RequiresLayoutUpdate(type))
                return;
            Dirty();
        }

        /// <summary>
        /// Called when a column is resized.
        /// </summary>
        /// <param name="column">The resized column.</param>
        void OnColumnResized(Column column)
        {
            m_ColumnsWidthDirty = true;
        }

        static bool IsClamped(float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Performs layouting of all columns using the specified containing width.
        /// </summary>
        /// <param name="width">The width of the container.</param>
        public void DoLayout(float width)
        {
            if (m_IsDirty)
                UpdateCache();

            var totalColumnsWidth = 0f;
            var fixedColumnsWidth = 0f;
            var totalStretchableWidth = 0f;
            var stretchableColumnsWithInvalidWidth = new List<Column>();
            var stretchableColumnsWithValidWidth = new List<Column>();

            /// step 1 - Compute the width of the fixed columns with invalid (not yet computed) width
            /// and adjust clamp widths if necessary
            foreach (var column in m_Columns)
            {
                if (!column.visible)
                    continue;

                // If the actual width is not yet computed then set it
                if (float.IsNaN(column.desiredWidth))
                {
                    // Ignore stretchable columns with invalid width for now as they need particular computation
                    if (m_Columns.stretchMode == Columns.StretchMode.GrowAndFill && column.stretchable)
                    {
                        stretchableColumnsWithInvalidWidth.Add(column);
                        continue;
                    }

                    column.desiredWidth = Mathf.Clamp(column.width.value, column.minWidth.value, column.maxWidth.value);
                }
                else
                {
                    // Ignore stretchable columns with invalid width for now as they need particular computation
                    if (m_Columns.stretchMode == Columns.StretchMode.GrowAndFill && column.stretchable)
                    {
                        stretchableColumnsWithValidWidth.Add(column);
                        totalStretchableWidth += GetDesiredWidth(column);
                    }

                    // If the actual width is no longer clamped with the min and max width then clamp it
                    if (!IsClamped(column.desiredWidth, column.minWidth.value, column.maxWidth.value))
                    {
                        column.desiredWidth = Mathf.Clamp(column.width.value, column.minWidth.value, column.maxWidth.value);
                    }
                }

                if (!column.stretchable)
                    fixedColumnsWidth += column.desiredWidth;
                totalColumnsWidth += column.desiredWidth;
            }

            /// step 2 - Compute the width of the stretchable columns with invalid width using the available remaining space
            if (stretchableColumnsWithInvalidWidth.Count > 0)
            {
                float availableWidth = Math.Max(0, width - fixedColumnsWidth);
                int count = m_StretchableColumns.Count;

                // Sort the columns ensure that the columns with the highest margins of growth are treated last
                stretchableColumnsWithInvalidWidth.Sort((c1, c2) => c1.maxWidth.value.CompareTo(c2.maxWidth.value));

                foreach (var column in stretchableColumnsWithInvalidWidth)
                {
                    var widthPerColumn = availableWidth / count;

                    column.desiredWidth = Mathf.Clamp(widthPerColumn, column.minWidth.value, column.maxWidth.value);
                    availableWidth = Math.Max(0, availableWidth - column.desiredWidth);
                    --count;
                }

                // Sort the columns ensure that the columns with the highest margins of growth are treated last
                stretchableColumnsWithValidWidth.Sort((c1, c2) => c1.maxWidth.value.CompareTo(c2.maxWidth.value));

                // Dispatch the remaining space proportionally among the stretchable column with already computed width
                foreach (var column in stretchableColumnsWithValidWidth)
                {
                    var oldWidth = GetDesiredWidth(column);
                    var ratio = oldWidth / totalStretchableWidth;
                    var widthPerColumn = availableWidth * ratio;

                    column.desiredWidth = Mathf.Clamp(widthPerColumn, column.minWidth.value, column.maxWidth.value);
                    availableWidth = Math.Max(0, availableWidth - column.desiredWidth);
                    totalStretchableWidth -= oldWidth;
                    --count;
                }
            }

            if (hasStretchableColumns)
            {
                // Ensure that we do not exceed the combined min and max width of columns
                float deltaWidth = 0;

                if (m_Columns.stretchMode == Columns.StretchMode.Grow)
                {
                    if (!float.IsNaN(m_PreviousWidth))
                        deltaWidth = m_PreviousWidth - width;
                }
                else
                    deltaWidth = columnsWidth - Mathf.Clamp(width, minColumnsWidth, maxColumnsWidth);

                if (deltaWidth != 0)
                {
                    StretchResizeColumns(m_StretchableColumns, m_FixedColumns, ref deltaWidth, false);
                }
            }
            m_PreviousWidth = width;
            m_IsDirty = false;
        }

        /// <summary>
        /// Resizes the specified columns to fill the specified delta width.
        /// </summary>
        /// <param name="stretchableColumns">The list of stretchable columns</param>
        /// <param name="fixedColumns">The list of fixed columns.</param>
        /// <param name="delta">The delta.</param>
        /// <param name="resizeToFit">Indicated whether this method is called by ResizeToFit.</param>
        public void StretchResizeColumns(List<Column> stretchableColumns, List<Column> fixedColumns, ref float delta, bool resizeToFit)
        {
            if (stretchableColumns.Count == 0 && fixedColumns.Count == 0)
                return;

            int count = stretchableColumns.Count;
            float distributedDelta = Math.Abs(delta);

            if (delta > 0)
            {
                if (!resizeToFit)
                {
                    // Step 1 - Start distributing the delta to the fixed columns to their desired width from the right to the left
                    for (int i = fixedColumns.Count - 1; i >= 0; --i)
                    {
                        var fixedColumn = fixedColumns[i];
                        var appliedDelta = 0f;
                        var boundWidth = Mathf.Clamp(fixedColumn.width.value, fixedColumn.minWidth.value, fixedColumn.maxWidth.value);

                        // If the width is not set then ignore it
                        if (fixedColumn.width.value == 0)
                            continue;

                        if (GetDesiredWidth(fixedColumn) > boundWidth)
                            appliedDelta = Math.Min(distributedDelta, Math.Abs(GetDesiredWidth(fixedColumn) - boundWidth));

                        if (appliedDelta > 0f)
                            ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) - appliedDelta, true);

                        distributedDelta -= appliedDelta;

                        // If there is no delta to distribute then stop
                        if (distributedDelta <= 0)
                            break;
                    }
                }

                // Step 2 - Start distributing the remaining delta amount to the stretchable columns proportionally to their current width
                if (distributedDelta > 0)
                {
                    // Sort the strectable columns to treat the columns with the smallest min with last
                    stretchableColumns.Sort((c1, c2) => c2.minWidth.value.CompareTo(c1.minWidth.value));
                    var totalStretchableWidth = 0f;

                    stretchableColumns.ForEach((c) => totalStretchableWidth += GetDesiredWidth(c));

                    for (var i = 0; i < stretchableColumns.Count; ++i)
                    {
                        var stretchableColumn = stretchableColumns[i];
                        var oldWidth = GetDesiredWidth(stretchableColumn);
                        var ratio = GetDesiredWidth(stretchableColumn) / totalStretchableWidth;
                        var deltaPerColumn = distributedDelta * ratio;

                        // Ignore the stretcable columns if they reach their minimum limit
                        var appliedDelta = 0f;
                        if (GetDesiredWidth(stretchableColumn) > stretchableColumn.minWidth.value)
                            appliedDelta = Math.Min(deltaPerColumn, GetDesiredWidth(stretchableColumn) - stretchableColumn.minWidth.value);

                        if (appliedDelta > 0f)
                            ResizeColumn(stretchableColumn, GetDesiredWidth(stretchableColumn) - appliedDelta, !resizeToFit);

                        totalStretchableWidth -= oldWidth;
                        distributedDelta -= appliedDelta;

                        // If there is no delta to distribute then stop
                        if (distributedDelta <= 0)
                            break;
                    }
                }

                if (resizeToFit)
                {
                    // Start distributing the remaining delta amount to the fixed columns proportionally to their current width
                    if (distributedDelta > 0)
                    {
                        // Sort the strectable columns to treat the columns with the smallest min with last
                        fixedColumns.Sort((c1, c2) => c2.minWidth.value.CompareTo(c1.minWidth.value));
                        var totalFixedWidth = 0f;

                        fixedColumns.ForEach((c) => totalFixedWidth += GetDesiredWidth(c));

                        for (var i = 0; i < fixedColumns.Count; ++i)
                        {
                            var fixedColumn = fixedColumns[i];
                            var oldWidth = GetDesiredWidth(fixedColumn);
                            var ratio = GetDesiredWidth(fixedColumn) / totalFixedWidth;
                            var deltaPerColumn = distributedDelta * ratio;

                            // Ignore the stretcable columns if they reach their minimum limit
                            var appliedDelta = 0f;
                            if (GetDesiredWidth(fixedColumn) > fixedColumn.minWidth.value)
                                appliedDelta = Math.Min(deltaPerColumn, GetDesiredWidth(fixedColumn) - fixedColumn.minWidth.value);

                            if (appliedDelta > 0f)
                                ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) - appliedDelta, false);

                            totalFixedWidth -= oldWidth;
                            distributedDelta -= appliedDelta;

                            // If there is no delta to distribute then stop
                            if (distributedDelta <= 0)
                                break;
                        }
                    }
                }
                else
                {
                    // step 3 - If there is still some delta to distribute then distribute it to the fixed columns
                    // to their minimum limit from the right to the left
                    if (distributedDelta > 0)
                    {
                        // Start distributing the delta amount the nonstretchable columns
                        for (int i = fixedColumns.Count - 1; i >= 0; --i)
                        {
                            var fixedColumn = fixedColumns[i];
                            // Ignore the stretcable columns if they reach their limit
                            var appliedDelta = 0f;
                            if (GetDesiredWidth(fixedColumn) > fixedColumn.minWidth.value)
                                appliedDelta = Math.Min(distributedDelta, GetDesiredWidth(fixedColumn) - fixedColumn.minWidth.value);
                            if (appliedDelta > 0f)
                                ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) - appliedDelta, true);
                            distributedDelta -= appliedDelta;

                            // If there is no delta to distribute then stop
                            if (distributedDelta <= 0)
                                break;
                        }
                    }
                }

                // Removed the extra delta that could not be distrubuted.
                delta = Math.Max(0, delta - distributedDelta);
            }
            else
            {
                if (!resizeToFit)
                {
                    // Start distributing the delta to the fixed columns to their max or desired width from the left to the right
                    for (int i = 0; i < fixedColumns.Count; ++i)
                    {
                        var fixedColumn = fixedColumns[i];
                        // Ignore the stretchable columns if they reach their limit
                        var appliedDelta = 0f;
                        var clampedWidth = Mathf.Clamp(fixedColumn.width.value, fixedColumn.minWidth.value, fixedColumn.maxWidth.value);

                        if (GetDesiredWidth(fixedColumn) < clampedWidth)
                            appliedDelta = Math.Min(distributedDelta, Math.Abs(clampedWidth - GetDesiredWidth(fixedColumn)));

                        if (appliedDelta > 0f)
                            ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) + appliedDelta, true);

                        distributedDelta -= appliedDelta;

                        // If there is no delta to distribute then stop
                        if (distributedDelta <= 0)
                            break;
                    }
                }

                // Step 2 - Start distributing the remaining delta amount to the stretchable columns proportionally to their max value
                if (distributedDelta > 0)
                {
                    // Sort the stretchable columns to treat the columns with the greatest margins to grow last
                    stretchableColumns.Sort((c1, c2) => c1.maxWidth.value.CompareTo(c2.maxWidth.value));

                    var totalStretchableWidth = 0f;

                    stretchableColumns.ForEach((c) => totalStretchableWidth += GetDesiredWidth(c));

                    // Start distributing the delta amount the stretchable columns
                    for (var i = 0; i < stretchableColumns.Count; ++i)
                    {
                        var stretchableColumn = stretchableColumns[i];
                        var oldWidth = GetDesiredWidth(stretchableColumn);
                        var ratio = GetDesiredWidth(stretchableColumn) / totalStretchableWidth;
                        var deltaPerColumn = distributedDelta * ratio;

                        // Ignore the stretcable columns if they reach their limit
                        var appliedDelta = 0f;
                        if (GetDesiredWidth(stretchableColumn) < stretchableColumn.maxWidth.value)
                            appliedDelta = Math.Min(deltaPerColumn, stretchableColumn.maxWidth.value - GetDesiredWidth(stretchableColumn));
                        if (appliedDelta > 0f)
                            ResizeColumn(stretchableColumn, GetDesiredWidth(stretchableColumn) + appliedDelta, !resizeToFit);

                        totalStretchableWidth -= oldWidth;
                        distributedDelta -= appliedDelta;

                        // If there is no delta to distribute then stop
                        if (distributedDelta <= 0)
                            break;
                    }
                }

                if (resizeToFit)
                {
                    // Sort the fixed columns to treat the columns with the greatest margins to grow last
                    fixedColumns.Sort((c1, c2) => c1.maxWidth.value.CompareTo(c2.maxWidth.value));

                    var totalFixedWidth = 0f;

                    fixedColumns.ForEach((c) => totalFixedWidth += GetDesiredWidth(c));

                    // Start distributing the delta amount the fixed columns
                    for (var i = 0; i < fixedColumns.Count; ++i)
                    {
                        var fixedColumn = fixedColumns[i];
                        var oldWidth = GetDesiredWidth(fixedColumn);
                        var ratio = GetDesiredWidth(fixedColumn) / totalFixedWidth;
                        var deltaPerColumn = distributedDelta * ratio;

                        // Ignore the stretcable columns if they reach their limit
                        var appliedDelta = 0f;
                        if (GetDesiredWidth(fixedColumn) < fixedColumn.maxWidth.value)
                            appliedDelta = Math.Min(deltaPerColumn, fixedColumn.maxWidth.value - GetDesiredWidth(fixedColumn));
                        if (appliedDelta > 0f)
                            ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) + appliedDelta, false);

                        totalFixedWidth -= oldWidth;
                        distributedDelta -= appliedDelta;

                        // If there is no delta to distribute then stop
                        if (distributedDelta <= 0)
                            break;
                    }
                }
                else
                {
                    // step 3 - If there is still some delta to distribute then distribute it to the fixed columns
                    // to their maximum limit from the left to the right
                    if (distributedDelta > 0)
                    {
                        for (int i = 0; i < fixedColumns.Count; ++i)
                        {
                            var fixedColumn = fixedColumns[i];
                            // Ignore the stretcable columns if they reach their limit
                            var appliedDelta = 0f;
                            var nextMaxWidth = fixedColumn.maxWidth.value;

                            if (GetDesiredWidth(fixedColumn) < nextMaxWidth)
                                appliedDelta = Math.Min(distributedDelta, Math.Abs(nextMaxWidth - GetDesiredWidth(fixedColumn)));

                            if (appliedDelta > 0f)
                                ResizeColumn(fixedColumn, GetDesiredWidth(fixedColumn) + appliedDelta, true);

                            distributedDelta -= appliedDelta;

                            // If there is no delta to distribute then stop
                            if (distributedDelta <= 0)
                                break;
                        }
                    }
                }

                delta = delta + distributedDelta;
            }
        }

        /// <summary>
        ///  Resizes all the columns to fit the specified width, starting by resizing stretchable columns and ending with fixed columns.
        /// </summary>
        /// <param name="width">The width of the container.</param>
        public void ResizeToFit(float width)
        {
            float delta = columnsWidth - Mathf.Clamp(width, minColumnsWidth, maxColumnsWidth);

            var stretchableColumnsToFit = new List<Column>(m_StretchableColumns); // Copy as the original list gets cleared by Dirty()
            var fixedColumnsToFit = new List<Column>(m_FixedColumns); // Copy as the original list gets cleared by Dirty()

            StretchResizeColumns(stretchableColumnsToFit, fixedColumnsToFit, ref delta, true);
        }

        /// <summary>
        /// Sets the desired width and optionally the width (when manually resized by user) of the specified column
        /// </summary>
        /// <param name="column">The column to resize.</param>
        /// <param name="width">The new width of the column.</param>
        /// <param name="setDesiredWidthOnly">Indicated whether only the desired width should be changed.</param>
        void ResizeColumn(Column column, float width, bool setDesiredWidthOnly = false)
        {
            if (m_DragResizeInPreviewMode)
            {
                m_PreviewDesiredWidths[column] = width;
            }
            else
            {
                if (!setDesiredWidthOnly)
                {
                    column.width = width;
                }
                column.desiredWidth = width;
            }
        }

        /// <summary>
        /// Initiates a column resize manipulation.
        /// </summary>
        /// <remarks>
        /// Must be called when starting interactively resizing the specified column.
        /// </remarks>
        /// <param name="column">The column to be resized.</param>
        /// <param name="pos">The position of the pointer.</param>
        /// <param name="previewMode">Indicates whether preview mode is active.</param>
        internal void BeginDragResize(Column column, float pos, bool previewMode)
        {
            if (m_IsDirty)
                throw new Exception("Cannot begin resizing columns because the layout needs to be updated");
            m_DragResizeInPreviewMode = previewMode;
            m_DragResizing = true;
            int index = column.visibleIndex;

            m_DragStartPos = pos;
            m_DragLastPos = pos;
            m_DragInitialColumnWidth = column.desiredWidth;
            m_DragStretchableColumns.Clear();
            m_DragFixedColumns.Clear();

            if (m_DragResizeInPreviewMode)
            {
                if (m_PreviewDesiredWidths == null)
                    m_PreviewDesiredWidths = new Dictionary<Column, float>();
                m_PreviewDesiredWidths[column] = column.desiredWidth;
            }

            for (var i = index + 1; i < m_Columns.visibleList.Count(); ++i)
            {
                var otherColumn = m_Columns.visibleList.ElementAt(i);

                if (!otherColumn.visible)
                    continue;
                if (otherColumn.stretchable)
                {
                    m_DragStretchableColumns.Add(otherColumn);
                }
                else
                {
                    m_DragFixedColumns.Add(otherColumn);
                }
                if (m_DragResizeInPreviewMode)
                    m_PreviewDesiredWidths[otherColumn] = otherColumn.desiredWidth;
            }
        }

        public float GetDesiredPosition(Column column)
        {
            if (!column.visible)
                return float.NaN;

            float pos = 0;

            for (var i = 0; i < column.visibleIndex; ++i)
            {
                var otherColumn = m_Columns.visibleList.ElementAt(i);
                var width = GetDesiredWidth(otherColumn);

                if (float.IsNaN(width))
                    continue;
                pos += width;
            }
            return pos;
        }

        public float GetDesiredWidth(Column c)
        {
            if (m_DragResizeInPreviewMode && m_PreviewDesiredWidths.ContainsKey(c))
                return m_PreviewDesiredWidths[c];
            return c.desiredWidth;
        }

        /// <summary>
        /// Resizes the specified column interactively to the specified position.
        /// </summary>
        /// <param name="column">The column to resize.</param>
        /// <param name="pos">The new position of the pointer.</param>
        public void DragResize(Column column, float pos)
        {
            // If it contains stretchable columns...
            if (hasStretchableColumns && m_Columns.stretchMode == Columns.StretchMode.GrowAndFill)
            {
                float delta = pos - m_DragLastPos;
                float newWidth = Mathf.Clamp(GetDesiredWidth(column) + delta, column.minWidth.value, column.maxWidth.value);

                // Recompute the new delta to ensure we dont exceed the limits
                delta = newWidth - GetDesiredWidth(column);

                StretchResizeColumns(m_DragStretchableColumns, m_DragFixedColumns, ref delta, false);

                // Recompute the new width with the new delta
                newWidth = Mathf.Clamp(GetDesiredWidth(column) + delta, column.minWidth.value, column.maxWidth.value);

                ResizeColumn(column, newWidth);
            }
            else
            {
                float delta = pos - m_DragStartPos;
                float newsize = Math.Max(column.minWidth.value, Math.Min(column.maxWidth.value, m_DragInitialColumnWidth + delta));

                ResizeColumn(column, newsize);
            }
            m_DragLastPos = pos;
        }

        /// <summary>
        /// Must be called when interactively resizing is finished.
        /// </summary>
        /// <param name="column">The column being resized.</param>
        /// <param name="cancelled">Indicates whether resizing has being cancelled.</param>
        internal void EndDragResize(Column column, bool cancelled)
        {
            if (m_DragResizeInPreviewMode)
            {
                m_DragResizeInPreviewMode = false;

                if (!cancelled)
                {
                    foreach (var columnPair in m_PreviewDesiredWidths)
                    {
                        ResizeColumn(columnPair.Key, columnPair.Value, columnPair.Key != column);
                    }
                }
                m_PreviewDesiredWidths.Clear();
            }
            m_DragResizing = false;
            m_DragStretchableColumns.Clear();
            m_DragFixedColumns.Clear();
        }

        /// <summary>
        /// Updates cached layout data of columns.
        /// </summary>
        void UpdateCache()
        {
            foreach (var column in m_Columns.visibleList)
            {
                if (column.stretchable)
                    m_StretchableColumns.Add(column);
                else
                    m_FixedColumns.Add(column);
                m_MaxColumnsWidth += column.maxWidth.value;
                m_MinColumnsWidth += column.minWidth.value;
            }
        }

        /// <summary>
        /// Clears the cached layout data of columns.
        /// </summary>
        void ClearCache()
        {
            m_StretchableColumns.Clear();
            m_FixedColumns.Clear();
            m_MaxColumnsWidth = 0;
            m_MinColumnsWidth = 0;
            m_ColumnsWidthDirty = true;
        }
    }
}
