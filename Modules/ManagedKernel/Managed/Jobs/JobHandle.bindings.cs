// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    ///<summary>Represents a handle to a job, which uniquely identifies a job scheduled in the job system.</summary>
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindings.h")]
    public struct JobHandle : IEquatable<JobHandle>
    {
        internal ulong jobGroup;
        internal int   version; // maps to isManual internally. Remove in 2023

        internal int    debugVersion;
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr debugInfo;

        ///<summary>Ensures that a job has completed.</summary>
        ///<remarks>The job system automatically prioritizes the job and any of its dependencies to run first in the queue, then attempts to execute the job on 
        ///the thread which calls the Complete method.
        ///
        ///**Note:** You can't use this method in single-threaded WebGL builds where the job represents a network transfer because HTTP transfers in web browsers must run to completion asynchronously.</remarks>
        public void Complete()
        {
            if (jobGroup == 0)
                return;

            ScheduleBatchedJobsAndComplete(ref this);
        }

        ///<summary>Ensures that all jobs have completed.</summary>
        ///<remarks>The job system automatically prioritizes all the given jobs and any of its dependencies to run first in the queue, then attempts to execute all 
        ///the jobs if they aren't executing on the worker threads yet. It completes as soon as all referenced jobs have completed.</remarks>
        unsafe public static void CompleteAll(ref JobHandle job0, ref JobHandle job1)
        {
            JobHandle* jobs = stackalloc JobHandle[2];
            jobs[0] = job0;
            jobs[1] = job1;
            ScheduleBatchedJobsAndCompleteAll(jobs, 2);

            job0 = new JobHandle();
            job1 = new JobHandle();
        }

        ///<summary>Ensures that all jobs have completed.</summary>
        ///<remarks>The job system automatically prioritizes all the given jobs and any of its dependencies to run first in the queue, then attempts to execute all 
        ///the jobs if they aren't executing on the worker threads yet. It completes as soon as all referenced jobs have completed.</remarks>
        unsafe public static void CompleteAll(ref JobHandle job0, ref JobHandle job1, ref JobHandle job2)
        {
            JobHandle* jobs = stackalloc JobHandle[3];
            jobs[0] = job0;
            jobs[1] = job1;
            jobs[2] = job2;
            ScheduleBatchedJobsAndCompleteAll(jobs, 3);

            job0 = new JobHandle();
            job1 = new JobHandle();
            job2 = new JobHandle();
        }

        ///<summary>Ensures that all jobs have completed.</summary>
        ///<remarks>The job system automatically prioritizes all the given jobs and any of its dependencies to run first in the queue, then attempts to execute all 
        ///the jobs if they aren't executing on the worker threads yet. It completes as soon as all referenced jobs have completed.</remarks>
        public unsafe static void CompleteAll(NativeArray<JobHandle> jobs)
        {
            ScheduleBatchedJobsAndCompleteAll(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        ///<summary>Determines if a task is running.</summary>
        ///<remarks>Returns false if the task is currently running. Returns true if the task has completed.</remarks>
        public bool IsCompleted { get { return ScheduleBatchedJobsAndIsCompleted(ref this); } }

        ///<summary>Makes Schedule methods available to worker threads.</summary>
        ///<remarks>By default, jobs are only put on a local queue when using job Schedule methods. <c>ScheduleBatchedJobs</c> makes them available to the worker threads to 
        ///execute. The job system intentionally delays job execution until you call <c>ScheduleBatchedJobs</c> manually because the cost of waking up worker threads can be expensive. It's 
        ///best practic to delay waking up worker threads until a few jobs have been scheduled. If you're scheduling jobs in a loop, wait to schedule the jobs until the end of the loop. 
        ///If you do significant amounts of work on the main thread between scheduling jobs, then it can make sense to <c>ScheduleBatchedJobs</c> between each job.</remarks>
        [NativeMethod("ScheduleBatchedScriptingJobs", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern  void ScheduleBatchedJobs();

        [NativeMethod("ScheduleBatchedScriptingJobsAndComplete", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern void      ScheduleBatchedJobsAndComplete(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndIsCompleted", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern bool      ScheduleBatchedJobsAndIsCompleted(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndCompleteAll", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ScheduleBatchedJobsAndCompleteAll(void* jobs, int count);


        ///<summary>Combines multiple dependencies into a single dependency.</summary>
        ///<remarks>All job schedule methods for <see cref="IJob" /> or <see cref="IJobParallelFor" /> job types take a single dependency. Sometimes you might need to express dependencies 
        ///against multiple running jobs at the same time. Use this method to combine a set of dependencies into a single dependency that can be passed to a job.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Schedule 3 jobs, jobs a and b can run in parallel to each other,
        /// // job c will only run once both jobA and jobB has completed
        ///
        /// // Schedule job a
        ///var jobA = new MyJob(...);
        ///var jobAHandle = jobA.Schedule();
        ///
        /// // Schedule job b
        ///var jobB = new MyJob(...);
        ///var jobBHandle = jobB.Schedule();
        ///
        /// // For job c, combine dependencies of job a and b
        /// // Then use that for scheduling the next job
        ///var jobC = new DependentJob(...);
        ///var dependency = JobHandle.CombineDependencies(jobAHandle, jobBHandle);
        ///jobC.Schedule(dependency);
        ///]]></code>
        ///</example>
        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1)
        {
            return CombineDependenciesInternal2(ref job0, ref job1);
        }

        ///<summary>Combines multiple dependencies into a single dependency.</summary>
        ///<remarks>All job schedule methods for <see cref="IJob" /> or <see cref="IJobParallelFor" /> job types take a single dependency. Sometimes you might need to express dependencies 
        ///against multiple running jobs at the same time. Use this method to combine a set of dependencies into a single dependency that can be passed to a job.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Schedule 3 jobs, jobs a and b can run in parallel to each other,
        /// // job c will only run once both jobA and jobB has completed
        ///
        /// // Schedule job a
        ///var jobA = new MyJob(...);
        ///var jobAHandle = jobA.Schedule();
        ///
        /// // Schedule job b
        ///var jobB = new MyJob(...);
        ///var jobBHandle = jobB.Schedule();
        ///
        /// // For job c, combine dependencies of job a and b
        /// // Then use that for scheduling the next job
        ///var jobC = new DependentJob(...);
        ///var dependency = JobHandle.CombineDependencies(jobAHandle, jobBHandle);
        ///jobC.Schedule(dependency);
        ///]]></code>
        ///</example>
        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1, JobHandle job2)
        {
            return CombineDependenciesInternal3(ref job0, ref job1, ref job2);
        }

        ///<summary>Combines multiple dependencies into a single dependency.</summary>
        ///<remarks>All job schedule methods for <see cref="IJob" /> or <see cref="IJobParallelFor" /> job types take a single dependency. Sometimes you might need to express dependencies 
        ///against multiple running jobs at the same time. Use this method to combine a set of dependencies into a single dependency that can be passed to a job.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Schedule 3 jobs, jobs a and b can run in parallel to each other,
        /// // job c will only run once both jobA and jobB has completed
        ///
        /// // Schedule job a
        ///var jobA = new MyJob(...);
        ///var jobAHandle = jobA.Schedule();
        ///
        /// // Schedule job b
        ///var jobB = new MyJob(...);
        ///var jobBHandle = jobB.Schedule();
        ///
        /// // For job c, combine dependencies of job a and b
        /// // Then use that for scheduling the next job
        ///var jobC = new DependentJob(...);
        ///var dependency = JobHandle.CombineDependencies(jobAHandle, jobBHandle);
        ///jobC.Schedule(dependency);
        ///]]></code>
        ///</example>
        unsafe public static JobHandle CombineDependencies(NativeArray<JobHandle> jobs)
        {
            return CombineDependenciesInternalPtr(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        ///<summary>Combines multiple dependencies into a single dependency.</summary>
        ///<remarks>All job schedule methods for <see cref="IJob" /> or <see cref="IJobParallelFor" /> job types take a single dependency. Sometimes you might need to express dependencies 
        ///against multiple running jobs at the same time. Use this method to combine a set of dependencies into a single dependency that can be passed to a job.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Schedule 3 jobs, jobs a and b can run in parallel to each other,
        /// // job c will only run once both jobA and jobB has completed
        ///
        /// // Schedule job a
        ///var jobA = new MyJob(...);
        ///var jobAHandle = jobA.Schedule();
        ///
        /// // Schedule job b
        ///var jobB = new MyJob(...);
        ///var jobBHandle = jobB.Schedule();
        ///
        /// // For job c, combine dependencies of job a and b
        /// // Then use that for scheduling the next job
        ///var jobC = new DependentJob(...);
        ///var dependency = JobHandle.CombineDependencies(jobAHandle, jobBHandle);
        ///jobC.Schedule(dependency);
        ///]]></code>
        ///</example>
        unsafe public static JobHandle CombineDependencies(NativeSlice<JobHandle> jobs)
        {
            return CombineDependenciesInternalPtr(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern JobHandle CombineDependenciesInternal2(ref JobHandle job0, ref JobHandle job1);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern JobHandle CombineDependenciesInternal3(ref JobHandle job0, ref JobHandle job1, ref JobHandle job2);

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        internal static extern unsafe JobHandle CombineDependenciesInternalPtr(void* jobs, int count);

        ///<summary>CheckFenceIsDependencyOrDidSyncFence.</summary>
        ///<param name="jobHandle">Job handle.</param>
        ///<param name="dependsOn">Job handle dependency.</param>
        ///<returns>Return value.</returns>
        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool CheckFenceIsDependencyOrDidSyncFence(JobHandle jobHandle, JobHandle dependsOn);

        // A manual job fence is completed by an explicit call rather than by a job finishing.
        // Jobs with a manual fence as a dependency will not run until CompleteManualJobFence is called, which makes
        // manual fences useful for creating jobs that are dependent on an asynchronous operation like file reads, or
        // for holding jobs in a known un-executed state (for example to test that dependencies exist between
        // two jobs).
        //
        // Note the asymmetry with the rest of the JobHandle API, which can easily cause deadlocks:
        //   Complete() *waits* for the fence, and will block forever unless something else signals it.
        //   CompleteManualJobFence *signals* the fence, releasing anything that depends on it.
        // Completing a handle that depends on a manual fence will block forever unless there is already a
        // thread that will complete the manual fence. The handle-combining APIs wait as well, so a manual
        // fence reached through CompleteAll or CombineDependencies must still be signaled explicitly or the
        // wait will hang.
        //
        // Unlike other JobHandle operations, this can be called from any thread. Because of the potential for
        // deadlocks if used improperly, it is left as an internal API.
        //
        // Manual fences have no purpose or meaning on a single-threaded system, so they are not available there
        // and will return a default JobHandle. They are not available on Web either, even in threaded builds since
        // waiting on a job that is gated behind a manual fence deadlocks the player there. The native job system
        // does the same thing on threaded Web without deadlocking, so the cause is not yet understood.
        [NativeConditional("ENABLE_JOB_SCHEDULER && !PLATFORM_WEBGL", "JobFence()")]
        [FreeFunction("CreateManualJobFence", isThreadSafe: true)]
        internal static extern JobHandle CreateManualJobFence();

        // Signal a fence created by CreateManualJobFence. Note this is not the counterpart to Complete(), see above.
        // This call will schedule the manual job, immediately complete it, and clear the handle passed by reference.
        //
        // Safe to call from any thread and from inside a job. When several callers race, the first to schedule it will
        // return true, and the rest will do nothing and return false. Also returns false, without reporting an error,
        // for a fence that has already been completed or a default JobHandle, so it is safe to call speculatively
        // during cleanup. Passing a handle that is not a manual fence will report an error and return false, not
        // throw.
        //
        // Like CreateManualJobFence, this API is not available on single-threaded systems or on Web, and will
        // return false.
        [NativeConditional("ENABLE_JOB_SCHEDULER && !PLATFORM_WEBGL", "false")]
        [FreeFunction("CompleteManualJobFence", isThreadSafe: true)]
        internal static extern bool CompleteManualJobFence(ref JobHandle handle);

        ///<exclude />
        public bool Equals(JobHandle other)
        {
            return jobGroup == other.jobGroup;
        }

        public override bool Equals(Object obj)
        {
            return obj is JobHandle && this == (JobHandle)obj;
        }

        ///<exclude />
        public static bool operator ==(JobHandle a, JobHandle b)
        {
            return a.jobGroup == b.jobGroup;
        }

        ///<exclude />
        public static bool operator !=(JobHandle a, JobHandle b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return jobGroup.GetHashCode();
        }

        public override string ToString() => jobGroup.ToString();
    }
}

namespace Unity.Jobs.LowLevel.Unsafe
{
    ///<summary>Contains unsafe utility methods for <see cref="Unity.Jobs.JobHandle" /> instances.</summary>
    public static class JobHandleUnsafeUtility
    {
        ///<summary>Combines multiple dependencies into a single one using an unsafe array of job handles.</summary>
        ///<seealso cref="JobHandle.CombineDependencies" />
        unsafe public static JobHandle CombineDependencies(JobHandle* jobs, int count)
        {
            return JobHandle.CombineDependenciesInternalPtr(jobs, count);
        }
    }
}

