// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Profiling.LowLevel.Unsafe
{
    // Metadata parameter.
    // Must be in sync with UnityProfilerMarkerData!
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct ProfilerMarkerData
    {
        [FieldOffset(0)] public byte Type;
        [FieldOffset(1)] readonly byte reserved0;
        [FieldOffset(2)] readonly ushort reserved1;
        [FieldOffset(4)] public uint Size;
        [FieldOffset(8)] public void* Ptr;
    };

    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.h")]
    [UsedByNativeCode]
    public static class ProfilerUnsafeUtility
    {
        // Built-in profiler categories.
        // Must be in sync with profiling::BuiltinCategory!
        public const ushort CategoryRender = 0;
        public const ushort CategoryScripts = 1;
        public const ushort CategoryGUI = 4;
        public const ushort CategoryPhysics = 5;
        public const ushort CategoryAnimation = 6;
        public const ushort CategoryAi = 7;
        public const ushort CategoryAudio = 8;
        public const ushort CategoryVideo = 11;
        public const ushort CategoryParticles = 12;
        public const ushort CategoryLightning = 13;
        public const ushort CategoryNetwork = 14;
        public const ushort CategoryLoading = 15;
        public const ushort CategoryOther = 16;
        public const ushort CategoryVr = 22;
        public const ushort CategoryAllocation = 23;
        public const ushort CategoryInput = 30;

        [ThreadSafe]
        public static extern IntPtr CreateMarker(string name, ushort categoryId, MarkerFlags flags, int metadataCount);

        [ThreadSafe]
        public static extern void SetMarkerMetadata(IntPtr markerPtr, int index, string name, byte type, byte unit);

        [ThreadSafe]
        public static extern void BeginSample(IntPtr markerPtr);

        [ThreadSafe]
        public static extern unsafe void BeginSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [ThreadSafe]
        public static extern void EndSample(IntPtr markerPtr);


        [ThreadSafe]
        public static extern unsafe void SingleSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [ThreadSafe]
        public static extern unsafe void* CreateCounterValue(out IntPtr counterPtr, string name, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        [ThreadSafe]
        public static extern unsafe void FlushCounterValue(void* counterValuePtr);

        [ThreadSafe]
        internal static extern void Internal_BeginWithObject(IntPtr markerPtr, UnityEngine.Object contextUnityObject);
        [NativeConditional("ENABLE_PROFILER")]
        internal static extern string Internal_GetName(IntPtr markerPtr);
    }
}
