// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search.Providers
{
    static class Query
    {
        internal const string type = "query";
        private const string displayName = "Queries";

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                filterId = "q:",
                isExplicitProvider = true,
                fetchItems = (context, items, provider) =>
                {
                    var queryItems = SearchQuery.GetAllSearchQueryItems(context);
                    if (string.IsNullOrEmpty(context.searchQuery))
                    {
                        items.AddRange(queryItems);
                    }
                    else
                    {
                        foreach (var qi in queryItems)
                        {
                            if (SearchUtils.MatchSearchGroups(context, qi.label, true) ||
                                SearchUtils.MatchSearchGroups(context, ((SearchQuery)qi.data).searchQuery, true))
                            {
                                items.Add(qi);
                            }
                        }
                    }
                    return null;
                }
            };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "exec", null, "Execute search query")
                {
                    closeWindowAfterExecution = false,
                    handler = (item) => SearchQuery.ExecuteQuery(item.context.searchView, (SearchQuery)item.data)
                },
                new SearchAction(type, "select", null, "Select search query", (item) =>
                {
                    var queryPath = AssetDatabase.GetAssetPath((SearchQuery)item.data);
                    if (!string.IsNullOrEmpty(queryPath))
                        Utils.FrameAssetFromPath(queryPath);
                })
            };
        }
    }
}
