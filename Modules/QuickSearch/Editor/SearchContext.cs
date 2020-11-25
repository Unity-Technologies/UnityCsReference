// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define QUICKSEARCH_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;

namespace UnityEditor.Search
{
    /// <summary>
    /// Various search options used to fetch items.
    /// </summary>
    [Flags]
    public enum SearchFlags
    {
        /// <summary>
        /// No specific search options.
        /// </summary>
        None = 0,

        /// <summary>
        /// Search items are fetch synchronously.
        /// </summary>
        Synchronous = 1 << 0,

        /// <summary>
        /// Fetch items will be sorted by the search service.
        /// </summary>
        Sorted = 1 << 1,

        /// <summary>
        /// Send the first items asynchronously
        /// </summary>
        FirstBatchAsync = 1 << 2,

        /// <summary>
        /// Sets the search to search for all results.
        /// </summary>
        WantsMore = 1 << 3,

        /// <summary>
        /// Adding debugging info while looking for results,
        /// </summary>
        Debug = 1 << 4,

        /// <summary>
        /// Prevent the search to use any indexing
        /// </summary>
        NoIndexing = 1 << 5,

        /// <summary>
        /// Default Search Flag
        /// </summary>
        Default = Sorted,

        /// <summary>
        /// Always show query errors even when there are results available.
        /// </summary>
        ShowErrorsWithResults = 1 << 24,

        /// <summary>
        /// Search View Flags
        /// </summary>
        SaveFilters = 1 << 25,

        /// <summary>
        /// Open QuickSearch reusing an existing window if any.
        /// </summary>
        ReuseExistingWindow = 1 << 26,

        /// <summary>
        /// Specify that a QuickSearch window list view supports multi-selection.
        /// </summary>
        Multiselect = 1 << 27,

        /// <summary>
        /// Specify that a QuickSearch window is dockable instead of being a modal popup window.
        /// </summary>
        Dockable = 1 << 28,

        /// <summary>
        /// Focus the search query when opening QuickSearch.
        /// </summary>
        FocusContext = 1 << 29,

        /// <summary>
        /// Hide all QuickSearch side panels.
        /// </summary>
        HidePanels = 1 << 30,

        /// <summary>
        /// Default options when opening a QuickSearch window.
        /// </summary>
        OpenDefault = SaveFilters | Multiselect | Dockable,
        /// <summary>
        /// Default options when opening a QuickSearch using the global shortcut.
        /// </summary>
        OpenGlobal = OpenDefault | ReuseExistingWindow,
        /// <summary>
        /// Options when opening QuickSearch in contextual mode (with only a few selected providers enabled).
        /// </summary>
        OpenContextual = Multiselect | Dockable | FocusContext,
        /// <summary>
        /// Options when opening QuickSearch as an Object Picker.
        /// </summary>
        OpenPicker = FocusContext | HidePanels | WantsMore
    }

    /// <summary>
    /// The search context encapsulate all the states necessary to perform a query. It allows the full
    /// customization of how a query would be performed.
    /// </summary>
    [DebuggerDisplay("{m_SearchText}")]
    public class SearchContext : IDisposable
    {
        private static readonly string[] k_Empty = new string[0];
        private string m_SearchText = "";
        private string m_CachedPhrase;
        private List<SearchQueryError> m_QueryErrors = new List<SearchQueryError>();

        [DebuggerDisplay("{provider} - Enabled: {enabled}")]
        internal class FilterDesc
        {
            public FilterDesc(SearchProvider provider, bool enabled)
            {
                this.provider = provider;
                this.enabled = enabled;
            }

            public readonly SearchProvider provider;
            public bool enabled;
        }

        private List<FilterDesc> m_ProviderDescs = new List<FilterDesc>();
        private bool m_Disposed = false;
        internal IEnumerable<FilterDesc> filters => m_ProviderDescs;

        internal SearchContext(SearchProvider provider)
        {
            m_ProviderDescs = new List<FilterDesc>() {  new FilterDesc(provider, true) };
            this.searchText = String.Empty;
            this.options = SearchFlags.Default;
        }

