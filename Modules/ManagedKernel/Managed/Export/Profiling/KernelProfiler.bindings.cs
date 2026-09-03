// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace Unity.Collections
{
    /// <summary>
    /// Direct bindings to NativeKernel profiler functions.
    /// These bypass the bridge indirection by calling directly into NativeKernel.
    /// </summary>
    [NativeHeader("ManagedKernel/Profiler/KernelProfiler.bindings.h")]
    internal static unsafe class KernelProfiler
    {
        internal const ushort CategoryScripts = 1;
        internal const ushort MarkerFlagScript = 2;
        internal const ushort MarkerFlagAvailabilityEditor = 4;

        [NativeMethod(IsThreadSafe = true)]
        internal static extern IntPtr CreateMarker(byte* name, int nameLen, ushort categoryId, ushort flags, int metadataCount);

        internal static IntPtr CreateMarker(string name, ushort categoryId, ushort flags, int metadataCount)
        {
            if (string.IsNullOrEmpty(name))
                return IntPtr.Zero;

            int byteCount = System.Text.Encoding.UTF8.GetByteCount(name);
            byte* utf8 = stackalloc byte[byteCount + 1];
            fixed (char* chars = name)
            {
                System.Text.Encoding.UTF8.GetBytes(chars, name.Length, utf8, byteCount);
            }
            utf8[byteCount] = 0;

            return CreateMarker(utf8, byteCount, categoryId, flags, metadataCount);
        }

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void BeginSample(IntPtr markerPtr);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void EndSample(IntPtr markerPtr);
    }
}
