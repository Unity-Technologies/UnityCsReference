using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for pointer capture events and mouse capture events.
    /// </summary>
    public interface IPointerCaptureEvent
    {
    }

    /// <summary>
    /// Base class for pointer capture events and mouse capture events.
    /// </summary>
    public abstract class PointerCaptureEventBase<T> : EventBase<T>, IPointerCaptureEvent where T : PointerCaptureEventBase<T>, new()
    {
        /// <summary>
        /// For PointerCaptureEvent and MouseCaptureEvent, returns the VisualElement that loses the pointer capture, if any. For PointerCaptureOutEvent and MouseCaptureOutEvent, returns the VisualElement that captures the pointer.
        /// </summary>
        public IEventHandler relatedTarget { get; private set; }
        /// <summary>
        /// Identifies the pointer that sends the event.
        /// </summary>
        /// <remarks>
        /// If the mouse sends the event, this property is set to 0. If a touchscreen device sends the event, this property is set to the finger ID, which ranges from 1 to the number of touches the device supports.
        /// </remarks>
        public int pointerId { get; private set; }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
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

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="target">For PointerCapture and MouseCapture events, the element that captures the pointer. For PointerCaptureOut and MouseCaptureOut events, the element that releases the pointer.</param>
        /// <param name="relatedTarget">For PointerCaptureEvent and MouseCaptureEvent, returns the element that loses the pointer capture, if any. For PointerCaptureOutEvent and MouseCaptureOutEvent, returns the element that captures the pointer.</param>
        /// <param name="pointerId">The pointer that is captured or released.</param>
        /// <returns>An initialized event.</returns>
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

    /// <summary>
    /// Event sent when a VisualElement releases a pointer.
    /// </summary>
    public class PointerCaptureOutEvent : PointerCaptureEventBase<PointerCaptureOutEvent>
    {
    }

    /// <summary>
    /// Event sent when a pointer is captured by a VisualElement.
    /// </summary>
    /// <remarks>
    /// When a pointer is captured by a VisualElement, all pointer events are sent to that VisualElement until the pointer is released.
    /// </remarks>
    public class PointerCaptureEvent : PointerCaptureEventBase<PointerCaptureEvent>
    {
    }


    /// <summary>
    /// Interface for mouse capture events.
    /// </summary>
    public interface IMouseCaptureEvent
    {
    }

    /// <summary>
    /// Event sent when the handler capturing the mouse changes.
    /// </summary>
    public abstract class MouseCaptureEventBase<T> : PointerCaptureEventBase<T>, IMouseCaptureEvent where T : MouseCaptureEventBase<T>, new()
    {
        /// <summary>
        /// In the case of a MouseCaptureEvent, this property is the IEventHandler that loses the capture. In the case of a MouseCaptureOutEvent, this property is the IEventHandler that gains the capture.
        /// </summary>
        public new IEventHandler relatedTarget => base.relatedTarget;

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="target">The handler taking or releasing the mouse capture.</param>
        /// <param name="relatedTarget">The related target.</param>
        /// <returns>An initialized event.</returns>
        public static T GetPooled(IEventHandler target, IEventHandler relatedTarget)
        {
            T e = GetPooled(target, relatedTarget, 0);
            return e;
        }

        /// <summary>
        /// Resets the event members to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
        }
    }

    /// <summary>
    /// Event sent before a handler stops capturing the mouse.
    /// </summary>
    public class MouseCaptureOutEvent : MouseCaptureEventBase<MouseCaptureOutEvent>
    {
    }

    /// <summary>
    /// Event sent after a handler starts capturing the mouse.
    /// </summary>
    public class MouseCaptureEvent : MouseCaptureEventBase<MouseCaptureEvent>
    {
    }
}
