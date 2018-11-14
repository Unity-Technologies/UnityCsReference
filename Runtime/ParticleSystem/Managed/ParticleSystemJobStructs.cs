// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEngine.Experimental.ParticleSystemJobs
{
    public struct ParticleSystemNativeArray3
    {
        public NativeArray<float> x { get; }
        public NativeArray<float> y { get; }
        public NativeArray<float> z { get; }

        unsafe internal ParticleSystemNativeArray3(ref JobDataInternal.Array3 ptrs, int count, object safetyHandle)
        {
            x = ParticleSystemJobData.CreateNativeArray<float>(ptrs.x, count, safetyHandle);
            y = ParticleSystemJobData.CreateNativeArray<float>(ptrs.y, count, safetyHandle);
            z = ParticleSystemJobData.CreateNativeArray<float>(ptrs.z, count, safetyHandle);
        }
    }

    public struct ParticleSystemNativeArray4
    {
        public NativeArray<float> x { get; }
        public NativeArray<float> y { get; }
        public NativeArray<float> z { get; }
        public NativeArray<float> w { get; }

        unsafe internal ParticleSystemNativeArray4(ref JobDataInternal.Array4 ptrs, int count, object safetyHandle)
        {
            x = ParticleSystemJobData.CreateNativeArray<float>(ptrs.x, count, safetyHandle);
            y = ParticleSystemJobData.CreateNativeArray<float>(ptrs.y, count, safetyHandle);
            z = ParticleSystemJobData.CreateNativeArray<float>(ptrs.z, count, safetyHandle);
            w = ParticleSystemJobData.CreateNativeArray<float>(ptrs.w, count, safetyHandle);
        }
    }

    public struct ParticleSystemJobData
    {
        internal static unsafe NativeArray<T> CreateNativeArray<T>(void* src, int count, object safetyHandle) where T : struct
        {
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(src, count, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, (AtomicSafetyHandle)safetyHandle);
            return arr;
        }

        public int count { get; }
        public ParticleSystemNativeArray3 positions { get; }
        public ParticleSystemNativeArray3 velocities { get; }
        public ParticleSystemNativeArray3 rotations { get; }
        public ParticleSystemNativeArray3 rotationalSpeeds { get; }
        public ParticleSystemNativeArray3 sizes { get; }
        public NativeArray<Color32> startColors { get; }
        public NativeArray<float> aliveTimePercent { get; }
        public NativeArray<float> inverseStartLifetimes { get; }
        public NativeArray<UInt32> randomSeeds { get; }
        public ParticleSystemNativeArray4 customData1 { get; }
        public ParticleSystemNativeArray4 customData2 { get; }

        unsafe internal ParticleSystemJobData(ref JobDataInternal nativeData, out object safetyHandle)
        {
            safetyHandle = AtomicSafetyHandle.Create();

            count = nativeData.count;
            positions = new ParticleSystemNativeArray3(ref nativeData.positions, count, safetyHandle);
            velocities = new ParticleSystemNativeArray3(ref nativeData.velocities, count, safetyHandle);
            rotations = new ParticleSystemNativeArray3(ref nativeData.rotations, count, safetyHandle);
            rotationalSpeeds = new ParticleSystemNativeArray3(ref nativeData.rotationalSpeeds, count, safetyHandle);
            sizes = new ParticleSystemNativeArray3(ref nativeData.sizes, count, safetyHandle);
            startColors = CreateNativeArray<Color32>(nativeData.startColors, count, safetyHandle);
            aliveTimePercent = CreateNativeArray<float>(nativeData.aliveTimePercent, count, safetyHandle);
            inverseStartLifetimes = CreateNativeArray<float>(nativeData.inverseStartLifetimes, count, safetyHandle);
            randomSeeds = CreateNativeArray<UInt32>(nativeData.randomSeeds, count, safetyHandle);
            customData1 = new ParticleSystemNativeArray4(ref nativeData.customData1, count, safetyHandle);
            customData2 = new ParticleSystemNativeArray4(ref nativeData.customData2, count, safetyHandle);
        }
    }

    internal struct JobDataInternal
    {
        internal struct Array3
        {
            internal unsafe float* x;
            internal unsafe float* y;
            internal unsafe float* z;
        }

        internal struct Array4
        {
            internal unsafe float* x;
            internal unsafe float* y;
            internal unsafe float* z;
            internal unsafe float* w;
        }

        internal int count;
        internal Array3 positions;
        internal Array3 velocities;
        internal Array3 rotations;
        internal Array3 rotationalSpeeds;
        internal Array3 sizes;
        internal unsafe Color32* startColors;
        internal unsafe float* aliveTimePercent;
        internal unsafe float* inverseStartLifetimes;
        internal unsafe UInt32* randomSeeds;
        internal Array4 customData1;
        internal Array4 customData2;
    }

    internal struct ProcessParticleSystemJobStruct<T> where T : struct, IParticleSystemJob
    {
        private static IntPtr jobReflectionData;

        public static IntPtr GetJobReflectionData()
        {
            if (jobReflectionData == IntPtr.Zero)
            {
                jobReflectionData = JobsUtility.CreateJobReflectionData(
                    typeof(T),
                    JobType.Single,
                    (ExecuteJobFunction)ExecuteProcessParticleSystem);
            }

            return jobReflectionData;
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr particleSystemPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);

        public static unsafe void ExecuteProcessParticleSystem(ref T data, IntPtr particleSystemPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
        {
            JobDataInternal jobDataInternal;
            UnsafeUtility.CopyPtrToStructure((void*)particleSystemPtr, out jobDataInternal);

            object safetyHandle;
            ParticleSystemJobData jobData = new ParticleSystemJobData(ref jobDataInternal, out safetyHandle);
            data.ProcessParticleSystem(jobData);

            AtomicSafetyHandle.CheckDeallocateAndThrow((AtomicSafetyHandle)safetyHandle);
            AtomicSafetyHandle.Release((AtomicSafetyHandle)safetyHandle);
        }
    }
}

