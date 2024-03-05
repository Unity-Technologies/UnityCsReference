// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        public static Awaitable NextFrameAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var awaitable = Awaitable.NewManagedAwaitable();
            _nextFrameAwaitables.Add(awaitable, Time.frameCount + 1);
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(awaitable, cancellationToken);
            }
            return awaitable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable WaitForSecondsAsync(float seconds, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = WaitForScondsInternal(seconds);
            return FromNativeAwaitableHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable FixedUpdateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = FixedUpdateInternal();
            return FromNativeAwaitableHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Awaitable EndOfFrameAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var awaitable = Awaitable.NewManagedAwaitable();
            _endOfFrameAwaitables.Add(awaitable, -1);
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

        private static readonly DoubleBufferedAwaitableList _nextFrameAwaitables = new();
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
                }
            }

            public void Add(Awaitable item, int frameIndex)
            {
                _awaitables.Add(new AwaitableAndFrameIndex(item, frameIndex));
            }
            public void Clear()
            {
                _awaitables.Clear();
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
    }
}
