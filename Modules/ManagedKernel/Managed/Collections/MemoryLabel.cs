// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Collections;

///<summary>Represents a memory label used for profiling and tracking memory allocations in Unity.</summary>
///<remarks>Memory labels are useful when you want to group allocations for specific objects or systems. They can help in debugging memory issues and understanding the memory footprint of your application.
///
///                When you create a <see cref="Unity.Collections.NativeArray{T}" /> or <see cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.Malloc" />, you can specify a memory label to associate with that allocation.
///
///                Memory labels can only be used with allocators that support memory labeling, i.e. <see cref="Allocator.Persistent" /> and <see cref="Allocator.Domain" />.</remarks>
///<example>
///  <code><![CDATA[using UnityEngine;
///using Unity.Collections;
///
///public class MemoryLabelExample : MonoBehaviour
///{
///    static readonly MemoryLabel myMemoryLabel = new MemoryLabel("MyArea", "MyObject", Allocator.Persistent);
///
///    NativeArray<int> myArray;
///
///    void Start()
///    {
///        // Create array with fixed length and memory label
///        myArray = new NativeArray<int>(10, myMemoryLabel);
///
///        for (int i = 0; i < myArray.Length; i++)
///        {
///            myArray[i] = i * 2;
///        }
///    }
///
///    public void OnDestroy()
///    {
///        // Dispose of the array to avoid leaks
///        if (myArray.IsCreated)
///        {
///            myArray.Dispose();
///        }
///    }
///}]]></code>
///</example>
[StructLayout(LayoutKind.Sequential, Size = 16)]
[UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(BindingsMarshaller))]
public readonly struct MemoryLabel
{
    [NativeDisableUnsafePtrRestriction]
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal readonly IntPtr pointer;
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal readonly Allocator allocator;

    ///<summary>Initializes a new instance of the <see cref="MemoryLabel" /> struct.</summary>
    ///<remarks>Creates a new memory label with the specified area name, object name, and allocator.
    /// Attempting to create a memory label with empty strings for the area name or object name, or with an unsupported allocator, throws an exception.
    ///
    ///You can create multiple memory labels with the same area name and object name. However, for the purposes of memory tracking, this is equivalent to reusing a previously created label.
    /// 
    /// Creating a memory label is a thread-safe operation.</remarks>
    ///<param name="areaName">The name of the memory area.</param>
    ///<param name="objectName">The name of the object being labeled.</param>
    ///<param name="allocator">The allocator to use. Defaults to <see cref="Allocator.Persistent" />. Only <see cref="Allocator.Persistent" /> and <see cref="Allocator.Domain" /> support memory labeling.</param>
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
        // Call native binding directly to avoid static initialization order issues with the bridge.
        this.pointer = MemoryLabelBindings.GetOrCreateMemLabel(areaName, objectName);
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
        // Call native binding directly to avoid static initialization order issues with the bridge.
        this.pointer = MemoryLabelBindings.GetOrCreateMemLabel__Unmanaged(areaName, areaNameLen, objectName, objectNameLen);
    }

    internal MemoryLabel(NativeData nativeData)
    {
        if (!SupportsAllocator(nativeData.allocator))
            throw new ArgumentException("Only Allocator.Persistent and Allocator.Domain support allocating with a label");

        this.pointer = nativeData.pointer;
        this.allocator = nativeData.allocator;
    }

    ///<summary>Determines whether the specified allocator supports memory labeling.</summary>
    ///<param name="allocator">The allocator to check.</param>
    ///<returns>True if the allocator supports labeling; otherwise, false.</returns>
    public static bool SupportsAllocator(Allocator allocator)
    {
        return allocator == Allocator.Persistent || allocator == Allocator.Domain;
    }

    static bool IsNullOrEmpty(string str) => string.IsNullOrEmpty(str);

    // This will only be referenced from Burst-generated code, in place of the version without the
    // __Unmanaged suffix. So we need to make sure it will not get stripped.
    [RequiredMember]
    static unsafe bool IsNullOrEmpty__Unmanaged(byte* name, int nameLen) => name == null || nameLen <= 0;

    internal long RelatedMemorySize => MemoryLabelBindings.GetMemLabelRelatedMemorySize(pointer);

    ///<summary>Gets a value indicating whether this memory label has been created.</summary>
    public bool IsCreated => allocator != Allocator.Invalid;

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    [VisibleToOtherModules("UnityEngine.CoreModule")]
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
