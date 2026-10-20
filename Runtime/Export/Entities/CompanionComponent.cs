// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace Unity.Entities
{
    /// <summary>
    /// An unmanaged component that references a UnityEngine.Component attached to an entity.
    /// </summary>
    /// <remarks>
    /// Register each type you use with <see cref="RegisterGenericComponentTypeAttribute"/>, for
    /// example <c>[assembly: RegisterGenericComponentType(typeof(CompanionComponent&lt;Light&gt;))]</c>.
    /// </remarks>
    public struct CompanionComponent<T> : IComponentData where T : Component
    {
        /// <summary>
        /// The reference to the UnityEngine.Component that this component exposes on the entity.
        /// </summary>
        public UnityObjectRef<T> CompanionRef;
    }

    /// <summary>
    /// Extension methods for reading companion components from an EntityManager.
    /// </summary>
    public static class CompanionComponentExtensions
    {
        /// <summary>
        /// Returns the UnityEngine.Component of type T referenced by the entity's companion component.
        /// </summary>
        /// <remarks>
        /// Resolving the reference is a managed call, so call this method from the main thread. Throws
        /// if <paramref name="entity"/> has no <see cref="CompanionComponent{T}"/>.
        /// </remarks>
        /// <param name="entityManager">The entity manager that stores <paramref name="entity"/>.</param>
        /// <param name="entity">The entity to read the companion component from.</param>
        /// <returns>The <typeparamref name="T"/> component referenced by the entity's <see cref="CompanionComponent{T}"/>.</returns>
        public static T GetCompanion<T>(this EntityManager entityManager, Entity entity)
            where T : Component
        {
            return entityManager.GetComponentData<CompanionComponent<T>>(entity).CompanionRef;
        }
    }

    internal static class CompanionComponentTypeIndices
    {
        [NoAutoStaticsCleanup] // TypeManager.Shutdown disposes it through CoreModuleLightShim.
        static UnsafeHashSet<TypeIndex> s_Set;

        internal static void RegisterIfWrapper(Type type, TypeIndex typeIndex)
        {
            if (!type.IsGenericType || type.IsGenericTypeDefinition)
                return;
            if (type.GetGenericTypeDefinition() != typeof(CompanionComponent<>))
                return;
            if (!s_Set.IsCreated)
                s_Set = new UnsafeHashSet<TypeIndex>(16, Allocator.Persistent);
            s_Set.Add(typeIndex);
        }

        internal static void Clear() => s_Set.Dispose();

        public static bool Contains(TypeIndex typeIndex) => s_Set.IsCreated && s_Set.Contains(typeIndex);
    }
}
