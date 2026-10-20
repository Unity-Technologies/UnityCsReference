// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    // Stores change version numbers, shared component indices, and entity count for all chunks belonging to an archetype in SOA layout
    [DebuggerTypeProxy(typeof(ArchetypeChunkDataDebugView))]
    internal unsafe struct ArchetypeChunkData
    {
        void* m_Data;
        public int Capacity { get; private set; } // maximum number of chunks that can be tracked before Grow() must be called
        public int Count { get; private set; } // number of chunks currently tracked [0..Capacity]

        readonly int SharedComponentCount;
        readonly int ComponentCount; // including shared components

        // ChangeVersions and SharedComponentValues stored like:
        //    type0: chunk0 chunk1 chunk2 ...
        //    type1: chunk0 chunk1 chunk2 ...
        //    type2: chunk0 chunk1 chunk2 ...
        //    ...

        ulong ChunkIndicesSize => (ulong)(sizeof(ChunkIndex) * Capacity);
        ulong ChangeVersionSize  => (ulong)(sizeof(uint) * ComponentCount * Capacity);
        ulong EntityCountSize => (ulong)(sizeof(int) * Capacity);
        ulong SharedComponentValuesSize => (ulong)(sizeof(int) * SharedComponentCount * Capacity);
        const ulong ComponentEnabledBitsSizePerComponentInChunk = (2 * sizeof(ulong)); // size of bits for ONE component in a chunk
        ulong PaddingForEnabledBitAlignmentSize => 16 - ((ChunkIndicesSize + ChangeVersionSize + EntityCountSize + SharedComponentValuesSize) % 16); // enabled bits must be 16-byte aligned
        public ulong ComponentEnabledBitsSizeTotalPerChunk => ComponentEnabledBitsSizePerComponentInChunk * (ulong)ComponentCount; // size of bits for ALL components in a chunk
        ulong ComponentEnabledBitsSize => ComponentEnabledBitsSizeTotalPerChunk * (ulong)Capacity; // size of bits for ALL components of ALL chunks
        public ulong ComponentEnabledBitsHierarchicalDataSizePerChunk => (ulong)(sizeof(int) * ComponentCount); // size of enabled bits hierarchical data for ONE chunk
        ulong ComponentEnabledBitsHierarchicalDataSize => (ComponentEnabledBitsHierarchicalDataSizePerChunk * (ulong) Capacity); // size of enabled bits hierarchical data for ALL chunks
        ulong BufferSize => ChunkIndicesSize + ChangeVersionSize + EntityCountSize + SharedComponentValuesSize + PaddingForEnabledBitAlignmentSize + ComponentEnabledBitsSize + ComponentEnabledBitsHierarchicalDataSize;

        ChunkIndex* ChunkIndices => (ChunkIndex*)m_Data;

        // ChangeVersions[ComponentCount * Capacity]
        //   - Order version is ChangeVersion[0] which is ChangeVersion[Entity]
        uint* ChangeVersions => (uint*)(((ulong)m_Data) + ChunkIndicesSize);

        // EntityCount[Capacity]
        int* EntityCount => (int*)(((ulong)ChangeVersions) + ChangeVersionSize);

        // SharedComponentValues[SharedComponentCount * Capacity]
        int* SharedComponentValues => (int*)(((ulong)EntityCount) + EntityCountSize);

        // ComponentEnabledBits[(RoundUp(ChunkEntityCapacity, 128)/128 * ComponentCount * Capacity]
        //    chunk0: type0 type1 type2 ...
        //    chunk1: type0 type1 type2 ...
        //    chunk1: type0 type1 type2 ...
        // Each type's bits are rounded up to a multiple of 128 bits, so that we can create an UnsafeBitArray of a single
        // type within a single chunk.
        // Types are stored in the same order in memory as component data (which is stable across runs),
        // *not* the type index within the archetype (which is not). The data for archetype->Types[N] within
        // each chunk is at mask index archetype->TypeIndexInArchetypeToMemoryOrderIndex[N]
        v128* ComponentEnabledBits => (v128*)(((ulong)SharedComponentValues) + SharedComponentValuesSize + PaddingForEnabledBitAlignmentSize);

        // Starts with single int32 for # of disabled components in entire archetype
        // ComponentEnabledBitsHierarchicalData[ComponentCount * Capacity]
        //    chunk0: type0 type1 type2 ...
        //    chunk1: type0 type1 type2 ...
        //    chunk1: type0 type1 type2 ...
        // Types are stored in the same order in memory as component data (which is stable across runs),
        // *not* the type index within the archetype (which is not). The data for archetype->Types[N] within
        // each chunk is at mask index archetype->TypeIndexInArchetypeToMemoryOrderIndex[N]
        int* ComponentEnabledBitsHierarchicalData => (int*)(((ulong)ComponentEnabledBits) + ComponentEnabledBitsSize);

        public ArchetypeChunkData(int componentCount, int sharedComponentCount)
        {
            m_Data = null;
            Capacity = 0;
            Count = 0;
            SharedComponentCount = sharedComponentCount;
            ComponentCount = componentCount;
        }

        public ChunkIndex this[int index] => ChunkIndices[index];

        public void Grow(int nextCapacity)
        {
            Assert.IsTrue(nextCapacity > Capacity);

            ulong nextChunkIndicesSize = (ulong)(sizeof(ChunkIndex) * nextCapacity);
            ulong nextChangeVersionSize  = (ulong)(sizeof(uint) * ComponentCount * nextCapacity);
            ulong nextEntityCountSize = (ulong)(sizeof(int) * nextCapacity);
            ulong nextSharedComponentValuesSize = (ulong)(sizeof(int) * SharedComponentCount * nextCapacity);
            ulong paddingForEnabledBitAlignmentSize = 16 - ((nextChunkIndicesSize + nextChangeVersionSize + nextEntityCountSize + nextSharedComponentValuesSize) % 16); // enabled bits must be 16-byte aligned
            ulong nextComponentEnabledBitsSize = ComponentEnabledBitsSizeTotalPerChunk * (ulong)nextCapacity;
            ulong nextComponentEnabledBitsHierarchicalDataSize = ComponentEnabledBitsHierarchicalDataSizePerChunk * (ulong) nextCapacity;
            ulong nextBufferSize = nextChunkIndicesSize + nextChangeVersionSize + nextEntityCountSize + nextSharedComponentValuesSize + paddingForEnabledBitAlignmentSize + nextComponentEnabledBitsSize + nextComponentEnabledBitsHierarchicalDataSize;
            ulong nextBufferPtr = (ulong)Memory.Unmanaged.Allocate((long)nextBufferSize, 16, Allocator.Persistent);

            var nextChunkData = (void*)nextBufferPtr;
            nextBufferPtr += nextChunkIndicesSize;
            uint* nextChangeVersions = (uint*)nextBufferPtr;
            nextBufferPtr += nextChangeVersionSize;
            int* nextEntityCount = (int*)nextBufferPtr;
            nextBufferPtr += nextEntityCountSize;
            int* nextSharedComponentValues = (int*)nextBufferPtr;
            nextBufferPtr += nextSharedComponentValuesSize;
            nextBufferPtr += paddingForEnabledBitAlignmentSize;
            Assert.AreEqual(0ul, nextBufferPtr & 0xF);
            byte* nextComponentEnabledBitsValues = (byte*)nextBufferPtr;
            nextBufferPtr += nextComponentEnabledBitsSize;
            int* nextComponentEnabledBitsHierarchicalDataValues = (int*)nextBufferPtr;
            // nextBufferPtr += nextComponentEnabledBitsHierarchicalDataSize;

            int prevCount = Count;
            int prevCapacity = Capacity;
            var prevChunkData = m_Data;
            uint* prevChangeVersions = ChangeVersions;
            int* prevEntityCount = EntityCount;
            int* prevSharedComponentValues = SharedComponentValues;
            v128* prevComponentEnabledBitsValues = ComponentEnabledBits;
            int* prevComponentEnabledBitsHierarchicalDataValues = ComponentEnabledBitsHierarchicalData;

            UnsafeUtility.MemCpy(nextChunkData, prevChunkData, sizeof(ChunkIndex) * prevCount);

            for (int i = 0; i < ComponentCount; i++)
                UnsafeUtility.MemCpy(nextChangeVersions + (i * nextCapacity), prevChangeVersions + (i * prevCapacity), sizeof(uint) * Count);

            for (int i = 0; i < SharedComponentCount; i++)
                UnsafeUtility.MemCpy(nextSharedComponentValues + (i * nextCapacity), prevSharedComponentValues + (i * prevCapacity), sizeof(uint) * Count);

            UnsafeUtility.MemCpy(nextEntityCount, prevEntityCount, sizeof(int) * Count);

            UnsafeUtility.MemCpy(nextComponentEnabledBitsValues, prevComponentEnabledBitsValues, (long)ComponentEnabledBitsSize);

            UnsafeUtility.MemCpy(nextComponentEnabledBitsHierarchicalDataValues, prevComponentEnabledBitsHierarchicalDataValues, (long)ComponentEnabledBitsHierarchicalDataSize);

            Memory.Unmanaged.Free(m_Data, Allocator.Persistent);

            m_Data = nextChunkData;
            Capacity = nextCapacity;
        }

        public bool InsideAllocation(ulong addr)
        {
            ulong startAddr = (ulong)m_Data;
            return (addr >= startAddr) && (addr <= (startAddr + BufferSize));
        }

        public int* GetSharedComponentValueArrayForType(int sharedComponentIndexInArchetype)
        {
            return SharedComponentValues + (sharedComponentIndexInArchetype * Capacity);
        }

        public int GetSharedComponentValue(int sharedComponentIndexInArchetype, int chunkIndex)
        {
            var sharedValues = GetSharedComponentValueArrayForType(sharedComponentIndexInArchetype);
            return sharedValues[chunkIndex];
        }

        public void SetSharedComponentValue(int sharedComponentIndexInArchetype, int chunkIndex, int value)
        {
            var sharedValues = GetSharedComponentValueArrayForType(sharedComponentIndexInArchetype);
            sharedValues[chunkIndex] = value;
        }

        public SharedComponentValues GetSharedComponentValues(int chunkIndex)
        {
            return new SharedComponentValues
            {
                firstIndex = SharedComponentValues + chunkIndex,
                stride = Capacity * sizeof(int)
            };
        }

        // Returns the bits for all types within a single chunk. Each type's bits are a v128.
        public v128* GetComponentEnabledMaskArrayForChunk(int chunkIndex)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            var bits = ComponentEnabledBits + (ComponentCount * chunkIndex);
            return (v128*)bits;
        }
        public int GetComponentEnabledBitsSizePerChunk()
        {
            return (int)ComponentEnabledBitsSizeTotalPerChunk;
        }

        // Returns the bits for a single type in a single chunk. Array count will be padded to a multiple of 64 entities.
        public UnsafeBitArray GetEnabledArrayForTypeInChunk(int typeMemoryOrderIndexInArchetype, int chunkIndex)
        {
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            var bits = ComponentEnabledBits + (ComponentCount * chunkIndex) + typeMemoryOrderIndexInArchetype;
            return new UnsafeBitArray(bits, (int)ComponentEnabledBitsSizePerComponentInChunk);
        }
        public v128* GetComponentEnabledMaskArrayForTypeInChunk(int typeMemoryOrderIndexInArchetype, int chunkIndex)
        {
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            return ComponentEnabledBits + (ComponentCount * chunkIndex) + typeMemoryOrderIndexInArchetype;
        }

        public void InitializeDisabledCountForChunk(int chunkIndex)
        {
            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            UnsafeUtility.MemClear(typeArrayForChunk, (long)ComponentEnabledBitsHierarchicalDataSizePerChunk);
        }

        public void SetEnabledBitsAndHierarchicalData(int chunkIndex, byte* componentEnabledBitValues, int* perComponentDisabledBitCount)
        {
            var bits = ComponentEnabledBits + (ComponentCount * chunkIndex);
            UnsafeUtility.MemCpy(bits, componentEnabledBitValues, (long)ComponentEnabledBitsSizeTotalPerChunk);

            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            UnsafeUtility.MemCpy(typeArrayForChunk, perComponentDisabledBitCount, (long)ComponentEnabledBitsHierarchicalDataSizePerChunk);
        }

        public int* GetChunkDisabledCounts(int chunkIndex)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            return ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
        }

        public int GetChunkDisabledCountForType(int typeMemoryOrderIndexInArchetype, int chunkIndex)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            return typeArrayForChunk[typeMemoryOrderIndexInArchetype];
        }

        public byte* GetPointerToComponentEnabledArrayForArchetype()
        {
            return (byte*)ComponentEnabledBits;
        }

        public int* GetPointerToChunkDisabledCountForArchetype()
        {
            return ComponentEnabledBitsHierarchicalData;
        }

        public int* GetPointerToChunkDisabledCountForType(int typeMemoryOrderIndexInArchetype, int chunkIndex)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            return typeArrayForChunk + typeMemoryOrderIndexInArchetype;
        }

        public void SetChunkDisabledCountForType(int typeMemoryOrderIndexInArchetype, int chunkIndex, int value)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            typeArrayForChunk[typeMemoryOrderIndexInArchetype] = value;
        }

        public void AdjustChunkDisabledCountForType(int typeMemoryOrderIndexInArchetype, int chunkIndex, int value)
        {
            Assert.IsTrue(chunkIndex >= 0 && chunkIndex < Capacity);
            Assert.IsTrue(typeMemoryOrderIndexInArchetype >= 0 && typeMemoryOrderIndexInArchetype < ComponentCount);

            var typeArrayForChunk = ComponentEnabledBitsHierarchicalData + chunkIndex * ComponentCount;
            typeArrayForChunk[typeMemoryOrderIndexInArchetype] += value;
        }

        public uint* GetChangeVersionArrayForType(int indexInArchetype)
        {
            Assert.IsTrue(indexInArchetype >= 0 && indexInArchetype < ComponentCount,
                "out-of-range indexInArchetype passed to GetChangeVersionArrayForType");
            return ChangeVersions + (indexInArchetype * Capacity);
        }

        public uint GetChangeVersion(int indexInArchetype, int chunkIndex)
        {
            var changeVersions = GetChangeVersionArrayForType(indexInArchetype);
            return changeVersions[chunkIndex];
        }

        public uint GetOrderVersion(int chunkIndex)
        {
            return GetChangeVersion(0, chunkIndex);
        }

        public void SetChangeVersion(int indexInArchetype, int chunkIndex, uint version)
        {
            var changeVersions = GetChangeVersionArrayForType(indexInArchetype);
            changeVersions[chunkIndex] = version;
        }

        public void SetAllChangeVersion(int chunkIndex, uint version)
        {
            for (int i = 1; i < ComponentCount; ++i)
                ChangeVersions[(i * Capacity) + chunkIndex] = version;
        }

        public void SetOrderVersion(int chunkIndex, uint changeVersion)
        {
            SetChangeVersion(0, chunkIndex, changeVersion);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int* GetChunkEntityCountArray()
        {
            return EntityCount;
        }

        public int GetChunkEntityCount(int chunkIndex)
        {
            return EntityCount[chunkIndex];
        }

        public void SetChunkEntityCount(int chunkIndex, int count)
        {
            // Note: this is generally not safe to call in isolation, as it may leave the chunk's enabled bits in an invalid state.
            EntityCount[chunkIndex] = count;
        }

        public void Add(ChunkIndex chunk, SharedComponentValues sharedComponentIndices, uint changeVersion)
        {
            var chunkIndex = Count++;

            ChunkIndices[chunkIndex] = chunk;

            for (int i = 0; i < SharedComponentCount; i++)
                SharedComponentValues[(i * Capacity) + chunkIndex] = sharedComponentIndices[i];

            // New chunk, so all versions are reset.
            for (int i = 0; i < ComponentCount; i++)
                ChangeVersions[(i * Capacity) + chunkIndex] = changeVersion;

            EntityCount[chunkIndex] = chunk.Count;
        }

        public void MoveChunks(in ArchetypeChunkData srcChunks)
        {
            if (Capacity < Count + srcChunks.Count)
                Grow(Count + srcChunks.Count);

            UnsafeUtility.MemCpy(ChunkIndices + Count, srcChunks.ChunkIndices, sizeof(ChunkIndex) * srcChunks.Count);

            Count += srcChunks.Count;
        }

        public void AddToCachedChunkList(ref UnsafeCachedChunkList chunkList, int matchingArchetypeIndex, int startIndex = 0)
        {
            chunkList.Append(ChunkIndices + startIndex, Count - startIndex, matchingArchetypeIndex);
        }

        public void RemoveAtSwapBack(int chunkIndex)
        {
            Count--;

            if (chunkIndex == Count)
                return;

            ChunkIndices[chunkIndex] = ChunkIndices[Count];

            for (int i = 0; i < SharedComponentCount; i++)
                SharedComponentValues[(i * Capacity) + chunkIndex] = SharedComponentValues[(i * Capacity) + Count];

            // On *chunk order* change, no versions changed, just moved to new location.
            for (int i = 0; i < ComponentCount; i++)
                ChangeVersions[(i * Capacity) + chunkIndex] = ChangeVersions[(i * Capacity) + Count];

            // Move the last chunk's enabled bits data to the new location, and clear the old data
            v128* srcEnabledBits = GetComponentEnabledMaskArrayForChunk(Count);
            v128* dstEnabledBits = GetComponentEnabledMaskArrayForChunk(chunkIndex);
            UnsafeUtility.MemCpy(dstEnabledBits, srcEnabledBits, (long)ComponentEnabledBitsSizeTotalPerChunk);
            int* srcDisabledCounts = GetChunkDisabledCounts(Count);
            int* dstDisabledCounts = GetChunkDisabledCounts(chunkIndex);
            UnsafeUtility.MemCpy(dstDisabledCounts, srcDisabledCounts, (long)ComponentEnabledBitsHierarchicalDataSizePerChunk);

            EntityCount[chunkIndex] = EntityCount[Count];
        }

        public void Dispose()
        {
            Memory.Unmanaged.Free(m_Data, Allocator.Persistent);
            m_Data = null;
            Capacity = 0;
            Count = 0;
        }
    }
}
