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

        ~Coroutine()
        {
            ReleaseCoroutine(m_Ptr);
        }

        [FreeFunction("Coroutine::CleanupCoroutineGC", true)]
        extern static void ReleaseCoroutine(IntPtr ptr);
    }
}
