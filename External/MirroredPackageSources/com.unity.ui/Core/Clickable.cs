namespace UnityEngine.UIElements
{
    public class Clickable : PointerManipulator
    {
        public event System.Action<EventBase> clickedWithEventInfo;
        public event System.Action clicked;

        private readonly long m_Delay; // in milliseconds
        private readonly long m_Interval; // in milliseconds

        protected bool active { get; set; }

        public Vector2 lastMousePosition { get; private set; }

        private IVisualElementScheduledItem m_Repeater;

        // delay is used to determine when the event begins.  Applies if delay > 0.
        // interval is used to determine the time delta between event repetitions.  Applies if interval > 0.
        public Clickable(System.Action handler, long delay, long interval) : this(handler)
        {
            m_Delay = delay;
            m_Interval = interval;
            active = false;
        }

        public Clickable(System.Action<EventBase> handler)
        {
            clickedWithEventInfo = handler;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        // Click-once type constructor
        public Clickable(System.Action handler)
        {
            clicked = handler;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            active = false;
        }

        private void OnTimer(TimerState timerState)
        {
            if ((clicked != null || clickedWithEventInfo != null) && IsRepeatable())
            {
                if (target.ContainsPoint(lastMousePosition))
                {
                    Invoke(null);

                    target.pseudoStates |= PseudoStates.Active;
                }
                else
                {
                    target.pseudoStates &= ~PseudoStates.Active;
                }
            }
        }

        private bool IsRepeatable()
        {
            return (m_Delay > 0 || m_Interval > 0);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        protected void Invoke(EventBase evt)
        {
            clicked?.Invoke();
            clickedWithEventInfo?.Invoke(evt);
        }

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
                ProcessDownEvent(evt, evt.localMousePosition, PointerId.mousePointerId);
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (active)
                ProcessMoveEvent(evt, evt.localMousePosition);
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (active && CanStopManipulation(evt))
                ProcessUpEvent(evt, evt.localMousePosition, PointerId.mousePointerId);
        }

        protected virtual void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            active = true;
            target.CapturePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(PointerId.mousePointerId);

            lastMousePosition = localPosition;
            if (IsRepeatable())
            {
                // Repeatable button clicks are performed on the MouseDown and at timer events
                if (target.ContainsPoint(localPosition))
                {
                    Invoke(evt);
                }

                if (m_Repeater == null)
                {
                    m_Repeater = target.schedule.Execute(OnTimer).Every(m_Interval).StartingIn(m_Delay);
                }
                else
                {
                    m_Repeater.ExecuteLater(m_Delay);
                }
            }

            target.pseudoStates |= PseudoStates.Active;

            evt.StopImmediatePropagation();
        }

        protected virtual void ProcessMoveEvent(EventBase evt, Vector2 localPosition)
        {
            lastMousePosition = localPosition;

            if (target.ContainsPoint(localPosition))
            {
                target.pseudoStates |= PseudoStates.Active;
            }
            else
            {
                target.pseudoStates &= ~PseudoStates.Active;
            }

            evt.StopPropagation();
        }

        protected virtual void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            active = false;
            target.ReleasePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(PointerId.mousePointerId);

            target.pseudoStates &= ~PseudoStates.Active;

            if (IsRepeatable())
            {
                // Repeatable button clicks are performed on the MouseDown and at timer events only
                m_Repeater?.Pause();
            }
            else
            {
                // Non repeatable button clicks are performed on the MouseUp
                if (target.ContainsPoint(localPosition))
                {
                    Invoke(evt);
                }
            }

            evt.StopPropagation();
        }
    }
}
