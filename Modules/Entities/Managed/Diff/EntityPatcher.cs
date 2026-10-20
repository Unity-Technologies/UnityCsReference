// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    /// <summary>
    /// Utility class to apply a <see cref="EntityChangeSet"/> to an <see cref="EntityManager"/>
    /// </summary>
    [BurstCompile]
    public static unsafe partial class EntityPatcher
    {
        const string s_ApplyChangeSetProfilerMarkerStr = "EntityPatcher.ApplyChangeSet";

        static readonly EntitiesProfilerMarker s_ApplyChangeSetProfilerMarker = new EntitiesProfilerMarker(s_ApplyChangeSetProfilerMarkerStr);
        static readonly EntitiesProfilerMarker s_BuildEntityLookupsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.BuildEntityLookups");
        static readonly EntitiesProfilerMarker s_BuildPackedLookupsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.BuildPackedLookups");
        static readonly EntitiesProfilerMarker s_ApplyDestroyEntitiesProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyDestroyEntities");
        static readonly EntitiesProfilerMarker s_ApplyCreateEntitiesProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyCreateEntities");
        static readonly EntitiesProfilerMarker s_ApplyEntityNamesProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyEntityNames");
        static readonly EntitiesProfilerMarker s_ApplyRemoveComponentsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyRemoveComponents");
        static readonly EntitiesProfilerMarker s_ApplyAddComponentsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyAddComponents");
        static readonly EntitiesProfilerMarker s_ApplySetSharedComponentsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplySetSharedComponents");
        static readonly EntitiesProfilerMarker s_ApplySetComponentsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplySetComponents");
        static readonly EntitiesProfilerMarker s_BuildPrefabAndLinkedEntityGroupLookupsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.BuildPrefabAndLinkedEntityGroupLookups");
        static readonly EntitiesProfilerMarker s_ApplyLinkedEntityGroupRemovalsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyLinkedEntityGroupRemovals");
        static readonly EntitiesProfilerMarker s_ApplyLinkedEntityGroupAdditionsProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyLinkedEntityGroupAdditions");
        static readonly EntitiesProfilerMarker s_ApplyEntityPatchesProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyEntityPatches");
        static readonly EntitiesProfilerMarker s_ApplyBlobAssetChangesProfilerMarker = new EntitiesProfilerMarker("EntityPatcher.ApplyBlobAssetChanges");

        internal static string[] CollectImportantProfilerMarkerStrings()
        {
            return new string [] {
                s_ApplyChangeSetProfilerMarkerStr
            };
        }

        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // DOTSR-354
        [BurstCompile]
        internal struct BuildComponentToEntityMultiHashMap : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<EntityGuid> ComponentTypeHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            [WriteOnly] public NativeParallelMultiHashMap<EntityGuid, Entity>.ParallelWriter ComponentToEntity;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);
                var components = chunk.GetNativeArray(ref ComponentTypeHandle);
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                for (var i = 0; i != entities.Length; i++)
                {
                    ComponentToEntity.Add(components[i], entities[i]);
                }
            }
        }

        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // DOTSR-354
        [BurstCompile]
        internal struct BuildComponentToEntityHashMap : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<EntityGuid> ComponentTypeHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            [WriteOnly] public NativeParallelHashMap<EntityGuid, Entity>.ParallelWriter ComponentToEntity;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);
                var components = chunk.GetNativeArray(ref ComponentTypeHandle);
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                for (var i = 0; i != entities.Length; i++)
                {
                    ComponentToEntity.TryAdd(components[i], entities[i]);
                }
            }
        }

        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // DOTSR-354
        [BurstCompile]
        internal struct BuildEntityToComponentHashMap : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<EntityGuid> EntityGuidComponentTypeHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            [WriteOnly] public NativeParallelHashMap<Entity, EntityGuid>.ParallelWriter EntityToEntityGuid;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);
                var components = chunk.GetNativeArray(ref EntityGuidComponentTypeHandle);
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                for (var i = 0; i != entities.Length; i++)
                {
                    EntityToEntityGuid.TryAdd(entities[i], components[i]);
                }
            }
        }

        [BurstCompile]
        internal struct CalculateLinkedEntityGroupEntitiesLengthJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public int* Count;
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public BufferTypeHandle<LinkedEntityGroup> LinkedEntityGroupTypeHandle;

            public void Execute()
            {
                var count = 0;
                for (var chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
                {
                    var linkedEntityGroups = Chunks[chunkIndex].GetBufferAccessorRO(ref LinkedEntityGroupTypeHandle);
                    for (var linkedEntityGroupIndex = 0; linkedEntityGroupIndex < linkedEntityGroups.Length; linkedEntityGroupIndex++)
                    {
                        count += linkedEntityGroups[linkedEntityGroupIndex].Length;
                    }
                }

                *Count = count;
            }
        }

        [BurstCompile]
        internal struct BuildLinkedEntityGroupHashMap : IJobChunk
        {
            [WriteOnly] public NativeParallelHashMap<Entity, Entity>.ParallelWriter EntityToLinkedEntityGroupRoot;
            [ReadOnly] public BufferTypeHandle<LinkedEntityGroup> LinkedEntityGroupTypeHandle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Assert.IsFalse(useEnabledMask);

                var linkedEntityGroups = chunk.GetBufferAccessorRO(ref LinkedEntityGroupTypeHandle);

                for (var bufferIndex = 0; bufferIndex != linkedEntityGroups.Length; bufferIndex++)
                {
                    var linkedEntityGroup = linkedEntityGroups[bufferIndex];
                    for (var elementIndex = 0; elementIndex != linkedEntityGroup.Length; elementIndex++)
                    {
                        EntityToLinkedEntityGroupRoot.TryAdd(linkedEntityGroup[elementIndex].Value, linkedEntityGroup[0].Value);
                    }
                }
            }
        }

        [BurstCompile]
        internal struct BuildPackedEntityLookupJob : IJobParallelFor
        {
            public int StartIndex;
            [ReadOnly] public NativeArray<EntityGuid> EntityGuids;
            [ReadOnly] public NativeParallelMultiHashMap<EntityGuid, Entity> EntityGuidToEntity;
            [WriteOnly] public NativeParallelMultiHashMap<int, Entity>.ParallelWriter PackedEntities;

            public void Execute(int index)
            {
                var entityGuid = EntityGuids[index + StartIndex];
                if (EntityGuidToEntity.TryGetFirstValue(entityGuid, out var entity, out var iterator))
                {
                    do
                    {
                        PackedEntities.Add(index + StartIndex, entity);
                    }
                    while (EntityGuidToEntity.TryGetNextValue(out entity, ref iterator));
                }
            }
        }

        internal struct BuildPackedTypeLookupJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ComponentTypeHash> TypeHashes;
            [WriteOnly] public NativeArray<ComponentType> PackedTypes;

            public void Execute(int index)
            {
                var typeHash = TypeHashes[index];
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(typeHash.StableTypeHash);
                var type = TypeManager.GetType(typeIndex);
                ComponentType componentType;
                if ((typeHash.Flags & ComponentTypeFlags.ChunkComponent) == ComponentTypeFlags.ChunkComponent)
                {
                    componentType = ComponentType.ChunkComponent(type);
                }
                else
                {
                    componentType = new ComponentType(type);
                }
                PackedTypes[index] = componentType;
            }
        }

        /// <summary>
        /// Applies the given change set to the given entity manager.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> to apply the change set to.</param>
        /// <param name="changeSet">The <see cref="EntityChangeSet"/> to apply.</param>
        public static void ApplyChangeSet(EntityManager entityManager, EntityChangeSet changeSet)
        {
            if (!changeSet.IsCreated)
            {
                return;
            }

            s_ApplyChangeSetProfilerMarker.Begin();

            using var entityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EntityGuid>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
                .Build(entityManager);
            using var prefabQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EntityGuid,Prefab>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
                .Build(entityManager);
            using var linkedEntityGroupQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EntityGuid,LinkedEntityGroup>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab)
                .Build(entityManager);

            var entityCount = entityQuery.CalculateEntityCount();

            using (var packedEntities = new NativeParallelMultiHashMap<int, Entity>(entityCount, Allocator.TempJob))
            using (var packedTypes = new NativeArray<ComponentType>(changeSet.TypeHashes.Length, Allocator.TempJob))
            using (var entityGuidToEntity = new NativeParallelMultiHashMap<EntityGuid, Entity>(entityCount, Allocator.TempJob))
            using (var entityToEntityGuid = new NativeParallelHashMap<Entity, EntityGuid>(entityQuery.CalculateEntityCount(), Allocator.TempJob))
            {
                BuildEntityLookups(
                    entityManager,
                    entityQuery,
                    entityGuidToEntity,
                    entityToEntityGuid);

                BuildPackedLookups(
                    changeSet,
                    entityGuidToEntity,
                    packedEntities,
                    packedTypes);

                s_ApplyDestroyEntitiesProfilerMarker.Begin();
                ApplyDestroyEntities(
                    entityManager,
                    changeSet.Entities,
                    packedEntities,
                    entityGuidToEntity,
                    changeSet.DestroyedEntityCount);
                s_ApplyDestroyEntitiesProfilerMarker.End();

                s_ApplyCreateEntitiesProfilerMarker.Begin();
                ApplyCreateEntitiesWithArchetypes(
                    entityManager,
                    packedEntities,
                    changeSet.AddArchetypes);
                s_ApplyCreateEntitiesProfilerMarker.End();

                s_ApplyRemoveComponentsProfilerMarker.Begin();
                ApplyRemoveComponents(
                    entityManager,
                    changeSet.RemoveComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);
                s_ApplyRemoveComponentsProfilerMarker.End();

                s_ApplyAddComponentsProfilerMarker.Begin();
                ApplyAddComponents(
                    entityManager,
                    changeSet.AddComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);
                s_ApplyAddComponentsProfilerMarker.End();

                ApplySetSharedComponents(
                    entityManager,
                    changeSet.SetSharedComponents,
                    changeSet.UnmanagedSharedComponentData,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);

                ApplySetComponents(
                    entityManager,
                    changeSet.SetComponents,
                    changeSet.ComponentData,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes,
                    entityGuidToEntity,
                    entityToEntityGuid);

                s_ApplyEntityNamesProfilerMarker.Begin();
                ApplyEntityNames(
                    entityManager,
                    changeSet.Names,
                    changeSet.NameChangedEntityGuids,
                    packedEntities,
                    entityGuidToEntity,
                    changeSet.CreatedEntityCount,
                    changeSet.NameChangedCount);
                s_ApplyEntityNamesProfilerMarker.End();

                var linkedEntityGroupEntitiesLength = CalculateLinkedEntityGroupEntitiesLength(entityManager, linkedEntityGroupQuery);

                using (var entityGuidToPrefab = new NativeParallelHashMap<EntityGuid, Entity>(prefabQuery.CalculateEntityCount(), Allocator.TempJob))
                using (var entityToLinkedEntityGroupRoot = new NativeParallelHashMap<Entity, Entity>(linkedEntityGroupEntitiesLength, Allocator.TempJob))
                {
                    BuildPrefabAndLinkedEntityGroupLookups(
                        entityManager,
                        entityQuery,
                        prefabQuery,
                        linkedEntityGroupQuery,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);

                    s_ApplyLinkedEntityGroupRemovalsProfilerMarker.Begin();
                    ApplyLinkedEntityGroupRemovals(
                        entityManager,
                        changeSet.LinkedEntityGroupRemovals,
                        changeSet.Entities,
                        packedEntities,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityToLinkedEntityGroupRoot);
                    s_ApplyLinkedEntityGroupRemovalsProfilerMarker.End();

                    s_ApplyLinkedEntityGroupAdditionsProfilerMarker.Begin();
                    ApplyLinkedEntityGroupAdditions(
                        entityManager,
                        changeSet.LinkedEntityGroupAdditions,
                        changeSet.Entities,
                        packedEntities,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);
                    s_ApplyLinkedEntityGroupAdditionsProfilerMarker.End();

                    ApplyEntityPatches(
                        entityManager,
                        changeSet.EntityReferenceChanges,
                        changeSet.Entities,
                        packedEntities,
                        packedTypes,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);
                }

                ApplyBlobAssetChanges(
                    entityManager,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes,
                    changeSet.CreatedBlobAssets,
                    changeSet.BlobAssetData,
                    changeSet.DestroyedBlobAssets,
                    changeSet.BlobAssetReferenceChanges);
            }
            s_ApplyChangeSetProfilerMarker.End();
        }

        /// <summary>
        /// Builds a lookup of <see cref="NativeParallelMultiHashMap{TEntityGuidComponent, Entity}"/> for the target world.
        /// </summary>
        /// <remarks>
        /// This will run over ALL entities in the world. This is very expensive.
        /// </remarks>
        static void BuildEntityLookups(
            EntityManager entityManager,
            EntityQuery entityQuery,
            NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid)
        {
            s_BuildEntityLookupsProfilerMarker.Begin();
            var buildEntityGuidToEntity = new BuildComponentToEntityMultiHashMap
            {
                EntityTypeHandle = entityManager.GetEntityTypeHandle(),
                ComponentTypeHandle = entityManager.GetComponentTypeHandle<EntityGuid>(true),
                ComponentToEntity = entityGuidToEntity.AsParallelWriter()
            }.ScheduleParallel(entityQuery, default);

            var buildEntityToEntityGuid = new BuildEntityToComponentHashMap
            {
                EntityTypeHandle = entityManager.GetEntityTypeHandle(),
                EntityGuidComponentTypeHandle = entityManager.GetComponentTypeHandle<EntityGuid>(true),
                EntityToEntityGuid = entityToEntityGuid.AsParallelWriter()
            }.ScheduleParallel(entityQuery, default);

            JobHandle.CombineDependencies(buildEntityGuidToEntity, buildEntityToEntityGuid).Complete();
            s_BuildEntityLookupsProfilerMarker.End();
        }

        static void BuildPrefabAndLinkedEntityGroupLookups(
            EntityManager entityManager,
            EntityQuery entityQuery,
            EntityQuery prefabQuery,
            EntityQuery linkedEntityGroupQuery,
            NativeParallelHashMap<EntityGuid, Entity> entityGuidToPrefab,
            NativeParallelHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            s_BuildPrefabAndLinkedEntityGroupLookupsProfilerMarker.Begin();
            var buildPrefabLookups = new BuildComponentToEntityHashMap
            {
                EntityTypeHandle = entityManager.GetEntityTypeHandle(),
                ComponentTypeHandle = entityManager.GetComponentTypeHandle<EntityGuid>(true),
                ComponentToEntity = entityGuidToPrefab.AsParallelWriter()
            }.ScheduleParallel(prefabQuery, default);

            var buildLinkedEntityGroupLookups = new BuildLinkedEntityGroupHashMap
            {
                EntityToLinkedEntityGroupRoot = entityToLinkedEntityGroupRoot.AsParallelWriter(),
                LinkedEntityGroupTypeHandle = entityManager.GetBufferTypeHandle<LinkedEntityGroup>(true)
            }.ScheduleParallel(linkedEntityGroupQuery, default);

            JobHandle.CombineDependencies(buildPrefabLookups, buildLinkedEntityGroupLookups).Complete();
            s_BuildPrefabAndLinkedEntityGroupLookupsProfilerMarker.End();
        }

        /// <summary>
        /// This method will generate lookups into the packed change set.
        ///
        /// 1) Maps existing entities in the world to <see cref="EntityChangeSet.Entities"/>
        /// 2) Maps types in the world to <see cref="EntityChangeSet.TypeHashes"/>
        ///
        /// These tables are used by subsequent methods to quickly access the packed data.
        /// </summary>
        static void BuildPackedLookups(
            EntityChangeSet changeSet,
            NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            s_BuildPackedLookupsProfilerMarker.Begin();
            var buildPackedEntityLookups = new BuildPackedEntityLookupJob
            {
                StartIndex = changeSet.CreatedEntityCount,
                EntityGuids = changeSet.Entities,
                EntityGuidToEntity = entityGuidToEntity,
                PackedEntities = packedEntities.AsParallelWriter()
            }.Schedule(changeSet.Entities.Length - changeSet.CreatedEntityCount, 64);

            var buildPackedTypeLookups = new BuildPackedTypeLookupJob
            {
                TypeHashes = changeSet.TypeHashes,
                PackedTypes = packedTypes,
            }.Schedule(changeSet.TypeHashes.Length, 64);

            JobHandle.CombineDependencies(buildPackedEntityLookups, buildPackedTypeLookups).Complete();
            s_BuildPackedLookupsProfilerMarker.End();
        }

        /// <summary>
        /// Creates all new entities described in the <see cref="EntityChangeSet"/>
        /// </summary>
        /// <remarks>
        /// This method only creates the entities and does not set any data.
        /// </remarks>
        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyCreateEntitiesWithArchetypes(
            in EntityManager entityManager,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeArray<FilteredArchetype> createdArchetypes)
        {
            for (int i = 0; i < createdArchetypes.Length; i++)
            {
                var filteredArchetype = createdArchetypes[i];


                // Convert ComponentType Indices to ComponentTypes and create a ComponentTypeSet
                var typeSet = new NativeArray<ComponentType>(filteredArchetype.TypeIndices.Length, Allocator.Temp);
                for (int j = 0; j < typeSet.Length; j++)
                {
                    typeSet[j] = ComponentType.ReadOnly(filteredArchetype.TypeIndices[j]);
                }

                // Create the archetype
                var entityGuidArchetype = entityManager.CreateArchetypeWithoutSimulateComponent(typeSet);

                using (var entities = new NativeArray<Entity>(filteredArchetype.EntityCount, Allocator.Temp))
                {
                    entityManager.CreateEntity(entityGuidArchetype, entities);
                    for (var j = 0; j < filteredArchetype.EntityCount; ++j)
                    {
                        // PackedEntityIndices stores the indices of the Entities that have this archetype in the PackedTypes
                        // By adding the Entity to that index, it lines up with the components in PackedTypes used in ApplyAddComponents
                        packedEntities.Add(filteredArchetype.PackedEntityIndices[j], entities[j]);
                    }
                }
            }
        }

        [BurstCompile]
        internal struct ApplyNamesJob : IJob
        {
            public EntityManager EntityManager;
            public NativeArray<FixedString64Bytes> Names;
            public NativeArray<EntityGuid> NameChangeEntityGuids;
            public int NameChangedCount;
            public int CreatedEntityCount;
            public NativeParallelMultiHashMap<int, Entity> PackedEntities;
            public NativeParallelMultiHashMap<EntityGuid, Entity> EntityGuidToEntity;

            [BurstCompile]
            public void Execute() {

                var namesPtr = (FixedString64Bytes*) Names.GetUnsafeReadOnlyPtr();

                // Set names for created entities
                for (var i = 0; i < CreatedEntityCount; i++)
                {
                    var entityName = namesPtr[i];

                    // Created entity in PackedEntities
                    if (PackedEntities.TryGetFirstValue(i, out var entity, out _))
                    {
                        EntityManager.SetName(entity, entityName);
                    }
                }

                // Set names for name changed entities
                for (var i = 0; i < NameChangedCount; i++)
                {
                    var index = i + CreatedEntityCount;
                    var entityName = namesPtr[index];
                    var entityGuid = NameChangeEntityGuids[i];
                    if (EntityGuidToEntity.TryGetFirstValue(entityGuid, out var entity, out _))
                    {
                        EntityManager.SetName(entity, entityName);
                    }
                }
            }
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "UNITY_EDITOR && !DOTS_DISABLE_DEBUG_NAMES", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal static void ApplyEntityNames(
            EntityManager entityManager,
            NativeArray<FixedString64Bytes> names,
            NativeArray<EntityGuid> nameChangedEntityGuids,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            int createdEntityCount,
            int nameChangedCount)
        {
            // No name changes
            if (names.Length == 0)
            {
                return;
            }

            new ApplyNamesJob
            {
                EntityManager = entityManager,
                Names = names,
                NameChangeEntityGuids = nameChangedEntityGuids,
                CreatedEntityCount = createdEntityCount,
                NameChangedCount = nameChangedCount,
                PackedEntities = packedEntities,
                EntityGuidToEntity = entityGuidToEntity,
            }.Run();
        }

        /// <summary>
        /// Destroys all entities described in the <see cref="EntityChangeSet"/>
        /// </summary>
        /// <remarks>
        /// Since building the <see cref="NativeParallelMultiHashMap{TEntityGuidComponent, Entity}"/> the entire world is expensive
        /// this method will incrementally update the map based on the destroyed entities.
        /// </remarks>
        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyDestroyEntities(
            in EntityManager entityManager,
            in NativeArray<EntityGuid> entities,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            int destroyedEntityCount)
        {
            var linkedEntityGroupType = ComponentType.ReadOnly<LinkedEntityGroup>();
            for (var i = entities.Length - destroyedEntityCount; i < entities.Length; i++)
            {
                if (!packedEntities.TryGetFirstValue(i, out var entity, out var iterator))
                {
                    continue;
                }

                do
                {
                    // Perform incremental updates on the entityGuidToEntity map to avoid a full rebuild.
                    // @NOTE We do NOT remove from the `entityToEntityGuid` here since the LinkedEntityGroup removal will need it to map back groups.
                    entityGuidToEntity.Remove(entities[i], entity);

                    // It's possible that this entity has already been deleted. This can happen in two different scenarios:
                    //  - the change set is inconsistent with the state of the world. That means that the world has diverged.
                    //  - the entity was part of a LinkedEntityGroup that has already been destroyed earlier while applying this patch
                    if (entityManager.Exists(entity))
                    {
                        // We need to remove the linked entity group component before destroying the entity, because
                        // otherwise we cannot handle the case when an entity has its linked entity group modified and
                        // is then destroyed: The Differ won't pick up the changes to the linked entity group, which
                        // means that the patcher operates on a stale linked entity group and deletes too many entities.
                        entityManager.RemoveComponent(entity, linkedEntityGroupType);
                        entityManager.DestroyEntity(entity);
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        [BurstCompile]
        internal struct ApplyAddComponentsJob : IJobFor
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityComponentStore* store;
            [ReadOnly]
            public NativeArray<PackedComponent> AddComponents;
            [ReadOnly]
            public NativeArray<EntityGuid> PackedEntityGuids;
            [ReadOnly]
            public NativeParallelMultiHashMap<int, Entity> PackedEntities;
            [ReadOnly]
            public NativeArray<ComponentType> PackedTypes;
            public int LinkedEntityGroupTypeIndex;

            public NativeList<SetComponentError>.ParallelWriter ComponentAlreadyExists;

            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute(int index)
            {
                var packedComponent = AddComponents[index];

                if (!PackedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    return;
                }

                var component = PackedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (!store->HasComponent(entity, component.TypeIndex, out _))
                    {
                        ecb.AddComponent(index, entity, component);
                    }
                    else
                    {
                        ComponentAlreadyExists.AddNoResize(new SetComponentError
                        {
                            ComponentType = component, Guid = PackedEntityGuids[packedComponent.PackedEntityIndex]
                        });
                    }
                } while (PackedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplyAddComponents(
            in EntityManager entityManager,
            in NativeArray<PackedComponent> addComponents,
            in NativeArray<EntityGuid> packedEntityGuids,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeArray<ComponentType> packedTypes)
        {
            var componentAlreadyExists = new NativeList<SetComponentError>(addComponents.Length, Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new ApplyAddComponentsJob()
            {
                store = entityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                AddComponents = addComponents,
                PackedEntities = packedEntities,
                PackedEntityGuids = packedEntityGuids,
                PackedTypes = packedTypes,
                LinkedEntityGroupTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>(),
                ComponentAlreadyExists = componentAlreadyExists.AsParallelWriter(),
                ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(addComponents.Length, 32, default).Complete();

            for (int i = 0; i < componentAlreadyExists.Length; i++)
            {
                var error = componentAlreadyExists[i];
                Debug.LogWarning($"AddComponent({error.Guid}, {error.ComponentType}) but the component already exists.");
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
            componentAlreadyExists.Dispose();
        }

        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyRemoveComponents(
            in EntityManager entityManager,
            in NativeArray<PackedComponent> removeComponents,
            in NativeArray<EntityGuid> packedEntityGuids,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeArray<ComponentType> packedTypes)
        {
            var entityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>();

            for (var i = 0; i < removeComponents.Length; i++)
            {
                var packedComponent = removeComponents[i];

                if (!packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    continue;
                }

                var component = packedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (component.TypeIndex == entityGuidTypeIndex)
                    {
                        // @TODO Add test cases around this.
                        // Should entityGuidToEntity be updated or should we throw and error.
                    }

                    if (entityManager.HasComponent(entity, component))
                    {
                        entityManager.RemoveComponent(entity, component);
                    }
                    else
                    {
                        Debug.LogWarning($"RemoveComponent({packedEntityGuids[packedComponent.PackedEntityIndex]}, {component}) but the component was already removed.");
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

         internal struct UnmanagedPackedComponentData
         {
             public int offset;
             public PackedComponent packedComponent;
         }

        [BurstCompile]
        internal struct ApplySetSharedComponentsJob : IJobFor
        {
            [NativeDisableUnsafePtrRestriction]
            public EntityComponentStore* store;
            [ReadOnly]
            public NativeArray<UnmanagedPackedComponentData> UnmanagedPackedComponents;
            public UnsafeAppendBuffer UnmanagedSharedComponentData;
            [ReadOnly]
            public NativeArray<EntityGuid> PackedEntityGuids;
            [ReadOnly]
            public NativeParallelMultiHashMap<int, Entity> PackedEntities;
            [ReadOnly]
            public NativeArray<ComponentType> PackedTypes;

            public NativeList<SetComponentError>.ParallelWriter EntityDoesNotExist;
            public NativeList<SetComponentError>.ParallelWriter ComponentDoesNotExist;

            public EntityCommandBuffer.ParallelWriter ecb;

            public void Execute(int index)
            {
                var packedComponent = UnmanagedPackedComponents[index].packedComponent;

                if (!PackedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    return;
                }

                var component = PackedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (!store->HasComponent(entity, component, out var entityExists))
                    {
                        if (entityExists)
                        {
                            ComponentDoesNotExist.AddNoResize(new SetComponentError
                            {
                                ComponentType = component, Guid = PackedEntityGuids[packedComponent.PackedEntityIndex]
                            });
                        }
                        else
                        {
                            EntityDoesNotExist.AddNoResize(new SetComponentError
                            {
                                ComponentType = component, Guid = PackedEntityGuids[packedComponent.PackedEntityIndex]
                            });
                        }
                    }
                    else
                    {
                        var offset = UnmanagedPackedComponents[index].offset;

                        var componentDataAddr = offset != -1
                            ? UnmanagedSharedComponentData.Ptr + offset
                            : null;

                        ecb.UnsafeSetSharedComponentNonDefault(0, entity, componentDataAddr, component.TypeIndex);
                    }
                } while (PackedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplySetSharedComponents(
            EntityManager entityManager,
            PackedSharedComponentDataChange[] sharedComponentDataChanges,
            UnsafeAppendBuffer unmanagedSharedComponentData,
            NativeArray<EntityGuid> packedEntityGuid,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            s_ApplySetSharedComponentsProfilerMarker.Begin();

            NativeArray<UnmanagedPackedComponentData> unmanagedPackedComponents = new NativeArray<UnmanagedPackedComponentData>(sharedComponentDataChanges.Length, Allocator.TempJob);

            for (int i = 0; i < sharedComponentDataChanges.Length; i++)
            {
                var packedSharedComponentDataChange = sharedComponentDataChanges[i];
                var offset = packedSharedComponentDataChange.UnmanagedSharedValueDataOffset;
                var packedComponent = packedSharedComponentDataChange.Component;
                unmanagedPackedComponents[i] = new UnmanagedPackedComponentData(){offset = offset, packedComponent = packedComponent};
            }

            var entityDoesNotExist = new NativeList<SetComponentError>(sharedComponentDataChanges.Length, Allocator.TempJob);
            var componentDoesNotExist = new NativeList<SetComponentError>(sharedComponentDataChanges.Length, Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new ApplySetSharedComponentsJob()
            {
                store = entityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                UnmanagedPackedComponents = unmanagedPackedComponents,
                UnmanagedSharedComponentData = unmanagedSharedComponentData,
                PackedEntities = packedEntities,
                PackedEntityGuids = packedEntityGuid,
                PackedTypes = packedTypes,
                ComponentDoesNotExist = componentDoesNotExist.AsParallelWriter(),
                EntityDoesNotExist = entityDoesNotExist.AsParallelWriter(),
                ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(unmanagedPackedComponents.Length, 64, default).Complete();
            unmanagedPackedComponents.Dispose();

            for (int i = 0; i < entityDoesNotExist.Length; i++)
            {
                var error = entityDoesNotExist[i];
                Debug.LogWarning($"SetComponent<{error.ComponentType}>({error.Guid}) but entity does not exist.");
            }
            for (int i = 0; i < componentDoesNotExist.Length; i++)
            {
                var error = componentDoesNotExist[i];
                Debug.LogWarning($"SetComponent<{error.ComponentType}>({error.Guid}) but component does not exist.");
            }

            ecb.Playback(entityManager);
            ecb.Dispose();

            entityDoesNotExist.Dispose();
            componentDoesNotExist.Dispose();

            s_ApplySetSharedComponentsProfilerMarker.End();
        }

        internal struct SetComponentError
        {
            public ComponentType ComponentType;
            public EntityGuid Guid;
        }

        [BurstCompile]
        internal struct ApplySetComponentsJob : IJob
        {
            public EntityManager EntityManager;
            [ReadOnly]
            public NativeArray<PackedComponentDataChange> Changes;
            [ReadOnly]
            public NativeArray<byte> Payload;
            [ReadOnly]
            public NativeArray<EntityGuid> PackedEntityGuids;
            [ReadOnly]
            public NativeParallelMultiHashMap<int, Entity> PackedEntities;
            [ReadOnly]
            public NativeArray<ComponentType> PackedTypes;

            public NativeParallelMultiHashMap<EntityGuid, Entity> EntityGuidToEntity;
            public NativeParallelHashMap<Entity, EntityGuid> EntityToEntityGuid;

            public NativeList<SetComponentError> EntityDoesNotExist;
            public NativeList<SetComponentError> ComponentDoesNotExist;

            public NativeList<Entity> EntitiesWithCompanionLink;

            public void Execute()
            {
                var entityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>();
                var companionLinkTypeIndex = CompanionComponentStore.CompanionLinkTypeIndex;

                var offset = 0L;
                for (var i = 0; i < Changes.Length; i++)
                {
                    var packedComponentDataChange = Changes[i];
                    var packedComponent = packedComponentDataChange.Component;
                    var component = PackedTypes[packedComponent.PackedTypeIndex];
                    var size = packedComponentDataChange.Size;
                    var data = (byte*)Payload.GetUnsafeReadOnlyPtr() + offset;
                    var componentTypeInArchetype = new ComponentTypeInArchetype(component);

                    if (PackedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                    {
                        do
                        {
                            if (!EntityManager.Exists(entity))
                            {
                                EntityDoesNotExist.Add(new SetComponentError
                                {
                                    ComponentType = component, Guid =PackedEntityGuids[packedComponent.PackedEntityIndex]
                                });
                            }
                            else if (!EntityManager.HasComponent(entity, component))
                            {
                                ComponentDoesNotExist.Add(new SetComponentError
                                {
                                    ComponentType = component, Guid =PackedEntityGuids[packedComponent.PackedEntityIndex]
                                });
                            }
                            else
                            {
                                // Both IComponentData and IBufferElement can be enableable (as well as empty)
                                // if the enabled value is changed since last frame (-1 == unchanged, 0 == changed to false, 1 == changed to true)
                                if (packedComponentDataChange.Enabled > -1)
                                {
                                    EntityManager.SetEnableableComponent(entity, component.TypeIndex,
                                        packedComponentDataChange.Enabled == 1);
                                }

                                if (componentTypeInArchetype.IsZeroSized)
                                {
                                    // Nothing to set.
                                }
                                else if (componentTypeInArchetype.IsBuffer)
                                {
                                    ref readonly var typeInfo = ref TypeManager.GetTypeInfo(componentTypeInArchetype.TypeIndex);
                                    var elementSize = typeInfo.ElementSize;
                                    var lengthInElements = size / elementSize;
                                    var header = (BufferHeader*)EntityManager.GetComponentDataRawRW(entity, component.TypeIndex);
                                    BufferHeader.Assign(header, data, lengthInElements, elementSize, 16, false, 0);
                                }
                                else
                                {
                                    var target = (byte*)EntityManager.GetComponentDataRawRW(entity, component.TypeIndex);

                                    // Perform incremental updates on the entityGuidToEntity map to avoid a full rebuild.
                                    if (componentTypeInArchetype.TypeIndex == entityGuidTypeIndex)
                                    {
                                        EntityGuid entityGuid;
                                        UnsafeUtility.MemCpy(&entityGuid, target, sizeof(EntityGuid));

                                        if (!entityGuid.Equals(default))
                                        {
                                            EntityGuidToEntity.Remove(entityGuid, entity);
                                        }

                                        UnsafeUtility.MemCpy(&entityGuid, data + packedComponentDataChange.Offset, size);
                                        EntityGuidToEntity.Add(entityGuid, entity);
                                        EntityToEntityGuid.TryAdd(entity, entityGuid);
                                    }

                                    // Collect all entities that have a companion link so we can fix up later
                                    if (componentTypeInArchetype.TypeIndex == companionLinkTypeIndex)
                                    {
                                        EntitiesWithCompanionLink.Add(entity);
                                    }

                                    UnsafeUtility.MemCpy(target + packedComponentDataChange.Offset, data, size);
                                }
                            }
                        }
                        while (PackedEntities.TryGetNextValue(out entity, ref iterator));
                    }

                    offset += size;
                }
            }
        }

        static void ApplySetComponents(
            EntityManager entityManager,
            NativeArray<PackedComponentDataChange> changes,
            NativeArray<byte> payload,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes,
            NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid)
        {
            entityManager.BeforeStructuralChange();

            s_ApplySetComponentsProfilerMarker.Begin();
            var entityDoesNotExist = new NativeList<SetComponentError>(Allocator.TempJob);
            var componentDoesNotExist = new NativeList<SetComponentError>(Allocator.TempJob);

            // Companions
            var entitiesWithCompanionLink = new NativeList<Entity>(Allocator.TempJob);

            new ApplySetComponentsJob
            {
                Changes = changes,
                EntitiesWithCompanionLink = entitiesWithCompanionLink,
                ComponentDoesNotExist = componentDoesNotExist,
                EntityDoesNotExist = entityDoesNotExist,
                EntityGuidToEntity = entityGuidToEntity,
                EntityManager = entityManager,
                EntityToEntityGuid = entityToEntityGuid,
                PackedEntities = packedEntities,
                PackedEntityGuids = packedEntityGuids,
                PackedTypes = packedTypes,
                Payload = payload
            }.Run();

            for (int i = 0; i < entityDoesNotExist.Length; i++)
            {
                var error = entityDoesNotExist[i];
                Debug.LogWarning($"SetComponent<{error.ComponentType}>({error.Guid}) but entity does not exist.");
            }
            for (int i = 0; i < componentDoesNotExist.Length; i++)
            {
                var error = componentDoesNotExist[i];
                Debug.LogWarning($"SetComponent<{error.ComponentType}>({error.Guid}) but component does not exist.");
            }

            var entitiesWithCompanionLinkCount = entitiesWithCompanionLink.Length;
            if (entitiesWithCompanionLinkCount > 0)
            {
                CompanionComponentStore.AssignCompanionComponentsToCompanionGameObjects(entityManager, entitiesWithCompanionLink.AsArray());
            }

            entityDoesNotExist.Dispose();
            componentDoesNotExist.Dispose();
            entitiesWithCompanionLink.Dispose();
            s_ApplySetComponentsProfilerMarker.End();
        }

        internal struct SharedComponentBlobAssetReferencePatch
        {
            public int Id;
            public ulong Target;
        }

        internal struct EntityComponentPair : IEquatable<EntityComponentPair>, IComparable<EntityComponentPair>
        {
            public Entity Entity;
            public ComponentType Component;

            public bool Equals(EntityComponentPair other)
            {
                return Entity == other.Entity && Component == other.Component;
            }

            public int CompareTo(EntityComponentPair other)
            {
                var result = Entity.CompareTo(other.Entity);
                if (result != 0) return result;
                result = Component.TypeIndex.CompareTo(other.Component.TypeIndex);
                return result;
            }
        }

        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyUnmanagedEntityPatches(
            in EntityManager entityManager,
            in NativeArray<EntityReferenceChange> changes,
            in NativeArray<EntityGuid> packedEntityGuids,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeArray<ComponentType> packedTypes,
            in NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            in NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid,
            in NativeParallelHashMap<EntityGuid, Entity> entityGuidToPrefab,
            in NativeParallelHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            for (var i = 0; i < changes.Length; i++)
            {
                var patch = changes[i];
                var packedComponent = patch.Component;
                var component = packedTypes[packedComponent.PackedTypeIndex];
                var targetEntityGuid = patch.Value;
                var targetOffset = patch.Offset;
                var multipleTargetEntities = false;
                Entity targetEntity;

                if (targetEntityGuid.Equals(default))
                {
                    targetEntity = Entity.Null;
                }
                else
                {
                    if (!entityGuidToEntity.TryGetFirstValue(targetEntityGuid, out targetEntity, out var patchSourceIterator))
                    {
                        Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity with guid-to-patch-to does not exist.");
                        continue;
                    }
                    multipleTargetEntities = entityGuidToEntity.TryGetNextValue(out _, ref patchSourceIterator);
                }

                if (packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    do
                    {
                        if (!entityManager.Exists(entity))
                        {
                            Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity to patch does not exist.");
                        }
                        else if (!entityManager.HasComponent(entity, component))
                        {
                            Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but component in entity to patch does not exist.");
                        }
                        else
                        {
                            // If just one entity has the GUID we're patching to, we can just use that entity.
                            // but if multiple entities have that GUID, we need to patch to the (one) entity that's in the destination entity's "group."
                            // that group is defined by a LinkedEntityGroup component on the destination entity's "root entity," which contains an array of entity references.
                            // the destination entity's "root entity" is defined by whatever entity owns the (one) LinkedEntityGroup that refers to the destination entity.
                            // so, we had to build a lookup table earlier, to take us from "destination entity" to "root entity of my group," so we can find this LinkedEntityGroup
                            // component, and riffle through it to find the (one) entity with the GUID we're looking for.
                            if (multipleTargetEntities)
                            {
                                targetEntity = Entity.Null;

                                if (entityToLinkedEntityGroupRoot.TryGetValue(entity, out var linkedEntityGroupRoot))
                                {
                                    // This entity is part of a LinkedEntityGroup
                                    var legEntities = entityManager.GetBuffer<LinkedEntityGroup>(linkedEntityGroupRoot).Reinterpret<Entity>();

                                    // Scan through the group and look for the entity with the target entityGuid.
                                    foreach(var legEntity in legEntities)
                                    {
                                        // Get the entityGuid from each element.
                                        if (entityToEntityGuid.TryGetValue(legEntity, out var entityGuidInGroup))
                                        {
                                            if (entityGuidInGroup.Equals(targetEntityGuid))
                                            {
                                                // Match found this is our entity
                                                targetEntity = legEntity;
                                                break;
                                            }
                                        }
                                    }
                                }

                                // We are not dealing with a LinkedEntityGroup at this point, let's hope it's a prefab.
                                if (targetEntity == Entity.Null && !entityGuidToPrefab.TryGetValue(targetEntityGuid, out targetEntity))
                                {
                                    Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but 2+ entities for GUID of entity-to-patch-to, and no root for entity-to-patch is, so we can't disambiguate.");
                                    continue;
                                }
                            }

                            if (component.IsBuffer)
                            {
                                var pointer = (byte*)entityManager.GetBufferRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetEntity, sizeof(Entity));
                            }
                            else
                            {
                                var pointer = (byte*)entityManager.GetComponentDataRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetEntity, sizeof(Entity));
                            }
                        }
                    }
                    while (packedEntities.TryGetNextValue(out entity, ref iterator));
                }
            }
        }

        static void ApplyEntityPatches(
            EntityManager entityManager,
            NativeArray<EntityReferenceChange> changes,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes,
            NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid,
            NativeParallelHashMap<EntityGuid, Entity> entityGuidToPrefab,
            NativeParallelHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            s_ApplyEntityPatchesProfilerMarker.Begin();
            if(changes.Length == 0)
            {
                s_ApplyEntityPatchesProfilerMarker.End();
                return;
            }

            ApplyUnmanagedEntityPatches(
                        entityManager,
                        changes,
                        packedEntityGuids,
                        packedEntities,
                        packedTypes,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);

            s_ApplyEntityPatchesProfilerMarker.End();
        }

        struct Child
        {
            public Entity RootEntity;
            public Entity ChildEntity;
            public EntityGuid ChildEntityGuid;
        }

        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyLinkedEntityGroupAdditions(
            in EntityManager entityManager,
            in NativeArray<LinkedEntityGroupChange> linkedEntityGroupChanges,
            in NativeArray<EntityGuid> packedEntityGuids,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            in NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid,
            in NativeParallelHashMap<EntityGuid, Entity> entityGuidToPrefab,
            in NativeParallelHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            using (var additions = new NativeList<Child>(Allocator.TempJob))
            {
                for (var i = 0; i < linkedEntityGroupChanges.Length; i++)
                {
                    var linkedEntityGroupAddition = linkedEntityGroupChanges[i];

                    // If we are asked to add a child to a linked entity group, then that child's guid must correspond to
                    // exactly one entity in the destination world that also has a Prefab component. Since we made a lookup
                    // from EntityGuid to Prefab entity before, we can use it to find the specific entity we want.
                    if (entityGuidToPrefab.TryGetValue(linkedEntityGroupAddition.ChildEntityGuid, out var prefabEntityToInstantiate))
                    {
                        if (entityGuidToEntity.TryGetFirstValue(linkedEntityGroupAddition.RootEntityGuid, out var rootEntity, out var iterator))
                        {
                            do
                            {
                                if (entityManager.HasComponent<Prefab>(rootEntity))
                                {
                                    entityManager.GetBuffer<LinkedEntityGroup>(rootEntity).Add(prefabEntityToInstantiate);
                                    entityToLinkedEntityGroupRoot.TryAdd(prefabEntityToInstantiate, rootEntity);
                                }
                                else
                                {
                                    var instantiatedEntity = entityManager.Instantiate(prefabEntityToInstantiate);
                                    var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(rootEntity);
                                    linkedEntityGroup.Add(instantiatedEntity);

                                    additions.Add(new Child
                                    {
                                        RootEntity = rootEntity,
                                        ChildEntity = instantiatedEntity,
                                        ChildEntityGuid = linkedEntityGroupAddition.ChildEntityGuid
                                    });
                                }
                            }
                            while (entityGuidToEntity.TryGetNextValue(out rootEntity, ref iterator));
                        }
                        else
                        {
                            Debug.LogWarning($"Tried to add a child to a linked entity group, but root entity didn't exist in destination world.");
                        }
                    }
                    else
                    {
                        // At this point we are dealing with a non-prefab linked entity group
                        // This can happen for disabled entities during conversion.

                        var hasRootEntity = entityGuidToEntity.TryGetFirstValue(linkedEntityGroupAddition.RootEntityGuid, out var rootEntity, out var rootIterator);
                        var hasChildEntity = entityGuidToEntity.TryGetFirstValue(linkedEntityGroupAddition.ChildEntityGuid, out var childEntityToLink, out var childIterator);

                        if (!hasRootEntity)
                        {
                            Debug.LogWarning("Failed to add a linked child. The specified root entity was not found in the destination world.");
                            continue;
                        }

                        if (!hasChildEntity)
                        {
                            Debug.LogWarning("Failed to add a linked child. The specified child entity was not found in the destination world.");
                            continue;
                        }

                        var multipleRootEntities = entityGuidToEntity.TryGetNextValue(out _, ref rootIterator);
                        var multipleChildEntities = entityGuidToEntity.TryGetNextValue(out _, ref childIterator);

                        if (multipleRootEntities)
                        {
                            Debug.LogWarning("Failed to add a linked child. Multiple instances of the root entity were found in the destination world.");
                            continue;
                        }

                        if (multipleChildEntities)
                        {
                            Debug.LogWarning("Failed to add a linked child. Multiple instances of the child entity were found in the destination world.");
                            continue;
                        }
                        entityManager.GetBuffer<LinkedEntityGroup>(rootEntity).Add(childEntityToLink);
                        entityToLinkedEntityGroupRoot.TryAdd(childEntityToLink, rootEntity);
                    }
                }

                for (var i = 0; i < additions.Length; i++)
                {
                    var addition = additions[i];
                    for (var packedEntityGuidIndex = 0; packedEntityGuidIndex < packedEntityGuids.Length; ++packedEntityGuidIndex)
                    {
                        if (!packedEntityGuids[packedEntityGuidIndex].Equals(addition.ChildEntityGuid))
                        {
                            continue;
                        }

                        packedEntities.Add(packedEntityGuidIndex, addition.ChildEntity);
                        break;
                    }

                    entityToEntityGuid.TryAdd(addition.ChildEntity, addition.ChildEntityGuid);
                    entityGuidToEntity.Add(addition.ChildEntityGuid, addition.ChildEntity);
                    entityToLinkedEntityGroupRoot.TryAdd(addition.ChildEntity, addition.RootEntity);
                }
            }
        }

        [GenerateTestsForBurstCompatibility]
        [BurstCompile]
        internal static void ApplyLinkedEntityGroupRemovals(
            in EntityManager entityManager,
            in NativeArray<LinkedEntityGroupChange> linkedEntityGroupChanges,
            in NativeArray<EntityGuid> packedEntityGuids,
            in NativeParallelMultiHashMap<int, Entity> packedEntities,
            in NativeParallelMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            in NativeParallelHashMap<Entity, EntityGuid> entityToEntityGuid,
            in NativeParallelHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            using (var removals = new NativeList<Child>(Allocator.TempJob))
            {
                for (var i = 0; i < linkedEntityGroupChanges.Length; ++i)
                {
                    var linkedEntityGroupRemoval = linkedEntityGroupChanges[i];
                    if (entityGuidToEntity.TryGetFirstValue(linkedEntityGroupRemoval.RootEntityGuid, out var rootEntity, out var iterator))
                    {
                        do
                        {
                            var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(rootEntity);

                            // Look for the remove child in the LinkedEntityGroupBuffer
                            for (var bufferIndex = 0; bufferIndex < linkedEntityGroup.Length; bufferIndex++)
                            {
                                var childEntity = linkedEntityGroup[bufferIndex].Value;

                                if (entityToEntityGuid.TryGetValue(childEntity, out var childEntityGuid) &&
                                    childEntityGuid.Equals(linkedEntityGroupRemoval.ChildEntityGuid))
                                {
                                    // This entity does not exist. It was most likely destroyed.
                                    // Remove it from the LinkedEntityGroup
                                    linkedEntityGroup.RemoveAt(bufferIndex);

                                    removals.Add(new Child
                                    {
                                        RootEntity = rootEntity,
                                        ChildEntity = childEntity,
                                        ChildEntityGuid = linkedEntityGroupRemoval.ChildEntityGuid,
                                    });
                                    break;
                                }
                            }

                            // if we got here without destroying an entity, then maybe the destination world destroyed it before we synced?
                            // not sure if that is a fatal error, or what.
                        }
                        while (entityGuidToEntity.TryGetNextValue(out rootEntity, ref iterator));
                    }
                }

                for (var i = 0; i < removals.Length; ++i)
                {
                    entityToLinkedEntityGroupRoot.Remove(removals[i].ChildEntity);
                }
            }
        }

        static int CalculateLinkedEntityGroupEntitiesLength(EntityManager entityManager, EntityQuery linkedEntityGroupQuery)
        {
            var count = 0;

            using (var chunks = linkedEntityGroupQuery.ToArchetypeChunkArray(Allocator.TempJob))
            {
                new CalculateLinkedEntityGroupEntitiesLengthJob
                {
                    Count = &count,
                    Chunks = chunks,
                    LinkedEntityGroupTypeHandle = entityManager.GetBufferTypeHandle<LinkedEntityGroup>(true)
                }.Schedule().Complete();
            }

            return count;
        }
    }
}
