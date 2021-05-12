// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobExtensions.JobStruct<>))]
    public interface IJob
    {
        void Execute();
    }

    public static class IJobExtensions
    {
        internal struct JobStruct<T> where T : struct, IJob
        {
            public static readonly IntPtr jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);

            public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public static void Execute(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                data.Execute();
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobStruct<T>.jobReflectionData, dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static void Run<T>(this T jobData) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), JobStruct<T>.jobReflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
