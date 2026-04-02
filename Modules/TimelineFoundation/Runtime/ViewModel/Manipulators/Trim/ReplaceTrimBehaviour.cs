// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using UnityEngine;

namespace Unity.Timeline.Foundation.ViewModel
{
    class ReplaceTrimBehaviour : TrimBehaviour
    {
        struct Context
        {
            public CutList trimmedItem;
            public CutList destination;
            public TimeRange initialRange;
            public TimeRange validReplaceRange;
        }

        Context m_Context;
        CutList m_Source;

        protected override void Begin(Item item)
        {
            m_Source = item.parent.GetCutList_Internal();
            m_Context = CreateContext(item, m_Source, location);
        }

        protected override void Trim(DiscreteTime requestedTime)
        {
            CutList.Iterator itr = m_Context.trimmedItem.IteratorAtId(itemToTrim.ID);
            var trimmed = new CutList.Editor(m_Context.trimmedItem);
            var destination = new CutList.Editor(m_Context.destination);

            if (location == Location.Start)
                TrimStart(trimmed, itr, requestedTime, destination);
            else //location == end
                TrimEnd(trimmed, itr, requestedTime, destination);

            destination.InsertMix(trimmed.Finish(), handler);
            viewModel.Dispatch(new SetTrackContents(itemToTrim.parent, destination.Finish()));
        }

        static Context CreateContext(Item item, CutList trackContents, Location location)
        {
            CutList.Iterator iterator = trackContents.IteratorAtId(item.ID);
            var destination = new CutList.Editor(trackContents);
            CutList extracted = trackContents.Extract(iterator);
            destination.RemoveItem(destination.IteratorAtId(item.ID));

            return new Context
            {
                initialRange = item.range,
                trimmedItem = extracted,
                destination = destination.Finish(),
                validReplaceRange = CalculateValidReplaceRange(item, location)
            };
        }

        static TimeRange CalculateValidReplaceRange(Item item, Location location)
        {
            if (location == Location.Start)
            {
                Item previousTr = item.PreviousTransition();
                DiscreteTime start = previousTr.IsValid() ? previousTr.end : DiscreteTime.Zero;
                return new TimeRange(start, item.end);
            }

            if (location == Location.End)
            {
                var nextTr = item.NextTransition();
                var end = nextTr.IsValid() ? nextTr.start : DiscreteTime.MaxValue;
                return new TimeRange(item.start, end);
            }

            return new TimeRange(DiscreteTime.Zero, DiscreteTime.MaxValue);
        }

        void TrimStart(CutList.Editor toTrim, CutList.Iterator itr, DiscreteTime requestedStart, CutList.Editor destination)
        {
            DiscreteTime effectiveStart = requestedStart.Clamp(m_Context.validReplaceRange);
            DiscreteTime trimDelta = effectiveStart - m_Context.initialRange.start;

            toTrim.TrimRangeStart(itr, trimDelta);
            toTrim.RippleMove(itr, trimDelta);

            var removeRange = new TimeRange(effectiveStart, m_Context.initialRange.end);
            RemoveOverlappingItems(destination, removeRange.Clamp(m_Context.validReplaceRange));
        }

        void TrimEnd(CutList.Editor toTrim, CutList.Iterator itr, DiscreteTime requestedEnd, CutList.Editor destination)
        {
            DiscreteTime effectiveEnd = requestedEnd.Clamp(m_Context.validReplaceRange);
            DiscreteTime trimDelta = effectiveEnd - m_Context.initialRange.end;

            toTrim.TrimRangeEnd(itr, trimDelta);

            var removeRange = new TimeRange(m_Context.initialRange.start, effectiveEnd);
            RemoveOverlappingItems(destination, removeRange.Clamp(m_Context.validReplaceRange));
        }

        void RemoveOverlappingItems(CutList.Editor cutList, TimeRange removeRange)
        {
            if (removeRange.duration > DiscreteTime.Zero)
                cutList.ReplaceRangeWithGap(removeRange, handler);
        }
    }
}
