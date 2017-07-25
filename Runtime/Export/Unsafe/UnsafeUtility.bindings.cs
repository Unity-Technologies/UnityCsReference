// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Unsafe/UnsafeUtility.bindings.h")]
    [StaticAccessor("UnsafeUtility", StaticAccessorType.DoubleColon)]
    internal static class UnsafeUtility
    {
        // Copies sizeof(T) bytes from ptr to output
        public static void CopyPtrToStructure<T>(IntPtr ptr, out T output) where T : struct
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        // Copies sizeof(T) bytes from output to ptr
        public static void CopyStructureToPtr<T>(ref T output, IntPtr ptr) where T : struct
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        public static T ReadArrayElement<T>(IntPtr source, int index)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        public static T ReadArrayElementWithStride<T>(IntPtr source, int index, int stride)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        public static void WriteArrayElement<T>(IntPtr destination, int index, T value)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        public static void WriteArrayElementWithStride<T>(IntPtr destination, int index, int stride, T value)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        // The address of the memory where the struct resides in memory
        public static IntPtr AddressOf<T>(ref T output) where T : struct
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        // The size of a struct
        public static int SizeOf<T>() where T : struct
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        // minimum alignment of a struct
        public static int AlignOf<T>() where T : struct
        {
            throw new NotImplementedException("Patching this method failed");
        }

        public unsafe static int OffsetOf<T>(string name) where T : struct
        {
            return (int)System.Runtime.InteropServices.Marshal.OffsetOf(typeof(T), name);
        }

        [ThreadSafe]
        public static extern IntPtr Malloc(int size, int alignment, UnityEngine.Collections.Allocator label);

        [ThreadSafe]
        public static extern void Free(IntPtr memory, UnityEngine.Collections.Allocator label);

        [ThreadSafe]
        public static extern void MemCpy(IntPtr destination, IntPtr source, int size);

        [ThreadSafe]
        public static extern void MemMove(IntPtr destination, IntPtr source, int size);
        [ThreadSafe]
        public static extern void MemClear(IntPtr destination, int size);

        [ThreadSafe]
        public static extern int SizeOfStruct(Type type);

        // @TODO : This is probably not the ideal place to have this?
        [ThreadSafe]
        public static extern void LogError(string msg, string filename, int linenumber);
    }
}
