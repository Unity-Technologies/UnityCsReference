// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // An immutable, canonically-ordered set of component types, shared by every element with the same
    // composition. Registry mutation is main-thread only, except EnqueueRelease.
    sealed class ComponentTypeSet
    {
        internal readonly RuntimeTypeHandle[] types;   // exact-sized, sorted by handle value

        readonly int m_Hash;                  // XOR of the entries' hashes, so add/remove re-XOR in O(1)
        ComponentTypeSet m_NextWithSameHash;
        int m_RefCount;                       // elements currently pointing at this record

        internal int Count => types.Length;

        internal int refCount => m_RefCount;  // test and diagnostics only

        ComponentTypeSet(RuntimeTypeHandle[] sortedTypes, int hash)
        {
            types = sortedTypes;
            m_Hash = hash;
        }

        // Never in the registry table and stable across resets, so element handles to it cannot go stale.
        [NoAutoStaticsCleanup]
        static readonly ComponentTypeSet s_Empty = new(Array.Empty<RuntimeTypeHandle>(), 0);

        // ResetRegistry, subscribed to UnloadingUtility, is the teardown for the registry statics.
        [NoAutoStaticsCleanup]
        static Dictionary<int, ComponentTypeSet> s_HashToFirstRecord;

        [NoAutoStaticsCleanup]
        static int s_RecordCount;

        // Releases from GC-finalized elements; the finalizer only enqueues, the main thread drains.
        [NoAutoStaticsCleanup]
        static readonly ConcurrentQueue<ComponentTypeSet> s_PendingReleases = new();

        static ComponentTypeSet()
        {
            UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.ComponentTypeSet, ResetRegistry);
            ResetRegistry();
        }

        internal static ComponentTypeSet Empty => s_Empty;

        internal static int recordCount => s_RecordCount;  // non-empty records; test and diagnostics only

        // Records hold no native memory; dropping the table is the whole reset. The pending releases are
        // drained first so the queue doesn't carry dropped records into the next scope; finalizers that run
        // after this refill it, and those entries only touch their own record's counter (see DrainPendingReleases).
        static void ResetRegistry()
        {
            DrainPendingReleases();
            s_HashToFirstRecord = new Dictionary<int, ComponentTypeSet>();
            s_RecordCount = 0;
        }

        // Test-only: swaps in a fresh registry and restores the old one on dispose. Records created
        // inside the scope stay valid after it; a change to one lands in whichever registry is active.
        internal readonly struct TestScope : IDisposable
        {
            readonly Dictionary<int, ComponentTypeSet> m_HashToFirstRecord;
            readonly int m_RecordCount;

            public TestScope()
            {
                m_HashToFirstRecord = s_HashToFirstRecord;
                m_RecordCount = s_RecordCount;
                ResetRegistry();
            }

            public void Dispose()
            {
                s_HashToFirstRecord = m_HashToFirstRecord;
                s_RecordCount = m_RecordCount;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong SortKey(RuntimeTypeHandle handle) => (ulong)handle.Value.ToInt64();

        // True with the index when present; otherwise false and index is the sorted insertion position.
        internal bool Find(RuntimeTypeHandle typeHandle, out int index)
        {
            var t = types;
            var key = SortKey(typeHandle);
            for (var i = 0; i < t.Length; i++)
            {
                var entry = SortKey(t[i]);
                if (entry == key)
                {
                    index = i;
                    return true;
                }

                if (entry > key)
                {
                    index = i;
                    return false;
                }
            }

            index = t.Length;
            return false;
        }

        // The shared record for current plus added; allocates only when no such record exists yet.
        internal static ComponentTypeSet GetWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, int insertIndex)
            => FindOrCreateRecord(current.m_Hash ^ added.GetHashCode(),
                new SingleTypeChange(current, added, insertIndex, isAdd: true));

        // The shared record for current without the entry at removeIndex; order preserved.
        internal static ComponentTypeSet GetWithRemoved(ComponentTypeSet current, int removeIndex)
        {
            if (current.types.Length == 1)
                return s_Empty;

            return FindOrCreateRecord(current.m_Hash ^ current.types[removeIndex].GetHashCode(),
                new SingleTypeChange(current, default, removeIndex, isAdd: false));
        }

        // One type added to or removed from `current` at `index`. The lookup below takes this rather than a
        // finished record, so a lookup that finds an existing record builds nothing.
        readonly struct SingleTypeChange
        {
            readonly ComponentTypeSet m_Current;
            readonly RuntimeTypeHandle m_Added;
            readonly int m_Index;
            readonly bool m_IsAdd;

            internal SingleTypeChange(ComponentTypeSet current, RuntimeTypeHandle added, int index, bool isAdd)
            {
                m_Current = current;
                m_Added = added;
                m_Index = index;
                m_IsAdd = isAdd;
            }

            internal bool Matches(ComponentTypeSet candidate)
                => m_IsAdd
                    ? candidate.MatchesWithAdded(m_Current, m_Added, m_Index)
                    : candidate.MatchesWithRemoved(m_Current, m_Index);

            internal ComponentTypeSet CreateRecord(int hash)
                => m_IsAdd
                    ? MakeWithAdded(m_Current, m_Added, m_Index, hash)
                    : MakeWithRemoved(m_Current, m_Index, hash);
        }

        // The only place the hash bucket and its collision chain are maintained.
        static ComponentTypeSet FindOrCreateRecord(int hash, in SingleTypeChange change)
        {
            if (!s_HashToFirstRecord.TryGetValue(hash, out var candidate))
            {
                var record = change.CreateRecord(hash);
                s_HashToFirstRecord.Add(hash, record);
                return record;
            }

            ComponentTypeSet previous;
            do
            {
                if (change.Matches(candidate))
                    return candidate;
                previous = candidate;
                candidate = candidate.m_NextWithSameHash;
            }
            while (candidate != null);

            var chained = change.CreateRecord(hash);
            previous.m_NextWithSameHash = chained;
            return chained;
        }

        static ComponentTypeSet MakeWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, int insertIndex, int hash)
        {
            var source = current.types;
            var merged = new RuntimeTypeHandle[source.Length + 1];
            for (var i = 0; i < insertIndex; i++)
                merged[i] = source[i];
            merged[insertIndex] = added;
            for (var i = insertIndex; i < source.Length; i++)
                merged[i + 1] = source[i];

            s_RecordCount++;
            return new ComponentTypeSet(merged, hash);
        }

        static ComponentTypeSet MakeWithRemoved(ComponentTypeSet current, int removeIndex, int hash)
        {
            var source = current.types;
            var reduced = new RuntimeTypeHandle[source.Length - 1];
            for (var i = 0; i < removeIndex; i++)
                reduced[i] = source[i];
            for (var i = removeIndex + 1; i < source.Length; i++)
                reduced[i - 1] = source[i];

            s_RecordCount++;
            return new ComponentTypeSet(reduced, hash);
        }

        // The Matches* probes compare against "current + change" without building the successor: no allocation.

        bool MatchesWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, int insertIndex)
            => types.Length == current.types.Length + 1
                && types[insertIndex].Equals(added)
                && SameExceptAt(types, current.types, insertIndex);

        // current is this record plus its own entry at removeIndex, so that entry needs no separate check.
        bool MatchesWithRemoved(ComponentTypeSet current, int removeIndex)
            => types.Length == current.types.Length - 1
                && SameExceptAt(current.types, types, removeIndex);

        // longer, minus its entry at index, equals shorter. Hash collisions make the full compare
        // necessary, not just the changed entry.
        static bool SameExceptAt(RuntimeTypeHandle[] longer, RuntimeTypeHandle[] shorter, int index)
        {
            for (var i = 0; i < index; i++)
            {
                if (!longer[i].Equals(shorter[i]))
                    return false;
            }

            for (var i = index; i < shorter.Length; i++)
            {
                if (!longer[i + 1].Equals(shorter[i]))
                    return false;
            }

            return true;
        }

        // The counts cannot be rebuilt later (dead elements are GC-reclaimed, live ones cannot be
        // enumerated), so every transition maintains them.

        internal void AddRef()
        {
            if (this != s_Empty)
                m_RefCount++;
        }

        internal void ReleaseRef()
        {
            if (this == s_Empty)
                return;

            m_RefCount--;
            Debug.Assert(m_RefCount >= 0, "ComponentTypeSet refcount went negative; a record was released more than acquired.");
        }

        // Thread-safe: enqueue only; DrainPendingReleases runs on the main thread.
        internal static void EnqueueRelease(ComponentTypeSet set)
        {
            if (set == null || set == s_Empty)
                return;

            s_PendingReleases.Enqueue(set);
        }

        internal static void DrainPendingReleases()
        {
            while (s_PendingReleases.TryDequeue(out var set))
                set.ReleaseRef();
        }
    }
}
