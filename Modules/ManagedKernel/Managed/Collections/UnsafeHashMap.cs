// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using Unity.Jobs;
using System.Runtime.InteropServices;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    internal unsafe struct HashMapHelper<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte* Ptr;

        [NativeDisableUnsafePtrRestriction]
        internal TKey* Keys;

        [NativeDisableUnsafePtrRestriction]
        internal int* Next;

        [NativeDisableUnsafePtrRestriction]
        internal int* Buckets;

        internal int Count;
        internal int Capacity;
        internal int Log2MinGrowth;
        internal int BucketCapacity;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;
        internal int SizeOfTValue;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal const int kMinCapacity = 256;
        internal const int kMaxCapacity = 2 << 28;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int CalcCapacityCeilPow2(int capacity)
        {
            capacity = math.max(math.max(1, Count), capacity);
            int newCapacity = math.max(capacity, 1 << Log2MinGrowth);
            int result = math.ceilpow2(newCapacity);

            return result;
        }

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr != null;
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || Count == 0;
        }

        internal void Clear()
        {
            UnsafeUtility.MemSet(Buckets, 0xff, (long)BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet(Next, 0xff, (long)Capacity * sizeof(int));

            Count = 0;
            FirstFreeIdx = -1;
            AllocatedIndex = 0;
        }

        internal void Init(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            CheckCapacity(capacity);

            Count = 0;
            Log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));

            capacity = CalcCapacityCeilPow2(capacity);
            Capacity = capacity;
            BucketCapacity = GetBucketSize(capacity);
            Allocator = allocator;
            SizeOfTValue = sizeOfValueT;

            long keyOffset, nextOffset, bucketOffset;
            long totalSize = CalculateDataSize(capacity, BucketCapacity, sizeOfValueT, out keyOffset, out nextOffset, out bucketOffset);

            Ptr = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, allocator);
            Keys = (TKey*)(Ptr + keyOffset);
            Next = (int*)(Ptr + nextOffset);
            Buckets = (int*)(Ptr + bucketOffset);

            Clear();
        }

        internal void Dispose()
        {
            Memory.Unmanaged.Free(Ptr, Allocator);
            Ptr = null;
            Keys = null;
            Next = null;
            Buckets = null;
            Count = 0;
            BucketCapacity = 0;
        }

        internal static HashMapHelper<TKey>* Alloc(int capacity, int sizeOfValueT, int minGrowth, AllocatorManager.AllocatorHandle allocator)
        {
            var data = (HashMapHelper<TKey>*)Memory.Unmanaged.Allocate(sizeof(HashMapHelper<TKey>), UnsafeUtility.AlignOf<HashMapHelper<TKey>>(), allocator);
            data->Init(capacity, sizeOfValueT, minGrowth, allocator);

            return data;
        }

        internal static void Free(HashMapHelper<TKey>* data)
        {
            if (data == null)
            {
                throw new InvalidOperationException("Hash based container has yet to be created or has been destroyed!");
            }
            data->Dispose();

            Memory.Unmanaged.Free(data, data->Allocator);
        }

        internal void Resize(int newCapacity)
        {
            CheckCapacity(newCapacity);

            newCapacity = math.max(newCapacity, Count);
            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            if (Capacity == newCapacity && BucketCapacity == newBucketCapacity)
            {
                return;
            }

            ResizeExact(newCapacity, newBucketCapacity);
        }

        internal void ResizeExact(int newCapacity, int newBucketCapacity)
        {
            long keyOffset, nextOffset, bucketOffset;
            long totalSize = CalculateDataSize(newCapacity, newBucketCapacity, SizeOfTValue, out keyOffset, out nextOffset, out bucketOffset);

            var oldPtr = Ptr;
            var oldKeys = Keys;
            var oldNext = Next;
            var oldBuckets = Buckets;
            var oldBucketCapacity = BucketCapacity;

            Ptr = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, Allocator);
            Keys = (TKey*)(Ptr + keyOffset);
            Next = (int*)(Ptr + nextOffset);
            Buckets = (int*)(Ptr + bucketOffset);
            Capacity = newCapacity;
            BucketCapacity = newBucketCapacity;

            Clear();

            for (int i = 0, num = oldBucketCapacity; i < num; ++i)
            {
                for (int idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
                {
                    var newIdx = TryAdd(oldKeys[idx]);
                    UnsafeUtility.MemCpy(Ptr + SizeOfTValue * newIdx, oldPtr + SizeOfTValue * idx, SizeOfTValue);
                }
            }

            Memory.Unmanaged.Free(oldPtr, Allocator);
        }

        internal void TrimExcess()
        {
            var capacity = CalcCapacityCeilPow2(Count);
            ResizeExact(capacity, GetBucketSize(capacity));
        }

        internal static long CalculateDataSize(int capacity, int bucketCapacity, int sizeOfTValue, out long outKeyOffset, out long outNextOffset, out long outBucketOffset)
        {
            long sizeOfTKey = sizeof(TKey);
            long sizeOfInt = sizeof(int);

            long valuesSize = (long)sizeOfTValue * capacity;
            long keysSize = sizeOfTKey * capacity;
            long nextSize = sizeOfInt * capacity;
            long bucketSize = sizeOfInt * bucketCapacity;
            long totalSize = valuesSize + keysSize + nextSize + bucketSize;

            outKeyOffset = 0 + valuesSize;
            outNextOffset = outKeyOffset + keysSize;
            outBucketOffset = outNextOffset + nextSize;

            return totalSize;
        }

        internal readonly int GetCount()
        {
            if (AllocatedIndex <= 0)
            {
                return 0;
            }

            var numFree = 0;

            for (var freeIdx = FirstFreeIdx; freeIdx >= 0; freeIdx = Next[freeIdx])
            {
                ++numFree;
            }

            return math.min(Capacity, AllocatedIndex) - numFree;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetBucket(in TKey key)
        {
            return (int)((uint)key.GetHashCode() & (BucketCapacity - 1));
        }

        internal int TryAdd(in TKey key)
        {
            if (-1 == Find(key))
            {
                // Allocate an entry from the free list
                int idx;
                int* next;

                if (AllocatedIndex >= Capacity && FirstFreeIdx < 0)
                {
                    if (Capacity == kMaxCapacity)
                    {
                        return -1;
                    }

                    int newCap = CalcCapacityCeilPow2(Capacity + (1 << Log2MinGrowth));
                    Resize(newCap);
                }

                idx = FirstFreeIdx;

                if (idx >= 0)
                {
                    FirstFreeIdx = Next[idx];
                }
                else
                {
                    idx = AllocatedIndex++;
                }

                CheckIndexOutOfBounds(idx);

                UnsafeUtility.WriteArrayElement(Keys, idx, key);
                var bucket = GetBucket(key);

                // Add the index to the hash-map
                next = Next;
                next[idx] = Buckets[bucket];
                Buckets[bucket] = idx;
                Count++;

                return idx;
            }

            return -1;
        }

        internal int Find(TKey key)
        {
            if (AllocatedIndex > 0)
            {
                // First find the slot based on the hash
                var bucket = GetBucket(key);
                var entryIdx = Buckets[bucket];

                if ((uint)entryIdx < (uint)Capacity)
                {
                    var nextPtrs = Next;
                    while (!UnsafeUtility.ReadArrayElement<TKey>(Keys, entryIdx).Equals(key))
                    {
                        entryIdx = nextPtrs[entryIdx];
                        if ((uint)entryIdx >= (uint)Capacity)
                        {
                            return -1;
                        }
                    }

                    return entryIdx;
                }
            }

            return -1;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            var idx = Find(key);

            if (-1 != idx)
            {
                item = UnsafeUtility.ReadArrayElement<TValue>(Ptr, idx);
                return true;
            }

            item = default;
            return false;
        }

        internal int TryRemove(TKey key)
        {
            if (Capacity != 0)
            {
                var removed = 0;

                // First find the slot based on the hash
                var bucket = GetBucket(key);

                var prevEntry = -1;
                var entryIdx = Buckets[bucket];

                while (entryIdx >= 0 && entryIdx < Capacity)
                {
                    if (UnsafeUtility.ReadArrayElement<TKey>(Keys, entryIdx).Equals(key))
                    {
                        ++removed;

                        // Found matching element, remove it
                        if (prevEntry < 0)
                        {
                            Buckets[bucket] = Next[entryIdx];
                        }
                        else
                        {
                            Next[prevEntry] = Next[entryIdx];
                        }

                        // And free the index
                        int nextIdx = Next[entryIdx];
                        Next[entryIdx] = FirstFreeIdx;
                        FirstFreeIdx = entryIdx;
                        entryIdx = nextIdx;

                        break;
                    }
                    else
                    {
                        prevEntry = entryIdx;
                        entryIdx = Next[entryIdx];
                    }
                }

                Count -= removed;
                return 0 != removed ? removed : -1;
            }

            return -1;
        }

        internal bool MoveNextSearch(ref int bucketIndex, ref int nextIndex, out int index)
        {
            for (int i = bucketIndex, num = BucketCapacity; i < num; ++i)
            {
                var idx = Buckets[i];

                if (idx != -1)
                {
                    index = idx;
                    bucketIndex = i + 1;
                    nextIndex = Next[idx];

                    return true;
                }
            }

            index = -1;
            bucketIndex = BucketCapacity;
            nextIndex = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext(ref int bucketIndex, ref int nextIndex, out int index)
        {
            if (nextIndex != -1)
            {
                index = nextIndex;
                nextIndex = Next[nextIndex];
                return true;
            }

            return MoveNextSearch(ref bucketIndex, ref nextIndex, out index);
        }

        internal NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity
                ; i < capacity && count < max
                ; ++i
                )
            {
                int bucket = Buckets[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(Keys, bucket);
                    bucket = Next[bucket];
                }
            }

            return result;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal NativeArray<TValue> GetValueArray<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity
                ; i < capacity && count < max
                ; ++i
                )
            {
                int bucket = Buckets[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TValue>(Ptr, bucket);
                    bucket = Next[bucket];
                }
            }

            return result;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        internal NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays<TValue>(AllocatorManager.AllocatorHandle allocator)
            where TValue : unmanaged
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Count, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0, count = 0, max = result.Length, capacity = BucketCapacity
                ; i < capacity && count < max
                ; ++i
                )
            {
                int bucket = Buckets[i];

                while (bucket != -1)
                {
                    result.Keys[count] = UnsafeUtility.ReadArrayElement<TKey>(Keys, bucket);
                    result.Values[count] = UnsafeUtility.ReadArrayElement<TValue>(Ptr, bucket);
                    count++;
                    bucket = Next[bucket];
                }
            }

            return result;
        }

        internal unsafe struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            internal HashMapHelper<TKey>* m_Data;
            internal int m_Index;
            internal int m_BucketIndex;
            internal int m_NextIndex;

            internal unsafe Enumerator(HashMapHelper<TKey>* data)
            {
                m_Data = data;
                m_Index = -1;
                m_BucketIndex = 0;
                m_NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                return m_Data->MoveNext(ref m_BucketIndex, ref m_NextIndex, out m_Index);
            }

            internal void Reset()
            {
                m_Index = -1;
                m_BucketIndex = 0;
                m_NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KVPair<TKey, TValue> GetCurrent<TValue>()
                where TValue : unmanaged
            {
                return new KVPair<TKey, TValue> { m_Data = m_Data, m_Index = m_Index };
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal TKey GetCurrentKey()
            {
                if (m_Index != -1)
                {
                    return m_Data->Keys[m_Index];
                }

                return default;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)Capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CheckCapacity(int capacity)
        {
            if (capacity > kMaxCapacity)
            {
                throw new ArgumentException($"Capacity {capacity} value too large. Maximum capacity is {kMaxCapacity}.");
            }
        }
    }

    /// <summary>
    /// An unordered, expandable associative array.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KVPair<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal HashMapHelper<TKey> m_Data;

        /// <summary>
        /// Initializes and returns an instance of UnsafeHashMap.
        /// </summary>
        /// <param name="initialCapacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeHashMap(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = default;
            m_Data.Init(initialCapacity, sizeof(TValue), HashMapHelper<TKey>.kMinCapacity, allocator);
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

            m_Data.Dispose();
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

            var jobHandle = new UnsafeDisposeJob { Ptr = m_Data.Ptr, Allocator = m_Data.Allocator }.Schedule(inputDeps);
            m_Data = default;

            return jobHandle;
        }

        /// <summary>
        /// Whether this hash map has been allocated (and not yet deallocated).
        /// </summary>
        /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.IsCreated;
        }

        /// <summary>
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or if the map has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.IsEmpty;
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Data.Count;
        }

        /// <summary>
        /// The number of key-value pairs that fit in the current allocation.
        /// </summary>
        /// <value>The number of key-value pairs that fit in the current allocation.</value>
        /// <param name="value">A new capacity. Must be larger than the current capacity.</param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => m_Data.Capacity;
            set => m_Data.Resize(value);
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
            m_Data.Clear();
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
            var idx = m_Data.TryAdd(key);
            if (-1 != idx)
            {
                UnsafeUtility.WriteArrayElement(m_Data.Ptr, idx, item);
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
            var idx = m_Data.TryAdd(key);
            if (-1 != idx)
            {
                UnsafeUtility.WriteArrayElement(m_Data.Ptr, idx, item);
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
            return -1 != m_Data.TryRemove(key);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            return m_Data.TryGetValue(key, out item);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            return -1 != m_Data.Find(key);
        }

        /// <summary>
        /// Sets the capacity to match what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess() => m_Data.TrimExcess();

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
                TValue result;
                if (!m_Data.TryGetValue(key, out result))
                {
                    ThrowKeyNotPresent(key);
                }

                return result;
            }

            set
            {
                var idx = m_Data.Find(key);
                if (-1 != idx)
                {
                    UnsafeUtility.WriteArrayElement(m_Data.Ptr, idx, value);
                    return;
                }

                TryAdd(key, value);
            }
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyArray(allocator);

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetValueArray<TValue>(allocator);

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyValueArrays<TValue>(allocator);

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
            //            return new Enumerator { Data = m_Data, Index = -1 };
            fixed (HashMapHelper<TKey>* data = &m_Data)
            {
                return new Enumerator { m_Enumerator = new HashMapHelper<TKey>.Enumerator(data) };
            }
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
        public struct Enumerator : IEnumerator<KVPair<TKey, TValue>>
        {
            internal HashMapHelper<TKey>.Enumerator m_Enumerator;

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
        /// Returns a readonly version of this UnsafeHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the UnsafeHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(ref m_Data);
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeHashMap. Does not have its own allocated storage.
        /// </summary>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KVPair<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            internal HashMapHelper<TKey> m_Data;

            internal ReadOnly(ref HashMapHelper<TKey> data)
            {
                m_Data = data;
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsCreated;
            }

            /// <summary>
            /// Whether this hash map is empty.
            /// </summary>
            /// <value>True if this hash map is empty or if the map has not been constructed.</value>
            public readonly bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.IsEmpty;
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.Count;
            }

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Data.Capacity;
            }

            /// <summary>
            /// Returns the value associated with a key.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool TryGetValue(TKey key, out TValue item) => m_Data.TryGetValue(key, out item);

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool ContainsKey(TKey key)
            {
                return -1 != m_Data.Find(key);
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
                    TValue result;
                    m_Data.TryGetValue(key, out result);
                    return result;
                }
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyArray(allocator);

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator) => m_Data.GetValueArray<TValue>(allocator);

            /// <summary>
            /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
            /// </summary>
            /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
            public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator) => m_Data.GetKeyValueArrays<TValue>(allocator);

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public readonly Enumerator GetEnumerator()
            {
                fixed (HashMapHelper<TKey>* data = &m_Data)
                {
                    return new Enumerator { m_Enumerator = new HashMapHelper<TKey>.Enumerator(data) };
                }
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

    internal sealed class UnsafeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        HashMapHelper<TKey> Data;

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue> target)
        {
            Data = target.m_Data;
        }

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue>.ReadOnly target)
        {
            Data = target.m_Data;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using (var kva = Data.GetKeyValueArrays<TValue>(Allocator.Temp))
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
