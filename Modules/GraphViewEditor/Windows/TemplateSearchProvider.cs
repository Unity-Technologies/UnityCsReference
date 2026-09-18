// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEditor.Search;
using UnityEditor.Search.Providers;

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

            var packagesIndexed = SearchDatabase.GetDefaultSearchDatabase()?.settings.IsPackagesIndexingEnabled() ?? false;
            var canUseAssetProvider = !adbOnlyQuery && packagesIndexed && IsIndexingComplete();
            var defaultQuery = $"t:{m_TemplateHelper.assetType.Name}";

            if (canUseAssetProvider)
            {
                if (!string.IsNullOrEmpty(searchQuery))
                    defaultQuery += $" {searchQuery}";
                if (!string.IsNullOrEmpty(m_HiddenSearchQuery))
                    defaultQuery = $"({m_HiddenSearchQuery}) and ({defaultQuery})";

                using var assetContext = Search.SearchService.CreateContext(new [] { "adb", "asset" }, defaultQuery, SearchFlags.Packages);
                assetContext.useExplicitProvidersAsNormalProviders = true;
                using var request = Search.SearchService.Request(assetContext);
                foreach (var item in request)
                    yield return item;

                IsSearching = false;
                yield break;
            }

            // QuickSearch's index isn't ready, so match the templates ourselves instead of querying it.
            var toolKey = m_TemplateHelper.toolKey.ToLowerInvariant();
            var hiddenMatcher = TemplateQueryMatcher.Parse(m_HiddenSearchQuery, toolKey);
            var userMatcher = TemplateQueryMatcher.Parse(searchQuery, toolKey);
            var matchesEverything = hiddenMatcher.IsMatchAll && userMatcher.IsMatchAll;

            using (var assetContext = Search.SearchService.CreateContext(new [] { "adb" }, defaultQuery, SearchFlags.Packages))
            {
                assetContext.useExplicitProvidersAsNormalProviders = true;
                using var request = Search.SearchService.Request(assetContext);
                foreach (var item in request)
                {
                    if (item == null)
                    {
                        yield return null;
                        continue;
                    }

                    if (item.data is not AssetProvider.AssetMetaInfo meta)
                        continue;

                    var path = AssetDatabase.GUIDToAssetPath(meta.guid);
                    if (string.IsNullOrEmpty(path))
                        continue;

                    // area in pre-filter means that a search shouldn't reach outside the area
                    if (!MatchesArea(path, hiddenMatcher.Area) || !MatchesArea(path, userMatcher.Area))
                        continue;

                    // Skips non-templates and gives us the template's search terms.
                    if (!m_TemplateHelper.TryGetTemplate(path, out var descriptor))
                        continue;

                    if (matchesEverything)
                    {
                        yield return item;
                        continue;
                    }

                    var labels = AssetDatabase.GetLabels(new GUID(meta.guid));
                    var document = GraphViewIndexerExtension.BuildSearchDocument(descriptor, labels);
                    if (hiddenMatcher.Matches(document) && userMatcher.Matches(document))
                        yield return item;
                }
            }

            IsSearching = false;
        }

        // Options a:asset, a:packages, a:all or none
        internal static bool MatchesArea(string assetPath, string area)
        {
            if (string.IsNullOrEmpty(area))
                return true;
            if (area == "assets")
                return assetPath.StartsWith("Assets/", StringComparison.Ordinal);
            if (area == "packages")
                return assetPath.StartsWith("Packages/", StringComparison.Ordinal);
            return true;
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

        private bool IsIndexingComplete()
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            return db != null && db.ready && !db.updating;
        }
    }
}
