// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal struct Unmarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T UnmarshalUnityObject<T>(IntPtr gcHandlePtr) where T : UnityEngine.Object
        {
            if (gcHandlePtr == IntPtr.Zero)
                return null;

            var gcHandle = FromIntPtrUnsafe(gcHandlePtr);
            var target = (T)gcHandle.Target;

            // This is to handle the MonoObjectNULL case
            // If the instance ID is zero then this is a fake null object and there is no native
            // object that owns this handle.  It was created in UnityEngineMarshalling.h and
            // needs to be freed here
            if (target.GetInstanceID() == 0)
                gcHandle.Free();
            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GCHandle FromIntPtrUnsafe(IntPtr gcHandle)
        {
            // Use a unsafe memory cast to avoid the overhead of checking the
            // IntPtr value for validity in the current domain. The Mono class 
            // library does this which introduces meaningful overhead. We assume
            // the GC handle we hold is valid for the current domain. There
            // is no such validity check when constructing and retrieving GC
            // handles in C++, and we want to avoid the overhead of the check
            // when doing the equivalent in C#.
           return UnsafeUtility.As<IntPtr, GCHandle>(ref gcHandle);
        }
    }

    [VisibleToOtherModules]
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentNullException(object obj, string parameterName)
        {
            if (obj is UnityEngine.Object unityObj)
                UnityEngine.Object.MarshalledUnityObject.TryThrowEditorNullExceptionObject(unityObj, parameterName);
            throw new ArgumentNullException(parameterName);
        }

        [DoesNotReturn]
        public static void ThrowNullReferenceException(object obj)
        {
            if (obj is UnityEngine.Object unityObj)
                UnityEngine.Object.MarshalledUnityObject.TryThrowEditorNullExceptionObject(unityObj, null);
            throw new NullReferenceException(); 
        }
    }
}
