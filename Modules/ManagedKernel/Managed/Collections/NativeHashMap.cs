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
using Unity.Jobs;

namespace Unity.Collections
{
    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct NativeHashMapDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeHashMap<int, int>* m_HashMapData;
        internal AllocatorManager.AllocatorHandle m_Allocator;

        internal AtomicSafetyHandle m_Safety;

        internal void Dispose()
        {
            var hashMapData = (HashMapHelper<int>*)m_HashMapData;
            HashMapHelper<int>.Free(hashMapData);
        }
    }

    [BurstCompile]
    internal unsafe struct NativeHashMapDisposeJob : IJob
    {
        internal NativeHashMapDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }

    /// <summary>
    /// A key-value pair.
    /// </summary>
    /// <remarks>Used for enumerators.</remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct KVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal HashMapHelper<TKey>* m_Data;
        internal int m_Index;
        internal int m_Next;

        /// <summary>
        ///  An invalid KeyValue.
        /// </summary>
        /// <value>In a hash map enumerator's initial state, its <see cref="UnsafeHashMap{TKey,TValue}.Enumerator.Current"/> value is Null.</value>
        public static KVPair<TKey, TValue> Null => new KVPair<TKey, TValue> { m_Index = -1 };

        /// <summary>
        /// The key.
        /// </summary>
        /// <value>The key. If this KeyValue is Null, returns the default of TKey.</value>
        public TKey Key
        {
            get
            {
                if (m_Index != -1)
                {
                    return m_Data->Keys[m_Index];
                }

                return default;
            }
        }

        /// <summary>
        /// Value of key/value pair.
        /// </summary>
        public ref TValue Value
        {
            get
            {
                if (m_Index == -1)
                    throw new ArgumentException("must be valid");

                return ref UnsafeUtility.AsRef<TValue>(m_Data->Ptr + sizeof(TValue) * m_Index);
            }
        }

        /// <summary>
        /// Gets the key and the value.
        /// </summary>
        /// <param name="key">Outputs the key. If this KeyValue is Null, outputs the default of TKey.</param>
        /// <param name="value">Outputs the value. If this KeyValue is Null, outputs the default of TValue.</param>
        /// <returns>True if the key-value pair is valid.</returns>
        public bool GetKeyValue(out TKey key, out TValue value)
        {
            if (m_Index != -1)
            {
                key = m_Data->Keys[m_Index];
                value = UnsafeUtility.ReadArrayElement<TValue>(m_Data->Ptr, m_Index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }

    /// <summary>
    /// An unordered, expandable associative array.
    /// </summary>
    /// <remarks>
    /// Not suitable for parallel write access. Use <see cref="NativeParallelHashMap{TKey, TValue}"/> instead.
    /// </remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct NativeHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal HashMapHelper<TKey>* m_Data;

        internal AtomicSafetyHandle m_Safety;
        static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeHashMap<TKey, TValue>>();

        /// <summary>
        /// Initializes and returns an instance of UnsafeHashMap.
        /// </summary>
        /// <param name="initialCapacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public NativeHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = HashMapHelper<TKey>.Alloc(initialCapacity, sizeof(TValue), HashMapHelper<TKey>.kMinCapacity, allocator);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<TKey>() || UnsafeUtility.IsNativeContainerType<TValue>())
                AtomicSafetyHandle.SetNestedContainer(m_Safety, true);

            CollectionHelper.SetStaticSafetyId<NativeHashMap<TKey, TValue>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
        }

        /// <summary>
        /// Releases all resources (memory).
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

            HashMapHelper<TKey>.Free(m_Data);
            m_Data = null;
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

            var jobHandle = new NativeHashMapDisposeJob { Data = new NativeHashMapDispose { m_HashMapData = (UnsafeHashMap<int, int>*)m_Data, m_Safety = m_Safety } }.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_Data = null;

            return jobHandle;
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data != null && m_Data->IsCreated;
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
                return m_Data->IsEmpty;
            }
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckRead();
                return m_Data->Count;
            }
        }

        /// <summary>
        /// The number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CheckRead();
                return m_Data->Capacity;
            }

            set
            {
                CheckWrite();
                m_Data->Resize(value);
            }
        }

        /// <summary>
        /// The maximum number of elements this type of container can hold.
        /// </summary>
        public const int MaxCapacity = HashMapHelper<TKey>.kMaxCapacity;

        /// <summary>
        /// Removes all key-value pairs.
        /// </summary>
        /// <remarks>Does not change the capacity.</remarks>
        public void Clear()
        {
            CheckWrite();
            m_Data->Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the key-value pair was added.</returns>
        public bool TryAdd(TKey key, TValue item)
        {
            CheckWrite();

            var idx = m_Data->TryAdd(key);
            if (-1 != idx)
            {
                UnsafeUtility.WriteArrayElement(m_Data->Ptr, idx, item);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
        /// <exception cref="InvalidOperationException">Thrown if there is not enough Capacity to contain the added element, and the container can't be resized.</exception>
        public void Add(TKey key, TValue item)
        {
            CheckWrite();

            var idx = m_Data->TryAdd(key);
            if (-1 != idx)
            {
                UnsafeUtility.WriteArrayElement(m_Data->Ptr, idx, item);
                return;
            }

            if (Capacity == MaxCapacity)
            {
                ThrowAtMaxCapacity();
                return;
            }

            ThrowKeyAlreadyAdded(key);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(TKey key)
        {
            CheckWrite();
            return -1 != m_Data->TryRemove(key);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            CheckRead();
            return m_Data->TryGetValue(key, out item);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            CheckRead();
            return -1 != m_Data->Find(key);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            CheckWrite();
            m_Data->TrimExcess();
        }

        /// <summary>
        /// Gets and sets values by key.
        /// </summary>
        /// <remarks>Getting a key that is not present will throw. Setting a key that is not already present will add the key.</remarks>
        /// <param name="key">The key to look up.</param>
        /// <value>The value associated with the key.</value>
        /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
        public TValue this[TKey key]
        {
            get
            {
                CheckRead();

                TValue result;
                if (!m_Data->TryGetValue(key, out result))
                {
                    ThrowKeyNotPresent(key);
                }

                return result;
            }

            set
            {
                CheckWrite();

                var idx = m_Data->Find(key);
                if (-1 == idx)
                {
                    TryAdd(key, value);
                    return;
                }

                UnsafeUtility.WriteArrayElement(m_Data->Ptr, idx, value);
            }
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_Data->GetKeyArray(allocator);
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_Data->GetValueArray<TValue>(allocator);
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            CheckRead();
            return m_Data->GetKeyValueArrays<TValue>(allocator);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
            var ash = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref ash);
            return new Enumerator
            {
                m_Safety = ash,
                m_Enumerator = new HashMapHelper<TKey>.Enumerator(m_Data),
            };
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
        /// </summary>
        /// <returns>Throws NotImplementedException.</returns>
        /// <exception cref="NotImplementedException">Method is not implemented.</exception>
        IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
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
        /// An enumerator over the key-value pairs of a container.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        public struct Enumerator : IEnumerator<KVPair<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            internal HashMapHelper<TKey>.Enumerator m_Enumerator;
            internal AtomicSafetyHandle m_Safety;

            /// <summary>
            /// Does nothing.
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// Advances the enumerator to the next key-value pair.
            /// </summary>
            /// <returns>True if <see cref="Current"/> is valid to read after the call.</returns>
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
            /// The current key-value pair.
            /// </summary>
            /// <value>The current key-value pair.</value>
            public KVPair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Enumerator.GetCurrent<TValue>();
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator in the container.
            /// </summary>
            object IEnumerator.Current => Current;
        }

        /// <summary>
        /// Returns a readonly version of this NativeHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the NativeHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref this);
        }

        /// <summary>
        /// A read-only alias for the value of a NativeHashMap. Does not have its own allocated storage.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsReadOnly]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KVPair<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            internal HashMapHelper<TKey>* m_Data;

            AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<ReadOnly>();

            internal ReadOnly(ref NativeHashMap<TKey, TValue> data)
            {
                m_Data = data.m_Data;
                m_Safety = data.m_Safety;
                CollectionHelper.SetStaticSafetyId<ReadOnly>(ref m_Safety, ref s_staticSafetyId.Data);
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data != null && m_Data->IsCreated;
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
                    return m_Data->IsEmpty;
                }
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckRead();
                    return m_Data->Count;
                }
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
                    return m_Data->Capacity;
                }
            }

            /// <summary>
            /// Returns the value associated with a key.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool TryGetValue(TKey key, out TValue item)
            {
                CheckRead();
                return m_Data->TryGetValue(key, out item);
            }

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool ContainsKey(TKey key)
            {
                CheckRead();
                return -1 != m_Data->Find(key);
            }

            /// <summary>
            /// Gets values by key.
            /// </summary>
            /// <remarks>Getting a key that is not present will throw.</remarks>
            /// <param name="key">The key to look up.</param>
            /// <value>The value associated with the key.</value>
            /// <exception cref="ArgumentException">For getting, thrown if the key was not present.</exception>
            public readonly TValue this[TKey key]
            {
                get
                {
                    CheckRead();

                    TValue result;
                    if (!m_Data->TryGetValue(key, out result))
                    {
                        ThrowKeyNotPresent(key);
                    }

                    return result;
                }
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_Data->GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                CheckRead();
                return m_Data->GetValueArray<TValue>(allocator);
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
                return m_Data->GetKeyValueArrays<TValue>(allocator);
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public readonly Enumerator GetEnumerator()
            {
                AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(m_Safety);
                var ash = m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref ash);
                return new Enumerator
                {
                    m_Safety = ash,
                    m_Enumerator = new HashMapHelper<TKey>.Enumerator(m_Data),
                };
            }

            /// <summary>
            /// This method is not implemented. Use <see cref="GetEnumerator"/> instead.
            /// </summary>
            /// <returns>Throws NotImplementedException.</returns>
            /// <exception cref="NotImplementedException">Method is not implemented.</exception>
            IEnumerator<KVPair<TKey, TValue>> IEnumerable<KVPair<TKey, TValue>>.GetEnumerator()
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

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void ThrowKeyNotPresent(TKey key)
            {
                throw new ArgumentException($"Key: {key} is not present.");
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void ThrowKeyAlreadyAdded(TKey key)
        {
            throw new ArgumentException($"An item with the same key has already been added: {key}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void ThrowAtMaxCapacity()
        {
            throw new InvalidOperationException($"Capacity is insufficient, and resize would fail (Capacity {Capacity} / {MaxCapacity}, Count {Count})!");
        }
    }

    internal unsafe sealed class NativeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        HashMapHelper<TKey>* Data;

        public NativeHashMapDebuggerTypeProxy(NativeHashMap<TKey, TValue> target)
        {
            Data = target.m_Data;
        }

        public NativeHashMapDebuggerTypeProxy(NativeHashMap<TKey, TValue>.ReadOnly target)
        {
            Data = target.m_Data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                if (Data == null)
                {
                    return default;
                }

                var result = new List<Pair<TKey, TValue>>();
                using (var kva = Data->GetKeyValueArrays<TValue>(Allocator.Temp))
                {
                    for (var i = 0; i < kva.Length; ++i)
                    {
                        result.Add(new Pair<TKey, TValue>(kva.Keys[i], kva.Values[i]));
                    }
                }
                return result;
            }
        }
    }
}
