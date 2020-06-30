using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This interface describes the data used by keyboard events.
    /// </summary>
    public interface IKeyboardEvent
    {
        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        EventModifiers modifiers { get; }
        /// <summary>
        /// Gets the character entered.
        /// </summary>
        /// <remarks>
        /// This is the character entered when a key is pressed, taking into account the current keyboard layout. For example,
        /// pressing the "A" key causes this property to return either "a" or "A", depending on whether the Shift
        /// key is pressed at the time. The Shift key itself does not produce a character. When  pressed, it returns
        /// an empty string.
        /// </remarks>
        char character { get; }
        /// <summary>
        /// The key code.
        /// </summary>
        /// <remarks>
        /// This is the code of the physical key that was pressed. It doesn't take into account the keyboard
        /// layout, and it displays modifier keys, since a key was pressed. For example, pressing the "A" key
        /// will cause this property to return KeyCode.A regardless of whether the Shift key is pressed or not.
        /// The Shift key itself returns KeyCode.LeftShift since it is a physical key on the keyboard.
        /// </remarks>
        KeyCode keyCode { get; }

        /// <summary>
        /// Gets a boolean value that indicates whether the Shift key is pressed. True means the Shift key is pressed.
        /// False means it isn't.
        /// </summary>
        bool shiftKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Ctrl key is pressed. True means the Ctrl key is pressed.
        /// False means it isn't.
        /// </summary>
        bool ctrlKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Windows/Cmd key is pressed. True means the Windows/Cmd key
        /// is pressed. False means it isn't.
        /// </summary>
        bool commandKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the Alt key is pressed. True means the Alt key is pressed.
        /// False means it isn't.
        /// </summary>
        bool altKey { get; }
        /// <summary>
        /// Gets a boolean value that indicates whether the platform-specific action key is pressed. True means the action
        /// key is pressed. False means it isn't.
        /// </summary>
        /// <remarks>
        /// The platform-specific action key is Cmd on macOs, and Ctrl on all other platforms.
        /// </remarks>
        bool actionKey { get; }
    }

    /// <summary>
    /// This is the base class for keyboard events.
    /// </summary>
    /// <remarks>
    /// The typical keyboard event loop is as follows:
    ///   - When a key is pressed, a <see cref="KeyDownEvent"/> is sent.
    ///   - If the key is held down for a duration determined by the OS, another KeyDownEvent with the same data is
    ///     sent. While the key is held down, the event is sent repeatedly at a frequency determined by the OS.
    ///   - When the key is released, a <see cref="KeyUpEvent"/> is sent.
    ///
    /// By default, keyboard events trickle down and bubble up. They are cancellable.
    ///
    /// </remarks>
    public abstract class KeyboardEventBase<T> : EventBase<T>, IKeyboardEvent where T : KeyboardEventBase<T>, new()
    {
        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        public EventModifiers modifiers { get; protected set; }
        /// <summary>
        /// Gets the character entered.
        /// </summary>
        /// <remarks>
        /// This is the character entered when a key is pressed, taking into account the current keyboard layout. For example,
        /// pressing the "A" key causes this property to return either "a" or "A", depending on whether the Shift
        /// key is pressed at the time. The Shift key itself does not produce a character. When  pressed, it returns
        /// an empty string.
        /// </remarks>
        public char character { get; protected set; }
        /// <summary>
        /// The key code.
        /// </summary>
        /// <remarks>
        /// This is the code of the physical key that was pressed. It doesn't take into account the keyboard
        /// layout, and it displays modifier keys, since a key was pressed. For example, pressing the "A" key
        /// will cause this property to return KeyCode.A regardless of whether the Shift key is pressed or not.
        /// The Shift key itself returns KeyCode.LeftShift since it is a physical key on the keyboard.
        /// </remarks>
        public KeyCode keyCode { get; protected set; }

        /// <summary>
        /// Gets a boolean value that indicates whether the Shift key is pressed. True means the Shift key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool shiftKey
        {
            get { return (modifiers & EventModifiers.Shift) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Ctrl key is pressed. True means the Ctrl key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool ctrlKey
        {
            get { return (modifiers & EventModifiers.Control) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Windows/Cmd key is pressed. True means the Windows/Cmd key
        /// is pressed. False means it isn't.
        /// </summary>
        public bool commandKey
        {
            get { return (modifiers & EventModifiers.Command) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the Alt key is pressed. True means the Alt key is pressed.
        /// False means it isn't.
        /// </summary>
        public bool altKey
        {
            get { return (modifiers & EventModifiers.Alt) != 0; }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the platform-specific action key is pressed. True means the action
        /// key is pressed. False means it isn't.
        /// </summary>
        /// <remarks>
        /// The platform-specific action key is Cmd on macOS, and Ctrl on all other platforms.
        /// </remarks>
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
        /// Gets a keyboard event from the event pool and initializes it with the given values. Use this function
        /// instead of creating new events. Events obtained using this method need to be released back to the pool.
        /// You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="c">The character for this event.</param>
        /// <param name="keyCode">The key code for this event.</param>
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
        /// Gets a keyboard event from the event pool and initializes it with the given values. Use this
        /// function instead of creating new events. Events obtained using this method need to be released
        /// back to the pool. You can use `Dispose()` to release them.
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
    /// This event is sent when a key is pressed.
    /// </summary>
    /// <remarks>
    /// This event trickles down and bubbles up. It is cancellable.
    /// </remarks>
    public class KeyDownEvent : KeyboardEventBase<KeyDownEvent>
    {
        // This is needed for TextEditor features that require an imguiEvent but receive non-imgui events
        internal void GetEquivalentImguiEvent(Event outImguiEvent)
        {
            if (imguiEvent != null)
            {
                outImguiEvent.CopyFrom(imguiEvent);
            }
            else
            {
                outImguiEvent.type = EventType.KeyDown;
                outImguiEvent.modifiers = modifiers;
                outImguiEvent.character = character;
                outImguiEvent.keyCode = keyCode;
            }
        }
    }

    /// <summary>
    /// This event is sent when a pressed key is released.
    /// </summary>
    /// <remarks>
    /// This event trickles down and bubbles up. It is cancellable.
    /// </remarks>
    public class KeyUpEvent : KeyboardEventBase<KeyUpEvent>
    {
    }
}
