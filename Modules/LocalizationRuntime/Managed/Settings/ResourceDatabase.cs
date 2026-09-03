// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Localization.Providers;
using Unity.SmartStrings;
using Unity.SmartStrings.PersistentVariables;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// Resolves localized strings and assets for the active localization settings.
/// </summary>
/// <remarks>
/// A resource database owns an <see cref="Unity.Localization.Providers.AssetProvider"/> chain and an internal per-locale asset cache. Access the
/// active database through <see cref="LocalizationSettings.Database"/>. Tables load through the provider chain like
/// any other asset (the database keeps no table list), so use the reference types <see cref="TableReference"/> and
/// <see cref="TableEntryReference"/> to identify what to resolve. String entries flagged as Smart are formatted
/// through the settings' Smart formatter (see <see cref="LocalizationSettings.GetSmartFormatter"/>), and resolved
/// values pass through the locale's post-processing hook, which pseudo-locales use to transform text. Each resolve
/// method has an asynchronous form that loads on demand and a synchronous form that reads only already-loaded
/// tables. Missing or empty entries follow the locale fallback chain defined by <see cref="Locale.FallbackCode"/>.
/// </remarks>
/// <example>
/// <para>Resolves a localized string in the selected locale.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/ResolveLocalizedStringExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="TableReference"/>
/// <seealso cref="TableEntryReference"/>
/// <seealso cref="LocalizedString"/>
/// <seealso cref="Locale"/>
/// <seealso cref="Unity.Localization.Providers.AssetProvider"/>
[Serializable]
public class ResourceDatabase
{
    [SerializeField] AssetProvider m_AssetProvider = new();

    LocaleAssetCache m_Cache;
    int m_ContentVersion;

    /// <summary>
    /// The provider chain used to load tables and localized assets.
    /// </summary>
    public AssetProvider AssetProvider => m_AssetProvider;

    // Releasing can destroy a provider-built table, so the reference types revalidate their cached entries against this.
    internal int ContentVersion => m_ContentVersion;

    LocaleAssetCache Cache => m_Cache ??= new LocaleAssetCache();

    /// <summary>
    /// Loads the resource table for a table reference in a locale through the provider chain.
    /// </summary>
    /// <remarks>
    /// The table loads asynchronously through the <see cref="Unity.Localization.Providers.AssetProvider"/> chain and is cached per locale, so
    /// repeated calls for the same table and locale reuse the loaded instance. Call this when you need the whole
    /// table, for example to enumerate its entries; to resolve a single value prefer
    /// <see cref="GetLocalizedStringAsync"/> or <see cref="GetLocalizedAssetAsync"/>. If the settings have not
    /// initialized yet, the first call triggers <see cref="LocalizationSettings.InitializeAsync"/>.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection to load.</param>
    /// <param name="locale">The locale to load the table for; null uses the selected locale.</param>
    /// <param name="cancellationToken">The token that cancels the load operation.</param>
    /// <returns>The loaded resource table, or null when no table matches the reference in the locale.</returns>
    /// <example>
    /// <para>Loads a table and reports the locale it was loaded for.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetTableAsyncExample.cs"/>
    /// </example>
    public async Awaitable<ResourceTable> GetTableAsync(TableReference tableRef, Locale locale = null, CancellationToken cancellationToken = default)
    {
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        locale = Resolve(locale);
        var address = TableAddress(tableRef);
        if (string.IsNullOrEmpty(address) || locale == null)
            return null;
        var cacheId = TableCacheId(address, locale);
        // Probe before building the key and the loader and releaser closures, so a hit allocates nothing.
        if (Cache.TryGetCached<ResourceTable>(locale.Identifier, cacheId, out var cachedTable))
            return cachedTable;
        var key = new AssetKey($"{address}_{locale.Code}", typeof(ResourceTable));
        // Tables load through the provider chain; the first provider to resolve owns Release.
        IAssetProvider resolver = null;
        return await Cache.GetAssetAsync<ResourceTable>(
            locale.Identifier, cacheId,
            async token =>
            {
                var providers = m_AssetProvider.Providers;
                for (var i = 0; i < providers.Count; i++)
                {
                    if (providers[i] == null)
                        continue;
                    await Awaitable.MainThreadAsync();
                    var candidate = await LoadFromProviderAsync(providers[i], key, token);
                    if (candidate != null)
                    {
                        resolver = providers[i];
                        return candidate;
                    }
                }
                return null;
            },
            asset => resolver?.Release(asset),
            cancellationToken);
    }

