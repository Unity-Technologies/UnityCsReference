// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace Unity.UI.Builder
{
    internal static class TypeExtensions
    {
        public static string GetDisplayFullName(this Type type)
        {
            var np = type.Namespace;
            var typeName = TypeUtility.GetTypeDisplayName(type);

            if (!string.IsNullOrEmpty(np))
                typeName = $"{np}.{typeName}";
            return typeName;
        }

        public static string GetFullNameWithAssembly(this Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
