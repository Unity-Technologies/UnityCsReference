// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public interface IMouseCaptureEvent
    {
    }

    public abstract class MouseCaptureEventBase<T> : EventBase<T>, IMouseCaptureEvent where T : MouseCaptureEventBase<T>, new()
    {
        public IEventHandler relatedTarget { get; private set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles;
            relatedTarget = null;
        }

        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget)
        {
            T e = GetPooled();
            e.target = target;
            e.relatedTarget = relatedTarget;
            return e;
        }

        protected MouseCaptureEventBase()
        {
            LocalInit();
        }
    }

    public class MouseCaptureOutEvent : MouseCaptureEventBase<MouseCaptureOutEvent>
    {
    }

    public class MouseCaptureEvent : MouseCaptureEventBase<MouseCaptureEvent>
    {
    }
}
