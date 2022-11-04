// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Security;
using UnityEngine.Internal;

namespace UnityEngine
{
    public partial class Awaitable
    {
        [ExcludeFromDocs]
        public Awaiter GetAwaiter() => new Awaiter(this);
        [ExcludeFromDocs]
        public struct Awaiter : INotifyCompletion
        {
            private readonly Awaitable _awaited;
            internal Awaiter(Awaitable awaited) => _awaited = awaited;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                _awaited.SetContinuation(continuation);
            }

            public bool IsCompleted => _awaited.IsCompleted;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult() => _awaited.PropagateExceptionAndRelease();

        }

    }
}
