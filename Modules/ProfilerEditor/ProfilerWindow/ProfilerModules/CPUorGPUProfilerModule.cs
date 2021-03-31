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
using UnityEditorInternal.Profiling;
using UnityEditorInternal;

namespace UnityEditor.Profiling
{
    internal interface IProfilerFrameTimeViewSampleSelectionControllerInternal
    {
        int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, string sampleName, out List<int> markerIdPath, string markerNamePath = null);
        int FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, int sampleMarkerId);
        void SetSelectionWithoutIntegrityChecks(ProfilerTimeSampleSelection selectionToSet, List<int> markerIdPath);
        IProfilerWindowController profilerWindow { get; }
        int GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection();
    }

    public interface IProfilerFrameTimeViewSampleSelectionController
    {
        event Action<IProfilerFrameTimeViewSampleSelectionController, ProfilerTimeSampleSelection> selectionChanged;
        ProfilerTimeSampleSelection selection { get; }
        string sampleNameSearchFilter { get; set; }
        int focusedThreadIndex { get; set; }
        bool SetSelection(ProfilerTimeSampleSelection selection);
        void ClearSelection();
    }
}

namespace UnityEditorInternal.Profiling
{
    interface IProfilerSampleNameProvider
    {
        string GetItemName(HierarchyFrameDataView frameData, int itemId);
        string GetMarkerName(HierarchyFrameDataView frameData, int markerId);
        string GetItemName(RawFrameDataView frameData, int itemId);
    }

    [Serializable]
    // TODO: refactor: rename to CpuOrGpuProfilerModule
    // together with CpuProfilerModule and GpuProfilerModule
    // in a PR that doesn't affect performance so that the sample names can be fixed as well without loosing comparability in Performance tests.
    internal abstract class CPUOrGPUProfilerModule : ProfilerModuleBase, IProfilerSampleNameProvider, IProfilerFrameTimeViewSampleSelectionController, IProfilerFrameTimeViewSampleSelectionControllerInternal
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
        IProfilerWindowController IProfilerFrameTimeViewSampleSelectionControllerInternal.profilerWindow => m_ProfilerWindow;

        protected CPUOrGPUProfilerModule(IProfilerWindowController profilerWindow,  string name, string localizedName, string iconName) : base(profilerWindow, name, localizedName, iconName, Chart.ChartType.StackedFill)
        {
            // check that the selection is still valid and wasn't badly deserialized on Domain Reload
            if (selection != null && (selection.markerPathDepth <= 0 || selection.rawSampleIndices == null))
                m_Selection = null;
        }

        protected bool fetchData
        {
            get { return !(m_ProfilerWindow == null || m_ProfilerWindow.ProfilerWindowOverheadIsAffectingProfilingRecordingData()) || updateViewLive; }
        }

        protected const string k_MainThreadName = "Main Thread";

        const string k_ViewTypeSettingsKey = "ViewType";
        const string k_HierarchyViewSettingsKeyPrefix = "HierarchyView.";

        protected abstract string ModuleName { get; }
        protected abstract string SettingsKeyPrefix { get; }
        string ViewTypeSettingsKey { get { return SettingsKeyPrefix + k_ViewTypeSettingsKey; } }
        string HierarchyViewSettingsKeyPrefix { get { return SettingsKeyPrefix + k_HierarchyViewSettingsKeyPrefix; } }

        string ProfilerViewFilteringOptionsKey => SettingsKeyPrefix + nameof(m_ProfilerViewFilteringOptions);

        protected abstract ProfilerViewType DefaultViewTypeSetting { get; }

        ProfilerTimeSampleSelection m_Selection;
        public ProfilerTimeSampleSelection selection
        {
            get { return m_Selection;  }
            private set
            {
                if (selection != null)
                    // revoke frameIndex guarantee on old selection before we loose control over it
                    selection.frameIndexIsSafe = false;

                m_Selection = value;
                // as soon as any selection is made, the thread focus will now be driven by the selection
                m_HierarchyOverruledThreadFromSelection = false;

                // I'm not sure how this can happen, but it does.
                if (selectionChanged == null)
                    selectionChanged = delegate {};
                selectionChanged(this, value);
            }
        }

        public event Action<IProfilerFrameTimeViewSampleSelectionController, ProfilerTimeSampleSelection> selectionChanged = delegate {};

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

