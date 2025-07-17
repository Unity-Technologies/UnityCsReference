// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    partial class ProjectSearchQueryTreeNodeHandler : BaseSearchQueryNodeHandler
    {
        class ProjectEntryInfo
        {
            public int Id;
            public string Path;
            public SearchQueryNodeData Node;
            public ProjectEntryInfo Parent;
            public List<ProjectEntryInfo> Children = new();

            public bool isValidQuery => Node is {ValidQuery: true};

            public override string ToString()
            {
                return $"{Path} ({Children.Count})";
            }
        }

        class ProjectEntryInfoComparer : IComparer<ProjectEntryInfo>
        {
            SearchQueryNodeComparer m_SearchQueryNodeComparer = new();

            public int Compare(ProjectEntryInfo x, ProjectEntryInfo y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                var result = m_SearchQueryNodeComparer.Compare(x.Node, y.Node);
                if (result != 0)
                    return result;

                return x.Path.CompareTo(y.Path);
            }
        }

        static ProjectEntryInfoComparer s_ProjectEntryComparer = new();

        string m_BaseFolder;
        string m_RootName;
        bool m_IsEditorResources;

        Dictionary<string, ProjectEntryInfo> m_ProjectEntries = new();

        public override string Name => m_RootName;
        public override int HandlerId => HashingUtils.GetHashCode($"{nameof(ProjectSearchQueryTreeNodeHandler)}_{m_BaseFolder}");

        public ProjectSearchQueryTreeNodeHandler(bool isEditorResources, string baseFolder, string rootName = null)
        {
            m_IsEditorResources = isEditorResources;
            m_BaseFolder = isEditorResources ? baseFolder.ToLowerInvariant() : baseFolder;
            m_RootName = string.IsNullOrEmpty(rootName) ? Path.GetFileName(m_BaseFolder) : rootName;

            if (!isEditorResources && !Directory.Exists(m_BaseFolder))
            {
                Debug.LogError($"The folder {m_BaseFolder} doesn't exists and cannot be mounted as a Query source.");
            }

            SearchQueryAsset.ListenToAssetChanges();
        }

        public override void BuildRoots(SearchContext context, string queryFilter = null)
        {
            m_Queries = EnumerateQueries();

            var filteredQueries = GetFilteredQueries(m_Queries, queryFilter);

            m_QueryIdLookup.Clear();
            m_ProjectEntries = new Dictionary<string, ProjectEntryInfo>();

            foreach (var query in filteredQueries)
                AddQuery(query);

            BuildRootItem();
        }

        void BuildRootItem()
        {
            if (!m_ProjectEntries.TryGetValue("/", out var rootEntry) && m_ProjectEntries.Count > 0)
                throw new ArgumentException($"Cannot find root entry for {m_BaseFolder} - {m_RootName} - Resources:{m_IsEditorResources}");

            m_RootItem = rootEntry?.Children.Count > 0 ? BuildItem(rootEntry) : null;
        }

        static TreeViewItemData<SearchQueryNodeData> BuildItem(ProjectEntryInfo entry)
        {
            var childItems = new List<TreeViewItemData<SearchQueryNodeData>>();
            foreach (var child in entry.Children)
            {
                childItems.Add(BuildItem(child));
            }
            return SearchQueryPanelTreeView.CreateItemData(entry.Id, entry.Node, childItems);
        }

        public override bool SupportsRename(ISearchQuery query)
        {
            // If Builtins -> no rename allowed
            // If from project: use the Project Browser to rename.
            return false;
        }

        public override ISearchQuery GetValidQuery(SearchQueryTreeViewItem item)
        {
            var query = base.GetValidQuery(item);
            if (query != null && query is SearchQueryAsset queryAsset && queryAsset)
            {
                return queryAsset;
            }
            return null;
        }

        public override void Rename(SearchQueryTreeViewItem item, string newName)
        {
            // If Builtins -> no rename allowed
            // If from project: use the Project Browser to rename.
        }

        public override void AddQuery(ISearchQuery query)
        {
            var treeId = query.GetTreeId();
            if (m_QueryIdLookup.ContainsKey(treeId) || string.IsNullOrEmpty(query.filePath))
                return;

            var queryFolder = Path.GetDirectoryName(query.filePath)?.Replace("\\", "/");
            if (queryFolder != null)
            {
                var folder = queryFolder.Replace(m_BaseFolder, "");
                var tokens = folder.Split('/');
                var path = "/";

                ProjectEntryInfo parentEntry = null;
                foreach(var t in tokens)
                {
                    var isRoot = t == "";
                    var entryName = isRoot ? m_RootName : t;

                    path = Path.Combine(path, t);

                    if (!m_ProjectEntries.TryGetValue(path, out var entry))
                    {
                        var projectEntryId = HashingUtils.GetHashCode(m_BaseFolder + path); // We need a unique id for each entry

                        entry = new ProjectEntryInfo { Id = projectEntryId, Path = path, Node = new SearchQueryNodeData(isRoot: isRoot, HandlerId, name: entryName) };
                        AddProjectEntry(entry, parentEntry);
                    }

                    parentEntry = entry;
                }

                var queryEntry = new ProjectEntryInfo { Id = treeId, Path = query.filePath, Node = new SearchQueryNodeData(isRoot: false, HandlerId, query.displayName, query.guid, treeId ) };
                AddProjectEntry(queryEntry, parentEntry);

                m_QueryIdLookup[treeId] = query;

                if (m_RootItem == null)
                    BuildRootItem();
            }
        }

        void AddProjectEntry(ProjectEntryInfo entry, ProjectEntryInfo parentEntry = null)
        {
            if (parentEntry != null)
            {
                var insertionIndex = GetSortedInsertionIndex(parentEntry.Children, entry, s_ProjectEntryComparer);
                parentEntry.Children.Insert(insertionIndex, entry);
                entry.Parent = parentEntry;
            }

            m_ProjectEntries.Add(entry.Path, entry);

            if (m_RootItem != null)
            {
                var parentId = entry.Parent?.Id ?? m_RootItem.Value.id;
                AddTreeViewItem(m_RootItem.Value, parentId, BuildItem(entry));
            }
        }

        public override void UpdateQuery(ISearchQuery query)
        {
            var treeId = query.GetTreeId();

            // When moving the assets in the file system, moving an asset is treated as a remove and add. We mimic this flow here
            if (m_QueryIdLookup.ContainsKey(treeId))
                RemoveQuery(treeId);

            AddQuery(query);
        }

        public override void RemoveQuery(ISearchQuery query)
        {
            var treeId = query.GetTreeId();
            RemoveQuery(treeId);
        }

        public override void RemoveQuery(int treeId)
        {
            if (m_QueryIdLookup.Remove(treeId))
            {
                foreach (var entry in m_ProjectEntries.Values)
                {
                    if (entry.Id == treeId)
                    {
                        RemoveProjectEntry(entry);
                        break;
                    }
                }
            }
        }

        public string RemoveQuery(string queryPath)
        {
            if (m_ProjectEntries.TryGetValue(queryPath, out var queryEntry))
            {
                m_QueryIdLookup.Remove(queryEntry.Id);
                RemoveProjectEntry(queryEntry);
                return queryEntry.Node.QueryId;
            }

            return null;
        }

        void RemoveProjectEntry(ProjectEntryInfo entry)
        {
            if (m_RootItem != null)
                RemoveTreeViewItem(m_RootItem.Value, entry.Id);

            m_ProjectEntries.Remove(entry.Path);

            var parent = entry.Parent;
            if (parent != null)
            {
                parent.Children.Remove(entry);
                if (parent.Children.Count == 0)
                    RemoveProjectEntry(parent);
            }

            if (m_RootItem != null && m_RootItem.Value.children.GetCount() == 0)
                m_RootItem = null;
        }

        static void AddTreeViewItem(TreeViewItemData<SearchQueryNodeData> rootItem, int parentId, TreeViewItemData<SearchQueryNodeData> newItem)
        {
            if (rootItem.id == parentId)
            {
               var children = new List<SearchQueryNodeData>();
                foreach (var child in rootItem.children)
                {
                    children.Add(child.data);
                }
                var index = children.Count > 0 ? GetSortedInsertionIndex(children, newItem.data, s_SearchQueryNodeComparer) : 0;

                rootItem.InsertChild(newItem, index);
                return;
            }

            foreach (var child in rootItem.children)
                AddTreeViewItem(child, parentId, newItem);
        }

        static void RemoveTreeViewItem(TreeViewItemData<SearchQueryNodeData> item, int itemId)
        {
            foreach (var child in item.children)
            {
                if (child.id == itemId)
                {
                    item.RemoveChild(itemId);
                    return;
                }
                RemoveTreeViewItem(child, itemId);
            }
        }

        public override void PopulateContextualMenu(TreeView tree, SearchContext context, SearchQueryTreeViewItem item, DropdownMenu menu)
        {
            var query = GetQuery(item.Data.TreeId);
            if (query == null)
                return;

            var queryAsset = (SearchQueryAsset)query;

            if (m_IsEditorResources)
            {
                // Editor resources are read-only - only allow to open in new window.
                menu.AppendAction(k_OpenInNewWindowMenuLabel, (action) =>
                {
                    SearchQuery.Open(query, SearchFlags.None);
                });

                return;
            }

            base.PopulateContextualMenu(tree, context, item, menu);

            menu.AppendAction(k_SetIconMenuLabel, (_) => SearchUtils.ShowIconPicker((newIcon, canceled) =>
            {
                if (canceled)
                    return;
                query.thumbnail = newIcon;
                EditorUtility.SetDirty(queryAsset);
                item.Emit(SearchEvent.ProjectQueryChanged, query);
            }));
            menu.AppendAction(k_SearchTemplateMenuLabel, (_) => queryAsset.isSearchTemplate = !queryAsset.isSearchTemplate, action =>
            {
                return query.isSearchTemplate ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
            });
            menu.AppendAction(k_EditInInspectorMenuLabel, (_) => Selection.activeObject = queryAsset);
            menu.AppendAction(Utils.GetRevealInFinderLabel(), (_) => EditorUtility.RevealInFinder(query.filePath));
            menu.AppendSeparator();
            menu.AppendAction(k_DeleteMenuLabel, (_) =>
            {
                if (item.viewState.activeQuery == query)
                    item.viewState.activeQuery = null;
                SearchQueryAsset.RemoveQuery(queryAsset);
            });
        }

        IEnumerable<ISearchQuery> EnumerateQueries(string queryFilter = null)
        {
            if (m_IsEditorResources)
            {
                var bundle = EditorGUIUtility.GetEditorAssetBundle();
                var bundlePaths = bundle.GetAllAssetNames();
                var searchQueriesPaths = new List<string>();
                foreach (var p in bundlePaths)
                {
                    if (p.Contains(m_BaseFolder))
                        searchQueriesPaths.Add(p);
                }

                foreach (var path in searchQueriesPaths)
                {
                    var query = bundle.LoadAsset<SearchQueryAsset>(path);
                    if (query != null)
                    {
                        query.filePath = path;
                        yield return query;
                    }
                }
            }
            else if (Directory.Exists(m_BaseFolder))
            {
                foreach (var q in SearchQueryAsset.EnumerateAll(m_BaseFolder))
                {
                    if (string.IsNullOrEmpty(queryFilter) || SearchQueryPanelTreeUtils.IsQueryNameMatchingFilter(queryFilter, q.displayName))
                        yield return q;
                }
            }
        }
    }
}
