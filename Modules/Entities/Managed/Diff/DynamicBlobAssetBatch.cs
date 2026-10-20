// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    unsafe struct BlobAssetPtrHashComparer : IComparer<BlobAssetPtr>
    {
        public int Compare(BlobAssetPtr x, BlobAssetPtr y)
        {
            var hashComparison = x.Hash.CompareTo(y.Hash);
            if (hashComparison != 0)
                return hashComparison;

            var lengthComparison = x.Length.CompareTo(y.Length);
            if (lengthComparison != 0)
                return lengthComparison;

            return UnsafeUtility.MemCmp(x.Data, y.Data, x.Length);
        }
    }

    unsafe struct BlobAssetCache : IDisposable
    {
        public NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr> BlobAssetRemap;
        public DynamicBlobAssetBatch* BlobAssetBatch;

        public BlobAssetCache(AllocatorManager.AllocatorHandle allocator)
        {
            BlobAssetBatch = DynamicBlobAssetBatch.Allocate(allocator);
            BlobAssetRemap = new NativeParallelHashMap<BlobAssetPtr, BlobAssetPtr>(1, allocator);
        }

        public void Dispose()
        {
            DynamicBlobAssetBatch.Free(BlobAssetBatch);
            BlobAssetRemap.Dispose();
            BlobAssetBatch = null;
        }
    }

    unsafe struct DynamicBlobAssetBatch
    {
        AllocatorManager.AllocatorHandle m_Allocator;
        int m_FramesToRetainBlobAssets;
        UnsafeList<BlobAssetPtr>* m_BlobAssets;

        public static DynamicBlobAssetBatch* Allocate(AllocatorManager.AllocatorHandle allocator)
        {
            var batch = (DynamicBlobAssetBatch*)Memory.Unmanaged.Allocate(sizeof(DynamicBlobAssetBatch), UnsafeUtility.AlignOf<DynamicBlobAssetBatch>(), allocator);
            batch->m_FramesToRetainBlobAssets = 1;
            batch->m_Allocator = allocator;
            batch->m_BlobAssets = UnsafeList<BlobAssetPtr>.Create(1, allocator);
            return batch;
        }

        public static void Free(DynamicBlobAssetBatch* batch)
        {
            var blobAssets = batch->m_BlobAssets->Ptr;

            for (var i = 0; i < batch->m_BlobAssets->Length; i++)
                Memory.Unmanaged.Free(blobAssets[i].Header, batch->m_Allocator);

            UnsafeList<BlobAssetPtr>.Destroy(batch->m_BlobAssets);
            Memory.Unmanaged.Free(batch, batch->m_Allocator);
        }

        public void SetFramesToRetainBlobAssets(int framesToRetainBlobAssets)
        {
            m_FramesToRetainBlobAssets = framesToRetainBlobAssets;
        }

        public NativeList<BlobAssetPtr> ToNativeList(AllocatorManager.AllocatorHandle allocator)
        {
            var list = new NativeList<BlobAssetPtr>(m_BlobAssets->Length, allocator);
            list.ResizeUninitialized(m_BlobAssets->Length);
            UnsafeUtility.MemCpy(list.GetUnsafePtr(), m_BlobAssets->Ptr, sizeof(BlobAssetPtr) * m_BlobAssets->Length);
            return list;
        }

        public BlobAssetPtr AllocateBlobAsset(void* data, int length, ulong hash)
        {
            var blobAssetHeader = (BlobAssetHeader*)Memory.Unmanaged.Allocate(length + sizeof(BlobAssetHeader), 16, m_Allocator);

            blobAssetHeader->Length = length;
            blobAssetHeader->ValidationPtr = blobAssetHeader + 1;
            blobAssetHeader->Allocator = Allocator.None;
            blobAssetHeader->Hash = hash;

            UnsafeUtility.MemCpy(blobAssetHeader + 1, data, length);

            m_BlobAssets->Add(new BlobAssetPtr(blobAssetHeader));

            return new BlobAssetPtr(blobAssetHeader);
        }

        public void SortByHash() => NativeSortExtension.Sort((BlobAssetPtr*)m_BlobAssets->Ptr, m_BlobAssets->Length, new BlobAssetPtrHashComparer());

        public bool TryGetBlobAsset(ulong hash, out BlobAssetPtr blobAssetPtr)
        {
            var blobAssets = (BlobAssetPtr*)m_BlobAssets->Ptr;

            for (var i = 0; i < m_BlobAssets->Length; i++)
            {
                if (blobAssets[i].Header->Hash != hash)
                    continue;

                blobAssetPtr = new BlobAssetPtr(blobAssets[i].Header);
                return true;
            }

            blobAssetPtr = default;
            return false;
        }

        public void ReleaseBlobAsset(EntityManager entityManager, ulong hash)
        {
            var blobAssets = (BlobAssetPtr*)m_BlobAssets->Ptr;

            for (var i = 0; i < m_BlobAssets->Length; i++)
            {
                if (blobAssets[i].Hash != hash)
                    continue;

                var entity = entityManager.CreateEntity(ComponentType.ReadWrite<RetainBlobAssets>(), ComponentType.ReadWrite<RetainBlobAssetPtr>());
                entityManager.SetComponentData(entity, new RetainBlobAssets { FramesToRetainBlobAssets = m_FramesToRetainBlobAssets});
                entityManager.SetComponentData(entity, new RetainBlobAssetPtr { BlobAsset = blobAssets[i].Header });

                // Entity lifetime will be bound to the CleanupComponents we added above, we can safely call DestroyEntity(),
                //  it will be actually destroyed when both components will be removed at cleanup.
                entityManager.DestroyEntity(entity);

                m_BlobAssets->RemoveAtSwapBack(i);
                return;
            }
        }

        public void ReleaseBlobAssetImmediately(ulong hash)
        {
            var blobAssets = (BlobAssetPtr*)m_BlobAssets->Ptr;

            for (var i = 0; i < m_BlobAssets->Length; i++)
            {
                if (blobAssets[i].Hash != hash)
                    continue;

                blobAssets[i].Header->Invalidate();
                Memory.Unmanaged.Free(blobAssets[i].Header, m_Allocator);
                m_BlobAssets->RemoveAtSwapBack(i);
                return;
            }
        }

        public void RemoveUnusedBlobAssets(NativeParallelHashMap<ulong, int> usedBlobAssets)
        {
            var blobAssets = (BlobAssetPtr*)m_BlobAssets->Ptr;

            for (var i = 0; i < m_BlobAssets->Length; i++)
            {
                if (!usedBlobAssets.ContainsKey(blobAssets[i].Hash))
                {
                    blobAssets[i].Header->Invalidate();
                    Memory.Unmanaged.Free(blobAssets[i].Header, m_Allocator);
                    m_BlobAssets->RemoveAtSwapBack(i--);
                }
            }
        }
    }
}
