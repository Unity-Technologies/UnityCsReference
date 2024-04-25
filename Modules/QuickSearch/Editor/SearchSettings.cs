// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SearchService;
using UnityEngine;

namespace UnityEditor.Search
{
    abstract class BaseDictionarySettings : IDictionary
    {
        public abstract int Count { get; }
        public abstract ICollection Keys { get; }
        public abstract ICollection Values { get; }

        public abstract object this[object key]
        {
            get;
            set;
        }

        public abstract IDictionaryEnumerator GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => true;
        public object SyncRoot => this;

        public void Add(object key, object value) { throw new NotSupportedException(); }
        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(object key) { throw new NotSupportedException(); }
        public void CopyTo(Array array, int index) { throw new NotSupportedException(); }
        public void Remove(object key) { throw new NotSupportedException(); }
    }

    class SearchProviderSettings : BaseDictionarySettings
    {
        public bool active;
        public int priority;
        public string defaultAction;

        public override int Count => 3;
        public override ICollection Keys => new string[] { nameof(active), nameof(priority), nameof(defaultAction) };
        public override ICollection Values => new object[] { active, priority, defaultAction };

        public SearchProviderSettings()
        {
            active = true;
            priority = 0;
            defaultAction = null;
        }

        public override object this[object key]
        {
            get
            {
                switch ((string)key)
                {
                    case nameof(active): return active;
                    case nameof(priority): return priority;
                    case nameof(defaultAction): return defaultAction;
                }
                return null;
            }

            set => throw new NotSupportedException();
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            var d = new Dictionary<string, object>()
            {
                {nameof(active), active},
                {nameof(priority), priority},
                {nameof(defaultAction), defaultAction}
            };
            return d.GetEnumerator();
        }
    }

    class ObjectSelectorsSettings : BaseDictionarySettings
    {
        public bool active;
        public int priority;

        public override int Count => 2;
        public override ICollection Keys => new string[] { nameof(active), nameof(priority) };
        public override ICollection Values => new object[] { active, priority };

        public ObjectSelectorsSettings()
        {
            active = true;
            priority = 0;
        }

        public override object this[object key]
        {
            get
            {
                switch ((string)key)
                {
                    case nameof(active): return active;
                    case nameof(priority): return priority;
                }
                return null;
            }

            set => throw new NotSupportedException();
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            var d = new Dictionary<string, object>()
            {
                {nameof(active), active},
                {nameof(priority), priority}
            };
            return d.GetEnumerator();
        }
    }

    class SearchSettingsStorage
    {
        public string settingsFolder { get; set; }
        public string settingsPath { get; set; }

        public string itemIconSizePrefKey { get; set; }= "Search.ItemIconSize";
        public string favoritesQueryPrefKey { get; set; } = "SearchQuery.Favorites";
        public string ignoredPropertiesCustomDependency { get; set; } = "SearchIndexIgnoredProperties";

        // Per project settings
        public bool trackSelection { get; set; }
        public bool fetchPreview { get; set; }
        public SearchFlags defaultFlags { get; set; }
        public bool keepOpen { get; set; }
        public string queryFolder { get; set; }
        public bool onBoardingDoNotAskAgain { get; set; }
        public bool showPackageIndexes { get; set; }
        public bool showStatusBar { get; set; }
        public bool hideTabs { get; set; }
        public SearchQuerySortOrder savedSearchesSortOrder { get; set; }
        public bool showSavedSearchPanel { get; set; }
        public Dictionary<string, string> scopes { get; private set; } = new();
        public Dictionary<string, SearchProviderSettings> providers { get; private set; } = new();
        public Dictionary<string, ObjectSelectorsSettings> objectSelectors { get; private set; } = new();
        public bool queryBuilder { get; set; }
        public string ignoredProperties { get; set; }
        public string helperWidgetCurrentArea { get; set; }
        public bool refreshSearchWindowsInPlayMode { get; set; }
        public int minIndexVariations { get; set; }
        public bool findProviderIndexHelper { get; set; }
        public int[] expandedQueries { get; set; } = Array.Empty<int>();

        public bool wantsMore
        {
            get => defaultFlags.HasAny(SearchFlags.WantsMore);
            set
            {
                if (value)
                    defaultFlags |= SearchFlags.WantsMore;
                else
                    defaultFlags &= ~SearchFlags.WantsMore;
            }
        }

        // User editor pref
        public float itemIconSize { get; set; } = (float)DisplayMode.List;

        int m_RecentSearchMaxCount = 20;
        public int recentSearchMaxCount
        {
            get => m_RecentSearchMaxCount;
            set
            {
                m_RecentSearchMaxCount = value;
                ApplyRecentSearchCapacity();
            }
        }
        public List<string> recentSearches = new();

        string m_DisabledIndexersString;
        HashSet<string> m_DisabledIndexers;
        public HashSet<string> disabledIndexers
        {
            get
            {
                if (m_DisabledIndexers == null)
                {
                    var entries = m_DisabledIndexersString ?? string.Empty;
                    m_DisabledIndexers = new HashSet<string>(entries.Split(new string[] { ";;;" }, StringSplitOptions.RemoveEmptyEntries));
                }
                return m_DisabledIndexers;
            }
        }

        public HashSet<string> searchItemFavorites = new();
        public HashSet<string> searchQueryFavorites = new();

        public int debounceMs
        {
            get { return UnityEditor.SearchUtils.debounceThresholdMs; }

            set { UnityEditor.SearchUtils.debounceThresholdMs = value; }
        }

