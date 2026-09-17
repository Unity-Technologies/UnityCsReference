// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using System.Diagnostics;

namespace Unity.Jobs
{
    ///<summary>An interface that represents a job that performs the same independent operation for each element of a native container or for a fixed number of iterations.</summary>
    ///<remarks>This job type has the following options to schedule work:
    ///
    ///- <see cref="Unity.Jobs.IJobForExtensions.RunByRef" /> runs the job on the main thread and finishes immediately.
    ///- <see cref="Unity.Jobs.IJobForExtensions.ScheduleByRef" /> schedules the job to run on a worker thread or the main thread, but indicates that the work should happen in a single thread. This option allows for work to be done off the main thread, but the work is performed sequentially.
    ///- <see cref="Unity.Jobs.IJobForExtensions.ScheduleParallelByRef" /> schedules the job to run asynchronously, distributing the work across multiple worker threads. This scheduling option can give the best performance, but race conditions can occur if the job writes to shared or global data without proper synchronization.
    ///
    ///<c>Execute(int index)</c> is executed once for each index from 0 to the provided length.
    ///
    ///<c>RunByRef</c> and <c>ScheduleByRef</c> guarantee that the the job's <c>Execute(int index)</c> method is invoked sequentially. <c>ScheduleParallelByRef</c> doesn't invoke the job's <c>Execute</c> method 
    ///sequentially because it's called from multiple worker threads in parallel to each other.
    ///
    ///Each iteration must be independent from other iterations and the safety system enforces this rule for you. The indices have no guaranteed order and are executed on multiple cores in parallel. 
    ///
    ///Unity automatically splits the work into chunks of no less than the provided <c>batchSize</c>, and schedules an appropriate number of jobs based on the number of worker threads, 
    ///the length of the array and the batch size. You should choose the batch size based on the amount of work performed in the job. If the batch size is too large, work may not be distributed evenly across the available worker threads. If it's too small, the overhead of fetching new work items may dominate the time it takes to process them. A simple job, for example adding a couple of 
    ///Vector3 to each other should have a batch size of 32 to 128. However, if the work performed has a large overhead then it's best practice to use a small batch size, for example,
    /// a batch size of 1. Work stealing is performed using atomic operations.
    ///
    ///You can use the returned <c>JobHandle</c> to check that the job has completed, or pass it to other jobs as a dependency. When you pass a <c>JobHandle</c> as a dependency, it
    ///ensures that the jobs are executed one after another on the worker threads.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[
    ///                    {code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijobfor-apidocs-example}
    ///]]></code>
    ///</example>
    ///<seealso cref="Unity.Jobs.IJobForExtensions" />
    [JobProducerType(typeof(IJobForExtensions.ForJobStruct<>))]
    public interface IJobFor
    {
        ///<summary>Performs work against a specific iteration index.</summary>
        ///<param name="index">The index of the for loop to perform work from.</param>
        ///<example nocheck="true">
        ///  <code><![CDATA[
        ///                        {code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijobfor-apidocs-example}
        ///]]></code>
        ///</example>
        void Execute(int index);
    }

    ///<summary>Contains extension methods for jobs implementing the <see cref="IJobFor" /> interface.</summary>
    ///<remarks>The methods in this extension class add an instance of a job to the work queue, while <see cref="IJobFor.Execute" /> defines how to perform an asynchronous task. Use <see cref="IJobForExtensions.ScheduleByRef" /> to request asynchronous execution, or <see cref="IJobForExtensions.RunByRef" /> to execute the job immediately on the main thread.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijobfor-apidocs-example}.]]></code>
    ///</example>
    ///<seealso cref="IJobFor" />
    public static class IJobForExtensions
    {
        internal struct ForJobStruct<T> where T : struct, IJobFor
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<ForJobStruct<T>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        break;

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);

