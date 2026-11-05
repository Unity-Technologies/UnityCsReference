// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.ExceptionServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Mono/Awaitable.h")]
    public partial class Awaitable
    {

        [RequiredByNativeCode]
        private static void SetExceptionFromNative(Awaitable self, Exception ex)
        {
            self.SetException(ex);
        }
    
        private void SetException(Exception ex)
        {
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                _exceptionToRethrow = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
        }


        // Message methods like Awake or Start can return an Awaitable that nothing awaits, so log its exception here.
        [RequiredByNativeCode]
        private static void LogExceptionsFromMessage(object returnValue, object context)
        {
            if (returnValue is not Awaitable awaitable)
                return;

            awaitable.SetContinuation(() =>
            {
                try
                {
                    awaitable.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Debug.LogException(e, context as Object);
                }
            });
        }

        [RequiredByNativeCode]
        private static void RunContinuationFromNative(Awaitable self)
        {
            self.RunContinuation();
        }

        private void RunContinuation()
        {
            Action continuation = null;
            bool lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                continuation = _continuation;
                _continuation = null;
            }
            finally
            {
                if (lockTaken)
                {
                    _spinLock.Exit();
                }
            }
            continuation?.Invoke();
        }


        [FreeFunction("Scripting::Awaitables::AttachManagedWrapper", IsThreadSafe = true)]
        private static extern void AttachManagedGCHandleToNativeAwaitable(IntPtr nativeAwaitable, UIntPtr gcHandle);

        [FreeFunction("Scripting::Awaitables::Release", IsThreadSafe = true)]
        private static extern void ReleaseNativeAwaitable(IntPtr nativeAwaitable);

        [FreeFunction("Scripting::Awaitables::Cancel", IsThreadSafe = true)]
        private static extern void CancelNativeAwaitable(IntPtr nativeAwaitable);

        [FreeFunction("Scripting::Awaitables::IsCompleted", IsThreadSafe = true)]
        private static extern int IsNativeAwaitableCompleted(IntPtr nativeAwaitable);
    }
}
