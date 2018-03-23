// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define PERF_PROFILE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Accessibility;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Scripting;


namespace UnityEditor
{
    [EditorWindowTitle(title = "Profiler", useTypeNameAsIconName = true)]
    internal class ProfilerWindow : EditorWindow, IProfilerWindowController, IHasCustomMenu
    {
        internal static class Styles
        {
            public static readonly GUIContent addArea = EditorGUIUtility.TrTextContent("Add Profiler", "Add a profiler area");
            public static readonly GUIContent deepProfile = EditorGUIUtility.TrTextContent("Deep Profile", "Instrument all mono calls to investigate scripts");
            public static readonly GUIContent profileEditor = EditorGUIUtility.TrTextContent("Profile Editor", "Enable profiling of the editor");
            public static readonly GUIContent noData = EditorGUIUtility.TrTextContent("No frame data available");
            public static readonly GUIContent frameDebugger = EditorGUIUtility.TrTextContent("Open Frame Debugger", "Frame Debugger for current game view");
            public static readonly GUIContent noFrameDebugger = EditorGUIUtility.TrTextContent("Frame Debugger", "Open Frame Debugger (Current frame needs to be selected)");
            public static readonly GUIContent gatherObjectReferences = EditorGUIUtility.TrTextContent("Gather object references", "Collect reference information to see where objects are referenced from. Disable this to save memory");

            public static readonly GUIContent memRecord = EditorGUIUtility.TrTextContent("Mem Record", "Record activity in the native memory system");
            public static readonly GUIContent profilerRecord = EditorGUIUtility.TrTextContentWithIcon("Record", "Record profiling information", "Profiler.Record");
            public static readonly GUIContent profilerInstrumentation = EditorGUIUtility.TrTextContent("Instrumentation", "Add Profiler Instrumentation to selected functions");
            public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Profiler.PrevFrame", "Go back one frame");
            public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Profiler.NextFrame", "Go one frame forwards");
            public static readonly GUIContent currentFrame = EditorGUIUtility.TrTextContent("Current", "Go to current frame");
            public static readonly GUIContent frame = EditorGUIUtility.TrTextContent("Frame: ");
            public static readonly GUIContent clearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play");
            public static readonly GUIContent clearData = EditorGUIUtility.TrTextContent("Clear");
            public static readonly GUIContent saveWindowTitle = EditorGUIUtility.TrTextContent("Save Window");
            public static readonly GUIContent saveProfilingData = EditorGUIUtility.TrTextContent("Save", "Save current profiling information to a binary file");
            public static readonly GUIContent loadWindowTitle = EditorGUIUtility.TrTextContent("Load Window");
            public static readonly GUIContent loadProfilingData = EditorGUIUtility.TrTextContent("Load", "Load binary profiling information from a file. Shift click to append to the existing data");
            public static readonly string[] loadProfilingDataFileFilters = new string[] { L10n.Tr("Profiler files"), "data,raw", L10n.Tr("All files"), "*" };
            public static readonly GUIContent[] reasons = GetLocalizedReasons();

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode");

            internal static GUIContent[] GetLocalizedReasons()
            {
                GUIContent[] gc = new GUIContent[11];
                gc[(int)MemoryInfoGCReason.SceneObject] = EditorGUIUtility.TrTextContent("Scene object (Unloaded by loading a new scene or destroying it)");
                gc[(int)MemoryInfoGCReason.BuiltinResource] = EditorGUIUtility.TrTextContent("Builtin Resource (Never unloaded)");
                gc[(int)MemoryInfoGCReason.MarkedDontSave] = EditorGUIUtility.TrTextContent("Object is marked Don't Save. (Must be explicitly destroyed or it will leak)");
                gc[(int)MemoryInfoGCReason.AssetMarkedDirtyInEditor] = EditorGUIUtility.TrTextContent("Asset is dirty and must be saved first (Editor only)");

                gc[(int)MemoryInfoGCReason.SceneAssetReferencedByNativeCodeOnly] = EditorGUIUtility.TrTextContent("Asset type created from code or stored in the scene, referenced from native code.");
                gc[(int)MemoryInfoGCReason.SceneAssetReferenced] = EditorGUIUtility.TrTextContent("Asset type created from code or stored in the scene, referenced from scripts and native code.");

                gc[(int)MemoryInfoGCReason.AssetReferencedByNativeCodeOnly] = EditorGUIUtility.TrTextContent("Asset referenced from native code.");
                gc[(int)MemoryInfoGCReason.AssetReferenced] = EditorGUIUtility.TrTextContent("Asset referenced from scripts and native code.");

                gc[(int)MemoryInfoGCReason.NotApplicable] = EditorGUIUtility.TrTextContent("Not Applicable");
                return gc;
            }

            public static readonly GUIStyle background = "OL Box";
            public static readonly GUIStyle header = "OL title";
            public static readonly GUIStyle label = "OL label";
            public static readonly GUIStyle entryEven = "OL EntryBackEven";
            public static readonly GUIStyle entryOdd = "OL EntryBackOdd";
            public static readonly GUIStyle profilerGraphBackground = "ProfilerScrollviewBackground";

            static Styles()
            {
                profilerGraphBackground.overflow.left = -(int)Chart.kSideWidth;
            }
        }

        private static readonly ProfilerArea[] ms_StackedAreas = { ProfilerArea.CPU, ProfilerArea.GPU, ProfilerArea.UI, ProfilerArea.GlobalIllumination };

        [NonSerialized]
        bool m_Initialized;

        [SerializeField]
        SplitterState m_VertSplit;

        SplitterState m_ViewSplit = new SplitterState(new[] { 70f, 30f }, new[] { 450, 50 }, null);
        SplitterState m_NetworkSplit = new SplitterState(new[] { 20f, 80f }, new[] { 100, 100 }, null);

        // For keeping correct "Recording" state on window maximizing
        [SerializeField]
        private bool m_Recording;

        private AttachProfilerUI m_AttachProfilerUI = new AttachProfilerUI();

        private Vector2 m_GraphPos = Vector2.zero;
        private Vector2[] m_PaneScroll = new Vector2[(int)ProfilerArea.AreaCount];
        private Vector2 m_PaneScroll_AudioChannels = Vector2.zero;
        private Vector2 m_PaneScroll_AudioDSP = Vector2.zero;
        private Vector2 m_PaneScroll_AudioClips = Vector2.zero;

        [SerializeField]
        string m_ActiveNativePlatformSupportModule;

        static List<ProfilerWindow> m_ProfilerWindows = new List<ProfilerWindow>();

        [SerializeField]
        ProfilerViewType m_ViewType = ProfilerViewType.Hierarchy;

        [SerializeField]
        ProfilerArea m_CurrentArea = ProfilerArea.CPU;

        ProfilerMemoryView m_ShowDetailedMemoryPane = ProfilerMemoryView.Simple;
        ProfilerAudioView m_ShowDetailedAudioPane = ProfilerAudioView.Stats;

        [SerializeField]
        bool m_ShowInactiveDSPChains = false;

        [SerializeField]
        bool m_HighlightAudibleDSPChains = true;

        [SerializeField]
        float m_DSPGraphZoomFactor = 1.0f;

        int m_CurrentFrame = -1;
        int m_LastFrameFromTick = -1;
        int m_PrevLastFrame = -1;
        int m_LastAudioProfilerFrame = -1;

        // Profiler charts
        ProfilerChart[] m_Charts;

