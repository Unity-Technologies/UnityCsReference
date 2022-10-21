// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
    [NativeHeader("Runtime/Mono/AsyncOperationCoroutine.h")]
    public partial class AwaitableCoroutine
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine FromAsyncOperation(AsyncOperation op, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = FromAsyncOperationInternal(op.m_Ptr);
            return FromNativeCoroutineHandle(ptr, cancellationToken);
        }


        [FreeFunction("Scripting::AwaitableCoroutines::FromAsyncOperation", ThrowsException = true)]
        private static extern IntPtr FromAsyncOperationInternal(IntPtr asyncOperation);
    }

    public static class AsyncOperationAwaitableExtensions
    {
        [ExcludeFromDocs]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine.Awaiter GetAwaiter(this AsyncOperation op)
        {
            return AwaitableCoroutine.FromAsyncOperation(op).GetAwaiter();
        }
    }
}
