// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search.Providers
{
    static class HelpProvider
    {
        internal static string type = "help";
        internal static string displayName = "Help";

        static Dictionary<SearchItem, Action<SearchItem, SearchContext>> m_StaticItemToAction;

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            var helpProvider = new SearchProvider(type, displayName)
            {
                priority = -1,
                filterId = "?",
                isExplicitProvider = true,
                fetchItems = (context, items, provider) => FetchItems(context, provider)
            };

            return helpProvider;
        }

        private static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            if (m_StaticItemToAction == null)
                BuildHelpItems(context, provider);

            var searchQuery = context.searchQuery.Trim();
            var helpItems = m_StaticItemToAction.Keys;
            var fetchAllItems = string.IsNullOrEmpty(searchQuery);
            foreach (var helpItem in helpItems)
            {
                if (fetchAllItems ||
                    helpItem.GetLabel(context, true).IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) != -1 ||
                    helpItem.GetDescription(context, true).IndexOf(searchQuery, StringComparison.OrdinalIgnoreCase) != -1)
                    yield return helpItem;
            }
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "help", null, "Help") {
                    closeWindowAfterExecution = false,
                    handler = (item) =>
                    {
                        if (item.id.StartsWith("help_recent_"))
                        {
                            item.context.searchView.SetSearchText((string)item.data);
                        }
                        else if (m_StaticItemToAction.TryGetValue(item, out var helpHandler))
                        {
                            helpHandler(item, item.context);
                        }
                    }
                }
            };
        }

        static void BuildHelpItems(SearchContext context, SearchProvider helpProvider)
        {
            if (context.searchView != null)
                context.searchView.itemIconSize = 1f;

            m_StaticItemToAction = new Dictionary<SearchItem, Action<SearchItem, SearchContext>>();

            // Settings provider: id, Search for...
            foreach (var provider in SearchService.OrderedProviders)
            {
                var helpItem = provider.isExplicitProvider ?
                    helpProvider.CreateItem(context, $"help_provider_{provider.id}",
                    $"Activate only <b>{provider.name}</b>",
                    $"Type <b>{provider.filterId}</b> to activate <b>{provider.name}</b>", null, null) :
                    helpProvider.CreateItem(context, $"help_provider_{provider.id}",
                    $"Search only <b>{provider.name}</b>",
                    $"Type <b>{provider.filterId}</b> to search <b>{provider.name}</b>", null, null);

                helpItem.score = m_StaticItemToAction.Count;
                helpItem.thumbnail = Icons.help;
                m_StaticItemToAction.Add(helpItem, (item, _context) => _context.searchView.SetSearchText(provider.filterId));
            }

            {
                var helpItem = helpProvider.CreateItem(context, "help_open_pref", "Open Search Preferences", null, null, null);
                helpItem.score = m_StaticItemToAction.Count;
                helpItem.thumbnail = Icons.settings;
                m_StaticItemToAction.Add(helpItem, (item, _context) => SettingsService.OpenUserPreferences(SearchSettings.settingsPreferencesKey));
            }
        }
    }
}
