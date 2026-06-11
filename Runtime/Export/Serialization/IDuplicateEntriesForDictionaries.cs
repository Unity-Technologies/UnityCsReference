// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;

namespace UnityEngine
{
    /// <summary>
    /// One cache entry in the editor's duplicate-dictionary-row store, keyed by
    /// (hosting EntityId, formatted dictionary identifier) inside <see cref="IDuplicateEntriesForDictionaries"/>.
    ///
    /// Two distinct states:
    /// <list type="bullet">
    /// <item><description><c>default(DuplicateEntriesData)</c> represents a <em>cache miss</em>: both
    /// <see cref="indices"/> and <see cref="entries"/> are null. <see cref="IDuplicateEntriesForDictionaries.Get"/>
    /// returns this when the host or path has no stored duplicate rows. Consumers must null-check the arrays
    /// before reading.</description></item>
    /// <item><description>A <em>constructed</em> instance is guaranteed (in develop builds) to have non-null
    /// <see cref="indices"/> and <see cref="entries"/> of the same length. Producers must additionally pass
    /// <see cref="indices"/> in strictly-ascending order so each <c>indices[i]</c> addresses <c>entries[i]</c>
    /// at a unique slot; this is a caller contract (not asserted here, to keep the hot inspector-repaint path
    /// allocation- and loop-free) that <see cref="DictionarySerialization.GetArrayWithHandledDuplicateEntries"/>
    /// relies on for its lockstep merge. The single producer (<c>DictionarySerialization.SetEntriesTyped</c>)
    /// satisfies this by appending <c>i</c> in monotonically-increasing order whenever <c>entries[i]</c> is a
    /// duplicate.</description></item>
    /// </list>
    /// </summary>
    internal struct DuplicateEntriesData
    {
        public readonly int[] indices;
        public readonly Array entries;
        public readonly int dictionaryCountWhenRecorded;

        // Constructed-instance invariants (the non-null and length-match checks are asserted in develop builds;
        // cost zero in player release because UnityEngine.Assertions.Assert is conditional on UNITY_ASSERTIONS):
        //   - indices and entries are both non-null. The "default = cache miss" state is reached via
        //     default(DuplicateEntriesData), not via passing null here.
        //   - indices.Length == entries.Length. Each indices[i] addresses entries[i] in the result array
        //     produced by GetArrayWithHandledDuplicateEntries.
        //   - indices is strictly ascending. NOT asserted here (per-element loop is too costly on the hot
        //     inspector-repaint path); upheld by construction at the single producer site
        //     DictionarySerialization.SetEntriesTyped, which appends `i` in monotonically-increasing order
        //     whenever entries[i] is a duplicate. The lockstep merge in GetArrayWithHandledDuplicateEntries
        //     depends on this; a violation would silently mis-place live entries into the result array.
        public DuplicateEntriesData(int[] indices, Array entries, int dictionaryCountWhenRecorded)
        {
            Assert.IsNotNull(indices,
                "DuplicateEntriesData.indices must not be null. Use default(DuplicateEntriesData) to represent a cache miss.");
            Assert.IsNotNull(entries,
                "DuplicateEntriesData.entries must not be null. Use default(DuplicateEntriesData) to represent a cache miss.");
            Assert.AreEqual(indices.Length, entries.Length,
                "DuplicateEntriesData.indices and entries must be the same length (each indices[i] addresses entries[i]).");

            this.indices = indices;
            this.entries = entries;
            this.dictionaryCountWhenRecorded = dictionaryCountWhenRecorded;
        }
    }

    /// <summary>
    /// Editor-only storage for duplicate dictionary serialization rows. The player uses a null reference;
    /// <see cref="DictionarySerialization"/> guards all access.
    ///
    /// Implementations must be safe for concurrent calls from any thread. The dictionary serialization
    /// callbacks (<see cref="DictionarySerialization.SetEntriesFromSerializedData"/> and
    /// <see cref="DictionarySerialization.GetDictionaryEntriesForSerialization"/>) are reachable from worker
    /// threads through the native transfer pipeline, while editor cleanup
    /// (<see cref="DictionarySerialization.PruneDuplicateDictionaryEntriesForUnloadedHosts"/>) and the public
    /// <c>SerializedProperty.GetDictionaryDuplicateEntryIndices</c> API are invoked from the main thread.
    /// </summary>
    internal interface IDuplicateEntriesForDictionaries
    {
        bool HasAnyCachedHosts { get; }

        DuplicateEntriesData Get(EntityId hostId, string dictionaryPath);

        void Store(EntityId hostId, string dictionaryPath, DuplicateEntriesData duplicateEntriesData);

        void Clear(EntityId hostId, string dictionaryPath);

        int PruneUnloadedHosts();

        bool HostHasDuplicateDictionaryEntries(EntityId hostId);
    }
}
