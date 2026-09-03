// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// A serializable reference to a single localization table entry that resolves to a typed entry value.
/// </summary>
/// <remarks>
/// A <see cref="LocalizedEntry{TEntry}"/> pairs the <see cref="LocalizedReference.TableReference"/> inherited from
/// <see cref="LocalizedReference"/> with a <see cref="Unity.Localization.TableEntryReference"/>, then resolves the entry as
/// <typeparamref name="TEntry"/>. When <see cref="LocalizedReference.EnableFallback"/> is set, resolution follows the locale
/// fallback chain. Lookups go through the <see cref="ResourceDatabase"/> provider chain: call <see cref="GetEntryAsync"/> to
/// load on demand, or <see cref="GetEntry"/> to read from already-loaded tables. The <see cref="LocalizedString"/> and
/// <see cref="LocalizedAsset{TObject}"/> types derive from typed versions of this class. Read the raw entry directly to
/// inspect variants or metadata that the higher-level types do not expose.
/// </remarks>
/// <typeparam name="TEntry">The entry interface to resolve, for example <see cref="IStringEntry"/> or <see cref="IAssetEntry"/>.</typeparam>
/// <example>
/// <para>Resolve the raw entry for a reference and read its value.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedEntryResolveExample.cs"/>
/// </example>
/// <seealso cref="LocalizedString"/>
/// <seealso cref="LocalizedAsset{TObject}"/>
/// <seealso cref="Unity.Localization.TableEntryReference"/>
/// <seealso cref="ResourceDatabase"/>
[Serializable]
public partial class LocalizedEntry<TEntry> : LocalizedReference where TEntry : class, IResourceEntry
{
    [SerializeField] TableEntryReference m_TableEntryReference;

    TEntry m_CachedEntry;
    LocaleIdentifier m_CachedLocale;
    int m_CachedVersion;
    bool m_CacheValid;

    /// <summary>
    /// The entry within the table collection that this reference resolves.
    /// </summary>
    /// <remarks>
    /// Identifies the entry by key name or key id. Assigning a new value re-resolves the reference and notifies any live
    /// listeners. Use <see cref="SetReference"/> to set the table and entry together with a single refresh.
    /// </remarks>
    public TableEntryReference TableEntryReference
    {
        get => m_TableEntryReference;
        set { m_TableEntryReference = value; ForceUpdate(); }
    }

    /// <summary>
    /// Whether this reference is unset and resolves to no value.
    /// </summary>
    /// <remarks>Returns <c>true</c> when either the table reference or the entry reference is unset.</remarks>
    public override bool IsEmpty => base.IsEmpty || m_TableEntryReference.IsEmpty;

    /// <summary>
    /// Sets the table and entry references together so the reference refreshes only once.
    /// </summary>
    /// <remarks>
    /// Prefer this over assigning <see cref="LocalizedReference.TableReference"/> and
    /// <see cref="LocalizedEntry{TEntry}.TableEntryReference"/> separately, because it applies both values with a single
    /// refresh of any live listeners.
    /// </remarks>
    /// <param name="table">The table collection to resolve the entry from.</param>
    /// <param name="entry">The entry within the table collection to resolve.</param>
    /// <example>
    /// <para>Point a reference at an entry in one call.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedEntrySetReferenceExample.cs"/>
    /// </example>
    public void SetReference(TableReference table, TableEntryReference entry)
    {
        m_TableEntryReference = entry;
        TableReference = table; // the base setter assigns the table and calls ForceUpdate once
    }

