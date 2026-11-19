// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;
using UnityEngine.TestTools;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList : IEnumerable<CutList.Item>
    {
        public enum ItemType
        {
            Invalid = 0,
            Clip,
            Gap
        }

        CutListData m_Data;

        //to create a cutlist, you need to go use a Builder
        CutList(CutListData data)
        {
            m_Data = data;
        }

        public DiscreteTime duration => m_Data.duration;
        public int Count => m_Data.items.Count;

        internal IEnumerable<ItemData> Items_Internal => m_Data.items;

        public IEnumerator<Item> GetEnumerator()
        {
            return Iterator.Create_Internal(this, -1, DiscreteTime.Zero);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Iterator.Create_Internal(this, -1, DiscreteTime.Zero);
        }

        [ExcludeFromCoverage]
        public Item GetItemForId(UniqueID id)
        {
            return m_Data.GetItemForId(id);
        }

        [ExcludeFromCoverage]
        public Iterator IteratorAtId(UniqueID id)
        {
            return m_Data.IteratorAtId(id);
        }

        [ExcludeFromCoverage]
        public Iterator IteratorAtIndex(int idx)
        {
            return m_Data.IteratorAtIndex(idx);
        }

        [ExcludeFromCoverage]
        public Iterator IteratorAtTime(DiscreteTime time, bool inclusiveEnd = false)
        {
            return m_Data.IteratorAtTime(time, inclusiveEnd);
        }

        [ExcludeFromCoverage]
        public Iterator GetIteratorAtStart()
        {
            return m_Data.GetIteratorAtStart();
        }

        [ExcludeFromCoverage]
        public Iterator GetIteratorAtEnd()
        {
            return m_Data.GetIteratorAtEnd(m_Data.duration);
        }

        CutListData Copy()
        {
            return m_Data.Copy();
        }
    }
}
