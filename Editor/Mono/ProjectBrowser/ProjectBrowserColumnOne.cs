// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    // Used for type check
    class SearchFilterTreeItem : TreeViewItem<EntityId>
    {
        bool m_IsFolder;
        public SearchFilterTreeItem(EntityId id, int depth, TreeViewItem<EntityId> parent, string displayName, bool isFolder)
            : base(id, depth, parent, displayName)
        {
            m_IsFolder = isFolder;
        }

        public bool isFolder {get {return m_IsFolder; }}
    }

    //------------------------------------------------
    // GUI section

    internal class ProjectBrowserColumnOneTreeViewGUI : AssetsTreeViewGUI
    {
        const float k_DistBetweenRootTypes = 15f;
        Texture2D k_FavoritesIcon = EditorGUIUtility.FindTexture("Favorite Icon");
        Texture2D k_FavoriteFolderIcon = EditorGUIUtility.FindTexture("FolderFavorite Icon");
        Texture2D k_FavoriteFilterIcon = EditorGUIUtility.FindTexture("Search Icon");
        bool m_IsCreatingSavedFilter = false;

        public ProjectBrowserColumnOneTreeViewGUI(TreeViewController<EntityId> treeView) : base(treeView)
        {
        }

        // ------------------
        // Size section


        override public Vector2 GetTotalSize()
        {
            Vector2 totalSize = base.GetTotalSize();

            totalSize.y += k_DistBetweenRootTypes * 1; // assumes that we have two root

            return totalSize;
        }

        public override Rect GetRowRect(int row, float rowWidth)
        {
            var rows = m_TreeView.data.GetRows();
            return new Rect(0, GetTopPixelOfRow(row, rows), rowWidth, k_LineHeight);
        }

        float GetTopPixelOfRow(int row, IList<TreeViewItem<EntityId>> rows)
        {
            float topPixel = row * k_LineHeight;

            var item = rows[row];

            // Add extra space before Assets root
            var itemType = item is SearchFilterTreeItem ? ProjectBrowser.ItemType.SavedFilter : ProjectBrowser.ItemType.Asset;
            if (itemType == ProjectBrowser.ItemType.Asset)
                topPixel += k_DistBetweenRootTypes;

            return topPixel;
        }

        override public int GetNumRowsOnPageUpDown(TreeViewItem<EntityId> fromItem, bool pageUp, float heightOfTreeView)
        {
            return (int)Mathf.Floor(heightOfTreeView / k_LineHeight) - 1; // -1 is fast fix for space between roots
        }

        // Should return the row number of the first and last row thats fits in the pixel rect defined by top and height
        override public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            float topPixel = m_TreeView.state.scrollPos.y;
            float heightInPixels = m_TreeView.GetTotalRect().height;

            firstRowVisible = (int)Mathf.Floor(topPixel / k_LineHeight);
            lastRowVisible = firstRowVisible + (int)Mathf.Ceil(heightInPixels / k_LineHeight);

            float rowsPerSpaceBetween = k_DistBetweenRootTypes / k_LineHeight;
            firstRowVisible -= (int)Mathf.Ceil(2 * rowsPerSpaceBetween); // for now we just add extra rows to ensure all rows are visible
            lastRowVisible += (int)Mathf.Ceil(2 * rowsPerSpaceBetween);

            firstRowVisible = Mathf.Max(firstRowVisible, 0);
            lastRowVisible = Mathf.Min(lastRowVisible,  m_TreeView.data.rowCount - 1);
        }

        // ------------------
        // Row Gui section

        override public void OnRowGUI(Rect rowRect, TreeViewItem<EntityId> item, int row, bool selected, bool focused)
        {
            bool useBoldFont = IsVisibleRootNode(item);
            DoItemGUI(rowRect, row, item, selected, focused, useBoldFont);
        }

        bool IsVisibleRootNode(TreeViewItem<EntityId> item)
        {
            return (m_TreeView.data as ProjectBrowserColumnOneTreeViewDataSource).IsVisibleRootNode(item);
        }

        protected override Texture GetIconForItem(TreeViewItem<EntityId> item)
        {
            if (item != null && item.icon != null)
            {
                var icon = item.icon;
                var folderItem = item as AssetsTreeViewDataSource.FolderTreeItemBase;
                if (folderItem != null)
                {
                    if (folderItem.IsEmpty)
                        icon = emptyFolderTexture;
                    else if (m_TreeView.data.IsExpanded(folderItem))
                        icon = openFolderTexture;
                }

                return icon;
            }

            SearchFilterTreeItem searchFilterItem = item as SearchFilterTreeItem;
            if (searchFilterItem != null)
            {
                if (IsVisibleRootNode(item))
                    return k_FavoritesIcon;
                if (searchFilterItem.isFolder)
                    return k_FavoriteFolderIcon;
                else
                    return k_FavoriteFilterIcon;
            }
            return base.GetIconForItem(item);
        }

        public static float GetListAreaGridSize()
        {
            float previewSize = -1f;
            if (ProjectBrowser.s_LastInteractedProjectBrowser != null)
                previewSize = ProjectBrowser.s_LastInteractedProjectBrowser.listAreaGridSize;
            return previewSize;
        }

        virtual internal void BeginCreateSavedFilter(SearchFilter filter)
        {
            string savedFilterName = "New Saved Search";

            m_IsCreatingSavedFilter = true;
            int filterId = SavedSearchFilters.AddSavedFilter(savedFilterName, filter, GetListAreaGridSize());
            EntityId entityId = FavoritesEntityIds.instance.GetOrAllocateEntityIdFor(filterId);
            m_TreeView.Frame(entityId, true, false);

            // Start naming the asset
            m_TreeView.state.renameOverlay.BeginRename(savedFilterName, entityId, 0f);
        }

        override protected void RenameEnded()
        {
            var entityId = GetRenameOverlay().userData;
            ProjectBrowser.ItemType type = ProjectBrowser.GetItemType(entityId);

            if (m_IsCreatingSavedFilter)
            {
                // Create saved filter
                m_IsCreatingSavedFilter = false;

                int filterId = FavoritesEntityIds.instance.GetIdFor(entityId);
                if (GetRenameOverlay().userAcceptedRename)
                {
                    SavedSearchFilters.SetName(filterId, GetRenameOverlay().name);
                    m_TreeView.SetSelection(new[] { entityId }, true);
                }
                else
                    SavedSearchFilters.RemoveSavedFilter(filterId);
            }
            else if (type == ProjectBrowser.ItemType.SavedFilter)
            {
                // Renamed saved filter
                if (GetRenameOverlay().userAcceptedRename)
                {
                    int filterId = FavoritesEntityIds.instance.GetIdFor(entityId);
                    SavedSearchFilters.SetName(filterId,  GetRenameOverlay().name);
                }
            }
            else
            {
                // Let base handle renaming of folders
                base.RenameEnded();

                // Ensure to sync filter to new folder name (so we still show the contents of the folder)
                if (GetRenameOverlay().userAcceptedRename)
                    m_TreeView.NotifyListenersThatSelectionChanged();
            }
        }
    }


    //------------------------------------------------
    // DataSource section

    internal class ProjectBrowserColumnOneTreeViewDataSource : LazyTreeViewDataSource<EntityId>
    {
        static string kProjectBrowserString = "ProjectBrowser";
        static Texture2D s_FolderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);

        public bool skipHiddenPackages { get; set; }

        public ProjectBrowserColumnOneTreeViewDataSource(TreeViewController<EntityId> treeView, bool skipHidden) : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
            skipHiddenPackages = skipHidden;
            SavedSearchFilters.AddChangeListener(ReloadData); // We reload on change
        }

        public override bool IsExpandable(TreeViewItem<EntityId> item)
        {
            return item.hasChildren && (item != m_RootItem || rootIsCollapsable);
        }

        public override bool CanBeMultiSelected(TreeViewItem<EntityId> item)
        {
            return ProjectBrowser.GetItemType(item.id) != ProjectBrowser.ItemType.SavedFilter;
        }

        public override bool CanBeParent(TreeViewItem<EntityId> item)
        {
            return !(item is SearchFilterTreeItem) || SavedSearchFilters.AllowsHierarchy();
        }

        public bool IsVisibleRootNode(TreeViewItem<EntityId> item)
        {
            // The main root Item is invisible the next level is visible root items
            return (item.parent != null && item.parent.parent == null);
        }

        public override bool IsRenamingItemAllowed(TreeViewItem<EntityId> item)
        {
            // The 'Assets' root and 'Filters' roots are not allowed to be renamed
            if (IsVisibleRootNode(item))
                return false;

            switch (ProjectBrowser.GetItemType(item.id))
            {
                case ProjectBrowser.ItemType.Asset:
                    return InternalEditorUtility.CanRenameAsset(item.id);
                case ProjectBrowser.ItemType.SavedFilter:
                    return true;
                default:
                    return false;
            }
        }

        public override void FetchData()
        {
            bool firstInitialize = !isInitialized;
            m_RootItem = new TreeViewItem<EntityId>(EntityId.None, 0, null, "Invisible Root Item");
            SetExpanded(m_RootItem, true); // ensure always visible

            // We want three roots: Favorites, Assets, and Packages
            var visibleRoots = new List<TreeViewItem<EntityId>>();

            // Favorites root
            var savedFiltersRootItem = SavedSearchFilters.ConvertToTreeView(FavoritesEntityIds.instance.GetOrAllocateEntityIdFor);
            visibleRoots.Add(savedFiltersRootItem);

            // Assets root
            EntityId assetsFolderEntityId = AssetDatabase.GetMainAssetOrInProgressProxyEntityId("Assets");
            int depth = 0;
            string displayName = "Assets";
            AssetsTreeViewDataSource.RootTreeItem assetRootItem = new AssetsTreeViewDataSource.RootTreeItem(assetsFolderEntityId, depth, m_RootItem, displayName);
            assetRootItem.icon = s_FolderIcon;
            visibleRoots.Add(assetRootItem);

            // Packages root
            displayName = PackageManager.Folders.GetPackagesPath();
            AssetsTreeViewDataSource.RootTreeItem packagesRootItem = new AssetsTreeViewDataSource.RootTreeItem(ProjectBrowser.kPackagesFolderInstanceId, depth, m_RootItem, displayName);
            packagesRootItem.icon = s_FolderIcon;
            visibleRoots.Add(packagesRootItem);

            m_RootItem.children = visibleRoots;

            // Set global expanded state for roots from EditorPrefs (must be before building the rows)
            if (firstInitialize)
            {
                foreach (var item in m_RootItem.children)
                {
                    bool expanded = EditorPrefs.GetBool(kProjectBrowserString + item.displayName, true);
                    SetExpanded(item, expanded);
                }
            }

            // Build rows
            //-----------
            m_Rows = new List<TreeViewItem<EntityId>>(100);

            // Favorites
            savedFiltersRootItem.parent = m_RootItem;
            m_Rows.Add(savedFiltersRootItem);
            if (IsExpanded(savedFiltersRootItem))
            {
                foreach (var f in savedFiltersRootItem.children)
                    m_Rows.Add(f);
            }
            else
            {
                savedFiltersRootItem.children = CreateChildListForCollapsedParent();
            }

            // Asset folders
            m_Rows.Add(assetRootItem);
            ReadAssetDatabase("Assets", assetRootItem, depth + 1, m_Rows);

            // Individual Package folders (under the Packages root item)
            m_Rows.Add(packagesRootItem);
            var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(skipHiddenPackages);
            if (IsExpanded(packagesRootItem))
            {
                depth++;
                foreach (var package in packages)
                {
                    var packageFolderEntityId = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(package.assetPath);

                    displayName = !string.IsNullOrEmpty(package.displayName) ? package.displayName : package.name;
                    AssetsTreeViewDataSource.PackageTreeItem packageItem = new AssetsTreeViewDataSource.PackageTreeItem(packageFolderEntityId, depth, packagesRootItem, displayName);
                    packageItem.icon = s_FolderIcon;
                    packagesRootItem.AddChild(packageItem);
                    m_Rows.Add(packageItem);
                    ReadAssetDatabase(package.assetPath, packageItem, depth + 1, m_Rows);
                }
            }
            else
            {
                if (packages.Length > 0)
                    packagesRootItem.children = CreateChildListForCollapsedParent();
            }

            m_NeedRefreshRows = false;
        }

        static bool HasSubFolders(IHierarchyIterator property)
        {
            var path = AssetDatabase.GUIDToAssetPath(property.guid);
            var subFolders = AssetDatabase.GetSubFolders(path);
            return subFolders.Length > 0;
        }

        private void ReadAssetDatabase(string assetFolderRootPath, TreeViewItem<EntityId> parent, int baseDepth, IList<TreeViewItem<EntityId>> allRows)
        {
            // Read from Assets directory
            var property = new HierarchyIterator(assetFolderRootPath);
            property.Reset();

            if (!IsExpanded(parent))
            {
                if (HasSubFolders(property))
                    parent.children = CreateChildListForCollapsedParent();
                return;
            }

            Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);

            var allFolders = new List<TreeViewItem<EntityId>>();
            var expandedIDs = m_TreeView.state.expandedIDs.ToArray();
            while (property.Next(expandedIDs))
            {
                if (property.isFolder)
                {
                    AssetsTreeViewDataSource.FolderTreeItem folderItem = new AssetsTreeViewDataSource.FolderTreeItem(property.guid, !property.hasChildren, property.GetEntityIdIfImported(), baseDepth + property.depth, null, property.name);
                    folderItem.icon = folderIcon;
                    allFolders.Add(folderItem);
                    allRows.Add(folderItem);
                    if (!IsExpanded(folderItem))
                    {
                        if (HasSubFolders(property))
                            folderItem.children = CreateChildListForCollapsedParent();
                    }
                    else // expanded status does not get updated when deleting/moving folders. We need to check if the expanded folder still has subFolders when reading the AssetDatabase
                    {
                        if (!HasSubFolders(property))
                            SetExpanded(folderItem, false);
                    }
                }
            }

            // Fix references
            TreeViewUtility<EntityId>.SetChildParentReferences(allFolders, parent);
        }

        public override void SetExpandedWithChildren(EntityId id, bool expand)
        {
            base.SetExpandedWithChildren(id, expand);
            PersistExpandedState(id, expand);
        }

        public override bool SetExpanded(EntityId id, bool expand)
        {
            if (base.SetExpanded(id, expand))
            {
                PersistExpandedState(id, expand);
                return true;
            }
            return false;
        }

        void PersistExpandedState(EntityId id, bool expand)
        {
            // Persist expanded state for ProjectBrowsers
            InternalEditorUtility.expandedProjectWindowItemIds = expandedIDs.ToArray();

            if (m_RootItem.hasChildren)
            {
                // Set global expanded state of roots (Assets folder and Favorites root)
                foreach (var item in m_RootItem.children)
                    if (item.id == id)
                        EditorPrefs.SetBool(kProjectBrowserString + item.displayName, expand);
            }
        }

        protected override void GetParentsAbove(EntityId id, HashSet<EntityId> parentsAbove)
        {
            ProjectBrowser.ItemType itemType = ProjectBrowser.GetItemType(id);
            if (itemType == ProjectBrowser.ItemType.SavedFilter)
            {
                EntityId parentEntityId = FavoritesEntityIds.instance.GetOrAllocateEntityIdFor(SavedSearchFilters.GetRootFilterId());
                parentsAbove.Add(parentEntityId);
            }
            else
            {
                // AssetDatabase folders (in Assets or Packages)
                var path = AssetDatabase.GetAssetPath(id);
                if (Directory.Exists(path))
                    parentsAbove.UnionWith(ProjectWindowUtil.GetAncestors(id));
            }
        }

        protected override void GetParentsBelow(EntityId id, HashSet<EntityId> parentsBelow)
        {
            var extra = GetParentsBelow(id);
            parentsBelow.UnionWith(extra);
        }

        private HashSet<EntityId> GetParentsBelow(EntityId id)
        {
            // Add all children expanded ids to hashset
            var parentsBelow = new HashSet<EntityId>();

            // Check if packages instance
            if (id == ProjectBrowser.kPackagesFolderInstanceId)
            {
                parentsBelow.Add(id);
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(skipHiddenPackages);
                foreach (var package in packages)
                {
                    var packageFolderEntityId = AssetDatabase.GetMainAssetOrInProgressProxyEntityId(package.assetPath);
                    parentsBelow.UnionWith(GetParentsBelow(packageFolderEntityId));
                }
                return parentsBelow;
            }

            var path = AssetDatabase.GetAssetPath(id);
            var search = new HierarchyIterator(path);
            if (search.Find(id, null))
            {
                parentsBelow.Add(id);

                int depth = search.depth;
                while (search.Next(default(EntityId[])) && search.depth > depth)
                {
                    if (search.isFolder && search.hasChildren)
                        parentsBelow.Add(search.entityId);
                }
            }
            return parentsBelow;
        }
    }

    internal class ProjectBrowserColumnOneTreeViewDragging : AssetsTreeViewDragging
    {
        public ProjectBrowserColumnOneTreeViewDragging(TreeViewController<EntityId> treeView) : base(treeView)
        {
        }

        public override void StartDrag(TreeViewItem<EntityId> draggedItem, List<EntityId> draggedItemIDs)
        {
            ProjectBrowser.ItemType itemType = ProjectBrowser.GetItemType(draggedItem.id);
            if (itemType == ProjectBrowser.ItemType.SavedFilter)
            {
                // Root Filters Item is not allowed to be dragged
                EntityId rootFiltersEntityId = FavoritesEntityIds.instance.GetOrAllocateEntityIdFor(SavedSearchFilters.GetRootFilterId());
                if (draggedItem.id == rootFiltersEntityId)
                    return;
            }

            ProjectWindowUtil.StartDrag(draggedItem.id, draggedItemIDs);
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem<EntityId> parentItem, TreeViewItem<EntityId> targetItem, bool perform, DropPosition dropPos)
        {
            if (targetItem == null)
                return DragAndDropVisualMode.None;

            object savedFilterData = DragAndDrop.GetGenericData(ProjectWindowUtil.k_DraggingFavoriteGenericData);

            // Dragging saved filter
            if (savedFilterData != null)
            {
                var entityId = (EntityId)savedFilterData;
                if (targetItem is SearchFilterTreeItem && parentItem is SearchFilterTreeItem)
                {
                    // map to filterIds
                    int filterId = FavoritesEntityIds.instance.GetIdFor(entityId);
                    int parentFilterId = FavoritesEntityIds.instance.GetIdFor(parentItem.id);
                    int targetFilterId = FavoritesEntityIds.instance.GetIdFor(targetItem.id);

                    bool validMove = SavedSearchFilters.CanMoveSavedFilter(filterId, parentFilterId, targetFilterId, dropPos == DropPosition.Below);
                    if (validMove && perform)
                    {
                        SavedSearchFilters.MoveSavedFilter(filterId, parentFilterId, targetFilterId, dropPos == DropPosition.Below);
                        m_TreeView.SetSelection(new[] { entityId }, false);
                        m_TreeView.NotifyListenersThatSelectionChanged();
                    }
                    return validMove ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;
                }
                return DragAndDropVisualMode.None;
            }
            // Dragging of folders into filters
            else
            {
                // Check if we are dragging a single folder
                if (targetItem is SearchFilterTreeItem)
                {
                    string genericData = DragAndDrop.GetGenericData(ProjectWindowUtil.k_IsFolderGenericData) as string;
                    if (genericData == "isFolder")
                    {
                        if (perform)
                        {
                            UnityEngine.Object[] objs = DragAndDrop.objectReferences;
                            if (objs.Length > 0)
                            {
                                string path = AssetDatabase.GetAssetPath(objs[0].GetEntityId());
                                if (!string.IsNullOrEmpty(path))
                                {
                                    // TODO: Fix with new AssetDatabase API when it is ready (GetName)
                                    string folderName = new DirectoryInfo(path).Name;
                                    SearchFilter searchFilter = new SearchFilter();
                                    searchFilter.folders = new[] {path};
                                    bool addAsChild = targetItem == parentItem;

                                    float previewSize = ProjectBrowserColumnOneTreeViewGUI.GetListAreaGridSize();

                                    int targetFilterId = FavoritesEntityIds.instance.GetIdFor(targetItem.id);
                                    int newFilterId = SavedSearchFilters.AddSavedFilterAfterFilterId(folderName, searchFilter, previewSize, targetFilterId, addAsChild);
                                    EntityId newEntityId = FavoritesEntityIds.instance.GetOrAllocateEntityIdFor(newFilterId);
                                    m_TreeView.SetSelection(new[] { newEntityId }, false);
                                    m_TreeView.NotifyListenersThatSelectionChanged();
                                }
                                else
                                {
                                    Debug.Log("Could not get asset path from id " + objs[0].GetEntityId());
                                }
                            }
                        }
                        return DragAndDropVisualMode.Copy; // Allow dragging folders to filters
                    }
                    return DragAndDropVisualMode.None; // Assets that are not folders are not allowed to be dragged to filters
                }
            }
            //  Assets are handled by base
            return base.DoDrag(parentItem, targetItem, perform, dropPos);
        }
    }

    // The FavoritesEntityIds is a ScriptableSingleton for internal state to survive assembly
    // reloading (life time: session of Editor, not persisted to disk)
    class FavoritesEntityIds : ScriptableSingleton<FavoritesEntityIds>
    {
        // This list is serializable so survives domain reloading
        List<IdPair> m_AllocatedEntityIds = [];

        // These dictionaries are not serializable so built on demand for performance
        Dictionary<int, EntityId> m_LookupEntityId = new();
        Dictionary<EntityId, int> m_LookupId = new();

        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        record struct IdPair(int id, EntityId entityId);

        public EntityId GetOrAllocateEntityIdFor(int id)
        {
            if (m_LookupEntityId.TryGetValue(id, out var entityId))
                return entityId;

            // Fallback to list traversal (should only happen once per id, at startup and after domain reloading)
            foreach (var pair in m_AllocatedEntityIds)
            {
                if (pair.id == id)
                {
                    CacheMapping(id, pair.entityId);
                    return pair.entityId;
                }
            }

            // Allocate new EntityId
            var newEntityId = EntityId.AllocateNextLowestEntityId();
            m_AllocatedEntityIds.Add(new IdPair(id, newEntityId));
            CacheMapping(id, newEntityId);
            return newEntityId;
        }

        public int GetIdFor(EntityId entityId)
        {
            if (TryGetIdFor(entityId, out int id))
                return id;

            throw new KeyNotFoundException("EntityId was not found: " + entityId);
        }

        public bool TryGetIdFor(EntityId entityId, out int id)
        {
            if (m_LookupId.TryGetValue(entityId, out int intId))
            {
                id = intId;
                return true;
            }

            // Fallback to list traversal (should only happen once per id, at startup and after domain reloading)
            foreach (var pair in m_AllocatedEntityIds)
            {
                if (pair.entityId == entityId)
                {
                    CacheMapping(pair.id, entityId);
                    id = pair.id;
                    return true;
                }
            }

            // When retrning false use the same convention for the out parameter
            // value as Dictionary.TryGetValue: using default value of the type.
            id = default;
            return false;
        }

        void CacheMapping(int id, EntityId entityId)
        {
            m_LookupEntityId[id] = entityId;
            m_LookupId[entityId] = id;
        }
    }
}
