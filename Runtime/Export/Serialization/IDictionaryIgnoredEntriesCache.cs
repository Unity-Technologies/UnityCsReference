// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;

namespace UnityEngine
{
    // One entry in the editor's ignored-dictionary-row cache, keyed by (host EntityId, dictionary path).
    // default(IgnoredEntriesData) is a cache miss: indices/entries are both null, so null-check before reading.
    // A constructed instance has non-null, equal-length arrays where indices[i] addresses entries[i]; callers must
    // pass indices in ascending order (unasserted for the hot path) — GetArrayWithHandledIgnoredEntries relies on it.
    internal struct IgnoredEntriesData
    {
        // The full set of serialized rows preserved outside the live dictionary and re-emitted by GetEntriesTyped:
        // both real duplicate-key rows and null-key placeholder rows. Parallel to <see cref="entries"/>, strictly ascending.
        public readonly int[] indices;
        public readonly Array entries;
        public readonly int dictionaryCountWhenRecorded;
        // The subset of <see cref="indices"/> that are genuine duplicate-key rows (TryAdd returned false), excluding
        // null-key placeholder rows. Non-null on a constructed instance (empty when there are no real duplicates).
        public readonly int[] duplicateKeyIndices;
        // The subset of <see cref="indices"/> that are null-key placeholder rows, disjoint from
        // <see cref="duplicateKeyIndices"/>. Non-null on a constructed instance (empty when there are none).
        public readonly int[] nullKeyIndices;

        // Constructed-instance invariants (asserted in develop builds): indices/entries non-null and same length.
        // indices must also be strictly ascending — not asserted (hot path), upheld by the single producer SetEntriesTyped.
        public IgnoredEntriesData(int[] indices, Array entries, int dictionaryCountWhenRecorded, int[] duplicateKeyIndices, int[] nullKeyIndices)
        {
            Assert.IsNotNull(indices,
                "IgnoredEntriesData.indices must not be null. Use default(IgnoredEntriesData) to represent a cache miss.");
            Assert.IsNotNull(entries,
                "IgnoredEntriesData.entries must not be null. Use default(IgnoredEntriesData) to represent a cache miss.");
            Assert.AreEqual(indices.Length, entries.Length,
                "IgnoredEntriesData.indices and entries must be the same length (each indices[i] addresses entries[i]).");
            Assert.IsNotNull(duplicateKeyIndices,
                "IgnoredEntriesData.duplicateKeyIndices must not be null (pass an empty array when there are no real duplicate-key rows).");
            Assert.IsNotNull(nullKeyIndices,
                "IgnoredEntriesData.nullKeyIndices must not be null (pass an empty array when there are no null-key rows).");

            this.indices = indices;
            this.entries = entries;
            this.dictionaryCountWhenRecorded = dictionaryCountWhenRecorded;
            this.duplicateKeyIndices = duplicateKeyIndices;
            this.nullKeyIndices = nullKeyIndices;
        }
    }

    /// <summary>
    /// Editor-only storage for ignored dictionary serialization rows. The player uses a null reference;
    /// <see cref="DictionarySerialization"/> guards all access.
    ///
    /// Implementations must be safe for concurrent calls from any thread. The dictionary serialization
    /// callbacks (<see cref="DictionarySerialization.SetEntriesFromSerializedData"/> and
    /// <see cref="DictionarySerialization.GetDictionaryEntriesForSerialization"/>) are reachable from worker
    /// threads through the native transfer pipeline, while editor cleanup
    /// (<see cref="DictionarySerialization.PruneIgnoredDictionaryEntriesForUnloadedHosts"/>) and the public
    /// <c>SerializedProperty.GetDictionaryDuplicateEntryIndices</c> API are invoked from the main thread.
    /// </summary>
    internal interface IDictionaryIgnoredEntriesCache
    {
        bool HasAnyCachedHosts { get; }

        IgnoredEntriesData Get(EntityId hostId, string dictionaryPath);

        void Store(EntityId hostId, string dictionaryPath, IgnoredEntriesData ignoredEntriesData);

        void Clear(EntityId hostId, string dictionaryPath);

        int PruneUnloadedHosts();

        bool HostHasIgnoredDictionaryEntries(EntityId hostId);
    }
}
