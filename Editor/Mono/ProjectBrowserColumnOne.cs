// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
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
                return item.icon;

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

    internal class ProjectBrowserColumnOneTreeViewDataSource : TreeViewDataSource
    {
        static string kProjectBrowserString = "ProjectBrowser";

        public ProjectBrowserColumnOneTreeViewDataSource(TreeViewController treeView) : base(treeView)
        {
            showRootItem = false;
            rootIsCollapsable = false;
            SavedSearchFilters.AddChangeListener(ReloadData); // We reload on change
        }

        public override bool SetExpanded(int id, bool expand)
        {
            if (base.SetExpanded(id, expand))
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
                return true;
            }
            return false;
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

            return base.IsRenamingItemAllowed(item);
        }

        public static int GetAssetsFolderInstanceID()
        {
            string rootDir = "Assets";
            string guid = AssetDatabase.AssetPathToGUID(rootDir);
            int instanceID = AssetDatabase.GetInstanceIDFromGUID(guid);
            return instanceID;
        }

        public override void FetchData()
        {
            m_RootItem = new TreeViewItem(System.Int32.MaxValue, 0, null, "Invisible Root Item");
            SetExpanded(m_RootItem, true); // ensure always visible

            // We want three roots: Favorites, Assets, and Saved Filters
            List<TreeViewItem> visibleRoots = new List<TreeViewItem>();

            // Fetch asset folders
            int assetsFolderInstanceID = GetAssetsFolderInstanceID();
            int depth = 0;
            string displayName = "Assets"; //CreateDisplayName (assetsFolderInstanceID);
            TreeViewItem assetRootItem = new TreeViewItem(assetsFolderInstanceID, depth, m_RootItem, displayName);
            ReadAssetDatabase(HierarchyType.Assets, assetRootItem, depth + 1);

            // Fetch packages
            TreeViewItem packagesRootItem = null;
            if (Unsupported.IsDeveloperBuild() && EditorPrefs.GetBool("ShowPackagesFolder"))
            {
                var packagesGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetPackagesMountPoint());
                var packagesFolderInstanceID = AssetDatabase.GetInstanceIDFromGUID(packagesGuid);
                string packagesDisplayName = AssetDatabase.GetPackagesMountPoint();
                packagesRootItem = new TreeViewItem(packagesFolderInstanceID, depth, m_RootItem, packagesDisplayName);
                ReadAssetDatabase(HierarchyType.Packages, packagesRootItem, depth + 1);
            }

            // Fetch saved filters
            TreeViewItem savedFiltersRootItem = SavedSearchFilters.ConvertToTreeView();
            savedFiltersRootItem.parent = m_RootItem;

            // Order
            visibleRoots.Add(savedFiltersRootItem);
            visibleRoots.Add(assetRootItem);

            if (packagesRootItem != null)
                visibleRoots.Add(packagesRootItem);

            m_RootItem.children = visibleRoots;

            // Get global expanded state of roots
            foreach (TreeViewItem item in m_RootItem.children)
            {
                bool expanded = EditorPrefs.GetBool(kProjectBrowserString + item.displayName, true);
                SetExpanded(item, expanded);
            }

            m_NeedRefreshRows = true;
        }

        private void ReadAssetDatabase(HierarchyType htype, TreeViewItem parent, int baseDepth)
        {
            // Read from Assets directory
            IHierarchyProperty property = new HierarchyProperty(htype);
            property.Reset();

            Texture2D folderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.folderIconName);
            Texture2D emptyFolderIcon = EditorGUIUtility.FindTexture(EditorResourcesUtility.emptyFolderIconName);

            List<TreeViewItem> allFolders = new List<TreeViewItem>();
            while (property.Next(null))
            {
                if (property.isFolder)
                {
                    TreeViewItem folderItem = new TreeViewItem(property.instanceID, baseDepth + property.depth, null, property.name);
                    folderItem.icon = property.hasChildren ? folderIcon : emptyFolderIcon;
                    allFolders.Add(folderItem);
                }
            }

            // Fix references
            TreeViewUtility.SetChildParentReferences(allFolders, parent);
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
                if (targetItem is SearchFilterTreeItem && parentItem is SearchFilterTreeItem)
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
