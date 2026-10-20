// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    public unsafe partial struct EntityManager
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the name assigned to an entity.
        /// </summary>
        /// <remarks>For performance, entity names only exist when running in the Unity Editor.</remarks>
        /// <param name="entity">The Entity object of the entity of interest.</param>
        /// <returns>The entity name, as a managed string.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public string GetName(Entity entity)
        {
            return GetCheckedEntityDataAccess()->GetName(entity);
        }

        /// <summary>
        /// Gets the name assigned to an entity.
        /// </summary>
        /// <remarks>For performance, entity names only exist when running in the Unity Editor.</remarks>
        /// <param name="entity">The Entity object of the entity of interest.</param>
        /// <param name="name">The entity's name will be stored here.</param>
        [GenerateTestsForBurstCompatibility]
        public void GetName(Entity entity, out FixedString64Bytes name)
        {
            GetCheckedEntityDataAccess()->GetName(entity, out name);
        }

        /// <summary>
        /// Sets the name of an entity.
        /// </summary>
        /// <remarks>
        /// <para>Note that any `System.String` names will implicitly cast to <see cref="FixedString64Bytes"/>.
        /// This conversion will throw, rather than truncate, if over capacity (61 characters).
        /// Thus, ensure your names fit, or manually truncate them first (via <see cref="FixedStringMethods.CopyFromTruncated{T}"/>).</para>
        /// <para>However, GameObjects converted to entities (via Baking) will have long names silently truncated.</para>
        /// <para>For performance, entity names only exist when running in the Unity Editor.</para>
        /// </remarks>
        /// <param name="entity">The Entity object of the entity to name.</param>
        /// <param name="name">The name to assign. See remarks for caveats.</param>
        [GenerateTestsForBurstCompatibility]
        public void SetName(Entity entity, FixedString64Bytes name)
        {
            GetCheckedEntityDataAccess()->SetName(entity, in name);
        }

        /// <summary>
        /// Options include internal entity types such as <see cref="Chunk.metaChunkEntity"/> or system entities.
        /// </summary>
        [Flags]
        public enum GetAllEntitiesOptions
        {
            /// <summary>
            /// Returns the same entities as <see cref="UniversalQuery"/>
            /// </summary>
            Default = 0,
            /// <summary>
            /// Includes any <see cref="Chunk.metaChunkEntity"/> in the query
            /// </summary>
            IncludeMeta = 1 << 0,
            /// <summary>
            /// Includes any System associated Entities in the query
            /// </summary>
            IncludeSystems = 1 << 1,
        }

        /// <summary>
        /// Gets all the entities managed by this EntityManager.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before getting the entities and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="allocator">The type of allocation for creating the NativeArray to hold the Entity objects.</param>
        /// <returns>An array of Entity objects referring to all the entities in the World.</returns>
        public NativeArray<Entity> GetAllEntities(Allocator allocator = Allocator.Temp)
            => GetAllEntities((AllocatorManager.AllocatorHandle) allocator, GetAllEntitiesOptions.Default);

        /// <summary>
        /// Gets all the entities managed by this EntityManager.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before getting the entities and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="allocator">The type of allocation for creating the NativeArray to hold the Entity objects.</param>
        /// <returns>An array of Entity objects referring to all the entities in the World.</returns>
        public NativeArray<Entity> GetAllEntities(AllocatorManager.AllocatorHandle allocator)
            => GetAllEntities(allocator, GetAllEntitiesOptions.Default);

        /// <summary>
        /// Gets all the entities managed by this EntityManager.
        /// </summary>
        /// <remarks>
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before getting the entities and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <param name="allocator">The type of allocation for creating the NativeArray to hold the Entity objects.</param>
        /// <param name="options">Specifies whether entities from chunk components should be included.</param>
        /// <returns>An array of Entity objects referring to all the entities in the World.</returns>
        public NativeArray<Entity> GetAllEntities(AllocatorManager.AllocatorHandle allocator, GetAllEntitiesOptions options)
        {
            NativeArray<ArchetypeChunk> chunks = default;
            if ((options & GetAllEntitiesOptions.IncludeMeta) != 0)
            {
                if ((options & GetAllEntitiesOptions.IncludeSystems) != 0)
                    chunks = GetAllChunksAndMetaChunksWithSystems(allocator);
                else
                    chunks = GetAllChunksAndMetaChunks(allocator);
            }
            else if ((options & GetAllEntitiesOptions.IncludeSystems) != 0)
                chunks = GetAllChunksWithSystems(allocator);
            else if (options == GetAllEntitiesOptions.Default)
                chunks = GetAllChunks(allocator);
            else
                throw new ArgumentException($"Invalid enum value", nameof(options));

            var count = ArchetypeChunkArray.TotalEntityCountInChunksIgnoreFiltering(chunks);
            var array = CollectionHelper.CreateNativeArray<Entity>(count, allocator);
            var entityType = GetEntityTypeHandle();
            var offset = 0;

            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var entities = chunk.GetNativeArray(entityType);
                array.Slice(offset, entities.Length).CopyFrom(entities);
                offset += entities.Length;
            }

            chunks.Dispose();
            return array;
        }

        /// <summary>
        /// Provides information and utility functions for debugging.
        /// </summary>
        public readonly struct EntityManagerDebug
        {
            private readonly EntityManager m_Manager;

            /// <summary>
            /// Creates an EntityManagerDebug from an EntityManager
            /// </summary>
            /// <param name="entityManager">The EntityManager to debug</param>
            public EntityManagerDebug(EntityManager entityManager)
            {
                m_Manager = entityManager;
            }

            /// <summary>
            /// Sets all unused chunk data for an archetype to the specified byte value.
            /// </summary>
            /// <param name="archetype">The archetype to modify</param>
            /// <param name="value">The value to set for any unused chunk data</param>
            public void PoisonUnusedDataInAllChunks(EntityArchetype archetype, byte value)
            {
                EntityComponentStore.AssertValidArchetype(m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore, archetype);

                for (var i = 0; i < archetype.Archetype->Chunks.Count; ++i)
                {
                    var chunk = archetype.Archetype->Chunks[i];
                    ChunkDataUtility.MemsetUnusedChunkData(archetype.Archetype, chunk.Buffer, value, chunk.Count);
                }
            }

            internal void IncrementGlobalSystemVersion(in SystemHandle handle = default)
            {
                m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->IncrementGlobalSystemVersion(in handle);
            }

            internal void SetGlobalSystemVersion(uint version, in SystemHandle handle = default)
            {
                m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->SetGlobalSystemVersion(version, in handle);
            }

            /// <summary>
            /// Checks to see if the world holds any shared component values
            /// </summary>
            /// <returns>True if the world does not hold any shared component values</returns>
            public bool IsSharedComponentManagerEmpty()
            {
                return m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->GetUnmanagedSharedComponentCount() == 0;
            }

            internal static string GetArchetypeDebugString(Archetype* a)
            {
                var buf = new System.Text.StringBuilder();
                buf.Append("(");

                for (var i = 0; i < a->TypesCount; i++)
                {
                    var componentTypeInArchetype = a->Types[i];
                    if (i > 0)
                        buf.Append(", ");
                    buf.Append(componentTypeInArchetype.ToString());
                }

                buf.Append(")");
                return buf.ToString();
            }

            /// <summary>
            /// The number of entities in the referenced EntityManager
            /// </summary>
            public int EntityCount
            {
                get
                {
                    var allEntities = m_Manager.GetAllEntities();
                    var count = allEntities.Length;
                    allEntities.Dispose();
                    return count;
                }
            }

            /// <summary>
            /// The highest entity index of all the entities in the EntityManager
            /// </summary>
            public int HighestIndexEntity
            {
                get
                {
                    var maxIndex = -1;

                    var entityDataAccess = m_Manager.GetCheckedEntityDataAccess();
                    var archetypes = entityDataAccess->EntityComponentStore->m_Archetypes;
                    for (int ai = 0, ac = archetypes.Length; ai < ac; ai++)
                    {
                        var chunks = archetypes[ai]->Chunks;
                        for (int ci = 0, cc = chunks.Count; ci < cc; ci++)
                        {
                            var chunk = chunks[ci];
                            var entities = (Entity*)chunk.Buffer;
                            for (int ei = 0, ec = chunk.Count; ei < ec; ei++)
                            {
                                var index = entities[ei].Index;
                                maxIndex = index > maxIndex ? index : maxIndex;
                            }
                        }
                    }

                    return maxIndex;
                }
            }

            /// <summary>
            /// Determines if chunks created in the <see cref="EntityComponentStore"/> use the specified <see cref="MemoryInitPattern"/>
            /// </summary>
            public bool UseMemoryInitPattern
            {
                get => m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->useMemoryInitPattern != 0;
                set => m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->useMemoryInitPattern = value ? (byte)1 : (byte)0;
            }

            /// <summary>
            /// A specified memory pattern used when initializing new chunks if <see cref="UseMemoryInitPattern"/> is set to true
            /// </summary>
            public byte MemoryInitPattern
            {
                get => m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->memoryInitPattern;
                set => m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->memoryInitPattern = value;
            }

            internal Entity GetMetaChunkEntity(Entity entity)
            {
                return m_Manager.GetChunk(entity).m_Chunk.MetaChunkEntity;
            }

            internal Entity GetMetaChunkEntity(ArchetypeChunk chunk)
            {
                return chunk.m_Chunk.MetaChunkEntity;
            }

            /// <summary>
            /// Returns true if we are inside of a Entities.ForEach or IJobChunk.Run and thus can not allow structural changes at the same time.
            /// </summary>
            internal bool IsInForEachDisallowStructuralChange =>
                m_Manager.GetCheckedEntityDataAccess()->DependencyManager->ForEachStructuralChange.Depth != 0;

            internal int IsInForEachDisallowStructuralChangeCounter =>
                m_Manager.GetCheckedEntityDataAccess()->DependencyManager->ForEachStructuralChange.Depth;

            internal void SetIsInForEachDisallowStructuralChangeCounter(int counter)
            {
                var access = m_Manager.GetCheckedEntityDataAccess();
                access->DependencyManager->ForEachStructuralChange.SetIsInForEachDisallowStructuralChangeCounter(counter);
            }
            /// <summary>
            /// Returns the name used for the profiler marker of the passed system. This is useful for inspecting profiling data using the ProfilerRecorder API.
            /// </summary>
            /// <param name="world">The world to query.</param>
            /// <param name="system">The system within the world to query.</param>
            /// <returns>The marker name as a string</returns>
            public static string GetSystemProfilerMarkerName(World world, SystemHandle system)
            {
                var systemPtr = world.Unmanaged.ResolveSystemState(system);
                if (systemPtr == null)
                    return null;
                return systemPtr->GetProfilerMarkerName(world);
            }

            /// <summary>
            /// Debug logs information about a given entity including the entity's version, index, and archetype
            /// </summary>
            /// <param name="entity">The entity to log</param>
            public void LogEntityInfo(Entity entity) => Scripting.LowLevel.Debug.Log(GetEntityInfo(entity));

            internal string GetAllEntityInfo(bool includeComponentValues=false)
            {
                var str = new System.Text.StringBuilder();
                using (var arr = m_Manager.UniversalQueryWithSystems.ToEntityArray(Allocator.Persistent))
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        GetEntityInfo(arr[i], includeComponentValues, str);
                        str.AppendLine();
                    }
                }

                return str.ToString();
            }

            /// <summary>
            /// Creates a string with the information about a given entity including the entity's version, index, and archetype.
            /// </summary>
            /// <param name="entity">The entity to get the information about</param>
            /// <returns>The string with the entity's information</returns>
            public string GetEntityInfo(Entity entity)
            {
                var entityComponentStore = m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore;

                // Entity.Index is now a 28-bit masked property — it can never be negative.
                // Use Debugger_Exists (which calls EntityStore.Exists safely) to detect non-existent entities.
                if (!EntityComponentStore.Debugger_Exists(entityComponentStore, entity))
                {
                    return "Entity.Invalid";
                }
                var str = new System.Text.StringBuilder();
                GetEntityInfo(entity, false, str);
                return str.ToString();
            }

            /// <summary>
            /// Gets the name of the system that last modified the component type of the given chunk
            /// </summary>
            /// <param name="chunk">The chunk to check</param>
            /// <param name="componentType">The component type to check</param>
            /// <returns>The name of the system that modified it if found</returns>
            public static string GetLastWriterSystemName(ArchetypeChunk chunk, ComponentType componentType)
            {
                var archetype = chunk.Archetype.Archetype;
                var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex);
                if (typeIndexInArchetype == -1)
                    return $"'{componentType}' was not present on the chunk.";

                var changeVersion = archetype->Chunks.GetChangeVersion(typeIndexInArchetype, chunk.m_Chunk.ListIndex);
                var system = World.FindSystemStateForChangeVersion(chunk.m_EntityComponentStore, changeVersion);

                if (system == null)
                    return "Couldn't find the system that modified the chunk.";
                else
                    return system->DebugName.ToString();
            }

            internal void GetEntityInfo(Entity entity, bool includeComponentValues, System.Text.StringBuilder str)
            {
                var entityComponentStore = m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore;
                var archetype = entityComponentStore->GetArchetype(entity);
                str.Append(entity.ToString());
                {
                    var name = m_Manager.GetName(entity);
                    if (!string.IsNullOrEmpty(name))
                        str.Append($" (name '{name}')");
                }
                for (var i = 0; i < archetype->TypesCount; i++)
                {
                    var componentTypeInArchetype = archetype->Types[i];
                    str.AppendFormat("  - {0}", componentTypeInArchetype.ToString());
                }

                if (includeComponentValues)
                {
                    for (var i = 0; i < archetype->TypesCount; i++)
                    {
                        var componentType = archetype->Types[i].ToComponentType();
                        if (componentType.IsBuffer || componentType.IsZeroSized || TypeManager.GetTypeInfo(componentType.TypeIndex).Category == TypeManager.TypeCategory.EntityData)
                            continue;
                        var comp = GetComponentBoxed(entity, componentType);
                        if (CoreModuleLightShim.Hooks.UnityObjectType.IsInstanceOfType(comp))
                            continue;
                        str.AppendLine();
                        str.AppendLine(componentType.ToString());
                        var json = EditorLightShim.Hooks.ComponentToJson(comp);
                        str.AppendLine(json);
                    }
                }
            }

            /// <summary>
            /// Gets the component object of a given entity
            /// </summary>
            /// <param name="entity">The entity</param>
            /// <param name="type">The component type to get the object of</param>
            /// <returns>The component object</returns>
            /// <exception cref="ArgumentException">Throws if the type does not exist on the entity</exception>
            public object GetComponentBoxed(Entity entity, ComponentType type)
            {
                var access = m_Manager.GetCheckedEntityDataAccess();

                access->EntityComponentStore->AssertEntitiesExist(&entity, 1);

                if (!access->HasComponent(entity, type))
                    throw new ArgumentException($"Component of type {type} does not exist on the entity.");

                return new DebuggerDataAccess(access->EntityComponentStore).GetComponentBoxedUnchecked(entity, type);
            }

            /// <summary>
            /// Gets the component object of a given entity based on the type
            /// </summary>
            /// <param name="entity">The entity</param>
            /// <param name="type">The type to get the object of</param>
            /// <returns>The component object</returns>
            /// <exception cref="ArgumentException">Throws if the type does not exist on the entity</exception>
            public object GetComponentBoxed(Entity entity, Type type)
            {
                var access = m_Manager.GetCheckedEntityDataAccess();

                access->EntityComponentStore->AssertEntitiesExist(&entity, 1);

                var archetype = access->EntityComponentStore->GetArchetype(entity);
                var typeIndex = ChunkDataUtility.GetTypeIndexFromType(archetype, type);
                if (typeIndex == TypeIndex.Null)
                    throw new ArgumentException($"A component with type:{type} has not been added to the entity.");

                return new DebuggerDataAccess(access->EntityComponentStore).GetComponentBoxedUnchecked(entity, ComponentType.FromTypeIndex(typeIndex));
            }

            // The package's EntityManagerDebugExtensions turns these into the GetAuthoringObjectForEntity,
            // GetEntitiesForAuthoringObject and GetPrimaryEntityForAuthoringObject overloads that take Unity objects.
            internal EntityId GetAuthoringEntityId(Entity entity)
            {
                if (m_Manager.HasComponent<EntityGuid>(entity))
                    return m_Manager.GetComponentData<EntityGuid>(entity).OriginatingEntityId;

                return EntityId.None;
            }

            [BurstCompile]
            internal struct BuildInstanceIDToEntityIndex : IJobChunk
            {
                public UnsafeParallelMultiHashMap<EntityId, Entity>.ParallelWriter EntityLookup;
                [ReadOnly]
                public ComponentTypeHandle<EntityGuid>                GuidType;
                [ReadOnly]
                public EntityTypeHandle                               EntityType;

                public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
                {
                    Assert.IsFalse(useEnabledMask);
                    var entities = chunk.GetNativeArray(EntityType);
                    var guids = chunk.GetNativeArray(ref GuidType);

                    for (int i = 0; i != entities.Length; i++)
                        EntityLookup.Add(guids[i].OriginatingEntityId, entities[i]);
                }
            }

            internal void GetEntitiesForAuthoringEntityId(EntityId entityId, NativeList<Entity> entities)
            {
                var lookup = GetCachedEntityGUIDToEntityIndexLookup();

                entities.Clear();
                foreach (var e in lookup.GetValuesForKey(entityId))
                    entities.Add(e);
            }

            internal Entity GetPrimaryEntityForAuthoringEntityId(EntityId entityId)
            {
                var access = m_Manager.GetCheckedEntityDataAccess();
                UpdateCachedEntityGUIDToEntity(access);

                var lookup = access->CachedEntityGUIDToEntityIndex;

                foreach (var e in lookup.GetValuesForKey(entityId))
                {
                    var data = access->GetComponentData<EntityGuid>(e);
                    if (data.Serial == 0)
                        return e;
                }
                return default;
            }

            void UpdateCachedEntityGUIDToEntity(EntityDataAccess* access)
            {
                var newVersion = m_Manager.GetComponentOrderVersion<EntityGuid>();

                if (access->m_CachedEntityGUIDToEntityIndexVersion != newVersion)
                {
                    access->CachedEntityGUIDToEntityIndex.Clear();
                    var count = access->m_EntityGuidQuery.CalculateEntityCount();
                    if (access->CachedEntityGUIDToEntityIndex.Capacity < count)
                        access->CachedEntityGUIDToEntityIndex.Capacity = count;

                    new BuildInstanceIDToEntityIndex
                    {
                        EntityLookup = access->CachedEntityGUIDToEntityIndex.AsParallelWriter(),
                        GuidType = m_Manager.GetComponentTypeHandle<EntityGuid>(true),
                        EntityType = m_Manager.GetEntityTypeHandle()
                    }.ScheduleParallel(access->m_EntityGuidQuery, default).Complete();
                    access->m_CachedEntityGUIDToEntityIndexVersion = newVersion;
                }
            }

            internal UnsafeParallelMultiHashMap<EntityId, Entity> GetCachedEntityGUIDToEntityIndexLookup()
            {
                var access = m_Manager.GetCheckedEntityDataAccess();
                UpdateCachedEntityGUIDToEntity(access);
                return access->CachedEntityGUIDToEntityIndex;
            }

            /// <summary>
            /// Several checks to ensure that the <see cref="EntityComponentStore"/>
            /// has all references that are expected at this time as well as the expected number of entities.
            /// </summary>
            /// <remarks>
            /// These checks can not safely run while any jobs are running against this EntityManager's World. The caller
            /// should consider calling CompleteAllTrackedJobs() first to enforce this constraint.
            /// </remarks>
            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            public void CheckInternalConsistency()
            {
                // Note that this is awkwardly written to avoid all safety checks except "we were created".
                // This is so unit tests can run out of the test body with jobs running and exclusive transactions still opened.
                AtomicSafetyHandle.CheckExistsAndThrow(m_Manager.m_Safety);

                var eda = m_Manager.m_EntityDataAccess;

                //@TODO: Validate from perspective of chunkquery...
                if (false == eda->EntityComponentStore->IsIntentionallyInconsistent)
                    eda->EntityComponentStore->CheckInternalConsistency();

                Assert.IsTrue(eda->AllSharedComponentReferencesAreFromChunks(eda->EntityComponentStore), "Not all shared component references originate from chunks");

                var chunkHeaderType = new ComponentType(typeof(ChunkHeader));
                var chunkQuery = eda->EntityQueryManager->CreateEntityQuery(eda, &chunkHeaderType, 1);

                int totalEntitiesFromQuery = eda->m_UniversalQueryWithSystems.CalculateEntityCount() + chunkQuery.CalculateEntityCount();
                int expectedEntities = eda->EntityComponentStore->CountEntities();
                Assert.AreEqual(expectedEntities, totalEntitiesFromQuery, $"expected {expectedEntities} entities in EntityComponentStore, but queries match {totalEntitiesFromQuery}");

                chunkQuery.Dispose();
            }
        }

        internal Entity GetEntityByEntityIndex(int index)
        {
            return GetCheckedEntityDataAccess()->GetEntityByEntityIndex(index);
        }
    }
}
