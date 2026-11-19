// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Unity.Localization;

/// <summary>
/// Caches assets scoped per <see cref="LocaleIdentifier"/>. Each request supplies how to load and release its
/// asset, so the same cache serves table loads (through the provider chain) and localized-asset loads (through
/// the owning entry).
/// </summary>
/// <remarks>
/// Concurrent requests for the same key share a single underlying load. Cached assets are released per locale
/// through <see cref="ReleaseAssetsForLocale"/>, or all at once through <see cref="ReleaseAll"/>. Each load is
/// handed back exactly once, through the releaser captured for it, and a release waits while another entry still
/// serves the same asset. All members must be called from the main thread.
/// </remarks>
sealed class LocaleAssetCache
{
    readonly Dictionary<CacheKey, CacheEntry> m_Cache = new();
    readonly Dictionary<CacheKey, PendingLoad> m_PendingLoads = new();
    readonly Dictionary<LocaleIdentifier, HashSet<CacheKey>> m_ByLocale = new();
    readonly Dictionary<EntityId, AssetUses> m_AssetUses = new();
    // Keys whose wrong-typed load could not be released yet, mapped to the asset that holds it back.
    Dictionary<CacheKey, EntityId> m_DeferredMismatches;
    // Keys already warned about a type mismatch, so a per-frame retry does not flood the console.
    HashSet<CacheKey> m_TypeMismatchWarned;

    /// <summary>
    /// Loads, or returns a cached, asset for <paramref name="locale"/> under <paramref name="cacheId"/>; <see langword="null"/> on a miss.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="locale">The locale scope.</param>
    /// <param name="cacheId">A stable key unique within the locale.</param>
    /// <param name="loader">Loads the asset when not cached.</param>
    /// <param name="releaser">Releases the asset on eviction.</param>
    /// <param name="cancellationToken">Token that cancels the load.</param>
    public Awaitable<T> GetAssetAsync<T>(LocaleIdentifier locale, string cacheId, Func<CancellationToken, Awaitable<Object>> loader, Action<Object> releaser, CancellationToken cancellationToken = default) where T : Object
    {
        if (!cancellationToken.IsCancellationRequested && TryPeekCached(locale, cacheId, typeof(T), out var cached) && cached != null)
            return AwaitableUtility.FromResult(cached as T);
        return CastAsync<T>(GetAssetObjectAsync(locale, cacheId, typeof(T), loader, releaser, cancellationToken));
    }

    /// <summary>
    /// Returns a cached asset, or loads it synchronously through <paramref name="syncLoader"/>; <see langword="null"/> on a miss.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="locale">The locale scope.</param>
    /// <param name="cacheId">A stable key unique within the locale.</param>
    /// <param name="syncLoader">Loads the asset immediately when not cached.</param>
    /// <param name="releaser">Releases the asset on eviction.</param>
    public T GetAsset<T>(LocaleIdentifier locale, string cacheId, Func<Object> syncLoader, Action<Object> releaser) where T : Object
    {
        if (TryPeekCached(locale, cacheId, typeof(T), out var cached) && cached != null)
            return cached as T;
        var cacheKey = new CacheKey(locale, cacheId, typeof(T));
        // Defer to an async load already in flight for this key rather than racing it and caching a duplicate.
        if (m_PendingLoads.ContainsKey(cacheKey))
            return null;
        if (MismatchDeferred(cacheKey))
            return null;
        var asset = syncLoader();
        if (asset == null)
            return null;
        if (asset is not T typed)
        {
            WarnTypeMismatch(cacheKey, asset);
            RecordMismatch(cacheKey, asset, releaser);
            return null;
        }
        Insert(cacheKey, asset, releaser);
        return typed;
    }

    /// <summary>
    /// Releases and evicts every asset cached for <paramref name="locale"/>.
    /// </summary>
    public void ReleaseAssetsForLocale(LocaleIdentifier locale)
    {
        if (m_ByLocale.TryGetValue(locale, out var keys))
        {
            // Snapshot: a releaser can re-enter and insert into the very collections being walked.
            using var pooled = ListPool<CacheKey>.Get(out var snapshot);
            snapshot.AddRange(keys);
            m_ByLocale.Remove(locale);
            for (var i = 0; i < snapshot.Count; i++)
            {
                if (m_Cache.TryGetValue(snapshot[i], out var entry))
                {
                    m_Cache.Remove(snapshot[i]);
                    DropUse(entry);
                }
            }
        }
        // Always cancel in-flight loads for the locale, even when nothing is cached yet.
        CancelPendingLoads(locale);
        // The held-back releases stay with the asset; only this locale's licence to skip the reload goes.
        ForgetMismatches(locale);
    }

