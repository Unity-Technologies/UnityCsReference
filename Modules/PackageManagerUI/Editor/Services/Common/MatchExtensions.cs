// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class MatchExtensions
    {
        public static bool AnyMatches<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (collection == null || predicate == null)
                return false;

            foreach (var item in collection)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }
    }
}
