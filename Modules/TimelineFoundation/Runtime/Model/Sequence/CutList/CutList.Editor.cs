// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;
using Unity.Timeline.Foundation.Time;
using UnityEngine.TestTools;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        internal class Editor : IEnumerable<Item>
        {
            [Flags]
            public enum ItemFilter
            {
                Clip = 1 << 0,
                Gap = 1 << 1,
                All = Clip | Gap
            }

            CutListData m_CutListData;

            public Editor()
            {
                m_CutListData = CutListData.New();
            }

            public Editor(CutList cutList)
            {
                m_CutListData = cutList.Copy();
            }

            public int Count => m_CutListData.items.Count;
            public DiscreteTime duration => m_CutListData.duration;

            List<ItemData> items => m_CutListData.items;

            public void ChangeRange(int index, TimeRange newRange)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = items[index];
                TimeRange previousRange = item.range;
                item.range = newRange;

                items[index] = item;
                m_CutListData.duration -= previousRange.duration - newRange.duration;
            }

            public void ChangeDuration(int index, DiscreteTime newDuration)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (newDuration < DiscreteTime.Zero)
                    throw new ArgumentException("Duration cannot be negative", nameof(newDuration));

                ItemData item = items[index];
                var newRange = new TimeRange(item.range.start, item.range.start + newDuration);
                ChangeRange(index, newRange);
            }

            public void TrimRangeStart(int index, DiscreteTime amount)
            {
                ItemData item = items[index];
                TimeRange previousRange = item.range;
                var newRange = new TimeRange(previousRange.start + amount, previousRange.end);

                if (newRange.start < DiscreteTime.Zero)
                    newRange += newRange.start.Abs();

                ChangeRange(index, newRange);
            }

            public void TrimRangeEnd(int index, DiscreteTime amount)
            {
                ItemData item = items[index];
                DiscreteTime newDuration = item.range.duration + amount;
                ChangeDuration(index, newDuration);
            }

            public void Insert(int index, in ItemBuilder itemBuilder)
            {
                if (index < 0 || index > items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = itemBuilder.GetItemData_Internal();
                items.Insert(index, item);
                m_CutListData.duration += item.range.duration;
            }

            public void Remove(int index)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = items[index];
                items.RemoveAt(index);
                m_CutListData.duration -= item.range.duration;
            }

            public void Replace(int index, in ItemBuilder item)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                Remove(index);
                Insert(index, item);
            }

            public void ChangeEndTransitionDuration(int index, DiscreteTime newDuration)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (newDuration < DiscreteTime.Zero)
                    throw new ArgumentException("End transition duration cannot be negative", nameof(newDuration));

                ItemData item = items[index];
                item.endTransitionDuration = newDuration;
                items[index] = item;
            }

            public void ChangeItemContent(int index, IItemContent newItemContent)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = items[index];
                item.content = new Internals.Content(newItemContent, item.content.transitionContent);
                items[index] = item;
            }

            public void ChangeEndTransitionContent(int index, IItemContent newTransitionContent)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = items[index];
                item.content = new Internals.Content(item.content.itemContent, newTransitionContent);
                items[index] = item;
            }

            public void RemoveEndTransition(int index)
            {
                if (index < 0 || index >= items.Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                ItemData item = items[index];
                var newContent = new Internals.Content(item.content.itemContent, null);
                item.endTransitionId = UniqueID.Invalid;
                item.endTransitionDuration = DiscreteTime.Zero;
                item.content = newContent;
                items[index] = item;
            }

            // Combines two consecutive gaps
            public void MergeContinuousGaps()
            {
                for (var i = 1; i < items.Count; i++)
                {
                    ItemData previous = items[i - 1];
                    ItemData current = items[i];

                    if (previous.type == ItemType.Gap && current.type == ItemType.Gap)
                    {
                        Remove(i);
                        ChangeDuration(i - 1, previous.range.duration + current.range.duration);
                        i--;
                    }
                }
            }

            // Removes gap located at the end of the cut list
            public void Trim()
            {
                if (items.Count <= 0)
                    return;

                int last = items.Count - 1;
                if (items[last].type == ItemType.Gap)
                    Remove(last);
            }

            public void RemoveItemsBetween(int leftIndex, int rightIndex)
            {
                if (leftIndex > rightIndex)
                    throw new InvalidOperationException("Invalid range");

                if (leftIndex < 0 || leftIndex >= Count)
                    throw new ArgumentOutOfRangeException(nameof(leftIndex));

                if (rightIndex < 0 || rightIndex > Count)
                    throw new ArgumentOutOfRangeException(nameof(rightIndex));

                if (leftIndex == rightIndex)
                    return;

                int rightBound = rightIndex - 1;
                int leftBound = leftIndex + 1;

                for (int i = rightBound; i >= leftBound; i--)
                    Remove(i);
            }

            public void RemoveZeroDurationItems(ItemFilter filter = ItemFilter.All)
            {
                for (int i = Count - 1; i >= 0; i--)
                {
                    ItemData item = m_CutListData.items[i];
                    if (TypeMatchesFilter(item.type, filter) && item.range.duration == DiscreteTime.Zero)
                        Remove(i);
                }
            }

            public void RemoveZeroDurationGaps()
            {
                RemoveZeroDurationItems(ItemFilter.Gap);
            }

            public CutList Finish()
            {
                var cutList = new CutList(m_CutListData);
                m_CutListData = new CutListData();
                return cutList;
            }

            public static bool TypeMatchesFilter(ItemType type, ItemFilter filter)
            {
                return type switch
                {
                    ItemType.Clip => (filter & ItemFilter.Clip) == ItemFilter.Clip,
                    ItemType.Gap => (filter & ItemFilter.Gap) == ItemFilter.Gap,
                    _ => false
                };
            }

            public IEnumerator<Item> GetEnumerator()
            {
                return Iterator.Create_Internal(m_CutListData, -1, DiscreteTime.Zero);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Iterator.Create_Internal(m_CutListData, -1, DiscreteTime.Zero);
            }

            [ExcludeFromCoverage]
            public Item GetItemForId(UniqueID id)
            {
                return m_CutListData.GetItemForId(id);
            }

            [ExcludeFromCoverage]
            public Iterator IteratorAtId(UniqueID id)
            {
                return m_CutListData.IteratorAtId(id);
            }

            [ExcludeFromCoverage]
            public Iterator IteratorAtIndex(int idx)
            {
                return m_CutListData.IteratorAtIndex(idx);
            }

            [ExcludeFromCoverage]
            public Iterator IteratorAtTime(DiscreteTime time, bool inclusiveEnd = false)
            {
                return m_CutListData.IteratorAtTime(time, inclusiveEnd);
            }

            [ExcludeFromCoverage]
            public Iterator GetIteratorAtStart()
            {
                return m_CutListData.GetIteratorAtStart();
            }

            [ExcludeFromCoverage]
            public Iterator GetIteratorAtEnd()
            {
                return m_CutListData.GetIteratorAtEnd(duration);
            }
        }
    }
}