    /// <summary>
    /// Releases and evicts every cached asset.
    /// </summary>
    public void ReleaseAll()
    {
        // Snapshot: a releaser can re-enter and insert into the very collections being walked.
        using var pooled = ListPool<CacheEntry>.Get(out var entries);
        entries.AddRange(m_Cache.Values);
        m_Cache.Clear();
        m_ByLocale.Clear();
        m_DeferredMismatches?.Clear();
        for (var i = 0; i < entries.Count; i++)
        {
            DropUse(entries[i]);
        }
        CancelPendingLoads(null);
    }

    void DropUse(CacheEntry entry)
    {
        if (!m_AssetUses.TryGetValue(entry.EntityId, out var uses))
        {
            SafeRelease(entry.Releaser, entry.Asset);
            return;
        }
        if (!uses.Release())
        {
            uses.Defer(entry.Releaser);
            return;
        }
        m_AssetUses.Remove(entry.EntityId);
        ForgetMismatches(entry.EntityId);
        SafeRelease(entry.Releaser, entry.Asset);
        var deferred = uses.Deferred;
        if (deferred == null)
            return;
        for (var i = 0; i < deferred.Count; i++)
        {
            ReleaseUnkept(entry.EntityId, entry.Asset, deferred[i]);
        }
    }

    // A load the cache is not keeping took a reference, so hand it back unless a live entry still serves the asset.
    bool ReleaseUnkept(EntityId entityId, Object asset, Action<Object> releaser)
    {
        if (m_AssetUses.TryGetValue(entityId, out var uses))
        {
            uses.Defer(releaser);
            return true;
        }
        SafeRelease(releaser, asset);
        return false;
    }

    // A mismatch that had to hold its release back repeats the load on every retry, so stop reloading while it stands.
    bool MismatchDeferred(CacheKey cacheKey)
    {
        if (m_DeferredMismatches == null || !m_DeferredMismatches.TryGetValue(cacheKey, out var entityId))
            return false;
        if (m_AssetUses.ContainsKey(entityId))
            return true;
        m_DeferredMismatches.Remove(cacheKey);
        return false;
    }

    void RecordMismatch(CacheKey cacheKey, Object asset, Action<Object> releaser)
    {
        var entityId = asset.GetEntityId();
        if (ReleaseUnkept(entityId, asset, releaser))
            (m_DeferredMismatches ??= new Dictionary<CacheKey, EntityId>())[cacheKey] = entityId;
    }

    void ForgetMismatches(LocaleIdentifier locale)
    {
        if (m_DeferredMismatches == null || m_DeferredMismatches.Count == 0)
            return;
        using var pooled = ListPool<CacheKey>.Get(out var keys);
        keys.AddRange(m_DeferredMismatches.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            if (keys[i].Locale.Equals(locale))
                m_DeferredMismatches.Remove(keys[i]);
        }
    }

    void ForgetMismatches(EntityId entityId)
    {
        if (m_DeferredMismatches == null || m_DeferredMismatches.Count == 0)
            return;
        using var pooled = ListPool<CacheKey>.Get(out var keys);
        keys.AddRange(m_DeferredMismatches.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            if (m_DeferredMismatches[keys[i]].Equals(entityId))
                m_DeferredMismatches.Remove(keys[i]);
        }
    }

    void CancelPendingLoads(LocaleIdentifier? locale)
    {
        if (m_PendingLoads.Count == 0)
            return;
        using var pooled = ListPool<CacheKey>.Get(out var keys);
        keys.AddRange(m_PendingLoads.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            if (locale.HasValue && !keys[i].Locale.Equals(locale.Value))
                continue;
            var load = m_PendingLoads[keys[i]];
            m_PendingLoads.Remove(keys[i]);
            load.LoadCts.Cancel();
        }
    }

