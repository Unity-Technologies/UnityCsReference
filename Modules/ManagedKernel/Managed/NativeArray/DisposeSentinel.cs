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
    ///<summary>Contains settings for native leak detection.</summary>
    public static class NativeLeakDetection
    {
        ///<summary>Set whether native memory leak detection should be enabled or disabled.</summary>
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
    ///<summary>Contains methods that automatically detect memory leaks.</summary>
    ///<remarks>The methods in <c>DisposeSentinel</c> are used by NativeContainer instances to automatically track memory leaks, and report them to you. However, you should use the <see cref="UnsafeUtility.MallocTracked" />
    ///method over <c>DisposeSentinel</c> where possible.
    ///
    ///<c>DisposeSentinel</c> is a managed object that's only referenced by the <c>NativeContainer</c> that holds the native data that you don't want to leak. The <c>DisposeSentinel</c> 
    ///finalizer is invoked when there aren't any references to the NativeContainer that owns it, and checks if the referenced data has been disposed correctly. If the data 
    ///hasn't been disposed of correctly, <c>DisposeSentinel</c> logs an error containing the information about when the initial allocation happened.
    ///
    ///<c>DisposeSentinel</c> creates garbage for the garbage collector to pick up, and any memory leaks are reported upon domain shutdown or reload.
    ///
    ///You can only use the <c>DisposeSentinel</c> class when ENABLE_UNITY_COLLECTIONS_CHECKS is defined.</remarks>
    ///<seealso cref="Unity.Collections.LowLevel.Unsafe.NativeContainerAttribute" />
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DisposeSentinel
    {
        static readonly IntPtr s_CreateProfilerMarkerPtr = KernelProfiler.CreateMarker("DisposeSentinel.Create", KernelProfiler.CategoryScripts, KernelProfiler.MarkerFlagScript | KernelProfiler.MarkerFlagAvailabilityEditor, 0);
        static readonly IntPtr s_LogErrorProfilerMarkerPtr = KernelProfiler.CreateMarker("DisposeSentinel.LogError", KernelProfiler.CategoryScripts, KernelProfiler.MarkerFlagScript | KernelProfiler.MarkerFlagAvailabilityEditor, 0);

        int m_IsCreated;
        StackTrace         m_StackTrace;

        private DisposeSentinel()
        {
        }

        ///<summary>Releases the <c>AtomicSafetyHandle</c> and clears the <c>DisposeSentinel</c>.</summary>
        ///<remarks>If the <c>AtomicSafetyHandle</c> can't be released, which usually happens either because a job is accessing the data or because the handle 
        ///has already been released, an exception is thrown.</remarks>
        ///<param name="safety">The <c>AtomicSafetyHandle</c> returned when invoking the **Create** method.</param>
        ///<param name="sentinel">The <c>DisposeSentinel</c> to clear.</param>
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

        ///<summary>Creates a new <see cref="AtomicSafetyHandle" /> and a new <see cref="DisposeSentinel" />, to be used to track safety and leaks on native data.</summary>
        ///<remarks>When <see cref="DisposeSentinel" /> is created, the call stack is stored in it to log a descriptive error when a memory leak is detected.</remarks>
        ///<param name="safety">The <see cref="AtomicSafetyHandle" /> to control access to the data related to the newly created <see cref="DisposeSentinel" />.</param>
        ///<param name="sentinel">The new <see cref="DisposeSentinel" />.</param>
        ///<param name="callSiteStackDepth">The stack depth where to extract the logging information from.</param>
        ///<param name="allocator">The allocator used for the native data being tracked.</param>
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

            KernelProfiler.BeginSample(s_CreateProfilerMarkerPtr);

            StackTrace stackTrace = null;
            if (mode == NativeLeakDetectionMode.EnabledWithStackTrace)
                stackTrace = new StackTrace(callSiteStackDepth + 2, true);

            sentinel = new DisposeSentinel
            {
                m_StackTrace = stackTrace,
                m_IsCreated = 1
            };

            KernelProfiler.EndSample(s_CreateProfilerMarkerPtr);
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~DisposeSentinel()
        {
            if (m_IsCreated != 0)
            {
                var fileName = "";
                var lineNb = 0;

                KernelProfiler.BeginSample(s_LogErrorProfilerMarkerPtr);

                if (m_StackTrace != null)
                {
                    var stackTrace = Unity.Scripting.StackTrace.Format(m_StackTrace);
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

                KernelProfiler.EndSample(s_LogErrorProfilerMarkerPtr);
            }
        }
#pragma warning restore UA5000

        ///<summary>Clears the <c>DisposeSentinel</c>.</summary>
        ///<remarks>A <c>DisposeSentinel</c> is usually cleared when the related <c>NativeContainer</c> is disposed.</remarks>
        ///<param name="sentinel">The <c>DisposeSentinel</c> to clear.</param>
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
