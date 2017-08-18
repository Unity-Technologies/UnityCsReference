// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class FocusEventBase : UIEvent
    {
        static List<long> s_EventSubTypeIds;

        protected new static long RegisterEventClass()
        {
            if (s_EventSubTypeIds == null)
            {
                s_EventSubTypeIds = new List<long>();
            }

            long id = UIEvent.RegisterEventClass();
            s_EventSubTypeIds.Add(id);
            return id;
        }

        public static bool Is(EventBase evt)
        {
            if (s_EventSubTypeIds == null || evt == null)
            {
                return false;
            }

            return s_EventSubTypeIds.Contains(evt.GetEventTypeId());
        }

        public Focusable relatedTarget { get; internal set; }

        public FocusChangeDirection direction { get; internal set; }

        public FocusEventBase(EventFlags flags, IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
            : base(flags, null)
        {
            this.target = target;
            this.relatedTarget = relatedTarget;
            this.direction = direction;
        }
    }

    public class FocusOutEvent : FocusEventBase
    {
        public static readonly long s_EventClassId;

        static FocusOutEvent()
        {
            s_EventClassId = RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public FocusOutEvent()
            : base(EventFlags.Bubbles, null, null, FocusChangeDirection.kUnspecified) {}

        public FocusOutEvent(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
            : base(EventFlags.Bubbles, target, relatedTarget, direction) {}
    }

    public class BlurEvent : FocusEventBase
    {
        public static readonly long s_EventClassId;

        static BlurEvent()
        {
            s_EventClassId = RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public BlurEvent()
            : base(EventFlags.Bubbles, null, null, FocusChangeDirection.kUnspecified) {}

        public BlurEvent(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
            : base(EventFlags.None, target, relatedTarget, direction) {}
    }

    public class FocusInEvent : FocusEventBase
    {
        public static readonly long s_EventClassId;

        static FocusInEvent()
        {
            s_EventClassId = RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public FocusInEvent()
            : base(EventFlags.Bubbles, null, null, FocusChangeDirection.kUnspecified) {}

        public FocusInEvent(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
            : base(EventFlags.Bubbles, target, relatedTarget, direction) {}
    }

    public class FocusEvent : FocusEventBase
    {
        public static readonly long s_EventClassId;

        static FocusEvent()
        {
            s_EventClassId = RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public FocusEvent()
            : base(EventFlags.Bubbles, null, null, FocusChangeDirection.kUnspecified) {}

        public FocusEvent(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
            : base(EventFlags.None, target, relatedTarget, direction) {}
    }
}
