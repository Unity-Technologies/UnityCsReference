// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.AssetImporters;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PackageManager;
using UnityEditor.TreeViewExamples;
using UnityEditorInternal;
using UnityEngine.Scripting;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // The title is also used for fetching the project window tab icon: Project.png
    [EditorWindowTitle(title = "Project", icon = "Project")]
    internal class ProjectBrowser : EditorWindow, IHasCustomMenu, ISearchableContainer
    {
        public const int kPackagesFolderInstanceId = int.MaxValue;
        public const int kAssetCreationInstanceID_ForNonExistingAssets = Int32.MaxValue - 1;

        private static readonly Color kFadedOutAssetsColor = new Color(1, 1, 1, 0.5f);

        public static Color GetAssetItemColor(int instanceID)
        {
            return (EditorUtility.isInSafeMode && !InternalEditorUtility.AssetReference.IsAssetImported(instanceID)) || AssetClipboardUtility.HasCutAsset(instanceID) ? GUI.color * kFadedOutAssetsColor : GUI.color;
        }

        private static readonly int[] k_EmptySelection = new int[0];

        private bool isFolderTreeViewContextClicked = false;

        private const string k_WarningImmutableSelectionFormat = "The operation \"{0}\" cannot be executed because the selection or package is read-only.";

        private const string k_WarningRootFolderDeletionFormat = "The operation \"{0}\" cannot be executed because the selection is a root folder.";

        private const string k_ImmutableSelectionActionFormat = " The operation \"{0}\" is not allowed in an immutable package.";

        // Alive ProjectBrowsers
        private static List<ProjectBrowser> s_ProjectBrowsers = new List<ProjectBrowser>();
        public static List<ProjectBrowser> GetAllProjectBrowsers()
        {
            return s_ProjectBrowsers;
        }

        public static ProjectBrowser s_LastInteractedProjectBrowser;

        internal enum ItemType
        {
            Asset,
            SavedFilter,
        }

        enum ViewMode
        {
            OneColumn,
            TwoColumns
        }

        public enum SearchViewState
        {
            NotSearching,
            AllAssets,
            InAssetsOnly,
            InPackagesOnly,
            SubFolders
        }

        // Styles used in the object selector
        class Styles
        {
            public GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public GUIStyle topBarBg = "ProjectBrowserTopBarBg";
            public GUIStyle selectedPathLabel = "Label";
            public GUIStyle exposablePopup = GetStyle("ExposablePopupMenu");
            public GUIStyle exposablePopupItem = GetStyle("ExposablePopupItem");
            public GUIStyle lockButton = "IN LockButton";
            public GUIStyle separator = "ArrowNavigationRight";

            public GUIContent m_FilterByLabel = EditorGUIUtility.TrIconContent("FilterByLabel", "Search by Label");
            public GUIContent m_FilterByType = EditorGUIUtility.TrIconContent("FilterByType", "Search by Type");
            public GUIContent m_FilterByImportLog = EditorGUIUtility.TrIconContent("d_console.erroricon.inactive.sml", "Search by Import Log Type");
            public GUIContent m_CreateDropdownContent = EditorGUIUtility.IconContent("CreateAddNew");
            public GUIContent m_SaveFilterContent = EditorGUIUtility.TrIconContent("Favorite", "Save search");
            public GUIContent m_PackageContentDefault = new GUIContent("", "");
            public GUIContent m_PackagesContentNotVisible = EditorGUIUtility.TrIconContent("PBrowserPackagesNotVisible", "Number of hidden packages, click to display packages.");
            public GUIContent m_PackagesContentVisible = EditorGUIUtility.TrIconContent("PBrowserPackagesVisible", "Number of displayed packages, click to hide packages.");
            public GUIContent m_EmptyFolderText = EditorGUIUtility.TrTextContent("This folder is empty");
            public GUIContent m_SearchIn = EditorGUIUtility.TrTextContent("Search:");

            public Styles()
            {
                selectedPathLabel.alignment = TextAnchor.MiddleLeft;
            }

            static GUIStyle GetStyle(string styleName)
            {
                return styleName; // Implicit construction of GUIStyle
            }
        }

        static Styles s_Styles;
        static int s_HashForSearchField = "ProjectBrowserSearchField".GetHashCode();

        // Search filter
        [SerializeField]
        SearchFilter m_SearchFilter;
        string m_OldSearch;
        bool m_SyncSearch;

        [NonSerialized]
        string m_SearchFieldText = "";

        // Display state
        [SerializeField]
        ViewMode m_ViewMode = ViewMode.TwoColumns;
        [SerializeField]
        int m_StartGridSize = 16;
        [SerializeField]
        string[] m_LastFolders = new string[0];
        [SerializeField]
        float m_LastFoldersGridSize = -1f;
        [SerializeField]
        string m_LastProjectPath;
        [SerializeField]
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        internal bool isLocked
        {
            get { return m_LockTracker.isLocked; }
            set { m_LockTracker.isLocked = value; }
        }

        public string searchText => m_SearchFieldText;

        bool m_EnableOldAssetTree = true;
        bool m_FocusSearchField;
        string m_SelectedPath;
        GUIContent m_SelectedPathContent = new GUIContent();
        bool m_DidSelectSearchResult = false;
        bool m_ItemSelectedByRightClickThisEvent = false;
        bool m_InternalSelectionChange = false; // to know when selection change originated in project view itself
        SearchFilter.SearchArea m_LastLocalAssetsSearchArea = SearchFilter.SearchArea.InAssetsOnly;
        PopupList.InputData m_AssetLabels;
        PopupList.InputData m_ObjectTypes;
        PopupList.InputData m_LogTypes;
        bool m_UseTreeViewSelectionInsteadOfMainSelection;
        bool useTreeViewSelectionInsteadOfMainSelection
        {
            get { return m_UseTreeViewSelectionInsteadOfMainSelection; }
            set { m_UseTreeViewSelectionInsteadOfMainSelection = value; }
        }


        // Folder TreeView
        [SerializeField]
        TreeViewStateWithAssetUtility m_FolderTreeState;
        TreeViewController m_FolderTree;
        int m_TreeViewKeyboardControlID;

        // Full Asset TreeViewState
        [SerializeField]
        TreeViewStateWithAssetUtility m_AssetTreeState;
        TreeViewController m_AssetTree;

        // Icon/List area
        [SerializeField]
        ObjectListAreaState m_ListAreaState; // state that survives assembly reloads
        ObjectListArea m_ListArea;
        internal ObjectListArea ListArea // Exposed for usage in tests
        {
            get { return m_ListArea; }

            // Used for tests only;
            set { m_ListArea = value; }
        }
        int m_ListKeyboardControlID;
        bool m_GrabKeyboardFocusForListArea = false;

        // List area header: breadcrumbs or search area menu
        List<KeyValuePair<GUIContent, string>> m_BreadCrumbs = new List<KeyValuePair<GUIContent, string>>();
        bool m_BreadCrumbLastFolderHasSubFolders = false;
        ExposablePopupMenu m_SearchAreaMenu;

        // Keep/Skip Hidden Packages
        [SerializeField]
        bool m_SkipHiddenPackages;

        // Layout
        const float k_MinHeight = 250;
        const float k_MinWidthOneColumn = 230f;// could be 205 with special handling
        const float k_MinWidthTwoColumns = 230f;
        static float k_ToolbarHeight => EditorGUI.kWindowToolbarHeight;
        static float k_BottomBarHeight => EditorGUI.kWindowToolbarHeight;
        [SerializeField]
        float m_DirectoriesAreaWidth = k_MinWidthTwoColumns / 2;
        const float k_ResizerWidth = 5f;
        const float k_SliderWidth = 55f;
        [NonSerialized]
        float m_SearchAreaMenuOffset = -1f;
        [NonSerialized]
        Rect m_ListAreaRect;
        [NonSerialized]
        Rect m_TreeViewRect;
        [NonSerialized]
        Rect m_BottomBarRect;
        [NonSerialized]
        Rect m_ListHeaderRect;
        [NonSerialized]
        private int m_LastFramedID = -1;

        // Used by search menu bar
        [NonSerialized]
        public GUIContent m_SearchAllAssets = EditorGUIUtility.TrTextContent("All");
        [NonSerialized]
        public GUIContent m_SearchInPackagesOnly = EditorGUIUtility.TrTextContent("In Packages");
        [NonSerialized]
        public GUIContent m_SearchInAssetsOnly = EditorGUIUtility.TrTextContent("In Assets");
        [NonSerialized]
        public GUIContent m_SearchInFolders = new GUIContent(""); // updated when needed

        [NonSerialized]
        private string m_lastSearchFilter;
        [NonSerialized]
        private Action m_NextSearchOffDelegate;

        internal static float searchUpdateDelaySeconds => SearchUtils.debounceThresholdMs / 1000f;

        ProjectBrowser()
        {
        }

        /* Keep for debugging
        void TestProjectItemOverlayCallback (string guid, Rect selectionRect)
        {
            GUI.Label (selectionRect, GUIContent.none, EditorStyles.helpBox);
        }*/

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            s_ProjectBrowsers.Add(this);
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.assetLabelsChanged += OnAssetLabelsChanged;
            EditorApplication.assetBundleNameChanged += OnAssetBundleNameChanged;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            s_LastInteractedProjectBrowser = this;

            SearchService.SearchService.syncSearchChanged += OnSyncSearchChanged;

            // Keep for debugging
            //EditorApplication.projectWindowItemOnGUI += TestProjectItemOverlayCallback;
        }

        void OnAfterAssemblyReload()
        {
            // Repaint to ensure ProjectBrowser UI is initialized. This fixes the issue where a new script is created from the application menu
            // bar while the project browser ui was not initialized.
            Repaint();
        }

        void OnDisable()
        {
            SearchService.SearchService.syncSearchChanged -= OnSyncSearchChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.assetLabelsChanged -= OnAssetLabelsChanged;
            EditorApplication.assetBundleNameChanged -= OnAssetBundleNameChanged;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            s_ProjectBrowsers.Remove(this);
        }

        private void OnSyncSearchChanged(SearchService.SearchService.SyncSearchEvent evt, string syncViewId, string searchQuery)
        {
            if (SearchService.ProjectSearch.HasEngineOverride())
            {
                if (evt == SearchService.SearchService.SyncSearchEvent.StartSession)
                {
                    m_OldSearch = m_SearchFilter.originalText;
                }

                if (syncViewId == SearchService.ProjectSearch.GetActiveSearchEngine().GetType().FullName)
                {
                    SetSearch(searchQuery);
                    m_SyncSearch = true;
                }
                else if (m_SyncSearch)
                {
                    m_SyncSearch = false;
                    SetSearch(m_OldSearch);
                    TopBarSearchSettingsChanged();
                }

                if (evt == SearchService.SearchService.SyncSearchEvent.EndSession)
                {
                    m_SyncSearch = false;
                    TopBarSearchSettingsChanged(false);
                }
            }
        }

        void OnPauseStateChanged(PauseState state)
        {
            EndRenaming();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            EndRenaming();
        }

        void OnAssetLabelsChanged()
        {
            if (Initialized()) // Otherwise Init will handle this
            {
                SetupAssetLabelList();
                if (m_SearchFilter.IsSearching())
                    InitListArea(); // Ensure to refresh search results: we could have results based on previous label settings
            }
        }

        void OnAssetBundleNameChanged()
        {
            if (m_ListArea != null)
                InitListArea();
        }

        void Awake()
        {
            if (m_ListAreaState != null)
            {
                m_ListAreaState.OnAwake();
            }

            if (m_FolderTreeState != null)
            {
                m_FolderTreeState.OnAwake();
                m_FolderTreeState.expandedIDs = new List<int>(InternalEditorUtility.expandedProjectWindowItems);
            }

            if (m_AssetTreeState != null)
            {
                m_AssetTreeState.OnAwake();
                m_AssetTreeState.expandedIDs = new List<int>(InternalEditorUtility.expandedProjectWindowItems);
            }

            if (m_SearchFilter != null)
            {
                EnsureValidFolders();
            }
        }

        static internal ItemType GetItemType(int instanceID)
        {
            if (SavedSearchFilters.IsSavedFilter(instanceID))
                return ItemType.SavedFilter;

            return ItemType.Asset;
        }

        // Return the logical path that user has active. Used when you need to create new assets from outside the project browser.
        internal string GetActiveFolderPath()
        {
            if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing && m_SearchFilter.folders.Length > 0)
                return m_SearchFilter.folders[0];

            return "Assets";
        }

        private bool IsInsideHiddenPackage(string path)
        {
            if (!m_SkipHiddenPackages)
                return false;

            return !PackageManagerUtilityInternal.IsPathInVisiblePackage(path);
        }

        void EnsureValidFolders()
        {
            if (m_SearchFilter == null)
                return;

            HashSet<string> validFolders = new HashSet<string>();
            foreach (string folder in m_SearchFilter.folders)
            {
                if (folder == PackageManager.Folders.GetPackagesPath())
                {
                    validFolders.Add(folder);
                    continue;
                }
                if (AssetDatabase.IsValidFolder(folder))
                {
                    if (IsInsideHiddenPackage(folder))
                        continue;

                    validFolders.Add(folder);
                }
                else
                {
                    // The folder does not exist (could have been deleted) now find first valid parent folder
                    string parentFolder = folder;
                    for (int i = 0; i < 30; ++i)
                    {
                        if (String.IsNullOrEmpty(parentFolder))
                            break;

                        parentFolder = ProjectWindowUtil.GetContainingFolder(parentFolder);
                        if (!String.IsNullOrEmpty(parentFolder) && AssetDatabase.IsValidFolder(parentFolder))
                        {
                            if (IsInsideHiddenPackage(parentFolder))
                                continue;

                            validFolders.Add(parentFolder);
                            break;
                        }
                    }
                }
            }

            m_SearchFilter.folders = validFolders.ToArray();
        }

        private void ResetViews()
        {
            if (m_AssetTree != null)
            {
                m_AssetTree.ReloadData();
                SetSearchFoldersFromCurrentSelection(); // We could have moved, deleted or renamed a folder so ensure we get folder paths by instanceID
            }

            if (m_FolderTree != null)
            {
                m_FolderTree.ReloadData();
                SetSearchFolderFromFolderTreeSelection(); // We could have moved, deleted or renamed a folder so ensure we get folders paths by instanceID
            }

            EnsureValidFolders();
            if (m_ListArea != null)
                InitListArea();

            RefreshSelectedPath();

            m_BreadCrumbs.Clear();
        }

        private void OnProjectChanged()
        {
            ResetViews();
        }

        public bool Initialized()
        {
            return m_ListArea != null;
        }

        public void Init()
        {
            if (Initialized())
                return;

            if (s_Styles == null)
            {
                s_Styles = new Styles();
            }

            m_FocusSearchField = false;

            bool firstTimeInit = m_SearchFilter == null;
            if (firstTimeInit)
                m_DirectoriesAreaWidth = Mathf.Min(position.width / 2, 200);

            // m_SearchFilter is serialized
            if (m_SearchFilter == null)
                m_SearchFilter = new SearchFilter();

            m_SearchFieldText = m_SearchFilter.FilterToSearchFieldString();

            CalculateRects(); // setup m_TreeViewRect and m_ListAreaRect before calling InitListArea and TreeViews
            RefreshSelectedPath();
            SetupDroplists();

            // m_ListAreaState is serialized

            if (m_ListAreaState == null)
                m_ListAreaState = new ObjectListAreaState();
            m_ListAreaState.m_RenameOverlay.isRenamingFilename = true;

            m_ListArea = new ObjectListArea(m_ListAreaState, this, false);
            m_ListArea.allowDeselection = true;
            m_ListArea.allowDragging = true;
            m_ListArea.allowFocusRendering = true;
            m_ListArea.allowMultiSelect = true;
            m_ListArea.allowRenaming = true;
            m_ListArea.allowBuiltinResources = false;
            m_ListArea.allowUserRenderingHook = true;
            m_ListArea.allowFindNextShortcut = true;
            m_ListArea.foldersFirst = GetShouldShowFoldersFirst();
            m_ListArea.repaintCallback += Repaint;
            m_ListArea.itemSelectedCallback += ListAreaItemSelectedCallback;
            m_ListArea.keyboardCallback += ListAreaKeyboardCallback;
            m_ListArea.gotKeyboardFocus += ListGotKeyboardFocus;
            m_ListArea.drawLocalAssetHeader += DrawLocalAssetHeader;
            m_ListArea.gridSize = m_StartGridSize;

            m_StartGridSize = Mathf.Clamp(m_StartGridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize);
            m_LastFoldersGridSize = Mathf.Min(m_LastFoldersGridSize, m_ListArea.maxGridSize);

            m_SearchAreaMenu = new ExposablePopupMenu();

            // m_FolderTreeState is our serialized state (ensure not to reassign it if our serialization system has created it for us)
            if (m_FolderTreeState == null)
                m_FolderTreeState = new TreeViewStateWithAssetUtility();
            m_FolderTreeState.renameOverlay.isRenamingFilename = true;

            // Full asset treeview
            if (m_AssetTreeState == null)
                m_AssetTreeState = new TreeViewStateWithAssetUtility();
            m_AssetTreeState.renameOverlay.isRenamingFilename = true;

            InitViewMode(m_ViewMode);

            EnsureValidSetup();

            RefreshSearchText();
            SyncFilterGUI();
            InitListArea();
        }

        public void SetSearch(string searchString)
        {
            SetSearch(SearchFilter.CreateSearchFilterFromString(searchString));
        }

        public void SetSearch(SearchFilter searchFilter)
        {
            if (!Initialized())
                Init();

            m_SearchFilter = searchFilter;
            m_SearchFieldText = searchFilter.FilterToSearchFieldString();

            TopBarSearchSettingsChanged();
        }

        internal void RefreshSearchIfFilterContains(string searchString)
        {
            if (!Initialized() || !m_SearchFilter.IsSearching())
                return;

            if (m_SearchFieldText.IndexOf(searchString) >= 0)
            {
                InitListArea();
            }
        }

        void SetSearchViewState(SearchViewState state)
        {
            switch (state)
            {
                case SearchViewState.AllAssets:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.AllAssets;
                    break;
                case SearchViewState.InAssetsOnly:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.InAssetsOnly;
                    break;
                case SearchViewState.InPackagesOnly:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.InPackagesOnly;
                    break;
                case SearchViewState.SubFolders:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.SelectedFolders;
                    break;
                case SearchViewState.NotSearching:
                    Debug.LogError("Invalid search mode as setter");
                    break;
            }

            // Sync gui to state
            InitSearchMenu();
            InitListArea();
        }

        SearchViewState GetSearchViewState()
        {
            switch (m_SearchFilter.GetState())
            {
                case SearchFilter.State.SearchingInAllAssets: return SearchViewState.AllAssets;
                case SearchFilter.State.SearchingInAssetsOnly: return SearchViewState.InAssetsOnly;
                case SearchFilter.State.SearchingInPackagesOnly: return SearchViewState.InPackagesOnly;
                case SearchFilter.State.SearchingInFolders: return SearchViewState.SubFolders;
            }
            return SearchViewState.NotSearching;
        }

        void SearchButtonClickedCallback(ExposablePopupMenu.ItemData itemClicked)
        {
            if (!itemClicked.m_On) // Behave like radio buttons: a button that is on cannot be turned off
            {
                SetSearchViewState((SearchViewState)itemClicked.m_UserData);

                if (m_SearchFilter.searchArea == SearchFilter.SearchArea.AllAssets ||
                    m_SearchFilter.searchArea == SearchFilter.SearchArea.InAssetsOnly ||
                    m_SearchFilter.searchArea == SearchFilter.SearchArea.InPackagesOnly ||
                    m_SearchFilter.searchArea == SearchFilter.SearchArea.SelectedFolders)
                    m_LastLocalAssetsSearchArea = m_SearchFilter.searchArea;
            }
        }

        void InitSearchMenu()
        {
            SearchViewState state = GetSearchViewState();
            if (state == SearchViewState.NotSearching)
                return;

            List<ExposablePopupMenu.ItemData> buttonData = new List<ExposablePopupMenu.ItemData>();

            GUIStyle onStyle = s_Styles.exposablePopupItem;
            GUIStyle offStyle = s_Styles.exposablePopupItem;
            bool hasFolderSelected = m_SearchFilter.folders.Length > 0;

            bool on = state == SearchViewState.AllAssets;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchAllAssets, on ? onStyle : offStyle, on, true, (int)SearchViewState.AllAssets));
            on = state == SearchViewState.InPackagesOnly;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchInPackagesOnly, on ? onStyle : offStyle, on, true, (int)SearchViewState.InPackagesOnly));
            on = state == SearchViewState.InAssetsOnly;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchInAssetsOnly, on ? onStyle : offStyle, on, true, (int)SearchViewState.InAssetsOnly));
            on = state == SearchViewState.SubFolders;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchInFolders, on ? onStyle : offStyle, on, hasFolderSelected, (int)SearchViewState.SubFolders));

            GUIContent popupButtonContent = m_SearchAllAssets;
            switch (state)
            {
                case SearchViewState.AllAssets:
                    popupButtonContent = m_SearchAllAssets;
                    break;
                case SearchViewState.InPackagesOnly:
                    popupButtonContent = m_SearchInPackagesOnly;
                    break;
                case SearchViewState.InAssetsOnly:
                    popupButtonContent = m_SearchInAssetsOnly;
                    break;
                case SearchViewState.SubFolders:
                    popupButtonContent = m_SearchInFolders;
                    break;
                case SearchViewState.NotSearching:
                    popupButtonContent = m_SearchInAssetsOnly;
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }

            ExposablePopupMenu.PopupButtonData popListData = new ExposablePopupMenu.PopupButtonData(popupButtonContent, s_Styles.exposablePopup);
            m_SearchAreaMenu.Init(buttonData, 10f, 450f, popListData, SearchButtonClickedCallback);
        }

        void RefreshSearchText()
        {
            if (m_SearchFilter.folders.Length > 0)
            {
                string[] baseFolders = ProjectWindowUtil.GetBaseFolders(m_SearchFilter.folders);

                string folderText = "";
                string toolTip = "";
                int maxShow = 3;
                for (int i = 0; i < baseFolders.Length && i < maxShow; ++i)
                {
                    var baseFolder = baseFolders[i];
                    var packageInfo = PackageManager.PackageInfo.FindForAssetPath(baseFolder);
                    if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.displayName))
                        baseFolder = Regex.Replace(baseFolder, @"^" + packageInfo.assetPath, PackageManager.Folders.GetPackagesPath() + "/" + packageInfo.displayName);
                    if (i > 0)
                        folderText += ", ";
                    string folderName = Path.GetFileName(baseFolder);
                    folderText += "'" + folderName + "'";

                    if (i == 0 && folderName != "Assets" && folderName != PackageManager.Folders.GetPackagesPath())
                        toolTip = baseFolder;
                }
                if (baseFolders.Length > maxShow)
                    folderText += " +";

                m_SearchInFolders.text = folderText;
                m_SearchInFolders.tooltip = toolTip;
            }
            else
            {
                m_SearchInFolders.text = "Selected folder";
                m_SearchInFolders.tooltip = "";
            }

            m_BreadCrumbs.Clear();
            InitSearchMenu();
        }

        void EnsureValidSetup()
        {
            if (m_LastProjectPath != Directory.GetCurrentDirectory())
            {
                // Clear project specific serialized state
                m_SearchFilter = new SearchFilter();
                m_LastFolders = new string[0];
                SyncFilterGUI();

                // If we have a selection try to frame it (could be a non asset object)
                if (Selection.activeInstanceID != 0)
                    FrameObjectPrivate(Selection.activeInstanceID, !m_LockTracker.isLocked, false);

                // If we did not frame anything above ensure to show Assets folder in two column mode
                if (m_ViewMode == ViewMode.TwoColumns && !IsShowingFolderContents())
                    SelectAssetsFolder();

                m_LastProjectPath = Directory.GetCurrentDirectory();
            }

            // In two column do not allow empty search filter, at least show the Assets folders
            if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.EmptySearchFilter)
            {
                SelectAssetsFolder();
            }
        }

        void OnGUIAssetCallback(int instanceID, Rect rect)
        {
            // User hook for rendering stuff on top of assets
            if (EditorApplication.projectWindowItemOnGUI != null)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(instanceID));
                EditorApplication.projectWindowItemOnGUI(guid, rect);
            }

            if (EditorApplication.projectWindowItemInstanceOnGUI != null)
            {
                EditorApplication.projectWindowItemInstanceOnGUI(instanceID, rect);
            }
        }

        private void InitOneColumnView()
        {
            m_AssetTree = new TreeViewController(this, m_AssetTreeState);
            m_AssetTree.deselectOnUnhandledMouseDown = true;
            m_AssetTree.selectionChangedCallback += AssetTreeSelectionCallback;
            m_AssetTree.keyboardInputCallback += AssetTreeKeyboardInputCallback;
            m_AssetTree.contextClickItemCallback += AssetTreeViewContextClick;
            m_AssetTree.contextClickOutsideItemsCallback += AssetTreeViewContextClickOutsideItems;
            m_AssetTree.itemDoubleClickedCallback += AssetTreeItemDoubleClickedCallback;
            m_AssetTree.onGUIRowCallback += OnGUIAssetCallback;
            m_AssetTree.dragEndedCallback += AssetTreeDragEnded;

            var data = new AssetsTreeViewDataSource(m_AssetTree, m_SkipHiddenPackages);
            data.foldersFirst = GetShouldShowFoldersFirst();

            m_AssetTree.Init(m_TreeViewRect,
                data,
                new AssetsTreeViewGUI(m_AssetTree),
                new AssetsTreeViewDragging(m_AssetTree)
            );
            m_AssetTree.ReloadData();
        }

        private void InitTwoColumnView()
        {
            m_FolderTree = new TreeViewController(this, m_FolderTreeState);
            m_FolderTree.deselectOnUnhandledMouseDown = false;
            m_FolderTree.selectionChangedCallback += FolderTreeSelectionCallback;
            m_FolderTree.contextClickItemCallback += FolderTreeViewContextClick;
            m_FolderTree.onGUIRowCallback += OnGUIAssetCallback;
            m_FolderTree.dragEndedCallback += FolderTreeDragEnded;
            m_FolderTree.Init(m_TreeViewRect,
                new ProjectBrowserColumnOneTreeViewDataSource(m_FolderTree, m_SkipHiddenPackages),
                new ProjectBrowserColumnOneTreeViewGUI(m_FolderTree),
                new ProjectBrowserColumnOneTreeViewDragging(m_FolderTree)
            );
            m_FolderTree.ReloadData();
        }

        void InitViewMode(ViewMode viewMode)
        {
            m_ViewMode = viewMode;

            // Reset
            m_FolderTree = null;
            m_AssetTree = null;

            useTreeViewSelectionInsteadOfMainSelection = false;

            switch (m_ViewMode)
            {
                case ViewMode.OneColumn:
                    InitOneColumnView();
                    break;
                case ViewMode.TwoColumns:
                    InitTwoColumnView();
                    break;
            }

            float minWidth = (m_ViewMode == ViewMode.OneColumn) ? k_MinWidthOneColumn : k_MinWidthTwoColumns;
            //if (position.width < minWidth)
            //  Debug.LogError ("ProjectBrowser: Ensure that mode cannot be changed resulting in an invalid min width: " + minWidth + " (cur width " + position.width + ")");

            minSize = new Vector2(minWidth, k_MinHeight);
            maxSize = new Vector2(10000, 10000);
        }

        private bool GetShouldShowFoldersFirst()
        {
            return Application.platform != RuntimePlatform.OSXEditor;
        }

        // Called when user changes view mode
        void SetViewMode(ViewMode newViewMode)
        {
            if (m_ViewMode == newViewMode)
                return;

            EndRenaming();
            InitViewMode(m_ViewMode == ViewMode.OneColumn ? ViewMode.TwoColumns : ViewMode.OneColumn);

            // Ensure same selection is framed in new view mode
            if (Selection.activeInstanceID != 0)
                FrameObjectPrivate(Selection.activeInstanceID, !m_LockTracker.isLocked, false);

            RepaintImmediately();
        }

        public void EndRenaming()
        {
            if (m_AssetTree != null)
                m_AssetTree.EndNameEditing(true);

            if (m_FolderTree != null)
                m_FolderTree.EndNameEditing(true);

            if (m_ListArea != null)
                m_ListArea.EndRename(true);
        }

        Dictionary<string, string[]> GetTypesDisplayNames()
        {
            return new Dictionary<string, string[]>
            {
                { "Animation Clip", new [] { "AnimationClip" } },
                { "Audio Clip", new [] { "AudioClip"} },
                { "Audio Mixer", new [] { "AudioMixer" } },
                { "Compute Shader", new [] { "ComputeShader" } },
                { "Font", new [] { "Font" } },
                { "GUI Skin", new [] { "GUISkin" } },
                { "Graph", new [] { "GraphAsset", "VisualEffectAsset", "ScriptGraphAsset" } },
                { "Material", new [] { "Material" } },
                { "Mesh", new [] { "Mesh" } },
                { "Model", new [] { "Model" } },
                { "Physics Material", new [] { "PhysicsMaterial" } },
                { "Prefab", new [] { "Prefab" } },
                { "Scene", new [] { "Scene"} },
                { "Script", new [] { "Script" } },
                { "Shader", new [] { "Shader" } },
                { "Sprite", new [] { "Sprite" } },
                { "Texture", new [] { "Texture" } },
                { "Video Clip", new [] { "VideoClip" } },
                { "Visual Effect Asset", new [] { "VisualEffectAsset", "VisualEffectSubgraph" } },

                // "Texture2D",
                // "RenderTexture",
                // "Cubemap",
                // "MovieTexture",
            };
        }

        public void TypeListCallback(PopupList.ListElement element)
        {
            if (!Event.current.control)
            {
                // Clear all selection except for clicked element
                foreach (var item in m_ObjectTypes.m_ListElements)
                    if (item != element)
                        item.selected = false;
            }

            // Toggle clicked element
            element.selected = !element.selected;
            string[] selectedDisplayNames = m_ObjectTypes.m_ListElements.Where(x => x.selected).SelectMany(x => x.types).ToArray();

            m_SearchFilter.classNames = selectedDisplayNames;
            m_SearchFieldText = m_SearchFilter.FilterToSearchFieldString();

            TopBarSearchSettingsChanged();
            Repaint();
        }

        public void AssetLabelListCallback(PopupList.ListElement element)
        {
            if (!Event.current.control)
            {
                // Clear all selection except for clicked element
                foreach (var item in m_AssetLabels.m_ListElements)
                    if (item != element)
                        item.selected = false;
            }

            // Toggle clicked element
            element.selected = !element.selected;
            m_SearchFilter.assetLabels = (from item in m_AssetLabels.m_ListElements where item.selected select item.text).ToArray();

            m_SearchFieldText = m_SearchFilter.FilterToSearchFieldString();

            TopBarSearchSettingsChanged();
            Repaint();
        }

        public void LogTypeListCallback(PopupList.ListElement element)
        {
            if (!Event.current.control)
            {
                // Clear all selection except for clicked element
                foreach (var item in m_LogTypes.m_ListElements)
                    if (item != element)
                        item.selected = false;
            }

            // Toggle clicked element
            element.selected = !element.selected;

            m_SearchFilter.importLogFlags = element.selected ? (UnityEditor.AssetImporters.ImportLogFlags)Enum.Parse(typeof(UnityEditor.AssetImporters.ImportLogFlags), element.types.First()) : ImportLogFlags.None;
            m_SearchFieldText = m_SearchFilter.FilterToSearchFieldString();

            TopBarSearchSettingsChanged();
            Repaint();
        }

        void SetupDroplists()
        {
            SetupAssetLabelList();
            SetupLogTypeList();

            // Types
            m_ObjectTypes = new PopupList.InputData();
            m_ObjectTypes.m_CloseOnSelection = false;
            m_ObjectTypes.m_AllowCustom = false;
            m_ObjectTypes.m_OnSelectCallback = TypeListCallback;
            m_ObjectTypes.m_SortAlphabetically = false;
            m_ObjectTypes.m_MaxCount = 0;
            var types = GetTypesDisplayNames();
            foreach (var keyPair in types)
            {
                m_ObjectTypes.AddElement(keyPair.Key, keyPair.Value);
            }
            m_ObjectTypes.m_ListElements[0].selected = true;
        }

        void SetupAssetLabelList()
        {
            // List of predefined asset labels (and in the future also user defined ones)
            Dictionary<string, float> tags = AssetDatabase.GetAllLabels();

            // AssetLabels
            m_AssetLabels = new PopupList.InputData();
            m_AssetLabels.m_CloseOnSelection = false;
            m_AssetLabels.m_AllowCustom = true;
            m_AssetLabels.m_OnSelectCallback = AssetLabelListCallback;
            m_AssetLabels.m_MaxCount = 15;
            m_AssetLabels.m_SortAlphabetically = true;

            foreach (var pair in tags)
            {
                PopupList.ListElement element = m_AssetLabels.NewOrMatchingElement(pair.Key);
                if (element.filterScore < pair.Value)
                    element.filterScore = pair.Value;
            }
        }

        void SetupLogTypeList()
        {
            m_LogTypes = new PopupList.InputData();
            m_LogTypes.m_CloseOnSelection = false;
            m_LogTypes.m_AllowCustom = false;
            m_LogTypes.m_OnSelectCallback = LogTypeListCallback;
            m_LogTypes.m_MaxCount = 2;
            m_LogTypes.m_SortAlphabetically = true;

            m_LogTypes.AddElement("Warnings", new string[] { "Warning" });
            m_LogTypes.AddElement("Errors", new string[] { "Error" });
        }

        void SyncFilterGUI()
        {
            // Sync Labels
            List<string> assetLabels = new List<string>(m_SearchFilter.assetLabels);
            foreach (PopupList.ListElement item in m_AssetLabels.m_ListElements)
                item.selected = assetLabels.Contains(item.text);

            // Sync Type
            List<string> classNames = new List<string>(m_SearchFilter.classNames);
            foreach (PopupList.ListElement item in m_ObjectTypes.m_ListElements)
                item.selected = classNames.Contains(item.text);

            // Sync Text field
            m_SearchFieldText = m_SearchFilter.FilterToSearchFieldString();
        }

        void ShowFolderContents(int folderInstanceID, bool revealAndFrameInFolderTree)
        {
            if (m_ViewMode != ViewMode.TwoColumns)
                Debug.LogError("ShowFolderContents should only be called in two column mode");

            if (folderInstanceID == 0)
                return;

            string folderPath = AssetDatabase.GetAssetPath(folderInstanceID);
            if (folderInstanceID == kPackagesFolderInstanceId)
                folderPath = PackageManager.Folders.GetPackagesPath();

            if (!m_SkipHiddenPackages || PackageManagerUtilityInternal.IsPathInVisiblePackage(folderPath))
            {
                m_SearchFilter.ClearSearch();
                m_SearchFilter.folders = new[] { folderPath };
                m_SearchFilter.skipHidden = m_SkipHiddenPackages;
                m_FolderTree.SetSelection(new[] { folderInstanceID }, revealAndFrameInFolderTree);
                FolderTreeSelectionChanged(true);
            }
        }

        bool IsShowingFolderContents()
        {
            return m_SearchFilter.folders.Length > 0;
        }

        void ListGotKeyboardFocus()
        {
        }

        void ListAreaKeyboardCallback()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            if (m_ListArea.BeginRename(0f))
                                Event.current.Use();
                        }
                        else // WindowsEditor
                        {
                            Event.current.Use();
                            OpenListAreaSelection();
                        }
                        break;

                    case KeyCode.DownArrow:
                        if (Application.platform == RuntimePlatform.OSXEditor && Event.current.command)
                        {
                            Event.current.Use();
                            OpenListAreaSelection();
                        }
                        break;
                    case KeyCode.UpArrow:
                        if (Application.platform == RuntimePlatform.OSXEditor && Event.current.command && m_ViewMode == ViewMode.TwoColumns)
                        {
                            ShowParentFolderOfCurrentlySelected();
                            Event.current.Use();
                        }
                        break;
                    case KeyCode.Backspace:
                        if (Application.platform != RuntimePlatform.OSXEditor && m_ViewMode == ViewMode.TwoColumns)
                        {
                            ShowParentFolderOfCurrentlySelected();
                            Event.current.Use();
                        }
                        break;
                    case KeyCode.F2:
                        if (Application.platform != RuntimePlatform.OSXEditor)
                        {
                            if (m_ListArea.BeginRename(0f))
                                Event.current.Use();
                        }
                        break;
                }
            }
        }

        void ShowParentFolderOfCurrentlySelected()
        {
            if (IsShowingFolderContents())
            {
                int[] selectedFolderInstanceIDs = m_FolderTree.GetSelection();
                if (selectedFolderInstanceIDs.Length == 1)
                {
                    TreeViewItem item = m_FolderTree.FindItem(selectedFolderInstanceIDs[0]);
                    if (item != null && item.parent != null && item.id != AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets"))
                    {
                        SetFolderSelection(new[] { item.parent.id }, true);
                        m_ListArea.Frame(item.id, true, false);
                        Selection.activeInstanceID = item.id;
                    }
                }
            }
        }

        void OpenListAreaSelection()
        {
            int[] selectedInstanceIDs = m_ListArea.GetSelection();
            int selectionCount = selectedInstanceIDs.Length;
            if (selectionCount > 0)
            {
                int numFolders = 0;
                foreach (int instanceID in selectedInstanceIDs)
                    if (ProjectWindowUtil.IsFolder(instanceID))
                        numFolders++;

                bool allFolders = numFolders == selectionCount;
                if (allFolders)
                {
                    OpenSelectedFolders();
                    GUIUtility.ExitGUI(); // Exit because if we are mouse clicking to open we are in the middle of iterating list area items..
                }
                else
                {
                    // If any assets in selection open them instead of opening folder
                    OpenAssetSelection(selectedInstanceIDs);

                    Repaint();
                    GUIUtility.ExitGUI(); // Exit because if we are mouse clicking to open we are in the middle of iterating list area items..
                }
            }
        }

        static void OpenAssetSelection(int[] selectedInstanceIDs)
        {
            foreach (int id in selectedInstanceIDs)
            {
                if (AssetDatabase.Contains(id))
                    AssetDatabase.OpenAsset(id);
            }
            GUIUtility.ExitGUI();
        }

        void SetAsLastInteractedProjectBrowser()
        {
            s_LastInteractedProjectBrowser = this;
        }

        void RefreshSelectedPath()
        {
            if (Selection.activeObject != null)
            {
                m_SelectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(m_SelectedPath) && IsInsideHiddenPackage(m_SelectedPath))
                {
                    m_SelectedPath = string.Empty;
                    Selection.activeObject = null;
                }
            }
            else
            {
                m_SelectedPath = string.Empty;
            }

            if (!string.IsNullOrEmpty(m_SelectedPath))
            {
                m_SelectedPathContent = new GUIContent(m_SelectedPath, AssetDatabase.GetCachedIcon(m_SelectedPath))
                {
                    tooltip = m_SelectedPath
                };
            }
            else
            {
                m_SelectedPathContent = new GUIContent();
            }
        }

        static void OpenSelectedFolders()
        {
            ProjectBrowser projectBrowser = s_LastInteractedProjectBrowser;

            if (projectBrowser != null)
            {
                int[] selectedInstanceIDs = projectBrowser.m_ListArea?.GetSelection();

                if (selectedInstanceIDs == null || selectedInstanceIDs.Length == 0)
                    return;

                projectBrowser.EndPing();

                if (projectBrowser.m_ViewMode == ViewMode.TwoColumns)
                {
                    projectBrowser.SetFolderSelection(selectedInstanceIDs, false);
                }
                else if (projectBrowser.m_ViewMode == ViewMode.OneColumn)
                {
                    projectBrowser.ClearSearch(); // shows tree instead of search
                    projectBrowser.m_AssetTree.Frame(selectedInstanceIDs[0], true, false);
                    projectBrowser.m_AssetTree.data.SetExpanded(selectedInstanceIDs[0], true);
                }

                projectBrowser.Repaint();
            }
        }

        // Called from EditorHelper
        [RequiredByNativeCode]
        static void OpenSelectedFoldersInInternalExplorer()
        {
            if (!IsFolderTreeViewContextClick())
            {
                OpenSelectedFolders();
            }
        }

        // Also called from list when navigating by keys
        void ListAreaItemSelectedCallback(bool doubleClicked)
        {
            SetAsLastInteractedProjectBrowser();

            int[] instanceIDs = m_ListArea.GetSelection();
            if (instanceIDs.Length > 0)
            {
                Selection.instanceIDs = instanceIDs;
                m_SearchFilter.searchArea = m_LastLocalAssetsSearchArea; // local asset was selected
                m_InternalSelectionChange = true;
            }
            else
            {
                Selection.activeObject = null;
            }

            if (Selection.instanceIDs != m_ListArea.GetSelection())
            {
                m_ListArea.InitSelection(Selection.instanceIDs);
            }

            m_FocusSearchField = false;

            if (Event.current.button == 1 && Event.current.type == EventType.MouseDown)
                m_ItemSelectedByRightClickThisEvent = true;

            RefreshSelectedPath();

            m_DidSelectSearchResult = m_SearchFilter.IsSearching();

            if (doubleClicked)
                OpenListAreaSelection();
        }

        void OnGotFocus()
        {
        }

        void OnLostFocus()
        {
            isFolderTreeViewContextClicked = false;
            // Added because this window uses RenameOverlay
            EndRenaming();
        }

        bool CanFrameAsset(int instanceID)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(path))
                return false;

            HierarchyProperty h = new HierarchyProperty(HierarchyType.Assets, false);
            if (h.Find(instanceID, null))
                return true;

            var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
            if (packageInfo != null)
            {
                h = new HierarchyProperty(packageInfo.assetPath, false);
                if (h.Find(instanceID, null))
                    return true;
            }
            return false;
        }

        void OnSelectionChange()
        {
            // We do not want to init our UI on OnSelectionChange because EditorStyles might not be allocated yet (at play/stop)
            if (m_ListArea == null)
                return;

            // Keep for debugging
            //Debug.Log ("OnSelectionChange (ProjectBrowser): " + DebugUtils.ListToString(new List<int>(Selection.instanceIDs)));

            // The list area selection state is based on the main selection (both in search mode and folderbrowsing)
            m_ListArea.InitSelection(Selection.instanceIDs);

            int instanceID = Selection.instanceIDs.Length > 0 ? Selection.instanceIDs[Selection.instanceIDs.Length - 1] : 0;
            if (m_ViewMode == ViewMode.OneColumn)
            {
                // If searching we are not showing the asset tree but we set selection anyways to ensure its
                // setup when clearing search
                bool revealSelectionAndFrameLast = !m_LockTracker.isLocked && CanFrameAsset(instanceID) && Selection.instanceIDs.Length <= 1;
                m_AssetTree.SetSelection(Selection.instanceIDs, revealSelectionAndFrameLast);
            }
            else if (m_ViewMode == ViewMode.TwoColumns)
            {
                if (!m_InternalSelectionChange)
                {
                    bool frame = !m_LockTracker.isLocked && Selection.instanceIDs.Length > 0 && CanFrameAsset(instanceID);
                    if (frame)
                    {
                        // If searching we keep the search when framing. If folder browsing we change folder
                        // and frame current selection in its folder
                        if (m_SearchFilter.IsSearching())
                        {
                            m_ListArea.Frame(instanceID, true, false);
                        }
                        else
                        {
                            FrameObjectInTwoColumnMode(instanceID, true, false);
                        }
                    }
                }
            }

            m_InternalSelectionChange = false;

            RefreshSelectedPath();
            Repaint();
        }

        void SetFoldersInSearchFilter(int[] selectedInstanceIDs)
        {
            m_SearchFilter.folders = GetFolderPathsFromInstanceIDs(selectedInstanceIDs);
            EnsureValidFolders();

            if (selectedInstanceIDs.Length > 0)
            {
                if (m_LastFoldersGridSize > 0)
                    m_ListArea.gridSize = (int)m_LastFoldersGridSize;
            }
        }

        internal void SetFolderSelection(int[] selectedInstanceIDs, bool revealSelectionAndFrameLastSelected)
        {
            SetFolderSelection(selectedInstanceIDs, revealSelectionAndFrameLastSelected, true);
        }

        private void SetFolderSelection(int[] selectedInstanceIDs, bool revealSelectionAndFrameLastSelected, bool folderWasSelected)
        {
            m_FolderTree.SetSelection(selectedInstanceIDs, revealSelectionAndFrameLastSelected);
            SetFoldersInSearchFilter(selectedInstanceIDs);
            FolderTreeSelectionChanged(folderWasSelected);
        }

        void AssetTreeItemDoubleClickedCallback(int instanceID)
        {
            OpenAssetSelection(Selection.instanceIDs);
        }

        void AssetTreeKeyboardInputCallback()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            Event.current.Use();
                            OpenAssetSelection(Selection.instanceIDs);
                        }
                        break;

                    case KeyCode.DownArrow:
                        if (Application.platform == RuntimePlatform.OSXEditor && Event.current.command)
                        {
                            Event.current.Use();
                            OpenAssetSelection(Selection.instanceIDs);
                        }
                        break;
                }
            }
        }

        void AssetTreeSelectionCallback(int[] selectedTreeViewInstanceIDs)
        {
            SetAsLastInteractedProjectBrowser();

            if (selectedTreeViewInstanceIDs.Length > 0)
                Selection.SetSelectionWithActiveInstanceID(selectedTreeViewInstanceIDs, selectedTreeViewInstanceIDs[0]);
            else
                Selection.activeInstanceID = 0;

            // The selection could be cancelled if an Inspector with hasUnsavedChanges is opened.
            // In that case, let's update the tree so the highlight is set back to the actual selection.
            if (Selection.instanceIDs != selectedTreeViewInstanceIDs)
                m_AssetTree.SetSelection(Selection.instanceIDs, true);

            RefreshSelectedPath();
            SetSearchFoldersFromCurrentSelection();
            RefreshSearchText();
        }

        void SetSearchFoldersFromCurrentSelection()
        {
            HashSet<string> folders = new HashSet<string>();

            foreach (int instanceID in Selection.instanceIDs)
            {
                if (instanceID == kPackagesFolderInstanceId)
                {
                    folders.Add(PackageManager.Folders.GetPackagesPath());
                    continue;
                }

                if (!AssetDatabase.Contains(instanceID))
                    continue;

                string path = AssetDatabase.GetAssetPath(instanceID);
                if (AssetDatabase.IsValidFolder(path))
                {
                    if (IsInsideHiddenPackage(path))
                        continue;

                    folders.Add(path);
                }
                else
                {
                    // Add containing folder of the selected asset
                    string folderPath = ProjectWindowUtil.GetContainingFolder(path);
                    if (!String.IsNullOrEmpty(folderPath))
                    {
                        if (IsInsideHiddenPackage(folderPath))
                            continue;

                        folders.Add(folderPath);
                    }
                }
            }

            // Set them as folders in search filter (so search in folder works correctly)
            m_SearchFilter.folders = ProjectWindowUtil.GetBaseFolders(folders.ToArray());

            // Keep for debugging
            // Debug.Log ("Search folders: " + DebugUtils.ListToString(new List<string>(m_SearchFilter.folders)));
        }

        void SetSearchFolderFromFolderTreeSelection()
        {
            if (m_FolderTree == null)
                return;

            m_SearchFilter.folders = GetFolderPathsFromInstanceIDs(m_FolderTree.GetSelection());

            if (m_SearchFilter.folders.Length != 0)
                return;

            //If we fail to find the folder path from the selected ID then probably the selection could be from Favorites.
            //At any point of time there can only be one selection from Favorites..
            //The Favorites have a custom InstanceID(starting from 1000000000) different from Assets and are saved in a cache,
            //Since we cant get the path from the AssetsUtility with these InstanceIDs we need to get them from cache.
            if (m_FolderTree.GetSelection().Length == 1)
            {
                int selectionID = m_FolderTree.GetSelection()[0];
                ItemType type = GetItemType(selectionID);
                if (type == ItemType.SavedFilter)
                {
                    SearchFilter filter = SavedSearchFilters.GetFilter(selectionID);

                    // Check if the filter is valid (the root of filters are not an actual filter)
                    if (ValidateFilter(selectionID, filter))
                    {
                        m_SearchFilter = filter;
                    }
                }
            }
        }

        void FolderTreeSelectionCallback(int[] selectedTreeViewInstanceIDs)
        {
            SetAsLastInteractedProjectBrowser();

            // Assumes only asset folders can be multi selected
            int firstTreeViewInstanceID = 0;
            if (selectedTreeViewInstanceIDs.Length > 0)
                firstTreeViewInstanceID = selectedTreeViewInstanceIDs[0];

            bool folderWasSelected = false;
            if (firstTreeViewInstanceID != 0)
            {
                ItemType type = GetItemType(firstTreeViewInstanceID);

                if (type == ItemType.Asset)
                {
                    SetFoldersInSearchFilter(selectedTreeViewInstanceIDs);

                    folderWasSelected = true;
                }

                if (type == ItemType.SavedFilter)
                {
                    SearchFilter filter = SavedSearchFilters.GetFilter(firstTreeViewInstanceID);

                    // Check if the filter is valid (the root of filters are not an actual filter)
                    if (ValidateFilter(firstTreeViewInstanceID, filter))
                    {
                        m_SearchFilter = filter;
                        EnsureValidFolders();
                        float previewSize = filter.GetState() == SearchFilter.State.FolderBrowsing ?
                            m_LastFoldersGridSize :
                            SavedSearchFilters.GetPreviewSize(firstTreeViewInstanceID);
                        if (previewSize > 0f)
                            m_ListArea.gridSize = Mathf.Clamp((int)previewSize, m_ListArea.minGridSize, m_ListArea.maxGridSize);
                        SyncFilterGUI();
                    }
                }
            }

            FolderTreeSelectionChanged(folderWasSelected);
        }

        bool ValidateFilter(int savedFilterID, SearchFilter filter)
        {
            if (filter == null)
                return false;

            // Folder validation
            SearchFilter.State state = filter.GetState();
            if (state == SearchFilter.State.FolderBrowsing || state == SearchFilter.State.SearchingInFolders)
            {
                foreach (string folderPath in filter.folders)
                {
                    int instanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(folderPath);
                    if (instanceID == 0)
                    {
                        if (EditorUtility.DisplayDialog("Folder not found", "The folder '" + folderPath + "' might have been deleted or belong to another project. Do you want to delete the favorite?", "Delete", "Cancel"))
                        {
                            SavedSearchFilters.RemoveSavedFilter(savedFilterID);
                            GUIUtility.ExitGUI(); // exit gui since we are iterating items we just reloaded
                        }

                        return false;
                    }
                }
            }
            return true;
        }

        void ShowAndHideFolderTreeSelectionAsNeeded()
        {
            if (m_ViewMode == ViewMode.TwoColumns && m_FolderTree != null)
            {
                bool isSavedFilterSelected = false;
                int[] selection = m_FolderTree.GetSelection();
                if (selection.Length > 0)
                {
                    isSavedFilterSelected = GetItemType(selection[0]) == ItemType.SavedFilter;
                }

                SearchViewState state = GetSearchViewState();
                switch (state)
                {
                    case SearchViewState.AllAssets:
                    case SearchViewState.InAssetsOnly:
                    case SearchViewState.InPackagesOnly:
                    case SearchViewState.SubFolders:
                    case SearchViewState.NotSearching:
                        {
                            if (!isSavedFilterSelected)
                                m_FolderTree.SetSelection(GetFolderInstanceIDs(m_SearchFilter.folders), true);
                        }
                        break;
                }
            }
        }

        // This method is being used by the EditorTests/Searching tests
        public string[] GetCurrentVisibleNames()
        {
            return m_ListArea.GetCurrentVisibleNames();
        }

        void InitListArea()
        {
            ShowAndHideFolderTreeSelectionAsNeeded();

            m_SearchFilter.skipHidden = m_SkipHiddenPackages;

            m_ListArea?.InitForSearch(m_ListAreaRect, HierarchyType.Assets,
                m_SearchFilter, false,
                s => AssetDatabase.GetMainAssetInstanceID(s));
            m_ListArea?.InitSelection(Selection.instanceIDs);
        }

        void OnInspectorUpdate()
        {
            if (m_ListArea != null)
                m_ListArea.OnInspectorUpdate();
        }

        void OnDestroy()
        {
            if (m_ListArea != null)
                m_ListArea.OnDestroy();

            if (this == s_LastInteractedProjectBrowser)
                s_LastInteractedProjectBrowser = null;
        }

        static void DeleteFilter(int filterInstanceID)
        {
            if (SavedSearchFilters.GetRootInstanceID() == filterInstanceID)
            {
                string title = "Cannot Delete";
                EditorUtility.DisplayDialog(title, "Deleting the 'Filters' root is not allowed", "OK");
            }
            else
            {
                string title = "Delete selected favorite?";
                if (EditorUtility.DisplayDialog(title, "You cannot undo this action.", "Delete", "Cancel"))
                {
                    SavedSearchFilters.RemoveSavedFilter(filterInstanceID);
                }
            }
        }

        [UsedByNativeCode]
        internal static string GetSelectedPath()
        {
            if (s_LastInteractedProjectBrowser == null)
                return string.Empty;

            return s_LastInteractedProjectBrowser.m_SelectedPath;
        }

        // Also called from C++ (used for AssetsMenu check if selection is Packages folder)
        [UsedByNativeCode]
        internal static bool SelectionIsPackagesRootFolder()
        {
            var pb = s_LastInteractedProjectBrowser;
            if (pb == null)
                return false;

            if (pb.m_ViewMode == ViewMode.OneColumn &&
                pb.m_AssetTree != null &&
                pb.m_AssetTree.IsSelected(kPackagesFolderInstanceId))
            {
                return true;
            }

            if (pb.m_ViewMode == ViewMode.TwoColumns &&
                (pb.useTreeViewSelectionInsteadOfMainSelection || Selection.activeInstanceID == 0) &&
                pb.m_FolderTree != null &&
                pb.m_FolderTree.IsSelected(kPackagesFolderInstanceId))
            {
                return true;
            }

            return Selection.activeInstanceID == kPackagesFolderInstanceId;
        }

        private static bool ShouldDiscardCommandsEventsForImmutablePackages()
        {
            var evt = Event.current;
            if ((evt.type == EventType.ExecuteCommand || evt.type == EventType.ValidateCommand || evt.keyCode == KeyCode.Escape) &&
                (evt.commandName == EventCommandNames.Cut ||
                 evt.commandName == EventCommandNames.Paste ||
                 evt.commandName == EventCommandNames.Delete ||
                 evt.commandName == EventCommandNames.SoftDelete ||
                 evt.commandName == EventCommandNames.Duplicate))
            {
                if (AssetsMenuUtility.SelectionHasImmutable())
                    return true;

                if (SelectionIsPackagesRootFolder())
                    return true;
            }

            return false;
        }

        private static bool ShouldDiscardCommandsEventsForRootFolders()
        {
            return ((Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.ValidateCommand) &&
                Event.current.commandName == EventCommandNames.Delete ||
                Event.current.commandName == EventCommandNames.SoftDelete)
                && !CanDeleteSelectedAssets();
        }

        void HandleCommandEventsForTreeView()
        {
            // Handle all event for tree view
            var evt = Event.current;
            EventType eventType = evt.type;
            if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand)
            {
                bool execute = eventType == EventType.ExecuteCommand;

                int[] instanceIDs = m_FolderTree.GetSelection();
                if (instanceIDs.Length == 0)
                    return;

                // Only one type can be selected at a time (and savedfilters can only be single-selected)
                ItemType itemType = GetItemType(instanceIDs[0]);

                // Check if event made on immutable package
                if (itemType == ItemType.Asset)
                {
                    if (ShouldDiscardCommandsEventsForImmutablePackages())
                    {
                        EditorUtility.DisplayDialog(L10n.Tr("Invalid Operation"), L10n.Tr(string.Format(k_ImmutableSelectionActionFormat, Event.current.commandName)), L10n.Tr("OK"));
                        return;
                    }
                    if (ShouldDiscardCommandsEventsForRootFolders())
                    {
                        EditorUtility.DisplayDialog(L10n.Tr("Invalid Operation"), L10n.Tr("Deleting a root folder is not allowed."), L10n.Tr("OK"));
                        return;
                    }
                }

                if (evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete)
                {
                    evt.Use();
                    if (execute)
                    {
                        if (itemType == ItemType.SavedFilter)
                        {
                            System.Diagnostics.Debug.Assert(instanceIDs.Length == 1); //We do not support multiselection for filters
                            DeleteFilter(instanceIDs[0]);
                            GUIUtility.ExitGUI(); // exit gui since we are iterating items we just reloaded
                        }
                        else if (itemType == ItemType.Asset)
                        {
                            bool askIfSure = Event.current.commandName == EventCommandNames.SoftDelete;
                            DeleteSelectedAssets(askIfSure);
                            if (askIfSure)
                                Focus(); // Workaround that we do not get focus back when dialog is closed
                        }
                    }
                    GUIUtility.ExitGUI();
                }
                else if (evt.commandName == EventCommandNames.Duplicate)
                {
                    if (execute)
                    {
                        if (itemType == ItemType.SavedFilter)
                        {
                            // TODO copy filter (get new name as assets)
                        }
                        else if (itemType == ItemType.Asset)
                        {
                            evt.Use();
                            int[] copiedFolders = AssetClipboardUtility.DuplicateFolders(instanceIDs);
                            SetFolderSelection(copiedFolders, true);
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        evt.Use();
                    }
                }
                else if (evt.commandName == EventCommandNames.Cut)
                {
                    evt.Use();
                    if (execute && itemType == ItemType.Asset)
                    {
                        AssetClipboardUtility.CutCopySelectedFolders(instanceIDs, AssetClipboardUtility.PerformedAction.Cut);
                        GUIUtility.ExitGUI();
                    }
                }
                else if (evt.commandName == EventCommandNames.Copy)
                {
                    evt.Use();
                    if (execute && itemType == ItemType.Asset)
                    {
                        AssetClipboardUtility.CutCopySelectedFolders(instanceIDs, AssetClipboardUtility.PerformedAction.Copy);
                        GUIUtility.ExitGUI();
                    }
                }
                else if (evt.commandName == EventCommandNames.Paste)
                {
                    evt.Use();
                    if (execute && itemType == ItemType.Asset && AssetClipboardUtility.CanPaste())
                    {
                        int[] copiedFolders = AssetClipboardUtility.PasteFolders();
                        SetFolderSelection(copiedFolders, true);
                        GUIUtility.ExitGUI();
                    }
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    evt.Use();
                    AssetClipboardUtility.CancelCut();
                    GUIUtility.ExitGUI();
                }
            }
        }

        public const string FocusProjectWindowCommand = "FocusProjectWindow";

        void HandleCommandEvents()
        {
            // Check if event made on immutable package
            if (ShouldDiscardCommandsEventsForImmutablePackages())
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, k_WarningImmutableSelectionFormat, Event.current.commandName);
                return;
            }
            // Check if event is delete on root folder
            // Note that if the folder is in favorite, there is no need for root check.
            // Because we only remove it from the favorite list, not delete the asset.
            if (!SelectionIsFavorite() && ShouldDiscardCommandsEventsForRootFolders())
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, k_WarningRootFolderDeletionFormat, Event.current.commandName);
                return;
            }

            var evt = Event.current;
            EventType eventType = evt.type;
            if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand || evt.keyCode == KeyCode.Escape)
            {
                bool execute = eventType == EventType.ExecuteCommand;

                if (evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete)
                {
                    evt.Use();
                    if (execute)
                    {
                        bool askIfSure = evt.commandName == EventCommandNames.SoftDelete;
                        DeleteSelectedAssets(askIfSure);
                        if (askIfSure)
                            Focus(); // Workaround that we do not get focus back when dialog is closed
                    }
                    GUIUtility.ExitGUI();
                }
                else if (evt.commandName == EventCommandNames.Duplicate)
                {
                    if (execute)
                    {
                        evt.Use();
                        AssetClipboardUtility.DuplicateSelectedAssets();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
                        if (selectedAssets.Length != 0)
                            evt.Use();
                    }
                }
                else if (evt.commandName == EventCommandNames.Cut)
                {
                    evt.Use();
                    AssetClipboardUtility.CutCopySelectedAssets(AssetClipboardUtility.PerformedAction.Cut);
                    Repaint();
                }
                else if (evt.commandName == EventCommandNames.Copy)
                {
                    evt.Use();
                    AssetClipboardUtility.CutCopySelectedAssets(AssetClipboardUtility.PerformedAction.Copy);
                }
                else if (evt.commandName == EventCommandNames.Paste)
                {
                    evt.Use();
                    if (execute)
                        AssetClipboardUtility.PasteSelectedAssets(m_ViewMode == ViewMode.TwoColumns);
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    AssetClipboardUtility.CancelCut();
                    Repaint();
                    GUIUtility.ExitGUI();
                }
                else if (evt.commandName == FocusProjectWindowCommand)
                {
                    if (execute)
                    {
                        FrameObjectPrivate(Selection.activeInstanceID, true, false);
                        evt.Use();
                        Focus();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        evt.Use();
                    }
                }
                else if (evt.commandName == EventCommandNames.SelectAll)
                {
                    if (execute)
                        SelectAll();
                    evt.Use();
                }
                // Frame selected assets
                else if (evt.commandName == EventCommandNames.FrameSelected)
                {
                    if (execute)
                    {
                        FrameObjectPrivate(Selection.activeInstanceID, true, false);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    evt.Use();
                }
                else if (evt.commandName == EventCommandNames.Find)
                {
                    if (execute)
                        m_FocusSearchField = true;
                    evt.Use();
                }
            }
        }

        void SelectAll()
        {
            if (m_ViewMode == ViewMode.OneColumn)
            {
                if (m_SearchFilter.IsSearching())
                {
                    m_ListArea.SelectAll();
                }
                else
                {
                    int[] instanceIDs = m_AssetTree.GetRowIDs();
                    m_AssetTree.SetSelection(instanceIDs, false);
                    AssetTreeSelectionCallback(instanceIDs);
                }
            }
            else if (m_ViewMode == ViewMode.TwoColumns)
            {
                m_ListArea.SelectAll();
            }
            else
            {
                Debug.LogError("Missing implementation for ViewMode " + m_ViewMode);
            }
        }

        float GetListHeaderHeight()
        {
            if (!m_SearchFilter.IsSearching())
                return k_ToolbarHeight;
            return m_SearchFilter.GetState() == SearchFilter.State.EmptySearchFilter ? 0f : k_ToolbarHeight;
        }

        void CalculateRects()
        {
            float listHeaderHeight = GetListHeaderHeight();
            if (m_ViewMode == ViewMode.OneColumn)
            {
                m_ListAreaRect = new Rect(0, EditorGUI.kWindowToolbarHeight + listHeaderHeight, position.width, position.height - k_ToolbarHeight - listHeaderHeight - k_BottomBarHeight);
                m_TreeViewRect = new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - k_ToolbarHeight - k_BottomBarHeight);
                m_BottomBarRect = new Rect(0, position.height - k_BottomBarHeight, position.width, k_BottomBarHeight);
                m_ListHeaderRect = new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, listHeaderHeight);
            }
            else //if (m_ViewMode == ViewMode.TwoColumns)
            {
                float listWidth = position.width - m_DirectoriesAreaWidth;

                m_ListAreaRect = new Rect(m_DirectoriesAreaWidth, EditorGUI.kWindowToolbarHeight + listHeaderHeight, listWidth, position.height - k_ToolbarHeight - listHeaderHeight - k_BottomBarHeight);
                m_TreeViewRect = new Rect(0, EditorGUI.kWindowToolbarHeight, m_DirectoriesAreaWidth, position.height - k_ToolbarHeight);
                m_BottomBarRect = new Rect(m_DirectoriesAreaWidth, position.height - k_BottomBarHeight, listWidth, k_BottomBarHeight);
                m_ListHeaderRect = new Rect(m_ListAreaRect.x, EditorGUI.kWindowToolbarHeight, m_ListAreaRect.width, listHeaderHeight);
            }
        }

        void EndPing()
        {
            if (m_ViewMode == ViewMode.OneColumn)
            {
                m_AssetTree.EndPing();
            }
            else
            {
                m_FolderTree.EndPing();
                m_ListArea.EndPing();
            }
        }

        void OnEvent()
        {
            // Let components handle new event
            if (m_AssetTree != null)
                m_AssetTree.OnEvent();

            if (m_FolderTree != null)
                m_FolderTree.OnEvent();

            if (m_ListArea != null)
                m_ListArea.OnEvent();
        }

        void OnGUI()
        {
            // Initialize m_Styles
            if (s_Styles == null)
                s_Styles = new Styles();

            if (!Initialized())
                Init();

            // We grab keyboard control ids early to ensure consistency (for focus rendering)
            m_ListKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_TreeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);

            OnEvent();

            m_ItemSelectedByRightClickThisEvent = false;

            // Size splitterRects for different areas of the browser
            ResizeHandling(position.height - k_ToolbarHeight);
            CalculateRects();

            Event evt = Event.current;

            Rect ProjectBrowserRect = new Rect(0, 0, position.width, position.height);
            if (evt.type == EventType.MouseDown && ProjectBrowserRect.Contains(evt.mousePosition))
            {
                EndPing();
                SetAsLastInteractedProjectBrowser();
            }

            if (m_GrabKeyboardFocusForListArea)
            {
                m_GrabKeyboardFocusForListArea = false;
                GUIUtility.keyboardControl = m_ListKeyboardControlID;
            }

            GUI.BeginGroup(ProjectBrowserRect, GUIContent.none);

            TopToolbar();
            BottomBar();

            if (m_ViewMode == ViewMode.OneColumn)
            {
                if (m_SearchFilter.IsSearching())
                {
                    SearchAreaBar();
                    if (GUIUtility.keyboardControl == m_TreeViewKeyboardControlID)
                        GUIUtility.keyboardControl = m_ListKeyboardControlID; // AssetTree is not shown so we can steal keyboard control
                    m_ListArea.OnGUI(m_ListAreaRect, m_ListKeyboardControlID);
                }
                else
                {
                    if (GUIUtility.keyboardControl == m_ListKeyboardControlID)
                        GUIUtility.keyboardControl = m_TreeViewKeyboardControlID; // List is not shown so we can steal keyboard control
                    m_AssetTree.OnGUI(m_TreeViewRect, m_TreeViewKeyboardControlID);
                }
            }
            else // ViewMode.TwoColumns
            {
                if (m_SearchFilter.IsSearching())
                    SearchAreaBar();
                else
                    BreadCrumbBar();

                // Folders
                m_FolderTree.OnGUI(m_TreeViewRect, m_TreeViewKeyboardControlID);

                // List Content
                m_ListArea.OnGUI(m_ListAreaRect, m_ListKeyboardControlID);

                // Vertical splitter line between folders and content (drawn before listarea so listarea ping is drawn on top of line)
                EditorGUIUtility.DrawHorizontalSplitter(new Rect(m_ListAreaRect.x + 1f, EditorGUI.kWindowToolbarHeight, 1, m_TreeViewRect.height));

                if (m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing && m_ListArea.numItemsDisplayed == 0)
                {
                    Vector2 size = EditorStyles.label.CalcSize(s_Styles.m_EmptyFolderText);
                    Rect textRect = new Rect(m_ListAreaRect.x + 2f + Mathf.Max(0, (m_ListAreaRect.width - size.x) * 0.5f), m_ListAreaRect.y + 10f, size.x, 20f);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUI.Label(textRect, s_Styles.m_EmptyFolderText, EditorStyles.label);
                    }
                }
            }

            // Handle ListArea context click after ListArea.OnGUI
            HandleContextClickInListArea(m_ListAreaRect);

            // Ensure we save current grid size
            if (m_ListArea.gridSize != m_StartGridSize)
            {
                m_StartGridSize = m_ListArea.gridSize;
                if (m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing)
                    m_LastFoldersGridSize = m_ListArea.gridSize;
            }

            GUI.EndGroup();

            if (m_ViewMode == ViewMode.TwoColumns)
                useTreeViewSelectionInsteadOfMainSelection = GUIUtility.keyboardControl == m_TreeViewKeyboardControlID;

            // Handle command events AFTER tree and list view since commands events should be handled by text fields first (rename overlay + search field)
            // Let folder/filters tree view try to handle command events first if it has keyboard focus
            if (m_ViewMode == ViewMode.TwoColumns && GUIUtility.keyboardControl == m_TreeViewKeyboardControlID)
                HandleCommandEventsForTreeView();
            HandleCommandEvents();
        }

        void HandleContextClickInListArea(Rect listRect)
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    // This section handles selecting the folders showing their content, if right clicked outside items.
                    // We do this to ensure our assets context menu is operating on the active folder(s)
                    if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing && evt.button == 1 && !m_ItemSelectedByRightClickThisEvent)
                    {
                        if (m_SearchFilter.folders.Length > 0 && listRect.Contains(evt.mousePosition))
                        {
                            m_InternalSelectionChange = true;
                            Selection.instanceIDs = GetFolderInstanceIDs(m_SearchFilter.folders);
                        }
                    }
                    break;

                case EventType.ContextClick:
                    if (listRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = 0;

                        // In safe mode non-scripts assets aren't selectable and therefore if you context click a non-script
                        // asset, then a context menu shouldn't be displayed.
                        if (!EditorUtility.isInSafeMode || Selection.instanceIDs.Length > 0)
                        {
                            // Context click in list area
                            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);
                        }

                        evt.Use();
                    }
                    break;
            }
        }

        void AssetTreeViewContextClick(int clickedItemID)
        {
            Event evt = Event.current;

            if (clickedItemID == 0)
            {
                // For non selectable assets, don't show context menu. Selection is deselected
                m_AssetTree.SetSelection(k_EmptySelection, false);
                AssetTreeSelectionCallback(k_EmptySelection);
            }
            else
            {
                // Context click with a selected Asset
                EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);
            }

            evt.Use();
        }

        void AssetTreeViewContextClickOutsideItems()
        {
            Event evt = Event.current;

            // Deselect all
            if (m_AssetTree.GetSelection().Length > 0)
            {
                m_AssetTree.SetSelection(k_EmptySelection, false);
                AssetTreeSelectionCallback(k_EmptySelection);
            }

            // Context click with no selected assets
            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);

            evt.Use();
        }

        void FolderTreeViewContextClick(int clickedItemID)
        {
            isFolderTreeViewContextClicked = true;
            Event evt = Event.current;
            System.Diagnostics.Debug.Assert(evt.type == EventType.ContextClick);
            if (SavedSearchFilters.IsSavedFilter(clickedItemID))
            {
                // Context click with a selected Filter
                if (clickedItemID != SavedSearchFilters.GetRootInstanceID())
                    SavedFiltersContextMenu.Show(clickedItemID);
            }
            else
            {
                // Context click on a folder (asset)
                EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);
            }
            evt.Use();
        }

        static bool IsFolderTreeViewContextClick()
        {
            ProjectBrowser projectBrowser = s_LastInteractedProjectBrowser;

            if (projectBrowser == null)
            {
                return true; // return true to ignore Context menu's 'Open' option in folder tree in the ProjectBrowser
            }
            else if (projectBrowser.isFolderTreeViewContextClicked)
            {
                projectBrowser.isFolderTreeViewContextClicked = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        void AssetTreeDragEnded(int[] draggedInstanceIds, bool draggedItemsFromOwnTreeView)
        {
            // We only change selection if 'draggedItemsFromOwnTreeView' == true.
            // This ensures that we do not override the selection that might have been set before
            // calling this callback when dragging e.g a gameobject to the projectbrowser (case 628939)
            if (draggedInstanceIds != null && draggedItemsFromOwnTreeView)
            {
                m_AssetTree.SetSelection(draggedInstanceIds, true);
                m_AssetTree.NotifyListenersThatSelectionChanged(); // behave as if selection was performed in treeview
                Repaint();
                GUIUtility.ExitGUI();
            }
        }

        void FolderTreeDragEnded(int[] draggedInstanceIds, bool draggedItemsFromOwnTreeView)
        {
            // In the folder tree we do not want to change selection as we do in the full asset tree when dragging is performed.
            // We do not want to change folder when dragging assets to folders (convention of both OSX and Win)
        }

        // This is our search field
        void TopToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                float listWidth = position.width - m_DirectoriesAreaWidth;
                float spaceBetween = 4f;
                bool compactMode = listWidth < 500; // We need quite some space for filtering text
                if (!compactMode)
                {
                    spaceBetween = 10f;
                }

                CreateDropdown();
                GUILayout.FlexibleSpace();

                GUILayout.Space(spaceBetween * 2f);
                SearchField();
                TypeDropDown();
                AssetLabelsDropDown();
                LogTypeDropDown();
                if (m_ViewMode == ViewMode.TwoColumns)
                {
                    ButtonSaveFilter();
                }
                ToggleHiddenPackagesVisibility();
            }
            GUILayout.EndHorizontal();
        }

        void SetOneColumn()
        {
            SetViewMode(ViewMode.OneColumn);
            EnsureValidSetup();
        }

        void SetTwoColumns()
        {
            SetViewMode(ViewMode.TwoColumns);
            EnsureValidSetup();
        }

        internal bool IsTwoColumns()
        {
            return m_ViewMode == ViewMode.TwoColumns;
        }

        void OpenTreeViewTestWindow()
        {
            GetWindow<TreeViewTestWindow>();
        }

        void ToggleExpansionAnimationPreference()
        {
            bool oldValue = EditorPrefs.GetBool(TreeViewController.kExpansionAnimationPrefKey, false);
            EditorPrefs.SetBool(TreeViewController.kExpansionAnimationPrefKey, !oldValue);
            EditorUtility.RequestScriptReload();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (m_EnableOldAssetTree)
            {
                GUIContent assetTreeText = EditorGUIUtility.TrTextContent("One Column Layout");
                GUIContent assetBrowserText = EditorGUIUtility.TrTextContent("Two Column Layout");

                menu.AddItem(assetTreeText, m_ViewMode == ViewMode.OneColumn, SetOneColumn);
                if (position.width >= k_MinWidthTwoColumns)
                    menu.AddItem(assetBrowserText, m_ViewMode == ViewMode.TwoColumns, SetTwoColumns);
                else
                    menu.AddDisabledItem(assetBrowserText);

                m_LockTracker.AddItemsToMenu(menu);

                if (Unsupported.IsDeveloperMode())
                {
                    menu.AddItem(EditorGUIUtility.TrTextContent("DEVELOPER/Open TreeView Test Window..."), false, OpenTreeViewTestWindow);
                    menu.AddItem(EditorGUIUtility.TrTextContent("DEVELOPER/Use TreeView Expansion Animation"), EditorPrefs.GetBool(TreeViewController.kExpansionAnimationPrefKey, false), ToggleExpansionAnimationPreference);
                }
            }
        }

        float DrawLocalAssetHeader(Rect r)
        {
            return 0;
        }

        void ResizeHandling(float height)
        {
            if (m_ViewMode == ViewMode.OneColumn)
                return;

            // Handle folders vs. items splitter
            const float minDirectoriesAreaWidth = 50f;
            const float minAssetsAreaWidth = 50f;
            Rect dragRect = new Rect(m_DirectoriesAreaWidth, EditorGUI.kWindowToolbarHeight, k_ResizerWidth, height);
            dragRect = EditorGUIUtility.HandleHorizontalSplitter(dragRect, position.width, minDirectoriesAreaWidth, minAssetsAreaWidth);
            m_DirectoriesAreaWidth = dragRect.x;
        }

        void ButtonSaveFilter()
        {
            // Only show when we have a active filter
            using (new EditorGUI.DisabledScope(!m_SearchFilter.IsSearching()))
            {
                if (GUILayout.Button(s_Styles.m_SaveFilterContent, EditorStyles.toolbarButtonRight))
                {
                    ProjectBrowserColumnOneTreeViewGUI ProjectBrowserTreeViewGUI = m_FolderTree.gui as ProjectBrowserColumnOneTreeViewGUI;
                    if (ProjectBrowserTreeViewGUI != null)
                    {
                        bool createNewFilter = true;
                        // If a filter is selected save to that filter
                        int[] treeViewSelection = m_FolderTree.GetSelection();
                        if (treeViewSelection.Length == 1)
                        {
                            int instanceID = treeViewSelection[0];
                            bool isRootFilter = SavedSearchFilters.GetRootInstanceID() == instanceID;

                            // Ask if filter should be overwritten
                            if (SavedSearchFilters.IsSavedFilter(instanceID) && !isRootFilter)
                            {
                                createNewFilter = false;
                                string title = "Overwrite Filter?";
                                string text = "Do you want to overwrite '" + SavedSearchFilters.GetName(instanceID) + "' or create a new filter?";
                                int result = 2; // cancel
                                result = EditorUtility.DisplayDialogComplex(title, text, "Overwrite", "Create", "Cancel");
                                if (result == 0)
                                    SavedSearchFilters.UpdateExistingSavedFilter(instanceID, m_SearchFilter, listAreaGridSize);
                                else if (result == 1)
                                    createNewFilter = true;
                            }
                        }

                        // Otherwise create new item in tree
                        if (createNewFilter)
                        {
                            // User wants to create new filter. We re-focus to ensure rename overlay gets input (dialog stole our focus and we might not get it back)
                            Focus();
                            ProjectBrowserTreeViewGUI.BeginCreateSavedFilter(m_SearchFilter);
                        }
                    }
                }
            }
        }

        void CreateDropdown()
        {
            var isInReadOnlyContext = AssetsMenuUtility.SelectionHasImmutable() || SelectionIsPackagesRootFolder() || !ModeService.HasCapability(ModeCapability.AllowAssetCreation, true);
            EditorGUI.BeginDisabledGroup(isInReadOnlyContext);
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_CreateDropdownContent, EditorStyles.toolbarCreateAddNewDropDown);
            if (EditorGUI.DropdownButton(r, s_Styles.m_CreateDropdownContent, FocusType.Passive, EditorStyles.toolbarCreateAddNewDropDown))
            {
                GUIUtility.hotControl = 0;
                EditorUtility.DisplayPopupMenu(r, "Assets/Create", null);
            }
            EditorGUI.EndDisabledGroup();
        }

        void AssetLabelsDropDown()
        {
            // Labels button
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_FilterByLabel, EditorStyles.toolbarButton);
            if (EditorGUI.DropdownButton(r, s_Styles.m_FilterByLabel, FocusType.Passive, EditorStyles.toolbarButton))
            {
                PopupWindow.Show(r, new PopupList(m_AssetLabels));
            }
        }

        void TypeDropDown()
        {
            // Object type button
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_FilterByType, EditorStyles.toolbarButton);
            if (EditorGUI.DropdownButton(r, s_Styles.m_FilterByType, FocusType.Passive, EditorStyles.toolbarButton))
            {
                PopupWindow.Show(r, new PopupList(m_ObjectTypes));
            }
        }

        void LogTypeDropDown()
        {
            //Log type button
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_FilterByImportLog, EditorStyles.toolbarButton);
            if (EditorGUI.DropdownButton(r, s_Styles.m_FilterByImportLog, FocusType.Passive, EditorStyles.toolbarButton))
            {
                PopupWindow.Show(r, new PopupList(m_LogTypes));
            }
        }

        private void ToggleHiddenPackagesVisibility()
        {
            s_Styles.m_PackageContentDefault = m_SkipHiddenPackages ? s_Styles.m_PackagesContentNotVisible : s_Styles.m_PackagesContentVisible;
            s_Styles.m_PackageContentDefault.text = PackageManagerUtilityInternal.HiddenPackagesCount.ToString();
            var skipHiddenPackage = GUILayout.Toggle(m_SkipHiddenPackages, s_Styles.m_PackageContentDefault, EditorStyles.toolbarButtonRight);

            if (skipHiddenPackage != m_SkipHiddenPackages)
            {
                m_SkipHiddenPackages = skipHiddenPackage;
                EndRenaming();

                if (m_AssetTree != null)
                {
                    var dataSource = m_AssetTree.data as AssetsTreeViewDataSource;
                    dataSource.skipHiddenPackages = m_SkipHiddenPackages;
                }

                if (m_FolderTree != null)
                {
                    var dataSource = m_FolderTree.data as ProjectBrowserColumnOneTreeViewDataSource;
                    dataSource.skipHiddenPackages = m_SkipHiddenPackages;
                }

                ResetViews();
            }
        }

        void SearchField()
        {
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchFieldWithJump, GUILayout.MinWidth(65), GUILayout.MaxWidth(300));
            int searchFieldControlID = EditorGUIUtility.GetControlID(s_HashForSearchField, FocusType.Passive, rect); // We use 'Passive' to ensure we only tab between folder tree and list area. Focus search field by using Ctrl+F.

            if (m_FocusSearchField)
            {
                GUIUtility.keyboardControl = searchFieldControlID;
                EditorGUIUtility.editingTextField = true;
                if (Event.current.type == EventType.Repaint)
                    m_FocusSearchField = false;
            }

            Event evt = Event.current;
            if (GUIUtility.keyboardControl == searchFieldControlID)
            {
                // On arrow down/up switch to control selection in list area
                if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow))
                {
                    if (!m_ListArea.IsLastClickedItemVisible())
                        m_ListArea.SelectFirst();

                    GUIUtility.keyboardControl = m_ListKeyboardControlID;
                    evt.Use();
                }

                SearchService.SearchService.HandleSearchEvent(this, evt, m_SearchFieldText);
            }

            m_lastSearchFilter = EditorGUI.ToolbarSearchField(
                searchFieldControlID,
                rect,
                m_SearchFieldText,
                m_SyncSearch ? EditorStyles.toolbarSearchFieldWithJumpSynced : EditorStyles.toolbarSearchFieldWithJump,
                string.IsNullOrEmpty(m_SearchFieldText) ? EditorStyles.toolbarSearchFieldCancelButtonWithJumpEmpty : EditorStyles.toolbarSearchFieldCancelButtonWithJump);

            if (m_lastSearchFilter != m_SearchFieldText || m_FocusSearchField)
            {
                // Update filter with string
                m_SearchFieldText = m_lastSearchFilter;

                m_NextSearchOffDelegate?.Invoke();
                m_NextSearchOffDelegate = EditorApplication.CallDelayed(UpdateSearchDelayed, searchUpdateDelaySeconds);
            }

            SearchService.SearchService.DrawOpenSearchButton(this, m_SearchFieldText);
        }

        void UpdateSearchDelayed()
        {
            m_SearchFilter.SearchFieldStringToFilter(m_SearchFieldText);
            SyncFilterGUI();
            TopBarSearchSettingsChanged();
            Repaint();
        }

        void TopBarSearchSettingsChanged(bool keyboardValidation = true)
        {
            if (!m_SearchFilter.IsSearching())
            {
                if (m_DidSelectSearchResult)
                {
                    m_DidSelectSearchResult = false;
                    FrameObjectPrivate(Selection.activeInstanceID, true, false);
                    if (GUIUtility.keyboardControl == 0 && keyboardValidation)
                    {
                        // Ensure item has focus for visual feedback and instant key navigation
                        if (m_ViewMode == ViewMode.OneColumn)
                            GUIUtility.keyboardControl = m_TreeViewKeyboardControlID;
                        else if (m_ViewMode == ViewMode.TwoColumns)
                            GUIUtility.keyboardControl = m_ListKeyboardControlID;
                    }
                }
                else if (m_ViewMode == ViewMode.TwoColumns)
                {
                    // Revert to last selected folders
                    if (GUIUtility.keyboardControl == 0 || !keyboardValidation || SelectionIsFavorite())
                    {
                        RevertToLastSelectedFolder(false);
                    }
                }
            }
            else
            {
                if (m_ViewMode == ViewMode.TwoColumns && SelectionIsFavorite())
                    RevertToLastSelectedFolder(false);
                InitSearchMenu();
            }

            InitListArea();
        }

        internal static int GetFolderInstanceID(string folderPath)
        {
            return folderPath == PackageManager.Folders.GetPackagesPath()
                ? kPackagesFolderInstanceId
                : AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(folderPath);
        }

        static int[] GetFolderInstanceIDs(string[] folders)
        {
            int[] folderInstanceIDs = new int[folders.Length];

            for (int i = 0; i < folders.Length; ++i)
            {
                folderInstanceIDs[i] = GetFolderInstanceID(folders[i]);
            }
            return folderInstanceIDs;
        }

        static string[] GetFolderPathsFromInstanceIDs(int[] instanceIDs)
        {
            List<string> paths = new List<string>();
            foreach (int instanceID in instanceIDs)
            {
                if (instanceID == kPackagesFolderInstanceId)
                {
                    paths.Add(PackageManager.Folders.GetPackagesPath());
                    continue;
                }
                string path = AssetDatabase.GetAssetPath(instanceID);
                if (!String.IsNullOrEmpty(path))
                    paths.Add(path);
            }
            return paths.ToArray();
        }

        void ClearSearch()
        {
            m_SearchFilter.ClearSearch();
            m_SearchFilter.skipHidden = m_SkipHiddenPackages;

            // Clear GUI
            m_SearchFieldText = "";
            m_AssetLabels.DeselectAll();
            m_ObjectTypes.DeselectAll();
            m_DidSelectSearchResult = false;
        }

        void FolderTreeSelectionChanged(bool folderWasSelected)
        {
            if (folderWasSelected)
            {
                SearchViewState state = GetSearchViewState();
                if (state == SearchViewState.AllAssets || state == SearchViewState.InAssetsOnly ||
                    state == SearchViewState.InPackagesOnly)
                {
                    // Clear all except folders if folder is set
                    string[] folders = m_SearchFilter.folders;
                    ClearSearch();
                    m_SearchFilter.folders = folders;

                    // Ensures that when selecting folders we start local asset search next time we search
                    m_SearchFilter.searchArea = m_LastLocalAssetsSearchArea;
                }

                m_LastFolders = m_SearchFilter.folders;
            }

            // End any rename that might be in progress.
            EndRenaming();

            RefreshSearchText();
            InitListArea();
        }

        void IconSizeSlider(Rect r)
        {
            // Slider
            EditorGUI.BeginChangeCheck();
            int newGridSize = (int)GUI.HorizontalSlider(r, m_ListArea.gridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize);
            if (EditorGUI.EndChangeCheck())
            {
                m_ListArea.gridSize = newGridSize;
            }
        }

        void SearchAreaBar()
        {
            // Background
            GUI.Label(m_ListHeaderRect, GUIContent.none, s_Styles.topBarBg);

            const float kMargin = 5f;
            Rect rect = m_ListHeaderRect;
            rect.x += kMargin;
            rect.height -= 1;
            rect.width -= 2 * kMargin;

            GUIStyle style = EditorStyles.boldLabel;

            GUI.Label(rect, s_Styles.m_SearchIn, style);
            if (m_SearchAreaMenuOffset < 0f)
                m_SearchAreaMenuOffset = style.CalcSize(s_Styles.m_SearchIn).x;

            rect.x += m_SearchAreaMenuOffset + 7f;
            rect.width -= m_SearchAreaMenuOffset + 7f;
            rect.width = m_SearchAreaMenu.OnGUI(rect);
        }

        void BreadCrumbBar()
        {
            if (m_ListHeaderRect.height <= 0f)
                return;

            if (m_SearchFilter.folders.Length == 0)
                return;

            Event evt = Event.current;

            // Give keyboard focus to list area if we mouse down in breadcrumbs
            if (evt.type == EventType.MouseDown && m_ListHeaderRect.Contains(evt.mousePosition))
            {
                GUIUtility.keyboardControl = m_ListKeyboardControlID;
                Repaint();
            }

            if (m_BreadCrumbs.Count == 0)
            {
                var path = m_SearchFilter.folders[0];
                if (IsInsideHiddenPackage(path))
                {
                    m_BreadCrumbLastFolderHasSubFolders = false;
                }
                else
                {
                    var folderNames = new List<string>();
                    var folderDisplayNames = new List<string>();
                    var packagesMountPoint = PackageManager.Folders.GetPackagesPath();
                    var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                    if (packageInfo != null)
                    {
                        // Packages root
                        folderNames.Add(packagesMountPoint);
                        folderDisplayNames.Add(packagesMountPoint);

                        // Package name/displayname
                        folderNames.Add(packageInfo.name);
                        folderDisplayNames.Add(string.IsNullOrEmpty(packageInfo.displayName)
                            ? packageInfo.name
                            : packageInfo.displayName);

                        // Rest of the path;
                        if (path != packageInfo.assetPath)
                        {
                            var subpaths = Regex.Replace(path, @"^" + packageInfo.assetPath + "/", "").Split('/');
                            folderNames.AddRange(subpaths);
                            folderDisplayNames.AddRange(subpaths);
                        }
                    }
                    else
                    {
                        folderNames.AddRange(path.Split('/'));
                        folderDisplayNames = folderNames;
                    }

                    var folderPath = "";
                    var i = 0;
                    foreach (var folderName in folderNames)
                    {
                        if (!string.IsNullOrEmpty(folderPath))
                            folderPath += "/";
                        folderPath += folderName;

                        m_BreadCrumbs.Add(new KeyValuePair<GUIContent, string>(new GUIContent(folderDisplayNames[i++]), folderPath));
                    }

                    if (path == packagesMountPoint)
                    {
                        m_BreadCrumbLastFolderHasSubFolders = PackageManagerUtilityInternal.GetAllVisiblePackages(m_SkipHiddenPackages).Length > 0;
                    }
                }
            }

            // Background
            GUI.Label(m_ListHeaderRect, GUIContent.none, s_Styles.topBarBg);

            // Folders
            Rect rect = m_ListHeaderRect;
            rect.y += s_Styles.topBarBg.padding.top;
            rect.x += s_Styles.topBarBg.padding.left;
            if (m_SearchFilter.folders.Length == 1)
            {
                for (int i = 0; i < m_BreadCrumbs.Count; ++i)
                {
                    bool lastElement = i == m_BreadCrumbs.Count - 1;
                    GUIStyle style = lastElement ? EditorStyles.boldLabel : EditorStyles.label; //EditorStyles.miniBoldLabel : EditorStyles.miniLabel;//
                    GUIContent folderContent = m_BreadCrumbs[i].Key;
                    string folderPath = m_BreadCrumbs[i].Value;
                    Vector2 size = style.CalcSize(folderContent);
                    rect.width = size.x;
                    if (GUI.Button(rect, folderContent, style))
                    {
                        ShowFolderContents(GetFolderInstanceID(folderPath), false);
                    }

                    rect.x += size.x;
                    if (!lastElement || m_BreadCrumbLastFolderHasSubFolders)
                    {
                        Rect buttonRect = new Rect(rect.x, rect.y + (rect.height - s_Styles.separator.fixedHeight) / 2, s_Styles.separator.fixedWidth, s_Styles.separator.fixedHeight);
                        if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, s_Styles.separator))
                        {
                            string currentSubFolder = "";
                            if (!lastElement)
                                currentSubFolder = m_BreadCrumbs[i + 1].Value;
                            BreadCrumbListMenu.Show(folderPath, currentSubFolder, buttonRect, this);
                        }
                    }
                    rect.x += s_Styles.separator.fixedWidth;
                }
            }
            else if (m_SearchFilter.folders.Length > 1)
            {
                GUI.Label(rect, GUIContent.Temp("Showing multiple folders..."), EditorStyles.miniLabel);
            }
        }

        void BottomBar()
        {
            if (m_BottomBarRect.height == 0f)
                return;

            Rect rect = m_BottomBarRect;

            // Background
            GUI.Label(rect, GUIContent.none, s_Styles.bottomBarBg);

            // Icons are fixed size in One Column mode, so only show icon size slider in Two Columns mode
            bool showIconSizeSlider = m_ViewMode == ViewMode.TwoColumns || m_SearchFilter.IsSearching();
            if (showIconSizeSlider)
            {
                Rect sliderRect = new Rect(rect.x + rect.width - k_SliderWidth - 16f
                    , rect.y + rect.height - (m_BottomBarRect.height + EditorGUI.kSingleLineHeight) / 2, k_SliderWidth,
                    m_BottomBarRect.height);
                IconSizeSlider(sliderRect);
            }

            // File path
            EditorGUIUtility.SetIconSize(new Vector2(16, 16)); // If not set we see icons scaling down if text is being cropped
            const float k_Margin = 2;
            rect.width -= k_Margin * 2;
            rect.x += k_Margin;
            rect.height = k_BottomBarHeight;

            if (showIconSizeSlider)
            {
                rect.width -= k_SliderWidth + 14f;
            }

            GUI.Label(rect, m_SelectedPathContent, s_Styles.selectedPathLabel);
            EditorGUIUtility.SetIconSize(new Vector2(0, 0));
        }

        public void SelectAssetsFolder()
        {
            ShowFolderContents(AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets"), true);
        }

        string ValidateCreateNewAssetPath(string pathName)
        {
            // Normally we create assets relative to current selected asset. If no asset is selected we normally create the asset in the root folder (Assets)
            // but in two column mode we want to create the asset in the currently shown folder if no asset is selected. (fix case 550484)
            if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing && m_SearchFilter.folders.Length > 0)
            {
                // Ensure pathName is not already a full asset path
                if (!pathName.StartsWith("assets/", StringComparison.CurrentCultureIgnoreCase))
                {
                    // If no assets are selected we use first currently shown folder path
                    if (Selection.GetFiltered(typeof(Object), SelectionMode.Assets).Length == 0)
                    {
                        pathName = Path.Combine(m_SearchFilter.folders[0], pathName);
                        pathName = pathName.Replace("\\", "/");
                    }
                }
            }
            return pathName;
        }

        internal void BeginPreimportedNameEditing(int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile, bool selectAssetBeingCreated)
        {
            if (!Initialized())
                Init();

            // End any rename that might be active, this will also end any asset currently being created
            EndRenaming();

            bool isCreatingNewFolder = endAction is DoCreateFolder;

            if (m_ViewMode == ViewMode.TwoColumns)
            {
                if (m_SearchFilter.GetState() != SearchFilter.State.FolderBrowsing)
                {
                    // Force select the Assets folder as default when no folders are selected
                    SelectAssetsFolder();
                }

                pathName = ValidateCreateNewAssetPath(pathName);

                if (m_ListAreaState.m_CreateAssetUtility.BeginNewAssetCreation(instanceID, endAction, pathName, icon, resourceFile, selectAssetBeingCreated))
                {
                    ShowFolderContents(AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(m_ListAreaState.m_CreateAssetUtility.folder), true);
                    m_ListArea.BeginNamingNewAsset(m_ListAreaState.m_CreateAssetUtility.originalName, instanceID, isCreatingNewFolder);
                }
            }
            else if (m_ViewMode == ViewMode.OneColumn)
            {
                // If search is active put new asset under selected folder otherwise in Assets
                if (m_SearchFilter.IsSearching())
                {
                    ClearSearch();
                }

                // Create in tree
                AssetsTreeViewGUI defaultTreeViewGUI = m_AssetTree.gui as AssetsTreeViewGUI;
                if (defaultTreeViewGUI != null)
                    defaultTreeViewGUI.BeginCreateNewAsset(instanceID, endAction, pathName, icon, resourceFile, selectAssetBeingCreated);
                else
                    Debug.LogError("Not valid defaultTreeViewGUI!");
            }
        }

        public void FrameObject(int instanceID, bool ping)
        {
            m_LockTracker.StopPingIcon();

            bool canFrame = CanFrameAsset(instanceID);
            if (!canFrame)
            {
                // Check if we can frame the main asset from the same asset path instead.
                // This ensures that Components or child GameObject of Prefabs or hidden sub assets will
                // still be located and pinged in the Project Browser (case 1262196).
                var path = AssetDatabase.GetAssetPath(instanceID);
                if (!string.IsNullOrEmpty(path))
                {
                    var mainObject = AssetDatabase.LoadMainAssetAtPath(path);
                    if (mainObject != null)
                    {
                        canFrame = CanFrameAsset(mainObject.GetInstanceID());
                        if (canFrame)
                            instanceID = mainObject.GetInstanceID();
                    }
                }
            }

            bool frame = ping || canFrame;
            if (frame && m_LockTracker.isLocked)
            {
                frame = false;

                // If the item is visible then we can ping it however if it requires revealing then we can not and should indicate why(locked project view).
                if (canFrame &&
                    ((m_ViewMode == ViewMode.TwoColumns && m_ListArea != null && !m_ListArea.IsShowing(instanceID))
                    || (m_ViewMode == ViewMode.OneColumn && m_AssetTree != null && m_AssetTree.data.GetRow(instanceID) == -1)))
                {
                    Repaint();
                    m_LockTracker.PingIcon();
                }
            }

            FrameObjectPrivate(instanceID, frame, ping);
            if (s_LastInteractedProjectBrowser == this)
            {
                m_GrabKeyboardFocusForListArea = true;
            }
        }

        private void FrameObjectPrivate(int instanceID, bool frame, bool ping)
        {
            if (instanceID == 0 || m_ListArea == null)
                return;

            // If framing the same instance as the last one we do not remove the ping
            // since issuing first a ping and then a framing should still show the ping.
            if (m_LastFramedID != instanceID)
                EndPing();
            m_LastFramedID = instanceID;

            ClearSearch();

            if (m_ViewMode == ViewMode.TwoColumns)
            {
                FrameObjectInTwoColumnMode(instanceID, frame, ping);
            }
            else if (m_ViewMode == ViewMode.OneColumn)
            {
                // Clear search to switch back to tree view
                m_AssetTree.Frame(instanceID, frame, ping);
            }
        }

        private void FrameObjectInTwoColumnMode(int instanceID, bool frame, bool ping)
        {
            int folderInstanceID = 0;

            if (instanceID == kPackagesFolderInstanceId)
                folderInstanceID = kPackagesFolderInstanceId;
            else
            {
                string assetPath = AssetDatabase.GetAssetPath(instanceID);
                if (!String.IsNullOrEmpty(assetPath))
                {
                    string containingFolder = ProjectWindowUtil.GetContainingFolder(assetPath);
                    if (!String.IsNullOrEmpty(containingFolder))
                        folderInstanceID = GetFolderInstanceID(containingFolder);

                    if (folderInstanceID == 0)
                        folderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets");
                }
            }

            // Could be a scene gameobject
            if (folderInstanceID != 0)
            {
                m_FolderTree.Frame(folderInstanceID, frame, ping);
                if (frame)
                    ShowFolderContents(folderInstanceID, true);
                m_ListArea.Frame(instanceID, frame, ping);
            }
        }

        // Also called from C++ (used for AssetSelection overriding)
        [UsedByNativeCode]
        internal static int[] GetTreeViewFolderSelection(bool forceUseTreeViewSelection = false)
        {
            // Since we can delete entire folder hierarchies with the returned selection we need to be very careful. We therefore require the following:
            // - The folder/favorite tree view must have keyboard focus
            // Note we cannot require window focus (focusedWindow) since on OSX window focus is lost to popup window when right clicking
            ProjectBrowser ob = s_LastInteractedProjectBrowser;

            if (ob != null && (ob.useTreeViewSelectionInsteadOfMainSelection || forceUseTreeViewSelection) && ob.m_FolderTree != null)
            {
                return s_LastInteractedProjectBrowser.m_FolderTree.GetSelection();
            }

            return k_EmptySelection;
        }

        public float listAreaGridSize
        {
            get { return m_ListArea.gridSize; }
        }

        [UsedByNativeCode]
        internal static bool CanDeleteSelectedAssets()
        {
            var treeViewSelection = GetTreeViewFolderSelection();
            var instanceIDs = treeViewSelection.Length > 0 ? new List<int>(treeViewSelection) : new List<int>(Selection.instanceIDs);

            var objectsToDelete = new HashSet<int>();
            foreach (var instanceID in instanceIDs)
            {
                if (instanceID == AssetDatabase.GetMainAssetOrInProgressProxyInstanceID("Assets") || instanceID == kPackagesFolderInstanceId)
                {
                    return false;
                }

                if (AssetDatabase.IsMainAsset(instanceID))
                {
                    var path = AssetDatabase.GetAssetPath(instanceID);
                    bool isRootFolder, isImmutable;
                    if (string.IsNullOrEmpty(path) || !AssetDatabase.TryGetAssetFolderInfo(path, out isRootFolder, out isImmutable) || isRootFolder || isImmutable)
                    {
                        return false;
                    }

                    objectsToDelete.Add(instanceID);
                }
            }

            return objectsToDelete.Count != 0;
        }

        [UsedByNativeCode]
        internal static void DeleteSelectedAssets(bool askIfSure)
        {
            int[] treeViewSelection = GetTreeViewFolderSelection();

            List<int> instanceIDs;
            if (treeViewSelection.Length > 0)
                instanceIDs = new List<int>(treeViewSelection);
            else
                instanceIDs = new List<int>(Selection.instanceIDs);

            if (instanceIDs.Count == 0)
                return;

            if (ProjectWindowUtil.DeleteAssets(instanceIDs, askIfSure))
            {
                // Ensure selection is cleared since StopAssetEditing() will restore selection from a backup saved in StartAssetEditing.
                Selection.instanceIDs = k_EmptySelection;
            }
        }

        [UsedByNativeCode]
        internal static void RenameSelectedAssets()
        {
            ProjectBrowser ob = s_LastInteractedProjectBrowser;
            if (ob != null)
            {
                if (ob.useTreeViewSelectionInsteadOfMainSelection && ob.m_FolderTree != null)
                {
                    ob.m_FolderTree.BeginNameEditing(0f);
                }
                else if (ob.m_ViewMode == ViewMode.OneColumn && !ob.m_SearchFilter.IsSearching() && ob.m_AssetTree != null)
                {
                    ob.m_AssetTree.BeginNameEditing(0f);
                }
                else if (ob.m_ListArea != null)
                {
                    ob.m_ListArea.BeginRename(0f);
                }
                else
                {
                    return;
                }

                // We need to ensure that we start with focus in the rename overlay (UUM-48858)
                ob.Focus();
                ob.Repaint();
            }
        }

        internal IHierarchyProperty GetHierarchyPropertyUsingFilter(string textFilter)
        {
            FilteredHierarchy filteredHierarchy = new FilteredHierarchy(HierarchyType.Assets);
            filteredHierarchy.searchFilter = SearchFilter.CreateSearchFilterFromString(textFilter);
            IHierarchyProperty property = FilteredHierarchyProperty.CreateHierarchyPropertyForFilter(filteredHierarchy);
            return property;
        }

        internal void ShowObjectsInList(int[] instanceIDs)
        {
            if (!Initialized())
                Init();

            if (m_ViewMode == ViewMode.TwoColumns)
            {
                m_ListArea.ShowObjectsInList(instanceIDs);
                m_FolderTree.SetSelection(k_EmptySelection, false); // Remove selection from folder tree since we show custom list (press F to focus)
            }
            else if (m_ViewMode == ViewMode.OneColumn)
            {
                foreach (int instanceID in Selection.instanceIDs)
                    m_AssetTree.Frame(instanceID, true, false);
            }
        }

        // Called from AssetsMenu
        [RequiredByNativeCode]
        static void ShowSelectedObjectsInLastInteractedProjectBrowser()
        {
            // Only one ProjectBrowser can have focus at a time so if we find one just return that one
            if (s_LastInteractedProjectBrowser != null)
            {
                int[] instanceIDs = Selection.instanceIDs;

                s_LastInteractedProjectBrowser.ShowObjectsInList(instanceIDs);
            }
        }

        // Called from DockArea
        protected virtual void ShowButton(Rect r)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_LockTracker.ShowButton(r, s_Styles.lockButton))
                Repaint();
        }

        internal bool SelectionIsFavorite()
        {
            if (m_FolderTree == null)
                return false;

            if (m_FolderTree.GetSelection().Length != 1)
                return false;

            int selectionID = m_FolderTree.GetSelection()[0];
            ItemType type = GetItemType(selectionID);
            return type == ItemType.SavedFilter;
        }

        private void RevertToLastSelectedFolder(bool folderWasSelected)
        {
            if (m_LastFolders != null && m_LastFolders.Length > 0)
            {
                m_SearchFilter.folders = m_LastFolders;
                if (m_FolderTree != null)
                    SetFolderSelection(GetFolderInstanceIDs(m_LastFolders), true, folderWasSelected);
            }
        }

        internal class SavedFiltersContextMenu
        {
            int m_SavedFilterInstanceID;

            static internal void Show(int savedFilterInstanceID)
            {
                // Curve context menu
                GUIContent delete = EditorGUIUtility.TrTextContent("Delete");

                GenericMenu menu = new GenericMenu();
                menu.AddItem(delete, false, new SavedFiltersContextMenu(savedFilterInstanceID).Delete);

                menu.ShowAsContext();
            }

            private SavedFiltersContextMenu(int savedFilterInstanceID)
            {
                m_SavedFilterInstanceID = savedFilterInstanceID;
            }

            private void Delete()
            {
                DeleteFilter(m_SavedFilterInstanceID);
            }
        }

        internal class BreadCrumbListMenu
        {
            static ProjectBrowser m_Caller;
            string m_SubFolder;

            static internal void Show(string folder, string currentSubFolder, Rect activatorRect, ProjectBrowser caller)
            {
                m_Caller = caller;

                // List of sub folders
                var subFolders = new List<string>();
                var subFolderDisplayNames = new List<string>();
                if (folder == Folders.GetPackagesPath())
                {
                    foreach (var package in PackageManagerUtilityInternal.GetAllVisiblePackages(caller.m_SkipHiddenPackages))
                    {
                        subFolders.Add(package.assetPath);
                        var displayName = !string.IsNullOrEmpty(package.displayName) ? package.displayName : package.name;
                        subFolderDisplayNames.Add(displayName);
                    }
                }
                else
                {
                    subFolders.AddRange(AssetDatabase.GetSubFolders(folder));
                    foreach (var subFolderPath in subFolders)
                        subFolderDisplayNames.Add(Path.GetFileName(subFolderPath));
                }

                var menu = new GenericMenu { allowDuplicateNames = true };
                if (subFolders.Count > 0)
                {
                    var i = 0;
                    foreach (var subFolderPath in subFolders)
                    {
                        menu.AddItem(new GUIContent(subFolderDisplayNames[i++]), subFolderPath == currentSubFolder, new BreadCrumbListMenu(subFolderPath).SelectSubFolder);
                        menu.ShowAsContext();
                    }
                }
                else
                {
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("No sub folders..."));
                }

                menu.DropDown(activatorRect);
            }

            private BreadCrumbListMenu(string subFolder)
            {
                m_SubFolder = subFolder;
            }

            private void SelectSubFolder()
            {
                int folderInstanceID = AssetDatabase.GetMainAssetOrInProgressProxyInstanceID(m_SubFolder);
                if (folderInstanceID != 0)
                    m_Caller.ShowFolderContents(folderInstanceID, false);
            }
        }
    }
}
