// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    static class MarkerListBuilderExtensions
    {
        public static T Add<T>(this T markerList,
            UniqueID id, DiscreteTime time = default, IItemContent content = default)
            where T : ICollection<Marker>
        {
            markerList.Add(new Marker(id, time, content));
            return markerList;
        }

        public static MarkerList ToMarkerList(this IEnumerable<Marker> markerList)
        {
            return new MarkerList(markerList);
        }
    }
}
