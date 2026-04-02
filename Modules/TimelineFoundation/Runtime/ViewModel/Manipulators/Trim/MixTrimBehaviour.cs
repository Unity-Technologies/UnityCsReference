// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Selection;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel.Internals;
using UnityEngine;

namespace Unity.Timeline.Foundation.ViewModel
{
    class MixTrimBehaviour : TrimBehaviour
    {
        readonly struct Context : IEquatable<Context>
        {
            public readonly CutList trimmedItem;
            public readonly CutList destination;
            public readonly TimeRange initialRange;
            public readonly TimeRange validTrimRange;

            public static readonly Context Invalid = default;

            public Context(CutList trimmedItem, CutList destination, TimeRange initialRange, TimeRange validTrimRange)
            {
                this.trimmedItem = trimmedItem;
                this.destination = destination;
                this.initialRange = initialRange;
                this.validTrimRange = validTrimRange;
            }

            public bool Equals(Context other) => Equals(trimmedItem, other.trimmedItem) && Equals(destination, other.destination) && initialRange.Equals(other.initialRange) && validTrimRange.Equals(other.validTrimRange);
            public override bool Equals(object obj) => obj is Context other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(trimmedItem, destination, initialRange, validTrimRange);
            public static bool operator ==(Context left, Context right) => left.Equals(right);
            public static bool operator !=(Context left, Context right) => !(left == right);
        }

        CutList m_Source;
        Context m_Context = Context.Invalid;
        Context m_AdjacentContext = Context.Invalid;

        protected override void Begin(Item item)
        {
            m_Source = item.parent.GetCutList_Internal();
            m_Context = CreateContext(item, m_Source, location, handler);
            m_AdjacentContext = CreateAdjacentContext(item, location, m_Source);
        }

        protected override void Trim(DiscreteTime requestedTime)
        {
            if (m_AdjacentContext == Context.Invalid)
            {
                Trim(itemToTrim, location, requestedTime, m_Context);
                viewModel.Dispatch(new Select(itemToTrim.ID));
            }
            else
            {
                Item adjacentItem = GetAdjacentClip(location, itemToTrim);
                if (adjacentItem == Item.Invalid) // if a transition has been trimmed to 0 then we'll need to find the adjacent clip allowing no-transition in between (as it has been removed).
                {
                    // a duplicate is created here as GetAdjacentClip will try to GetNext/Previous on the Item. However, the index of the trimmed item may be invalid due to the removed transition
                    int indexOffset = location == Location.Start ? -1 : 1;
                    Item duplicate = ItemFactory.CreateClip(itemToTrim.ID, itemToTrim.parent, itemToTrim.index + indexOffset, itemToTrim.range, itemToTrim.contentRange, itemToTrim.GetGenericContent());
                    adjacentItem = GetAdjacentClip(
                        location,
                        duplicate,
                        false);
                }

                // if m_AdjacentContext is set, then the manipulation is centered on a transition edge. In this case we need to perform the Trim on the adjacent item on its opposite edge.
                Trim(adjacentItem, InvertLocation(location), requestedTime, m_AdjacentContext);
            }
        }

        protected override void Finish()
        {
            base.Finish();
            m_AdjacentContext = Context.Invalid;
            m_Context = Context.Invalid;
            m_Source = null;
        }

