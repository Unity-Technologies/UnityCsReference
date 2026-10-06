// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Jobs;
using ManagedKernel.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// An unordered, expandable associative array. Each key can have more than one associated value.
    /// </summary>
    /// <remarks>
    /// Unlike a regular UnsafeParallelHashMap, an UnsafeParallelMultiHashMap can store multiple key-value pairs with the same key.
    ///
    /// The keys are not deduplicated: two key-value pairs with the same key are stored as fully separate key-value pairs.
    /// </remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeParallelMultiHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeParallelMultiHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of UnsafeParallelMultiHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeParallelMultiHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeParallelHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);
            Clear();
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or the hash map has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || UnsafeParallelHashMapData.IsEmpty(m_Buffer);
        }

        /// <summary>
        /// Returns the current number of key-value pairs in this hash map.
        /// </summary>
        /// <remarks>Key-value pairs with matching keys are counted as separate, individual pairs.</remarks>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public readonly int Count()
        {
            if (m_Buffer->allocatedIndexLength <= 0)
            {
                return 0;
            }

            return UnsafeParallelHashMapData.GetCount(m_Buffer);
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
                UnsafeParallelHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeParallelHashMapData* data = m_Buffer;
                UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeParallelHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = UnsafeParallelHashMapData.kMaxCapacity;

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            UnsafeParallelHashMapBase<TKey, TValue>.Clear(m_Buffer);
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
            UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, true, m_AllocatorLabel);
        }

        /// <summary>
        /// Removes a key and its associated value(s).
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of removed key-value pairs. If the key was not present, returns 0.</returns>
        public int Remove(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, key, true);
        }

        /// <summary>
        /// Removes all key-value pairs with a particular key and a particular value.
        /// </summary>
        /// <remarks>Removes all key-value pairs which have a particular key and which *also have* a particular value.
        /// In other words: (key *AND* value) rather than (key *OR* value).</remarks>
        /// <typeparam name="TValueEQ">The type of the value.</typeparam>
        /// <param name="key">The key of the key-value pairs to remove.</param>
        /// <param name="value">The value of the key-value pairs to remove.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public void Remove<TValueEQ>(TKey key, TValueEQ value)
            where TValueEQ : unmanaged, IEquatable<TValueEQ>
        {
            UnsafeParallelHashMapBase<TKey, TValueEQ>.RemoveKeyValue(m_Buffer, key, value);
        }

        /// <summary>
        /// Removes a single key-value pair.
        /// </summary>
        /// <param name="it">An iterator representing the key-value pair to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if the iterator is invalid.</exception>
        public void Remove(NativeParallelMultiHashMapIterator<TKey> it)
        {
            UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, it);
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
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out it);
        }

        /// <summary>
        /// Advances an iterator to the next value associated with its key.
        /// </summary>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public readonly bool TryGetNextValue(out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetNextValueAtomic(m_Buffer, out item, ref it);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present in this hash map.</returns>
        public readonly bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// Returns the number of values associated with a given key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The number of values associated with the key. Returns 0 if the key was not present.</returns>
        public readonly int CountValuesForKey(TKey key)
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
            return UnsafeParallelHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref it, ref item);
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Buffer != null;
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

            UnsafeParallelHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Creates and schedules a job that will dispose this hash map.
        /// </summary>
        /// <param name="inputDeps">A job handle. The newly scheduled job will depend upon this handle.</param>
        /// <returns>The handle of a new job that will dispose this hash map.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return inputDeps;
            }

            var jobHandle = new UnsafeParallelHashMapDisposeJob { Data = m_Buffer, Allocator = m_AllocatorLabel }.Schedule(inputDeps);
            m_Buffer = null;
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
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an array with a copy of all the values (in no particular order).
        /// </summary>
        /// <remarks>The values are not deduplicated. If you sort the returned array,
        /// you can use <see cref="Unity.Collections.NativeParallelHashMapExtensions.Unique{T}"/> to remove duplicate values.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all the values (in no particular order).</returns>
        public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all the keys and values (in no particular order).
        /// </summary>
        /// <remarks>A key with *N* values is included *N* times in the array.
        /// </remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all the keys and values (in no particular order).</returns>
        public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an enumerator over the values of an individual key.
        /// </summary>
        /// <param name="key">The key to get an enumerator for.</param>
        /// <returns>An enumerator over the values of a key.</returns>
        public Enumerator GetValuesForKey(TKey key)
        {
            return new Enumerator { hashmap = this, key = key, isFirst = true };
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
            internal UnsafeParallelMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

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
                if (isFirst)
                {
                    isFirst = false;
                    return hashmap.TryGetFirstValue(key, out value, out iterator);
                }

                return hashmap.TryGetNextValue(out value, ref iterator);
            }

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            /// The current value.
            /// </summary>
            /// <value>The current value.</value>
            public TValue Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => value;
            }

            object IEnumerator.Current => Current;

            /// <summary>
            /// Returns this enumerator.
            /// </summary>
            /// <returns>This enumerator.</returns>
            public Enumerator GetEnumerator() { return this; }
        }

        /// <summary>
        /// Returns a parallel writer for this hash map.
        /// </summary>
        /// <returns>A parallel writer for this hash map.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

            writer.m_ThreadIndex = 0;
            writer.m_Buffer = m_Buffer;

            return writer;
        }

        /// <summary>
        /// A parallel writer for an UnsafeParallelMultiHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeParallelMultiHashMap.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// Returns the number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Buffer->keyCapacity;
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
                Assert.IsTrue(m_ThreadIndex >= 0);
                UnsafeParallelHashMapBase<TKey, TValue>.AddAtomicMulti(m_Buffer, key, item, m_ThreadIndex);
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
            public void Add(TKey key, TValue item, int threadIndexOverride)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                UnsafeParallelHashMapBase<TKey, TValue>.AddAtomicMulti(m_Buffer, key, item, threadIndexOverride);
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public KeyValueEnumerator GetEnumerator()
        {
            return new KeyValueEnumerator { m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Buffer) };
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
        public struct KeyValueEnumerator : IEnumerator<KeyValue<TKey, TValue>>
        {
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
            public bool MoveNext() => m_Enumerator.MoveNext();

            /// <summary>
            /// Resets the enumerator to its initial state.
            /// </summary>
            public void Reset() => m_Enumerator.Reset();

            /// <summary>
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KeyValue<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Enumerator.GetCurrent<TKey, TValue>();
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
            return new ReadOnly(this);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeParallelHashMap. Does not have its own allocated storage.
        /// </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KeyValue<TKey, TValue>>
        {
            internal UnsafeParallelMultiHashMap<TKey, TValue> m_MultiHashMapData;

            internal ReadOnly(UnsafeParallelMultiHashMap<TKey, TValue> container)
            {
                m_MultiHashMapData = container;
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

                    return m_MultiHashMapData.IsEmpty;
                }
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count()
            {
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
                return m_MultiHashMapData.TryGetNextValue(out item, ref it);
            }

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool ContainsKey(TKey key)
            {
                return m_MultiHashMapData.ContainsKey(key);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_MultiHashMapData.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
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
                return m_MultiHashMapData.GetKeyValueArrays(allocator);
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <remarks>A key with *N* values is visited by the enumerator *N* times.</remarks>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public KeyValueEnumerator GetEnumerator()
            {
                return new KeyValueEnumerator
                {
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
    }

    internal sealed class UnsafeParallelMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
        where TValue : unmanaged
    {
        UnsafeParallelMultiHashMap<TKey, TValue> m_Target;

        public UnsafeParallelMultiHashMapDebuggerTypeProxy(UnsafeParallelMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public static (NativeArray<TKey>, int) GetUniqueKeyArray(ref UnsafeParallelMultiHashMap<TKey, TValue> hashMap, AllocatorManager.AllocatorHandle allocator)
        {
            var withDuplicates = hashMap.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return (withDuplicates, uniques);
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = GetUniqueKeyArray(ref m_Target, Allocator.Temp);

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
}
