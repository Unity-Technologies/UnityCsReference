// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    static class EditorMixExtensions
    {
        public static bool ValidateInsertMix(this CutList cutList, IEnumerable<CutList.Item> toInsert, IContentHandler contentHandler = null)
        {
            foreach (CutList.Item toInsertItem in toInsert)
            {
                if (!toInsertItem.isClip)
                    continue;

                foreach (CutList.Item destinationItem in cutList)
                {
                    if (destinationItem.visibleStart >= toInsertItem.visibleEnd)
                        break;

                    if (!destinationItem.isClip)
                        continue;

                    if (!ValidateClipInsertion(toInsertItem, destinationItem, contentHandler))
                        return false;
                }
            }
            return true;
        }

        static bool ValidateClipInsertion(CutList.Item toInsertItem, CutList.Item destinationItem, IContentHandler contentHandler)
        {
            TimeRange toInsertRange = toInsertItem.trimmedRange;
            TimeRange destinationRange = destinationItem.visibleRange;

            //cannot completely overlap clip
            if (toInsertRange.CompletelyOverlapsStrict(destinationRange) || destinationRange.CompletelyOverlapsStrict(toInsertRange))
                return false;

            //insert transition cannot overlap destination item
            CutList.Transition toInsertTr = toInsertItem.GetRightTransition();
            if (toInsertTr.isValid && toInsertTr.range.Overlaps(destinationRange))
                return false;

            //destination transition cannot overlap insertion item
            CutList.Transition destinationTr = destinationItem.GetRightTransition();
            if (destinationTr.isValid && destinationTr.range.Overlaps(toInsertItem.visibleRange))
                return false;

            //if items overlap, check if can blend
            if (destinationRange.Overlaps(toInsertRange) && !contentHandler.CanBlendSafe(destinationItem.content, toInsertItem.content))
                return false;

            return true;
        }

        public static void InsertMix(this CutList.Editor editor, CutList toInsert, IContentHandler contentHandler = null)
        {
            foreach (CutList.Item itemToInsert in toInsert)
            {
                if (!itemToInsert.isClip)
                    continue;

                DiscreteTime visibleStart = itemToInsert.visibleStart;
                CutList.ItemBuilder itemBuilder = ConvertInsertItem(itemToInsert);
                if (visibleStart >= editor.duration) //insert at end
                {
                    DiscreteTime gapDuration = visibleStart - editor.duration;
                    if (gapDuration > DiscreteTime.Zero)
                        editor.Insert(editor.Count, CutList.ItemBuilder.Gap().WithDuration(gapDuration));
                    editor.Insert(editor.Count, itemBuilder);
                    continue;
                }

                InsertMixItem(editor, visibleStart, itemBuilder, contentHandler);
            }
        }

        public static DiscreteTime CalculateMinimumTrimStartTime(CutList.Iterator item, IContentHandler contentHandler = null)
        {
            if (!item.IsValid())
                throw new ArgumentException(nameof(item));

            CutList.Iterator previousClipItr = item.PreviousClip();
            if (previousClipItr.IsValid())
            {
                //check if we can blend with the previous clip
                CutList.Item previousClip = previousClipItr.Current;
                bool canBlend = contentHandler.CanBlendSafe(item.Current.content, previousClip.content);
                return canBlend ? previousClip.noOverlapStart : previousClip.visibleEnd;
            }

            return DiscreteTime.Zero;
        }

        public static DiscreteTime CalculateMaximumTrimEndTime(CutList.Iterator item, IContentHandler contentHandler = null)
        {
            if (!item.IsValid())
                throw new ArgumentException(nameof(item));

            CutList.Iterator nextClipItr = item.NextClip();
            if (nextClipItr.IsValid())
            {
                //check if we can blend with the next clip
                CutList.Item nextClip = nextClipItr.Current;
                bool canBlend = contentHandler.CanBlendSafe(item.Current.content, nextClip.content);
                return canBlend ? nextClip.noOverlapEnd : nextClip.visibleStart;
            }

            return DiscreteTime.MaxValue;
        }

        static CutList.ItemBuilder ConvertInsertItem(CutList.Item item)
        {
            var itemBuilder = (CutList.ItemBuilder)item;
            return itemBuilder
                .WithEndTransition(UniqueID.Invalid, DiscreteTime.Zero)
                .WithDuration(item.visibleDuration);
        }

        static void InsertMixItem(this CutList.Editor editor,
            DiscreteTime atTime, CutList.ItemBuilder toInsert, IContentHandler contentHandler)
        {
            var insertionRange = new TimeRange(atTime, atTime + toInsert.duration);

            CutList.Iterator leftItr = editor.IteratorAtTime(insertionRange.start);
            CutList.Iterator rightItr = editor.IteratorAtTime(insertionRange.end, true);

            if (rightItr == editor.GetIteratorAtEnd())
                rightItr = rightItr.Previous();

            CutList.Item leftItem = leftItr.Current;
            CutList.Item rightItem = rightItr.Current;
            TimeRange leftOverlap = leftItem.trimmedRange.OverlapWith(insertionRange);
            TimeRange rightOverlap = rightItem.trimmedRange.OverlapWith(insertionRange);

            if (leftItr == rightItr)
            {
                if (leftItem.isGap) //split gap
                {
                    CutListInsertExtensions.InsertInto_Internal(editor, leftItr, atTime, toInsert);
                    return;
                }
                //edge case where the destination item is completely overlapped by the inserted item
                rightOverlap = TimeRange.Empty;
            }

            if (rightItr > leftItr)
                editor.RemoveItemsBetween(leftItr, rightItr);

            //if the inserted item is smaller than the existing item, insert it before
            if (insertionRange.start >= leftItem.trimmedRange.start && leftItem.trimmedRange.end > insertionRange.end)
                editor.Insert(leftItr, toInsert);
            else
                editor.Insert(leftItr + 1, toInsert);

            BlendWithNext(editor, leftItr, leftOverlap.duration, contentHandler);
            BlendWithNext(editor, leftItr.Next(), rightOverlap.duration, contentHandler);
            editor.RemoveZeroDurationGaps();
        }

        static void BlendWithNext(this CutList.Editor editor,
            CutList.Iterator itr, DiscreteTime overlap, IContentHandler contentHandler)
        {
            if (overlap <= DiscreteTime.Zero)
                return;

            CutList.Item leftItem = itr.Current;
            CutList.Item rightItem = itr.Next().Current;

            if (leftItem.isClip && rightItem.isClip) //blend
            {
                var (splitBefore, splitAfter) = overlap.Split();
                DiscreteTime leftDuration = leftItem.trimmedDuration - splitBefore;
                DiscreteTime rightDuration = rightItem.trimmedDuration - splitAfter;

                BlendResult blendResult = contentHandler.BlendSafe(leftItem.content, rightItem.content);

                var builder = (CutList.ItemBuilder)itr.Current;
                builder = builder.WithDuration(leftDuration);
                builder = builder.WithEndTransition(blendResult.id, overlap, blendResult.transitionContent);

                editor.Replace(itr, builder);
                editor.ChangeDuration(itr + 1, rightDuration);
            }
            else if (leftItem.isGap) //trim left
            {
                DiscreteTime leftDuration = leftItem.trimmedDuration - overlap;
                editor.ChangeDuration(itr, leftDuration);
            }
            else if (rightItem.isGap) //trim right
            {
                DiscreteTime rightDuration = rightItem.trimmedDuration - overlap;
                editor.ChangeDuration(itr.Next(), rightDuration);
            }
        }
    }
}
