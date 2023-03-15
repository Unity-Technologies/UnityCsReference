// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    // This is an event to hold unimplemented UnityEngine.Event EventType.
    // The goal of this is to be able to pass these events to IMGUI.
    /// <summary>
    /// Class used to send a IMGUI event that has no equivalent UIElements event.
    /// </summary>
    [EventCategory(EventCategory.IMGUI)]
    public class IMGUIEvent : EventBase<IMGUIEvent>
    {
        static IMGUIEvent()
        {
            SetCreateFunction(() => new IMGUIEvent());
        }

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
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown;
        }

        /// <summary>
        /// Constructor. Use GetPooled() to get an event from a pool of reusable events.
        /// </summary>
        public IMGUIEvent()
        {
            LocalInit();
        }

        internal override void Dispatch(BaseVisualElementPanel panel)
        {
            EventDispatchUtilities.DispatchToPanelRoot(this, panel);
        }
    }
}
