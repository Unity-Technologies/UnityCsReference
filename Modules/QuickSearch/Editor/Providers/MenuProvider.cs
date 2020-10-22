// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

        private const string type = "menu";
        private const string displayName = "Menus";

        private static string[] shortcutIds;
        private static QueryEngine<MenuData> queryEngine = null;
        private static readonly List<MenuData> menus = new List<MenuData>();

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            List<string> itemNames = new List<string>();
            List<string> shortcuts = new List<string>();
            GetMenuInfo(itemNames, shortcuts);

            for (int i = 0; i < itemNames.Count; ++i)
            {
                var menuItem = itemNames[i];
                menus.Add(new MenuData()
                {
                    path = menuItem,
                    words = SplitMenuPath(menuItem).Concat(new string[] { menuItem }).Select(w => w.ToLowerInvariant()).ToArray()
                });
            }

            queryEngine = new QueryEngine<MenuData>();
            queryEngine.AddFilter("id", m => m.path);
            queryEngine.SetSearchDataCallback(m => m.words, s => s.ToLowerInvariant(), StringComparison.Ordinal);

            queryEngine.SetNestedQueryHandler((q, f) => q.Split(',').Select(w => w.Trim()));
            queryEngine.SetFilterNestedQueryTransformer<string, string>("id", s => s);

            return new SearchProvider(type, displayName)
            {
                priority = 80,
                filterId = "m:",
                showDetailsOptions = ShowDetailsOptions.ListView | ShowDetailsOptions.Actions,

                onEnable = () =>
                {
                    shortcutIds = ShortcutManager.instance.GetAvailableShortcutIds().ToArray();
                },

                onDisable = () =>
                {
                    shortcutIds = new string[0];
                },

                fetchItems = (context, items, provider) =>
                {
                    if (string.IsNullOrEmpty(context.searchQuery))
                        return null;
                    var query = queryEngine.Parse(context.searchQuery);
                    if (!query.valid)
                        return null;
                    return query.Apply(menus).Select(m => provider.CreateItem(context, m.path));
                },

                fetchLabel = (item, context) =>
                {
                    return item.label ?? (item.label = Utils.GetNameFromPath(item.id));
                },

                fetchDescription = (item, context) =>
                {
                    if (String.IsNullOrEmpty(item.description))
                        item.description = GetMenuDescription(item.id);
                    return item.description;
                },

                fetchThumbnail = (item, context) => Icons.shortcut
            };
        }

        private static IEnumerable<string> SplitMenuPath(string menuPath)
        {
            return menuPath.Split(new char[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries).Reverse();
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
            if (!shortcutBinding.keyCombinationSequence.Any())
                return menuName;

            return $"{menuName} ({shortcutBinding})";
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
                        EditorApplication.delayCall += () => EditorApplication.ExecuteMenuItem(menuId);
                    }
                }
            };
        }

        [Shortcut("Help/Search/Menu", KeyCode.M, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        internal static void OpenQuickSearch()
        {
            var qs = QuickSearch.OpenWithContextualProvider(type, Settings.type);
            qs.itemIconSize = 1; // Open in list view by default.
        }

        private static void GetMenuInfo(List<string> outItemNames, List<string> outItemDefaultShortcuts)
        {
            Menu.GetMenuItemDefaultShortcuts(outItemNames, outItemDefaultShortcuts);
        }
    }
}
