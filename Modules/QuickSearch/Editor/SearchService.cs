// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Attribute used to declare a static method that will create a new search provider at load time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SearchItemProviderAttribute : Attribute
    {
        [RequiredSignature] internal static SearchProvider CreateProvider() { return null; }
    }

    /// <summary>
    /// Attribute used to declare a static method that define new actions for specific search providers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SearchActionsProviderAttribute : Attribute
    {
        [RequiredSignature] internal static IEnumerable<SearchAction> CreateActionHandlers() { return null; }
    }

    /// <summary>
    /// Principal Quick Search API to initiate searches and fetch results.
    /// </summary>
    public static class SearchService
    {
        private const int k_MaxFetchTimeMs = 50;

        /// <summary>
        /// Returns the list of all providers (active or not)
        /// </summary>
        public static List<SearchProvider> Providers { get; private set; }

        /// <summary>
        /// Returns the list of providers sorted by priority.
        /// </summary>
        public static IEnumerable<SearchProvider> OrderedProviders
        {
            get
            {
                return Providers.OrderBy(p => p.priority + (p.isExplicitProvider ? 100000 : 0));
            }
        }

        static SearchService()
        {
            Refresh();
            SetupSearchFirstUse();
        }

        private static void SetupSearchFirstUse()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SearchSettings.onBoardingDoNotAskAgain || Utils.IsRunningTests())
                return;

            if (!Utils.IsMainProcess())
                return;

            EditorApplication.delayCall += () =>
            {
                if (SearchDatabase.EnumeratePaths(SearchDatabase.IndexLocation.assets).Count() == 0)
                    SearchDatabase.CreateDefaultIndex();

                SearchSettings.onBoardingDoNotAskAgain = true;
                SearchSettings.Save();
            };
        }

        /// <summary>
        /// Returns the data of a search provider given its ID.
        /// </summary>
        /// <param name="providerId">Unique ID of the provider</param>
        /// <returns>The matching provider</returns>
        public static SearchProvider GetProvider(string providerId)
        {
            return Providers.Find(p => p.id == providerId);
        }

        /// <summary>
        /// Returns the search action data for a given provider and search action id.
        /// </summary>
        /// <param name="provider">Provider to lookup</param>
        /// <param name="actionId">Unique action ID within the provider.</param>
        /// <returns>The matching action</returns>
        public static SearchAction GetAction(SearchProvider provider, string actionId)
        {
            if (provider == null)
                return null;
            return provider.actions.Find(a => a.id == actionId);
        }

        /// <summary>
        /// Activate or deactivate a search provider.
        /// Call Refresh after this to take effect on the next search.
        /// </summary>
        /// <param name="providerId">Provider id to activate or deactivate</param>
        /// <param name="active">Activation state</param>
        public static void SetActive(string providerId, bool active = true)
        {
            var provider = Providers.FirstOrDefault(p => p.id == providerId);
            if (provider == null)
                return;
            SearchSettings.GetProviderSettings(providerId).active = active;
            provider.active = active;
        }

        /// <summary>
        /// Clears everything and reloads all search providers.
        /// </summary>
        /// <remarks>Use with care. Useful for unit tests.</remarks>
        public static void Refresh()
        {
            RefreshProviders();
            RefreshProviderActions();
        }

        /// <summary>
        /// Refreshes all open windows.
        /// </summary>
        public static void RefreshWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<QuickSearch>();
            if (windows == null)
                return;
            foreach (var win in windows)
            {
                win.Refresh();
            }
        }

        /// <summary>
        /// Create context from a list of provider id.
        /// </summary>
        /// <param name="providerIds">List of provider id</param>
        /// <param name="searchText">seach Query</param>
        /// <param name="flags">Options defining how the query will be performed</param>
        /// <returns>New SearchContext</returns>
        public static SearchContext CreateContext(IEnumerable<string> providerIds, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(providerIds.Select(id => GetProvider(id)).Where(p => p != null), searchText, flags);
        }

        /// <summary>
        /// Create context from a list of providers.
        /// </summary>
        /// <param name="providers">List of providers</param>
        /// <param name="searchText">seach Query</param>
        /// <param name="flags">Options defining how the query will be performed</param>
        /// <returns>New SearchContext</returns>
        public static SearchContext CreateContext(IEnumerable<SearchProvider> providers, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(providers, searchText, flags);
        }

        /// <summary>
        /// Create a search context with a single search provider.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public static SearchContext CreateContext(SearchProvider provider, string searchText = "")
        {
            return CreateContext(new[] { provider }, searchText);
        }

        /// <summary>
        /// Create a search context for a single search provider.
        /// </summary>
        /// <param name="providerId">Search provider ID string (such as asset, scene, find, etc.)</param>
        /// <param name="searchText">Initial search text to be used to evaluate the query.</param>
        /// <param name="flags">Additional search options to be used for the query evaluation.</param>
        /// <returns>The newly created search context. You need to call Dispose on the SearchContext when you are done using it for queries.</returns>
        public static SearchContext CreateContext(string providerId, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return CreateContext(new[] { providerId }, searchText, flags);
        }

        /// <summary>
        /// Create a search context with all active providers.
        /// </summary>
        /// <returns></returns>
        public static SearchContext CreateContext(string searchText, SearchFlags flags)
        {
            return CreateContext(Providers.Where(p => p.active).ToList(), searchText, flags);
        }

        public static SearchContext CreateContext(string searchText)
        {
            return CreateContext(searchText, SearchFlags.Default);
        }

        /// <summary>
        /// Initiate a search and return all search items matching the search context. Other items can be found later using the asynchronous searches.
        /// </summary>
        /// <param name="context">The current search context</param>
        /// <param name="options">Options defining how the query will be performed</param>
        /// <returns>A list of search items matching the search query.</returns>
        public static List<SearchItem> GetItems(SearchContext context, SearchFlags options = SearchFlags.Default)
        {
            // Stop all search sessions every time there is a new search.
            context.sessions.StopAllAsyncSearchSessions();
            context.searchFinishTime = context.searchStartTime = EditorApplication.timeSinceStartup;
            context.sessionEnded -= OnSearchEnded;
            context.sessionEnded += OnSearchEnded;

            if (options.HasFlag(SearchFlags.WantsMore))
                context.wantsMore = true;

            if (options.HasFlag(SearchFlags.Synchronous))
                context.options |= SearchFlags.Synchronous;

            int fetchProviderCount = 0;
            var allItems = new List<SearchItem>(3);
            foreach (var provider in context.providers)
            {
                try
                {
                    var watch = new System.Diagnostics.Stopwatch();
                    watch.Start();
                    fetchProviderCount++;
                    var iterator = provider.fetchItems(context, allItems, provider);
                    if (iterator != null && options.HasFlag(SearchFlags.Synchronous))
                    {
                        using (var stackedEnumerator = new SearchEnumerator<SearchItem>(iterator))
                        {
                            while (stackedEnumerator.MoveNext())
                            {
                                if (stackedEnumerator.Current != null)
                                    allItems.Add(stackedEnumerator.Current);
                            }
                        }
                    }
                    else
                    {
                        var session = context.sessions.GetProviderSession(context, provider.id);
                        session.Reset(context, iterator, k_MaxFetchTimeMs);
                        session.Start();
                        var sessionEnded = !session.FetchSome(allItems, k_MaxFetchTimeMs);
                        if (options.HasFlag(SearchFlags.FirstBatchAsync))
                            session.SendItems(context.subset != null ? allItems.Intersect(context.subset) : allItems);
                        if (sessionEnded)
                            session.Stop();
                    }
                    provider.RecordFetchTime(watch.Elapsed.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception($"Failed to get fetch {provider.name} provider items.", ex));
                }
            }

            if (fetchProviderCount == 0)
            {
                OnSearchEnded(context);
                context.sessions.StopAllAsyncSearchSessions();
            }

            if (context.subset != null)
                allItems = new List<SearchItem>(allItems.Intersect(context.subset));

            if (!options.HasFlag(SearchFlags.Sorted))
                return allItems;

            allItems.Sort(SortItemComparer);
            return allItems.GroupBy(i => i.id).Select(i => i.First()).ToList();
        }

        /// <summary>
        /// Execute a search request that will fetch search results asynchronously.
        /// </summary>
        /// <param name="context">Search context used to track asynchronous request.</param>
        /// <param name="options">Options defining how the query will be performed</param>
        /// <returns>Asynchronous list of search items.</returns>
        public static ISearchList Request(SearchContext context, SearchFlags options = SearchFlags.None)
        {
            if (options.HasFlag(SearchFlags.Synchronous))
            {
                throw new NotSupportedException($"Use {nameof(SearchService)}.{nameof(GetItems)}(context, " +
                    $"{nameof(SearchFlags)}.{nameof(SearchFlags.Synchronous)}) to fetch items synchronously.");
            }

            ISearchList results = null;
            if (options.HasFlag(SearchFlags.Sorted))
                results = new SortedSearchList(context);
            else
                results = new AsyncSearchList(context);

            results.AddItems(GetItems(context, options));
            return results;
        }

        /// <summary>
        /// Run a query on all active providers.
        /// </summary>
        /// <param name="searchText">Search query to execute.</param>
        /// <returns></returns>
        public static ISearchList Request(string searchText, SearchFlags options = SearchFlags.None)
        {
            var activeProviders = Providers.Where(p => p.active).ToList();
            var context = CreateContext(activeProviders, searchText, options);
            return Request(context, options);
        }

        /// <summary>
        /// Execute a search request and callback when the search is completed.
        /// This will create a new search context that will be Disposed when the request is finished.
        /// </summary>
        public static void Request(string searchText, Action<SearchContext, IList<SearchItem>> onSearchCompleted, SearchFlags options = SearchFlags.None)
        {
            var context = CreateContext(searchText, options);
            Request(context, (c, items) =>
            {
                onSearchCompleted?.Invoke(c, items);
                c.Dispose();
            }, options);
        }

        /// <summary>
        /// Execute a search request and callback for every incoming items and when the search is completed.
        /// This will create a new search context that will be Disposed when the request is finished.
        /// </summary>
        public static void Request(string searchText,
            Action<SearchContext, IEnumerable<SearchItem>> onIncomingItems,
            Action<SearchContext> onSearchCompleted,
            SearchFlags options = SearchFlags.None)
        {
            var context = CreateContext(searchText, options);
            Request(context, onIncomingItems, (c) =>
            {
                onSearchCompleted?.Invoke(c);
                c.Dispose();
            }, options);
        }

        /// <summary>
        /// Execute a search request and callback when the search is completed.
        /// The user is responsible for disposing of the search context.
        /// </summary>
        public static void Request(SearchContext context, Action<SearchContext, IList<SearchItem>> onSearchCompleted, SearchFlags options = SearchFlags.None)
        {
            var results = new List<SearchItem>();
            Request(context,
                (c, items) => results.AddRange(items),
                (c) => onSearchCompleted?.Invoke(c, results),
                options);
        }

        // <summary>
        /// Execute a search request and callback for every incoming items and when the search is completed.
        /// The user is responsible for disposing of the search context.
        /// </summary>
        public static void Request(SearchContext context,
            Action<SearchContext, IEnumerable<SearchItem>> onIncomingItems,
            Action<SearchContext> onSearchCompleted,
            SearchFlags options = SearchFlags.None)
        {
            var sessionCount = 0;
            context.asyncItemReceived += (c, items) => onIncomingItems?.Invoke(c, items.Where(e => e != null));
            context.sessionStarted += c => ++ sessionCount;
            context.sessionEnded += c =>
            {
                --sessionCount;
                if (sessionCount == 0)
                    onSearchCompleted?.Invoke(c);
            };
            GetItems(context, options | SearchFlags.FirstBatchAsync);
        }

        private static void OnSearchEnded(SearchContext context)
        {
            context.searchFinishTime = EditorApplication.timeSinceStartup;
        }

        private static int SortItemComparer(SearchItem item1, SearchItem item2)
        {
            var po = item1.provider.priority.CompareTo(item2.provider.priority);
            if (po != 0)
                return po;
            po = item1.score.CompareTo(item2.score);
            if (po != 0)
                return po;
            return string.Compare(item1.id, item2.id, StringComparison.Ordinal);
        }

        private static void RefreshProviders()
        {
            Providers = TypeCache.GetMethodsWithAttribute<SearchItemProviderAttribute>()
                .Select(LoadProvider)
                .Where(provider => provider != null)
                .ToList();
        }

        private static SearchProvider LoadProvider(System.Reflection.MethodInfo methodInfo)
        {
            try
            {
                SearchProvider fetchedProvider = null;
                using (var fetchLoadTimer = new DebugTimer(null))
                {
                    fetchedProvider = methodInfo.Invoke(null, null) as SearchProvider;
                    if (fetchedProvider == null)
                        return null;

                    fetchedProvider.loadTime = fetchLoadTimer.timeMs;

                    // Load per provider user settings
                    if (SearchSettings.TryGetProviderSettings(fetchedProvider.id, out var providerSettings))
                    {
                        fetchedProvider.active = providerSettings.active;
                        fetchedProvider.priority = providerSettings.priority;
                    }
                }
                return fetchedProvider;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return null;
            }
        }

        private static void RefreshProviderActions()
        {
            foreach (var action in TypeCache.GetMethodsWithAttribute<SearchActionsProviderAttribute>()
                     .SelectMany(methodInfo => methodInfo.Invoke(null, null) as IEnumerable<object>)
                     .Where(a => a != null).Cast<SearchAction>())
            {
                var provider = Providers.Find(p => p.id == action.providerId);
                if (provider == null)
                    continue;
                provider.actions.Add(action);
            }
            SearchSettings.SortActionsPriority();
        }

        /// <summary>
        /// Creates and open a new instance of Quick Search
        /// </summary>
        /// <param name="context">Initial search context of QuickSearch</param>
        /// <param name="topic">QuickSearch search topic</param>
        /// <param name="defaultWidth">Initial width of the window.</param>
        /// <param name="defaultHeight">Initial height of the window.</param>
        /// <param name="multiselect">True if the search support multi-selection or not.</param>
        /// <param name="dockable">If true, creates a dockable QuickSearch Window (that will be closed when an item is activated). If false, it will create a DropDown (borderless, undockable and unmovable) version of QuickSearch.</param>
        /// <param name="saveFilters">True if user provider filters should be saved for next search session</param>
        /// <param name="reuseExisting">If true, try to reuse an already existing instance of QuickSearch. If false will create a new QuickSearch window.</param>
        /// <returns>Returns the Quick Search editor window instance.</returns>
        public static ISearchView ShowWindow(SearchContext context = null, string topic = "Unity", float defaultWidth = 850, float defaultHeight = 539,
            bool saveFilters = true, bool reuseExisting = false, bool multiselect = true, bool dockable = true)
        {
            var flags = SearchFlags.None;
            if (saveFilters) flags |= SearchFlags.SaveFilters;
            if (reuseExisting) flags |= SearchFlags.ReuseExistingWindow;
            if (multiselect) flags |= SearchFlags.Multiselect;
            if (dockable) flags |= SearchFlags.Dockable;
            var view = QuickSearch.Create(context, topic, flags).ShowWindow(defaultWidth, defaultHeight, flags);
            if (context != null && !string.IsNullOrEmpty(context.searchText))
                view.Refresh();
            return view;
        }

        /// <summary>
        /// Open QuickSearch in contextual mode enabling only the providers specified.
        /// </summary>
        /// <param name="providerIds">List of provider ids to enabled for QuickSearch</param>
        /// <returns>Returns the QuickSearch window.</returns>
        public static ISearchView ShowContextual(params string[] providerIds)
        {
            return QuickSearch.OpenWithContextualProvider(null, providerIds, SearchFlags.OpenContextual);
        }

        /// <summary>
        /// Use Quick Search to as an object picker to select any object based on the specified filter type.
        /// </summary>
        /// <param name="selectHandler">Callback to trigger when a user selects an item.</param>
        /// <param name="trackingHandler">Callback to trigger when the user is modifying QuickSearch selection (i.e. tracking the currently selected item)</param>
        /// <param name="searchText">Initial search text for QuickSearch.</param>
        /// <param name="typeName">Type name of the object to select. Can be used to replace filterType.</param>
        /// <param name="filterType">Type of the object to select.</param>
        /// <param name="defaultWidth">Initial width of the window.</param>
        /// <param name="defaultHeight">Initial height of the window.</param>
        /// <param name="flags">Options flags modifying how the Search window will be opened.</param>
        /// <returns>Returns the QuickSearch window.</returns>
        public static ISearchView ShowObjectPicker(
            Action<UnityEngine.Object, bool> selectHandler,
            Action<UnityEngine.Object> trackingHandler,
            string searchText, string typeName, Type filterType,
            float defaultWidth = 850, float defaultHeight = 539, SearchFlags flags = SearchFlags.OpenPicker)
        {
            return QuickSearch.ShowObjectPicker(selectHandler, trackingHandler, searchText, typeName, filterType, defaultWidth, defaultHeight, flags);
        }
    }
}
