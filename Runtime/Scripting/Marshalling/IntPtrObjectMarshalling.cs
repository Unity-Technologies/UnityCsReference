// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using UnityEngine.Scripting;

namespace UnityEngine.Bindings
{
    // Why all of the ENABLE_CORECLR defines?
    // This code is only used on CoreCLR and references API's that we only define on CoreCLR (e.g ScriptingUtility.Get/SetValueAtOffset)
    // But the RequiredByNativeCodeMethods's are processed on the Mono backend so this class and those methods need to exist there, even though we
    // can't implement them and won't call them.

    internal class IntPtrObjectMarshalling
    {
        [RequiredByNativeCode]
        internal static object CreateDefault(IntPtr type, out IntPtr nativePointer)
        {
            throw new PlatformNotSupportedException("IntPtrObjectMarshalling is only supported with CoreCLR scripting backend.");
        }

        [RequiredByNativeCode]
        internal static object CreateFromNative(IntPtr typePtr, IntPtr ptr)
        {
            throw new PlatformNotSupportedException("IntPtrObjectMarshalling is only supported with CoreCLR scripting backend.");
        }

        [RequiredByNativeCode]
        internal static IntPtr GetIntPtr(object obj)
        {
            throw new PlatformNotSupportedException("IntPtrObjectMarshalling is only supported with CoreCLR scripting backend.");
        }

        [RequiredByNativeCode]
        internal static void SetIntPtr(object obj, IntPtr ptr)
        {
            throw new PlatformNotSupportedException("IntPtrObjectMarshalling is only supported with CoreCLR scripting backend.");
        }

    }
}
