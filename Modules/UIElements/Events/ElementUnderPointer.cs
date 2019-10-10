// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class ElementUnderPointer
    {
        VisualElement[] m_PendingTopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        VisualElement[] m_TopElementUnderPointer = new VisualElement[PointerId.maxPointers];
        private IPointerEvent[] m_TriggerPointerEvent = new IPointerEvent[PointerId.maxPointers];
        private IMouseEvent[] m_TriggerMouseEvent = new IMouseEvent[PointerId.maxPointers];

        internal VisualElement GetTopElementUnderPointer(int pointerId)
        {
            return m_PendingTopElementUnderPointer[pointerId];
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId)
        {
            Debug.Assert(pointerId >= 0);

            VisualElement previousTopElementUnderPointer = m_TopElementUnderPointer[pointerId];
            if (newElementUnderPointer == previousTopElementUnderPointer)
            {
                return;
            }

            m_PendingTopElementUnderPointer[pointerId] = newElementUnderPointer;
            m_TriggerPointerEvent[pointerId] = null;
            m_TriggerMouseEvent[pointerId] = null;
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, EventBase triggerEvent)
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
                if (m_TopElementUnderPointer[i] == m_PendingTopElementUnderPointer[i])
                {
                    continue;
                }

                if (m_TriggerPointerEvent[i] == null && m_TriggerMouseEvent[i] == null)
                {
                    using (new EventDispatcherGate(dispatcher))
                    {
                        Vector2 position = PointerDeviceState.GetPointerPosition(i);

                        PointerEventsHelper.SendOverOut(m_TopElementUnderPointer[i],
                            m_PendingTopElementUnderPointer[i], null,
                            position, i);
                        PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                            m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i], null, position, i);

                        if (i == PointerId.mousePointerId)
                        {
                            MouseEventsHelper.SendMouseOverMouseOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i],
                                null, position);
                            MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i], null, position);
                        }
                    }
                }

                if (m_TriggerPointerEvent[i] != null)
                {
                    if ((m_TriggerPointerEvent[i] as EventBase)?.eventTypeId == PointerMoveEvent.TypeId() ||
                        (m_TriggerPointerEvent[i] as EventBase)?.eventTypeId == PointerDownEvent.TypeId() ||
                        (m_TriggerPointerEvent[i] as EventBase)?.eventTypeId == PointerUpEvent.TypeId() ||
                        (m_TriggerPointerEvent[i] as EventBase)?.eventTypeId == PointerCancelEvent.TypeId())
                    {
                        using (new EventDispatcherGate(dispatcher))
                        {
                            PointerEventsHelper.SendOverOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i],
                                m_TriggerPointerEvent[i], m_TriggerPointerEvent[i].position, i);
                            PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i],
                                m_TriggerPointerEvent[i], m_TriggerPointerEvent[i].position, i);
                        }
                    }

                    m_TriggerPointerEvent[i] = null;
                }

                if (m_TriggerMouseEvent[i] != null)
                {
                    if ((m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == MouseMoveEvent.TypeId() ||
                        (m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == MouseDownEvent.TypeId() ||
                        (m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == MouseUpEvent.TypeId() ||
                        (m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == WheelEvent.TypeId())
                    {
                        using (new EventDispatcherGate(dispatcher))
                        {
                            MouseEventsHelper.SendMouseOverMouseOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i],
                                m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                            MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i],
                                m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                        }
                    }
                    else if ((m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == MouseEnterWindowEvent.TypeId() ||
                             (m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == MouseLeaveWindowEvent.TypeId()
                    )
                    {
                        using (new EventDispatcherGate(dispatcher))
                        {
                            PointerEventsHelper.SendOverOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i], null,
                                m_TriggerMouseEvent[i].mousePosition, i);
                            PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i], null,
                                m_TriggerMouseEvent[i].mousePosition, i);

                            if (i == PointerId.mousePointerId)
                            {
                                MouseEventsHelper.SendMouseOverMouseOut(m_TopElementUnderPointer[i],
                                    m_PendingTopElementUnderPointer[i],
                                    m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                                MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                    m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i],
                                    m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                            }
                        }
                    }
                    else if ((m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == DragUpdatedEvent.TypeId() ||
                             (m_TriggerMouseEvent[i] as EventBase)?.eventTypeId == DragExitedEvent.TypeId())
                    {
                        using (new EventDispatcherGate(dispatcher))
                        {
                            PointerEventsHelper.SendOverOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i], null,
                                m_TriggerMouseEvent[i].mousePosition, i);
                            PointerEventsHelper.SendEnterLeave<PointerLeaveEvent, PointerEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i], null,
                                m_TriggerMouseEvent[i].mousePosition, i);

                            MouseEventsHelper.SendMouseOverMouseOut(m_TopElementUnderPointer[i],
                                m_PendingTopElementUnderPointer[i],
                                m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                            MouseEventsHelper.SendEnterLeave<MouseLeaveEvent, MouseEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i],
                                m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                            MouseEventsHelper.SendEnterLeave<DragLeaveEvent, DragEnterEvent>(
                                m_TopElementUnderPointer[i], m_PendingTopElementUnderPointer[i],
                                m_TriggerMouseEvent[i], m_TriggerMouseEvent[i].mousePosition);
                        }
                    }

                    m_TriggerMouseEvent[i] = null;
                }

                m_TopElementUnderPointer[i] = m_PendingTopElementUnderPointer[i];
            }
        }
    }
}
