// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ItemView : IReadOnlyCollection<Item>
    {
        public enum Direction
        {
            Forward,
            Backward
        }

        public static Enumerator CreateBackwardEnumerator(IReadOnlyList<Item> items, int fromIndex, ItemTypeFlags flag = ItemTypeFlags.All)
        {
            return new Enumerator(items, fromIndex, flag, Direction.Backward);
        }

        public static Enumerator CreateForwardEnumerator(IReadOnlyList<Item> items, int fromIndex, ItemTypeFlags flag = ItemTypeFlags.All)
        {
            return new Enumerator(items, fromIndex, flag, Direction.Forward);
        }

        readonly IReadOnlyList<Item> m_Items;
        readonly ItemTypeFlags m_Flag;
        readonly Direction m_Direction;

        public ItemView(IReadOnlyList<Item> items, ItemTypeFlags typeFlag = ItemTypeFlags.All, Direction direction = Direction.Forward)
        {
            m_Items = items;
            m_Flag = typeFlag;
            m_Direction = direction;
        }

        public int Count => CountItems();
        public IReadOnlyList<Item> AllItems => m_Items;
        public ItemView Reverse() => new ItemView(m_Items, m_Flag, ReverseDirection(m_Direction));

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<Item> IEnumerable<Item>.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator()
        {
            return m_Direction == Direction.Forward ? new Enumerator(m_Items, -1, m_Flag, m_Direction)
                : new Enumerator(m_Items, m_Items.Count, m_Flag, m_Direction);
        }

        int CountItems()
        {
            var count = 0;
            using Enumerator enumerator = GetEnumerator();
            while (enumerator.MoveNext())
                count++;
            return count;
        }

        static Direction ReverseDirection(Direction direction)
        {
            return direction == Direction.Forward ? Direction.Backward : Direction.Forward;
        }

        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal struct Enumerator : IEnumerator<Item>
        {
            readonly ItemTypeFlags m_Flag;
            readonly Direction m_Direction;
            IReadOnlyList<Item> m_Items;
            int m_Index;

            public Item Current { get; private set; }
            object IEnumerator.Current => Current;

            public Enumerator(IReadOnlyList<Item> items, int fromIdx, ItemTypeFlags flag, Direction direction)
            {
                m_Items = items;
                m_Flag = flag;
                m_Index = fromIdx;
                m_Direction = direction;
                Current = Item.Invalid;
            }

            public bool MoveNext()
            {
                int nextIndex = m_Direction == Direction.Forward ? GetNextItem(m_Index) : GetPreviousItem(m_Index);
                if (nextIndex >= m_Items.Count || nextIndex < 0)
                    return EndIteration();

                m_Index = nextIndex;
                Current = m_Items[m_Index];
                return true;
            }

            bool EndIteration()
            {
                m_Index = m_Items.Count;
                Current = Item.Invalid;
                return false;
            }

            readonly int GetNextItem(int fromIndex)
            {
                while (++fromIndex < m_Items.Count)
                {
                    if (m_Items[fromIndex].type.MatchesFlag(m_Flag))
                        return fromIndex;
                }

                return fromIndex;
            }

            readonly int GetPreviousItem(int fromIndex)
            {
                while (--fromIndex >= 0)
                {
                    if (fromIndex >= m_Items.Count)
                        return -1;
                    if (m_Items[fromIndex].type.MatchesFlag(m_Flag))
                        return fromIndex;
                }

                return fromIndex;
            }

            public void Reset()
            {
                m_Index = -1;
                Current = Item.Invalid;
            }

            public void Dispose()
            {
                m_Items = null;
            }
        }
    }
}
