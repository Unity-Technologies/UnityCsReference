// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;

namespace UnityEditor
{
    // The title is also used for fetching the project window tab icon: Project.png
    [EditorWindowTitle(title = "Project", icon = "Project")]
    internal class ProjectBrowser : EditorWindow, IHasCustomMenu
    {
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
            SubFolders,
            AssetStore
        }

        // Styles used in the object selector
        class Styles
        {
            public GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public GUIStyle topBarBg = "ProjectBrowserTopBarBg";
            public GUIStyle selectedPathLabel = "Label";
            public GUIStyle exposablePopup = GetStyle("ExposablePopupMenu");
            public GUIStyle lockButton = "IN LockButton";
            public GUIStyle foldout = "AC RightArrow";

            public GUIContent m_FilterByLabel = new GUIContent(EditorGUIUtility.FindTexture("FilterByLabel"), "Search by Label");
            public GUIContent m_FilterByType = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Search by Type");
            public GUIContent m_CreateDropdownContent = new GUIContent("Create");
            public GUIContent m_SaveFilterContent = new GUIContent(EditorGUIUtility.FindTexture("Favorite"), "Save search");
            public GUIContent m_EmptyFolderText = new GUIContent("This folder is empty");
            public GUIContent m_SearchIn = new GUIContent("Search:");

            static GUIStyle GetStyle(string styleName)
            {
                return styleName; // Implicit construction of GUIStyle

                // For fast testing in editor resources grab the style directly from the skin
                //GUISkin skin = EditorGUIUtility.LoadRequired ("Builtin Skins/DarkSkin/Skins/ProjectBrowserSkin.guiSkin") as GUISkin;
                //return skin.GetStyle (styleName);
            }
        }
        static Styles s_Styles;
        static int s_HashForSearchField = "ProjectBrowserSearchField".GetHashCode();

        // Search filter
        [SerializeField]
        SearchFilter m_SearchFilter;

        [System.NonSerialized]
        string m_SearchFieldText = "";

        // Display state
        [SerializeField]
        ViewMode m_ViewMode = ViewMode.TwoColumns;
        [SerializeField]
        int m_StartGridSize = 64;
        [SerializeField]
        string[] m_LastFolders = new string[0];
        [SerializeField]
        float m_LastFoldersGridSize = -1f;
        [SerializeField]
        string m_LastProjectPath;
        [SerializeField]
        bool m_IsLocked;

        bool m_EnableOldAssetTree = true;
        bool m_FocusSearchField;
        string m_SelectedPath;
        List<GUIContent> m_SelectedPathSplitted = new List<GUIContent>();
        float m_LastListWidth;
        bool m_DidSelectSearchResult = false;
        bool m_ItemSelectedByRightClickThisEvent = false;
        bool m_InternalSelectionChange = false; // to know when selection change originated in project view itself
        SearchFilter.SearchArea m_LastLocalAssetsSearchArea = SearchFilter.SearchArea.AllAssets;
        PopupList.InputData m_AssetLabels;
        PopupList.InputData m_ObjectTypes;
        bool m_UseTreeViewSelectionInsteadOfMainSelection;
        bool useTreeViewSelectionInsteadOfMainSelection
        {
            get {return m_UseTreeViewSelectionInsteadOfMainSelection; }
            set {m_UseTreeViewSelectionInsteadOfMainSelection = value; }
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
        ObjectListArea  m_ListArea;
        int m_ListKeyboardControlID;
        bool m_GrabKeyboardFocusForListArea = false;

        // List area header: breadcrumbs or search area menu
        List<KeyValuePair<GUIContent, string>> m_BreadCrumbs = new List<KeyValuePair<GUIContent, string>>();
        bool m_BreadCrumbLastFolderHasSubFolders = false;
        ExposablePopupMenu m_SearchAreaMenu;

        // Layout
        const float     k_MinHeight = 250;
        const float     k_MinWidthOneColumn = 230f;// could be 205 with special handling
        const float     k_MinWidthTwoColumns = 230f;
        float           m_ToolbarHeight;
        const float     k_BottomBarHeight = EditorGUI.kWindowToolbarHeight;
        float           k_MinDirectoriesAreaWidth = 110;
        [SerializeField]
        float           m_DirectoriesAreaWidth = k_MinWidthTwoColumns / 2;
        const float     k_ResizerWidth = 5f;
        const float     k_SliderWidth = 55f;
        [System.NonSerialized]
        float m_SearchAreaMenuOffset = -1f;
        [System.NonSerialized]
        Rect m_ListAreaRect;
        [System.NonSerialized]
        Rect m_TreeViewRect;
        [System.NonSerialized]
        Rect m_BottomBarRect;
        [System.NonSerialized]
        Rect m_ListHeaderRect;
        [System.NonSerialized]
        private int m_LastFramedID = -1;

        // Used by search menu bar
        [System.NonSerialized]
        public GUIContent m_SearchAllAssets = new GUIContent("Assets"); // do not localize this: Assets=folder name
        [System.NonSerialized]
        public GUIContent m_SearchInFolders = new GUIContent(""); // updated when needed
        [System.NonSerialized]
        public GUIContent m_SearchAssetStore = new GUIContent("Asset Store"); // updated when needed

        [System.NonSerialized]
        private string m_lastSearchFilter;
        [System.NonSerialized]
        private double m_NextSearch = double.MaxValue;

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
            EditorApplication.projectWindowChanged += OnProjectChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.assetLabelsChanged += OnAssetLabelsChanged;
            EditorApplication.assetBundleNameChanged += OnAssetBundleNameChanged;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            s_LastInteractedProjectBrowser = this;

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
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.projectWindowChanged -= OnProjectChanged;
            EditorApplication.assetLabelsChanged -= OnAssetLabelsChanged;
            EditorApplication.assetBundleNameChanged -= OnAssetBundleNameChanged;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            s_ProjectBrowsers.Remove(this);
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

        string GetAnalyticsSizeLabel(float size)
        {
            if (size > 600)
                return "Larger than 600 pix";
            if (size < 240)
                return "Less than 240 pix";
            return "240 - 600 pix";
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

        void EnsureValidFolders()
        {
            HashSet<string> validFolders = new HashSet<string>();
            foreach (string folder in m_SearchFilter.folders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    validFolders.Add(folder);
                }
                else
                {
                    // The folder does not exist (could have been deleted) now find first valid parent folder
                    string parentFolder = folder;
                    for (int i = 0; i < 30; ++i)
                    {
                        if (string.IsNullOrEmpty(parentFolder))
                            break;

                        parentFolder = ProjectWindowUtil.GetContainingFolder(parentFolder);
                        if (!string.IsNullOrEmpty(parentFolder) && AssetDatabase.IsValidFolder(parentFolder))
                        {
                            validFolders.Add(parentFolder);
                            break;
                        }
                    }
                }
            }

            m_SearchFilter.folders = validFolders.ToArray();
        }

        private void OnProjectChanged()
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

        public bool Initialized()
        {
            return m_ListArea != null;
        }

        public void Init()
        {
            if (Initialized())
                return;

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
            m_ListArea.assetStoreSearchEnded += AssetStoreSearchEndedCallback;
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
                case SearchViewState.SubFolders:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.SelectedFolders;
                    break;
                case SearchViewState.AssetStore:
                    m_SearchFilter.searchArea = SearchFilter.SearchArea.AssetStore;
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
                case SearchFilter.State.SearchingInAllAssets:   return SearchViewState.AllAssets;
                case SearchFilter.State.SearchingInFolders:     return SearchViewState.SubFolders;
                case SearchFilter.State.SearchingInAssetStore:  return SearchViewState.AssetStore;
            }
            return SearchViewState.NotSearching;
        }

