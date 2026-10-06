// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using ManagedKernel.Assertions;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// A bucket of key-value pairs. Used as the internal storage for hash maps.
    /// </summary>
    /// <remarks>Exposed publicly only for advanced use cases.</remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct UnsafeParallelHashMapBucketData
    {
        internal UnsafeParallelHashMapBucketData(byte* v, byte* k, byte* n, byte* b, int bcm)
        {
            values = v;
            keys = k;
            next = n;
            buckets = b;
            bucketCapacityMask = bcm;
        }

        /// <summary>
        /// The buffer of values.
        /// </summary>
        /// <value>The buffer of values.</value>
        public readonly byte* values;

        /// <summary>
        /// The buffer of keys.
        /// </summary>
        /// <value>The buffer of keys.</value>
        public readonly byte* keys;

        /// <summary>
        /// The next bucket in the chain.
        /// </summary>
        /// <value>The next bucket in the chain.</value>
        public readonly byte* next;

        /// <summary>
        /// The first bucket in the chain.
        /// </summary>
        /// <value>The first bucket in the chain.</value>
        public readonly byte* buckets;

        /// <summary>
        /// One less than the bucket capacity.
        /// </summary>
        /// <value>One less than the bucket capacity.</value>
        public readonly int bucketCapacityMask;
    }

    [StructLayout(LayoutKind.Explicit)]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeParallelHashMapData
    {
        [FieldOffset(0)]
        internal byte* values;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        internal byte* keys;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(16)]
        internal byte* next;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(24)]
        internal byte* buckets;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(32)]
        internal int keyCapacity;

        [FieldOffset(36)]
        internal int bucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int allocatedIndexLength;

        internal const int kMaxCapacity = int.MaxValue / 2;

        const int kFirstFreeTLSOffset = JobsUtility.CacheLineSize < 64 ? 64 : JobsUtility.CacheLineSize;
        internal int* firstFreeTLS => (int*)((byte*)UnsafeUtility.AddressOf(ref this) + kFirstFreeTLSOffset);

        // 64 is the cache line size on x86, arm usually has 32 - so it is possible to save some memory there
        internal const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        internal static long GetBucketSize(int capacity)
        {
            return (long)capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        internal static void AllocateHashMap<TKey, TValue>(int length, long bucketLength, AllocatorManager.AllocatorHandle label,
            out UnsafeParallelHashMapData* outBuf)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            CheckCapacity(length);

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            // Calculate the size of UnsafeParallelHashMapData since we need to account for how many
            // jow worker threads the runtime has available. -1 since UnsafeParallelHashMapData.firstFreeTLS accounts for 1 int already
            Assert.IsTrue(sizeof(UnsafeParallelHashMapData) <= kFirstFreeTLSOffset);
            int hashMapDataSize = kFirstFreeTLSOffset + (sizeof(int) * IntsPerCacheLine * maxThreadCount);
            UnsafeParallelHashMapData* data = (UnsafeParallelHashMapData*)Memory.Unmanaged.Allocate(hashMapDataSize, JobsUtility.CacheLineSize, label);

            bucketLength = math.ceilpow2(bucketLength);

            data->keyCapacity = length;
            data->bucketCapacityMask = (int)(bucketLength - 1);

            long keyOffset, nextOffset, bucketOffset;
            long totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out keyOffset, out nextOffset, out bucketOffset);

            data->values = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            data->keys = data->values + keyOffset;
            data->next = data->values + nextOffset;
            data->buckets = data->values + bucketOffset;

            outBuf = data;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        internal static void ReallocateHashMap<TKey, TValue>(UnsafeParallelHashMapData* data, int newCapacity, long newBucketCapacity, AllocatorManager.AllocatorHandle label)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            CheckHashMapReallocateDoesNotShrink(data, newCapacity);

            long keyOffset, nextOffset, bucketOffset;
            long totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out keyOffset, out nextOffset, out bucketOffset);

            byte* newData = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, label);
            byte* newKeys = newData + keyOffset;
            byte* newNext = newData + nextOffset;
            byte* newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->values, (long)data->keyCapacity * sizeof(TValue));
            UnsafeUtility.MemCpy(newKeys, data->keys, (long)data->keyCapacity * sizeof(TKey));
            UnsafeUtility.MemCpy(newNext, data->next, (long)data->keyCapacity * sizeof(int));

            for (int emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            int newBucketCapacityMask = (int)(newBucketCapacity - 1);

            for (int bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
            {
                int* buckets = (int*)data->buckets;
                int* nextPtrs = (int*)newNext;
                while (buckets[bucket] >= 0)
                {
                    int curEntry = buckets[bucket];
                    buckets[bucket] = nextPtrs[curEntry];
                    int newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() & newBucketCapacityMask;
                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            Memory.Unmanaged.Free(data->values, label);
            if (data->allocatedIndexLength > data->keyCapacity)
            {
                data->allocatedIndexLength = data->keyCapacity;
            }

            data->values = newData;
            data->keys = newKeys;
            data->next = newNext;
            data->buckets = newBuckets;
            data->keyCapacity = newCapacity;
            data->bucketCapacityMask = newBucketCapacityMask;
        }

        internal static void DeallocateHashMap(UnsafeParallelHashMapData* data, AllocatorManager.AllocatorHandle allocator)
        {
            Memory.Unmanaged.Free(data->values, allocator);
            Memory.Unmanaged.Free(data, allocator);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        internal static long CalculateDataSize<TKey, TValue>(int length, long bucketLength, out long keyOffset, out long nextOffset, out long bucketOffset)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            long sizeOfTValue = sizeof(TValue);
            long sizeOfTKey = sizeof(TKey);
            long sizeOfInt = sizeof(int);

            long valuesSize = CollectionHelper.Align(sizeOfTValue * length, JobsUtility.CacheLineSize);
            long keysSize = CollectionHelper.Align(sizeOfTKey * length, JobsUtility.CacheLineSize);
            long nextSize = CollectionHelper.Align(sizeOfInt * length, JobsUtility.CacheLineSize);
            long bucketSize = CollectionHelper.Align(sizeOfInt * bucketLength, JobsUtility.CacheLineSize);
            long totalSize = valuesSize + keysSize + nextSize + bucketSize;

            keyOffset = 0 + valuesSize;
            nextOffset = keyOffset + keysSize;
            bucketOffset = nextOffset + nextSize;

            return totalSize;
        }

        internal static bool IsEmpty(UnsafeParallelHashMapData* data)
        {
            if (data->allocatedIndexLength <= 0)
            {
                return true;
            }

            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;
            var capacityMask = data->bucketCapacityMask;

            for (int i = 0; i <= capacityMask; ++i)
            {
                int bucket = bucketArray[i];

                if (bucket != -1)
                {
                    return false;
                }
            }

            return true;
        }

        internal static int GetCount(UnsafeParallelHashMapData* data)
        {
            if (data->allocatedIndexLength <= 0)
            {
                return 0;
            }

            var bucketNext = (int*)data->next;
            var freeListSize = 0;

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            for (int tls = 0; tls < maxThreadCount; ++tls)
            {
                for (var freeIdx = data->firstFreeTLS[tls * IntsPerCacheLine]
                    ; freeIdx >= 0
                    ; freeIdx = bucketNext[freeIdx]
                )
                {
                    ++freeListSize;
                }
            }

            return math.min(data->keyCapacity, data->allocatedIndexLength) - freeListSize;
        }

        internal static bool MoveNextSearch(UnsafeParallelHashMapData* data, ref int bucketIndex, ref int nextIndex, out int index)
        {
            var bucketArray = (int*)data->buckets;
            var capacityMask = data->bucketCapacityMask;
            for (int i = bucketIndex; i <= capacityMask; ++i)
            {
                var idx = bucketArray[i];

                if (idx != -1)
                {
                    var bucketNext = (int*)data->next;
                    index = idx;
                    bucketIndex = i + 1;
                    nextIndex = bucketNext[idx];

                    return true;
                }
            }

            index = -1;
            bucketIndex = capacityMask + 1;
            nextIndex = -1;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool MoveNext(UnsafeParallelHashMapData* data, ref int bucketIndex, ref int nextIndex, out int index)
        {
            if (nextIndex != -1)
            {
                var bucketNext = (int*)data->next;
                index = nextIndex;
                nextIndex = bucketNext[nextIndex];
                return true;
            }

            return MoveNextSearch(data, ref bucketIndex, ref nextIndex, out index);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        internal static void GetKeyArray<TKey>(UnsafeParallelHashMapData* data, NativeArray<TKey> result)
            where TKey : unmanaged
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            for (int i = 0, count = 0, max = result.Length; i <= data->bucketCapacityMask && count < max; ++i)
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        internal static void GetValueArray<TValue>(UnsafeParallelHashMapData* data, NativeArray<TValue> result)
            where TValue : unmanaged
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            for (int i = 0, count = 0, max = result.Length, capacityMask = data->bucketCapacityMask
                ; i <= capacityMask && count < max
                ; ++i
                )
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result[count++] = UnsafeUtility.ReadArrayElement<TValue>(data->values, bucket);
                    bucket = bucketNext[bucket];
                }
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        internal static void GetKeyValueArrays<TKey, TValue>(UnsafeParallelHashMapData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : unmanaged
            where TValue : unmanaged
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            for (int i = 0, count = 0, max = result.Length, capacityMask = data->bucketCapacityMask
                ; i <= capacityMask && count < max
                ; ++i
                )
            {
                int bucket = bucketArray[i];

                while (bucket != -1)
                {
                    result.Keys[count] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, bucket);
                    result.Values[count] = UnsafeUtility.ReadArrayElement<TValue>(data->values, bucket);
                    count++;
                    bucket = bucketNext[bucket];
                }
            }
        }

        internal UnsafeParallelHashMapBucketData GetBucketData()
        {
            return new UnsafeParallelHashMapBucketData(values, keys, next, buckets, bucketCapacityMask);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckHashMapReallocateDoesNotShrink(UnsafeParallelHashMapData* data, int newCapacity)
        {
            if (data->keyCapacity > newCapacity)
                throw new InvalidOperationException("Shrinking a hash map is not supported");
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

    [NativeContainer]
    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeParallelHashMapDataDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        internal AtomicSafetyHandle m_Safety;

        public void Dispose()
        {
            UnsafeParallelHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
        }
    }

    [BurstCompile]
    internal unsafe struct UnsafeParallelHashMapDataDisposeJob : IJob
    {
        internal UnsafeParallelHashMapDataDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    internal struct UnsafeParallelHashMapBase<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal static unsafe void Clear(UnsafeParallelHashMapData* data)
        {
            UnsafeUtility.MemSet(data->buckets, 0xff, ((long)data->bucketCapacityMask + 1) * 4);
            UnsafeUtility.MemSet(data->next, 0xff, ((long)data->keyCapacity) * 4);

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            for (int tls = 0; tls < maxThreadCount; ++tls)
            {
                data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] = -1;
            }

            data->allocatedIndexLength = 0;
        }

        private const int SentinelRefilling = -2;
        private const int SentinelSwapInProgress = -3;
        internal static unsafe int AllocEntry(UnsafeParallelHashMapData* data, int threadIndex)
        {
            int idx;
            int* nextPtrs = (int*)data->next;

            do
            {
                do
                {
                    idx = Volatile.Read(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine]);
                } while (idx == SentinelSwapInProgress);

                // Check if this thread has a free entry. Negative value means there is nothing free.
                if (idx < 0)
                {
                    // Try to refill local cache. The local cache is a linked list of 16 free entries.

                    // Indicate to other threads that we are refilling the cache.
                    // -2 means refilling cache.
                    // -1 means nothing free on this thread.
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], SentinelRefilling);

                    // If it failed try to get one from the never-allocated array
                    if (data->allocatedIndexLength < data->keyCapacity)
                    {
                        idx = Interlocked.Add(ref data->allocatedIndexLength, 16) - 16;

                        if (idx < data->keyCapacity - 1)
                        {
                            int count = math.min(16, data->keyCapacity - idx);

                            // Set up a linked list of free entries.
                            for (int i = 1; i < count; ++i)
                            {
                                nextPtrs[idx + i] = idx + i + 1;
                            }

                            // Last entry points to null.
                            nextPtrs[idx + count - 1] = -1;

                            // The first entry is going to be allocated to someone so it also points to null.
                            nextPtrs[idx] = -1;

                            // Set the TLS first free to the head of the list, which is the one after the entry we are returning.
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], idx + 1);

                            return idx;
                        }

                        if (idx == data->keyCapacity - 1)
                        {
                            // We tried to allocate more entries for this thread but we've already hit the key capacity,
                            // so we are in fact out of space. Record that this thread has no more entries.
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], -1);

                            return idx;
                        }
                    }

                    // If we reach here, then we couldn't allocate more entries for this thread, so it's completely empty.
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], -1);

                    int maxThreadCount = JobsUtility.ThreadIndexCount;
                    // Failed to get any, try to get one from another free list
                    bool again = true;
                    while (again)
                    {
                        again = false;
                        for (int other = (threadIndex + 1) % maxThreadCount
                             ; other != threadIndex
                             ; other = (other + 1) % maxThreadCount
                        )
                        {
                            // Attempt to grab a free entry from another thread and switch the other thread's free head
                            // atomically.
                            do
                            {
                                do
                                {
                                    idx = Volatile.Read(ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine]);
                                } while (idx == SentinelSwapInProgress);

                                if (idx < 0)
                                {
                                    break;
                                }
                            }
                            while (Interlocked.CompareExchange(
                                ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine]
                                , SentinelSwapInProgress
                                , idx
                                   ) != idx
                            );

                            if (idx == -2)
                            {
                                // If the thread was refilling the cache, then try again.
                                again = true;
                            }
                            else if (idx >= 0)
                            {
                                // We succeeded in getting an entry from another thread so remove this entry from the
                                // linked list.
                                Interlocked.Exchange(ref data->firstFreeTLS[other * UnsafeParallelHashMapData.IntsPerCacheLine], nextPtrs[idx]);
                                nextPtrs[idx] = -1;
                                return idx;
                            }
                        }
                    }
                    ThrowFull();
                }

                CheckOutOfCapacity(idx, data->keyCapacity);
            }
            while (Interlocked.CompareExchange(
                ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine]
                , SentinelSwapInProgress
                , idx
                   ) != idx
            );

            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine], nextPtrs[idx]);
            nextPtrs[idx] = -1;
            return idx;
        }

        internal static unsafe void FreeEntry(UnsafeParallelHashMapData* data, int idx, int threadIndex)
        {
            int* nextPtrs = (int*)data->next;
            int next = -1;

            do
            {
                do
                {
                    next = Volatile.Read(ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine]);
                } while (next == SentinelSwapInProgress);
                nextPtrs[idx] = next;
            }
            while (Interlocked.CompareExchange(
                ref data->firstFreeTLS[threadIndex * UnsafeParallelHashMapData.IntsPerCacheLine]
                , idx
                , next
                   ) != next
            );
        }

        internal static unsafe bool TryAddAtomic(UnsafeParallelHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            TValue tempItem;
            NativeParallelMultiHashMapIterator<TKey> tempIt;
            if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                return false;
            }

            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            // Make the bucket's head idx. If the exchange returns something other than -1, then the bucket had
            // a non-null head which means we need to do more checks...
            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                int* nextPtrs = (int*)data->next;
                int next = -1;

                do
                {
                    // Link up this entry with the rest of the bucket under the assumption that this key
                    // doesn't already exist in the bucket. This assumption could be wrong, which will be
                    // checked later.
                    next = buckets[bucket];
                    nextPtrs[idx] = next;

                    // If the key already exists then we should free the entry we took earlier.
                    if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        FreeEntry(data, idx, threadIndex);

                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, next) != next);
            }

            return true;
        }

        internal static unsafe void AddAtomicMulti(UnsafeParallelHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            int nextPtr;
            int* nextPtrs = (int*)data->next;
            do
            {
                nextPtr = buckets[bucket];
                nextPtrs[idx] = nextPtr;
            }
            while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtr) != nextPtr);
        }

        internal static unsafe bool TryAdd(UnsafeParallelHashMapData* data, TKey key, TValue item, bool isMultiHashMap, AllocatorManager.AllocatorHandle allocation)
        {
            TValue tempItem;
            NativeParallelMultiHashMapIterator<TKey> tempIt;

            if (isMultiHashMap || !TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                // Allocate an entry from the free list
                int idx;
                int* nextPtrs;

                if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
                {
                    int maxThreadCount = JobsUtility.ThreadIndexCount;
                    for (int tls = 1; tls < maxThreadCount; ++tls)
                    {
                        if (data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] >= 0)
                        {
                            idx = data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine];
                            nextPtrs = (int*)data->next;
                            data->firstFreeTLS[tls * UnsafeParallelHashMapData.IntsPerCacheLine] = nextPtrs[idx];
                            nextPtrs[idx] = -1;
                            data->firstFreeTLS[0] = idx;
                            break;
                        }
                    }

                    if (data->firstFreeTLS[0] < 0)
                    {
                        if (data->keyCapacity == UnsafeParallelHashMapData.kMaxCapacity)
                        {
                            return false;
                        }

                        int newCap = UnsafeParallelHashMapData.GrowCapacity(data->keyCapacity);
                        UnsafeParallelHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeParallelHashMapData.GetBucketSize(newCap), allocation);
                    }
                }

                idx = data->firstFreeTLS[0];

                if (idx >= 0)
                {
                    data->firstFreeTLS[0] = ((int*)data->next)[idx];
                }
                else
                {
                    idx = data->allocatedIndexLength++;
                }

                CheckIndexOutOfBounds(data, idx);

                // Write the new value to the entry
                UnsafeUtility.WriteArrayElement(data->keys, idx, key);
                UnsafeUtility.WriteArrayElement(data->values, idx, item);

                int bucket = key.GetHashCode() & data->bucketCapacityMask;
                // Add the index to the hash-map
                int* buckets = (int*)data->buckets;
                nextPtrs = (int*)data->next;
                nextPtrs[idx] = buckets[bucket];
                buckets[bucket] = idx;

                return true;
            }

            return false;
        }

        internal static unsafe int Remove(UnsafeParallelHashMapData* data, TKey key, bool isMultiHashMap)
        {
            if (data->keyCapacity == 0)
            {
                return 0;
            }

            var removed = 0;

            // First find the slot based on the hash
            var buckets = (int*)data->buckets;
            var nextPtrs = (int*)data->next;
            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            var prevEntry = -1;
            var entryIdx = buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data->keyCapacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        buckets[bucket] = nextPtrs[entryIdx];
                    }
                    else
                    {
                        nextPtrs[prevEntry] = nextPtrs[entryIdx];
                    }

                    // And free the index
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = data->firstFreeTLS[0];
                    data->firstFreeTLS[0] = entryIdx;
                    entryIdx = nextIdx;

                    // Can only be one hit in regular hashmaps, so return
                    if (!isMultiHashMap)
                    {
                        break;
                    }
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = nextPtrs[entryIdx];
                }
            }

            return removed;
        }

        internal static unsafe void Remove(UnsafeParallelHashMapData* data, NativeParallelMultiHashMapIterator<TKey> it)
        {
            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int* nextPtrs = (int*)data->next;
            int bucket = it.key.GetHashCode() & data->bucketCapacityMask;

            int entryIdx = buckets[bucket];

            if (entryIdx == it.EntryIndex)
            {
                buckets[bucket] = nextPtrs[entryIdx];
            }
            else
            {
                while (entryIdx >= 0 && nextPtrs[entryIdx] != it.EntryIndex)
                {
                    entryIdx = nextPtrs[entryIdx];
                }

                if (entryIdx < 0)
                {
                    ThrowInvalidIterator();
                }

                nextPtrs[entryIdx] = nextPtrs[it.EntryIndex];
            }

            // And free the index
            nextPtrs[it.EntryIndex] = data->firstFreeTLS[0];
            data->firstFreeTLS[0] = it.EntryIndex;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        internal static unsafe void RemoveKeyValue<TValueEQ>(UnsafeParallelHashMapData* data, TKey key, TValueEQ value)
            where TValueEQ : unmanaged, IEquatable<TValueEQ>
        {
            if (data->keyCapacity == 0)
            {
                return;
            }

            var buckets = (int*)data->buckets;
            var keyCapacity = (uint)data->keyCapacity;
            var prevNextPtr = buckets + (key.GetHashCode() & data->bucketCapacityMask);
            var entryIdx = *prevNextPtr;

            if ((uint)entryIdx >= keyCapacity)
            {
                return;
            }

            var nextPtrs = (int*)data->next;
            var keys = data->keys;
            var values = data->values;
            var firstFreeTLS = data->firstFreeTLS;

            do
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key)
                    && UnsafeUtility.ReadArrayElement<TValueEQ>(values, entryIdx).Equals(value))
                {
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = firstFreeTLS[0];
                    firstFreeTLS[0] = entryIdx;
                    *prevNextPtr = entryIdx = nextIdx;
                }
                else
                {
                    prevNextPtr = nextPtrs + entryIdx;
                    entryIdx = *prevNextPtr;
                }
            }
            while ((uint)entryIdx < keyCapacity);
        }

        internal static unsafe bool TryGetFirstValueAtomic(UnsafeParallelHashMapData* data, TKey key, out TValue item, out NativeParallelMultiHashMapIterator<TKey> it)
        {
            it.key = key;

            if (data->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryGetNextValueAtomic(data, out item, ref it);
        }

        internal static unsafe bool TryGetNextValueAtomic(UnsafeParallelHashMapData* data, out TValue item, ref NativeParallelMultiHashMapIterator<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            item = default;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            int* nextPtrs = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(data->values, entryIdx);

            return true;
        }

        internal static unsafe bool SetValue(UnsafeParallelHashMapData* data, ref NativeParallelMultiHashMapIterator<TKey> it, ref TValue item)
        {
            int entryIdx = it.EntryIndex;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            UnsafeUtility.WriteArrayElement(data->values, entryIdx, item);
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckOutOfCapacity(int idx, int keyCapacity)
        {
            if (idx >= keyCapacity)
            {
                throw new InvalidOperationException(string.Format("nextPtr idx {0} beyond capacity {1}", idx, keyCapacity));
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static unsafe void CheckIndexOutOfBounds(UnsafeParallelHashMapData* data, int idx)
        {
            if (idx < 0 || idx >= data->keyCapacity)
                throw new InvalidOperationException("Internal HashMap error");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void ThrowFull()
        {
            throw new InvalidOperationException("HashMap is full");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void ThrowInvalidIterator()
        {
            throw new InvalidOperationException("Invalid iterator passed to HashMap remove");
        }
    }

    /// <summary>
    /// A key-value pair.
    /// </summary>
    /// <remarks>Used for enumerators.</remarks>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(int), typeof(int)})]
    public unsafe struct KeyValue<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal UnsafeParallelHashMapData* m_Buffer;
        internal int m_Index;
        internal int m_Next;

        /// <summary>
        ///  An invalid KeyValue.
        /// </summary>
        /// <value>In a hash map enumerator's initial state, its <see cref="UnsafeParallelHashMap{TKey,TValue}.Enumerator.Current"/> value is Null.</value>
        public static KeyValue<TKey, TValue> Null => new KeyValue<TKey, TValue>{m_Index = -1};

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
                    return UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
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

                return ref UnsafeUtility.AsRef<TValue>(m_Buffer->values + sizeof(TValue) * m_Index);
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
                key = UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
                value = UnsafeUtility.ReadArrayElement<TValue>(m_Buffer->values, m_Index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }

    internal unsafe struct UnsafeParallelHashMapDataEnumerator
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal int m_Index;
        internal int m_BucketIndex;
        internal int m_NextIndex;

        internal unsafe UnsafeParallelHashMapDataEnumerator(UnsafeParallelHashMapData* data)
        {
            m_Buffer = data;
            m_Index = -1;
            m_BucketIndex = 0;
            m_NextIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool MoveNext()
        {
            return UnsafeParallelHashMapData.MoveNext(m_Buffer, ref m_BucketIndex, ref m_NextIndex, out m_Index);
        }

        internal void Reset()
        {
            m_Index = -1;
            m_BucketIndex = 0;
            m_NextIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal KeyValue<TKey, TValue> GetCurrent<TKey, TValue>()
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new KeyValue<TKey, TValue> { m_Buffer = m_Buffer, m_Index = m_Index };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TKey GetCurrentKey<TKey>()
            where TKey : unmanaged, IEquatable<TKey>
        {
            if (m_Index != -1)
            {
                return UnsafeUtility.ReadArrayElement<TKey>(m_Buffer->keys, m_Index);
            }

            return default;
        }
    }

    /// <summary>
    /// An unordered, expandable associative array.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {Count()}, Capacity = {Capacity}, IsCreated = {IsCreated}, IsEmpty = {IsEmpty}")]
    [DebuggerTypeProxy(typeof(UnsafeParallelHashMapDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
    public unsafe struct UnsafeParallelHashMap<TKey, TValue>
        : INativeDisposable
        , IEnumerable<KeyValue<TKey, TValue>> // Used by collection initializers.
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeParallelHashMapData* m_Buffer;
        internal AllocatorManager.AllocatorHandle m_AllocatorLabel;

        /// <summary>
        /// Initializes and returns an instance of UnsafeParallelHashMap.
        /// </summary>
        /// <param name="capacity">The number of key-value pairs that should fit in the initial allocation.</param>
        /// <param name="allocator">The allocator to use.</param>
        public UnsafeParallelHashMap(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeParallelHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);

            Clear();
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
        /// Whether this hash map is empty.
        /// </summary>
        /// <value>True if this hash map is empty or the hash map has not been constructed.</value>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsCreated || UnsafeParallelHashMapData.IsEmpty(m_Buffer);
        }

        /// <summary>
        /// The current number of key-value pairs in this hash map.
        /// </summary>
        /// <returns>The current number of key-value pairs in this hash map.</returns>
        public readonly int Count() => UnsafeParallelHashMapData.GetCount(m_Buffer);

        /// <summary>
        /// The number of key-value pairs that fit in the current allocation.
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
        /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <returns>True if the key-value pair was added.</returns>
        public bool TryAdd(TKey key, TValue item)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, false, m_AllocatorLabel);
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// If the key is already present in the collection, this method <b>should</b> throw, but currently does not!
        /// Instead, it silently fails, without replacing said value.
        /// </summary>
        /// <remarks>
        /// Prefer <see cref="TryAdd"/> if your intent is to allow graceful failure, as this API will eventually become more strict (and begin to throw).
        /// </remarks>
        /// <param name="key">The key to add.</param>
        /// <param name="item">The value to add.</param>
        /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
        /// <exception cref="InvalidOperationException">Thrown if there is not enough Capacity to contain the added element, and the container can't be resized.</exception>
        public void Add(TKey key, TValue item)
        {
            var added = UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, false, m_AllocatorLabel);

            if (!added)
            {
                if (m_Buffer->keyCapacity == UnsafeParallelHashMapData.kMaxCapacity)
                {
                    ThrowAtMaxCapacity();
                    return;
                }

                ThrowKeyAlreadyAdded(key);
            }
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if a key-value pair was removed.</returns>
        public bool Remove(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.Remove(m_Buffer, key, false) != 0;
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <returns>True if the key was present.</returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            NativeParallelMultiHashMapIterator<TKey> tempIt;
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out tempIt);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>True if the key was present.</returns>
        public bool ContainsKey(TKey key)
        {
            return UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var tempValue, out var tempIt);
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
                TValue res;
                TryGetValue(key, out res);
                return res;
            }

            set
            {
                if (UnsafeParallelHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var item, out var iterator))
                {
                    UnsafeParallelHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref iterator, ref value);
                }
                else
                {
                    UnsafeParallelHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, value, false, m_AllocatorLabel);
                }
            }
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
        /// Returns an array with a copy of all this hash map's keys (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
        public NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TKey>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns an array with a copy of all this hash map's values (in no particular order).
        /// </summary>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
        public NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
        {
            var result = CollectionHelper.CreateNativeArray<TValue>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
        /// </summary>
        /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(UnsafeParallelHashMapData.GetCount(m_Buffer), allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeParallelHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
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
        /// Returns a readonly version of this UnsafeParallelHashMap instance.
        /// </summary>
        /// <remarks>ReadOnly containers point to the same underlying data as the UnsafeParallelHashMap it is made from.</remarks>
        /// <returns>ReadOnly instance for this.</returns>
        public ReadOnly AsReadOnly()
        {
            return new ReadOnly(this);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void ThrowKeyAlreadyAdded(in TKey key)
        {
            throw new ArgumentException($"An item with the same key has already been added: {key}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void ThrowAtMaxCapacity()
        {
            throw new InvalidOperationException($"Capacity is insufficient, and resize would fail (Capacity {Capacity} / {MaxCapacity}, Count {Count()})!");
        }

        /// <summary>
        /// A read-only alias for the value of a UnsafeParallelHashMap. Does not have its own allocated storage.
        /// </summary>
        [DebuggerDisplay("Count = {m_HashMapData.Count()}, Capacity = {m_HashMapData.Capacity}, IsCreated = {m_HashMapData.IsCreated}, IsEmpty = {IsEmpty}")]
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public struct ReadOnly
            : IEnumerable<KeyValue<TKey, TValue>>
        {
            internal UnsafeParallelHashMap<TKey, TValue> m_HashMapData;

            internal ReadOnly(UnsafeParallelHashMap<TKey, TValue> hashMapData)
            {
                m_HashMapData = hashMapData;
            }

            /// <summary>
            /// Whether this hash map has been allocated (and not yet deallocated).
            /// </summary>
            /// <value>True if this hash map has been allocated (and not yet deallocated).</value>
            public readonly bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_HashMapData.IsCreated;
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

                    return m_HashMapData.IsEmpty;
                }
            }

            /// <summary>
            /// The current number of key-value pairs in this hash map.
            /// </summary>
            /// <returns>The current number of key-value pairs in this hash map.</returns>
            public readonly int Count()
            {
                return m_HashMapData.Count();
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
                    return m_HashMapData.Capacity;
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
                return m_HashMapData.TryGetValue(key, out item);
            }

            /// <summary>
            /// Returns true if a given key is present in this hash map.
            /// </summary>
            /// <param name="key">The key to look up.</param>
            /// <returns>True if the key was present.</returns>
            public readonly bool ContainsKey(TKey key)
            {
                return m_HashMapData.ContainsKey(key);
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
                    TValue res;

                    if (m_HashMapData.TryGetValue(key, out res))
                    {
                        return res;
                    }

                    ThrowKeyNotPresent(key);

                    return default;
                }
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's keys (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's keys (in no particular order).</returns>
            public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_HashMapData.GetKeyArray(allocator);
            }

            /// <summary>
            /// Returns an array with a copy of all this hash map's values (in no particular order).
            /// </summary>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>An array with a copy of all this hash map's values (in no particular order).</returns>
            public readonly NativeArray<TValue> GetValueArray(AllocatorManager.AllocatorHandle allocator)
            {
                return m_HashMapData.GetValueArray(allocator);
            }

            /// <summary>
            /// Returns a NativeKeyValueArrays with a copy of all this hash map's keys and values.
            /// </summary>
            /// <remarks>The key-value pairs are copied in no particular order. For all `i`, `Values[i]` will be the value associated with `Keys[i]`.</remarks>
            /// <param name="allocator">The allocator to use.</param>
            /// <returns>A NativeKeyValueArrays with a copy of all this hash map's keys and values.</returns>
            public readonly NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(AllocatorManager.AllocatorHandle allocator)
            {
                return m_HashMapData.GetKeyValueArrays(allocator);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            readonly void ThrowKeyNotPresent(TKey key)
            {
                throw new ArgumentException($"Key: {key} is not present in the NativeParallelHashMap.");
            }

            /// <summary>
            /// Returns an enumerator over the key-value pairs of this hash map.
            /// </summary>
            /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
            public readonly Enumerator GetEnumerator()
            {
                return new Enumerator
                {
                    m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_HashMapData.m_Buffer),
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

        /// <summary>
        /// A parallel writer for a NativeParallelHashMap.
        /// </summary>
        /// <remarks>
        /// Use <see cref="AsParallelWriter"/> to create a parallel writer for a NativeParallelHashMap.
        /// </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeParallelHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// Returns the index of the current thread.
            /// </summary>
            /// <remarks>In a job, each thread gets its own copy of the ParallelWriter struct, and the job system assigns
            /// each copy the index of its thread.</remarks>
            /// <value>The index of the current thread.</value>
            public int ThreadIndex => m_ThreadIndex;

            /// <summary>
            /// The number of key-value pairs that fit in the current allocation.
            /// </summary>
            /// <value>The number of key-value pairs that fit in the current allocation.</value>
            public readonly int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UnsafeParallelHashMapData* data = m_Buffer;
                    return data->keyCapacity;
                }
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <returns>True if the key-value pair was added.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool TryAdd(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                return UnsafeParallelHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, m_ThreadIndex);
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method returns false without modifying the hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            /// <returns>True if the key-value pair was added.</returns>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public bool TryAdd(TKey key, TValue item, int threadIndexOverride)
            {
                Assert.IsTrue(threadIndexOverride >= 0);
                return UnsafeParallelHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, threadIndexOverride);
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public void Add(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                bool result = UnsafeParallelHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, m_ThreadIndex);
                if (!result)
                    ThrowKeyAlreadyAdded(key);
            }

            /// <summary>
            /// Adds a new key-value pair.
            /// </summary>
            /// <remarks>If the key is already present, this method throws without modifying the hash map.</remarks>
            /// <param name="key">The key to add.</param>
            /// <param name="item">The value to add.</param>
            /// <param name="threadIndexOverride">The thread index which must be set by a field from a job struct with the <see cref="NativeSetThreadIndexAttribute"/> attribute.</param>
            /// <exception cref="ArgumentException">Thrown if the key was already present.</exception>
            /// <exception cref="InvalidOperationException">If the underlying collection is full (as it cannot be resized within a parallel writer).</exception>
            public void Add(TKey key, TValue item, int threadIndexOverride)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                bool result = UnsafeParallelHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, threadIndexOverride);

                if (!result)
                {
                    ThrowKeyAlreadyAdded(key);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns>An enumerator over the key-value pairs of this hash map.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator { m_Enumerator = new UnsafeParallelHashMapDataEnumerator(m_Buffer) };
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
        /// An enumerator over the key-value pairs of a hash map.
        /// </summary>
        /// <remarks>
        /// In an enumerator's initial state, <see cref="Current"/> is not valid to read.
        /// From this state, the first <see cref="MoveNext"/> call advances the enumerator to the first key-value pair.
        /// </remarks>
        public struct Enumerator : IEnumerator<KeyValue<TKey, TValue>>
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
    }

    [BurstCompile]
    internal unsafe struct UnsafeParallelHashMapDisposeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public UnsafeParallelHashMapData* Data;
        public AllocatorManager.AllocatorHandle Allocator;

        public void Execute()
        {
            UnsafeParallelHashMapData.DeallocateHashMap(Data, Allocator);
        }
    }

    sealed internal class UnsafeParallelHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        UnsafeParallelHashMap<TKey, TValue> m_Target;

        public UnsafeParallelHashMapDebuggerTypeProxy(UnsafeParallelHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using (var kva = m_Target.GetKeyValueArrays(Allocator.Temp))
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

    /// <summary>
    /// For internal use only.
    /// </summary>
    public unsafe struct UntypedUnsafeParallelHashMap
    {
#pragma warning disable 169
        [NativeDisableUnsafePtrRestriction]
        UnsafeParallelHashMapData* m_Buffer;
        AllocatorManager.AllocatorHandle m_AllocatorLabel;
#pragma warning restore 169
    }
}
