// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal interface IBuildListActions
    {
        void DeleteBuild(BuildEntry build);
        void DeleteAllBuilds();
        void ShowInExplorer(BuildEntry build);
        void CopyPath(BuildEntry build);
        void RegenerateAnalysis(BuildEntry build);
    }

    [EditorWindowTitle(title = "Build Analysis", icon = "UnityEditor.ProfilerWindow")]
    internal class BuildAnalysisWindow : EditorWindow, IBuildListActions
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
        private const string k_InspectorOpenKey = k_KeyPrefix + "InspectorPanelOpen";

        private const string k_InspectorToggleTooltip = "Toggle Inspector";
        private const string k_InspectorToggleDisabledTooltip = "The inspector is only available on the Assets tab";

        private TwoPaneSplitView m_SplitView;
        private ToolbarToggle m_InspectorToggle;
        private TabView m_TabView;
        private Tab m_OverviewTab;
        private Tab m_AssetsTab;
        private BuildListPanel m_BuildListPanel;

        private BuildAnalysisService m_Service;
        private BuildAnalysisTabHost m_TabHost;
        private BuildHistoryWatcher m_Watcher;
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
            m_TabView = rootVisualElement.Q<TabView>("main-tabs");
            m_OverviewTab = rootVisualElement.Q<Tab>("overview-tab");
            m_AssetsTab = rootVisualElement.Q<Tab>("assets-tab");

            var buildListHost = rootVisualElement.Q<VisualElement>("build-list-host");
            m_BuildListPanel = new BuildListPanel(this);
            m_BuildListPanel.SelectionChanged += OnBuildSelectionChanged;
            buildListHost.Add(m_BuildListPanel);

            SetupInspectorToggle();
            SetupTabs();

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
                tooltip = k_InspectorToggleTooltip,
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

            var assetsTabView = new AssetsTabView();
            assetsTabView.InspectorOpenRequested += () => m_InspectorToggle.value = true;
            m_TabHost.Register(m_AssetsTab, assetsTabView);

            // Only the Assets tab has an inspector; disable the toggle on tabs that don't.
            m_TabView.activeTabChanged += (_, activeTab) => UpdateInspectorToggleEnabled(activeTab);
            UpdateInspectorToggleEnabled(m_TabView.activeTab);

            m_TabHost.NotifyCurrentTabVisibility();
            m_TabHost.SetInspectorOpen(m_InspectorToggle.value);
            m_TabHost.SetSelection(null, null);
        }

        private void UpdateInspectorToggleEnabled(Tab activeTab)
        {
            var supportsInspector = activeTab == m_AssetsTab;
            m_InspectorToggle.SetEnabled(supportsInspector);
            m_InspectorToggle.tooltip = supportsInspector
                ? k_InspectorToggleTooltip
                : k_InspectorToggleDisabledTooltip;
        }

        private static void ApplyThemeClass(VisualElement view)
        {
            view.RemoveFromClassList(k_UssClassDark);
            view.RemoveFromClassList(k_UssClassLight);
            view.AddToClassList(EditorGUIUtility.isProSkin ? k_UssClassDark : k_UssClassLight);
        }

        private void RefreshBuildList()
        {
            m_BuildListPanel.SetBuilds(m_Service.GetBuilds(), BuildHistory.BuildHistoryLimit);
        }

        private void OnBuildSelectionChanged(BuildEntry selection)
        {
            if (selection == null)
            {
                m_SelectedBuildAnalysis = null;
                m_TabHost.SetSelection(null, null);
                return;
            }

            m_SelectedBuildAnalysis = m_Service.GetBuildAnalysis(selection.BuildSessionGUID);
            m_TabHost.SetSelection(selection, m_SelectedBuildAnalysis);
        }

        private void SavePersistedState()
        {
            // OnEnable/OnDisable can run without CreateGUI in between: on unmaximize Unity
            // deserializes the maximize backup window (firing OnEnable) and immediately destroys
            // it (firing OnDisable), but never shows it, so CreateGUI never assigns m_SplitView.
            if (m_SplitView == null)
                return;
            EditorPrefs.SetFloat(k_SplitterKey, m_SplitView.fixedPaneInitialDimension);
        }

        // ===== IBuildListActions =====

        void IBuildListActions.DeleteBuild(BuildEntry build)
        {
            if (build == null)
                return;

            var confirm = EditorUtility.DisplayDialog(
                "Delete Build Report Directory",
                $"Delete Build Report Directory from {BuildHistory.BuildHistoryDirectory}?\n\nThis does not delete asset database artifacts nor build outputs. This cannot be undone.",
                "Delete",
                "Cancel");
            if (!confirm)
                return;

            try
            {
                m_Service.DeleteBuild(build.BuildSessionGUID);
                m_BuildListPanel.ClearSelection();
                RefreshBuildList();
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

        void IBuildListActions.DeleteAllBuilds()
        {
            var builds = m_Service.GetBuilds();
            if (builds.Length == 0)
                return;

            var directoryNoun = builds.Length == 1 ? "Build Report Directory" : "Build Report Directories";
            var confirm = EditorUtility.DisplayDialog(
                "Delete All Build Report Directories",
                $"Delete all {builds.Length} {directoryNoun} from {BuildHistory.BuildHistoryDirectory}?\n\nThis does not delete asset database artifacts nor build outputs. This cannot be undone.",
                "Delete All",
                "Cancel");
            if (!confirm)
                return;

            try
            {
                m_Service.DeleteAllBuilds();
                m_BuildListPanel.ClearSelection();
                RefreshBuildList();
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

        void IBuildListActions.ShowInExplorer(BuildEntry build)
        {
            if (build == null)
                return;
            EditorUtility.OpenWithDefaultApp(build.FolderPath);
        }

        void IBuildListActions.CopyPath(BuildEntry build)
        {
            if (build == null)
                return;
            EditorGUIUtility.systemCopyBuffer = build.FolderPath;
        }

        void IBuildListActions.RegenerateAnalysis(BuildEntry build)
        {
            if (build == null)
                return;

            try
            {
                m_SelectedBuildAnalysis = m_Service.RegenerateBuildAnalysis(build.BuildSessionGUID);
                m_TabHost.SetSelection(build, m_SelectedBuildAnalysis);
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to re-generate analysis: {e.Message}");
            }
        }
    }
}
