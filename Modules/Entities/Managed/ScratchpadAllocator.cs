// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Entities
{
    /// <summary>
    /// A fast allocator which allocates memory from its scratchpad.
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct ScratchpadAllocator : AllocatorManager.IAllocator
    {
        const int kMaximumAlignment = 16384; // 16k, can't align any coarser than this many bytes

        byte* m_pointer; // pointer to the memory preserved
        internal AllocatorManager.AllocatorHandle m_handle;
        internal int m_bytes;
        internal int m_next;

        /// <summary>
        /// Try to allocate, free, or reallocate a block of memory.
        /// </summary>
        /// <param name="block">The memory block to allocate, free, or reallocate</param>
        /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
        public int Try(ref AllocatorManager.Block block)
        {
            if (block.Range.Pointer == IntPtr.Zero)
            {
                var next = m_next;
                var mask = JobsUtility.CacheLineSize - 1;
                var bytes = (block.Bytes + mask) & ~mask;

                if(m_next + bytes > m_bytes)
                {
                    throw new InvalidOperationException($"ScratchpadAllocator: Request {block.Bytes} bytes on top of current {m_next} bytes which exceeds max available {m_bytes}.");
                }

                m_next += (int)bytes;
                unsafe
                {
                    block.Range.Pointer = (IntPtr)(m_pointer + next);
                }
                return 0;
            }

            // "Free" should be a no-op
            if (block.Range.Items == 0)
            {
                // we could check to see if the pointer belongs to us, if we want to be strict about it.
                return 0;
            }

            return -1;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        internal static int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            unsafe { return ((ScratchpadAllocator*)state)->Try(ref block); }
        }

        /// <summary>
        /// Rewind the allocator; invalidate all allocations made from it.
        /// </summary>
        [BurstCompile]
        public void Rewind()
        {
            m_handle.Rewind();
            m_next = 0;
        }

        /// <summary>
        /// All allocators must implement this property, in order to be installed in the custom allocator table.
        /// </summary>
        [ExcludeFromBurstCompatTesting("Returns managed delegate")]
        public AllocatorManager.TryFunction Function => Try;

        /// <summary>
        /// Retrieve the AllocatorHandle associated with this allocator. The handle is used as an index into a
        /// global table, for times when a reference to the allocator object isn't available.
        /// </summary>
        /// <value>The AllocatorHandle retrieved.</value>
        public AllocatorManager.AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

        /// <summary>
        /// Retrieve the Allocator associated with this allocator handle.
        /// </summary>
        /// <value>The Allocator retrieved.</value>
        public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

        /// <summary>
        /// Check whether this allocator is a custom allocator.
        /// </summary>
        /// <remarks>The AllocatorHandle is a custom allocator if its Index is larger or equal to `FirstUserIndex`.</remarks>
        /// <value>True if this AllocatorHandle is a custom allocator.</value>
        public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

        /// <summary>
        /// Check whether this allocator will automatically dispose allocations.
        /// </summary>
        /// <remarks>Allocations made by Scrachpad allocator are automatically disposed.</remarks>
        /// <value>Always true</value>
        public bool IsAutoDispose { get { return true; } }

        /// <summary>
        /// Dispose the allocator.
        /// </summary>
        public void Dispose()
        {
            Rewind();
            var memoryLabel = Memory.CreateLabel("Entities", "Allocator.Scratchpad", Allocator.Persistent);
            Memory.Unmanaged.Free(m_pointer, memoryLabel);
        }

        /// <summary>
        /// Initializes the allocator. Must be called before first use.
        /// </summary>
        /// <param name="bytes">The amount of memory to allocate.</param>
        public void Initialize(int bytes)
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Allocator.Scratchpad", Allocator.Persistent);
            m_pointer = (byte*)Memory.Unmanaged.Allocate(bytes, kMaximumAlignment, memoryLabel);
            m_bytes = bytes;
            m_next = 0;
        }

        /// <summary>
        /// Get remaining bytes that are available to allocate.
        /// </summary>
        /// <returns>Returns the remaining bytes available.</returns>
        public int GetAvailableBytes()
        {
            return (m_bytes - m_next);
        }

        /// <summary>
        /// Allocate a NativeArray of type T from memory that's guaranteed to 
        /// remain valid until <see cref="Rewind"/> is called on the Scratchpad.
        /// </summary>
        /// <remarks>
        /// This memory isn't shared between threads and you don't need to Dispose 
        /// the NativeArray so it's allocated. You can't Dispose the memory to free 
        /// it: it's automatically freed when <see cref="Rewind"/> is called.
        /// </remarks>
        /// <param name="length">The number of items in the NativeArray.</param>
        /// <typeparam name="T">The NativeArray.</typeparam>
        /// <returns>Returns the NativeArray.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public NativeArray<T> AllocateNativeArray<T>(int length) where T : unmanaged
        { 
            unsafe
            {
                var container = new NativeArray<T>();
                container.m_Buffer = this.Allocate(default(T), length);
                container.m_Length = length;
                container.m_AllocatorLabel = ToAllocator;
                container.m_MinIndex = 0;
                container.m_MaxIndex = length - 1;
                container.m_Safety = CollectionHelper.CreateSafetyHandle(ToAllocator);
                CollectionHelper.SetStaticSafetyId<NativeArray<T>>(ref container.m_Safety, ref NativeArrayExtensions.NativeArrayStaticId<T>.s_staticSafetyId.Data);
                Handle.AddSafetyHandle(container.m_Safety);
                return container;
            }
        }

        /// <summary>
        /// Allocate a NativeList of type T from memory that's guaranteed to 
        /// remain valid until <see cref="Rewind"/> is called on the Scratchpad.
        /// </summary>
        /// <remarks>
        /// This memory isn't shared between threads and you don't need to Dispose 
        /// the NativeList so it's allocated. You can't Dispose the memory to free
        /// it: it's automatically freed when <see cref="Rewind"/> is called.
        /// </remarks>
        /// <typeparam name="T">The NativeList</typeparam>
        /// <param name="capacity">The number of items the list can hold.</param>
        /// <returns>Returns the NativeList</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public NativeList<T> AllocateNativeList<T>(int capacity) where T : unmanaged
        {
            unsafe
            {
                var container = new NativeList<T>();
                container.m_ListData = this.Allocate(default(UnsafeList<T>), 1);
                container.m_ListData->Ptr = this.Allocate(default(T), capacity);
                container.m_ListData->m_length = 0;
                container.m_ListData->m_capacity = capacity;
                container.m_ListData->Allocator = Handle;
                container.m_Safety = CollectionHelper.CreateSafetyHandle(ToAllocator);
                CollectionHelper.SetStaticSafetyId<NativeList<T>>(ref container.m_Safety, ref NativeList<T>.s_staticSafetyId.Data);
                AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(container.m_Safety, true);
                Handle.AddSafetyHandle(container.m_Safety);
                return container;
            }
        }
    };


    /// <summary>
    /// A scratch pad that contains multiple scratch pad allocators.  User can can get a scratch pad allocator from the pad
    /// corresponding to its thread index.  It automatically invalidates all allocations made from it, when "rewound" by the user.
    /// </summary>
    [BurstCompile]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct Scratchpad
    {
        [NativeDisableUnsafePtrRestriction]
        AllocatorHelper<ScratchpadAllocator>* m_allocatorHelpers;

        int m_numAllocatorHelpers;

        [NativeSetThreadIndex]
        internal int m_ThreadIndex;

        /// <summary>
        /// Retrieve the ScratchpadAllocator from the Scratchpad with index corresponding to the running thread index.
        /// </summary>
        /// <returns>Returns reference to the ScratchpadAllocator for the running thread.</returns>
        public ref ScratchpadAllocator GetScratchpadAllocator()
        {
            var allocatorHelperPtr = m_allocatorHelpers + m_ThreadIndex;
            return ref allocatorHelperPtr->Allocator;
        }

        /// <summary>
        /// Retrieve the ScratchpadAllocator from the Scratchpad with index corresponding to the running thread index.
        /// </summary>
        /// <param name="threadIndex">Index of the running thread.</param>
        /// <returns>Returns reference to the ScratchpadAllocator for the running thread.</returns>
        public ref ScratchpadAllocator GetScratchpadAllocator(int threadIndex)
        {
            var allocatorHelperPtr = m_allocatorHelpers + threadIndex;
            return ref allocatorHelperPtr->Allocator;
        }

        /// <summary>
        /// Create a Scratchpad.
        /// </summary>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Base index into the global function table of the allocator to be created.</param>
        /// <param name="numScratchpadAllocators">The number of ScratchpadAllocators the created Scratchpad should contain.</param>
        /// <param name="initialSizeInBytes">The initial size of the Scratchpad in bytes. Set to 32,768 by default.</param>
        [ExcludeFromBurstCompatTesting("Accesses managed delegates")]
        public Scratchpad(int numScratchpadAllocators, int initialSizeInBytes = 32768, bool isGlobal = false, int globalIndex = 0)
        {
            this = default;
            Initialize(numScratchpadAllocators, initialSizeInBytes, isGlobal, globalIndex);
        }

        /// <summary>
        /// Initialize a Scratchpad.
        /// </summary>
        /// <param name="numScratchpadAllocators">The number of ScratchpadAllocators the created Scratchpad should contain.</param>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Base index into the global function table of the allocator to be created.</param>
        /// <param name="initialSizeInBytes">The initial size of the Scratchpad in bytes. Set to 32,768 by default.</param>
        [ExcludeFromBurstCompatTesting("Accesses managed delegate")]
        public void Initialize(int numScratchpadAllocators, int initialSizeInBytes = 32768, bool isGlobal = false, int globalIndex = 0)
        {
            m_numAllocatorHelpers = numScratchpadAllocators;

            m_allocatorHelpers = (AllocatorHelper<ScratchpadAllocator>*)Memory.Unmanaged.Allocate(sizeof(AllocatorHelper<ScratchpadAllocator>) * m_numAllocatorHelpers,
                                                                                                        JobsUtility.CacheLineSize,
                                                                                                        Allocator.Persistent);

            for (var i = 0; i < m_numAllocatorHelpers; i++)
            {
                m_allocatorHelpers[i] = new AllocatorHelper<ScratchpadAllocator>(Allocator.Persistent, isGlobal, globalIndex + i);
                m_allocatorHelpers[i].Allocator.Initialize(initialSizeInBytes);
            }
        }

        /// <summary>
        /// Dispose the Scratchpad. This must be called to unregister and free the ScratchpadAllocators this Scratchpad contains.
        /// </summary>
        [ExcludeFromBurstCompatTesting("Accesses managed delegate")]
        public void Dispose()
        {
            if (JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("You cannot Dispose a Scratchpad from a Job.");

            for (var i = 0; i < m_numAllocatorHelpers; i++)
            {
                m_allocatorHelpers[i].Allocator.Dispose();
                m_allocatorHelpers[i].Dispose();
            }

            Memory.Unmanaged.Free(m_allocatorHelpers, Allocator.Persistent);
        }

        /// <summary>
        /// Rewind the Scratchpad; rewinds all ScratchpadAllocator invalidate all allocations made from it, and potentially also free memory blocks
        /// it has allocated from the system.
        /// </summary>
        public void Rewind()
        {
            if (JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("You cannot Rewind a whole Scratchpad from a Job.");

            for (var i = 0; i < m_numAllocatorHelpers; i++)
            {
                m_allocatorHelpers[i].Allocator.Rewind();
            }
        }
    }

    /// <summary>
    /// A global scratchpad.
    /// </summary>
    public struct GlobalScratchpad
    {
        /// <summary>
        /// Pre-allocated memory size in bytes.
        /// </summary>
        /// <remarks>This is the total number of memory in bytes that can be allocated by a global scratchpad allocator.</remarks>
        internal const ushort BlockSize = 32768;

        /// <summary>
        /// A flag indicating whether the global scratchpad is initialized.
        /// </summary>
        internal static readonly SharedStatic<byte> IsInstalled = SharedStatic<byte>.GetOrCreate<GlobalScratchpad, byte>();

        /// <summary>
        /// A thread index which is used to for each individual thread to its scratchpad allocator.
        /// </summary>
        internal static readonly SharedStatic<int> ThreadIndex = SharedStatic<int>.GetOrCreate<GlobalScratchpad, int>();

        /// <summary>
        /// A shared static to hold the global scratchpad allocators.
        /// </summary>
        internal static readonly SharedStatic<Scratchpad> Pad = SharedStatic<Scratchpad>.GetOrCreate<GlobalScratchpad, Scratchpad>();

        /// <summary>
        /// Initialize the global scratchpad, create and register all the global scratchpad allocators.
        /// </summary>
        public static void Initialize()
        {
            if (IsInstalled.Data == 0)
            {
                int maxThreadCount = JobsUtility.ThreadIndexCount;
                Pad.Data.Initialize(maxThreadCount, BlockSize, true, (int)AllocatorManager.GlobalAllocatorBaseIndex);
                IsInstalled.Data = 1;
            }
        }

        /// <summary>
        /// Retrieve a global scratchpad allocator.
        /// </summary>
        /// <returns>Returns the global scratchpad allocator.</returns>
        public static ref ScratchpadAllocator GetAllocator()
        {
            if (IsInstalled.Data == 0)
            {
                throw new InvalidOperationException("GlobalScratchpad is not initialized.");
            }

            int index = Interlocked.Increment(ref ThreadIndex.Data);
            index = (index - 1) % AllocatorManager.NumGlobalScratchAllocators;
            return ref Pad.Data.GetScratchpadAllocator(index);
        }

        /// <summary>
        /// Rewind the Scratchpad; rewinds all ScratchpadAllocator invalidate all allocations made from it, and potentially also free memory blocks
        /// it has allocated from the system.
        /// </summary>
        public static void Rewind()
        {
            if (IsInstalled.Data == 0)
            {
                throw new InvalidOperationException("GlobalScratchpad is not initialized.");
            }

            if (JobsUtility.IsExecutingJob)
            {
                throw new InvalidOperationException("You cannot Rewind GlobalScratchpad from a Job.");
            }

            Interlocked.Exchange(ref ThreadIndex.Data, 0);
            Pad.Data.Rewind();
        }

        /// <summary>
        /// Dispose the GlobalScratchpad.
        /// </summary>
        [ExcludeFromBurstCompatTesting("Accesses managed delegate")]
        internal static void Dispose()
        {
            if (IsInstalled.Data == 0)
            {
                throw new InvalidOperationException("GlobalScratchpad is not initialized.");
            }

            if (JobsUtility.IsExecutingJob)
            {
                throw new InvalidOperationException("You cannot Dispose a Scratchpad from a Job.");
            }

            Pad.Data.Dispose();
            IsInstalled.Data = 0;
        }
    }
}
