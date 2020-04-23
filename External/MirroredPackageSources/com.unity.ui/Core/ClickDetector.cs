using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class ClickDetector
    {
        private class ButtonClickStatus
        {
            public VisualElement m_Target;
            public Vector3 m_PointerDownPosition;
            public long m_LastPointerDownTime;
            public int m_ClickCount;

            public void Reset()
            {
                m_Target = null;
                m_ClickCount = 0;
                m_LastPointerDownTime = 0;
                m_PointerDownPosition = Vector3.zero;
            }
        }

        private List<ButtonClickStatus> m_ClickStatus;

        // In milliseconds
        internal static int s_DoubleClickTime { get; set; } = -1;

        public ClickDetector()
        {
            m_ClickStatus = new List<ButtonClickStatus>(PointerId.maxPointers);
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                m_ClickStatus.Add(new ButtonClickStatus());
            }

            if (s_DoubleClickTime == -1)
            {
                s_DoubleClickTime = Event.GetDoubleClickTime();
            }
        }

        void StartClickTracking(EventBase evt)
        {
            IPointerEvent pe = evt as IPointerEvent;
            if (pe == null)
            {
                return;
            }

            var clickStatus = m_ClickStatus[pe.pointerId];

            var newTarget = evt.target as VisualElement;

            if (newTarget != clickStatus.m_Target)
            {
                clickStatus.Reset();
            }

            clickStatus.m_Target = newTarget;

            if (evt.timestamp - clickStatus.m_LastPointerDownTime > s_DoubleClickTime)
            {
                clickStatus.m_ClickCount = 1;
            }
            else
            {
                clickStatus.m_ClickCount++;
            }

            clickStatus.m_LastPointerDownTime = evt.timestamp;
            clickStatus.m_PointerDownPosition = pe.position;
        }

        void SendClickEvent(EventBase evt)
        {
            IPointerEvent pe = evt as IPointerEvent;
            if (pe == null)
            {
                return;
            }

            var clickStatus = m_ClickStatus[pe.pointerId];

            // Filter out event where button is released outside the window.
            var element = evt.target as VisualElement;
            if (element != null && element.worldBound.Contains(pe.position))
            {
                if (clickStatus.m_Target != null && clickStatus.m_ClickCount > 0)
                {
                    var target = clickStatus.m_Target.FindCommonAncestor(evt.target as VisualElement);
                    if (target != null)
                    {
                        using (var clickEvent = ClickEvent.GetPooled(evt as PointerUpEvent, clickStatus.m_ClickCount))
                        {
                            clickEvent.target = target;
                            target.SendEvent(clickEvent);
                        }
                    }
                }
            }
        }

        void CancelClickTracking(EventBase evt)
        {
            IPointerEvent pe = evt as IPointerEvent;
            if (pe == null)
            {
                return;
            }
            var clickStatus = m_ClickStatus[pe.pointerId];
            clickStatus.Reset();
        }

        public void ProcessEvent(EventBase evt)
        {
            IPointerEvent pe = evt as IPointerEvent;
            if (pe == null)
            {
                return;
            }

            if (evt.eventTypeId == PointerDownEvent.TypeId() && pe.button == 0)
            {
                StartClickTracking(evt);
            }
            else if (evt.eventTypeId == PointerMoveEvent.TypeId())
            {
                // Button 1 pressed while another button was already pressed.
                if (pe.button == 0 && (pe.pressedButtons & 1) == 1)
                {
                    StartClickTracking(evt);
                }
                // Button 1 released while another button is still pressed.
                else if (pe.button == 0 && (pe.pressedButtons & 1) == 0)
                {
                    SendClickEvent(evt);
                }
                // Pointer moved or other button pressed/released
                else
                {
                    var clickStatus = m_ClickStatus[pe.pointerId];
                    if (clickStatus.m_Target != null)
                    {
                        // stop the multi-click sequence on move
                        clickStatus.m_LastPointerDownTime = 0;
                    }
                }
            }
            else if (evt.eventTypeId == PointerCancelEvent.TypeId() ||
                     evt.eventTypeId == PointerStationaryEvent.TypeId() ||
                     evt.eventTypeId == DragUpdatedEvent.TypeId())
            {
                CancelClickTracking(evt);
            }
            else if (evt.eventTypeId == PointerUpEvent.TypeId() && pe.button == 0)
            {
                SendClickEvent(evt);
            }
        }
    }
}
