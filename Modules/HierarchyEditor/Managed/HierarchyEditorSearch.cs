// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Search;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    class HierarchySearchUtils
    {
        public static void GetTerminalNodes(IQueryNode parent, List<IQueryNode> nodes)
        {
            if (parent.type == QueryNodeType.Filter || parent.type == QueryNodeType.Search)
            {
                nodes.Add(parent);
            }

            if (parent.children == null)
                return;

            foreach (var c in parent.children)
            {
                GetTerminalNodes(c, nodes);
            }
        }

        public static HierarchySearchFilter FilterNodeToSearchFilter(FilterNode node)
        {
            return HierarchySearchFilter.CreateFilter(node.filterId, node.op.token, node.filterValue);
        }
    }

    class HierarchyEditorSearchQueryParser : IHierarchySearchQueryParser
    {
        static readonly Regex SerializedPropertyRx = new Regex(@"(#[\w\d\.]+)");
        readonly QueryEngine<GameObject> m_QueryTokenizer;
        public HierarchyEditorSearchQueryParser()
        {
            m_QueryTokenizer = new QueryEngine<GameObject>();
            m_QueryTokenizer.AddFilter(SerializedPropertyRx, OnDummyPropertyWithHash);
            m_QueryTokenizer.validateFilters = false;
        }

        private string OnDummyPropertyWithHash(GameObject obj, string propertyName)
        {
            return "";
        }

        public HierarchySearchQueryDescriptor ParseQuery(string queryStr)
        {
            if (string.IsNullOrEmpty(queryStr) || string.IsNullOrWhiteSpace(queryStr))
                return HierarchySearchQueryDescriptor.Empty;

            var query = m_QueryTokenizer.ParseQuery(queryStr);
            if (!query.valid)
                return HierarchySearchQueryDescriptor.InvalidQuery;

            var nodes = new List<IQueryNode>();
            var graph = query.queryGraph;
            if (graph.root == null)
                return HierarchySearchQueryDescriptor.InvalidQuery;

            HierarchySearchUtils.GetTerminalNodes(graph.root, nodes);

            var filters = new List<HierarchySearchFilter>();
            var textValues = new List<string>();
            foreach (var node in nodes)
            {
                if (node is FilterNode filter)
                {
                    filters.Add(HierarchySearchUtils.FilterNodeToSearchFilter(filter));
                }
                else if (node is SearchNode search)
                {
                    textValues.Add(search.searchValue);
                }
            }

            return new HierarchySearchQueryDescriptor(filters.ToArray(), textValues.ToArray());
        }
    }

}