    /// <summary>
    /// Resolves an entry, following the locale fallback chain when the entry is missing or empty.
    /// </summary>
    /// <remarks>
    /// The entry loads through the provider chain. When the requested locale has no usable value, resolution walks
    /// the fallback chain defined by <see cref="Locale.FallbackCode"/> until it finds a value or the chain ends. The
    /// result is a raw <see cref="IResourceEntry"/>; to get a formatted string or a loaded asset, use
    /// <see cref="GetLocalizedStringAsync"/> or <see cref="GetLocalizedAssetAsync"/> instead.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the entry to resolve within the table.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <param name="cancellationToken">The token that cancels the load operation.</param>
    /// <param name="enableFallback">Whether to walk the locale fallback chain when the locale has no usable value.</param>
    /// <returns>The resolved entry, or null when neither the locale nor its fallback chain has a usable value.</returns>
    /// <example>
    /// <para>Resolves an entry and reads it when it is a string entry.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetEntryAsyncExample.cs"/>
    /// </example>
    public async Awaitable<IResourceEntry> GetEntryAsync(TableReference tableRef, TableEntryReference entryRef, Locale locale = null, CancellationToken cancellationToken = default, bool enableFallback = true)
    {
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        return await ResolveEntryAsync(tableRef, entryRef, locale, cancellationToken, enableFallback);
    }

