// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor.IMGUI.Controls;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine.Assertions;
using System.Text;
using UnityEngine.UIElements;
using TreeViewController = UnityEditor.IMGUI.Controls.TreeViewController<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewDataSource = UnityEditor.IMGUI.Controls.TreeViewDataSource<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

namespace UnityEditor
{
    [Serializable]
    internal class SceneHierarchy
    {
        string kContextMenuItemCut { get { return "Cut " + Menu.GetHotkey("Edit/Cut"); } }
        string kContextMenuItemCopy { get { return "Copy " + Menu.GetHotkey("Edit/Copy"); } }
        string kContextMenuItemPaste { get { return "Paste " + Menu.GetHotkey("Edit/Paste"); } }
        string kContextMenuItemDuplicate { get { return "Duplicate " + Menu.GetHotkey("Edit/Duplicate"); } }
        string kContextMenuItemDelete { get { return "Delete " + Menu.GetHotkey("Edit/Delete"); } }
        string kContextMenuItemPasteAsChildKeepLocalTransform { get { return "Paste Special/Paste as Child (Keep Local Transform) " + Menu.GetHotkey("Edit/Paste Special/Paste as Child (Keep Local Transform)"); } }

        static class Styles
        {
            const string kCustomSorting = "CustomSorting";
            const string kWarningSymbol = "console.warnicon.sml";
            const string kWarningMessage = "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.";

            public static GUIContent defaultSortingContent = EditorGUIUtility.TrIconContent(kCustomSorting);
            public static GUIContent createContent = EditorGUIUtility.IconContent("CreateAddNew");
            public static GUIContent fetchWarning = new GUIContent("", EditorGUIUtility.FindTexture(kWarningSymbol), kWarningMessage);

            public static GUIStyle lockButton = "IN LockButton";

            public static GUIContent renamingEnabledContent = EditorGUIUtility.TrTextContent("Rename New Objects");
            public static GUIContent setOriginLabel = new GUIContent("Set as Default Parent");
            public static GUIContent clearOriginLabel = new GUIContent("Clear Default Parent");
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
        bool m_RectSelectInProgress;

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
        [NonSerialized]
        bool m_IgnoreNextHierarchyChangedEvent;

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

        internal static bool s_DebugPreviewScenes
        {
            get { return EditorSceneManager.GetPreviewScenesVisibleInHierarchy(); }
            set
            {
                EditorSceneManager.SetPreviewScenesVisibleInHierarchy(value);
                SceneHierarchyWindow.ReloadAllHierarchyWindows();
            }
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

        public bool hasSortMethods { get { return m_SortingObjects != null && m_SortingObjects.Count > 1; } }

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

        internal TreeViewController treeView
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
            m_TreeViewState.searchString = ""; // search string is controlled by m_SearchFilter, clearing old layouts, case 1346524

            if (m_SortingObjects == null)
                SetUpSortMethodLists();

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

            dataSource.searchMode = m_SearchMode;
            dataSource.searchString = m_SearchFilter;
            dataSource.scenes = m_CustomScenes;
            dataSource.sortingState = m_SortingObjects[m_CurrentSortingName];
            dragging.parentForDraggedObjectsOutsideItems = m_CustomParentForNewGameObjects;
            dragging.SetCustomDragHandler(m_CustomDragHandler);
            gui.renameEnded += ItemRenameEnded;

            m_TreeView.Init(treeViewRect, dataSource, gui, dragging);
            m_TreeView.ReloadData();
        }

        void ItemRenameEnded(bool userAcceptedRename, int itemID, string name, string originalName)
        {
            if (userAcceptedRename && name != originalName)
            {
                // Handle reloading immediately when an internal change happens instead of waiting for
                // the delayed OnHierarchyChange event (fixes case 981190)
                ObjectNames.SetNameSmartWithInstanceID(itemID, name);
                m_IgnoreNextHierarchyChangedEvent = true;
                ReloadData();

                EditorApplication.RepaintAnimationWindow();
            }
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

        void PlayModeStateChanged(PlayModeStateChange state)
        {
            treeViewReloadNeeded = true;
        }

        void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            ClearSearchSessionIfAny();
            ExpandTreeViewItem(scene.handle, true);
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ClearSearchSessionIfAny();
            ExpandTreeViewItem(scene.handle, true);
        }

        void ClearSearchSessionIfAny()
        {
            var dataSource = treeView.data as GameObjectTreeViewDataSource;
            if (dataSource != null && !string.IsNullOrEmpty(dataSource.searchString))
            {
                var oldSearch = dataSource.searchString;
                dataSource.searchString = "";
                dataSource.searchString = oldSearch;
            }
        }

        internal void ExpandTreeViewItem(int id, bool expand)
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
            // On lost focus can be called before OnEnable have been called
            if (m_TreeView != null)
            {
                // Added because this window uses RenameOverlay
                m_TreeView.EndNameEditing(true);
            }
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
            RectSelection.rectSelectionStarting += SceneViewRectSelectionStarting;
            RectSelection.rectSelectionFinished += SceneViewRectSelectionFinished;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

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
            RectSelection.rectSelectionStarting -= SceneViewRectSelectionStarting;
            RectSelection.rectSelectionFinished -= SceneViewRectSelectionFinished;
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
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

        void SceneViewRectSelectionStarting()
        {
            m_RectSelectInProgress = true;
        }

        void SceneViewRectSelectionFinished()
        {
            m_RectSelectInProgress = false;
            selectionSyncNeeded = true;
            SyncIfNeeded();
            Repaint();
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
                bool frame = (!isLocked || m_FrameOnSelectionSync || userJustInteracted) && !m_RectSelectInProgress;
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

            DoSceneVisibilityBackgroundOverflow(searchPathHeight);

            DoPingRequest();
            ExecuteCommands();
            HandleKeyboard();
        }

        // TODO: Make sure it checks its own scenes: Here we assume we alw
        public static bool IsSceneHeaderInHierarchyWindow(Scene scene)
        {
            return scene.IsValid();
        }

