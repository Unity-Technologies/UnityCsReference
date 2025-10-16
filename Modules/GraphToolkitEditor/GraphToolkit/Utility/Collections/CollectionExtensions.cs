// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphToolkit
{
    static class CollectionExtensions
    {
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
        /// Checks if the IReadOnlyLCollection <paramref name="source"/> contains <paramref name="value"/>.
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

        public static bool ContainsReference<T>(this IEnumerable<T> container, T search) where T : class
        {
            foreach (var element in container)
            {
                if (ReferenceEquals(element, search))
                    return true;
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

        public static List<T> OfTypeToList<T, T2>(this IEnumerable<T2> list)
        {
            var result = new List<T>();
            foreach (var element in list)
                if (element is T tElement)
                    result.Add(tElement);

            return result;
        }

        public static IDisposable OfTypeToPooledList<T, T2>(this IEnumerable<T2> original, out List<T> filtered)
        {
            var handle = ListPool<T>.Get(out filtered);
            foreach (var element in original)
                if (element is T tElement)
                    filtered.Add(tElement);

            return handle;
        }

        public static List<T> SelectToList<T, T2>(this IReadOnlyList<T2> list, Func<T2, T> selector)
        {
            var result = new List<T>(list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                result.Add(selector(list[i]));
            }

            return result;
        }

        public static IReadOnlyList<T2> Cast<T1, T2>(this IReadOnlyList<T1> list)
        {
            if (typeof(T1) == typeof(T2))
                return (IReadOnlyList<T2>)list;
            var result = new List<T2>(list.Count);
            foreach (var element in list)
            {
                if (element is T2 t2)
                    result.Add(t2);
                else
                    throw new InvalidCastException($"Cannot cast from type {typeof(T1).FullName} to type {typeof(T2).FullName}");
            }

            return result;
        }

        public static List<KeyValuePair<TKey, TValue>> ToList<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict)
        {
            var list = new List<KeyValuePair<TKey, TValue>>(dict.Count);
            foreach (var kvp in dict)
            {
                list.Add(kvp);
            }

            return list;
        }
    }
}
