// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.Animations
{
    internal enum JobMethodIndex
    {
        ProcessRootMotionMethodIndex = 0,
        ProcessAnimationMethodIndex,
        MethodIndexCount
    }


    internal struct ProcessAnimationJobStruct<T>
        where T : struct, IAnimationJob
    {
        private static IntPtr jobReflectionData;

        public static IntPtr GetJobReflectionData()
        {
            if (jobReflectionData == IntPtr.Zero)
            {
                jobReflectionData = JobsUtility.CreateJobReflectionData(
                    typeof(T),
                    JobType.Single,
                    (ExecuteJobFunction)Execute);
            }

            return jobReflectionData;
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr animationStreamPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);


        public static unsafe void Execute(ref T data, IntPtr animationStreamPtr, IntPtr methodIndex, ref JobRanges ranges, int jobIndex)
        {
            AnimationStream animationStream;
            UnsafeUtility.CopyPtrToStructure((void*)animationStreamPtr, out animationStream);

            JobMethodIndex jobMethodIndex = (JobMethodIndex)methodIndex.ToInt32();
            switch (jobMethodIndex)
            {
                case JobMethodIndex.ProcessRootMotionMethodIndex:
                    data.ProcessRootMotion(animationStream);
                    break;
                case JobMethodIndex.ProcessAnimationMethodIndex:
                    data.ProcessAnimation(animationStream);
                    break;
                default:
                    throw new NotImplementedException("Invalid Animation jobs method index.");
            }
        }
    }
}

