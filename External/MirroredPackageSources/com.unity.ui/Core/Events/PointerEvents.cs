using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A static class that holds pointer type values.
    /// </summary>
    /// <remarks>
    /// These values are used as the values for IPointerEvent.pointerType.
    /// </remarks>
    public static class PointerType
    {
        /// <summary>
        /// The pointer type for mouse events.
        /// </summary>
        public static readonly string mouse = "mouse";
        /// <summary>
        /// The pointer type for touch events.
        /// </summary>
        public static readonly string touch = "touch";
        /// <summary>
        /// The pointer type for pen events.
        /// </summary>
        public static readonly string pen = "pen";
        /// <summary>
        /// The pointer type for events created by unknown devices.
        /// </summary>
        public static readonly string unknown = "";

        internal static string GetPointerType(int pointerId)
        {
            if (pointerId == PointerId.mousePointerId)
                return mouse;

            return touch;
        }

        // A direct manipulation device is a device where the user directly manipulates elements
        // (like a touch screen), without any cursor acting as an intermediate.
        internal static bool IsDirectManipulationDevice(string pointerType)
        {
            return ReferenceEquals(pointerType, touch)
                || ReferenceEquals(pointerType, pen);
        }
    }

    /// <summary>
    /// A static class that holds pointer ID values.
    /// </summary>
    /// <remarks>
    /// These values are used as the values for IPointerEvent.pointerId.
    /// </remarks>
    public static class PointerId
    {
        /// <summary>
        /// The maximum number of pointers the implementation supports.
        /// </summary>
        public static readonly int maxPointers = 32;
        /// <summary>
        /// Represents an invalid pointer ID value.
        /// </summary>
        public static readonly int invalidPointerId = -1;
        /// <summary>
        /// The mouse pointer ID.
        /// </summary>
        public static readonly int mousePointerId = 0;
        /// <summary>
        /// The base ID for touch pointers.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a touch event is a number between touchPointerIdBase and touchPointerIdBase + touchPointerCount - 1.
        /// </remarks>
        public static readonly int touchPointerIdBase = 1;
        /// <summary>
        /// The number of touch pointers the implementation supports.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a touch event is a number between touchPointerIdBase and touchPointerIdBase + touchPointerCount - 1.
        /// </remarks>
        public static readonly int touchPointerCount = 20;
        /// <summary>
        /// The base ID for pen pointers.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a pen event is a number between penPointerIdBase and penPointerIdBase + penPointerCount - 1.
        /// </remarks>
        public static readonly int penPointerIdBase = touchPointerIdBase + touchPointerCount;
        /// <summary>
        /// The number of pen pointers the implementation supports.
        /// </summary>
        /// <remarks>
        /// The pointer ID for a pen event is a number between penPointerIdBase and penPointerIdBase + penPointerCount - 1.
        /// </remarks>
        public static readonly int penPointerCount = 2;

        internal static IEnumerable<int> hoveringPointers
        {
            get { yield return mousePointerId; }
        }
    }

    /// <summary>
    /// Interface for pointer events.
    /// </summary>
    public interface IPointerEvent
    {
        /// <summary>
        /// Identifies the pointer that sends the event.
        /// </summary>
        /// <remarks>
        /// If the mouse sends the event, this property is set to 0. If a touchscreen device sends the event, this property is set to the finger ID, which ranges from 1 to the number of touches the device supports.
        /// </remarks>
        int pointerId { get; }
        /// <summary>
        /// The type of pointer that created this event. This value is taken from the value defined in `PointerType`.
        /// </summary>
        string pointerType { get; }
        /// <summary>
        /// Returns true if the pointer is a primary pointer
        /// </summary>
        /// <remarks>
        /// A primary pointer is a pointer that manipulates the mouse cursor. The mouse pointer is always a primary pointer. For touch events, the first finger that touches the screen is the primary pointer. Once processed, pointer events from primary pointers generate compatibility mouse events.
        /// </remarks>
        bool isPrimary { get; }
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
        /// The pointer position in the Screen or World coordinate system.
        /// </summary>
        Vector3 position { get; }
        /// <summary>
        /// The pointer position in the current target coordinate system.
        /// </summary>
        Vector3 localPosition { get; }
        /// <summary>
        /// The difference between the pointer's position during the previous mouse event and its position during the current mouse event.
        /// </summary>
        Vector3 deltaPosition { get; }
        /// <summary>
        /// The amount of time that has passed since the last recorded change in pointer values, in seconds.
        /// </summary>
        float deltaTime { get; }
        /// <summary>
        /// The number of times the button is pressed.
        /// </summary>
        int clickCount { get; }
        /// <summary>
        /// The amount of pressure currently applied by a touch. If the device does not report pressure, the value of this property is 1.0f.
        /// </summary>
        float pressure { get; }
        /// <summary>
        /// The pressure applied to an additional pressure-sensitive control on the stylus.
        /// </summary>
        float tangentialPressure { get; }
        /// <summary>
        /// Angle of the stylus relative to the surface, in radians
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        float altitudeAngle { get; }
        /// <summary>
        /// Angle of the stylus relative to the x-axis, in radians.
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        float azimuthAngle { get; }
        /// <summary>
        /// The rotation of the stylus around its axis, in radians.
        /// </summary>
        float twist { get; }
        /// <summary>
        /// An estimate of the radius of a touch. Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </summary>
        Vector2 radius { get; }
        /// <summary>
        /// Determines the accuracy of the touch radius. Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </summary>
        Vector2 radiusVariance { get; }

        /// <summary>
        /// Flags that hold pressed modifier keys (Alt, Ctrl, Shift, Windows/Cmd).
        /// </summary>
        EventModifiers modifiers { get; }
        /// <summary>
        /// Returns true if the Shift key is pressed.
        /// </summary>
        bool shiftKey { get; }
        /// <summary>
        /// Returns true if the Ctrl key is pressed.
        /// </summary>
        bool ctrlKey { get; }
        /// <summary>
        /// Returns true if the Windows/Cmd key is pressed.
        /// </summary>
        bool commandKey { get; }
        /// <summary>
        /// Returns true if the Alt key is pressed.
        /// </summary>
        bool altKey { get; }
        /// <summary>
        /// Returns true if the platform-specific action key is pressed. This key is Cmd on macOS, and Ctrl on all other platforms.
        /// </summary>
        bool actionKey { get; }
    }

    internal interface IPointerEventInternal
    {
        bool triggeredByOS { get; set; }

        bool recomputeTopElementUnderPointer { get; set; }
    }

    /// <summary>
    /// Base class for pointer events.
    /// </summary>
    /// <remarks>
    /// Pointer events are sent by the mouse, touchscreen, or digital pens.
    /// </remarks>
    public abstract class PointerEventBase<T> : EventBase<T>, IPointerEvent, IPointerEventInternal
        where T : PointerEventBase<T>, new()
    {
        /// <summary>
        /// Identifies the pointer that sends the event.
        /// </summary>
        /// <remarks>
        /// If the mouse sends the event, this property is set to 0. If a touchscreen device sends the event, this property is set to the finger ID, which ranges from 1 to the number of touches the device supports.
        /// </remarks>
        public int pointerId { get; protected set; }
        /// <summary>
        /// The type of pointer that created this event. This value is taken from the value defined in `PointerType`.
        /// </summary>
        public string pointerType { get; protected set; }
        /// <summary>
        /// Returns true if the pointer is a primary pointer
        /// </summary>
        /// <remarks>
        /// A primary pointer is a pointer that manipulates the mouse cursor. The mouse pointer is always a primary pointer. For touch events, the first finger that touches the screen is the primary pointer. Once processed, pointer events from primary pointers generate compatibility mouse events.
        /// </remarks>
        public bool isPrimary { get; protected set; }
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
        /// The pointer position in the Screen or World coordinate system.
        /// </summary>
        public Vector3 position { get; protected set; }
        /// <summary>
        /// The pointer position in the current target coordinate system.
        /// </summary>
        public Vector3 localPosition { get; protected set; }
        /// <summary>
        /// The difference between the pointer's position during the previous mouse event and its position during the current mouse event.
        /// </summary>
        public Vector3 deltaPosition { get; protected set; }
        /// <summary>
        /// The amount of time that has passed since the last recorded change in pointer values, in seconds.
        /// </summary>
        public float deltaTime { get; protected set; }
        /// <summary>
        /// The number of times the button is pressed.
        /// </summary>
        public int clickCount { get; protected set; }
        /// <summary>
        /// The amount of pressure currently applied by a touch. If the device does not report pressure, the value of this property is 1.0f.
        /// </summary>
        public float pressure { get; protected set; }
        /// <summary>
        /// The pressure applied to an additional pressure-sensitive control on the stylus.
        /// </summary>
        public float tangentialPressure { get; protected set; }
        /// <summary>
        /// Angle of the stylus relative to the surface, in radians
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is parallel to the surface. A value of pi/2 indicates that it is perpendicular to the surface.
        /// </remarks>
        public float altitudeAngle { get; protected set; }
        /// <summary>
        /// Angle of the stylus relative to the x-axis, in radians.
        /// </summary>
        /// <remarks>
        /// A value of 0 indicates that the stylus is pointed along the x-axis of the device.
        /// </remarks>
        public float azimuthAngle { get; protected set; }
        /// <summary>
        /// The rotation of the stylus around its axis, in radians.
        /// </summary>
        public float twist { get; protected set; }
        /// <summary>
        /// An estimate of the radius of a touch. Add `radiusVariance` to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </summary>
        public Vector2 radius { get; protected set; }
        /// <summary>
        /// Determines the accuracy of the touch radius. Add this value to the radius to get the maximum touch radius, subtract it to get the minimum touch radius.
        /// </summary>
        public Vector2 radiusVariance { get; protected set; }

        /// <summary>
        /// Flags that hold pressed modifier keys (Alt, Ctrl, Shift, Windows/Cmd).
        /// </summary>
        public EventModifiers modifiers { get; protected set; }

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

        bool IPointerEventInternal.triggeredByOS { get; set; }

        bool IPointerEventInternal.recomputeTopElementUnderPointer { get; set; }

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
            propagateToIMGUI = false;

            pointerId = 0;
            pointerType = PointerType.unknown;
            isPrimary = false;
            button = -1;
            pressedButtons = 0;
            position = Vector3.zero;
            localPosition = Vector3.zero;
            deltaPosition = Vector3.zero;
            deltaTime = 0;
            clickCount = 0;
            pressure = 0;
            tangentialPressure = 0;

            altitudeAngle = 0;
            azimuthAngle = 0;
            twist = 0;
            radius = Vector2.zero;
            radiusVariance = Vector2.zero;

            modifiers = EventModifiers.None;

            ((IPointerEventInternal)this).triggeredByOS = false;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = false;
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
                    localPosition = element.WorldToLocal(position);
                }
                else
                {
                    localPosition = position;
                }
            }
        }

        private static bool IsMouse(Event systemEvent)
        {
            EventType t = systemEvent.rawType;
            return t == EventType.MouseMove
                || t == EventType.MouseDown
                || t == EventType.MouseUp
                || t == EventType.MouseDrag
                || t == EventType.ContextClick
                || t == EventType.MouseEnterWindow
                || t == EventType.MouseLeaveWindow;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">An IMGUI mouse event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();

            if (!(IsMouse(systemEvent) || systemEvent.rawType == EventType.DragUpdated))
            {
                Debug.Assert(false, "Unexpected event type: " + systemEvent.rawType + " (" + systemEvent.type + ")");
            }

            switch (systemEvent.pointerType)
            {
                default:
                    e.pointerType = PointerType.mouse;
                    e.pointerId = PointerId.mousePointerId;
                    break;
                case UnityEngine.PointerType.Touch:
                    e.pointerType = PointerType.touch;
                    e.pointerId = PointerId.touchPointerIdBase;
                    break;
                case UnityEngine.PointerType.Pen:
                    e.pointerType = PointerType.pen;
                    e.pointerId = PointerId.penPointerIdBase;
                    break;
            }

            e.isPrimary = true;

            e.altitudeAngle = 0;
            e.azimuthAngle = 0;
            e.twist = 0;
            e.radius = Vector2.zero;
            e.radiusVariance = Vector2.zero;

            e.imguiEvent = systemEvent;

            if (systemEvent.rawType == EventType.MouseDown)
            {
                PointerDeviceState.PressButton(PointerId.mousePointerId, systemEvent.button);
                e.button = systemEvent.button;
            }
            else if (systemEvent.rawType == EventType.MouseUp)
            {
                PointerDeviceState.ReleaseButton(PointerId.mousePointerId, systemEvent.button);
                e.button = systemEvent.button;
            }
            else if (systemEvent.rawType == EventType.MouseMove)
            {
                e.button = -1;
            }

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = systemEvent.mousePosition;
            e.localPosition = systemEvent.mousePosition;
            e.deltaPosition = systemEvent.delta;
            e.clickCount = systemEvent.clickCount;
            e.modifiers = systemEvent.modifiers;

            switch (systemEvent.pointerType)
            {
                default:
                    e.pressure = e.pressedButtons == 0 ? 0f : 0.5f;
                    break;
                case UnityEngine.PointerType.Touch:
                case UnityEngine.PointerType.Pen:
                    e.pressure = systemEvent.pressure;
                    break;
            }

            e.tangentialPressure = 0;

            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="touch">A <see cref="Touch"/> structure from the InputManager.</param>
        /// <param name="modifiers">The modifier keys held down during the event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(Touch touch, EventModifiers modifiers = EventModifiers.None)
        {
            T e = GetPooled();

            e.pointerId = touch.fingerId + PointerId.touchPointerIdBase;
            e.pointerType = PointerType.touch;

            bool otherTouchDown = false;
            for (var i = PointerId.touchPointerIdBase;
                 i < PointerId.touchPointerIdBase + PointerId.touchPointerCount;
                 i++)
            {
                if (i != e.pointerId && PointerDeviceState.GetPressedButtons(i) != 0)
                {
                    otherTouchDown = true;
                    break;
                }
            }
            e.isPrimary = !otherTouchDown;

            if (touch.phase == TouchPhase.Began)
            {
                PointerDeviceState.PressButton(e.pointerId, 0);
                e.button = 0;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                PointerDeviceState.ReleaseButton(e.pointerId, 0);
                e.button = 0;
            }
            else
            {
                e.button = -1;
            }

            e.pressedButtons = PointerDeviceState.GetPressedButtons(e.pointerId);
            e.position = touch.position;
            e.localPosition = touch.position;
            e.deltaPosition = touch.deltaPosition;
            e.deltaTime = touch.deltaTime;
            e.clickCount = touch.tapCount;
            e.pressure = Mathf.Abs(touch.maximumPossiblePressure) > Mathf.Epsilon ? touch.pressure / touch.maximumPossiblePressure : 1f;
            e.tangentialPressure = 0;

            e.altitudeAngle = touch.altitudeAngle;
            e.azimuthAngle = touch.azimuthAngle;
            e.twist = 0;
            e.radius = new Vector2(touch.radius, touch.radius);
            e.radiusVariance = new Vector2(touch.radiusVariance, touch.radiusVariance);

            e.modifiers = modifiers;

            ((IPointerEventInternal)e).triggeredByOS = true;

            return e;
        }

        internal static T GetPooled(IPointerEvent triggerEvent, Vector2 position, int pointerId)
        {
            if (triggerEvent != null)
            {
                return GetPooled(triggerEvent);
            }

            T e = GetPooled();
            e.position = position;
            e.localPosition = position;
            e.pointerId = pointerId;
            e.pointerType = PointerType.GetPointerType(pointerId);
            return e;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="triggerEvent">The event that sent this event.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(IPointerEvent triggerEvent)
        {
            T e = GetPooled();
            if (triggerEvent != null)
            {
                e.pointerId = triggerEvent.pointerId;
                e.pointerType = triggerEvent.pointerType;
                e.isPrimary = triggerEvent.isPrimary;
                e.button = triggerEvent.button;
                e.pressedButtons = triggerEvent.pressedButtons;
                e.position = triggerEvent.position;
                e.localPosition = triggerEvent.localPosition;
                e.deltaPosition = triggerEvent.deltaPosition;
                e.deltaTime = triggerEvent.deltaTime;
                e.clickCount = triggerEvent.clickCount;
                e.pressure = triggerEvent.pressure;
                e.tangentialPressure = triggerEvent.tangentialPressure;

                e.altitudeAngle = triggerEvent.altitudeAngle;
                e.azimuthAngle = triggerEvent.azimuthAngle;
                e.twist = triggerEvent.twist;
                e.radius = triggerEvent.radius;
                e.radiusVariance = triggerEvent.radiusVariance;

                e.modifiers = triggerEvent.modifiers;

                IPointerEventInternal pointerEventInternal = triggerEvent as IPointerEventInternal;
                if (pointerEventInternal != null)
                {
                    ((IPointerEventInternal)e).triggeredByOS = pointerEventInternal.triggeredByOS;
                }
            }
            return e;
        }

        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            if (((IPointerEventInternal)this).triggeredByOS)
            {
                PointerDeviceState.SavePointerPosition(pointerId, position, panel);
            }
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            for (var i = 0; i < PointerId.maxPointers; i++)
            {
                panel.ProcessPointerCapture(i);
            }

            // If ShouldSendCompatibilityMouseEvents == true, mouse event will take care of this.
            if (!panel.ShouldSendCompatibilityMouseEvents(this) && ((IPointerEventInternal)this).triggeredByOS)
            {
                (panel as BaseVisualElementPanel)?.CommitElementUnderPointers();
            }

            base.PostDispatch(panel);
        }

        protected PointerEventBase()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when a pointer is pressed.
    /// </summary>
    /// <remarks>
    /// PointerDownEvent is sent the first time a finger touches the screen or a mouse button is pressed. Touching the screen with more fingers or pressing additional buttons triggers PointerMoveEvents.
    /// </remarks>
    public sealed class PointerDownEvent : PointerEventBase<PointerDownEvent>
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
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerDownEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (!isDefaultPrevented)
            {
                if (panel.ShouldSendCompatibilityMouseEvents(this))
                {
                    using (var evt = MouseDownEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
            }
            else
            {
                panel.PreventCompatibilityMouseEvents(pointerId);
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// Event sent when a pointer changes state.
    /// </summary>
    public sealed class PointerMoveEvent : PointerEventBase<PointerMoveEvent>
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
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerMoveEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                if (imguiEvent != null && imguiEvent.rawType == EventType.MouseDown)
                {
                    using (var evt = MouseDownEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
                else if (imguiEvent != null && imguiEvent.rawType == EventType.MouseUp)
                {
                    using (var evt = MouseUpEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
                else
                {
                    using (var evt = MouseMoveEvent.GetPooled(this))
                    {
                        evt.target = target;
                        evt.target.SendEvent(evt);
                    }
                }
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// An event sent when a pointer does not change for a set amount of time determined by the operating system.
    /// </summary>
    public sealed class PointerStationaryEvent : PointerEventBase<PointerStationaryEvent>
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
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerStationaryEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when the last depressed button of a pointer is released.
    /// </summary>
    public sealed class PointerUpEvent : PointerEventBase<PointerUpEvent>
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
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerUpEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (PointerType.IsDirectManipulationDevice(pointerType))
            {
                panel.ReleasePointer(pointerId);
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;
                basePanel?.ClearCachedElementUnderPointer(this);
            }

            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                using (var evt = MouseUpEvent.GetPooled(this))
                {
                    evt.target = target;
                    evt.target.SendEvent(evt);
                }
            }

            panel.ActivateCompatibilityMouseEvents(pointerId);

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// Event sent when pointer interaction is cancelled.
    /// </summary>
    public sealed class PointerCancelEvent : PointerEventBase<PointerCancelEvent>
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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerCancelEvent()
        {
            LocalInit();
        }

        protected internal override void PostDispatch(IPanel panel)
        {
            if (PointerType.IsDirectManipulationDevice(pointerType))
            {
                panel.ReleasePointer(pointerId);
                BaseVisualElementPanel basePanel = panel as BaseVisualElementPanel;
                basePanel?.ClearCachedElementUnderPointer(this);
            }

            if (panel.ShouldSendCompatibilityMouseEvents(this))
            {
                using (var evt = MouseUpEvent.GetPooled(this))
                {
                    evt.target = target;
                    target.SendEvent(evt);
                }
            }

            base.PostDispatch(panel);
        }
    }

    /// <summary>
    /// The event sent when the left mouse button is clicked.
    /// </summary>
    /// <remarks>
    /// A click consists of a mouse down event followed by a mouse up event on the same VisualElement. The mouse might move between the two events.
    /// </remarks>
    public sealed class ClickEvent : PointerEventBase<ClickEvent>
    {
        internal static ClickEvent GetPooled(PointerUpEvent pointerEvent, int clickCount)
        {
            var evt = PointerEventBase<ClickEvent>.GetPooled(pointerEvent);
            evt.clickCount = clickCount;
            return evt;
        }
    }

    /// <summary>
    /// Event sent when a pointer enters a VisualElement or one of its descendant.
    /// </summary>
    public sealed class PointerEnterEvent : PointerEventBase<PointerEnterEvent>
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
            propagation = EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerEnterEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when a pointer exits an element and all of its descendant.
    /// </summary>
    public sealed class PointerLeaveEvent : PointerEventBase<PointerLeaveEvent>
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
            propagation = EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public PointerLeaveEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent when a pointer enters a VisualElement.
    /// </summary>
    public sealed class PointerOverEvent : PointerEventBase<PointerOverEvent>
    {
    }

    /// <summary>
    /// Event sent when a pointer exits an element.
    /// </summary>
    public sealed class PointerOutEvent : PointerEventBase<PointerOutEvent>
    {
    }
}