        string m_SampleNameSearchFilter = null;
        public string sampleNameSearchFilter
        {
            get => m_SampleNameSearchFilter;
            set
            {
                m_SampleNameSearchFilter = value;
                if (m_FrameDataHierarchyView != null)
                    m_FrameDataHierarchyView.treeView.searchString = value;
            }
        }

        public int focusedThreadIndex
        {
            // From a user's perspective:
            // When in Timeline view, with an active selection, the thread index in which the selection resides in.
            // Otherwise, whatever thread index (Raw) Hierarchy view is currently showing or will be showing when the view next changes to it.
            //
            // Actually, the shown thread in Hierarchy views is driven by the active selection, unless overruled by m_HierarchyOverruledThreadFromSelection
            // and will otherwise just be what ever the user chose via this API or the thread selection dropdown.
            //
            // The effect from a user's persepective is pretty much the same but m_FrameDataHierarchyView might not yet have been set to reflect this as it does so somewhat lazyily
            // Therefore this API does some checks to establish the effect before it will happen in a way that users shouldn't need to care about.
            get
            {
                // if (multiple threads or a thread group is focused / shown in Hierarchy)
                //      return FrameDataView.invalidThreadIndex;

                var hierarchyThreadIndex = m_FrameDataHierarchyView != null ? m_FrameDataHierarchyView.threadIndex : FrameDataView.invalidThreadIndex;
                if (ViewType != ProfilerViewType.Timeline && m_HierarchyOverruledThreadFromSelection && hierarchyThreadIndex >= 0)
                    return m_FrameDataHierarchyView.threadIndex;

                if (selection != null && m_ProfilerWindow.selectedFrameIndex >= 0)
                    return selection.GetThreadIndex((int)m_ProfilerWindow.selectedFrameIndex);

                return hierarchyThreadIndex;
            }
            set
            {
                if (m_ProfilerWindow.selectedFrameIndex < 0)
                {
                    throw new InvalidOperationException($"Can't set {nameof(focusedThreadIndex)} while it is not showing any frame data {nameof(FrameDataView)}.");
                }

                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", $"The thread index {value} can't be set because it is negative.");

                using (var iter = new ProfilerFrameDataIterator())
                {
                    var threadCount = iter.GetThreadCount((int)m_ProfilerWindow.selectedFrameIndex);
                    if (value >= threadCount)
                        throw new ArgumentOutOfRangeException("value", $"The chosen thread index {value} is out of range of valid thread indices in this frame. Frame index: {m_ProfilerWindow.selectedFrameIndex}, thread count: {threadCount}.");
                    m_HierarchyOverruledThreadFromSelection = true;

                    // Frame the thread. This is independent of the checks below, as it only relates to Timeline view.
                    // For Timeline view it doesn't matter what the status on the Hierarchy view was: setting this value should trigger a one-off framing of the thread.
                    FrameThread(value);

                    // only reload frame data if the thread index to focus is different to the thread index of the currently shown frame data view.
                    if (value != m_FrameDataHierarchyView.threadIndex)
                    {
                        using (var dataView = new RawFrameDataView((int)m_ProfilerWindow.selectedFrameIndex, value))
                        {
                            var frameDataView = GetFrameDataView(dataView.threadGroupName, dataView.threadName, dataView.threadId);

                            // once a valid thread has been chosen, based on the thread index, the thread index should no longer be used to determine which thread to focus on going forward
                            // because the thread index is too unstable frame over frame. The thread group name and name, as well as the thread ID are way more reliable, and will be gotten from m_FrameDataHierarchyView.
                            // if it isn't valid m_FocusedThreadIndex will stay at its set value until it resolves to a valid one druing an OnGUI phase.
                            if (frameDataView == null || !frameDataView.valid)
                                throw new InvalidOperationException($"The provided thread index does not belong to a valid {nameof(FrameDataView)}.");

                            m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
                        }
                    }
                }
            }
        }

        protected virtual void FrameThread(int threadIndex)
        {
        }

        [SerializeField]
        protected ProfilerFrameDataHierarchyView m_FrameDataHierarchyView;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal ProfilerFrameDataHierarchyView FrameDataHierarchyView => m_FrameDataHierarchyView;

