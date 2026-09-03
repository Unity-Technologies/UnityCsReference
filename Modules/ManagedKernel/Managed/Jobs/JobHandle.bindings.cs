// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [NativeHeader("ManagedKernel/Jobs/ScriptBindings/JobsBindings.h")]
    public struct JobHandle : IEquatable<JobHandle>
    {
        internal ulong jobGroup;
        internal int   version; // maps to isManual internally. Remove in 2023

        internal int    debugVersion;
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr debugInfo;

        public void Complete()
        {
            if (jobGroup == 0)
                return;

            ScheduleBatchedJobsAndComplete(ref this);
        }

        unsafe public static void CompleteAll(ref JobHandle job0, ref JobHandle job1)
        {
            JobHandle* jobs = stackalloc JobHandle[2];
            jobs[0] = job0;
            jobs[1] = job1;
            ScheduleBatchedJobsAndCompleteAll(jobs, 2);

            job0 = new JobHandle();
            job1 = new JobHandle();
        }

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

        public unsafe static void CompleteAll(NativeArray<JobHandle> jobs)
        {
            ScheduleBatchedJobsAndCompleteAll(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

        public bool IsCompleted { get { return ScheduleBatchedJobsAndIsCompleted(ref this); } }

        [NativeMethod("ScheduleBatchedScriptingJobs", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern  void ScheduleBatchedJobs();

        [NativeMethod("ScheduleBatchedScriptingJobsAndComplete", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern void      ScheduleBatchedJobsAndComplete(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndIsCompleted", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern bool      ScheduleBatchedJobsAndIsCompleted(ref JobHandle job);

        [NativeMethod("ScheduleBatchedScriptingJobsAndCompleteAll", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        static extern unsafe void ScheduleBatchedJobsAndCompleteAll(void* jobs, int count);


        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1)
        {
            return CombineDependenciesInternal2(ref job0, ref job1);
        }

        public static JobHandle CombineDependencies(JobHandle job0, JobHandle job1, JobHandle job2)
        {
            return CombineDependenciesInternal3(ref job0, ref job1, ref job2);
        }

        unsafe public static JobHandle CombineDependencies(NativeArray<JobHandle> jobs)
        {
            return CombineDependenciesInternalPtr(jobs.GetUnsafeReadOnlyPtr(), jobs.Length);
        }

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

        public bool Equals(JobHandle other)
        {
            return jobGroup == other.jobGroup;
        }

        public override bool Equals(Object obj)
        {
            return obj is JobHandle && this == (JobHandle)obj;
        }

        public static bool operator ==(JobHandle a, JobHandle b)
        {
            return a.jobGroup == b.jobGroup;
        }

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
    public static class JobHandleUnsafeUtility
    {
        unsafe public static JobHandle CombineDependencies(JobHandle* jobs, int count)
        {
            return JobHandle.CombineDependenciesInternalPtr(jobs, count);
        }
    }
}

