// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Accessibility;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine.Scripting;
using UnityEngine.Experimental.Networking.PlayerConnection;
using ConnectionUtility = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUIUtility;
using ConnectionGUILayout = UnityEditor.Experimental.Networking.PlayerConnection.EditorGUILayout;


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

            public static readonly GUIContent optionsButtonContent = EditorGUIUtility.TrIconContent("_Popup", "Options");

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode");

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

        private const string k_CPUUnstackableSeriesName = "Others";
        private static readonly ProfilerArea[] ms_StackedAreas = { ProfilerArea.CPU, ProfilerArea.GPU, ProfilerArea.UI, ProfilerArea.GlobalIllumination };

        [NonSerialized]
        bool m_Initialized;

        [SerializeField]
        SplitterState m_VertSplit;


        const int k_VertSplitterMinSizes = 100;
        const float k_LineHeight = 16.0f;
        const float k_RightPaneMinSize = 80;

        // For keeping correct "Recording" state on window maximizing
        [SerializeField]
        private bool m_Recording;

        private IConnectionState m_AttachProfilerState;

        private Vector2 m_GraphPos = Vector2.zero;

        [SerializeField]
        string m_ActiveNativePlatformSupportModule = null;

        static List<ProfilerWindow> m_ProfilerWindows = new List<ProfilerWindow>();

        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests through reflection
        [SerializeField]
        ProfilerViewType m_ViewType = ProfilerViewType.Timeline;

        [SerializeField]
        ProfilerArea? m_CurrentArea = ProfilerArea.CPU;

        int m_CurrentFrame = -1;
        int m_LastFrameFromTick = -1;
        int m_PrevLastFrame = -1;

        // Profiler charts
        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests through reflection
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

        HierarchyFrameDataView m_FrameDataView;

        [SerializeField]
        ProfilerModuleBase[] m_ProfilerModules;

        // used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal T GetProfilerModule<T>(ProfilerArea area) where T : ProfilerModuleBase
        {
            var index = (int)area;
            if (index >= 0 && index < m_ProfilerModules.Length)
                return m_ProfilerModules[index] as T;
            return null;
        }

        private ProfilerMemoryRecordMode m_SelectedMemRecordMode = ProfilerMemoryRecordMode.None;
        private readonly char s_CheckMark = '\u2714'; // unicode


        public string ConnectedTargetName => m_AttachProfilerState.connectionName;

        public bool ConnectedToEditor => m_AttachProfilerState.connectedToTarget == ConnectionTarget.Editor;

        [SerializeField]
        private bool m_ClearOnPlay;

        const string kProfilerRecentSaveLoadProfilePath = "ProfilerRecentSaveLoadProfilePath";
        const string kProfilerEnabledSessionKey = "ProfilerEnabled";

        internal delegate void SelectionChangedCallback(string selectedPropertyPath);
        public event SelectionChangedCallback selectionChanged = delegate {};

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
            return CreateProperty(HierarchyFrameDataView.columnDontSort);
        }

        public ProfilerProperty CreateProperty(int sortType)
        {
            int targetedFrame = GetActiveVisibleFrameIndex();
            if (targetedFrame < ProfilerDriver.lastFrameIndex - ProfilerDriver.maxHistoryLength)
            {
                return null;
            }

            var property = new ProfilerProperty();
            property.SetRoot(targetedFrame, sortType, (int)m_ViewType);
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
            m_ProfilerWindows.Add(this);
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged += Initialize;
            ProfilerUserSettings.settingsChanged += OnSettingsChanged;
        }

        void InitializeIfNeeded()
        {
            if (m_Initialized)
                return;

            Initialize();
        }

        void Initialize()
        {
            // When reinitializing (e.g. because Colorblind mode or PlatformModule changed) we don't need a new state
            if (m_AttachProfilerState == null)
                m_AttachProfilerState = ConnectionUtility.GetAttachToPlayerState(this, (player) => ClearFramesCallback());

            int historySize = ProfilerUserSettings.frameCount;

            m_Charts = new ProfilerChart[Profiler.areaCount];

            Color[] chartAreaColors = ProfilerColors.chartAreaColors;

            for (int i = 0; i < Profiler.areaCount; i++)
            {
                float scale = 1.0f;
                Chart.ChartType chartType = Chart.ChartType.Line;
                string[] statisticsNames = ProfilerDriver.GetGraphStatisticsPropertiesForArea((ProfilerArea)i);
                int length = statisticsNames.Length;
                if (Array.IndexOf(ms_StackedAreas, (ProfilerArea)i) != -1)
                {
                    chartType = Chart.ChartType.StackedFill;
                    scale = 1.0f / 1000.0f;
                }

                ProfilerChart chart = CreateProfilerChart((ProfilerArea)i, chartType, scale, length);

                if (chart.m_Area == ProfilerArea.CPU)
                {
                    chart.SetOnSeriesToggleCallback(OnToggleCPUChartSeries);
                }

                for (int s = 0; s < length; s++)
                {
                    chart.m_Series[s] = new ChartSeriesViewData(statisticsNames[s], historySize, chartAreaColors[s % chartAreaColors.Length]);
                    for (int frameIdx = 0; frameIdx < historySize; ++frameIdx)
                        chart.m_Series[s].xValues[frameIdx] = (float)frameIdx;
                }

                m_Charts[(int)i] = chart;
            }

            if (m_VertSplit == null || m_VertSplit.relativeSizes == null || m_VertSplit.relativeSizes.Length == 0)
                m_VertSplit = new SplitterState(new[] { 50f, 50f }, new[] { k_VertSplitterMinSizes, k_VertSplitterMinSizes }, null);
            // 2 times the min splitter size plus one line height for the toolbar up top
            minSize = new Vector2(Chart.kSideWidth + k_RightPaneMinSize, k_VertSplitterMinSizes * m_VertSplit.minSizes.Length + k_LineHeight);

            // TODO: only create modules for active charts and otherwise lazy initialize them.
            if (m_ProfilerModules == null)
            {
                m_ProfilerModules = new ProfilerModuleBase[]
                {
                    new CPUProfilerModule(), //CPU
                    new GPUProfilerModule(), //GPU
                    new RenderingProfilerModule(), //Rendering
                    new MemoryProfilerModule(), //Memory
                    new AudioProfilerModule(), //Audio
                    new VideoProfilerModule(), //Video
                    new PhysicsProfilerModule(), //Physics
                    new Physics2DProfilerModule(), //Physics2D
                    new NetworkingMessagesProfilerModule(), //NetworkMessages
                    new NetworkingOperationsProfilerModule(), //NetworkOperations
                    new UIProfilerModule(), //UI
                    new UIDetailsProfilerModule(), //UIDetails
                    new GlobalIlluminationProfilerModule(), //GlobalIllumination
                };
            }

            foreach (var module in m_ProfilerModules)
            {
                module?.OnEnable(this);
            }

            UpdateCharts();
            foreach (var chart in m_Charts)
                chart.LoadAndBindSettings();

            m_Initialized = true;
        }

        void OnSettingsChanged()
        {
            Initialize();
        }

        void OnToggleCPUChartSeries(bool wasToggled)
        {
            if (wasToggled)
            {
                int historyLength = ProfilerDriver.maxHistoryLength - 1;
                int firstEmptyFrame = ProfilerDriver.lastFrameIndex - historyLength;
                int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);

                ComputeChartScaleValue(ProfilerArea.CPU, historyLength, firstEmptyFrame, firstFrame);
            }
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
            ProfilerChart profilerChart = (ProfilerChart)sender;
            profilerChart.active = false;
            m_ProfilerModules[(int)profilerChart.m_Area].OnDisable();
            m_ProfilerModules[(int)profilerChart.m_Area].OnClosed();
            m_CurrentArea = null;
            GUIUtility.ExitGUI();
        }

        void OnChartSelected(Chart sender)
        {
            ProfilerArea newArea = ((ProfilerChart)sender).m_Area;

            if (m_CurrentArea == newArea)
                return;
            var oldArea = m_CurrentArea;
            m_CurrentArea = newArea;

            // if switched out of CPU area, reset selected property
            if (m_CurrentArea != ProfilerArea.CPU)
            {
                ClearSelectedPropertyPath();
            }

            if (oldArea.HasValue)
            {
                m_ProfilerModules[(int)oldArea.Value].OnDisable();
            }

            m_ProfilerModules[(int)newArea].OnEnable(this);

            Repaint();
            GUIUtility.keyboardControl = 0;
            GUIUtility.ExitGUI();
        }

        void CheckForPlatformModuleChange()
        {
            if (m_ActiveNativePlatformSupportModule == null)
            {
                m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
                return;
            }


            if (m_ActiveNativePlatformSupportModule != EditorUtility.GetActiveNativePlatformSupportModuleName())
            {
                ProfilerDriver.ClearAllFrames();
                Initialize();
                m_ActiveNativePlatformSupportModule = EditorUtility.GetActiveNativePlatformSupportModuleName();
            }
        }

        void OnDisable()
        {
            m_AttachProfilerState.Dispose();
            m_AttachProfilerState = null;
            m_ProfilerWindows.Remove(this);
            foreach (var module in m_ProfilerModules)
            {
                module.OnDisable();
            }
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged -= Initialize;
            ProfilerUserSettings.settingsChanged -= OnSettingsChanged;
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

        // Used by Native method DoBuildPlayer_PostBuild() in BuildPlayer.cpp
        [MenuItem("Window/Analysis/Profiler %7", false, 0)]
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

        static bool CheckFrameData(ProfilerProperty property)
        {
            return property != null && property.frameDataReady;
        }

        public HierarchyFrameDataView GetFrameDataView(string threadName, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();
            var threadIndex = 0;
            using (var frameIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameIterator.GetThreadCount(frameIndex);
                for (var i = 0; i < threadCount; ++i)
                {
                    frameIterator.SetRoot(frameIndex, i);
                    var grp = frameIterator.GetGroupName();
                    var thrd = frameIterator.GetThreadName();
                    var name = string.IsNullOrEmpty(grp) ? thrd : grp + "." + thrd;
                    if (threadName == name)
                    {
                        threadIndex = i;
                        break;
                    }
                }
            }

            if (m_FrameDataView != null && m_FrameDataView.valid)
            {
                if (m_FrameDataView.frameIndex == frameIndex && m_FrameDataView.threadIndex == threadIndex && m_FrameDataView.viewMode == viewMode)
                    return m_FrameDataView;
            }

            if (m_FrameDataView != null)
                m_FrameDataView.Dispose();

            m_FrameDataView = new HierarchyFrameDataView(frameIndex, threadIndex, viewMode, profilerSortColumn, sortAscending);
            return m_FrameDataView;
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
                    int identifier = ProfilerDriver.GetStatisticsIdentifierForArea(cpuChart.m_Area, UnityString.Format("Selected{0}", chart.name));
                    float maxValue;
                    ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, 1.0f, cpuChart.m_Data.overlays[i].yValues, out maxValue);
                    cpuChart.m_Data.overlays[i].yScale = cpuChart.m_DataScale;
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
                    var series = chart.m_Data.unstackableSeriesIndex == j && chart.m_Data.hasOverlay ?
                        chart.m_Data.overlays[j] : chart.m_Series[j];

                    if (series.enabled)
                        timeNow += series.yValues[k];
                }
                if (timeNow > timeMax)
                    timeMax = timeNow;
                if (timeNow > timeMaxExcludeFirst && k + firstEmptyFrame >= firstFrame + 1)
                    timeMaxExcludeFirst = timeNow;
            }
            if (timeMaxExcludeFirst != 0.0f)
                timeMax = timeMaxExcludeFirst;

            timeMax = Math.Min(timeMax * chart.m_DataScale, m_ChartMaxClamp);

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
            int unstackableChartIndex = -1;
            var maxValues = new float[chart.m_Series.Length];
            for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
            {
                int identifier = ProfilerDriver.GetStatisticsIdentifierForArea(chart.m_Area, chart.m_Series[i].name);
                float maxValue;
                ProfilerDriver.GetStatisticsValues(identifier, firstEmptyFrame, 1.0f, chart.m_Series[i].yValues, out maxValue);
                chart.m_Series[i].yScale = chart.m_DataScale;
                maxValue *= chart.m_DataScale;

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
                    if (chart.m_Area == ProfilerArea.CPU)
                    {
                        if (chart.m_Series[i].name == k_CPUUnstackableSeriesName)
                        {
                            unstackableChartIndex = i;
                            break;
                        }
                    }
                }
            }
            if (chart.m_Area == ProfilerArea.NetworkMessages || chart.m_Area == ProfilerArea.NetworkOperations)
            {
                for (int i = 0, count = chart.m_Series.Length; i < count; ++i)
                    chart.m_Series[i].rangeAxis = new Vector2(0f, 0.9f * totalMaxValue);
                chart.m_Data.maxValue = totalMaxValue;
            }
            chart.m_Data.Assign(chart.m_Series, unstackableChartIndex, firstEmptyFrame, firstFrame);
            ProfilerDriver.GetStatisticsAvailable(chart.m_Area, firstEmptyFrame, chart.m_Data.dataAvailable);

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
#pragma warning disable CS0618
                    NetworkDetailStats.m_NetworkOperations.Clear();
