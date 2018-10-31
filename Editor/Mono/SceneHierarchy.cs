// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine.Assertions;

namespace UnityEditor
{
    [Serializable]
    internal class SceneHierarchy
    {
        static class Styles
        {
            const string kCustomSorting = "CustomSorting";
            const string kWarningSymbol = "console.warnicon.sml";
            const string kWarningMessage = "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.";

            public static GUIContent defaultSortingContent = new GUIContent(EditorGUIUtility.FindTexture(kCustomSorting));
            public static GUIContent createContent = EditorGUIUtility.TrTextContent("Create");
            public static GUIContent fetchWarning = new GUIContent("", EditorGUIUtility.FindTexture(kWarningSymbol), kWarningMessage);

            public static GUIStyle lockButton = "IN LockButton";
        }

        EditorWindow m_EditorWindow;

        int m_FrameRequestID;
        bool m_FrameRequestPing;

        Scene[] m_CustomScenes;

        public Scene[] customScenes
        {
            get { return m_CustomScenes; }
            set { m_CustomScenes = value; Init(); }
        }

        Transform m_CustomParentForNewGameObjects;
        public Transform customParentForNewGameObjects
        {
            set
            {
                m_CustomParentForNewGameObjects = value;
                if (m_TreeView != null && m_TreeView.dragging != null)
                    ((GameObjectsTreeViewDragging)m_TreeView.dragging).parentForDraggedObjectsOutsideItems = m_CustomParentForNewGameObjects;
            }
        }

        GameObjectsTreeViewDragging.CustomDraggingDelegate m_CustomDragHandler;
        public void SetCustomDragHandler(GameObjectsTreeViewDragging.CustomDraggingDelegate handler)
        {
            m_CustomDragHandler = handler;
            if (m_TreeView != null && m_TreeView.dragging != null)
                ((GameObjectsTreeViewDragging)m_TreeView.dragging).SetCustomDragHandler(handler);
        }

        public bool hasCustomScenes
        {
            get { return customScenes != null && customScenes.Length > 0; }
        }

        public Rect position { get; set; }
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
        EditorGUIUtility.EditorLockTracker m_LockTracker = new EditorGUIUtility.EditorLockTracker();

        internal bool isLocked
        {
            get { return m_LockTracker.isLocked; }
            set { m_LockTracker.isLocked = value; }
        }

        [NonSerialized]
        List<string> m_ParentNamesForSelectedSearchResult = new List<string>();

        string m_SearchFilter;
        SearchableEditorWindow.SearchModeHierarchyWindow m_SearchMode = SearchableEditorWindow.SearchModeHierarchyWindow.All;

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
        [NonSerialized]
        double m_LastUserInteractionTime;

        public static bool s_Debug
        {
            get { return SessionState.GetBool("HierarchyWindowDebug", false); }
            set { SessionState.SetBool("HierarchyWindowDebug", value); }
        }

        public static bool s_DebugPrefabStage
        {
            get { return SessionState.GetBool("PrefabStageDebug", false); }
            set { SessionState.SetBool("PrefabStageDebug", value); }
        }

        internal static bool s_DebugPersistingExpandedState
        {
            get { return SessionState.GetBool("ExpandedStateDebug", false); }
            set { SessionState.SetBool("ExpandedStateDebug", value); }
        }

        internal bool hasSearchFilter
        {
            get { return !string.IsNullOrEmpty(m_SearchFilter); }
        }

        [SerializeField]
        string m_CurrentSortingName = ""; // serialize as string

