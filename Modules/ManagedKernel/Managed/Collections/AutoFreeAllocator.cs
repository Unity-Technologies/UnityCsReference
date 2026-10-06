// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    unsafe internal struct ArrayOfArrays<T> : IDisposable where  T : unmanaged
    {
        AllocatorManager.AllocatorHandle m_backingAllocatorHandle;
        int m_lengthInElements;
        int m_capacityInElements;
        int m_log2BlockSizeInElements;
        int m_blocks;
        IntPtr* m_block;

        int BlockSizeInElements => 1 << m_log2BlockSizeInElements;
        int BlockSizeInBytes => BlockSizeInElements * sizeof(T);
        int BlockMask => BlockSizeInElements - 1;

        public int Length => m_lengthInElements;
        public int Capacity => m_capacityInElements;

        public ArrayOfArrays(int capacityInElements, AllocatorManager.AllocatorHandle backingAllocatorHandle, int log2BlockSizeInElements = 12)
        {
            this = default;
            m_backingAllocatorHandle = backingAllocatorHandle;
            m_lengthInElements = 0;
            m_capacityInElements = capacityInElements;
            m_log2BlockSizeInElements = log2BlockSizeInElements;
            m_blocks = (capacityInElements + BlockMask) >> m_log2BlockSizeInElements;
            m_block = (IntPtr*)Memory.Unmanaged.Allocate(sizeof(IntPtr) * m_blocks, 16, m_backingAllocatorHandle);
            UnsafeUtility.MemSet(m_block, 0, sizeof(IntPtr) * m_blocks);
        }

        public void LockfreeAdd(T t)
        {
            var elementIndex = Interlocked.Increment(ref m_lengthInElements) - 1;
            var blockIndex = BlockIndexOfElement(elementIndex);
            CheckBlockIndex(blockIndex);
            if(m_block[blockIndex] == IntPtr.Zero)
            {
                void* pointer = Memory.Unmanaged.Allocate(BlockSizeInBytes, 16, m_backingAllocatorHandle); // $$$!
                var lastBlock = math.min(m_blocks, blockIndex + 4); // don't overgrow too fast, simply to avoid a $$$ free
                for(; blockIndex < lastBlock; ++blockIndex)
                    if(IntPtr.Zero == Interlocked.CompareExchange(ref m_block[blockIndex], (IntPtr)pointer, IntPtr.Zero))
                        break; // install the new block, into *any* empty slot available, to avoid wasting the time we spent on malloc
                if(blockIndex == lastBlock)
                    Memory.Unmanaged.Free(pointer, m_backingAllocatorHandle); // $$$, only if absolutely necessary
            }
            this[elementIndex] = t;
        }

        public ref T this[int elementIndex]
        {
            get
            {
                CheckElementIndex(elementIndex);
                var blockIndex = BlockIndexOfElement(elementIndex);
                CheckBlockIndex(blockIndex);
                CheckBlockIsNotNull(blockIndex);
                IntPtr blockIntPtr = m_block[blockIndex];
                var elementIndexInBlock = elementIndex & BlockMask;
                T* blockPointer = (T*)blockIntPtr;
                return ref blockPointer[elementIndexInBlock];
            }
        }

        public void Rewind()
        {
            m_lengthInElements = 0;
        }

        public void Clear()
        {
            Rewind();
            for(var i = 0; i < m_blocks; ++i)
                if(m_block[i] != IntPtr.Zero)
                {
                    Memory.Unmanaged.Free((void*)m_block[i], m_backingAllocatorHandle);
                    m_block[i] = IntPtr.Zero;
                }
        }

        public void Dispose()
        {
            Clear();
            Memory.Unmanaged.Free(m_block, m_backingAllocatorHandle);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckElementIndex(int elementIndex)
        {
            if (elementIndex >= m_lengthInElements)
                throw new ArgumentException($"Element index {elementIndex} must be less than length in elements {m_lengthInElements}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckBlockIndex(int blockIndex)
        {
            if (blockIndex >= m_blocks)
                throw new ArgumentException($"Block index {blockIndex} must be less than number of blocks {m_blocks}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckBlockIsNotNull(int blockIndex)
        {
            if(m_block[blockIndex] == IntPtr.Zero)
                throw new ArgumentException($"Block index {blockIndex} is a null pointer.");
        }

        public void RemoveAtSwapBack(int elementIndex)
        {
            this[elementIndex] = this[Length-1];
            --m_lengthInElements;
        }

        int BlockIndexOfElement(int elementIndex)
        {
            return elementIndex >> m_log2BlockSizeInElements;
        }

        public void TrimExcess()
        {
            for(var blockIndex = BlockIndexOfElement(m_lengthInElements + BlockMask); blockIndex < m_blocks; ++blockIndex)
            {
                CheckBlockIndex(blockIndex);
                if(m_block[blockIndex] != IntPtr.Zero)
                {
                    var blockIntPtr = m_block[blockIndex];
                    void* blockPointer = (void*)blockIntPtr;
                    Memory.Unmanaged.Free(blockPointer, m_backingAllocatorHandle);
                    m_block[blockIndex] = IntPtr.Zero;
                }
            }
        }
    }

    [BurstCompile]
    internal struct AutoFreeAllocator : AllocatorManager.IAllocator
    {
        ArrayOfArrays<IntPtr> m_allocated;
        ArrayOfArrays<IntPtr> m_tofree;
        AllocatorManager.AllocatorHandle m_handle;
        AllocatorManager.AllocatorHandle m_backingAllocatorHandle;

        unsafe public void Update()
        {
            for(var i = m_tofree.Length; i --> 0;)
                for(var j = m_allocated.Length; j --> 0;)
                    if(m_allocated[j] == m_tofree[i])
                    {
                        Memory.Unmanaged.Free((void*)m_tofree[i], m_backingAllocatorHandle);
                        m_allocated.RemoveAtSwapBack(j);
                        break;
                    }
            m_tofree.Rewind();
            m_allocated.TrimExcess();
        }

        unsafe public void Initialize(AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            m_allocated = new ArrayOfArrays<IntPtr>(1024 * 1024, backingAllocatorHandle);
            m_tofree = new ArrayOfArrays<IntPtr>(128 * 1024, backingAllocatorHandle);
            m_backingAllocatorHandle = backingAllocatorHandle;
        }

        unsafe public void FreeAll()
        {
            Update();
            m_handle.Rewind();
            for(var i = 0; i < m_allocated.Length; ++i)
                Memory.Unmanaged.Free((void*) m_allocated[i], m_backingAllocatorHandle);
            m_allocated.Rewind();
        }

        /// <summary>
        /// Dispose the allocator. This must be called to free the memory blocks that were allocated from the system.
        /// </summary>
        public void Dispose()
        {
            FreeAll();
            m_tofree.Dispose();
            m_allocated.Dispose();
        }

        /// <summary>
        /// The allocator function. It can allocate, deallocate, or reallocate.
        /// </summary>
        public AllocatorManager.TryFunction Function => Try;

        /// <summary>
        /// Invoke the allocator function.
        /// </summary>
        /// <param name="block">The block to allocate, deallocate, or reallocate. See <see cref="AllocatorManager.Try"/></param>
        /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
        public int Try(ref AllocatorManager.Block block)
        {
            unsafe
            {
                if (block.Range.Pointer == IntPtr.Zero)
                {
                    if (block.Bytes == 0)
                    {
                        return 0;
                    }

                    var ptr = (byte*)Memory.Unmanaged.Allocate(block.Bytes, block.Alignment, m_backingAllocatorHandle);
                    block.Range.Pointer = (IntPtr)ptr;
                    block.AllocatedItems = block.Range.Items;

                    m_allocated.LockfreeAdd(block.Range.Pointer);

                    return 0;
                }

                if (block.Range.Items == 0)
                {
                    m_tofree.LockfreeAdd(block.Range.Pointer);

                    block.Range.Pointer = IntPtr.Zero;
                    block.AllocatedItems = 0;

                    return 0;
                }

                return -1;
            }
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        internal static int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            unsafe { return ((AutoFreeAllocator*)state)->Try(ref block); }
        }

        /// <summary>
        /// This allocator.
        /// </summary>
        /// <value>This allocator.</value>
        public AllocatorManager.AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

        /// <summary>
        /// Cast the Allocator index into Allocator
        /// </summary>
        public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

        /// <summary>
        /// Check whether an allocator is a custom allocator
        /// </summary>
        public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

        /// <summary>
        /// Check whether this allocator will automatically dispose allocations.
        /// </summary>
        /// <remarks>Allocations made by Auto free allocator are automatically disposed.</remarks>
        /// <value>Always true</value>
        public bool IsAutoDispose { get { return true; } }
    }
}
