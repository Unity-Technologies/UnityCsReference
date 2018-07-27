// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.Experimental.Animations
{
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
                    (ExecuteJobFunction)ExecuteProcessRootMotion,
                    (ExecuteJobFunction)ExecuteProcessAnimation);
            }

            return jobReflectionData;
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr animationStreamPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);

        public static unsafe void ExecuteProcessAnimation(ref T data, IntPtr animationStreamPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
        {
            AnimationStream animationStream;
            UnsafeUtility.CopyPtrToStructure((void*)animationStreamPtr, out animationStream);
            data.ProcessAnimation(animationStream);
        }

        public static unsafe void ExecuteProcessRootMotion(ref T data, IntPtr animationStreamPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
        {
            AnimationStream animationStream;
            UnsafeUtility.CopyPtrToStructure((void*)animationStreamPtr, out animationStream);
            data.ProcessRootMotion(animationStream);
        }
    }
}

