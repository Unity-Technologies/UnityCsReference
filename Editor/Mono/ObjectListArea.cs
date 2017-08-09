// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.VersionControl;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Math = System.Math;
using IndexOutOfRangeException = System.IndexOutOfRangeException;


namespace UnityEditor
{
    [System.Serializable]
    internal class ObjectListAreaState
    {
        // Selection state
        public List<int> m_SelectedInstanceIDs = new List<int>();
        public int m_LastClickedInstanceID;     // Used for navigation
        public bool m_HadKeyboardFocusLastEvent; // Needs to survive domain reloads to prevent setting selection on got keyboard focus

        // Expanded instanceIDs
        public List<int> m_ExpandedInstanceIDs = new List<int>();

        // Rename state
        public RenameOverlay m_RenameOverlay = new RenameOverlay();

        // Create new asset state
        public CreateAssetUtility m_CreateAssetUtility = new CreateAssetUtility();
        public int m_NewAssetIndexInList = -1;

        // Misc state
        public Vector2 m_ScrollPosition;
        public int m_GridSize = 64;

        public void OnAwake()
        {
            // Clear state that should not survive closing/starting Unity
            m_NewAssetIndexInList = -1;
            m_RenameOverlay.Clear();
            m_CreateAssetUtility = new CreateAssetUtility();
        }
    }


    internal partial class ObjectListArea
    {
        class Styles
        {
            public GUIStyle resultsLabel = new GUIStyle("PR Label");
            public GUIStyle resultsGridLabel = GetStyle("ProjectBrowserGridLabel");
            public GUIStyle resultsGrid = "ObjectPickerResultsGrid";
            public GUIStyle background = "ObjectPickerBackground";
            public GUIStyle previewTextureBackground = "ObjectPickerPreviewBackground";
            public GUIStyle groupHeaderMiddle = GetStyle("ProjectBrowserHeaderBgMiddle");
            public GUIStyle groupHeaderTop = GetStyle("ProjectBrowserHeaderBgTop");
            public GUIStyle groupHeaderLabel = "Label";
            public GUIStyle groupHeaderLabelCount = "MiniLabel";
            public GUIStyle groupFoldout = "IN Foldout";
            public GUIStyle toolbarBack = "ObjectPickerToolbar";
            public GUIStyle resultsFocusMarker;
            public GUIStyle miniRenameField = new GUIStyle("PR TextField");
            public GUIStyle ping = new GUIStyle("PR Ping");
            public GUIStyle miniPing = new GUIStyle("PR Ping");
            public GUIStyle iconDropShadow = GetStyle("ProjectBrowserIconDropShadow");
            public GUIStyle textureIconDropShadow = GetStyle("ProjectBrowserTextureIconDropShadow");
            public GUIStyle iconAreaBg = GetStyle("ProjectBrowserIconAreaBg");
            public GUIStyle previewBg = GetStyle("ProjectBrowserPreviewBg");
            public GUIStyle subAssetBg = GetStyle("ProjectBrowserSubAssetBg");
            public GUIStyle subAssetBgOpenEnded = GetStyle("ProjectBrowserSubAssetBgOpenEnded");
            public GUIStyle subAssetBgCloseEnded = GetStyle("ProjectBrowserSubAssetBgCloseEnded");
            public GUIStyle subAssetBgMiddle = GetStyle("ProjectBrowserSubAssetBgMiddle");
            public GUIStyle subAssetBgDivider = GetStyle("ProjectBrowserSubAssetBgDivider");
            public GUIStyle subAssetExpandButton = GetStyle("ProjectBrowserSubAssetExpandBtn");

            public GUIContent m_AssetStoreNotAvailableText = new GUIContent("The Asset Store is not available");

            public Styles()
            {
                // Use same colors as resultsGridLabel but with flexible size
                resultsFocusMarker = new GUIStyle(resultsGridLabel);
                resultsFocusMarker.fixedHeight = resultsFocusMarker.fixedWidth = 0;

                // Mini-label version for icon view
                miniRenameField.font = EditorStyles.miniLabel.font;
                miniRenameField.alignment = TextAnchor.LowerCenter;

                ping.fixedHeight = 16f;
                ping.padding.right = 10;

                // Mini-ping version for icon view
                miniPing.font = EditorStyles.miniLabel.font;
                miniPing.alignment = TextAnchor.MiddleCenter;

                resultsLabel.alignment = TextAnchor.MiddleLeft;
            }

            static GUIStyle GetStyle(string styleName)
            {
                return styleName; // Implicit construction of GUIStyle

                // For fast testing in editor resources
                //GUISkin skin = EditorGUIUtility.LoadRequired ("Builtin Skins/DarkSkin/Skins/ProjectBrowserSkin.guiSkin") as GUISkin;
                //return skin.GetStyle (styleName);
            }
        }
        static Styles s_Styles;

        // State persisted across assembly reloads
        ObjectListAreaState m_State;

        // Key navigation
        const int kHome = int.MinValue;
        const int kEnd = int.MaxValue;
        const int kPageDown = int.MaxValue - 1;
        const int kPageUp = int.MinValue + 1;
        int m_SelectionOffset = 0;

        const float k_ListModeVersionControlOverlayPadding = 14f;
        static bool s_VCEnabled = false;

        PingData m_Ping = new PingData();

        EditorWindow m_Owner;

        public bool allowDragging { get; set; }
        public bool allowRenaming { get; set; }
        public bool allowMultiSelect { get; set; }
        public bool allowDeselection { get; set; }
        public bool allowFocusRendering { get; set; }
        public bool allowBuiltinResources { get; set; }
        public bool allowUserRenderingHook { get; set; }
        public bool allowFindNextShortcut { get; set; }
        public bool foldersFirst { get; set; }
        int m_KeyboardControlID;

        Dictionary<int, string> m_InstanceIDToCroppedNameMap = new Dictionary<int, string>();
        int m_WidthUsedForCroppingName;
        bool m_AllowRenameOnMouseUp = true;


        Vector2 m_LastScrollPosition = new Vector2(0, 0);
        double LastScrollTime = 0;


        const double kDelayQueryAfterScroll = 0.0;


        public bool selectedAssetStoreAsset;


        internal Texture m_SelectedObjectIcon = null;

        LocalGroup m_LocalAssets;

        List<AssetStoreGroup> m_StoreAssets;
        string m_AssetStoreError = "";


        // List of all available groups
        List<Group> m_Groups;

        // Layout
        Rect m_TotalRect;
        Rect m_VisibleRect;
        const int kListLineHeight = 16;
        private int m_pingIndex;

        int m_MinIconSize = 32;
        int m_MinGridSize = 16;
        int m_MaxGridSize = 96;
        bool m_AllowThumbnails = true;
        const int kSpaceForScrollBar = 16;
        int m_LeftPaddingForPinging = 0;
        bool m_FrameLastClickedItem = false;

        bool m_ShowLocalAssetsOnly = true;


        // Asset store resources
        const double kQueryDelay = 0.2;
        public bool m_RequeryAssetStore;

        bool m_QueryInProgress = false;
        int m_ResizePreviewCacheTo = 0;

