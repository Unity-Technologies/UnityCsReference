// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class HelpProvider
    {
        internal const string type = "help";
        internal const string displayName = "Help";

        delegate void HelpHandler();

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            var helpProvider = new SearchProvider(type, displayName, FetchItems)
            {
                priority = -1,
                filterId = "?",
                isExplicitProvider = true,
                fetchThumbnail = FetchIcon
            };

            return helpProvider;
        }

        private static Texture2D FetchIcon(SearchItem item, SearchContext context) => Icons.help;

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider helpProvider)
        {
            var searchView = context.searchView;
            if (searchView != null)
                searchView.itemIconSize = 1f;

            foreach (var p in SearchService.OrderedProviders)
            {
                if (p.priority < 0)
                    continue;
                var id = $"help_provider_{p.id}";
                var label = p.isExplicitProvider ? $"Activate only <b>{p.name}</b>" : $"Search only <b>{p.name}</b>";
                var description = p.isExplicitProvider ? $"Type <b>{p.filterId}</b> to activate <b>{p.name}</b>"
                    : $"Type <b>{p.filterId}</b> to search <b>{p.name}</b>";

                if (label.IndexOf(context.searchQuery, StringComparison.OrdinalIgnoreCase) == -1 &&
                    description.IndexOf(context.searchQuery, StringComparison.OrdinalIgnoreCase) == -1)
                    continue;

                HelpHandler helpHandler = () => searchView.SetSearchText(p.filterId);
                yield return helpProvider.CreateItem(context, id, p.priority, label, description, null, helpHandler);
            }

            yield return helpProvider.CreateItem(context, "help_open_pref", 9999, "Open Search Preferences", null, Icons.settings, (HelpHandler)OpenPreferences);
        }

        static void OpenPreferences()
        {
            SettingsService.OpenUserPreferences(SearchSettings.settingsPreferencesKey);
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            yield return new SearchAction(type, "help", null, "Help", ExecuteHelp) { closeWindowAfterExecution = false };
        }

        private static void ExecuteHelp(SearchItem item)
        {
            if (item.data is HelpHandler helpHandler)
                helpHandler.Invoke();
        }
    }
}
