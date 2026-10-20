// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

namespace Unity.Entities
{
    /// <summary>
    ///     Assign Value to each element of NativeArray
    /// </summary>
    /// <typeparam name="T">Type of element in NativeArray</typeparam>
    [BurstCompile]
    public struct MemsetNativeArray<T> : IJobParallelFor
        where T : struct
    {
        /// <summary>
        /// The destination array, to which the value is copied repeatedly.
        /// </summary>
        public NativeArray<T> Source;

        /// <summary>
        /// The value to copy repeatedly to the destination array.
        /// </summary>
        public T Value;

        /// <summary>
        /// This function is executed once for each work unit of the job, potentially concurrently.
        /// Each work unit copies one value into the destination array.
        /// </summary>
        /// <param name="index">The work unit index to process</param>
        public void Execute(int index)
        {
            Source[index] = Value;
        }
    }
}
