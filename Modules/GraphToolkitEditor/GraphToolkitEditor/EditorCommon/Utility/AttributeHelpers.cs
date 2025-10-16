// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    static class AttributeHelpers
    {
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            var attrs = type.GetCustomAttributes(typeof(T), false);
            if (attrs.Length == 0)
                return null;
            return attrs[0] as T;
        }
    }
}
