// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace UnityEditor.Search
{
    /// <summary>
    /// An async search session tracks all incoming items found by a search provider that weren't returned right away after the search was initiated.
    /// </summary>
    class SearchSession
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

        private const long k_MaxTimePerUpdate = 10; // milliseconds

        private SearchEnumerator<SearchItem> m_ItemsEnumerator = new SearchEnumerator<SearchItem>();
        private long m_MaxFetchTimePerProviderMs;
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
        /// Called when the system is ready to process any new async results.
        /// </summary>
        public void OnUpdate()
        {
            var newItems = new List<SearchItem>();
            var atEnd = !FetchSome(newItems, m_MaxFetchTimePerProviderMs);

            if (newItems.Count > 0)
                SendItems(newItems);

            if (atEnd)
            {
                Stop();
            }
        }

        /// <summary>
        /// Resolved a batch of items asynchronously.
        /// </summary>
        /// <param name="items"></param>
        public void SendItems(IEnumerable<SearchItem> items)
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
            m_MaxFetchTimePerProviderMs = maxFetchTimePerProviderMs;
            if (itemEnumerator != null)
            {
                m_ItemsEnumerator = new SearchEnumerator<SearchItem>(itemEnumerator);
                Utils.tick += OnUpdate;
            }
        }

        internal void Start()
        {
            sessionStarted?.Invoke(m_Context);
        }

        /// <summary>
        /// Stop the async search session and discard any new search results.
        /// </summary>
        public void Stop()
        {
            if (searchInProgress)
                sessionEnded?.Invoke(m_Context);

            searchInProgress = false;
            Utils.tick -= OnUpdate;
            m_ItemsEnumerator.Dispose();
        }

        /// <summary>
        /// Request to fetch new async search results.
        /// </summary>
        /// <param name="items">The list of items to append new results to.</param>
        /// <param name="quantity">The maximum amount of items to be added to @items</param>
        /// <param name="doNotCountNull">Ignore all yield return null results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<SearchItem> items, int quantity, bool doNotCountNull)
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
        public bool FetchSome(List<SearchItem> items, int quantity, bool doNotCountNull, long maxFetchTimeMs)
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
        public bool FetchSome(List<SearchItem> items, long maxFetchTimeMs)
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
    /// A MultiProviderAsyncSearchSession holds all the providers' async search sessions.
    /// </summary>
    class MultiProviderAsyncSearchSession
    {
        private Dictionary<string, SearchSession> m_SearchSessions = new Dictionary<string, SearchSession>();

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
                session.sessionStarted += OnProviderAsyncSessionStarted;
                session.sessionEnded += OnProviderAsyncSessionEnded;
                session.asyncItemReceived += OnProviderAsyncItemReceived;
                m_SearchSessions.Add(providerId, session);
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

        /// <summary>
        /// Stops all active async search sessions held by this MultiProviderAsyncSearchSession.
        /// </summary>
        public void StopAllAsyncSearchSessions()
        {
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
