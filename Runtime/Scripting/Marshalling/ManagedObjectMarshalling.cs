// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ManagedObjectMarshalling
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertToManaged<T>(IntPtr ptr) where T : class
        {
            return Unsafe.As<IntPtr, T>(ref ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToUnmanaged(object obj)
        {
            return Unsafe.As<object, IntPtr>(ref obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cleanup(IntPtr ptr)
        {
        }
    }
}
