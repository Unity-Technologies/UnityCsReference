// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor
{
    //If you change the class name, make sure you update the entry
    //13905 in ResourceManager.cpp for RegisterBuiltinEditorScript
    internal class ImportActivityWindow : EditorWindow
    {
        public static string kTimeStampFormat = "dd-MM-yyyy hh:mm:ss";
        public static ImportActivityWindow m_Instance = null;
        public static Vector2 kIdealWindowSize = new Vector2(1280, 720);

        public static void OpenFromPropertyEditor(Object inspectedObject)
        {
            var path = AssetDatabase.GetAssetPath(inspectedObject);
            var guid = AssetDatabase.AssetPathToGUID(path);
            OpenWindow(guid, true);
        }

        [MenuItem("Assets/View in Import Activity Window", secondaryPriority = 2)]
        public static void ViewInImportActivity()
        {
            var guid = Selection.assetGUIDs.Length > 0 ? Selection.assetGUIDs[0] : "";
            OpenWindow(guid, true);
        }

        [MenuItem("Window/Analysis/Import Activity")]
        public static void ShowWindow()
        {
            OpenWindow();
        }

        private static void OpenWindow(string selectedObjectGuid = "", bool openViaRightClick = false)
        {
            // Docking new winow to some tab as of some wider window, since it feels more intuitive to start working with such layout
            // instead of tiny floating window. Also it should not be docked on top of Project Browser, since we need to select assets
            var window = GetWindow<ImportActivityWindow>();
            m_Instance = window;
            window.titleContent.text = "Import Activity";
            if (window.IsInitialized)
            {
                window.RefreshAllLists(m_Instance.m_ImportActivityState.rightContentState);
                window.FocusOnSelectedItem(selectedObjectGuid);
            }
            else
            {
                window.InitWithDimensions(window.position, selectedObjectGuid, openViaRightClick);

                window.minSize = CalculateWindowMinSize(Screen.currentResolution);
            }
        }

        internal static Vector2 CalculateWindowMinSize(Resolution resolution)
        {
            //If screen is big enough, return the ideal size
            if (resolution.width >= kIdealWindowSize.x && resolution.height >= kIdealWindowSize.y)
            {
                return kIdealWindowSize;
            }

            //keep aspect ratio
            var availableWidth = 0.9f * resolution.width;
            var targetHeight = (kIdealWindowSize.y / kIdealWindowSize.x) * availableWidth;
            var targetDimensions = new Vector2(availableWidth, targetHeight);
            return targetDimensions;
        }

        internal enum RightContentState
        {
            ShowOverview,
            ShowSelectedItem
        }

        internal RightContentState m_RightContentState;

        private struct Column
        {
            public string Name;
            public int Width;

            public Column(string name, int width)
            {
                Name = name;
                Width = width;
            }
        }

        internal struct SelectedItemContainers
        {
            public VisualElement SelectedItemRoot;
            public TwoPaneSplitView SelectedItemSplitView;

            //Right side
            public VisualElement SelectedItemView;
            public Label assetName;
            public (VisualElement container, Label header, IMGUIContainer content, string path, Object loadedAsset)assetWithObjectField;
            public (VisualElement container, Label header, Label content)guid;
            public (VisualElement container, Label header, Label content)assetSize;
            public (VisualElement container, Label header, Label content)path;
            public (VisualElement container, Label header, Label content)editorRevision;
            public (VisualElement container, Label header, Label content)timeStamp;
            public (VisualElement container, Label header, Label content)duration;
            public (VisualElement container, IMGUIContainer content, ArtifactBrowserTreeViewNested<ArtifactDifferenceReporter.ArtifactInfoDifference> treeView, Label header)reasonsForImport;

            public (VisualElement container, Label header, Label content)producedArtifacts;
            public Label dependencies;

            //Left side
            public VisualElement PreviousRevisionsContainer;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView)previousRevisions;
        }

        internal struct ProjectOverviewContainer
        {
            public VisualElement SummaryView;
            public Label overView;
            public Label mostDependenciesHeader;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView)mostDependencies;

            public Label longestDurationHeader;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView)longestDuration;
        }

        [Serializable]
        internal struct ToolBarContainer
        {
            public (IMGUIContainer container, ArtifactBrowserToolbar toolbar)options;
        }

        internal class ArtifactInfoTreeViewItem
        {
            private string m_ShortenedName = null;
            private string m_ImporterName = null;
            private string m_ImportDuration = null;

            public ArtifactInfo artifactInfo;

            public string shortenedName
            {
                get
                {
                    if (string.IsNullOrEmpty(m_ShortenedName))
                        m_ShortenedName = GetShortenedAssetName(artifactInfo.assetPath);
                    return m_ShortenedName;
                }
            }

            public string importerName
            {
                get
                {
                    if (string.IsNullOrEmpty(m_ImporterName))
                        m_ImporterName = artifactInfo.importStats.importerClassName;
                    return m_ImporterName;
                }
            }

            public string importDuration
            {
                get
                {
                    if (string.IsNullOrEmpty(m_ImportDuration))
                        m_ImportDuration = Math.Max(1, artifactInfo.importStats.importTimeMicroseconds / 1000)
                            .ToString("###,###,###");
                    return m_ImportDuration;
                }
            }

            private string GetShortenedAssetName(string importStatsAssetPath)
            {
                //30 chars max, then asset name
                var path = importStatsAssetPath;

                var start = -1;
                for (int i = path.Length - 1; i >= 0; --i)
                {
                    if (path[i] == '/')
                    {
                        start = i;
                        break;
                    }
                }

                //Assets or Packages folders
                if (start == -1)
                    return importStatsAssetPath;
                //Move 1 character past the "slash"
                start++;
                var assetName = path.Substring(start, path.Length - start);
                return assetName;
            }
        }

        internal static ScalableGUIContent s_OpenFolderIcon;
        internal static ScalableGUIContent s_EmptyFolderIcon;

        public static float k_IconWidth = 16f;
        public const int kLeftPadding = 12;
        public const int kRightAlignPadding = 8;
        private const float k_LabelColorDark = 38 / 255f;
        private const float k_LabelColorLight = 165 / 255f;
        private Color LabelColor => EditorGUIUtility.isProSkin ?
        new Color(k_LabelColorDark, k_LabelColorDark, k_LabelColorDark) :
        new Color(k_LabelColorLight, k_LabelColorLight, k_LabelColorLight);

        private const float k_ListHeaderColorDark = 62 / 255f;
        private const float k_ListHeaderColorLight = 221 / 255f;
        private Color ListHeaderColor => EditorGUIUtility.isProSkin ?
        new Color(k_ListHeaderColorDark, k_ListHeaderColorDark, k_ListHeaderColorDark) :
        new Color(k_ListHeaderColorLight, k_ListHeaderColorLight, k_ListHeaderColorLight);

        private VisualElement m_LeftContent;
        internal VisualElement m_RightContent;

        internal string m_SelectedAssetGuid;
        private ArtifactInfo m_SelectedArtifactInfo;

        // Data lists
        private readonly List<ArtifactInfoTreeViewItem> m_AllAssetsList = new List<ArtifactInfoTreeViewItem>();
        private readonly List<(string dependencyName, ArtifactInfoDependency dependency)> m_DependenciesList = new List<(string, ArtifactInfoDependency)>();
        private readonly List<ArtifactInfoProducedFiles> m_ProducedFilesList = new List<ArtifactInfoProducedFiles>();
        private readonly List<ArtifactInfoTreeViewItem> m_MostDependencyAssets = new List<ArtifactInfoTreeViewItem>();
        private readonly List<ArtifactInfoTreeViewItem> m_LongestDurationAssets = new List<ArtifactInfoTreeViewItem>();
        private List<ArtifactInfoTreeViewItem> m_PreviousRevisionsList = new List<ArtifactInfoTreeViewItem>();
        private List<ArtifactDifferenceReporter.ArtifactInfoDifference> m_ReasonsToReimportList = new List<ArtifactDifferenceReporter.ArtifactInfoDifference>();

        // List views
        internal ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> m_AllAssetsListView;
        private ArtifactBrowserTreeView<(string, ArtifactInfoDependency)> m_DependenciesListView;
        private ArtifactBrowserTreeView<ArtifactInfoProducedFiles> m_ProducedFilesListView;

        //Search
        //private ExposablePopupMenu m_SearchAreaMenu;

        private Rect m_DesiredDimensions;
        private TwoPaneSplitView m_SplitView;

        internal SelectedItemContainers m_ItemContainers;
        internal ProjectOverviewContainer m_Overview;
        private IMGUIContainer m_DependenciesListViewContainer;
        private IMGUIContainer m_ProducedFilesListViewContainer;
        private IMGUIContainer m_AllAssetsListViewContainer;
        internal ToolBarContainer m_Toolbar;

        private Dictionary<string, Texture> m_IconCache;


        [SerializeField]
        public bool IsInitialized { get; set; }


        public void ClearAll()
        {
            if (rootVisualElement != null)
                rootVisualElement.Clear();
        }

        public void RefreshAllLists(RightContentState state)
        {
            RefreshAllListsExceptProducedArtifacts();
            UpdateSelectedView(state);
        }

        public void InitWithDimensions(Rect windowPosition, string selectedObjectGuid = "", bool openViaRightClick = false)
        {
            IsInitialized = false;

            if (m_ImportActivityState == null)
                m_ImportActivityState = new ImportActivityState();

            ClearAll();

            SetupRoot();
            CreateAllAssetsList();
            CreateOverview(windowPosition);
            CreateListViews();

            CreateToolBar(windowPosition);
            CreateSplitViewContainers(windowPosition);

            CreateSelectedItemContainersWithSplitView(windowPosition);
            UpdateSelectedItemView(windowPosition);

            CreateListViewContainers(windowPosition);
            InitIconCache();

            IsInitialized = true;

            RefreshAllLists(m_ImportActivityState.rightContentState);

            m_AllAssetsListView.multiColumnHeader.state.sortedColumnIndex = m_ImportActivityState.allAssetsState.sortedColumnIndex;

            RestoreListViewSelection();
            if (m_RightContentState == RightContentState.ShowSelectedItem || openViaRightClick)
                FocusOnSelectedItem(selectedObjectGuid, m_ImportActivityState.selectedArtifactID);

            m_DesiredDimensions = new Rect(windowPosition.x, windowPosition.y, windowPosition.width, windowPosition.height);
        }

        internal override void OnResized()
        {
            if (!IsInitialized)
                return;

            if (m_Toolbar.options.toolbar.ShowPreviousImports)
            {
                m_ItemContainers.SelectedItemSplitView.style.height = position.height;
                m_ItemContainers.PreviousRevisionsContainer.style.height = position.height;
            }
        }

        private void UpdateSelectedItemView(Rect windowPosition)
        {
            m_ItemContainers.SelectedItemRoot.Clear();

            if (m_Toolbar.options.toolbar.ShowPreviousImports)
            {
                m_ItemContainers.SelectedItemSplitView.Clear();
                var targetDimensions = 360;

                InstantiateSelectedItemSplitView(targetDimensions);

                m_ItemContainers.SelectedItemSplitView.style.height = windowPosition.height;
                m_ItemContainers.SelectedItemView.style.minWidth = 480;

                m_ItemContainers.SelectedItemView.AddToClassList("split-container");
                m_ItemContainers.PreviousRevisionsContainer.AddToClassList("split-container");

                m_ItemContainers.SelectedItemSplitView.Add(m_ItemContainers.PreviousRevisionsContainer);
                m_ItemContainers.SelectedItemSplitView.Add(m_ItemContainers.SelectedItemView);


                m_ItemContainers.SelectedItemRoot.Add(m_ItemContainers.SelectedItemSplitView);
                m_ItemContainers.SelectedItemRoot.style.minWidth = 580;
            }
            else
            {
                m_ItemContainers.SelectedItemView = new VisualElement();
                m_ItemContainers.SelectedItemView.style.minWidth = 480;
                //Rebuild it
                AddAllContainertoSelectedItemsView();

                //Add it to the root here
                m_ItemContainers.SelectedItemRoot.Add(m_ItemContainers.SelectedItemView);

                m_AllAssetsListView.ClearPrevIndices();
            }
        }

        private void InitIconCache()
        {
            m_IconCache = new Dictionary<string, Texture>();
            m_IconCache.Add(".fbx", null);
            m_IconCache.Add(".uss", null);
            m_IconCache.Add(".uxml", null);
            m_IconCache.Add(".cs", null);
            m_IconCache.Add(".md", null);
            m_IconCache.Add(".json", null);
            m_IconCache.Add(".txt", null);
            m_IconCache.Add(".shader", null);
            m_IconCache.Add(".compute", null);
            m_IconCache.Add(".ttf", null);
        }

        private void CreateToolBar(Rect windowPosition)
        {
            //Toolbar
            var toolbar = new VisualElement();
            m_Toolbar = new ToolBarContainer();

            if (m_Toolbar.options.toolbar == null)
                m_Toolbar.options.toolbar = new ArtifactBrowserToolbar(windowPosition, OnOverviewClicked, UpdateSelectedItemView, OnToolbarSearch, OnShowPreviewImporterRevisions, m_ImportActivityState);

            m_Toolbar.options.container = new IMGUIContainer(m_Toolbar.options.toolbar.OnGUI);

            toolbar.Add(m_Toolbar.options.container);

            rootVisualElement.Add(toolbar);
        }

        private void OnShowPreviewImporterRevisions()
        {
            if (m_SelectedArtifactInfo == null)
                return;

            UpdatePreviousRevisionsForSelectedArtifact(m_SelectedArtifactInfo);
            OnSelectRevision(0);
        }

        internal void OnToolbarSearch(string search)
        {
            m_AllAssetsListView.searchString = search;
            m_AllAssetsListView.state.searchString = search;
        }

        private void CreateOverview(Rect windowPosition)
        {
            m_Overview = new ProjectOverviewContainer();
            m_Overview.overView = CreateListLabel("Overview", TextAnchor.MiddleCenter, 0, FontStyle.Bold, 16);
            m_Overview.overView.style.fontSize = 20;
            m_Overview.overView.style.borderBottomColor = ListHeaderColor;

            m_Overview.mostDependenciesHeader = CreateListLabel("Most Dependencies", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 16, 8);
            m_Overview.mostDependenciesHeader.style.borderBottomColor = ListHeaderColor;

            var mostDependenciesColumns = CreateColumns(new Column("Asset Path", 360), new Column("Total Dependencies", 125));
            var longestDurationColumns = CreateColumns(new Column("Asset Path", 360), new Column("Import Duration (ms)", 125));

            GetMostDependencyAssets();
            m_Overview.mostDependencies.treeView = CreateTreeView(m_Overview.mostDependencies.treeView,
                m_MostDependencyAssets, mostDependenciesColumns, MostDependenciesSelector);

            m_Overview.mostDependencies.treeView.OnDoubleClickedItem = OnMostDependenciesDoubleClickedItem;
            m_Overview.mostDependencies.treeView.CanSort = false;
            m_Overview.mostDependencies.treeView.CellGUICallback = CellGUIForMostDependencies;

            m_Overview.mostDependencies.container = new IMGUIContainer(m_Overview.mostDependencies.treeView.OnGUI);
            m_Overview.mostDependencies.container.style.maxHeight = windowPosition.height * 0.25f;
            m_Overview.mostDependencies.container.style.minHeight = windowPosition.height * 0.25f;

            GetLongestDurationAssets();
            m_Overview.longestDurationHeader = CreateListLabel("Longest Import Duration", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 16, 8);
            m_Overview.longestDurationHeader.style.borderBottomColor = ListHeaderColor;

            m_Overview.longestDuration.treeView = CreateTreeView(m_Overview.longestDuration.treeView,
                m_LongestDurationAssets, longestDurationColumns, LongestDurationSelector);

            m_Overview.longestDuration.treeView.OnDoubleClickedItem = OnLongestDurationDoubleClickedItem;
            m_Overview.longestDuration.treeView.CanSort = false;
            m_Overview.longestDuration.treeView.CellGUICallback = CellGUIForLongestDuration;

            m_Overview.longestDuration.container = new IMGUIContainer(m_Overview.longestDuration.treeView.OnGUI);
            m_Overview.longestDuration.container.style.maxHeight = windowPosition.height * 0.25f;
            m_Overview.longestDuration.container.style.minHeight = windowPosition.height * 0.25f;

            m_Overview.SummaryView = new VisualElement();
            m_Overview.SummaryView.Add(m_Overview.overView);
            m_Overview.SummaryView.Add(m_Overview.mostDependenciesHeader);
            m_Overview.SummaryView.Add(m_Overview.mostDependencies.container);
            m_Overview.SummaryView.Add(m_Overview.longestDurationHeader);
            m_Overview.SummaryView.Add(m_Overview.longestDuration.container);
        }

        private void OnLongestDurationDoubleClickedItem(int id)
        {
            if (id - 1 < 0)
                return;

            var selectedItem = m_LongestDurationAssets.ElementAt(id - 1);
            var selectedItemGUID = selectedItem.artifactInfo.artifactKey.guid;
            FocusOnSelectedItem(selectedItemGUID.ToString());
        }

        private void OnMostDependenciesDoubleClickedItem(int id)
        {
            if (id - 1 < 0)
                return;

            var selectedItem = m_MostDependencyAssets.ElementAt(id - 1);
            var selectedItemGUID = selectedItem.artifactInfo.artifactKey.guid;
            FocusOnSelectedItem(selectedItemGUID.ToString());
        }

        private void GetLongestDurationAssets()
        {
            var artifactInfoTreeViewItems = new List<ArtifactInfoTreeViewItem>(m_StartupData.longestDurationAssets.Length);
            for (int i = 0; i < m_StartupData.longestDurationAssets.Length; ++i)
            {
                artifactInfoTreeViewItems.Add(new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = m_StartupData.longestDurationAssets[i]
                });
            }

            m_LongestDurationAssets.Clear();
            m_LongestDurationAssets.AddRange(artifactInfoTreeViewItems);
        }

        private void GetMostDependencyAssets()
        {
            var artifactInfoTreeViewItems = new List<ArtifactInfoTreeViewItem>(m_StartupData.mostDependencyAssets.Length);
            for (int i = 0; i < m_StartupData.mostDependencyAssets.Length; ++i)
            {
                artifactInfoTreeViewItems.Add(new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = m_StartupData.mostDependencyAssets[i]
                });
            }

            m_MostDependencyAssets.Clear();
            m_MostDependencyAssets.AddRange(artifactInfoTreeViewItems);
        }

        private void CreateAllAssetsList()
        {
            //Get all assets
            m_StartupData.Initialize();
            var allCurrentRevisions = GetAllCurrentRevisions();

            var artifactInfoTreeViewItems = new List<ArtifactInfoTreeViewItem>(allCurrentRevisions.Length);
            for (int i = 0; i < allCurrentRevisions.Length; ++i)
            {
                artifactInfoTreeViewItems.Add(new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = allCurrentRevisions[i]
                });
            }
            m_AllAssetsList.AddRange(artifactInfoTreeViewItems);
        }

        private VisualElement CreateHeaderAndContentLabelContainer(Rect windowPosition, string headerText, string contentText, out Label headerLabel, out Label contentLabel)
        {
            var availableSpace = 620;
            var headerLength = availableSpace * 0.2f;
            var contentLength = availableSpace - headerLength;
            headerLabel = CreateListLabel(headerText, TextAnchor.MiddleLeft, 0, FontStyle.Normal);
            headerLabel.style.flexBasis = new StyleLength(headerLength);
            headerLabel.style.minHeight = 20;
            headerLabel.style.maxHeight = 20;

            contentLabel = CreateListLabel(contentText, TextAnchor.MiddleLeft, 0, FontStyle.Normal);
            contentLabel.style.paddingLeft = 0;
            contentLabel.style.flexBasis = new StyleLength(contentLength);
            contentLabel.style.flexGrow = new StyleFloat(1);
            contentLabel.style.minHeight = 20;
            contentLabel.style.maxHeight = 20;

            var assetPathSubContainer = new VisualElement();
            assetPathSubContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            assetPathSubContainer.style.minHeight = 20;
            assetPathSubContainer.style.minWidth = 300;
            assetPathSubContainer.Add(headerLabel);
            assetPathSubContainer.Add(contentLabel);

            return assetPathSubContainer;
        }

        private void CreateSelectedItemContainersWithSplitView(Rect windowPosition)
        {
            m_ItemContainers.SelectedItemRoot = new VisualElement();
            var targetDimensions = 300;
            InstantiateSelectedItemSplitView(targetDimensions);
            m_ItemContainers.SelectedItemRoot.style.minWidth = 580;
            m_ItemContainers.SelectedItemSplitView.style.minWidth = 580;

            CreatePreviousRevisionsContainers(windowPosition);
            CreateSelectedItemRightSideContainers(windowPosition);

            m_ItemContainers.SelectedItemSplitView.Add(m_ItemContainers.PreviousRevisionsContainer);
            m_ItemContainers.SelectedItemSplitView.Add(m_ItemContainers.SelectedItemView);

            m_ItemContainers.SelectedItemRoot.Add(m_ItemContainers.SelectedItemSplitView);
        }

        private void InstantiateSelectedItemSplitView(int targetDimensions)
        {
            m_ItemContainers.SelectedItemSplitView = new TwoPaneSplitView(0, targetDimensions, TwoPaneSplitViewOrientation.Horizontal);
        }

        private void CreatePreviousRevisionsContainers(Rect windowPosition)
        {
            m_ItemContainers.PreviousRevisionsContainer = new VisualElement();

            var previousRevisionsColumns = CreateColumns(new Column("Imported", 120), new Column("Artifact ID", 150), new Column("Importer", 150));

            var previousRevisionsVisibleColumns = m_ImportActivityState.previousRevisionsState.GetVisibleColumns();
            m_ItemContainers.previousRevisions.treeView = CreateTreeView(m_ItemContainers.previousRevisions.treeView,
                m_PreviousRevisionsList, previousRevisionsColumns,
                PreviousRevisionSelector, GetPreviousRevisionSelectorSortCallbacks(), false, false, false, previousRevisionsVisibleColumns);

            m_ItemContainers.previousRevisions.treeView.SelectionChangedCallback += OnSelectRevision;
            m_ItemContainers.previousRevisions.treeView.CellGUICallback = CellGUIForPreviousRevisions;
            m_ItemContainers.previousRevisions.treeView.SetSorting(m_ImportActivityState.previousRevisionsState.sortedColumnIndex, m_ImportActivityState.previousRevisionsState.sortAscending);

            m_ItemContainers.previousRevisions.container = new IMGUIContainer(m_ItemContainers.previousRevisions.treeView.OnGUI);
            m_ItemContainers.PreviousRevisionsContainer.Add(m_ItemContainers.previousRevisions.container);
            m_ItemContainers.PreviousRevisionsContainer.style.minWidth = 100;
        }

        private void OnSelectRevision(int index)
        {
            if (index < 0)
                return;

            var selected = m_PreviousRevisionsList[index];
            m_SelectedArtifactInfo = selected.artifactInfo;
            UpdateViewToSelectedArtifactInfo(m_SelectedArtifactInfo);
        }

        private IEnumerable<ArtifactInfo> GatherPreviousRevisionsForSelectedArtifact(ArtifactInfo selectedAsset)
        {
            var revisions = AssetDatabase.GetArtifactInfos(selectedAsset.artifactKey.guid);

            if (m_Toolbar.options.toolbar.ShowPreviewImporterRevisions)
                return revisions;

            //filter out preview importers
            return revisions.Where(revision => !revision.artifactKey.importerType.ToString().EndsWith("PreviewImporter", StringComparison.Ordinal));
        }

        private void CreateSelectedItemRightSideContainers(Rect windowPosition)
        {
            m_ItemContainers.SelectedItemView = new VisualElement();
            m_ItemContainers.SelectedItemView.style.minWidth = 480;
            m_ItemContainers.assetName = CreateListLabel("", TextAnchor.MiddleLeft, 0);
            m_ItemContainers.assetName.style.fontSize = 20;

            CreateAssetPathWithObjectField(windowPosition);

            m_ItemContainers.guid.container = CreateHeaderAndContentLabelContainer(windowPosition, "GUID", "", out m_ItemContainers.guid.header, out m_ItemContainers.guid.content);
            m_ItemContainers.assetSize.container = CreateHeaderAndContentLabelContainer(windowPosition, "Asset Size", "", out m_ItemContainers.assetSize.header, out m_ItemContainers.assetSize.content);
            m_ItemContainers.path.container = CreateHeaderAndContentLabelContainer(windowPosition, "Path", "", out m_ItemContainers.path.header, out m_ItemContainers.path.content);
            m_ItemContainers.editorRevision.container = CreateHeaderAndContentLabelContainer(windowPosition, "Editor Revision", "", out m_ItemContainers.editorRevision.header, out m_ItemContainers.editorRevision.content);
            m_ItemContainers.timeStamp.container = CreateHeaderAndContentLabelContainer(windowPosition, "Timestamp", "", out m_ItemContainers.timeStamp.header, out m_ItemContainers.timeStamp.content);
            m_ItemContainers.duration.container = CreateHeaderAndContentLabelContainer(windowPosition, "Duration", "", out m_ItemContainers.duration.header, out m_ItemContainers.duration.content);

            m_ItemContainers.reasonsForImport.header =
                CreateListLabel("Reason for Import", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 4, 2);

            var reasonsForImportColumns = CreateColumns(new Column("Reason", 790));


            m_ItemContainers.reasonsForImport.treeView = CreateTreeViewNested(m_ItemContainers.reasonsForImport.treeView, m_ReasonsToReimportList,
                reasonsForImportColumns, ReasonForImportSelector, null, true, true, false);

            m_ItemContainers.reasonsForImport.treeView.CanSort = false;
            m_ItemContainers.reasonsForImport.treeView.Reload();
            m_ItemContainers.reasonsForImport.treeView.searchString = m_ImportActivityState.reasonForImportSearchString;

            m_ItemContainers.reasonsForImport.content = new IMGUIContainer(m_ItemContainers.reasonsForImport.treeView.OnGUI);
            m_ItemContainers.reasonsForImport.content.RegisterCallback<MouseUpEvent>(HandleReasonsForReimportRightClick);

            m_ItemContainers.reasonsForImport.container = new VisualElement();
            m_ItemContainers.reasonsForImport.container.style.paddingBottom = 32;

            m_ItemContainers.reasonsForImport.container.Add(m_ItemContainers.reasonsForImport.header);
            m_ItemContainers.reasonsForImport.container.Add(m_ItemContainers.reasonsForImport.content);

            m_ItemContainers.dependencies = CreateListLabel("Dependencies", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 16);

            m_ProducedFilesListViewContainer = new IMGUIContainer(m_ProducedFilesListView.OnGUI);
            CalculateMaxHeightFromItemCount(m_ProducedFilesList.Count, m_ProducedFilesListViewContainer);
            m_ProducedFilesListViewContainer.RegisterCallback<MouseUpEvent>(HandleProducedArtifactsRightClick);
            m_ProducedFilesListViewContainer.style.paddingLeft = kLeftPadding;

            m_DependenciesListViewContainer = new IMGUIContainer(m_DependenciesListView.OnGUI);
            CalculateMaxHeightFromItemCount(m_DependenciesList.Count, m_DependenciesListViewContainer);
            m_DependenciesListViewContainer.style.paddingLeft = kLeftPadding;
            m_DependenciesListViewContainer.RegisterCallback<MouseUpEvent>(HandleDependenciesRightClick);

            CreateProducedFilesContainer(windowPosition);
            AddAllContainertoSelectedItemsView();
        }

        private void AddAllContainertoSelectedItemsView()
        {
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.assetName);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.assetWithObjectField.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.guid.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.assetSize.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.path.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.editorRevision.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.timeStamp.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.duration.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.reasonsForImport.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.producedArtifacts.container);

            m_ItemContainers.SelectedItemView.Add(m_ProducedFilesListViewContainer);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.dependencies);
            m_ItemContainers.SelectedItemView.Add(m_DependenciesListViewContainer);
        }

        private void CreateProducedFilesContainer(Rect windowPosition)
        {
            m_ItemContainers.producedArtifacts.container = CreateHeaderAndContentLabelContainer(windowPosition, "Produced Files/Artifacts", "", out m_ItemContainers.producedArtifacts.header, out m_ItemContainers.producedArtifacts.content);
            var headerLength = 200;
            var contentLength = 200;

            m_ItemContainers.producedArtifacts.header.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_ItemContainers.producedArtifacts.content.style.unityFontStyleAndWeight = FontStyle.Bold;

            m_ItemContainers.producedArtifacts.header.style.flexBasis = new StyleLength(headerLength);
            m_ItemContainers.producedArtifacts.content.style.flexBasis = new StyleLength(contentLength);

            m_ItemContainers.producedArtifacts.content.style.unityTextAlign = TextAnchor.MiddleRight;
            m_ItemContainers.producedArtifacts.content.style.paddingRight = kLeftPadding;

            m_ItemContainers.producedArtifacts.container.Add(m_ItemContainers.producedArtifacts.header);
            m_ItemContainers.producedArtifacts.container.Add(m_ItemContainers.producedArtifacts.content);
        }

        private static string GetOSSpecificShowIn()
        {
            if (System.Environment.OSVersion.Platform == System.PlatformID.MacOSX ||
                System.Environment.OSVersion.Platform == System.PlatformID.Unix)
                return "Reveal in Finder";
            else
                return "Show in Explorer";
        }

        private void HandleReasonsForReimportRightClick(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var menu = new GenericMenu();
            var copyItemName = "Copy";
            var selectedItems = m_ProducedFilesListView.GetSelection();
            if (selectedItems.Count() == 0)
            {
                menu.AddDisabledItem(new GUIContent(copyItemName));
            }
            else
            {
                menu.AddItem(new GUIContent(copyItemName), false, CopySelectedReasonsToClipboard);
            }

            var menuRect = new Rect(evt.mousePosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void HandleDependenciesRightClick(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var menu = new GenericMenu();
            var copyItemName = "Copy";
            var selectedItems = m_ProducedFilesListView.GetSelection();
            if (selectedItems.Count() == 0)
            {
                menu.AddDisabledItem(new GUIContent(copyItemName));
            }
            else
            {
                menu.AddItem(new GUIContent(copyItemName), false, CopySelectedDependenciesToClipboard);
            }

            var menuRect = new Rect(evt.mousePosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void HandleProducedArtifactsRightClick(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var menu = new GenericMenu();
            var copyItemName = "Copy";
            var revealInExplorerItemName = GetOSSpecificShowIn();
            var selectedItems = m_ProducedFilesListView.GetSelection();
            if (selectedItems.Count() == 0)
            {
                menu.AddDisabledItem(new GUIContent(copyItemName));
                menu.AddDisabledItem(new GUIContent(revealInExplorerItemName));
            }
            else
            {
                menu.AddItem(new GUIContent(copyItemName), false, CopySelectedProducedFilesLineToClipboard);

                //If Inline we can't reveal
                var listIndex = Math.Max(0, selectedItems[0] - 1);
                var artifactInfoTreeViewItem = m_ProducedFilesList[listIndex];

                if (artifactInfoTreeViewItem.storage == ArtifactInfoProducedFiles.kStorageInline)
                    menu.AddDisabledItem(new GUIContent(revealInExplorerItemName));
                else
                    menu.AddItem(new GUIContent(revealInExplorerItemName), false, RevealSelectedArtifactInfinder);
            }

            var menuRect = new Rect(evt.mousePosition, Vector2.zero);
            menu.DropDown(menuRect);
        }

        private void RevealSelectedArtifactInfinder()
        {
            foreach (var curDependency in m_ProducedFilesListView.GetSelection())
            {
                var index = Math.Max(0, curDependency - 1);
                var entry = m_ProducedFilesList[index];

                if (entry.storage == ArtifactInfoProducedFiles.kStorageInline)
                    continue;

                var fullPath = Path.GetFullPath(entry.libraryPath);
                EditorUtility.RevealInFinder(fullPath);
            }
        }

        private void CopySelectedDependenciesToClipboard()
        {
            StringBuilder contents = new StringBuilder();
            foreach (var curDependency in m_DependenciesListView.GetSelection())
            {
                var index = Math.Max(0, curDependency - 1);
                var entry = m_DependenciesList[index];
                contents.Append($"{entry.dependencyName} , {entry.dependency.value}");
                contents.AppendLine();
            }
            GUIUtility.systemCopyBuffer = contents.ToString();
        }

        private void CopySelectedReasonsToClipboard()
        {
            StringBuilder contents = new StringBuilder();
            var rows = m_ItemContainers.reasonsForImport.treeView.GetRows();
            foreach (var curReason in m_ItemContainers.reasonsForImport.treeView.GetSelection())
            {
                var index = Math.Max(0, curReason - 1);

                var reason = "";
                if (index > m_ReasonsToReimportList.Count)
                {
                    index -= (m_ReasonsToReimportList.Count + 2); //Clicked on a top level description
                    reason = rows[index].displayName;
                }
                else
                {
                    reason = m_ReasonsToReimportList[index].message;
                }

                contents.Append($"{reason}");
                contents.AppendLine();
            }
            GUIUtility.systemCopyBuffer = contents.ToString();
        }

        private void CopySelectedProducedFilesLineToClipboard()
        {
            StringBuilder contents = new StringBuilder();
            foreach (var curDependency in m_ProducedFilesListView.GetSelection())
            {
                var index = Math.Max(0, curDependency - 1);
                var entry = m_ProducedFilesList[index];
                contents.Append($"{entry.libraryPath} , {entry.extension} , {GetAssetFileSize(entry)}");
                contents.AppendLine();
            }
            GUIUtility.systemCopyBuffer = contents.ToString();
        }

        private void CreateAssetPathWithObjectField(Rect windowPosition)
        {
            var availableSpace = 620;
            var headerLength = availableSpace * 0.2f;
            var contentLength = availableSpace - headerLength;
            m_ItemContainers.assetWithObjectField.header = CreateListLabel("Asset", TextAnchor.MiddleLeft, 0, FontStyle.Normal);
            m_ItemContainers.assetWithObjectField.header.style.flexBasis = new StyleLength(headerLength);
            m_ItemContainers.assetWithObjectField.header.style.minHeight = 20;
            m_ItemContainers.assetWithObjectField.header.style.maxHeight = 20;

            m_ItemContainers.assetWithObjectField.content = new IMGUIContainer(OnDrawAssetPathObjectField);
            m_ItemContainers.assetWithObjectField.content.style.minWidth = 350;
            m_ItemContainers.assetWithObjectField.content.style.maxWidth = 350;
            m_ItemContainers.assetWithObjectField.content.style.maxHeight = 20;
            m_ItemContainers.assetWithObjectField.content.style.minHeight = 20;
            m_ItemContainers.assetWithObjectField.container = new VisualElement();
            m_ItemContainers.assetWithObjectField.container.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            m_ItemContainers.assetWithObjectField.container.style.minHeight = 20;
            m_ItemContainers.assetWithObjectField.container.style.maxHeight = 20;


            m_ItemContainers.assetWithObjectField.container.Add(m_ItemContainers.assetWithObjectField.header);
            m_ItemContainers.assetWithObjectField.container.Add(m_ItemContainers.assetWithObjectField.content);
        }

        public void OnDrawAssetPathObjectField()
        {
            if (m_ItemContainers.assetWithObjectField.path == null)
                return;

            if (m_ItemContainers.assetWithObjectField.loadedAsset == null)
            {
                m_ItemContainers.assetWithObjectField.loadedAsset = AssetDatabase.LoadAssetAtPath<Object>(m_ItemContainers.assetWithObjectField.path);
            }

            var rect = m_ItemContainers.assetWithObjectField.content.rect;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.ObjectField(rect, m_ItemContainers.assetWithObjectField.loadedAsset, typeof(Object), false);
            }
        }

        [Serializable]
        public class ImportActivityTreeViewState
        {
            [SerializeField] public int sortedColumnIndex;
            [SerializeField] public bool sortAscending;
            [SerializeField] public int[] visibleColumns;
            [SerializeField] public string searchString;
            [SerializeField] public int selectedItem;

            public ImportActivityTreeViewState(int columnIndex, bool ascending, int[] columns, string search, int selectedItemID)
            {
                sortedColumnIndex = columnIndex;
                sortAscending = ascending;
                visibleColumns = columns;
                searchString = search;
                selectedItem = selectedItemID;
            }

            public void StoreVisibleColumns(int[] columns)
            {
                visibleColumns = new int[columns.Length];
                for (int i = 0; i < columns.Length; ++i)
                {
                    visibleColumns[i] = columns[i];
                }
            }

            public int[] GetVisibleColumns()
            {
                var columns = new List<int>();
                for (int i = 0; i < visibleColumns.Length; ++i)
                {
                    if (visibleColumns[i] != -1)
                        columns.Add(visibleColumns[i]);
                }

                return columns.ToArray();
            }
        }

        [Serializable]
        public class ImportActivityState
        {
            [SerializeField] public int useRelativeTimeStamps;
            [SerializeField] public int showPreviousImports;
            [SerializeField] public int includePreviewImporter;
            [SerializeField] public RightContentState rightContentState;
            [SerializeField] public string toolBarSearchString;
            [SerializeField] public string selectedArtifactID;
            [SerializeField] public string reasonForImportSearchString;

            [SerializeField] public ImportActivityTreeViewState allAssetsState;
            [SerializeField] public ImportActivityTreeViewState previousRevisionsState;
            [SerializeField] public ImportActivityTreeViewState producedFilesState;
            [SerializeField] public ImportActivityTreeViewState dependenciesState;
            public ImportActivityState()
            {
                useRelativeTimeStamps = 0;
                showPreviousImports = -1;
                includePreviewImporter = -1;
                rightContentState = RightContentState.ShowOverview;
                toolBarSearchString = "";

                reasonForImportSearchString = "";

                allAssetsState = new ImportActivityTreeViewState(1, false, new int[] { 0, 1, 2, -1 }, "", 0);
                previousRevisionsState = new ImportActivityTreeViewState(0, false, new int[] { 0, 1, 2 }, "", 0);
                producedFilesState = new ImportActivityTreeViewState(0, false, new int[] { 0, 1, 2 }, "", 0);
                dependenciesState = new ImportActivityTreeViewState(0, false, new int[] { 0, 1 }, "", 0);
            }
        }

        public ImportActivityState m_ImportActivityState;

        public void OnEnable()
        {
            if (m_DesiredDimensions != default(Rect))
                InitWithDimensions(m_DesiredDimensions, m_SelectedAssetGuid);
            if (s_OpenFolderIcon == null)
                s_OpenFolderIcon = new ScalableGUIContent(null, null, EditorResources.openedFolderIconName);
            if (s_EmptyFolderIcon == null)
                s_EmptyFolderIcon = new ScalableGUIContent(null, null, EditorResources.emptyFolderIconName);

            if (m_Instance == null)
                m_Instance = this;
        }

        public void StoreTreeViewToState<T>(ImportActivityTreeViewState state, ArtifactBrowserTreeView<T> treeView)
        {
            state.sortedColumnIndex = treeView.multiColumnHeader.sortedColumnIndex;
            state.sortAscending = treeView.multiColumnHeader.sortedColumnIndex != -1 && treeView.multiColumnHeader.IsSortedAscending(state.sortedColumnIndex);
            state.selectedItem = treeView.state.lastClickedID;
            var visibleColumns = treeView.multiColumnHeader.state.visibleColumns;
            state.StoreVisibleColumns(visibleColumns);
        }

        public void OnDisable()
        {
            if (position != null && m_SplitView != null && m_SplitView.fixedPane != null)
                m_DesiredDimensions = new Rect(position.x, position.y, m_SplitView.fixedPane.rect.width * 2, position.height);

            //Saving values for Domain Reload
            //All Assets List View
            StoreTreeViewToState(m_ImportActivityState.allAssetsState, m_AllAssetsListView);

            //Previous Revisions List View
            StoreTreeViewToState(m_ImportActivityState.previousRevisionsState, m_ItemContainers.previousRevisions.treeView);

            //Produced Files list view
            StoreTreeViewToState(m_ImportActivityState.producedFilesState, m_ProducedFilesListView);

            //Dependencies List View
            StoreTreeViewToState(m_ImportActivityState.dependenciesState, m_DependenciesListView);
            m_ImportActivityState.dependenciesState.searchString = m_DependenciesListView.searchString;

            //Toolbar
            m_ImportActivityState.reasonForImportSearchString = m_ItemContainers.reasonsForImport.treeView.searchString;

            if (m_SelectedArtifactInfo != null)
                m_ImportActivityState.selectedArtifactID = m_SelectedArtifactInfo.artifactID;

            m_Instance = null;
        }

        public void RestoreListViewSelection()
        {
            m_DependenciesListView.RestoreSelection();
            m_ProducedFilesListView.RestoreSelection();
            m_Overview.mostDependencies.treeView.RestoreSelection();
            m_Overview.longestDuration.treeView.RestoreSelection();
        }

        public void FocusOnSelectedItem(string selectedObjectGuid, string selectedArtifactID = "")
        {
            if (string.IsNullOrEmpty(selectedObjectGuid))
                return;

            // Get the index for selected asset entry in the tree list view
            var selectedPath = AssetDatabase.GUIDToAssetPath(selectedObjectGuid);
            var selectedIndex = m_AllAssetsList.FindIndex(asset => asset.artifactInfo.importStats.assetPath.Equals(selectedPath, StringComparison.Ordinal));
            if (selectedIndex == -1)
            {
                Debug.LogWarning($"Asset '{selectedPath}' is selected in artifact browser, but index of the entry was not found. Please report a bug.");
                return;
            }

            m_SelectedArtifactInfo = m_AllAssetsList[selectedIndex].artifactInfo;

            if (!string.IsNullOrEmpty(selectedArtifactID))
            {
                var artifactInfos = AssetDatabase.GetArtifactInfos(new GUID(selectedObjectGuid));
                foreach (var curInfo in artifactInfos)
                {
                    if (string.CompareOrdinal(curInfo.artifactID, selectedArtifactID) == 0)
                    {
                        m_SelectedArtifactInfo = curInfo;
                        break;
                    }
                }
            }

            m_SelectedAssetGuid = selectedObjectGuid;


            UpdateViewToSelectedArtifactInfo(m_SelectedArtifactInfo);

            // Keep the same selection in the tree list view, gui list view are not 0
            m_AllAssetsListView.SetSelection(new List<int> { selectedIndex + 1 }, TreeViewSelectionOptions.RevealAndFrame);
            m_AllAssetsListView.Sort(m_AllAssetsListView.GetRows()); //Need to call sort before FrameItem, otherwise view gets out of order
            m_AllAssetsListView.FrameItem(selectedIndex + 1);
        }

        private void SetupRoot()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
        }

        private Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoTreeViewItem>, int>[] GetArtifactInfoSortCallbacks()
        {
            var callbacks = new Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoTreeViewItem>, int>[4];
            callbacks[0] = ArtifactInfoSortSelector_ShortenedName;
            callbacks[1] = ArtifactInfoSortSelector_TimeStamp;
            callbacks[2] = ArtifactInfoSortSelector_ImportDuration;
            callbacks[3] = ArtifactInfoSortSelector_ImporterName;
            return callbacks;
        }

        private Func<TreeViewItem, TreeViewItem, int, List<(string, ArtifactInfoDependency)>, int>[] GetPropertySelectorSortCallbacks()
        {
            var callbacks = new Func<TreeViewItem, TreeViewItem, int, List<(string, ArtifactInfoDependency)>, int>[2];
            callbacks[0] = PropertySortSelector_DependencyName;
            callbacks[1] = PropertySortSelector_DependencyValue;
            return callbacks;
        }

        private Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoProducedFiles>, int>[] GetProducedFilesSelectorSortCallbacks()
        {
            var callbacks = new Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoProducedFiles>, int>[3];
            callbacks[0] = ProducedFilesInfoSortSelector_LibraryPath;
            callbacks[1] = ProducedFilesInfoSortSelector_Extension;
            callbacks[2] = ProducedFilesInfoSortSelector_FileSize;
            return callbacks;
        }

        private Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoTreeViewItem>, int>[] GetPreviousRevisionSelectorSortCallbacks()
        {
            var callbacks = new Func<TreeViewItem, TreeViewItem, int, List<ArtifactInfoTreeViewItem>, int>[3];
            callbacks[0] = PreviousRevisionSortSelector_ImportedTimeStamp;
            callbacks[1] = PreviousRevisionSortSelector_ArtifactID;
            callbacks[2] = PreviousRevisionSortSelector_ImporterName;
            return callbacks;
        }

        private void CreateListViews()
        {
            var artifactColumns = CreateColumns(new Column("Asset", 200), new Column("Last Import", 120), new Column("Duration (ms)", 85), new Column("Importer", 160));
            var importStatsColumns = CreateColumns(new Column("Name", 260), new Column("Value", 330));
            var dependenciesColumn = CreateColumns(new Column("Dependency Name", 360), new Column("Dependency Value", 250));
            var producedFilesColumns = CreateColumns(new Column("File Library Path", 420), new Column("Extension", 80), new Column("Size", 80));

            var allAssetsVisibleColumns = m_ImportActivityState.allAssetsState.GetVisibleColumns();

            m_AllAssetsListView = CreateTreeView(m_AllAssetsListView, m_AllAssetsList,
                artifactColumns, ArtifactInfoSelector, GetArtifactInfoSortCallbacks(), false, false, true, allAssetsVisibleColumns);

            m_AllAssetsListView.CellGUICallback = CellGUIForAllAssets;

            var dependenciesVisibleColumns = m_ImportActivityState.dependenciesState.GetVisibleColumns();
            m_DependenciesListView = CreateTreeView(null, m_DependenciesList,
                dependenciesColumn, PropertySelector, GetPropertySelectorSortCallbacks(), true, true, true, dependenciesVisibleColumns);

            m_DependenciesListView.searchString = m_ImportActivityState.dependenciesState.searchString;
            m_DependenciesListView.CellGUICallback = CellGUIForDependencies;

            var producedFilesVisibleColumns = m_ImportActivityState.producedFilesState.GetVisibleColumns();
            m_ProducedFilesListView = CreateTreeView(m_ProducedFilesListView, m_ProducedFilesList,
                producedFilesColumns, ProducedFilesInfoSelector, GetProducedFilesSelectorSortCallbacks(), false, true, true, producedFilesVisibleColumns);

            m_ProducedFilesListView.CellGUICallback = CellGUIForProducedFiles;

            m_AllAssetsListView.Reload();
        }

        private Rect DrawIconForArtifactInfo(Rect rect, TreeViewItem item, ArtifactInfo element)
        {
            // Draw icon
            Rect iconRect = rect;
            iconRect.width = k_IconWidth;
            iconRect.x += 2;

            Texture icon = GetIconForItem(item, element);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            rect.x += k_IconWidth * 1.2f;
            rect.width -= k_IconWidth * 1.2f;
            return rect;
        }

        private void DrawRightAlignedLabel(Rect rect, string textForLabel)
        {
            rect.width -= kRightAlignPadding;
            TreeView.DefaultGUI.LabelRightAligned(rect, textForLabel, false, false);
        }

        protected Texture GetIconForItem(TreeViewItem item, ArtifactInfo artifactInfo)
        {
            if (item == null || artifactInfo == null)
                return null;

            Texture icon = item.icon;
            string selectedExtension = null;
            foreach (var cachedIcon in m_IconCache)
            {
                if (artifactInfo.assetPath.EndsWith(cachedIcon.Key, StringComparison.OrdinalIgnoreCase))
                {
                    icon = cachedIcon.Value;
                    selectedExtension = cachedIcon.Key;
                    break;
                }
            }

            if (icon == null && item.id != 0)
            {
                string path = artifactInfo.importStats.assetPath;
                icon = AssetDatabase.GetCachedIcon(path);

                if (!string.IsNullOrEmpty(selectedExtension) && m_IconCache.ContainsKey(selectedExtension))
                {
                    var value = m_IconCache[selectedExtension];
                    if (value == null)
                        m_IconCache[selectedExtension] = icon;
                }
            }

            var folderItem = item as AssetsTreeViewDataSource.FolderTreeItemBase;
            if (folderItem != null)
            {
                if (folderItem.IsEmpty)
                    icon = emptyFolderTexture;
            }

            return icon;
        }

        internal static Texture2D emptyFolderTexture
        {
            get
            {
                GUIContent folderContent = s_EmptyFolderIcon;
                return folderContent.image as Texture2D;
            }
        }

        internal static Texture2D openFolderTexture
        {
            get
            {
                GUIContent folderContent = s_OpenFolderIcon;
                return folderContent.image as Texture2D;
            }
        }

        private static IComparable ArtifactInfoSelector(ArtifactInfoTreeViewItem element, int index)
        {
            return index switch
            {
                0 => element.shortenedName,
                1 => GetTimeStamp(element.artifactInfo),
                2 => element.importDuration,
                3 => element.importerName,
                _ => "",
            };
        }

        private bool CellGUIForAllAssets(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = ArtifactInfoSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 2)
            {
                DrawRightAlignedLabel(rect, contentText);
                return true;
            }

            var content = new GUIContent(contentText);

            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;
            else if (columnIndex == 1 && elementForIcon.importStats.assetPath.StartsWith("Package", StringComparison.Ordinal))
                content.tooltip = "Note: Assets inside packages may come from project templates and will have a timestamp which is relative to when the template was created.";

            EditorGUI.LabelField(rect, content);

            return true;
        }

        private bool CellGUIForMostDependencies(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = MostDependenciesSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 1)
            {
                DrawRightAlignedLabel(rect, contentText);
                return true;
            }

            var content = new GUIContent(contentText);

            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;

            EditorGUI.LabelField(rect, content);

            return true;
        }

        private bool CellGUIForLongestDuration(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = LongestDurationSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 1)
            {
                DrawRightAlignedLabel(rect, contentText);
                return true;
            }

            var content = new GUIContent(contentText);

            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;

            EditorGUI.LabelField(rect, content);

            return true;
        }

        private bool CellGUIForDependencies(Rect rect, TreeViewItem item, int columnIndex, (string, ArtifactInfoDependency) dependencyNameToArtifactInfoDependency)
        {
            var contentText = PropertySelector(dependencyNameToArtifactInfoDependency, columnIndex).ToString();
            var content = new GUIContent(contentText);
            EditorGUI.LabelField(rect, content);
            return true;
        }

        private bool CellGUIForProducedFiles(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoProducedFiles producedFiles)
        {
            var contentText = ProducedFilesInfoSelector(producedFiles, columnIndex).ToString();
            var content = new GUIContent(contentText);
            EditorGUI.LabelField(rect, content);
            return true;
        }

        private bool CellGUIForPreviousRevisions(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem)
        {
            var contentText = PreviousRevisionSelector(artifactInfoTreeViewItem, columnIndex).ToString();
            var content = new GUIContent(contentText);

            if (artifactInfoTreeViewItem.artifactInfo.isCurrentArtifact)
                EditorGUI.LabelField(rect, content, EditorStyles.boldLabel);
            else
                EditorGUI.LabelField(rect, content);
            return true;
        }

        private void OnOverviewClicked()
        {
            UpdateSelectedView(RightContentState.ShowOverview);
        }

        private static IComparable PropertySelector((string, ArtifactInfoDependency) element, int index)
        {
            return index switch
            {
                0 => element.Item1,
                1 => element.Item2.value.ToString(),
                _ => "",
            };
        }

        private static int PropertySortSelector_DependencyName(TreeViewItem item1, TreeViewItem item2, int index, List<(string, ArtifactInfoDependency)> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.Item1, element2.Item1);
        }

        private static int PropertySortSelector_DependencyValue(TreeViewItem item1, TreeViewItem item2, int index, List<(string, ArtifactInfoDependency)> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.Item2.value.ToString(), element2.Item2.value.ToString());
        }

        private static IComparable TupleSelector((string, string) element, int index)
        {
            return index switch
            {
                0 => element.Item1,
                1 => element.Item2,
                _ => "",
            };
        }

        internal static string GetRelativeTimeStamp(DateTime importedTimeStamp)
        {
            var timeDifference = DateTime.Now - importedTimeStamp;

            if (timeDifference.Days >= 365)
            {
                var years = (int)Math.Floor(timeDifference.Days / 365.0f);
                if (years < 2)
                    return "1 year ago";
                return $"{years} years ago";
            }
            else if (timeDifference.Days > 0 && timeDifference.Days < 365)
            {
                if (timeDifference.Days < 2)
                {
                    return "1 day ago";
                }

                return $"{timeDifference.Days} days ago";
            }
            else if (timeDifference.Hours > 0 && timeDifference.Hours < 24)
            {
                if (timeDifference.Hours < 2)
                {
                    return "1 hour ago";
                }

                return $"{timeDifference.Hours} hours ago";
            }
            else if (timeDifference.Minutes > 0 && timeDifference.Minutes < 60)
            {
                if (timeDifference.Minutes < 10)
                {
                    if (timeDifference.Minutes < 5)
                    {
                        return "a few minutes ago";
                    }

                    return "5 minutes ago";
                }

                return $"{timeDifference.Minutes} minutes ago";
            }
            else if (timeDifference.Seconds < 60)
            {
                return "a few seconds ago";
            }

            return "Invalid date";
        }

        private static IComparable ReasonForImportSelector(ArtifactDifferenceReporter.ArtifactInfoDifference element, int index)
        {
            return index switch
            {
                0 => element.message,
                _ => "",
            };
        }

        private static IComparable GetTimeStamp(ArtifactInfo element)
        {
            if (m_Instance == null)
                return "";

            if (m_Instance.m_Toolbar.options.toolbar.ShowRelativeTimeStamps)
            {
                return GetRelativeTimeStamp(new DateTime(element.importStats.importedTimestamp).ToLocalTime());
            }

            var formattedTimeStamp = new DateTime(element.importStats.importedTimestamp).ToLocalTime()
                .ToString(kTimeStampFormat);

            return formattedTimeStamp;
        }

        private static IComparable PreviousRevisionSelector(ArtifactInfoTreeViewItem element, int index)
        {
            return index switch
            {
                0 => GetTimeStamp(element.artifactInfo),
                1 => element.artifactInfo.artifactID,
                2 => element.importerName,
                _ => "",
            };
        }

        private static int PreviousRevisionSortSelector_ImportedTimeStamp(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return element1.artifactInfo.importStats.importedTimestamp.CompareTo(element2.artifactInfo.importStats.importedTimestamp);
        }

        private static int PreviousRevisionSortSelector_ArtifactID(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.artifactInfo.artifactID, element2.artifactInfo.artifactID);
        }

        private static int PreviousRevisionSortSelector_ImporterName(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.importerName, element2.importerName);
        }

        private static int ArtifactInfoSortSelector_ShortenedName(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> itemList)
        {
            ArtifactInfoTreeViewItem element1 = itemList[item1.id - 1];
            ArtifactInfoTreeViewItem element2 = itemList[item2.id - 1];
            return string.CompareOrdinal(element1.shortenedName, element2.shortenedName);
        }

        private static int ArtifactInfoSortSelector_TimeStamp(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> itemList)
        {
            ArtifactInfoTreeViewItem element1 = itemList[item1.id - 1];
            ArtifactInfoTreeViewItem element2 = itemList[item2.id - 1];
            return element1.artifactInfo.timeStamp.CompareTo(element2.artifactInfo.timeStamp);
        }

        private static int ArtifactInfoSortSelector_ImportDuration(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> itemList)
        {
            ArtifactInfoTreeViewItem element1 = itemList[item1.id - 1];
            ArtifactInfoTreeViewItem element2 = itemList[item2.id - 1];
            return element1.artifactInfo.importDuration.CompareTo(element2.artifactInfo.importDuration);
        }

        private static int ArtifactInfoSortSelector_ImporterName(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> itemList)
        {
            ArtifactInfoTreeViewItem element1 = itemList[item1.id - 1];
            ArtifactInfoTreeViewItem element2 = itemList[item2.id - 1];
            return string.CompareOrdinal(element1.importerName, element2.importerName);
        }

        private static IComparable ProducedFilesInfoSelector(ArtifactInfoProducedFiles element, int index)
        {
            return index switch
            {
                0 => element.libraryPath,
                1 => element.extension,
                2 => GetAssetFileSize(element),
                _ => "",
            };
        }

        private static int ProducedFilesInfoSortSelector_LibraryPath(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoProducedFiles> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.libraryPath, element2.libraryPath);
        }

        private static int ProducedFilesInfoSortSelector_Extension(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoProducedFiles> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.extension, element2.extension);
        }

        private static int ProducedFilesInfoSortSelector_FileSize(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoProducedFiles> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return GetAssetFileSizeBytes(element1).CompareTo(GetAssetFileSizeBytes(element2));
        }

        private static IComparable MostDependenciesSelector(ArtifactInfoTreeViewItem element, int index)
        {
            return index switch
            {
                0 => element.shortenedName,
                1 => element.artifactInfo.dependencyCount,
                _ => "",
            };
        }

        private static IComparable LongestDurationSelector(ArtifactInfoTreeViewItem element, int index)
        {
            return index switch
            {
                0 => element.shortenedName,
                1 => element.importDuration,
                _ => "",
            };
        }

        private ArtifactBrowserTreeViewNested<T> CreateTreeViewNested<T>(ArtifactBrowserTreeViewNested<T> copy,
            List<T> list, MultiColumnHeaderState.Column[] columns,
            Func<T, int, IComparable> columnValueSelector, Func<TreeViewItem, TreeViewItem, int, List<T>, int>[] columnValueSortSelector,
            bool searchState = false, bool padBorders = true, bool iconShowState = true, int[] visibleColumns = null)
        {
            var headerState = new MultiColumnHeaderState(columns);

            if (visibleColumns != null)
                headerState.visibleColumns = visibleColumns;


            var header = new MultiColumnHeader(headerState);
            var state = new TreeViewState();

            var tree = new ArtifactBrowserTreeViewNested<T>(list, state, header);
            tree.SetSearch(searchState);
            tree.PadBorders = padBorders;

            tree.ColumnValueSelector = columnValueSelector;
            tree.ColumnValueSortSelector = columnValueSortSelector;


            // Copies all data from previous serialized window version.
            // This is needed to keep state after domain reload
            if (copy != null)
                tree.CopyState(copy);

            return tree;
        }

        private ArtifactBrowserTreeView<T> CreateTreeView<T>(ArtifactBrowserTreeView<T> copy,
            List<T> list, MultiColumnHeaderState.Column[] columns,
            Func<T, int, IComparable> columnValueSelector, Func<TreeViewItem, TreeViewItem, int, List<T>, int>[] columnValueSortSelector = null,
            bool searchState = false, bool padBorders = true, bool iconShowState = true, int[] visibleColumns = null)
        {
            var headerState = new MultiColumnHeaderState(columns);

            if (visibleColumns != null)
                headerState.visibleColumns = visibleColumns;


            var header = new MultiColumnHeader(headerState);
            var state = new TreeViewState();

            var tree = new ArtifactBrowserTreeView<T>(list, state, header);
            tree.SetSearch(searchState);
            tree.PadBorders = padBorders;

            tree.ColumnValueSelector = columnValueSelector;
            tree.ColumnValueSortSelector = columnValueSortSelector;

            // Copies all data from previous serialized window version.
            // This is needed to keep state after domain reload
            if (copy != null)
                tree.CopyState(copy);

            return tree;
        }

        private static MultiColumnHeaderState.Column[] CreateColumns(params Column[] columns)
        {
            var colCount = columns.Count();
            return columns.Select(col =>
            {
                return new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.TrTextContent(col.Name),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Right,
                    autoResize = false,
                    width = col.Width,
                };
            }).ToArray();
        }

        private void CreateSplitViewContainers(Rect windowPosition)
        {
            // Left/right content containers
            m_LeftContent = new VisualElement();
            m_LeftContent.style.minWidth = 100;

            m_RightContent = new VisualElement();
            m_RightContent.style.minWidth = 580;

            m_LeftContent.AddToClassList("split-container");
            m_RightContent.AddToClassList("split-container");

            // Adding a small padding to the splitter there is a
            // gap between text on the left and on the right sides
            m_LeftContent.style.paddingRight = 3;
            m_RightContent.style.paddingLeft = 3;

            // Split view
            var targetDimensions = 460;
            m_SplitView = new TwoPaneSplitView(0, targetDimensions, TwoPaneSplitViewOrientation.Horizontal);
            m_SplitView.style.backgroundColor = ListHeaderColor;
            m_SplitView.Add(m_LeftContent);
            m_SplitView.Add(m_RightContent);
            m_SplitView.CaptureMouse();
            rootVisualElement.Add(m_SplitView);
        }

        private void CreateListViewContainers(Rect windowPosition)
        {
            m_AllAssetsListViewContainer = new IMGUIContainer(m_AllAssetsListView.OnGUI);
            m_AllAssetsListViewContainer.style.paddingLeft = 12;
            m_AllAssetsListViewContainer.style.minHeight = 720 - (EditorGUI.kSingleLineHeight);
            m_AllAssetsListView.SelectionChangedCallback += AllAssetListViewSelectionCallback;

            m_LeftContent.Add(m_AllAssetsListViewContainer);
        }

        private void CalculateMaxHeightFromItemCount(int count, VisualElement element)
        {
            count = Mathf.Clamp(count + 1, 1, 10);
            var fixedSize = Math.Min(400, 28 + (count * 16));
            element.style.maxHeight = fixedSize;
            element.style.minHeight = fixedSize;
        }

        private Label CreateListLabel(string text, TextAnchor anchor = TextAnchor.MiddleCenter, int borderBottomWidth = 1, FontStyle fontStyle = FontStyle.Bold, int paddingTop = 4, int paddingBottom = 2)
        {
            var label = new Label(text);
            label.style.unityTextAlign = anchor;
            label.style.paddingTop = paddingTop;
            label.style.paddingBottom = paddingBottom;
            label.style.paddingLeft = kLeftPadding;
            label.style.backgroundColor = ListHeaderColor;
            label.style.unityFontStyleAndWeight = fontStyle;

            label.style.borderBottomColor = LabelColor;
            label.style.borderBottomWidth = borderBottomWidth;
            return label;
        }

        private void AllAssetListViewSelectionCallback(int index)
        {
            if (index > m_AllAssetsList.Count - 1 || index < 0)
                index = -1;

            var newlySelected = index != -1 ? m_AllAssetsList[index].artifactInfo : null;
            var didSelectionChange = newlySelected?.artifactID != m_SelectedArtifactInfo?.artifactID;

            if (didSelectionChange || (!didSelectionChange && m_RightContentState == RightContentState.ShowOverview))
            {
                m_SelectedArtifactInfo = newlySelected;
                m_SelectedAssetGuid = newlySelected.artifactKey.guid.ToString();
                RefreshAllListsExceptProducedArtifacts();
            }
        }

        void UpdatePreviousRevisionsForSelectedArtifact(ArtifactInfo selectedArtifactInfo)
        {
            m_PreviousRevisionsList.Clear();
            var previousVersions = GatherPreviousRevisionsForSelectedArtifact(selectedArtifactInfo);

            m_PreviousRevisionsList.AddRange(
                previousVersions.Select(
                    previousInfo => new ArtifactInfoTreeViewItem() { artifactInfo = previousInfo }));

            m_ItemContainers.previousRevisions.treeView.Reload();
            var rows = m_ItemContainers.previousRevisions.treeView.GetRows();
            m_ItemContainers.previousRevisions.treeView.Sort(rows);
        }

        void UpdateViewToSelectedArtifactInfo(ArtifactInfo selectedArtifactInfo)
        {
            var previousArtifactInfo = GetPreviouslySelectedArtifactInfo(selectedArtifactInfo);

            UpdateSelectedView(RightContentState.ShowSelectedItem);

            UpdatePreviousRevisionsForSelectedArtifact(selectedArtifactInfo);

            if (m_Toolbar.options.toolbar.ShowPreviousImports)
            {
                var index = m_PreviousRevisionsList.FindLastIndex(rev => rev.artifactInfo.artifactID == m_SelectedArtifactInfo.artifactID);

                if (index != -1)
                    m_ItemContainers.previousRevisions.treeView.SetSelection(new List<int>() { index + 1 });
            }

            UpdateItemContainers(selectedArtifactInfo, previousArtifactInfo);
            m_DependenciesList.Clear();
            m_ProducedFilesList.Clear();
            m_DependenciesList.AddRange(selectedArtifactInfo.dependencies.Select(pair => (pair.Key, pair.Value))); //TODO: Make helper functions
            m_ProducedFilesList.AddRange(selectedArtifactInfo.producedFiles);

            CalculateMaxHeightFromItemCount(m_DependenciesList.Count, m_DependenciesListViewContainer);
            CalculateMaxHeightFromItemCount(m_ProducedFilesList.Count, m_ProducedFilesListViewContainer);

            //Clear the selection, so that the next frame will load the correct object
            m_ItemContainers.assetWithObjectField.loadedAsset = null;

            UpdateReasonForReimport(selectedArtifactInfo, previousArtifactInfo);

            ReloadAndSortListViews();
        }

        private void UpdateReasonForReimport(ArtifactInfo selectedArtifactInfo, ArtifactInfo previousArtifactInfo)
        {
            m_ReasonsToReimportList.Clear();

            IEnumerable<ArtifactDifferenceReporter.ArtifactInfoDifference> differences = new List<ArtifactDifferenceReporter.ArtifactInfoDifference>();

            if (previousArtifactInfo == null)
            {
                m_ItemContainers.reasonsForImport.header.text = "Reason for Import";
                m_ReasonsToReimportList.Clear();

                var noPreviousRevisionsMessage = "No previous revisions found";
                var noPreviousRevisions = new ArtifactDifferenceReporter.ArtifactInfoDifference(noPreviousRevisionsMessage, ArtifactDifferenceReporter.DiffType.None, null, null);
                noPreviousRevisions.message = noPreviousRevisionsMessage;
                noPreviousRevisions.categoryKey = noPreviousRevisionsMessage;

                m_ReasonsToReimportList.Add(noPreviousRevisions);
            }
            else
            {
                var reporter = new ArtifactDifferenceReporter();
                var messages = reporter.GatherDifferences(previousArtifactInfo, selectedArtifactInfo);
                differences = reporter.GetAllDifferences();
                var reimportMessages = new StringBuilder();

                reimportMessages.AppendLine(messages.Count() > 1
                    ? $"Reasons for Import ({messages.Count()})"
                    : "Reason (1)");

                m_ReasonsToReimportList = differences.ToList();
            }

            m_ItemContainers.reasonsForImport.treeView.UpdateItemList(m_ReasonsToReimportList);
            m_ItemContainers.reasonsForImport.treeView.Reload();
            CalculateMaxHeightFromItemCount(m_ReasonsToReimportList.Count, m_ItemContainers.reasonsForImport.content);
        }

        private ArtifactInfo GetPreviouslySelectedArtifactInfo(ArtifactInfo selectedArtifactInfo)
        {
            var guid = selectedArtifactInfo.artifactKey.guid;
            var infos = AssetDatabase.GetArtifactInfos(guid);
            long maxTimeStamp = long.MinValue;
            ArtifactInfo previouslySelected = null;

            var selectedArtifactInfoImporterType = selectedArtifactInfo
                .artifactKey.importerType;

            foreach (var curInfo in infos)
            {
                if (curInfo.artifactID == selectedArtifactInfo.artifactID)
                    continue;

                //Make sure its the same importer
                if (curInfo.artifactKey.importerType != selectedArtifactInfoImporterType)
                    continue;

                if (curInfo.importStats.importedTimestamp > selectedArtifactInfo.importStats.importedTimestamp)
                    continue;

                if (curInfo.importStats.importedTimestamp > maxTimeStamp)
                {
                    maxTimeStamp = curInfo.importStats.importedTimestamp;
                    previouslySelected = curInfo;
                }
            }

            return previouslySelected;
        }

        private void RefreshAllListsExceptProducedArtifacts()
        {
            m_DependenciesList.Clear();
            m_ProducedFilesList.Clear();

            var isSelectedArtifactNotNull = m_SelectedArtifactInfo != null;
            if (isSelectedArtifactNotNull)
            {
                UpdateViewToSelectedArtifactInfo(m_SelectedArtifactInfo);
                return;
            }

            ReloadAndSortListViews();
        }

        private void UpdateSelectedView(RightContentState state)
        {
            if (state == RightContentState.ShowSelectedItem)
            {
                if (m_RightContent.Contains(m_Overview.SummaryView))
                {
                    m_RightContent.Remove(m_Overview.SummaryView);
                    m_Toolbar.options.toolbar.SetOverviewButtonState(true);
                }
                if (!m_RightContent.Contains(m_ItemContainers.SelectedItemRoot))
                    m_RightContent.Add(m_ItemContainers.SelectedItemRoot);

                m_RightContentState = state;
                m_ImportActivityState.rightContentState = m_RightContentState;
            }
            else if (state == RightContentState.ShowOverview)
            {
                if (m_RightContent.Contains(m_ItemContainers.SelectedItemRoot))
                    m_RightContent.Remove(m_ItemContainers.SelectedItemRoot);
                if (!m_RightContent.Contains(m_Overview.SummaryView))
                {
                    m_RightContent.Add(m_Overview.SummaryView);
                    m_Toolbar.options.toolbar.SetOverviewButtonState(false);
                }

                m_RightContentState = state;
                m_ImportActivityState.rightContentState = m_RightContentState;
            }
        }

        private void UpdateItemContainers(ArtifactInfo artifactInfo, ArtifactInfo previousArtifactInfo)
        {
            if (artifactInfo == null)
                return;

            var path = m_SelectedArtifactInfo.importStats.assetPath;
            var start = path.LastIndexOf("/") + 1;
            var assetName = path.Substring(start, path.Length - start);

            m_ItemContainers.assetName.text = assetName;
            m_ItemContainers.assetWithObjectField.path = artifactInfo.importStats.assetPath;

            m_ItemContainers.guid.content.text = artifactInfo.artifactKey.guid.ToString();
            m_ItemContainers.assetSize.content.text = GetAssetFileSize(artifactInfo.importStats.assetPath);
            m_ItemContainers.path.content.text = GetEllidedAssetName(artifactInfo.importStats.assetPath);
            m_ItemContainers.path.content.tooltip = artifactInfo.importStats.assetPath;
            m_ItemContainers.editorRevision.content.text = artifactInfo.importStats.editorRevision;

            m_ItemContainers.timeStamp.content.text =
                new DateTime(artifactInfo.importStats.importedTimestamp).ToLocalTime().ToString(kTimeStampFormat);

            var importDuration =
                Math.Max(1, m_SelectedArtifactInfo.importStats.importTimeMicroseconds / 1000).ToString("###,###,###") + " ms";

            if (previousArtifactInfo != null)
            {
                var currentMillis = Math.Floor(artifactInfo.importStats.importTimeMicroseconds / 1000.0f);
                var prevMillis = Math.Floor(previousArtifactInfo.importStats.importTimeMicroseconds / 1000.0f);
                var importDelta = ((currentMillis - prevMillis) / prevMillis) * 100;

                var importDeltaString = importDelta.ToString("###,###,###");

                if (importDelta > 0)
                    importDuration += $" (+{importDeltaString}%)";
                else if (importDelta < 0)
                    importDuration += $" ({importDeltaString}%)";
            }

            m_ItemContainers.duration.content.text = importDuration;

            m_ItemContainers.producedArtifacts.header.text = $"Produced Files/Artifacts ({artifactInfo.producedFiles.Length})";
            m_ItemContainers.producedArtifacts.content.text = GetTotalProducedArtifactsSize(artifactInfo.producedFiles);
            m_ItemContainers.dependencies.text = $"Dependencies ({artifactInfo.dependencies.Count})";
        }

        private string GetTotalProducedArtifactsSize(ArtifactInfoProducedFiles[] artifactInfoProducedFiles)
        {
            long totalSize = 0;
            foreach (var curProducedFile in artifactInfoProducedFiles)
            {
                if (curProducedFile.storage == ArtifactInfoProducedFiles.kStorageLibrary)
                {
                    var fullPath = Path.GetFullPath(curProducedFile.libraryPath);
                    var fileInfo = new FileInfo(fullPath);
                    totalSize += fileInfo.Length;
                }
                else
                {
                    totalSize += curProducedFile.inlineStorage;
                }
            }

            var totalSizeString = FormatBytes(totalSize);
            return totalSizeString;
        }

        private static long GetAssetFileSizeBytes(ArtifactInfoProducedFiles element)
        {
            if (element.storage == ArtifactInfoProducedFiles.kStorageLibrary)
            {
                GetAssetFileSize(element.libraryPath, out long sizeInBytes);
                return sizeInBytes;
            }

            var inMemorySize = element.inlineStorage;
            return inMemorySize;
        }

        private static string GetAssetFileSize(ArtifactInfoProducedFiles element)
        {
            if (element.storage == ArtifactInfoProducedFiles.kStorageLibrary)
                return GetAssetFileSize(element.libraryPath);

            var inMemorySize = element.inlineStorage;
            return FormatBytes(inMemorySize);
        }

        private static string GetAssetFileSize(string importStatsAssetPath)
        {
            return GetAssetFileSize(importStatsAssetPath, out var ignoreValue);
        }

        private static string GetAssetFileSize(string importStatsAssetPath, out long sizeInBytes)
        {
            var fullPath = Path.GetFullPath(importStatsAssetPath);
            var fileInfo = new FileInfo(fullPath);

            //Directory "size" isn't allowed
            if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                sizeInBytes = 0;
                //TODO: Handle in database files
                if (!Directory.Exists(fullPath))
                    return "";
                return "Directory";
            }

            sizeInBytes = fileInfo.Length;

            var fileSizeString = FormatBytes(fileInfo.Length);
            return fileSizeString;
        }

        internal static string FormatBytes(long byteCount)
        {
            string[] suf = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(byteCount) * num} {suf[place]}";
        }

        private static string GetEllidedAssetName(string importStatsAssetPath)
        {
            var slashCount = 0;

            for (int i = 0; i < importStatsAssetPath.Length; ++i)
            {
                if (importStatsAssetPath[i] == '/')
                    slashCount++;
            }

            if (slashCount <= 1)
                return importStatsAssetPath;

            //30 chars max, then asset name
            var path = importStatsAssetPath;
            var start = path.LastIndexOf("/", StringComparison.Ordinal);
            var assetName = path.Substring(start, path.Length - start);

            var kMaxLength = 15;
            if (importStatsAssetPath.Length < kMaxLength)
                return importStatsAssetPath;

            var firstSlash = importStatsAssetPath.IndexOf('/') + 1;

            var suffix = importStatsAssetPath.Substring(0, firstSlash);
            var finalString = $"{suffix}...{assetName}";
            return finalString;
        }

        struct ImportActivityWindowStartupDataWrapper
        {
            private ArtifactInfo[] m_AllCurrentRevisions;
            private ArtifactInfo[] m_LongestDurationAssets;
            private ArtifactInfo[] m_MostDependenciesAssets;

            public void Initialize()
            {
                m_AllCurrentRevisions = AssetDatabase.GetImportActivityWindowStartupData(ImportActivityWindowStartupData.AllCurrentRevisions);
                m_LongestDurationAssets = AssetDatabase.GetImportActivityWindowStartupData(ImportActivityWindowStartupData.LongestImportDuration);
                m_MostDependenciesAssets = AssetDatabase.GetImportActivityWindowStartupData(ImportActivityWindowStartupData.MostDependencies);
                AssetDatabase.GetImportActivityWindowStartupData(ImportActivityWindowStartupData.ClearCache);
            }

            public ArtifactInfo[] allCurrentRevisions { get { return m_AllCurrentRevisions; }}

            public ArtifactInfo[] longestDurationAssets { get { return m_LongestDurationAssets; }}

            public ArtifactInfo[] mostDependencyAssets { get { return m_MostDependenciesAssets; }}
        }

        private ImportActivityWindowStartupDataWrapper m_StartupData;

        private ArtifactInfo[] GetAllCurrentRevisions()
        {
            return m_StartupData.allCurrentRevisions;
        }

        private List<ArtifactInfo> GetAllCurrentRevisions(IEnumerable<string> allAssetPaths)
        {
            var allGUIDs = allAssetPaths.Select(AssetDatabase.GUIDFromAssetPath).ToArray();
            var currentRevisions = AssetDatabase.GetCurrentRevisions(allGUIDs);
            return currentRevisions.ToList();
        }

        private void ReloadAndSortListViews()
        {
            if (!IsInitialized)
                return;

            m_DependenciesListView.Reload();
            m_ProducedFilesListView.Reload();

            m_Overview.mostDependencies.treeView.Reload();
            m_Overview.longestDuration.treeView.Reload();

            if (m_DependenciesListView.multiColumnHeader.state.sortedColumnIndex == -1 &&
                m_ImportActivityState.dependenciesState.sortedColumnIndex != -1)
            {
                m_DependenciesListView.SetSorting(m_ImportActivityState.dependenciesState.sortedColumnIndex, m_ImportActivityState.dependenciesState.sortAscending);
                m_DependenciesListView.SetSelection(new List<int>() { m_ImportActivityState.dependenciesState.selectedItem });
            }
            else
                m_DependenciesListView.Sort(m_DependenciesListView.GetRows());

            if (m_ProducedFilesListView.multiColumnHeader.state.sortedColumnIndex == -1 &&
                m_ImportActivityState.producedFilesState.sortedColumnIndex != -1)
            {
                m_ProducedFilesListView.SetSorting(m_ImportActivityState.producedFilesState.sortedColumnIndex, m_ImportActivityState.producedFilesState.sortAscending);
                m_ProducedFilesListView.SetSelection(new List<int>() { m_ImportActivityState.producedFilesState.selectedItem });
            }
            else
                m_ProducedFilesListView.Sort(m_ProducedFilesListView.GetRows());

            m_DependenciesListView.RestoreSelection();
            m_ProducedFilesListView.RestoreSelection();
        }

        internal class ArtifactBrowserToolbar
        {
            public GUIContent ShowOverviewText = EditorGUIUtility.TrTextContent("Show Overview");
            public GUIContent Options = EditorGUIUtility.TrTextContent("Options");

            public string[] m_DropDownOptions;

            public int[] m_Selected;
            public bool[] m_Separator;

            public int counter;
            private string m_SearchString;

            public bool ShowRelativeTimeStamps => m_Selected[(int)OptionsEnum.UseRelativeTimeStamps] != -1;
            public bool ShowPreviousImports => m_Selected[(int)OptionsEnum.ShowPreviousImports] != -1;
            public bool ShowPreviewImporterRevisions => m_Selected[(int)OptionsEnum.ShowPreviewImporterRevisions] != -1;

            private Rect m_WindowPosition;
            private Action m_OnOverviewClicked;
            private Action<Rect> m_OnShowPreviousImportsToggle;
            private Action<string> m_OnSearchChanged;
            private Action m_OnShowPreviewImporterRevisionsToggle;
            private ImportActivityState m_State;

            internal enum OptionsEnum
            {
                UseRelativeTimeStamps,
                ShowPreviousImports,
                ShowPreviewImporterRevisions
            }

            public ArtifactBrowserToolbar(Rect windowPosition, Action onOverViewClicked, Action<Rect> showPreviousImportsToggle, Action<string> onSearchChanged, Action showPreviewImporterRevisionsToggle, ImportActivityState state)
            {
                m_WindowPosition = windowPosition;
                m_OnOverviewClicked = onOverViewClicked;
                m_OnShowPreviousImportsToggle = showPreviousImportsToggle;
                m_OnShowPreviewImporterRevisionsToggle = showPreviewImporterRevisionsToggle;
                m_OnSearchChanged = onSearchChanged;

                m_DropDownOptions = new[]
                {
                    "Use relative timestamps",
                    "Show previous imports",
                    "Include PreviewImporter"
                };

                m_Selected = new[]
                {
                    state.useRelativeTimeStamps,
                    state.showPreviousImports,
                    state.includePreviewImporter
                };

                m_State = state;
                m_Separator = new[] { false, false, false };
                m_ShowingOverview = state.rightContentState != RightContentState.ShowOverview;
                m_SearchString = state.toolBarSearchString;

                if (!string.IsNullOrEmpty(m_SearchString))
                    m_OnSearchChanged(m_SearchString);
            }

            private bool m_ShowingOverview = false;

            public void SetOverviewButtonState(bool state)
            {
                m_ShowingOverview = state;
            }

            public void OnGUI()
            {
                var rect = GUILayoutUtility.GetRect(0, 5000, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight);

                var overviewRect = rect;
                overviewRect.width = 120;

                var disabledScope = new EditorGUI.DisabledScope(!m_ShowingOverview);

                if (EditorGUI.Button(overviewRect, ShowOverviewText, EditorStyles.toolbarButton))
                {
                    m_OnOverviewClicked();
                }

                disabledScope.Dispose();


                var optionsRect = overviewRect;
                optionsRect.x += overviewRect.width;
                optionsRect.width = 120;

                if (EditorGUI.DropdownButton(optionsRect, Options, FocusType.Passive, EditorStyles.toolbarDropDownRight))
                {
                    GUIUtility.hotControl = 0;
                    EditorUtility.DisplayCustomMenuWithSeparators(optionsRect, m_DropDownOptions, m_Separator, m_Selected,
                        OnItemSelected, null);
                }

                optionsRect.x += 120;

                var searchFieldRect = optionsRect;
                searchFieldRect.x = rect.width - 300;
                searchFieldRect.width = 300;
                var search = EditorGUI.ToolbarSearchField(searchFieldRect, m_SearchString, false);

                if (search.GetHashCode() != m_SearchString.GetHashCode())
                {
                    m_SearchString = search;
                    m_State.toolBarSearchString = m_SearchString;
                    m_OnSearchChanged(m_SearchString);
                }
            }

            internal void OnItemSelected(object userdata, string[] options, int selected)
            {
                var isSelected = m_Selected[selected] == -1;
                m_Selected[selected] = isSelected ? selected : -1;

                if (selected == (int)OptionsEnum.ShowPreviousImports)
                {
                    m_OnShowPreviousImportsToggle(m_WindowPosition);
                    m_State.showPreviousImports = m_Selected[selected];
                }
                else if (selected == (int)OptionsEnum.UseRelativeTimeStamps)
                {
                    m_State.useRelativeTimeStamps = m_Selected[selected];
                }
                else if (selected == (int)OptionsEnum.ShowPreviewImporterRevisions)
                {
                    m_OnShowPreviewImporterRevisionsToggle();
                    m_State.includePreviewImporter = m_Selected[selected];
                }
            }
        }

        internal class ArtifactBrowserTreeViewNested<T> : ArtifactBrowserTreeView<T>
        {
            private readonly string kNoTopLevelDescription = "kNoTopLevelDescription";
            private Dictionary<string, Dictionary<ArtifactDifferenceReporter.DiffType, string>> m_KeyToTopLevelDescription;
            public ArtifactBrowserTreeViewNested(List<T> items, TreeViewState treeViewState, MultiColumnHeader multicolumnHeader) : base(items, treeViewState, multicolumnHeader)
            {
                m_KeyToTopLevelDescription = new Dictionary<string, Dictionary<ArtifactDifferenceReporter.DiffType, string>>();

                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_artifactFormatVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_allImporterVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_ImporterType, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegister_ImporterVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_NameOfAsset, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Added, "GuidOfPathLocation: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Added, "HashOfSourceAssetByGUID: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Added, "HashOfGuidsOfChildren: a dependency on the Hash of all GUIDs belonging to assets in a folder was added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Added, "HashOfContent: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_Platform, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Added, "CustomDependency: A dependency on a custom dependency was added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_PlatformGroup, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kIndeterministicImporter, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);

                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_artifactFormatVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_allImporterVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_ImporterType, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegister_ImporterVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_NameOfAsset, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Removed, "GuidOfPathLocation: a dependency on an Asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Removed, "HashOfSourceAssetByGUID: a dependency on an Asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Removed, "MetaFileHash: a dependency on a .meta file has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Removed, "HashOfContent: a dependency on an asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Removed, "HashOfGuidsOfChildren: a dependency on the Hash of all GUIDs belonging to assets in a folder was removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_Platform, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Removed, "CustomDependency: a dependency on a custom dependency was removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_PlatformGroup, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kIndeterministicImporter, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);

                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_artifactFormatVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_allImporterVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_ImporterType, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegister_ImporterVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_NameOfAsset, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Modified, "GuidOfPathLocation: an asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Modified, "HashOfSourceAssetByGUID: an asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Modified, "MetaFileHash: a .meta file that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Modified, "HashOfContent: a source asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Modified, "HashOfGuidsOfChildren: the Hash of all GUIDs belonging to assets in a folder has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_Platform, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Modified, "CustomDependency: a custom dependency was modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kImportParameter_PlatformGroup, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kIndeterministicImporter, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
            }

            private void AddTopLevelDescription(string key, ArtifactDifferenceReporter.DiffType type, string message)
            {
                m_KeyToTopLevelDescription.TryGetValue(key,
                    out var entry);
                if (entry == null)
                    entry = new Dictionary<ArtifactDifferenceReporter.DiffType, string>();
                entry.Add(type, message);
                m_KeyToTopLevelDescription[key] = entry;
            }

            private Dictionary<string, TreeViewItem> BuildBuckets(Dictionary<string, ArtifactDifferenceReporter.ArtifactInfoDifference> messagesToDifference, List<TreeViewItem> allItems)
            {
                var duplicatedMessageIndices = new List<int>();
                foreach (var curItem in m_ItemList)
                {
                    var curDifference = curItem as ArtifactDifferenceReporter.ArtifactInfoDifference;
                    if (curDifference != null)
                    {
                        if (messagesToDifference.ContainsKey(curDifference.message))
                            duplicatedMessageIndices.Add(m_ItemList.IndexOf(curItem));
                        else
                            messagesToDifference.Add(curDifference.message, curDifference);
                    }
                }

                for (int i = duplicatedMessageIndices.Count - 1; i >= 0; --i)
                    m_ItemList.RemoveAt(duplicatedMessageIndices[i]);

                var categoryKeyCounts = new Dictionary<string, int>();
                foreach (var curItem in m_ItemList)
                {
                    var item = curItem as ArtifactDifferenceReporter.ArtifactInfoDifference;
                    if (item != null)
                    {
                        categoryKeyCounts.TryGetValue(item.categoryKey, out var count);
                        count++;
                        categoryKeyCounts[item.categoryKey] = count;
                    }
                }

                var buckets = new Dictionary<string, TreeViewItem>();
                for (var i = 0; i < m_ItemList.Count; i++)
                {
                    var item = m_ItemList[i] as ArtifactDifferenceReporter.ArtifactInfoDifference;
                    var category = GetCategoryFromMessage(messagesToDifference, m_ItemList[i] as ArtifactDifferenceReporter.ArtifactInfoDifference);

                    //Make sure that there's more than 1 entry of this kind.
                    //If there's only one, we skip adding a top level description as we don't need
                    //a foldout menu for a single entry.
                    if (!string.IsNullOrEmpty(category) && !buckets.ContainsKey(category) && categoryKeyCounts[item.categoryKey] > 1)
                    {
                        //last step, make sure there's a top level description for this
                        if (string.CompareOrdinal(category, kNoTopLevelDescription) == 0)
                            continue;

                        var header = new TreeViewItem
                        { id = m_ItemList.Count + i + 1, depth = 0, displayName = category };
                        buckets.Add(category, header);
                        allItems.Add(header);
                    }
                }

                return buckets;
            }

            private string GetCategoryFromMessage(
                Dictionary<string, ArtifactDifferenceReporter.ArtifactInfoDifference> messagesToDifference, ArtifactDifferenceReporter.ArtifactInfoDifference item)
            {
                if (messagesToDifference.TryGetValue(item.message, out var curDifference))
                {
                    if (m_KeyToTopLevelDescription.TryGetValue(curDifference.categoryKey, out var diffTypeContainer))
                    {
                        if (diffTypeContainer.TryGetValue(curDifference.diffType, out var category))
                        {
                            return category;
                        }
                    }
                }

                return null;
            }

            protected override TreeViewItem BuildRoot()
            {
                m_RootItem = new TreeViewItem { id = 0, depth = -1 };
                var allItems = new List<TreeViewItem>();

                var messagesToDifference = new Dictionary<string, ArtifactDifferenceReporter.ArtifactInfoDifference>();
                var buckets = BuildBuckets(messagesToDifference, allItems);

                for (var i = 0; i < m_ItemList.Count; i++)
                {
                    var category = GetCategoryFromMessage(messagesToDifference, m_ItemList[i] as ArtifactDifferenceReporter.ArtifactInfoDifference);

                    if (!string.IsNullOrEmpty(category) && buckets.TryGetValue(category, out var header))
                    {
                        if (header != null)
                        {
                            var child = new TreeViewItem
                            {
                                id = i + 1,
                                depth = 1,
                                parent = header,
                                displayName = ColumnValueSelector(m_ItemList[i], 0).ToString()
                            };
                            header.AddChild(child);
                        }
                    }
                    else
                    {
                        var topLevelEntry = new TreeViewItem
                        {
                            id = i + 1,
                            depth = 0,
                            displayName = ColumnValueSelector(m_ItemList[i], 0).ToString()
                        };
                        allItems.Add(topLevelEntry);
                    }
                }

                SetupParentsAndChildrenFromDepths(m_RootItem, allItems);
                return m_RootItem;
            }

            protected override void CellGUI(Rect cellRect, TreeViewItem item, int columnIndex, ref RowGUIArgs args)
            {
                if (args.item.depth == 0 && args.item.hasChildren)
                {
                    cellRect.x += k_IconWidth;
                    var headerContent = new GUIContent(item.displayName);

                    EditorGUI.LabelField(cellRect, headerContent);
                    return;
                }

                if (args.item.depth > 0)
                    cellRect.x += k_IconWidth * 2;

                var element = m_ItemList[args.item.id - 1];

                CenterRectUsingSingleLineHeight(ref cellRect);

                var content = new GUIContent(ColumnValueSelector(element, columnIndex).ToString());

                EditorGUI.LabelField(cellRect, content);
            }

            protected override void OnSortingChanged(MultiColumnHeader header)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var prevSelectedItems = state.selectedIDs;

                var rows = GetRows();
                Sort(rows);

                //Restore the selection
                SetSelection(prevSelectedItems);
            }
        }

        internal class ArtifactBrowserTreeView<T> : TreeView
        {
            public Func<T, int, IComparable> ColumnValueSelector { get; set; }
            public Func<TreeViewItem, TreeViewItem, int, List<T>, int>[] ColumnValueSortSelector;
            public Func<Rect, TreeViewItem, int, T, bool> CellGUICallback { get; set; }

            public Action<int> OnDoubleClickedItem;


            public bool CanSort { get; set; }

            internal List<T> m_ItemList;

            public bool PadBorders = false;

            public event Action<int> SelectionChangedCallback;
            private bool m_SearchEnabled;
            private IList<int> m_PrevSelectedIndices = new int[0];
            private int m_SelectedItem = -1;

            protected TreeViewItem m_RootItem = null;
            public ArtifactBrowserTreeView(List<T> items, TreeViewState treeViewState, MultiColumnHeader multicolumnHeader) : base(treeViewState, multicolumnHeader)
            {
                m_ItemList = items;
                multicolumnHeader.sortingChanged += OnSortingChanged;
                showAlternatingRowBackgrounds = true;
                showBorder = true;
                CanSort = true;
            }

            public void CopyState(ArtifactBrowserTreeView<T> copy)
            {
                if (copy == null)
                {
                    Debug.LogWarning("RestoreState was called with null argument. Previous state could not be restored. Please report a bug");
                    return;
                }

                this.m_SelectedItem = copy.m_SelectedItem;
            }

            public void RestoreSelection()
            {
                if (state.selectedIDs != null && state.selectedIDs.Count > 0 && state.selectedIDs[0] > 0)
                {
                    var selectedID = state.selectedIDs[0];
                    if (m_ItemList != null && m_ItemList.Count > selectedID)
                    {
                        this.SetSelection(new List<int> { state.selectedIDs[0] });
                        FrameItem(state.selectedIDs[0]);
                    }
                }
            }

            public virtual void UpdateItemList(List<T> items)
            {
                m_ItemList = items;
                BuildRoot();
            }

            public void SetSearch(bool searchState)
            {
                m_SearchEnabled = searchState;
            }

            public void ClearPrevIndices()
            {
                m_PrevSelectedIndices = new int[0];
            }

            protected override TreeViewItem BuildRoot()
            {
                m_RootItem = new TreeViewItem { id = 0, depth = -1 };
                var allItems = new List<TreeViewItem>(m_ItemList.Count);

                for (var i = 0; i < m_ItemList.Count; i++)
                    allItems.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = ColumnValueSelector(m_ItemList[i], 0).ToString() });

                SetupParentsAndChildrenFromDepths(m_RootItem, allItems);
                return m_RootItem;
            }

            protected override void SingleClickedItem(int id)
            {
                m_PrevSelectedIndices = new int[] { id - 1 };

                // First element is counted from 1 onwards, not 0, thus subtracting 1 to have a 0 based index
                m_SelectedItem = id - 1;
                SelectionChangedCallback?.Invoke(m_SelectedItem);
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                m_PrevSelectedIndices = selectedIds;

                // First element is counted from 1 onwards, not 0, thus subtracting 1 to have a 0 based index
                m_SelectedItem = selectedIds.Count > 0 ? selectedIds.First() - 1 : -1;
                SelectionChangedCallback?.Invoke(m_SelectedItem);
            }

            protected override void DoubleClickedItem(int id)
            {
                if (OnDoubleClickedItem != null)
                    OnDoubleClickedItem(id);
            }

            public void OnGUI()
            {
                var rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);

                if (PadBorders)
                {
                    rect.x += kLeftPadding;
                    rect.width -= kLeftPadding * 2;
                }

                if (m_SearchEnabled)
                {
                    var searchFieldRect = rect;
                    searchFieldRect.y += 4;
                    searchFieldRect.height = EditorGUI.kSingleLineHeight;
                    GUILayout.BeginHorizontal();
                    var search = EditorGUI.ToolbarSearchField(searchFieldRect, searchString, false);
                    GUILayout.EndHorizontal();

                    if (searchString != search)
                    {
                        searchString = search;
                    }

                    rect.y += EditorGUI.kSingleLineHeight + 4;
                }

                base.OnGUI(rect);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                    CellGUI(args.GetCellRect(i), args.item, i, ref args);
            }

            protected virtual void CellGUI(Rect cellRect, TreeViewItem item, int columnIndex, ref RowGUIArgs args)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var element = m_ItemList[args.item.id - 1];

                CenterRectUsingSingleLineHeight(ref cellRect);

                CellGUICallback(cellRect, item, columnIndex, element);
            }

            protected virtual void OnSortingChanged(MultiColumnHeader header)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var selectedItems = state.selectedIDs.Select(id => m_ItemList[Math.Max(0, id - 1)]).ToList();

                var rows = GetRows();
                Sort(rows);

                //Restore the selection
                var prevSelectedItems = selectedItems.Select(e => m_ItemList.IndexOf(e) + 1).ToList();
                SetSelection(prevSelectedItems);
            }

            public void Sort(IList<TreeViewItem> rows)
            {
                if (!CanSort)
                    return;

                var sortedColumnIndex = multiColumnHeader.sortedColumnIndex;
                if (sortedColumnIndex == -1)
                    return;

                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var selectedMethod = ColumnValueSortSelector[sortedColumnIndex];


                Comparison<TreeViewItem> sortAscend = (TreeViewItem lhs, TreeViewItem rhs) =>
                {
                    return selectedMethod(lhs, rhs, sortedColumnIndex, m_ItemList);
                };

                Comparison<TreeViewItem> sortDescend = (TreeViewItem lhs, TreeViewItem rhs) => - sortAscend.Invoke(lhs, rhs);

                var rowList = rows as List<TreeViewItem>;
                if (rowList == null)
                    return;

                var isSortedAscending = multiColumnHeader.IsSortedAscending(multiColumnHeader.state.sortedColumnIndex);

                rowList.Sort(isSortedAscending ? sortAscend : sortDescend);
            }

            public void SetSorting(int sortedColumnIndex, bool isSortedAscending)
            {
                if (sortedColumnIndex == -1)
                    return;

                multiColumnHeader.SetSorting(sortedColumnIndex, isSortedAscending);
            }
        }

        // TODO: This might not correctly update assets if they were imported in on demand,
        // since it realies on OnPostprocess Callback. We should check it.
        public void NotifyAssetImported(string[] importedAssets, string[] assetPathsGone, string[] renamedAssets)
        {
            // Building dictionary for faster asset lookup
            var allAssetsDictionary = m_AllAssetsList
                .Select((asset, index) => (asset, index)) // enclosing asset index within a tuple
                .ToDictionary(t => t.asset.artifactInfo.importStats.assetPath, t => t); // producing dictionary: asset path -> tuple

            // Collect guids and update entries of assets which are already in the tree list view
            var guids = importedAssets
                .Where(path => allAssetsDictionary.ContainsKey(path)) // Select existing assets which were reimported
                .Select(AssetDatabase.GUIDFromAssetPath)
                .ToArray();

            var currentRevisions = AssetDatabase.GetCurrentRevisions(guids);

            foreach (var curArtifactInfo in currentRevisions)
            {
                var index = allAssetsDictionary[curArtifactInfo.importStats.assetPath].index;
                m_AllAssetsList[index] = new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = curArtifactInfo
                };
            }

            // Removing entires of assets which were deleted or renamed to different path with recent import
            var assetsToRemove = new HashSet<string>(assetPathsGone, StringComparer.Ordinal); // Hashset for faster lookup
            m_AllAssetsList.RemoveAll(asset => assetsToRemove.Contains(asset.artifactInfo.importStats.assetPath));

            var revisions = AssetDatabase.GetCurrentRevisions(
                importedAssets
                    .Where(path =>
                    !allAssetsDictionary
                        .ContainsKey(path))     // Existing assets were already updated above, thus excluding
                    .Union(renamedAssets)
                    .Select(AssetDatabase.GUIDFromAssetPath)
                    .ToArray());

            var artifactInfoTreeViewItems = new List<ArtifactInfoTreeViewItem>(revisions.Count());
            for (int i = 0; i < revisions.Count(); ++i)
            {
                artifactInfoTreeViewItems.Add(new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = revisions.ElementAt(i)
                });
            }

            // Adding entries for newly imported assets and renamed assets with recent import
            m_AllAssetsList.AddRange(artifactInfoTreeViewItems);

            // Update tree list view
            m_AllAssetsListView.UpdateItemList(m_AllAssetsList);
            m_AllAssetsListView.Reload();
            m_AllAssetsListView.Sort(m_AllAssetsListView.GetRows());
            m_AllAssetsListView.RestoreSelection();
            m_DependenciesListView.RestoreSelection();
            m_ProducedFilesListView.RestoreSelection();

            //At this point, we need to make sure the previous revisions window
            //is showing, otherwise don't update the content
            if (m_RightContentState == RightContentState.ShowOverview)
                return;

            var selectedGUID = new GUID(m_SelectedAssetGuid);
            for (int i = 0; i < guids.Length; ++i)
            {
                if (guids[i] == selectedGUID)
                {
                    UpdatePreviousRevisionsForSelectedArtifact(m_SelectedArtifactInfo);
                    break;
                }
            }
        }
    }

    internal class ArtifactBrowserPostProcessor : AssetPostprocessor
    {
        private static string[] m_ImportedAssets;
        private static string[] m_AssetPathsGone;
        private static string[] m_RenamedAssets;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            //handle imported assets
            if (ImportActivityWindow.m_Instance != null)
            {
                EditorApplication.delayCall += UpdateImportedAssetsNextTick;
                m_ImportedAssets = importedAssets;
                m_AssetPathsGone = deletedAssets.Union(movedFromAssetPaths).ToArray();
                m_RenamedAssets = movedAssets;
            }
        }

        private static void UpdateImportedAssetsNextTick()
        {
            var artifactBrowser = ImportActivityWindow.m_Instance;
            if (artifactBrowser != null && m_ImportedAssets != null)
            {
                artifactBrowser.NotifyAssetImported(m_ImportedAssets, m_AssetPathsGone, m_RenamedAssets);
                m_ImportedAssets = null;
            }
        }
    }
}
