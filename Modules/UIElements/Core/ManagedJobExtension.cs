// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Jobs;

namespace UnityEngine.UIElements
{
    // Job extension to either schedule or run a job immediately on platforms that don't fully
    // support managed jobs, i.e. Web Platform with "Native C/C++ multithreading" enabled
    static class ManagedJobExtension
    {
        public static JobHandle ScheduleOrRunJob<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelFor
        {
            return jobData.Schedule(arrayLength, innerloopBatchCount, dependsOn);
        }
    }
}
