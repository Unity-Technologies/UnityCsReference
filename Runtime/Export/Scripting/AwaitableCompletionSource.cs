// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;

namespace UnityEngine
{
    public class AwaitableCompletionSource
    {
        volatile int _state; // 0: not raise, 1: raised or canceled
        public Awaitable Awaitable { get; private set; } = Awaitable.NewManagedAwaitable();

        public void SetResult()
        {
            if (!TrySetResult()) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        public void SetCanceled()
        {
            if (!TrySetCanceled()) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        public void SetException(Exception exception)
        {
            if (!TrySetException(exception)) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        bool CheckAndAcquireCompletionState() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;

        public bool TrySetResult()
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.RaiseManagedCompletion();
            return true;
        }

        public bool TrySetCanceled()
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.Cancel();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.RaiseManagedCompletion(exception);
            return true;
        }

        public void Reset()
        {
            Awaitable = Awaitable.NewManagedAwaitable();
            _state = 0;
        }
    }

    public class AwaitableCompletionSource<T>
    {
        volatile int _state; // 0: not raised, 1: raised or canceled
        public Awaitable<T> Awaitable { get; private set; } = Awaitable<T>.GetManaged();

        public void SetResult(in T value)
        {
            if (!TrySetResult(value)) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        public void SetCanceled()
        {
            if (!TrySetCanceled()) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        public void SetException(Exception exception)
        {
            if (!TrySetException(exception)) throw new InvalidOperationException("Can't raise completion of the same Awaitable twice");
        }

        bool CheckAndAcquireCompletionState() => Interlocked.CompareExchange(ref _state, 1, 0) == 0;

        public bool TrySetResult(in T value)
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.SetResultAndRaiseContinuation(value);
            return true;
        }

        public bool TrySetCanceled()
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.Cancel();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (!CheckAndAcquireCompletionState())
            {
                return false;
            }
            Awaitable.SetExceptionAndRaiseContinuation(exception);
            return true;
        }

        public void Reset()
        {
            Awaitable = Awaitable<T>.GetManaged();
            _state = 0;
        }
    }
}