        void TreeViewItemDoubleClicked(int instanceID)
        {
            bool setActiveScene = false;

            Scene scene = EditorSceneManager.GetSceneByHandle(instanceID);
            if (IsSceneHeaderInHierarchyWindow(scene))
            {
                setActiveScene = true;
            }
            else if (SubSceneGUI.IsUsingSubScenes())
            {
                var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject != null && SubSceneGUI.IsSubSceneHeader(gameObject))
                {
                    scene = SubSceneGUI.GetSubScene(gameObject);
                    setActiveScene = true;
                }
            }

            if (setActiveScene)
            {
                // scene header selected
                if (scene.isLoaded && SceneManager.CanSetAsActiveScene(scene))
                    SceneManager.SetActiveScene(scene);
            }
            else
            {
                SceneView.FrameLastActiveSceneView();
                EditorGUIUtility.ExitGUI();
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

        void OnRowGUICallback(int itemID, Rect rect)
        {
            GameObjectTreeViewGUI.UserCallbackRowGUI(itemID, rect);
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
                GameObjectTreeViewGUI.RemoveInvalidActiveParentObjects();
                Repaint();
                GUIUtility.ExitGUI();
            }
        }

        bool IsRenaming()
        {
            return m_TreeView != null && m_TreeView.state.renameOverlay.IsRenaming();
        }

        public void ReloadData()
        {
            int rowCountBeforeReloading = 0;
            if (IsRenaming())
                rowCountBeforeReloading = m_TreeView.data.rowCount;

            if (m_TreeView == null)
                Init();
            else
                m_TreeView.ReloadData();

            if (IsRenaming())
            {
                // Ensure renaming is ended if the item being renamed is no longer part
                // of the rows or if the row count changed (new objects deleted/added).
                // If the renamed item is present then ensure it is visible to the user.
                int id = m_TreeView.state.renameOverlay.userData;
                bool endRenaming = m_TreeView.data.rowCount != rowCountBeforeReloading;
                if (!endRenaming)
                    endRenaming = m_TreeView.data.GetRows().FirstOrDefault(item => item.id == id) == null;
                if (endRenaming)
                    m_TreeView.EndNameEditing(false);
                else
                    m_TreeView.Frame(id, true, false);
            }
        }

        GameObjectTreeViewDataSource dataSource { get { return (GameObjectTreeViewDataSource)treeView.data; } }
        GameObjectsTreeViewDragging dragging { get { return (GameObjectsTreeViewDragging)treeView.dragging; } }

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

        void TreeViewSelectionChanged(int[] ids)
        {
            //Last selected should be the active selected object to reflect the behavior of the scene view selection
            int active = ids.Length > 0 ? ids[ids.Length - 1] : 0;
            Selection.SetSelectionWithActiveInstanceID(ids, active);

            if (!IsTreeViewSelectionInSyncWithBackend())
            {
                selectionSyncNeeded = true;
            }

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
            if (m_IgnoreNextHierarchyChangedEvent)
            {
                m_IgnoreNextHierarchyChangedEvent = false;
                return;
            }

            treeViewReloadNeeded = true;
        }

        void OnEvent()
        {
            treeView.OnEvent();
        }

        // Tree view item handle their own scene visibility background.
        // So the background doesn't stop if we don't have enough item to fill the entire tree view rect, we draw the background in the unused space.
        private void DoSceneVisibilityBackgroundOverflow(float reservedFooterSpace)
        {
            float treeViewHeight = treeView.gui.GetTotalSize().y;
            Rect rectWithNoRows = treeViewRect;

            //If the tree view already covers the entire rect, we don't need to fill the overflow
            if (rectWithNoRows.height <= treeViewHeight)
                return;

            rectWithNoRows.yMin += treeViewHeight;
            rectWithNoRows.height -= reservedFooterSpace;

            SceneVisibilityHierarchyGUI.DrawBackground(rectWithNoRows);
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
        void AddCreateGameObjectItemsToMenu(GenericMenu menu, UnityEngine.Object[] context, bool includeCreateEmptyChild, bool useCreateEmptyParentMenuItem, bool includeGameObjectInPath, int targetSceneHandle, MenuUtils.ContextMenuOrigin origin)
        {
            ScriptingMenuItem[] menus = Menu.GetMenuItems("GameObject", true, false);
            int previousMenuItemPosition = -1;

            foreach (var menuItem in menus)
            {
                string path = menuItem.path;

                string hotkey = Menu.GetHotkey(menuItem.path);

                UnityEngine.Object[] tempContext = context;
                if (!includeCreateEmptyChild && path.ToLower() == "GameObject/Create Empty Child".ToLower())
                    continue;

                if (!useCreateEmptyParentMenuItem && path.ToLower() == "GameObject/Create Empty Parent".ToLower())
                {
                    if (GOCreationCommands.ValidateCreateEmptyParent())
                        menu.AddItem(EditorGUIUtility.TrTextContent("Create Empty Parent " + hotkey), false, GOCreationCommands.CreateEmptyParent);
                    continue;
                }

                // The first item after the GameObject creation menu items
                if (path.ToLower() == GameObjectUtility.GetFirstItemPathAfterGameObjectCreationMenuItems().ToLower())
                    continue;

                string menupath = path;

                // cut away "GameObject/"
                if (!includeGameObjectInPath)
                    menupath = path.Substring(11);

                if (!string.IsNullOrEmpty(hotkey))
                    menupath += " " + hotkey;

                MenuUtils.ExtractOnlyEnabledMenuItem(menuItem,
                    menu,
                    menupath,
                    tempContext,
                    targetSceneHandle,
                    BeforeCreateGameObjectMenuItemWasExecuted,
                    AfterCreateGameObjectMenuItemWasExecuted,
                    origin,
                    previousMenuItemPosition);

                previousMenuItemPosition = menuItem.priority;
            }

            MenuUtils.RemoveInvalidMenuItems(menu);
        }

        void BeforeCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, MenuUtils.ContextMenuOrigin origin, int userData)
        {
            int sceneHandle = userData;
            if (origin == MenuUtils.ContextMenuOrigin.Scene || origin == MenuUtils.ContextMenuOrigin.Subscene)
                GOCreationCommands.forcePlaceObjectsAtWorldOrigin = true;
            EditorSceneManager.SetTargetSceneForNewGameObjects(sceneHandle);
        }

