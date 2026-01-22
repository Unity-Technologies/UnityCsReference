// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;

namespace Unity.GraphToolkit.Editor
{
    static class TypeExtensions
    {
        internal static bool IsListOrArray(this Type type)
        {
            return typeof(IList).IsAssignableFrom(type);
        }
    }
}