        string m_LastAssetStoreQuerySearchFilter = "";
        string[] m_LastAssetStoreQueryClassName = new string[0];
        string[] m_LastAssetStoreQueryLabels = new string[0];
        double m_LastAssetStoreQueryChangeTime = 0.0;

        double m_NextDirtyCheck = 0;


        // Callbacks
        System.Action m_RepaintWantedCallback;
        System.Action<bool> m_ItemSelectedCallback;
        System.Action m_KeyboardInputCallback;
        System.Action m_GotKeyboardFocus;
        System.Func<Rect, float> m_DrawLocalAssetHeader;
        System.Action m_AssetStoreSearchEnded;

        public System.Action repaintCallback                { get {return m_RepaintWantedCallback; } set {m_RepaintWantedCallback = value; }}
        public System.Action<bool> itemSelectedCallback     { get {return m_ItemSelectedCallback; }  set {m_ItemSelectedCallback = value; }}
        public System.Action keyboardCallback               { get {return m_KeyboardInputCallback; } set {m_KeyboardInputCallback = value; }}
        public System.Action gotKeyboardFocus               { get {return m_GotKeyboardFocus; }      set {m_GotKeyboardFocus = value; }}
        public System.Action assetStoreSearchEnded          { get {return m_AssetStoreSearchEnded; } set {m_AssetStoreSearchEnded = value; }}
        public System.Func<Rect, float> drawLocalAssetHeader { get {return m_DrawLocalAssetHeader; }  set {m_DrawLocalAssetHeader = value; }}

        // Debug
        static internal bool s_Debug = false;

        public ObjectListArea(ObjectListAreaState state, EditorWindow owner, bool showNoneItem)
        {
            m_State = state;
            m_Owner = owner;

            AssetStorePreviewManager.MaxCachedImages = 72; // Magic number. Will be reset on first layout.

            m_StoreAssets = new List<AssetStoreGroup>();
            m_RequeryAssetStore = false;

            m_LocalAssets = new LocalGroup(this, "", showNoneItem);

            m_Groups = new List<Group>();
            m_Groups.Add(m_LocalAssets);
        }

        public void ShowObjectsInList(int[] instanceIDs)
        {
            // Clear asset store search etc.
            Init(m_TotalRect, HierarchyType.Assets, new SearchFilter(), false);

            // Set list manually
            m_LocalAssets.ShowObjectsInList(instanceIDs);
        }

        // This method is being used by the EditorTests/Searching tests
        public string[] GetCurrentVisibleNames()
        {
            var list = m_LocalAssets.GetVisibleNameAndInstanceIDs();
            return list.Select(x => x.Key).ToArray();
        }

        public void Init(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails)
        {
            // Keep for debugging
            //Debug.Log ("Init ObjectListArea: " + searchFilter);

            m_TotalRect = m_VisibleRect = rect;

            m_LocalAssets.UpdateFilter(hierarchyType, searchFilter, foldersFirst);
            m_LocalAssets.UpdateAssets();

            foreach (AssetStoreGroup g in m_StoreAssets)
                g.UpdateFilter(hierarchyType, searchFilter, foldersFirst);

            bool isFolderBrowsing = (searchFilter.GetState() == SearchFilter.State.FolderBrowsing);
            if (isFolderBrowsing)
            {
                // Do not allow asset store searching when we have folder filtering
                m_LastAssetStoreQuerySearchFilter = "";
                m_LastAssetStoreQueryClassName = new string[0];
                m_LastAssetStoreQueryLabels = new string[0];
            }
            else
            {
                m_LastAssetStoreQuerySearchFilter = searchFilter.nameFilter == null ? "" : searchFilter.nameFilter;
                bool disableClassConstraint = searchFilter.classNames == null ||
                    System.Array.IndexOf(searchFilter.classNames, "Object") >= 0;
                m_LastAssetStoreQueryClassName = disableClassConstraint ? new string[0] : searchFilter.classNames;
                m_LastAssetStoreQueryLabels = searchFilter.assetLabels == null ? new string[0] : searchFilter.assetLabels;
            }

            m_LastAssetStoreQueryChangeTime = EditorApplication.timeSinceStartup;
            m_RequeryAssetStore = true;

            m_ShowLocalAssetsOnly = isFolderBrowsing || (searchFilter.GetState() != SearchFilter.State.SearchingInAssetStore);
            m_AssetStoreError = "";


            if (checkThumbnails)
                m_AllowThumbnails = ObjectsHaveThumbnails(hierarchyType, searchFilter);
            else
                m_AllowThumbnails = true;

            Repaint();

            // Clear instanceID to cropped name cache on init
            ClearCroppedLabelCache();

            // Prepare data
            SetupData(true);
        }

        bool HasFocus()
        {
            if (!allowFocusRendering)
                return true;
            return m_KeyboardControlID == GUIUtility.keyboardControl && m_Owner.m_Parent.hasFocus;
        }

        void QueryAssetStore()
        {
            bool searchChanged = m_RequeryAssetStore;
            m_RequeryAssetStore = false;

            // We disable Asset Store searching here. Note that we still query to get hits when searching local assets
            if (m_ShowLocalAssetsOnly && !ShowAssetStoreHitsWhileSearchingLocalAssets())
            {
                return;
            }

            bool hasValidFilter = m_LastAssetStoreQuerySearchFilter != "" || m_LastAssetStoreQueryClassName.Length != 0 || m_LastAssetStoreQueryLabels.Length != 0;

            // Make sure that we have only one pending asset store query
            // No need to call Repaint() to recheck this condition later because the
            // query callback will call that for us.
            if (m_QueryInProgress)
                return;

            if (!hasValidFilter)
            {
                ClearAssetStoreGroups();
                return;
            }

            // In order not to query prematurely we delay the query a bit to let the user type
            // more characters in the search input box if needed.
            if ((m_LastAssetStoreQueryChangeTime + kQueryDelay) > EditorApplication.timeSinceStartup)
            {
                m_RequeryAssetStore = true;
                Repaint();
                return;
            }

            m_QueryInProgress = true;

            // Remember the filter to check for changes after we have received the reply from asset store
            string queryFilter = m_LastAssetStoreQuerySearchFilter + m_LastAssetStoreQueryClassName + m_LastAssetStoreQueryLabels;

            AssetStoreSearchResults.Callback dg = delegate(AssetStoreSearchResults results) {
                    m_QueryInProgress = false;

                    // If filter changed while fetching the result then requery using the new filter.
                    if (queryFilter != m_LastAssetStoreQuerySearchFilter + m_LastAssetStoreQueryClassName + m_LastAssetStoreQueryLabels)
                        m_RequeryAssetStore = true;

                    if (results.error != null && results.error != "")
                    {
                        if (s_Debug)
                            Debug.LogError("Error performing Asset Store search: " + results.error);
                        else
                            System.Console.Write("Error performing Asset Store search: " + results.error);
                        m_AssetStoreError = results.error;
                        m_Groups.Clear();
                        m_Groups.Add(m_LocalAssets);
                        Repaint();

                        if (assetStoreSearchEnded != null)
                            assetStoreSearchEnded();

                        return;
                    }

                    m_AssetStoreError = "";

                    // Clear groups and use the ones from server
                    List<string> existingGroupNames = new List<string>();
                    foreach (AssetStoreGroup g in m_StoreAssets)
                        existingGroupNames.Add(g.Name);

                    m_Groups.Clear();
                    m_Groups.Add(m_LocalAssets);

                    foreach (AssetStoreSearchResults.Group inGroup in results.groups)
                    {
                        existingGroupNames.Remove(inGroup.name);
                        AssetStoreGroup group = m_StoreAssets.Find(g => g.Name == inGroup.name);

                        if (group == null)
                        {
                            group = new AssetStoreGroup(this, inGroup.label, inGroup.name);
                            m_StoreAssets.Add(group);
                        }
                        m_Groups.Add(group);

                        // Set total found if initial request or different from 0
                        if (inGroup.limit != 0)
                        {
                            group.ItemsAvailable = inGroup.totalFound;
                        }

                        if (inGroup.offset == 0 && inGroup.limit != 0)
                            group.Assets = inGroup.assets;
                        else
                            group.Assets.AddRange(inGroup.assets);
                    }

                    // Remove groups not valid for this request
                    foreach (string k in existingGroupNames)
                        m_StoreAssets.RemoveAll(g => g.Name == k);

                    EnsureAssetStoreGroupsAreOpenIfAllClosed();

                    Repaint();

                    if (assetStoreSearchEnded != null)
                        assetStoreSearchEnded();
                };

            List<AssetStoreClient.SearchCount> groupsQuery = new List<AssetStoreClient.SearchCount>();

            if (!searchChanged)
            {
                // More items for the same search criteria. Just apply offsets and limits.
                foreach (AssetStoreGroup v in m_StoreAssets)
                {
                    AssetStoreClient.SearchCount t = new AssetStoreClient.SearchCount();
                    if (v.Visible && v.NeedItems)
                    {
                        t.offset = v.Assets.Count;
                        t.limit = v.ItemsWantedShown - t.offset;
                    }
                    t.name = v.Name;
                    groupsQuery.Add(t);
                }
            }

            AssetStoreClient.SearchAssets(m_LastAssetStoreQuerySearchFilter,
                m_LastAssetStoreQueryClassName,
                m_LastAssetStoreQueryLabels,
                groupsQuery, dg);
        }

