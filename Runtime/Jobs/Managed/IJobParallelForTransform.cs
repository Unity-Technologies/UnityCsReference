// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

//@TODO: Move this into Runtime/Transform folder with the test of Transform component
namespace UnityEngine.Jobs
{
    [JobProducerType(typeof(IJobParallelForTransformExtensions.TransformParallelForLoopStruct < >))]
    public interface IJobParallelForTransform
    {
        void Execute(int index, TransformAccess transform);
    }

    public static class IJobParallelForTransformExtensions
    {
        internal struct TransformParallelForLoopStruct<T> where T : struct, IJobParallelForTransform
        {
            static public IntPtr                    jobReflectionData;

            public static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                return jobReflectionData;
            }

            public delegate void ExecuteJobFunction(ref T jobData, System.IntPtr additionalPtr, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
            public static unsafe void Execute(ref T jobData, System.IntPtr jobData2, System.IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                IntPtr transformAccessArray;
                UnsafeUtility.CopyPtrToStructure((void*)jobData2, out transformAccessArray);

                int* sortedToUserIndex = (int*)TransformAccessArray.GetSortedToUserIndex(transformAccessArray);
                TransformAccess* sortedTransformAccess = (TransformAccess*)TransformAccessArray.GetSortedTransformAccess(transformAccessArray);

                int begin;
                int end;
                JobsUtility.GetJobRange(ref ranges, jobIndex, out begin, out end);
                for (int i = begin; i < end; i++)
                {
                    int sortedIndex = i;
                    int userIndex = sortedToUserIndex[sortedIndex];

                    jobData.Execute(userIndex, sortedTransformAccess[sortedIndex]);
                }
            }
        }

        unsafe static public JobHandle Schedule<T>(this T jobData, TransformAccessArray transforms, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForTransform
        {
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), TransformParallelForLoopStruct<T>.Initialize(), dependsOn, ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelForTransform(ref scheduleParams, transforms.GetTransformAccessArrayForSchedule());
        }

        //@TODO: Run
    }
}
