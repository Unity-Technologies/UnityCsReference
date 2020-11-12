// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.IMGUI.Controls;
using Unity.Profiling;
using UnityEditor.MPE;

namespace UnityEditorInternal.Profiling
{
    interface IProfilerSampleNameProvider
    {
        string GetItemName(HierarchyFrameDataView frameData, int itemId);
        string GetMarkerName(HierarchyFrameDataView frameData, int markerId);
        string GetItemName(RawFrameDataView frameData, int itemId);
    }

    [Serializable]
    internal abstract class CPUOrGPUProfilerModule : ProfilerModuleBase, IProfilerSampleNameProvider
    {
        [SerializeField]
        protected ProfilerViewType m_ViewType = ProfilerViewType.Timeline;

        // internal because it is used by performance tests
        [SerializeField]
        internal bool updateViewLive;

        [SerializeField]
        int m_CurrentFrameIndex = FrameDataView.invalidOrCurrentFrameIndex;

        protected int CurrentFrameIndex
        {
            get
            {
                return m_CurrentFrameIndex;
            }
            set
            {
                if (m_CurrentFrameIndex != value)
                {
                    m_CurrentFrameIndex = value;
                    FrameChanged(value);
                }
            }
        }

        protected CPUOrGPUProfilerModule(IProfilerWindowController profilerWindow, string name, string iconName) : base(profilerWindow, name, iconName, Chart.ChartType.StackedFill) {}

        protected bool fetchData
        {
            get { return !(m_ProfilerWindow == null || (m_ProfilerWindow.IsRecording() && (ProfilerDriver.IsConnectionEditor()))) || updateViewLive; }
        }

        protected const string k_MainThreadName = "Main Thread";

        const string k_ViewTypeSettingsKey = "ViewType";
        const string k_HierarchyViewSettingsKeyPrefix = "HierarchyView.";
        protected abstract string SettingsKeyPrefix { get; }
        string ViewTypeSettingsKey { get { return SettingsKeyPrefix + k_ViewTypeSettingsKey; } }
        string HierarchyViewSettingsKeyPrefix { get { return SettingsKeyPrefix + k_HierarchyViewSettingsKeyPrefix; } }

        string ProfilerViewFilteringOptionsKey => SettingsKeyPrefix + nameof(m_ProfilerViewFilteringOptions);

        protected abstract ProfilerViewType DefaultViewTypeSetting { get; }

        [SerializeField]
        SampleSelection m_selection = SampleSelection.InvalidSampleSelection;
        internal SampleSelection Selection
        {
            get { return m_selection ?? SampleSelection.InvalidSampleSelection;  }
            private set
            {
                // revoke frameIndex guarantee on old selection before we loose control over it
                Selection.frameIndexIsSafe = false;
                m_selection = value;
                m_HierarchyOverruledThreadFromSelection = false;
            }
        }

        // anything that resets the selection, resets this override
        // this is here so that a user can purposefully change aways from a thread with a selection in it and go through other frames without being reset to the thread of the selection
        [SerializeField]
        bool m_HierarchyOverruledThreadFromSelection = false;
        // err on the side of caution, don't serialize this.
        // This bool is only used for a performance optimization and precise selection of one particular instance of a sample when switching views on the same frame.
        [NonSerialized]
        protected bool m_FrameIndexOfSelectionGuaranteedToBeValid = false;

        [Flags]
        public enum ProfilerViewFilteringOptions
        {
            None = 0,
            CollapseEditorBoundarySamples = 1 << 0, // Session based override, default to off
            ShowFullScriptingMethodNames = 1 << 1,
            ShowExecutionFlow = 1 << 2,
        }

        static readonly GUIContent[] k_ProfilerViewFilteringOptions =
        {
            EditorGUIUtility.TrTextContent("Collapse EditorOnly Samples", "Samples that are only created due to profiling the editor are collapsed by default, renamed to EditorOnly [<FunctionName>] and any GC Alloc incurred by them will not be accumulated."),
            EditorGUIUtility.TrTextContent("Show Full Scripting Method Names", "Display fully qualified method names including assembly name and namespace."),
            EditorGUIUtility.TrTextContent("Show Flow Events", "Visualize job scheduling and execution."),
        };

        [SerializeField]
        int m_ProfilerViewFilteringOptions = (int)ProfilerViewFilteringOptions.CollapseEditorBoundarySamples;

        [SerializeField]
        protected ProfilerFrameDataHierarchyView m_FrameDataHierarchyView;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal ProfilerFrameDataHierarchyView FrameDataHierarchyView => m_FrameDataHierarchyView;

