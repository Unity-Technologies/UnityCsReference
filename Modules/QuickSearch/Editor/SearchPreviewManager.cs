// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Search
{
    delegate SearchPreview FetchPreviewCallback(SearchItem item, SearchContext context, FetchPreviewOptions options, Vector2 size);
    delegate void AsyncFetchPreviewCallback(SearchItem item, SearchContext context, FetchPreviewOptions options, Vector2 size, OnPreviewReady onReadyCallback);
    delegate void OnPreviewReady(SearchItem item, SearchContext context, SearchPreview preview);

    readonly struct SearchPreviewKey : IComparable<SearchPreviewKey>, IEquatable<SearchPreviewKey>
    {
        public readonly int searchItemHashCode;
        public readonly FetchPreviewOptions options;
        public readonly Vector2 size;

        public SearchPreviewKey(int searchItemHashCode, FetchPreviewOptions options, in Vector2 size)
        {
            this.searchItemHashCode = searchItemHashCode;
            this.options = options;
            this.size = size;
        }

        public SearchPreviewKey(SearchItem item, FetchPreviewOptions options, in Vector2 size)
            : this(item.GetHashCode(), options, size)
        {}

        public int CompareTo(SearchPreviewKey other)
        {
            var compare = searchItemHashCode.CompareTo(other.searchItemHashCode);
            if (compare != 0)
                return compare;
            compare = options.CompareTo(other.options);
            if (compare != 0)
                return compare;
            return size.sqrMagnitude.CompareTo(other.size.sqrMagnitude);
        }

        public bool Equals(SearchPreviewKey other)
        {
            return searchItemHashCode == other.searchItemHashCode && options == other.options && size == other.size;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(searchItemHashCode, options, size);
        }
    }

    readonly struct SearchPreview
    {
        public readonly SearchPreviewKey key;
        public readonly Texture2D texture;
        public readonly DateTime creationTime;

        public bool valid => texture != null && texture;

        public static SearchPreview invalid = new(new SearchPreviewKey(0, FetchPreviewOptions.None, Vector2.zero), null);

        public SearchPreview(in SearchPreviewKey key, Texture2D texture)
            : this(key, texture, DateTime.UtcNow)
        {}

        public SearchPreview(in SearchPreviewKey key, Texture2D texture, in DateTime creationTime)
        {
            this.key = key;
            this.texture = texture;
            this.creationTime = creationTime;
        }

        public SearchPreview(SearchItem item, FetchPreviewOptions options, in Vector2 size, Texture2D texture)
            : this(new SearchPreviewKey(item, options, size), texture, DateTime.UtcNow)
        {}

        public SearchPreview(SearchItem item, FetchPreviewOptions options, in Vector2 size, Texture2D texture, in DateTime creationTime)
            : this(new SearchPreviewKey(item, options, size), texture, creationTime)
        {}
    }

    class SearchPreviewAsyncFetch
    {
        public readonly Action asyncCallbackOff;
        public readonly ConcurrentBag<OnPreviewReady> readyCallbacks;

        public SearchPreviewAsyncFetch(Action off)
        {
            this.readyCallbacks = new ConcurrentBag<OnPreviewReady>();
            this.asyncCallbackOff = off;
        }
    }

    class SearchPreviewManager
    {
        ConcurrentDictionary<SearchPreviewKey, SearchPreview> m_PreviewCollections = new();
        ConcurrentDictionary<SearchPreviewKey, SearchPreviewAsyncFetch> m_FetchPreviewOffs = new();

        public int poolSize { get; set; }
        public int count => m_PreviewCollections.Count;

        public SearchPreviewManager(int poolSize)
        {
            this.poolSize = poolSize;
        }

        public SearchPreviewManager()
        {
            poolSize = 50;
        }

        public Action FetchPreview(SearchItem item, SearchContext context, FetchPreviewOptions options, in Vector2 size, FetchPreviewCallback fetchCallback, OnPreviewReady readyCallback, double delayInSeconds = 0.2d)
        {
            return FetchPreview(item, context, new SearchPreviewKey(item, options, size), fetchCallback, readyCallback, delayInSeconds);
        }

        public Action FetchPreview(SearchItem item, SearchContext context, FetchPreviewOptions options, in Vector2 size, AsyncFetchPreviewCallback fetchCallback, OnPreviewReady readyCallback, double delayInSeconds = 0.2d)
        {
            return FetchPreview(item, context, new SearchPreviewKey(item, options, size), fetchCallback, readyCallback, delayInSeconds);
        }

        public Action FetchPreview(SearchItem item, SearchContext context, in SearchPreviewKey key, FetchPreviewCallback fetchCallback, OnPreviewReady readyCallback, double delayInSeconds = 0.2d)
        {
            return FetchPreview(item, context, key, (searchItem, searchContext, options, size, callback) =>
            {
                var searchPreview = fetchCallback(searchItem, searchContext, options, size);
                callback(searchItem, searchContext, searchPreview);
            }, readyCallback, delayInSeconds);
        }

        public Action FetchPreview(SearchItem item, SearchContext context, in SearchPreviewKey key, AsyncFetchPreviewCallback fetchCallback, OnPreviewReady readyCallback, double delayInSeconds = 0.2d)
        {
            if (m_PreviewCollections.TryGetValue(key, out var searchPreview) && searchPreview.valid)
            {
                readyCallback?.Invoke(item, context, searchPreview);
                return () => { };
            }

            return FetchPreviewAsync(item, context, key, fetchCallback, readyCallback, delayInSeconds);
        }

        public SearchPreview FetchPreview(SearchItem item, FetchPreviewOptions options, in Vector2 size)
        {
            return FetchPreview(new SearchPreviewKey(item, options, size));
        }

        public SearchPreview FetchPreview(in SearchPreviewKey key)
        {
            if (!m_PreviewCollections.TryGetValue(key, out var searchPreview))
                return SearchPreview.invalid;
            if (!searchPreview.valid)
                return SearchPreview.invalid;
            return searchPreview;
        }

        public void CancelFetch(SearchItem item, FetchPreviewOptions options, in Vector2 size)
        {
            CancelFetch(new SearchPreviewKey(item, options, size));
        }

        public void CancelFetch(in SearchPreviewKey key)
        {
            if (!m_FetchPreviewOffs.TryRemove(key, out var previewAsyncFetch))
                return;
            previewAsyncFetch.asyncCallbackOff?.Invoke();
            previewAsyncFetch.readyCallbacks.Clear();
        }

        public void ReleasePreview(SearchItem item, FetchPreviewOptions options, in Vector2 size)
        {
            ReleasePreview(new SearchPreviewKey(item, options, size));
        }

        public void ReleasePreview(in SearchPreviewKey key)
        {
            m_PreviewCollections.TryRemove(key, out _);
            CancelFetch(key);
        }

        public void ReleaseOldPreviews(TimeSpan elapsedTime)
        {
            var now = DateTime.UtcNow;
            var oldPreviews = m_PreviewCollections.Where(pair =>
            {
                var lifeTime = now - pair.Value.creationTime;
                return lifeTime > elapsedTime;
            }).Select(pair => pair.Key).ToArray();
            foreach (var oldPreviewKey in oldPreviews)
            {
                ReleasePreview(oldPreviewKey);
            }
        }

        public bool HasPreview(SearchItem item, FetchPreviewOptions options, in Vector2 size)
        {
            return m_PreviewCollections.TryGetValue(new SearchPreviewKey(item, options, size), out _);
        }

        public bool HasPreview(in SearchPreviewKey key)
        {
            return m_PreviewCollections.TryGetValue(key, out _);
        }

        internal bool IsAnyPreviewRequestedForItem(SearchItem item)
        {
            var itemHash = item.GetHashCode();
            foreach (var k in m_FetchPreviewOffs.Keys)
                if (k.searchItemHashCode == itemHash)
                    return true;
            return false;
        }

        internal bool IsAnyPreviewLoadedForItem(SearchItem item)
        {
            var itemHash = item.GetHashCode();
            foreach (var k in m_PreviewCollections.Keys)
                if (k.searchItemHashCode == itemHash)
                    return true;
            return false;
        }

        public void Clear()
        {
            m_PreviewCollections.Clear();

            // Not really atomic
            var asyncFetchOffs = m_FetchPreviewOffs.ToArray();
            m_FetchPreviewOffs.Clear();
            foreach (var kvp in asyncFetchOffs)
            {
                kvp.Value.asyncCallbackOff();
            }
        }

        Action FetchPreviewAsync(SearchItem item, SearchContext context, in SearchPreviewKey key, AsyncFetchPreviewCallback fetchCallback, OnPreviewReady readyCallback, double delayInSeconds)
        {
            SearchPreviewAsyncFetch created = null;
            var localKey = key;
            var asyncFetch = m_FetchPreviewOffs.GetOrAdd(key, previewKey =>
            {
                // I don't think we will often be in a situation where we try to fetch multiple previews for the same key
                // therefore I think this is fine.
                var asyncCallbackOff = Utils.CallDelayed(() => OnAsyncFetch(item, context, localKey, fetchCallback), delayInSeconds);
                created = new SearchPreviewAsyncFetch(asyncCallbackOff);
                return created;
            });
            if (created != null && !ReferenceEquals(created, asyncFetch))
                created.asyncCallbackOff();

            asyncFetch.readyCallbacks.Add(readyCallback);

            return asyncFetch.asyncCallbackOff;
        }

        void OnAsyncFetch(SearchItem item, SearchContext context, in SearchPreviewKey key, AsyncFetchPreviewCallback fetchCallback)
        {
            // If we were already canceled, return
            if (!m_FetchPreviewOffs.ContainsKey(key))
                return;
            var localKey = key;
            fetchCallback(item, context, key.options, key.size, (searchItem, searchContext, preview) => OnAsyncFetchDone(searchItem, searchContext, preview, localKey));
        }

        void OnAsyncFetchDone(SearchItem item, SearchContext context, in SearchPreview searchPreview, in SearchPreviewKey originalKey)
        {
            if (searchPreview.valid)
                AddSearchPreview(searchPreview);

            // Might have been canceled and removed already
            if (!m_FetchPreviewOffs.Remove(originalKey, out var asyncFetch))
                return;

            if (InternalEditorUtility.CurrentThreadIsMainThread())
            {
                DispatchFetchPreviewResult(asyncFetch.readyCallbacks, item, context, searchPreview);
            }
            else
            {
                var localPreview = searchPreview;
                Dispatcher.Enqueue(() =>
                {
                    DispatchFetchPreviewResult(asyncFetch.readyCallbacks, item, context, localPreview);
                });
            }
        }

        static void DispatchFetchPreviewResult(ConcurrentBag<OnPreviewReady> readyCallbacks, SearchItem item, SearchContext context, in SearchPreview preview)
        {
            while (readyCallbacks.TryTake(out var readyCallback))
            {
                readyCallback?.Invoke(item, context, preview);
            }
        }

        void AddSearchPreview(in SearchPreview searchPreview)
        {
            // This block is not atomic. There is a chance that we add multiple previews and check the size at the same time.
            // This means that we might end up removing more than we needed.
            // Also, this doesn't protect against releases that might happen at the same time, once again leading to more
            // previews being removed than necessary.
            // But there is a guarantee that we will never have more than poolsize item.
            var localPreview = searchPreview;
            m_PreviewCollections.AddOrUpdate(searchPreview.key, localPreview, (_, _) => localPreview);
            while (m_PreviewCollections.Count > poolSize)
            {
                var oldestKey = FindOldestPreview();
                if (m_PreviewCollections.TryRemove(oldestKey, out _))
                    return;
                // Since Count and TryRemove already spins for access, we don't need to spin before retrying.
            }
        }

        SearchPreviewKey FindOldestPreview()
        {
            SearchPreviewKey oldestKey = new();
            var oldestLifeTime = new TimeSpan(0);
            var now = DateTime.UtcNow;
            foreach (var (key, preview) in m_PreviewCollections)
            {
                var previewLifeTime = now - preview.creationTime;
                if (previewLifeTime > oldestLifeTime)
                {
                    oldestKey = key;
                    oldestLifeTime = previewLifeTime;
                }
            }

            return oldestKey;
        }
    }
}
