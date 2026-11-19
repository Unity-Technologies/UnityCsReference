// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// Reads and changes entities without first looking up the <see cref="World"/> that owns them.
    /// </summary>
    /// <remarks>
    /// Every per-entity method comes in two forms:
    ///
    /// * `X(entity)` acts in whichever world owns the entity. Use it when the world is incidental, for example when
    /// the entity came from another world, or from a reference stored on a component.
    /// * `X(world, entity)` acts in the given world and behaves exactly like `world.EntityManager.X(entity)`: an entity
    /// from another world is treated as missing. Use it when the entity must be in that world.
    ///
    /// Methods that create something take the world it goes into, because there's no entity to take it from.
    /// <see cref="Instantiate(WorldUnmanaged, Entity)"/> copies an entity from any world into the given world.
    ///
    /// Values that are only meaningful in one world, such as a shared component index, come back with that world.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public static unsafe class GlobalEntityManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EntityDataAccess* GetCheckedEntityDataAccess(Entity entity) => GetCheckedEntityDataAccess(entity, out _, out _);

        // The owner comes from the entity's chunk, so callers that need the chunk reuse it instead of looking it up again
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static EntityDataAccess* GetCheckedEntityDataAccess(Entity entity, out EntityInChunk entityInChunk, out Archetype* archetype)
        {
            entityInChunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity);
            archetype = EntityComponentStore.GetArchetypeOrNull(entityInChunk);
            if (Hint.Unlikely(archetype == null))
                ThrowEntityDoesNotExist();
            var access = archetype->EntityDataAccess;
            CheckCanAccess(access);
            return access;
        }

        // The owner is found from the entity, so only the EntityManager's safety and transaction checks apply
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckCanAccess(EntityDataAccess* access)
        {
            access->m_WorldUnmanaged.EntityManager.GetCheckedEntityDataAccess();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Archetype* GetCheckedArchetypeOrNull(Entity entity)
        {
            var archetype = EntityComponentStore.GetArchetypeOrNull(entity);
            if (archetype != null)
                CheckCanAccess(archetype->EntityDataAccess);
            return archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool HasComponent(Entity entity, TypeIndex typeIndex)
        {
            var archetype = GetCheckedArchetypeOrNull(entity);
            return archetype != null && ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex) != -1;
        }

        static void ThrowEntityDoesNotExist()
        {
            throw new ArgumentException("The entity does not exist.");
        }

        /// <summary>
        /// Gets the world that owns an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The world that owns the entity.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static WorldUnmanaged GetWorldUnmanaged(Entity entity) => GetCheckedEntityDataAccess(entity)->m_WorldUnmanaged;

        /// <summary>
        /// Gets the managed world that owns an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>The world that owns the entity.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        [ExcludeFromBurstCompatTesting("Returns managed World")]
        public static World GetWorld(Entity entity) => GetCheckedEntityDataAccess(entity)->ManagedEntityDataAccess.World;

        /// <summary>
        /// Checks whether an entity exists in any world.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity exists.</returns>
        public static bool Exists(Entity entity) => GetCheckedArchetypeOrNull(entity) != null;

        /// <summary>
        /// Checks whether an entity exists in the given world.
        /// </summary>
        /// <param name="world">The world to look in.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity exists in <paramref name="world"/>.</returns>
        public static bool Exists(WorldUnmanaged world, Entity entity) => world.EntityManager.Exists(entity);

        /// <summary>
        /// Checks whether an entity has a specific type of component.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity exists and has the component.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool HasComponent<T>(Entity entity) => HasComponent(entity, TypeManager.GetTypeIndex<T>());

        /// <inheritdoc cref="HasComponent{T}(Entity)"/>
        /// <param name="world">The world to look in. An entity from another world doesn't have the component.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool HasComponent<T>(WorldUnmanaged world, Entity entity) => world.EntityManager.HasComponent<T>(entity);

        /// <summary>
        /// Checks whether an entity has a specific type of component.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <returns>True if the entity exists and has the component.</returns>
        public static bool HasComponent(Entity entity, ComponentType type) => HasComponent(entity, type.TypeIndex);

        /// <inheritdoc cref="HasComponent(Entity, ComponentType)"/>
        /// <param name="world">The world to look in. An entity from another world doesn't have the component.</param>
        public static bool HasComponent(WorldUnmanaged world, Entity entity, ComponentType type) => world.EntityManager.HasComponent(entity, type);

        /// <summary>
        /// Checks whether an entity has a dynamic buffer of a specific type.
        /// </summary>
        /// <typeparam name="T">The buffer element type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the entity exists and has the buffer.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public static bool HasBuffer<T>(Entity entity) where T : struct, IBufferElementData => HasComponent(entity, TypeManager.GetTypeIndex<T>());

        /// <inheritdoc cref="HasBuffer{T}(Entity)"/>
        /// <param name="world">The world to look in. An entity from another world doesn't have the buffer.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public static bool HasBuffer<T>(WorldUnmanaged world, Entity entity) where T : struct, IBufferElementData => world.EntityManager.HasBuffer<T>(entity);

        /// <summary>
        /// Gets the value of a component for an entity.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>The component value.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponentData<T>(Entity entity) where T : unmanaged, IComponentData
        {
            UnsafeUtility.CopyPtrToStructure(GetComponentDataRO(entity, TypeManager.GetTypeIndex<T>()), out T value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte* GetComponentDataRO(Entity entity, TypeIndex typeIndex)
        {
            var access = GetCheckedEntityDataAccess(entity, out var entityInChunk, out var archetype);
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            access->EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);
            if (!access->IsInExclusiveTransaction)
                access->DependencyManager->CompleteWriteDependency(typeIndex);

            return ChunkDataUtility.GetComponentDataWithTypeRO(entityInChunk.Chunk, archetype, entityInChunk.IndexInChunk, typeIndex);
        }

        /// <inheritdoc cref="GetComponentData{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static T GetComponentData<T>(WorldUnmanaged world, Entity entity) where T : unmanaged, IComponentData => world.EntityManager.GetComponentData<T>(entity);

        /// <summary>
        /// Gets a read/write reference to a component of an entity.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>A reference to the component, which bumps its change version.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RefRW<T> GetComponentDataRW<T>(Entity entity) where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var data = GetComponentDataRW(entity, typeIndex, out var access);
            return new RefRW<T>(data, access->DependencyManager->Safety.GetSafetyHandle(typeIndex, false));
        }

        /// <inheritdoc cref="GetComponentDataRW{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static RefRW<T> GetComponentDataRW<T>(WorldUnmanaged world, Entity entity) where T : unmanaged, IComponentData => world.EntityManager.GetComponentDataRW<T>(entity);

        /// <summary>
        /// Sets the value of a component of an entity.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="componentData">The new value.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponentData<T>(Entity entity, T componentData) where T : unmanaged, IComponentData
        {
            var ptr = GetComponentDataRW(entity, TypeManager.GetTypeIndex<T>(), out _);
            UnsafeUtility.CopyStructureToPtr(ref componentData, ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte* GetComponentDataRW(Entity entity, TypeIndex typeIndex, out EntityDataAccess* access)
        {
            access = GetCheckedEntityDataAccess(entity, out var entityInChunk, out var archetype);
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            access->EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);
            if (!access->IsInExclusiveTransaction)
                access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            return ChunkDataUtility.GetComponentDataWithTypeRW(entityInChunk.Chunk, archetype, entityInChunk.IndexInChunk, typeIndex,
                access->EntityComponentStore->GlobalSystemVersion);
        }

        /// <inheritdoc cref="SetComponentData{T}(Entity, T)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static void SetComponentData<T>(WorldUnmanaged world, Entity entity, T componentData) where T : unmanaged, IComponentData => world.EntityManager.SetComponentData(entity, componentData);

        /// <summary>
        /// Gets a read-only pointer to a component of an entity.
        /// </summary>
        /// <remarks>
        /// The data is <see cref="TypeManager.TypeInfo.SizeInChunk"/> bytes long. The pointer is invalid after the next
        /// structural change in the entity's world.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">An unmanaged <see cref="IComponentData"/> type that isn't zero-sized.</param>
        /// <returns>A pointer to the component's data, which must not be written to.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't unmanaged component data or is zero-sized.</exception>
        public static void* GetComponentDataPtrRO(Entity entity, ComponentType type)
        {
            AssertCanPointToComponentData(type.TypeIndex);
            return GetComponentDataRO(entity, type.TypeIndex);
        }

        /// <inheritdoc cref="GetComponentDataPtrRO(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void* GetComponentDataPtrRO(WorldUnmanaged world, Entity entity, ComponentType type)
        {
            AssertCanPointToComponentData(type.TypeIndex);
            return world.EntityManager.GetComponentDataRawRO(entity, type.TypeIndex);
        }

        /// <summary>
        /// Gets a read/write pointer to a component of an entity.
        /// </summary>
        /// <remarks>
        /// The data is <see cref="TypeManager.TypeInfo.SizeInChunk"/> bytes long. The pointer is invalid after the next
        /// structural change in the entity's world. Getting it bumps the component's change version.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">An unmanaged <see cref="IComponentData"/> type that isn't zero-sized.</param>
        /// <returns>A pointer to the component's data.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't unmanaged component data or is zero-sized.</exception>
        public static void* GetComponentDataPtrRW(Entity entity, ComponentType type)
        {
            AssertCanPointToComponentData(type.TypeIndex);
            return GetComponentDataRW(entity, type.TypeIndex, out _);
        }

        /// <inheritdoc cref="GetComponentDataPtrRW(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void* GetComponentDataPtrRW(WorldUnmanaged world, Entity entity, ComponentType type)
        {
            AssertCanPointToComponentData(type.TypeIndex);
            return world.EntityManager.GetComponentDataRawRW(entity, type.TypeIndex);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void AssertCanPointToComponentData(TypeIndex typeIndex)
        {
            if (Hint.Unlikely(!typeIndex.IsComponentType || typeIndex.IsChunkComponent || typeIndex.IsManagedTypeInternal))
                throw new ArgumentException($"{typeIndex} isn't an unmanaged IComponentData type.");
            if (Hint.Unlikely(typeIndex.IsZeroSized))
                throw new ArgumentException($"{typeIndex} is zero-sized, so it has no data to point to.");
        }

        /// <summary>
        /// Gets a read-only pointer to a chunk component of the chunk that holds an entity.
        /// </summary>
        /// <remarks>
        /// The value lives on the chunk's meta entity, and is shared by every entity in the chunk. The data is
        /// <see cref="TypeManager.TypeInfo.SizeInChunk"/> bytes of the component's type. The pointer is invalid after the
        /// next structural change in the entity's world.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">A chunk component type, such as <see cref="ComponentType.ChunkComponent{T}"/> returns, of an
        /// unmanaged <see cref="IComponentData"/> type that isn't zero-sized.</param>
        /// <returns>A pointer to the component's data, which must not be written to.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the chunk component, or if
        /// <paramref name="type"/> isn't a chunk component of unmanaged component data.</exception>
        public static void* GetChunkComponentDataPtrRO(Entity entity, ComponentType type)
        {
            var metaEntity = GetMetaChunkEntity(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, out var typeIndex);
            return GetComponentDataRO(metaEntity, typeIndex);
        }

        /// <inheritdoc cref="GetChunkComponentDataPtrRO(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void* GetChunkComponentDataPtrRO(WorldUnmanaged world, Entity entity, ComponentType type)
        {
            var metaEntity = GetMetaChunkEntity(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, out var typeIndex);
            return world.EntityManager.GetComponentDataRawRO(metaEntity, typeIndex);
        }

        /// <summary>
        /// Gets a read/write pointer to a chunk component of the chunk that holds an entity.
        /// </summary>
        /// <remarks>
        /// The value lives on the chunk's meta entity, and is shared by every entity in the chunk. The data is
        /// <see cref="TypeManager.TypeInfo.SizeInChunk"/> bytes of the component's type. The pointer is invalid after the
        /// next structural change in the entity's world. Getting it bumps the component's change version in the meta
        /// entity's chunk.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">A chunk component type, such as <see cref="ComponentType.ChunkComponent{T}"/> returns, of an
        /// unmanaged <see cref="IComponentData"/> type that isn't zero-sized.</param>
        /// <returns>A pointer to the component's data.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the chunk component, or if
        /// <paramref name="type"/> isn't a chunk component of unmanaged component data.</exception>
        public static void* GetChunkComponentDataPtrRW(Entity entity, ComponentType type)
        {
            var metaEntity = GetMetaChunkEntity(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, out var typeIndex);
            return GetComponentDataRW(metaEntity, typeIndex, out _);
        }

        /// <inheritdoc cref="GetChunkComponentDataPtrRW(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void* GetChunkComponentDataPtrRW(WorldUnmanaged world, Entity entity, ComponentType type)
        {
            var metaEntity = GetMetaChunkEntity(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, out var typeIndex);
            return world.EntityManager.GetComponentDataRawRW(metaEntity, typeIndex);
        }

        // The meta entity holds the chunk component as the plain component type
        static Entity GetMetaChunkEntity(EntityDataAccess* access, Entity entity, TypeIndex chunkTypeIndex, out TypeIndex typeIndex)
        {
            if (Hint.Unlikely(!chunkTypeIndex.IsChunkComponent))
                throw new ArgumentException($"{chunkTypeIndex} isn't a chunk component type.");
            access->EntityComponentStore->AssertEntityHasComponent(entity, chunkTypeIndex);
            typeIndex = TypeManager.GetTypeInfo(chunkTypeIndex).TypeIndex;
            AssertCanPointToComponentData(typeIndex);
            return access->EntityComponentStore->GetChunk(entity).MetaChunkEntity;
        }

        /// <summary>
        /// Gets the version of a component in the chunk that holds an entity.
        /// </summary>
        /// <remarks>
        /// The version is the same one <see cref="ArchetypeChunk.GetChangeVersion{T}(ref ComponentTypeHandle{T})"/>
        /// returns, so it is per chunk, not per entity. Writing to the component in any entity of the chunk sets it to the
        /// world's current <see cref="EntityManager.GlobalSystemVersion"/>. A structural change can also set it.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">The component type.</param>
        /// <returns>The change version.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        public static uint GetChangeVersion(Entity entity, ComponentType type)
        {
            var access = GetCheckedEntityDataAccess(entity, out var entityInChunk, out var archetype);
            return GetChangeVersion(access, entity, entityInChunk, archetype, type.TypeIndex);
        }

        /// <inheritdoc cref="GetChangeVersion(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static uint GetChangeVersion(WorldUnmanaged world, Entity entity, ComponentType type)
        {
            var access = world.EntityManager.GetCheckedEntityDataAccess(entity);
            var entityInChunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity);
            return GetChangeVersion(access, entity, entityInChunk, access->EntityComponentStore->GetArchetype(entity), type.TypeIndex);
        }

        static uint GetChangeVersion(EntityDataAccess* access, Entity entity, EntityInChunk entityInChunk, Archetype* archetype, TypeIndex typeIndex)
        {
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!access->IsInExclusiveTransaction)
                access->DependencyManager->CompleteWriteDependency(typeIndex);
            return archetype->Chunks.GetChangeVersion(ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex), entityInChunk.Chunk.ListIndex);
        }

        /// <summary>
        /// Checks whether an enableable component of an entity is enabled.
        /// </summary>
        /// <typeparam name="T">The enableable component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the component is enabled.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComponentEnabled<T>(Entity entity) where T : unmanaged, IEnableableComponent => IsComponentEnabled(entity, TypeManager.GetTypeIndex<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsComponentEnabled(Entity entity, TypeIndex typeIndex)
        {
            var access = GetCheckedEntityDataAccess(entity, out var entityInChunk, out var archetype);
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!access->IsInExclusiveTransaction)
                access->DependencyManager->CompleteWriteDependency(typeIndex);
            return access->EntityComponentStore->IsComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk,
                ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex));
        }

        /// <inheritdoc cref="IsComponentEnabled{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public static bool IsComponentEnabled<T>(WorldUnmanaged world, Entity entity) where T : unmanaged, IEnableableComponent => world.EntityManager.IsComponentEnabled<T>(entity);

        /// <summary>
        /// Checks whether an enableable component of an entity is enabled.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">A component type that implements <see cref="IEnableableComponent"/>.</param>
        /// <returns>True if the component is enabled.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't enableable.</exception>
        public static bool IsComponentEnabled(Entity entity, ComponentType type)
        {
            EntityComponentStore.AssertComponentEnableable(type.TypeIndex);
            return IsComponentEnabled(entity, type.TypeIndex);
        }

        /// <inheritdoc cref="IsComponentEnabled(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static bool IsComponentEnabled(WorldUnmanaged world, Entity entity, ComponentType type) => world.EntityManager.IsComponentEnabled(entity, type);

        /// <summary>
        /// Enables or disables an enableable component of an entity.
        /// </summary>
        /// <typeparam name="T">The enableable component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="value">True to enable the component, false to disable it.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetComponentEnabled<T>(Entity entity, bool value) where T : unmanaged, IEnableableComponent => SetComponentEnabled(entity, TypeManager.GetTypeIndex<T>(), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetComponentEnabled(Entity entity, TypeIndex typeIndex, bool value)
        {
            var access = GetCheckedEntityDataAccess(entity, out var entityInChunk, out var archetype);
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!access->IsInExclusiveTransaction)
                access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            access->EntityComponentStore->SetComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk,
                ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex), value, access->EntityComponentStore->GlobalSystemVersion);
        }

        /// <inheritdoc cref="SetComponentEnabled{T}(Entity, bool)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        public static void SetComponentEnabled<T>(WorldUnmanaged world, Entity entity, bool value) where T : unmanaged, IEnableableComponent => world.EntityManager.SetComponentEnabled<T>(entity, value);

        /// <summary>
        /// Enables or disables an enableable component of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="type">A component type that implements <see cref="IEnableableComponent"/>.</param>
        /// <param name="value">True to enable the component, false to disable it.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't enableable.</exception>
        public static void SetComponentEnabled(Entity entity, ComponentType type, bool value)
        {
            EntityComponentStore.AssertComponentEnableable(type.TypeIndex);
            SetComponentEnabled(entity, type.TypeIndex, value);
        }

        /// <inheritdoc cref="SetComponentEnabled(Entity, ComponentType, bool)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void SetComponentEnabled(WorldUnmanaged world, Entity entity, ComponentType type, bool value) => world.EntityManager.SetComponentEnabled(entity, type, value);

        /// <summary>
        /// Gets the dynamic buffer of an entity.
        /// </summary>
        /// <typeparam name="T">The buffer element type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="isReadOnly">True if the buffer is only read, so writes to it aren't tracked as changes.</param>
        /// <returns>The buffer.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the buffer.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public static DynamicBuffer<T> GetBuffer<T>(Entity entity, bool isReadOnly = false) where T : unmanaged, IBufferElementData => EntityManager.GetBufferInternal<T>(GetCheckedEntityDataAccess(entity), entity, isReadOnly);

        /// <inheritdoc cref="GetBuffer{T}(Entity, bool)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public static DynamicBuffer<T> GetBuffer<T>(WorldUnmanaged world, Entity entity, bool isReadOnly = false) where T : unmanaged, IBufferElementData => world.EntityManager.GetBuffer<T>(entity, isReadOnly);

        // Internal so the native GlobalEntityManager can reach buffers until DynamicBuffer has a public untyped twin
        internal static UnsafeUntypedDynamicBuffer GetBuffer(Entity entity, ComponentType type, bool isReadOnly = false) => GetBuffer(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, isReadOnly);

        internal static UnsafeUntypedDynamicBuffer GetBuffer(WorldUnmanaged world, Entity entity, ComponentType type, bool isReadOnly = false) => GetBuffer(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, isReadOnly);

        static UnsafeUntypedDynamicBuffer GetBuffer(EntityDataAccess* access, Entity entity, TypeIndex typeIndex, bool isReadOnly)
        {
            // Nothing constrains the type as it does for GetBuffer<T>, and any other type's data isn't a buffer header
            if (Hint.Unlikely(!typeIndex.IsBuffer))
                throw new ArgumentException($"{typeIndex} isn't an IBufferElementData type.");
            var store = access->EntityComponentStore;
            store->AssertEntityHasComponent(entity, typeIndex);
            if (!access->IsInExclusiveTransaction)
            {
                if (isReadOnly)
                    access->DependencyManager->CompleteWriteDependency(typeIndex);
                else
                    access->DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            }

            var header = (BufferHeader*)(isReadOnly
                ? store->GetComponentDataWithTypeRO(entity, typeIndex)
                : store->GetComponentDataWithTypeRW(entity, typeIndex, store->GlobalSystemVersion));
            ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
            var safetyHandles = &access->DependencyManager->Safety;
            return new UnsafeUntypedDynamicBuffer(header, typeInfo.BufferCapacity, typeInfo.ElementSize, typeInfo.AlignmentInBytes,
                safetyHandles->GetSafetyHandle(typeIndex, isReadOnly), safetyHandles->GetBufferSafetyHandle(typeIndex), isReadOnly,
                store->useMemoryInitPattern != 0, store->memoryInitPattern);
        }

        /// <summary>
        /// Gets the name of an entity.
        /// </summary>
        /// <remarks>Entity names only exist in the Unity Editor.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="name">The entity's name.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static void GetName(Entity entity, out FixedString64Bytes name)
        {
            GetCheckedEntityDataAccess(entity)->GetName(entity, out name);
        }

        /// <inheritdoc cref="GetName(Entity, out FixedString64Bytes)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void GetName(WorldUnmanaged world, Entity entity, out FixedString64Bytes name) => world.EntityManager.GetName(entity, out name);

        /// <summary>
        /// Sets the name of an entity.
        /// </summary>
        /// <remarks>Entity names only exist in the Unity Editor.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="name">The name to assign.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static void SetName(Entity entity, FixedString64Bytes name)
        {
            GetCheckedEntityDataAccess(entity)->SetName(entity, in name);
        }

        /// <inheritdoc cref="SetName(Entity, FixedString64Bytes)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void SetName(WorldUnmanaged world, Entity entity, FixedString64Bytes name) => world.EntityManager.SetName(entity, name);

        // A component's Object state: what the engine keeps for a data component outside its data, such as its hide flags.
        // It lives in the owning world's store, not in the chunk, so writing it doesn't change the component's change
        // version. Internal until the state moves into chunk components; the public API is shaped then.

        // Gets the Object state of a component of an entity; the defaults until it is set.
        // Throws ArgumentException if the entity doesn't exist or doesn't have the component.
        internal static ComponentObjectState GetComponentObjectState(Entity entity, ComponentType type) =>
            GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->GetComponentObjectState(entity, type.TypeIndex);

        // An entity that another world owns is treated as missing
        internal static ComponentObjectState GetComponentObjectState(WorldUnmanaged world, Entity entity, ComponentType type) =>
            GetCheckedStore(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->GetComponentObjectState(entity, type.TypeIndex);

        // Sets the whole Object state of a component. To change one field, get the state, change the field and set it.
        // Throws ArgumentException if the entity doesn't exist or doesn't have the component.
        internal static void SetComponentObjectState(Entity entity, ComponentType type, in ComponentObjectState state) =>
            GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->SetComponentObjectState(entity, type.TypeIndex, state);

        internal static void SetComponentObjectState(WorldUnmanaged world, Entity entity, ComponentType type, in ComponentObjectState state) =>
            GetCheckedStore(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->SetComponentObjectState(entity, type.TypeIndex, state);

        // Whether the component's Object state differs from the defaults; a component at the defaults keeps no entry
        internal static bool HasComponentObjectState(Entity entity, ComponentType type) =>
            GetCheckedStore(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex)->HasComponentObjectState(entity, type.TypeIndex);

        static EntityComponentStore* GetCheckedStore(EntityDataAccess* access, Entity entity, TypeIndex typeIndex)
        {
            var store = access->EntityComponentStore;
            store->AssertEntityHasComponent(entity, typeIndex);
            return store;
        }

        /// <summary>
        /// Gets the value of a shared component of an entity.
        /// </summary>
        /// <typeparam name="T">The shared component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>The shared component value.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static T GetSharedComponent<T>(Entity entity) where T : unmanaged, ISharedComponentData => GetCheckedEntityDataAccess(entity)->GetSharedComponentData_Unmanaged<T>(entity);

        /// <inheritdoc cref="GetSharedComponent{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static T GetSharedComponent<T>(WorldUnmanaged world, Entity entity) where T : unmanaged, ISharedComponentData => world.EntityManager.GetSharedComponent<T>(entity);

        /// <summary>
        /// Gets the index of a shared component value of an entity, and the world the index belongs to.
        /// </summary>
        /// <remarks>Shared component indices are per world, so the index is only meaningful in <paramref name="world"/>.</remarks>
        /// <typeparam name="T">The shared component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="world">The world that owns the entity, and so the index.</param>
        /// <returns>The shared component index in <paramref name="world"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static int GetSharedComponentIndex<T>(Entity entity, out WorldUnmanaged world) where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess(entity);
            world = access->m_WorldUnmanaged;
            var typeIndex = TypeManager.GetTypeIndex<T>();
            access->EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            return access->EntityComponentStore->GetSharedComponentDataIndex(entity, typeIndex);
        }

        /// <summary>
        /// Gets the index of a shared component value of an entity in the given world.
        /// </summary>
        /// <typeparam name="T">The shared component type.</typeparam>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>The shared component index in <paramref name="world"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static int GetSharedComponentIndex<T>(WorldUnmanaged world, Entity entity) where T : unmanaged, ISharedComponentData => world.EntityManager.GetSharedComponentIndex<T>(entity);

        /// <summary>
        /// Sets the value of a shared component of an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <typeparam name="T">The shared component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="componentData">The new value.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static void SetSharedComponent<T>(Entity entity, T componentData) where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            access->SetSharedComponentData_Unmanaged(entity, TypeManager.GetTypeIndex<T>(), UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="SetSharedComponent{T}(Entity, T)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static void SetSharedComponent<T>(WorldUnmanaged world, Entity entity, T componentData) where T : unmanaged, ISharedComponentData => world.EntityManager.SetSharedComponent(entity, componentData);

        /// <summary>
        /// Gets a read-only pointer to the value of a shared component of an entity.
        /// </summary>
        /// <remarks>
        /// The value is <see cref="TypeManager.TypeInfo.TypeSize"/> bytes long, and is shared by every entity with the
        /// same value in the entity's world. The pointer is invalid after the next structural change in that world.
        /// </remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">An <see cref="ISharedComponentData"/> type.</param>
        /// <returns>A pointer to the value, which must not be written to.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't a shared component type.</exception>
        public static void* GetSharedComponentPtrRO(Entity entity, ComponentType type) => GetSharedComponentPtrRO(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex);

        /// <inheritdoc cref="GetSharedComponentPtrRO(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void* GetSharedComponentPtrRO(WorldUnmanaged world, Entity entity, ComponentType type) => GetSharedComponentPtrRO(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex);

        static void* GetSharedComponentPtrRO(EntityDataAccess* access, Entity entity, TypeIndex typeIndex)
        {
            AssertIsSharedComponent(typeIndex);
            var store = access->EntityComponentStore;
            store->AssertEntityHasComponent(entity, typeIndex);
            // Index 0 is the type's default value, which is the first entry of its list
            return store->GetSharedComponentDataAddr_Unmanaged(store->GetSharedComponentDataIndex(entity, typeIndex), typeIndex);
        }

        /// <summary>
        /// Sets the value of a shared component of an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="type">An <see cref="ISharedComponentData"/> type.</param>
        /// <param name="componentData">The new value, <see cref="TypeManager.TypeInfo.TypeSize"/> bytes long.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist or doesn't have the component, or if
        /// <paramref name="type"/> isn't a shared component type.</exception>
        public static void SetSharedComponent(Entity entity, ComponentType type, void* componentData) => SetSharedComponent(GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, componentData);

        /// <inheritdoc cref="SetSharedComponent(Entity, ComponentType, void*)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void SetSharedComponent(WorldUnmanaged world, Entity entity, ComponentType type, void* componentData) => SetSharedComponent(world.EntityManager.GetCheckedEntityDataAccess(entity), entity, type.TypeIndex, componentData);

        static void SetSharedComponent(EntityDataAccess* access, Entity entity, TypeIndex typeIndex, void* componentData)
        {
            AssertIsSharedComponent(typeIndex);
            var size = TypeManager.GetTypeInfo(typeIndex).TypeSize;
            var defaultValue = stackalloc byte[size];
            UnsafeUtility.MemClear(defaultValue, size);
            var changes = access->BeginStructuralChanges();
            access->SetSharedComponentData_Unmanaged(entity, typeIndex, componentData, defaultValue);
            access->EndStructuralChanges(ref changes);
        }

        // The shared component lists are looked up by type, so any other type would read or insert into the wrong list
        static void AssertIsSharedComponent(TypeIndex typeIndex)
        {
            if (Hint.Unlikely(!typeIndex.IsSharedComponentType))
                throw new ArgumentException($"{typeIndex} isn't an ISharedComponentData type.");
        }

        /// <summary>
        /// Adds a shared component to an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <typeparam name="T">The shared component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="componentData">The value.</param>
        /// <returns>False if the entity already had the component, in which case only its value is set.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static bool AddSharedComponent<T>(Entity entity, T componentData) where T : unmanaged, ISharedComponentData
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var defaultValue = default(T);
            var added = access->AddSharedComponentDataDuringStructuralChange_Unmanaged(entity, ComponentType.ReadWrite<T>(),
                UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.AddressOf(ref defaultValue));
            access->EndStructuralChanges(ref changes);
            return added;
        }

        /// <inheritdoc cref="AddSharedComponent{T}(Entity, T)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public static bool AddSharedComponent<T>(WorldUnmanaged world, Entity entity, T componentData) where T : unmanaged, ISharedComponentData => world.EntityManager.AddSharedComponent(entity, componentData);

        /// <summary>
        /// Adds a component to an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>False if the entity already had the component.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool AddComponent<T>(Entity entity) => AddComponent(entity, ComponentType.ReadWrite<T>());

        /// <inheritdoc cref="AddComponent{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool AddComponent<T>(WorldUnmanaged world, Entity entity) => world.EntityManager.AddComponent<T>(entity);

        /// <summary>
        /// Adds a component to an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>False if the entity already had the component.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static bool AddComponent(Entity entity, ComponentType componentType)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var added = access->AddComponentDuringStructuralChange(entity, componentType);
            access->EndStructuralChanges(ref changes);
            return added;
        }

        /// <inheritdoc cref="AddComponent(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static bool AddComponent(WorldUnmanaged world, Entity entity, ComponentType componentType) => world.EntityManager.AddComponent(entity, componentType);

        /// <summary>
        /// Adds components to an entity.
        /// </summary>
        /// <remarks>This is one structural change, however many types the set holds. Types the entity already has are
        /// skipped.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="types">The component types.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static void AddComponent(Entity entity, in ComponentTypeSet types)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            access->AddMultipleComponentsDuringStructuralChange(entity, types);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="AddComponent(Entity, in ComponentTypeSet)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void AddComponent(WorldUnmanaged world, Entity entity, in ComponentTypeSet types) => world.EntityManager.AddComponent(entity, types);

        /// <summary>
        /// Adds a component to an entity and sets its value.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="componentData">The value.</param>
        /// <returns>False if the entity already had the component, in which case only its value is set.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool AddComponentData<T>(Entity entity, T componentData) where T : unmanaged, IComponentData
        {
            var type = ComponentType.ReadWrite<T>();
            if (type.IsZeroSized)
                return AddComponent(entity, type);

            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var added = access->AddComponentDuringStructuralChange(entity, type, UnsafeUtility.AddressOf(ref componentData), UnsafeUtility.SizeOf<T>());
            access->EndStructuralChanges(ref changes);
            return added;
        }

        /// <inheritdoc cref="AddComponentData{T}(Entity, T)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool AddComponentData<T>(WorldUnmanaged world, Entity entity, T componentData) where T : unmanaged, IComponentData => world.EntityManager.AddComponentData(entity, componentData);

        /// <summary>
        /// Removes a component from an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns>False if the entity didn't have the component.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool RemoveComponent<T>(Entity entity) => RemoveComponent(entity, ComponentType.ReadWrite<T>());

        /// <inheritdoc cref="RemoveComponent{T}(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static bool RemoveComponent<T>(WorldUnmanaged world, Entity entity) => world.EntityManager.RemoveComponent<T>(entity);

        /// <summary>
        /// Removes a component from an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="componentType">The component type.</param>
        /// <returns>False if the entity didn't have the component.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static bool RemoveComponent(Entity entity, ComponentType componentType)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            var removed = access->RemoveComponentDuringStructuralChange(entity, componentType);
            access->EndStructuralChanges(ref changes);
            return removed;
        }

        /// <inheritdoc cref="RemoveComponent(Entity, ComponentType)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static bool RemoveComponent(WorldUnmanaged world, Entity entity, ComponentType componentType) => world.EntityManager.RemoveComponent(entity, componentType);

        /// <summary>
        /// Removes components from an entity.
        /// </summary>
        /// <remarks>This is one structural change, however many types the set holds. Types the entity doesn't have are
        /// skipped.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="types">The component types.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static void RemoveComponent(Entity entity, in ComponentTypeSet types)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            access->RemoveComponentDuringStructuralChange(entity, types);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="RemoveComponent(Entity, in ComponentTypeSet)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void RemoveComponent(WorldUnmanaged world, Entity entity, in ComponentTypeSet types) => world.EntityManager.RemoveComponent(entity, types);

        /// <summary>
        /// Gets the component types of an entity.
        /// </summary>
        /// <remarks>The types come in the order of the entity's archetype, which is stable while the archetype is
        /// unchanged.</remarks>
        /// <param name="entity">The entity.</param>
        /// <param name="allocator">The allocator for the returned array.</param>
        /// <returns>The entity's component types.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static NativeArray<ComponentType> GetComponentTypes(Entity entity, AllocatorManager.AllocatorHandle allocator)
        {
            GetCheckedEntityDataAccess(entity, out _, out var archetype);
            return new EntityArchetype { Archetype = archetype }.GetComponentTypes(allocator);
        }

        /// <inheritdoc cref="GetComponentTypes(Entity, AllocatorManager.AllocatorHandle)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static NativeArray<ComponentType> GetComponentTypes(WorldUnmanaged world, Entity entity, AllocatorManager.AllocatorHandle allocator) => world.EntityManager.GetComponentTypes(entity, allocator);

        /// <summary>
        /// Destroys an entity.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="entity">The entity.</param>
        /// <exception cref="ArgumentException">Thrown if the entity doesn't exist.</exception>
        public static void DestroyEntity(Entity entity)
        {
            var access = GetCheckedEntityDataAccess(entity);
            var changes = access->BeginStructuralChanges();
            access->DestroyEntityInternalDuringStructuralChange(&entity, 1);
            access->EndStructuralChanges(ref changes);
        }

        /// <inheritdoc cref="DestroyEntity(Entity)"/>
        /// <param name="world">The world that owns the entity. An entity from another world is treated as missing.</param>
        public static void DestroyEntity(WorldUnmanaged world, Entity entity) => world.EntityManager.DestroyEntity(entity);

        /// <summary>
        /// Creates an entity with no components in the given world.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="world">The world to create the entity in.</param>
        /// <returns>The new entity.</returns>
        public static Entity CreateEntity(WorldUnmanaged world) => world.EntityManager.CreateEntity();

        /// <summary>
        /// Creates an entity of an archetype in the given world.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="world">The world to create the entity in.</param>
        /// <param name="archetype">An archetype created in <paramref name="world"/>.</param>
        /// <returns>The new entity.</returns>
        public static Entity CreateEntity(WorldUnmanaged world, EntityArchetype archetype) => world.EntityManager.CreateEntity(archetype);

        /// <summary>
        /// Creates an entity with the given components in the given world.
        /// </summary>
        /// <remarks>This is a structural change.</remarks>
        /// <param name="world">The world to create the entity in.</param>
        /// <param name="types">The component types of the new entity.</param>
        /// <returns>The new entity.</returns>
        public static Entity CreateEntity(WorldUnmanaged world, ReadOnlySpan<ComponentType> types) => world.EntityManager.CreateEntity(types);

        /// <summary>
        /// Creates an archetype in the given world.
        /// </summary>
        /// <param name="world">The world to create the archetype in.</param>
        /// <param name="types">The component types of the archetype.</param>
        /// <returns>The archetype, which is only valid in <paramref name="world"/>.</returns>
        public static EntityArchetype CreateArchetype(WorldUnmanaged world, ReadOnlySpan<ComponentType> types) => world.EntityManager.CreateArchetype(types);

        /// <summary>
        /// Clones an entity from any world into the given world.
        /// </summary>
        /// <remarks>
        /// The clone has the same components and values as the source, without <see cref="Prefab"/> and cleanup
        /// components. If the source has a <see cref="LinkedEntityGroup"/>, the whole group is cloned, and references
        /// between the cloned entities point at the clones.
        ///
        /// When <paramref name="srcEntity"/> is in another world, references to entities outside the group become
        /// <see cref="Entity.Null"/>, because they point into the source world.
        ///
        /// This is a structural change in <paramref name="dstWorld"/>. Instantiating from another world also completes
        /// the source world's jobs that write to the components being cloned.
        /// </remarks>
        /// <param name="dstWorld">The world to create the clone in.</param>
        /// <param name="srcEntity">The entity to clone, in any world.</param>
        /// <returns>The clone, in <paramref name="dstWorld"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="srcEntity"/> doesn't exist.</exception>
        public static Entity Instantiate(WorldUnmanaged dstWorld, Entity srcEntity)
        {
            var srcAccess = GetCheckedEntityDataAccess(srcEntity);
            var access = dstWorld.EntityManager.GetCheckedEntityDataAccess();

            Entity entity;
            EntityComponentStore.ArchetypeChanges changes;
            if (srcAccess == access)
            {
                access->PrepareForAdditiveStructuralChanges(access->EntityComponentStore->GetArchetype(srcEntity)->InstantiateArchetype);
                changes = access->BeginAdditiveStructuralChanges();
            }
            else
            {
                srcAccess->CompleteWriteDependenciesToInstantiate(srcEntity);
                changes = access->BeginStructuralChanges();
            }
            access->InstantiateInternalDuringStructuralChange(srcEntity, &entity, 1);
            access->EndStructuralChanges(ref changes);
            return entity;
        }
    }
}
