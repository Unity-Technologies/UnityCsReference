// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements.HierarchyV2;

/// <summary>
/// Manipulator used to resize columns in a multi column view.
/// </summary>
class CollectionViewColumnResizer : PointerManipulator
{
    protected bool m_Active;

    bool m_Resizing;
    Vector2 m_Start;

    CollectionViewMultiColumnCollectionHeader m_Header;
    Column m_Column;
    VisualElement m_PreviewElement;

    public ColumnLayout columnLayout { get; set; }

    /// <summary>
    /// Indicates whether columns automatically resize as user drags or only resize when user releases the Pointer.
    /// </summary>
    public bool preview { get; set; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="column"></param>
    public CollectionViewColumnResizer(Column column)
    {
        m_Column = column;
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
        target.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void OnKeyDown(KeyDownEvent e)
    {
        if (e.keyCode == KeyCode.Escape && m_Resizing && preview)
            EndDragResize(0, true);
    }

    void OnPointerDown(PointerDownEvent e)
    {
        if (m_Active)
        {
            e.StopImmediatePropagation();
            return;
        }

        if (CanStartManipulation(e))
        {
            var ve = (e.currentTarget as VisualElement);
            m_Header = ve.GetFirstAncestorOfType<CollectionViewMultiColumnCollectionHeader>();
            preview = m_Column.collection.resizePreview;

            if (preview)
            {
                if (m_PreviewElement == null)
                    m_PreviewElement = new MultiColumnHeaderColumnResizePreview();

                var previewParent = m_Header.GetFirstAncestorOfType<ScrollView>()?.parent ?? m_Header.parent;
                previewParent.hierarchy.Add(m_PreviewElement);
            }

            columnLayout = m_Header.columnLayout;
            m_Start = ve.ChangeCoordinatesTo(m_Header, e.localPosition);
            BeginDragResize(m_Start.x);
            m_Active = true;
            target.CaptureMouse();
            e.StopPropagation();
        }
    }

    void OnPointerMove(PointerMoveEvent e)
    {
        if (!m_Active || !target.HasPointerCapture(e.pointerId))
            return;

        var ve = (e.currentTarget as VisualElement);
        var pos = ve.ChangeCoordinatesTo(m_Header, e.localPosition);

        DragResize(pos.x);
        e.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent e)
    {
        if (!m_Active || !target.HasPointerCapture(e.pointerId) || !CanStopManipulation(e))
            return;

        var ve = (e.currentTarget as VisualElement);
        var pos = ve.ChangeCoordinatesTo(m_Header, e.localPosition);

        EndDragResize(pos.x, false);
        m_Active = false;
        target.ReleasePointer(e.pointerId);
        e.StopPropagation();
    }

    /// <summary>
    /// Called when starting resizing using Pointer.
    /// </summary>
    /// <param name="pos">The current position of the pointer.</param>
    void BeginDragResize(float pos)
    {
        m_Resizing = true;
        columnLayout?.BeginDragResize(m_Column, m_Start.x, preview);

        if (preview)
        {
            UpdatePreviewPosition();
        }
    }

    /// <summary>
    /// Called when drag resizing using Pointer.
    /// </summary>
    /// <param name="pos">The current position of the pointer.</param>
    void DragResize(float pos)
    {
        if (!m_Resizing)
            return;

        columnLayout?.DragResize(m_Column, pos);

        if (preview)
        {
            UpdatePreviewPosition();
        }
    }

    void UpdatePreviewPosition()
    {
        var freezeState = m_Header.GetColumnFreezeState(m_Column);
        float previewPosition;

        if (freezeState == FreezeState.None)
        {
            // Scrollable column: calculate with scroll offset
            var frozenLeftWidth = CollectionViewFrozenColumnUtility.CalculateFrozenLeftOffsetUpTo(m_Header, columnLayout, null);
            var scrollableOffset = 0f;

            foreach (var col in m_Header.columns.visibleList)
            {
                if (col == m_Column)
                {
                    scrollableOffset += columnLayout.GetDesiredWidth(col);
                    break;
                }

                if (m_Header.GetColumnFreezeState(col) == FreezeState.None)
                    scrollableOffset += columnLayout.GetDesiredWidth(col);
            }

            // Get scroll offset from the container
            var scrollContainer = m_Header.Q<VisualElement>(CollectionViewFrozenColumnUtility.ScrollableColumnsContainerName);
            var scrollOffset = scrollContainer != null ? scrollContainer.resolvedStyle.translate.x : 0f;

            previewPosition = frozenLeftWidth + scrollableOffset + scrollOffset;
        }
        else
        {
            // Frozen column - use absolute position
            previewPosition = columnLayout.GetDesiredPosition(m_Column) + columnLayout.GetDesiredWidth(m_Column);
        }

        m_PreviewElement.style.left = previewPosition;
    }

    /// <summary>
    /// Called when finishing resizing using Pointer.
    /// </summary>
    /// <param name="pos">The current position of the pointer.</param>
    /// <param name="cancelled">Indicates whether resizing has been cancelled.</param>
    void EndDragResize(float pos, bool cancelled)
    {
        if (!m_Resizing)
            return;

        if (preview)
        {
            m_PreviewElement?.RemoveFromHierarchy();
            m_PreviewElement = null;
        }

        columnLayout?.EndDragResize(m_Column, cancelled);
        m_Resizing = false;
    }
}