        float[] m_ChartOldMax = new[]
        {
            -1.0f, // Cpu
            -1.0f, // Gpu
            0, // Rendering,
            0, // Memory,
            0, // Audio,
            0, // Video,
            0, // Physics,
            0, // Physics2D,
            0, // NetworkMessages,
            0, // NetworkOperations,
            -1.0f, // UI,
            0, // UIDetails,
            0, // GlobalIllumination,
            0, // AreaCount,
        };
        float m_ChartMaxClamp = 70000.0f;

        // Profiling GUI constants
        const float kRowHeight = 16;
        const float kIndentPx = 16;
        const float kBaseIndent = 8;

        const float kNameColumnSize = 350;

        FrameDataView m_FrameDataView;

        [SerializeField]
        ProfilerFrameDataHierarchyView m_CPUFrameDataHierarchyView;
        [SerializeField]
        ProfilerFrameDataHierarchyView m_GPUFrameDataHierarchyView;

        ProfilerTimelineGUI m_CPUTimelineGUI;

        struct CachedProfilerPropertyConfig
        {
            public int frameIndex;
            public ProfilerArea area;
            public ProfilerViewType viewType;
            public ProfilerColumn sortType;
        }
        private CachedProfilerPropertyConfig m_CPUOrGPUProfilerPropertyConfig;
        private ProfilerProperty m_CPUOrGPUProfilerProperty;

        [SerializeField]
        private UISystemProfiler m_UISystemProfiler;

        private MemoryTreeList m_ReferenceListView;
        private MemoryTreeListClickable m_MemoryListView;
        private bool m_GatherObjectReferences = true;

        [SerializeField]
        private AudioProfilerGroupTreeViewState m_AudioProfilerGroupTreeViewState;
        private AudioProfilerGroupView m_AudioProfilerGroupView = null;
        private AudioProfilerGroupViewBackend m_AudioProfilerGroupViewBackend;

        [SerializeField]
        private AudioProfilerClipTreeViewState m_AudioProfilerClipTreeViewState;
        private AudioProfilerClipView m_AudioProfilerClipView = null;
        private AudioProfilerClipViewBackend m_AudioProfilerClipViewBackend;

        private AudioProfilerDSPView m_AudioProfilerDSPView;

        private ProfilerMemoryRecordMode m_SelectedMemRecordMode = ProfilerMemoryRecordMode.None;
        private readonly char s_CheckMark = '\u2714'; // unicode

        bool wantsMemoryRefresh { get { return m_MemoryListView.RequiresRefresh; } }

        [SerializeField]
        private bool m_ClearOnPlay;

        private static string[] ProfilerColumnNames(ProfilerColumn[] columns)
        {
            var allNames = Enum.GetNames(typeof(ProfilerColumn));
            var names = new string[columns.Length];

            for (var i = 0; i < columns.Length; i++)
            {
                switch (columns[i])
                {
                    case ProfilerColumn.FunctionName:
                        names[i] = LocalizationDatabase.GetLocalizedString("Overview");
                        break;
                    case ProfilerColumn.TotalPercent:
                        names[i] = LocalizationDatabase.GetLocalizedString("Total");
                        break;
                    case ProfilerColumn.SelfPercent:
                        names[i] = LocalizationDatabase.GetLocalizedString("Self");
                        break;
                    case ProfilerColumn.Calls:
                        names[i] = LocalizationDatabase.GetLocalizedString("Calls");
                        break;
                    case ProfilerColumn.GCMemory:
                        names[i] = LocalizationDatabase.GetLocalizedString("GC Alloc");
                        break;
                    case ProfilerColumn.TotalTime:
                        names[i] = LocalizationDatabase.GetLocalizedString("Time ms");
                        break;
                    case ProfilerColumn.SelfTime:
                        names[i] = LocalizationDatabase.GetLocalizedString("Self ms");
                        break;
                    case ProfilerColumn.DrawCalls:
                        names[i] = LocalizationDatabase.GetLocalizedString("DrawCalls");
                        break;
                    case ProfilerColumn.TotalGPUTime:
                        names[i] = LocalizationDatabase.GetLocalizedString("GPU ms");
                        break;
                    case ProfilerColumn.SelfGPUTime:
                        names[i] = LocalizationDatabase.GetLocalizedString("Self ms");
                        break;
                    case ProfilerColumn.TotalGPUPercent:
                        names[i] = LocalizationDatabase.GetLocalizedString("Total");
                        break;
                    case ProfilerColumn.SelfGPUPercent:
                        names[i] = LocalizationDatabase.GetLocalizedString("Self");
                        break;
                    case ProfilerColumn.WarningCount:
                        names[i] = LocalizationDatabase.GetLocalizedString("|Warnings");
                        break;
                    case ProfilerColumn.ObjectName:
                        names[i] = LocalizationDatabase.GetLocalizedString("Name");
                        break;
                    default:
                        names[i] = "ProfilerColumn." + allNames[(int)columns[i]];
                        break;
                }
            }

            return names;
        }

        const string kProfilerRecentSaveLoadProfilePath = "ProfilerRecentSaveLoadProfilePath";
        const string kProfilerEnabledSessionKey = "ProfilerEnabled";

        internal delegate void SelectionChangedCallback(string selectedPropertyPath);
        public event SelectionChangedCallback selectionChanged;

        public void SetSelectedPropertyPath(string path)
        {
            if (ProfilerDriver.selectedPropertyPath != path)
            {
                ProfilerDriver.selectedPropertyPath = path;
                selectionChanged.Invoke(path);
                UpdateCharts();
            }
        }

        public void ClearSelectedPropertyPath()
        {
            if (ProfilerDriver.selectedPropertyPath != string.Empty)
            {
                ProfilerDriver.selectedPropertyPath = string.Empty;
                selectionChanged.Invoke(string.Empty);
                UpdateCharts();
            }
        }

        public ProfilerProperty CreateProperty()
        {
            return CreateProperty(ProfilerColumn.DontSort);
        }

        public ProfilerProperty CreateProperty(ProfilerColumn sortType)
        {
            var property = new ProfilerProperty();
            property.SetRoot(GetActiveVisibleFrameIndex(), sortType, m_ViewType);
            property.onlyShowGPUSamples = m_CurrentArea == ProfilerArea.GPU;
            return property;
        }

        public int GetActiveVisibleFrameIndex()
        {
            // Update the current frame only at fixed intervals,
            // otherwise it looks weird when it is rapidly jumping around when we have a lot of repaints
            return m_CurrentFrame == -1 ? m_LastFrameFromTick : m_CurrentFrame;
        }

        public bool IsRecording()
        {
            return m_Recording && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || !ProfilerDriver.IsConnectionEditor());
        }

        void OnEnable()
        {
            InitializeIfNeeded();

            titleContent = GetLocalizedTitleContent();
            m_AttachProfilerUI.OnProfilerTargetChanged = ClearFramesCallback;
            m_ProfilerWindows.Add(this);
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged += Initialize;
        }

        void InitializeIfNeeded()
        {
            if (m_Initialized)
                return;

            Initialize();
        }

