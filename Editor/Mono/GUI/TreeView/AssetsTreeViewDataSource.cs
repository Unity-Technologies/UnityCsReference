// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Experimental;

namespace UnityEditor
{
    // AssetsTreeViewDataSource only fetches current visible items of the asset database tree, because we derive from LazyTreeViewDataSource
    // Note: every time a Item's expanded state changes FetchData is called

    internal class AssetsTreeViewDataSource : LazyTreeViewDataSource
    {
        public class RootItem
        {
            public int instanceID { get; }
            public string displayName { get; }
            public string path { get; }
            public bool skipValidation { get; }

            public RootItem(int instanceID, string displayName, string path, bool skipValidation = false)
            {
                this.instanceID = instanceID;
                this.displayName = displayName;
                this.path = path;
                this.skipValidation = skipValidation;
            }
        }

        public bool foldersOnly { get; set; }
        public bool foldersFirst { get; set; }
        private List<RootItem> m_Roots;
        private Dictionary<string, TreeViewItem> m_RootsTreeViewItem;
        private bool m_ExpandAtFirstTime;
        const HierarchyType k_HierarchyType = HierarchyType.Assets;

        private AssetsTreeViewDataSource(TreeViewController treeView)
            : base(treeView)
        {
            m_ExpandAtFirstTime = true;
            showRootItem = false;
            rootIsCollapsable = false;
        }

        public AssetsTreeViewDataSource(TreeViewController treeView, List<RootItem> roots)
            : this(treeView)
        {
            m_Roots = roots;
        }

        public AssetsTreeViewDataSource(TreeViewController treeView, int rootInstanceID)
            : this(treeView)
        {
            m_Roots = new List<RootItem>();
            m_Roots.Add(new RootItem(rootInstanceID, null, null));
        }

