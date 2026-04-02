// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Timeline.Foundation.Common
{
    static class CollectionUtilities
    {
        public static bool Contains<T>(this IReadOnlyCollection<T> collection, T item)
        {
            switch (collection)
            {
                case IReadOnlyList<T> list:
                    foreach (T i in list)
                    {
                        if (i.Equals(item))
                            return true;
                    }

                    break;
                case HashSet<T> hashSet:
                    return hashSet.Contains(item);
                default:
                    return false;
            }

            return false;
        }
    }
}
