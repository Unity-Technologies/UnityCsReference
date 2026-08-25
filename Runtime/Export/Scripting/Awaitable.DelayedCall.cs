// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Mono/DelayedCallAwaitable.h")]
    public partial class Awaitable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ThrowIfNotMainThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != _mainThreadId)
            {
                throw new InvalidOperationException("This operation can only be performed on the main thread.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable NextFrameAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotMainThread();
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var awaitable = Awaitable.NewManagedAwaitable();
            _nextFrameAwaitables.Add(awaitable, Time.frameCount + 1);
            awaitable._managedCompletionQueue = _nextFrameAwaitables;
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(awaitable, cancellationToken);
            }
            return awaitable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable WaitForSecondsAsync(float seconds, CancellationToken cancellationToken = default)
        {
            ThrowIfNotMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = WaitForScondsInternal(seconds);
            return FromNativeAwaitableHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable FixedUpdateAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotMainThread();
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = FixedUpdateInternal();
            return FromNativeAwaitableHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable EndOfFrameAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfNotMainThread();
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var awaitable = Awaitable.NewManagedAwaitable();
            _endOfFrameAwaitables.Add(awaitable, -1);
            awaitable._managedCompletionQueue = _endOfFrameAwaitables;
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(awaitable, cancellationToken);
            }
            return awaitable;
        }


        private static bool _nextFrameAndEndOfFrameWiredUp = false;
        static CancellationTokenRegistration _nextFrameAndEndOfFrameWiredUpCTRegistration = default;
        static void EnsureDelayedCallWiredUp()
        {
            if (_nextFrameAndEndOfFrameWiredUp)
            {
                return;
            }
            _nextFrameAndEndOfFrameWiredUp = true;
            WireupNextFrameAndEndOfFrameCallbacks();
            _nextFrameAndEndOfFrameWiredUpCTRegistration = Application.exitCancellationToken.Register(OnDelayedCallManagerCleared);
        }

        [RequiredByNativeCode]
        static void OnDelayedCallManagerCleared()
        {
            _nextFrameAndEndOfFrameWiredUp = false;
            _nextFrameAwaitables.Clear();
            _endOfFrameAwaitables.Clear();
        }

        private static readonly DoubleBufferedAwaitableList _nextFrameAwaitables = new(keepsEditorPlayerLoopUpdating: true);
        private static readonly DoubleBufferedAwaitableList _endOfFrameAwaitables = new();

        private struct AwaitableAndFrameIndex
        {
            public Awaitable Awaitable { get; }
            public int FrameIndex { get; }

            public AwaitableAndFrameIndex(Awaitable awaitable, int frameIndex)
            {
                Awaitable = awaitable;
                FrameIndex = frameIndex;
            }
        }

        class DoubleBufferedAwaitableList
        {
            private List<AwaitableAndFrameIndex> _awaitables = new();
            private List<AwaitableAndFrameIndex> _scratch = new();
            private readonly bool _keepsEditorPlayerLoopUpdating;

            public DoubleBufferedAwaitableList(bool keepsEditorPlayerLoopUpdating = false)
            {
                _keepsEditorPlayerLoopUpdating = keepsEditorPlayerLoopUpdating;
            }

            // In edit mode the player loop only runs on demand; awaitables in this list are only
            // completed from the player loop, so the editor must be told to keep it running while
            // any are pending (UUM-148723).
            // Only the main thread notifies: Awaitable.Cancel() calls Remove from a CancellationToken
            // callback on whatever thread cancelled the token, and the native count is a plain int
            // read by the main-thread editor loop — a racing background-thread write could publish a
            // stale 0 with awaitables still pending, re-introducing the hang this fixes. A skipped
            // notification instead leaves the count stale but >0, which at worst runs one extra
            // player loop update whose SwapAndComplete publishes the correct count again.
            [Conditional("UNITY_EDITOR")]
            private void NotifyPendingAwaitableCount()
            {
                if (_keepsEditorPlayerLoopUpdating && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
                    SetPendingNextFrameAwaitableCount(_awaitables.Count);
            }

            public void SwapAndComplete()
            {
                var oldScratch = _scratch;
                var toIterate = _awaitables;
                _awaitables = oldScratch;
                _scratch = toIterate;
                try
                {
                    foreach (var item in toIterate)
                    {
                        if (!item.Awaitable.IsDettachedOrCompleted)
                        {
                            if (Time.frameCount >= item.FrameIndex || item.FrameIndex == -1)
                            {
                                item.Awaitable.RaiseManagedCompletion();
                            }
                            else
                            {
                                oldScratch.Add(item);
                            }
                        }
                    }
                }
                finally
                {
                    toIterate.Clear();
                    NotifyPendingAwaitableCount();
                }
            }

            public void Add(Awaitable item, int frameIndex)
            {
                _awaitables.Add(new AwaitableAndFrameIndex(item, frameIndex));
                NotifyPendingAwaitableCount();
            }
            public void Remove(Awaitable item)
            {
                _awaitables.RemoveAll(x => x.Awaitable == item);
                NotifyPendingAwaitableCount();
            }
            public void Clear()
            {
                _awaitables.Clear();
                NotifyPendingAwaitableCount();
            }
        }

        [RequiredByNativeCode]
        private static void OnUpdate()
        {
            _nextFrameAwaitables.SwapAndComplete();
        }

        [RequiredByNativeCode]
        private static void OnEndOfFrame()
        {
            _endOfFrameAwaitables.SwapAndComplete();
        }

        [FreeFunction("Scripting::Awaitables::NextFrameAwaitable")]
        private static extern IntPtr NextFrameInternal();
        [FreeFunction("Scripting::Awaitables::WaitForSecondsAwaitable")]
        private static extern IntPtr WaitForScondsInternal(float seconds);
        [FreeFunction("Scripting::Awaitables::FixedUpdateAwaitable")]
        private static extern IntPtr FixedUpdateInternal();
        [FreeFunction("Scripting::Awaitables::EndOfFrameAwaitable")]
        private static extern IntPtr EndOfFrameInternal();
        [FreeFunction("Scripting::Awaitables::WireupNextFrameAndEndOfFrameCallbacks")]
        private static extern void WireupNextFrameAndEndOfFrameCallbacks();
        [FreeFunction("Scripting::Awaitables::SetPendingNextFrameAwaitableCount")]
        private static extern void SetPendingNextFrameAwaitableCount(int count);
    }
}
