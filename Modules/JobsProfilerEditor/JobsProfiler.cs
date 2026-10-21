// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Networking.PlayerConnection;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;
using System.Threading;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Text;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;
using System.Globalization;
using UnityEditor.Compilation;


namespace UnityEditor.JobsProfiling;


internal struct Settings
{
    internal static readonly float[] TickModulos = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 30000, 60000 };
    internal const string TickFormatMilliseconds = "{0}ms";
    internal const string TickFormatSeconds = "{0}s";
    internal const int TickLabelSeparation = 60;
    internal const int InitialLabelCount = 32;
}

internal class JobsProfiler : VisualElement
{
    TimelineBarView m_timelineView = null;
    FrameCache m_frameCache;
    Filter m_filter;
    ToolbarSearchField m_searchField;
    VisualElement m_timeline;
    VisualElement m_filterSpacer;
    long m_currentFrame = 0;
    bool m_disposed = false;

    internal event Action<int, int, int> sampleSelected;
    internal event Action sampleSelectionCleared;

    internal void SetSampleSelection(int frameIndex, int threadIndex, int rawSampleIndex, bool focus)
    {
        if (m_disposed)
            return;

        m_timelineView.SetSampleSelection(frameIndex, threadIndex, rawSampleIndex, focus);
    }

    internal void ClearSampleSelection()
    {
        if (m_disposed)
            return;

        m_timelineView.ClearSampleSelection();
    }

    void OnEventSelected(int frameIndex, int eventIndex)
    {
        FrameData frameData;
        if (m_frameCache.GetFrame(frameIndex, out frameData) &&
            SampleEventMapping.TryFindRawSample(frameData, eventIndex, out int threadIndex, out int rawSampleIndex))
        {
            sampleSelected?.Invoke(frameIndex, threadIndex, rawSampleIndex);
        }
        else
        {
            sampleSelectionCleared?.Invoke();
        }
    }

    void OnEventSelectionCleared()
    {
        sampleSelectionCleared?.Invoke();
    }

    internal void SelectFrame(long frame)
    {
        if (m_disposed)
            return;

        m_currentFrame = frame;
        m_timelineView.SetCurrentFrame((int)frame);
    }
    void OnGeometryChangedEvent(GeometryChangedEvent e)
    {
        if (m_timelineView != null)
            m_timelineView.Update();

        UpdateSearchBarPosition();
    }

    void UpdateSearchBarPosition()
    {
        // Position searchbar right edge to align with timeline right edge
        if (m_timeline != null && m_searchField != null && m_filterSpacer != null)
        {
            var timelineRect = m_timeline.worldBound;
            var parentRect = m_filterSpacer.parent.worldBound;
            var searchWidth = m_searchField.resolvedStyle.width;

            if (timelineRect.width > 0 && searchWidth > 0 && parentRect.width > 0)
            {
                // Convert timeline right edge to parent-relative coordinates
                float timelineRightRelative = timelineRect.xMax - parentRect.xMin;
                float spacerWidth = timelineRightRelative - searchWidth;

                if (spacerWidth > 0)
                    m_filterSpacer.style.width = spacerWidth;
            }
        }
    }

    internal void Update()
    {
        if (m_disposed)
            return;

        int firstFrame = ProfilerDriver.firstFrameIndex;
        int lastFrame = ProfilerDriver.lastFrameIndex;

        if (firstFrame == -1 || lastFrame == -1)
        {
            m_timelineView.ClearData();
            m_frameCache.ClearCache();
        }

        m_timelineView.Update();
        m_frameCache.Update();
        UpdateSearchBarPosition();
    }

    internal void ClearCaches()
    {
        if (m_disposed)
            return;

        m_timelineView.ClearData();
        m_frameCache.ClearCache();
    }

    void ClearedProfile()
    {
        ClearCaches();
    }
    internal void Create()
    {
        m_frameCache = new FrameCache();
        var mainVisualTree = JobsProfilerResources.LoadVisualTreeAsset(JobsProfilerResources.MainUxml);
        var tree = mainVisualTree.Instantiate();

        m_searchField = tree.Query<ToolbarSearchField>("filter").First();
        m_filterSpacer = tree.Query<VisualElement>("filter_spacer").First();
        m_filterSpacer.style.flexGrow = 0;

        VisualElement mv = tree.Query<VisualElement>("main_view").First();
        mv.style.flexGrow = 1;
        tree.style.flexGrow = 1;
        tree.AddToClassList("jobs-profiler-main-background");

        m_filter = new Filter(m_frameCache, m_searchField);

        // Load timeline stylesheet and add theme color reader
        var timelineStyleSheet = JobsProfilerResources.LoadStyleSheet(JobsProfilerResources.TimelineUss);
        tree.styleSheets.Add(timelineStyleSheet);

        var themeColorReader = new JobsProfilerSettings.ThemeColorReader();
        tree.Add(themeColorReader);

        m_timelineView = new TimelineBarView(mv, tree, m_frameCache, m_filter);
        m_timelineView.eventSelected += OnEventSelected;
        m_timelineView.eventSelectionCleared += OnEventSelectionCleared;

        // Get reference to timeline element for dynamic searchbar sizing
        m_timeline = m_timelineView.Query<VisualElement>("timeline").First();
        m_timelineView.style.display = DisplayStyle.Flex;
        m_timelineView.SetCurrentFrame((int)m_currentFrame);
        m_timelineView.m_stats.Show();

        Add(tree);

        EditorApplication.update += Update;
        RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);

        ProfilerDriver.profileCleared += ClearedProfile;

