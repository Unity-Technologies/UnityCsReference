// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;

namespace Unity.Profiling
{
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.h")]
    [Flags]
    internal enum MarkerFlags
    {
        Default = 0,

        AvailabilityEditor = 1 << 2,
        AvailabilityNonDevelopment = 1 << 3,

        Warning = 1 << 4,

        VerbosityDebug = 1 << 10,
        VerbosityInternal = 1 << 11,
        VerbosityAdvanced = 1 << 12
    }

    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ProfilerMarker
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr m_Ptr;

        public ProfilerMarker(string name)
        {
            m_Ptr = Internal_Create(name, MarkerFlags.Default);
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
            Internal_Begin(m_Ptr);
        }

        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object contextUnityObject)
        {
            Internal_BeginWithObject(m_Ptr, contextUnityObject);
        }

        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
            Internal_End(m_Ptr);
        }

        [UsedByNativeCode]
        public struct AutoScope : IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            internal IntPtr m_Ptr;

            internal AutoScope(IntPtr markerPtr)
            {
                m_Ptr = markerPtr;
                Internal_Begin(markerPtr);
            }

            public void Dispose()
            {
                Internal_End(m_Ptr);
            }
        }

        public AutoScope Auto()
        {
            return new AutoScope(m_Ptr);
        }

        [ThreadSafe]
        [NativeConditional("ENABLE_PROFILER", "NULL")]
        static extern IntPtr Internal_Create(string name, MarkerFlags flags);

        [ThreadSafe]
        [NativeConditional("ENABLE_PROFILER")]
        static extern void Internal_Begin(IntPtr markerPtr);

        [ThreadSafe]
        [NativeConditional("ENABLE_PROFILER")]
        static extern void Internal_BeginWithObject(IntPtr markerPtr, UnityEngine.Object contextUnityObject);

        [ThreadSafe]
        [NativeConditional("ENABLE_PROFILER")]
        static extern void Internal_End(IntPtr markerPtr);
    }
}
