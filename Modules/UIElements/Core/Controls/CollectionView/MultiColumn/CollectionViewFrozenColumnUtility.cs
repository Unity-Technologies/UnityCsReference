// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements.HierarchyV2;

/// <summary>
/// Utility class for frozen column calculations in multi-column collection views.
/// Centralizes all position, width, and offset calculations for frozen columns.
/// </summary>
[VisibleToOtherModules("UnityEngine.HierarchyModule")]
internal static class CollectionViewFrozenColumnUtility
{
    /// <summary>
    /// The name of the scrollable columns container element.
    /// </summary>
    public const string ScrollableColumnsContainerName = "scrollable-columns-container";

    /// <summary>
    /// The name of the scrollable columns clipping container element.
    /// This container clips the scrollable columns to prevent them from overlapping frozen columns.
    /// </summary>
    public const string ScrollableColumnsClippingContainerName = "scrollable-columns-clipping-container";

    /// <summary>
    /// The name of the scrollable columns resize handler container element.
    /// This container holds the resize handles for scrollable (non-frozen) columns.
    /// </summary>
    public const string ScrollableColumnsResizeHandlerContainerName = "scrollable-columns-resize-handler-container";

    /// <summary>
    /// Calculates the total width of all frozen columns (both left and right).
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="columns">The columns collection.</param>
    /// <returns>Total width of all frozen columns in pixels.</returns>
    public static float CalculateTotalFrozenWidth(CollectionViewMultiColumnCollectionHeader header, Columns columns)
    {
        if (header?.columnDataMap == null || columns?.visibleList == null)
            return 0f;

        var frozenWidth = 0f;
        foreach (var col in columns.visibleList)
        {
            var freezeState = header.GetColumnFreezeState(col);
            if (freezeState != FreezeState.None &&
                header.columnDataMap.TryGetValue(col, out var colData))
            {
                frozenWidth += colData.control.resolvedStyle.width;
            }
        }

        return frozenWidth;
    }

    /// <summary>
    /// Calculates the widths of frozen-left and frozen-right columns separately.
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="columnLayout">The column layout manager.</param>
    /// <param name="frozenLeftWidth">Output: total width of frozen-left columns.</param>
    /// <param name="frozenRightWidth">Output: total width of frozen-right columns.</param>
    public static void CalculateFrozenWidths(CollectionViewMultiColumnCollectionHeader header, ColumnLayout columnLayout, out float frozenLeftWidth, out float frozenRightWidth)
    {
        frozenLeftWidth = 0f;
        frozenRightWidth = 0f;

        if (header == null || columnLayout?.columns?.visibleList == null)
            return;

        foreach (var col in columnLayout.columns.visibleList)
        {
            var freezeState = header.GetColumnFreezeState(col);
            if (freezeState == FreezeState.FreezeLeft)
                frozenLeftWidth += columnLayout.GetDesiredWidth(col);
            else if (freezeState == FreezeState.FreezeRight)
                frozenRightWidth += columnLayout.GetDesiredWidth(col);
        }
    }

    /// <summary>
    /// Calculates the position offset for all frozen-left columns up to (but not including) the target column.
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="columnLayout">The column layout manager.</param>
    /// <param name="targetColumn">The target column to calculate offset up to.</param>
    /// <returns>Total width of frozen-left columns before the target column.</returns>
    public static float CalculateFrozenLeftOffsetUpTo(CollectionViewMultiColumnCollectionHeader header, ColumnLayout columnLayout, Column targetColumn)
    {
        var offset = 0f;
        foreach (var col in columnLayout.columns.visibleList)
        {
            if (col == targetColumn && targetColumn != null) // Add null check
                break;

            if (header.GetColumnFreezeState(col) == FreezeState.FreezeLeft)
                offset += columnLayout.GetDesiredWidth(col);
            else if (targetColumn == null)
                break; // If no target, stop at first non-frozen-left
        }
        return offset;
    }

    /// <summary>
    /// Calculates the position offset for all frozen-right columns up to (but not including) the target column.
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="columnLayout">The column layout manager.</param>
    /// <param name="targetColumn">The target column to calculate offset up to.</param>
    /// <returns>Total width of frozen-right columns before the target column.</returns>
    public static float CalculateFrozenRightOffsetUpTo(CollectionViewMultiColumnCollectionHeader header, ColumnLayout columnLayout, Column targetColumn)
    {
        var offset = 0f;
        foreach (var col in columnLayout.columns.visibleList)
        {
            if (col == targetColumn)
                break;

            if (header.GetColumnFreezeState(col) == FreezeState.FreezeRight)
                offset += columnLayout.GetDesiredWidth(col);
        }

        return offset;
    }

    /// <summary>
    /// Calculates the resize handle position for a given column.
    /// Frozen columns use absolute positioning, scrollable columns use relative positioning.
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="column">The column to calculate handle position for.</param>
    /// <param name="colData">The column's visual data.</param>
    /// <param name="scrollableColumnsResize">The scrollable columns resize container.</param>
    /// <returns>The left position for the resize handle in pixels.</returns>
    public static float CalculateResizeHandlePosition(CollectionViewMultiColumnCollectionHeader header, Column column, CollectionViewMultiColumnCollectionHeader.ColumnData colData, VisualElement scrollableColumnsResize)
    {
        if (header == null || column == null || colData?.control == null)
            return 0f;

        var freezeState = header.GetColumnFreezeState(column);

        if (freezeState == FreezeState.None)
        {
            // Scrollable: calculate relative position within the scrollable container
            var columnRightInWorld = colData.control.worldBound.xMax;
            var scrollContainerLeftInWorld = scrollableColumnsResize.worldBound.xMin;
            return columnRightInWorld - scrollContainerLeftInWorld;
        }
        else
        {
            // Frozen (left or right): Calculate absolute position in header space
            var columnRightInWorld = colData.control.worldBound.xMax;
            var headerLeftInWorld = header.worldBound.xMin;
            return columnRightInWorld - headerLeftInWorld;
        }
    }

    /// <summary>
    /// Updates the resize handle positions for all scrollable (non-frozen) columns.
    /// Called when frozen-left columns resize and shift the scrollable container.
    /// </summary>
    /// <param name="header">The multi-column header.</param>
    /// <param name="scrollableColumnsResize">The scrollable columns resize container.</param>
    public static void UpdateScrollableHandlePositions(CollectionViewMultiColumnCollectionHeader header, VisualElement scrollableColumnsResize)
    {
        if (header?.columnDataMap == null || scrollableColumnsResize == null)
            return;

        foreach (var kvp in header.columnDataMap)
        {
            var colFreezeState = header.GetColumnFreezeState(kvp.Key);
            if (colFreezeState != FreezeState.None)
                continue;

            var pos = CalculateResizeHandlePosition(header, kvp.Key, kvp.Value, scrollableColumnsResize);
            kvp.Value.resizeHandle.style.left = pos;
        }
    }
}
