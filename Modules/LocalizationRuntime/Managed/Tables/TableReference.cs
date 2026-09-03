// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// A serializable reference to a table collection, identified either by its name or by its collection GUID.
/// </summary>
/// <remarks>
/// A table reference identifies a table collection without holding a direct object reference, which keeps it lightweight and
/// serializable. It stores either a collection name or a collection GUID; <see cref="ReferenceType"/> reports which form is in
/// use, and <see cref="IsEmpty"/> is <c>true</c> when neither is set. Assign one implicitly from a string to reference by name,
/// or call <see cref="FromGuid(string)"/> to reference by GUID. Name references are convenient in code, while GUID references
/// keep resolving after a collection is renamed. Pair a table reference with a <see cref="TableEntryReference"/> to address a
/// single entry, and resolve either against a <see cref="ResourceDatabase"/> or a <see cref="ResourceTable"/>.
/// </remarks>
/// <example>
/// <para>Create references by name and by GUID, then inspect how each identifies its collection.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceOverviewExample.cs"/>
/// </example>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="TableEntryReference"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public struct TableReference : IEquatable<TableReference>
{
    /// <summary>
    /// The way a table reference identifies its collection.
    /// </summary>
    /// <remarks>
    /// A <see cref="TableReference"/> stores either a name or a GUID, and this enumeration reports which form is present. Read
    /// it from <see cref="TableReference.ReferenceType"/> rather than setting it directly, because the value is derived from the
    /// stored data. <c>Empty</c> indicates the reference points at nothing.
    /// </remarks>
    /// <example>
    /// <para>Branch on how a reference identifies its collection.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceTypeSwitchExample.cs"/>
    /// </example>
    public enum Type
    {
        /// <summary>
        /// This reference points at no table collection.
        /// </summary>
        Empty,

        /// <summary>
        /// The collection is referenced by its GUID.
        /// </summary>
        Guid,

        /// <summary>
        /// The collection is referenced by its name.
        /// </summary>
        Name
    }

    [SerializeField] string m_TableCollectionName;
    [SerializeField] GUID m_TableCollectionNameGuid;

    /// <summary>
    /// How this reference identifies its collection.
    /// </summary>
    public Type ReferenceType
    {
        get
        {
            if (!m_TableCollectionNameGuid.Empty())
                return Type.Guid;
            if (!string.IsNullOrEmpty(m_TableCollectionName))
                return Type.Name;
            return Type.Empty;
        }
    }

    /// <summary>
    /// The name of the referenced collection, or null when the reference is empty or GUID-based.
    /// </summary>
    public string TableCollectionName => m_TableCollectionName;

    /// <summary>
    /// The GUID of the referenced collection, or an empty GUID when the reference is empty or name-based.
    /// </summary>
    public GUID TableCollectionNameGuid => m_TableCollectionNameGuid;

    /// <summary>
    /// Whether this reference points at nothing.
    /// </summary>
    public bool IsEmpty => ReferenceType == Type.Empty;

    /// <summary>
    /// Converts a collection name into a name-based table reference.
    /// </summary>
    /// <remarks>
    /// Lets you assign a string wherever a <see cref="TableReference"/> is expected. The result has a
    /// <see cref="ReferenceType"/> of <c>Name</c>. An empty or null string produces an empty reference.
    /// </remarks>
    /// <param name="tableCollectionName">The name of the table collection to reference.</param>
    /// <returns>A table reference that identifies the collection by name.</returns>
    /// <example>
    /// <para>Reference a collection by name through the implicit conversion.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceFromNameExample.cs"/>
    /// </example>
    public static implicit operator TableReference(string tableCollectionName)
        => new() { m_TableCollectionName = tableCollectionName };

    /// <summary>
    /// Creates a table reference that identifies a collection by its GUID.
    /// </summary>
    /// <remarks>
    /// GUID references keep resolving after a collection is renamed, unlike name references. Dashed or differently cased
    /// input is accepted and canonicalized, so it compares equal to its plain hex form. The result has a
    /// <see cref="ReferenceType"/> of <c>Guid</c>, or <c>Empty</c> when the string is not a valid GUID.
    /// </remarks>
    /// <param name="tableCollectionNameGuid">The GUID of the table collection to reference.</param>
    /// <returns>A table reference that identifies the collection by GUID.</returns>
    /// <example>
    /// <para>Reference a collection by GUID so it survives a rename.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceFromGuidExample.cs"/>
    /// </example>
    public static TableReference FromGuid(string tableCollectionNameGuid)
        => new() { m_TableCollectionNameGuid = ParseGuid(tableCollectionNameGuid) };

    /// <summary>
    /// Creates a table reference that identifies a collection by its GUID.
    /// </summary>
    /// <remarks>
    /// Use this overload when you already hold a <see cref="UnityEngine.GUID"/>, for example one returned by the asset
    /// database. The result has a <see cref="ReferenceType"/> of <c>Guid</c>, or <c>Empty</c> when the GUID is empty.
    /// </remarks>
    /// <param name="tableCollectionNameGuid">The GUID of the table collection to reference.</param>
    /// <returns>A table reference that identifies the collection by GUID.</returns>
    /// <example>
    /// <para>Reference a collection from a GUID you already hold.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceFromTypedGuidExample.cs"/>
    /// </example>
    public static TableReference FromGuid(GUID tableCollectionNameGuid)
        => new() { m_TableCollectionNameGuid = tableCollectionNameGuid };

    static GUID ParseGuid(string guid)
        => System.Guid.TryParse(guid, out var parsed) ? new GUID(parsed.ToString("N")) : default;

    /// <summary>
    /// Checks whether this reference identifies the same collection as another.
    /// </summary>
    /// <remarks>
    /// References compare the way they resolve: a GUID reference compares by GUID and ignores any name text (the GUID
    /// wins during resolution too), and a name reference compares by ordinal name. References of different forms are
    /// not equal. Because <see cref="FromGuid(string)"/> canonicalizes its input, a dashed or differently cased GUID
    /// compares equal to its plain hex form. Empty references are equal to each other.
    /// </remarks>
    /// <param name="other">The table reference to compare with this one.</param>
    /// <returns><c>true</c> if both references identify the same collection; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare a name reference against a GUID reference for the same collection.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceEqualsExample.cs"/>
    /// </example>
    public bool Equals(TableReference other)
    {
        var type = ReferenceType;
        if (type != other.ReferenceType)
            return false;
        return type switch
        {
            Type.Guid => m_TableCollectionNameGuid.Equals(other.m_TableCollectionNameGuid),
            Type.Name => string.Equals(m_TableCollectionName, other.m_TableCollectionName, StringComparison.Ordinal),
            _ => true
        };
    }

    /// <summary>
    /// Checks whether this reference equals another object.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when <paramref name="obj"/> is not a <see cref="TableReference"/>. Otherwise, defers to
    /// <see cref="Equals(TableReference)"/> for value comparison.
    /// </remarks>
    /// <param name="obj">The object to compare with this reference.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is a table reference that identifies the same collection; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare against a boxed reference.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceEqualsObjectExample.cs"/>
    /// </example>
    public override bool Equals(object obj) => obj is TableReference other && Equals(other);

    /// <summary>
    /// Computes a hash code for this reference.
    /// </summary>
    /// <remarks>
    /// Consistent with <see cref="Equals(TableReference)"/>: references that compare equal produce the same hash code,
    /// including GUIDs that differ only by case or dashes, because <see cref="FromGuid(string)"/> canonicalizes them.
    /// Safe to use as a dictionary key.
    /// </remarks>
    /// <returns>An integer hash code derived from the part of the reference that resolution uses.</returns>
    /// <example>
    /// <para>Use a table reference as a dictionary key.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceHashCodeExample.cs"/>
    /// </example>
    public override int GetHashCode()
    {
        return ReferenceType switch
        {
            Type.Guid => m_TableCollectionNameGuid.GetHashCode(),
            Type.Name => StringComparer.Ordinal.GetHashCode(m_TableCollectionName),
            _ => 0
        };
    }

    /// <summary>
    /// Returns a readable string describing this reference.
    /// </summary>
    /// <remarks>
    /// Formats a GUID reference as "Guid:" followed by the GUID, a name reference as the name itself, and an empty reference
    /// as &lt;none&gt;. Intended for debugging and logging.
    /// </remarks>
    /// <returns>A human-readable description of the reference.</returns>
    /// <example>
    /// <para>Log the reference for debugging.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableReferenceToStringExample.cs"/>
    /// </example>
    public override string ToString()
    {
        return ReferenceType switch
        {
            Type.Guid => $"Guid:{m_TableCollectionNameGuid}",
            Type.Name => m_TableCollectionName,
            _ => "<none>"
        };
    }
}

