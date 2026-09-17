// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// Provides extension methods for hash maps.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class NativeParallelHashMapExtensions
    {
        /// <summary>
        /// Removes duplicate values from this sorted array and returns the number of values remaining.
        /// </summary>
        /// <remarks>
        /// Uses `Equals` to determine whether values are duplicates.
        ///
        /// Expects the array to already be sorted.
        ///
        /// The remaining elements will be tightly packed at the front of the array.
        /// </remarks>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="array">The array from which to remove duplicates.</param>
        /// <returns>The number of unique elements in this array.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int) })]
        public static int Unique<T>(this NativeArray<T> array)
            where T : unmanaged, IEquatable<T>
        {
            if (array.Length == 0)
            {
                return 0;
            }

            int first = 0;
            int last = array.Length;
            var result = first;
            while (++first != last)
            {
                if (!array[result].Equals(array[first]))
                {
                    array[++result] = array[first];
                }
            }

            return ++result;
        }

        /// <summary>
        /// Returns an array populated with the unique keys from this multi hash map.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="container">The multi hash map.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array populated with the unique keys from this multi hash map.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static (NativeArray<TKey>, int) GetUniqueKeyArray<TKey, TValue>(this UnsafeParallelMultiHashMap<TKey, TValue> container, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            var result = container.GetKeyArray(allocator);
            result.Sort();
            int uniques = result.Unique();
            return (result, uniques);
        }

        /// <summary>
        /// Returns an array populated with the unique keys from this multi hash map.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="container">The multi hash map.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <returns>An array populated with the unique keys from this multi hash map.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static (NativeArray<TKey>, int) GetUniqueKeyArray<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> container, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>, IComparable<TKey>
            where TValue : unmanaged
        {
            var result = container.GetKeyArray(allocator);
            result.Sort();
            int uniques = result.Unique();
            return (result, uniques);
        }

        /// <summary>
        /// Returns a "bucket" view of this hash map.
        /// </summary>
        /// <remarks>
        /// Internally, the elements of a hash map are split into buckets of type <see cref="UnsafeParallelHashMapBucketData"/>.
        ///
        /// With buckets, a job can safely access the elements of a hash map concurrently as long as each individual bucket is accessed
        /// only from an individual thread. Effectively, it is not safe to read elements of an individual bucket concurrently,
        /// but it is safe to read elements of separate buckets concurrently.
        /// </remarks>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="container">The hash map.</param>
        /// <returns>A "bucket" view of this hash map.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static unsafe UnsafeParallelHashMapBucketData GetUnsafeBucketData<TKey, TValue>(this NativeParallelHashMap<TKey, TValue> container)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return container.m_HashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Returns a "bucket" view of this multi hash map.
        /// </summary>
        /// <remarks>
        /// Internally, the elements of a hash map are split into buckets of type <see cref="UnsafeParallelHashMapBucketData"/>.
        ///
        /// With buckets, a job can safely access the elements of a hash map concurrently as long as each individual bucket is accessed
        /// only from an individual thread. Effectively, it is not safe to read elements of an individual bucket concurrently,
        /// but it is safe to read elements of separate buckets concurrently.
        /// </remarks>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="container">The multi hash map.</param>
        /// <returns>A "bucket" view of this multi hash map.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static unsafe UnsafeParallelHashMapBucketData GetUnsafeBucketData<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> container)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return container.m_MultiHashMapData.m_Buffer->GetBucketData();
        }

        /// <summary>
        /// Removes all occurrences of a particular key-value pair.
        /// </summary>
        /// <remarks>Removes all key-value pairs which have a particular key and which *also have* a particular value.
        /// In other words: (key *AND* value) rather than (key *OR* value).</remarks>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="container">The multi hash map.</param>
        /// <param name="key">The key of the key-value pairs to remove.</param>
        /// <param name="value">The value of the key-value pairs to remove.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new [] { typeof(int), typeof(int) })]
        public static void Remove<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> container, TKey key, TValue value)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(container.m_Safety);
            container.m_MultiHashMapData.Remove(key, value);
        }
    }
}
