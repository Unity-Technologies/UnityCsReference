// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities
{
    static unsafe partial class EntityDiffer
    {
        readonly struct BlobAssetChanges : IDisposable
        {
            public readonly NativeList<BlobAssetChange> CreatedBlobAssets;
            public readonly NativeList<ulong> DestroyedBlobAssets;
            public readonly NativeList<byte> BlobAssetData;
            public readonly bool IsCreated;

            public BlobAssetChanges(AllocatorManager.AllocatorHandle allocator)
            {
                CreatedBlobAssets = new NativeList<BlobAssetChange>(1, allocator);
                DestroyedBlobAssets = new NativeList<ulong>(1, allocator);
                BlobAssetData = new NativeList<byte>(1, allocator);
                IsCreated = true;
            }

            public void Dispose()
            {
                CreatedBlobAssets.Dispose();
                DestroyedBlobAssets.Dispose();
                BlobAssetData.Dispose();
            }
        }

        internal struct BlobAssetsWithDistinctHash : IDisposable
        {
            public NativeList<BlobAssetPtr> BlobAssets;
            public NativeParallelHashMap<ulong, int> BlobAssetsMap;

            public BlobAssetsWithDistinctHash(AllocatorManager.AllocatorHandle allocator)
            {
                BlobAssets = new NativeList<BlobAssetPtr>(1, allocator);
                BlobAssetsMap = new NativeParallelHashMap<ulong, int>(1, allocator);
            }

            public void Dispose()
            {
                BlobAssets.Dispose();
                BlobAssetsMap.Dispose();
            }

            public void TryAdd(BlobAssetPtr blobAssetPtr)
            {
                if (BlobAssetsMap.TryGetValue(blobAssetPtr.Header->Hash, out _))
                    return;

                BlobAssetsMap.TryAdd(blobAssetPtr.Header->Hash, BlobAssets.Length);
                BlobAssets.Add(new BlobAssetPtr(blobAssetPtr.Header));
            }
        }

        [BurstCompile]
        internal struct SortBlobAssetPtr : IJob
        {
            public NativeArray<BlobAssetPtr> Array;
            public void Execute() => Array.Sort(new BlobAssetPtrHashComparer());
        }

        [BurstCompile]
        internal struct GatherCreatedAndDestroyedBlobAssets : IJob
        {
            [ReadOnly] public NativeList<BlobAssetPtr> AfterBlobAssets;
            [ReadOnly] public NativeList<BlobAssetPtr> BeforeBlobAssets;
            [WriteOnly] public NativeList<BlobAssetPtr> CreatedBlobAssets;
            [WriteOnly] public NativeList<BlobAssetPtr> DestroyedBlobAssets;
            [WriteOnly] public NativeList<BlobAssetPtr> SameHashDifferentAddressBlobAssets;

            public void Execute()
            {
                var afterIndex = 0;
                var beforeIndex = 0;

                var comparer = new BlobAssetPtrHashComparer();

                while (afterIndex < AfterBlobAssets.Length && beforeIndex < BeforeBlobAssets.Length)
                {
                    var afterBlobAsset = AfterBlobAssets[afterIndex];
                    var beforeBlobAsset = BeforeBlobAssets[beforeIndex];

                    var compare = comparer.Compare(afterBlobAsset, beforeBlobAsset);

                    if (compare < 0)
                    {
                        CreatedBlobAssets.Add(afterBlobAsset);
                        afterIndex++;
                    }
                    else if (compare == 0)
                    {
                        afterIndex++;
                        beforeIndex++;

                        if (beforeBlobAsset.Data != afterBlobAsset.Data)
                        {
                            SameHashDifferentAddressBlobAssets.Add(afterBlobAsset);
                        }
                    }
                    else
                    {
                        beforeIndex++;
                        DestroyedBlobAssets.Add(beforeBlobAsset);
                    }
                }

                while (afterIndex < AfterBlobAssets.Length)
                {
                    CreatedBlobAssets.Add(AfterBlobAssets[afterIndex++]);
                }

                while (beforeIndex < BeforeBlobAssets.Length)
                {
                    DestroyedBlobAssets.Add(BeforeBlobAssets[beforeIndex++]);
                }
            }
        }

        [BurstCompile]
        internal struct GatherBlobAssetChanges : IJob
        {
            [ReadOnly] public NativeList<BlobAssetPtr> CreatedBlobAssets;
            [ReadOnly] public NativeList<BlobAssetPtr> DestroyedBlobAssets;
            [WriteOnly] public NativeList<BlobAssetChange> CreatedBlobAssetChanges;
            [WriteOnly] public NativeList<ulong> DestroyedBlobAssetChanges;
            public NativeList<byte> BlobAssetData;

            public void Execute()
            {
                var totalBlobAssetLength = 0;

                for (var i = 0; i < CreatedBlobAssets.Length; i++)
                {
                    var length = CreatedBlobAssets[i].Header->Length;
                    CreatedBlobAssetChanges.Add(new BlobAssetChange {Length = length, Hash = CreatedBlobAssets[i].Header->Hash});
                    totalBlobAssetLength += length;
                }

                for (var i = 0; i < DestroyedBlobAssets.Length; i++)
                {
                    DestroyedBlobAssetChanges.Add(DestroyedBlobAssets[i].Header->Hash);
                }

                BlobAssetData.Capacity = totalBlobAssetLength;

                for (var i = 0; i < CreatedBlobAssets.Length; i++)
                {
                    BlobAssetData.AddRange(CreatedBlobAssets[i].Data, CreatedBlobAssets[i].Length);
                }
            }
        }

        internal static BlobAssetsWithDistinctHash GetBlobAssetsWithDistinctHash(
            EntityComponentStore* entityComponentStore,
            NativeArray<ArchetypeChunk> chunks,
            AllocatorManager.AllocatorHandle allocator)
        {
            s_GetBlobAssetsWithDistinctHash.Begin();
            var blobAssetsWithDistinctHash = new BlobAssetsWithDistinctHash(allocator);

            var typeInfoMap = TypeManager.GetTypeInfoDataPointer();
            var blobAssetRefOffsetPtr = TypeManager.GetBlobAssetRefOffsetsPointer();

            for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex].m_Chunk;
                var archetype = entityComponentStore->GetArchetype(chunk);

                // skip this chunk only if we are _certain_ there are no blob asset refs.
                if (!archetype->HasBlobAssetRefs)
                    continue;

                var typesCount = archetype->TypesCount;
                var entityCount = chunks[chunkIndex].Count;

                for (var unorderedTypeIndexInArchetype = 0; unorderedTypeIndexInArchetype < typesCount; unorderedTypeIndexInArchetype++)
                {
                    var typeIndexInArchetype = archetype->TypeMemoryOrderIndexToIndexInArchetype[unorderedTypeIndexInArchetype];
                    var componentTypeInArchetype = archetype->Types[typeIndexInArchetype];
                    if (componentTypeInArchetype.IsZeroSized)
                        continue;

                    ref readonly var typeInfo = ref *TypeManager.GetTypeInfoPointer(typeInfoMap, componentTypeInArchetype.TypeIndex);
                    if (!typeInfo.HasBlobAssetRefs)
                        continue;

                    var chunkBuffer = chunk.Buffer;
                    var subArrayOffset = archetype->Offsets[typeIndexInArchetype];
                    var componentArrayStart = chunkBuffer + subArrayOffset;

                    var blobAssetRefCount = typeInfo.BlobAssetRefOffsetCount;

                    var blobAssetRefOffsets = blobAssetRefOffsetPtr + typeInfo.BlobAssetRefOffsetStartIndex;

                    if (componentTypeInArchetype.IsBuffer)
                    {
                        var header = (BufferHeader*)componentArrayStart;
                        var strideSize = archetype->SizeOfs[typeIndexInArchetype];
                        var elementSize = typeInfo.ElementSize;

                        for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
                        {
                            var bufferStart = BufferHeader.GetElementPointer(header);
                            var bufferEnd = bufferStart + header->Length * elementSize;

                            for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                            {
                                AddBlobAssetsWithDistinctHash(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetsWithDistinctHash);
                            }

                            header = (BufferHeader*)(((byte*)header) + strideSize);
                        }
                    }
                    else
                    {
                        var componentSize = archetype->SizeOfs[typeIndexInArchetype];
                        var end = componentArrayStart + componentSize * entityCount;

                        for (var componentData = componentArrayStart; componentData < end; componentData += componentSize)
                        {
                            AddBlobAssetsWithDistinctHash(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetsWithDistinctHash);
                        }
                    }
                }

                var sharedComponentValues = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);
                for (var i = 0; i < archetype->NumSharedComponents; i++)
                {
                    var sharedComponentIndex = sharedComponentValues[i];
                    if (!TypeManager.GetTypeInfo(EntityComponentStore.GetComponentTypeFromSharedComponentIndex(sharedComponentIndex)).HasBlobAssetRefs)
                        continue;

                    var typeIndex = EntityComponentStore.GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
                    ref readonly var ct = ref TypeManager.GetTypeInfo(typeIndex);
                    var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                    if (blobAssetRefCount <= 0)
                        continue;

                    var dataPtr = (byte*)entityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
                    var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                    AddBlobAssetsWithDistinctHash(dataPtr, blobAssetRefOffsets, blobAssetRefCount, blobAssetsWithDistinctHash);
                }
            }

            new SortBlobAssetPtr
            {
                Array = blobAssetsWithDistinctHash.BlobAssets.AsDeferredJobArray()
            }.Run();

            s_GetBlobAssetsWithDistinctHash.End();

            return blobAssetsWithDistinctHash;
        }

        static void AddBlobAssetsWithDistinctHash(
            byte* componentData,
            TypeManager.EntityOffsetInfo* blobAssetRefOffsets,
            int blobAssetRefCount,
            BlobAssetsWithDistinctHash blobAssets)
        {
            for (var i = 0; i < blobAssetRefCount; ++i)
            {
                var blobAssetRefOffset = blobAssetRefOffsets[i].Offset;
                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + blobAssetRefOffset);

                if (blobAssetRefPtr->m_Ptr == null)
                    continue;

                try
                {
                    // Try to read ValidationPtr, this might throw if the memory has been unmapped
                    blobAssetRefPtr->ValidateNotNull();
                }
                catch (Exception)
                {
                    continue;
                }

                blobAssets.TryAdd(new BlobAssetPtr(blobAssetRefPtr->Header));
            }
        }

        static BlobAssetChanges GetBlobAssetChanges(
            NativeList<BlobAssetPtr> afterBlobAssets,
            NativeList<BlobAssetPtr> beforeBlobAssets,
            AllocatorManager.AllocatorHandle allocator,
            out JobHandle jobHandle,
            JobHandle dependsOn = default)
        {
            var changes = new BlobAssetChanges(allocator);

            var createdBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob);
            var destroyedBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob);
            var sameHashDifferentAddressBlobAssets = new NativeList<BlobAssetPtr>(1, Allocator.TempJob);

            jobHandle = new GatherCreatedAndDestroyedBlobAssets
            {
                AfterBlobAssets = afterBlobAssets,
                BeforeBlobAssets = beforeBlobAssets,
                CreatedBlobAssets = createdBlobAssets,
                DestroyedBlobAssets = destroyedBlobAssets,
                SameHashDifferentAddressBlobAssets = sameHashDifferentAddressBlobAssets
            }.Schedule(dependsOn);

            jobHandle = new GatherBlobAssetChanges
            {
                CreatedBlobAssets = createdBlobAssets,
                DestroyedBlobAssets = destroyedBlobAssets,
                CreatedBlobAssetChanges = changes.CreatedBlobAssets,
                DestroyedBlobAssetChanges = changes.DestroyedBlobAssets,
                BlobAssetData = changes.BlobAssetData
            }.Schedule(jobHandle);

            jobHandle = JobHandle.CombineDependencies
                (
                    createdBlobAssets.Dispose(jobHandle),
                    destroyedBlobAssets.Dispose(jobHandle),
                    sameHashDifferentAddressBlobAssets.Dispose(jobHandle)
                );

            return changes;
        }
    }
}