    Awaitable<Object> GetAssetObjectAsync(LocaleIdentifier locale, string cacheId, System.Type type, Func<CancellationToken, Awaitable<Object>> loader, Action<Object> releaser, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cacheKey = new CacheKey(locale, cacheId, type);

        if (m_Cache.TryGetValue(cacheKey, out var cached))
        {
            if (cached.Asset != null)
                return AwaitableUtility.FromResult(cached.Asset);
            // The asset was destroyed or unloaded out from under us; drop it and reload.
            m_Cache.Remove(cacheKey);
            RemoveFromLocaleIndex(cacheKey);
            DropUse(cached);
        }

        if (m_PendingLoads.TryGetValue(cacheKey, out var load))
            return AddWaiter(load, cacheKey, cancellationToken);

        if (MismatchDeferred(cacheKey))
            return AwaitableUtility.FromResult<Object>(null);

        load = new PendingLoad { Loader = loader, Releaser = releaser };
        m_PendingLoads.Add(cacheKey, load);
        var awaitable = AddWaiter(load, cacheKey, cancellationToken);
        RunLoadAsync(cacheKey, load);
        return awaitable;
    }

    Awaitable<Object> AddWaiter(PendingLoad load, CacheKey cacheKey, CancellationToken cancellationToken)
    {
        var waiter = new Waiter { Source = new AwaitableCompletionSource<Object>() };
        load.Waiters.Add(waiter);
        if (cancellationToken.CanBeCanceled)
        {
            var mainThreadId = Thread.CurrentThread.ManagedThreadId;
            waiter.Registration = cancellationToken.Register(() =>
            {
                if (Thread.CurrentThread.ManagedThreadId == mainThreadId)
                    CancelWaiter(load, cacheKey, waiter);
                else
                    CancelWaiterOnMainThread(load, cacheKey, waiter);
            });
        }
        return waiter.Source.Awaitable;
    }

