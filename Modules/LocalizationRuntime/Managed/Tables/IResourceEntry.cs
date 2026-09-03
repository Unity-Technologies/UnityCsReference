// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// Represents a single entry in a resource table, identified by a stable key id and carrying per-entry metadata.
/// </summary>
/// <remarks>
/// An entry holds one locale's value for a key. Entries are stored polymorphically in a <see cref="ResourceTable"/>
/// through <c>[SerializeReference]</c>, so string, asset, and custom kinds share one entry list and new kinds drop in
/// without changing the table. <see cref="KeyId"/> ties the entry to a shared key in the collection's
/// <see cref="SharedTableData"/>, and <see cref="Metadata"/> holds comments and custom properties for that entry.
/// Fetch an entry from a table with <see cref="ResourceTable.GetEntry(long)"/> or its overloads, then work with it
/// through the value interface that matches its kind, <see cref="IStringEntry"/> or <see cref="IAssetEntry"/>.
/// </remarks>
/// <example>
/// <para>Read the key id and metadata of an entry fetched from a table.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IResourceEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="IStringEntry"/>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="ResourceEntryBase"/>
/// <seealso cref="SharedTableData"/>
public interface IResourceEntry
{
    /// <summary>
    /// The stable key id this entry provides a value for.
    /// </summary>
    /// <remarks>
    /// The id is assigned by the collection's <see cref="SharedTableData"/> and never changes for the life of the key,
    /// so a renamed key keeps its entries. An id of <c>0</c> means the entry is not bound to a key.
    /// </remarks>
    long KeyId { get; }

    /// <summary>
    /// The metadata attached to this entry, such as comments or custom properties.
    /// </summary>
    /// <remarks>
    /// This metadata is specific to one locale's entry. Key-wide metadata that every locale shares lives on the
    /// <see cref="SharedTableData.SharedTableEntry"/> instead.
    /// </remarks>
    MetadataCollection Metadata { get; }

    /// <summary>
    /// The table this entry belongs to.
    /// </summary>
    /// <remarks>
    /// The owning <see cref="ResourceTable"/> sets this when the entry is added or when the table is loaded, so a
    /// resolved entry can reach its table's <see cref="ResourceTable.SharedData"/> and per-locale data. It is
    /// <see langword="null"/> for an entry that is not part of a table.
    /// </remarks>
    ResourceTable Table { get; }

    /// <summary>
    /// The shared key data for this entry, common to the key across every locale.
    /// </summary>
    /// <remarks>
    /// Resolved from the <see cref="Table"/>'s <see cref="ResourceTable.SharedData"/> by <see cref="KeyId"/>, so it
    /// carries the key text and per-key flags such as <see cref="SharedTableData.SharedTableEntry.IsSmart"/>. It is
    /// <see langword="null"/> when the entry has no <see cref="Table"/> or the key is absent from the shared data.
    /// </remarks>
    SharedTableData.SharedTableEntry SharedEntry { get; }
}

/// <summary>
/// Represents a resource entry whose localized value is a string.
/// </summary>
/// <remarks>
/// A string entry stores the text for one key in one locale, returned by <see cref="Value"/> before any Smart String
/// formatting is applied. Whether the value is formatted as a Smart String is a per-key flag on the collection's
/// <see cref="SharedTableData"/>, not a property of the entry. The built-in implementation is <see cref="StringEntry"/>;
/// fetch one from a table with <see cref="ResourceTable.GetEntry{T}(string)"/> using <see cref="IStringEntry"/> or
/// <see cref="StringEntry"/> as the type argument.
/// </remarks>
/// <example>
/// <para>Read the localized text through the string entry view.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IStringEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="StringEntry"/>
/// <seealso cref="IResourceEntry"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="SharedTableData"/>
public interface IStringEntry : IResourceEntry
{
    /// <summary>
    /// The localized text for this entry, before Smart String formatting.
    /// </summary>
    /// <remarks>
    /// For a variant-driven key this is the default value that applies when no variant matches. The Smart String flag
    /// and the active variant keys are stored per-key on the shared data, not on the entry.
    /// </remarks>
    string Value { get; }
}

