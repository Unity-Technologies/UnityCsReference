// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public interface IPointerCaptureEvent
    {
    }

    public abstract class PointerCaptureEventBase<T> : EventBase<T>, IPointerCaptureEvent where T : PointerCaptureEventBase<T>, new()
    {
        public IEventHandler relatedTarget { get; private set; }
        public int pointerId { get; private set; }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.TricklesDown | EventPropagation.Bubbles;
            relatedTarget = null;
            pointerId = PointerId.invalidPointerId;
        }

        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget, int pointerId)
        {
            T e = GetPooled();
            e.target = target;
            e.relatedTarget = relatedTarget;
            e.pointerId = pointerId;
            return e;
        }

        protected PointerCaptureEventBase()
        {
            LocalInit();
        }
    }

    public class PointerCaptureOutEvent : PointerCaptureEventBase<PointerCaptureOutEvent>
    {
    }

    public class PointerCaptureEvent : PointerCaptureEventBase<PointerCaptureEvent>
    {
    }


    public interface IMouseCaptureEvent
    {
    }

    public abstract class MouseCaptureEventBase<T> : PointerCaptureEventBase<T>, IMouseCaptureEvent where T : MouseCaptureEventBase<T>, new()
    {
        public new IEventHandler relatedTarget => base.relatedTarget;

        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget)
        {
            T e = GetPooled(target, relatedTarget, 0);
            return e;
        }

        protected override void Init()
        {
            base.Init();
        }
    }

    public class MouseCaptureOutEvent : MouseCaptureEventBase<MouseCaptureOutEvent>
    {
    }

    public class MouseCaptureEvent : MouseCaptureEventBase<MouseCaptureEvent>
    {
    }
}
