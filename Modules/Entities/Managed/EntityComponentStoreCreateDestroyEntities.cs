// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    internal unsafe partial struct EntityComponentStore
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        public void CreateEntities(Archetype* archetype, Entity* entities, int count)
        {
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = archetype;

            while (count != 0)
            {
                var chunk = GetChunkWithEmptySlots(ref archetypeChunkFilter);
                var unusedCount = archetype->ChunkCapacity - chunk.Count;
                var allocateCount = math.min(count, unusedCount);

                ChunkDataUtility.Allocate(archetype, chunk, entities, allocateCount);

                count -= allocateCount;

                if (entities != null)
                    entities += allocateCount;
            }
        }

        public void AllocateAndAssignChunksToExistingEntities(Archetype* archetype, Entity* existingEntities, int count)
        {
            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = archetype;

            while (count != 0)
            {
                var chunk = GetChunkWithEmptySlots(ref archetypeChunkFilter);
                var unusedCount = archetype->ChunkCapacity - chunk.Count;
                var allocateCount = math.min(count, unusedCount);

                ChunkDataUtility.AssignChunkToExistingEntities(archetype, chunk, existingEntities, allocateCount);

                count -= allocateCount;

                if (existingEntities != null)
                    existingEntities += allocateCount;
            }
        }

        public void DestroyEntities(EntityQueryImpl* queryImpl, ref BufferTypeHandle<LinkedEntityGroup> linkedEntityGroupTypeHandle)
        {
            var chunkCacheIterator = new UnsafeChunkCacheIterator(queryImpl->_Filter,
                queryImpl->_QueryData->HasEnableableComponents != 0,
                queryImpl->GetMatchingChunkCache(), queryImpl->_QueryData->MatchingArchetypes.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledMask = default;
            int maxChunkCount = chunkCacheIterator.Length;
            using var chunksToProcess = new UnsafeList<ChunkAndEnabledMask>(maxChunkCount, Allocator.Temp);
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var archetypeChunk, out int chunkEntityCount,
                       out byte useEnabledMask, ref chunkEnabledMask))
            {
                // Structural changes are not allowed while using an UnsafeChunkCacheIterator, so we just track
                // the chunks & metadata to process outside the loop.
                chunksToProcess.AddNoResize(new ChunkAndEnabledMask
                {
                    Chunk = archetypeChunk.m_Chunk,
                    EnabledMask = chunkEnabledMask,
                    ChunkEntityCount = chunkEntityCount,
                    UseEnabledMask = useEnabledMask,
                });
            }
            // Validate that for all LinkedEntityGroup roots that we're about to destroy, all the entities in their
            // LinkedEntityGroup buffers are also destroyed.
            // Architecturally this validation should happen in EntityDataAccess, but it is more efficient to implement
            // once we've already computed the chunksToProcess array.
            AssertWillDestroyAllInLinkedEntityGroup(chunksToProcess, ref linkedEntityGroupTypeHandle,
                queryImpl->_QueryData->HasEnableableComponents != 0);
            // Apply the structural change to all chunks
            var chunkBatches = stackalloc EntityBatchInChunk[TypeManager.MaximumChunkCapacity];
            for(int iChunk=0,chunkCount=chunksToProcess.Length; iChunk<chunkCount; ++iChunk)
            {
                var chunk = chunksToProcess[iChunk].Chunk;
                if (chunksToProcess[iChunk].UseEnabledMask == 0)
                {
                    // All entities in the chunk are enabled; process it en masse
                    DestroyBatch(new EntityBatchInChunk {Chunk = chunk, StartIndex = 0, Count = chunksToProcess[iChunk].ChunkEntityCount});
                }
                else
                {
                    // Populate a list of entity batches in this chunk to process
                    int batchCount = 0;
                    int batchEndIndex = 0;
                    chunkEnabledMask = chunksToProcess[iChunk].EnabledMask;
                    while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out int batchStartIndex, out batchEndIndex))
                    {
                        chunkBatches[batchCount++] = new EntityBatchInChunk
                        {
                            Chunk = chunk, Count = batchEndIndex - batchStartIndex, StartIndex = batchStartIndex
                        };
                    }
                    Assert.IsTrue(batchCount <= TypeManager.MaximumChunkCapacity);
                    // Iterate batches backwards, to avoid mutating entities we haven't processed yet
                    for (int i = batchCount - 1; i >= 0; i--)
                    {
                        DestroyBatch(chunkBatches[i]);
                    }
                }
            }
        }

        public void DestroyEntities(Entity* entities, int count)
        {
            var entityIndex = 0;

            var additionalDestroyList = new UnsafeList<Entity>(0, Allocator.TempJob);
            int minDestroyStride = int.MaxValue;
            int maxDestroyStride = 0;

            while (entityIndex != count)
            {
                var entityBatchInChunk =
                    GetFirstEntityBatchInChunk(entities + entityIndex, count - entityIndex);
                var chunk = entityBatchInChunk.Chunk;
                var batchCount = entityBatchInChunk.Count;
                var indexInChunk = entityBatchInChunk.StartIndex;

                if (chunk == ChunkIndex.Null)
                {
                    entityIndex += batchCount;
                    continue;
                }

                AddToDestroyList(chunk, indexInChunk, batchCount, count, ref additionalDestroyList,
                    ref minDestroyStride, ref maxDestroyStride);

                DestroyBatch(new EntityBatchInChunk {Chunk = chunk, StartIndex = indexInChunk, Count = batchCount});

                entityIndex += batchCount;
            }

            // Apply additional destroys from any LinkedEntityGroup
            if (!additionalDestroyList.IsEmpty)
            {
                var additionalDestroyPtr = additionalDestroyList.Ptr;
                // Optimal for destruction speed is if entities with same archetype/chunk are followed one after another.
                // So we lay out the to be destroyed objects assuming that the destroyed entities are "similar":
                // Reorder destruction by index in entityGroupArray...

                //@TODO: This is a very specialized fastpath that is likely only going to give benefits in the stress test.
                ///      Figure out how to make this more general purpose.
                if (minDestroyStride == maxDestroyStride)
                {
                    var reordered = (Entity*)Memory.Unmanaged.Allocate(additionalDestroyList.Length * sizeof(Entity), 16,
                        Allocator.TempJob);
                    int batchCount = additionalDestroyList.Length / minDestroyStride;
                    for (int i = 0; i != batchCount; i++)
                    {
                        for (int j = 0; j != minDestroyStride; j++)
                            reordered[j * batchCount + i] = additionalDestroyPtr[i * minDestroyStride + j];
                    }

                    DestroyEntities(reordered, additionalDestroyList.Length);
                    Memory.Unmanaged.Free(reordered, Allocator.TempJob);
                }
                else
                {
                    DestroyEntities(additionalDestroyPtr, additionalDestroyList.Length);
                }
            }

            additionalDestroyList.Dispose();
        }

        public ChunkIndex GetCleanChunkNoMetaChunk(Archetype* archetype, SharedComponentValues sharedComponentValues)
        {
            var newChunk = AllocateChunk();
            ChunkDataUtility.AddEmptyChunk(archetype, newChunk, sharedComponentValues);

            return newChunk;
        }

        public ChunkIndex GetCleanChunk(Archetype* archetype, SharedComponentValues sharedComponentValues)
        {
            var newChunk = AllocateChunk();
            ChunkDataUtility.AddEmptyChunk(archetype, newChunk, sharedComponentValues);

            if (archetype->MetaChunkArchetype != null)
                CreateMetaEntityForChunk(newChunk);

            return newChunk;
        }

        public void InstantiateEntities(Entity srcEntity, Entity* outputEntities, int instanceCount)
        {
            AssertEntityExists(srcEntity);

            if (HasComponent(srcEntity, m_LinkedGroupType, out _))
            {
                var header = (BufferHeader*)GetComponentDataWithTypeRO(srcEntity, m_LinkedGroupType);
                var entityPtr = (Entity*)BufferHeader.GetElementPointer(header);
                var entityCount = header->Length;

                InstantiateEntitiesGroup(entityPtr, entityCount, outputEntities, true, instanceCount, true);
            }
            else
            {
                InstantiateEntitiesGroup(&srcEntity, 1, outputEntities, true, instanceCount, true);
            }
        }

        public void InstantiateExistingEntities(Entity srcEntity, Entity* existingEntities, int instanceCount)
        {
            AssertEntityExists(srcEntity);

            if (HasComponent(srcEntity, m_LinkedGroupType, out _))
            {
                var header = (BufferHeader*)GetComponentDataWithTypeRO(srcEntity, m_LinkedGroupType);
                var entityPtr = (Entity*)BufferHeader.GetElementPointer(header);
                var entityCount = header->Length;

                InstantiateExistingEntitiesGroup(entityPtr, entityCount, existingEntities, instanceCount, true);
            }
            else
            {
                InstantiateExistingEntitiesGroup(&srcEntity, 1, existingEntities, instanceCount, true);
            }
        }

        public void InstantiateEntities(Entity* srcEntity, Entity* outputEntities, int entityCount, bool removePrefab)
        {
            InstantiateEntitiesGroup(srcEntity, entityCount, outputEntities, false, 1, removePrefab);
        }

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------

        internal void DestroyMetaChunkEntity(Entity entity)
        {
            RemoveComponent(entity, m_ChunkHeaderComponentType);
            DestroyEntities(&entity, 1);
        }

        internal void CreateMetaEntityForChunk(ChunkIndex chunk)
        {
            fixed(EntityComponentStore* entityComponentStore = &this)
            {
                var archetype = entityComponentStore->GetArchetype(chunk);
                var metaEntity = Entity.Null;
                CreateEntities(archetype->MetaChunkArchetype, &metaEntity, 1);

                chunk.MetaChunkEntity = metaEntity;

                var chunkHeader = (ChunkHeader*)GetComponentDataWithTypeRW(metaEntity, m_ChunkHeaderType, GlobalSystemVersion);

                chunkHeader->ArchetypeChunk = new ArchetypeChunk(chunk, entityComponentStore);
            }
        }

        internal struct InstantiateRemapChunk
        {
            public ChunkIndex Chunk;
            public int IndexInChunk;
            public int AllocatedCount;
            public int InstanceBeginIndex;
        }

        void AddToDestroyList(ChunkIndex chunk, int indexInChunk, int batchCount, int inputDestroyCount,
            ref UnsafeList<Entity> entitiesList, ref int minBufferLength, ref int maxBufferLength)
        {
            var archetype = GetArchetype(chunk);
            int indexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, m_LinkedGroupType);
            if (indexInArchetype != -1)
            {
                var baseHeader = ChunkDataUtility.GetComponentDataWithTypeRO(chunk, archetype, indexInChunk, m_LinkedGroupType);
                var stride = archetype->SizeOfs[indexInArchetype];
                for (int i = 0; i != batchCount; i++)
                {
                    var header = (BufferHeader*)(baseHeader + stride * i);

                    var entityGroupCount = header->Length - 1;
                    if (entityGroupCount <= 0)
                        continue;

                    var entityGroupArray = (Entity*)BufferHeader.GetElementPointer(header) + 1;

                    if (entitiesList.Capacity == 0)
                        entitiesList.SetCapacity(inputDestroyCount * entityGroupCount /*, Allocator.TempJob*/);
                    entitiesList.AddRange(entityGroupArray, entityGroupCount /*, Allocator.TempJob*/);

                    minBufferLength = math.min(minBufferLength, entityGroupCount);
                    maxBufferLength = math.max(maxBufferLength, entityGroupCount);
                }
            }
        }


        void DestroyBatch(in EntityBatchInChunk batch)
        {
            var chunk = batch.Chunk;
            var archetype = GetArchetype(chunk);

            if (!archetype->CleanupNeeded)
            {
                FireOnRemovedCallbacks(chunk, archetype, batch.StartIndex, batch.Count);
                ChunkDataUtility.Deallocate(archetype, batch);
            }
            else
            {
                var startIndex = batch.StartIndex;
                var count = batch.Count;

                var cleanupResidueArchetype = archetype->CleanupResidueArchetype;
                if (archetype == cleanupResidueArchetype)
                    return;

                var dstArchetypeChunkFilter = new ArchetypeChunkFilter();
                dstArchetypeChunkFilter.Archetype = cleanupResidueArchetype;

                var srcSharedComponentValues = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);
                if (RequiresBuildingResidueSharedComponentIndices(archetype, dstArchetypeChunkFilter.Archetype))
                {
                    BuildResidueSharedComponentIndices(archetype, dstArchetypeChunkFilter.Archetype, srcSharedComponentValues, dstArchetypeChunkFilter.SharedComponentValues);
                }
                else
                {
                    srcSharedComponentValues.CopyTo(dstArchetypeChunkFilter.SharedComponentValues, 0, archetype->NumSharedComponents);
                }

                if (count == chunk.Count)
                    Move(chunk, ref dstArchetypeChunkFilter);
                else
                    Move(new EntityBatchInChunk {Chunk = chunk, StartIndex = startIndex, Count = count}, ref dstArchetypeChunkFilter);
            }
        }

        bool RequiresBuildingResidueSharedComponentIndices(Archetype* srcArchetype,
            Archetype* dstArchetype)
        {
            return dstArchetype->NumSharedComponents > 0 &&
                dstArchetype->NumSharedComponents != srcArchetype->NumSharedComponents;
        }

        void BuildResidueSharedComponentIndices(Archetype* srcArchetype, Archetype* dstArchetype,
            SharedComponentValues srcSharedComponentValues, int* dstSharedComponentValues)
        {
            int oldFirstShared = srcArchetype->FirstSharedComponent;
            int newFirstShared = dstArchetype->FirstSharedComponent;
            int newCount = dstArchetype->NumSharedComponents;

            for (int oldIndex = 0, newIndex = 0; newIndex < newCount; ++newIndex, ++oldIndex)
            {
                var t = dstArchetype->Types[newIndex + newFirstShared];
                while (t != srcArchetype->Types[oldIndex + oldFirstShared])
                    ++oldIndex;
                dstSharedComponentValues[newIndex] = srcSharedComponentValues[oldIndex];
            }
        }

        int InstantiateEntitiesOne(Entity srcEntity, Entity* outputEntities, int instanceCount, InstantiateRemapChunk* remapChunks, int remapChunksCount, bool removePrefab)
        {
            var src = GetEntityInChunk(srcEntity);
            var srcArchetype = GetArchetype(src.Chunk);

            var dstArchetype = removePrefab ? srcArchetype->InstantiateArchetype : srcArchetype->CopyArchetype;

            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;

            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(src.Chunk.ListIndex);
            if (RequiresBuildingResidueSharedComponentIndices(srcArchetype, dstArchetype))
            {
                BuildResidueSharedComponentIndices(srcArchetype, dstArchetype, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                // Always copy shared component indices since GetChunkWithEmptySlots might reallocate the storage of SharedComponentValues
                srcSharedComponentValues.CopyTo(archetypeChunkFilter.SharedComponentValues, 0, dstArchetype->NumSharedComponents);
            }

            int instanceBeginIndex = 0;
            while (instanceBeginIndex != instanceCount)
            {
                var chunk = GetChunkWithEmptySlots(ref archetypeChunkFilter);
                var indexInChunk = chunk.Count;
                var unusedCount = dstArchetype->ChunkCapacity - indexInChunk;
                var allocateCount = math.min(instanceCount - instanceBeginIndex, unusedCount);

                ChunkDataUtility.AllocateClone(dstArchetype, chunk, outputEntities + instanceBeginIndex, allocateCount, srcEntity);

                FireOnAddedCallbacks(chunk, dstArchetype, indexInChunk, allocateCount);

                if (remapChunks != null)
                {
                    remapChunks[remapChunksCount].Chunk = chunk;
                    remapChunks[remapChunksCount].IndexInChunk = indexInChunk;
                    remapChunks[remapChunksCount].AllocatedCount = allocateCount;
                    remapChunks[remapChunksCount].InstanceBeginIndex = instanceBeginIndex;
                    remapChunksCount++;
                }

                instanceBeginIndex += allocateCount;
            }

            return remapChunksCount;
        }

        int InstantiateExistingEntitiesOne(Entity srcEntity, Entity* existingEntities, int instanceCount, InstantiateRemapChunk* remapChunks, int remapChunksCount, bool removePrefab)
        {
            var src = GetEntityInChunk(srcEntity);
            var srcArchetype = GetArchetype(src.Chunk);

            var dstArchetype = removePrefab ? srcArchetype->InstantiateArchetype : srcArchetype->CopyArchetype;

            var archetypeChunkFilter = new ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = dstArchetype;

            var srcSharedComponentValues = srcArchetype->Chunks.GetSharedComponentValues(src.Chunk.ListIndex);
            if (RequiresBuildingResidueSharedComponentIndices(srcArchetype, dstArchetype))
            {
                BuildResidueSharedComponentIndices(srcArchetype, dstArchetype, srcSharedComponentValues, archetypeChunkFilter.SharedComponentValues);
            }
            else
            {
                // Always copy shared component indices since GetChunkWithEmptySlots might reallocate the storage of SharedComponentValues
                srcSharedComponentValues.CopyTo(archetypeChunkFilter.SharedComponentValues, 0, dstArchetype->NumSharedComponents);
            }

            int instanceBeginIndex = 0;
            while (instanceBeginIndex != instanceCount)
            {
                var chunk = GetChunkWithEmptySlots(ref archetypeChunkFilter);
                var indexInChunk = chunk.Count;
                var unusedCount = dstArchetype->ChunkCapacity - indexInChunk;
                var allocateCount = math.min(instanceCount - instanceBeginIndex, unusedCount);

                ChunkDataUtility.AllocateCloneForExistingEntities(dstArchetype, chunk, existingEntities + instanceBeginIndex, allocateCount, srcEntity);

                FireOnAddedCallbacks(chunk, dstArchetype, indexInChunk, allocateCount);

                if (remapChunks != null)
                {
                    remapChunks[remapChunksCount].Chunk = chunk;
                    remapChunks[remapChunksCount].IndexInChunk = indexInChunk;
                    remapChunks[remapChunksCount].AllocatedCount = allocateCount;
                    remapChunks[remapChunksCount].InstanceBeginIndex = instanceBeginIndex;
                    remapChunksCount++;
                }

                instanceBeginIndex += allocateCount;
            }

            return remapChunksCount;
        }

        void InstantiateEntitiesGroup(Entity* srcEntities, int srcEntityCount, Entity* outputRootEntities, bool outputRootEntityOnly, int instanceCount, bool removePrefab)
        {
            int totalCount = srcEntityCount * instanceCount;

            var tempAllocSize = sizeof(Entity) * totalCount +
                sizeof(InstantiateRemapChunk) * totalCount + sizeof(Entity) * instanceCount;
            byte* allocation;
            const int kMaxStackAllocSize = 16 * 1024;

            if (tempAllocSize > kMaxStackAllocSize)
            {
                allocation = (byte*)Memory.Unmanaged.Allocate(tempAllocSize, 16, Allocator.Temp);
            }
            else
            {
                var temp = stackalloc byte[tempAllocSize];

                allocation = temp;
            }

            var entityRemap = (Entity*)allocation;
            var remapChunks = (InstantiateRemapChunk*)(entityRemap + totalCount);
            var outputEntities = (Entity*)(remapChunks + totalCount);

            var remapChunksCount = 0;

            for (int i = 0; i != srcEntityCount; i++)
            {
                var srcEntity = srcEntities[i];

                remapChunksCount = InstantiateEntitiesOne(srcEntity, outputEntities, instanceCount, remapChunks, remapChunksCount, removePrefab);

                for (int r = 0; r != instanceCount; r++)
                {
                    var ptr = entityRemap + (r * srcEntityCount + i);
                    *ptr = outputEntities[r];
                }

                if (outputRootEntityOnly)
                {
                    if (i == 0)
                    {
                        for (int r = 0; r != instanceCount; r++)
                            outputRootEntities[r] = outputEntities[r];
                    }
                }
                else
                {
                    for (int r = 0; r != instanceCount; r++)
                        outputRootEntities[r * srcEntityCount + i] = outputEntities[r];
                }
            }


            for (int i = 0; i != remapChunksCount; i++)
            {
                var chunk = remapChunks[i].Chunk;
                var dstArchetype = GetArchetype(chunk);
                var allocatedCount = remapChunks[i].AllocatedCount;
                var indexInChunk = remapChunks[i].IndexInChunk;
                var instanceBeginIndex = remapChunks[i].InstanceBeginIndex;

                var localRemap = entityRemap + instanceBeginIndex * srcEntityCount;

                EntityRemapUtility.PatchEntitiesForPrefab(dstArchetype->ScalarEntityPatches + 1, dstArchetype->ScalarEntityPatchCount - 1,
                    dstArchetype->BufferEntityPatches, dstArchetype->BufferEntityPatchCount,
                    chunk.Buffer, indexInChunk, allocatedCount, srcEntities, localRemap, srcEntityCount);
            }

            if (tempAllocSize > kMaxStackAllocSize)
                Memory.Unmanaged.Free(allocation, Allocator.Temp);
        }

        void InstantiateExistingEntitiesGroup(Entity* srcEntities, int srcEntityCount, Entity* existingEntities, int instanceCount, bool removePrefab)
        {
            int totalCount = srcEntityCount * instanceCount;

            var tempAllocSize = sizeof(Entity) * totalCount +
                sizeof(InstantiateRemapChunk) * totalCount + sizeof(Entity) * instanceCount;
            byte* allocation;
            const int kMaxStackAllocSize = 16 * 1024;

            if (tempAllocSize > kMaxStackAllocSize)
            {
                allocation = (byte*)Memory.Unmanaged.Allocate(tempAllocSize, 16, Allocator.Temp);
            }
            else
            {
                var temp = stackalloc byte[tempAllocSize];

                allocation = temp;
            }

            var entityRemap = (Entity*)allocation;
            var remapChunks = (InstantiateRemapChunk*)(entityRemap + totalCount);
            var childEntities = (Entity*)(remapChunks + totalCount);

            var remapChunksCount = 0;

            for (int i = 0; i != srcEntityCount; i++)
            {
                var srcEntity = srcEntities[i];

                if (i == 0)
                {
                    remapChunksCount = InstantiateExistingEntitiesOne(srcEntity, existingEntities, instanceCount, remapChunks, remapChunksCount, removePrefab);

                    for (int r = 0; r != instanceCount; r++)
                    {
                        var ptr = entityRemap + (r * srcEntityCount + i);
                        *ptr = existingEntities[r];
                    }
                }
                else
                {
                    remapChunksCount = InstantiateEntitiesOne(srcEntity, childEntities, instanceCount, remapChunks, remapChunksCount, true);

                    for (int r = 0; r != instanceCount; r++)
                    {
                        var ptr = entityRemap + (r * srcEntityCount + i);
                        *ptr = childEntities[r];
                    }
                }
            }


            for (int i = 0; i != remapChunksCount; i++)
            {
                var chunk = remapChunks[i].Chunk;
                var dstArchetype = GetArchetype(chunk);
                var allocatedCount = remapChunks[i].AllocatedCount;
                var indexInChunk = remapChunks[i].IndexInChunk;
                var instanceBeginIndex = remapChunks[i].InstanceBeginIndex;

                var localRemap = entityRemap + instanceBeginIndex * srcEntityCount;

                EntityRemapUtility.PatchEntitiesForPrefab(dstArchetype->ScalarEntityPatches + 1, dstArchetype->ScalarEntityPatchCount - 1,
                    dstArchetype->BufferEntityPatches, dstArchetype->BufferEntityPatchCount,
                    chunk.Buffer, indexInChunk, allocatedCount, srcEntities, localRemap, srcEntityCount);
            }

            if (tempAllocSize > kMaxStackAllocSize)
                Memory.Unmanaged.Free(allocation, Allocator.Temp);
        }

        EntityBatchInChunk GetFirstEntityBatchInChunk(Entity* entities, int count)
        {
            // This is optimized for the case where the array of entities are allocated contiguously in the chunk
            // Thus the compacting of other elements can be batched


            var entityInChunk = Exists(entities[0]) ? GetEntityInChunk(entities[0]) : default;
            var chunk = entityInChunk.Chunk;
            var indexInChunk = entityInChunk.IndexInChunk;

            int batchCount = 0;

            for (; batchCount < count; batchCount++)
            {
                var entity = entities[batchCount];
                entityInChunk = Exists(entity) ? GetEntityInChunk(entity) : default;
                if (entityInChunk.Chunk != chunk || entityInChunk.IndexInChunk != indexInChunk + batchCount)
                {
                    break;
                }
            }

            Assert.IsTrue(chunk == ChunkIndex.Null || indexInChunk < chunk.Count);
            Assert.IsTrue(chunk == ChunkIndex.Null || indexInChunk + batchCount <= chunk.Count);

            return new EntityBatchInChunk
            {
                Chunk = chunk,
                Count = batchCount,
                StartIndex = indexInChunk
            };
        }

        public static JobHandle GetCreatedAndDestroyedEntities(EntityComponentStore* store, NativeList<int> state, NativeList<Entity> createdEntities, NativeList<Entity> destroyedEntities, bool async)
        {
            JobHandle jobHandle = default;
            GetCreatedAndDestroyedEntities(store, ref state, ref createdEntities, ref destroyedEntities, ref jobHandle, async);
            return jobHandle;
        }

        [BurstCompile]
        static void GetCreatedAndDestroyedEntities(EntityComponentStore* store, ref NativeList<int> state, ref NativeList<Entity> createdEntities, ref NativeList<Entity> destroyedEntities, ref JobHandle jobHandle, bool async)
        {
            var newState = new NativeList<int>(async ? Allocator.TempJob : Allocator.Temp);

            var archetypes = store->m_Archetypes;
            for (int ai = 0, ac = archetypes.Length; ai < ac; ai++)
            {
                var archetype = store->m_Archetypes.Ptr[ai];

                if (archetype->HasChunkHeader)
                {
                    // skip meta entities
                    continue;
                }

                var chunks = archetype->Chunks;
                for (int ci = 0, cc = chunks.Count; ci < cc; ci++)
                {
                    var chunk = chunks[ci];
                    var entities = (Entity*)chunk.Buffer;

                    newState.AddRange(entities, chunk.Count * 2); // x2 because an entity is two ints
                }
            }

            var job = new GetCreatedAndDestroyedEntitiesJob
            {
                OldState = state,
                NewState = newState,
                CreatedEntities = createdEntities,
                DestroyedEntities = destroyedEntities,
            };

            if (!async)
            {
                job.Execute();
            }
            else
            {
                jobHandle = job.Schedule();
                jobHandle = newState.Dispose(jobHandle);
                jobHandle.Complete();
            }
        }

        [BurstCompile]
        internal struct GetCreatedAndDestroyedEntitiesJob : IJob
        {
            public NativeList<int>    OldState;
            public NativeList<int>    NewState;

            public NativeList<Entity> CreatedEntities;
            public NativeList<Entity> DestroyedEntities;

            public void Execute()
            {
                CreatedEntities.Clear();
                DestroyedEntities.Clear();

                var oldEntities = OldState.AsArray().Reinterpret<Entity>(sizeof(int));
                var newEntities = NewState.AsArray().Reinterpret<Entity>(sizeof(int));

                newEntities.Sort();

                int oi = 0;
                int ni = 0;

                int oc = oldEntities.Length;
                int nc = newEntities.Length;

                while (true)
                {
                    if (oi == oc)
                    {
                        // Only new entities from now on
                        if (ni != nc)
                        {
                            CreatedEntities.AddRange(newEntities.GetSubArray(ni, nc - ni));
                        }

                        break;
                    }

                    if (ni == nc)
                    {
                        // Only old entities from now on
                        if (oi != oc)
                        {
                            DestroyedEntities.AddRange(oldEntities.GetSubArray(oi, oc - oi));
                        }

                        break;
                    }

                    var cmp = oldEntities[oi].CompareTo(newEntities[ni]);

                    if (cmp == 0)
                    {
                        oi++;
                        ni++;
                        continue;
                    }

                    if (cmp < 0)
                    {
                        DestroyedEntities.Add(oldEntities[oi]);
                        oi++;
                    }
                    else
                    {
                        CreatedEntities.Add(newEntities[ni]);
                        ni++;
                    }
                }

                OldState.CopyFrom(NewState);
            }
        }
    }
}
