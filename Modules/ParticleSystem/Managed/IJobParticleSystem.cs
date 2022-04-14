// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using System.Diagnostics;

namespace UnityEngine.ParticleSystemJobs
{
    [JobProducerType(typeof(ParticleSystemJobStruct<>))]
    public interface IJobParticleSystem
    {
        void Execute(ParticleSystemJobData jobData);
    }

    [JobProducerType(typeof(ParticleSystemParallelForJobStruct<>))]
    public interface IJobParticleSystemParallelFor
    {
        void Execute(ParticleSystemJobData jobData, int index);
    }

    [JobProducerType(typeof(ParticleSystemParallelForBatchJobStruct<>))]
    public interface IJobParticleSystemParallelForBatch
    {
        void Execute(ParticleSystemJobData jobData, int startIndex, int count);
    }

    public static class IJobParticleSystemExtensions
    {
        public static void EarlyJobInit<T>()
            where T : struct, IJobParticleSystem
        {
            ParticleSystemJobStruct<T>.Initialize();
        }

        internal static IntPtr GetReflectionData<T>()
            where T : struct, IJobParticleSystem
        {
            ParticleSystemJobStruct<T>.Initialize();
            var reflectionData = ParticleSystemJobStruct<T>.jobReflectionData.Data;
            ParticleSystemJobUtility.CheckReflectionDataCorrect(reflectionData);
            return reflectionData;
        }
    }

    public static class IJobParticleSystemParallelForExtensions
    {
        public static void EarlyJobInit<T>()
            where T : struct, IJobParticleSystemParallelFor
        {
            ParticleSystemParallelForJobStruct<T>.Initialize();
        }

        internal static IntPtr GetReflectionData<T>()
            where T : struct, IJobParticleSystemParallelFor
        {
            ParticleSystemParallelForJobStruct<T>.Initialize();
            var reflectionData = ParticleSystemParallelForJobStruct<T>.jobReflectionData.Data;
            ParticleSystemJobUtility.CheckReflectionDataCorrect(reflectionData);
            return reflectionData;
        }
    }

    public static class IJobParticleSystemParallelForBatchExtensions
    {
        public static void EarlyJobInit<T>()
            where T : struct, IJobParticleSystemParallelForBatch
        {
            ParticleSystemParallelForBatchJobStruct<T>.Initialize();
        }

        internal static IntPtr GetReflectionData<T>()
            where T : struct, IJobParticleSystemParallelForBatch
        {
            ParticleSystemParallelForBatchJobStruct<T>.Initialize();
            var reflectionData = ParticleSystemParallelForBatchJobStruct<T>.jobReflectionData.Data;
            ParticleSystemJobUtility.CheckReflectionDataCorrect(reflectionData);
            return reflectionData;
        }
    }

    static class ParticleSystemJobUtility
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Support for burst compiled calls to Schedule depends on the Jobs package.\n\nFor generic job types, please include [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))] in your source file.");
        }

        unsafe internal static JobsUtility.JobScheduleParameters CreateScheduleParams<T>(ref T jobData, ParticleSystem ps, JobHandle dependsOn, IntPtr jobReflectionData) where T : struct
        {
            dependsOn = JobHandle.CombineDependencies(ps.GetManagedJobHandle(), dependsOn);
            return new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), jobReflectionData, dependsOn, ScheduleMode.Parallel);
        }
    }

    public static class IParticleSystemJobExtensions
    {
        unsafe public static JobHandle Schedule<T>(this T jobData, ParticleSystem ps, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystem
        {
            var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemExtensions.GetReflectionData<T>());
            var handle = ParticleSystem.ScheduleManagedJob(ref scheduleParams, ps.GetManagedJobData());
            ps.SetManagedJobHandle(handle);
            return handle;
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, ParticleSystem ps, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystemParallelFor
        {
            var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemParallelForExtensions.GetReflectionData<T>());
            var handle = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, ps.GetManagedJobData(), null);
            ps.SetManagedJobHandle(handle);
            return handle;
        }

        unsafe public static JobHandle ScheduleBatch<T>(this T jobData, ParticleSystem ps, int innerLoopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystemParallelForBatch
        {
            var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemParallelForBatchExtensions.GetReflectionData<T>());
            var handle = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerLoopBatchCount, ps.GetManagedJobData(), null);
            ps.SetManagedJobHandle(handle);
            return handle;
        }
    }
}

