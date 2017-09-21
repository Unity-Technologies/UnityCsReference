// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IFocusEvent
    {
        Focusable relatedTarget { get; }

        FocusChangeDirection direction { get; }
    }

    public abstract class FocusEventBase<T> : EventBase<T>, IFocusEvent, IPropagatableEvent where T : FocusEventBase<T>, new()
    {
        public Focusable relatedTarget { get; protected set; }

        public FocusChangeDirection direction { get; protected set; }

        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Capturable;
            relatedTarget = null;
            direction = FocusChangeDirection.unspecified;
        }

        public static T GetPooled(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction)
        {
            T e = GetPooled();
            e.target = target;
            e.relatedTarget = relatedTarget;
            e.direction = direction;
            return e;
        }

        protected FocusEventBase()
        {
            Init();
        }
    }

    public class FocusOutEvent : FocusEventBase<FocusOutEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
        }

        public FocusOutEvent()
        {
            Init();
        }
    }

    public class BlurEvent : FocusEventBase<BlurEvent>
    {
    }

    public class FocusInEvent : FocusEventBase<FocusInEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.Capturable;
        }

        public FocusInEvent()
        {
            Init();
        }
    }

    public class FocusEvent : FocusEventBase<FocusEvent>
    {
    }
}
