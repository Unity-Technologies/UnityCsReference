// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using System.Runtime.InteropServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// A fixed-size circular buffer.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeRingQueueDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeRingQueue<T>
        : INativeDisposable
        where T : unmanaged
    {
        /// <summary>
        /// The internal buffer where the content is stored.
        /// </summary>
        /// <value>The internal buffer where the content is stored.</value>
        [NativeDisableUnsafePtrRestriction]
        public T* Ptr;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        /// <value>The allocator used to create the internal buffer.</value>
        public AllocatorManager.AllocatorHandle Allocator;

        internal readonly int m_Capacity;
        internal int m_Filled;
        internal int m_Write;
        internal int m_Read;

        /// <summary>
        /// Whether the queue is empty.
        /// </summary>
        /// <value>True if the queue is empty or the queue has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Filled == 0;
        }

        /// <summary>
        /// The number of elements currently in this queue.
        /// </summary>
        /// <value>The number of elements currently in this queue.</value>
        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Filled;
        }

        /// <summary>
        /// The number of elements that fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that fit in the internal buffer.</value>
        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Capacity;
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = int.MaxValue;

        /// <summary>
        /// Initializes and returns an instance of UnsafeRingQueue which aliasing an existing buffer.
        /// </summary>
        /// <param name="ptr">An existing buffer to set as the internal buffer.</param>
        /// <param name="capacity">The capacity.</param>
        public UnsafeRingQueue(T* ptr, int capacity)
        {
            Ptr = ptr;
            Allocator = AllocatorManager.None;
            m_Capacity = capacity;
            m_Filled = 0;
            m_Write = 0;
            m_Read = 0;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeRingQueue.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public UnsafeRingQueue(int capacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocator = allocator;
            m_Capacity = capacity;
            m_Filled = 0;
            m_Write = 0;
            m_Read = 0;
            long sizeInBytes = (long)capacity * sizeof(T);
            Ptr = (T*)Memory.Unmanaged.Allocate(sizeInBytes, 16, allocator);

            if (options == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(Ptr, sizeInBytes);
            }
        }

        internal static UnsafeRingQueue<T>* Alloc(AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeRingQueue<T>* data = (UnsafeRingQueue<T>*)Memory.Unmanaged.Allocate(sizeof(UnsafeRingQueue<T>), UnsafeUtility.AlignOf<UnsafeRingQueue<T>>(), allocator);
            return data;
        }

        internal static void Free(UnsafeRingQueue<T>* data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("UnsafeRingQueue has yet to be created or has been destroyed!");
            }
            var allocator = data->Allocator;
            data->Dispose();
            Memory.Unmanaged.Free(data, allocator);
        }

        /// <summary>
        /// Whether this queue has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this queue has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr != null;
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                Memory.Unmanaged.Free(Ptr, Allocator);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this queue.
        /// </summary>
        /// <param name="inputDeps">The handle of a job which the new job will depend upon.</param>
        /// <returns>The handle of a new job that will dispose this queue. The new job depends upon inputDeps.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                var jobHandle = new UnsafeDisposeJob { Ptr = Ptr, Allocator = Allocator }.Schedule(inputDeps);

                Ptr = null;
                Allocator = AllocatorManager.Invalid;

                return jobHandle;
            }

            Ptr = null;

            return inputDeps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryEnqueueInternal(T value)
        {
            if (m_Filled == m_Capacity)
                return false;
            Ptr[m_Write] = value;
            m_Write++;
            if (m_Write == m_Capacity)
                m_Write = 0;
            m_Filled++;
            return true;
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is full.</remarks>
        /// <param name="value">The value to be added.</param>
        /// <returns>True if the value was added.</returns>
        public bool TryEnqueue(T value)
        {
            return TryEnqueueInternal(value);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void ThrowQueueFull()
        {
            throw new InvalidOperationException("Trying to enqueue into full queue.");
        }

        /// <summary>
        /// Adds an element at the front of the queue.
        /// </summary>
        /// <param name="value">The value to be added.</param>
        /// <exception cref="InvalidOperationException">Thrown if the queue was full.</exception>
        public void Enqueue(T value)
        {
            if (!TryEnqueueInternal(value))
            {
                ThrowQueueFull();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TryDequeueInternal(out T item)
        {
            item = Ptr[m_Read];
            if (m_Filled == 0)
                return false;
            m_Read = m_Read + 1;
            if (m_Read == m_Capacity)
                m_Read = 0;
            m_Filled--;
            return true;
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <remarks>Does nothing if the queue is empty.</remarks>
        /// <param name="item">Outputs the element removed.</param>
        /// <returns>True if an element was removed.</returns>
        public bool TryDequeue(out T item)
        {
            return TryDequeueInternal(out item);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void ThrowQueueEmpty()
        {
            throw new InvalidOperationException("Trying to dequeue from an empty queue");
        }

        /// <summary>
        /// Removes the element from the end of the queue.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the queue was empty.</exception>
        /// <returns>Returns the removed element.</returns>
        public T Dequeue()
        {
            if (!TryDequeueInternal(out T item))
            {
                ThrowQueueEmpty();
            }

            return item;
        }
    }

    internal sealed class UnsafeRingQueueDebugView<T>
        where T : unmanaged
    {
        UnsafeRingQueue<T> Data;

        public UnsafeRingQueueDebugView(UnsafeRingQueue<T> data)
        {
            Data = data;
        }

        public unsafe T[] Items
        {
            get
            {
                T[] result = new T[Data.Length];

                var read = Data.m_Read;
                var capacity = Data.m_Capacity;

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data.Ptr[(read + i) % capacity];
                }

                return result;
            }
        }
    }
}
