// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Collections
{
    /// <summary>
    /// Direct bindings to NativeKernel memory profiler functions.
    /// </summary>
    [NativeHeader("ManagedKernel/Profiler/KernelMemoryProfiler.bindings.h")]
    internal static unsafe class KernelMemoryProfiler
    {
        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern IntPtr GetOrCreateMemLabel(byte* areaName, int areaNameLen, byte* objectName, int objectNameLen);

        internal static IntPtr GetOrCreateMemLabel(string areaName, string objectName)
        {
            if (string.IsNullOrEmpty(areaName) && string.IsNullOrEmpty(objectName))
                return IntPtr.Zero;

            byte* areaUtf8 = null;
            byte* objectUtf8 = null;
            int areaLen = 0;
            int objectLen = 0;

            if (!string.IsNullOrEmpty(areaName))
            {
                areaLen = System.Text.Encoding.UTF8.GetByteCount(areaName);
                byte* areaBuffer = stackalloc byte[areaLen + 1];
                fixed (char* chars = areaName)
                {
                    System.Text.Encoding.UTF8.GetBytes(chars, areaName.Length, areaBuffer, areaLen);
                }
                areaBuffer[areaLen] = 0;
                areaUtf8 = areaBuffer;
            }

            if (!string.IsNullOrEmpty(objectName))
            {
                objectLen = System.Text.Encoding.UTF8.GetByteCount(objectName);
                byte* objectBuffer = stackalloc byte[objectLen + 1];
                fixed (char* chars = objectName)
                {
                    System.Text.Encoding.UTF8.GetBytes(chars, objectName.Length, objectBuffer, objectLen);
                }
                objectBuffer[objectLen] = 0;
                objectUtf8 = objectBuffer;
            }

            return GetOrCreateMemLabel(areaUtf8, areaLen, objectUtf8, objectLen);
        }

        [NativeMethod(IsThreadSafe = true)]
        [NativeConditional("ENABLE_MEM_PROFILER")]
        internal static extern long GetMemLabelRelatedMemorySize(IntPtr label);
    }
}