        Dictionary<string, HierarchySorting> m_SortingObjects = null;
        bool m_AllowAlphaNumericalSort;

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
                dataSource.sortingState = m_SortingObjects[m_CurrentSortingName];
            }
        }

        public bool hasSortMethods { get { return m_SortingObjects.Count > 1; } }

        public int treeViewKeyboardControlID { get { return m_TreeViewKeyboardControlID; } }

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

        Rect treeViewRect
        {
            get { return position; }
        }

        internal TreeViewState treeViewState
        {
            get { return m_TreeViewState; }
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

        public List<GameObject> GetExpandedGameObjects()
        {
            dataSource.EnsureFullyInitialized();

            var gameObjects = new List<GameObject>();
            for (int i = 0; i < m_TreeView.data.rowCount; ++i)
            {
                var item = (GameObjectTreeViewItem)m_TreeView.data.GetItem(i);
                if (item.hasChildren && m_TreeView.data.IsExpanded(item))
                {
                    if (item.objectPPTR is GameObject)
                        gameObjects.Add((GameObject)item.objectPPTR);
                }
            }
            return gameObjects;
        }

        void Repaint()
        {
            // m_EditorWindow is null before Awake is called on startup (we have events like OnBecameVisible and OnFocus which is called before Awake)
            if (m_EditorWindow != null)
                m_EditorWindow.Repaint();
        }

        void Init()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            m_TreeView = new TreeViewController(m_EditorWindow, m_TreeViewState);
            m_TreeView.itemDoubleClickedCallback += TreeViewItemDoubleClicked;
            m_TreeView.selectionChangedCallback += TreeViewSelectionChanged;
            m_TreeView.onGUIRowCallback += OnRowGUICallback;
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

            dataSource.searchMode = m_SearchMode;
            dataSource.searchString = m_SearchFilter;
            dataSource.scenes = m_CustomScenes;
            dataSource.sortingState = m_SortingObjects[m_CurrentSortingName];
            dragging.parentForDraggedObjectsOutsideItems = m_CustomParentForNewGameObjects;
            dragging.SetCustomDragHandler(m_CustomDragHandler);

            m_TreeView.ReloadData();
        }

        bool AreCustomScenesValid(Scene[] customScenes)
        {
            if (customScenes == null)
                return true;

            foreach (var scene in customScenes)
                if (!scene.IsValid())
                    return false;

            return true;
        }

        bool IsShowingPreviewScene()
        {
            if (m_CustomScenes != null && m_CustomScenes.Length > 0)
                return EditorSceneManager.IsPreviewScene(m_CustomScenes[0]);
            return false;
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
            //if (treeView.GetSelection().Any())
            //    treeView.Frame(treeView.GetSelection().First(), true, false);
            ReloadData();
        }

        internal void SetupForTesting()
        {
            m_AllowAlphaNumericalSort = true;
            SetUpSortMethodLists();
        }

        public void DirtySortingMethods()
        {
            m_AllowAlphaNumericalSort = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false);
            SetUpSortMethodLists();
            //m_SceneHierarchy.SetSelection((treeView.GetSelection(), true);
            ReloadData();
        }

        public void SetFocusAndEnsureSelectedItem()
        {
            GUIUtility.keyboardControl = m_TreeViewKeyboardControlID;
            EditorGUIUtility.editingTextField = false;

            if (m_TreeView.data.rowCount > 0)
            {
                if (m_TreeView.IsLastClickedPartOfRows())
                {
                    m_TreeView.Frame(m_TreeViewState.lastClickedID, true, false);
                }
                else
                {
                    m_TreeView.SetSelection(new[] { m_TreeView.data.GetRows()[0].id }, true);
                    m_TreeView.NotifyListenersThatSelectionChanged();
                }
            }
        }

        public void SetCurrentRootInstanceID(int instanceID)
        {
            m_CurrenRootInstanceID = instanceID;
            Init();
            EditorGUIUtility.ExitGUI(); // exit gui since this can be called while iterating items
        }

        public int[] GetExpandedIDs()
        {
            return m_TreeViewState.expandedIDs.ToArray();
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

        public virtual void Awake(EditorWindow editorWindow)
        {
            m_EditorWindow = editorWindow;

            if (m_TreeViewState != null)
            {
                // Clear states that should not survive between restarts of Unity
                m_TreeViewState.OnAwake();
            }
        }

        public virtual void OnBecameVisible()
        {
            // We need to ensure Hierarchy window is reloaded when becoming visible because
            // while it is hidden it does not receive OnHierarchyChanged callbacks (case 611409)
            // During assembly reload editor windows are recreated, at that point scene count is 0 so ignore that event
            if (SceneManager.sceneCount > 0)
            {
                treeViewReloadNeeded = true;
            }
        }

        public virtual void OnLostFocus()
        {
            // Added because this window uses RenameOverlay
            treeView.EndNameEditing(true);
        }

        public virtual void OnEnable()
        {
            Assert.IsTrue(m_EditorWindow != null, "Editor Window is null. It should survive assembly reload");

            EditorApplication.projectChanged += ReloadData; // Required to know if a prefab gets deleted. Better way of doing this?
            EditorApplication.editorApplicationQuit += OnQuit;
            EditorApplication.projectWasLoaded += OnProjectWasLoaded;
            EditorApplication.refreshHierarchy += Repaint;
            EditorApplication.dirtyHierarchySorting += DirtySortingMethods;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            m_AllowAlphaNumericalSort = EditorPrefs.GetBool("AllowAlphaNumericHierarchy", false) || !InternalEditorUtility.isHumanControllingUs; // Always allow alphasorting when running automated tests so we can test alpha sorting
            SetUpSortMethodLists();

            if (!AreCustomScenesValid(m_CustomScenes))
                m_CustomScenes = null;

            if (m_TreeViewKeyboardControlID == 0)
                m_TreeViewKeyboardControlID = EditorGUIUtility.GetPermanentControlID();
        }

        public virtual void OnDisable()
        {
            EditorApplication.projectChanged -= ReloadData;
            EditorApplication.editorApplicationQuit -= OnQuit;
            EditorApplication.projectWasLoaded -= OnProjectWasLoaded;
            EditorApplication.refreshHierarchy -= Repaint;
            EditorApplication.dirtyHierarchySorting -= DirtySortingMethods;
            EditorSceneManager.newSceneCreated -= OnSceneCreated;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
        }

        internal void OnProjectWasLoaded()
        {
            // Game objects will have new instanceIDs in a new Unity session, so clear
            // the expanded state on project start up (we still need this even though we also clear in OnQuit to be sure if loading a old layout)
            m_TreeViewState.expandedIDs.Clear();

            // If only one scene ensure it is expanded (new project)
            if (SceneManager.sceneCount == 1)
            {
                treeView.data.SetExpanded(SceneManager.GetSceneAt(0).handle, true);
            }

            // Ensure scenes are expanded from last session
            SetScenesExpanded(m_ExpandedScenes);
        }

        public virtual void OnQuit()
        {
            m_ExpandedScenes = GetExpandedSceneNames().ToList();

            // Game objects will have new instanceIDs in the next Unity session, so clear all state before serializing to layout to prevent saving redundant data
            m_TreeViewState = new TreeViewState();
        }

        public virtual void OnDestroy()
        {
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
                bool frame = !isLocked || m_FrameOnSelectionSync || userJustInteracted;
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

            if (evt.type == EventType.MouseDown && position.Contains(evt.mousePosition))
            {
                treeView.EndPing();
            }
        }

        public void OnGUI(Rect rect)
        {
            position = rect;

            OnEvent();
            SyncIfNeeded();
            DetectUserInteraction();

            float searchPathHeight = DoSearchResultPathGUI();
            DoTreeView(searchPathHeight);

            DoPingRequest();
            ExecuteCommands();
        }

        // TODO: Make sure it checks its own scenes: Here we assume we alw
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
                if (scene.isLoaded && !EditorSceneManager.IsPreviewScene(scene))
                    EditorSceneManager.SetActiveScene(scene);
            }
            else
            {
                SceneView.FrameLastActiveSceneView();
            }
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

        void OnRowGUICallback(int instanceID, Rect rect)
        {
            if (EditorApplication.hierarchyWindowItemOnGUI != null)
            {
                // Adjust rect for the right aligned column for the prefab isolation button
                rect.xMax -=
                    GameObjectTreeViewGUI.GameObjectStyles.rightArrow.fixedWidth +
                    GameObjectTreeViewGUI.GameObjectStyles.rightArrow.margin.horizontal;
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

        GameObjectTreeViewDataSource dataSource { get { return (GameObjectTreeViewDataSource)treeView.data; } }

        public void SetSearchFilter(string searchString, SearchableEditorWindow.SearchModeHierarchyWindow searchMode)
        {
            // Search didnt change do nothing.
            if (m_SearchMode == searchMode && m_SearchFilter == searchString)
                return;

            m_SearchMode = searchMode;
            m_SearchFilter = searchString;

            // Reload tree with new search data
            dataSource.searchMode = searchMode;
            dataSource.searchString = searchString;
            ReloadData();

            // If the user clears the search we frame the last selection he made during the search
            if (m_DidSelectSearchResult && string.IsNullOrEmpty(searchString))
            {
                m_DidSelectSearchResult = false;
                FrameObjectPrivate(Selection.activeInstanceID, true, false, false);

                // Ensure item has focus for visual feedback and instant key navigation
                if (GUIUtility.keyboardControl == 0)
                    GUIUtility.keyboardControl = m_TreeViewKeyboardControlID;
            }
        }

        /* public void SetSearch(string searchString, SearchableEditorWindow.SearchModeHierarchyWindow searchMode)
         {
             m_SearchFilter = searchString;
             m_SearchMode = searchMode;

             if (m_TreeView == null)
             {
                 Init();
             }
             else
             {
                 dataSource.searchString = searchString;
                 dataSource.searchMode = searchMode;
                 ReloadData();
             }
         }*/

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

        public void OnSelectionChange()
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

        public void OnHierarchyChange()
        {
            if (m_TreeView != null)
                m_TreeView.EndNameEditing(false);
            treeViewReloadNeeded = true;
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
            if (isLocked)
                m_FrameOnSelectionSync = true;
        }

        public void GameObjectCreateDropdownButton()
        {
            Rect rect = GUILayoutUtility.GetRect(Styles.createContent, EditorStyles.toolbarDropDown, null);
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbarDropDown.Draw(rect, Styles.createContent, false, false, false, false);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                GUIUtility.hotControl = 0;

                GenericMenu menu = new GenericMenu();
                var context = m_CustomParentForNewGameObjects != null ? new[] { m_CustomParentForNewGameObjects.gameObject } : null;
                var targetSceneHandle = m_CustomParentForNewGameObjects != null ? m_CustomParentForNewGameObjects.gameObject.scene.handle : kInvalidSceneHandle;
                AddCreateGameObjectItemsToMenu(menu, context, true, false, targetSceneHandle);
                menu.DropDown(rect);

                Event.current.Use();
            }
        }

        public void SortMethodsDropDownButton()
        {
            if (hasSortMethods)
            {
                // Labels button
                GUIContent content = m_SortingObjects[currentSortingName].content;
                if (content == null)
                {
                    content = Styles.defaultSortingContent;
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

        bool GetIsCustomParentSelected()
        {
            if (m_CustomParentForNewGameObjects == null)
                return false;

            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] == m_CustomParentForNewGameObjects.gameObject)
                    return true;
            }

            return false;
        }

        bool GetIsNotEditable()
        {
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if ((selected[i].hideFlags & HideFlags.NotEditable) != 0)
                    return true;
            }
            return false;
        }

        void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            bool execute = evt.type == EventType.ExecuteCommand;

            if (evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete)
            {
                if (execute && !GetIsCustomParentSelected())
                    DeleteGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Duplicate)
            {
                if (execute)
                    DuplicateGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Copy)
            {
                if (execute)
                    CopyGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Paste)
            {
                if (execute)
                    PasteGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.SelectAll)
            {
                if (execute)
                    SelectAll();
                evt.Use();
                GUIUtility.ExitGUI();
            }
        }

        void CreateGameObjectContextClick(GenericMenu menu, int contextClickedItemID)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Copy"), false, CopyGO);
            menu.AddItem(EditorGUIUtility.TrTextContent("Paste"), false, PasteGO);

            menu.AddSeparator("");
            // TODO: Add this back in.
            if (!hasSearchFilter && m_TreeViewState.selectedIDs.Count == 1 && !GetIsNotEditable())
                menu.AddItem(EditorGUIUtility.TrTextContent("Rename"), false, RenameGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Rename"));
            menu.AddItem(EditorGUIUtility.TrTextContent("Duplicate"), false, DuplicateGO);

            if (GetIsCustomParentSelected() || GetIsNotEditable())
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Delete"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Delete"), false, DeleteGO);

            menu.AddSeparator("");

            // Prefab menu items that only make sense if a single object is selected.
            if (m_TreeViewState.selectedIDs.Count == 1)
            {
                GameObjectTreeViewItem item = treeView.FindItem(m_TreeViewState.selectedIDs[0]) as GameObjectTreeViewItem;
                if (item != null && (item.objectPPTR as GameObject) != null)
                {
                    GameObject go = (GameObject)(item.objectPPTR);
                    string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    GameObject prefabAsset = (GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath);

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        if (PrefabUtility.IsPartOfModelPrefab(go))
                        {
                            menu.AddItem(EditorGUIUtility.TrTextContent("Open Model"), false, () =>
                            {
                                GameObject asset = PrefabUtility.GetOriginalSourceOrVariantRoot(go);
                                AssetDatabase.OpenAsset(asset);
                            });
                        }
                        else
                        {
                            menu.AddItem(EditorGUIUtility.TrTextContent("Open Prefab Asset"), false, () =>
                            {
                                PrefabStageUtility.OpenPrefab(assetPath, go, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyContextMenu);
                            });
                        }

                        menu.AddItem(EditorGUIUtility.TrTextContent("Select Prefab Asset"), false, () =>
                        {
                            Selection.activeObject = prefabAsset;
                            EditorGUIUtility.PingObject(prefabAsset.GetInstanceID());
                        });
                    }

                    if (PrefabUtility.IsAddedGameObjectOverride(go))
                    {
                        // Handle added GameObject or prefab.
                        Transform parentTransform = go.transform.parent;
                        PrefabUtility.HandleApplyRevertMenuItems(
                            "Added GameObject",
                            parentTransform.gameObject,
                            (menuItemContent, sourceGo) =>
                            {
                                TargetChoiceHandler.ObjectInstanceAndSourcePathInfo info = new TargetChoiceHandler.ObjectInstanceAndSourcePathInfo();
                                info.instanceObject = go;
                                info.assetPath = AssetDatabase.GetAssetPath(sourceGo);
                                GameObject rootGo = PrefabUtility.GetRootGameObject(sourceGo);
                                if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootGo))
                                    menu.AddDisabledItem(menuItemContent);
                                else
                                    menu.AddItem(menuItemContent, false, TargetChoiceHandler.ApplyPrefabAddedGameObject, info);
                            },
                            (menuItemContent) =>
                            {
                                menu.AddItem(menuItemContent, false, TargetChoiceHandler.RevertPrefabAddedGameObject, go);
                            }
                        );
                    }
                }
            }

            if (AnyOutermostPrefabRoots())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Unpack Prefab"), false, UnpackPrefab);
                menu.AddItem(EditorGUIUtility.TrTextContent("Unpack Prefab Completely"), false, UnpackPrefabCompletely);
            }

            GameObject[] selectedGameObjects = Selection.transforms.Select(t => t.gameObject).ToArray();

            // All Create GameObject menu items
            {
                menu.AddSeparator("");

                int targetSceneForCreation = selectedGameObjects.Length > 0 ? selectedGameObjects.Last().scene.handle : GetLastSceneInHierarchy().handle;

                // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
                // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
                AddCreateGameObjectItemsToMenu(menu, selectedGameObjects, false, false, targetSceneForCreation);
            }
            menu.ShowAsContext();
        }

        protected void AddCreateGameObjectItemsToSceneMenu(GenericMenu menu, Scene scene)
        {
            AddCreateGameObjectItemsToMenu(menu, Selection.transforms.Select(t => t.gameObject).ToArray(), false, true, scene.handle);
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
                var content = EditorGUIUtility.TrTextContent("Set Active Scene");
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
                    menu.AddItem(EditorGUIUtility.TrTextContent("Save Scene"), false, SaveSelectedScenes, contextClickedItemID);
                    menu.AddItem(EditorGUIUtility.TrTextContent("Save Scene As"), false, SaveSceneAs, contextClickedItemID);
                    if (hasMultipleScenes)
                        menu.AddItem(EditorGUIUtility.TrTextContent("Save All"), false, SaveAllScenes, contextClickedItemID);
                    else
                        menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Save All"));
                }
                else
                {
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Save Scene"));
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Save Scene As"));
                    menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Save All"));
                }
                menu.AddSeparator("");
            }

            bool isUnloadOrRemoveValid = EditorSceneManager.loadedSceneCount != GetNumLoadedScenesInSelection();

            if (scene.isLoaded)
            {
                // Unload
                var content = EditorGUIUtility.TrTextContent("Unload Scene");
                bool canUnloadScenes = isUnloadOrRemoveValid && !EditorApplication.isPlaying && !string.IsNullOrEmpty(scene.path);
                if (canUnloadScenes)
                    menu.AddItem(content, false, UnloadSelectedScenes, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
            }
            else
            {
                // Load
                var content = EditorGUIUtility.TrTextContent("Load Scene");
                bool canLoadScenes = !EditorApplication.isPlaying;
                if (canLoadScenes)
                    menu.AddItem(content, false, LoadSelectedScenes, contextClickedItemID);
                else
                    menu.AddDisabledItem(content);
            }

            // Remove
            var removeContent = EditorGUIUtility.TrTextContent("Remove Scene");
            bool selectedAllScenes = GetSelectedScenes().Count == EditorSceneManager.sceneCount;
            bool canRemoveScenes = isUnloadOrRemoveValid && !selectedAllScenes && !EditorApplication.isPlaying;
            if (canRemoveScenes)
                menu.AddItem(removeContent, false, RemoveSelectedScenes, contextClickedItemID);
            else
                menu.AddDisabledItem(removeContent);

            // Discard changes
            if (scene.isLoaded)
            {
                var content = EditorGUIUtility.TrTextContent("Discard changes");
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
            var selectAssetContent = EditorGUIUtility.TrTextContent("Select Scene Asset");
            if (!string.IsNullOrEmpty(scene.path))
                menu.AddItem(selectAssetContent, false, SelectSceneAsset, contextClickedItemID);
            else
                menu.AddDisabledItem(selectAssetContent);

            var addSceneContent = EditorGUIUtility.TrTextContent("Add New Scene");
            if (!EditorApplication.isPlaying)
                menu.AddItem(addSceneContent, false, AddNewScene, contextClickedItemID);
            else
                menu.AddDisabledItem(addSceneContent);

            // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
            // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
            if (scene.isLoaded)
            {
                menu.AddSeparator("");
                AddCreateGameObjectItemsToSceneMenu(menu, scene);
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

        List<int> GetSelectedGameObjects()
        {
            var selectedGameObjects = new List<int>();
            int[] instanceIDs = m_TreeView.GetSelection();
            foreach (int id in instanceIDs)
            {
                if (!IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(id)))
                {
                    selectedGameObjects.Add(id);
                }
            }
            return selectedGameObjects;
        }

        Scene GetLastSceneInHierarchy()
        {
            return dataSource.GetLastScene();
        }

        void ContextClickOutsideItems()
        {
            Event evt = Event.current;
            evt.Use();

            // Clear selection when clicking outside items so the new game object is
            // added to the last loaded scene in the Hierarchy (otherwise it would be parented
            // to current selcection)
            Selection.activeInstanceID = 0;

            var menu = new GenericMenu();

            CreateGameObjectContextClick(menu, 0);

            menu.ShowAsContext();
        }

        void ItemContextClick(int contextClickedItemID)
        {
            Event evt = Event.current;

            evt.Use();
            var menu = new GenericMenu();

            Scene scene = EditorSceneManager.GetSceneByHandle(contextClickedItemID);
            bool clickedSceneHeader = IsSceneHeaderInHierarchyWindow(scene);

            if (clickedSceneHeader)
            {
                CreateMultiSceneHeaderContextClick(menu, contextClickedItemID);
            }
            else
            {
                CreateGameObjectContextClick(menu, contextClickedItemID);
            }

            menu.ShowAsContext();
        }

        void CopyGO()
        {
            Unsupported.CopyGameObjectsToPasteboard();
        }

        void PasteGO()
        {
            bool customParentIsSelected = GetIsCustomParentSelected();
            Unsupported.PasteGameObjectsFromPasteboard();
            if (customParentIsSelected)
                Selection.activeTransform.SetParent(m_CustomParentForNewGameObjects, true);
        }

        void DuplicateGO()
        {
            bool customParentIsSelected = GetIsCustomParentSelected();
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            if (customParentIsSelected)
                Selection.activeTransform.SetParent(m_CustomParentForNewGameObjects, true);
        }

        void RenameGO()
        {
            treeView.BeginNameEditing(0f);
        }

        void DeleteGO()
        {
            Unsupported.DeleteGameObjectSelection();
        }

        bool AnyOutermostPrefabRoots()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var go = gameObjects[i];
                if (go != null && PrefabUtility.IsPartOfNonAssetPrefabInstance(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    return true;
            }
            return false;
        }

        void UnpackPrefab()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var go = gameObjects[i];
                if (go != null && PrefabUtility.IsPartOfNonAssetPrefabInstance(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction);
            }
        }

        void UnpackPrefabCompletely()
        {
            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                var go = gameObjects[i];
                if (go != null && PrefabUtility.IsPartOfNonAssetPrefabInstance(go) && PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }
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
            string title = LocalizationDatabase.GetLocalizedString("Discard Changes");
            string message = LocalizationDatabase.GetLocalizedString("Are you sure you want to discard the changes in the following scenes:\n\n   {0}\n\nYour changes will be lost.");

            string sceneNames = string.Join("\n   ", modifiedScenes.Select(scene => scene.name).ToArray());
            message = string.Format(message, sceneNames);

            return EditorUtility.DisplayDialog(title, message, LocalizationDatabase.GetLocalizedString("OK"), LocalizationDatabase.GetLocalizedString("Cancel"));
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
                var title = EditorGUIUtility.TrTextContent("Save Untitled Scene").text;
                var subTitle = EditorGUIUtility.TrTextContent("Existing Untitled scene needs to be saved before creating a new scene. Only one untitled scene is supported at a time.").text;
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
            var sceneObject = AssetDatabase.LoadMainAssetAtPath(scene.path);
            Selection.activeObject = sceneObject;
            EditorGUIUtility.PingObject(sceneObject);
        }

        private void SelectAll()
        {
            int[] instanceIDs = treeView.GetRowIDs();
            treeView.SetSelection(instanceIDs, false);
            TreeViewSelectionChanged(instanceIDs);
        }

        public virtual void AddItemsToWindowMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu);
            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Hierarchy "), s_Debug, () => s_Debug = !s_Debug);
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Prefab Scene"), s_DebugPrefabStage, () => s_DebugPrefabStage = !s_DebugPrefabStage);
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Expanded State Persistence"), s_DebugPersistingExpandedState, () => s_DebugPersistingExpandedState = !s_DebugPersistingExpandedState);
            }
        }

        private void DoPingRequest()
        {
            if (m_FrameRequestID != 0)
            {
                FrameObjectPrivate(m_FrameRequestID, true, m_FrameRequestPing, true);
                m_FrameRequestID = 0;
            }
        }

        public void FrameObject(int instanceID, bool ping)
        {
            m_FrameRequestID = instanceID;
            m_FrameRequestPing = ping;
            treeViewReloadNeeded = true;
            Repaint();
        }

        private void FrameObjectPrivate(int instanceID, bool frame, bool ping, bool animatedFraming)
        {
            if (instanceID == 0)
                return;

            // If framing the same instance as the last one we do not remove the ping
            // since issuing first a ping and then a framing should still show the ping.
            if (m_LastFramedID != instanceID)
                treeView.EndPing();

            m_LastFramedID = instanceID;

            treeView.Frame(instanceID, frame, ping, animatedFraming);

            FrameObjectPrivate(InternalEditorUtility.GetGameObjectInstanceIDFromComponent(instanceID), frame, ping, animatedFraming);
        }

        internal virtual void DoWindowLockButton(Rect r)
        {
            m_LockTracker.ShowButton(r, Styles.lockButton);
        }

        float DoSearchResultPathGUI()
        {
            if (!hasSearchFilter)
                return 0;

            const float rowHeight = 18f;
            const float minTreeViewHeight = 32;
            float treeViewHeight = treeViewRect.height;

            var names = m_ParentNamesForSelectedSearchResult;
            names.Clear();
            names.Add("Path:");

            if (m_TreeView.HasSelection())
            {
                int selectedID = m_TreeView.GetSelection()[0];
                TransformPath.AddGameObjectNames(selectedID, names);

                float maxHeight = Math.Max(0, treeViewHeight - minTreeViewHeight);
                int maxNumVisibleRows = Mathf.FloorToInt(maxHeight / rowHeight);
                int removeRows = names.Count - maxNumVisibleRows;
                if (removeRows > 0)
                {
                    int index = names.Count / 2 - removeRows / 2;
                    bool addElipsis = maxNumVisibleRows >= 2;
                    names.RemoveRange(index, removeRows + (addElipsis ? 1 : 0));
                    if (addElipsis)
                        names.Insert(index, "...");
                }
            }

            float height = names.Count * rowHeight;

            Rect backgroundRect = treeViewRect;
            backgroundRect.yMin += treeViewHeight - height;
            GUI.Label(backgroundRect, GUIContent.none, EditorStyles.inspectorBig);

            Rect rowRect = treeViewRect;
            rowRect.xMin += 10f;
            rowRect.height = rowHeight;
            rowRect.y = treeViewRect.yMax - height + (rowHeight - EditorGUI.kSingleLineHeight) / 2;
            for (int i = 0; i < names.Count; ++i)
            {
                GUI.Label(rowRect, names[i]);
                rowRect.y += rowHeight;
                if (i == 0)
                    rowRect.xMin += 10f;
            }

            return height;
        }

        static class TransformPath
        {
            public static void AddGameObjectNames(int gameObjectInstanceID, List<string> list)
            {
                var gameObject = InternalEditorUtility.GetObjectFromInstanceID(gameObjectInstanceID) as GameObject;
                if (gameObject == null)
                    return;

                AddGameObjectNames(gameObject, list);
            }

            public static void AddGameObjectNames(GameObject target, List<string> list)
            {
                Transform transform = target.transform;
                while (transform != null)
                {
                    list.Add(transform.gameObject.name);
                    transform = transform.parent;
                }
            }
        }
    }
}
