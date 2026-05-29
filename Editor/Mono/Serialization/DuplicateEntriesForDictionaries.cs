// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    /// <summary>
    /// Managed-side storage for duplicate dictionary entries (e.g. duplicate keys) per (instance, property path).
    /// Outer key is the hosting object's entity id; inner key is the formatted property path for the dictionary field.
    /// Editor and Editor play mode only; not included in the player.
    ///
    /// Thread-safe: every public member acquires <see cref="m_Lock"/>. The dictionary serialization callbacks
    /// (<see cref="DictionarySerialization.SetEntriesFromSerializedData"/> and
    /// <see cref="DictionarySerialization.GetDictionaryEntriesForSerialization"/>) are reachable from worker
    /// threads through the native transfer pipeline, while editor cleanup
    /// (<see cref="DictionarySerialization.PruneDuplicateDictionaryEntriesWhere"/>) and the public
    /// <c>SerializedProperty.GetDictionaryDuplicateEntryIndices</c> API are invoked from the main thread.
    /// </summary>
    internal sealed class DuplicateEntriesForDictionaries : IDuplicateEntriesForDictionaries
    {
        private readonly object m_Lock = new object();
        private readonly Dictionary<EntityId, Dictionary<string, DuplicateEntriesData>> m_DuplicateEntriesByHost = new Dictionary<EntityId, Dictionary<string, DuplicateEntriesData>>();

        public bool HasAnyCachedHosts
        {
            get
            {
                lock (m_Lock)
                {
                    return m_DuplicateEntriesByHost.Count > 0;
                }
            }
        }

        public void Store(EntityId hostId, string dictionaryPath, DuplicateEntriesData duplicateEntriesData)
        {
            if (duplicateEntriesData.indices == null || duplicateEntriesData.indices.Length == 0
                || duplicateEntriesData.entries == null || duplicateEntriesData.entries.Length == 0)
                return;
            if (hostId == EntityId.None || string.IsNullOrEmpty(dictionaryPath))
                return;
            lock (m_Lock)
            {
                if (!m_DuplicateEntriesByHost.TryGetValue(hostId, out var inner))
                {
                    inner = new Dictionary<string, DuplicateEntriesData>();
                    m_DuplicateEntriesByHost[hostId] = inner;
                }
                inner[dictionaryPath] = duplicateEntriesData;
            }
        }

        public DuplicateEntriesData Get(EntityId hostId, string dictionaryPath)
        {
            if (hostId == EntityId.None || string.IsNullOrEmpty(dictionaryPath))
                return default;
            lock (m_Lock)
            {
                if (!m_DuplicateEntriesByHost.TryGetValue(hostId, out var inner))
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
                if (!m_DuplicateEntriesByHost.TryGetValue(hostId, out var inner))
                    return;
                inner.Remove(dictionaryPath);
                if (inner.Count == 0)
                    m_DuplicateEntriesByHost.Remove(hostId);
            }
        }

        public int PruneHostsWhere(Predicate<EntityId> shouldRemoveHost)
        {
            if (shouldRemoveHost == null)
                return 0;

            // Snapshot keys under the lock, run the caller-supplied predicate without it,
            // then remove matched keys under the lock. The predicate can synchronously
            // re-enter Store/Clear on the same thread; m_Lock is Monitor-based and
            // recursive, so iterating m_DuplicateEntriesByHost directly would let the
            // re-entrant mutation bump Dictionary._version and throw InvalidOperationException
            // on the next MoveNext.
            EntityId[] snapshot;
            lock (m_Lock)
            {
                int count = m_DuplicateEntriesByHost.Count;
                if (count == 0)
                    return 0;
                snapshot = new EntityId[count];
                m_DuplicateEntriesByHost.Keys.CopyTo(snapshot, 0);
            }

            List<EntityId> toRemove = null;
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (shouldRemoveHost(snapshot[i]))
                {
                    toRemove ??= new List<EntityId>();
                    toRemove.Add(snapshot[i]);
                }
            }
            if (toRemove == null)
                return 0;

            // Remove may return false if a re-entrant Clear already removed the host;
            // count only the actual removals.
            int removed = 0;
            lock (m_Lock)
            {
                foreach (EntityId id in toRemove)
                {
                    if (m_DuplicateEntriesByHost.Remove(id))
                        removed++;
                }
            }
            return removed;
        }

        public bool HostHasDuplicateDictionaryEntries(EntityId hostId)
        {
            if (hostId == EntityId.None)
                return false;
            lock (m_Lock)
            {
                return m_DuplicateEntriesByHost.TryGetValue(hostId, out var inner) && inner.Count > 0;
            }
        }
    }
}
