// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// A localization table that holds the translated values for one locale in a collection.
/// </summary>
/// <remarks>
/// Each table targets a single locale (reported by <see cref="ResourceTable.LocaleIdentifier"/>) and shares its keys with every
/// other locale table in the collection through one <see cref="SharedTableData"/>. Values are stored as polymorphic
/// <see cref="IResourceEntry"/> objects (strings, assets, variants, or custom kinds) held through
/// <c>[SerializeReference]</c>, so a single table type covers every entry kind. Look entries up by stable key id or
/// by key text with the <see cref="GetEntry(long)"/> family, add plain strings with
/// <see cref="AddStringEntry(string, string, bool?)"/>, and add fully-formed entries with
/// <see cref="AddEntry(IResourceEntry)"/>. Tables belong to a collection: create one through the Resource Tables
/// window, or with the ScriptableObject.CreateInstance method, assigning <see cref="SharedData"/> before resolving
/// keys by text.
/// </remarks>
/// <example>
/// <para>Create a table, add a string, and read it back.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableOverviewExample.cs"/>
/// </example>
/// <seealso cref="SharedTableData"/>
/// <seealso cref="IResourceEntry"/>
/// <seealso cref="IStringEntry"/>
/// <seealso cref="Unity.Localization.LocaleIdentifier"/>
public sealed class ResourceTable : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] string m_LocaleCode;
    [SerializeField] SharedTableData m_SharedData;
    [SerializeField] MetadataCollection m_Metadata = new();
    [SerializeReference] List<IResourceEntry> m_Entries = new();

    Dictionary<long, IResourceEntry> m_EntryCache;

    /// <summary>
    /// The locale this table provides values for.
    /// </summary>
    /// <remarks>
    /// A collection holds one table per locale, distinguished by this identifier.
    /// </remarks>
    public LocaleIdentifier LocaleIdentifier
    {
        get => new(m_LocaleCode);
        set => m_LocaleCode = value.Code;
    }

    /// <summary>
    /// The shared key data for the collection this table belongs to.
    /// </summary>
    /// <remarks>
    /// Every locale table in a collection points at the same <see cref="SharedTableData"/>, which maps key text to
    /// the stable ids used by the entries here. Lookups by key text (for example <see cref="GetEntry(string)"/>) and
    /// <see cref="AddStringEntry(string, string, bool?)"/> resolve through it, so assign it before those calls resolve
    /// keys.
    /// </remarks>
    public SharedTableData SharedData
    {
        get => m_SharedData;
        set
        {
            if (m_SharedData == value)
                return;
            m_SharedData = value;
            for (var i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i] is ResourceEntryBase entry)
                    entry.SetTable(this);
            }
        }
    }

    /// <summary>
    /// The name of the collection this table belongs to.
    /// </summary>
    /// <remarks>
    /// Resolved through <see cref="SharedData"/>. Returns null when no shared data is assigned.
    /// </remarks>
    public string TableCollectionName => m_SharedData != null ? m_SharedData.TableCollectionName : null;

    /// <summary>
    /// The metadata attached to this table for its locale.
    /// </summary>
    /// <remarks>
    /// This metadata applies only to this locale's table. Collection-level metadata shared across every locale lives
    /// on <see cref="SharedData"/>.
    /// </remarks>
    public MetadataCollection Metadata => m_Metadata;

    /// <summary>
    /// The entries this table holds for its locale.
    /// </summary>
    /// <remarks>
    /// The list is read-only. Change its contents through <see cref="AddEntry(IResourceEntry)"/>,
    /// <see cref="AddStringEntry(string, string, bool?)"/>, <see cref="RemoveEntry(long)"/>, and
    /// <see cref="RemoveAllEntries()"/>.
    /// </remarks>
    public IReadOnlyList<IResourceEntry> Entries
    {
        get
        {
            _ = Cache;
            return m_Entries;
        }
    }

    Dictionary<long, IResourceEntry> Cache
    {
        get
        {
            if (m_EntryCache == null)
                RebuildCache();
            return m_EntryCache;
        }
    }

    void RebuildCache()
    {
        m_EntryCache = new Dictionary<long, IResourceEntry>(m_Entries.Count);
        for (var i = 0; i < m_Entries.Count; i++)
        {
            var entry = m_Entries[i];
            if (entry == null)
                continue;
            // Restore the runtime back-reference dropped on deserialization so the entry can reach the shared data.
            if (entry is ResourceEntryBase resourceEntry)
                resourceEntry.SetTable(this);
            if (entry.KeyId != 0)
                m_EntryCache[entry.KeyId] = entry;
        }
    }

    /// <summary>
    /// Returns the entry stored for a stable key id.
    /// </summary>
    /// <remarks>
    /// Looks the id up among this table's entries. Returns <see langword="null"/> when the id is <c>0</c> or when no
    /// entry exists for it. To narrow the result to a specific entry kind, use <see cref="GetEntry{T}(long)"/>.
    /// </remarks>
    /// <param name="keyId">The stable key id to look up.</param>
    /// <returns>The matching entry, or <see langword="null"/> when none exists.</returns>
    /// <example>
    /// <para>Look up an entry by the key id of a previously added string.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableGetEntryByIdExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry{T}(long)"/>
    /// <seealso cref="GetEntry(string)"/>
    /// <seealso cref="AddEntry(IResourceEntry)"/>
    public IResourceEntry GetEntry(long keyId) => keyId != 0 && Cache.TryGetValue(keyId, out var entry) ? entry : null;

    /// <summary>
    /// Returns the entry stored for a stable key id as a specific entry kind.
    /// </summary>
    /// <remarks>
    /// Casts the result of <see cref="GetEntry(long)"/> to <typeparamref name="T"/>. Returns <see langword="null"/>
    /// when no entry exists for the id or when the entry is not of that kind. Pass an entry class such as
    /// <see cref="StringEntry"/> or a view interface such as <see cref="IStringEntry"/>.
    /// </remarks>
    /// <typeparam name="T">The entry kind or view interface to return.</typeparam>
    /// <param name="keyId">The stable key id to look up.</param>
    /// <returns>The matching entry as <typeparamref name="T"/>, or <see langword="null"/> when it is missing or a different kind.</returns>
    /// <example>
    /// <para>Read a value by fetching the entry as a typed string entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableGetEntryOfTypeByIdExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry(long)"/>
    /// <seealso cref="GetEntry{T}(string)"/>
    /// <seealso cref="IStringEntry"/>
    public T GetEntry<T>(long keyId) where T : class, IResourceEntry => GetEntry(keyId) as T;

    /// <summary>
    /// Returns the entry stored for a key.
    /// </summary>
    /// <remarks>
    /// Resolves the key text to its stable id through <see cref="SharedData"/>, then returns the matching entry.
    /// Returns <see langword="null"/> when no shared data is assigned, the key is absent, or this table has no entry
    /// for it. To narrow the result to a specific entry kind, use <see cref="GetEntry{T}(string)"/>.
    /// </remarks>
    /// <param name="key">The key text to look up.</param>
    /// <returns>The matching entry, or <see langword="null"/> when none exists.</returns>
    /// <example>
    /// <para>Look up an entry by its key text.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableGetEntryByKeyExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry{T}(string)"/>
    /// <seealso cref="GetEntry(long)"/>
    /// <seealso cref="SharedData"/>
    public IResourceEntry GetEntry(string key)
    {
        var id = m_SharedData != null ? m_SharedData.GetId(key) : 0;
        return GetEntry(id);
    }

    /// <summary>
    /// Returns the entry stored for a key as a specific entry kind.
    /// </summary>
    /// <remarks>
    /// Resolves the key text through <see cref="SharedData"/>, then casts the result of <see cref="GetEntry(string)"/>
    /// to <typeparamref name="T"/>. Returns <see langword="null"/> when the key cannot be resolved, no entry exists,
    /// or the entry is not of that kind. Pass an entry class such as <see cref="StringEntry"/> or a view interface
    /// such as <see cref="IStringEntry"/>.
    /// </remarks>
    /// <typeparam name="T">The entry kind or view interface to return.</typeparam>
    /// <param name="key">The key text to look up.</param>
    /// <returns>The matching entry as <typeparamref name="T"/>, or <see langword="null"/> when it is missing or a different kind.</returns>
    /// <example>
    /// <para>Read a value by fetching the entry for a key as a typed string entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableGetEntryOfTypeByKeyExample.cs"/>
    /// </example>
    /// <seealso cref="GetEntry(string)"/>
    /// <seealso cref="GetEntry{T}(long)"/>
    /// <seealso cref="IStringEntry"/>
    public T GetEntry<T>(string key) where T : class, IResourceEntry => GetEntry(key) as T;

    /// <summary>
    /// Adds an entry, replacing any existing entry with the same key id.
    /// </summary>
    /// <remarks>
    /// The entry must already carry a non-zero key id, so build it against a key that exists in
    /// <see cref="SharedData"/>, for example one returned by <see cref="SharedTableData.AddKey(string)"/>. A
    /// <see langword="null"/> entry, or one whose key id is <c>0</c>, is ignored. To add a plain string without
    /// constructing the entry yourself, use <see cref="AddStringEntry(string, string, bool?)"/>.
    /// </remarks>
    /// <param name="entry">The entry to add.</param>
    /// <example>
    /// <para>Add a shared key, then store a string entry for it in this locale.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableAddEntryExample.cs"/>
    /// </example>
    /// <seealso cref="AddStringEntry(string, string, bool?)"/>
    /// <seealso cref="RemoveEntry(long)"/>
    /// <seealso cref="SharedTableData"/>
    public void AddEntry(IResourceEntry entry)
    {
        if (entry == null || entry.KeyId == 0)
            return;
        // A new key is the common case (import), so avoid the RemoveEntry scan unless one already exists.
        if (Cache.ContainsKey(entry.KeyId))
            RemoveEntry(entry.KeyId);
        m_Entries.Add(entry);
        m_EntryCache[entry.KeyId] = entry;
        if (entry is ResourceEntryBase resourceEntry)
            resourceEntry.SetTable(this);
    }

    /// <summary>
    /// Adds or updates a plain string entry for a key.
    /// </summary>
    /// <remarks>
    /// Adds the key to <see cref="SharedData"/> when it is new, then stores the value for this locale. When an entry
    /// already exists for the key, its value is overwritten. When <paramref name="isSmart"/> is supplied it is stored
    /// on the shared key, so it applies to every locale and can turn an existing key back into a plain string; when it
    /// is omitted the key's existing flag is left unchanged. Returns <see langword="null"/> when no shared data is
    /// assigned or the key is empty.
    /// </remarks>
    /// <param name="key">The key text; added to the shared data when new.</param>
    /// <param name="value">The localized value to store for this locale.</param>
    /// <param name="isSmart">Whether the value is a Smart String, formatted at resolve time; omit it to leave the key's flag unchanged.</param>
    /// <returns>The added or updated entry, or <see langword="null"/> when it cannot be added.</returns>
    /// <example>
    /// <para>Add plain and Smart String entries to a table.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableAddStringEntryExample.cs"/>
    /// </example>
    /// <seealso cref="AddEntry(IResourceEntry)"/>
    /// <seealso cref="GetEntry{T}(string)"/>
    /// <seealso cref="IStringEntry"/>
    public StringEntry AddStringEntry(string key, string value, bool? isSmart = null)
    {
        if (m_SharedData == null || string.IsNullOrEmpty(key))
            return null;
        var shared = m_SharedData.AddKey(key);
        if (shared == null)
            return null;
        if (isSmart.HasValue)
            shared.IsSmart = isSmart.Value;
        var id = shared.Id;
        if (GetEntry(id) is StringEntry existing)
        {
            existing.Value = value;
            return existing;
        }
        var entry = new StringEntry(id, value);
        AddEntry(entry);
        return entry;
    }

    /// <summary>
    /// Removes the entry for a stable key id from this table.
    /// </summary>
    /// <remarks>
    /// Removes only this locale's value. The key stays in <see cref="SharedData"/> and the other locale tables keep
    /// their entries. Does nothing when no entry matches the id. To drop the key from the whole collection, call
    /// <see cref="SharedTableData.RemoveKey(string)"/> as well.
    /// </remarks>
    /// <param name="keyId">The stable key id of the entry to remove.</param>
    /// <example>
    /// <para>Remove a single entry and confirm it is gone.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableRemoveEntryExample.cs"/>
    /// </example>
    /// <seealso cref="RemoveAllEntries()"/>
    /// <seealso cref="AddEntry(IResourceEntry)"/>
    /// <seealso cref="SharedTableData"/>
    public void RemoveEntry(long keyId)
    {
        if (keyId == 0)
            return;
        for (var i = m_Entries.Count - 1; i >= 0; i--)
        {
            if (m_Entries[i] != null && m_Entries[i].KeyId == keyId)
                m_Entries.RemoveAt(i);
        }
        m_EntryCache?.Remove(keyId);
    }

    /// <summary>
    /// Removes every entry from this table.
    /// </summary>
    /// <remarks>
    /// Clears only this locale's values. The shared keys in <see cref="SharedData"/> and the entries of the other
    /// locale tables are untouched.
    /// </remarks>
    /// <example>
    /// <para>Clear all of a table's per-locale values.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceTableRemoveAllEntriesExample.cs"/>
    /// </example>
    /// <seealso cref="RemoveEntry(long)"/>
    /// <seealso cref="Entries"/>
    /// <seealso cref="SharedTableData"/>
    public void RemoveAllEntries()
    {
        m_Entries.Clear();
        m_EntryCache = null;
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void InvalidateCache() => m_EntryCache = null;

    void ISerializationCallbackReceiver.OnBeforeSerialize() { }

    // Undo restores m_Entries through deserialization; a kept cache would serve deleted entries.
    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // The constructor does not run on deserialize, so guard against missing fields from older data.
        m_Entries ??= new List<IResourceEntry>();
        m_Metadata ??= new MetadataCollection();
        m_EntryCache = null;
    }
}
