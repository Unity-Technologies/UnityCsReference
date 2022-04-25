// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.LowLevel
{
    // Profiler marker usage flags.
    // Must be in sync with UnityProfilerMarkerFlags!
    [Flags]
    public enum MarkerFlags : ushort
    {
        Default = 0,

        Script = 1 << 1,
        ScriptInvoke = 1 << 5,
        ScriptDeepProfiler = 1 << 6,

        AvailabilityEditor = 1 << 2,
        AvailabilityNonDevelopment = 1 << 3,

        Warning = 1 << 4,

        Counter = 1 << 7,

        SampleGPU = 1 << 8,
    }

    // Supported profiler metadata types.
    // Must be in sync with UnityProfilerMarkerDataType!
    public enum ProfilerMarkerDataType : byte
    {
        InstanceId = 1,
        Int32 = 2,
        UInt32 = 3,
        Int64 = 4,
        UInt64 = 5,
        Float = 6,
        Double = 7,
        String16 = 9,
        Blob8 = 11,
        GfxResourceId = 12,
    }
}
