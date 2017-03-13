// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    // TreeViewDataSource is a base abstract class for a data source for a TreeView.
    // Usage:
    //   Override FetchData () and build the entire tree with m_RootItem as root.
    //   Configure showRootItem and rootIsCollapsable as wanted
    //
    // Note: if dealing with very large trees use LazyTreeViewDataSource instead: it assumes that tree only contains visible items.

    internal abstract class TreeViewDataSource : ITreeViewDataSource
    {
        protected readonly TreeViewController m_TreeView;                   // TreeView using this data source
        protected TreeViewItem m_RootItem;
        protected IList<TreeViewItem> m_Rows;
        protected bool m_NeedRefreshRows = true;
        protected TreeViewItem m_FakeItem;

        public bool showRootItem { get; set; }
        public bool rootIsCollapsable { get; set; }
        public bool alwaysAddFirstItemToSearchResult { get; set; } // is only used in searches when showRootItem is false. It Doesn't make sense for visible roots
        public TreeViewItem root { get { return m_RootItem; } }
        public System.Action onVisibleRowsChanged;

        protected List<int> expandedIDs
        {
            get {return m_TreeView.state.expandedIDs; }
            set { m_TreeView.state.expandedIDs = value; }
        }

        public TreeViewDataSource(TreeViewController treeView)
        {
            m_TreeView = treeView;
            showRootItem = true;
            rootIsCollapsable = false;
            m_RootItem = null;
            onVisibleRowsChanged = null;
        }

        virtual public void OnInitialize()
        {
        }

        // Implement this function and build entire tree with m_RootItem as root
        public abstract void FetchData();

        public virtual void ReloadData()
        {
            m_FakeItem = null;
            FetchData();
        }

        virtual public TreeViewItem FindItem(int id)
        {
            return TreeViewUtility.FindItem(id, m_RootItem);
        }

        virtual public bool IsRevealed(int id)
        {
            IList<TreeViewItem> rows = GetRows();
            return TreeViewController.GetIndexOfID(rows, id) >= 0;
        }

        virtual public void RevealItem(int id)
        {
            if (IsRevealed(id))
                return;

            // Reveal (expand parents up to root)
            TreeViewItem item = FindItem(id);
            if (item != null)
            {
                TreeViewItem parent = item.parent;
                while (parent != null)
                {
                    SetExpanded(parent, true);
                    parent = parent.parent;
                }
            }
        }

        virtual public void OnSearchChanged()
        {
            m_NeedRefreshRows = true;
        }

        //----------------------------
        // Visible Item section

        protected void GetVisibleItemsRecursive(TreeViewItem item, IList<TreeViewItem> items)
        {
            if (item != m_RootItem || showRootItem)
                items.Add(item);

            if (item.hasChildren && IsExpanded(item))
                foreach (TreeViewItem child in item.children)
                    GetVisibleItemsRecursive(child, items);
        }

        protected void SearchRecursive(TreeViewItem item, string search, IList<TreeViewItem> searchResult)
        {
            if (item.displayName.ToLower().Contains(search))
                searchResult.Add(item);

            if (item.children != null)
                foreach (TreeViewItem child in item.children)
                    SearchRecursive(child, search, searchResult);
        }

        virtual protected List<TreeViewItem> ExpandedRows(TreeViewItem root)
        {
            var result = new List<TreeViewItem>();
            GetVisibleItemsRecursive(m_RootItem, result);
            return result;
        }

        // Searches the current tree by displayName.
        virtual protected List<TreeViewItem> Search(TreeViewItem root, string search)
        {
            var result = new List<TreeViewItem>();

            if (showRootItem)
            {
                SearchRecursive(root, search, result);
                result.Sort(new TreeViewItemAlphaNumericSort());
            }
            else
            {
                int startIndex = alwaysAddFirstItemToSearchResult ? 1 : 0;

                if (root.hasChildren)
                {
                    for (int i = startIndex; i < root.children.Count; ++i)
                    {
                        SearchRecursive(root.children[i], search, result);
                    }
                    result.Sort(new TreeViewItemAlphaNumericSort());

                    if (alwaysAddFirstItemToSearchResult)
                        result.Insert(0, root.children[0]);
                }
            }

            return result;
        }

        virtual public int rowCount
        {
            get
            {
                return GetRows().Count;
            }
        }

        virtual public int GetRow(int id)
        {
            var rows = GetRows();
            for (int row = 0; row < rows.Count; ++row)
            {
                if (rows[row].id == id)
                    return row;
            }
            return -1;
        }

        virtual public TreeViewItem GetItem(int row)
        {
            return GetRows()[row];
        }

        // Get the flattend tree of visible items.
        virtual public IList<TreeViewItem> GetRows()
        {
            InitIfNeeded();
            return m_Rows;
        }

        virtual public void InitIfNeeded()
        {
            // Cached for large trees...
            if (m_Rows == null || m_NeedRefreshRows)
            {
                if (m_RootItem != null)
                {
                    if (m_TreeView.isSearching)
                        m_Rows = Search(m_RootItem, m_TreeView.searchString.ToLower());
                    else
                        m_Rows = ExpandedRows(m_RootItem);
                }
                else
                {
                    Debug.LogError("TreeView root item is null. Ensure that your TreeViewDataSource sets up at least a root item.");
                    m_Rows = new List<TreeViewItem>();
                }

                m_NeedRefreshRows = false;

                // TODO: This should be named something like: 'onVisibleRowsReloaded'
                if (onVisibleRowsChanged != null)
                    onVisibleRowsChanged();

                // Expanded state has changed ensure that we repaint
                m_TreeView.Repaint();
            }
        }

        public bool isInitialized
        {
            get { return m_RootItem != null && m_Rows != null; }
        }

        //----------------------------
        // Expanded/collapsed section

        virtual public int[] GetExpandedIDs()
        {
            return expandedIDs.ToArray();
        }

        virtual public void SetExpandedIDs(int[] ids)
        {
            expandedIDs = new List<int>(ids);
            expandedIDs.Sort();
            m_NeedRefreshRows = true;
            OnExpandedStateChanged();
        }

        virtual public bool IsExpanded(int id)
        {
            return expandedIDs.BinarySearch(id) >= 0;
        }

        virtual public bool SetExpanded(int id, bool expand)
        {
            bool expanded = IsExpanded(id);
            if (expand != expanded)
            {
                if (expand)
                {
                    System.Diagnostics.Debug.Assert(!expandedIDs.Contains(id));
                    expandedIDs.Add(id);
                    expandedIDs.Sort();
                }
                else
                {
                    expandedIDs.Remove(id);
                }
                m_NeedRefreshRows = true;
                OnExpandedStateChanged();
                return true;
            }
            return false;
        }

        virtual public void SetExpandedWithChildren(int id, bool expand)
        {
            SetExpandedWithChildren(FindItem(id), expand);
        }

        virtual public void SetExpandedWithChildren(TreeViewItem fromItem, bool expand)
        {
            if (fromItem == null)
            {
                Debug.LogError("item is null");
                return;
            }

            HashSet<int> parents = TreeViewUtility.GetParentsBelowItem(fromItem);

            // Get existing expanded in hashset
            HashSet<int> oldExpandedSet = new HashSet<int>(expandedIDs);

            if (expand)
                oldExpandedSet.UnionWith(parents);
            else
                oldExpandedSet.ExceptWith(parents);

            // Bulk set expanded ids (is sorted in SetExpandedIDs)
            SetExpandedIDs(oldExpandedSet.ToArray());
        }

        virtual public void SetExpanded(TreeViewItem item, bool expand)
        {
            SetExpanded(item.id, expand);
        }

        virtual public bool IsExpanded(TreeViewItem item)
        {
            return IsExpanded(item.id);
        }

        virtual public bool IsExpandable(TreeViewItem item)
        {
            // Ignore expansion (foldout arrow) when showing search results
            if (m_TreeView.isSearching)
                return false;
            return item.hasChildren;
        }

        virtual public bool CanBeMultiSelected(TreeViewItem item)
        {
            return true;
        }

        virtual public bool CanBeParent(TreeViewItem item)
        {
            return true;
        }

        virtual public void OnExpandedStateChanged()
        {
            if (m_TreeView.expandedStateChanged != null)
                m_TreeView.expandedStateChanged();
        }

        //----------------------------
        // Renaming section

        virtual public bool IsRenamingItemAllowed(TreeViewItem item)
        {
            return true;
        }

        //----------------------------
        // Insert tempoary Item section

        // Fake Item should be inserted into the m_VisibleRows (not the tree itself).
        virtual public void InsertFakeItem(int id, int parentID, string name, Texture2D icon)
        {
            Debug.LogError("InsertFakeItem missing implementation");
        }

        virtual public bool HasFakeItem()
        {
            return m_FakeItem != null;
        }

        virtual public void RemoveFakeItem()
        {
            if (!HasFakeItem())
                return;

            var visibleRows = GetRows();
            int index = TreeViewController.GetIndexOfID(visibleRows, m_FakeItem.id);
            if (index != -1)
            {
                visibleRows.RemoveAt(index);
            }
            m_FakeItem = null;
        }
    }
}
