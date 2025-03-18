// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SearchService;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Search;

using System.Reflection;

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
        // TODO: This MaxFetchTime should be revisited. First, 50ms is very long for one frame.
        // Second, this is PER PROVIDER! Which means if you have 3 providers running, each can take up to 50ms
        // for a total of 150ms per frame!
        internal const int k_MaxFetchTimeMs = 50;
        private const int k_MaxSessionTimeMs = SearchSession.k_InfiniteSession;
        static SearchProvider s_SearchServiceProvider;

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
                return SearchUtils.SortProvider(Providers);
            }
        }

        internal static List<AdvancedObjectSelector> ObjectSelectors { get; private set; }

        internal static IEnumerable<AdvancedObjectSelector> OrderedObjectSelectors => ObjectSelectors.OrderBy(p => p.priority);

        static SearchService()
        {
            Initialize();
        }

        internal static void Initialize()
        {
            Refresh();
            RefreshObjectSelectors();
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

        internal static SearchProvider GetDefaultProvider()
        {
            return s_SearchServiceProvider;
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
            if (EditorApplication.isPlayingOrWillChangePlaymode && !SearchSettings.refreshSearchWindowsInPlayMode)
                return;

            ForceRefreshWindows();
        }

        internal static void ForceRefreshWindows()
        {
            var windows = Resources.FindObjectsOfTypeAll<SearchWindow>();
            if (windows == null)
                return;
            foreach (var win in windows)
            {
                if (!win.IsPicker())
                    win.Refresh();
            }
        }

        internal static void RefreshObjectSelectors()
        {
            var validators = ReflectionUtils.LoadAllMethodsWithAttribute<AdvancedObjectSelectorValidatorAttribute, AdvancedObjectSelectorValidator>(
                (loaded, mi, attribute, handler) =>
                    LoadAdvancedObjectSelectorAttribute<AdvancedObjectSelectorValidatorAttribute, AdvancedObjectSelectorValidator, AdvancedObjectSelectorValidatorHandler>(
                    loaded, mi, attribute, handler, "Advanced Object Selector Validator", (a, h) => GenerateAdvancedObjectSelectorValidatorWrapper(a, h)),
                MethodSignature.FromDelegate<AdvancedObjectSelectorValidatorHandler>(), ReflectionUtils.AttributeLoaderBehavior.DoNotThrowOnValidation).ToDictionary(validator => validator.id.GetHashCode());

            ObjectSelectors = ReflectionUtils.LoadAllMethodsWithAttribute<AdvancedObjectSelectorAttribute, AdvancedObjectSelector>(
                (loaded, mi, attribute, handler) =>
                    LoadAdvancedObjectSelectorAttribute<AdvancedObjectSelectorAttribute, AdvancedObjectSelector, AdvancedObjectSelectorHandler>(
                    loaded, mi, attribute, handler, "Advanced Object Selector", (a, h) => GenerateAdvancedObjectSelectorWrapper(validators, a, h)),
                MethodSignature.FromDelegate<AdvancedObjectSelectorHandler>(), ReflectionUtils.AttributeLoaderBehavior.DoNotThrowOnValidation).ToList();
        }

        static THandlerWrapper LoadAdvancedObjectSelectorAttribute<TAttribute, THandlerWrapper, TDelegate>(IReadOnlyCollection<THandlerWrapper> loaded, MethodInfo mi, TAttribute attribute, Delegate handler, string attributeName, Func<TAttribute, TDelegate, THandlerWrapper> wrapperGenerator)
        where TAttribute : Attribute, IAdvancedObjectSelectorAttribute
        where THandlerWrapper : IAdvancedObjectSelector
        {
            if (string.IsNullOrEmpty(attribute.id))
                throw new CustomAttributeFormatException($"Null or empty {attributeName} id for handler \"{ReflectionUtils.GetMethodFullName(mi)}\"");

            if (handler is TDelegate selectorHandler)
            {
                if (loaded.Any(p => p.id.Equals(attribute.id, StringComparison.Ordinal)))
                    throw new CustomAttributeFormatException($"{attributeName} id \"{attribute.id}\" for \"{ReflectionUtils.GetMethodFullName(mi)}\" is already used by another handler.");
                return wrapperGenerator(attribute, selectorHandler);
            }
            throw new CustomAttributeFormatException($"Invalid {attributeName} handler \"{attribute.id}\" using \"{ReflectionUtils.GetMethodFullName(mi)}\"");
        }

        static AdvancedObjectSelectorValidator GenerateAdvancedObjectSelectorValidatorWrapper(AdvancedObjectSelectorValidatorAttribute attribute, AdvancedObjectSelectorValidatorHandler handler)
        {
            return new AdvancedObjectSelectorValidator(attribute.id, handler);
        }

        static AdvancedObjectSelector GenerateAdvancedObjectSelectorWrapper(Dictionary<int, AdvancedObjectSelectorValidator> validators, AdvancedObjectSelectorAttribute attribute, AdvancedObjectSelectorHandler handler)
        {
            if (!validators.TryGetValue(attribute.id.GetHashCode(), out var validator))
                throw new CustomAttributeFormatException($"Advanced Object Selector id \"{attribute.id}\" does not have a matching validator.");

            var priority = attribute.defaultPriority;
            var active = attribute.defaultActive;
            var displayName = string.IsNullOrEmpty(attribute.displayName) ? SearchUtils.ToPascalWithSpaces(attribute.id) : attribute.displayName;
            if (SearchSettings.TryGetObjectSelectorSettings(attribute.id, out var settings))
            {
                priority = settings.priority;
                active = settings.active;
            }
            return new AdvancedObjectSelector(attribute.id, displayName, priority, active, handler, validator);
        }

        internal static AdvancedObjectSelector GetObjectSelector(string selectorId)
        {
            return ObjectSelectors.Find(p => p.id.Equals(selectorId, StringComparison.Ordinal));
        }

        /// <summary>
        /// Create context from a list of provider id.
        /// </summary>
        /// <param name="providerIds">List of provider id</param>
        /// <param name="searchText">Search Query</param>
        /// <param name="flags">Options defining how the query will be performed</param>
        /// <returns>New SearchContext</returns>
        public static SearchContext CreateContext(IEnumerable<string> providerIds, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(GetProviders(providerIds), searchText, flags);
        }

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, IEnumerable<string> providerIds, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(GetProviders(providerIds), searchText, flags, runtimeContext);
        }

        /// <summary>
        /// Create context from a list of providers.
        /// </summary>
        /// <param name="providers">List of providers</param>
        /// <param name="searchText">Search Query</param>
        /// <param name="flags">Options defining how the query will be performed</param>
        /// <returns>New SearchContext</returns>
        public static SearchContext CreateContext(IEnumerable<SearchProvider> providers, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(providers, searchText, flags);
        }

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, IEnumerable<SearchProvider> providers, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return new SearchContext(providers, searchText, flags, runtimeContext);
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

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, SearchProvider provider, string searchText = "")
        {
            return CreateContext(runtimeContext, new[] { provider }, searchText);
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

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, string providerId, string searchText = "", SearchFlags flags = SearchFlags.Default)
        {
            return CreateContext(runtimeContext, new[] { providerId }, searchText, flags);
        }

        /// <summary>
        /// Create a search context with all active providers.
        /// </summary>
        /// <returns></returns>
        public static SearchContext CreateContext(string searchText, SearchFlags flags)
        {
            return CreateContext(GetActiveProviders(), searchText, flags);
        }

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, string searchText, SearchFlags flags)
        {
            return CreateContext(runtimeContext, GetActiveProviders(), searchText, flags);
        }

        public static SearchContext CreateContext(string searchText)
        {
            return CreateContext(searchText, SearchFlags.Default);
        }

        internal static SearchContext CreateContext(RuntimeSearchContext runtimeContext, string searchText)
        {
            return CreateContext(runtimeContext, searchText, SearchFlags.Default);
        }

        /// <summary>
        /// Initiate a search and return all search items matching the search context. Other items can be found later using the asynchronous searches.
        /// </summary>
        /// <param name="context">The current search context</param>
        /// <param name="options">Options defining how the query will be performed</param>
        /// <returns>A list of search items matching the search query.</returns>
        public static List<SearchItem> GetItems(SearchContext context, SearchFlags options = SearchFlags.Default)
        {
            return GetItems(context, GetNewSessionGuid(), options);
        }

        static List<SearchItem> GetItems(SearchContext context, Hash128 sessionGuid, SearchFlags options = SearchFlags.Default)
        {
            // Stop all search sessions every time there is a new search.
            context.sessions.StopAllAsyncSearchSessions();
            context.searchFinishTime = context.searchStartTime = DateTime.UtcNow.Ticks;
            context.sessionEnded -= OnSearchEnded;
            context.sessionEnded += OnSearchEnded;

            context.sessions.StartSessions(context, sessionGuid);

            if (options.HasAny(SearchFlags.WantsMore))
                context.wantsMore = true;

            // Transfer all options from options to context.options
            context.options |= options;

            int fetchProviderCount = 0;
            var allItems = new List<SearchItem>(3);

            if (TryParseExpression(context, out var expression))
            {
                var iterator = EvaluateExpression(expression, context);
                PrepareProviderSession(s_SearchServiceProvider, iterator, context);
                HandleItemsIteratorSession(allItems, s_SearchServiceProvider, context);
                fetchProviderCount++;
            }
            else
            {
                // We have to start all providers first otherwise we can get in a situation where
                // each provider triggers a session ended because they all finish in the first batch,
                // leading to SessionStarted-SessionEnded-SessionStarted-SessionEnded...
                fetchProviderCount = PrepareProviderSessions(context.providers, context, allItems);
                foreach (var provider in context.providers)
                {
                    try
                    {
                        var watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        HandleItemsIteratorSession(allItems, provider, context);
                        provider.RecordFetchTime(watch.Elapsed.TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(new Exception($"Failed to fetch {provider.name} items.", ex));
                        var session = context.sessions.GetProviderSession(provider);
                        session.Stop();
                    }
                }
            }

            if (fetchProviderCount == 0)
            {
                context.sessions.CallSessionStarted(context);
                OnSearchEnded(context);
                context.sessions.StopAllAsyncSearchSessions();
                context.sessions.CallSessionEnded(context);
            }

            if (!context.options.HasAny(SearchFlags.Sorted))
                return allItems;

            allItems.Sort(SortItemComparer);
            return allItems.GroupBy(i => i.id).Select(i => i.First()).ToList();
        }

        static int PrepareProviderSessions(IEnumerable<SearchProvider> providers, SearchContext context, List<SearchItem> allItems)
        {
            var fetchProviderCount = 0;
            foreach (var provider in providers)
            {
                try
                {
                    var iterator = provider.fetchItems(context, allItems, provider);
                    if (iterator == allItems)
                        iterator = null;

                    PrepareProviderSession(provider, iterator, context);
                    ++fetchProviderCount;
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception($"Failed to get fetch {provider.name} provider items.", ex));
                    var session = context.sessions.GetProviderSession(provider);
                    session.Stop();
                }
            }
            return fetchProviderCount;
        }

        static void PrepareProviderSession(SearchProvider provider, object iterator, SearchContext context)
        {
            var session = context.sessions.GetProviderSession(provider);
            session.Reset(context.sessions.currentSessionContext, iterator, k_MaxFetchTimeMs, k_MaxSessionTimeMs);
            session.Start();
        }

        static void HandleItemsIteratorSession(List<SearchItem> allItems, SearchProvider provider, SearchContext context)
        {
            var session = context.sessions.GetProviderSession(provider);
            if (session.itemsEnumerator != null && context.options.HasAny(SearchFlags.Synchronous))
            {
                var stackedEnumerator = session.itemsEnumerator;
                while (stackedEnumerator.MoveNext())
                {
                    if (stackedEnumerator.Current != null)
                        allItems.Add(stackedEnumerator.Current);
                }

                session.Stop();
            }
            else
            {
                var sessionEnded = !session.FetchSome(allItems, k_MaxFetchTimeMs);
                if (context.options.HasAny(SearchFlags.FirstBatchAsync) && allItems.Count > 0)
                {
                    session.SendItems(allItems);
                    allItems.Clear();
                }
                if (sessionEnded)
                    session.Stop();
            }
        }

        /// <summary>
        /// Execute a search request that will fetch search results asynchronously.
        /// </summary>
        /// <param name="context">Search context used to track asynchronous request.</param>
        /// <param name="options">Options defining how the query will be performed</param>
        /// <returns>Asynchronous list of search items.</returns>
        public static ISearchList Request(SearchContext context, SearchFlags options = SearchFlags.None)
        {
            context.options |= options;
            ISearchList results = null;
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
            {
                results = new ConcurrentSearchList(context);

                Dispatcher.Enqueue(() =>
                {
                    results.AddItems(GetItems(context, context.options));
                    (results as ConcurrentSearchList)?.GetItemsDone();
                });

                return results;
            }

            if (context.options.HasAny(SearchFlags.Sorted))
                results = new SortedSearchList(context);
            else
                results = new AsyncSearchList(context);

            results.AddItems(GetItems(context, context.options));
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
            return Request(context, context.options);
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
            }, context.options);
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
            }, context.options);
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
                context.options);
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
            // TODO: Clean up this method. Those callbacks will allocate new objects each time this is called.
            // We rely on some requestId to avoid issues with previous sessions. This could be improved.

            // If we are in a thread, defer the execution to the main thread to avoid
            // potential synchronization issues with the code below.
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
            {
                Dispatcher.Enqueue(() =>
                {
                    Request(context, onIncomingItems, onSearchCompleted, options);
                });
                return;
            }

            var requestId = GetNewSessionGuid();
            context.options |= options;
            if (context.options.HasAny(SearchFlags.Debug))
                Debug.Log($"{requestId} Request started {context.searchText} ({context.options})");
            var batchCount = 1;

            void ReceiveItems(SearchContext c, IEnumerable<SearchItem> items)
            {
                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} #{batchCount++} Request incoming batch {context.searchText}");
                onIncomingItems?.Invoke(c, items.Where(e => e != null));
            }

            void OnSessionStarted(SearchContext c)
            {
                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request session begin {context.searchText}");
            }

            void OnSessionEnded(SearchContext c)
            {
                if (c.sessions.currentSessionContext.guid != requestId)
                    return;
                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request session ended {context.searchText}");

                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request async ended {context.searchText}");
                context.asyncItemReceived -= ReceiveItems;
                context.sessionStarted -= OnSessionStarted;
                context.sessionEnded -= OnSessionEnded;
                onSearchCompleted?.Invoke(c);
            }

            context.asyncItemReceived += ReceiveItems;
            context.sessionStarted += OnSessionStarted;
            var firstResults = GetItems(context, requestId, context.options);
            if (firstResults.Count > 0)
                ReceiveItems(context, firstResults);

            // When search is completed in the first batch, sessionEnded is called before
            // we get here. To avoid OnSessionEnded being called before ReceiveItems,
            // we need to make sure that OnSessionEnded is registered only after the call to GetItems.
            // However, we can only do this if the call to GetItems and the sessions are done in the main thread.
            if (context.searchInProgress)
                context.sessionEnded += OnSessionEnded;
            else
            {
                OnSessionEnded(context);
            }
        }

        static Hash128 GetNewSessionGuid()
        {
            return Hash128.Compute(Guid.NewGuid().ToByteArray());
        }

        private static void OnSearchEnded(SearchContext context)
        {
            context.searchFinishTime = DateTime.UtcNow.Ticks;
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
            Providers = SearchUtils.SortProvider(TypeCache.GetMethodsWithAttribute<SearchItemProviderAttribute>()
                .Select(LoadProvider)
                .Where(provider => provider != null))
                .ToList();
            s_SearchServiceProvider = SearchServiceProvider.CreateProvider();
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
                         catch (Exception ex)
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
            if (reuseExisting) flags |= SearchFlags.ReuseExistingWindow;
            if (multiselect) flags |= SearchFlags.Multiselect;
            if (dockable) flags |= SearchFlags.Dockable;
            return SearchWindow.Create<SearchWindow>(context, topic, flags).ShowWindow(defaultWidth, defaultHeight, flags);
        }

        /// <summary>
        /// Show a search window.
        /// </summary>
        /// <param name="viewState">Defines search view parameters for creation</param>
        /// <returns></returns>
        public static ISearchView ShowWindow(SearchViewState viewState)
        {
            return SearchWindow.Create(viewState).ShowWindow(viewState.windowSize.x, viewState.windowSize.y,
                SearchFlags.OpenDefault | (viewState.context?.options ?? SearchFlags.None));
        }

        /// <summary>
        /// Open QuickSearch in contextual mode enabling only the providers specified.
        /// </summary>
        /// <param name="providerIds">List of provider ids to enabled for QuickSearch</param>
        /// <returns>Returns the QuickSearch window.</returns>
        public static ISearchView ShowContextual(params string[] providerIds)
        {
            return SearchUtils.OpenWithContextualProviders(null, providerIds,
                contextualFlags: SearchUtils.OpenWithContextualProvidersFlags.None);
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
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, searchText, "object", "api");
            var state = CreateObjectPickerState(selectHandler, trackingHandler, searchText, typeName, filterType, defaultWidth, defaultHeight, flags);
            return ShowPicker(state);
        }

        public static ISearchView ShowPicker(
            SearchContext context,
            Action<SearchItem, bool> selectHandler,
            Action<SearchItem> trackingHandler = null,
            Func<SearchItem, bool> filterHandler = null,
            IEnumerable<SearchItem> subset = null,
            string title = null, float itemSize = 64f, float defaultWidth = 650f, float defaultHeight = 539f, SearchFlags flags = SearchFlags.None)
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchPickerOpens, context.searchText, "item", "api");
            var state = CreatePickerState(context, selectHandler, trackingHandler, filterHandler, subset, title, itemSize, defaultWidth, defaultHeight, flags);
            return SearchPickerWindow.ShowPicker(state);
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

        public static IEnumerable<SearchProvider> GetActiveProviders()
        {
            return SearchUtils.GetActiveProviders(Providers);
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
                if (!AssetDatabase.TryGetAssetFolderInfo(indexPath, out var rootFolder, out var immutable) || immutable)
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

        public static IEnumerable<ISearchDatabase> EnumerateDatabases()
        {
            foreach (var db in SearchDatabase.EnumerateAll())
                yield return db;
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
                    SearchDatabase.Unload(db);

                    try
                    {
                        if (options.HasNone(IndexingOptions.Keep) && System.IO.File.Exists(indexPath))
                        {
                            if (options.HasAny(IndexingOptions.Temporary))
                                System.IO.File.Delete(indexPath);
                            else
                            {
                                AssetDatabase.DeleteAsset(indexPath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignore any file IO errors (for the user)
                        Console.WriteLine(ex.Message);
                    }
                });
            }
            else
                Utils.CallDelayed(() => TrackCreateIndex(db, options, indexName, indexPath, onIndexReady, delay), delay);
        }

        static bool TryParseExpression(SearchContext context, out SearchExpression expression)
        {
            expression = null;
            if (string.IsNullOrEmpty(context.searchText) || context.options.HasAny(SearchFlags.QueryString))
                return false;

            if (!CouldContainExpression(context))
                return false;

            var rootExpression = ParseExpression(context);
            if (rootExpression == null || (rootExpression.types.HasAny(SearchExpressionType.QueryString) &&
                    rootExpression.parameters.Length == 0 && rootExpression.innerText == rootExpression.outerText && !rootExpression.hasEscapedNestedExpressions) ||
                !rootExpression.types.HasAny(SearchExpressionType.Iterable))
                return false;

            expression = rootExpression;
            return true;
        }

        static bool CouldContainExpression(SearchContext context)
        {
            var text = context.searchText;
            return text.IndexOf('{') != -1 || text.IndexOf('}') != -1 ||
                text.IndexOf('[') != -1 || text.IndexOf(']') != -1;
        }

        static SearchExpression ParseExpression(SearchContext context)
        {
            try
            {
                return SearchExpression.Parse(context);
            }
            catch (SearchExpressionParseException ex)
            {
                var queryError = new SearchQueryError(ex.index, ex.length, ex.Message,
                    context, s_SearchServiceProvider, fromSearchQuery: false, SearchQueryErrorType.Error);
                context.AddSearchQueryError(queryError);
                return null;
            }
        }

        static IEnumerable<SearchItem> EvaluateExpression(SearchExpression expression, SearchContext context)
        {
            using (SearchMonitor.GetView())
            {
                var evaluationFlags = SearchExpressionExecutionFlags.ThreadedEvaluation;
                var it = expression.Execute(context, evaluationFlags).GetEnumerator();
                while (EvaluateExpression(context, it))
                    yield return it.Current;

                SearchUtils.SetupColumns(context, expression);
            }
        }

        static bool EvaluateExpression(SearchContext context, IEnumerator<SearchItem> it)
        {
            try
            {
                return it.MoveNext();
            }
            catch (SearchExpressionEvaluatorException ex)
            {
                var queryError = new SearchQueryError(ex.errorView.startIndex, ex.errorView.length, ex.Message,
                    context, s_SearchServiceProvider, fromSearchQuery: false, SearchQueryErrorType.Error);
                context.AddSearchQueryError(queryError);
                return false;
            }
        }

        internal static SearchViewState CreateObjectPickerState(Action<UnityEngine.Object, bool> selectHandler,
            Action<UnityEngine.Object> trackingHandler,
            string searchText, string typeName, Type filterType,
            float defaultWidth = 850, float defaultHeight = 539,
            SearchFlags flags = SearchFlags.None)
        {
            var context = CreateContext(GetObjectProviders(), searchText, flags | SearchFlags.OpenPicker);
            var state = SearchViewState.CreatePickerState("Select object", context, selectHandler, trackingHandler, typeName, filterType);
            state.position = new Rect(0, 0, defaultWidth, defaultHeight);
            return state;
        }

        internal static SearchViewState CreatePickerState(
            SearchContext context,
            Action<SearchItem, bool> selectHandler,
            Action<SearchItem> trackingHandler = null,
            Func<SearchItem, bool> filterHandler = null,
            IEnumerable<SearchItem> subset = null,
            string title = null, float itemSize = 64f, float defaultWidth = 650f, float defaultHeight = 539f, SearchFlags flags = SearchFlags.None)
        {
            context.options |= flags | SearchFlags.OpenPicker;
            var state = SearchViewState.CreatePickerState(title, context, selectHandler, trackingHandler, filterHandler);
            state.position = new Rect(0, 0, defaultWidth, defaultHeight);
            state.itemSize = itemSize;
            return state;
        }
    }
}