        void SearchButtonClickedCallback(ExposablePopupMenu.ItemData itemClicked)
        {
            if (!itemClicked.m_On) // Behave like radio buttons: a button that is on cannot be turned off
            {
                SetSearchViewState((SearchViewState)itemClicked.m_UserData);

                if (m_SearchFilter.searchArea == SearchFilter.SearchArea.AllAssets || m_SearchFilter.searchArea == SearchFilter.SearchArea.SelectedFolders)
                    m_LastLocalAssetsSearchArea = m_SearchFilter.searchArea;
            }
        }

        void InitSearchMenu()
        {
            SearchViewState state = GetSearchViewState();
            if (state == SearchViewState.NotSearching)
                return;

            List<ExposablePopupMenu.ItemData> buttonData = new List<ExposablePopupMenu.ItemData>();

            GUIStyle onStyle = "ExposablePopupItem";
            GUIStyle offStyle = "ExposablePopupItem";
            bool hasFolderSelected = m_SearchFilter.folders.Length > 0;
            m_SearchAssetStore.text = m_ListArea.GetAssetStoreButtonText();

            bool on = state == SearchViewState.AllAssets;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchAllAssets, on ? onStyle : offStyle, on, true, (int)SearchViewState.AllAssets));
            on = state == SearchViewState.SubFolders;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchInFolders, on ? onStyle : offStyle, on, hasFolderSelected, (int)SearchViewState.SubFolders));
            on = state == SearchViewState.AssetStore;
            buttonData.Add(new ExposablePopupMenu.ItemData(m_SearchAssetStore, on ? onStyle : offStyle, on, true, (int)SearchViewState.AssetStore));

            GUIContent popupButtonContent = m_SearchAllAssets;
            switch (state)
            {
                case SearchViewState.AllAssets:
                    popupButtonContent = m_SearchAllAssets;
                    break;
                case SearchViewState.SubFolders:
                    popupButtonContent = m_SearchInFolders;
                    break;
                case SearchViewState.AssetStore:
                case SearchViewState.NotSearching:
                    popupButtonContent = m_SearchAssetStore;
                    break;
                default:
                    Debug.LogError("Unhandled enum");
                    break;
            }

            ExposablePopupMenu.PopupButtonData popListData = new ExposablePopupMenu.PopupButtonData(popupButtonContent, s_Styles.exposablePopup);
            m_SearchAreaMenu.Init(buttonData, 10f, 450f, popListData, SearchButtonClickedCallback);
        }

        void AssetStoreSearchEndedCallback()
        {
            InitSearchMenu();
        }

        static public void ShowAssetStoreHitsWhileSearchingLocalAssetsChanged()
        {
            foreach (var ob in s_ProjectBrowsers)
            {
                ob.m_ListArea.ShowAssetStoreHitCountWhileSearchingLocalAssetsChanged();
                ob.InitSearchMenu();
            }
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
                    if (i > 0)
                        folderText += ", ";
                    string folderName = Path.GetFileNameWithoutExtension(baseFolders[i]);
                    folderText += "'" + folderName + "'";

                    if (i == 0 && folderName != "Assets") // We dont show tooltip for the root folder: Assets
                        toolTip = baseFolders[i];
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
                    FrameObjectPrivate(Selection.activeInstanceID, !m_IsLocked, false);

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
        }

        void InitViewMode(ViewMode viewMode)
        {
            m_ViewMode = viewMode;

            // Reset
            m_FolderTree = null;
            m_AssetTree = null;

            useTreeViewSelectionInsteadOfMainSelection = false;

            if (m_ViewMode == ViewMode.OneColumn)
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

                string guid = AssetDatabase.AssetPathToGUID("Assets");
                var data = new AssetsTreeViewDataSource(m_AssetTree, AssetDatabase.GetInstanceIDFromGUID(guid), false, false);
                data.foldersFirst = GetShouldShowFoldersFirst();

                m_AssetTree.Init(m_TreeViewRect,
                    data,
                    new AssetsTreeViewGUI(m_AssetTree),
                    new AssetsTreeViewDragging(m_AssetTree)
                    );
                m_AssetTree.ReloadData();
            }
            else if (m_ViewMode == ViewMode.TwoColumns)
            {
                m_FolderTree = new TreeViewController(this, m_FolderTreeState);
                m_FolderTree.deselectOnUnhandledMouseDown = false;
                m_FolderTree.selectionChangedCallback += FolderTreeSelectionCallback;
                m_FolderTree.contextClickItemCallback += FolderTreeViewContextClick;
                m_FolderTree.onGUIRowCallback += OnGUIAssetCallback;
                m_FolderTree.dragEndedCallback += FolderTreeDragEnded;
                m_FolderTree.Init(m_TreeViewRect,
                    new ProjectBrowserColumnOneTreeViewDataSource(m_FolderTree),
                    new ProjectBrowserColumnOneTreeViewGUI(m_FolderTree),
                    new ProjectBrowserColumnOneTreeViewDragging(m_FolderTree)
                    );
                m_FolderTree.ReloadData();
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
                FrameObjectPrivate(Selection.activeInstanceID, !m_IsLocked, false);

            RepaintImmediately();
        }

        void EndRenaming()
        {
            if (m_AssetTree != null)
                m_AssetTree.EndNameEditing(true);

            if (m_FolderTree != null)
                m_FolderTree.EndNameEditing(true);

            if (m_ListArea != null)
                m_ListArea.EndRename(true);
        }

        string[] GetTypesDisplayNames()
        {
            return new[]
            {
                "AnimationClip",
                "AudioClip",
                "AudioMixer",
                "Font",
                "GUISkin",
                "Material",
                "Mesh",
                "Model",
                "PhysicMaterial",
                "Prefab",
                "Scene",
                "Script",
                "Shader",
                "Sprite",
                "Texture",
                "VideoClip",

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
            string[] selectedDisplayNames = (from item in m_ObjectTypes.m_ListElements where item.selected select item.text).ToArray();

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

        void SetupDroplists()
        {
            SetupAssetLabelList();

            // Types
            m_ObjectTypes = new PopupList.InputData();
            m_ObjectTypes.m_CloseOnSelection = false;
            m_ObjectTypes.m_AllowCustom = false;
            m_ObjectTypes.m_OnSelectCallback = TypeListCallback;
            m_ObjectTypes.m_SortAlphabetically = false;
            m_ObjectTypes.m_MaxCount = 0;
            string[] types = GetTypesDisplayNames();
            for (int i = 0; i < types.Length; ++i)
            {
                PopupList.ListElement element = m_ObjectTypes.NewOrMatchingElement(types[i]);
                if (i == 0)
                    element.selected = true;
            }
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

        static int GetParentInstanceID(int objectInstanceID)
        {
            string propertyPath = AssetDatabase.GetAssetPath(objectInstanceID);
            int pos = propertyPath.LastIndexOf("/");
            if (pos >= 0)
            {
                string folderPath = propertyPath.Substring(0, pos);
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(folderPath, typeof(UnityEngine.Object));
                if (obj != null)
                    return obj.GetInstanceID();
            }
            else
            {
                Debug.LogError("Invalid path: " + propertyPath);
            }
            return -1;
        }

        bool IsShowingFolder(int folderInstanceID)
        {
            string folderPath = AssetDatabase.GetAssetPath(folderInstanceID);
            bool contains = new List<string>(m_SearchFilter.folders).Contains(folderPath);
            return contains;
        }

        void ShowFolderContents(int folderInstanceID, bool revealAndFrameInFolderTree)
        {
            if (m_ViewMode != ViewMode.TwoColumns)
                Debug.LogError("ShowFolderContents should only be called in two column mode");

            if (folderInstanceID == 0)
                return;

            string folderPath = AssetDatabase.GetAssetPath(folderInstanceID);
            m_SearchFilter.ClearSearch();
            m_SearchFilter.folders = new[] {folderPath};
            m_FolderTree.SetSelection(new[] {folderInstanceID}, revealAndFrameInFolderTree);
            FolderTreeSelectionChanged(true);
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
                    if (item != null && item.parent != null && item.id != ProjectBrowserColumnOneTreeViewDataSource.GetAssetsFolderInstanceID())
                    {
                        SetFolderSelection(new[] {item.parent.id}, true);
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
                    if (m_ViewMode == ViewMode.TwoColumns)
                    {
                        SetFolderSelection(selectedInstanceIDs, false);
                    }
                    else if (m_ViewMode == ViewMode.OneColumn)
                    {
                        ClearSearch(); // shows tree instead of search
                        m_AssetTree.Frame(selectedInstanceIDs[0], true, false);
                    }

                    Repaint();
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
                m_SelectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            else
                m_SelectedPath = "";

            // By clearing we auto refresh it when needed (in an OnGUI code path because we need Styles)
            m_SelectedPathSplitted.Clear();
        }

        // Also called from list when navigating by keys
        void ListAreaItemSelectedCallback(bool doubleClicked)
        {
            SetAsLastInteractedProjectBrowser();

            Selection.activeObject = null;
            int[] instanceIDs = m_ListArea.GetSelection();
            if (instanceIDs.Length > 0)
            {
                Selection.instanceIDs = instanceIDs;
                m_SearchFilter.searchArea = m_LastLocalAssetsSearchArea; // local asset was selected
                m_InternalSelectionChange = true;
            }
            else if (AssetStoreAssetSelection.Count > 0)
                Selection.activeObject = AssetStoreAssetInspector.Instance;

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
            // Added because this window uses RenameOverlay
            EndRenaming();
        }

        bool ShouldFrameAsset(int instanceID)
        {
            HierarchyProperty h = new HierarchyProperty(HierarchyType.Assets);
            return h.Find(instanceID, null);
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
                bool revealSelectionAndFrameLast = !m_IsLocked && ShouldFrameAsset(instanceID);
                m_AssetTree.SetSelection(Selection.instanceIDs, revealSelectionAndFrameLast);
            }
            else if (m_ViewMode == ViewMode.TwoColumns)
            {
                if (!m_InternalSelectionChange)
                {
                    bool frame = !m_IsLocked && Selection.instanceIDs.Length > 0 && ShouldFrameAsset(instanceID);
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

            // Clear asset store asset selection
            if (Selection.activeObject != null && Selection.activeObject.GetType() != typeof(AssetStoreAssetInspector))
            {
                m_ListArea.selectedAssetStoreAsset = false;
                AssetStoreAssetSelection.Clear();
            }

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
            m_FolderTree.SetSelection(selectedInstanceIDs, revealSelectionAndFrameLastSelected);
            SetFoldersInSearchFilter(selectedInstanceIDs);
            FolderTreeSelectionChanged(true);
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

            Selection.activeObject = null;
            if (selectedTreeViewInstanceIDs.Length > 0)
                Selection.instanceIDs = selectedTreeViewInstanceIDs;

            RefreshSelectedPath();
            SetSearchFoldersFromCurrentSelection();
            RefreshSearchText();
        }

        void SetSearchFoldersFromCurrentSelection()
        {
            HashSet<string> folders = new HashSet<string>();

            foreach (int instanceID in Selection.instanceIDs)
            {
                if (!AssetDatabase.Contains(instanceID))
                    continue;

                string path = AssetDatabase.GetAssetPath(instanceID);
                if (AssetDatabase.IsValidFolder(path))
                {
                    folders.Add(path);
                }
                else
                {
                    // Add containing folder of the selected asset
                    string folderPath = ProjectWindowUtil.GetContainingFolder(path);
                    if (!string.IsNullOrEmpty(folderPath))
                        folders.Add(folderPath);
                }
            }

            // Set them as folders in search filter (so search in folder works correctly)
            m_SearchFilter.folders = ProjectWindowUtil.GetBaseFolders(folders.ToArray());

            // Keep for debugging
            // Debug.Log ("Search folders: " + DebugUtils.ListToString(new List<string>(m_SearchFilter.folders)));
        }

        void SetSearchFolderFromFolderTreeSelection()
        {
            if (m_FolderTree != null)
                m_SearchFilter.folders = GetFolderPathsFromInstanceIDs(m_FolderTree.GetSelection());
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
                        float previewSize = SavedSearchFilters.GetPreviewSize(firstTreeViewInstanceID);
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
                    int instanceID = AssetDatabase.GetMainAssetInstanceID(folderPath);
                    if (instanceID == 0)
                    {
                        if (EditorUtility.DisplayDialog("Folder not found", "The folder '" + folderPath + "' might have been deleted or belong to another project. Do you want to delete the favorite?", "Delete", "Cancel"))
                            SavedSearchFilters.RemoveSavedFilter(savedFilterID);

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
                    case SearchViewState.AssetStore:
                    {
                        if (!isSavedFilterSelected)
                            m_FolderTree.SetSelection(new int[0], false);
                    }
                    break;

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

            var hierarchyType = HierarchyType.Assets;
            if (m_SearchFilter.folders.Any(AssetDatabase.IsPackagedAssetPath))
                hierarchyType = HierarchyType.Packages;
            m_ListArea.Init(m_ListAreaRect, hierarchyType, m_SearchFilter, false);
            m_ListArea.InitSelection(Selection.instanceIDs);
        }

        void OnInspectorUpdate()
        {
            if (m_ListArea != null)
                m_ListArea.OnInspectorUpdate();


            // if it's time for a search we do it
            if (EditorApplication.timeSinceStartup > m_NextSearch)
            {
                //Perform the Search
                m_NextSearch = double.MaxValue;
                m_SearchFilter.SearchFieldStringToFilter(m_SearchFieldText);
                SyncFilterGUI();
                TopBarSearchSettingsChanged();
                Repaint();
            }
        }

        void OnDestroy()
        {
            if (m_ListArea != null)
                m_ListArea.OnDestroy();

            if (this == s_LastInteractedProjectBrowser)
                s_LastInteractedProjectBrowser = null;
        }

        // Returns list of duplicated instanceIDs
        static internal int[] DuplicateFolders(int[] instanceIDs)
        {
            AssetDatabase.Refresh();

            List<string> copiedPaths = new List<string>();
            bool failed = false;
            int asssetsFolderInstanceID = ProjectBrowserColumnOneTreeViewDataSource.GetAssetsFolderInstanceID();

            foreach (int instanceID in instanceIDs)
            {
                if (instanceID == asssetsFolderInstanceID)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(InternalEditorUtility.GetObjectFromInstanceID(instanceID));
                string newPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Copy
                if (newPath.Length != 0)
                    failed |= !AssetDatabase.CopyAsset(assetPath, newPath);
                else
                    failed |= true;

                if (!failed)
                {
                    copiedPaths.Add(newPath);
                }
            }

            AssetDatabase.Refresh();

            int[] copiedAssets = new int[copiedPaths.Count];
            for (int i = 0; i < copiedPaths.Count; i++)
            {
                copiedAssets[i] = AssetDatabase.LoadMainAssetAtPath(copiedPaths[i]).GetInstanceID();
            }

            return copiedAssets;
        }

        static void DeleteFilter(int filterInstanceID)
        {
            if (SavedSearchFilters.GetRootInstanceID() == filterInstanceID)
            {
                string title = "Cannot Delete";
                EditorUtility.DisplayDialog(title, "Deleting the 'Filters' root is not allowed", "Ok");
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

        // Returns true if we should early out of OnGUI
        bool HandleCommandEventsForTreeView()
        {
            // Handle all event for tree view
            EventType eventType = Event.current.type;
            if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand)
            {
                bool execute = eventType == EventType.ExecuteCommand;

                int[] instanceIDs = m_FolderTree.GetSelection();
                if (instanceIDs.Length == 0)
                    return false;

                // Only one type can be selected at a time (and savedfilters can only be single-selected)
                ItemType itemType = GetItemType(instanceIDs[0]);

                if (Event.current.commandName == "Delete" || Event.current.commandName == "SoftDelete")
                {
                    Event.current.Use();
                    if (execute)
                    {
                        if (itemType == ItemType.SavedFilter)
                        {
                            System.Diagnostics.Debug.Assert(instanceIDs.Length == 1); //We do not support multiselection for filters
                            DeleteFilter(instanceIDs[0]);
                            EditorGUIUtility.ExitGUI(); // exit gui since we are iterating items we just reloaded
                        }
                        else if (itemType == ItemType.Asset)
                        {
                            bool askIfSure = Event.current.commandName == "SoftDelete";
                            DeleteSelectedAssets(askIfSure);
                            if (askIfSure)
                                Focus(); // Workaround that we do not get focus back when dialog is closed
                        }
                    }
                    GUIUtility.ExitGUI();
                }
                else if (Event.current.commandName == "Duplicate")
                {
                    if (execute)
                    {
                        if (itemType == ItemType.SavedFilter)
                        {
                            // TODO copy filter (get new name as assets)
                        }
                        else if (itemType == ItemType.Asset)
                        {
                            Event.current.Use();
                            int[] copiedFolders = DuplicateFolders(instanceIDs);
                            SetFolderSelection(copiedFolders, true);
                            GUIUtility.ExitGUI();
                        }
                    }
                    else
                    {
                        Event.current.Use();
                    }
                }
            }

            return false;
        }

        // Returns true if we should early out of OnGUI
        bool HandleCommandEvents()
        {
            EventType eventType = Event.current.type;
            if (eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand)
            {
                bool execute = eventType == EventType.ExecuteCommand;

                if (Event.current.commandName == "Delete" || Event.current.commandName == "SoftDelete")
                {
                    Event.current.Use();
                    if (execute)
                    {
                        bool askIfSure = Event.current.commandName == "SoftDelete";
                        DeleteSelectedAssets(askIfSure);
                        if (askIfSure)
                            Focus(); // Workaround that we do not get focus back when dialog is closed
                    }
                    GUIUtility.ExitGUI();
                }
                else if (Event.current.commandName == "Duplicate")
                {
                    if (execute)
                    {
                        Event.current.Use();
                        ProjectWindowUtil.DuplicateSelectedAssets();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
                        if (selectedAssets.Length != 0)
                            Event.current.Use();
                    }
                }
                else if (Event.current.commandName == "FocusProjectWindow")
                {
                    if (execute)
                    {
                        FrameObjectPrivate(Selection.activeInstanceID, true, false);
                        Event.current.Use();
                        Focus();
                        GUIUtility.ExitGUI();
                    }
                    else
                    {
                        Event.current.Use();
                    }
                }
                else if (Event.current.commandName == "SelectAll")
                {
                    if (execute)
                        SelectAll();
                    Event.current.Use();
                }
                // Frame selected assets
                else if (Event.current.commandName == "FrameSelected")
                {
                    if (execute)
                    {
                        FrameObjectPrivate(Selection.activeInstanceID, true, false);
                        Event.current.Use();
                        GUIUtility.ExitGUI();
                    }
                    Event.current.Use();
                }
                else if (Event.current.commandName == "Find")
                {
                    if (execute)
                        m_FocusSearchField = true;
                    Event.current.Use();
                }
            }
            return false;
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

        void RefreshSplittedSelectedPath()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            m_SelectedPathSplitted.Clear();

            if (string.IsNullOrEmpty(m_SelectedPath))
            {
                m_SelectedPathSplitted.Add(new GUIContent());
            }
            else
            {
                string displayPath = m_SelectedPath;
                if (m_SelectedPath.StartsWith("assets/", System.StringComparison.CurrentCultureIgnoreCase))
                    displayPath = m_SelectedPath.Substring("assets/".Length);

                if (m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing)
                {
                    m_SelectedPathSplitted.Add(new GUIContent(System.IO.Path.GetFileName(m_SelectedPath), AssetDatabase.GetCachedIcon(m_SelectedPath)));
                }
                else
                {
                    float availableWidth = position.width - m_DirectoriesAreaWidth - k_SliderWidth - 16f;
                    Vector2 stringSize = s_Styles.selectedPathLabel.CalcSize(GUIContent.Temp(displayPath));
                    if (stringSize.x + 25f > availableWidth)
                    {
                        /*
                        // Full path to subassets
                        IHierarchyProperty activeSelectedInHierachy = new HierarchyProperty (HierarchyType.Assets);
                        activeSelectedInHierachy.Find (m_ListAreaState.m_LastClickedInstanceID, null);
                        do
                        {
                            m_SelectedPathSplitted.Add (new GUIContent (activeSelectedInHierachy.name, activeSelectedInHierachy.icon));
                        }
                        while (activeSelectedInHierachy.Parent());
                        m_SelectedPathSplitted.Reverse ();
                         */

                        string[] split = displayPath.Split('/');
                        string curPath = "Assets/";
                        for (int i = 0; i < split.Length; ++i)
                        {
                            curPath += split[i];
                            Texture icon = AssetDatabase.GetCachedIcon(curPath);

                            m_SelectedPathSplitted.Add(new GUIContent(split[i], icon));
                            curPath += "/";
                        }
                    }
                    else
                    {
                        m_SelectedPathSplitted.Add(new GUIContent(displayPath, AssetDatabase.GetCachedIcon(m_SelectedPath)));
                    }
                }
            }
        }

        float GetBottomBarHeight()
        {
            if (m_SelectedPathSplitted.Count == 0)
                RefreshSplittedSelectedPath();

            // Only show bottom bar in one column mode when searching
            if (m_ViewMode == ViewMode.OneColumn && !m_SearchFilter.IsSearching())
                return 0f;

            return k_BottomBarHeight * m_SelectedPathSplitted.Count;
        }

        float GetListHeaderHeight()
        {
            return m_SearchFilter.GetState() == SearchFilter.State.EmptySearchFilter ? 0f : EditorGUI.kWindowToolbarHeight + 1;
        }

        void CalculateRects()
        {
            float bottomBarHeight = GetBottomBarHeight();
            float listHeaderHeight = GetListHeaderHeight();
            if (m_ViewMode == ViewMode.OneColumn)
            {
                m_ListAreaRect = new Rect(0, m_ToolbarHeight + listHeaderHeight, position.width, position.height - m_ToolbarHeight - listHeaderHeight - bottomBarHeight);
                m_TreeViewRect = new Rect(0, m_ToolbarHeight, position.width, position.height - m_ToolbarHeight - bottomBarHeight);
                m_BottomBarRect = new Rect(0, position.height - bottomBarHeight, position.width, bottomBarHeight);
                m_ListHeaderRect = new Rect(0, m_ToolbarHeight, position.width, listHeaderHeight);
            }
            else //if (m_ViewMode == ViewMode.TwoColumns)
            {
                float listWidth = position.width - m_DirectoriesAreaWidth;

                m_ListAreaRect = new Rect(m_DirectoriesAreaWidth, m_ToolbarHeight + listHeaderHeight, listWidth, position.height - m_ToolbarHeight - listHeaderHeight - bottomBarHeight);
                m_TreeViewRect = new Rect(0, m_ToolbarHeight, m_DirectoriesAreaWidth, position.height - m_ToolbarHeight);
                m_BottomBarRect = new Rect(m_DirectoriesAreaWidth, position.height - bottomBarHeight, listWidth, bottomBarHeight);
                m_ListHeaderRect = new Rect(m_ListAreaRect.x, m_ToolbarHeight, m_ListAreaRect.width, listHeaderHeight);
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

            m_ToolbarHeight = EditorGUI.kWindowToolbarHeight + 1;
            m_ItemSelectedByRightClickThisEvent = false;

            // Size splitterRects for different areas of the browser
            ResizeHandling(position.width, position.height - m_ToolbarHeight);
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

                // Vertical splitter line between folders and content (drawn before listarea so listarea ping is drawn on top of line)
                EditorGUIUtility.DrawHorizontalSplitter(new Rect(m_ListAreaRect.x, m_ToolbarHeight, 1, m_TreeViewRect.height));

                // List Content
                m_ListArea.OnGUI(m_ListAreaRect, m_ListKeyboardControlID);

                if (m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing  && m_ListArea.numItemsDisplayed == 0)
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
                    if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing  && evt.button == 1 && !m_ItemSelectedByRightClickThisEvent)
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
                        // Context click in list area (can be an asset store asset or a local asset)
                        if (AssetStoreAssetSelection.GetFirstAsset() != null)
                            AssetStoreItemContextMenu.Show();
                        else
                            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);

                        evt.Use();
                    }
                    break;
            }
        }

        void AssetTreeViewContextClick(int clickedItemID)
        {
            Event evt = Event.current;

            // Context click with a selected Asset
            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);

            evt.Use();
        }

        void AssetTreeViewContextClickOutsideItems()
        {
            Event evt = Event.current;

            // Deselect all
            if (m_AssetTree.GetSelection().Length > 0)
            {
                int[] newSelection = new int[0];
                m_AssetTree.SetSelection(newSelection, false);
                AssetTreeSelectionCallback(newSelection);
            }

            // Context click with no selected assets
            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/", null);

            evt.Use();
        }

        void FolderTreeViewContextClick(int clickedItemID)
        {
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
            GUILayout.BeginArea(new Rect(0, 0, position.width, m_ToolbarHeight));

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
                GUILayout.Space(spaceBetween);
                TypeDropDown();
                AssetLabelsDropDown();
                if (m_ViewMode == ViewMode.TwoColumns)
                {
                    ButtonSaveFilter();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        void SetOneColumn()
        {
            SetViewMode(ViewMode.OneColumn);
        }

        void SetTwoColumns()
        {
            SetViewMode(ViewMode.TwoColumns);
        }

        internal bool IsTwoColumns()
        {
            return m_ViewMode == ViewMode.TwoColumns;
        }

        void OpenTreeViewTestWindow()
        {
            GetWindow<TreeViewExamples.TreeViewTestWindow>();
        }

        void ToggleExpansionAnimationPreference()
        {
            bool oldValue = EditorPrefs.GetBool(TreeViewController.kExpansionAnimationPrefKey, false);
            EditorPrefs.SetBool(TreeViewController.kExpansionAnimationPrefKey, !oldValue);
            InternalEditorUtility.RequestScriptReload();
        }

        void ToggleShowPackagesInAssetsFolder()
        {
            bool showPackagesInAssetsFolder = EditorPrefs.GetBool("ShowPackagesFolder", false);
            EditorPrefs.SetBool("ShowPackagesFolder", !showPackagesInAssetsFolder);
            EditorApplication.projectWindowChanged();
        }


        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (m_EnableOldAssetTree)
            {
                GUIContent assetTreeText =      new GUIContent("One Column Layout");
                GUIContent assetBrowserText =   new GUIContent("Two Column Layout");

                menu.AddItem(assetTreeText, m_ViewMode == ViewMode.OneColumn, SetOneColumn);
                if (position.width >= k_MinWidthTwoColumns)
                    menu.AddItem(assetBrowserText, m_ViewMode == ViewMode.TwoColumns, SetTwoColumns);
                else
                    menu.AddDisabledItem(assetBrowserText);

                if (Unsupported.IsDeveloperBuild())
                {
                    menu.AddItem(new GUIContent("DEVELOPER/Show Packages in Project Window"), EditorPrefs.GetBool("ShowPackagesFolder", false), ToggleShowPackagesInAssetsFolder);
                    menu.AddItem(new GUIContent("DEVELOPER/Open TreeView Test Window..."), false, OpenTreeViewTestWindow);
                    menu.AddItem(new GUIContent("DEVELOPER/Use TreeView Expansion Animation"), EditorPrefs.GetBool(TreeViewController.kExpansionAnimationPrefKey, false), ToggleExpansionAnimationPreference);
                }
            }
        }

        float DrawLocalAssetHeader(Rect r)
        {
            return 0;
        }

        void ResizeHandling(float width, float height)
        {
            if (m_ViewMode == ViewMode.OneColumn)
                return;

            // Handle folders vs. items splitter
            Rect dragRect = new Rect(m_DirectoriesAreaWidth, m_ToolbarHeight, k_ResizerWidth, height);
            dragRect = EditorGUIUtility.HandleHorizontalSplitter(dragRect, position.width, k_MinDirectoriesAreaWidth, k_MinWidthTwoColumns - k_MinDirectoriesAreaWidth);
            m_DirectoriesAreaWidth = dragRect.x;

            // Refresh selected path indicator for new width
            float listWidth = position.width - m_DirectoriesAreaWidth;
            if (listWidth != m_LastListWidth)
                RefreshSplittedSelectedPath();
            m_LastListWidth = listWidth;
        }

        void ButtonSaveFilter()
        {
            // Only show when we have a active filter
            using (new EditorGUI.DisabledScope(!m_SearchFilter.IsSearching()))
            {
                if (GUILayout.Button(s_Styles.m_SaveFilterContent, EditorStyles.toolbarButton))
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
                            bool isRootFilter = SavedSearchFilters.GetRootInstanceID() ==  instanceID;

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
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_CreateDropdownContent, EditorStyles.toolbarDropDown);
            if (EditorGUI.DropdownButton(r, s_Styles.m_CreateDropdownContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                GUIUtility.hotControl = 0;
                EditorUtility.DisplayPopupMenu(r, "Assets/Create", null);
            }
        }

        void AssetLabelsDropDown()
        {
            // Labels button
            Rect r = GUILayoutUtility.GetRect(s_Styles.m_FilterByLabel, EditorStyles.toolbarButton);
            if (EditorGUI.DropdownButton(r, s_Styles.m_FilterByLabel, FocusType.Passive, EditorStyles.toolbarButton))
            {
                PopupWindow.Show(r, new PopupList(m_AssetLabels), null, ShowMode.PopupMenuWithKeyboardFocus);
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

        void SearchField()
        {
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.MinWidth(65), GUILayout.MaxWidth(300));
            int searchFieldControlID = EditorGUIUtility.GetControlID(s_HashForSearchField, FocusType.Passive, rect); // We use 'Passive' to ensure we only tab between folder tree and list area. Focus search field by using Ctrl+F.

            if (m_FocusSearchField)
            {
                GUIUtility.keyboardControl = searchFieldControlID;
                EditorGUIUtility.editingTextField = true;
                if (Event.current.type == EventType.Repaint)
                    m_FocusSearchField = false;
            }

            // On arrow down/up swicth to control selection in list area
            Event evt = Event.current;
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow))
            {
                if (GUIUtility.keyboardControl == searchFieldControlID)
                {
                    if (!m_ListArea.IsLastClickedItemVisible())
                        m_ListArea.SelectFirst();

                    GUIUtility.keyboardControl = m_ListKeyboardControlID;
                    evt.Use();
                }
            }

            m_lastSearchFilter = EditorGUI.ToolbarSearchField(searchFieldControlID, rect, m_SearchFieldText, false);

            if (m_lastSearchFilter != m_SearchFieldText || m_FocusSearchField)
            {
                // Update filter with string
                m_SearchFieldText = m_lastSearchFilter;

                m_NextSearch = EditorApplication.timeSinceStartup + SearchableEditorWindow.k_SearchTimerDelaySecs;
            }
        }

        void TopBarSearchSettingsChanged()
        {
            if (!m_SearchFilter.IsSearching())
            {
                if (m_DidSelectSearchResult)
                {
                    m_DidSelectSearchResult = false;
                    FrameObjectPrivate(Selection.activeInstanceID, true, false);
                    if (GUIUtility.keyboardControl == 0)
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
                    if (GUIUtility.keyboardControl == 0 && m_LastFolders != null && m_LastFolders.Length > 0)
                    {
                        m_SearchFilter.folders = m_LastFolders;
                        SetFolderSelection(GetFolderInstanceIDs(m_LastFolders), true);
                    }
                }
            }
            else
            {
                InitSearchMenu();
            }

            InitListArea();
        }

        static int[] GetFolderInstanceIDs(string[] folders)
        {
            int[] folderInstanceIDs = new int[folders.Length];
            for (int i = 0; i < folders.Length; ++i)
                folderInstanceIDs[i] = AssetDatabase.GetMainAssetInstanceID(folders[i]);
            return folderInstanceIDs;
        }

        static string[] GetFolderPathsFromInstanceIDs(int[] instanceIDs)
        {
            List<string> paths = new List<string>();
            foreach (int instanceID in instanceIDs)
            {
                string path = AssetDatabase.GetAssetPath(instanceID);
                if (!string.IsNullOrEmpty(path))
                    paths.Add(path);
            }
            return paths.ToArray();
        }

        void ClearSearch()
        {
            m_SearchFilter.ClearSearch();

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
                if (state == SearchViewState.AllAssets || state == SearchViewState.AssetStore)
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
                AssetStorePreviewManager.AbortSize(m_ListArea.gridSize);
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
            rect.width -= 2 * kMargin;
            rect.y += 1;

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
                string path = m_SearchFilter.folders[0];

                string[] folderNames = path.Split('/');
                var packagesRoot = AssetDatabase.GetPackagesMountPoint();
                if (path.StartsWith(packagesRoot))
                {
                    // Translate the packages root mount point
                    folderNames = System.Text.RegularExpressions.Regex.Replace(path, "^" + packagesRoot, AssetDatabase.GetPackagesMountPoint()).Split('/');
                }

                string folderPath = "";

                foreach (string folderName in folderNames)
                {
                    if (!string.IsNullOrEmpty(folderPath))
                        folderPath += "/";
                    folderPath += folderName;

                    m_BreadCrumbs.Add(new KeyValuePair<GUIContent, string>(new GUIContent(folderName), folderPath));
                }

                m_BreadCrumbLastFolderHasSubFolders = AssetDatabase.GetSubFolders(path).Length > 0;
            }

            // Background
            GUI.Label(m_ListHeaderRect, GUIContent.none, s_Styles.topBarBg);

            // Folders
            Rect rect = m_ListHeaderRect;
            rect.y += 1;
            rect.x += 4;
            if (m_SearchFilter.folders.Length == 1)
            {
                for (int i = 0; i < m_BreadCrumbs.Count; ++i)
                {
                    bool lastElement = i == m_BreadCrumbs.Count - 1;
                    GUIStyle style = lastElement ? EditorStyles.boldLabel : EditorStyles.label; //EditorStyles.miniBoldLabel : EditorStyles.miniLabel;//
                    GUIContent folderContent = m_BreadCrumbs[i].Key;
                    string folderPath =  m_BreadCrumbs[i].Value;
                    Vector2 size = style.CalcSize(folderContent);
                    rect.width = size.x;
                    if (GUI.Button(rect, folderContent, style))
                    {
                        ShowFolderContents(AssetDatabase.GetMainAssetInstanceID(folderPath), false);
                    }

                    rect.x += size.x + 3f;
                    if (!lastElement || m_BreadCrumbLastFolderHasSubFolders)
                    {
                        Rect buttonRect = new Rect(rect.x, rect.y + 2, 13, 13);
                        if (EditorGUI.DropdownButton(buttonRect, GUIContent.none, FocusType.Passive, s_Styles.foldout))
                        {
                            string currentSubFolder = "";
                            if (!lastElement)
                                currentSubFolder = m_BreadCrumbs[i + 1].Value;
                            BreadCrumbListMenu.Show(folderPath, currentSubFolder, buttonRect, this);
                        }
                    }
                    rect.x += 11f;
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

            Rect sliderRect = new Rect(rect.x + rect.width - k_SliderWidth - 16f, rect.y + rect.height - 17f, k_SliderWidth, 17f);
            IconSizeSlider(sliderRect);

            // File path
            EditorGUIUtility.SetIconSize(new Vector2(16, 16)); // If not set we see icons scaling down if text is being cropped
            const float k_Margin = 2;
            rect.width -= k_Margin * 2;
            rect.x += k_Margin;
            rect.height = k_BottomBarHeight;
            for (int i = m_SelectedPathSplitted.Count - 1; i >= 0; --i)
            {
                if (i == 0)
                    rect.width = rect.width - k_SliderWidth - 14f;
                GUI.Label(rect, m_SelectedPathSplitted[i], s_Styles.selectedPathLabel);
                rect.y += k_BottomBarHeight;
            }
            EditorGUIUtility.SetIconSize(new Vector2(0, 0));
        }

        void SelectAssetsFolder()
        {
            ShowFolderContents(ProjectBrowserColumnOneTreeViewDataSource.GetAssetsFolderInstanceID(), true);
        }

        string ValidateCreateNewAssetPath(string pathName)
        {
            // Normally we create assets relative to current selected asset. If no asset is selected we normally create the asset in the root folder (Assets)
            // but in two column mode we want to create the asset in the currently shown folder if no asset is selected. (fix case 550484)
            if (m_ViewMode == ViewMode.TwoColumns && m_SearchFilter.GetState() == SearchFilter.State.FolderBrowsing && m_SearchFilter.folders.Length > 0)
            {
                // Ensure pathName is not already a full asset path
                if (!pathName.StartsWith("assets/", System.StringComparison.CurrentCultureIgnoreCase))
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

        internal void BeginPreimportedNameEditing(int instanceID, EndNameEditAction endAction, string pathName, Texture2D icon, string resourceFile)
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

                if (m_ListAreaState.m_CreateAssetUtility.BeginNewAssetCreation(instanceID, endAction, pathName, icon, resourceFile))
                {
                    ShowFolderContents(AssetDatabase.GetMainAssetInstanceID(m_ListAreaState.m_CreateAssetUtility.folder), true);
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
                    defaultTreeViewGUI.BeginCreateNewAsset(instanceID, endAction, pathName, icon, resourceFile);
                else
                    Debug.LogError("Not valid defaultTreeViewGUI!");
            }
        }

        public void FrameObject(int instanceID, bool ping)
        {
            bool frame = !m_IsLocked && (ping || ShouldFrameAsset(instanceID));
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

            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string containingFolder = ProjectWindowUtil.GetContainingFolder(assetPath);
                if (!string.IsNullOrEmpty(containingFolder))
                    folderInstanceID = AssetDatabase.GetMainAssetInstanceID(containingFolder);

                if (folderInstanceID == 0)
                    folderInstanceID = ProjectBrowserColumnOneTreeViewDataSource.GetAssetsFolderInstanceID();
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
        static int[] GetTreeViewFolderSelection()
        {
            // Since we can delete entire folder hierarchies with the returned selection we need to be very careful. We therefore require the following:
            // - The folder/favorite tree view must have keyboard focus
            // Note we cannot require window focus (focusedWindow) since on OSX window focus is lost to popup window when right clicking
            ProjectBrowser ob = s_LastInteractedProjectBrowser;

            if (ob != null && ob.useTreeViewSelectionInsteadOfMainSelection && ob.m_FolderTree != null)
            {
                return s_LastInteractedProjectBrowser.m_FolderTree.GetSelection();
            }

            return new int[0];
        }

        public float listAreaGridSize
        {
            get { return m_ListArea.gridSize; }
        }

        int GetProjectBrowserDebugID()
        {
            for (int i = 0; i < s_ProjectBrowsers.Count; ++i)
                if (s_ProjectBrowsers[i] == this)
                    return i;
            return -1;
        }

        // FIXME: The validation logic is duplicated on the C++ side in CanDeleteSelectedAssets
        // Keep these in sync for now
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

            ProjectWindowUtil.DeleteAssets(instanceIDs, askIfSure);

            // Ensure selection is cleared since StopAssetEditing() will restore selection from a backup saved in StartAssetEditing.
            Selection.instanceIDs = new int[0];
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
                m_FolderTree.SetSelection(new int[0], false); // Remove selection from folder tree since we show custom list (press F to focus)
            }
            else if (m_ViewMode == ViewMode.OneColumn)
            {
                foreach (int instanceID in Selection.instanceIDs)
                    m_AssetTree.Frame(instanceID, true, false);
            }
        }

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

            m_IsLocked = GUI.Toggle(r, m_IsLocked, GUIContent.none, s_Styles.lockButton);
        }

        internal class SavedFiltersContextMenu
        {
            int m_SavedFilterInstanceID;

            static internal void Show(int savedFilterInstanceID)
            {
                // Curve context menu
                GUIContent delete = new GUIContent("Delete");

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
                string[] subFolders = AssetDatabase.GetSubFolders(folder);
                GenericMenu menu = new GenericMenu();
                if (subFolders.Length >= 0)
                {
                    currentSubFolder = System.IO.Path.GetFileName(currentSubFolder);
                    foreach (string subFolderPath in subFolders)
                    {
                        string subFolderName = System.IO.Path.GetFileName(subFolderPath);
                        menu.AddItem(new GUIContent(subFolderName), subFolderName == currentSubFolder, new BreadCrumbListMenu(subFolderPath).SelectSubFolder);
                        menu.ShowAsContext();
                    }
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("No sub folders..."));
                }

                menu.DropDown(activatorRect);
            }

            private BreadCrumbListMenu(string subFolder)
            {
                m_SubFolder = subFolder;
            }

            private void SelectSubFolder()
            {
                int folderInstanceID = AssetDatabase.GetMainAssetInstanceID(m_SubFolder);
                if (folderInstanceID != 0)
                    m_Caller.ShowFolderContents(folderInstanceID, false);
            }
        }

        internal class AssetStoreItemContextMenu
        {
            static internal void Show()
            {
                GenericMenu menu = new GenericMenu();

                GUIContent assetStoreWindow = new GUIContent("Show in Asset Store window");
                AssetStoreAsset activeAsset = AssetStoreAssetSelection.GetFirstAsset();
                if (activeAsset != null && activeAsset.id != 0)
                    menu.AddItem(assetStoreWindow, false, new AssetStoreItemContextMenu().OpenAssetStoreWindow);
                else
                    menu.AddDisabledItem(assetStoreWindow);

                menu.ShowAsContext();
            }

            private void OpenAssetStoreWindow()
            {
                AssetStoreAsset activeAsset = AssetStoreAssetSelection.GetFirstAsset();
                if (activeAsset != null)
                    AssetStoreAssetInspector.OpenItemInAssetStore(activeAsset);
            }

            private AssetStoreItemContextMenu()
            {
            }
        }
    }
}
