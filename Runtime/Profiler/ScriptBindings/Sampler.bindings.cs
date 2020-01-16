// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Profiling
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Sampler.bindings.h")]
    [NativeHeader("Runtime/Profiler/Marker.h")]
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
            IntPtr nativeRecorder = GetRecorderInternal(m_Ptr);
            if (nativeRecorder == IntPtr.Zero)
                return Recorder.s_InvalidRecorder;

            return new Recorder(nativeRecorder);
        }

        public static Sampler Get(string name)
        {
            IntPtr nativeSampler = GetSamplerInternal(name);
            if (nativeSampler == IntPtr.Zero)
                return s_InvalidSampler;

            return new Sampler(nativeSampler);
        }

        public static int GetNames(List<string> names)
        {
            return GetSamplerNamesInternal(names);
        }

        [NativeMethod(Name = "GetName", IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        private extern string GetSamplerName();

        public string name
        {
            get { return isValid ? GetSamplerName() : null; }
        }

        [NativeMethod(Name = "ProfilerBindings::GetRecorderInternal", IsFreeFunction = true)]
        private extern static IntPtr GetRecorderInternal(IntPtr ptr);

        [NativeMethod(Name = "ProfilerBindings::GetSamplerInternal", IsFreeFunction = true)]
        private extern static IntPtr GetSamplerInternal([NotNull] string name);

        [NativeMethod(Name = "ProfilerBindings::GetSamplerNamesInternal", IsFreeFunction = true)]
        private extern static int GetSamplerNamesInternal(List<string> namesScriptingPtr);
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Sampler.bindings.h")]
    [NativeHeader("Runtime/Profiler/Marker.h")]
    public sealed class CustomSampler : Sampler
    {
        internal static CustomSampler s_InvalidCustomSampler = new CustomSampler();

        // This class can't be explicitly created
        internal CustomSampler() {}
        internal CustomSampler(IntPtr ptr) { m_Ptr = ptr; }

        public static CustomSampler Create(string name, bool collectGpuData = false)
        {
            IntPtr nativeSampler = CreateInternal(name, collectGpuData);
            if (nativeSampler == IntPtr.Zero)
                return s_InvalidCustomSampler;
            return new CustomSampler(nativeSampler);
        }

        [NativeMethod(Name = "ProfilerBindings::CreateCustomSamplerInternal", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        static extern IntPtr CreateInternal([NotNull] string name, bool collectGpuData);

        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
            Begin_Internal(m_Ptr);
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object targetObject)
        {
            BeginWithObject_Internal(m_Ptr, targetObject);
        }

        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
            End_Internal(m_Ptr);
        }

        [NativeMethod(Name = "ProfilerBindings::CustomSampler_Begin", IsFreeFunction = true, IsThreadSafe = true)]
        static extern void Begin_Internal(IntPtr ptr);

        [NativeMethod(Name = "ProfilerBindings::CustomSampler_BeginWithObject", IsFreeFunction = true, IsThreadSafe = true)]
        static extern void BeginWithObject_Internal(IntPtr ptr, UnityEngine.Object targetObject);

        [NativeMethod(Name = "ProfilerBindings::CustomSampler_End", IsFreeFunction = true, IsThreadSafe = true)]
        static extern void End_Internal(IntPtr ptr);
    }
}
