// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Profiling
{
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ProfilerMarker
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly IntPtr m_Ptr;

        [MethodImpl(256)]
        public ProfilerMarker(string name)
        {
            m_Ptr = Internal_Create(name, MarkerFlags.Default);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
            Internal_Begin(m_Ptr);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void Begin(UnityEngine.Object contextUnityObject)
        {
            Internal_BeginWithObject(m_Ptr, contextUnityObject);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
            Internal_End(m_Ptr);
        }

        [UsedByNativeCode]
        public struct AutoScope : IDisposable
        {
            [NativeDisableUnsafePtrRestriction]
            internal readonly IntPtr m_Ptr;

            [MethodImpl(256)]
            internal AutoScope(IntPtr markerPtr)
            {
                m_Ptr = markerPtr;
                Internal_Begin(markerPtr);
            }

            [MethodImpl(256)]
            public void Dispose()
            {
                Internal_End(m_Ptr);
            }
        }

        [MethodImpl(256)]
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
