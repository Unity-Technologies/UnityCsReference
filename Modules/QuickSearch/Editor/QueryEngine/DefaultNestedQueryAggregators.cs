// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search
{
    static class MaxAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            var empty = !enumerable.Any();
            return empty ? new T[] {} : new T[] {enumerable.Max()};
        }
    }

    static class MinAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            var empty = !enumerable.Any();
            return empty ? new T[] {} : new T[] {enumerable.Min()};
        }
    }

    static class FirstAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            var empty = !enumerable.Any();
            return empty ? new T[] {} : new T[] {enumerable.First()};
        }
    }

    static class LastAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            var empty = !enumerable.Any();
            return empty ? new T[] {} : new T[] {enumerable.Last()};
        }
    }
}
