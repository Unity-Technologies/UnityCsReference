// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;

namespace UnityEditor.UIElements
{
    internal static class UxmlAttributeComparison
    {
        public static bool ObjectEquals(object a, object b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
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
