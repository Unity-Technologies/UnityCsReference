// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.LowLevel
{
    // Profiler marker usage flags.
    // Must be in sync with profiling::Marker::Flags!
    [Flags]
    public enum MarkerFlags : ushort
    {
        Default = 0,

        Script = 1 << 1,
        ScriptInvoke = 1 << 5,
        ScriptDeepProfiler = 1 << 6,

        AvailabilityEditor = 1 << 2,

        Warning = 1 << 4,
    }

    // Supported profiler metadata types.
    // Must be in sync with profiling::Marker::Metadata::Type!
    public enum ProfilerMarkerDataType : byte
    {
        Int32 = 2,
        UInt32 = 3,
        Int64 = 4,
        UInt64 = 5,
        Float = 6,
        Double = 7,
        String16 = 9,
    };
}