/// <summary>
/// Represents a resource entry whose localized value is an asset that the entry loads and releases itself.
/// </summary>
/// <remarks>
/// Each concrete kind owns how its asset is referenced and resolved: <see cref="AssetEntry"/> keeps a direct object
/// reference, <see cref="ResourceAssetEntry"/> stores a <c>Resources</c> path, and a package can add other kinds such
/// as an Addressables-backed entry. An entry opts into loading by implementing <see cref="IAsyncAssetEntry"/>,
/// <see cref="ISynchronousAssetEntry"/>, or both; the localization system prefers the asynchronous version and falls
/// back to the synchronous one. Every loaded asset should be handed back with
/// <see cref="ReleaseAsset(UnityEngine.Object)"/> so the owning kind can free it. Fetch an asset entry from a table
/// with <see cref="ResourceTable.GetEntry{T}(string)"/> using <see cref="IAssetEntry"/> as the type argument.
/// </remarks>
/// <example>
/// <para>Check whether an asset entry has a value assigned.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IAssetEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="AssetEntry"/>
/// <seealso cref="ResourceAssetEntry"/>
/// <seealso cref="AssetEntryBase{TStore}"/>
/// <seealso cref="IAsyncAssetEntry"/>
/// <seealso cref="ISynchronousAssetEntry"/>
public interface IAssetEntry : IResourceEntry
{
    /// <summary>
    /// Returns whether this entry has an asset assigned.
    /// </summary>
    /// <remarks>
    /// Tests the stored reference without loading anything, so it is cheap to call before resolving the asset.
    /// </remarks>
    /// <returns><c>true</c> when an asset is assigned; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Skip the load when the entry has no asset.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IAssetEntryHasAssetExample.cs"/>
    /// </example>
    /// <seealso cref="ReleaseAsset(UnityEngine.Object)"/>
    bool HasAsset();

    /// <summary>
    /// Releases an asset previously returned by a load.
    /// </summary>
    /// <remarks>
    /// Hand every loaded asset back to the same entry so the owning kind can free it. <see cref="AssetEntry"/> holds a
    /// direct reference and does nothing, while <see cref="ResourceAssetEntry"/> unloads through
    /// <see cref="UnityEngine.Resources"/>.
    /// </remarks>
    /// <param name="asset">The asset to release.</param>
    /// <example>
    /// <para>Release an asset once you are finished with it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IAssetEntryReleaseAssetExample.cs"/>
    /// </example>
    /// <seealso cref="HasAsset()"/>
    void ReleaseAsset(Object asset);
}

/// <summary>
/// An optional capability for an asset entry that loads its asset asynchronously.
/// </summary>
/// <remarks>
/// Implement this alongside <see cref="IAssetEntry"/> when the entry loads through an asynchronous operation, such as a
/// Resources request or an Addressables handle. The localization system calls this on the asynchronous resolve paths.
/// An entry that already has its asset in hand implements <see cref="ISynchronousAssetEntry"/> instead; the built-in
/// entries are synchronous only.
/// </remarks>
/// <example>
/// <para>Load an asset entry and log the result.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IAsyncAssetEntryLoadAssetAsyncExample.cs"/>
/// </example>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="ISynchronousAssetEntry"/>
public interface IAsyncAssetEntry : IAssetEntry
{
    /// <summary>
    /// Loads the asset assigned to this entry.
    /// </summary>
    /// <remarks>
    /// The load is resolved by the concrete kind. The result is <see langword="null"/> when no asset is assigned or it
    /// cannot be resolved. Release the returned asset with <see cref="IAssetEntry.ReleaseAsset(UnityEngine.Object)"/>
    /// once you are finished with it.
    /// </remarks>
    /// <param name="cancellationToken">A token that cancels the load.</param>
    /// <returns>An awaitable that resolves to the loaded asset, or <see langword="null"/> on a miss.</returns>
    Awaitable<Object> LoadAssetAsync(CancellationToken cancellationToken);
}

/// <summary>
/// An optional capability for an asset entry that can resolve its asset immediately, without an asynchronous load.
/// </summary>
/// <remarks>
/// Implement this alongside <see cref="IAssetEntry"/> when the entry already has its asset in hand, such as a direct
/// object reference or a synchronous <c>Resources</c> load, so <see cref="LocalizedAsset{TObject}"/> can resolve it
/// on the synchronous path (the <c>GetLocalizedAsset</c> accessor, or under the <see cref="LoadingPreference.Synchronous"/>
/// loading preference). An entry kind that can only load asynchronously (for example an Addressables-backed kind provided by a
/// package) does not implement this interface, and the synchronous path falls back to whatever is already cached.
/// </remarks>
/// <example>
/// <para>The built-in asset entries implement this, so a localized asset backed by them resolves synchronously.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/SynchronousAssetEntryExample.cs"/>
/// </example>
/// <seealso cref="IAssetEntry"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
public interface ISynchronousAssetEntry
{
    /// <summary>
    /// Tries to resolve the entry's asset without loading asynchronously.
    /// </summary>
    /// <remarks>
    /// Return <see langword="true"/> and set <paramref name="asset"/> when the asset resolves immediately; return
    /// <see langword="false"/> on a miss and leave <paramref name="asset"/> as <see langword="null"/>.
    /// </remarks>
    /// <param name="asset">The resolved asset, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the asset resolved synchronously; otherwise <see langword="false"/>.</returns>
    bool TryLoadAsset(out Object asset);
}

