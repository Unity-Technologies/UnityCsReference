// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// Managed-side storage for ignored dictionary entries (e.g. duplicate keys, null-key placeholders) per (instance, property path).
    /// Outer key is the hosting object's entity id; inner key is the formatted property path for the dictionary field.
    /// Editor and Editor play mode only; not included in the player.
    ///
    /// Thread-safe: every public member acquires <see cref="m_Lock"/>. The dictionary serialization callbacks
    /// (<see cref="DictionarySerialization.SetEntriesFromSerializedData"/> and
    /// <see cref="DictionarySerialization.GetDictionaryEntriesForSerialization"/>) are reachable from worker
    /// threads through the native transfer pipeline, while editor cleanup
    /// (<see cref="DictionarySerialization.PruneIgnoredDictionaryEntriesForUnloadedHosts"/>) and the public
    /// <c>SerializedProperty.GetDictionaryDuplicateEntryIndices</c> API are invoked from the main thread.
    /// </summary>
    internal sealed class DictionaryIgnoredEntriesCache : IDictionaryIgnoredEntriesCache
    {
        private readonly object m_Lock = new object();
        private readonly Dictionary<EntityId, Dictionary<string, IgnoredEntriesData>> m_IgnoredEntriesByHost = new Dictionary<EntityId, Dictionary<string, IgnoredEntriesData>>();

        public bool HasAnyCachedHosts
        {
            get
            {
                lock (m_Lock)
                {
                    return m_IgnoredEntriesByHost.Count > 0;
                }
            }
        }

        public void Store(EntityId hostId, string dictionaryPath, IgnoredEntriesData ignoredEntriesData)
        {
            if (ignoredEntriesData.indices == null || ignoredEntriesData.indices.Length == 0
                || ignoredEntriesData.entries == null || ignoredEntriesData.entries.Length == 0)
                return;
            if (hostId == EntityId.None || string.IsNullOrEmpty(dictionaryPath))
                return;
            lock (m_Lock)
            {
                if (!m_IgnoredEntriesByHost.TryGetValue(hostId, out var inner))
                {
                    inner = new Dictionary<string, IgnoredEntriesData>();
                    m_IgnoredEntriesByHost[hostId] = inner;
                }
                inner[dictionaryPath] = ignoredEntriesData;
            }
        }

        public IgnoredEntriesData Get(EntityId hostId, string dictionaryPath)
        {
            if (hostId == EntityId.None || string.IsNullOrEmpty(dictionaryPath))
                return default;
            lock (m_Lock)
            {
                if (!m_IgnoredEntriesByHost.TryGetValue(hostId, out var inner))
                    return default;
                if (!inner.TryGetValue(dictionaryPath, out var data))
                    return default;
                return data;
            }
        }

        public void Clear(EntityId hostId, string dictionaryPath)
        {
            if (hostId == EntityId.None || string.IsNullOrEmpty(dictionaryPath))
                return;
            lock (m_Lock)
            {
                if (!m_IgnoredEntriesByHost.TryGetValue(hostId, out var inner))
                    return;
                inner.Remove(dictionaryPath);
                if (inner.Count == 0)
                    m_IgnoredEntriesByHost.Remove(hostId);
            }
        }

        // Main-thread only. Resources.IsInstanceLoaded resolves via Object::IDToPointer, which is the
        // non-thread-safe variant and DebugAsserts on the main thread (see Runtime/BaseClasses/BaseObject.h
        // and Runtime/BaseClasses/EntityIdStore.cpp::GetNativePtr). All current call sites
        // (ObjectChangeEvents.changesPublished, EditorSceneManager.sceneClosed, SceneManager.sceneUnloaded)
        // fire on the main thread.
        public int PruneUnloadedHosts()
        {
            // m_Lock is held across the whole pass, including the native IsInstanceLoaded calls. That is safe
            // here for two reasons:
            //   1. IsInstanceLoaded is lock-free on the native side -- it is a seqlock-style atomic read on
            //      the entity id slot, so no native mutex is acquired and there is no possibility of lock
            //      inversion against locks held by worker-thread serialization paths.
            //   2. The per-host cost is a few atomic loads, so the critical section stays in the microsecond
            //      range even for large caches and never approaches the worker-thread contention.
            // A small toRemove list is still required because C# disallows mutating a Dictionary while
            // enumerating it; it is not a snapshot of the full host set.
            lock (m_Lock)
            {
                if (m_IgnoredEntriesByHost.Count == 0)
                    return 0;

                List<EntityId> toRemove = null;
                foreach (EntityId hostId in m_IgnoredEntriesByHost.Keys)
                {
                    if (hostId == EntityId.None || !Resources.IsInstanceLoaded(hostId))
                    {
                        toRemove ??= new List<EntityId>();
                        toRemove.Add(hostId);
                    }
                }

                if (toRemove == null)
                    return 0;

                foreach (EntityId id in toRemove)
                    m_IgnoredEntriesByHost.Remove(id);
                return toRemove.Count;
            }
        }

        public bool HostHasIgnoredDictionaryEntries(EntityId hostId)
        {
            if (hostId == EntityId.None)
                return false;
            lock (m_Lock)
            {
                return m_IgnoredEntriesByHost.TryGetValue(hostId, out var inner) && inner.Count > 0;
            }
        }
    }
}
