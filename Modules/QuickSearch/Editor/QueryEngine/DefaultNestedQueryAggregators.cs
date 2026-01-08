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
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? new T[] {} : new T[] {enumerable.Max()};
#pragma warning restore RS0030
        }
    }

    static class MinAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? new T[] {} : new T[] {enumerable.Min()};
#pragma warning restore RS0030
        }
    }

    static class FirstAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? new T[] {} : new T[] {enumerable.First()};
#pragma warning restore RS0030
        }
    }

    static class LastAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? new T[] {} : new T[] {enumerable.Last()};
#pragma warning restore RS0030
        }
    }
}
