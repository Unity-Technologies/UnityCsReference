// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Search;

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

        internal static void SetupSearchFirstUse()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SearchSettings.onBoardingDoNotAskAgain || Utils.IsRunningTests())
                return;

            if (!Utils.IsMainProcess())
                return;

            if (!SearchDatabase.Enumerate(SearchDatabase.IndexLocation.assets).Any())
                SearchDatabase.CreateDefaultIndex();

            SearchSettings.onBoardingDoNotAskAgain = true;
            SearchSettings.Save();
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

        internal static SearchProvider GetProvider(Type providerType)
        {
            if (providerType.BaseType != typeof(SearchProvider))
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"Trying to instantiate a type that is not a Search Provider: \"{providerType.FullName}\".");
                return null;
            }

            return Activator.CreateInstance(providerType) as SearchProvider;
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
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

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
            return new SearchContext(GetProviders(providerIds), searchText, flags);
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
            return CreateContext(GetActiveProviders(), searchText, flags);
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
            context.searchFinishTime = context.searchStartTime = DateTime.Now.Ticks;
            context.sessionEnded -= OnSearchEnded;
            context.sessionEnded += OnSearchEnded;

            if (options.HasAny(SearchFlags.WantsMore))
                context.wantsMore = true;

            if (options.HasAny(SearchFlags.Synchronous))
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
                    if (iterator != null && options.HasAny(SearchFlags.Synchronous))
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
                        if (options.HasAny(SearchFlags.FirstBatchAsync))
                        {
                            session.SendItems(context.subset != null ? allItems.Intersect(context.subset) : allItems);
                            allItems.Clear();
                        }
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

            if (!options.HasAny(SearchFlags.Sorted))
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
            ISearchList results = null;
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
            {
                results = new ConcurrentSearchList(context);

                Dispatcher.Enqueue(() =>
                {
                    results.AddItems(GetItems(context, options));
                    (results as ConcurrentSearchList)?.GetItemsDone();
                });

                return results;
            }

            if (options.HasAny(SearchFlags.Sorted))
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
            var activeProviders = GetActiveProviders();
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

        /// <summary>
        /// Execute a search request and callback for every incoming items and when the search is completed.
        /// The user is responsible for disposing of the search context.
        /// </summary>
        public static void Request(SearchContext context,
            Action<SearchContext, IEnumerable<SearchItem>> onIncomingItems,
            Action<SearchContext> onSearchCompleted,
            SearchFlags options = SearchFlags.None)
        {
            var requestId = Guid.NewGuid().ToString("N");
            if (options.HasAny(SearchFlags.Debug))
                Debug.Log($"{requestId} Request started {context.searchText} ({options | context.options})");
            var sessionCount = 0;
            var firstBatchResolved = false;
            var completed = false;
            var batchCount = 1;

            void ReceiveItems(SearchContext c, IEnumerable<SearchItem> items)
            {
                if (options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} #{batchCount++} Request incoming batch {context.searchText}");
                onIncomingItems?.Invoke(c, items.Where(e => e != null));
            }

            void OnSessionStarted(SearchContext c)
            {
                if (options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request session begin {context.searchText}");
                ++sessionCount;
            }

            void OnSessionEnded(SearchContext c)
            {
                if (options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request session ended {context.searchText}");
                --sessionCount;
                if (sessionCount == 0 && firstBatchResolved)
                {
                    if (options.HasAny(SearchFlags.Debug))
                        Debug.Log($"{requestId} Request async ended {context.searchText}");
                    context.asyncItemReceived -= ReceiveItems;
                    context.sessionStarted -= OnSessionStarted;
                    context.sessionEnded -= OnSessionEnded;
                    onSearchCompleted?.Invoke(c);
                    completed = true;
                }
            }

            context.asyncItemReceived += ReceiveItems;
            context.sessionStarted += OnSessionStarted;
            context.sessionEnded += OnSessionEnded;
            GetItems(context, options | SearchFlags.FirstBatchAsync);
            firstBatchResolved = true;
            if (sessionCount == 0 && !completed)
            {
                if (options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request sync ended {context.searchText}");
                context.asyncItemReceived -= ReceiveItems;
                context.sessionStarted -= OnSessionStarted;
                context.sessionEnded -= OnSessionEnded;
                onSearchCompleted?.Invoke(context);
            }
        }

        private static void OnSearchEnded(SearchContext context)
        {
            context.searchFinishTime = DateTime.Now.Ticks;
        }

        private static int SortItemComparer(SearchItem item1, SearchItem item2)
        {
            var po = item1.provider.priority.CompareTo(item2.provider.priority);
            if (po != 0)
                return po;
            po = item1.score.CompareTo(item2.score);
            if (po != 0)
                return po;
            return string.CompareOrdinal(item1.id, item2.id);
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
                Debug.LogWarning($"Cannot load Search Provider method: {methodInfo.Name} ({ex.Message})");
                return null;
            }
        }

        private static void RefreshProviderActions()
        {
            foreach (var action in TypeCache.GetMethodsWithAttribute<SearchActionsProviderAttribute>()
                     .Select(methodInfo => {
                         try
                         {
                             return methodInfo.Invoke(null, null) as IEnumerable<object>;
                         }
                         catch(Exception ex)
                         {
                             Debug.LogWarning($"Cannot load register Search Actions method: {methodInfo.Name} ({ex.Message})");
                             return null;
                         }
                    }).Where(actionArray => actionArray != null)
                     .SelectMany(actionArray => actionArray)
                     .Where(action => action != null).Cast<SearchAction>())
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
            return QuickSearch.Create<QuickSearch>(context, topic, flags).ShowWindow(defaultWidth, defaultHeight, flags);
        }

        /// <summary>
        /// Show a search window.
        /// </summary>
        /// <param name="viewState">Defines search view parameters for creation</param>
        /// <returns></returns>
        public static ISearchView ShowWindow(SearchViewState viewState)
        {
            return QuickSearch.Create(viewState).ShowWindow();
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
            float defaultWidth = 850, float defaultHeight = 539,
            SearchFlags flags = SearchFlags.None)
        {
            var context = CreateContext(GetObjectProviders(), searchText, flags | SearchFlags.OpenPicker);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, searchText, "object", "api");
            return ShowPicker(new SearchViewState(context, selectHandler, trackingHandler, typeName, filterType)
            {
                position = new Rect(0, 0, defaultWidth, defaultHeight)
            }.SetSearchViewFlags(SearchViewFlags.None));
        }

        public static ISearchView ShowPicker(
            SearchContext context,
            Action<SearchItem, bool> selectHandler,
            Action<SearchItem> trackingHandler = null,
            Func<SearchItem, bool> filterHandler = null,
            IEnumerable<SearchItem> subset = null,
            string title = null, float itemSize = 64f, float defaultWidth = 850f, float defaultHeight = 539f, SearchFlags flags = SearchFlags.None)
        {
            if (subset != null)
                context.subset = subset.ToList();
            context.options |= flags | SearchFlags.OpenPicker;
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, context.searchText, "item", "api");
            return SearchPickerWindow.ShowPicker(new SearchViewState(context, selectHandler)
            {
                trackingHandler = trackingHandler,
                filterHandler = filterHandler,
                title = title,
                itemSize = itemSize,
                position = new Rect(0, 0, defaultWidth, defaultHeight)
            }.SetSearchViewFlags(SearchViewFlags.None));
        }

        /// <summary>
        /// Open and show the Search Picker window.
        /// </summary>
        /// <param name="viewState">View parameters</param>
        /// <returns>Returns the newly create search view instance.</returns>
        public static ISearchView ShowPicker(SearchViewState viewState)
        {
            return SearchPickerWindow.ShowPicker(viewState);
        }

        internal static IEnumerable<SearchProvider> GetActiveProviders()
        {
            return Providers.Where(p => p.active);
        }

        internal static IEnumerable<SearchProvider> GetProviders(params string[] providerIds)
        {
            return providerIds.Select(GetProvider).Where(p => p != null);
        }

        internal static IEnumerable<SearchProvider> GetProviders(IEnumerable<string> providerIds)
        {
            return providerIds.Select(GetProvider).Where(p => p != null);
        }

        internal static IEnumerable<SearchProvider> GetObjectProviders()
        {
            yield return GetProvider(Search.Providers.BuiltInSceneObjectsProvider.type);
            yield return GetProvider(Search.Providers.AssetProvider.type);
            yield return GetProvider(Search.Providers.AdbProvider.type);
        }

        /// <summary>
        /// Create a new index and callback user code to indicate that the indexing is finished.
        /// </summary>
        /// <param name="name">Unique name of the index to be used.</param>
        /// <param name="onIndexReady">Callback invoked when the new search index is ready to be used.</param>
        public static void CreateIndex(
            in string name,
            in IndexingOptions options,
            IEnumerable<string> roots,
            IEnumerable<string> includes,
            IEnumerable<string> excludes,
            Action<string, string, Action> onIndexReady)
        {
            var indexName = name;
            var indexPath = name;
            if (name.EndsWith(".index"))
                indexName = name.Substring(0, name.Length - 6);
            else
                indexPath = $"{name}.index";

            if (options.HasNone(IndexingOptions.Temporary))
            {
                indexName = System.IO.Path.GetFileNameWithoutExtension(indexPath);
                if (!AssetDatabase.GetAssetFolderInfo(indexPath, out var rootFolder, out var immutable) || immutable)
                    indexPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/{indexName}.index");
            }
            else
            {
                indexPath = AssetDatabase.GenerateUniqueAssetPath($"Temp/{indexPath}");

                if (roots == null)
                    roots = new[] { "Assets" };
            }

            roots = roots ?? Enumerable.Empty<string>();
            includes = includes ?? Enumerable.Empty<string>();
            excludes = excludes ?? Enumerable.Empty<string>();

            var indexDir = System.IO.Path.GetDirectoryName(indexPath);
            if (!System.IO.Directory.Exists(indexDir))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(indexDir), System.IO.Path.GetFileName(indexDir));

            Utils.WriteTextFileToDisk(indexPath,
                $"{{\n\t" +
                $"\"roots\": [{string.Join(",", roots.Select(p => $"\"{p}\""))}],\n\t" +
                $"\"includes\": [{string.Join(",", includes.Select(p => $"\"{p}\""))}],\n\t" +
                $"\"excludes\": [{string.Join(",", excludes.Select(p => $"\"{p}\""))}],\n\t" +
                $"\"options\": {{\n\t\t" +
                $"\"types\": {options.HasAny(IndexingOptions.Types).ToString().ToLowerInvariant()},\n\t\t" +
                $"\"properties\": {options.HasAny(IndexingOptions.Properties).ToString().ToLowerInvariant()},\n\t\t" +
                $"\"extended\": {options.HasAny(IndexingOptions.Extended).ToString().ToLowerInvariant()},\n\t\t" +
                $"\"dependencies\": {options.HasAny(IndexingOptions.Dependencies).ToString().ToLowerInvariant()}\n\t}},\n\t" +
                $"\"baseScore\": 9999\n}}");

            var db = SearchDatabase.ImportAsset(indexPath);
            TrackCreateIndex(db, options, indexName, indexPath, onIndexReady, 1d);
        }

        /// <summary>
        /// Checks if a search index is ready to be used.
        /// </summary>
        /// <param name="name">Name or path of the search index to be checked. Pass null if you want to check all available indexes</param>
        /// <returns></returns>
        public static bool IsIndexReady(string name)
        {
            return SearchDatabase.EnumerateAll().Where(db =>
            {
                if (string.IsNullOrEmpty(name))
                    return true;
                if (string.Equals(db.name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(db.path, name, StringComparison.OrdinalIgnoreCase))
                    return true;
                return false;
            }).All(db => db.ready && !db.updating);
        }

        static void TrackCreateIndex(SearchDatabase db, IndexingOptions options, string indexName, string indexPath, Action<string, string, Action> onIndexReady, double delay)
        {
            if (db.ready)
            {
                onIndexReady?.Invoke(indexName, indexPath.Replace("\\", "/"), () =>
                {
                    if (EditorUtility.IsPersistent(db))
                        Resources.UnloadAsset(db);
                    if (options.HasNone(IndexingOptions.Keep))
                        AssetDatabase.DeleteAsset(indexPath);
                });
            }
            else
                Utils.CallDelayed(() => TrackCreateIndex(db, options, indexName, indexPath, onIndexReady, delay), delay);
        }
    }
}