        public void Load()
        {
            IDictionary settings = null;
            try
            {
                settings = (IDictionary)SJSON.Load(settingsPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"We weren't able to parse search user settings at {settingsPath}. We will fallback to default settings.\n{ex}");
            }

            trackSelection = ReadSetting(settings, nameof(trackSelection), true);
            fetchPreview = ReadSetting(settings, nameof(fetchPreview), true);
            defaultFlags = (SearchFlags)ReadSetting(settings, nameof(defaultFlags), (int)SearchFlags.None);
            keepOpen = ReadSetting(settings, nameof(keepOpen), false);
            queryFolder = ReadSetting(settings, nameof(queryFolder), "Assets");
            onBoardingDoNotAskAgain = ReadSetting(settings, nameof(onBoardingDoNotAskAgain), false);
            showPackageIndexes = ReadSetting(settings, nameof(showPackageIndexes), false);
            showStatusBar = ReadSetting(settings, nameof(showStatusBar), false);
            hideTabs = ReadSetting(settings, nameof(hideTabs), false);
            savedSearchesSortOrder = (SearchQuerySortOrder)ReadSetting(settings, nameof(savedSearchesSortOrder), 0);
            showSavedSearchPanel = ReadSetting(settings, nameof(showSavedSearchPanel), false);
            queryBuilder = ReadSetting(settings, nameof(queryBuilder), false);
            ignoredProperties = ReadSetting(settings, nameof(ignoredProperties), "id;name;classname;imagecontentshash");
            helperWidgetCurrentArea = ReadSetting(settings, nameof(helperWidgetCurrentArea), "all");
            m_DisabledIndexersString = ReadSetting(settings, nameof(disabledIndexers), "");
            refreshSearchWindowsInPlayMode = ReadSetting(settings, nameof(refreshSearchWindowsInPlayMode), false);
            minIndexVariations = ReadSetting(settings, nameof(minIndexVariations), 2);
            findProviderIndexHelper = ReadSetting(settings, nameof(findProviderIndexHelper), true);

            itemIconSize = EditorPrefs.GetFloat(itemIconSizePrefKey, itemIconSize);


            var searches = ReadSetting<object[]>(settings, nameof(recentSearches));
            if (searches != null)
                recentSearches = searches.Cast<string>().ToList();

            var favoriteItems = ReadSetting<object[]>(settings, nameof(searchItemFavorites));
            if (favoriteItems != null)
                searchItemFavorites.UnionWith(favoriteItems.Cast<string>());

            var expandedObjects = ReadSetting<object[]>(settings, nameof(expandedQueries));
            if (expandedObjects != null)
                expandedQueries = expandedObjects.Select(Convert.ToInt32).ToArray();

            scopes = ReadProperties<string>(settings, nameof(scopes));
            providers = ReadProviderSettings(settings, nameof(providers));
            objectSelectors = ReadPickerSettings(settings, nameof(objectSelectors));

            LoadFavorites();

            RegisterIgnoredPropertiesCustomDependencies();
        }

        public void Save()
        {
            var settings = new Dictionary<string, object>
            {
                [nameof(trackSelection)] = trackSelection,
                [nameof(refreshSearchWindowsInPlayMode)] = refreshSearchWindowsInPlayMode,
                [nameof(fetchPreview)] = fetchPreview,
                [nameof(defaultFlags)] = (int)defaultFlags,
                [nameof(keepOpen)] = keepOpen,
                [nameof(queryFolder)] = queryFolder,
                [nameof(onBoardingDoNotAskAgain)] = onBoardingDoNotAskAgain,
                [nameof(showPackageIndexes)] = showPackageIndexes,
                [nameof(showStatusBar)] = showStatusBar,
                [nameof(scopes)] = scopes,
                [nameof(providers)] = providers,
                [nameof(objectSelectors)] = objectSelectors,
                [nameof(recentSearches)] = recentSearches,
                [nameof(searchItemFavorites)] = searchItemFavorites.ToList(),
                [nameof(savedSearchesSortOrder)] = (int)savedSearchesSortOrder,
                [nameof(showSavedSearchPanel)] = showSavedSearchPanel,
                [nameof(hideTabs)] = hideTabs,
                [nameof(expandedQueries)] = expandedQueries ?? Array.Empty<int>(),
                [nameof(queryBuilder)] = queryBuilder,
                [nameof(ignoredProperties)] = ignoredProperties,
                [nameof(helperWidgetCurrentArea)] = helperWidgetCurrentArea,
                [nameof(disabledIndexers)] = string.Join(";;;", disabledIndexers),
                [nameof(minIndexVariations)] = minIndexVariations,
                [nameof(findProviderIndexHelper)] = findProviderIndexHelper,

            };

            RetriableOperation<IOException>.Execute(() =>
            {
                CreateFolderIfNeeded();
                SJSON.Save(settings, settingsPath);
            }, 5, TimeSpan.FromMilliseconds(10));
            SaveFavorites();

            EditorPrefs.SetFloat(itemIconSizePrefKey, itemIconSize);

            RegisterIgnoredPropertiesCustomDependencies();
        }

        public void RegisterIgnoredPropertiesCustomDependencies()
        {
            if (AssetDatabase.IsAssetImportWorkerProcess() || EditorApplication.isUpdating)
                return;
            AssetDatabaseAPI.RegisterCustomDependency(ignoredPropertiesCustomDependency, Hash128.Compute(ignoredProperties));
        }

        public void ClearSettingsFile()
        {
            CreateFolderIfNeeded();
            Utils.WriteTextFileToDisk(settingsPath, "{}");
        }

        void CreateFolderIfNeeded()
        {
            if (!Directory.Exists(settingsFolder))
                Directory.CreateDirectory(settingsFolder);
        }

        public void SetScopeValue(string prefix, int hash, string value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value;
        }

