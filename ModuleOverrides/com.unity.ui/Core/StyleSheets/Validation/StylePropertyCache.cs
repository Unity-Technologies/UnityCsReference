// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class StylePropertyCache
    {
        public static bool TryGetSyntax(string name, out string syntax)
        {
            return s_PropertySyntaxCache.TryGetValue(name, out syntax);
        }

        public static string FindClosestPropertyName(string name)
        {
            float cost = float.MaxValue;
            string closestName = null;

            foreach (var propName in s_PropertySyntaxCache.Keys)
            {
                float factor = 1;
                // Add some weight to the check if the name is part of the property name
                if (propName.Contains(name))
                    factor = 0.1f;

                float d = StringUtils.LevenshteinDistance(name, propName) * factor;
                if (d < cost)
                {
                    cost = d;
                    closestName = propName;
                }
            }

            return closestName;
        }
    }
}
