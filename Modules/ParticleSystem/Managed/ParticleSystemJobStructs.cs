// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using static Unity.Collections.LowLevel.Unsafe.BurstLike;

namespace UnityEngine.ParticleSystemJobs
{
    public struct ParticleSystemNativeArray3
    {
        public NativeArray<float> x;
        public NativeArray<float> y;
        public NativeArray<float> z;

        public Vector3 this[int index]
        {
            get
            {
                return new Vector3(x[index], y[index], z[index]);
            }

            set
            {
                x[index] = value.x;
                y[index] = value.y;
                z[index] = value.z;
            }
        }
    }

    public struct ParticleSystemNativeArray4
    {
        public NativeArray<float> x;
        public NativeArray<float> y;
        public NativeArray<float> z;
        public NativeArray<float> w;

        public Vector4 this[int index]
        {
            get
            {
                return new Vector4(x[index], y[index], z[index], w[index]);
            }

            set
            {
                x[index] = value.x;
                y[index] = value.y;
                z[index] = value.z;
                w[index] = value.w;
            }
        }
    }

    public struct ParticleSystemJobData
    {
        public int count { get; }
        public ParticleSystemNativeArray3 positions { get; }
        public ParticleSystemNativeArray3 velocities { get; }
        public ParticleSystemNativeArray3 axisOfRotations { get; }
        public ParticleSystemNativeArray3 rotations { get; }
        public ParticleSystemNativeArray3 rotationalSpeeds { get; }
        public ParticleSystemNativeArray3 sizes { get; }
        public NativeArray<Color32> startColors { get; }
        public NativeArray<float> aliveTimePercent { get; }
        public NativeArray<float> inverseStartLifetimes { get; }
        public NativeArray<UInt32> randomSeeds { get; }
        public ParticleSystemNativeArray4 customData1 { get; }
        public ParticleSystemNativeArray4 customData2 { get; }
        public NativeArray<int> meshIndices { get; }

        internal AtomicSafetyHandle m_Safety;

        unsafe internal ParticleSystemJobData(ref NativeParticleData nativeData) : this()
        {
            m_Safety = AtomicSafetyHandle.Create();

            count = nativeData.count;

            positions = CreateNativeArray3(ref nativeData.positions, count);
            velocities = CreateNativeArray3(ref nativeData.velocities, count);
            axisOfRotations = CreateNativeArray3(ref nativeData.axisOfRotations, count);
            rotations = CreateNativeArray3(ref nativeData.rotations, count);
            rotationalSpeeds = CreateNativeArray3(ref nativeData.rotationalSpeeds, count);
            sizes = CreateNativeArray3(ref nativeData.sizes, count);
            startColors = CreateNativeArray<Color32>(nativeData.startColors, count);
            aliveTimePercent = CreateNativeArray<float>(nativeData.aliveTimePercent, count);
            inverseStartLifetimes = CreateNativeArray<float>(nativeData.inverseStartLifetimes, count);
            randomSeeds = CreateNativeArray<UInt32>(nativeData.randomSeeds, count);
            customData1 = CreateNativeArray4(ref nativeData.customData1, count);
            customData2 = CreateNativeArray4(ref nativeData.customData2, count);
            meshIndices = CreateNativeArray<int>(nativeData.meshIndices, count);
        }

        unsafe internal NativeArray<T> CreateNativeArray<T>(void* src, int count) where T : struct
        {
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(src, count, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, m_Safety);

            return arr;
        }

        unsafe internal ParticleSystemNativeArray3 CreateNativeArray3(ref NativeParticleData.Array3 ptrs, int count)
        {
            return new ParticleSystemNativeArray3
            {
                x = CreateNativeArray<float>(ptrs.x, count),
                y = CreateNativeArray<float>(ptrs.y, count),
                z = CreateNativeArray<float>(ptrs.z, count)
            };
        }

        unsafe internal ParticleSystemNativeArray4 CreateNativeArray4(ref NativeParticleData.Array4 ptrs, int count)
        {
            return new ParticleSystemNativeArray4
            {
                x = CreateNativeArray<float>(ptrs.x, count),
                y = CreateNativeArray<float>(ptrs.y, count),
                z = CreateNativeArray<float>(ptrs.z, count),
                w = CreateNativeArray<float>(ptrs.w, count)
            };
        }
    }

