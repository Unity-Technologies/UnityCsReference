// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections;

namespace Unity.Collections
{
    /// <summary>
    /// An unmanaged queue.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public unsafe struct NativeQueue<T>
        : INativeDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeQueue<T>* m_Queue;

        AtomicSafetyHandle m_Safety;

        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeQueue<T>>();

        /// <summary>
        /// Initializes and returns an instance of NativeQueue.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        public NativeQueue(AllocatorManager.AllocatorHandle allocator)
        {
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.InitNativeContainer<T>(m_Safety);
            CollectionHelper.SetStaticSafetyId<NativeQueue<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            m_Queue = UnsafeQueue<T>.Alloc(allocator);
            *m_Queue = new UnsafeQueue<T>(allocator);

        }

        /// <summary>
        /// Returns true if this queue is empty.
        /// </summary>
        /// <returns>True if this queue has no items or if the queue has not been constructed.</returns>
        public readonly bool IsEmpty()
        {
            if (IsCreated)
            {
                CheckRead();
                return m_Queue->IsEmpty();
            }
            return true;
        }

        /// <summary>
        /// Returns the current number of elements in this queue.
        /// </summary>
        /// <remarks>Note that getting the count requires traversing the queue's internal linked list of blocks.
        /// Where possible, cache this value instead of reading the property repeatedly.</remarks>
        /// <returns>The current number of elements in this queue.</returns>
        public readonly int Count
        {
            get
            {
                CheckRead();
                return m_Queue->Count;
            }
        }

        /// <summary>
        /// Returns the element at the front of this queue without removing it.
        /// </summary>
        /// <returns>The element at the front of this queue.</returns>
        public T Peek()
        {
            CheckRead();
            return m_Queue->Peek();
        }

        /// <summary>
        /// Adds an element at the back of this queue.
        /// </summary>
        /// <param name="value">The value to be enqueued.</param>
        public void Enqueue(T value)
        {
            CheckWrite();
            m_Queue->Enqueue(value);
        }

        /// <summary>
        /// Removes and returns the element at the front of this queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this queue is empty.</exception>
        /// <returns>The element at the front of this queue.</returns>
        public T Dequeue()
        {
            CheckWrite();
            return m_Queue->Dequeue();
        }

        /// <summary>
        /// Removes and outputs the element at the front of this queue.
        /// </summary>
        /// <param name="item">Outputs the removed element.</param>
        /// <returns>True if this queue was not empty.</returns>
        public bool TryDequeue(out T item)
        {
            CheckWrite();
            return m_Queue->TryDequeue(out item);
        }

        /// <summary>
        /// Returns an array containing a copy of this queue's content.
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array containing a copy of this queue's content. The elements are ordered in the same order they were
        /// enqueued, *e.g.* the earliest enqueued element is copied to index 0 of the array.</returns>
        public NativeArray<T> ToArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_Queue->ToArray(allocator);
        }

        /// <summary>
        /// Removes all elements from this queue.
        /// </summary>
        public void Clear()
        {
            CheckWrite();
            m_Queue->Clear();
        }

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Queue != null && m_Queue->IsCreated;
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
            UnsafeQueue<T>.Free(m_Queue);
            m_Queue = null;
        }

        /// <summary>
        /// Creates and schedules a job that releases all resources (memory and safety handles) of this queue.
        /// </summary>
        /// <param name="inputDeps">The dependency for the new job.</param>
        /// <returns>The handle of the new job. The job depends upon `inputDeps` and releases all resources (memory and safety handles) of this queue.</returns>
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

            var jobHandle = new NativeQueueDisposeJob { Data = new NativeQueueDispose { m_QueueData = (UnsafeQueue<int>*)m_Queue, m_Safety = m_Safety } }.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_Queue = null;

            return jobHandle;

        }

        /// <summary>
        /// An enumerator over the values of a container.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<T>
        {
            internal AtomicSafetyHandle m_Safety;

            internal UnsafeQueue<T>.Enumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value.
            /// </summary>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                return m_Enumerator.MoveNext();
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                m_Enumerator.Reset();
            }

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Enumerator.Current;
            }

            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this NativeQueue instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the NativeQueue it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// A read-only alias for the value of a NativeQueue. Does not have its own allocated storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct ReadOnly
            : IEnumerable<T>
        {
            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();
            UnsafeQueue<T>.ReadOnly m_ReadOnly;

            internal ReadOnly(ref NativeQueue<T> data)
            {
                m_Safety = data.m_Safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref m_Safety, ref s_staticSafetyId.Data);
                m_ReadOnly = new UnsafeQueue<T>.ReadOnly(ref *data.m_Queue);
            }

            /// <summary>
            /// Whether this container been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this container has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_ReadOnly.IsCreated;
            }

            /// <summary>
            /// Returns true if this queue is empty.
            /// </summary>
            /// <remarks>Note that getting the count requires traversing the queue's internal linked list of blocks.
            /// Where possible, cache this value instead of reading the property repeatedly.</remarks>
            /// <returns>True if this queue has no items or if the queue has not been constructed.</returns>
            public readonly bool IsEmpty()
            {
                CheckRead();
                return m_ReadOnly.IsEmpty();
            }

            /// <summary>
            /// Returns the current number of elements in this queue.
            /// </summary>
            /// <remarks>Note that getting the count requires traversing the queue's internal linked list of blocks.
            /// Where possible, cache this value instead of reading the property repeatedly.</remarks>
            /// <returns>The current number of elements in this queue.</returns>
            public readonly int Count
            {
                get
                {
                    CheckRead();
                    return m_ReadOnly.Count;
                }
            }

            /// <summary>
            /// The element at an index.
            /// </summary>
            /// <param name="index">An index.</param>
            /// <value>The element at the index.</value>
            /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of bounds.</exception>
            public readonly T this[int index]
            {
                get
                {
                    CheckRead();
                    return m_ReadOnly[index];
                }
            }

            /// <summary>
            /// Returns an enumerator over the items of this container.
            /// </summary>
            /// <returns>An enumerator over the items of this container.</returns>
            public readonly Enumerator GetEnumerator()
            {
                var ash = m_Safety;
                AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(ash);
                AtomicSafetyHandle.UseSecondaryVersion(ref ash);

                return new Enumerator
                {
                    m_Safety = ash,
                    m_Enumerator = m_ReadOnly.GetEnumerator(),
                };
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }
        }

        /// <summary>
        /// Returns a parallel writer for this queue.
        /// </summary>
        /// <returns>A parallel writer for this queue.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref ParallelWriter.s_staticSafetyId.Data);
            writer.unsafeWriter = m_Queue->AsParallelWriter();

            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeQueue.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeQueue.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe struct ParallelWriter
        {
            internal UnsafeQueue<T>.ParallelWriter unsafeWriter;

            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();

            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            public void Enqueue(T value)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                unsafeWriter.Enqueue(value);
            }

            /// <summary>
            /// Adds an element at the back of the queue.
            /// </summary>
            /// <param name="value">The value to be enqueued.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            public void Enqueue(T value, int threadIndexOverride)
            {
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                unsafeWriter.Enqueue(value, threadIndexOverride);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckWrite()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
    }

    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct NativeQueueDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeQueue<int>* m_QueueData;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            UnsafeQueue<int>.Free(m_QueueData);
        }
    }

    [BurstCompile]
    struct NativeQueueDisposeJob : IJob
    {
        public NativeQueueDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}
