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
    internal struct Unmarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T UnmarshalUnityObject<T>(IntPtr gcHandlePtr) where T : UnityEngine.Object
        {
            if (gcHandlePtr == IntPtr.Zero)
                return null;

            var gcHandle = GCHandle.FromIntPtr(gcHandlePtr);
            var target = (T)gcHandle.Target;

            // This is to handle the MonoObjectNULL case
            // If the instance ID is zero then this is a fake null object and there is no native
            // object that owns this handle.  It was created in UnityEngineMarshalling.h and
            // needs to be freed here
            if (target.GetInstanceID() == 0)
                gcHandle.Free();
            return target;
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
