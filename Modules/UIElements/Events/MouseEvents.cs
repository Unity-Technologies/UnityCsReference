// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IMouseEvent
    {
        EventModifiers modifiers { get; }
        Vector2 mousePosition { get; }
        Vector2 localMousePosition { get; }
        Vector2 mouseDelta { get; }
        int clickCount { get; }
        int button { get; }

        bool shiftKey { get; }
        bool ctrlKey { get; }
        bool commandKey { get; }
        bool altKey { get; }
        bool actionKey { get; }
    }

    internal interface IMouseEventInternal
    {
        bool hasUnderlyingPhysicalEvent { get; set; }
    }

    public abstract class MouseEventBase<T> : EventBase<T>, IMouseEvent, IMouseEventInternal where T : MouseEventBase<T>, new()
    {
        public EventModifiers modifiers { get; protected set; }
        public Vector2 mousePosition { get; protected set; }
        public Vector2 localMousePosition { get; internal set; }
        public Vector2 mouseDelta { get; protected set; }
        public int clickCount { get; protected set; }
        public int button { get; protected set; }

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

        bool IMouseEventInternal.hasUnderlyingPhysicalEvent { get; set; }

        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.TricklesDown | EventFlags.Cancellable;
            modifiers = EventModifiers.None;
            mousePosition = Vector2.zero;
            localMousePosition = Vector2.zero;
            mouseDelta = Vector2.zero;
            clickCount = 0;
            button = 0;
            ((IMouseEventInternal)this).hasUnderlyingPhysicalEvent = false;
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
                    localMousePosition = element.WorldToLocal(mousePosition);
                }
            }
        }

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
                e.clickCount = systemEvent.clickCount;
                ((IMouseEventInternal)e).hasUnderlyingPhysicalEvent = true;
            }
            return e;
        }

        public static T GetPooled(Vector2 mousePosition)
        {
            T e = GetPooled();
            e.mousePosition = mousePosition;
            return e;
        }

        public static T GetPooled(IMouseEvent triggerEvent)
        {
            T e = GetPooled();
            if (triggerEvent != null)
            {
                e.modifiers = triggerEvent.modifiers;
                e.mousePosition = triggerEvent.mousePosition;
                e.localMousePosition = triggerEvent.mousePosition;
                e.mouseDelta = triggerEvent.mouseDelta;
                e.button = triggerEvent.button;
                e.clickCount = triggerEvent.clickCount;

                IMouseEventInternal mouseEventInternal = triggerEvent as IMouseEventInternal;
                if (mouseEventInternal != null)
                {
                    ((IMouseEventInternal)e).hasUnderlyingPhysicalEvent = mouseEventInternal.hasUnderlyingPhysicalEvent;
                }
            }
            return e;
        }

        protected MouseEventBase()
        {
            Init();
        }
    }

    public class MouseDownEvent : MouseEventBase<MouseDownEvent>
    {
    }

    public class MouseUpEvent : MouseEventBase<MouseUpEvent>
    {
    }

    public class MouseMoveEvent : MouseEventBase<MouseMoveEvent>
    {
    }

    public class ContextClickEvent : MouseEventBase<ContextClickEvent>
    {
    }

    public class WheelEvent : MouseEventBase<WheelEvent>
    {
        public Vector3 delta { get; private set; }

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

        protected override void Init()
        {
            base.Init();
            delta = Vector3.zero;
        }

        public WheelEvent()
        {
            Init();
        }
    }

    public class MouseEnterEvent : MouseEventBase<MouseEnterEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown | EventFlags.Cancellable;
        }

        public MouseEnterEvent()
        {
            Init();
        }
    }

    public class MouseLeaveEvent : MouseEventBase<MouseLeaveEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown | EventFlags.Cancellable;
        }

        public MouseLeaveEvent()
        {
            Init();
        }
    }

    public class MouseEnterWindowEvent : MouseEventBase<MouseEnterWindowEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Cancellable;
        }

        public MouseEnterWindowEvent()
        {
            Init();
        }
    }

    public class MouseLeaveWindowEvent : MouseEventBase<MouseLeaveWindowEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Cancellable;
        }

        public MouseLeaveWindowEvent()
        {
            Init();
        }
    }

    public class MouseOverEvent : MouseEventBase<MouseOverEvent>
    {
    }

    public class MouseOutEvent : MouseEventBase<MouseOutEvent>
    {
    }

    public class ContextualMenuPopulateEvent : MouseEventBase<ContextualMenuPopulateEvent>
    {
        public DropdownMenu menu { get; private set; }
        public EventBase triggerEvent { get; private set; }

        ContextualMenuManager m_ContextualMenuManager;

        public static ContextualMenuPopulateEvent GetPooled(EventBase triggerEvent, DropdownMenu menu, IEventHandler target, ContextualMenuManager menuManager)
        {
            ContextualMenuPopulateEvent e = GetPooled();
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

                IMouseEventInternal mouseEventInternal = triggerEvent as IMouseEventInternal;
                if (mouseEventInternal != null)
                {
                    ((IMouseEventInternal)e).hasUnderlyingPhysicalEvent = mouseEventInternal.hasUnderlyingPhysicalEvent;
                }
            }

            e.target = target;
            e.menu = menu;
            e.m_ContextualMenuManager = menuManager;

            return e;
        }

        protected override void Init()
        {
            base.Init();
            menu = null;
            m_ContextualMenuManager = null;

            if (triggerEvent != null)
            {
                triggerEvent.Dispose();
                triggerEvent = null;
            }
        }

        public ContextualMenuPopulateEvent()
        {
            Init();
        }

        protected internal override void PostDispatch()
        {
            if (!isDefaultPrevented && m_ContextualMenuManager != null)
            {
                menu.PrepareForDisplay(triggerEvent);
                m_ContextualMenuManager.DoDisplayMenu(menu, triggerEvent);
            }
        }
    }
}
