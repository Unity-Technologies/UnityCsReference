// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor.Search.Providers
{
    static class AdbProvider
    {
        public const string type = "adb";

        static ObjectQueryEngine<UnityEngine.Object> m_ResourcesQueryEngine;

        public static IEnumerable<int> EnumerateInstanceIDs(in string searchQuery, in Type filterType, in SearchFlags flags)
        {
            var searchFilter = new SearchFilter
            {
                searchArea = GetSearchArea(flags),
                showAllHits = flags.HasAny(SearchFlags.WantsMore),
                originalText = searchQuery
            };
            if (!string.IsNullOrEmpty(searchQuery))
                SearchUtility.ParseSearchString(searchQuery, searchFilter);
            if (filterType != null && searchFilter.classNames.Length == 0)
                searchFilter.classNames = new[] { filterType.Name };
            searchFilter.filterByTypeIntersection = true;
            return EnumerateInstanceIDs(searchFilter);
        }


        static SearchFilter.SearchArea GetSearchArea(in SearchFlags searchFlags)
        {
            if (searchFlags.HasAny(SearchFlags.Packages))
                return SearchFilter.SearchArea.AllAssets;
            return SearchFilter.SearchArea.InAssetsOnly;
        }

        static IEnumerable<int> EnumerateInstanceIDs(SearchFilter searchFilter)
        {
            var rIt = AssetDatabase.EnumerateAllAssets(searchFilter);
            while (rIt.MoveNext())
            {
                if (rIt.Current.pptrValue)
                    yield return rIt.Current.instanceID;
            }
        }


        public static IEnumerable<int> EnumerateInstanceIDs(SearchContext context)
        {
            if (context.filterType == null && context.empty)
                return Enumerable.Empty<int>();
            if (context.userData is SearchFilter legacySearchFilter)
            {
                legacySearchFilter.filterByTypeIntersection = true;
                return EnumerateInstanceIDs(legacySearchFilter);
            }
            return EnumerateInstanceIDs(context.searchQuery, context.filterType, context.options);
        }

        public static IEnumerable<string> EnumeratePaths(SearchContext context)
        {
            foreach (var id in EnumerateInstanceIDs(context))
                yield return AssetDatabase.GetAssetPath(id);
        }

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery) && context.filterType == null)
                yield break;

            if (m_ResourcesQueryEngine == null)
            {
                m_ResourcesQueryEngine = new ObjectQueryEngine()
                {
                    reportError = false
                };
            }

            // Search asset database
            foreach (var id in EnumerateInstanceIDs(context))
            {
                var path = AssetDatabase.GetAssetPath(id);
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(id).ToString();
                string label = null;
                var flags = SearchDocumentFlags.Asset;
                if (AssetDatabase.IsSubAsset(id))
                {
                    var obj = UnityEngine.Object.FindObjectFromInstanceID(id);
                    var filename = Path.GetFileNameWithoutExtension(path);
                    label = obj?.name ?? filename;
                    path = Utils.RemoveInvalidCharsFromPath($"{filename}/{label}", ' ');
                    flags |= SearchDocumentFlags.Nested;
                }
                var item = AssetProvider.CreateItem("ADB", context, provider, context.filterType, gid, path, 998, flags);
                if (label != null)
                {
                    item.label = label;
                }
                yield return item;
            }

            // Search builtin resources
            var resources = AssetDatabase.LoadAllAssetsAtPath("library/unity default resources")
                .Concat(AssetDatabase.LoadAllAssetsAtPath("resources/unity_builtin_extra"));
            if (context.wantsMore)
                resources = resources.Concat(AssetDatabase.LoadAllAssetsAtPath("library/unity editor resources"));

            if (context.filterType != null)
                resources = resources.Where(r => context.filterType.IsAssignableFrom(r.GetType()));

            if (!string.IsNullOrEmpty(context.searchQuery))
                resources = m_ResourcesQueryEngine.Search(context, provider, resources);
            else if (context.filterType == null)
                yield break;

            foreach (var obj in resources)
            {
                if (!obj)
                    continue;
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (gid.identifierType == 0)
                    continue;
                yield return AssetProvider.CreateItem("Resources", context, provider, gid.ToString(), null, 1998, SearchDocumentFlags.Resources);
            }
        }

        static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                yield break;

        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, "Asset Database")
            {
                type = "asset",
                active = false,
                priority = 2500,
                fetchItems = (context, items, provider) => FetchItems(context, SearchService.GetProvider("asset") ?? provider),
                fetchPropositions = (context, options) => FetchPropositions(context, options)
            };
        }

        [MenuItem("Window/Search/Asset Database", priority = 1271)] static void OpenProvider() => SearchService.ShowContextual(type);
        [ShortcutManagement.Shortcut("Help/Search/Asset Database")] static void OpenShortcut() => QuickSearch.OpenWithContextualProvider(type);
    }

}
