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

        // Parallel to types. Part of the set's identity: a record replaced by a code reload owns none of the
        // handles of slots created before it, so sets that differ only by record must stay distinct.
        internal readonly PerTypeStore[] records;

        readonly int m_Hash;                  // XOR of the entries' hashes, so add/remove re-XOR in O(1)
        ComponentTypeSet m_NextWithSameHash;
        int m_RefCount;                       // elements currently pointing at this record

        // Main-thread only, like the rest of the registry.
        // Hashed rather than scanned: Empty's fan-out is every type that can start an element, and a
        // linear scan of those costs more from two entries up.
        AddSuccessor[] m_AddSuccessors;         // open-addressed on the added handle, power-of-two length
        int m_AddSuccessorCount;
        ComponentTypeSet[] m_RemoveSuccessors;  // indexed by the removed entry's position
        int m_SuccessorGeneration;

        struct AddSuccessor
        {
            internal RuntimeTypeHandle type;
            internal ComponentTypeSet set;
            internal int insertIndex;
        }

        internal int Count => types.Length;

        internal int refCount => m_RefCount;  // test and diagnostics only

        ComponentTypeSet(RuntimeTypeHandle[] sortedTypes, PerTypeStore[] parallelRecords, int hash)
        {
            types = sortedTypes;
            records = parallelRecords;
            m_Hash = hash;
            m_SuccessorGeneration = s_Generation;
        }

        // Never in the registry table and stable across resets, so element handles to it cannot go stale.
        [NoAutoStaticsCleanup]
        static readonly ComponentTypeSet s_Empty = new(Array.Empty<RuntimeTypeHandle>(), Array.Empty<PerTypeStore>(), 0);

        // ResetRegistry, subscribed to UnloadingUtility, is the teardown for the registry statics.
        [NoAutoStaticsCleanup]
        static Dictionary<int, ComponentTypeSet> s_HashToFirstRecord;

        [NoAutoStaticsCleanup]
        static int s_RecordCount;

        // Bumped by every registry swap, never restored: a restored value would revalidate the successors a
        // surviving record memoized against a table that is no longer active.
        [NoAutoStaticsCleanup]
        static int s_Generation;

        // Releases from GC-finalized elements; the finalizer only enqueues, the main thread drains.
        [NoAutoStaticsCleanup]
        static readonly ConcurrentQueue<ComponentTypeSet> s_PendingReleases = new();

        static ComponentTypeSet()
        {
            UnloadingUtility.InitializeOnEveryCodeLoad(UnloadingSubscriber.ComponentTypeSet, Reinitialize);
        }

        static void Reinitialize()
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
            s_Generation++;
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
                s_Generation++;
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

        internal static ComponentTypeSet GetWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, PerTypeStore addedRecord, out int insertIndex)
        {
            current.EnsureSuccessorsCurrent();

            var successors = current.m_AddSuccessors;
            var staleProbe = -1;
            if (successors != null)
            {
                var mask = successors.Length - 1;
                for (var probe = SuccessorBucket(added, mask); ; probe = (probe + 1) & mask)
                {
                    ref var entry = ref successors[probe];
                    if (entry.set == null)
                        break;

                    if (entry.type.Equals(added))
                    {
                        if (ReferenceEquals(entry.set.records[entry.insertIndex], addedRecord))
                        {
                            insertIndex = entry.insertIndex;
                            return entry.set;
                        }

                        staleProbe = probe;   // memoized against a record a code reload has since replaced
                        break;
                    }
                }
            }

            var duplicate = current.Find(added, out insertIndex);
            // Callers reject duplicates before requirements resolution; a hit here means that broke.
            Debug.Assert(!duplicate, "Adding a component type that is already in the element's type set.");
            var successor = FindOrCreateRecord(current.m_Hash ^ added.GetHashCode(),
                new SingleTypeChange(current, added, addedRecord, insertIndex, isAdd: true));

            if (staleProbe >= 0)
                successors[staleProbe].set = successor;
            else
                current.StoreAddSuccessor(added, successor, insertIndex);
            return successor;
        }

        // The shared record for current without the entry at removeIndex; order preserved.
        internal static ComponentTypeSet GetWithRemoved(ComponentTypeSet current, int removeIndex)
        {
            if (current.types.Length == 1)
                return s_Empty;

            current.EnsureSuccessorsCurrent();

            var successors = current.m_RemoveSuccessors ??= new ComponentTypeSet[current.types.Length];
            var memoized = successors[removeIndex];
            if (memoized != null)
                return memoized;

            var successor = FindOrCreateRecord(current.m_Hash ^ current.types[removeIndex].GetHashCode(),
                new SingleTypeChange(current, default, null, removeIndex, isAdd: false));

            successors[removeIndex] = successor;
            return successor;
        }

        // A handle is pointer-aligned, so the low bits carry nothing; fold two shifted copies instead.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int SuccessorBucket(RuntimeTypeHandle handle, int mask)
        {
            var key = SortKey(handle);
            return (int)(((key >> 4) ^ (key >> 20)) & (ulong)mask);
        }

        void EnsureSuccessorsCurrent()
        {
            if (m_SuccessorGeneration == s_Generation)
                return;

            m_AddSuccessors = null;
            m_AddSuccessorCount = 0;
            m_RemoveSuccessors = null;
            m_SuccessorGeneration = s_Generation;
        }

        void StoreAddSuccessor(RuntimeTypeHandle added, ComponentTypeSet successor, int insertIndex)
        {
            if (m_AddSuccessors == null)
                m_AddSuccessors = new AddSuccessor[4];
            else if ((m_AddSuccessorCount + 1) * 2 > m_AddSuccessors.Length)
                GrowAddSuccessors();

            PlaceAddSuccessor(m_AddSuccessors, added, successor, insertIndex);
            m_AddSuccessorCount++;
        }

        void GrowAddSuccessors()
        {
            var grown = new AddSuccessor[m_AddSuccessors.Length * 2];
            foreach (var entry in m_AddSuccessors)
            {
                if (entry.set != null)
                    PlaceAddSuccessor(grown, entry.type, entry.set, entry.insertIndex);
            }

            m_AddSuccessors = grown;
        }

        static void PlaceAddSuccessor(AddSuccessor[] successors, RuntimeTypeHandle added, ComponentTypeSet successor, int insertIndex)
        {
            var mask = successors.Length - 1;
            var probe = SuccessorBucket(added, mask);
            while (successors[probe].set != null)
                probe = (probe + 1) & mask;

            successors[probe].type = added;
            successors[probe].set = successor;
            successors[probe].insertIndex = insertIndex;
        }

        // One type added to or removed from `current` at `index`. The lookup below takes this rather than a
        // finished record, so a lookup that finds an existing record builds nothing.
        readonly struct SingleTypeChange
        {
            readonly ComponentTypeSet m_Current;
            readonly RuntimeTypeHandle m_Added;
            readonly PerTypeStore m_AddedRecord;
            readonly int m_Index;
            readonly bool m_IsAdd;

            internal SingleTypeChange(ComponentTypeSet current, RuntimeTypeHandle added, PerTypeStore addedRecord, int index, bool isAdd)
            {
                m_Current = current;
                m_Added = added;
                m_AddedRecord = addedRecord;
                m_Index = index;
                m_IsAdd = isAdd;
            }

            internal bool Matches(ComponentTypeSet candidate)
                => m_IsAdd
                    ? candidate.MatchesWithAdded(m_Current, m_Added, m_AddedRecord, m_Index)
                    : candidate.MatchesWithRemoved(m_Current, m_Index);

            internal ComponentTypeSet CreateRecord(int hash)
                => m_IsAdd
                    ? MakeWithAdded(m_Current, m_Added, m_AddedRecord, m_Index, hash)
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

        static ComponentTypeSet MakeWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, PerTypeStore addedRecord, int insertIndex, int hash)
        {
            s_RecordCount++;
            return new ComponentTypeSet(
                InsertAt(current.types, insertIndex, added),
                InsertAt(current.records, insertIndex, addedRecord),
                hash);
        }

        static ComponentTypeSet MakeWithRemoved(ComponentTypeSet current, int removeIndex, int hash)
        {
            s_RecordCount++;
            return new ComponentTypeSet(
                RemoveAt(current.types, removeIndex),
                RemoveAt(current.records, removeIndex),
                hash);
        }

        static TItem[] InsertAt<TItem>(TItem[] source, int index, TItem item)
        {
            var merged = new TItem[source.Length + 1];
            Array.Copy(source, 0, merged, 0, index);
            merged[index] = item;
            Array.Copy(source, index, merged, index + 1, source.Length - index);
            return merged;
        }

        static TItem[] RemoveAt<TItem>(TItem[] source, int index)
        {
            var reduced = new TItem[source.Length - 1];
            Array.Copy(source, 0, reduced, 0, index);
            Array.Copy(source, index + 1, reduced, index, source.Length - index - 1);
            return reduced;
        }

        // The Matches* probes compare against "current + change" without building the successor: no allocation.

        bool MatchesWithAdded(ComponentTypeSet current, RuntimeTypeHandle added, PerTypeStore addedRecord, int insertIndex)
            => types.Length == current.types.Length + 1
                && types[insertIndex].Equals(added)
                && ReferenceEquals(records[insertIndex], addedRecord)
                && SameExceptAt(types, current.types, insertIndex)
                && SameRecordsExceptAt(records, current.records, insertIndex);

        // current is this record plus its own entry at removeIndex, so that entry needs no separate check.
        bool MatchesWithRemoved(ComponentTypeSet current, int removeIndex)
            => types.Length == current.types.Length - 1
                && SameExceptAt(current.types, types, removeIndex)
                && SameRecordsExceptAt(current.records, records, removeIndex);

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

        static bool SameRecordsExceptAt(PerTypeStore[] longer, PerTypeStore[] shorter, int index)
        {
            for (var i = 0; i < index; i++)
            {
                if (!ReferenceEquals(longer[i], shorter[i]))
                    return false;
            }

            for (var i = index; i < shorter.Length; i++)
            {
                if (!ReferenceEquals(longer[i + 1], shorter[i]))
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
