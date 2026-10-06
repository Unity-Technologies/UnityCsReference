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
        [NativeHeader("Runtime/ScriptingBackend/ScriptingRuntimeBootstrap.h")]
        [FreeFunction("Scripting::Core::MarshalObjectsAsGCHandles", IsThreadSafe = true)]
        static extern bool MarshalObjectsAsGCHandles();

        static readonly bool s_marshalObjectsAsGHandles;

        static ManagedObjectMarshalling()
        {
            s_marshalObjectsAsGHandles = MarshalObjectsAsGCHandles();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ConvertToManaged<T>(IntPtr ptr) where T : class
        {
            var value = (nint)ptr;
            if (IsHandle(value))
            {
                var handle = (nuint)value & ~kBitMask;
                return (T)((GCHandle)(nint)handle).Target;
            }

            return Unsafe.As<IntPtr, T>(ref ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ConvertToUnmanaged(object obj)
        {
            if (obj == null)
                return IntPtr.Zero;

            if (s_marshalObjectsAsGHandles)
            {
                var handle = (nuint)(nint)(IntPtr)GCHandle.Alloc(obj);
                handle |= kHandleMask;

                return (nint)handle;
            }

            return Unsafe.As<object, IntPtr>(ref obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cleanup(IntPtr ptr)
        {
            var value = (nint)ptr;
            if (IsHandle(value))
            {
                var handle = (nuint)value & ~kBitMask;
                ((GCHandle)(nint)handle).Free();
            }
        }

        static bool IsPointer(IntPtr value) { return (((nuint)(nint)value) & kBitMask) == kPtrMask; }
        static bool IsRef(IntPtr value) { return (((nuint)(nint)value) & kBitMask) == kRefMask; }
        static bool IsHandle(IntPtr value) { return (((nuint)(nint)value) & kHandleMask) == kHandleMask; }

        const nuint kBitMask = 0b11;
        const nuint kPtrMask = 0b00;
        const nuint kRefMask = 0b01;
        const nuint kHandleMask = 0b10; // the low bit may or may not be set depending on the runtime
    }
}


