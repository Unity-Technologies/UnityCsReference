// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// A fixed-size buffer from which you can make allocations.
    /// </summary>
    /// <remarks>Allocations from a scratch allocator are not individually deallocated.
    /// Instead, when you're done using all the allocations from a scratch allocator, you dispose the allocator as a whole.</remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeScratchAllocator
    {
        void* m_Pointer;
        int m_LengthInBytes;
        readonly int m_CapacityInBytes;

        /// <summary>
        /// Initializes and returns an instance of UnsafeScratchAllocator.
        /// </summary>
        /// <param name="ptr">An existing buffer to use as the allocator's internal buffer.</param>
        /// <param name="capacityInBytes">The size in bytes of the internal buffer.</param>
        public UnsafeScratchAllocator(void* ptr, int capacityInBytes)
        {
            m_Pointer = ptr;
            m_LengthInBytes = 0;
            m_CapacityInBytes = capacityInBytes;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckAllocationDoesNotExceedCapacity(ulong requestedSize)
        {
            if (requestedSize > (ulong)m_CapacityInBytes)
                throw new ArgumentException($"Cannot allocate more than provided size in UnsafeScratchAllocator. Requested: {requestedSize} Size: {m_LengthInBytes} Capacity: {m_CapacityInBytes}");
        }

        /// <summary>
        /// Returns an allocation from the allocator's internal buffer.
        /// </summary>
        /// <param name="sizeInBytes">The size of the new allocation.</param>
        /// <param name="alignmentInBytes">The alignment of the new allocation.</param>
        /// <returns>A pointer to the new allocation.</returns>
        /// <exception cref="ArgumentException">Thrown if the new allocation would exceed the capacity of the allocator.</exception>
        public void* Allocate(int sizeInBytes, int alignmentInBytes)
        {
            if (sizeInBytes == 0)
                return null;
            var alignmentMask = (ulong)(alignmentInBytes - 1);
            var end = (ulong)(IntPtr)m_Pointer + (ulong)m_LengthInBytes;
            end = (end + alignmentMask) & ~alignmentMask;
            var lengthInBytes = (byte*)(IntPtr)end - (byte*)m_Pointer;
            lengthInBytes += sizeInBytes;
            CheckAllocationDoesNotExceedCapacity((ulong)lengthInBytes);
            m_LengthInBytes = (int)lengthInBytes;
            return (void*)(IntPtr)end;
        }

        /// <summary>
        /// Returns an allocation from the allocator's internal buffer.
        /// </summary>
        /// <remarks>The allocation size in bytes is at least `count * sizeof(T)`. The space consumed by the allocation may be a little larger than this size due to alignment.</remarks>
        /// <typeparam name="T">The type of element to allocate space for.</typeparam>
        /// <param name="count">The number of elements to allocate space for. Defaults to 1.</param>
        /// <returns>A pointer to the new allocation.</returns>
        /// <exception cref="ArgumentException">Thrown if the new allocation would exceed the capacity of the allocator.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public void* Allocate<T>(int count = 1) where T : unmanaged
        {
            return Allocate(sizeof(T) * count, UnsafeUtility.AlignOf<T>());
        }
    }
}
