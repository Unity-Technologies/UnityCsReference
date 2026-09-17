// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using AOT;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using ManagedKernel.Assertions;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections
{
    internal struct UnmanagedArray<T> : IDisposable where T : unmanaged
    {
        IntPtr m_pointer;
        int m_length;
        public int Length => m_length;
        AllocatorManager.AllocatorHandle m_allocator;
        public UnmanagedArray(int length, AllocatorManager.AllocatorHandle allocator)
        {
            unsafe
            {
                m_pointer = (IntPtr)Memory.Unmanaged.Array.Allocate<T>(length, allocator);
            }
            m_length = length;
            m_allocator = allocator;
        }
        public void Dispose()
        {
            unsafe
            {
                Memory.Unmanaged.Free((T*)m_pointer, Allocator.Persistent);
            }
        }
        public unsafe T* GetUnsafePointer()
        {
            return (T*)m_pointer;
        }
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { unsafe { return ref ((T*)m_pointer)[index]; } }
        }
    }

    /// <summary>
    /// An allocator that is fast like a linear allocator, is threadsafe, and automatically invalidates
    /// all allocations made from it, when "rewound" by the user.
    /// </summary>
    [BurstCompile]
    public struct RewindableAllocator : AllocatorManager.IAllocator
    {
        internal struct Union
        {
            internal long m_long;

            // Number of bits used to store current position in a block to give out memory.
            // This limits the maximum block size to 1TB (2^40).
            const int currentBits = 40;
            // Offset of current position in m_long
            const int currentOffset = 0;
            // Number of bits used to store the allocation count in a block
            const long currentMask = (1L << currentBits) - 1;

            // Number of bits used to store allocation count in a block.
            // This limits the maximum number of allocations per block to 16 millions (2^24)
            const int allocCountBits = 24;
            // Offset of allocation count in m_long
            const int allocCountOffset = currentOffset + currentBits;
            const long allocCountMask = (1L << allocCountBits) - 1;

            // Current position in a block to give out memory
            internal long m_current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (m_long >> currentOffset) & currentMask;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    m_long &= ~(currentMask << currentOffset);
                    m_long |= (value & currentMask) << currentOffset;
                }
            }

            // The number of allocations in a block
            internal long m_allocCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return (m_long >> allocCountOffset) & allocCountMask;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    m_long &= ~(allocCountMask << allocCountOffset);
                    m_long |= (value & allocCountMask) << allocCountOffset;
                }
            }
        }
        [GenerateTestsForBurstCompatibility]
        internal unsafe struct MemoryBlock : IDisposable
        {
            // can't align any coarser than this many bytes
            public const int kMaximumAlignment = 16384;
            // pointer to contiguous memory
            public byte* m_pointer;
            // how many bytes of contiguous memory it points to
            public long m_bytes;
            // Union of current position to give out memory and allocation counts
            public Union m_union;
            // Memory label for tracking allocations
            MemoryLabel m_memoryLabel;

            public MemoryBlock(long bytes, MemoryLabel memoryLabel)
            {
                m_memoryLabel = memoryLabel;
                m_pointer = (byte*)Memory.Unmanaged.Allocate(bytes, kMaximumAlignment, m_memoryLabel);
                Assert.IsTrue(m_pointer != null, "Memory block allocation failed, system out of memory");
                m_bytes = bytes;
                m_union = default;
            }

            public void Rewind()
            {
                m_union = default;
            }

            public void Dispose()
            {
                Memory.Unmanaged.Free(m_pointer, m_memoryLabel);
                m_pointer = null;
                m_bytes = 0;
                m_union = default;
            }

            public bool Contains(IntPtr ptr)
            {
                unsafe
                {
                    void* pointer = (void*)ptr;
                    return (pointer >= m_pointer) && (pointer < m_pointer + m_union.m_current);
                }
            }
        };

        // Log2 of Maximum memory block size.  Cannot exceed MemoryBlock.Union.currentBits.
        const int kLog2MaxMemoryBlockSize = 26;

        // Maximum memory block size.  Can exceed maximum memory block size if user requested more.
        const long kMaxMemoryBlockSize = 1L << kLog2MaxMemoryBlockSize;  // 64MB

        /// Minimum memory block size, 128KB.
        const long kMinMemoryBlockSize = 128 * 1024;

        /// Maximum number of memory blocks.
        const int kMaxNumBlocks = 64;

        // Bit mask (bit 31) of the memory block busy flag indicating whether the block is busy rewinding.
        const int kBlockBusyRewindMask = 0x1 << 31;

        // Bit mask of the memory block busy flag indicating whether the block is busy allocating.
        const int kBlockBusyAllocateMask = ~kBlockBusyRewindMask;

        Spinner m_spinner;
        AllocatorManager.AllocatorHandle m_handle;
        UnmanagedArray<MemoryBlock> m_block;
        MemoryLabel m_memoryLabel;
        int m_last;                 // highest-index block that has memory to allocate from
        int m_used;                 // highest-index block that we actually allocated from, since last rewind
        byte m_enableBlockFree;     // flag indicating if allocator enables individual block free
        byte m_reachMaxBlockSize;   // flag indicating if reach maximum block size

        /// <summary>
        /// Initializes the allocator. Must be called before first use.
        /// </summary>
        /// <param name="initialSizeInBytes">The initial capacity of the allocator, in bytes</param>
        /// <param name="enableBlockFree">A flag indicating if allocator enables individual block free</param>
        public void Initialize(int initialSizeInBytes, bool enableBlockFree = false)
        {
            m_spinner = default;
            m_memoryLabel = Memory.CreateLabel("Collections", "Allocator.Rewindable", Allocator.Persistent);
            m_block = new UnmanagedArray<MemoryBlock>(kMaxNumBlocks, Allocator.Persistent);
            // Initial block size should be larger than min block size
            var blockSize = initialSizeInBytes > kMinMemoryBlockSize ? initialSizeInBytes : kMinMemoryBlockSize;
            m_block[0] = new MemoryBlock(blockSize, m_memoryLabel);
            m_last = m_used = 0;
            m_enableBlockFree = enableBlockFree ? (byte)1 : (byte)0;
            m_reachMaxBlockSize = (initialSizeInBytes >= kMaxMemoryBlockSize) ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Property to get and set enable block free flag, a flag indicating whether the allocator should enable individual block to be freed.
        /// </summary>
        public bool EnableBlockFree
        {
            get => m_enableBlockFree != 0;
            set => m_enableBlockFree = value ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Retrieves the number of memory blocks that the allocator has requested from the system.
        /// </summary>
        public int BlocksAllocated => (int)(m_last + 1);

        /// <summary>
        /// Retrieves the size of the initial memory block, as requested in the Initialize function.
        /// </summary>
        public int InitialSizeInBytes => (int)(m_block[0].m_bytes);

        /// <summary>
        /// Retrieves the maximum memory block size.
        /// </summary>
        internal long MaxMemoryBlockSize => kMaxMemoryBlockSize;

        /// <summary>
        /// Retrieves the total bytes of the memory blocks allocated by this allocator.
        /// </summary>
        internal long BytesAllocated
        {
            get
            {
                long totalBytes = 0;
                for(int i = 0; i <= m_last; i++)
                {
                    totalBytes += m_block[i].m_bytes;
                }
                return totalBytes;
            }
        }

        /// <summary>
        /// Rewind the allocator; invalidate all allocations made from it, and potentially also free memory blocks
        /// it has allocated from the system.
        /// </summary>
        public void Rewind()
        {
            if (JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("You cannot Rewind a RewindableAllocator from a Job.");
            m_handle.Rewind(); // bump the allocator handle version, invalidate all dependents
            while (m_last > m_used) // *delete* all blocks we didn't even allocate from this time around.
                m_block[m_last--].Dispose();
            while (m_used > 0) // simply *rewind* all blocks we used in this update, to avoid allocating again, every update.
                m_block[m_used--].Rewind();
            m_block[0].Rewind();
        }

        /// <summary>
        /// Dispose the allocator. This must be called to free the memory blocks that were allocated from the system.
        /// </summary>
        public void Dispose()
        {
            if (JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("You cannot Dispose a RewindableAllocator from a Job.");
            m_used = 0; // so that we delete all blocks in Rewind() on the next line
            Rewind();
            m_block[0].Dispose();
            m_block.Dispose();
            m_last = m_used = 0;
        }

        /// <summary>
        /// All allocators must implement this property, in order to be installed in the custom allocator table.
        /// </summary>
        [ExcludeFromBurstCompatTesting("Uses managed delegate")]
        public AllocatorManager.TryFunction Function => Try;

        unsafe int TryAllocate(ref AllocatorManager.Block block, int startIndex, int lastIndex, long alignedSize, long alignmentMask)
        {
            for (int best = startIndex; best <= lastIndex; best++)
            {
                Union oldUnion;
                Union readUnion = default;
                long begin = 0;
                bool skip = false;
                readUnion.m_long = Interlocked.Read(ref m_block[best].m_union.m_long);
                do
                {
                    begin = (readUnion.m_current + alignmentMask) & ~alignmentMask;
                    if (begin + block.Bytes > m_block[best].m_bytes)
                    {
                        skip = true;
                        break;
                    }
                    oldUnion = readUnion;
                    Union newUnion = default;
                    newUnion.m_current = (begin + alignedSize) > m_block[best].m_bytes ? m_block[best].m_bytes : (begin + alignedSize);
                    newUnion.m_allocCount = readUnion.m_allocCount + 1;
                    readUnion.m_long = Interlocked.CompareExchange(ref m_block[best].m_union.m_long, newUnion.m_long, oldUnion.m_long);
                } while (readUnion.m_long != oldUnion.m_long);

                if(skip)
                {
                    continue;
                }

                block.Range.Pointer = (IntPtr)(m_block[best].m_pointer + begin);
                block.AllocatedItems = block.Range.Items;

                Interlocked.MemoryBarrier();
                int oldUsed;
                int readUsed;
                int newUsed;
                readUsed = m_used;
                do
                {
                    oldUsed = readUsed;
                    newUsed = best > oldUsed ? best : oldUsed;
                    readUsed = Interlocked.CompareExchange(ref m_used, newUsed, oldUsed);
                } while (newUsed != oldUsed);

                return AllocatorManager.kErrorNone;
            }

            return AllocatorManager.kErrorBufferOverflow;
        }

        /// <summary>
        /// Try to allocate, free, or reallocate a block of memory. This is an internal function, and
        /// is not generally called by the user.
        /// </summary>
        /// <param name="block">The memory block to allocate, free, or reallocate</param>
        /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
        public int Try(ref AllocatorManager.Block block)
        {
            if (block.Range.Pointer == IntPtr.Zero)
            {
                // Make the alignment multiple of cacheline size
                var alignment = math.max(JobsUtility.CacheLineSize, block.Alignment);
                var extra = alignment != JobsUtility.CacheLineSize ? 1 : 0;
                var cachelineMask = JobsUtility.CacheLineSize - 1;
                if (extra == 1)
                {
                    alignment = (alignment + cachelineMask) & ~cachelineMask;
                }

                // Adjust the size to be multiple of alignment, add extra alignment
                // to size if alignment is more than cacheline size
                var mask = alignment - 1L;
                var size = (block.Bytes + extra * alignment + mask) & ~mask;

                // Check all the blocks to see if any of them have enough memory
                var last = m_last;
                int error = TryAllocate(ref block, 0, m_last, size, mask);
                if (error == AllocatorManager.kErrorNone)
                {
                    return error;
                }

                // If that fails, allocate another block that's guaranteed big enough, and allocate from it.
                // Allocate twice as much as last time until it reaches MaxMemoryBlockSize, after that, increase
                // the block size by MaxMemoryBlockSize.
                m_spinner.Acquire();

                // After getting the lock, we must try to allocate again, because if many threads waited at
                // the lock, the first one allocates and when it unlocks, it's likely that there's space for the
                // other threads' allocations in the first thread's block.
                error = TryAllocate(ref block, last, m_last, size, mask);
                if (error == AllocatorManager.kErrorNone)
                {
                    m_spinner.Release();
                    return error;
                }

                long bytes;
                if (m_reachMaxBlockSize == 0)
                {
                    bytes = m_block[m_last].m_bytes << 1;
                }
                else
                {
                    bytes = m_block[m_last].m_bytes + kMaxMemoryBlockSize;
                }
                // if user asks more, skip smaller sizes
                bytes = math.max(bytes, size);
                m_reachMaxBlockSize = (bytes >= kMaxMemoryBlockSize) ? (byte)1 : (byte)0;
                m_block[m_last + 1] = new MemoryBlock(bytes, m_memoryLabel);
                Interlocked.Increment(ref m_last);
                error = TryAllocate(ref block, m_last, m_last, size, mask);
                m_spinner.Release();
                return error;
            }

            // To free memory, no-op unless allocator enables individual block to be freed
            if (block.Range.Items == 0)
            {
                if (m_enableBlockFree != 0)
                {
                    for (int blockIndex = 0; blockIndex <= m_last; ++blockIndex)
                    {
                        if (m_block[blockIndex].Contains(block.Range.Pointer))
                        {
                            Union oldUnion;
                            Union readUnion = default;
                            readUnion.m_long = Interlocked.Read(ref m_block[blockIndex].m_union.m_long);
                            do
                            {
                                oldUnion = readUnion;
                                Union newUnion = readUnion;
                                newUnion.m_allocCount--;
                                if (newUnion.m_allocCount == 0)
                                {
                                    newUnion.m_current = 0;
                                }
                                readUnion.m_long = Interlocked.CompareExchange(ref m_block[blockIndex].m_union.m_long, newUnion.m_long, oldUnion.m_long);
                            } while (readUnion.m_long != oldUnion.m_long);
                        }
                    }
                }
                return 0; // we could check to see if the pointer belongs to us, if we want to be strict about it.
            }

            return -1;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        internal static int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            unsafe { return ((RewindableAllocator*)state)->Try(ref block); }
        }

        /// <summary>
        /// Retrieve the AllocatorHandle associated with this allocator. The handle is used as an index into a
        /// global table, for times when a reference to the allocator object isn't available.
        /// </summary>
        /// <value>The AllocatorHandle retrieved.</value>
        public AllocatorManager.AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

        /// <summary>
        /// Retrieve the Allocator associated with this allocator.
        /// </summary>
        /// <value>The Allocator retrieved.</value>
        public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

        /// <summary>
        /// Check whether this AllocatorHandle is a custom allocator.
        /// </summary>
        /// <value>True if this AllocatorHandle is a custom allocator.</value>
        public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

        /// <summary>
        /// Check whether this allocator will automatically dispose allocations.
        /// </summary>
        /// <remarks>Allocations made by Rewindable allocator are automatically disposed.</remarks>
        /// <value>Always true</value>
        public bool IsAutoDispose { get { return true; } }

        /// <summary>
        /// Allocate a NativeArray of type T from memory that is guaranteed to remain valid until the end of the
        /// next Update of this World. There is no need to Dispose the NativeArray so allocated. It is not possible
        /// to free the memory by Disposing it - it is automatically freed after the end of the next Update for this
        /// World.
        /// </summary>
        /// <typeparam name="T">The element type of the NativeArray to allocate.</typeparam>
        /// <param name="length">The length of the NativeArray to allocate, measured in elements.</param>
        /// <returns>The NativeArray allocated by this function.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public NativeArray<T> AllocateNativeArray<T>(int length) where T : unmanaged
        {
            var container = new NativeArray<T>();
            unsafe
            {
                container.m_Buffer = this.AllocateStruct(default(T), length);
            }
            container.m_Length = length;
            container.m_AllocatorLabel = Allocator.None;
            container.m_MinIndex = 0;
            container.m_MaxIndex = length - 1;
            container.m_Safety = CollectionHelper.CreateSafetyHandle(ToAllocator);
            CollectionHelper.SetStaticSafetyId<NativeArray<T>>(ref container.m_Safety, ref NativeArrayExtensions.NativeArrayStaticId<T>.s_staticSafetyId.Data);
            Handle.AddSafetyHandle(container.m_Safety);
            return container;
        }

        /// <summary>
        /// Allocate a NativeList of type T from memory that is guaranteed to remain valid until the end of the
        /// next Update of this World. There is no need to Dispose the NativeList so allocated. It is not possible
        /// to free the memory by Disposing it - it is automatically freed after the end of the next Update for this
        /// World. The NativeList must be initialized with its maximum capacity; if it were to dynamically resize,
        /// up to 1/2 of the total final capacity would be wasted, because the memory can't be dynamically freed.
        /// </summary>
        /// <typeparam name="T">The element type of the NativeList to allocate.</typeparam>
        /// <param name="capacity">The capacity of the NativeList to allocate, measured in elements.</param>
        /// <returns>The NativeList allocated by this function.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public NativeList<T> AllocateNativeList<T>(int capacity) where T : unmanaged
        {
            var container = new NativeList<T>();
            unsafe
            {
                container.m_ListData = this.Allocate(default(UnsafeList<T>), 1);
                container.m_ListData->Ptr = this.Allocate(default(T), capacity);
                container.m_ListData->m_length = 0;
                container.m_ListData->m_capacity = capacity;
                container.m_ListData->Allocator = Allocator.None;
            }
            container.m_Safety = CollectionHelper.CreateSafetyHandle(ToAllocator);
            CollectionHelper.SetStaticSafetyId<NativeList<T>>(ref container.m_Safety, ref NativeList<T>.s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(container.m_Safety, true);
            Handle.AddSafetyHandle(container.m_Safety);
            return container;
        }
    }
}
