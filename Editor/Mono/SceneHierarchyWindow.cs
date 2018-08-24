// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;


namespace UnityEditor
{
    [EditorWindowTitle(title = "Hierarchy", useTypeNameAsIconName = true)]
    class SceneHierarchyWindow : SearchableEditorWindow, IHasCustomMenu
    {
        static SceneHierarchyWindow s_LastInteractedHierarchy;
        public static SceneHierarchyWindow lastInteractedHierarchyWindow { get { return s_LastInteractedHierarchy; } }

        class Styles
        {
            const string kCustomSorting = "CustomSorting";
            const string kWarningSymbol = "console.warnicon.sml";
            const string kWarningMessage = "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.";

            public GUIContent defaultSortingContent = new GUIContent(EditorGUIUtility.FindTexture(kCustomSorting));
            public GUIContent createContent = new GUIContent("Create");
            public GUIContent fetchWarning = new GUIContent("", EditorGUIUtility.FindTexture(kWarningSymbol), kWarningMessage);

            public GUIStyle MiniButton;
            public GUIStyle lockButton = "IN LockButton";

            public Styles()
            {
                MiniButton = "ToolbarButton";
            }
        }
        static Styles s_Styles;

        private static List<SceneHierarchyWindow> s_SceneHierarchyWindow = new List<SceneHierarchyWindow>();
        public static List<SceneHierarchyWindow> GetAllSceneHierarchyWindows()
        {
            return s_SceneHierarchyWindow;
        }

        const int kInvalidSceneHandle = 0;
        TreeViewController m_TreeView;
        [SerializeField]
        TreeViewState m_TreeViewState;
        [SerializeField]
        List<string> m_ExpandedScenes = new List<string>(); // saved in layout so we can expand on next Unity session (expanded state is saved per window)
        int m_TreeViewKeyboardControlID;

        [SerializeField]
        private int m_CurrenRootInstanceID = 0;
        [SerializeField]
        bool m_Locked;
        [SerializeField]
        string m_CurrentSortingName = ""; // serialize as string
        [NonSerialized]
        private int m_LastFramedID = -1;
        [NonSerialized]
        bool m_TreeViewReloadNeeded;
        [NonSerialized]
        bool m_SelectionSyncNeeded;
        [NonSerialized]
        bool m_FrameOnSelectionSync;
        [NonSerialized]
        bool m_DidSelectSearchResult;
        Dictionary<string, HierarchySorting> m_SortingObjects = null;
        bool m_AllowAlphaNumericalSort;
        [NonSerialized]
        double m_LastUserInteractionTime;

        bool m_Debug;

        internal static bool debug
        {
            get { return lastInteractedHierarchyWindow.m_Debug; }
            set { lastInteractedHierarchyWindow.m_Debug = value; }
        }

        public static bool s_Debug
        {
            get { return SessionState.GetBool("HierarchyWindowDebug", false); }
            set { SessionState.SetBool("HierarchyWindowDebug", value); }
        }

        bool treeViewReloadNeeded
        {
            get { return m_TreeViewReloadNeeded; }
            set
            {
                m_TreeViewReloadNeeded = value;
                if (value)
                {
                    Repaint();
                    if (s_Debug)
                        Debug.Log("Reload treeview on next event");
                }
            }
        }

        bool selectionSyncNeeded
        {
            get { return m_SelectionSyncNeeded; }
            set
            {
                m_SelectionSyncNeeded = value;
                if (value)
                {
                    Repaint();
                    if (s_Debug)
                        Debug.Log("Selection sync and frameing on next event");
                }
            }
        }

        string currentSortingName
        {
            get { return m_CurrentSortingName; }
            set
            {
                m_CurrentSortingName = value;

                // Ensure backwards compability (if current sort is not found, default to transform sort)
                if (!m_SortingObjects.ContainsKey(m_CurrentSortingName))
                {
                    m_CurrentSortingName = GetNameForType(typeof(TransformSorting));
                }

                // Sync treeview with new sort state
                var dataSource = (GameObjectTreeViewDataSource)treeView.data;
                dataSource.sortingState = m_SortingObjects[m_CurrentSortingName];
            }
        }

        bool hasSortMethods { get { return m_SortingObjects.Count > 1; } }

        Rect treeViewRect
        {
            get { return new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height - EditorGUI.kWindowToolbarHeight); }
        }

        private TreeViewController treeView
        {
            get
            {
                if (m_TreeView == null)
                    Init();
                return m_TreeView;
            }
        }

        void Init()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            m_TreeView = new TreeViewController(this, m_TreeViewState);
            m_TreeView.itemDoubleClickedCallback += TreeViewItemDoubleClicked;
            m_TreeView.selectionChangedCallback += TreeViewSelectionChanged;
            m_TreeView.onGUIRowCallback += OnGUIAssetCallback;
            m_TreeView.dragEndedCallback += OnDragEndedCallback;
            m_TreeView.contextClickItemCallback += ItemContextClick;
            m_TreeView.contextClickOutsideItemsCallback += ContextClickOutsideItems;
            m_TreeView.deselectOnUnhandledMouseDown = true;

