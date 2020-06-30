using JetBrains.Annotations;

namespace UnityEngine.UIElements
{
    interface IEventInterpreter
    {
        bool IsActivationEvent(EventBase evt);
        bool IsCancellationEvent(EventBase evt);
        bool IsNavigationEvent(EventBase evt, out NavigationDirection direction);
    }

    enum NavigationDirection
    {
        None,
        Previous, Next,
        Left, Right, Up, Down,
        PageUp, PageDown, Home, End
    }

    class EventInterpreter : IEventInterpreter
    {
        internal static readonly EventInterpreter s_Instance = new EventInterpreter();

        public virtual bool IsActivationEvent(EventBase evt)
        {
            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                var keyDownEvent = (KeyDownEvent)evt;
                return keyDownEvent.keyCode == KeyCode.KeypadEnter ||
                    keyDownEvent.keyCode == KeyCode.Return;
            }

            return false;
        }

        public virtual bool IsCancellationEvent(EventBase evt)
        {
            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                var keyDownEvent = (KeyDownEvent)evt;
                return keyDownEvent.keyCode == KeyCode.Escape;
            }

            return false;
        }

        public virtual bool IsNavigationEvent(EventBase evt, out NavigationDirection direction)
        {
            if (evt.eventTypeId == KeyDownEvent.TypeId())
            {
                return (direction = GetNavigationDirection((KeyDownEvent)evt)) != NavigationDirection.None;
            }

            direction = NavigationDirection.None;
            return false;
        }

        private NavigationDirection GetNavigationDirection(KeyDownEvent keyDownEvent)
        {
            bool Shift() => (keyDownEvent.modifiers & EventModifiers.Shift) != 0;
            switch (keyDownEvent.keyCode)
            {
                case KeyCode.Tab: return Shift() ? NavigationDirection.Previous : NavigationDirection.Next;
                case KeyCode.LeftArrow: return NavigationDirection.Left;
                case KeyCode.RightArrow: return NavigationDirection.Right;
                case KeyCode.UpArrow: return NavigationDirection.Up;
                case KeyCode.DownArrow: return NavigationDirection.Down;
                case KeyCode.PageUp: return NavigationDirection.PageUp;
                case KeyCode.PageDown: return NavigationDirection.PageDown;
                case KeyCode.Home: return NavigationDirection.Home;
                case KeyCode.End: return NavigationDirection.End;
            }
            return NavigationDirection.None;
        }
    }
}
