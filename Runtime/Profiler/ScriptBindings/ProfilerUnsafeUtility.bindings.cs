// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CallConvCdecl = System.Runtime.CompilerServices.CallConvCdecl;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

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
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly unsafe struct ProfilerCategoryDescription
    {
        [FieldOffset(0)]  public readonly ushort Id;
        [FieldOffset(2)]  public readonly ushort Flags;
        [FieldOffset(4)]  public readonly Color32 Color;
        [FieldOffset(8)]  readonly int reserved0;
        [FieldOffset(12)] public readonly int NameUtf8Len;
        [FieldOffset(16)] public readonly byte* NameUtf8;

        public string Name => ProfilerUnsafeUtility.Utf8ToString(NameUtf8, NameUtf8Len);
    }

    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerUnsafeUtility.bindings.h")]
    [UsedByNativeCode]
    [IgnoredByDeepProfiler]
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
        public const ushort CategoryLighting = 13;
        [Obsolete("CategoryLightning has been renamed. Use CategoryLighting instead (UnityUpgradable) -> CategoryLighting", false)]
        public const ushort CategoryLightning = 13;
        public const ushort CategoryNetwork = 14;
        public const ushort CategoryLoading = 15;
        public const ushort CategoryOther = 16;
        public const ushort CategoryVr = 22;
        public const ushort CategoryAllocation = 23;
        public const ushort CategoryInternal = 24;
        public const ushort CategoryFileIO = 25;
        public const ushort CategoryInput = 30;
        public const ushort CategoryVirtualTexturing = 31;
        internal const ushort CategoryGPU = 32;
        public const ushort CategoryPhysics2D = 33;
        public const ushort CategoryU2D = 39;
        public const ushort CategoryUIToolkit = 40;
        public const ushort CategoryPhysicsCore2D = 42;
        internal const ushort CategoryAny = 0xFFFF;

        [NativeMethod(IsThreadSafe = true)]
        internal static extern ushort CreateCategory(string name, ProfilerCategoryColor colorIndex);

        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe ushort CreateCategory__Unmanaged(byte* name, int nameLen, ProfilerCategoryColor colorIndex);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe ushort CreateCategory(char* name, int nameLen, ProfilerCategoryColor colorIndex)
        {
            return CreateCategory_Unsafe(name, nameLen, colorIndex);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe ushort CreateCategory_Unsafe(char* name, int nameLen, ProfilerCategoryColor colorIndex);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe ushort GetCategoryByName(char* name, int nameLen)
        {
            return GetCategoryByName_Unsafe(name, nameLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe ushort GetCategoryByName_Unsafe(char* name, int nameLen);

        [NativeMethod(IsThreadSafe = true)]
        public static extern ProfilerCategoryDescription GetCategoryDescription(ushort categoryId);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern Color32 GetCategoryColor(ProfilerCategoryColor colorIndex);

        [NativeMethod(IsThreadSafe = true)]
        public static extern IntPtr CreateMarker(string name, ushort categoryId, MarkerFlags flags, int metadataCount);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe IntPtr CreateMarker__Unmanaged(byte* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe IntPtr CreateMarker(char* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount)
        {
            return CreateMarker_Unsafe(name, nameLen, categoryId, flags, metadataCount);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe IntPtr CreateMarker_Unsafe(char* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern IntPtr GetMarker(string name);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetMarkerMetadata(IntPtr markerPtr, int index, string name, byte type, byte unit);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe void SetMarkerMetadata__Unmanaged(IntPtr markerPtr, int index, byte* name, int nameLen, byte type, byte unit);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe void SetMarkerMetadata(IntPtr markerPtr, int index, char* name, int nameLen, byte type, byte unit)
        {
            SetMarkerMetadata_Unsafe(markerPtr, index, name, nameLen, type, unit);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void SetMarkerMetadata_Unsafe(IntPtr markerPtr, int index, char* name, int nameLen, byte type, byte unit);

        // Mirrors the leading fields of the native profiling::Marker.
        [StructLayout(LayoutKind.Sequential)]
        internal struct NativeMarker
        {
            public IntPtr Callback;
            public uint Id;
            public ushort Flags;
            public ushort Category;
            public IntPtr Name;
            public IntPtr MetadataDesc;
            public IntPtr NextStaticMarkerOrCounterValue;
            public IntPtr GpuCallback;
        }

        // Mirrors native profiling::Profiler::ScriptingProfilerState.
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProfilerState
        {
            public int Active;
            public IntPtr BeginSample;
            public IntPtr BeginSampleWithMetadata;
            public IntPtr EndSample;
        }

        static class NativeProfilerState
        {
            internal static readonly IntPtr s_Value = GetProfilerState();
        }

        [NativeMethod(IsThreadSafe = true)]
        [BurstAuthorizedExternalMethod]
        static extern IntPtr GetProfilerState();

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        static unsafe bool IsListening(IntPtr markerPtr, in ProfilerState state)
        {
            var marker = (NativeMarker*)markerPtr;
            return state.Active != 0 || marker == null || marker->Callback != IntPtr.Zero || marker->GpuCallback != IntPtr.Zero;
        }

        // 256 : Aggressive inlining, 512 : Aggressive optimization
        [MethodImpl(256 | 512)]
        public static unsafe void BeginSample(IntPtr markerPtr)
        {
            ref var state = ref *(ProfilerState*)NativeProfilerState.s_Value;
            if (IsListening(markerPtr, state))
                InvokeSample(state.BeginSample, markerPtr);
        }

        // 256 : Aggressive inlining, 512 : Aggressive optimization
        [MethodImpl(256 | 512)]
        public static unsafe void BeginSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata)
        {
            ref var state = ref *(ProfilerState*)NativeProfilerState.s_Value;
            if (IsListening(markerPtr, state))
                InvokeSampleWithMetadata(state.BeginSampleWithMetadata, markerPtr, metadataCount, metadata);
        }

        // 256 : Aggressive inlining, 512 : Aggressive optimization
        [MethodImpl(256 | 512)]
        public static unsafe void EndSample(IntPtr markerPtr)
        {
            ref var state = ref *(ProfilerState*)NativeProfilerState.s_Value;
            if (IsListening(markerPtr, state))
                InvokeSample(state.EndSample, markerPtr);
        }

        // Kept out of line so callers don't pay the native call frame setup when nothing is listening.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe void InvokeSample(IntPtr function, IntPtr markerPtr)
        {
            ((delegate* unmanaged[Cdecl]<IntPtr, void>)function)(markerPtr);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe void InvokeSampleWithMetadata(IntPtr function, IntPtr markerPtr, int metadataCount, void* metadata)
        {
            ((delegate* unmanaged[Cdecl]<IntPtr, int, void*, void>)function)(markerPtr, metadataCount, metadata);
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void SingleSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void* CreateCounterValue(out IntPtr counterPtr, string name, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);
        // Burst shadow
        [NativeMethod(IsThreadSafe = true)]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe void* CreateCounterValue__Unmanaged(out IntPtr counterPtr, byte* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public static unsafe void* CreateCounterValue(out IntPtr counterPtr, char* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions)
        {
            return CreateCounterValue_Unsafe(out counterPtr, name, nameLen, categoryId, flags, dataType, dataUnit, dataSize, counterOptions);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void* CreateCounterValue_Unsafe(out IntPtr counterPtr, char* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        [NativeMethod(IsThreadSafe = true)]
        public static extern unsafe void FlushCounterValue(void* counterValuePtr);

        internal static unsafe string Utf8ToString(byte* chars, int charsLen)
        {
            if (chars == null)
                return null;

            var arr = new byte[charsLen];
            Marshal.Copy((IntPtr)chars, arr, 0, charsLen);
            return Encoding.UTF8.GetString(arr, 0, charsLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern uint CreateFlow(ushort categoryId);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void FlowEvent(uint flowId, ProfilerFlowEventType flowEventType);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void Internal_BeginWithObject(IntPtr markerPtr, UnityEngine.Object contextUnityObject);

        [NativeConditional("ENABLE_PROFILER")]
        internal static extern string Internal_GetName(IntPtr markerPtr);

        public static extern long Timestamp
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public struct TimestampConversionRatio
        {
            public long Numerator;
            public long Denominator;
        }

        public static extern TimestampConversionRatio TimestampToNanosecondsConversionRatio
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern IntPtr GetOrCreateMemLabel(string areaName, string objectName);

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        // This will only be referenced from Burst-generated code, in place of the version without the
        // __Unmanaged suffix. So we need to make sure it will not get stripped.
        [RequiredMember]
        internal static extern unsafe IntPtr GetOrCreateMemLabel__Unmanaged(byte* areaName, int areaNameLen, byte* objectName, int objectNameLen);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern long GetMemLabelRelatedMemorySize(IntPtr label);

    }
}
