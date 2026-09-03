// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Localization.Providers;
using Unity.Localization.Providers.FileTables;

namespace Unity.Localization.Editor;

/// <summary>
/// The editor half of a file-backed content source, supplying the writer that generates its table files.
/// </summary>
/// <remarks>
/// A <see cref="FileTableProvider"/> reads tables from files at runtime, and this generates those files when player
/// data is built. Subclass it, return a writer matching the provider's reader, and register it with an
/// <see cref="AssetProviderEditorAttribute"/> naming the provider type. Without a registered editor the provider's
/// files are never generated and its tables do not resolve in a player.
/// </remarks>
/// <example>
/// <para>Register a writer for a custom file format.</para>
/// <code source="../../../../Modules/LocalizationRuntimeEditor/Tests/UTFTests/Editor/Localization.Samples/CustomFileTableProviderEditorExample.cs"/>
/// </example>
/// <seealso cref="ITableFileWriter"/>
/// <seealso cref="FileTableProvider"/>
public abstract class FileTableProviderEditor : AssetProviderEditor
{
    /// <summary>
    /// The writer that turns a table snapshot into the provider's file format.
    /// </summary>
    /// <remarks>
    /// Return a writer whose output the provider's reader parses back, so a generated file round-trips.
    /// </remarks>
    public abstract ITableFileWriter Writer { get; }

    /// <summary>
    /// Records a collection so the provider serves its table files at runtime.
    /// </summary>
    /// <remarks>
    /// A file source resolves a table by collection guid and locale rather than by individual table, so this records
    /// the collection once. Ignored when the collection has no shared data or no collection guid.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <param name="collection">The collection to record.</param>
    public override void RegisterCollection(IAssetProvider provider, ResourceTableCollection collection)
    {
        if (provider is not FileTableProvider fileProvider || collection == null || collection.SharedData == null)
            return;
        var shared = collection.SharedData;
        if (shared.TableCollectionNameGuid.Empty())
            return;
        fileProvider.AddOwnedCollection(shared.TableCollectionNameGuid.ToString(), collection.TableCollectionName);
    }

    /// <summary>
    /// Drops a collection so the provider stops serving its table files.
    /// </summary>
    /// <remarks>
    /// Called when a collection moves to a different content source, so this one no longer claims its addresses.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <param name="collection">The collection to drop.</param>
    public override void UnregisterCollection(IAssetProvider provider, ResourceTableCollection collection)
    {
        if (provider is not FileTableProvider fileProvider || collection == null || collection.SharedData == null)
            return;
        var shared = collection.SharedData;
        if (!shared.TableCollectionNameGuid.Empty())
            fileProvider.RemoveOwnedCollection(shared.TableCollectionNameGuid.ToString());
    }

    /// <summary>
    /// Drops every collection the provider records.
    /// </summary>
    /// <remarks>
    /// Called before registrations are rebuilt from the surviving collections.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    public override void ClearRegistrations(IAssetProvider provider)
    {
        if (provider is FileTableProvider fileProvider)
            fileProvider.ClearOwnedCollections();
    }
}
