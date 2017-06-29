// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class MouseEventBase : UIEvent
    {
        public EventModifiers modifiers { get; private set; }
        public Vector2 mousePosition { get; private set; }
        public Vector2 localMousePosition { get; internal set; }
        public Vector2 mouseDelta { get; private set; }
        public int clickCount { get; private set; }
        public int button { get; private set; }

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

        public MouseEventBase(EventFlags flags, Event systemEvent)
            : base(flags, systemEvent)
        {
            if (systemEvent != null)
            {
                modifiers = systemEvent.modifiers;
                mousePosition = systemEvent.mousePosition;
                localMousePosition = systemEvent.mousePosition;
                mouseDelta = systemEvent.delta;
                button = systemEvent.button;
                clickCount = systemEvent.clickCount;
            }
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
                    localMousePosition = element.GlobalToBound(mousePosition);
                }
            }
        }
    }

    public class MouseDownEvent : MouseEventBase
    {
        static readonly long s_EventClassId;
        static MouseDownEvent()
        {
            s_EventClassId = EventBase.RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public MouseDownEvent()
            : base(EventFlags.Bubbles | EventFlags.Cancellable, null) {}

        public MouseDownEvent(Event systemEvent)
            : base(EventFlags.Bubbles | EventFlags.Cancellable, systemEvent)
        {
        }
    }

    public class MouseUpEvent : MouseEventBase
    {
        static readonly long s_EventClassId;
        static MouseUpEvent()
        {
            s_EventClassId = EventBase.RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public MouseUpEvent()
            : base(EventFlags.Bubbles | EventFlags.Cancellable, null) {}

        public MouseUpEvent(Event systemEvent)
            : base(EventFlags.Bubbles | EventFlags.Cancellable, systemEvent)
        {
        }
    }

    public class MouseMoveEvent : MouseEventBase
    {
        static readonly long s_EventClassId;
        static MouseMoveEvent()
        {
            s_EventClassId = EventBase.RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public MouseMoveEvent()
            : base(EventFlags.Bubbles | EventFlags.Cancellable, null) {}

        public MouseMoveEvent(Event systemEvent)
            : base(EventFlags.Bubbles | EventFlags.Cancellable, systemEvent)
        {
        }
    }

    public class WheelEvent : MouseEventBase
    {
        public Vector3 delta { get; private set; }

        static readonly long s_EventClassId;
        static WheelEvent()
        {
            s_EventClassId = EventBase.RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public WheelEvent()
            : base(EventFlags.Bubbles | EventFlags.Cancellable, null) {}

        public WheelEvent(Event systemEvent)
            : base(EventFlags.Bubbles | EventFlags.Cancellable, systemEvent)
        {
            delta = systemEvent.delta;
        }
    }
}
