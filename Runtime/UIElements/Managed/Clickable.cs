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

        private IVisualElementScheduledItem m_Repeater;

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
                var localMousePosition = target.ChangeCoordinatesTo(target.shadow.parent, lastMousePosition);
                if (target.ContainsPoint(localMousePosition))
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

        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                target.TakeCapture();
                lastMousePosition = evt.localMousePosition;

                if (IsRepeatable())
                {
                    // Repeatable button clicks are performed on the MouseDown and at timer events
                    var localMousePosition = target.ChangeCoordinatesTo(target.shadow.parent, evt.localMousePosition);
                    if (clicked != null && target.ContainsPoint(localMousePosition))
                    {
                        clicked();
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

                evt.StopPropagation();
            }
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (target.HasCapture())
            {
                lastMousePosition = evt.localMousePosition;
                evt.StopPropagation();
            }
        }

        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (CanStopManipulation(evt))
            {
                target.ReleaseCapture();

                if (IsRepeatable())
                {
                    // Repeatable button clicks are performed on the MouseDown and at timer events only
                    if (m_Repeater != null)
                    {
                        m_Repeater.Pause();
                    }
                }
                else
                {
                    // Non repeatable button clicks are performed on the MouseUp
                    if (clicked != null && target.ContainsPoint(target.ChangeCoordinatesTo(target.shadow.parent, evt.localMousePosition)))
                    {
                        clicked();
                    }
                }
                target.pseudoStates &= ~PseudoStates.Active;
                evt.StopPropagation();
            }
        }
    }
}
