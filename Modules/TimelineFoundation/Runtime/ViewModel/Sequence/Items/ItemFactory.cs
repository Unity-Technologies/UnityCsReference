// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    static class ItemFactory
    {
        public static Item CreateClip(UniqueID handle, Track parent, int index, TimeRange range, TimeRange contentRange, IItemContent content)
        {
            return new Item(handle, index, Item.Type.Clip, range, contentRange, parent, content);
        }

        public static Item CreateGap(UniqueID handle, Track parent, int index, TimeRange range, IItemContent content)
        {
            return new Item(handle, index, Item.Type.Gap, range, TimeRange.Empty, parent, content);
        }

        public static Item CreateTransition(UniqueID handle, Track parent, int index, TimeRange range, IItemContent content)
        {
            return new Item(handle, index, Item.Type.Transition, range, TimeRange.Empty, parent, content);
        }

        public static Item CreateMarker(UniqueID id, Track parent, int index, DiscreteTime time, IItemContent content)
        {
            return new Item(id, index, Item.Type.Marker, new TimeRange(time, time), TimeRange.Empty, parent, content);
        }
    }
}
