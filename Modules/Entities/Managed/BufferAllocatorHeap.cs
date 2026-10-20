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
    internal unsafe struct BufferAllocatorHeap : IBufferAllocator
    {
        UnsafeList<IntPtr> Buffers;
        UnsafeBitArray FreeList;
        readonly AllocatorManager.AllocatorHandle Handle;
        readonly int BufferSizeInBytes;

        const int kBufferAlignment = 64; //cache line size

        /// <summary>
        /// Constructs an allocator.
        /// </summary>
        /// <param name="budgetInBytes">Budget of the allocator in bytes.</param>
        /// <param name="bufferSizeInBytes">Size of each buffer to be allocated in bytes.</param>
        /// <param name="handle">An AllocatorHandle to use for buffer allocations and internal bookkeeping structures.</param>
        public BufferAllocatorHeap(int budgetInBytes, int bufferSizeInBytes, AllocatorManager.AllocatorHandle handle)
        {
            Handle = handle;
            BufferSizeInBytes = bufferSizeInBytes;
            var bufferCount = (budgetInBytes + bufferSizeInBytes - 1) / bufferSizeInBytes;

            // Use Legacy allocator such that when custom allocator rewinds, Buffers is still valid.
            Buffers = new UnsafeList<IntPtr>(bufferCount, AllocatorManager.LegacyOf(handle));

            for (int i = 0; i < bufferCount; ++i)
            {
                Buffers.Add(IntPtr.Zero);
            }

            FreeList = new UnsafeBitArray(bufferCount, AllocatorManager.LegacyOf(handle));
        }

        /// <summary>
        /// Allocates an index which corresponds to a buffer.
        /// </summary>
        /// <returns>Allocated index. If allocation fails, returned index is negative.</returns>
        /// <exception cref="InvalidOperationException">Thrown when allocator is exhausted.</exception>
        public int Allocate()
        {
            int index = FreeList.Find(0, BufferCapacity, 1);
            if (index >= BufferCapacity)
            {
                ThrowFreeListEmpty();
                return -1;
            }

            var bufferPtr = AllocatorManager.Allocate(Handle, sizeof(byte), kBufferAlignment, BufferSizeInBytes);

            if (bufferPtr == null)
            {
                ThrowAllocationFailed();
                return -1;
            }

            FreeList.Set(index, true);
            Buffers[index] = (IntPtr) bufferPtr;

            return index;
        }

        /// <summary>
        /// Frees the buffer represented by the given index.
        /// </summary>
        /// <param name="index">Index to buffer.</param>
        /// <exception cref="ArgumentException">Thrown when index is less than zero or when greater than or equal to BufferCapacity</exception>
        public void Free(int index)
        {
            CheckInvalidIndexToFree(index);
            AllocatorManager.Free(Handle, (void*) Buffers[index]);
            FreeList.Set(index, false);
            Buffers[index] = IntPtr.Zero;
        }

        /// <summary>
        /// Converts an index to a pointer.
        /// </summary>
        /// <param name="index">Index to a buffer.</param>
        public void* this[int index] => (void*) Buffers[index];

        /// <summary>
        /// Maximum number of buffers that can be allocated at once.
        /// </summary>
        public int BufferCapacity => Buffers.Length;

        /// <summary>
        /// Checks if all the buffers in the allocator have been allocated.
        /// </summary>
        public bool IsEmpty => FreeList.CountBits(0, FreeList.Length) >= BufferCapacity;

        /// <summary>
        /// Number of buffers currently allocated.
        /// </summary>
        public int AllocatedCount => FreeList.CountBits(0, FreeList.Length);

        /// <summary>
        /// Disposes the allocator.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the reserved address range cannot be freed.</exception>
        public void Dispose()
        {
            for (int i = 0; i < Buffers.Length; ++i)
            {
                AllocatorManager.Free(Handle, (void*) Buffers[i]);
            }

            Buffers.Dispose();
            FreeList.Dispose();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void ThrowFreeListEmpty()
        {
            throw new InvalidOperationException("Cannot allocate, allocator is exhausted.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void ThrowAllocationFailed()
        {
            throw new InvalidOperationException("Failed to allocate buffer.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckInvalidIndexToFree(int index)
        {
            if (index < 0 || index >= BufferCapacity)
            {
                throw new ArgumentException($"Cannot free index {index}, it is outside the expected range [0, {BufferCapacity}).");
            }
        }
    }
}
