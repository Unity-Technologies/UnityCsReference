// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Jobs;
using UnityEngine;

using System.Text;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    /// <summary>
    /// The exception throw when encountering multiple entities with the same <see cref="EntityGuid"/> value.
    /// </summary>
    [Serializable]
    class DuplicateEntityGuidException : Exception
    {
        /// <summary>
        /// The duplicate guids found in during the diff and the counts of how many times they were duplicated.
        /// </summary>
        public DuplicateEntityGuid[] DuplicateEntityGuids { get; private set; }

        /// <summary>
        /// Initialized a new instance of the <see cref="DuplicateEntityGuidException"/> class.
        /// </summary>
        public DuplicateEntityGuidException(DuplicateEntityGuid[] duplicates)
            : base(CreateMessage(duplicates)) { DuplicateEntityGuids = duplicates; }

        static string CreateMessage(DuplicateEntityGuid[] duplicates)
        {
            var message = $"Found {duplicates.Length} {nameof(EntityGuid)} components that are shared by more than one Entity";

            message += "\n" + ToString(duplicates);

            return message;
        }

        static string ToString(DuplicateEntityGuid[] duplicates)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < duplicates.Length; ++i)
            {
                if (i == 10)
                {
                    sb.AppendLine($"...{duplicates.Length - i} more...");
                    break;
                }

                var dup = duplicates[i];
                var obj = CoreModuleLightShim.Hooks.EntityIdToObject(dup.EntityGuid.OriginatingEntityId);
                var name = obj != null ? obj.ToString() : "<not found>";

                sb.AppendLine($"guid = {dup.EntityGuid}, count = {dup.DuplicateCount}, obj = {name}");
            }

            return sb.ToString();
        }

        public override string ToString() => ToString(DuplicateEntityGuids);
    }

    /// <summary>
    /// Parameters used to configure the the execution of the differ.
    /// </summary>
    [Flags]
    public enum EntityManagerDifferOptions
    {
        /// <summary>
        /// Shortcut for "no options required".
        /// </summary>
        None = 0,

        /// <summary>
        /// If set; the resulting <see cref="EntityChanges"/> will include the forward change set.
        /// </summary>
        IncludeForwardChangeSet = 1 << 1,

        /// <summary>
        /// If set; the resulting <see cref="EntityChanges"/> will include the reverse change set.
        ///
        /// This can be applied to the world to reverse the changes (i.e. undo).
        /// </summary>
        IncludeReverseChangeSet = 1 << 2,

        /// <summary>
        /// If set; the shadow world will be updated with the latest changes.
        /// </summary>
        FastForwardShadowWorld = 1 << 3,

        /// <summary>
        /// If set; all references to destroyed or missing entities will be set to Entity.Null before computing changes.
        ///
        /// When applying a change this is needed to patch references to restored entities (they were destroyed but are being brought back by the change set).
        /// </summary>
        /// <remarks>
        /// Performance scales with the total number of entities with the <see cref="EntityGuid"/> component in the source world.
        /// </remarks>
        ClearMissingReferences = 1 << 4,

        /// <summary>
        /// If this flag is set; the entire world is checks for duplicate <see cref="EntityGuid"/> components.
        /// </summary>
        /// <remarks>
        /// Performance scales with the number of created entities in the source world with the <see cref="EntityGuid"/> component.
        /// </remarks>
        ValidateUniqueEntityGuid = 1 << 5,

        /// <summary>
        /// If set, components are not compared bit-wise. Bit-wise comparison implies that two components that
        /// have references to entities that have the same GUID but different indices/versions are different.
        /// Similarly blob asset references to blob assets that have the same hash but live at different addresses
        /// will be considered different as well. This is often not desirable. For these cases, it is more apt to
        /// check that GUIDs and hashes match.
        /// </summary>
        /// /// <remarks>
        /// This makes comparing components potentially more expensive.
        /// </remarks>
        UseReferentialEquality = 1 << 6,

        /// <summary>
        /// The default set of options used by the <see cref="EntityDiffer"/>
        /// </summary>
        Default = IncludeForwardChangeSet |
            IncludeReverseChangeSet |
            FastForwardShadowWorld |
            ClearMissingReferences |
            ValidateUniqueEntityGuid
    }


    /// <summary>
    /// The <see cref="EntityDiffer"/> is used to build a set of changes between two worlds.
    /// </summary>
    /// <remarks>
    /// This class can be used to determine both forward and/or reverse changes between the worlds.
    ///
    /// This class relies on the <see cref="EntityGuid"/> to uniquely identify entities, and expects that each entity
    /// will have a unique value for this component. If any duplicate <see cref="EntityGuid"/> values are encountered
    /// a <see cref="DuplicateEntityGuidException"/> will be thrown.
    ///
    /// <seealso cref="EntityManagerDiffer"/> for tracking changes over time.
    /// </remarks>
    unsafe partial class EntityDiffer
    {
        const string s_GetChangesProfilerMarkerStr = "GetChanges";

        static readonly EntitiesProfilerMarker s_GetChangesProfilerMarker = new EntitiesProfilerMarker(s_GetChangesProfilerMarkerStr);
        static readonly EntitiesProfilerMarker s_CreateEntityChangeSetProfilerMarker = new EntitiesProfilerMarker(nameof(CreateEntityChangeSet));
        static readonly EntitiesProfilerMarker s_GetEntityNamesProfilerMarker = new EntitiesProfilerMarker(nameof(GetEntityNames));
        static readonly EntitiesProfilerMarker s_GetChangedSharedComponentsProfilerMarker = new EntitiesProfilerMarker(nameof(GetChangedSharedComponents));
        static readonly EntitiesProfilerMarker s_CopyAndReplaceChunksProfilerMarker = new EntitiesProfilerMarker(nameof(CopyAndReplaceChunks));
        static readonly EntitiesProfilerMarker s_DestroyChunksProfilerMarker = new EntitiesProfilerMarker(nameof(DestroyChunks));
        static readonly EntitiesProfilerMarker s_CloneAndAddChunksProfilerMarker = new EntitiesProfilerMarker(nameof(CloneAndAddChunks));
        static readonly EntitiesProfilerMarker s_GetBlobAssetsWithDistinctHash = new EntitiesProfilerMarker(nameof(GetBlobAssetsWithDistinctHash));

        internal static string[] CollectImportantProfilerMarkerStrings()
        {
            return new string [] {
                s_GetChangesProfilerMarkerStr
            };
        }

        /// <summary>
        /// CachedComponentChanges are used to not reallocate the data between each GetChanges call as this can cause big
        /// performance problems in cases where there is lots of changes happening.
        /// </summary>
        public struct CachedComponentChanges
        {
            public ComponentChanges ForwardComponentChanges;
            public ComponentChanges ReverseComponentChanges;

            public CachedComponentChanges(int count)
            {
                ForwardComponentChanges = new ComponentChanges(count);
                ReverseComponentChanges = new ComponentChanges(count);
            }
            public void Dispose()
            {
                ForwardComponentChanges.Dispose();
                ReverseComponentChanges.Dispose();
            }
        }

        /// <summary>
        /// Generates a detailed change set between <see cref="srcEntityManager"/> and <see cref="dstEntityManager"/>.
        /// All entities to be considered must have the <see cref="EntityGuid"/> component with a unique value.
        /// The resulting <see cref="Entities.EntityChanges"/> must be disposed when no longer needed.
        /// </summary>
        /// <remarks>
        /// When using the <see cref="EntityManagerDifferOptions.FastForwardShadowWorld"/> the destination world must be a direct ancestor to
        /// the source world, and must only be updated using this call or similar methods. There should be no direct changes to destination world.
        /// </remarks>
        internal static EntityChanges GetChanges(
            ref CachedComponentChanges cachedComponentChanges,
            EntityManager srcEntityManager,
            EntityManager dstEntityManager,
            EntityManagerDifferOptions options,
            EntityQueryDesc entityQueryDesc,
            BlobAssetCache blobAssetCache,
            AllocatorManager.AllocatorHandle allocator)
        {
            var changes = default(EntityChanges);

            using (s_GetChangesProfilerMarker.Auto())
            {
                CheckEntityGuidComponent(entityQueryDesc);

                if (options == EntityManagerDifferOptions.None)
                    return changes;

                srcEntityManager.CompleteAllTrackedJobs();
                dstEntityManager.CompleteAllTrackedJobs();

                var srcEntityQuery = srcEntityManager.CreateEntityQuery(entityQueryDesc);
                var dstEntityQuery = dstEntityManager.CreateEntityQuery(entityQueryDesc);
                int maxSrcChunkCount = srcEntityQuery.CalculateChunkCountWithoutFiltering();

                // Gather a set of a chunks to consider for diffing in both the src and dst worlds.
                using (var srcChunks = srcEntityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var srcChunksJob))
                using (var dstChunks = dstEntityQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out var dstChunksJob))
                {
                    JobHandle clearMissingReferencesJob = default;

                    if (CheckOption(options, EntityManagerDifferOptions.ClearMissingReferences))
                    {
                        // Opt-in feature.
                        // This is a special user case for references to destroyed entities.
                        // If entity is destroyed, any references to that entity will remain set but become invalid (i.e. broken).
                        // This option ensures that references to non-existent entities will be explicitly set to Entity.Null which
                        // will force it to be picked up in the change set.
                        ClearMissingReferences(srcEntityManager, srcChunks, out clearMissingReferencesJob, srcChunksJob);
                    }

                    var archetypeChunkChangesJobDependencies = JobHandle.CombineDependencies(srcChunksJob, dstChunksJob, clearMissingReferencesJob);

                    // Broad phased chunk comparison.
                    using (var archetypeChunkChanges = GetArchetypeChunkChanges(
                        srcChunks: srcChunks,
                        dstChunks: dstChunks,
                        maxSrcChunkCount,
                        allocator: Allocator.TempJob,
                        jobHandle: out var archetypeChunkChangesJob,
                        dependsOn: archetypeChunkChangesJobDependencies))
                    {
                        // Explicitly sync at this point to parallelize subsequent jobs by chunk.
                        archetypeChunkChangesJob.Complete();

                        // Gather a sorted set of entities based on which chunks have changes.
                        using (var srcEntities = GetSortedEntitiesInChunk(
                            srcEntityManager,
                            archetypeChunkChanges.CreatedSrcChunks, Allocator.TempJob,
                            jobHandle: out var srcEntitiesJob))
                        using (var dstEntities = GetSortedEntitiesInChunk(
                            dstEntityManager,
                            archetypeChunkChanges.DestroyedDstChunks, Allocator.TempJob,
                            jobHandle: out var dstEntitiesJob))
                        using (var srcBlobAssetsWithDistinctHash = GetBlobAssetsWithDistinctHash(
                            srcEntityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                            srcChunks.AsArray(), Allocator.TempJob))
                        using (var dstBlobAssetsWithDistinctHash = blobAssetCache.BlobAssetBatch->ToNativeList(Allocator.TempJob))
                        {
                            var duplicateEntityGuids = default(NativeList<DuplicateEntityGuid>);
                            var forwardEntityChanges = default(EntityInChunkChanges);
                            var reverseEntityChanges = default(EntityInChunkChanges);
                            var forwardBlobAssetChanges = default(BlobAssetChanges);
                            var reverseBlobAssetChanges = default(BlobAssetChanges);

                            try
                            {
                                JobHandle getDuplicateEntityGuidsJob = default;
                                JobHandle forwardChangesJob = default;
                                JobHandle reverseChangesJob = default;

                                if (CheckOption(options, EntityManagerDifferOptions.ValidateUniqueEntityGuid))
                                {
                                    // Guid validation will happen incrementally and only consider changed entities in the source world.
                                    duplicateEntityGuids = GetDuplicateEntityGuids(
                                        srcEntities, Allocator.TempJob,
                                        jobHandle: out getDuplicateEntityGuidsJob,
                                        dependsOn: srcEntitiesJob);
                                }

                                if (CheckOption(options, EntityManagerDifferOptions.IncludeForwardChangeSet))
                                {
                                    forwardEntityChanges = GetEntityInChunkChanges(
                                        srcEntityManager,
                                        dstEntityManager,
                                        srcEntities,
                                        dstEntities,
                                        Allocator.TempJob,
                                        jobHandle: out var forwardEntityChangesJob,
                                        dependsOn: JobHandle.CombineDependencies(srcEntitiesJob, dstEntitiesJob));

                                    // We need to wait here in order to read the data for pre-allocating the componentChanges sizes
                                    forwardEntityChangesJob.Complete();

                                    cachedComponentChanges.ForwardComponentChanges.ResizeAndClear(
                                        forwardEntityChanges.CreatedEntities.Length +
                                        forwardEntityChanges.ModifiedEntities.Length +
                                        forwardEntityChanges.DestroyedEntities.Length);

                                    JobHandle forwardComponentChangesJob = default;

                                    GetComponentChanges(
                                        ref cachedComponentChanges.ForwardComponentChanges,
                                        forwardEntityChanges,
                                        (options & EntityManagerDifferOptions.UseReferentialEquality) != 0,
                                        default,
                                        blobAssetCache.BlobAssetRemap,
                                        Allocator.TempJob,
                                        jobHandle: out forwardComponentChangesJob);

                                    forwardBlobAssetChanges = GetBlobAssetChanges(
                                        srcBlobAssetsWithDistinctHash.BlobAssets,
                                        dstBlobAssetsWithDistinctHash,
                                        Allocator.TempJob,
                                        jobHandle: out var forwardBlobAssetsChangesJob);

                                    forwardChangesJob = JobHandle.CombineDependencies(forwardComponentChangesJob, forwardBlobAssetsChangesJob);
                                }

                                if (CheckOption(options, EntityManagerDifferOptions.IncludeReverseChangeSet))
                                {
                                    reverseEntityChanges = GetEntityInChunkChanges(
                                        dstEntityManager,
                                        srcEntityManager,
                                        dstEntities,
                                        srcEntities,
                                        Allocator.TempJob,
                                        jobHandle: out var reverseEntityChangesJob,
                                        dependsOn: JobHandle.CombineDependencies(srcEntitiesJob, dstEntitiesJob));

                                    // We need to wait here in order to read the data for pre-allocating the componentChanges sizes
                                    reverseEntityChangesJob.Complete();
                                    reverseEntityChangesJob = default;

                                    cachedComponentChanges.ReverseComponentChanges.ResizeAndClear(
                                        reverseEntityChanges.CreatedEntities.Length +
                                        reverseEntityChanges.ModifiedEntities.Length +
                                        reverseEntityChanges.DestroyedEntities.Length);

                                    GetComponentChanges(
                                        ref cachedComponentChanges.ReverseComponentChanges,
                                        reverseEntityChanges,
                                        (options & EntityManagerDifferOptions.UseReferentialEquality) != 0,
                                        blobAssetCache.BlobAssetRemap,
                                        default,
                                        Allocator.TempJob,
                                        jobHandle: out var reverseComponentChangesJob,
                                        dependsOn: reverseEntityChangesJob);

                                    reverseBlobAssetChanges = GetBlobAssetChanges(
                                        dstBlobAssetsWithDistinctHash,
                                        srcBlobAssetsWithDistinctHash.BlobAssets,
                                        Allocator.TempJob,
                                        jobHandle: out var reverseBlobAssetsChangesJob);

                                    reverseChangesJob = JobHandle.CombineDependencies(reverseComponentChangesJob, reverseBlobAssetsChangesJob);
                                }

                                JobHandle jobHandle;

                                using (var jobs = new NativeList<JobHandle>(5, Allocator.Temp))
                                {
                                    jobs.Add(clearMissingReferencesJob);
                                    jobs.Add(getDuplicateEntityGuidsJob);

                                    if (CheckOption(options, EntityManagerDifferOptions.IncludeForwardChangeSet) ||
                                        CheckOption(options, EntityManagerDifferOptions.IncludeReverseChangeSet))
                                    {
                                        jobs.Add(forwardChangesJob);
                                        jobs.Add(reverseChangesJob);
                                    }
                                    else
                                    {
                                        jobs.Add(srcEntitiesJob);
                                        jobs.Add(dstEntitiesJob);
                                    }

                                    jobHandle = JobHandle.CombineDependencies(jobs.AsArray());
                                }

                                jobHandle.Complete();

                                if (duplicateEntityGuids.IsCreated && duplicateEntityGuids.Length > 0)
                                    throw new DuplicateEntityGuidException(duplicateEntityGuids.AsArray().ToArray());

                                var forwardChangeSet = CreateEntityChangeSet(forwardEntityChanges, cachedComponentChanges.ForwardComponentChanges, forwardBlobAssetChanges, allocator);
                                var reverseChangeSet = CreateEntityChangeSet(reverseEntityChanges, cachedComponentChanges.ReverseComponentChanges, reverseBlobAssetChanges, allocator);

                                changes = new EntityChanges(forwardChangeSet, reverseChangeSet);

                                if (CheckOption(options, EntityManagerDifferOptions.FastForwardShadowWorld))
                                {
                                    CopyAndReplaceChunks(srcEntityManager, dstEntityManager, dstEntityQuery, archetypeChunkChanges);
                                    UpdateBlobAssetCache(blobAssetCache, srcBlobAssetsWithDistinctHash.BlobAssets,
                                        dstBlobAssetsWithDistinctHash);
                                }
                            }
                            catch (Exception e)
                            {
                                // Throw a message for the user
                                Debug.LogException(e);
                                // Re-Throw with all internal data (doesn't show up in log)
                                throw e;
                            }
                            finally
                            {
                                if (duplicateEntityGuids.IsCreated) duplicateEntityGuids.Dispose();
                                if (forwardEntityChanges.IsCreated) forwardEntityChanges.Dispose();
                                if (reverseEntityChanges.IsCreated) reverseEntityChanges.Dispose();
                                if (forwardBlobAssetChanges.IsCreated) forwardBlobAssetChanges.Dispose();
                                if (reverseBlobAssetChanges.IsCreated) reverseBlobAssetChanges.Dispose();
                            }
                        }
                    }
                }
            }

            return changes;
        }

        internal static void PrecomputeBlobAssetCache(EntityManager entityManager, EntityQueryDesc entityQueryDesc, BlobAssetCache blobAssetCache)
        {
            CheckEntityGuidComponent(entityQueryDesc);
            entityManager.CompleteAllTrackedJobs();
            var srcEntityQuery = entityManager.CreateEntityQuery(entityQueryDesc);
            // Gather a set of a chunks to consider for diffing in both the src and dst worlds.
            using (var srcChunks = srcEntityQuery.ToArchetypeChunkArray(Allocator.TempJob))
            {
                using (var srcBlobAssetsWithDistinctHash = GetBlobAssetsWithDistinctHash(
                    entityManager.GetCheckedEntityDataAccess()->EntityComponentStore,
                    srcChunks, Allocator.TempJob))
                using (var dstBlobAssets = new NativeList<BlobAssetPtr>(Allocator.TempJob))
                {
                    UpdateBlobAssetCache(blobAssetCache, srcBlobAssetsWithDistinctHash.BlobAssets, dstBlobAssets);
                }
            }
        }

        static void UpdateBlobAssetCache(BlobAssetCache blobAssetCache, NativeList<BlobAssetPtr> srcBlobAssets, NativeList<BlobAssetPtr> dstBlobAssets)
        {
            var batch = blobAssetCache.BlobAssetBatch;
            var remap = blobAssetCache.BlobAssetRemap;

            using (var createdBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob))
            using (var destroyedBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob))
            using (var sameHashDifferentAddressBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob))
            {
                new GatherCreatedAndDestroyedBlobAssets
                {
                    CreatedBlobAssets = createdBlobAssets,
                    DestroyedBlobAssets = destroyedBlobAssets,
                    AfterBlobAssets = srcBlobAssets,
                    BeforeBlobAssets = dstBlobAssets,
                    SameHashDifferentAddressBlobAssets = sameHashDifferentAddressBlobAssets
                }.Run();

                var toDelete = new NativeList<BlobAssetPtr>(Allocator.Temp);

                for (var i = 0; i < destroyedBlobAssets.Length; i++)
                {
                    if (!batch->TryGetBlobAsset(destroyedBlobAssets[i].Hash, out _))
                    {
                        throw new Exception(
                            $"Failed to destroy a BlobAsset to the shadow world. A BlobAsset with the Hash=[{createdBlobAssets[i].Header->Hash}] does not exists.");
                    }

                    batch->ReleaseBlobAssetImmediately(destroyedBlobAssets[i].Hash);

                    foreach (var keyValue in remap)
                    {
                        if (destroyedBlobAssets[i].Data != keyValue.Value.Data)
                            continue;
                        toDelete.Add(keyValue.Key);
                        break;
                    }
                }

                foreach (var blobAssetPtr in toDelete)
                {
                    remap.Remove(blobAssetPtr);
                }
                toDelete.Clear();

                {
                    var deferredAdd = new NativeArray<(BlobAssetPtr key, BlobAssetPtr value)>(sameHashDifferentAddressBlobAssets.Length, Allocator.Temp);
                    var deferredAddCount = 0;
                    foreach (var update in sameHashDifferentAddressBlobAssets)
                    {
                        foreach (var keyValue in remap)
                        {
                            if (keyValue.Value.Hash != update.Hash)
                                continue;

                            if (keyValue.Key.Data != update.Data)
                            {
                                toDelete.Add(keyValue.Key);
                                deferredAdd[deferredAddCount] = (update, keyValue.Value);
                                deferredAddCount += 1;
                            }

                            break;
                        }
                    }

                    foreach (var blobAssetPtr in toDelete)
                    {
                        remap.Remove(blobAssetPtr);
                    }

                    for (int i = 0; i < deferredAddCount; i++)
                    {
                        remap.Add(deferredAdd[i].key, deferredAdd[i].value);
                    }
                }

                // NOTE : it's important that adding the new blobs is done AFTER remapping the existing ones.
                // Because if the address of a previously existing blob has been reused, adding it before
                // updating the remap table would try to insert the same key twice.
                for (var i = 0; i < createdBlobAssets.Length; i++)
                {
                    if (batch->TryGetBlobAsset(createdBlobAssets[i].Header->Hash, out _))
                    {
                        throw new Exception(
                            $"Failed to copy a BlobAsset to the shadow world. A BlobAsset with the Hash=[{createdBlobAssets[i].Header->Hash}] already exists.");
                    }

                    var blobAssetPtr = batch->AllocateBlobAsset(createdBlobAssets[i].Data,
                        createdBlobAssets[i].Length, createdBlobAssets[i].Header->Hash);
                    remap.Add(createdBlobAssets[i], blobAssetPtr);
                }

                if (destroyedBlobAssets.Length > 0 || createdBlobAssets.Length > 0)
                {
                    batch->SortByHash();
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckEntityGuidComponent(EntityQueryDesc entityQueryDesc)
        {
            foreach (var type in entityQueryDesc.All)
            {
                if (type.GetManagedType() == typeof(EntityGuid))
                {
                    return;
                }
            }

            throw new ArgumentException($"{nameof(EntityDiffer)} custom query requires an {nameof(EntityGuid)} component in the All filter.");
        }

        static JobHandle CombineDependencies(JobHandle job1, JobHandle job2, JobHandle job3)
        {
            var array = new NativeArray<JobHandle>(3, Allocator.Temp)
            {
                [0] = job1,
                [1] = job2,
                [2] = job2
            };

            var jobHandle = JobHandle.CombineDependencies(array);

            array.Dispose();

            return jobHandle;
        }

        static bool CheckOption(EntityManagerDifferOptions options, EntityManagerDifferOptions option)
            => (options & option) == option;
    }
}