                    var endThatCompilerCanSeeWillNeverChange = end;
                    for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        jobData.Execute(i);
                }
            }
        }

        ///<summary>Gathers and caches reflection data for the internal job system's managed bindings.</summary>
        ///<remarks>Unity is responsible for calling this method - don't call it yourself. When the Jobs package is included in the project, Unity generates code 
        ///to call EarlyJobInit at startup. This results in the following benefits:
        ///
        ///* Job initialization doesn't lazily occur during job scheduling, which would increase the time it takes to schedule a job.
        ///* Burst compiled code may schedule jobs because the reflection part of initialization, which is not compatible with Burst compiler constraints, has already happened in EarlyJobInit.
        ///
        ///**Note**: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobFor&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with <see cref="Unity.Jobs.RegisterGenericJobTypeAttribute" />.</remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobFor
        {
            ForJobStruct<T>.Initialize();
        }

        private static IntPtr GetReflectionData<T>()
            where T : struct, IJobFor
        {
            ForJobStruct<T>.Initialize();
            var reflectionData = ForJobStruct<T>.jobReflectionData.Data;
            JobValidationInternal.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        ///<summary>Schedules the job for execution on a worker thread.</summary>
        ///<remarks>For large job structs, use <see cref="IJobForExtensions.ScheduleByRef" /> to avoid any large pass-by-values.
        ///
        ///This variant processes all loop elements in a single worker thread. To distribute the work across multiple worker threads, use <see cref="IJobForExtensions.ScheduleParallelByRef" />.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<param name="dependency">The <see cref="Unity.Jobs.JobHandle" /> of the job's dependency. You can use dependencies to make sure that a job executes on worker threads 
        ///after the dependency has completed execution and two jobs that read or write to same data don't run in parallel.</param>
        ///<returns>The <see cref="Unity.Jobs.JobHandle" /> of the scheduled job. You can use the <c>JobHandle</c> as a dependency for a later job or to make sure that the job 
        ///completes on the main thread.</returns>
        ///<seealso cref="IJobFor" />
        unsafe public static JobHandle Schedule<T>(this T jobData, int arrayLength, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependency, ScheduleMode.Single);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        ///<summary>Schedules the job to execute concurrently on multiple worker threads.</summary>
        ///<remarks>For large job structs, use <see cref="IJobForExtensions.ScheduleParallelByRef" /> to avoid any large pass-by-values.
        ///
        ///To process all loop elements on a single worker thread, use <see cref="IJobForExtensions.ScheduleByRef" />.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<param name="innerloopBatchCount">The number of iterations which workstealing is performed over. For example, a value of 32 means the job queue steals 32 
        ///iterations and then performs them in an efficient inner loop.</param>
        ///<param name="dependency">The <see cref="Unity.Jobs.JobHandle" /> of the job's dependency. You can use dependencies to make sure that a job executes on worker threads 
        ///after the dependency has completed execution and two jobs that read or write to same data don't run in parallel.</param>
        ///<returns>The <see cref="Unity.Jobs.JobHandle" /> of the scheduled job. You can use the <c>JobHandle</c> as a dependency for a later job or to make sure that the job 
        ///completes on the main thread.</returns>
        ///<seealso cref="IJobFor" />
        unsafe public static JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependency, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        ///<summary>Performs the job's Execute method immediately on the same thread.</summary>
        ///<remarks>While this method is useful in development or test environments, it is typically a mistake to use it in production; it offers no real benefits over implementing the job code as a simple main-thread function call. To schedule a job to run asynchronously, use <see cref="IJobForExtensions.ScheduleByRef" />.
        ///
        ///For large job structs, use <see cref="IJobForExtensions.RunByRef" /> to avoid any large pass-by-values.</remarks>
        ///<param name="jobData">The job and data to execute.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<seealso cref="IJobFor" />
        unsafe public static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        ///<summary>Schedules the job for execution on a worker thread.</summary>
        ///<remarks>This variant passes the job struct by reference instead of by value, which can be faster than <see cref="IJobForExtensions.Schedule" /> for larger job structs. Note that worker threads always operate on a local copy of the job struct.
        ///
        ///This variant processes all loop elements in a single worker thread. To distribute the work across multiple worker threads, use <see cref="IJobForExtensions.ScheduleParallelByRef" />.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<param name="dependency">The <see cref="Unity.Jobs.JobHandle" /> of the job's dependency. You can use dependencies to make sure that a job executes on worker threads 
        ///after the dependency has completed execution and two jobs that read or write to same data don't run in parallel.</param>
        ///<returns>The <see cref="Unity.Jobs.JobHandle" /> of the scheduled job. You can use the <c>JobHandle</c> as a dependency for a later job or to make sure that the job 
        ///completes on the main thread.</returns>
        ///<seealso cref="IJobFor" />
        unsafe public static JobHandle ScheduleByRef<T>(ref this T jobData, int arrayLength, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependency, ScheduleMode.Single);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        ///<summary>Schedules the job to execute concurrently, distributing work across multiple worker threads.</summary>
        ///<remarks>This variant passes the job struct by reference instead of by value, which can be faster than <see cref="IJobForExtensions.ScheduleParallel" /> for larger job structs. Note that worker threads always operate on a local copy of the job struct.
        ///
        ///To process all loop elements on a single worker thread, use <see cref="IJobForExtensions.ScheduleByRef" />.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<param name="innerloopBatchCount">The number of iterations which workstealing is performed over. For example, a value of 32 means the job queue steals 32 
        ///iterations and then performs them in an efficient inner loop.</param>
        ///<param name="dependency">The <see cref="Unity.Jobs.JobHandle" /> of the job's dependency. You can use dependencies to make sure that a job executes on worker threads 
        ///after the dependency has completed execution and two jobs that read or write to same data don't run in parallel.</param>
        ///<returns>The <see cref="Unity.Jobs.JobHandle" /> of the scheduled job. You can use the <c>JobHandle</c> as a dependency for a later job or to make sure that the job 
        ///completes on the main thread.</returns>
        ///<seealso cref="IJobFor" />
        unsafe public static JobHandle ScheduleParallelByRef<T>(ref this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependency, ScheduleMode.Parallel);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        ///<summary>Performs the job's Execute method immediately on the same thread.</summary>
        ///<remarks>This variant passes the job struct by reference instead of by value, which can be faster than <see cref="IJobForExtensions.Run" /> for larger job structs. Note that worker threads always operate on a local copy of the job struct.
        ///
        ///While this method is useful in development or test environments, it is typically a mistake to use it in production; it offers no real benefits over implementing the job code as a simple main-thread function call. To schedule a job to run asynchronously, use <see cref="IJobForExtensions.ScheduleByRef" />.</remarks>
        ///<param name="jobData">The job and data to execute.</param>
        ///<param name="arrayLength">This job's Execute method will be called this many times, with its index argument ranging from 0 to (arrayLength - 1). Typically, this corresponds to the length of an array or array-like container passed in the job struct, but this is not necessarily the case.</param>
        ///<seealso cref="IJobFor" />
        unsafe public static void RunByRef<T>(ref this T jobData, int arrayLength) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
