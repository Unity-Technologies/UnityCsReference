// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities.Serialization.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities.Serialization
{
    struct ExternalEntityReference : IBufferElementData
    {
        public Entity Entity;
    }

    struct StableArchetypeCompare : IComparer<IntPtr>
    {
        unsafe public int Compare(IntPtr x, IntPtr y)
        {
            var hashX = ((Archetype*)x)->StableHash;
            var hashY = ((Archetype*)y)->StableHash;

            return hashX.CompareTo(hashY);
        }
    }

    [BurstCompile]
    internal unsafe partial struct SerializeUtilityInterop
    {

        [BurstCompile]
        internal static int AllocAndQueueReadChunkCommands(long readOffset, int totalChunkCount, UnsafeList<SerializeUtility.MegaChunkInfo>* megaChunkInfo, UnsafeList<ReadCommand>* readCommands)
        {
            int chunkCountRead = 0;
            while (totalChunkCount != chunkCountRead)
            {
                if (EntityComponentStore.AllocateContiguousChunk(totalChunkCount - chunkCountRead, out var chunks, out var allocatedCount) != 0)
                {
                    return totalChunkCount;
                }

                ReadCommand cmd;
                cmd.Buffer = chunks.GetPtr();
                cmd.Offset = readOffset;
                cmd.Size = Chunk.kChunkSize * allocatedCount;
                readCommands->Add(cmd);

                megaChunkInfo->Add(new SerializeUtility.MegaChunkInfo(chunks, allocatedCount));
                chunkCountRead += allocatedCount;
                readOffset += Chunk.kChunkSize * allocatedCount;
            }

            return totalChunkCount;
        }

        [BurstCompile]
        internal static void AddExistingChunk(Archetype* archetype, ChunkIndex chunk, int* sharedComponentIndices, byte* enabledBitsValuesForChunk, int* perComponentDisabledBitCount)
        {
            ChunkDataUtility.AddExistingChunk(archetype, chunk, sharedComponentIndices, enabledBitsValuesForChunk, perComponentDisabledBitCount);
        }

        [BurstCompile]
        internal static void ImportChunks(
            SerializeUtility.WorldDeserializationStatus* status,
            ref BurstableMemoryBinaryReader bufferReader,
            UnsafeList<EntityArchetype>* archetypes,
            int* sharedComponentArray,
            int numSharedComponents,
            int* sharedComponentRemap,
            UnsafeList<ArchetypeChunk>* blobAssetRefChunks,
            byte* componentEnabledBits,
            int* enabledBitsHierarchicalData,
            EntityComponentStore* ecs,
			ref NativeArray<EntityId> entityIds,
            UnsafeList<ArchetypeChunk>* chunksWithMetaChunkEntities,
            Entity* externalEntitiesReferences,
            int externalEntitiesRefCount,
            ref Entity prefabRoot)
        {
            var tc = bufferReader.ReadInt();
            var totalChunkCount = status->TotalChunkCount;
            var totalBlobAssetSize = status->BlobAssetSize;
            var blobAssetBuffer = status->BlobAssetBuffer;

            if (status->TotalChunkCount != tc)
            {
                throw new InvalidOperationException("Internal deserialization error: total chunk count doesn't match");
            }

            // The entity counts for each chuck are stored in the chunk metadata for serialization.
            // They need to be hoisted out and stored in the global per chunk metadata array.

            var totalEntityCount = 0;
            var megaChunkInfoList = status->MegaChunkInfoList;
            var megaChunkInfoLength = megaChunkInfoList.Length;
            for (int i = 0; i < megaChunkInfoLength; i++)
            {
                var chunkCount = megaChunkInfoList[i].MegaChunkSize;
                var megaChunkIndex = megaChunkInfoList[i].MegaChunkIndex;
                var chunkPointer = megaChunkIndex.GetPtr();

                for (int j = 0; j < chunkCount; j++)
                {
                    var chunkIndex = new ChunkIndex(megaChunkIndex + j);
                    var entityCount = chunkPointer->CountForSerialization;
                    chunkIndex.Count = entityCount;
                    chunkPointer = (Chunk*)((byte*)chunkPointer + Chunk.kChunkSize);
                    totalEntityCount += entityCount;
                }
            }

            var entityRemap = new NativeArray<EntityRemapUtility.EntityRemapInfo>(totalEntityCount + externalEntitiesRefCount, Allocator.Temp);

            // TODO - entity versions are getting in the way here, it would be better if we could allocate
            // all the entities directly in the remap array. Also, versions make little sense when deserializing.
            var globalEntities = new NativeArray<Entity>(totalEntityCount, Allocator.Temp);
            EntityComponentStore.s_entityStore.Data.AllocateEntities(globalEntities);

            // In the case of section zero, this buffer is meaningless and uninitialized.
            if (externalEntitiesReferences != null)
            {
                EntityComponentStore.s_entityStore.Data.AllocateEntities(externalEntitiesReferences, externalEntitiesRefCount);
            }

            {
                int globalEntitiesBaseIndex = 0;

                for (int i = 0; i < megaChunkInfoLength; i++)
                {
                    var chunkCount = megaChunkInfoList[i].MegaChunkSize;
                    var megaChunkIndex = megaChunkInfoList[i].MegaChunkIndex;

                    for (int j = 0; j < chunkCount; j++)
                    {
                        var chunkIndex = new ChunkIndex(megaChunkIndex + j);
                        {
                            var entityBuffer = (Entity*)chunkIndex.Buffer;
                            var entityCount = chunkIndex.Count;
                            for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
                            {
                                var target = globalEntities[globalEntitiesBaseIndex + entityIndex];
                                EntityRemapUtility.AddEntityRemapping(ref entityRemap, entityBuffer[entityIndex], target);
                            }

                            globalEntitiesBaseIndex += entityCount;
                        }
                    }
                }

                for (int i = 0; i < externalEntitiesRefCount; i++)
                {
                    // Pay close attention to the version number of the target entity being replaced by zero.
                    // The idea is to store a valid index, but force the entity to be considered as invalid.
                    // It will only become valid once the section is merged into the main world.

                    // Setting the external reference placeholder to have version 0 makes it more explicit
                    // that this isn't a valid entity. If we would keep the actual version it would still
                    // be detected as not part of the world because because the entity has no chunk.
                    // So this is only an extra precaution which shouldn't be strictly required.
                    var src = new Entity { Index = globalEntitiesBaseIndex + i, Version = 1 };
                    var dst = new Entity { Index = externalEntitiesReferences[i].Index, Version = 0 };
                    EntityRemapUtility.AddEntityRemapping(ref entityRemap, src, dst);
                }
            }

            var curMegaChunkIndex = 0;
            var megaChunkInfo = status->MegaChunkInfoList;
            var chunkLeftToLoad = megaChunkInfo[0].MegaChunkSize;
            var chunk = megaChunkInfo[0].MegaChunkIndex;
            var sharedComponentArraysIndex = 0;
            var enabledBitsForChunk = componentEnabledBits;
            var enabledBitsHierarchicalDataForChunk = enabledBitsHierarchicalData;
            var remapedSharedComponentValues = stackalloc int[EntityComponentStore.kMaxSharedComponentCount];
            for (int i = 0; i < totalChunkCount; ++i)
            {
                var archetype = archetypes->ElementAt(chunk.GetPtr()->ArchetypeIndexForSerialization).Archetype;
                ecs->SetArchetype(chunk, archetype);
                var numSharedComponentsInArchetype = archetype->NumSharedComponents;
                int* sharedComponentValueArray = sharedComponentArray + sharedComponentArraysIndex;

                for (int j = 0; j < numSharedComponentsInArchetype; ++j)
                {
                    // The shared component 0 is not part of the array, so an index equal to the array size is valid.
                    if (sharedComponentValueArray[j] > numSharedComponents)
                    {
                        throw new ArgumentException(
                            $"Archetype uses shared component at index {sharedComponentValueArray[j]} but only {numSharedComponents} are available, check if the shared scene has been properly loaded.");
                    }
                }

                RemapSharedComponentIndices(remapedSharedComponentValues, archetype, sharedComponentRemap, sharedComponentValueArray);
                sharedComponentArraysIndex += numSharedComponentsInArchetype;

                // Allocate additional heap memory for buffers that have overflown into the heap, and read their data.
                int bufferAllocationCount = bufferReader.ReadInt();
                if (bufferAllocationCount > 0)
                {
                    // TODO: PERF: Batch malloc interface.
                    for (int pi = 0; pi < bufferAllocationCount; ++pi)
                    {
                        var chunkOffset = bufferReader.ReadInt();
                        var allocSizeBytes = bufferReader.ReadInt();
                        var target = (BufferHeader*) OffsetFromPointer(chunk.Buffer, chunkOffset);

                        // TODO: Alignment
                        target->Pointer = (byte*) Memory.Unmanaged.Allocate(allocSizeBytes, 8, Allocator.Persistent);

                        bufferReader.ReadBytes(target->Pointer, allocSizeBytes);
                    }
                }

                if (totalBlobAssetSize != 0 && archetype->HasBlobAssetRefs)
                {
                    blobAssetRefChunks->Add(new ArchetypeChunk(chunk, ecs));
                    PatchBlobAssetsInChunkAfterLoad(archetype, chunk.Buffer, chunk.Count, (byte*) blobAssetBuffer);
                }

                if (archetype->HasUnityObjectRefs)
                    PatchUnityObjectRefsInChunkAfterLoad(archetype, chunk.Buffer, chunk.Count, entityIds);

                chunk.SequenceNumber = ecs->AllocateSequenceNumber();

                chunk.MetaChunkEntity = EntityRemapUtility.RemapEntity(ref entityRemap, chunk.MetaChunkEntity);

                EntityRemapUtility.PatchEntities(
                    archetype->ScalarEntityPatches,
                    archetype->ScalarEntityPatchCount,
                    archetype->BufferEntityPatches,
                    archetype->BufferEntityPatchCount,
                    chunk.Buffer,
                    chunk.Count,
                    ref entityRemap);

                SerializeUtilityInterop.AddExistingChunk(archetype, chunk, remapedSharedComponentValues, enabledBitsForChunk, enabledBitsHierarchicalDataForChunk);
                enabledBitsForChunk += archetype->Chunks.ComponentEnabledBitsSizeTotalPerChunk;
                enabledBitsHierarchicalDataForChunk += archetype->TypesCount;

                if (chunk.MetaChunkEntity != Entity.Null)
                {
                    chunksWithMetaChunkEntities->Add(new ArchetypeChunk(chunk, ecs));
                }

                if (--chunkLeftToLoad == 0 && (i + 1) != totalChunkCount)
                {
                    chunk = megaChunkInfo[++curMegaChunkIndex].MegaChunkIndex;
                    chunkLeftToLoad = megaChunkInfo[curMegaChunkIndex].MegaChunkSize;
                }
                else
                {
                    chunk = new ChunkIndex(chunk + 1);
                }
            }

            prefabRoot = EntityRemapUtility.RemapEntity(ref entityRemap, prefabRoot);
        }
        static void RemapSharedComponentIndices(int* destValues, Archetype* archetype, int* remappedIndices, int* sourceValues)
        {
            int i = 0;
            for (int iType = 1; iType < archetype->TypesCount; ++iType)
            {
                int orderedIndex = archetype->TypeMemoryOrderIndexToIndexInArchetype[iType] - archetype->FirstSharedComponent;
                if (0 <= orderedIndex && orderedIndex < archetype->NumSharedComponents)
                    destValues[orderedIndex] = remappedIndices[sourceValues[i++]];
            }
        }
        static byte* OffsetFromPointer(void* ptr, int offset)
        {
            return ((byte*)ptr) + offset;
        }

        private static unsafe void PatchUnityObjectRefsInChunkAfterLoad(Archetype* archetype, byte* chunkBuffer, int entityCount, NativeArray<EntityId> entityIds)
        {
            var typeCount = archetype->TypesCount;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var unityObjectRefCount = ct.UnityObjectRefOffsetCount;

                if (unityObjectRefCount == 0)
                    continue;

                var unityObjectRefOffsets = TypeManager.GetUnityObjectRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            PatchUnityObjectRefsInComponentAfterLoad(componentData, unityObjectRefOffsets,unityObjectRefCount, entityIds);
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        PatchUnityObjectRefsInComponentAfterLoad(componentData, unityObjectRefOffsets, unityObjectRefCount, entityIds);
                    }
                }
            }
        }

        internal static unsafe void PatchUnityObjectRefsInComponentAfterLoad(byte* componentData, TypeManager.EntityOffsetInfo* unityObjectRefOffsets, int unityObjectRefCount, NativeArray<EntityId> entityIds)
        {
            for (int i = 0; i < unityObjectRefCount; ++i)
            {
                var unityObjectRefOffset = unityObjectRefOffsets[i].Offset;
                var unityObjectRefPtr = (UntypedUnityObjectRef*)(componentData + unityObjectRefOffset);
                // We have stored in this data an index into an array, or -1 if not valid, so while this cast looks dangerous it matches what we write here
                var index = (int)EntityId.ToULong(unityObjectRefPtr->entityId);
                var entityId = index != -1 ? entityIds[index] : EntityId.None;
                unityObjectRefPtr->entityId = entityId;
            }
        }

        private static void PatchBlobAssetsInChunkAfterLoad(Archetype* archetype, byte* chunkBuffer, int entityCount, byte* allBlobAssetData)
        {
            var typeCount = archetype->TypesCount;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                if (blobAssetRefCount == 0)
                    continue;

                var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        for (int ei = 0; ei < header->Length; ++ei)
                        {
                            byte* componentData = bufferStart + ei * elementSize;
                            for (int i = 0; i < blobAssetRefCount; ++i)
                            {
                                var offset = blobAssetRefOffsets[i].Offset;
                                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + offset);
                                int value = (int)blobAssetRefPtr->m_Ptr;
                                byte* ptr = null;
                                if (value != -1)
                                {
                                    ptr = allBlobAssetData + value;
                                }
                                blobAssetRefPtr->m_Ptr = ptr;
                            }
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else if (blobAssetRefCount > 0)
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        for (int i = 0; i < blobAssetRefCount; ++i)
                        {
                            var offset = blobAssetRefOffsets[i].Offset;
                            var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + offset);
                            int value = (int)blobAssetRefPtr->m_Ptr;
                            byte* ptr = null;
                            if (value != -1)
                            {
                                ptr = allBlobAssetData + value;
                            }
                            blobAssetRefPtr->m_Ptr = ptr;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Utility class to serialize and deserialize <see cref="World"/> objects.
    /// </summary>
    public static partial class SerializeUtility
    {
        internal struct Settings
        {
            internal static readonly Settings Default = new Settings
            {
                SerializeComponentTypeNames = true,
                PrefabRoot = Entity.Null
            };

            /// <summary>
            /// Serialize into the "Debug" Node a Node that contains the type name of each ComponentType
            /// </summary>
            /// <remarks>Set to true if you need a more accurate exception during the Deserialization about missing types</remarks>
            internal bool SerializeComponentTypeNames;

            internal bool RequiresDebugSection => SerializeComponentTypeNames;
            internal Entity PrefabRoot;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BufferPatchRecord
        {
            public int ChunkOffset;
            public int AllocSizeBytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BlobAssetRefPatchRecord
        {
            public int ChunkOffset;
            public int BlobDataOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SharedComponentRecord
        {
            public ulong StableTypeHash;
            public int ComponentSize;
        }

        /// <summary>
        /// The version of the serialization file format.
        /// </summary>
        public const int CurrentFileFormatVersion = 80;
        internal const int MaxSubsceneHeaderSize = 1<<16;

        private static unsafe UnsafeList<EntityArchetype> ReadArchetypes(BinaryReader reader, NativeArray<TypeIndex> types, ExclusiveEntityTransaction entityManager,
            out int totalEntityCount)
        {
            int archetypeCount = reader.ReadInt();
            var archetypes = new UnsafeList<EntityArchetype>(archetypeCount, Allocator.Temp);
            totalEntityCount = 0;
            var tempComponentTypes = new NativeList<ComponentType>(Allocator.Temp);
            for (int i = 0; i < archetypeCount; ++i)
            {
                var archetypeEntityCount = reader.ReadInt();
                totalEntityCount += archetypeEntityCount;
                int archetypeComponentTypeCount = reader.ReadInt();
                tempComponentTypes.Clear();
                for (int iType = 0; iType < archetypeComponentTypeCount; ++iType)
                {
                    var typeHashIndexInFile = new TypeIndex { Value = reader.ReadInt() };
                    int typeHashIndexInFileNoFlags = typeHashIndexInFile.Index;
                    var typeIndex = types[typeHashIndexInFileNoFlags];
                    if (TypeManager.IsChunkComponent(typeHashIndexInFile))
                        typeIndex = TypeManager.MakeChunkComponentTypeIndex(typeIndex);

                    tempComponentTypes.Add(ComponentType.FromTypeIndex(typeIndex));
                }

                // This should NOT automatically add simulate to archetypes if the source archetype didn't already have it.
                archetypes.Add(entityManager.EntityManager.CreateArchetypeWithoutSimulateComponent(
                    (ComponentType*)tempComponentTypes.GetUnsafePtr(), tempComponentTypes.Length));
            }

            tempComponentTypes.Dispose();
            return archetypes;
        }

        internal const string k_ExportedTypesDebugLogFileName = "ExportedTypes.log";

        private static NativeArray<TypeIndex> ReadTypeArray(BinaryReader reader, DotsSerializationReader dotsReader)
        {
            var isNameLoaded = false;
            var hasTypesName = false;
            UnsafeParallelHashMap<ulong, int> namesFromStableHash = default;
            StringTableReaderHandle stringStable = default;

            // Unknown type rarely occur, so we built the type hash/name map only at the first call of this method
            // If the debug section is not present in the file, we can't build the map and return false
            bool GetTypeName(ulong stableHash, out FixedString512Bytes name)
            {
                if (isNameLoaded == false)
                {
                    isNameLoaded = true;
                    var debugSectionNode = dotsReader.RootNode.FindNode(DebugSectionNodeType, 2);

                    if (debugSectionNode.IsValid)
                    {
                        hasTypesName = true;
                        stringStable = dotsReader.OpenStringTableNode(debugSectionNode.FindNode<DotsSerialization.StringTableNode>());
                        var typesNameNode = debugSectionNode.FindNode<DotsSerialization.TypeNamesNode>();
                        var typeCount = typesNameNode.As<DotsSerialization.TypeNamesNode>().TypeCount;
                        namesFromStableHash = new UnsafeParallelHashMap<ulong, int>(typeCount, Allocator.Temp);
                        using (var readerHandle = typesNameNode.GetReaderHandle())
                        {
                            var r = readerHandle.Reader;
                            for (var i = 0; i < typeCount; i++)
                            {
                                namesFromStableHash.Add(r.ReadULong(), r.ReadInt());
                            }
                        }
                    }
                }

                if (!hasTypesName)
                {
                    name = default;
                    return false;
                }

                name = stringStable.GetString512(namesFromStableHash[stableHash]);
                return true;
            }

            {
                var typeCount = reader.ReadInt();

                var types = new NativeArray<TypeIndex>(typeCount, Allocator.Temp);
                for (int i = 0; i < typeCount; ++i)
                {
                    var stableTypeHash = reader.ReadULong();
                    var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(stableTypeHash);
                    if (typeIndex == TypeIndex.Null)
                    {
                        var res = GetTypeName(stableTypeHash, out var typeName);
                        if (res == false)
                        {
                            throw new ArgumentException($"Cannot find TypeIndex for type hash {stableTypeHash.ToString()}. Check in the debug file {k_ExportedTypesDebugLogFileName} of your project Logs folder (<projectName>/Logs) the corresponding Component type name for the type hash {stableTypeHash.ToString()}. And ensure your runtime depends on all assemblies defining the Component types your data uses.");
                        }

                        throw new ArgumentException($"Cannot find Type {typeName} for type hash {stableTypeHash.ToString()}. Ensure your runtime depends on all assemblies defining the Component types your data uses.");
                    }

                    types[i] = typeIndex;
                }

                if (hasTypesName)
                {
                    namesFromStableHash.Dispose();
                    stringStable.Dispose();
                }
                return types;
            }
        }

        internal static unsafe UnsafePtrList<Archetype> GetAllArchetypes(EntityComponentStore* entityComponentStore, AllocatorManager.AllocatorHandle allocator)
        {
            int count = 0;
            for (var i = 0; i < entityComponentStore->m_Archetypes.Length; ++i)
            {
                var archetype = entityComponentStore->m_Archetypes.Ptr[i];
                if (ShouldSerializeArchetype(archetype))
                {
                    count++;
                }
            }

            var archetypes = new UnsafePtrList<Archetype>(count, allocator);
            archetypes.Resize(count, NativeArrayOptions.UninitializedMemory);
            count = 0;
            for (var i = 0; i < entityComponentStore->m_Archetypes.Length; ++i)
            {
                var archetype = entityComponentStore->m_Archetypes.Ptr[i];
                if (ShouldSerializeArchetype(archetype))
                {
                    archetypes.Ptr[count++] = entityComponentStore->m_Archetypes.Ptr[i];
                }
            }

            NativeSortExtension.Sort((IntPtr*)archetypes.Ptr, archetypes.Length, default(StableArchetypeCompare));

            return archetypes;
        }

        static unsafe bool ShouldSerializeArchetype(Archetype* archetype)
        {
            if (archetype->EntityCount <= 0 || archetype->HasSystemInstanceComponents)
                return false;
            return true;
        }

        /// <summary>
        /// Uses a <see cref="BinaryWriter"/> to serialize a <see cref="World"/>.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> of the serialized world.</param>
        /// <param name="writer">The serialization object.</param>
        public static void SerializeWorld(EntityManager entityManager, BinaryWriter writer)
        {
            var remapSize = entityManager.HighestEntityIndex() + 1;
            var entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(remapSize, Allocator.Temp);
            SerializeWorldInternal(entityManager, writer, UnityObjectRefMap.None, entityRemapInfos, default, Settings.Default);
            entityRemapInfos.Dispose();
        }

        /// <summary>
        /// Uses a <see cref="BinaryWriter"/> to serialize a <see cref="World"/>.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> of the serialized world.</param>
        /// <param name="writer">The serialization object.</param>
        /// <param name="referencedObjects">The UnityEngine.Object references from the serialized world.</param>
        public static void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out object[] referencedObjects)
        {
            var remapSize = entityManager.HighestEntityIndex() + 1;
            var entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(remapSize, Allocator.Temp);
            var unityObjectRefs = new UnityObjectRefMap(Allocator.Temp);
            SerializeWorldInternal(entityManager, writer, unityObjectRefs, entityRemapInfos,default, Settings.Default);

            referencedObjects = unityObjectRefs.ToObjectArray();

            entityRemapInfos.Dispose();
            unityObjectRefs.Dispose();
        }

        /// <summary>
        /// Uses a <see cref="BinaryWriter"/> to serialize a <see cref="World"/>.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> of the serialized world.</param>
        /// <param name="writer">The serialization object.</param>
        /// <param name="entityRemapInfos">The Entity remapping to apply during serialization.</param>
        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            SerializeWorldInternal(entityManager, writer, UnityObjectRefMap.None, entityRemapInfos, default, Settings.Default);
        }

        /// <summary>
        /// Uses a <see cref="BinaryWriter"/> to serialize a <see cref="World"/>.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> of the serialized world.</param>
        /// <param name="writer">The serialization object.</param>
        /// <param name="referencedObjects">The UnityEngine.Object references from the serialized world.</param>
        /// <param name="entityRemapInfos">The entity remapping to apply during serialization.</param>
        public static void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out object[] referencedObjects, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            var unityObjectRefs = new UnityObjectRefMap(Allocator.Temp);
            SerializeWorldInternal(entityManager, writer, unityObjectRefs, entityRemapInfos, default, Settings.Default);
            referencedObjects = unityObjectRefs.ToObjectArray();
            unityObjectRefs.Dispose();
        }

        /// <summary>
        /// Gets the entity representing the scene section with the index passed in.
        /// If createIfMissing is true the section entity is created if it doesn't already exist.
        /// Metadata components added to this section entity will be serialized into the entity scene header.
        /// At runtime these components will be added to the scene section entities when the scene is resolved.
        /// Only struct IComponentData components without BlobAssetReferences or Entity members are supported.
        /// </summary>
        /// <param name="sectionIndex">The section index for which to get the scene section entity</param>
        /// <param name="manager">The entity manager to which the entity belongs</param>
        /// <param name="cachedSceneSectionEntityQuery">The EntityQuery used to find the entity. Initially an null query should be passed in,
        /// the same query can the be passed in for subsequent calls to avoid recreating the query</param>
        /// <param name="createIfMissing">If true the section entity is created if it doesn't already exist. If false Entity.Null is returned for missing section entities</param>
        /// <returns>The entity representing the scene section</returns>
        public static Entity GetSceneSectionEntity(int sectionIndex, EntityManager manager, ref EntityQuery cachedSceneSectionEntityQuery, bool createIfMissing = true)
        {
            if (cachedSceneSectionEntityQuery == default)
            {
                var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<SectionMetadataSetup>();
                cachedSceneSectionEntityQuery = manager.CreateEntityQuery(builder);
            }

            var sectionComponent = new SectionMetadataSetup {SceneSectionIndex = sectionIndex};
            cachedSceneSectionEntityQuery.SetSharedComponentFilter(sectionComponent);
            using (var sectionEntities = cachedSceneSectionEntityQuery.ToEntityArray(Allocator.TempJob))
            {
                if (sectionEntities.Length == 0)
                {
                    if (!createIfMissing)
                        return Entity.Null;
                    var sceneSectionEntity = manager.CreateEntity();
                    manager.AddSharedComponent(sceneSectionEntity, sectionComponent);
                    return sceneSectionEntity;
                }

                if (sectionEntities.Length == 1)
                    return sectionEntities[0];

                throw new InvalidOperationException($"Multiple scene section entities with section index {sectionIndex} found");
            }
        }

        internal static readonly Hash128 WorldFileType                      = new Hash128("7F090F7311DF4BA5BA3094AA33D0FAA7");
        internal static readonly Hash128 WorldNodeType                      = new Hash128("E9FDFDEB4775443ABB236D4858232894");
        internal static readonly Hash128 DebugSectionNodeType               = new Hash128("582BAFE96EC44A72ACD9CCBC850FDBC4");
        internal static readonly Hash128 TypesNameStringTableNodeType       = new Hash128("4847374145504072849AAC7B1921E7FD");
        internal static readonly Hash128 TypesNameNodeType                  = new Hash128("828510FAD38F4EA78CD908D4D780388B");
        internal static readonly Hash128 ChunkLayoutNodeType               = new Hash128("7B2E5A1D3F48C69E0A1D7C3B5E9F2A84");
        internal static readonly Hash128 ArchetypesNodeType                 = new Hash128("F5364E1CCB62466A9883F6F9554D4F0C");
        internal static readonly Hash128 SharedAndManagedComponentsNodeType = new Hash128("D565355C5CF34C0DBBD4A06ADDA948B1");
        internal static readonly Hash128 EnabledBitsNodeType                = new Hash128("5846E500EA614C1DA94AFB85AFD8F4F4");
        internal static readonly Hash128 BlobAssetsNodeType                 = new Hash128("9A26954FF1ED4CC5A64E8AAD4F64773A");
        internal static readonly Hash128 ChunksNodeType                     = new Hash128("2EA7CE3325F04D7A84CEAB46790C628A");
        internal static readonly Hash128 BufferDataNodeType                 = new Hash128("E33124BFAC2649D792DE36E4611DDD70");
        internal static readonly Hash128 PrefabNodeType                     = new Hash128("2A84A183583A4FAD8CB7105AE3C47598");

        internal struct MegaChunkInfo
        {
            public int MegaChunkSize;
            public ChunkIndex MegaChunkIndex;

            public MegaChunkInfo(ChunkIndex fistChunkIndex, int chunkCount)
            {
                Assert.IsTrue(chunkCount > 0);
                MegaChunkSize = chunkCount;
                MegaChunkIndex = fistChunkIndex;
            }
        }

        internal unsafe struct WorldDeserializationStatus
        {
            internal UnsafeList<MegaChunkInfo> MegaChunkInfoList;
            public DotsSerializationReader.NodeHandle.PrefetchState ArchetypePrefetchState;
            [NativeDisableUnsafePtrRestriction]
            public void* BlobAssetBuffer;
            public int BlobAssetSize;
            public DotsSerializationReader.NodeHandle.PrefetchState SharedComponentPrefetchState;
            public DotsSerializationReader.NodeHandle.PrefetchState EnabledBitsPrefetchState;
            public DotsSerializationReader.NodeHandle.PrefetchState BufferElementPrefetchState;
            public DotsSerializationReader.NodeHandle.PrefetchState PrefabPrefetchState;
            public int TotalChunkCount;

            public void Dispose()
            {
                MegaChunkInfoList.Dispose();
                ArchetypePrefetchState.Dispose();
                SharedComponentPrefetchState.Dispose();
                EnabledBitsPrefetchState.Dispose();
                BufferElementPrefetchState.Dispose();
                PrefabPrefetchState.Dispose();
                Memory.Unmanaged.Free(BlobAssetBuffer, Allocator.Persistent);
            }
        }

        internal struct WorldDeserializationResult
        {
            internal Entity PrefabRoot;
        }

        internal static unsafe void FillReadCommands(DotsSerializationReader dotsReader, UnsafeList<ReadCommand>* readCommands, out WorldDeserializationStatus status)
        {
            status = new WorldDeserializationStatus();

            var worldNodeHandle = dotsReader.RootNode.FindNode(WorldNodeType);

            // Fetch Archetypes data from disk
            {
                var archetypesNodeHandle = worldNodeHandle.FindNode(ArchetypesNodeType);
                if (archetypesNodeHandle.IsValid == false)
                {
                    throw new InvalidOperationException($"Binary entity scene file must contain a ArchetypesNodeType");
                }

                status.ArchetypePrefetchState = archetypesNodeHandle.PrefetchRawDataRead(out var rc);
                readCommands->Add(rc);
            }

            // Read BlobAsset from disk to final memory buffer
            {
                if (worldNodeHandle.TryFindNode(BlobAssetsNodeType, out var blobAssetNodeHandle))
                {
                    status.BlobAssetSize = (int)blobAssetNodeHandle.DataSize;
                    status.BlobAssetBuffer = Memory.Unmanaged.Allocate(status.BlobAssetSize, 16, Allocator.Persistent);

                    ReadCommand rc = default;
                    rc.Buffer = status.BlobAssetBuffer;
                    rc.Offset = blobAssetNodeHandle.DataStartingOffset;
                    rc.Size = status.BlobAssetSize;
                    readCommands->Add(rc);
                }
            }

            // Fetch Shared & Managed component data from disk
            {
                var sharedComponentsNodeHandle = worldNodeHandle.FindNode(SharedAndManagedComponentsNodeType);
                if (sharedComponentsNodeHandle.IsValid == false)
                {
                    throw new InvalidOperationException($"Binary entity scene file must contain a SharedAndManagedComponentsNodeType");
                }
                status.SharedComponentPrefetchState = sharedComponentsNodeHandle.PrefetchRawDataRead(out var rc);
                readCommands->Add(rc);
            }

            // Read Enabled bits data from disk
            {
                var enabledBitsNodeHandle = worldNodeHandle.FindNode(EnabledBitsNodeType);
                if (enabledBitsNodeHandle.IsValid == false)
                {
                    throw new InvalidOperationException($"Binary entity scene file must contain an EnabledBitsNodeType");
                }
                status.EnabledBitsPrefetchState = enabledBitsNodeHandle.PrefetchRawDataRead(out var rc);
                readCommands->Add(rc);
            }

            // Read Chunk from disk to final memory buffer
            {
                var chunksNodeHandle = worldNodeHandle.FindNode(ChunksNodeType);
                if (chunksNodeHandle.IsValid == false)
                {
                    throw new InvalidOperationException($"Binary entity scene file must contain a ChunksNodeType");
                }

                var totalChunkCount = (int)(chunksNodeHandle.DataSize / Chunk.kChunkSize);

                long readOffset = chunksNodeHandle.DataStartingOffset;

                var megaChunkInfo = new UnsafeList<MegaChunkInfo>(8, Allocator.Persistent);

                if (SerializeUtilityInterop.AllocAndQueueReadChunkCommands(readOffset, totalChunkCount, &megaChunkInfo, readCommands) != totalChunkCount)
                {
                    throw new Exception("Contiguous Chunk allocation failed");
                }

                status.MegaChunkInfoList = megaChunkInfo;
                status.TotalChunkCount = totalChunkCount;
            }

            // Fetch Buffer Element data from disk
            {
                var bufferDataNodeHandle = worldNodeHandle.FindNode(BufferDataNodeType);
                if (bufferDataNodeHandle.IsValid == false)
                {
                    throw new InvalidOperationException($"Binary entity scene file must contain a BufferDataNodeType");
                }
                status.BufferElementPrefetchState = bufferDataNodeHandle.PrefetchRawDataRead(out var rc);
                readCommands->Add(rc);
            }

            // Fetch Prefab
            {
                if (worldNodeHandle.TryFindNode(PrefabNodeType, out var prefabNodeHandle))
                {
                    status.PrefabPrefetchState = prefabNodeHandle.PrefetchRawDataRead(out var rc);
                    readCommands->Add(rc);
                }
            }
        }


        internal static unsafe void EndDeserializeWorld(ExclusiveEntityTransaction manager, DotsSerializationReader dotsReader, ref WorldDeserializationStatus status, out WorldDeserializationResult deserializationResult, int externalEntitiesRefRange, int sceneSectionIndex, NativeArray<EntityId> unityObjects)
        {
            deserializationResult = default;
            var access = manager.EntityManager.GetCheckedEntityDataAccess();
            var ecs = access->EntityComponentStore;
            var companionStore = access->CompanionComponentStore;

            void CleanupWhenDeserializationIsInterrupted(ref WorldDeserializationStatus status)
            {
                var megaChunkInfoList = status.MegaChunkInfoList;
                var megaChunkInfoLength = megaChunkInfoList.Length;
                for (int i = 0; i < megaChunkInfoLength; i++)
                {
                    var chunkCount = megaChunkInfoList[i].MegaChunkSize;
                    var firstChunk = new ChunkIndex(megaChunkInfoList[i].MegaChunkIndex);
                    EntityComponentStore.FreeContiguousChunks(firstChunk, chunkCount);
                }

                status.Dispose();
            }

            if (ecs->CountEntities() != 0)
            {
                // If we have entities and they are all systems, copy them to a temporary world.
                // After deserialization, the systems that may have been transferred will be removed, and we will copy
                // these systems back.
                using (var query = manager.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<SystemInstance>()))
                {
                    if (!query.IsEmptyIgnoreFilter)
                    {
                        CleanupWhenDeserializationIsInterrupted(ref status);
                        throw new ArgumentException(
                            $"DeserializeWorld can only be used on completely empty World. Please create a new empty World and add Systems after loading is complete instead.");
                    }
                }

                if (ecs->CountEntities() != 0)
                {
                    CleanupWhenDeserializationIsInterrupted(ref status);
                    throw new ArgumentException(
                        $"DeserializeWorld can only be used on completely empty EntityManager. Please create a new empty World and use EntityManager.MoveEntitiesFrom to move the loaded entities into the destination world instead.");
                }
            }

            int totalEntityCount;
            UnsafeList<EntityArchetype> archetypes;
            NativeArray<int> sharedComponentArray;
            int numSharedComponents;
            NativeList<int> sharedComponentRemap = new NativeList<int>(Allocator.Temp);
            UnsafeList<ArchetypeChunk> blobAssetRefChunks;
            var blobAssetOwner = default(BlobAssetOwner);
            NativeArray<TypeIndex> types;

            // Read Archetypes
            using (var stream = status.ArchetypePrefetchState.CreateStream())
            {
                types = ReadTypeArray(stream, dotsReader);
                archetypes = ReadArchetypes(stream, types, manager, out totalEntityCount);
            }

            // Read BlobAssets
            var totalBlobAssetSize = status.BlobAssetSize;
            var blobAssetBuffer = status.BlobAssetBuffer;
            if (totalBlobAssetSize != 0)
            {
                blobAssetOwner = new BlobAssetOwner(blobAssetBuffer, status.BlobAssetSize);
            }
            blobAssetRefChunks = new UnsafeList<ArchetypeChunk>(32, Allocator.Temp);

            // Read Shared and Managed components
            // Note, do not move this code to "using (var sharedComponentReader = status.SharedComponentPrefetchState.CreateStream()) {}"
            // until UUM-27771 is resolved.
            var sharedComponentReader = status.SharedComponentPrefetchState.CreateStream();

            // read the full index list
            int sharedComponentArraysLength = sharedComponentReader.ReadInt();
            sharedComponentArray = new NativeArray<int>(sharedComponentArraysLength, Allocator.Temp);
            sharedComponentReader.ReadArray(sharedComponentArray, sharedComponentArraysLength);

            // 0 index is special and means default shared component value
            // Also see below the offset + 1 indices for the same reason
            sharedComponentRemap.Add(0);

            var unmanagedSharedComponentCount = ReadUnmanagedSharedComponents(manager, sharedComponentReader, sharedComponentRemap,
                (WorldDeserializationStatus*)UnsafeUtility.AddressOf(ref status), unityObjects);

            ReadEmptyManagedComponents(sharedComponentReader);
            numSharedComponents = unmanagedSharedComponentCount;

            sharedComponentReader.Dispose();

            // Get pointers to enabled bits data
            var enabledBitsData = (byte*)status.EnabledBitsPrefetchState._buffer;
            var enabledBitsSizeInBytes = *((int*) enabledBitsData);
            enabledBitsData += sizeof(int);
            var enabledBitsHierarchicalData = (int*)(enabledBitsData + enabledBitsSizeInBytes + sizeof(int));


            // Read Chunk Buffer data elements
            var totalChunkCount = status.TotalChunkCount;
            var chunksWithMetaChunkEntities = new UnsafeList<ArchetypeChunk>(totalChunkCount, Allocator.Temp);
            using (var bufferReader = (BurstableMemoryBinaryReader)status.BufferElementPrefetchState.CreateStream())
            {
                var reader = bufferReader;
                NativeArray<Entity> extRefBuffer = default;

                // Make sure this code doesn't run if the entity store V2 isn't active.
                // It looks harmless enough, but the creation of an additional entity at this point with
                // entity store V1 throws off the external references.

                // IMPORTANT : it is a valid edge case for section 0 to have 0 entities,
                // so checking if externalEntitiesRefRange is 0 or not isn't equivalent to checking if the section
                // index itself is 0 or not. Every section beyond 0 needs a ExternalEntityReference buffer, even if empty.
                if (sceneSectionIndex > 0)
                {
                    // Only sections above zero are expected to have external entities.
                    // When using entity store V2, those need to be allocated in the streaming world.
                    // So we need to keep track of those, and we do so through an entity with a singleton buffer.
                    var externalRefEntity = manager.CreateEntity();
                    var externalRefBuffer = manager.AddBuffer<ExternalEntityReference>(externalRefEntity);
                    externalRefBuffer.ResizeUninitialized(externalEntitiesRefRange);
                    extRefBuffer = externalRefBuffer.AsNativeArray().Reinterpret<Entity>();
                }

                if (status.PrefabPrefetchState._buffer != null)
                {
                    if(status.PrefabPrefetchState._size != sizeof(Entity))
                        throw new InvalidOperationException($"Internal deserialization error: Unexpected size of PrefabPrefetchState Expected:{sizeof(Entity)} Actual:{status.PrefabPrefetchState._size}");
                    deserializationResult.PrefabRoot = *(Entity*)status.PrefabPrefetchState._buffer;
                }

                SerializeUtilityInterop.ImportChunks((WorldDeserializationStatus*)UnsafeUtility.AddressOf(ref status),
                    ref reader,
                    &archetypes,
                    (int*)sharedComponentArray.GetUnsafePtr(),
                    numSharedComponents,
                    (int*)sharedComponentRemap.GetUnsafePtr(),
                    &blobAssetRefChunks,
                    enabledBitsData,
                    enabledBitsHierarchicalData,
                    ecs,
                    ref unityObjects,
                    &chunksWithMetaChunkEntities,
                    extRefBuffer.IsCreated ? (Entity*)extRefBuffer.GetUnsafePtr() : null,
                    extRefBuffer.Length,
                    ref deserializationResult.PrefabRoot);

                status.BlobAssetBuffer = null; // BlobAssetOwner takes ownership of BlobAssetBuffer
                for (int i = 0; i < chunksWithMetaChunkEntities.Length; ++i)
                {
                    var chunkw = chunksWithMetaChunkEntities[i].m_Chunk;
                    manager.SetComponentData(chunkw.MetaChunkEntity, new ChunkHeader { ArchetypeChunk = chunksWithMetaChunkEntities[i] });
                }
                chunksWithMetaChunkEntities.Dispose();
                archetypes.Dispose();
            }

            companionStore.Playback(ref ecs->CompanionChangesTracker);
            ecs->InvalidateChunkListCacheForChangedArchetypes();

            if (totalBlobAssetSize != 0)
            {
                var barc = new NativeArray<ArchetypeChunk>(blobAssetRefChunks.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(barc.GetUnsafePtr(), blobAssetRefChunks.Ptr, sizeof(ArchetypeChunk)*blobAssetRefChunks.Length);
                manager.AddSharedComponent(barc, blobAssetOwner);
                blobAssetRefChunks.Dispose();
                barc.Dispose();
            }

            status.Dispose();
            blobAssetOwner.Release();
            types.Dispose();

            // Chunks have now taken over ownership of the shared components (reference counts have been added)
            // so remove the ref that was added on deserialization
            for (int i = 0; i < numSharedComponents; ++i)
            {
                var shareComponentIndex = sharedComponentRemap[i + 1];
                access->RemoveSharedComponentReference(shareComponentIndex);
            }
            sharedComponentRemap.Dispose();
        }

        /// <summary>
        /// Deserializes a <see cref="World"/> object.
        /// </summary>
        /// <param name="manager">The <see cref="ExclusiveEntityTransaction"/> object ensuring that the deserialization is atomic.</param>
        /// <param name="reader">The deserialization object.</param>
        /// <param name="unityObjectArray">The UnityEngine.Object references that are patched in during deserialization.</param>
        public static unsafe void DeserializeWorld(ExclusiveEntityTransaction manager, BinaryReader reader, object[] unityObjectArray = null)
        {
            // So we do this to keep compatibility
            NativeArray<EntityId> unityObjects = default;
            if (unityObjectArray != null && unityObjectArray.Length > 0)
            {
                unityObjects = new NativeArray<EntityId>(unityObjectArray.Length, Allocator.Temp);

                for (int i = 0; i < unityObjectArray.Length; i++)
                {
                    unityObjects[i] = CoreModuleLightShim.Hooks.GetEntityId(unityObjectArray[i]);
                }
            }
            else
            {
                unityObjects = new NativeArray<EntityId>(0, Allocator.Temp);
            }
            DeserializeWorldInternal(manager, reader, out _, 0, 0, unityObjects);
            unityObjects.Dispose();
        }

        internal static unsafe void DeserializeWorldInternal(ExclusiveEntityTransaction manager, BinaryReader reader, out WorldDeserializationResult deserializationResult, int externalEntitiesRefRange, int sceneSectionIndex, NativeArray<EntityId> unityObjects)
        {

            if (reader is StreamBinaryReader)
            {
                using (var dotsReader = DotsSerialization.CreateReader(reader))
                {
                    var filePath = ((StreamBinaryReader) reader).FilePath;
                    var readCommands = new UnsafeList<ReadCommand>(1, Allocator.Persistent);
                    FillReadCommands(dotsReader, &readCommands, out var status);
                    CoreModuleLightShim.Hooks.ReadFile(filePath, readCommands.Ptr, (uint)readCommands.Length, AssetLoadingSubsystem.EntitiesScene);
                    readCommands.Dispose();
                    EndDeserializeWorld(manager, dotsReader, ref status, out deserializationResult, externalEntitiesRefRange, sceneSectionIndex, unityObjects);
                    return;
                }
            }

            using(var dotsReader = DotsSerialization.CreateReader(reader))
            {
                var readCommands = new UnsafeList<ReadCommand>(1, Allocator.Temp);
                FillReadCommands(dotsReader, &readCommands, out var status);
                for (int i = 0; i < readCommands.Length; ++i)
                {
                    reader.Position = readCommands[i].Offset;
                    reader.ReadBytes(readCommands[i].Buffer, (int)readCommands[i].Size);
                }
                EndDeserializeWorld(manager, dotsReader, ref status, out deserializationResult, externalEntitiesRefRange, sceneSectionIndex, unityObjects);
            }
        }

        internal static unsafe BlobAssetReference<DotsSerialization.BlobHeader> SerializeWorldInternal(EntityManager entityManager, BinaryWriter writer, UnityObjectRefMap unityObjectRefs, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos, NativeParallelHashSet<UntypedWeakReferenceId> weakAssetRefs, Settings settings, bool buildBlobHeader = false)
        {
            BlobAssetReference<DotsSerialization.BlobHeader> blobHeader = default;
            using (var dotsWriter = DotsSerialization.CreateWriter(writer, WorldFileType, "EntityBinaryFile"))
            {
                using (dotsWriter.CreateNode<DotsSerialization.FolderNode>(WorldNodeType))
                {
                    var access = entityManager.GetCheckedEntityDataAccess();
                    var entityComponentStore = access->EntityComponentStore;

                    // Write Debug Section if needed (also when ImHex is enabled for type name lookup)
                    if (settings.RequiresDebugSection)
                    {
                        using (dotsWriter.CreateNode<DotsSerialization.FolderNode>(DebugSectionNodeType))
                        {
                            WriteTypeNames(dotsWriter, ref entityManager);
                            if (ImHexSettings.Enabled)
                                WriteChunkLayouts(dotsWriter, entityComponentStore);
                        }
                    }

                    // Write the archetype node
                    using(var archetypeArray = WriteArchetypesNode(dotsWriter, entityComponentStore))
                    {
                        // Build ShareComponent mapping tables
                        BuildSharedComponents(archetypeArray, out var sharedComponentArrays, out var sharedComponentsToSerialize);

                        // Write the BlobAssets
                        WriteBlobAssetNode(entityManager, dotsWriter, archetypeArray, out var blobAssetMap, out var blobAssetOffsets);
                        using (blobAssetMap)
                        using (blobAssetOffsets)
                        {

                            var totalChunkCount = GenerateRemapInfo(entityManager, archetypeArray, entityRemapInfos);

                            // Write Shared and Managed components
                            WriteSharedAndManagedComponents(entityManager,
                                entityRemapInfos,
                                dotsWriter,
                                sharedComponentArrays,
                                sharedComponentsToSerialize,
                                blobAssetMap,
                                blobAssetOffsets,
                                unityObjectRefs);

                            WriteEnabledBits(dotsWriter, archetypeArray);

                            //TODO: ensure chunks are defragged?
                            using(var bufferPatches = new NativeList<BufferPatchRecord>(128, Allocator.Temp))
                            using(var bufferDataList = new NativeList<IntPtr>(128, Allocator.Temp))
                            using(var bufferPatchesCountPerChunk = new NativeList<int>(128, Allocator.Temp))
                            {
                                var stackBytes = stackalloc byte[Chunk.kChunkSize];
                                var tempChunk = (Chunk*) stackBytes;

                                // Write chunks
                                WriteChunks(entityRemapInfos, dotsWriter, archetypeArray, tempChunk, blobAssetOffsets,
                                    blobAssetMap, bufferPatches, bufferPatchesCountPerChunk, bufferDataList, weakAssetRefs, unityObjectRefs);

                                var j = 0;
                                for (int i = 0; i < bufferPatchesCountPerChunk.Length; i++)
                                {
                                    j += bufferPatchesCountPerChunk[i];
                                }

                                // Write Buffer Data Elements heaps
                                WriteBufferDataElements(dotsWriter, totalChunkCount, archetypeArray,
                                    bufferPatchesCountPerChunk,
                                    bufferPatches, bufferDataList);
                            }

                            var prefabRoot = EntityRemapUtility.RemapEntity(ref entityRemapInfos, settings.PrefabRoot);
                            if (prefabRoot != Entity.Null)
                            {
                                WritePrefabNode(dotsWriter, prefabRoot);
                            }
                        }
                    }

                    // NB if we are not building a blob header, the header will be written automatically when the
                    // writer is disposed.
                    if (buildBlobHeader)
                        blobHeader = dotsWriter.WriteHeaderAndBlob();
                }
            }

            return blobHeader;
        }

        private static unsafe void WriteBufferDataElements(DotsSerializationWriter dotsWriter, int totalChunkCount,
            UnsafePtrList<Archetype> archetypeArray, NativeList<int> bufferPatchesCountPerChunk,
            NativeList<BufferPatchRecord> bufferPatches, NativeList<IntPtr> bufferDataList)
        {
            using (var chunkNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(BufferDataNodeType))
            using (var writerHandle = chunkNode.GetWriterHandle())
            {
                chunkNode.NodeHeader.Revision = 1;

                var w = writerHandle.Writer;
                w.ImHexWrite<int>("totalChunkCount");
                w.Write(totalChunkCount);
                var curChunkIndex = 0;
                var curRecordStartIndex = 0;
                var bufferDataRecordIndex = 0;

                for (int a = 0; a < archetypeArray.Length; ++a)
                {
                    var archetype = archetypeArray.Ptr[a];

                    for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                    {
                        // Write the count of patch records for this chunk
                        var recordCount = bufferPatchesCountPerChunk[curChunkIndex++];
                        w.ImHexWrite<int>("recordCount");
                        w.Write(recordCount);

                        if (recordCount > 0)
                        {
                            // Write heap backed data for each required patch.
                            // TODO: PERF: Investigate static-only deserialization could manage one block and mark in pointers somehow that they are not individual
                            for (int i = 0; i < recordCount; ++i)
                            {
                                var patch = bufferPatches[curRecordStartIndex + i];
                                var bufferData = (void*)bufferDataList[bufferDataRecordIndex++];
                                w.ImHexWrite<int>("chunkOffset");
                                w.Write(patch.ChunkOffset);
                                w.ImHexWrite<int>("allocSizeBytes");
                                w.Write(patch.AllocSizeBytes);
                                w.ImHexWriteArray<byte>("bufferData", patch.AllocSizeBytes);
                                w.WriteBytes(bufferData, patch.AllocSizeBytes);
                                Memory.Unmanaged.Free(bufferData, Allocator.Persistent);
                            }
                            curRecordStartIndex += recordCount;
                        }
                    }
                }
            }
        }

        private static unsafe void WriteChunks(NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos,
            DotsSerializationWriter dotsWriter,
            UnsafePtrList<Archetype> archetypeArray,
            Chunk* tempChunk,
            NativeArray<int> blobAssetOffsets,
            NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap,
            NativeList<BufferPatchRecord> bufferPatches,
            NativeList<int> bufferPatchesCountPerChunk,
            NativeList<IntPtr> bufferDataList,
            NativeParallelHashSet<UntypedWeakReferenceId> weakAssetRefs,
            UnityObjectRefMap unityObjectRefs)
        {
            using (var chunkNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(ChunksNodeType))
            using (var writerHandle = chunkNode.GetWriterHandle())
            {
                chunkNode.NodeHeader.Revision = 1;

                var w = writerHandle.Writer;
                var imHexEnabled = Hint.Unlikely(ImHexSettings.Enabled);

                var paddingMaskCache = new UnsafeHashMap<int, NativeArray<byte>>(16, Allocator.Temp);
                for (var a = 0; a < archetypeArray.Length; ++a)
                {
                    var archetype = archetypeArray.Ptr[a];

                    if (imHexEnabled)
                    {
                        for (var ti = 0; ti < archetype->TypesCount; ti++)
                            w.ImHexPattern?.RegisterSerializedType(archetype->Types[ti].TypeIndex);
                    }

                    for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                    {
                        var chunk = archetype->Chunks[ci];

                        UnsafeUtility.MemCpy(tempChunk, chunk.GetPtr(), Chunk.kChunkSize);

                        var tempChunkBuffer = tempChunk->Buffer;
                        var entityCount = chunk.Count;

                        tempChunk->CountForSerialization = entityCount;
                        tempChunk->metaChunkEntity = EntityRemapUtility.RemapEntity(ref entityRemapInfos, tempChunk->metaChunkEntity);

                        // Prevent patching from touching buffers allocated memory
                        BufferHeader.PatchAfterCloningChunk(archetype, tempChunkBuffer, entityCount);

                        EntityRemapUtility.PatchEntities(archetype->ScalarEntityPatches, archetype->ScalarEntityPatchCount, archetype->BufferEntityPatches, archetype->BufferEntityPatchCount,
                            tempChunkBuffer, entityCount, ref entityRemapInfos);
                        if (archetype->HasBlobAssetRefs)
                            PatchBlobAssetsInChunkBeforeSave(archetype, tempChunkBuffer, entityCount, blobAssetOffsets, blobAssetMap);

                        if (archetype->HasWeakAssetRefs)
                            GetWeakAssetRefsInChunk(archetype, tempChunkBuffer, entityCount, weakAssetRefs);

                        if (archetype->HasUnityObjectRefs)
                            GetAndPatchUnityObjectRefsInChunkBeforeSave(archetype, tempChunkBuffer, entityCount, unityObjectRefs);

                        ClearStructPadding(archetype, tempChunkBuffer, entityCount, ref paddingMaskCache);
                        ClearChunkHeaderComponents(archetype, tempChunkBuffer, entityCount);
                        ChunkDataUtility.MemsetUnusedChunkData(archetype, tempChunkBuffer, 0, entityCount);
                        var startPatchesIndex = bufferPatches.Length;
                        FillBufferPatchRecordsAndClearBufferPointer(archetype, tempChunkBuffer, entityCount, bufferPatches, bufferDataList);

                        var recordCount = bufferPatches.Length - startPatchesIndex;
                        bufferPatchesCountPerChunk.Add(recordCount);

                        tempChunk->ArchetypeIndexForSerialization = a;

                        w.WriteBytes(tempChunk, Chunk.kChunkSize);
                    }
                }
            }
        }

        private static unsafe void WriteSharedAndManagedComponents(EntityManager entityManager, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos, DotsSerializationWriter dotsWriter,
            NativeArray<int> sharedComponentArrays, NativeArray<int> unmanagedSharedComponentIndices,
            NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap, NativeArray<int> blobAssetOffsets,
            UnityObjectRefMap unityObjectRefs
            )
        {
            using (var sharedComponentsNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(SharedAndManagedComponentsNodeType))
            using (var writerHandle = sharedComponentsNode.GetWriterHandle())
            {
                var w = writerHandle.Writer;
                w.ImHexWrite<int>("sharedComponentArray_Length");
                w.Write(sharedComponentArrays.Length);
                w.WriteArray(sharedComponentArrays);
                sharedComponentArrays.Dispose();

                WriteUnmanagedSharedComponents(
                    entityManager,
                    unmanagedSharedComponentIndices,
                    w,
                    unityObjectRefs,
                    (EntityRemapUtility.EntityRemapInfo*) entityRemapInfos.GetUnsafePtr(),
                    blobAssetMap,
                    blobAssetOffsets);

                WriteEmptyManagedComponents(w);
            }
        }

        private static unsafe void WriteEnabledBits(DotsSerializationWriter dotsWriter, UnsafePtrList<Archetype> archetypeArray)
        {
            var enabledBitsDataSizeInBytes = 0;
            var enabledBitsHierarchicalDataSizeInBytes = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
            {
                var archetype = archetypeArray[archetypeIndex];
                enabledBitsDataSizeInBytes += archetype->Chunks.Count * (int)archetype->Chunks.ComponentEnabledBitsSizeTotalPerChunk;
                enabledBitsHierarchicalDataSizeInBytes += archetype->Chunks.Count * (int)archetype->Chunks.ComponentEnabledBitsHierarchicalDataSizePerChunk;
            }

            using (var enabledBitsNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(EnabledBitsNodeType))
            using (var writerHandle = enabledBitsNode.GetWriterHandle())
            {
                var w = writerHandle.Writer;
                w.ImHexWrite<int>("enabledBitsDataSizeInBytes");
                w.Write(enabledBitsDataSizeInBytes);
                for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
                {
                    var archetype = archetypeArray[archetypeIndex];
                    var chunks = archetype->Chunks;

                    var bitsPtr = chunks.GetPointerToComponentEnabledArrayForArchetype();
                    var bitsSizeInBytesForArchetype = chunks.Count * (int)archetype->Chunks.ComponentEnabledBitsSizeTotalPerChunk;
                    w.ImHexWriteArray<byte>($"enableBitsForArchetype_{archetypeIndex}", bitsSizeInBytesForArchetype);
                    w.WriteBytes(bitsPtr, bitsSizeInBytesForArchetype);
                }

                w.ImHexWrite<int>("enabledBitsHierarchicalDataSizeInBytes");
                w.Write(enabledBitsHierarchicalDataSizeInBytes);
                for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
                {
                    var archetype = archetypeArray[archetypeIndex];
                    var chunks = archetype->Chunks;

                    var hierarchicalDataPtr = chunks.GetPointerToChunkDisabledCountForArchetype();
                    var hierarchicalDataSizeInBytesForArchetype = chunks.Count * (int)archetype->Chunks.ComponentEnabledBitsHierarchicalDataSizePerChunk;
                    w.ImHexWriteArray<byte>($"hierarchicalDataForArchetype_{archetypeIndex}", hierarchicalDataSizeInBytesForArchetype);
                    w.WriteBytes(hierarchicalDataPtr, hierarchicalDataSizeInBytesForArchetype);
                }
            }
        }

        private static void WriteBlobAssetNode(EntityManager entityManager, DotsSerializationWriter dotsWriter,
            UnsafePtrList<Archetype> archetypeArray, out NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap, out NativeArray<int> blobAssetOffsets)
        {
            using (var blobAssetNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(BlobAssetsNodeType))
            using (var writerHandle = blobAssetNode.GetWriterHandle())
            {
                blobAssetNode.NodeHeader.Revision = 1;
                GatherAllUsedBlobAssets(entityManager, archetypeArray, out var blobAssets, out blobAssetMap);
                SortBlobAssetsByHash(ref blobAssets, ref blobAssetMap);
                WriteBlobAssetBatch(writerHandle.Writer, blobAssets.AsArray(), out blobAssetOffsets);
                blobAssets.Dispose();
            }
        }

        static void SortBlobAssetsByHash(ref NativeList<BlobAssetPtr> blobAssets, ref NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            if (blobAssets.Length <= 1)
                return;

            var array = blobAssets.AsArray();
            array.Sort(new BlobAssetPtrHashComparer());

            // Rebuild map with new sorted indices
            blobAssetMap.Clear();
            for (var i = 0; i < array.Length; i++)
                blobAssetMap[array[i]] = i;
        }

        private static unsafe void WritePrefabNode(DotsSerializationWriter dotsWriter, Entity prefabRoot)
        {
            using (var prefabNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(PrefabNodeType))
            using (var writerHandle = prefabNode.GetWriterHandle())
            {
                prefabNode.NodeHeader.Revision = 1;
                writerHandle.Writer.ImHexWrite<Entity>("prefabNode");
                writerHandle.Writer.WriteBytes(&prefabRoot, sizeof(Entity));
            }
        }

        internal unsafe static int CalculateBlobAssetBatchTotalSize(NativeArray<BlobAssetPtr> blobAssets, out NativeArray<int> blobAssetOffsets)
        {
            blobAssetOffsets = new NativeArray<int>(blobAssets.Length, Allocator.Temp);
            int totalBlobAssetSize = sizeof(BlobAssetBatch);

            for (int i = 0; i < blobAssets.Length; ++i)
            {
                totalBlobAssetSize += sizeof(BlobAssetHeader);
                blobAssetOffsets[i] = totalBlobAssetSize;
                totalBlobAssetSize += Align16(blobAssets[i].Header->Length);
            }

            return totalBlobAssetSize;
        }

        internal static unsafe void WriteBlobAssetBatch(BinaryWriter writer, NativeArray<BlobAssetPtr> blobAssets, int totalBlobAssetBatchSize)
        {
            var blobAssetBatch = BlobAssetBatch.CreateForSerialize(blobAssets.Length, totalBlobAssetBatchSize);
            writer.ImHexWrite<BlobAssetBatch>("blobAssetBatch");
            writer.WriteBytes(&blobAssetBatch, sizeof(BlobAssetBatch));
            var zeroBytes = int4.zero;
            for (int i = 0; i < blobAssets.Length; ++i)
            {
                var blobAssetLength = blobAssets[i].Header->Length;
                var blobAssetHash = blobAssets[i].Header->Hash;
                var header = BlobAssetHeader.CreateForSerialize(Align16(blobAssetLength), blobAssetHash);
                writer.ImHexWrite<BlobAssetHeader>("blobAssetHeader");
                writer.WriteBytes(&header, sizeof(BlobAssetHeader));
                writer.ImHexWriteArray<byte>("blobAsset", blobAssetLength);
                writer.WriteBytes(blobAssets[i].Header + 1, blobAssetLength);
                writer.ImHexWriteArray<byte>("zeroBytes", header.Length - blobAssetLength);
                writer.WriteBytes(&zeroBytes, header.Length - blobAssetLength);
            }
        }

        internal static void WriteBlobAssetBatch(BinaryWriter writer, NativeArray<BlobAssetPtr> blobAssets, out NativeArray<int> blobAssetOffsets)
        {
            var totalBlobAssetBatchSize = CalculateBlobAssetBatchTotalSize(blobAssets, out blobAssetOffsets);
            WriteBlobAssetBatch(writer, blobAssets, totalBlobAssetBatchSize);
        }

        private static void BuildSharedComponents(UnsafePtrList<Archetype> archetypeArray, out NativeArray<int> sharedComponentArrays,
            out NativeArray<int> sharedComponentsToSerialize)
        {
            var sharedComponentMapping = GatherSharedComponents(archetypeArray, out var sharedComponentArraysTotalCount);
            sharedComponentArrays = new NativeArray<int>(sharedComponentArraysTotalCount, Allocator.Temp);
            FillSharedComponentArrays(sharedComponentArrays, archetypeArray, sharedComponentMapping);

            sharedComponentsToSerialize = new NativeArray<int>(sharedComponentMapping.Count() - 1, Allocator.Temp);
            using (var keyArray = sharedComponentMapping.GetKeyArray(Allocator.Temp))
            {
                foreach (var key in keyArray)
                {
                    if (key == 0)
                        continue;

                    if (sharedComponentMapping.TryGetValue(key, out var val))
                        sharedComponentsToSerialize[val - 1] = key;
                }
            }
        }

        private static unsafe void WriteTypeNames(DotsSerializationWriter dotsWriter, ref EntityManager em)
        {
            var entityComponentStore = em.GetCheckedEntityDataAccess()->EntityComponentStore;

            var typesHashSet = new UnsafeParallelHashSet<TypeIndex>(1024, Allocator.Temp);
            var archetypeArray = GetAllArchetypes(entityComponentStore, Allocator.Temp);
            for (int i = 0; i != archetypeArray.Length; i++)
            {
                var archetype = archetypeArray.Ptr[i];
                for (int iType = 0; iType < archetype->TypesCount; ++iType)
                    typesHashSet.Add(archetype->Types[iType].TypeIndex);
            }

            using (var stringTable = dotsWriter.CreateStringTableNode(TypesNameStringTableNodeType))
            {
                using (var typesNameNode = dotsWriter.CreateNode<DotsSerialization.TypeNamesNode>(TypesNameNodeType))
                using (var writerHandle = typesNameNode.GetWriterHandle())
                {
                    var writer = writerHandle.Writer;
                    typesNameNode.NodeHeader.TypeCount = typesHashSet.Count();

                    foreach (var typeIndex in typesHashSet)
                    {
                        ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
                        writer.ImHexWrite<ulong>("StableTypeHash");
                        writer.Write(typeInfo.StableTypeHash);
                        writer.ImHexWrite<int>("IndexToNameInStringTable");
                        writer.Write(stringTable.WriteString(typeInfo.DebugTypeName.ToString()));
                    }
                }
            }

            typesHashSet.Dispose();
        }

        static unsafe void WriteChunkLayouts(DotsSerializationWriter dotsWriter, EntityComponentStore* entityComponentStore)
        {
            using var layoutNode = dotsWriter.CreateNode<DotsSerialization.ChunkLayoutNode>(ChunkLayoutNodeType);
            using var writerHandle = layoutNode.GetWriterHandle();
            var writer = writerHandle.Writer;

            var archetypeArray = GetAllArchetypes(entityComponentStore, Allocator.Temp);

            writer.Write(Chunk.kChunkSize);
            writer.Write(Chunk.kBufferOffset);

            layoutNode.NodeHeader.ArchetypeCount = archetypeArray.Length;
            writer.Write(archetypeArray.Length);

            var maxTypesCount = 0;
            for (int a = 0; a < archetypeArray.Length; a++)
            {
                if (archetypeArray.Ptr[a]->TypesCount > maxTypesCount)
                    maxTypesCount = archetypeArray.Ptr[a]->TypesCount;
            }
            var sortedIndices = new NativeArray<int>(maxTypesCount, Allocator.Temp);

            for (int a = 0; a < archetypeArray.Length; a++)
            {
                var archetype = archetypeArray.Ptr[a];
                writer.Write(archetype->ChunkCapacity);

                for (int i = 0; i < archetype->TypesCount; i++)
                    sortedIndices[i] = i;
                var slice = sortedIndices.GetSubArray(0, archetype->TypesCount);
                slice.Sort(new OffsetComparer(archetype));

                var componentCount = 0;
                for (int s = 0; s < archetype->TypesCount; s++)
                {
                    if (!archetype->Types[sortedIndices[s]].TypeIndex.IsBuffer)
                        componentCount++;
                }
                writer.Write(componentCount);

                for (int s = 0; s < archetype->TypesCount; s++)
                {
                    int i = sortedIndices[s];
                    if (archetype->Types[i].TypeIndex.IsBuffer)
                        continue;
                    var typeInfo = TypeManager.GetTypeInfo(archetype->Types[i].TypeIndex);
                    writer.Write(typeInfo.StableTypeHash);
                    writer.Write(archetype->Offsets[i]);
                    writer.Write((int)archetype->SizeOfs[i]);
                }
            }
            sortedIndices.Dispose();
        }

        unsafe struct OffsetComparer : IComparer<int>
        {
            Archetype* m_Archetype;
            public OffsetComparer(Archetype* archetype) { m_Archetype = archetype; }
            public int Compare(int a, int b) => m_Archetype->Offsets[a].CompareTo(m_Archetype->Offsets[b]);
        }

        private static unsafe UnsafePtrList<Archetype> WriteArchetypesNode(DotsSerializationWriter dotsWriter, EntityComponentStore* entityComponentStore)
        {
            UnsafePtrList<Archetype> archetypeArray;
            using (var archetypeNode = dotsWriter.CreateNode<DotsSerialization.RevisionedRawDataNode>(ArchetypesNodeType))
            using (var writerHandle = archetypeNode.GetWriterHandle())
            {
                archetypeNode.NodeHeader.Revision = 1;

                var writer = writerHandle.Writer;

                archetypeArray = GetAllArchetypes(entityComponentStore, Allocator.Temp);

                var typeHashToIndexMap = new UnsafeHashMap<ulong, int>(archetypeArray.Length * 8, Allocator.Temp);
                var typeHashToTypeIndex = Hint.Unlikely(ImHexSettings.Enabled)
                    ? new UnsafeHashMap<ulong, TypeIndex>(archetypeArray.Length * 8, Allocator.Temp)
                    : default;
                for (var i = 0; i != archetypeArray.Length; i++)
                {
                    var archetype = archetypeArray.Ptr[i];
                    for (var iType = 0; iType < archetype->TypesCount; ++iType)
                    {
                        var typeIndex = archetype->Types[iType].TypeIndex;
                        var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                        var hash = typeInfo.StableTypeHash;

                        ValidateTypeForSerialization(typeInfo);

                        typeHashToIndexMap.TryAdd(hash, i);
                        if (Hint.Unlikely(ImHexSettings.Enabled))
                            typeHashToTypeIndex.TryAdd(hash, typeIndex);
                    }
                }

                using (var typeHashSet = typeHashToIndexMap.GetKeyArray(Allocator.Temp))
                {
                    writer.ImHexWrite<int>("typeHashSet_Length");
                    writer.Write(typeHashSet.Length);
                    foreach (ulong hash in typeHashSet)
                    {
                        if (Hint.Unlikely(ImHexSettings.Enabled))
                        {
                            var typeInfo = TypeManager.GetTypeInfo(typeHashToTypeIndex[hash]);
                            writer.ImHexPattern?.WriteTypeWithPosition<ulong>($"typeHashSet", writer.Position, $"Belongs to type: {typeInfo.Type}");
                        }
                        writer.Write(hash);
                    }

                    for (int i = 0; i < typeHashSet.Length; ++i)
                        typeHashToIndexMap[typeHashSet[i]] = i;
                }

                WriteArchetypes(writer, archetypeArray, typeHashToIndexMap);
                typeHashToIndexMap.Dispose();
                if (typeHashToTypeIndex.IsCreated)
                    typeHashToTypeIndex.Dispose();
            }

            return archetypeArray;
        }

        static int Align16(int x)
        {
            return (x + 15) & ~15;
        }

        // Managed shared components and managed components are no longer supported, but the file format still has
        // their section. I write it empty so files keep the same layout, and the reader rejects any file where it isn't.
        static void WriteEmptyManagedComponents(BinaryWriter writer)
        {
            writer.ImHexWrite<int>("sharedComponentRecordArray_Length");
            writer.Write(0);
            writer.ImHexWrite<int>("allManagedObjectsBuffer_Length");
            writer.Write(0);
            writer.ImHexWrite<int>("managedComponentCount");
            writer.Write(0);
        }

        static unsafe void WriteUnmanagedSharedComponents(
            EntityManager entityManager,
            NativeArray<int> unmanagedSharedComponentIndices,
            BinaryWriter writer,
            UnityObjectRefMap unityObjectRefs,
            EntityRemapUtility.EntityRemapInfo* remapping,
            NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap,
            NativeArray<int> blobAssetOffsets)
        {
            var allUnmanagedSharedComponentBuffer = new UnsafeAppendBuffer(0, 16, Allocator.Temp);

            var access = entityManager.GetCheckedEntityDataAccess();

            var sharedComponentRecordArray = new NativeArray<SharedComponentRecord>(unmanagedSharedComponentIndices.Length, Allocator.Temp);
            var paddingMaskCache = new UnsafeHashMap<int, NativeArray<byte>>(16, Allocator.Temp);
            for (int i = 0; i < unmanagedSharedComponentIndices.Length; ++i)
            {
                var index = unmanagedSharedComponentIndices[i];
                Assert.IsTrue(Entities.EntityComponentStore.IsUnmanagedSharedComponentIndex(index));
                sharedComponentRecordArray[i] = WriteUnmanagedSharedComponent(index, remapping, blobAssetMap, blobAssetOffsets, access, ref allUnmanagedSharedComponentBuffer, unityObjectRefs, ref paddingMaskCache);
            }

            writer.ImHexWrite<int>("sharedComponentRecordArray_Length");
            writer.Write(sharedComponentRecordArray.Length);
            writer.WriteArray(sharedComponentRecordArray);

            writer.ImHexWrite<int>("allUnmanagedSharedComponentBuffer_Length");
            writer.Write(allUnmanagedSharedComponentBuffer.Length);
            writer.ImHexWriteArray<byte>("allUnmanagedSharedComponentBuffer", allUnmanagedSharedComponentBuffer.Length);
            writer.WriteBytes(allUnmanagedSharedComponentBuffer.Ptr, allUnmanagedSharedComponentBuffer.Length);

            sharedComponentRecordArray.Dispose();
            allUnmanagedSharedComponentBuffer.Dispose();
        }

        static unsafe SharedComponentRecord WriteUnmanagedSharedComponent(
            int sharedComponentIndex,
            EntityRemapUtility.EntityRemapInfo* remapping,
            NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap,
            NativeArray<int> blobAssetOffsets, EntityDataAccess* access,
            ref UnsafeAppendBuffer allManagedObjectsBuffer,
            UnityObjectRefMap unityObjectRefs,
            ref UnsafeHashMap<int, NativeArray<byte>> paddingMaskCache)
        {
            var typeIndex = Entities.EntityComponentStore.GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
            var dataPtr = (byte*)access->EntityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
            ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
            var size = typeInfo.TypeSize;

            Assert.IsTrue(size >= 0);

            var record = new SharedComponentRecord()
            {
                StableTypeHash = typeInfo.StableTypeHash,
                ComponentSize = size
            };

            // entity remapping in shared components is disabled until a clear decision is made on how to handle it DOTS-5038

            if (typeInfo.BlobAssetRefOffsetCount > 0)
            {
                PatchBlobAssetRefInfoBeforeSave(dataPtr, TypeManager.GetBlobAssetRefOffsets(typeInfo), typeInfo.BlobAssetRefOffsetCount, blobAssetOffsets, blobAssetMap);
            }

            if (typeInfo.HasUnityObjectRefs)
                GetAndPatchUnityObjectRefsInComponentBeforeSave(dataPtr, TypeManager.GetUnityObjectRefOffsets(typeInfo), typeInfo.UnityObjectRefOffsetCount, unityObjectRefs);

            var paddingMask = GetOrBuildPaddingMask(typeIndex, size, ref paddingMaskCache);
            if (paddingMask.IsCreated)
                ApplyPaddingMask(dataPtr, size, (byte*)paddingMask.GetUnsafeReadOnlyPtr());

            allManagedObjectsBuffer.Add(dataPtr, size);
            return record;
        }

        static unsafe int ReadUnmanagedSharedComponents(ExclusiveEntityTransaction manager, BinaryReader reader, NativeList<int> sharedComponentRemap,
            SerializeUtility.WorldDeserializationStatus* status, NativeArray<EntityId> unityObjects)
        {
            byte* blobAssetBuffer = (byte*)status->BlobAssetBuffer;

            var unmanagedSharedComponentCount = ReadSharedComponentMetadata(reader, out var sharedComponentRecordArray);

            int dataSize = reader.ReadInt();

            using var unmanagedComponentBuffer = new UnsafeAppendBuffer(dataSize, 16, Allocator.Temp);
            unmanagedComponentBuffer.ResizeUninitialized(dataSize);
            reader.ReadBytes(unmanagedComponentBuffer.Ptr, dataSize);
            var unmanagedStream = unmanagedComponentBuffer.AsReader();

            var access = manager.EntityManager.GetCheckedEntityDataAccess();
            for (int i = 0; i < sharedComponentRecordArray.Length; ++i)
            {
                var record = sharedComponentRecordArray[i];
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(record.StableTypeHash);
                int sharedComponentIndex;
                {
                    byte* data = (byte*)unmanagedStream.ReadNext(record.ComponentSize);

                    var ct = TypeManager.GetTypeInfo(typeIndex);
                    var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                    if (blobAssetRefCount > 0)
                    {
                        var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);

                        // We patch the blob asset ref
                        for (int blobIndex = 0; blobIndex < blobAssetRefCount; ++blobIndex)
                        {
                            var offset = blobAssetRefOffsets[blobIndex].Offset;
                            var blobAssetRefPtr = (BlobAssetReferenceData*)(data + offset);
                            int value = (int)blobAssetRefPtr->m_Ptr;
                            byte* ptr = null;
                            if (value != -1)
                            {
                                ptr = blobAssetBuffer + value;
                            }
                            blobAssetRefPtr->m_Ptr = ptr;
                        }
                    }

                    if (ct.HasUnityObjectRefs)
                        SerializeUtilityInterop.PatchUnityObjectRefsInComponentAfterLoad(data, TypeManager.GetUnityObjectRefOffsets(ct), ct.UnityObjectRefOffsetCount, unityObjects);

                    var hashCode = TypeManager.GetHashCode(data, typeIndex);
                    sharedComponentIndex = access->EntityComponentStore->InsertSharedComponent_Unmanaged(typeIndex, hashCode, data, null);
                }
                // When deserializing a shared component it is possible that its hashcode changes if for example the referenced object (a UnityEngine.Object for example) becomes null.
                // This can result in the sharedComponentIndex at serialize time being different from the sharedComponentIndex at load time.
                // Thus we keep a remap table to handle this potential remap.
                // NOTE: in most cases the remap table will always be all indices matching,
                // But it doesn't look like it's worth optimizing this away at this point.
                sharedComponentRemap.Add(sharedComponentIndex);
            }

            return unmanagedSharedComponentCount;
        }

        static void ReadEmptyManagedComponents(BinaryReader reader)
        {
            var managedSharedComponentCount = reader.ReadInt();
            var managedDataSize = reader.ReadInt();
            var managedComponentCount = reader.ReadInt();
            if (managedSharedComponentCount != 0 || managedDataSize != 0 || managedComponentCount != 0)
                throw new InvalidOperationException("The serialized world contains managed components or managed shared components, which are no longer supported. Re-bake or re-serialize it with this version of Entities.");
        }

        // True when a component is valid to using in world serialization. A component IsSerializable when it is valid to blit
        // the data across storage media. Thus components containing pointers have an IsSerializable of false as the component
        // is blittable but no longer valid upon deserialization.
        private static bool IsTypeValidForSerialization(Type type, HashSet<Type> validTypes)
        {
            if (validTypes.Contains(type))
                return true;
            if (type.GetCustomAttribute<ChunkSerializableAttribute>() != null)
                return true;

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.IsStatic)
                    continue;

                if (field.FieldType.IsPointer || (field.FieldType == typeof(UIntPtr) || field.FieldType == typeof(IntPtr)))
                {
                    return false;
                }
                else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                {
                    if (!IsTypeValidForSerialization(field.FieldType, validTypes))
                        return false;
                }
            }

            validTypes.Add(type);

            return true;
        }

        [BurstDiscard]
        private static void ValidateTypeForSerialization(TypeManager.TypeInfo typeInfo)
        {
            if (typeInfo.TypeIndex.IsChunkSerializable)
                return;

            if (typeInfo.Category == TypeManager.TypeCategory.ISharedComponentData && typeInfo.TypeIndex.HasEntityReferences)
            {
                throw new ArgumentException(
                    $"Shared component type '{typeInfo.Type}' might contain a (potentially nested) Entity field. " +
                    $"Serializing of shared components with Entity fields is not supported as Entity references are not patched when deserializing. " +
                    $"If for whatever reason this component should still be serialized, add the [ChunkSerializable] attribute to your type to bypass this error.");
            }

            throw new ArgumentException($"Blittable component type '{typeInfo.Type}' contains a (potentially nested) pointer field. " +
                                        $"Serializing bare pointers will likely lead to runtime errors. Remove this field and consider serializing the data " +
                                        $"it points to another way such as by using a BlobAssetReference or a [Serializable] ISharedComponent. If for whatever " +
                                        $"reason the pointer field should in fact be serialized, add the [ChunkSerializable] attribute to your type to bypass this error.");
        }

        static int ReadSharedComponentMetadata(BinaryReader reader, out NativeArray<SharedComponentRecord> sharedComponentRecordArray)
        {
            var sharedComponentRecordArrayLength = reader.ReadInt();
            sharedComponentRecordArray = new NativeArray<SharedComponentRecord>(sharedComponentRecordArrayLength, Allocator.Temp);
            reader.ReadArray(sharedComponentRecordArray, sharedComponentRecordArrayLength);

            return sharedComponentRecordArrayLength;
        }

        static unsafe void GatherAllUsedBlobAssets(
            EntityManager entityManager,
            UnsafePtrList<Archetype> archetypeArray,
            out NativeList<BlobAssetPtr> blobAssets,
            out NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            blobAssetMap = new NativeParallelHashMap<BlobAssetPtr, int>(100, Allocator.TempJob);

            blobAssets = new NativeList<BlobAssetPtr>(100, Allocator.TempJob);

            var access = entityManager.GetCheckedEntityDataAccess();
            for (int a = 0; a < archetypeArray.Length; ++a)
            {
                var archetype = archetypeArray.Ptr[a];
                if (!archetype->HasBlobAssetRefs)
                    continue;

                var typeCount = archetype->TypesCount;
                for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                {
                    var chunk = archetype->Chunks[ci];
                    var entityCount = chunk.Count;
                    for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
                    {
                        var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                        var type = archetype->Types[ti];
                        if (type.IsZeroSized)
                            continue;

                        ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                        var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                        if (blobAssetRefCount == 0)
                            continue;

                        var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                        var chunkBuffer = chunk.Buffer;

                        if (blobAssetRefCount > 0)
                        {
                            int subArrayOffset = archetype->Offsets[ti];
                            byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                            if (type.IsBuffer)
                            {
                                BufferHeader* header = (BufferHeader*)componentArrayStart;
                                int strideSize = archetype->SizeOfs[ti];
                                int elementSize = ct.ElementSize;

                                for (int bi = 0; bi < entityCount; ++bi)
                                {
                                    var bufferStart = BufferHeader.GetElementPointer(header);
                                    var bufferEnd = bufferStart + header->Length * elementSize;
                                    for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                                    {
                                        AddBlobAssetRefInfo(componentData, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);
                                    }

                                    header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                                }
                            }
                            else
                            {
                                int componentSize = archetype->SizeOfs[ti];
                                byte* end = componentArrayStart + componentSize * entityCount;
                                for (var componentData = componentArrayStart; componentData < end; componentData += componentSize)
                                {
                                    AddBlobAssetRefInfo(componentData, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);
                                }
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

                        var dataPtr = (byte*)access->EntityComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
                        var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                        AddBlobAssetRefInfo(dataPtr, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);
                    }
                }
            }
        }

        private static unsafe void AddBlobAssetRefInfo(byte* componentData, TypeManager.EntityOffsetInfo* blobAssetRefOffsets, int blobAssetRefCount,
            ref NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap, ref NativeList<BlobAssetPtr> blobAssets)
        {
            for (int i = 0; i < blobAssetRefCount; ++i)
            {
                var blobAssetRefOffset = blobAssetRefOffsets[i].Offset;
                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + blobAssetRefOffset);
                if (blobAssetRefPtr->m_Ptr == null)
                    continue;

                var blobAssetPtr = new BlobAssetPtr(blobAssetRefPtr->Header);
                if (!blobAssetMap.TryGetValue(blobAssetPtr, out var blobAssetIndex))
                {
                    blobAssetIndex = blobAssets.Length;
                    blobAssets.Add(blobAssetPtr);
                    blobAssetMap.TryAdd(blobAssetPtr, blobAssetIndex);
                }
            }
        }

        private static unsafe void GetAndPatchUnityObjectRefsInChunkBeforeSave(Archetype* archetype, byte* chunkBuffer, int entityCount, UnityObjectRefMap unityObjectRefs)
        {
            var typeCount = archetype->TypesCount;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var unityObjectRefCount = ct.UnityObjectRefOffsetCount;

                if (unityObjectRefCount == 0)
                    continue;

                var unityObjectRefOffsets = TypeManager.GetUnityObjectRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            GetAndPatchUnityObjectRefsInComponentBeforeSave(componentData, unityObjectRefOffsets, unityObjectRefCount, unityObjectRefs);
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        GetAndPatchUnityObjectRefsInComponentBeforeSave(componentData, unityObjectRefOffsets, unityObjectRefCount, unityObjectRefs);
                    }
                }
            }
        }

        private static unsafe void GetAndPatchUnityObjectRefsInComponentBeforeSave(byte* componentData, TypeManager.EntityOffsetInfo* unityObjectRefOffsets, int unityObjectRefCount,
            UnityObjectRefMap unityObjectRefs)
        {
            for (int i = 0; i < unityObjectRefCount; ++i)
            {
                var unityObjectRefOffset = unityObjectRefOffsets[i].Offset;
                var unityObjectRefPtr = (UntypedUnityObjectRef*)(componentData + unityObjectRefOffset);
                var value = unityObjectRefs.Add(unityObjectRefPtr->entityId);
                unityObjectRefPtr->entityId = EntityId.FromULong((ulong)value);
            }
        }

        private static unsafe void GetWeakAssetRefsInChunk(Archetype* archetype, byte* chunkBuffer, int entityCount, NativeParallelHashSet<UntypedWeakReferenceId> weakAssetRefs)
        {
            var typeCount = archetype->TypesCount;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var weakAssetRefCount = ct.WeakAssetRefOffsetCount;

                if (weakAssetRefCount == 0)
                    continue;

                var weakAssetRefOffsets = TypeManager.GetWeakAssetRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            GetWeakAssetRefsInComponent(componentData, weakAssetRefOffsets, weakAssetRefCount, weakAssetRefs);
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        GetWeakAssetRefsInComponent(componentData, weakAssetRefOffsets, weakAssetRefCount, weakAssetRefs);
                    }
                }
            }
        }

        private static unsafe void GetWeakAssetRefsInComponent(byte* componentData, TypeManager.EntityOffsetInfo* weakAssetRefOffsets, int weakAssetRefCount,
            NativeParallelHashSet<UntypedWeakReferenceId> weakAssetRefs)
        {
            for (int i = 0; i < weakAssetRefCount; ++i)
            {
                var weakAssetRef = *(UntypedWeakReferenceId*)(componentData + weakAssetRefOffsets[i].Offset);
                weakAssetRefs.Add(weakAssetRef);
            }
        }

        private static unsafe void PatchBlobAssetsInChunkBeforeSave(Archetype* archetype, byte* chunkBuffer, int entityCount,
            NativeArray<int> blobAssetOffsets, NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            var typeCount = archetype->TypesCount;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrderIndexToIndexInArchetype[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                if (blobAssetRefCount == 0)
                    continue;

                var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            PatchBlobAssetRefInfoBeforeSave(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetOffsets, blobAssetMap);
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else if (blobAssetRefCount > 0)
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        PatchBlobAssetRefInfoBeforeSave(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetOffsets, blobAssetMap);
                    }
                }
            }
        }

        private static unsafe void PatchBlobAssetRefInfoBeforeSave(byte* componentData, TypeManager.EntityOffsetInfo* blobAssetRefOffsets, int blobAssetRefCount,
            NativeArray<int> blobAssetOffsets, NativeParallelHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            for (int i = 0; i < blobAssetRefCount; ++i)
            {
                var blobAssetRefOffset = blobAssetRefOffsets[i].Offset;
                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + blobAssetRefOffset);
                int value = -1;
                if (blobAssetRefPtr->m_Ptr != null)
                {
                    value = blobAssetMap[new BlobAssetPtr(blobAssetRefPtr->Header)];
                    value = blobAssetOffsets[value];
                }
                blobAssetRefPtr->m_Ptr = (byte*)value;
            }
        }

        private static unsafe void FillBufferPatchRecordsAndClearBufferPointer(Archetype* archetype, byte* chunkBuffer, int entityCount, NativeList<BufferPatchRecord> bufferPatches, NativeList<IntPtr> bufferPtrs)
        {
            // Find all buffer pointer locations and work out how much memory the deserializer must allocate on load.
            for (int ti = 0; ti < archetype->TypesCount; ++ti)
            {
                int indexInTypeArray = archetype->TypeMemoryOrderIndexToIndexInArchetype[ti];
                var type = archetype->Types[indexInTypeArray];

                if (type.IsBuffer)
                {
                    Assert.IsFalse(type.IsZeroSized);

                    ref readonly var ct = ref TypeManager.GetTypeInfo(type.TypeIndex);
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var offset = archetype->Offsets[indexInTypeArray];
                        var sizeOf = archetype->SizeOfs[indexInTypeArray];

                        var header = (BufferHeader*)(chunkBuffer + (offset + sizeOf * bi));

                        if (header->Pointer != null)
                        {
                            int capacityInBytes = elementSize * header->Capacity;
                            bufferPatches.Add(new BufferPatchRecord
                            {
                                ChunkOffset = (int)((byte*)header - chunkBuffer),
                                AllocSizeBytes = capacityInBytes
                            });
                            bufferPtrs.Add((IntPtr)header->Pointer);
                            header->Pointer = null;
                        }
                    }
                }
            }
        }

        static unsafe void FillSharedComponentIndexRemap(int* remapArray, Archetype* archetype)
        {
            int i = 0;
            for (int iType = 1; iType < archetype->TypesCount; ++iType)
            {
                int orderedIndex = archetype->TypeMemoryOrderIndexToIndexInArchetype[iType] - archetype->FirstSharedComponent;
                if (0 <= orderedIndex && orderedIndex < archetype->NumSharedComponents)
                    remapArray[i++] = orderedIndex;
            }
        }

        private static unsafe void FillSharedComponentArrays(NativeArray<int> sharedComponentArrays, UnsafePtrList<Archetype> archetypeArray, NativeParallelHashMap<int, int> sharedComponentMapping)
        {
            var sharedComponentIndexRemap = stackalloc int[EntityComponentStore.kMaxSharedComponentCount];
            int index = 0;
            for (int iArchetype = 0; iArchetype < archetypeArray.Length; ++iArchetype)
            {
                var archetype = archetypeArray.Ptr[iArchetype];
                int numSharedComponents = archetype->NumSharedComponents;
                if (numSharedComponents == 0)
                    continue;

                FillSharedComponentIndexRemap(sharedComponentIndexRemap, archetype);
                var archetypeChunkData = archetype->Chunks;
                for (int iChunk = 0; iChunk < archetypeChunkData.Count; ++iChunk)
                {
                    var sharedComponents = archetypeChunkData.GetSharedComponentValues(archetypeChunkData[iChunk].ListIndex);
                    for (int iType = 0; iType < numSharedComponents; iType++)
                    {
                        int remappedIndex = sharedComponentIndexRemap[iType];
                        sharedComponentArrays[index++] = sharedComponentMapping[sharedComponents[remappedIndex]];
                    }
                }
            }
            Assert.AreEqual(sharedComponentArrays.Length, index);
        }

        private static unsafe NativeParallelHashMap<int, int> GatherSharedComponents(UnsafePtrList<Archetype> archetypeArray, out int sharedComponentArraysTotalCount)
        {
            sharedComponentArraysTotalCount = 0;
            var sharedIndexToSerialize = new NativeParallelHashMap<int, int>(1024, Allocator.Temp);
            sharedIndexToSerialize.TryAdd(0, 0); // All default values map to 0
            int nextIndex = 1;
            for (int iArchetype = 0; iArchetype < archetypeArray.Length; ++iArchetype)
            {
                var archetype = archetypeArray.Ptr[iArchetype];
                sharedComponentArraysTotalCount += archetype->Chunks.Count * archetype->NumSharedComponents;

                int numSharedComponents = archetype->NumSharedComponents;
                for (int iType = 0; iType < numSharedComponents; iType++)
                {
                    var sharedComponents = archetype->Chunks.GetSharedComponentValueArrayForType(iType);
                    for (int iChunk = 0; iChunk < archetype->Chunks.Count; ++iChunk)
                    {
                        int sharedComponentIndex = sharedComponents[iChunk];
                        if (!sharedIndexToSerialize.TryGetValue(sharedComponentIndex, out var val))
                        {
                            sharedIndexToSerialize.TryAdd(sharedComponentIndex, nextIndex++);
                        }
                    }
                }
            }

            return sharedIndexToSerialize;
        }

        private static unsafe void ClearChunkHeaderComponents(Archetype* archetype, byte* chunkBuffer, int entityCount)
        {
            var chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, chunkHeaderTypeIndex);
            if (typeIndexInArchetype == -1)
                return;

            var startOffset = archetype->Offsets[typeIndexInArchetype];
            var chunkHeaders = (ChunkHeader*)(chunkBuffer + startOffset);
            for (int i = 0; i < entityCount; ++i)
            {
                chunkHeaders[i] = ChunkHeader.Null;
            }
        }

        static unsafe byte* OffsetFromPointer(void* ptr, int offset)
        {
            return ((byte*)ptr) + offset;
        }

        static unsafe void WriteArchetypes(BinaryWriter writer, UnsafePtrList<Archetype> archetypeArray, UnsafeHashMap<ulong, int> typeHashToIndexMap)
        {
            writer.ImHexWrite<int>("archetypeArray_Length");
            writer.Write(archetypeArray.Length);

            for (int a = 0; a < archetypeArray.Length; ++a)
            {
                var archetype = archetypeArray.Ptr[a];

                writer.ImHexWrite<int>("archetype_EntityCount");
                writer.Write(archetype->EntityCount);
                writer.ImHexWrite<int>("archetype_TypesCount");
                writer.Write(archetype->TypesCount - 1);
                for (int i = 1; i < archetype->TypesCount; ++i)
                {
                    var componentType = archetype->Types[i];
                    int flag = componentType.IsChunkComponent ? TypeManager.ChunkComponentTypeFlag : 0;
                    var hash = TypeManager.GetTypeInfo(componentType.TypeIndex).StableTypeHash;

                    writer.ImHexWrite<int>("archetype_Component_TypeFlags");
                    writer.Write(typeHashToIndexMap[hash] | flag);
                }
            }
        }

        static unsafe int GenerateRemapInfo(EntityManager entityManager, UnsafePtrList<Archetype> archetypeArray, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            // It's valid to use the entity index 0 as the version is set to 1. There is no conflict with Entity.Null (index 0, version 0)
            int nextEntityId = 0;

            int totalChunkCount = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
            {
                var archetype = archetypeArray.Ptr[archetypeIndex];
                for (int i = 0; i < archetype->Chunks.Count; ++i)
                {
                    var chunk = archetype->Chunks[i];
                    for (int iEntity = 0, count = chunk.Count; iEntity < count; ++iEntity)
                    {
                        var entity = *(Entity*)ChunkDataUtility.GetComponentDataRO(chunk, archetype, iEntity, 0);
                        EntityRemapUtility.AddEntityRemapping(ref entityRemapInfos, entity, new Entity { Version = 1, Index = nextEntityId });
                        ++nextEntityId;
                    }

                    totalChunkCount += 1;
                }
            }

            return totalChunkCount;
        }

        static unsafe void ClearStructPadding(Archetype* archetype, byte* chunkBuffer, int entityCount, ref UnsafeHashMap<int, NativeArray<byte>> paddingMaskCache)
        {
            // Regular components: data is stored inline in the chunk buffer.
            for (var ti = 1; ti < archetype->FirstBufferComponent; ti++)
            {
                var typeIndex = archetype->Types[ti].TypeIndex;
                if (archetype->Types[ti].IsZeroSized)
                    continue;

                ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
                var typeSize = typeInfo.SizeInChunk;
                var mask = GetOrBuildPaddingMask(typeIndex, typeSize, ref paddingMaskCache);
                if (!mask.IsCreated)
                    continue;

                var componentOffset = archetype->Offsets[ti];
                var componentStride = archetype->SizeOfs[ti];
                var maskPtr = (byte*)mask.GetUnsafeReadOnlyPtr();

                for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
                {
                    byte* componentData = chunkBuffer + componentOffset + componentStride * entityIndex;
                    ApplyPaddingMask(componentData, typeSize, maskPtr);
                }
            }

            // Buffer components: data lives behind a BufferHeader, so we mask each element individually.
            for (var ti = archetype->FirstBufferComponent; ti < archetype->BufferComponentsEnd; ti++)
            {
                var typeIndex = archetype->Types[ti].TypeIndex;
                ref readonly var typeInfo = ref TypeManager.GetTypeInfo(typeIndex);
                var elementSize = typeInfo.ElementSize;
                if (elementSize == 0) // zero-capacity buffers have no data to clear
                    continue;

                var mask = GetOrBuildPaddingMask(typeIndex, elementSize, ref paddingMaskCache);
                if (!mask.IsCreated)
                    continue;

                var componentOffset = archetype->Offsets[ti];
                var componentStride = archetype->SizeOfs[ti];
                var maskPtr = (byte*)mask.GetUnsafeReadOnlyPtr();

                for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
                {
                    var header = (BufferHeader*)(chunkBuffer + componentOffset + componentStride * entityIndex);
                    var elemPtr = BufferHeader.GetElementPointer(header);
                    var elemCount = header->Length;

                    for (var elementIndex = 0; elementIndex < elemCount; elementIndex++)
                    {
                        byte* elem = elemPtr + (long)elementIndex * elementSize;
                        ApplyPaddingMask(elem, elementSize, maskPtr);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or builds a byte mask that identifies which bytes in a component are data (0xFF) versus
        /// struct padding (0x00). The mask is cached per type index for reuse across multiple instances.
        /// </summary>
        static unsafe NativeArray<byte> GetOrBuildPaddingMask(TypeIndex typeIndex, int size, ref UnsafeHashMap<int, NativeArray<byte>> paddingMaskCache)
        {
            if (paddingMaskCache.TryGetValue(typeIndex.Index, out var cached))
                return cached;

            var fastEqualityInfoPtr = TypeManager.GetFastEqualityTypeInfoPointer();
            var layoutInfo = fastEqualityInfoPtr[typeIndex.Index].LayoutInfo;

            // IEquatable<T> types have no pre-built LayoutInfo, build one on demand.
            NativeArray<FastEquality.LayoutInfo> fallbackLayoutInfo = default;
            if (!layoutInfo.IsCreated || layoutInfo.Length == 0)
            {
                var type = TypeManager.GetTypeInfo(typeIndex).Type;
                if (type != null && UnsafeUtility.IsUnmanaged(type))
                {
                    fallbackLayoutInfo = FastEquality.CreateDescriptor(type);
                    layoutInfo = fallbackLayoutInfo;
                }
            }

            if (!layoutInfo.IsCreated || layoutInfo.Length == 0)
            {
                if (fallbackLayoutInfo.IsCreated)
                    fallbackLayoutInfo.Dispose();
                paddingMaskCache.Add(typeIndex.Index, default);
                return default;
            }

            var mask = new NativeArray<byte>(size, Allocator.Temp);
            var maskPtr = (byte*)mask.GetUnsafePtr();
            FastEquality.FillFieldMask(layoutInfo, maskPtr, size);

            if (fallbackLayoutInfo.IsCreated)
                fallbackLayoutInfo.Dispose();

            var hasPadding = false;
            for (var i = 0; i < size; i++)
            {
                if (maskPtr[i] != 0xFF)
                {
                    hasPadding = true;
                    break;
                }
            }

            if (!hasPadding)
            {
                mask.Dispose();
                paddingMaskCache.Add(typeIndex.Index, default);
                return default;
            }

            paddingMaskCache.Add(typeIndex.Index, mask);
            return mask;
        }

        /// <summary>
        /// Zeroes the padding bytes of a component by ANDing its data with a prebuilt padding mask.
        /// Data bytes (masked 0xFF) are preserved, padding bytes (masked 0x00) are cleared.
        /// </summary>
        static unsafe void ApplyPaddingMask(byte* data, int size, byte* mask)
        {
            for (var i = 0; i < size; i++)
                data[i] &= mask[i];
        }
    }
}
