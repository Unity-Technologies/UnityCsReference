// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchProviderSettings : IDictionary
    {
        public bool active;
        public int priority;
        public string defaultAction;

        public int Count => 3;
        public ICollection Keys => new string[] { nameof(active), nameof(priority), nameof(defaultAction) };
        public ICollection Values => new object[] { active, priority, defaultAction };

        public SearchProviderSettings()
        {
            active = true;
            priority = 0;
            defaultAction = null;
        }

        public object this[object key]
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

        public IDictionaryEnumerator GetEnumerator()
        {
            var d = new Dictionary<string, object>()
            {
                {nameof(active), active},
                {nameof(priority), priority},
                {nameof(defaultAction), defaultAction}
            };
            return d.GetEnumerator();
        }

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

    static class SearchSettings
    {
        const string k_ProjectUserSettingsPath = "UserSettings/Search.settings";
        public const string settingsPreferencesKey = "Preferences/Search";
        public static readonly string globalSearchSettingsFolder = Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Search").Replace("\\", "/");

        // Per project settings
        public static bool trackSelection { get; set; }
        public static bool fetchPreview { get; set; }
        public static bool wantsMore { get; set; }
        public static bool keepOpen { get; set; }
        public static string queryFolder { get; set; }
        public static float itemIconSize { get; set; }
        public static bool onBoardingDoNotAskAgain { get; set; }
        public static bool showPackageIndexes { get; set; }
        public static bool showStatusBar { get; set; }
        public static bool useExpressions { get; set; }
        public static Dictionary<string, string> scopes { get; private set; }
        public static Dictionary<string, SearchProviderSettings> providers { get; private set; }
        public const int k_RecentSearchMaxCount = 20;
        public static List<string> recentSearches = new List<string>(k_RecentSearchMaxCount);
        public static SearchQuerySortOrder savedSearchesSortOrder { get; set; }

        public static int debounceMs
        {
            get
            {
                return UnityEditor.SearchUtils.debounceThresholdMs;
            }

            set
            {
                UnityEditor.SearchUtils.debounceThresholdMs = value;
            }
        }

        static SearchSettings()
        {
            Load();
        }

        private static void Load()
        {
            if (!File.Exists(k_ProjectUserSettingsPath))
            {
                if (!Directory.Exists("UserSettings/"))
                    Directory.CreateDirectory("UserSettings/");
                File.WriteAllText(k_ProjectUserSettingsPath, "{}");
            }

            IDictionary settings = null;
            try
            {
                settings = (IDictionary)SJSON.Load(k_ProjectUserSettingsPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"We weren't able to parse search user settings at {k_ProjectUserSettingsPath}. We will fallback to default settings.\n{ex}");
            }

            trackSelection = ReadSetting(settings, nameof(trackSelection), true);
            fetchPreview = ReadSetting(settings, nameof(fetchPreview), true);
            wantsMore = ReadSetting(settings, nameof(wantsMore), false);
            keepOpen = ReadSetting(settings, nameof(keepOpen), false);
            itemIconSize = ReadSetting(settings, nameof(itemIconSize), 1.0f);
            queryFolder = ReadSetting(settings, nameof(queryFolder), "Assets");
            onBoardingDoNotAskAgain = ReadSetting(settings, nameof(onBoardingDoNotAskAgain), false);
            showPackageIndexes = ReadSetting(settings, nameof(showPackageIndexes), false);
            showStatusBar = ReadSetting(settings, nameof(showStatusBar), false);
            useExpressions = ReadSetting(settings, nameof(useExpressions), false);
            savedSearchesSortOrder = (SearchQuerySortOrder)ReadSetting(settings, nameof(savedSearchesSortOrder), 0);


            var searches = ReadSetting<object[]>(settings, nameof(recentSearches));
            if (searches != null)
            {
                recentSearches = searches.Cast<string>().ToList();
            }

            scopes = ReadProperties<string>(settings, nameof(scopes));
            providers = ReadProviderSettings(settings, nameof(providers));
        }

        public static void Save()
        {
            var settings = new Dictionary<string, object>
            {
                [nameof(trackSelection)] = trackSelection,
                [nameof(fetchPreview)] = fetchPreview,
                [nameof(wantsMore)] = wantsMore,
                [nameof(keepOpen)] = keepOpen,
                [nameof(itemIconSize)] = itemIconSize,
                [nameof(queryFolder)] = queryFolder,
                [nameof(onBoardingDoNotAskAgain)] = onBoardingDoNotAskAgain,
                [nameof(showPackageIndexes)] = showPackageIndexes,
                [nameof(showStatusBar)] = showStatusBar,
                [nameof(useExpressions)] = useExpressions,
                [nameof(scopes)] = scopes,
                [nameof(providers)] = providers,
                [nameof(recentSearches)] = recentSearches,
                [nameof(savedSearchesSortOrder)] = (int)savedSearchesSortOrder,

            };

            SJSON.Save(settings, k_ProjectUserSettingsPath);
        }

        public static void SetScopeValue(string prefix, int hash, string value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value;
        }

        public static void SetScopeValue(string prefix, int hash, int value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value.ToString();
        }

        public static void SetScopeValue(string prefix, int hash, float value)
        {
            scopes[$"{prefix}.{hash:X8}"] = value.ToString();
        }

        public static string GetScopeValue(string prefix, int hash, string defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
                return value;
            return defaultValue;
        }

        public static int GetScopeValue(string prefix, int hash, int defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
            {
                return Convert.ToInt32(value);
            }
            return defaultValue;
        }

        public static float GetScopeValue(string prefix, int hash, float defaultValue)
        {
            if (scopes.TryGetValue($"{prefix}.{hash:X8}", out var value))
            {
                return Convert.ToSingle(value);
            }
            return defaultValue;
        }

        public static SearchFlags GetContextOptions()
        {
            SearchFlags options = SearchFlags.Default;
            if (wantsMore)
                options |= SearchFlags.WantsMore;
            return options;
        }

        public static SearchFlags ApplyContextOptions(SearchFlags options)
        {
            if (wantsMore)
                options |= SearchFlags.WantsMore;

            if (useExpressions)
                options |= SearchFlags.Expression;

            return options;
        }

        public static void ApplyContextOptions(SearchContext context)
        {
            context.options = ApplyContextOptions(context.options);
        }

        public static void AddRecentSearch(string search)
        {
            recentSearches.Insert(0, search);
            if (recentSearches.Count > k_RecentSearchMaxCount)
                recentSearches.RemoveRange(k_RecentSearchMaxCount, recentSearches.Count - k_RecentSearchMaxCount);
            recentSearches = recentSearches.Distinct().ToList();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateSearchSettings()
        {
            var settings = new SettingsProvider(settingsPreferencesKey, SettingsScope.User)
            {
                guiHandler = DrawSearchSettings,
                keywords = new[] { "quick", "omni", "search" },
            };
            return settings;
        }

        static void DrawSearchServiceSettings()
        {
            EditorGUILayout.LabelField("Search Engines", EditorStyles.largeLabel);
            var orderedApis = UnityEditor.SearchService.SearchService.searchApis.OrderBy(api => api.displayName);
            foreach (var api in orderedApis)
            {
                var searchContextName = api.displayName;
                var searchEngines = OrderSearchEngines(api.engines);
                if (searchEngines.Count == 0)
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    try
                    {
                        var items = searchEngines.Select(se => new GUIContent(se.name,
                            searchEngines.Count == 1 ?
                            $"Search engine for {searchContextName}" :
                            $"Set search engine for {searchContextName}")).ToArray();
                        var activeEngine = api.GetActiveSearchEngine();
                        var activeEngineIndex = Math.Max(searchEngines.FindIndex(engine => engine.name == activeEngine?.name), 0);

                        GUILayout.Space(20);
                        GUILayout.Label(new GUIContent(searchContextName), GUILayout.Width(175));
                        GUILayout.Space(20);

                        using (var scope = new EditorGUI.ChangeCheckScope())
                        {
                            var newSearchEngine = EditorGUILayout.Popup(activeEngineIndex, items, GUILayout.ExpandWidth(true));
                            if (scope.changed)
                            {
                                api.SetActiveSearchEngine(searchEngines[newSearchEngine].name);
                                GUI.changed = true;
                            }
                            GUILayout.Space(10);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
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
                        trackSelection = Toggle(Styles.trackSelectionContent, nameof(trackSelection), trackSelection);
                        fetchPreview = Toggle(Styles.fetchPreviewContent, nameof(fetchPreview), fetchPreview);
                        var newDebounceMs = EditorGUILayout.IntSlider(Styles.debounceThreshold, debounceMs, 0, 1000);
                        if (newDebounceMs != debounceMs)
                            debounceMs = newDebounceMs;

                        GUILayout.Space(10);
                        DrawProviderSettings();
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        Save();
                    }

                    GUILayout.Space(10);
                    DrawSearchServiceSettings();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static SearchAnalytics.GenericEvent SendDebounceValueChanged()
        {
            var e = SearchAnalytics.GenericEvent.Create(null, SearchAnalytics.GenericEventType.PreferenceChanged, nameof(debounceMs));
            e.intPayload1 = debounceMs;
            return e;
        }

        private static bool Toggle(GUIContent content, string propertyName, bool value)
        {
            var newValue = EditorGUILayout.Toggle(content, value);
            if (newValue != value)
                SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, propertyName, newValue.ToString());
            return newValue;
        }

        private static T ReadSetting<T>(IDictionary settings, string key, T defaultValue = default)
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

        private static float ReadSetting(IDictionary settings, string key, float defaultValue = 0)
        {
            return (float)ReadSetting(settings, key, (double)defaultValue);
        }

        private static int ReadSetting(IDictionary settings, string key, int defaultValue = 0)
        {
            return (int)ReadSetting(settings, key, (double)defaultValue);
        }

        private static Dictionary<string, SearchProviderSettings> ReadProviderSettings(IDictionary settings, string fieldName)
        {
            var properties = new Dictionary<string, SearchProviderSettings>();
            if (SJSON.TryGetValue(settings, fieldName, out var _data) && _data is IDictionary dataDict)
            {
                foreach (var p in dataDict)
                {
                    try
                    {
                        if (p is DictionaryEntry e && e.Value is IDictionary vdict)
                        {
                            properties[(string)e.Key] = new SearchProviderSettings()
                            {
                                active = Convert.ToBoolean(vdict[nameof(SearchProviderSettings.active)]),
                                priority = (int)(double)vdict[nameof(SearchProviderSettings.priority)],
                                defaultAction = vdict[nameof(SearchProviderSettings.defaultAction)] as string,
                            };
                        }
                    }
                    catch
                    {
                        // ignore copy
                    }
                }
            }
            return properties;
        }

        private static Dictionary<string, T> ReadProperties<T>(IDictionary settings, string fieldName)
        {
            var properties = new Dictionary<string, T>();
            if (SJSON.TryGetValue(settings, fieldName, out var _data) && _data is IDictionary dataDict)
            {
                foreach (var p in dataDict)
                {
                    try
                    {
                        if (p is DictionaryEntry e)
                            properties[(string)e.Key] = (T)e.Value;
                    }
                    catch
                    {
                        // ignore copy
                    }
                }
            }
            return properties;
        }

        private static void DrawProviderSettings()
        {
            EditorGUILayout.LabelField("Provider Settings", EditorStyles.largeLabel);
            foreach (var p in SearchService.OrderedProviders)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);

                var settings = GetProviderSettings(p.id);

                var wasActive = p.active;
                p.active = GUILayout.Toggle(wasActive, Styles.toggleActiveContent);
                if (p.active != wasActive)
                {
                    SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceChanged, "activateProvider", p.id, p.active.ToString());
                    settings.active = p.active;
                }

                using (new EditorGUI.DisabledGroupScope(!p.active))
                {
                    GUILayout.Label(new GUIContent(p.name, $"{p.id} ({p.priority})"), GUILayout.Width(175));
                }

                if (!p.isExplicitProvider)
                {
                    if (GUILayout.Button(Styles.increasePriorityContent, Styles.priorityButton))
                        LowerProviderPriority(p);

                    if (GUILayout.Button(Styles.decreasePriorityContent, Styles.priorityButton))
                        UpperProviderPriority(p);
                }
                else
                {
                    GUILayoutUtility.GetRect(Styles.increasePriorityContent, Styles.priorityButton);
                    GUILayoutUtility.GetRect(Styles.increasePriorityContent, Styles.priorityButton);
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
            if (GUILayout.Button(Styles.resetDefaultsContent, GUILayout.MaxWidth(170)))
                ResetProviderSettings();
            GUILayout.EndHorizontal();
        }

        public static SearchProviderSettings GetProviderSettings(string providerId)
        {
            if (TryGetProviderSettings(providerId, out var settings))
                return settings;

            var provider = SearchService.GetProvider(providerId);
            if (provider == null)
                return new SearchProviderSettings();

            providers[providerId] = new SearchProviderSettings() { active = provider.active, priority = provider.priority, defaultAction = null };
            return providers[providerId];
        }

        public static bool TryGetProviderSettings(string providerId, out SearchProviderSettings settings)
        {
            return providers.TryGetValue(providerId, out settings);
        }

        private static void ResetProviderSettings()
        {
            SearchAnalytics.SendEvent(null, SearchAnalytics.GenericEventType.PreferenceReset);
            providers.Clear();
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

        public static void SortActionsPriority()
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

        public static string GetFullQueryFolderPath()
        {
            var initialFolder = Utils.CleanPath(new DirectoryInfo(queryFolder).FullName);
            if (!Directory.Exists(initialFolder) || !Utils.IsPathUnderProject(initialFolder))
                initialFolder = new DirectoryInfo("Assets").FullName;
            return initialFolder;
        }

        static class Styles
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

            public static GUIStyle browseBtn = new GUIStyle("Button") { fixedWidth = 70 };

            public static GUIContent toggleActiveContent = new GUIContent("", "Enable or disable this provider. Disabled search provider will be completely ignored by the search service.");
            public static GUIContent resetDefaultsContent = new GUIContent("Reset Providers Settings", "All search providers will restore their initial preferences (priority, active, default action)");
            public static GUIContent increasePriorityContent = new GUIContent("\u2191", "Increase the provider's priority");
            public static GUIContent decreasePriorityContent = new GUIContent("\u2193", "Decrease the provider's priority");
            public static GUIContent trackSelectionContent = new GUIContent(
                "Track the current selection in the search view.",
                "Tracking the current selection can alter other window state, such as pinging the project browser or the scene hierarchy window.");
            public static GUIContent fetchPreviewContent = new GUIContent(
                "Generate an asset preview thumbnail for found items",
                "Fetching the preview of the items can consume more memory and make searches within very large project slower.");
            public static GUIContent dockableContent = new GUIContent("Open Search as dockable window");
            public static GUIContent debugContent = new GUIContent("[DEV] Display additional debugging information");
            public static GUIContent debounceThreshold = new GUIContent("Select the typing debounce threshold (ms)");
        }
    }
}
