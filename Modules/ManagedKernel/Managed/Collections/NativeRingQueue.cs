// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    /// A fixed-size circular buffer. For single-threaded uses only.
    /// </summary>
    /// <remarks>
    /// This container can't be used in parallel jobs, just on single-thread (for example: main thread, or single IJob).
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(NativeRingQueueDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct NativeRingQueue<T>
        : INativeDisposable
        where T : unmanaged
    {
        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeRingQueue<T>>();
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeRingQueue<T>* m_RingQueue;

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_RingQueue != null && m_RingQueue->IsCreated;
        }

        /// <summary>
        /// Whether the queue is empty.
        /// </summary>
        /// <value>True if the queue is empty or the queue has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_RingQueue == null || m_RingQueue->Length == 0;
        }

        /// <summary>
        /// The number of elements currently in this queue.
        /// </summary>
        /// <value>The number of elements currently in this queue.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return CollectionHelper.AssumePositive(m_RingQueue->Length);
            }
        }

        /// <summary>
        /// The number of elements that fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that fit in the internal buffer.</value>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return CollectionHelper.AssumePositive(m_RingQueue->Capacity);
            }
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = UnsafeRingQueue<T>.MaxCapacity;

        /// <summary>
        /// Initializes and returns an instance of NativeRingQueue.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public NativeRingQueue(int capacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            CollectionHelper.CheckAllocator(allocator);
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<NativeRingQueue<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            m_RingQueue = UnsafeRingQueue<T>.Alloc(allocator);
            *m_RingQueue = new UnsafeRingQueue<T>(capacity, allocator, options);
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
            UnsafeRingQueue<T>.Free(m_RingQueue);
            m_RingQueue = null;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this queue.
        /// </summary>
        /// <param name="inputDeps">The handle of a job which the new job will depend upon.</param>
        /// <returns>The handle of a new job that will dispose this queue. The new job depends upon inputDeps.</returns>
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

            var jobHandle = new NativeRingQueueDisposeJob { Data = new NativeRingQueueDispose { m_QueueData = (UnsafeRingQueue<int>*)m_RingQueue, m_Safety = m_Safety } }.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_RingQueue = null;

            return jobHandle;
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is full.</remarks>
        /// <param name="value">The value to be added.</param>
        /// <returns>True if the value was added.</returns>
        public bool TryEnqueue(T value)
        {
            CheckWrite();
            return m_RingQueue->TryEnqueue(value);
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <exception cref="InvalidOperationException">Thrown if the queue was full.</exception>
        public void Enqueue(T value)
        {
            CheckWrite();
            m_RingQueue->Enqueue(value);
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="item">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryDequeue(out T item)
        {
            CheckRead();
            return m_RingQueue->TryDequeue(out item);
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue was empty.</exception>
        /// <returns>Returns the removed element.</returns>
        public T Dequeue()
        {
            CheckRead();
            return m_RingQueue->Dequeue();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckRead()
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckWrite()
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }
    }

    internal unsafe sealed class NativeRingQueueDebugView<T>
        where T : unmanaged
    {
        UnsafeRingQueue<T>* Data;

        public NativeRingQueueDebugView(NativeRingQueue<T> data)
        {
            Data = data.m_RingQueue;
        }

        public T[] Items
        {
            get
            {
                T[] result = new T[Data->Length];

                var read = Data->m_Read;
                var capacity = Data->m_Capacity;

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data->Ptr[(read + i) % capacity];
                }

                return result;
            }
        }
    }

    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct NativeRingQueueDispose
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeRingQueue<int>* m_QueueData;

        public AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            UnsafeRingQueue<int>.Free(m_QueueData);
        }
    }

    [BurstCompile]
    internal unsafe struct NativeRingQueueDisposeJob : IJob
    {
        public NativeRingQueueDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}
