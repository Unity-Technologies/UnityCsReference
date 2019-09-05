// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace Unity.Profiling.LowLevel
{
    [Flags]
    public enum MarkerFlags
    {
        Default = 0,

        Script = 1 << 1,
        ScriptInvoke = 1 << 5,
        ScriptDeepProfiler = 1 << 6,

        AvailabilityEditor = 1 << 2,

        Warning = 1 << 4,
    }

    internal enum MarkerEventType : ushort
    {
        Begin = 0,
        End = 1,
    };

    internal enum MarkerEventDataType : byte
    {
        None = 0,
        InstanceId = 1,
        Int32 = 2,
        UInt32 = 3,
        Int64 = 4,
        UInt64 = 5,
        Float = 6,
        Double = 7,
        String = 8,
        String16 = 9,
        Vec3 = 10,
        Blob8 = 11
    };

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal unsafe struct MarkerEventData // Metadata parameter. Must be in sync with UnityProfilerMarkerData
    {
        [FieldOffset(0)] public byte type;
        [FieldOffset(1)] public byte reserved0;
        [FieldOffset(2)] public ushort reserved1;
        [FieldOffset(4)] public uint size;
        [FieldOffset(8)] public void* ptr;
    };
}
