// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    [ExcludeFromDocs]
    public struct AwaitableCoroutineAsyncMethodBuilder
    {
        private interface IStateMachineBox : IDisposable
        {
            AwaitableCoroutine ResultingCoroutine { get; set; }
            Action MoveNext { get; }
        }
        private class StateMachineBox<TStateMachine> : IStateMachineBox where TStateMachine : IAsyncStateMachine
        {
            static readonly ThreadSafeObjectPool<StateMachineBox<TStateMachine>> _pool = new (()=>new());
            public static StateMachineBox<TStateMachine> GetOne() => _pool.Get();
            public void Dispose()
            {
                // reset the statemachine to avoid keeping any reference through it
                // set resulting corouting to null, to avoid reusing an instance that has been reset to the pool on continuation
                // put the box back to the pool
                StateMachine = default;
                ResultingCoroutine = null;
                _pool.Release(this);                
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

            public AwaitableCoroutine ResultingCoroutine { get; set; }
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
        public static AwaitableCoroutineAsyncMethodBuilder Create() => default;

        AwaitableCoroutine _resultingCoroutine;
        public AwaitableCoroutine Task
        {
            get
            {
                if(_resultingCoroutine != null)
                {
                    return _resultingCoroutine;
                }
                if(_stateMachineBox != null)
                {
                    return _resultingCoroutine = _stateMachineBox.ResultingCoroutine ??= AwaitableCoroutine.NewManagedCoroutine();
                }
                return _resultingCoroutine = AwaitableCoroutine.NewManagedCoroutine();
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
