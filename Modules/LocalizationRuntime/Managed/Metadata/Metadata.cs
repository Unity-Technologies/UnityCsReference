// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Marks a type as metadata that can be attached to a locale, a table, a shared key, or a per-locale entry.
/// </summary>
/// <remarks>
/// Implementations are serialized by reference through <see cref="MetadataCollection"/>, so each metadata item can carry its
/// own serialized fields, for example a <see cref="Comment"/> or an <see cref="ExcludeEntryFromEditorExport"/> flag. Apply
/// <see cref="MetadataAttribute"/> to an implementation to control where it can be attached and how it appears in the
/// editor's "Add Metadata" menu.
/// </remarks>
/// <example>
/// <para>Implement the interface to define a custom metadata item that carries its own fields.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/IMetadataExample.cs"/>
/// </example>
/// <seealso cref="MetadataCollection"/>
/// <seealso cref="MetadataAttribute"/>
/// <seealso cref="Comment"/>
/// <seealso cref="ExcludeEntryFromEditorExport"/>
public interface IMetadata
{
}

/// <summary>
/// Holds the metadata items attached to a single localization target.
/// </summary>
/// <remarks>
/// A collection is present on entries, shared keys, tables, and the shared data, so authors can attach comments and custom
/// properties (for example excluding an entry from export). Items are stored by reference, so any <see cref="IMetadata"/>
/// implementation keeps its own serialized fields. Query items by type with <see cref="GetMetadata{T}"/>,
/// <see cref="GetMetadatas{T}"/>, and <see cref="Contains{T}"/>, and change the contents with
/// <see cref="AddMetadata(IMetadata)"/> and <see cref="RemoveMetadata(IMetadata)"/>.
/// </remarks>
/// <example>
/// <para>Add a comment to a collection, then read it back by type.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionOverviewExample.cs"/>
/// </example>
/// <seealso cref="IMetadata"/>
/// <seealso cref="Comment"/>
/// <seealso cref="ExcludeEntryFromEditorExport"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public class MetadataCollection
{
    [SerializeReference]
    List<IMetadata> m_Items = new();

    /// <summary>
    /// The metadata items in this collection.
    /// </summary>
    public IReadOnlyList<IMetadata> Entries => m_Items;

    /// <summary>
    /// Whether any metadata is present.
    /// </summary>
    public bool HasData => m_Items.Count > 0;

    /// <summary>
    /// Returns the first metadata item of the given type.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GetMetadatas{T}"/> when a target can hold more than one item of the same type.
    /// </remarks>
    /// <typeparam name="T">The metadata type to find.</typeparam>
    /// <returns>The first item assignable to <typeparamref name="T"/>, or null when none is present.</returns>
    /// <example>
    /// <para>Read the first comment attached to a collection.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionGetMetadataExample.cs"/>
    /// </example>
    public T GetMetadata<T>() where T : class, IMetadata
    {
        for (var i = 0; i < m_Items.Count; i++)
        {
            if (m_Items[i] is T match)
                return match;
        }
        return null;
    }

    /// <summary>
    /// Enumerates every metadata item of the given type.
    /// </summary>
    /// <remarks>
    /// Items are returned in the order they were added. Use <see cref="GetMetadata{T}"/> when you only need the first match.
    /// </remarks>
    /// <typeparam name="T">The metadata type to enumerate.</typeparam>
    /// <returns>A sequence of the items assignable to <typeparamref name="T"/>.</returns>
    /// <example>
    /// <para>Iterate every comment attached to a collection.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionGetMetadatasExample.cs"/>
    /// </example>
    public IEnumerable<T> GetMetadatas<T>() where T : class, IMetadata
    {
        for (var i = 0; i < m_Items.Count; i++)
        {
            if (m_Items[i] is T match)
                yield return match;
        }
    }

    /// <summary>
    /// Checks whether a metadata item of the given type is present.
    /// </summary>
    /// <remarks>
    /// Equivalent to testing whether <see cref="GetMetadata{T}"/> returns a non-null value.
    /// </remarks>
    /// <typeparam name="T">The metadata type to test for.</typeparam>
    /// <returns><c>true</c> if at least one item is assignable to <typeparamref name="T"/>; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Check whether a collection already has a comment.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionContainsExample.cs"/>
    /// </example>
    public bool Contains<T>() where T : class, IMetadata => GetMetadata<T>() != null;

    /// <summary>
    /// Adds a metadata item to the collection.
    /// </summary>
    /// <remarks>
    /// A null item is ignored. Some metadata kinds are marked single-instance through <see cref="MetadataAttribute"/>. This
    /// method does not enforce that, so check with <see cref="Contains{T}"/> first when only one item of a type is wanted.
    /// </remarks>
    /// <param name="metadata">The item to add.</param>
    /// <example>
    /// <para>Attach a comment to a collection.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionAddMetadataExample.cs"/>
    /// </example>
    public void AddMetadata(IMetadata metadata)
    {
        if (metadata != null)
            m_Items.Add(metadata);
    }

    /// <summary>
    /// Removes a specific metadata item from the collection.
    /// </summary>
    /// <remarks>
    /// The item is removed by reference, so pass the same instance that was added.
    /// </remarks>
    /// <param name="metadata">The item to remove.</param>
    /// <returns><c>true</c> if the item was present and removed; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Remove a previously added metadata item.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Metadata/MetadataCollectionRemoveMetadataExample.cs"/>
    /// </example>
    public bool RemoveMetadata(IMetadata metadata) => m_Items.Remove(metadata);
}
