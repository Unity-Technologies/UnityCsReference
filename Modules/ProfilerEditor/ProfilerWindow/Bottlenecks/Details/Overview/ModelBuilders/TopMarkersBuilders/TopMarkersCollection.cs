// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    // A simple array of 'top markers' in which we track the index of the lowest
    // one so we can quickly replace it.
    //
    // We avoid the cost of keeping this sorted whilst it is being added to and
    // sort once at the end when retrieving the final list. This keeps the common
    // path of looking up and replacing the lowest item fast.
    //
    // There is intentionally no checking of duplicate marker IDs; if you add
    // multiple markers with the same ID they are treated as separate markers.
    // This is used to find top markers across a range, as we don't want marker
    // instances across frames to be combined.
    class TopMarkersCollection
    {
        const uint k_NoIndex = uint.MaxValue;
        readonly Marker[] m_Markers;
        readonly uint m_Capacity;

        uint m_Count;
        uint m_IndexOfLowestTopMarker;

        public TopMarkersCollection(uint capacity)
        {
            if (capacity == 0U)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

            m_Markers = new Marker[capacity];
            m_Capacity = capacity;
            m_Count = 0;
            m_IndexOfLowestTopMarker = k_NoIndex;
        }

        // Adds a marker with the provided data to the collection if the provided value is
        // larger than the lowest marker's value in the collection. Returns true if the
        // marker was added, or false if not.
        public bool AddMarkerIfNecessary(
                int markerId,
                ulong value,
                Marker.Unit units,
                uint numberOfInstances,
                int frameIndex,
                int threadIndex)
        {
            // Don't add marker if the value is zero.
            if (value == 0UL)
                return false;

            if (m_Count == m_Capacity)
            {
                // If the top list is full, check if the marker belongs in the top list.
                if (value > m_Markers[m_IndexOfLowestTopMarker].Value)
                {
                    // Add the marker to the list by replacing the lowest existing one, and find the new lowest.
                    m_Markers[m_IndexOfLowestTopMarker] = new Marker(
                        markerId,
                        value,
                        units,
                        numberOfInstances,
                        frameIndex,
                        threadIndex);
                    m_IndexOfLowestTopMarker = FindIndexOfLowestTopMarker();

                    return true;
                }
            }
            else
            {
                // If the top list is not full, add the marker to the list and find the new lowest.
                var isFirstMarker = m_Count == 0;
                m_Markers[m_Count++] = new Marker(
                    markerId,
                    value,
                    units,
                    numberOfInstances,
                    frameIndex,
                    threadIndex);

                var indexOfLowestTopMarker = (isFirstMarker) ? 0 : FindIndexOfLowestTopMarker();
                m_IndexOfLowestTopMarker = indexOfLowestTopMarker;

                return true;
            }

            return false;
        }

        public Marker[] ToSortedArray()
        {
            Array.Sort(m_Markers, (a, b) => { return b.Value.CompareTo(a.Value); });
            m_IndexOfLowestTopMarker = m_Count - 1;

            Marker[] copy = new Marker[m_Count];
            Array.Copy(m_Markers, copy, m_Count);
            return copy;
        }

        uint FindIndexOfLowestTopMarker()
        {
            var lowestValue = ulong.MaxValue;
            var lowestIndex = k_NoIndex;
            for (var i = 0U; i < m_Count; ++i)
            {
                var value = m_Markers[i].Value;
                if (value < lowestValue)
                {
                    lowestValue = value;
                    lowestIndex = i;
                }
            }

            return lowestIndex;
        }
    }
}
