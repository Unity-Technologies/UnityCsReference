// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Unity.Collections;

namespace UnityEditor.Search.Providers
{
    static partial class MenuProvider
    {
        struct MenuData
        {
            public string path;
            public string[] words;
        }

        // Finder always lists everything on an empty query, even with other active providers; set via context.userData.
        internal enum ProviderMode
        {
            Searcher,
            Finder,
        }

        internal const string type = "menu";
        private const string displayName = "Menus";
        private const string disabledMenuExecutionWarning = "The menu you are trying to execute is disabled. It will not be executed.";

        [AutoStaticsCleanupOnCodeReload]
        private static string[] shortcutIds;
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = true, skipNestedQueries = true };
        [AutoStaticsCleanupOnCodeReload]
        private static QueryEngine<MenuData> queryEngine = null;
        [AutoStaticsCleanupOnCodeReload]
        private static List<MenuData> menus;

        [AutoStaticsCleanupOnCodeReload]
        private static Delayer debounce = null;

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
                showDetailsOptions = ShowDetailsOptions.ListView | ShowDetailsOptions.Actions | ShowDetailsOptions.DefaultGroup,

                #pragma warning disable UAC2001 // Avoid Linq
                onEnable = () => shortcutIds = ShortcutManager.instance.GetAvailableShortcutIds().ToArray(),
#pragma warning restore UAC2001
                onDisable = () => shortcutIds = Array.Empty<string>(),

                fetchItems = FetchItems,

                fetchLabel = (item, context) =>
                {
                    if (item.label == null)
                    {
                        var menuName = Utils.GetFileName(item.id);
                        var enabled = Menu.GetEnabled(item.id);
                        var @checked = Menu.GetChecked(item.id);
                        item.label = $"{menuName}{(enabled ? "" : " (disabled)")} {(@checked ? "\u2611" : "")}";

                        // Only in Finder mode: a menu item can be on the toolbar too, marked active like elements.
                        if (GetProviderMode(context) == ProviderMode.Finder)
                            item.label = MainToolbarElementProvider.FormatElementLabel(item.label, MainToolbarElementProvider.IsMenuItemActive(item.id));
                    }
                    return item.label;
                },

                fetchDescription = (item, context) =>
                {
                    if (string.IsNullOrEmpty(item.description))
                        item.description = GetMenuDescription(item.id);
                    return item.description;
                },

                fetchThumbnail = (item, context) =>
                {
                    // Only in Finder mode: a menu item can be on the toolbar too, dimmed like elements.
                    if (GetProviderMode(context) == ProviderMode.Finder)
                        item.thumbnailAlpha = MainToolbarElementProvider.IsMenuItemActive(item.id) ? 1f : MainToolbarElementProvider.inactiveThumbnailAlpha;
                    return Icons.shortcut;
                },
                fetchPropositions = (context, options) => FetchPropositions(context, options),
                fetchParentDescriptor = FetchParentDescriptor
            };
        }

        private static SearchItemParentDescriptor FetchParentDescriptor(SearchItem item, SearchContext context)
        {
            var lastSeparatorIndex = item.id.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
                return default;

            return new SearchItemParentDescriptor(item.id.Substring(0, lastSeparatorIndex), SearchItemParentType.TokenSeparatedId);
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
                    #pragma warning disable UAC2001 // Avoid Linq
                    words = SplitMenuPath(menuItem).Select(w => Utils.FastToLower(w)).ToArray()
#pragma warning restore UAC2001
                });
            }

            menus = localMenus;
        }

        static ProviderMode GetProviderMode(SearchContext context) =>
            context.userData is ProviderMode userDataMode ? userDataMode : ProviderMode.Searcher;

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            var mode = GetProviderMode(context);
            #pragma warning disable UAC2005 // Avoid Linq
            var showAllOnEmptyQuery = (mode == ProviderMode.Searcher && context.providers.Count() == 1) || mode == ProviderMode.Finder;
#pragma warning restore UAC2005
            var query = (string.IsNullOrEmpty(context.searchQuery) && showAllOnEmptyQuery) ? null : queryEngine.ParseQuery(context.searchQuery);
            if (query != null && !query.valid)
            {
                #pragma warning disable UAC2001 // Avoid Linq
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
#pragma warning restore UAC2001
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
            var splits = menuPath.Split(new char[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(splits);
            foreach (var m in splits)
                yield return m;
        }

        private static string GetMenuDescription(string menuName)
        {
            var sm = ShortcutManager.instance;
            if (sm == null)
                return menuName;

            var shortcutId = menuName;
            if (!shortcutIds.Contains(shortcutId))
            {
                shortcutId = "Main Menu/" + menuName;
                if (!shortcutIds.Contains(shortcutId))
                    return menuName;
            }
            var shortcutBinding = ShortcutManager.instance.GetShortcutBinding(shortcutId);
#pragma warning disable UAC2002 // Avoid Linq
            if (!shortcutBinding.keyCombinationSequence.Any())
#pragma warning restore UAC2002
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
            qs.context.userData = ProviderMode.Searcher;
            qs.itemIconSize = 1; // Open in list view by default.
        }

        private static void GetMenuInfo(List<string> outItemNames, List<string> outItemDefaultShortcuts)
        {
            Utils.GetMenuItemDefaultShortcuts(outItemNames, outItemDefaultShortcuts);
        }
    }
}
