// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    /// <summary>
    /// Enables iteration over chunks belonging to a set of archetypes.
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility()]
    internal unsafe partial struct ChunkIterationUtility
    {
        /// <summary>
        /// Creates a NativeArray with all the chunks in a given archetype filtered by the provided EntityQueryFilter.
        /// This function will not sync the needed types in the EntityQueryFilter so they have to be synced manually before calling this function.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="jobHandle">Handle to the GatherChunks job used to fill the output array.</param>
        /// <param name="filter">Filter used to filter the resulting chunks</param>
        /// <param name="dependsOn">All jobs spawned will depend on this JobHandle</param>
        /// <returns>NativeArray of all the chunks in the matchingArchetypes list.</returns>
        [Obsolete("Remove with CreateArchetypeChunkArrayAsync. (RemovedAfter Entities 1.0)")]
        public static NativeArray<ArchetypeChunk> CreateArchetypeChunkArrayAsync(UnsafeMatchingArchetypePtrList matchingArchetypes,
            AllocatorManager.AllocatorHandle allocator, out JobHandle jobHandle, ref EntityQueryFilter filter,
            JobHandle dependsOn = default(JobHandle))
        {
            var archetypeCount = matchingArchetypes.Length;

            var offsets =
                new NativeArray<int>(archetypeCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var chunkCount = 0;
            {
                var ptrs = matchingArchetypes.Ptr;
                for (int i = 0; i < archetypeCount; ++i)
                {
                    var archetype = ptrs[i]->Archetype;
                    offsets[i] = chunkCount;
                    chunkCount += archetype->Chunks.Count;
                }
            }

            if (!filter.RequiresMatchesFilter)
            {
                var chunks = CollectionHelper.CreateNativeArray<ArchetypeChunk>(chunkCount, allocator, NativeArrayOptions.UninitializedMemory);
                var gatherChunksJob = new GatherChunksToArrayJob
                {
                    MatchingArchetypes = matchingArchetypes.Ptr,
                    entityComponentStore = matchingArchetypes.entityComponentStore,
                    Offsets = offsets,
                    Chunks = chunks
                };
                jobHandle = gatherChunksJob.Schedule(archetypeCount, 1, dependsOn);

                return chunks;
            }
            else
            {
                var filteredCounts =  new NativeArray<int>(archetypeCount + 1, Allocator.TempJob);
                var sparseChunks = new NativeArray<ArchetypeChunk>(chunkCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                var gatherChunksJob = new GatherChunksToArrayWithFilteringJob
                {
                    MatchingArchetypes = matchingArchetypes.Ptr,
                    Filter = filter,
                    Offsets = offsets,
                    FilteredCounts = filteredCounts,
                    SparseChunks = sparseChunks,
                    entityComponentStore = matchingArchetypes.entityComponentStore
                };
                gatherChunksJob.Schedule(archetypeCount, 1, dependsOn).Complete();

                // accumulated filtered counts: filteredCounts[i] becomes the destination offset
                int totalChunks = 0;
                for (int i = 0; i < archetypeCount; ++i)
                {
                    int currentCount = filteredCounts[i];
                    filteredCounts[i] = totalChunks;
                    totalChunks += currentCount;
                }
                filteredCounts[archetypeCount] = totalChunks;

                var joinedChunks = CollectionHelper.CreateNativeArray<ArchetypeChunk>(totalChunks, allocator, NativeArrayOptions.UninitializedMemory);

                jobHandle = new JoinChunksJob
                {
                    DestinationOffsets = filteredCounts,
                    SparseChunks = sparseChunks,
                    Offsets = offsets,
                    JoinedChunks = joinedChunks
                }.Schedule(archetypeCount, 1);

                return joinedChunks;
            }
        }

        private interface IChunkGatherer
        {
            void AddNoResize(ChunkIndex chunkIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ToChunkList<TGatherer>(in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            ref TGatherer outChunks) where TGatherer : struct, IChunkGatherer
        {
            var cachedChunkIndices = cachedChunkList.ChunkIndices;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableable = hasEnableableComponents != 0;
            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            var ecs = cachedChunkList.EntityComponentStore;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableable)
            {
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outChunks.AddNoResize(cachedChunkIndices[chunkIndexInCache]);
                }
            }
            else if (hasEnableable)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;
                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    if (chunkEnabledMask.ULong0 == 0 && chunkEnabledMask.ULong1 == 0)
                        continue;
                    outChunks.AddNoResize(cachedChunkIndices[chunkIndexInCache]);
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (!currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;
                    outChunks.AddNoResize(cachedChunkIndices[chunkIndexInCache]);
                }
            }
        }

        private struct ArchetypeChunkListGatherer : IChunkGatherer
        {
            public NativeList<ArchetypeChunk> ArchetypeChunks;
            public EntityComponentStore* Ecs;
            public void AddNoResize(ChunkIndex chunkIndex) => ArchetypeChunks.AddNoResize(new ArchetypeChunk(chunkIndex, Ecs));
        }

        [BurstCompile]
        public static void ToArchetypeChunkList(in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            ref NativeList<ArchetypeChunk> outChunks)
        {
            var gatherer = new ArchetypeChunkListGatherer
            {
                ArchetypeChunks = outChunks,
                Ecs = cachedChunkList.EntityComponentStore
            };
            ToChunkList(cachedChunkList, matchingArchetypes, ref filter, hasEnableableComponents, ref gatherer);
        }

        private struct ChunkIndexListGatherer : IChunkGatherer
        {
            public NativeList<ChunkIndex> ChunkIndices;
            public void AddNoResize(ChunkIndex chunkIndex) => ChunkIndices.AddNoResize(chunkIndex);
        }

        [BurstCompile]
        public static void ToChunkIndexList(in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            ref NativeList<ChunkIndex> outChunkIndices)
        {
            var gatherer = new ChunkIndexListGatherer
            {
                ChunkIndices = outChunkIndices
            };
            ToChunkList(cachedChunkList, matchingArchetypes, ref filter, hasEnableableComponents, ref gatherer);
        }

        [BurstCompile]
        public static void GatherEntitiesWithFilter(Entity* entities,ref EntityQueryFilter filter,
            in UnsafeCachedChunkList cache, ref UnsafeMatchingArchetypePtrList matchingArchetypePtrList)
        {
            Entity* copyDest = entities;
            var chunkCacheIterator = new UnsafeChunkCacheIterator(filter, true, cache, matchingArchetypePtrList.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledMask = default;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out var chunkEntityCount, out byte useEnableBits, ref chunkEnabledMask))
            {
                Entity* chunkEntities = (Entity*)chunk.m_Chunk.Buffer; // Entity is always the first table in the chunk buffer
                if (useEnableBits == 0)
                {
                    UnsafeUtility.MemCpy(copyDest, chunkEntities, chunkEntityCount * sizeof(Entity));
                    copyDest += chunkEntityCount;
                }
                else
                {
                    // If this branch ever becomes a bottleneck for the many-small-batches case, we could add a third path
                    // that iterates over the chunkEnabledMask directly, similar to what IJobEntity generates.
                    int batchEndIndex = 0;
                    while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out int batchStartIndex, out batchEndIndex))
                    {
                        int batchEntityCount = batchEndIndex - batchStartIndex;
                        UnsafeUtility.MemCpy(copyDest, chunkEntities+batchStartIndex, batchEntityCount * sizeof(Entity));
                        copyDest += batchEntityCount;
                    }
                }
            }
        }

        [BurstCompile]
        public static void GatherEntitiesWithoutFilter(Entity* entities, in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes)
        {
            var chunkCacheIterator = new UnsafeChunkCacheIterator(default, false, cachedChunkList, matchingArchetypes.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledBits = default;
            Entity* copyDest = entities;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out int chunkEntityCount,
                       out byte useEnabledBits, ref chunkEnabledBits))
            {
                Entity* copySrc = (Entity*)chunk.m_Chunk.Buffer; // Entity is always the first table in the chunk buffer
                UnsafeUtility.MemCpy(copyDest, copySrc, chunkEntityCount*sizeof(Entity));
                copyDest += chunkEntityCount;
            }
        }

        /// <summary>
        ///     Creates a NativeArray containing the entities in a given EntityQuery.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">An atomic safety handle required by GatherEntitiesJob so it can call GetNativeArray() on chunks.</param>
        /// <param name="entityQuery">EntityQuery to gather entities from.</param>
        /// <param name="entityCount">number of entities to reserve for the returned NativeArray.</param>
        /// <param name="filter">EntityQueryFilter for calculating the length of the output array.</param>
        /// <returns>NativeArray of the entities in a given EntityQuery.</returns>
        public static NativeArray<Entity> CreateEntityArray(UnsafeMatchingArchetypePtrList matchingArchetypes,
            AllocatorManager.AllocatorHandle allocator,
            EntityTypeHandle typeHandle,
            EntityQuery entityQuery,
            int entityCount)
        {
            var cache = entityQuery.__impl->GetMatchingChunkCache();
            var entities = CollectionHelper.CreateNativeArray<Entity>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            var requiresFilter = entityQuery.HasFilter();
            var hasEnableableComponents = entityQuery.__impl->_QueryData->HasEnableableComponents != 0;
            if (!requiresFilter && !hasEnableableComponents)
            {
                GatherEntitiesWithoutFilter((Entity*)entities.GetUnsafePtr(), in cache, in matchingArchetypes);
            }
            else
            {
                var filter = entityQuery.__impl->_Filter;
                GatherEntitiesWithFilter((Entity*)entities.GetUnsafePtr(), ref filter, in cache, ref matchingArchetypes);
            }


            return entities;
        }


        /// <summary>
        ///  Obsolete. Use <see cref="CreateEntityListAsync"/> instead.
        /// </summary>
        /// <remarks> **Obsolete.** Use <see cref="CreateEntityListAsync"/> instead.
        ///
        /// Creates a NativeArray containing the entities in a given EntityQuery.</remarks>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">An atomic safety handle required by GatherEntitiesJob so it can call GetNativeArray() on chunks.</param>
        /// <param name="entityQuery">EntityQuery to gather entities from.</param>
        /// <param name="entityCount">number of entities to reserve for the returned NativeArray.</param>
        /// <param name="outJobHandle">Handle to the GatherEntitiesJob job used to fill the output array.</param>
        /// <param name="dependsOn">Handle to a job this GatherEntitiesJob must wait on.</param>
        /// <returns>NativeArray of the entities in a given EntityQuery.</returns>
        [Obsolete("Use CreateEntityListAsync instead. (RemovedAfter Entities 1.0)")]
        public static NativeArray<Entity> CreateEntityArrayAsync(UnsafeMatchingArchetypePtrList matchingArchetypes,
            AllocatorManager.AllocatorHandle allocator,
            EntityTypeHandle typeHandle,
            EntityQuery entityQuery,
            int entityCount,
            out JobHandle outJobHandle,
            JobHandle dependsOn)
        {
            var entities = CollectionHelper.CreateNativeArray<Entity>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);

            var job = new GatherEntitiesToArrayJob
            {
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
                EntityTypeHandle = typeHandle,
                Entities = (byte*)entities.GetUnsafePtr()
            };
            outJobHandle = job.ScheduleParallel(entityQuery, baseIndexJob);

            return entities;
        }

        /// <summary>
        ///     Creates a NativeList containing the entities in a given EntityQuery.
        /// </summary>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">An atomic safety handle required by GatherEntitiesJob so it can call GetNativeArray() on chunks.</param>
        /// <param name="entityQuery">EntityQuery to gather entities from.</param>
        /// <param name="maxEntityCount">number of entities to reserve for the returned NativeArray.</param>
        /// <param name="outJobHandle">Handle to the GatherEntitiesJob job used to fill the output array.</param>
        /// <param name="dependsOn">Handle to a job this GatherEntitiesJob must wait on.</param>
        /// <returns>NativeList of the entities in a given EntityQuery.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static NativeList<Entity> CreateEntityListAsync(AllocatorManager.AllocatorHandle allocator,
            EntityTypeHandle typeHandle,
            EntityQuery entityQuery,
            int maxEntityCount,
            JobHandle dependsOn,
            out JobHandle outJobHandle)
        {
            var entities = new NativeList<Entity>(maxEntityCount, allocator);

            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);

            var job = new GatherEntitiesJob
            {
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
                OutputList = new TypelessUnsafeList{
                    Ptr = (byte*)entities.m_ListData->Ptr,
                    Length = &(entities.m_ListData->m_length),
                    Capacity = entities.m_ListData->Capacity,
                    m_Safety = entities.m_Safety,
                    },
                EntityTypeHandle = typeHandle,
            };
            outJobHandle = job.ScheduleParallelByRef(entityQuery, baseIndexJob);

            return entities;
        }

        [BurstCompile]
        public static void GatherComponentDataWithFilter(byte* componentData, TypeIndex typeIndex,
            in UnsafeCachedChunkList cache, in UnsafeMatchingArchetypePtrList matchingArchetypePtrList,
            ref EntityQueryFilter filter)
        {
            byte* copyDest = componentData;
            var chunkCacheIterator = new UnsafeChunkCacheIterator(filter, true, cache, matchingArchetypePtrList.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledMask = default;
            LookupCache typeLookupCache = default;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out var chunkEntityCount,
                       out byte useEnableBits, ref chunkEnabledMask))
            {
                var chunkArchetype = chunkCacheIterator._CurrentMatchingArchetype->Archetype;
                if (chunkArchetype != typeLookupCache.Archetype)
                    typeLookupCache.Update(chunkArchetype, typeIndex);
                var chunkComponentData = chunk.m_Chunk.Buffer + typeLookupCache.ComponentOffset;

                if (useEnableBits == 0)
                {
                    UnsafeUtility.MemCpy(copyDest, chunkComponentData, chunkEntityCount * typeLookupCache.ComponentSizeOf);
                    copyDest += chunkEntityCount*typeLookupCache.ComponentSizeOf;
                }
                else
                {
                    // If this branch ever becomes a bottleneck for the many-small-batches case, we could add a third path
                    // that iterates over the chunkEnabledMask directly, similar to what IJobEntity generates.
                    int batchEndIndex = 0;
                    while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out int batchStartIndex, out batchEndIndex))
                    {
                        int batchEntityCount = batchEndIndex - batchStartIndex;
                        UnsafeUtility.MemCpy(copyDest, chunkComponentData+batchStartIndex * typeLookupCache.ComponentSizeOf, batchEntityCount * typeLookupCache.ComponentSizeOf);
                        copyDest += batchEntityCount*typeLookupCache.ComponentSizeOf;
                    }
                }
            }
        }

        [BurstCompile]
        public static void GatherComponentDataWithoutFilter(byte* componentData, TypeIndex typeIndex, in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypePtrList)
        {
            var chunkCacheIterator = new UnsafeChunkCacheIterator(default, false, cachedChunkList, matchingArchetypePtrList.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledBits = default;
            LookupCache typeLookupCache = default;
            byte* copyDest = componentData;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out int chunkEntityCount,
                       out byte useEnabledBits, ref chunkEnabledBits))
            {
                var chunkArchetype = chunkCacheIterator._CurrentMatchingArchetype->Archetype;
                if (chunkArchetype != typeLookupCache.Archetype)
                    typeLookupCache.Update(chunkArchetype, typeIndex);
                var copySrc = chunk.m_Chunk.Buffer + typeLookupCache.ComponentOffset;
                var copySize = typeLookupCache.ComponentSizeOf * chunkEntityCount;
                UnsafeUtility.MemCpy(copyDest, copySrc, copySize);
                copyDest += copySize;
            }
        }

        /// <summary>
        /// Obsolete. Use <see cref="CreateComponentDataListAsync"/> instead.
        /// </summary>
        /// <remarks>
        /// **Obsolete.** Use <see cref="CreateComponentDataListAsync"/> instead.
        ///
        /// Creates a NativeArray with the value of a single component for all entities matching the provided query.
        /// The job that schedules this method populates the NativeArray. This method
        /// doesn't sync the needed types in the EntityQueryFilter, so you have to be sync them manually before calling this.
        /// </remarks>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">Type handle for the component whose values should be extracted.</param>
        /// <param name="entityCount">Number of entities that match the query. Used as the output array size.</param>
        /// <param name="entityQuery">Entities that match this query will be included in the output.</param>
        /// <param name="outJobHandle">Handle to the job that will populate the output array. The caller must complete this job before the output array contents are valid.</param>
        /// <param name="dependsOn">Input job dependencies for the array-populating job.</param>
        /// <returns>NativeArray of all the chunks in the matchingArchetypes list.</returns>
        [Obsolete("Use CreateComponentDataListAsync instead. (RemovedAfter Entities 1.0)")]
        public static NativeArray<T> CreateComponentDataArrayAsync<T>(
            AllocatorManager.AllocatorHandle allocator,
            ref ComponentTypeHandle<T> typeHandle,
            int entityCount,
            EntityQuery entityQuery,
            out JobHandle outJobHandle,
            JobHandle dependsOn)
            where T : unmanaged, IComponentData
        {
            var componentData = CollectionHelper.CreateNativeArray<T>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);

            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);
            var job = new GatherComponentDataToArrayJob
            {
                ComponentData = (byte*)componentData.GetUnsafePtr(),
                TypeIndex = typeHandle.m_TypeIndex,
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
            };
            outJobHandle = job.ScheduleParallel(entityQuery, baseIndexJob);

            return componentData;
        }

        /// <summary>
        /// Creates a NativeList with the value of a single component for all entities matching the provided query.
        /// The array will be populated by a job scheduled by this function.
        /// This function will not sync the needed types in the EntityQueryFilter so they have to be synced manually before calling this function.
        /// </summary>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">Type handle for the component whose values should be extracted.</param>
        /// <param name="maxEntityCount">Number of entities that match the query. Used as the output array size.</param>
        /// <param name="entityQuery">Entities that match this query will be included in the output.</param>
        /// <param name="dependsOn">Input job dependencies for the array-populating job.</param>
        /// <param name="outJobHandle">Handle to the job that will populate the output array. The caller must complete this job before the output array contents are valid.</param>
        /// <returns>NativeList of all the chunks in the matchingArchetypes list.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static NativeList<T> CreateComponentDataListAsync<T>(
            AllocatorManager.AllocatorHandle allocator,
            ref DynamicComponentTypeHandle typeHandle,
            int maxEntityCount,
            EntityQuery entityQuery,
            JobHandle dependsOn,
            out JobHandle outJobHandle)
            where T : unmanaged, IComponentData
        {
            var componentData = new NativeList<T>(maxEntityCount, allocator);

            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);

            // Can't pass a generic NativeList<T> to a non-generic job, so we pass individual ptr/length/capacity fields
            // instead (plus a copy of the list's safety handle)
            var job = new GatherComponentDataJob
            {
                OutputList = new TypelessUnsafeList{
                    Ptr = (byte*)componentData.m_ListData->Ptr,
                    Length = &(componentData.m_ListData->m_length),
                    Capacity = componentData.m_ListData->Capacity,
                    m_Safety = componentData.m_Safety,
                    },
                TypeHandle = typeHandle,
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
            };
            outJobHandle = job.ScheduleParallelByRef(entityQuery, baseIndexJob);

            return componentData;
        }

        /// <summary>
        /// Creates a NativeArray with the value of a single component for all entities matching the provided query.
        /// This function will not sync the needed types in the EntityQueryFilter so they have to be synced manually before calling this function.
        /// </summary>
        /// <param name="allocator">Allocator to use for the array.</param>
        /// <param name="typeHandle">Type handle for the component whose values should be extracted.</param>
        /// <param name="entityCount">Number of entities that match the query. Used as the output array size.</param>
        /// <param name="entityQuery">Entities that match this query will be included in the output.</param>
        /// <returns>NativeArray of all the chunks in the matchingArchetypes list.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static NativeArray<T> CreateComponentDataArray<T>(
            AllocatorManager.AllocatorHandle allocator,
            ref ComponentTypeHandle<T> typeHandle,
            int entityCount,
            EntityQuery entityQuery)
            where T : unmanaged, IComponentData
        {
            var cache = entityQuery.__impl->GetMatchingChunkCache();
            var matchingArchetypes = entityQuery.__impl->_QueryData->MatchingArchetypes;

            var componentData = CollectionHelper.CreateNativeArray<T>(entityCount, allocator, NativeArrayOptions.UninitializedMemory);
            var requiresFilter = entityQuery.HasFilter();
            var hasEnableableComponents = entityQuery.__impl->_QueryData->HasEnableableComponents != 0;
            if (!requiresFilter && !hasEnableableComponents)
            {
                GatherComponentDataWithoutFilter((byte*)componentData.GetUnsafePtr(), typeHandle.m_TypeIndex, in cache, in matchingArchetypes);
            }
            else
            {
                var filter = entityQuery.__impl->_Filter;
                GatherComponentDataWithFilter((byte*)componentData.GetUnsafePtr(), typeHandle.m_TypeIndex, in cache, in matchingArchetypes, ref filter);
            }

            return componentData;
        }

        [BurstCompile]
        private static void CopyComponentArrayToChunksWithFilter(byte* componentData, TypeIndex typeIndex,
            ref UnsafeMatchingArchetypePtrList matchingArchetypePtrList, ref EntityQueryFilter filter, in UnsafeCachedChunkList cache,
            uint globalSystemVersion)
        {
            byte* copySrc = componentData;
            var chunkCacheIterator = new UnsafeChunkCacheIterator(filter, true, cache, matchingArchetypePtrList.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledMask = default;
            LookupCache typeLookupCache = default;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out var chunkEntityCount,
                       out byte useEnableBits, ref chunkEnabledMask))
            {
                var chunkArchetype = chunkCacheIterator._CurrentMatchingArchetype->Archetype;
                var chunkComponentData = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, chunkArchetype,
                    0, typeIndex, globalSystemVersion, ref typeLookupCache);

                if (useEnableBits == 0)
                {
                    UnsafeUtility.MemCpy(chunkComponentData, copySrc, chunkEntityCount * typeLookupCache.ComponentSizeOf);
                    copySrc += chunkEntityCount*typeLookupCache.ComponentSizeOf;
                }
                else
                {
                    // If this branch ever becomes a bottleneck for the many-small-batches case, we could add a third path
                    // that iterates over the chukEnabledMask directly, similar to what IJobEntity generates.
                    int batchEndIndex = 0;
                    while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, batchEndIndex, out int batchStartIndex, out batchEndIndex))
                    {
                        int batchEntityCount = batchEndIndex - batchStartIndex;
                        UnsafeUtility.MemCpy(chunkComponentData+batchStartIndex * typeLookupCache.ComponentSizeOf, copySrc, batchEntityCount * typeLookupCache.ComponentSizeOf);
                        copySrc += batchEntityCount*typeLookupCache.ComponentSizeOf;
                    }
                }
            }
        }

        [BurstCompile]
        public static void CopyComponentArrayToChunksWithoutFilter(byte* componentData, TypeIndex typeIndex, in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypePtrList, uint globalSystemVersion)
        {
            var chunkCacheIterator = new UnsafeChunkCacheIterator(default, false, cachedChunkList, matchingArchetypePtrList.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledBits = default;
            LookupCache typeLookupCache = default;
            byte* copySrc = componentData;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var chunk, out int chunkEntityCount,
                       out byte useEnabledBits, ref chunkEnabledBits))
            {
                var chunkArchetype = chunkCacheIterator._CurrentMatchingArchetype->Archetype;
                var copyDest = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, chunkArchetype, 0, typeIndex,
                    globalSystemVersion, ref typeLookupCache);
                var copySize = typeLookupCache.ComponentSizeOf * chunkEntityCount;
                UnsafeUtility.MemCpy(copyDest, copySrc, copySize);
                copySrc += copySize;
            }
        }

        /// <summary> Obsolete. Use <see cref="CopyFromComponentDataListAsync"/> instead.</summary>
        [Obsolete("Use CopyFromComponentDataListAsync instead. (RemovedAfter Entities 1.0)")]
        public static void CopyFromComponentDataArrayAsync<T>(UnsafeMatchingArchetypePtrList matchingArchetypes,
            NativeArray<T> componentDataArray,
            ref ComponentTypeHandle<T> typeHandle,
            EntityQuery entityQuery,
            ref EntityQueryFilter filter,
            out JobHandle jobHandle,
            JobHandle dependsOn)
            where T : unmanaged, IComponentData
        {
            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);
            var job = new CopyComponentArrayToChunksJob
            {
                ComponentData = (byte*)componentDataArray.GetUnsafePtr(),
                TypeIndex = typeHandle.m_TypeIndex,
                GlobalSystemVersion = typeHandle.GlobalSystemVersion,
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
            };
            jobHandle = job.ScheduleParallel(entityQuery, baseIndexJob);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static void CopyFromComponentDataListAsync<T>(
            NativeList<T> componentDataList,
            ref DynamicComponentTypeHandle typeHandle,
            EntityQuery entityQuery,
            JobHandle dependsOn,
            out JobHandle outJobHandle
            )
            where T : unmanaged, IComponentData
        {
            var chunkBaseEntityIndices =
                entityQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, dependsOn, out var baseIndexJob);
            // Can't pass a generic NativeList<T> to a non-generic job, so we pass individual ptr/length/capacity fields
            // instead (plus a copy of the list's safety handle)
            var job = new CopyComponentListToChunksJob
            {
                InputList = new TypelessUnsafeList{
                    Ptr = (byte*)componentDataList.m_ListData->Ptr,
                    Length = &(componentDataList.m_ListData->m_length),
                    Capacity = componentDataList.m_ListData->Capacity,
                    m_Safety = componentDataList.m_Safety,
                },
                TypeHandle = typeHandle,
                ChunkBaseEntityIndices = chunkBaseEntityIndices,
            };
            outJobHandle = job.ScheduleParallelByRef(entityQuery, baseIndexJob);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public static void CopyFromComponentDataArray<T>(
            NativeArray<T> componentDataArray,
            ref ComponentTypeHandle<T> typeHandle,
            EntityQuery entityQuery)
            where T : unmanaged, IComponentData
        {
            var matchingArchetypePtrList = entityQuery.__impl->_QueryData->MatchingArchetypes;
            var cache = entityQuery.__impl->GetMatchingChunkCache();

            var requiresFilter = entityQuery.HasFilter();
            var hasEnableableComponents = entityQuery.__impl->_QueryData->HasEnableableComponents != 0;
            if (!requiresFilter && !hasEnableableComponents)
            {
                CopyComponentArrayToChunksWithoutFilter((byte*)componentDataArray.GetUnsafePtr(), typeHandle.m_TypeIndex,
                    in cache, in matchingArchetypePtrList, typeHandle.GlobalSystemVersion);
            }
            else
            {
                var filter = entityQuery.__impl->_Filter;
                CopyComponentArrayToChunksWithFilter((byte*)componentDataArray.GetUnsafePtr(), typeHandle.m_TypeIndex,
                    ref matchingArchetypePtrList, ref filter, in cache, typeHandle.GlobalSystemVersion);
            }
        }

        /// <summary>
        ///     Total number of chunks in a given MatchingArchetype list.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <returns>Number of chunks in a list of archetypes.</returns>
        [BurstCompile]
        public static int CalculateChunkCount(in UnsafeCachedChunkList cachedChunkList, ref UnsafeMatchingArchetypePtrList matchingArchetypes,
            ref EntityQueryFilter filter, int queryHasEnableableComponents)
        {
            var totalChunkCount = 0;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableableComponents = queryHasEnableableComponents == 1;
            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableableComponents)
            {
                totalChunkCount = cachedChunkList.Length;
            }
            else if (hasEnableableComponents)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    if (chunkEnabledMask.ULong0 != 0 || chunkEnabledMask.ULong1 != 0)
                        totalChunkCount += 1;
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;
                    totalChunkCount += 1;
                }
            }

            return totalChunkCount;
        }

        [BurstCompile]
        public static bool IsEmpty(ref EntityQueryImpl queryImpl, in EntityQueryFilter filter)
        {
            var chunkCacheIterator = new UnsafeChunkCacheIterator(filter, queryImpl._QueryData->HasEnableableComponents != 0,
                queryImpl.GetMatchingChunkCache(), queryImpl._QueryData->MatchingArchetypes.Ptr);
            int chunkIndex = -1;
            v128 chunkEnabledMask = default;
            while (chunkCacheIterator.MoveNextChunk(ref chunkIndex, out var archetypeChunk, out var chunkEntityCount,
                       out byte chunkUsesEnabledBits, ref chunkEnabledMask))
            {
                // if we make it here at all, we found a chunk with >1 enabled entity, so the query isn't empty
                return false;
            }

            return true;
        }

        [BurstCompile]
        public static void CalculateFilteredChunkIndexArray(in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            ref NativeArray<int> outFilteredChunkIndices)
        {
            var filteredChunkCount = 0;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableable = hasEnableableComponents == 1;
            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableable)
            {
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outFilteredChunkIndices[chunkIndexInCache] = chunkIndexInCache;
                }
            }
            else if (hasEnableable)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outFilteredChunkIndices[chunkIndexInCache] = -1; // default value for filtered-out chunks
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;
                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    if (chunkEnabledMask.ULong0 == 0 && chunkEnabledMask.ULong1 == 0)
                        continue;
                    outFilteredChunkIndices[chunkIndexInCache] = filteredChunkCount++;
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outFilteredChunkIndices[chunkIndexInCache] = -1; // default value for filtered-out chunks
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (!currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;
                    outFilteredChunkIndices[chunkIndexInCache] = filteredChunkCount++;
                }
            }
        }

        [BurstCompile]
        public static void CalculateBaseEntityIndexArray(in UnsafeCachedChunkList cachedChunkList,
            in UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            ref NativeArray<int> outChunkBaseEntityIndices)
        {
            var totalEntityCount = 0;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableable = hasEnableableComponents == 1;
            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableable)
            {
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outChunkBaseEntityIndices[chunkIndexInCache] = totalEntityCount;
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        var currentArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex]->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    totalEntityCount += currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                }
            }
            else if (hasEnableable)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outChunkBaseEntityIndices[chunkIndexInCache] = totalEntityCount;
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    totalEntityCount += EnabledBitUtility.countbits(chunkEnabledMask);
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    outChunkBaseEntityIndices[chunkIndexInCache] = totalEntityCount;
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (!currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];

                    totalEntityCount += chunkEntityCount;
                }
            }
        }

        /// <summary>
        ///     Total number of entities contained in a given MatchingArchetype list.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="filter">EntityQueryFilter to use when calculating total number of entities.</param>
        /// <param name="hasEnableableComponents">True if this query includes any enableable component types</param>
        /// <returns>Number of entities</returns>
        [BurstCompile]
        public static int CalculateEntityCount(in UnsafeCachedChunkList cachedChunkList,
            ref UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents)
        {
            var totalEntityCount = 0;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableable = hasEnableableComponents == 1;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableable)
            {
                int matchingArchetypeCount = matchingArchetypes.Length;
                for (int i = 0; i < matchingArchetypeCount; ++i)
                    totalEntityCount += matchingArchetypesPtr[i]->Archetype->EntityCount;
                return totalEntityCount;
            }

            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            if (hasEnableable)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    totalEntityCount += EnabledBitUtility.countbits(chunkEnabledMask);
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];

                    totalEntityCount += chunkEntityCount;
                }
            }
            return totalEntityCount;
        }

        /// <summary>
        ///     Total number of entities contained in a given MatchingArchetype list.
        /// </summary>
        /// <param name="matchingArchetypes">List of matching archetypes.</param>
        /// <param name="filter">EntityQueryFilter to use when calculating total number of entities.</param>
        /// <param name="hasEnableableComponents">True if this query includes any enableable component types</param>
        /// <returns>Number of entities</returns>
        [BurstCompile]
        public static int CalculateEntityCountAndSingleton(in UnsafeCachedChunkList cachedChunkList,
            ref UnsafeMatchingArchetypePtrList matchingArchetypes, ref EntityQueryFilter filter, int hasEnableableComponents,
            out MatchingArchetype* firstMatchArchetype, out ChunkIndex firstMatchChunk, out int firstMatchEntityIndexInChunk)
        {
            firstMatchArchetype = null;
            firstMatchChunk = ChunkIndex.Null;
            firstMatchEntityIndexInChunk = -1;
            var totalEntityCount = 0;
            var requiresFilter = filter.RequiresMatchesFilter;
            var hasEnableable = hasEnableableComponents == 1;
            var matchingArchetypesPtr = matchingArchetypes.Ptr;
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableable)
            {
                int matchingArchetypeCount = matchingArchetypes.Length;
                for (int i = 0; i < matchingArchetypeCount; ++i)
                {
                    int archetypeEntityCount = matchingArchetypesPtr[i]->Archetype->EntityCount;
                    totalEntityCount += archetypeEntityCount;
                    if (Hint.Unlikely(firstMatchArchetype == null && archetypeEntityCount > 0))
                    {
                        firstMatchArchetype = matchingArchetypesPtr[i];
                        firstMatchChunk = firstMatchArchetype->Archetype->Chunks[0];
                        firstMatchEntityIndexInChunk = 0;
                    }
                }

                return totalEntityCount;
            }

            var chunkMatchingArchetypeIndexPtr = cachedChunkList.PerChunkMatchingArchetypeIndex->Ptr;
            var chunkIndexInArchetypePtr = cachedChunkList.ChunkIndexInArchetype->Ptr;
            int cachedChunkCount = cachedChunkList.Length;
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            if (hasEnableable)
            {
                // per-entity + per-chunk filtering
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState = new EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    int chunkEnabledEntityCount = EnabledBitUtility.countbits(chunkEnabledMask);
                    totalEntityCount += chunkEnabledEntityCount;
                    if (Hint.Unlikely(firstMatchArchetype == null && chunkEnabledEntityCount > 0))
                    {
                        firstMatchArchetype = currentMatchingArchetype;
                        firstMatchChunk = firstMatchArchetype->Archetype->Chunks[chunkIndexInArchetype];
                        firstMatchEntityIndexInChunk = EnabledBitUtility.tzcnt_u128(chunkEnabledMask);
                    }
                }
            }
            else
            {
                // chunk filtering only
                for (int chunkIndexInCache = 0; chunkIndexInCache < cachedChunkCount; ++chunkIndexInCache)
                {
                    if (Hint.Unlikely(chunkMatchingArchetypeIndexPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = chunkMatchingArchetypeIndexPtr[chunkIndexInCache];
                        currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    }
                    int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref filter))
                        continue;

                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];

                    totalEntityCount += chunkEntityCount;
                    if (Hint.Unlikely(firstMatchArchetype == null && chunkEntityCount > 0))
                    {
                        firstMatchArchetype = currentMatchingArchetype;
                        firstMatchChunk = firstMatchArchetype->Archetype->Chunks[chunkIndexInArchetype];
                        firstMatchEntityIndexInChunk = 0;
                    }
                }
            }
            return totalEntityCount;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal static BufferAccessor<T> GetChunkBufferAccessor<T>(Archetype* archetype, ChunkIndex chunk, bool isWriting, int typeIndexInArchetype, uint systemVersion, AtomicSafetyHandle safety0, AtomicSafetyHandle safety1)
            where T : unmanaged, IBufferElementData
        {
            int internalCapacity = archetype->BufferCapacities[typeIndexInArchetype];

            byte* ptr = (!isWriting)
                ? ChunkDataUtility.GetComponentDataRO(chunk, archetype, 0, typeIndexInArchetype)
                : ChunkDataUtility.GetComponentDataRW(chunk, archetype, 0, typeIndexInArchetype, systemVersion);

            var length = chunk.Count;
            int stride = archetype->SizeOfs[typeIndexInArchetype];

            return new BufferAccessor<T>(ptr, length, stride, !isWriting, safety0, safety1, internalCapacity);
        }

        internal static void* GetChunkComponentDataROPtr(Archetype* archetype, ChunkIndex chunk, int indexInArchetype)
        {
            return chunk.Buffer + archetype->Offsets[indexInArchetype];
        }

        // Helper struct to bundle some invariant data when computing the enabled-bit mask for several chunks within the same archetype
        [NoAlias]
        internal struct EnabledMaskMatchingArchetypeState
        {
            [NoAlias] public MatchingArchetype* MatchingArchetype;
            [NoAlias] public int*               MatchingArchetypeAllComponentMemoryOrderIndices;
            [NoAlias] public int*               MatchingArchetypeAnyComponentMemoryOrderIndices;
            [NoAlias] public int*               MatchingArchetypeNoneComponentMemoryOrderIndices;
            [NoAlias] public int*               MatchingArchetypeDisabledComponentMemoryOrderIndices;
            [NoAlias] public v128*              ArchetypeEnabledBits;
            public int                          ArchetypeEnabledBitsPerChunkOffset;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EnabledMaskMatchingArchetypeState(MatchingArchetype* matchingArchetype)
            {
                var chunkData = matchingArchetype->Archetype->Chunks;
                MatchingArchetype = matchingArchetype;
                MatchingArchetypeAllComponentMemoryOrderIndices = matchingArchetype->EnableableTypeMemoryOrderInArchetype_All;
                MatchingArchetypeNoneComponentMemoryOrderIndices = matchingArchetype->EnableableTypeMemoryOrderInArchetype_None;
                MatchingArchetypeAnyComponentMemoryOrderIndices = matchingArchetype->EnableableTypeMemoryOrderInArchetype_Any;
                MatchingArchetypeDisabledComponentMemoryOrderIndices = matchingArchetype->EnableableTypeMemoryOrderInArchetype_Disabled;
                ArchetypeEnabledBits = chunkData.GetComponentEnabledMaskArrayForChunk(0);
                ArchetypeEnabledBitsPerChunkOffset = chunkData.GetComponentEnabledBitsSizePerChunk() / sizeof(v128);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IgnoreWarning(1305)] // Suppresses warning about intrinsic use when compiled with FloatMode=FloatMode.Deterministic
        internal static void GetEnabledMask(int chunkIndexInArchetype, int chunkEntityCount, in EnabledMaskMatchingArchetypeState archetypeState, out v128 enabledMask)
        {
            var matchingArchetype = archetypeState.MatchingArchetype;

            var allComponentCount = matchingArchetype->EnableableComponentsCount_All;
            var noneComponentCount = matchingArchetype->EnableableComponentsCount_None;
            var anyComponentCount = matchingArchetype->EnableableComponentsCount_Any;
            var disabledComponentCount =  matchingArchetype->EnableableComponentsCount_Disabled;

            int* allTypeMemoryOrderIndices = archetypeState.MatchingArchetypeAllComponentMemoryOrderIndices;
            int* noneTypeMemoryOrderIndices = archetypeState.MatchingArchetypeNoneComponentMemoryOrderIndices;
            int* anyTypeMemoryOrderIndices = archetypeState.MatchingArchetypeAnyComponentMemoryOrderIndices;
            int* disabledTypeMemoryOrderIndices = archetypeState.MatchingArchetypeDisabledComponentMemoryOrderIndices;

            var bitsForTypes = archetypeState.ArchetypeEnabledBits + archetypeState.ArchetypeEnabledBitsPerChunkOffset * chunkIndexInArchetype;

            enabledMask = new v128(ulong.MaxValue);
            // At the very least we want to narrow the mask to include only valid entities.
            // The mask for "All" types will implicitly mask out invalid entities, so this is only needed if
            // no All types are present.
            if (Hint.Unlikely(allComponentCount == 0))
            {
                enabledMask = EnabledBitUtility.ShiftRight(new v128(ulong.MaxValue), 128-chunkEntityCount);
            }
            if (X86.Sse2.IsSse2Supported)
            {
                for (int i = 0; i < allComponentCount; ++i) // masks for "All" types are AND'd together
                {
                    var memoryOrderIndexInArchetype = allTypeMemoryOrderIndices[i];
                    enabledMask = X86.Sse2.and_si128(bitsForTypes[memoryOrderIndexInArchetype], enabledMask);
                }
                if (anyComponentCount > 0)
                {
                    v128 anyCombinedMask = bitsForTypes[anyTypeMemoryOrderIndices[0]];
                    for (int i = 1; i < anyComponentCount; ++i) // masks for "Any" types are OR'd with each other, then AND'd with the final result
                    {
                        var memoryOrderIndexInArchetype = anyTypeMemoryOrderIndices[i];
                        anyCombinedMask = X86.Sse2.or_si128(bitsForTypes[memoryOrderIndexInArchetype], anyCombinedMask);
                    }
                    enabledMask = X86.Sse2.and_si128(anyCombinedMask, enabledMask);
                }
                for (int i = 0; i < noneComponentCount; ++i) // masks for "None" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = noneTypeMemoryOrderIndices[i];
                    enabledMask = X86.Sse2.andnot_si128(bitsForTypes[memoryOrderIndexInArchetype], enabledMask);
                }
                for (int i = 0; i < disabledComponentCount; ++i) // masks for "Disabled" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = disabledTypeMemoryOrderIndices[i];
                    enabledMask = X86.Sse2.andnot_si128(bitsForTypes[memoryOrderIndexInArchetype], enabledMask);
                }
            }
            else if (Arm.Neon.IsNeonSupported)
            {
                for (int i = 0; i < allComponentCount; ++i) // masks for "All" types are AND'd together
                {
                    var memoryOrderIndexInArchetype = allTypeMemoryOrderIndices[i];
                    enabledMask = Arm.Neon.vandq_u64(bitsForTypes[memoryOrderIndexInArchetype], enabledMask);
                }
                if (anyComponentCount > 0)
                {
                    v128 anyCombinedMask = bitsForTypes[anyTypeMemoryOrderIndices[0]];
                    for (int i = 1; i < anyComponentCount; ++i) // masks for "Any" types are OR'd with each other, then AND'd with the final result
                    {
                        var memoryOrderIndexInArchetype = anyTypeMemoryOrderIndices[i];
                        anyCombinedMask = Arm.Neon.vorrq_u64(bitsForTypes[memoryOrderIndexInArchetype], anyCombinedMask);
                    }
                    enabledMask = Arm.Neon.vandq_u64(anyCombinedMask, enabledMask);
                }
                for (int i = 0; i < noneComponentCount; ++i) // masks for "None" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = noneTypeMemoryOrderIndices[i];
                    enabledMask = Arm.Neon.vandq_u64(Arm.Neon.vmvnq_u32(bitsForTypes[memoryOrderIndexInArchetype]), enabledMask);
                }
                for (int i = 0; i < disabledComponentCount; ++i) // masks for "Disabled" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = disabledTypeMemoryOrderIndices[i];
                    enabledMask = Arm.Neon.vandq_u64(Arm.Neon.vmvnq_u32(bitsForTypes[memoryOrderIndexInArchetype]), enabledMask);
                }
            }
            else
            {
                for (int i = 0; i < allComponentCount; ++i) // masks for "All" types are AND'd together
                {
                    var memoryOrderIndexInArchetype = allTypeMemoryOrderIndices[i];
                    enabledMask.ULong0 = bitsForTypes[memoryOrderIndexInArchetype].ULong0 & enabledMask.ULong0;
                    enabledMask.ULong1 = bitsForTypes[memoryOrderIndexInArchetype].ULong1 & enabledMask.ULong1;
                }
                if (anyComponentCount > 0)
                {
                    v128 anyCombinedMask = new v128(0);
                    for (int i = 0; i < anyComponentCount; ++i) // masks for "Any" types are OR'd with each other, then AND'd with the final result
                    {
                        var memoryOrderIndexInArchetype = anyTypeMemoryOrderIndices[i];
                        anyCombinedMask.ULong0 = bitsForTypes[memoryOrderIndexInArchetype].ULong0 | anyCombinedMask.ULong0;
                        anyCombinedMask.ULong1 = bitsForTypes[memoryOrderIndexInArchetype].ULong1 | anyCombinedMask.ULong1;
                    }
                    enabledMask.ULong0 = anyCombinedMask.ULong0 & enabledMask.ULong0;
                    enabledMask.ULong1 = anyCombinedMask.ULong1 & enabledMask.ULong1;
                }
                for (int i = 0; i < noneComponentCount; ++i) // masks for "None" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = noneTypeMemoryOrderIndices[i];
                    enabledMask.ULong0 = (~bitsForTypes[memoryOrderIndexInArchetype].ULong0 & enabledMask.ULong0);
                    enabledMask.ULong1 = (~bitsForTypes[memoryOrderIndexInArchetype].ULong1 & enabledMask.ULong1);
                }

                for (int i = 0; i < disabledComponentCount; ++i) // masks for "Disabled" types are negated and AND'd together
                {
                    var memoryOrderIndexInArchetype = disabledTypeMemoryOrderIndices[i];
                    enabledMask.ULong0 = (~bitsForTypes[memoryOrderIndexInArchetype].ULong0 & enabledMask.ULong0);
                    enabledMask.ULong1 = (~bitsForTypes[memoryOrderIndexInArchetype].ULong1 & enabledMask.ULong1);
                }
            }
        }

        [BurstCompile]
        public static void GetEnabledMask(ChunkIndex chunk, MatchingArchetype* matchingArchetype, out v128 enabledMask)
        {
            Assert.IsTrue(matchingArchetype->Archetype->ChunkCapacity <= 128);
            GetEnabledMask(chunk.ListIndex, chunk.Count, new EnabledMaskMatchingArchetypeState(matchingArchetype), out enabledMask);
        }
        internal static void GetEnabledMaskNoBurstForTests(ChunkIndex chunk, MatchingArchetype* matchingArchetype, out v128 enabledMask)
        {
            Assert.IsTrue(matchingArchetype->Archetype->ChunkCapacity <= 128);
            GetEnabledMask(chunk.ListIndex, chunk.Count, new EnabledMaskMatchingArchetypeState(matchingArchetype), out enabledMask);
        }


        [BurstCompile]
        public static void SetEnabledBitsOnAllChunks(ref EntityQueryImpl queryImpl, TypeIndex typeIndex, bool value,
            uint globalSystemVersion)
        {
            var chunkList = queryImpl.GetMatchingChunkCache();
            var chunkCount = chunkList.Length;
            var chunkIndices = chunkList.ChunkIndices;
            int* chunkIndexInArchetypePtr = chunkList.ChunkIndexInArchetype->Ptr;
            MatchingArchetype** matchingArchetypesPtr = queryImpl._QueryData->MatchingArchetypes.Ptr;
            int* matchingArchetypeIndicesPtr = chunkList.PerChunkMatchingArchetypeIndex->Ptr;
            bool requiresFilter = queryImpl._Filter.RequiresMatchesFilter;

            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            v128* chunkEnabledBitsForType = null;
            int* chunkDisabledCountForType = null;
            int chunkEnabledBitsIncrement = 0;
            int* chunkEntityCountsPtr = null;
            uint* chunkChangeVersionsForType = null;
            for (int chunkIndexInCache = 0; chunkIndexInCache < chunkCount; ++chunkIndexInCache)
            {
                var chunk = chunkIndices[chunkIndexInCache];
                if (Hint.Unlikely(matchingArchetypeIndicesPtr[chunkIndexInCache] != currentMatchingArchetypeIndex))
                {
                    // Update per-archetype state
                    currentMatchingArchetypeIndex = matchingArchetypeIndicesPtr[chunkIndexInCache];
                    currentMatchingArchetype = matchingArchetypesPtr[currentMatchingArchetypeIndex];
                    var currentArchetype = currentMatchingArchetype->Archetype;
                    chunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                    var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(currentArchetype, typeIndex);
                    int memoryOrderIndexInArchetype = currentArchetype->TypeIndexInArchetypeToMemoryOrderIndex[typeIndexInArchetype];
                    chunkEnabledBitsForType = currentArchetype->Chunks.GetComponentEnabledMaskArrayForTypeInChunk(memoryOrderIndexInArchetype, 0);
                    chunkDisabledCountForType =
                        currentArchetype->Chunks.GetPointerToChunkDisabledCountForType(memoryOrderIndexInArchetype, 0);
                    chunkEnabledBitsIncrement = currentArchetype->Chunks.GetComponentEnabledBitsSizePerChunk() / sizeof(v128);
                    chunkChangeVersionsForType = currentArchetype->Chunks.GetChangeVersionArrayForType(typeIndexInArchetype);
                }

                if (requiresFilter && !chunk.MatchesFilter(currentMatchingArchetype, ref queryImpl._Filter))
                    continue;

                int chunkIndexInArchetype = chunkIndexInArchetypePtr[chunkIndexInCache];
                int chunkEntityCount = chunkEntityCountsPtr[chunkIndexInArchetype];
                chunkChangeVersionsForType[chunkIndexInArchetype] = globalSystemVersion;
                if (value)
                {
                    if (chunkEntityCount < 64)
                        *(chunkEnabledBitsForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = new v128((1UL << chunkEntityCount)-1, 0UL);
                    else if (chunkEntityCount < 128)
                        *(chunkEnabledBitsForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = new v128(ulong.MaxValue, (1UL << (chunkEntityCount-64))-1);
                    else
                        *(chunkEnabledBitsForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = new v128(ulong.MaxValue, ulong.MaxValue);
                    *(chunkDisabledCountForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = 0;
                }
                else
                {
                    *(chunkEnabledBitsForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = default(v128);
                    *(chunkDisabledCountForType + chunkEnabledBitsIncrement * chunkIndexInArchetype) = chunkEntityCount;
                }
            }
        }
    }

    /// <summary>
    /// Utilities to help manipulate the per-component enabled bits within a given chunk.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    [BurstCompile]
    internal static class EnabledBitUtility
    {
        /// <summary>
        /// Retrieves the next contiguous range of set bits starting at or after the provided index in the provided mask.
        /// </summary>
        /// <param name="mask">The enabled-bit mask.</param>
        /// <param name="firstIndexToCheck">The index at which to start checking for enabled bits.
        /// This method ignores all bits whose indices are smaller than the index provided here.</param>
        /// <param name="nextRangeBegin">The index at which the first enabled bit is found.</param>
        /// <param name="nextRangeEnd">The index of the first bit NOT in the next range.</param>
        /// <returns>True if another range of contiguous bits was found (in which case, the range info is stored in
        /// <paramref name="nextRangeBegin"/> and <paramref name="nextRangeEnd"/>. Otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetNextRange(v128 mask, int firstIndexToCheck, out int nextRangeBegin, out int nextRangeEnd)
        {
            mask = ShiftRight(mask, firstIndexToCheck);

            // count zero bits before first enabled bit (that's our start)
            var start = tzcnt_u128(mask);
            mask = ShiftRight(mask, start);

            // The mask has been shifted so now we need to find the first disabled bit (that's the count)
            v128 negatedMask = new v128(~mask.ULong0, ~mask.ULong1);
            var count = tzcnt_u128(negatedMask);
            nextRangeBegin = firstIndexToCheck + start;
            nextRangeEnd = nextRangeBegin + count;

            return count != 0;
        }

        /// <summary>
        /// Performs a binary logical shift to the right.
        /// </summary>
        /// <param name="v">The value to be shifted</param>
        /// <param name="n">The amount of bits to shift</param>
        /// <returns>The shifted value</returns>
        internal static v128 ShiftRight(in v128 v, int n)
        {
            if (Hint.Unlikely(n >= 128))
                return default;
            if (Hint.Unlikely(n == 0))
                return v;
            if (n >= 64)
                return new v128(v.ULong1 >> (n-64), 0);
            // 0 < n < 64
            ulong lowToLow = v.ULong0 >> n;
            ulong highToLow = v.ULong1 << (64 - n);
            return new v128(lowToLow | highToLow, v.ULong1 >> n);
        }

        [BurstCompile]
        internal static void ShiftRightBurstForTests(ref v128 v, out v128 vOut, int n)
        {
            vOut = ShiftRight(v, n);
        }

        internal static int tzcnt_u128 (v128 u)
        {
            int hi = math.tzcnt(u.ULong1) + 64;
            int lo = math.tzcnt(u.ULong0);
            return lo == 64 ? hi : lo;
        }

        internal static int countbits (v128 u)
        {
            return math.countbits(u.ULong0) + math.countbits(u.ULong1);
        }
    }
}
