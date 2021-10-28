// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    internal abstract class DragEventsProcessor
    {
        enum DragState
        {
            None,
            CanStartDrag,
            Dragging
        }

        bool m_IsRegistered;
        DragState m_DragState;
        private Vector3 m_Start;
        internal readonly VisualElement m_Target;

        // Used in tests
        internal bool isRegistered => m_IsRegistered;

        private const int k_DistanceToActivation = 5;

        // Need to store the args here for player, since we won't have access to DragAndDropUtility methods.
        internal DefaultDragAndDropClient dragAndDropClient;
        internal virtual bool supportsDragEvents => true;

        internal bool useDragEvents => isEditorContext && supportsDragEvents;

        bool isEditorContext
        {
            get
            {
                Assert.IsNotNull(m_Target);
                Assert.IsNotNull(m_Target.parent);
                return m_Target.panel.contextType == ContextType.Editor;
            }
        }

        internal DragEventsProcessor(VisualElement target)
        {
            m_Target = target;
            m_Target.RegisterCallback<AttachToPanelEvent>(RegisterCallbacksFromTarget);
            m_Target.RegisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);

            RegisterCallbacksFromTarget();
        }

        private void RegisterCallbacksFromTarget(AttachToPanelEvent evt)
        {
            RegisterCallbacksFromTarget();
        }

        private void RegisterCallbacksFromTarget()
        {
            if (m_IsRegistered)
                return;

            m_IsRegistered = true;

            m_Target.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            m_Target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            m_Target.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            m_Target.RegisterCallback<PointerCancelEvent>(OnPointerCancelEvent);

            m_Target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }

        private void UnregisterCallbacksFromTarget(DetachFromPanelEvent evt)
        {
            UnregisterCallbacksFromTarget();
        }

        /// <summary>
        /// Unregisters all pointer and drag callbacks.
        /// </summary>
        /// <param name="unregisterPanelEvents">Whether or not we should also unregister panel attach/detach events. Use this when you are about to replace the dragger instance.</param>
        internal void UnregisterCallbacksFromTarget(bool unregisterPanelEvents = false)
        {
            m_IsRegistered = false;

            m_Target.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
            m_Target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            m_Target.UnregisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
            m_Target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);

            if (unregisterPanelEvents)
            {
                m_Target.UnregisterCallback<AttachToPanelEvent>(RegisterCallbacksFromTarget);
                m_Target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
            }
        }

        protected abstract bool CanStartDrag(Vector3 pointerPosition);
        protected abstract StartDragArgs StartDrag(Vector3 pointerPosition);
        protected abstract DragVisualMode UpdateDrag(Vector3 pointerPosition);

        protected abstract void OnDrop(Vector3 pointerPosition);
        protected abstract void ClearDragAndDropUI();

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                m_DragState = DragState.None;
                return;
            }

            if (CanStartDrag(evt.position))
            {
                m_DragState = DragState.CanStartDrag;
                m_Start = evt.position;
            }
        }

        internal void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (!useDragEvents)
            {
                if (m_DragState == DragState.Dragging)
                {
                    m_Target.ReleasePointer(evt.pointerId);
                    OnDrop(evt.position);
                    ClearDragAndDropUI();
                    evt.StopPropagation();
                }
            }

            m_DragState = DragState.None;
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            if (evt.target == m_Target)
                ClearDragAndDropUI();
        }

        void OnPointerCancelEvent(PointerCancelEvent evt)
        {
            if (!useDragEvents)
                ClearDragAndDropUI();
        }

        private void OnDragExitedEvent(DragExitedEvent evt)
        {
            if (!useDragEvents)
                return;

            ClearDragAndDropUI();
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (!useDragEvents)
                return;

            m_DragState = DragState.None;
            OnDrop(evt.mousePosition);

            ClearDragAndDropUI();
            DragAndDropUtility.dragAndDrop.AcceptDrag();
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (!useDragEvents)
                return;

            var visualMode = UpdateDrag(evt.mousePosition);
            DragAndDropUtility.dragAndDrop.SetVisualMode(visualMode);
        }


        private void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (useDragEvents)
            {
                if (m_DragState != DragState.CanStartDrag)
                    return;
            }
            else
            {
                if (m_DragState == DragState.Dragging)
                {
                    UpdateDrag(evt.position);
                    return;
                }

                if (m_DragState != DragState.CanStartDrag)
                    return;
            }

            if (Mathf.Abs(m_Start.x - evt.position.x) > k_DistanceToActivation ||
                Mathf.Abs(m_Start.y - evt.position.y) > k_DistanceToActivation)
            {
                var startDragArgs = StartDrag(m_Start);

                if (useDragEvents)
                {
                    // Drag can only be started by mouse events or else it will throw an error, so we leave early.
                    if (Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseDrag)
                        return;

                    DragAndDropUtility.dragAndDrop.StartDrag(startDragArgs);
                }
                else // Force default drag and drop client for runtime panels.
                {
                    m_Target.CapturePointer(evt.pointerId);
                    evt.StopPropagation();

                    dragAndDropClient = new DefaultDragAndDropClient();
                    dragAndDropClient.StartDrag(startDragArgs);
                }

                m_DragState = DragState.Dragging;
            }
        }
    }
}
