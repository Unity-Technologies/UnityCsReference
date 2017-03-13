// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEditorInternal;


namespace UnityEditor
{
    // AssetsTreeViewDataSource only fetches current visible items of the asset database tree, because we derive from LazyTreeViewDataSource
    // Note: every time a Item's expanded state changes FetchData is called

    internal class AssetsTreeViewDataSource : LazyTreeViewDataSource
    {
        public bool foldersOnly { get; set; }
        public bool foldersFirst { get; set; }
        readonly int m_RootInstanceID;
        const HierarchyType k_HierarchyType = HierarchyType.Assets;

        public AssetsTreeViewDataSource(TreeViewController treeView, int rootInstanceID, bool showRootItem, bool rootItemIsCollapsable)
            : base(treeView)
        {
            m_RootInstanceID = rootInstanceID;
            ((TreeViewDataSource)this).showRootItem = showRootItem;
            rootIsCollapsable = rootItemIsCollapsable;
        }

        static string CreateDisplayName(int instanceID)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(instanceID));
        }

        public override void FetchData()
        {
            // Create root Item
            int depth = 0;
            m_RootItem = new TreeViewItem(m_RootInstanceID, depth, null, CreateDisplayName(m_RootInstanceID));
            if (!showRootItem)
                SetExpanded(m_RootItem, true);

            // Find start Item
            IHierarchyProperty property = new HierarchyProperty(k_HierarchyType);
            property.Reset();
            bool found = property.Find(m_RootInstanceID, null);
            if (!found)
                Debug.LogError("Root Asset with id " + m_RootInstanceID + " not found!!");

            int minDepth = property.depth + (showRootItem ? 0 : 1);
            int[] expanded = expandedIDs.ToArray();
            Texture2D emptyFolderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.emptyFolderIconName);

            // Fetch visible items
            m_Rows = new List<TreeViewItem>();
            while (property.NextWithDepthCheck(expanded, minDepth))
            {
                if (!foldersOnly || property.isFolder)
                {
                    depth = property.depth - minDepth;
                    TreeViewItem item;
                    if (property.isFolder)
                        item = new FolderTreeItem(property.instanceID, depth, null, property.name);
                    else
                        item = new NonFolderTreeItem(property.instanceID, depth, null, property.name);

                    if (property.isFolder && !property.hasChildren)
                        item.icon = emptyFolderIcon;
                    else
                        item.icon = property.icon;

                    if (property.hasChildren)
                    {
                        item.AddChild(null); // add a dummy child in children list to ensure we show the collapse arrow (because we do not fetch data for collapsed items)
                    }
                    m_Rows.Add(item);
                }
            }

            // Setup reference between child and parent items
            TreeViewUtility.SetChildParentReferences(m_Rows, m_RootItem);

            if (foldersFirst)
            {
                FoldersFirstRecursive(m_RootItem);
                m_Rows.Clear();
                GetVisibleItemsRecursive(m_RootItem, m_Rows);
            }

            // Must be called before InitSelection (it calls GetVisibleItems)
            m_NeedRefreshRows = false;

            // We want to reset selection on copy/duplication/delete
            bool frameLastSelected = false; // use false because we might just be expanding/collapsing a Item (which would prevent collapsing a Item with a selected child)
            m_TreeView.SetSelection(Selection.instanceIDs, frameLastSelected);
        }

        static void FoldersFirstRecursive(TreeViewItem item)
        {
            if (!item.hasChildren)
                return;

            // Parent child relation is untouched, we simply move child folders to the beginning of
            // the children array while keeping folders and files sorted.
            TreeViewItem[] children = item.children.ToArray();
            for (int nonFolderPos = 0; nonFolderPos < item.children.Count; ++nonFolderPos)
            {
                if (children[nonFolderPos] == null)
                    continue;

                if (children[nonFolderPos] is NonFolderTreeItem)
                {
                    for (int folderPos = nonFolderPos + 1; folderPos < children.Length; ++folderPos)
                    {
                        if (!(children[folderPos] is FolderTreeItem))
                            continue;

                        TreeViewItem folderItem = children[folderPos];
                        int length = folderPos - nonFolderPos;
                        System.Array.Copy(children, nonFolderPos, children, nonFolderPos + 1, length);
                        children[nonFolderPos] = folderItem;
                        break;
                    }
                }

                FoldersFirstRecursive(children[nonFolderPos]);
            }
            item.children = new List<TreeViewItem>(children);
        }

        protected override HashSet<int> GetParentsAbove(int id)
        {
            int[] ancestorsInstanceIDs = ProjectWindowUtil.GetAncestors(id);
            return new HashSet<int>(ancestorsInstanceIDs);
        }

        // Should return the items that have children from id and below
        protected override HashSet<int> GetParentsBelow(int id)
        {
            // Add all children expanded ids to hashset
            HashSet<int> parentsBelow = new HashSet<int>();
            IHierarchyProperty search = new HierarchyProperty(k_HierarchyType);
            if (search.Find(id, null))
            {
                parentsBelow.Add(id);

                int depth = search.depth;
                while (search.Next(null) && search.depth > depth)
                {
                    if (search.hasChildren)
                        parentsBelow.Add(search.instanceID);
                }
            }
            return parentsBelow;
        }

        override public void OnExpandedStateChanged()
        {
            if (k_HierarchyType == HierarchyType.Assets)
                InternalEditorUtility.expandedProjectWindowItems = expandedIDs.ToArray(); // Persist expanded state for ProjectBrowsers

            base.OnExpandedStateChanged();
        }

        override public bool IsRenamingItemAllowed(TreeViewItem item)
        {
            // Only main representations can be renamed (currently)
            if (AssetDatabase.IsSubAsset(item.id))
                return false;

            // All items can be renamed except from the root Item
            return item.parent != null;
        }

        protected CreateAssetUtility GetCreateAssetUtility()
        {
            return ((TreeViewStateWithAssetUtility)m_TreeView.state).createAssetUtility;
        }

        public int GetInsertAfterItemIDForNewItem(string newName, TreeViewItem parentItem, bool isCreatingNewFolder, bool foldersFirst)
        {
            if (!parentItem.hasChildren)
                return parentItem.id;

            // Find pos under parent
            int insertAfterID = parentItem.id;
            for (int idx = 0; idx < parentItem.children.Count; ++idx)
            {
                int instanceID = parentItem.children[idx].id;
                bool isFolder = parentItem.children[idx] is FolderTreeItem;

                // Skip folders when inserting a normal asset if folders is sorted first
                if (foldersFirst && isFolder && !isCreatingNewFolder)
                {
                    insertAfterID = instanceID;
                    continue;
                }

                // When inserting a folder in folders first list break when we reach normal assets
                if (foldersFirst && !isFolder && isCreatingNewFolder)
                {
                    break;
                }

                // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
                string propertyPath = AssetDatabase.GetAssetPath(instanceID);
                if (EditorUtility.NaturalCompare(Path.GetFileNameWithoutExtension(propertyPath), newName) > 0)
                {
                    break;
                }

                insertAfterID = instanceID;
            }
            return insertAfterID;
        }

        override public void InsertFakeItem(int id, int parentID, string name, Texture2D icon)
        {
            bool isCreatingNewFolder = GetCreateAssetUtility().endAction is DoCreateFolder;

            TreeViewItem checkItem = FindItem(id);
            if (checkItem != null)
            {
                Debug.LogError("Cannot insert fake Item because id is not unique " + id + " Item already there: " + checkItem.displayName);
                return;
            }

            if (FindItem(parentID) != null)
            {
                // Ensure parent Item's children is visible
                SetExpanded(parentID, true);

                var visibleRows = GetRows();

                TreeViewItem parentItem;
                int parentIndex = TreeViewController.GetIndexOfID(visibleRows, parentID);
                if (parentIndex >= 0)
                    parentItem = visibleRows[parentIndex];
                else
                    parentItem = m_RootItem; // Fallback to root Item as parent

                // Create fake folder for insertion
                int indentLevel = parentItem.depth + (parentItem == m_RootItem ? 0 : 1);
                m_FakeItem = new TreeViewItem(id, indentLevel, parentItem, name);
                m_FakeItem.icon = icon;

                // Find pos under parent
                int insertAfterID = GetInsertAfterItemIDForNewItem(name, parentItem, isCreatingNewFolder, foldersFirst);

                // Find pos in expanded rows and insert
                int index = TreeViewController.GetIndexOfID(visibleRows, insertAfterID);
                if (index >= 0)
                {
                    // Ensure to bypass all children of 'insertAfterID'
                    while (++index < visibleRows.Count)
                    {
                        if (visibleRows[index].depth <= indentLevel)
                            break;
                    }

                    if (index < visibleRows.Count)
                        visibleRows.Insert(index, m_FakeItem);
                    else
                        visibleRows.Add(m_FakeItem);
                }
                else
                {
                    // not visible parent: insert as first
                    if (visibleRows.Count > 0)
                        visibleRows.Insert(0, m_FakeItem);
                    else
                        visibleRows.Add(m_FakeItem);
                }

                m_NeedRefreshRows = false;

                m_TreeView.Frame(m_FakeItem.id, true, false);
                m_TreeView.Repaint();
            }
            else
            {
                Debug.LogError("No parent Item found");
            }
        }

        internal class SemiNumericDisplayNameListComparer : IComparer<TreeViewItem>
        {
            public int Compare(TreeViewItem x, TreeViewItem y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return EditorUtility.NaturalCompare(x.displayName, y.displayName);
            }
        }

        // Classes used for type checking
        class FolderTreeItem : TreeViewItem
        {
            public FolderTreeItem(int id, int depth, TreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }
        class NonFolderTreeItem : TreeViewItem
        {
            public NonFolderTreeItem(int id, int depth, TreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }
    }
}
