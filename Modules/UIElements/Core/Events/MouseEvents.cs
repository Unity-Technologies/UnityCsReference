// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for mouse events.
    /// </summary>
    /// <remarks>
    /// Refer to the [[wiki:UIE-Mouse-Events|Mouse events]] manual page for more information and examples.
    /// </remarks>
    public interface IMouseEvent
    {
        /// <summary>
        /// Flag set holding the pressed modifier keys (Alt, Ctrl, Shift, Windows/Command).
        /// </summary>
        /// <remarks>
        /// See <see cref="EventModifiers"/> for more information.
        /// </remarks>
        EventModifiers modifiers { get; }
        /// <summary>
        /// The mouse position in the panel coordinate system.
        /// </summary>
        Vector2 mousePosition { get; }
        /// <summary>
        /// The mouse position in the current target coordinate system.
        /// </summary>
        /// <remarks>
        /// The value of this property changes throughout the propagation process for each element receiving the event along the propagation path.
        /// </remarks>
        /// <seealso cref="EventBase.currentTarget"/>
        Vector2 localMousePosition { get; }
        /// <summary>
        /// Gets the difference between the mouse's position during the previous mouse event and its position during the
        /// current mouse event.
        /// </summary>
        /// <remarks>
        /// This value is based on <see cref="IMouseEvent.mousePosition"/> and is expressed in panel world coordinates.
        /// </remarks>
        Vector2 mouseDelta { get; }
        /// <summary>
        /// The number of times a button is pressed consecutively.
        /// </summary>
        /// <remarks>See <see cref="IPointerEvent.clickCount"/></remarks>
        int clickCount { get; }
        /// <summary>
        /// A value that indicates which mouse button was pressed or released (if any) to cause this event:
        /// 0 is the left button, 1 is the right button, 2 is the middle button.
        /// A negative value indicates that no mouse button changed state during this event.
        /// </summary>
        int button { get; }
        /// <summary>
        /// A bitmask that describes the currently pressed buttons.
        /// </summary>
        /// <remarks>
        /// Pressing a mouse button sets a bit; releasing the button clears it. The left mouse button sets/clears Bit 0. The right mouse button sets/clears Bit 1. The middle mouse button sets/clears Bit 2. Additional buttons set/clear other bits.
        /// </remarks>
        int pressedButtons { get; }

        /// <summary>
        /// Return true if the Shift key is pressed.
        /// </summary>
        /// <remarks>
        /// See <see cref="EventModifiers.Shift"/> for more information.
        /// </remarks>
        bool shiftKey { get; }
        /// <summary>
        /// Return true if the Ctrl key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Control"/> for more information.
        /// </remarks>
        bool ctrlKey { get; }
        /// <summary>
        /// Return true if the Windows/Command key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Command"/> for more information.
        /// </remarks>
        bool commandKey { get; }
        /// <summary>
        /// Return true if the Alt key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Alt"/> for more information.
        /// </remarks>
        bool altKey { get; }
        /// <summary>
        /// Returns true if the platform-specific action key is pressed.
        /// </summary>
        /// <remarks>
        /// This key is Cmd on macOS, and Ctrl on all other platforms.
        /// </remarks>
        bool actionKey { get; }
    }

    internal interface IMouseEventInternal
    {
        IPointerEvent sourcePointerEvent { get; }
        bool recomputeTopElementUnderMouse { get; }
    }

    /// <summary>
    /// The base class for mouse events.
    /// </summary>
    /// <remarks>
    /// Refer to the [[wiki:UIE-Mouse-Events|Mouse events]] manual page for more information and examples.
    /// </remarks>
    [EventCategory(EventCategory.Pointer)]
    public abstract class MouseEventBase<T> : EventBase<T>, IMouseEvent, IMouseEventInternal, IPointerOrMouseEvent
        where T : MouseEventBase<T>, new()
    {
        /// <summary>
        /// Flags that hold pressed modifier keys (Alt, Ctrl, Shift, Windows/Cmd).
        /// </summary>
        /// <remarks>
        /// See <see cref="EventModifiers"/> for more information.
        /// </remarks>
        public EventModifiers modifiers { get; protected set; }
        /// <summary>
        /// The mouse position in the panel coordinate system.
        /// </summary>
        public Vector2 mousePosition { get; protected set; }
        /// <summary>
        /// The mouse position in the current target coordinate system.
        /// </summary>
        /// <remarks>
        /// The value of this property changes throughout the propagation process for each element receiving the event along the propagation path.
        /// </remarks>
        /// <seealso cref="EventBase.currentTarget"/>
        public Vector2 localMousePosition { get; internal set; }
        /// <summary>
        /// Gets the difference between the mouse's position during the previous mouse event and its position during the
        /// current mouse event.
        /// </summary>
        /// <remarks>
        /// This value is based on <see cref="IMouseEvent.mousePosition"/> and is expressed in panel world coordinates.
        /// </remarks>
        public Vector2 mouseDelta { get; protected set; }
        /// <summary>
        /// The number of times a button is pressed consecutively.
        /// </summary>
        /// <remarks>See <see cref="IPointerEvent.clickCount"/></remarks>
        public int clickCount { get; protected set; }
        /// <summary>
        /// A value that indicates which mouse button was pressed or released (if any) to cause this event:
        /// 0 is the left button, 1 is the right button, 2 is the middle button.
        /// A negative value indicates that no mouse button changed state during this event.
        /// </summary>
        public int button { get; protected set; }
        /// <summary>
        /// A bitmask that describes the currently pressed buttons.
        /// </summary>
        /// <remarks>
        /// Pressing a mouse button sets a bit; releasing the button clears it. The left mouse button sets/clears Bit 0. The right mouse button sets/clears Bit 1. The middle mouse button sets/clears Bit 2. Additional buttons set/clear other bits.
        /// </remarks>
        public int pressedButtons { get; protected set; }

        /// <summary>
        /// Returns true if the Shift key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Shift"/> for more information.
        /// </remarks>
        public bool shiftKey
        {
            get { return (modifiers & EventModifiers.Shift) != 0; }
        }

        /// <summary>
        /// Returns true if the Ctrl key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Control"/> for more information.
        /// </remarks>
        public bool ctrlKey
        {
            get { return (modifiers & EventModifiers.Control) != 0; }
        }

        /// <summary>
        /// Returns true if the Windows/Cmd key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Command"/> for more information.
        /// </remarks>
        public bool commandKey
        {
            get { return (modifiers & EventModifiers.Command) != 0; }
        }

        /// <summary>
        /// Returns true if the Alt key is pressed.
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EventModifiers.Alt"/> for more information.
        /// </remarks>
        public bool altKey
        {
            get { return (modifiers & EventModifiers.Alt) != 0; }
        }

        /// <summary>
        /// Returns true if the platform-specific action key is pressed.
        /// </summary>
        /// <remarks>
        /// This key is Cmd on macOS, and Ctrl on all other platforms.
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

        internal IPointerEvent sourcePointerEvent { get; set; }
        internal bool recomputeTopElementUnderMouse { get; set; }

        IPointerEvent IMouseEventInternal.sourcePointerEvent => sourcePointerEvent;
        bool IMouseEventInternal.recomputeTopElementUnderMouse => recomputeTopElementUnderMouse;

        int IPointerOrMouseEvent.pointerId => PointerId.mousePointerId;
        Vector3 IPointerOrMouseEvent.position => mousePosition;

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            modifiers = EventModifiers.None;
            mousePosition = Vector2.zero;
            localMousePosition = Vector2.zero;
            mouseDelta = Vector2.zero;
            clickCount = 0;
            button = 0;
            pressedButtons = 0;
            sourcePointerEvent = null;
            recomputeTopElementUnderMouse = false;
        }

        /// <summary>
        /// The current target of the event. The current target is the element in the propagation path for which event handlers are currently being executed.
        /// </summary>
        public override IEventHandler currentTarget
        {
            get { return base.currentTarget; }
            internal set
            {
                base.currentTarget = value;

                var element = currentTarget as VisualElement;
                if (element != null)
                {
                    localMousePosition = element.WorldToLocal(mousePosition);
                }
                else
                {
                    localMousePosition = mousePosition;
                }
            }
        }

        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            if (recomputeTopElementUnderMouse)
            {
                // UUM-4156: save pointer position only for Mouse events that don't have an equivalent Pointer event.
                if (sourcePointerEvent == null)
                {
                    PointerDeviceState.SavePointerPosition(PointerId.mousePointerId, mousePosition, panel, panel.contextType);
                    ((BaseVisualElementPanel)panel).RecomputeTopElementUnderPointer(PointerId.mousePointerId, mousePosition, this);
                }
                // UUM-91321: discard OS-driven compatibility mouse position when receiving other primary pointer data.
                else if (sourcePointerEvent.pointerId != PointerId.mousePointerId)
                {
                    var position = BaseVisualElementPanel.s_OutsidePanelCoordinates;
                    PointerDeviceState.SavePointerPosition(PointerId.mousePointerId, position, null, panel.contextType);
                    ((BaseVisualElementPanel)panel).SetTopElementUnderPointer(null, PointerId.mousePointerId, position);
                }
            }
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            DebuggerEventDispatchUtilities.PostDispatch(this, (BaseVisualElementPanel)panel);

            if (sourcePointerEvent is EventBase pointerEvent)
            {
                // pointerEvent processing should not be done and it should not have returned to the pool.
                Debug.Assert(!pointerEvent.processed, "!pointerEvent.processed");

                // We are a compatibility mouse event. Pass our status to the original pointer event.
                if (isPropagationStopped)
                {
                    pointerEvent.StopPropagation();
                }
                if (isImmediatePropagationStopped)
                {
                    pointerEvent.StopImmediatePropagation();
                }
                pointerEvent.processedByFocusController |= processedByFocusController;
            }
            else if (recomputeTopElementUnderMouse)
            {
                // If pointerEvent != null, pointerEvent.PostDispatch() will take care of this.
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }

            base.PostDispatch(panel);
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToCapturingElementOrElementUnderPointer(this, panel,
                PointerId.mousePointerId, mousePosition);
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();
            e.imguiEvent = systemEvent;
            if (systemEvent != null)
            {
                e.modifiers = systemEvent.modifiers;
                e.mousePosition = systemEvent.mousePosition;
                e.localMousePosition = systemEvent.mousePosition;
                e.mouseDelta = systemEvent.delta;
                e.button = systemEvent.button;
                e.pressedButtons = PointerDeviceState.GetPressedButtons(PointerId.mousePointerId);
                e.clickCount = systemEvent.clickCount;
            }
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="position">The mouse position.</param>
        /// <param name="button">The mouse button pressed.</param>
        /// <param name="clickCount">The number of consecutive mouse clicks received.</param>
        /// <param name="delta">The relative movement of the mouse compared to the mouse position when the last event was received.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Vector2 position, int button, int clickCount, Vector2 delta,
            EventModifiers modifiers = EventModifiers.None)
        {
            T e = GetPooled();

            e.modifiers = modifiers;
            e.mousePosition = position;
            e.localMousePosition = position;
            e.mouseDelta = delta;
            e.button = button;
            e.pressedButtons = PointerDeviceState.GetPressedButtons(PointerId.mousePointerId);
            e.clickCount = clickCount;

            return e;
        }

        internal static T GetPooled(IMouseEvent triggerEvent, Vector2 mousePosition)
        {
            if (triggerEvent != null)
            {
                return GetPooled(triggerEvent);
            }

            T e = GetPooled();
            e.mousePosition = mousePosition;
            e.localMousePosition = mousePosition;
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="triggerEvent">The event that sent this event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(IMouseEvent triggerEvent)
        {
            T e = EventBase<T>.GetPooled(triggerEvent as EventBase);
            if (triggerEvent != null)
            {
                e.modifiers = triggerEvent.modifiers;
                e.mousePosition = triggerEvent.mousePosition;
                e.localMousePosition = triggerEvent.mousePosition;
                e.mouseDelta = triggerEvent.mouseDelta;
                e.button = triggerEvent.button;
                e.pressedButtons = triggerEvent.pressedButtons;
                e.clickCount = triggerEvent.clickCount;
            }
            return e;
        }

        // This function is protected so that only specific subclasses can offer the
        // functionality from specific IPointerEvent types.
        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="pointerEvent">The pointer event that sent this event.</param>
        /// <returns>An initialized event.</returns>
        protected static T GetPooled(IPointerEvent pointerEvent)
        {
            T e = GetPooled();

            e.elementTarget = (pointerEvent as EventBase)?.elementTarget;
            e.imguiEvent = (pointerEvent as EventBase)?.imguiEvent;

            e.modifiers = pointerEvent.modifiers;
            e.mousePosition = pointerEvent.position;
            e.localMousePosition = pointerEvent.position;
            e.mouseDelta = pointerEvent.deltaPosition;
            e.button = pointerEvent.button == -1 ? 0 : pointerEvent.button;
            e.pressedButtons = pointerEvent.pressedButtons;
            e.clickCount = pointerEvent.clickCount;

            IPointerEventInternal pointerEventInternal = pointerEvent as IPointerEventInternal;
            if (pointerEventInternal != null)
            {
                // Link the mouse event and the pointer event so we can forward
                // the propagation result (for tests and for IMGUI)
                e.sourcePointerEvent = pointerEvent;
            }

            return e;
        }

        protected MouseEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// This event is sent when a mouse button is pressed.
    /// </summary>
    /// <remarks>
    /// The mouse down event is sent to a visual element when a mouse button is pressed inside the element.
    /// A MouseDownEvent uses the default mouse event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements won't receive this event by default.
    /// </remarks>
    [EventCategory(EventCategory.PointerDown)]
    public class MouseDownEvent : MouseEventBase<MouseDownEvent>
    {
        static MouseDownEvent()
        {
            SetCreateFunction(() => new MouseDownEvent());
        }

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                EventPropagation.SkipDisabledElements;
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseDownEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseDownEvent GetPooled(Event systemEvent)
        {
            return MouseEventBase<MouseDownEvent>.GetPooled(systemEvent);
        }

        private static MouseDownEvent MakeFromPointerEvent(IPointerEvent pointerEvent)
        {
            return MouseEventBase<MouseDownEvent>.GetPooled(pointerEvent);
        }

        internal static MouseDownEvent GetPooled(PointerDownEvent pointerEvent)
        {
            return MakeFromPointerEvent(pointerEvent);
        }

        internal static MouseDownEvent GetPooled(PointerMoveEvent pointerEvent)
        {
            return MakeFromPointerEvent(pointerEvent);
        }
    }

    /// <summary>
    /// This event is sent when a mouse button is released.
    /// </summary>
    /// <remarks>
    /// The mouse up event is sent to a visual element when a mouse button is released inside the element.
    /// A MouseUpEvent uses the default mouse event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements won't receive this event by default.
    /// </remarks>
    public class MouseUpEvent : MouseEventBase<MouseUpEvent>
    {
        static MouseUpEvent()
        {
            SetCreateFunction(() => new MouseUpEvent());
        }

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                EventPropagation.SkipDisabledElements;
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseUpEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseUpEvent GetPooled(Event systemEvent)
        {
            return MouseEventBase<MouseUpEvent>.GetPooled(systemEvent);
        }

        private static MouseUpEvent MakeFromPointerEvent(IPointerEvent pointerEvent)
        {
            return MouseEventBase<MouseUpEvent>.GetPooled(pointerEvent);
        }

        internal static MouseUpEvent GetPooled(PointerUpEvent pointerEvent)
        {
            return MakeFromPointerEvent(pointerEvent);
        }

        internal static MouseUpEvent GetPooled(PointerMoveEvent pointerEvent)
        {
            return MakeFromPointerEvent(pointerEvent);
        }

        internal static MouseUpEvent GetPooled(PointerCancelEvent pointerEvent)
        {
            return MakeFromPointerEvent(pointerEvent);
        }
    }

    /// <summary>
    /// This event is sent when the mouse moves.
    /// </summary>
    /// <remarks>
    /// The mouse move event is sent to the visual element under the current mouse position whenever the mouse position has changed.
    /// A MouseMoveEvent uses the default mouse event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements receive this event by default.
    /// </remarks>
    /// <seealso cref="MouseEnterEvent"/>
    /// <seealso cref="MouseLeaveEvent"/>
    /// <seealso cref="MouseOverEvent"/>
    /// <seealso cref="MouseOutEvent"/>
    [EventCategory(EventCategory.PointerMove)]
    public class MouseMoveEvent : MouseEventBase<MouseMoveEvent>
    {
        static MouseMoveEvent()
        {
            SetCreateFunction(() => new MouseMoveEvent());
        }

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
            // Trickles down, bubbles up and can be cancelled. Disabled elements receive this event by default.
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseMoveEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseMoveEvent GetPooled(Event systemEvent)
        {
            // For a MouseMove event type, systemEvent.button reflects
            // (at least on Windows) the currently pressed buttons,
            // whereas for mouse up/down
            // it reflects the mouse button being pressed or released
            // (there can be other buttons already pressed down).
            // These are two different semantics. We choose the second
            // one and track the button state using PointerButtonTracker.
            // We thus reset e.button for mouse move events.
            MouseMoveEvent e = MouseEventBase<MouseMoveEvent>.GetPooled(systemEvent);
            e.button = 0;
            return e;
        }

        internal static MouseMoveEvent GetPooled(PointerMoveEvent pointerEvent)
        {
            return MouseEventBase<MouseMoveEvent>.GetPooled(pointerEvent);
        }
    }

    /// <summary>
    /// The event sent when clicking the right mouse button.
    /// </summary>
    public class ContextClickEvent : MouseEventBase<ContextClickEvent>
    {
        static ContextClickEvent()
        {
            SetCreateFunction(() => new ContextClickEvent());
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public ContextClickEvent()
        {
            LocalInit();
        }

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
        }
    }

    /// <summary>
    /// This event is sent when the mouse wheel moves.
    /// </summary>
    /// <remarks>
    /// The mouse wheel event is sent to the visual element under the mouse when the mouse scroll wheel value changes.
    /// A WheelEvent uses the default mouse event propagation path: it trickles down, bubbles up
    /// and can be cancelled.
    /// Disabled elements won't receive this event by default.
    /// </remarks>
    public class WheelEvent : MouseEventBase<WheelEvent>
    {
        /// <summary>
        /// The magnitude of WheelEvent.delta that corresponds to exactly one tick of the scroll wheel.
        /// </summary>
        /// <remarks>
        /// UIToolkit's scroll factor is the same as IMGUI's scroll factor both in Editor and Runtime.
        /// </remarks>
        public const float scrollDeltaPerTick = Event.scrollWheelDeltaPerTick;

        static WheelEvent()
        {
            SetCreateFunction(() => new WheelEvent());
        }

        /// <summary>
        /// The amount of scrolling applied with the mouse wheel.
        /// </summary>
        public Vector3 delta { get; private set; }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">A wheel IMGUI event.</param>
        /// <returns>An initialized event.</returns>
        public new static WheelEvent GetPooled(Event systemEvent)
        {
            WheelEvent e = MouseEventBase<WheelEvent>.GetPooled(systemEvent);
            if (systemEvent != null)
            {
                e.delta = systemEvent.delta;
            }
            return e;
        }

        internal static WheelEvent GetPooled(Vector3 delta, Vector3 mousePosition, EventModifiers modifiers = default)
        {
            WheelEvent e = GetPooled();
            e.delta = delta;
            e.mousePosition = mousePosition;
            e.modifiers = modifiers;
            return e;
        }

        internal static WheelEvent GetPooled(Vector3 delta, IPointerEvent pointerEvent)
        {
            WheelEvent e = GetPooled(pointerEvent);
            e.delta = delta;
            return e;
        }

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown |
                          EventPropagation.SkipDisabledElements;
            delta = Vector3.zero;
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public WheelEvent()
        {
            LocalInit();
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot(this, panel, PointerId.mousePointerId,
                mousePosition);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters an element or one of its descendent elements.
    /// The event trickles down but does not bubble up.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class MouseEnterEvent : MouseEventBase<MouseEnterEvent>
    {
        static MouseEnterEvent()
        {
            SetCreateFunction(() => new MouseEnterEvent());
        }

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
            propagation = EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseEnterEvent()
        {
            LocalInit();
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToAssignedTarget(this, panel);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer exits an element and all its descendent elements.
    /// The event trickles down but does not bubble up.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class MouseLeaveEvent : MouseEventBase<MouseLeaveEvent>
    {
        static MouseLeaveEvent()
        {
            SetCreateFunction(() => new MouseLeaveEvent());
        }

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
            propagation = EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseLeaveEvent()
        {
            LocalInit();
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToAssignedTarget(this, panel);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters an element.
    /// The event trickles down and bubbles up.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class MouseOverEvent : MouseEventBase<MouseOverEvent>
    {
        static MouseOverEvent()
        {
            SetCreateFunction(() => new MouseOverEvent());
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToAssignedTarget(this, panel);
        }

        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            // Updating cursor has to happen on MouseOver/Out because exiting a child does not send a mouse enter to the parent.
            // We can use MouseEvents instead of PointerEvents since only the mouse has a displayed cursor.
            elementTarget.UpdateCursorStyle(eventTypeId);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer exits an element.
    /// The event trickles down and bubbles up.
    /// </summary>
    [EventCategory(EventCategory.EnterLeave)]
    public class MouseOutEvent : MouseEventBase<MouseOutEvent>
    {
        static MouseOutEvent()
        {
            SetCreateFunction(() => new MouseOutEvent());
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToAssignedTarget(this, panel);
        }

        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            // Updating cursor has to happen on MouseOver/Out because exiting a child does not send a mouse enter to the parent.
            // We can use MouseEvents instead of PointerEvents since only the mouse has a displayed cursor.
            elementTarget.UpdateCursorStyle(eventTypeId);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters a window.
    /// The event bubbles up but does not trickle down.
    /// </summary>
    [EventCategory(EventCategory.EnterLeaveWindow)]
    public class MouseEnterWindowEvent : MouseEventBase<MouseEnterWindowEvent>
    {
        static MouseEnterWindowEvent()
        {
            SetCreateFunction(() => new MouseEnterWindowEvent());
        }

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
            propagation = EventPropagation.Bubbles;
            recomputeTopElementUnderMouse = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseEnterWindowEvent()
        {
            LocalInit();
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot(this, panel, PointerId.mousePointerId,
                mousePosition);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer exits a window.
    /// The event bubbles up but does not trickle down.
    /// </summary>
    [EventCategory(EventCategory.EnterLeaveWindow)]
    public class MouseLeaveWindowEvent : MouseEventBase<MouseLeaveWindowEvent>
    {
        static MouseLeaveWindowEvent()
        {
            SetCreateFunction(() => new MouseLeaveWindowEvent());
        }

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
            propagation = EventPropagation.Bubbles;

            // Don't recompute top element before dispatch, we want a last valid target for this event.
            // Note that PostDispatch clears the top element right after, so it will still end up null.
            recomputeTopElementUnderMouse = false;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseLeaveWindowEvent()
        {
            LocalInit();
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI MouseLeaveWindow event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseLeaveWindowEvent GetPooled(Event systemEvent)
        {
            if (systemEvent != null)
                PointerDeviceState.ReleaseAllButtons(PointerId.mousePointerId);

            return MouseEventBase<MouseLeaveWindowEvent>.GetPooled(systemEvent);
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            // If mouse leaves the window, make sure element under mouse is null.
            // However, if pressed button != 0, we are getting a MouseLeaveWindowEvent as part of
            // of a drag and drop operation, at the very beginning of the drag. Since
            // we are not really exiting the window, we do not want to set the element
            // under mouse to null in this case.
            if (pressedButtons == 0 && panel is BaseVisualElementPanel elementPanel)
            {
                elementPanel.ClearCachedElementUnderPointer(PointerId.mousePointerId, this);
                // Call CommitElementUnderPointers manually because recomputeTopElementUnderMouse is false.
                elementPanel.CommitElementUnderPointers();
            }

            base.PostDispatch(panel);
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot(this, panel, PointerId.mousePointerId,
                mousePosition);
        }
    }

    /// <summary>
    /// The event sent when a contextual menu requires menu items.
    /// The event trickles down and bubbles up.
    /// </summary>
    public class ContextualMenuPopulateEvent : MouseEventBase<ContextualMenuPopulateEvent>
    {
        static ContextualMenuPopulateEvent()
        {
            SetCreateFunction(() => new ContextualMenuPopulateEvent());
        }

        /// <summary>
        /// The menu to populate.
        /// </summary>
        public DropdownMenu menu { get; private set; }
        /// <summary>
        /// The event that triggered the ContextualMenuPopulateEvent.
        /// </summary>
        public EventBase triggerEvent { get; private set; }

        ContextualMenuManager m_ContextualMenuManager;

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="triggerEvent">The event that triggered the display of the contextual menu.</param>
        /// <param name="menu">The menu to populate.</param>
        /// <param name="target">The element that triggered the display of the contextual menu.</param>
        /// <param name="menuManager">The menu manager that displays the menu.</param>
        /// <returns>An initialized event.</returns>
        public static ContextualMenuPopulateEvent GetPooled(EventBase triggerEvent, DropdownMenu menu, IEventHandler target, ContextualMenuManager menuManager)
        {
            ContextualMenuPopulateEvent e = GetPooled(triggerEvent);
            if (triggerEvent != null)
            {
                triggerEvent.Acquire();
                e.triggerEvent = triggerEvent;

                if (triggerEvent is IMouseEvent mouseEvent)
                {
                    e.modifiers = mouseEvent.modifiers;
                    e.mousePosition = mouseEvent.mousePosition;
                    e.localMousePosition = mouseEvent.mousePosition;
                    e.mouseDelta = mouseEvent.mouseDelta;
                    e.button = mouseEvent.button;
                    e.clickCount = mouseEvent.clickCount;
                }
                else if (triggerEvent is IPointerEvent pointerEvent)
                {
                    e.modifiers = pointerEvent.modifiers;
                    e.mousePosition = pointerEvent.position;
                    e.localMousePosition = pointerEvent.position;
                    e.mouseDelta = pointerEvent.deltaPosition;
                    e.button = pointerEvent.button;
                    e.clickCount = pointerEvent.clickCount;
                }
            }

            e.elementTarget = (VisualElement) target;
            e.menu = menu;
            e.m_ContextualMenuManager = menuManager;

            return e;
        }

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
            menu = null;
            m_ContextualMenuManager = null;

            if (triggerEvent != null)
            {
                triggerEvent.Dispose();
                triggerEvent = null;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContextualMenuPopulateEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            // StopPropagation will prevent more items from being added to the menu but will not to prevent the menu
            // from showing. To achieve that result, call menu.Clear() and then StopImmediatePropagation().
            if (menu.Count > 0 && m_ContextualMenuManager != null)
            {
                menu.PrepareForDisplay(triggerEvent);
                m_ContextualMenuManager.DoDisplayMenu(menu, triggerEvent);
            }

            base.PostDispatch(panel);
        }
    }
}
