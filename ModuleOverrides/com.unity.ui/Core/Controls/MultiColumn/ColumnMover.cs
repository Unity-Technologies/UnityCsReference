// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.Internal
{
    /// <summary>
    /// Shows a preview of the column being moved.
    /// </summary>
    class MultiColumnHeaderColumnMovePreview : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = MultiColumnHeaderColumn.ussClassName + "__move-preview";

        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiColumnHeaderColumnMovePreview()
        {
            AddToClassList(ussClassName);

            pickingMode = PickingMode.Ignore;
        }
    }

    /// <summary>
    /// Shows where the column being moved will be reordered.
    /// </summary>
    class MultiColumnHeaderColumnMoveLocationPreview : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = MultiColumnHeaderColumn.ussClassName + "__move-location-preview";
        public static readonly string visualUssClassName = ussClassName + "__visual";

        /// <summary>
        /// Constructor.
        /// </summary>
        public MultiColumnHeaderColumnMoveLocationPreview()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            var visual = new VisualElement();
            visual.AddToClassList(visualUssClassName);
            visual.pickingMode = PickingMode.Ignore;
            Add(visual);
        }
    }

    /// <summary>
    /// Manipulator used to move columns in a multi column view
    /// </summary>
    class ColumnMover : PointerManipulator
    {
        const float k_StartDragDistance = 5f;

        float m_StartPos;
        float m_LastPos;
        bool m_Active;
        bool m_Moving;
        bool m_Cancelled;
        MultiColumnCollectionHeader m_Header;
        VisualElement m_PreviewElement;
        MultiColumnHeaderColumnMoveLocationPreview m_LocationPreviewElement;
        Column m_ColumnToMove;
        float m_ColumnToMovePos;
        float m_ColumnToMoveWidth;
        Column m_DestinationColumn;
        bool m_MoveBeforeDestination;

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
                if(m_Moving == value)
                    return;
                m_Moving = value;
                movingChanged?.Invoke(this);
            }
        }


        public event Action<ColumnMover> activeChanged;
        public event Action<ColumnMover> movingChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ColumnMover()
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

            if (!evt.DiscardMouseEventsOnMobile())
            {
                ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
            }
        }

        /// <summary>
        /// This method is called when a PointerMoveEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!active) return;

            if (!evt.DiscardMouseEventsOnMobile())
            {
                ProcessMoveEvent(evt, evt.localPosition);
            }
        }

        /// <summary>
        /// This method is called when a PointerUpEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerUp(PointerUpEvent evt)
        {
            if (!active || !CanStopManipulation(evt)) return;

            if (!evt.DiscardMouseEventsOnMobile())
            {
                ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
            }
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

            if(moving)
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
            var header = ve.GetFirstAncestorOfType<MultiColumnCollectionHeader>();

            if (!header.columns.reorderable)
                return;

            m_Header = header;
            var pos = ve.ChangeCoordinatesTo(m_Header, localPosition);
            columnLayout = m_Header.columnLayout;
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
            float right = 0;
            var columns = columnLayout.columns;

            foreach (var column in columns.visibleList)
            {
                right += columnLayout.GetDesiredWidth(column);

                if (m_ColumnToMove == null)
                {
                    if (right > pos)
                        m_ColumnToMove = column;
                }
            }
            moving = true;
            m_LastPos = pos;
            m_PreviewElement = new MultiColumnHeaderColumnMovePreview();
            m_LocationPreviewElement = new MultiColumnHeaderColumnMoveLocationPreview();
            m_Header.hierarchy.Add(m_PreviewElement);

            VisualElement locationPreviewParent = m_Header.GetFirstAncestorOfType<ScrollView>()?.parent??m_Header;
            locationPreviewParent.hierarchy.Add(m_LocationPreviewElement);
            m_ColumnToMovePos = columnLayout.GetDesiredPosition(m_ColumnToMove);
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
                m_LocationPreviewElement.style.left = columnLayout.GetDesiredPosition(m_DestinationColumn) + (!m_MoveBeforeDestination ?
                    columnLayout.GetDesiredWidth(m_DestinationColumn) : 0);
            }
        }

        void UpdateMoveLocation()
        {
            float right = 0;

            m_DestinationColumn = null;
            m_MoveBeforeDestination = false;

            foreach (var column in columnLayout.columns.visibleList)
            {
                m_DestinationColumn = column;
                var w = columnLayout.GetDesiredWidth(m_DestinationColumn);
                var centerPos = right + w / 2;

                right += w;

                if (right > m_LastPos)
                {
                    m_MoveBeforeDestination = (m_LastPos < centerPos);
                    break;
                }
            }

            UpdatePreviewPosition();
        }

        /// <summary>
        /// Called when finishing resizing using mouse.
        /// </summary>
        /// <param name="cancelled">Indicates whether drag move was cancelled.</param>
        void EndDragMove(bool cancelled)
        {
            if (!moving || m_Cancelled)
                return;

            m_Cancelled = cancelled;

            if (!cancelled)
            {
                int destIndex = m_DestinationColumn.displayIndex;

                if (!m_MoveBeforeDestination)
                    destIndex++;

                if (m_ColumnToMove.displayIndex < destIndex)
                    destIndex--;

                // If we move the colum at the same location then ignore
                if (m_ColumnToMove.displayIndex != destIndex)
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
    }
}
