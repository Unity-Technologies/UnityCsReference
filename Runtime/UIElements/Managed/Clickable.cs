// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class Clickable : MouseManipulator
    {
        public event System.Action clicked;

        private readonly long m_Delay; // in milliseconds
        private readonly long m_Interval; // in milliseconds

        public Vector2 lastMousePosition { get; private set; }

        // delay is used to determine when the event begins.  Applies if delay > 0.
        // interval is used to determine the time delta between event repetitions.  Applies if interval > 0.
        public Clickable(System.Action handler, long delay, long interval) : this(handler)
        {
            m_Delay = delay;
            m_Interval = interval;
        }

        // Click-once type constructor
        public Clickable(System.Action handler)
        {
            clicked = handler;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        private void OnTimer(TimerState timerState)
        {
            if (clicked != null && IsRepeatable())
            {
                if (target.ContainsPointToLocal(lastMousePosition))
                {
                    clicked();
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

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (CanStartManipulation(evt))
                    {
                        this.TakeCapture();
                        lastMousePosition = evt.mousePosition;
                        if (IsRepeatable())
                        {
                            // Repeatable button clicks are performed on the MouseDown and at timer events
                            if (clicked != null && target.ContainsPointToLocal(evt.mousePosition))
                                clicked();

                            this.Schedule(OnTimer)
                            .StartingIn(m_Delay)
                            .Every(m_Interval);
                        }
                        target.pseudoStates |= PseudoStates.Active;
                        return EventPropagation.Stop;
                    }
                    break;

                case EventType.MouseUp:
                    if (CanStopManipulation(evt))
                    {
                        this.ReleaseCapture();

                        if (IsRepeatable())
                        {
                            // Repeatable button clicks are performed on the MouseDown and at timer events only
                            target.Unschedule(OnTimer);
                        }
                        else
                        {
                            // Non repeatable button clicks are performed on the MouseUp
                            if (clicked != null && target.ContainsPointToLocal(evt.mousePosition))
                                clicked();
                        }
                        target.pseudoStates &= ~PseudoStates.Active;
                        return EventPropagation.Stop;
                    }
                    break;

                case EventType.MouseDrag:
                    if (this.HasCapture())
                    {
                        lastMousePosition = evt.mousePosition;
                        return EventPropagation.Stop;
                    }
                    break;
            }
            return EventPropagation.Continue;
        }
    }
}