    /// <summary>
    /// Resolves the entry asynchronously, loading the table if it is not already available.
    /// </summary>
    /// <remarks>
    /// The entry is resolved as <typeparamref name="TEntry"/> through the <see cref="ResourceDatabase"/> provider chain,
    /// following the locale fallback chain when <see cref="LocalizedReference.EnableFallback"/> is set. The result is null
    /// when the reference is empty, no resource database is configured, or the entry cannot be found. The resolved entry is
    /// cached and reused until the reference changes or the resolving locale changes, so repeated calls do not resolve
    /// again. The <see cref="LocalizedString"/> and <see cref="LocalizedAsset{TObject}"/> types build on this cache to
    /// format or load the same entry without resolving it every time.
    /// </remarks>
    /// <param name="cancellationToken">A token that cancels the asynchronous load. The default token never cancels.</param>
    /// <returns>An awaitable that produces the resolved entry, or null when it cannot be resolved.</returns>
    /// <example>
    /// <para>Load an entry asynchronously.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedEntryGetEntryAsyncExample.cs"/>
    /// </example>
    public Awaitable<TEntry> GetEntryAsync(CancellationToken cancellationToken = default)
    {
        if (!LocalizationSettings.InitializationSettled)
            return InitializeThenResolveAsync(cancellationToken);
        return GetEntryAsync(ResolvingLocale(), cancellationToken);
    }

    internal Awaitable<TEntry> GetEntryAsync(Locale locale, CancellationToken cancellationToken)
    {
        var localeId = locale != null ? locale.Identifier : default;
        if (TryGetCachedEntry(localeId, out var cached))
            return AwaitableUtility.FromResult(cached);
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return AwaitableUtility.FromResult<TEntry>(null);
        return ResolveAsync(database, locale, localeId, cancellationToken);
    }

    async Awaitable<TEntry> InitializeThenResolveAsync(CancellationToken cancellationToken)
    {
        await LocalizationSettings.EnsureInitializedForUse();
        return await GetEntryAsync(ResolvingLocale(), cancellationToken);
    }

    async Awaitable<TEntry> ResolveAsync(ResourceDatabase database, Locale locale, LocaleIdentifier localeId, CancellationToken cancellationToken)
    {
        var entry = await database.GetEntryAsync(TableReference, TableEntryReference, locale, cancellationToken, EnableFallback) as TEntry;
        CacheEntry(entry, localeId, missIsFinal: true);
        return entry;
    }

    /// <summary>
    /// Resolves the entry synchronously from already-loaded tables without triggering a load.
    /// </summary>
    /// <remarks>
    /// The entry is resolved as <typeparamref name="TEntry"/>. The result is null when the required table is not loaded, the
    /// reference is empty, no resource database is configured, or the entry cannot be found. Use <see cref="GetEntryAsync"/>
    /// to load the table on demand instead. The resolved entry is cached and reused until the reference changes or the
    /// resolving locale changes.
    /// </remarks>
    /// <returns>The resolved entry, or null when it is not available synchronously.</returns>
    /// <example>
    /// <para>Read an entry from an already-loaded table.</para>
    /// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Reference/LocalizedEntryGetEntryExample.cs"/>
    /// </example>
    public TEntry GetEntry()
    {
        var locale = ResolvingLocale();
        var localeId = locale != null ? locale.Identifier : default;
        if (TryGetCachedEntry(localeId, out var cached))
            return cached;
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null || IsEmpty)
            return null;
        var entry = database.GetEntry(TableReference, TableEntryReference, locale, EnableFallback) as TEntry;
        CacheEntry(entry, localeId, missIsFinal: false);
        return entry;
    }

    /// <inheritdoc/>
    protected override void ForceUpdate() => InvalidateCache();

    void InvalidateCache()
    {
        m_CachedEntry = null;
        m_CacheValid = false;
    }

    bool TryGetCachedEntry(LocaleIdentifier locale, out TEntry entry)
    {
        if (m_CacheValid && m_CachedLocale.Equals(locale))
        {
            var database = LocalizationSettings.ResourceDatabase;
            if (database != null && database.ContentVersion == m_CachedVersion)
            {
                entry = m_CachedEntry;
                return true;
            }
        }
        entry = null;
        return false;
    }

    void CacheEntry(TEntry entry, LocaleIdentifier locale, bool missIsFinal)
    {
        if (entry == null && !missIsFinal)
        {
            InvalidateCache();
            return;
        }
        var database = LocalizationSettings.ResourceDatabase;
        if (database == null)
        {
            InvalidateCache();
            return;
        }
        m_CachedEntry = entry;
        m_CachedLocale = locale;
        m_CachedVersion = database.ContentVersion;
        m_CacheValid = true;
    }
}
