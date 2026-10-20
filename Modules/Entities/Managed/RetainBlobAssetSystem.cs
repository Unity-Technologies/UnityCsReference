// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Burst;
using Unity.Collections;

namespace Unity.Entities
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial class RetainBlobAssetSystem : SystemBase
    {
        // Written without SystemAPI: the Entities source generator doesn't run on the engine module.
        EntityQuery m_OwnersToRetainQuery;
        EntityQuery m_RetainedBatchesQuery;
        EntityQuery m_RetainedBlobAssetsQuery;
        EntityQuery m_AllRetainedBatchesQuery;
        EntityQuery m_AllRetainedBlobAssetsQuery;

        protected override void OnCreate()
        {
            m_OwnersToRetainQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<BlobAssetOwner>(), ComponentType.ReadOnly<RetainBlobAssets>() },
                None = new ComponentType[] { ComponentType.ReadOnly<RetainBlobAssetBatchPtr>() },
            });
            m_RetainedBatchesQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<RetainBlobAssets>(), ComponentType.ReadOnly<RetainBlobAssetBatchPtr>() },
                None = new ComponentType[] { ComponentType.ReadOnly<BlobAssetOwner>() },
            });
            m_RetainedBlobAssetsQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<RetainBlobAssets>(), ComponentType.ReadOnly<RetainBlobAssetPtr>() },
                None = new ComponentType[] { ComponentType.ReadOnly<BlobAssetOwner>() },
            });
            // OnDestroy's queries also include entities that still have a BlobAssetOwner. Like the generated code,
            // every query is registered here, which RequireMatchingQueriesForUpdate takes into account.
            m_AllRetainedBatchesQuery = GetEntityQuery(ComponentType.ReadOnly<RetainBlobAssetBatchPtr>(), ComponentType.ReadOnly<RetainBlobAssets>());
            m_AllRetainedBlobAssetsQuery = GetEntityQuery(ComponentType.ReadOnly<RetainBlobAssetPtr>(), ComponentType.ReadOnly<RetainBlobAssets>());
        }

        protected unsafe override void OnUpdate()
        {
            var owners = m_OwnersToRetainQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in owners)
            {
                var blobOwner = EntityManager.GetSharedComponent<BlobAssetOwner>(entity);
                BlobAssetBatch.Retain(blobOwner.BlobAssetBatchPtr);
                EntityManager.AddComponentData(entity, new RetainBlobAssetBatchPtr { BlobAssetBatchPtr = blobOwner.BlobAssetBatchPtr});
            }

            var retainBlobAssetBatchEntities = m_RetainedBatchesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in retainBlobAssetBatchEntities)
            {
                var retainBlobAssets = EntityManager.GetComponentData<RetainBlobAssets>(entity);
                if (retainBlobAssets.FramesToRetainBlobAssets-- <= 0)
                {
                    EntityManager.RemoveComponent<RetainBlobAssets>(entity);
                    EntityManager.RemoveComponent<RetainBlobAssetBatchPtr>(entity);
                }
                else
                    EntityManager.SetComponentData(entity, retainBlobAssets);
            }

            var retainBlobAssetsEntities = m_RetainedBlobAssetsQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in retainBlobAssetsEntities)
            {
                var retainBlobAssets = EntityManager.GetComponentData<RetainBlobAssets>(entity);
                var retainBlobAssetPtr = EntityManager.GetComponentData<RetainBlobAssetPtr>(entity);
                if (retainBlobAssets.FramesToRetainBlobAssets-- <= 0)
                {
                    retainBlobAssetPtr.BlobAsset->Invalidate();
                    Memory.Unmanaged.Free(retainBlobAssetPtr.BlobAsset, Allocator.Persistent);
                    EntityManager.RemoveComponent<RetainBlobAssets>(entity);
                    EntityManager.RemoveComponent<RetainBlobAssetPtr>(entity);
                }
                else
                    EntityManager.SetComponentData(entity, retainBlobAssets);
            }
        }

        protected override unsafe void OnDestroy()
        {
            var batches = m_AllRetainedBatchesQuery.ToComponentDataArray<RetainBlobAssetBatchPtr>(Allocator.Temp);
            foreach (var retainPtr in batches)
                BlobAssetBatch.Release(retainPtr.BlobAssetBatchPtr);

            var assets = m_AllRetainedBlobAssetsQuery.ToComponentDataArray<RetainBlobAssetPtr>(Allocator.Temp);
            foreach (var retainPtr in assets)
            {
                retainPtr.BlobAsset->Invalidate();
                Memory.Unmanaged.Free(retainPtr.BlobAsset, Allocator.Persistent);
            }
        }
    }
}
