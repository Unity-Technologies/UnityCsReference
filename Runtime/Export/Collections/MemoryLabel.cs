// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections;

[StructLayout(LayoutKind.Sequential, Size = 16)]
public readonly struct MemoryLabel
{
    [NativeDisableUnsafePtrRestriction] internal readonly IntPtr pointer;
    internal readonly Allocator allocator;

    public MemoryLabel(string areaName, string objectName, Allocator allocator = Allocator.Persistent)
    {
        if (IsNullOrEmpty(areaName))
            throw new ArgumentNullException(nameof(areaName));

        if (IsNullOrEmpty(objectName))
            throw new ArgumentNullException(nameof(objectName));

        if (!SupportsAllocator(allocator))
            throw new ArgumentException("Only Allocator.Persistent and Allocator.Domain support allocating with a label");

        this.allocator = allocator;
        // Important: this returns a null pointer in non-development builds.
        this.pointer = ProfilerUnsafeUtility.GetOrCreateMemLabel(areaName, objectName);
    }

    public static bool SupportsAllocator(Allocator allocator)
    {
        return allocator == Allocator.Persistent || allocator == Allocator.Domain;
    }

    static bool IsNullOrEmpty(string str) => string.IsNullOrEmpty(str);

    // This will only be referenced from Burst-generated code, in place of the version without the
    // __Unmanaged suffix. So we need to make sure it will not get stripped.
    [RequiredMember]
    static unsafe bool IsNullOrEmpty__Unmanaged(byte* name, int nameLen) => name == null || nameLen <= 0;

    internal long RelatedMemorySize => ProfilerUnsafeUtility.GetMemLabelRelatedMemorySize(pointer);

    public bool IsCreated => allocator != Allocator.Invalid;

    internal void CheckArgument()
    {
        if (!IsCreated)
            throw new ArgumentException("MemoryLabel has not been created. Use the constructor to create it.");
    }
}
