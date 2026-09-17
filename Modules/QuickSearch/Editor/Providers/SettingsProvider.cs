// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Search.Providers
{
    static partial class Settings
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

        static partial class SettingsProviderCache
        {
            internal partial class LazyInitStatics
            {
                readonly SettingsProviderInfo[] m_value;
                readonly QueryEngine<SettingsProviderInfo> m_queryEngine;

                public SettingsProviderInfo[] value { get => m_value; }
                public QueryEngine<SettingsProviderInfo> queryEngine { get => m_queryEngine; }

                public LazyInitStatics()
                {
                    #pragma warning disable UAC2001 // Avoid Linq
                    m_value = FetchSettingsProviders()
#pragma warning restore UAC2001
                        .Select(provider => new SettingsProviderInfo()
                    {
                        path = provider.settingsPath,
                        label = provider.label,
                        scope = provider.scope,
                        #pragma warning disable UAC2001 // Avoid Linq
                        searchables = new[] {provider.settingsPath, provider.label}
#pragma warning restore UAC2001
                            .Concat(provider.keywords)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => Utils.FastToLower(s)).ToArray()
                    })
                        .ToArray();

                    var iconName = "Filter Icon";
                    var icon = Utils.LoadIcon(iconName);
                    #pragma warning disable UAC2001 // Avoid Linq
                    var scopeValues = Enum.GetNames(typeof(SettingsScope)).Select(n => Utils.FastToLower(n));
#pragma warning restore UAC2001
                    m_queryEngine = new QueryEngine<SettingsProviderInfo>();
                    m_queryEngine.SetSearchDataCallback(info => info.searchables, s => Utils.FastToLower(s), StringComparison.Ordinal);
                    m_queryEngine.SetFilter("scope", info => info.scope, new[] { ":", "=", "!=", "<", ">", "<=", ">=" })
                        .SetGlobalPropositionData(category: "Scope", priority: 0, icon: icon, color: QueryColors.typeIcon)
                        .AddOrUpdatePropositionData(label: "Project", replacement: "scope=" + SearchUtils.GetListMarkerReplacementText("project", scopeValues, iconName, QueryColors.typeIcon), help: "Search project settings")
                        .AddOrUpdatePropositionData(label: "User", replacement: "scope=" + SearchUtils.GetListMarkerReplacementText("user", scopeValues, iconName, QueryColors.typeIcon), help: "Search user settings");

                    m_queryEngine.AddOperatorHandler(":", (SettingsScope ev, SettingsScope fv, StringComparison sc) => ev.ToString().IndexOf(fv.ToString(), sc) != -1);
                    m_queryEngine.AddOperatorHandler(":", (SettingsScope ev, string fv, StringComparison sc) => ev.ToString().IndexOf(fv, sc) != -1);
                    m_queryEngine.AddOperatorHandler("=", (SettingsScope ev, SettingsScope fv) => ev == fv);
                    m_queryEngine.AddOperatorHandler("!=", (SettingsScope ev, SettingsScope fv) => ev != fv);
                    m_queryEngine.AddOperatorHandler("<", (SettingsScope ev, SettingsScope fv) => ev < fv);
                    m_queryEngine.AddOperatorHandler(">", (SettingsScope ev, SettingsScope fv) => ev > fv);
                    m_queryEngine.AddOperatorHandler("<=", (SettingsScope ev, SettingsScope fv) => ev <= fv);
                    m_queryEngine.AddOperatorHandler(">=", (SettingsScope ev, SettingsScope fv) => ev >= fv);
                }
            }

            [AutoStaticsCleanupOnCodeReload]
            private static Lazy<LazyInitStatics> s_LazyInitStatics = new(() => new LazyInitStatics());

            public static SettingsProviderInfo[] value { get => s_LazyInitStatics.Value.value; }
            public static QueryEngine<SettingsProviderInfo> queryEngine { get => s_LazyInitStatics.Value.queryEngine; }

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
                fetchLabel = (item, context) => item.label ?? (item.label = Utils.GetFileName(item.id)),
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
                #pragma warning disable UAC2001 // Avoid Linq
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
#pragma warning restore UAC2001
                yield break;
            }

            #pragma warning disable UAC2001 // Avoid Linq
            yield return query.Apply(SettingsProviderCache.value).Select(spi => provider.CreateItem(context, spi.path, spi.label, spi.path, null, null));
#pragma warning restore UAC2001
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
                    var item = items[^1];
                    if (item.id.StartsWith("Project/"))
                        SettingsService.OpenProjectSettings(item.id);
                    else
                        SettingsService.OpenUserPreferences(item.id);
                })
            };
        }
    }
}
