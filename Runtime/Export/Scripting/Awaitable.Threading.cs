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
        internal static void SetSynchronizationContext(SynchronizationContext synchronizationContext) {
            _synchronizationContext = synchronizationContext;
        }

        public static SynchronizationContext MainThreadSynchronizationContext => _synchronizationContext;

        public static MainThreadAwaitable MainThreadAsync()
        {
            return new MainThreadAwaitable(_synchronizationContext);
        }

        public static BackgroundThreadAwaitable BackgroundThreadAsync()
        {
            return new BackgroundThreadAwaitable(_synchronizationContext);
        }
    }

    public static class AwaitableExtension
    {
        public static async Awaitable<T> OnBackgroundThread<T>(this Task<T> task)
        {
            var result = await task;
            await Awaitable.BackgroundThreadAsync();
            return result;
        }

        public static async Awaitable<T> OnMainThread<T>(this Task<T> task)
        {
            var result = await task;
            await Awaitable.MainThreadAsync();
            return result;
        }

        public static async Awaitable<T> OnEndOfFrame<T>(this Task<T> task,CancellationToken cancellationToken = default)
        {
            var result = await task;
            await Awaitable.EndOfFrameAsync(cancellationToken);
            return result;
        }

        public static async Awaitable<T> OnFixedUpdate<T>(this Task<T> task,CancellationToken cancellationToken = default)
        {
            var result = await task;
            await Awaitable.FixedUpdateAsync(cancellationToken);
            return result;
        }

        public static async Awaitable<T> OnNextFrame<T>(this Task<T> task,CancellationToken cancellationToken = default)
        {
            var result = await task;
            await Awaitable.NextFrameAsync(cancellationToken);
            return result;
        }

        public static async Awaitable OnBackgroundThread(this Task task)
        {
            await task;
            await Awaitable.BackgroundThreadAsync();
        }

        public static async Awaitable OnMainThread(this Task task)
        {
            await task;
            await Awaitable.MainThreadAsync();
        }

        public static async Awaitable OnEndOfFrame(this Task task,CancellationToken cancellationToken = default)
        {
            await task;
            await Awaitable.EndOfFrameAsync(cancellationToken);
        }

        public static async Awaitable OnFixedUpdate(this Task task,CancellationToken cancellationToken = default)
        {
            await task;
            await Awaitable.FixedUpdateAsync(cancellationToken);
        }

        public static async Awaitable OnNextFrame(this Task task,CancellationToken cancellationToken = default)
        {
            await task;
            await Awaitable.NextFrameAsync(cancellationToken);
        }
    }

    [Internal.ExcludeFromDocs]
    public struct MainThreadAwaitable : INotifyCompletion
    {
        private readonly SynchronizationContext _synchronizationContext;
        internal MainThreadAwaitable(SynchronizationContext syncContext)
        {
            _synchronizationContext = syncContext;
        }

        public MainThreadAwaitable GetAwaiter() => this;
        public bool IsCompleted => _synchronizationContext == SynchronizationContext.Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() { }

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
        internal BackgroundThreadAwaitable(SynchronizationContext syncContext)
        {
            _synchronizationContext = syncContext;
        }

        public BackgroundThreadAwaitable GetAwaiter() => this;
        public bool IsCompleted => _synchronizationContext != SynchronizationContext.Current;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult() { }

        public void OnCompleted(Action continuation)
        {
            Task.Run(continuation);
        }
    }
}
