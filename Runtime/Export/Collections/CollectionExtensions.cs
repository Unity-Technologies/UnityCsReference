// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine.Bindings;

namespace Unity.Collections
{
    [VisibleToOtherModules]
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Resizes the array and adds the item to the end.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="item">The item to add to the array.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void AddToArray<T>(ref T[] array, T item)
        {
            Array.Resize(ref array, array.Length + 1);
            array[^1] = item;
        }

        /// <summary>
        /// Resizes the array and inserts the item to the provided index.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="index">The insert at which we add the item.</param>
        /// <param name="item">The item to add to the array.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void InsertIntoArray<T>(ref T[] array, int index, T item)
        {
            if (index < 0 || index > array.Length)
                throw new IndexOutOfRangeException("Trying to insert into an array out of bounds.");

            var current = array;
            array = new T[current.Length + 1];
            Array.Copy(current, array, index);
            Array.Copy(current, index, array, index + 1, current.Length - index);
            array[index] = item;
        }

        /// <summary>
        /// Removes the item from the array and resizes it.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="item">The item to remove from the array.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal static bool RemoveFromArray<T>(ref T[] array, T item)
        {
            var removeIndex = Array.IndexOf(array, item);
            if (removeIndex == -1)
                return false;

            RemoveFromArray(ref array, removeIndex);
            return true;
        }

        /// <summary>
        /// Removes the item from the array and resizes it.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="index">The index to remove from the array.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal static void RemoveFromArray<T>(ref T[] array, int index)
        {
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            for (int i = 0, j = 0; i < array.Length; i++)
            {
                if (i != index)
                    array[j++] = array[i];
            }
            Array.Resize(ref array, array.Length - 1);
        }

        /// <summary>
        /// Gets the minimum element from the list.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="list">The list.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal static T Min<T>([DisallowNull]this IList<T> list, IComparer<T> comparer = null)
        {
            if (list.Count == 0)
                throw new InvalidOperationException("list contains no elements");

            T min = list[0];
            comparer ??= Comparer<T>.Default;

            for (int i = 1; i < list.Count; i++)
            {
                if (comparer.Compare(list[i], min) < 0)
                    min = list[i];
            }
            return min;
        }
        /// <summary>
        /// Gets the minimum element from the list.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="list">The list.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal static T Max<T>([DisallowNull]this IList<T> list, IComparer<T> comparer = null)
        {
            if (list.Count == 0)
                throw new InvalidOperationException("list contains no elements");

            T max = list[0];
            comparer ??= Comparer<T>.Default;

            for (int i = 1; i < list.Count; i++)
            {
                if (comparer.Compare(list[i], max) > 0)
                    max = list[i];
            }
            return max;
        }

        /// <summary>
        /// Add element to the correct position in presorted List. This methods reduce a need to call Sort() on the List.
        /// </summary>
        /// <param name="list">Presorted List</param>
        /// <param name="item">Element to add</param>
        /// <param name="comparer">Comparator if Comparer<T>.Default is not suite</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentNullException">Can throw exception if list is null</exception>
        internal static void AddSorted<T>([DisallowNull] this List<T> list, T item, IComparer<T> comparer = null)
        {
            if (list == null)
                throw new ArgumentNullException($"{nameof(list)} must not be null.");

            comparer ??= Comparer<T>.Default;

            //No elements in the list yet
            if (list.Count == 0)
            {
                list.Add(item);
                return;
            }

            if (comparer.Compare(list[^1], item) <= 0)
            {
                list.Add(item);
                return;
            }

            if (comparer.Compare(list[0], item) >= 0)
            {
                list.Insert(0, item);
                return;
            }

            var index = list.BinarySearch(item, comparer);
            if (index < 0)
                index = ~index;
            list.Insert(index, item);
        }

        /// <summary>
        /// Adds <paramref name="count"/> elements <paramref name="value"/> to the list.
        /// </summary>
        /// <param name="dest">The list to modify.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="count">The number of values to add.</param>
        /// <typeparam name="T">The type of value.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dest"/> is null.</exception>
        public static void Fill<T>([DisallowNull] this List<T> dest, T value, int count)
        {
            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            dest.Capacity = Math.Max(dest.Capacity, dest.Count + count);
            while (count-- > 0)
            {
                dest.Add(value);
            }
        }

        /// <summary>
        /// Returns the first element of collection sorted by comparer. This method reduce a need to call Sort() and FirstOrDefault() on the List.
        /// </summary>
        /// <param name="collection">Collection to inspect</param>
        /// <param name="comparer">Comparator if Comparer<T>.Default is not suite</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The first element if the list was sorted or default</returns>
        /// <exception cref="ArgumentNullException">Can throw exception if list is null</exception>
        internal static T FirstOrDefaultSorted<T>(this IEnumerable<T> collection, IComparer<T> comparer = null)
        {
            if (collection == null)
                throw new ArgumentNullException($"{nameof(collection)} must not be null.");

            comparer ??= Comparer<T>.Default;

            var firstAssignment = false;
            T element = default;
            foreach (var e in collection)
            {
                if (!firstAssignment)
                {
                    element = e;
                    firstAssignment = true;
                }

                if(comparer.Compare(e, element) < 0)
                    element = e;
            }

            return element;
        }

        /// <summary>
        /// Returns collections as a string. This can be useful for debug collections in Debug.Log.
        /// String is also Json compatible. It use [ , , ] convention.
        /// </summary>
        /// <param name="collection">Serializable collection</param>
        /// <param name="serializeElement">Function to serialize element of collection</param>
        /// <typeparam name="T">Collection type</typeparam>
        /// <returns>Serialized collection</returns>
        /// <exception cref="ArgumentNullException">Can produce exception if collection or serialize method is null</exception>
        internal static string SerializedView<T>([DisallowNull] this IEnumerable<T> collection, [DisallowNull] Func<T, string> serializeElement)
        {
            if (collection == null)
                throw new ArgumentNullException($"{nameof(collection)} must not be null.");

            if (serializeElement == null)
                throw new ArgumentNullException($"Argument {nameof(serializeElement)} must not be null.");

            var builder = new StringBuilder();
            builder.Append("[");
            foreach (var item in collection)
            {
                if (item == null)
                    builder.Append("null");
                else
                    builder.Append(serializeElement.Invoke(item));
                builder.Append(',');
            }

            // remove trailing comma if we added any items
            if (builder.Length > 1)
                builder.Length--;

            builder.Append("]");
            return builder.ToString();
        }

        /// <summary>
        /// Check if element is in collection. This method replace Linq implementation to reduce GC allocations.
        /// </summary>
        /// <param name="collection">Collection to inspect</param>
        /// <param name="element">Element to find</param>
        /// <typeparam name="T">Collection type</typeparam>
        /// <returns>True if element found and False if not</returns>
        /// <exception cref="ArgumentNullException">Can produce exception if collection is null</exception>
        internal static bool ContainsByEquals<T>([DisallowNull] this IEnumerable<T> collection, T element)
        {
            if (collection == null)
                throw new ArgumentNullException($"{nameof(collection)} must not be null.");

            var comparer = EqualityComparer<T>.Default;
            foreach (var e in collection)
            {
                if (comparer.Equals(e, element))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether the array contains the specified item.
        /// </summary>
        /// <typeparam name="T">The item type.</typeparam>
        /// <param name="array">The array.</param>
        /// <param name="item">The item to locate in the array.</param>
        [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIBuilderModule")]
        internal static bool Contains<T>([DisallowNull]this T[] array, T item) => Array.IndexOf(array, item) != -1;
    }
}
