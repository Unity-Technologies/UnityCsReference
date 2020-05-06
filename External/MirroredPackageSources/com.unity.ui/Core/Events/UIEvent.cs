namespace UnityEngine.UIElements
{
    // This is an event to hold unimplemented UnityEngine.Event EventType.
    // The goal of this is to be able to pass these events to IMGUI.
    /// <summary>
    /// Class used to send a IMGUI event that has no equivalent UIElements event.
    /// </summary>
    public class IMGUIEvent : EventBase<IMGUIEvent>
    {
        /// <summary>
        /// Gets an event from the event pool and initializes it with the given values. Use this function instead of creating new events. Events obtained using this method need to be released back to the pool. You can use `Dispose()` to release them.
        /// </summary>
        /// <param name="systemEvent">The IMGUI event used to initialize the event.</param>
        /// <returns>An initialized event.</returns>
        public static IMGUIEvent GetPooled(Event systemEvent)
        {
            IMGUIEvent e = GetPooled();
            e.imguiEvent = systemEvent;
            return e;
        }

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public IMGUIEvent()
        {
            LocalInit();
        }
    }
}
