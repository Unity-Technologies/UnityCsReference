// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections;

[StructLayout(LayoutKind.Sequential, Size = 16)]
[UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BindingsMarshaller))]
public readonly struct MemoryLabel
{
    [NativeDisableUnsafePtrRestriction]
    internal readonly IntPtr pointer;
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

    internal unsafe MemoryLabel(byte* areaName, int areaNameLen, byte* objectName, int objectNameLen, Allocator allocator = Allocator.Persistent)
    {
        if (IsNullOrEmpty__Unmanaged(areaName, areaNameLen))
            throw new ArgumentNullException(nameof(areaName));

        if (IsNullOrEmpty__Unmanaged(objectName, objectNameLen))
            throw new ArgumentNullException(nameof(objectName));

        if (!SupportsAllocator(allocator))
            throw new ArgumentException("Only Allocator.Persistent and Allocator.Domain support allocating with a label");
        
        this.allocator = allocator;
        // Important: this returns a null pointer in non-development builds.
        this.pointer = ProfilerUnsafeUtility.GetOrCreateMemLabel__Unmanaged(areaName, areaNameLen, objectName, objectNameLen);
    }

    internal MemoryLabel(NativeData nativeData)
    {
        if (!SupportsAllocator(nativeData.allocator))
            throw new ArgumentException("Only Allocator.Persistent and Allocator.Domain support allocating with a label");

        this.pointer = nativeData.pointer;
        this.allocator = nativeData.allocator;
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

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    internal void CheckArgument()
    {
        if (!IsCreated)
            throw new ArgumentException("MemoryLabel has not been created. Use the constructor to create it.");
    }

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal struct NativeData
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr pointer;
        internal Allocator allocator;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void CheckNativeToManaged()
        {
            if (!SupportsAllocator(allocator))
                throw new ArgumentException("Only Allocator.Persistent and Allocator.Domain support allocating with a label");
        }
    }

    internal static class BindingsMarshaller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
        public static unsafe NativeData ConvertToUnmanaged(ref MemoryLabel memoryLabel)
        {
            memoryLabel.CheckArgument();
            fixed (MemoryLabel* memoryLabelPtr = &memoryLabel)
                return *(NativeData*)(memoryLabelPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
        public static unsafe MemoryLabel ConvertToManaged(in NativeData nativeData)
        {
            nativeData.CheckNativeToManaged();
            fixed (NativeData* nativeDataPtr = &nativeData)
                return *(MemoryLabel*)(nativeDataPtr);
        }
    }
}
