// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
                    searchables = new[] {provider.settingsPath, provider.label}
                        .Concat(provider.keywords)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Select(s => Utils.FastToLower(s)).ToArray()
                })
                    .ToArray();

                var iconName = "Filter Icon";
                var icon = Utils.LoadIcon(iconName);
                var scopeValues = Enum.GetNames(typeof(SettingsScope)).Select(n => Utils.FastToLower(n));
                queryEngine = new QueryEngine<SettingsProviderInfo>();
                queryEngine.SetSearchDataCallback(info => info.searchables, s => Utils.FastToLower(s), StringComparison.Ordinal);
                queryEngine.SetFilter("scope", info => info.scope, new[] { ":", "=", "!=", "<", ">", "<=", ">=" })
                    .SetGlobalPropositionData(category: "Scope", priority: 0, icon: icon, color: QueryColors.typeIcon)
                    .AddOrUpdatePropositionData(label: "Project", replacement: "scope:" + SearchUtils.GetListMarkerReplacementText("project", scopeValues, iconName, QueryColors.typeIcon), help: "Search project settings")
                    .AddOrUpdatePropositionData(label: "User", replacement: "scope:" + SearchUtils.GetListMarkerReplacementText("user", scopeValues, iconName, QueryColors.typeIcon), help: "Search user settings");

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
                fetchThumbnail = (item, context) => Icons.settings,
                fetchPropositions = (context, options) => FetchPropositions(context, options)
            };
        }

        static IEnumerator FetchItems(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;

            var query = SettingsProviderCache.queryEngine.ParseQuery(context.searchQuery);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                yield break;
            }

            yield return query.Apply(SettingsProviderCache.value).Select(spi => provider.CreateItem(context, spi.path, spi.label, spi.path, null, null));
        }

        static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                yield break;

            foreach (var p in QueryAndOrBlock.BuiltInQueryBuilderPropositions())
                yield return p;

            foreach (var f in SettingsProviderCache.queryEngine.GetPropositions())
                yield return f;
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
