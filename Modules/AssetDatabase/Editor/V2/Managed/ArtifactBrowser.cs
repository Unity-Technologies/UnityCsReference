// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor
{
    internal class ArtifactBrowser : EditorWindow
    {
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

        [MenuItem("Window/Internal/Artifact Browser", false, 10, true)]
        public static void ShowWindow()
        {
            // Docking new winow to some tab as of some wider window, since it feels more intuitive to start working with such layout
            // instead of tiny floating window. Also it should not be docked on top of Project Browser, since we need to select assets
            var window = GetWindow<ArtifactBrowser>(typeof(SceneView), typeof(GameView));
            window.InitWithDimensions(window.position);
            window.titleContent = new GUIContent("Artifact Browser");
            window.minSize = new Vector2(320, 320);
        }

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
        private VisualElement m_RightContent;

        private string m_SelectedAssetGuid;
        private ArtifactInfo m_SelectedArtifactInfo;

        // Data lists
        private readonly List<ArtifactInfo> m_ProducedArtifactsList = new List<ArtifactInfo>();
        private readonly List<(string, string)> m_ArtifactImportStatsList = new List<(string, string)>();
        private readonly List<(string, ArtifactInfoDependency)> m_DependenciesList = new List<(string, ArtifactInfoDependency)>();
        private readonly List<ArtifactInfoProducedFiles> m_ProducedFilesList = new List<ArtifactInfoProducedFiles>();

        // List views
        private ArtifactBrowserTreeView<ArtifactInfo> m_ProducedArtifactsListView;
        private ArtifactBrowserTreeView<(string, string)> m_ArtifactImportStatsListView;
        private ArtifactBrowserTreeView<(string, ArtifactInfoDependency)> m_DependenciesListView;
        private ArtifactBrowserTreeView<ArtifactInfoProducedFiles> m_ProducedFilesListView;
        private Label m_ProducedArtifactsLabel;

        private Font m_MonospaceFont;
        private Rect m_DesiredDimensions;
        private TwoPaneSplitView m_SplitView;

        public void InitWithDimensions(Rect windowPosition)
        {
            m_MonospaceFont = Font.CreateDynamicFontFromOSFont("Lucida Console", 13);
            SetupRoot();
            CreateListViews();

            CreateSplitViewContainers(windowPosition);
            CreateListViewContainers();

            RefreshAllLists();
            OnSelectionChanged();

            m_DesiredDimensions = new Rect(windowPosition.x, windowPosition.y, windowPosition.width, windowPosition.height);
        }

        public void OnEnable()
        {
            if (m_DesiredDimensions != default(Rect))
                InitWithDimensions(m_DesiredDimensions);
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void OnDisable()
        {
            m_DesiredDimensions = new Rect(position.x, position.y, m_SplitView.fixedPane.rect.width * 2, position.height);
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            m_SelectedAssetGuid = Selection.assetGUIDs.Length > 0 ? Selection.assetGUIDs[0] : null;

            if (!string.IsNullOrEmpty(m_SelectedAssetGuid))
            {
                //So it looks like:
                //Produced Artifacts (assetName.extension)
                var fileName = $"({Path.GetFileName(AssetDatabase.GUIDToAssetPath(m_SelectedAssetGuid))})";
                m_ProducedArtifactsLabel.text = $"Produced Artifacts {fileName}";
            }

            m_ProducedArtifactsListView.SetSelection(new[] { 1 }); // First artifact is slelected by default
            RefreshAllLists();
        }

        private void SetupRoot()
        {
            // UIElementsEditorUtility.AddDefaultEditorStyleSheets(rootVisualElement);
            rootVisualElement.style.flexDirection = FlexDirection.Row;
        }

        private void CreateListViews()
        {
            var artifactColumns = CreateColumns(new Column("Artifact ID", 260), new Column("Import Time", 160), new Column("Importer Type", 180), new Column("Is Current Artifact", 120));
            var importStatsColumns = CreateColumns(new Column("Name", 260), new Column("Value", 330));
            var dependenciesColumn = CreateColumns(new Column("Dependency Name", 330), new Column("Dependency Value", 250), new Column("Type", 120));
            var producedFilesColumns = CreateColumns(new Column("File Library Path", 420), new Column("Extension", 80));

            m_ProducedArtifactsListView = CreateTreeView(m_ProducedArtifactsList, artifactColumns, m_MonospaceFont);
            m_ProducedArtifactsListView.ColumnValueSelector = ArtifactInfoSelector;

            m_ArtifactImportStatsListView = CreateTreeView(m_ArtifactImportStatsList, importStatsColumns, m_MonospaceFont);
            m_ArtifactImportStatsListView.ColumnValueSelector = TupleSelector;

            m_DependenciesListView = CreateTreeView(m_DependenciesList, dependenciesColumn, m_MonospaceFont);
            m_DependenciesListView.ColumnValueSelector = PropertySelector;

            m_ProducedFilesListView = CreateTreeView(m_ProducedFilesList, producedFilesColumns, m_MonospaceFont);
            m_ProducedFilesListView.ColumnValueSelector = ProducedFilesInfoSelector;
        }

        private static IComparable PropertySelector((string, ArtifactInfoDependency) element, int index)
        {
            // *begin-nonstandard-formatting*
            return index switch
            {
                0 => element.Item1,
                1 => element.Item2.value.ToString(),
                2 => element.Item2.type.ToString(),
                _ => "",
            };
            // *end-nonstandard-formatting*
        }

        private static IComparable TupleSelector((string, string) element, int index)
        {
            // *begin-nonstandard-formatting*
            return index switch
            {
                0 => element.Item1,
                1 => element.Item2,
                _ => "",
            };
            // *end-nonstandard-formatting*
        }

        private static IComparable ArtifactInfoSelector(ArtifactInfo element, int index)
        {
            // *begin-nonstandard-formatting*
            return index switch
            {
                0 => element.artifactID,
                1 => new DateTime(element.importStats.importedTimestamp).ToLocalTime(),
                2 => element.artifactKey.importerType.ToString(),
                3 => element.isCurrentArtifact,
                _ => "",
            };
            // *end-nonstandard-formatting*
        }

        private static IComparable ProducedFilesInfoSelector(ArtifactInfoProducedFiles element, int index)
        {
            // *begin-nonstandard-formatting*
            return index switch
            {
                0 => element.libraryPath,
                1 => element.extension,
                _ => "",
            };
            // *end-nonstandard-formatting*
        }

        private static ArtifactBrowserTreeView<T> CreateTreeView<T>(List<T> list, MultiColumnHeaderState.Column[] columns, Font font)
        {
            var headerState = new MultiColumnHeaderState(columns);
            var header = new MultiColumnHeader(headerState);
            var state = new TreeViewState();
            return new ArtifactBrowserTreeView<T>(list, state, header) { itemFont = font };
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
            m_RightContent = new VisualElement();
            m_LeftContent.AddToClassList("split-container");
            m_RightContent.AddToClassList("split-container");

            // Adding a small padding to the splitter there is a
            // gap between text on the left and on the right sides
            m_LeftContent.style.paddingRight = 3;
            m_RightContent.style.paddingLeft = 3;

            // Split view
            m_SplitView = new TwoPaneSplitView(0, windowPosition.width / 2, TwoPaneSplitViewOrientation.Horizontal);
            m_SplitView.Add(m_LeftContent);
            m_SplitView.Add(m_RightContent);
            m_SplitView.CaptureMouse();
            rootVisualElement.Add(m_SplitView);
        }

        private void CreateListViewContainers()
        {
            var producedArtifactsListViewContainer = new IMGUIContainer(m_ProducedArtifactsListView.OnGUI);
            m_ProducedArtifactsListView.SelectionChanged += SelectionCallback;

            var artifactImportStatsListViewContainer = new IMGUIContainer(m_ArtifactImportStatsListView.OnGUI);
            var dependenciesListViewContainer = new IMGUIContainer(m_DependenciesListView.OnGUI);
            var producedFilesListViewContainer = new IMGUIContainer(m_ProducedFilesListView.OnGUI);

            m_ProducedArtifactsLabel = CreateListLabel("Produced Artifacts");

            m_LeftContent.Add(m_ProducedArtifactsLabel);
            m_LeftContent.Add(producedArtifactsListViewContainer);

            m_LeftContent.Add(CreateListLabel("Artifact Import Stats"));
            m_LeftContent.Add(artifactImportStatsListViewContainer);

            m_RightContent.Add(CreateListLabel("Dependencies"));
            m_RightContent.Add(dependenciesListViewContainer);

            m_RightContent.Add(CreateListLabel("Produced Files"));
            m_RightContent.Add(producedFilesListViewContainer);
        }

        private Label CreateListLabel(string text)
        {
            var label = new Label(text);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.paddingTop = 4;
            label.style.paddingBottom = 2;
            label.style.backgroundColor = ListHeaderColor;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            label.style.borderBottomColor = LabelColor;
            label.style.borderBottomWidth = 1;
            return label;
        }

        private void SelectionCallback(int index)
        {
            if (index > m_ProducedArtifactsList.Count - 1 || index < 0)
                index = -1;

            var newlySelected = index != -1 ? m_ProducedArtifactsList[index] : null;
            var didSelectionChange = newlySelected?.artifactID != m_SelectedArtifactInfo?.artifactID;

            if (didSelectionChange)
            {
                m_SelectedArtifactInfo = newlySelected;
                RefreshAllListsExceptProducedArtifacts();
            }
        }

        private void RefreshAllLists()
        {
            m_ProducedArtifactsList.Clear();
            if (!string.IsNullOrEmpty(m_SelectedAssetGuid))
                m_ProducedArtifactsList.AddRange(AssetDatabase.GetArtifactInfos(new GUID(m_SelectedAssetGuid)));

            RefreshAllListsExceptProducedArtifacts();
        }

        private void RefreshAllListsExceptProducedArtifacts()
        {
            m_DependenciesList.Clear();
            m_ProducedFilesList.Clear();
            m_ArtifactImportStatsList.Clear();

            var isSelectedArtifactNotNull = m_SelectedArtifactInfo != null;
            if (isSelectedArtifactNotNull)
            {
                m_DependenciesList.AddRange(m_SelectedArtifactInfo.dependencies.Select(pair => (pair.Key, pair.Value))); //TODO: Make helper functions
                m_ProducedFilesList.AddRange(m_SelectedArtifactInfo.producedFiles);
                m_ArtifactImportStatsList.AddRange(new[]
                {
                    ("Asset path", m_SelectedArtifactInfo.importStats.assetPath),
                    ("Editor revision", m_SelectedArtifactInfo.importStats.editorRevision),
                    ("Timestamp", new DateTime(m_SelectedArtifactInfo.importStats.importedTimestamp).ToLocalTime().ToString()),
                    ("Import time", (m_SelectedArtifactInfo.importStats.importTimeMicroseconds / 1000.0f).ToString("0.000") + " ms")
                });
            }

            ReloadAndSortListViews();
        }

        private void ReloadAndSortListViews()
        {
            m_ProducedArtifactsListView.Reload();
            m_ArtifactImportStatsListView.Reload();
            m_DependenciesListView.Reload();
            m_ProducedFilesListView.Reload();

            m_ProducedArtifactsListView.Sort();
            m_ArtifactImportStatsListView.Sort();
            m_DependenciesListView.Sort();
            m_ProducedFilesListView.Sort();
        }

        private class ArtifactBrowserTreeView<T> : TreeView
        {
            public Func<T, int, IComparable> ColumnValueSelector { get; set; }
            public Font itemFont { get; set; }

            private bool m_IsSortedAscending;
            private int m_SortedColumnIndex = -1;
            private readonly List<T> m_ItemList;

            public new event Action<int> SelectionChanged;

            public ArtifactBrowserTreeView(List<T> items, TreeViewState treeViewState, MultiColumnHeader multicolumnHeader) : base(treeViewState, multicolumnHeader)
            {
                m_ItemList = items;
                multicolumnHeader.sortingChanged += OnSortingChanged;
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = 0, depth = -1 };
                var allItems = new List<TreeViewItem>(m_ItemList.Count);

                for (var i = 0; i < m_ItemList.Count; i++)
                    allItems.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = ColumnValueSelector(m_ItemList[i], 0).ToString() });

                SetupParentsAndChildrenFromDepths(root, allItems);
                return root;
            }

            public void OnGUI()
            {
                var rect = GUILayoutUtility.GetRect(0, 10000, 0, 10000);
                base.OnGUI(rect);

                if (SelectionChanged != null)
                {
                    var selectedIndices = GetSelection();

                    // First element is counted from 1 onwards, not 0, thus subtracting 1 to have a 0 based index
                    var index = selectedIndices.Count > 0 ? selectedIndices.First() - 1 : -1;
                    SelectionChanged.Invoke(index);
                }
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                    CellGUI(args.GetCellRect(i), args.item, i, ref args);
            }

            private void CellGUI(Rect cellRect, TreeViewItem item, int columnIndex, ref RowGUIArgs args)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var element = m_ItemList[args.item.id - 1];

                CenterRectUsingSingleLineHeight(ref cellRect);

                var fontBefore = GUI.skin.label.font;
                EditorStyles.label.font = itemFont;
                try
                {
                    EditorGUI.LabelField(cellRect, new GUIContent(ColumnValueSelector(element, columnIndex).ToString()));
                }
                finally
                {
                    EditorStyles.label.font = fontBefore;
                }
            }

            private void OnSortingChanged(MultiColumnHeader header)
            {
                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                var selectedItems = state.selectedIDs.Select(id => m_ItemList[id - 1]).ToList();

                m_SortedColumnIndex = header.sortedColumnIndex;
                m_IsSortedAscending = header.IsSortedAscending(m_SortedColumnIndex);
                Sort();

                //Restore the selection
                var prevSelectedItems = selectedItems.Select(e => m_ItemList.IndexOf(e) + 1).ToList();
                SetSelection(prevSelectedItems);
            }

            public void Sort()
            {
                if (m_SortedColumnIndex == -1)
                    return;

                if (m_ItemList == null || m_ItemList.Count == 0)
                    return;

                Comparison<T> sortAscend = (T lhs, T rhs) =>
                {
                    return ColumnValueSelector(lhs, m_SortedColumnIndex).CompareTo(ColumnValueSelector(rhs, m_SortedColumnIndex));
                };
                Comparison<T> sortDescend = (T lhs, T rhs) =>
                {
                    return -ColumnValueSelector(lhs, m_SortedColumnIndex).CompareTo(ColumnValueSelector(rhs, m_SortedColumnIndex));
                };

                m_ItemList.Sort(m_IsSortedAscending ? sortAscend : sortDescend);
            }
        }
    }
}
