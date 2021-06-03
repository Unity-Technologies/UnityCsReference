// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Search.Providers
{
    static class AdbProvider
    {
        public const string type = "adb";

        static ObjectQueryEngine<UnityEngine.Object> m_ResourcesQueryEngine;

        public static IEnumerable<string> EnumeratePaths(string searchQuery, SearchFlags flags)
        {
            var searchFilter = new SearchFilter
            {
                searchArea = GetSearchArea(flags),
                showAllHits = true,
                originalText = searchQuery
            };
            SearchUtility.ParseSearchString(searchQuery, searchFilter);
            return EnumeratePaths(searchFilter);
        }

        public static IEnumerable<string> EnumeratePaths(Type type, SearchFlags flags)
        {
            return EnumeratePaths(new SearchFilter
            {
                searchArea = GetSearchArea(flags),
                showAllHits = true,
                classNames = new[] { type.Name }
            });
        }


        static SearchFilter.SearchArea GetSearchArea(in SearchFlags searchFlags)
        {
            if (searchFlags.HasAny(SearchFlags.Packages))
                return SearchFilter.SearchArea.AllAssets;
            return SearchFilter.SearchArea.InAssetsOnly;
        }

        static IEnumerable<string> EnumeratePaths(SearchFilter searchFilter)
        {
            var rIt = AssetDatabase.EnumerateAllAssets(searchFilter);
            while (rIt.MoveNext())
            {
                if (rIt.Current.pptrValue)
                    yield return AssetDatabase.GetAssetPath(rIt.Current.instanceID);
            }
        }


        public static IEnumerable<string> EnumeratePaths(SearchContext context)
        {
            if (!string.IsNullOrEmpty(context.searchQuery))
                return EnumeratePaths(context.searchQuery, context.options);

            if (context.filterType != null)
                return EnumeratePaths(context.filterType, context.options);
            return Enumerable.Empty<string>();
        }

        static IEnumerable<SearchItem> FetchItems(SearchContext context, SearchProvider provider)
        {
            if (m_ResourcesQueryEngine == null)
                m_ResourcesQueryEngine = new ObjectQueryEngine();

            // Search asset database
            foreach (var path in EnumeratePaths(context))
                yield return AssetProvider.CreateItem("ADB", context, provider, null, path, 998, SearchDocumentFlags.Asset);

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

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, "Asset Database")
            {
                active = false,
                priority = 2500,
                fetchItems = (context, items, provider) => FetchItems(context, SearchService.GetProvider("asset") ?? provider)
            };
        }

        [MenuItem("Window/Search/Asset Database")] static void OpenProvider() => SearchService.ShowContextual(type);
        [ShortcutManagement.Shortcut("Help/Search/Asset Database")] static void OpenShortcut() => QuickSearch.OpenWithContextualProvider(type);
    }
}
