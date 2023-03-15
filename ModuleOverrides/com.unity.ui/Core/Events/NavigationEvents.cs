// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for all navigation events.
    /// </summary>
    public interface INavigationEvent
    {
        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        EventModifiers modifiers { get; }

        /// <summary>
        /// Describes the type of device this event was created from.
        /// </summary>
        /// <remarks>
        /// The device type indicates whether there should be an KeyDownEvent observable before this navigation event.
        /// </remarks>
        internal NavigationDeviceType deviceType { get; }

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
        /// The platform-specific action key is Cmd on macOS, and Ctrl on all other platforms.
        /// </remarks>
        bool actionKey { get; }
    }

    /// <summary>
    /// Describes types of devices that can generate navigation events.
    /// This can help avoid duplicated treatment of events when some controls react to keyboard input
    /// using KeyDownEvent while others react to navigation events coming from the same keyboard input.
    /// </summary>
    internal enum NavigationDeviceType
    {
        /// <summary>
        /// Indicates that no specific information is known about this device.
        /// </summary>
        /// <remarks>
        /// Controls reacting to navigation events from an unknown device should react conservatively.
        /// For example, if there is a conflict between a KeyDownEvent and a subsequent navigation event,
        /// a control could assume that the device type is a keyboard and conservatively block the navigation event.
        /// </remarks>
        Unknown = 0,
        /// <summary>
        /// Indicates that this device is known to be a keyboard.
        /// </summary>
        /// <remarks>
        /// This device should also send a KeyDownEvent immediately before any navigation event it generates.
        /// </remarks>
        Keyboard,
        /// <summary>
        /// Indicates that this device is anything else than a keyboard (it could be a Gamepad, for example).
        /// </summary>
        /// <remarks>
        /// This device should not send a KeyDownEvent before the navigation events it generates.
        /// </remarks>
        NonKeyboard
    }

    /// <summary>
    /// Navigation events abstract base class.
    ///
    /// By default, navigation events trickle down and bubble up. Disabled elements won't receive these events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [EventCategory(EventCategory.Navigation)]
    public abstract class NavigationEventBase<T> : EventBase<T>, INavigationEvent where T : NavigationEventBase<T>, new()
    {
        /// <summary>
        /// Gets flags that indicate whether modifier keys (Alt, Ctrl, Shift, Windows/Cmd) are pressed.
        /// </summary>
        public EventModifiers modifiers { get; protected set; }

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
                if (Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.OSXPlayer)
                {
                    return commandKey;
                }
                else
                {
                    return ctrlKey;
                }
            }
        }

        /// <summary>
        /// Describes the type of device this event was created from.
        /// </summary>
        /// <remarks>
        /// The device type indicates whether there should be an KeyDownEvent observable before this navigation event.
        /// </remarks>
        NavigationDeviceType INavigationEvent.deviceType => deviceType;
        internal NavigationDeviceType deviceType { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        protected NavigationEventBase()
        {
            LocalInit();
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                EventPropagation.SkipDisabledElements;
            modifiers = EventModifiers.None;
            deviceType = NavigationDeviceType.Unknown;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <param name="deviceType">The type of device this event was created from.</param>
        /// <returns>An initialized navigation event.</returns>
        public static T GetPooled(EventModifiers modifiers = EventModifiers.None)
        {
            T e = EventBase<T>.GetPooled();
            e.modifiers = modifiers;
            e.deviceType = NavigationDeviceType.Unknown;
            return e;
        }

        internal static T GetPooled(NavigationDeviceType deviceType, EventModifiers modifiers = EventModifiers.None)
        {
            T e = EventBase<T>.GetPooled();
            e.modifiers = modifiers;
            e.deviceType = deviceType;
            return e;
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToFocusedElementOrPanelRoot(this, panel);
        }
    }

    /// <summary>
    /// Event typically sent when the user presses the D-pad, moves a joystick or presses the arrow keys.
    /// </summary>
    public class NavigationMoveEvent : NavigationEventBase<NavigationMoveEvent>
    {
        static NavigationMoveEvent()
        {
            SetCreateFunction(() => new NavigationMoveEvent());
        }

        /// <summary>
        /// Move event direction.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// No specific direction.
            /// </summary>
            None,
            /// <summary>
            /// Left.
            /// </summary>
            Left,
            /// <summary>
            /// Up.
            /// </summary>
            Up,
            /// <summary>
            /// Right.
            /// </summary>
            Right,
            /// <summary>
            /// Down.
            /// </summary>
            Down,

            /// <summary>
            /// Forwards, toward next element.
            /// </summary>
            Next,
            /// <summary>
            /// Backwards, toward previous element.
            /// </summary>
            Previous,
        }

        internal static Direction DetermineMoveDirection(float x, float y, float deadZone = 0.6f)
        {
            // if vector is too small... just return
            if (new Vector2(x, y).sqrMagnitude < deadZone * deadZone)
                return Direction.None;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                if (x > 0)
                    return Direction.Right;
                return Direction.Left;
            }
            else
            {
                if (y > 0)
                    return Direction.Up;
                return Direction.Down;
            }
        }

        /// <summary>
        /// The direction of the navigation.
        /// </summary>
        public Direction direction { get; private set; }

        /// <summary>
        /// The move vector, if applicable.
        /// </summary>
        /// <remarks>
        /// This information is not guaranteed to be available through all input sources that can generate
        /// NavigationMoveEvents. UI Toolkit standard controls should never depend on this value being set.
        /// </remarks>
        public Vector2 move { get; private set; }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="moveVector">The move vector.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized navigation event.</returns>
        public static NavigationMoveEvent GetPooled(Vector2 moveVector, EventModifiers modifiers = EventModifiers.None)
        {
            NavigationMoveEvent e = GetPooled(NavigationDeviceType.Unknown, modifiers);
            e.direction = DetermineMoveDirection(moveVector.x, moveVector.y);
            e.move = moveVector;
            return e;
        }

        internal static NavigationMoveEvent GetPooled(Vector2 moveVector, NavigationDeviceType deviceType, EventModifiers modifiers = EventModifiers.None)
        {
            NavigationMoveEvent e = GetPooled(deviceType, modifiers);
            e.direction = DetermineMoveDirection(moveVector.x, moveVector.y);
            e.move = moveVector;
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values.
        /// Use this function instead of creating new events.
        /// Events obtained from this method should be released back to the pool using Dispose().
        /// </summary>
        /// <param name="direction">The logical direction of the navigation.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized navigation event.</returns>
        /// <remarks>
        /// This method doesn't set any move vector. See other overload of the method for more information.
        /// </remarks>
        public static NavigationMoveEvent GetPooled(Direction direction, EventModifiers modifiers = EventModifiers.None)
        {
            NavigationMoveEvent e = GetPooled(NavigationDeviceType.Unknown, modifiers);
            e.direction = direction;
            e.move = Vector2.zero;
            return e;
        }

        internal static NavigationMoveEvent GetPooled(Direction direction, NavigationDeviceType deviceType, EventModifiers modifiers = EventModifiers.None)
        {
            NavigationMoveEvent e = GetPooled(deviceType, modifiers);
            e.direction = direction;
            e.move = Vector2.zero;
            return e;
        }

        /// <summary>
        /// Initialize the event members.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NavigationMoveEvent()
        {
            LocalInit();
        }

        void LocalInit()
        {
            direction = Direction.None;
            move = Vector2.zero;
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            panel.focusController.SwitchFocusOnEvent(panel.focusController.GetLeafFocusedElement(), this);

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// Event sent when the user presses the cancel button.
    /// </summary>
    public class NavigationCancelEvent : NavigationEventBase<NavigationCancelEvent>
    {
        static NavigationCancelEvent()
        {
            SetCreateFunction(() => new NavigationCancelEvent());
        }
    }

    /// <summary>
    /// Event sent when the user presses the submit button.
    /// </summary>
    public class NavigationSubmitEvent : NavigationEventBase<NavigationSubmitEvent>
    {
        static NavigationSubmitEvent()
        {
            SetCreateFunction(() => new NavigationSubmitEvent());
        }
    }
}
