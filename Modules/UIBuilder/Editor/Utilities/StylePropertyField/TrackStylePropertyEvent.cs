// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal enum StylePropertyTrackingType
    {
        Register,
        Unregister
    }

    internal class TrackStylePropertyEvent : EventBase<TrackStylePropertyEvent>
    {
        static TrackStylePropertyEvent()
        {
            SetCreateFunction(() => new TrackStylePropertyEvent());
        }

        public string propertyName { get; set; }
        public StylePropertyTrackingType type { get; set; }

        public static TrackStylePropertyEvent GetPooled(string propertyName, StylePropertyTrackingType type)
        {
            var evt = GetPooled();
            evt.bubbles = true;
            evt.tricklesDown = false;
            evt.propertyName = propertyName;
            evt.type = type;
            return evt;
        }
    }
}
