// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

    internal class AssetsTreeViewDataSource : LazyTreeViewDataSource<EntityId>
    {
        private class RootItem
        {
            public EntityId instanceID { get; }
            public string displayName { get; }
            public string path { get; }
            public bool skipValidation { get; }

            public RootItem(EntityId instanceID, string displayName, string path, bool skipValidation = false)
            {
                this.instanceID = instanceID;
                this.displayName = displayName;
                this.path = path;
                this.skipValidation = skipValidation;
            }
        }

        public bool skipHiddenPackages { get; set; }
        public bool foldersOnly { get; set; }
        public bool foldersFirst { get; set; }

        private Dictionary<string, TreeViewItem<EntityId>> m_RootsTreeViewItem;
        private bool m_ExpandAtFirstTime;
        private List<RootItem> m_Roots;
        private EntityId m_rootInstanceID;

        const HierarchyType k_HierarchyType = HierarchyType.Assets;

        public AssetsTreeViewDataSource(TreeViewController<EntityId> treeView, bool skipHidden = true)
            : base(treeView)
        {
            m_ExpandAtFirstTime = true;
            showRootItem = false;
            rootIsCollapsable = false;
            skipHiddenPackages = skipHidden;
        }

        public AssetsTreeViewDataSource(TreeViewController<EntityId> treeView, EntityId rootInstanceID)
            : this(treeView)
        {
            m_rootInstanceID = rootInstanceID;
        }

        static string CreateDisplayName(EntityId entityId)
        {
            return Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(entityId));
        }

        private void BuildRoots()
        {
            m_Roots = new List<RootItem>();

            if (m_rootInstanceID != EntityId.None)
            {
                m_Roots.Add(new RootItem(m_rootInstanceID, null, null, true));
                return;
            }

            var packagesMountPoint = PackageManager.Folders.GetPackagesPath();

            var assetsFolderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyEntityId("Assets");
            m_Roots.Add(new RootItem(assetsFolderInstanceID, null, null, true));

            var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(skipHiddenPackages);
            m_Roots.Add(new RootItem(ProjectBrowser.kPackagesFolderInstanceId, packagesMountPoint, packagesMountPoint, true));
            foreach (var package in packages)
            {
                var displayName = !string.IsNullOrEmpty(package.displayName) ? package.displayName : package.name;
                var packageFolderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(package.assetPath);
                if (packageFolderInstanceID == EntityId.None)
                    continue;

                m_Roots.Add(new RootItem(packageFolderInstanceID, displayName, package.assetPath));
            }
        }

        public override void ReloadData()
        {
            BuildRoots();
            base.ReloadData();
        }

        public override void FetchData()
        {
            // Create root Item
            int depth = 0;
            var multiRoot = (m_Roots.Count > 1);
            if (multiRoot)
            {
                m_RootItem = new TreeViewItem<EntityId>(EntityId.None, depth, null, "Invisible Root Item");
                SetExpanded(m_RootItem, true);
            }
            else
            {
                var rootInstanceID = m_Roots[0].instanceID;
                var displayName = m_Roots[0].displayName ?? CreateDisplayName(rootInstanceID);
                m_RootItem = new TreeViewItem<EntityId>(rootInstanceID, depth, null, displayName);
                SetExpanded(m_RootItem, true);
            }

            m_Rows = new List<TreeViewItem<EntityId>>(m_Roots.Count * 256);
            Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);
            var assetsInstanceIDs = AssetDatabase.GetMainAssetOrInProgressProxyEntityId("Assets");
            var projectPath = Path.GetFileName(Directory.GetCurrentDirectory());

            // Fetch root Items
            m_RootsTreeViewItem = new Dictionary<string, TreeViewItem<EntityId>>(m_Roots.Count);
            foreach (var root in m_Roots)
            {
                var rootInstanceID = root.instanceID;
                var displayName = root.displayName ?? CreateDisplayName(rootInstanceID);
                var rootPath = root.path ?? AssetDatabase.GetAssetPath(rootInstanceID);
                var rootGuid = AssetDatabase.AssetPathToGUID(rootPath);

                var property = new HierarchyIterator(rootPath);
                if (!root.skipValidation && !property.Find(rootInstanceID, null))
                {
                    if (rootInstanceID == EntityId.None)
                        Debug.LogError("Root Asset with path " + rootPath + " not valid!!");
                    continue;
                }

                var minDepth = property.depth;
                var subDepth = multiRoot ? 0 : -1;
                TreeViewItem<EntityId> rootItem;
                if (multiRoot)
                {
                    var parentItem = m_RootItem;
                    var rootDepth = minDepth;
                    rootDepth++;

                    // Find parent treeView item
                    var parentPath = Path.GetDirectoryName(rootPath);
                    // Mono Upgrade note: Directory.GetParent now attempts to resolve the full path as part of the call. This causes Unity's
                    // path remapping to be triggered which causes Package paths to get remapped to PackageCache.
                    if (String.IsNullOrEmpty(parentPath))
                        parentPath = Directory.GetParent(rootPath).Name;
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
                        rootItem = new FolderTreeItem(rootGuid, !property.hasChildren, rootInstanceID, rootDepth, parentItem, displayName);
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

                if (!skipHiddenPackages)
                    property.SetSearchFilter(new SearchFilter {skipHidden = false});

                var expandIDs = GetExpandedIDs();
                var rows = new List<TreeViewItem<EntityId>>();
                bool shouldExpandIt = m_ExpandAtFirstTime && (rootItem.id == assetsInstanceIDs);
                if (IsExpanded(rootItem.id) && (rootItem == m_RootItem || IsExpanded(rootItem.parent.id)) || shouldExpandIt)
                {
                    m_ExpandAtFirstTime = false;

                    while (property.NextWithDepthCheck(expandIDs, minDepth))
                    {
                        if (!foldersOnly || property.isFolder)
                        {
                            depth = property.depth - minDepth;
                            TreeViewItem<EntityId> item;
                            if (property.isFolder)
                                item = new FolderTreeItem(property.guid, !property.hasChildren, property.entityId, depth + subDepth, null, property.name);
                            else
                                item = new NonFolderTreeItem(property.guid, property.GetEntityIdIfImported(), depth + subDepth, null, property.name);

                            item.icon = property.icon;

                            if (property.hasChildren)
                            {
                                item.AddChild(null); // add a dummy child in children list to ensure we show the collapse arrow (because we do not fetch data for collapsed items)
                            }
                            rows.Add(item);
                        }
                    }

                    // Setup reference between child and parent items
                    TreeViewUtility<EntityId>.SetChildParentReferences(rows, rootItem);
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

                ((List<TreeViewItem<EntityId>>)m_Rows).AddRange(rows);
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
            m_TreeView.SetSelection(Selection.entityIds, frameLastSelected);
        }

        static void FoldersFirstRecursive(TreeViewItem<EntityId> item)
        {
            if (!item.hasChildren)
                return;

            // Parent child relation is untouched, we simply move child folders to the beginning of
            // the children array while keeping folders and files sorted.
            var children = item.children.ToArray();
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

                        var folderItem = children[folderPos];
                        int length = folderPos - nonFolderPos;
                        System.Array.Copy(children, nonFolderPos, children, nonFolderPos + 1, length);
                        children[nonFolderPos] = folderItem;
                        break;
                    }
                }

                FoldersFirstRecursive(children[nonFolderPos]);
            }
            item.children = new List<TreeViewItem<EntityId>>(children);
        }

        protected override void GetParentsAbove(EntityId id, HashSet<EntityId> parentsAbove)
        {
            ProjectWindowUtil.GetAncestors(id, parentsAbove);
        }

        // Should return the items that have children from id and below
        protected override void GetParentsBelow(EntityId id, HashSet<EntityId> parentsBelow)
        {
            // Add all children expanded ids to hashset
            if (m_Roots.Count > 1)
            {
                var assetsInstanceIDs = AssetDatabase.GetMainAssetOrInProgressProxyEntityId("Assets");
                if (id != assetsInstanceIDs)
                {
                    // Search in created first-level root items
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var item = m_RootsTreeViewItem.Values.FirstOrDefault(tvi => tvi.id == id && tvi.depth == 0);
#pragma warning restore RS0030
                    if (item != null)
                    {
                        parentsBelow.Add(id);
                        foreach (var child in item.children)
                        {
                            if (child != null && child.hasChildren)
                                GetParentsBelow(child.id, parentsBelow);
                        }
                        return;
                    }
                }
            }

            // Search in all roots
            foreach (var root in m_Roots)
            {
                var rootPath = root.path ?? AssetDatabase.GetAssetPath(root.instanceID);
                var search = new HierarchyIterator(rootPath);
                if (search.Find(id, null))
                {
                    parentsBelow.Add(id);

                    int depth = search.depth;
                    while (search.Next(null) && search.depth > depth)
                    {
                        if (search.hasChildren)
                            parentsBelow.Add(search.entityId);
                    }
                    break;
                }
            }
        }

        override public void OnExpandedStateChanged()
        {
            if (k_HierarchyType == HierarchyType.Assets)
                InternalEditorUtility.expandedProjectWindowItemIds = expandedIDs.ToArray(); // Persist expanded state for ProjectBrowsers

            base.OnExpandedStateChanged();
        }

        override public bool IsRenamingItemAllowed(TreeViewItem<EntityId> item)
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

        public EntityId GetInsertAfterItemIDForNewItem(string newName, TreeViewItem<EntityId> parentItem, bool isCreatingNewFolder, bool foldersFirst)
        {
            if (!parentItem.hasChildren)
                return parentItem.id;

            // Find pos under parent
            EntityId insertAfterID = parentItem.id;
            for (int idx = 0; idx < parentItem.children.Count; ++idx)
            {
                EntityId entityId = parentItem.children[idx].id;
                bool isFolder = parentItem.children[idx] is FolderTreeItem;

                // Skip folders when inserting a normal asset if folders is sorted first
                if (foldersFirst && isFolder && !isCreatingNewFolder)
                {
                    insertAfterID = entityId;
                    continue;
                }

                // When inserting a folder in folders first list break when we reach normal assets
                if (foldersFirst && !isFolder && isCreatingNewFolder)
                {
                    break;
                }

                // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
                string propertyPath = AssetDatabase.GetAssetPath(entityId);
                if (EditorUtility.NaturalCompare(Path.GetFileNameWithoutExtension(propertyPath), newName) > 0)
                {
                    break;
                }

                insertAfterID = entityId;
            }
            return insertAfterID;
        }

        override public void InsertFakeItem(EntityId id, EntityId parentID, string name, Texture2D icon)
        {
            bool isCreatingNewFolder = GetCreateAssetUtility().endAction is DoCreateFolder;

            var checkItem = FindItem(id);
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

                TreeViewItem<EntityId> parentItem;
                int parentIndex = TreeViewController<EntityId>.GetIndexOfID(visibleRows, parentID);
                if (parentIndex >= 0)
                    parentItem = visibleRows[parentIndex];
                else
                    parentItem = m_RootItem; // Fallback to root Item as parent

                // Create fake folder for insertion
                int indentLevel = parentItem.depth + (parentItem == m_RootItem ? 0 : 1);
                m_FakeItem = new TreeViewItem<EntityId>(id, indentLevel, parentItem, name);
                m_FakeItem.icon = icon;

                // Find pos under parent
                EntityId insertAfterID = GetInsertAfterItemIDForNewItem(name, parentItem, isCreatingNewFolder, foldersFirst);

                // Find pos in expanded rows and insert
                int index = TreeViewController<EntityId>.GetIndexOfID(visibleRows, insertAfterID);
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

        internal class SemiNumericDisplayNameListComparer : IComparer<TreeViewItem<EntityId>>
        {
            public int Compare(TreeViewItem<EntityId> x, TreeViewItem<EntityId> y)
            {
                if (x == y) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                return EditorUtility.NaturalCompare(x.displayName, y.displayName);
            }
        }

        // Classes used for type checking
        internal interface IAssetTreeViewItem
        {
            string Guid { get; }
        }

        internal abstract class FolderTreeItemBase : TreeViewItem<EntityId>
        {
            public virtual bool IsEmpty
            {
                get { return false; }
            }
            protected FolderTreeItemBase(EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }

        internal class RootTreeItem : FolderTreeItemBase
        {
            public RootTreeItem(EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }
        internal class PackageTreeItem : FolderTreeItemBase
        {
            public PackageTreeItem(EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName)
                : base(id, depth, parent, displayName)
            {
            }
        }

        internal class FolderTreeItem : FolderTreeItemBase, IAssetTreeViewItem
        {
            public string Guid { get; }
            public override bool IsEmpty { get; }

            public FolderTreeItem(string guid, bool isEmpty, EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                Guid = guid;
                IsEmpty = isEmpty;
            }
        }

        class NonFolderTreeItem : TreeViewItem<EntityId>, IAssetTreeViewItem
        {
            public string Guid { get; }
            public NonFolderTreeItem(string guid, EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                Guid = guid;
            }
        }
    }
}
