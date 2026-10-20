// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Diagnostics;

namespace Unity.Entities
{
    internal struct SortingUtilities
    {
        public static unsafe void InsertSorted(ComponentType* data, int length, ComponentType newValue)
        {
            while (length > 0 && newValue < data[length - 1])
            {
                data[length] = data[length - 1];
                --length;
            }

            data[length] = newValue;
        }

        public static unsafe void InsertSorted(ComponentTypeInArchetype* data, int length, ComponentType newValue)
        {
            var newVal = new ComponentTypeInArchetype(newValue);
            while (length > 0 && newVal < data[length - 1])
            {
                data[length] = data[length - 1];
                --length;
            }

            data[length] = newVal;
        }
    }

    // @macton This version simply fixes the sorting issues in the most expidient way.
    // - Next version will need to reimplement the merge sort with lookaside buffers.
    struct IndexedValue<T> : IComparable<IndexedValue<T>>
        where T : struct, IComparable<T>
    {
        public T Value;
        public int Index;
        public int CompareTo(IndexedValue<T> other) => Value.CompareTo(other.Value);
    }

    // @macton This version simply fixes the sorting issues in the most expidient way.
    // - Next version will need to reimplement the merge sort with lookaside buffers.
    struct IndexedInt : IComparable<IndexedInt>
    {
        public int Value;
        public int Index;
        public int CompareTo(IndexedInt other) => Value.CompareTo(other.Value);
    }

    [BurstCompile]
    struct CopyIndexedInt : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> Src;
        public NativeArray<IndexedInt> Dst;

