// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using System.Diagnostics;
using Unity.Burst;

namespace Unity.Jobs
{
    /// <summary>
    /// Calculates the number of iterations to perform in a job that must execute before an IJobParallelForDefer job.
    /// </summary>
    /// <remarks>
    /// A replacement for IJobParallelFor when the number of work items is not known at Schedule time.
    ///
    /// When Scheduling the job's Execute(int index) method will be invoked on multiple worker threads in 
    /// parallel to each other.
    ///
    /// Execute(int index) will be executed once for each index from 0 to the provided length. Each iteration
    /// must be independent from other iterations and the safety system enforces this rule for you. The indices 
    /// have no guaranteed order and are executed on multiple cores in parallel.
    ///
    /// Unity automatically splits the work into chunks of no less than the provided batchSize, and schedules 
    /// an appropriate number of jobs based on the number of worker threads, the length of the array and the batch size.
    ///
    /// Choose a batch size sbased on the amount of work performed in the job. A simple job, 
    /// for example adding a couple of float3 to each other could have a batch size of 32 to 128. However, 
    /// if the work performed is very expensive then it's best to use a small batch size, such as a batch 
    /// size of 1. IJobParallelFor performs work stealing using atomic operations. Batch sizes can be 
    /// small but they aren't free.
    ///
    /// The returned JobHandle can be used to ensure that the job has completed. Or it can be passed to other jobs as 
    /// a dependency, ensuring that the jobs are executed one after another on the worker threads.
    /// </remarks>
    [JobProducerType(typeof(IJobParallelForDeferExtensions.JobParallelForDeferProducer<>))]
    public interface IJobParallelForDefer
    {
        /// <summary>
        /// Implement this method to perform work against a specific iteration index.
        /// </summary>
        /// <param name="index">The index of the Parallel for loop at which to perform work.</param>
        void Execute(int index);
    }

    /// <summary>
    /// Extension class for the IJobParallelForDefer job type providing custom overloads for scheduling and running.
    /// </summary>
    public static class IJobParallelForDeferExtensions
    {
        internal struct JobParallelForDeferProducer<T> where T : struct, IJobParallelForDefer
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelForDeferProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out int begin, out int end))
                        break;

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);

                    // Cache the end value to make it super obvious to the
                    // compiler that `end` will never change during the loops
                    // iteration.
                    var endThatCompilerCanSeeWillNeverChange = end;
                    for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        jobData.Execute(i);
                }
            }
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings. Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <remarks>
        /// When the Jobs package is included in the project, Unity generates code to call EarlyJobInit at startup. This allows Burst compiled code to schedule jobs because the reflection part of initialization, which is not compatible with burst compiler constraints, has already happened in EarlyJobInit.
        /// 
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobParallelForDefer&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForDefer
        {
            JobParallelForDeferProducer<T>.Initialize();
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// list.Length is used as the iteration count.
        /// Note that it is required to embed the list on the job struct as well.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="list">list.Length is used as the iteration count.</param>
        /// <param name="innerloopBatchCount">Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform them in an efficient inner loop.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        /// <typeparam name="U">List element type</typeparam>
        public static unsafe JobHandle Schedule<T, U>(this T jobData, NativeList<U> list, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
            return ScheduleInternal(ref jobData, innerloopBatchCount,
                list.GetUnsafeList(),
                atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// list.Length is used as the iteration count.
        /// Note that it is required to embed the list on the job struct as well.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="list">list.Length is used as the iteration count.</param>
        /// <param name="innerloopBatchCount">Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform them in an efficient inner loop.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        /// <typeparam name="U">List element type</typeparam>
        public static unsafe JobHandle ScheduleByRef<T, U>(this ref T jobData, NativeList<U> list, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
            where U : unmanaged
        {
            void* atomicSafetyHandlePtr = null;
            // Calculate the deferred atomic safety handle before constructing JobScheduleParameters so
            // DOTS Runtime can validate the deferred list statically similar to the reflection based
            // validation in Big Unity.
            var safety = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref list);
            atomicSafetyHandlePtr = UnsafeUtility.AddressOf(ref safety);
            return ScheduleInternal(ref jobData, innerloopBatchCount,
                list.GetUnsafeList(),
                atomicSafetyHandlePtr, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// forEachCount is a pointer to the number of iterations, when dependsOn has completed.
        /// This API is unsafe, it is recommended to use the NativeList based Schedule method instead.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="forEachCount">*forEachCount is used as the iteration count.</param>
        /// <param name="innerloopBatchCount">Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform them in an efficient inner loop.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<T>(this T jobData, int* forEachCount, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
        {
            var forEachListPtr = (byte*)forEachCount - sizeof(void*);
            return ScheduleInternal(ref jobData, innerloopBatchCount, forEachListPtr, null, dependsOn);
        }

        /// <summary>
        /// Schedule the job for execution on worker threads.
        /// forEachCount is a pointer to the number of iterations, when dependsOn has completed.
        /// This API is unsafe, it is recommended to use the NativeList based Schedule method instead.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="forEachCount">*forEachCount is used as the iteration count.</param>
        /// <param name="innerloopBatchCount">Granularity in which workstealing is performed. A value of 32, means the job queue will steal 32 iterations and then perform them in an efficient inner loop.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static unsafe JobHandle ScheduleByRef<T>(this ref T jobData, int* forEachCount, int innerloopBatchCount,
            JobHandle dependsOn = new JobHandle())
            where T : struct, IJobParallelForDefer
        {
            var forEachListPtr = (byte*)forEachCount - sizeof(void*);
            return ScheduleInternal(ref jobData, innerloopBatchCount, forEachListPtr, null, dependsOn);
        }

        private static unsafe JobHandle ScheduleInternal<T>(ref T jobData,
            int innerloopBatchCount,
            void* forEachListPtr,
            void *atomicSafetyHandlePtr,
            JobHandle dependsOn) where T : struct, IJobParallelForDefer
        {
            JobParallelForDeferProducer<T>.Initialize();
            var reflectionData = JobParallelForDeferProducer<T>.jobReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, forEachListPtr, atomicSafetyHandlePtr);
        }
    }
}
