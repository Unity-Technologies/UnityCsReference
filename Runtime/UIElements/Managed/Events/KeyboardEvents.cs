// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IKeyboardEvent
    {
        EventModifiers modifiers { get; }
        char character { get; }
        KeyCode keyCode { get; }

        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool commandKey { get; }
        bool altKey { get; }
    }

    public abstract class KeyboardEventBase<T> : EventBase<T>, IKeyboardEvent where T : KeyboardEventBase<T>, new()
    {
        public EventModifiers modifiers { get; protected set; }
        public char character { get; protected set; }
        public KeyCode keyCode { get; protected set; }

        public bool shiftKey
        {
            get { return (modifiers & EventModifiers.Shift) != 0; }
        }

        public bool ctrlKey
        {
            get { return (modifiers & EventModifiers.Control) != 0; }
        }

        public bool commandKey
        {
            get { return (modifiers & EventModifiers.Command) != 0; }
        }

        public bool altKey
        {
            get { return (modifiers & EventModifiers.Alt) != 0; }
        }

        // FIXME: see https://www.w3.org/TR/DOM-Level-3-Events/#interface-keyboardevent for key, code and location values.
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable | EventFlags.Cancellable;
            modifiers = default(EventModifiers);
            character = default(char);
            keyCode = default(KeyCode);
        }

        public static T GetPooled(char c, KeyCode keyCode, EventModifiers modifiers)
        {
            T e = GetPooled();
            e.modifiers = modifiers;
            e.character = c;
            e.keyCode = keyCode;
            return e;
        }

        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();
            e.imguiEvent = systemEvent;
            if (systemEvent != null)
            {
                e.modifiers = systemEvent.modifiers;
                e.character = systemEvent.character;
                e.keyCode = systemEvent.keyCode;
            }
            return e;
        }

        protected KeyboardEventBase()
        {
            Init();
        }
    }

    public class KeyDownEvent : KeyboardEventBase<KeyDownEvent>
    {
    }

    public class KeyUpEvent : KeyboardEventBase<KeyUpEvent>
    {
    }
}
