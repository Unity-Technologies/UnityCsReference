// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class CollectionExtensions
    {
        public static bool AnyMatches<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            foreach (var item in collection)
                if (condition(item))
                    return true;

            return false;
        }

        public static bool ContainsMatches<T>(this IEnumerable<T> collection, T itemToCheck, IEqualityComparer<T> comparer = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (itemToCheck == null)
                throw new ArgumentNullException(nameof(itemToCheck));

            comparer ??= EqualityComparer<T>.Default;
            foreach (var item in collection)
                if (comparer.Equals(itemToCheck, item))
                    return true;

            return false;
        }

        public static bool AllMatches<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            foreach (var item in collection)
                if (!condition(item))
                    return false;

            return true;
        }

        public static int CountMatches<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            var count = 0;
            foreach (var item in collection)
                if (condition(item))
                    ++count;

            return count;
        }

        public static T FirstMatch<T>(this IEnumerable<T> collection, Func<T, bool> condition) where T : class
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            foreach (var item in collection)
                if (condition(item))
                    return item;

            return null;
        }

        public static T LastMatch<T>(this IList<T> collection, Func<T, bool> condition) where T : class
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            for (var i = collection.Count - 1; i >= 0; i--)
            {
                var item = collection[i];
                if (condition(item))
                    return item;
            }
            return null;
        }

        public static IEnumerable<T> Filter<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            foreach (var item in collection)
                if (condition(item))
                    yield return item;
        }

        public static IEnumerable<TResult> FilterByType<TResult>(this IEnumerable collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
                if (item is TResult result)
                    yield return result;
        }

        public static IEnumerable<T> EnumerateDistinct<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var seen = new HashSet<T>();
            foreach (var item in collection)
                if (seen.Add(item))
                    yield return item;
        }

        public static IEnumerable<TResult> SelectAsEnumerable<T, TResult>(this IEnumerable<T> collection, Func<T, TResult> selector)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            foreach (var item in collection)
                yield return selector(item);
        }

        public static IEnumerable<string> SelectNonEmpty<T>(this IEnumerable<T> collection, Func<T, string> selector)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            foreach (var item in collection)
            {
                var str = selector(item);
                if (!string.IsNullOrEmpty(str))
                    yield return str;
            }
        }

        public static TResult[] SelectToNewArray<T, TResult>(this IReadOnlyCollection<T> collection, Func<T, TResult> selector)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            var result = new TResult[collection.Count];
            var index = 0;
            foreach (var item in collection)
                result[index++] = selector(item);
            return result;
        }

        public static HashSet<TResult> SelectToNewHashSet<T, TResult>(this IEnumerable<T> collection, Func<T, TResult> selector)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            var result = new HashSet<TResult>();
            foreach (var item in collection)
                result.Add(selector(item));
            return result;
        }

        public static Dictionary<TKey, T> ToNewDictionary<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector)
        {
            Dictionary<TKey, T> outputDictionary = null;
            ToDictionary(collection, keySelector, ref outputDictionary);
            return outputDictionary;
        }

        public static void ToDictionary<T, TKey>(this IEnumerable<T> collection, Func<T, TKey> keySelector, ref Dictionary<TKey, T> outputDictionary)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (keySelector == null)
                throw new ArgumentNullException(nameof(keySelector));

            if (outputDictionary == null)
                outputDictionary = new Dictionary<TKey, T>();
            else
                outputDictionary.Clear();

            foreach (var item in collection)
                outputDictionary.Add(keySelector(item), item);
        }

        public static void ToHashSet<T>(this IEnumerable<T> collection, ref HashSet<T> outputHashSet)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (outputHashSet == null)
                outputHashSet = new HashSet<T>();
            else
                outputHashSet.Clear();

            foreach (var item in collection)
                outputHashSet.Add(item);
        }

        public static T[] ToNewArray<T>(this IEnumerable<T> collection, int initialCapacity)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var result = new T[initialCapacity];
            var count = 0;
            foreach (var item in collection)
            {
                // We resize the array to at least 4 to handle the case where initialCapacity is 0 (which should be allowed
                // as sometimes the input collection is empty). We use 4 as that's a number used as the default by many implementations.
                if (count == result.Length)
                    Array.Resize(ref result, Math.Max(result.Length * 2, 4));
                result[count++] = item;
            }
            if (count != result.Length)
                Array.Resize(ref result, count);
            return result;
        }

        public static T[] ToNewArray<T>(this IReadOnlyCollection<T> collection)
        {
            T[] outputArray = null;
            ToArray(collection, ref outputArray);
            return outputArray;
        }

        public static void ToArray<T>(this IReadOnlyCollection<T> collection, ref T[] outputArray)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (outputArray == null)
                outputArray = new T[collection.Count];
            else if (outputArray.Length != collection.Count)
                Array.Resize(ref outputArray, collection.Count);
            var index = 0;
            using var enumerator = collection.GetEnumerator();
            while (enumerator.MoveNext() && index < collection.Count)
                outputArray[index++] = enumerator.Current;
        }

        public static IEnumerable<T> Join<T>(this IEnumerable<T> collection, params IEnumerable<T>[] collectionsToJoin)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collectionsToJoin == null)
                throw new ArgumentNullException(nameof(collectionsToJoin));

            foreach (var item in collection)
                yield return item;

            foreach (var list in collectionsToJoin)
                if (list != null)
                    foreach (var item in list)
                        yield return item;
        }

        public static IEnumerable<T> Join<T>(this IEnumerable<T> collection, params T[] collectionToJoin)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collectionToJoin == null)
                throw new ArgumentNullException(nameof(collectionToJoin));

            foreach (var item in collection)
                yield return item;

            foreach (var item in collectionToJoin)
                yield return item;
        }

        public static bool IsSequenceEqual<T>(this IReadOnlyCollection<T> first, IReadOnlyCollection<T> second, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(first, second))
                return true;

            if (first == null || second == null || first.Count != second.Count)
                return false;

            comparer ??= EqualityComparer<T>.Default;
            using var firstEnum = first.GetEnumerator();
            using var secondEnum = second.GetEnumerator();
            while (firstEnum.MoveNext())
            {
                secondEnum.MoveNext();
                if (!comparer.Equals(firstEnum.Current, secondEnum.Current))
                    return false;
            }
            return true;
        }
    }
}
