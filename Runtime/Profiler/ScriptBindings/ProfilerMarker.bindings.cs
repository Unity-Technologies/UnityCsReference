// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace Unity.Profiling
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Profiler/ScriptBindings/ProfilerMarker.bindings.h")]
    public struct ProfilerMarker
    {
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        internal readonly IntPtr m_Ptr;

        public IntPtr Handle => m_Ptr;

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public ProfilerMarker(string name)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, ProfilerUnsafeUtility.CategoryScripts, MarkerFlags.Default, 0);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void Begin()
        {
            ProfilerUnsafeUtility.BeginSample(m_Ptr);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void Begin(Object contextUnityObject)
        {
            ProfilerUnsafeUtility.Internal_BeginWithObject(m_Ptr, contextUnityObject);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public void End()
        {
            ProfilerUnsafeUtility.EndSample(m_Ptr);
        }

        [Conditional("ENABLE_PROFILER")]
        internal void GetName(ref string name)
        {
            name = ProfilerUnsafeUtility.Internal_GetName(m_Ptr);
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
                ProfilerUnsafeUtility.BeginSample(markerPtr);
            }

            [MethodImpl(256)]
            public void Dispose()
            {
                ProfilerUnsafeUtility.EndSample(m_Ptr);
            }
        }

        [MethodImpl(256)]
        public AutoScope Auto()
        {
            return new AutoScope(m_Ptr);
        }
    }
}
