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
        public const string resourcesItemTag = "Resources";
        internal const string k_DefaultResources = "library/unity default resources";
        internal const string k_EditorResources = "library/unity editor resources";
        internal const string k_BuiltinExtraResources = "resources/unity_builtin_extra";
        static string[] s_ResourcePaths = new string[] {
             k_DefaultResources,
             k_EditorResources,
             k_BuiltinExtraResources
        };

        static ObjectQueryEngine<UnityEngine.Object> m_ResourcesQueryEngine;

        public static SearchFilter CreateSearchFilter(in string searchQuery, in Type filterType, in SearchFlags flags)
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
            return searchFilter;
        }

        public static IEnumerable<int> EnumerateInstanceIDs(in string searchQuery, in Type filterType, in SearchFlags flags)
        {
            var searchFilter = CreateSearchFilter(searchQuery, filterType, flags);
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

        static Dictionary<string, UnityEngine.Object[]> s_BundleResourceObjects = new Dictionary<string, UnityEngine.Object[]>();
        static IEnumerable<UnityEngine.Object> GetAllResourcesAtPath(in string path)
        {
            if (s_BundleResourceObjects.TryGetValue(path, out var objects))
                return objects;
            objects = AssetDatabase.LoadAllAssetsAtPath(path).ToArray();
            s_BundleResourceObjects[path] = objects;
            return objects;
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
                if (string.IsNullOrEmpty(path))
                    continue;
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(id).ToString();
                var flags = SearchDocumentFlags.Asset;
                if (AssetDatabase.IsSubAsset(id))
                {
                    var obj = UnityEngine.Object.FindObjectFromInstanceID(id);
                    var filename = Path.GetFileNameWithoutExtension(path);
                    path = Utils.RemoveInvalidCharsFromPath($"{filename}/", ' ');
                    flags |= SearchDocumentFlags.Nested;
                }
                // If this ever changes and we no longer use the AssetProvider to create items, please update the test SearchEngineTests.ProjectSearch_AlwaysReturnsPaths
                var item = AssetProvider.CreateItem("ADB", context, provider, context.filterType, gid, path, 998, flags);
                yield return item;
            }

            // Search builtin resources
            var resources = GetAllResourcesAtPath(k_DefaultResources)
                .Concat(GetAllResourcesAtPath(k_BuiltinExtraResources));
            if (context.wantsMore)
                resources = resources.Concat(GetAllResourcesAtPath(k_EditorResources));

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
                // If this ever changes and we no longer use the AssetProvider to create items, please update the test SearchEngineTests.ProjectSearch_AlwaysReturnsPaths
                yield return AssetProvider.CreateItem(resourcesItemTag, context, provider, gid.ToString(), null, 1998, SearchDocumentFlags.Resources);
            }
        }

        internal static bool IsResourcePath(string path)
        {
            foreach (var resourcePath in s_ResourcePaths)
            {
                if (path.Equals(resourcePath, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        static IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder))
                yield break;

            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryTypeBlock)))
                yield return f;
            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryLabelBlock)))
                yield return f;
            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryAreaFilterBlock)))
                yield return f;
            foreach (var f in QueryListBlockAttribute.GetPropositions(typeof(QueryBundleFilterBlock)))
                yield return f;

            yield return new SearchProposition(category: null, "Reference", "ref:<$object:none,UnityEngine.Object$>", "Find all assets referencing a specific asset.");
            yield return new SearchProposition(category: null, "Glob", "glob:\"Assets/**/*.png\"", "Search according to a glob query.");
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

        [MenuItem("Window/Search/Asset Database", priority = 1271)] static void OpenProvider() => SearchUtils.OpenWithContextualProviders(type);
        [ShortcutManagement.Shortcut("Help/Search/Asset Database")] static void OpenShortcut() => SearchUtils.OpenWithContextualProviders(type);
    }

    [QueryListBlock(null, "area", "a", ":")]
    class QueryAreaFilterBlock : QueryListBlock
    {
        public QueryAreaFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Filter Icon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            yield return CreateProposition(flags, "All", "all", "Search all", score: -99);
            yield return CreateProposition(flags, "Assets", "assets", "Search in Assets folder only", score: -98);
            yield return CreateProposition(flags, "Packages", "packages", "Search in packages only", score: -97);
        }
    }

    [QueryListBlock("Bundle", "bundle", "b", ":")]
    class QueryBundleFilterBlock : QueryListBlock
    {
        public QueryBundleFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = Utils.LoadIcon("Filter Icon");
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            var bundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var bundleName in bundleNames)
            {
                yield return CreateProposition(flags, bundleName, bundleName, $"Search inside bundle \"{bundleName}\"");
            }
        }
    }
}
