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

        static public CustomSampler Create(string name)
        {
            IntPtr nativeSampler = CreateInternal(name);
            if (nativeSampler == IntPtr.Zero)
                return s_InvalidCustomSampler;
            return new CustomSampler(nativeSampler);
        }

        [NativeMethod(Name = "ProfilerBindings::CreateCustomSamplerInternal", IsFreeFunction = true, ThrowsException = true, IsThreadSafe = true)]
        private extern static IntPtr CreateInternal([NotNull] string name);

        [Conditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::CustomSampler_Begin", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        public extern void Begin();

        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object targetObject)
        {
            BeginWithObject(targetObject);
        }

        [Conditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::CustomSampler_BeginWithObject", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void BeginWithObject(UnityEngine.Object targetObject);

        [Conditional("ENABLE_PROFILER")]
        [NativeMethod(Name = "ProfilerBindings::CustomSampler_End", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        public extern void End();
    }
}
