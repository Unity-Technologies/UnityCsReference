// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace UnityEditor.Search.Providers
{
    static class Settings
    {
        internal const string type = "settings";
        private const string displayName = "Settings";

        static class SettingsPaths
        {
            public readonly static string[] value;

            static SettingsPaths()
            {
                value = FetchSettingsProviders().Select(provider => provider.settingsPath).ToArray();
            }

            private static SettingsProvider[] FetchSettingsProviders()
            {
                return SettingsService.FetchSettingsProviders();
            }
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                filterId = "set:",
                showDetailsOptions = ShowDetailsOptions.ListView,
                fetchItems = (context, items, provider) =>
                {
                    if (string.IsNullOrEmpty(context.searchQuery))
                        return null;

                    items.AddRange(SettingsPaths.value
                        .Where(path => SearchUtils.MatchSearchGroups(context, path, true))
                        .Select(path => provider.CreateItem(context, path, null, path, null, null)));
                    return null;
                },

                fetchLabel = (item, context) => item.label ?? (item.label = Utils.GetNameFromPath(item.id)),

                fetchThumbnail = (item, context) => Icons.settings
            };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "open", null, "Open project settings", (items) =>
                {
                    var item = items.Last();
                    if (item.id.StartsWith("Project/"))
                        SettingsService.OpenProjectSettings(item.id);
                    else
                        SettingsService.OpenUserPreferences(item.id);
                })
            };
        }
    }
}
