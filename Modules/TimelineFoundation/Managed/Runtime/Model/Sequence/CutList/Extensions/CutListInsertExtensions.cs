// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model.Internals
{
    static class CutListInsertExtensions
    {
        public static void InsertInto_Internal(CutList.Editor editor, CutList.Iterator itr, DiscreteTime atTime,
            CutList.ItemBuilder toInsert, IContentHandler contentHandler = null)
        {
            CutList.Item item = itr.Current;
            TimeRange itemRange = item.trimmedRange;
            var toInsertRange = new TimeRange(atTime, atTime + toInsert.duration);
            DiscreteTime leftDuration = toInsertRange.start - itemRange.start;
            DiscreteTime rightDuration = itemRange.end - toInsertRange.end;

            if (toInsertRange.CompletelyOverlaps(itemRange)) //complete overlap
            {
                editor.Replace(itr, toInsert);
            }
            else if (leftDuration <= DiscreteTime.Zero) //trim at the right
            {
                editor.Insert(itr, toInsert);
                TrimStart_Internal(editor, itr.Next(), rightDuration);
            }
            else if (rightDuration <= DiscreteTime.Zero) //trim at the left
            {
                editor.ChangeDuration(itr, leftDuration);
                editor.Insert(itr + 1, toInsert);
            }
            else
            {
                editor.Split(itr, leftDuration, rightDuration, contentHandler);
                editor.Insert(itr + 1, toInsert);
            }
        }

        public static void TrimStart_Internal(CutList.Editor editor, CutList.Iterator itr, DiscreteTime newDuration)
        {
            CutList.Item item = itr.Current;
            DiscreteTime clipIn = item.trimmedDuration - newDuration;

            if (newDuration == DiscreteTime.Zero)
                editor.Remove(itr);
            else if (clipIn > DiscreteTime.Zero && item.isClip) //cannot have negative clip in
                editor.ChangeRange(itr, new TimeRange(clipIn, clipIn + newDuration));
            else
                editor.ChangeDuration(itr, newDuration);
        }
    }
}
