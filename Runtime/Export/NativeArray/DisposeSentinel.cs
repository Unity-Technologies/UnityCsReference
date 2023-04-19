// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Collections
{
    public static class NativeLeakDetection
    {
        public static NativeLeakDetectionMode Mode
        {
            get
            {
                return UnsafeUtility.GetLeakDetectionMode();
            }
            set
            {
                if (value < NativeLeakDetectionMode.Disabled || value > NativeLeakDetectionMode.EnabledWithStackTrace)
                {
                    throw new ArgumentException("NativeLeakDetectionMode out of range");
                }

                UnsafeUtility.SetLeakDetectionMode(value);
            }
        }
    }
}


namespace Unity.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DisposeSentinel
    {
        static readonly IntPtr s_CreateProfilerMarkerPtr = Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.CreateMarker("DisposeSentinel.Create", Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.CategoryScripts, Profiling.LowLevel.MarkerFlags.Script | Profiling.LowLevel.MarkerFlags.AvailabilityEditor, 0);
        static readonly IntPtr s_LogErrorProfilerMarkerPtr = Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.CreateMarker("DisposeSentinel.LogError", Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.CategoryScripts, Profiling.LowLevel.MarkerFlags.Script | Profiling.LowLevel.MarkerFlags.AvailabilityEditor, 0);

        int m_IsCreated;
        StackTrace         m_StackTrace;

        private DisposeSentinel()
        {
        }

        public static void Dispose(ref AtomicSafetyHandle safety, ref DisposeSentinel sentinel)
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            // If the safety handle is for a temp allocation, create a new safety handle for this instance which can be marked as invalid
            // Setting it to new AtomicSafetyHandle is not enough since the handle needs a valid node pointer in order to give the correct errors
            if (AtomicSafetyHandle.IsTempMemoryHandle(safety))
            {
                int staticSafetyId = safety.staticSafetyId;
                safety = AtomicSafetyHandle.GetTempMemoryHandle();
                safety.staticSafetyId = staticSafetyId;
            }
            AtomicSafetyHandle.Release(safety);
            Clear(ref sentinel);
        }

        public static void Create(out AtomicSafetyHandle safety, out DisposeSentinel sentinel, int callSiteStackDepth, Allocator allocator)
        {
            safety = (allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
            sentinel = null;
            if (allocator == Allocator.Temp || allocator == Allocator.AudioKernel)
                return;

            if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("Jobs can only create Temp memory");

            CreateInternal(ref sentinel, callSiteStackDepth);
        }

        [Unity.Burst.BurstDiscard]
        private static void CreateInternal(ref DisposeSentinel sentinel, int callSiteStackDepth)
        {
            var mode = NativeLeakDetection.Mode;
            if (mode == NativeLeakDetectionMode.Disabled)
                return;

            Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.BeginSample(s_CreateProfilerMarkerPtr);

            StackTrace stackTrace = null;
            if (mode == NativeLeakDetectionMode.EnabledWithStackTrace)
                stackTrace = new StackTrace(callSiteStackDepth + 2, true);

            sentinel = new DisposeSentinel
            {
                m_StackTrace = stackTrace,
                m_IsCreated = 1
            };

            Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.EndSample(s_CreateProfilerMarkerPtr);
        }

        ~DisposeSentinel()
        {
            if (m_IsCreated != 0)
            {
                var fileName = "";
                var lineNb = 0;

                Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.BeginSample(s_LogErrorProfilerMarkerPtr);

                if (m_StackTrace != null)
                {
                    var stackTrace = UnityEngine.StackTraceUtility.ExtractFormattedStackTrace(m_StackTrace);
                    var err = "A Native Collection has not been disposed, resulting in a memory leak. Allocated from:\n" + stackTrace;

                    if (m_StackTrace.FrameCount != 0)
                    {
                        fileName = m_StackTrace.GetFrame(0).GetFileName();
                        lineNb = m_StackTrace.GetFrame(0).GetFileLineNumber();
                    }

                    UnsafeUtility.LogError(err, fileName, lineNb);
                }
                else
                {
                    var err = "A Native Collection has not been disposed, resulting in a memory leak. " +
                        "Enable Full StackTraces to get more details. Leak tracking may be enabled via `Unity.Collections.NativeLeakDetection.Mode` " +
                        "or from the editor preferences menu Edit > Preferences > Jobs > Leak Detection Level.";
                    UnsafeUtility.LogError(err, fileName, lineNb);
                }

                Profiling.LowLevel.Unsafe.ProfilerUnsafeUtility.EndSample(s_LogErrorProfilerMarkerPtr);
            }
        }

        [Unity.Burst.BurstDiscard]
        public static void Clear(ref DisposeSentinel sentinel)
        {
            if (sentinel != null)
            {
                sentinel.m_IsCreated = 0;
                sentinel = null;
            }
        }

        internal struct Dummy
        {
            public class Class { };
            public static implicit operator Dummy(Class value)
            {
                return default;
            }
        }

        internal static void Create(out AtomicSafetyHandle safety, out Dummy sentinel, int callSiteStackDepth, Allocator allocator)
        {
            safety = (allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
            sentinel = default;
            if (allocator == Allocator.Temp || allocator == Allocator.AudioKernel)
                return;

            if (Unity.Jobs.LowLevel.Unsafe.JobsUtility.IsExecutingJob)
                throw new InvalidOperationException("Jobs can only create Temp memory");

            sentinel = default;
        }

        internal static void Clear(ref Dummy sentinel)
        {
            sentinel = default;
        }

    }
}
