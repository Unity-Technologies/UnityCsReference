// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using UnityEngine.Bindings;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Use this event to improve text input UX by showing composition string at the end of current text field string,
    /// but don't append it to the text, as string might change, reduce in side, etc.
    /// For actual text modification use TextInputEvent.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal struct IMECompositionEvent : IEventProperties
    {
        // TODO most composition strings will be of limited size, like <100 chars, can we avoid a managed object then?
        // TODO maybe by splitting event into multiple if more string is really big?
        // TODO same problem in text input event
        public string compositionString;

        public DiscreteTime timestamp { get; set; }
        public EventSource eventSource { get; set; }
        public uint playerId { get; set; } // TODO probably it doesn't make any sense for splitscreen multiplayer
        public EventModifiers eventModifiers { get; set; }

        public override string ToString()
        {
            return $"IME '{compositionString}'";
        }
    }
}