        static string CreateDisplayName(int instanceID)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(instanceID));
        }

        public override void FetchData()
        {
            // Create root Item
            int depth = 0;
            var multiRoot = (m_Roots.Count > 1);
            if (multiRoot)
            {
                m_RootItem = new TreeViewItem(-1, depth, null, "Invisible Root Item");
                SetExpanded(m_RootItem, true);
            }
            else
            {
                var rootInstanceID = m_Roots[0].instanceID;
                var displayName = m_Roots[0].displayName ?? CreateDisplayName(rootInstanceID);
                m_RootItem = new TreeViewItem(rootInstanceID, depth, null, displayName);
                SetExpanded(m_RootItem, true);
            }

            m_Rows = new List<TreeViewItem>(m_Roots.Count * 256);
            Texture2D emptyFolderIcon = EditorGUIUtility.FindTexture(EditorResources.emptyFolderIconName);
            Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);
            var assetsInstanceIDs = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets");
            var projectPath = Path.GetFileName(Directory.GetCurrentDirectory());

            // Fetch root Items
            m_RootsTreeViewItem = new Dictionary<string, TreeViewItem>(m_Roots.Count);
            foreach (var root in m_Roots)
            {
                var rootInstanceID = root.instanceID;
                var displayName = root.displayName ?? CreateDisplayName(rootInstanceID);
                var rootPath = root.path ?? AssetDatabase.GetAssetPath(rootInstanceID);

                var property = new HierarchyProperty(rootPath);
                if (!root.skipValidation && !property.Find(rootInstanceID, null))
                {
                    Debug.LogError("Root Asset with id " + rootInstanceID + " not valid!!");
                    continue;
                }

                var minDepth = property.depth;
                var subDepth = multiRoot ? 0 : -1;
                TreeViewItem rootItem;
                if (multiRoot)
                {
                    var parentItem = m_RootItem;
                    var rootDepth = minDepth;
                    rootDepth++;

                    // Find parent treeView item
                    var parentPath = Directory.GetParent(rootPath).Name;
                    if (parentPath != projectPath)
                    {
                        if (!m_RootsTreeViewItem.TryGetValue(parentPath, out parentItem))
                        {
                            Debug.LogError("Cannot find parent for " + rootInstanceID);
                            continue;
                        }

                        rootDepth++;
                        subDepth++;
                    }

                    // Create root item TreeView item
                    if (subDepth > 0)
                        rootItem = new FolderTreeItem(rootInstanceID, rootDepth, parentItem, displayName);
                    else
                        rootItem = new RootTreeItem(rootInstanceID, rootDepth, parentItem, displayName);
                    rootItem.icon = folderIcon;
                    parentItem.AddChild(rootItem);
                }
                else
                {
                    rootItem = m_RootItem;
                }

                m_RootsTreeViewItem[rootPath] = rootItem;

                var expandIDs = GetExpandedIDs();
                var rows = new List<TreeViewItem>();
                bool shouldExpandIt = m_ExpandAtFirstTime && (rootItem.id == assetsInstanceIDs);
                if (IsExpanded(rootItem.id) && (rootItem == m_RootItem || IsExpanded(rootItem.parent.id)) || shouldExpandIt)
                {
                    m_ExpandAtFirstTime = false;

                    while (property.NextWithDepthCheck(expandIDs, minDepth))
                    {
                        if (!foldersOnly || property.isFolder)
                        {
                            depth = property.depth - minDepth;
                            TreeViewItem item;
                            if (property.isFolder)
                                item = new FolderTreeItem(property.instanceID, depth + subDepth, null, property.name);
                            else
                                item = new NonFolderTreeItem(property.instanceID, depth + subDepth, null, property.name);

                            if (property.isFolder && !property.hasChildren)
                                item.icon = emptyFolderIcon;
                            else
                                item.icon = property.icon;

                            if (property.hasChildren)
                            {
                                item.AddChild(null); // add a dummy child in children list to ensure we show the collapse arrow (because we do not fetch data for collapsed items)
                            }
                            rows.Add(item);
                        }
                    }

                    // Setup reference between child and parent items
                    TreeViewUtility.SetChildParentReferences(rows, rootItem);
                }
                else
                {
                    rootItem.AddChild(null);
                }

                if (shouldExpandIt && !IsExpanded(rootItem))
                    SetExpanded(rootItem, true);

                if (multiRoot && IsExpanded(rootItem.parent.id))
                {
                    m_Rows.Add(rootItem);
                }

                ((List<TreeViewItem>)m_Rows).AddRange(rows);
            }

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

            if (m_Roots.Count > 1)
            {
                var assetsInstanceIDs = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets");
                if (id != assetsInstanceIDs)
                {
                    // Search in created first-level root items
                    var item = m_RootsTreeViewItem.Values.FirstOrDefault(tvi => tvi.id == id && tvi.depth == 0);
                    if (item != null)
                    {
                        parentsBelow.Add(id);
                        foreach (var child in item.children)
                        {
                            if (child != null && child.hasChildren)
                                parentsBelow.UnionWith(GetParentsBelow(child.id));
                        }
                        return parentsBelow;
                    }
                }
            }

            // Search in all roots
            foreach (var root in m_Roots)
            {
                var rootPath = root.path ?? AssetDatabase.GetAssetPath(root.instanceID);
                IHierarchyProperty search = new HierarchyProperty(rootPath);
                if (search.Find(id, null))
                {
                    parentsBelow.Add(id);

                    int depth = search.depth;
                    while (search.Next(null) && search.depth > depth)
                    {
                        if (search.hasChildren)
                            parentsBelow.Add(search.instanceID);
                    }
                    break;
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
            // Root items cannot be renamed
            if (AssetDatabase.IsSubAsset(item.id) || (item.parent == null))
                return false;

            // Root items cannot be renamed
            if (item is RootTreeItem)
                return false;

            return InternalEditorUtility.CanRenameAsset(item.id);
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
        internal class RootTreeItem : TreeViewItem
        {
            public RootTreeItem(int id, int depth, TreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }
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
