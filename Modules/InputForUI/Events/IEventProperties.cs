// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;

namespace UnityEngine.InputForUI
{
    /// <summary>
    /// Common properties that should be implemented by all events
    /// </summary>
    internal interface IEventProperties
    {
        /// <summary>
        /// Timestamp when the event happened
        /// </summary>
        public DiscreteTime timestamp { get; }

        /// <summary>
        /// Event source for the current event.
        /// Could be used in various ways,
        /// one use case in UITK is to ignore navigation events coming from keyboard when we're focused on text field.
        /// </summary>
        public EventSource eventSource { get; }

        /// <summary>
        /// Player Id for split-screen multiplayer scenario. Events associated with specific player will be marked with player id.
        /// From [0, EventProvider.playerCount)
        /// </summary>
        public uint playerId { get; }

        /// <summary>
        /// State of keyboard modifiers when the event happened.
        /// </summary>
        public EventModifiers eventModifiers { get; }
    }
}
