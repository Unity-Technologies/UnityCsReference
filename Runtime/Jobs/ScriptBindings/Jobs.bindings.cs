// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Jobs.LowLevel.Unsafe
{
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class JobProducerTypeAttribute : Attribute
    {
        public Type ProducerType { get; }

        public JobProducerTypeAttribute(Type producerType)
        {
            ProducerType = producerType;
        }
    }

    public unsafe struct JobRanges
    {
        public int  batchSize;
        public int  numJobs;
        public int  totalIterationCount;
        public int  numPhases;
        public int  indicesPerPhase;

        public IntPtr startEndIndex;
        public IntPtr phaseData;
    }

    public enum ScheduleMode
    {
        Run          = 0,
        Batched      = 1
    }

    public enum JobType
    {
        Single      = 0,
        ParallelFor = 1
    }

    [NativeType(Header = "Runtime/Jobs/ScriptBindings/JobsBindings.h")]
    public static class JobsUtility
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct JobScheduleParameters
        {
            public JobHandle    dependency;
            public int          scheduleMode;
            public IntPtr       reflectionData;
            public IntPtr       jobDataPtr;

            unsafe public JobScheduleParameters(void* i_jobData, IntPtr i_reflectionData, JobHandle i_dependency, ScheduleMode i_scheduleMode)
            {
                dependency = i_dependency;
                jobDataPtr = (IntPtr)i_jobData;
                reflectionData = i_reflectionData;
                scheduleMode = (int)i_scheduleMode;
            }
        }

        public static unsafe void GetJobRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex)
        {
            int* startEndIndices = (int*)ranges.startEndIndex;
            beginIndex = startEndIndices[jobIndex * 2];
            endIndex = startEndIndices[jobIndex * 2 + 1];
        }

        [NativeMethod(IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool GetWorkStealingRange(ref JobRanges ranges, int jobIndex, out int beginIndex, out int endIndex);

        [FreeFunction("ScheduleManagedJob")]
        public static extern JobHandle Schedule(ref JobScheduleParameters parameters);

        [FreeFunction("ScheduleManagedJobParallelFor")]
        public static extern JobHandle ScheduleParallelFor(ref JobScheduleParameters parameters, int arrayLength, int innerloopBatchCount);

        [FreeFunction("ScheduleManagedJobParallelForTransform")]
        public static extern JobHandle ScheduleParallelForTransform(ref JobScheduleParameters parameters, IntPtr transfromAccesssArray);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        unsafe public static extern void PatchBufferMinMaxRanges(IntPtr bufferRangePatchData, void* jobdata, int startIndex, int rangeSize);

        [FreeFunction]
        private static extern IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, JobType jobType, object managedJobFunction0, object managedJobFunction1, object managedJobFunction2);

        public static IntPtr CreateJobReflectionData(Type type, JobType jobType, object managedJobFunction0, object managedJobFunction1 = null, object managedJobFunction2 = null)
        {
            return CreateJobReflectionData(type, type, jobType, managedJobFunction0, managedJobFunction1, managedJobFunction2);
        }

        public static IntPtr CreateJobReflectionData(Type wrapperJobType, Type userJobType, JobType jobType, object managedJobFunction0)
        {
            return CreateJobReflectionData(wrapperJobType, userJobType, jobType, managedJobFunction0, null, null);
        }

        public static extern bool JobDebuggerEnabled {[FreeFunction] get; [FreeFunction] set; }
        public static extern bool JobCompilerEnabled {[FreeFunction] get; [FreeFunction] set; }

        //@TODO: @timj Should we decrease this???
        public const int MaxJobThreadCount = 128;
        public const int CacheLineSize = 64;
    }
}