        /// <summary>
        /// Create a new search context.
        /// </summary>
        /// <param name="providers">The list of providers used to resolve the specified query.</param>
        /// <param name="searchText">The search query to perform.</param>
        /// <param name="options">Options to further controlled the query.</param>
        public SearchContext(IEnumerable<SearchProvider> providers, string searchText, SearchFlags options)
        {
            this.providers = providers.ToList();
            this.searchText = searchText;
            this.options = options;
        }

        /// <summary>
        /// Create a new search context.
        /// </summary>
        /// <param name="providers">The list of providers used to resolve the specified query.</param>
        /// <param name="searchText">The search query to perform.</param>
        public SearchContext(IEnumerable<SearchProvider> providers, string searchText)
            : this(providers, searchText, SearchFlags.Default)
        {
        }

        /// <summary>
        /// Create a new search context.
        /// </summary>
        /// <param name="providers">The list of providers used to resolve the specified query.</param>
        public SearchContext(IEnumerable<SearchProvider> providers)
            : this(providers, String.Empty, SearchFlags.Default)
        {
        }

        /// <summary>
        /// Search context finalizer.
        /// </summary>
        ~SearchContext()
        {
            Dispose(false);
        }

        /// <summary>
        /// Reset all provider filter to the specified value. This allows enabling or disabling all providers in one call.
        /// A disabled provider won't be ask to provider items to resolve the query.
        /// </summary>
        /// <param name="enableAll">If true enable all providers. If false disable all providers.</param>
        public void ResetFilter(bool enableAll)
        {
            foreach (var t in m_ProviderDescs)
                t.enabled = enableAll;
        }

        /// <summary>
        /// Enable or disable a single provider.
        /// A disabled provider won't be ask to provider items to resolve the query.
        /// </summary>
        /// <param name="providerId">Id of the provider. See SearchProvider.<see cref="SearchProvider.name"/>.id.</param>
        /// <param name="isEnabled">If true, enable the provider to perform query.</param>
        public void SetFilter(string providerId, bool isEnabled)
        {
            var index = m_ProviderDescs.FindIndex(t => t.provider.id == providerId);
            if (index != -1)
            {
                m_ProviderDescs[index].enabled = isEnabled;
            }
        }

        /// <summary>
        /// Checks if a provider is available to process a query.
        /// </summary>
        /// <param name="providerId">If of the provider. See SearchProvider.<see cref="SearchProvider.name"/>.id.</param>
        /// <returns></returns>
        public bool IsEnabled(string providerId)
        {
            var index = m_ProviderDescs.FindIndex(t => t.provider.id == providerId);
            if (index != -1)
            {
                return m_ProviderDescs[index].enabled;
            }

            return false;
        }

        /// <summary>
        /// Dispose of the Search Context
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Progress handle to set the search current progress.
        /// </summary>
        public int progressId { get; set; } = -1;

        /// <summary>
        /// Processed search query (no filterId, no textFilters)
        /// </summary>
        public string searchQuery { get; private set; } = String.Empty;

        /// <summary>
        /// Character offset of the processed search query in the raw search text.
        /// </summary>
        public int searchQueryOffset { get; private set; } = 0;

        /// <summary>
        /// Search query tokenized by words. All text filters are discarded and all words are lower cased.
        /// </summary>
        public string[] searchWords { get; private set; } = k_Empty;

        /// <summary>
        /// Returns a phrase that contains only words separated by spaces
        /// </summary>
        public string searchPhrase
        {
            get
            {
                if (m_CachedPhrase == null && searchWords.Length > 0)
                    m_CachedPhrase = string.Join(" ", searchWords).Trim();
                return m_CachedPhrase ?? String.Empty;
            }
        }

        /// <summary>
        /// All tokens containing a colon (':')
        /// </summary>
        public string[] textFilters { get; private set; } = k_Empty;

