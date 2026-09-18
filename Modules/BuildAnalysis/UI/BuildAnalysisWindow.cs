// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading.Tasks;
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

    [EditorWindowTitle(title = "Build Analysis", icon = "BuildAnalysisWindow")]
    internal class BuildAnalysisWindow : EditorWindow, IBuildListActions
    {
        private const string k_OpenWindowCommand = "ContentBuild/OpenBuildAnalysisWindow";

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

        private const string k_HelpButtonTooltip = "Open Build Analysis documentation";
        // Manual topic slug (Documentation/ManualDocs/md/build-analysis-window-reference.md).
        private const string k_DocumentationPage = "build-analysis-window-reference";

        private const string k_LoadingMessage = "Analyzing build…";

        private TwoPaneSplitView m_SplitView;
        private ToolbarToggle m_InspectorToggle;
        private TabView m_TabView;
        private Tab m_OverviewTab;
        private Tab m_AssetsTab;
        private LoadingOverlay m_LoadingOverlay;
        private BuildListPanel m_BuildListPanel;

        private BuildAnalysisService m_Service;
        private BuildAnalysisTabHost m_TabHost;
        private BuildHistoryWatcher m_Watcher;

        private SelectionGate m_Gate;

        private GUID m_PendingSelection;

        [MenuItem("Window/Analysis/Build Analysis")]
        internal static void ShowWindow()
        {
            ShowWindow(default);
        }

        /// <summary>
        /// Open the Build Analysis window and select the build with the given session GUID.
        /// Pass an empty GUID to open the window without changing the current selection.
        /// If no build matches the GUID, the window still opens and a warning is logged.
        /// </summary>
        internal static void ShowWindow(GUID buildSessionGUID)
        {
            if (CommandService.Exists(k_OpenWindowCommand))
            {
                CommandService.Execute(k_OpenWindowCommand, CommandHint.Menu, buildSessionGUID);
                return;
            }

            var window = GetWindow<BuildAnalysisWindow>(false);
            window.minSize = new Vector2(750, 400);

            // Request the build selection. The UI may not be built yet (CreateGUI can be deferred to a
            // later editor tick), in which case this no-ops now and CreateGUI re-runs it once the
            // build list exists. An empty GUID leaves the current selection untouched.
            window.m_PendingSelection = buildSessionGUID;
            window.ApplyPendingSelection();
        }

        private void ApplyPendingSelection()
        {
            // m_BuildListPanel is null until CreateGUI runs, keep the request pending until then.
            if (m_PendingSelection.Empty() || m_BuildListPanel == null)
                return;

            var buildSessionGUID = m_PendingSelection;
            m_PendingSelection = default;

            // Force a refresh from BuildHistory.
            // A caller can link to a build it just produced. BuildHistoryWatcher only polls
            // ~1Hz, so m_BuildListPanel's list can be stale and would not yet contain that build session guid.
            m_Service.Refresh();
            RefreshBuildList();

            if (!m_BuildListPanel.SelectBuild(buildSessionGUID))
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} No build session GUID found for '{buildSessionGUID}'.");
        }

        private void OnEnable()
        {
            var buildHistory = new BuildHistoryProvider();
            var fileSystem = new BuildAnalysisFileSystem();

            var enumerator = new BuildEnumerator(buildHistory);
            var converter = new BuildReportConverter();
            var assetResolver = new SourceBuildAssetResolver(buildHistory, converter);
            var analyzer = new BuildAnalyzer(converter, fileSystem, buildHistory, assetResolver);
            m_Service = new BuildAnalysisService(enumerator, analyzer, fileSystem, buildHistory);

            m_Watcher = new BuildHistoryWatcher(buildHistory);
            m_Watcher.BuildHistoryChanged += RefreshBuildList;
            m_Watcher.Enable();

            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            m_Service?.Dispose();
            m_Watcher.Disable();
            m_Watcher.BuildHistoryChanged -= RefreshBuildList;
            SavePersistedState();
        }

        // Cancel in-flight analysis before the domain is torn down so continuations don't run against a
        // half-dead window. OnEnable rebuilds a fresh service (and cancellation token) after the reload.
        private void OnBeforeAssemblyReload()
        {
            m_Service?.CancelPending();
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

            m_Gate = new SelectionGate(() => rootVisualElement?.panel != null);

            var buildListHost = rootVisualElement.Q<VisualElement>("build-list-host");
            m_BuildListPanel = new BuildListPanel(this);
            m_BuildListPanel.SelectionChanged += OnBuildSelectionChanged;
            buildListHost.Add(m_BuildListPanel);

            SetupInspectorToggle();
            SetupTabs();

            // Ctrl+Tab / Ctrl+Shift+Tab cycles the content tabs, from anywhere in the window.
            m_TabHost.RegisterShortcuts(rootVisualElement);

            var splitterPos = EditorPrefs.GetFloat(k_SplitterKey, 100);
            m_SplitView.fixedPaneInitialDimension = splitterPos;

            RefreshBuildList();
            ApplyPendingSelection();
        }

        private void SetupInspectorToggle()
        {
            var tabViewport = m_TabView.Q<VisualElement>(className: "unity-tab-view__content-viewport");
            if (tabViewport == null)
                throw new InvalidOperationException($"{BuildAnalysisConstants.k_ConsoleLogPrefix} TabView content viewport  .unity-tab-view__content-viewport not found.");

            SetupHelpButton(tabViewport);

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

        private void SetupHelpButton(VisualElement tabViewport)
        {
            var helpButton = new ToolbarButton(() => Help.BrowseURL(GetDocumentationUrl()))
            {
                name = "help-button",
                tooltip = k_HelpButtonTooltip,
            };
            helpButton.AddToClassList("help-button");
            tabViewport.Add(helpButton);
        }

        private static string GetDocumentationUrl()
        {
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            return $"https://docs.unity3d.com/{version.Major}.{version.Minor}/Documentation/Manual/{k_DocumentationPage}.html";
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

            m_LoadingOverlay = new LoadingOverlay();
            m_TabView.contentContainer.Add(m_LoadingOverlay);

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

        private async void OnBuildSelectionChanged(BuildEntry selection)
        {
            if (selection == null)
            {
                // Invalidate any in-flight load (so its continuation is dropped as stale) and clear the view.
                m_Gate.Clear();
                m_TabHost.SetSelection(null, null);
                m_LoadingOverlay.Hide();
                return;
            }

            // Already loading or showing this build (e.g. a build-list refresh re-fired selection) — skip.
            if (m_Gate.IsCurrentTarget(selection.BuildSessionGUID))
                return;

            await LoadAndApplyAsync(selection, () => m_Service.GetBuildAnalysisAsync(selection.BuildSessionGUID));
        }

        // Apply for both selection and regenerate: show the overlay, await the result, and
        // update the tabs only if this is still the latest request and the window is still alive.
        private async Task LoadAndApplyAsync(BuildEntry build, Func<Task<BuildAnalysis>> load)
        {
            var seq = m_Gate.Begin(build.BuildSessionGUID);

            // Show the loading overlay over the active tab's content before we await.
            m_LoadingOverlay.Show(k_LoadingMessage);

            BuildAnalysis analysis;
            try
            {
                analysis = await load();
            }
            catch (OperationCanceledException)
            {
                // Service was torn down / reloaded mid-load (Dispose or CancelPending cancelled the token
                // before the off-thread work started). Swallow so the async-void caller doesn't log it; the
                // window is gone or being rebuilt, so there's nothing to update.
                return;
            }
            catch (Exception e)
            {
                if (!m_Gate.IsStale(seq))
                {
                    m_TabHost.SetSelection(null, null); // clear to no-selection
                    m_LoadingOverlay.Hide();
                    m_Gate.Clear(); // a failed load isn't shown — drop the target so re-selecting retries
                    Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to analyze build: {e.Message}");
                }
                return;
            }

            if (m_Gate.IsStale(seq))
                return;

            m_TabHost.SetSelection(build, analysis);
            m_LoadingOverlay.Hide();

            // A build that produced no analysis isn't shown — drop the target so re-selecting retries.
            if (analysis == null)
                m_Gate.Clear();
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

        async void IBuildListActions.RegenerateAnalysis(BuildEntry build)
        {
            if (build == null)
                return;
            await LoadAndApplyAsync(build, () => m_Service.RegenerateBuildAnalysisAsync(build.BuildSessionGUID));
        }
    }

    // Decides which async selection/regenerate result is still worth applying.
    internal sealed class SelectionGate
    {
        private readonly Func<bool> m_IsAlive;
        private int m_Seq;
        private GUID m_TargetGuid;

        public SelectionGate(Func<bool> isAlive) => m_IsAlive = isAlive;

        public int Begin(GUID target)
        {
            m_TargetGuid = target;
            return ++m_Seq;
        }

        public bool IsStale(int seq) => seq != m_Seq || !m_IsAlive();

        public bool IsCurrentTarget(GUID guid) => guid == m_TargetGuid;

        public void Clear()
        {
            m_Seq++;
            m_TargetGuid = default;
        }
    }
}
