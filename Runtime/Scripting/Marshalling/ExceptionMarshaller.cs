// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ExceptionMarshaller
    {
        [ThreadStatic]
        [AutoStaticsCleanupOnCodeReload]
        static Exception s_pendingException;

        // This method is called from Burst direct call methods
        // because in that context we don't know whether there's
        // a pending exception or not.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPendingException()
        {
            var exc = s_pendingException;
            if (exc != null)
            {
                ThrowPendingException();
            }
        }

        // This method is called from generated bindings methods.
        // It's called when we know that there is a pending exception,
        // so we can skip the check in CheckPendingException.
        // 
        // This method has special handling when compiled with Burst.
        // Specifically, Burst replaces the method body with Burst-compatible
        // code that exits out of the current Burst entry point,
        // and then managed code calls CheckPendingException.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowPendingException()
        {
            System.Diagnostics.Debug.Assert(s_pendingException != null);

            var exc = s_pendingException;
            s_pendingException = null;
            throw exc;
        }

        // called from C++
        [UnityEngine.Scripting.RequiredByNativeCode]
        static void SetPendingException(Exception ex)
        {
            s_pendingException = ex;
        }
    }
}