#pragma warning restore
                }
            }

            // Opened a load pop-up, MacOS will redraw the window so bail out now
            EditorGUIUtility.ExitGUI();
        }

        private void DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Graph types
            Rect popupRect = GUILayoutUtility.GetRect(Styles.addArea, EditorStyles.toolbarDropDown, GUILayout.Width(Chart.kSideWidth - EditorStyles.toolbarDropDown.padding.left));
            if (EditorGUI.DropdownButton(popupRect, Styles.addArea, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                int length = m_Charts.Length;
                var names = new string[length];
                var enabled = new bool[length];
                for (int c = 0; c < length; ++c)
                {
                    names[c] = L10n.Tr(((ProfilerArea)c).ToString());
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

            EditorGUI.BeginDisabledGroup(m_AttachProfilerState.connectedToTarget != ConnectionTarget.Editor);

            // Deep profiling
            SetProfileDeepScripts(GUILayout.Toggle(ProfilerDriver.deepProfiling, Styles.deepProfile, EditorStyles.toolbarButton));

            // Profile Editor
            ProfilerDriver.profileEditor = GUILayout.Toggle(ProfilerDriver.profileEditor, Styles.profileEditor, EditorStyles.toolbarButton);

            EditorGUI.EndDisabledGroup();

            // Engine attach
            ConnectionGUILayout.AttachToPlayerDropdown(m_AttachProfilerState, EditorStyles.toolbarDropDown);

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
                    L10n.Tr("None"), L10n.Tr("Managed Allocations")
                };
                if (Unsupported.IsDeveloperMode())
                {
                    names = new string[]
                    {
                        L10n.Tr("None"), L10n.Tr("Managed Allocations"), L10n.Tr("All Allocations (fast)"), L10n.Tr("All Allocations (full)")
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
            m_ProfilerModules[(int)ProfilerArea.CPU].Clear();
            m_ProfilerModules[(int)ProfilerArea.GPU].Clear();

            ProfilerDriver.ClearAllFrames();
            m_LastFrameFromTick = -1;

#pragma warning disable CS0618
            NetworkDetailStats.m_NetworkOperations.Clear();
#pragma warning restore
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

            if (!m_CurrentArea.HasValue && Event.current.type == EventType.Repaint)
            {
                for (int i = 0; i < m_Charts.Length; i++)
                {
                    if (m_Charts[i].active)
                    {
                        m_Charts[i].ChartSelected();
                        break;
                    }
                }
            }
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
                var chart = m_Charts[c];
                if (!chart.active)
                    continue;

                newCurrentFrame = chart.DoChartGUI(newCurrentFrame, m_CurrentArea == chart.m_Area);
            }

            if (newCurrentFrame != m_CurrentFrame)
            {
                SetCurrentFrame(newCurrentFrame);
                Repaint();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.BeginVertical();
            if (m_CurrentArea.HasValue)
            {
                var detailViewPosition = new Rect(0, m_VertSplit.realSizes[0] + EditorGUI.kSingleLineHeight, position.width, m_VertSplit.realSizes[1]);

                var detailViewToolbar = detailViewPosition;
                detailViewToolbar.height = EditorStyles.toolbar.CalcHeight(GUIContent.none, 10.0f);
                m_ProfilerModules[(int)m_CurrentArea].DrawToolbar(detailViewPosition);

                detailViewPosition.yMin += detailViewToolbar.height;
                m_ProfilerModules[(int)m_CurrentArea].DrawView(detailViewPosition);
            }
            else
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
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
