// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// A flat, serializable snapshot of one per-locale table.
/// </summary>
/// <remarks>
/// The shape is deliberately non-polymorphic (no <c>[SerializeReference]</c>) so <see cref="UnityEngine.JsonUtility"/>
/// and other simple serializers round-trip it, and self-contained so a single file rebuilds a usable table. The
/// provider turns a snapshot back into a <see cref="ResourceTable"/> and its <see cref="SharedTableData"/>. Each
/// element of <see cref="Entries"/> is one key together with this locale's value, so the file reads as one row per
/// entry rather than parallel key and value lists.
/// </remarks>
/// <seealso cref="ITableFileReader"/>
[Serializable]
public class ResourceTableData
{
    /// <summary>
    /// The format version, so a reader can reject or migrate older data.
    /// </summary>
    public string SchemaVersion = "3";

    /// <summary>
    /// The collection name this table belongs to.
    /// </summary>
    public string CollectionName;

    /// <summary>
    /// The stable collection guid, used as the address the runtime resolves the table under.
    /// </summary>
    /// <remarks>
    /// Stored as the 32-character hexadecimal form of a <see cref="UnityEngine.GUID"/> so the file stays readable and
    /// portable; the converter parses it back into a typed guid.
    /// </remarks>
    public string CollectionGuid;

    /// <summary>
    /// The locale code this table targets.
    /// </summary>
    public string LocaleCode;

    /// <summary>
    /// The entries in this table, one row per key, carrying both the shared key data and this locale's value.
    /// </summary>
    public List<EntryData> Entries = new();
}

/// <summary>
/// One row of a snapshot: a key's shared data plus this locale's value, either a string or a serialized entry.
/// </summary>
/// <remarks>
/// The value is a string entry when <see cref="TypeName"/> is empty, using <see cref="Value"/> and
/// <see cref="Variants"/>; otherwise it is a non-string entry that implements <see cref="IFileDataEntry"/>,
/// reconstructed from <see cref="TypeName"/> and <see cref="EntryJson"/>. The shared fields (<see cref="Key"/>,
/// <see cref="IsSmart"/>, <see cref="VariantKeys"/>, <see cref="Selector"/>) are the same across every locale and are
/// repeated in each per-locale file so a single file rebuilds the shared data it needs.
/// </remarks>
[Serializable]
public class EntryData
{
    /// <summary>
    /// The stable key id, preserved so references by id keep resolving.
    /// </summary>
    public long Id;

    /// <summary>
    /// The key text.
    /// </summary>
    public string Key;

    /// <summary>
    /// Whether the key's string values are Smart Strings.
    /// </summary>
    public bool IsSmart;

    /// <summary>
    /// The active variant keys for this key.
    /// </summary>
    public List<string> VariantKeys = new();

    /// <summary>
    /// The variant selector for this key, or a default when the key is plain.
    /// </summary>
    /// <remarks>
    /// The key is plain when the selector's <see cref="SelectorData.TypeName"/> is empty.
    /// </remarks>
    public SelectorData Selector = new();

    /// <summary>
    /// The default string value, used when the entry is a string entry and no variant matches.
    /// </summary>
    public string Value;

    /// <summary>
    /// The per-variant string values for a string entry.
    /// </summary>
    public List<VariantData> Variants = new();

    /// <summary>
    /// The assembly-qualified type name of a non-string entry, or empty when the row is a string entry.
    /// </summary>
    public string TypeName;

    /// <summary>
    /// A non-string entry's own fields as JSON.
    /// </summary>
    /// <remarks>
    /// Restored onto a fresh instance with <see cref="UnityEngine.JsonUtility"/>. Only meaningful when
    /// <see cref="TypeName"/> is set.
    /// </remarks>
    public string EntryJson;
}

/// <summary>
/// A serialized variant selector: its type plus the selector's own serialized fields.
/// </summary>
[Serializable]
public struct SelectorData
{
    /// <summary>
    /// The selector's assembly-qualified type name, or empty when there is no selector.
    /// </summary>
    public string TypeName;

    /// <summary>
    /// The selector's fields as JSON.
    /// </summary>
    /// <remarks>
    /// Restored onto a fresh instance with <see cref="UnityEngine.JsonUtility"/>.
    /// </remarks>
    public string Json;
}

/// <summary>
/// One variant value: the selector key it applies to and the string payload.
/// </summary>
[Serializable]
public struct VariantData
{
    /// <summary>
    /// The selector key this value applies to.
    /// </summary>
    public string Key;

    /// <summary>
    /// The string payload.
    /// </summary>
    public string Value;
}
