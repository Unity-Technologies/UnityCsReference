// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

static class EnumerableExtensions
{
    public static string SeparateWith(this IEnumerable<string> values, string separator)
    {
        return string.Join(separator, values);
    }

    public static (List<T> True, List<T> False) SplitBy<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
        (List<T> True, List<T> False)result = (new List<T>(collection.Count), new List<T>(collection.Count));
        foreach (var item in collection)
            if (predicate(item))
                result.True.Add(item);
            else
                result.False.Add(item);
        return result;
    }
}
