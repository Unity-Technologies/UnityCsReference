// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;

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
                window.minSize = CalculateWindowMinSize(Screen.currentResolution);
                window.InitWithDimensions(window.position, selectedObjectGuid, openViaRightClick);
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
            public bool AutoResize;

            public Column(string name, int width, bool autoResize = true)
            {
                Name = name;
                Width = width;
                AutoResize = autoResize;
            }
        }

        internal struct SelectedItemContainers
        {
            public VisualElement SelectedItemRoot;
            public TwoPaneSplitView SelectedItemSplitView;

            //Right side
            public VisualElement SelectedItemView;
            public Label assetName;
            public (VisualElement container, Label header, IMGUIContainer content, string path, Object loadedAsset) assetWithObjectField;
            public (VisualElement container, Label header, Label content) guid;
            public (VisualElement container, Label header, Label content) assetSize;
            public (VisualElement container, Label header, Label content) path;
            public (VisualElement container, Label header, Label content) editorRevision;
            public (VisualElement container, Label header, Label content) timeStamp;
            public (VisualElement container, Label header, Label content) duration;
            public (VisualElement container, Label header, Label content) importResultID;
            public (VisualElement container, Label header, Label content) dependenciesID;
            public (VisualElement container, Label header, Label content) staticDependenciesID;
            public (VisualElement container, Label header, Label content) importResultOutputID;
            public (IMGUIContainer container, ArtifactBrowserTreeViewNested<ArtifactDifferenceReporter.ArtifactInfoDifference> treeView, Label header) reasonsForImport;

            public (VisualElement container, Label header, Label content) producedArtifacts;
            public Label dependencies;

            //Left side
            public VisualElement PreviousRevisionsContainer;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView) previousRevisions;
        }

        internal struct ProjectOverviewContainer
        {
            public VisualElement SummaryView;
            public Label overView;
            public Label mostDependenciesHeader;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView) mostDependencies;

            public Label longestDurationHeader;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ArtifactInfoTreeViewItem> treeView) longestDuration;

            public Button analyzeImportProcess;
            public (IMGUIContainer container, ArtifactBrowserTreeView<ProjectAnalysisTreeViewItem> treeView) importProcessAnalysis;
        }

        internal struct ToolBarContainer
        {
            public (IMGUIContainer container, ArtifactBrowserToolbar toolbar) options;
        }

        public struct PostProcessorStaticFieldWarningMessage
        {
            public string message;
            public string additionalInfo;
            public string additionalWarning;
            public string filePath;
            public int lineNumber;
            public int columnNumber;
        }

        internal class ProjectAnalysisTreeViewItem
        {
            public (string message, string additionalInfo, string additionalWarning, string filePath, int lineNumber, int column) itemDetails;

            public string message
            {
                get
                {
                    return itemDetails.message;
                }
            }

            public string additionalInfo
            {
                get
                {
                    return itemDetails.additionalInfo;
                }
            }
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
        public const float kFirstColumnIndent = 5f;
        public const float kContentsLeftPadding = 18f;
        public const int kRightAlignPadding = 8;
        public const int kMarginBetweenSections = 16;
        private const float k_LabelColorDark = 38 / 255f;
        private const float k_LabelColorLight = 165 / 255f;
        private Color LabelColor => EditorGUIUtility.isProSkin ?
        new Color(k_LabelColorDark, k_LabelColorDark, k_LabelColorDark) :
        new Color(k_LabelColorLight, k_LabelColorLight, k_LabelColorLight);

        private const float k_ListHeaderColorDark = 62 / 255f;
        private const float k_ListHeaderColorLight = 221 / 255f;
        private Color ListHeaderColor => EditorGUIUtility.isProSkin ?
        new Color(k_ListHeaderColorDark, k_ListHeaderColorDark, k_ListHeaderColorDark, 0) :
        new Color(k_ListHeaderColorLight, k_ListHeaderColorLight, k_ListHeaderColorLight, 0);

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
        private readonly List<ProjectAnalysisTreeViewItem> m_ProjectAnalysisResults = new List<ProjectAnalysisTreeViewItem>();
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


        [field: SerializeField]
        public bool IsInitialized { get; set; }

        // internal for testing
        internal void SearchAllAssets(string search)
        {
            m_AllAssetsListView.searchString = search;
        }

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
                FocusOnSelectedItem(selectedObjectGuid, m_ImportActivityState.selectedImportResultID);

            m_DesiredDimensions = new Rect(windowPosition.x, windowPosition.y, windowPosition.width, windowPosition.height);
        }

        private void UpdateSelectedItemView(Rect windowPosition)
        {
            m_ItemContainers.SelectedItemRoot.Clear();

            if (m_Toolbar.options.toolbar.ShowPreviousImports)
            {
                m_ItemContainers.SelectedItemSplitView.Clear();
                var targetDimensions = 360;

                InstantiateSelectedItemSplitView(targetDimensions);

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
                m_ItemContainers.SelectedItemView.style.paddingLeft = kContentsLeftPadding;
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
                m_Toolbar.options.toolbar = new ArtifactBrowserToolbar(windowPosition, OnOverviewClicked, UpdateSelectedItemView, OnShowPreviewImporterRevisions, m_ImportActivityState);

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

        private void CreateOverview(Rect windowPosition)
        {
            m_Overview = new ProjectOverviewContainer();
            m_Overview.overView = CreateListLabel("Overview", TextAnchor.MiddleLeft, 0, FontStyle.Bold);
            m_Overview.overView.style.fontSize = 20;
            m_Overview.overView.style.borderBottomColor = ListHeaderColor;

            m_Overview.mostDependenciesHeader = CreateListLabel("Most Dependencies", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 16, 8);
            m_Overview.mostDependenciesHeader.style.borderBottomColor = ListHeaderColor;

            var mostDependenciesColumns = CreateColumns(new Column("Asset Path", 360), new Column("Total Dependencies", 125, false));
            var longestDurationColumns = CreateColumns(new Column("Asset Path", 360), new Column("Import Duration (ms)", 125, false));
            var projectAnalysisColumns = CreateColumns(new Column("Message", 720), new Column("Info", 1280));

            GetMostDependencyAssets();
            m_Overview.mostDependencies.treeView = CreateTreeView(m_Overview.mostDependencies.treeView, m_MostDependencyAssets, mostDependenciesColumns, MostDependenciesSelector);
            m_Overview.mostDependencies.treeView.OnDoubleClickedItem = OnMostDependenciesDoubleClickedItem;
            m_Overview.mostDependencies.treeView.CanSort = false;
            m_Overview.mostDependencies.treeView.CellGUICallback = CellGUIForMostDependencies;
            m_Overview.mostDependencies.container = m_Overview.mostDependencies.treeView.CreateAndSetupIMGUIContainer();

            GetLongestDurationAssets();
            m_Overview.longestDurationHeader = CreateListLabel("Longest Import Duration", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 16, 8);
            m_Overview.longestDurationHeader.style.borderBottomColor = ListHeaderColor;

            m_Overview.longestDuration.treeView = CreateTreeView(m_Overview.longestDuration.treeView, m_LongestDurationAssets, longestDurationColumns, LongestDurationSelector);
            m_Overview.longestDuration.treeView.OnDoubleClickedItem = OnLongestDurationDoubleClickedItem;
            m_Overview.longestDuration.treeView.CanSort = false;
            m_Overview.longestDuration.treeView.CellGUICallback = CellGUIForLongestDuration;
            m_Overview.longestDuration.container = m_Overview.longestDuration.treeView.CreateAndSetupIMGUIContainer();

            // Analyze process button
            m_Overview.analyzeImportProcess = new Button(OnAnalyseImportProcessClicked);
            m_Overview.analyzeImportProcess.text = "Analyze Import Process";
            m_Overview.analyzeImportProcess.style.alignSelf = Align.FlexStart;  // makes sure the button uses a size based on its content
            m_Overview.analyzeImportProcess.style.marginTop = 16;
            m_Overview.analyzeImportProcess.style.marginBottom = 4;
            m_Overview.analyzeImportProcess.style.marginLeft = 0;
            m_Overview.analyzeImportProcess.style.paddingLeft = 30;
            m_Overview.analyzeImportProcess.style.paddingRight = 30;

            // Project Analysis section
            var importProcessAnalysisColumn = m_ImportActivityState.projectAnalysisState.GetVisibleColumns();
            m_Overview.importProcessAnalysis.treeView = CreateTreeView<ProjectAnalysisTreeViewItem>(m_Overview.importProcessAnalysis.treeView,
                m_ProjectAnalysisResults, projectAnalysisColumns, ProjectAnalysisSelector, GetImportProcessAnalysisSortCallbacks(), visibleColumns: importProcessAnalysisColumn);
            m_Overview.importProcessAnalysis.treeView.CellGUICallback = CellGUIForProjectAnalysis;
            m_Overview.importProcessAnalysis.treeView.OnDoubleClickedItem = OnProjectAnalysisEntryDoubleClicked;
            m_Overview.importProcessAnalysis.treeView.Reload();
            m_Overview.importProcessAnalysis.container = m_Overview.importProcessAnalysis.treeView.CreateAndSetupIMGUIContainer();
            m_Overview.importProcessAnalysis.container.RegisterCallback<MouseUpEvent>(HandleProjectAnalysisRightClick);

            // Summary
            m_Overview.SummaryView = new VisualElement();
            m_Overview.SummaryView.style.paddingLeft = kContentsLeftPadding;
            m_Overview.SummaryView.Add(m_Overview.overView);
            m_Overview.SummaryView.Add(m_Overview.mostDependenciesHeader);
            m_Overview.SummaryView.Add(m_Overview.mostDependencies.container);
            m_Overview.SummaryView.Add(m_Overview.longestDurationHeader);
            m_Overview.SummaryView.Add(m_Overview.longestDuration.container);
            m_Overview.SummaryView.Add(m_Overview.analyzeImportProcess);
            m_Overview.SummaryView.Add(m_Overview.importProcessAnalysis.container);
        }

        private void OnLongestDurationDoubleClickedItem(int id)
        {
            if (id - 1 < 0)
                return;

            var selectedItem = m_LongestDurationAssets[id - 1];
            var selectedItemGUID = selectedItem.artifactInfo.artifactKey.guid;
            FocusOnSelectedItem(selectedItemGUID.ToString());
        }

        private void OnMostDependenciesDoubleClickedItem(int id)
        {
            if (id - 1 < 0)
                return;

            var selectedItem = m_MostDependencyAssets[id - 1];
            var selectedItemGUID = selectedItem.artifactInfo.artifactKey.guid;
            FocusOnSelectedItem(selectedItemGUID.ToString());
        }

        private void OnProjectAnalysisEntryDoubleClicked(int id)
        {
            if (id - 1 < 0)
                return;

            var selectedItem = m_ProjectAnalysisResults[id - 1];

            // File could not be found, so we log this
            // warning to the console on double click
            if (string.IsNullOrEmpty(selectedItem.itemDetails.filePath))
                Debug.LogWarning(selectedItem.itemDetails.additionalWarning);
            else
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal
                (
                    selectedItem.itemDetails.filePath,
                    selectedItem.itemDetails.lineNumber,
                    selectedItem.itemDetails.column
                );
            }
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
            var headerLength = availableSpace * 0.25f; // Increased from 0.2f to 0.25f for more header space
            var contentLength = availableSpace - headerLength;
            headerLabel = CreateListLabel(headerText, TextAnchor.MiddleLeft, 0, FontStyle.Normal);
            headerLabel.style.flexBasis = new StyleLength(headerLength);
            headerLabel.style.minHeight = 20;
            headerLabel.style.maxHeight = 20;

            // Create a selectable label for the content
            contentLabel = new Label(contentText);
            contentLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            contentLabel.style.paddingLeft = 8; // Added padding to shift values to the right
            contentLabel.style.paddingTop = 4;
            contentLabel.style.paddingBottom = 2;
            contentLabel.style.flexBasis = new StyleLength(contentLength);
            contentLabel.style.flexGrow = new StyleFloat(1);
            contentLabel.style.minHeight = 20;
            contentLabel.style.maxHeight = 20;
            contentLabel.style.backgroundColor = new Color(0, 0, 0, 0); // Transparent background
            contentLabel.style.unityFontStyleAndWeight = FontStyle.Normal;

            // Make the label selectable by enabling text selection
            contentLabel.selection.isSelectable = true;

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
            m_ItemContainers.SelectedItemRoot.style.flexBasis = new StyleLength(new Length(100, LengthUnit.Percent));
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

            var previousRevisionsColumns = CreateColumns(new Column("Imported", 120, false), new Column("Import Result ID", 150, false), new Column("Importer", 150));

            var previousRevisionsVisibleColumns = m_ImportActivityState.previousRevisionsState.GetVisibleColumns();
            m_ItemContainers.previousRevisions.treeView = CreateTreeView(m_ItemContainers.previousRevisions.treeView, m_PreviousRevisionsList, previousRevisionsColumns,
                PreviousRevisionSelector, GetPreviousRevisionSelectorSortCallbacks(), true, false, previousRevisionsVisibleColumns);
            m_ItemContainers.previousRevisions.treeView.searchString = m_ImportActivityState.previousRevisionsState.searchString;
            m_ItemContainers.previousRevisions.treeView.allowFullHeight = true;
            m_ItemContainers.previousRevisions.treeView.renderBorder = false;
            m_ItemContainers.previousRevisions.treeView.searchFieldHorizontalMargins = 2f;
            m_ItemContainers.previousRevisions.treeView.SelectionChangedCallback += OnSelectRevision;
            m_ItemContainers.previousRevisions.treeView.CellGUICallback = CellGUIForPreviousRevisions;
            m_ItemContainers.previousRevisions.treeView.SearchHandlerCallback = SearchHandlerForPreviousRevisions;
            m_ItemContainers.previousRevisions.treeView.SetSorting(m_ImportActivityState.previousRevisionsState.sortedColumnIndex, m_ImportActivityState.previousRevisionsState.sortAscending);
            m_ItemContainers.previousRevisions.container = m_ItemContainers.previousRevisions.treeView.CreateAndSetupIMGUIContainer();
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return revisions.Where(revision => revision.artifactKey.importerType == null || !revision.artifactKey.importerType.ToString().EndsWith("PreviewImporter", StringComparison.Ordinal));
#pragma warning restore UA2001
        }

        private void CreateSelectedItemRightSideContainers(Rect windowPosition)
        {
            m_ItemContainers.SelectedItemView = new VisualElement();
            m_ItemContainers.SelectedItemView.style.minWidth = 480;
            m_ItemContainers.SelectedItemView.style.paddingLeft = 12;
            m_ItemContainers.assetName = CreateListLabel("", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 4, 12);
            m_ItemContainers.assetName.style.fontSize = 20;

            CreateAssetPathWithObjectField(windowPosition);

            m_ItemContainers.guid.container = CreateHeaderAndContentLabelContainer(windowPosition, "GUID", "", out m_ItemContainers.guid.header, out m_ItemContainers.guid.content);
            m_ItemContainers.assetSize.container = CreateHeaderAndContentLabelContainer(windowPosition, "Asset Size", "", out m_ItemContainers.assetSize.header, out m_ItemContainers.assetSize.content);
            m_ItemContainers.path.container = CreateHeaderAndContentLabelContainer(windowPosition, "Path", "", out m_ItemContainers.path.header, out m_ItemContainers.path.content);
            m_ItemContainers.editorRevision.container = CreateHeaderAndContentLabelContainer(windowPosition, "Editor Revision", "", out m_ItemContainers.editorRevision.header, out m_ItemContainers.editorRevision.content);
            m_ItemContainers.timeStamp.container = CreateHeaderAndContentLabelContainer(windowPosition, "Timestamp", "", out m_ItemContainers.timeStamp.header, out m_ItemContainers.timeStamp.content);
            m_ItemContainers.duration.container = CreateHeaderAndContentLabelContainer(windowPosition, "Duration", "", out m_ItemContainers.duration.header, out m_ItemContainers.duration.content);
            m_ItemContainers.staticDependenciesID.container = CreateHeaderAndContentLabelContainer(windowPosition, "Static Dependencies ID", "", out m_ItemContainers.staticDependenciesID.header, out m_ItemContainers.staticDependenciesID.content);
            m_ItemContainers.dependenciesID.container = CreateHeaderAndContentLabelContainer(windowPosition, "Dependencies ID", "", out m_ItemContainers.dependenciesID.header, out m_ItemContainers.dependenciesID.content);
            m_ItemContainers.importResultOutputID.container = CreateHeaderAndContentLabelContainer(windowPosition, "Import Result Output ID", "", out m_ItemContainers.importResultOutputID.header, out m_ItemContainers.importResultOutputID.content);
            m_ItemContainers.importResultID.container = CreateHeaderAndContentLabelContainer(windowPosition, "Import Result ID", "", out m_ItemContainers.importResultID.header, out m_ItemContainers.importResultID.content);

            // Reason for import section
            var reasonsForImportColumns = CreateColumns(new Column("Reason", 790));
            m_ItemContainers.reasonsForImport.header = CreateListLabel("Reason for Import", TextAnchor.MiddleLeft, 0, FontStyle.Bold, 4, 2);
            m_ItemContainers.reasonsForImport.header.style.marginTop = kMarginBetweenSections;
            m_ItemContainers.reasonsForImport.treeView = CreateTreeViewNested(m_ItemContainers.reasonsForImport.treeView, m_ReasonsToReimportList, reasonsForImportColumns, ReasonForImportSelector, null, true, false);
            m_ItemContainers.reasonsForImport.treeView.CanSort = false;
            m_ItemContainers.reasonsForImport.treeView.Reload();
            m_ItemContainers.reasonsForImport.treeView.searchString = m_ImportActivityState.reasonForImportSearchString;
            m_ItemContainers.reasonsForImport.container = m_ItemContainers.reasonsForImport.treeView.CreateAndSetupIMGUIContainer();
            m_ItemContainers.reasonsForImport.container.RegisterCallback<MouseUpEvent>(HandleReasonsForReimportRightClick);

            // Produced files section
            CreateProducedFilesContainer(windowPosition);
            m_ProducedFilesListViewContainer = m_ProducedFilesListView.CreateAndSetupIMGUIContainer();
            m_ProducedFilesListViewContainer.RegisterCallback<MouseUpEvent>(HandleProducedArtifactsRightClick);

            // Dependencies section
            m_ItemContainers.dependencies = CreateListLabel("Dependencies", TextAnchor.MiddleLeft, 0, FontStyle.Bold);
            m_ItemContainers.dependencies.style.marginTop = kMarginBetweenSections;

            m_DependenciesListViewContainer = m_DependenciesListView.CreateAndSetupIMGUIContainer();
            m_DependenciesListViewContainer.RegisterCallback<MouseUpEvent>(HandleDependenciesRightClick);

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
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.staticDependenciesID.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.dependenciesID.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.importResultOutputID.container);
            m_ItemContainers.SelectedItemView.Add(m_ItemContainers.importResultID.container);
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
            m_ItemContainers.producedArtifacts.container.Add(m_ItemContainers.producedArtifacts.header);
            m_ItemContainers.producedArtifacts.container.Add(m_ItemContainers.producedArtifacts.content);
            m_ItemContainers.producedArtifacts.container.style.marginTop = kMarginBetweenSections;
            m_ItemContainers.producedArtifacts.container.style.marginBottom = 3;

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
            if (selectedItems.Count == 0)
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

        private void HandleProjectAnalysisRightClick(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.RightMouse)
                return;

            var menu = new GenericMenu();
            var copyItemName = "Copy";
            var selectedItems = m_Overview.importProcessAnalysis.treeView.GetSelection();
            if (selectedItems.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent(copyItemName));
            }
            else
            {
                menu.AddItem(new GUIContent(copyItemName), false, CopyProjectAnalysisEntries);
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
            if (selectedItems.Count == 0)
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
            if (selectedItems.Count == 0)
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

                if (UDS.UsingFullBackend())
                {
                    const string kUDSTempFolder = "Temp/UDS";
                    Directory.CreateDirectory(kUDSTempFolder);
                    var tempPath = Path.Combine(kUDSTempFolder, Path.GetFileName(entry.libraryPath));

                    if (!File.Exists(tempPath))
                        FileUtil.CopyFileOrDirectory(entry.libraryPath, tempPath);

                    EditorUtility.RevealInFinder(tempPath);
                }
                else
                {
                    var fullPath = Path.GetFullPath(entry.libraryPath);
                    EditorUtility.RevealInFinder(fullPath);
                }
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

        private void CopyProjectAnalysisEntries()
        {
            StringBuilder contents = new StringBuilder();
            var rows = m_Overview.importProcessAnalysis.treeView.GetRows();
            foreach (var curEntryIndex in m_Overview.importProcessAnalysis.treeView.GetSelection())
            {
                var index = Math.Max(0, curEntryIndex - 1);
                var entry = m_ProjectAnalysisResults[index];
                var extraInfo = "";

                // Copy the relevant file info, and if not available
                // copy the warning over
                if (string.IsNullOrEmpty(entry.itemDetails.filePath))
                    extraInfo = entry.itemDetails.additionalWarning;
                else
                    extraInfo =
                        $"path: {entry.itemDetails.filePath}, line: {entry.itemDetails.lineNumber}, column: {entry.itemDetails.column}";

                contents.Append($"{entry.message} , {entry.additionalInfo}, {extraInfo}");
                contents.AppendLine();
            }
            GUIUtility.systemCopyBuffer = contents.ToString();
        }

        private void CreateAssetPathWithObjectField(Rect windowPosition)
        {
            var availableSpace = 620;
            var headerLength = availableSpace * 0.25f; // Changed from 0.2f to 0.25f to match other fields
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
            m_ItemContainers.assetWithObjectField.content.style.paddingLeft = 8; // Added padding to align with other field values
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
            // Adjust rect to align with other field values by removing the extra padding that ObjectField adds
            rect.x += 8; // Move ObjectField to the right to align with other field values
            rect.width -= 8; // Compensate width for the x adjustment

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
            [SerializeField, FormerlySerializedAs("selectedArtifactID")] public string selectedImportResultID;
            [SerializeField] public string reasonForImportSearchString;

            [SerializeField] public ImportActivityTreeViewState allAssetsState;
            [SerializeField] public ImportActivityTreeViewState previousRevisionsState;
            [SerializeField] public ImportActivityTreeViewState producedFilesState;
            [SerializeField] public ImportActivityTreeViewState dependenciesState;
            [SerializeField] public ImportActivityTreeViewState projectAnalysisState;
            [SerializeField] public bool populateImportProcessAnalysis;

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
                projectAnalysisState = new ImportActivityTreeViewState(0, false, new int[] { 0, 1 }, "", 0);
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
            state.searchString = treeView.state.searchString;
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

            // Project Analysis results
            StoreTreeViewToState(m_ImportActivityState.projectAnalysisState, m_Overview.importProcessAnalysis.treeView);

            // Whether or not to repopulate the import process list after domain reload
            m_ImportActivityState.populateImportProcessAnalysis = m_ProjectAnalysisResults.Count > 0;

            if (m_SelectedArtifactInfo != null)
                m_ImportActivityState.selectedImportResultID = m_SelectedArtifactInfo.importResultID;

            m_Instance = null;
        }

        public void RestoreListViewSelection()
        {
            m_DependenciesListView.RestoreSelection();
            m_ProducedFilesListView.RestoreSelection();
            m_Overview.mostDependencies.treeView.RestoreSelection();
            m_Overview.longestDuration.treeView.RestoreSelection();
            m_Overview.importProcessAnalysis.treeView.RestoreSelection();

            if (m_ImportActivityState.populateImportProcessAnalysis)
                OnAnalyseImportProcessClicked();

            m_Overview.importProcessAnalysis.treeView.RestoreSelection();
        }

        public void FocusOnSelectedItem(string selectedObjectGuid, string selectedImportResultID = "")
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

            if (!string.IsNullOrEmpty(selectedImportResultID))
            {
                var artifactInfos = AssetDatabase.GetArtifactInfos(new GUID(selectedObjectGuid));
                foreach (var curInfo in artifactInfos)
                {
                    if (string.CompareOrdinal(curInfo.importResultID, selectedImportResultID) == 0)
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
            callbacks[1] = PreviousRevisionSortSelector_ImportResultID;
            callbacks[2] = PreviousRevisionSortSelector_ImporterName;
            return callbacks;
        }

        private Func<TreeViewItem, TreeViewItem, int, List<ProjectAnalysisTreeViewItem>, int>[] GetImportProcessAnalysisSortCallbacks()
        {
            var callbacks = new Func<TreeViewItem, TreeViewItem, int, List<ProjectAnalysisTreeViewItem>, int>[3];
            callbacks[0] = ImportProcessAnalysis_Message;
            callbacks[1] = ImportProcessAnalysis_Info;
            return callbacks;
        }

        private void CreateListViews()
        {
            var artifactColumns = CreateColumns(new Column("Asset", 200), new Column("Last Import", 120, false), new Column("Duration (ms)", 85, false), new Column("Importer", 160));
            var importStatsColumns = CreateColumns(new Column("Name", 260), new Column("Value", 330));
            var dependenciesColumn = CreateColumns(new Column("Dependency Name", 360), new Column("Dependency Value", 250));
            var producedFilesColumns = CreateColumns(new Column("File Library Path", 420), new Column("Extension", 80, false), new Column("Size", 80, false));

            var allAssetsVisibleColumns = m_ImportActivityState.allAssetsState.GetVisibleColumns();

            m_AllAssetsListView = CreateTreeView(m_AllAssetsListView, m_AllAssetsList, artifactColumns, ArtifactInfoSelector, GetArtifactInfoSortCallbacks(), true, true, allAssetsVisibleColumns);
            m_AllAssetsListView.searchString = m_ImportActivityState.allAssetsState.searchString;
            m_AllAssetsListView.allowFullHeight = true;
            m_AllAssetsListView.renderBorder = false;
            m_AllAssetsListView.renderAlternatingRowBackgrounds = true;
            m_AllAssetsListView.searchFieldHorizontalMargins = 4f;
            m_AllAssetsListView.CellGUICallback = CellGUIForAllAssets;

            var dependenciesVisibleColumns = m_ImportActivityState.dependenciesState.GetVisibleColumns();
            m_DependenciesListView = CreateTreeView(null, m_DependenciesList, dependenciesColumn, PropertySelector, GetPropertySelectorSortCallbacks(), true, true, dependenciesVisibleColumns);
            m_DependenciesListView.searchString = m_ImportActivityState.dependenciesState.searchString;
            m_DependenciesListView.CellGUICallback = CellGUIForDependencies;

            var producedFilesVisibleColumns = m_ImportActivityState.producedFilesState.GetVisibleColumns();
            m_ProducedFilesListView = CreateTreeView(m_ProducedFilesListView, m_ProducedFilesList, producedFilesColumns, ProducedFilesInfoSelector, GetProducedFilesSelectorSortCallbacks(), false, true, producedFilesVisibleColumns);
            m_ProducedFilesListView.CellGUICallback = CellGUIForProducedFiles;

            m_AllAssetsListView.Reload();
        }

        private Rect DrawIconForArtifactInfo(Rect rect, TreeViewItem item, ArtifactInfo element, float leftMargin = 5f)
        {
            // Draw icon
            Rect iconRect = rect;
            iconRect.x += leftMargin;
            iconRect.width = k_IconWidth;

            Texture icon = GetIconForItem(item, element);
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            rect.xMin = rect.xMin + k_IconWidth + leftMargin + 4f; // spacing between icon and label
            return rect;
        }

        private Rect DrawIconForProjectAnalysisResult(Rect rect, ProjectAnalysisTreeViewItem item)
        {
            // Draw icon
            Rect iconRect = rect;
            iconRect.width = k_IconWidth;
            iconRect.x += 2;

            Texture icon = EditorGUIUtility.LoadIcon("console.warnicon.sml");
            if (icon != null)
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            rect.x += k_IconWidth * 1.2f;
            rect.width -= k_IconWidth * 1.2f;
            return rect;
        }

        private void DrawRightAlignedLabel(Rect rect, string textForLabel, bool selected, bool focused)
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

        private bool CellGUIForAllAssets(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem, bool selected, bool focused)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = ArtifactInfoSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 2)
            {
                DrawRightAlignedLabel(rect, contentText, selected, focused);
                return true;
            }

            var content = GUIContent.Temp(contentText);
            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;
            else if (columnIndex == 1 && elementForIcon.importStats.assetPath.StartsWith("Package", StringComparison.Ordinal))
                content.tooltip = "Note: Assets inside packages may come from project templates and will have a timestamp which is relative to when the template was created.";

            DrawCellLabel(rect, content, selected, focused);

            return true;
        }

        void DrawCellLabel(Rect rect, GUIContent content, bool selected, bool focused)
        {
            TreeView.DefaultStyles.label.Draw(rect, content, false, false, selected, focused);
        }

        private bool CellGUIForMostDependencies(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem, bool selected, bool focused)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = MostDependenciesSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 1)
            {
                DrawRightAlignedLabel(rect, contentText, selected, focused);
                return true;
            }

            var content = GUIContent.Temp(contentText);
            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;
            DrawCellLabel(rect, content, selected, focused);

            return true;
        }

        private bool CellGUIForLongestDuration(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem, bool selected, bool focused)
        {
            ArtifactInfo elementForIcon = artifactInfoTreeViewItem.artifactInfo;
            var contentText = LongestDurationSelector(artifactInfoTreeViewItem, columnIndex).ToString();

            if (columnIndex == 0)
                rect = DrawIconForArtifactInfo(rect, item, elementForIcon);
            else if (columnIndex == 1)
            {
                DrawRightAlignedLabel(rect, contentText, selected, focused);
                return true;
            }

            var content = GUIContent.Temp(contentText);
            if (columnIndex == 0)
                content.tooltip = artifactInfoTreeViewItem.artifactInfo.importStats.assetPath;
            DrawCellLabel(rect, content, selected, focused);

            return true;
        }

        private bool CellGUIForDependencies(Rect rect, TreeViewItem item, int columnIndex, (string, ArtifactInfoDependency) dependencyNameToArtifactInfoDependency, bool selected, bool focused)
        {
            var contentText = PropertySelector(dependencyNameToArtifactInfoDependency, columnIndex).ToString();
            DrawCellLabel(rect, GUIContent.Temp(contentText), selected, focused);
            return true;
        }

        private bool CellGUIForProjectAnalysis(Rect rect, TreeViewItem treeViewItem, int columnIndex, ProjectAnalysisTreeViewItem item, bool selected, bool focused)
        {
            var contentText = columnIndex == 0 ? item.message : item.additionalInfo;

            // Add the icon
            if (columnIndex == 0)
                rect = DrawIconForProjectAnalysisResult(rect, item);

            DrawCellLabel(rect, GUIContent.Temp(contentText), selected, focused);
            return true;
        }

        private bool CellGUIForProducedFiles(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoProducedFiles producedFiles, bool selected, bool focused)
        {
            var contentText = ProducedFilesInfoSelector(producedFiles, columnIndex).ToString();
            DrawCellLabel(rect, GUIContent.Temp(contentText), selected, focused);
            return true;
        }

        private bool CellGUIForPreviousRevisions(Rect rect, TreeViewItem item, int columnIndex, ArtifactInfoTreeViewItem artifactInfoTreeViewItem, bool selected, bool focused)
        {
            var contentText = PreviousRevisionSelector(artifactInfoTreeViewItem, columnIndex).ToString();
            var content = GUIContent.Temp(contentText);

            if (artifactInfoTreeViewItem.artifactInfo.isCurrentArtifact)
                TreeView.DefaultStyles.boldLabel.Draw(rect, content, isHover: false, isActive: false, on: true, hasKeyboardFocus: true);
            else
                DrawCellLabel(rect, content, selected, focused);
            return true;
        }

        private bool SearchHandlerForPreviousRevisions(TreeViewItem item, ArtifactInfoTreeViewItem info, string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
                return false;
            if (info.importerName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (info.artifactInfo.importResultID.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private void OnOverviewClicked()
        {
            UpdateSelectedView(RightContentState.ShowOverview);
        }

        private void OnAnalyseImportProcessClicked()
        {
            if (m_ProjectAnalysisResults != null)
                m_ProjectAnalysisResults.Clear();

            //TODO: Update the code on the editor
            //UnityEditor.AssetImportWorkerPostProcessorHelper.StaticVariableDetector.FindStaticVariablesInPostProcessorsAndScriptedImporters();
            var warnings = StaticFieldCollector.FindStaticVariablesInPostProcessorsAndScriptedImporters();
            for (int i = 0; i < warnings.Length; ++i)
            {
                m_ProjectAnalysisResults.Add(new ProjectAnalysisTreeViewItem()
                {
                    itemDetails = (warnings[i].message, warnings[i].additionalInfo, warnings[i].additionalWarning, warnings[i].filePath, warnings[i].lineNumber, warnings[i].columnNumber)
                });
            }
            m_Overview.importProcessAnalysis.treeView.Reload();
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
                1 => element.artifactInfo.importResultID,
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

        private static int PreviousRevisionSortSelector_ImportResultID(TreeViewItem item1, TreeViewItem item2, int index, List<ArtifactInfoTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.artifactInfo.importResultID, element2.artifactInfo.importResultID);
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

        private static int ImportProcessAnalysis_Message(TreeViewItem item1, TreeViewItem item2, int index, List<ProjectAnalysisTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.message , element2.message);
        }

        private static int ImportProcessAnalysis_Info(TreeViewItem item1, TreeViewItem item2, int index, List<ProjectAnalysisTreeViewItem> items)
        {
            var element1 = items[item1.id - 1];
            var element2 = items[item2.id - 1];
            return string.CompareOrdinal(element1.additionalInfo, element2.additionalInfo);
        }

        private static IComparable ProducedFilesInfoSelector(ArtifactInfoProducedFiles element, int index)
        {
            return index switch
            {
                0 => Path.GetFullPath(element.libraryPath),
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

        private static IComparable ProjectAnalysisSelector(ProjectAnalysisTreeViewItem element, int index)
        {
            return index switch
            {
                0 => element.message,
                _ => "",
            };
        }

        private ArtifactBrowserTreeViewNested<T> CreateTreeViewNested<T>(ArtifactBrowserTreeViewNested<T> copy,
            List<T> list, MultiColumnHeaderState.Column[] columns,
            Func<T, int, IComparable> columnValueSelector, Func<TreeViewItem, TreeViewItem, int, List<T>, int>[] columnValueSortSelector,
            bool searchState = false, bool iconShowState = true, int[] visibleColumns = null)
        {
            var headerState = new MultiColumnHeaderState(columns);

            if (visibleColumns != null)
                headerState.visibleColumns = visibleColumns;

            var header = new MultiColumnHeader(headerState);
            var state = new TreeViewState();

            var tree = new ArtifactBrowserTreeViewNested<T>(list, state, header);
            tree.SetSearch(searchState);
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
            bool searchState = false, bool iconShowState = true, int[] visibleColumns = null)
        {
            var headerState = new MultiColumnHeaderState(columns);

            if (visibleColumns != null)
                headerState.visibleColumns = visibleColumns;

            var header = new MultiColumnHeader(headerState);
            var state = new TreeViewState();

            var tree = new ArtifactBrowserTreeView<T>(list, state, header);
            tree.sortByNameAfterSearch = false; // we handle sorting ourselves
            tree.SetSearch(searchState);

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
            var colCount = columns.Length;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return columns.Select(col =>
#pragma warning restore UA2001
            {
                return new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.TrTextContent(col.Name),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Center,
                    autoResize = col.AutoResize,
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

            // Split view
            var targetDimensions = 460;
            m_SplitView = new TwoPaneSplitView(0, targetDimensions, TwoPaneSplitViewOrientation.Horizontal);
            m_SplitView.style.backgroundColor = ListHeaderColor;
            m_SplitView.style.marginLeft = 0;
            m_SplitView.style.marginRight = 12;
            m_SplitView.style.marginBottom = 0;
            m_SplitView.style.overflow = Overflow.Visible;
            m_SplitView.Add(m_LeftContent);
            m_SplitView.Add(m_RightContent);
            m_SplitView.CaptureMouse();

            rootVisualElement.Add(m_SplitView);
        }

        private void CreateListViewContainers(Rect windowPosition)
        {
            m_AllAssetsListViewContainer = new IMGUIContainer(m_AllAssetsListView.OnGUI);
            m_AllAssetsListViewContainer.style.minHeight = m_AllAssetsListView.minimumHeight;
            m_AllAssetsListView.SelectionChangedCallback += AllAssetListViewSelectionCallback;

            m_LeftContent.Add(m_AllAssetsListViewContainer);
        }

        private Label CreateListLabel(string text, TextAnchor anchor = TextAnchor.MiddleCenter, int borderBottomWidth = 1, FontStyle fontStyle = FontStyle.Bold, int paddingTop = 4, int paddingBottom = 2)
        {
            var label = new Label(text);
            label.style.unityTextAlign = anchor;
            label.style.paddingTop = paddingTop;
            label.style.paddingBottom = paddingBottom;
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
            var didSelectionChange = newlySelected?.importResultID != m_SelectedArtifactInfo?.importResultID;

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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                previousVersions.Select(
#pragma warning restore UA2001
                    previousInfo => new ArtifactInfoTreeViewItem() { artifactInfo = previousInfo }));

            m_ItemContainers.previousRevisions.treeView.Reload();
        }

        void UpdateViewToSelectedArtifactInfo(ArtifactInfo selectedArtifactInfo)
        {
            var previousArtifactInfo = GetPreviouslySelectedArtifactInfo(selectedArtifactInfo);

            UpdateSelectedView(RightContentState.ShowSelectedItem);

            UpdatePreviousRevisionsForSelectedArtifact(selectedArtifactInfo);

            if (m_Toolbar.options.toolbar.ShowPreviousImports)
            {
                var index = m_PreviousRevisionsList.FindLastIndex(rev => rev.artifactInfo.importResultID == m_SelectedArtifactInfo.importResultID);

                if (index != -1)
                    m_ItemContainers.previousRevisions.treeView.SetSelection(new List<int>() { index + 1 });
            }

            UpdateItemContainers(selectedArtifactInfo, previousArtifactInfo);
            m_DependenciesList.Clear();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_DependenciesList.AddRange(selectedArtifactInfo.dependencies.Select(pair => (pair.Key, pair.Value))); //TODO: Make helper functions
#pragma warning restore UA2001

            m_ProducedFilesList.Clear();
            var producedFiles = selectedArtifactInfo.producedFiles;
            if (producedFiles != null)
                m_ProducedFilesList.AddRange(producedFiles);

            //Clear the selection, so that the next frame will load the correct object
            m_ItemContainers.assetWithObjectField.loadedAsset = null;

            UpdateReasonForReimport(selectedArtifactInfo, previousArtifactInfo);

            ReloadAndSortListViews();
        }

        private void UpdateReasonForReimport(ArtifactInfo selectedArtifactInfo, ArtifactInfo previousArtifactInfo)
        {
            m_ReasonsToReimportList.Clear();

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
                var differences = reporter.GetAllDifferences();
                var reimportMessages = new StringBuilder();

                reimportMessages.AppendLine(messages.Count > 1
                    ? $"Reasons for Import ({messages.Count})"
                    : "Reason (1)");

                m_ReasonsToReimportList = new List<ArtifactDifferenceReporter.ArtifactInfoDifference>(differences);
            }

            m_ItemContainers.reasonsForImport.treeView.UpdateItemList(m_ReasonsToReimportList);
            m_ItemContainers.reasonsForImport.treeView.Reload();
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
                if (curInfo.importResultID == selectedArtifactInfo.importResultID)
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

            // Populate the new ID fields with custom tooltips
            m_ItemContainers.importResultID.content.text = artifactInfo.importResultID;
            m_ItemContainers.importResultID.content.tooltip = "A unique identifier for an import result. This ID is generated as a hash of both the import's dependencies (inputs) and its resulting output. Unity's asset database uses it internally to track a specific import result.";

            m_ItemContainers.dependenciesID.content.text = artifactInfo.dependenciesID.ToString();
            m_ItemContainers.dependenciesID.content.tooltip = "ID is a hash of all dependencies that affect this asset's import (input). This includes both static and dynamic dependencies.";

            m_ItemContainers.staticDependenciesID.content.text = artifactInfo.staticDependenciesID.ToString();
            m_ItemContainers.staticDependenciesID.content.tooltip = "ID is a hash of static dependencies only. Static dependencies include importer settings, importer version, and other factors that are determined before import time. Dynamic dependencies are discovered during the import.";

            m_ItemContainers.importResultOutputID.content.text = artifactInfo.importResultOutputID.ToString();
            m_ItemContainers.importResultOutputID.content.tooltip = "ID is a hash of the output of an import. This includes both meta data of the import result and the actual import artifacts generated during the import.";

            var producedFiles = artifactInfo.producedFiles;
            string headerText;
            string contentText;
            if (producedFiles != null)
            {
                headerText = $"Produced Files/Artifacts ({producedFiles.Length})";
                contentText = GetTotalProducedArtifactsSize(producedFiles);
            }
            else
            {
                headerText = $"Produced Files/Artifacts (n/a)";
                contentText = "<n/a>";
            }
            m_ItemContainers.producedArtifacts.header.text = headerText;
            m_ItemContainers.producedArtifacts.content.text = contentText;
            m_ItemContainers.dependencies.text = $"Dependencies ({artifactInfo.dependencies.Count})";
        }

        private string GetTotalProducedArtifactsSize(ArtifactInfoProducedFiles[] artifactInfoProducedFiles)
        {
            long totalSize = 0;
            foreach (var curProducedFile in artifactInfoProducedFiles)
            {
                if (curProducedFile.storage == ArtifactInfoProducedFiles.kStorageLibrary)
                {
                    if (UDS.UsingFullBackend())
                    {
                        var tempPath = FileUtil.GetUniqueTempPathInProject();
                        FileUtil.CopyFileOrDirectory(curProducedFile.libraryPath, tempPath);
                        var fileInfo = new FileInfo(tempPath);
                        totalSize += fileInfo.Length;
                        File.Delete(tempPath);
                    }
                    else
                    {
                        var fullPath = Path.GetFullPath(curProducedFile.libraryPath);
                        var fileInfo = new FileInfo(fullPath);
                        totalSize += fileInfo.Length;
                    }
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
            if (FileUtil.IsDir(importStatsAssetPath))
            {
                sizeInBytes = 0;
                return "Directory";
            }

            sizeInBytes = (long)FileUtil.GetSize(importStatsAssetPath);

            var fileSizeString = FormatBytes(sizeInBytes);
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

            public ArtifactInfo[] allCurrentRevisions { get { return m_AllCurrentRevisions; } }

            public ArtifactInfo[] longestDurationAssets { get { return m_LongestDurationAssets; } }

            public ArtifactInfo[] mostDependencyAssets { get { return m_MostDependenciesAssets; } }
        }

        private ImportActivityWindowStartupDataWrapper m_StartupData;

        private ArtifactInfo[] GetAllCurrentRevisions()
        {
            return m_StartupData.allCurrentRevisions;
        }

        private List<ArtifactInfo> GetAllCurrentRevisions(IEnumerable<string> allAssetPaths)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var allGUIDs = allAssetPaths.Select(AssetDatabase.GUIDFromAssetPath).ToArray();
#pragma warning restore UA2001
            var currentRevisions = AssetDatabase.GetCurrentRevisions(allGUIDs);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return currentRevisions.ToList();
#pragma warning restore UA2001
        }

        private void ReloadAndSortListViews()
        {
            if (!IsInitialized)
                return;

            m_DependenciesListView.Reload();
            m_ProducedFilesListView.Reload();

            m_Overview.mostDependencies.treeView.Reload();
            m_Overview.longestDuration.treeView.Reload();
            m_Overview.importProcessAnalysis.treeView.Reload();

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

            if (m_ImportActivityState.populateImportProcessAnalysis)
                m_Overview.importProcessAnalysis.treeView.SetSelection(new List<int>() { m_ImportActivityState.projectAnalysisState.selectedItem });

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

            public bool ShowRelativeTimeStamps => m_Selected[(int)OptionsEnum.UseRelativeTimeStamps] != -1;
            public bool ShowPreviousImports => m_Selected[(int)OptionsEnum.ShowPreviousImports] != -1;
            public bool ShowPreviewImporterRevisions => m_Selected[(int)OptionsEnum.ShowPreviewImporterRevisions] != -1;

            private Rect m_WindowPosition;
            private Action m_OnOverviewClicked;
            private Action<Rect> m_OnShowPreviousImportsToggle;
            private Action m_OnShowPreviewImporterRevisionsToggle;
            private ImportActivityState m_State;

            internal enum OptionsEnum
            {
                UseRelativeTimeStamps,
                ShowPreviousImports,
                ShowPreviewImporterRevisions
            }

            public ArtifactBrowserToolbar(Rect windowPosition, Action onOverViewClicked, Action<Rect> showPreviousImportsToggle, Action showPreviewImporterRevisionsToggle, ImportActivityState state)
            {
                m_WindowPosition = windowPosition;
                m_OnOverviewClicked = onOverViewClicked;
                m_OnShowPreviousImportsToggle = showPreviousImportsToggle;
                m_OnShowPreviewImporterRevisionsToggle = showPreviewImporterRevisionsToggle;

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
            }

            private bool m_ShowingOverview = false;

            public void SetOverviewButtonState(bool state)
            {
                m_ShowingOverview = state;
            }

            public void OnGUI()
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                {
                    var disabledScope = new EditorGUI.DisabledScope(!m_ShowingOverview);
                    if (GUILayout.Button(ShowOverviewText, EditorStyles.toolbarButton))
                    {
                        m_OnOverviewClicked();
                    }
                    disabledScope.Dispose();

                    if (EditorGUILayout.DropdownButton(Options, FocusType.Passive, EditorStyles.toolbarDropDownRight))
                    {
                        GUIUtility.hotControl = 0;
                        EditorUtility.DisplayCustomMenuWithSeparators(GUILayoutUtility.topLevel.GetLast(), m_DropDownOptions, m_Separator, m_Selected, OnItemSelected, null);
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
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
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterType, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_NameOfAsset, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Added, "GuidOfPathLocation: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Added, "HashOfSourceAssetByGUID: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Added, "HashOfGuidsOfChildren: a dependency on the Hash of all GUIDs belonging to assets in a folder was added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Added, "HashOfContent: a dependency on an asset has been added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_Platform, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Added, "CustomDependency: A dependency on a custom dependency was added");
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_PlatformGroup, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kIndeterministicImporter, ArtifactDifferenceReporter.DiffType.Added, kNoTopLevelDescription);

                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_artifactFormatVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_allImporterVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterType, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_NameOfAsset, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Removed, "GuidOfPathLocation: a dependency on an Asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Removed, "HashOfSourceAssetByGUID: a dependency on an Asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Removed, "MetaFileHash: a dependency on a .meta file has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Removed, "HashOfContent: a dependency on an asset has been removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Removed, "HashOfGuidsOfChildren: a dependency on the Hash of all GUIDs belonging to assets in a folder was removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_Platform, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Removed, "CustomDependency: a dependency on a custom dependency was removed");
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_PlatformGroup, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kIndeterministicImporter, ArtifactDifferenceReporter.DiffType.Removed, kNoTopLevelDescription);

                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_artifactFormatVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kGlobal_allImporterVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterType, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_ImporterVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kImporterRegistry_PostProcessorVersionHash, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_NameOfAsset, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_GuidOfPathLocation, ArtifactDifferenceReporter.DiffType.Modified, "GuidOfPathLocation: an asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_HashOfSourceAssetByGUID, ArtifactDifferenceReporter.DiffType.Modified, "HashOfSourceAssetByGUID: an asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kSourceAsset_MetaFileHash, ArtifactDifferenceReporter.DiffType.Modified, "MetaFileHash: a .meta file that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfContent, ArtifactDifferenceReporter.DiffType.Modified, "HashOfContent: a source asset that is depended on has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_HashOfGuidsOfChildren, ArtifactDifferenceReporter.DiffType.Modified, "HashOfGuidsOfChildren: the Hash of all GUIDs belonging to assets in a folder has been modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kArtifact_FileIdOfMainObject, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_Platform, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_TextureImportCompression, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ColorSpace, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_GraphicsAPIMask, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_ScriptingRuntimeVersion, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_CustomDependency, ArtifactDifferenceReporter.DiffType.Modified, "CustomDependency: a custom dependency was modified");
                AddTopLevelDescription(ArtifactDifferenceReporter.kEnvironment_PlatformGroup, ArtifactDifferenceReporter.DiffType.Modified, kNoTopLevelDescription);
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
                cellRect.xMin += kFirstColumnIndent;
                if (args.item.depth == 0 && args.item.hasChildren)
                {
                    cellRect.x += k_IconWidth;
                    var headerContent = new GUIContent(item.displayName);

                    DefaultGUI.Label(cellRect, item.displayName, args.selected, args.focused);
                    return;
                }

                if (args.item.depth > 0)
                    cellRect.x += k_IconWidth * 2;

                var element = m_ItemList[args.item.id - 1];
                CenterRectUsingSingleLineHeight(ref cellRect);
                var content = ColumnValueSelector(element, columnIndex).ToString();
                DefaultGUI.Label(cellRect, content, args.selected, args.focused);
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
            public Func<Rect, TreeViewItem, int, T, bool, bool, bool> CellGUICallback { get; set; }
            public Func<TreeViewItem, T, string, bool> SearchHandlerCallback { get; set; }
            public Action<int> OnDoubleClickedItem { get; set; }
            public bool CanSort { get; set; }
            internal List<T> m_ItemList;
            public event Action<int> SelectionChangedCallback;
            private bool m_SearchEnabled;
            private IList<int> m_PrevSelectedIndices = Array.Empty<int>();
            private int m_SelectedItem = -1;
            internal float minimumHeight => (m_SearchEnabled ? k_SearchFieldTotalHeight : 0) + 77f; // 80 is for the treeview with header and vertical scroll bar that can show both arrows and a dragger
            internal bool allowFullHeight { get; set; } = false;
            internal bool renderBorder { get => showBorder; set => showBorder = value; }
            internal bool renderAlternatingRowBackgrounds { get => showAlternatingRowBackgrounds; set => showAlternatingRowBackgrounds = value; }
            internal float searchFieldHorizontalMargins { get; set; } = 0;

            const float kSearchFieldHeight = EditorGUI.kSingleLineHeight;
            const float kSearchFieldVerticalMargins = 4;

            float k_SearchFieldTotalHeight => kSearchFieldHeight + kSearchFieldVerticalMargins;

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

            public IMGUIContainer CreateAndSetupIMGUIContainer()
            {
                var imguiContainer = new IMGUIContainer(this.OnGUI);
                imguiContainer.style.minHeight = minimumHeight;
                imguiContainer.style.flexShrink = 1;
                imguiContainer.style.flexGrow = 0;
                return imguiContainer;
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
            }

            public void SetSearch(bool searchState)
            {
                m_SearchEnabled = searchState;
            }

            public void ClearPrevIndices()
            {
                m_PrevSelectedIndices = Array.Empty<int>();
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

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                var rows = base.BuildRows(root);
                Sort(rows);
                return rows;
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
                m_SelectedItem = selectedIds.Count > 0 ? selectedIds[0] - 1 : -1;
                SelectionChangedCallback?.Invoke(m_SelectedItem);
            }

            protected override void DoubleClickedItem(int id)
            {
                if (OnDoubleClickedItem != null)
                    OnDoubleClickedItem(id);
            }

            public void OnGUI()
            {
                // Allocate rect in layout system
                const float kScrollbarHeight = 14;
                float heightNeeded = totalHeight + kScrollbarHeight + (m_SearchEnabled ? k_SearchFieldTotalHeight : 0);
                if (heightNeeded < minimumHeight)
                    heightNeeded = minimumHeight;
                var availableRect = GUILayoutUtility.GetRect(0, 10000, 0, allowFullHeight ? 10000 : heightNeeded);

                // Search field
                if (m_SearchEnabled)
                {
                    var searchFieldRect = availableRect;
                    searchFieldRect.x += searchFieldHorizontalMargins * 2;
                    searchFieldRect.width -= searchFieldHorizontalMargins * 3;
                    searchFieldRect.y += kSearchFieldVerticalMargins;
                    searchFieldRect.height = kSearchFieldHeight;
                    var search = EditorGUI.ToolbarSearchField(searchFieldRect, searchString, false);
                    if (searchString != search)
                        searchString = search;
                }

                // Tree view
                Rect treeViewRect = availableRect;
                if (m_SearchEnabled)
                {
                    treeViewRect.y += k_SearchFieldTotalHeight;
                    treeViewRect.height -= k_SearchFieldTotalHeight;
                }

                base.OnGUI(treeViewRect);
            }

            protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
            {
                if (SearchHandlerCallback != null)
                {
                    var element = m_ItemList[item.id - 1];
                    return SearchHandlerCallback(item, element, search);
                }
                else
                    return base.DoesItemMatchSearch(item, search);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                    CellGUI(args.GetCellRect(i), args.item, args.GetColumn(i), ref args);
            }

            protected virtual void CellGUI(Rect cellRect, TreeViewItem item, int columnIndex, ref RowGUIArgs args)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var element = m_ItemList[args.item.id - 1];

                CenterRectUsingSingleLineHeight(ref cellRect);

                if (columnIndex == 0)
                    cellRect.xMin += kFirstColumnIndent;

                CellGUICallback(cellRect, item, columnIndex, element, args.selected, args.focused);
            }

            protected virtual void OnSortingChanged(MultiColumnHeader header)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var selectedItems = state.selectedIDs.ConvertAll(id => m_ItemList[Math.Max(0, id - 1)]);

                var rows = GetRows();
                Sort(rows);

                //Restore the selection
                var prevSelectedItems = selectedItems.ConvertAll(e => m_ItemList.IndexOf(e) + 1);
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

                Comparison<TreeViewItem> sortDescend = (TreeViewItem lhs, TreeViewItem rhs) => -sortAscend.Invoke(lhs, rhs);

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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var allAssetsDictionary = m_AllAssetsList
#pragma warning restore UA2001
                .Select((asset, index) => (asset, index)) // enclosing asset index within a tuple
                .ToDictionary(t => t.asset.artifactInfo.importStats.assetPath, t => t); // producing dictionary: asset path -> tuple

            // Collect guids and update entries of assets which are already in the tree list view
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var guids = importedAssets
#pragma warning restore UA2001
                .Where(allAssetsDictionary.ContainsKey) // Select existing assets which were reimported
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
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                importedAssets
#pragma warning restore UA2001
                    .Where(path =>
                    !allAssetsDictionary
                        .ContainsKey(path))     // Existing assets were already updated above, thus excluding
                    .Union(renamedAssets)
                    .Select(AssetDatabase.GUIDFromAssetPath)
                    .ToArray());

            var artifactInfoTreeViewItems = new List<ArtifactInfoTreeViewItem>(revisions.Length);
            for (int i = 0; i < revisions.Length; ++i)
            {
                artifactInfoTreeViewItems.Add(new ArtifactInfoTreeViewItem()
                {
                    artifactInfo = revisions[i]
                });
            }

            // Adding entries for newly imported assets and renamed assets with recent import
            m_AllAssetsList.AddRange(artifactInfoTreeViewItems);

            // Update tree list view
            m_AllAssetsListView.UpdateItemList(m_AllAssetsList);
            m_AllAssetsListView.Reload();
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
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // Handle imported assets
            if (ImportActivityWindow.m_Instance != null)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                string[] assetPathsGone = deletedAssets.Union(movedFromAssetPaths).ToArray();
#pragma warning restore UA2001

                // Create a lambda expression that captures the desired variables and calls the method with parameters
                EditorApplication.delayCall += () => UpdateImportedAssetsNextTick(importedAssets, assetPathsGone, movedAssets);
            }
        }

        private static void UpdateImportedAssetsNextTick(string[] importedAssets, string[] assetPathsGone, string[] renamedAssets)
        {
            var artifactBrowser = ImportActivityWindow.m_Instance;
            if (artifactBrowser != null)
            {
                artifactBrowser.NotifyAssetImported(importedAssets, assetPathsGone, renamedAssets);
            }
        }
    }
}