        public void Execute(int index)
        {
            Dst[index] = new IndexedInt
            {
                Value = Src[index],
                Index = index
            };
        }
    }

    [BurstCompile]
    struct SegmentSortInt : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<IndexedInt> Data;
        public int SegmentWidth;

        public void Execute(int index)
        {
            var startIndex = index * SegmentWidth;
            var segmentLength = ((Data.Length - startIndex) < SegmentWidth) ? (Data.Length - startIndex) : SegmentWidth;
            var slice = new NativeSlice<IndexedInt>(Data, startIndex, segmentLength);
            NativeSortExtension.Sort(slice);
        }
    }

    [BurstCompile]
    unsafe struct SegmentSortMergeInt : IJob
    {
        [ReadOnly]
        public NativeArray<IndexedInt> IndexedSourceBuffer;

        public NativeArray<int> SourceIndexBySortedSourceIndex;
        public NativeList<int> SortedSourceIndexBySharedIndex;
        public NativeList<int> SharedIndexCountsBySharedIndex;
        public NativeArray<int> SharedIndicesBySourceIndex;
        public int SegmentWidth;

        public void Execute()
        {
            var length = IndexedSourceBuffer.Length;
            if (length == 0)
                return;

            var segmentCount = (length + (SegmentWidth - 1)) / SegmentWidth;
            var segmentIndex = stackalloc int[segmentCount];

            var lastSharedIndex = -1;
            var lastSharedValue = 0;

            for (int sortIndex = 0; sortIndex < length; sortIndex++)
            {
                // find next best
                int bestSegmentIndex = -1;
                IndexedInt bestValue = default(IndexedInt);

                for (int i = 0; i < segmentCount; i++)
                {
                    var startIndex = i * SegmentWidth;
                    var offset = segmentIndex[i];
                    var segmentLength = ((length - startIndex) < SegmentWidth) ? (length - startIndex) : SegmentWidth;
                    if (offset == segmentLength)
                        continue;

                    var nextValue = IndexedSourceBuffer[startIndex + offset];
                    if (bestSegmentIndex != -1)
                    {
                        if (nextValue.CompareTo(bestValue) > 0)
                            continue;
                    }

                    bestValue = nextValue;
                    bestSegmentIndex = i;
                }

                segmentIndex[bestSegmentIndex]++;
                SourceIndexBySortedSourceIndex[sortIndex] = bestValue.Index;

                if ((lastSharedIndex != -1) && (bestValue.Value.CompareTo(lastSharedValue) == 0))
                {
                    SharedIndexCountsBySharedIndex[lastSharedIndex]++;
                }
                else
                {
                    lastSharedIndex++;
                    lastSharedValue = bestValue.Value;

                    SortedSourceIndexBySharedIndex.Add(sortIndex);
                    SharedIndexCountsBySharedIndex.Add(1);
                }

                SharedIndicesBySourceIndex[bestValue.Index] = lastSharedIndex;
            }
        }
    }


    /// <summary>
    ///     Merge sort index list referencing NativeArray values.
    /// </summary>
    /// <remarks>
    ///     Provide list of shared values, indices to shared values, and lists of source i
    ///     value indices with identical shared value.
    ///     As an example:
    ///     Given Source NativeArray: [A,A,A,B,B,C,C,A,B]
    ///     Provides:
    ///     Shared value indices: [0,0,0,1,1,2,2,0,1]
    ///     Shared value counts: [4,3,2] (number of occurrences of a shared value)
    ///     Shared values: [A,B,C] (not stored in this structure)
    ///     Sorted indices: [0,1,2,7,3,4,8,5,6] (using these indices to look up values in the source array would give you [A,A,A,A,B,B,B,C,C])
    ///     Shared value start offsets (into sorted indices): [0,4,7]
    /// </remarks>
    public struct NativeArraySharedInt : IDisposable
    {
        [ReadOnly] private readonly NativeArray<int> m_SourceBuffer;

        // Sorted indices into m_SourceBuffer
        private NativeArray<int> m_SourceIndexBySortedSourceIndex;

        // For each unique value found in m_SourceBuffer, what is the index into m_SortedSourceIndices
        private NativeList<int> m_SortedSourceIndexBySharedIndex;

        // For each index of m_SharedValueIndices, how many values in m_SortedSourceIndices share same value?
        private NativeList<int> m_SharedIndexCountsBySharedIndex;

        // For each index of m_SourceBuffer, what is the index into m_SharedValue
        private NativeArray<int> m_SharedIndicesBySourceIndex;

        /// <summary>
        /// Original Source Values (passed into constructor)
        /// </summary>
        public NativeArray<int> SourceBuffer => m_SourceBuffer;

        /// <summary>
        /// Construct a NativeArraySharedInt struct from a NativeArray
        /// </summary>
        /// <param name="sourceBuffer">Original source values.</param>
        /// <param name="allocator">The NativeArray allocation type.</param>
        public NativeArraySharedInt(NativeArray<int> sourceBuffer, AllocatorManager.AllocatorHandle allocator)
        {
            m_SourceBuffer = sourceBuffer;
            m_SourceIndexBySortedSourceIndex = CollectionHelper.CreateNativeArray<int>(sourceBuffer.Length, allocator);
            m_SortedSourceIndexBySharedIndex = new NativeList<int>(allocator);
            m_SharedIndexCountsBySharedIndex = new NativeList<int>(allocator);
            m_SharedIndicesBySourceIndex = CollectionHelper.CreateNativeArray<int>(sourceBuffer.Length, allocator);
        }

        /// <summary>
        /// Dispose this NativeArraySharedInt struct
        /// </summary>
        public void Dispose()
        {
            m_SourceIndexBySortedSourceIndex.Dispose();
            m_SortedSourceIndexBySharedIndex.Dispose();
            m_SharedIndexCountsBySharedIndex.Dispose();
            m_SharedIndicesBySourceIndex.Dispose();
        }

        /// <summary>
        ///     Schedule jobs to collect and sort shared values.
        /// </summary>
        /// <param name="inputDeps">Dependent JobHandle</param>
        /// <returns>JobHandle</returns>
        public JobHandle Schedule(JobHandle inputDeps)
        {
            var length = m_SourceBuffer.Length;
            if (length == 0)
                return inputDeps;

            var segmentCount = (length + 1023) / 1024;
            var copyIndexedValues = new NativeArray<IndexedInt>(length, Allocator.TempJob);
            var copyIndexedValuesJob = new CopyIndexedInt
            {
                Src = m_SourceBuffer,
                Dst = copyIndexedValues
            };
            var copyIndexValuesJobHandle = copyIndexedValuesJob.Schedule(length, 1024, inputDeps);

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            var workerSegmentCount = segmentCount / maxThreadCount;
            var segmentSortJob = new SegmentSortInt
            {
                Data = copyIndexedValues,
                SegmentWidth = 1024
            };
            var segmentSortJobHandle =
                segmentSortJob.Schedule(segmentCount, workerSegmentCount, copyIndexValuesJobHandle);
            var segmentSortMergeJob = new SegmentSortMergeInt
            {
                IndexedSourceBuffer = copyIndexedValues,
                SourceIndexBySortedSourceIndex = m_SourceIndexBySortedSourceIndex,
                SortedSourceIndexBySharedIndex = m_SortedSourceIndexBySharedIndex,
                SharedIndexCountsBySharedIndex = m_SharedIndexCountsBySharedIndex,
                SharedIndicesBySourceIndex = m_SharedIndicesBySourceIndex,
                SegmentWidth = 1024
            };
            var segmentSortMergeJobHandle = segmentSortMergeJob.Schedule(segmentSortJobHandle);

            return copyIndexedValues.Dispose(segmentSortMergeJobHandle);
        }

        /// <summary>
        ///     Indices into source NativeArray sorted by value
        /// </summary>
        /// <returns>Index NativeArray where each element refers to an element in the source NativeArray</returns>
        public NativeArray<int> GetSortedIndices() => m_SourceIndexBySortedSourceIndex;

        /// <summary>
        ///     Number of shared (unique) values in source NativeArray
        /// </summary>
        public int SharedValueCount => m_SortedSourceIndexBySharedIndex.Length;

        /// <summary>
        ///     Index of shared value associated with an element in the source buffer.
        ///     For example, given source array: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     Given the index 2 into the source array (A), the return value would be 0 (A in shared values).
        /// </summary>
        /// <param name="indexIntoSourceBuffer">Index of source value</param>
        /// <returns>Index into the list of shared values</returns>
        public int GetSharedIndexBySourceIndex(int indexIntoSourceBuffer) =>
            m_SharedIndicesBySourceIndex[indexIntoSourceBuffer];

        /// <summary>
        ///     Indices into shared values.
        ///     For example, given source array: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     shared index array would contain: [0,0,0,1,1,2,2,0,1]
        /// </summary>
        /// <returns>Index NativeArray where each element refers to the index of a shared value in a list of shared (unique) values.</returns>
        public NativeArray<int> GetSharedIndexArray() => m_SharedIndicesBySourceIndex;

        /// <summary>
        ///     Array of indices into shared value indices NativeArray which share the same source value
        ///     For example, given Source NativeArray: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     Shared value indices: [0,0,0,1,1,2,2,0,1]
        ///     Given the index 2 into the source array (A),
        ///     the returned array would contain: [0,1,2,7] (indices in SharedValueIndices that have a value of 0, i.e. where A is in the shared values)
        /// </summary>
        /// <param name="indexIntoSourceBuffer">Index of source value</param>
        /// <returns>Index NativeArray where each element refers to an index into the shared value indices array.</returns>
        public NativeArray<int> GetSharedValueIndicesBySourceIndex(int indexIntoSourceBuffer)
        {
            var sharedIndex = m_SharedIndicesBySourceIndex[indexIntoSourceBuffer];
            return GetSharedValueIndicesBySharedIndex(sharedIndex);
        }

        /// <summary>
        ///     Number of occurrences of a shared (unique) value shared by a given a source index.
        ///     For example, given source array: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     Shared value counts: [4,3,2] (number of occurrences of a shared value)
        ///     Given the index 2 into the source array (A), the return value would be 4 (for 4 occurrences of A in the source buffer).
        /// </summary>
        /// <param name="indexIntoSourceBuffer">Index of source value.</param>
        /// <returns>Count of total occurrences of the shared value at a source buffer index in the source buffer.</returns>
        public int GetSharedValueIndexCountBySourceIndex(int indexIntoSourceBuffer)
        {
            var sharedIndex = m_SharedIndicesBySourceIndex[indexIntoSourceBuffer];
            return m_SharedIndexCountsBySharedIndex[sharedIndex];
        }

        /// <summary>
        ///     Array of number of occurrences of all shared values.
        ///     For example, given source array: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     Shared value counts: [4,3,2] (number of occurrences of a shared value)
        /// </summary>
        /// <returns>Count NativeArray where each element refers to the number of occurrences of each shared value.</returns>
        public unsafe NativeArray<int> GetSharedValueIndexCountArray() => m_SharedIndexCountsBySharedIndex.AsArray();

        /// <summary>
        ///     Array of indices into source NativeArray which share the same shared value
        ///     For example, given source array: [A,A,A,B,B,C,C,A,B]
        ///     shared values are: [A,B,C]
        ///     Shared value counts: [4,3,2] (number of occurrences of a shared value)
        ///     Shared value start offsets (into sorted indices): [0,4,7]
        ///     Given the index 0 into the shared value array (A), the returned array would contain [0,1,2,7] (indices into the source array which point to the shared value A).
        /// </summary>
        /// <param name="sharedValueIndex">Index of shared value</param>
        /// <returns>Index NativeArray where each element refers to an index into the source array.</returns>
        public unsafe NativeArray<int> GetSharedValueIndicesBySharedIndex(int sharedValueIndex)
        {
            var sortedSourceIndex = m_SortedSourceIndexBySharedIndex[sharedValueIndex];
            var sharedValueIndexCount = m_SharedIndexCountsBySharedIndex[sharedValueIndex];

            // Capacity cannot be changed, so offset is valid.
            var rawIndices =
                ((int*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(
                    m_SourceIndexBySortedSourceIndex)) + sortedSourceIndex;
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(rawIndices, sharedValueIndexCount, Allocator.None);
            SharedValueIndicesSetSafetyHandle(ref arr);
            return arr;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void SharedValueIndicesSetSafetyHandle(ref NativeArray<int> arr)
        {
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr,
                NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_SourceIndexBySortedSourceIndex));
        }

    }
}
