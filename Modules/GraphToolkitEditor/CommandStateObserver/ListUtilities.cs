// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolkit.CSO
{
    static class ListUtilities
    {
        /// <summary>
        /// Add items from <paramref name="source"/> to <paramref name="dest"/>, only if it is not already present.
        /// </summary>
        /// <param name="dest">The destination list.</param>
        /// <param name="source">The source list.</param>
        /// <typeparam name="T">Type of the list elements.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="dest"/> or <paramref name="source"/> is null.</exception>
        /// <remarks>This method is intended to be used with small <paramref name="source"/> (max. 100 elements).</remarks>
        public static void AddFewDistinct<T>(this List<T> dest, IList<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            for (var index = 0; index < source.Count; index++)
            {
                var element = source[index];
                if (!dest.Contains(element))
                {
                    dest.Add(element);
                }
            }
        }

        /// <summary>
        /// Checks if the IReadOnlyList <paramref name="source"/> contains <paramref name="value"/>.
        /// </summary>
        /// <param name="source">The container to check.</param>
        /// <param name="value">The value to find.</param>
        /// <typeparam name="T">The type of values.</typeparam>
        /// <returns>True if <paramref name="source"/> contains <paramref name="value"/>, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
        public static bool Contains<T>(this IReadOnlyList<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ICollection<T> collection)
            {
                return collection.Contains(value);
            }

            var comp = EqualityComparer<T>.Default;
            for (var index = 0; index < source.Count; index++)
            {
                var element = source[index];
                if (comp.Equals(element, value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the IReadOnlyCollection <paramref name="source"/> contains <paramref name="value"/>.
        /// </summary>
        /// <param name="source">The container to check.</param>
        /// <param name="value">The value to find.</param>
        /// <typeparam name="T">The type of values.</typeparam>
        /// <returns>True if <paramref name="source"/> contains <paramref name="value"/>, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null.</exception>
        public static bool Contains<T>(this IReadOnlyCollection<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ICollection<T> collection)
            {
                return collection.Contains(value);
            }

            var comp = EqualityComparer<T>.Default;
            foreach (var element in source)
            {
                if (comp.Equals(element, value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if at least one element from the IReadOnlyList <paramref name="source"/> matches the predicate <paramref name="predicate"/>.
        /// </summary>
        /// <param name="source">The list to check.</param>
        /// <param name="predicate">The predicate to use.</param>
        /// <typeparam name="T">The type of values.</typeparam>
        /// <returns>True if <paramref name="source"/> contains at least one element that matches <paramref name="predicate"/>, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="predicate"/> is null.</exception>
        public static bool Any<T>(this IReadOnlyList<T> source, Func<T, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            for (var index = 0; index < source.Count; index++)
            {
                var element = source[index];
                if (predicate(element))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes an element from a list by swapping it with the last element and removing the last element.
        /// </summary>
        /// <param name="self">The list to remove the element from.</param>
        /// <param name="element">The element to remove.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        public static void SwapRemove<T>(this List<T> self, T element)
        {
            var i = self.IndexOf(element);
            SwapRemoveAt(self, i);
        }

        /// <summary>
        /// Removes an element from a list by swapping it with the last element and removing the last element.
        /// </summary>
        /// <param name="self">The list to remove the element from.</param>
        /// <param name="elementIndex">The index of the element to remove.</param>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        public static void SwapRemoveAt<T>(this List<T> self, int elementIndex)
        {
            if (elementIndex < self.Count - 1)
            {
                self[elementIndex] = self[^1];
            }
            self.RemoveAt(self.Count - 1);
        }
    }
}