        void Initialize()
        {
            int historySize = ProfilerDriver.maxHistoryLength - 1;

            m_Charts = new ProfilerChart[(int)ProfilerArea.AreaCount];

            Color[] chartAreaColors = ProfilerColors.chartAreaColors;

            for (ProfilerArea i = 0; i < ProfilerArea.AreaCount; i++)
            {
                float scale = 1.0f;
                Chart.ChartType chartType = Chart.ChartType.Line;
                string[] statisticsNames = ProfilerDriver.GetGraphStatisticsPropertiesForArea(i);
                int length = statisticsNames.Length;
                if (Array.IndexOf(ms_StackedAreas, i) != -1)
                {
                    chartType = Chart.ChartType.StackedFill;
                    scale = 1.0f / 1000.0f;
                }

                ProfilerChart chart = CreateProfilerChart(i, chartType, scale, length);
                for (int s = 0; s < length; s++)
                {
                    chart.m_Series[s] = new ChartSeriesViewData(statisticsNames[s], historySize, chartAreaColors[s % chartAreaColors.Length]);
                    for (int frameIdx = 0; frameIdx < historySize; ++frameIdx)
                        chart.m_Series[s].xValues[frameIdx] = (float)frameIdx;
                }

                m_Charts[(int)i] = chart;
            }

            if (m_VertSplit == null || m_VertSplit.relativeSizes == null || m_VertSplit.relativeSizes.Length == 0)
                m_VertSplit = new SplitterState(new[] { 50f, 50f }, new[] { 50, 50 }, null);

            if (m_ReferenceListView == null)
                m_ReferenceListView = new MemoryTreeList(this, null);
            if (m_MemoryListView == null)
                m_MemoryListView = new MemoryTreeListClickable(this, m_ReferenceListView);

            if (m_CPUFrameDataHierarchyView == null)
                m_CPUFrameDataHierarchyView = new ProfilerFrameDataHierarchyView();
            m_CPUFrameDataHierarchyView.gpuView = false;
            m_CPUFrameDataHierarchyView.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_CPUFrameDataHierarchyView.selectionChanged += CPUOrGPUViewSelectionChanged;
            selectionChanged += m_CPUFrameDataHierarchyView.SetSelectionFromLegacyPropertyPath;

            if (m_GPUFrameDataHierarchyView == null)
                m_GPUFrameDataHierarchyView = new ProfilerFrameDataHierarchyView();
            m_GPUFrameDataHierarchyView.gpuView = true;
            m_GPUFrameDataHierarchyView.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_GPUFrameDataHierarchyView.selectionChanged += CPUOrGPUViewSelectionChanged;
            selectionChanged += m_GPUFrameDataHierarchyView.SetSelectionFromLegacyPropertyPath;

            m_CPUTimelineGUI = new ProfilerTimelineGUI(this);
            m_CPUTimelineGUI.viewTypeChanged += CPUOrGPUViewTypeChanged;

            m_UISystemProfiler = new UISystemProfiler();

            UpdateCharts();
            foreach (var chart in m_Charts)
                chart.LoadAndBindSettings();

            m_Initialized = true;
        }

        void CPUOrGPUViewSelectionChanged(int id)
        {
            if (m_FrameDataView == null || !m_FrameDataView.IsValid())
                return;

            SetSelectedPropertyPath(m_FrameDataView.GetItemPath(id));
        }

        void CPUOrGPUViewTypeChanged(ProfilerViewType viewtype)
        {
            if (m_ViewType == viewtype)
                return;

            m_ViewType = viewtype;
        }

        ProfilerChart CreateProfilerChart(ProfilerArea i, Chart.ChartType chartType, float scale, int length)
        {
            ProfilerChart newChart = (i == ProfilerArea.UIDetails)
                ? new UISystemProfilerChart(chartType, scale, length)
                : new ProfilerChart(i, chartType, scale, length);
            newChart.selected += OnChartSelected;
            newChart.closed += OnChartClosed;
            return newChart;
        }

        void OnChartClosed(Chart sender)
        {
            var currentAreaChart = m_Charts[(int)m_CurrentArea];
            if (currentAreaChart == sender)
            {
                m_CurrentArea = ProfilerArea.AreaCount;
                m_UISystemProfiler.CurrentAreaChanged(m_CurrentArea);
            }
            ProfilerChart profilerChart = (ProfilerChart)sender;
            profilerChart.active = false;
        }

        void OnChartSelected(Chart sender)
        {
            ProfilerArea newArea = m_CurrentArea;
            for (int i = 0, numCharts = m_Charts.Length; i < numCharts; ++i)
            {
                if (m_Charts[i] == sender)
                {
                    newArea = (ProfilerArea)i;
                    break;
                }
            }
            if (m_CurrentArea == newArea)
                return;
            m_CurrentArea = newArea;

            // if switched out of CPU area, reset selected property
            if (m_CurrentArea != ProfilerArea.CPU)
            {
                ClearSelectedPropertyPath();
            }
            m_UISystemProfiler.CurrentAreaChanged(m_CurrentArea);
            Repaint();
            GUIUtility.keyboardControl = 0;
            GUIUtility.ExitGUI();
        }

        void CheckForPlatformModuleChange()
        {
            if (m_ActiveNativePlatformSupportModule != EditorUtility.GetActiveNativePlatformSupportModuleName())
            {
                ProfilerDriver.ResetHistory();
                Initialize();
                m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
            }
        }

        void OnDisable()
        {
            m_ProfilerWindows.Remove(this);
            m_UISystemProfiler.CurrentAreaChanged(ProfilerArea.AreaCount);
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged -= Initialize;
        }

        void Awake()
        {
            if (!Profiler.supported)
                return;

            // Track enabled state per Editor session
            m_Recording = SessionState.GetBool(kProfilerEnabledSessionKey, true);

            // This event gets called every time when some other window is maximized and then unmaximized
            ProfilerDriver.enabled = m_Recording;

            m_SelectedMemRecordMode = ProfilerDriver.memoryRecordMode;
        }

