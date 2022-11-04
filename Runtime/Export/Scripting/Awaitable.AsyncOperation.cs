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
    [NativeHeader("Runtime/Mono/AsyncOperationAwaitable.h")]
    public partial class Awaitable
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable FromAsyncOperation(AsyncOperation op, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = FromAsyncOperationInternal(op.m_Ptr);
            return FromNativeAwaitableHandle(ptr, cancellationToken);
        }


        [FreeFunction("Scripting::Awaitables::FromAsyncOperation", ThrowsException = true)]
        private static extern IntPtr FromAsyncOperationInternal(IntPtr asyncOperation);
    }

    public static class AsyncOperationAwaitableExtensions
    {
        [ExcludeFromDocs]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable.Awaiter GetAwaiter(this AsyncOperation op)
        {
            return Awaitable.FromAsyncOperation(op).GetAwaiter();
        }
    }
}
