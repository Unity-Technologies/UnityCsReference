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
            JobValidationInternal.CheckReflectionDataCorrect<T>(reflectionData);
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
            JobValidationInternal.CheckReflectionDataCorrect<T>(reflectionData);
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
            JobValidationInternal.CheckReflectionDataCorrect<T>(reflectionData);
            return reflectionData;
        }
    }

    static class ParticleSystemJobUtility
    {
        unsafe internal static JobsUtility.JobScheduleParameters CreateScheduleParams<T>(ref T jobData, ParticleSystem ps, JobHandle dependsOn, IntPtr jobReflectionData) where T : struct
        {
            dependsOn = JobHandle.CombineDependencies(ps.GetManagedJobHandle(), dependsOn);
            return new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), jobReflectionData, dependsOn, ScheduleMode.Parallel);
        }
    }

    public static class IParticleSystemJobExtensions
    {
        private static readonly string k_UserJobScheduledOutsideOfCallbackErrorMsg = "Particle System jobs can only be scheduled in MonoBehaviour.OnParticleUpdateJobScheduled()";
        unsafe public static JobHandle Schedule<T>(this T jobData, ParticleSystem ps, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystem
        {
            if (ParticleSystem.UserJobCanBeScheduled())
            {
                var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemExtensions.GetReflectionData<T>());
                var handle = ParticleSystem.ScheduleManagedJob(ref scheduleParams, ps.GetManagedJobData());
                ps.SetManagedJobHandle(handle);
                return handle;
            }
            else
            {
                throw new InvalidOperationException(k_UserJobScheduledOutsideOfCallbackErrorMsg);
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, ParticleSystem ps, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystemParallelFor
        {
            if (ParticleSystem.UserJobCanBeScheduled())
            {
                var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemParallelForExtensions.GetReflectionData<T>());
                var handle = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, minIndicesPerJobCount, ps.GetManagedJobData(), null);
                ps.SetManagedJobHandle(handle);
                return handle;
            }
            else
            {
                throw new InvalidOperationException(k_UserJobScheduledOutsideOfCallbackErrorMsg);
            }
        }

        unsafe public static JobHandle ScheduleBatch<T>(this T jobData, ParticleSystem ps, int innerLoopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParticleSystemParallelForBatch
        {
            if (ParticleSystem.UserJobCanBeScheduled())
            {
                var scheduleParams = ParticleSystemJobUtility.CreateScheduleParams(ref jobData, ps, dependsOn, IJobParticleSystemParallelForBatchExtensions.GetReflectionData<T>());
                var handle = JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerLoopBatchCount, ps.GetManagedJobData(), null);
                ps.SetManagedJobHandle(handle);
                return handle;
            }
            else
            {
                throw new InvalidOperationException(k_UserJobScheduledOutsideOfCallbackErrorMsg);
            }
        }
    }
}

