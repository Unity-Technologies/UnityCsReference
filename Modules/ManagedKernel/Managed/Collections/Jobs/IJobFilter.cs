// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using Unity.Burst;
using Unity.Mathematics;

namespace Unity.Jobs
{
    /// <summary>
    ///  **Obsolete.** Use <see cref="IJobFilterExtensions"/> instead.
    /// </summary>
    [Obsolete("'JobParallelIndexListExtensions' has been deprecated; Use 'IJobFilterExtensions' instead.", false)]
    public static class JobParallelIndexListExtensions
    {
        /// <summary>
        /// **Obsolete.**
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="indices"></param>
        /// <param name="arrayLength"></param>
        /// <param name="innerloopBatchCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        [Obsolete("The signature for 'ScheduleAppend' has changed. 'innerloopBatchCount' is no longer part of this API.", false)]
        public static unsafe JobHandle ScheduleAppend<T>(this T jobData, NativeList<int> indices, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobFilter
            => jobData.ScheduleAppend(indices, arrayLength, dependsOn);

        /// <summary>
        /// **Obsolete.**
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="indices"></param>
        /// <param name="innerloopBatchCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        [Obsolete("The signature for 'ScheduleFilter' has changed. 'innerloopBatchCount' is no longer part of this API.")]
        public static unsafe JobHandle ScheduleFilter<T>(this T jobData, NativeList<int> indices, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobFilter
            => jobData.ScheduleFilter(indices, dependsOn);
    }

    /// <summary>
    /// **Obsolete.** Use <see cref="IJobFilter"/> instead.
    /// </summary>
    [Obsolete("'IJobParallelForFilter' has been deprecated; use 'IJobFilter' instead. (UnityUpgradable) -> IJobFilter")]
    public interface IJobParallelForFilter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool Execute(int index);
    }


    /// <summary>
    /// Filters a list of indices.
    /// </summary>
    /// <remarks>
    /// IJobFilter allows for custom jobs to implement a bool Execute(int index) job function used to filter a list of indices.
    /// For a provided list and index range, the list will be modified to append all indices for which Execute returns true or to exclude all indices for which Execute returns false
    /// depending on if ScheduleAppend or Schedule is used, respectfully, for enqueuing the job with the job system.
    /// </remarks>
    [JobProducerType(typeof(IJobFilterExtensions.JobFilterProducer<>))]
    public interface IJobFilter
    {
        /// <summary>
        /// Filter function. A list of indices is provided when scheduling this job type. The
        /// Execute function will be called once for each index returning true or false if the job data at
        /// the passed in index should be filtered or not.
        /// </summary>
        /// <param name="index">Index to use when reading job data for the purpose of filtering</param>
        /// <returns>Returns true for data at index</returns>
        bool Execute(int index);
    }

    /// <summary>
    /// Extension class for the IJobFilter job type providing custom overloads for scheduling and running.
    /// </summary>
    public static class IJobFilterExtensions
    {
        internal struct JobFilterProducer<T> where T : struct, IJobFilter
        {
            public struct JobWrapper
            {
                [NativeDisableParallelForRestriction]
                public NativeList<int> outputIndices;
                public int appendCount;
                public T JobData;
            }

            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobFilterProducer<T>>();

            [BurstDiscard]
            internal static void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(JobWrapper), typeof(T), (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(ref JobWrapper jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            /// <summary>
            /// Job Producer method invoked by the Job System when running an IJobFilter Job.
            /// </summary>
            /// <param name="jobWrapper">IJobFilter wrapper type</param>
            /// <param name="additionalPtr">unused</param>
            /// <param name="bufferRangePatchData">Buffer data JobRanges</param>
            /// <param name="ranges">unused</param>
            /// <param name="jobIndex">unused</param>
            public static void Execute(ref JobWrapper jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                if (jobWrapper.appendCount == -1)
                    ExecuteFilter(ref jobWrapper, bufferRangePatchData);
                else
                    ExecuteAppend(ref jobWrapper, bufferRangePatchData);
            }

            public static unsafe void ExecuteAppend(ref JobWrapper jobWrapper, System.IntPtr bufferRangePatchData)
            {
                int oldLength = jobWrapper.outputIndices.Length;
                jobWrapper.outputIndices.Capacity = math.max(jobWrapper.appendCount + oldLength, jobWrapper.outputIndices.Capacity);

                int* outputPtr = (int*)jobWrapper.outputIndices.GetUnsafePtr();
                int outputIndex = oldLength;

                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper),
                    0, jobWrapper.appendCount);
                for (int i = 0; i != jobWrapper.appendCount; i++)
                {
                    if (jobWrapper.JobData.Execute(i))
                    {
                        outputPtr[outputIndex] = i;
                        outputIndex++;
                    }
                }

                jobWrapper.outputIndices.ResizeUninitialized(outputIndex);
            }

            public static unsafe void ExecuteFilter(ref JobWrapper jobWrapper, System.IntPtr bufferRangePatchData)
            {
                int* outputPtr = (int*)jobWrapper.outputIndices.GetUnsafePtr();
                int inputLength = jobWrapper.outputIndices.Length;

                int outputCount = 0;
                for (int i = 0; i != inputLength; i++)
                {
                    int inputIndex = outputPtr[i];

                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobWrapper), inputIndex, 1);

