// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    class MarkerList : IReadOnlyList<Marker>
    {
        static readonly List<Marker> k_EmptyMarkerList = new(0);

        public static readonly MarkerList Empty = new();

        readonly List<Marker> m_SortedMarkers;

        public MarkerList()
        {
            m_SortedMarkers = k_EmptyMarkerList;
        }

        public MarkerList(IEnumerable<Marker> markers)
        {
            m_SortedMarkers = new List<Marker>(markers);
            m_SortedMarkers.StableSort();
        }

        public int Count => m_SortedMarkers.Count;
        public DiscreteTime Duration => m_SortedMarkers.Count == 0 ? DiscreteTime.Zero : m_SortedMarkers[^1].time;
        public Marker this[int i] => m_SortedMarkers[i];

        public List<Marker>.Enumerator GetEnumerator()
        {
            return m_SortedMarkers.GetEnumerator();
        }

        public TimeRange GetEffectiveRange()
        {
            return Count == 0 ? TimeRange.Empty : new TimeRange(this[0].time, this[^1].time);
        }

        IEnumerator<Marker> IEnumerable<Marker>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    static class MarkerListExtensions
    {
        public static IItemContent GetContentForId(this IEnumerable<Marker> markers, UniqueID id)
        {
            return GetMarkerForId(markers, id).content;
        }

        public static Marker GetMarkerForId(this IEnumerable<Marker> markers, UniqueID id)
        {
            foreach (Marker marker in markers)
            {
                if (id == marker.id)
                    return marker;
            }

            return Marker.Invalid;
        }
    }
}
