// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEditor.Search
{
    class BaseAsyncIEnumerableHandler<T>
    {
        protected SearchEnumerator<T> m_ItemsEnumerator = new SearchEnumerator<T>();

        protected const long k_MaxTimePerUpdate = 10; // milliseconds
        protected long maxFetchTimePerUpdate { get; set; } = k_MaxTimePerUpdate;

        public BaseAsyncIEnumerableHandler()
        {
        }

        public BaseAsyncIEnumerableHandler(object itemEnumerator)
        {
            Reset(itemEnumerator);
        }

        public virtual void OnUpdate()
        {
            var newItems = new List<T>();
            var atEnd = !FetchSome(newItems, maxFetchTimePerUpdate);

            if (newItems.Count > 0)
                SendItems(newItems);

            if (atEnd)
            {
                Stop();
            }
        }

        public virtual void SendItems(IEnumerable<T> items) {}

        internal virtual void Start() {}

        public virtual void Stop()
        {
            Utils.tick -= OnUpdate;

            lock (this)
                m_ItemsEnumerator.Dispose();
        }

        public virtual void Reset(object itemEnumerator, long maxFetchTimePerUpdate = k_MaxTimePerUpdate)
        {
            // Remove and add the event handler in case it was already removed.
            Stop();
            this.maxFetchTimePerUpdate = maxFetchTimePerUpdate;
            if (itemEnumerator != null)
            {
                lock (this)
                    m_ItemsEnumerator = new SearchEnumerator<T>(itemEnumerator);
                Utils.tick += OnUpdate;
            }
        }

        /// <summary>
        /// Request to fetch new async search results.
        /// </summary>
        /// <param name="items">The list of items to append new results to.</param>
        /// <param name="quantity">The maximum amount of items to be added to @items</param>
        /// <param name="doNotCountNull">Ignore all yield return null results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<T> items, int quantity, bool doNotCountNull)
        {
            if (m_ItemsEnumerator.Count == 0)
                return false;

            var atEnd = false;
            for (var i = 0; i < quantity && !atEnd; ++i)
            {
                atEnd = !m_ItemsEnumerator.NextItem(out var item);
                if (item == null)
                {
                    if (doNotCountNull)
                        --i;
                    continue;
                }
                items.Add(item);
            }

            return !atEnd;
        }

        /// <summary>
        /// Request to fetch new async search results.
        /// </summary>
        /// <param name="items">The list of items to append new results to.</param>
        /// <param name="quantity">The maximum amount of items to add to @items</param>
        /// <param name="doNotCountNull">Ignore all yield return null results.</param>
        /// <param name="maxFetchTimeMs">The amount of time allowed to yield new results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<T> items, int quantity, bool doNotCountNull, long maxFetchTimeMs)
        {
            if (m_ItemsEnumerator.Count == 0)
                return false;

            var atEnd = false;
            var timeToFetch = Stopwatch.StartNew();
            for (var i = 0; i < quantity && !atEnd && timeToFetch.ElapsedMilliseconds < maxFetchTimeMs; ++i)
            {
                atEnd = !m_ItemsEnumerator.NextItem(out var item);
                if (item == null)
                {
                    if (doNotCountNull)
                        --i;
                    continue;
                }
                items.Add(item);
            }

            return !atEnd;
        }

        /// <summary>
        /// Request to fetch new async search results.
        /// </summary>
        /// <param name="items">The list of items to append new results to.</param>
        /// <param name="maxFetchTimeMs">The amount of time allowed to yield new results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<T> items, long maxFetchTimeMs)
        {
            if (m_ItemsEnumerator.Count == 0)
                return false;

            var atEnd = false;
            var timeToFetch = Stopwatch.StartNew();
            while (!atEnd && timeToFetch.ElapsedMilliseconds < maxFetchTimeMs)
            {
                atEnd = !m_ItemsEnumerator.NextItem(out var item);
                if (!atEnd && item != null)
                    items.Add(item);
            }

            return !atEnd;
        }
    }


    /// <summary>
    /// An async search session tracks all incoming items found by a search provider that weren't returned right away after the search was initiated.
    /// </summary>
    class SearchSession : BaseAsyncIEnumerableHandler<SearchItem>
    {
        /// <summary>
        /// This event is used to receive any async search result.
        /// </summary>
        public event Action<SearchContext, IEnumerable<SearchItem>> asyncItemReceived;

        /// <summary>
        /// This event is used to know when a search has started to fetch new search items.
        /// </summary>
        public event Action<SearchContext> sessionStarted;

        /// <summary>
        /// This event is used to know when a search has finished fetching items.
        /// </summary>
        public event Action<SearchContext> sessionEnded;

        private SearchContext m_Context;

        /// <summary>
        /// Checks if this async search session is active.
        /// </summary>
        public bool searchInProgress { get; set; } = false;

        public SearchSession(SearchContext context)
        {
            m_Context = context;
        }

        /// <summary>
        /// Resolved a batch of items asynchronously.
        /// </summary>
        /// <param name="items"></param>
        public override void SendItems(IEnumerable<SearchItem> items)
        {
            asyncItemReceived?.Invoke(m_Context, items);
        }

        /// <summary>
        /// Hard reset an async search session.
        /// </summary>
        /// <param name="itemEnumerator">The enumerator that will yield new search results. This object can be an IEnumerator or IEnumerable</param>
        /// <param name="maxFetchTimePerProviderMs">The amount of time allowed to yield new results.</param>
        /// <remarks>Normally async search sessions are re-used per search provider.</remarks>
        public void Reset(SearchContext context, object itemEnumerator, long maxFetchTimePerProviderMs = k_MaxTimePerUpdate)
        {
            // Remove and add the event handler in case it was already removed.
            Stop();
            searchInProgress = true;
            m_Context = context;
            maxFetchTimePerUpdate = maxFetchTimePerProviderMs;
            if (itemEnumerator != null)
            {
                lock (this)
                    m_ItemsEnumerator = new SearchEnumerator<SearchItem>(itemEnumerator);
                Utils.tick += OnUpdate;
            }
        }

        internal override void Start()
        {
            sessionStarted?.Invoke(m_Context);
            base.Start();
        }

        /// <summary>
        /// Stop the async search session and discard any new search results.
        /// </summary>
        public override void Stop()
        {
            if (searchInProgress)
                sessionEnded?.Invoke(m_Context);

            searchInProgress = false;
            base.Stop();
        }
    }

    /// <summary>
    /// A MultiProviderAsyncSearchSession holds all the providers' async search sessions.
    /// </summary>
    class MultiProviderAsyncSearchSession
    {
        private System.Threading.CancellationTokenSource m_CancelSource;
        private ConcurrentDictionary<string, SearchSession> m_SearchSessions = new ConcurrentDictionary<string, SearchSession>();

        /// <summary>
        /// This event is used to receive any async search result.
        /// </summary>
        public event Action<SearchContext, IEnumerable<SearchItem>> asyncItemReceived;

        /// <summary>
        /// This event is triggered when a session has started to fetch search items.
        /// </summary>
        public event Action<SearchContext> sessionStarted;

        /// <summary>
        /// This event is triggered when a session has finished to fetch all search items.
        /// </summary>
        public event Action<SearchContext> sessionEnded;

        /// <summary>
        /// Checks if any of the providers' async search are active.
        /// </summary>
        public bool searchInProgress => m_SearchSessions.Any(session => session.Value.searchInProgress);

        internal System.Threading.CancellationToken cancelToken => m_CancelSource?.Token ?? default(System.Threading.CancellationToken);

        /// <summary>
        /// Returns the specified provider's async search session.
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns>The provider's async search session.</returns>
        public SearchSession GetProviderSession(SearchContext context, string providerId)
        {
            if (!m_SearchSessions.TryGetValue(providerId, out var session))
            {
                session = new SearchSession(context);
                if (m_SearchSessions.TryAdd(providerId, session))
                {
                    session.sessionStarted += OnProviderAsyncSessionStarted;
                    session.sessionEnded += OnProviderAsyncSessionEnded;
                    session.asyncItemReceived += OnProviderAsyncItemReceived;
                }
                else
                    throw new Exception($"Failed to add session for {providerId}");
            }

            return session;
        }

        private void OnProviderAsyncSessionStarted(SearchContext context)
        {
            sessionStarted?.Invoke(context);
        }

        private void OnProviderAsyncSessionEnded(SearchContext context)
        {
            sessionEnded?.Invoke(context);
        }

        private void OnProviderAsyncItemReceived(SearchContext context, IEnumerable<SearchItem> items)
        {
            asyncItemReceived?.Invoke(context, items);
        }

        public void StartSessions()
        {
            if (m_CancelSource != null)
            {
                m_CancelSource.Dispose();
                m_CancelSource = null;
            }
            m_CancelSource = new System.Threading.CancellationTokenSource();
        }

        /// <summary>
        /// Stops all active async search sessions held by this MultiProviderAsyncSearchSession.
        /// </summary>
        public void StopAllAsyncSearchSessions()
        {
            m_CancelSource?.Cancel();
            foreach (var searchSession in m_SearchSessions)
            {
                searchSession.Value.Stop();
            }
        }

        /// <summary>
        /// Clears all async search sessions held by this MultiProviderAsyncSearchSession.
        /// </summary>
        public void Clear()
        {
            m_CancelSource?.Dispose();
            m_CancelSource = null;

            foreach (var searchSession in m_SearchSessions)
            {
                searchSession.Value.asyncItemReceived -= OnProviderAsyncItemReceived;
                searchSession.Value.sessionStarted -= OnProviderAsyncSessionStarted;
                searchSession.Value.sessionEnded -= OnProviderAsyncSessionEnded;
            }
            m_SearchSessions.Clear();
            asyncItemReceived = null;
        }
    }
}
