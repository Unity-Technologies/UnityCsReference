// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface IMouseCaptureEvent
    {
    }

    public abstract class MouseCaptureEventBase<T> : EventBase<T>, IMouseCaptureEvent, IPropagatableEvent where T : MouseCaptureEventBase<T>, new()
    {
        protected override void Init()
        {
            base.Init();
            flags = EventFlags.Capturable | EventFlags.Bubbles;
        }

        public static T GetPooled(IEventHandler target)
        {
            T e = GetPooled();
            e.target = target;
            return e;
        }

        protected MouseCaptureEventBase()
        {
            Init();
        }
    }

    public class MouseCaptureOutEvent : MouseCaptureEventBase<MouseCaptureOutEvent>
    {
    }

    public class MouseCaptureEvent : MouseCaptureEventBase<MouseCaptureEvent>
    {
    }
}
