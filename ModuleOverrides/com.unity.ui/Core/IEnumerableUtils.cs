// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    }
}
