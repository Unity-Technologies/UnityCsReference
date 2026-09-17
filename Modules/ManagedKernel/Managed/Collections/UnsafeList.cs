// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{

    [BurstCompile]
    internal unsafe struct UnsafeDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            AllocatorManager.Free(Allocator, Ptr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct UntypedUnsafeList
    {
#pragma warning disable 169
        // <WARNING>
        // 'Header' of this struct must binary match `UntypedUnsafeList`, `UnsafeList`, `UnsafePtrList`, and `NativeArray` struct.
        [NativeDisableUnsafePtrRestriction]
        internal readonly void* Ptr;
        internal readonly int m_length;
        internal readonly int m_capacity;
        internal readonly AllocatorManager.AllocatorHandle Allocator;
        internal readonly int padding;
#pragma warning restore 169
    }

    /// <summary>
    /// An unmanaged, resizable list.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeListTDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeList<T>
        : INativeDisposable
        , INativeList<T>
        , IEnumerable<T> // Used by collection initializers.
        where T : unmanaged
    {
        // <WARNING>
        // 'Header' of this struct must binary match `UntypedUnsafeList`, `UnsafeList`, `UnsafePtrList`, and `NativeArray` struct.
        // Fields must match UntypedUnsafeList structure, please don't reorder and don't insert anything in between first 4 fields

        /// <summary>
        /// The internal buffer of this list.
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public T* Ptr;

        /// <summary>
        /// The number of elements.
        /// </summary>
        public int m_length;

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        public int m_capacity;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        public AllocatorManager.AllocatorHandle Allocator;

        readonly int padding;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => CollectionHelper.AssumePositive(m_length);

            set
            {
                if (value > Capacity)
                {
                    Resize(value);
                }
                else
                {
                    m_length = value;
                }
            }
        }

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that can fit in the internal buffer.</value>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => CollectionHelper.AssumePositive(m_capacity);
            set => SetCapacity(value);
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = int.MaxValue;

        /// <summary>
        /// The element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <value>The element at the index.</value>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CollectionHelper.CheckIndexInRange(index, m_length);
                return Ptr[CollectionHelper.AssumePositive(index)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CollectionHelper.CheckIndexInRange(index, m_length);
                Ptr[CollectionHelper.AssumePositive(index)] = value;
            }
        }

        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ElementAt(int index)
        {
            CollectionHelper.CheckIndexInRange(index, m_length);
            return ref Ptr[CollectionHelper.AssumePositive(index)];
        }

        /// <summary>
        /// Get a pointer to the data.
        /// </summary>
        /// <remarks>When container has safety checks enabled it performs a job safety check for write access.</remarks>
        /// <returns>A pointer to the data.</returns>
        [WriteAccessRequired]
        public T* GetUnsafePtr()
        {
            return Ptr;
        }

        /// <summary>
        /// Get a pointer to the data.
        /// </summary>
        /// <remarks>When container has safety checks enabled it performs a job safety check for read-only access.</remarks>
        /// <returns>A pointer to the data.</returns>
        public readonly T* GetUnsafeReadOnlyPtr()
        {
            return Ptr;
        }

        /// <summary>
        /// Implicit cast to Span&lt;T&gt;.
        /// </summary>
        /// <param name="container">Container to cast to Span&lt;T&gt;.</param>
        /// <returns>Span&lt;T&gt; view of this container.</returns>
        [WriteAccessRequired]
        public static implicit operator Span<T>(in UnsafeList<T> container)
        {
            return container.AsSpan();
        }

        /// <summary>
        /// Implicit cast to ReadOnlySpan&lt;T&gt;.
        /// </summary>
        /// <param name="container">Container to cast to ReadOnlySpan&lt;T&gt;.</param>
        /// <returns>ReadOnlySpan&lt;T&gt; view of this container.</returns>
        public static implicit operator ReadOnlySpan<T>(in UnsafeList<T> container)
        {
            return container.AsReadOnlySpan();
        }

        /// <summary>
        /// Returns Span&lt;T&gt; view of this container.
        /// </summary>
        /// <returns>Span&lt;T&gt; view of this container.</returns>
        [WriteAccessRequired]
        public Span<T> AsSpan()
        {
            return new Span<T>(GetUnsafePtr(), Length);
        }

        /// <summary>
        /// Returns ReadOnlySpan&lt;T&gt; view of this container.
        /// </summary>
        /// <returns>ReadOnlySpan&lt;T&gt; view of this container.</returns>
        public readonly ReadOnlySpan<T> AsReadOnlySpan()
        {
            return new ReadOnlySpan<T>(GetUnsafeReadOnlyPtr(), Length);
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeList.
        /// </summary>
        /// <param name="ptr">An existing byte array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        public UnsafeList(T* ptr, int length) : this()
        {
            Ptr = ptr;
            m_length = length;
            m_capacity = length;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafeList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public UnsafeList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
            Allocator = allocator;
            padding = 0;

            SetCapacity(math.max(initialCapacity, 1));

            if (options == NativeArrayOptions.ClearMemory && Ptr != null)
            {
                var sizeOf = sizeof(T);
                UnsafeUtility.MemClear(Ptr, (long)m_capacity * sizeOf);
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        internal static UnsafeList<T>* Create<U>(int initialCapacity, ref U allocator, NativeArrayOptions options) where U : unmanaged, AllocatorManager.IAllocator
        {
            UnsafeList<T>* listData = allocator.Allocate(default(UnsafeList<T>), 1);
            *listData = new UnsafeList<T>(initialCapacity, allocator.Handle, options);
            return listData;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        internal static void Destroy<U>(UnsafeList<T>* listData, ref U allocator) where U : unmanaged, AllocatorManager.IAllocator
        {
            CheckNull(listData);
            listData->Dispose(ref allocator);
            allocator.Free(listData, sizeof(UnsafeList<T>), UnsafeUtility.AlignOf<UnsafeList<T>>(), 1);
        }

        /// <summary>
        /// Returns a new list.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafeList<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafeList<T>* listData = AllocatorManager.Allocate<UnsafeList<T>>(allocator);
            *listData = new UnsafeList<T>(initialCapacity, allocator, options);

            return listData;
        }

        /// <summary>
        /// Destroys the list.
        /// </summary>
        /// <param name="listData">The list to destroy.</param>
        public static void Destroy(UnsafeList<T>* listData)
        {
            CheckNull(listData);
            var allocator = listData->Allocator;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Whether the list is empty.
        /// </summary>
        /// <value>True if the list is empty or the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || m_length == 0;
        }

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr != null;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        internal void Dispose<U>(ref U allocator) where U : unmanaged, AllocatorManager.IAllocator
        {
            allocator.Free(Ptr, m_capacity);
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
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

            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                AllocatorManager.Free(Allocator, Ptr, m_capacity);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            m_length = 0;
            m_capacity = 0;
        }

        /// <summary>
        /// Creates and schedules a job that frees the memory of this list.
        /// </summary>
        /// <param name="inputDeps">The dependency for the new job.</param>
        /// <returns>The handle of the new job. The job depends upon `inputDeps` and frees the memory of this list.</returns>
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

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            m_length = 0;
        }

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            long oldLength = m_length;

            if (length > Capacity)
            {
                SetCapacity(length);
            }

            m_length = length;

            if (options == NativeArrayOptions.ClearMemory && oldLength < length)
            {
                long num = length - oldLength;
                byte* ptr = (byte*)Ptr;
                var sizeOf = sizeof(T);
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        void ResizeExact<U>(ref U allocator, int newCapacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            newCapacity = math.max(0, newCapacity);

            CollectionHelper.CheckAllocator(Allocator);
            T* newPointer = null;

            var alignOf = UnsafeUtility.AlignOf<T>();
            var sizeOf = sizeof(T);

            if (newCapacity > 0)
            {
                newPointer = (T*)allocator.Allocate(sizeOf, alignOf, newCapacity);

                if (Ptr != null && m_capacity > 0)
                {
                    var itemsToCopy = math.min(newCapacity, Capacity);
                    var bytesToCopy = (long)itemsToCopy * (long)sizeOf;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            allocator.Free(Ptr, Capacity);

            Ptr = newPointer;
            m_capacity = newCapacity;
            m_length = math.min(m_length, newCapacity);
        }

        void ResizeExact(int capacity)
        {
            ResizeExact(ref Allocator, capacity);
        }

        void SetCapacity<U>(ref U allocator, int capacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            CollectionHelper.CheckCapacityInRange(capacity, Length);

            var sizeOf = sizeof(T);
            var newCapacity = math.max(capacity, CollectionHelper.CacheLineSize / sizeOf);
            long ceilpow2 = math.ceilpow2((long)newCapacity);
            newCapacity = (int)math.min(MaxCapacity, ceilpow2);

            if (newCapacity == Capacity)
            {
                return;
            }

            ResizeExact(ref allocator, newCapacity);
        }

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity)
        {
            SetCapacity(ref Allocator, capacity);
        }

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess()
        {
            if (Capacity != m_length)
            {
                ResizeExact(m_length);
            }
        }

        /// <summary>
        /// Adds an element to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by 1. Never increases the capacity.
        /// </remarks>
        /// <param name="value">The value to add to the end of the list.</param>
        /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNoResize(T value)
        {
            CheckNoResizeHasEnoughCapacity(1);
            UnsafeUtility.WriteArrayElement(Ptr, m_length, value);
            m_length += 1;
        }

        /// <summary>
        /// Copies elements from a buffer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by `count`. Never increases the capacity.
        /// </remarks>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void* ptr, int count)
        {
            CheckNoResizeHasEnoughCapacity(count);
            var sizeOf = (long)sizeof(T);
            void* dst = (byte*)Ptr + m_length * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, (long)count * sizeOf);
            m_length += count;
        }

        /// <summary>
        /// Copies elements from a ReadOnlySpan to the end of this list.
        /// </summary>
        /// <param name="roSpan">ReadOnlySpan to copy from.</param>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(ReadOnlySpan<T> roSpan)
        {
            var count = roSpan.Length;
            if (count > 0)
            {
                fixed (T* ptr = &roSpan[0])
                {
                    AddRangeNoResize(ptr, count);
                }
            }
        }

        /// <summary>
        /// Copies the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Never increases the capacity.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public void AddRangeNoResize(UnsafeList<T> list)
        {
            AddRangeNoResize(list.Ptr, CollectionHelper.AssumePositive(list.Length));
        }

        /// <summary>
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <remarks>
        /// Increments the length by 1. Increases the capacity if necessary.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T value)
        {
            var idx = m_length;
            if (m_length < m_capacity)
            {
                Ptr[idx] = value;
                m_length++;
                return;
            }

            CheckResize(idx, 1, MaxCapacity);
            Resize(idx + 1);
            Ptr[idx] = value;
        }

        /// <summary>
        /// Copies the elements of a buffer to the end of this list.
        /// </summary>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of elements to copy from the buffer.</param>
        /// <remarks>
        /// Increments the length by `count`. Increases the capacity if necessary.
        /// </remarks>
        public void AddRange(void* ptr, int count)
        {
            var idx = m_length;

            if (m_length > Capacity - count)
            {
                CheckResize(m_length, count, MaxCapacity);
                Resize(m_length + count);
            }
            else
            {
                m_length += count;
            }

            var sizeOf = (long)sizeof(T);
            void* dst = (byte*)Ptr + (long)idx * sizeOf;
            UnsafeUtility.MemCpy(dst, ptr, (long)count * sizeOf);
        }

        /// <summary>
        /// Copies elements from a ReadOnlySpan to the end of this list.
        /// </summary>
        /// <param name="roSpan">ReadOnlySpan to copy from.</param>
        public void AddRange(ReadOnlySpan<T> roSpan)
        {
            var count = roSpan.Length;
            if (count > 0)
            {
                fixed (T* ptr = &roSpan[0])
                {
                    AddRange(ptr, count);
                }
            }
        }

        /// <summary>
        /// Copies the elements of another container to the end of the list.
        /// </summary>
        /// <param name="container">The container to copy from.</param>
        /// <remarks>
        /// The length is increased by the length of the other container. Increases the capacity if necessary.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public void AddRange(UnsafeList<T> container)
        {
            AddRange(container.AsReadOnlySpan());
        }

        /// <summary>
        /// Appends value count times to the end of this list.
        /// </summary>
        /// <param name="value">The value to add to the end of this list.</param>
        /// <param name="count">The number of times to replicate the value.</param>
        /// <remarks>
        /// Length is incremented by count. If necessary, the capacity is increased.
        /// </remarks>
        public void AddReplicate(in T value, int count)
        {
            var idx = m_length;

            if (m_length > Capacity - count)
            {
                CheckResize(m_length, count, MaxCapacity);
                Resize(m_length + count);
            }
            else
            {
                m_length += count;
            }

            fixed (void* ptr = &value)
            {
                UnsafeUtility.MemCpyReplicate(Ptr + (long)idx, ptr, sizeof(T), count);
            }
        }

        /// <summary>
        /// Shifts elements toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts elements in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `end - begin`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `end` equals `begin`, the method does nothing.
        ///
        /// The element at index `begin` will be copied to index `end`, the element at index `begin + 1` will be copied to `end + 1`, and so forth.
        ///
        /// The indexes `begin` up to `end` are not cleared: they will contain whatever values they held prior.
        /// </remarks>
        /// <param name="begin">The index of the first element that will be shifted up.</param>
        /// <param name="end">The index where the first shifted element will end up.</param>
        /// <exception cref="ArgumentException">Thrown if `end &lt; begin`.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end)
        {
            CheckBeginEndNoLength(begin, end);

            // Because we've checked begin and end in `CheckBeginEnd` above, we can now
            // assume they are positive.
            begin = CollectionHelper.AssumePositive(begin);
            end = CollectionHelper.AssumePositive(end);

            int items = end - begin;
            if (items < 1)
            {
                return;
            }

            var oldLength = m_length;

            if (m_length > Capacity - items)
            {
                CheckResize(m_length, items, MaxCapacity);
                Resize(m_length + items);
            }
            else
            {
                m_length += items;
            }

            var itemsToCopy = oldLength - begin;

            if (itemsToCopy < 1)
            {
                return;
            }

            var sizeOf = sizeof(T);
            var bytesToCopy = itemsToCopy * sizeOf;
            unsafe
            {
                byte* ptr = (byte*)Ptr;
                byte* dest = ptr + end * sizeOf;
                byte* src = ptr + begin * sizeOf;
                UnsafeUtility.MemMove(dest, src, bytesToCopy);
            }
        }

        /// <summary>
        /// Shifts elements toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts elements in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `count`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `count` equals `0`, the method does nothing.
        ///
        /// The element at index `index` will be copied to index `index + count`, the element at index `index + 1` will be copied to `index + count + 1`, and so forth.
        ///
        /// The indexes `index` up to `index + count` are not cleared: they will contain whatever values they held prior.
        /// </remarks>
        /// <param name="index">The index of the first element that will be shifted up.</param>
        /// <param name="count">The number of elements to insert.</param>
        /// <exception cref="ArgumentException">Thrown if `count` is negative.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void InsertRange(int index, int count) => InsertRangeWithBeginEnd(index, index + count);

        /// <summary>
        /// Copies the last element of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove an element from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last element.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index)
        {
            CollectionHelper.CheckIndexInRange(index, m_length);

            index = CollectionHelper.AssumePositive(index);
            int copyFrom = m_length - 1;
            T* dst = (T*)Ptr + index;
            T* src = (T*)Ptr + copyFrom;
            (*dst) = (*src);
            m_length -= 1;
        }

        /// <summary>
        /// Copies the last *N* elements of this list to a range in this list. Decrements the length by *N*.
        /// </summary>
        /// <remarks>
        /// Copies the last `count` elements to the indexes `index` up to `index + count`.
        ///
        /// Useful as a cheap way to remove elements from a list when you don't care about preserving order.
        /// </remarks>
        /// <param name="index">The index of the first element to overwrite.</param>
        /// <param name="count">The number of elements to copy and remove.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRangeSwapBack(int index, int count)
        {
            CheckIndexCount(index, count);

            index = CollectionHelper.AssumePositive(index);
            count = CollectionHelper.AssumePositive(count);

            if (count > 0)
            {
                int copyFrom = math.max(m_length - count, index + count);
                var sizeOf = (long)sizeof(T);
                void* dst = (byte*)Ptr + (long)index * sizeOf;
                void* src = (byte*)Ptr + (long)copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (long)(m_length - copyFrom) * (long)sizeOf);
                m_length -= count;
            }
        }

        /// <summary>
        /// Removes the element at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index)
        {
            CollectionHelper.CheckIndexInRange(index, m_length);

            index = CollectionHelper.AssumePositive(index);

            T* dst = Ptr + (long)index;
            T* src = dst + 1;
            m_length--;

            // Because these tend to be smaller (< 1MB), and the cost of jumping context to native and back is
            // so high, this consistently optimizes to better code than UnsafeUtility.MemCpy
            for (int i = index; i < m_length; i++)
            {
                *dst++ = *src++;
            }
        }

        /// <summary>
        /// Removes *N* elements in a range, shifting everything above the range down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="index">The index of the first element to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the elements, `RemoveRangeSwapBackWithBeginEnd`
        /// is a more efficient way to remove elements.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRange(int index, int count)
        {
            CheckIndexCount(index, count);

            index = CollectionHelper.AssumePositive(index);
            count = CollectionHelper.AssumePositive(count);

            if (count > 0)
            {
                int copyFrom = math.min(index + count, m_length);
                var sizeOf = (long)sizeof(T);
                void* dst = (byte*)Ptr + (long)index * sizeOf;
                void* src = (byte*)Ptr + (long)copyFrom * sizeOf;
                UnsafeUtility.MemCpy(dst, src, (long)(m_length - copyFrom) * sizeOf);
                m_length -= count;
            }
        }

        /// <summary>
        /// Returns a read only of this list.
        /// </summary>
        /// <returns>A read only of this list.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(Ptr, Length);
        }

        /// <summary>
        /// A read only for an UnsafeList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsReadOnly"/> to create a read only for a list.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ReadOnly
            : IEnumerable<T>
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T* Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            /// <summary>
            /// Get a pointer to the data.
            /// </summary>
            /// <remarks>When container has safety checks enabled it performs a job safety check for read-only access.</remarks>
            /// <returns>A pointer to the data.</returns>
            public readonly T* GetUnsafeReadOnlyPtr()
            {
                return Ptr;
            }

            // Excluded from docs: the generator cannot match a conversion operator declared in a
            // nested type to its assembly entry, and crashes on the unmatched member. Same treatment
            // as the equivalent NativeArray<T>.ReadOnly operator.
            ///<exclude />
            public static implicit operator ReadOnlySpan<T>(in ReadOnly container)
            {
                return container.AsReadOnlySpan();
            }

            /// <summary>
            /// Returns ReadOnlySpan&lt;T&gt; view of this container.
            /// </summary>
            /// <returns>ReadOnlySpan&lt;T&gt; view of this container.</returns>
            public readonly ReadOnlySpan<T> AsReadOnlySpan()
            {
                return new ReadOnlySpan<T>(GetUnsafeReadOnlyPtr(), Length);
            }

            /// <summary>
            /// Whether this list has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this list has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Ptr != null;
            }

            /// <summary>
            /// Whether the list is empty.
            /// </summary>
            /// <value>True if the list is empty or the list has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => !IsCreated || Length == 0;
            }

            internal ReadOnly(T* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Returns an enumerator over the elements of the list.
            /// </summary>
            /// <returns>An enumerator over the elements of the list.</returns>
            public Enumerator GetEnumerator()
            {
                return new Enumerator { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };
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

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// **Obsolete.** Use <see cref="AsReadOnly"/> instead.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
//        [Obsolete("'AsParallelReader' has been deprecated; use 'AsReadOnly' instead. (UnityUpgradable) -> AsReadOnly")]
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// **Obsolete.** Use <see cref="ReadOnly"/> instead.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelReader"/> to create a parallel reader for a list.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
//        [Obsolete("'ParallelReader' has been deprecated; use 'ReadOnly' instead. (UnityUpgradable) -> ReadOnly")]
        public unsafe struct ParallelReader
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T* Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            internal ParallelReader(T* ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Get a pointer to the data.
            /// </summary>
            /// <remarks>When container has safety checks enabled it performs a job safety check for read-only access.</remarks>
            /// <returns>A pointer to the data.</returns>
            public readonly T* GetUnsafeReadOnlyPtr() => Ptr;

            // Excluded from docs: see the note on the ReadOnly cast above.
            ///<exclude />
            public static implicit operator ReadOnlySpan<T>(in ParallelReader container) => container.AsReadOnlySpan();

            /// <summary>
            /// Returns ReadOnlySpan&lt;T&gt; view of this container.
            /// </summary>
            /// <returns>ReadOnlySpan&lt;T&gt; view of this container.</returns>
            public readonly ReadOnlySpan<T> AsReadOnlySpan() => new ReadOnlySpan<T>(GetUnsafeReadOnlyPtr(), Length);
        }

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter((UnsafeList<T>*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        /// A parallel writer for an UnsafeList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public unsafe struct ParallelWriter
        {
            /// <summary>
            /// The data of the list.
            /// </summary>
            public readonly void* Ptr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return ListData->Ptr;
                }
            }

            /// <summary>
            /// The UnsafeList to write to.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<T>* ListData;

            internal unsafe ParallelWriter(UnsafeList<T>* listData)
            {
                ListData = listData;
            }

            /// <summary>
            /// Adds an element to the end of the list.
            /// </summary>
            /// <param name="value">The value to add to the end of the list.</param>
            /// <remarks>
            /// Increments the length by 1. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void AddNoResize(T value)
            {
                var idx = Interlocked.Increment(ref ListData->m_length) - 1;
                ListData->CheckNoResizeHasEnoughCapacity(idx, 1);
                UnsafeUtility.WriteArrayElement(ListData->Ptr, idx, value);
            }

            /// <summary>
            /// Copies elements from a buffer to the end of the list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of elements to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count`. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void AddRangeNoResize(void* ptr, int count)
            {
                var idx = Interlocked.Add(ref ListData->m_length, count) - count;
                ListData->CheckNoResizeHasEnoughCapacity(idx, count);
                void* dst = (byte*)ListData->Ptr + (long)idx * sizeof(T);
                UnsafeUtility.MemCpy(dst, ptr, (long)count * (long)sizeof(T));
            }

            /// <summary>
            /// Copies elements from a ReadOnlySpan to the end of this list.
            /// </summary>
            /// <param name="roSpan">ReadOnlySpan to copy from.</param>
            /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void AddRangeNoResize(ReadOnlySpan<T> roSpan)
            {
                var count = roSpan.Length;
                if (count > 0)
                {
                    fixed (T* ptr = &roSpan[0])
                    {
                        AddRangeNoResize(ptr, count);
                    }
                } 
            }

            /// <summary>
            /// Copies the elements of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length by the length of the other list. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
            public void AddRangeNoResize(UnsafeList<T> list)
            {
                AddRangeNoResize(list.Ptr, list.Length);
            }
        }

        /// <summary>
        /// Copies all elements of specified container to this container.
        /// </summary>
        /// <param name="other">An container to copy into this container.</param>
        public void CopyFrom(in ReadOnlySpan<T> other)
        {
            Resize(other.Length);
            if (other.Length > 0)
            {
                fixed (T* ptr = &other[0])
                {
                    UnsafeUtility.MemCpy(Ptr, ptr, (long)sizeof(T) * (long)other.Length);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator over the elements of the list.
        /// </summary>
        /// <returns>An enumerator over the elements of the list.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Ptr = Ptr, m_Length = Length, m_Index = -1 };
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
        /// An enumerator over the elements of a list.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is invalid.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first element of the list.
        /// </remarks>
        public struct Enumerator : IEnumerator<T>
        {
            internal T* m_Ptr;
            internal int m_Length;
            internal int m_Index;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next element of the list.
            /// </summary>
            /// <remarks>
            /// The first `MoveNext` call advances the enumerator to the first element of the list. Before this call, `Current` is not valid to read.
            /// </remarks>
            /// <returns>True if `Current` is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Length;

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// The current element.
            /// </summary>
            /// <value>The current element.</value>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Ptr[m_Index];
            }

            object IEnumerator.Current => Current;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckNull(void* listData)
        {
            if (listData == null)
            {
                throw new InvalidOperationException("UnsafeList has yet to be created or has been destroyed!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckIndexCount(int index, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException($"Value for count {count} must be positive.");
            }

            if (index < 0)
            {
                throw new IndexOutOfRangeException($"Value for index {index} must be positive.");
            }

            if (index > Length)
            {
                throw new IndexOutOfRangeException($"Value for index {index} is out of bounds.");
            }

            if (index + count > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for count {count} is out of bounds.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckBeginEndNoLength(int begin, int end)
        {
            if (begin > end)
            {
                throw new ArgumentException($"Value for begin {begin} index must less or equal to end {end}.");
            }

            if (begin < 0)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} must be positive.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckBeginEnd(int begin, int end)
        {
            CheckBeginEndNoLength(begin, end);

            if (begin > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for begin {begin} is out of bounds.");
            }

            if (end > Length)
            {
                throw new ArgumentOutOfRangeException($"Value for end {end} is out of bounds.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckNoResizeHasEnoughCapacity(int length)
        {
            CheckNoResizeHasEnoughCapacity(length, Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckNoResizeHasEnoughCapacity(int length, int index)
        {
            if (Capacity < index + length)
            {
                throw new InvalidOperationException($"AddNoResize assumes that list capacity is sufficient (Capacity {Capacity}, Length {Length}), requested length {length}!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckResize(int currentLength, int numElements, int maxCapacity)
        {
            long newLength = (long)currentLength + numElements;

            if (maxCapacity < newLength)
            {
                throw new InvalidOperationException($"Capacity is insufficient, and resize would fail (Capacity {maxCapacity}, Length {currentLength}), requested length {newLength}!");
            }
        }
    }

    /// <summary>
    /// Provides extension methods for UnsafeList.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe static class UnsafeListExtensions
    {
        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in this list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">A value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the value if it is found. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this UnsafeList<T> container, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf(container.AsReadOnlySpan(), value);
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this UnsafeList<T> container, U value) where T : unmanaged, IEquatable<U>
        {
            return container.IndexOf(value) != -1;
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in this list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">A value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the value if it is found. Returns -1 if no occurrence is found.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this UnsafeList<T>.ReadOnly container, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf(container.AsReadOnlySpan(), value);
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this UnsafeList<T>.ReadOnly container, U value) where T : unmanaged, IEquatable<U>
        {
            return container.IndexOf(value) != -1;
        }

        // Docs must match the other IndexOf overloads: MemDocToMemXml collapses UnsafeList<T>,
        // UnsafeList<T>.ReadOnly and UnsafeList<T>.ParallelReader to one signature, and differing
        // docs across the group fail the ScriptReference build. ParallelReader's deprecation is
        // documented on the type itself.
        /// <summary>
        /// Finds the index of the first occurrence of a particular value in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in this list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">A value to locate.</param>
        /// <returns>The zero-based index of the first occurrence of the value if it is found. Returns -1 if no occurrence is found.</returns>
//        [Obsolete("'UnsafeList<T>.ParallelReader' has been deprecated; use 'UnsafeList<T>.ReadOnly' instead.")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static int IndexOf<T, U>(this UnsafeList<T>.ParallelReader container, U value) where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf(container.AsReadOnlySpan(), value);
        }

        // Docs must match the other Contains overloads: see the note on IndexOf above.
        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <typeparam name="U">The type of value to locate.</typeparam>
        /// <param name="container">This list.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
//        [Obsolete("'UnsafeList<T>.ParallelReader' has been deprecated; use 'UnsafeList<T>.ReadOnly' instead.")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static bool Contains<T, U>(this UnsafeList<T>.ParallelReader container, U value) where T : unmanaged, IEquatable<U>
        {
            return container.IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if this container and another have equal length and content.
        /// </summary>
        /// <typeparam name="T">The type of the source container's elements.</typeparam>
        /// <param name="container">The container to compare for equality.</param>
        /// <param name="other">The other container to compare for equality.</param>
        /// <returns>True if the containers have equal length and content.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static bool ArraysEqual<T>(this UnsafeList<T> container, in UnsafeList<T> other)
            where T : unmanaged, IEquatable<T>
        {
            if (container.Length != other.Length)
                return false;

            for (int i = 0; i != container.Length; i++)
            {
                if (!container[i].Equals(other[i]))
                    return false;
            }

            return true;
        }

    }

    internal sealed class UnsafeListTDebugView<T>
        where T : unmanaged
    {
        UnsafeList<T> Data;

        public UnsafeListTDebugView(UnsafeList<T> data)
        {
            Data = data;
        }

        public unsafe T[] Items
        {
            get
            {
                T[] result = new T[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data.Ptr[i];
                }

                return result;
            }
        }
    }

    /// <summary>
    /// An unmanaged, resizable list of pointers.
    /// </summary>
    /// <typeparam name="T">The type of pointer element.</typeparam>
    [DebuggerDisplay("Length = {Length}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafePtrListDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafePtrList<T>
        : INativeDisposable
        // IIndexable<T> and INativeList<T> can't be implemented because this[index] and ElementAt return T* instead of T.
        , IEnumerable<IntPtr> // Used by collection initializers.
        where T : unmanaged
    {
        // <WARNING>
        // 'Header' of this struct must binary match `UntypedUnsafeList`, `UnsafeList`, `UnsafePtrList`, and `NativeArray` struct.
        // Fields must match UntypedUnsafeList structure, please don't reorder and don't insert anything in between first 4 fields

        /// <summary>
        /// The internal buffer of this list.
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        public readonly T** Ptr;

        /// <summary>
        /// The number of elements.
        /// </summary>
        public readonly int m_length;

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        public readonly int m_capacity;

        /// <summary>
        /// The allocator used to create the internal buffer.
        /// </summary>
        public readonly AllocatorManager.AllocatorHandle Allocator;

        readonly int padding;

        /// <summary>
        /// The number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.ListDataRO().Length;
            set => this.ListData().Length = value;
        }

        /// <summary>
        /// The number of elements that can fit in the internal buffer.
        /// </summary>
        /// <value>The number of elements that can fit in the internal buffer.</value>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => this.ListDataRO().Capacity;
            set => this.ListData().Capacity = value;
        }

        /// <summary>
        /// The element at an index.
        /// </summary>
        /// <param name="index">An index.</param>
        /// <value>The element at the index.</value>
        public T* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CollectionHelper.CheckIndexInRange(index, Length);
                return Ptr[CollectionHelper.AssumePositive(index)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CollectionHelper.CheckIndexInRange(index, Length);
                Ptr[CollectionHelper.AssumePositive(index)] = value;
            }
        }

        /// <summary>
        /// Returns a reference to the element at a given index.
        /// </summary>
        /// <param name="index">The index to access. Must be in the range of [0..Length).</param>
        /// <returns>A reference to the element at the index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T* ElementAt(int index)
        {
            CollectionHelper.CheckIndexInRange(index, Length);
            return ref Ptr[CollectionHelper.AssumePositive(index)];
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafePtrList.
        /// </summary>
        /// <param name="ptr">An existing pointer array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        public unsafe UnsafePtrList(T** ptr, int length) : this()
        {
            Ptr = ptr;
            m_length = length;
            m_capacity = length;
            Allocator = AllocatorManager.None;
        }

        /// <summary>
        /// Initializes and returns an instance of UnsafePtrList.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public unsafe UnsafePtrList(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            Ptr = null;
            m_length = 0;
            m_capacity = 0;
            padding = 0;
            Allocator = AllocatorManager.None;

            this.ListData() = new UnsafeList<IntPtr>(initialCapacity, allocator, options);
        }

        /// <summary>
        /// Returns a new list of pointers.
        /// </summary>
        /// <param name="ptr">An existing pointer array to set as the internal buffer.</param>
        /// <param name="length">The length.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafePtrList<T>* Create(T** ptr, int length)
        {
            UnsafePtrList<T>* listData = AllocatorManager.Allocate<UnsafePtrList<T>>(AllocatorManager.Persistent);
            *listData = new UnsafePtrList<T>(ptr, length);
            return listData;
        }

        /// <summary>
        /// Returns a new list of pointers.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the list.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        /// <returns>A pointer to the new list.</returns>
        public static UnsafePtrList<T>* Create(int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            UnsafePtrList<T>* listData = AllocatorManager.Allocate<UnsafePtrList<T>>(allocator);
            *listData = new UnsafePtrList<T>(initialCapacity, allocator, options);
            return listData;
        }

        /// <summary>
        /// Destroys the list.
        /// </summary>
        /// <param name="listData">The list to destroy.</param>
        public static void Destroy(UnsafePtrList<T>* listData)
        {
            UnsafeList<IntPtr>.CheckNull(listData);
            var allocator = listData->ListData().Allocator.Value == AllocatorManager.Invalid.Value
                ? AllocatorManager.Persistent
                : listData->ListData().Allocator
            ;
            listData->Dispose();
            AllocatorManager.Free(allocator, listData);
        }

        /// <summary>
        /// Whether the list is empty.
        /// </summary>
        /// <value>True if the list is empty or the list has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || Length == 0;
        }

        /// <summary>
        /// Whether this list has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this list has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr != null;
        }

        /// <summary>
        /// Releases all resources (memory).
        /// </summary>
        public void Dispose()
        {
            this.ListData().Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that frees the memory of this list.
        /// </summary>
        /// <param name="inputDeps">The dependency for the new job.</param>
        /// <returns>The handle of the new job. The job depends upon `inputDeps` and frees the memory of this list.</returns>
        public JobHandle Dispose(JobHandle inputDeps) => this.ListData().Dispose(inputDeps);

        /// <summary>
        /// Sets the length to 0.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear() => this.ListData().Clear();

        /// <summary>
        /// Sets the length, expanding the capacity if necessary.
        /// </summary>
        /// <param name="length">The new length.</param>
        /// <param name="options">Whether newly allocated bytes should be zeroed out.</param>
        public void Resize(int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) => this.ListData().Resize(length, options);

        /// <summary>
        /// Sets the capacity.
        /// </summary>
        /// <param name="capacity">The new capacity.</param>
        public void SetCapacity(int capacity) => this.ListData().SetCapacity(capacity);

        /// <summary>
        /// Sets the capacity to match the length.
        /// </summary>
        public void TrimExcess() => this.ListData().TrimExcess();

        /// <summary>
        /// Returns the index of the first occurrence of a specific pointer in the list.
        /// </summary>
        /// <param name="ptr">The pointer to search for in the list.</param>
        /// <returns>The index of the first occurrence of the pointer. Returns -1 if it is not found in the list.</returns>
        public int IndexOf(void* ptr)
        {
            for (int i = 0; i < Length; ++i)
            {
                if (Ptr[i] == ptr) return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns true if the list contains at least one occurrence of a specific pointer.
        /// </summary>
        /// <param name="ptr">The pointer to search for in the list.</param>
        /// <returns>True if the list contains at least one occurrence of the pointer.</returns>
        public bool Contains(void* ptr)
        {
            return IndexOf(ptr) != -1;
        }

        /// <summary>
        /// Adds a pointer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by 1. Never increases the capacity.
        /// </remarks>
        /// <param name="value">The pointer to add to the end of the list.</param>
        /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
        public void AddNoResize(void* value)
        {
            this.ListData().AddNoResize((IntPtr)value);
        }

        /// <summary>
        /// Copies pointers from a buffer to the end of this list.
        /// </summary>
        /// <remarks>
        /// Increments the length by `count`. Never increases the capacity.
        /// </remarks>
        /// <param name="ptr">The buffer to copy from.</param>
        /// <param name="count">The number of pointers to copy from the buffer.</param>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(void** ptr, int count) => this.ListData().AddRangeNoResize(ptr, count);

        /// <summary>
        /// Copies the pointers of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Never increases the capacity.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
        public void AddRangeNoResize(UnsafePtrList<T> list) => this.ListData().AddRangeNoResize(list.Ptr, list.Length);

        /// <summary>
        /// Adds a pointer to the end of the list.
        /// </summary>
        /// <param name="value">The pointer to add to the end of this list.</param>
        /// <remarks>
        /// Increments the length by 1. Increases the capacity if necessary.
        /// </remarks>
        public void Add(in IntPtr value)
        {
            this.ListData().Add(value);
        }

        /// <summary>
        /// Adds a pointer to the end of the list.
        /// </summary>
        /// <param name="value">The pointer to add to the end of this list.</param>
        /// <remarks>
        /// Increments the length by 1. Increases the capacity if necessary.
        /// </remarks>
        public void Add(void* value)
        {
            this.ListData().Add((IntPtr)value);
        }

        /// <summary>
        /// Adds elements from a buffer to this list.
        /// </summary>
        /// <param name="ptr">A pointer to the buffer.</param>
        /// <param name="length">The number of elements to add to the list.</param>
        public void AddRange(void* ptr, int length) => this.ListData().AddRange(ptr, length);

        /// <summary>
        /// Copies the elements of another list to the end of this list.
        /// </summary>
        /// <param name="list">The other list to copy from.</param>
        /// <remarks>
        /// Increments the length by the length of the other list. Increases the capacity if necessary.
        /// </remarks>
        public void AddRange(UnsafePtrList<T> list) => this.ListData().AddRange(list.ListData());

        /// <summary>
        /// Shifts pointers toward the end of this list, increasing its length.
        /// </summary>
        /// <remarks>
        /// Right-shifts pointers in the list so as to create 'free' slots at the beginning or in the middle.
        ///
        /// The length is increased by `end - begin`. If necessary, the capacity will be increased accordingly.
        ///
        /// If `end` equals `begin`, the method does nothing.
        ///
        /// The pointer at index `begin` will be copied to index `end`, the pointer at index `begin + 1` will be copied to `end + 1`, and so forth.
        ///
        /// The indexes `begin` up to `end` are not cleared: they will contain whatever pointers they held prior.
        /// </remarks>
        /// <param name="begin">The index of the first pointer that will be shifted up.</param>
        /// <param name="end">The index where the first shifted pointer will end up.</param>
        /// <exception cref="ArgumentException">Thrown if `end &lt; begin`.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `begin` or `end` are out of bounds.</exception>
        public void InsertRangeWithBeginEnd(int begin, int end) => this.ListData().InsertRangeWithBeginEnd(begin, end);

        /// <summary>
        /// Copies the last pointer of this list to the specified index. Decrements the length by 1.
        /// </summary>
        /// <remarks>Useful as a cheap way to remove a pointer from this list when you don't care about preserving order.</remarks>
        /// <param name="index">The index to overwrite with the last pointer.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAtSwapBack(int index) => this.ListData().RemoveAtSwapBack(index);

        /// <summary>
        /// Copies the last *N* pointer of this list to a range in this list. Decrements the length by *N*.
        /// </summary>
        /// <remarks>
        /// Copies the last `count` pointers to the indexes `index` up to `index + count`.
        ///
        /// Useful as a cheap way to remove pointers from a list when you don't care about preserving order.
        /// </remarks>
        /// <param name="index">The index of the first pointer to overwrite.</param>
        /// <param name="count">The number of pointers to copy and remove.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRangeSwapBack(int index, int count) => this.ListData().RemoveRangeSwapBack(index, count);

        /// <summary>
        /// Removes the pointer at an index, shifting everything above it down by one. Decrements the length by 1.
        /// </summary>
        /// <param name="index">The index of the pointer to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the pointers, <see cref="RemoveAtSwapBack(int)"/> is a more efficient way to remove pointers.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds.</exception>
        public void RemoveAt(int index) => this.ListData().RemoveAt(index);

        /// <summary>
        /// Removes *N* pointers in a range, shifting everything above the range down by *N*. Decrements the length by *N*.
        /// </summary>
        /// <param name="index">The index of the first pointer to remove.</param>
        /// <param name="count">The number of pointers to remove.</param>
        /// <remarks>
        /// If you don't care about preserving the order of the pointers, `RemoveRangeSwapBackWithBeginEnd`
        /// is a more efficient way to remove pointers.
        /// </remarks>
        /// <exception cref="IndexOutOfRangeException">Thrown if `index` is out of bounds</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if `count` is negative,
        /// or `index + count` exceeds the length.</exception>
        public void RemoveRange(int index, int count) => this.ListData().RemoveRange(index, count);

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. It will throw NotImplementedException if it is used.
        /// </summary>
        /// <remarks>Use Enumerator GetEnumerator() instead.</remarks>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<IntPtr> IEnumerable<IntPtr>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a read only of this list.
        /// </summary>
        /// <returns>A read only of this list.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(Ptr, Length);
        }

        /// <summary>
        /// A read only for an UnsafePtrList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsReadOnly"/> to create a read only for a list.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ReadOnly
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T** Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            /// <summary>
            /// Whether this list has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this list has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => Ptr != null;
            }

            /// <summary>
            /// Whether the list is empty.
            /// </summary>
            /// <value>True if the list is empty or the list has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => !IsCreated || Length == 0;
            }

            internal ReadOnly(T** ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Returns the index of the first occurrence of a specific pointer in the list.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>The index of the first occurrence of the pointer. Returns -1 if it is not found in the list.</returns>
            public int IndexOf(void* ptr)
            {
                for (int i = 0; i < Length; ++i)
                {
                    if (Ptr[i] == ptr) return i;
                }
                return -1;
            }

            /// <summary>
            /// Returns true if the list contains at least one occurrence of a specific pointer.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>True if the list contains at least one occurrence of the pointer.</returns>
            public bool Contains(void* ptr)
            {
                return IndexOf(ptr) != -1;
            }
        }

        /// <summary>
        /// **Obsolete**. Use <see cref="AsReadOnly"/> instead.
        /// </summary>
        /// <returns>A parallel reader of this list.</returns>
//        [Obsolete("'AsParallelReader' has been deprecated; use 'AsReadOnly' instead. (UnityUpgradable) -> AsReadOnly")]
        public ParallelReader AsParallelReader()
        {
            return new ParallelReader(Ptr, Length);
        }

        /// <summary>
        /// **Obsolete.** Use <see cref="ReadOnly"/> instead.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelReader"/> to create a parallel reader for a list.
        /// </remarks>
//        [Obsolete("'ParallelReader' has been deprecated; use 'ReadOnly' instead. (UnityUpgradable) -> ReadOnly")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ParallelReader
        {
            /// <summary>
            /// The internal buffer of the list.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T** Ptr;

            /// <summary>
            /// The number of elements.
            /// </summary>
            public readonly int Length;

            internal ParallelReader(T** ptr, int length)
            {
                Ptr = ptr;
                Length = length;
            }

            /// <summary>
            /// Returns the index of the first occurrence of a specific pointer in the list.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>The index of the first occurrence of the pointer. Returns -1 if it is not found in the list.</returns>
            public int IndexOf(void* ptr)
            {
                for (int i = 0; i < Length; ++i)
                {
                    if (Ptr[i] == ptr) return i;
                }
                return -1;
            }

            /// <summary>
            /// Returns true if the list contains at least one occurrence of a specific pointer.
            /// </summary>
            /// <param name="ptr">The pointer to search for in the list.</param>
            /// <returns>True if the list contains at least one occurrence of the pointer.</returns>
            public bool Contains(void* ptr)
            {
                return IndexOf(ptr) != -1;
            }
        }

        /// <summary>
        /// Returns a parallel writer of this list.
        /// </summary>
        /// <returns>A parallel writer of this list.</returns>
        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(Ptr, (UnsafeList<IntPtr>*)UnsafeUtility.AddressOf(ref this));
        }

        /// <summary>
        /// A parallel writer for an UnsafePtrList&lt;T&gt;.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a list.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public unsafe struct ParallelWriter
        {
            /// <summary>
            /// The data of the list.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public readonly T** Ptr;

            /// <summary>
            /// The UnsafeList to write to.
            /// </summary>
            [NativeDisableUnsafePtrRestriction]
            public UnsafeList<IntPtr>* ListData;

            internal unsafe ParallelWriter(T** ptr, UnsafeList<IntPtr>* listData)
            {
                Ptr = ptr;
                ListData = listData;
            }

            /// <summary>
            /// Adds a pointer to the end of the list.
            /// </summary>
            /// <param name="value">The pointer to add to the end of the list.</param>
            /// <remarks>
            /// Increments the length by 1. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if incrementing the length would exceed the capacity.</exception>
            public void AddNoResize(T* value) => ListData->AddNoResize((IntPtr)value);

            /// <summary>
            /// Copies pointers from a buffer to the end of the list.
            /// </summary>
            /// <param name="ptr">The buffer to copy from.</param>
            /// <param name="count">The number of pointers to copy from the buffer.</param>
            /// <remarks>
            /// Increments the length by `count`. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(T** ptr, int count) => ListData->AddRangeNoResize(ptr, count);

            /// <summary>
            /// Copies the pointers of another list to the end of this list.
            /// </summary>
            /// <param name="list">The other list to copy from.</param>
            /// <remarks>
            /// Increments the length by the length of the other list. Never increases the capacity.
            /// </remarks>
            /// <exception cref="InvalidOperationException">Thrown if the increased length would exceed the capacity.</exception>
            public void AddRangeNoResize(UnsafePtrList<T> list) => ListData->AddRangeNoResize(list.Ptr, list.Length);
        }
    }

    [GenerateTestsForBurstCompatibility]
    internal static class UnsafePtrListExtensions
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref UnsafeList<IntPtr> ListData<T>(ref this UnsafePtrList<T> from) where T : unmanaged => ref UnsafeUtility.As<UnsafePtrList<T>, UnsafeList<IntPtr>>(ref from);

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<IntPtr> ListDataRO<T>(this UnsafePtrList<T> from) where T : unmanaged => UnsafeUtility.As<UnsafePtrList<T>, UnsafeList<IntPtr>>(ref from);
    }

    internal sealed class UnsafePtrListDebugView<T>
        where T : unmanaged
    {
        UnsafePtrList<T> Data;

        public UnsafePtrListDebugView(UnsafePtrList<T> data)
        {
            Data = data;
        }

        public unsafe T*[] Items
        {
            get
            {
                T*[] result = new T*[Data.Length];

                for (var i = 0; i < result.Length; ++i)
                {
                    result[i] = Data.Ptr[i];
                }

                return result;
            }
        }
    }
}
