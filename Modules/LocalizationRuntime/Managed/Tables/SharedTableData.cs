// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

/// <summary>
/// Per-key flags stored once on the shared table entry and shared across every locale.
/// </summary>
/// <remarks>
/// These flags live on a key's <see cref="SharedTableData.SharedTableEntry"/>, so they apply to that key in every locale
/// rather than to a single translation. The enumeration is a bit field, so values combine with bitwise operators. The
/// <see cref="Smart"/> flag marks a key whose string values are Smart Strings.
/// </remarks>
/// <example>
/// <para>Combine and test the flags on a key's entry.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedEntryFlagsOverviewExample.cs"/>
/// </example>
/// <seealso cref="SharedTableData"/>
/// <seealso cref="SharedTableData.SharedTableEntry"/>
[Flags]
public enum SharedEntryFlags
{
    /// <summary>
    /// No flags are set.
    /// </summary>
    None = 0,

    /// <summary>
    /// The key's string values are Smart Strings, formatted through Smart Strings at resolve time.
    /// </summary>
    Smart = 1
}

/// <summary>
/// The shared key table for a localization table collection: the set of string keys and their stable ids that every
/// per-locale table in the collection uses.
/// </summary>
/// <remarks>
/// Each key maps to a stable <c>long</c> id that never changes for the life of the key, so renaming a key keeps its
/// translations intact. Ids come from the <see cref="KeyGenerator"/>, a <see cref="DistributedUIDGenerator"/> by default,
/// so they are non-sequential and an id of <c>0</c> means "no key". Every per-locale <see cref="ResourceTable"/> in the
/// collection stores its values against these ids. Add and look up keys with <see cref="AddKey(string)"/>,
/// <see cref="GetId(string)"/>, and <see cref="GetKey(long)"/>, and rename in place with <see cref="RenameKey(long,string)"/>.
/// This type derives from <see cref="UnityEngine.ScriptableObject"/>, so create an instance with
/// <see cref="UnityEngine.ScriptableObject"/> factory methods rather than the <c>new</c> operator.
/// </remarks>
/// <example>
/// <para>Create shared data, add a key, and resolve between key text and id.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataOverviewExample.cs"/>
/// </example>
/// <seealso cref="SharedTableData.SharedTableEntry"/>
/// <seealso cref="SharedEntryFlags"/>
/// <seealso cref="IKeyGenerator"/>
/// <seealso cref="ResourceTable"/>
public sealed class SharedTableData : ScriptableObject, ISerializationCallbackReceiver
{
    /// <summary>
    /// A single key in the shared data, pairing the key text with its stable id.
    /// </summary>
    /// <remarks>
    /// Each entry carries the key's id, its text, the per-key <see cref="SharedEntryFlags"/>, and a
    /// <see cref="MetadataCollection"/> shared across every locale. Obtain one from <see cref="SharedTableData.GetEntry(string)"/>
    /// or <see cref="SharedTableData.AddKey(string)"/> rather than constructing it directly, so the owning
    /// <see cref="SharedTableData"/> keeps its id and key lookups in sync.
    /// </remarks>
    /// <example>
    /// <para>Read the id, key, and flags from an entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableEntryOverviewExample.cs"/>
    /// </example>
    /// <seealso cref="SharedTableData"/>
    /// <seealso cref="SharedEntryFlags"/>
    /// <seealso cref="MetadataCollection"/>
    [Serializable]
    public sealed class SharedTableEntry
    {
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("Id")] long m_Id;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("Key")] string m_Key;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("Flags")] SharedEntryFlags m_Flags;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("Metadata")] MetadataCollection m_Metadata = new();

        /// <summary>
        /// The stable id of the key.
        /// </summary>
        /// <remarks>
        /// Assigned once when the key is added and never changed afterwards, so it stays valid across key renames.
        /// The owning <see cref="SharedTableData"/> keys its id lookup on this value, so it cannot be assigned directly.
        /// </remarks>
        public long Id
        {
            get => m_Id;
            internal set => m_Id = value;
        }

        /// <summary>
        /// The key text.
        /// </summary>
        /// <remarks>
        /// Change it through <see cref="SharedTableData.RenameKey(long,string)"/>, which also updates the owning shared
        /// data's key lookup; it cannot be assigned directly.
        /// </remarks>
        public string Key
        {
            get => m_Key;
            internal set => m_Key = value;
        }

        /// <summary>
        /// Per-key flags shared across every locale.
        /// </summary>
        /// <remarks>
        /// See <see cref="SharedEntryFlags"/> for the available flags. Use <see cref="IsSmart"/> for the common Smart String case.
        /// </remarks>
        public SharedEntryFlags Flags
        {
            get => m_Flags;
            set => m_Flags = value;
        }

        /// <summary>
        /// Metadata attached to the key across every locale.
        /// </summary>
        /// <remarks>
        /// Holds <see cref="IMetadata"/> items such as author comments. This is per-key metadata; collection-level metadata
        /// lives on <see cref="SharedTableData.Metadata"/>.
        /// </remarks>
        public MetadataCollection Metadata => m_Metadata;

        /// <summary>
        /// Whether the key's string values are Smart Strings.
        /// </summary>
        /// <remarks>
        /// A convenience over the <see cref="SharedEntryFlags.Smart"/> bit in <see cref="Flags"/>. Setting it toggles that
        /// bit without disturbing the others.
        /// </remarks>
        public bool IsSmart
        {
            get => (Flags & SharedEntryFlags.Smart) != 0;
            set => Flags = value ? Flags | SharedEntryFlags.Smart : Flags & ~SharedEntryFlags.Smart;
        }
    }

    [SerializeField] string m_TableCollectionName;
    // Left empty; the editor assigns the collection's asset guid when the asset is created.
    [SerializeField] GUID m_TableCollectionNameGuid;
    [SerializeReference] IKeyGenerator m_KeyGenerator = new DistributedUIDGenerator();
    [SerializeField] List<SharedTableEntry> m_Entries = new();
    [SerializeField] MetadataCollection m_Metadata = new();

    Dictionary<string, SharedTableEntry> m_KeyToEntry;
    Dictionary<long, SharedTableEntry> m_IdToEntry;

    /// <summary>
    /// The name of the table collection this shared data belongs to.
    /// </summary>
    public string TableCollectionName
    {
        get => m_TableCollectionName;
        set => m_TableCollectionName = value;
    }

    /// <summary>
    /// The asset guid of the table collection.
    /// </summary>
    /// <remarks>
    /// The value is the default all-zero <see cref="UnityEngine.GUID"/> until the editor assigns the collection's asset guid
    /// when the asset is created. The setter is internal, so at runtime this property is effectively read-only.
    /// </remarks>
    public GUID TableCollectionNameGuid
    {
        get => m_TableCollectionNameGuid;
        [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
        internal set => m_TableCollectionNameGuid = value;
    }

    /// <summary>
    /// The keys in this shared data, in their current order.
    /// </summary>
    /// <remarks>
    /// Each element is a <see cref="SharedTableData.SharedTableEntry"/> pairing a key with its stable id. Reorder the keys
    /// with <see cref="SetKeyOrder(IReadOnlyList{long})"/>.
    /// </remarks>
    public IReadOnlyList<SharedTableEntry> Entries => m_Entries;

    /// <summary>
    /// Collection-level metadata shared across all locale tables.
    /// </summary>
    /// <remarks>
    /// Holds <see cref="IMetadata"/> items that apply to the whole collection, as opposed to the per-key metadata on each
    /// <see cref="SharedTableData.SharedTableEntry"/>.
    /// </remarks>
    public MetadataCollection Metadata => m_Metadata;

    /// <summary>
    /// The generator that assigns stable ids to new keys.
    /// </summary>
    /// <remarks>
    /// Defaults to a <see cref="DistributedUIDGenerator"/>. Setting it to <see langword="null"/> restores that default
    /// rather than leaving the property unset. <see cref="AddKey(string)"/> calls <see cref="IKeyGenerator.GetNextKey"/> to
    /// obtain each new id.
    /// </remarks>
    public IKeyGenerator KeyGenerator
    {
        get => m_KeyGenerator ??= new DistributedUIDGenerator();
        set => m_KeyGenerator = value ?? new DistributedUIDGenerator();
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void InvalidateCache()
    {
        m_KeyToEntry = null;
        m_IdToEntry = null;
    }

    /// <summary>
    /// Removes every key from the shared data.
    /// </summary>
    /// <remarks>
    /// This clears the shared key table only. The entries in each per-locale <see cref="ResourceTable"/> are left untouched,
    /// so clear the shared data and every locale table together to avoid orphaning translations.
    /// </remarks>
    /// <example>
    /// <para>Remove all keys from the shared data.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataClearExample.cs"/>
    /// </example>
    /// <seealso cref="RemoveKey(string)"/>
    /// <seealso cref="AddKey(string)"/>
    public void Clear()
    {
        m_Entries.Clear();
        InvalidateCache();
    }

    /// <summary>
    /// Returns the stable id for a key.
    /// </summary>
    /// <remarks>
    /// Returns <c>0</c> when the key is empty, null, or not present. Ids are stable across renames, so store the id rather
    /// than the key text when you need a reference that survives a rename.
    /// </remarks>
    /// <param name="key">The key to look up.</param>
    /// <returns>The key's stable id, or <c>0</c> when it is absent.</returns>
    /// <example>
    /// <para>Look up the id assigned to a key.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataGetIdExample.cs"/>
    /// </example>
    /// <seealso cref="GetKey(long)"/>
    /// <seealso cref="GetEntry(string)"/>
    public long GetId(string key)
    {
        if (string.IsNullOrEmpty(key))
            return 0;
        EnsureMaps();
        return m_KeyToEntry.TryGetValue(key, out var entry) ? entry.Id : 0;
    }

    /// <summary>
    /// Returns the key text for a stable id.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when the id is <c>0</c> or not present.
    /// </remarks>
    /// <param name="id">The id to look up.</param>
    /// <returns>The key text, or <see langword="null"/> when the id is absent.</returns>
    /// <example>
    /// <para>Resolve a stable id back to its key text.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataGetKeyExample.cs"/>
    /// </example>
    /// <seealso cref="GetId(string)"/>
    /// <seealso cref="GetEntry(long)"/>
    public string GetKey(long id)
    {
        if (id == 0)
            return null;
        EnsureMaps();
        return m_IdToEntry.TryGetValue(id, out var entry) ? entry.Key : null;
    }

    /// <summary>
    /// Returns the entry for a key.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when the key is empty, null, or not present. The entry exposes the key's id, flags,
    /// and metadata through <see cref="SharedTableData.SharedTableEntry"/>.
    /// </remarks>
    /// <param name="key">The key to look up.</param>
    /// <returns>The matching entry, or <see langword="null"/> when the key is absent.</returns>
    /// <example>
    /// <para>Look up an entry by its key text.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataGetEntryByKeyExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry(long)"/>
    /// <seealso cref="GetId(string)"/>
    public SharedTableEntry GetEntry(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        EnsureMaps();
        return m_KeyToEntry.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns the entry for a stable id.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when no key uses the id. An id of <c>0</c> means "no key" and always resolves
    /// to <see langword="null"/>.
    /// </remarks>
    /// <param name="id">The id to look up.</param>
    /// <returns>The matching entry, or <see langword="null"/> when the id is absent.</returns>
    /// <example>
    /// <para>Look up an entry by its stable id.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataGetEntryByIdExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry(string)"/>
    /// <seealso cref="GetKey(long)"/>
    public SharedTableEntry GetEntry(long id)
    {
        if (id == 0)
            return null;
        EnsureMaps();
        return m_IdToEntry.TryGetValue(id, out var entry) ? entry : null;
    }

    /// <summary>
    /// Returns whether a key is marked as a Smart String.
    /// </summary>
    /// <remarks>
    /// Equivalent to testing the <see cref="SharedEntryFlags.Smart"/> flag on the key's entry. Returns
    /// <see langword="false"/> when the id is absent.
    /// </remarks>
    /// <param name="id">The stable key id.</param>
    /// <returns><c>true</c> when the key's values are Smart Strings; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Check whether a key uses Smart Strings.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataIsSmartExample.cs"/>
    /// </example>
    /// <seealso cref="SharedEntryFlags"/>
    /// <seealso cref="SharedTableData.SharedTableEntry.IsSmart"/>
    public bool IsSmart(long id) => GetEntry(id) is { } entry && entry.IsSmart;

    /// <summary>
    /// The selector that drives variants for the key with <paramref name="id"/>, resolved by cascade: the key's own
    /// <see cref="VariantSelectorMetadata"/> first, then the collection's, then <see cref="VariantResolver.GlobalSelector"/>.
    /// Returns <see langword="null"/> when no tier defines one.
    /// </summary>
    /// <param name="id">The stable key id.</param>
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal IVariantSelector GetVariantSelector(long id)
        => GetEntry(id)?.Metadata.GetMetadata<VariantSelectorMetadata>()?.Selector
           ?? m_Metadata.GetMetadata<VariantSelectorMetadata>()?.Selector
           ?? VariantResolver.GlobalSelector;

    /// <summary>
    /// The variant key that applies to the key with <paramref name="id"/> right now, or <see langword="null"/> when it is not variant-driven.
    /// </summary>
    /// <param name="id">The stable key id.</param>
    internal string GetVariantKey(long id) => GetVariantSelector(id)?.CurrentKey;

    /// <summary>
    /// Whether the key with <paramref name="id"/> is variant-driven through any cascade tier (key, collection, or global).
    /// </summary>
    /// <param name="id">The stable key id.</param>
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal bool IsVariant(long id) => GetVariantSelector(id) != null;

    /// <summary>
    /// Adds a key and returns its entry.
    /// </summary>
    /// <remarks>
    /// When the key already exists, its existing entry is returned rather than a duplicate being created. A new id is drawn
    /// from the <see cref="KeyGenerator"/> and retried until it is unique. Returns <see langword="null"/> when the key is
    /// empty or null.
    /// </remarks>
    /// <param name="key">The key to add.</param>
    /// <returns>The new or existing entry, or <see langword="null"/> when the key is empty.</returns>
    /// <example>
    /// <para>Add a key and observe that adding it again returns the same entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataAddKeyExample.cs"/>
    /// </example>
    /// <seealso cref="AddKey(string,long)"/>
    /// <seealso cref="RemoveKey(string)"/>
    /// <seealso cref="RenameKey(long,string)"/>
    public SharedTableEntry AddKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;
        EnsureMaps();
        if (m_KeyToEntry.TryGetValue(key, out var existing))
            return existing;

        const int maxAttempts = 5000;
        var id = KeyGenerator.GetNextKey();
        for (var attempts = 1; m_IdToEntry.ContainsKey(id); attempts++)
        {
            if (attempts >= maxAttempts)
                throw new InvalidOperationException($"{nameof(IKeyGenerator)} did not produce a free key id after {attempts} attempts.");
            id = KeyGenerator.GetNextKey();
        }

        return AddKey(key, id);
    }

    /// <summary>
    /// Adds a key with an explicit id and returns its entry.
    /// </summary>
    /// <remarks>
    /// Use this to preserve ids when rebuilding from serialized data, for example importing or loading from a file. Returns
    /// the existing entry when the key is already present, logging a warning if the requested id differs from the stored
    /// one. Returns <see langword="null"/> when the key is empty, the id is <c>0</c>, or the id is already used by another
    /// key.
    /// </remarks>
    /// <param name="key">The key to add.</param>
    /// <param name="id">The stable id to assign.</param>
    /// <returns>The new or existing entry, or <see langword="null"/> when the key or id cannot be used.</returns>
    /// <example>
    /// <para>Add a key with an id preserved from serialized data.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataAddKeyWithIdExample.cs"/>
    /// </example>
    /// <seealso cref="AddKey(string)"/>
    /// <seealso cref="GetEntry(long)"/>
    public SharedTableEntry AddKey(string key, long id)
    {
        if (string.IsNullOrEmpty(key) || id == 0)
            return null;
        EnsureMaps();
        if (m_KeyToEntry.TryGetValue(key, out var existing))
        {
            if (existing.Id != id)
                Debug.LogWarning($"Key '{key}' already exists with id {existing.Id}; ignoring requested id {id}.");
            return existing;
        }
        if (m_IdToEntry.ContainsKey(id))
            return null;

        var entry = new SharedTableEntry { Id = id, Key = key };
        m_Entries.Add(entry);
        m_KeyToEntry[key] = entry;
        m_IdToEntry[id] = entry;
        return entry;
    }

    /// <summary>
    /// Reorders the keys to match a given sequence of ids.
    /// </summary>
    /// <remarks>
    /// Keys are arranged in the order their ids appear in <paramref name="orderedIds"/>. Ids that are not listed keep their
    /// relative order and follow the listed keys. A null or empty list leaves the order unchanged.
    /// </remarks>
    /// <param name="orderedIds">The desired key-id order.</param>
    /// <example>
    /// <para>Move a key to the front by listing its id first.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataSetKeyOrderExample.cs"/>
    /// </example>
    /// <seealso cref="Entries"/>
    public void SetKeyOrder(IReadOnlyList<long> orderedIds)
    {
        if (orderedIds == null || orderedIds.Count == 0)
            return;
        var byId = new Dictionary<long, SharedTableEntry>(m_Entries.Count);
        for (var i = 0; i < m_Entries.Count; i++)
        {
            if (m_Entries[i] != null)
                byId[m_Entries[i].Id] = m_Entries[i];
        }
        var reordered = new List<SharedTableEntry>(m_Entries.Count);
        var used = new HashSet<long>(orderedIds.Count);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (byId.TryGetValue(orderedIds[i], out var entry) && used.Add(orderedIds[i]))
                reordered.Add(entry);
        }
        for (var i = 0; i < m_Entries.Count; i++)
        {
            if (m_Entries[i] != null && !used.Contains(m_Entries[i].Id))
                reordered.Add(m_Entries[i]);
        }
        m_Entries = reordered;
        InvalidateCache();
    }

    /// <summary>
    /// Removes a key from the shared data.
    /// </summary>
    /// <remarks>
    /// This removes the key from the shared key table only. The per-locale <see cref="ResourceTable"/> entries for the key
    /// are left untouched, so remove the key from the shared data and every locale table together to avoid orphaning
    /// translations. Does nothing when the key is absent.
    /// </remarks>
    /// <param name="key">The key to remove.</param>
    /// <example>
    /// <para>Remove a single key from the shared data.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataRemoveKeyExample.cs"/>
    /// </example>
    /// <seealso cref="AddKey(string)"/>
    /// <seealso cref="Clear"/>
    public void RemoveKey(string key)
    {
        var entry = GetEntry(key);
        if (entry == null)
            return;
        for (var i = m_Entries.Count - 1; i >= 0; i--)
        {
            if (m_Entries[i] != null && string.Equals(m_Entries[i].Key, key, StringComparison.Ordinal))
                m_Entries.RemoveAt(i);
        }
        m_KeyToEntry.Remove(entry.Key);
        m_IdToEntry.Remove(entry.Id);
    }

    /// <summary>
    /// Renames a key while keeping its id.
    /// </summary>
    /// <remarks>
    /// The key's stable id does not change, so existing translations keyed by id continue to resolve. Returns
    /// <see langword="false"/> when the new name is empty, no key uses the id, or another key already uses the new name.
    /// Renaming a key to its current name succeeds and makes no change.
    /// </remarks>
    /// <param name="id">The stable id of the key to rename.</param>
    /// <param name="newKey">The new key text.</param>
    /// <returns><c>true</c> when the key was renamed or already had the requested name; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Fix a typo in a key without changing its id.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/SharedTableDataRenameKeyExample.cs"/>
    /// </example>
    /// <seealso cref="AddKey(string)"/>
    /// <seealso cref="GetId(string)"/>
    public bool RenameKey(long id, string newKey)
    {
        if (string.IsNullOrEmpty(newKey))
            return false;
        EnsureMaps();
        if (!m_IdToEntry.TryGetValue(id, out var entry))
            return false;
        if (string.Equals(entry.Key, newKey, StringComparison.Ordinal))
            return true;
        if (m_KeyToEntry.ContainsKey(newKey))
            return false;
        m_KeyToEntry.Remove(entry.Key);
        entry.Key = newKey;
        m_KeyToEntry[newKey] = entry;
        return true;
    }

    void EnsureMaps()
    {
        if (m_KeyToEntry != null && m_IdToEntry != null)
            return;
        m_KeyToEntry = new Dictionary<string, SharedTableEntry>(m_Entries.Count);
        m_IdToEntry = new Dictionary<long, SharedTableEntry>(m_Entries.Count);
        for (var i = 0; i < m_Entries.Count; i++)
        {
            var entry = m_Entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.Key))
                continue;
            m_KeyToEntry[entry.Key] = entry;
            // An id of 0 means "no key"; a legacy or hand-edited entry carrying it must not answer id lookups.
            if (entry.Id != 0)
                m_IdToEntry[entry.Id] = entry;
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize() { }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // The constructor does not run on deserialize, so guard against missing fields from older data.
        m_Entries ??= new List<SharedTableEntry>();
        m_Metadata ??= new MetadataCollection();
        InvalidateCache();
    }
}
