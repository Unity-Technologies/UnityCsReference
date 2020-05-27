// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Used for type check
    class SearchFilterTreeItem : TreeViewItem
    {
        bool m_IsFolder;
        public SearchFilterTreeItem(int id, int depth, TreeViewItem parent, string displayName, bool isFolder)
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

        public ProjectBrowserColumnOneTreeViewGUI(TreeViewController treeView) : base(treeView)
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

        float GetTopPixelOfRow(int row, IList<TreeViewItem> rows)
        {
            float topPixel = row * k_LineHeight;

            // Assumes Saved filter are second root
            TreeViewItem item = rows[row];
            ProjectBrowser.ItemType type = ProjectBrowser.GetItemType(item.id);
            if (type == ProjectBrowser.ItemType.Asset)
                topPixel += k_DistBetweenRootTypes;

            return topPixel;
        }

        override public int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
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

        override public void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
        {
            bool useBoldFont = IsVisibleRootNode(item);
            DoItemGUI(rowRect, row, item, selected, focused, useBoldFont);
        }

        bool IsVisibleRootNode(TreeViewItem item)
        {
            return (m_TreeView.data as ProjectBrowserColumnOneTreeViewDataSource).IsVisibleRootNode(item);
        }

        protected override Texture GetIconForItem(TreeViewItem item)
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
            int instanceID = SavedSearchFilters.AddSavedFilter(savedFilterName, filter, GetListAreaGridSize());
            m_TreeView.Frame(instanceID, true, false);

            // Start naming the asset
            m_TreeView.state.renameOverlay.BeginRename(savedFilterName, instanceID, 0f);
        }

        override protected void RenameEnded()
        {
            int instanceID = GetRenameOverlay().userData;
            ProjectBrowser.ItemType type = ProjectBrowser.GetItemType(instanceID);

            if (m_IsCreatingSavedFilter)
            {
                // Create saved filter
                m_IsCreatingSavedFilter = false;

                if (GetRenameOverlay().userAcceptedRename)
                {
                    SavedSearchFilters.SetName(instanceID,  GetRenameOverlay().name);
                    m_TreeView.SetSelection(new[] { instanceID }, true);
                }
                else
                    SavedSearchFilters.RemoveSavedFilter(instanceID);
            }
            else if (type == ProjectBrowser.ItemType.SavedFilter)
            {
                // Renamed saved filter
                if (GetRenameOverlay().userAcceptedRename)
                {
                    SavedSearchFilters.SetName(instanceID,  GetRenameOverlay().name);
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

    internal class ProjectBrowserColumnOneTreeViewDataSource : LazyTreeViewDataSource
    {
        static string kProjectBrowserString = "ProjectBrowser";
        static Texture2D s_FolderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);

        public bool skipHiddenPackages { get; set; }

        public ProjectBrowserColumnOneTreeViewDataSource(TreeViewController treeView, bool skipHidden) : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
            skipHiddenPackages = skipHidden;
            SavedSearchFilters.AddChangeListener(ReloadData); // We reload on change
        }

        public override bool IsExpandable(TreeViewItem item)
        {
            return item.hasChildren && (item != m_RootItem || rootIsCollapsable);
        }

        public override bool CanBeMultiSelected(TreeViewItem item)
        {
            return ProjectBrowser.GetItemType(item.id) != ProjectBrowser.ItemType.SavedFilter;
        }

        public override bool CanBeParent(TreeViewItem item)
        {
            return !(item is SearchFilterTreeItem) || SavedSearchFilters.AllowsHierarchy();
        }

        public bool IsVisibleRootNode(TreeViewItem item)
        {
            // The main root Item is invisible the next level is visible root items
            return (item.parent != null && item.parent.parent == null);
        }

        public override bool IsRenamingItemAllowed(TreeViewItem item)
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
            m_RootItem = new TreeViewItem(0, 0, null, "Invisible Root Item");
            SetExpanded(m_RootItem, true); // ensure always visible

            // We want three roots: Favorites, Assets, and Packages
            List<TreeViewItem> visibleRoots = new List<TreeViewItem>();

            // Favorites root
            TreeViewItem savedFiltersRootItem = SavedSearchFilters.ConvertToTreeView();
            visibleRoots.Add(savedFiltersRootItem);

            // Assets root
            int assetsFolderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets");
            int depth = 0;
            string displayName = "Assets";
            AssetsTreeViewDataSource.RootTreeItem assetRootItem = new AssetsTreeViewDataSource.RootTreeItem(assetsFolderInstanceID, depth, m_RootItem, displayName);
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
                foreach (TreeViewItem item in m_RootItem.children)
                {
                    // Do not expand Packages root item
                    if (item.id == ProjectBrowser.kPackagesFolderInstanceId)
                        continue;
                    bool expanded = EditorPrefs.GetBool(kProjectBrowserString + item.displayName, true);
                    SetExpanded(item, expanded);
                }
            }

            // Build rows
            //-----------
            m_Rows = new List<TreeViewItem>(100);

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
                    var packageFolderInstanceId = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(package.assetPath);

                    displayName = !string.IsNullOrEmpty(package.displayName) ? package.displayName : package.name;
                    AssetsTreeViewDataSource.PackageTreeItem packageItem = new AssetsTreeViewDataSource.PackageTreeItem(packageFolderInstanceId, depth, packagesRootItem, displayName);
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

        static bool HasSubFolders(IHierarchyProperty property)
        {
            var path = AssetDatabase.GUIDToAssetPath(property.guid);
            var subFolders = AssetDatabase.GetSubFolders(path);
            return subFolders.Length > 0;
        }

        private void ReadAssetDatabase(string assetFolderRootPath, TreeViewItem parent, int baseDepth, IList<TreeViewItem> allRows)
        {
            // Read from Assets directory
            IHierarchyProperty property = new HierarchyProperty(assetFolderRootPath);
            property.Reset();

            if (!IsExpanded(parent))
            {
                if (HasSubFolders(property))
                    parent.children = CreateChildListForCollapsedParent();
                return;
            }

            Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResources.folderIconName);

            List<TreeViewItem> allFolders = new List<TreeViewItem>();
            var expandedIDs = m_TreeView.state.expandedIDs.ToArray();
            while (property.Next(expandedIDs))
            {
                if (property.isFolder)
                {
                    AssetsTreeViewDataSource.FolderTreeItem folderItem = new AssetsTreeViewDataSource.FolderTreeItem(property.guid, !property.hasChildren, property.GetInstanceIDIfImported(), baseDepth + property.depth, null, property.name);
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
            TreeViewUtility.SetChildParentReferences(allFolders, parent);
        }

        public override void SetExpandedWithChildren(int id, bool expand)
        {
            base.SetExpandedWithChildren(id, expand);
            PersistExpandedState(id, expand);
        }

        public override bool SetExpanded(int id, bool expand)
        {
            if (base.SetExpanded(id, expand))
            {
                PersistExpandedState(id, expand);
                return true;
            }
            return false;
        }

        void PersistExpandedState(int id, bool expand)
        {
            // Persist expanded state for ProjectBrowsers
            InternalEditorUtility.expandedProjectWindowItems = expandedIDs.ToArray();

            if (m_RootItem.hasChildren)
            {
                // Set global expanded state of roots (Assets folder and Favorites root)
                foreach (TreeViewItem item in m_RootItem.children)
                    if (item.id == id)
                        EditorPrefs.SetBool(kProjectBrowserString + item.displayName, expand);
            }
        }

        protected override void GetParentsAbove(int id, HashSet<int> parentsAbove)
        {
            if (SavedSearchFilters.IsSavedFilter(id))
            {
                parentsAbove.Add(SavedSearchFilters.GetRootInstanceID());
            }
            else
            {
                // AssetDatabase folders (in Assets or Packages)
                var path = AssetDatabase.GetAssetPath(id);
                if (Directory.Exists(path))
                    parentsAbove.UnionWith(ProjectWindowUtil.GetAncestors(id));
            }
        }

        protected override void GetParentsBelow(int id, HashSet<int> parentsBelow)
        {
            var extra = GetParentsBelow(id);
            parentsBelow.UnionWith(extra);
        }

        private HashSet<int> GetParentsBelow(int id)
        {
            // Add all children expanded ids to hashset
            HashSet<int> parentsBelow = new HashSet<int>();

            // Check if packages instance
            if (id == ProjectBrowser.kPackagesFolderInstanceId)
            {
                parentsBelow.Add(id);
                var packages = PackageManagerUtilityInternal.GetAllVisiblePackages(skipHiddenPackages);
                foreach (var package in packages)
                {
                    var packageFolderInstanceId = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(package.assetPath);
                    parentsBelow.UnionWith(GetParentsBelow(packageFolderInstanceId));
                }
                return parentsBelow;
            }

            var path = AssetDatabase.GetAssetPath(id);
            IHierarchyProperty search = new HierarchyProperty(path);
            if (search.Find(id, null))
            {
                parentsBelow.Add(id);

                int depth = search.depth;
                while (search.Next(null) && search.depth > depth)
                {
                    if (search.isFolder && search.hasChildren)
                        parentsBelow.Add(search.instanceID);
                }
            }
            return parentsBelow;
        }
    }

    internal class ProjectBrowserColumnOneTreeViewDragging : AssetsTreeViewDragging
    {
        public ProjectBrowserColumnOneTreeViewDragging(TreeViewController treeView) : base(treeView)
        {
        }

        public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
        {
            if (SavedSearchFilters.IsSavedFilter(draggedItem.id))
            {
                // Root Filters Item is not allowed to be dragged
                if (draggedItem.id == SavedSearchFilters.GetRootInstanceID())
                    return;
            }

            ProjectWindowUtil.StartDrag(draggedItem.id, draggedItemIDs);
        }

        public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPos)
        {
            if (targetItem == null)
                return DragAndDropVisualMode.None;

            object savedFilterData = DragAndDrop.GetGenericData(ProjectWindowUtil.k_DraggingFavoriteGenericData);

            // Dragging saved filter
            if (savedFilterData != null)
            {
                int instanceID = (int)savedFilterData;
                if (targetItem is SearchFilterTreeItem && parentItem is SearchFilterTreeItem)// && targetItem.id != draggedInstanceID && parentItem.id != draggedInstanceID)
                {
                    bool validMove = SavedSearchFilters.CanMoveSavedFilter(instanceID, parentItem.id, targetItem.id, dropPos == DropPosition.Below);
                    if (validMove && perform)
                    {
                        SavedSearchFilters.MoveSavedFilter(instanceID, parentItem.id, targetItem.id, dropPos == DropPosition.Below);
                        m_TreeView.SetSelection(new[] { instanceID }, false);
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
                            Object[] objs = DragAndDrop.objectReferences;
                            if (objs.Length > 0)
                            {
                                string path = AssetDatabase.GetAssetPath(objs[0].GetInstanceID());
                                if (!string.IsNullOrEmpty(path))
                                {
                                    // TODO: Fix with new AssetDatabase API when it is ready (GetName)
                                    string folderName = new DirectoryInfo(path).Name;
                                    SearchFilter searchFilter = new SearchFilter();
                                    searchFilter.folders = new[] {path};
                                    bool addAsChild = targetItem == parentItem;

                                    float previewSize = ProjectBrowserColumnOneTreeViewGUI.GetListAreaGridSize();
                                    int instanceID = SavedSearchFilters.AddSavedFilterAfterInstanceID(folderName, searchFilter, previewSize, targetItem.id, addAsChild);
                                    m_TreeView.SetSelection(new[] { instanceID }, false);
                                    m_TreeView.NotifyListenersThatSelectionChanged();
                                }
                                else
                                {
                                    Debug.Log("Could not get asset path from id " + objs[0].GetInstanceID());
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
}
