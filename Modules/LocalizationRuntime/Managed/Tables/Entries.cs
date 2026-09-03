// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

/// <summary>
/// Serves as the base class for the built-in resource entry kinds, holding the stable key id and per-entry metadata.
/// </summary>
/// <remarks>
/// This class implements the storage that every entry kind shares: the <see cref="KeyId"/> that ties the entry to a
/// shared key, and the <see cref="Metadata"/> for that entry. Concrete kinds add their value on top, for example
/// <see cref="StringEntry"/> for text or <see cref="AssetEntry"/> for an object reference. Derive from this class (or
/// from <see cref="AssetEntryBase{TStore}"/>) to add a custom entry kind. Entries are stored polymorphically in a
/// <see cref="ResourceTable"/>, so a new kind needs no change to the table.
/// </remarks>
/// <example>
/// <para>Read the shared members through a concrete string entry.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/ResourceEntryBaseOverviewExample.cs"/>
/// </example>
/// <seealso cref="StringEntry"/>
/// <seealso cref="AssetEntryBase{TStore}"/>
/// <seealso cref="IResourceEntry"/>
/// <seealso cref="ResourceTable"/>
[Serializable]
public abstract class ResourceEntryBase : IResourceEntry
{
    [SerializeField] long m_KeyId;
    [SerializeField] MetadataCollection m_Metadata = new();

    // Runtime back-references (not serialized): the owning table sets them so an entry can reach its shared data.
    ResourceTable m_Table;
    SharedTableData.SharedTableEntry m_SharedEntry;

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    protected ResourceEntryBase() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    protected ResourceEntryBase(long keyId) => m_KeyId = keyId;

    /// <inheritdoc/>
    public long KeyId => m_KeyId;

    // Import remaps entries onto the live collection's key ids when a key matched by name has a different id.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void SetKeyId(long keyId) => m_KeyId = keyId;

    /// <inheritdoc/>
    public MetadataCollection Metadata => m_Metadata;

    /// <inheritdoc/>
    public ResourceTable Table => m_Table;

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal void SetTable(ResourceTable table)
    {
        m_Table = table;
        m_SharedEntry = null;
    }

    /// <inheritdoc/>
    public SharedTableData.SharedTableEntry SharedEntry
        => m_SharedEntry ??= m_Table != null && m_Table.SharedData != null ? m_Table.SharedData.GetEntry(m_KeyId) : null;
}

/// <summary>
/// Represents a localized string entry that stores a single text value with no variants.
/// </summary>
/// <remarks>
/// This is the built-in <see cref="IStringEntry"/> kind, holding one locale's text for a key. Add one to a table with
/// <see cref="ResourceTable.AddStringEntry(string, string, bool?)"/>, which also registers the key in the shared data,
/// or construct one for an existing key id and add it with <see cref="ResourceTable.AddEntry(IResourceEntry)"/>.
/// Whether the value is treated as a Smart String is a per-key flag on the <see cref="SharedTableData"/>, not on the
/// entry.
/// </remarks>
/// <example>
/// <para>Add a string entry to a table and read its value.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/StringEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="IStringEntry"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="ResourceEntryBase"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public class StringEntry : ResourceEntryBase, IStringEntry
{
    [SerializeField, TextArea] string m_Value;

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public StringEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    /// <param name="value">The localized value.</param>
    public StringEntry(long keyId, string value = null) : base(keyId)
    {
        m_Value = value;
    }

    /// <inheritdoc/>
    public string Value
    {
        get => m_Value;
        set => m_Value = value;
    }
}

/// <summary>
/// A localized string entry with per-variant values, used when the key is variant-driven. The default value is
/// inherited from <see cref="StringEntry"/>; only variant keys add the extra storage.
/// </summary>
[Serializable]
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal sealed class VariantStringEntry : StringEntry, IVariantStringEntry
{
    [SerializeField] List<Variant<string>> m_Variants = new();

    /// <summary>
    /// Creates an empty entry.
    /// </summary>
    public VariantStringEntry() { }

    /// <summary>
    /// Creates an entry for a key id.
    /// </summary>
    /// <param name="keyId">The stable key id.</param>
    /// <param name="value">The default localized value.</param>
    public VariantStringEntry(long keyId, string value = null) : base(keyId, value) { }

    /// <summary>
    /// The per-variant values (the active key set is on the shared entry).
    /// </summary>
    public List<Variant<string>> Variants => m_Variants;

    /// <inheritdoc/>
    public void RemoveVariant(string key)
    {
        for (var i = m_Variants.Count - 1; i >= 0; i--)
        {
            if (m_Variants[i].Key == key)
                m_Variants.RemoveAt(i);
        }
    }

    /// <inheritdoc/>
    public string GetValue(string variantKey) => VariantResolver.Resolve(Value, variantKey, m_Variants);
}