        public void SetScopeValue(string prefix, int hash, int value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value.ToString();
        }

        public void SetScopeValue(string prefix, int hash, float value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value.ToString();
        }

        public void SetScopeValue(string prefix, int hash, Rect rect)
        {
            scopes[$"{prefix}.{hash:X8}"] = $"{rect.x};{rect.y};{rect.width};{rect.height}";
        }

        public string GetScopeValue(string prefix, int hash, string defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
                return value;
            return defaultValue;
        }

        public int GetScopeValue(string prefix, int hash, int defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
            {
                return Convert.ToInt32(value);
            }
            return defaultValue;
        }

        public float GetScopeValue(string prefix, int hash, float defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        public Rect GetScopeValue(string prefix, int hash, Rect defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
            {
                var rs = value.Split(";");
                if (rs.Length == 4)
                    return new Rect(Convert.ToSingle(rs[0]), Convert.ToSingle(rs[1]), Convert.ToSingle(rs[2]), Convert.ToSingle(rs[3]));
            }
            return defaultValue;
        }

        public void AddRecentSearch(string search)
        {
            recentSearches.Insert(0, search);
            ApplyRecentSearchCapacity();
            recentSearches = recentSearches.Distinct().ToList();
        }

        void ApplyRecentSearchCapacity()
        {
            if (recentSearches.Count > recentSearchMaxCount)
                recentSearches.RemoveRange(recentSearchMaxCount, recentSearches.Count - recentSearchMaxCount);
        }

        public ObjectSelectorsSettings GetObjectSelectorSettings(string selectorId, bool defaultActive, int defaultPriority)
        {
            if (TryGetObjectSelectorSettings(selectorId, out var settings))
                return settings;

            objectSelectors[selectorId] = new ObjectSelectorsSettings() { active = defaultActive, priority = defaultPriority };
            return objectSelectors[selectorId];
        }

        public bool TryGetObjectSelectorSettings(string selectorId, out ObjectSelectorsSettings settings)
        {
            return objectSelectors.TryGetValue(selectorId, out settings);
        }

        public void ResetObjectSelectorSettings()
        {
            objectSelectors.Clear();
        }

        public SearchProviderSettings GetProviderSettings(string providerId, bool defaultActive, int defaultPriority, string defaultAction, bool addToSettings = true)
        {
            if (TryGetProviderSettings(providerId, out var settings))
                return settings;

            var newSettings = new SearchProviderSettings() { active = defaultActive, priority = defaultPriority, defaultAction = defaultAction };
            if (addToSettings)
                providers[providerId] = newSettings;
            return newSettings;
        }

        public bool TryGetProviderSettings(string providerId, out SearchProviderSettings settings)
        {
            return providers.TryGetValue(providerId, out settings);
        }

        public void ResetProviderSettings()
        {
            providers.Clear();
        }

        public void ToggleCustomIndexer(string name, bool enable)
        {
            if (enable)
                disabledIndexers.Remove(name);
            else
                disabledIndexers.Add(name);
            Save();
        }

        public void AddItemFavorite(string itemId)
        {
            searchItemFavorites.Add(itemId);
        }

        public void RemoveItemFavorite(string itemId)
        {
            searchItemFavorites.Remove(itemId);
        }

        public void LoadFavorites()
        {
            var favoriteString = EditorPrefs.GetString(favoritesQueryPrefKey, "");
            searchQueryFavorites.UnionWith(favoriteString.Split(new string[] { ";;;" }, StringSplitOptions.RemoveEmptyEntries));
        }

        public void SaveFavorites()
        {
            EditorPrefs.SetString(favoritesQueryPrefKey, string.Join(";;;", searchQueryFavorites));
        }

        static T ReadSetting<T>(IDictionary settings, string key, T defaultValue = default)
        {
            try
            {
                if (SJSON.TryGetValue(settings, key, out var value))
                    return (T)value;
            }
            catch (Exception)
            {
                // Any error will return the default value.
            }

            return defaultValue;
        }

        static float ReadSetting(IDictionary settings, string key, float defaultValue = 0)
        {
            return (float)ReadSetting(settings, key, (double)defaultValue);
        }

        static int ReadSetting(IDictionary settings, string key, int defaultValue = 0)
        {
            return (int)ReadSetting(settings, key, (double)defaultValue);
        }

        static Dictionary<string, SearchProviderSettings> ReadProviderSettings(IDictionary settings, string fieldName)
        {
            return ReadDictionary<SearchProviderSettings>(settings, fieldName, vdict => new SearchProviderSettings()
            {
                active = Convert.ToBoolean(vdict[nameof(SearchProviderSettings.active)]),
                priority = (int)(double)vdict[nameof(SearchProviderSettings.priority)],
                defaultAction = vdict[nameof(SearchProviderSettings.defaultAction)] as string,
            });
        }

        static Dictionary<string, ObjectSelectorsSettings> ReadPickerSettings(IDictionary settings, string fieldName)
        {
            return ReadDictionary<ObjectSelectorsSettings>(settings, fieldName, vdict => new ObjectSelectorsSettings()
            {
                active = Convert.ToBoolean(vdict[nameof(SearchProviderSettings.active)]),
                priority = (int)(double)vdict[nameof(SearchProviderSettings.priority)]
            });
        }

        static Dictionary<string, T> ReadProperties<T>(IDictionary settings, string fieldName)
        {
            return ReadDictionary(settings, fieldName, o => (T)o);
        }

        static Dictionary<string, T> ReadDictionary<T>(IDictionary settings, string fieldName, Func<object, T> valueCreator)
        {
            return ReadDictionary(settings, fieldName, e => true, valueCreator);
        }

        static Dictionary<string, T> ReadDictionary<T>(IDictionary settings, string fieldName, Func<IDictionary, BaseDictionarySettings> valueCreator)
            where T : BaseDictionarySettings
        {
            return ReadDictionary(settings, fieldName, e => e.Value is IDictionary, o =>
            {
                var vdict = o as IDictionary;
                return (T)valueCreator(vdict);
            });
        }

        static Dictionary<string, T> ReadDictionary<T>(IDictionary settings, string fieldName, Func<DictionaryEntry, bool> extraPredicate, Func<object, T> valueCreator)
        {
            var d = new Dictionary<string, T>();
            if (SJSON.TryGetValue(settings, fieldName, out var _data) && _data is IDictionary dataDict)
            {
                foreach (var p in dataDict)
                {
                    try
                    {
                        if (p is DictionaryEntry e && extraPredicate(e))
                            d[(string)e.Key] = valueCreator(e.Value);
                    }
                    catch
                    {
                        // ignore copy
                    }
                }
            }
            return d;
        }
    }

