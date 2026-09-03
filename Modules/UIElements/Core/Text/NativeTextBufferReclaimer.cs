// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.TextCore;

namespace UnityEngine.UIElements
{
    // NativeTextBuffer wraps a NativeArray<char> allocated with Allocator.Persistent. Disposing such an
    // allocation must happen on the main thread, so an object collected by the GC cannot free its buffer directly from the finalizer thread.
    static class NativeTextBufferReclaimer
    {
        [NoAutoStaticsCleanup] // holds pending Persistent allocations; clearing on reload would leak them before Collect() can dispose them
        static readonly ConcurrentQueue<NativeArray<char>> s_BuffersToFree = new();

        // Since this just about pre-emptively freeing up memory, and because we always try to free one node before
        // allocating a new one, we will limit the number of iterations per frame
        internal const int k_MaxIterationsPerCollect = 100;

        // Used in tests. Number of allocations still waiting to be freed.
        internal static int PendingDisposalCount => s_BuffersToFree.Count;

        static NativeTextBufferReclaimer()
        {
            UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.NativeTextBufferReclaimer, Shutdown);
        }

        /// <summary>
        /// Takes ownership of the buffer's backing allocation and queues it for disposal on the main thread.
        /// </summary>
        public static void EnqueueForDisposal(ref NativeTextBuffer buffer)
        {
            var array = buffer.ReleaseBuffer();
            if (array.IsCreated)
                s_BuffersToFree.Enqueue(array);
        }

        /// <summary>
        /// Frees a bounded number of queued allocations. Must be called on the main thread.
        /// </summary>
        public static void Collect()
        {
            int iterations = 0;
            while (iterations < k_MaxIterationsPerCollect && s_BuffersToFree.TryDequeue(out var array))
            {
                if (array.IsCreated)
                    array.Dispose();
                iterations++;
            }
        }

        /// <summary>
        /// Frees every queued allocation, without the per-frame iteration cap. Called on code unloading,
        /// as the last guaranteed main-thread collect before a domain reload.
        /// </summary>
        internal static void Shutdown()
        {
            while (s_BuffersToFree.TryDequeue(out var array))
            {
                if (array.IsCreated)
                    array.Dispose();
            }
        }
    }
}
