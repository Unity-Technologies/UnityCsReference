// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Build.Analysis
{
    [EditorWindowTitle(title = "Build Analysis", icon = "UnityEditor.ProfilerWindow")]
    internal class BuildAnalysisWindow : EditorWindow
    {
        private const string k_OpenWindowCommand = "ContentBuild/OpenBuildAnalysisWindow";

        private const string k_WindowTitle = "Build Analysis";
        private const string k_UxmlPath = "BuildAnalysis/UXML/BuildAnalysisWindow.uxml";
        private const string k_UssPath = "BuildAnalysis/StyleSheets/BuildAnalysisWindow.uss";
        private const string k_UssClassDark = "build-analysis-window--dark";
        private const string k_UssClassLight = "build-analysis-window--light";

        // State persistence keys
        private const string k_KeyPrefix = "BuildAnalysisWindow.";
        private const string k_SplitterKey = k_KeyPrefix + "SplitterPosition";
        private const string k_SelectedBuildSessionKey = k_KeyPrefix + "SelectedBuildSessionGUID";
        private const string k_InspectorOpenKey = k_KeyPrefix + "InspectorPanelOpen";

        private TwoPaneSplitView m_SplitView;
        private ListView m_BuildListView;
        private ToolbarSearchField m_SearchField;
        private ToolbarMenu m_SettingsMenu;
        private ToolbarToggle m_InspectorToggle;
        private TabView m_TabView;
        private Tab m_OverviewTab;
        private Tab m_AssetsTab;
        private VisualElement m_EmptyState;
        private Label m_EmptyStateTitle;
        private Label m_EmptyStateDescription;

        private BuildAnalysisService m_Service;
        private BuildAnalysisTabHost m_TabHost;
        private BuildHistoryWatcher m_Watcher;

        private BuildEntry[] m_AllBuilds = Array.Empty<BuildEntry>();
        private BuildEntry[] m_FilteredBuilds = Array.Empty<BuildEntry>();
        private string m_CurrentSearchText = string.Empty;
        private BuildEntry m_SelectedBuild;
        private BuildAnalysis m_SelectedBuildAnalysis;

        [MenuItem("Window/Analysis/Build Analysis")]
        internal static void ShowWindow()
        {
            if (CommandService.Exists(k_OpenWindowCommand))
            {
                CommandService.Execute(k_OpenWindowCommand, CommandHint.Menu);
                return;
            }

            var window = GetWindow<BuildAnalysisWindow>(false);
            window.titleContent = new GUIContent(k_WindowTitle);
            window.minSize = new Vector2(750, 400);
        }

        private void OnEnable()
        {
            var buildHistory = new BuildHistoryProvider();
            var fileSystem = new BuildAnalysisFileSystem();

            var enumerator = new BuildEnumerator(buildHistory);
            var analyzer = new BuildAnalyzer(new BuildReportConverter(), fileSystem, buildHistory);
            m_Service = new BuildAnalysisService(enumerator, analyzer, fileSystem, new BuildAnalysisProgressReporter(), buildHistory);

            m_Watcher = new BuildHistoryWatcher(buildHistory);
            m_Watcher.BuildHistoryChanged += RefreshBuildList;
            m_Watcher.Enable();
        }

        private void OnDisable()
        {
            m_Watcher.Disable();
            m_Watcher.BuildHistoryChanged -= RefreshBuildList;
            SavePersistedState();
        }

        public void CreateGUI()
        {
            var visualTree = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            if (visualTree == null)
            {
                throw new InvalidOperationException($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Cannot load {k_UxmlPath}.");
            }

            visualTree.CloneTree(rootVisualElement);

            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            rootVisualElement.styleSheets.Add(styleSheet);
            ApplyThemeClass(rootVisualElement);

            m_SplitView = rootVisualElement.Q<TwoPaneSplitView>("build-analysis-split");
            m_BuildListView = rootVisualElement.Q<ListView>("build-list");
            m_SearchField = rootVisualElement.Q<ToolbarSearchField>("build-search");
            m_SettingsMenu = rootVisualElement.Q<ToolbarMenu>("build-settings-menu");
            m_TabView = rootVisualElement.Q<TabView>("main-tabs");
            m_OverviewTab = rootVisualElement.Q<Tab>("overview-tab");
            m_AssetsTab = rootVisualElement.Q<Tab>("assets-tab");
            m_EmptyState = rootVisualElement.Q<VisualElement>("empty-state");
            m_EmptyStateTitle = rootVisualElement.Q<Label>("empty-state-title");
            m_EmptyStateDescription = rootVisualElement.Q<Label>("empty-state-description");

            SetupBuildListView();
            SetupSettingsMenu();
            SetupInspectorToggle();
            SetupTabs();

            m_SearchField.RegisterValueChangedCallback(evt => FilterBuilds(evt.newValue));

            var splitterPos = EditorPrefs.GetFloat(k_SplitterKey, 100);
            m_SplitView.fixedPaneInitialDimension = splitterPos;

            RefreshBuildList();
        }

        private void SetupInspectorToggle()
        {
            var tabViewport = m_TabView.Q<VisualElement>(className: "unity-tab-view__content-viewport");
            if (tabViewport == null)
                throw new InvalidOperationException($"{BuildAnalysisConstants.k_ConsoleLogPrefix} TabView content viewport  .unity-tab-view__content-viewport not found.");

            m_InspectorToggle = new ToolbarToggle
            {
                name = "inspector-toggle",
                tooltip = "Toggle Inspector",
            };
            m_InspectorToggle.AddToClassList("inspector-toggle");
            tabViewport.Add(m_InspectorToggle);

            m_InspectorToggle.SetValueWithoutNotify(EditorPrefs.GetBool(k_InspectorOpenKey, false));
            m_InspectorToggle.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(k_InspectorOpenKey, evt.newValue);
                m_TabHost.SetInspectorOpen(evt.newValue);
            });
        }

        private void SetupTabs()
        {
            m_TabHost = new BuildAnalysisTabHost(m_TabView);
            m_TabHost.Register(m_OverviewTab, new OverviewTabView());
            m_TabHost.Register(m_AssetsTab, new AssetsTabView());
            m_TabHost.NotifyCurrentTabVisibility();
            m_TabHost.SetInspectorOpen(m_InspectorToggle.value);
            m_TabHost.SetSelection(null, null);
        }

        private void SetupBuildListView()
        {
            m_BuildListView.makeItem = MakeListItem;
            m_BuildListView.bindItem = BindListItem;
            m_BuildListView.selectionType = SelectionType.Single;
            m_BuildListView.fixedItemHeight = 45;
            m_BuildListView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            m_BuildListView.selectionChanged += OnBuildSelectionChanged;

            // Context menu
            m_BuildListView.AddManipulator(new ContextualMenuManipulator(PopulateBuildContextMenu));
        }

        private void SetupSettingsMenu()
        {
            m_SettingsMenu.menu.AppendAction("Delete All Builds...", _ => OnDeleteAllBuilds());
        }

        private VisualElement MakeListItem()
        {
            var itemContainer = new VisualElement();
            itemContainer.AddToClassList("build-list-item-container");

            var item = new VisualElement();
            item.AddToClassList("build-list-item");

            // Row 1: Platform icon + BuildName + Status
            var row1 = new VisualElement();
            row1.AddToClassList("build-list-item__row1");

            var platformIcon = new Image { name = "platform-icon" };
            platformIcon.AddToClassList("platform-icon");

            row1.Add(platformIcon);

            var buildName = new Label { name = "build-name" };
            buildName.AddToClassList("build-name");
            row1.Add(buildName);

            var statusIcon = new VisualElement { name = "status-icon" };
            statusIcon.AddToClassList("status-icon");
            row1.Add(statusIcon);

            // Secondary metadata row: left (size/time), right (date)
            var metaRow = new VisualElement();
            metaRow.AddToClassList("build-list-item__meta-row");

            var row2 = new Label { name = "size-duration" };
            row2.AddToClassList("build-list-item__row2");

            var row3 = new Label { name = "timestamp" };
            row3.AddToClassList("build-list-item__row3");

            item.Add(row1);
            metaRow.Add(row2);
            metaRow.Add(row3);
            item.Add(metaRow);

            itemContainer.Add(item);
            return itemContainer;
        }

        private void BindListItem(VisualElement element, int index)
        {
            if (index < 0 || index >= m_FilteredBuilds.Length)
                return;

            var build = m_FilteredBuilds[index];

            element.Q<Label>("build-name").text = build.BuildName;
            element.Q<Label>("size-duration").text = $"{FormatUtility.FormatSize(build.TotalSizeBytes)} • {FormatUtility.FormatDuration(build.TotalTimeMs)}";
            element.Q<Label>("timestamp").text = FormatUtility.FormatBuildDate(build.BuildStartedAt);

            var statusIcon = element.Q<VisualElement>("status-icon");
            if (build.BuildResult == BuildResult.Succeeded)
            {
                statusIcon.AddToClassList("status-icon--success");
                statusIcon.RemoveFromClassList("status-icon--failed");
            }
            else
            {
                statusIcon.AddToClassList("status-icon--failed");
                statusIcon.RemoveFromClassList("status-icon--success");
            }

            var platformIcon = element.Q<Image>("platform-icon");
            platformIcon.image = IconUtility.GetPlatformIcon(build.Platform);
        }

        private void OnBuildSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item == null)
                    continue;

                if (item is BuildEntry selectedBuild)
                {
                    SelectBuild(selectedBuild);
                    return;
                }

                throw new InvalidOperationException($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Build list selection must be {nameof(BuildEntry)} but got {item.GetType().FullName}.");
            }
        }

        private static void ApplyThemeClass(VisualElement view)
        {
            view.RemoveFromClassList(k_UssClassDark);
            view.RemoveFromClassList(k_UssClassLight);
            view.AddToClassList(EditorGUIUtility.isProSkin ? k_UssClassDark : k_UssClassLight);
        }

        private void SelectBuild(BuildEntry build)
        {
            m_SelectedBuild = build;
            m_SelectedBuildAnalysis = null;

            // Save selection
            if (build != null)
            {
                EditorPrefs.SetString(k_SelectedBuildSessionKey, build.BuildSessionGUID.ToString());
            }

            UpdateContentArea();
        }

        private void UpdateContentArea()
        {
            if (m_SelectedBuild != null)
                m_SelectedBuildAnalysis = m_Service.GetBuildAnalysis(m_SelectedBuild.BuildSessionGUID);

            m_TabHost.SetSelection(m_SelectedBuild, m_SelectedBuildAnalysis);
        }

        private void RefreshBuildList()
        {
            m_AllBuilds = m_Service.GetBuilds();
            FilterBuilds(m_CurrentSearchText);

            UpdateEmptyState();

            // Restore selection if possible
            var storedGuidString = EditorPrefs.GetString(k_SelectedBuildSessionKey);
            if (!string.IsNullOrEmpty(storedGuidString) && GUID.TryParse(storedGuidString, out var storedGuid))
            {
                BuildEntry selectedBuild = null;
                for (var i = 0; i < m_FilteredBuilds.Length; i++)
                {
                    if (m_FilteredBuilds[i].BuildSessionGUID == storedGuid)
                    {
                        selectedBuild = m_FilteredBuilds[i];
                        break;
                    }
                }

                if (selectedBuild != null)
                {
                    var index = Array.IndexOf(m_FilteredBuilds, selectedBuild);
                    if (index >= 0)
                    {
                        m_BuildListView.SetSelection(index);
                    }
                }
            }
        }

        private void FilterBuilds(string searchText)
        {
            m_CurrentSearchText = searchText;

            if (string.IsNullOrEmpty(searchText))
            {
                m_FilteredBuilds = m_AllBuilds;
            }
            else
            {
                // Case-insensitive search on BuildName and Platform
                var filteredBuilds = new List<BuildEntry>(m_AllBuilds.Length);
                foreach (var build in m_AllBuilds)
                {
                    if (build.BuildName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        build.Platform.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        filteredBuilds.Add(build);
                    }
                }

                m_FilteredBuilds = filteredBuilds.ToArray();
            }

            m_BuildListView.itemsSource = m_FilteredBuilds;
            m_BuildListView.Rebuild();

            // ListView preserves selectedIndex across Rebuild, which can leave the
            // highlight on a different BuildEntry than m_SelectedBuild. Remap the
            // selection by GUID, or clear it (and the right pane) if the selected
            // build was filtered out.
            RestoreOrClearSelection();

            UpdateEmptyState();
        }

        private void RestoreOrClearSelection()
        {
            if (m_SelectedBuild == null)
            {
                m_BuildListView.ClearSelection();
                return;
            }

            var selectedGuid = m_SelectedBuild.BuildSessionGUID;
            for (var i = 0; i < m_FilteredBuilds.Length; i++)
            {
                if (m_FilteredBuilds[i].BuildSessionGUID == selectedGuid)
                {
                    m_BuildListView.SetSelectionWithoutNotify(new[] { i } as IEnumerable<int>);
                    return;
                }
            }

            m_BuildListView.ClearSelection();
            m_SelectedBuild = null;
            m_SelectedBuildAnalysis = null;
            m_TabHost.SetSelection(null, null);
        }

        private void UpdateEmptyState()
        {
            var hasBuilds = m_FilteredBuilds.Length > 0;

            m_BuildListView.style.display = hasBuilds ? DisplayStyle.Flex : DisplayStyle.None;
            m_EmptyState.style.display = hasBuilds ? DisplayStyle.None : DisplayStyle.Flex;

            if (!hasBuilds)
            {
                if (m_AllBuilds.Length == 0)
                {
                    m_EmptyStateTitle.text = "No builds available";
                    m_EmptyStateDescription.text = "Builds will appear here automatically\nafter you build your project.";
                }
                else
                {
                    m_EmptyStateTitle.text = "No matching builds";
                    m_EmptyStateDescription.text = $"No builds match '{m_CurrentSearchText}'";
                }
            }
        }

        private void PopulateBuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Show in Explorer",
                _ => ShowInExplorer(),
                m_SelectedBuild != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy Path",
                _ => CopyPath(),
                m_SelectedBuild != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (Unsupported.IsDeveloperMode())
            {
                evt.menu.AppendAction("Run Analysis Again",
                    _ => RunAnalysisAgain(),
                    m_SelectedBuild != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Delete",
                _ => DeleteBuild(),
                m_SelectedBuild != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private void ShowInExplorer()
        {
            if (m_SelectedBuild == null)
                return;

            EditorUtility.OpenWithDefaultApp(m_SelectedBuild.FolderPath);
        }

        private void CopyPath()
        {
            if (m_SelectedBuild == null)
                return;

            EditorGUIUtility.systemCopyBuffer = m_SelectedBuild.FolderPath;
        }

        private void DeleteBuild()
        {
            if (m_SelectedBuild == null)
                return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete Build",
                $"Delete build data for '{m_SelectedBuild.BuildName}'?\nThis cannot be undone.",
                "Delete",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                m_Service.DeleteBuild(m_SelectedBuild.BuildSessionGUID);
                ClearSelectionAndRefresh();
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Access denied: {e.Message}");
            }
            catch (IOException e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to delete build: {e.Message}");
            }
        }

        private void RunAnalysisAgain()
        {
            if (m_SelectedBuild == null)
                return;

            try
            {
                m_SelectedBuildAnalysis = m_Service.RegenerateBuildAnalysis(m_SelectedBuild.BuildSessionGUID);
                m_TabHost.SetSelection(m_SelectedBuild, m_SelectedBuildAnalysis);
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to re-generate analysis: {e.Message}");
            }
        }

        private void OnDeleteAllBuilds()
        {
            if (m_AllBuilds.Length == 0)
                return;

            bool confirm = EditorUtility.DisplayDialog(
                "Delete All Builds",
                $"Delete all {m_AllBuilds.Length} build(s) from {BuildHistory.BuildHistoryDirectory}?\nThis cannot be undone.",
                "Delete All",
                "Cancel");

            if (!confirm)
                return;

            try
            {
                m_Service.DeleteAllBuilds();
                ClearSelectionAndRefresh();
                m_Watcher.SyncRevision();
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Access denied: {e.Message}");
            }
            catch (IOException e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to clear builds: {e.Message}");
            }
        }

        private void ClearSelectionAndRefresh()
        {
            m_SelectedBuild = null;
            m_SelectedBuildAnalysis = null;
            m_TabHost.SetSelection(null, null);
            EditorPrefs.DeleteKey(k_SelectedBuildSessionKey);
            RefreshBuildList();
        }

        private void SavePersistedState()
        {
            // Save splitter position
            EditorPrefs.SetFloat(k_SplitterKey, m_SplitView.fixedPaneInitialDimension);
        }
    }
}
