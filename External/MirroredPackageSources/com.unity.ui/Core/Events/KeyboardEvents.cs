namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for keyboard events.
    /// </summary>
    public interface IKeyboardEvent
    {
        /// <summary>
        /// Flag set holding the pressed modifier keys (Alt, Control, Shift, Windows/Command).
        /// </summary>
        EventModifiers modifiers { get; }
        /// <summary>
        /// The character.
        /// </summary>
        char character { get; }
        /// <summary>
        /// The key code.
        /// </summary>
        KeyCode keyCode { get; }

        /// <summary>
        /// Return true if the Shift key is pressed.
        /// </summary>
        bool shiftKey { get; }
        /// <summary>
        /// Return true if the Control key is pressed.
        /// </summary>
        bool ctrlKey { get; }
        /// <summary>
        /// Return true if the Windows/Command key is pressed.
        /// </summary>
        bool commandKey { get; }
        /// <summary>
        /// Return true if the Alt key is pressed.
        /// </summary>
        bool altKey { get; }
        /// <summary>
        /// Returns true if the platform-specific action key is pressed. This key is Cmd on macOS, and Ctrl on all other platforms.
        /// </summary>
        bool actionKey { get; }
    }

    /// <summary>
    /// Base class for keyboard events.
    /// </summary>
    public abstract class KeyboardEventBase<T> : EventBase<T>, IKeyboardEvent where T : KeyboardEventBase<T>, new()
    {
        /// <summary>
        /// Flags that hold pressed modifier keys (Alt, Ctrl, Shift, Windows/Cmd).
        /// </summary>
        public EventModifiers modifiers { get; protected set; }
        /// <summary>
        /// The character.
        /// </summary>
        public char character { get; protected set; }
        /// <summary>
        /// The key code.
        /// </summary>
        public KeyCode keyCode { get; protected set; }

        /// <summary>
        /// Returns true if the Shift key is pressed.
        /// </summary>
        public bool shiftKey
        {
            get { return (modifiers & EventModifiers.Shift) != 0; }
        }

        /// <summary>
        /// Returns true if the Ctrl key is pressed.
        /// </summary>
        public bool ctrlKey
        {
            get { return (modifiers & EventModifiers.Control) != 0; }
        }

        /// <summary>
        /// Returns true if the Windows/Cmd key is pressed.
        /// </summary>
        public bool commandKey
        {
            get { return (modifiers & EventModifiers.Command) != 0; }
        }

        /// <summary>
        /// Returns true if the Alt key is pressed.
        /// </summary>
        public bool altKey
        {
            get { return (modifiers & EventModifiers.Alt) != 0; }
        }

        /// <summary>
        /// Returns true if the platform-specific action key is pressed. This key is Cmd on macOS, and Ctrl on all other platforms.
        /// </summary>
        public bool actionKey
        {
            get
            {
                if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                {
                    return commandKey;
                }
                else
                {
                    return ctrlKey;
                }
            }
        }

        // FIXME: see https://www.w3.org/TR/DOM-Level-3-Events/#interface-keyboardevent for key, code and location values.
        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
            modifiers = default(EventModifiers);
            character = default(char);
            keyCode = default(KeyCode);
        }

        /// <summary>
        /// Gets a keyboard event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="c">The character for this event.</param>
        /// <param name="keyCode">The keyCode for this event.</param>
        /// <param name="modifiers">Event modifier keys that are active for this event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(char c, KeyCode keyCode, EventModifiers modifiers)
        {
            T e = GetPooled();
            e.modifiers = modifiers;
            e.character = c;
            e.keyCode = keyCode;
            return e;
        }

        /// <summary>
        /// Gets a keyboard event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">A keyboard IMGUI event.</param>
        /// <returns>An initialized event.</returns>
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
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when a key is pressed on the keyboard. This event trickles down and bubbles up. This event is cancellable.
    /// </summary>
    public class KeyDownEvent : KeyboardEventBase<KeyDownEvent>
    {
    }

    /// <summary>
    /// Event sent when a key is released on the keyboard. This event trickles down and bubbles up. This event is cancellable.
    /// </summary>
    public class KeyUpEvent : KeyboardEventBase<KeyUpEvent>
    {
    }
}
