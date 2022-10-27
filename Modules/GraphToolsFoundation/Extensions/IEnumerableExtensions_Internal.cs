// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.GraphToolsFoundation
{
    // ReSharper disable once InconsistentNaming
    static class IEnumerableExtensions_Internal
    {
        internal static int IndexOf_Internal<T>(this IEnumerable<T> source, T element)
        {
            if (source is IList<T> list)
                return list.IndexOf(element);

            int i = 0;
            foreach (var x in source)
            {
                if (Equals(x, element))
                    return i;
                i++;
            }

            return -1;
        }
    }
}
