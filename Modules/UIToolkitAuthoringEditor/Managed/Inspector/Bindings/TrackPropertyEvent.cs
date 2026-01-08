// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum PropertyTrackingType
    {
        Register,
        Unregister
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class TrackPropertyEvent : EventBase<TrackPropertyEvent>
    {
        static TrackPropertyEvent()
        {
            SetCreateFunction(() => new TrackPropertyEvent());
        }

        public ITrackablePropertyProvider provider { get; set; }
        public string propertyName { get; set; }

        public static TrackPropertyEvent GetPooled(ITrackablePropertyProvider provider, string propertyName)
        {
            var evt = GetPooled();
            evt.bubbles = true;
            evt.tricklesDown = false;
            evt.provider = provider;
            evt.propertyName = propertyName;
            return evt;
        }
    }
}
