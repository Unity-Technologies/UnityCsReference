// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements
{
    internal abstract partial class DragEventsProcessor
    {
        internal enum DragState
        {
            None,
            CanStartDrag,
            Dragging
        }

        // A drag-and-drop client is shared across the views of its context (one per editor/player),
        // so only one drag operation can exist at a time per client: starting or cleaning up
        // another would corrupt its payload. Owners are tracked per client so a drag in one
        // context does not block drags in the other.
        [AutoStaticsCleanupOnCodeReload]
        static Dictionary<IDragAndDrop, DragEventsProcessor> s_CurrentDragOwners = new();

        bool m_IsRegistered;
        DragState m_DragState;
        int m_ActivePointerId = PointerId.invalidPointerId;
        Vector3 m_Start;
        bool m_PendingPerformDrag;

        protected readonly VisualElement m_Target;

        // Used in tests
        internal bool isRegistered => m_IsRegistered;
        internal DragState dragState => m_DragState;
        internal bool pendingPerformDrag => m_PendingPerformDrag;

        protected virtual bool supportsDragEvents => true;
        bool useDragEvents => isEditorContext && supportsDragEvents;

        protected internal virtual IDragAndDrop dragAndDrop => DragAndDropUtility.GetDragAndDrop(m_Target.panel);

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
            m_Target.RegisterCallback<PointerOutEvent>(OnPointerOutEvent);
            // Register to event when display style is changed to clear any drag UI.
            m_Target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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
            ReleaseDragOwnership();

            m_Target.UnregisterCallback<PointerDownEvent>(OnPointerDownEvent);
            m_Target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
            m_Target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveEvent);
            m_Target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            m_Target.UnregisterCallback<PointerCancelEvent>(OnPointerCancelEvent);
            m_Target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCapturedOut);
            m_Target.UnregisterCallback<PointerOutEvent>(OnPointerOutEvent);
            m_Target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_Target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            m_Target.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);

            if (unregisterPanelEvents)
            {
                m_Target.UnregisterCallback<AttachToPanelEvent>(RegisterCallbacksFromTarget);
                m_Target.UnregisterCallback<DetachFromPanelEvent>(UnregisterCallbacksFromTarget);
            }
        }

        protected abstract bool CanStartDrag(Vector3 pointerPosition, EventModifiers modifiers);

        protected virtual bool ShouldHandlePointerDown(PointerDownEvent evt) => true;

        // Internal for tests.
        protected internal abstract StartDragArgs StartDrag(Vector3 pointerPosition, EventModifiers modifiers);
        protected internal abstract void UpdateDrag(Vector3 pointerPosition, EventModifiers modifiers);
        protected internal abstract void OnDrop(Vector3 pointerPosition, EventModifiers modifiers);

        protected abstract void ClearDragAndDropUI(bool dragCancelled);

        // Only one drag gesture can be tracked at a time; while one is in progress, events from
        // other pointers (e.g. a second finger on touch devices, UUM-117689) must not affect it.
        bool IsForeignPointer(int pointerId)
        {
            return m_DragState != DragState.None && pointerId != m_ActivePointerId;
        }

        bool IsDragOwnedByAnother()
        {
            return s_CurrentDragOwners.TryGetValue(dragAndDrop, out var owner) && owner != this;
        }

        void ReleaseDragOwnership()
        {
            // Looked up by value: the client needs the panel, which may be gone by detach time.
            foreach (var pair in s_CurrentDragOwners)
            {
                if (pair.Value == this)
                {
                    s_CurrentDragOwners.Remove(pair.Key);
                    return;
                }
            }
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            // Only an actual drag is protected from other pointers. A mere drag candidate can be
            // taken over by any pointer going down, so a device that vanished mid-press (e.g.
            // unplugged) can never block the next pointer from starting a drag.
            if (m_DragState == DragState.Dragging && evt.pointerId != m_ActivePointerId)
                return;

            if (evt.button != (int)MouseButton.LeftMouse || !ShouldHandlePointerDown(evt))
            {
                m_DragState = DragState.None;
                m_ActivePointerId = PointerId.invalidPointerId;
                ReleaseDragOwnership();
                return;
            }

            // The dragging pointer is already pressed, so a left-button down from it is a spurious
            // duplicate from the input backend; acting on it would restart the drag mid-flight.
            if (m_DragState == DragState.Dragging)
                return;

            if (CanStartDrag(evt.position, evt.modifiers))
            {
                m_DragState = DragState.CanStartDrag;
                m_ActivePointerId = evt.pointerId;
                m_Start = evt.position;
                m_PendingPerformDrag = false;
            }
        }

        private void OnPointerOutEvent(PointerOutEvent evt)
        {
            // The sqrMagnitude will sometimes be less than 100 when dragging an item near the end of the target (ListView/TreeView).
            // For this special occasion, we want to set a flag that will allow the next move to bypass the threshold.
            // Note that on mobile devices, this event would only happen when dragging an item, which at that stage, the
            // dragging has already happened.
            if (m_DragState == DragState.CanStartDrag && evt.pointerId == m_ActivePointerId)
            {
                if ((m_Start - evt.position).sqrMagnitude <= 0)
                    return;

                m_PendingPerformDrag = true;
                evt.StopPropagation();
            }
        }

        internal void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (IsForeignPointer(evt.pointerId))
                return;

            if (!useDragEvents && m_DragState == DragState.Dragging)
            {
                var target = GetDropTarget(evt.position) ?? this;
                target.UpdateDrag(evt.position, evt.modifiers);
                target.OnDrop(evt.position, evt.modifiers);
                target.ClearDragAndDropUI(false);
                evt.StopPropagation();
            }

            m_Target.ReleasePointer(evt.pointerId);
            ClearDragAndDropUI(m_DragState == DragState.Dragging);
            ReleaseDragOwnership();
            if (!s_CurrentDragOwners.ContainsKey(dragAndDrop))
                dragAndDrop.DragCleanup();
            m_DragState = DragState.None;
            m_ActivePointerId = PointerId.invalidPointerId;
            m_PendingPerformDrag = false;
        }

        private void OnPointerLeaveEvent(PointerLeaveEvent evt)
        {
            if (IsForeignPointer(evt.pointerId))
                return;

            ClearDragAndDropUI(false);
        }

        void OnPointerCancelEvent(PointerCancelEvent evt)
        {
            if (IsForeignPointer(evt.pointerId))
                return;

            CancelDragAndDrop(evt.pointerId);
        }

        private void OnPointerCapturedOut(PointerCaptureOutEvent evt)
        {
            if (IsForeignPointer(evt.pointerId))
                return;

            CancelDragAndDrop();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Clear any drag UI when the target becomes invisible.
            if (m_Target.resolvedStyle.display == DisplayStyle.None)
            {
                CancelDragAndDrop();
            }
        }

        void CancelDragAndDrop(int releaseCapturePointerId = -1)
        {
            if (m_DragState == DragState.None && !m_PendingPerformDrag)
                return;

            // Whenever the pointer is captured by another element, like a text input, we should reset the drag state.
            if (!useDragEvents)
                ClearDragAndDropUI(true);

            if (releaseCapturePointerId != -1)
                m_Target.ReleasePointer(releaseCapturePointerId);
            ClearDragAndDropUI(m_DragState == DragState.Dragging);
            ReleaseDragOwnership();
            if (!s_CurrentDragOwners.ContainsKey(dragAndDrop))
                dragAndDrop.DragCleanup();
            m_DragState = DragState.None;
            m_ActivePointerId = PointerId.invalidPointerId;
            m_PendingPerformDrag = false;
        }

        private void OnDragExitedEvent(DragExitedEvent evt)
        {
            if (!useDragEvents)
                return;

            // The matching pointer up is consumed by the editor drag-and-drop operation, so this
            // is where an exited drag ends. It ends as a cancel, undoing the drag's side effects
            // (e.g. a tree's automatic expansions), and its state is cleared, or the next press
            // would be read as part of it.
            if (m_DragState == DragState.Dragging)
                ClearDragAndDropUI(true);
            m_DragState = DragState.None;
            m_ActivePointerId = PointerId.invalidPointerId;
            m_PendingPerformDrag = false;
            ReleaseDragOwnership();
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
            m_ActivePointerId = PointerId.invalidPointerId;
            ReleaseDragOwnership();
            var target = GetDropTarget(evt.mousePosition);
            target?.OnDrop(evt.mousePosition, evt.modifiers);
            target?.ClearDragAndDropUI(false);

            m_Target.ReleasePointer(PointerId.mousePointerId);
            m_PendingPerformDrag = false;
            evt.StopPropagation();
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (!useDragEvents)
                return;

            var target = GetDropTarget(evt.mousePosition);
            target?.UpdateDrag(evt.mousePosition, evt.modifiers);
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

            if (IsForeignPointer(evt.pointerId))
                return;

            if (!useDragEvents && m_DragState == DragState.Dragging)
            {
                var target = GetDropTarget(evt.position) ?? this;
                target.UpdateDrag(evt.position, evt.modifiers);
                m_PendingPerformDrag = false;
                return;
            }

            // Ignore moves if we're not on the target that started drag.
            if (m_DragState != DragState.CanStartDrag)
                return;

            // A popup opened mid-gesture can consume the PointerUp; don't start a drag if the button is no longer held.
            if ((evt.pressedButtons & (1 << (int)MouseButton.LeftMouse)) == 0)
            {
                m_DragState = DragState.None;
                m_ActivePointerId = PointerId.invalidPointerId;
                m_PendingPerformDrag = false;
                return;
            }

            var delta = m_Start - evt.position;
            if (delta.sqrMagnitude >= ScrollView.ScrollThresholdSquared || m_PendingPerformDrag)
            {
                // A drag in another view already owns the shared drag-and-drop client.
                if (IsDragOwnedByAnother())
                    return;

                var startDragArgs = StartDrag(m_Start, evt.modifiers);

                if (startDragArgs.visualMode is DragVisualMode.Rejected)
                {
                    m_DragState = DragState.None;
                    m_ActivePointerId = PointerId.invalidPointerId;
                    return;
                }

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
                s_CurrentDragOwners[dragAndDrop] = this;
                m_Target.CapturePointer(evt.pointerId);
                evt.isHandledByDraggable = true;
                m_PendingPerformDrag = false;
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
