// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using Unity.Localization.Providers;
using Unity.Localization.Providers.FileTables;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor;

/// <summary>
/// Aggregates the asset provider editors from the active settings' provider chain.
/// </summary>
/// <remarks>
/// The registry discovers editors by the <see cref="AssetProviderEditorAttribute"/>, resolves the editor for a
/// provider instance, and drives collection discovery and registration for the table window and settings page.
/// </remarks>
internal static partial class AssetProviderEditors
{
    [AutoStaticsCleanup] // keyed by Type and holds editor instances; both go stale on reload
    static Dictionary<Type, AssetProviderEditor> s_Editors;
    [NoAutoStaticsCleanup] // stateless fallback; a readonly field cannot be reassigned anyway
    static readonly AssetProviderEditor s_Default = new DefaultAssetProviderEditor();
    [AutoStaticsCleanup] // asset scan cache; CollectionsChanged already drops it
    static List<ResourceTableCollection> s_Collections;

    sealed class DefaultAssetProviderEditor : AssetProviderEditor { }

    static AssetProviderEditors()
    {
        // Dropped on CollectionsChanged, so the per-repaint scans below stay a memory lookup.
        LocalizationEditorSettings.CollectionsChanged += () => s_Collections = null;
    }

    /// <summary>
    /// Returns the types that can still be added to a chain that already holds the given providers.
    /// </summary>
    /// <remarks>
    /// A chain holds at most one provider of each type, because a collection names its provider by type.
    /// </remarks>
    /// <param name="existing">The providers already in the chain.</param>
    /// <returns>The instantiable provider types not already present, ordered by title.</returns>
    internal static List<Type> AddableProviderTypes(IEnumerable<IAssetProvider> existing)
    {
        var taken = new HashSet<Type>();
        foreach (var provider in existing)
        {
            if (provider != null)
                taken.Add(provider.GetType());
        }
        var types = new List<Type>();
        foreach (var type in GetInstantiableTypes<IAssetProvider>(excludeUnityObjects: true))
        {
            if (!taken.Contains(type))
                types.Add(type);
        }
        types.Sort((a, b) => string.Compare(ProviderTitle(a), ProviderTitle(b), StringComparison.Ordinal));
        return types;
    }

    /// <summary>
    /// Enumerates every table collection asset in the project.
    /// </summary>
    /// <returns>All <see cref="ResourceTableCollection"/> assets found by the asset database.</returns>
    public static IEnumerable<ResourceTableCollection> FindAllCollections()
    {
        return s_Collections ??= ScanCollections();
    }

