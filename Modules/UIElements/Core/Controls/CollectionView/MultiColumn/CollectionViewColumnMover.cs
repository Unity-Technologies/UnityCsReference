// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements.HierarchyV2;

/// <summary>
/// Manipulator used to move columns in a multi column view
/// </summary>
class CollectionViewColumnMover : PointerManipulator
{
    const float k_StartDragDistance = 5f;

    float m_StartPos;
    float m_LastPos;
    float m_ColumnToMovePos;
    float m_ColumnToMoveWidth;
    float m_ScrollOffset;

    bool m_Active;
    bool m_Moving;
    bool m_Cancelled;
    bool m_MoveBeforeDestination;

    Column m_ColumnToMove;
    Column m_DestinationColumn;
    CollectionViewMultiColumnCollectionHeader m_Header;
    MultiColumnHeaderColumnMoveLocationPreview m_LocationPreviewElement;
    VisualElement m_PreviewElement;

    public ColumnLayout columnLayout { get; set; }

    public bool active
    {
        get => m_Active;
        set
        {
            if (m_Active == value)
                return;
            m_Active = value;
            activeChanged?.Invoke(this);
        }
    }

    public bool moving
    {
        get => m_Moving;
        set
        {
            if (m_Moving == value)
                return;
            m_Moving = value;
            movingChanged?.Invoke(this);
        }
    }


    public event Action<CollectionViewColumnMover> activeChanged;
    public event Action<CollectionViewColumnMover> movingChanged;

    /// <summary>
    /// Constructor
    /// </summary>
    public CollectionViewColumnMover()
    {
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        target.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

        target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    /// <summary>
    /// This method is called when a PointerDownEvent is sent to the target element.
    /// </summary>
    /// <param name="evt">The event.</param>
    void OnPointerDown(PointerDownEvent evt)
    {
        if (!CanStartManipulation(evt)) return;

        var ve = (evt.currentTarget as VisualElement);
        var header = ve.GetFirstAncestorOfType<CollectionViewMultiColumnCollectionHeader>();

        // Check if this is a frozen column
        if (header != null)
        {
            var columnControl = ve as CollectionViewMultiColumnHeaderColumn;
            if (columnControl != null)
            {
                var freezeState = header.GetColumnFreezeState(columnControl.column);
                if (freezeState != FreezeState.None)
                {
                    // Frozen column - stop event from going through to columns behind
                    evt.StopImmediatePropagation();
                    return;
                }
            }
        }

        ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
    }

    /// <summary>
    /// This method is called when a PointerMoveEvent is sent to the target element.
    /// </summary>
    /// <param name="evt">The event.</param>
    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!active) return;