        void EnsureAssetStoreGroupsAreOpenIfAllClosed()
        {
            if (m_StoreAssets.Count > 0)
            {
                int numExpanded = 0;
                foreach (var group in m_StoreAssets)
                    if (group.Visible)
                        numExpanded++;
                if (numExpanded == 0)
                    foreach (var group in m_StoreAssets)
                        group.Visible = group.visiblePreference = true;
            }
        }

        void RequeryAssetStore()
        {
            m_RequeryAssetStore = true;
        }

        void ClearAssetStoreGroups()
        {
            m_Groups.Clear();
            m_Groups.Add(m_LocalAssets);
            m_StoreAssets.Clear();
            Repaint();
        }

        public string GetAssetStoreButtonText()
        {
            string buttonText = "Asset Store";
            if (ShowAssetStoreHitsWhileSearchingLocalAssets())
            {
                for (int i = 0; i < m_StoreAssets.Count; ++i)
                {
                    if (i == 0)
                        buttonText += ": ";
                    else
                        buttonText += " \u2215 "; // forward slash
                    AssetStoreGroup group = m_StoreAssets[i];
                    buttonText += (group.ItemsAvailable > 999 ? "999+" : group.ItemsAvailable.ToString());
                }
            }
            return buttonText;
        }

        bool ShowAssetStoreHitsWhileSearchingLocalAssets()
        {
            return EditorPrefs.GetBool("ShowAssetStoreSearchHits", true); // See PreferencesWindow
        }

        public void ShowAssetStoreHitCountWhileSearchingLocalAssetsChanged()
        {
            if (ShowAssetStoreHitsWhileSearchingLocalAssets())
            {
                RequeryAssetStore();
            }
            else
            {
                if (m_ShowLocalAssetsOnly)
                    ClearAssetStoreGroups(); // do not clear if we are wathcing asset store results
            }
            Repaint();
        }

        internal float GetVisibleWidth()
        {
            return m_VisibleRect.width;
        }

        public float m_SpaceBetween = 6f;
        public float m_TopMargin = 10f;
        public float m_BottomMargin = 10f;
        public float m_RightMargin = 10f;
        public float m_LeftMargin = 10f;

        public void OnGUI(Rect position, int keyboardControlID)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            s_VCEnabled = Provider.isActive;

            Event evt = Event.current;

            m_TotalRect = position;

            FrameLastClickedItemIfWanted();

            // Background
            GUI.Label(m_TotalRect, GUIContent.none, s_Styles.iconAreaBg);

            // For keyboard focus handling (for Tab support and rendering of keyboard focus state)
            m_KeyboardControlID = keyboardControlID;

            // Grab keyboard focus on mousedown in entire rect
            if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = m_KeyboardControlID;
                m_AllowRenameOnMouseUp = true; // Reset on mouse down

                Repaint(); // Ensure repaint so we can show we have keyboard focus
            }

            bool hasKeyboardFocus = m_KeyboardControlID == GUIUtility.keyboardControl;
            if (hasKeyboardFocus != m_State.m_HadKeyboardFocusLastEvent)
            {
                m_State.m_HadKeyboardFocusLastEvent = hasKeyboardFocus;

                // We got focus
                if (hasKeyboardFocus)
                {
                    if (evt.type == EventType.MouseDown)
                        m_AllowRenameOnMouseUp = false; // If we got focus by mouse down then we do not want to begin renaming if clicking on an already selected item

                    if (m_GotKeyboardFocus != null)
                        m_GotKeyboardFocus();
                }
            }

            // For key navigation: Auto set selection to first element if selection is not shown currently when tabbing
            if (evt.keyCode == KeyCode.Tab && evt.type == EventType.KeyDown && !hasKeyboardFocus && !IsShowingAny(GetSelection()))
            {
                int firstInstanceID;
                if (m_LocalAssets.InstanceIdAtIndex(0, out firstInstanceID))
                    Selection.activeInstanceID = firstInstanceID;
            }


