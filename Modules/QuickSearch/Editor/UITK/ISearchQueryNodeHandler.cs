// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class SearchQueryNodeData
    {
        public bool IsRoot;
        public int HandlerId;
        public string Name;
        public string QueryId;
        public int TreeId;

        public bool ValidQuery => !string.IsNullOrWhiteSpace(QueryId);

        public SearchQueryNodeData(bool isRoot, int handlerId, string name, string queryId = "", int treeId = -1)
        {
            this.IsRoot = isRoot;
            this.HandlerId = handlerId;
            this.Name = name;
            this.QueryId = queryId;
            this.TreeId = treeId;
        }

        public SearchQueryNodeData(int handlerId, string name, string queryId, int treeId)
            : this(false, handlerId, name, queryId, treeId) { }

        public override string ToString()
        {
            return $"{Name} - Handler:{HandlerId} Query:{QueryId}";
        }
    }

    class SearchQueryNodeComparer : IComparer<SearchQueryNodeData>
    {
        public int Compare(SearchQueryNodeData x, SearchQueryNodeData y)
        {
            // Root items should always be first
            switch (x.IsRoot)
            {
                case true when !y.IsRoot:
                    return -1;
                case false when y.IsRoot:
                    return 1;
            }

            return x.ValidQuery switch
            {
                // Then we sort by invalid queries (i.e. folders) first
                false when y.ValidQuery => -1,
                true when !y.ValidQuery => 1,
                // Finally we sort by name
                _ => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    class TreeViewItemComparer : IComparer<TreeViewItemData<SearchQueryNodeData>>
    {
        SearchQueryNodeComparer m_SearchQueryNodeComparer = new();
        public int Compare(TreeViewItemData<SearchQueryNodeData> x, TreeViewItemData<SearchQueryNodeData> y)
        {
            return m_SearchQueryNodeComparer.Compare(x.data, y.data);
        }
    }

    interface ISearchQueryNodeHandler : IDisposable
    {
        int HandlerId { get; }
        TreeViewItemData<SearchQueryNodeData>? RootItem { get; }
        string Name { get; }
        event Action<ISearchQueryNodeHandler> queryListChanged;

        void BuildRoots(SearchContext context, string queryFilter = null);
        void Rename(SearchQueryTreeViewItem item, string newName);

        ISearchQuery GetQuery(int treeId);
        ISearchQuery GetQuery(string queryId);
        ISearchQuery GetValidQuery(SearchQueryTreeViewItem item);

        bool SupportsRename(ISearchQuery query);
        
        void PopulateContextualMenu(TreeView tree, SearchContext context, SearchQueryTreeViewItem item, DropdownMenu menu);

        void ActivateItem(TreeView tree, SearchQueryTreeViewItem item);
        void BindItem(TreeView tree, SearchQueryTreeViewItem item, int itemIndex);
    }
}
