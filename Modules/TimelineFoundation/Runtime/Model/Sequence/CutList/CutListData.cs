// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model.Internals
{
    struct CutListData
    {
        public DiscreteTime duration;
        public List<ItemData> items;

        public CutListData(List<ItemData> items, DiscreteTime duration)
        {
            this.duration = duration;
            this.items = items;
        }

        public static CutListData New()
        {
            return new CutListData
            {
                items = new List<ItemData>()
            };
        }
    }

    static class CutListDataExtensions
    {
        public static CutListData Copy(this CutListData original)
        {
            var itemsCopy = new List<ItemData>(original.items);
            return new CutListData(itemsCopy, original.duration);
        }

        public static CutList.Item GetItemForId(this CutListData cutListData, UniqueID id)
        {
            CutList.Iterator itr = cutListData.IteratorAtId(id);
            if (!itr.IsValid())
                return CutList.Item.Invalid;

            return itr.Current;
        }

        public static CutList.Iterator IteratorAtId(this CutListData cutListData, UniqueID id)
        {
            DiscreteTime currentTime = DiscreteTime.Zero;

            for (var i = 0; i < cutListData.items.Count; i++)
            {
                ItemData item = cutListData.items[i];
                if (item.id == id)
                    return CutList.Iterator.Create_Internal(cutListData, i, currentTime);
                currentTime += item.range.duration;
            }

            return default;
        }

        public static CutList.Iterator IteratorAtIndex(this CutListData cutListData, int idx)
        {
            DiscreteTime currentTime = DiscreteTime.Zero;

            if (idx < 0 || idx > cutListData.items.Count)
                return default;

            for (var i = 0; i < idx; i++)
                currentTime += cutListData.items[i].range.duration;

            return CutList.Iterator.Create_Internal(cutListData, idx, currentTime);
        }

        public static CutList.Iterator IteratorAtTime(this CutListData cutListData, DiscreteTime time, bool inclusiveEnd = false)
        {
            DiscreteTime currentTime = DiscreteTime.Zero;

            for (var i = 0; i < cutListData.items.Count; i++)
            {
                DiscreteTime duration = cutListData.items[i].range.duration;
                if (!inclusiveEnd && currentTime + duration > time)
                    return CutList.Iterator.Create_Internal(cutListData, i, currentTime);
                if (inclusiveEnd && currentTime + duration >= time)
                    return CutList.Iterator.Create_Internal(cutListData, i, currentTime);
                currentTime += duration;
            }

            return GetIteratorAtEnd(cutListData, currentTime);
        }

        public static CutList.Iterator GetIteratorAtStart(this CutListData cutListData)
        {
            return CutList.Iterator.Create_Internal(cutListData);
        }

        public static CutList.Iterator GetIteratorAtEnd(this CutListData cutListData, DiscreteTime totalDuration)
        {
            return CutList.Iterator.Create_Internal(cutListData, cutListData.items.Count, totalDuration);
        }
    }
}
