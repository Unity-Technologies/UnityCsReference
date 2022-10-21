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
    - Can be awaited only once: a given AwaitableCoroutine object can't be awaited by multiple async functions. This is considered undefined behaviour
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
    There is Scripting::AwaitableCoroutines::AwaitableCoroutine base class providing interop with this managed coroutine type, that can be extended so that
    Native asynchronous code can be exposed as an await-compatible type in user code.
    See AsyncOperationCoroutine implementation as an example
     * */
    [AsyncMethodBuilder(typeof(AwaitableCoroutineAsyncMethodBuilder))]
    public partial class AwaitableCoroutine : IEnumerator
    {
        readonly struct CoroutineHandle
        {
            private readonly IntPtr _handle;
            public bool IsNull => _handle == IntPtr.Zero;
            public bool IsManaged => _handle == ManagedHandle._handle;
            public CoroutineHandle(IntPtr handle) => _handle = handle;

            public static CoroutineHandle ManagedHandle = new CoroutineHandle(new IntPtr(-1));
            public static CoroutineHandle NullHandle = new CoroutineHandle(IntPtr.Zero);
            public static implicit operator IntPtr(CoroutineHandle handle) => handle._handle;
            public static implicit operator CoroutineHandle(IntPtr handle) => new CoroutineHandle(handle);
        }

        private readonly object _syncRoot = new();

        // on discarding a coroutine object, if it has a GC handle allocated, we deallocate it to avoid leaking the object
        static readonly ThreadSafeObjectPool<AwaitableCoroutine> _pool = new(() => new());
        CoroutineHandle _handle;
        ExceptionDispatchInfo _exceptionToRethrow;
        bool _managedCoroutineDone;
        Action _continuation;
        CancellationTokenRegistration? _cancelTokenRegistration;
        private AwaitableCoroutine() { }

        internal static AwaitableCoroutine NewManagedCoroutine()
        {
            var coroutine = _pool.Get();
            coroutine._handle = CoroutineHandle.ManagedHandle;
            return coroutine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static AwaitableCoroutine FromNativeCoroutineHandle(IntPtr nativeHandle, CancellationToken cancellationToken)
        {
            var coroutine = _pool.Get();
            coroutine._handle = nativeHandle;
            unsafe
            {
                AttachManagedGCHandleToNativeCoroutine(nativeHandle, (UIntPtr)(void*)GCHandle.ToIntPtr(GCHandle.Alloc(coroutine)));
            }
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(coroutine, cancellationToken);
            }
            return coroutine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WireupCancellation(AwaitableCoroutine coroutine, CancellationToken cancellationToken)
        {
            coroutine._cancelTokenRegistration = cancellationToken.Register(coroutine => ((AwaitableCoroutine)coroutine).Cancel(), coroutine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseManagedCompletion(Exception exception)
        {
            lock (_syncRoot)
            {
                if (exception != null)
                {
                    _exceptionToRethrow = ExceptionDispatchInfo.Capture(exception);
                }
                _managedCoroutineDone = true;
                _continuation?.Invoke();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RaiseManagedCompletion()
        {
            lock (_syncRoot)
            {
                _managedCoroutineDone = true;
                _continuation?.Invoke();
            }
        }

        internal void PropagateExceptionAndRelease()
        {
            lock (_syncRoot)
            {
                CheckPointerValidity();
                if (_cancelTokenRegistration.HasValue)
                {
                    _cancelTokenRegistration.Value.Dispose();
                    _cancelTokenRegistration = null;
                }
                _managedCoroutineDone = false;
                var ptr = _handle;
                _handle = CoroutineHandle.NullHandle;
                var toRethrow = _exceptionToRethrow;
                _exceptionToRethrow = null;
                _continuation = null;
                if (!ptr.IsManaged && !ptr.IsNull)
                {
                    ReleaseNativeCoroutine(ptr);
                }
                _pool.Release(this);
                toRethrow?.Throw();
            }
        }

        public void Cancel()
        {
            lock (_syncRoot)
            {
                CheckPointerValidity();
                if (_handle.IsManaged)
                {
                    RaiseManagedCompletion(new OperationCanceledException());
                }
                else
                {
                    CancelNativeCoroutine(_handle);
                }
            }
        }

        public bool IsCompleted
        {
            get
            {
                lock (_syncRoot)
                {
                    CheckPointerValidity();
                    if (_handle.IsManaged)
                    {
                        return _managedCoroutineDone;
                    }
                    return IsNativeCoroutineCompleted(_handle) != 0;
                }
            }
        }

        internal bool IsDettachedOrCompleted
        {
            get
            {
                lock (_syncRoot)
                {
                    return _handle.IsNull || IsCompleted;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckPointerValidity()
        {
            if (_handle.IsNull)
            {
                throw new InvalidOperationException("Coroutine is in detached state");
            }
        }


        internal void SetContinuation(Action continuation)
        {
            lock (_syncRoot)
            {
                _continuation = continuation;
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
