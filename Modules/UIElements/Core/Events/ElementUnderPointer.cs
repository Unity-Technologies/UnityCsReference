// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{

    class ElementUnderPointer
    {
        VisualElement[] m_PendingTopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        VisualElement[] m_TopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        private IPointerOrMouseEvent[] m_TriggerEvent = new IPointerOrMouseEvent[PointerId.maxPointers];
        private Vector2[] m_PickingPointerPositions = new Vector2[PointerId.maxPointers];

        // Some Events need to temporarily set the elementUnderPointer to a specific value to leave a predictable
        // state for an expected Event chain.
        // For example, MouseLeaveWindowEvent sets elementUnderPointer to null (see MouseEventDispatchingStrategy)
        // to ensure it generates a proper MouseLeaveWindowEvent | MouseOutEvent event chain.
        // Those temporary values should be overwritten by the next call to Panel.RecomputeTopElementUnderPointer().
        private bool[] m_IsPickingPointerTemporaries = new bool[PointerId.maxPointers];

        internal VisualElement GetTopElementUnderPointer(int pointerId, out Vector2 pickPosition, out bool isTemporary)
        {
            pickPosition = m_PickingPointerPositions[pointerId];
            isTemporary = m_IsPickingPointerTemporaries[pointerId];
            return m_PendingTopElementUnderPointer[pointerId];
        }

        internal VisualElement GetTopElementUnderPointer(int pointerId)
        {
            return m_PendingTopElementUnderPointer[pointerId];
        }


        internal void RemoveElementUnderPointer(VisualElement elementToRemove)
        {
            for (int pointerId = 0; pointerId < m_TopElementUnderPointer.Length; ++pointerId)
            {
                var previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];

                if (previousTopElementUnderPointer == elementToRemove)
                {
                    SetElementUnderPointer(null, pointerId, null);
                }
            }
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, Vector2 pointerPos)
        {
            Debug.Assert(pointerId >= 0, "SetElementUnderPointer expects pointerId >= 0");

            var previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];
            m_IsPickingPointerTemporaries[pointerId] = false;
            m_PickingPointerPositions[pointerId] = pointerPos;
            if (previousTopElementUnderPointer == newElementUnderPointer)
            {
                return;
            }

            m_PendingTopElementUnderPointer[pointerId] = newElementUnderPointer;
            m_TriggerEvent[pointerId] = null;
        }

        Vector2 GetEventPointerPosition(EventBase triggerEvent)
        {
            var pointerEvent = triggerEvent as IPointerEvent;

            if (pointerEvent != null)
            {
                return new Vector2(pointerEvent.position.x, pointerEvent.position.y);
            }

            var mouseEvt = triggerEvent as IMouseEvent;

            if (mouseEvt != null)
            {
                return mouseEvt.mousePosition;
            }

            return new Vector2(float.MinValue, float.MinValue);
        }

        internal void SetTemporaryElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, EventBase triggerEvent)
        {
            SetElementUnderPointer(newElementUnderPointer, pointerId, triggerEvent, temporary: true);
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, EventBase triggerEvent)
        {
            SetElementUnderPointer(newElementUnderPointer, pointerId, triggerEvent, temporary: false);
        }

        void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, EventBase triggerEvent, bool temporary)
        {
            Debug.Assert(pointerId >= 0, "SetElementUnderPointer expects pointerId >= 0");

            m_IsPickingPointerTemporaries[pointerId] = temporary;
            m_PickingPointerPositions[pointerId] = GetEventPointerPosition(triggerEvent);
            m_PendingTopElementUnderPointer[pointerId] = newElementUnderPointer;

            var previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];
            if (previousTopElementUnderPointer == newElementUnderPointer)
            {
                return;
            }

            if (m_TriggerEvent[pointerId] == null && triggerEvent is IPointerOrMouseEvent p)
            {
                m_TriggerEvent[pointerId] = p;
            }
        }

        internal bool CommitElementUnderPointers(EventDispatcher dispatcher, ContextType contextType)
        {
            bool elementUnderPointerChanged = false;
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                var triggerEvent = m_TriggerEvent[i];
                var previous = m_TopElementUnderPointer[i];
                var current = m_PendingTopElementUnderPointer[i];

                if (previous == current)
                {
                    if (triggerEvent != null)
                    {
                        m_PickingPointerPositions[i] = triggerEvent.position;
                    }
                    continue;
                }

                elementUnderPointerChanged = true;

                m_TopElementUnderPointer[i] = current;

                if (triggerEvent == null)
                {
                    using (new EventDispatcherGate(dispatcher))
                    {
                        Vector2 position = PointerDeviceState.GetPointerPosition(i, contextType);

                        PointerEventsHelper.SendOverOut(previous, current, null, position, i);
                        PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                            previous, current, null, position, i);

                        m_PickingPointerPositions[i] = position;
                        if (i == PointerId.mousePointerId)
                        {
                            MouseEventsHelper.SendMouseOverMouseOut(previous, current, null, position);
                            MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                previous, current, null, position);
                        }
                    }
                }

                if (triggerEvent != null)
                {
                    var pos = triggerEvent.position;
                    m_PickingPointerPositions[i] = pos;

                    if (triggerEvent is EventBase baseEvent)
                    {
                        if (baseEvent.eventTypeId == PointerMoveEvent.TypeId() ||
                            baseEvent.eventTypeId == PointerDownEvent.TypeId() ||
                            baseEvent.eventTypeId == PointerUpEvent.TypeId() ||
                            baseEvent.eventTypeId == PointerCancelEvent.TypeId())
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                PointerEventsHelper.SendOverOut(previous, current, (IPointerEvent)triggerEvent, pos, i);
                                PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                    previous, current, (IPointerEvent)triggerEvent, pos, i);

                                if (triggerEvent.pointerId == PointerId.mousePointerId &&
                                    ((IPointerEventInternal)triggerEvent).compatibilityMouseEvent is { } mouseEvent)
                                {
                                    MouseEventsHelper.SendMouseOverMouseOut(previous, current, mouseEvent, pos);
                                    MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                        previous, current, mouseEvent, pos);
                                }
                            }
                        }
                        else if (
                            baseEvent.eventTypeId == WheelEvent.TypeId())
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                MouseEventsHelper.SendMouseOverMouseOut(previous, current, (IMouseEvent)triggerEvent, pos);
                                MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                    previous, current, (IMouseEvent)triggerEvent, pos);
                            }
                        }
                        else if (baseEvent.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                                 baseEvent.eventTypeId == MouseLeaveWindowEvent.TypeId()
                                )
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                PointerEventsHelper.SendOverOut(previous, current, null, pos, i);
                                PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                    previous, current, null, pos, i);

                                if (i == PointerId.mousePointerId)
                                {
                                    MouseEventsHelper.SendMouseOverMouseOut(previous, current, (IMouseEvent)triggerEvent, pos);
                                    MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                        previous, current, (IMouseEvent)triggerEvent, pos);
                                }
                            }
                        }
                        else if (baseEvent.eventTypeId == DragUpdatedEvent.TypeId() ||
                                 baseEvent.eventTypeId == DragExitedEvent.TypeId())
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                PointerEventsHelper.SendOverOut(previous, current, null, pos, i);
                                PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                    previous, current, null, pos, i);

                                MouseEventsHelper.SendMouseOverMouseOut(previous, current, (IMouseEvent)triggerEvent, pos);
                                MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                    previous, current, (IMouseEvent)triggerEvent, pos);
                                MouseEventsHelper.SendEnterLeave<DragLeaveEvent, DragEnterEvent>(
                                    previous, current, (IMouseEvent)triggerEvent, pos);
                            }
                        }
                    }
                }

                m_TriggerEvent[i] = null;
            }
            return elementUnderPointerChanged;
        }
    }
}