        /// <summary>
        /// Editor window that initiated the search.
        /// </summary>
        public EditorWindow focusedWindow { get; internal set; }

        /// <summary>
        /// Search context options
        /// </summary>
        public SearchFlags options { get; set; }

        /// <summary>
        /// Indicates if the search should return results as many as possible.
        /// </summary>
        public bool wantsMore
        {
            get
            {
                return options.HasFlag(SearchFlags.WantsMore);
            }

            set
            {
                if (value)
                    options |= SearchFlags.WantsMore;
                else
                    options &= ~SearchFlags.WantsMore;
            }
        }

        /// <summary>
        /// Raw search text (i.e. what is in the search text box)
        /// </summary>
        public string searchText
        {
            get => m_SearchText;

            set
            {
                if (m_SearchText.Equals(value))
                    return;

                m_SearchText = value ?? String.Empty;

                // Reset a few values
                filterId = null;
                textFilters = searchWords = k_Empty;
                searchQuery = searchText.TrimStart();
                searchQueryOffset = 0;

                if (String.IsNullOrEmpty(searchQuery))
                    return;


                foreach (var providerFilterId in m_ProviderDescs.Select(desc => desc.provider.filterId))
                {
                    if (searchQuery.StartsWith(providerFilterId, StringComparison.OrdinalIgnoreCase))
                    {
                        filterId = providerFilterId;
                        searchQuery = searchQuery.Remove(0, providerFilterId.Length).TrimStart();
                        break;
                    }
                }

                searchQueryOffset = searchText.Length - searchQuery.Length;
                searchQuery = searchQuery.TrimEnd();
                var tokens = searchQuery.ToLowerInvariant().Split(' ').ToArray();
                searchWords = tokens.Where(t => t.IndexOf(':') == -1).ToArray();
                textFilters = tokens.Where(t => t.IndexOf(':') != -1).ToArray();
            }
        }

        /// <summary>
        /// Which Providers are active for this particular context.
        /// </summary>
        public IEnumerable<SearchProvider> providers
        {
            get
            {
                if (filterId != null)
                    return m_ProviderDescs.Where(d => d.provider.filterId == filterId).Select(d => d.provider);

                if (m_ProviderDescs.Count == 1)
                    return m_ProviderDescs.Select(d => d.provider);

                return m_ProviderDescs.Where(d => d.enabled && !d.provider.isExplicitProvider).Select(d => d.provider);
            }

            private set
            {
                if (m_ProviderDescs?.Count > 0)
                    EndSession();

                if (value != null)
                    m_ProviderDescs = value.Select(provider => new FilterDesc(provider, true)).ToList();
                else
                    m_ProviderDescs?.Clear();

                BeginSession();
            }
        }

        /// <summary>
        /// Indicates if an asynchronous search is currently in progress for this context.
        /// </summary>
        public bool searchInProgress => sessions.searchInProgress;

        /// <summary>
        /// Return the search result selection if any.
        /// </summary>
        public SearchSelection selection => searchView?.selection;

        /// <summary>
        /// Search view holding and presenting the search results.
        /// </summary>
        public ISearchView searchView { get; internal set; }

        /// <summary>
        /// Explicit filter id. Usually it is the first search token like h:, p: to do an explicit search for a given provider.
        /// Can be null
        /// </summary>
        public string filterId { get; private set; }

        /// <summary>
        /// This event is used to receive any async search result.
        /// </summary>
        public event Action<SearchContext, IEnumerable<SearchItem>> asyncItemReceived
        {
            add
            {
                lock (this)
                    sessions.asyncItemReceived += value;
            }
            remove
            {
                lock (this)
                    sessions.asyncItemReceived -= value;
            }
        }

        /// <summary>
        /// Invoked when a Search is started.
        /// </summary>
        public event Action<SearchContext> sessionStarted
        {
            add
            {
                lock (this)
                    sessions.sessionStarted += value;
            }
            remove
            {
                lock (this)
                    sessions.sessionStarted -= value;
            }
        }

