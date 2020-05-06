namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for focus events.
    /// </summary>
    public interface IFocusEvent
    {
        /// <summary>
        /// Related target. See implementation for specific meaning.
        /// </summary>
        Focusable relatedTarget { get; }

        /// <summary>
        /// Direction of the focus change.
        /// </summary>
        FocusChangeDirection direction { get; }
    }

    /// <summary>
    /// Base class for focus related events.
    /// </summary>
    public abstract class FocusEventBase<T> : EventBase<T>, IFocusEvent where T : FocusEventBase<T>, new()
    {
        /// <summary>
        /// For FocusOut and Blur events, contains the element that gains the focus. For FocusIn and Focus events, contains the element that loses the focus.
        /// </summary>
        public Focusable relatedTarget { get; private set; }

        /// <summary>
        /// Direction of the focus change.
        /// </summary>
        public FocusChangeDirection direction { get; private set; }

        /// <summary>
        /// The focus controller that emitted the event.
        /// </summary>
        protected FocusController focusController { get; private set; }
        internal bool IsFocusDelegated { get; private set; }

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
            propagation = EventPropagation.TricklesDown;
            relatedTarget = null;
            direction = FocusChangeDirection.unspecified;
            focusController = null;
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="target">The event target.</param>
        /// <param name="relatedTarget">The related target.</param>
        /// <param name="direction">The direction of the focus change.</param>
        /// <param name="focusController">The object that manages the focus.</param>
        /// <returns>An initialized event.</returns>
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

    /// <summary>
    /// Event sent immediately before an element loses focus. This event trickles down and bubbles up. This event cannot be cancelled.
    /// </summary>
    public class FocusOutEvent : FocusEventBase<FocusOutEvent>
    {
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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Avoid creating new event instances. Instead, use GetPooled() to get an instance from a pool of reusable event instances.
        /// </summary>
        public FocusOutEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent immediately after an element has lost focus. This event trickles down, it does not bubble up, and it cannot be cancelled.
    /// </summary>
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

    /// <summary>
    /// Event sent immediately before an element gains focus. This event trickles down and bubbles up. This event cannot be cancelled.
    /// </summary>
    public class FocusInEvent : FocusEventBase<FocusInEvent>
    {
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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FocusInEvent()
        {
            LocalInit();
        }
    }

    /// <summary>
    /// Event sent immediately after an element has gained focus. This event trickles down, it does not bubble up, and it cannot be cancelled.
    /// </summary>
    public class FocusEvent : FocusEventBase<FocusEvent>
    {
        protected internal override void PreDispatch(IPanel panel)
        {
            base.PreDispatch(panel);
            focusController.DoFocusChange(target as Focusable);
        }
    }
}
