// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.ObjectModel;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    /// <summary>
    /// Represents a state of Sequence selection
    /// </summary>
    readonly struct SequenceSelection
    {
        public readonly ReadOnlyCollection<UniqueID> tracks;
        public readonly ReadOnlyCollection<UniqueID> clips;
        public readonly ReadOnlyCollection<UniqueID> transitions;
        public readonly ReadOnlyCollection<UniqueID> markers;

        public SequenceSelection(ReadOnlyCollection<UniqueID> tracks,
            ReadOnlyCollection<UniqueID> clips,
            ReadOnlyCollection<UniqueID> transitions,
            ReadOnlyCollection<UniqueID> markers)
        {
            this.tracks = tracks;
            this.clips = clips;
            this.transitions = transitions;
            this.markers = markers;
        }
    }
}