    static List<ResourceTableCollection> ScanCollections()
    {
        var result = new List<ResourceTableCollection>();
        var guids = AssetDatabase.FindAssets($"t:{nameof(ResourceTableCollection)}");
        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var collection = AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(path);
            if (collection != null)
                result.Add(collection);
        }
        return result;
    }

    /// <summary>
    /// Enumerates the providers in the active settings' chain, in order.
    /// </summary>
    /// <returns>The providers in the active chain, or nothing when there are no settings.</returns>
    public static IEnumerable<IAssetProvider> ChainProviders()
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var chain = settings != null && settings.Database != null ? settings.Database.AssetProvider : null;
        if (chain == null)
            yield break;
        foreach (var provider in chain.Providers)
        {
            if (provider != null)
                yield return provider;
        }
    }

    /// <summary>
    /// Returns the collections shown in the table window.
    /// </summary>
    /// <remarks>
    /// The list is each provider's collections, plus any collection not attributed to a provider present in the chain,
    /// so the window always populates.
    /// </remarks>
    /// <returns>The known collections, de-duplicated and ordered by provider.</returns>
    public static List<ResourceTableCollection> GetKnownCollections()
    {
        var result = new List<ResourceTableCollection>();
        var seen = new HashSet<ResourceTableCollection>();

        foreach (var provider in ChainProviders())
        {
            var editor = GetEditor(provider) ?? s_Default;
            foreach (var collection in editor.EnumerateCollections(provider))
            {
                if (collection != null && seen.Add(collection))
                    result.Add(collection);
            }
        }

        // Safety net for collections whose ProviderId is empty or names a provider not in the chain.
        foreach (var collection in FindAllCollections())
        {
            if (collection != null && seen.Add(collection))
                result.Add(collection);
        }
        return result;
    }

    /// <summary>
    /// Returns the editor registered for a provider's type.
    /// </summary>
    /// <param name="provider">The provider instance.</param>
    /// <returns>The registered editor, or <see langword="null"/> when none is registered.</returns>
    public static AssetProviderEditor GetEditor(IAssetProvider provider)
    {
        if (provider == null)
            return null;
        s_Editors ??= BuildEditors();
        return s_Editors.TryGetValue(provider.GetType(), out var editor) ? editor : null;
    }

    /// <summary>
    /// Registers a collection's tables with the provider that serves it.
    /// </summary>
    /// <remarks>
    /// The provider is named by the collection's provider id, defaulting to the first provider in the chain. On first
    /// registration the provider id is resolved from the asset path and stored on the collection.
    /// </remarks>
    /// <param name="collection">The collection to register.</param>
    public static void RegisterCollection(ResourceTableCollection collection)
    {
        if (collection == null)
            return;
        MoveCollection(collection, ResolveTargetProvider(collection));
    }

    /// <summary>
    /// Moves a collection's tables to a provider, removing them from the one that served it before.
    /// </summary>
    /// <remarks>
    /// The previous provider is read from the collection's provider id, so callers must leave that id alone and let
    /// this assign it. Recorded as one undo step covering both the id and the provider registrations.
    /// </remarks>
    /// <param name="collection">The collection to move.</param>
    /// <param name="target">The provider that should serve the collection.</param>
    internal static void MoveCollection(ResourceTableCollection collection, IAssetProvider target)
    {
        if (collection == null || target == null)
            return;

        // One entry for both, since the id lives on the collection and the registrations live in the settings.
        var settings = LocalizationEditorSettings.ActiveSettings;
        Undo.RecordObjects(settings != null
            ? new UnityEngine.Object[] { collection, settings }
            : new UnityEngine.Object[] { collection }, "Change Content Source");

        // A collection that moved provider (in or out of a Resources folder) must lose its stale tables first.
        var previous = string.IsNullOrEmpty(collection.ProviderId) ? null : ResolveProvider(collection);
        if (previous != null && !ReferenceEquals(previous, target))
            (GetEditor(previous) ?? s_Default).UnregisterCollection(previous, collection);

        var targetId = target.Id;
        if (collection.ProviderId != targetId)
        {
            collection.ProviderId = targetId;
            EditorUtility.SetDirty(collection);
        }

        (GetEditor(target) ?? s_Default).RegisterCollection(target, collection);

        if (settings != null)
            EditorUtility.SetDirty(settings);
    }

    internal static void RebuildRegistrations()
    {
        foreach (var provider in ChainProviders())
            (GetEditor(provider) ?? s_Default).ClearRegistrations(provider);
        foreach (var collection in FindAllCollections())
            RegisterCollection(collection);
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings != null)
            EditorUtility.SetDirty(settings);
    }

    static IAssetProvider ResolveTargetProvider(ResourceTableCollection collection)
    {
        var path = AssetDatabase.GetAssetPath(collection);
        var current = string.IsNullOrEmpty(collection.ProviderId) ? null : ResolveProvider(collection);
        if (IsUnderResources(path))
            return current is ResourceFolderProvider ? current : ResolveForNewCollection(path);
        if (current != null && current is not ResourceFolderProvider)
            return current;
        return ResolveForNewCollection(path);
    }

    // Case-insensitive: Unity treats a resources folder as a Resources folder on every platform.
    internal static bool IsUnderResources(string assetPath)
        => !string.IsNullOrEmpty(assetPath) && assetPath.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0;

    // Resources.Load resolves against the nearest enclosing folder, so the last marker is the one that counts.
    internal static string ResourcesRelativePath(string assetPath)
    {
        const string marker = "/Resources/";
        var index = string.IsNullOrEmpty(assetPath) ? -1 : assetPath.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;
        var relative = assetPath.Substring(index + marker.Length);
        var dot = relative.LastIndexOf('.');
        return dot >= 0 ? relative.Substring(0, dot) : relative;
    }

    static Type ClaimingProviderType(string assetPath)
    {
        s_Editors ??= BuildEditors();
        foreach (var entry in s_Editors)
        {
            if (entry.Value.ClaimsPath(assetPath))
                return entry.Key;
        }
        return null;
    }

    internal static IAssetProvider ResolveForNewCollection(string assetPath)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var chain = settings != null && settings.Database != null ? settings.Database.AssetProvider : null;
        if (chain == null)
            return null;

        var claimedType = ClaimingProviderType(assetPath);
        if (claimedType != null)
            return EnsureProviderOfType(chain, claimedType);

        // The selected provider is maintained to always be a live entry in the chain (or null).
        var selected = settings.SelectedProvider;
        if (selected != null && selected is not ResourceFolderProvider)
            return selected;

        foreach (var provider in chain.Providers)
        {
            if (provider != null && provider is not ResourceFolderProvider)
                return provider;
        }
        return null;
    }

    static IAssetProvider EnsureProviderOfType(AssetProvider chain, Type type)
    {
        foreach (var provider in chain.Providers)
        {
            if (provider != null && provider.GetType() == type)
                return provider;
        }
        var created = (IAssetProvider)Activator.CreateInstance(type);
        chain.AddProvider(created);
        return created;
    }

    internal static bool IsCorrectlyAssigned(ResourceTableCollection collection)
    {
        if (collection == null)
            return false;
        var resolved = ResolveProvider(collection);
        if (IsUnderResources(AssetDatabase.GetAssetPath(collection)))
            return resolved is ResourceFolderProvider;
        return !string.IsNullOrEmpty(collection.ProviderId)
            && resolved != null
            && resolved.Id == collection.ProviderId
            && resolved is not ResourceFolderProvider;
    }

    internal static string ProviderTitle(Type type)
    {
        return (type != null ? type.Name : string.Empty) switch
        {
            nameof(ResourceFolderProvider) => L10n.Tr("Resources folder", null),
            nameof(ReferencedAssetProvider) => L10n.Tr("Direct references", null),
            nameof(JsonResourceProvider) => L10n.Tr("Data files (JSON)", null),
            var name => ObjectNames.NicifyVariableName(name)
        };
    }

    /// <summary>
    /// Adds or removes a locale's tables across the provider chain, then persists the change.
    /// </summary>
    /// <param name="locale">The locale being enabled or disabled.</param>
    /// <param name="enabled">Whether the locale is now enabled.</param>
    public static void SetLocaleEnabled(Locale locale, bool enabled)
    {
        if (locale == null)
            return;
        foreach (var provider in ChainProviders())
            (GetEditor(provider) ?? s_Default).SetLocaleEnabled(provider, locale, enabled);

        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings != null)
            EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Returns disabled locales whose tables are served by a Resources folder provider.
    /// </summary>
    /// <remarks>
    /// Each entry pairs the locale with its collection name. Their assets still ship because anything under a
    /// Resources folder is always included in a build.
    /// </remarks>
    /// <returns>The disabled locales served by Resources, with their collection names.</returns>
    public static List<(Locale locale, string collectionName)> ResourcesServedDisabledTables()
    {
        var result = new List<(Locale, string)>();
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            return result;
        foreach (var provider in ChainProviders())
        {
            if (provider is not ResourceFolderProvider)
                continue;
            var editor = GetEditor(provider) ?? s_Default;
            foreach (var collection in editor.EnumerateCollections(provider))
            {
                if (collection == null)
                    continue;
                foreach (var locale in settings.AvailableLocales)
                {
                    if (locale == null || locale.Enabled)
                        continue;
                    if (collection.GetTable(locale.Identifier) != null)
                        result.Add((locale, collection.TableCollectionName));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Returns the codes of every locale that has a table served by a Resources folder provider.
    /// </summary>
    /// <remarks>
    /// Computed in a single pass so a caller drawing one row per locale can look each up without re-scanning the
    /// project. The set is case-insensitive to match <see cref="LocaleIdentifier"/> comparison.
    /// </remarks>
    /// <returns>The locale codes served by a Resources provider.</returns>
    public static HashSet<string> LocaleCodesServedByResources()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in ChainProviders())
        {
            if (provider is not ResourceFolderProvider)
                continue;
            var editor = GetEditor(provider) ?? s_Default;
            foreach (var collection in editor.EnumerateCollections(provider))
            {
                if (collection == null)
                    continue;
                foreach (var table in collection.Tables)
                {
                    if (table != null)
                        result.Add(table.LocaleIdentifier.Code);
                }
            }
        }
        return result;
    }

    internal static TableReference CreateReference(ResourceTableCollection collection)
    {
        if (collection == null)
            return default;
        return (GetEditor(ResolveProvider(collection)) ?? s_Default).CreateReference(collection);
    }

    internal static IAssetProvider ResolveProvider(ResourceTableCollection collection)
    {
        IAssetProvider fallback = null;
        foreach (var provider in ChainProviders())
        {
            if (provider.Id == collection.ProviderId)
                return provider;
            fallback ??= provider;
        }
        return fallback;
    }

    // Excludes Object-derived types, which Activator cannot create.
    internal static IEnumerable<Type> GetInstantiableTypes<T>(bool excludeUnityObjects)
    {
        foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
        {
            if (type.IsAbstract || type.IsGenericType)
                continue;
            if (excludeUnityObjects && typeof(UnityEngine.Object).IsAssignableFrom(type))
                continue;
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            if (!type.IsSerializable)
                continue;
            yield return type;
        }
    }

    static Dictionary<Type, AssetProviderEditor> BuildEditors()
    {
        var map = new Dictionary<Type, AssetProviderEditor>();
        foreach (var type in TypeCache.GetTypesWithAttribute<AssetProviderEditorAttribute>())
        {
            if (type.IsAbstract)
                continue;
            var attribute = (AssetProviderEditorAttribute)Attribute.GetCustomAttribute(type, typeof(AssetProviderEditorAttribute));
            if (attribute?.ProviderType != null)
                map[attribute.ProviderType] = (AssetProviderEditor)Activator.CreateInstance(type);
        }
        return map;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
