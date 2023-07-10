// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct ComponentType
{
    public int Size;

    public static ComponentType Create<T>() where T : unmanaged => new ComponentType {Size = UnsafeUtility.SizeOf<T>()};
}

/// <summary>
/// The <see cref="LayoutDataStore"/> is used to store "componentized" data for a set of objects. This storage is extremely simple and does
/// not handle any sort of type index mapping. Each component is identified by it's array index specified at creation time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
unsafe partial struct LayoutDataStore : IDisposable
{
    const int k_ChunkSize = 32 * 1024; // 32 kb

    [StructLayout(LayoutKind.Sequential)]
    struct Chunk
    {
        [NativeDisableUnsafePtrRestriction] public byte* Buffer;
    }

    /// <summary>
    /// The <see cref="ComponentDataStore"/> stores a contiguous array of one specific component where the <see cref="LayoutHandle.Index"/> maps to the data for that node.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct ComponentDataStore : IDisposable
    {
        /// <summary>
        /// The allocator to use for the chunk storage.
        /// </summary>
        public Allocator Allocator;

        /// <summary>
        /// The size of the component.
        /// </summary>
        public int Size;

        /// <summary>
        /// The number of elements per chunk.
        /// </summary>
        public int ComponentCountPerChunk;

        /// <summary>
        /// The number of allocated chunks.
        /// </summary>
        public int ChunkCount;

        /// <summary>
        /// The component data ptr.
        /// </summary>
        [NativeDisableUnsafePtrRestriction] Chunk* m_Chunks;

        public ComponentDataStore(int size, Allocator allocator)
        {
            Allocator = allocator;
            Size = size;
            ComponentCountPerChunk = k_ChunkSize / size;
            ChunkCount = 0;
            m_Chunks = null;
        }

        public void Dispose()
        {
            if (null == m_Chunks)
                return;

            for (var i = 0; i < ChunkCount; i++)
                UnsafeUtility.Free(m_Chunks[i].Buffer, Allocator);

            UnsafeUtility.Free(m_Chunks, Allocator);

            ChunkCount = 0;
            m_Chunks = null;
        }

        public byte* GetComponentDataPtr(int index)
        {
            var chunkIndex = index / ComponentCountPerChunk;
            var indexInChunk = index % ComponentCountPerChunk;

            return m_Chunks[chunkIndex].Buffer + indexInChunk * Size;
        }

        public void EnsureCapacity(int capacity)
        {
            var newChunkCount = capacity / ComponentCountPerChunk + 1;

            if (newChunkCount > ChunkCount)
            {
                // Grow the chunk ptr array.
                m_Chunks = (Chunk*)ResizeArray(m_Chunks, ChunkCount, newChunkCount, UnsafeUtility.SizeOf<Chunk>(), UnsafeUtility.AlignOf<Chunk>(), Allocator);

                // Allocate new chunks.
                for (var i = ChunkCount; i<newChunkCount; i++)
                {
                    m_Chunks[i] = new Chunk
                    {
                        Buffer = (byte*)UnsafeUtility.Malloc(k_ChunkSize, 4, Allocator)
                    };
                }

                ChunkCount = newChunkCount;
            }
        }

        public void ResizeCapacity(int capacity)
        {
            var newChunkCount = capacity / ComponentCountPerChunk + 1;

            if (newChunkCount > ChunkCount)
            {
                // Grow the chunk ptr array.
                m_Chunks = (Chunk*)ResizeArray(m_Chunks, ChunkCount, newChunkCount, UnsafeUtility.SizeOf<Chunk>(), UnsafeUtility.AlignOf<Chunk>(), Allocator);

                // Allocate new chunks.
                for (var i = ChunkCount; i<newChunkCount; i++)
                {
                    m_Chunks[i] = new Chunk
                    {
                        Buffer = (byte*)UnsafeUtility.Malloc(k_ChunkSize, 4, Allocator)
                    };
                }
            }
            else if (newChunkCount < ChunkCount)
            {
                // Free up allocated chunks.
                for (var i = ChunkCount - 1; i >= newChunkCount; i--)
                {
                    UnsafeUtility.Free(m_Chunks[i].Buffer, Allocator);
                }

                // Shrink down the chunk ptr array.
                m_Chunks = (Chunk*)ResizeArray(m_Chunks, ChunkCount, newChunkCount, UnsafeUtility.SizeOf<Chunk>(), UnsafeUtility.AlignOf<Chunk>(), Allocator);
            }

            ChunkCount = newChunkCount;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Data
    {
        public int Capacity;
        public int NextFreeIndex;
        public int ComponentCount;

        [NativeDisableUnsafePtrRestriction] public int* Versions;
        [NativeDisableUnsafePtrRestriction] public ComponentDataStore* Components;
    }

    readonly Allocator m_Allocator;
    [NativeDisableUnsafePtrRestriction] Data* m_Data;

    public bool IsValid => null != m_Data;

    public int Capacity => m_Data->Capacity;

    public LayoutDataStore(ComponentType[] components, int initialCapacity, Allocator allocator)
    {
        Assert.IsTrue(components.Length > 0, $"{nameof(LayoutDataStore)} requires at least one component size.");
        Assert.IsTrue(components[0].Size >= sizeof(int), $"{nameof(LayoutDataStore)} requires a minimum element size of {sizeof(int)} to alias");

        m_Allocator = allocator;
        m_Data = (Data*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Data>(), UnsafeUtility.AlignOf<Data>(), m_Allocator);
        UnsafeUtility.MemClear(m_Data, UnsafeUtility.SizeOf<Data>());

        m_Data->ComponentCount = components.Length;
        m_Data->Components = (ComponentDataStore*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ComponentDataStore>() * components.Length, UnsafeUtility.AlignOf<ComponentDataStore>(), allocator);

        for (var i = 0; i < components.Length; i++)
        {
            m_Data->Components[i] = new ComponentDataStore(components[i].Size, allocator);
        }

        ResizeCapacity(initialCapacity);

        m_Data->NextFreeIndex = 0;
    }

    public void Dispose()
    {
        for (var i = 0; i < m_Data->ComponentCount; i++)
            m_Data->Components[i].Dispose();

        UnsafeUtility.Free(m_Data->Versions, m_Allocator);
        UnsafeUtility.Free(m_Data->Components, m_Allocator);
        UnsafeUtility.Free(m_Data, m_Allocator);

        m_Data = null;
    }

    public bool Exists(in LayoutHandle handle)
    {
        if ((uint)handle.Index >= m_Data->Capacity)
            return false;

        return m_Data->Versions[handle.Index] == handle.Version;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void* GetComponentDataPtr(int index, int componentIndex)
    {
        return m_Data->Components[componentIndex].GetComponentDataPtr(index);
    }

    public LayoutHandle Allocate()
    {
        return Allocate(null, 0);
    }

    LayoutHandle Allocate(byte** data, int count)
    {
        // Fetch the next available index. This is the element we are about to initialize.
        var index = m_Data->NextFreeIndex;

        // Fetch the next element in the chain before we overwrite the node data.
        var nextIndex = GetNextFreeIndex(m_Data->Components, index);

        if (nextIndex == -1)
        {
            // This element is the last in the chain. Initiate a resize.
            IncreaseCapacity();

            // At this point we now have our next index ready. Retrieve it before we overwrite the node data.
            nextIndex = GetNextFreeIndex(m_Data->Components, index);
        }

        var version = m_Data->Versions[index];

        m_Data->NextFreeIndex = nextIndex;

        for (var i = 0; i < count; i++)
        {
            var ptr = m_Data->Components[i].GetComponentDataPtr(index);
            UnsafeUtility.MemCpy(ptr, data[i], m_Data->Components[i].Size);
        }

        return new LayoutHandle(index, version);
    }

    public void Free(in LayoutHandle handle)
    {
        if (!Exists(handle))
            throw new InvalidOperationException($"Failed to Free handle with Index={handle.Index} Version={handle.Version}");

        m_Data->Versions[handle.Index]++;
        SetNextFreeIndex(m_Data->Components, handle.Index, m_Data->NextFreeIndex);
        m_Data->NextFreeIndex = handle.Index;
    }

    static void SetNextFreeIndex(ComponentDataStore* ptr, int index, int value)
    {
        *(int*)ptr->GetComponentDataPtr(index) = value;
    }

    static int GetNextFreeIndex(ComponentDataStore* ptr, int index)
    {
        return *(int*)ptr->GetComponentDataPtr(index);
    }

    void IncreaseCapacity()
    {
        ResizeCapacity((int) (m_Data->Capacity * 1.5f));
    }

    void ResizeCapacity(int capacity)
    {
        Assert.IsTrue(capacity > 0);

        m_Data->Versions = (int*)ResizeArray(m_Data->Versions, m_Data->Capacity, capacity, sizeof(int), 4, m_Allocator);

        for (var i=0; i<m_Data->ComponentCount; i++)
            m_Data->Components[i].ResizeCapacity(capacity);

        // Start one element back to maintain the linked list.
        var start = m_Data->Capacity > 0 ? m_Data->Capacity - 1 : 0;

        for (var i = start; i < capacity; i++)
        {
            m_Data->Versions[i] = 1;

            // Create a linked list of free elements using the first 4 bytes of the data structure.
            SetNextFreeIndex(m_Data->Components, i, i + 1);
        }

        // The last element receives a special index indicating we are at the end of the array and should resize.
        SetNextFreeIndex(m_Data->Components, capacity - 1, -1);
        m_Data->Capacity = capacity;
    }

    static void* ResizeArray(void* fromPtr, long fromCount, long toCount, long size, int align, Allocator allocator)
    {
        Assert.IsTrue(toCount > 0);

        var toPtr = UnsafeUtility.Malloc(size * toCount, align, allocator);
        Assert.IsTrue(toPtr != null);

        if (fromCount <= 0)
            return toPtr;

        var countToCopy = toCount < fromCount ? toCount : fromCount;
        var bytesToCopy = countToCopy * size;

        UnsafeUtility.MemCpy(toPtr, fromPtr, bytesToCopy);
        UnsafeUtility.Free(fromPtr, allocator);

        return toPtr;
    }

    public LayoutHandle Allocate<T0>(in T0 component0) where T0 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        {
            var data = stackalloc byte*[1];

            data[0] = (byte*)ptr0;

            return Allocate(data, 1);
        }
    }

    public LayoutHandle Allocate<T0, T1, T2>(in T0 component0, in T1 component1, in T2 component2)
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        fixed (T1* ptr1 = &component1)
        fixed (T2* ptr2 = &component2)
        {
            var data = stackalloc byte*[3];

            data[0] = (byte*)ptr0;
            data[1] = (byte*)ptr1;
            data[2] = (byte*)ptr2;

            return Allocate(data, 3);
        }
    }

    public LayoutHandle Allocate<T0, T1, T2, T3>(in T0 component0, in T1 component1, in T2 component2, in T3 component3)
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        fixed (T1* ptr1 = &component1)
        fixed (T2* ptr2 = &component2)
        fixed (T3* ptr3 = &component3)
        {
            var data = stackalloc byte*[4];

            data[0] = (byte*)ptr0;
            data[1] = (byte*)ptr1;
            data[2] = (byte*)ptr2;
            data[3] = (byte*)ptr3;

            return Allocate(data, 4);
        }
    }

