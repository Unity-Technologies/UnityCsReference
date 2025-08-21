// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Bindings
{
    // Notes:
    // The OutArray works by passing a stack reference to a managed array into native code
    // The OutArray structs must be ref structs - so they are always on the stack and so their array field is on the stack.
    // The structs then use custom managed marshalling pass a struct with 3 pointers into native code:
    //    1. The pointer to the array reference on the stack
    //    2. A native callback
    //    3. A managed callback - this managed callback is a pointer to a generic method, so it "captures" the generic argument so we can issue a newarr instead of calling Array.CreateInstance

    /// <summary>
    /// Returns an array that is created and filled from native code.  Native code requests the creation through a callback and fills the pinned array
    /// </summary>
    [VisibleToOtherModules]
    [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(OutArray<>.BindingsMarshaller))]
    internal ref struct OutArray<T> where T : unmanaged
    {
        public static Array CreateArray(int length)
        {
            return new T[length];
        }

        public unsafe static class BindingsMarshaller
        {
            public static OutArrayNativeData ConvertToUnmanaged(ref OutArray<T> marshalled)
            {
                return new OutArrayNativeData
                {
                    createAndCallback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, delegate* unmanaged[Cdecl]<byte*, IntPtr, void>, IntPtr, void>)&CreateAndFillCalbacks.CreateAndCallbackPinned1,
                    arrayRef = (IntPtr)UnsafeUtility.AsPointer(ref marshalled.array),
                    createArray = (IntPtr)(delegate* <int, Array>)&CreateArray
                };
            }

            public static OutArray<T> ConvertToManaged(in OutArrayNativeData unmanaged)
            {
                return new OutArray<T> { array = UnsafeUtility.ClassAsRef<T[]>((void*)unmanaged.arrayRef) };
            }
        }

        [Ignore]
        private T[] array;

        public T[] Value => array;
    }

    /// <summary>
    /// Returns an 2 dimensioned array (e.g. int[,]) that is created and filled from native code.  Native code requests the creation through a callback and fills the pinned array
    /// </summary>
    [VisibleToOtherModules]
    [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(OutArray2D<>.BindingsMarshaller))]
    internal ref struct OutArray2D<T> where T : unmanaged
    {
        public static Array CreateArray(int length1, int lenght2)
        {
            return new T[length1, lenght2];
        }

        public unsafe static class BindingsMarshaller
        {
            public static OutArrayNativeData ConvertToUnmanaged(ref OutArray2D<T> marshalled)
            {
                return new OutArrayNativeData
                {
                    createAndCallback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int, delegate* unmanaged[Cdecl]<byte*, IntPtr, void>, IntPtr, void>)&CreateAndFillCalbacks.CreateAndCallbackPinned2,
                    arrayRef = (IntPtr)UnsafeUtility.AsPointer(ref marshalled.array),
                    createArray = (IntPtr)(delegate* <int, int, Array>)&CreateArray
                };
            }

            public static OutArray2D<T> ConvertToManaged(in OutArrayNativeData unmanaged)
            {
                return new OutArray2D<T>  { array = UnsafeUtility.ClassAsRef<T[,]>((void*)unmanaged.arrayRef) };
            }
        }

        [Ignore]
        private T[,] array;

        public T[,] Value => array;
    }

    /// <summary>
    /// Returns a 3 dimensioned array (e.g. int[,,]) that is created and filled from native code.  Native code requests the creation through a callback and fills the pinned array
    /// </summary>
    [VisibleToOtherModules]
    [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(OutArray3D<>.BindingsMarshaller))]
    internal ref struct OutArray3D<T> where T : unmanaged
    {
        public static Array CreateArray(int length1, int lenght2, int length3)
        {
            return new T[length1, lenght2, length3];
        }

        public unsafe static class BindingsMarshaller
        {
            public static OutArrayNativeData ConvertToUnmanaged(ref OutArray3D<T> marshalled)
            {
                return new OutArrayNativeData
                {
                    createAndCallback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, int, int, delegate* unmanaged[Cdecl]<byte*, IntPtr, void>, IntPtr, void>)&CreateAndFillCalbacks.CreateAndCallbackPinned3,
                    arrayRef = (IntPtr)UnsafeUtility.AsPointer(ref marshalled.array),
                    createArray = (IntPtr)(delegate*<int, int, int, Array>)&CreateArray
                };
            }

            public static OutArray3D<T> ConvertToManaged(in OutArrayNativeData unmanaged)
            {
                return new OutArray3D<T> { array = UnsafeUtility.ClassAsRef<T[,,]>((void*)unmanaged.arrayRef) };
            }
        }

        [Ignore]
        private T[,,] array;

        public T[,,] Value => array;
    }

    [VisibleToOtherModules]
    unsafe ref struct OutArrayNativeData
    {
        public IntPtr arrayRef;             // Pointer to an Array& on the stack
        public IntPtr createAndCallback;    // Pointer to [UnmanagedCallersOnly] function that will be called from native code
        public IntPtr createArray;          // Managed function pointer to allocate the array
    }

    static class CreateAndFillCalbacks
    {
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe void CreateAndCallbackPinned1(IntPtr arrayPointer, IntPtr createArrayCb, int size, delegate* unmanaged[Cdecl]<byte*, IntPtr, void> callback, IntPtr arg)
        {
            ref Array arrayRef = ref UnsafeUtility.ClassAsRef<Array>((void*)arrayPointer);
            arrayRef = ((delegate*<int, Array>)createArrayCb)(size);

            fixed (byte* arrayData = UnsafeUtility.As<byte[]>(arrayRef))
            {
                callback(arrayData, arg);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe void CreateAndCallbackPinned2(IntPtr arrayPointer, IntPtr createArrayCb, int size1, int size2, delegate* unmanaged[Cdecl]<byte*, IntPtr, void> callback, IntPtr arg)
        {
            ref Array arrayRef = ref UnsafeUtility.ClassAsRef<Array>((void*)arrayPointer);
            arrayRef = ((delegate*<int, int, Array>)createArrayCb)(size1, size2);

            fixed (byte* arrayData = UnsafeUtility.As<byte[,]>(arrayRef))
            {
                callback(arrayData, arg);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static unsafe void CreateAndCallbackPinned3(IntPtr arrayPointer, IntPtr createArrayCb, int size1, int size2, int size3, delegate* unmanaged[Cdecl]<byte*, IntPtr, void> callback, IntPtr arg)
        {
            ref Array arrayRef = ref UnsafeUtility.ClassAsRef<Array>((void*)arrayPointer);
            arrayRef = ((delegate*<int, int, int, Array>)createArrayCb)(size1, size2, size2);

            fixed (byte* arrayData = UnsafeUtility.As<byte[,,]>(arrayRef))
            {
                callback(arrayData, arg);
            }
        }
    }
}
