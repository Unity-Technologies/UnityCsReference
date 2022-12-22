// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.UI.Builder
{
    internal static class TypeExtensions
    {
        public static string GetFullNameWithAssembly(this Type type) => $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
