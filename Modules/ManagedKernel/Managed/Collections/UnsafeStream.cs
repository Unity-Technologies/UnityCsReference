// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using ManagedKernel.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeStreamBlock
    {
        internal UnsafeStreamBlock* Next;
        internal fixed byte Data[1];
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeStreamRange
    {
        internal UnsafeStreamBlock* Block;
        internal int OffsetInFirstBlock;
        internal int ElementCount;

        /// One byte past the end of the last byte written
        internal int LastOffset;
        internal int NumberOfBlocks;
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeStreamBlockData
    {
        internal const int AllocationSize = 4 * 1024;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal UnsafeStreamBlock** Blocks;
        internal int BlockCount;

        internal AllocatorManager.Block Ranges;
        internal int RangeCount;

        internal UnsafeStreamBlock* Allocate(UnsafeStreamBlock* oldBlock, int threadIndex)
        {
            Assert.IsTrue(threadIndex < BlockCount && threadIndex >= 0);

            UnsafeStreamBlock* block = (UnsafeStreamBlock*)Memory.Unmanaged.Array.Resize(null, 0, AllocationSize, Allocator, 1, 16);
            block->Next = null;

            if (oldBlock == null)
            {
                // Append our new block in front of the previous head.
                block->Next = Blocks[threadIndex];
                Blocks[threadIndex] = block;
            }
            else
            {
                block->Next = oldBlock->Next;
                oldBlock->Next = block;
            }

            return block;
        }

        internal void Free(UnsafeStreamBlock* oldBlock)
        {
            Memory.Unmanaged.Array.Resize(oldBlock, AllocationSize, 0, Allocator, 1, 16);
        }
    }

    /// <summary>
    /// A set of untyped, append-only buffers. Allows for concurrent reading and concurrent writing without synchronization.
    /// </summary>
    /// <remarks>
    /// As long as each individual buffer is written in one thread and read in one thread, multiple
    /// threads can read and write the stream concurrently, *e.g.*
    /// while thread *A* reads from buffer *X* of a stream, thread *B* can read from
    /// buffer *Y* of the same stream.
    ///
    /// Each buffer is stored as a chain of blocks. When a write exceeds a buffer's current capacity, another block
    /// is allocated and added to the end of the chain. Effectively, expanding the buffer never requires copying the existing
    /// data (unlike, for example, with <see cref="NativeList{T}"/>).
    ///
    /// **All writing to a stream should be completed before the stream is first read. Do not write to a stream after the first read.**
    ///
    /// Writing is done with <see cref="NativeStream.Writer"/>, and reading is done with <see cref="NativeStream.Reader"/>.
    /// An individual reader or writer cannot be used concurrently across threads. Each thread must use its own.
    ///
    /// The data written to an individual buffer can be heterogeneous in type, and the data written
    /// to different buffers of a stream can be entirely different in type, number, and order. Just make sure
    /// that the code reading from a particular buffer knows what to expect to read from it.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeStream
        : INativeDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal AllocatorManager.Block m_BlockData;

        /// <summary>
        /// Initializes and returns an instance of UnsafeStream.
        /// </summary>
        /// <param name="bufferCount">The number of buffers to give the stream. You usually want
        /// one buffer for each thread that will read or write the stream.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeStream(int bufferCount, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out this, allocator);
            AllocateForEach(bufferCount);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used on the main thread after completing the returned job or used in other jobs that depend upon the returned job.
        ///
        /// Using a job to allocate the buffers can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <typeparam name="T">Ignored.</typeparam>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">A list whose length determines the number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static JobHandle ScheduleConstruct<T>(out UnsafeStream stream, NativeList<T> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJobList { List = (UntypedUnsafeList*)bufferCount.GetUnsafeList(), Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used on the main thread after completing the returned job or used in other jobs that depend upon the returned job.
        ///
        /// Allocating the buffers in a job can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">An array whose value at index 0 determines the number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        public static JobHandle ScheduleConstruct(out UnsafeStream stream, NativeArray<int> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJob { Length = bufferCount, Container = stream };
            return jobData.Schedule(dependency);
        }

        internal static void AllocateBlock(out UnsafeStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            int maxThreadCount = JobsUtility.ThreadIndexCount;

            int blockCount = maxThreadCount;

            int allocationSize = sizeof(UnsafeStreamBlockData) + sizeof(UnsafeStreamBlock*) * blockCount;

            AllocatorManager.Block blk = AllocatorManager.AllocateBlock(ref allocator, allocationSize, 16, 1);
            UnsafeUtility.MemClear( (void*)blk.Range.Pointer, blk.AllocatedBytes);

            stream.m_BlockData = blk;

            var blockData = (UnsafeStreamBlockData*)blk.Range.Pointer;
            blockData->Allocator = allocator;
            blockData->BlockCount = blockCount;
            blockData->Blocks = (UnsafeStreamBlock**)(blk.Range.Pointer + sizeof(UnsafeStreamBlockData));

            blockData->Ranges = default;
            blockData->RangeCount = 0;
        }

        internal void AllocateForEach(int forEachCount)
        {
            var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
            blockData->Ranges = AllocatorManager.AllocateBlock(ref m_BlockData.Range.Allocator, sizeof(UnsafeStreamRange), 16, forEachCount);
            blockData->RangeCount = forEachCount;
            UnsafeUtility.MemClear((void*)blockData->Ranges.Range.Pointer, blockData->Ranges.AllocatedBytes);
        }

        /// <summary>
        /// Returns true if this stream is empty.
        /// </summary>
        /// <returns>True if this stream is empty or the stream has not been constructed.</returns>
        public readonly bool IsEmpty()
        {
            if (!IsCreated)
            {
                return true;
            }

            var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
            var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

            for (int i = 0; i != blockData->RangeCount; i++)
            {
                if (ranges[i].ElementCount > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether this stream has been allocated (and not yet deallocated).
        /// </summary>
        /// <remarks>Does not necessarily reflect whether the buffers of the stream have themselves been allocated.</remarks>
        /// <value>True if this stream has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_BlockData.Range.Pointer != IntPtr.Zero;
        }

        /// <summary>
        /// The number of buffers in this stream.
        /// </summary>
        /// <value>The number of buffers in this stream.</value>
        public readonly int ForEachCount => ((UnsafeStreamBlockData*)m_BlockData.Range.Pointer)->RangeCount;

        /// <summary>
        /// Returns a reader of this stream.
        /// </summary>
        /// <returns>A reader of this stream.</returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        /// <summary>
        /// Returns a writer of this stream.
        /// </summary>
        /// <returns>A writer of this stream.</returns>
        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        /// <summary>
        /// Returns the total number of items in the buffers of this stream.
        /// </summary>
        /// <remarks>Each <see cref="Writer.Write{T}"/> and <see cref="Writer.Allocate"/> call increments this number.</remarks>
        /// <returns>The total number of items in the buffers of this stream.</returns>
        public int Count()
        {
            int itemCount = 0;

            var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
            var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

            for (int i = 0; i != blockData->RangeCount; i++)
            {
                itemCount += ranges[i].ElementCount;
            }

            return itemCount;
        }

        /// <summary>
        /// Returns a new NativeArray copy of this stream's data.
        /// </summary>
        /// <remarks>The length of the array will equal the count of this stream.
        ///
        /// Each buffer of this stream is copied to the array, one after the other.
        /// </remarks>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A new NativeArray copy of this stream's data.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public NativeArray<T> ToNativeArray<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
        {
            var array = CollectionHelper.CreateNativeArray<T>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            var reader = AsReader();

            int offset = 0;
            for (int i = 0; i != reader.ForEachCount; i++)
            {
                reader.BeginForEachIndex(i);
                int rangeItemCount = reader.RemainingItemCount;
                for (int j = 0; j < rangeItemCount; ++j)
                {
                    array[offset] = reader.Read<T>();
                    offset++;
                }
                reader.EndForEachIndex();
            }

            return array;
        }

        internal readonly void* GetForEachCountPtr() => UnsafeUtility.AddressOf(ref ((UnsafeStreamBlockData*)m_BlockData.Range.Pointer)->RangeCount);

        void Deallocate()
        {
            if (!IsCreated)
            {
                return;
            }

            var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;

            for (int i = 0; i != blockData->BlockCount; i++)
            {
                UnsafeStreamBlock* block = blockData->Blocks[i];
                while (block != null)
                {
                    UnsafeStreamBlock* next = block->Next;
                    blockData->Free(block);
                    block = next;
                }
            }

            if (blockData->RangeCount > 0)
            {
                blockData->Ranges.Dispose();
            }

            m_BlockData.Dispose();
            m_BlockData = default;
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            Deallocate();
        }

        /// <summary>
        /// Creates and schedules a job that will release all resources (memory and safety handles) of this stream.
        /// </summary>
        /// <param name="inputDeps">A job handle which the newly scheduled job will depend upon.</param>
        /// <returns>The handle of a new job that will release all resources (memory and safety handles) of this stream.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);

            m_BlockData = default;

            return jobHandle;
        }

        // The three jobs below are internal rather than private so that
        // Unity.Jobs.CollectionsJobReflectionRegistration can register their reflection data by name.
        [BurstCompile]
        internal struct DisposeJob : IJob
        {
            public UnsafeStream Container;

            public void Execute()
            {
                Container.Deallocate();
            }
        }

        [BurstCompile]
        internal struct ConstructJobList : IJob
        {
            public UnsafeStream Container;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public UntypedUnsafeList* List;

            public void Execute()
            {
                if (List->m_length > 0)
                {
                    Container.AllocateForEach(List->m_length);
                }
            }
        }

        [BurstCompile]
        internal struct ConstructJob : IJob
        {
            public UnsafeStream Container;

            [ReadOnly]
            public NativeArray<int> Length;

            public void Execute()
            {
                var forEachCount = Length.Length > 0 ? Length[0] : 0;
                if (forEachCount > 0)
                {
                    Container.AllocateForEach(forEachCount);
                }
            }
        }

        /// <summary>
        /// Writes data into a buffer of an <see cref="UnsafeStream"/>.
        /// </summary>
        /// <remarks>An individual writer can only be used for one buffer of one stream.
        /// Do not create more than one writer for an individual buffer.</remarks>
        [GenerateTestsForBurstCompatibility]
        public unsafe struct Writer
        {
            [NativeDisableUnsafePtrRestriction]
            internal AllocatorManager.Block m_BlockData;

            [NativeDisableUnsafePtrRestriction]
            UnsafeStreamBlock* m_CurrentBlock;

            [NativeDisableUnsafePtrRestriction]
            byte* m_CurrentPtr;

            [NativeDisableUnsafePtrRestriction]
            byte* m_CurrentBlockEnd;

            internal int m_ForeachIndex;
            int m_ElementCount;

            [NativeDisableUnsafePtrRestriction]
            UnsafeStreamBlock* m_FirstBlock;

            int m_FirstOffset;
            int m_NumberOfBlocks;

            [NativeSetThreadIndex]
            int m_ThreadIndex;

            internal Writer(ref UnsafeStream stream)
            {
                m_BlockData = stream.m_BlockData;
                m_ForeachIndex = int.MinValue;
                m_ElementCount = -1;
                m_CurrentBlock = null;
                m_CurrentBlockEnd = null;
                m_CurrentPtr = null;
                m_FirstBlock = null;
                m_NumberOfBlocks = 0;
                m_FirstOffset = 0;
                m_ThreadIndex = 0;
            }

            /// <summary>
            /// The number of buffers in the stream of this writer.
            /// </summary>
            /// <value>The number of buffers in the stream of this writer.</value>
            public int ForEachCount => ((UnsafeStreamBlockData*)m_BlockData.Range.Pointer)->RangeCount;

            /// <summary>
            /// Readies this writer to write to a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this writer. For an individual writer, call this method only once.
            ///
            /// When done using this writer, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to write.</param>
            public void BeginForEachIndex(int foreachIndex)
            {
                m_ForeachIndex = foreachIndex;
                m_ElementCount = 0;
                m_NumberOfBlocks = 0;
                m_FirstBlock = m_CurrentBlock;
                m_FirstOffset = (int)(m_CurrentPtr - (byte*)m_CurrentBlock);
            }

            /// <summary>
            /// Readies the buffer written by this writer for reading.
            /// </summary>
            /// <remarks>Must be called before reading the buffer written by this writer.</remarks>
            public void EndForEachIndex()
            {
                var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
                var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

                ranges[m_ForeachIndex].ElementCount = m_ElementCount;
                ranges[m_ForeachIndex].OffsetInFirstBlock = m_FirstOffset;
                ranges[m_ForeachIndex].Block = m_FirstBlock;

                ranges[m_ForeachIndex].LastOffset = (int)(m_CurrentPtr - (byte*)m_CurrentBlock);
                ranges[m_ForeachIndex].NumberOfBlocks = m_NumberOfBlocks;
            }

            /// <summary>
            /// Write a value to a buffer.
            /// </summary>
            /// <remarks>The value is written to the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <typeparam name="T">The type of value to write.</typeparam>
            /// <param name="value">The value to write.</param>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void Write<T>(T value) where T : unmanaged
            {
                ref T dst = ref Allocate<T>();
                dst = value;
            }

            /// <summary>
            /// Allocate space in a buffer.
            /// </summary>
            /// <remarks>The space is allocated in the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <typeparam name="T">The type of value to allocate space for.</typeparam>
            /// <returns>A reference to the allocation.</returns>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public ref T Allocate<T>() where T : unmanaged
            {
                int size = sizeof(T);
                return ref UnsafeUtility.AsRef<T>(Allocate(size));
            }

            /// <summary>
            /// Allocate space in a buffer.
            /// </summary>
            /// <remarks>The space is allocated in the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.</remarks>
            /// <param name="size">The number of bytes to allocate.</param>
            /// <returns>The allocation.</returns>
            public byte* Allocate(int size)
            {
                byte* ptr = m_CurrentPtr;
                m_CurrentPtr += size;

                if (m_CurrentPtr > m_CurrentBlockEnd)
                {
                    UnsafeStreamBlock* oldBlock = m_CurrentBlock;

                    var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;

                    m_CurrentBlock = blockData->Allocate(oldBlock, m_ThreadIndex);
                    m_CurrentPtr = m_CurrentBlock->Data;

                    if (m_FirstBlock == null)
                    {
                        m_FirstOffset = (int)(m_CurrentPtr - (byte*)m_CurrentBlock);
                        m_FirstBlock = m_CurrentBlock;
                    }
                    else
                    {
                        m_NumberOfBlocks++;
                    }

                    m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;
                    ptr = m_CurrentPtr;
                    m_CurrentPtr += size;
                }

                m_ElementCount++;

                return ptr;
            }
        }

        /// <summary>
        /// State of a <see cref="Reader"/>.
        /// </summary>
        public struct ReaderState
        {
            internal UnsafeStreamBlock* m_CurrentBlock;
            internal byte* m_CurrentPtr;
            internal byte* m_CurrentBlockEnd;
            internal int m_RemainingItemCount;
            internal int m_LastBlockSize;
        }

        /// <summary>
        /// Reads data from a buffer of an <see cref="UnsafeStream"/>.
        /// </summary>
        /// <remarks>An individual reader can only be used for one buffer of one stream.
        /// Do not create more than one reader for an individual buffer.</remarks>
        [GenerateTestsForBurstCompatibility]
        public unsafe struct Reader
        {
            [NativeDisableUnsafePtrRestriction]
            internal AllocatorManager.Block m_BlockData;

            [NativeDisableUnsafePtrRestriction]
            internal UnsafeStreamBlock* m_CurrentBlock;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentPtr;

            [NativeDisableUnsafePtrRestriction]
            internal byte* m_CurrentBlockEnd;

            internal int m_RemainingItemCount;
            internal int m_LastBlockSize;

            internal Reader(ref UnsafeStream stream)
            {
                m_BlockData = stream.m_BlockData;
                m_CurrentBlock = null;
                m_CurrentPtr = null;
                m_CurrentBlockEnd = null;
                m_RemainingItemCount = 0;
                m_LastBlockSize = 0;
            }

            /// <summary>
            /// Gets or sets the state of this reader.
            /// </summary>
            /// <remarks> Setting a state obtained from a different reader leads to undefined behavior. </remarks>
            public ReaderState State
            {
                get => new()
                    {
                        m_CurrentBlock = m_CurrentBlock,
                        m_CurrentPtr = m_CurrentPtr,
                        m_CurrentBlockEnd = m_CurrentBlockEnd,
                        m_RemainingItemCount = m_RemainingItemCount,
                        m_LastBlockSize = m_LastBlockSize
                    };
                set
                {
                    m_CurrentBlock = value.m_CurrentBlock;
                    m_CurrentPtr = value.m_CurrentPtr;
                    m_CurrentBlockEnd = value.m_CurrentBlockEnd;
                    m_RemainingItemCount = value.m_RemainingItemCount;
                    m_LastBlockSize = value.m_LastBlockSize;
                }
            }


            /// <summary>
            /// Readies this reader to read a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this reader. For an individual reader, call this method only once.
            ///
            /// When done using this reader, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to read.</param>
            /// <returns>The number of remaining elements to read from the buffer.</returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
                var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

                m_RemainingItemCount = ranges[foreachIndex].ElementCount;
                m_LastBlockSize = ranges[foreachIndex].LastOffset;

                m_CurrentBlock = ranges[foreachIndex].Block;
                m_CurrentPtr = (byte*)m_CurrentBlock + ranges[foreachIndex].OffsetInFirstBlock;
                m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;

                return m_RemainingItemCount;
            }

            /// <summary>
            /// Does nothing.
            /// </summary>
            /// <remarks>Included only for consistency with <see cref="NativeStream"/>.</remarks>
            public void EndForEachIndex()
            {
            }

            /// <summary>
            /// The number of buffers in the stream of this reader.
            /// </summary>
            /// <value>The number of buffers in the stream of this reader.</value>
            public int ForEachCount => ((UnsafeStreamBlockData*)m_BlockData.Range.Pointer)->RangeCount;

            /// <summary>
            /// The number of items not yet read from the buffer.
            /// </summary>
            /// <value>The number of items not yet read from the buffer.</value>
            public int RemainingItemCount => m_RemainingItemCount;

            /// <summary>
            /// Returns a pointer to the next position to read from the buffer. Advances the reader some number of bytes.
            /// </summary>
            /// <param name="size">The number of bytes to advance the reader.</param>
            /// <returns>A pointer to the next position to read from the buffer.</returns>
            /// <exception cref="System.ArgumentException">Thrown if the reader has been advanced past the end of the buffer.</exception>
            public byte* ReadUnsafePtr(int size)
            {
                m_RemainingItemCount--;

                byte* ptr = m_CurrentPtr;
                m_CurrentPtr += size;

                if (m_CurrentPtr > m_CurrentBlockEnd)
                {
                    m_CurrentBlock = m_CurrentBlock->Next;
                    m_CurrentPtr = m_CurrentBlock->Data;

                    m_CurrentBlockEnd = (byte*)m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;

                    ptr = m_CurrentPtr;
                    m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary>
            /// Reads the next value from the buffer.
            /// </summary>
            /// <remarks>Each read advances the reader to the next item in the buffer.</remarks>
            /// <typeparam name="T">The type of value to read.</typeparam>
            /// <returns>A reference to the next value from the buffer.</returns>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public ref T Read<T>() where T : unmanaged
            {
                int size = sizeof(T);
                return ref UnsafeUtility.AsRef<T>(ReadUnsafePtr(size));
            }

            /// <summary>
            /// Reads the next value from the buffer. Does not advance the reader.
            /// </summary>
            /// <typeparam name="T">The type of value to read.</typeparam>
            /// <returns>A reference to the next value from the buffer.</returns>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public ref T Peek<T>() where T : unmanaged
            {
                int size = sizeof(T);

                byte* ptr = m_CurrentPtr;
                if (ptr + size > m_CurrentBlockEnd)
                {
                    ptr = m_CurrentBlock->Next->Data;
                }

                return ref UnsafeUtility.AsRef<T>(ptr);
            }

            /// <summary>
            /// Returns the total number of items in the buffers of the stream.
            /// </summary>
            /// <returns>The total number of items in the buffers of the stream.</returns>
            public int Count()
            {
                var blockData = (UnsafeStreamBlockData*)m_BlockData.Range.Pointer;
                var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

                int itemCount = 0;
                for (int i = 0; i != blockData->RangeCount; i++)
                {
                    itemCount += ranges[i].ElementCount;
                }

                return itemCount;
            }
        }
    }
}
