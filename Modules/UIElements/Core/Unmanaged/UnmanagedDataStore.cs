// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Unmanaged;

[StructLayout(LayoutKind.Sequential)]
struct UnmanagedComponentType
{
    public int Size;
    public int Align;

    public static UnmanagedComponentType Create<T>() where T : unmanaged
    {
        return new UnmanagedComponentType
        {
            Size = UnsafeUtility.SizeOf<T>(),
            Align = UnsafeUtility.AlignOf<T>()
        };
    }
}

/// <summary>
/// The <see cref="UnmanagedDataStore"/> is used to store "componentized" data for a set of objects. This storage is extremely simple and does
/// not handle any sort of type index mapping. Each component is identified by it's array index specified at creation time.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
unsafe partial struct UnmanagedDataStore : IDisposable
{
    const int k_ChunkSize = 32 * 1024; // 32 kb

    [StructLayout(LayoutKind.Sequential)]
    struct Chunk
    {
        [NativeDisableUnsafePtrRestriction] public byte* Buffer;
    }

    /// <summary>
    /// The <see cref="ComponentDataStore"/> stores a contiguous array of one specific component where the <see cref="UnmanagedDataHandle.Index"/> maps to the data for that node.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentDataStore : IDisposable
    {
        /// <summary>
        /// The label to use for the chunk storage.
        /// </summary>
        public readonly MemoryLabel MemoryLabel;

        /// <summary>
        /// The size of the component.
        /// </summary>
        public int Size;

        /// <summary>
        /// The alignment of the component.
        /// </summary>
        public int Align;

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

        /// <summary>
        /// The component data ptr.
        /// </summary>
        [NativeDisableUnsafePtrRestriction] public byte* InitialData;

        public ComponentDataStore(int size, int align, MemoryLabel allocLabel, byte* initialData)
        {
            Size = size;
            Align = align;
            ComponentCountPerChunk = k_ChunkSize / size;
            ChunkCount = 0;
            MemoryLabel = allocLabel;
            m_Chunks = null;
            InitialData = (byte*)UnsafeUtility.Malloc(size, align, MemoryLabel);
            UnsafeUtility.MemCpy(InitialData, initialData, size);
        }

        public void Dispose()
        {
            if (m_Chunks != null)
            {
                for (var i = 0; i < ChunkCount; i++)
                    UnsafeUtility.Free(m_Chunks[i].Buffer, MemoryLabel);

                UnsafeUtility.Free(m_Chunks, MemoryLabel);
                ChunkCount = 0;
                m_Chunks = null;
            }

            UnsafeUtility.Free(InitialData, MemoryLabel);
            InitialData = null;
        }

        public byte* GetComponentDataPtr(int index)
        {
            var chunkIndex = index / ComponentCountPerChunk;
            var indexInChunk = index % ComponentCountPerChunk;

            return m_Chunks[chunkIndex].Buffer + indexInChunk * Size;
        }

        public void ResizeCapacity(int capacity)
        {
            var newChunkCount = capacity / ComponentCountPerChunk + 1;

            if (newChunkCount > ChunkCount)
            {
                // Grow the chunk ptr array.
                m_Chunks = (Chunk*)ResizeArray(m_Chunks, ChunkCount, newChunkCount, UnsafeUtility.SizeOf<Chunk>(), UnsafeUtility.AlignOf<Chunk>(), MemoryLabel);

                // Allocate new chunks.
                for (var i = ChunkCount; i < newChunkCount; i++)
                {
                    m_Chunks[i] = new Chunk
                    {
                        Buffer = (byte*)UnsafeUtility.Malloc(k_ChunkSize, Align, MemoryLabel)
                    };

                    UnsafeUtility.MemCpyReplicate(m_Chunks[i].Buffer, InitialData, Size, ComponentCountPerChunk);
                }
            }
            else if (newChunkCount < ChunkCount)
            {
                // Free up allocated chunks.
                for (var i = ChunkCount - 1; i >= newChunkCount; i--)
                {
                    UnsafeUtility.Free(m_Chunks[i].Buffer, MemoryLabel);
                }

                // Shrink down the chunk ptr array.
                m_Chunks = (Chunk*)ResizeArray(m_Chunks, ChunkCount, newChunkCount, UnsafeUtility.SizeOf<Chunk>(), UnsafeUtility.AlignOf<Chunk>(), MemoryLabel);
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
        [NativeDisableUnsafePtrRestriction] public int* FreeIndices;
        [NativeDisableUnsafePtrRestriction] public ComponentDataStore* Components;
    }

    readonly MemoryLabel m_MemoryLabel;
    [NativeDisableUnsafePtrRestriction] Data* m_Data;

    public bool IsValid => null != m_Data;

    public int Capacity => m_Data->Capacity;

    public UnmanagedDataStore(UnmanagedComponentType[] components, ReadOnlySpan<MemoryLabel> labels, byte** initialData, int initialCapacity, Allocator allocator)
    {
        Assert.IsTrue(components.Length > 0, $"{nameof(UnmanagedDataStore)} requires at least one component size.");
        Assert.IsTrue(components[0].Size >= sizeof(int), $"{nameof(UnmanagedDataStore)} requires a minimum element size of {sizeof(int)} to alias");
        Assert.AreEqual(components.Length, labels.Length, "Expected a matching number of component names and components.");
        m_MemoryLabel = new (nameof(UIElements), $"Layout.{nameof(UnmanagedDataStore)}", allocator);
        m_Data = (Data*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Data>(), UnsafeUtility.AlignOf<Data>(), m_MemoryLabel);
        UnsafeUtility.MemClear(m_Data, UnsafeUtility.SizeOf<Data>());

        m_Data->ComponentCount = components.Length;
        m_Data->Components = (ComponentDataStore*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<ComponentDataStore>() * components.Length, UnsafeUtility.AlignOf<ComponentDataStore>(), m_MemoryLabel);

        for (var i = 0; i < components.Length; i++)
        {
            Debug.Assert(initialData[i] != null);
            m_Data->Components[i] = new ComponentDataStore(components[i].Size, components[i].Align, labels[i], initialData[i]);
        }

        ResizeCapacity(initialCapacity);

        m_Data->NextFreeIndex = 0;
    }

    public void Dispose()
    {
        for (var i = 0; i < m_Data->ComponentCount; i++)
            m_Data->Components[i].Dispose();

        UnsafeUtility.Free(m_Data->Versions, m_MemoryLabel);
        UnsafeUtility.Free(m_Data->FreeIndices, m_MemoryLabel);
        UnsafeUtility.Free(m_Data->Components, m_MemoryLabel);
        UnsafeUtility.Free(m_Data, m_MemoryLabel);

        m_Data = null;
    }

    public bool Exists(in UnmanagedDataHandle handle)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsFree(int index)
    {
        // Special value nextIndex == index indicates the node is not free.
        return GetNextFreeIndex(index) != index;
    }

    public UnmanagedDataHandle Allocate()
    {
        // Fetch the next available index. This is the element we are about to initialize.
        var index = m_Data->NextFreeIndex;

        // Fetch the next element in the chain before we overwrite the node data.
        var nextIndex = GetNextFreeIndex(index);

        if (nextIndex == -1)
        {
            // This element is the last in the chain. Initiate a resize.
            IncreaseCapacity();

            // At this point we now have our next index ready. Retrieve it before we overwrite the node data.
            nextIndex = GetNextFreeIndex(index);
        }

        // Set a special nextIndex == index case to indicate the node is not free.
        SetNextFreeIndex(index, index);

        var version = m_Data->Versions[index];

        m_Data->NextFreeIndex = nextIndex;

        return new UnmanagedDataHandle(index, version);
    }

    public void Free(in UnmanagedDataHandle handle)
    {
        if (!Exists(handle))
            throw new InvalidOperationException($"Failed to Free handle with Index={handle.Index} Version={handle.Version}");

        var index = handle.Index;
        m_Data->Versions[index]++;
        SetNextFreeIndex(index, m_Data->NextFreeIndex);
        m_Data->NextFreeIndex = index;

        for (var i = 0; i < m_Data->ComponentCount; i++)
        {
            var ptr = m_Data->Components[i].GetComponentDataPtr(index);
            UnsafeUtility.MemCpy(ptr, m_Data->Components[i].InitialData, m_Data->Components[i].Size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void SetNextFreeIndex(int index, int value)
    {
        m_Data->FreeIndices[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    int GetNextFreeIndex(int index)
    {
        return m_Data->FreeIndices[index];
    }

    void IncreaseCapacity()
    {
        // A word on the growth rate used here.
        // A rate of 1.5f could lead to more frequent resizing of the m_Version array.
        // But this is not so much of an issue for the chunk storage which:
        // 1. "rounds up" the capacity to determine the number of chunks (i.e. capacity / ChunkSize)
        // 2. only resizes its internal list of chunks and nothing else
        // So we prioritize not growing the chunk-base storage too fast,
        // especially considering it never shrinks automatically at the moment.
        const float growRate = 1.5f;
        ResizeCapacity((int)(m_Data->Capacity * growRate));
    }

    void ResizeCapacity(int capacity)
    {
        Assert.IsTrue(capacity > 0);

        m_Data->Versions = (int*)ResizeArray(m_Data->Versions, m_Data->Capacity, capacity, sizeof(int), 4, m_MemoryLabel);
        m_Data->FreeIndices = (int*)ResizeArray(m_Data->FreeIndices, m_Data->Capacity, capacity, sizeof(int), 4, m_MemoryLabel);

        for (var i=0; i<m_Data->ComponentCount; i++)
            m_Data->Components[i].ResizeCapacity(capacity);

        // Start one element back to maintain the linked list.
        var start = m_Data->Capacity > 0 ? m_Data->Capacity - 1 : 0;

        for (var i = start; i < capacity; i++)
        {
            m_Data->Versions[i] = 1;

            // Create a linked list of free elements.
            SetNextFreeIndex(i, i + 1);
        }

        // The last element receives a special index indicating we are at the end of the array and should resize.
        SetNextFreeIndex(capacity - 1, -1);
        m_Data->Capacity = capacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void* ResizeArray(void* fromPtr, long fromCount, long toCount, long size, int align, MemoryLabel label)
    {
        Assert.IsTrue(toCount > 0);

        var toPtr = UnsafeUtility.Realloc(fromPtr, size * toCount, align, label);
        Assert.IsTrue(toPtr != null);

        return toPtr;
    }
}
