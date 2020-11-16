namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for mouse events.
    /// </summary>
    public interface IMouseEvent
    {
        /// <summary>
        /// Flag set holding the pressed modifier keys (Alt, Ctrl, Shift, Windows/Command).
        /// </summary>
        EventModifiers modifiers { get; }
        /// <summary>
        /// The mouse position in the panel coordinate system.
        /// </summary>
        Vector2 mousePosition { get; }
        /// <summary>
        /// The mouse position in the current target coordinate system.
        /// </summary>
        Vector2 localMousePosition { get; }
        /// <summary>
        /// Mouse position difference between the last mouse event and this one.
        /// </summary>
        Vector2 mouseDelta { get; }
        /// <summary>
        /// The number of times the button is pressed.
        /// </summary>
        int clickCount { get; }
        /// <summary>
        /// Integer that indicates which mouse button is pressed: 0 is the left button, 1 is the right button, 2 is the middle button.
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
        bool shiftKey { get; }
        /// <summary>
        /// Return true if the Ctrl key is pressed.
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

    internal interface IMouseEventInternal
    {
        bool triggeredByOS { get; set; }
        bool recomputeTopElementUnderMouse { get; set; }
        IPointerEvent sourcePointerEvent { get; set; }
    }

    /// <summary>
    /// The base class for mouse events.
    /// </summary>
    public abstract class MouseEventBase<T> : EventBase<T>, IMouseEvent, IMouseEventInternal where T : MouseEventBase<T>, new()
    {
        /// <summary>
        /// Flags that hold pressed modifier keys (Alt, Ctrl, Shift, Windows/Cmd).
        /// </summary>
        public EventModifiers modifiers { get; protected set; }
        /// <summary>
        /// The mouse position in the screen coordinate system.
        /// </summary>
        public Vector2 mousePosition { get; protected set; }
        /// <summary>
        /// The mouse position in the current target coordinate system.
        /// </summary>
        public Vector2 localMousePosition { get; internal set; }
        /// <summary>
        /// The difference of the mouse position between the previous mouse event and the current mouse event.
        /// </summary>
        public Vector2 mouseDelta { get; protected set; }
        /// <summary>
        /// The number of times the button is pressed.
        /// </summary>
        public int clickCount { get; protected set; }
        /// <summary>
        /// Integer that indicates which mouse button is pressed: 0 is the left button, 1 is the right button, 2 is the middle button.
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

        bool IMouseEventInternal.triggeredByOS { get; set; }

        bool IMouseEventInternal.recomputeTopElementUnderMouse { get; set; }

        IPointerEvent IMouseEventInternal.sourcePointerEvent { get; set; }

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
            modifiers = EventModifiers.None;
            mousePosition = Vector2.zero;
            localMousePosition = Vector2.zero;
            mouseDelta = Vector2.zero;
            clickCount = 0;
            button = 0;
            pressedButtons = 0;
            ((IMouseEventInternal)this).triggeredByOS = false;
            ((IMouseEventInternal)this).recomputeTopElementUnderMouse = true;
            ((IMouseEventInternal)this).sourcePointerEvent = null;
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

            if (((IMouseEventInternal)this).triggeredByOS)
            {
                PointerDeviceState.SavePointerPosition(PointerId.mousePointerId, mousePosition, panel);
            }
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            EventBase pointerEvent = ((IMouseEventInternal)this).sourcePointerEvent as EventBase;
            if (pointerEvent != null)
            {
                // pointerEvent processing should be done and it should not have returned to the pool.
                Debug.Assert(pointerEvent.processed);

                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();

                // We are a compatibility mouse event. Pass our status to the original pointer event.
                if (isPropagationStopped)
                {
                    pointerEvent.StopPropagation();
                }
                if (isImmediatePropagationStopped)
                {
                    pointerEvent.StopImmediatePropagation();
                }
                if (isDefaultPrevented)
                {
                    pointerEvent.PreventDefault();
                }
                pointerEvent.processedByFocusController |= processedByFocusController;
            }

            base.PostDispatch(panel);
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
                ((IMouseEventInternal)e).triggeredByOS = true;
                ((IMouseEventInternal)e).recomputeTopElementUnderMouse = true;
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
            return GetPooled(position, button, clickCount, delta, modifiers, false);
        }

        internal static T GetPooled(Vector2 position, int button, int clickCount, Vector2 delta,
            EventModifiers modifiers, bool fromOS)
        {
            T e = GetPooled();

            e.modifiers = modifiers;
            e.mousePosition = position;
            e.localMousePosition = position;
            e.mouseDelta = delta;
            e.button = button;
            e.pressedButtons = PointerDeviceState.GetPressedButtons(PointerId.mousePointerId);
            e.clickCount = clickCount;
            ((IMouseEventInternal)e).triggeredByOS = fromOS;
            ((IMouseEventInternal)e).recomputeTopElementUnderMouse = true;

            return e;
        }

        internal static T GetPooled(IMouseEvent triggerEvent, Vector2 mousePosition, bool recomputeTopElementUnderMouse)
        {
            if (triggerEvent != null)
            {
                return GetPooled(triggerEvent);
            }

            T e = GetPooled();
            e.mousePosition = mousePosition;
            e.localMousePosition = mousePosition;
            ((IMouseEventInternal)e).recomputeTopElementUnderMouse = recomputeTopElementUnderMouse;
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

                IMouseEventInternal mouseEventInternal = triggerEvent as IMouseEventInternal;
                if (mouseEventInternal != null)
                {
                    ((IMouseEventInternal)e).triggeredByOS = mouseEventInternal.triggeredByOS;
                    ((IMouseEventInternal)e).recomputeTopElementUnderMouse = false;
                }
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

            e.target = (pointerEvent as EventBase)?.target;
            e.imguiEvent = (pointerEvent as EventBase)?.imguiEvent;

            if ((pointerEvent as EventBase)?.path != null)
                e.path = (pointerEvent as EventBase).path;

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
                ((IMouseEventInternal)e).triggeredByOS = pointerEventInternal.triggeredByOS;
                ((IMouseEventInternal)e).recomputeTopElementUnderMouse = true;

                // Link the mouse event and the pointer event so we can forward
                // the propagation result (for tests and for IMGUI)
                ((IMouseEventInternal)e).sourcePointerEvent = pointerEvent;
            }

            return e;
        }

        protected MouseEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Mouse down event.
    /// </summary>
    public class MouseDownEvent : MouseEventBase<MouseDownEvent>
    {
        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseDownEvent GetPooled(Event systemEvent)
        {
            if (systemEvent != null)
                PointerDeviceState.PressButton(PointerId.mousePointerId, systemEvent.button);

            return MouseEventBase<MouseDownEvent>.GetPooled(systemEvent);
        }

        private static MouseDownEvent MakeFromPointerEvent(IPointerEvent pointerEvent)
        {
            if (pointerEvent != null && pointerEvent.button >= 0)
            {
                PointerDeviceState.PressButton(PointerId.mousePointerId, pointerEvent.button);
            }

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
    /// Mouse up event.
    /// </summary>
    public class MouseUpEvent : MouseEventBase<MouseUpEvent>
    {
        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public new static MouseUpEvent GetPooled(Event systemEvent)
        {
            if (systemEvent != null)
                PointerDeviceState.ReleaseButton(PointerId.mousePointerId, systemEvent.button);

            return MouseEventBase<MouseUpEvent>.GetPooled(systemEvent);
        }

        private static MouseUpEvent MakeFromPointerEvent(IPointerEvent pointerEvent)
        {
            // Release the equivalent mouse button
            if (pointerEvent != null && pointerEvent.button >= 0)
            {
                PointerDeviceState.ReleaseButton(PointerId.mousePointerId, pointerEvent.button);
            }

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
    /// Mouse move event.
    /// </summary>
    public class MouseMoveEvent : MouseEventBase<MouseMoveEvent>
    {
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
    }

    /// <summary>
    /// Mouse wheel event.
    /// </summary>
    public class WheelEvent : MouseEventBase<WheelEvent>
    {
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
            e.imguiEvent = systemEvent;
            if (systemEvent != null)
            {
                e.delta = systemEvent.delta;
            }
            return e;
        }

        internal static WheelEvent GetPooled(Vector3 delta, Vector3 mousePosition)
        {
            WheelEvent e = GetPooled();
            e.delta = delta;
            e.mousePosition = mousePosition;
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
            delta = Vector3.zero;
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public WheelEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters an element or one of its descendent elements. The event is cancellable, it does not trickle down, and it does not bubble up.
    /// </summary>
    public class MouseEnterEvent : MouseEventBase<MouseEnterEvent>
    {
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
            propagation = EventPropagation.TricklesDown | EventPropagation.Cancellable;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseEnterEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer exits an element and all its descendent elements. The event is cancellable, it does not trickle down, and it does not bubble up.
    /// </summary>
    public class MouseLeaveEvent : MouseEventBase<MouseLeaveEvent>
    {
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
            propagation = EventPropagation.TricklesDown | EventPropagation.Cancellable;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseLeaveEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters a window. The event is cancellable, it does not trickle down, and it does not bubble up.
    /// </summary>
    public class MouseEnterWindowEvent : MouseEventBase<MouseEnterWindowEvent>
    {
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
            propagation = EventPropagation.Cancellable;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public MouseEnterWindowEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            EventBase pointerEvent = ((IMouseEventInternal)this).sourcePointerEvent as EventBase;
            if (pointerEvent == null)
            {
                // If pointerEvent != null, base.PostDispatch() will take care of this.
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer exits a window. The event is cancellable, it does not trickle down, and it does not bubble up.
    /// </summary>
    public class MouseLeaveWindowEvent : MouseEventBase<MouseLeaveWindowEvent>
    {
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
            propagation = EventPropagation.Cancellable;
            ((IMouseEventInternal)this).recomputeTopElementUnderMouse = false;
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
            EventBase pointerEvent = ((IMouseEventInternal)this).sourcePointerEvent as EventBase;
            if (pointerEvent == null)
            {
                // If pointerEvent != null, base.PostDispatch() will take care of this.
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }
            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// Event sent when the mouse pointer enters an element. The event trickles down, it bubbles up, and it is cancellable.
    /// </summary>
    public class MouseOverEvent : MouseEventBase<MouseOverEvent>
    {
    }

    /// <summary>
    /// Event sent when the mouse pointer exits an element. The event trickles down, it bubbles up, and it is cancellable.
    /// </summary>
    public class MouseOutEvent : MouseEventBase<MouseOutEvent>
    {
    }

    /// <summary>
    /// The event sent when a contextual menu requires menu items.
    /// </summary>
    public class ContextualMenuPopulateEvent : MouseEventBase<ContextualMenuPopulateEvent>
    {
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

                IMouseEvent mouseEvent = triggerEvent as IMouseEvent;
                if (mouseEvent != null)
                {
                    e.modifiers = mouseEvent.modifiers;
                    e.mousePosition = mouseEvent.mousePosition;
                    e.localMousePosition = mouseEvent.mousePosition;
                    e.mouseDelta = mouseEvent.mouseDelta;
                    e.button = mouseEvent.button;
                    e.clickCount = mouseEvent.clickCount;
                }
                else
                {
                    IPointerEvent pointerEvent = triggerEvent as IPointerEvent;
                    if (pointerEvent != null)
                    {
                        e.modifiers = pointerEvent.modifiers;
                        e.mousePosition = pointerEvent.position;
                        e.localMousePosition = pointerEvent.position;
                        e.mouseDelta = pointerEvent.deltaPosition;
                        e.button = pointerEvent.button;
                        e.clickCount = pointerEvent.clickCount;
                    }
                }

                IMouseEventInternal mouseEventInternal = triggerEvent as IMouseEventInternal;
                if (mouseEventInternal != null)
                {
                    ((IMouseEventInternal)e).triggeredByOS = mouseEventInternal.triggeredByOS;
                }
                else
                {
                    IPointerEventInternal pointerEventInternal = triggerEvent as IPointerEventInternal;
                    if (pointerEventInternal != null)
                    {
                        ((IMouseEventInternal)e).triggeredByOS = pointerEventInternal.triggeredByOS;
                    }
                }
            }

            e.target = target;
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
            if (!isDefaultPrevented && m_ContextualMenuManager != null)
            {
                menu.PrepareForDisplay(triggerEvent);
                m_ContextualMenuManager.DoDisplayMenu(menu, triggerEvent);
            }

            base.PostDispatch(panel);
        }
    }
}
