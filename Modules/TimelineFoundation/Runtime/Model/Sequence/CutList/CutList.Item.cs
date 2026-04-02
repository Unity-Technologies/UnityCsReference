// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;
using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        /*
         * *** Item ranges ***
         *
                A    B   C    D         E     F   G     H
                |    |   |    |         |     |   |     |
                |    |   |    |         |     |   |     |
                |    +----------------------------+     |
                |    |         Container          |     |
                |    |   +--------------------+   |     |
                |    |   |      Content       |   |     |
                |    |   +--------------------+   |     |
                |    |                            |     |
                |    +----------------------------+     |
                |             |         |               |
                +-------------+         +---------------+
                | LTransition |         |  RTransition  |
                +-------------+         +---------------+
         *
         * Content range: C - F (origin: B)
         *   range of the content inside a container in local time (from the clip's start).
         *
         * Visible Range: A - H (origin: 0)
         *   range of the container that is visible in global time (includes transitions).
         *
         * Trimmed range: B - G (origin: 0)
         *   range of the container in global time, without transitions.
         *
         * No overlap range: D - E (origin: 0)
         *   range of the container in global time, without any transition overlap.
         */
        public readonly struct Item : IEquatable<Item>
        {
            public static readonly Item Invalid = new Item(int.MinValue);

            readonly int m_Index;
            readonly ItemData m_Data;
            readonly DiscreteTime m_Offset;
            readonly CutListData m_CutListData;

            Item(CutListData data, int index, DiscreteTime offset)
            {
                m_CutListData = data;
                m_Data = data.items[index];
                m_Index = index;
                m_Offset = offset;
            }

            Item(int index)
            {
                m_Index = index;
                m_Data = ItemData.Invalid;
                m_Offset = DiscreteTime.MinValue;
                m_CutListData = new CutListData();
            }

            internal static Item Create_Internal(CutListData data, int index, DiscreteTime offset)
            {
                return new Item(data, index, offset);
            }

            //Item data

            public UniqueID handle => m_Data.id;
            public ItemType type => m_Data.type;
            public IItemContent content => m_Data.content.itemContent;
            public bool isClip => m_Data.type == ItemType.Clip;
            public bool isGap => m_Data.type == ItemType.Gap;

            //Item ranges

            public TimeRange trimmedRange => new TimeRange(m_Offset, m_Offset + m_Data.range.duration);
            public DiscreteTime trimmedStart => m_Offset;
            public DiscreteTime trimmedEnd => m_Offset + m_Data.range.duration;
            public DiscreteTime trimmedDuration => m_Data.range.duration;

            public TimeRange visibleRange => new TimeRange(visibleStart, visibleEnd);
            public DiscreteTime visibleStart => m_Offset - GetLeftTransition().GetOverlap_Internal(Transition.Location.Right);
            public DiscreteTime visibleEnd => m_Offset + m_Data.range.duration + GetRightTransition().GetOverlap_Internal(Transition.Location.Left);
            public DiscreteTime visibleDuration => visibleRange.duration;

            public TimeRange contentRange => m_Data.range;
            public DiscreteTime contentStart => m_Data.range.start;
            public DiscreteTime contentEnd => m_Data.range.end;
            public DiscreteTime contentDuration => m_Data.range.duration;

            public TimeRange noOverlapRange => new TimeRange(noOverlapStart, noOverlapEnd);
            public DiscreteTime noOverlapStart => m_Offset + GetLeftTransition().GetOverlap_Internal(Transition.Location.Right);
            public DiscreteTime noOverlapEnd
            {
                get
                {
                    Item next = GetNext();
                    DiscreteTime end = next != Invalid ? next.visibleStart : m_Offset + m_Data.range.duration - GetRightTransition().GetOverlap_Internal(Transition.Location.Left);
                    return DiscreteTimeTimeExtensions.Max(noOverlapStart, end);
                }
            }

            public DiscreteTime noOverlapDuration => noOverlapRange.duration;

            //Transition

            public bool hasLeftTransition => GetLeftTransition().isValid;
            public bool hasRightTransition => m_Data.endTransitionId != UniqueID.Invalid;

            public Transition GetLeftTransition()
            {
                Item previous = GetPrevious();
                return previous != Invalid ? previous.GetRightTransition() : Transition.Invalid;
            }

            public Transition GetRightTransition()
            {
                if (m_Data.endTransitionId == UniqueID.Invalid)
                    return Transition.Invalid;

                DiscreteTime transitionDuration = m_Data.endTransitionDuration;
                (DiscreteTime leftOffset, DiscreteTime rightOffset) = transitionDuration.Split();
                var range = new TimeRange(trimmedEnd - leftOffset, trimmedEnd + transitionDuration - rightOffset);
                IItemContent transitionContent = m_Data.content.transitionContent;
                return new Transition(m_Data.endTransitionId, range, transitionContent);
            }

            public Item GetPrevious()
            {
                if (m_Index <= 0)
                    return Invalid;

                ItemData previousItemData = m_CutListData.items[m_Index - 1];
                DiscreteTime offset = m_Offset - previousItemData.range.duration;
                return new Item(m_CutListData, m_Index - 1, offset);
            }

            public Item GetNext()
            {
                if (m_Index >= m_CutListData.items.Count - 1)
                    return Invalid;

                DiscreteTime offset = trimmedEnd;
                return new Item(m_CutListData, m_Index + 1, offset);
            }

            public bool Equals(Item other)
            {
                return handle == other.handle;
            }

            public override bool Equals(object obj)
            {
                return obj is Item other && Equals(other);
            }

            public override int GetHashCode()
            {
                return handle.GetHashCode();
            }

            public static bool operator ==(Item left, Item right)
            {
                return left.handle == right.handle;
            }

            public static bool operator !=(Item left, Item right)
            {
                return !(left == right);
            }

            [ExcludeFromCoverage]
            public override string ToString()
            {
                var leftTransition = $"left transition: {hasLeftTransition} {GetLeftTransition().duration.ToString()}";
                var rightTransition = $"right transition: {hasRightTransition} {GetRightTransition().duration.ToString()}";
                return $"Item type: {type}, trimmed: {trimmedRange}, content: {contentRange}, {leftTransition}, {rightTransition}, ID:{handle}";
            }

            internal ItemData ToItemData_Internal()
            {
                return m_Data;
            }
        }
    }
}
