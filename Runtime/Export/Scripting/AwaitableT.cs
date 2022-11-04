// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.CompilerServices;
using System.Security;
using UnityEngine.Internal;

namespace UnityEngine
{
    [AsyncMethodBuilder(typeof(Awaitable.AwaitableAsyncMethodBuilder<>))]
    public class Awaitable<T>
    {
        static Awaitable.ThreadSafeObjectPool<Awaitable<T>> _pool = new (()=>new ());
        private Awaitable _awaitable;
        T _result;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ContinueWith(Action continuation)
        {
            _awaitable.SetContinuation(continuation);
        }
        private T GetResult()
        {
            try
            {
                _awaitable.PropagateExceptionAndRelease();
                return _result;
            }
            finally
            {
                _awaitable = null;
                _result = default;
                _pool.Release(this);
            }
        }

        internal void SetResultAndRaiseContinuation(T result)
        {
            _result = result;
            _awaitable.RaiseManagedCompletion(null);
        }

        internal void SetExceptionAndRaiseContinuation(Exception exception)
        {
            _awaitable.RaiseManagedCompletion(exception);
        }

        public void Cancel()
        {
            _awaitable.Cancel();
        }

        private Awaitable() { }

        internal static Awaitable<T> GetManaged()
        {
            var innerCoroutine = Awaitable.NewManagedAwaitable();
            var result = _pool.Get();
            result._awaitable = innerCoroutine;
            return result;
        }

        [ExcludeFromDocs]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }
        [ExcludeFromDocs]
        public struct Awaiter : INotifyCompletion
        {
            private readonly Awaitable<T> _coroutine;

            public Awaiter(Awaitable<T> coroutine)
            {
                _coroutine = coroutine;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                _coroutine.ContinueWith(continuation);
            }

            public bool IsCompleted => _coroutine._awaitable.IsCompleted;
            public T GetResult() => _coroutine.GetResult();
        }
    }
}
