// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SearchService;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Profiling;


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
        // Default max fetch time for an auto updated search session. This is a max per frame update, no matter how many providers there are in the session.
        internal static readonly TimeSpan k_MaxFetchTime = TimeSpan.FromMilliseconds(16);
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

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        internal static IEnumerable<AdvancedObjectSelector> OrderedObjectSelectors => ObjectSelectors.OrderBy(p => p.priority);
#pragma warning restore UA2001

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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var provider = Providers.FirstOrDefault(p => p.id == providerId);
#pragma warning restore UA2001
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var validators = ReflectionUtils.LoadAllMethodsWithAttribute<AdvancedObjectSelectorValidatorAttribute, AdvancedObjectSelectorValidator>(
#pragma warning restore UA2001
                (loaded, mi, attribute, handler) =>
                    LoadAdvancedObjectSelectorAttribute<AdvancedObjectSelectorValidatorAttribute, AdvancedObjectSelectorValidator, AdvancedObjectSelectorValidatorHandler>(
                    loaded, mi, attribute, handler, "Advanced Object Selector Validator", (a, h) => GenerateAdvancedObjectSelectorValidatorWrapper(a, h)),
                MethodSignature.FromDelegate<AdvancedObjectSelectorValidatorHandler>(), ReflectionUtils.AttributeLoaderBehavior.DoNotThrowOnValidation).ToDictionary(validator => validator.id.GetHashCode());

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            ObjectSelectors = ReflectionUtils.LoadAllMethodsWithAttribute<AdvancedObjectSelectorAttribute, AdvancedObjectSelector>(
#pragma warning restore UA2001
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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (loaded.Any(p => p.id.Equals(attribute.id, StringComparison.Ordinal)))
#pragma warning restore UA2001
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
        [Obsolete("GetItems is deprecated, please use Request instead. GetItems will be removed in a future version.")]
        public static List<SearchItem> GetItems(SearchContext context, SearchFlags options = SearchFlags.Default)
        {
            return GetItems(context, GetNewSessionGuid(), options);
        }

        static List<SearchItem> GetItems(SearchContext context, Hash128 sessionGuid, SearchFlags options)
        {
            PrepareSearchSession(context, context.session, sessionGuid, options, new EditorApplicationUpdateMechanism<SearchItem>(k_MaxFetchTime));
            return FetchSessionFirstBatch(context.session, context);
        }

        static void PrepareSearchSession(SearchContext context, SearchSession session, Hash128 sessionGuid, SearchFlags options, IAsyncIEnumerableHandlerUpdateMechanism<SearchItem> searchSessionUpdateMechanism)
        {
            // Stop the search session every time we prepare a new search.
            session.Stop();
            context.searchFinishTime = context.searchStartTime = DateTime.UtcNow.Ticks;
            context.sessionEnded -= OnSearchEnded;
            context.sessionEnded += OnSearchEnded;

            session.Reset(new SearchSessionContext(context, sessionGuid), searchSessionUpdateMechanism);

            // Transfer all options from options to context.options
            context.options |= options;

            var allItems = new List<SearchItem>();

            if (TryParseExpression(context, out var expression))
            {
                var iterator = EvaluateExpression(expression, context);
                PrepareProviderSession(session, s_SearchServiceProvider, iterator);
            }
            else
            {
                PrepareProviderSessions(context.providers, session, context, allItems);
            }

            // If we have some items already, it means that a provider populated the items argument, and either:
            // 1- returned null from fetchItems
            // 2- returned the same items argument from fetchItems
            // In either case, they did not return an iterator, so we have to handle the items as an additional enumerator in the session
            if (allItems.Count > 0)
            {
                session.AddProviderEnumerator(new SearchProviderFetchEnumerator(s_SearchServiceProvider, allItems));
            }
        }

        static List<SearchItem> FetchSessionFirstBatch(SearchSession session, SearchContext context)
        {
            var firstSessionBatch = new List<SearchItem>();

            // Start the session since we are about to fetch items.
            session.Start();
            var enumerationEnded = false;
            var searchItemEnumerator = session.ItemsEnumerator;
            if (searchItemEnumerator != null && context.options.HasAny(SearchFlags.Synchronous))
            {
                while (searchItemEnumerator.MoveNext())
                {
                    if (searchItemEnumerator.Current != null)
                        firstSessionBatch.Add(searchItemEnumerator.Current);
                }

                enumerationEnded = true;
            }
            else
            {
                enumerationEnded = !session.FetchSome(firstSessionBatch, k_MaxFetchTime);
            }

            if (firstSessionBatch.Count > 0)
                session.SendItems(firstSessionBatch);

            // If the enumeration ended, stop the session.
            if (enumerationEnded)
            {
                session.Stop();
            }

            return firstSessionBatch;
        }

        static void PrepareProviderSessions(IEnumerable<SearchProvider> providers, SearchSession session, SearchContext context, List<SearchItem> allItems)
        {
            foreach (var provider in providers)
            {
                try
                {
                    var watch = new System.Diagnostics.Stopwatch();
                    provider.ResetFetchTime();
                    watch.Start();
                    var iterator = provider.fetchItems(context, allItems, provider);
                    watch.Stop();
                    provider.IncrementFetchTime(watch.Elapsed);
                    if (iterator == allItems)
                        iterator = null;

                    // fetchItems could return null if allItems was populated directly. In this case, we
                    // don't add the iterator to the session, because we will handle allItems directly afterwards.
                    if (iterator != null)
                        PrepareProviderSession(session, provider, iterator);
                }
                catch (Exception ex)
                {
                    Debug.LogException(new Exception($"Failed to get {provider.name} provider fetchItems enumerator.", ex));
                }
            }
        }

        static void PrepareProviderSession(SearchSession session, SearchProvider provider, object iterator)
        {
            session.AddProviderEnumerator(new SearchProviderFetchEnumerator(provider, iterator));
        }

        /// <summary>
        /// Execute a search request that will fetch search results asynchronously.
        /// </summary>
        /// <param name="context">Search context used to track asynchronous request.</param>
        /// <param name="options">Options defining how the query will be performed</param>
        /// <returns>Asynchronous list of search items.</returns>
        public static ISearchList Request(SearchContext context, SearchFlags options = SearchFlags.None)
        {
            return Request(context, options, false);
        }

        static ISearchList Request(SearchContext context, SearchFlags options, bool searchListOwnsContext)
        {
            context.options |= options;
            ISearchList results = null;
            var sessionGuid = GetNewSessionGuid();
            var updateMechanism = new EditorApplicationUpdateMechanism<SearchItem>(k_MaxFetchTime);
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
            {
                results = new ConcurrentSearchList(context, searchListOwnsContext);

                Dispatcher.Enqueue(() =>
                {
                    PrepareSearchSession(context, context.session, sessionGuid, options, updateMechanism);
                    FetchSessionFirstBatch(context.session, context);
                    (results as ConcurrentSearchList)?.GetItemsDone();
                });

                return results;
            }

            results = new AsyncSearchList(context, searchListOwnsContext);

            PrepareSearchSession(context, context.session, sessionGuid, options, updateMechanism);
            FetchSessionFirstBatch(context.session, context);
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
            // Pass true to "searchListOwnsContext" to make sure the context is disposed with the searchList.
            return Request(context, context.options, true);
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
            var requestId = GetNewSessionGuid();
            context.options |= options;
            if (context.options.HasAny(SearchFlags.Debug))
                Debug.Log($"{requestId} Request started {context.searchText} ({context.options})");
            var batchCount = 1;

            void ReceiveItems(SearchContext c, IEnumerable<SearchItem> items)
            {
                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} #{batchCount++} Request incoming batch {context.searchText}");
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                onIncomingItems?.Invoke(c, items.Where(e => e != null));
#pragma warning restore UA2001
            }

            void OnSessionStarted(SearchContext c)
            {
                if (context.options.HasAny(SearchFlags.Debug))
                    Debug.Log($"{requestId} Request session begin {context.searchText}");
            }

            void OnSessionEnded(SearchContext c)
            {
                if (c.session.guid != requestId)
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

            // Prepare search session
            PrepareSearchSession(context, context.session, requestId, context.options, new EditorApplicationUpdateMechanism<SearchItem>(k_MaxFetchTime));

            // Register to async events
            context.asyncItemReceived += ReceiveItems;
            context.sessionStarted += OnSessionStarted;
            context.sessionEnded += OnSessionEnded;

            FetchSessionFirstBatch(context.session, context);
        }

        internal static IEnumerator<SearchItem> RequestEnumerator(SearchContext context)
        {
            var requestId = GetNewSessionGuid();
            PrepareSearchSession(context, context.session, requestId, context.options, new ManualUpdateMechanism<SearchItem>());

            // The session will be started and stopped by the enumerator.
            return new SearchSessionEnumerator(context.session);
        }

        static Hash128 GetNewSessionGuid()
        {
            return Hash128.Compute(Guid.NewGuid().ToByteArray());
        }

        private static void OnSearchEnded(SearchContext context)
        {
            context.searchFinishTime = DateTime.UtcNow.Ticks;
        }

        private static void RefreshProviders()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            Providers = SearchUtils.SortProvider(TypeCache.GetMethodsWithAttribute<SearchItemProviderAttribute>()
#pragma warning restore UA2001
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var action in TypeCache.GetMethodsWithAttribute<SearchActionsProviderAttribute>()
#pragma warning restore UA2001
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return providerIds.Select(GetProvider).Where(p => p != null);
#pragma warning restore UA2001
        }

        internal static IEnumerable<SearchProvider> GetProviders(IEnumerable<string> providerIds)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return providerIds.Select(GetProvider).Where(p => p != null);
#pragma warning restore UA2001
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
        [Obsolete]
        public static void CreateIndex(
            in string name,
            in IndexingOptions options,
            IEnumerable<string> roots,
            IEnumerable<string> includes,
            IEnumerable<string> excludes,
            Action<string, string, Action> onIndexReady)
        {
            // This is not allowed anymore.
            Debug.LogWarning("You cannot create new index anymore. Unity uses a single index solution.");
        }

        internal static bool IsDeepIndexingEnabled()
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            return db.settings.options.extended;
        }

        internal static bool IsPackageIndexingEnabled()
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return db.settings.roots.Any(r => r == "Packages");
#pragma warning restore UA2001
        }

        internal static void ChangeIndexingSettings(bool deepIndexing, bool packageIndexing, Action indexingReady)
        {
            var settingsDirty = false;
            var db = SearchDatabase.GetDefaultSearchDatabase();
            if (db.settings.options.extended != deepIndexing)
            {
                db.settings.options.extended = deepIndexing;
                settingsDirty = true;
            }

            if (IsPackageIndexingEnabled() != packageIndexing)
            {
                if (packageIndexing)
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    db.settings.roots = db.settings.roots.Concat(new [] {"Packages"}).ToArray();
#pragma warning restore UA2001
                else
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    db.settings.roots = db.settings.roots.Where(r => !r.Equals("Packages", StringComparison.InvariantCultureIgnoreCase)).ToArray();
#pragma warning restore UA2001
                }
                settingsDirty = true;
            }

            if (settingsDirty)
                db.SaveSettingsOptions(true);

            WaitForForIndexReady(indexingReady);
        }

        internal static void WaitForForIndexReady(Action indexingReady)
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            if (db.ready)
            {
                indexingReady?.Invoke();
            }
            else
            {
                Utils.CallDelayed(() => WaitForForIndexReady(indexingReady), 1d);
            }
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return SearchDatabase.EnumerateAll().Where(db =>
#pragma warning restore UA2001
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
