// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;

namespace Unity.Timeline.Foundation.Model
{
    static class CutListEditorExtensions
    {
        public static void Split(this CutList.Editor editor,
            CutList.Iterator iterator, DiscreteTime leftDuration, DiscreteTime rightDuration,
            IContentHandler contentHandler = null)
        {
            if (!iterator.IsValid())
                throw new ArgumentNullException(nameof(iterator));
            if (leftDuration <= DiscreteTime.Zero)
                throw new ArgumentOutOfRangeException(nameof(leftDuration));
            if (rightDuration <= DiscreteTime.Zero)
                throw new ArgumentOutOfRangeException(nameof(rightDuration));

            CutList.Item item = iterator.Current;
            editor.ChangeDuration(iterator, leftDuration);
            editor.RemoveEndTransition(iterator);

            if (item.type == CutList.ItemType.Clip)
            {
                IItemContent contentCopy = contentHandler.CloneSafe(item.content);
                DiscreteTime previousClipIn = item.contentStart;
                DiscreteTime clipInDuration = item.trimmedDuration - rightDuration;
                CutList.ItemBuilder clip = CutList.ItemBuilder.Clip()
                    .WithClipIn(clipInDuration + previousClipIn)
                    .WithDuration(rightDuration)
                    .WithContent(contentCopy);
                CutList.Transition rightTransition = item.GetRightTransition();
                if (rightTransition.isValid)
                    clip = clip.WithEndTransition(rightTransition.id, rightTransition.duration, rightTransition.content);
                editor.Insert(iterator.currentIndex + 1, clip);
            }
            else if (item.type == CutList.ItemType.Gap)
            {
                editor.Insert(iterator.currentIndex + 1, CutList.ItemBuilder.Gap().WithDuration(rightDuration));
            }
        }

        /// <summary>
        /// Remove the specified item from the cutList.
        /// This method will extend adjacent items if there is a transition between the two.
        /// </summary>
        /// <param name="editor">The cutList editor to remove an item from.</param>
        /// <param name="iterator">The item to remove.</param>
        /// <param name="insertGap">True if the item should be replaced by a gap.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RemoveItem(this CutList.Editor editor, CutList.Iterator iterator, bool insertGap = true)
        {
            if (!iterator.IsValid())
                throw new ArgumentNullException(nameof(iterator));

            CutList.Item toRemove = iterator.Current;
            DiscreteTime gapDuration = toRemove.noOverlapDuration;
            DiscreteTime leftOverlap = toRemove.GetLeftTransition().GetOverlap_Internal(CutList.Transition.Location.Right);
            DiscreteTime rightOverlap = toRemove.GetRightTransition().GetOverlap_Internal(CutList.Transition.Location.Left);

            //extend previous item and remove transition
            CutList.Iterator previous = iterator.Previous();
            if (previous.IsValid())
            {
                if (leftOverlap > DiscreteTime.Zero)
                {
                    DiscreteTime newDuration = previous.Current.trimmedDuration + leftOverlap;
                    editor.RemoveEndTransition(previous);
                    editor.ChangeDuration(previous, newDuration);
                }
            }

            //extend next item and remove transition
            CutList.Iterator next = iterator.Next();
            if (next.IsValid() && next != editor.GetIteratorAtEnd())
            {
                if (rightOverlap > DiscreteTime.Zero)
                {
                    DiscreteTime newDuration = next.Current.trimmedDuration + rightOverlap;
                    editor.ChangeDuration(next, newDuration);
                }
            }

            if (insertGap)
                editor.Replace(iterator, CutList.ItemBuilder.Gap().WithDuration(gapDuration)); //insert gap
            else
                editor.Remove(iterator);

            editor.MergeContinuousGaps();
            editor.Trim();
        }
    }
}
