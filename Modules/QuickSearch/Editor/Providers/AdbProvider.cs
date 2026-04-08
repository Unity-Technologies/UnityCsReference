// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    static class AdbProvider
    {
        public const string type = "adb";
        public const string filterId = "adb:";

        // Used with context.searchWords.Contains so the + is necessary.
        public const string implicitToggle = "+implicit";
        public const string explicitToggle = "+explicit";
        // Used with HasToggle: + must not be there.
        public const string showAllHitsToggle = "showallhits";
        public const string typeIntersectionToggle = "typeintersection";
        public const string typeUnionToggle = "typeunion";

        public const string resourcesItemTag = "Resources";
        internal const string k_DefaultResources = "library/unity default resources";
        internal const string k_EditorResources = "library/unity editor resources";
        internal const string k_BuiltinExtraResources = "resources/unity_builtin_extra";
        static string[] s_ResourcePaths = new string[] {
             k_DefaultResources,
             k_EditorResources,
             k_BuiltinExtraResources
        };

        static QueryEngine<UnityEngine.Object> m_AdbExplicitQueryEngine;
        static QueryEngine<UnityEngine.Object> m_AdbImplicitQueryEngine;
        static ObjectQueryEngine<UnityEngine.Object> m_ResourcesQueryEngine;

        static readonly string[] k_Operators = new [] {":", "="};

        internal static QueryEngine<UnityEngine.Object> adbExplicitQueryEngine
        {
            get
            {
                if (m_AdbExplicitQueryEngine == null)
                    InitQueryEngine();
                return m_AdbExplicitQueryEngine;
            }
        }

        internal static QueryEngine<UnityEngine.Object> adbImplicitQueryEngine
        {
            get
            {
                if (m_AdbImplicitQueryEngine == null)
                    InitImplicitQueryEngine();
                return m_AdbImplicitQueryEngine;
            }
        }

        internal static ObjectQueryEngine<UnityEngine.Object> resourcesQueryEngine
        {
            get
            {
                return m_ResourcesQueryEngine ??= new ObjectQueryEngine() { reportError = false };
            }
        }

        internal static bool ValidateAdbExplicitQuery(string query)
        {
            var parsedQuery = adbExplicitQueryEngine.ParseQuery(query);
            return parsedQuery.valid;
        }

        internal static bool ValidateAdbImplicitQuery(string query)
        {
            var parsedQuery = adbImplicitQueryEngine.ParseQuery(query);
            return parsedQuery.valid;
        }

        internal static bool IsExplicitQuery(SearchContext context)
        {
            return (context.searchQuery.StartsWith(filterId) || context.providers.Count() == 1 || context.searchWords.Contains(explicitToggle)) && !context.searchWords.Contains(implicitToggle);
        }

        internal static string ConvertProjectQueryToAdb(string query, ref bool filterByTypeIntersection)
        {
            var parsedQuery = UnityEditor.Search.Providers.AdbProvider.adbExplicitQueryEngine.ParseQuery(query);
            return ConvertProjectQueryToAdb(parsedQuery, ref filterByTypeIntersection);
        }

        internal static string ConvertProjectQueryToAdb(ParsedQuery<UnityEngine.Object> query, ref bool filterByTypeIntersection)
        {
            if (!query.valid)
                return null;

            var rootNode = query.evaluationGraph.root;
            var queryStr = new StringBuilder();
            var conversionSuccess = ConvertProjectQueryToAdb(rootNode, queryStr, ref filterByTypeIntersection);
            return conversionSuccess ? queryStr.ToString() : null;
        }

        internal static bool ConvertProjectQueryToAdb(IQueryNode node, StringBuilder query, ref bool filterByTypeIntersection)
        {
            bool IsTypeNode(IQueryNode node)
            {
                if (node is FilterNode filter)
                    return filter.filterId == "t";
                return false;
            }

            bool IsValidAndChildNode(IQueryNode node)
            {
                if (node.leaf)
                    return node.type == QueryNodeType.Search || node.type == QueryNodeType.Filter;
                return true;
            }

            bool IsValidOrChildNode(IQueryNode node)
            {
                if (node.leaf)
                {
                    return IsTypeNode(node);
                }

                return true;
            }

            if (node == null)
                return false;

            if (node.leaf)
            {
                switch (node.type)
                {
                    case QueryNodeType.Filter:
                    case QueryNodeType.Search:
                        if (query.Length > 0)
                        {
                            query.Append(" ");
                        }
                        query.Append(node.token.text);
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                switch (node.type)
                {
                    case QueryNodeType.And:
                        if (node.children.Count == 2 &&
                            IsValidAndChildNode(node.children[0]) &&
                            IsValidAndChildNode(node.children[1]))
                        {
                            break;
                        }

                        return false;
                    case QueryNodeType.Or:
                        if (node.children.Count == 2 && IsValidOrChildNode(node.children[0]) && IsValidOrChildNode(node.children[1]))
                        {
                            //We have a valid OR between types. Query needs to be processed with implicit or.
                            filterByTypeIntersection = false;
                            break;
                        }
                        return false;
                    case QueryNodeType.Where:
                        break;
                    default:
                        return false;
                }
            }

            if (!node.leaf && node.children.Count > 0)
            {
                foreach (var c in node.children)
                {
                    if (!ConvertProjectQueryToAdb(c, query, ref filterByTypeIntersection))
                        return false;
                }
            }

            return true;
        }

        static IEnumerable<string> GetWords(UnityEngine.Object obj)
        {
            yield return obj.name;
        }

        internal static void InitQueryEngine()
        {
            var options = new QueryValidationOptions() { validateFilters = true };
            m_AdbExplicitQueryEngine = new QueryEngine<UnityEngine.Object>(options);
            // Type
            m_AdbExplicitQueryEngine.AddFilter("t", k_Operators);
            // Label
            m_AdbExplicitQueryEngine.AddFilter("l", k_Operators);
            // Area
            m_AdbExplicitQueryEngine.AddFilter("a", k_Operators);
            // Bundle
            m_AdbExplicitQueryEngine.AddFilter("b", k_Operators);
            // Import log
            m_AdbExplicitQueryEngine.AddFilter("i", k_Operators);
            // reference to instanceID
            m_AdbExplicitQueryEngine.AddFilter("ref", k_Operators);
            // Glob search
            m_AdbExplicitQueryEngine.AddFilter("glob", k_Operators);
            m_AdbExplicitQueryEngine.SetSearchDataCallback(GetWords);
        }

        internal static void InitImplicitQueryEngine()
        {
            var options = new QueryValidationOptions() { validateFilters = true };
            m_AdbImplicitQueryEngine = new QueryEngine<UnityEngine.Object>(options);
            m_AdbImplicitQueryEngine.AddFilter("t", k_Operators);
            m_AdbImplicitQueryEngine.AddFilter("l", k_Operators);
            m_AdbImplicitQueryEngine.AddFilter("a", k_Operators);
            m_AdbImplicitQueryEngine.SetSearchDataCallback(GetWords);
        }

        internal static SearchFilter CreateSearchFilter(string searchQuery, SearchContext context)
        {
            if (context.userData is SearchFilter legacySearchFilter)
            {
                return legacySearchFilter;
            }

            var flags = context.options;
            var filterType = context.filterType;
            var searchFilter = new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.SelectedFolders, // Init to this value to see if the query itself contains an override for the area.
                originalText = searchQuery
            };

            if (!string.IsNullOrEmpty(searchQuery))
            {
                SearchUtility.ParseSearchString(searchQuery, searchFilter);
            }

            // The query doesn't contain any area overrides: apply the are found in the context
            if (searchFilter.searchArea == SearchFilter.SearchArea.SelectedFolders)
            {
                searchFilter.searchArea = flags.HasAny(SearchFlags.Packages) ? SearchFilter.SearchArea.AllAssets : SearchFilter.SearchArea.InAssetsOnly;
            }

            if (filterType != null && searchFilter.classNames.Length == 0)
                searchFilter.classNames = new [] { filterType.Name };

            return searchFilter;
        }

        static SearchFilter.SearchArea GetSearchArea(in SearchFlags searchFlags)
        {
            if (searchFlags.HasAny(SearchFlags.Packages))
                return SearchFilter.SearchArea.AllAssets;
            return SearchFilter.SearchArea.InAssetsOnly;
        }

        static IEnumerable<int> EnumerateInstanceIDs(SearchFilter searchFilter)
        {
            if (searchFilter == null || searchFilter.GetState() == SearchFilter.State.EmptySearchFilter)
                yield break;

            var rIt = AssetDatabase.EnumerateAllAssets(searchFilter);
            while (rIt.MoveNext())
            {
                yield return rIt.Current.entityId;
            }
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

            ParsedQuery<UnityEngine.Object> parsedQuery;
            if (IsExplicitQuery(context))
            {
                // This is an explicit query on the provider, used the full query engine
                parsedQuery = adbExplicitQueryEngine.ParseQuery(context.searchQuery);
            }
            else
            {
                // This is an implicit query that hits the ADBProvider because useExplicitProvider is true. Only support subset that is compatible with asset provider.
                parsedQuery = adbImplicitQueryEngine.ParseQuery(context.searchQuery);
            }

            if (parsedQuery == null)
                yield break;

            if (!parsedQuery.valid)
            {
                context.AddSearchQueryErrors(parsedQuery.errors.Select(e => new SearchQueryError(e, context, provider)));
                yield break;
            }

            // Convert OR with type, get rid of toggles.
            var filterByTypeIntersection = true;
            var searchQuery = ConvertProjectQueryToAdb(parsedQuery, ref filterByTypeIntersection);
            if (searchQuery == null)
                yield break;

            // Search asset database
            var searchFilter = CreateSearchFilter(searchQuery, context);
            if (searchFilter == null)
                yield break;

            // Note: by default ADB search assumes all type searching uses a boolean implicit OR: t:Texture t:AudioClip => t:Texture OR t:AudioClip
            // This switch make the ADB search use an explicit AND instead: t:Texture t:AudioClip => t:Texture AND t:AudioClip
            searchFilter.filterByTypeIntersection = (filterByTypeIntersection || parsedQuery.HasToggle(typeIntersectionToggle)) && !parsedQuery.HasToggle(typeUnionToggle);
            searchFilter.showAllHits = searchFilter.showAllHits || parsedQuery.HasToggle(showAllHitsToggle);

            foreach (var id in EnumerateInstanceIDs(searchFilter))
            {
                var path = AssetDatabase.GetAssetPath((EntityId)id);
                if (string.IsNullOrEmpty(path))
                    continue;
                var gid = GlobalObjectId.GetGlobalObjectIdSlow((EntityId)id).ToString();
                var flags = SearchDocumentFlags.Asset;
                if (AssetDatabase.IsSubAsset((EntityId)id))
                {
                    var obj = UnityEngine.Object.FindObjectFromInstanceID(id);
                    var filename = Path.GetFileNameWithoutExtension(path);
                    path = Utils.ReplaceInvalidCharsFromPath($"{filename}/", ' ');
                    flags |= SearchDocumentFlags.Nested;
                }
                // If this ever changes and we no longer use the AssetProvider to create items, please update the test SearchEngineTests.ProjectSearch_AlwaysReturnsPaths
                var item = AssetProvider.CreateItem("ADB", context, provider, context.filterType, gid, path, 998, flags);
                yield return item;
            }

            // Builtin extra are always searched (similar to Legacy picker)
            var resources = GetAllResourcesAtPath(k_BuiltinExtraResources);
            // Add editorResources and defaultResources if needed.
            if (context.wantsMore)
                resources = resources.Concat(GetAllResourcesAtPath(k_EditorResources)).Concat(GetAllResourcesAtPath(k_DefaultResources));

            if (context.filterType != null)
                resources = resources.Where(r => context.filterType.IsAssignableFrom(r.GetType()));

            if (!string.IsNullOrEmpty(searchQuery))
                resources = resourcesQueryEngine.Search(searchQuery, context, provider, resources);
            else if (context.filterType == null)
                yield break;

            foreach (var obj in resources)
            {
                if (!obj)
                {
                    yield return null;
                    continue;
                }

                var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                if (gid.identifierType == 0)
                {
                    yield return null;
                    continue;
                }

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

            yield return new SearchProposition(category: null, "Reference", "ref=<$object:none,UnityEngine.Object$>", "Find all assets referencing a specific asset.");
            yield return new SearchProposition(category: null, "Glob", "glob=\"Assets/**/*.png\"", "Search according to a glob query.");
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, "Asset Database")
            {
                filterId = filterId,
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

    [QueryListBlock("Bundle", "bundle", "b")]
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
                var bundleStr = SearchUtils.EscapeLiteralString(bundleName);
                yield return CreateProposition(flags, bundleName, bundleStr, $"Search inside bundle {bundleStr}");
            }
        }
    }
}
