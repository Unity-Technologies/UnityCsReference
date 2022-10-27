// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

            var newTarget = evt.elementTarget;

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
            var element = evt.elementTarget;
            if (element != null && ContainsPointer(element, pe.position))
            {
                if (clickStatus.m_Target != null && clickStatus.m_ClickCount > 0)
                {
                    var target = clickStatus.m_Target.FindCommonAncestor(evt.elementTarget);
                    if (target != null)
                    {
                        using (var clickEvent = ClickEvent.GetPooled(pe, clickStatus.m_ClickCount))
                        {
                            clickEvent.elementTarget = target;
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

        public void ProcessEvent<TEvent>(PointerEventBase<TEvent> evt)
            where TEvent : PointerEventBase<TEvent>, new()
        {
            if (evt.eventTypeId == PointerDownEvent.TypeId() && evt.button == 0)
            {
                StartClickTracking(evt);
            }
            else if (evt.eventTypeId == PointerMoveEvent.TypeId())
            {
                // Button 1 pressed while another button was already pressed.
                if (evt.button == 0 && (evt.pressedButtons & 1) == 1)
                {
                    StartClickTracking(evt);
                }
                // Button 1 released while another button is still pressed.
                else if (evt.button == 0 && (evt.pressedButtons & 1) == 0)
                {
                    SendClickEvent(evt);
                }
                // Pointer moved or other button pressed/released
                else
                {
                    var clickStatus = m_ClickStatus[evt.pointerId];
                    if (clickStatus.m_Target != null)
                    {
                        // stop the multi-click sequence on move
                        clickStatus.m_LastPointerDownTime = 0;
                    }
                }
            }
            else if (evt.eventTypeId == PointerCancelEvent.TypeId())
            {
                //TODO: #if UNITY_EDITOR maybe we need to react to DragUpdatedEvent too by calling CancelClickTracking
                CancelClickTracking(evt);

                // Note that we don't cancel the click when we have a PointerStationaryEvent anymore. Touch stationary
                // events are sent on each frame where the touch doesn't move, starting immediately after the frame
                // where the touch begin event occured. If we want to cancel the ClickEvent after the touch has been
                // idle for some time, then we need to manually track the duration of the stationary phase.
            }
            else if (evt.eventTypeId == PointerUpEvent.TypeId() && evt.button == 0)
            {
                SendClickEvent(evt);
            }
        }

        private static bool ContainsPointer(VisualElement element, Vector2 position)
        {
            if (!element.worldBound.Contains(position) || element.panel == null)
                return false;

            // We need to use Pick(position) because PointerUpEvent sometimes calls ClearCachedElementUnderPointer
            var elementUnderPointer = element.panel.Pick(position);
            return element == elementUnderPointer || element.Contains(elementUnderPointer);
        }
    }
}
