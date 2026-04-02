// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    static class EditorReplaceExtensions
    {
        public static void InsertReplace(
            this CutList.Editor editor, CutList toInsert, IContentHandler contentHandler = null)
        {
            var replaceRange = TimeRange.Empty;
            foreach (CutList.Item itemToInsert in toInsert)
            {
                if (!itemToInsert.isClip)
                    continue;

                if (itemToInsert.trimmedStart >= editor.duration)
                    continue;

                if (replaceRange == TimeRange.Empty)
                    replaceRange = itemToInsert.visibleRange;
                else
                    replaceRange = replaceRange.Union(itemToInsert.visibleRange);
            }

            editor.ReplaceRangeWithGap(replaceRange, contentHandler);
            editor.RemoveZeroDurationItems();
            editor.InsertMix(toInsert, contentHandler);
        }

        public static void ReplaceRangeWithGap(this CutList.Editor editor, TimeRange gapRange, IContentHandler contentHandler = null)
        {
            if (gapRange == TimeRange.Empty) return;
            gapRange = gapRange.Clamp(DiscreteTime.Zero, DiscreteTime.MaxValue);

            CutList.Iterator leftItr = editor.IteratorAtTime(gapRange.start, true);
            CutList.Iterator rightItr = editor.IteratorAtTime(gapRange.end, true);

            if (leftItr == editor.GetIteratorAtEnd()) //insert at the end
            {
                CutList.GapBuilder gap = new CutList.GapBuilder().WithDuration(gapRange.end - editor.duration);
                editor.Insert(editor.Count, gap);
                return;
            }

            if (rightItr == editor.GetIteratorAtEnd())
                rightItr = rightItr.Previous();

            CutList.GapBuilder toInsert = new CutList.GapBuilder().WithDuration(gapRange.duration);
            editor.InsertReplaceItem(leftItr, rightItr, gapRange.start, toInsert, contentHandler);
        }

        static void InsertReplaceItem(this CutList.Editor editor, CutList.Iterator leftItr,
            CutList.Iterator rightItr, DiscreteTime at, CutList.ItemBuilder toInsert, IContentHandler contentHandler)
        {
            CutList.Item leftItem = leftItr.Current;
            CutList.Item rightItem = rightItr.Current;

            var insertionRange = new TimeRange(at, at + toInsert.duration);
            RemoveTransitionIfOverlaps(editor, leftItr, insertionRange);
            RemoveTransitionIfOverlaps(editor, rightItr, insertionRange);

            if (leftItem == rightItem) //split
            {
                Internals.CutListInsertExtensions.InsertInto_Internal(editor, leftItr, at, toInsert, contentHandler);
                return;
            }

            TimeRange leftOverlap = leftItem.trimmedRange.OverlapWith(insertionRange);
            editor.ChangeDuration(leftItr, leftItem.trimmedDuration - leftOverlap.duration);

            TimeRange rightOverlap = rightItem.trimmedRange.OverlapWith(insertionRange);
            Internals.CutListInsertExtensions.TrimStart_Internal(
                editor, rightItr, rightItem.trimmedDuration - rightOverlap.duration);

            editor.RemoveItemsBetween(leftItr, rightItr);
            editor.Insert(leftItr + 1, toInsert);
            editor.RemoveZeroDurationGaps();
        }

        static void RemoveTransitionIfOverlaps(CutList.Editor editor, CutList.Iterator itr, TimeRange range)
        {
            CutList.Item item = itr.Current;
            TimeRange leftTransitionRange = item.GetLeftTransition().range;
            if (leftTransitionRange.Overlaps(range))
            {
                editor.RemoveEndTransition(itr - 1);
                editor.IncreaseClipIn(itr, leftTransitionRange.duration.Half());
            }

            TimeRange rightTransitionRange = item.GetRightTransition().range;
            if (rightTransitionRange.Overlaps(range))
            {
                editor.RemoveEndTransition(itr);
                editor.IncreaseClipIn(itr.Next(), rightTransitionRange.duration.Half());
            }
        }

        static void IncreaseClipIn(this CutList.Editor editor, CutList.Iterator itr, DiscreteTime increase)
        {
            CutList.Item item = itr.Current;
            DiscreteTime newClipIn = item.contentStart + increase;
            var newRange = new TimeRange(newClipIn, newClipIn + item.trimmedDuration);
            editor.ChangeRange(itr, newRange);
        }
    }
}
