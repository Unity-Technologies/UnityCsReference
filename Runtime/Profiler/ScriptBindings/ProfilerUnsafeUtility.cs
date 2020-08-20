// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text;
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
    };

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public readonly unsafe struct ProfilerCategoryDescription
    {
        [FieldOffset(0)]  public readonly ushort Id;
        [FieldOffset(4)]  public readonly Color32 Color;
        [FieldOffset(8)]  readonly int reserved0;
        [FieldOffset(12)] public readonly int NameUtf8Len;
        [FieldOffset(16)] public readonly byte* NameUtf8;

        public string Name => ProfilerUnsafeUtility.Utf8ToString(NameUtf8, NameUtf8Len);
    }

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
        public const ushort CategoryLighting = 13;
        [Obsolete("CategoryLightning has been renamed. Use CategoryLighting instead (UnityUpgradable) -> CategoryLighting", false)]
        public const ushort CategoryLightning = 13;
        public const ushort CategoryNetwork = 14;
        public const ushort CategoryLoading = 15;
        public const ushort CategoryOther = 16;
        public const ushort CategoryVr = 22;
        public const ushort CategoryAllocation = 23;
        public const ushort CategoryInternal = 24;
        public const ushort CategoryInput = 30;
        public const ushort CategoryVirtualTexturing = 31;
        internal const ushort CategoryAny = 0xFFFF;

        [ThreadSafe]
        public static extern unsafe ushort GetCategoryByName(char* name, int nameLen);

        [ThreadSafe]
        public static extern ProfilerCategoryDescription GetCategoryDescription(ushort categoryId);

        public static unsafe IntPtr CreateMarker(string name, ushort categoryId, MarkerFlags flags, int metadataCount)
        {
            fixed(char* c = name)
            {
                return CreateMarker(c, name.Length, categoryId, flags, metadataCount);
            }
        }

        [ThreadSafe]
        public static extern unsafe IntPtr CreateMarker(char* name, int nameLen, ushort categoryId, MarkerFlags flags, int metadataCount);

        public static unsafe void SetMarkerMetadata(IntPtr markerPtr, int index, string name, byte type, byte unit)
        {
            if (String.IsNullOrEmpty(name))
            {
                SetMarkerMetadata(markerPtr, index, null, 0, type, unit);
            }
            else
            {
                fixed(char* c = name)
                {
                    SetMarkerMetadata(markerPtr, index, c, name.Length, type, unit);
                }
            }
        }

        [ThreadSafe]
        public static extern unsafe void SetMarkerMetadata(IntPtr markerPtr, int index, char* name, int nameLen, byte type, byte unit);

        [ThreadSafe]
        public static extern void BeginSample(IntPtr markerPtr);

        [ThreadSafe]
        public static extern unsafe void BeginSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        [ThreadSafe]
        public static extern void EndSample(IntPtr markerPtr);

        [ThreadSafe]
        public static extern unsafe void SingleSampleWithMetadata(IntPtr markerPtr, int metadataCount, void* metadata);

        public static unsafe void* CreateCounterValue(out IntPtr counterPtr, string name, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions)
        {
            fixed(char* c = name)
            {
                return CreateCounterValue(out counterPtr, c, name.Length, categoryId, flags, dataType, dataUnit, dataSize, counterOptions);
            }
        }

        [ThreadSafe]
        public static extern unsafe void* CreateCounterValue(out IntPtr counterPtr, char* name, int nameLen, ushort categoryId, MarkerFlags flags, byte dataType, byte dataUnit, int dataSize, ProfilerCounterOptions counterOptions);

        [ThreadSafe]
        public static extern unsafe void FlushCounterValue(void* counterValuePtr);

        internal static unsafe string Utf8ToString(byte* chars, int charsLen)
        {
            if (chars == null)
                return null;

            var arr = new byte[charsLen];
            Marshal.Copy((IntPtr)chars, arr, 0, charsLen);
            return Encoding.UTF8.GetString(arr, 0, charsLen);
        }

        [ThreadSafe]
        public static extern uint CreateFlow(ushort categoryId);

        [ThreadSafe]
        public static extern void FlowEvent(uint flowId, ProfilerFlowEventType flowEventType);

        [ThreadSafe]
        internal static extern void Internal_BeginWithObject(IntPtr markerPtr, UnityEngine.Object contextUnityObject);
        [NativeConditional("ENABLE_PROFILER")]
        internal static extern string Internal_GetName(IntPtr markerPtr);

        public static extern long Timestamp
        {
            [ThreadSafe]
            get;
        }

        public struct TimestampConversionRatio
        {
            public long Numerator;
            public long Denominator;
        }

        public static extern TimestampConversionRatio TimestampToNanosecondsConversionRatio
        {
            [ThreadSafe]
            get;
        }
    }
}
