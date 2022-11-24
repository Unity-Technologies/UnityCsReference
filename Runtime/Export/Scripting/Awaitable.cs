// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnityEngine
{
    /* Awaitable Coroutines are async/await compatible types specially tailored for running in Unity. (they can be awaited, and they can be used as return type
     * of an async method)
     * Compared to System.Threading.Tasks it is much less smart and takes shortcuts to improve performance based on
     * Unity specific assumption.
     * Main differences compared to .net tasks:
    - Can be awaited only once: a given Awaitable object can't be awaited by multiple async functions. This is considered undefined behaviour
    (considering it undefined behaviour allows for optimizations non-achievable otherwise)
    - Never capture an ExecutionContext: for security purposes, .net Tasks capture execution contexts on awaiting to be able to handle things like
    propagating impersonnation contexts accross async calls. We don't want to support that and can elliminate associated costs
    - Never capture Synchronization context:
    Coroutines continuations are run synchronously from the code raising the completion. In most cases, it will be from the Unity main frame (eg. for Delayed Call Coroutines and existing AsyncOperations)
    Methods returning awaitable coroutines with completion being raised in a background thread will need to mention it explicitly in their documentation
    - Awaiter.GetResults won't block until completion. Calling GetResults() before the operation is completed is an undefined behaviour
    - Are pooled object:
    Those are reference types, so that they can be referenced accross different stacks, efficiently copied etc, but to avoid too many allocations,
    they are pooled. ObjectPool has been enhanced a bit to account to avoid Stack<T> bounds checks with common get/release patterns produced by async state machines
    - Can be implemented in either managed or native code:
    There is Scripting::Awaitables::Awaitable base class providing interop with this managed coroutine type, that can be extended so that
    Native asynchronous code can be exposed as an await-compatible type in user code.
    See AsyncOperationCoroutine implementation as an example
     * */
    [AsyncMethodBuilder(typeof(AwaitableAsyncMethodBuilder))]
    public partial class Awaitable : IEnumerator
    {
        readonly struct AwaitableHandle
        {
            private readonly IntPtr _handle;
            public bool IsNull => _handle == IntPtr.Zero;
            public bool IsManaged => _handle == ManagedHandle._handle;
            public AwaitableHandle(IntPtr handle) => _handle = handle;

            public static AwaitableHandle ManagedHandle = new AwaitableHandle(new IntPtr(-1));
            public static AwaitableHandle NullHandle = new AwaitableHandle(IntPtr.Zero);
            public static implicit operator IntPtr(AwaitableHandle handle) => handle._handle;
            public static implicit operator AwaitableHandle(IntPtr handle) => new AwaitableHandle(handle);
        }

        private readonly ManagedLockWithSingleThreadBias _spinLock = default;

        static readonly ThreadSafeObjectPool<Awaitable> _pool = new(() => new());
        AwaitableHandle _handle;
        ExceptionDispatchInfo _exceptionToRethrow;
        bool _managedAwaitableDone;
        Action _continuation;
        CancellationTokenRegistration? _cancelTokenRegistration;
        private Awaitable() { }

        internal static Awaitable NewManagedAwaitable()
        {
            var awaitable = _pool.Get();
            awaitable._handle = AwaitableHandle.ManagedHandle;
            return awaitable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Awaitable FromNativeAwaitableHandle(IntPtr nativeHandle, CancellationToken cancellationToken)
        {
            var awaitable = _pool.Get();
            awaitable._handle = nativeHandle;
            unsafe
            {
                AttachManagedGCHandleToNativeAwaitable(nativeHandle, (UIntPtr)(void*)GCHandle.ToIntPtr(GCHandle.Alloc(awaitable)));
            }
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(awaitable, cancellationToken);
            }
            return awaitable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WireupCancellation(Awaitable awaitable, CancellationToken cancellationToken)
        {
            if (awaitable == null)
            {
                throw new ArgumentNullException(nameof(awaitable));
            }
            try
            {
                awaitable._spinLock.Acquire();
                awaitable._cancelTokenRegistration = cancellationToken.Register(coroutine => ((Awaitable)coroutine).Cancel(), awaitable);
            }
            finally
            {
                awaitable._spinLock.Release();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseManagedCompletion(Exception exception)
        {
            Action continuation = null;
            try
            {
                _spinLock.Acquire();
                if (exception != null)
                {
                    _exceptionToRethrow = ExceptionDispatchInfo.Capture(exception);
                }
                _managedAwaitableDone = true;
                continuation = _continuation;
                _continuation = null;
            }
            finally
            {
                _spinLock.Release();
            }
            continuation?.Invoke();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseManagedCompletion()
        {
            Action continuation = null;
            try
            {
                _spinLock.Acquire();
                _managedAwaitableDone = true;
                continuation = _continuation;
                _continuation = null;
            }
            finally
            {
                _spinLock.Release();
            }
            continuation?.Invoke();
        }

        internal void PropagateExceptionAndRelease()
        {
            try
            {
                _spinLock.Acquire();
                CheckPointerValidity();
                if (_cancelTokenRegistration.HasValue)
                {
                    _cancelTokenRegistration.Value.Dispose();
                    _cancelTokenRegistration = null;
                }
                _managedAwaitableDone = false;
                var ptr = _handle;
                _handle = AwaitableHandle.NullHandle;
                var toRethrow = _exceptionToRethrow;
                _exceptionToRethrow = null;
                _continuation = null;
                if (!ptr.IsManaged && !ptr.IsNull)
                {
                    ReleaseNativeAwaitable(ptr);
                }
                _pool.Release(this);
                toRethrow?.Throw();
            }
            finally
            {
                _spinLock.Release();
            }
        }

        public void Cancel()
        {
            var handle = CheckPointerValidity();
            if (handle.IsManaged)
            {
                RaiseManagedCompletion(new OperationCanceledException());
            }
            else
            {
                CancelNativeAwaitable(handle);
            }
        }

        private bool IsCompletedNoLock
        {
            get
            {
                CheckPointerValidity();
                if (_handle.IsManaged)
                {
                    return _managedAwaitableDone;
                }
                return IsNativeAwaitableCompleted(_handle) != 0;
            }
        }
        public bool IsCompleted
        {
            get
            {
                try
                {
                    _spinLock.Acquire();
                    return IsCompletedNoLock;
                }
                finally
                {
                    _spinLock.Release();
                }
            }
        }

        internal bool IsDettachedOrCompleted
        {
            get
            {
                try
                {
                    _spinLock.Acquire();
                    if (_handle.IsNull)
                    {
                        return true;
                    }
                    CheckPointerValidity();
                    if (_handle.IsManaged)
                    {
                        return _managedAwaitableDone;
                    }
                    return IsNativeAwaitableCompleted(_handle) != 0;
                }
                finally
                {
                    _spinLock.Release();
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private AwaitableHandle CheckPointerValidity()
        {
            var handle = _handle;
            if (handle.IsNull)
            {
                throw new InvalidOperationException("Awaitable is in detached state");
            }
            return handle;
        }


        internal void SetContinuation(Action continuation)
        {
            bool done = false;
            try
            {
                _spinLock.Acquire();
                if (IsCompletedNoLock)
                {
                    done = true;
                }
                else
                {
                    _continuation = continuation;
                }
            }
            finally
            {
                _spinLock.Release();
            }
            if (done)
            {
                continuation();
            }
        }

        // legacy coroutine interop

        bool IEnumerator.MoveNext()
        {
            if (IsCompleted)
            {
                RaiseManagedCompletion();
                return false;
            }
            return true;
        }

        void IEnumerator.Reset()
        {
        }

        object IEnumerator.Current => null;
    }
}