/// <summary>
/// A serializable reference to a single table entry, identified either by its key text or by its stable key id.
/// </summary>
/// <remarks>
/// An entry reference addresses one row inside a table collection without holding a direct object reference. It stores either
/// the key text or the numeric key id; <see cref="ReferenceType"/> reports which form is present, and <see cref="IsEmpty"/> is
/// <c>true</c> when neither is set. Key ids are stable across key renames, so they are the safer choice for serialized data,
/// while key text is more readable in code. Convert between the two forms with <see cref="ResolveKeyId(SharedTableData)"/> and
/// <see cref="ResolveKeyName(SharedTableData)"/>, passing the collection's <see cref="SharedTableData"/>. Combine it with a
/// <see cref="TableReference"/> to address an entry in a specific collection.
/// </remarks>
/// <example>
/// <para>Create entry references by key text and by key id, then resolve one to an id.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceOverviewExample.cs"/>
/// </example>
/// <seealso cref="TableReference"/>
/// <seealso cref="ResourceDatabase"/>
/// <seealso cref="ResourceTable"/>
[Serializable]
public struct TableEntryReference : IEquatable<TableEntryReference>
{
    /// <summary>
    /// The way a table entry reference identifies its entry.
    /// </summary>
    /// <remarks>
    /// A <see cref="TableEntryReference"/> stores either a key name or a numeric key id, and this enumeration reports which
    /// form is present. Read it from <see cref="TableEntryReference.ReferenceType"/>; the value is derived from the stored data
    /// rather than set directly. <c>Empty</c> indicates the reference points at no entry.
    /// </remarks>
    /// <example>
    /// <para>Branch on how an entry reference identifies its entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceTypeSwitchExample.cs"/>
    /// </example>
    public enum Type
    {
        /// <summary>
        /// This reference points at no table entry.
        /// </summary>
        Empty,

