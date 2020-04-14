// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public interface IFocusEvent
    {
        Focusable relatedTarget { get; }

        FocusChangeDirection direction { get; }
    }

    public abstract class FocusEventBase<T> : EventBase<T>, IFocusEvent where T : FocusEventBase<T>, new()
    {
        public Focusable relatedTarget { get; private set; }

        public FocusChangeDirection direction { get; private set; }

        protected FocusController focusController { get; private set; }

        internal bool IsFocusDelegated { get; private set; }
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown;
            relatedTarget = null;
            direction = FocusChangeDirection.unspecified;
            focusController = null;
        }

        public static T GetPooled(IEventHandler target, Focusable relatedTarget, FocusChangeDirection direction, FocusController focusController, bool bIsFocusDelegated = false)
        {
            T e = GetPooled();
            e.target = target;
            e.relatedTarget = relatedTarget;
            e.direction = direction;
            e.focusController = focusController;
            e.IsFocusDelegated = bIsFocusDelegated;
            return e;
        }

        protected FocusEventBase()
        {
            LocalInit();
        }
    }

    public class FocusOutEvent : FocusEventBase<FocusOutEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        public FocusOutEvent()
        {
            LocalInit();
        }
    }

    public class BlurEvent : FocusEventBase<BlurEvent>
    {
        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);

            if (relatedTarget == null)
            {
                focusController.DoFocusChange(null);
            }
        }
    }

    public class FocusInEvent : FocusEventBase<FocusInEvent>
    {
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        public FocusInEvent()
        {
            LocalInit();
        }
    }

    public class FocusEvent : FocusEventBase<FocusEvent>
    {
        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);
            focusController.DoFocusChange(target as Focusable);
        }
    }
}
