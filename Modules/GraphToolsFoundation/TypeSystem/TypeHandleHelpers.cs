// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// Methods for creating type handles.
    /// </summary>
    static class TypeHandleHelpers
    {
        static System.Text.RegularExpressions.Regex s_GenericTypeExtractionRegex = new System.Text.RegularExpressions.Regex(@"(?<=\[\[)(.*?)(?=\]\])");

        internal static List<ValueTuple<string, TypeHandle>> customIdToTypeHandle_Internal = new List<ValueTuple<string, TypeHandle>>();
        static List<ValueTuple<string, Type>> s_CustomIdToType = new List<ValueTuple<string, Type>>();

        internal static Func<string, Type> GetMovedFromType;

        static string Serialize(Type t)
        {
            var mapping = s_CustomIdToType.Find(e => e.Item2 == t);
            if (mapping != default)
            {
                return mapping.Item1;
            }

            return t.AssemblyQualifiedName;
        }

        static Type Deserialize(string serializedType)
        {
            var mapping = s_CustomIdToType.Find(e => e.Item1 == serializedType);
            if (mapping != default)
            {
                return mapping.Item2;
            }

            return GetTypeFromName_Internal(serializedType);
        }

        internal static Type GetTypeFromName_Internal(string assemblyQualifiedName)
        {
            Type retType = typeof(Unknown);
            if (!string.IsNullOrEmpty(assemblyQualifiedName))
            {
                var type = Type.GetType(assemblyQualifiedName);
                if (type == null)
                {
                    // Check if the type has moved
                    assemblyQualifiedName = ExtractAssemblyQualifiedName(assemblyQualifiedName, out var isList);
                    var movedType = GetMovedFromType?.Invoke(assemblyQualifiedName);
                    if (movedType != null)
                    {
                        type = movedType;
                        if (isList)
                        {
                            type = typeof(List<>).MakeGenericType(type);
                        }
                    }
                }

                retType = type ?? retType;
            }
            return retType;
        }

        static string ExtractAssemblyQualifiedName(string fullTypeName, out bool isList)
        {
            isList = false;
            if (fullTypeName.StartsWith("System.Collections.Generic.List"))
            {
                fullTypeName = s_GenericTypeExtractionRegex.Match(fullTypeName).Value;
                isList = true;
            }

            // remove the assembly version string
            var versionIdx = fullTypeName.IndexOf(", Version=");
            if (versionIdx > 0)
                fullTypeName = fullTypeName.Substring(0, versionIdx);
            // replace all '+' with '/' to follow the Unity serialization convention for nested types
            fullTypeName = fullTypeName.Replace("+", "/");
            return fullTypeName;
        }

        internal static Type ResolveType_Internal(TypeHandle th)
        {
            return Deserialize(th.Identification);
        }

        internal static bool IsCustomTypeHandle_Internal(string id)
        {
            return customIdToTypeHandle_Internal.Exists(e => e.Item1 == id) && !s_CustomIdToType.Exists(e => e.Item1 == id);
        }

        /// <summary>
        /// Creates a type handle for a custom type.
        /// </summary>
        /// <param name="uniqueId">The unique identifier for the custom type.</param>
        /// <returns>A type handle representing the custom type.</returns>
        public static TypeHandle GenerateCustomTypeHandle(string uniqueId)
        {
            TypeHandle th;
            var typeHandleMapping = customIdToTypeHandle_Internal.Find(e => e.Item1 == uniqueId);
            if (typeHandleMapping != default)
            {
                Debug.LogWarning(uniqueId + " is already registered in TypeSerializer");
                return typeHandleMapping.Item2;
            }

            th = TypeHandle.Create_Internal(uniqueId);
            customIdToTypeHandle_Internal.Add((uniqueId, th));
            return th;
        }

        /// <summary>
        /// Creates a type handle for a type using a custom identifier.
        /// </summary>
        /// <param name="t">The type for which to create a type handle.</param>
        /// <param name="customUniqueId">The unique custom identifier for the type.</param>
        /// <returns>A type handle for the type.</returns>
        public static TypeHandle GenerateCustomTypeHandle(Type t, string customUniqueId)
        {
            TypeHandle th;

            var typeHandleMapping = customIdToTypeHandle_Internal.Find(e => e.Item1 == customUniqueId);
            if (typeHandleMapping != default)
            {
                Debug.LogWarning(customUniqueId + " is already registered in TypeSerializer");
                return typeHandleMapping.Item2;
            }

            var typeMapping = s_CustomIdToType.Find(e => e.Item2 == t);
            if (typeMapping != default)
            {
                Debug.LogWarning(t.FullName + " is already registered in TypeSerializer");
            }

            th = TypeHandle.Create_Internal(customUniqueId);

            customIdToTypeHandle_Internal.Add((customUniqueId, th));
            s_CustomIdToType.Add((customUniqueId, t));

            return th;
        }

        /// <summary>
        /// Creates a type handle for a type.
        /// </summary>
        /// <typeparam name="T">The type for which to create a type handle.</typeparam>
        /// <returns>A type handle for the type.</returns>
        public static TypeHandle GenerateTypeHandle<T>()
        {
            return GenerateTypeHandle(typeof(T));
        }

        /// <summary>
        /// Creates a type handle for a type.
        /// </summary>
        /// <param name="t">The type for which to create a type handle.</param>
        /// <returns>A type handle for the type.</returns>
        public static TypeHandle GenerateTypeHandle(Type t)
        {
            Assert.IsNotNull(t);
            return TypeHandle.Create_Internal(Serialize(t));
        }
    }
}