        /// <summary>
        /// The entry is referenced by its key text.
        /// </summary>
        Name,

        /// <summary>
        /// The entry is referenced by its stable key id.
        /// </summary>
        Id
    }

    [SerializeField] long m_KeyId;
    [SerializeField] string m_Key;

    /// <summary>
    /// How this reference identifies its entry.
    /// </summary>
    public Type ReferenceType
    {
        get
        {
            if (m_KeyId != 0)
                return Type.Id;
            if (!string.IsNullOrEmpty(m_Key))
                return Type.Name;
            return Type.Empty;
        }
    }

    /// <summary>
    /// The stable key id of the referenced entry, or 0 when the reference is empty or name-based.
    /// </summary>
    public long KeyId => m_KeyId;

    /// <summary>
    /// The key text of the referenced entry, or null when the reference is empty or id-based.
    /// </summary>
    public string Key => m_Key;

    /// <summary>
    /// Whether this reference points at nothing.
    /// </summary>
    public bool IsEmpty => ReferenceType == Type.Empty;

    /// <summary>
    /// Converts key text into a name-based entry reference.
    /// </summary>
    /// <remarks>
    /// Lets you assign a string wherever a <see cref="TableEntryReference"/> is expected. The result has a
    /// <see cref="ReferenceType"/> of <c>Name</c>. An empty or null string produces an empty reference.
    /// </remarks>
    /// <param name="key">The key text of the entry to reference.</param>
    /// <returns>An entry reference that identifies the entry by key text.</returns>
    /// <example>
    /// <para>Reference an entry by key text through the implicit conversion.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceFromKeyExample.cs"/>
    /// </example>
    public static implicit operator TableEntryReference(string key) => new() { m_Key = key };

    /// <summary>
    /// Converts a key id into an id-based entry reference.
    /// </summary>
    /// <remarks>
    /// Lets you assign a long wherever a <see cref="TableEntryReference"/> is expected. The result has a
    /// <see cref="ReferenceType"/> of <c>Id</c>. Key ids are stable across key renames.
    /// </remarks>
    /// <param name="keyId">The stable key id of the entry to reference.</param>
    /// <returns>An entry reference that identifies the entry by key id.</returns>
    /// <example>
    /// <para>Reference an entry by key id through the implicit conversion.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceFromKeyIdExample.cs"/>
    /// </example>
    public static implicit operator TableEntryReference(long keyId) => new() { m_KeyId = keyId };

