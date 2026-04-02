// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolkit
{
    /// <summary>
    /// Describes a TypeHandle, it allows to define a custom friendly name instead of a generated one
    /// </summary>
    struct TypeHandleDescriptor
    {
        /// <summary>
        /// Creates a new TypeHandleDescriptor
        /// </summary>
        /// <param name="typeHandle">TypeHandle to describe</param>
        /// <param name="friendlyName">Friendly name for this TypeHanlde</param>
        public TypeHandleDescriptor(TypeHandle typeHandle, string friendlyName)
        {
            TypeHandle = typeHandle;
            FriendlyName = friendlyName;
        }

        /// <summary>
        /// TypeHandle being described
        /// </summary>
        public TypeHandle TypeHandle { get; }
        /// <summary>
        /// Friendly name to used with this TypeHandle
        /// </summary>
        public string FriendlyName { get; }
    }

    /// <summary>
    /// Methods for creating type handles.
    /// </summary>
    [UnityRestricted]
    internal static class TypeHandleHelpers
    {
        static Dictionary<string, TypeHandleDescriptor> s_CustomIdToTypeHandleInternal = new();
        static Dictionary<string, Type> s_CustomIdToType = new();

        // For tests only
        internal static (Dictionary<string, TypeHandleDescriptor>, Dictionary<string, Type>) GetState()
        {
            return (s_CustomIdToTypeHandleInternal, s_CustomIdToType);
        }

        // For tests only
        internal static void SetState((Dictionary<string, TypeHandleDescriptor>, Dictionary<string, Type>) state)
        {
            s_CustomIdToTypeHandleInternal = state.Item1;
            s_CustomIdToType = state.Item2;
        }

        internal static Type ResolveType(TypeHandle th)
        {
            if (th.Identification != null && s_CustomIdToTypeHandleInternal.ContainsKey(th.Identification))
            {
                return s_CustomIdToType.TryGetValue(th.Identification, out var type) ? type : typeof(Unknown);
            }

            return InternalTypeHelpers.GetTypeFromTypeName(th.Identification) ?? typeof(Unknown);
        }

        internal static string ResolveMovedFromType(string identification)
        {
            if (identification != null && !s_CustomIdToTypeHandleInternal.ContainsKey(identification))
            {
                var type = InternalTypeHelpers.ResolveMovedFromType(identification);
                return type?.AssemblyQualifiedName;
            }
            return null;
        }

        internal static bool IsCustomTypeHandle(this TypeHandle typeHandle)
        {
            return typeHandle.Identification != null && s_CustomIdToTypeHandleInternal.ContainsKey(typeHandle.Identification);
        }

        internal static string GetFriendlyName_Internal(this TypeHandle typeHandle)
        {
            if (typeHandle.Identification == null)
                return null;
            return s_CustomIdToTypeHandleInternal.GetValueOrDefault(typeHandle.Identification).FriendlyName;
        }

        static (TypeHandle, bool) GetOrCreateCustomTypeHandle(string uniqueId, string friendlyName = null)
        {
            if (!s_CustomIdToTypeHandleInternal.TryGetValue(uniqueId, out var thd))
            {
                var th = TypeHandle.Create(uniqueId);
                s_CustomIdToTypeHandleInternal[uniqueId] = new TypeHandleDescriptor(th, friendlyName);
                return (th, true);
            }

            if (thd.FriendlyName != friendlyName)
            {
                throw new InvalidOperationException($"A custom TypeHandle with same friendly name '{friendlyName}' already exists");
            }

            return (thd.TypeHandle, false);
        }

        /// <summary>
        /// Creates a type handle for a custom type.
        /// </summary>
        /// <param name="uniqueId">The unique identifier for the custom type.</param>
        /// <param name="friendlyName">Friendly name for this TypeHandle</param>
        /// <returns>A type handle representing the custom type.</returns>
        public static TypeHandle GenerateCustomTypeHandle(string uniqueId, string friendlyName = null)
        {
            var (th, isNew) = GetOrCreateCustomTypeHandle(uniqueId, friendlyName);

            if (!isNew)
            {
                Debug.LogWarning(uniqueId + " is already registered in TypeSerializer");
            }

            return th;
        }

        /// <summary>
        /// Creates a type handle for a type using a custom identifier.
        /// </summary>
        /// <param name="t">The type for which to create a type handle.</param>
        /// <param name="uniqueId">The unique custom identifier for the type.</param>
        /// <param name="friendlyName">Friendly name for this TypeHandle</param>
        /// <returns>A type handle for the type.</returns>
        public static TypeHandle GenerateCustomTypeHandle(Type t, string uniqueId, string friendlyName = null)
        {
            var (th, isNew) = GetOrCreateCustomTypeHandle(uniqueId, friendlyName);

            if (isNew)
            {
                s_CustomIdToType[uniqueId] = t;
            }
            else
            {
                if (th.Resolve() != t)
                {
                    throw new ArgumentException($"TypeHandle {uniqueId} already refers to a different type.", t.Name);
                }

                Debug.LogWarning(uniqueId + " is already registered in TypeSerializer");
            }

            return th;
        }

        /// <summary>
        /// Rebinds a custom type handle to a new type.
        /// </summary>
        /// <param name="typeHandle">The type handle to rebind.</param>
        /// <param name="t">The type to bind the type handle to.</param>
        public static void RebindCustomTypeHandle(TypeHandle typeHandle, Type t)
        {
            if (typeHandle.Identification != null && s_CustomIdToTypeHandleInternal.ContainsKey(typeHandle.Identification))
            {
                s_CustomIdToType[typeHandle.Identification] = t;
            }
            else
            {
                throw new ArgumentException("TypeHandle is not a custom type handle.", nameof(typeHandle));
            }
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
        /// <param name="friendlyName">Friendly name for this TypeHandle</param>
        /// <returns>A type handle for the type.</returns>
        public static TypeHandle GenerateTypeHandle(Type t, string friendlyName = null)
        {
            Assert.IsNotNull(t);
            var identification = t.AssemblyQualifiedName;
            if (!string.IsNullOrEmpty(friendlyName))
            {
                if (s_CustomIdToTypeHandleInternal.GetValueOrDefault(identification).FriendlyName is { } existingFriendlyName && friendlyName != existingFriendlyName)
                {
                    throw new InvalidOperationException($"A type with same identification but a different friendly name exists {existingFriendlyName} != {friendlyName}");
                }
            }

            var th = TypeHandle.Create(t.AssemblyQualifiedName);
            if (!string.IsNullOrEmpty(friendlyName))
            {
                s_CustomIdToTypeHandleInternal[identification] = new TypeHandleDescriptor(th, friendlyName);
            }

            return th;
        }
    }
}
