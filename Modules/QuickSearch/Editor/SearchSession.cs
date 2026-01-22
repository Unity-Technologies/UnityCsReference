// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityEditor.Search
{
    readonly struct SearchSessionContext
    {
        public readonly SearchContext searchContext;
        public readonly Hash128 guid;

        public SearchSessionContext(SearchContext context, Hash128 guid)
        {
            this.searchContext = context;
            this.guid = guid;
        }

        public SearchSessionContext(SearchContext context)
            : this(context, Hash128.Compute(Guid.NewGuid().ToByteArray()))
        {}
    }

    class BaseAsyncIEnumerableHandler<T>
    {
        IAsyncIEnumerableHandlerUpdateMechanism<T> m_UpdateMechanism;
        protected IEnumerator<T> m_ItemsEnumerator = null;

        public IEnumerator<T> ItemsEnumerator => m_ItemsEnumerator;
        public IAsyncIEnumerableHandlerUpdateMechanism<T> UpdateMechanism => m_UpdateMechanism;

        public virtual void Update(List<T> newItems, TimeSpan maxFetchTime)
        {
            var atEnd = !FetchSome(newItems, maxFetchTime);

            if (newItems.Count > 0)
                SendItems(newItems);

            if (atEnd)
            {
                Stop();
            }
        }

        public virtual void SendItems(IEnumerable<T> items) {}

        public virtual void Start()
        {
            m_UpdateMechanism?.Start();
        }

        public virtual void Stop()
        {
            m_UpdateMechanism?.Stop();

            lock (this)
            {
                m_ItemsEnumerator?.Dispose();
                m_ItemsEnumerator = null;
            }
        }

        public virtual void Reset(IEnumerator<T> itemEnumerator, IAsyncIEnumerableHandlerUpdateMechanism<T> updateMechanism)
        {
            // Remove and add the event handler in case it was already removed.
            Stop();
            m_UpdateMechanism?.DetachFromHandler(this);
            m_UpdateMechanism = updateMechanism;
            m_UpdateMechanism.AttachToHandler(this);
            if (itemEnumerator != null)
            {
                lock (this)
                    m_ItemsEnumerator = itemEnumerator;
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
            if (m_ItemsEnumerator == null)
                return false;

            var atEnd = false;
            for (var i = 0; i < quantity && !atEnd; ++i)
            {
                atEnd = !m_ItemsEnumerator.MoveNext();
                var item = atEnd ? default : m_ItemsEnumerator.Current;
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
        /// <param name="maxFetchTime">The amount of time allowed to yield new results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<T> items, int quantity, bool doNotCountNull, TimeSpan maxFetchTime)
        {
            if (m_ItemsEnumerator == null)
                return false;

            var atEnd = false;
            var timeToFetch = Stopwatch.StartNew();
            for (var i = 0; i < quantity && !atEnd && timeToFetch.Elapsed < maxFetchTime; ++i)
            {
                atEnd = !m_ItemsEnumerator.MoveNext();
                var item = atEnd ? default : m_ItemsEnumerator.Current;
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
        /// <param name="maxFetchTime">The amount of time allowed to yield new results.</param>
        /// <returns>Returns true if there is still some results to fetch later or false if we've fetched everything remaining.</returns>
        public bool FetchSome(List<T> items, TimeSpan maxFetchTime)
        {
            if (m_ItemsEnumerator == null)
                return false;

            var atEnd = false;
            var timeToFetch = Stopwatch.StartNew();
            while (!atEnd && timeToFetch.Elapsed < maxFetchTime)
            {
                atEnd = !m_ItemsEnumerator.MoveNext();
                var item = atEnd ? default : m_ItemsEnumerator.Current;
                if (!atEnd && item != null)
                    items.Add(item);
            }

            return !atEnd;
        }

        /// <summary>
        /// Request to fetch a single async search result.
        /// </summary>
        /// <param name="item">Fetched item, if any.</param>
        /// <returns>True if we successfully retrieved an item, false otherwise.</returns>
        public bool FetchOne(out T item)
        {
            item = default;
            if (m_ItemsEnumerator == null)
                return false;
            var atEnd = !m_ItemsEnumerator.MoveNext();
            item = atEnd ? default : m_ItemsEnumerator.Current;
            return !atEnd;
        }
    }

    /// <summary>
    /// An async search session tracks all incoming items found by all search providers that weren't returned right away after the search was initiated.
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

        SearchSessionContext m_Context;
        CancellationTokenSource m_CancelSource;

        /// <summary>
        /// Checks if this async search session is active.
        /// </summary>
        public bool searchInProgress { get; set; } = false;

        public SearchContext context => m_Context.searchContext;
        public Hash128 guid => m_Context.guid;

        public SearchAggregateEnumerator<SearchItem> aggregateEnumerator => m_ItemsEnumerator as SearchAggregateEnumerator<SearchItem>;

        internal CancellationToken cancelToken => m_CancelSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Resolved a batch of items asynchronously.
        /// </summary>
        /// <param name="items"></param>
        public override void SendItems(IEnumerable<SearchItem> items)
        {
            asyncItemReceived?.Invoke(m_Context.searchContext, items);
        }

        /// <summary>
        /// Hard reset an async search session.
        /// </summary>
        /// <param name="sessionContext">The search session context for this search session.</param>
        /// <param name="updateMechanism">The update mechanism for this session. This dictates how the Update method is called.</param>
        public void Reset(SearchSessionContext sessionContext, IAsyncIEnumerableHandlerUpdateMechanism<SearchItem> updateMechanism)
        {
            base.Reset(new SearchAggregateEnumerator<SearchItem>(SearchAggregateEnumerationStyle.RoundRobin), updateMechanism);
            m_Context = sessionContext;

            // Reset calls Stop, so no need to redo the same thing here.
            // Just create a new cancellation token source.
            m_CancelSource = new CancellationTokenSource();
        }

        public override void Start()
        {
            // In case Reset was not called.
            m_CancelSource ??= new CancellationTokenSource();

            searchInProgress = true;
            sessionStarted?.Invoke(m_Context.searchContext);
            base.Start();
        }

        /// <summary>
        /// Stop the async search session and discard any new search results.
        /// </summary>
        public override void Stop()
        {
            m_CancelSource?.Cancel();
            m_CancelSource?.Dispose();
            m_CancelSource = null;

            if (searchInProgress)
                sessionEnded?.Invoke(m_Context.searchContext);

            searchInProgress = false;
            base.Stop();
        }

        public void AddProviderEnumerator(SearchProviderFetchEnumerator providerEnumerator)
        {
            if (aggregateEnumerator == null)
                throw new InvalidOperationException("Cannot add a provider enumerator to a non-aggregate enumerator.");

            aggregateEnumerator.AddEnumerator(providerEnumerator);
        }

        public int GetProviderCount()
        {
            return aggregateEnumerator?.Count ?? 0;
        }
    }
}
