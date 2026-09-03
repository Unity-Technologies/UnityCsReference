// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Model.Internals;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        public struct Iterator : IEnumerator<Item>, IEquatable<Iterator>
        {
            readonly CutListData m_CutListData;
            DiscreteTime m_CurrentDuration;

            public int currentIndex { get; private set; }

            Iterator(CutListData cutListData)
                : this(cutListData, 0, DiscreteTime.Zero) { }

            Iterator(CutList cutList, int index, DiscreteTime duration)
                : this(cutList.m_Data, index, duration) { }

            Iterator(CutListData cutListData, int index, DiscreteTime duration)
            {
                m_CutListData = cutListData;
                currentIndex = index;
                m_CurrentDuration = duration;
            }

            internal static Iterator Create_Internal(CutListData cutListData) => new (cutListData);
            internal static Iterator Create_Internal(CutList cutList, int index, DiscreteTime duration) => new (cutList, index, duration);
            internal static Iterator Create_Internal(CutListData data, int index, DiscreteTime duration) => new (data, index, duration);

            List<ItemData> items => m_CutListData.items;

            public bool MoveNext()
            {
                if (items.Count < 1)
                    return false;

                if (currentIndex == -1) //did not start yet
                {
                    currentIndex++;
                    return true;
                }

                if (currentIndex + 1 < items.Count)
                {
                    ItemData currentItem = items[currentIndex];
                    m_CurrentDuration += currentItem.range.duration;
                    currentIndex++;
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                m_CurrentDuration = DiscreteTime.Zero;
                currentIndex = 0;
            }

            public bool IsValid() => currentIndex >= 0 && items != null && currentIndex < items.Count;

            public Item Current => BuildCurrentItem();

            Item BuildCurrentItem()
            {
                if (!IsValid())
                    return Item.Invalid;

                return Item.Create_Internal(m_CutListData, currentIndex, m_CurrentDuration);
            }

            public Iterator Previous()
            {
                if (currentIndex <= 0)
                    return default;

                return BuildPrevious();
            }

            Iterator BuildPrevious()
            {
                int previousIndex = currentIndex - 1;
                ItemData previousData = items[previousIndex];
                DiscreteTime itrDuration = m_CurrentDuration - previousData.range.duration;

                return new Iterator(m_CutListData, previousIndex, itrDuration);
            }

            public Iterator Next()
            {
                if (currentIndex + 1 > items.Count)
                    return default;

                return BuildNext();
            }

            Iterator BuildNext()
            {
                int nextIndex = currentIndex + 1;
                ItemData currentData = items[currentIndex];
                DiscreteTime itrDuration = m_CurrentDuration + currentData.range.duration;

                return new Iterator(m_CutListData, nextIndex, itrDuration);
            }

            public Iterator PreviousClip() => PreviousItem(ItemType.Clip);
            public Iterator NextClip() => NextItem(ItemType.Clip);
            public Iterator PreviousGap() => PreviousItem(ItemType.Gap);
            public Iterator NextGap() => NextItem(ItemType.Gap);

            Iterator PreviousItem(ItemType type)
            {
                Iterator prev = Previous();
                while (prev.IsValid())
                {
                    if (prev.Current.type == type)
                        return prev;
                    prev = prev.Previous();
                }

                return default;
            }

            Iterator NextItem(ItemType type)
            {
                while (MoveNext())
                {
                    if (Current.type == type)
                        return this;
                }

                return default;
            }

            object IEnumerator.Current => Current;

            public void Dispose() { }

            public static implicit operator int(Iterator itr)
            {
                return itr.currentIndex;
            }

            public static bool operator ==(Iterator lhs, Iterator rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Iterator lhs, Iterator rhs)
            {
                return !(lhs == rhs);
            }

            public bool Equals(Iterator other)
            {
                return currentIndex == other.currentIndex;
            }

            public override bool Equals(object obj)
            {
                return obj != null && Equals((Iterator)obj);
            }

            public override int GetHashCode()
            {
                return currentIndex;
            }
        }
    }
}