    public static class SearchSettings
    {
        internal static readonly string projectLocalSettingsFolder = Utils.CleanPath(new DirectoryInfo("UserSettings").FullName);
        internal static readonly string projectLocalSettingsPath = $"{projectLocalSettingsFolder}/Search.settings";
        internal const string settingsPreferencesKey = "Preferences/Search";

        static SearchSettingsStorage s_SettingsStorage;

        // Per project settings
        internal static bool trackSelection
        {
            get => s_SettingsStorage.trackSelection;
            set => s_SettingsStorage.trackSelection = value;
        }

        internal static bool fetchPreview
        {
            get => s_SettingsStorage.fetchPreview;
            set => s_SettingsStorage.fetchPreview = value;
        }

        internal static SearchFlags defaultFlags
        {
            get => s_SettingsStorage.defaultFlags;
            set => s_SettingsStorage.defaultFlags = value;
        }

        internal static bool keepOpen
        {
            get => s_SettingsStorage.keepOpen;
            set => s_SettingsStorage.keepOpen = value;
        }

        internal static string queryFolder
        {
            get => s_SettingsStorage.queryFolder;
            set => s_SettingsStorage.queryFolder = value;
        }

        internal static bool onBoardingDoNotAskAgain
        {
            get => s_SettingsStorage.onBoardingDoNotAskAgain;
            set => s_SettingsStorage.onBoardingDoNotAskAgain = value;
        }

        internal static bool showPackageIndexes
        {
            get => s_SettingsStorage.showPackageIndexes;
            set => s_SettingsStorage.showPackageIndexes = value;
        }

        internal static bool showStatusBar
        {
            get => s_SettingsStorage.showStatusBar;
            set => s_SettingsStorage.showStatusBar = value;
        }

        internal static bool hideTabs
        {
            get => s_SettingsStorage.hideTabs;
            set => s_SettingsStorage.hideTabs = value;
        }

        internal static SearchQuerySortOrder savedSearchesSortOrder
        {
            get => s_SettingsStorage.savedSearchesSortOrder;
            set => s_SettingsStorage.savedSearchesSortOrder = value;
        }

        internal static bool showSavedSearchPanel
        {
            get => s_SettingsStorage.showSavedSearchPanel;
            set => s_SettingsStorage.showSavedSearchPanel = value;
        }

        internal static Dictionary<string, string> scopes => s_SettingsStorage.scopes;
        internal static Dictionary<string, SearchProviderSettings> providers => s_SettingsStorage.providers;
        internal static Dictionary<string, ObjectSelectorsSettings> objectSelectors => s_SettingsStorage.objectSelectors;

        internal static bool queryBuilder
        {
            get => s_SettingsStorage.queryBuilder;
            set => s_SettingsStorage.queryBuilder = value;
        }

        internal static string ignoredProperties
        {
            get => s_SettingsStorage.ignoredProperties;
            set => s_SettingsStorage.ignoredProperties = value;
        }

        internal static string helperWidgetCurrentArea
        {
            get => s_SettingsStorage.helperWidgetCurrentArea;
            set => s_SettingsStorage.helperWidgetCurrentArea = value;
        }

        internal static bool refreshSearchWindowsInPlayMode
        {
            get => s_SettingsStorage.refreshSearchWindowsInPlayMode;
            set => s_SettingsStorage.refreshSearchWindowsInPlayMode = value;
        }

        internal static int minIndexVariations
        {
            get => s_SettingsStorage.minIndexVariations;
            set => s_SettingsStorage.minIndexVariations = value;
        }

        internal static bool findProviderIndexHelper
        {
            get => s_SettingsStorage.findProviderIndexHelper;
            set => s_SettingsStorage.findProviderIndexHelper = value;
        }

        internal static int[] expandedQueries
        {
            get => s_SettingsStorage.expandedQueries;
            set => s_SettingsStorage.expandedQueries = value;
        }

        internal static bool wantsMore
        {
            get => s_SettingsStorage.wantsMore;
            set => s_SettingsStorage.wantsMore = value;
        }

        const int k_RecentSearchMaxCount = 20;
        internal static IReadOnlyList<string> recentSearches => s_SettingsStorage.recentSearches;

        // User editor pref
        internal static float itemIconSize
        {
            get => s_SettingsStorage.itemIconSize;
            set => s_SettingsStorage.itemIconSize = value;
        }

        internal static HashSet<string> disabledIndexers => s_SettingsStorage.disabledIndexers;

        // TODO: That's not good that it is public like that. Should have been a property.
        public static HashSet<string> searchItemFavorites = new();

