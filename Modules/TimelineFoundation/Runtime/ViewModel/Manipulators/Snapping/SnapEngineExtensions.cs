// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class SnapEngineExtensions
    {
        public static SnapEngine AddSequenceEdges(this SnapEngine snapEngine, Sequence sequence, IReadOnlyCollection<Item> filterOut = null)
        {
            snapEngine.AddEdge(sequence.duration);

            foreach (Track track in sequence.GetFlattenedChildren())
                snapEngine.AddTrackEdges(track, filterOut);
            return snapEngine;
        }

        public static SnapEngine AddTrackEdges(this SnapEngine snapEngine, Track track, IReadOnlyCollection<Item> filterOut = null)
        {
            snapEngine.AddItemEdges(track.Items, filterOut);
            return snapEngine;
        }

        public static SnapEngine AddItemEdges(this SnapEngine snapEngine, IEnumerable<Item> items, IReadOnlyCollection<Item> filterOut = null)
        {
            foreach (Item item in items)
            {
#pragma warning disable UA2007 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (filterOut != null && filterOut.Contains(item))
#pragma warning restore UA2007
                    continue;

                if (item.isClip)
                {
                    TimeRange visibleRange = item.GetVisibleRange();
                    snapEngine.AddEdge(visibleRange.start)
                        .AddEdge(visibleRange.end);
                }
                else if (item.isMarker)
                {
                    snapEngine.AddEdge(item.start);
                }
            }

            return snapEngine;
        }
    }
}
