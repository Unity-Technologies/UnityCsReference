// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SequenceDiff : IEquatable<SequenceDiff>
    {
        static uint id = 1;

        readonly uint m_Id;
        public Sequence sequence { get; }
        public readonly HierarchyDiff hierarchyChanges;
        public readonly IReadOnlyList<TrackChange> trackChanges;
        public readonly IReadOnlyList<UniqueID> itemContentChanges;
        public readonly IReadOnlyList<UniqueID> markerContentChanges;

        SequenceDiff(Sequence sequence,
                     HierarchyDiff hierarchyChanges,
                     IEnumerable<TrackChange> trackChanges,
                     IEnumerable<UniqueID> itemContentChanges,
                     IEnumerable<UniqueID> markerContentChanges)
        {
            m_Id = ++id;
            this.sequence = sequence;
            this.hierarchyChanges = hierarchyChanges;
            this.trackChanges = new List<TrackChange>(trackChanges);
            this.itemContentChanges = new List<UniqueID>(itemContentChanges);
            this.markerContentChanges = new List<UniqueID>(markerContentChanges);
        }

        public bool HasChanges()
        {
            return hierarchyChanges.HasChanges() || trackChanges?.Count > 0 || itemContentChanges?.Count > 0 || markerContentChanges?.Count > 0;
        }

        public static SequenceDiff Empty(Sequence sequence)
        {
            return new SequenceDiff(sequence, HierarchyDiff.Empty, Array.Empty<TrackChange>(), Array.Empty<UniqueID>(), Array.Empty<UniqueID>());
        }

        public static Builder CreateBuilder(Sequence sequence)
        {
            return new Builder(sequence);
        }

        public struct Builder
        {
            Sequence m_Sequence;

            Dictionary<Track, TrackChange.Type> m_ChangedTracks;
            List<Track> m_AddedTracks;
            List<Track> m_RemovedTracks;
            List<Track> m_ReorderedTracks;
            HashSet<UniqueID> m_ChangedItems;
            HashSet<UniqueID> m_ChangedMarkers;

            public Builder(Sequence sequence)
            {
                m_Sequence = sequence;

                m_ChangedTracks = new Dictionary<Track, TrackChange.Type>();
                m_AddedTracks = new List<Track>();
                m_RemovedTracks = new List<Track>();
                m_ReorderedTracks = new List<Track>();
                m_ChangedItems = new HashSet<UniqueID>();
                m_ChangedMarkers = new HashSet<UniqueID>();
            }

            public Builder PushTrackMetadataChange(Track track)
            {
                if (m_ChangedTracks.ContainsKey(track))
                    m_ChangedTracks[track] |= TrackChange.Type.Metadata;
                else
                    m_ChangedTracks.Add(track, TrackChange.Type.Metadata);
                return this;
            }

            public Builder PushTrackContentChange(Track track)
            {
                if (m_ChangedTracks.ContainsKey(track))
                    m_ChangedTracks[track] |= TrackChange.Type.Content;
                else
                    m_ChangedTracks.Add(track, TrackChange.Type.Content);
                return this;
            }

            public Builder PushTrackAdded(Track toAdd)
            {
                m_AddedTracks.Add(toAdd);
                return this;
            }

            public Builder PushTrackRemoved(Track toRemove)
            {
                m_RemovedTracks.Add(toRemove);
                return this;
            }

            public Builder PushTrackReordered(Track toMove)
            {
                m_ReorderedTracks.Add(toMove);
                return this;
            }

            public Builder PushItemContentChanged(UniqueID hasChanged)
            {
                m_ChangedItems.Add(hasChanged);
                return this;
            }

            public Builder PushMarkerContentChanged(UniqueID hasChanged)
            {
                m_ChangedMarkers.Add(hasChanged);
                return this;
            }

            public SequenceDiff Finish()
            {
                var hierarchyDiff = new HierarchyDiff(m_AddedTracks, m_RemovedTracks, m_ReorderedTracks);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                IEnumerable<TrackChange> changedTracks = m_ChangedTracks.Select(i => new TrackChange(i.Key, i.Value));
#pragma warning restore UA2001

                var sequenceDiff = new SequenceDiff(m_Sequence, hierarchyDiff, changedTracks, m_ChangedItems, m_ChangedMarkers);

                m_ChangedTracks = null;
                m_AddedTracks = null;
                m_RemovedTracks = null;
                m_ReorderedTracks = null;
                m_Sequence = null;

                m_ChangedItems = null;
                m_ChangedMarkers = null;

                return sequenceDiff;
            }
        }

        public bool Equals(SequenceDiff other)
        {
            return m_Id == other.m_Id;
        }

        public override bool Equals(object obj)
        {
            return obj is SequenceDiff other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)m_Id;
        }

        public static bool operator ==(SequenceDiff lhs, SequenceDiff rhs)
        {
            return lhs.m_Id == rhs.m_Id;
        }

        public static bool operator !=(SequenceDiff lhs, SequenceDiff rhs)
        {
            return lhs.m_Id != rhs.m_Id;
        }
    }
}