            // Both when showing all gos and a sub tree we hide the root
            bool showRootItem = false;
            bool rootItemIsCollapsable = false;

            var dataSource = new GameObjectTreeViewDataSource(m_TreeView, m_CurrenRootInstanceID, showRootItem, rootItemIsCollapsable);
            var dragging = new GameObjectsTreeViewDragging(m_TreeView);
            var gui = new GameObjectTreeViewGUI(m_TreeView, false);
            m_TreeView.Init(treeViewRect, dataSource, gui, dragging);

            dataSource.searchMode = (int)searchMode;
            dataSource.searchString = m_SearchFilter;

            m_AllowAlphaNumericalSort = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false) || !InternalEditorUtility.isHumanControllingUs; // Always allow alphasorting when running automated tests so we can test alpha sorting

            SetUpSortMethodLists();

            m_TreeView.ReloadData();
        }

        internal void SetupForTesting()
        {
            m_AllowAlphaNumericalSort = true;
            SetUpSortMethodLists();
        }

        public void SetCurrentRootInstanceID(int instanceID)
        {
            m_CurrenRootInstanceID = instanceID;
            Init();
            EditorGUIUtility.ExitGUI(); // exit gui since this can be called while iterating items
        }

        // This method is being used by the EditorTests/Searching tests
        public string[] GetCurrentVisibleObjects()
        {
            var rows = m_TreeView.data.GetRows();
            var result = new string[rows.Count];

            for (int i = 0; i < rows.Count; ++i)
                result[i] = rows[i].displayName;
            return result;
        }

        internal void SelectPrevious()
        {
            m_TreeView.OffsetSelection(-1);
        }

        internal void SelectNext()
        {
            m_TreeView.OffsetSelection(1);
        }

        void OnProjectWasLoaded()
        {
            // Game objects will have new instanceIDs in a new Unity session, so clear
            // the expanded state on project start up.
            m_TreeViewState.expandedIDs.Clear();

            // If only one scene ensure it is expanded (new project)
            if (SceneManager.sceneCount == 1)
            {
                treeView.data.SetExpanded(SceneManager.GetSceneAt(0).handle, true);
            }

            // Ensure scenes are expanded from last session
            SetScenesExpanded(m_ExpandedScenes);
        }

        IEnumerable<string> GetExpandedSceneNames()
        {
            List<string> expandedScenes = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (treeView.data.IsExpanded(scene.handle))
                {
                    expandedScenes.Add(scene.name);
                }
            }
            return expandedScenes;
        }

        void SetScenesExpanded(List<string> sceneNames)
        {
            List<int> sceneHandles = new List<int>();
            foreach (string sceneName in sceneNames)
            {
                Scene scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid())
                {
                    sceneHandles.Add(scene.handle);
                }
            }
            if (sceneHandles.Count > 0)
                treeView.data.SetExpandedIDs(sceneHandles.ToArray());
        }

        void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            ExpandTreeViewItem(scene.handle, true);
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ExpandTreeViewItem(scene.handle, true);
        }

        void ExpandTreeViewItem(int id, bool expand)
        {
            var dataSource = treeView.data as TreeViewDataSource;
            if (dataSource != null)
                dataSource.SetExpanded(id, expand);
        }

        void Awake()
        {
            m_HierarchyType = HierarchyType.GameObjects;
            if (m_TreeViewState != null)
            {
                // Clear states that should not survive between restarts of Unity
                m_TreeViewState.OnAwake();
            }
        }

        private void OnBecameVisible()
        {
            // We need to ensure Hierarchy window is reloaded when becoming visible because
            // while it is hidden it does not receive OnHierarchyChanged callbacks (case 611409)
            // During assembly reload editor windows are recreated, at that point scene count is 0 so ignore that event
            if (SceneManager.sceneCount > 0)
                treeViewReloadNeeded = true;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            titleContent = GetLocalizedTitleContent();
            s_SceneHierarchyWindow.Add(this);
            EditorApplication.projectWindowChanged += ReloadData; // Required to know if a prefab gets deleted. Better way of doing this?
            EditorApplication.editorApplicationQuit += OnQuit;
            EditorApplication.searchChanged += SearchChanged;
            EditorApplication.projectWasLoaded += OnProjectWasLoaded;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            s_LastInteractedHierarchy = this;
        }

        public override void OnDisable()
        {
            EditorApplication.projectWindowChanged -= ReloadData;
            EditorApplication.editorApplicationQuit -= OnQuit;
            EditorApplication.searchChanged -= SearchChanged;
            EditorApplication.projectWasLoaded -= OnProjectWasLoaded;
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            s_SceneHierarchyWindow.Remove(this);
        }

        void OnQuit()
        {
            m_ExpandedScenes = GetExpandedSceneNames().ToList();
        }

        public void OnDestroy()
        {
            if (s_LastInteractedHierarchy == this)
            {
                s_LastInteractedHierarchy = null;

                // Set another existing hierarchy as last interacted if available
                foreach (var hierarchy in s_SceneHierarchyWindow)
                    if (hierarchy != this)
                        s_LastInteractedHierarchy = hierarchy;
            }
        }

        void SetAsLastInteractedHierarchy()
        {
            s_LastInteractedHierarchy = this;
        }

        void SyncIfNeeded()
        {
            if (treeViewReloadNeeded)
            {
                treeViewReloadNeeded = false;
                ReloadData();
            }

            if (selectionSyncNeeded)
            {
                selectionSyncNeeded = false;

                bool userJustInteracted = (EditorApplication.timeSinceStartup - m_LastUserInteractionTime) < 0.2;
                bool frame = !m_Locked || m_FrameOnSelectionSync || userJustInteracted;
                bool animatedFraming = userJustInteracted && frame;
                m_FrameOnSelectionSync = false;

                treeView.SetSelection(Selection.instanceIDs, frame, animatedFraming);
            }
        }

        void DetectUserInteraction()
        {
            Event evt = Event.current;
            if (evt.type != EventType.Layout && evt.type != EventType.Repaint)
            {
                m_LastUserInteractionTime = EditorApplication.timeSinceStartup;
            }
        }

        void OnGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            DetectUserInteraction();
            SyncIfNeeded();

            m_TreeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);

            OnEvent();

            Rect SceneHierarchyRect = new Rect(0, 0, position.width, position.height);
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && SceneHierarchyRect.Contains(evt.mousePosition))
            {
                treeView.EndPing();
                SetAsLastInteractedHierarchy();
            }

            DoToolbar();
            float searchPathHeight = DoSearchResultPathGUI();
            DoTreeView(searchPathHeight);
            ExecuteCommands();
        }

        void OnLostFocus()
        {
            // Added because this window uses RenameOverlay
            treeView.EndNameEditing(true);
        }

        public static bool IsSceneHeaderInHierarchyWindow(Scene scene)
        {
            return scene.IsValid();
        }

        void TreeViewItemDoubleClicked(int instanceID)
        {
            Scene scene = EditorSceneManager.GetSceneByHandle(instanceID);

            if (IsSceneHeaderInHierarchyWindow(scene))
            {
                // scene header selected
                if (scene.isLoaded)
                    EditorSceneManager.SetActiveScene(scene);
            }
            else
                // GameObject selected
                SceneView.FrameLastActiveSceneView();
        }

        public void SetExpandedRecursive(int id, bool expand)
        {
            TreeViewItem item = treeView.data.FindItem(id);

            // If the item is null reload the data as the scene might have changed.
            if (item == null)
            {
                ReloadData();
                item = treeView.data.FindItem(id);
            }

            if (item != null)
                treeView.data.SetExpandedWithChildren(item, expand);
        }

        void OnGUIAssetCallback(int instanceID, Rect rect)
        {
            // User hook for rendering stuff on top of assets
            if (EditorApplication.hierarchyWindowItemOnGUI != null)
            {
                EditorApplication.hierarchyWindowItemOnGUI(instanceID, rect);
            }
        }

        void OnDragEndedCallback(int[] draggedInstanceIds, bool draggedItemsFromOwnTreeView)
        {
            // We only change selection if 'draggedItemsFromOwnTreeView' == true.
            // This ensures that we do not override the selection that might have been set before
            // calling this callback when dragging e.g a prefab to the hierarchy (case 628939)
            if (draggedInstanceIds != null && draggedItemsFromOwnTreeView)
            {
                ReloadData();
                treeView.SetSelection(draggedInstanceIds, true);
                treeView.NotifyListenersThatSelectionChanged(); // behave as if selection was performed in treeview
                Repaint();
                GUIUtility.ExitGUI();
            }
        }

        public void ReloadData()
        {
            if (m_TreeView == null)
                Init();
            else
                m_TreeView.ReloadData();
        }

        public void SearchChanged()
        {
            var data = (GameObjectTreeViewDataSource)treeView.data;

            // Search didnt change do nothing.
            if (data.searchMode == (int)searchMode && data.searchString == m_SearchFilter)
                return;

            data.searchMode = (int)searchMode;
            data.searchString = m_SearchFilter;

            if (m_SearchFilter == "")
            {
                treeView.Frame(Selection.activeInstanceID, true, false);
            }

            ReloadData();
        }

        void TreeViewSelectionChanged(int[] ids)
        {
            Selection.instanceIDs = ids;

            m_DidSelectSearchResult = !string.IsNullOrEmpty(m_SearchFilter);
        }

        bool IsTreeViewSelectionInSyncWithBackend()
        {
            if (m_TreeView != null)
                return m_TreeView.state.selectedIDs.SequenceEqual(Selection.instanceIDs);
            return false;
        }

        void OnSelectionChange()
        {
            if (!IsTreeViewSelectionInSyncWithBackend())
            {
                selectionSyncNeeded = true;
            }
            else
            {
                if (s_Debug)
                    Debug.Log("OnSelectionChange: Selection is already in sync so no framing will happen");
            }
        }

        void OnHierarchyChange()
        {
            if (m_TreeView != null)
                m_TreeView.EndNameEditing(false);
            treeViewReloadNeeded = true;
        }

        float DoSearchResultPathGUI()
        {
            if (!hasSearchFilter)
                return 0;

            GUILayout.FlexibleSpace();
            Rect verticalRect = EditorGUILayout.BeginVertical(EditorStyles.inspectorBig);
            GUILayout.Label("Path:");

            if (m_TreeView.HasSelection())
            {
                int selectedID = m_TreeView.GetSelection()[0];

                IHierarchyProperty activeSelectedInHierachy = new HierarchyProperty(HierarchyType.GameObjects);
                activeSelectedInHierachy.Find(selectedID, null);
                if (activeSelectedInHierachy.isValid)
                {
                    // We have a valid game object: now show its path
                    do
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label(activeSelectedInHierachy.icon);
                        GUILayout.Label(activeSelectedInHierachy.name);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                    while (activeSelectedInHierachy.Parent());
                }
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(0);
            return verticalRect.height;
        }

        void OnEvent()
        {
            treeView.OnEvent();
        }

        void DoTreeView(float searchPathHeight)
        {
            // subtract the search path height from the available height.
            Rect rect = treeViewRect;
            rect.height -= searchPathHeight;
            treeView.OnGUI(rect, m_TreeViewKeyboardControlID);
        }

        void DoToolbar()
        {
            // Gameobject popup dropdown
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            CreateGameObjectPopup();

            //Search field GUI
            GUILayout.Space(6);

            if (s_Debug)
            {
                int firstRow, lastRow;
                m_TreeView.gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);
                GUILayout.Label(string.Format("{0} ({1}, {2})", m_TreeView.data.rowCount, firstRow, lastRow), EditorStyles.miniLabel);
                GUILayout.Space((6));
            }

            GUILayout.FlexibleSpace();
            Event evt = Event.current;

            // When searchfield has focus give keyboard focus to the tree view on Down/UpArrow
            if (hasSearchFilterFocus && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow))
            {
                GUIUtility.keyboardControl = m_TreeViewKeyboardControlID;

                // If nothing is selected ensure first item is selected, otherwise ensure current
                // selection is visible (we just gave focus to the tree)
                if (treeView.IsLastClickedPartOfRows())
                {
                    treeView.Frame(treeView.state.lastClickedID, true, false);
                    m_DidSelectSearchResult = !string.IsNullOrEmpty(m_SearchFilter);
                }
                else
                    treeView.OffsetSelection(1);  // Selects first item

                evt.Use();
            }

            SearchFieldGUI();

            // Sortmethod GUI
            GUILayout.Space(6);

            if (hasSortMethods)
            {
                if (Application.isPlaying && ((GameObjectTreeViewDataSource)treeView.data).isFetchAIssue)
                {
                    GUILayout.Toggle(false, s_Styles.fetchWarning, s_Styles.MiniButton);
                }

                SortMethodsDropDown();
            }


            GUILayout.EndHorizontal();
        }

        internal override void SetSearchFilter(string searchFilter, SearchableEditorWindow.SearchMode searchMode, bool setAll, bool delayed = false)
        {
            base.SetSearchFilter(searchFilter, searchMode, setAll, delayed);

            // If the user clears the search we frame the last selection he made during the search
            if (m_DidSelectSearchResult && string.IsNullOrEmpty(searchFilter))
            {
                m_DidSelectSearchResult = false;
                FrameObjectPrivate(Selection.activeInstanceID, true, false, false);

                // Ensure item has focus for visual feedback and instant key navigation
                if (GUIUtility.keyboardControl == 0)
                    GUIUtility.keyboardControl = m_TreeViewKeyboardControlID;
            }
        }

        /*
         * NOTE: AddCreateGameObjectItemsToMenu() cooks existing menu, so that make sure menu entries are added to
         *               localization entry.
         * @Localization("Create Empty", "MenuItem")
         * @Localization("Create Empty Child", "MenuItem")
         */
        void AddCreateGameObjectItemsToMenu(GenericMenu menu, UnityEngine.Object[] context, bool includeCreateEmptyChild, bool includeGameObjectInPath, int targetSceneHandle)
        {
            string[] menus = Unsupported.GetSubmenus("GameObject");
            foreach (string path in menus)
            {
                UnityEngine.Object[] tempContext = context;
                if (!includeCreateEmptyChild && path.ToLower() == "GameObject/Create Empty Child".ToLower())
                    continue;
                // Don't include context for Wizards (item ends with ...), since reparenting doesn't work here anyway, but a multiselection
                // would cause multiple wizards to be opened simultaneously
                if (path.EndsWith("..."))
                    tempContext = null;
                // The first item after the GameObject creation menu items
                if (path.ToLower() == "GameObject/Center On Children".ToLower())
                    return;

                string menupath = path;
                if (!includeGameObjectInPath)
                    menupath = path.Substring(11); // cut away "GameObject/"
                MenuUtils.ExtractMenuItemWithPath(path, menu, menupath, tempContext, targetSceneHandle, BeforeCreateGameObjectMenuItemWasExecuted, AfterCreateGameObjectMenuItemWasExecuted);
            }
        }

        void BeforeCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, int userData)
        {
            int sceneHandle = userData;
            EditorSceneManager.SetTargetSceneForNewGameObjects(sceneHandle);
        }

        void AfterCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, int userData)
        {
            EditorSceneManager.SetTargetSceneForNewGameObjects(kInvalidSceneHandle);

            // Ensure framing when creating game objects even if we are locked
            if (m_Locked)
                m_FrameOnSelectionSync = true;
        }

        void CreateGameObjectPopup()
        {
            Rect rect = GUILayoutUtility.GetRect(s_Styles.createContent, EditorStyles.toolbarDropDown, null);
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbarDropDown.Draw(rect, s_Styles.createContent, false, false, false, false);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                GUIUtility.hotControl = 0;

                GenericMenu menu = new GenericMenu();
                AddCreateGameObjectItemsToMenu(menu, null, true, false, kInvalidSceneHandle);
                menu.DropDown(rect);

                Event.current.Use();
            }
        }

        void SortMethodsDropDown()
        {
            if (hasSortMethods)
            {
                // Labels button
                GUIContent content = m_SortingObjects[currentSortingName].content;
                if (content == null)
                {
                    content = s_Styles.defaultSortingContent;
                    content.tooltip = currentSortingName;
                }

                Rect r = GUILayoutUtility.GetRect(content, EditorStyles.toolbarButton);
                if (EditorGUI.DropdownButton(r, content, FocusType.Passive, EditorStyles.toolbarButton))
                {
                    // Build list items
                    var sortFunctionItems = new List<SceneHierarchySortingWindow.InputData>();
                    foreach (var entry in m_SortingObjects)
                    {
                        var data = new SceneHierarchySortingWindow.InputData();
                        data.m_TypeName = entry.Key;
                        data.m_Name = ObjectNames.NicifyVariableName(entry.Key); //entry.Key == class name;
                        data.m_Selected = entry.Key == m_CurrentSortingName;
                        sortFunctionItems.Add(data);
                    }

                    // Show popup
                    if (SceneHierarchySortingWindow.ShowAtPosition(new Vector2(r.x, r.y + r.height), sortFunctionItems, SortFunctionCallback))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        void SetUpSortMethodLists()
        {
            m_SortingObjects = new Dictionary<string, HierarchySorting>();

            var transformSorting = new TransformSorting();
            m_SortingObjects.Add(GetNameForType(transformSorting.GetType()), transformSorting);

            // The user have to activate AlphabeticalSorting in the preferences (we ensure AlphabeticalSorting is added for integration tests)
            if (m_AllowAlphaNumericalSort || !InternalEditorUtility.isHumanControllingUs)
            {
                var alphabeticalSorting = new AlphabeticalSorting();
                m_SortingObjects.Add(GetNameForType(alphabeticalSorting.GetType()), alphabeticalSorting);
            }

            // Ensure to reconstruct state setup when setting sorting name
            currentSortingName = m_CurrentSortingName;
        }

        string GetNameForType(Type type)
        {
            return type.Name;
        }

        void SortFunctionCallback(SceneHierarchySortingWindow.InputData data)
        {
            SetSortFunction(data.m_TypeName);
        }

        public void SetSortFunction(Type sortType)
        {
            SetSortFunction(GetNameForType(sortType));
        }

        void SetSortFunction(string sortTypeName)
        {
            if (!m_SortingObjects.ContainsKey(sortTypeName))
            {
                Debug.LogError("Invalid search type name: " + sortTypeName);
                return;
            }

            currentSortingName = sortTypeName;
            if (treeView.GetSelection().Any())
                treeView.Frame(treeView.GetSelection().First(), true, false);
            treeView.ReloadData();
        }

        public void DirtySortingMethods()
        {
            m_AllowAlphaNumericalSort = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);
            SetUpSortMethodLists();
            treeView.SetSelection(treeView.GetSelection(), true);
            treeView.ReloadData();
        }

        void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            bool execute = evt.type == EventType.ExecuteCommand;

            if (evt.commandName == "Delete" || evt.commandName == "SoftDelete")
            {
                if (execute)
                    DeleteGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "Duplicate")
            {
                if (execute)
                    DuplicateGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "Copy")
            {
                if (execute)
                    CopyGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "Paste")
            {
                if (execute)
                    PasteGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "SelectAll")
            {
                if (execute)
                    SelectAll();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "FrameSelected")
            {
                if (evt.type == EventType.ExecuteCommand)
                    FrameObjectPrivate(Selection.activeInstanceID, true, true, true);
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == "Find")
            {
                if (evt.type == EventType.ExecuteCommand)
                    FocusSearchField();
                evt.Use();
            }
        }

        void CreateGameObjectContextClick(GenericMenu menu, int contextClickedItemID)
        {
            menu.AddItem(EditorGUIUtility.TextContent("Copy"), false, CopyGO);
            menu.AddItem(EditorGUIUtility.TextContent("Paste"), false, PasteGO);

            menu.AddSeparator("");
            // TODO: Add this back in.
            if (!hasSearchFilter && m_TreeViewState.selectedIDs.Count == 1)
                menu.AddItem(EditorGUIUtility.TextContent("Rename"), false, RenameGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Rename"));
            menu.AddItem(EditorGUIUtility.TextContent("Duplicate"), false, DuplicateGO);
            menu.AddItem(EditorGUIUtility.TextContent("Delete"), false, DeleteGO);

            menu.AddSeparator("");
            bool hasPrefabParent = false;

            if (m_TreeViewState.selectedIDs.Count == 1)
            {
                GameObjectTreeViewItem item = treeView.FindItem(m_TreeViewState.selectedIDs[0]) as GameObjectTreeViewItem;
                if (item != null)
                {
                    UnityEngine.Object prefab = PrefabUtility.GetPrefabParent(item.objectPPTR);
                    if (prefab != null)
                    {
                        menu.AddItem(EditorGUIUtility.TextContent("Select Prefab"), false, () =>
                            {
                                Selection.activeObject = prefab;
                                EditorGUIUtility.PingObject(prefab.GetInstanceID());
                            });

                        hasPrefabParent = true;
                    }
                }
            }

            if (!hasPrefabParent)
                menu.AddDisabledItem(EditorGUIUtility.TextContent("Select Prefab"));

            menu.AddSeparator("");

            // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
            // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
            AddCreateGameObjectItemsToMenu(menu, Selection.transforms.Select(t => t.gameObject).ToArray(), false, false, kInvalidSceneHandle);
            menu.ShowAsContext();
        }

        void CreateMultiSceneHeaderContextClick(GenericMenu menu, int contextClickedItemID)
        {
            Scene scene = EditorSceneManager.GetSceneByHandle(contextClickedItemID);
            if (!IsSceneHeaderInHierarchyWindow(scene))
            {
                Debug.LogError("Context clicked item is not a scene");
                return;
            }

            bool hasMultipleScenes = EditorSceneManager.sceneCount > 1;

            // Set active
            if (scene.isLoaded)
            {
                var content = EditorGUIUtility.TextContent("Set Active Scene");
                if (hasMultipleScenes && SceneManager.GetActiveScene() != scene)
                    menu.AddItem(content, false, SetSceneActive, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
                menu.AddSeparator("");
            }

            // Save
            if (scene.isLoaded)
            {
                if (!EditorApplication.isPlaying)
                {
                    menu.AddItem(EditorGUIUtility.TextContent("Save Scene"), false, SaveSelectedScenes, contextClickedItemID);
                    menu.AddItem(EditorGUIUtility.TextContent("Save Scene As"), false, SaveSceneAs, contextClickedItemID);
                    if (hasMultipleScenes)
                        menu.AddItem(EditorGUIUtility.TextContent("Save All"), false, SaveAllScenes, contextClickedItemID);
                    else
                        menu.AddDisabledItem(EditorGUIUtility.TextContent("Save All"));
                }
                else
                {
                    menu.AddDisabledItem(EditorGUIUtility.TextContent("Save Scene"));
                    menu.AddDisabledItem(EditorGUIUtility.TextContent("Save Scene As"));
                    menu.AddDisabledItem(EditorGUIUtility.TextContent("Save All"));
                }
                menu.AddSeparator("");
            }

            bool isUnloadOrRemoveValid = EditorSceneManager.loadedSceneCount != GetNumLoadedScenesInSelection();

            if (scene.isLoaded)
            {
                // Unload
                var content = EditorGUIUtility.TextContent("Unload Scene");
                bool canUnloadScenes = isUnloadOrRemoveValid && !EditorApplication.isPlaying && !string.IsNullOrEmpty(scene.path);
                if (canUnloadScenes)
                    menu.AddItem(content, false, UnloadSelectedScenes, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
            }
            else
            {
                // Load
                var content = EditorGUIUtility.TextContent("Load Scene");
                bool canLoadScenes = !EditorApplication.isPlaying;
                if (canLoadScenes)
                    menu.AddItem(content, false, LoadSelectedScenes, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
            }

            // Remove
            var removeContent = EditorGUIUtility.TextContent("Remove Scene");
            bool selectedAllScenes = GetSelectedScenes().Count == EditorSceneManager.sceneCount;
            bool canRemoveScenes = isUnloadOrRemoveValid && !selectedAllScenes && !EditorApplication.isPlaying;
            if (canRemoveScenes)
                menu.AddItem(removeContent, false, RemoveSelectedScenes, contextClickedItemID);
            else
                menu.AddDisabledItem(removeContent);

            // Discard changes
            if (scene.isLoaded)
            {
                var content = EditorGUIUtility.TextContent("Discard changes");
                var selectedSceneHandles = GetSelectedScenes();
                var modifiedScenes = GetModifiedScenes(selectedSceneHandles);
                bool canDiscardChanges = !EditorApplication.isPlaying && modifiedScenes.Length > 0;
                if (canDiscardChanges)
                    menu.AddItem(content, false, DiscardChangesInSelectedScenes, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
            }

            // Ping Scene Asset
            menu.AddSeparator("");
            var selectAssetContent = EditorGUIUtility.TextContent("Select Scene Asset");
            if (!string.IsNullOrEmpty(scene.path))
                menu.AddItem(selectAssetContent, false, SelectSceneAsset, contextClickedItemID);
            else
                menu.AddDisabledItem(selectAssetContent);

            var addSceneContent = EditorGUIUtility.TextContent("Add New Scene");
            if (!EditorApplication.isPlaying)
                menu.AddItem(addSceneContent, false, AddNewScene, contextClickedItemID);
            else
                menu.AddDisabledItem(addSceneContent);

            // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
            // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
            if (scene.isLoaded)
            {
                menu.AddSeparator("");
                AddCreateGameObjectItemsToMenu(menu, Selection.transforms.Select(t => t.gameObject).ToArray(), false, true, scene.handle);
            }
        }

        int GetNumLoadedScenesInSelection()
        {
            int loadedScenes = 0;
            foreach (int handle in GetSelectedScenes())
            {
                if (EditorSceneManager.GetSceneByHandle(handle).isLoaded)
                    loadedScenes++;
            }
            return loadedScenes;
        }

        // returns selected scene handles
        List<int> GetSelectedScenes()
        {
            var selectedSceneHandles = new List<int>();
            int[] instanceIDs = m_TreeView.GetSelection();
            foreach (int id in instanceIDs)
            {
                if (IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(id)))
                {
                    selectedSceneHandles.Add(id);
                }
            }
            return selectedSceneHandles;
        }

        void ContextClickOutsideItems()
        {
            Event evt = Event.current;
            evt.Use();

            var menu = new GenericMenu();
            CreateGameObjectContextClick(menu, kInvalidSceneHandle);

            menu.ShowAsContext();
        }

        void ItemContextClick(int contextClickedItemID)
        {
            Event evt = Event.current;

            evt.Use();
            var menu = new GenericMenu();

            bool clickedSceneHeader = IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(contextClickedItemID));

            if (clickedSceneHeader)
                CreateMultiSceneHeaderContextClick(menu, contextClickedItemID);
            else
                CreateGameObjectContextClick(menu, contextClickedItemID);

            menu.ShowAsContext();
        }

        void CopyGO()
        {
            Unsupported.CopyGameObjectsToPasteboard();
        }

        void PasteGO()
        {
            Unsupported.PasteGameObjectsFromPasteboard();
        }

        void DuplicateGO()
        {
            Unsupported.DuplicateGameObjectsUsingPasteboard();
        }

        void RenameGO()
        {
            treeView.BeginNameEditing(0f);
        }

        void DeleteGO()
        {
            Unsupported.DeleteGameObjectSelection();
        }

        void SetSceneActive(object userData)
        {
            int itemID = (int)userData;
            EditorSceneManager.SetActiveScene(EditorSceneManager.GetSceneByHandle(itemID));
        }

        private void LoadSelectedScenes(object userdata)
        {
            List<int> selectedScenes = GetSelectedScenes();
            foreach (var id in selectedScenes)
            {
                var scene = EditorSceneManager.GetSceneByHandle(id);
                if (!scene.isLoaded)
                    EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
            }

            EditorApplication.RequestRepaintAllViews();
        }

        private void SaveSceneAs(object userdata)
        {
            int itemID = (int)userdata;

            var scene = EditorSceneManager.GetSceneByHandle(itemID);
            if (scene.isLoaded)
            {
                EditorSceneManager.SaveSceneAs(scene);
            }
        }

        private void SaveAllScenes(object userdata)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        void SaveSelectedScenes(object userdata)
        {
            List<int> selectedScenes = GetSelectedScenes();
            foreach (var id in selectedScenes)
            {
                var scene = EditorSceneManager.GetSceneByHandle(id);
                if (scene.isLoaded)
                    EditorSceneManager.SaveScene(scene);
            }
        }

        void UnloadSelectedScenes(object userdata)
        {
            const bool removeScenesFromHierarchy = false;
            CloseSelectedScenes(removeScenesFromHierarchy);
        }

        void RemoveSelectedScenes(object userData)
        {
            const bool removeScenesFromHierarchy = true;
            CloseSelectedScenes(removeScenesFromHierarchy);
        }

        bool UserAllowedDiscardingChanges(Scene[] modifiedScenes)
        {
            string title = "Discard Changes";
            string message = "Are you sure you want to discard the changes in the following scenes:\n\n   {0}\n\nYour changes will be lost.";

            string sceneNames = string.Join("\n   ", modifiedScenes.Select(scene => scene.name).ToArray());
            message = string.Format(message, sceneNames);

            return EditorUtility.DisplayDialog(title, message, "OK", "Cancel");
        }

        void DiscardChangesInSelectedScenes(object userData)
        {
            var expandedSceneNames = GetExpandedSceneNames();
            var selectedSceneHandles = GetSelectedScenes();
            var modifiedScenes = GetModifiedScenes(selectedSceneHandles);
            var modifiedScenesWithSavePath = modifiedScenes.Where(scene => !string.IsNullOrEmpty(scene.path)).ToArray();

            if (!UserAllowedDiscardingChanges(modifiedScenesWithSavePath))
                return;

            if (modifiedScenesWithSavePath.Length != modifiedScenes.Length)
                Debug.LogWarning("Discarding changes in a scene that have not yet been saved is not supported. Save the scene first or create a new scene.");

            foreach (var scene in modifiedScenesWithSavePath)
            {
                EditorSceneManager.ReloadScene(scene);
            }

            // When reloading a single scene it will be given a new scene handle which will collapse it in the Hierarchy.
            // Here we ensure same scene are expanded
            if (SceneManager.sceneCount == 1)
                SetScenesExpanded(expandedSceneNames.ToList());

            EditorApplication.RequestRepaintAllViews();
        }

        Scene[] GetModifiedScenes(List<int> handles)
        {
            return handles.Select(handle => EditorSceneManager.GetSceneByHandle(handle)).Where(scene => scene.isDirty).ToArray();
        }

        void CloseSelectedScenes(bool removeScenes)
        {
            var selectedSceneHandles = GetSelectedScenes();

            var modifiedScenes = GetModifiedScenes(selectedSceneHandles);
            bool userCancelled = !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(modifiedScenes);
            if (userCancelled)
                return;

            foreach (var id in selectedSceneHandles)
                EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByHandle(id), removeScenes);

            EditorApplication.RequestRepaintAllViews();
        }

        void AddNewScene(object userData)
        {
            // Check for existing untitled scene
            Scene untitledScene = EditorSceneManager.GetSceneByPath("");
            if (untitledScene.IsValid())
            {
                var title = EditorGUIUtility.TextContent("Save Untitled Scene").text;
                var subTitle = EditorGUIUtility.TextContent("Existing Untitled scene needs to be saved before creating a new scene. Only one untitled scene is supported at a time.").text;
                if (EditorUtility.DisplayDialog(title, subTitle, "Save", "Cancel"))
                {
                    if (!EditorSceneManager.SaveScene(untitledScene))
                        return;
                }
                else
                    return;
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);

            // Move new scene after context clicked scene
            int itemID = (int)userData;
            var scene = EditorSceneManager.GetSceneByHandle(itemID);
            if (scene.IsValid())
                EditorSceneManager.MoveSceneAfter(newScene, scene);
        }

        void SelectSceneAsset(object userData)
        {
            int itemID = (int)userData;
            var scene = EditorSceneManager.GetSceneByHandle(itemID);
            string guid = AssetDatabase.AssetPathToGUID(scene.path);
            int instanceID = AssetDatabase.GetInstanceIDFromGUID(guid);
            Selection.activeInstanceID = instanceID;
            EditorGUIUtility.PingObject(instanceID);
        }

        private void SelectAll()
        {
            int[] instanceIDs = treeView.GetRowIDs();
            treeView.SetSelection(instanceIDs, false);
            TreeViewSelectionChanged(instanceIDs);
        }

        static void ToggleDebugMode()
        {
            s_Debug = !s_Debug;
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (Unsupported.IsDeveloperBuild())
            {
                menu.AddItem(new GUIContent("DEVELOPER/Toggle DebugMode"), false, ToggleDebugMode);
            }
        }

        public void FrameObject(int instanceID, bool ping)
        {
            const bool animatedFraming = true;
            FrameObjectPrivate(instanceID, true, ping, animatedFraming);
        }

        private void FrameObjectPrivate(int instanceID, bool frame, bool ping, bool animatedFraming)
        {
            if (instanceID == 0)
                return;

            // If framing the same instance as the last one we do not remove the ping
            // since issuing first a ping and then a framing should still show the ping.
            if (m_LastFramedID != instanceID)
                treeView.EndPing();

            SetSearchFilter("", SearchableEditorWindow.SearchMode.All, true);

            m_LastFramedID = instanceID;

            treeView.Frame(instanceID, frame, ping, animatedFraming);

            FrameObjectPrivate(InternalEditorUtility.GetGameObjectInstanceIDFromComponent(instanceID), frame, ping, animatedFraming);
        }

        // Called from DockArea
        protected virtual void ShowButton(Rect r)
        {
            if (s_Styles == null)
                s_Styles = new Styles();
            m_Locked = GUI.Toggle(r, m_Locked, GUIContent.none, s_Styles.lockButton);
        }
    }

    // Used for type checking and text and icon for sort dropdown in the Hierarchy
    internal abstract class HierarchySorting
    {
        public virtual GUIContent content { get { return null; } }
    }

    internal class TransformSorting : HierarchySorting
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("DefaultSorting"), "Transform Child Order");
        public override GUIContent content { get { return m_Content; } }
    }

    internal class AlphabeticalSorting : HierarchySorting
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("AlphabeticalSorting"), "Alphabetical Order");
        public override GUIContent content { get { return m_Content; } }
    }

    // User defined sorting was disabled for 5.4 because of performance reasons. We cannot ensure sorting
    // that is fast enough. So to prevent killing framerate in playmode we have removed BaseHierarchySort and friends

    [Obsolete("BaseHierarchySort is no longer supported because of performance reasons")]
    public class TransformSort : BaseHierarchySort
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("DefaultSorting"), "Transform Child Order");
        public override GUIContent content { get { return m_Content; } }
    }

    [Obsolete("BaseHierarchySort is no longer supported because of performance reasons")]
    public class AlphabeticalSort : BaseHierarchySort
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("AlphabeticalSorting"), "Alphabetical Order");
        public override GUIContent content { get { return m_Content; } }
    }


    [Obsolete("BaseHierarchySort is no longer supported because of performance reasons")]
    public abstract class BaseHierarchySort : IComparer<GameObject>
    {
        public virtual GUIContent content { get { return null; } }
        public virtual int Compare(GameObject lhs, GameObject rhs) { return 0; }
    }
}