    async void CancelWaiterOnMainThread(PendingLoad load, CacheKey cacheKey, Waiter waiter)
    {
        try
        {
            await Awaitable.MainThreadAsync();
            CancelWaiter(load, cacheKey, waiter);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void CancelWaiter(PendingLoad load, CacheKey cacheKey, Waiter waiter)
    {
        if (waiter.Done)
            return;
        waiter.Done = true;
        waiter.Registration.Dispose();
        waiter.Source.TrySetCanceled();
        if (!load.Waiters.Remove(waiter))
            return;
        if (load.Waiters.Count == 0 && m_PendingLoads.TryGetValue(cacheKey, out var current) && current == load)
        {
            m_PendingLoads.Remove(cacheKey);
            load.LoadCts.Cancel();
        }
    }

    // async void: anything escaping the delivery itself surfaces as an unhandled exception instead of vanishing.
    async void RunLoadAsync(CacheKey cacheKey, PendingLoad load)
    {
        Object result = null;
        Exception error = null;
        try
        {
            await Awaitable.MainThreadAsync();
            result = await load.Loader(load.LoadCts.Token);
        }
        catch (OperationCanceledException)
        {
            // The last waiter cancelled the shared load; there is nothing to deliver.
        }
        catch (Exception e)
        {
            error = e;
        }

        await Awaitable.MainThreadAsync();

        var stillCurrent = m_PendingLoads.TryGetValue(cacheKey, out var current) && current == load;
        if (stillCurrent)
            m_PendingLoads.Remove(cacheKey);

        // Delivery must run even if Insert or the releaser throws, or waiters hang forever and the CTS leaks.
        try
        {
            if (error == null && result != null)
            {
                // A wrong-typed result is not cached, as in TryGet.
                if (!cacheKey.Type.IsInstanceOfType(result))
                {
                    WarnTypeMismatch(cacheKey, result);
                    RecordMismatch(cacheKey, result, load.Releaser);
                    result = null;
                }
                else if (stillCurrent)
                {
                    Insert(cacheKey, result, load.Releaser);
                }
                else
                {
                    ReleaseUnkept(result.GetEntityId(), result, load.Releaser);
                    result = null;
                }
            }
        }
        finally
        {
            var waiters = load.Waiters.ToArray();
            load.Waiters.Clear();
            for (var i = 0; i < waiters.Length; i++)
            {
                var waiter = waiters[i];
                if (waiter.Done)
                    continue;
                waiter.Done = true;
                waiter.Registration.Dispose();
                try
                {
                    if (error != null)
                        waiter.Source.TrySetException(error);
                    else
                        waiter.Source.TrySetResult(result);
                }
                catch (Exception e)
                {
                    // A continuation resumed by the completion threw; keep delivering to the remaining waiters.
                    Debug.LogException(e);
                }
            }
            load.LoadCts.Dispose();
        }
    }

    void WarnTypeMismatch(CacheKey cacheKey, Object asset)
    {
        if (!(m_TypeMismatchWarned ??= new HashSet<CacheKey>()).Add(cacheKey))
            return;
        Debug.LogWarning($"Localized asset '{cacheKey.Id}' loaded as '{asset.GetType().Name}' but '{cacheKey.Type.Name}' was requested; treating it as missing.");
    }

    static void SafeRelease(Action<Object> releaser, Object asset)
    {
        try
        {
            releaser?.Invoke(asset);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    void Insert(CacheKey cacheKey, Object asset, Action<Object> releaser)
    {
        // Superseding an entry drops its use first, so the counts track live entries exactly.
        if (m_Cache.TryGetValue(cacheKey, out var superseded))
        {
            m_Cache.Remove(cacheKey);
            DropUse(superseded);
        }
        var entityId = asset.GetEntityId();
        m_Cache[cacheKey] = new CacheEntry(asset, releaser, entityId);
        if (!m_AssetUses.TryGetValue(entityId, out var uses))
            m_AssetUses[entityId] = uses = new AssetUses();
        uses.Acquire();
        if (!m_ByLocale.TryGetValue(cacheKey.Locale, out var keys))
        {
            keys = new HashSet<CacheKey>();
            m_ByLocale.Add(cacheKey.Locale, keys);
        }
        keys.Add(cacheKey);
    }

    void RemoveFromLocaleIndex(CacheKey cacheKey)
    {
        if (m_ByLocale.TryGetValue(cacheKey.Locale, out var keys))
            keys.Remove(cacheKey);
    }

    static async Awaitable<T> CastAsync<T>(Awaitable<Object> source) where T : Object
    {
        var asset = await source;
        return asset as T;
    }

    /// <summary>
    /// Returns the cached asset for (<paramref name="locale"/>, <paramref name="cacheId"/>) without loading; <see langword="false"/> on a miss.
    /// </summary>
    /// <typeparam name="T">The expected asset type.</typeparam>
    /// <param name="locale">The locale scope.</param>
    /// <param name="cacheId">The stable cache key.</param>
    /// <param name="asset">The cached asset, or <see langword="null"/>.</param>
    public bool TryGetCached<T>(LocaleIdentifier locale, string cacheId, out T asset) where T : Object
    {
        if (TryPeekCached(locale, cacheId, typeof(T), out var obj) && obj != null && obj is T typed)
        {
            asset = typed;
            return true;
        }
        asset = null;
        return false;
    }

    bool TryPeekCached(LocaleIdentifier locale, string cacheId, System.Type type, out Object asset)
    {
        if (m_Cache.TryGetValue(new CacheKey(locale, cacheId, type), out var entry))
        {
            asset = entry.Asset;
            return true;
        }
        asset = null;
        return false;
    }

    internal int CachedCount => m_Cache.Count;
    internal int PendingLoadCount => m_PendingLoads.Count;

    sealed class AssetUses
    {
        int m_LiveEntries;

        // Releases held back while a live entry hands the asset out; a releaser that unloads would destroy it.
        public List<Action<Object>> Deferred { get; private set; }

        public void Acquire() => m_LiveEntries++;

        public bool Release() => --m_LiveEntries <= 0;

        public void Defer(Action<Object> releaser) => (Deferred ??= new List<Action<Object>>()).Add(releaser);
    }

    readonly struct CacheEntry
    {
        public readonly Object Asset;
        public readonly Action<Object> Releaser;
        // Captured at insert time so eviction can still find the asset's uses after it is destroyed.
        public readonly EntityId EntityId;

        public CacheEntry(Object asset, Action<Object> releaser, EntityId entityId)
        {
            Asset = asset;
            Releaser = releaser;
            EntityId = entityId;
        }
    }

    readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly LocaleIdentifier Locale;
        public readonly string Id;
        public readonly System.Type Type;

        public CacheKey(LocaleIdentifier locale, string id, System.Type type)
        {
            Locale = locale;
            Id = id;
            Type = type;
        }

        public bool Equals(CacheKey other) => Locale.Equals(other.Locale) && string.Equals(Id, other.Id, StringComparison.Ordinal) && Type == other.Type;

        public override bool Equals(object obj) => obj is CacheKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Locale, Id, Type);
    }

    sealed class PendingLoad
    {
        public readonly List<Waiter> Waiters = new();
        public readonly CancellationTokenSource LoadCts = new();
        public Func<CancellationToken, Awaitable<Object>> Loader;
        public Action<Object> Releaser;
    }

    sealed class Waiter
    {
        public AwaitableCompletionSource<Object> Source;
        public CancellationTokenRegistration Registration;
        public bool Done;
    }
}
