// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    internal abstract class DragEventsProcessor
    {
        internal enum DragState
        {
            None,
            CanStartDrag,
            Dragging
        }

        bool m_IsRegistered;
        DragState m_DragState;
        Vector3 m_Start;
        protected readonly VisualElement m_Target;

        // Used in tests
        internal bool isRegistered => m_IsRegistered;
        internal DragState dragState => m_DragState;

        protected virtual bool supportsDragEvents => true;
        bool useDragEvents => isEditorContext && supportsDragEvents;

        protected IDragAndDrop dragAndDrop => DragAndDropUtility.GetDragAndDrop(m_Target.panel);

        internal virtual bool isEditorContext
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

            m_Target.RegisterCallback<PointerDownEvent>(OnPointerDownEvent);
            m_Target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            m_Target.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            m_Target.RegisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
            m_Target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCapturedOut);

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

            m_Target.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent);
            m_Target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            m_Target.UnregisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
            m_Target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCapturedOut);
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

        // Internal for tests.
        protected internal abstract StartDragArgs StartDrag(Vector3 pointerPosition);
        protected internal abstract void UpdateDrag(Vector3 pointerPosition);
        protected internal abstract void OnDrop(Vector3 pointerPosition);

        protected abstract void ClearDragAndDropUI(bool dragCancelled);

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
            if (!useDragEvents && m_DragState == DragState.Dragging)
            {
                var target = GetDropTarget(evt.position) ?? this;
                target.UpdateDrag(evt.position);
                target.OnDrop(evt.position);
                target.ClearDragAndDropUI(false);
                evt.StopPropagation();
            }

            m_Target.ReleasePointer(evt.pointerId);
            ClearDragAndDropUI(m_DragState == DragState.Dragging);
            dragAndDrop.DragCleanup();
            m_DragState = DragState.None;
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            ClearDragAndDropUI(false);
        }

        void OnPointerCancelEvent(PointerCancelEvent evt)
        {
            if (!useDragEvents)
                ClearDragAndDropUI(true);

            m_Target.ReleasePointer(evt.pointerId);
            ClearDragAndDropUI(m_DragState == DragState.Dragging);
            dragAndDrop.DragCleanup();
            m_DragState = DragState.None;
        }

        private void OnPointerCapturedOut(PointerCaptureOutEvent evt)
        {
            // Whenever the pointer is captured by another element, like a text input, we should reset the drag state.
            if (!useDragEvents)
                ClearDragAndDropUI(true);

            ClearDragAndDropUI(m_DragState == DragState.Dragging);
            dragAndDrop.DragCleanup();
            m_DragState = DragState.None;
        }

        private void OnDragExitedEvent(DragExitedEvent evt)
        {
            if (!useDragEvents)
                return;

            var view = evt.target as BaseVerticalCollectionView;
            view?.dragger?.ClearDragAndDropUI(false);

            var target = GetDropTarget(evt.mousePosition);
            if (target != null && target != view?.dragger)
            {
                target.ClearDragAndDropUI(false);
            }

            evt.StopPropagation();
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            if (!useDragEvents)
                return;

            m_DragState = DragState.None;
            var target = GetDropTarget(evt.mousePosition);
            target?.OnDrop(evt.mousePosition);
            target?.ClearDragAndDropUI(false);

            m_Target.ReleasePointer(PointerId.mousePointerId);
            evt.StopPropagation();
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (!useDragEvents)
                return;

            var target = GetDropTarget(evt.mousePosition);
            target?.UpdateDrag(evt.mousePosition);
            if (target != this)
            {
                ClearDragAndDropUI(false);
            }

            evt.StopPropagation();
        }


        private void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (evt.isHandledByDraggable)
                return;

            if (!useDragEvents && m_DragState == DragState.Dragging)
            {
                var target = GetDropTarget(evt.position) ?? this;
                target.UpdateDrag(evt.position);
                return;
            }

            // Ignore moves if we're not on the target that started drag.
            if (m_DragState != DragState.CanStartDrag)
                return;

            var delta = m_Start - evt.position;
            if (delta.sqrMagnitude >= ScrollView.ScrollThresholdSquared)
            {
                var startDragArgs = StartDrag(m_Start);

                if (!useDragEvents)
                {
                    if (supportsDragEvents)
                    {
                        dragAndDrop.StartDrag(startDragArgs, evt.position);
                    }
                }
                // Drag can only be started by mouse events or else it will throw an error, so we leave early.
                else if (Event.current != null && Event.current.type != EventType.MouseDown && Event.current.type != EventType.MouseDrag)
                {
                    return;
                }
                else
                {
                    dragAndDrop.StartDrag(startDragArgs, evt.position);
                }

                m_DragState = DragState.Dragging;
                m_Target.CapturePointer(evt.pointerId);
                evt.isHandledByDraggable = true;
                evt.StopPropagation();
            }
        }

        DragEventsProcessor GetDropTarget(Vector2 position)
        {
            DragEventsProcessor target = null;
            if (m_Target.worldBound.Contains(position))
            {
                target = this;
            }
            else if (supportsDragEvents)
            {
                var leafTarget = m_Target.elementPanel.Pick(position);
                var targetView = leafTarget?.GetFirstOfType<BaseVerticalCollectionView>();
                target = targetView?.dragger;
            }

            return target;
        }
    }
}
