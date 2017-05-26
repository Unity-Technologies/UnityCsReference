// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public abstract class UIEvent : EventBase
    {
        public UIEvent(EventFlags flags, Event systemEvent)
            : base(flags, systemEvent)
        {
        }
    }

    // This is an event to hold unimplemented UnityEngine.Event EventType.
    // The goal of this is to be able to pass these events to IMGUI.
    public class IMGUIEvent : UIEvent
    {
        static readonly long s_EventClassId;
        static IMGUIEvent()
        {
            s_EventClassId = EventBase.RegisterEventClass();
        }

        public override long GetEventTypeId()
        {
            return s_EventClassId;
        }

        public IMGUIEvent()
            : base(EventFlags.Bubbles | EventFlags.Cancellable, null)
        {
        }

        public IMGUIEvent(Event systemEvent)
            : base(EventFlags.Bubbles | EventFlags.Cancellable, systemEvent)
        {
        }
    }
}
