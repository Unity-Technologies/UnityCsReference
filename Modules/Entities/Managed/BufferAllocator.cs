// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;

namespace Unity.Entities
{
    internal unsafe interface IBufferAllocator : IDisposable
    {
        /// <summary>
        /// Allocates an index which corresponds to a buffer.
        /// </summary>
        /// <returns>Allocated index. If allocation fails, returned index is negative.</returns>
        int Allocate();

        /// <summary>
        /// Frees the buffer represented by the given index.
        /// </summary>
        /// <param name="index">Index to buffer.</param>
        void Free(int index);

        /// <summary>
        /// Converts an index to a pointer.
        /// </summary>
        /// <param name="index">Index to a buffer.</param>
        void* this[int index] { get; }

        /// <summary>
        /// Maximum number of buffers that can be allocated at once.
        /// </summary>
        int BufferCapacity { get; }

        /// <summary>
        /// Checks if all the buffers in the allocator have been allocated.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Number of buffers currently allocated.
        /// </summary>
        int AllocatedCount { get; }
    }

    [GenerateTestsForBurstCompatibility]
    internal struct BufferAllocator : IBufferAllocator
    {
        BufferAllocatorHeap Allocator;

        /// <summary>
        /// Creates and initializes a BufferAllocator.
        /// </summary>
        /// <param name="budgetBytes">The budget for this allocator in bytes.</param>
        /// <param name="bufferBytes">The size of each buffer that will be allocated in bytes.</param>
        /// <param name="handle">The backing allocator to use for both internal BufferAllocator state and the buffers that will be allocated.</param>
        public BufferAllocator(int budgetBytes, int bufferBytes, AllocatorManager.AllocatorHandle handle)
        {
            Allocator = new BufferAllocatorHeap(budgetBytes, bufferBytes, handle);
        }

        /// <summary>
        /// Disposes this allocator.
        /// </summary>
        public void Dispose()
        {
            Allocator.Dispose();
        }

        /// <summary>
        /// Allocates an index which corresponds to a buffer.
        /// </summary>
        /// <returns>Allocated index. If allocation fails, returned index is negative.</returns>
        public int Allocate()
        {
            return Allocator.Allocate();
        }

        /// <summary>
        /// Frees the buffer represented by the given index.
        /// </summary>
        /// <param name="index">Index to buffer.</param>
        public void Free(int index)
        {
            Allocator.Free(index);
        }

        /// <summary>
        /// Converts an index to a pointer.
        /// </summary>
        /// <param name="index">Index to a buffer.</param>
        public unsafe void* this[int index] => Allocator[index];

        /// <summary>
        /// Maximum number of buffers that can be allocated at once.
        /// </summary>
        public int BufferCapacity => Allocator.BufferCapacity;

        /// <summary>
        /// Checks if all the buffers in the allocator have been allocated.
        /// </summary>
        public bool IsEmpty => Allocator.IsEmpty;

        /// <summary>
        /// Number of buffers currently allocated.
        /// </summary>
        public int AllocatedCount => Allocator.AllocatedCount;
    }
}
