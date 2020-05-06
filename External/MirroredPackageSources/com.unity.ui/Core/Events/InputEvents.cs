namespace UnityEngine.UIElements
{
    /// <summary>
    /// Sends an event when text from a TextField changes.
    /// </summary>
    public class InputEvent : EventBase<InputEvent>
    {
        /// <summary>
        /// The text before the change occured.
        /// </summary>
        public string previousData { get; protected set; }
        /// <summary>
        /// The new text.
        /// </summary>
        public string newData { get; protected set; }

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
            previousData = default(string);
            newData = default(string);
        }

        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="previousData">The text before the change occured.</param>
        /// <param name="newData">The new text.</param>
        /// <returns>An initialized event.</returns>
        public static InputEvent GetPooled(string previousData, string newData)
        {
            InputEvent e = GetPooled();
            e.previousData = previousData;
            e.newData = newData;
            return e;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public InputEvent()
        {
            LocalInit();
        }
    }
}
