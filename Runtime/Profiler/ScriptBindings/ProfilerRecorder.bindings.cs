// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Profiling.LowLevel.Unsafe
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly unsafe struct ProfilerRecorderDescription
    {
        [FieldOffset(0)] readonly ProfilerCategory category;
        [FieldOffset(2)] readonly MarkerFlags flags;
        [FieldOffset(4)] readonly ProfilerMarkerDataType dataType;
        [FieldOffset(5)] readonly ProfilerMarkerDataUnit unitType;
        [FieldOffset(8)] readonly int reserved0;
        [FieldOffset(12)] readonly int nameUtf8Len;
        [FieldOffset(16)] readonly byte* nameUtf8;

        public ProfilerCategory Category => category;
        public MarkerFlags Flags => flags;
        public ProfilerMarkerDataType DataType => dataType;
        public ProfilerMarkerDataUnit UnitType => unitType;
        public int NameUtf8Len => nameUtf8Len;
        public byte* NameUtf8 => nameUtf8;
        public string Name => ProfilerUnsafeUtility.Utf8ToString(nameUtf8, nameUtf8Len);
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct ProfilerRecorderHandle
    {
        const ulong k_InvalidHandle = ~0x0ul;

        [FieldOffset(0)]
        internal readonly ulong handle;

        internal ProfilerRecorderHandle(ulong handle)
        {
            this.handle = handle;
        }

        public bool Valid => handle != 0 && handle != k_InvalidHandle;

        internal static ProfilerRecorderHandle Get(ProfilerMarker marker)
        {
            return new ProfilerRecorderHandle((ulong)marker.Handle.ToInt64());
        }

        internal static unsafe ProfilerRecorderHandle Get(ProfilerCategory category, string statName)
        {
            if (string.IsNullOrEmpty(statName))
                throw new ArgumentException("String must be not null or empty", nameof(statName));

            fixed(char* c = statName)
            {
                return GetByName(category, c, statName.Length);
            }
        }

        public static ProfilerRecorderDescription GetDescription(ProfilerRecorderHandle handle)
        {
            if (!handle.Valid)
                throw new ArgumentException("ProfilerRecorderHandle is not initialized or is not available", nameof(handle));

            return GetDescriptionInternal(handle);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public static extern void GetAvailable(List<ProfilerRecorderHandle> outRecorderHandleList);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern unsafe ProfilerRecorderHandle GetByName(ProfilerCategory category, char* name, int nameLen);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerRecorderDescription GetDescriptionInternal(ProfilerRecorderHandle handle);
    }
}

namespace Unity.Profiling
{
    [Flags]
    public enum ProfilerRecorderOptions
    {
        None = 0,
        StartImmediately = 1 << 0,
        KeepAliveDuringDomainReload = 1 << 1,
        CollectOnlyOnCurrentThread = 1 << 2,
        WrapAroundWhenCapacityReached = 1 << 3,
        SumAllSamplesInFrame = 1 << 4,

        Default = WrapAroundWhenCapacityReached | SumAllSamplesInFrame
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Value = {Value}; Count = {Count}")]
    public struct ProfilerRecorderSample
    {
        long value;
        long count;
        long refValue;

        public long Value => value;
        public long Count => count;
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerRecorder.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    [DebuggerTypeProxy(typeof(ProfilerRecorderDebugView))]
    public struct ProfilerRecorder : IDisposable
    {
        internal ulong handle;

        internal enum ControlOptions
        {
            Start = 0,
            Stop = 1,
            Reset = 2,
            Release = 4,
        }

        internal enum CountOptions
        {
            Count = 0,
            MaxCount = 1,
        }

        internal ProfilerRecorder(ProfilerRecorderOptions options)
        {
            this = Create(new LowLevel.Unsafe.ProfilerRecorderHandle(), 0, options);
        }

        public ProfilerRecorder(string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
            : this(ProfilerCategory.Any, statName, capacity, options)
        {
        }

        public ProfilerRecorder(string categoryName, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
            : this(new ProfilerCategory(categoryName), statName, capacity, options)
        {
        }

        public unsafe ProfilerRecorder(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            LowLevel.Unsafe.ProfilerRecorderHandle statHandle;
            fixed(char* c = statName)
            {
                statHandle = LowLevel.Unsafe.ProfilerRecorderHandle.GetByName(category, c, statName.Length);
            }
            this = Create(statHandle, capacity, options);
        }

        public unsafe ProfilerRecorder(ProfilerCategory category, char* statName, int statNameLen, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            var statHandle = LowLevel.Unsafe.ProfilerRecorderHandle.GetByName(category, statName, statNameLen);
            this = Create(statHandle, capacity, options);
        }

        public ProfilerRecorder(ProfilerMarker marker, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            this = Create(LowLevel.Unsafe.ProfilerRecorderHandle.Get(marker), capacity, options);
        }

        public ProfilerRecorder(LowLevel.Unsafe.ProfilerRecorderHandle statHandle, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            this = Create(statHandle, capacity, options);
        }

        public static unsafe ProfilerRecorder StartNew(ProfilerCategory category, string statName, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            fixed(char* c = statName)
            {
                return new ProfilerRecorder(category, c, statName.Length, capacity, options | ProfilerRecorderOptions.StartImmediately);
            }
        }

        public static ProfilerRecorder StartNew(ProfilerMarker marker, int capacity = 1, ProfilerRecorderOptions options = ProfilerRecorderOptions.Default)
        {
            return new ProfilerRecorder(marker, capacity, options | ProfilerRecorderOptions.StartImmediately);
        }

        internal static ProfilerRecorder StartNew()
        {
            return Create(new LowLevel.Unsafe.ProfilerRecorderHandle(), 0, ProfilerRecorderOptions.StartImmediately);
        }

        public bool Valid => handle != 0 && GetValid(this);

        public ProfilerMarkerDataType DataType
        {
            get
            {
                CheckInitializedAndThrow();
                return GetValueDataType(this);
            }
        }

        public ProfilerMarkerDataUnit UnitType
        {
            get
            {
                CheckInitializedAndThrow();
                return GetValueUnitType(this);
            }
        }

        public void Start()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Start);
        }

        public void Stop()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Stop);
        }

        public void Reset()
        {
            CheckInitializedAndThrow();
            Control(this, ControlOptions.Reset);
        }

        public long CurrentValue
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCurrentValue(this);
            }
        }

        public double CurrentValueAsDouble
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCurrentValueAsDouble(this);
            }
        }

        public long LastValue
        {
            get
            {
                CheckInitializedAndThrow();
                return GetLastValue(this);
            }
        }

        public double LastValueAsDouble
        {
            get
            {
                CheckInitializedAndThrow();
                return GetLastValueAsDouble(this);
            }
        }

        public int Capacity
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCount(this, CountOptions.MaxCount);
            }
        }

        public int Count
        {
            get
            {
                CheckInitializedAndThrow();
                return GetCount(this, CountOptions.Count);
            }
        }

        public bool IsRunning
        {
            get
            {
                CheckInitializedAndThrow();
                return GetRunning(this);
            }
        }

        public bool WrappedAround
        {
            get
            {
                CheckInitializedAndThrow();
                return GetWrapped(this);
            }
        }

        public ProfilerRecorderSample GetSample(int index)
        {
            CheckInitializedAndThrow();
            return GetSampleInternal(this, index);
        }

        public void CopyTo(List<ProfilerRecorderSample> outSamples, bool reset = false)
        {
            if (outSamples == null)
                throw new ArgumentNullException(nameof(outSamples));
            CheckInitializedAndThrow();
            CopyTo_List(this, outSamples, reset);
        }

        public unsafe int CopyTo(ProfilerRecorderSample* dest, int destSize, bool reset = false)
        {
            CheckInitializedWithParamsAndThrow(dest);
            return CopyTo_Pointer(this, dest, destSize, reset);
        }

        public unsafe ProfilerRecorderSample[] ToArray()
        {
            CheckInitializedAndThrow();

            var count = Count;
            var array = new ProfilerRecorderSample[count];
            fixed(ProfilerRecorderSample* p = array)
            {
                _ = CopyTo_Pointer(this, p, count, false);
            }

            return array;
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern ProfilerRecorder Create(LowLevel.Unsafe.ProfilerRecorderHandle statHandle, int maxSampleCount, ProfilerRecorderOptions options);

        [NativeMethod(IsThreadSafe = true)]
        static extern void Control(ProfilerRecorder handle, ControlOptions options);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerMarkerDataUnit GetValueUnitType(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern ProfilerMarkerDataType GetValueDataType(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern long GetCurrentValue(ProfilerRecorder handle);
        [NativeMethod(IsThreadSafe = true)]
        static extern double GetCurrentValueAsDouble(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern long GetLastValue(ProfilerRecorder handle);
        [NativeMethod(IsThreadSafe = true)]
        static extern double GetLastValueAsDouble(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetCount(ProfilerRecorder handle, CountOptions countOptions);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetValid(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetWrapped(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetRunning(ProfilerRecorder handle);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        static extern ProfilerRecorderSample GetSampleInternal(ProfilerRecorder handle, int index);

        [NativeMethod(IsThreadSafe = true)]
        static extern void CopyTo_List(ProfilerRecorder handle, List<ProfilerRecorderSample> outSamples, bool reset);

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe int CopyTo_Pointer(ProfilerRecorder handle, ProfilerRecorderSample* outSamples, int outSamplesSize, bool reset);

        public void Dispose()
        {
            if (handle == 0)
                return;

            Control(this, ControlOptions.Release);
            handle = 0;
        }

        [BurstDiscard]
        unsafe void CheckInitializedWithParamsAndThrow(ProfilerRecorderSample* dest)
        {
            if (handle == 0)
                throw new InvalidOperationException("ProfilerRecorder object is not initialized or has been disposed.");
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
        }

        [BurstDiscard]
        void CheckInitializedAndThrow()
        {
            if (handle == 0)
                throw new InvalidOperationException("ProfilerRecorder object is not initialized or has been disposed.");
        }
    }

    sealed class ProfilerRecorderDebugView
    {
        ProfilerRecorder m_Recorder;

        public ProfilerRecorderDebugView(ProfilerRecorder r)
        {
            m_Recorder = r;
        }

        public ProfilerRecorderSample[] Items => m_Recorder.ToArray();
    }
}
