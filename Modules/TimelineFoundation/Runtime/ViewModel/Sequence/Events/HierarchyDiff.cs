// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct HierarchyDiff
    {
        public readonly IReadOnlyList<Track> addedTracks;
        public readonly IReadOnlyList<Track> removedTracks;
        public readonly IReadOnlyList<Track> reorderedTracks;

        public static readonly HierarchyDiff Empty = new HierarchyDiff(
            Array.Empty<Track>(),
            Array.Empty<Track>(),
            Array.Empty<Track>());

        public HierarchyDiff(IReadOnlyList<Track> addedTracks, IReadOnlyList<Track> removedTracks, IReadOnlyList<Track> reorderedTracks)
        {
            this.addedTracks = addedTracks;
            this.removedTracks = removedTracks;
            this.reorderedTracks = reorderedTracks;
        }

        public bool HasChanges()
        {
            return addedTracks?.Count > 0 || removedTracks?.Count > 0 || reorderedTracks?.Count > 0;
        }
    }
}
