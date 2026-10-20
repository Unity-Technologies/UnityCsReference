// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary> Obsolete. Use <see cref="BufferLookup{T}"/> instead.</summary>
    /// <typeparam name="T">The type of <see cref="IBufferElementData"/> to access.</typeparam>
    [Obsolete("This type has been renamed to BufferLookup<T>. (RemovedAfter Entities 1.0) (UnityUpgradable) -> BufferLookup<T>", true)]
    public struct BufferFromEntity<T> where T : unmanaged, IBufferElementData
    {
    }

    /// <summary>
    /// A [NativeContainer] that provides access to all instances of DynamicBuffer components with elements of type T,
    /// indexed by <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IBufferElementData"/> to access.</typeparam>
    /// <remarks>
    /// BufferLookup is a native container that provides array-like access to DynamicBuffer components of a specific
    /// type. For example, while iterating over a set of entities, you can use BufferLookup to get and set  DynamicBuffers of unrelated entities.
    ///
    /// To get a BufferLookup, call <see cref="SystemAPI.GetBufferLookup{T}"/>.
    ///
    /// Pass a BufferLookup container to a job by defining a public field of the appropriate type
    /// in your IJob implementation. You can safely read from BufferLookup in any job, but by
    /// default, you cannot write to components in the container in parallel jobs (including
    /// <see cref="IJobEntity"/>, <see cref="SystemAPI.Query{T}"/> and <see cref="IJobChunk"/>). If you know that two instances of a parallel
    /// job can never write to the same index in the container, you can disable the restriction on parallel writing
    /// by adding [NativeDisableParallelForRestrictionAttribute] to the BufferLookup field definition in the job struct.
    ///
    ///
    /// [NativeContainer]: https://docs.unity3d.com/ScriptReference/Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute
    /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
    /// </remarks>
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct BufferLookup<T> where T : unmanaged, IBufferElementData
    {
        internal AtomicSafetyHandle m_Safety0;
        internal AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;

        [NativeDisableUnsafePtrRestriction] private readonly EntityDataAccess* m_Access;
        LookupCache m_Cache;
        private readonly TypeIndex m_TypeIndex;

        uint m_GlobalSystemVersion;
        int m_InternalCapacity;
        private readonly byte  m_IsReadOnly;

        internal uint GlobalSystemVersion => m_GlobalSystemVersion;


        internal BufferLookup(TypeIndex typeIndex, EntityDataAccess* access, bool isReadOnly,
                                  AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            m_Safety0 = safety;
            m_ArrayInvalidationSafety = arrayInvalidationSafety;
            m_SafetyReadOnlyCount = isReadOnly ? 2 : 0;
            m_SafetyReadWriteCount = isReadOnly ? 0 : 2;
            m_TypeIndex = typeIndex;
            m_Access = access;
            m_IsReadOnly = isReadOnly ? (byte)1 : (byte)0;
            m_Cache = default;
            m_GlobalSystemVersion = access->EntityComponentStore->GlobalSystemVersion;

            if (!TypeManager.IsBuffer(m_TypeIndex))
            {
                var typeName = m_TypeIndex.ToFixedString();
                throw new ArgumentException(
                    $"GetComponentBufferArray<{typeName}> must be IBufferElementData");
            }

            m_InternalCapacity = TypeManager.GetTypeInfo<T>().BufferCapacity;
        }


        /// <summary>
        /// Retrieves the buffer components associated with the specified <see cref="Entity"/>, if it exists. Then reports if the instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// /// <param name="bufferData">The buffer component of type T for the given entity, if it exists.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not.</returns>
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> bufferData) => TryGetBuffer(entity, out bufferData, out _);

        /// <summary>
        /// Retrieves the buffer components associated with the specified <see cref="Entity"/>, if it exists. Then reports if the instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="bufferData">The buffer component of type T for the given entity, if it exists.</param>
        /// <param name="entityExists">Denotes whether the given entity exists. Use to distinguish entity non-existence
        /// from buffer non-existence.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not.</returns>
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> bufferData, out bool entityExists)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            entityExists = ecs->Exists(entity);
            if (Hint.Unlikely(!entityExists))
            {
                bufferData = default;
                return false;
            }

            var header = (m_IsReadOnly != 0)?
                (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache) :
                (BufferHeader*)ecs->GetOptionalComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_Cache);

            if (header != null)
            {
                bufferData =  new DynamicBuffer<T>(header, m_Safety0, m_ArrayInvalidationSafety, m_IsReadOnly != 0, false, 0, m_InternalCapacity);
                return true;
            }
            else
            {
                bufferData = default;
                return false;
            }
        }

        /// <summary>
        /// Reports whether the specified entity exists.
        /// Does not consider whether this buffer exists on the given entity.
        /// </summary>
        /// <param name="entity">The referenced entity.</param>
        /// <returns>True if the entity exists, regardless of whether this entity has the given buffer.</returns>
        /// <seealso cref="TryGetBuffer(Unity.Entities.Entity,out Unity.Entities.DynamicBuffer{T},out bool)"/>
        public bool EntityExists(Entity entity)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            return ecs->Exists(entity);
        }

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        public bool HasBuffer(Entity entity) => HasBuffer(entity, out _);

        /// <summary>
        /// Reports whether the specified <see cref="Entity"/> instance still refers to a valid entity and that it has a
        /// buffer component of type T.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entityExists">Denotes whether the given entity exists. Use to distinguish entity non-existence
        /// from buffer non-existence.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        public bool HasBuffer(Entity entity, out bool entityExists)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            return ecs->HasComponent(entity, m_TypeIndex, ref m_Cache, out entityExists);
        }

        /// <summary> Obsolete. Use <see cref="HasBuffer(Unity.Entities.Entity)"/> instead.</summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity has a buffer component of type T, and false if it does not. Also returns false if
        /// the Entity instance refers to an entity that has been destroyed.</returns>
        [Obsolete("This method has been renamed to HasBuffer(). (RemovedAfter Entities 1.0)", false)] // Can't use (UnityUpgradable) due to transitive update restriction
        public bool HasComponent(Entity entity)
        {
            return HasBuffer(entity);
        }

        /// <summary>
        /// Reports whether any of IBufferElementData components of the type T, in the chunk containing the
        /// specified <see cref="Entity"/>, could have changed.
        /// </summary>
        /// <remarks>
        /// Note that for efficiency, the change version applies to whole chunks not individual entities. The change
        /// version is incremented even when another job or system that has declared write access to a component does
        /// not actually change the component value.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="version">The version to compare. In a system, this parameter should be set to the
        /// current <see cref="Unity.Entities.ComponentSystemBase.LastSystemVersion"/> at the time the job is run or
        /// scheduled.</param>
        /// <returns>True, if the version number stored in the chunk for this component is more recent than the version
        /// passed to the <paramref name="version"/> parameter.</returns>
        public bool DidChange(Entity entity, uint version)
        {
            var ecs = m_Access->EntityComponentStore;
            var chunk = ecs->GetChunk(entity);
            var archetype = ecs->GetArchetype(chunk);
            if (Hint.Unlikely(archetype != m_Cache.Archetype))
                m_Cache.Update(archetype, m_TypeIndex);
            var typeIndexInArchetype = m_Cache.IndexInArchetype;
            if (typeIndexInArchetype == -1) return false;
            var chunkVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.ListIndex);

            return ChangeVersionUtility.DidChange(chunkVersion, version);
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}"/> instance of type <typeparamref name="T"/> for the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>A <see cref="DynamicBuffer{T}"/> type.</returns>
        /// <remarks>
        /// Normally, you cannot write to buffers accessed using a BufferLookup instance
        /// in a parallel Job. This restriction is in place because multiple threads could write to the same buffer,
        /// leading to a race condition and nondeterministic results. However, when you are certain that your algorithm
        /// cannot write to the same buffer from different threads, you can manually disable this safety check
        /// by putting the [NativeDisableParallelForRestriction] attribute on the BufferLookup field in the Job.
        ///
        /// [NativeDisableParallelForRestrictionAttribute]: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeDisableParallelForRestrictionAttribute.html
        /// </remarks>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="entity"/> does not have a buffer
        /// component of type <typeparamref name="T"/>.</exception>
        public DynamicBuffer<T> this[Entity entity]
        {
            get
            {
                var ecs = m_Access->EntityComponentStore;
                // Note that this check is only for the lookup table into the entity manager
                // The native array performs the actual read only / write only checks
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
                ecs->AssertEntityHasComponent(entity, m_TypeIndex, ref m_Cache);

                var header = (m_IsReadOnly != 0)?
                    (BufferHeader*)ecs->GetComponentDataWithTypeRO(entity, m_TypeIndex, ref m_Cache) :
                    (BufferHeader*)ecs->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_Cache);

                return new DynamicBuffer<T>(header, m_Safety0, m_ArrayInvalidationSafety, m_IsReadOnly != 0, false, 0, m_InternalCapacity);
            }
        }

        /// <summary>
        /// Checks whether the <see cref="IBufferElementData"/> of type T is enabled on the specified <see cref="Entity"/>.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be checked.</param>
        /// <returns>True if the specified component is enabled, or false if it is disabled.</returns>
        /// <seealso cref="SetBufferEnabled"/>
        public bool IsBufferEnabled(Entity entity)
        {
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            return m_Access->IsComponentEnabled(entity, m_TypeIndex, ref m_Cache);
        }

        /// <summary>Obsolete. Use <see cref="IsBufferEnabled"/> instead.</summary>
        /// <remarks>**Obsolete.** Use <see cref="IsBufferEnabled"/> instead.
        ///
        /// Checks whether the <see cref="IBufferElementData"/> of type T is enabled on the specified <see cref="Entity"/>.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be checked.</param>
        /// <seealso cref="SetBufferEnabled"/>
        [Obsolete("Use IsBufferEnabled (RemovedAfter: Entities pre-1.0)")] // Can't use (UnityUpgradable) due to transitive update restriction
        public void IsComponentEnabled(Entity entity) => IsBufferEnabled(entity);

        /// <summary>Obsolete. Use <see cref="SetBufferEnabled"/> instead.</summary>
        /// <remarks>**Obsolete.** Use <see cref="SetBufferEnabled"/> instead.
        ///
        /// Enable or disable the <see cref="IBufferElementData"/> of type T on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </remarks>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// <seealso cref="IsBufferEnabled"/>
        [Obsolete("Use SetBufferEnabled (RemovedAfter: Entities pre-1.0)")] // Can't use (UnityUpgradable) due to transitive update restriction
        public void SetComponentEnabled(Entity entity, bool value) => SetBufferEnabled(entity, value);

        /// <summary>
        /// Enable or disable the <see cref="IBufferElementData"/> of type T on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change (even if it occurs on a worker thread), or affect the value of the component.
        /// For the purposes of EntityQuery matching, an entity with a disabled component will behave as if it does not
        /// have that component. The type T must implement the <see cref="IEnableableComponent"/> interface.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="Entity"/> does not exist.</exception>
        /// <param name="entity">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        /// <seealso cref="IsBufferEnabled"/>
        public void SetBufferEnabled(Entity entity, bool value)
        {
            // Note that this check is only for the lookup table into the entity manager
            // The native array performs the actual read only / write only checks
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            m_Access->SetComponentEnabled(entity, m_TypeIndex, value, ref m_Cache);
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="system">The system on which this type handle is cached.</param>
        public void Update(SystemBase system)
        {
            Update(ref *system.m_StatePtr);
        }

        /// <summary>
        /// When a BufferLookup is cached by a system across multiple system updates, calling this function
        /// inside the system's OnUpdate() method performs the minimal incremental updates necessary to make the
        /// type handle safe to use.
        /// </summary>
        /// <param name="systemState">The SystemState of the system on which this type handle is cached.</param>
        public void Update(ref SystemState systemState)
        {
            // NOTE: We could in theory fetch all this data from m_Access.EntityComponentStore and void the SystemState from being passed in.
            //       That would unfortunately allow this API to be called from a job. So we use the required system parameter as a way of signifying to the user that this can only be invoked from main thread system code.
            //       Additionally this makes the API symmetric to ComponentTypeHandle.
            m_GlobalSystemVersion =  systemState.m_EntityComponentStore->GlobalSystemVersion;

            var safetyHandles = &m_Access->DependencyManager->Safety;
            m_Safety0 = safetyHandles->GetSafetyHandleForComponentLookup(m_TypeIndex, m_IsReadOnly != 0);
            m_ArrayInvalidationSafety = safetyHandles->GetBufferHandleForBufferLookup(m_TypeIndex);
        }

        SafeBitRef MakeSafeBitRef(ulong* ptr, int offsetInBits)
            => new SafeBitRef(ptr, offsetInBits, m_Safety0);
        /// <summary>
        /// Gets a safe reference to the buffer component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns>Returns a safe reference to the component enabled state.
        /// Throws an exception if the component doesn't exist.</returns>
        public EnabledRefRW<T2> GetEnabledRefRW<T2>(Entity entity) where T2 : unmanaged, IEnableableComponent, IBufferElementData
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, m_TypeIndex, ref m_Cache);

            int indexInBitField;
            int* ptrChunkDisabledCount;
            var ptr = ecs->GetEnabledRawRW(entity, m_TypeIndex, ref m_Cache, m_GlobalSystemVersion,
                out indexInBitField, out ptrChunkDisabledCount);

            return new EnabledRefRW<T2>(MakeSafeBitRef(ptr, indexInBitField), ptrChunkDisabledCount);
        }

        /// <summary>
        /// Gets a safe reference to the component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns>Returns a safe reference to the component enabled state. If the component
        /// doesn't exist, it returns a default <see cref="EnabledRefRW{T}"/>.</returns>
        public EnabledRefRW<T2> GetEnabledRefRWOptional<T2>(Entity entity)
            where T2 : unmanaged, IBufferElementData, IEnableableComponent
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            if (!HasBuffer(entity))
                return new EnabledRefRW<T2>(default, default);

            var ecs = m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, m_TypeIndex, ref m_Cache);

            var ptr = ecs->GetEnabledRawRW(entity, m_TypeIndex, ref m_Cache, m_GlobalSystemVersion,
                out var indexInBitField, out var ptrChunkDisabledCount);

            return new EnabledRefRW<T2>(MakeSafeBitRef(ptr, indexInBitField), ptrChunkDisabledCount);
        }
        /// <summary> Obsolete. Use <see cref="GetEnabledRefRWOptional{T}"/> instead.</summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns>Returns a safe reference to the component enabled state. If the component
        /// doesn't exist, it returns a default <see cref="EnabledRefRW{T}"/>.</returns>
        [Obsolete("This method has been renamed to GetEnabledRefRWOptional<T>. (RemovedAfter Entities 1.0)", false)]
        public EnabledRefRW<T2> GetComponentEnabledRefRWOptional<T2>(Entity entity)
            where T2 : unmanaged, IBufferElementData, IEnableableComponent
        {
            return GetEnabledRefRWOptional<T2>(entity);
        }

        /// <summary>
        /// Gets a safe reference to the component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns>Returns a safe reference to the component enabled state.
        /// Throws an exception if the component doesn't exist.</returns>
        public EnabledRefRO<T2> GetEnabledRefRO<T2>(Entity entity) where T2 : unmanaged, IEnableableComponent, IBufferElementData
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            var ecs = m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, m_TypeIndex, ref m_Cache);
            int indexInBitField;
            var ptr = ecs->GetEnabledRawRO(entity, m_TypeIndex, ref m_Cache, out indexInBitField, out _);
            return new EnabledRefRO<T2>(MakeSafeBitRef(ptr, indexInBitField));
        }

        /// <summary>
        /// Gets a safe reference to the component enabled state.
        /// </summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns> Returns a safe reference to the component enabled state.
        /// If the component doesn't exist, returns a default <see cref="EnabledRefRO{T}"/>.</returns>
        public EnabledRefRO<T2> GetEnabledRefROOptional<T2>(Entity entity)
            where T2 : unmanaged, IBufferElementData, IEnableableComponent
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety0);
            if (!HasBuffer(entity))
                return new EnabledRefRO<T2>(default);

            var ecs = m_Access->EntityComponentStore;
            ecs->AssertEntityHasComponent(entity, m_TypeIndex, ref m_Cache);
            int indexInBitField;
            var ptr = ecs->GetEnabledRawRO(entity, m_TypeIndex, ref m_Cache, out indexInBitField, out _);
            return new EnabledRefRO<T2>(MakeSafeBitRef(ptr, indexInBitField));
        }

        /// <summary> Obsolete. Use <see cref="GetEnabledRefROOptional{T}"/> instead.</summary>
        /// <typeparam name="T2">The component type</typeparam>
        /// <param name="entity">The referenced entity</param>
        /// <returns> Returns a safe reference to the component enabled state.
        /// If the component doesn't exist, returns a default <see cref="EnabledRefRO{T}"/>.</returns>
        [Obsolete("This method has been renamed to GetEnabledRefROOptional<T>. (RemovedAfter Entities 1.0)", false)]
        public EnabledRefRO<T2> GetComponentEnabledRefROOptional<T2>(Entity entity)
            where T2 : unmanaged, IBufferElementData, IEnableableComponent
        {
            return GetEnabledRefROOptional<T2>(entity);
        }
    }
}
