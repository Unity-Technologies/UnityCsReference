// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeUtility
    {
        // Copies sizeof(T) bytes from ptr to output
        [MethodImpl(256)] // AggressiveInlining
        unsafe public static void CopyPtrToStructure<T>(void* ptr, out T output) where T : struct
        {
	    UnsafeUtilityInternal.CopyPtrToStructure<T>(ptr, out output);
        }

        // Copies sizeof(T) bytes from output to ptr
        [MethodImpl(256)] // AggressiveInlining
        unsafe public static void CopyStructureToPtr<T>(ref T input, void* ptr) where T : struct
        {
            UnsafeUtilityInternal.CopyStructureToPtr<T>(ref input, ptr);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static T ReadArrayElement<T>(void* source, int index)
        {
	    return UnsafeUtilityInternal.ReadArrayElement<T>(source, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static T ReadArrayElementWithStride<T>(void* source, int index, int stride)
        {
	    return UnsafeUtilityInternal.ReadArrayElementWithStride<T>(source, index, stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void WriteArrayElement<T>(void* destination, int index, T value)
        {
            UnsafeUtilityInternal.WriteArrayElement<T>(destination, index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void WriteArrayElementWithStride<T>(void* destination, int index, int stride, T value)
        {
            UnsafeUtilityInternal.WriteArrayElementWithStride<T>(destination, index, stride, value);
        }

        // The address of the memory where the struct resides in memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static void* AddressOf<T>(ref T output) where T : struct
        {
            return UnsafeUtilityInternal.AddressOf<T>(ref output);
        }

        // The size of a struct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>() where T : struct
        {
            return UnsafeUtilityInternal.SizeOf<T>();
        }


        // minimum alignment of a struct
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AlignOf<T>() where T : struct
        {
            return UnsafeUtilityInternal.AlignOf<T>();
        }

        // Reinterprets reference as reference of different type.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T As<U, T>(ref U from)
        {
            return ref UnsafeUtilityInternal.As<U, T>(ref from);
        }

        // Reinterprets reference type as different reference type.
        internal static T As<T>(object from) where T : class
        {
            return UnsafeUtilityInternal.As<T>(from);
        }

        // The address of the memory where the struct resides in memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static ref T AsRef<T>(void* ptr) where T : struct
        {
            return ref UnsafeUtilityInternal.AsRef<T>(ptr);
        }

        // The address of the memory where the class resides in memory
        unsafe internal static ref T ClassAsRef<T>(void* ptr) where T : class
        {
            return ref UnsafeUtilityInternal.ClassAsRef<T>(ptr);
        }

        // The address of the memory where the struct resides in memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static ref T ArrayElementAsRef<T>(void* ptr, int index) where T : struct
        {
            return ref UnsafeUtilityInternal.ArrayElementAsRef<T>(ptr, index);
        }

        // converts generic enum to int without boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnumToInt<T>(T enumValue) where T : struct, IConvertible
        {
            return UnsafeUtilityInternal.EnumToInt<T>(enumValue);
        }

        // generic enum equals check without boxing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EnumEquals<T>(T lhs, T rhs) where T : struct, IConvertible
        {
            return UnsafeUtilityInternal.EnumEquals<T>(lhs, rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static ref T Add<T> (ref T source, int elementOffset) where T : unmanaged
        {
            return ref UnsafeUtilityInternal.Add<T>(ref source, elementOffset);
        }

        // The address of the memory where the struct resides in memory
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe internal static void* AsPointer<T>(ref T output)
        {
            return UnsafeUtilityInternal.AsPointer<T>(ref output);
        }

        // A reference that is null
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T NullRef<T>()
        {
            return ref UnsafeUtilityInternal.NullRef<T>();
        }
    }
}