        internal ProfilerViewFilteringOptions ViewOptions => (ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests
        internal ProfilerViewType ViewType
        {
            get { return m_ViewType; }
            set { CPUOrGPUViewTypeChanged(value);}
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (m_FrameDataHierarchyView == null)
                m_FrameDataHierarchyView = new ProfilerFrameDataHierarchyView(HierarchyViewSettingsKeyPrefix);
            m_FrameDataHierarchyView.OnEnable(this, m_ProfilerWindow, false);

            // safety guarding against event registration leaks due to an imbalance of OnEnable/OnDisable Calls, by deregistering first
            m_FrameDataHierarchyView.viewTypeChanged -= CPUOrGPUViewTypeChanged;
            m_FrameDataHierarchyView.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_FrameDataHierarchyView.selectionChanged -= SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            m_FrameDataHierarchyView.selectionChanged += SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            m_FrameDataHierarchyView.userChangedThread -= ThreadSelectionInHierarchyViewChanged;
            m_FrameDataHierarchyView.userChangedThread += ThreadSelectionInHierarchyViewChanged;
            ProfilerDriver.profileLoaded -= ProfileLoaded;
            ProfilerDriver.profileLoaded += ProfileLoaded;

            m_ViewType = (ProfilerViewType)EditorPrefs.GetInt(ViewTypeSettingsKey, (int)DefaultViewTypeSetting);
            m_ProfilerViewFilteringOptions = SessionState.GetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);

            TryRestoringSelection();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            TryRestoringSelection();
        }

