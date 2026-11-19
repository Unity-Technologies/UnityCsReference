// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Profiling
{
    // Profiler marker for asynchronous operations whose begin and end are not a stacked pair on a
    // single thread. Begin() yields an asyncId that the caller stores and passes to End() when the
    // operation completes - possibly on a different thread, many frames later.
    //
    // Unlike ProfilerMarker (a synchronous, thread-local begin/end scope), an AsyncProfilerMarker
    // span is identified solely by its asyncId. Each span becomes its own track in the captured data.
    //
    // Usage:
    //   static readonly AsyncProfilerMarker s_Read = new AsyncProfilerMarker(ProfilerCategory.Loading, "File.ReadAsync");
    //   ulong id = 0;
    //   s_Read.Begin(ref id, filename);   // store id in the request context
    //   ... async work, possibly another thread / later frame ...
    //   s_Read.End(id, summary);          // completion
    [UsedByNativeCode]
    [IgnoredByDeepProfiler]
    [StructLayout(LayoutKind.Sequential)]
    internal struct AsyncProfilerMarker
    {
        [NativeDisableUnsafePtrRestriction]
        [NonSerialized]
        internal readonly IntPtr m_Ptr;

        public IntPtr Handle => m_Ptr;

        // 256 : Aggressive inlining
        [MethodImpl(256)]
        public AsyncProfilerMarker(ProfilerCategory category, string name)
        {
            m_Ptr = ProfilerUnsafeUtility.CreateMarker(name, category, MarkerFlags.Default, 0);
        }

        // Opens an async span. Writes the session-unique asyncId into asyncId. Store it and pass it
        // to End. The whole call (including evaluation of arguments) is stripped when profiling is
        // compiled out, which is why asyncId is a ref out-parameter rather than a return value:
        // initialize it to 0 ("unset") at the call site and it stays 0 when the call is stripped.
        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public unsafe void Begin(ref ulong asyncId)
        {
            asyncId = ProfilerUnsafeUtility.BeginAsyncSample(m_Ptr, 0, null);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public unsafe void Begin(ref ulong asyncId, string metadata)
        {
            if (string.IsNullOrEmpty(metadata))
            {
                asyncId = ProfilerUnsafeUtility.BeginAsyncSample(m_Ptr, 0, null);
            }
            else
            {
                ProfilerMarkerData data = new ProfilerMarkerData { Type = (byte)ProfilerMarkerDataType.String16 };
                fixed (char* metadataChars = metadata)
                {
                    data.Size = ((uint)metadata.Length + 1) * 2;
                    data.Ptr = metadataChars;
                    asyncId = ProfilerUnsafeUtility.BeginAsyncSample(m_Ptr, 1, &data);
                }
            }
        }

        // Closes the async span identified by asyncId. An asyncId of 0 (never begun, or begun while
        // profiling was inactive) is a no-op. A duplicate end is dropped on the editor side.
        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public unsafe void End(ulong asyncId)
        {
            ProfilerUnsafeUtility.EndAsyncSample(asyncId, 0, null);
        }

        [MethodImpl(256)]
        [Conditional("ENABLE_PROFILER")]
        public unsafe void End(ulong asyncId, string metadata)
        {
            if (string.IsNullOrEmpty(metadata))
            {
                ProfilerUnsafeUtility.EndAsyncSample(asyncId, 0, null);
            }
            else
            {
                ProfilerMarkerData data = new ProfilerMarkerData { Type = (byte)ProfilerMarkerDataType.String16 };
                fixed (char* metadataChars = metadata)
                {
                    data.Size = ((uint)metadata.Length + 1) * 2;
                    data.Ptr = metadataChars;
                    ProfilerUnsafeUtility.EndAsyncSample(asyncId, 1, &data);
                }
            }
        }
    }
}
