// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public static class PointerType
    {
        public static readonly string mouse = "mouse";
        public static readonly string touch = "touch";
        public static readonly string pen = "pen";
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

    public static class PointerId
    {
        public static readonly int maxPointers = 32;
        public static readonly int invalidPointerId = -1;
        public static readonly int mousePointerId = 0;
        public static readonly int touchPointerIdBase = 1;
        public static readonly int touchPointerCount = 20;
        public static readonly int penPointerIdBase = touchPointerIdBase + touchPointerCount;
        public static readonly int penPointerCount = 2;

        internal static IEnumerable<int> hoveringPointers
        {
            get { yield return mousePointerId; }
        }
    }

    public interface IPointerEvent
    {
        int pointerId { get; }
        string pointerType { get; }
        bool isPrimary { get; }
        int button { get; }
        int pressedButtons { get; }

        Vector3 position { get; }
        Vector3 localPosition { get; }
        Vector3 deltaPosition { get; }
        float deltaTime { get; }
        int clickCount { get; }
        float pressure { get; }
        float tangentialPressure { get; }
        float altitudeAngle { get; }
        float azimuthAngle { get; }
        float twist { get; }
        Vector2 radius { get; }
        Vector2 radiusVariance { get; }

        EventModifiers modifiers { get; }
        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool commandKey { get; }
        bool altKey { get; }
        bool actionKey { get; }
    }

    internal interface IPointerEventInternal
    {
        bool triggeredByOS { get; set; }

        bool recomputeTopElementUnderPointer { get; set; }
    }

    public abstract class PointerEventBase<T> : EventBase<T>, IPointerEvent, IPointerEventInternal
        where T : PointerEventBase<T>, new()
    {
        public int pointerId { get; protected set; }
        public string pointerType { get; protected set; }
        public bool isPrimary { get; protected set; }
        public int button { get; protected set; }
        public int pressedButtons { get; protected set; }
        public Vector3 position { get; protected set; }
        public Vector3 localPosition { get; protected set; }
        public Vector3 deltaPosition { get; protected set; }
        public float deltaTime { get; protected set; }
        public int clickCount { get; protected set; }
        public float pressure { get; protected set; }
        public float tangentialPressure { get; protected set; }
        public float altitudeAngle { get; protected set; }
        public float azimuthAngle { get; protected set; }
        public float twist { get; protected set; }
        public Vector2 radius { get; protected set; }
        public Vector2 radiusVariance { get; protected set; }

        public EventModifiers modifiers { get; protected set; }

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

        public static T GetPooled(Event systemEvent)
        {
            T e = GetPooled();

            Debug.Assert(IsMouse(systemEvent) || systemEvent.rawType == EventType.DragUpdated, "Unexpected event type: " + systemEvent.rawType + " (" + systemEvent.type + ")");

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
            if (!panel.ShouldSendCompatibilityMouseEvents(this))
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

    public sealed class PointerDownEvent : PointerEventBase<PointerDownEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

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

    public sealed class PointerMoveEvent : PointerEventBase<PointerMoveEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

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
                else if (imguiEvent != null && imguiEvent.rawType == EventType.DragUpdated)
                {
                    using (var evt = DragUpdatedEvent.GetPooled(this))
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

    public sealed class PointerStationaryEvent : PointerEventBase<PointerStationaryEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

        public PointerStationaryEvent()
        {
            LocalInit();
        }
    }

    public sealed class PointerUpEvent : PointerEventBase<PointerUpEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            ((IPointerEventInternal)this).recomputeTopElementUnderPointer = true;
        }

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
                basePanel?.SetElementUnderPointer(null, this);
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

    public sealed class PointerCancelEvent : PointerEventBase<PointerCancelEvent>
    {
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
                basePanel?.SetElementUnderPointer(null, this);
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

    public sealed class PointerEnterEvent : PointerEventBase<PointerEnterEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown;
        }

        public PointerEnterEvent()
        {
            LocalInit();
        }
    }

    public sealed class PointerLeaveEvent : PointerEventBase<PointerLeaveEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown;
        }

        public PointerLeaveEvent()
        {
            LocalInit();
        }
    }

    public sealed class PointerOverEvent : PointerEventBase<PointerOverEvent>
    {
    }

    public sealed class PointerOutEvent : PointerEventBase<PointerOutEvent>
    {
    }
}
