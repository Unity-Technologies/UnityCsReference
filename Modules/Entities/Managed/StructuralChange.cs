// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [BurstCompile]
    static unsafe class StructuralChange
    {
        [BurstCompile]
        public static void AddComponentEntitiesBatch(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList, TypeIndex typeIndex)
        {
            entityComponentStore->AddComponent(entityBatchList, ComponentType.FromTypeIndex(typeIndex), 0);
        }

        [BurstCompile]
        public static void AddComponentsEntitiesBatch(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList, in ComponentTypeSet typeSet)
        {
            entityComponentStore->AddComponents(entityBatchList, typeSet);
        }

        [BurstCompile]
        public static bool AddComponentEntity(EntityComponentStore* entityComponentStore, Entity* entity, TypeIndex typeIndex)
        {
            return entityComponentStore->AddComponent(*entity, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static bool AddComponentEntityWithValue(EntityComponentStore* entityComponentStore, Entity* entity, TypeIndex typeIndex,
            byte* componentValue, int componentSize)
        {
            return entityComponentStore->AddComponent(*entity, ComponentType.FromTypeIndex(typeIndex), componentValue, componentSize);
        }

        [BurstCompile]
        public static void AddComponentEntitiesBatchWithValue(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList,
            TypeIndex typeIndex, byte* componentValue, int componentSize)
        {
            entityComponentStore->AddComponent(entityBatchList, ComponentType.FromTypeIndex(typeIndex), 0, componentValue, componentSize);
        }

        [BurstCompile]
        public static void AddComponentQueryWithDeferredOnAddedCallbacks(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl,
            TypeIndex typeIndex, UnsafeList<EntityBatchInChunk>* deferredOnAddedRanges)
        {
            var value = new EntityComponentStore.AddedComponentValue { TypeIndex = typeIndex, DeferredOnAddedRanges = deferredOnAddedRanges };
            entityComponentStore->AddComponent(queryImpl, ComponentType.FromTypeIndex(typeIndex), 0, value);
        }

        [BurstCompile]
        public static void AddComponentsEntity(EntityComponentStore* entityComponentStore, Entity* entity, in ComponentTypeSet typeSet)
        {
            entityComponentStore->AddComponent(*entity, typeSet);
        }

        [BurstCompile]
        public static void AddComponentChunks(EntityComponentStore* entityComponentStore, ArchetypeChunk* chunks, int chunkCount, TypeIndex typeIndex)
        {
            entityComponentStore->AddComponent(chunks, chunkCount, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void AddComponentQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl *queryImpl, TypeIndex typeIndex)
        {
            entityComponentStore->AddComponent(queryImpl, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void AddComponentsQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl, in ComponentTypeSet typeSet)
        {
            entityComponentStore->AddComponents(queryImpl, typeSet);
        }

        [BurstCompile]
        public static bool RemoveComponentEntity(EntityComponentStore* entityComponentStore, Entity* entity, TypeIndex typeIndex)
        {
            return entityComponentStore->RemoveComponent(*entity, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void RemoveComponentsEntity(EntityComponentStore* entityComponentStore, Entity* entity, in ComponentTypeSet typeSet)
        {
            entityComponentStore->RemoveComponent(*entity, typeSet);
        }

        [BurstCompile]
        public static void RemoveComponentEntitiesBatch(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList, TypeIndex typeIndex)
        {
            entityComponentStore->RemoveComponent(entityBatchList, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void RemoveComponentsEntitiesBatch(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList, in ComponentTypeSet typeSet)
        {
            entityComponentStore->RemoveComponents(entityBatchList, typeSet);
        }

        [BurstCompile]
        public static void RemoveComponentChunks(EntityComponentStore* entityComponentStore, ArchetypeChunk* chunks, int chunkCount, TypeIndex typeIndex)
        {
            entityComponentStore->RemoveComponent(chunks, chunkCount, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void RemoveComponentQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl, TypeIndex typeIndex)
        {
            entityComponentStore->RemoveComponent(queryImpl, ComponentType.FromTypeIndex(typeIndex));
        }

        [BurstCompile]
        public static void RemoveComponentsQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl, in ComponentTypeSet typeSet)
        {
            entityComponentStore->RemoveComponents(queryImpl, typeSet);
        }

        [BurstCompile]
        public static void AddSharedComponentDataIndexWithBurst(EntityComponentStore* entityComponentStore, Entity* entities, int entityCount,
            in ComponentType componentType, int newSharedComponentDataIndex)
        {
            for (int i = 0; i < entityCount; ++i)
            {
                entityComponentStore->AddComponent(entities[i], componentType);
                entityComponentStore->SetSharedComponentDataIndex(entities[i], componentType, newSharedComponentDataIndex);
            }
        }

        [BurstCompile]
        public static void SetSharedComponentDataIndexWithBurst(EntityComponentStore* entityComponentStore, Entity* entities, int entityCount,
            in ComponentType componentType, int newSharedComponentDataIndex)
        {
            for (int i = 0; i < entityCount; ++i)
            {
                entityComponentStore->SetSharedComponentDataIndex(entities[i], componentType, newSharedComponentDataIndex);
            }
        }

        [BurstCompile]
        public static void SetSharedComponentDataIndexWithBurst(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl,
            in ComponentType componentType, int newSharedComponentDataIndex)
        {
            entityComponentStore->SetSharedComponentDataIndex(queryImpl, componentType, newSharedComponentDataIndex);
        }

        [BurstCompile]
        public static void AddSharedComponentChunks(EntityComponentStore* entityComponentStore, ArchetypeChunk* chunks, int chunkCount, TypeIndex componentTypeIndex, int sharedComponentIndex)
        {
            entityComponentStore->AddComponent(chunks, chunkCount, ComponentType.FromTypeIndex(componentTypeIndex), sharedComponentIndex);
        }

        [BurstCompile]
        public static void AddSharedComponentQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl, TypeIndex componentTypeIndex, int sharedComponentIndex)
        {
            entityComponentStore->AddComponent(queryImpl, ComponentType.FromTypeIndex(componentTypeIndex), sharedComponentIndex);
        }

        [BurstCompile]
        public static void MoveEntityArchetype(EntityComponentStore* entityComponentStore, Entity* entity, void* dstArchetype)
        {
            entityComponentStore->Move(*entity, (Archetype*)dstArchetype);
        }

        [BurstCompile]
        public static void MoveEntityArchetypeBatch(EntityComponentStore* entityComponentStore, UnsafeList<EntityBatchInChunk>* entityBatchList, void* dstArchetype)
        {
            entityComponentStore->MoveArchetype(entityBatchList, (Archetype*)dstArchetype);
        }

        [BurstCompile]
        public static void SetChunkComponent(EntityComponentStore* entityComponentStore, ArchetypeChunk* chunks, int chunkCount, void* componentData, TypeIndex componentTypeIndex)
        {
            entityComponentStore->SetChunkComponent(chunks, chunkCount, componentData, componentTypeIndex);
        }

        [BurstCompile]
        public static void CreateEntity(EntityComponentStore* entityComponentStore, void* archetype, Entity* outEntities, int count)
        {
            entityComponentStore->CreateEntities((Archetype*)archetype, outEntities, count);
        }

        [BurstCompile]
        public static void AllocateAndAssignChunksToExistingEntities(EntityComponentStore* entityComponentStore, void* archetype, Entity* existingEntities, int count)
        {
            entityComponentStore->AllocateAndAssignChunksToExistingEntities((Archetype*)archetype, existingEntities, count);
        }

        [BurstCompile]
        public static void DeallocateAndUnAssignChunksToExistingEntities(EntityComponentStore* entityComponentStore, Entity* entities, int count)
        {
            entityComponentStore->DestroyEntities(entities, count);
        }

        [BurstCompile]
        public static void DestroyEntity(EntityComponentStore* entityComponentStore, Entity* entities, int count)
        {
            entityComponentStore->DestroyEntities(entities, count);
        }

        [BurstCompile]
        public static void DestroyChunksQuery(EntityComponentStore* entityComponentStore, EntityQueryImpl* queryImpl,
            ref BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupTypeHandle)
        {
            entityComponentStore->DestroyEntities(queryImpl, ref linkedEntityGroupTypeHandle);
        }

        [BurstCompile]
        public static void InstantiateEntity(EntityComponentStore* entityComponentStore, Entity* srcEntity, Entity* outputEntities, int instanceCount)
        {
            entityComponentStore->InstantiateEntities(*srcEntity, outputEntities, instanceCount);
        }

        [BurstCompile]
        public static void InstantiateEntities(EntityComponentStore* entityComponentStore, Entity* srcEntities, Entity* outputEntities, int entityCount, bool removePrefab)
        {
            entityComponentStore->InstantiateEntities(srcEntities, outputEntities, entityCount, removePrefab);
        }

        [BurstCompile]
        public static void InstantiateExistingEntities(EntityComponentStore* entityComponentStore, Entity* srcEntity, Entity* existingEntities, int instanceCount)
        {
            entityComponentStore->InstantiateExistingEntities(*srcEntity, existingEntities, instanceCount);
        }

    }
}
