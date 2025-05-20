// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor.IMGUI.Controls
{
    // TreeViewDataSource is a base abstract class for a data source for a TreeView.
    // Usage:
    //   Override FetchData () and build the entire tree with m_RootItem as root.
    //   Configure showRootItem and rootIsCollapsable as wanted
    //
    // Note: if dealing with very large trees use LazyTreeViewDataSource instead: it assumes that tree only contains visible items.

internal abstract class TreeViewDataSource<TIdentifier> : ITreeViewDataSource<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        protected TreeViewController<TIdentifier> m_TreeView { get { return m_TreeViewInternal; } set { m_TreeViewInternal = value; } } // TreeView using this data source
        protected TreeViewController<TIdentifier> m_TreeViewInternal;                   // TreeView using this data source
        protected TreeViewItem<TIdentifier> m_RootItem;
        protected IList<TreeViewItem<TIdentifier>> m_Rows;
        protected bool m_NeedRefreshRows = true;
        protected TreeViewItem<TIdentifier> m_FakeItem;

        public bool showRootItem { get; set; }
        public bool rootIsCollapsable { get; set; }
        public bool alwaysAddFirstItemToSearchResult { get; set; } // is only used in searches when showRootItem is false. It Doesn't make sense for visible roots
        public TreeViewItem<TIdentifier> root { get { return m_RootItem; } }
        public System.Action onVisibleRowsChanged;

        protected List<TIdentifier> expandedIDs
        {
            get {return m_TreeView.state.expandedIDs; }
            set { m_TreeView.state.expandedIDs = value; }
        }

        public TreeViewDataSource(TreeViewController<TIdentifier> treeView)
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

        virtual public TreeViewItem<TIdentifier> FindItem(TIdentifier id)
        {
            return TreeViewUtility<TIdentifier>.FindItem(id, m_RootItem);
        }

        virtual public bool IsRevealed(TIdentifier id)
        {
            IList<TreeViewItem<TIdentifier>> rows = GetRows();
            return TreeViewController<TIdentifier>.GetIndexOfID(rows, id) >= 0;
        }

        virtual public void RevealItem(TIdentifier id)
        {
            if (IsRevealed(id))
                return;

            // Reveal (expand parents up to root)
            TreeViewItem<TIdentifier> item = FindItem(id);
            if (item != null)
            {
                TreeViewItem<TIdentifier> parent = item.parent;
                while (parent != null)
                {
                    SetExpanded(parent, true);
                    parent = parent.parent;
                }
            }
        }

        virtual public void RevealItems(TIdentifier[] ids)
        {
            HashSet<TIdentifier> expandedSet = new HashSet<TIdentifier>(expandedIDs);
            int orgSize = expandedSet.Count;

            // Add all parents above id
            foreach (var id in ids)
            {
                if (IsRevealed(id))
                    continue;
                // Reveal (expand parents up to root)
                TreeViewItem<TIdentifier> item = FindItem(id);
                if (item != null)
                {
                    TreeViewItem<TIdentifier> parent = item.parent;
                    while (parent != null)
                    {
                        expandedSet.Add(parent.id);
                        parent = parent.parent;
                    }
                }
            }

            if (orgSize != expandedSet.Count)
            {
                // Bulk set expanded ids (is sorted in SetExpandedIDs)
                SetExpandedIDs(expandedSet.ToArray());

                // Refresh immediately if any Item was expanded
                if (m_NeedRefreshRows)
                    FetchData();
            }
        }

        virtual public void OnSearchChanged()
        {
            m_NeedRefreshRows = true;
        }

        //----------------------------
        // Visible Item section

        protected void GetVisibleItemsRecursive(TreeViewItem<TIdentifier> item, IList<TreeViewItem<TIdentifier>> items)
        {
            if (item != m_RootItem || showRootItem)
                items.Add(item);

            if (item.hasChildren && IsExpanded(item))
                foreach (TreeViewItem<TIdentifier> child in item.children)
                    GetVisibleItemsRecursive(child, items);
        }

        protected void SearchRecursive(TreeViewItem<TIdentifier> item, string search, IList<TreeViewItem<TIdentifier>> searchResult)
        {
            if (item.displayName.ToLower().Contains(search))
                searchResult.Add(item);

            if (item.children != null)
                foreach (TreeViewItem<TIdentifier> child in item.children)
                    SearchRecursive(child, search, searchResult);
        }

        virtual protected List<TreeViewItem<TIdentifier>> ExpandedRows(TreeViewItem<TIdentifier> root)
        {
            var result = new List<TreeViewItem<TIdentifier>>();
            GetVisibleItemsRecursive(m_RootItem, result);
            return result;
        }

        // Searches the current tree by displayName.
        virtual protected List<TreeViewItem<TIdentifier>> Search(TreeViewItem<TIdentifier> root, string search)
        {
            var result = new List<TreeViewItem<TIdentifier>>();

            if (showRootItem)
            {
                SearchRecursive(root, search, result);
                result.Sort(new TreeViewItemAlphaNumericSort<TIdentifier>());
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
                    result.Sort(new TreeViewItemAlphaNumericSort<TIdentifier>());

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

        virtual public int GetRow(TIdentifier id)
        {
            var rows = GetRows();
            for (int row = 0; row < rows.Count; ++row)
            {
                if (rows[row].id.Equals(id))
                    return row;
            }
            return -1;
        }

        virtual public TreeViewItem<TIdentifier> GetItem(int row)
        {
            return GetRows()[row];
        }

        // Get the flattend tree of visible items.
        virtual public IList<TreeViewItem<TIdentifier>> GetRows() => GetRowsInternal();
        virtual public IList<TreeViewItem<TIdentifier>> GetRowsInternal()
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
                    m_Rows = new List<TreeViewItem<TIdentifier>>();
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

        virtual public TIdentifier[] GetExpandedIDs()
        {
            return expandedIDs.ToArray();
        }

        virtual public void SetExpandedIDs(TIdentifier[] ids)
        {
            expandedIDs = new List<TIdentifier>(ids);
            expandedIDs.Sort();
            m_NeedRefreshRows = true;
            OnExpandedStateChanged();
        }

        virtual public bool IsExpanded(TIdentifier id)
        {
            return expandedIDs.BinarySearch(id) >= 0;
        }

        virtual public bool SetExpanded(TIdentifier id, bool expand)
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

        virtual public void SetExpandedWithChildren(TIdentifier id, bool expand)
        {
            SetExpandedWithChildren(FindItem(id), expand);
        }

        virtual public void SetExpandedWithChildren(TreeViewItem<TIdentifier> fromItem, bool expand)
        {
            if (fromItem == null)
            {
                Debug.LogError("item is null");
                return;
            }

            HashSet<TIdentifier> parents = new HashSet<TIdentifier>();
            TreeViewUtility<TIdentifier>.GetParentsBelowItem(fromItem, parents);

            // Get existing expanded in hashset
            HashSet<TIdentifier> oldExpandedSet = new HashSet<TIdentifier>(expandedIDs);

            if (expand)
                oldExpandedSet.UnionWith(parents);
            else
                oldExpandedSet.ExceptWith(parents);

            // Bulk set expanded ids (is sorted in SetExpandedIDs)
            SetExpandedIDs(oldExpandedSet.ToArray());
        }

        virtual public void SetExpanded(TreeViewItem<TIdentifier> item, bool expand)
        {
            SetExpanded(item.id, expand);
        }

        virtual public bool IsExpanded(TreeViewItem<TIdentifier> item) => IsExpandedInternal(item);
        virtual public bool IsExpandedInternal(TreeViewItem<TIdentifier> item)
        {
            return IsExpanded(item.id);
        }

        virtual public bool IsExpandable(TreeViewItem<TIdentifier> item) => IsExpandableInternal(item);
        virtual public bool IsExpandableInternal(TreeViewItem<TIdentifier> item)
        {
            // Ignore expansion (foldout arrow) when showing search results
            if (m_TreeView.isSearching)
                return false;
            return item.hasChildren;
        }

        virtual public bool CanBeMultiSelected(TreeViewItem<TIdentifier> item)
        {
            return true;
        }

        virtual public bool CanBeParent(TreeViewItem<TIdentifier> item) => CanBeParentInternal(item);
        virtual public bool CanBeParentInternal(TreeViewItem<TIdentifier> item)
        {
            return true;
        }

        virtual public List<TIdentifier> GetNewSelection(TreeViewItem<TIdentifier> clickedItem, TreeViewSelectState<TIdentifier> selectState)
        {
            // Get ids from items
            var visibleRows = GetRows();
            List<TIdentifier> allIDs = new List<TIdentifier>(visibleRows.Count);
            for (int i = 0; i < visibleRows.Count; ++i)
                allIDs.Add(visibleRows[i].id);

            bool allowMultiselection = CanBeMultiSelected(clickedItem);

            // todo: add support for other types of TIdentifier, Importantly 'InstanceID'
            if (clickedItem.id is int clickedIntID && selectState.lastClickedID is int lastClickedIntID)
                return  InternalEditorUtility.GetNewSelection(
                    clickedIntID, allIDs as List<int>,
                    selectState.selectedIDs as List<int>,
                    lastClickedIntID, selectState.keepMultiSelection, selectState.useShiftAsActionKey, allowMultiselection
                ) as List<TIdentifier>;

            throw new System.NotImplementedException("InternalEditorUtility.GetNewSelection not implemented for type " + clickedItem.id.GetType());
        }

        virtual public void OnExpandedStateChanged()
        {
            if (m_TreeView.expandedStateChanged != null)
                m_TreeView.expandedStateChanged();
        }

        //----------------------------
        // Renaming section

        virtual public bool IsRenamingItemAllowed(TreeViewItem<TIdentifier> item)
        {
            return true;
        }

        //----------------------------
        // Insert tempoary Item section

        // Fake Item should be inserted into the m_VisibleRows (not the tree itself).
        virtual public void InsertFakeItem(TIdentifier id, TIdentifier parentID, string name, Texture2D icon)
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
            int index = TreeViewController<TIdentifier>.GetIndexOfID(visibleRows, m_FakeItem.id);
            if (index != -1)
            {
                visibleRows.RemoveAt(index);
            }
            m_FakeItem = null;
        }
    }
}
