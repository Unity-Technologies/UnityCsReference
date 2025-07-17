// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    [Serializable]
    class SearchQueryTreeConfig
    {
        internal const string ProjectQueriesFolder = "Assets";
        internal const string BuiltInQueriesFolder = "searchqueries";

        internal const string UserQueriesLabel = "User";
        internal const string ProjectQueriesLabel = "Project";
        internal const string BuiltinQueriesLabel = "Builtins";

        [SerializeField] internal SearchFunctor<Func<ISearchQueryNodeHandler>>[] NodeSources;

        public SearchQueryTreeConfig(params Func<ISearchQueryNodeHandler>[] nodeSources)
        {
            NodeSources = new SearchFunctor<Func<ISearchQueryNodeHandler>>[nodeSources.Length];
            for (int i = 0; i < nodeSources.Length; ++i)
            {
                NodeSources[i] = new SearchFunctor<Func<ISearchQueryNodeHandler>>(nodeSources[i]);
            }
        }

        public static ISearchQueryNodeHandler DefaultUserQueryTreeNodeHandlerCreator()
        {
            return new UserSearchQueryNodeHandler();
        }

        public static ISearchQueryNodeHandler DefaultProjectQueryTreeNodeHandlerCreator()
        {
            return new ProjectSearchQueryTreeNodeHandler(isEditorResources: false, ProjectQueriesFolder, ProjectQueriesLabel);
        }

        public static ISearchQueryNodeHandler DefaultResourceQueryTreeNodeHandlerCreator()
        {
            return new ProjectSearchQueryTreeNodeHandler(isEditorResources: true, BuiltInQueriesFolder, BuiltinQueriesLabel);
        }

        public static SearchQueryTreeConfig CreateDefault()
        {
            return new SearchQueryTreeConfig(
                DefaultUserQueryTreeNodeHandlerCreator,
                DefaultProjectQueryTreeNodeHandlerCreator,
                DefaultResourceQueryTreeNodeHandlerCreator);
        }
    }

    static class SearchQueryPanelTreeUtils
    {
        internal static T GetFirstElement<T>(IEnumerable<T> collection)
        {
            var enumerator = collection.GetEnumerator();

            if (enumerator.MoveNext())
                return enumerator.Current;

            throw new InvalidOperationException("The collection is empty.");
        }

        internal static int GetTreeId(this ISearchQuery query)
        {
            return HashingUtils.GetHashCode(query.guid);
        }

        internal static bool IsQueryNameMatchingFilter(string queryFilter, string queryName)
        {
            return string.IsNullOrEmpty(queryFilter) || queryName.IndexOf(queryFilter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static ISearchQuery[] ParseQueries(ISearchEvent evt)
        {
            var argCount = evt.argumentCount;
            if (argCount == 0)
                return Array.Empty<ISearchQuery>();

            var evtArg = evt.GetArgument<object>(0);
            return evtArg switch
            {
                IList<SearchQueryAsset> queryAssets => ConvertToISearchQueryArray(queryAssets),
                IList<ISearchQuery> queries => ConvertToISearchQueryArray(queries),
                ISearchQuery query => new[] { query },
                _ => new ISearchQuery[argCount]
            };
        }

        internal static string[] ParseQueryPaths(ISearchEvent evt)
        {
            var argCount = evt.argumentCount;
            if (argCount == 0)
                return Array.Empty<string>();

            var evtArg = evt.GetArgument<object>(0);
            switch (argCount)
            {
                case 1 when evtArg is IList<SearchQueryAsset> queryAssets:
                {
                    var queryPaths = new string[queryAssets.Count];
                    for (int i = 0; i < queryAssets.Count; i++)
                    {
                        queryPaths[i] = queryAssets[i].filePath;
                    }

                    return queryPaths;
                }
                case >= 1 when evtArg is SearchQueryAsset:
                {
                    var queryPaths = new string[argCount];
                    for (var i = 0; i < argCount; ++i)
                        queryPaths[i] = evt.GetArgument<SearchQueryAsset>(i).filePath;

                    return queryPaths;
                }
                case 1 when evtArg is string[] paths:
                    return paths;
                case >= 1 when evtArg is string:
                {
                    var queryPaths = new string[argCount];
                    for (var i = 0; i < argCount; ++i)
                        queryPaths[i] = evt.GetArgument<string>(i);
                    return queryPaths;
                }
                default:
                    return new string[argCount];
            }
        }

        internal static string[] ParseQueryIds(ISearchEvent evt)
        {
            var argCount = evt.argumentCount;
            if (argCount == 0)
                return Array.Empty<string>();

            var evtArg = evt.GetArgument<object>(0);
            return evtArg switch
            {
                IList<ISearchQuery> queries => ConvertToQueryGuidArray(queries),
                ISearchQuery query => new[] { query.guid },
                string[] queryIds => queryIds,
                string queryId => new[] { queryId },
                _ => new string[argCount]
            };
        }

        static ISearchQuery[] ConvertToISearchQueryArray(IList<ISearchQuery> queries)
        {
            var result = new ISearchQuery[queries.Count];
            for (int i = 0; i < queries.Count; i++)
            {
                result[i] = queries[i];
            }
            return result;
        }

        static ISearchQuery[] ConvertToISearchQueryArray(IList<SearchQueryAsset> queryAssets)
        {
            var result = new ISearchQuery[queryAssets.Count];
            for (int i = 0; i < queryAssets.Count; i++)
            {
                result[i] = queryAssets[i];
            }
            return result;
        }

        static string[] ConvertToQueryGuidArray(IList<ISearchQuery> queries)
        {
            var result = new string[queries.Count];
            for (int i = 0; i < queries.Count; i++)
            {
                result[i] = queries[i].guid;
            }
            return result;
        }
    }
}
