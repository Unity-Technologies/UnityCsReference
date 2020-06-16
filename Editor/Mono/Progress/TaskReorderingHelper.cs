// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class TaskReorderingHelper
    {
        private Action<int, int> removeAtInsertAt;
        private IComparer<Progress.Item> comparer = new ProgressOrderComparer();

        private List<Progress.Item> itemsUpdated = new List<Progress.Item>();
        private List<int> itemsUpdatedIndex = new List<int>();

        private int lastReorderedItemsIndex;
        private int nbReorder;

        public TaskReorderingHelper(Action<int, int> removeAtInsertAt)
        {
            this.removeAtInsertAt = removeAtInsertAt;
        }

        internal void Clear()
        {
            itemsUpdated.Clear();
            itemsUpdatedIndex.Clear();
        }

        internal void AddItemToReorder(Progress.Item op, int i)
        {
            itemsUpdated.Add(op);
            itemsUpdatedIndex.Add(i);
        }

        internal void ReorderItems(int nbItems, Func<int, Progress.Item> getElementAt)
        {
            nbReorder = 0;
            lastReorderedItemsIndex = -1;
            for (int i = 0; i < itemsUpdated.Count; i++)
            {
                if (nbReorder > itemsUpdated.Count * 5) // To prevent an infinite loop in case of a bug, we exit when > 5 * length (did not find cases with more than 1.75 * length when testing so there is a little margin)
                    break;
                if (i == lastReorderedItemsIndex) // if that item was the last one reordered we don't need to try to reorder it again
                    continue;
                var op = itemsUpdated[i];
                var index = itemsUpdatedIndex[i];
                if (NeedsReordering(op, index, nbItems, getElementAt))
                {
                    int insertIndex = FindIndexToInsertAt(op, nbItems, getElementAt);
                    removeAtInsertAt(insertIndex, index);

                    UpdateIndexes(index, insertIndex); // We need to update the index of the updated items because an item has just been reordered

                    // in that case we need to go through the list of updated items again because the elements were not correctly ordered so there might be some error done
                    // (when testing if we need to reorder the item we only look before and after but those can be in the updated items too)
                    lastReorderedItemsIndex = i; // we keep the one we just updated so that we don't update it again which would useless most of the time (if not useless then it means other updated items need reordering which would trigger this item's reordering)
                    i = -1;
                    nbReorder++;
                }
            }
        }

        private void UpdateIndexes(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex < destinationIndex)
                --destinationIndex;
            for (int i = 0; i < itemsUpdatedIndex.Count; ++i)
            {
                if (itemsUpdatedIndex[i] == sourceIndex)
                    itemsUpdatedIndex[i] = destinationIndex;
                else if (itemsUpdatedIndex[i] > sourceIndex && itemsUpdatedIndex[i] <= destinationIndex)
                    itemsUpdatedIndex[i]--;
                else if (itemsUpdatedIndex[i] < sourceIndex && itemsUpdatedIndex[i] >= destinationIndex)
                    itemsUpdatedIndex[i]++;
            }
        }

        private bool NeedsReordering(Progress.Item op, int elementIndex, int nbItems, Func<int, Progress.Item> getElementAt)
        {
            bool previousElementWellPlaced = true;
            bool nextElementWellPlaced = true;
            if (elementIndex > 0)
                previousElementWellPlaced = comparer.Compare(getElementAt(elementIndex - 1), op) != -1; // if the previous item should not be before the current item then it needs reordering
            if (elementIndex < nbItems - 1)
                nextElementWellPlaced = comparer.Compare(op, getElementAt(elementIndex + 1)) != -1; // if the current item should not be before the next item then it needs reordering
            return !(previousElementWellPlaced && nextElementWellPlaced); // if one is not well placed then it needs reordering
        }

        internal int FindIndexToInsertAt(Progress.Item itemToInsert, int nbItems, Func<int, Progress.Item> getElementAt)
        {
            // We must find the 1st element that should go after the item to insert
            for (int i = 0; i < nbItems; i++)
            {
                if (itemToInsert == getElementAt(i)) // used when reordering a list, we need to ignore it
                    continue;

                if (comparer.Compare(itemToInsert, getElementAt(i)) == 1)
                    return i;
            }
            return nbItems; // This case should only happen for a new item and thus we need to add it at the end of the collection
        }
    }

    internal class ProgressOrderComparer : IComparer<Progress.Item>
    {
        public int Compare(Progress.Item source, Progress.Item compared)
        {
            int compare = CompareStatus(source.status, compared.status);
            if (compare == 0)
            {
                if (source.priority.CompareTo(compared.priority) == 0)
                    compare = source.startTime.CompareTo(compared.startTime);
                else
                    compare = source.priority.CompareTo(compared.priority);
            }

            return compare;
        }

        private static int CompareStatus(Progress.Status statusSource, Progress.Status statusToCompare)
        {
            // Pause and Running have the same priority (we don't want the item to change position when clicking pause/resume)
            if ((statusSource == Progress.Status.Running || statusSource == Progress.Status.Paused) && (statusToCompare != Progress.Status.Running && statusToCompare != Progress.Status.Paused))
                return 1;
            if (statusSource == Progress.Status.Failed && (statusToCompare != Progress.Status.Failed && statusToCompare != Progress.Status.Running && statusToCompare != Progress.Status.Paused))
                return 1;
            if (statusSource == Progress.Status.Canceled && statusToCompare == Progress.Status.Succeeded)
                return 1;
            if (statusSource == statusToCompare
                // Same thing, Pause and Running have the same priority (we don't want the item to change position when clicking pause/resume)
                || (statusSource == Progress.Status.Running || statusSource == Progress.Status.Paused) && (statusToCompare == Progress.Status.Running || statusToCompare == Progress.Status.Paused))
                return 0;
            return -1;
        }
    }
}