        /// <summary>
        /// Invoked when a Search has ended.
        /// </summary>
        public event Action<SearchContext> sessionEnded
        {
            add
            {
                lock (this)
                    sessions.sessionEnded += value;
            }
            remove
            {
                lock (this)
                    sessions.sessionEnded -= value;
            }
        }

        internal void SetFilteredProviders(IEnumerable<string> providerIds)
        {
            ResetFilter(false);
            foreach (var id in providerIds)
                SetFilter(id, true);
        }

        private void BeginSession()
        {

            foreach (var desc in m_ProviderDescs)
            {
                using (var enableTimer = new DebugTimer(null))
                {
                    desc.provider.OnEnable(enableTimer.timeMs);
                }
            }
        }

        private void EndSession()
        {
            sessions.StopAllAsyncSearchSessions();
            sessions.Clear();

            foreach (var desc in m_ProviderDescs)
                desc.provider.OnDisable();

        }

        /// <summary>
        /// Dispose of the SearchContext. Will End the Search session.
        /// </summary>
        /// <param name="disposing">Is the SearchItem currently being disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                EndSession();

                m_ProviderDescs = null;
                m_Disposed = true;
            }
        }

        /// <summary>
        /// Returns the time it took to evaluate the last query in milliseconds.
        /// </summary>
        internal double searchElapsedTime => (searchFinishTime - searchStartTime) * 1000.0;
        internal double searchStartTime { get; set; } = 0;
        internal double searchFinishTime { get; set; } = 0;

        /// <summary>
        /// Indicates that the search results should be filter for this type.
        /// </summary>
        internal Type filterType { get; set; }

        /// <summary>
        /// An instance of MultiProviderAsyncSearchSession holding all the async search sessions associated with this search context.
        /// </summary>
        internal MultiProviderAsyncSearchSession sessions { get; } = new MultiProviderAsyncSearchSession();

        /// <summary>
        /// Get the SearchContext unique hashcode.
        /// </summary>
        /// <returns>Returns the SearchContext unique hashcode.</returns>
        public override int GetHashCode()
        {
            return filters.Select(d => d.provider.id.GetHashCode()).Aggregate((int)options, (h1, h2) => (h1 ^ h2).GetHashCode());
        }

        /// <summary>
        /// Define a subset of items that can be searched.
        /// </summary>
        internal List<SearchItem> subset { get; set; }

        /// <summary>
        /// Add a new query error on this context.
        /// </summary>
        /// <param name="error">The new error.</param>
        public void AddSearchQueryError(SearchQueryError error)
        {
            lock (this)
            {
                m_QueryErrors.Add(error);
            }
        }

        /// <summary>
        /// Add new query errors on this context.
        /// </summary>
        /// <param name="errors">The new errors.</param>
        public void AddSearchQueryErrors(IEnumerable<SearchQueryError> errors)
        {
            lock (this)
            {
                m_QueryErrors.AddRange(errors);
            }
        }

        internal void ClearErrors()
        {
            lock (this)
            {
                m_QueryErrors.Clear();
            }
        }

        internal bool HasError(SearchQueryErrorType errorType)
        {
            lock (this)
            {
                return m_QueryErrors.Exists(error => error.type == errorType);
            }
        }

        internal IEnumerable<SearchQueryError> GetErrors(SearchQueryErrorType errorType)
        {
            lock (this)
            {
                // Return a new list since the list can be modified asynchronously
                return m_QueryErrors.Where(error => error.type == errorType).ToList();
            }
        }

        internal IEnumerable<SearchQueryError> GetAllErrors()
        {
            lock (this)
            {
                // Return a new list since the list can be modified asynchronously
                return m_QueryErrors.ToArray();
            }
        }

        internal IEnumerable<SearchQueryError> GetErrorsByProvider(string providerId)
        {
            lock (this)
            {
                // Return a new list since the list can be modified asynchronously
                return m_QueryErrors.Where(error => error.provider.id == providerId).ToList();
            }
        }
    }
}
