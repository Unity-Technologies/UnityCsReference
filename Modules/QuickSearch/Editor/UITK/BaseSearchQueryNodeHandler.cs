// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    abstract class BaseSearchQueryNodeHandler : ISearchQueryNodeHandler
    {
        protected static readonly string k_SaveMenuLabel = L10n.Tr("Save");
        protected static readonly string k_OpenInNewWindowMenuLabel = L10n.Tr("Open in new window");
        protected static readonly string k_RenameMenuLabel = L10n.Tr("Rename");
        protected static readonly string k_SetIconMenuLabel = L10n.Tr("Set Icon...");
        protected static readonly string k_SearchTemplateMenuLabel = L10n.Tr("Search Template");
        protected static readonly string k_DeleteMenuLabel = L10n.Tr("Delete");
        protected static readonly string k_EditInInspectorMenuLabel = L10n.Tr("Edit in Inspector");

        protected static SearchQueryNodeComparer s_SearchQueryNodeComparer = new();
        protected static TreeViewItemComparer s_TreeViewItemComparer = new();
        protected IEnumerable<ISearchQuery> m_Queries;
        protected Dictionary<int, ISearchQuery> m_QueryIdLookup = new();
        protected TreeViewItemData<SearchQueryNodeData>? m_RootItem;

        public static Texture2D FolderIcon = EditorGUIUtility.FindTexture("Folder Icon");

        #region ISearchQueryNodeHandler API
        public abstract string Name { get; }
        public abstract int HandlerId { get; }
        public TreeViewItemData<SearchQueryNodeData>? RootItem => m_RootItem;

        public abstract void BuildRoots(SearchContext context, string queryFilter = null);
        public event Action<ISearchQueryNodeHandler> queryListChanged;

        public abstract void Rename(SearchQueryTreeViewItem item, string newName);

        public abstract void Dispose();

        public ISearchQuery GetQuery(int treeId)
        {
            if (m_QueryIdLookup.TryGetValue(treeId, out var query))
                return query;
            return null;
        }

        public ISearchQuery GetQuery(string queryId)
        {
            foreach (var query in m_QueryIdLookup.Values)
            {
                if (query.guid == queryId)
                    return query;
            }
            return null;
        }

        public virtual bool SupportsRename(ISearchQuery query)
        {
            return true;
        }
        
        internal IEnumerable<ISearchQuery> GetFilteredQueries(IEnumerable<ISearchQuery> queries, string queryFilter)
        {
            var filteredQueries = new List<ISearchQuery>();
            foreach (var q in queries)
            {
                if (!IsQueryVisible(queryFilter, q))
                    continue;
                filteredQueries.Add(q);
            }
            return filteredQueries;
        }

        public virtual bool IsQueryVisible(string queryFilter, ISearchQuery query)
        {
            return SearchQueryPanelTreeUtils.IsQueryNameMatchingFilter(queryFilter, query.displayName);
        }

        public virtual void PopulateContextualMenu(TreeView tree, SearchContext context, SearchQueryTreeViewItem item, DropdownMenu menu)
        {
            var query = GetQuery(item.Data.TreeId);
            if (query == null)
                return;

            if (item.viewState.activeQuery == query && !context.empty)
            {
                menu.AppendAction(k_SaveMenuLabel, (_) => item.Emit(SearchEvent.SaveActiveSearchQuery));
                menu.AppendSeparator();
            }
            menu.AppendAction(k_OpenInNewWindowMenuLabel, (action) =>
            {
                SearchQuery.Open(query, SearchFlags.None);
            });
            menu.AppendSeparator();
            if (SupportsRename(query))
                menu.AppendAction(k_RenameMenuLabel, (_) => item.Rename());
        }

        public void ActivateItem(TreeView tree, SearchQueryTreeViewItem item)
        {
            var query = GetQuery(item.Data.TreeId);
            if (query == null)
                return;
            item.Emit(SearchEvent.ExecuteSearchQuery, query);
        }

        public void BindItem(TreeView tree, SearchQueryTreeViewItem item, int itemIndex)
        {
            if (item.Data.IsRoot)
            {
                // Root item
                item.Bind(null, item.Data.Name);
                return;
            }

            var query = GetValidQuery(item);
            BindItemToQuery(tree, item, query);
        }
        
        public virtual void BindItemToQuery(TreeView tree, SearchQueryTreeViewItem item, ISearchQuery query)
        {
            if (query != null)
            {
                item.Bind(SearchQuery.GetIcon(query), query.displayName, query.itemCount);
            }
            else
            {
                item.Bind(FolderIcon, item.Data.Name);
            }
        }

        public virtual ISearchQuery GetValidQuery(SearchQueryTreeViewItem item)
        {
            if (!item.Data.ValidQuery)
                return null;

            var query = GetQuery(item.Data.TreeId);
            return query;
        }
        #endregion

        protected void BuildRoots(SearchContext context, IEnumerable<ISearchQuery> queries, string queryFilter = null)
        {
            m_Queries = GetValidQueries(queries, context.GetProviders());
            var filteredQueries = GetFilteredQueries(m_Queries, queryFilter);

            List<TreeViewItemData<SearchQueryNodeData>> queryTreeItems = new();
            foreach (var query in filteredQueries)
            {
                var treeId = query.GetTreeId();
                var queryItem = SearchQueryPanelTreeView.CreateItemData(HandlerId, query.displayName, query.guid, treeId);
                queryTreeItems.Add(queryItem);
                m_QueryIdLookup[treeId] = query;
            }

            queryTreeItems.Sort(s_TreeViewItemComparer);
            m_RootItem = queryTreeItems.Count > 0 ? SearchQueryPanelTreeView.CreateItemData(isRoot: true, HandlerId, Name, queryTreeItems) : null;
        }

        protected void NotifyQueryListChanged()
        {
            queryListChanged?.Invoke(this);
        }

        public static IEnumerable<ISearchQuery> GetValidQueries(IEnumerable<ISearchQuery> queries, IEnumerable<SearchProvider> providers)
        {
            var unallowedFilterIds = GetUnallowedFilterIds(providers);
            foreach (var q in queries)
            {
                var anyUnallowed = false;
                foreach (var filterId in unallowedFilterIds)
                {
                    if (q.searchText.StartsWith(filterId))
                    {
                        anyUnallowed = true;
                        break;
                    }
                }

                if (anyUnallowed)
                    continue;

                var matchesAnyProvider = false;
                foreach (var provider in providers)
                {
                    if (q.searchText.StartsWith(provider.filterId))
                    {
                        matchesAnyProvider = true;
                        break;
                    }
                }

                if (matchesAnyProvider)
                {
                    yield return q;
                }
                else
                {
                    var queryProviders = q.GetProviderIds();
#pragma warning disable UA2002 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (!queryProviders.Any())
#pragma warning restore UA2002
                    {
                        yield return q;
                    }
                    else if (DoProvidersListOverlap(queryProviders, providers))
                    {
                        yield return q;
                    }
                }
            }

            bool DoProvidersListOverlap(IEnumerable<string> providerIds, IEnumerable<SearchProvider> searchProviders)
            {
                foreach (var pid in providerIds)
                {
                    foreach (var provider in searchProviders)
                    {
                        if (provider.id == pid)
                            return true;
                    }
                }
                return false;
            }
        }

        protected static int GetSortedInsertionIndex<T>(IEnumerable<T> collection, T entry, IComparer<T> comparer)
        {
            int i = 0;
            foreach(var item in collection)
            {
                if (comparer.Compare(item, entry) > 0)
                    return i;
                i++;
            }
            return i;
        }

        static List<string> GetUnallowedFilterIds(IEnumerable<SearchProvider> searchProviders)
        {
            var unallowedProviders = new List<SearchProvider>();
            foreach (var p1 in SearchService.GetActiveProviders())
            {
                var isAllowed = true;
                foreach (var p2 in searchProviders)
                {
                    if (p1.id == p2.id)
                    {
                        isAllowed = false;
                        break;
                    }
                }

                if (isAllowed)
                    unallowedProviders.Add(p1);
            }

            var unallowedFilterIds = new List<string>();
            foreach (var provider in unallowedProviders)
                unallowedFilterIds.Add(provider.filterId);

            return unallowedFilterIds;
        }
    }
}
