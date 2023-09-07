// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class IEnumerableExtensions
    {
        internal static bool HasValues(this IEnumerable<string> collection)
        {
            if (collection == null)
            {
                return false;
            }

            foreach (var unused in collection)
            {
                return true;
            }

            return false;
        }

        internal static bool NoElementOfTypeMatchesPredicate<T>(this IEnumerable collection, Func<T, bool> predicate)
        {
            foreach (var item in collection)
            {
                if (item is T element && predicate(element))
                    return false;
            }

            return true;
        }

        internal static int GetCount(this IEnumerable collection)
        {
            var count = 0;
            foreach (var item in collection)
            {
                count++;
            }

            return count;
        }
    }
}
