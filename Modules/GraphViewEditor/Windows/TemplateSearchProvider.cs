// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEditor.Search;

namespace UnityEditor.Experimental.GraphView
{
    class TemplateSearchProvider : SearchProvider
    {
        private const string kProviderId = "template";
        private string m_HiddenSearchQuery;
        private ITemplateHelper m_TemplateHelper;
        private bool m_AdbOnly = false;

        public const string kUncategorized = "Uncategorized";

        public TemplateSearchProvider(ITemplateHelper templateHelper, string hiddenSearchQuery, bool adbOnly) : base(kProviderId)
        {
            m_TemplateHelper= templateHelper;
            m_HiddenSearchQuery = hiddenSearchQuery;
            m_AdbOnly = adbOnly;
            fetchItems = (context, items, provider) => SearchItems(context, provider);
            toObject = (item, _) => item.ToObject();
            fetchPropositions = FetchPropositions;
        }

        public bool IsSearching { get; private set; }

        IEnumerable<SearchItem> SearchItems(SearchContext context, SearchProvider provider)
        {
            var searchQuery = context.searchQuery;
            IsSearching = true;
            // Used in testing to avoid triggering indexing: the switch *adbonly* could also be passed manually to ensure we do do an unindexed search
            var adbOnlyQuery = m_AdbOnly || context.searchQuery.Contains("*adbonly*");
            if (adbOnlyQuery)
            {
                searchQuery = searchQuery.Replace("*adbonly*", "");
            }

            // We can used asset provider only when the indexing is complete
            var isIndexingComplete = true;
            if (!adbOnlyQuery)
            {
                var sw = new System.Diagnostics.Stopwatch();
                isIndexingComplete = IsIndexingComplete();
                while (!isIndexingComplete && sw.Elapsed.TotalSeconds < 5)
                {
                    yield return null;
                    isIndexingComplete = IsIndexingComplete();
                }

                sw.Stop();
            }

            var canUseAssetProvider = !adbOnlyQuery && isIndexingComplete;
            var defaultQuery = $"t:{m_TemplateHelper.assetType.Name}";

            if (!string.IsNullOrEmpty(searchQuery))
            {
                // ADB doesn't support ( ) or boolean operator
                defaultQuery += $" {searchQuery}";
            }

            if (!string.IsNullOrEmpty(m_HiddenSearchQuery))
            {
                if (canUseAssetProvider)
                    defaultQuery = $"({m_HiddenSearchQuery}) and ({defaultQuery})";
                else
                {
                    // ADB doesn't support ( ) or boolean operator
                    defaultQuery += $" {m_HiddenSearchQuery}";
                }
            }

            // ADB provider is always available, but does not provide search and filter capabilities
            var providerIds = canUseAssetProvider ? new [] { "adb", "asset" } : new [] { "adb" };
            using var assetContext = Search.SearchService.CreateContext(providerIds, defaultQuery, SearchFlags.Packages);
            assetContext.useExplicitProvidersAsNormalProviders = true;
            using var request = Search.SearchService.Request(assetContext);
            foreach (var item in request)
            {
                if (item == null)
                    yield return null;
                else
                {
                    yield return item;
                }
            }

            IsSearching = false;
        }

        // Note: this is a way to open the search window with only your provider.
        /* [MenuItem("Template/Search Template")]
        static void SearchTemplate()
        {
            var assetContext = Search.SearchService.CreateContext(new[] { "template" }, "t:VisualEffectAsset", SearchFlags.Packages);
            Search.SearchService.ShowWindow(assetContext);
        }
        */

        // Note: this is a way to register your provider so it is available in the SearchWindow. It can help debug some workflows.
        /* [SearchItemProvider]
        static SearchProvider CreateTemplateProvider()
        {
            var type = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => t.FullName == "UnityEngine.VFX.VisualEffectAsset");
            return new TemplateSearchProvider(type, string.Empty);
        }
        */

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            var searchIcon = Search.Utils.LoadIcon("QuickSearch/SearchWindow");

