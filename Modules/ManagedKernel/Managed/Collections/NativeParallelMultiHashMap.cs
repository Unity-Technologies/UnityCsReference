// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.NotBurstCompatible;
using Unity.Jobs;

namespace Unity.Collections
{
    /// <summary>
    /// An iterator over all values associated with an individual key in a multi hash map.
    /// </summary>
    /// <remarks>The iteration order over the values associated with a key is an implementation detail. Do not rely upon any particular ordering.</remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
    public struct NativeParallelMultiHashMapIterator<TKey>
        where TKey : unmanaged
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;

        /// <summary>
        /// Returns the entry index.
        /// </summary>
        /// <returns>The entry index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetEntryIndex() => EntryIndex;
    }

    /// <summary>
    /// An unordered, expandable associative array. Each key can have more than one associated value.
    /// </summary>
    /// <remarks>
    /// Unlike a regular NativeParallelHashMap, a NativeParallelMultiHashMap can store multiple key-value pairs with the same key.
    ///
    /// The keys are not deduplicated: two key-value pairs with the same key are stored as fully separate key-value pairs.
    /// </remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeParallelMultiHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct NativeParallelMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal UnsafeParallelMultiHashMap<TKey, TValue> m_MultiHashMapData;

        internal AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeParallelMultiHashMap<TKey, TValue>>();

        /// <summary>
        /// Returns a newly allocated multi hash map.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeParallelMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            this = default;
            Initialize(capacity, ref allocator);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
        internal void Initialize<U>(int capacity, ref U allocator)
            where U : unmanaged, AllocatorManager.IAllocator
        {
            m_MultiHashMapData = new UnsafeParallelMultiHashMap<TKey, TValue>(capacity, allocator.Handle);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator.ToAllocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);

            CollectionHelper.SetStaticSafetyId<NativeParallelMultiHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if the hash map is empty or if the hash map has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return m_MultiHashMapData.IsEmpty;
            }
        }

        /// <summary>
        /// Returns the current number of key-value pairs in this hash map.
        /// </summary>
        /// <remarks>Key-value pairs with matching keys are counted as separate, individual pairs.</remarks>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public readonly int Count()
        {
            CheckRead();
            return m_MultiHashMapData.Count();
        }

        /// <summary>
        /// Returns the number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        /// <exception cref="InvalidOperationException">Thrown if `value` is less than the current capacity.</exception>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CheckRead();
                return m_MultiHashMapData.Capacity;
            }

            set
            {
                CheckWrite();
                m_MultiHashMapData.Capacity = value;
            }
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = UnsafeParallelMultiHashMap<TKey, TValue>.MaxCapacity;

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            m_MultiHashMapData.Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>
        /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
        /// </remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        public void Add(TKey key, TValue item)
        {
            CheckWrite();
            m_MultiHashMapData.Add(key, item);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of removed key-value pairs. If the key was not present, returns 0.</returns>
        public int Remove(TKey key)
        {
            CheckWrite();
            return m_MultiHashMapData.Remove(key);
        }

        /// <summary>
        /// Removes a single key-value pair.
        /// </summary>
        /// <param name="it">An iterator representing the key-value pair to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the iterator is invalid.</exception>
        public void Remove(NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            m_MultiHashMapData.Remove(it);
        }

        /// <summary>
        /// Gets an iterator for a key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">Outputs the associated value represented by the iterator.</param>
        /// <param name="it">Outputs an iterator.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetFirstValue(key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckRead();
            return m_MultiHashMapData.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present in this hash map.</returns>
        public bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// Returns the number of values associated with a given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The number of values associated with the key. Returns 0 if the key was not present.</returns>
        public int CountValuesForKey(TKey key)
        {
            if (!TryGetFirstValue(key, out var value, out var iterator))
            {
                return 0;
            }

            var count = 1;
            while (TryGetNextValue(out value, ref iterator))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Sets a new value for an existing key-value pair.
        /// </summary>
        /// <param name="item">The new value.</param>
        /// <param name="it">The iterator representing a key-value pair.</param>
        /// <returns>True if a value was overwritten.</returns>
        public bool SetValue(TValue item, NativeParallelMultiHashMapIterator<TKey> it)
        {
            CheckWrite();
            return m_MultiHashMapData.SetValue(item, it);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated => m_MultiHashMapData.IsCreated;

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
            m_MultiHashMapData.Dispose();
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
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

            var jobHandle = new UnsafeParallelHashMapDataDisposeJob { Data = new UnsafeParallelHashMapDataDispose { m_Buffer = m_MultiHashMapData.m_Buffer, m_AllocatorLabel = m_MultiHashMapData.m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
            m_MultiHashMapData.m_Buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Returns an array with a copy of all the keys (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        ///
        /// Use `GetUniqueKeyArray` of <see cref="Unity.Collections.NativeParallelHashMapExtensions"/> instead if you only want one occurrence of each key.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all the values (in no particular order).
        /// </summary>
        /// <remarks>The values are not deduplicated. If you sort the returned array,
        /// you can use <see cref="Unity.Collections.NativeParallelHashMapExtensions.Unique{T}"/> to remove duplicate values.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all the keys and values (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        /// </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all the keys and values (in no particular order).</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_MultiHashMapData.GetKeyValueArrays(allocator);
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_MultiHashMapData.AsParallelWriter();
            writer.m_Safety = m_Safety;
            CollectionHelper.SetStaticSafetyId<ParallelWriter>(ref writer.m_Safety, ref s_staticSafetyId.Data);
            return writer;
        }

        /// <summary>
        /// A parallel writer for a NativeParallelMultiHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeParallelMultiHashMap.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public unsafe struct ParallelWriter
        {
            internal UnsafeParallelMultiHashMap<TKey, TValue>.ParallelWriter m_Writer;

            internal AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ParallelWriter>();
            /// <summary>
            /// Returns the index of the current thread.
            /// </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

            /// <summary>
            /// Returns the number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                    return m_Writer.Capacity;
                }
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>
            /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
            /// </remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public void Add(TKey key, TValue item)
            {
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
                m_Writer.Add(key, item);
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>
            /// If a key-value pair with this key is already present, an additional separate key-value pair is added.
            /// </remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public void Add(TKey key, TValue item, int threadIndexOverride)
            {
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
                m_Writer.Add(key, item, threadIndexOverride);
            }
        }

        /// <summary>
        /// Returns an enumerator over the values of an individual key.
        /// </summary>
        /// <param name="key">The key to get an enumerator for.</param>
        /// <returns>An enumerator over the values of a key.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return new Enumerator { hashmap = this, key = key, isFirst = 1 };
        }

        /// <summary>
        /// An enumerator over the values of an individual key in a multi hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first value of the key.
        /// </remarks>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal NativeParallelMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal byte isFirst;

            TValue value;
            NativeParallelMultiHashMapIterator<TKey> iterator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next value of the key.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                //Avoids going beyond the end of the collection.
                if (isFirst == 1)
                {
                    isFirst = 0;
                    return hashmap.TryGetFirstValue(key, out value, out iterator);
                }

                return hashmap.TryGetNextValue(out value, ref iterator);
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => isFirst = 1;

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return value;
                }
            }

            object IEnumerator.Current => Current;

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var ash = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
            return new KeyValueEnumerator
            {
                m_Safety = ash,
                m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_MultiHashMapData.m_Buffer),
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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

        /// <summary>
        /// An enumerator over the key-value pairs of a multi hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.
        ///
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// The first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
            internal AtomicSafetyHandle m_Safety;
            internal UnsafeParallelHashMapDataEnumerator m_Enumerator;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe bool MoveNext()
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
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public readonly KeyValue<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                    return m_Enumerator.GetCurrent<TKey, TValue>();
                }
            }

            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this NativeParallelHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the NativeParallelHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            var ash = m_Safety;
            return new ReadOnly(m_MultiHashMapData, ash);
        }

        /// <summary>
        /// A read-only alias for the value of a NativeParallelHashMap. Does not have its own allocated storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [DebuggerTypeProxy(typeof(NativeParallelHashMapDebuggerTypeProxy<,>))]
        [DebuggerDisplay("Count = {m_HashMapData.Count()}, Capacity = {m_HashMapData.Capacity}, IsCreated = {m_HashMapData.IsCreated}, IsEmpty = {IsEmpty}")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KeyValue<TKey, TValue>>
        {
            internal UnsafeParallelMultiHashMap<TKey, TValue> m_MultiHashMapData;

            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();

            [GenerateTestsForBurstCompatibility(CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
            internal ReadOnly(UnsafeParallelMultiHashMap<TKey, TValue> container, AtomicSafetyHandle safety)
            {
                m_MultiHashMapData = container;
                m_Safety = safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref m_Safety, ref s_staticSafetyId.Data);
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_MultiHashMapData.IsCreated;
            }

            /// <summary>
            /// Whether this hash map is empty.
            /// </summary>
            /// <value>True if this hash map is empty or if the map has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (!IsCreated)
                    {
                        return true;
                    }

                    CheckRead();
                    return m_MultiHashMapData.IsEmpty;
                }
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count()
            {
                CheckRead();
                return m_MultiHashMapData.Count();
            }

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    return m_MultiHashMapData.Capacity;
                }
            }

            /// <summary>
            /// Gets an iterator for a key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="item">Outputs the associated value represented by the iterator.</param>
            /// <param name="it">Outputs an iterator.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool TryGetFirstValue(TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
            {
                CheckRead();
                return m_MultiHashMapData.TryGetFirstValue(key, out item, out it);
            }

            /// <summary>
            /// Advances an iterator to the next value associated with its key.
            /// </summary>
            /// <param name="item">Outputs the next value.</param>
            /// <param name="it">A reference to the iterator to advance.</param>
            /// <returns>True if the key was present and had another value.</returns>
            public readonly bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
            {
                CheckRead();
                return m_MultiHashMapData.TryGetNextValue(out item, ref it);
            }

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool ContainsKey(TKey key)
            {
                CheckRead();
                return m_MultiHashMapData.ContainsKey(key);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_MultiHashMapData.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_MultiHashMapData.GetValueArray(allocator);
            }

            /// <summary>
            /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
            /// </summary>
            /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
            public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_MultiHashMapData.GetKeyValueArrays(allocator);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            readonly void CheckRead()
            {
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void ThrowKeyNotPresent(TKey key)
            {
                throw new ArgumentException($"Key: {key} is not present in the NativeParallelHashMap.");
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public KeyValueEnumerator GetEnumerator()
            {
                AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
                var ash = m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref ash);
                return new KeyValueEnumerator
                {
                    m_Safety = ash,
                    m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_MultiHashMapData.m_Buffer),
                };
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<KeyValue<TKey, TValue>> IEnumerable<KeyValue<TKey, TValue>>.GetEnumerator()
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
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
        }
    }

    internal sealed class NativeParallelMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        NativeParallelMultiHashMap<TKey, TValue> m_Target;

        public NativeParallelMultiHashMapDebuggerTypeProxy(NativeParallelMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                (NativeArray<TKey>, int) keys = default;
                using(NativeParallelHashMap<TKey,TValue> uniques = new NativeParallelHashMap<TKey,TValue>(m_Target.Count(),Allocator.Temp))
                {
                    var enumerator = m_Target.GetEnumerator();
                    while(enumerator.MoveNext())
                        uniques.TryAdd(enumerator.Current.Key,default);
                    keys.Item1 = uniques.GetKeyArray(Allocator.Temp);
                    keys.Item2 = keys.Item1.Length;
                }
                using (keys.Item1)
                {
                    for (var k = 0; k < keys.Item2; ++k)
                    {
                        var values = new List<TValue>();
                        if (m_Target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            }
                            while (m_Target.TryGetNextValue(out value, ref iterator));
                        }

                        result.Add(new ListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Extension methods for NativeParallelMultiHashMap.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public unsafe static class NativeParallelMultiHashMapExtensions
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        internal static void Initialize<TKey, TValue, U>(ref this NativeParallelMultiHashMap<TKey, TValue> container,
                                                            int capacity,
                                                            ref U allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where U : unmanaged, AllocatorManager.IAllocator
        {
            container.m_MultiHashMapData = new UnsafeParallelMultiHashMap<TKey, TValue>(capacity, allocator.Handle);

            container.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);

            CollectionHelper.SetStaticSafetyId<NativeParallelMultiHashMap<TKey, TValue>>(ref container.m_Safety, ref NativeParallelMultiHashMap<TKey, TValue>.s_staticSafetyId.Data);
        }
    }
}
