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
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore UA2002
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? System.Array.Empty<T>() : [enumerable.Max()];
#pragma warning restore UA2001
        }
    }

    static class MinAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore UA2002
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? System.Array.Empty<T>() : [enumerable.Min()];
#pragma warning restore UA2001
        }
    }

    static class FirstAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore UA2002
            #pragma warning disable UA2010 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? System.Array.Empty<T>() : [enumerable.First()];
#pragma warning restore UA2010
        }
    }

    static class LastAggregator<T>
    {
        public static IEnumerable<T> Aggregate(IEnumerable<T> enumerable)
        {
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var empty = !enumerable.Any();
#pragma warning restore UA2002
            #pragma warning disable UA2009 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return empty ? System.Array.Empty<T>() : [enumerable.Last()];
#pragma warning restore UA2009
        }
    }
}
