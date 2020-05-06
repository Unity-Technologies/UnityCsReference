namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base interface for ChangeEvent.
    /// </summary>
    public interface IChangeEvent
    {
    }

    /// <summary>
    /// Sends an event when a value in a field changes.
    /// </summary>
    public class ChangeEvent<T> : EventBase<ChangeEvent<T>>, IChangeEvent
    {
        /// <summary>
        /// The value before the change occured.
        /// </summary>
        public T previousValue { get; protected set; }
        /// <summary>
        /// The new value.
        /// </summary>
        public T newValue { get; protected set; }

        /// <summary>
        /// Sets the event to its initial state.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
            previousValue = default(T);
            newValue = default(T);
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="previousValue">The previous value.</param>
        /// <param name="newValue">The new value.</param>
        /// <returns>An initialized event.</returns>
        public static ChangeEvent<T> GetPooled(T previousValue, T newValue)
        {
            ChangeEvent<T> e = GetPooled();
            e.previousValue = previousValue;
            e.newValue = newValue;
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ChangeEvent()
        {
            LocalInit();
        }
    }
}
