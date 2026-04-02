// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class SequenceQuery
    {
        public static IEnumerable<Item> GetItemSelectionOnTrack(SelectionData selectionData, SequenceData sequenceData, Track track)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetAllSelectedItems(selectionData, sequenceData).Where(i => i != Item.Invalid && track.Equals(i.parent));
#pragma warning restore UA2001
        }

        public static IEnumerable<Track> GetSelectedTracks(SelectionData selectionData, SequenceData sequenceData)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return selectionData.selection.tracks.Select(sequenceData.GetTrackFromId);
#pragma warning restore UA2001
        }

        public static IEnumerable<Item> ItemsInRange(this IEnumerable<Item> items, TimeRange range)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return items.Where(i => range.Intersects(i.GetVisibleRange()));
#pragma warning restore UA2001
        }

        public static IEnumerable<Item> ItemsInRange(this Track track, TimeRange range, ItemTypeFlags flag = ItemTypeFlags.All)
        {
            return track.Items.Only(flag).ItemsInRange(range);
        }

        public static IEnumerable<Item> ItemsBetween(IEnumerable<Track> tracks, Item item1, Item item2)
        {
            var orderedTracks = new List<Track>(tracks);
            orderedTracks.Sort((t1, t2) => t1.index.CompareTo(t2.index));

            var items = new List<Item>();
            foreach (var track in orderedTracks)
                items.AddRange(track.Items);

            int count = Math.Abs(items.IndexOf(item1) - items.IndexOf(item2)) - 1;
            if (count < 1)
                return Array.Empty<Item>();
            return items.GetRange(items.IndexOf(item1.parent.index < item2.parent.index ? item1 : item2) + 1,
                count);
        }

        public static IEnumerable<Item> GetAllSelectedItems(SelectionData selectionData, SequenceData sequenceData)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return selectionData.selection.clips
                .Concat(selectionData.selection.markers)
                .Select(sequenceData.GetItemFromId);
#pragma warning restore UA2001
        }

        public static IEnumerable<Track> TracksBetween(Sequence sequence, Track track1, Track track2, Func<Track, bool> exclude = null)
        {
            if (track1 == null)
                throw new ArgumentNullException(nameof(track1));
            if (track2 == null)
                throw new ArgumentNullException(nameof(track2));

            int start = Math.Min(track1.index, track2.index);
            int end = Math.Max(track1.index, track2.index);

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            IEnumerable<Track> tracks = sequence.GetFlattenedChildren().Where(t => t.index > start && t.index < end);
            if (exclude != null)
                return tracks.Where(t => !exclude.Invoke(t));
#pragma warning restore UA2001
            return tracks;
        }

        public static bool TrackInCollapsedGroup(Track track)
        {
            var parent = track.parent;
            while (parent is Track parentTrack)
            {
                if (parentTrack.GetGenericMetadata().collapsed)
                    return true;
                parent = parentTrack.parent;
            }
            return false;
        }
    }
}
