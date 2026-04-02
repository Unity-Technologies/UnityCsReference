// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SequenceLookup
    {
        readonly IReadOnlyDictionary<UniqueID, Item> m_ItemsIdLookup;
        readonly IReadOnlyDictionary<UniqueID, Track> m_TracksIdLookup;

        public SequenceLookup(IReadOnlyDictionary<UniqueID, Track> tracks, IReadOnlyDictionary<UniqueID, Item> items)
        {
            m_TracksIdLookup = tracks;
            m_ItemsIdLookup = items;
        }

        public static SequenceLookup CreateFrom(Sequence sequence)
        {
            if (sequence == null)
                return new SequenceLookup(new Dictionary<UniqueID, Track>(), new Dictionary<UniqueID, Item>());

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            Dictionary<UniqueID, Track> tracks = sequence.GetFlattenedChildren().ToDictionary(t => t.ID);
            Dictionary<UniqueID, Item> items = tracks.Values.SelectMany(t => t.Items).ToDictionary(t => t.ID);
#pragma warning restore UA2001

            return new SequenceLookup(tracks, items);
        }

        public static SequenceLookup CreateFrom(SequenceLookup previous, SequenceDiff diff)
        {
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (diff.trackChanges.Any(tc => tc.IsChangeOfType(TrackChange.Type.Content)) ||
#pragma warning restore UA2006
                diff.hierarchyChanges.addedTracks.Count > 0 ||
                diff.hierarchyChanges.removedTracks.Count > 0)
            {
                return CreateFrom(diff.sequence);
            }

            return previous;
        }

        public bool DoesIdExist(UniqueID id)
        {
            return m_ItemsIdLookup.ContainsKey(id) || m_TracksIdLookup.ContainsKey(id);
        }

        public Item GetItemFromId(UniqueID id)
        {
            return m_ItemsIdLookup?.GetValue(id) ?? Item.Invalid;
        }

        public Track GetTrackFromId(UniqueID id)
        {
            return m_TracksIdLookup?.GetValue(id);
        }

        public IEnumerable<Item> Items => m_ItemsIdLookup.Values;
    }
}
