// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.SearchService;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    enum SearchPickerType
    {
        None,
        AdvancedSearchPicker,
        ObjectField,
        SearchContextAttribute
    }

    class RuntimeSearchContext
    {
        public SearchPickerType pickerType;
        public ISearchContext searchEngineContext;

        public ProjectSearchContext projectSearchContext => searchEngineContext.engineScope == SearchEngineScope.Project ? (ProjectSearchContext)searchEngineContext : null;
        public SceneSearchContext sceneSearchContext => searchEngineContext.engineScope == SearchEngineScope.Scene ? (SceneSearchContext)searchEngineContext : null;
        public ObjectSelectorSearchContext selectorSearchContext => searchEngineContext.engineScope == SearchEngineScope.ObjectSelector ? (ObjectSelectorSearchContext)searchEngineContext : null;

        Object m_CurrentObject;
        public Object currentObject
        {
            get
            {
                if (m_CurrentObject != null)
                    return m_CurrentObject;

                if (searchEngineContext?.engineScope == SearchEngineScope.ObjectSelector)
                    return selectorSearchContext?.currentObject;

                return null;
            }
            set => m_CurrentObject = value;
        }

        Object[] m_EditedObjects;
        public Object[] editedObjects
        {
            get
            {
                if (m_EditedObjects != null)
                    return m_EditedObjects;

                if (searchEngineContext?.engineScope == SearchEngineScope.ObjectSelector)
                    return selectorSearchContext?.editedObjects;

                return null;
            }
            set => m_EditedObjects = value;
        }

        IEnumerable<Type> m_RequiredTypes;
        public IEnumerable<Type> requiredTypes
        {
            get
            {
                if (m_RequiredTypes == null)
                    return searchEngineContext?.requiredTypes;
                return m_RequiredTypes;
            }
            set => m_RequiredTypes = value;
        }

        IEnumerable<string> m_RequiredTypeNames;
        public IEnumerable<string> requiredTypeNames
        {
            get
            {
                if (m_RequiredTypeNames == null)
                    return searchEngineContext?.requiredTypeNames;
                return m_RequiredTypeNames;
            }
            set => m_RequiredTypeNames = value;
        }
        public IEnumerable<int> allowedInstanceIds;

        SearchFilter m_SearchFilter;
        public SearchFilter searchFilter
        {
            get
            {
                if (m_SearchFilter != null)
                    return m_SearchFilter;

                if (searchEngineContext?.engineScope == SearchEngineScope.Project)
                    return projectSearchContext?.searchFilter;
                if (searchEngineContext?.engineScope == SearchEngineScope.Scene)
                    return sceneSearchContext?.searchFilter;
                if (searchEngineContext?.engineScope == SearchEngineScope.ObjectSelector)
                    return selectorSearchContext?.searchFilter;

                return null;
            }
            set => m_SearchFilter = value;
        }
    }

    /// <summary>
    /// The search context encapsulate all the states necessary to perform a query. It allows the full
    /// customization of how a query would be performed.
    /// </summary>
    [DebuggerDisplay("{m_SearchText}")]
    public class SearchContext : IDisposable
    {
        private static volatile int s_NextSessionId = 0;
        private static readonly string[] k_Empty = new string[0];

        internal int sessionId;
        private string m_SearchText = "";
        private string m_CachedPhrase;
        private bool m_Disposed = false;
        private readonly List<SearchProvider> m_Providers;
        private readonly List<SearchQueryError> m_QueryErrors = new List<SearchQueryError>();

        internal RuntimeSearchContext runtimeContext { get; set; }

        /// <summary>
        /// This special constructor is used to create dummy context for default providers.
        /// It is normal that no session is initialized.
        /// </summary>
        /// <see cref="SearchProvider.defaultContext"/>
        /// <param name="provider">Default context provider</param>
        internal SearchContext(SearchProvider provider)
            : this(provider, null)
        {}

        internal SearchContext(SearchProvider provider, RuntimeSearchContext runtimeContext)
        {
            m_Providers = new List<SearchProvider>() { provider };
            searchText = string.Empty;
            options = SearchFlags.Default;
            this.runtimeContext = runtimeContext;
        }

        /// <summary>
        /// Create a new search context.
        /// </summary>
        /// <param name="providers">The list of providers used to resolve the specified query.</param>
        /// <param name="searchText">The search query to perform.</param>
        /// <param name="options">Options to further controlled the query.</param>
        public SearchContext(IEnumerable<SearchProvider> providers, string searchText, SearchFlags options)
            : this(providers, searchText, options, null)
        {}

        internal SearchContext(IEnumerable<SearchProvider> providers, string searchText, SearchFlags options, RuntimeSearchContext runtimeContext)
        {
            m_Providers = FilterProviders(providers);
            this.options = options;
            this.searchText = searchText ?? string.Empty;
            this.runtimeContext = runtimeContext;
            BeginSession();
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

        internal SearchContext(IEnumerable<SearchProvider> providers, string searchText, RuntimeSearchContext runtimeContext)
            : this(providers, searchText, SearchFlags.Default, runtimeContext)
        {}

        /// <summary>
        /// Create a new search context.
        /// </summary>
        /// <param name="providers">The list of providers used to resolve the specified query.</param>
        public SearchContext(IEnumerable<SearchProvider> providers)
            : this(providers, string.Empty, SearchFlags.Default)
        {
        }

        internal SearchContext(IEnumerable<SearchProvider> providers, RuntimeSearchContext runtimeContext)
            : this(providers, string.Empty, SearchFlags.Default, runtimeContext)
        {}

        public SearchContext(SearchContext context)
            : this(context.providers, context.searchText, context.options, context.runtimeContext)
        {}

        /// <summary>
        /// Search context finalizer.
        /// </summary>
        ~SearchContext()
        {
            Dispose(false);
        }

        [Obsolete("ResetFilter has been deprecated and there is no replacement.", error: true)]
        public void ResetFilter(bool enableAll)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Enable or disable a single provider.
        /// A disabled provider won't be ask to provider items to resolve the query.
        /// </summary>
        /// <param name="providerId">Id of the provider. See SearchProvider.<see cref="SearchProvider.name"/>.id.</param>
        /// <param name="isEnabled">If true, enable the provider to perform query.</param>
        public void SetFilter(string providerId, bool isEnabled)
        {
            var provider = m_Providers.FirstOrDefault(p => p.id == providerId);
            if (!isEnabled && provider != null)
            {
                SetProviders(m_Providers.Where(p => p != provider).ToList());
            }
            else if (isEnabled && provider == null)
            {
                provider = SearchService.GetProvider(providerId);
                if (provider != null)
                    SetProviders(m_Providers.Concat(new[] { provider }).ToList());
            }
        }

        /// <summary>
        /// Checks if a provider is available to process a query.
        /// </summary>
        /// <param name="providerId">If of the provider. See SearchProvider.<see cref="SearchProvider.name"/>.id.</param>
        /// <returns></returns>
        public bool IsEnabled(string providerId)
        {
            return m_Providers.Any(p => p.id == providerId);
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
        public string searchQuery { get; private set; } = string.Empty;

        /// <summary>
        /// Original search query with all markers
        /// </summary>
        internal string rawSearchQuery { get; private set; } = string.Empty;

        /// <summary>
        /// Keep a trace of all parsed query markers in the searchText
        /// </summary>
        internal QueryMarker[] markers;

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
                return m_CachedPhrase ?? string.Empty;
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
                return (options & SearchFlags.WantsMore) == SearchFlags.WantsMore;
            }

            set
            {
                if (value)
                    options |= SearchFlags.WantsMore;
                else
                    options &= ~SearchFlags.WantsMore;
            }
        }

        internal bool showPackages
        {
            get => (options & SearchFlags.Packages) == SearchFlags.Packages;

            set
            {
                if (value)
                    options |= SearchFlags.Packages;
                else
                    options &= ~SearchFlags.Packages;
            }
        }

        internal bool noIndexing
        {
            get => (options & SearchFlags.NoIndexing) == SearchFlags.NoIndexing;

            set
            {
                if (value)
                    options |= SearchFlags.NoIndexing;
                else
                    options &= ~SearchFlags.NoIndexing;
            }
        }

        internal bool debug
        {
            get => (options & SearchFlags.Debug) == SearchFlags.Debug;

            set
            {
                if (value)
                    options |= SearchFlags.Debug;
                else
                    options &= ~SearchFlags.Debug;
            }
        }

        public bool empty => string.IsNullOrEmpty(m_SearchText);

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
                SetSearchText(value);
            }
        }

        private void SetSearchText(string value)
        {
            m_SearchText = value ?? string.Empty;

            // Reset a few values
            filterId = null;
            textFilters = searchWords = k_Empty;

            searchQueryOffset = 0;
            rawSearchQuery = SearchUtils.ParseSearchText(searchText, m_Providers, out var filteredProvider);
            searchQuery = QueryMarker.ReplaceMarkersWithRawValues(rawSearchQuery, out markers);
            if (filteredProvider != null)
                filterId = filteredProvider.filterId;

            if (string.IsNullOrEmpty(searchQuery))
                return;

            searchQueryOffset = searchText.Length - rawSearchQuery.Length;
            searchQuery = searchQuery.TrimEnd();
            var tokens = Utils.Simplify(searchQuery).ToLowerInvariant().Split(' ').ToArray();
            searchWords = tokens.Where(t => t.IndexOf(':') == -1).ToArray();
            textFilters = tokens.Where(t => t.IndexOf(':') != -1).ToArray();
        }

        /// <summary>
        /// Which Providers are active for this particular context.
        /// </summary>
        public IEnumerable<SearchProvider> providers
        {
            get
            {
                if (filterId != null)
                    return m_Providers.Where(p => p.filterId == filterId);

                if (m_Providers.Count == 1)
                    return m_Providers;

                return m_Providers.Where(p => !p.isExplicitProvider);
            }
        }

        internal IList<SearchProvider> GetProviders()
        {
            return m_Providers;
        }

        internal void AddProvider(SearchProvider provider)
        {
            UpdateProviders(() => m_Providers.Add(provider));
        }

        internal void RemoveProvider(SearchProvider provider)
        {
            UpdateProviders(() => m_Providers.Remove(provider));
        }

        internal void SetProviders(IEnumerable<SearchProvider> providers = null)
        {
            UpdateProviders(() =>
            {
                m_Providers.Clear();
                if (providers != null)
                    m_Providers.AddRange(FilterProviders(providers));
            });
        }

        private static List<SearchProvider> FilterProviders(IEnumerable<SearchProvider> providers)
        {
            return providers.OrderBy(p => p.priority).Distinct().ToList();
        }

        private void UpdateProviders(Action updateOperation)
        {
            if (updateOperation == null)
                return;

            if (m_Providers.Count > 0)
                EndSession();
            updateOperation();
            BeginSession();
            SetSearchText(m_SearchText);
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

        private void BeginSession()
        {
            sessionId = System.Threading.Interlocked.Increment(ref s_NextSessionId);

            if (options.HasAny(SearchFlags.Debug))
                UnityEngine.Debug.Log($"[{sessionId}] Start search session {String.Join(", ", providers.Select(p=>p.id))} -> {searchText}");

            foreach (var p in m_Providers)
            {
                using (var enableTimer = new DebugTimer(null))
                {
                    p.OnEnable(enableTimer.timeMs);
                }
            }
        }

        private void EndSession()
        {
            sessions.StopAllAsyncSearchSessions();
            sessions.Clear();

            foreach (var p in m_Providers)
                p.OnDisable();

            if (options.HasAny(SearchFlags.Debug))
                UnityEngine.Debug.Log($"[{sessionId}] End search session {string.Join(", ", providers.Select(p => p.id))}");
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
                m_Disposed = true;
            }
        }

        /// <summary>
        /// Returns the time it took to evaluate the last query in milliseconds.
        /// </summary>
        internal double searchElapsedTime => TimeSpan.FromTicks(searchFinishTime - searchStartTime).TotalMilliseconds;
        internal long searchStartTime { get; set; } = 0;
        internal long searchFinishTime { get; set; } = 0;

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
            var validContextHashOptiopns = options & ~SearchFlags.OpenGlobal;
            return m_Providers.Select(p => p.id.GetHashCode()).Aggregate((int)validContextHashOptiopns, (h1, h2) => (h1 ^ h2).GetHashCode());
        }

        /// <summary>
        /// Define a subset of items that can be searched.
        /// </summary>
        internal List<SearchItem> subset { get; set; }

        /// <summary>
        /// The user data field can be used to store private data when running a custom query.
        /// </summary>
        internal object userData { get; set; }

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

        public override string ToString()
        {
            return $"[{GetProviders().Count}, {options}] {searchText.Replace("\n", "")}";
        }

        internal bool Tick()
        {
            if (!options.HasAny(SearchFlags.Synchronous))
                return true;
            sessions.Tick();
            return Dispatcher.ProcessOne();
        }
    }
}
