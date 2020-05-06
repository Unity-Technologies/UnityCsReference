namespace UnityEngine.UIElements
{
    /// <summary>
    /// Event sent after layout calculations, when the position or the dimension of an element changes. This event cannot be cancelled, it does not trickle down, and it does not bubble up.
    /// </summary>
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent>
    {
        /// <summary>
        /// Gets an event from the event pool and initializes the event with the specified values. Use this method instead of instancing new events. Use Dispose() to release events back to the event pool.
        /// </summary>
        /// <param name="oldRect">The old dimensions of the element.</param>
        /// <param name="newRect">The new dimensions of the element.</param>
        /// <returns>An initialized event.</returns>
        public static GeometryChangedEvent GetPooled(Rect oldRect, Rect newRect)
        {
            GeometryChangedEvent e = GetPooled();
            e.oldRect = oldRect;
            e.newRect = newRect;
            return e;
        }

        /// <summary>
        /// Resets the event values to their initial values.
        /// </summary>
        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            oldRect = Rect.zero;
            newRect = Rect.zero;
            layoutPass = 0;
        }

        /// <summary>
        /// The old dimensions of the element.
        /// </summary>
        public Rect oldRect { get; private set; }
        /// <summary>
        /// The new dimensions of the element.
        /// </summary>
        public Rect newRect { get; private set; }

        internal int layoutPass {get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GeometryChangedEvent()
        {
            LocalInit();
        }
    }
}
