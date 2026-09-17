// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Non-generic face of <see cref="ManagedComponentRegistry{T}"/>, so the teardown path can reach a
    /// registry without knowing the component type.
    /// </summary>
    abstract class ManagedComponentRegistry
    {
        protected bool m_Disposed;

        public bool IsValid => !m_Disposed;

        // Takes a strong reference to every box still registered, so that a release hook running later in
        // the same teardown cannot let one be collected before the sweep reaches it.
        public abstract void PinLiveBoxes();

        // Runs the release hook on every box still registered, for components attached at code unload.
        public abstract void ReleaseLiveBoxes();

        public abstract void Dispose();
    }

    /// <summary>
    /// The live <see cref="ManagedComponentBox{T}"/> instances of one component type, held weakly.
    /// </summary>
    /// <remarks>
    /// Only the code-unload sweep reads this: it is the one service the removed process-wide managed store
    /// provided. Entries are weak because a strong one would root the element of any component that
    /// references it, which is the leak the store shipped with.
    ///
    /// While the manager is running, a registered box is always reachable — its element holds it, or the
    /// free-queue does between <c>~VisualElement</c> and the drain that recycles it — so a collected entry
    /// cannot arise, and removal needs no compaction pass and leaves no stale handle. Teardown is the one
    /// point where that does not hold, because a finalizer running then drops its slots without enqueueing,
    /// so <see cref="PinLiveBoxes"/> takes strong references before any hook runs. Rooting is harmless
    /// there and only there: the domain is going away, so nothing is being kept from a finalizer.
    ///
    /// Only a type that declares <c>[ReleaseComponentResources]</c> is tracked at all: for any other, the
    /// sweep would find nothing to run, so its instances take no entry and no handle.
    /// </remarks>
    sealed class ManagedComponentRegistry<T> : ManagedComponentRegistry where T : struct, IVisualElementComponent
    {
        readonly List<GCHandle> m_Entries = new();

        // Non-null only between PinLiveBoxes and Dispose, i.e. for the duration of a teardown.
        ManagedComponentBox<T>[] m_Pinned;

        public int Count => m_Entries.Count;

        public void Add(ManagedComponentBox<T> box)
        {
            // A type with no [ReleaseComponentResources] method has nothing for the sweep to run, so it
            // needs no entries at all — the same condition ReleaseLiveBoxes early-outs on. Reading the
            // handler here rather than caching it at construction keeps this independent of whether the
            // per-type cache has assigned it yet. Remove is a no-op for a box that was never added, so the
            // two sides cannot disagree.
            if (m_Disposed || ComponentManager<T>.ReleaseResourcesHandler == null)
                return;

            m_Entries.Add(GCHandle.Alloc(box, GCHandleType.Weak));
            box.registry = this;
            box.registryIndex = m_Entries.Count - 1;
        }

        public void Remove(ManagedComponentBox<T> box)
        {
            // registryIndex is -1 on a box Add skipped or already removed, which the unsigned compare rejects.
            var index = box.registryIndex;
            if (m_Disposed || (uint)index >= (uint)m_Entries.Count)
                return;

            m_Entries[index].Free();
            box.registry = null;
            box.registryIndex = -1;

            var last = m_Entries.Count - 1;
            if (index != last)
            {
                m_Entries[index] = m_Entries[last];

                // Alive to read: a registered box is held by its element or by the pending free-queue.
                if (m_Entries[index].Target is ManagedComponentBox<T> moved)
                    moved.registryIndex = index;
            }

            m_Entries.RemoveAt(last);
        }

        public override void PinLiveBoxes()
        {
            if (m_Disposed || m_Pinned != null || ComponentManager<T>.ReleaseResourcesHandler == null)
                return;

            var pinned = new ManagedComponentBox<T>[m_Entries.Count];
            for (var i = 0; i < pinned.Length; i++)
            {
                pinned[i] = m_Entries[i].Target as ManagedComponentBox<T>;
            }

            m_Pinned = pinned;
        }

        public override void ReleaseLiveBoxes()
        {
            if (m_Disposed)
                return;

            // Idempotent, so a caller that walks one type directly still pins before it reads.
            PinLiveBoxes();

            var pinned = m_Pinned;
            if (pinned == null)
                return;

            // Dropped as the sweep finishes, so this type is not left holding a stale snapshot. Types the
            // teardown has not swept yet keep theirs.
            m_Pinned = null;

            for (var i = 0; i < pinned.Length; i++)
            {
                var box = pinned[i];

                // Still registered here means still attached, so not already released. A component removed
                // before the teardown began is pinned and queued at once, and the drain releases it, recycles
                // it and — through the pool's reset — clears the flag that would otherwise stop this walk
                // running its hook a second time, on the reset value.
                if (box == null || box.registry != this)
                    continue;

                try
                {
                    // Releases once: the free-queue may also reach this box after the walk.
                    box.ReleaseResources();
                }
                catch (Exception e)
                {
                    // The walk is the last chance to run these hooks, so one component's failure cannot end it.
                    Debug.LogException(e);
                }
            }
        }

        public override void Dispose()
        {
            if (m_Disposed)
                return;

            for (var i = 0; i < m_Entries.Count; i++)
            {
                m_Entries[i].Free();
            }

            m_Entries.Clear();
            m_Pinned = null;
            m_Disposed = true;
        }
    }
}
