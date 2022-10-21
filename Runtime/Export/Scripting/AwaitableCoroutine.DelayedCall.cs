// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Runtime/Mono/DelayedCallCoroutine.h")]
    public partial class AwaitableCoroutine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine NextFrameAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var coroutine = AwaitableCoroutine.NewManagedCoroutine();
            _nextFrameCoroutines.Add(coroutine);
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(coroutine, cancellationToken);
            }
            return coroutine;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine WaitForSecondsAsync(float seconds, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = WaitForScondsInternal(seconds);
            return FromNativeCoroutineHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine FixedUpdateAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ptr = FixedUpdateInternal();
            return FromNativeCoroutineHandle(ptr, cancellationToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AwaitableCoroutine EndOfFrameAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsureDelayedCallWiredUp();
            var coroutine = AwaitableCoroutine.NewManagedCoroutine();
            _endOfFrameCoroutines.Add(coroutine);
            if (cancellationToken.CanBeCanceled)
            {
                WireupCancellation(coroutine, cancellationToken);
            }
            return coroutine;
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
            _nextFrameCoroutines.Clear();
            _endOfFrameCoroutines.Clear();
        }

        private static readonly DoubleBufferedAwaitableList _nextFrameCoroutines = new();
        private static readonly DoubleBufferedAwaitableList _endOfFrameCoroutines = new();

        class DoubleBufferedAwaitableList
        {
            private List<AwaitableCoroutine> _coroutines = new();
            private List<AwaitableCoroutine> _scratch = new();
            public void SwapAndComplete()
            {
                var oldScratch = _scratch;
                var toIterate = _coroutines;
                _coroutines = oldScratch;
                _scratch = toIterate;
                try
                {
                    foreach (var item in toIterate)
                    {
                        if (!item.IsDettachedOrCompleted) // might already have been completed
                            item.RaiseManagedCompletion();
                    }
                }
                finally
                {
                    toIterate.Clear();
                }
            }

            public void Add(AwaitableCoroutine item)
            {
                _coroutines.Add(item);
            }
            public void Clear()
            {
                _coroutines.Clear();
            }
        }

        [RequiredByNativeCode]
        private static void OnUpdate()
        {
            _nextFrameCoroutines.SwapAndComplete();
        }

        [RequiredByNativeCode]
        private static void OnEndOfFrame()
        {
            _endOfFrameCoroutines.SwapAndComplete();
        }

        [FreeFunction("Scripting::AwaitableCoroutines::NextFrameCoroutine")]
        private static extern IntPtr NextFrameInternal();
        [FreeFunction("Scripting::AwaitableCoroutines::WaitForSecondsCoroutine")]
        private static extern IntPtr WaitForScondsInternal(float seconds);
        [FreeFunction("Scripting::AwaitableCoroutines::FixedUpdateCoroutine")]
        private static extern IntPtr FixedUpdateInternal();
        [FreeFunction("Scripting::AwaitableCoroutines::EndOfFrameCoroutine")]
        private static extern IntPtr EndOfFrameInternal();
        [FreeFunction("Scripting::AwaitableCoroutines::WireupNextFrameAndEndOfFrameCallbacks")]
        private static extern void WireupNextFrameAndEndOfFrameCallbacks();
    }
}