        // Subscribe to color blind mode changes to refresh the view
        JobsProfilerSettings.ColorBlindModeChanged += OnColorBlindModeChanged;
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        Cleanup();
    }

    internal void Cleanup()
    {
        if (m_disposed)
            return;
        m_disposed = true;

        EditorApplication.update -= Update;
        ProfilerDriver.profileCleared -= ClearedProfile;
        JobsProfilerSettings.ColorBlindModeChanged -= OnColorBlindModeChanged;
        UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        if (m_timelineView != null)
        {
            m_timelineView.eventSelected -= OnEventSelected;
            m_timelineView.eventSelectionCleared -= OnEventSelectionCleared;
        }
        m_timelineView?.Dispose();
        m_filter?.Dispose();
        m_frameCache?.Dispose();
    }

    internal void AddSettingsMenuItems(GenericMenu menu)
    {
        m_timelineView?.m_settingsMenu?.AddMenuItems(menu);
    }

    void OnColorBlindModeChanged()
    {
        m_timelineView?.RefreshSemanticColors();
        m_timelineView?.MarkDirtyRepaint();
    }
}

internal class JobsProfilerViewController : ProfilerModuleViewController, IProfilerViewOptionsMenuContributor
{
    internal JobsProfilerViewController(ProfilerWindow profilerWindow) : base(profilerWindow) { }
    private JobsProfiler m_jobsProfiler;
    private IProfilerFrameTimeViewSampleSelectionController m_cpuModule;
    private bool m_isSettingCpuModuleSelection;

    void ApplyCpuModuleSelection()
    {
        var selection = m_cpuModule?.selection;
        int frameIndex = (int)ProfilerWindow.selectedFrameIndex;
        if (frameIndex == FrameDataView.invalidOrCurrentFrameIndex)
            frameIndex = ProfilerDriver.lastFrameIndex;

        if (selection != null && SampleEventMapping.TryFindRawSample(selection, m_cpuModule as IProfilerSampleNameProvider, frameIndex, out int threadIndex, out int rawSampleIndex))
            m_jobsProfiler.SetSampleSelection(frameIndex, threadIndex, rawSampleIndex, true);
        else
            m_jobsProfiler.ClearSampleSelection();
    }

    void OnCpuModuleSelectionChanged(IProfilerFrameTimeViewSampleSelectionController controller, ProfilerTimeSampleSelection selection)
    {
        if (!m_isSettingCpuModuleSelection)
            ApplyCpuModuleSelection();
    }

    void OnSampleSelected(int frameIndex, int threadIndex, int rawSampleIndex)
    {
        using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
        {
            if (m_cpuModule == null || frameData == null || !frameData.valid)
                return;

            var selection = new ProfilerTimeSampleSelection(frameIndex, frameData.threadGroupName, frameData.threadName, frameData.threadId, rawSampleIndex, frameData.GetSampleName(rawSampleIndex));
            m_isSettingCpuModuleSelection = true;
            try
            {
                m_cpuModule.SetSelection(selection);
            }
            finally
            {
                m_isSettingCpuModuleSelection = false;
            }
        }
    }

    void OnSampleSelectionCleared()
    {
        if (m_cpuModule == null)
            return;

        m_isSettingCpuModuleSelection = true;
        try
        {
            m_cpuModule.ClearSelection();
        }
        finally
        {
            m_isSettingCpuModuleSelection = false;
        }
    }

    public void AddViewOptionsMenuItems(GenericMenu menu)
    {
        m_jobsProfiler?.AddSettingsMenuItems(menu);
    }

    void LoadedProfile()
    {
        m_jobsProfiler.ClearCaches();
        m_jobsProfiler.SelectFrame((int)ProfilerWindow.selectedFrameIndex);
    }

    protected override VisualElement CreateView()
    {
        int selectedFrame = (int)ProfilerWindow.selectedFrameIndex;

        m_jobsProfiler = new JobsProfiler();
        m_jobsProfiler.Create();
        m_jobsProfiler.sampleSelected += OnSampleSelected;
        m_jobsProfiler.sampleSelectionCleared += OnSampleSelectionCleared;

        ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
        m_jobsProfiler.style.flexShrink = 1;
        m_jobsProfiler.style.flexGrow = 1;
        m_jobsProfiler.SelectFrame(selectedFrame);

        ProfilerDriver.profileLoaded += LoadedProfile;

        m_cpuModule = ProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
        if (m_cpuModule != null)
            m_cpuModule.selectionChanged += OnCpuModuleSelectionChanged;
        ApplyCpuModuleSelection();

        return m_jobsProfiler;
    }

    void OnSelectedFrameIndexChanged(long selectedFrameIndex)
    {
        m_jobsProfiler.SelectFrame(selectedFrameIndex);
        if (!m_isSettingCpuModuleSelection)
            ApplyCpuModuleSelection();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            ProfilerDriver.profileLoaded -= LoadedProfile;
            if (m_cpuModule != null)
                m_cpuModule.selectionChanged -= OnCpuModuleSelectionChanged;
            if (m_jobsProfiler != null)
            {
                m_jobsProfiler.sampleSelected -= OnSampleSelected;
                m_jobsProfiler.sampleSelectionCleared -= OnSampleSelectionCleared;
            }
            m_jobsProfiler?.Cleanup();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Supplies the Jobs Profiler view to the Profiler Window's CPU module, which presents it as its
/// 'New Timeline' view type. Discovered by the Profiler Window through TypeCache.
/// </summary>
internal class JobsProfilerTimelineViewProvider : JobsProfilerViewProvider
{
    internal override ProfilerModuleViewController CreateViewController(ProfilerWindow profilerWindow)
    {
        return new JobsProfilerViewController(profilerWindow);
    }
}
