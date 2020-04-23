namespace UnityEngine.UIElements
{
    // This is an event to hold unimplemented UnityEngine.Event EventType.
    // The goal of this is to be able to pass these events to IMGUI.
    public class IMGUIEvent : EventBase<IMGUIEvent>
    {
        public static IMGUIEvent GetPooled(Event systemEvent)
        {
            IMGUIEvent e = GetPooled();
            e.imguiEvent = systemEvent;
            return e;
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        void LocalInit()
        {
            propagation = EventPropagation.Bubbles | EventPropagation.TricklesDown | EventPropagation.Cancellable;
        }

        public IMGUIEvent()
        {
            LocalInit();
        }
    }
}
