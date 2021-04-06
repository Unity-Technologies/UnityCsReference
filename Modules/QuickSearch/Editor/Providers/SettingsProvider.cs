// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Search.Providers
{
    static class Settings
    {
        internal const string type = "settings";
        private const string displayName = "Settings";

        struct SettingsProviderInfo
        {
            public string path;
            public string label;
            public SettingsScope scope;
            public string[] searchables;
        }

        static class SettingsProviderCache
        {
            public static readonly SettingsProviderInfo[] value;
            public static readonly QueryEngine<SettingsProviderInfo> queryEngine;

            static SettingsProviderCache()
            {
                value = FetchSettingsProviders()
                    .Select(provider => new SettingsProviderInfo()
                {
                    path = provider.settingsPath,
                    label = provider.label,
                    scope = provider.scope,
                    searchables = new[] {provider.settingsPath, provider.label}.Concat(provider.keywords).Select(s => Utils.FastToLower(s)).ToArray()
                })
                    .ToArray();

                queryEngine = new QueryEngine<SettingsProviderInfo>();
                queryEngine.SetSearchDataCallback(info => info.searchables, s => Utils.FastToLower(s), StringComparison.Ordinal);
                queryEngine.AddFilter("scope", info => info.scope, new[] {":", "=", "!=", "<", ">", "<=", ">="});

                queryEngine.AddOperatorHandler(":", (SettingsScope ev, SettingsScope fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1);
                queryEngine.AddOperatorHandler(":", (SettingsScope ev, string fv, StringComparison sc) => ev.ToString().IndexOf(fv, sc) != -1);
                queryEngine.AddOperatorHandler("=", (SettingsScope ev, SettingsScope fv) => ev == fv);
                queryEngine.AddOperatorHandler("!=", (SettingsScope ev, SettingsScope fv) => ev != fv);
                queryEngine.AddOperatorHandler("<", (SettingsScope ev, SettingsScope fv) => ev < fv);
                queryEngine.AddOperatorHandler(">", (SettingsScope ev, SettingsScope fv) => ev > fv);
                queryEngine.AddOperatorHandler("<=", (SettingsScope ev, SettingsScope fv) => ev <= fv);
                queryEngine.AddOperatorHandler(">=", (SettingsScope ev, SettingsScope fv) => ev >= fv);
            }

            private static SettingsProvider[] FetchSettingsProviders()
            {
                return Utils.FetchSettingsProviders();
            }
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                filterId = "set:",
                showDetailsOptions = ShowDetailsOptions.ListView,
                fetchItems = (context, items, provider) => FetchItems(context, provider),
                fetchLabel = (item, context) => item.label ?? (item.label = Utils.GetNameFromPath(item.id)),

                fetchThumbnail = (item, context) => Icons.settings
            };
        }

        static IEnumerator FetchItems(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            var query = SettingsProviderCache.queryEngine.Parse(context.searchQuery);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                yield break;
            }

            yield return query.Apply(SettingsProviderCache.value).Select(spi => provider.CreateItem(context, spi.path, spi.label, spi.path, null, null));
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
