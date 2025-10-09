// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class UserSearchQueryNodeHandler : BaseSearchQueryNodeHandler
    {
        public override string Name => SearchQueryTreeConfig.UserQueriesLabel;
        public override int HandlerId => HashingUtils.GetHashCode(nameof(UserSearchQueryNodeHandler));

        public override void BuildRoots(SearchContext context, string queryFilter = null) => BuildRoots(context, SearchQuery.userQueries, queryFilter);

        public override void Rename(SearchQueryTreeViewItem item, string newName)
        {
            if (item == null || string.IsNullOrWhiteSpace(newName))
                return;

            var query = GetQuery(item.Data.TreeId);
            var userQuery = (SearchQuery)query;
            userQuery.name = newName;
            SearchQuery.SaveSearchQuery(userQuery);
        }

        public override void AddQuery(ISearchQuery query)
        {
            var treeId = HashingUtils.GetHashCode(query.guid);

            if (!m_QueryIdLookup.TryAdd(treeId, query))
                return;

            var queryItem = SearchQueryPanelTreeView.CreateItemData(HandlerId, query.displayName, query.guid, treeId);

            if (m_RootItem == null)
                m_RootItem = SearchQueryPanelTreeView.CreateItemData(isRoot: true, HandlerId, Name, new() { queryItem });
            else
            {
                var rootChildren = new List<TreeViewItemData<SearchQueryNodeData>>(m_RootItem.Value.children);
                var index = GetSortedInsertionIndex(rootChildren, queryItem, s_TreeViewItemComparer);
                m_RootItem.Value.InsertChild(queryItem, index);
            }
        }

        public override void UpdateQuery(ISearchQuery query)
        {
            // Determine if the query is already in the tree
            // If not add it, else update the existing query

            var treeId = HashingUtils.GetHashCode(query.guid);

            var rootChildren = m_RootItem?.children;
            TreeViewItemData<SearchQueryNodeData>? queryItem = null;
            if (rootChildren != null && rootChildren.GetCount() > 0)
            {
                foreach (var child in rootChildren)
                {
                    if (child.data.TreeId == treeId)
                    {
                        queryItem = child;
                        break;
                    }
                }
            }

            if (!m_QueryIdLookup.ContainsKey(treeId) || queryItem == null)
                AddQuery(query);
            else
            {
                queryItem.Value.data.Name = query.displayName;
                m_QueryIdLookup[treeId] = query;

                // Re-insert the item sorted by new name
                if (rootChildren.GetCount() > 0)
                {
                    m_RootItem.Value.RemoveChild(treeId);
                    var index = GetSortedInsertionIndex(new List<TreeViewItemData<SearchQueryNodeData>>(rootChildren), queryItem.Value, s_TreeViewItemComparer);
                    m_RootItem.Value.InsertChild(queryItem.Value, index);
                }
            }
        }

        public override void RemoveQuery(ISearchQuery query)
        {
            var treeId = HashingUtils.GetHashCode(query.guid);
            RemoveQuery(treeId);
        }

        public override void RemoveQuery(int queryId)
        {
            m_RootItem?.RemoveChild(queryId);
            m_QueryIdLookup.Remove(queryId, out _);

            if (m_RootItem?.children.GetCount() == 0)
                m_RootItem = null;
        }

        public override void PopulateContextualMenu(TreeView tree, SearchContext context, SearchQueryTreeViewItem item, DropdownMenu menu)
        {
            var query = GetQuery(item.Data.TreeId);
            if (query == null)
                return;

            base.PopulateContextualMenu(tree, context, item, menu);

            menu.AppendAction(k_SetIconMenuLabel, (_) => SearchUtils.ShowIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                query.thumbnail = newIcon;
                SearchQuery.SaveSearchQuery((SearchQuery)query);
            }));
            menu.AppendAction(k_SearchTemplateMenuLabel, (_) => ((SearchQuery)query).isSearchTemplate = !query.isSearchTemplate, action =>
            {
                return query.isSearchTemplate ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            });
            menu.AppendAction(Utils.GetRevealInFinderLabel(), (_) => EditorUtility.RevealInFinder(query.filePath));
            menu.AppendSeparator();
            menu.AppendAction(k_DeleteMenuLabel, (_) =>
            {
                if (item.viewState.activeQuery == query)
                    item.viewState.activeQuery = null;
                SearchQuery.RemoveSearchQuery((SearchQuery)query);
            });
        }
    }
}
