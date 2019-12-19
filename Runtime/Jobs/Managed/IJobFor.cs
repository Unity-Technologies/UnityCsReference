// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobForExtensions.ForJobStruct<>))]
    public interface IJobFor
    {
        void Execute(int index);
    }

    public static class IJobForExtensions
    {
        internal struct ForJobStruct<T> where T : struct, IJobFor
        {
            public static IntPtr                            jobReflectionData;

            public static IntPtr Initialize(bool asParallel)
            {
                if (jobReflectionData == IntPtr.Zero)
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                return jobReflectionData;
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

        unsafe public static JobHandle Schedule<T>(this T jobData, int arrayLength, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ForJobStruct<T>.Initialize(false), dependency, ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }

        unsafe public static JobHandle ScheduleParallel<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependency) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ForJobStruct<T>.Initialize(true), dependency, ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        unsafe public static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobFor
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), ForJobStruct<T>.Initialize(false), new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
