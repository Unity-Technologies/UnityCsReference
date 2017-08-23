// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
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
            flags = EventFlags.Bubbles | EventFlags.Capturable | EventFlags.Cancellable;
        }

        public IMGUIEvent()
        {
            Init();
        }
    }
}
