// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    static class EditorRippleExtensions
    {
        public static void RippleMove(this CutList.Editor editor, CutList.Iterator iterator, DiscreteTime delta)
        {
            if (!iterator.IsValid())
                return;

            if (iterator.Current.isGap) //change gap
            {
                DiscreteTime newDuration = DiscreteTimeTimeExtensions.Max(DiscreteTime.Zero, iterator.Current.trimmedDuration + delta);
                editor.ChangeDuration(iterator, newDuration);
                return;
            }

            if (delta < DiscreteTime.Zero)
                RippleTowardsLeft(editor, iterator, delta);
            else
                RippleTowardsRight(editor, iterator, delta);
        }

        public static void RippleInsert(this CutList.Editor destination, CutList insertionSource, IContentHandler handler = null)
        {
            CutList.Iterator firstSourceClipItr = insertionSource.FirstClip();

            if (firstSourceClipItr == insertionSource.GetIteratorAtEnd()) // nothing to insert
                return;

            CutList.Item firstSourceClip = firstSourceClipItr.Current;
            var insertionRange = new TimeRange(firstSourceClip.visibleRange.start, insertionSource.duration);

            CutList.Iterator destinationItem = destination.IteratorAtTime(insertionRange.start);

            if (destinationItem != destination.GetIteratorAtEnd()) //ripple destination items
            {
                DiscreteTime rippleDuration = insertionRange.duration;
                if (destinationItem.Current.isClip)
                    rippleDuration += insertionRange.start - destinationItem.Current.visibleStart;
                else
                    rippleDuration -= destinationItem.Current.visibleEnd - insertionRange.start;

                if (rippleDuration > DiscreteTime.Zero)
                    destination.RippleMove(destinationItem, rippleDuration);
            }

            destination.InsertReplace(insertionSource, handler);
        }

        static void RippleTowardsLeft(CutList.Editor editor, CutList.Iterator iterator, DiscreteTime delta)
        {
            CutList.Iterator previousItr = iterator.Previous();
            CutList.Item previousItem = iterator.Current.GetPrevious();

            if (previousItem.type == CutList.ItemType.Gap) //if previous item is a gap, trim it
            {
                DiscreteTime desiredDuration = previousItem.trimmedDuration + delta;
                DiscreteTime newDuration = DiscreteTimeTimeExtensions.Max(DiscreteTime.Zero, desiredDuration);
                editor.ChangeDuration(previousItr, newDuration);
            }
        }

        static void RippleTowardsRight(CutList.Editor editor, CutList.Iterator iterator, DiscreteTime delta)
        {
            CutList.Iterator previousItr = iterator.Previous();
            CutList.Item previousItem = iterator.Current.GetPrevious();

            if (previousItem.type == CutList.ItemType.Gap) //if previous item is a gap, extend it
            {
                DiscreteTime newDuration = previousItem.trimmedDuration + delta;
                editor.ChangeDuration(previousItr, newDuration);
            }
            else
            {
                if (previousItem.hasRightTransition)
                {
                    DiscreteTime rightTransition = previousItem.GetRightTransition().duration;
                    DiscreteTime newRightTransition = DiscreteTimeTimeExtensions.Max(DiscreteTime.Zero, rightTransition - delta);
                    delta -= ChangeClipsBlend(editor, iterator, newRightTransition);
                }
                if (delta > DiscreteTime.Zero)
                {
                    CutList.ItemBuilder gap = CutList.ItemBuilder.Gap().WithDuration(delta);
                    editor.Insert(iterator, gap);
                }
            }
        }

        static DiscreteTime ChangeClipsBlend(CutList.Editor editor, CutList.Iterator iterator, DiscreteTime newBlendDuration)
        {
            CutList.Iterator previousItr = iterator.Previous();
            CutList.Item previousItem = iterator.Current.GetPrevious();

            DiscreteTime diff = previousItem.GetRightTransition().duration - newBlendDuration;
            if (newBlendDuration <= DiscreteTime.Zero)
            {
                editor.RemoveEndTransition(previousItr);
                diff = previousItem.GetRightTransition().duration;
            }
            else
                editor.ChangeEndTransitionDuration(previousItr, newBlendDuration);

            editor.ChangeDuration(iterator, iterator.Current.trimmedDuration + diff.Half());
            editor.ChangeDuration(previousItr, previousItem.trimmedDuration + diff.Half());
            return diff;
        }

        static CutList.Iterator FirstClip(this CutList cutList)
        {
            CutList.Iterator itr = cutList.GetIteratorAtStart();
            while (itr != cutList.GetIteratorAtEnd())
                return itr.Current.isClip ? itr : itr.Next();

            return itr;
        }
    }
}
