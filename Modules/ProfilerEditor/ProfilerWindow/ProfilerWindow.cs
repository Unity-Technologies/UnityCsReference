// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Profiling.Editor.UI;
using UnityEditor.Accessibility;
using UnityEditor.MPE;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Profiling;
using UnityEditor.Profiling.Analytics;
using UnityEditor.Profiling.ModuleEditor;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Profiling;
using UnityEngine.Scripting;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using ViewController = Unity.Profiling.Editor.UI.ViewController;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Profiler", icon = "UnityEditor.ProfilerWindow")]
    public sealed class ProfilerWindow : EditorWindow, IHasCustomMenu, IProfilerWindowController, ProfilerModulesDropdownWindow.IResponder, BottlenecksChartViewController.IResponder
    {
        internal static class Styles
        {
            public static readonly GUIContent addArea = EditorGUIUtility.TrTextContent("Profiler Modules", "Add and remove profiler modules");
            public static readonly GUIContent deepProfile = EditorGUIUtility.TrTextContent("Deep Profile", "Instrument all scripting method calls to investigate scripts");
            public static readonly GUIContent deepProfileNotSupported = EditorGUIUtility.TrTextContent("Deep Profile", "Build a Player with Deep Profiling Support to be able to enable instrumentation of all scripting methods in a Player.");
            public static readonly GUIContent noActiveModules = EditorGUIUtility.TrTextContent("No Profiler Modules are active. Activate modules from the top left-hand drop-down.");

            public static readonly string enableDeepProfilingWarningDialogTitle = L10n.Tr("Enable deep script profiling");
            public static readonly string enableDeepProfilingWarningDialogContent = L10n.Tr("Enabling deep profiling requires reloading scripts.");
            public static readonly string disableDeepProfilingWarningDialogTitle = L10n.Tr("Disable deep script profiling");
            public static readonly string disableDeepProfilingWarningDialogContent = L10n.Tr("Disabling deep profiling requires reloading all scripts.");
            public static readonly string domainReloadWarningDialogButton = L10n.Tr("Reload");
            public static readonly string cancelDialogButton = L10n.Tr("Cancel");

            public static readonly GUIContent recordCallstacks = EditorGUIUtility.TrTextContent("Call Stacks", "Record call stacks for special samples such as \"GC.Alloc\". " +
                "To see the call stacks, select a sample in the CPU Usage module, e.g. in Timeline view. " +
                "To also see call stacks in Hierarchy view, switch from \"No Details\" to \"Related Data\", select a \"GC.Alloc\" sample and select \"N/A\" items from the list.");
            public static readonly string[] recordCallstacksOptions =
            {
                L10n.Tr("GC.Alloc"), L10n.Tr("UnsafeUtility.Malloc(Persistent)"), L10n.Tr("JobHandle.Complete")
            };
            public static readonly string[] recordCallstacksDevelopmentOptions =
            {
                L10n.Tr("GC.Alloc"), L10n.Tr("UnsafeUtility.Malloc(Persistent)"), L10n.Tr("JobHandle.Complete"), L10n.Tr("Native Allocations (Editor Only)")
            };
            public static readonly ProfilerMemoryRecordMode[] recordCallstacksEnumValues =
            {
                ProfilerMemoryRecordMode.GCAlloc, ProfilerMemoryRecordMode.UnsafeUtilityMalloc, ProfilerMemoryRecordMode.JobHandleComplete, ProfilerMemoryRecordMode.NativeAlloc
            };

            public static readonly GUIContent profilerRecordOff = EditorGUIUtility.TrIconContent("Record Off", "Record profiling information (F9)");
            public static readonly GUIContent profilerRecordOn = EditorGUIUtility.TrIconContent("Record On", "Record profiling information (F9)");

            public static SVC<Color> borderColor =
                new SVC<Color>("--theme-profiler-border-color-darker", Color.black);
            public static readonly GUIContent showHideCaptures = EditorGUIUtility.TrIconContent("LeftPanel", "Show/Hide Captures List");
            public static readonly GUIContent prevFrame = EditorGUIUtility.TrIconContent("Animation.PrevKey", "Previous frame");
            public static readonly GUIContent nextFrame = EditorGUIUtility.TrIconContent("Animation.NextKey", "Next frame");
            public static readonly GUIContent currentFrame = EditorGUIUtility.TrIconContent("Animation.LastKey", "Current frame");
            public static readonly GUIContent frame = EditorGUIUtility.TrTextContent("Frame: ", "Selected frame / Total number of frames");
            public static readonly GUIContent clearOnPlay = EditorGUIUtility.TrTextContent("Clear on Play", "Clear the captured data on entering Play Mode, or connecting to a new Player");
            public static readonly GUIContent clearData = EditorGUIUtility.TrTextContent("Clear", "Clear the captured data");
            public static readonly GUIContent saveProfilingData = EditorGUIUtility.TrIconContent("SaveAs", "Save current profiling information to a binary file");
            public static readonly GUIContent loadWindowTitle = EditorGUIUtility.TrTextContent("Load Window");
            public static readonly GUIContent loadProfilingData = EditorGUIUtility.TrIconContent("Import", "Load binary profiling information from a file. Shift click to append to the existing data");
            public static readonly string[] loadProfilingDataFileFilters = new string[] { L10n.Tr("Profiler files"), "data,raw", L10n.Tr("All files"), "*" };

            public static readonly GUIContent optionsButtonContent = EditorGUIUtility.TrIconContent("_Menu", "Additional Options");
            public static readonly GUIContent helpButtonContent = EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)");
            public static readonly GUIContent preferencesButtonContent = EditorGUIUtility.TrTextContent("Preferences", "Open User Preferences for the Profiler");

            public static readonly GUIContent accessibilityModeLabel = EditorGUIUtility.TrTextContent("Color Blind Mode", "Switch the color scheme to color blind safe colors");
            public static readonly GUIContent showStatsLabelsOnCurrentFrameLabel = EditorGUIUtility.TrTextContent("Show Stats for 'current frame'", "Show stats labels when the 'current frame' toggle is on.");

            public static readonly GUIStyle background = "OL box flat";
            public static readonly GUIStyle profilerGraphBackground = "ProfilerScrollviewBackground";

            public static readonly int kButtonWidth = 25;
            public static readonly GUILayoutOption buttonWidthOption = GUILayout.MaxWidth(kButtonWidth);
            public static readonly GUILayoutOption chartWidthOption = GUILayout.Width(Chart.kSideWidth - 1 - kButtonWidth);

            static Styles()
            {
                profilerGraphBackground.overflow.left = -(int)Chart.kSideWidth;
            }
        }

        static List<ProfilerWindow> s_ProfilerWindows = new List<ProfilerWindow>();

        const string k_UxmlResourceName = "ProfilerWindow.uxml";
        const string k_UssClass_Dark = "profiler-view--dark";
        const string k_UssClass_Light = "profiler-view--light";
        const string k_UxmlIdentifier_ToolbarViewContainer = "profiler-view__toolbar-view-container";
        const string k_UxmlIdentifier_BottlenecksViewContainer = "profiler-view__bottlenecks-view-container";
        const string k_UxmlIdentifier_CapturesListViewContainer = "profiler-view__captures-list-view-container";
        const string k_UxmlIdentifier_SplitView = "profiler-view__split-view";
        const string k_UxmlIdentifier_ChartsViewContainer = "profiler-view__charts-view-container";
        const string k_UxmlIdentifier_DetailsViewContainer = "profiler-view__details-view-container";
        const string k_MainSplitViewFixedPaneSizePreferenceKey = "ProfilerWindow.MainSplitView.FixedPaneSize";
        const string k_CapturesSplitViewFixedPaneSizePreferenceKey = "ProfilerWindow.CapturesSplitView.FixedPaneSize";
        const string k_CapturesSplitViewToggleIsVisibleStatePreferenceKey = "ProfilerWindow.CapturesSplitView.ToggleState";
        const int k_NoModuleSelected = -1;
        const string k_SelectedModuleIndexPreferenceKey = "ProfilerWindow.SelectedModuleIndex";
        const string k_DynamicModulesPreferenceKey = "ProfilerWindow.DynamicModules";
        const string k_FrameSelectionRangeStartKey = "ProfilerWindow.FrameSelectionRangeStart";
        const string k_FrameSelectionRangeEndKey = "ProfilerWindow.FrameSelectionRangeEnd";
        const string k_BottlenecksChartIsSelected = "ProfilerWindow.BottlenecksChartIsSelected";
        const int k_NoFrameSelectionSession = -99;

        static readonly Vector2 k_MinimumWindowSize = new Vector2(900f, 216f);
        // the minimum width required to draw all the buttons on the toolbar. This is used to truncate the active connection name.
        static readonly int k_MinimumToolbarContentWidth = 820;

        [NonSerialized]
        float m_FrameCountLabelMinWidth = 0;

        // For keeping correct "Recording" state on window maximizing
        [SerializeField]
        bool m_Recording;

        IConnectionStateInternal m_AttachProfilerState;

        Vector2 m_GraphPos = Vector2.zero;

        [SerializeField]
        string m_ActiveNativePlatformSupportModuleName;

        [NonSerialized]
        int m_SelectedModuleIndex = k_NoModuleSelected;

        int m_CurrentFrame = FrameDataView.invalidOrCurrentFrameIndex;
        int m_LastFrameFromTick = FrameDataView.invalidOrCurrentFrameIndex;

        bool m_CurrentFrameEnabled = false;

        [NonSerialized]
        bool m_IsInitialized = false;

        [NonSerialized]
        bool m_WasCreateGUICalled = false;

        [NonSerialized]
        bool m_LastGPUModuleActiveState = false;

        const int k_MainThreadIndex = 0;

        HierarchyFrameDataView m_FrameDataView;

        [Obsolete("cpuModuleName is deprecated. Use cpuModuleIdentifier instead. (UnityUpgradable) -> cpuModuleIdentifier")]
        public const string cpuModuleName = cpuModuleIdentifier;
        [Obsolete("gpuModuleName is deprecated. Use gpuModuleIdentifier instead. (UnityUpgradable) -> gpuModuleIdentifier")]
        public const string gpuModuleName = gpuModuleIdentifier;
        public const string cpuModuleIdentifier = CPUProfilerModule.k_Identifier;
        public const string gpuModuleIdentifier = GPUProfilerModule.k_Identifier;

        [SerializeReference] List<ProfilerModule> m_AllModules;

        internal IEnumerable<ProfilerModule> Modules => m_AllModules;
        // This is used by our performance tests to prevent the Profiler window from automatically repainting whilst profiling so we can control the repaints more precisely.
        internal bool IgnoreRepaintAllProfilerWindowsTick { get; set; }

        internal ProfilerCategoryActivator m_CategoryActivator;

        ProfilerMemoryRecordMode m_CurrentCallstackRecordMode = ProfilerMemoryRecordMode.None;
        [SerializeField]
        ProfilerMemoryRecordMode m_CallstackRecordMode = ProfilerMemoryRecordMode.None;

        internal string ConnectedTargetName => m_AttachProfilerState.connectionName;
        internal bool ConnectedToEditor => m_AttachProfilerState.connectedToTarget == ConnectionTarget.Editor;
        internal TwoPaneSplitView MainSplitView { get; private set; }

        [SerializeField]
        bool m_ClearOnPlay;

        // Data
        IProfilerCaptureDataService m_DataService;
        IProfilerPersistentSettingsService m_PersistentSettingsService;
        bool? m_IsLegacyChartsGUIScrollBarVisible = false;

        // UI references.
        IMGUIContainer m_ToolbarIMGUIContainer;
        IMGUIContainer m_ChartsIMGUIContainer;
        VisualElement m_BottlenecksViewContainer;
        VisualElement m_DetailsViewContainer;

        BottlenecksChartViewController m_BottlenecksChartViewController;
        ViewController m_BottlenecksDetailsViewController;

        internal VisualElement DetailsViewContainer => m_DetailsViewContainer;

        // Assistant controller
        CpuProfilerAssistantController m_CpuProfilerAssistantController;

        // Captures list
        CapturesListViewController m_CapturesListViewController;
        VisualElement m_CapturesListViewContainer;
        TwoPaneSplitView m_CapturesListSplitView;
        CaptureDataService m_CaptureDataService;
        ScreenshotsManager m_ScreenshotsManager;
        string m_CurrentLoadedCaptureFile;
        internal string CurrentLoadedCaptureFile
        {
            get => m_CurrentLoadedCaptureFile;
            private set
            {
                if (m_CurrentLoadedCaptureFile == value)
                    return;

                m_CurrentLoadedCaptureFile = value;
                if (string.IsNullOrEmpty(m_CurrentLoadedCaptureFile))
                {
                    // We already mark this when a capture is loaded, so
                    // only do it here if one is being cleared/changed.
                    m_CaptureDataService.LoadedCapturesHaveChanged();
                }
            }
        }

        const string kProfilerRecentSaveLoadProfilePath = "ProfilerRecentSaveLoadProfilePath";
        const string kProfilerEnabledSessionKey = "ProfilerEnabled";
        const string kProfilerEditorTargetModeEnabledSessionKey = "ProfilerTargetMode";
        const string kProfilerDeepProfilingWarningSessionKey = "ProfilerDeepProfilingWarning";

        internal event Action<int, bool> currentFrameChanged = delegate {};
        internal event Action frameDataViewAboutToBeDisposed = delegate {};
        public event Action<long> SelectedFrameIndexChanged;

        internal event Action<bool> recordingStateChanged = delegate {};
        internal event Action<bool> deepProfileChanged = delegate {};
        internal event Action<ProfilerMemoryRecordMode> memoryRecordingModeChanged = delegate {};

        [Obsolete("selectedModuleName is deprecated. Use selectedModuleIdentifier instead. (UnityUpgradable) -> selectedModuleIdentifier")]
        public string selectedModuleName => selectedModuleIdentifier;

        public string selectedModuleIdentifier => selectedModule?.Identifier ?? null;

        const string kJobsProfilerIdentifier = "JobsProfilerModule, Unity.JobsProfiler.Editor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
        internal ProfilerModule jobsProfilerModule { get; private set; }

        internal ProfilerModule selectedModule
        {
            get
            {
                return ModuleAtIndex(m_SelectedModuleIndex);
            }
            set
            {
                // As far as I can tell, the only time this check is required is when a new frame is selected in a chart view that is already selected. We can mitigate that when we restructure charts for UIToolkit.
                if (selectedModule == value)
                    return;

                SelectModule(value);
            }
        }

        // IProfilerWindow Interface implementation
        public long selectedFrameIndex
        {
            get => GetActiveVisibleFrameIndex();
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value < 0 for the {nameof(selectedFrameIndex)}.");
                if (value < firstAvailableFrameIndex)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value smaller than {nameof(firstAvailableFrameIndex)} which is currently {firstAvailableFrameIndex}.");
                if (value > lastAvailableFrameIndex)
                    throw new ArgumentOutOfRangeException("value", $"Can't set a value greater than {nameof(lastAvailableFrameIndex)} which is currently {lastAvailableFrameIndex}.");
                SetActiveVisibleFrameIndex((int)value);
            }
        }

        static bool ProfilerHasAnyFrames()
        {
            return ProfilerDriver.lastFrameIndex != FrameDataView.invalidOrCurrentFrameIndex;
        }

        // At the time of writing, this is only used by the Highlights module to support range
        // selection. Over time, users of selectedFrameIndex can be migrated to support a
        // range selection before updating the public API.
        private Range? m_SelectedFrameRange;

        internal Range? SelectedFrameRange
        {
            get
            {
                if (m_SelectedFrameRange == null && ProfilerHasAnyFrames())
                {
                    // SessionState doesn't appear to have a "HasKey", so try getting with an invalid default.
                    var rangeStart = SessionState.GetInt(k_FrameSelectionRangeStartKey, k_NoFrameSelectionSession);
                    var rangeEnd = SessionState.GetInt(k_FrameSelectionRangeEndKey, k_NoFrameSelectionSession);

                    if (rangeStart != k_NoFrameSelectionSession)
                        m_SelectedFrameRange = new Range(rangeStart, rangeEnd);
                }

                return m_SelectedFrameRange;
            }
            set => m_SelectedFrameRange = value;
        }

        // these properties act as a redirect to ProfilerDriver for now.
        // Once the Profiler Window isn't so tightly coupled to the ProfilerDriver singleton anymore, they will relate just the data stream displayed in this instance.
        public long firstAvailableFrameIndex => ProfilerDriver.firstFrameIndex;
        public long lastAvailableFrameIndex => ProfilerDriver.lastFrameIndex;

        internal ProfilerProperty CreateProperty()
        {
            return CreateProperty(HierarchyFrameDataView.columnDontSort);
        }

        internal ProfilerProperty CreateProperty(int sortType)
        {
            var targetedFrame = GetActiveVisibleFrameIndex();
            if (targetedFrame < 0)
                targetedFrame = ProfilerDriver.lastFrameIndex;
            if (targetedFrame < Math.Max(0, ProfilerDriver.firstFrameIndex))
            {
                return null;
            }

            var property = new ProfilerProperty();
            property.SetRoot(targetedFrame, sortType, (int)ProfilerViewType.Hierarchy);
            property.onlyShowGPUSamples = (selectedModule is GPUProfilerModule);
            return property;
        }

        internal int GetActiveVisibleFrameIndex()
        {
            // Update the current frame only at fixed intervals,
            // otherwise it looks weird when it is rapidly jumping around when we have a lot of repaints
            return m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex ? m_LastFrameFromTick : m_CurrentFrame;
        }

        internal bool ProfilerWindowOverheadIsAffectingProfilingRecordingData()
        {
            return ProcessService.level == ProcessLevel.Main && IsSetToRecord() && ProfilerDriver.IsConnectionEditor() && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || ProfilerDriver.profileEditor);
        }

        internal bool IsRecording()
        {
            return IsSetToRecord() && ((EditorApplication.isPlaying && !EditorApplication.isPaused) || ProfilerDriver.profileEditor || !ProfilerDriver.IsConnectionEditor());
        }

        internal bool IsSetToRecord()
        {
            return m_Recording;
        }

        public IProfilerFrameTimeViewSampleSelectionController GetFrameTimeViewSampleSelectionController(string moduleIdentifier)
        {
            switch (moduleIdentifier)
            {
                case cpuModuleIdentifier:
                case "CPU Usage": // Support for deprecated API constant ProfilerWindow.cpuModuleName.
                    var cpuModule = this.GetProfilerModuleByType<CPUProfilerModule>();
                    return cpuModule;
                case gpuModuleIdentifier:
                case "GPU Usage": // Support for deprecated API constant ProfilerWindow.gpuModuleName.
                    var gpuModule = this.GetProfilerModuleByType<GPUProfilerModule>();
                    return gpuModule;
                default:
                    throw new ArgumentException($"\"{moduleIdentifier}\" is not a valid module identifier for a module implementing IProfilerFrameTimeViewSampleSelectionController. Try \"{nameof(ProfilerWindow.cpuModuleIdentifier)}\" or \"{nameof(ProfilerWindow.gpuModuleIdentifier)}\" instead.", $"{nameof(moduleIdentifier)}");
            }
        }

        // TODO: Remove this once Profile Analizer version 1.0.5 is verified.
        // Used by Profiler Analyzer via reflection.
        internal T GetProfilerModule<T>(ProfilerArea area) where T : ProfilerModuleBase
        {
            foreach (var module in m_AllModules)
            {
                if (module.area == area)
                {
                    return module as T;
                }
            }

            return null;
        }

        // Used by Tests/ProfilerEditorTests/ProfilerModulePreferenceKeyTests.
        internal ProfilerModule GetProfilerModuleByType(Type type)
        {
            ProfilerModule fittingModule = null;
            if (type.IsAbstract)
                throw new ArgumentException($"{nameof(type)} can't be abstract.", $"{nameof(type)}");

            foreach (var module in m_AllModules)
            {
                if (type == module.GetType())
                {
                    fittingModule = module;
                }
            }
            if (fittingModule == null)
                throw new ArgumentException($"A {nameof(type)} of {type.Name} is not a type that describes an existing Profiler Module.", $"{nameof(type)}");
            return fittingModule;
        }

        internal void SetProfilerModuleActiveState(ProfilerModule module, bool active)
        {
            if (module == null)
                throw new ArgumentNullException($"{nameof(module)}");
            var moduleIndex = IndexOfModule(module);
            if (moduleIndex == k_NoModuleSelected)
                throw new ArgumentException($"The {module.DisplayName} module is not registered with the Profiler Window.", $"{nameof(module)}");
            m_AllModules[moduleIndex].active = active;
        }

        internal bool GetProfilerModuleActiveState(ProfilerModule module)
        {
            if (module == null)
                throw new ArgumentNullException($"{nameof(module)}");
            var moduleIndex = IndexOfModule(module);
            if (moduleIndex == k_NoModuleSelected)
                throw new ArgumentException($"The {module.DisplayName} module is not registered with the Profiler Window.", $"{nameof(module)}");
            return m_AllModules[moduleIndex].active;
        }

        void OnEnable()
        {
            Initialize();
            m_DataService = new LegacySingletonProfilerCaptureDataService();
            m_PersistentSettingsService = new LegacyGlobalProfilerPersistentSettingsService();
            m_CpuProfilerAssistantController = new CpuProfilerAssistantController();
            ConstructVisualTree(m_DataService, m_PersistentSettingsService);
            SubscribeToGlobalEvents();
        }

        void CreateGUI()
        {
            m_WasCreateGUICalled = true;

            // If there is already an open instance of the Module Editor window, resubscribe to the onChangesConfirmed event.
            if (ModuleEditorWindow.TryGetOpenInstance(out var moduleEditorWindow))
            {
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }

            foreach (var module in m_AllModules)
            {
                module.OnEnable();
            }

            // Select the last selected module this session. If there wasn't one, try to select the first active module.
            var moduleIndexToSelect = SessionState.GetInt(k_SelectedModuleIndexPreferenceKey, k_NoModuleSelected);
            if (moduleIndexToSelect != k_NoModuleSelected)
                SelectModuleAtIndex(moduleIndexToSelect);
            else
            {
                // If we were looking at the highlights/bottlenecks view and had a domain reload, refocus it
                if (SessionState.GetBool(k_BottlenecksChartIsSelected, false) && ProfilerHasAnyFrames())
                {
                    ((BottlenecksChartViewController.IResponder)this).ChartViewSelectedFrameRange(SelectedFrameRange);
                }
                else
                    SelectFirstActiveModule();
            }
        }

        void OnDisable()
        {
            if (!m_IsInitialized) return;

            SaveViewSettings();
            m_AttachProfilerState.Dispose();
            m_AttachProfilerState = null;
            s_ProfilerWindows.Remove(this);

            if (m_WasCreateGUICalled)
            {
                DeselectSelectedModuleIfNecessary();
                foreach (var module in m_AllModules)
                {
                    module.OnDisable();
                }
            }

            m_BottlenecksChartViewController.Dispose();
            m_BottlenecksChartViewController = null;
            m_CapturesListViewController.Dispose();
            m_CapturesListViewController = null;
            m_CaptureDataService.Dispose();
            m_CaptureDataService = null;
            m_ScreenshotsManager.Dispose();
            m_ScreenshotsManager = null;
            m_PersistentSettingsService.Dispose();
            m_PersistentSettingsService = null;
            m_DataService.Dispose();
            m_DataService = null;
            m_CpuProfilerAssistantController.Dispose();
            m_CpuProfilerAssistantController = null;

            UnsubscribeFromGlobalEvents();

            m_IsInitialized = false;
            m_WasCreateGUICalled = false;
        }

        void Initialize()
        {
            minSize = k_MinimumWindowSize;
            titleContent = GetLocalizedTitleContent();
            s_ProfilerWindows.Add(this); // TODO Remove until we have a need for this.

            var existingModules = m_AllModules;
            m_AllModules = InitializeAllModules(existingModules);

            m_AttachProfilerState = PlayerConnectionGUIUtility.GetConnectionState(this, OnTargetedEditorConnectionChanged, IsEditorConnectionTargeted, OnConnectedToPlayer) as IConnectionStateInternal;
            m_CategoryActivator = new ProfilerCategoryActivator();

            m_ActiveNativePlatformSupportModuleName = EditorUtility.GetActiveNativePlatformSupportModuleName();
            m_IsInitialized = true;
        }

        List<ProfilerModule> InitializeAllModules(List<ProfilerModule> existingModules)
        {
            var modules = new List<ProfilerModule>();
            InitializeAllCompileTimeDefinedProfilerModulesIntoCollection(ref modules, existingModules);
            InitializeAllDynamicProfilerModulesIntoCollection(ref modules, existingModules);
            SortModuleCollectionInPlace(ref modules);

            return modules;
        }

        void InitializeAllCompileTimeDefinedProfilerModulesIntoCollection(ref List<ProfilerModule> modules, List<ProfilerModule> existingModules)
        {
            // Find all defined Profiler module types.
            var moduleTypes = TypeCache.GetTypesDerivedFrom<ProfilerModule>();
            foreach (var moduleType in moduleTypes)
            {
                if (!ProfilerModuleTypeValidator.IsValidModuleTypeDefinition(moduleType, out ProfilerModuleMetadataAttribute moduleMetadata, out string errorDescription))
                {
                    if (!string.IsNullOrEmpty(errorDescription))
                        Debug.LogError(errorDescription);

                    continue;
                }

                // Initialize the module. Instantiate if necessary.
                var moduleIdentifier = moduleType.AssemblyQualifiedName;
                var moduleExists = TryGetModuleInCollection(moduleIdentifier, existingModules, out ProfilerModule module);
                try
                {
                    if (!moduleExists)
                    {
                        module = Activator.CreateInstance(moduleType) as ProfilerModule;
                    }

                    var args = new ProfilerModule.InitializationArgs(moduleIdentifier, moduleMetadata.DisplayName, moduleMetadata.IconPath, this);
                    module.Initialize(args);

                    if (moduleIdentifier == kJobsProfilerIdentifier)
                        jobsProfilerModule = module;
                    else
                        modules.Add(module);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to create Profiler module of type {moduleType}. {e.Message}");
                    continue;
                }
            }
        }

        void InitializeAllDynamicProfilerModulesIntoCollection(ref List<ProfilerModule> modules, List<ProfilerModule> existingModules)
        {
            var json = EditorPrefs.GetString(k_DynamicModulesPreferenceKey);
            var serializedDynamicModules = JsonUtility.FromJson<DynamicProfilerModule.SerializedDataCollection>(json);
            if (serializedDynamicModules != null)
            {
                for (int i = 0; i < serializedDynamicModules.Length; i++)
                {
                    var moduleData = serializedDynamicModules[i];

                    // Initialize the module. Instantiate if necessary.
                    var moduleIdentifier = moduleData.m_Name; // Dynamic modules use their name as their identifier for legacy reasons.
                    var moduleExists = TryGetModuleInCollection(moduleIdentifier, existingModules, out DynamicProfilerModule module);
                    if (!moduleExists)
                    {
                        module = new DynamicProfilerModule();
                    }

                    var args = new ProfilerModule.InitializationArgs(moduleIdentifier, moduleData.m_Name, DynamicProfilerModule.iconPath, this);
                    module.Initialize(args, moduleData.m_ChartCounters, moduleData.m_DetailCounters);
                    modules.Add(module);
                }
            }
        }

        bool TryGetModuleInCollection<T>(string identifier, IEnumerable<ProfilerModule> collection, out T module) where T : ProfilerModule
        {
            module = null;

            if (collection == null)
                return false;

            foreach (var mod in collection)
            {
                // Collection can contain null modules, for example when a type has been removed.
                if (mod != null)
                {
                    if (identifier.Equals(mod.Identifier))
                    {
                        module = mod as T;
                        return (module != null);
                    }
                }
            }

            return false;
        }

        void SortModuleCollectionInPlace(ref List<ProfilerModule> modules)
        {
            modules.Sort((a, b) =>
            {
                // Sort by order.
                var result = a.orderIndex.CompareTo(b.orderIndex);
                if (result == 0)
                {
                    // Secondary sort by name.
                    result = a.DisplayName.CompareTo(b.DisplayName);
                }

                return result;
            });

            // Commit this sorted order index to any modules with an undefined order index (e.g. they specified no default).
            for (int i = 0; i < modules.Count; ++i)
            {
                var module = modules[i];
                var orderIndex = module.orderIndex;
                if (orderIndex == ProfilerModule.k_UndefinedOrderIndex)
                {
                    module.orderIndex = i;
                }
            }
        }

        void ConstructVisualTree(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService persistentSettingsService)
        {
            var template = EditorGUIUtility.Load(k_UxmlResourceName) as VisualTreeAsset;
            template.CloneTree(rootVisualElement);

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            rootVisualElement.AddToClassList(themeUssClass);

            m_ToolbarIMGUIContainer = rootVisualElement.Q<IMGUIContainer>(k_UxmlIdentifier_ToolbarViewContainer);
            m_ToolbarIMGUIContainer.onGUIHandler = DoLegacyToolbarGUI;

            m_BottlenecksChartViewController = new BottlenecksChartViewController(dataService, persistentSettingsService, this, this, rootVisualElement);
            m_BottlenecksViewContainer = rootVisualElement.Q<VisualElement>(k_UxmlIdentifier_BottlenecksViewContainer);
            m_BottlenecksViewContainer.Add(m_BottlenecksChartViewController.View);
            var bottleneckViewVisible = persistentSettingsService.IsBottleneckViewVisible;
            SetBottleneckViewVisible(bottleneckViewVisible);

            m_CaptureDataService = new CaptureDataService(this);
            m_ScreenshotsManager = new ScreenshotsManager();
            m_CapturesListViewController = new CapturesListViewController(this, m_CaptureDataService, m_ScreenshotsManager);
            m_CapturesListViewContainer = rootVisualElement.Q<VisualElement>(k_UxmlIdentifier_CapturesListViewContainer);
            m_CapturesListSplitView = (TwoPaneSplitView)m_CapturesListViewContainer.parent;
            // TwoPaneSplitView.viewDataKey is not currently supported so we need to manually persist its state.
            var capturePaneSize = EditorPrefs.GetFloat(k_CapturesSplitViewFixedPaneSizePreferenceKey, 270);
            m_CapturesListSplitView.fixedPaneInitialDimension = capturePaneSize;
            m_CapturesListViewContainer.Add(m_CapturesListViewController.View);
            ShowCapturesList(EditorPrefs.GetBool(k_CapturesSplitViewToggleIsVisibleStatePreferenceKey, true));

            m_ChartsIMGUIContainer = rootVisualElement.Q<IMGUIContainer>(k_UxmlIdentifier_ChartsViewContainer);
            m_ChartsIMGUIContainer.onGUIHandler = DoLegacyChartsGUI;

            MainSplitView = rootVisualElement.Q<TwoPaneSplitView>(k_UxmlIdentifier_SplitView);
            // TwoPaneSplitView.viewDataKey is not currently supported so we need to manually persist its state.
            var fixedPaneSize = EditorPrefs.GetFloat(k_MainSplitViewFixedPaneSizePreferenceKey, k_MinimumWindowSize.y * 0.5f);
            MainSplitView.fixedPaneInitialDimension = fixedPaneSize;

            m_DetailsViewContainer = rootVisualElement.Q<VisualElement>(k_UxmlIdentifier_DetailsViewContainer);
        }

        void OnProfilerFrameRecorded(int _, int __)
        {
            CurrentLoadedCaptureFile = string.Empty;
        }

        void SubscribeToGlobalEvents()
        {
            // maximize playmode will call ondisable which will unsubscribe for clear events.
            // we unsubscribe here to make sure that we dont end up with multiple
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged += OnSettingsChanged;
            ProfilerUserSettings.settingsChanged += OnSettingsChanged;
            ProfilerDriver.profileLoaded += OnProfileLoaded;
            ProfilerDriver.profileCleared += OnProfileCleared;
            ProfilerDriver.profilerCaptureSaved += ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerCaptureLoaded += ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerConnected += ProfilerWindowAnalytics.SendConnectionEvent;
            ProfilerDriver.profilerCaptureStarted += ProfilerWindowAnalytics.StartCapture;
            ProfilerDriver.NewProfilerFrameRecorded += OnProfilerFrameRecorded;
        }

        void UnsubscribeFromGlobalEvents()
        {
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            UserAccessiblitySettings.colorBlindConditionChanged -= OnSettingsChanged;
            ProfilerUserSettings.settingsChanged -= OnSettingsChanged;
            ProfilerDriver.profileLoaded -= OnProfileLoaded;
            ProfilerDriver.profileCleared -= OnProfileCleared;
            ProfilerDriver.profilerCaptureSaved -= ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerCaptureLoaded -= ProfilerWindowAnalytics.SendSaveLoadEvent;
            ProfilerDriver.profilerConnected -= ProfilerWindowAnalytics.SendConnectionEvent;
            ProfilerDriver.profilerCaptureStarted -= ProfilerWindowAnalytics.StartCapture;
            ProfilerDriver.NewProfilerFrameRecorded -= OnProfilerFrameRecorded;
        }

        void OnSettingsChanged()
        {
            // Don't apply changed settings when we could be outside of a GUI call (e.g. when a setting was changed from closing a context menu, like Color Blind mode).
            // Event.current seems to be null even in a scheduled callback so, always schedule this for later.
            rootVisualElement.schedule.Execute(ApplySettingsChange).StartingIn(0);
            Repaint();
        }

        void ApplySettingsChange()
        {
            SaveViewSettings();

            foreach (var module in m_AllModules)
            {
                module.Rebuild();
            }
            Repaint();
        }

        void Clear()
        {
            // Clear All Frames calls ProfilerDriver.profileCleared which in turn calls OnProfileCleared
            ProfilerDriver.ClearAllFrames();
        }

        void OnProfileCleared()
        {
            ResetForClearedOrLoaded(true);
        }

        void OnProfileLoaded()
        {
            ResetForClearedOrLoaded(false);
        }

        void ResetForClearedOrLoaded(bool cleared)
        {
            // Reset frame state
            m_LastFrameFromTick = FrameDataView.invalidOrCurrentFrameIndex;
            m_FrameCountLabelMinWidth = 0;
            foreach (var module in m_AllModules)
            {
                module.Clear();
            }
            // Reset the cached data view
            if (m_FrameDataView != null)
                DisposeFrameDataView();
            m_FrameDataView = null;

            if (cleared)
            {
                SetCurrentFrameDontPause(FrameDataView.invalidOrCurrentFrameIndex);
                m_CurrentFrameEnabled = true;
                CurrentLoadedCaptureFile = string.Empty;
                m_ScreenshotsManager.ResetTemporaryScreenshot();
            }

            foreach (var module in m_AllModules)
            {
                module.Clear();
                module.Update();
            }

            RepaintImmediately();
        }

        internal ProfilerModule[] GetProfilerModules()
        {
            var copy = new ProfilerModule[m_AllModules.Count];
            m_AllModules.CopyTo(copy);
            return copy;
        }

        internal void GetProfilerModules(ref List<ProfilerModule> outModules)
        {
            if (outModules == null)
            {
                outModules = new List<ProfilerModule>(m_AllModules);
                return;
            }
            outModules.Clear();
            outModules.AddRange(m_AllModules);
        }

        internal void CloseModule(ProfilerModule module)
        {
            if (module == selectedModule)
            {
                SelectFirstActiveModule();
            }
        }

        void CheckForPlatformModuleChange()
        {
            var activeNativePlatformSupportModuleName = EditorUtility.GetActiveNativePlatformSupportModuleName();
            if (m_ActiveNativePlatformSupportModuleName != activeNativePlatformSupportModuleName)
            {
                OnActiveNativePlatformSupportModuleChanged(activeNativePlatformSupportModuleName);
            }
        }

        void OnActiveNativePlatformSupportModuleChanged(string activeNativePlatformSupportModuleName)
        {
            ProfilerDriver.ClearAllFrames();
            m_ActiveNativePlatformSupportModuleName = activeNativePlatformSupportModuleName;

            foreach (var module in m_AllModules)
            {
                module.OnNativePlatformSupportModuleChanged();
            }
            Repaint();
        }

        void SaveViewSettings()
        {
            foreach (var module in m_AllModules)
            {
                module.SaveViewSettings();
            }

            if (MainSplitView.fixedPane is { resolvedStyle: not null } && !float.IsNaN(MainSplitView.fixedPane.resolvedStyle.height))
                EditorPrefs.SetFloat(k_MainSplitViewFixedPaneSizePreferenceKey, MainSplitView.fixedPane.resolvedStyle.height);

            if (m_CapturesListSplitView is { resolvedStyle: not null } && !float.IsNaN(m_CapturesListSplitView.resolvedStyle.width) &&
                m_CapturesListSplitView.fixedPane.style.display != DisplayStyle.None)
                EditorPrefs.SetFloat(k_CapturesSplitViewFixedPaneSizePreferenceKey, m_CapturesListSplitView.fixedPane.resolvedStyle.width);

            SessionState.SetInt(k_SelectedModuleIndexPreferenceKey, m_SelectedModuleIndex);

            // Don't go via accessor, since we save view on shutdown - at which point ProfilerDriver is
            // no longer around, and the accessor makes use of that with ProfilerHasAnyFrames.
            SessionState.SetInt(k_FrameSelectionRangeStartKey, m_SelectedFrameRange == null ? k_NoFrameSelectionSession : m_SelectedFrameRange.Value.Start.Value);
            SessionState.SetInt(k_FrameSelectionRangeEndKey, m_SelectedFrameRange == null ? k_NoFrameSelectionSession : m_SelectedFrameRange.Value.End.Value);

            SessionState.SetBool(k_BottlenecksChartIsSelected, IsBottleneckViewVisible());
        }

        void Awake()
        {
            if (!Profiler.supported)
                return;

            // Track enabled state
            if (ProfilerUserSettings.rememberLastRecordState)
                m_Recording = EditorPrefs.GetBool(kProfilerEnabledSessionKey, ProfilerUserSettings.defaultRecordState);
            else
                m_Recording = SessionState.GetBool(kProfilerEnabledSessionKey, ProfilerUserSettings.defaultRecordState);

            // This event gets called every time when some other window is maximized and then unmaximized
            ProfilerDriver.enabled = m_Recording;

            ProfilerDriver.profileEditor = SessionState.GetBool(kProfilerEditorTargetModeEnabledSessionKey,
                ProfilerUserSettings.defaultTargetMode == ProfilerEditorTargetMode.Editmode || ProfilerDriver.profileEditor);

            // Update the current callstack capture mode.
            m_CurrentCallstackRecordMode = ProfilerDriver.memoryRecordMode;
            ProfilerWindowAnalytics.OnProfilerWindowAwake();
        }

        void OnPlaymodeStateChanged(PlayModeStateChange stateChange)
        {
            m_CurrentFrameEnabled = false;
            if (stateChange == PlayModeStateChange.EnteredPlayMode)
            {
                ClearFramesOnPlayOrPlayerConnectionChange();
            }
        }

        void OnPauseStateChanged(PauseState stateChange)
        {
            m_CurrentFrameEnabled = false;
        }

        internal void ClearFramesOnPlayOrPlayerConnectionChange()
        {
            if (m_ClearOnPlay)
                Clear();
        }

        void OnDestroy()
        {
            // We're being temporary "hidden" on maximize, do nothing
            if (WindowLayout.GetMaximizedWindow() != null)
                return;

            // When window is destroyed, we disable profiling
            if (Profiler.supported)
                ProfilerDriver.enabled = false;

            // Report window and session shutdown
            ProfilerWindowAnalytics.OnProfilerWindowDestroy();
        }

        void OnFocus()
        {
            // set the real state of profiler. OnDestroy is called not only when window is destroyed, but also when maximized state is changed
            if (Profiler.supported)
            {
                ProfilerDriver.enabled = m_Recording;
            }
        }

        void OnLostFocus()
        {
            if (GUIUtility.hotControl != 0)
            {
                // The chart may not have had the chance to release the hot control before we lost focus.
                // This happens when changing the selected frame, which may pause the game and switch the focus to another view.
                for (int i = 0; i < m_AllModules.Count; ++i)
                {
                    var module = m_AllModules[i];
                    module.OnLostFocus();
                }
            }
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(Styles.accessibilityModeLabel, UserAccessiblitySettings.colorBlindCondition != ColorBlindCondition.Default, OnToggleColorBlindMode);
        }

        void OnToggleColorBlindMode()
        {
            UserAccessiblitySettings.colorBlindCondition = UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default
                ? ColorBlindCondition.Deuteranopia
                : ColorBlindCondition.Default;
        }

        void OnToggleShowStatsLabelsOnCurrentFrame()
        {
            ProfilerUserSettings.showStatsLabelsOnCurrentFrame = !ProfilerUserSettings.showStatsLabelsOnCurrentFrame;
        }

        // Used by Native method DoBuildPlayer_PostBuild() in BuildPlayer.cpp
        [MenuItem("Window/Analysis/Profiler %7", false, 0)]
        [RequiredByNativeCode]
        internal static ProfilerWindow ShowProfilerWindow()
        {
            return EditorWindow.GetWindow<ProfilerWindow>(false);
        }

        // The Constructor shouldn't be part of the public API
        internal ProfilerWindow() {}

        [MenuItem("Window/Analysis/Profiler (Standalone Process)", false, 1)]
        static void ShowProfilerOOP()
        {
            if (EditorUtility.DisplayDialog("Profiler (Standalone Process)",
                "The Standalone Profiler launches the Profiler window in a separate process from the Editor. " +
                "This means that the performance of the Editor does not affect profiling data, and the Profiler does not affect the performance of the Editor. " +
                "It takes around 3-4 seconds to launch.", "OK", DialogOptOutDecisionType.ForThisUser, "UseOutOfProcessProfiler"))
            {
                ProfilerRoleProvider.LaunchProfilerProcess();
            }
        }

        static string GetRecordingStateName(string defaultName)
        {
            if (!String.IsNullOrEmpty(defaultName))
                return $"of {defaultName}";
            if (ProfilerDriver.profileEditor)
                return "editmode";
            return "playmode";
        }

        [ShortcutManagement.Shortcut("Profiling/Profiler/RecordToggle", KeyCode.F9)]
        static void RecordToggle()
        {
            var commandHandled = false;
            if (CommandService.Exists("ProfilerRecordToggle"))
            {
                var result = CommandService.Execute("ProfilerRecordToggle", CommandHint.Shortcut);
                commandHandled = Convert.ToBoolean(result);
            }

            if (!commandHandled)
            {
                if (HasOpenInstances<ProfilerWindow>())
                {
                    var profilerWindow = GetWindow<ProfilerWindow>();
                    profilerWindow.SetRecordingEnabled(!profilerWindow.IsSetToRecord());
                }
                else
                {
                    ProfilerDriver.enabled = !ProfilerDriver.enabled;
                }

                using (var state = PlayerConnectionGUIUtility.GetConnectionState(null, null))
                {
                    var connectionName = "";
                    if (state.connectedToTarget != ConnectionTarget.Editor)
                        connectionName = state.connectionName;
                    EditorGUI.hyperLinkClicked -= EditorGUI_HyperLinkClicked;
                    EditorGUI.hyperLinkClicked += EditorGUI_HyperLinkClicked;
                    if (ProfilerDriver.enabled)
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Recording {GetRecordingStateName(connectionName)} has started...");
                    else
                        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Recording has ended.\r\nClick <color={EditorGUIUtility.GetHyperlinkColorForSkin()}><link=\"openprofiler='true'\">here</link></color> to open the profiler window.");
                }
            }
        }

        static void EditorGUI_HyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if (args.hyperLinkData.ContainsKey("openprofiler"))
                ShowProfilerWindow();
        }

        [RequiredByNativeCode]
        static void RepaintAllProfilerWindows()
        {
            foreach (ProfilerWindow window in s_ProfilerWindows)
            {
                if (!window.IgnoreRepaintAllProfilerWindowsTick)
                {
                    // This is useful hack when you need to profile in the editor and dont want it to affect your framerate...
                    // NOTE: we should make this an option in the UI somehow...
                    //if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick && EditorWindow.focusedWindow == window)

                    if (ProfilerDriver.lastFrameIndex != window.m_LastFrameFromTick)
                    {
                        window.m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
                        window.m_BottlenecksChartViewController.ReloadData();
                        window.m_ChartsIMGUIContainer.MarkDirtyRepaint();
                        window.InvokeSelectedFrameIndexChangedEventIfNecessary(window.m_LastFrameFromTick);
                    }
                }
            }
        }

        void SetProfileDeepScripts(bool deep)
        {
            bool currentDeep = ProfilerDriver.deepProfiling;
            if (currentDeep == deep)
                return;


            if (ProfilerDriver.IsConnectionEditor())
            {
                SetEditorDeepProfiling(deep);
            }
            else
            {
                // When connected to the player, we send deep profiler mode command immediately.
                ProfilerDriver.deepProfiling = deep;
            }
            deepProfileChanged?.Invoke(deep);
        }

        string PickFrameLabel()
        {
            // Frames indices are incremented by 1 for display purposes.
            var lastFrameForDisplay = (ProfilerDriver.lastFrameIndex + 1);
            if (SelectedFrameRange.HasValue)
            {
                var selectedFrameRange = SelectedFrameRange.Value;
                var startFrameForDisplay = selectedFrameRange.Start.Value + 1;
                var isSingleFrame = (selectedFrameRange.End.Value - selectedFrameRange.Start.Value) == 1;
                if (isSingleFrame)
                {
                    return $"{startFrameForDisplay} / {lastFrameForDisplay}";
                }
                else
                {
                    // A C# Range's end value is exclusive, so we don't need to add one for display.
                    var endFrameForDisplay = selectedFrameRange.End.Value;
                    return $"{startFrameForDisplay}–{endFrameForDisplay} / {lastFrameForDisplay}";
                }
            }
            else
            {
                return $"{lastFrameForDisplay} / {lastFrameForDisplay}";
            }
        }

        void PrevFrame()
        {
            int previousFrame = ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame);
            if (previousFrame != FrameDataView.invalidOrCurrentFrameIndex)
                SetCurrentFrame(previousFrame);
        }

        void NextFrame()
        {
            int nextFrame = ProfilerDriver.GetNextFrameIndex(m_CurrentFrame);
            if (nextFrame != FrameDataView.invalidOrCurrentFrameIndex)
                SetCurrentFrame(nextFrame);
        }

        static bool CheckFrameData(ProfilerProperty property)
        {
            return property != null && property.frameDataReady;
        }

        internal HierarchyFrameDataView GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();
            var foundThreadIndex = FrameDataView.invalidThreadIndex;
            using (var frameIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameIterator.GetThreadCount(frameIndex);
                for (var i = 0; i < threadCount; ++i)
                {
                    frameIterator.SetRoot(frameIndex, i);
                    var grp = frameIterator.GetGroupName();
                    // only string compare if both names aren't null or empty, i.e. don't compare ` null != "" ` but treat null the same as empty
                    if (!(string.IsNullOrEmpty(grp) && string.IsNullOrEmpty(groupName)) && grp != groupName)
                        continue;
                    var thrd = frameIterator.GetThreadName();
                    if (threadName == thrd)
                    {
                        using (var rawFrameData = new RawFrameDataView(frameIndex, i))
                        {
                            // do we have a valid thread id to check against and a direct match?
                            if (threadId == FrameDataView.invalidThreadId || threadId == rawFrameData.threadId)
                            {
                                foundThreadIndex = i;
                                break;
                            }
                            // else store the first found thread index as a fallback
                            if (foundThreadIndex < 0)
                                foundThreadIndex = i;
                        }
                    }
                }
            }
            return GetFrameDataView(foundThreadIndex, viewMode, profilerSortColumn, sortAscending);
        }

        void DisposeFrameDataView()
        {
            frameDataViewAboutToBeDisposed();
            m_FrameDataView.Dispose();
        }

        internal HierarchyFrameDataView GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
        {
            var frameIndex = GetActiveVisibleFrameIndex();

            if (frameIndex < firstAvailableFrameIndex || frameIndex > lastAvailableFrameIndex)
            {
                // if the frame index is out of range, invalidate the FrameDataView
                if (m_FrameDataView != null && m_FrameDataView.valid)
                    DisposeFrameDataView();
            }
            else if (frameIndex != FrameDataView.invalidOrCurrentFrameIndex)
            {
                // if the frame is valid but the thread index is not, fallback onto main thread
                if (threadIndex < 0)
                    threadIndex = k_MainThreadIndex;
                else
                {
                    using (var iter = new ProfilerFrameDataIterator())
                    {
                        iter.SetRoot(frameIndex, k_MainThreadIndex);
                        if (threadIndex >= iter.GetThreadCount(frameIndex))
                            threadIndex = k_MainThreadIndex;
                    }
                }
            }

            if (m_FrameDataView != null && m_FrameDataView.valid)
            {
                if (m_FrameDataView.frameIndex == frameIndex && m_FrameDataView.threadIndex == threadIndex && m_FrameDataView.viewMode == viewMode)
                    return m_FrameDataView;
            }

            if (m_FrameDataView != null)
                DisposeFrameDataView();
            m_FrameDataView = new HierarchyFrameDataView(frameIndex, threadIndex, viewMode, profilerSortColumn, sortAscending);
            return m_FrameDataView;
        }

        void UpdateModules()
        {
            foreach (var module in m_AllModules)
            {
                if (module.active)
                {
                    module.Update();
                }
            }
        }

        void SetCallstackRecordMode(ProfilerMemoryRecordMode memRecordMode)
        {
            if (memRecordMode == m_CurrentCallstackRecordMode)
                return;
            m_CurrentCallstackRecordMode = memRecordMode;
            ProfilerDriver.memoryRecordMode = memRecordMode;
            memoryRecordingModeChanged?.Invoke(memRecordMode);
        }

        void ToggleCallstackRecordModeFlag(object userData, string[] options, int selected)
        {
            m_CallstackRecordMode ^= Styles.recordCallstacksEnumValues[selected];
            if (m_CurrentCallstackRecordMode != ProfilerMemoryRecordMode.None)
                SetCallstackRecordMode(m_CallstackRecordMode);
        }

        internal void SaveProfilingData()
        {
            var dateString = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture);
            var prodName = Application.productName;

            // Sanitise the product name
            var prodNameSanitised = new StringBuilder(Regex.Replace(prodName, @"(\.\.|[/\\])", "_"));
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var t in invalidChars)
                prodNameSanitised.Replace(t, '_');
            invalidChars = Path.GetInvalidPathChars();
            foreach (var t in invalidChars)
                prodNameSanitised.Replace(t, '_');

            var filePath = $"{ProfilerUserSettings.AbsoluteProfilerCaptureStoragePath}/{prodNameSanitised}_{dateString}.data";

            EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, filePath);
            if (ProfilerDriver.SaveProfile(filePath))
            {
                // Saving the .data was successful, now save the bottleneck data and screenshot
                // Make sure we're up to date, ready to write out the highlights file.
                m_BottlenecksChartViewController.ReloadData();
                m_BottlenecksChartViewController.SaveHighlightsInfo(filePath);
                // If we fail to find screenshot data, try once with the previous frame,
                // in case the last one was cut short mid-write.
                if (!m_ScreenshotsManager.WriteOutMostRecentScreenshot(filePath, ProfilerDriver.lastFrameIndex))
                    m_ScreenshotsManager.WriteOutMostRecentScreenshot(filePath, ProfilerDriver.lastFrameIndex - 1);
                CurrentLoadedCaptureFile = filePath;
                m_CaptureDataService.SetCapturesFolderDirty();
            }
        }

        internal void LoadProfilingData(bool keepExistingData)
        {
            string recent = EditorPrefs.GetString(kProfilerRecentSaveLoadProfilePath);
            string selected = EditorUtility.OpenFilePanelWithFilters(Styles.loadWindowTitle.text, recent, Styles.loadProfilingDataFileFilters);

            LoadProfilingData(keepExistingData, selected);
        }

        internal void LoadProfilingData(bool keepExistingData, string path, bool showWarning = false)
        {
            if (string.IsNullOrEmpty(path))
                return;
            EditorPrefs.SetString(kProfilerRecentSaveLoadProfilePath, path);

            var profilerHasFrames = ProfilerHasAnyFrames();
            if (showWarning && !keepExistingData && profilerHasFrames && path != CurrentLoadedCaptureFile &&
                !File.Exists(CurrentLoadedCaptureFile) && // No need to show warning if the data won't be lost
                !EditorUtility.DisplayDialog("Load Profiler Capture",
                    "Loading Capture will clear currently loaded data. Continue?", "OK", "Cancel"))
                return;

            if (!ProfilerDriver.LoadProfile(path, keepExistingData))
                return;

            // If we're appending, don't mark the newly loaded file as being the sole opened data.
            if (keepExistingData && profilerHasFrames)
                CurrentLoadedCaptureFile = string.Empty;
            else
                CurrentLoadedCaptureFile = path;

            // Stop current profiling if data was loaded successfully
            ProfilerDriver.enabled = m_Recording = false;
            SessionState.SetBool(kProfilerEnabledSessionKey, m_Recording);
            if (ProfilerUserSettings.rememberLastRecordState)
                EditorPrefs.SetBool(kProfilerEnabledSessionKey, m_Recording);

            // If there's an existing screenshot, start with that. If the user adds to this capture and saves,
            // it'll be better to have the old screenshot to show than nothing.
            if (!m_ScreenshotsManager.ReadInOrReset(path))
            {
                // If there's no screenshot already written out, search the metadata.
                // If we fail to find screenshot data, try once with the previous frame,
                // in case the last one was cut short mid-write.
                if (!m_ScreenshotsManager.WriteOutMostRecentScreenshot(path, ProfilerDriver.lastFrameIndex))
                    m_ScreenshotsManager.WriteOutMostRecentScreenshot(path, ProfilerDriver.lastFrameIndex - 1);
            }

            // Update the serialised highlights data.
            if (m_BottlenecksChartViewController.SaveHighlightsInfo(path))
                m_CaptureDataService.SetCapturesFolderDirty();
        }

        internal void SetRecordingEnabled(bool profilerEnabled)
        {
            ProfilerDriver.enabled = profilerEnabled;
            m_Recording = profilerEnabled;

            SessionState.SetBool(kProfilerEnabledSessionKey, profilerEnabled);
            if (ProfilerUserSettings.rememberLastRecordState)
                EditorPrefs.SetBool(kProfilerEnabledSessionKey, profilerEnabled);

            if (profilerEnabled)
                ProfilerWindowAnalytics.StartCapture();

            recordingStateChanged?.Invoke(m_Recording);

            Repaint();
        }

        void DrawMainToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawShowHideCapturesView();
            DrawModuleSelectionDropdownMenu();

            // Engine attach
            PlayerConnectionGUILayout.ConnectionTargetSelectionDropdown(m_AttachProfilerState, EditorStyles.toolbarDropDown, Mathf.Max((int)(position.width - k_MinimumToolbarContentWidth), 60));

            // Record
            var profilerEnabled = GUILayout.Toggle(m_Recording, m_Recording ? Styles.profilerRecordOn : Styles.profilerRecordOff, EditorStyles.toolbarButton);
            if (profilerEnabled != m_Recording)
                SetRecordingEnabled(profilerEnabled);

            FrameNavigationControls();

            using (new EditorGUI.DisabledScope(!ProfilerHasAnyFrames()))
            {
                // Clear
                if (GUILayout.Button(Styles.clearData, EditorStyles.toolbarButton))
                {
                    Clear();
                    GUIUtility.ExitGUI();
                }
            }

            // Separate File/Stream control elements from toggles
            GUILayout.FlexibleSpace();
            // Clear on Play
            SetClearOnPlay(GUILayout.Toggle(GetClearOnPlay(), Styles.clearOnPlay, EditorStyles.toolbarButton));

            // Deep profiling
            var deepProfilerSupported = m_AttachProfilerState.deepProfilingSupported;
            using (new EditorGUI.DisabledScope(!deepProfilerSupported))
            {
                SetProfileDeepScripts(GUILayout.Toggle(ProfilerDriver.deepProfiling, deepProfilerSupported ? Styles.deepProfile : Styles.deepProfileNotSupported, EditorStyles.toolbarButton));
            }

            // Allocation call stacks
            AllocationCallstacksToolbarItem();

            // keep more space between the toggles and the overflow/help icon buttons on the far right, keep deep profiling closer to the other controls
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();
            GUILayout.FlexibleSpace();

            // Load profile
            if (GUILayout.Button(Styles.loadProfilingData, EditorStyles.toolbarButton, Styles.buttonWidthOption))
            {
                LoadProfilingData(Event.current.shift);

                // Opened a load pop-up, MacOS will redraw the window so bail out now
                EditorGUIUtility.ExitGUI();
            }

            // Save profile
            using (new EditorGUI.DisabledScope(!ProfilerHasAnyFrames()))
            {
                if (GUILayout.Button(Styles.saveProfilingData, EditorStyles.toolbarButton))
                {
                    SaveProfilingData();

                    // Opened a save pop-up, MacOS will redraw the window so bail out now
                    EditorGUIUtility.ExitGUI();
                }
            }

            // Open Manual
            if (GUILayout.Button(Styles.helpButtonContent, EditorStyles.toolbarButton))
            {
                var help = Help.FindHelpNamed("ProfilerWindow");
                Help.BrowseURL(help);
            }

            // Overflow Menu
            var overflowMenuRect = GUILayoutUtility.GetRect(Styles.optionsButtonContent, EditorStyles.toolbarButtonRight);
            if (GUI.Button(overflowMenuRect, Styles.optionsButtonContent, EditorStyles.toolbarButtonRight))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(Styles.showStatsLabelsOnCurrentFrameLabel, ProfilerUserSettings.showStatsLabelsOnCurrentFrame, OnToggleShowStatsLabelsOnCurrentFrame);
                menu.AddSeparator("");
                menu.AddItem(Styles.preferencesButtonContent, false, OpenProfilerPreferences);
                menu.DropDown(overflowMenuRect);
            }

            GUILayout.EndHorizontal();
        }

        void ShowCapturesList(bool visible)
        {
            if (visible)
                m_CapturesListSplitView.UnCollapse();
            else
            {
                if (!float.IsNaN(m_CapturesListSplitView.resolvedStyle.width))
                    EditorPrefs.SetFloat(k_CapturesSplitViewFixedPaneSizePreferenceKey, m_CapturesListSplitView.fixedPane.resolvedStyle.width);
                m_CapturesListSplitView.CollapseChild(0);
            }

            EditorPrefs.SetBool(k_CapturesSplitViewToggleIsVisibleStatePreferenceKey, visible);
        }

        void DrawShowHideCapturesView()
        {
            if (GUILayout.Button(Styles.showHideCaptures, EditorStyles.toolbarButtonLeft, Styles.buttonWidthOption))
            {
                // Toggle based on existing visibility
                ShowCapturesList(m_CapturesListSplitView.fixedPane.style.display == DisplayStyle.None);
            }
        }

        void DrawModuleSelectionDropdownMenu()
        {
            Rect popupRect = GUILayoutUtility.GetRect(Styles.addArea, EditorStyles.toolbarDropDown, Styles.chartWidthOption);
            if (EditorGUI.DropdownButton(popupRect, Styles.addArea, FocusType.Passive, EditorStyles.toolbarDropDownLeft))
            {
                var popupScreenRect = GUIUtility.GUIToScreenRect(popupRect);
                var bottleneckViewVisible = m_PersistentSettingsService.IsBottleneckViewVisible;
                if (ProfilerModulesDropdownWindow.TryPresentIfNoOpenInstances(popupScreenRect, m_AllModules, bottleneckViewVisible, out var modulesDropdownWindow))
                {
                    modulesDropdownWindow.responder = this;
                }
            }
        }

        void OpenProfilerPreferences()
        {
            var settings = SettingsWindow.Show(SettingsScope.User, "Preferences/Analysis/Profiler");
            if (settings == null)
            {
                Debug.LogError("Could not find Preferences for 'Analysis/Profiler'");
            }
        }

        void AllocationCallstacksToolbarItem()
        {
            // Whenever we unset all flags, we fallback to the default GC Alloc callstacks.
            if (m_CallstackRecordMode == ProfilerMemoryRecordMode.None)
                m_CallstackRecordMode = ProfilerMemoryRecordMode.GCAlloc;

            var selectedMemRecordMode = m_CurrentCallstackRecordMode;
            var toggled = selectedMemRecordMode != ProfilerMemoryRecordMode.None;
            var oldToggleState = toggled;
            if (EditorGUILayout.DropDownToggle(ref toggled, Styles.recordCallstacks, EditorStyles.toolbarDropDownToggle))
            {
                var rect = GUILayoutUtility.topLevel.GetLast();
                var names = Unsupported.IsDeveloperMode() ? Styles.recordCallstacksDevelopmentOptions : Styles.recordCallstacksOptions;
                var selected = new List<int>();
                for (var i = 0; i < names.Length; ++i)
                {
                    if ((m_CallstackRecordMode & Styles.recordCallstacksEnumValues[i]) != 0)
                        selected.Add(i);
                }
                EditorUtility.DisplayCustomMenu(rect, names, selected.ToArray(), ToggleCallstackRecordModeFlag, null);
                GUIUtility.ExitGUI();
            }
            if (toggled != oldToggleState)
            {
                selectedMemRecordMode = m_CurrentCallstackRecordMode != ProfilerMemoryRecordMode.None ? ProfilerMemoryRecordMode.None : m_CallstackRecordMode;
                SetCallstackRecordMode(selectedMemRecordMode);
            }
        }

        void FrameNavigationControls()
        {
            // TODO We shouldn't be relying on GUI cycles to reset state like this.
            if (m_CurrentFrame > ProfilerDriver.lastFrameIndex)
            {
                SetCurrentFrameDontPause(ProfilerDriver.lastFrameIndex);
            }

            // Previous/next/current buttons
            using (new EditorGUI.DisabledScope(ProfilerDriver.GetPreviousFrameIndex(m_CurrentFrame) == FrameDataView.invalidOrCurrentFrameIndex))
            {
                if (GUILayout.Button(Styles.prevFrame, EditorStyles.toolbarButton))
                {
                    if (m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex)
                        PrevFrame();
                    PrevFrame();
                }
            }

            using (new EditorGUI.DisabledScope(ProfilerDriver.GetNextFrameIndex(m_CurrentFrame) == FrameDataView.invalidOrCurrentFrameIndex))
            {
                if (GUILayout.Button(Styles.nextFrame, EditorStyles.toolbarButton))
                    NextFrame();
            }


            using (new EditorGUI.DisabledScope(ProfilerDriver.lastFrameIndex < 0))
            {
                if (GUILayout.Toggle(ProfilerDriver.lastFrameIndex >= 0 && m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex, Styles.currentFrame, EditorStyles.toolbarButton))
                {
                    if (!m_CurrentFrameEnabled)
                    {
                        SelectAndStayOnLatestFrame();
                    }
                }
                else if (m_CurrentFrame == FrameDataView.invalidOrCurrentFrameIndex)
                {
                    m_CurrentFrameEnabled = false;
                    PrevFrame();
                }
                else if (m_CurrentFrameEnabled && m_CurrentFrame >= 0)
                {
                    m_CurrentFrameEnabled = false;
                }
            }

            // Frame number
            var frameCountLabel = new GUIContent(Styles.frame.text + PickFrameLabel());
            float maxWidth, minWidth;
            EditorStyles.toolbarLabel.CalcMinMaxWidth(frameCountLabel, out minWidth, out maxWidth);
            if (minWidth > m_FrameCountLabelMinWidth)
                // to avoid increasing the size in too fine-grained intervals, add a 10 pixel buffer.
                m_FrameCountLabelMinWidth = minWidth + 10;
            GUILayout.Label(frameCountLabel, EditorStyles.toolbarLabel, GUILayout.MinWidth(m_FrameCountLabelMinWidth));
        }

        public void SelectAndStayOnLatestFrame()
        {
            SetCurrentFrame(FrameDataView.invalidOrCurrentFrameIndex);
            m_LastFrameFromTick = ProfilerDriver.lastFrameIndex;
            m_CurrentFrameEnabled = true;
        }

        void SetCurrentFrameDontPause(int frame)
        {
            Range? frameRange = null;
            if (frame != FrameDataView.invalidOrCurrentFrameIndex)
                frameRange = new Range(frame, frame + 1);
            SetCurrentFrameRangeDontPause(frameRange);
        }

        void SetCurrentFrameRangeDontPause(Range? frameRange)
        {
            // 'Frame index' API uses the start of the range.
            m_CurrentFrame = (frameRange.HasValue) ? frameRange.Value.Start.Value : FrameDataView.invalidOrCurrentFrameIndex;

            SelectedFrameRange = frameRange;
            InvokeSelectedFrameIndexChangedEventIfNecessary(m_CurrentFrame);
        }

        void SetCurrentFrame(int frame)
        {
            bool shouldPause = frame != FrameDataView.invalidOrCurrentFrameIndex && ProfilerDriver.enabled && !ProfilerDriver.profileEditor && m_CurrentFrame != frame;
            if (shouldPause && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.isPaused = true;

            currentFrameChanged?.Invoke(frame, shouldPause);

            SetCurrentFrameDontPause(frame);
        }

        internal void SetActiveVisibleFrameIndex(int frame)
        {
            if (frame != FrameDataView.invalidOrCurrentFrameIndex && (frame < ProfilerDriver.firstFrameIndex || frame > ProfilerDriver.lastFrameIndex))
                throw new ArgumentOutOfRangeException($"{nameof(frame)}");

            currentFrameChanged?.Invoke(frame, false);
            SetCurrentFrameDontPause(frame);
            Repaint();
        }

        void DoLegacyToolbarGUI()
        {
            EventType eventType = Event.current.type;
            if (eventType == EventType.MouseDown)
                ProfilerWindowAnalytics.RecordMouseDownUsabilityEvent();
            else if (eventType == EventType.KeyDown)
                ProfilerWindowAnalytics.RecordKeyDownUsabilityEvent();

            CheckForPlatformModuleChange();

            DrawMainToolbar();
        }

        void DoLegacyChartsGUI()
        {
            m_GraphPos = EditorGUILayout.BeginScrollView(m_GraphPos, Styles.profilerGraphBackground);

            // MainSplitView.fixedPane will be null on the first pass through here as MainSplitView picks up its children in its PostDisplaySetup.
            var fixedPaneRect = (MainSplitView.fixedPane != null) ?  MainSplitView.fixedPane.layout : Rect.zero;
            var verticalScrollbarStyle = GUI.skin.verticalScrollbar;
            var scrollViewContentWidth = fixedPaneRect.width - verticalScrollbarStyle.fixedWidth - verticalScrollbarStyle.padding.horizontal;
            var scrollViewViewportHeight = fixedPaneRect.height;
            int newCurrentFrame = DrawModuleChartViews(new Vector2(scrollViewContentWidth, scrollViewViewportHeight));
            if (newCurrentFrame != m_CurrentFrame)
            {
                SetCurrentFrame(newCurrentFrame);
                Repaint();
                if (Event.current.type != EventType.Repaint)
                    GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        int DrawModuleChartViews(Vector2 containerSize)
        {
            // Calculate the total minimum chart height of all active modules.
            var totalMinimumChartHeight = 0f;
            var activeModuleCount = 0;
            var lastActiveModuleIndex = -1;
            for (int i = 0; i < m_AllModules.Count; ++i)
            {
                var module = m_AllModules[i];
                if (module.active)
                {
                    if (module.Identifier == kJobsProfilerIdentifier)
                        continue;

                    totalMinimumChartHeight += module.GetMinimumChartHeight();
                    activeModuleCount++;
                    lastActiveModuleIndex = i;
                }
            }

            var newCurrentFrame = m_CurrentFrame;
            if (activeModuleCount > 0)
            {
                // If there will be empty space below the charts, calculate how much to expand each chart by to fill this space.
                var additionalChartHeight = 0f;
                var requiresChartHeightExpansion = totalMinimumChartHeight < containerSize.y;
                if (requiresChartHeightExpansion)
                {
                    var verticalSpaceToFill = containerSize.y - totalMinimumChartHeight;
                    additionalChartHeight = GUIUtility.RoundToPixelGrid(verticalSpaceToFill / activeModuleCount);
                }

                var accumulatedExpandedChartHeight = 0f;
                for (int i = 0; i < m_AllModules.Count; ++i)
                {
                    var module = m_AllModules[i];
                    if (module.active)
                    {
                        if (module.Identifier == kJobsProfilerIdentifier)
                            continue;

                        // Calculate final chart height.
                        var chartHeight = module.GetMinimumChartHeight();
                        if (requiresChartHeightExpansion)
                        {
                            // Due to rounding additionalChartHeight to the pixel grid, we make the last chart fill the remaining space. This ensures that exactly the whole space is filled whilst maintaining that all expanded charts remain on the pixel grid.
                            if (i == lastActiveModuleIndex)
                            {
                                var remainingHeightToFill = containerSize.y - accumulatedExpandedChartHeight;
                                chartHeight = remainingHeightToFill;
                            }
                            else
                            {
                                chartHeight += additionalChartHeight;
                                accumulatedExpandedChartHeight += chartHeight;
                            }
                        }

                        // Reserve a chart rect with the layout system.
                        var chartRect = GUILayoutUtility.GetRect(containerSize.x, chartHeight);

                        // Don't draw or update any charts during the layout pass, where rects are not computed yet.
                        if (Event.current.type != EventType.Layout)
                        {
                            // Only draw or update modules that will be visible in the scroll view's viewport.
                            if (GUIClip.visibleRect.Overlaps(chartRect))
                            {
                                // DrawChartView also handles interaction so we can't only call it when repainting.
                                bool isSelected = (m_SelectedModuleIndex == i);
                                var lastVisibleFrameIndex = ProfilerDriver.lastFrameIndex;
                                newCurrentFrame = module.DrawChartView(chartRect, newCurrentFrame, isSelected, lastVisibleFrameIndex);
                            }
                        }
                    }
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                // Temporary workaround whilst Bottleneck is not a module due to upcoming transition to UIToolkit.
                if (!m_BottlenecksViewContainer.visible)
                    GUILayout.Label(Styles.noActiveModules);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            if (Event.current.type == EventType.Repaint)
            {
                var isScrollBarVisible = totalMinimumChartHeight > containerSize.y;
                if (!m_IsLegacyChartsGUIScrollBarVisible.HasValue || m_IsLegacyChartsGUIScrollBarVisible != isScrollBarVisible)
                {
                    m_IsLegacyChartsGUIScrollBarVisible = isScrollBarVisible;
                    OnLegacyChartsGUIScrollbarVisibilityChanged();
                }
            }

            return newCurrentFrame;
        }

        void OnLegacyChartsGUIScrollbarVisibilityChanged()
        {
            var marginRight = m_IsLegacyChartsGUIScrollBarVisible.Value ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
            m_BottlenecksViewContainer.style.marginRight = marginRight;
        }

        void ProfilerModulesDropdownWindow.IResponder.OnModuleActiveStateChanged()
        {
            m_ChartsIMGUIContainer.MarkDirtyRepaint();

            // If we have no module selected, including bottleneck, try to select the first one.
            if (m_SelectedModuleIndex == k_NoModuleSelected && m_BottlenecksDetailsViewController == null)
                SelectFirstActiveModule();
        }

        // Temporary workaround whilst Bottleneck is not a module due to upcoming transition to UIToolkit.
        void ProfilerModulesDropdownWindow.IResponder.OnBottlenecksActiveStateChanged(bool active)
        {
            SetBottleneckViewVisible(active);
        }

        internal void SetBottleneckViewVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_BottlenecksViewContainer, visible);
            SetCategoriesInUse(new [] { ProfilerCategory.Render.Name }, visible);
            m_PersistentSettingsService.IsBottleneckViewVisible = visible;

            // If the bottleneck view was disabled whilst it was selected (determined by its details view controller existing), select another module.
            if (!visible && m_BottlenecksDetailsViewController != null)
                SelectFirstActiveModule();

            // If GPU module state changed, force update CPU module
            var gpuModule = this.GetProfilerModuleByType<GPUProfilerModule>();
            if (gpuModule.active != m_LastGPUModuleActiveState)
            {
                m_LastGPUModuleActiveState = gpuModule.active;

                var cpuModule = this.GetProfilerModuleByType<CPUProfilerModule>();
                cpuModule.Update();
            }
        }

        internal bool IsBottleneckViewVisible()
        {
            return (m_BottlenecksDetailsViewController != null);
        }

        void ProfilerModulesDropdownWindow.IResponder.OnConfigureModules()
        {
            ModuleEditorWindow moduleEditorWindow;
            if (ModuleEditorWindow.TryGetOpenInstance(out moduleEditorWindow))
            {
                moduleEditorWindow.Focus();
            }
            else
            {
                moduleEditorWindow = ModuleEditorWindow.Present(m_AllModules, ConnectedToEditor);
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }
        }

        void ProfilerModulesDropdownWindow.IResponder.OnRestoreDefaultModules()
        {
            DeselectSelectedModuleIfNecessary();

            int index = m_AllModules.Count - 1;
            while (index >= 0)
            {
                var module = m_AllModules[index];
                if (module is DynamicProfilerModule)
                {
                    DeleteProfilerModuleAtIndex(index);
                }

                module.ResetToDefaultPreferences();

                index--;
            }

            SortModuleCollectionInPlace(ref m_AllModules);

            PersistDynamicModulesToEditorPrefs();
            UpdateModules();
            Repaint();

            if (ModuleEditorWindow.TryGetOpenInstance(out var moduleEditorWindow))
            {
                moduleEditorWindow.Close();
                moduleEditorWindow = ModuleEditorWindow.Present(m_AllModules, ConnectedToEditor);
                moduleEditorWindow.onChangesConfirmed += OnModuleEditorChangesConfirmed;
            }
        }

        void OnModuleEditorChangesConfirmed(ReadOnlyCollection<ModuleData> modules, ReadOnlyCollection<ModuleData> deletedModules)
        {
            var selectedModuleIndexCached = m_SelectedModuleIndex;

            int index = 0;
            foreach (var moduleData in modules)
            {
                switch (moduleData.editedState)
                {
                    case ModuleData.EditedState.Created:
                    {
                        CreateNewProfilerModule(moduleData, index);
                        break;
                    }

                    case ModuleData.EditedState.Updated:
                    {
                        UpdateProfilerModule(moduleData, index, selectedModuleIndexCached);
                        break;
                    }
                }

                index++;
            }

            foreach (var moduleData in deletedModules)
            {
                DeleteProfilerModule(moduleData);
            }

            // If any modules were deleted, all existing modules should update/refresh their order index.
            bool hasDeletedModules = deletedModules.Count > 0;
            if (hasDeletedModules)
            {
                for (int i = 0; i < m_AllModules.Count; i++)
                {
                    var profilerModule = m_AllModules[i];
                    profilerModule.orderIndex = i;
                }
            }

            SortModuleCollectionInPlace(ref m_AllModules);
            PersistDynamicModulesToEditorPrefs();
            UpdateModules();
            Repaint();
        }

        void CreateNewProfilerModule(ModuleData moduleData, int orderIndex)
        {
            var identifier = moduleData.name; // Dynamic modules use their name as their identifier for legacy reasons.
            var module = new DynamicProfilerModule();

            var args = new ProfilerModule.InitializationArgs(identifier, moduleData.name, DynamicProfilerModule.iconPath, this);
            var chartCounters = new List<ProfilerCounterData>(moduleData.chartCounters);
            var detailCounters = new List<ProfilerCounterData>(moduleData.detailCounters);
            module.Initialize(args, chartCounters, detailCounters);

            module.orderIndex = orderIndex;

            m_AllModules.Add(module);
            module.OnEnable();
        }

        void UpdateProfilerModule(ModuleData moduleData, int orderIndex, int selectedModuleIndexCached)
        {
            var currentProfilerModuleIdentifier = moduleData.currentProfilerModuleIdentifier;
            int updatedModuleIndex = IndexOfModuleWithIdentifier(currentProfilerModuleIdentifier);
            if (updatedModuleIndex < 0)
            {
                throw new IndexOutOfRangeException($"Unable to update module '{moduleData.name}' at index '{updatedModuleIndex}'.");
            }

            var module = m_AllModules[updatedModuleIndex];
            var isSelectedIndex = (module.orderIndex == selectedModuleIndexCached);

            var chartCounters = new List<ProfilerCounterData>(moduleData.chartCounters);
            var detailCounters = new List<ProfilerCounterData>(moduleData.detailCounters);
            // Only legacy modules can have their name and counters set like this.
            if (module is ProfilerModuleBase legacyModule)
            {
                legacyModule.SetNameAndUpdateAllPreferences(moduleData.name);
                legacyModule.SetCounters(chartCounters, detailCounters);
            }
            module.orderIndex = orderIndex;

            if (isSelectedIndex)
            {
                m_SelectedModuleIndex = orderIndex;
            }
        }

        void DeleteProfilerModule(ModuleData moduleData)
        {
            var currentProfilerModuleIdentifier = moduleData.currentProfilerModuleIdentifier;
            int deletedModuleIndex = IndexOfModuleWithIdentifier(currentProfilerModuleIdentifier);
            DeleteProfilerModuleAtIndex(deletedModuleIndex);
        }

        void DeleteProfilerModuleAtIndex(int index)
        {
            if (index < 0 || index >= m_AllModules.Count)
            {
                throw new IndexOutOfRangeException($"Unable to delete module at index '{index}'.");
            }

            var moduleToDelete = m_AllModules[index];
            // Ensure that active areas in use are decremented. Additionally, if moduleToDelete is currently selected, the SetActive(false) call will invoke its OnDeselected callback prior to the OnDisable callback below (via its chart's onClosed callback, which invokes the IProfilerWindowController's CloseModule method, which invokes the OnDeselected callback).
            moduleToDelete.active = false;
            moduleToDelete.OnDisable();
            moduleToDelete.DeleteAllPreferences();
            m_AllModules.RemoveAt(index);
        }

        int IndexOfModuleWithIdentifier(string moduleIdentifier)
        {
            int moduleIndex = k_NoModuleSelected;
            for (int i = 0; i < m_AllModules.Count; i++)
            {
                var module = m_AllModules[i];
                if (module.Identifier.Equals(moduleIdentifier))
                {
                    moduleIndex = i;
                    break;
                }
            }

            return moduleIndex;
        }

        void PersistDynamicModulesToEditorPrefs()
        {
            var serializableDynamicModules = DynamicProfilerModule.SerializedDataCollection.FromDynamicProfilerModulesInCollection(m_AllModules);
            var json = JsonUtility.ToJson(serializableDynamicModules);
            EditorPrefs.SetString(k_DynamicModulesPreferenceKey, json);
        }

        internal void SetClearOnPlay(bool enabled)
        {
            m_ClearOnPlay = enabled;
        }

        internal bool GetClearOnPlay()
        {
            return m_ClearOnPlay;
        }

        void OnTargetedEditorConnectionChanged(EditorConnectionTarget change)
        {
            switch (change)
            {
                case EditorConnectionTarget.None:
                case EditorConnectionTarget.MainEditorProcessPlaymode:
                    ProfilerDriver.profileEditor = false;
                    recordingStateChanged?.Invoke(m_Recording);
                    break;
                case EditorConnectionTarget.MainEditorProcessEditmode:
                    ProfilerDriver.profileEditor = true;
                    recordingStateChanged?.Invoke(m_Recording);
                    break;
                default:
                    ProfilerDriver.profileEditor = false;
                    if (Unsupported.IsDeveloperMode())
                        Debug.LogError($"{change} is not implemented!");
                    break;
            }

            SessionState.SetBool(kProfilerEditorTargetModeEnabledSessionKey, ProfilerDriver.profileEditor);
        }

        bool IsEditorConnectionTargeted(EditorConnectionTarget connection)
        {
            switch (connection)
            {
                case EditorConnectionTarget.None:
                case EditorConnectionTarget.MainEditorProcessPlaymode:
                case EditorConnectionTarget.MainEditorProcessEditmode:
                    return ProfilerDriver.profileEditor;
                default:
                    if (Unsupported.IsDeveloperMode())
                        Debug.LogError($"{connection} is not implemented!");
                    return ProfilerDriver.profileEditor == false;
            }
        }

        void OnConnectedToPlayer(string player, EditorConnectionTarget? editorConnectionTarget)
        {
            if (editorConnectionTarget == null || editorConnectionTarget.Value == EditorConnectionTarget.None)
                ClearFramesOnPlayOrPlayerConnectionChange();
        }

        internal static bool SetEditorDeepProfiling(bool deep)
        {
            var doApply = true;

            if (ProcessService.level == ProcessLevel.Main)
            {
                // When enabling / disabling deep script profiling we need to reload scripts.
                // In play mode this might be intrusive. So ask the user first.
                if (EditorApplication.isPlaying)
                {
                    if (deep)
                        doApply = EditorUtility.DisplayDialog(Styles.enableDeepProfilingWarningDialogTitle, Styles.enableDeepProfilingWarningDialogContent, Styles.domainReloadWarningDialogButton, Styles.cancelDialogButton, DialogOptOutDecisionType.ForThisSession, kProfilerDeepProfilingWarningSessionKey);
                    else
                        doApply = EditorUtility.DisplayDialog(Styles.disableDeepProfilingWarningDialogTitle, Styles.disableDeepProfilingWarningDialogContent, Styles.domainReloadWarningDialogButton, Styles.cancelDialogButton, DialogOptOutDecisionType.ForThisSession, kProfilerDeepProfilingWarningSessionKey);
                }
            }

            if (doApply)
            {
                ProfilerDriver.deepProfiling = deep;
                if (ProcessService.level == ProcessLevel.Main)
                    EditorUtility.RequestScriptReload();
            }

            return doApply;
        }

        internal void SetCategoriesInUse(IEnumerable<string> categoryNames, bool inUse)
        {
            if (inUse)
                foreach (var categoryName in categoryNames)
                    m_CategoryActivator.RetainCategory(categoryName);
            else
                foreach (var categoryName in categoryNames)
                    m_CategoryActivator.ReleaseCategory(categoryName);
        }

        /// <summary>
        /// Select the Profiler module at the specified index. The currently selected Profiler module will be deselected.
        /// </summary>
        /// <param name="index"></param>
        void SelectModuleAtIndex(int index)
        {
            var module = ModuleAtIndex(index);
            SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(module, index);
        }

        /// <summary>
        /// Select the specified Profiler module. The currently selected Profiler module will be deselected.
        /// </summary>
        /// <param name="module"></param>
        void SelectModule(ProfilerModule module)
        {
            var index = IndexOfModule(module);
            SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(module, index);
        }

        /// <summary>
        /// Select the first active Profiler module. The currently selected Profiler module will be deselected.
        /// </summary>
        void SelectFirstActiveModule()
        {
            var moduleIndexToSelect = k_NoModuleSelected;
            for (int i = 0; i < m_AllModules.Count; ++i)
            {
                var module = m_AllModules[i];
                if (module.active)
                {
                    moduleIndexToSelect = i;
                    break;
                }
            }

            SelectModuleAtIndex(moduleIndexToSelect);
        }

        void SelectModuleWithIndexAndDeselectSelectedModuleIfNecessary(ProfilerModule moduleToSelect, int moduleIndexToSelect)
        {
            DeselectSelectedModuleIfNecessary();

            // If the bottlenecks details view is currently shown, dispose of it
            if (m_BottlenecksDetailsViewController != null)
            {
                m_BottlenecksDetailsViewController.Dispose();
                m_BottlenecksDetailsViewController = null;
            }

            if (moduleToSelect != null)
            {
                // Ensure the module being selected is active.
                if (!moduleToSelect.active)
                {
                    moduleToSelect.active = true;
                }

                // Send module selection analytics event
                ProfilerWindowAnalytics.SwitchActiveView(moduleToSelect.DisplayName);

                try
                {
                    // Create the module's details view and add it to the hierarchy.
                    var detailsView = moduleToSelect.CreateDetailsView();
                    if (detailsView == null)
                        throw new InvalidOperationException($"{moduleToSelect.DisplayName} did not provide a details view.");
                    m_DetailsViewContainer.Add(detailsView);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Unable to create a details view for the module '{moduleToSelect.DisplayName}'. {e.Message}\n{e.StackTrace}");
                }

                m_SelectedModuleIndex = moduleIndexToSelect;
            }
        }

        void DeselectSelectedModuleIfNecessary()
        {
            DeselectModuleAtIndexIfNecessary(m_SelectedModuleIndex);
        }

        void DeselectModuleAtIndexIfNecessary(int index)
        {
            var moduleIndexToDeselect = index;
            var moduleToDeselect = ModuleAtIndex(moduleIndexToDeselect);
            if (moduleToDeselect != null)
            {
                moduleToDeselect.CloseDetailsView();
                m_SelectedModuleIndex = k_NoModuleSelected;
            }
        }

        int IndexOfModule(ProfilerModule module)
        {
            int index = k_NoModuleSelected;
            for (int i = 0; i < m_AllModules.Count; i++)
            {
                var m = m_AllModules[i];
                if (m.Equals(module))
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        ProfilerModule ModuleAtIndex(int index)
        {
            if ((index != k_NoModuleSelected) && (index >= 0) && (index < m_AllModules.Count))
            {
                return m_AllModules[index];
            }

            return null;
        }

        void BottlenecksChartViewController.IResponder.ChartViewSelectedFrameRange(Range? frameRange)
        {
            // Create the bottleneck details view if necessary.
            if (m_BottlenecksDetailsViewController == null)
            {
                DeselectSelectedModuleIfNecessary();
                m_BottlenecksDetailsViewController = new BottlenecksDetailsViewController(m_DataService, m_PersistentSettingsService, this);
                m_DetailsViewContainer.Add(m_BottlenecksDetailsViewController.View);

                ProfilerWindowAnalytics.SwitchActiveView("Highlights");

                // If the Bottleneck module was not already selected, do not change frame. This matches
                // existing behaviour to help with switching modules without changing frame.
                return;
            }

            SetCurrentFrameRangeDontPause(frameRange);
        }

        // Ideally we wouldn't need this method. However, the current IMGUI tangle means setting the current frame can
        // occur an unpredictable number of times per frame. We want to ensure we only invoke this event once for the
        // selected frame. Fully transitioning to UIToolkit (especially on the toolbar) should simplify this.
        Range? m_LastReportedSelectedFrameRange;
        void InvokeSelectedFrameIndexChangedEventIfNecessary(int newFrame)
        {
            // The selected frame range can change without the selected index changing; for example, when the start of the range is the same and the legacy 'frame index' API selects the start of the range. This is why the last reported range is tracked, rather than the last reported index, even though the index is what is actually passed to the API. This allow us to support the existing 'frame index' API without changing its behaviour, whilst supporting range select internally as we migrate more modules to it.
            var selectionWasCleared = (SelectedFrameRange == null) && (m_LastReportedSelectedFrameRange != null);
            var selectionWasChanged = (SelectedFrameRange != null) && (SelectedFrameRange.Equals(m_LastReportedSelectedFrameRange) == false);
            var hasNewData = newFrame != m_CurrentFrame;
            if (selectionWasCleared || selectionWasChanged || hasNewData)
            {
                SelectedFrameIndexChanged?.Invoke(selectedFrameIndex);
                m_LastReportedSelectedFrameRange = SelectedFrameRange;
            }
        }

        static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .ToUpperInvariant();
        }

        static bool IsSamePath(string path1, string path2)
        {
            return NormalizePath(path1) == NormalizePath(path2);
        }

        internal bool CaptureFileIsOpen(string path)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(m_CurrentLoadedCaptureFile))
                return false;

            return IsSamePath(path, m_CurrentLoadedCaptureFile);
        }

        internal void CaptureRenamed(string sourceFilePath, string targetFilePath)
        {
            if (CaptureFileIsOpen(sourceFilePath))
                CurrentLoadedCaptureFile = targetFilePath;
        }

        long IProfilerWindowController.selectedFrameIndex { get => selectedFrameIndex; set => selectedFrameIndex = value; }
        ProfilerModule IProfilerWindowController.selectedModule { get => selectedModule; set => selectedModule = value; }
        ProfilerModule IProfilerWindowController.GetProfilerModuleByType(Type T) => GetProfilerModuleByType(T);
        ProfilerModule IProfilerWindowController.GetJobsProfilerModule() => jobsProfilerModule;
        void IProfilerWindowController.Repaint() => Repaint();


        event Action<int, bool> IProfilerWindowController.currentFrameChanged
        {
            add { currentFrameChanged += value; }
            remove { currentFrameChanged -= value; }
        }

        event Action IProfilerWindowController.frameDataViewAboutToBeDisposed
        {
            add { frameDataViewAboutToBeDisposed += value; }
            remove { frameDataViewAboutToBeDisposed -= value; }
        }

        void IProfilerWindowController.SetClearOnPlay(bool enabled) => SetClearOnPlay(enabled);
        bool IProfilerWindowController.GetClearOnPlay() => GetClearOnPlay();
        HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(string groupName, string threadName, ulong threadId, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            => GetFrameDataView(groupName, threadName, threadId, viewMode, profilerSortColumn, sortAscending);
        HierarchyFrameDataView IProfilerWindowController.GetFrameDataView(int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int profilerSortColumn, bool sortAscending)
            => GetFrameDataView(threadIndex, viewMode, profilerSortColumn, sortAscending);
        bool IProfilerWindowController.IsRecording() => IsRecording();
        bool IProfilerWindowController.ProfilerWindowOverheadIsAffectingProfilingRecordingData() => ProfilerWindowOverheadIsAffectingProfilingRecordingData();
        string IProfilerWindowController.ConnectedTargetName { get => ConnectedTargetName; }
        bool IProfilerWindowController.ConnectedToEditor { get => ConnectedToEditor; }

        bool IProfilerWindowController.CpuProfilerAssistantSupported => m_CpuProfilerAssistantController.Supported;

        void IProfilerWindowController.RequestCpuProfilerAssistance(Rect screenRect, CpuProfilerAssistantController.CpuProfilerContext attachment, string request)
        {
            m_CpuProfilerAssistantController.LaunchCpuProfilerAssistant(screenRect, attachment, request);
        }

        ProfilerProperty IProfilerWindowController.CreateProperty() => CreateProperty();
        ProfilerProperty IProfilerWindowController.CreateProperty(int sortType) => CreateProperty(sortType);
        void IProfilerWindowController.CloseModule(ProfilerModule module) => CloseModule(module);

        // This type is kept to avoid "Missing Type" exceptions when deserializing the window layout (a bug). Delete this type once fix for this (https://fogbugz.unity3d.com/f/cases/1273439/) has landed.
        [Serializable] internal class ProfilerWindowControllerProxy {}
    }
}
