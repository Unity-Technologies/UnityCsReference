// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ObjectAsGCHandleMarshaller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToUnmanaged(object obj, GCHandleType handleType)
        {
            if (obj == null)
                return IntPtr.Zero;

            return GCHandle.ToIntPtr(GCHandle.Alloc(obj, handleType));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertToManaged<T>(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return default;

            return (T)GCHandle.FromIntPtr(handle).Target;
        }
    }

    [VisibleToOtherModules]
    internal static class GCHandleMarshaller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToUnmanaged(GCHandle handle)
        {
            return GCHandle.ToIntPtr(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GCHandle ConvertToManaged(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return default;

            return GCHandle.FromIntPtr(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
                GCHandle.FromIntPtr(handle).Free();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ResolveSafe<T>(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return default;
            return (T)GCHandle.FromIntPtr(handle).Target;
        }
    }
}
