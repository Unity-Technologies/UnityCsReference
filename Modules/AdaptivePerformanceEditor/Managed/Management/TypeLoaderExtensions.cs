// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;

namespace UnityEditor.AdaptivePerformance.Editor
{
    internal static class TypeLoaderExtensions
    {
        public static TypeCache.TypeCollection GetTypesWithInterface<T>(this Assembly asm)
        {
            return TypeCache.GetTypesDerivedFrom(typeof(T));
        }

        public static TypeCache.TypeCollection GetAllTypesWithInterface<T>()
        {
            return TypeCache.GetTypesDerivedFrom(typeof(T));
        }

        public static TypeCache.TypeCollection GetTypesWithAttribute<T>(this Assembly asm)
        {
            return TypeCache.GetTypesWithAttribute(typeof(T));
        }

        public static TypeCache.TypeCollection GetAllTypesWithAttribute<T>()
        {
            return TypeCache.GetTypesWithAttribute(typeof(T));
        }
    }
}
