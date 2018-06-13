// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Profiling
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Profiler/ScriptBindings/Recorder.bindings.h")]
    [NativeHeader("Runtime/Profiler/Recorder.h")]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class Recorder
    {
        internal IntPtr m_Ptr;
        static internal Recorder s_InvalidRecorder = new Recorder();

        // This class can't be explicitly created
        internal Recorder() {}
        internal Recorder(IntPtr ptr) { m_Ptr = ptr; }

        ~Recorder()
        {
            if (m_Ptr != IntPtr.Zero)
                DisposeNative(m_Ptr);
        }

        public static Recorder Get(string samplerName)
        {
            IntPtr nativeRecorder = GetInternal(samplerName);
            if (nativeRecorder == IntPtr.Zero)
                return s_InvalidRecorder;

            return new Recorder(nativeRecorder);
        }

        [NativeMethod(Name = "ProfilerBindings::GetRecorderInternal", IsFreeFunction = true)]
        private extern static IntPtr GetInternal(string samplerName);

        public bool isValid
        {
            get { return m_Ptr != IntPtr.Zero; }
        }

        [NativeMethod(Name = "ProfilerBindings::DisposeNativeRecorder", IsFreeFunction = true, IsThreadSafe = true)]
        private extern static void DisposeNative(IntPtr ptr);

        public bool enabled
        {
            get { return isValid ? IsEnabled() : false; }
            set { if (isValid) SetEnabled(value); }
        }

        [NativeMethod(IsThreadSafe = true)]
        private extern bool IsEnabled();

        [NativeMethod(IsThreadSafe = true)]
        private extern void SetEnabled(bool enabled);

        public long elapsedNanoseconds
        {
            get { return isValid ? GetElapsedNanoseconds() : 0; }
        }

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        private extern long GetElapsedNanoseconds();

        public int sampleBlockCount
        {
            get { return isValid ? GetSampleBlockCount() : 0; }
        }

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_PROFILER")]
        private extern int GetSampleBlockCount();

        [ThreadSafe]
        public extern void FilterToCurrentThread();

        [ThreadSafe]
        public extern void CollectFromAllThreads();
    }
}