        internal static event Action<string, bool> providerActivationChanged;

        internal static int debounceMs
        {
            get => s_SettingsStorage.debounceMs;
            set => s_SettingsStorage.debounceMs = value;
        }

        static SearchSettings()
        {
            s_SettingsStorage = new SearchSettingsStorage()
            {
                settingsFolder = projectLocalSettingsFolder,
                settingsPath = projectLocalSettingsPath,
                recentSearchMaxCount = k_RecentSearchMaxCount,
                itemIconSize = (float)DisplayMode.List,
                expandedQueries = Array.Empty<int>()
            };
            Load();
        }

        internal static void Load()
        {
            if (Application.HasARGV("cleanTestPrefs") || !File.Exists(projectLocalSettingsPath))
            {
                s_SettingsStorage.ClearSettingsFile();
            }
            s_SettingsStorage.Load();
            searchItemFavorites = s_SettingsStorage.searchItemFavorites;
        }

        internal static void Save()
        {
            s_SettingsStorage?.Save();
        }

        internal static void SetScopeValue(string prefix, int hash, string value)
        {
            s_SettingsStorage.SetScopeValue(prefix, hash, value);
        }

        internal static void SetScopeValue(string prefix, int hash, int value)
        {
            s_SettingsStorage.SetScopeValue(prefix, hash, value);
        }

        internal static void SetScopeValue(string prefix, int hash, float value)
        {
            s_SettingsStorage.SetScopeValue(prefix, hash, value);
        }

        internal static void SetScopeValue(string prefix, int hash, Rect rect)
        {
            s_SettingsStorage.SetScopeValue(prefix, hash, rect);
        }

        internal static string GetScopeValue(string prefix, int hash, string defaultValue)
        {
            return s_SettingsStorage.GetScopeValue(prefix, hash, defaultValue);
        }

        internal static int GetScopeValue(string prefix, int hash, int defaultValue)
        {
            return s_SettingsStorage.GetScopeValue(prefix, hash, defaultValue);
        }

        internal static float GetScopeValue(string prefix, int hash, float defaultValue)
        {
            return s_SettingsStorage.GetScopeValue(prefix, hash, defaultValue);
        }

        internal static Rect GetScopeValue(string prefix, int hash, Rect defaultValue)
        {
            return s_SettingsStorage.GetScopeValue(prefix, hash, defaultValue);
        }

        internal static SearchFlags GetContextOptions()
        {
            return SearchFlags.Default | defaultFlags;
        }

        internal static SearchFlags ApplyContextOptions(SearchFlags options)
        {
            return options | defaultFlags;
        }

        internal static void ApplyContextOptions(SearchContext context)
        {
            context.options = ApplyContextOptions(context.options);
        }

        internal static void AddRecentSearch(string search)
        {
            s_SettingsStorage.AddRecentSearch(search);
        }

        [SettingsProvider]
        internal static SettingsProvider CreateSearchSettings()
        {
            var settings = new SettingsProvider(settingsPreferencesKey, SettingsScope.User)
            {
                guiHandler = DrawSearchSettings,
                keywords = new[] { "quick", "search",  }
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>()) // If you change this or add a new class, please update the test SearchSettingsTests.ContentClass_OnlyHasGUIContent
                    .Concat(SearchService.OrderedObjectSelectors.Select(s => s.displayName))
                    .Concat(SearchService.OrderedProviders.Select(p => p.name))
                    .Concat(GetOrderedApis().Select(api => api.displayName))
            };
            return settings;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateSearchIndexSettings()
        {
            return new SettingsProvider("Preferences/Search/Indexing", SettingsScope.User)
            {
                guiHandler = DrawSearchIndexingSettings,
                keywords = new[] { "search", "index", "indexer", "custom" },
            };
        }

        static void DrawSearchServiceSettings()
        {
            EditorGUILayout.LabelField(L10n.Tr("Search Engines"), EditorStyles.largeLabel);
            var orderedApis = GetOrderedApis();
            foreach (var api in orderedApis)
            {
                var searchContextName = api.displayName;
                var searchEngines = OrderSearchEngines(api.engines);
                if (searchEngines.Count == 0)
                    continue;

                var activeEngine = api.GetActiveSearchEngine();
                using (new EditorGUILayout.HorizontalScope())
                {
                    try
                    {
                        var items = searchEngines.Select(se => new GUIContent(se.name,
                            searchEngines.Count == 1 ?
                            $"Search engine for {searchContextName}" :
                            $"Set search engine for {searchContextName}")).ToArray();
                        var activeEngineIndex = Math.Max(searchEngines.FindIndex(engine => engine.name == activeEngine?.name), 0);
                        GUILayout.Space(20);
                        using (var scope = new EditorGUI.ChangeCheckScope())
                        {
                            var newSearchEngine = EditorGUILayout.Popup(new GUIContent(searchContextName), activeEngineIndex, items, GUILayout.ExpandWidth(true));
                            if (scope.changed)
                            {
                                api.SetActiveSearchEngine(searchEngines[newSearchEngine].name);
                                GUI.changed = true;
                            }
                            GUILayout.Space(35);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }

                activeEngine = api.GetActiveSearchEngine();
                if (api.engineScope == SearchEngineScope.ObjectSelector && activeEngine.name == QuickSearchEngine.k_Name &&
                    activeEngine is ObjectSelectorEngine)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);
                        DrawAdvancedObjectSelectorsSettings();
                    }
                }
            }
        }

        static IEnumerable<ISearchApi> GetOrderedApis()
        {
            return UnityEditor.SearchService.SearchService.searchApis.OrderBy(api => api.displayName);
        }

        static void DrawAdvancedObjectSelectorsSettings()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Space(10);

