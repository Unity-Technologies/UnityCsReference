// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;

namespace Unity.Timeline.Foundation.Model
{
    static class MarkerEditorExtensions
    {
        public static T InsertMarkers<T>(this T markers, IEnumerable<Marker> toInsert, DiscreteTime offset = default)
            where T : ICollection<Marker>
        {
            foreach (Marker markerToInsert in toInsert)
            {
                markers.Add(markerToInsert.WithTime(markerToInsert.time + offset));
            }

            return markers;
        }

        public static T RippleMarkers<T>(this T markers, DiscreteTime time, DiscreteTime delta)
            where T : IList<Marker>
        {
            for (var i = 0; i < markers.Count; i++)
            {
                Marker marker = markers[i];
                if (marker.time >= time)
                    markers[i] = marker.WithTime(marker.time + delta);
            }

            return markers;
        }
    }
}
