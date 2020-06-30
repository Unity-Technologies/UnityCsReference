namespace UnityEngine.UIElements
{
    /// <summary>
    /// This event is sent after layout calculations, when the position or the dimension of an element changes.
    /// </summary>
    /// <remarks>
    /// This event does not trickle down or bubble up. It cannot be cancelled.
    /// </remarks>
    public class GeometryChangedEvent : EventBase<GeometryChangedEvent>
    {
        /// <summary>
        /// Gets an event from the event pool, and initializes it with the specified values. Use this method
        /// instead of instancing new events. Use Dispose() to release events back to the event pool.
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
        /// Gets the element's old dimensions.
        /// </summary>
        public Rect oldRect { get; private set; }
        /// <summary>
        /// Gets the elements's new dimensions.
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
