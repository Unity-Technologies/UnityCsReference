// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor
{
    static class SearchServiceSettings
    {
        public const string settingsPreferencesKey = "Preferences/Search Service";

        [UsedImplicitly, SettingsProvider]
        static SettingsProvider CreateSearchSettings()
        {
            var settings = new SettingsProvider(settingsPreferencesKey, SettingsScope.User)
            {
                keywords = new[] { "search" },
                guiHandler = searchContext =>
                {
                    EditorGUIUtility.labelWidth = 500;
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(10);
                        GUILayout.BeginVertical();
                        {
                            GUILayout.Space(10);
                            DrawProviderSettings();
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
            };
            return settings;
        }

        static void DrawProviderSettings()
        {
            EditorGUILayout.LabelField("Search Engines", EditorStyles.largeLabel);
            foreach (var api in SearchService.GetSearchApis())
            {
                var searchContextName = api.displayName;
                var searchEngines = OrderSearchEngines(api.engines);
                if (searchEngines.Count ==  0)
                    continue;

                using (new EditorGUILayout.HorizontalScope())
                {
                    try
                    {
                        var items = searchEngines.Select(se => new GUIContent(se.displayName,
                            searchEngines.Count == 1 ?
                            $"Search engine for {searchContextName}" :
                            $"Set search engine for {searchContextName}")).ToArray();
                        var activeEngine = api.GetActiveSearchEngine();
                        var activeEngineIndex = Math.Max(searchEngines.FindIndex(engine => engine.id == activeEngine?.id), 0);

                        GUILayout.Space(20);
                        GUILayout.Label(new GUIContent(searchContextName), GUILayout.Width(175));
                        GUILayout.Space(20);

                        using (var scope = new EditorGUI.ChangeCheckScope())
                        {
                            var newSearchEngine = EditorGUILayout.Popup(activeEngineIndex, items, GUILayout.ExpandWidth(true));
                            if (scope.changed)
                            {
                                api.SetActiveSearchEngine(searchEngines[newSearchEngine].id);
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

        static List<SearchService.ISearchEngineBase> OrderSearchEngines(IEnumerable<SearchService.ISearchEngineBase> engines)
        {
            var defaultEngine = engines.First(engine => engine is DefaultSearchEngineBase);
            var overrides = engines.Where(engine => !(engine is DefaultSearchEngineBase));
            var orderedSearchEngines = new List<SearchService.ISearchEngineBase> { defaultEngine };
            orderedSearchEngines.AddRange(overrides);
            return orderedSearchEngines;
        }
    }
}
