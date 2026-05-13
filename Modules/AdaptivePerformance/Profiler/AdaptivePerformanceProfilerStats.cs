// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.AdaptivePerformance
{
    /// <summary>
    /// Profiler Stats reporting helper class. Stores all adaptive performance markers and helper functions.
    /// </summary>
    public static class AdaptivePerformanceProfilerStats
    {
        /// <summary>
        /// Profiler Category is set to scripts for Adaptive Performance.
        /// </summary>
        public static readonly ProfilerCategory AdaptivePerformanceProfilerCategory = ProfilerCategory.Scripts;
        /// <summary>
        /// Custom Profiler Marker that allows you to sample different data from Unity Profiler.
        /// </summary>
        public readonly struct CustomProfilerMarker<T> where T : unmanaged
        {
            readonly ProfilerMarker m_Marker;
            readonly byte m_Type;

            /// <summary>
            /// Construct the marker with the name of the marker and the dataUnit the marker is collected on.
            /// </summary>
            public CustomProfilerMarker(string name, ProfilerMarkerDataUnit dataUnit)
            {
                m_Marker = new ProfilerMarker(AdaptivePerformanceProfilerCategory, name, MarkerFlags.Counter);
                m_Type = GetProfilerMarkerDataType();
                ProfilerUnsafeUtility.SetMarkerMetadata(m_Marker.Handle, 0, null, m_Type, (byte)dataUnit);
            }
            /// <summary>
            /// Sample a single data point in the frame.
            /// </summary>
            public void Sample(T value)
            {
                unsafe
                {
                    var data = new ProfilerMarkerData
                    {
                        Type = m_Type,
                        Size = (uint)UnsafeUtility.SizeOf<T>(),
                        Ptr = UnsafeUtility.AddressOf(ref value)
                    };
                    ProfilerUnsafeUtility.SingleSampleWithMetadata(m_Marker.Handle, 1, &data);
                }
            }
            private static byte GetProfilerMarkerDataType()
            {
                switch (Type.GetTypeCode(typeof(T)))
                {
                    case TypeCode.Int32:
                        return (byte)ProfilerMarkerDataType.Int32;
                    case TypeCode.UInt32:
                        return (byte)ProfilerMarkerDataType.UInt32;
                    case TypeCode.Int64:
                        return (byte)ProfilerMarkerDataType.Int64;
                    case TypeCode.UInt64:
                        return (byte)ProfilerMarkerDataType.UInt64;
                    case TypeCode.Single:
                        return (byte)ProfilerMarkerDataType.Float;
                    case TypeCode.Double:
                        return (byte)ProfilerMarkerDataType.Double;
                    case TypeCode.String:
                        return (byte)ProfilerMarkerDataType.String16;
                    default:
                        throw new ArgumentException($"Type {typeof(T)} is unsupported by ProfilerCounter.");
                }
            }
        }

        /// <summary>
        /// Profiler counter to report cpu frametime.
        /// </summary>
        public static CustomProfilerMarker<float> CurrentCPUMarker = new CustomProfilerMarker<float>("CPU frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report cpu average frametime.
        /// </summary>
        public static CustomProfilerMarker<float> AvgCPUMarker = new CustomProfilerMarker<float>("CPU avg frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report gpu frametime.
        /// </summary>
        public static CustomProfilerMarker<float> CurrentGPUMarker = new CustomProfilerMarker<float>("GPU frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report gpu average frametime.
        /// </summary>
        public static CustomProfilerMarker<float> AvgGPUMarker = new CustomProfilerMarker<float>("GPU avg frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report cpu performance level.
        /// </summary>
        public static CustomProfilerMarker<int> CurrentCPULevelMarker = new CustomProfilerMarker<int>("CPU performance level", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report gpu performance level.
        /// </summary>
        public static CustomProfilerMarker<int> CurrentGPULevelMarker = new CustomProfilerMarker<int>("GPU performance level", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report frametime.
        /// </summary>
        public static CustomProfilerMarker<float> CurrentFrametimeMarker = new CustomProfilerMarker<float>("Frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report average frametime.
        /// </summary>
        public static CustomProfilerMarker<float> AvgFrametimeMarker = new CustomProfilerMarker<float>("Avg frametime", ProfilerMarkerDataUnit.TimeNanoseconds);
        /// <summary>
        /// Profiler counter to report the thermal warning level.
        /// </summary>
        public static CustomProfilerMarker<int> WarningLevelMarker = new CustomProfilerMarker<int>("Thermal Warning Level", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the temperature level.
        /// </summary>
        public static CustomProfilerMarker<float> TemperatureLevelMarker = new CustomProfilerMarker<float>("Temperature Level", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the temperature trend.
        /// </summary>
        public static CustomProfilerMarker<float> TemperatureTrendMarker = new CustomProfilerMarker<float>("Temperature Trend", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the bottleneck.
        /// </summary>
        public static CustomProfilerMarker<int> BottleneckMarker = new CustomProfilerMarker<int>("Bottleneck", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the performance mode.
        /// </summary>
        public static CustomProfilerMarker<int> PerformanceModeMarker = new CustomProfilerMarker<int>("Performance Mode", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the CPU utilization (normalized 0-1).
        /// </summary>
        public static CustomProfilerMarker<float> CpuUtilizationMarker = new CustomProfilerMarker<float>("CPU Utilization", ProfilerMarkerDataUnit.Count);
        /// <summary>
        /// Profiler counter to report the GPU utilization (normalized 0-1).
        /// </summary>
        public static CustomProfilerMarker<float> GpuUtilizationMarker = new CustomProfilerMarker<float>("GPU Utilization", ProfilerMarkerDataUnit.Count);

        /// <summary>
        /// GUID for the Adaptive Performance Profile Module definition.
        /// </summary>
        public static readonly Guid kAdaptivePerformanceProfilerModuleGuid = new Guid("42c5aeb7-fb77-4172-a384-34063f1bd332");

        /// <summary>
        /// The Scaler data tag defines a tag for the scalers to send them via the emit frame data function.
        /// </summary>
        public static readonly int kScalerDataTag = 0;

        const int kMaxScalerNameSizeInBytes = 320;

        static List<ScalerInfo> scalerInfos = new List<ScalerInfo>();
        static Dictionary<string, int> scalerInfosIndex = new Dictionary<string, int>();

        /// <summary>
        /// ScalerInfo is a struct used to collect and send scaler info to the profile collectively.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct ScalerInfo
        {
            /// <summary>
            /// The name of the scaler. 320 characters max.
            /// </summary>
            public fixed byte scalerName[320];
            /// <summary>
            /// If the scaler is currently enabled.
            /// </summary>
            public uint enabled;
            /// <summary>
            /// The override state of the scaler.
            /// </summary>
            public int overrideLevel;
            /// <summary>
            /// The current level of the scaler.
            /// </summary>
            public int currentLevel;
            /// <summary>
            /// The maximum level of the scaler.
            /// </summary>
            public int maxLevel;
            /// <summary>
            /// The actual scale of the scaler.
            /// </summary>
            public float scale;
            /// <summary>
            /// State if the scaler is currently applied.
            /// </summary>
            public uint applied;
        }

        /// <summary>
        /// Adaptive Performance sends scaler data to the profiler each frame. It is collected from multiple places with this method and flushed once with <see cref="FlushScalerDataToProfilerStream"/>.
        /// </summary>
        /// <param name="scalerName"> The name of the scaler. 320 characters max. </param>
        /// <param name="enabled"> If the scaler is currently enabled.</param>
        /// <param name="overrideLevel">The override state of the scaler.</param>
        /// <param name="currentLevel">The current level of the scaler.</param>
        /// <param name="scale">The actual scale of the scaler.</param>
        /// <param name="applied">If the scaler is currently applied.</param>
        /// <param name="maxLevel">The maximum level of the scaler.</param>
        public static void EmitScalerDataToProfilerStream(string scalerName, bool enabled, int overrideLevel, int currentLevel, float scale, bool applied, int maxLevel)
        {
            if (!Profiler.enabled || scalerName.Length == 0)
                return;

            int scalerIndex = -1;
            ScalerInfo info = default;

            unsafe
            {
                if (scalerInfosIndex.TryGetValue(scalerName, out int index))
                {
                    scalerIndex = index;
                    info = scalerInfos[scalerIndex];
                }

                else
                {
                    info = new ScalerInfo();
                    int copyLen = Math.Min(scalerName.Length, kMaxScalerNameSizeInBytes);
                    for (int i = 0; i < copyLen; ++i)
                        info.scalerName[i] = (byte)scalerName[i];
                    for (int i = copyLen; i < kMaxScalerNameSizeInBytes; ++i)
                        info.scalerName[i] = 0;
                }


                info.enabled = (uint)(enabled ? 1 : 0);
                info.overrideLevel = overrideLevel;
                info.currentLevel = currentLevel;
                info.scale = scale;
                info.maxLevel = maxLevel;
                info.applied = (uint)(applied ? 1 : 0);

                if (scalerIndex == -1) {
                    scalerInfos.Add(info);
                    scalerInfosIndex[scalerName] = scalerInfos.Count - 1;
                }
                else
                    scalerInfos[scalerIndex] = info;
            }
        }

        /// <summary>
        /// Flushes the Adaptive Performance scaler data for this frame. Used in conjunction with <see cref="EmitScalerDataToProfilerStream"/>.
        /// </summary>
        public static void FlushScalerDataToProfilerStream()
        {
            AdaptivePerformanceProfilerNative.EmitFrameMetaData<ScalerInfo>(kAdaptivePerformanceProfilerModuleGuid, kScalerDataTag, scalerInfos);
        }
    }
}
