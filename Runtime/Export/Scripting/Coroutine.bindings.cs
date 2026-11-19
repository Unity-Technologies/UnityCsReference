// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // MonoBehaviour.StartCoroutine returns a Coroutine. Instances of this class are only used to reference these coroutines and do not hold any exposed properties or functions.
    [NativeHeader("Runtime/Mono/Coroutine.h")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public sealed class Coroutine : YieldInstruction
    {
        internal IntPtr m_Ptr;
        Coroutine() {}

#pragma warning disable UAC0045 // Cleans up the native coroutine handle; CleanupCoroutineGC is thread-safe and the lifetime is tied to the GC.
        ~Coroutine()
        {
            ReleaseCoroutine(m_Ptr);
        }
#pragma warning restore UAC0045

        [FreeFunction("Coroutine::CleanupCoroutineGC", true)]
        extern static void ReleaseCoroutine(IntPtr ptr);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Coroutine coroutine) => coroutine.m_Ptr;
        }
    }
}