    /// <summary>
    /// Resolves this reference to a numeric key id using the given shared table data.
    /// </summary>
    /// <remarks>
    /// When the reference already holds an id, that id is returned directly. When it holds key text, the id is looked up in
    /// <paramref name="sharedData"/>. Returns <c>0</c> when the reference is empty, or when the key text is not present in
    /// <paramref name="sharedData"/>.
    /// </remarks>
    /// <param name="sharedData">The shared table data to resolve the key against.</param>
    /// <returns>The resolved key id, or <c>0</c> when the reference cannot be resolved.</returns>
    /// <example>
    /// <para>Resolve an entry reference to a stable key id.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceResolveKeyIdExample.cs"/>
    /// </example>
    public long ResolveKeyId(SharedTableData sharedData)
    {
        if (ReferenceType == Type.Id)
            return m_KeyId;
        if (ReferenceType == Type.Name && sharedData != null)
            return sharedData.GetId(m_Key);
        return 0;
    }

    /// <summary>
    /// Resolves this reference to a key name using the given shared table data.
    /// </summary>
    /// <remarks>
    /// When the reference already holds key text, that text is returned directly. When it holds a key id, the name is looked
    /// up in <paramref name="sharedData"/>. Returns null when the reference is empty, or when the id is not present in
    /// <paramref name="sharedData"/>.
    /// </remarks>
    /// <param name="sharedData">The shared table data to resolve the key against.</param>
    /// <returns>The resolved key text, or null when the reference cannot be resolved.</returns>
    /// <example>
    /// <para>Resolve an entry reference to its human-readable key text.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceResolveKeyNameExample.cs"/>
    /// </example>
    public string ResolveKeyName(SharedTableData sharedData)
    {
        if (ReferenceType == Type.Name)
            return m_Key;
        if (ReferenceType == Type.Id && sharedData != null)
            return sharedData.GetKey(m_KeyId);
        return null;
    }

    /// <summary>
    /// Checks whether this reference identifies the same entry as another.
    /// </summary>
    /// <remarks>
    /// References compare the way they resolve: an id reference compares by key id and ignores any key text (the id
    /// wins during resolution too), and a name reference compares by ordinal key text. References of different forms
    /// are not equal. Empty references are equal to each other.
    /// </remarks>
    /// <param name="other">The entry reference to compare with this one.</param>
    /// <returns><c>true</c> if both references identify the same entry; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two entry references for value equality.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceEqualsExample.cs"/>
    /// </example>
    public bool Equals(TableEntryReference other)
    {
        var type = ReferenceType;
        if (type != other.ReferenceType)
            return false;
        return type switch
        {
            Type.Id => m_KeyId == other.m_KeyId,
            Type.Name => string.Equals(m_Key, other.m_Key, StringComparison.Ordinal),
            _ => true
        };
    }

    /// <summary>
    /// Checks whether this reference equals another object.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when <paramref name="obj"/> is not a <see cref="TableEntryReference"/>. Otherwise, defers to
    /// <see cref="Equals(TableEntryReference)"/> for value comparison.
    /// </remarks>
    /// <param name="obj">The object to compare with this reference.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an entry reference that identifies the same entry; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare against a boxed reference.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceEqualsObjectExample.cs"/>
    /// </example>
    public override bool Equals(object obj) => obj is TableEntryReference other && Equals(other);

    /// <summary>
    /// Computes a hash code for this reference.
    /// </summary>
    /// <remarks>
    /// Consistent with <see cref="Equals(TableEntryReference)"/>: references that compare equal produce the same hash code.
    /// Safe to use as a dictionary key.
    /// </remarks>
    /// <returns>An integer hash code derived from the part of the reference that resolution uses.</returns>
    /// <example>
    /// <para>Use an entry reference as a dictionary key.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceHashCodeExample.cs"/>
    /// </example>
    public override int GetHashCode()
    {
        return ReferenceType switch
        {
            Type.Id => m_KeyId.GetHashCode(),
            Type.Name => StringComparer.Ordinal.GetHashCode(m_Key),
            _ => 0
        };
    }

    /// <summary>
    /// Returns a readable string describing this reference.
    /// </summary>
    /// <remarks>
    /// Formats an id reference as "Id:" followed by the id, a name reference as the key text itself, and an empty reference as
    /// &lt;none&gt;. Intended for debugging and logging.
    /// </remarks>
    /// <returns>A human-readable description of the reference.</returns>
    /// <example>
    /// <para>Log the entry reference for debugging.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Components/TableEntryReferenceToStringExample.cs"/>
    /// </example>
    public override string ToString()
    {
        return ReferenceType switch
        {
            Type.Id => $"Id:{m_KeyId}",
            Type.Name => m_Key,
            _ => "<none>"
        };
    }
}
