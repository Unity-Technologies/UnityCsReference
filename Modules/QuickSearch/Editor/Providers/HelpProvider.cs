// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

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
                fetchItems = (context, items, provider) =>
                {
                    if (m_StaticItemToAction == null)
                        BuildHelpItems(context, provider);

                    var helpItems = m_StaticItemToAction.Keys;

                    if (string.IsNullOrEmpty(context.searchQuery) || string.IsNullOrWhiteSpace(context.searchQuery))
                    {
                        items.AddRange(helpItems);
                        return null;
                    }

                    items.AddRange(helpItems.Where(item => SearchUtils.MatchSearchGroups(context, item.label) || SearchUtils.MatchSearchGroups(context, item.description)));
                    return null;
                }
            };

            return helpProvider;
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

            // Action queries
            foreach (var kvp in SearchService.ActionIdToProviders)
            {
                var actionId = kvp.Key;
                var supportedProviderIds = kvp.Value;
                var provider = SearchService.GetProvider(supportedProviderIds[0]);
                var action = SearchService.GetAction(provider, actionId);
                if (action == null)
                    continue;

                var desc = Utils.FormatProviderList(supportedProviderIds.Select(providerId => SearchService.GetProvider(providerId)), showFetchTime: false);
                var helpItem = helpProvider.CreateItem(context, $"help_action_query_{actionId}",
                    $"{action.displayName} for {desc}",
                    $"Type <b> >{actionId}</b>", null, null);
                helpItem.thumbnail = Icons.shortcut;
                helpItem.score = m_StaticItemToAction.Count;
                m_StaticItemToAction.Add(helpItem, (item, _context) => _context.searchView.SetSearchText($">{actionId} "));
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
