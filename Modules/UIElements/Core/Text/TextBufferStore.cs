// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.TextCore;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements
{
    struct TextBufferData
    {
        // Source text, exposed as TextElement.textBuffer.
        public NativeTextBuffer buffer;

        // Processed text when it differs from the source (password-masked, placeholder, elided),
        // exposed as TextElement.processedTextBuffer and written by UITKTextHandle.
        public NativeTextBuffer processedBuffer;
    }

    /// <summary>
    /// Owns the native text buffers of every <see cref="TextElement"/> in a sparse
    /// <see cref="UnmanagedDataStore"/>, so they are always freed on the main thread and never
    /// depend on finalizers: explicit release frees the slot immediately, garbage-collected
    /// elements enqueue their handle for <see cref="Collect"/>, and <see cref="Shutdown"/> walks
    /// the live slots before code unload, ahead of the native leak check.
    /// </summary>
    partial class TextBufferStore
    {
        enum SharedManagerState
        {
            Uninitialized, // The SharedManager was not accessed yet
            Initialized, // The SharedManager was accessed and created
            Shutdown // The SharedManager was disposed and must not be re-created
        }

        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Initialization gate re-evaluated on demand by SharedManager after cleanup resets it")]
        static SharedManagerState s_Initialized;

        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Shared instance recreated on demand by Initialize() after cleanup nulls it")]
        static TextBufferStore s_SharedInstance = null;

        public static bool IsSharedManagerCreated => s_Initialized == SharedManagerState.Initialized;

        public static TextBufferStore SharedManager
        {
            get
            {
                Initialize();
                return s_SharedInstance;
            }
        }

        // Same ordering assumptions as LayoutManager: the first call is always main-thread (a
        // VisualElement cannot be created on another thread), and Shutdown only runs after it.
        static void Initialize()
        {
            if (s_Initialized != SharedManagerState.Uninitialized)
                return;

            s_Initialized = SharedManagerState.Initialized;
            s_SharedInstance = new TextBufferStore();
            UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.TextBufferStore, Shutdown);
        }

        static void Shutdown()
        {
            if (s_Initialized != SharedManagerState.Initialized)
                return;

            s_Initialized = SharedManagerState.Shutdown;

            s_SharedInstance.Dispose();
            s_SharedInstance = null;
        }

        const int k_InitialCapacity = 32;

        // Bounds the per-frame cost of Collect; Allocate frees one pending slot per allocation, so
        // the queue cannot grow unbounded under churn.
        internal const int k_MaxFreesPerCollect = 100;

        UnmanagedDataStore m_Store;

        readonly ConcurrentQueue<UnmanagedDataHandle> m_ToFree = new();

        // Last allocated index in the store, bounding the Dispose walk.
        int m_HighMark = -1;

        // Used in tests. Number of slots still waiting to be freed.
        internal int PendingDisposalCount => m_ToFree.Count;

        // Used in tests. Mutated on the main thread only: incremented by Allocate, decremented when
        // a slot is actually freed.
        internal int LiveCount { get; private set; }

        unsafe TextBufferStore()
        {
            var componentTypes = new[] { UnmanagedComponentType.Create<TextBufferData>() };
            var componentLabels = new[] { new MemoryLabel(nameof(UIElements), $"Text.{nameof(TextBufferData)}") };
            var initial = default(TextBufferData);
            var initialData = stackalloc byte*[1];
            initialData[0] = (byte*)&initial;

            m_Store = new UnmanagedDataStore(componentTypes, componentLabels, initialData, k_InitialCapacity, Allocator.Persistent, "TextBuffer");
        }

        public UnmanagedDataHandle Allocate()
        {
            if (m_ToFree.TryDequeue(out var pending))
                FreeSlot(pending);

            var handle = m_Store.Allocate();

            if (handle.Index > m_HighMark)
                m_HighMark = handle.Index;

            LiveCount++;
            return handle;
        }

        public bool Exists(in UnmanagedDataHandle handle) => m_Store.Exists(handle);

        public unsafe ref TextBufferData GetRef(in UnmanagedDataHandle handle)
        {
            return ref *(TextBufferData*)m_Store.GetComponentDataPtr(handle.Index, 0);
        }

        /// <summary>
        /// Queues a slot for freeing on the main thread. Thread-safe: the only member a finalizer
        /// may call.
        /// </summary>
        public void EnqueueForDisposal(in UnmanagedDataHandle handle)
        {
            m_ToFree.Enqueue(handle);
        }

        /// <summary>
        /// Frees a slot and its buffers. Must be called on the main thread. Stale handles (already
        /// freed, or from before a store re-creation) are ignored.
        /// </summary>
        public void FreeSlot(in UnmanagedDataHandle handle)
        {
            if (!m_Store.Exists(handle))
                return;

            ref var data = ref GetRef(handle);
            data.buffer.Dispose();
            data.processedBuffer.Dispose();

            m_Store.Free(handle);
            LiveCount--;
        }

        /// <summary>
        /// Frees a bounded number of queued slots. Must be called on the main thread.
        /// </summary>
        public void Collect()
        {
            var iterations = 0;
            while (iterations < k_MaxFreesPerCollect && m_ToFree.TryDequeue(out var handle))
            {
                FreeSlot(handle);
                iterations++;
            }
        }

        unsafe void Dispose()
        {
            // Queued handles still designate allocated slots, so the walk covers them; entries left
            // in the queue afterwards die with this instance.
            for (var i = 0; i <= m_HighMark; i++)
            {
                if (m_Store.IsFree(i))
                    continue;

                var data = (TextBufferData*)m_Store.GetComponentDataPtr(i, 0);
                data->buffer.Dispose();
                data->processedBuffer.Dispose();
            }

            m_Store.Dispose();
        }
    }
}