    unsafe internal struct NativeParticleData
    {
        internal struct Array3
        {
            internal float* x;
            internal float* y;
            internal float* z;
        }

        internal struct Array4
        {
            internal float* x;
            internal float* y;
            internal float* z;
            internal float* w;
        }

        internal int count;
        internal Array3 positions;
        internal Array3 velocities;
        internal Array3 axisOfRotations;
        internal Array3 rotations;
        internal Array3 rotationalSpeeds;
        internal Array3 sizes;
        internal void* startColors;
        internal void* aliveTimePercent;
        internal void* inverseStartLifetimes;
        internal void* randomSeeds;
        internal Array4 customData1;
        internal Array4 customData2;
        internal void* meshIndices;
    }

    unsafe struct NativeListData
    {
        public void* system;
        public int length;
        public int capacity;
    }

    internal struct ParticleSystemJobStruct<T> where T : struct, IJobParticleSystem
    {
        public static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<ParticleSystemJobStruct<T>>();

        [BurstDiscard]
        public static void Initialize()
        {
            if (jobReflectionData.Data == IntPtr.Zero)
                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex);
        public static unsafe void Execute(ref T data, IntPtr listDataPtr, IntPtr unusedPtr, ref JobRanges ranges, int jobIndex)
        {
            NativeParticleData particleData;
            var listData = (NativeListData*)listDataPtr;
            ParticleSystem.CopyManagedJobData(listData->system, out particleData);

            ParticleSystemJobData jobData = new ParticleSystemJobData(ref particleData);
            data.Execute(jobData);

            AtomicSafetyHandle.CheckDeallocateAndThrow(jobData.m_Safety);
            AtomicSafetyHandle.Release(jobData.m_Safety);
        }
    }

    internal struct ParticleSystemParallelForJobStruct<T> where T : struct, IJobParticleSystemParallelFor
    {
        public static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<ParticleSystemParallelForJobStruct<T>>();

        [BurstDiscard]
        public static void Initialize()
        {
            if (jobReflectionData.Data == IntPtr.Zero)
                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr listDataPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        public static unsafe void Execute(ref T data, IntPtr listDataPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            NativeParticleData particleData;
            var listData = (NativeListData*)listDataPtr;
            ParticleSystem.CopyManagedJobData(listData->system, out particleData);

            ParticleSystemJobData jobData = new ParticleSystemJobData(ref particleData);

            while (true)
            {
                int begin;
                int end;
                if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    break;

                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref data), begin, end - begin);

                for (var i = begin; i < end; ++i)
                    data.Execute(jobData, i);
            }

            AtomicSafetyHandle.CheckDeallocateAndThrow(jobData.m_Safety);
            AtomicSafetyHandle.Release(jobData.m_Safety);
        }
    }

    internal struct ParticleSystemParallelForBatchJobStruct<T> where T : struct, IJobParticleSystemParallelForBatch
    {
        public static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<ParticleSystemParallelForBatchJobStruct<T>>();

        [BurstDiscard]
        public static void Initialize()
        {
            if (jobReflectionData.Data == IntPtr.Zero)
                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction)Execute);
        }

        public delegate void ExecuteJobFunction(ref T data, IntPtr listDataPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
        public static unsafe void Execute(ref T data, IntPtr listDataPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
        {
            NativeParticleData particleData;
            var listData = (NativeListData*)listDataPtr;
            ParticleSystem.CopyManagedJobData(listData->system, out particleData);

            ParticleSystemJobData jobData = new ParticleSystemJobData(ref particleData);

            while (true)
            {
                int begin;
                int end;
                if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    break;

                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref data), begin, end - begin);

                data.Execute(jobData, begin, end - begin);
            }

            AtomicSafetyHandle.CheckDeallocateAndThrow(jobData.m_Safety);
            AtomicSafetyHandle.Release(jobData.m_Safety);
        }
    }
}

