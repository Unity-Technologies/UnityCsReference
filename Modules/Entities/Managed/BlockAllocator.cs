// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct BlockAllocator : IDisposable
    {
        BufferAllocator m_bufferAllocator;
        private UnsafeList<int> m_allocations;
        int m_currentBlockIndex;
        byte* m_nextPtr;

        private const int ms_Log2BlockSize = 16;
        private const int ms_BlockSize = 1 << ms_Log2BlockSize;
        internal const int MaxBudgetInBytes = (int.MaxValue / ms_BlockSize) * ms_BlockSize;

        private int ms_BudgetInBytes => m_bufferAllocator.BufferCapacity << ms_Log2BlockSize;

        internal int BudgetInBytes => ms_BudgetInBytes;
        internal int AllocatedBytes => m_bufferAllocator.AllocatedCount << ms_Log2BlockSize;

        public BlockAllocator(AllocatorManager.AllocatorHandle handle, int budgetInBytes)
        {
            budgetInBytes = Math.Min(budgetInBytes, MaxBudgetInBytes);
            m_bufferAllocator = new BufferAllocator(budgetInBytes, ms_BlockSize, handle);
            m_nextPtr = null;
            var blocks = (budgetInBytes + ms_BlockSize - 1) >> ms_Log2BlockSize;
            m_allocations = new UnsafeList<int>(blocks, handle);

            for (int i = 0; i < blocks; ++i)
            {
                m_allocations.Add(0);
            }

            m_currentBlockIndex = -1;
        }

        public void Dispose()
        {
            m_bufferAllocator.Dispose();
            m_allocations.Dispose();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckBlockHasAllocations(int blockIndex)
        {
            if (m_allocations.Ptr[blockIndex] <= 0) // if that block has no allocations, we can't proceed
                throw new ArgumentException($"Cannot free this pointer from BlockAllocator: no more allocations to free in its block.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void ThrowCouldNotFindPointer()
        {
            throw new ArgumentException($"Cannot free this pointer from BlockAllocator: can't be found in any block.");
        }

        public void Free(void* pointer)
        {
            if (pointer == null)
                return;
            var blocks = m_allocations.Length; // how many blocks have we allocated?
            for (var i = blocks - 1; i >= 0; --i)
            {
                var block = (byte*)m_bufferAllocator[i]; // get a pointer to the block.
                if (pointer >= block && pointer < block + ms_BlockSize) // is the pointer we want to free in this block?
                {
                    CheckBlockHasAllocations(i);
                    if (--m_allocations.Ptr[i] == 0) // if this was the last allocation in the block,
                    {
                        if (i == blocks - 1) // if it's the last block,
                            m_nextPtr = (byte*)m_bufferAllocator[i]; // just forget that we allocated anything from it, but keep it for later allocations
                        else
                        {
                            m_bufferAllocator.Free(i);

                            // If the current block is freed then we should reset it to ensure we
                            // allocate a new block on the next allocation.
                            if (i == m_currentBlockIndex)
                            {
                                m_currentBlockIndex = -1;
                            }
                        }
                    }
                    return;
                }
            }
            ThrowCouldNotFindPointer();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckAllocationTooLarge(int bytesToAllocate, int alignment)
        {
            if (bytesToAllocate > ms_BlockSize)
                throw new ArgumentException($"Cannot allocate more than {ms_BlockSize} in BlockAllocator. Requested: {bytesToAllocate}");

            // This check is to be sure that the given allocation size and alignment can even be guaranteed by the
            // allocator. Due to the fixed block sizes, there are some values of bytesToAllocate < ms_BlockSize which
            // may fail due to the alignment requirement.
            var worstCaseBytesWithAlignment = bytesToAllocate + alignment - 1;
            if (worstCaseBytesWithAlignment > ms_BlockSize)
            {
                var maxAllocationSizeForGivenAlignment = ms_BlockSize - (alignment - 1);

                throw new ArgumentException($"Cannot guarantee allocation of {bytesToAllocate} bytes. Allocation size must be <= {maxAllocationSizeForGivenAlignment} bytes to guarantee allocation.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckExceededBudget()
        {
            if (m_bufferAllocator.IsEmpty)
                throw new InvalidOperationException($"Cannot allocate more than the BlockAllocator budget of {ms_BudgetInBytes} bytes.");
        }

        /// <summary>
        /// Allocates memory out of a block.
        /// </summary>
        /// <remarks>
        /// Not all allocation sizes and alignment combinations are valid. The maximum value bytesToAllocate can be is
        /// (ms_BlockSize - (alignment - 1)).
        /// </remarks>
        /// <param name="bytesToAllocate">Bytes to allocate.</param>
        /// <param name="alignment">Alignment in bytes for the allocation.</param>
        /// <returns>Pointer to allocation, or null if the budget is exhausted.</returns>
        public byte* Allocate(int bytesToAllocate, int alignment)
        {
            CheckAllocationTooLarge(bytesToAllocate, alignment);
            var nextAligned = (byte*)CollectionHelper.AlignPointer(m_nextPtr, alignment);
            var nextAllocationEnd = nextAligned + bytesToAllocate;

            // If we haven't allocated a block or the next allocation end is past the end of the current block, then allocate a new block.
            if (m_currentBlockIndex < 0 || nextAllocationEnd > (byte*)m_bufferAllocator[m_currentBlockIndex] + ms_BlockSize)
            {
                CheckExceededBudget();
                // Allocate a fresh block of memory
                int index = m_bufferAllocator.Allocate();
                if (index < 0)
                    return null;
                m_allocations.Ptr[index] = 0;
                m_currentBlockIndex = index;
                nextAligned = (byte*)CollectionHelper.AlignPointer(m_bufferAllocator[m_currentBlockIndex], alignment);
                nextAllocationEnd = nextAligned + bytesToAllocate;
            }

            var pointer = nextAligned;
            m_nextPtr = nextAllocationEnd;
            m_allocations.Ptr[m_currentBlockIndex]++;
            return pointer;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] {typeof(BurstCompatibleComponentData)})]
        public T* Allocate<T>(int items = 1) where T : unmanaged
        {
            return (T*)Allocate(items * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>());
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] {typeof(BurstCompatibleComponentData)})]
        public byte* Construct(int size, int alignment, void* src)
        {
            var res = Allocate(size, alignment);
            UnsafeUtility.MemCpy(res, src, size);
            return res;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] {typeof(BurstCompatibleComponentData)})]
        public T* Construct<T>(T* src) where T : unmanaged
        {
            return (T*)Construct(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), src);
        }
    }
}
