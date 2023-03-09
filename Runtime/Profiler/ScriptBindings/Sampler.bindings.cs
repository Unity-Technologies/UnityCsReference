// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Profiling;

namespace UnityEngine.Profiling
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Sampler.bindings.h")]
    public class Sampler
    {
        internal IntPtr m_Ptr;
        internal static Sampler s_InvalidSampler = new Sampler();

        // This class can't be explicitly created
        internal Sampler() {}
        internal Sampler(IntPtr ptr) { m_Ptr = ptr; }

        public bool isValid
        {
            get { return m_Ptr != IntPtr.Zero; }
        }

        public Recorder GetRecorder()
        {
            var handle = new ProfilerRecorderHandle((ulong)m_Ptr.ToInt64());
            return new Recorder(handle);
        }

        public static Sampler Get(string name)
        {
            IntPtr nativeSampler = ProfilerUnsafeUtility.GetMarker(name);
            if (nativeSampler == IntPtr.Zero)
                return s_InvalidSampler;

            return new Sampler(nativeSampler);
        }

        public static int GetNames(List<string> names)
        {
            var availableMarkers = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableMarkers);

            if (names != null)
            {
                if (names.Count < availableMarkers.Count)
                {
                    names.Capacity = availableMarkers.Count;
                    for (int i = names.Count; i < availableMarkers.Count; i++)
                        names.Add(null);
                }

                int index = 0;
                foreach (var h in availableMarkers)
                {
                    var statDesc = ProfilerRecorderHandle.GetDescription(h);
                    names[index] = statDesc.Name;
                    index++;
                }
            }

            return availableMarkers.Count;
        }

        public string name
        {
            get { return ProfilerUnsafeUtility.Internal_GetName(m_Ptr); }
        }
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Sampler.bindings.h")]
    [NativeHeader("Runtime/Profiler/Marker.h")]
    public sealed class CustomSampler : Sampler
    {
        internal static CustomSampler s_InvalidCustomSampler = new CustomSampler();

        // This class can't be explicitly created
        internal CustomSampler() {}
        private CustomSampler(IntPtr ptr) : base(ptr) { }

        public static CustomSampler Create(string name, bool collectGpuData = false)
        {
            IntPtr nativeSampler = ProfilerUnsafeUtility.CreateMarker(name, ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.AvailabilityNonDevelopment | (collectGpuData ? MarkerFlags.SampleGPU : 0), 0);
            if (nativeSampler == IntPtr.Zero)
                return s_InvalidCustomSampler;
            return new CustomSampler(nativeSampler);
        }

        [Conditional("ENABLE_PROFILER")]
        [IgnoredByDeepProfiler]
        public void Begin()
        {
            ProfilerUnsafeUtility.BeginSample(m_Ptr);
        }

        [Conditional("ENABLE_PROFILER")]
        [IgnoredByDeepProfiler]
        public void Begin(Object targetObject)
        {
            ProfilerUnsafeUtility.Internal_BeginWithObject(m_Ptr, targetObject);
        }

        [Conditional("ENABLE_PROFILER")]
        [IgnoredByDeepProfiler]
        public void End()
        {
            ProfilerUnsafeUtility.EndSample(m_Ptr);
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(CustomSampler customSampler) => customSampler.m_Ptr;
        }
    }
}
