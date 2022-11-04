// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal struct Unmarshal
    {
        public static T UnmarshalUnityObject<T>(IntPtr gcHandlePtr) where T : UnityEngine.Object
        {
            if (gcHandlePtr == IntPtr.Zero)
                return null;

            var gcHandle = GCHandle.FromIntPtr(gcHandlePtr);
            return (T)gcHandle.Target;
        }
    }
}
