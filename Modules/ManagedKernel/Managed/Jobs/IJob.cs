// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace Unity.Jobs
{
    [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.ParticleSystemModule")]
    internal static class JobValidationInternal
    {
        [VisibleToOtherModules("UnityEngine.CoreModule", "UnityEngine.ParticleSystemModule")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckReflectionDataCorrect<T>(IntPtr reflectionData)
        {
            bool burstCompiled = true;
            CheckReflectionDataCorrectInternal<T>(reflectionData, ref burstCompiled);
            if (burstCompiled && reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))] in your source file.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        static void CheckReflectionDataCorrectInternal<T>(IntPtr reflectionData, ref bool burstCompiled)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException($"Reflection data was not set up by an Initialize() call.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof({typeof(T)}))] in your source file.");
            burstCompiled = false;
        }
    }


    ///<summary>An interface that allows you to schedule a single job that runs in parallel to other jobs and the main thread.</summary>
    ///<remarks>After a job is scheduled, the job's Execute method is invoked on a worker thread. You can use the returned <see cref="Unity.Jobs.JobHandle" /> to make sure that the job 
    ///has completed. You can also pass the JobHandle to other jobs as a dependency, which ensures that jobs are executed one after another on the worker threads.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[
    ///                    {code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijob-apidocs-example}
    ///]]></code>
    ///</example>
    ///<seealso cref="Unity.Jobs.IJobForExtensions.ScheduleByRef" />
    [JobProducerType(typeof(IJobExtensions.JobStruct<>))]
    public interface IJob
    {
        ///<summary>Implement this method to perform work on a worker thread.</summary>
        ///<example nocheck="true">
        ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijob-apidocs-example}]]></code>
        ///</example>
        void Execute();
    }

    ///<summary>Contains extension methods for jobs implementing the <see cref="IJob" /> interface.</summary>
    ///<remarks>The methods in this extension class add an instance of a job to the work queue, while <see cref="IJob.Execute" /> defines how to perform an asynchronous task. Use <see cref="IJobExtensions.ScheduleByRef" /> to request asynchronous execution, or <see cref="IJobExtensions.RunByRef" /> to execute the job immediately on the main thread.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Jobs/Assets/DocumentationExamples/ManagedJobDocExamples.cs#ijob-apidocs-example}]]></code>
    ///</example>
    ///<seealso cref="IJob" />
    public static class IJobExtensions
    {
        internal struct JobStruct<T> where T : struct, IJob
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobStruct<T>>();

            [BurstDiscard]
            internal static unsafe void Initialize()
            {
                if (jobReflectionData.Data == IntPtr.Zero)
                    jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
            }

            internal delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                data.Execute();
            }
        }

        ///<summary>Gathers and caches reflection data for the internal job system's managed bindings.</summary>
        ///<remarks>Unity is responsible for calling this method: don't call it yourself. When the Jobs package is included in the project, Unity generates 
        ///code to call EarlyJobInit at startup. This results in the following benefits:
        ///
        ///* Job initialization doesn't lazily occur during job scheduling, which would increase the time it takes to schedule a job.
        ///* Burst compiled code may schedule jobs because the reflection part of initialization, which is not compatible with Burst compiler constraints, has already happened in EarlyJobInit.
        ///
        ///**Note**: While the Jobs package code generator handles this automatically for all closed job types, you must register those with generic arguments 
        ///(like IJob&amp;lt;MyJobType&amp;lt;T&amp;gt;&amp;gt;) manually for each specialization with <see cref="Unity.Jobs.RegisterGenericJobTypeAttribute" />.</remarks>
        public static void EarlyJobInit<T>()
            where T : struct, IJob
        {
            JobStruct<T>.Initialize();
        }

        static IntPtr GetReflectionData<T>()
            where T : struct, IJob
        {
            JobStruct<T>.Initialize();
            var reflectionData = JobStruct<T>.jobReflectionData.Data;
            JobValidationInternal.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }

        ///<summary>Schedules the job for execution on a worker thread.</summary>
        ///<remarks>For large job structs, use <see cref="IJobExtensions.ScheduleByRef" /> to avoid any large pass-by-values.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="dependsOn">The dependency of the job. Dependencies ensure that a job executes on worker threads after the dependency has completed execution, and
        ///that two jobs reading or writing to same data do not run in parallel.</param>
        ///<returns>The handle identifying the scheduled job, which you can use as a dependency for a later job or to ensure completion on the main thread.</returns>
        ///<seealso cref="IJob" />
        unsafe public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        ///<summary>Performs the job's Execute method immediately on the same thread.</summary>
        ///<remarks>While this method is useful in development or test environments, it is typically a mistake to use it in production; it offers no real benefits over implementing the job code as a simple main-thread function call. To schedule a job to run asynchronously, use <see cref="IJobExtensions.ScheduleByRef" />.
        ///
        ///For large job structs, use <see cref="IJobExtensions.RunByRef" /> to avoid any large pass-by-values.</remarks>
        ///<param name="jobData">The job and data to run.</param>
        ///<seealso cref="IJob" />
        unsafe public static void Run<T>(this T jobData) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }

        ///<summary>Schedules the job for execution on a worker thread.</summary>
        ///<remarks>This variant passes the job struct by reference instead of by value, which can be faster than <see cref="IJobExtensions.Schedule" /> for larger job structs. Note that worker threads always operate on a local copy of the job struct.</remarks>
        ///<param name="jobData">The job and data to schedule.</param>
        ///<param name="dependsOn">The dependency of the job. Dependencies ensure that a job executes on worker threads after the dependency has completed execution, and
        ///that two jobs reading or writing to same data do not run in parallel.</param>
        ///<returns>The handle identifying the scheduled job, which you can use as a dependency for a later job or to ensure completion on the main thread.</returns>
        ///<seealso cref="IJob" />
        unsafe public static JobHandle ScheduleByRef<T>(ref this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        ///<summary>Performs the job's Execute method immediately on the main thread.</summary>
        ///<remarks>This variant passes the job struct by reference instead of by value, which can be faster than <see cref="IJobExtensions.Run" /> for larger job structs. Note that worker threads always operate on a local copy of the job struct.
        ///
        ///While this method is useful in development or test environments, it is typically a mistake to use it in production; it offers no real benefits over implementing the job code as a simple main-thread function call. To schedule a job to run asynchronously, use <see cref="IJobExtensions.ScheduleByRef" />.</remarks>
        ///<param name="jobData">The job and data to run.</param>
        ///<seealso cref="IJob" />
        unsafe public static void RunByRef<T>(ref this T jobData) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
