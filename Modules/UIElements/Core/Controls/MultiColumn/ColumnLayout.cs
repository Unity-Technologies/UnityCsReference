// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Lays out columns within their containers.
    /// </summary>
    class ColumnLayout
    {
        List<Column> m_StretchableColumns = new List<Column>();
        List<Column> m_FixedColumns = new List<Column>();
        List<Column> m_RelativeWidthColumns = new List<Column>();
        List<Column> m_MixedWidthColumns = new List<Column>();
        Columns m_Columns;
        float m_ColumnsWidth = 0;
        bool m_ColumnsWidthDirty = true;
        float m_MaxColumnsWidth = 0;
        float m_MinColumnsWidth = 0;
        bool m_IsDirty = false;
        float m_PreviousWidth = float.NaN;
        float m_LayoutWidth = float.NaN;

        // Drag info
        bool m_DragResizeInPreviewMode;
        bool m_DragResizing = false;
        float m_DragStartPos;
        float m_DragLastPos;
        float m_DragInitialColumnWidth;
        List<Column> m_DragStretchableColumns = new List<Column>();
        List<Column> m_DragRelativeColumns = new List<Column>();
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
        /// The width of the container.
        /// </summary>
        public float layoutWidth => m_LayoutWidth;

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
        /// Indicates whether the layout contains relative width columns.
        /// </summary>
        public bool hasRelativeWidthColumns => m_RelativeWidthColumns.Count > 0 || m_MixedWidthColumns.Count > 0;

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
            m_LayoutWidth = width;
            if (m_IsDirty)
                UpdateCache();

            if (hasRelativeWidthColumns)
                UpdateMinAndMaxColumnsWidth();

            var totalColumnsWidth = 0f;
            var fixedColumnsWidth = 0f;
            var totalStretchableWidth = 0f;
            var stretchableColumnsWithInvalidWidth = new List<Column>();
            var stretchableColumnsWithValidWidth = new List<Column>();

            // step 1 - Compute the width of the fixed columns with invalid (not yet computed) width
            // and adjust clamp widths if necessary
            foreach (var column in m_Columns)
            {
                if (!column.visible)
                    continue;

                var minWidth = column.GetMinWidth(m_LayoutWidth);
                var maxWidth = column.GetMaxWidth(m_LayoutWidth);
                var columnWidth = column.GetWidth(m_LayoutWidth);

                // If the actual width is not yet computed then set it
                if (float.IsNaN(column.desiredWidth))
                {
                    // Ignore stretchable columns with invalid width for now as they need particular computation
                    if (m_Columns.stretchMode == Columns.StretchMode.GrowAndFill && column.stretchable)
                    {
                        stretchableColumnsWithInvalidWidth.Add(column);
                        continue;
                    }

                    column.desiredWidth = Mathf.Clamp(columnWidth, minWidth, maxWidth);
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
                    if (!IsClamped(column.desiredWidth, minWidth, maxWidth))
                    {
                        column.desiredWidth = Mathf.Clamp(columnWidth, minWidth, maxWidth);
                    }

                    if (columns.stretchMode == Columns.StretchMode.Grow && column.width.unit == LengthUnit.Percent)
                    {
                        column.desiredWidth = Mathf.Clamp(columnWidth, minWidth, maxWidth);
                    }
                }

                if (!column.stretchable)
                    fixedColumnsWidth += column.desiredWidth;
                totalColumnsWidth += column.desiredWidth;
            }

            // step 2 - Compute the width of the stretchable columns with invalid width using the available remaining space
            if (stretchableColumnsWithInvalidWidth.Count > 0)
            {
                float availableWidth = Math.Max(0, width - fixedColumnsWidth);
                int count = m_StretchableColumns.Count;

                // Sort the columns ensure that the columns with the highest margins of growth are treated last
                stretchableColumnsWithInvalidWidth.Sort((c1, c2) => c1.GetMaxWidth(m_LayoutWidth).CompareTo(c2.GetMaxWidth(m_LayoutWidth)));

                foreach (var column in stretchableColumnsWithInvalidWidth)
                {
                    var widthPerColumn = availableWidth / count;

                    column.desiredWidth = Mathf.Clamp(widthPerColumn, column.GetMinWidth(m_LayoutWidth), column.GetMaxWidth(m_LayoutWidth));
                    availableWidth = Math.Max(0, availableWidth - column.desiredWidth);
                    --count;
                }

                // Sort the columns ensure that the columns with the highest margins of growth are treated last
                stretchableColumnsWithValidWidth.Sort((c1, c2) => c1.GetMaxWidth(m_LayoutWidth).CompareTo(c2.GetMaxWidth(m_LayoutWidth)));

                // Dispatch the remaining space proportionally among the stretchable column with already computed width
                foreach (var column in stretchableColumnsWithValidWidth)
                {
                    var oldWidth = GetDesiredWidth(column);
                    var ratio = oldWidth / totalStretchableWidth;
                    var widthPerColumn = availableWidth * ratio;

                    column.desiredWidth = Mathf.Clamp(widthPerColumn, column.GetMinWidth(m_LayoutWidth), column.GetMaxWidth(m_LayoutWidth));
                    availableWidth = Math.Max(0, availableWidth - column.desiredWidth);
                    totalStretchableWidth -= oldWidth;
                    --count;
                }
            }

            if (hasStretchableColumns || (hasRelativeWidthColumns && m_Columns.stretchMode == Columns.StretchMode.GrowAndFill))
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
                    // Copy as the original list gets cleared by Dirty()

                    using var i = ListPool<Column>.Get(out var stretchableColumnsToFit);
                    using var j = ListPool<Column>.Get(out var fixedColumnsToFit);
                    using var k = ListPool<Column>.Get(out var relativeWidthColumnsToFit);

                    stretchableColumnsToFit.AddRange(m_StretchableColumns);
                    fixedColumnsToFit.AddRange(m_FixedColumns);
                    relativeWidthColumnsToFit.AddRange(m_RelativeWidthColumns);

                    StretchResizeColumns(stretchableColumnsToFit, fixedColumnsToFit, relativeWidthColumnsToFit, ref deltaWidth, false, false);
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
        public void StretchResizeColumns(List<Column> stretchableColumns, List<Column> fixedColumns, List<Column> relativeWidthColumns, ref float delta, bool resizeToFit, bool dragResize)
        {
            if (stretchableColumns.Count == 0 && relativeWidthColumns.Count == 0 && fixedColumns.Count == 0)
                return;

            if (delta > 0)
            {
                DistributeOverflow(stretchableColumns, fixedColumns, relativeWidthColumns, ref delta, resizeToFit, dragResize);
            }
            else
            {
                DistributeExcess(stretchableColumns, fixedColumns, relativeWidthColumns, ref delta, resizeToFit, dragResize);
            }
        }

        void DistributeOverflow(List<Column> stretchableColumns, List<Column> fixedColumns, List<Column> relativeWidthColumns, ref float delta, bool resizeToFit, bool dragResize)
        {
            var distributedDelta = Math.Abs(delta);

            if (!resizeToFit && !dragResize)
            {
                // Start distributing the delta to the fixed columns to their desired width from the right to the left
                distributedDelta = RecomputeToDesiredWidth(fixedColumns, distributedDelta, true, true);

                // Recompute the desired width of the percentage columns from the right to the left
                distributedDelta = RecomputeToDesiredWidth(relativeWidthColumns, distributedDelta, true, true);
            }

            // Start distributing the remaining delta amount to the stretchable columns proportionally to their current width
            distributedDelta = RecomputeToMinWidthProportionally(stretchableColumns, distributedDelta, !(resizeToFit || dragResize));

            if (resizeToFit)
            {
                distributedDelta = RecomputeToMinWidthProportionally(relativeWidthColumns, distributedDelta, false);
                distributedDelta = RecomputeToMinWidthProportionally(fixedColumns, distributedDelta, false);

                // When distributing proportionally to the min width, we may have some delta left if the ones with lowest proportions reach min width first
                distributedDelta = RecomputeToMinWidth(relativeWidthColumns, distributedDelta, false);
                distributedDelta = RecomputeToMinWidth(fixedColumns, distributedDelta, false);

            }
            else if (dragResize)
            {
                // Start distributing the delta to the percentage columns to their min width from the left to the right
                distributedDelta = RecomputeToMinWidth(relativeWidthColumns, distributedDelta, true);

                // Start distributing the delta to the fixed columns to their min from the left to the right
                distributedDelta = RecomputeToMinWidth(fixedColumns, distributedDelta, true);
            }
            else
            {
                // If there is still some delta to distribute then distribute it to the relative and fixed columns
                // to their minimum limit from the right to the left
                if (distributedDelta > 0)
                {
                    distributedDelta = RecomputeToMinWidth(relativeWidthColumns, distributedDelta, true);
                    distributedDelta = RecomputeToMinWidth(fixedColumns, distributedDelta, true);
                }
            }

            // Removed the extra delta that could not be distributed.
            delta = Math.Max(0, delta - distributedDelta);
        }

        void DistributeExcess(List<Column> stretchableColumns, List<Column> fixedColumns, List<Column> relativeWidthColumns, ref float delta, bool resizeToFit, bool dragResize)
        {
            var distributedDelta = Math.Abs(delta);

            if (!resizeToFit && !dragResize)
            {
                // Start distributing the delta to the fixed columns to their desired width from the right to the left
                distributedDelta = RecomputeToDesiredWidth(fixedColumns, distributedDelta, true, false);

                // Recompute the desired width of the percentage columns from the right to the left
                distributedDelta = RecomputeToDesiredWidth(relativeWidthColumns, distributedDelta, true, false);
            }

            if (dragResize)
            {
                distributedDelta = RecomputeToDesiredWidth(fixedColumns, distributedDelta, true, false);
                distributedDelta = RecomputeToDesiredWidth(relativeWidthColumns, distributedDelta, true, false);
            }

            // Start distributing the remaining delta amount to the stretchable columns proportionally to their max value
            distributedDelta = RecomputeToMaxWidthProportionally(stretchableColumns, distributedDelta, !(resizeToFit || dragResize));

            // If there is still some delta to distribute then distribute it to the fixed and relative columns proportionally
            // to their maximum limit from the right to the left
            if (resizeToFit)
            {
                distributedDelta = RecomputeToMaxWidthProportionally(relativeWidthColumns, distributedDelta, false);
                distributedDelta = RecomputeToMaxWidthProportionally(fixedColumns, distributedDelta, false);

                // When distributing proportionally to the max width, we may have some delta left if the ones with highest proportions reach max width first
                distributedDelta = RecomputeToMaxWidth(relativeWidthColumns, distributedDelta, false);
                distributedDelta = RecomputeToMaxWidth(fixedColumns, distributedDelta, false);
            }

            delta += distributedDelta;
        }

        float RecomputeToMaxWidthProportionally(List<Column> columns, float distributedDelta, bool setDesiredWidthOnly = false)
        {
            if (distributedDelta > 0)
            {
                // Sort the columns to treat the columns with the greatest margins to grow last
                columns.Sort((c1, c2) => c1.GetMaxWidth(m_LayoutWidth).CompareTo(c2.GetMaxWidth(m_LayoutWidth)));

                var totalColumnWidth = 0f;

                columns.ForEach((c) => totalColumnWidth += GetDesiredWidth(c));

                // Start distributing the delta amount to the columns
                for (var i = 0; i < columns.Count; ++i)
                {
                    var column = columns[i];
                    var oldWidth = GetDesiredWidth(column);
                    var ratio = GetDesiredWidth(column) / totalColumnWidth;
                    var deltaPerColumn = distributedDelta * ratio;
                    var appliedDelta = 0f;
                    var maxWidth = column.GetMaxWidth(m_LayoutWidth);

                    if (GetDesiredWidth(column) < maxWidth)
                        appliedDelta = Math.Min(deltaPerColumn, maxWidth - GetDesiredWidth(column));
                    if (appliedDelta > 0f)
                        ResizeColumn(column, GetDesiredWidth(column) + appliedDelta, setDesiredWidthOnly);

                    totalColumnWidth -= oldWidth;
                    distributedDelta -= appliedDelta;

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }

            return distributedDelta;
        }

        float RecomputeToMinWidthProportionally(List<Column> columns, float distributedDelta, bool setDesiredWidthOnly = false)
        {
            if (distributedDelta > 0)
            {
                // Sort the fixed columns to treat the columns with the smallest min with last
                columns.Sort((c1, c2) => c2.GetMinWidth(m_LayoutWidth).CompareTo(c1.GetMinWidth(m_LayoutWidth)));

                var totalColumnsWidth = 0f;

                columns.ForEach((c) => totalColumnsWidth += GetDesiredWidth(c));

                for (var i = 0; i < columns.Count; ++i)
                {
                    var column = columns[i];
                    var oldWidth = GetDesiredWidth(column);
                    var ratio = GetDesiredWidth(column) / totalColumnsWidth;
                    var deltaPerColumn = distributedDelta * ratio;
                    var appliedDelta = 0f;

                    if (GetDesiredWidth(column) > column.GetMinWidth(m_LayoutWidth))
                        appliedDelta = Math.Min(deltaPerColumn, GetDesiredWidth(column) - column.GetMinWidth(m_LayoutWidth));

                    if (appliedDelta > 0f)
                        ResizeColumn(column, GetDesiredWidth(column) - appliedDelta, setDesiredWidthOnly);

                    totalColumnsWidth -= oldWidth;
                    distributedDelta -= appliedDelta;

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }

            return distributedDelta;
        }

        float RecomputeToDesiredWidth(List<Column> columns, float distributedDelta, bool setDesiredWidthOnly, bool distributeOverflow)
        {
            if (distributeOverflow)
            {
                for (var i = columns.Count - 1; i >= 0; --i)
                {
                    distributedDelta = RecomputeToDesiredWidth(columns[i], distributedDelta, setDesiredWidthOnly, true);

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }
            else
            {
                for (var i = 0; i < columns.Count; ++i)
                {
                    distributedDelta = RecomputeToDesiredWidth(columns[i], distributedDelta, setDesiredWidthOnly, false);

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }

            return distributedDelta;
        }

        float RecomputeToDesiredWidth(Column column, float distributedDelta, bool setDesiredWidthOnly, bool distributeOverflow)
        {
            var appliedDelta = 0f;
            var clampedWidth = Mathf.Clamp(column.GetWidth(m_LayoutWidth), column.GetMinWidth(m_LayoutWidth), column.GetMaxWidth(m_LayoutWidth));

            if (GetDesiredWidth(column) > clampedWidth && distributeOverflow)
                appliedDelta = Math.Min(distributedDelta, Math.Abs(GetDesiredWidth(column) - clampedWidth));

            if (GetDesiredWidth(column) < clampedWidth && !distributeOverflow)
                appliedDelta = Math.Min(distributedDelta, Math.Abs(clampedWidth - GetDesiredWidth(column)));

            var width = distributeOverflow ? GetDesiredWidth(column) - appliedDelta : GetDesiredWidth(column) + appliedDelta;

            if (appliedDelta > 0f)
                ResizeColumn(column, width, setDesiredWidthOnly);

            distributedDelta -= appliedDelta;

            return distributedDelta;
        }

        float RecomputeToMinWidth(List<Column> columns, float distributedDelta, bool setDesiredWidthOnly = false)
        {
            if (distributedDelta > 0)
            {
                for (int i = columns.Count - 1; i >= 0; --i)
                {
                    var column = columns[i];
                    var appliedDelta = 0f;

                    if (GetDesiredWidth(column) > column.GetMinWidth(m_LayoutWidth))
                        appliedDelta = Math.Min(distributedDelta, GetDesiredWidth(column) - column.GetMinWidth(m_LayoutWidth));
                    if (appliedDelta > 0f)
                        ResizeColumn(column, GetDesiredWidth(column) - appliedDelta, setDesiredWidthOnly);

                    distributedDelta -= appliedDelta;

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }

            return distributedDelta;
        }

        float RecomputeToMaxWidth(List<Column> columns, float distributedDelta, bool setDesiredWidthOnly = false)
        {
            if (distributedDelta > 0)
            {
                for (var i = 0; i < columns.Count; ++i)
                {
                    var column = columns[i];
                    var appliedDelta = 0f;

                    if (GetDesiredWidth(column) < column.GetMaxWidth(m_LayoutWidth))
                        appliedDelta = Math.Min(distributedDelta, Math.Abs(column.GetMaxWidth(m_LayoutWidth) - GetDesiredWidth(column)));

                    if (appliedDelta > 0f)
                        ResizeColumn(column, GetDesiredWidth(column) + appliedDelta, setDesiredWidthOnly);

                    distributedDelta -= appliedDelta;

                    // If there is no delta to distribute then stop
                    if (distributedDelta <= 0)
                        break;
                }
            }
            return distributedDelta;
        }

        /// <summary>
        ///  Resizes all the columns to fit the specified width, starting by resizing stretchable columns and ending with fixed columns.
        /// </summary>
        /// <param name="width">The width of the container.</param>
        public void ResizeToFit(float width)
        {
            float delta = columnsWidth - Mathf.Clamp(width, minColumnsWidth, maxColumnsWidth);

            // Copy as the original list gets cleared by Dirty()
            using var i = ListPool<Column>.Get(out var stretchableColumnsToFit);
            using var j = ListPool<Column>.Get(out var fixedColumnsToFit);
            using var k = ListPool<Column>.Get(out var relativeWidthColumnsToFit);

            stretchableColumnsToFit.AddRange(m_StretchableColumns);
            fixedColumnsToFit.AddRange(m_FixedColumns);
            relativeWidthColumnsToFit.AddRange(m_RelativeWidthColumns);

            StretchResizeColumns(stretchableColumnsToFit, fixedColumnsToFit, relativeWidthColumnsToFit, ref delta, true, false);

            if (m_IsDirty)
                UpdateCache();
        }

        /// <summary>
        /// Sets the desired width and optionally the width (when manually resized by user) of the specified column
        /// </summary>
        /// <param name="column">The column to resize.</param>
        /// <param name="width">The new width of the column.</param>
        /// <param name="setDesiredWidthOnly">Indicated whether only the desired width should be changed.</param>
        void ResizeColumn(Column column, float width, bool setDesiredWidthOnly = false)
        {
            var widthInPercent = new Length(width / layoutWidth * 100, LengthUnit.Percent);

            if (m_DragResizeInPreviewMode)
            {
                m_PreviewDesiredWidths[column] = width;
            }
            else
            {
                if (!setDesiredWidthOnly)
                {
                    column.width = column.width.unit == LengthUnit.Percent ? widthInPercent : width;
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
            m_DragRelativeColumns.Clear();

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
                else if (otherColumn.width.unit == LengthUnit.Percent)
                {
                    m_DragRelativeColumns.Add(otherColumn);
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
            var minWidth = column.GetMinWidth(m_LayoutWidth);
            var maxWidth = column.GetMaxWidth(m_LayoutWidth);

            // If it contains stretchable columns...
            if (m_Columns.stretchMode == Columns.StretchMode.GrowAndFill)
            {
                float delta = pos - m_DragLastPos;

                float newWidth = Mathf.Clamp(GetDesiredWidth(column) + delta, minWidth, maxWidth);

                // Recompute the new delta to ensure we dont exceed the limits
                delta = newWidth - GetDesiredWidth(column);

                // if there's excess but no stretchable columns, just resize the column without affecting the others
                if (m_DragStretchableColumns.Count == 0 && delta < 0)
                {
                    StretchResizeColumns(m_DragStretchableColumns, m_DragFixedColumns, m_DragRelativeColumns, ref delta, false, true);

                    // Recompute the new width with the new delta
                    newWidth = Mathf.Clamp(GetDesiredWidth(column) + newWidth - GetDesiredWidth(column), minWidth, maxWidth);
                }
                else
                {
                    // if there is already excess, use that first
                    if (delta > 0 && columnsWidth + delta < m_LayoutWidth)
                    {
                        var requiredDelta = delta < m_LayoutWidth - columnsWidth ? 0 : delta - (m_LayoutWidth - columnsWidth);

                        StretchResizeColumns(m_DragStretchableColumns, m_DragFixedColumns, m_DragRelativeColumns, ref requiredDelta, false, true);

                        // Recompute the new width with the new delta
                        newWidth = Mathf.Clamp(GetDesiredWidth(column) + delta - requiredDelta, minWidth, maxWidth);
                    }
                    else
                    {
                        StretchResizeColumns(m_DragStretchableColumns, m_DragFixedColumns, m_DragRelativeColumns, ref delta, false, true);

                        // Recompute the new width with the new delta
                        newWidth = Mathf.Clamp(GetDesiredWidth(column) + delta, minWidth, maxWidth);
                    }
                }

                ResizeColumn(column, newWidth);
            }
            else
            {
                float delta = pos - m_DragStartPos;
                float newSize = Math.Max(minWidth, Math.Min(maxWidth, m_DragInitialColumnWidth + delta));

                ResizeColumn(column, newSize);
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
            m_DragRelativeColumns.Clear();
        }

        /// <summary>
        /// Updates cached layout data of columns.
        /// </summary>
        void UpdateCache()
        {
            ClearCache();
            foreach (var column in m_Columns.visibleList)
            {
                if (column.stretchable && columns.stretchMode == Columns.StretchMode.GrowAndFill)
                    m_StretchableColumns.Add(column);
                else if (column.width.unit == LengthUnit.Pixel)
                    m_FixedColumns.Add(column);
                // Columns with percentage widths are added to relativeWidthColumns regardless of their stretchability.
                if (column.width.unit == LengthUnit.Percent)
                    m_RelativeWidthColumns.Add(column);

                if (column.width.unit == LengthUnit.Pixel && (column.minWidth.unit == LengthUnit.Percent || column.maxWidth.unit == LengthUnit.Percent))
                    m_MixedWidthColumns.Add(column);

                m_MaxColumnsWidth += column.GetMaxWidth(m_LayoutWidth);
                m_MinColumnsWidth += column.GetMinWidth(m_LayoutWidth);
            }
        }

        void UpdateMinAndMaxColumnsWidth()
        {
            m_MaxColumnsWidth = 0;
            m_MinColumnsWidth = 0;
            foreach (var column in m_Columns.visibleList)
            {
                m_MaxColumnsWidth += column.GetMaxWidth(m_LayoutWidth);
                m_MinColumnsWidth += column.GetMinWidth(m_LayoutWidth);
            }
        }

        /// <summary>
        /// Clears the cached layout data of columns.
        /// </summary>
        void ClearCache()
        {
            m_StretchableColumns.Clear();
            m_RelativeWidthColumns.Clear();
            m_FixedColumns.Clear();
            m_MaxColumnsWidth = 0;
            m_MinColumnsWidth = 0;
            m_ColumnsWidthDirty = true;
        }
    }
}
