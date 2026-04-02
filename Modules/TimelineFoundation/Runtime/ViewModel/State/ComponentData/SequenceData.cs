// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SequenceData : IReadOnlyData
    {
        public readonly SequenceDiff lastDiff;
        public readonly Sequence sequence;
        public readonly SequenceLookup lookup;

        public SequenceData(Sequence sequence, SequenceLookup lookup)
            : this(sequence, SequenceDiff.Empty(sequence), lookup) { }

        public SequenceData(Sequence sequence, SequenceDiff lastDiff, SequenceLookup lookup)
        {
            this.sequence = sequence;
            this.lastDiff = lastDiff;
            this.lookup = lookup;
        }

        public static implicit operator Sequence(SequenceData data) => data.sequence;

        public Item GetItemFromId(UniqueID id) => lookup.GetItemFromId(id);
        public Track GetTrackFromId(UniqueID id) => lookup.GetTrackFromId(id);
        public IEnumerable<Item> Items => lookup.Items;
    }
}