/// <summary>
/// Marks a resource entry whose stored value is plain data that round-trips through JSON, so a file-backed table
/// provider can export it to a data file and rebuild it at runtime without shipping the authored table asset.
/// </summary>
/// <remarks>
/// An entry that implements this interface serializes losslessly through <see cref="UnityEngine.JsonUtility"/>: it
/// holds no <see cref="UnityEngine.Object"/> references, and whatever it points at is reachable in a player on its
/// own, such as a Resources path or an Addressables address supplied by a package. A file-backed provider writes
/// these entries into its data files; a table whose entries do not all satisfy this contract keeps its asset in the
/// build so the remaining references survive. Per-entry <see cref="IResourceEntry.Metadata"/> does not round-trip
/// through file data. <see cref="ResourceAssetEntry"/> implements this because it stores a Resources path, while
/// <see cref="AssetEntry"/> does not, because it holds a direct object reference.
/// </remarks>
/// <example>
/// <para>Check whether an entry can be exported to a localization data file.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Tables/IFileDataEntryOverviewExample.cs"/>
/// </example>
/// <seealso cref="IResourceEntry"/>
/// <seealso cref="ResourceAssetEntry"/>
/// <seealso cref="AssetEntry"/>
public interface IFileDataEntry : IResourceEntry
{
}

/// <summary>
/// A per-locale entry that holds variant values. The variant structure (which selector, which keys) is per-key
/// on the shared data (<see cref="SharedTableData.SharedTableEntry"/>); this manages the local variant values.
/// </summary>
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal interface IVariantEntry : IResourceEntry
{
    /// <summary>
    /// Removes the per-locale value stored for <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The selector key.</param>
    void RemoveVariant(string key);
}

/// <summary>
/// A string entry that resolves per-variant values. The caller passes the current variant key (from the shared
/// selector); the value falls back to <see cref="IStringEntry.Value"/> when no variant matches.
/// </summary>
internal interface IVariantStringEntry : IStringEntry, IVariantEntry
{
    /// <summary>
    /// The raw value for <paramref name="variantKey"/> (null or unmatched resolves to the default), before Smart formatting.
    /// </summary>
    /// <param name="variantKey">The selector key to resolve; <see langword="null"/> for the default.</param>
    string GetValue(string variantKey);
}

/// <summary>
/// An asset entry that resolves per-variant values. The caller passes the current variant key (from the shared
/// selector); resolution falls back to the default when no variant matches.
/// </summary>
internal interface IVariantAssetEntry : IAssetEntry, IVariantEntry
{
    /// <summary>
    /// Whether a value is assigned for <paramref name="variantKey"/> (falling back to the default).
    /// </summary>
    /// <param name="variantKey">The selector key to resolve; <see langword="null"/> for the default.</param>
    bool HasAsset(string variantKey);

    /// <summary>
    /// Releases an asset previously returned by a variant load.
    /// </summary>
    /// <param name="variantKey">The selector key the asset was loaded for; <see langword="null"/> for the default.</param>
    /// <param name="asset">The asset to release.</param>
    void ReleaseAsset(string variantKey, Object asset);
}

/// <summary>
/// A variant asset entry that can resolve its per-variant asset immediately, without an asynchronous load.
/// </summary>
internal interface ISynchronousVariantAssetEntry
{
    /// <summary>
    /// Tries to resolve the asset for <paramref name="variantKey"/> (falling back to the default) without loading asynchronously.
    /// </summary>
    /// <param name="variantKey">The selector key to resolve; <see langword="null"/> for the default.</param>
    /// <param name="asset">The resolved asset, or <see langword="null"/> on a miss.</param>
    /// <returns><see langword="true"/> when the asset resolved synchronously; otherwise <see langword="false"/>.</returns>
    bool TryLoadAsset(string variantKey, out Object asset);
}
