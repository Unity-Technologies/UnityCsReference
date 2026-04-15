// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements;

/// <summary>
/// Contains common operations for associating keys and values into a sorted list
/// This allows the same key to associate with multiple values without allocating lots of small lists into a Dictionary
/// We usually expect to deal with small data sets (i.e. less than a 1000 entries) where O(log N) operations
/// wouldn't be meaningfully slower than O(1) operations
/// </summary>
static class SortedPairListExtensions
{
    /// <summary>
    /// Returns at which index values for a specific start (if not found, length is 0)
    /// </summary>
    public static (int start, int length) FindRangeForKey<TKey, TValue>(this List<(TKey key, TValue value)> list, IComparer<(TKey, TValue)> comparer, TKey key, TValue defaultValue)
        where TKey : IComparable<TKey>
    {
        // Searching for the default values gives a random index in the range where for this key
        int index = list.BinarySearch((key, defaultValue), comparer);

        // If the default value doesn't exist, we can retrieve the index to where it should be inserted
        if (index < 0)
            index = ~index;

        // From this random index, we can walk back to the first entry with that key
        int firstIndex = index;
        while (firstIndex > 0 && list[firstIndex - 1].key.CompareTo(key) == 0)
            firstIndex--;

        // Now count again in the other direction to find the range length
        int length = 0;
        for (int i = firstIndex; i < list.Count && list[i].key.CompareTo(key) == 0; i++)
        {
            length++;
        }

        return (firstIndex, length);
    }

    /// <summary>
    /// Removes all values for a given key, if the key exists.
    /// </summary>
    public static void RemoveRangeForKey<TKey, TValue>(this List<(TKey key, TValue value)> list, IComparer<(TKey, TValue)> comparer, TKey key, TValue defaultValue)
        where TKey : IComparable<TKey>
    {
        (int firstIndex, int removeCount) = list.FindRangeForKey(comparer, key, defaultValue);
        if (firstIndex >= 0 && removeCount > 0)
            list.RemoveRange(firstIndex, removeCount);
    }

    /// <summary>
    /// Performs a search for where to insert a key/value pair, and then performs the insertion if the pair doesn't already exist
    /// </summary>
    public static bool InsertUniquePair<TKey, TValue>(this List<(TKey key, TValue value)> list, IComparer<(TKey, TValue)> comparer, TKey key, TValue value)
    {
        var keyValue = (key, value);
        int index = list.BinarySearch(keyValue, comparer);
        if (index < 0)
        {
            index = ~index;
            list.Insert(index, keyValue);
            return true;
        }

        return false;
    }
}
