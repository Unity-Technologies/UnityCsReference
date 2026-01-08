// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class MenuProvider
    {
        struct MenuData
        {
            public string path;
            public string[] words;
        }

        internal const string type = "menu";
        private const string displayName = "Menus";
        private const string disabledMenuExecutionWarning = "The menu you are trying to execute is disabled. It will not be executed.";

        private static string[] shortcutIds;
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = true, skipNestedQueries = true };
        private static QueryEngine<MenuData> queryEngine = null;
        private static List<MenuData> menus;

        private static Delayer debounce;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            List<string> itemNames = new List<string>();
            List<string> shortcuts = new List<string>();
            GetMenuInfo(itemNames, shortcuts);

            System.Threading.Tasks.Task.Run(() => BuildMenus(itemNames));

            queryEngine = new QueryEngine<MenuData>(k_QueryEngineOptions);
            queryEngine.SetFilter("id", m => m.path)
                .AddOrUpdatePropositionData(label: "Menu Path", replacement: "id:create/", help: "Filter by menu path.", priority: 9999);
            queryEngine.SetSearchDataCallback(m => m.words, s => Utils.FastToLower(s), StringComparison.Ordinal);

            debounce = Delayer.Debounce(_ => TriggerBackgroundUpdate(itemNames, shortcuts));

            Menu.menuChanged -= OnMenuChanged;
            Menu.menuChanged += OnMenuChanged;

            return new SearchProvider(type, displayName)
            {
                priority = 80,
                filterId = "m:",
                showDetailsOptions = ShowDetailsOptions.ListView | ShowDetailsOptions.Actions,

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                onEnable = () => shortcutIds = ShortcutManager.instance.GetAvailableShortcutIds().ToArray(),
#pragma warning restore RS0030
                onDisable = () => shortcutIds = new string[0],

                fetchItems = FetchItems,

                fetchLabel = (item, context) =>
                {
                    if (item.label == null)
                    {
                        var menuName = Utils.GetFileName(item.id);
                        var enabled = Menu.GetEnabled(item.id);
                        var @checked = Menu.GetChecked(item.id);
                        item.label = $"{menuName}{(enabled ? "" : " (disabled)")} {(@checked ? "\u2611" : "")}";
                    }
                    return item.label;
                },

                fetchDescription = (item, context) =>
                {
                    if (string.IsNullOrEmpty(item.description))
                        item.description = GetMenuDescription(item.id);
                    return item.description;
                },

                fetchThumbnail = (item, context) => Icons.shortcut,
                fetchPropositions = (context, options) => FetchPropositions(context, options)
            };
        }

        private static void OnMenuChanged()
        {
            debounce.Execute();
        }

        private static void TriggerBackgroundUpdate(List<string> itemNames, List<string> shortcuts)
        {
            GetMenuInfo(itemNames, shortcuts);
            menus = null;
            System.Threading.Tasks.Task.Run(() => BuildMenus(itemNames));
        }


        private static void BuildMenus(List<string> itemNames)
        {
            var localMenus = new List<MenuData>();
            for (int i = 0; i < itemNames.Count; ++i)
            {
                var menuItem = itemNames[i];
                localMenus.Add(new MenuData
                {
                    path = menuItem,
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    words = SplitMenuPath(menuItem).Select(w => Utils.FastToLower(w)).ToArray()
#pragma warning restore RS0030
                });
            }

            menus = localMenus;
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var query = (string.IsNullOrEmpty(context.searchQuery) && context.providers.Count() == 1) ? null : queryEngine.ParseQuery(context.searchQuery);
#pragma warning restore RS0030
            if (query != null && !query.valid)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
#pragma warning restore RS0030
                yield break;
            }

            while (menus == null)
                yield return null;

            var results = query == null ? menus : query.Apply(menus, false);
            foreach (var m in results)
                yield return provider.CreateItem(context, m.path);
        }

        private static IEnumerable<string> SplitMenuPath(string menuPath)
        {
            yield return menuPath;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var m in menuPath.Split(new char[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries).Reverse())
#pragma warning restore RS0030
                yield return m;
        }

        private static string GetMenuDescription(string menuName)
        {
            var sm = ShortcutManager.instance;
            if (sm == null)
                return menuName;

            var shortcutId = menuName;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!shortcutIds.Contains(shortcutId))
#pragma warning restore RS0030
            {
                shortcutId = "Main Menu/" + menuName;
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (!shortcutIds.Contains(shortcutId))
#pragma warning restore RS0030
                    return menuName;
            }
            var shortcutBinding = ShortcutManager.instance.GetShortcutBinding(shortcutId);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!shortcutBinding.keyCombinationSequence.Any())
#pragma warning restore RS0030
                return menuName;

            return $"{menuName} ({shortcutBinding})";
        }

        static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                yield break;

            foreach (var p in QueryAndOrBlock.BuiltInQueryBuilderPropositions())
                yield return p;

            foreach (var proposition in queryEngine.GetPropositions())
                yield return proposition;
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction("menu", "select", null, "Execute shortcut")
                {
                    handler = (item) =>
                    {
                        var menuId = item.id;
                        if (!Menu.GetEnabled(menuId))
                        {
                            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, disabledMenuExecutionWarning);
                            return;
                        }
                        EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem(menuId);
                    }
                }
            };
        }

        [Shortcut("Help/Search/Menu")]
        internal static void OpenQuickSearch()
        {
            var qs = SearchUtils.OpenWithContextualProviders(type, Settings.type);
            qs.itemIconSize = 1; // Open in list view by default.
        }

        private static void GetMenuInfo(List<string> outItemNames, List<string> outItemDefaultShortcuts)
        {
            Utils.GetMenuItemDefaultShortcuts(outItemNames, outItemDefaultShortcuts);
        }
    }
}
