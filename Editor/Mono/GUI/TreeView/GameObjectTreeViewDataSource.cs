// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.IMGUI.Controls;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // GameObjectTreeViewDataSource only fetches current visible items of the scene tree, because we derive from LazyTreeViewDataSource
    // Note: every time a Item's expanded state changes FetchData is called

    internal class GameObjectTreeViewDataSource : LazyTreeViewDataSource
    {
        const double k_LongFetchTime = 0.05; // How much time is considered to be a long fetch
        const double k_FetchDelta = 0.1; // How much time between long fetches is acceptable.
        const int k_MaxDelayedFetch = 5; // How many consecutive fetches can be delayed.
        const HierarchyType k_HierarchyType = HierarchyType.GameObjects;
        const int k_DefaultStartCapacity = 1000;

        int m_RootInstanceID;
        string m_SearchString = "";
        readonly SearchService.SearchSessionHandler m_SearchSessionHandler = new SearchService.SearchSessionHandler(SearchService.SearchEngineScope.Scene);
        SearchableEditorWindow.SearchModeHierarchyWindow m_SearchMode = 0; // 0 = All
        double m_LastFetchTime = 0.0;
        int m_DelayedFetches = 0;
        bool m_NeedsChildParentReferenceSetup;
        bool m_RowsPartiallyInitialized;
        int m_RowCount;
        List<TreeViewItem> m_ListOfRows; // We need the generic List type in this class for Add/RemoveRange and Sorting (m_Rows is IList)
        List<GameObjectTreeViewItem> m_StickySceneHeaderItems = new List<GameObjectTreeViewItem>();
        internal event System.Action beforeReloading;
        public HierarchySorting sortingState = new TransformSorting();

        public List<GameObjectTreeViewItem> sceneHeaderItems { get { return m_StickySceneHeaderItems; } }
        public string searchString
        {
            get
            {
                return m_SearchString;
            }
            set
            {
                if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(m_SearchString))
                    ClearSearchFilter();

                m_SearchString = value;
            }
        }
        public SearchableEditorWindow.SearchModeHierarchyWindow searchMode { get { return m_SearchMode; } set { m_SearchMode = value; } }
        public bool isFetchAIssue { get { return m_DelayedFetches >= k_MaxDelayedFetch; } }

        public Scene[] scenes { get; set; }

        public GameObjectTreeViewDataSource(TreeViewController treeView, int rootInstanceID, bool showRoot, bool rootItemIsCollapsable)
            : base(treeView)
        {
            m_RootInstanceID = rootInstanceID;
            showRootItem = showRoot;
            rootIsCollapsable = rootItemIsCollapsable;
        }

        internal void SetupChildParentReferencesIfNeeded()
        {
            // Ensure all rows are initialized (not only the visible)
            EnsureFullyInitialized();

            // We delay the calling SetChildParentReferences until needed to ensure fast reloading of
            // the game object hierarchy (in playmode this prevent hiccups for large scenes)
            if (m_NeedsChildParentReferenceSetup)
            {
                m_NeedsChildParentReferenceSetup = false;
                TreeViewUtility.SetChildParentReferences(GetRows(), m_RootItem);
            }
        }

        public void EnsureFullyInitialized()
        {
            if (m_RowsPartiallyInitialized)
            {
                InitializeFull();
                m_RowsPartiallyInitialized = false;
            }
        }

        override public int rowCount
        {
            get
            {
                return m_RowCount;
            }
        }

        public override void RevealItem(int itemID)
        {
            // Optimization: Only spend time on revealing item if it is part of the Hierarchy
            if (IsValidHierarchyInstanceID(itemID))
                base.RevealItem(itemID);
        }

        public override void RevealItems(int[] itemIDs)
        {
            // Optimization: Only spend time on revealing item if it is part of the Hierarchy
            HashSet<int> validItemIDs = new HashSet<int>();
            foreach (var itemID in itemIDs)
            {
                if (IsValidHierarchyInstanceID(itemID))
                    validItemIDs.Add(itemID);
            }

            base.RevealItems(validItemIDs.ToArray());
        }

        override public bool IsRevealed(int id)
        {
            return GetRow(id) != -1;
        }

        private bool IsValidHierarchyInstanceID(int instanceID)
        {
            bool isScene = SceneHierarchy.IsSceneHeaderInHierarchyWindow(EditorSceneManager.GetSceneByHandle(instanceID));
            bool isGameObject = InternalEditorUtility.GetTypeWithoutLoadingObject(instanceID) == typeof(GameObject);
            return isScene || isGameObject;
        }

        HierarchyProperty FindHierarchyProperty(int instanceID)
        {
            // Optimization: Prevent search by early out if 'id' is not a game object (or scene handle)
            if (!IsValidHierarchyInstanceID(instanceID))
                return null;

            HierarchyProperty property = CreateHierarchyProperty();
            if (property.Find(instanceID, m_TreeView.state.expandedIDs.ToArray()))
                return property;

            return null;
        }

        override public int GetRow(int id)
        {
            bool isSearching = !string.IsNullOrEmpty(m_SearchString);
            if (isSearching)
                return base.GetRow(id);

            // We read from the backend directly instead of using GetRows()
            // because we might not yet have initialized tree view after creating a new
            // game object. GetRows also needs full init which we want to prevent
            HierarchyProperty property = FindHierarchyProperty(id);
            if (property != null)
                return property.row;

            return -1;
        }

        override public TreeViewItem GetItem(int row)
        {
            return m_Rows[row];
        }

        public override IList<TreeViewItem> GetRows()
        {
            InitIfNeeded();
            EnsureFullyInitialized();

            return m_Rows;
        }

        override public TreeViewItem FindItem(int id)
        {
            // Since this is a LazyTreeViewDataSource that only knows about expanded items
            // we need to reveal the item before searching for it (expand its ancestors)
            RevealItem(id);

            // Since RevealItem can have reloaded data because items was revealed we need to make
            // sure that parent child references is set up
            SetupChildParentReferencesIfNeeded();

            return base.FindItem(id);
        }

        bool AreScenesValid(Scene[] customScenes)
        {
            if (customScenes == null)
                return false;

            foreach (var scene in customScenes)
                if (!scene.IsValid())
                    return false;

            return true;
        }

        internal HierarchyProperty CreateHierarchyProperty()
        {
            HierarchyProperty property = new HierarchyProperty(k_HierarchyType);
            property.alphaSorted = IsUsingAlphaSort();
            property.showSceneHeaders = ShouldShowSceneHeaders();
            if (SceneHierarchyHooks.provideSubScenes != null)
                property.SetSubScenes(SceneHierarchyHooks.provideSubScenes());

            if (AreScenesValid(scenes))
                property.SetCustomScenes(scenes);
            return property;
        }

        bool ShouldShowSceneHeaders()
        {
            // Don't show headers if there's a single scene and it's a preview scene.
            return (scenes == null || scenes.Length != 1 || !EditorSceneManager.IsPreviewScene(scenes[0]));
        }

        void CreateRootItem(HierarchyProperty property)
        {
            int rootDepth = 0; // hiddden

            if (property.isValid)
            {
                // Game Object sub tree
                m_RootItem = new GameObjectTreeViewItem(m_RootInstanceID, rootDepth, null, property.name);
            }
            else
            {
                // All game objects
                m_RootItem = new GameObjectTreeViewItem(m_RootInstanceID, rootDepth, null, "RootOfAll");
            }
        }

        void ClearSearchFilter()
        {
            var property = CreateHierarchyProperty();
            property.SetSearchFilter("", 0); // 0 = All

            m_SearchSessionHandler.EndSession();
        }

        public override void FetchData()
        {
            beforeReloading?.Invoke();

            Profiler.BeginSample("SceneHierarchyWindow.FetchData");
            m_RowsPartiallyInitialized = false;
            double fetchStartTime = EditorApplication.timeSinceStartup;

            HierarchyProperty property = CreateHierarchyProperty();
            if (m_RootInstanceID != 0)
            {
                bool found = property.Find(m_RootInstanceID, null);
                if (!found)
                {
                    Debug.LogError("Root gameobject with id " + m_RootInstanceID + " not found!!");
                    m_RootInstanceID = 0;
                    property.Reset();
                }
            }

            CreateRootItem(property);

            // Must be set before SetSelection (they calls GetRows which will fetch again if m_NeedRefreshVisibleFolders = true)
            m_NeedRefreshRows = false;

            m_NeedsChildParentReferenceSetup = true;

            bool subTreeWanted = m_RootInstanceID != 0;
            bool isSearching = !string.IsNullOrEmpty(m_SearchString);

            if (isSearching)
            {
                m_SearchSessionHandler.BeginSession(() => new SearchService.HierarchySearchContext {rootProperty = property});
            }

            if (isSearching || subTreeWanted)
            {
                InitializeProgressivly(property, subTreeWanted, isSearching);
            }
            else
            {
                // We delay the setup of the full data and calling SetChildParentReferences to ensure fast reloading of
                // the game object hierarchy
                InitializeMinimal();
            }

            double fetchEndTime = EditorApplication.timeSinceStartup;
            double fetchTotalTime = fetchEndTime - fetchStartTime;
            double fetchDeltaTime = fetchEndTime - m_LastFetchTime;

            // If we have two relatively close consecutive fetches check execution time.
            if (fetchDeltaTime > k_FetchDelta && fetchTotalTime > k_LongFetchTime)
                m_DelayedFetches++;
            else
                m_DelayedFetches = 0;
            m_LastFetchTime = fetchStartTime;

            // We want to reset selection on copy/duplication/delete
            m_TreeView.SetSelection(Selection.instanceIDs, false); // use false because we might just be expanding/collapsing a Item (which would prevent collapsing a Item with a selected child)

            CreateSceneHeaderItems();

            if (SceneHierarchy.s_Debug)
                Debug.Log("Fetch time: " + fetchTotalTime * 1000.0 + " ms, alphaSort = " + IsUsingAlphaSort());

            Profiler.EndSample();
        }

        public override bool CanBeParent(TreeViewItem item)
        {
            // Ensure parent child-parent references are setup if requesting parent information
            SetupChildParentReferencesIfNeeded();

            return base.CanBeParent(item);
        }

        bool IsUsingAlphaSort()
        {
            return sortingState.GetType() == typeof(AlphabeticalSorting);
        }

        static void Resize(List<TreeViewItem> list, int count)
        {
            int cur = list.Count;
            if (count < cur)
                list.RemoveRange(count, cur - count);
            else if (count > cur)
            {
                if (count > list.Capacity) // this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                    list.Capacity = count + 20; // add some extra to prevent alloc'ing when adding to list
                list.AddRange(Enumerable.Repeat(default(TreeViewItem), count - cur)); // add range is nulls
            }
        }

        private void ResizeItemList(int count)
        {
            AllocateBackingArrayIfNeeded();

            if (m_ListOfRows.Count != count)
            {
                Resize(m_ListOfRows, count);
                Assert.AreEqual(m_ListOfRows.Count, count, "List of rows count incorrect");
            }
        }

        private void AllocateBackingArrayIfNeeded()
        {
            if (m_Rows == null)
            {
                int startCapacity = m_RowCount > k_DefaultStartCapacity ? m_RowCount : k_DefaultStartCapacity;
                m_ListOfRows = new List<TreeViewItem>(startCapacity);
                m_Rows = m_ListOfRows;
            }
        }

        private void InitializeMinimal()
        {
            int[] expanded = m_TreeView.state.expandedIDs.ToArray();

            HierarchyProperty property = CreateHierarchyProperty();
            m_RowCount = property.CountRemaining(expanded);
            ResizeItemList(m_RowCount);
            property.Reset();

            if (SceneHierarchy.s_Debug)
                Log("Init minimal (" + m_RowCount + ") num scenes " + SceneManager.sceneCount);

            int firstRow, lastRow;
            m_TreeView.gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);

            InitializeRows(property, firstRow, lastRow);

            m_RowsPartiallyInitialized = true;
        }

        void InitializeFull()
        {
            if (SceneHierarchy.s_Debug)
                Log("Init full (" + m_RowCount + ")");

            HierarchyProperty property = CreateHierarchyProperty();
            m_RowCount = property.CountRemaining(m_TreeView.state.expandedIDs.ToArray());
            ResizeItemList(m_RowCount);
            property.Reset();

            InitializeRows(property, 0, m_RowCount - 1);
        }

        // Used for search results, sub tree view (of e.g. a prefab) and custom sorting
        void InitializeProgressivly(HierarchyProperty property, bool subTreeWanted, bool isSearching)
        {
            AllocateBackingArrayIfNeeded();

            int minAllowedDepth = subTreeWanted ? property.depth + 1 : 0;

            if (!isSearching)
            {
                // Subtree setup
                int row = 0;
                int[] expanded = expandedIDs.ToArray();
                int subtractDepth = subTreeWanted ? property.depth + 1 : 0;

                while (property.NextWithDepthCheck(expanded, minAllowedDepth))
                {
                    var item = EnsureCreatedItem(row);
                    InitTreeViewItem(item, property, property.hasChildren, property.depth - subtractDepth);
                    row++;
                }
                m_RowCount = row;
            }
            else // Searching
            {
                m_RowCount = InitializeSearchResults(property, minAllowedDepth);
            }

            // Now shrink to fit if needed
            ResizeItemList(m_RowCount);
        }

        // Returns number of rows in search result
        int InitializeSearchResults(HierarchyProperty property, int minAllowedDepth)
        {
            // Search setup
            const bool kShowItemHasChildren = false;
            const int kItemDepth = 0;
            int currentSceneHandle = -1;
            int row = 0;
            var searchFilter = SearchableEditorWindow.CreateFilter(searchString, (SearchableEditorWindow.SearchMode)m_SearchMode);
            var searchContext = (SearchService.HierarchySearchContext)m_SearchSessionHandler.context;
            searchContext.filter = searchFilter;
            searchContext.rootProperty = property;

            m_SearchSessionHandler.BeginSearch(searchString);

            var headerRows = new List<int>();
            while (property.NextWithDepthCheck(null, minAllowedDepth))
            {
                if (!SearchService.Scene.Filter(m_SearchString, property, searchContext))
                {
                    property.SetFilteredVisibility(false);
                    continue;
                }

                property.SetFilteredVisibility(true);
                var item = EnsureCreatedItem(row);
                // Add scene headers when encountering a new scene (and it's not a header in itself)
                if (AddSceneHeaderToSearchIfNeeded(item, property, ref currentSceneHandle))
                {
                    row++;
                    headerRows.Add(row);

                    if (IsSceneHeader(property))
                        continue; // no need to add it

                    item = EnsureCreatedItem(row); // prepare for item below
                }
                InitTreeViewItem(item, property, kShowItemHasChildren, kItemDepth);
                row++;
            }

            m_SearchSessionHandler.EndSearch();

            int numRows = row;

            // Now sort scene section
            if (headerRows.Count > 0)
            {
                int currentSortStart = headerRows[0];
                for (int i = 1; i < headerRows.Count; i++)
                {
                    int count = headerRows[i] - currentSortStart - 1;
                    m_ListOfRows.Sort(currentSortStart, count, new TreeViewItemAlphaNumericSort());
                    currentSortStart = headerRows[i];
                }

                // last section
                m_ListOfRows.Sort(currentSortStart, numRows - currentSortStart, new TreeViewItemAlphaNumericSort());
            }


            return numRows;
        }

        bool AddSceneHeaderToSearchIfNeeded(GameObjectTreeViewItem item, HierarchyProperty property, ref int currentSceneHandle)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                return false;

            Scene scene = property.GetScene();
            if (currentSceneHandle != scene.handle)
            {
                currentSceneHandle = scene.handle;
                InitTreeViewItem(item, scene.handle, scene, true, 0, null, false, 0);
                return true;
            }
            return false;
        }

        GameObjectTreeViewItem EnsureCreatedItem(int row)
        {
            if (row >= m_Rows.Count)
                m_Rows.Add(null);

            var item = (GameObjectTreeViewItem)m_Rows[row];
            if (item == null)
            {
                item = new GameObjectTreeViewItem(0, 0, null, null);
                m_Rows[row] = item;
            }
            return item;
        }

        void InitializeRows(HierarchyProperty property, int firstRow, int lastRow)
        {
            property.Reset();

            int[] expanded = expandedIDs.ToArray();

            // Skip items if needed
            if (firstRow > 0)
            {
                int numRowsToSkip = firstRow;
                if (!property.Skip(numRowsToSkip, expanded))
                    Debug.LogError("Failed to skip " + numRowsToSkip);
            }

            // Fetch visible items
            int row = firstRow;

            while (property.Next(expanded) && row <= lastRow)
            {
                var item = EnsureCreatedItem(row);
                InitTreeViewItem(item, property, property.hasChildren, property.depth);
                row++;
            }
        }

        private void InitTreeViewItem(GameObjectTreeViewItem item, HierarchyProperty property, bool itemHasChildren, int itemDepth)
        {
            InitTreeViewItem(item, property.instanceID, property.GetScene(), IsSceneHeader(property), property.colorCode, property.pptrValue, itemHasChildren, itemDepth);
        }

        private void InitTreeViewItem(GameObjectTreeViewItem item, int itemID, Scene scene, bool isSceneHeader, int colorCode, Object pptrObject, bool hasChildren, int depth)
        {
            item.children = null;
            item.id = itemID;
            item.depth = depth;
            item.parent = null;
            item.icon = null;
            if (isSceneHeader)
                item.displayName = string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name;
            else
                item.displayName = null; // For GameObject, name is empty as the name gets set from the objectPPTR if the Item is used in GameObjectTreeViewGUI.

            item.colorCode = colorCode;
            item.objectPPTR = pptrObject;
            item.isSceneHeader = isSceneHeader;
            item.scene = scene;

            item.lazyInitializationDone = false;
            item.showPrefabModeButton = false;
            item.overlayIcon = null;

            if (hasChildren)
            {
                item.children = CreateChildListForCollapsedParent(); // add a dummy child in children list to ensure we show the collapse arrow (because we do not fetch data for collapsed items)
            }
        }

        bool IsSceneHeader(HierarchyProperty property)
        {
            return property.pptrValue == null;
        }

        protected override void GetParentsAbove(int id, HashSet<int> parentsAbove)
        {
            if (!IsValidHierarchyInstanceID(id))
                return;

            IHierarchyProperty propertyIterator = CreateHierarchyProperty();
            if (propertyIterator.Find(id, null))
            {
                while (propertyIterator.Parent())
                {
                    parentsAbove.Add((propertyIterator.instanceID));
                }
            }
        }

        // Should return the items that have children from id and below
        protected override void GetParentsBelow(int id, HashSet<int> parentsBelow)
        {
            // Add all children expanded ids to hashset
            if (!IsValidHierarchyInstanceID(id))
                return;

            IHierarchyProperty search = CreateHierarchyProperty();
            if (search.Find(id, null))
            {
                parentsBelow.Add(id);

                int depth = search.depth;
                while (search.Next(null) && search.depth > depth)
                {
                    if (search.hasChildren)
                        parentsBelow.Add(search.instanceID);
                }
            }
        }

        static void Log(string text)
        {
            //System.Console.WriteLine(text);
            Debug.Log(text);
        }

        override public bool IsRenamingItemAllowed(TreeViewItem item)
        {
            GameObjectTreeViewItem goItem = item as GameObjectTreeViewItem;
            if (goItem.isSceneHeader)
                return false;

            if (SubSceneGUI.IsUsingSubScenes() && SubSceneGUI.IsSubSceneHeader((GameObject)goItem.objectPPTR))
                return false;

            return true;
        }

        void CreateSceneHeaderItems()
        {
            m_StickySceneHeaderItems.Clear();

            int numScenesInHierarchy = EditorSceneManager.sceneCount;

            if (SubSceneGUI.IsUsingSubScenes())
            {
                for (int i = 0; i < numScenesInHierarchy; ++i)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    var subSceneInfo = SubSceneGUI.GetSubSceneInfo(scene);
                    if (subSceneInfo.isValid)
                    {
                        var item = new GameObjectTreeViewItem(0, 0, null, null);
                        var transform = subSceneInfo.transform;
                        GameObject gameObject = transform.gameObject;
                        int depth = SubSceneGUI.CalculateHierarchyDepthOfSubScene(subSceneInfo);
                        InitTreeViewItem(item, gameObject.GetInstanceID(), subSceneInfo.scene, false, 0, gameObject, false, depth);
                        m_StickySceneHeaderItems.Add(item);
                    }
                    else
                    {
                        var item = new GameObjectTreeViewItem(0, 0, null, null);
                        InitTreeViewItem(item, scene.handle, scene, true, 0, null, false, 0);
                        m_StickySceneHeaderItems.Add(item);
                    }
                }
            }
            else
            {
                for (int i = 0; i < numScenesInHierarchy; ++i)
                {
                    Scene scene = SceneManager.GetSceneAt(i);

                    var item = new GameObjectTreeViewItem(0, 0, null, null);
                    InitTreeViewItem(item, scene.handle, scene, true, 0, null, false, 0);
                    m_StickySceneHeaderItems.Add(item);
                }
            }
        }

        public Scene GetLastScene()
        {
            if (scenes != null)
            {
                for (int i = scenes.Length - 1; i >= 0; i--)
                    if (scenes[i].isLoaded && !scenes[i].isSubScene)
                        return scenes[i];
            }

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i).isLoaded && !SceneManager.GetSceneAt(i).isSubScene)
                    return SceneManager.GetSceneAt(i);

            Assert.IsTrue(false, "No loaded scene could be found");
            return new Scene();
        }
    }
}
