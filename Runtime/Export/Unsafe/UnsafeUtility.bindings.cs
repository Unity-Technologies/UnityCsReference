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
    internal static class UnsafeUtility
    {
        // Copies sizeof(T) bytes from ptr to output
        public static void CopyPtrToStructure<T>(IntPtr ptr, out T output) where T : struct
        {
            // @patched at compile time
            output = (T)Marshal.PtrToStructure(ptr, typeof(T));
        }

        // Copies sizeof(T) bytes from output to ptr
        public static void CopyStructureToPtr<T>(ref T output, IntPtr ptr) where T : struct
        {
            // @patched at compile time
            Marshal.StructureToPtr(output, ptr, false);
        }

        public static T ReadArrayElement<T>(IntPtr source, int index)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        public static void WriteArrayElement<T>(IntPtr destination, int index, T value)
        {
            // @patched at compile time
            throw new NotImplementedException("Patching this method failed");
        }

        // The address of the memory where the struct resides in memory
        public static IntPtr AddressOf<T>(ref T output) where T : struct
        {
            // @patched at compile time
            throw new NotImplementedException("UnsafeUtility.AddressOf : patching failed");
        }

        // The size of a struct
        public static int SizeOf<T>() where T : struct
        {
            //@TODO: Optimize to be constant lookup
            return SizeOfStruct(typeof(T));
        }

        // minimum alignment of a struct
        public static int AlignOf<T>() where T : struct
        {
            //@TODO: Implement fully
            return 4;
        }

        [ThreadSafe]
        public static extern IntPtr Malloc(int size, int alignment, UnityEngine.Collections.Allocator label);

        [ThreadSafe]
        public static extern void Free(IntPtr memory, UnityEngine.Collections.Allocator label);

        [ThreadSafe]
        public static extern void MemCpy(IntPtr destination, IntPtr source, int size);

        [ThreadSafe]
        public static extern int SizeOfStruct(Type type);

        // @TODO : This is probably not the ideal place to have this?
        [ThreadSafe]
        public static extern void LogError(string msg, string filename, int linenumber);
    }
}
