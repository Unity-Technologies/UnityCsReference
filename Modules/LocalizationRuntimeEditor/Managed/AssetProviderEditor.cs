// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Localization.Providers;

namespace Unity.Localization.Editor;

/// <summary>
/// Defines the editor-side companion to a runtime asset provider.
/// </summary>
/// <remarks>
/// Providers load a collection's tables and shared data. The editor enumerates the collections a provider serves
/// (discovery for the table window) and registers a collection's per-locale tables with the provider. Localized
/// asset values live on the entries themselves, so there is no per-asset registry here. See <see cref="IAssetEntry"/>.
/// The base implementation discovers collections by matching the provider id and performs no table registration, so
/// override the registration hooks for a specific provider type.
/// </remarks>
public abstract class AssetProviderEditor
{
    /// <summary>
    /// Returns the collections assigned to a provider.
    /// </summary>
    /// <remarks>
    /// Collections are matched to a provider by their provider id.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <returns>The collections the provider serves.</returns>
    public IEnumerable<ResourceTableCollection> EnumerateCollections(IAssetProvider provider)
    {
        var id = provider.Id;
        foreach (var collection in AssetProviderEditors.FindAllCollections())
        {
            if (collection != null && collection.ProviderId == id)
                yield return collection;
        }
    }

    /// <summary>
    /// Registers every per-locale table in a collection with a provider.
    /// </summary>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <param name="collection">The collection whose tables to register.</param>
    public virtual void RegisterCollection(IAssetProvider provider, ResourceTableCollection collection)
    {
        if (collection == null)
            return;
        foreach (var table in collection.Tables)
        {
            if (table == null)
                continue;
            if (IsLocaleEnabled(table.LocaleIdentifier))
                RegisterTable(provider, collection, table);
            else
                UnregisterTable(provider, collection, table);
        }
    }

    /// <summary>
    /// Removes every per-locale table in a collection from a provider.
    /// </summary>
    /// <remarks>
    /// Called when a collection moves to a different provider so the previous provider drops its stale mappings.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <param name="collection">The collection whose tables to remove.</param>
    public virtual void UnregisterCollection(IAssetProvider provider, ResourceTableCollection collection)
    {
        if (collection == null)
            return;
        foreach (var table in collection.Tables)
        {
            if (table != null)
                UnregisterTable(provider, collection, table);
        }
    }

    /// <summary>
    /// Drops every table registration the provider holds.
    /// </summary>
    /// <remarks>
    /// Called before rebuilding registrations from the surviving collections, so a deleted collection or a removed
    /// content source cannot leave stale mappings behind.
    /// </remarks>
    /// <param name="provider">The provider instance from the settings chain.</param>
    public virtual void ClearRegistrations(IAssetProvider provider) { }

    /// <summary>
    /// Adds or removes a locale's tables across the provider's collections when the locale is toggled.
    /// </summary>
    /// <param name="provider">The provider instance from the settings chain.</param>
    /// <param name="locale">The locale being toggled.</param>
    /// <param name="enabled">Whether the locale is now enabled.</param>
    public void SetLocaleEnabled(IAssetProvider provider, Locale locale, bool enabled)
    {
        if (provider == null || locale == null)
            return;
        foreach (var collection in EnumerateCollections(provider))
        {
            var table = collection != null ? collection.GetTable(locale.Identifier) : null;
            if (table == null)
                continue;
            if (enabled)
                RegisterTable(provider, collection, table);
            else
                UnregisterTable(provider, collection, table);
        }
    }

    /// <summary>
    /// Registers a single per-locale table with the provider.
    /// </summary>
    /// <remarks>
    /// Override per provider type. The base implementation does nothing.
    /// </remarks>
    /// <param name="provider">The provider instance.</param>
    /// <param name="collection">The owning collection.</param>
    /// <param name="table">The per-locale table to register.</param>
    protected virtual void RegisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table) { }

    /// <summary>
    /// Removes a single per-locale table from the provider.
    /// </summary>
    /// <remarks>
    /// Override per provider type. The base implementation does nothing.
    /// </remarks>
    /// <param name="provider">The provider instance.</param>
    /// <param name="collection">The owning collection.</param>
    /// <param name="table">The per-locale table to remove.</param>
    protected virtual void UnregisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table) { }

    /// <summary>
    /// Creates the table reference to store when a collection is assigned to a localized reference.
    /// </summary>
    /// <param name="collection">The collection being referenced.</param>
    /// <returns>The reference to persist for the collection.</returns>
    public virtual TableReference CreateReference(ResourceTableCollection collection)
    {
        if (collection == null)
            return default;
        var shared = collection.SharedData;
        if (shared != null && !shared.TableCollectionNameGuid.Empty())
            return TableReference.FromGuid(shared.TableCollectionNameGuid);
        return (TableReference)collection.TableCollectionName;
    }

    /// <summary>
    /// Returns whether this provider should serve a collection saved at a given path.
    /// </summary>
    /// <remarks>
    /// The decision is made from the path. The base implementation makes no claim.
    /// </remarks>
    /// <param name="assetPath">The collection asset path.</param>
    /// <returns><see langword="true"/> when the provider claims the path; otherwise <see langword="false"/>.</returns>
    public virtual bool ClaimsPath(string assetPath) => false;

    /// <summary>
    /// Returns the address a per-locale table is served under, keyed by collection name.
    /// </summary>
    /// <remarks>
    /// The form is <c>{collectionName}_{localeCode}</c>. Register this alongside
    /// <see cref="GuidTableAddress"/> so a reference by either name or guid resolves.
    /// </remarks>
    /// <param name="collection">The collection the table belongs to.</param>
    /// <param name="table">The per-locale table.</param>
    /// <returns>The address the table is served under.</returns>
    /// <seealso cref="GuidTableAddress"/>
    protected static string TableAddress(ResourceTableCollection collection, ResourceTable table)
        => $"{collection.TableCollectionName}_{table.LocaleIdentifier.Code}";

    /// <summary>
    /// Returns the address a per-locale table is served under, keyed by the collection guid.
    /// </summary>
    /// <remarks>
    /// The form is <c>{collectionGuid}_{localeCode}</c>. A guid survives renaming the collection, so this is the
    /// address a stored reference should use. Returns an empty string when the collection has no shared data or its
    /// guid is unset.
    /// </remarks>
    /// <param name="collection">The collection the table belongs to.</param>
    /// <param name="table">The per-locale table.</param>
    /// <returns>The address the table is served under, or an empty string when the collection has no guid.</returns>
    /// <seealso cref="TableAddress"/>
    protected static string GuidTableAddress(ResourceTableCollection collection, ResourceTable table)
    {
        var shared = collection.SharedData;
        if (shared == null || shared.TableCollectionNameGuid.Empty())
            return string.Empty;
        return $"{shared.TableCollectionNameGuid}_{table.LocaleIdentifier.Code}";
    }

    internal static bool IsLocaleEnabled(LocaleIdentifier id)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return true;
        var locale = settings.GetLocale(id);
        return locale != null && locale.Enabled;
    }
}
