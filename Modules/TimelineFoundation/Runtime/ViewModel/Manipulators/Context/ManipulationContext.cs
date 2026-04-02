// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ManipulationContext
    {
        public readonly TimeRange totalRange;

        public readonly IReadOnlyList<Track> allTracks;
        public readonly IReadOnlyList<Item> allItems;
        public readonly IReadOnlyList<ManipulatedTrack> manipulatedTracks;

        public ManipulationContext(TimeRange totalRange,
                                   IReadOnlyList<Track> allTracks,
                                   IReadOnlyList<Item> allItems,
                                   IReadOnlyList<ManipulatedTrack> manipulatedTracks)
        {
            this.totalRange = totalRange;
            this.allTracks = allTracks;
            this.allItems = allItems;
            this.manipulatedTracks = manipulatedTracks;
        }
    }

    readonly struct ManipulatedTrack
    {
        public readonly Track track;
        public readonly IReadOnlyList<Item> manipulatedItems;
        public readonly IReadOnlyList<Item> items;
        public readonly CutList originalCutList;
        public readonly MarkerList originalMarkerList;

        public ManipulatedTrack(Track track, IReadOnlyList<Item> manipulatedItems)
        {
            this.track = track;
            this.manipulatedItems = manipulatedItems;
            items = track.Items;
            originalCutList = track.GetCutList_Internal();
            originalMarkerList = track.GetMarkerList_Internal();
        }
    }
}