                    if (jobWrapper.JobData.Execute(inputIndex))
                    {
                        outputPtr[outputCount] = inputIndex;
                        outputCount++;
                    }
                }

                jobWrapper.outputIndices.ResizeUninitialized(outputCount);
            }
        }

        /// <summary>
        /// Gathers and caches reflection data for the internal job system's managed bindings. Unity is responsible for calling this method - don't call it yourself.
        /// </summary>
        /// <typeparam name="T">Job type</typeparam>
        /// <remarks>
        /// When the Collections package is included in the project, Unity generates code to call EarlyJobInit at startup. This allows Burst compiled code to schedule jobs because the reflection part of initialization, which is not compatible with burst compiler constraints, has already happened in EarlyJobInit.
        /// 
        /// __Note__: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments (like IJobFilter&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with [[Unity.Jobs.RegisterGenericJobTypeAttribute]].
        /// </remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.Initialize();
        }

        static IntPtr GetReflectionData<T>()
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.Initialize();
            var reflectionData = JobFilterProducer<T>.jobReflectionData.Data;
            CollectionHelper.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        /// <summary>
        /// Schedules a job that will execute the filter job for all integers in indices from index 0 until arrayLength. Each integer which passes the filter (i.e. true is returned from Execute()) will be appended to the indices list.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be appended to this list.</param>
        /// <param name="arrayLength">Number of indices to filter starting from index 0.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleAppend<T>(this T jobData, NativeList<int> indices, int arrayLength, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobFilter
        {
            return jobData.ScheduleAppendByRef(indices, arrayLength, dependsOn);
        }

        /// <summary>
        /// Schedules a job that will execute the filter job for all integers in indices from index 0 until arrayLength. Each integer which passes the filter (i.e. true is returned from Execute()) will be used to repopulate the indices list.
        /// This has the effect of excluding all integer values that do not pass the filter.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be stored in this list.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleFilter<T>(this T jobData, NativeList<int> indices, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobFilter
        {
            return jobData.ScheduleFilterByRef(indices, dependsOn);
        }

        /// <summary>
        /// Executes the appending filter job, on the main thread. See IJobFilterExtensions.ScheduleAppend for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="indices">List of indices to be filtered and appended to.</param>
        /// <param name="arrayLength">Length of array the filter job will append to.</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunAppend<T>(this T jobData, NativeList<int> indices, int arrayLength)
            where T : struct, IJobFilter
        {
            jobData.RunAppendByRef(indices, arrayLength);
        }

        /// <summary>
        /// Executes the filter job, on the main thread. See IJobFilterExtensions.Schedule for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be stored in this list.</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunFilter<T>(this T jobData, NativeList<int> indices)
            where T : struct, IJobFilter
        {
            jobData.RunFilterByRef(indices);
        }

        /// <summary>
        /// Schedules a job that will execute the filter job for all integers in indices from index 0 until arrayLength. Each integer which passes the filter (i.e. true is returned from Execute()) will be appended to the indices list.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be appended to this list.</param>
        /// <param name="arrayLength">Number of indices to filter starting from index 0.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleAppendByRef<T>(ref this T jobData, NativeList<int> indices, int arrayLength, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.JobWrapper jobWrapper = new JobFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = arrayLength
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        /// <summary>
        /// Schedules a job that will execute the filter job for all integers in indices from index 0 until arrayLength. Each integer which passes the filter (i.e. true is returned from Execute()) will be used to repopulate the indices list.
        /// This has the effect of excluding all integer values that do not pass the filter.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be stored in this list.</param>
        /// <param name="dependsOn">Dependencies are used to ensure that a job executes on workerthreads after the dependency has completed execution. Making sure that two jobs reading or writing to same data do not run in parallel.</param>
        /// <returns>JobHandle The handle identifying the scheduled job. Can be used as a dependency for a later job or ensure completion on the main thread.</returns>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe JobHandle ScheduleFilterByRef<T>(ref this T jobData, NativeList<int> indices, JobHandle dependsOn = new JobHandle())
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.JobWrapper jobWrapper = new JobFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = -1
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        /// <summary>
        /// Executes the appending filter job, on the main thread. See IJobFilterExtensions.ScheduleAppend for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be appended to this list.</param>
        /// <param name="arrayLength">Number of indices to filter starting from index 0.</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunAppendByRef<T>(ref this T jobData, NativeList<int> indices, int arrayLength)
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.JobWrapper jobWrapper = new JobFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = arrayLength
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }

        /// <summary>
        /// Executes the filter job, on the main thread. See IJobFilterExtensions.Schedule for more information on how appending is performed.
        /// </summary>
        /// <param name="jobData">The job and data to schedule. In this variant, the jobData is
        /// passed by reference, which may be necessary for unusually large job structs.</param>
        /// <param name="indices">List of indices to be filtered. Filtered results will be stored in this list.</param>
        /// <typeparam name="T">Job type</typeparam>
        public static unsafe void RunFilterByRef<T>(ref this T jobData, NativeList<int> indices)
            where T : struct, IJobFilter
        {
            JobFilterProducer<T>.JobWrapper jobWrapper = new JobFilterProducer<T>.JobWrapper()
            {
                JobData = jobData,
                outputIndices = indices,
                appendCount = -1
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobWrapper), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