            HandleKeyboard(true);
            HandleZoomScrolling();
            HandleListArea();
            DoOffsetSelection();
            HandleUnusedEvents();
            // GUI.Label(position, new GUIContent(AssetStorePreviewManager.StatsString()));
        }

        void FrameLastClickedItemIfWanted()
        {
            if (m_FrameLastClickedItem && Event.current.type == EventType.Repaint)
            {
                m_FrameLastClickedItem = false;
                double timeSinceLastDraw = EditorApplication.timeSinceStartup - m_LocalAssets.m_LastClickedDrawTime;
                if (m_State.m_SelectedInstanceIDs.Count > 0 && timeSinceLastDraw < 0.2)
                    Frame(m_State.m_LastClickedInstanceID, true, false);
            }
        }

        void HandleUnusedEvents()
        {
            if (allowDeselection && Event.current.type == EventType.MouseDown && Event.current.button == 0 && m_TotalRect.Contains(Event.current.mousePosition))
                SetSelection(new int[0], false);
        }

        public bool CanShowThumbnails()
        {
            //
            //      return m_AllowThumbnails || m_StoreAssets.Find( g => g.ItemsAvailable > 0) != null;
            // #else
            return m_AllowThumbnails;
            //
        }

        public int gridSize
        {
            get { return m_State.m_GridSize; }
            set
            {
                if (m_State.m_GridSize != value)
                {
                    m_State.m_GridSize = value;
                    m_FrameLastClickedItem = true;
                }
            }
        }

        public int minGridSize
        {
            get { return m_MinGridSize; }
        }
        public int maxGridSize
        {
            get { return m_MaxGridSize; }
        }


        public int numItemsDisplayed
        {
            get { return m_LocalAssets.ItemCount; }
        }

        static string CreateFilterString(string searchString, string requiredClassName)
        {
            string filter = searchString;

            if (!string.IsNullOrEmpty(requiredClassName))
            {
                filter += " t:" + requiredClassName;
            }

            return filter;
        }

        bool ObjectsHaveThumbnails(HierarchyType type, SearchFilter searchFilter)
        {
            // Check if we have any built-ins, if so we have thumbs since all builtins have thumbs
            if (m_LocalAssets.HasBuiltinResources)
                return true;

            // Check if current hierarchy have thumbs
            FilteredHierarchy hierarchy = new FilteredHierarchy(type);
            hierarchy.searchFilter = searchFilter;
            IHierarchyProperty assetProperty = FilteredHierarchyProperty.CreateHierarchyPropertyForFilter(hierarchy);
            int[] empty = new int[0];
            if (assetProperty.CountRemaining(empty) == 0)
                return true; // allow thumbnails: we prefer asset-store results as icons over list

            assetProperty.Reset();
            while (assetProperty.Next(empty))
            {
                if (assetProperty.hasFullPreviewImage)
                    return true;
            }

            return false;
        }

        internal void OnDestroy()
        {
            AssetPreview.DeletePreviewTextureManagerByID(GetAssetPreviewManagerID());
        }

        void Repaint()
        {
            if (m_RepaintWantedCallback != null)
                m_RepaintWantedCallback();
        }

        public void OnEvent()
        {
            GetRenameOverlay().OnEvent();
        }

        CreateAssetUtility GetCreateAssetUtility()
        {
            return m_State.m_CreateAssetUtility;
        }

        RenameOverlay GetRenameOverlay()
        {
            return m_State.m_RenameOverlay;
        }

        internal void BeginNamingNewAsset(string newAssetName, int instanceID, bool isCreatingNewFolder)
        {
            m_State.m_NewAssetIndexInList = m_LocalAssets.IndexOfNewText(newAssetName, isCreatingNewFolder, foldersFirst);
            if (m_State.m_NewAssetIndexInList != -1)
            {
                Frame(instanceID, true, false);
                GetRenameOverlay().BeginRename(newAssetName, instanceID, 0f);
            }
            else
            {
                Debug.LogError("Failed to insert new asset into list");
            }

            Repaint();
        }

        public bool BeginRename(float delay)
        {
            if (!allowRenaming)
                return false;

            // Only allow renaming when one item is selected
            if (m_State.m_SelectedInstanceIDs.Count != 1)
                return false;
            int instanceID = m_State.m_SelectedInstanceIDs[0];

            // Only main representations can be renamed (currently)
            if (AssetDatabase.IsSubAsset(instanceID))
                return false;

            // Builtin assets cannot be renamed
            if (m_LocalAssets.IsBuiltinAsset(instanceID))
                return false;

            if (!AssetDatabase.Contains(instanceID))
                return false;

            string name = m_LocalAssets.GetNameOfLocalAsset(instanceID);
            if (name == null)
                return false;

            return GetRenameOverlay().BeginRename(name, instanceID, delay);
        }

        public void EndRename(bool acceptChanges)
        {
            if (GetRenameOverlay().IsRenaming())
            {
                GetRenameOverlay().EndRename(acceptChanges);
                RenameEnded();
            }
        }

        void RenameEnded()
        {
            // We are done renaming (user accepted/rejected, we lost focus etc, other grabbed renameOverlay etc.)
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int instanceID = GetRenameOverlay().userData; // we passed in an instanceID as userData

            // Are we creating new asset?
            if (GetCreateAssetUtility().IsCreatingNewAsset())
            {
                if (GetRenameOverlay().userAcceptedRename)
                    GetCreateAssetUtility().EndNewAssetCreation(name);
            }
            else // renaming existing asset
            {
                if (GetRenameOverlay().userAcceptedRename)
                {
                    ObjectNames.SetNameSmartWithInstanceID(instanceID, name);
                }
            }

            if (GetRenameOverlay().HasKeyboardFocus())
                GUIUtility.keyboardControl = m_KeyboardControlID;

            if (GetRenameOverlay().userAcceptedRename)
            {
                Frame(instanceID, true, false); // frames existing assets (new ones could have instanceID 0)
            }

            ClearRenameState();
        }

        void ClearRenameState()
        {
            // Cleanup
            GetRenameOverlay().Clear();
            GetCreateAssetUtility().Clear();
            m_State.m_NewAssetIndexInList = -1;
        }

        internal void HandleRenameOverlay()
        {
            if (GetRenameOverlay().IsRenaming())
            {
                GUIStyle renameStyle = (IsListMode() ? null : s_Styles.miniRenameField);
                if (!GetRenameOverlay().OnGUI(renameStyle))
                {
                    RenameEnded();
                    GUIUtility.ExitGUI(); // We exit gui because we are iterating items and when we end naming a new asset this will change the order of items we are iterating.
                }
            }
        }

        public bool IsSelected(int instanceID)
        {
            return m_State.m_SelectedInstanceIDs.Contains(instanceID);
        }

        public int[] GetSelection()
        {
            return m_State.m_SelectedInstanceIDs.ToArray();
        }

        public bool IsLastClickedItemVisible()
        {
            return GetSelectedAssetIdx() >= 0;
        }

        public void SelectAll()
        {
            List<int> list = m_LocalAssets.GetInstanceIDs();
            SetSelection(list.ToArray(), false);
        }

        void SetSelection(int[] selectedInstanceIDs, bool doubleClicked)
        {
            InitSelection(selectedInstanceIDs);

            if (m_ItemSelectedCallback != null)
            {
                Repaint();
                m_ItemSelectedCallback(doubleClicked);
            }
        }

        public void InitSelection(int[] selectedInstanceIDs)
        {
            // Note that selectedInstanceIDs can be gameObjects
            m_State.m_SelectedInstanceIDs = new List<int>(selectedInstanceIDs);

            // Keep for debugging
            //Debug.Log ("InitSelection (ObjectListArea): new selection " + DebugUtils.ListToString(m_State.m_SelectedInstanceIDs));

            if (m_State.m_SelectedInstanceIDs.Count > 0)
            {
                // Only init last clicked instance if it is NOT part of our selection (we need it for navigation)
                if (!m_State.m_SelectedInstanceIDs.Contains(m_State.m_LastClickedInstanceID))
                    m_State.m_LastClickedInstanceID = m_State.m_SelectedInstanceIDs[m_State.m_SelectedInstanceIDs.Count - 1];
            }
            else
            {
                m_State.m_LastClickedInstanceID = 0;
            }


            if (Selection.activeObject == null || Selection.activeObject.GetType() != typeof(AssetStoreAssetInspector))
            {
                // Debug.Log("type is " + (Selection.activeObject == null ? "null " : Selection.activeObject.name) + " instance IDS ");
                // foreach (int i in selectedInstanceIDs)
                // {
                //  Debug.Log("selected instance ID " + i.ToString());
                // }
                selectedAssetStoreAsset = false;
                AssetStoreAssetSelection.Clear();
            }
        }

        void SetSelection(AssetStoreAsset assetStoreResult, bool doubleClicked)
        {
            m_State.m_SelectedInstanceIDs.Clear();

            selectedAssetStoreAsset = true;
            AssetStoreAssetSelection.Clear(); // TODO: remove when multiselect is to be supported
            AssetStorePreviewManager.CachedAssetStoreImage item = AssetStorePreviewManager.TextureFromUrl(assetStoreResult.staticPreviewURL, assetStoreResult.name, gridSize, s_Styles.resultsGridLabel, s_Styles.resultsGrid, true);
            Texture2D lowresPreview = item.image;
            AssetStoreAssetSelection.AddAsset(assetStoreResult, lowresPreview);
            if (m_ItemSelectedCallback != null)
            {
                Repaint();
                m_ItemSelectedCallback(doubleClicked);
            }
        }

        void HandleZoomScrolling()
        {
            if (EditorGUI.actionKey && Event.current.type == EventType.ScrollWheel && m_TotalRect.Contains(Event.current.mousePosition))
            {
                int sign = Event.current.delta.y > 0 ? -1 : 1;
                gridSize = Mathf.Clamp(gridSize + sign * 7, minGridSize, maxGridSize);

                if (sign < 0 && gridSize < m_MinIconSize)
                    gridSize = m_MinGridSize;
                if (sign > 0 && gridSize < m_MinIconSize)
                    gridSize = m_MinIconSize;

                Event.current.Use();
                GUI.changed = true;
            }
        }

        bool IsPreviewIconExpansionModifierPressed()
        {
            return Event.current.alt;
        }

        bool AllowLeftRightArrowNavigation()
        {
            bool gridMode = !m_LocalAssets.ListMode && !IsPreviewIconExpansionModifierPressed();
            bool validItemCount = !m_ShowLocalAssetsOnly || (m_LocalAssets.ItemCount > 1);
            return gridMode && validItemCount;
        }

        public void HandleKeyboard(bool checkKeyboardControl)
        {
            // Are we allowed to handle keyboard events?
            if (checkKeyboardControl && GUIUtility.keyboardControl != m_KeyboardControlID || !GUI.enabled)
                return;

            // Let client handle keyboard first
            if (m_KeyboardInputCallback != null)
                m_KeyboardInputCallback();

            // Now default list area handling
            if (Event.current.type == EventType.KeyDown)
            {
                int offset = 0;

                if (IsLastClickedItemVisible())
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                            offset = -m_LocalAssets.m_Grid.columns; // we assume that all groups have same number of columns
                            break;
                        case KeyCode.DownArrow:
                            offset = m_LocalAssets.m_Grid.columns;
                            break;
                        case KeyCode.LeftArrow:
                            if (AllowLeftRightArrowNavigation())
                                offset = -1;
                            break;
                        case KeyCode.RightArrow:
                            if (AllowLeftRightArrowNavigation())
                                offset = 1;
                            break;
                        case KeyCode.Home:
                            offset = kHome;
                            break;
                        case KeyCode.End:
                            offset = kEnd;
                            break;
                        case KeyCode.PageUp:
                            offset = kPageUp;
                            break;
                        case KeyCode.PageDown:
                            offset = kPageDown;
                            break;
                    }
                }
                else
                {
                    // Select first on any key navigation events if not selection is present
                    bool validNavigationKey = false;
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftArrow:
                        case KeyCode.RightArrow:
                            validNavigationKey = AllowLeftRightArrowNavigation();
                            break;

                        case KeyCode.UpArrow:
                        case KeyCode.DownArrow:
                        case KeyCode.Home:
                        case KeyCode.End:
                        case KeyCode.PageUp:
                        case KeyCode.PageDown:
                            validNavigationKey = true;
                            break;
                    }

                    if (validNavigationKey)
                    {
                        SelectFirst();
                        Event.current.Use();
                    }
                }

                if (offset != 0)
                {
                    // If nothing is selected then select first object and ignore the offset (when showing none GetSelectedAssetIdx return -1)
                    if (GetSelectedAssetIdx() < 0 && !m_LocalAssets.ShowNone)
                        SetSelectedAssetByIdx(0);
                    else
                        m_SelectionOffset = offset;

                    Event.current.Use();
                    GUI.changed = true;
                }
                else
                {
                    if (allowFindNextShortcut && m_LocalAssets.DoCharacterOffsetSelection())
                        Event.current.Use();
                }
            }
        }

        void DoOffsetSelectionSpecialKeys(int idx, int maxIndex)
        {
            float itemHeight = m_LocalAssets.m_Grid.itemSize.y + m_LocalAssets.m_Grid.verticalSpacing;
            int columns = m_LocalAssets.m_Grid.columns;

            switch (m_SelectionOffset)
            {
                case kPageUp:
                    // on OSX paging only scrolls the scrollbar
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        m_State.m_ScrollPosition.y -= m_TotalRect.height;
                        m_SelectionOffset = 0;
                        return;
                    }
                    else
                    {
                        m_SelectionOffset = -Mathf.RoundToInt(m_TotalRect.height / itemHeight) * columns;
                        // we want it to go to the very top row, but stay on same column
                        m_SelectionOffset = Mathf.Max(-Mathf.FloorToInt(idx / (float)columns) * columns, m_SelectionOffset);
                    }
                    break;
                case kPageDown:
                    // on OSX paging only scrolls the scrollbar
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        m_State.m_ScrollPosition.y += m_TotalRect.height;
                        m_SelectionOffset = 0;
                        return;
                    }
                    else
                    {
                        m_SelectionOffset = Mathf.RoundToInt(m_TotalRect.height / itemHeight) * columns;
                        // we want it to go to the very bottom row, but stay on same column
                        int remainingItems = maxIndex - idx;
                        m_SelectionOffset = Mathf.Min(Mathf.FloorToInt(remainingItems / (float)columns) * columns, m_SelectionOffset);
                    }
                    break;
                case kHome:
                    m_SelectionOffset = 0;
                    SetSelectedAssetByIdx(0); // assumes that 'none' is the first item
                    return;
                case kEnd:
                    m_SelectionOffset = maxIndex - idx;
                    break;
            }
        }

        void DoOffsetSelection()
        {
            if (m_SelectionOffset == 0)
                return;

            int maxGridIndex = GetMaxIdx();
            if (maxGridSize == -1)
                return; // no items

            int selectedAssetIdx = GetSelectedAssetIdx();
            selectedAssetIdx = selectedAssetIdx < 0 ? 0 : selectedAssetIdx; // default to first item

            DoOffsetSelectionSpecialKeys(selectedAssetIdx, maxGridIndex);

            // Special keys on some OSs will simply scroll and not change selection
            if (m_SelectionOffset == 0)
                return;

            int newGridIdx = selectedAssetIdx + m_SelectionOffset;
            m_SelectionOffset = 0;

            // We ignore the offset if newIdx is less than 0 or clamp to item list length.
            // This ensures that we stay on the same column at top or jump to the last item when navigating with keys.
            if (newGridIdx < 0)
                newGridIdx = selectedAssetIdx;
            else
                newGridIdx = Mathf.Min(newGridIdx, maxGridIndex);

            // If newIdx is on one of the two bottom rows we scroll to the bottom row because we might jump to another row
            // when navigating (because the last row is half empty). This is a usability decision and can be removed.
            int scrollGridIdx = newGridIdx;
            //if (newGridIdx >= m_Columns * m_Rows - m_Columns * 2)
            //  scrollGridIdx = maxGridIndex + 2 * m_Columns;

            SetSelectedAssetByIdx(scrollGridIdx);
        }

        public void OffsetSelection(int selectionOffset)
        {
            m_SelectionOffset = selectionOffset;
        }

        public void SelectFirst()
        {
            int startIndex = 0;
            if (m_ShowLocalAssetsOnly && m_LocalAssets.ShowNone && m_LocalAssets.ItemCount > 1)
                startIndex = 1;
            SetSelectedAssetByIdx(startIndex);
        }

        public int GetInstanceIDByIndex(int index)
        {
            int instanceID;
            if (m_LocalAssets.InstanceIdAtIndex(index, out instanceID))
                return instanceID;
            return 0;
        }

        void SetSelectedAssetByIdx(int selectedIdx)
        {
            // instanceID can be 0 if 'None' item is at index
            int instanceID;
            if (m_LocalAssets.InstanceIdAtIndex(selectedIdx, out instanceID))
            {
                Rect r = m_LocalAssets.m_Grid.CalcRect(selectedIdx, 0f);
                ScrollToPosition(AdjustRectForFraming(r));
                Repaint();

                int[] newSelection;
                if (IsLocalAssetsCurrentlySelected())
                    newSelection = m_LocalAssets.GetNewSelection(instanceID, false, true).ToArray(); // Handle multi selection
                else
                    newSelection = new[] {instanceID};                                              // If current selection is asset store asset do not allow multiselection

                SetSelection(newSelection, false);
                m_State.m_LastClickedInstanceID = instanceID;
                return;
            }


            selectedIdx -= m_LocalAssets.m_Grid.rows * m_LocalAssets.m_Grid.columns;
            float offset = m_LocalAssets.Height;

            foreach (AssetStoreGroup g in m_StoreAssets)
            {
                if (!g.Visible)
                {
                    offset += g.Height;
                    continue;
                }

                AssetStoreAsset asset = g.AssetAtIndex(selectedIdx);
                if (asset != null)
                {
                    Rect r = g.m_Grid.CalcRect(selectedIdx, offset);
                    ScrollToPosition(AdjustRectForFraming(r));
                    Repaint();
                    SetSelection(asset, false);
                    break;
                }
                selectedIdx -= g.m_Grid.rows * g.m_Grid.columns;
                offset += g.Height;
            }
        }

        void Reveal(int instanceID)
        {
            if (!AssetDatabase.Contains(instanceID))
                return;

            // We only show one level of subassets so just expand parent asset
            int mainAssetInstanceID = AssetDatabase.GetMainAssetInstanceID(AssetDatabase.GetAssetPath(instanceID));
            bool isSubAsset = mainAssetInstanceID != instanceID;
            if (isSubAsset)
                m_LocalAssets.ChangeExpandedState(mainAssetInstanceID, true);
        }

        // Frames only local assets
        public bool Frame(int instanceID, bool frame, bool ping)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            int index = -1;

            // Check if it is an asset we are creating
            if (GetCreateAssetUtility().IsCreatingNewAsset() && m_State.m_NewAssetIndexInList != -1)
                if (GetCreateAssetUtility().instanceID == instanceID)
                    index = m_State.m_NewAssetIndexInList;

            // Ensure instanceID is visible
            if (frame)
                Reveal(instanceID);

            // Check local assets
            if (index == -1)
                index = m_LocalAssets.IndexOf(instanceID);

            if (index != -1)
            {
                if (frame)
                {
                    float yOffset = 0f;
                    Rect r = m_LocalAssets.m_Grid.CalcRect(index, yOffset);
                    CenterRect(AdjustRectForFraming(r));
                    Repaint();
                }

                if (ping)
                    BeginPing(instanceID);
                return true;
            }

            return false;
        }

        int GetSelectedAssetIdx()
        {
            // Find index of selection
            int offsetIdx = m_LocalAssets.IndexOf(m_State.m_LastClickedInstanceID);
            if (offsetIdx != -1)
                return offsetIdx;

            offsetIdx = m_LocalAssets.m_Grid.rows * m_LocalAssets.m_Grid.columns;

            // Project or builtin asset not selected. Check asset store asset.
            if (AssetStoreAssetSelection.Count == 0)
                return -1;

            AssetStoreAsset asset = AssetStoreAssetSelection.GetFirstAsset();
            if (asset == null)
                return -1;
            int assetID = asset.id;

            foreach (AssetStoreGroup g in m_StoreAssets)
            {
                if (!g.Visible)
                    continue;

                int idx = g.IndexOf(assetID);
                if (idx != -1)
                    return offsetIdx + idx;

                offsetIdx += g.m_Grid.rows * g.m_Grid.columns;
            }

            return -1;
        }

        bool SkipGroup(Group group)
        {
            // We either show local assets or asset store results here
            if (m_ShowLocalAssetsOnly)
            {
                if (group is AssetStoreGroup)
                    return true;
            }
            else
            {
                if (group is LocalGroup)
                    return true;
            }

            return false;
        }

        int GetMaxIdx()
        {
            int groupLastIdx = 0;
            int groupSizesAccumulated = 0;
            int lastGroupSize = 0;

            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                if (!g.Visible)
                    continue;

                groupSizesAccumulated += lastGroupSize;
                lastGroupSize = g.m_Grid.rows * g.m_Grid.columns;
                groupLastIdx = g.ItemCount - 1;
            }
            int max = groupSizesAccumulated + groupLastIdx;
            return (lastGroupSize + max) == 0 ? -1 : max;
        }

        bool IsLocalAssetsCurrentlySelected()
        {
            int currentSelectedInstanceID = m_State.m_SelectedInstanceIDs.FirstOrDefault();
            if (currentSelectedInstanceID != 0)
            {
                int index = m_LocalAssets.IndexOf(currentSelectedInstanceID);
                return index != -1;
            }

            return false;
        }

        private void SetupData(bool forceReflow)
        {
            // Make sure the groups contains the correct assets to show
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;
                g.UpdateAssets();
            }

            if (forceReflow || Event.current.type == EventType.Repaint)
            {
                // Reflow according to number of items, scrollbar presence, item dims etc.
                Reflow();
            }
        }

        bool IsObjectSelector()
        {
            // ShowNone is only used in object select window
            return m_LocalAssets.ShowNone;
        }

        void HandleListArea()
        {
            SetupData(false);

            // Requery the Asset Store to get more assets if content area has changed size
            // so that more assets fits in there.

            // We don't want asset store asset in the object selector
            if (!IsObjectSelector() && !m_QueryInProgress)
            {
                bool needItems = m_StoreAssets.Exists(g => g.NeedItems);
                if (needItems || m_RequeryAssetStore)
                {
                    QueryAssetStore(); // need more data to fill required rows with asset store assets
                }
            }

            // Figure out height needed to contain all assets
            float totalContentsHeight = 0f;
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                totalContentsHeight += g.Height;

                // ShowNone is only used in object select window and we don't want asset store asset there
                if (m_LocalAssets.ShowNone)
                    break;
            }

            Rect scrollRect = m_TotalRect;
            Rect contentRect = new Rect(0, 0, 1, totalContentsHeight);
            bool scrollBarVisible = totalContentsHeight > m_TotalRect.height;

            m_VisibleRect = m_TotalRect;
            if (scrollBarVisible)
                m_VisibleRect.width -= kSpaceForScrollBar;


            double timeNow = EditorApplication.timeSinceStartup;
            m_LastScrollPosition = m_State.m_ScrollPosition;

            bool needRepaint = false;
            m_State.m_ScrollPosition = GUI.BeginScrollView(scrollRect, m_State.m_ScrollPosition, contentRect);
            {
                Vector2 scrollPos = m_State.m_ScrollPosition; // Copy scroll pos since the draw calls may change it

                if (m_LastScrollPosition != m_State.m_ScrollPosition)
                    LastScrollTime = timeNow;

                float yOffset = 0f;
                int rowsBeingUsed = 0;
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    // rect contains the offset rect where the group should draw
                    g.Draw(yOffset, scrollPos, ref rowsBeingUsed);
                    needRepaint = needRepaint || g.NeedsRepaint;
                    yOffset += g.Height;

                    // ShowNone is only used in object select window and we don't want asset store asset there
                    if (m_LocalAssets.ShowNone)
                        break;
                }

                HandlePing();
                if (needRepaint)
                    Repaint();
            } GUI.EndScrollView();


            // We delay this resizing of cache until after the resized grid has been
            // draw in order to let the cache have the correct lastUsed timestamps on the
            // cached images. This is important when the cache is decreased in order not
            // to dispose the wrong cache entries
            if (m_ResizePreviewCacheTo > 0 && AssetStorePreviewManager.MaxCachedImages != m_ResizePreviewCacheTo)
                AssetStorePreviewManager.MaxCachedImages = m_ResizePreviewCacheTo;

            if (Event.current.type == EventType.Repaint)
                AssetStorePreviewManager.AbortOlderThan(timeNow);

            if (!m_ShowLocalAssetsOnly && !string.IsNullOrEmpty(m_AssetStoreError))
            {
                Vector2 size = EditorStyles.label.CalcSize(s_Styles.m_AssetStoreNotAvailableText);
                Rect textRect = new Rect(m_TotalRect.x + 2f + Mathf.Max(0, (m_TotalRect.width - size.x) * 0.5f), m_TotalRect.y + 10f, size.x, 20f);
                using (new EditorGUI.DisabledScope(true))
                {
                    GUI.Label(textRect, s_Styles.m_AssetStoreNotAvailableText, EditorStyles.label);
                }
            }
        }

        bool IsListMode()
        {
            if (allowMultiSelect)
                return (gridSize == kListLineHeight); // ProjectBrowser (should auto change layout on content but entirely user controlled)
            else
                return (gridSize == kListLineHeight) || !CanShowThumbnails(); // ObjectSelector
        }

        void Reflow()
        {
            if (gridSize < 20)
                gridSize = m_MinGridSize;
            else if (gridSize < m_MinIconSize)
                gridSize = m_MinIconSize;

            // We're in list mode.
            if (IsListMode())
            {
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    g.ListMode = true;
                    UpdateGroupSizes(g);

                    // ShowNone is only used in object select window and we don't want aiy sset store asset there
                    if (m_LocalAssets.ShowNone)
                        break;
                }

                m_ResizePreviewCacheTo = Mathf.CeilToInt((float)m_TotalRect.height / kListLineHeight) + 10;
            }
            // we're in thumbnail mode
            else
            {
                // Grid without scrollbar
                float totalHeight = 0;
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    g.ListMode = false;
                    UpdateGroupSizes(g);

                    totalHeight += g.Height;

                    // ShowNone is only used in object select window and we don't want asset store asset there
                    if (m_LocalAssets.ShowNone)
                        break;
                }

                // Grid with scrollbar
                bool scrollbarVisible = m_TotalRect.height < totalHeight;
                if (scrollbarVisible)
                {
                    // Make room for the scrollbar
                    foreach (Group g in m_Groups)
                    {
                        if (SkipGroup(g))
                            continue;

                        g.m_Grid.fixedWidth = m_TotalRect.width - kSpaceForScrollBar;
                        g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.m_Grid.CalcRows(g.ItemsWantedShown));
                        g.UpdateHeight();

                        // ShowNone is only used in object select window and we don't want asset store asset there
                        if (m_LocalAssets.ShowNone)
                            break;
                    }
                }

                int maxVisibleItems = GetMaxNumVisibleItems();

                m_ResizePreviewCacheTo = maxVisibleItems * 2;

                AssetPreview.SetPreviewTextureCacheSize(maxVisibleItems * 2 + 30, GetAssetPreviewManagerID());
            }
        }

        void UpdateGroupSizes(Group g)
        {
            if (g.ListMode)
            {
                g.m_Grid.fixedWidth = m_VisibleRect.width;
                g.m_Grid.itemSize = new Vector2(m_VisibleRect.width, kListLineHeight);
                g.m_Grid.topMargin = 0f;
                g.m_Grid.bottomMargin = 0f;
                g.m_Grid.leftMargin = 0f;
                g.m_Grid.rightMargin = 0f;
                g.m_Grid.verticalSpacing = 0f;
                g.m_Grid.minHorizontalSpacing = 0f;
                g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.ItemsWantedShown);

                g.UpdateHeight();
            }
            else
            {
                g.m_Grid.fixedWidth = m_TotalRect.width;
                g.m_Grid.itemSize = new Vector2(gridSize, gridSize + 14);
                g.m_Grid.topMargin = 10f;
                g.m_Grid.bottomMargin = 10f;
                g.m_Grid.leftMargin = 10f;
                g.m_Grid.rightMargin = 10f;
                g.m_Grid.verticalSpacing = 15f;
                g.m_Grid.minHorizontalSpacing = 12f;
                g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.m_Grid.CalcRows(g.ItemsWantedShown));

                g.UpdateHeight();
            }
        }

        int GetMaxNumVisibleItems()
        {
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                return g.m_Grid.GetMaxVisibleItems(m_TotalRect.height);
            }

            Assert.IsTrue(false, "Unhandled group");
            return 0;
        }

        static Rect AdjustRectForFraming(Rect r)
        {
            r.height += (s_Styles.resultsGridLabel.fixedHeight * 2);
            r.y -= s_Styles.resultsGridLabel.fixedHeight;
            return r;
        }

        void CenterRect(Rect r)
        {
            float middle = (r.yMax + r.yMin) / 2;
            float middleVisibleRect = m_TotalRect.height / 2;

            m_State.m_ScrollPosition.y = middle - middleVisibleRect;

            // Ensure clamped
            ScrollToPosition(r);
        }

        void ScrollToPosition(Rect r)
        {
            float top = r.y;
            float bottom = r.yMax;
            float viewHeight = m_TotalRect.height;

            if (bottom > viewHeight + m_State.m_ScrollPosition.y)
            {
                m_State.m_ScrollPosition.y = bottom - viewHeight;
            }
            if (top < m_State.m_ScrollPosition.y)
            {
                m_State.m_ScrollPosition.y = top;
            }

            m_State.m_ScrollPosition.y = Mathf.Max(m_State.m_ScrollPosition.y, 0f);
        }

        public void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup > m_NextDirtyCheck && m_LocalAssets.IsAnyLastRenderedAssetsDirty())
            {
                // If an asset is dirty we ensure to get a updated preview by clearing cache of temporary previews
                AssetPreview.ClearTemporaryAssetPreviews();
                Repaint();
                m_NextDirtyCheck = EditorApplication.timeSinceStartup + 0.77;
            }

            if (AssetStorePreviewManager.CheckRepaint())
                Repaint();
        }

        void ClearCroppedLabelCache()
        {
            m_InstanceIDToCroppedNameMap.Clear();
        }

        protected string GetCroppedLabelText(int instanceID, string fullText, float cropWidth)
        {
            // Clear when width changes
            if (m_WidthUsedForCroppingName != (int)cropWidth)
                ClearCroppedLabelCache();

            string croppedText;
            if (!m_InstanceIDToCroppedNameMap.TryGetValue(instanceID, out croppedText))
            {
                // Ensure to clean up once in a while
                if (m_InstanceIDToCroppedNameMap.Count > GetMaxNumVisibleItems() * 2 + 30)
                    ClearCroppedLabelCache();

                // Check if we need to crop
                int characterCountVisible = s_Styles.resultsGridLabel.GetNumCharactersThatFitWithinWidth(fullText, cropWidth);
                if (characterCountVisible == -1)
                {
                    Repaint();
                    return fullText; // failed: do not cache result
                }

                if (characterCountVisible > 1 && characterCountVisible != fullText.Length)
                    croppedText = fullText.Substring(0, characterCountVisible - 1) + ("\u2026"); // 'horizontal ellipsis' (U+2026) is: ...
                else
                    croppedText = fullText;

                m_InstanceIDToCroppedNameMap[instanceID] = croppedText;
                m_WidthUsedForCroppingName = (int)cropWidth;
            }
            return croppedText;
        }

        public bool IsShowing(int instanceID)
        {
            return m_LocalAssets.IndexOf(instanceID) >= 0;
        }

        public bool IsShowingAny(int[] instanceIDs)
        {
            if (instanceIDs.Length == 0)
                return false;

            foreach (int instanceID in instanceIDs)
                if (IsShowing(instanceID))
                    return true;

            return false;
        }

        protected Texture GetIconByInstanceID(int instanceID)
        {
            Texture icon = null;
            if (instanceID != 0)
            {
                string path = AssetDatabase.GetAssetPath(instanceID);
                icon = AssetDatabase.GetCachedIcon(path);
            }
            return icon;
        }

        internal int GetAssetPreviewManagerID()
        {
            return m_Owner.GetInstanceID();
        }

        // Pings only local assets
        public void BeginPing(int instanceID)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            // Check local assets
            int index =  m_LocalAssets.IndexOf(instanceID);

            if (index != -1)
            {
                string name = null;
                HierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.Assets);
                if (hierarchyProperty.Find(instanceID, null))
                {
                    name = hierarchyProperty.name;
                }
                if (name == null)
                    return;

                m_Ping.m_TimeStart = Time.realtimeSinceStartup;
                m_Ping.m_AvailableWidth = m_VisibleRect.width;
                m_pingIndex = index;

                float vcPadding = s_VCEnabled ? k_ListModeVersionControlOverlayPadding : 0f;

                GUIContent cont = new GUIContent(m_LocalAssets.ListMode ? name : GetCroppedLabelText(instanceID, name, m_WidthUsedForCroppingName));
                string label = cont.text;

                if (m_LocalAssets.ListMode)
                {
                    const float iconWidth = 16;
                    m_Ping.m_PingStyle = s_Styles.ping;
                    Vector2 pingLabelSize = m_Ping.m_PingStyle.CalcSize(cont);
                    m_Ping.m_ContentRect.width = pingLabelSize.x + vcPadding + iconWidth;
                    m_Ping.m_ContentRect.height = pingLabelSize.y;
                    m_LeftPaddingForPinging = hierarchyProperty.isMainRepresentation ? LocalGroup.k_ListModeLeftPadding : LocalGroup.k_ListModeLeftPaddingForSubAssets;
                    FilteredHierarchy.FilterResult res = m_LocalAssets.LookupByInstanceID(instanceID);
                    m_Ping.m_ContentDraw = (Rect r) =>
                        {
                            ObjectListArea.LocalGroup.DrawIconAndLabel(r, res, label, hierarchyProperty.icon, false, false);
                        };
                }
                else
                {
                    m_Ping.m_PingStyle = s_Styles.miniPing;
                    Vector2 pingLabelSize = m_Ping.m_PingStyle.CalcSize(cont);
                    m_Ping.m_ContentRect.width = pingLabelSize.x;
                    m_Ping.m_ContentRect.height = pingLabelSize.y;
                    m_Ping.m_ContentDraw = (Rect r) =>
                        {
                            // We need to temporary adjust style to render into content rect (org anchor is middle-centered)
                            TextAnchor orgAnchor = s_Styles.resultsGridLabel.alignment;
                            s_Styles.resultsGridLabel.alignment = TextAnchor.UpperLeft;
                            s_Styles.resultsGridLabel.Draw(r, label, false, false, false, false);
                            s_Styles.resultsGridLabel.alignment = orgAnchor;
                        };
                }
                Vector2 pos = CalculatePingPosition();
                m_Ping.m_ContentRect.x = pos.x;
                m_Ping.m_ContentRect.y = pos.y;

                Repaint();
            }
        }

        public void EndPing()
        {
            m_Ping.m_TimeStart = -1f;
        }

        void HandlePing()
        {
            // We need to update m_Ping.m_ContentTopLeft in icon mode. The position might change if user resizes the window while pinging
            if (m_Ping.isPinging && !m_LocalAssets.ListMode)
            {
                Vector2 pos = CalculatePingPosition();
                m_Ping.m_ContentRect.x = pos.x;
                m_Ping.m_ContentRect.y = pos.y;
            }
            m_Ping.HandlePing();

            if (m_Ping.isPinging)
                Repaint();
        }

        Vector2 CalculatePingPosition()
        {
            Rect gridRect = m_LocalAssets.m_Grid.CalcRect(m_pingIndex, 0f);

            if (m_LocalAssets.ListMode)
            {
                return new Vector2(m_LeftPaddingForPinging, gridRect.y);
            }
            else
            {
                // TODO: Find out why Y offset 3 is needed
                float width = m_Ping.m_ContentRect.width;
                return new Vector2(gridRect.center.x - width / 2f + m_Ping.m_PingStyle.padding.left, gridRect.yMax - s_Styles.resultsGridLabel.fixedHeight + 3);
            }
        }
    }
}  // namespace UnityEditor
