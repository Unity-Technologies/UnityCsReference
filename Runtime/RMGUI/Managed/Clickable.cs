// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    public delegate void ClickEvent();

    public class Clickable : Manipulator
    {
        public event ClickEvent OnClick;

        private readonly long m_Delay; // in milliseconds
        private readonly long m_Interval; // in milliseconds

        private Vector2 m_LastMousePos;

        // delay is used to determine when the event begins.  Applies if delay > 0.
        // interval is used to determine the time delta between event repetitions.  Applies if interval > 0.
        public Clickable(ClickEvent handler, long delay, long interval)
        {
            OnClick += handler;
            m_Delay = delay;
            m_Interval = interval;
        }

        // Click-once type constructor
        public Clickable(ClickEvent handler)
        {
            OnClick += handler;
        }

        private void OnTimer(TimerState timerState)
        {
            if (OnClick != null && IsRepeatable())
            {
                if (target.ContainsPoint(m_LastMousePos))
                {
                    OnClick();
                    target.paintFlags |= PaintFlags.Active;
                }
                else
                {
                    target.paintFlags &= ~PaintFlags.Active;
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
                    this.TakeCapture();
                    if (IsRepeatable())
                    {
                        this.Schedule(OnTimer)
                        .StartingIn(m_Delay)
                        .Every(m_Interval);
                    }
                    target.paintFlags |= PaintFlags.Active;
                    m_LastMousePos = evt.mousePosition;
                    return EventPropagation.Stop;

                case EventType.MouseUp:
                    if (this.HasCapture())
                    {
                        if (IsRepeatable())
                        {
                            target.Unschedule(OnTimer);
                        }
                        this.ReleaseCapture();

                        // TODO: if repeatable and we have repeated we will do one last extra click...
                        if (OnClick != null && target.ContainsPoint(target.ChangeCoordinatesTo(target.parent, evt.mousePosition)))
                        {
                            OnClick();
                        }
                        target.paintFlags &= ~PaintFlags.Active;
                        return EventPropagation.Stop;
                    }
                    break;

                case EventType.MouseDrag:
                    if (this.HasCapture())
                    {
                        m_LastMousePos = evt.mousePosition;
                        return EventPropagation.Stop;
                    }
                    break;
            }
            return EventPropagation.Continue;
        }
    }
}
