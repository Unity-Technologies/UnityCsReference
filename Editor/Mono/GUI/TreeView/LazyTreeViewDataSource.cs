// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;


namespace UnityEditor.IMGUI.Controls
{
    // LazyTreeViewDataSource assumes that the Item tree only contains visible items, optimal for large data sets.
    // Usage:
    //    - Override FetchData () and build the tree with visible items with m_RootItem as root  (and and populate the m_VisibleRows List)
    //    - FetchData () is called every time the expanded state changes.
    //    - Configure showRootItem and rootIsCollapsable as wanted
    //
    // Note: if dealing with small trees consider using TreeViewDataSource instead: it assumes that the tree contains all items.

    internal abstract class LazyTreeViewDataSource : TreeViewDataSource
    {
        public LazyTreeViewDataSource(TreeViewController treeView)
            : base(treeView)
        {
        }

        public static List<TreeViewItem> CreateChildListForCollapsedParent()
        {
            // To mark a collapsed parent we use a list with one element that is null.
            // The null element in the children list ensures we show the collapse arrow.
            return new List<TreeViewItem>() { null };
        }

        public static bool IsChildListForACollapsedParent(IList<TreeViewItem> childList)
        {
            return (childList != null && childList.Count == 1 && childList[0] == null); // see CreateChildListForCollapsedParent
        }

        // Return all ancestor items of the Item with 'id'
        protected abstract HashSet<int> GetParentsAbove(int id);

        // Return all descendant items that have children from the Item with 'id'
        protected abstract HashSet<int> GetParentsBelow(int id);

        override public void RevealItem(int itemID)
        {
            // Get existing expanded in hashset
            HashSet<int> expandedSet = new HashSet<int>(expandedIDs);
            int orgSize = expandedSet.Count;

            // Get all parents above id
            HashSet<int> candidates = GetParentsAbove(itemID);

            // Add parent ids
            expandedSet.UnionWith(candidates);

            if (orgSize != expandedSet.Count)
            {
                // Bulk set expanded ids (is sorted in SetExpandedIDs)
                SetExpandedIDs(expandedSet.ToArray());

                // Refresh immediately if any Item was expanded
                if (m_NeedRefreshRows)
                    FetchData();
            }
        }

        override public TreeViewItem FindItem(int itemID)
        {
            // Since this is a LazyTreeViewDataSource that only knows about expanded items
            // we need to reveal the item before searching for it (expand its ancestors)
            RevealItem(itemID);

            // Now find the item after we have expanded and created parent items
            return base.FindItem(itemID);
        }

        override public void SetExpandedWithChildren(TreeViewItem item, bool expand)
        {
            SetExpandedWithChildren(item.id, expand);
        }

        // Override for special handling of recursion
        // We cannot recurse normally to tree Item children because we have not loaded children of collapsed items
        // therefore let client implement GetParentsBelow to fetch ids instead
        override public void SetExpandedWithChildren(int id, bool expand)
        {
            // Get existing expanded in hashset
            HashSet<int> oldExpandedSet = new HashSet<int>(expandedIDs);

            // Add all children expanded ids to hashset
            HashSet<int> candidates = GetParentsBelow(id);

            if (expand)     oldExpandedSet.UnionWith(candidates);
            else            oldExpandedSet.ExceptWith(candidates);

            // Bulk set expanded ids (is sorted in SetExpandedIDs)
            SetExpandedIDs(oldExpandedSet.ToArray());

            // Keep for debugging
            // Debug.Log ("New expanded state (bulk): " + DebugUtils.ListToString(new List<int>(expandedIDs)));
        }

        public override void InitIfNeeded()
        {
            // Cached for large trees...
            if (m_Rows == null || m_NeedRefreshRows)
            {
                FetchData(); // Only need to fetch visible data..

                m_NeedRefreshRows = false;

                if (onVisibleRowsChanged != null)
                    onVisibleRowsChanged();

                m_TreeView.Repaint();
            }
        }

        // Get the flattened tree of visible items. Use GetFirstAndLastRowVisible to cull invisible items
        override public IList<TreeViewItem> GetRows()
        {
            InitIfNeeded();
            return m_Rows;
        }
    }
}