    async Awaitable<IResourceEntry> ResolveEntryAsync(TableReference tableRef, TableEntryReference entryRef, Locale locale, CancellationToken cancellationToken, bool enableFallback = true)
    {
        var current = Resolve(locale);
        HashSet<string> visited = null;
        while (current != null)
        {
            var table = await GetTableAsync(tableRef, current, cancellationToken);
            var entry = ResolveFromReference(table, entryRef);
            if (entry != null && IsUsable(entry, CurrentVariantKey(table, entry.KeyId)))
                return entry;
            if (!enableFallback)
                break;
            // Stop if the fallback chain loops back on itself.
            visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!visited.Add(current.Code))
                break;
            current = ResolveFallback(current);
        }
        return null;
    }

    /// <summary>
    /// Resolves a localized string, applying Smart formatting and the locale post-processing hook.
    /// </summary>
    /// <remarks>
    /// The entry loads through the provider chain, following the locale fallback chain when
    /// <paramref name="enableFallback"/> is true. When the entry is marked as Smart, the value is formatted with the
    /// settings' Smart formatter (see <see cref="LocalizationSettings.GetSmartFormatter"/>) using
    /// <paramref name="args"/> and <paramref name="localVariables"/>. The result is then passed through the locale's
    /// post-processing hook, which pseudo-locales use to transform text. To read an already-loaded value without
    /// awaiting, use <see cref="GetLocalizedString"/>.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the string entry to resolve.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <param name="args">The Smart String arguments to format placeholders with; null when the entry takes no arguments.</param>
    /// <param name="cancellationToken">The token that cancels the load operation.</param>
    /// <param name="enableFallback">Whether to walk the locale fallback chain when the locale has no value.</param>
    /// <param name="localVariables">The extra variables that Smart String placeholders resolve against, in addition to the arguments.</param>
    /// <returns>The formatted localized string, or null when the entry is not a string entry or has no value.</returns>
    /// <example>
    /// <para>Resolves a Smart String that inserts the player's score.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetLocalizedStringWithArgsExample.cs"/>
    /// </example>
    public async Awaitable<string> GetLocalizedStringAsync(TableReference tableRef, TableEntryReference entryRef, Locale locale = null, object[] args = null, CancellationToken cancellationToken = default, bool enableFallback = true, IVariableGroup localVariables = null)
    {
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        locale = Resolve(locale);
        var entry = await ResolveEntryAsync(tableRef, entryRef, locale, cancellationToken, enableFallback);
        return FormatEntry(entry as IStringEntry, locale, args, localVariables);
    }

    /// <summary>
    /// Resolves a localized string synchronously, or null when it cannot be resolved without an asynchronous load.
    /// </summary>
    /// <remarks>
    /// This is the synchronous companion to <see cref="GetLocalizedStringAsync"/>. It resolves from tables already
    /// cached for the locale, and otherwise loads the table through any provider that implements
    /// <see cref="ISynchronousAssetProvider"/> (direct references or a <c>Resources</c> load); a table that only a
    /// purely asynchronous provider can supply resolves to null until it is loaded. Use it on the main thread when you
    /// cannot await. Smart entries are formatted and the locale post-processing hook is applied like the async form.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the string entry to resolve.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <param name="args">The Smart String arguments to format placeholders with; null when the entry takes no arguments.</param>
    /// <param name="enableFallback">Whether to walk the locale fallback chain when the locale has no value.</param>
    /// <param name="localVariables">The extra variables that Smart String placeholders resolve against, in addition to the arguments.</param>
    /// <returns>The formatted localized string, or null when the table is not loaded or the entry has no value.</returns>
    /// <example>
    /// <para>Reads an already-loaded string without awaiting.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetLocalizedStringSyncExample.cs"/>
    /// </example>
    public string GetLocalizedString(TableReference tableRef, TableEntryReference entryRef, Locale locale = null, object[] args = null, bool enableFallback = true, IVariableGroup localVariables = null)
    {
        LocalizationSettings.RequestInitialization();
        locale = Resolve(locale);
        var entry = ResolveEntry(tableRef, entryRef, locale, enableFallback);
        return FormatEntry(entry as IStringEntry, locale, args, localVariables);
    }

    internal string FormatEntry(IStringEntry entry, Locale locale, object[] args, IVariableGroup localVariables = null)
        => entry != null ? PostProcess(locale, FormatString(entry, entry.Table, args, localVariables)) : null;

    static string FormatString(IStringEntry stringEntry, ResourceTable table, object[] args, IVariableGroup localVariables = null)
    {
        var value = EntryValue(stringEntry, CurrentVariantKey(table, stringEntry.KeyId));
        if (!string.IsNullOrEmpty(value) && IsSmart(table, stringEntry.KeyId))
        {
            try
            {
                var formatter = LocalizationSettings.Instance != null ? LocalizationSettings.Instance.GetSmartFormatter() : Smart.Default;
                value = Format(formatter, value, args ?? Array.Empty<object>(), localVariables);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Smart String formatting failed for key {stringEntry.KeyId}: {e.Message}");
            }
        }
        return value;
    }

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static string Format(SmartFormatter formatter, string value, object[] args, IVariableGroup localVariables)
    {
        if (localVariables == null)
            return formatter.Format(value, args);

        var parsed = formatter.Parser.ParseFormat(value);
        try
        {
            parsed.AdditionalData = new AdditionalFormatData { LocalVariables = localVariables };
            return formatter.Format(parsed, args);
        }
        finally
        {
            // Clear before returning the pooled Format so our variables never leak into a reused instance.
            parsed.AdditionalData = null;
            parsed.Dispose();
        }
    }

    /// <summary>
    /// Resolves a localized asset: the direct reference when present, otherwise through the provider chain.
    /// </summary>
    /// <remarks>
    /// The entry resolves in the requested locale, following the fallback chain when it has no asset. The loaded
    /// asset is cached per locale and variant, so a matching entry is loaded once and reused. Release cached assets
    /// with <see cref="ReleaseAssets"/> or <see cref="ReleaseAllAssets"/> when they are no longer needed.
    /// </remarks>
    /// <typeparam name="TObject">The type of asset to load, such as a texture, sprite, or audio clip.</typeparam>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the asset entry to resolve within the table.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <param name="cancellationToken">The token that cancels the load operation.</param>
    /// <returns>The loaded localized asset, or null when the entry has no asset for the locale.</returns>
    /// <example>
    /// <para>Loads a localized sprite for the selected locale.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetLocalizedAssetAsyncExample.cs"/>
    /// </example>
    public async Awaitable<TObject> GetLocalizedAssetAsync<TObject>(TableReference tableRef, TableEntryReference entryRef, Locale locale = null, CancellationToken cancellationToken = default) where TObject : Object
    {
        if (!LocalizationSettings.InitializationSettled)
            await LocalizationSettings.EnsureInitializedForUse();
        locale = Resolve(locale);
        var entry = await ResolveEntryAsync(tableRef, entryRef, locale, cancellationToken);
        return await LoadLocalizedAssetAsync<TObject>(entry as IAssetEntry, locale, cancellationToken);
    }

    internal async Awaitable<TObject> LoadLocalizedAssetAsync<TObject>(IAssetEntry entry, Locale locale, CancellationToken cancellationToken = default) where TObject : Object
    {
        if (entry == null)
            return null;
        var variantKey = CurrentVariantKey(entry.Table, entry.KeyId);
        var cacheId = AssetCacheId(entry.Table, entry.KeyId, variantKey);
        var id = locale != null ? locale.Identifier : default;
        if (entry is IVariantAssetEntry variantAsset)
            return await Cache.GetAssetAsync<TObject>(id, cacheId,
                VariantEntryLoader(entry, variantKey),
                asset => variantAsset.ReleaseAsset(variantKey, asset),
                cancellationToken);
        return await Cache.GetAssetAsync<TObject>(id, cacheId,
            EntryLoader(entry),
            asset => entry.ReleaseAsset(asset),
            cancellationToken);
    }

    static Func<CancellationToken, Awaitable<Object>> EntryLoader(IAssetEntry entry)
    {
        if (entry is IAsyncAssetEntry asyncEntry)
            return asyncEntry.LoadAssetAsync;
        if (entry is ISynchronousAssetEntry syncEntry)
            return _ => AwaitableUtility.FromResult(syncEntry.TryLoadAsset(out var asset) ? asset : null);
        return _ => AwaitableUtility.FromResult<Object>(null);
    }

    // The built-in variant kinds have no async form, so only the sync path and a miss fallback are needed.
    static Func<CancellationToken, Awaitable<Object>> VariantEntryLoader(IResourceEntry entry, string variantKey)
    {
        if (entry is ISynchronousVariantAssetEntry syncEntry)
            return _ => AwaitableUtility.FromResult(syncEntry.TryLoadAsset(variantKey, out var asset) ? asset : null);
        return _ => AwaitableUtility.FromResult<Object>(null);
    }

    static Awaitable<Object> LoadFromProviderAsync(IAssetProvider provider, AssetKey key, CancellationToken cancellationToken)
    {
        if (provider is IAsyncAssetProvider asyncProvider)
            return asyncProvider.LoadAssetAsync<Object>(key, cancellationToken);
        if (provider is ISynchronousAssetProvider syncProvider)
            return AwaitableUtility.FromResult(syncProvider.TryLoadAsset<Object>(key, out var asset) ? asset : null);
        return AwaitableUtility.FromResult<Object>(null);
    }

    /// <summary>
    /// Resolves a localized asset synchronously from a synchronous provider and entry.
    /// </summary>
    /// <remarks>
    /// This is the synchronous companion to <see cref="GetLocalizedAssetAsync"/>. It resolves the table through a
    /// provider that implements <see cref="ISynchronousAssetProvider"/> and loads the asset through an entry that
    /// implements <see cref="ISynchronousAssetEntry"/>, such as a direct reference or a <c>Resources</c> load. It
    /// returns null when the table or the asset can only be loaded asynchronously and is not already cached.
    /// </remarks>
    /// <typeparam name="TObject">The asset type to resolve.</typeparam>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the entry to resolve within the table.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <returns>The resolved asset, or null when it cannot be resolved synchronously.</returns>
    /// <example>
    /// <para>Reads a localized asset synchronously.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetLocalizedAssetSyncExample.cs"/>
    /// </example>
    public TObject GetLocalizedAsset<TObject>(TableReference tableRef, TableEntryReference entryRef, Locale locale = null) where TObject : Object
    {
        LocalizationSettings.RequestInitialization();
        locale = Resolve(locale);
        var entry = ResolveEntry(tableRef, entryRef, locale);
        return LoadLocalizedAsset<TObject>(entry as IAssetEntry, locale);
    }

    internal TObject LoadLocalizedAsset<TObject>(IAssetEntry entry, Locale locale) where TObject : Object
    {
        if (entry == null)
            return null;
        var variantKey = CurrentVariantKey(entry.Table, entry.KeyId);
        var cacheId = AssetCacheId(entry.Table, entry.KeyId, variantKey);
        var id = locale != null ? locale.Identifier : default;
        // Probe before building the loader and releaser closures, so a hit on the steady-state path allocates nothing.
        if (Cache.TryGetCached<TObject>(id, cacheId, out var cached))
            return cached;
        if (entry is IVariantAssetEntry variantAsset && entry is ISynchronousVariantAssetEntry variantSync)
            return Cache.GetAsset<TObject>(id, cacheId,
                () => variantSync.TryLoadAsset(variantKey, out var asset) ? asset : null,
                asset => variantAsset.ReleaseAsset(variantKey, asset));
        if (entry is ISynchronousAssetEntry entrySync)
            return Cache.GetAsset<TObject>(id, cacheId,
                () => entrySync.TryLoadAsset(out var asset) ? asset : null,
                entry.ReleaseAsset);
        return null;
    }

    static string AssetCacheId(ResourceTable table, long keyId, string variantKey)
    {
        var address = TableAddress(table);
        if (string.IsNullOrEmpty(address) && table != null)
            address = "#" + table.GetEntityId();
        return $"asset:{address}:{keyId}:{variantKey}";
    }

    /// <summary>
    /// Releases every cached asset loaded for the given locale.
    /// </summary>
    /// <remarks>
    /// Use this to free memory for a locale you no longer need, for example after switching away from it. Assets
    /// requested again later are reloaded through the provider chain. To release every locale's assets at once, use
    /// <see cref="ReleaseAllAssets"/>.
    /// </remarks>
    /// <param name="locale">The locale whose cached assets to release.</param>
    /// <example>
    /// <para>Frees the cached assets for a locale after switching away from it.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/ReleaseAssetsExample.cs"/>
    /// </example>
    public void ReleaseAssets(Locale locale)
    {
        if (locale == null)
            return;
        m_ContentVersion++;
        m_Cache?.ReleaseAssetsForLocale(locale.Identifier);
    }

    /// <summary>
    /// Releases every cached asset across all locales.
    /// </summary>
    /// <remarks>
    /// Frees all localized assets held by the database's cache. Each asset is reloaded through the provider chain the
    /// next time it is requested. To release only one locale's assets, use <see cref="ReleaseAssets"/>.
    /// </remarks>
    /// <example>
    /// <para>Releases all cached assets, for example before unloading a scene.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/ReleaseAllAssetsExample.cs"/>
    /// </example>
    public void ReleaseAllAssets()
    {
        m_ContentVersion++;
        m_Cache?.ReleaseAll();
    }

    internal void OnLocaleChanged(LocaleIdentifier previous)
    {
        if (!string.IsNullOrEmpty(previous.Code))
            ReleaseLocaleAssetsNextFrame(previous);
    }

    async void ReleaseLocaleAssetsNextFrame(LocaleIdentifier identifier)
    {
        try
        {
            // One frame's grace so change handlers can swap in the new locale's assets before the old ones unload.
            await Awaitable.NextFrameAsync();
            var selected = LocalizationSettings.SelectedLocale;
            if (selected != null && selected.Identifier == identifier)
                return;
            m_ContentVersion++;
            m_Cache?.ReleaseAssetsForLocale(identifier);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Returns the resource table for a reference if it is already loaded, or null otherwise.
    /// </summary>
    /// <remarks>
    /// This is the synchronous companion to <see cref="GetTableAsync"/>. It returns a cached table, otherwise it
    /// resolves the table through any provider in the chain that implements <see cref="ISynchronousAssetProvider"/>,
    /// such as direct references or a <c>Resources</c> load. A table that only a purely asynchronous provider can
    /// supply returns null until <see cref="GetTableAsync"/> loads it. Use it on the main thread when you cannot await.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection to look up.</param>
    /// <param name="locale">The locale to look up; null uses the selected locale.</param>
    /// <returns>The resource table for the reference in the locale, or null when it cannot be resolved synchronously.</returns>
    /// <example>
    /// <para>Reads a table if it is loaded, otherwise loads it asynchronously.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetTableSyncExample.cs"/>
    /// </example>
    public ResourceTable GetTable(TableReference tableRef, Locale locale = null)
    {
        LocalizationSettings.RequestInitialization();
        locale = Resolve(locale);
        var address = TableAddress(tableRef);
        if (string.IsNullOrEmpty(address) || locale == null)
            return null;
        var cacheId = TableCacheId(address, locale);
        if (Cache.TryGetCached<ResourceTable>(locale.Identifier, cacheId, out var table))
            return table;
        var key = new AssetKey($"{address}_{locale.Code}", typeof(ResourceTable));
        return LoadTableSync(locale.Identifier, cacheId, key);
    }

    ResourceTable LoadTableSync(LocaleIdentifier locale, string cacheId, AssetKey key)
    {
        var providers = m_AssetProvider.Providers;
        for (var i = 0; i < providers.Count; i++)
        {
            if (providers[i] is not ISynchronousAssetProvider sync)
                continue;
            var provider = providers[i];
            var table = Cache.GetAsset<ResourceTable>(locale, cacheId,
                () => sync.TryLoadAsset<Object>(key, out var asset) ? asset : null, provider.Release);
            if (table != null)
                return table;
        }
        return null;
    }

    /// <summary>
    /// Resolves an entry synchronously, following the fallback chain, or null when an async load is required.
    /// </summary>
    /// <remarks>
    /// This is the synchronous companion to <see cref="GetEntryAsync"/>. It resolves from tables already cached for the
    /// locale, and otherwise loads the table through any provider that implements <see cref="ISynchronousAssetProvider"/>,
    /// following the <see cref="Locale.FallbackCode"/> chain. It returns null when the required table can only be loaded
    /// asynchronously and is not already cached.
    /// </remarks>
    /// <param name="tableRef">The reference to the table collection that holds the entry.</param>
    /// <param name="entryRef">The reference to the entry to resolve within the table.</param>
    /// <param name="locale">The locale to resolve in; null uses the selected locale.</param>
    /// <param name="enableFallback">Whether to walk the locale fallback chain when the locale has no usable value.</param>
    /// <returns>The resolved entry, or null when the table is not loaded or no usable value exists.</returns>
    /// <example>
    /// <para>Reads an entry that is already loaded.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/GetEntrySyncExample.cs"/>
    /// </example>
    public IResourceEntry GetEntry(TableReference tableRef, TableEntryReference entryRef, Locale locale = null, bool enableFallback = true)
    {
        LocalizationSettings.RequestInitialization();
        return ResolveEntry(tableRef, entryRef, locale, enableFallback);
    }

    IResourceEntry ResolveEntry(TableReference tableRef, TableEntryReference entryRef, Locale locale, bool enableFallback = true)
    {
        var current = Resolve(locale);
        HashSet<string> visited = null;
        while (current != null)
        {
            var table = GetTable(tableRef, current);
            var entry = ResolveFromReference(table, entryRef);
            if (entry != null && IsUsable(entry, CurrentVariantKey(table, entry.KeyId)))
                return entry;
            if (!enableFallback)
                break;
            // Stop if the fallback chain loops back on itself.
            visited ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!visited.Add(current.Code))
                break;
            current = ResolveFallback(current);
        }
        return null;
    }

    static Locale Resolve(Locale locale) => locale ?? LocalizationSettings.SelectedLocale;

    static string TableCacheId(string address, Locale locale) => $"table:{address}_{locale.Code}";

    static string TableAddress(TableReference tableRef)
        => tableRef.ReferenceType == TableReference.Type.Guid ? tableRef.TableCollectionNameGuid.ToString() : tableRef.TableCollectionName;

    static string TableAddress(ResourceTable table)
        => table != null ? table.TableCollectionName ?? string.Empty : string.Empty;

    static Locale ResolveFallback(Locale locale)
    {
        if (locale == null || string.IsNullOrEmpty(locale.FallbackCode))
            return null;
        return LocalizationSettings.Instance != null ? LocalizationSettings.Instance.GetLocale(locale.FallbackCode) : null;
    }

    static bool IsUsable(IResourceEntry entry, string variantKey)
    {
        return entry switch
        {
            IStringEntry stringEntry => !string.IsNullOrEmpty(EntryValue(stringEntry, variantKey)),
            IAssetEntry assetEntry => EntryHasAsset(assetEntry, variantKey),
            _ => entry != null
        };
    }

    static IResourceEntry ResolveFromReference(ResourceTable table, TableEntryReference entryRef)
        => table != null && table.SharedData != null ? table.GetEntry(entryRef.ResolveKeyId(table.SharedData)) : null;

    static string CurrentVariantKey(ResourceTable table, long keyId)
        => table != null && table.SharedData != null ? table.SharedData.GetVariantKey(keyId) : null;

    static bool IsSmart(ResourceTable table, long keyId)
        => table != null && table.SharedData != null && table.SharedData.IsSmart(keyId);

    static string EntryValue(IStringEntry entry, string variantKey)
        => entry is IVariantStringEntry variant ? variant.GetValue(variantKey) : entry.Value;

    static bool EntryHasAsset(IAssetEntry entry, string variantKey)
        => entry is IVariantAssetEntry variant ? variant.HasAsset(variantKey) : entry.HasAsset();

    static string PostProcess(Locale locale, string value)
    {
        if (value != null && locale is IPostProcessValueLocale postProcess)
            return postProcess.PostProcessValue(value);
        return value;
    }
}
