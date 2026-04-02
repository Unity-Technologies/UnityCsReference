// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;

namespace Unity.Timeline.Foundation.ViewModel
{
    /// <summary>
    /// Information used for an insertion operation.
    /// </summary>
    readonly struct InsertionParameters
    {
        ///<summary> The time at which the items should be inserted. </summary>
        public readonly DiscreteTime insertAtTime;

        ///<summary> The time difference between the previous insertion and the current insertion operation.</summary>
        public readonly DiscreteTime delta;

        ///<summary> The time difference between the beginning of the manipulation and the current insertion operation.</summary>
        public readonly DiscreteTime totalDelta;

        ///<summary> The tracks that should receive the items. </summary>
        public readonly IReadOnlyList<Track> destinationTracks;

        public InsertionParameters(DiscreteTime insertAtTime, DiscreteTime delta, DiscreteTime totalDelta, IReadOnlyList<Track> destinationTracks)
        {
            this.insertAtTime = insertAtTime;
            this.delta = delta;
            this.totalDelta = totalDelta;
            this.destinationTracks = destinationTracks;
        }
    }
}
