// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.Localization.Editor;

/// <summary>
/// Represents the source asset for a set of localized tables: shared key data plus one table per locale.
/// </summary>
/// <remarks>
/// A collection groups a single <see cref="SharedTableData"/> with one <see cref="ResourceTable"/> per locale.
/// Authoring edits the per-locale tables through the collection; at runtime the <see cref="ResourceDatabase"/>
/// resolves through the individual tables it has registered, not the collection, which is editor-only.
/// </remarks>
public sealed class ResourceTableCollection : ScriptableObject
{
    [SerializeField] SharedTableData m_SharedData;
    [SerializeField] List<ResourceTable> m_Tables = new();
    [SerializeField] string m_ProviderId;

    /// <summary>
    /// Gets or sets the shared key data for the collection.
    /// </summary>
    public SharedTableData SharedData
    {
        get => m_SharedData;
        set
        {
            if (value != null && !ReferenceEquals(value, m_SharedData) && OwnedByAnotherCollection(value))
            {
                Debug.LogWarning($"Shared table data '{value.name}' already belongs to another table collection and was not assigned to '{name}'.", this);
                return;
            }
            m_SharedData = value;
            for (var i = 0; i < m_Tables.Count; i++)
            {
                if (m_Tables[i] != null)
                    m_Tables[i].SharedData = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the identifier of the asset provider this collection registers its tables and assets with.
    /// </summary>
    /// <remarks>
    /// The identifier is the provider type name resolved through the settings provider chain.
    /// </remarks>
    public string ProviderId
    {
        get => m_ProviderId;
        internal set => m_ProviderId = value;
    }

    /// <summary>
    /// Gets the collection name.
    /// </summary>
    /// <remarks>
    /// Resolved through the shared data name, falling back to the asset name when the shared data has no name.
    /// </remarks>
    public string TableCollectionName => m_SharedData != null && !string.IsNullOrEmpty(m_SharedData.TableCollectionName)
        ? m_SharedData.TableCollectionName
        : name;

    /// <summary>
    /// Gets the per-locale tables in this collection.
    /// </summary>
    public IReadOnlyList<ResourceTable> Tables => m_Tables;

    /// <summary>
    /// Adds a per-locale table to the collection.
    /// </summary>
    /// <param name="table">
    /// The table to add. Ignored when null, already present, built against different shared data, owned by another
    /// collection, or another table for its locale is already in the collection.
    /// </param>
    public void AddTable(ResourceTable table)
    {
        if (table == null || m_Tables.Contains(table))
            return;
        if (m_SharedData != null && table.SharedData != null && table.SharedData != m_SharedData)
            return;
        if (GetTable(table.LocaleIdentifier) != null)
            return;
        if (OwnedByAnotherCollection(table))
            return;
        m_Tables.Add(table);
    }

    bool OwnedByAnotherCollection(SharedTableData shared)
    {
        foreach (var other in AssetProviderEditors.FindAllCollections())
        {
            if (other != null && !ReferenceEquals(other, this) && other.SharedData == shared)
                return true;
        }
        return false;
    }

    bool OwnedByAnotherCollection(ResourceTable table)
    {
        foreach (var other in AssetProviderEditors.FindAllCollections())
        {
            if (other == null || ReferenceEquals(other, this))
                continue;
            var tables = other.Tables;
            for (var i = 0; i < tables.Count; i++)
            {
                if (ReferenceEquals(tables[i], table))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns the table for a locale.
    /// </summary>
    /// <param name="locale">The locale to find.</param>
    /// <returns>The matching <see cref="ResourceTable"/>, or <see langword="null"/> when none exists.</returns>
    public ResourceTable GetTable(LocaleIdentifier locale)
    {
        for (var i = 0; i < m_Tables.Count; i++)
        {
            if (m_Tables[i] != null && m_Tables[i].LocaleIdentifier == locale)
                return m_Tables[i];
        }
        return null;
    }
}
