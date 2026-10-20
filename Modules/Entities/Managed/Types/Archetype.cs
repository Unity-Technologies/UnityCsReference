// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    [Flags]
    internal enum ArchetypeFlags : ushort
    {
        CleanupComplete     = 1 << 0,
        CleanupNeeded       = 1 << 1,
        Disabled            = 1 << 2,
        Prefab              = 1 << 3,
        HasChunkHeader      = 1 << 4,
        HasBlobAssetRefs    = 1 << 5,
        HasCompanionComponents = 1 << 6,
        HasBufferComponents = 1 << 7,
        HasWeakAssetRefs    = 1 << 10,
        HasSystemInstanceComponents = 1 << 11,
        HasUnityObjectRefs  = 1 << 12,
        HasOnAddedCallbacks = 1 << 13,
        HasOnRemovedCallbacks = 1 << 14,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Archetype
    {
        public ArchetypeChunkData Chunks;
        public UnsafeList<ChunkIndex> ChunksWithEmptySlots;

        public ChunkListMap FreeChunksBySharedComponents;
        public ComponentTypeInArchetype* Types; // Array with TypeCount elements, sorted by TypeIndex during CreateArchetype().
        public int* EnableableTypeIndexInArchetype; // Array with EnableableTypesCount elements

        // back pointer to EntityQueryData(s), used for chunk list caching
        public UnsafeList<IntPtr> MatchingQueryData;

        // single-linked list used for invalidating chunk list caches
        public Archetype* NextChangedArchetype;

        public int EntityCount;
        public int ChunkCapacity;

        public int TypesCount;
        public int EnableableTypesCount;
        public int InstanceSize;
        public int InstanceSizeWithOverhead;
        public int ScalarEntityPatchCount;
        public int BufferEntityPatchCount;
        public ulong StableHash;
        public ulong BloomFilterMask;

        // The order that per-component-type data is stored in memory within an archetype does not necessarily match
        // the order that types are stored in the Types/Offsets/SizeOfs/etc. arrays. The memory order of types is stable across
        // runs; the Types array is sorted by TypeIndex, and the TypeIndex <-> ComponentType mapping is *not* guaranteed to
        // be stable across runs.
        // These two arrays each have TypeCount elements, and are used to convert between these two orderings.
        // - MemoryOrderIndex is the order an archetype's component data is actually stored in memory. This is stable across runs.
        // - IndexInArchetype is the order that types appear in the archetype->Types[] array. This is *not* necessarily stable across runs.
        //   (also called IndexInTypeArray in some APIs)
        public int* TypeMemoryOrderIndexToIndexInArchetype; // The Nth element is the IndexInArchetype of the type with MemoryOrderIndex=N
        public int* TypeIndexInArchetypeToMemoryOrderIndex; // The Nth element is the MemoryOrderIndex of the type with IndexInArchetype=N

        // These arrays each have TypeCount elements, ordered by IndexInArchetype (the same order as the Types array)
        public int*    Offsets; // Byte offset of each component type's data within this archetype's chunk buffer.
        public ushort* SizeOfs; // Size in bytes of each component type
        public int*    BufferCapacities; // For IBufferElementData components, the buffer capacity of each component. Not meaningful for non-buffer components.

        // Order of components in the types array is always:
        // Entity, native component data, buffer components, tag component, shared components, chunk components
        public short FirstBufferComponent;
        public short FirstTagComponent;
        public short FirstSharedComponent;
        public short FirstChunkComponent;

        public ArchetypeFlags Flags;

        public Archetype* CopyArchetype; // Removes cleanup components
        public Archetype* InstantiateArchetype; // Removes cleanup components & prefabs
        public Archetype* CleanupResidueArchetype;
        public Archetype* MetaChunkArchetype;

        public EntityRemapUtility.EntityPatchInfo* ScalarEntityPatches;
        public EntityRemapUtility.BufferEntityPatchInfo* BufferEntityPatches;

        // @macton Temporarily store back reference to EntityComponentStore.
        // - In order to remove this we need to sever the connection to CompanionChangesTracker
        //   when structural changes occur.
        public EntityComponentStore* EntityComponentStore;

        public fixed byte QueryMaskArray[128];

        public bool CleanupComplete => (Flags & ArchetypeFlags.CleanupComplete) != 0;
        public bool CleanupNeeded => (Flags & ArchetypeFlags.CleanupNeeded) != 0;
        public bool Disabled => (Flags & ArchetypeFlags.Disabled) != 0;
        public bool Prefab => (Flags & ArchetypeFlags.Prefab) != 0;
        public bool HasChunkHeader => (Flags & ArchetypeFlags.HasChunkHeader) != 0;
        public bool HasBlobAssetRefs => (Flags & ArchetypeFlags.HasBlobAssetRefs) != 0;
        // True when the archetype has a CompanionLink, so companion cleanup can gate on it.
        public bool HasCompanionComponents => (Flags & ArchetypeFlags.HasCompanionComponents) != 0;
        public bool HasWeakAssetRefs => (Flags & ArchetypeFlags.HasWeakAssetRefs) != 0;
        public bool HasUnityObjectRefs => (Flags & ArchetypeFlags.HasUnityObjectRefs) != 0;
        public bool HasSystemInstanceComponents => (Flags & ArchetypeFlags.HasSystemInstanceComponents) != 0;
        public bool HasOnAddedCallbacks => (Flags & ArchetypeFlags.HasOnAddedCallbacks) != 0;
        public bool HasOnRemovedCallbacks => (Flags & ArchetypeFlags.HasOnRemovedCallbacks) != 0;

        public int NumNativeComponentData => FirstBufferComponent - 1;
        public int NumBufferComponents => FirstTagComponent - FirstBufferComponent;
        public int NumTagComponents => FirstSharedComponent - FirstTagComponent;
        public int NumSharedComponents => FirstChunkComponent - FirstSharedComponent;
        public int NumChunkComponents => TypesCount - FirstChunkComponent;
        public int NonZeroSizedTypesCount => FirstTagComponent;

        // These help when iterating specific component types
        // for(int iType=archetype->FirstBufferComponent; iType<archetype->BufferComponentsEnd;++iType) {...}
        public int NativeComponentsEnd => FirstBufferComponent;
        public int BufferComponentsEnd => FirstTagComponent;
        public int TagComponentsEnd => FirstSharedComponent;
        public int SharedComponentsEnd => FirstChunkComponent;
        public int ChunkComponentsEnd => TypesCount;

        public bool HasChunkComponents => FirstChunkComponent != TypesCount;

        public override string ToString()
        {
            var info = "";
            for (var i = 0; i < TypesCount; i++)
            {
                var componentTypeInArchetype = Types[i];
                info += $"  - {componentTypeInArchetype}";
            }

            return info;
        }

        public void AddToChunkList(ChunkIndex chunk, SharedComponentValues sharedComponentIndices, uint changeVersion, ref EntityComponentStore.ChunkListChanges changes)
        {
            chunk.ListIndex = Chunks.Count;
            if (Chunks.Count == Chunks.Capacity)
            {
                var newCapacity = (Chunks.Capacity == 0) ? 1 : (Chunks.Capacity * 2);

                // The shared component indices we are inserting belong to the same archetype so they need to be adjusted after reallocation
                if (Chunks.InsideAllocation((ulong)sharedComponentIndices.firstIndex))
                {
                    int chunkIndex = (int)(sharedComponentIndices.firstIndex - Chunks.GetSharedComponentValueArrayForType(0));
                    Chunks.Grow(newCapacity);
                    sharedComponentIndices = Chunks.GetSharedComponentValues(chunkIndex);
                }
                else
                {
                    Chunks.Grow(newCapacity);
                }
            }

            Chunks.Add(chunk, sharedComponentIndices, changeVersion);

            fixed(Archetype* archetype = &this)
            {
                changes.TrackArchetype(archetype);
            }
        }

        public void RemoveFromChunkList(ChunkIndex chunk, ref EntityComponentStore.ChunkListChanges changes)
        {
            var chunkListIndex = chunk.ListIndex;
            Chunks.RemoveAtSwapBack(chunkListIndex);
            var chunkThatMoved = Chunks[chunkListIndex];
            chunkThatMoved.ListIndex = chunkListIndex;

            fixed(Archetype* archetype = &this)
            {
                changes.TrackArchetype(archetype);
            }
        }

        void AddToChunkListWithEmptySlots(ChunkIndex chunk)
        {
            chunk.ListWithEmptySlotsIndex = ChunksWithEmptySlots.Length;
            ChunksWithEmptySlots.Add(chunk);
        }

        void RemoveFromChunkListWithEmptySlots(ChunkIndex chunk)
        {
            var index = chunk.ListWithEmptySlotsIndex;
            Assert.IsTrue(index >= 0 && index < ChunksWithEmptySlots.Length);
            Assert.IsTrue(ChunksWithEmptySlots.Ptr[index] == chunk);
            ChunksWithEmptySlots.RemoveAtSwapBack(index);

            if (index < ChunksWithEmptySlots.Length)
            {
                var chunkThatMoved = ChunksWithEmptySlots.Ptr[index];
                chunkThatMoved.ListWithEmptySlotsIndex = index;
            }
        }

        /// <summary>
        /// Remove chunk from archetype tracking of chunks with available slots.
        /// - Does not check if chunk has space.
        /// - Does not check if chunk is locked.
        /// </summary>
        /// <param name="chunk"></param>
        internal void EmptySlotTrackingRemoveChunk(ChunkIndex chunk)
        {
            fixed (Archetype* archetype = &this)
            {
                Assert.AreEqual((ulong)archetype, (ulong)EntityComponentStore->GetArchetype(chunk));
            }
            if (NumSharedComponents == 0)
                RemoveFromChunkListWithEmptySlots(chunk);
            else
                FreeChunksBySharedComponents.Remove(chunk);
        }

        /// <summary>
        /// Add chunk to archetype tracking of chunks with available slots.
        /// - Does not check if chunk has space.
        /// - Does not check if chunk is locked.
        /// </summary>
        /// <param name="chunk"></param>
        internal void EmptySlotTrackingAddChunk(ChunkIndex chunk)
        {
            fixed (Archetype* archetype = &this)
            {
                Assert.AreEqual((ulong)archetype, (ulong)EntityComponentStore->GetArchetype(chunk));
            }
            if (NumSharedComponents == 0)
                AddToChunkListWithEmptySlots(chunk);
            else
                FreeChunksBySharedComponents.Add(chunk);
        }

        internal ChunkIndex GetExistingChunkWithEmptySlots(SharedComponentValues sharedComponentValues)
        {
            if (NumSharedComponents == 0)
            {
                if (ChunksWithEmptySlots.Length != 0)
                {
                    var chunk = ChunksWithEmptySlots[0];
                    Assert.AreNotEqual(chunk.Count, ChunkCapacity);
                    return chunk;
                }
            }

            // note: will be ChunkIndex.Null if none available.
            return FreeChunksBySharedComponents.TryGet(sharedComponentValues, NumSharedComponents);
        }

        internal bool CompareMask(EntityQueryMask mask)
        {
            return (byte)(QueryMaskArray[mask.Index] & mask.Mask) == mask.Mask;
        }

        internal void SetMask(EntityQueryMask mask)
        {
            QueryMaskArray[mask.Index] |= mask.Mask;
        }
    }
}
