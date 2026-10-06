// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Collections
{
    /// <summary>
    /// Extension methods for lists.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Finds and removes the first occurrence of a particular value in the list.
        /// </summary>
        /// <remarks>
        /// If found, the first occurrence of the value is overwritten by the last element of the list, and the list's length is decremented by one.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate and remove.</param>
        /// <returns>Returns true if an element was removed.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, T value)
        {
            int index = list.IndexOf(value);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Finds and removes the first value which satisfies a predicate.
        /// </summary>
        /// <remarks>
        /// The first value satisfying the predicate is overwritten by the last element of the list, and the list's length is decremented by one.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="matcher">The predicate for testing the elements of the list.</param>
        /// <returns>Returns true if an element was removed.</returns>
        public static bool RemoveSwapBack<T>(this List<T> list, Predicate<T> matcher)
        {
            int index = list.FindIndex(matcher);
            if (index < 0)
                return false;

            RemoveAtSwapBack(list, index);
            return true;
        }

        /// <summary>
        /// Removes the value at an index.
        /// </summary>
        /// <remarks>
        /// The value at the index is overwritten by the last element of the list, and the list's length is decremented by one.
        /// </remarks>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="index">The index at which to remove an element from the list.</param>
        public static void RemoveAtSwapBack<T>(this List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            list[index] = list[lastIndex];
            list.RemoveAt(lastIndex);
        }

        /// <summary>
        /// Returns a copy of this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to copy.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A copy of this list.</returns>
        public static NativeList<T> ToNativeList<T>(this List<T> list, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
        {
            var container = new NativeList<T>(list.Count, allocator);
            for (int i = 0; i < list.Count; i++)
            {
                container.AddNoResize(list[i]);
            }
            return container;
        }

        /// <summary>
        /// Returns an array that is a copy of this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to copy.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array that is a copy of this list.</returns>
        public unsafe static NativeArray<T> ToNativeArray<T>(this List<T> list, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
        {
            var container = CollectionHelper.CreateNativeArray<T>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < list.Count; i++)
            {
                container[i] = list[i];
            }
            return container;
        }

        internal static List<T> CreateWithDefaultValue<T>(T value, int count)
        {
            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
                result.Add(value);
            return result;
        }
    }
}
