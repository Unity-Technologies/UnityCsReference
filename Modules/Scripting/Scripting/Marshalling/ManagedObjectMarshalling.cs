// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ManagedObjectMarshalling
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertToManaged<T>(IntPtr ptr) where T : class
        {
            if (ptr == IntPtr.Zero)
                return null;

            return (T)GCHandle.FromIntPtr(ptr).Target;
        }

        // Native-to-managed proxies use this instead of ConvertToManaged.
        // ScriptingInvocation never checked argument types, so some native callers pass objects that are not a T.
        // TODO: Use ConvertToManaged here once those callers pass the right types.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertToManagedForProxy<T>(IntPtr ptr) where T : class
        {
            if (ptr == IntPtr.Zero)
                return null;

            return Unsafe.As<T>(GCHandle.FromIntPtr(ptr).Target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToUnmanaged(object obj)
        {
            if (obj == null)
                return IntPtr.Zero;

            return GCHandle.ToIntPtr(GCHandle.Alloc(obj));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToNativeForProxy(object obj)
        {
            return ConvertToUnmanaged(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cleanup(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
                GCHandle.FromIntPtr(ptr).Free();
        }
    }
}
