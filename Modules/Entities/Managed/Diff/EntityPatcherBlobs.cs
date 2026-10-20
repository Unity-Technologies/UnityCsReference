// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    [DisableAutoCreation]
    unsafe partial class EntityPatcherBlobAssetSystem : SystemBase
    {
        DynamicBlobAssetBatch* m_BlobAssetBatchPtr;

        protected override void OnCreate()
        {
            m_BlobAssetBatchPtr = DynamicBlobAssetBatch.Allocate(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            DynamicBlobAssetBatch.Free(m_BlobAssetBatchPtr);
            m_BlobAssetBatchPtr = null;
        }

        public void SetFramesToRetainBlobAssets(int framesToRetainBlobAssets)
        {
            m_BlobAssetBatchPtr->SetFramesToRetainBlobAssets(framesToRetainBlobAssets);
        }

        public void AllocateBlobAsset(void* ptr, int len, ulong hash)
        {
            m_BlobAssetBatchPtr->AllocateBlobAsset(ptr, len, hash);
        }

        public void ReleaseBlobAsset(EntityManager entityManager, ulong hash)
        {
            m_BlobAssetBatchPtr->ReleaseBlobAsset(entityManager, hash);
        }

        public bool TryGetBlobAsset(ulong hash, out BlobAssetPtr ptr)
        {
            return m_BlobAssetBatchPtr->TryGetBlobAsset(hash, out ptr);
        }

        public void ReleaseUnusedBlobAssets()
        {
            using (var chunks = EntityManager.CreateEntityQuery(EntityManagerDiffer.EntityGuidQueryDesc).ToArchetypeChunkArray(Allocator.TempJob))
            using (var blobAssets = EntityDiffer.GetBlobAssetsWithDistinctHash(EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore, chunks, Allocator.TempJob))
            {
                m_BlobAssetBatchPtr->RemoveUnusedBlobAssets(blobAssets.BlobAssetsMap);
            }
        }

        protected override void OnUpdate() {}
    }

    public static unsafe partial class EntityPatcher
    {
        static void ApplyBlobAssetChanges(
            EntityManager entityManager,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeParallelMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes,
            NativeArray<BlobAssetChange> createdBlobAssets,
            NativeArray<byte> createdBlobAssetData,
            NativeArray<ulong> destroyedBlobAssets,
            NativeArray<BlobAssetReferenceChange> blobAssetReferenceChanges)
        {
            if (createdBlobAssets.Length == 0 && blobAssetReferenceChanges.Length == 0 && destroyedBlobAssets.Length == 0)
                return;

            s_ApplyBlobAssetChangesProfilerMarker.Begin();

            var sharedComponentBlobAssetReferencePatches = new NativeParallelMultiHashMap<EntityComponentPair, SharedComponentBlobAssetReferencePatch>(blobAssetReferenceChanges.Length, Allocator.Temp);

            var patcherBlobAssetSystem = entityManager.World.GetOrCreateSystemManaged<EntityPatcherBlobAssetSystem>();

            var blobAssetDataPtr = (byte*)createdBlobAssetData.GetUnsafePtr();

            for (var i = 0; i < createdBlobAssets.Length; i++)
            {
                if (!patcherBlobAssetSystem.TryGetBlobAsset(createdBlobAssets[i].Hash, out _))
                {
                    patcherBlobAssetSystem.AllocateBlobAsset(blobAssetDataPtr, createdBlobAssets[i].Length, createdBlobAssets[i].Hash);
                }

                blobAssetDataPtr += createdBlobAssets[i].Length;
            }

            for (var i = 0; i < destroyedBlobAssets.Length; i++)
            {
                patcherBlobAssetSystem.ReleaseBlobAsset(entityManager, destroyedBlobAssets[i]);
            }

            for (var i = 0; i < blobAssetReferenceChanges.Length; i++)
            {
                var packedComponent = blobAssetReferenceChanges[i].Component;
                var component = packedTypes[packedComponent.PackedTypeIndex];
                var targetOffset = blobAssetReferenceChanges[i].Offset;

                BlobAssetReferenceData targetBlobAssetReferenceData = default;
                if (patcherBlobAssetSystem.TryGetBlobAsset(blobAssetReferenceChanges[i].Value, out var blobAssetPtr))
                {
                    targetBlobAssetReferenceData = new BlobAssetReferenceData {m_Ptr = (byte*)blobAssetPtr.Data};
                }

                if (packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    do
                    {
                        if (!entityManager.Exists(entity))
                        {
                            Debug.LogWarning($"ApplyBlobAssetReferencePatches<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity to patch does not exist.");
                        }
                        else if (!entityManager.HasComponent(entity, component))
                        {
                            Debug.LogWarning($"ApplyBlobAssetReferencePatches<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but component in entity to patch does not exist.");
                        }
                        else
                        {
                            if (component.IsBuffer)
                            {
                                var pointer = (byte*)entityManager.GetBufferRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetBlobAssetReferenceData, sizeof(BlobAssetReferenceData));
                            }
                            else if (component.IsSharedComponent)
                            {
                                sharedComponentBlobAssetReferencePatches.Add(
                                    new EntityComponentPair { Entity = entity, Component = component },
                                    new SharedComponentBlobAssetReferencePatch { Id = targetOffset, Target = blobAssetReferenceChanges[i].Value });
                            }
                            else
                            {
                                var pointer = (byte*)entityManager.GetComponentDataRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetBlobAssetReferenceData, sizeof(BlobAssetReferenceData));
                            }
                        }
                    }
                    while (packedEntities.TryGetNextValue(out entity, ref iterator));
                }
            }
            s_ApplyBlobAssetChangesProfilerMarker.End();

            // Apply all shared component patches
            using (var keys = sharedComponentBlobAssetReferencePatches.GetKeyArray(Allocator.Temp))
            {
                keys.Sort();
                var uniqueCount = keys.Unique();

                for (var i = 0; i < uniqueCount; i++)
                {
                    var pair = keys[i];
                    var patches = sharedComponentBlobAssetReferencePatches.GetValuesForKey(pair);

                    if (pair.Component.IsSharedComponent)
                    {
                        var access = entityManager.GetCheckedEntityDataAccess();
                        var changes = access->BeginStructuralChanges();
                        var sharedComponentIndex = access->EntityComponentStore->GetSharedComponentDataIndex(pair.Entity, pair.Component.TypeIndex);
                        var dataPtr = (byte*)access->EntityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, pair.Component.TypeIndex);
                        foreach (var patch in patches)
                        {
                            var targetOffset = patch.Id;

                            BlobAssetReferenceData targetBlobAssetReferenceData;
                            if (patcherBlobAssetSystem.TryGetBlobAsset(patch.Target, out var blobAssetPtr))
                            {
                                targetBlobAssetReferenceData = new BlobAssetReferenceData {m_Ptr = (byte*)blobAssetPtr.Data};
                            }
                            UnsafeUtility.MemCpy(dataPtr + targetOffset, &targetBlobAssetReferenceData, sizeof(BlobAssetReferenceData));
                        }

                        var hashCode = 0;
                        if (dataPtr != null)
                            hashCode = TypeManager.GetHashCode(dataPtr, pair.Component.TypeIndex);

                        access->SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(pair.Entity, pair.Component.TypeIndex, hashCode, dataPtr);
                        access->EndStructuralChanges(ref changes);
                    }

                    patches.Dispose();
                }
            }

            sharedComponentBlobAssetReferencePatches.Dispose();

            // Workaround to catch some special cases where the memory is never released. (e.g. reloading a scene, toggling live-link on/off).
            patcherBlobAssetSystem.ReleaseUnusedBlobAssets();
        }
    }
}
