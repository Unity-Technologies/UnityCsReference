// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [NativeType(Header = "Runtime/Jobs/ScriptBindings/JobsBindings.h")]
    public struct JobHandle
    {
        internal IntPtr  jobGroup;
        internal int     version;

        public void Complete()
        {
            if (jobGroup == IntPtr.Zero)
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

        // Jobs are not scheduled immediately.
        // They are scheduled when you call JobHandle.ScheduleBatchedJobs or JobHandle.Complete();
        // This is done for performance reasons since scheduling individual jobs results in expensive Semaphore.Signal calls
        // By scheduling many jobs at the same time delayed this cost will instead be paid only once per ScheduleBatchedJobs calls.
        [NativeMethod(IsFreeFunction = true)]
        public static extern  void ScheduleBatchedJobs();

        [NativeMethod(IsFreeFunction = true)]
        static extern void      ScheduleBatchedJobsAndComplete(ref JobHandle job);

        [NativeMethod(IsFreeFunction = true)]
        static extern bool      ScheduleBatchedJobsAndIsCompleted(ref JobHandle job);

        [NativeMethod(IsFreeFunction = true)]
        unsafe extern static void ScheduleBatchedJobsAndCompleteAll(void* jobs, int count);


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

        [NativeMethod(IsFreeFunction = true)]
        static extern JobHandle CombineDependenciesInternal2(ref JobHandle job0, ref JobHandle job1);

        [NativeMethod(IsFreeFunction = true)]
        static extern JobHandle CombineDependenciesInternal3(ref JobHandle job0, ref JobHandle job1, ref JobHandle job2);

        [NativeMethod(IsFreeFunction = true)]
        unsafe internal static extern JobHandle CombineDependenciesInternalPtr(void* jobs, int count);

        [NativeMethod(IsFreeFunction = true)]
        public static extern bool CheckFenceIsDependencyOrDidSyncFence(JobHandle jobHandle, JobHandle dependsOn);
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

