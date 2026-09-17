// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using ManagedKernel.Assertions;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
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
    /// data (unlike with <see cref="NativeList{T}"/>, for example).
    ///
    /// **All writing to a stream should be completed before the stream is first read. Do not write to a stream after the first read.**
    /// Violating these rules won't *necessarily* cause any problems, but they are the intended usage pattern.
    ///
    /// Writing is done with <see cref="NativeStream.Writer"/>, and reading is done with <see cref="NativeStream.Reader"/>.
    /// An individual reader or writer cannot be used concurrently across threads: each thread must use its own.
    ///
    /// The data written to an individual buffer can be heterogeneous in type, and the data written
    /// to different buffers of a stream can be entirely different in type, number, and order. Just make sure
    /// that the code reading from a particular buffer knows what to expect to read from it.
    /// </remarks>
    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    public unsafe struct NativeStream : INativeDisposable
    {
        UnsafeStream m_Stream;

        AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeStream>();

        /// <summary>
        /// Initializes and returns an instance of NativeStream.
        /// </summary>
        /// <param name="bufferCount">The number of buffers to give the stream. You usually want
        /// one buffer for each thread that will read or write the stream.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeStream(int bufferCount, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out this, allocator);
            m_Stream.AllocateForEach(bufferCount);
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
        public static JobHandle ScheduleConstruct<T>(out NativeStream stream, NativeList<T> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJobList { List = (UntypedUnsafeList*)bufferCount.GetUnsafeList(), Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used...
        /// - after completing the returned job
        /// - or in other jobs that depend upon the returned job.
        ///
        /// Allocating the buffers in a job can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">An array whose value at index 0 determines the number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        public static JobHandle ScheduleConstruct(out NativeStream stream, NativeArray<int> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJobArray { ForEachCountArray = bufferCount, Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Creates and schedules a job to allocate a new stream.
        /// </summary>
        /// <remarks>The stream can be used...
        /// - after completing the returned job
        /// - or in other jobs that depend upon the returned job.
        ///
        /// Allocating the buffers in a job can be more efficient, particularly for a stream with many buffers.
        /// </remarks>
        /// <param name="stream">Outputs the new stream.</param>
        /// <param name="bufferCount">The number of buffers in the stream.</param>
        /// <param name="dependency">A job handle. The new job will depend upon this handle.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>The handle of the new job.</returns>
        public static JobHandle ScheduleConstruct(out NativeStream stream, NativeReference<int> bufferCount, JobHandle dependency, AllocatorManager.AllocatorHandle allocator)
        {
            AllocateBlock(out stream, allocator);
            var jobData = new ConstructJob { ForEachCount = bufferCount, Container = stream };
            return jobData.Schedule(dependency);
        }

        /// <summary>
        /// Returns true if this stream is empty.
        /// </summary>
        /// <returns>True if this stream is empty or the stream has not been constructed.</returns>
        public readonly bool IsEmpty()
        {
            CheckRead();
            return m_Stream.IsEmpty();
        }

        /// <summary>
        /// Whether this stream has been allocated (and not yet deallocated).
        /// </summary>
        /// <remarks>Does not necessarily reflect whether the buffers of the stream have themselves been allocated.</remarks>
        /// <value>True if this stream has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Stream.IsCreated;
        }

        /// <summary>
        /// The number of buffers in this stream.
        /// </summary>
        /// <value>The number of buffers in this stream.</value>
        public readonly int ForEachCount
        {
            get
            {
                CheckRead();
                return m_Stream.ForEachCount;
            }
        }

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
            CheckRead();
            return m_Stream.Count();
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
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public NativeArray<T> ToNativeArray<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
        {
            CheckRead();
            return m_Stream.ToNativeArray<T>(allocator);
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return;
            }

            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
            m_Stream.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will release all resources (memory and safety handles) of this stream.
        /// </summary>
        /// <param name="inputDeps">A job handle which the newly scheduled job will depend upon.</param>
        /// <returns>The handle of a new job that will release all resources (memory and safety handles) of this stream.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!AtomicSafetyHandle.IsDefaultValue(m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new NativeStreamDisposeJob { Data = new NativeStreamDispose { m_StreamData = m_Stream, m_Safety = m_Safety } }.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_Stream = default;

            return jobHandle;
        }

        internal UnsafeStream GetUnsafeStream() => m_Stream;

        // The three jobs below are internal rather than private so that
        // Unity.Jobs.CollectionsJobReflectionRegistration can register their reflection data by name.
        [BurstCompile]
        internal struct ConstructJobList : IJob
        {
            public NativeStream Container;

            [ReadOnly]
            [NativeDisableUnsafePtrRestriction]
            public UntypedUnsafeList* List;

            public void Execute()
            {
                var forEachCount = List->m_length;
                if (forEachCount > 0)
                {
                    Container.AllocateForEach(forEachCount);
                }
            }
        }

        [BurstCompile]
        internal struct ConstructJobArray : IJob
        {
            public NativeStream Container;

            [ReadOnly]
            public NativeArray<int> ForEachCountArray;

            public void Execute()
            {
                var forEachCount = ForEachCountArray.Length > 0 ? ForEachCountArray[0] : 0;
                if (forEachCount > 0)
                {
                    Container.AllocateForEach(ForEachCountArray[0]);
                }
            }
        }

        [BurstCompile]
        internal struct ConstructJob : IJob
        {
            public NativeStream Container;

            public NativeReference<int>.ReadOnly ForEachCount;

            public void Execute()
            {
                if (ForEachCount.Value > 0)
                {
                    Container.AllocateForEach(ForEachCount.Value);
                }
            }
        }

        static void AllocateBlock(out NativeStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            CollectionHelper.CheckAllocator(allocator);

            UnsafeStream.AllocateBlock(out stream.m_Stream, allocator);

            stream.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId(ref stream.m_Safety, ref s_staticSafetyId.Data, "Unity.Collections.NativeStream");
        }

        void AllocateForEach(int forEachCount)
        {
            CheckForEachCountGreaterThanZero(forEachCount);

            var blockData = (UnsafeStreamBlockData*)m_Stream.m_BlockData.Range.Pointer;
            var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

            Assert.IsTrue(ranges == null);
            Assert.AreEqual(0, blockData->RangeCount);
            Assert.AreNotEqual(0, blockData->BlockCount);

            m_Stream.AllocateForEach(forEachCount);
        }

        /// <summary>
        /// Writes data into a buffer of a <see cref="NativeStream"/>.
        /// </summary>
        /// <remarks>An individual writer can only be used for one buffer of one stream.
        /// Do not create more than one writer for an individual buffer.</remarks>
        [NativeContainer]
        [NativeContainerSupportsMinMaxWriteRestriction]
        [GenerateTestsForBurstCompatibility]
        public unsafe struct Writer
        {
            UnsafeStream.Writer m_Writer;

            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Writer>();
#pragma warning disable CS0414 // warning CS0414: The field 'NativeStream.Writer.m_Length' is assigned but its value is never used
            int m_Length;
#pragma warning restore CS0414
            int m_MinIndex;
            int m_MaxIndex;
            [NativeDisableUnsafePtrRestriction]
            void* m_PassByRefCheck;

            internal Writer(ref NativeStream stream)
            {
                m_Writer = stream.m_Stream.AsWriter();

                m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "Unity.Collections.NativeStream.Writer");
                m_Length = int.MaxValue;
                m_MinIndex = int.MinValue;
                m_MaxIndex = int.MinValue;
                m_PassByRefCheck = null;
            }

            /// <summary>
            /// The number of buffers in the stream of this writer.
            /// </summary>
            /// <value>The number of buffers in the stream of this writer.</value>
            public int ForEachCount
            {
                get
                {
                    AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                    return m_Writer.ForEachCount;
                }
            }

            /// <summary>
            /// For internal use only.
            /// </summary>
            /// <param name="foreEachIndex"></param>
            public void PatchMinMaxRange(int foreEachIndex)
            {
                m_MinIndex = foreEachIndex;
                m_MaxIndex = foreEachIndex;
            }

            /// <summary>
            /// Readies this writer to write to a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this writer. For an individual writer, call this method only once.
            ///
            /// After calling BeginForEachIndex on this writer, passing this writer into functions must be passed by reference.
            ///
            /// When done using this writer, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to write.</param>
            public void BeginForEachIndex(int foreachIndex)
            {
                CheckBeginForEachIndex(foreachIndex);
                m_Writer.BeginForEachIndex(foreachIndex);
            }

            /// <summary>
            /// Readies the buffer written by this writer for reading.
            /// </summary>
            /// <remarks>Must be called before reading the buffer written by this writer.</remarks>
            public void EndForEachIndex()
            {
                CheckEndForEachIndex();
                m_Writer.EndForEachIndex();

                m_Writer.m_ForeachIndex = int.MinValue;
            }

            /// <summary>
            /// Write a value to a buffer.
            /// </summary>
            /// <remarks>The value is written to the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.
            /// </remarks>
            /// <typeparam name="T">The type of value to write.</typeparam>
            /// <param name="value">The value to write.</param>
            /// <exception cref="ArgumentException">Thrown if BeginForEachIndex was not called.</exception>
            /// <exception cref="ArgumentException">Thrown when the NativeStream.Writer instance has been passed by value instead of by reference.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public void Write<T>(T value) where T : unmanaged
            {
                ref T dst = ref Allocate<T>();
                dst = value;
            }

            /// <summary>
            /// Allocate space in a buffer.
            /// </summary>
            /// <remarks>The space is allocated in the buffer which was specified
            /// with <see cref="BeginForEachIndex"/>.
            /// </remarks>
            /// <typeparam name="T">The type of value to allocate space for.</typeparam>
            /// <returns>A reference to the allocation.</returns>
            /// <exception cref="ArgumentException">Thrown if BeginForEachIndex was not called.</exception>
            /// <exception cref="ArgumentException">Thrown when the NativeStream.Writer instance has been passed by value instead of by reference.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public ref T Allocate<T>() where T : unmanaged
            {
                if (UnsafeUtility.IsNativeContainerType<T>())
                    AtomicSafetyHandle.SetNestedContainer(m_Safety, true);
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
            /// <exception cref="ArgumentException">Thrown if BeginForEachIndex was not called.</exception>
            /// <exception cref="ArgumentException">Thrown when the NativeStream.Writer instance has been passed by value instead of by reference.</exception>
            public byte* Allocate(int size)
            {
                CheckAllocateSize(size);
                return m_Writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckBeginForEachIndex(int foreachIndex)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if (m_PassByRefCheck == null)
                {
                    m_PassByRefCheck = UnsafeUtility.AddressOf(ref this);
                }
                var blockData = (UnsafeStreamBlockData*)m_Writer.m_BlockData.Range.Pointer;
                var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

                if (foreachIndex < m_MinIndex || foreachIndex > m_MaxIndex)
                {
                    // When the code is not running through the job system no ParallelForRange patching will occur
                    // We can't grab m_BlockStream->RangeCount on creation of the writer because the RangeCount can be initialized
                    // in a job after creation of the writer
                    if (m_MinIndex == int.MinValue && m_MaxIndex == int.MinValue)
                    {
                        m_MinIndex = 0;

                        m_MaxIndex = blockData->RangeCount - 1;
                    }

                    if (foreachIndex < m_MinIndex || foreachIndex > m_MaxIndex)
                    {
                        throw new ArgumentException($"Index {foreachIndex} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in NativeStream.");
                    }
                }

                if (m_Writer.m_ForeachIndex != int.MinValue)
                {
                    throw new ArgumentException($"BeginForEachIndex must always be balanced by a EndForEachIndex call");
                }

                Assert.IsTrue(foreachIndex >= 0 && foreachIndex < blockData->RangeCount);

                if (0 != ranges[foreachIndex].ElementCount)
                {
                    throw new ArgumentException($"BeginForEachIndex can only be called once for the same index ({foreachIndex}).");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckEndForEachIndex()
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if (m_Writer.m_ForeachIndex == int.MinValue)
                {
                    throw new System.ArgumentException("EndForEachIndex must always be called balanced by a BeginForEachIndex or AppendForEachIndex call");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckAllocateSize(int size)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                if (m_PassByRefCheck != UnsafeUtility.AddressOf(ref this)
                ||  m_Writer.m_ForeachIndex == int.MinValue)
                {
                    throw new ArgumentException("BeginForEachIndex has not been called on NativeStream.Writer, or NativeStream.Writer is not passed by reference.");
                }

                if (size > UnsafeStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
            }
        }

        /// <summary>
        /// Reads data from a buffer of a <see cref="NativeStream"/>.
        /// </summary>
        /// <remarks>An individual reader can only be used for one buffer of one stream.
        /// Do not create more than one reader for an individual buffer.</remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [GenerateTestsForBurstCompatibility]
        public unsafe struct Reader
        {
            UnsafeStream.Reader m_Reader;

            int m_RemainingBlocks;
            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Reader>();

            internal Reader(ref NativeStream stream)
            {
                m_Reader = stream.m_Stream.AsReader();

                m_RemainingBlocks = 0;
                m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "Unity.Collections.NativeStream.Reader");
            }

            /// <summary>
            /// Readies this reader to read a particular buffer of the stream.
            /// </summary>
            /// <remarks>Must be called before using this reader. For an individual reader, call this method only once.
            ///
            /// When done using this reader, you must call <see cref="EndForEachIndex"/>.</remarks>
            /// <param name="foreachIndex">The index of the buffer to read.</param>
            /// <returns>The number of elements left to read from the buffer.</returns>
            public int BeginForEachIndex(int foreachIndex)
            {
                CheckBeginForEachIndex(foreachIndex);

                var remainingItemCount = m_Reader.BeginForEachIndex(foreachIndex);

                var blockData = (UnsafeStreamBlockData*)m_Reader.m_BlockData.Range.Pointer;
                var ranges = (UnsafeStreamRange*)blockData->Ranges.Range.Pointer;

                m_RemainingBlocks = ranges[foreachIndex].NumberOfBlocks;
                if (m_RemainingBlocks == 0)
                {
                    m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + m_Reader.m_LastBlockSize;
                }

                return remainingItemCount;
            }

            /// <summary>
            /// Checks if all data has been read from the buffer.
            /// </summary>
            /// <remarks>If you intentionally don't want to read *all* the data in the buffer, don't call this method.
            /// Otherwise, calling this method is recommended, even though it's not strictly necessary.</remarks>
            /// <exception cref="ArgumentException">Thrown if not all the buffer's data has been read.</exception>
            public void EndForEachIndex()
            {
                m_Reader.EndForEachIndex();
                CheckEndForEachIndex();
            }

            /// <summary>
            /// The number of buffers in the stream of this reader.
            /// </summary>
            /// <value>The number of buffers in the stream of this reader.</value>
            public int ForEachCount
            {
                get
                {
                    CheckRead();
                    return m_Reader.ForEachCount;
                }
            }

            /// <summary>
            /// The number of items not yet read from the buffer.
            /// </summary>
            /// <value>The number of items not yet read from the buffer.</value>
            public int RemainingItemCount => m_Reader.RemainingItemCount;

            /// <summary>
            /// Returns the internal unsafe stream reader.
            /// </summary>
            /// <remarks>Internally, the NativeStream.Reader uses an unsafe stream reader for access to the stream content.</remarks>
            /// <returns>The internal unsafe stream reader.</returns>
            public UnsafeStream.Reader GetUnsafeReader() => m_Reader;

            /// <summary>
            /// Returns a pointer to the next position to read from the buffer. Advances the reader some number of bytes.
            /// </summary>
            /// <param name="size">The number of bytes to advance the reader.</param>
            /// <returns>A pointer to the next position to read from the buffer.</returns>
            /// <exception cref="ArgumentException">Thrown if the reader would advance past the end of the buffer.</exception>
            public byte* ReadUnsafePtr(int size)
            {
                CheckReadSize(size);

                m_Reader.m_RemainingItemCount--;

                byte* ptr = m_Reader.m_CurrentPtr;
                m_Reader.m_CurrentPtr += size;

                if (m_Reader.m_CurrentPtr > m_Reader.m_CurrentBlockEnd)
                {
                    /*
                     * On netfw/mono/il2cpp, doing m_CurrentBlock->Data does not throw, because it knows that it can
                     * just do pointer + 8. On netcore, doing that throws a NullReferenceException. So, first check for
                     * out of bounds accesses, and only then update m_CurrentBlock and m_CurrentPtr.
                     */
                    m_RemainingBlocks--;

                    CheckNotReadingOutOfBounds(size);
                    m_Reader.m_CurrentBlock = m_Reader.m_CurrentBlock->Next;
                    m_Reader.m_CurrentPtr = m_Reader.m_CurrentBlock->Data;

                    if (m_RemainingBlocks <= 0)
                    {
                        m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + m_Reader.m_LastBlockSize;
                    }
                    else
                    {
                        m_Reader.m_CurrentBlockEnd = (byte*)m_Reader.m_CurrentBlock + UnsafeStreamBlockData.AllocationSize;
                    }
                    ptr = m_Reader.m_CurrentPtr;
                    m_Reader.m_CurrentPtr += size;
                }

                return ptr;
            }

            /// <summary>
            /// Reads the next value from the buffer.
            /// </summary>
            /// <remarks>Each read advances the reader to the next item in the buffer.</remarks>
            /// <typeparam name="T">The type of value to read.</typeparam>
            /// <returns>A reference to the next value from the buffer.</returns>
            /// <exception cref="ArgumentException">Thrown if the reader would advance past the end of the buffer.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
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
            /// <exception cref="ArgumentException">Thrown if the read would go past the end of the buffer.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
            public ref T Peek<T>() where T : unmanaged
            {
                int size = sizeof(T);
                CheckReadSize(size);

                return ref m_Reader.Peek<T>();
            }

            /// <summary>
            /// Returns the total number of items in the buffers of the stream.
            /// </summary>
            /// <returns>The total number of items in the buffers of the stream.</returns>
            public int Count()
            {
                CheckRead();
                return m_Reader.Count();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckNotReadingOutOfBounds(int size)
            {
                if (m_RemainingBlocks < 0)
                    throw new System.ArgumentException("Reading out of bounds");

                if (m_RemainingBlocks == 0 && size + sizeof(void*) > m_Reader.m_LastBlockSize)
                    throw new System.ArgumentException("Reading out of bounds");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckReadSize(int size)
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                Assert.IsTrue(size <= UnsafeStreamBlockData.AllocationSize - (sizeof(void*)));
                if (m_Reader.m_RemainingItemCount < 1)
                {
                    throw new ArgumentException("There are no more items left to be read.");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckBeginForEachIndex(int forEachIndex)
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                var blockData = (UnsafeStreamBlockData*)m_Reader.m_BlockData.Range.Pointer;

                if ((uint)forEachIndex >= (uint)blockData->RangeCount)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(forEachIndex), $"foreachIndex: {forEachIndex} must be between 0 and ForEachCount: {blockData->RangeCount}");
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckEndForEachIndex()
            {
                if (m_Reader.m_RemainingItemCount != 0)
                {
                    throw new System.ArgumentException("Not all elements (Count) have been read. If this is intentional, simply skip calling EndForEachIndex();");
                }

                if (m_Reader.m_CurrentBlockEnd != m_Reader.m_CurrentPtr)
                {
                    throw new System.ArgumentException("Not all data (Data Size) has been read. If this is intentional, simply skip calling EndForEachIndex();");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckForEachCountGreaterThanZero(int forEachCount)
        {
            if (forEachCount <= 0)
                throw new ArgumentException("foreachCount must be > 0", "foreachCount");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }
    }

    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct NativeStreamDispose
    {
        public UnsafeStream m_StreamData;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            m_StreamData.Dispose();
        }
    }

    [BurstCompile]
    struct NativeStreamDisposeJob : IJob
    {
        public NativeStreamDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Provides unsafe utility methods for NativeStream.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static unsafe class NativeStreamUnsafeUtility
    {
        /// <summary>
        /// Returns a pointer to this stream's <see cref="NativeStream.ForEachCount">buffer counter</see>.
        /// </summary>
        /// <param name="stream"> The stream.</param>
        /// <returns>A pointer to this stream's <see cref="NativeStream.ForEachCount">buffer counter</see>.</returns>
        [GenerateTestsForBurstCompatibility]
        public static void* GetUnsafeForEachCountPtr(ref this NativeStream stream)
        {
            return stream.GetUnsafeStream().GetForEachCountPtr();
        }
    }
}