        public override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(ViewTypeSettingsKey, (int)m_ViewType);
            SessionState.SetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);
            m_FrameDataHierarchyView?.SaveViewSettings();
        }

        public override void OnDisable()
        {
            SaveViewSettings();
            base.OnDisable();
            m_FrameDataHierarchyView?.OnDisable();
            if (m_FrameDataHierarchyView != null)
            {
                m_FrameDataHierarchyView.viewTypeChanged -= CPUOrGPUViewTypeChanged;
                m_FrameDataHierarchyView.selectionChanged -= SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView;
            }

            // In Standalone Profiler, ProfilerDriver (or rather profiling.s_ProfilerSessionInstance) is dead during shutdown. Clearing the Property Path would therefore crash.
            // So ... lets not do that.
            // Also, Application.quitting is never fired in UMPE mode so we have no way of knowing if Standalone Profiler is qutting or getting Disabled for other reasons.
            // Cleaning the selected Property Path is kinda secondary anyways so to avoid crashes in normal usage or native tests, let's just not clear the path out in UMPE modes.
            if (ProcessService.level == ProcessLevel.Main)
                ClearSelectedPropertyPath();

            ProfilerDriver.profileLoaded -= ProfileLoaded;
            Clear();
        }

        public override void DrawToolbar(Rect position)
        {
            // Hierarchy view still needs to be broken apart into Toolbar and View.
        }

        public void DrawOptionsMenuPopup()
        {
            var position = GUILayoutUtility.GetRect(ProfilerWindow.Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(position, ProfilerWindow.Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                var pm = new GenericMenu();
                for (var i = 0; i < k_ProfilerViewFilteringOptions.Length; i++)
                {
                    var option = (ProfilerViewFilteringOptions)(1 << i);
                    if (ViewType == ProfilerViewType.Timeline && option == ProfilerViewFilteringOptions.CollapseEditorBoundarySamples)
                        continue;

                    if (option == ProfilerViewFilteringOptions.ShowExecutionFlow && ViewType != ProfilerViewType.Timeline)
                        continue;

                    pm.AddItem(k_ProfilerViewFilteringOptions[i], OptionEnabled(option), () => ToggleOption(option));
                }
                pm.Popup(position, -1);
            }
        }

        bool OptionEnabled(ProfilerViewFilteringOptions option)
        {
            return (option & (ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions) != ProfilerViewFilteringOptions.None;
        }

        internal virtual void SetOption(ProfilerViewFilteringOptions option, bool on)
        {
            if (on)
                m_ProfilerViewFilteringOptions = (int)((ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions | option);
            else
                m_ProfilerViewFilteringOptions = (int)((ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions & ~option);

            SessionState.SetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);
            m_FrameDataHierarchyView.Clear();
        }

        protected virtual void ToggleOption(ProfilerViewFilteringOptions option)
        {
            SetOption(option, !OptionEnabled(option));
        }

        public override void DrawDetailsView(Rect position)
        {
            CurrentFrameIndex = m_ProfilerWindow.GetActiveVisibleFrameIndex();
            m_FrameDataHierarchyView.DoGUI(fetchData ? GetFrameDataView() : null, fetchData, ref updateViewLive, m_ViewType);
        }

        HierarchyFrameDataView GetFrameDataView()
        {
            var viewMode = HierarchyFrameDataView.ViewModes.Default;
            if (m_ViewType == ProfilerViewType.Hierarchy)
                viewMode |= HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            return m_ProfilerWindow.GetFrameDataView(m_FrameDataHierarchyView.groupName, m_FrameDataHierarchyView.threadName, m_FrameDataHierarchyView.threadId, viewMode | GetFilteringMode(), m_FrameDataHierarchyView.sortedProfilerColumn, m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }

        HierarchyFrameDataView GetFrameDataView(string threadGroupName, string threadName, ulong threadId)
        {
            var viewMode = HierarchyFrameDataView.ViewModes.Default;
            if (m_ViewType == ProfilerViewType.Hierarchy)
                viewMode |= HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            return m_ProfilerWindow.GetFrameDataView(threadGroupName, threadName, threadId, viewMode | GetFilteringMode(), m_FrameDataHierarchyView.sortedProfilerColumn, m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }

        HierarchyFrameDataView GetFrameDataView(int threadIndex)
        {
            var viewMode = HierarchyFrameDataView.ViewModes.Default;
            if (m_ViewType == ProfilerViewType.Hierarchy)
                viewMode |= HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            return m_ProfilerWindow.GetFrameDataView(threadIndex, viewMode | GetFilteringMode(), m_FrameDataHierarchyView.sortedProfilerColumn, m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }

        protected virtual HierarchyFrameDataView.ViewModes GetFilteringMode()
        {
            return HierarchyFrameDataView.ViewModes.Default;
        }

        protected void CPUOrGPUViewTypeChanged(ProfilerViewType viewtype)
        {
            if (m_ViewType == viewtype)
                return;
            m_ViewType = viewtype;

            ProfilerWindowAnalytics.AddNewView(m_ViewType == ProfilerViewType.Timeline
                ? ProfilerWindowAnalytics.profilerCPUModuleTimeline
                : ProfilerWindowAnalytics.profilerCPUModuleHierarchy);

            // reset the hierarchy overruling if the user leaves the Hierarchy space
            // otherwise, switching back and forth between hierarchy views and Timeline feels inconsistent once you overruled the thread selection
            // basically, the override is in effect as long as the user sees the thread selection drop down, once that's gone, so is the override. (out of sight, out of mind)
            if (viewtype == ProfilerViewType.Timeline)
                m_HierarchyOverruledThreadFromSelection = false;
            ApplySelection(true, true);
        }

        void ThreadSelectionInHierarchyViewChanged(string threadGroupName, string threadName)
        {
            m_HierarchyOverruledThreadFromSelection = true;
        }

        void FrameChanged(int frameIndex)
        {
            if (Selection.valid)
            {
                ApplySelection(false, !m_ProfilerWindow.IsRecording());
            }
        }

        void ProfileLoaded()
        {
            Selection.frameIndexIsSafe = false;
            Clear();
            TryRestoringSelection();
        }

        static readonly ProfilerMarker k_SetSelectionIntegrityCheckMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(CPUOrGPUProfilerModule.SetSelection)} Integrity Check");
        static readonly ProfilerMarker k_SetSelectionApplyMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(CPUOrGPUProfilerModule.SetSelection)} Apply Selection");


        int IntegrityCheckFrameAndThreadDataOfSelection(long frameIndex, string threadGroupName, string threadName, ref ulong threadId)
        {
            if (string.IsNullOrEmpty(threadName))
                throw new ArgumentException($"{nameof(threadName)} can't be null or empty.");

            if (ProfilerDriver.firstFrameIndex == FrameDataView.invalidOrCurrentFrameIndex)
                throw new Exception("No frame data is loaded, so there's no data to select from.");

            if (frameIndex > ProfilerDriver.lastFrameIndex || frameIndex < ProfilerDriver.firstFrameIndex)
                throw new ArgumentOutOfRangeException(nameof(frameIndex));

            var threadIndex = FrameDataView.invalidThreadIndex;
            using (var frameView = new ProfilerFrameDataIterator())
            {
                var threadCount = frameView.GetThreadCount((int)frameIndex);
                if (threadGroupName == null)
                    threadGroupName = string.Empty; // simplify this to empty
                threadIndex = SampleSelection.GetThreadIndex((int)frameIndex, threadGroupName, threadName, threadId);
                if (threadIndex < 0 || threadIndex >= threadCount)
                    throw new ArgumentException($"A Thread named: \"{threadName}\" in group \"{threadGroupName}\" could not be found in frame {frameIndex}");
                using (var frameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, threadIndex))
                {
                    if (threadId != FrameDataView.invalidThreadId && frameData.threadId != threadId)
                        throw new ArgumentException($"A Thread named: \"{threadName}\" in group \"{threadGroupName}\" was found in frame {frameIndex}, but its thread id {frameData.threadId} did not match the provided {threadId}");
                    else
                        threadId = frameData.threadId;
                }
            }
            return threadIndex;
        }

        // selects first occurence of this sample in the given frame and thread (and optionally given the markerNamePath leading up to it or a (grand, (grand)...) parent of it)
        public bool SetSelection(int frameIndex, string threadGroupName, string threadName, string sampleName, string markerNamePath = null, ulong threadId = FrameDataView.invalidThreadId)
        {
            SampleSelection selection;
            List<int> markerIdPath;
            using (k_SetSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (string.IsNullOrEmpty(sampleName))
                    throw new ArgumentException($"{nameof(sampleName)} can't be null or empty. Hint: To clear a selection, use {nameof(ClearSelection)} instead.");


                var threadIndex = IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                int selectedSampleRawIndex = FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, sampleName, out markerIdPath, markerNamePath);

                selection = new SampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
                if (!selection.valid)
                    return false;
            }
            using (k_SetSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        // selects first occurence of this sample in the given frame and thread and markerId path leading up to it or a (grand, (grand)...) parent of it
        public bool SetSelection(int frameIndex, string threadGroupName, string threadName, int sampleMarkerId, List<int> markerIdPath = null, ulong threadId = FrameDataView.invalidThreadId)
        {
            SampleSelection selection;
            using (k_SetSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (sampleMarkerId == FrameDataView.invalidMarkerId)
                    throw new ArgumentException($"{nameof(sampleMarkerId)} can't invalid ({FrameDataView.invalidMarkerId}). Hint: To clear a selection, use {nameof(ClearSelection)} instead.");

                var threadIndex = IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                string sampleName = null;
                int selectedSampleRawIndex = FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, sampleMarkerId);

                selection = new SampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
                if (!selection.valid)
                    return false;
            }
            using (k_SetSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        /// <summary>
        /// Search for a sample fitting the '/' seperated path to it and select it
        /// </summary>
        /// <param name="markerNameOrMarkerNamePath">'/' seperated path to the marker </param>
        /// <param name="frameIndex"> The frame to make the selection in, or -1 to select in currently active frame. </param>
        /// <param name="threadIndex"> The index of the thread to find the sample in. </param>
        /// <returns></returns>
        public bool SetSelection(string markerNameOrMarkerNamePath, int frameIndex = FrameDataView.invalidOrCurrentFrameIndex, string threadGroupName = null, string threadName = k_MainThreadName, ulong threadId = FrameDataView.invalidThreadId)
        {
            SampleSelection selection;
            List<int> markerIdPath;
            using (k_SetSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (string.IsNullOrEmpty(markerNameOrMarkerNamePath))
                    throw new ArgumentException($"{nameof(markerNameOrMarkerNamePath)} can't be null or empty. Hint: To clear a selection, use {nameof(ClearSelection)} instead.");

                if (frameIndex == FrameDataView.invalidOrCurrentFrameIndex)
                {
                    frameIndex = GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection();
                }

                var threadIndex = IntegrityCheckFrameAndThreadDataOfSelection(frameIndex, threadGroupName, threadName, ref threadId);

                int lastSlashIndex = markerNameOrMarkerNamePath.LastIndexOf('/');
                string sampleName = lastSlashIndex == -1 ? markerNameOrMarkerNamePath : markerNameOrMarkerNamePath.Substring(lastSlashIndex + 1, markerNameOrMarkerNamePath.Length - (lastSlashIndex + 1));

                if (lastSlashIndex == -1)// no path provided? just find the first sample
                    markerNameOrMarkerNamePath = null;

                int selectedSampleRawIndex = FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, 0, sampleName, out markerIdPath, markerNameOrMarkerNamePath);

                selection = new SampleSelection(frameIndex, threadGroupName, threadName, threadId, selectedSampleRawIndex, sampleName);
                if (!selection.valid)
                    return false;
            }
            using (k_SetSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        public bool SetSelection(SampleSelection selection)
        {
            var markerIdPath = new List<int>();
            using (k_SetSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (!selection.valid)
                    throw new ArgumentException($"{nameof(selection)} can't be invalid. To clear a selection, use {nameof(ClearSelection)} instead.");

                // Since SetSelection is going to validate the the frame index, it is fine to use the unsafeFrameIndex and set selection.frameIndexIsSafe once everything is checked
                var threadId = selection.threadId;
                var threadIndex = IntegrityCheckFrameAndThreadDataOfSelection(selection.unsafeFrameIndex, selection.threadGroupName, selection.threadName, ref threadId);

                if (threadId != selection.threadId)
                    throw new ArgumentException($"The {nameof(selection)}.{nameof(selection.threadId)} of {selection.threadId} does not match to a fitting thread in frame {selection.unsafeFrameIndex}.");

                if (selection.rawSampleIndices != null && selection.rawSampleIndices.Count > 1)
                {
                    // multiple rawSampleIndices are currently only allowed if they all correspond to one item in Hierarchy view
                    using (var frameData = new HierarchyFrameDataView((int)selection.unsafeFrameIndex,
                        threadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                        m_FrameDataHierarchyView.sortedProfilerColumn,
                        m_FrameDataHierarchyView.sortedProfilerColumnAscending))
                    {
                        var itemId = m_FrameDataHierarchyView.treeView.GetItemIDFromRawFrameDataViewIndex(frameData, selection.rawSampleIndex, null);
                        var rawIds = new List<int>();
                        frameData.GetItemRawFrameDataViewIndices(itemId, rawIds);
                        for (int i = 1; i < selection.rawSampleIndices.Count; i++)
                        {
                            var found = false;
                            for (int j = 0; j < rawIds.Count; j++)
                            {
                                if (selection.rawSampleIndices[i] < 0)
                                {
                                    throw new ArgumentException($"The passed raw id {selection.rawSampleIndices[i]} is invalid.");
                                }
                                if (selection.rawSampleIndices[i] == rawIds[j])
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                throw new ArgumentException($"The passed raw id {selection.rawSampleIndices[i]} does not belong to the same Hierarchy Item as {selection.rawSampleIndices[0]}");
                            }
                        }
                    }
                }

                using (var frameData = new RawFrameDataView((int)selection.unsafeFrameIndex, threadIndex))
                {
                    var name = string.Empty;
                    var foundSampleIndex = ProfilerTimelineGUI.GetItemMarkerIdPath(frameData, this, selection.rawSampleIndex, ref name, ref markerIdPath);
                    if (foundSampleIndex != selection.rawSampleIndex)
                        throw new ArgumentException($"Provided {nameof(selection.rawSampleIndex)}: {selection.rawSampleIndex} was not found.");
                    // don't trust the name and marker id data, override with found data.
                    // Reason: Marker Ids could change and the sample name could be altered by the sample name formatter (e.g. to be/not be the fully qualified method name)
                    selection.GenerateMarkerNamePath(frameData, name, markerIdPath);
                }
            }
            using (k_SetSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                return true;
            }
        }

        public void ClearSelection()
        {
            SetSelectionWithoutIntegrityChecks(SampleSelection.InvalidSampleSelection, null);
            ApplySelection(false, false);
        }

        public SampleSelection GetSelection()
        {
            return Selection;
        }

        // Used for testing
        internal virtual void GetSelectedSampleIdsForCurrentFrameAndView(ref List<int> ids)
        {
            ids.Clear();
            if (Selection.valid)
            {
                ids.AddRange(m_FrameDataHierarchyView.treeView.GetSelection());
            }
        }

        // Only call this for SetSelection code that runs before SetSelectionWithoutIntegrityChecks sets the activel visible Frame index
        // We don't want to desync from ProfilerWindow.m_LastFrameFromTick unless we're about to set it to something else and forcing a repaint anyways.
        // Most OnGUI scope code in this class should be able to rely on CurrentFrameIndex instead.
        int GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection()
        {
            if (m_ProfilerWindow == null)
                return FrameDataView.invalidOrCurrentFrameIndex;
            var currentFrame = m_ProfilerWindow.GetActiveVisibleFrameIndex();
            return currentFrame == FrameDataView.invalidOrCurrentFrameIndex ? ProfilerDriver.lastFrameIndex : currentFrame;
        }

        protected void SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView(SampleSelection selection)
        {
            // trust the internal views to provide a correct frame index
            selection.frameIndexIsSafe = true;
            SetSelectionWithoutIntegrityChecks(selection, null);
        }

        protected void SetSelectionWithoutIntegrityChecks(SampleSelection selection, List<int> markerIdPath)
        {
            if (selection.valid)
            {
                if (selection.frameIndex != m_ProfilerWindow.GetActiveVisibleFrameIndex())
                    m_ProfilerWindow.SetActiveVisibleFrameIndex(selection.frameIndex != FrameDataView.invalidOrCurrentFrameIndex ? (int)selection.frameIndex : ProfilerDriver.lastFrameIndex);
                if (string.IsNullOrEmpty(selection.legacyMarkerPath))
                {
                    var frameDataView = GetFrameDataView(selection.threadGroupName, selection.threadName, selection.threadId);
                    if (frameDataView == null || !frameDataView.valid)
                        return;
                    selection.GenerateMarkerNamePath(frameDataView, markerIdPath);
                }
                Selection = selection;
                SetSelectedPropertyPath(selection.legacyMarkerPath, Selection.threadName);
            }
            else
            {
                Selection = SampleSelection.InvalidSampleSelection;
                ClearSelectedPropertyPath();
            }
        }

        protected virtual void SetSelectedPropertyPath(string path, string threadName)
        {
            // Only CPU view currently supports Chart filtering by property path
        }

        protected virtual void ClearSelectedPropertyPath()
        {
            // Only CPU view currently supports Chart filtering by property path
        }

        void TryRestoringSelection()
        {
            if (Selection.valid)
            {
                if (ProfilerDriver.firstFrameIndex >= 0 && ProfilerDriver.lastFrameIndex >= 0)
                {
                    ApplySelection(true, true);
                }
                SetSelectedPropertyPath(Selection.legacyMarkerPath, Selection.threadName);
            }
        }

        protected static readonly ProfilerMarker k_ApplyValidSelectionMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(ApplySelection)}");
        protected static readonly ProfilerMarker k_ApplySelectionClearMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(ApplySelection)} Clear");
        virtual protected void ApplySelection(bool viewChanged, bool frameSelection)
        {
            if (ViewType == ProfilerViewType.Hierarchy || ViewType == ProfilerViewType.RawHierarchy)
            {
                if (Selection.valid)
                {
                    using (k_ApplyValidSelectionMarker.Auto())
                    {
                        var currentFrame = m_ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (Selection.frameIndexIsSafe && Selection.frameIndex == currentFrame)
                        {
                            var frameDataView = m_HierarchyOverruledThreadFromSelection ? GetFrameDataView() : GetFrameDataView(Selection.threadGroupName, Selection.threadName, Selection.threadId);
                            // avoid Selection Migration happening twice during SetFrameDataView by clearing the old one out first
                            m_FrameDataHierarchyView.ClearSelection();
                            m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
                            if (!frameDataView.valid)
                                return;

                            var treeViewID = ProfilerFrameDataHierarchyView.invalidTreeViewId;
                            // GetItemIDFromRawFrameDataViewIndex is a bit expensive so only use that if showing the Raw view (where the raw id is relevant)
                            // or when the cheaper option (setting selection via MarkerIdPath) isn't available
                            if (ViewType == ProfilerViewType.RawHierarchy || (Selection.markerPathDepth <= 0))
                            {
                                treeViewID = m_FrameDataHierarchyView.treeView.GetItemIDFromRawFrameDataViewIndex(frameDataView, Selection.rawSampleIndex, Selection.markerIdPath);
                            }
                            if (treeViewID == ProfilerFrameDataHierarchyView.invalidTreeViewId)
                            {
                                if (Selection.markerPathDepth > 0)
                                {
                                    m_FrameDataHierarchyView.SetSelection(Selection, viewChanged || frameSelection);
                                }
                            }
                            else
                            {
                                var ids = new List<int>()
                                {
                                    treeViewID
                                };
                                m_FrameDataHierarchyView.treeView.SetSelection(ids, TreeViewSelectionOptions.RevealAndFrame);
                            }
                        }
                        else if (currentFrame >= 0 && Selection.markerPathDepth > 0)
                        {
                            var frameDataView = m_HierarchyOverruledThreadFromSelection ? GetFrameDataView() : GetFrameDataView(Selection.threadGroupName, Selection.threadName, Selection.threadId);
                            if (!frameDataView.valid)
                                return;
                            // avoid Selection Migration happening twice during SetFrameDataView by clearing the old one out first
                            m_FrameDataHierarchyView.ClearSelection();
                            m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
                            m_FrameDataHierarchyView.SetSelection(Selection, (!m_ProfilerWindow.IsRecording() || !ProfilerDriver.IsConnectionEditor()) && (viewChanged || frameSelection));
                        }
                        // else: the selection was not in the shown frame AND there was no other frame to select it in or the Selection contains no marker path.
                        // So either there is no data to apply the selection to, or the selection isn't one that can be applied to another frame because there is no path
                        // either way, it is save to not Apply the selection.
                    }
                }
                else
                {
                    using (k_ApplySelectionClearMarker.Auto())
                    {
                        m_FrameDataHierarchyView.ClearSelection();
                    }
                }
            }
        }

        protected int GetThreadIndexInCurrentFrameToApplySelectionFromAnotherFrame(SampleSelection selection)
        {
            var currentFrame = m_ProfilerWindow.GetActiveVisibleFrameIndex();
            return selection.GetThreadIndex(currentFrame);
        }

        protected virtual int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, string sampleName, out List<int> markerIdPath, string markerNamePath = null)
        {
            if (ViewType == ProfilerViewType.RawHierarchy || ViewType == ProfilerViewType.Hierarchy)
            {
                markerIdPath = null;
                return FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, markerNamePath, FrameDataView.invalidMarkerId);
            }

            markerIdPath = new List<int>();
            return RawFrameDataView.invalidSampleIndex;
        }

        protected virtual int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, int sampleMarkerId)
        {
            if (ViewType == ProfilerViewType.RawHierarchy || ViewType == ProfilerViewType.Hierarchy)
            {
                Debug.Assert(sampleMarkerId != RawFrameDataView.invalidSampleIndex);
                return FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, null, sampleMarkerId);
            }
            return RawFrameDataView.invalidSampleIndex;
        }

        struct HierarchySampleIterationInfo { public int sampleId; public int sampleDepth; }

        int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, string markerNamePath, int sampleMarkerId)
        {
            if (ViewType == ProfilerViewType.RawHierarchy || ViewType == ProfilerViewType.Hierarchy)
            {
                if (frameIndex < 0)
                    // If the last frame was supposed to be looked at, and this happens during a SetSelection call, it should have been adjusted to a valid frame index there.
                    // This method here should not cause the Profiler Window to toggle on the "Current Frame" toggle.
                    throw new ArgumentOutOfRangeException("frameIndex", "frameIndex can't be below 0");

                m_ProfilerWindow.SetActiveVisibleFrameIndex(frameIndex);
                var frameData = GetFrameDataView(threadIndex);

                var sampleIdPath = new List<int>();
                var children = new List<int>();
                frameData.GetItemChildren(frameData.GetRootItemID(), children);
                var yetToVisit = new Stack<HierarchySampleIterationInfo>();

                var rawIds = new List<int>();
                int foundSampleIndex = RawFrameDataView.invalidSampleIndex;

                if (sampleMarkerId == FrameDataView.invalidMarkerId)
                    sampleMarkerId = frameData.GetMarkerId(sampleName);

                if (markerIdPath != null && markerIdPath.Count > 0)
                {
                    int enclosingScopeId = FindNextMatchingSampleIdInScope(frameData, null, sampleIdPath, children, yetToVisit, false, markerIdPath[sampleIdPath.Count]);
                    while (enclosingScopeId != RawFrameDataView.invalidSampleIndex && sampleIdPath.Count <= markerIdPath.Count)
                    {
                        if (sampleIdPath.Count == markerIdPath.Count)
                        {
                            // lets, for a moment, assume that the searched sample is the last one in the specified path
                            var sampleId = enclosingScopeId;
                            if ((sampleMarkerId != FrameDataView.invalidMarkerId && sampleMarkerId != markerIdPath[sampleIdPath.Count - 1]) ||  (sampleName != null && frameData.GetMarkerName(markerIdPath[sampleIdPath.Count - 1]) != sampleName))
                            {
                                // the searched sample is NOT the same as the last one in the path, so search for it
                                if (sampleMarkerId == FrameDataView.invalidMarkerId)
                                    sampleId = FindNextMatchingSampleIdInScope(frameData, sampleName, sampleIdPath, children, yetToVisit, true);
                                else
                                    sampleId = FindNextMatchingSampleIdInScope(frameData, null, sampleIdPath, children, yetToVisit, true, sampleMarkerId);
                            }
                            if (sampleId != RawFrameDataView.invalidSampleIndex)
                            {
                                foundSampleIndex = sampleId;
                                // add further marker Ids as needed
                                for (int i = markerIdPath.Count; i < sampleIdPath.Count; i++)
                                {
                                    markerIdPath.Add(frameData.GetItemMarkerID(sampleIdPath[i]));
                                }
                                break;
                            }
                            // searched sample wasn't found, continue search one scope higher than the full path
                            while (sampleIdPath.Count >= markerIdPath.Count && sampleIdPath.Count > 0)
                            {
                                sampleIdPath.RemoveAt(sampleIdPath.Count - 1);
                            }
                        }
                        enclosingScopeId = FindNextMatchingSampleIdInScope(frameData, null, sampleIdPath, children, yetToVisit, false, markerIdPath[sampleIdPath.Count]);
                    }
                }
                else if (!string.IsNullOrEmpty(markerNamePath))
                {
                    var path = markerNamePath.Split('/');
                    if (path != null && path.Length > 0)
                    {
                        int enclosingScopeId = FindNextMatchingSampleIdInScope(frameData, path[sampleIdPath.Count], sampleIdPath, children, yetToVisit, false);
                        while (enclosingScopeId != RawFrameDataView.invalidSampleIndex && sampleIdPath.Count <= path.Length)
                        {
                            if (sampleIdPath.Count == path.Length)
                            {
                                // lets, for a moment, assume that the searched sample is the last one in the specified path
                                var sampleId = enclosingScopeId;
                                if (path[sampleIdPath.Count - 1] != sampleName)
                                {
                                    // the searched sample is NOT the same as the last one in the path, so search for it
                                    sampleId = FindNextMatchingSampleIdInScope(frameData, sampleName, sampleIdPath, children, yetToVisit, true);
                                }
                                if (sampleId != RawFrameDataView.invalidSampleIndex)
                                {
                                    foundSampleIndex = sampleId;
                                    break;
                                }
                                // searched sample wasn't found, continue search one scope higher than the full path
                                while (sampleIdPath.Count >= path.Length && sampleIdPath.Count > 0)
                                {
                                    sampleIdPath.RemoveAt(sampleIdPath.Count - 1);
                                }
                            }
                            enclosingScopeId = FindNextMatchingSampleIdInScope(frameData, path[sampleIdPath.Count], sampleIdPath, children, yetToVisit, false);
                        }
                    }
                }
                else
                {
                    if (sampleMarkerId == FrameDataView.invalidMarkerId)
                        foundSampleIndex = FindNextMatchingSampleIdInScope(frameData, sampleName, sampleIdPath, children, yetToVisit, true);
                    else
                        foundSampleIndex = FindNextMatchingSampleIdInScope(frameData, null, sampleIdPath, children, yetToVisit, true, sampleMarkerId);
                }

                if (foundSampleIndex != RawFrameDataView.invalidSampleIndex)
                {
                    if (string.IsNullOrEmpty(sampleName))
                        sampleName = GetItemName(frameData, foundSampleIndex);
                    if (markerIdPath == null)
                        markerIdPath = new List<int>();
                    if (markerIdPath.Count == 0)
                    {
                        SampleSelection.GetCleanMarkerIdsFromSampleIds(frameData, sampleIdPath, markerIdPath);
                    }

                    frameData.GetItemRawFrameDataViewIndices(foundSampleIndex, rawIds);
                    Debug.Assert(rawIds.Count > 0, "Frame data is Invalid");
                    return rawIds[0];
                }
            }
            markerIdPath = new List<int>();
            return RawFrameDataView.invalidSampleIndex;
        }

        int FindNextMatchingSampleIdInScope(HierarchyFrameDataView frameData, string sampleName, List<int> sampleIdPath,
            List<int> children, Stack<HierarchySampleIterationInfo> yetToVisit, bool searchRecursively, int markerId = FrameDataView.invalidMarkerId)
        {
            if (markerId == FrameDataView.invalidMarkerId)
                markerId = frameData.GetMarkerId(sampleName);
            if (children.Count > 0)
            {
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    yetToVisit.Push(new HierarchySampleIterationInfo { sampleId = children[i], sampleDepth = sampleIdPath.Count });
                }
                children.Clear();
            }
            while (yetToVisit.Count > 0)
            {
                var sample = yetToVisit.Pop();
                int higherlevelScopeSampleToReturnTo = RawFrameDataView.invalidSampleIndex;
                while (sample.sampleDepth < sampleIdPath.Count && sampleIdPath.Count > 0)
                {
                    // if this sample came from a higher scope, step backwards on the path.
                    higherlevelScopeSampleToReturnTo = sampleIdPath[sampleIdPath.Count - 1];
                    sampleIdPath.RemoveAt(sampleIdPath.Count - 1);
                }
                if (!searchRecursively && higherlevelScopeSampleToReturnTo >= 0)
                {
                    // the sample scope to check against is no longer the one that was provided to this method, so bail out and get the right one
                    yetToVisit.Push(sample);
                    return higherlevelScopeSampleToReturnTo;
                }

                var isEditorOnlySample = (frameData.GetItemMarkerFlags(sample.sampleId) & MarkerFlags.AvailabilityEditor) != 0;

                bool found = (isEditorOnlySample && (sampleName != null && GetItemName(frameData, sample.sampleId).Contains(sampleName)
                    || sampleName == null && GetItemName(frameData, sample.sampleId).Contains(frameData.GetMarkerName(markerId))))
                    || markerId == FrameDataView.invalidMarkerId && GetItemName(frameData, sample.sampleId) == sampleName
                    || markerId == frameData.GetItemMarkerID(sample.sampleId);
                if (found || searchRecursively)
                {
                    sampleIdPath.Add(sample.sampleId);

                    frameData.GetItemChildren(sample.sampleId, children);
                    for (int i = children.Count - 1; i >= 0; i--)
                    {
                        yetToVisit.Push(new HierarchySampleIterationInfo { sampleId = children[i], sampleDepth = sampleIdPath.Count });
                    }
                    children.Clear();
                    if (found)
                        return sample.sampleId;
                }
            }
            return RawFrameDataView.invalidSampleIndex;
        }

        public override void Clear()
        {
            base.Clear();
            m_CurrentFrameIndex = FrameDataView.invalidOrCurrentFrameIndex;
            m_FrameDataHierarchyView?.Clear();
            m_FrameIndexOfSelectionGuaranteedToBeValid = false;
        }

        public void Repaint()
        {
            m_ProfilerWindow.Repaint();
        }

        static readonly ProfilerMarker k_GetItemNameScriptingSimplificationMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(GetItemName)} Scripting Name Simplification");
        const int k_AnyFullManagedMarker = (int)(MarkerFlags.ScriptInvoke | MarkerFlags.ScriptDeepProfiler);
        public string GetItemName(HierarchyFrameDataView frameData, int itemId)
        {
            var name = frameData.GetItemName(itemId);
            if ((ViewOptions & ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0)
                return name;

            var flags = frameData.GetItemMarkerFlags(itemId);
            if (((int)flags & k_AnyFullManagedMarker) == 0)
                return name;

            var namespaceDelimiterIndex = name.IndexOf(':');
            if (namespaceDelimiterIndex == -1)
                return name;

            // Marker added so we can attribute the GC Alloc and time spend for simplifying the name and have performance tests against this
            // TODO: Use MutableString for this once available.
            using (k_GetItemNameScriptingSimplificationMarker.Auto())
            {
                ++namespaceDelimiterIndex;
                if (namespaceDelimiterIndex < name.Length && name[namespaceDelimiterIndex] == ':')
                    return name.Substring(namespaceDelimiterIndex + 1);

                return name;
            }
        }

        public string GetMarkerName(HierarchyFrameDataView frameData, int markerId)
        {
            var name = frameData.GetMarkerName(markerId);
            if ((ViewOptions & ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0)
                return name;

            var namespaceDelimiterIndex = name.IndexOf(':');
            if (namespaceDelimiterIndex == -1)
                return name;

            // Marker added so we can attribute the GC Alloc and time spend for simplifying the name and have performance tests against this
            // TODO: Use MutableString for this once available.
            using (k_GetItemNameScriptingSimplificationMarker.Auto())
            {
                ++namespaceDelimiterIndex;
                if (namespaceDelimiterIndex < name.Length && name[namespaceDelimiterIndex] == ':')
                    return name.Substring(namespaceDelimiterIndex + 1);

                return name;
            }
        }

        public string GetItemName(RawFrameDataView frameData, int itemId)
        {
            var name = frameData.GetSampleName(itemId);
            if ((ViewOptions & ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0)
                return name;

            var flags = frameData.GetSampleFlags(itemId);
            if (((int)flags & k_AnyFullManagedMarker) == 0)
                return name;

            var namespaceDelimiterIndex = name.IndexOf(':');
            if (namespaceDelimiterIndex == -1)
                return name;

            // Marker added so we can attribute the GC Alloc and time spend for simplifying the name and have performance tests against this
            // TODO: Use MutableString for this once available.
            using (k_GetItemNameScriptingSimplificationMarker.Auto())
            {
                ++namespaceDelimiterIndex;
                if (namespaceDelimiterIndex < name.Length && name[namespaceDelimiterIndex] == ':')
                    return name.Substring(namespaceDelimiterIndex + 1);

                return name;
            }
        }
    }
}
