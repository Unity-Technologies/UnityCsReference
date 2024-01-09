// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlAttributeComparison
    {
        public static bool ObjectEquals(object a, object b)
        {
            // Comparison between (Object)null and null is considered as "equal".
            var aIsObjectNull = a != null && typeof(Object).IsAssignableFrom(a.GetType()) && !(a as Object);
            var bIsObjectNull = b != null && typeof(Object).IsAssignableFrom(b.GetType()) && !(b as Object);
            if ((aIsObjectNull && b is null) || (bIsObjectNull && a is null) || (aIsObjectNull && bIsObjectNull))
                return true;
            if (a is null && b is null)
                return true;
            if ((a is string && b == null) || (a == null && b is string))
            {
                // Null and empty are considered the same
                var aStr = a as string;
                var bStr = b as string;
                if (string.IsNullOrEmpty(aStr) && string.IsNullOrEmpty(bStr))
                    return true;
                return aStr == bStr;
            }
            if (a is null || b is null)
                return false;
            if (a is IList aList && b is IList bList)
                return ListEquals(aList, bList);
            if (a is not string &&
                a is not UnityEngine.Object &&
                a.GetType().IsClass &&
                UxmlAttributeConverter.TryConvertToString(a, null, out var aString) &&
                UxmlAttributeConverter.TryConvertToString(b, null, out var bString))
            {
                return aString == bString;
            }

            return a.Equals(b);
        }

        static bool ListEquals(IList a, IList b)
        {
            if (a.Count != b.Count)
                return false;

            // Compare contents, expecting the order of the elements to match.
            for (int i = 0; i < a.Count; ++i)
            {
                if (!ObjectEquals(a[i], b[i]))
                    return false;
            }
            return true;
        }
    }
}
