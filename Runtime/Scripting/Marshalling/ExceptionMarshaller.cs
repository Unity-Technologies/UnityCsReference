// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    internal static class ExceptionMarshaller
    {
        [ThreadStatic]
        static Exception s_pendingException;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPendingException()
        {
            var exc = s_pendingException;
            if (exc != null)
            {
                s_pendingException = null;
                throw exc;
            }
        }

        // called from C++
        [UnityEngine.Scripting.RequiredByNativeCode]
        static void SetPendingException(Exception ex)
        {
            s_pendingException = ex;
        }
    }
}