        void AfterCreateGameObjectMenuItemWasExecuted(string menuPath, UnityEngine.Object[] contextObjects, MenuUtils.ContextMenuOrigin origin, int userData)
        {
            EditorSceneManager.SetTargetSceneForNewGameObjects(kInvalidSceneHandle);
            GOCreationCommands.forcePlaceObjectsAtWorldOrigin = false;
            // Ensure framing when creating game objects even if we are locked
            if (isLocked)
                m_FrameOnSelectionSync = true;
        }

        public void GameObjectCreateDropdownButton()
        {
            Rect rect = GUILayoutUtility.GetRect(Styles.createContent, EditorStyles.toolbarCreateAddNewDropDown, null);
            bool mouseOver = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbarCreateAddNewDropDown.Draw(rect, Styles.createContent, mouseOver, false, false, false);

            if (Event.current.type == EventType.MouseDown && mouseOver)
            {
                GUIUtility.hotControl = 0;

                GenericMenu menu = new GenericMenu();
                var targetSceneHandle = m_CustomParentForNewGameObjects != null ? m_CustomParentForNewGameObjects.gameObject.scene.handle : kInvalidSceneHandle;
                // The context should be null, just like it is in the main menu. Case 1185434.
                AddCreateGameObjectItemsToMenu(menu, null, true, true, false, targetSceneHandle, MenuUtils.ContextMenuOrigin.Toolbar);

                SceneHierarchyHooks.AddCustomItemsToCreateMenu(menu);

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

                Rect r = GUILayoutUtility.GetRect(content, EditorStyles.toolbarButtonRight);
                if (EditorGUI.DropdownButton(r, content, FocusType.Passive, EditorStyles.toolbarButtonRight))
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

        static bool IsChildOfSelectionOrSelected(Transform transform)
        {
            if (transform == null)
                return false;

            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                var current = transform;
                while (current != null)
                {
                    if (selected[i] == current.gameObject)
                        return true;
                    current = current.parent;
                }
            }

            return false;
        }

        void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
            {
                return;
            }

            bool execute = evt.type == EventType.ExecuteCommand;
            // If the custom parent for new objects is set we don't allow cut, copy or duplicate any ancestors of this object
            // as it might be context objects (as in Prefab Mode in Context).
            bool allowCutCopyAndDuplicate = m_CustomParentForNewGameObjects == null
                                            || !IsChildOfSelectionOrSelected(m_CustomParentForNewGameObjects.parent);

            if (evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete)
            {
                if (execute && !IsChildOfSelectionOrSelected(m_CustomParentForNewGameObjects))
                    DeleteGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Duplicate)
            {
                if (execute && allowCutCopyAndDuplicate)
                    DuplicateGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Rename)
            {
                if (execute)
                    RenameGO();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Cut)
            {
                if (allowCutCopyAndDuplicate)
                    ClipboardUtility.CutGO();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.Copy)
            {
                if (execute && allowCutCopyAndDuplicate)
                    ClipboardUtility.CopyGO();
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
            else if (evt.commandName == EventCommandNames.DeselectAll)
            {
                if (execute)
                    DeselectAll();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.InvertSelection)
            {
                if (execute)
                    InvertSelection();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.SelectChildren)
            {
                if (execute)
                    SelectChildren();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.SelectPrefabRoot)
            {
                if (execute)
                    SelectPrefabRoot();
                evt.Use();
                GUIUtility.ExitGUI();
            }
            else if (evt.commandName == EventCommandNames.UndoRedoPerformed)
            {
                ReloadData();
                GUIUtility.ExitGUI();
            }
        }

        void HandleKeyboard()
        {
            Event evt = Event.current;

            if (evt.keyCode == KeyCode.Escape && CutBoard.CanGameObjectsBePasted())
            {
                ClipboardUtility.ResetCutboardAndRepaintHierarchyWindows();
                GUIUtility.ExitGUI();
            }
        }

        void CreateSubSceneGameObjectContextClick(GenericMenu menu, int contextClickedItemID)
        {
            // For Sub Scenes GameObjects, have menu items for cut, paste and delete.
            // Not copy or duplicate, since multiple of the same Sub Scene is not supported anyway.

            menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemCut), false, ClipboardUtility.CutGO);
            if (CutBoard.CanGameObjectsBePasted() || Unsupported.CanPasteGameObjectsFromPasteboard())
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemPaste), false, PasteGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemPaste));

            if (ClipboardUtility.CanPasteAsChild())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemPasteAsChildKeepLocalTransform), false, () => ClipboardUtility.PasteGOAsChild(false));
                menu.AddItem(EditorGUIUtility.TrTextContent("Paste as Child (Keep World Transform)"), false, () => ClipboardUtility.PasteGOAsChild(true));
            }
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Special"));

            menu.AddSeparator("");

            if (IsChildOfSelectionOrSelected(m_CustomParentForNewGameObjects))
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Delete GameObject"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Delete GameObject"), false, DeleteGO);
        }

        void PopulateGenericMenuWithPrefabMenuItems(GenericMenu menu, int contextClickedItemID)
        {
            GameObject go = null;
            string assetPath = null;
            GameObject prefabAsset = null;
            if (m_TreeViewState.selectedIDs.Count == 1)
            {
                GameObjectTreeViewItem item = treeView.FindItem(m_TreeViewState.selectedIDs[0]) as GameObjectTreeViewItem;
                if (item != null && (item.objectPPTR as GameObject) != null)
                {
                    go = (GameObject)(item.objectPPTR);
                    assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                    prefabAsset = (GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath);
                }
            }

            var contextClickedGameObject = contextClickedItemID == 0 ? null : EditorUtility.InstanceIDToObject(contextClickedItemID) as GameObject;
            var isSelectPrefabRootAvailable = IsSelectPrefabRootAvailable();

            if (contextClickedGameObject != null || !string.IsNullOrEmpty(assetPath))
                menu.AddSeparator("");

            if (!string.IsNullOrEmpty(assetPath))
            {
                if (PrefabUtility.IsPartOfModelPrefab(prefabAsset))
                {
                    menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Open Model"), false, () =>
                    {
                        AssetDatabase.OpenAsset(prefabAsset);
                    });
                }
                else
                {
                    menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Open Asset in Context"), false, () =>
                    {
                        PrefabStageUtility.OpenPrefab(assetPath, go, PrefabStage.Mode.InContext, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyContextMenu);
                    });
                    menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Open Asset in Isolation"), false, () =>
                    {
                        PrefabStageUtility.OpenPrefab(assetPath, go, PrefabStage.Mode.InIsolation, StageNavigationManager.Analytics.ChangeType.EnterViaInstanceHierarchyContextMenu);
                    });
                }
            }

            if (!string.IsNullOrEmpty(assetPath))
            {
                menu.AddSeparator("Prefab/");
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Select Asset"), false, () =>
                {
                    Selection.activeObject = prefabAsset;
                    EditorGUIUtility.PingObject(prefabAsset.GetInstanceID());
                });
            }

            if (isSelectPrefabRootAvailable)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Select Root"), false, SelectPrefabRoot);
            }

            GameObject sourceRoot = GetSourceRootWhereGameObjectIsAddedAsOverride(go);
            if (sourceRoot != null)
            {
                var s = PrefabUtility.GetOriginalSourceRootWhereGameObjectIsAdded(go);
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Go to Added GameObject in '" + sourceRoot.name + "'"), false, () =>
                {
                    PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(sourceRoot), PrefabUtility.GetNearestPrefabInstanceRoot(go), PrefabStage.Mode.InIsolation);
                });
            }

            GameObject[] selectedGOs = Selection.gameObjects;

            if (selectedGOs.Any() && PrefabUtility.IsAllAddedGameObjectOverrides(selectedGOs))
            {
                go = selectedGOs[0];
                // Handle added GameObject or prefab.
                Transform parentTransform = go.transform.parent;
                PrefabUtility.HandleApplyRevertMenuItems(
                    (selectedGOs.Length > 1) ? "Added GameObjects" : "Added GameObject",
                    parentTransform.gameObject,
                    (menuItemContent, sourceGo, _) =>
                    {
                        GameObject rootGo = PrefabUtility.GetRootGameObject(sourceGo);
                        if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootGo) || EditorUtility.IsPersistent(parentTransform) || !PrefabUtility.HasSameParent(selectedGOs))
                            menu.AddDisabledItem(menuItemContent);
                        else
                        {
                            string assetPath = AssetDatabase.GetAssetPath(sourceGo);
                            TargetChoiceHandler.ObjectInstanceAndSourcePathInfo[] childInfos = new TargetChoiceHandler.ObjectInstanceAndSourcePathInfo[selectedGOs.Length];
                            for (int i = 0; i < selectedGOs.Length; i++)
                            {
                                GameObject go = selectedGOs[i];
                                TargetChoiceHandler.ObjectInstanceAndSourcePathInfo info = new TargetChoiceHandler.ObjectInstanceAndSourcePathInfo();
                                info.instanceObject = go;
                                info.assetPath = assetPath;
                                childInfos[i] = info;
                            }

                            menu.AddItem(menuItemContent, false, TargetChoiceHandler.ApplyPrefabAddedGameObjects, childInfos);
                        }
                    },
                    (menuItemContent) =>
                    {
                        menu.AddItem(menuItemContent, false, TargetChoiceHandler.RevertPrefabAddedGameObjects, selectedGOs);
                    }
                );
            }

            if (contextClickedGameObject != null)
            {
                menu.AddSeparator("Prefab/");
                List<GameObject> listOfInstanceRoots;
                List<GameObject> listOfPlainGameObjects;
                PrefabReplaceUtility.FindGameObjectsToReplace(contextClickedGameObject, out listOfPlainGameObjects, out listOfInstanceRoots);

                var multiselection = listOfInstanceRoots.Count > 1 || listOfPlainGameObjects.Count > 1;

                PrefabReplaceUtility.AddReplaceMenuItemsToMenuBasedOnCurrentSelection(menu, "Prefab/", contextClickedGameObject, listOfInstanceRoots, listOfPlainGameObjects, null);
            }

            if (AnyOutermostPrefabRoots())
            {
                menu.AddSeparator("Prefab/");
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Unpack"), false, UnpackPrefab);
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Unpack Completely"), false, UnpackPrefabCompletely);
                menu.AddSeparator("Prefab/");
                menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Remove Unused Overrides..."), false, RemoveSelectedPrefabInstanceUnusedOverrides);
            }
        }

        void CreateGameObjectContextClick(GenericMenu menu, int contextClickedItemID)
        {
            bool itemIsSelected = Selection.gameObjects.Length > 0;
            // If the custom parent for new objects is set we don't allow cut, copy or duplicate any ancestors of this object
            // as it might be context objects (as in Prefab Mode in Context).
            bool allowCutCopyAndDuplicate = m_CustomParentForNewGameObjects == null
                                            || !IsChildOfSelectionOrSelected(m_CustomParentForNewGameObjects.parent);

            if (itemIsSelected && allowCutCopyAndDuplicate)
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemCut), false, ClipboardUtility.CutGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemCut));
            if (itemIsSelected && allowCutCopyAndDuplicate)
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemCopy), false, ClipboardUtility.CopyGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemCopy));
            if (CutBoard.CanGameObjectsBePasted() || Unsupported.CanPasteGameObjectsFromPasteboard())
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemPaste), false, PasteGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemPaste));
            if (ClipboardUtility.CanPasteAsChild())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemPasteAsChildKeepLocalTransform), false, () => ClipboardUtility.PasteGOAsChild(false));
                menu.AddItem(EditorGUIUtility.TrTextContent("Paste Special/Paste as Child (Keep World Transform)"), false, () => ClipboardUtility.PasteGOAsChild(true));
            }
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Paste Special"));

            if (itemIsSelected && !hasSearchFilter && m_TreeViewState.selectedIDs.Count == 1 && !GetIsNotEditable())
                menu.AddItem(EditorGUIUtility.TrTextContent("Rename"), false, RenameGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Rename"));

            if (itemIsSelected && allowCutCopyAndDuplicate)
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemDuplicate), false, DuplicateGO);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemDuplicate));

            if (m_CustomParentForNewGameObjects != null && IsChildOfSelectionOrSelected(m_CustomParentForNewGameObjects) || !itemIsSelected)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(kContextMenuItemDelete));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent(kContextMenuItemDelete), false, DeleteGO);


            menu.AddSeparator("");

            menu.AddItem(EditorGUIUtility.TrTextContent("Select All"), false, SelectAll);

            if (itemIsSelected)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Deselect All"), false, DeselectAll);
                menu.AddItem(EditorGUIUtility.TrTextContent("Invert Selection"), false, InvertSelection);
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Deselect All"));
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Invert Selection"));
            }

            if (IsSelectChildrenAvailable())
                menu.AddItem(EditorGUIUtility.TrTextContent("Select Children"), false, SelectChildren);
            else
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Select Children"));

            menu.AddSeparator("");
            if (Selection.activeGameObject)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Find References in Scene"), false, FindReferenceInScene);
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Find References in Scene"));
            }

            menu.AddSeparator("");

            GameObject selectedObject = null;
            if (Selection.objects.Length > 0)
                selectedObject = Selection.objects[Selection.objects.Length - 1] as GameObject;

            if (Selection.count == 0 && treeView.hoveredItem == null || selectedObject && (selectedObject.name == PrefabUtility.kDummyPrefabStageRootObjectName || PrefabStageUtility.IsGameObjectThePrefabRootInAnyPrefabStage(selectedObject)))
            {
                menu.AddDisabledItem(Styles.setOriginLabel);
            }
            else if (selectedObject && (selectedObject.GetInstanceID() != GetDefaultParentForSession(selectedObject.scene.guid) || EditorSceneManager.GetActiveScene().guid != selectedObject.scene.guid))
            {
                menu.AddItem(Styles.setOriginLabel, false, () =>
                {
                    SetDefaultParentObject(false);
                });
            }
            else
            {
                menu.AddItem(Styles.clearOriginLabel, false, () => { ClearDefaultParentObject(); });
            }

            // Prefab menu items that only make sense if a single object is selected.
            PopulateGenericMenuWithPrefabMenuItems(menu, contextClickedItemID);

            GameObject[] selectedGameObjects = Selection.transforms.Select(t => t.gameObject).ToArray();

            // All Create GameObject menu items
            {
                menu.AddSeparator("");

                int targetSceneForCreation = selectedGameObjects.Length > 0
                    ? selectedGameObjects.Last().scene.handle
                    : SceneManager.GetActiveScene().handle;

                // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
                // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
                AddCreateGameObjectItemsToMenu(menu,
                    selectedGameObjects,
                    false,
                    false,
                    false,
                    targetSceneForCreation,
                    contextClickedItemID == 0
                    ? MenuUtils.ContextMenuOrigin.None
                    : MenuUtils.ContextMenuOrigin.GameObject);
            }

            SceneHierarchyHooks.AddCustomGameObjectContextMenuItems(menu, contextClickedItemID == 0 ? null : (GameObject)EditorUtility.InstanceIDToObject(contextClickedItemID));

            if (selectedGameObjects.Length > 0)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Properties..."), false, () => PropertyEditor.OpenPropertyEditorOnSelection());
            }
        }

        internal void PopulateDropdownMenuWithPrefabMenuItems(DropdownMenu menu)
        {
            if (m_TreeViewState.selectedIDs.Count != 1) return;

            int selectedGameObjectInstanceID = m_TreeViewState.selectedIDs[0];
            GenericMenu genericMenuForGettingPrefabItems = new GenericMenu();
            PopulateGenericMenuWithPrefabMenuItems(genericMenuForGettingPrefabItems, selectedGameObjectInstanceID);

            for (int i = 0, prefabMenuItemsCount = genericMenuForGettingPrefabItems.menuItems.Count; i < prefabMenuItemsCount; ++i)
            {
                GenericMenu.MenuItem menuItem = genericMenuForGettingPrefabItems.menuItems[i];
                if (menuItem.separator)
                    menu.AppendSeparator(menuItem.content.text);
                else if (menuItem.func2 != null && menuItem.userData != null)
                    menu.AppendAction(menuItem.content.text, action => { menuItem.func2(menuItem.userData); });
                else if (menuItem.func != null)
                    menu.AppendAction(menuItem.content.text, action => { menuItem.func(); });
                else
                    menu.AppendAction(menuItem.content.text, action => { }, DropdownMenuAction.Status.Disabled);
            }
        }

        private void FindReferenceInScene()
        {
            var selectedObject = Selection.activeObject;
            if (!selectedObject)
                return;
            SearchableEditorWindow.SearchForReferencesToInstanceID(selectedObject.GetInstanceID());
        }

        private void FindReferenceInProject()
        {
            var selectedObject = Selection.activeObject;
            if (!selectedObject)
                return;
            SearchableEditorWindow.SearchForReferencesInProject(selectedObject);
        }

        protected void AddCreateGameObjectItemsToSceneMenu(GenericMenu menu, Scene scene)
        {
            AddCreateGameObjectItemsToMenu(menu, Selection.transforms.Select(t => t.gameObject).ToArray(), false, false, true, scene.handle, MenuUtils.ContextMenuOrigin.Scene);
        }

        protected void AddCreateGameObjectItemsToSubSceneMenu(GenericMenu menu, Scene scene)
        {
            AddCreateGameObjectItemsToMenu(menu, new UnityEngine.Object[0], false, true, true, scene.handle, MenuUtils.ContextMenuOrigin.Subscene);
        }

        internal bool CanScenesBeReloaded(Scene[] scenes)
        {
            foreach (var scene in scenes)
            {
                var path = scene.path;
                if (string.IsNullOrEmpty(path))
                    return false;

                if (!System.IO.File.Exists(path))
                    return false;
            }
            return true;
        }

        internal void CreateSceneHeaderContextClick(GenericMenu menu, Scene scene)
        {
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
                if (hasMultipleScenes && SceneManager.CanSetAsActiveScene(scene))
                    menu.AddItem(content, false, SetSceneActive, scene);
                else
                    menu.AddDisabledItem(content);
                menu.AddSeparator("");
            }

            // Save
            if (scene.isLoaded)
            {
                if (!EditorApplication.isPlaying || EditorSceneManager.IsAuthoringScene(scene))
                {
                    menu.AddItem(EditorGUIUtility.TrTextContent("Save Scene"), false, SaveSelectedScenes, scene);
                    if (!scene.isSubScene)
                        menu.AddItem(EditorGUIUtility.TrTextContent("Save Scene As"), false, SaveSceneAs, scene);
                    if (hasMultipleScenes)
                        menu.AddItem(EditorGUIUtility.TrTextContent("Save All"), false, SaveAllScenes, scene);
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

            bool isUnloadOrRemoveValid = SceneManager.loadedSceneCount != GetNumLoadedScenesInSelection();

            if (!scene.isSubScene)
            {
                if (scene.isLoaded)
                {
                    // Unload
                    var content = EditorGUIUtility.TrTextContent("Unload Scene");
                    bool canUnloadScenes = isUnloadOrRemoveValid && !EditorApplication.isPlaying && !string.IsNullOrEmpty(scene.path);
                    if (canUnloadScenes)
                        menu.AddItem(content, false, UnloadSelectedScenes, scene);
                    else
                        menu.AddDisabledItem(content);
                }
                else
                {
                    // Load
                    var content = EditorGUIUtility.TrTextContent("Load Scene");
                    bool canLoadScenes = !EditorApplication.isPlaying;
                    if (canLoadScenes)
                        menu.AddItem(content, false, LoadSelectedScenes, scene);
                    else
                        menu.AddDisabledItem(content);
                }

                // Remove
                var removeContent = EditorGUIUtility.TrTextContent("Remove Scene");
                bool selectedAllScenes = GetSelectedScenes().Count == EditorSceneManager.sceneCount;
                bool canRemoveScenes = isUnloadOrRemoveValid && !selectedAllScenes && !EditorApplication.isPlaying;
                if (canRemoveScenes)
                    menu.AddItem(removeContent, false, RemoveSelectedScenes, scene);
                else
                    menu.AddDisabledItem(removeContent);
            }

            // Discard changes
            if (scene.isLoaded)
            {
                var content = EditorGUIUtility.TrTextContent("Discard changes");
                var selectedSceneHandles = GetSelectedScenes();
                var modifiedScenes = GetModifiedScenes(selectedSceneHandles);
                bool canReload = modifiedScenes.Length > 0 && CanScenesBeReloaded(modifiedScenes);
                bool canDiscardChanges = !EditorApplication.isPlaying && canReload;
                if (canDiscardChanges)
                    menu.AddItem(content, false, DiscardChangesInSelectedScenes, scene);
                else
                    menu.AddDisabledItem(content);
            }

            // Ping Scene Asset
            menu.AddSeparator("");
            var selectAssetContent = EditorGUIUtility.TrTextContent("Find In Project");
            if (!string.IsNullOrEmpty(scene.path))
                menu.AddItem(selectAssetContent, false, SelectSceneAsset, scene);
            else
                menu.AddDisabledItem(selectAssetContent);

            if (!scene.isSubScene)
            {
                var addSceneContent = EditorGUIUtility.TrTextContent("Add New Scene");
                if (!EditorApplication.isPlaying)
                    menu.AddItem(addSceneContent, false, AddNewScene, scene);
                else
                    menu.AddDisabledItem(addSceneContent);
            }

            menu.AddSeparator("");
            menu.AddItem(EditorGUIUtility.TrTextContent("Prefab/Remove Unused Overrides..."), false, RemoveAllPrefabInstancesUnusedOverridesFromSceneForMenuItem, scene);

            // Set the context of each MenuItem to the current selection, so the created gameobjects will be added as children
            // Sets includeCreateEmptyChild to false, since that item is superfluous here (the normal "Create Empty" is added as a child anyway)
            if (scene.isLoaded)
            {
                menu.AddSeparator("");
                if (scene.isSubScene)
                    AddCreateGameObjectItemsToSubSceneMenu(menu, scene);
                else
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
                else
                {
                    GameObject gameObject = EditorUtility.InstanceIDToObject(id) as GameObject;
                    if (gameObject)
                    {
                        var subSceneInfo = SubSceneGUI.GetSubSceneInfo(gameObject);
                        if (subSceneInfo.isValid && subSceneInfo.scene.IsValid())
                            selectedSceneHandles.Add(subSceneInfo.scene.handle);
                    }
                }
            }

            return selectedSceneHandles;
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
                CreateSceneHeaderContextClick(menu, scene);
                // Let users add extra items.
                SceneHierarchyHooks.AddCustomSceneHeaderContextMenuItems(menu, scene);
            }
            else
            {
                GameObject gameObject = EditorUtility.InstanceIDToObject(contextClickedItemID) as GameObject;
                var subSceneInfo = SubSceneGUI.GetSubSceneInfo(gameObject);
                if (subSceneInfo.isValid)
                {
                    CreateSubSceneGameObjectContextClick(menu, contextClickedItemID);
                    menu.AddSeparator(string.Empty);

                    if (subSceneInfo.scene.IsValid())
                    {
                        // Sub scenes where the scene object exists can reuse menu for regular scenes.
                        CreateSceneHeaderContextClick(menu, subSceneInfo.scene);
                    }
                    else
                    {
                        // Sub scenes where only the info exists, but not the scene object, need special handling.
                        SubSceneGUI.CreateClosedSubSceneContextClick(menu, subSceneInfo);
                    }
                    // Let users add extra items.
                    SceneHierarchyHooks.AddCustomSubSceneHeaderContextMenuItems(menu, subSceneInfo);
                }
                else
                {
                    CreateGameObjectContextClick(menu, contextClickedItemID);
                }
            }

            menu.ShowAsContext();
        }

        void PasteGO()
        {
            ClipboardUtility.PasteGO(m_CustomParentForNewGameObjects);
        }

        void DuplicateGO()
        {
            ClipboardUtility.DuplicateGO(m_CustomParentForNewGameObjects);
        }

        void RenameGO()
        {
            treeView.BeginNameEditing(0f);
        }

        internal void RenameNewGO()
        {
            // end renaming if any GO has active one
            treeView.EndNameEditing(true);

            if (m_EditorWindow != null)
                m_EditorWindow.Focus();

            treeViewReloadNeeded = true;
            SyncIfNeeded();
            RenameGO();
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

        void RemoveAllPrefabInstancesUnusedOverridesFromSceneForMenuItem(object userData)
        {
            RemoveAllPrefabInstancesUnusedOverridesFromScene((Scene)userData);
        }

        void RemoveSelectedPrefabInstanceUnusedOverrides()
        {
            PrefabUtility.InstanceOverridesInfo[] instanceOverrideInfos = PrefabUtility.GetPrefabInstancesOverridesInfos(Selection.gameObjects);

            AskUserToRemovePrefabInstanceUnusedOverrides(instanceOverrideInfos);
        }

        void RemoveAllPrefabInstancesUnusedOverridesFromScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            PrefabUtility.InstanceOverridesInfo[] instanceOverridesInfos = null;

            List<GameObject> gos = GetScenePrefabInstancesWithNonDefaultOverrides(scene);
            if (gos != null && gos.Any())
                instanceOverridesInfos = PrefabUtility.GetPrefabInstancesOverridesInfos(gos.ToArray());

            AskUserToRemovePrefabInstanceUnusedOverrides(instanceOverridesInfos);
        }

        bool AskUserToRemovePrefabInstanceUnusedOverrides(PrefabUtility.InstanceOverridesInfo[] instanceOverridesInfos)
        {
            if (PrefabUtility.DoRemovePrefabInstanceUnusedOverridesDialog(instanceOverridesInfos))
            {
                PrefabUtility.RemovePrefabInstanceUnusedOverrides(instanceOverridesInfos, InteractionMode.UserAction);
                return true;
            }

            return false;
        }

        List<GameObject> GetScenePrefabInstancesWithNonDefaultOverrides(Scene scene)
        {
            List<GameObject> gameObjects = new List<GameObject>();
            TransformVisitor visitor = new TransformVisitor();

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                visitor.VisitAll(root.transform, (transform, list) => {
                    GameObject go = transform.gameObject;
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.HasPrefabInstanceNonDefaultOverridesOrUnusedOverrides_CachedForUI(go))
                    {
                        gameObjects.Add(go);
                    }
                }, null);
            }

            return gameObjects;
        }

        void SetSceneActive(object userData)
        {
            Scene scene = (Scene)userData;
            EditorSceneManager.SetActiveScene(scene);
        }

        private static string GetDefaultParentKeyForScene(string sceneGUID)
        {
            return String.Format("DefaultParentObject-{0}", sceneGUID);
        }

        [Shortcut("Hierarchy View/Set as Default Parent")]
        private static void SetOriginShortcut()
        {
            SetDefaultParentObject(true);
            SceneHierarchyWindow.lastInteractedHierarchyWindow?.Repaint();
        }

        internal static int GetDefaultParentForSession(string sceneGUID)
        {
            return SessionState.GetInt(GetDefaultParentKeyForScene(sceneGUID), 0);
        }

        internal static void SetDefaultParentForSession(string sceneGUID, int instanceID)
        {
            SessionState.SetInt(GetDefaultParentKeyForScene(sceneGUID), instanceID);
        }

        [RequiredByNativeCode]
        internal static void UpdateSessionStateInfoAndActiveParentObjectValuesForScene(string sceneGUID, int id)
        {
            SetDefaultParentForSession(sceneGUID, id);
            GameObjectTreeViewGUI.UpdateActiveParentObjectValuesForScene(sceneGUID, id);
        }

        internal static void SetDefaultParentObject(bool toggle, GameObject defaultParentObject = null)
        {
            UnityEngine.GameObject lastSelectedObject = null;
            int id = 0;

            if (defaultParentObject != null)
            {
                lastSelectedObject = defaultParentObject;
                id = lastSelectedObject.GetInstanceID();
            }
            else if (Selection.objects.Length > 0)
            {
                lastSelectedObject = Selection.objects[Selection.objects.Length - 1] as GameObject;
                if (lastSelectedObject != null && !PrefabStageUtility.IsGameObjectThePrefabRootInAnyPrefabStage(lastSelectedObject))
                    id = lastSelectedObject.GetInstanceID();
            }

            var sceneGUID = "";
            Scene scene;

            if (lastSelectedObject)
            {
                // entering a prefab from within a prefab creates a dummy object
                // we don't want it to be selectable as an origin object
                if (lastSelectedObject.name == PrefabUtility.kDummyPrefabStageRootObjectName)
                    return;

                sceneGUID = lastSelectedObject.scene.guid;
                scene = lastSelectedObject.scene;
            }
            else
            {
                scene = EditorSceneManager.GetActiveScene();
                sceneGUID = scene.guid;
            }

            string undoText = "Set Default Parent Object";
            int currentlySetID = GetDefaultParentForSession(sceneGUID);

            var objectInstanceID = lastSelectedObject ? lastSelectedObject.GetInstanceID() : 0;
            var isPrefabStage = PrefabStageUtility.IsPrefabStageScene(scene);

            // if we're toggling an object that is already the default parent object in an inactive scene,
            // we set the active scene and leave the same object as the active parent
            // this is to avoid confusion because active parent objects are not highlighted in inactive scenes
            if (toggle && currentlySetID == objectInstanceID && sceneGUID != EditorSceneManager.GetActiveScene().guid && !isPrefabStage)
            {
                EditorSceneManager.SetActiveScene(scene);
                return;
            }

            // we check if we're toggling inside the active scene
            // if yes - we deactivate the current active parent object
            // if not - we allow setting a new parent object immediately without unsetting the previous object first
            // (it gets overriden on the first shortcut click)
            if (toggle && currentlySetID != 0 && (sceneGUID == EditorSceneManager.GetActiveScene().guid || isPrefabStage))
            {
                if (lastSelectedObject == null)
                    return;

                id = 0;
                undoText = "Clear Default Parent Object";
            }

            if (toggle && currentlySetID != 0 || lastSelectedObject != null)
            {
                InternalEditorUtility.RegisterSetDefaultParentObjectUndo(sceneGUID, GetDefaultParentForSession(sceneGUID), undoText);
                if (!isPrefabStage)
                    EditorSceneManager.SetActiveScene(scene);
            }

            UpdateSessionStateInfoAndActiveParentObjectValuesForScene(sceneGUID, id);
        }

        internal static void ClearDefaultParentObject(string sceneGUID = "")
        {
            UnityEngine.GameObject lastSelectedObject = null;

            if (sceneGUID == "")
            {
                if (Selection.objects.Length > 0)
                {
                    lastSelectedObject = Selection.objects[Selection.objects.Length - 1] as GameObject;
                }
                if (lastSelectedObject)
                    sceneGUID = lastSelectedObject.scene.guid;
                else
                    sceneGUID = EditorSceneManager.GetActiveScene().guid;
            }

            InternalEditorUtility.RegisterSetDefaultParentObjectUndo(sceneGUID, GetDefaultParentForSession(sceneGUID), "Clear Default Parent Object");
            UpdateSessionStateInfoAndActiveParentObjectValuesForScene(sceneGUID, 0);
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
            Scene scene = (Scene)userdata;
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
            Scene scene = (Scene)userData;
            if (scene.IsValid())
                EditorSceneManager.MoveSceneAfter(newScene, scene);
        }

        void SelectSceneAsset(object userData)
        {
            Scene scene = (Scene)userData;
            var sceneObject = AssetDatabase.LoadMainAssetAtPath(scene.path);
            Selection.activeObject = sceneObject;
            EditorGUIUtility.PingObject(sceneObject);
        }

        void SelectAll()
        {
            int[] instanceIDs = treeView.GetRowIDs();
            treeView.SetSelection(instanceIDs, false);
            TreeViewSelectionChanged(instanceIDs);
        }

        void DeselectAll()
        {
            int[] instanceIDs = new int[0];
            treeView.SetSelection(instanceIDs, false);
            TreeViewSelectionChanged(instanceIDs);
        }

        void InvertSelection()
        {
            int[] instanceIDs = treeView.GetRowIDs().Except(treeView.GetSelection()).ToArray();
            treeView.SetSelection(instanceIDs, true);
            TreeViewSelectionChanged(instanceIDs);
        }

        bool IsSelectChildrenAvailable()
        {
            foreach (var id in treeView.GetSelection())
            {
                var scene = EditorSceneManager.GetSceneByHandle(id);
                if (IsSceneHeaderInHierarchyWindow(scene) && scene.isLoaded)
                {
                    foreach (var rootGameObject in scene.GetRootGameObjects())
                    {
                        if (rootGameObject.transform.GetComponentsInChildren<Transform>(true).Length > 1)
                            return true;
                    }
                }
                else
                {
                    var go = InternalEditorUtility.GetObjectFromInstanceID(id) as GameObject;
                    if (go != null)
                    {
                        if (go.transform.GetComponentsInChildren<Transform>(true).Length > 1)
                            return true;
                    }
                }
            }

            return false;
        }

        void SelectChildren()
        {
            List<int> instanceIDs = new List<int>(treeView.GetSelection());
            foreach (var id in treeView.GetSelection())
            {
                var scene = EditorSceneManager.GetSceneByHandle(id);
                if (IsSceneHeaderInHierarchyWindow(scene))
                {
                    foreach (var rootGameObject in scene.GetRootGameObjects())
                    {
                        instanceIDs.Add(rootGameObject.GetInstanceID());
                        instanceIDs.AddRange(rootGameObject.transform.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.GetInstanceID()));
                    }
                }
                else
                {
                    var go = InternalEditorUtility.GetObjectFromInstanceID(id) as GameObject;
                    if (go != null)
                        instanceIDs.AddRange(go.transform.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject.GetInstanceID()));
                }
            }

            var newSelection = instanceIDs.Distinct().ToArray();
            treeView.SetSelection(newSelection, true);

            TreeViewSelectionChanged(newSelection);
        }

        private bool IsSelectPrefabRootAvailable()
        {
            foreach (var id in treeView.GetSelection())
            {
                var go = InternalEditorUtility.GetObjectFromInstanceID(id) as GameObject;
                if (go != null)
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (root != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void SelectPrefabRoot()
        {
            List<int> instanceIDs = new List<int>(treeView.GetSelection().Length);
            foreach (var id in treeView.GetSelection())
            {
                var go = InternalEditorUtility.GetObjectFromInstanceID(id) as GameObject;
                if (go != null)
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                    if (root != null)
                    {
                        instanceIDs.Add(root.GetInstanceID());
                    }
                }
            }

            var newSelection = instanceIDs.Distinct().ToArray();
            treeView.SetSelection(newSelection, true);
            TreeViewSelectionChanged(newSelection);
        }

        static internal GameObject GetSourceRootWhereGameObjectIsAddedAsOverride(GameObject go)
        {
            if (go == null)
                return null;

            var source = PrefabUtility.GetCorrespondingObjectFromSource(go);

            while (source != null)
            {
                if (PrefabUtility.IsAddedGameObjectOverride(source))
                    return PrefabUtility.GetPrefabAssetRootGameObject(source);

                source = PrefabUtility.GetCorrespondingObjectFromSource(source);
            }

            return null;
        }

        public void CollapseAll()
        {
            treeViewState.expandedIDs.Clear();
            ReloadData();
        }

        public void ExpandAll()
        {
            int[] instanceIDs = treeView.GetRowIDs();
            foreach (var id in instanceIDs)
                SetExpandedRecursive(id, true);
            ReloadData();
        }

        public virtual void AddItemsToWindowMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, CollapseAll);
            menu.AddSeparator("");
            m_LockTracker.AddItemsToMenu(menu);

            menu.AddItem(Styles.renamingEnabledContent, SceneHierarchyWindow.s_EnterRenameModeForNewGO, SceneHierarchyWindow.SwitchEnterRenameModeForNewGO);

            if (Unsupported.IsDeveloperMode())
            {
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Hierarchy "), s_Debug, () => s_Debug = !s_Debug);
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Prefab Scene"), s_DebugPrefabStage, () => s_DebugPrefabStage = !s_DebugPrefabStage);
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Expanded State Persistence"), s_DebugPersistingExpandedState, () => s_DebugPersistingExpandedState = !s_DebugPersistingExpandedState);
                menu.AddItem(new GUIContent("DEVELOPER/Debug Mode - Preview Scenes"), s_DebugPreviewScenes, () => s_DebugPreviewScenes = !s_DebugPreviewScenes);
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

        public void GetSelectedScenes(List<Scene> scenes)
        {
            foreach (var selectedID in treeViewState.selectedIDs)
            {
                var scene = EditorSceneManager.GetSceneByHandle(selectedID);

                if (scene.IsValid())
                {
                    scenes.Add(scene);
                }
            }
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
