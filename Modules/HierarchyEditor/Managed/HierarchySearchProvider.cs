// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.Search.Providers;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    interface IHierarchySearchPropositionProvider
    {
        IEnumerable<SearchProposition> FetchPropositions(HierarchyViewModel viewModel, SearchContext context, SearchPropositionOptions options);
    }

    [QueryListBlock("Node Types", "nodetype", "nodetype", ":")]
    class QueryNodeTypeBlock : QueryListBlock
    {
        public QueryNodeTypeBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr)
            : base(source, id, value, attr)
        {
            icon = UnityEditor.Search.SearchUtils.GetTypeIcon(typeof(GameObject));
            alwaysDrawLabel = false;
        }

        public static IEnumerable<SearchProposition> GetPropositions(Unity.Hierarchy.Hierarchy hierarchy, SearchPropositionFlags flags)
        {
            var category = flags.HasAny(SearchPropositionFlags.NoCategory) ? null : "Node Types";
            var icon = UnityEditor.Search.SearchUtils.GetTypeIcon(typeof(GameObject));
            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                var name = handler.GetNodeTypeName();
                yield return new SearchProposition(category: category, label: name, replacement: $"nodetype={name}",
                    data: name, icon: icon);
            }
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags)
        {
            if (source is QueryBuilder builder)
            {
                if (builder.searchView is HierarchySearchView view && view.Window != null)
                {
                    return GetPropositions(view.Window.Hierarchy, flags);
                }
            }

            return Array.Empty<SearchProposition>();
        }
    }

    class HierarchySearchProvider : SearchProvider
    {
        HierarchyView m_HierarchyView;

        public HierarchySearchProvider(HierarchyView view)
            : base("hierarchyv2", "Hierarchy")
        {
            m_HierarchyView = view;
            fetchPropositions = FetchPropositions;
            isExplicitProvider = true;
            active = true;
            priority = 2500;
            showDetails = false;
        }

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            var hierarchy = m_HierarchyView.Source;
            foreach (var prop in QueryNodeTypeBlock.GetPropositions(hierarchy, SearchPropositionFlags.None))
            {
                yield return prop;
            }

            foreach (var handler in hierarchy.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchySearchPropositionProvider provider)
                {
                    foreach (var prop in provider.FetchPropositions(m_HierarchyView.ViewModel, context, options))
                        yield return prop;
                }
            }
        }
    }
}
