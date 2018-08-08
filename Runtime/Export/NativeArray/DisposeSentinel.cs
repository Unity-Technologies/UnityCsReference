// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Unity.Collections
{
    public enum NativeLeakDetectionMode
    {
        Enabled = 0,
        Disabled = 1
    }

    public static class NativeLeakDetection
    {
        // For performance reasons no assignment operator (static initializer cost in il2cpp)
        // and flipped enabled / disabled enum value
        static int s_NativeLeakDetectionMode;

        public static NativeLeakDetectionMode Mode { get { return (NativeLeakDetectionMode)s_NativeLeakDetectionMode; } set { s_NativeLeakDetectionMode = (int)value; } }
    }
}


namespace Unity.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DisposeSentinel
    {
        int                m_IsCreated;
        StackFrame         m_StackFrame;

        private DisposeSentinel()
        {
        }

        public static void Dispose(ref AtomicSafetyHandle safety, ref DisposeSentinel sentinel)
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(safety);
            // If the safety handle is for a temp allocation, create a new safety handle for this instance which can be marked as invalid
            // Setting it to new AtomicSafetyHandle is not enough since the handle needs a valid node pointer in order to give the correct errors
            if (AtomicSafetyHandle.IsTempMemoryHandle(safety))
                safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.Release(safety);
            Clear(ref sentinel);
        }

        public static void Create(out AtomicSafetyHandle safety, out DisposeSentinel sentinel, int callSiteStackDepth, Allocator allocator)
        {
            safety = (allocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();

            if (NativeLeakDetection.Mode == NativeLeakDetectionMode.Enabled && allocator != Allocator.Temp)
            {
                sentinel = new DisposeSentinel
                {
                    m_StackFrame = new StackFrame(callSiteStackDepth + 2, true),
                    m_IsCreated = 1
                };
            }
            else
            {
                sentinel = null;
            }
        }

        ~DisposeSentinel()
        {
            if (m_IsCreated != 0)
            {
                var fileName = m_StackFrame.GetFileName();
                var lineNb = m_StackFrame.GetFileLineNumber();

                var err = string.Format("A Native Collection has not been disposed, resulting in a memory leak. It was allocated at {0}:{1}.", fileName, lineNb);
                UnsafeUtility.LogError(err, fileName, lineNb);
            }
        }

        public static void Clear(ref DisposeSentinel sentinel)
        {
            if (sentinel != null)
            {
                sentinel.m_IsCreated = 0;
                sentinel = null;
            }
        }
    }
}
