// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.CompilerServices;
using System.Security;
using UnityEngine.Internal;

namespace UnityEngine
{
    [AsyncMethodBuilder(typeof(AwaitableCoroutineAsyncMethodBuilder<>))]
    public class AwaitableCoroutine<T>
    {
        static ThreadSafeObjectPool<AwaitableCoroutine<T>> _pool = new (()=>new ());
        private AwaitableCoroutine _coroutine;
        T _result;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ContinueWith(Action continuation)
        {
            _coroutine.SetContinuation(continuation);
        }
        private T GetResult()
        {
            try
            {
                _coroutine.PropagateExceptionAndRelease();
                return _result;
            }
            finally
            {
                _coroutine = null;
                _result = default;
                _pool.Release(this);
            }
        }

        internal void SetResultAndRaiseContinuation(T result)
        {
            _result = result;
            _coroutine.RaiseManagedCompletion(null);
        }

        internal void SetExceptionAndRaiseContinuation(Exception exception)
        {
            _coroutine.RaiseManagedCompletion(exception);
        }

        public void Cancel()
        {
            _coroutine.Cancel();
        }

        private AwaitableCoroutine() { }

        internal static AwaitableCoroutine<T> GetManaged()
        {
            var innerCoroutine = AwaitableCoroutine.NewManagedCoroutine();
            var result = _pool.Get();
            result._coroutine = innerCoroutine;
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
            private readonly AwaitableCoroutine<T> _coroutine;

            public Awaiter(AwaitableCoroutine<T> coroutine)
            {
                _coroutine = coroutine;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                _coroutine.ContinueWith(continuation);
            }

            public bool IsCompleted => _coroutine._coroutine.IsCompleted;
            public T GetResult() => _coroutine.GetResult();
        }
    }
}
