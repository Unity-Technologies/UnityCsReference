// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Networking.PlayerConnection;
using System.Runtime.CompilerServices;

using System.Runtime.InteropServices;
using System.Threading;
using UnityEditorInternal;

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

    internal void SelectFrame(long frame)
    {
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
        EditorApplication.update -= Update;
        ProfilerDriver.profileCleared -= ClearedProfile;
        JobsProfilerSettings.ColorBlindModeChanged -= OnColorBlindModeChanged;
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

        ProfilerWindow.SelectedFrameIndexChanged += OnSelectedFrameIndexChanged;
        m_jobsProfiler.style.flexShrink = 1;
        m_jobsProfiler.style.flexGrow = 1;
        m_jobsProfiler.SelectFrame(selectedFrame);

        ProfilerDriver.profileLoaded += LoadedProfile;

        return m_jobsProfiler;
    }

    void OnSelectedFrameIndexChanged(long selectedFrameIndex)
    {
        m_jobsProfiler.SelectFrame(selectedFrameIndex);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ProfilerWindow.SelectedFrameIndexChanged -= OnSelectedFrameIndexChanged;
            ProfilerDriver.profileLoaded -= LoadedProfile;
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