        ProcessMoveEvent(evt, evt.localPosition);
    }

    /// <summary>
    /// This method is called when a PointerUpEvent is sent to the target element.
    /// </summary>
    /// <param name="evt">The event.</param>
    void OnPointerUp(PointerUpEvent evt)
    {
        if (!active || !CanStopManipulation(evt)) return;

        ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
    }

    /// <summary>
    /// This method is called when a PointerCancelEvent is sent to the target element.
    /// </summary>
    /// <param name="evt">The event.</param>
    void OnPointerCancel(PointerCancelEvent evt)
    {
        if (!active || !CanStopManipulation(evt)) return;

        ProcessCancelEvent(evt, evt.pointerId);
    }

    /// <summary>
    /// This method is called when a PointerCaptureOutEvent is sent to the target element.
    /// </summary>
    /// <param name="evt">The event.</param>
    void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (!active) return;

        ProcessCancelEvent(evt, evt.pointerId);
    }

    /// <summary>
    /// This method processes the up cancel sent to the target Element.
    /// </summary>
    protected void ProcessCancelEvent(EventBase evt, int pointerId)
    {
        active = false;
        target.ReleasePointer(pointerId);
        if (!(evt is IPointerEvent))
            target.panel.ProcessPointerCapture(pointerId);

        if (moving)
            EndDragMove(true);

        evt.StopPropagation();
    }

    void OnKeyDown(KeyDownEvent e)
    {
        if (e.keyCode == KeyCode.Escape && moving)
            EndDragMove(true);
    }

    void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
    {
        if (active)
        {
            evt.StopImmediatePropagation();
            return;
        }

        target.CapturePointer(pointerId);
        if (!(evt is IPointerEvent))
            target.panel.ProcessPointerCapture(pointerId);

        var ve = (evt.currentTarget as VisualElement);
        var header = ve.GetFirstAncestorOfType<CollectionViewMultiColumnCollectionHeader>();

        if (!header.columns.reorderable)
            return;

        m_Header = header;
        var pos = ve.ChangeCoordinatesTo(m_Header, localPosition);
        columnLayout = m_Header.columnLayout;

        // Get scroll offset directly from header if it exposes it, or calculate once
        m_ScrollOffset = 0f;
        var scrollContainer = m_Header.Q<VisualElement>(CollectionViewFrozenColumnUtility.ScrollableColumnsContainerName);
        if (scrollContainer != null)
            m_ScrollOffset = scrollContainer.resolvedStyle.translate.x;

        m_Cancelled = false;
        m_StartPos = pos.x;
        active = true;
        evt.StopPropagation();
    }

    void ProcessMoveEvent(EventBase e, Vector2 localPosition)
    {
        if (m_Cancelled)
            return;

        var ve = (e.currentTarget as VisualElement);
        var pos = ve.ChangeCoordinatesTo(m_Header, localPosition);

        if (!moving && Mathf.Abs(m_StartPos - pos.x) > k_StartDragDistance)
        {
            BeginDragMove(m_StartPos);
        }

        if (moving)
        {
            DragMove(pos.x);
        }

        e.StopPropagation();
    }

    void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
    {
        active = false;
        target.ReleasePointer(pointerId);
        if (!(evt is IPointerEvent))
            target.panel.ProcessPointerCapture(pointerId);

        var shouldStopPropagationImmediately = moving || m_Cancelled;

        EndDragMove(false);

        if (shouldStopPropagationImmediately)
            evt.StopImmediatePropagation();
        else
            evt.StopPropagation();
    }

    /// <summary>
    /// Called when starting moving using mouse.
    /// </summary>
    /// <param name="pos">The current position of the pointer.</param>
    void BeginDragMove(float pos)
    {
        var columns = columnLayout.columns;
        m_ColumnToMove = null;

        CollectionViewFrozenColumnUtility.CalculateFrozenWidths(m_Header, columnLayout, out var frozenLeftWidth, out var frozenRightWidth);

        var logicalPos = 0f;

        foreach (var column in columns.visibleList)
        {
            var width = columnLayout.GetDesiredWidth(column);
            var freezeState = m_Header.GetColumnFreezeState(column);

            // Calculate visual position in header space
            float visualPos;

            if (freezeState == FreezeState.FreezeLeft)
            {
                // Frozen-left: position from left edge
                visualPos = CollectionViewFrozenColumnUtility.CalculateFrozenLeftOffsetUpTo(m_Header, columnLayout, column);
            }
            else if (freezeState == FreezeState.None)
            {
                // Scrollable: starts after frozen-left, with scroll offset
                visualPos = frozenLeftWidth + logicalPos + m_ScrollOffset;
            }
            else // FreezeRight
            {
                var offsetInFrozenSection = CollectionViewFrozenColumnUtility.CalculateFrozenRightOffsetUpTo(m_Header, columnLayout, column);
                visualPos = m_Header.resolvedStyle.width - frozenRightWidth + offsetInFrozenSection;
            }

            // Check if click is within this column
            if (pos >= visualPos && pos < visualPos + width)
            {
                m_ColumnToMove = column;
                break;
            }

            // Only increment logical position for scrollable columns
            if (freezeState == FreezeState.None)
                logicalPos += width;
        }

        // Check if column is frozen - prevent dragging
        if (m_ColumnToMove != null && m_Header.GetColumnFreezeState(m_ColumnToMove) != FreezeState.None)
        {
            m_ColumnToMove = null;
            return;
        }

        if (m_ColumnToMove == null)
            return;

        moving = true;
        m_LastPos = pos;
        m_PreviewElement = new MultiColumnHeaderColumnMovePreview();
        m_LocationPreviewElement = new MultiColumnHeaderColumnMoveLocationPreview();
        m_Header.hierarchy.Add(m_PreviewElement);
        m_PreviewElement.BringToFront();

        VisualElement locationPreviewParent = m_Header.GetFirstAncestorOfType<ScrollView>()?.parent ?? m_Header;
        locationPreviewParent.hierarchy.Add(m_LocationPreviewElement);
        m_LocationPreviewElement.BringToFront();
        m_ColumnToMovePos = 0f;

        var moveColumnFreezeState = m_Header.GetColumnFreezeState(m_ColumnToMove);

        if (moveColumnFreezeState == FreezeState.None)
        {
            var frozenLeftOffset = CollectionViewFrozenColumnUtility.CalculateFrozenLeftOffsetUpTo(m_Header, columnLayout, null);
            var scrollableOffset = 0f;

            foreach (var col in columns.visibleList)
            {
                if (col == m_ColumnToMove)
                    break;

                if (m_Header.GetColumnFreezeState(col) == FreezeState.None)
                    scrollableOffset += columnLayout.GetDesiredWidth(col);
            }

            // Visual position = frozen-left width + scrollable offset + scroll translation
            m_ColumnToMovePos = frozenLeftOffset + scrollableOffset + m_ScrollOffset;
        }
        else
        {
            m_ColumnToMovePos = columnLayout.GetDesiredPosition(m_ColumnToMove);
        }

        m_ColumnToMoveWidth = columnLayout.GetDesiredWidth(m_ColumnToMove);
        UpdateMoveLocation();
    }

    /// <summary>
    /// Called when moving using mouse.
    /// </summary>
    /// <param name="pos">The current position of the pointer.</param>
    internal void DragMove(float pos)
    {
        m_LastPos = pos;
        UpdateMoveLocation();
    }

    void UpdatePreviewPosition()
    {
        m_PreviewElement.style.left = m_ColumnToMovePos + m_LastPos - m_StartPos;
        m_PreviewElement.style.width = m_ColumnToMoveWidth;

        if (m_DestinationColumn != null)
        {
            CollectionViewFrozenColumnUtility.CalculateFrozenWidths(m_Header, columnLayout, out var frozenLeftWidth, out var frozenRightWidth);

            // Calculate the logical position within scrollable area
            var logicalPosition = 0f;
            var destFreezeState = m_Header.GetColumnFreezeState(m_DestinationColumn);

            foreach (var col in columnLayout.columns.visibleList)
            {
                if (col == m_DestinationColumn)
                    break;

                // Only count scrollable columns
                if (m_Header.GetColumnFreezeState(col) == FreezeState.None)
                    logicalPosition += columnLayout.GetDesiredWidth(col);
            }

            if (!m_MoveBeforeDestination)
                logicalPosition += columnLayout.GetDesiredWidth(m_DestinationColumn);

            var previewPosition = frozenLeftWidth + logicalPosition;
            if (destFreezeState == FreezeState.None)
                previewPosition += m_ScrollOffset;

            // Calculate frozen-right boundary and use it to clamp the preview to visible scrollable area
            var frozenRightStart = m_Header.resolvedStyle.width - frozenRightWidth;

            if (previewPosition < frozenLeftWidth)
            {
                // Preview would be hidden behind frozen-left
                previewPosition = frozenLeftWidth;
            }
            else if (previewPosition >= frozenRightStart)
            {
                // Preview would overlap frozen-right
                previewPosition = frozenRightStart;
            }

            m_LocationPreviewElement.style.display = DisplayStyle.Flex;
            m_LocationPreviewElement.style.left = previewPosition;
        }
        else
        {
            m_LocationPreviewElement.style.display = DisplayStyle.None;
        }
    }

    void UpdateMoveLocation()
    {
        m_DestinationColumn = null;
        m_MoveBeforeDestination = false;

        CollectionViewFrozenColumnUtility.CalculateFrozenWidths(m_Header, columnLayout, out var frozenLeftWidth, out var frozenRightWidth);

        var logicalPos = 0f;
        Column lastScrollableColumn = null;

        foreach (var column in columnLayout.columns.visibleList)
        {
            var w = columnLayout.GetDesiredWidth(column);
            var freezeState = m_Header.GetColumnFreezeState(column);

            // Calculate visual position in header space
            float visualPos;

            switch (freezeState)
            {
                case FreezeState.FreezeLeft:
                    visualPos = CollectionViewFrozenColumnUtility.CalculateFrozenLeftOffsetUpTo(m_Header, columnLayout, column);
                    break;
                case FreezeState.None:
                    visualPos = frozenLeftWidth + logicalPos + m_ScrollOffset;
                    lastScrollableColumn = column;
                    break;
                default:
                {
                    var offsetInFrozenSection = CollectionViewFrozenColumnUtility.CalculateFrozenRightOffsetUpTo(m_Header, columnLayout, column);
                    visualPos = m_Header.resolvedStyle.width - frozenRightWidth + offsetInFrozenSection;
                    break;
                }
            }

            // Check if mouse is within this column's bounds
            if (m_LastPos >= visualPos && m_LastPos < visualPos + w)
            {
                if (freezeState == FreezeState.None)
                {
                    m_DestinationColumn = column;
                    m_MoveBeforeDestination = (m_LastPos < visualPos + w / 2);
                }

                break;
            }

            // Only increment logical position for scrollable columns
            if (freezeState == FreezeState.None)
                logicalPos += w;
        }

        // Special case: if mouse is between last scrollable column and frozen-right area
        if (m_DestinationColumn == null && lastScrollableColumn != null)
        {
            var lastScrollableVisualPos = frozenLeftWidth;
            foreach (var col in columnLayout.columns.visibleList)
            {
                if (col == lastScrollableColumn)
                    break;
                if (m_Header.GetColumnFreezeState(col) == FreezeState.None)
                    lastScrollableVisualPos += columnLayout.GetDesiredWidth(col);
            }

            lastScrollableVisualPos += m_ScrollOffset;

            var frozenRightStart = m_Header.resolvedStyle.width - frozenRightWidth;

            // If mouse is in the gap between scrollable and frozen-right areas
            if (m_LastPos >= lastScrollableVisualPos && m_LastPos < frozenRightStart)
            {
                m_DestinationColumn = lastScrollableColumn;
                m_MoveBeforeDestination = false;
            }
        }

        UpdatePreviewPosition();
    }

    /// <summary>
    /// Called when finishing moving using mouse.
    /// </summary>
    /// <param name="cancelled">Indicates whether drag move was cancelled.</param>
    void EndDragMove(bool cancelled)
    {
        if (!moving || m_Cancelled)
            return;

        m_Cancelled = cancelled;

        if (!cancelled && m_DestinationColumn != null)
        {
            var destIndex = m_DestinationColumn.displayIndex;

            if (!m_MoveBeforeDestination)
                destIndex++;

            if (m_ColumnToMove.displayIndex < destIndex)
                destIndex--;

            // Check if destination would place column in frozen area and only reorder if valid and position changed
            var isValidDrop = !IsInvalidDropPosition(m_ColumnToMove, destIndex);

            if (isValidDrop && m_ColumnToMove.displayIndex != destIndex)
            {
                columnLayout.columns.ReorderDisplay(m_ColumnToMove.displayIndex, destIndex);
            }
        }

        m_PreviewElement?.RemoveFromHierarchy();
        m_PreviewElement = null;
        m_LocationPreviewElement?.RemoveFromHierarchy();
        m_LocationPreviewElement = null;
        m_ColumnToMove = null;
        moving = false;
    }

    bool IsInvalidDropPosition(Column column, int destDisplayIndex)
    {
        // Can't drop non-frozen column in frozen area
        var columns = columnLayout.columns;

        // Find first and last non-frozen column positions
        var firstNonFrozenIndex = -1;
        var lastNonFrozenIndex = -1;
        var i = 0;

        foreach (var visibleColumn in columns.displayList)
        {
            var freezeState = m_Header.GetColumnFreezeState(visibleColumn);
            if (freezeState == FreezeState.None)
            {
                if (firstNonFrozenIndex == -1)
                    firstNonFrozenIndex = i;
                lastNonFrozenIndex = i;
            }

            i++;
        }

        // If no non-frozen columns exist, all drops are invalid
        if (firstNonFrozenIndex == -1)
            return true;

        // If trying to drop outside non-frozen range, it's invalid
        return destDisplayIndex < firstNonFrozenIndex || destDisplayIndex > lastNonFrozenIndex + 1;
    }
}
