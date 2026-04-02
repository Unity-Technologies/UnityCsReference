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

        // Bits 10-12 for verbosity levels. Allows to filter markers during visualization.
        // Set only ONE of these levels for a given marker or their bits will conflict.
        VerbosityDebug =    1 << 10,    // Internal debug markers - e.g. JobSystem Idle.
        VerbosityInternal = 2 << 10,    // Internal markers - e.g. Mutex/semaphore waits.
        VerbosityExternal = 3 << 10,    // Marker should be echoed to any attached external profilers (such as Superluminal) even if they normally wouldn't capture it. This is usually applied to markers which have additional context that might be useful in those tools
        VerbosityAdvanced = 4 << 10,    // Markers which are useful for advanced users - e.g. Loading.

        // Marker which was created with a recorder to make an early binding.
        // If we create marker with the same name later, we just pick and initialized this one.
        // In this way we can setup callback pointers for dynamic markers.
        // Precreated = 1 << 15
    }

    // Supported profiler metadata types.
    // Must be in sync with UnityProfilerMarkerDataType!
    public enum ProfilerMarkerDataType : byte
    {
        [Obsolete("Use EntityId, this will be removed in future versions (UnityUpgradable) -> EntityId")]
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
        EntityId = 13,
    }
}
