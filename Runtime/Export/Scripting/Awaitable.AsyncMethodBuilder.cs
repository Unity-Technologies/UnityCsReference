// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.Internal;
using UnityEngine.Pool;

namespace UnityEngine
{
    public partial class Awaitable
    {
        [ExcludeFromDocs]
        public struct AwaitableAsyncMethodBuilder
        {
            private interface IStateMachineBox : IDisposable
            {
                Awaitable ResultingCoroutine { get; set; }
                Action MoveNext { get; }
            }
            private class StateMachineBox<TStateMachine> : IStateMachineBox where TStateMachine : IAsyncStateMachine
            {
                static readonly ThreadLocal<ObjectPool<StateMachineBox<TStateMachine>>> _pool =
                    new(() => new ObjectPool<StateMachineBox<TStateMachine>>(() => new(), collectionCheck: false));
                public static StateMachineBox<TStateMachine> GetOne() => _pool.Value.Get();
                public void Dispose()
                {
                    // reset the statemachine to avoid keeping any reference through it
                    // set resulting corouting to null, to avoid reusing an instance that has been reset to the pool on continuation
                    // put the box back to the pool
                    StateMachine = default;
                    ResultingCoroutine = null;
                    _pool.Value.Release(this);
                }

                public TStateMachine StateMachine { get; set; }
                private void DoMoveNext()
                {
                    StateMachine.MoveNext();
                }
                public Action MoveNext { get; }
                public StateMachineBox()
                {
                    MoveNext = DoMoveNext;
                }

                public Awaitable ResultingCoroutine { get; set; }
            }

            IStateMachineBox _stateMachineBox;

            IStateMachineBox EnsureStateMachineBox<TStateMachine>() where TStateMachine : IAsyncStateMachine
            {
                if (_stateMachineBox != null)
                {
                    return _stateMachineBox;
                }
                _stateMachineBox = StateMachineBox<TStateMachine>.GetOne();
                _stateMachineBox.ResultingCoroutine = _resultingCoroutine;
                return _stateMachineBox;
            }
            public static AwaitableAsyncMethodBuilder Create() => default;

            Awaitable _resultingCoroutine;
            public Awaitable Task
            {
                get
                {
                    if (_resultingCoroutine != null)
                    {
                        return _resultingCoroutine;
                    }
                    if (_stateMachineBox != null)
                    {
                        return _resultingCoroutine = _stateMachineBox.ResultingCoroutine ??= Awaitable.NewManagedAwaitable();
                    }
                    return _resultingCoroutine = Awaitable.NewManagedAwaitable();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
            {
                var box = EnsureStateMachineBox<TStateMachine>();
                ((StateMachineBox<TStateMachine>)box).StateMachine = stateMachine;
                stateMachine.MoveNext();
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetStateMachine(IAsyncStateMachine stateMachine)
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
            {
                var box = EnsureStateMachineBox<TStateMachine>();
                ((StateMachineBox<TStateMachine>)box).StateMachine = stateMachine;
                awaiter.OnCompleted(box.MoveNext);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
                where TAwaiter : ICriticalNotifyCompletion
                where TStateMachine : IAsyncStateMachine
            {
                var box = EnsureStateMachineBox<TStateMachine>();
                ((StateMachineBox<TStateMachine>)box).StateMachine = stateMachine;
                awaiter.UnsafeOnCompleted(box.MoveNext);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetException(Exception e)
            {
                Task.RaiseManagedCompletion(e);
                _stateMachineBox.Dispose();
                _stateMachineBox = null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetResult()
            {
                Task.RaiseManagedCompletion();
                _stateMachineBox.Dispose();
                _stateMachineBox = null;
            }
        }
    }
}
