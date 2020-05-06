namespace UnityEngine.UIElements
{
    class ElementUnderPointer
    {
        VisualElement[] m_PendingTopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        VisualElement[] m_TopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        private IPointerEvent[] m_TriggerPointerEvent = new IPointerEvent[PointerId.maxPointers];
        private IMouseEvent[] m_TriggerMouseEvent = new IMouseEvent[PointerId.maxPointers];
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

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, Vector2 pointerPos)
        {
            Debug.Assert(pointerId >= 0);

            VisualElement previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];
            m_IsPickingPointerTemporaries[pointerId] = false;
            m_PickingPointerPositions[pointerId] = pointerPos;
            if (newElementUnderPointer == previousTopElementUnderPointer)
            {
                return;
            }

            m_PendingTopElementUnderPointer[pointerId] = newElementUnderPointer;
            m_TriggerPointerEvent[pointerId] = null;
            m_TriggerMouseEvent[pointerId] = null;
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

        internal void SetTemporaryElementUnderPointer(VisualElement newElementUnderPointer, EventBase triggerEvent)
        {
            SetElementUnderPointer(newElementUnderPointer, triggerEvent, temporary: true);
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, EventBase triggerEvent)
        {
            SetElementUnderPointer(newElementUnderPointer, triggerEvent, temporary: false);
        }

        void SetElementUnderPointer(VisualElement newElementUnderPointer, EventBase triggerEvent, bool temporary)
        {
            int pointerId = -1;
            if (triggerEvent is IPointerEvent)
            {
                pointerId = ((IPointerEvent)triggerEvent).pointerId;
            }
            else if (triggerEvent is IMouseEvent)
            {
                pointerId = PointerId.mousePointerId;
            }

            Debug.Assert(pointerId >= 0);

            m_IsPickingPointerTemporaries[pointerId] = temporary;
            m_PickingPointerPositions[pointerId] = GetEventPointerPosition(triggerEvent);

            VisualElement previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];
            if (newElementUnderPointer == previousTopElementUnderPointer)
            {
                return;
            }

            m_PendingTopElementUnderPointer[pointerId] = newElementUnderPointer;
            if (m_TriggerPointerEvent[pointerId] == null && triggerEvent is IPointerEvent)
            {
                m_TriggerPointerEvent[pointerId] = triggerEvent as IPointerEvent;
            }

            if (m_TriggerMouseEvent[pointerId] == null && triggerEvent is IMouseEvent)
            {
                m_TriggerMouseEvent[pointerId] = triggerEvent as IMouseEvent;
            }
        }

        internal void CommitElementUnderPointers(EventDispatcher dispatcher)
        {
            for (var i = 0; i < m_TopElementUnderPointer.Length; i++)
            {
                var triggerPointerEvent = m_TriggerPointerEvent[i];
                var previous = m_TopElementUnderPointer[i];
                var current = m_PendingTopElementUnderPointer[i];

                if (current == previous)
                {
                    if (triggerPointerEvent != null)
                    {
                        var pos3d = triggerPointerEvent.position;
                        m_PickingPointerPositions[i] = new Vector2(pos3d.x, pos3d.y);
                    }
                    else if (m_TriggerMouseEvent[i] != null)
                    {
                        m_PickingPointerPositions[i] = m_TriggerMouseEvent[i].mousePosition;
                    }

                    continue;
                }

                m_TopElementUnderPointer[i] = current;

                if (triggerPointerEvent == null && m_TriggerMouseEvent[i] == null)
                {
                    using (new EventDispatcherGate(dispatcher))
                    {
                        Vector2 position = PointerDeviceState.GetPointerPosition(i);

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

                if (triggerPointerEvent != null)
                {
                    var pos3d = triggerPointerEvent.position;
                    m_PickingPointerPositions[i] = new Vector2(pos3d.x, pos3d.y);

                    var baseEvent = triggerPointerEvent as EventBase;
                    if (baseEvent != null && (
                        baseEvent.eventTypeId == PointerMoveEvent.TypeId() ||
                        baseEvent.eventTypeId == PointerDownEvent.TypeId() ||
                        baseEvent.eventTypeId == PointerUpEvent.TypeId() ||
                        baseEvent.eventTypeId == PointerCancelEvent.TypeId()))
                    {
                        using (new EventDispatcherGate(dispatcher))
                        {
                            PointerEventsHelper.SendOverOut(previous, current, triggerPointerEvent, pos3d, i);
                            PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                previous, current, triggerPointerEvent, pos3d, i);
                        }
                    }
                }

                m_TriggerPointerEvent[i] = null;

                var triggerMouseEvent = m_TriggerMouseEvent[i];
                if (triggerMouseEvent != null)
                {
                    Vector2 mousePos = triggerMouseEvent.mousePosition;
                    m_PickingPointerPositions[i] = mousePos;
                    var baseEvent = triggerMouseEvent as EventBase;
                    if (baseEvent != null)
                    {
                        if (baseEvent.eventTypeId == MouseMoveEvent.TypeId() ||
                            baseEvent.eventTypeId == MouseDownEvent.TypeId() ||
                            baseEvent.eventTypeId == MouseUpEvent.TypeId() ||
                            baseEvent.eventTypeId == WheelEvent.TypeId())
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                MouseEventsHelper.SendMouseOverMouseOut(previous, current, triggerMouseEvent, mousePos);
                                MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                    previous, current, triggerMouseEvent, mousePos);
                            }
                        }
                        else if (baseEvent.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                                 baseEvent.eventTypeId == MouseLeaveWindowEvent.TypeId()
                        )
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                PointerEventsHelper.SendOverOut(previous, current, null, mousePos, i);
                                PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                    previous, current, null, mousePos, i);

                                if (i == PointerId.mousePointerId)
                                {
                                    MouseEventsHelper.SendMouseOverMouseOut(previous, current, triggerMouseEvent, mousePos);
                                    MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                        previous, current, triggerMouseEvent, mousePos);
                                }
                            }
                        }
                        else if (baseEvent.eventTypeId == DragUpdatedEvent.TypeId() ||
                                 baseEvent.eventTypeId == DragExitedEvent.TypeId())
                        {
                            using (new EventDispatcherGate(dispatcher))
                            {
                                PointerEventsHelper.SendOverOut(previous, current, null, mousePos, i);
                                PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                    previous, current, null, mousePos, i);

                                MouseEventsHelper.SendMouseOverMouseOut(previous, current, triggerMouseEvent, mousePos);
                                MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                    previous, current, triggerMouseEvent, mousePos);
                                MouseEventsHelper.SendEnterLeave<DragLeaveEvent, DragEnterEvent>(
                                    previous, current, triggerMouseEvent, mousePos);
                            }
                        }
                    }

                    m_TriggerMouseEvent[i] = null;
                }
            }
        }
    }
}
