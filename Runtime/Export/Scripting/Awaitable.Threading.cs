// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine
{
    partial class Awaitable
    {
        static SynchronizationContext _synchronizationContext;
        static int _mainThreadId;
        internal static void SetSynchronizationContext(UnitySynchronizationContext synchronizationContext) {
            _synchronizationContext = synchronizationContext;
            _mainThreadId = synchronizationContext.MainThreadId;
        }

        public static MainThreadAwaitable MainThreadAsync()
        {
            return new MainThreadAwaitable(_synchronizationContext, _mainThreadId);
        }

        public static BackgroundThreadAwaitable BackgroundThreadAsync()
        {
            return new BackgroundThreadAwaitable(_synchronizationContext, _mainThreadId);
        }
    }

    [Internal.ExcludeFromDocs]
    public struct MainThreadAwaitable : INotifyCompletion
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly int _mainThreadId;

        internal MainThreadAwaitable(SynchronizationContext syncContext, int mainThreadId)
        {
            _synchronizationContext = syncContext;
            _mainThreadId = mainThreadId;
        }

        public MainThreadAwaitable GetAwaiter() => this;
        public bool IsCompleted => Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() {
        }

        public void OnCompleted(Action continuation)
        {
            _synchronizationContext.Post(DoOnCompleted, continuation);
        }

        static void DoOnCompleted(object continuation)
        {
            (continuation as Action)?.Invoke();
        }
    }

    [Internal.ExcludeFromDocs]
    public struct BackgroundThreadAwaitable : INotifyCompletion
    {
        private readonly SynchronizationContext _synchronizationContext;
        private readonly int _mainThreadId;

        internal BackgroundThreadAwaitable(SynchronizationContext syncContext, int mainThreadId)
        {
            _synchronizationContext = syncContext;
            _mainThreadId = mainThreadId;
        }

        public BackgroundThreadAwaitable GetAwaiter() => this;
        public bool IsCompleted => Thread.CurrentThread.ManagedThreadId != _mainThreadId;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() {
        }

        public void OnCompleted(Action continuation)
        {
            Task.Run(continuation);
        }
    }
}