                foreach (var selector in SearchService.OrderedObjectSelectors)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);

                    var wasActive = selector.active;
                    var c = new GUIContent(selector.displayName, Content.toggleObjectSelectorActiveContent.tooltip);
                    if (EditorGUILayout.ToggleLeft(c, wasActive, GUILayout.Width(200)) != wasActive)
                    {
                        TogglePickerActive(selector);
                    }

                    if (GUILayout.Button(Content.increaseObjectSelectorPriorityContent, Styles.priorityButton))
                        DecreaseObjectSelectorPriority(selector, SearchService.ObjectSelectors);

                    if (GUILayout.Button(Content.decreaseObjectSelectorPriorityContent, Styles.priorityButton))
                        IncreaseObjectSelectorPriority(selector, SearchService.ObjectSelectors);

                    GUILayout.Space(20);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                if (GUILayout.Button(Content.resetObjectSelectorContent, GUILayout.MaxWidth(170)))
                    ResetObjectSelectorSettings();
                GUILayout.EndHorizontal();
            }
        }

        internal static ObjectSelectorsSettings GetObjectSelectorSettings(AdvancedObjectSelector selector)
        {
            return s_SettingsStorage.GetObjectSelectorSettings(selector.id, selector.active, selector.priority);
        }

        internal static ObjectSelectorsSettings GetObjectSelectorSettings(string selectorId)
        {
            var selector = SearchService.GetObjectSelector(selectorId);
            if (selector == null)
                return new ObjectSelectorsSettings();
            return GetObjectSelectorSettings(selector);
        }

        static void ResetObjectSelectorSettings()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.ObjectSelectorSettingsReset);
            s_SettingsStorage.ResetObjectSelectorSettings();
            SearchService.RefreshObjectSelectors();
        }

        internal static void TogglePickerActive(AdvancedObjectSelector selector)
        {
            var newActive = !selector.active;
            var settings = GetObjectSelectorSettings(selector);
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, "activateObjectSelector", selector.id, newActive.ToString());
            settings.active = newActive;
            selector.active = newActive;
        }

        internal static void DecreaseObjectSelectorPriority(AdvancedObjectSelector selector, IEnumerable<AdvancedObjectSelector> allSelectors)
        {
            var sortedPickers = allSelectors.OrderBy(p => p.priority).ToList();
            for (int i = 1, end = sortedPickers.Count; i < end; ++i)
            {
                var cp = sortedPickers[i];
                if (!cp.Equals(selector))
                    continue;

                var adj = sortedPickers[i - 1];
                var temp = selector.priority;
                if (cp.priority == adj.priority)
                    temp++;

                selector.priority = adj.priority;
                adj.priority = temp;

                GetObjectSelectorSettings(adj).priority = adj.priority;
                GetObjectSelectorSettings(selector).priority = selector.priority;
                break;
            }
        }

        internal static void IncreaseObjectSelectorPriority(AdvancedObjectSelector selector, IEnumerable<AdvancedObjectSelector> allSelectors)
        {
            var sortedPickers = allSelectors.OrderBy(p => p.priority).ToList();
            for (int i = 0, end = sortedPickers.Count - 1; i < end; ++i)
            {
                var cp = sortedPickers[i];
                if (!cp.Equals(selector))
                    continue;

                var adj = sortedPickers[i + 1];
                var temp = selector.priority;
                if (cp.priority == adj.priority)
                    temp--;

                selector.priority = adj.priority;
                adj.priority = temp;

                GetObjectSelectorSettings(adj).priority = adj.priority;
                GetObjectSelectorSettings(selector).priority = selector.priority;
                break;
            }
        }

        static List<UnityEditor.SearchService.ISearchEngineBase> OrderSearchEngines(IEnumerable<UnityEditor.SearchService.ISearchEngineBase> engines)
        {
            var defaultEngine = engines.First(engine => engine is UnityEditor.SearchService.LegacySearchEngineBase);
            var overrides = engines.Where(engine => !(engine is UnityEditor.SearchService.LegacySearchEngineBase));
            var orderedSearchEngines = new List<UnityEditor.SearchService.ISearchEngineBase> { defaultEngine };
            orderedSearchEngines.AddRange(overrides);
            return orderedSearchEngines;
        }


        internal static bool TryGetObjectSelectorSettings(string selectorId, out ObjectSelectorsSettings settings)
        {
            return s_SettingsStorage.TryGetObjectSelectorSettings(selectorId, out settings);
        }

        private static void DrawSearchSettings(string searchContext)
        {
            EditorGUIUtility.labelWidth = 350;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(10);
                    EditorGUI.BeginChangeCheck();
                    {
                        trackSelection = Toggle(Content.trackSelectionContent, nameof(trackSelection), trackSelection);
                        fetchPreview = Toggle(Content.fetchPreviewContent, nameof(fetchPreview), fetchPreview);
                        refreshSearchWindowsInPlayMode = Toggle(Content.refreshSearchWindowsInPlayModeContent, nameof(refreshSearchWindowsInPlayMode), refreshSearchWindowsInPlayMode);
                        var newDebounceMs = EditorGUILayout.IntSlider(Content.debounceThreshold, debounceMs, 0, 1000);
                        if (newDebounceMs != debounceMs)
                            debounceMs = newDebounceMs;

                        GUILayout.Space(10);
                        DrawProviderSettings();

                        GUILayout.Space(10);
                        DrawSearchServiceSettings();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Save();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawSearchIndexingSettings(string searchContext)
        {
            EditorGUIUtility.labelWidth = 350;
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(10);
                GUILayout.BeginVertical();
                {
                    EditorGUILayout.HelpBox("Any changes here requires to restart the editor to take effect.", MessageType.Warning);
                    if (EditorGUILayout.DropdownButton(Utils.GUIContentTemp("Custom Indexers"), FocusType.Passive))
                        OpenCustomIndexerMenu();

                    EditorGUI.BeginChangeCheck();
                    if (Unsupported.IsSourceBuild())
                    {
                        findProviderIndexHelper = EditorGUILayout.Toggle("Use Find Provider", findProviderIndexHelper);
                        minIndexVariations = EditorGUILayout.IntField("Min Variations", minIndexVariations);
                        if (minIndexVariations < 1)
                        {
                            minIndexVariations = 1;
                        }
                        else if (minIndexVariations > 5)
                        {
                            minIndexVariations = 5;
                        }
                    }

                    EditorGUILayout.LabelField(L10n.Tr("Ignored properties (Use line break or ; to separate tokens)"), EditorStyles.largeLabel);
                    ignoredProperties = EditorGUILayout.TextArea(ignoredProperties, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    if (EditorGUI.EndChangeCheck())
                        Save();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static void OpenCustomIndexerMenu()
        {
            var menu = new GenericMenu();
            foreach (var customIndexerMethodInfo in TypeCache.GetMethodsWithAttribute<CustomObjectIndexerAttribute>())
            {
                var name = $"{customIndexerMethodInfo.DeclaringType.FullName}.{customIndexerMethodInfo.Name}";
                var enabled = !disabledIndexers.Contains(name);
                menu.AddItem(new GUIContent(name), enabled, () => ToggleCustomIndexer(name, !enabled));
            }
            menu.ShowAsContext();
        }

        private static void ToggleCustomIndexer(string name, bool enable)
        {
            s_SettingsStorage.ToggleCustomIndexer(name, enable);
        }

        private static bool Toggle(GUIContent content, string propertyName, bool value)
        {
            var newValue = EditorGUILayout.Toggle(content, value);
            if (newValue != value)
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, propertyName, newValue.ToString());
            return newValue;
        }

        private static void DrawProviderSettings()
        {
            EditorGUILayout.LabelField(L10n.Tr("Provider Settings"), EditorStyles.largeLabel);
            foreach (var p in SearchService.OrderedProviders)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                var settings = GetProviderSettings(p.id);

                var content = p.name + " (" + $"{p.filterId}" + ")";
                var wasActive = p.active;
                GUILayout.Label(new GUIContent(content, Content.toggleProviderActiveContent.tooltip), GUILayout.Width(200));
                if (p.active != wasActive)
                {
                    SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, "activateProvider", p.id, p.active.ToString());
                    settings.active = p.active;
                    if (providerActivationChanged != null)
                        providerActivationChanged.Invoke(p.id, p.active);
                }

                if (!p.isExplicitProvider)
                {
                    if (GUILayout.Button(Content.increaseProviderPriorityContent, Styles.priorityButton))
                        LowerProviderPriority(p);

                    if (GUILayout.Button(Content.decreaseProviderPriorityContent, Styles.priorityButton))
                        UpperProviderPriority(p);
                }
                else
                {
                    GUILayoutUtility.GetRect(Content.increaseProviderPriorityContent, Styles.priorityButton);
                    GUILayoutUtility.GetRect(Content.increaseProviderPriorityContent, Styles.priorityButton);
                }

                GUILayout.Space(20);

                using (new EditorGUI.DisabledScope(p.actions.Count < 2))
                {
                    EditorGUI.BeginChangeCheck();
                    var items = p.actions.Select(a => new GUIContent(
                        string.IsNullOrEmpty(a.displayName) ? a.content.text : a.displayName,
                        a.content.image,
                        p.actions.Count == 1 ?
                        $"Default action for {p.name} (Enter)" :
                        $"Set default action for {p.name} (Enter)")).ToArray();
                    if (items.Length == 0)
                    {
                        items = new[] { new GUIContent("No actions available") };
                    }
                    var newDefaultAction = EditorGUILayout.Popup(0, items, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetDefaultAction(p.id, p.actions[newDefaultAction].id);
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button(Content.resetProvidersContent, GUILayout.MaxWidth(170)))
                ResetProviderSettings();
            GUILayout.EndHorizontal();
        }

        internal static SearchProviderSettings GetProviderSettings(string providerId)
        {
            var provider = SearchService.GetProvider(providerId);
            SearchProviderSettings defaultSettings = null;
            if (provider == null)
                defaultSettings = new SearchProviderSettings();

            return s_SettingsStorage.GetProviderSettings(providerId, provider?.active ?? defaultSettings.active, provider?.priority ?? defaultSettings.priority, null, provider != null);
        }

        internal static bool TryGetProviderSettings(string providerId, out SearchProviderSettings settings)
        {
            return s_SettingsStorage.TryGetProviderSettings(providerId, out settings);
        }

        private static void ResetProviderSettings()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceReset);
            s_SettingsStorage.ResetProviderSettings();
            SearchService.Refresh();
        }

        private static void LowerProviderPriority(SearchProvider provider)
        {
            var sortedProviderList = SearchService.Providers.Where(p => !p.isExplicitProvider).OrderBy(p => p.priority).ToList();
            for (int i = 1, end = sortedProviderList.Count; i < end; ++i)
            {
                var cp = sortedProviderList[i];
                if (cp != provider)
                    continue;

                var adj = sortedProviderList[i - 1];
                var temp = provider.priority;
                if (cp.priority == adj.priority)
                    temp++;

                provider.priority = adj.priority;
                adj.priority = temp;

                GetProviderSettings(adj.id).priority = adj.priority;
                GetProviderSettings(provider.id).priority = provider.priority;
                break;
            }
        }

        private static void UpperProviderPriority(SearchProvider provider)
        {
            var sortedProviderList = SearchService.Providers.Where(p => !p.isExplicitProvider).OrderBy(p => p.priority).ToList();
            for (int i = 0, end = sortedProviderList.Count - 1; i < end; ++i)
            {
                var cp = sortedProviderList[i];
                if (cp != provider)
                    continue;

                var adj = sortedProviderList[i + 1];
                var temp = provider.priority;
                if (cp.priority == adj.priority)
                    temp--;

                provider.priority = adj.priority;
                adj.priority = temp;

                GetProviderSettings(adj.id).priority = adj.priority;
                GetProviderSettings(provider.id).priority = provider.priority;
                break;
            }
        }

        private static void SetDefaultAction(string providerId, string actionId)
        {
            if (string.IsNullOrEmpty(providerId) || string.IsNullOrEmpty(actionId))
                return;

            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, "SetDefaultAction", providerId, actionId);
            GetProviderSettings(providerId).defaultAction = actionId;
            SortActionsPriority();
        }

        internal static void SortActionsPriority()
        {
            foreach (var searchProvider in SearchService.Providers)
                SortActionsPriority(searchProvider);
        }

        private static void SortActionsPriority(SearchProvider searchProvider)
        {
            if (searchProvider.actions.Count == 1)
                return;

            var defaultActionId = GetProviderSettings(searchProvider.id).defaultAction;
            if (string.IsNullOrEmpty(defaultActionId))
                return;
            if (searchProvider.actions.Count == 0 || defaultActionId == searchProvider.actions[0].id)
                return;

            searchProvider.actions.Sort((action1, action2) =>
            {
                if (action1.id == defaultActionId)
                    return -1;

                if (action2.id == defaultActionId)
                    return 1;

                return 0;
            });
        }

        internal static string GetFullQueryFolderPath()
        {
            var initialFolder = Utils.CleanPath(new DirectoryInfo(queryFolder).FullName);
            if (!Directory.Exists(initialFolder) || !Utils.IsPathUnderProject(initialFolder))
                initialFolder = new DirectoryInfo("Assets").FullName;
            return initialFolder;
        }

        class Styles
        {
            public static GUIStyle priorityButton = new GUIStyle("Button")
            {
                fixedHeight = 20,
                fixedWidth = 20,
                fontSize = 14,
                padding = new RectOffset(0, 0, 0, 4),
                margin = new RectOffset(1, 1, 1, 1),
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };
        }

        internal class Content
        {
            public static GUIContent toggleProviderActiveContent = EditorGUIUtility.TrTextContent("", "Enable or disable this provider. Disabled search provider will be completely ignored by the search service.");
            public static GUIContent resetProvidersContent = EditorGUIUtility.TrTextContent("Reset Providers Settings", "All search providers will restore their initial preferences (priority, active, default action)");
            public static GUIContent increaseProviderPriorityContent = EditorGUIUtility.TrTextContent("\u2191", "Increase the provider's priority");
            public static GUIContent decreaseProviderPriorityContent = EditorGUIUtility.TrTextContent("\u2193", "Decrease the provider's priority");
            public static GUIContent trackSelectionContent = EditorGUIUtility.TrTextContent(
                "Track the current selection in the search view.",
                "Tracking the current selection can alter other window state, such as pinging the project browser or the scene hierarchy window.");
            public static GUIContent fetchPreviewContent = EditorGUIUtility.TrTextContent(
                "Generate an asset preview thumbnail for found items",
                "Fetching the preview of the items can consume more memory and make searches within very large project slower.");
            public static GUIContent refreshSearchWindowsInPlayModeContent = EditorGUIUtility.TrTextContent(
                "Refresh Search views in Play Mode",
                "Automatically refresh search views when hierarchy changes happened in Play Mode");
            public static GUIContent dockableContent = EditorGUIUtility.TrTextContent("Open Search as dockable window");
            public static GUIContent debugContent = EditorGUIUtility.TrTextContent("[DEV] Display additional debugging information");
            public static GUIContent debounceThreshold = EditorGUIUtility.TrTextContent("Select the typing debounce threshold (ms)");

            public static GUIContent toggleObjectSelectorActiveContent = EditorGUIUtility.TrTextContent("", "Enable or disable this object selector. Disabled object selectors will be completely ignored.");
            public static GUIContent resetObjectSelectorContent = EditorGUIUtility.TrTextContent("Reset Selector Settings", "All object selectors will restore their initial preferences (priority, active)");
            public static GUIContent increaseObjectSelectorPriorityContent = EditorGUIUtility.TrTextContent("\u2191", "Increase the object selector's priority");
            public static GUIContent decreaseObjectSelectorPriorityContent = EditorGUIUtility.TrTextContent("\u2193", "Decrease the object selector's priority");
        }

        public static void AddItemFavorite(SearchItem item)
        {
            searchItemFavorites.Add(item.id);
            s_SettingsStorage.AddItemFavorite(item.id);
            Dispatcher.Emit(SearchEvent.ItemFavoriteStateChanged, new SearchEventPayload(item.context, item.id));
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchAddFavoriteItem, item.provider.id);
        }

        public static void RemoveItemFavorite(SearchItem item)
        {
            searchItemFavorites.Remove(item.id);
            s_SettingsStorage.RemoveItemFavorite(item.id);
            Dispatcher.Emit(SearchEvent.ItemFavoriteStateChanged, new SearchEventPayload(item.context, item.id));
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.QuickSearchRemoveFavoriteItem, item.provider.id);
        }
    }
}