        void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                ClearFramesCallback();
            }
        }

        void ClearFramesCallback()
        {
            if (m_ClearOnPlay)
                Clear();
        }

        void OnDestroy()
        {
            // When window is destroyed, we disable profiling
            if (Profiler.supported && !EditorApplication.isPlayingOrWillChangePlaymode)
                ProfilerDriver.enabled = false;
        }

        void OnFocus()
        {
            // set the real state of profiler. OnDestroy is called not only when window is destroyed, but also when maximized state is changed
            if (Profiler.supported)
                ProfilerDriver.enabled = m_Recording;
        }

        void OnLostFocus()
        {
            if (GUIUtility.hotControl != 0)
            {
                // The chart may not have had the chance to release the hot control before we lost focus.
                // This happens when changing the selected frame, which may pause the game and switch the focus to another view.
                for (int c = 0; c < m_Charts.Length; ++c)
                {
                    ProfilerChart chart = m_Charts[c];
                    chart.OnLostFocus();
                }
            }
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
        }

        private void OnToggleColorBlindMode()
        {
            UserAccessiblitySettings.colorBlindCondition = UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default
                ? ColorBlindCondition.Deuteranopia
                : ColorBlindCondition.Default;
        }

        static void ShowProfilerWindow()
        {
            EditorWindow.GetWindow<ProfilerWindow>(false);
        }

        [RequiredByNativeCode]
        static void RepaintAllProfilerWindows()
        {
            foreach (ProfilerWindow window in m_ProfilerWindows)
            {
                // This is useful hack when you need to profile in the editor and dont want it to affect your framerate...
                // NOTE: we should make this an option in the UI somehow...
                //if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick && EditorWindow.focusedWindow == window)

                if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick)
                {
                    window.m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
                    window.RepaintImmediately();
                }
            }
        }

        static void SetMemoryProfilerInfo(ObjectMemoryInfo[] memoryInfo, int[] referencedIndices)
        {
            foreach (var profilerWindow in m_ProfilerWindows)
            {
                if (profilerWindow.wantsMemoryRefresh)
                {
                    profilerWindow.m_MemoryListView.SetRoot(MemoryElementDataManager.GetTreeRoot(memoryInfo, referencedIndices));
                }
            }
        }

        static void SetProfileDeepScripts(bool deep)
        {
            bool currentDeep = ProfilerDriver.deepProfiling;
            if (currentDeep == deep)
                return;

            bool doApply = true;

            // When enabling / disabling deep script profiling we need to reload scripts. In play mode this might be intrusive. So ask the user first.
            if (EditorApplication.isPlaying)
            {
                if (deep)
                {
                    doApply = EditorUtility.DisplayDialog("Enable deep script profiling", "Enabling deep profiling requires reloading scripts.", "Reload", "Cancel");
                }
                else
                {
                    doApply = EditorUtility.DisplayDialog("Disable deep script profiling", "Disabling deep profiling requires reloading all scripts", "Reload", "Cancel");
                }
            }

            if (doApply)
            {
                ProfilerDriver.deepProfiling = deep;
                InternalEditorUtility.RequestScriptReload();
            }
        }

        string PickFrameLabel()
        {
            if (m_CurrentFrame == -1)
                return "Current";
            return (m_CurrentFrame + 1) + " / " + (ProfilerDriver.lastFrameIndex + 1);
        }

        void PrevFrame()
        {
            int previousFrame = ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame);
            if (previousFrame != -1)
                SetCurrentFrame(previousFrame);
        }

        void NextFrame()
        {
            int nextFrame = ProfilerDriver.GetNextFrameIndex(m_CurrentFrame);
            if (nextFrame != -1)
                SetCurrentFrame(nextFrame);
        }

        void DrawCPUTimelineViewToolbar(ProfilerTimelineGUI timelineView, FrameDataView frameDataView)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            timelineView.DrawToolbar(frameDataView);

            EditorGUILayout.EndHorizontal();

            HandleCommandEvents();
        }

        void HandleCommandEvents() {}

        private static bool CheckFrameData(ProfilerProperty property)
        {
            return property.frameDataReady;
        }

        private void DrawCPUOrGPUPane(ProfilerFrameDataHierarchyView hierarchyView, ProfilerTimelineGUI timelinePane)
        {
            var frameDataView = GetFrameDataView(m_ViewType, hierarchyView.sortedProfilerColumn, hierarchyView.sortedProfilerColumnAscending);

            if (timelinePane != null && m_ViewType == ProfilerViewType.Timeline)
            {
                DrawCPUTimelineViewToolbar(timelinePane, frameDataView);

                float lowerPaneSize = m_VertSplit.realSizes[1];
                lowerPaneSize -= EditorStyles.toolbar.CalcHeight(GUIContent.none, 10.0f) + 2.0f;
                timelinePane.DoGUI(frameDataView, position.width, position.height - lowerPaneSize, lowerPaneSize);
            }
            else
            {
                hierarchyView.DoGUI(frameDataView);
            }
        }

        public ProfilerProperty GetRootProfilerProperty(ProfilerColumn sortType)
        {
            if (m_CPUOrGPUProfilerProperty != null && m_CPUOrGPUProfilerPropertyConfig.frameIndex == GetActiveVisibleFrameIndex() && m_CPUOrGPUProfilerPropertyConfig.area == m_CurrentArea && m_CPUOrGPUProfilerPropertyConfig.viewType == m_ViewType && m_CPUOrGPUProfilerPropertyConfig.sortType == sortType)
            {
                m_CPUOrGPUProfilerProperty.ResetToRoot();
                return m_CPUOrGPUProfilerProperty;
            }

            if (m_CPUOrGPUProfilerProperty != null)
                m_CPUOrGPUProfilerProperty.Cleanup();
            m_CPUOrGPUProfilerProperty = CreateProperty(sortType);

            m_CPUOrGPUProfilerPropertyConfig.frameIndex = GetActiveVisibleFrameIndex();
            m_CPUOrGPUProfilerPropertyConfig.area = m_CurrentArea;
            m_CPUOrGPUProfilerPropertyConfig.viewType = m_ViewType;
            m_CPUOrGPUProfilerPropertyConfig.sortType = sortType;

            return m_CPUOrGPUProfilerProperty;
        }

        public FrameDataView GetFrameDataView(ProfilerViewType viewType, ProfilerColumn profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();
            if (m_FrameDataView != null && m_FrameDataView.IsValid())
            {
                if (m_FrameDataView.frameIndex == frameIndex && m_FrameDataView.viewType == viewType)
                    return m_FrameDataView;
            }

            m_FrameDataView = new FrameDataView(viewType, frameIndex, 0, profilerSortColumn, sortAscending);
            return m_FrameDataView;
        }

        private void DrawMemoryPane(SplitterState splitter)
        {
            DrawMemoryToolbar();

            if (m_ShowDetailedMemoryPane == ProfilerMemoryView.Simple)
                DrawOverviewText(ProfilerArea.Memory);
            else
                DrawDetailedMemoryPane(splitter);
        }

        private void DrawDetailedMemoryPane(SplitterState splitter)
        {
            SplitterGUILayout.BeginHorizontalSplit(splitter);

            m_MemoryListView.OnGUI();
            m_ReferenceListView.OnGUI();

            SplitterGUILayout.EndHorizontalSplit();
        }

        static Rect GenerateRect(ref int row, int indent)
        {
            var rect = new Rect(indent * kIndentPx + kBaseIndent, row * kRowHeight, 0, kRowHeight);
            rect.xMax = kNameColumnSize;

            row++;

            return rect;
        }

        private String[] msgNames =
        {
            "UserMessage", "ObjectDestroy", "ClientRpc", "ObjectSpawn", "Owner", "Command", "LocalPlayerTransform", "SyncEvent", "SyncVars", "SyncList", "ObjectSpawnScene", "NetworkInfo", "SpawnFinished", "ObjectHide", "CRC", "ClientAuthority"
        };

        private bool[] msgFoldouts = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };

        void DrawNetworkOperationsPane()
        {
            SplitterGUILayout.BeginHorizontalSplit(m_NetworkSplit);

            GUILayout.Label(ProfilerDriver.GetOverviewText(m_CurrentArea, GetActiveVisibleFrameIndex()), EditorStyles.wordWrappedLabel);

            m_PaneScroll[(int)m_CurrentArea] = GUILayout.BeginScrollView(m_PaneScroll[(int)m_CurrentArea], Styles.background);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Operation Detail");
            EditorGUILayout.LabelField("Over 5 Ticks");
            EditorGUILayout.LabelField("Over 10 Ticks");
            EditorGUILayout.LabelField("Total");
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel += 1;

            for (short msgId = 0; msgId < msgNames.Length; msgId++)
            {
                if (!NetworkDetailStats.m_NetworkOperations.ContainsKey(msgId))
                    continue;

                msgFoldouts[msgId] = EditorGUILayout.Foldout(msgFoldouts[msgId], msgNames[msgId] + ":");
                if (msgFoldouts[msgId])
                {
                    EditorGUILayout.BeginVertical();
                    var detail = NetworkDetailStats.m_NetworkOperations[msgId];

                    EditorGUI.indentLevel += 1;

                    foreach (var entryName in detail.m_Entries.Keys)
                    {
                        int tick = (int)Time.time;
                        var entry = detail.m_Entries[entryName];

                        if (entry.m_IncomingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("IN:" + entryName);
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }

                        if (entry.m_OutgoingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("OUT:" + entryName);
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUI.indentLevel -= 1;
            GUILayout.EndScrollView();
            SplitterGUILayout.EndHorizontalSplit();
        }


        private void AudioProfilerToggle(ProfilerCaptureFlags toggleFlag)
        {
            bool oldState = (AudioSettings.profilerCaptureFlags & (int)toggleFlag) != 0;
            bool newState = GUILayout.Toggle(oldState, "Record", EditorStyles.toolbarButton);
            if (oldState != newState)
                ProfilerDriver.SetAudioCaptureFlags((AudioSettings.profilerCaptureFlags & ~(int)toggleFlag) | (newState
                                                                                                               ? (int)toggleFlag
                                                                                                               : 0));
        }

        private void DrawAudioPane()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            ProfilerAudioView newShowDetailedAudioPane = m_ShowDetailedAudioPane;
            if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Stats, "Stats", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Stats;
            if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Channels, "Channels", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Channels;
            if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Groups, "Groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Groups;
            if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.ChannelsAndGroups, "Channels and groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.ChannelsAndGroups;
            if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.DSPGraph, "DSP Graph", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.DSPGraph;
            if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Clips, "Clips", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Clips;
            if (newShowDetailedAudioPane != m_ShowDetailedAudioPane)
            {
                m_ShowDetailedAudioPane = newShowDetailedAudioPane;
                m_LastAudioProfilerFrame = -1; // force update
            }
            if (m_ShowDetailedAudioPane == ProfilerAudioView.Stats)
            {
                GUILayout.Space(5);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                DrawOverviewText(m_CurrentArea);
            }
            else if (m_ShowDetailedAudioPane == ProfilerAudioView.DSPGraph)
            {
                GUILayout.Space(5);
                AudioProfilerToggle(ProfilerCaptureFlags.DSPNodes);
                GUILayout.Space(5);
                m_ShowInactiveDSPChains = GUILayout.Toggle(m_ShowInactiveDSPChains, "Show inactive", EditorStyles.toolbarButton);
                if (m_ShowInactiveDSPChains)
                    m_HighlightAudibleDSPChains = GUILayout.Toggle(m_HighlightAudibleDSPChains, "Highlight audible", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                var totalRect = GUILayoutUtility.GetRect(20f, 10000f, 10, 20000f);

                m_PaneScroll_AudioDSP = GUI.BeginScrollView(totalRect, m_PaneScroll_AudioDSP, new Rect(0, 0, 10000, 20000));

                var clippingRect = new Rect(m_PaneScroll_AudioDSP.x, m_PaneScroll_AudioDSP.y, totalRect.width, totalRect.height);

                if (m_AudioProfilerDSPView == null)
                    m_AudioProfilerDSPView = new AudioProfilerDSPView();

                ProfilerProperty property = CreateProperty();
                if (CheckFrameData(property))
                {
                    m_AudioProfilerDSPView.OnGUI(clippingRect, property, m_ShowInactiveDSPChains, m_HighlightAudibleDSPChains, ref m_DSPGraphZoomFactor, ref m_PaneScroll_AudioDSP);
                }
                property.Cleanup();

                GUI.EndScrollView();

                Repaint();
            }
            else if (m_ShowDetailedAudioPane == ProfilerAudioView.Clips)
            {
                GUILayout.Space(5);
                AudioProfilerToggle(ProfilerCaptureFlags.Clips);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                var totalRect = GUILayoutUtility.GetRect(20f, 20000f, 10, 10000f);
                var statsRect = new Rect(totalRect.x, totalRect.y, 230f, totalRect.height);
                var treeRect = new Rect(statsRect.xMax, totalRect.y, totalRect.width - statsRect.width, totalRect.height);

                // STATS
                var content = ProfilerDriver.GetOverviewText(m_CurrentArea, GetActiveVisibleFrameIndex());
                var textSize = EditorStyles.wordWrappedLabel.CalcSize(GUIContent.Temp(content));
                m_PaneScroll_AudioClips = GUI.BeginScrollView(statsRect, m_PaneScroll_AudioClips, new Rect(0, 0, textSize.x, textSize.y));
                GUI.Label(new Rect(3, 3, textSize.x, textSize.y), content, EditorStyles.wordWrappedLabel);
                GUI.EndScrollView();
                EditorGUI.DrawRect(new Rect(statsRect.xMax - 1, statsRect.y, 1, statsRect.height), Color.black);

                // TREE
                if (m_AudioProfilerClipTreeViewState == null)
                    m_AudioProfilerClipTreeViewState = new AudioProfilerClipTreeViewState();

                if (m_AudioProfilerClipViewBackend == null)
                    m_AudioProfilerClipViewBackend = new AudioProfilerClipViewBackend(m_AudioProfilerClipTreeViewState);

                ProfilerProperty property = CreateProperty();
                if (CheckFrameData(property))
                {
                    if (m_CurrentFrame == -1 || m_LastAudioProfilerFrame != m_CurrentFrame)
                    {
                        m_LastAudioProfilerFrame = m_CurrentFrame;
                        var sourceItems = property.GetAudioProfilerClipInfo();
                        if (sourceItems != null && sourceItems.Length > 0)
                        {
                            var items = new List<AudioProfilerClipInfoWrapper>();
                            foreach (var s in sourceItems)
                            {
                                items.Add(new AudioProfilerClipInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset)));
                            }
                            m_AudioProfilerClipViewBackend.SetData(items);
                            if (m_AudioProfilerClipView == null)
                            {
                                m_AudioProfilerClipView = new AudioProfilerClipView(this, m_AudioProfilerClipTreeViewState);
                                m_AudioProfilerClipView.Init(treeRect, m_AudioProfilerClipViewBackend);
                            }
                        }
                    }
                    if (m_AudioProfilerClipView != null)
                        m_AudioProfilerClipView.OnGUI(treeRect);
                }
                property.Cleanup();
            }
            else
            {
                GUILayout.Space(5);
                AudioProfilerToggle(ProfilerCaptureFlags.Channels);
                GUILayout.Space(5);
                bool resetAllAudioClipPlayCountsOnPlay = GUILayout.Toggle(AudioUtil.resetAllAudioClipPlayCountsOnPlay, "Reset play count on play", EditorStyles.toolbarButton);
                if (resetAllAudioClipPlayCountsOnPlay != AudioUtil.resetAllAudioClipPlayCountsOnPlay)
                    AudioUtil.resetAllAudioClipPlayCountsOnPlay = resetAllAudioClipPlayCountsOnPlay;
                if (Unsupported.IsDeveloperMode())
                {
                    GUILayout.Space(5);
                    bool showAllGroups = EditorPrefs.GetBool("AudioProfilerShowAllGroups");
                    bool newShowAllGroups = GUILayout.Toggle(showAllGroups, "Show all groups (dev mode only)", EditorStyles.toolbarButton);
                    if (showAllGroups != newShowAllGroups)
                        EditorPrefs.SetBool("AudioProfilerShowAllGroups", newShowAllGroups);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                var totalRect = GUILayoutUtility.GetRect(20f, 20000f, 10, 10000f);
                var statsRect = new Rect(totalRect.x, totalRect.y, 230f, totalRect.height);
                var treeRect = new Rect(statsRect.xMax, totalRect.y, totalRect.width - statsRect.width, totalRect.height);

                // STATS
                var content = ProfilerDriver.GetOverviewText(m_CurrentArea, GetActiveVisibleFrameIndex());
                var textSize = EditorStyles.wordWrappedLabel.CalcSize(GUIContent.Temp(content));
                m_PaneScroll_AudioChannels = GUI.BeginScrollView(statsRect, m_PaneScroll_AudioChannels, new Rect(0, 0, textSize.x, textSize.y));
                GUI.Label(new Rect(3, 3, textSize.x, textSize.y), content, EditorStyles.wordWrappedLabel);
                GUI.EndScrollView();
                EditorGUI.DrawRect(new Rect(statsRect.xMax - 1, statsRect.y, 1, statsRect.height), Color.black);

                // TREE
                if (m_AudioProfilerGroupTreeViewState == null)
                    m_AudioProfilerGroupTreeViewState = new AudioProfilerGroupTreeViewState();

                if (m_AudioProfilerGroupViewBackend == null)
                    m_AudioProfilerGroupViewBackend = new AudioProfilerGroupViewBackend(m_AudioProfilerGroupTreeViewState);

                ProfilerProperty property = CreateProperty();
                if (CheckFrameData(property))
                {
                    if (m_CurrentFrame == -1 || m_LastAudioProfilerFrame != m_CurrentFrame)
                    {
                        m_LastAudioProfilerFrame = m_CurrentFrame;
                        var sourceItems = property.GetAudioProfilerGroupInfo();
                        if (sourceItems != null && sourceItems.Length > 0)
                        {
                            var items = new List<AudioProfilerGroupInfoWrapper>();
                            foreach (var s in sourceItems)
                            {
                                bool isGroup = (s.flags & AudioProfilerGroupInfoHelper.AUDIOPROFILER_FLAGS_GROUP) != 0;
                                if (m_ShowDetailedAudioPane == ProfilerAudioView.Channels && isGroup)
                                    continue;
                                if (m_ShowDetailedAudioPane == ProfilerAudioView.Groups && !isGroup)
                                    continue;
                                items.Add(new AudioProfilerGroupInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset), property.GetAudioProfilerNameByOffset(s.objectNameOffset), m_ShowDetailedAudioPane == ProfilerAudioView.Channels));
                            }
                            m_AudioProfilerGroupViewBackend.SetData(items);
                            if (m_AudioProfilerGroupView == null)
                            {
                                m_AudioProfilerGroupView = new AudioProfilerGroupView(this, m_AudioProfilerGroupTreeViewState);
                                m_AudioProfilerGroupView.Init(treeRect, m_AudioProfilerGroupViewBackend);
                            }
                        }
                    }
                    if (m_AudioProfilerGroupView != null)
                        m_AudioProfilerGroupView.OnGUI(treeRect, m_ShowDetailedAudioPane == ProfilerAudioView.Channels);
                }
                property.Cleanup();
            }
        }

        static void DrawBackground(int row, bool selected)
        {
            var currentRect = new Rect(1, kRowHeight * row, GUIClip.visibleRect.width, kRowHeight);

            var background = (row % 2 == 0
                              ? Styles.entryEven
                              : Styles.entryOdd);
            if (Event.current.type == EventType.Repaint)
                background.Draw(currentRect, GUIContent.none, false, false, selected, false);
        }

        private void DrawMemoryToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_ShowDetailedMemoryPane = (ProfilerMemoryView)EditorGUILayout.EnumPopup(m_ShowDetailedMemoryPane, EditorStyles.toolbarDropDown, GUILayout.Width(70f));

            GUILayout.Space(5f);

            if (m_ShowDetailedMemoryPane == ProfilerMemoryView.Detailed)
            {
                if (GUILayout.Button("Take Sample: " + m_AttachProfilerUI.GetConnectedProfiler(), EditorStyles.toolbarButton))
                    RefreshMemoryData();

                m_GatherObjectReferences = GUILayout.Toggle(m_GatherObjectReferences, Styles.gatherObjectReferences, EditorStyles.toolbarButton);

                if (m_AttachProfilerUI.IsEditor())
                    GUILayout.Label("Memory usage in editor is not as it would be in a player", EditorStyles.toolbarButton);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshMemoryData()
        {
            m_MemoryListView.RequiresRefresh = true;
            ProfilerDriver.RequestObjectMemoryInfo(m_GatherObjectReferences);
        }

        private static void UpdateChartGrid(float timeMax, ChartViewData data)
        {
            if (timeMax < 1500)
            {
                data.SetGrid(new float[] { 1000, 250, 100 }, new[] { "1ms (1000FPS)", "0.25ms (4000FPS)", "0.1ms (10000FPS)" });
            }
            else if (timeMax < 10000)
            {
                data.SetGrid(new float[] { 8333, 4000, 1000 }, new[] { "8ms (120FPS)", "4ms (250FPS)", "1ms (1000FPS)" });
            }
            else if (timeMax < 30000)
            {
                data.SetGrid(new float[] { 16667, 10000, 5000 }, new[] { "16ms (60FPS)", "10ms (100FPS)", "5ms (200FPS)" });
            }
            else if (timeMax < 100000)
            {
                data.SetGrid(new float[] { 66667, 33333, 16667 }, new[] { "66ms (15FPS)", "33ms (30FPS)", "16ms (60FPS)" });
            }
            else
            {
                data.SetGrid(new float[] { 500000, 200000, 66667 }, new[] { "500ms (2FPS)", "200ms (5FPS)", "66ms (15FPS)" });
            }
        }

        private void UpdateCharts()
        {
            int historyLength = ProfilerDriver.maxHistoryLength - 1;
            int firstEmptyFrame = ProfilerDriver.lastFrameIndex - historyLength;
            int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);

            // Collect chart values
            foreach (var chart in m_Charts)
            {
                UpdateSingleChart(chart, firstEmptyFrame, firstFrame);
            }

            // CPU chart overlay values
            string selectedName = ProfilerDriver.selectedPropertyPath;
            bool hasCPUOverlay = (selectedName != string.Empty) && m_CurrentArea == ProfilerArea.CPU;
            ProfilerChart cpuChart = m_Charts[(int)ProfilerArea.CPU];
            if (hasCPUOverlay)
            {
                cpuChart.m_Data.hasOverlay = true;
                int numCharts = cpuChart.m_Data.numSeries;
                for (int i = 0; i < numCharts; ++i)
                {
                    var chart = cpuChart.m_Data.series[i];
                    cpuChart.m_Data.overlays[i] = new ChartSeriesViewData(chart.name, chart.yValues.Length, chart.color);
                    for (int frameIdx = 0; frameIdx < chart.yValues.Length; ++frameIdx)
                        cpuChart.m_Data.overlays[i].xValues[frameIdx] = (float)frameIdx;
                    int identifier = ProfilerDriver.GetStatisticsIdentifier(string.Format("Selected{0}", cpuChart.m_Data.series[i].name));
                    float maxValue;
                    ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, cpuChart.m_DataScale, cpuChart.m_Data.overlays[i].yValues, out maxValue);
                }
            }
            else
            {
                cpuChart.m_Data.hasOverlay = false;
            }

            // CPU, GPU & UI chart scale value
            for (int i = 0; i < ms_StackedAreas.Length; i++)
                ComputeChartScaleValue(ms_StackedAreas[i], historyLength, firstEmptyFrame, firstFrame);

            // Is GPU Profiling supported warning
            string warning = null;
            if (!ProfilerDriver.isGPUProfilerSupported)
            {
                warning = "GPU profiling is not supported by the graphics card driver. Please update to a newer version if available.";

                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    if (!ProfilerDriver.isGPUProfilerSupportedByOS)
                        warning = "GPU profiling requires Mac OS X 10.7 (Lion) and a capable video card. GPU profiling is currently not supported on mobile.";
                    else
                        warning = "GPU profiling is not supported by the graphics card driver (or it was disabled because of driver bugs).";
                }
            }
            m_Charts[(int)ProfilerArea.GPU].m_NotSupportedWarning = warning;
        }

        private void ComputeChartScaleValue(ProfilerArea i, int historyLength, int firstEmptyFrame, int firstFrame)
        {
            ProfilerChart chart = m_Charts[(int)i];
            float timeMax = 0.0f;
            float timeMaxExcludeFirst = 0.0f;
            for (int k = 0; k < historyLength; k++)
            {
                float timeNow = 0.0F;
                for (int j = 0; j < chart.m_Series.Length; j++)
                {
                    if (chart.m_Series[j].enabled)
                        timeNow += chart.m_Series[j].yValues[k];
                }
                if (timeNow > timeMax)
                    timeMax = timeNow;
                if (timeNow > timeMaxExcludeFirst && k + firstEmptyFrame >= firstFrame + 1)
                    timeMaxExcludeFirst = timeNow;
            }
            if (timeMaxExcludeFirst != 0.0f)
                timeMax = timeMaxExcludeFirst;

            timeMax = Math.Min(timeMax, m_ChartMaxClamp);

            // Do not apply the new scale immediately, but gradually go towards it
            if (m_ChartOldMax[(int)i] > 0.0f)
                timeMax = Mathf.Lerp(m_ChartOldMax[(int)i], timeMax, 0.4f);
            m_ChartOldMax[(int)i] = timeMax;

            for (int k = 0; k < chart.m_Data.numSeries; ++k)
                chart.m_Data.series[k].rangeAxis = new Vector2(0f, timeMax);
            UpdateChartGrid(timeMax, chart.m_Data);
        }

        internal static void UpdateSingleChart(ProfilerChart chart, int firstEmptyFrame, int firstFrame)
        {
            float totalMaxValue = 1;
            var maxValues = new float[chart.m_Series.Length];
            for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
            {
                int identifier = ProfilerDriver.GetStatisticsIdentifier(chart.m_Series[i].name);
                float maxValue;
                ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, chart.m_DataScale, chart.m_Series[i].yValues, out maxValue);

                // Minimum size so we don't generate nans during drawing
                maxValue = Mathf.Max(maxValue, 0.0001F);

                if (maxValue > totalMaxValue)
                    totalMaxValue = maxValue;

                if (chart.m_Type == Chart.ChartType.Line)
                {
                    // Scale line charts so they never hit the top. Scale them slightly differently for each line
                    // so that in "no stuff changing" case they will not end up being exactly the same.
                    maxValues[i] = maxValue * (1.05f + i * 0.05f);
                    chart.m_Series[i].rangeAxis = new Vector2(0f, maxValues[i]);
                }
                else
                {
                    maxValues[i] = maxValue;
                }
            }
            if (chart.m_Area == ProfilerArea.NetworkMessages || chart.m_Area == ProfilerArea.NetworkOperations)
            {
                for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
                    chart.m_Series[i].rangeAxis = new Vector2(0f, 0.9f * totalMaxValue);
                chart.m_Data.maxValue = totalMaxValue;
            }
            chart.m_Data.Assign(chart.m_Series, firstEmptyFrame, firstFrame);

            if (chart is UISystemProfilerChart)
                ((UISystemProfilerChart)chart).Update(firstFrame, ProfilerDriver.maxHistoryLength - 1);
        }

        void AddAreaClick(object userData, string[] options, int selected)
        {
            m_Charts[selected].active = true;
        }

        void MemRecordModeClick(object userData, string[] options, int selected)
        {
            m_SelectedMemRecordMode = (ProfilerMemoryRecordMode)selected;
            ProfilerDriver.memoryRecordMode = m_SelectedMemRecordMode;
        }

        void SaveProfilingData()
        {
            string recent = EditorPrefs.GetString(kProfilerRecentSaveLoadProfilePath);
            string directory = string.IsNullOrEmpty(recent)
                ? ""
                : System.IO.Path.GetDirectoryName(recent);
            string filename = string.IsNullOrEmpty(recent)
                ? ""
                : System.IO.Path.GetFileName(recent);

            string selected = EditorUtility.SaveFilePanel(Styles.saveWindowTitle.text, directory, filename, "data");
            if (selected.Length != 0)
            {
                EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, selected);
                ProfilerDriver.SaveProfile(selected);
            }

            // Opened a save pop-up, MacOS will redraw the window so bail out now
            EditorGUIUtility.ExitGUI();
        }

        void LoadProfilingData(bool keepExistingData)
        {
            string recent = EditorPrefs.GetString(kProfilerRecentSaveLoadProfilePath);
            string selected = EditorUtility.OpenFilePanelWithFilters(Styles.loadWindowTitle.text, recent, Styles.loadProfilingDataFileFilters);
            if (selected.Length != 0)
            {
                EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, selected);
                if (ProfilerDriver.LoadProfile(selected, keepExistingData))
                {
                    // Stop current profiling if data was loaded successfully
                    ProfilerDriver.enabled = m_Recording = false;
                    SessionState.SetBool(kProfilerEnabledSessionKey, m_Recording);
                    NetworkDetailStats.m_NetworkOperations.Clear();
                }
            }

            // Opened a load pop-up, MacOS will redraw the window so bail out now
            EditorGUIUtility.ExitGUI();
        }

        private void DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Graph types
            Rect popupRect = GUILayoutUtility.GetRect(Styles.addArea, EditorStyles.toolbarDropDown, GUILayout.Width(120));
            if (EditorGUI.DropdownButton(popupRect, Styles.addArea, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                int length = m_Charts.Length;
                var names = new string[length];
                var enabled = new bool[length];
                for (int c = 0; c < length; ++c)
                {
                    names[c] = ((ProfilerArea)c).ToString();
                    enabled[c] = !m_Charts[c].active;
                }
                EditorUtility.DisplayCustomMenu(popupRect, names, enabled, null, AddAreaClick, null);
            }

            GUILayout.FlexibleSpace();

            // Record
            var profilerEnabled = GUILayout.Toggle(m_Recording, Styles.profilerRecord, EditorStyles.toolbarButton);
            if (profilerEnabled != m_Recording)
            {
                ProfilerDriver.enabled = profilerEnabled;
                m_Recording = profilerEnabled;
                SessionState.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            }

            EditorGUI.BeginDisabledGroup(!m_AttachProfilerUI.IsEditor());

            // Deep profiling
            SetProfileDeepScripts(GUILayout.Toggle(ProfilerDriver.deepProfiling, Styles.deepProfile, EditorStyles.toolbarButton));

            // Profile Editor
            ProfilerDriver.profileEditor = GUILayout.Toggle(ProfilerDriver.profileEditor, Styles.profileEditor, EditorStyles.toolbarButton);

            EditorGUI.EndDisabledGroup();

            // Engine attach
            m_AttachProfilerUI.OnGUILayout(this);

            // Allocation callstacks
            AllocationCallstacksToolbarItem();

            GUILayout.FlexibleSpace();

            SetClearOnPlay(GUILayout.Toggle(GetClearOnPlay(), Styles.clearOnPlay, EditorStyles.toolbarButton));

            // Clear
            if (GUILayout.Button(Styles.clearData, EditorStyles.toolbarButton))
            {
                Clear();
            }

            // Load profile
            if (GUILayout.Button(Styles.loadProfilingData, EditorStyles.toolbarButton))
                LoadProfilingData(Event.current.shift);

            // Save profile
            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex == -1))
            {
                if (GUILayout.Button(Styles.saveProfilingData, EditorStyles.toolbarButton))
                    SaveProfilingData();
            }

            GUILayout.Space(5);

            FrameNavigationControls();

            GUILayout.EndHorizontal();
        }

        void AllocationCallstacksToolbarItem()
        {
            // Memory record
            Styles.memRecord.text = "Allocation Callstacks";
            if (m_SelectedMemRecordMode != ProfilerMemoryRecordMode.None)
                Styles.memRecord.text += " [" + s_CheckMark + "]";

            Rect popupRect = GUILayoutUtility.GetRect(Styles.memRecord, EditorStyles.toolbarDropDown, GUILayout.Width(130));
            if (EditorGUI.DropdownButton(popupRect, Styles.memRecord, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                string[] names = new string[]
                {
                    "None", "Managed Allocations"
                };
                if (Unsupported.IsDeveloperMode())
                {
                    names = new string[]
                    {
                        "None", "Managed Allocations", "All Allocations (fast)", "All Allocations (full)"
                    };
                }

                var enabled = new bool[names.Length];
                for (int c = 0; c < names.Length; ++c)
                    enabled[c] = true;
                var selected = new int[] { (int)m_SelectedMemRecordMode };
                EditorUtility.DisplayCustomMenu(popupRect, names, enabled, selected, MemRecordModeClick, null);
            }
        }

        void Clear()
        {
            m_CPUFrameDataHierarchyView.Clear();
            m_GPUFrameDataHierarchyView.Clear();

            ProfilerDriver.ClearAllFrames();

            NetworkDetailStats.m_NetworkOperations.Clear();
        }

        private void FrameNavigationControls()
        {
            if (m_CurrentFrame > ProfilerDriver.lastFrameIndex)
            {
                SetCurrentFrameDontPause(ProfilerDriver.lastFrameIndex);
            }

            // Frame number
            GUILayout.Label(Styles.frame, EditorStyles.miniLabel);
            GUILayout.Label("   " + PickFrameLabel(), EditorStyles.miniLabel, GUILayout.Width(100));

            // Previous/next/current buttons

            GUI.enabled = ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame) != -1;
            if (GUILayout.Button(Styles.prevFrame, EditorStyles.toolbarButton))
                PrevFrame();

            GUI.enabled = ProfilerDriver.GetNextFrameIndex(m_CurrentFrame) != -1;
            if (GUILayout.Button(Styles.nextFrame, EditorStyles.toolbarButton))
                NextFrame();

            GUI.enabled = true;
            GUILayout.Space(10);
            if (GUILayout.Button(Styles.currentFrame, EditorStyles.toolbarButton))
            {
                SetCurrentFrame(-1);
                m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
            }
        }

        static void DrawOtherToolbar(ProfilerArea area)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (area == ProfilerArea.Rendering)
            {
                if (GUILayout.Button(GUI.enabled
                        ? Styles.frameDebugger
                        : Styles.noFrameDebugger, EditorStyles.toolbarButton))
                {
                    FrameDebuggerWindow dbg = FrameDebuggerWindow.ShowFrameDebuggerWindow();
                    dbg.EnableIfNeeded();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverviewText(ProfilerArea area)
        {
            if (area == ProfilerArea.AreaCount)
                return;

            string activeText = ProfilerDriver.GetOverviewText(area, GetActiveVisibleFrameIndex());
            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);

            m_PaneScroll[(int)area] = GUILayout.BeginScrollView(m_PaneScroll[(int)area], Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            GUILayout.EndScrollView();
        }

        private void DrawPane(ProfilerArea area)
        {
            DrawOtherToolbar(area);
            DrawOverviewText(area);
        }

        void SetCurrentFrameDontPause(int frame)
        {
            m_CurrentFrame = frame;
        }

        void SetCurrentFrame(int frame)
        {
            if (frame != -1 && ProfilerDriver.enabled && !ProfilerDriver.profileEditor && m_CurrentFrame != frame && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPaused = true;

            if (ProfilerInstrumentationPopup.InstrumentationEnabled)
                ProfilerInstrumentationPopup.UpdateInstrumentableFunctions();

            SetCurrentFrameDontPause(frame);
        }

        void OnGUI()
        {
            CheckForPlatformModuleChange();
            InitializeIfNeeded();

            // Initialization
            DrawMainToolbar();

            SplitterGUILayout.BeginVerticalSplit(m_VertSplit);

            m_GraphPos = EditorGUILayout.BeginScrollView(m_GraphPos, Styles.profilerGraphBackground);

            if (m_PrevLastFrame != ProfilerDriver.lastFrameIndex)
            {
                UpdateCharts();
                m_PrevLastFrame = ProfilerDriver.lastFrameIndex;
            }

            int newCurrentFrame = m_CurrentFrame;
            for (int c = 0; c < m_Charts.Length; ++c)
            {
                ProfilerChart chart = m_Charts[c];
                if (!chart.active)
                    continue;

                newCurrentFrame = chart.DoChartGUI(newCurrentFrame, m_CurrentArea);
            }

            if (newCurrentFrame != m_CurrentFrame)
            {
                SetCurrentFrame(newCurrentFrame);
                Repaint();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.BeginVertical();

            switch (m_CurrentArea)
            {
                case ProfilerArea.CPU:
                    DrawCPUOrGPUPane(m_CPUFrameDataHierarchyView, m_CPUTimelineGUI);
                    break;
                case ProfilerArea.GPU:
                    DrawCPUOrGPUPane(m_GPUFrameDataHierarchyView, null);
                    break;
                case ProfilerArea.Memory:
                    DrawMemoryPane(m_ViewSplit);
                    break;
                case ProfilerArea.Audio:
                    DrawAudioPane();
                    break;
                case ProfilerArea.NetworkMessages:
                    DrawPane(m_CurrentArea);
                    break;
                case ProfilerArea.NetworkOperations:
                    DrawNetworkOperationsPane();
                    break;
                case ProfilerArea.UI:
                case ProfilerArea.UIDetails:
                    m_UISystemProfiler.DrawUIPane(this, m_CurrentArea, (UISystemProfilerChart)m_Charts[(int)ProfilerArea.UIDetails]);
                    break;
                default:
                    DrawPane(m_CurrentArea);
                    break;
            }

            GUILayout.EndVertical();

            SplitterGUILayout.EndVerticalSplit();
        }

        public void SetClearOnPlay(bool enabled)
        {
            m_ClearOnPlay = enabled;
        }

        public bool GetClearOnPlay()
        {
            return m_ClearOnPlay;
        }
    }
}