            yield return new SearchProposition(category: "Area", label: "Only Assets", replacement: "a:assets", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: searchIcon, color: QueryColors.area);
            yield return new SearchProposition(category: "Area", label: "Only Packages", replacement: "a:packages", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: searchIcon, color: QueryColors.area);

            foreach (var category in GetPropositions(m_TemplateHelper.assetType, m_TemplateHelper.toolKey.ToLowerInvariant()))
                yield return category;
            foreach (var prop in QueryAndOrBlock.BuiltInQueryBuilderPropositions())
                yield return prop;
            foreach (var prop in m_TemplateHelper.GetSearchPropositions())
                yield return prop;
        }

        private static IEnumerable<SearchProposition> GetPropositions(Type assetType, string toolKey)
        {
            var assetIcon = AssetPreview.GetMiniTypeThumbnailFromType(assetType);
            var labelIcon = Search.Utils.LoadIcon("QuickSearch/AssetLabelIconSquare");
            var dbs = SearchDatabase.EnumerateAll();
            var categories = new List<string>();
            var labels = new List<string>();
            var customProposition = new Dictionary<string, HashSet<string>>();
            var categoryKey = $"{toolKey}.category:";
            var labelKey = $"{toolKey}.label:";
            var nameKey = $"{toolKey}.name:";
            var customKey = $"{toolKey}.";
            var toolKeyLength = toolKey.Length + 1; // +1 for the dot

            foreach (var db in dbs)
            {
                if (!db.loaded || db.settings.options.disabled)
                    continue;
                foreach (var kw in db.index.GetKeywords())
                {
                    if (kw.StartsWith(categoryKey))
                    {
                        var category = kw.Replace(categoryKey, string.Empty);
                        if (!string.IsNullOrEmpty(category) && !category.StartsWith('|'))
                            categories.Add(category);
                    }
                    else if (kw.StartsWith(labelKey))
                    {
                        var label = kw.Replace(labelKey, string.Empty);
                        if (!string.IsNullOrEmpty(label) && !label.StartsWith('|'))
                            labels.Add(label);
                    }
                    else if (kw.StartsWith(nameKey))
                        continue;
                    else if (kw.StartsWith(customKey))
                    {
                        var tokens = kw.Split(":");
                        if (tokens.Length == 2)
                        {
                            if (string.IsNullOrEmpty(tokens[1]) || tokens[1].StartsWith('|'))
                                continue;

                            var key = tokens[0].Substring(toolKeyLength);
                            if (customProposition.TryGetValue(key, out var list))
                            {
                                list.Add(tokens[1]);
                            }
                            else
                            {
                                customProposition[key] = new HashSet<string> { tokens[1] };
                            }
                        }
                    }
                }
            }

            categoryKey = categoryKey.Replace(':', '=');
            var sb = new StringBuilder();
            foreach (var category in categories)
            {
                sb.Append($"\"{category}\", ");
            }
            var allCategories = sb.ToString().TrimEnd(new [] {',', ' '});
            foreach (var category in categories)
            {
                yield return new SearchProposition(category: "Category", label:category, replacement: $"{categoryKey}<$list:\"{category}\", [{allCategories}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: assetIcon, color: QueryColors.property);
            }

            foreach (var label in labels)
            {
                yield return new SearchProposition(category: "Labels", label:label, replacement: $"l:{label}", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: labelIcon, color: QueryColors.word);
            }
            foreach (var kvp in customProposition)
            {
                sb.Clear();
                foreach (var v in kvp.Value)
                {
                    sb.Append($"\"{v}\", ");
                }
                var allValues = sb.ToString().TrimEnd(new [] {',', ' '});
                foreach (var v in kvp.Value)
                {
                    yield return new SearchProposition(category: kvp.Key, label:v, replacement: $"{toolKey}.{kvp.Key}:<$list:\"{v}\", [{allValues}]$>", moveCursor: TextCursorPlacement.MoveAutoComplete, icon: assetIcon, color: QueryColors.property);
                }
            }
        }

        private bool IsIndexingComplete() => SearchDatabase.GetDefaultSearchDatabase().ready;
    }
}
