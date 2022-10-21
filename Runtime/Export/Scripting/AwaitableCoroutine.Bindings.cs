// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.ExceptionServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Mono/AwaitableCoroutine.h")]
    public partial class AwaitableCoroutine
    {

        [RequiredByNativeCode(GenerateProxy = true)]
        private void SetExceptionFromNative(Exception ex)
        {
            lock (_syncRoot)
            { 
                _exceptionToRethrow = ExceptionDispatchInfo.Capture(ex);
            }
        }


        [RequiredByNativeCode(GenerateProxy = true)]
        private void RunContinuation()
        {
            lock (_syncRoot)
            {
                _continuation?.Invoke();
            }
        }


        [FreeFunction("Scripting::AwaitableCoroutines::AttachManagedWrapper", IsThreadSafe = true)]
        private static extern void AttachManagedGCHandleToNativeCoroutine(IntPtr nativeCoroutine, UIntPtr gcHandle);

        [FreeFunction("Scripting::AwaitableCoroutines::Release", IsThreadSafe = true)]
        private static extern void ReleaseNativeCoroutine(IntPtr nativeCoroutine);

        [FreeFunction("Scripting::AwaitableCoroutines::Cancel", IsThreadSafe = true)]
        private static extern void CancelNativeCoroutine(IntPtr nativeCoroutine);

        [FreeFunction("Scripting::AwaitableCoroutines::IsCompleted", IsThreadSafe = true)]
        private static extern int IsNativeCoroutineCompleted(IntPtr nativeCoroutine);
    }
}