        internal ProfilerViewFilteringOptions ViewOptions => (ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests
        internal virtual ProfilerViewType ViewType
        {
            get { return m_ViewType; }
            set { CPUOrGPUViewTypeChanged(value); }
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
            if (!string.IsNullOrEmpty(sampleNameSearchFilter))
                m_FrameDataHierarchyView.treeView.searchString = sampleNameSearchFilter;
            m_FrameDataHierarchyView.searchChanged -= SearchFilterInHierarchyViewChanged;
            m_FrameDataHierarchyView.searchChanged += SearchFilterInHierarchyViewChanged;
            ProfilerDriver.profileLoaded -= ProfileLoaded;
            ProfilerDriver.profileLoaded += ProfileLoaded;
            ProfilerDriver.profileCleared -= ProfileCleared;
            ProfilerDriver.profileCleared += ProfileCleared;

            m_ViewType = (ProfilerViewType)EditorPrefs.GetInt(ViewTypeSettingsKey, (int)DefaultViewTypeSetting);
            m_ProfilerViewFilteringOptions = SessionState.GetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);

            TryRestoringSelection();
        }

        protected override void OnSelected()
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

            ProfilerDriver.profileLoaded -= ProfileLoaded;
            ProfilerDriver.profileCleared -= ProfileCleared;
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
            CurrentFrameIndex = (int)m_ProfilerWindow.selectedFrameIndex;
            m_FrameDataHierarchyView.DoGUI(fetchData ? GetFrameDataView() : null, fetchData, ref updateViewLive, m_ViewType);
        }

        HierarchyFrameDataView GetFrameDataView()
        {
            return GetFrameDataView(m_FrameDataHierarchyView.groupName, m_FrameDataHierarchyView.threadName, m_FrameDataHierarchyView.threadId);
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
            {
                m_HierarchyOverruledThreadFromSelection = false;
            }
            ApplySelection(true, true);
        }

        void ThreadSelectionInHierarchyViewChanged(string threadGroupName, string threadName, int threadIndex)
        {
            var frameDataView = (threadIndex != FrameDataView.invalidThreadIndex) ?
                GetFrameDataView(threadIndex) :
                GetFrameDataView(threadGroupName, threadName, FrameDataView.invalidThreadId);

            m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
            if (frameDataView != null && frameDataView.valid)
            {
                // once a valid thread has been chosen, based on the thread index, the thread index should no longer be used to determine which thread to focus on going forward
                // because the thread index is too unstable frame over frame. The thread group name and name, as well as the thread ID are way more reliable, and will be gotten from m_FrameDataHierarchyView.
                // if it isn't valid m_FocusedThreadIndex will stay at its set value until it resolves to a valid one druing an OnGUI phase.
                m_HierarchyOverruledThreadFromSelection = true;
                return;
            }
            // fail save, we should actually never get here but even if, fail silently and gracefully.
            m_HierarchyOverruledThreadFromSelection = false;
        }

        void SearchFilterInHierarchyViewChanged(string sampleNameSearchFiler)
        {
            m_SampleNameSearchFilter = sampleNameSearchFiler;
        }

        void FrameChanged(int frameIndex)
        {
            if (selection != null)
            {
                ApplySelection(false, fetchData);
            }
        }

        void ProfileLoaded()
        {
            if (selection != null)
                selection.frameIndexIsSafe = false;
            Clear();
            TryRestoringSelection();
        }

        void ProfileCleared()
        {
            if (selection != null)
                selection.frameIndexIsSafe = false;
            Clear();
        }

        internal static readonly ProfilerMarker setSelectionIntegrityCheckMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(CPUOrGPUProfilerModule.SetSelection)} Integrity Check");
        internal static readonly ProfilerMarker setSelectionApplyMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(CPUOrGPUProfilerModule.SetSelection)} Apply Selection");

        internal static int IntegrityCheckFrameAndThreadDataOfSelection(long frameIndex, string threadGroupName, string threadName, ref ulong threadId)
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
                    threadGroupName = string.Empty; // simplify null to empty
                threadIndex = ProfilerTimeSampleSelection.GetThreadIndex((int)frameIndex, threadGroupName, threadName, threadId);
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

        public bool SetSelection(ProfilerTimeSampleSelection selection)
        {
            var markerIdPath = new List<int>();
            using (setSelectionIntegrityCheckMarker.Auto())
            {
                // this could've come from anywhere, check the inputs first
                if (selection == null)
                    throw new ArgumentException($"{nameof(selection)} can't be invalid. To clear a selection, use {nameof(ClearSelection)} instead.");

                // Since SetSelection is going to validate the the frame index, it is fine to use the unsafeFrameIndex and set selection.frameIndexIsSafe once everything is checked
                var threadId = selection.threadId;
                var threadIndex = IntegrityCheckFrameAndThreadDataOfSelection(selection.frameIndex, selection.threadGroupName, selection.threadName, ref threadId);

                if (threadId != selection.threadId)
                    throw new ArgumentException($"The {nameof(selection)}.{nameof(selection.threadId)} of {selection.threadId} does not match to a fitting thread in frame {selection.frameIndex}.");

                if (selection.rawSampleIndices != null && selection.rawSampleIndices.Count > 1)
                {
                    // multiple rawSampleIndices are currently only allowed if they all correspond to one item in Hierarchy view
                    using (var frameData = new HierarchyFrameDataView((int)selection.frameIndex,
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

                using (var frameData = new RawFrameDataView((int)selection.frameIndex, threadIndex))
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
            using (setSelectionApplyMarker.Auto())
            {
                // looks good, apply
                selection.frameIndexIsSafe = true;
                SetSelectionWithoutIntegrityChecks(selection, markerIdPath);
                ApplySelection(false, true);
                return true;
            }
        }

        public void ClearSelection()
        {
            SetSelectionWithoutIntegrityChecks(null, null);
            ApplySelection(false, false);
        }

        public ProfilerTimeSampleSelection GetSelection()
        {
            return selection;
        }

        // Used for testing
        internal virtual void GetSelectedSampleIdsForCurrentFrameAndView(ref List<int> ids)
        {
            ids.Clear();
            if (selection != null)
            {
                ids.AddRange(m_FrameDataHierarchyView.treeView.GetSelection());
            }
        }

        // Only call this for SetSelection code that runs before SetSelectionWithoutIntegrityChecks sets the active visible Frame index
        // We don't want to desync from ProfilerWindow.m_LastFrameFromTick unless we're about to set it to something else and forcing a repaint anyways.
        // Most OnGUI scope code in this class should be able to rely on CurrentFrameIndex instead.
        int IProfilerFrameTimeViewSampleSelectionControllerInternal.GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection() => GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection();
        int GetActiveVisibleFrameIndexOrLatestFrameForSettingTheSelection()
        {
            if (m_ProfilerWindow == null)
                return FrameDataView.invalidOrCurrentFrameIndex;
            var currentFrame = (int)m_ProfilerWindow.selectedFrameIndex;
            return currentFrame == FrameDataView.invalidOrCurrentFrameIndex ? ProfilerDriver.lastFrameIndex : currentFrame;
        }

        protected void SetSelectionWithoutIntegrityChecksOnSelectionChangeInDetailedView(ProfilerTimeSampleSelection selection)
        {
            if (selection == null)
            {
                ClearSelection();
                return;
            }
            // trust the internal views to provide a correct frame index
            selection.frameIndexIsSafe = true;
            SetSelectionWithoutIntegrityChecks(selection, null);
        }

        void IProfilerFrameTimeViewSampleSelectionControllerInternal.SetSelectionWithoutIntegrityChecks(ProfilerTimeSampleSelection selectionToSet, List<int> markerIdPath)
        {
            SetSelectionWithoutIntegrityChecks(selectionToSet, markerIdPath);
            ApplySelection(false, true);
        }

        protected void SetSelectionWithoutIntegrityChecks(ProfilerTimeSampleSelection selectionToSet, List<int> markerIdPath)
        {
            if (selectionToSet != null)
            {
                if (selectionToSet.safeFrameIndex != m_ProfilerWindow.selectedFrameIndex)
                    m_ProfilerWindow.SetActiveVisibleFrameIndex(selectionToSet.safeFrameIndex != FrameDataView.invalidOrCurrentFrameIndex ? (int)selectionToSet.safeFrameIndex : ProfilerDriver.lastFrameIndex);
                if (string.IsNullOrEmpty(selectionToSet.legacyMarkerPath))
                {
                    var frameDataView = GetFrameDataView(selectionToSet.threadGroupName, selectionToSet.threadName, selectionToSet.threadId);
                    if (frameDataView == null || !frameDataView.valid)
                        return;
                    selectionToSet.GenerateMarkerNamePath(frameDataView, markerIdPath);
                }
                selection = selectionToSet;
                SetSelectedPropertyPath(selectionToSet.legacyMarkerPath, selectionToSet.threadName);
            }
            else
            {
                selection = null;
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
            if (selection != null)
            {
                // check that the selection is still valid and wasn't badly deserialized on Domain Reload
                if (selection.markerPathDepth <= 0 || selection.rawSampleIndices == null)
                {
                    m_Selection = null;
                    return;
                }

                if (ProfilerDriver.firstFrameIndex >= 0 && ProfilerDriver.lastFrameIndex >= 0)
                {
                    ApplySelection(true, true);
                }
                SetSelectedPropertyPath(selection.legacyMarkerPath, selection.threadName);
            }
        }

        protected static readonly ProfilerMarker k_ApplyValidSelectionMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(ApplySelection)}");
        protected static readonly ProfilerMarker k_ApplySelectionClearMarker = new ProfilerMarker($"{nameof(CPUOrGPUProfilerModule)}.{nameof(ApplySelection)} Clear");
        virtual protected void ApplySelection(bool viewChanged, bool frameSelection)
        {
            if (ViewType == ProfilerViewType.Hierarchy || ViewType == ProfilerViewType.RawHierarchy)
            {
                if (selection != null)
                {
                    using (k_ApplyValidSelectionMarker.Auto())
                    {
                        var currentFrame = m_ProfilerWindow.selectedFrameIndex;
                        if (selection.frameIndexIsSafe && selection.safeFrameIndex == currentFrame)
                        {
                            var treeViewID = ProfilerFrameDataHierarchyView.invalidTreeViewId;
                            if (fetchData)
                            {
                                var frameDataView = m_HierarchyOverruledThreadFromSelection ? GetFrameDataView() : GetFrameDataView(selection.threadGroupName, selection.threadName, selection.threadId);
                                // avoid Selection Migration happening twice during SetFrameDataView by clearing the old one out first
                                m_FrameDataHierarchyView.ClearSelection();
                                m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
                                if (!frameDataView.valid)
                                    return;

                                // GetItemIDFromRawFrameDataViewIndex is a bit expensive so only use that if showing the Raw view (where the raw id is relevant)
                                // or when the cheaper option (setting selection via MarkerIdPath) isn't available
                                if (ViewType == ProfilerViewType.RawHierarchy || (selection.markerPathDepth <= 0))
                                {
                                    treeViewID = m_FrameDataHierarchyView.treeView.GetItemIDFromRawFrameDataViewIndex(frameDataView, selection.rawSampleIndex, selection.markerIdPath);
                                }
                            }

                            if (treeViewID == ProfilerFrameDataHierarchyView.invalidTreeViewId)
                            {
                                if (selection.markerPathDepth > 0)
                                {
                                    m_FrameDataHierarchyView.SetSelection(selection, viewChanged || frameSelection);
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
                        else if (currentFrame >= 0 && selection.markerPathDepth > 0)
                        {
                            if (fetchData)
                            {
                                var frameDataView = m_HierarchyOverruledThreadFromSelection ? GetFrameDataView() : GetFrameDataView(selection.threadGroupName, selection.threadName, selection.threadId);
                                if (!frameDataView.valid)
                                    return;
                                // avoid Selection Migration happening twice during SetFrameDataView by clearing the old one out first
                                m_FrameDataHierarchyView.ClearSelection();
                                m_FrameDataHierarchyView.SetFrameDataView(frameDataView);
                            }
                            m_FrameDataHierarchyView.SetSelection(selection, (viewChanged || frameSelection));
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

        protected int GetThreadIndexInCurrentFrameToApplySelectionFromAnotherFrame(ProfilerTimeSampleSelection selection)
        {
            var currentFrame = (int)m_ProfilerWindow.selectedFrameIndex;
            return selection.GetThreadIndex(currentFrame);
        }

        int IProfilerFrameTimeViewSampleSelectionControllerInternal.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, string sampleName, out List<int> markerIdPath, string markerNamePath)
            => FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, sampleName, out markerIdPath, markerNamePath);
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

        int IProfilerFrameTimeViewSampleSelectionControllerInternal.FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(int frameIndex, int threadIndex, ref string sampleName, ref List<int> markerIdPath, int sampleMarkerId)
            => FindMarkerPathAndRawSampleIndexToFirstMatchingSampleInCurrentView(frameIndex, threadIndex, ref sampleName, ref markerIdPath, sampleMarkerId);
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
                        ProfilerTimeSampleSelection.GetCleanMarkerIdsFromSampleIds(frameData, sampleIdPath, markerIdPath);
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
