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

        protected FocusController m_FocusController;

        protected override void Init()
        {
            base.Init();
            flags = EventFlags.TricklesDown;
            relatedTarget = null;
            direction = FocusChangeDirection.unspecified;
            m_FocusController = null;
        }

        public static T GetPooled(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction, FocusController focusController)
        {
            T e = GetPooled();
            e.target = target;
            e.relatedTarget = relatedTarget;
            e.direction = direction;
            e.m_FocusController = focusController;
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
            flags = EventFlags.Bubbles | EventFlags.TricklesDown;
        }

        public FocusOutEvent()
        {
            Init();
        }
    }

    public class BlurEvent : FocusEventBase<BlurEvent>
    {
        protected internal override void PreDispatch()
        {
            m_FocusController.DoFocusChange(relatedTarget);
        }
    }

    public class FocusInEvent : FocusEventBase<FocusInEvent>
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Bubbles | EventFlags.TricklesDown;
        }

        public FocusInEvent()
        {
            Init();
        }
    }

    public class FocusEvent : FocusEventBase<FocusEvent>
    {
        protected internal override void PreDispatch()
        {
            m_FocusController.DoFocusChange(target as Focusable);
        }
    }
}