    public LayoutHandle Allocate<T0, T1, T2, T3, T4>(in T0 component0, in T1 component1, in T2 component2, in T3 component3, in T4 component4)
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        fixed (T1* ptr1 = &component1)
        fixed (T2* ptr2 = &component2)
        fixed (T3* ptr3 = &component3)
        fixed (T4* ptr4 = &component4)
        {
            var data = stackalloc byte*[5];

            data[0] = (byte*)ptr0;
            data[1] = (byte*)ptr1;
            data[2] = (byte*)ptr2;
            data[3] = (byte*)ptr3;
            data[4] = (byte*)ptr4;

            return Allocate(data, 5);
        }
    }

    public LayoutHandle Allocate<T0, T1, T2, T3, T4, T5>(in T0 component0, in T1 component1, in T2 component2, in T3 component3, in T4 component4, in T5 component5)
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        fixed (T1* ptr1 = &component1)
        fixed (T2* ptr2 = &component2)
        fixed (T3* ptr3 = &component3)
        fixed (T4* ptr4 = &component4)
        fixed (T5* ptr5 = &component5)
        {
            var data = stackalloc byte*[6];

            data[0] = (byte*)ptr0;
            data[1] = (byte*)ptr1;
            data[2] = (byte*)ptr2;
            data[3] = (byte*)ptr3;
            data[4] = (byte*)ptr4;
            data[5] = (byte*)ptr5;

            return Allocate(data, 6);
        }
    }

    public LayoutHandle Allocate<T0, T1, T2, T3, T4, T5, T6>(in T0 component0, in T1 component1, in T2 component2, in T3 component3, in T4 component4, in T5 component5, in T6 component6)
        where T0 : unmanaged
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where T4 : unmanaged
        where T5 : unmanaged
        where T6 : unmanaged
    {
        fixed (T0* ptr0 = &component0)
        fixed (T1* ptr1 = &component1)
        fixed (T2* ptr2 = &component2)
        fixed (T3* ptr3 = &component3)
        fixed (T4* ptr4 = &component4)
        fixed (T5* ptr5 = &component5)
        fixed (T6* ptr6 = &component6)
        {
            var data = stackalloc byte*[7];

            data[0] = (byte*)ptr0;
            data[1] = (byte*)ptr1;
            data[2] = (byte*)ptr2;
            data[3] = (byte*)ptr3;
            data[4] = (byte*)ptr4;
            data[5] = (byte*)ptr5;
            data[6] = (byte*)ptr6;

            return Allocate(data, 7);
        }
    }
}