        void Trim(Item item, Location trimLocation, DiscreteTime requestedTime, Context context)
        {
            CutList.Iterator itr = context.trimmedItem.IteratorAtId(item.ID);
            if (!itr.IsValid())
            {
                if (context == m_AdjacentContext) // this occurs when we're manipulating the adjacent clip but the blend between the two has been trimmed to 0
                {
                    itr = m_Context.trimmedItem.IteratorAtId(item.ID);
                    if (!itr.IsValid())
                        return;

                    if (trimLocation == Location.Start || trimLocation == Location.End)
                    {
                        itr = m_AdjacentContext.trimmedItem.IteratorAtId(item.ID);
                        var adjacentTrimmed = Trim(trimLocation, requestedTime, context, new CutList.Editor(m_AdjacentContext.trimmedItem), itr);
                        viewModel.Dispatch(new SetTrackContents(item.parent, adjacentTrimmed.Finish()));
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            var trimmed = new CutList.Editor(context.trimmedItem);
            CutList.Editor result = Trim(trimLocation, requestedTime, context, trimmed, itr);
            viewModel.Dispatch(new SetTrackContents(item.parent, result.Finish()));
        }

        CutList.Editor Trim(Location trimLocation, DiscreteTime requestedTime, Context context, CutList.Editor trimmed, CutList.Iterator itr)
        {
            if (trimLocation == Location.Start)
                TrimStart(trimmed, itr, requestedTime, context);
            else //location == end
                TrimEnd(trimmed, itr, requestedTime, context);

            var result = new CutList.Editor(context.destination);
            result.InsertMix(trimmed.Finish(), handler);
            return result;
        }

        static Context CreateContext(Item item, CutList trackContents, Location location, IContentHandler handler)
        {
            CutList.Iterator iterator = trackContents.IteratorAtId(item.ID);
            var destination = new CutList.Editor(trackContents);
            CutList extracted = trackContents.Extract(iterator);
            destination.RemoveItem(destination.IteratorAtId(item.ID));

            return new Context(extracted, destination.Finish(), item.range, CalculateValidTrimRange(iterator, location, handler));
        }

        static Context CreateAdjacentContext(Item item, Location location, CutList source)
        {
            Item adjacent = GetAdjacentClip(location, item);
            if (adjacent != Item.Invalid)
            {
                TimeRange validRange;
                TimeRange initialRange;
                switch (location)
                {
                    case Location.Start:
                        Item next = adjacent.Next();
                        initialRange = new TimeRange(adjacent.start, next.end);
                        validRange = new TimeRange(adjacent.start, item.end);
                        break;
                    case Location.End:
                        Item previous = adjacent.Previous();
                        initialRange = new TimeRange(previous.start, adjacent.end);
                        validRange = new TimeRange(item.start, adjacent.end);
                        break;
                    default:
                    {
                        initialRange = TimeRange.Empty;
                        validRange = TimeRange.Empty;
                        break;
                    }
                }

                return CreateContext(adjacent, initialRange, validRange, source);
            }

            return Context.Invalid;
        }

        static Context CreateContext(Item item, TimeRange initialRange, TimeRange validRange, CutList trackContents)
        {
            CutList.Iterator iterator = trackContents.IteratorAtId(item.ID);
            var destination = new CutList.Editor(trackContents);
            CutList extracted = trackContents.Extract(iterator);
            destination.RemoveItem(destination.IteratorAtId(item.ID));

            return new Context(extracted, destination.Finish(), initialRange, validRange);
        }

        static TimeRange CalculateValidTrimRange(CutList.Iterator itr, Location location, IContentHandler handler)
        {
            if (location == Location.Start)
            {
                DiscreteTime start = EditorMixExtensions.CalculateMinimumTrimStartTime(itr, handler);
                return new TimeRange(start, itr.Current.noOverlapEnd);
            }
            else //location == end
            {
                DiscreteTime end = EditorMixExtensions.CalculateMaximumTrimEndTime(itr, handler);
                return new TimeRange(itr.Current.noOverlapStart, end);
            }
        }

        static void TrimStart(CutList.Editor toTrim, CutList.Iterator itr, DiscreteTime requestedStart, Context context)
        {
            DiscreteTime effectiveStart = requestedStart.Clamp(context.validTrimRange);
            DiscreteTime trimDelta = effectiveStart - context.initialRange.start;

            toTrim.TrimRangeStart(itr, trimDelta);
            toTrim.RippleMove(itr, trimDelta);
        }

        static void TrimEnd(CutList.Editor toTrim, CutList.Iterator itr, DiscreteTime requestedEnd, Context context)
        {
            DiscreteTime effectiveEnd = requestedEnd.Clamp(context.validTrimRange);
            DiscreteTime trimDelta = effectiveEnd - context.initialRange.end;

            toTrim.TrimRangeEnd(itr, trimDelta);
        }

        static Item GetAdjacentClip(Location location, Item item, bool withTransitionsOnly = true)
        {
            Item overrideItem = Item.Invalid;
            switch (location)
            {
                case Location.Start:
                    Item previous = item.Previous();
                    if (withTransitionsOnly)
                    {
                        if (previous.isTransition)
                            overrideItem = previous.Previous();
                    }
                    else if (previous.isClip)
                        return previous;

                    break;
                case Location.End:
                    var next = item.Next();
                    if (withTransitionsOnly)
                    {
                        if (next.isTransition)
                            overrideItem = next.Next();
                    }
                    else if (next.isClip)
                        return next;

                    break;
            }

            return overrideItem.type != Item.Type.Clip ? Item.Invalid : overrideItem;
        }
    }
}
