// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;
using Unity.Burst;
using System.Diagnostics;
using UnityEngine.Bindings;

namespace Unity.Jobs
{
    [VisibleToOtherModules("UnityEngine.ParticleSystemModule")]
    internal static class JobValidationInternal
    {
        [VisibleToOtherModules("UnityEngine.ParticleSystemModule")]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckReflectionDataCorrect<T>(IntPtr reflectionData)
        {
            bool burstCompiled = true;
            CheckReflectionDataCorrectInternal<T>(reflectionData, ref burstCompiled);
            if (burstCompiled && reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call. Support for burst compiled calls to Schedule depends on the Collections package.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))] in your source file.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        static void CheckReflectionDataCorrectInternal<T>(IntPtr reflectionData, ref bool burstCompiled)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException($"Reflection data was not set up by an Initialize() call. Support for burst compiled calls to Schedule depends on the Collections package.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof({typeof(T)}))] in your source file.");
            burstCompiled = false;
        }
    }


    [JobProducerType(typeof(IJobExtensions.JobStruct<>))]
    public interface IJob
    {
        void Execute();
    }

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

        unsafe public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static void Run<T>(this T jobData) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static JobHandle ScheduleByRef<T>(ref this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), dependsOn, ScheduleMode.Single);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static void RunByRef<T>(ref this T jobData) where T : struct, IJob
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), GetReflectionData<T>(), new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
