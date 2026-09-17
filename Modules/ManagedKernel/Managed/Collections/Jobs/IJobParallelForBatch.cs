// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Diagnostics;
using Unity.Burst;

namespace Unity.Jobs
{
    /// <summary>
    /// Job type allowing for data to be operated on in parallel batches. 
    /// </summary>
    /// <remarks>
    /// When scheduling an IJobParallelForBatch job the number of elements to work on is specified along with a batch size. Jobs will then run in parallel
    /// invoking Execute at a particular 'startIndex' of your working set and for a specified 'count' number of elements.
    /// </remarks>
    [JobProducerType(typeof(IJobParallelForBatchExtensions.JobParallelForBatchProducer<>))]
    public interface IJobParallelForBatch
    {
        /// <summary>
        /// Function operation on a "batch" of data contained within the job.
        /// </summary>
        /// <param name="startIndex">Starting index of job data to safely access.</param>
        /// <param name="count">Number of elements to operate on in the batch.</param>
        void Execute(int startIndex, int count);
    }

    /// <summary>
    /// Extension class for the IJobParallelForBatch job type providing custom overloads for scheduling and running.
    /// </summary>
    public static class IJobParallelForBatchExtensions
    {
        internal struct JobParallelForBatchProducer<T> where T : struct, IJobParallelForBatch
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobParallelForBatchProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public unsafe static void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    if (!JobsUtility.GetWorkStealingRange(
                        ref ranges,
                        jobIndex, out int begin, out int end))
                        return;

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);

                    jobData.Execute(begin, end - begin);
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
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobParallelForBatch&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobParallelForBatch
        {
            JobParallelForBatchProducer<T>.Initialize();
        }

        static IntPtr GetReflectionData<T>()
            where T : struct, IJobParallelForBatch
        {
            JobParallelForBatchProducer<T>.Initialize();
            var reflectionData = JobParallelForBatchProducer<T>.jobReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle Schedule<T>(this T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, indicesPerJobCount);
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleByRef<T>(this ref T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, indicesPerJobCount);
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, indicesPerJobCount);
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleParallelByRef<T>(this ref T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, indicesPerJobCount);
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleBatch<T>(this T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            return ScheduleParallel(jobData, arrayLength, indicesPerJobCount, dependsOn);
        }

        /// <summary>
        /// Schedules a job that will execute the parallel batch job for all `arrayLength` elements in batches of `indicesPerJobCount`.
        /// The Execute() method for Job T will be provided the start index and number of elements to safely operate on.
        /// In cases where `indicesPerJobCount` is not a multiple of `arrayLength`, the `count` provided to the Execute method of Job T will be smaller than the `indicesPerJobCount` specified here.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleBatchByRef<T>(this ref T jobData, int arrayLength, int indicesPerJobCount,
            JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBatch
        {
            return ScheduleParallelByRef(ref jobData, arrayLength, indicesPerJobCount, dependsOn);
        }

        /// <summary>
        /// Executes the parallel batch job but on the main thread. See IJobParallelForBatchExtensions.Schedule for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch. This argument is ignored when using .Run()</param>
        /// <typeparam name="T">Job type</typeparam>
        /// <remarks>
        /// Unlike Schedule, since the job is running on the main thread no parallelization occurs and thus no `indicesPerJobCount` batch size is required to be specified.
        /// </remarks>
        public static unsafe void Run<T>(this T jobData, int arrayLength, int indicesPerJobCount) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        /// <summary>
        /// Executes the parallel batch job but on the main thread. See IJobParallelForBatchExtensions.Schedule for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <param name="indicesPerJobCount">Number of elements to consider in a single parallel batch. This argument is ignored when using .RunByRef()</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunByRef<T>(this ref T jobData, int arrayLength, int indicesPerJobCount) where T : struct, IJobParallelForBatch
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        /// <summary>
        /// Executes the parallel batch job but on the main thread. See IJobParallelForBatchExtensions.ScheduleBatch for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <typeparam name="T">Job type</typeparam>
        /// <remarks>
        /// Unlike ScheduleBatch, since the job is running on the main thread no parallelization occurs and thus no `indicesPerJobCount` batch size is required to be specified.
        /// </remarks>
        public static unsafe void RunBatch<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForBatch
        {
            Run(jobData, arrayLength, arrayLength);
        }

        /// <summary>
        /// Executes the parallel batch job but on the main thread. See IJobParallelForBatchExtensions.ScheduleBatch for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="arrayLength">Total number of elements to consider when batching.</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunBatchByRef<T>(this ref T jobData, int arrayLength) where T : struct, IJobParallelForBatch
        {
            RunByRef(ref jobData, arrayLength, arrayLength);
        }
    }
}
