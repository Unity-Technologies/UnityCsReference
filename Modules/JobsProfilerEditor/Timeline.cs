// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Globalization;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine.UIElements.Experimental;
using UnityEditor.UIElements;

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.JobsProfiling;

internal struct SelectedFrameRange
{
    /// Used for selecting how many frames we have surrounding the active one
    internal const int k_SelectionRange = 3;
    /// Start frame in the selection
    internal int start;
    /// The "active" frame
    internal int active;
    /// end frame
    internal int end;
}

/// <summary>
/// Temporary used for the active frames we want to render/show
/// </summary>
internal struct FrameIndex
{
    /// Current time in the timeline
    internal float time;
    /// Total time for the frame
    internal float frameTime;
    /// Value to multiply the color with to do fading
    internal float fade;
    /// Index to the frame in the frame cache
    internal int frameCacheIndex;
}

/// <summary>
/// Returned from GetActiveFrames with info about the active frames
/// </summary>
internal struct FrameDataIndex
{
    internal FrameData data;
    internal FrameIndex index;
}

internal enum TransitionMode
{
    Fixed,
    Smooth,
};

internal struct InternalJobHandle
{
    internal uint index;
    internal uint generation;

    internal InternalJobHandle(ulong value)
    {
        index = (uint)(value >> 32);
        generation = (uint)(value & 0xffffffff);
    }

    internal ulong ToUlong()
    {
        return (((ulong)index) << 32) | (ulong)generation;
    }

    internal bool IsValid()
    {
        return index != 0 && generation != 0;
    }
}

/// <summary>
/// The type of metadata that we are processing. This has to match the C++ side: JobsProfilerMetadata.h
/// </summary>
internal enum MetadataType
{
    ScheduleJob,

    CombineDependencies,
    WaitOnJob,
    WaitForCompleted,
    AllocateJob,
    ScheduleAllocatedJob,
    KickJobs,

    BeginJob,
    EndJob,

    BeginPreExecute,
    EndPreExecute,

    BeginPostExecute,
    EndPostExecute,
}

/// <summary>
/// Keeps info about data when we are scheduling a job
/// </summary>

internal struct ScheduledJobInfo
{
    // JobHandle for the job
    internal InternalJobHandle handle;
    /// Count when scheduling the job (1 for single jobs)
    internal uint count;
    /// The grain sized used when scheduling parallel jobs
    internal uint grainSize;
    /// Index into the event stream when the job was scheduled
    internal int eventIndex;
    /// How many jobs this job depends on
    internal int dependencyCount;
    /// Index into the dependency table
    internal int dependencyTableIndex;
};

internal enum JobFlowState
{
    /// The job is being waited on.
    WaitedOn,
    /// The job is being waited on, but was finished at that point.
    CompletedNoWait,
    /// Job is being scheduled
    BeginSchedule,
}

/// <summary>
/// This data that describes starts/end "states" of a job. That is if a job has been scheduled,
/// being waited on, etc
/// </summary>
internal struct JobFlow
{
    // JobHandle for the job
    internal InternalJobHandle handle;
    /// Index into the event stream
    internal int eventIndex;
    /// True if we were blocking while waiting on the job
    internal JobFlowState state;
    internal Color32 getColor(bool colorBlind)
    {
        Color32 color = JobsProfilerColors.GetFallbackColor(colorBlind);

        switch (state)
        {
            case JobFlowState.BeginSchedule:
            {
                color = getScheduleColor(colorBlind);
                break;
            }

            case JobFlowState.CompletedNoWait:
            {
                color = JobsProfilerColors.GetCompletedNoWaitColor(colorBlind);
                break;
            }

            case JobFlowState.WaitedOn:
            {
                color = JobsProfilerColors.GetCompletedWaitColor(colorBlind);
                break;
            }
        }

        return color;
    }

    static internal Color32 getScheduleColor(bool colorBlind)
    {
        return JobsProfilerColors.GetScheduleColor(colorBlind);
    }
};
/// <summary>
/// Different settings for rendering the timeline
/// </summary>
internal struct TimelineSettings
{
    internal float4x4 mat;
    /// Size of the window we are rendering to
    internal Rect windowRect;
    // The size of a bar in the timeline
    internal float barSize;
    // Inverse of the bar size 1.0 - (1/barSize) that is used for 1 pixel gap
    internal float invYBarSize;
    /// Show schudled by
    internal bool showScheduledBy;
    internal bool zoomOnEventFocus;
    internal bool showDependsOn;
    internal bool showDependantOn;
    internal bool showCompletedByWait;
    internal bool showCompletedByNoWait;
    internal bool zoomOnEventHover;
    internal bool showFoldedGroupPreview;
    /// Resolved on the main thread; IsColorBlindMode uses reflection and cannot run in a job
    internal bool colorBlindMode;
    /// When an element is smaller than this we expand the hit area to make it easier to select
    internal const float ExpandWidthHitArea = 4.0f;
    /// This is the total size of the hit area when we expand it (in pixels)
    internal const float ExpandWidthHitAreaTotalSize = 5.0f;
    /// Show the fully dependency chain for a job
    internal bool showFullDependencyChain;

    internal const int NavigateNextParallelEventCount = 3;
}

/// <summary>
/// Used for sorting the different Thread groups such as "Job", "Other Threads", etc
/// </summary>
internal struct ThreadGroupInfo
{
    /// Name of the group
    internal FixedString128Bytes name;
    /// This is the calculated max depth for all the current visible groups.
    /// We need to do it this way so that all the threads line up correctly in the timeline
    internal int maxDepth;
    /// Whether this group is folded/collapsed
    internal bool isFolded;
    /// Vertical offset position for this group's label
    internal float offset;
}

/// Mouse state used inside job to t
struct MouseState
{
    /// Position of the mouse
    internal float2 pos;
    /// If the alt key is pressed (This is used when zooming/panning so we shouldn't react to changes then)
    internal int isAltKeyDown;
    /// If left mouse button is down
    internal int isLeftDown;
    /// If right mouse button is down
    internal int isRightDown;
}

/// <summary>
/// Holds info about which event that started the job and which completed/waited on.
/// </summary>
internal struct StartCompleteInfo
{
    /// Parent index of the event that started the job
    internal int startEventParentIndex;
    /// Index of the event that started the job
    internal int startEventIndex;
    /// Frame of the start event
    internal int startFrame;
    /// Parent index of the event that completed the job
    internal int completeEventParentIndex;
    /// Index of the event that completed the job
    internal int completeEventIndex;
    /// Frame of the complete event event
    internal int completeFrame;

    /// Event index 0 and frame 0 are both valid, so -1 is the "no such event" sentinel
    internal static StartCompleteInfo CreateInvalid()
    {
        return new StartCompleteInfo
        {
            startEventParentIndex = -1,
            startEventIndex = -1,
            startFrame = -1,
            completeEventParentIndex = -1,
            completeEventIndex = -1,
            completeFrame = -1,
        };
    }
}

/// <summary>
/// Holds single entry of data for the DependencyInfo struct. See below for more info
/// </summary>
internal struct DependJobInfo
{
    /// Jobhandle that has been selected
    internal InternalJobHandle jobHandle;
    /// Which frame the job is present in
    internal int frameIndex;
    /// Index of the job given it's frame
    internal int eventIndex;
}

/// <summary>
/// This data is generated by the GenerateDependenciesMeshJob with info about the dependencies
/// for the seleceted job. This includes the depenency and the list of dependant jobs.
/// This info can be used for anything, but is primary used for the UI to allow the user to navigate
/// to the connected jobs.
/// </summary>
internal struct DependenciesInfo
{
    /// Info about the job start and complete events
    internal NativeArray<StartCompleteInfo> startComplete;
    /// The various directions we can navigate to for job dependencies
    internal NativeList<DependJobInfo> dependencyJobs;
    /// The various directions we can navigate to for job dependants
    internal NativeList<DependJobInfo> dependantJobs;
}

/// <summary>
/// Keeps track of the current selected job
/// </summary>
internal struct JobSelection
{
    /// <summary>
    /// Update state is used to determine how to render the bars
    /// </summary>
    internal enum State
    {
        // No selection made
        Default,
        // We have a job selected
        Selected,
        // Begin selection/range dragging
        BeginDrag,
        // Dragging a range
        Drag,
        // Dragging Ended
        EndDrag,
    }

    internal State state;
    internal State dragState;
    internal int frameIndex;
    internal int eventIndex;
    internal int markerId;
    internal bool updatedSelection;
    internal bool updatedFocus;
    // indicates that this is a temporary selection. This is used for the UI to show what the next selection will be if selected.
    internal bool hover;
    internal InternalJobHandle jobHandle;
}

/// <summary>
/// This is used when we are transitioning between two events. This is used to smooth out the transition instead of just jumping to the next event.
/// </summary>
class SmoothEventTransition
{
    internal enum State
    {
        None,
        Transition,
        End,
    }
    internal SmoothEventTransition()
    {
        m_pos.x = 0.0f;
        m_pos.y = 16.6f;
        m_pos.z = 0.0f;
        m_start = m_pos;
        m_end = m_pos;
    }

    float m_time = 0.0f;
    State m_state = State.None;
    float3 m_start;
    float3 m_end;
    float3 m_pos;
    bool m_fixed = false;

    internal void SetTarget(float time, float duration, float height)
    {
        m_start = m_pos;
        m_end.x = time;
        m_end.y = duration;
        m_end.z = height;
        m_end.z = height;
        m_time = 0.0f;
        m_state = State.Transition;
        m_fixed = false;
    }

    internal void SetFixedTarget(float time, float duration, float height)
    {
        m_pos.x = time;
        m_pos.y = duration;
        m_pos.z = height;
        m_start = m_pos;
        m_end = m_pos;
        m_time = 0.0f;
        m_state = State.Transition;
        m_fixed = true;
    }

    internal bool Update(float dt)
    {
        switch (m_state)
        {
            case State.None:
                break;

            case State.Transition:
            {
                m_time += dt;

                if (m_time >= 1.0f)
                {
                    m_time = 1.0f;
                    m_state = State.End;
                }

                var t = math.smoothstep(0.0f, 1.0f, m_time);
                m_pos = math.lerp(m_start, m_end, t);

                return true;
            }

            case State.End:
            {
                m_time = 0.0f;
                m_state = State.None;
                break;
            }
        }

        return false;
    }
    internal float3 Pos { get { return m_pos; } }
    internal bool Fixed { get { return m_fixed; } }
}


/// <summary>
/// Used for showing the range of of a drag selection
/// </summary>
struct DragRange
{
    internal TextElement textLabel;
    internal float startTime;
    internal float endTime;
    internal bool show;
}

/// <summary>
/// Used when starting a mesh generation job to pass in the data needed
/// </summary>
struct GenerateMeshContext
{
    internal PrimitiveRenderer renderer;
    internal JobHandle jobHandle;
    internal NativeHashMap<ulong, int> visibleDepthOutput;
}

/// <summary>
/// Generates the bars mesh output and text positions
/// </summary>
[BurstCompile()]
struct GenerateMeshJob : IJob
{
    [ReadOnly]
    internal TimelineSettings m_settings;

    [ReadOnly]
    internal NativeArray<ThreadInfo> m_threads;

    [ReadOnly]
    internal NativeList<ProfilingEvent> m_events;

    [ReadOnly]
    internal NativeHashMap<int, ulong> m_eventHandleLookup;

    [ReadOnly]
    internal NativeList<Color32> m_markerColors;

    [ReadOnly]
    internal MouseState m_mouseState;

    [ReadOnly]
    internal FrameIndex m_frameIndex;

    [ReadOnly]
    internal NativeHashMap<ulong, ThreadPosition> m_threadOffsets;

    [ReadOnly]
    internal NativeList<JobFlow> m_jobFlows;

    [ReadOnly]
    internal float m_threadOffset;

    [ReadOnly]
    internal bool m_useFilter;

    const float kStartJobEvtSpacing = 1.5f;

    // Background color for fading non-selected frames (passed in from TimelineBarView)
    [ReadOnly]
    internal Color32 m_backgroundColor;

    // Selection blend colors (passed in for Burst compatibility)
    [ReadOnly]
    internal Color32 m_selectionBlendColor;
    [ReadOnly]
    internal Color32 m_deselectionBlendColor;

    //[ReadOnly]
    //internal NativeList<JobInfo> m_jobsInfo;

    internal NativeArray<JobSelection> m_jobSelection;

    [ReadOnly]
    internal NativeHashSet<int> m_idFilters;

    /// <summary>
    /// Output: max visible depth per thread (threadId -> maxLevel+1)
    /// </summary>
    internal NativeHashMap<ulong, int> m_visibleDepthOutput;

    internal PrimitiveRenderer m_renderer;

    enum MergeState
    {
        /// Regular rendering, just render bars and progress
        Default,
        /// If we have lots of tiny bars in a row we try to merge them instead
        MergeBar,
    }

    void DrawQuad(float2 c0, float2 c1, Color32 color, int eventIndex, bool isColorReplaced)
    {
        var jobSelection = m_jobSelection[0];

        if (jobSelection.state == JobSelection.State.Selected && !isColorReplaced)
        {
            if (jobSelection.eventIndex == eventIndex && jobSelection.frameIndex == m_frameIndex.frameCacheIndex)
                color = Color32.Lerp(color, m_selectionBlendColor, 0.2f);
            else
                color = Color32.Lerp(color, m_deselectionBlendColor, 0.4f);
        }

        // Fade non-selected frames toward background color (not black)
        color = Color32.Lerp(color, m_backgroundColor, m_frameIndex.fade);

        // this ensures that we always have at least 2 pixel wide quads to reduce flickering
        if ((c1.x - c0.x) < 2.0f)
            c1.x = c0.x + 2.0f;

        m_renderer.DrawQuadColor(c0, c1, color);
    }

    internal void DrawArrow(float2 pos, Color32 color, float size, PrimitiveRenderer.ArrowDirection direction, int eventIndex)
    {
        var jobSelection = m_jobSelection[0];

        if (jobSelection.state == JobSelection.State.Selected)
        {
            if (jobSelection.eventIndex == eventIndex && jobSelection.frameIndex == m_frameIndex.frameCacheIndex)
                color = Color32.Lerp(color, m_selectionBlendColor, 0.2f);
            else
                color = Color32.Lerp(color, m_deselectionBlendColor, 0.4f);
        }

        // Fade non-selected frames toward background color (not black)
        color = Color32.Lerp(color, m_backgroundColor, m_frameIndex.fade);

        m_renderer.DrawArrowWithSize(pos, color, size, direction);
    }

    /// Draws a range
    void DrawThreadRange(in ThreadInfo threadInfo, float threadOffset, NativeBitArray scheduleJobEvents)
    {
        MergeState state = MergeState.Default;
        float2 mergeStartCorner = new float2(0.0f);
        float2 mergeCorner = new float2(0.0f);
        ushort prevLevel = 0;
        int prevCategory = 0;
        bool prevIsFiltered = false;

        var jobSelection = m_jobSelection[0];

        float2 scale = new float2(m_settings.mat.c0.x, m_settings.mat.c1.y);
        float2 trans = new float2(m_settings.mat.c3.x, m_settings.mat.c3.y);

        // Get animation progress for smooth clipping
        ThreadPosition threadPos;
        float currentMaxDepth = float.MaxValue;
        bool isFoldedPreview = false;
        float previewHeightMultiplier = 1.0f;

        if (m_threadOffsets.TryGetValue(threadInfo.threadId, out threadPos))
        {
            // Calculate current visible depth based on animation progress
            float maxDepth = (float)threadPos.depth;
            currentMaxDepth = math.lerp(1.0f, maxDepth, threadPos.animationProgress);

            // Check for folded preview mode
            isFoldedPreview = threadPos.visibility == ThreadVisibility.Preview;
            if (isFoldedPreview)
            {
                currentMaxDepth = 1.0f; // Only render level 0 events in preview
                previewHeightMultiplier = threadPos.previewHeight;
            }
        }

        for (int eventIndex = threadInfo.eventStart; eventIndex < threadInfo.eventEnd;)
        {
            ProfilingEvent profEvent = m_events[eventIndex];

            // Skip events beyond current animated depth
            if (profEvent.level >= currentMaxDepth)
            {
                eventIndex++;
                continue;
            }

            float2 posLocal = new float2(profEvent.startTime + m_frameIndex.time, threadOffset + profEvent.level);
            float2 sizeLocal = new float2(profEvent.time, m_settings.invYBarSize);

            // Clip bar height if partially visible (event is being clipped)
            float visibleHeight = math.min(1.0f, currentMaxDepth - profEvent.level);
            sizeLocal.y *= visibleHeight;

            // Apply height multiplier for folded preview mode
            sizeLocal.y *= previewHeightMultiplier;
            float2 cornerLocal = posLocal + sizeLocal;

            float2 pos = (posLocal * scale) + trans;
            float2 corner = (cornerLocal * scale) + trans;

            float2 size = corner - pos;

            Rect barRect = new Rect(pos.x, pos.y, size.x, size.y);

            // Make sure the bar is within the window otherwise we skip render it
            if (!m_settings.windowRect.Overlaps(barRect))
            {
                eventIndex++;
                continue;
            }

            // Track max visible depth for this thread
            int eventDepth = profEvent.level + 1;
            if (m_visibleDepthOutput.TryGetValue(threadInfo.threadId, out int currentMax))
            {
                if (eventDepth > currentMax)
                    m_visibleDepthOutput[threadInfo.threadId] = eventDepth;
            }
            else
            {
                m_visibleDepthOutput.TryAdd(threadInfo.threadId, eventDepth);
            }

            // Event filtering - dim filtered events instead of hiding them
            bool isFilteredOut = m_useFilter && !m_idFilters.Contains(profEvent.markerId);

            // If we have a job start event we make it wider to make it more visible and easier to select.
            bool isScheduleJobEvent = scheduleJobEvents.IsSet(eventIndex);

            if (isScheduleJobEvent)
            {
                pos.x -= kStartJobEvtSpacing;
                corner.x += kStartJobEvtSpacing;
                size.x = corner.x - pos.x;
            }

            ushort level = profEvent.level;
            int catId = profEvent.categoryId;

            // Hit testing - do this BEFORE merge logic so small events can be selected
            // Only hover if right mouse button isn't down and event is not filtered out
            if (m_mouseState.isRightDown == 0 && !isFilteredOut)
            {
                float2 mousePos = m_mouseState.pos;

                float2 hitMin = new float2(pos.x, pos.y);
                float2 hitMax = new float2(corner.x, corner.y);

                bool2 inside0;
                bool2 inside1;

                // Hit area is smaller than this value we want to expand it to make selection easier.
                if ((hitMax.x - hitMin.x) < TimelineSettings.ExpandWidthHitArea)
                {
                    float middlePoint = (hitMin.x + hitMax.x) * 0.5f;
                    float2 startCorner = hitMin;
                    float2 endCorner = hitMax;

                    startCorner.x = middlePoint - (TimelineSettings.ExpandWidthHitAreaTotalSize * 0.5f);
                    endCorner.x = middlePoint + (TimelineSettings.ExpandWidthHitAreaTotalSize * 0.5f);

                    inside0 = mousePos >= startCorner;
                    inside1 = mousePos < endCorner;
                }
                else
                {
                    inside0 = mousePos >= hitMin;
                    inside1 = mousePos < hitMax;
                }

                if (inside0.x && inside0.y && inside1.x && inside1.y)
                {
                    // Check if we have selected a job and aren't panning and we don't have a drag selection active
                    if (m_mouseState.isLeftDown == 1 &&
                        m_mouseState.isAltKeyDown == 0 &&
                        jobSelection.dragState != JobSelection.State.Drag)
                    {
                        ulong jobHandle;

                        m_eventHandleLookup.TryGetValue(eventIndex, out jobHandle);
                        jobSelection.state = JobSelection.State.Selected;
                        jobSelection.frameIndex = m_frameIndex.frameCacheIndex;
                        jobSelection.eventIndex = eventIndex;
                        jobSelection.jobHandle = new InternalJobHandle(jobHandle);
                        jobSelection.updatedSelection = true;
                        jobSelection.updatedFocus = true;
                        jobSelection.hover = false;
                        jobSelection.markerId = profEvent.markerId;
                    }
                }

                m_jobSelection[0] = jobSelection;
            }

            // if we have a bar that is less than a pixel we clamp it, but also start checking if we can merge it
            if (size.x < 0.99f)
            {
                size.x = 1.0f;

                if (state == MergeState.MergeBar)
                {
                    // Make sure we have the same type of event when merging. The filter state is part of
                    // this because a merged run is drawn as a single quad with a single appearance, so a
                    // run mixing matching and nonmatching markers could only ever render one of them right.
                    if (prevLevel == level && prevCategory == catId && prevIsFiltered == isFilteredOut)
                    {
                        // in order to merge the bar we need to make sure that the new bar is close/intersecting
                        // with the current merge corner, otherwise this may be another small event that has a large offset
                        float expectedCorner = mergeCorner.x + 1.0f;
                        if (corner.x < expectedCorner)
                        {
                            mergeCorner.x = corner.x;
                            eventIndex++;
                            continue;
                        }
                    }
                }
                else
                {
                    // Enter merge state
                    state = MergeState.MergeBar;
                    mergeStartCorner = new float2(pos.x, pos.y);
                    mergeCorner = new float2(pos.x + 1.0f, corner.y);
                    prevLevel = profEvent.level;
                    prevCategory = profEvent.categoryId;
                    prevIsFiltered = isFilteredOut;
                    eventIndex++;
                    continue;
                }
            }

            //
            float2 c0 = new float2(pos.x, pos.y);
            float2 c1 = new float2(pos.x + size.x, corner.y);

            // If we are here and in merge bar state it means that we need to draw the bar.
            // The way we do this is to reset the active values with the stored ones and
            // run the code as usually, but we don't update the eventIndex, now when we
            // come around to this code the next time the state has been changed and we
            // will draw the bar again with the active values and move on to next index
            if (state == MergeState.MergeBar)
            {
                c0 = mergeStartCorner;
                c1 = mergeCorner;
                catId = prevCategory;
                isFilteredOut = prevIsFiltered;
            }

            if (isScheduleJobEvent && state == MergeState.Default && m_settings.showScheduledBy)
            {
                float arrowSize = 6.0f;
                Color32 color = JobFlow.getScheduleColor(m_settings.colorBlindMode);

                // Apply dimming for preview mode (folded group compact view)
                if (isFoldedPreview)
                    color = Color32.Lerp(color, Color.black, 0.4f);

                // Apply grayscale + dimming for filtered out events
                if (isFilteredOut)
                {
                    byte luminance = (byte)(0.299f * color.r + 0.587f * color.g + 0.114f * color.b);
                    color = new Color32(luminance, luminance, luminance, color.a);
                    color = Color32.Lerp(color, Color.black, 0.5f);
                }

                // Pass true as isColorReplaced when filtered or preview to skip selection dimming in DrawQuad
                bool skipSelectionDimming = isFoldedPreview || isFilteredOut;
                DrawQuad(c0, c1, color, eventIndex, skipSelectionDimming);

                float2 arrowPos = new float2(c1.x - kStartJobEvtSpacing, c1.y - arrowSize + 1.0f);

                // If we haven't selected this job we draw an arrow (skip in preview mode and filtered events)
                if (m_settings.showScheduledBy && !isFoldedPreview && !isFilteredOut && !(jobSelection.state == JobSelection.State.Selected && jobSelection.eventIndex == eventIndex))
                    DrawArrow(arrowPos, color, arrowSize, PrimitiveRenderer.ArrowDirection.Down, eventIndex);
            }
            else
            {
                // Draw the bar
                Color32 color = m_markerColors[catId];

                // Apply dimming for preview mode (folded group compact view)
                if (isFoldedPreview)
                    color = Color32.Lerp(color, Color.black, 0.4f);

                // Apply grayscale + dimming for filtered out events
                if (isFilteredOut)
                {
                    byte luminance = (byte)(0.299f * color.r + 0.587f * color.g + 0.114f * color.b);
                    color = new Color32(luminance, luminance, luminance, color.a);
                    color = Color32.Lerp(color, Color.black, 0.5f);
                }

                // Pass true as isColorReplaced when filtered or preview to skip selection dimming in DrawQuad
                bool skipSelectionDimming = isFoldedPreview || isFilteredOut;
                if (state == MergeState.Default)
                    DrawQuad(c0, c1, color, eventIndex, skipSelectionDimming);
                else
                    DrawQuad(c0, c1, color, -1, skipSelectionDimming);
            }

            if (state == MergeState.MergeBar)
                state = MergeState.Default;
            else
                eventIndex++;
        }

        // if we are here and we ar still in merge state we exited the loop above without drawing the quad so
        // we need to do it here
        if (state == MergeState.MergeBar)
        {
            Color32 mergeColor = m_markerColors[prevCategory];
            if (isFoldedPreview)
                mergeColor = Color32.Lerp(mergeColor, Color.black, 0.4f);

            // Apply grayscale + dimming for filtered out events
            if (prevIsFiltered)
            {
                byte luminance = (byte)(0.299f * mergeColor.r + 0.587f * mergeColor.g + 0.114f * mergeColor.b);
                mergeColor = new Color32(luminance, luminance, luminance, mergeColor.a);
                mergeColor = Color32.Lerp(mergeColor, Color.black, 0.5f);
            }

            bool skipSelectionDimming = isFoldedPreview || prevIsFiltered;
            DrawQuad(mergeStartCorner, mergeCorner, mergeColor, -1, skipSelectionDimming);
        }

        m_jobSelection[0] = jobSelection;
    }

    public void Execute()
    {
        // The reason is because we want to draw them in a different way compared to other event
        // to make it a bit clearer to the user. We build a bitfield here as a majority of cases
        // a regular event will not be a start job event.
        NativeBitArray scheduleJobEvents = new NativeBitArray(m_events.Length, Allocator.Temp);

        m_renderer.Begin();

        for (int i = 0, count = m_jobFlows.Length; i < count; ++i)
        {
            JobFlow flow = m_jobFlows[i];

            if (flow.state != JobFlowState.BeginSchedule)
                continue;

            scheduleJobEvents.Set(flow.eventIndex, true);
        }

        foreach (var thread in m_threads)
        {
            ThreadPosition threadPos;

            if (m_threadOffsets.TryGetValue(thread.threadId, out threadPos))
            {
                DrawThreadRange(thread, threadPos.offset, scheduleJobEvents);
            }
        }

        // If this frame isn't faded it's the active one and we draw frame lines around the data
        if (m_frameIndex.fade == 0.0)
        {
            float height = m_settings.windowRect.height;

            float3 startLocal = new float3(m_frameIndex.time, 0.0f, 0.0f);
            float3 endLocal = new float3(m_frameIndex.time + m_frameIndex.frameTime, 0.0f, 0.0f);

            float3 start = transform(m_settings.mat, startLocal);
            float3 end = transform(m_settings.mat, endLocal);

            float2 c0 = new float2(start.x, start.y);
            float2 c1 = new float2(start.x + 1.0f, height);

            m_renderer.DrawQuadColor(c0, c1, Color.grey);

            c0 = new float2(end.x, end.y);
            c1 = new float2(end.x + 1.0f, height);

            m_renderer.DrawQuadColor(c0, c1, Color.grey);
        }

        scheduleJobEvents.Dispose();
    }
}

class TickLabels
{
    class LabelValue
    {
        internal float value;
        internal WrapText label;
    }

    TickHandler m_tickHandler;
    List<LabelValue> m_labels;
    VisualElement m_parent;
    ZoomableArea m_zoomArea;
    bool m_skipRendering = false;
    float m_endFrameEnd = 16.0f;

    internal TickLabels(TickHandler tickHandler, ZoomableArea zoomArea, VisualElement parent)
    {
        m_labels = new List<LabelValue>(Settings.InitialLabelCount);
        m_parent = parent;
        m_tickHandler = tickHandler;
        m_zoomArea = zoomArea;
        m_parent.generateVisualContent += RenderTickMarks;
    }

    string FormatTickLabel(float time, int level)
    {
        string format = Settings.TickFormatMilliseconds;
        var period = m_tickHandler.GetPeriodOfLevel(level);
        var log10 = Mathf.FloorToInt(Mathf.Log10(period));
        if (log10 >= 3)
        {
            time /= 1000;
            format = Settings.TickFormatSeconds;
        }

        return String.Format(CultureInfo.InvariantCulture.NumberFormat, format, time.ToString("N" + Mathf.Max(0, -log10)));
    }

    internal void RenderTickMarks(MeshGenerationContext mgc)
    {
        if (m_skipRendering)
            return;

        Rect rect = m_zoomArea.drawRect;
        rect.x = rect.y = 0.0f;

        mgc.painter2D.lineWidth = 1.0f;

        // TODO: Move
        const float kTickRulerFatThreshold = 0.5f;     // size of ruler tick marks at which they begin getting fatter
        const float kTickRulerHeightMax = 0.7f; // height of the ruler tick marks when they are highest

        var baseColor = Color.white;
        baseColor.a *= 0.75f;

        for (int l = 0; l < m_tickHandler.tickLevels; l++)
        {
            var strength = m_tickHandler.GetStrengthOfLevel(l) * .8f;
            if (strength < 0.1f)
                continue;
            var ticks = m_tickHandler.GetTicksAtLevel(l, true);
            for (int i = 0; i < ticks.Length; i++)
            {
                // Draw line
                var time = ticks[i];
                var x = m_zoomArea.TimeToPixel(time, rect);
                var height = 20.0f * Mathf.Min(1, strength) * kTickRulerHeightMax;

                float activeFrameFade = 0.5f;

                if (time >= 0.0f && time <= m_endFrameEnd)
                    activeFrameFade = 1.0f;

                var color = new Color(1, 1, 1, strength / kTickRulerFatThreshold) * baseColor * activeFrameFade;
                // TODO: We should really profile this

                float yStart = 20.0f - height + 0.5f;
                float yEnd = 20.0f - 0.5f;

                mgc.painter2D.strokeColor = color;
                mgc.painter2D.BeginPath();
                mgc.painter2D.MoveTo(new Vector2(x, yStart));
                mgc.painter2D.LineTo(new Vector2(x, yEnd));
                mgc.painter2D.Stroke();

                //TimeArea.DrawVerticalLineFast(x, timeRulerRect.height - height + 0.5f, timeRulerRect.height - 0.5f, color);
            }
        }
    }

    internal void Update(in TickHandler tickHandler, in ZoomableArea zoomArea, int selectedFrame, TextElement rangeText, float frameEnd)
    {
        m_endFrameEnd = frameEnd;

        if (selectedFrame == -1)
        {
            m_skipRendering = true;
            HideLabels();
            m_parent.MarkDirtyRepaint();
            return;
        }

        m_skipRendering = false;
        bool hasCreatedLabels = false;

        Rect rect = zoomArea.drawRect;
        rect.x = rect.y = 0.0f;

        var labelWidth = Settings.TickLabelSeparation;
        int labelLevel = tickHandler.GetLevelWithMinSeparation(labelWidth);
        float[] labelTicks = tickHandler.GetTicksAtLevel(labelLevel, false);

        int labelCount = labelTicks.Length;
        int labelWidgetCount = m_labels.Count;

        // Add new labels if we have more than before
        for (int i = 0; i < labelCount - labelWidgetCount; ++i)
        {
            var labelValue = new LabelValue
            {
                label = new WrapText(),
                value = float.MaxValue,
            };

            labelValue.label.usageHints = UsageHints.DynamicTransform;
            labelValue.label.style.overflow = Overflow.Hidden;
            labelValue.label.style.position = Position.Absolute;

            m_parent.Add(labelValue.label);
            m_labels.Add(labelValue);

            hasCreatedLabels = true;
        }

        for (int i = 0; i < labelCount; i++)
        {
            var time = labelTicks[i];
            var textLabel = m_labels[i].label;

            float labelPos = Mathf.Floor(zoomArea.TimeToPixel(time, rect));

            // As it's expensive to update the label text we check if the value is the same first
            // and skip the update if it hasn't changed size last update
            if (!Mathf.Approximately(m_labels[i].value, time))
            {
                m_labels[i].value = time;
                textLabel.m_text.text = FormatTickLabel(time, labelLevel);
            }

            textLabel.visible = true;
            textLabel.SetTranslate(new float2(labelPos + 3, 0.0f));

            if (time >= 0.0f && time <= frameEnd)
                textLabel.SetEnabled(true);
            else
                textLabel.SetEnabled(false);
        }

        // If we have more labels than being displayed we hide the rest of them
        for (int i = labelCount; i < m_labels.Count; ++i)
            m_labels[i].label.visible = false;

        // TODO: We should only do this if we need to
        m_parent.MarkDirtyRepaint();

        // Because we are dynamicly creating labels and we always want to make sure
        // that the range text is on top we need to remove and add it again.
        if (hasCreatedLabels)
        {
            m_parent.Remove(rangeText);
            m_parent.Add(rangeText);
        }
    }

    internal void HideLabels()
    {
        foreach (var label in m_labels)
            label.label.visible = false;
    }
}


/// Displays the rectanges bars
class TimelineBarView : VisualElement, IDisposable
{
    // Background color of the timeline area - used for fading non-selected frames
    internal static Color BackgroundColor => JobsProfilerSettings.TimelineBackground;

    FrameCache m_frameCache;

    internal ThreadLabels m_threads;
    internal TextRenderer m_textRenderer;
    internal TickHandler m_tickHandler;
    internal TickLabels m_tickLabels;
    internal ZoomableArea m_zoomArea;
    internal JobsInfoPanel m_jobInfoPanel;
    internal Scroller m_verticalScroller;
    internal MinMaxSlider m_horizontalScroller;
    internal Stats m_stats;
    internal SettingsMenu m_settingsMenu;
    // Settings
    internal DropdownField m_paralleJobsNavigation;
    private TwoPaneSplitView m_splitView;
    private ToolbarToggle m_jobsInfoToggle;
    private VisualElement m_timeline;

    internal List<NativeArray<JobSelection>> m_jobSelectionJobs;
    internal JobSelection m_jobSelection;
    internal NativeHashMap<ulong, ThreadPosition> m_threadOffsets;
    internal bool m_isAnimating; // True when any thread is animating
    internal const int kMinVisibleDepth = 1; // Minimum height is 1 row (top-level events always visible)
    internal NativeHashMap<ulong, int> m_collectedVisibleDepths; // Merged visible depths from all frame jobs
    internal float m_totalThreadHeight; // Total height in bar units (calculated by CalculateThreadOffsets)
    internal DependenciesInfo m_dependencyInfo;
    internal TimelineSettings m_settings;
    internal SelectedFrameRange m_selectedFrameRange;
    internal MouseState m_mouseData;
    // We use this to track which frames from the profiler driver that we have processed and cached
    internal int m_nextFrame = -1;
    internal int m_currentFrameIndex = -1;
    internal int m_prevFrameIndex = -1;
    internal int m_frameCounter = 0;
    //internal FontAsset m_font;
    internal bool m_isCacheEnabled;
    internal DragRange m_dragRange;
    private float m_barSize = JobsProfilerSettings.BarHeight;

    /// Order in which to draw the thread groups
    private NativeArray<ThreadGroupInfo> m_threadGroupOrder;
    private NativeList<GenerateMeshContext> m_activeMeshGenerators;
    private NativeArray<PrimitiveRenderer> m_primitiveRenderers;
    private Filter m_filter;
    private SmoothEventTransition m_posTransition = new SmoothEventTransition();
    private bool m_disposed = false;

    private double m_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
    private const int PreAllocateTextElementCount = 1024;
    private const int kPrimitiveRendererCount = SelectedFrameRange.k_SelectionRange * 2 + 1;
    private const float kShowTimelineHeight = 200.0f;
    internal const int kTickRulerDistMin = 3;
    internal const int kTickRulerDistFull = 80; // distance between ruler tick marks where they gain full strength

    //internal List<ManualTextElement> m_barTexts;

    internal TimelineBarView(VisualElement parent, VisualElement rootView, FrameCache frameCache, Filter filter)
    {
        m_threads = new ThreadLabels();
        m_threads.SetThreadFoldCallback(OnThreadFoldToggled);
        m_threads.SetGroupFoldCallback(OnGroupFoldToggled);
        m_tickHandler = new TickHandler();
        m_zoomArea = new ZoomableArea();
        m_frameCache = frameCache;
        m_filter = filter;

        m_jobSelectionJobs = new List<NativeArray<JobSelection>>();
        m_activeMeshGenerators = new NativeList<GenerateMeshContext>(1, AllocatorManager.Persistent);
        m_primitiveRenderers = new NativeArray<PrimitiveRenderer>(kPrimitiveRendererCount, Allocator.Persistent);

        for (int i = 0; i < kPrimitiveRendererCount; ++i)
            m_primitiveRenderers[i] = new PrimitiveRenderer(1024);

        // Set up the default order for the thread groups
        // Main Thread is expanded by default, all others are folded
        m_threadGroupOrder = new NativeArray<ThreadGroupInfo>(new ThreadGroupInfo[]
        {
            new ThreadGroupInfo { name = "Main Thread", isFolded = false },
            new ThreadGroupInfo { name = "Job", isFolded = true },
            new ThreadGroupInfo { name = "Render Thread", isFolded = true },
            new ThreadGroupInfo { name = "Profiler", isFolded = true },
            new ThreadGroupInfo { name = "Scripting Threads", isFolded = true },
            new ThreadGroupInfo { name = "Background Job", isFolded = true },
            new ThreadGroupInfo { name = "Other Threads", isFolded = true },
        }, Allocator.Persistent);

        var visualTree = JobsProfilerResources.LoadVisualTreeAsset(JobsProfilerResources.TimelineUxml);
        var rootVisualElement = visualTree.Instantiate();

        rootVisualElement.style.flexGrow = 1;
        style.flexGrow = 1;
        AddToClassList("jobs-profiler-main-background");

        Add(rootVisualElement);

        // Apply semantic colors (theme colors are in UXML via USS classes)
        ApplySemanticColors(rootVisualElement);

        VisualElement tick_marks = this.Query<VisualElement>("tick_marks").First();
        m_verticalScroller = this.Query<Scroller>("timeline_vertical_scroll").First();
        m_verticalScroller.value = 0;
        m_verticalScroller.valueChanged += verticalScroll;

        m_horizontalScroller = this.Query<MinMaxSlider>("timeline_horizontal_scroll").First();
        m_horizontalScroller.value = new Vector2(10, 20);
        m_horizontalScroller.RegisterCallback<ChangeEvent<Vector2>>(horizontalScroll);

        m_paralleJobsNavigation = this.Query<DropdownField>("parallel_jobs_navigation").First();

        m_splitView = this.Query<TwoPaneSplitView>("split_view").First();
        m_splitView.fixedPaneInitialDimension = kShowTimelineHeight;
        m_splitView.RemoveAt(1);

        m_stats = new Stats(m_splitView, frameCache, m_filter);

        m_tickLabels = new TickLabels(m_tickHandler, m_zoomArea, tick_marks);

        m_jobInfoPanel = new JobsInfoPanel(this, m_stats.LabelStyles);

        m_jobInfoPanel.CompletedBy.RegisterCallback<PointerUpLinkTagEvent>(SelectCompletedBy);
        m_jobInfoPanel.ScheduledBy.RegisterCallback<PointerUpLinkTagEvent>(SelectScheduledBy);

        m_jobInfoPanel.CompletedBy.styleSheets.Add(m_stats.LabelStyles);
        m_jobInfoPanel.ScheduledBy.styleSheets.Add(m_stats.LabelStyles);

        m_settings = new TimelineSettings();
        m_threadOffsets = new NativeHashMap<ulong, ThreadPosition>(1, Allocator.Persistent);
        m_collectedVisibleDepths = new NativeHashMap<ulong, int>(64, Allocator.Persistent);
        m_isAnimating = false;

        m_dependencyInfo.startComplete = new NativeArray<StartCompleteInfo>(1, Allocator.Persistent);
        m_dependencyInfo.dependencyJobs = new NativeList<DependJobInfo>(32, Allocator.Persistent);
        m_dependencyInfo.dependantJobs = new NativeList<DependJobInfo>(32, Allocator.Persistent);
        m_isCacheEnabled = true;

        m_settings.mat = Unity.Mathematics.float4x4.identity;
        m_settings.windowRect.width = 100.0f;
        m_settings.windowRect.height = 100.0f;
        m_settings.barSize = JobsProfilerSettings.BarHeight;
        m_settings.invYBarSize = 1.0f - (1.0f / m_settings.barSize);

        m_tickHandler.SetTickModulos(Settings.TickModulos);

        m_zoomArea.hRangeLocked = false;
        m_zoomArea.vRangeLocked = false;
        m_zoomArea.vAllowExceedBaseRangeMax = false;
        m_zoomArea.vAllowExceedBaseRangeMin = false;
        m_zoomArea.hBaseRangeMin = 0;
        m_zoomArea.vBaseRangeMin = 0;
        m_zoomArea.vScaleMax = 1f;
        m_zoomArea.vScaleMin = 1f;
        m_zoomArea.vRangeMin = 0.0f;
        m_zoomArea.scaleWithWindow = true;
        m_zoomArea.margin = 10;
        m_zoomArea.topmargin = 0;
        m_zoomArea.bottommargin = 0;
        m_zoomArea.upDirection = ZoomableArea.YDirection.Negative;
        m_zoomArea.vZoomLockedByDefault = true;
        m_zoomArea.SetShownHRangeInsideMargins(0.0f, 16.6f); // inital start position is a 60 hz frame

        style.overflow = Overflow.Hidden;

        m_textRenderer = new TextRenderer();

        m_timeline = rootVisualElement.Query<VisualElement>("timeline").First();
        m_timeline.generateVisualContent += OnGenerateVisualContent;

        m_timeline.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
        m_timeline.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent, TrickleDown.TrickleDown);
        m_timeline.RegisterCallback<PointerUpEvent>(OnPointerUpEvent, TrickleDown.TrickleDown);
        m_timeline.RegisterCallback<WheelEvent>(OnWheelEvent, TrickleDown.TrickleDown);

        RegisterCallback<KeyDownEvent>(OnKeyDownEvent, TrickleDown.TrickleDown);

        focusable = true;

        VisualElement threads_view = this.Query<VisualElement>("threads_element").First();
        threads_view.Add(this.m_threads);

        m_dragRange.textLabel = new TextElement();
        m_dragRange.textLabel.style.position = Position.Absolute;
        m_dragRange.textLabel.AddToClassList("jobs-profiler-drag-label-background");
        m_dragRange.textLabel.style.borderRightWidth = 1;
        m_dragRange.textLabel.style.borderLeftWidth = 1;
        m_dragRange.textLabel.style.borderTopWidth = 1;
        m_dragRange.textLabel.style.borderBottomWidth = 1;

        m_dragRange.textLabel.text = "";
        m_dragRange.textLabel.visible = false;

        tick_marks.Add(m_dragRange.textLabel);

        m_textRenderer.LinkTextElements(m_timeline);

        m_settings.showScheduledBy = true;
        m_settings.showDependsOn = true;
        m_settings.showDependantOn =  true;
        m_settings.showCompletedByWait = true;
        m_settings.showCompletedByNoWait = true;

        m_settingsMenu = new SettingsMenu();

        VisualElement toggleContainer = rootView.Query<VisualElement>("jobs_info_toggle_container").First();
        m_jobsInfoToggle = new ToolbarToggle { text = "Jobs Info", value = true };
        m_jobsInfoToggle.RegisterValueChangedCallback(OnJobsInfoToggleChanged);
        toggleContainer.Add(m_jobsInfoToggle);

        parent.Add(this);
    }

    private void verticalScroll(float value)
    {
        // Only updated if needed
        if (!Mathf.Approximately(m_zoomArea.temp_y, -value))
            m_zoomArea.temp_y = -value;
    }
    private void horizontalScroll(ChangeEvent<Vector2> evt)
    {
        m_zoomArea.UpdateHorizontalScrolling(evt.newValue);
    }

    internal void ClearData()
    {
        WaitPreviousFrameJobs();
        m_textRenderer.HideLables();
        m_tickLabels.HideLabels();
        m_stats.ClearData();
        m_jobInfoPanel.Deactivate();
        m_jobInfoPanel.ClearHistory();
        m_nextFrame = -1;
    }

    void OnJobsInfoToggleChanged(ChangeEvent<bool> evt)
    {
        bool show = evt.newValue;

        // Stats table
        m_stats.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        // Hide the TwoPaneSplitView drag handle too
        var dragHandle = m_splitView.Q(className: "unity-two-pane-split-view__dragline-anchor");
        if (dragHandle != null)
            dragHandle.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

        // Arrow flags are gated by the master toggle in Update() every frame, so no
        // need to set them here.

        // JobsInfoPanel dependency section
        m_jobInfoPanel.SetDependencyInfoVisible(show);

        m_settingsMenu.SetArrowsEnabled(show);

        MarkDirtyRepaint();
    }

    /// <summary>
    /// Repaint the legend swatches after the color-blind setting changes.
    /// </summary>
    internal void RefreshSemanticColors()
    {
        ApplySemanticColors(this);
    }

    /// <summary>
    /// Apply semantic colors to UXML elements after loading.
    /// Theme colors are applied via USS classes defined in the UXML.
    /// </summary>
    void ApplySemanticColors(VisualElement root)
    {
        bool colorBlind = JobsProfilerSettings.IsColorBlindMode;

        var scheduledByColor = root.Q<VisualElement>("scheduled_by_color");
        if (scheduledByColor != null)
            scheduledByColor.style.backgroundColor = (Color)JobsProfilerColors.GetScheduleColor(colorBlind);

        var completedByColor = root.Q<VisualElement>("completed_by_color");
        if (completedByColor != null)
            completedByColor.style.backgroundColor = (Color)JobsProfilerColors.GetCompletedWaitColor(colorBlind);

        var dependsOnColors = root.Query<VisualElement>("depends_on_color").ToList();
        foreach (var dependsOnColor in dependsOnColors)
            dependsOnColor.style.backgroundColor = (Color)JobsProfilerColors.GetDependencyColor(colorBlind);
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        m_disposed = true;

        WaitPreviousFrameJobs();

        m_dependencyInfo.startComplete.Dispose();
        m_dependencyInfo.dependencyJobs.Dispose();
        m_dependencyInfo.dependantJobs.Dispose();
        m_threadGroupOrder.Dispose();
        m_threadOffsets.Dispose();
        m_collectedVisibleDepths.Dispose();

        foreach (var selection in m_jobSelectionJobs)
            selection.Dispose();

        foreach (var primitiveRenderer in m_primitiveRenderers)
            primitiveRenderer.Dispose();

        m_primitiveRenderers.Dispose();
        m_activeMeshGenerators.Dispose();

        m_textRenderer.Dispose();
        m_stats.Dispose();
    }

    internal void SetCurrentFrame(int currentFrame)
    {
        m_nextFrame = currentFrame;

        m_stats.SelectFrame(currentFrame);
    }

    void UpdateDirectionLabel(UnityEngine.UIElements.Button button, in DependJobInfo info)
    {
        if (info.eventIndex == -1)
        {
            button.text = "...";
            button.SetEnabled(false);
        }
        else
        {
            button.text = m_frameCache.GetEventStringForFrame(info.frameIndex, info.eventIndex);
            button.SetEnabled(true);
        }
    }

    void UpdateScheduleCompeletedBy(Label label, int frameIndex, int eventIndex)
    {
        if (eventIndex < 0 || frameIndex < 0)
        {
            label.text = "N/A";
            label.SetEnabled(false);
        }
        else
        {
            label.text = String.Format("<link=\"0\"><color={0}>{1}</color></link>",
                    JobsProfilerSettings.LinkColorHex, m_frameCache.GetEventStringForFrame(frameIndex, eventIndex));
            label.SetEnabled(true);
        }
    }

    internal void Update()
    {
        // calculate deltaTime
        double currentTimeSinceStartup = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(currentTimeSinceStartup - m_lastTimeSinceStartup);
        m_lastTimeSinceStartup = currentTimeSinceStartup;

        WaitPreviousFrameJobs();

        // Update fold and visible depth animations (must be after WaitPreviousFrameJobs which collects visible depths)
        UpdateAnimations(deltaTime);

        if (m_nextFrame == -1)
        {
            // This is to make sure we clear out the old view
            m_tickLabels.Update(m_tickHandler, m_zoomArea, m_nextFrame, m_dragRange.textLabel, 0.0f);
            m_timeline.MarkDirtyRepaint();
            m_threads.Hide();
            return;
        }

        m_threads.Show();
        m_filter.Update(m_nextFrame);
        m_stats.Update();

        // Sync settings from the settings menu (arrow flags are gated by the Jobs Info master toggle)
        bool jobsInfoOn = m_jobsInfoToggle.value;
        m_settings.zoomOnEventFocus = m_settingsMenu.ZoomOnEventFocus;
        m_settings.showScheduledBy        = jobsInfoOn;
        m_settings.showDependsOn          = jobsInfoOn && m_settingsMenu.ShowDependsOn;
        m_settings.showDependantOn        = jobsInfoOn && m_settingsMenu.ShowDependantOn;
        m_settings.showCompletedByWait    = jobsInfoOn && m_settingsMenu.ShowCompletedByWait;
        m_settings.showCompletedByNoWait  = jobsInfoOn && m_settingsMenu.ShowCompletedByNoWait;
        m_settings.showFullDependencyChain = m_settingsMenu.ShowFullDependencyChain;
        m_settings.zoomOnEventHover = m_settingsMenu.ZoomOnEventHover;
        m_settings.showFoldedGroupPreview = m_settingsMenu.ShowFoldedGroupPreview;
        m_settings.colorBlindMode = JobsProfilerSettings.IsColorBlindMode;
        m_settings.barSize = m_barSize;
        m_settings.invYBarSize = 1.0f - (1.0f / m_settings.barSize);

        m_selectedFrameRange.start = Math.Max(m_nextFrame - SelectedFrameRange.k_SelectionRange, ProfilerDriver.firstFrameIndex);
        m_selectedFrameRange.active = m_nextFrame;
        m_selectedFrameRange.end = Math.Min(m_nextFrame + SelectedFrameRange.k_SelectionRange, ProfilerDriver.lastFrameIndex + 1);

        CacheFrameData(m_nextFrame);
        UpdateZoom();
        m_timeline.MarkDirtyRepaint();

        float frameTime = 0.0f;

        FrameData frame;

        if (m_frameCache.GetFrame(m_nextFrame, out frame))
        {
            // Use the total height calculated by CalculateThreadOffsets
            float totalThreadHeightPixels = m_totalThreadHeight * m_barSize;

            m_threads.Update(m_threadOffsets, frame, m_threadGroupOrder, m_settings.mat);
            m_verticalScroller.highValue = totalThreadHeightPixels;
            m_verticalScroller.Adjust(0.25f);
            m_zoomArea.hBaseRangeMax = frame.info[0].frameTime;
            m_zoomArea.vBaseRangeMax = totalThreadHeightPixels;
            frameTime = frame.info[0].frameTime;
        }

        ClickLinkEvent clickStatEvent = m_stats.GetClickLinkEvent();
        ClickLinkEvent clickOrHoverEvent = m_jobInfoPanel.GetClickOrHoverEvent();

        if (clickStatEvent.jobId != 0)
            UpdateClickEvent(clickStatEvent);

        if (clickOrHoverEvent.jobId != 0)
            UpdateClickEvent(clickOrHoverEvent);

        ScheduleJobs();

        m_tickLabels.Update(m_tickHandler, m_zoomArea, m_nextFrame, m_dragRange.textLabel, frameTime);

        bool isHistoryNav = false;
        ClickLinkEvent historyNavEvent = m_jobInfoPanel.GetHistoryNavigation();
        if (historyNavEvent.jobId != 0)
        {
            isHistoryNav = true;
            UpdateClickEvent(historyNavEvent);
        }

        var t = m_jobSelection;

        // TODO: Clean this up a bit
        if ((t.updatedSelection || t.updatedFocus) && t.state == JobSelection.State.Selected && !t.hover)
        {
            m_jobInfoPanel.JobName.text = m_frameCache.GetEventStringForFrame(t.frameIndex, t.eventIndex);
            m_jobInfoPanel.JobTime.text = String.Format("{0:F5} ms", m_frameCache.GetTimeForEvent(t.frameIndex, t.eventIndex));

            m_jobInfoPanel.Activate();

            m_jobInfoPanel.Update(
                m_dependencyInfo.dependencyJobs,
                m_dependencyInfo.dependantJobs,
                t,
                m_frameCache);

            StartCompleteInfo startCompInfo = m_dependencyInfo.startComplete[0];

            UpdateScheduleCompeletedBy(m_jobInfoPanel.ScheduledBy,
                startCompInfo.startFrame, startCompInfo.startEventParentIndex);

            UpdateScheduleCompeletedBy(m_jobInfoPanel.CompletedBy,
                startCompInfo.completeFrame, startCompInfo.completeEventIndex);

            FrameData frameData;

            if (m_frameCache.GetFrame(t.frameIndex, out frameData))
                m_stats.SelectRowByMarkerId(frameData.events[t.eventIndex].markerId);

            if (!isHistoryNav)
                m_jobInfoPanel.PushHistory(t.frameIndex, t.eventIndex);
            else
                m_jobInfoPanel.RestoreHistoryTab();
        }
        else if (t.state == JobSelection.State.Default)
        {
            m_jobInfoPanel.Deactivate();
        }
        else if (t.jobHandle.ToUlong() == 0)
        {
            m_jobInfoPanel.ClearLists();
        }

        if (m_posTransition.Update(deltaTime))
        {
            var pos = m_posTransition.Pos;
            if (m_posTransition.Fixed)
                FocusOnPosition(pos.x, pos.y, pos.z, true);
            else
                FocusOnPosition(pos.x, pos.y, pos.z, m_settings.zoomOnEventHover);
        }

        if (m_dragRange.show)
        {
            float t0 = m_dragRange.startTime;
            float t1 = m_dragRange.endTime;

            // Sort so we know which value is the start and which is the end
            if (t0 > t1)
            {
                float tmp = t0;
                t0 = t1;
                t1 = tmp;
            }

            float xStart = m_zoomArea.TimeToPixel(t0, m_zoomArea.drawRect);
            float xEnd = m_zoomArea.TimeToPixel(t1, m_zoomArea.drawRect);

            float value = t1 - t0;

            if (value < float.Epsilon)
            {
                m_dragRange.textLabel.visible = false;
            }
            else
            {
                String text = String.Format("{0} ms", value);
                Vector2 textSize = m_dragRange.textLabel.MeasureTextSize(text, 0.0f, MeasureMode.Undefined, 0.0f, MeasureMode.Undefined);
                float selectionWidth = xEnd - xStart;
                float textCenter = (selectionWidth - textSize.x) / 2.0f;

                m_dragRange.textLabel.visible = true;
                m_dragRange.textLabel.text = text;
                m_dragRange.textLabel.style.translate = new StyleTranslate(new Translate(new Length(xStart + textCenter), new Length(0.0f), 0.0f));
            }
        }
    }
    internal void UpdateClickEvent(ClickLinkEvent evnt)
    {
        if (evnt.frameIndex >= 0 && evnt.eventIndex >= 0)
        {
            SetCurrentFrame(evnt.frameIndex);
            DependJobInfo depInfo = new DependJobInfo();
            depInfo.eventIndex = evnt.eventIndex;
            depInfo.frameIndex = evnt.frameIndex;
            UpdateSelectedEvent(depInfo, evnt.hover);
        }
    }

    void UpdateSelectedEvent(in DependJobInfo depInfo, bool hover)
    {
        JobSelection selection = m_jobSelection;
        InternalJobHandle jobHandle = depInfo.jobHandle;

        selection.state = JobSelection.State.Selected;
        selection.frameIndex = depInfo.frameIndex;
        selection.eventIndex = depInfo.eventIndex;

        // If we don't have a valid job handle, we try to find it
        if (!jobHandle.IsValid())
            jobHandle = m_frameCache.GetJobHandleForFrameEvent(depInfo.frameIndex, depInfo.eventIndex);

        selection.jobHandle = jobHandle;
        selection.updatedSelection = true;
        selection.updatedFocus = true;
        selection.hover = hover;

        m_jobSelection = selection;
        FocusOnElement(selection.frameIndex, selection.eventIndex, m_settings.zoomOnEventFocus, false, selection.hover, TransitionMode.Smooth);
    }

    void SelectScheduledBy(PointerUpLinkTagEvent evt)
    {
        int eventIndex = m_dependencyInfo.startComplete[0].startEventIndex;
        int frameIndex = m_dependencyInfo.startComplete[0].startFrame;
        FocusOnElement(frameIndex, eventIndex, m_settings.zoomOnEventFocus, false, false, TransitionMode.Smooth);
    }
    void SelectCompletedBy(PointerUpLinkTagEvent evt)
    {
        int eventIndex = m_dependencyInfo.startComplete[0].completeEventIndex;
        int frameIndex = m_dependencyInfo.startComplete[0].completeFrame;
        FocusOnElement(frameIndex, eventIndex, m_settings.zoomOnEventFocus, false, false, TransitionMode.Smooth);
    }

    void CacheFrameData(int currentFrameIndex)
    {
        if (!m_isCacheEnabled)
            return;

        using (var frameData = ProfilerDriver.GetRawFrameDataView(currentFrameIndex, 0))
        {
            if (frameData != null && frameData.valid)
            {
                m_prevFrameIndex = m_currentFrameIndex;
                m_currentFrameIndex = currentFrameIndex;
            }
        }

        int startRange = Math.Max(currentFrameIndex - 3, ProfilerDriver.firstFrameIndex);
        int endRange = Math.Min(currentFrameIndex + 3, ProfilerDriver.lastFrameIndex + 1);

        m_frameCache.CacheRange(startRange, endRange);
    }

    /// Get the ranges of frames we want to render/update
    NativeList<FrameDataIndex> GetActiveFrameRange()
    {
        int frameCount = m_frameCache.GetNumberOfFrames();

        if (frameCount == 0)
            return new NativeList<FrameDataIndex>();

        int start = m_selectedFrameRange.start;
        int end = m_selectedFrameRange.end;
        int totalFrameCount = end - start;

        if (totalFrameCount <= 0)
            return new NativeList<FrameDataIndex>();

        float timeStart = 0.0f;

        for (int i = start; i < end; ++i)
        {
            FrameData frame;

            if (i == m_selectedFrameRange.active)
                break;

            if (!m_frameCache.GetFrame(i, out frame))
                continue;

            timeStart -= frame.info[0].frameTime;
        }

        var frames = new NativeList<FrameDataIndex>(totalFrameCount, Allocator.Temp);

        for (int i = 0; i < totalFrameCount; ++i)
        {
            int frameIndex = start + i;
            FrameData frame;

            if (!m_frameCache.GetFrame(frameIndex, out frame))
                continue;

            float frameTime = frame.info[0].frameTime;

            // Opacity value for rendering the not selected frames more faded toward background
            float fade = (frameIndex == m_selectedFrameRange.active) ? 0.0f : 0.8f;

            var fi = new FrameIndex
            {
                time = timeStart,
                fade = fade,
                frameCacheIndex = frameIndex,
                frameTime = frameTime,
            };

            frames.Add(new FrameDataIndex
            {
                index = fi,
                data = frame,
            });

            timeStart += frameTime;
        }

        return frames;
    }

    struct ThreadGroupFrameIndex
    {
        internal ThreadGroup group;
        internal int frameIndex;
    }

    struct ThreadTempInfo
    {
        internal string name;
        internal ulong threadId;
        internal int depth;
    }

    /// <summary>
    /// Returns a list of thread groups that match the given name and the frame index they are in.
    /// </summary>
    private List<ThreadGroupFrameIndex> GetGroups(in NativeList<FrameDataIndex> frames, in FixedString128Bytes groupName)
    {
        List<ThreadGroupFrameIndex> groups = new List<ThreadGroupFrameIndex>(frames.Length);

        for (int i = 0; i < frames.Length; ++i)
        {
            var frame = frames[i].data;

            foreach (var group in frame.threadGroups)
            {
                if (group.name == groupName)
                {
                    groups.Add(new ThreadGroupFrameIndex
                    {
                        group = group,
                        frameIndex = i,
                    });
                    break;
                }
            }
        }

        return groups;
    }

    (bool isFolded, float animationProgress) GetThreadFoldState(ulong threadId, FixedString128Bytes groupName, in NativeHashMap<ulong, ThreadPosition> preservedStates)
    {
        // Default: fold all threads except Main Thread
        bool isMainThread = groupName.ToString() == "Main Thread";
        bool isFolded = !isMainThread;
        float animationProgress = isMainThread ? 1.0f : 0.0f;

        // Preserve existing user state if available, but not from Preview mode.
        // Preview mode sets isFolded=true for compact display, which shouldn't
        // persist when the group is unfolded.
        if (preservedStates.TryGetValue(threadId, out ThreadPosition existingPos) &&
            existingPos.visibility != ThreadVisibility.Preview)
        {
            isFolded = existingPos.isFolded;
            animationProgress = existingPos.animationProgress;
        }

        return (isFolded, animationProgress);
    }

    void UpdateAnimations(float deltaTime)
    {
        const float animationSpeed = 10.0f; // Complete animation in ~0.1 seconds
        bool anyAnimating = false;

        var keys = m_threadOffsets.GetKeyArray(Allocator.Temp);
        for (int i = 0; i < keys.Length; i++)
        {
            ulong threadId = keys[i];
            if (m_threadOffsets.TryGetValue(threadId, out ThreadPosition threadPos))
            {
                bool threadAnimating = false;

                // Fold/unfold animation
                float targetProgress = threadPos.isFolded ? 0.0f : 1.0f;
                if (math.abs(threadPos.animationProgress - targetProgress) > 0.01f)
                {
                    float step = animationSpeed * deltaTime;
                    threadPos.animationProgress = math.lerp(threadPos.animationProgress, targetProgress, step);
                    if (math.abs(threadPos.animationProgress - targetProgress) < 0.01f)
                        threadPos.animationProgress = targetProgress;
                    threadAnimating = true;
                }

                // Visible depth animation - only animate if we have collected depth data for this thread.
                // If the thread scrolled out of view, we won't have data for it and should preserve
                // its current visible depth to prevent threads from jumping when scrolling.
                if (m_collectedVisibleDepths.TryGetValue(threadId, out int collected))
                {
                    float targetVisibleDepth = math.clamp(collected, kMinVisibleDepth, threadPos.depth);

                    if (math.abs(threadPos.visibleDepth - targetVisibleDepth) > 0.01f)
                    {
                        float step = animationSpeed * deltaTime;
                        threadPos.visibleDepth = math.lerp(threadPos.visibleDepth, targetVisibleDepth, step);
                        if (math.abs(threadPos.visibleDepth - targetVisibleDepth) < 0.01f)
                            threadPos.visibleDepth = targetVisibleDepth;
                        threadAnimating = true;
                    }
                }

                if (threadAnimating)
                {
                    m_threadOffsets[threadId] = threadPos;
                    anyAnimating = true;
                }
            }
        }
        keys.Dispose();

        m_isAnimating = anyAnimating;
    }

    void OnGroupFoldToggled(FixedString128Bytes groupName)
    {
        // Find the group and toggle its fold state
        for (int i = 0; i < m_threadGroupOrder.Length; i++)
        {
            if (m_threadGroupOrder[i].name.Equals(groupName))
            {
                var group = m_threadGroupOrder[i];
                group.isFolded = !group.isFolded;
                m_threadGroupOrder[i] = group;
                break;
            }
        }
    }

    void OnThreadFoldToggled(ulong threadId)
    {
        // Toggle fold state and start animation
        if (m_threadOffsets.TryGetValue(threadId, out ThreadPosition threadPos))
        {
            threadPos.isFolded = !threadPos.isFolded;
            // animationProgress stays at current value, will be updated in UpdateAnimations
            m_threadOffsets[threadId] = threadPos;
            m_isAnimating = true;
        }
    }

    /// <summary>
    /// Calculates the preview heights (in bar units) for a folded group based on thread count.
    /// Returns the bar height (for rendering) and offset step (bar + spacing for positioning).
    /// </summary>
    void CalculateFoldedPreviewHeight(int threadCount, out float barHeight, out float offsetStep)
    {
        const float kMinPreviewPixels = 2.0f;
        const float kPreviewSpacingPixels = 1.0f;

        if (threadCount == 1)
        {
            barHeight = 1.0f;
            offsetStep = 1.0f;
        }
        else
        {
            barHeight = kMinPreviewPixels / m_barSize;
            offsetStep = (kMinPreviewPixels + kPreviewSpacingPixels) / m_barSize;
        }
    }

    // TODO: Move to job
    /// <summary>
    /// This constructs the thread offsets for each frame. This code is a bit complex because it has to deal
    /// with the fact that a frame can have different number of threads. To make things a bit easier we calculate a hash
    /// for each group. If the hash matches along all the frames we assume they are the same and we can do a fast path when
    /// looping over all of the groups. Otherwise we need to "diff" the thread data between frames to find the differences.
    /// </summary>
    float CalculateThreadOffsets(in NativeList<FrameDataIndex> frames)
    {
        // Preserve fold states and animation progress before clearing
        var preservedStates = new NativeHashMap<ulong, ThreadPosition>(m_threadOffsets.Count, Allocator.Temp);
        foreach (var kvp in m_threadOffsets)
        {
            preservedStates.TryAdd(kvp.Key, kvp.Value);
        }

        m_threadOffsets.Clear();

        float threadOffset = 0.0f;

        for (int groupIndex = 0; groupIndex < m_threadGroupOrder.Length; groupIndex++)
        {
            var group = m_threadGroupOrder[groupIndex];
            List<ThreadGroupFrameIndex> groupsIndex = GetGroups(frames, group.name);

            if (groupsIndex.Count == 0)
                continue;

            // Calculate max thread count across all frames for this group
            // This is needed early to determine if this is a single-thread group
            int maxThreadCountForGroup = 0;
            for (int i = 0; i < groupsIndex.Count; ++i)
            {
                int threadCount = groupsIndex[i].group.arrayEnd - groupsIndex[i].group.arrayIndex;
                if (threadCount > maxThreadCountForGroup)
                    maxThreadCountForGroup = threadCount;
            }

            bool isSingleThreadGroup = maxThreadCountForGroup == 1;

            // Store the offset for this group so we can position its label
            group.offset = threadOffset;
            m_threadGroupOrder[groupIndex] = group;

            // Reserve space for the group header label only for multi-thread groups
            // For single-thread groups, the group label aligns with the thread bars
            if (!isSingleThreadGroup)
                threadOffset += 1.0f;

            // If group is folded, either skip or show preview based on setting
            if (group.isFolded)
            {
                if (m_settings.showFoldedGroupPreview)
                {
                    // Get thread count from the frame with most threads
                    int previewThreadCount = 0;
                    int previewFrameIndex = 0;
                    for (int i = 0; i < groupsIndex.Count; ++i)
                    {
                        int threadCount = groupsIndex[i].group.arrayEnd - groupsIndex[i].group.arrayIndex;
                        if (threadCount > previewThreadCount)
                        {
                            previewThreadCount = threadCount;
                            previewFrameIndex = i;
                        }
                    }

                    // Calculate preview heights per thread (bar height for rendering, offset step for positioning)
                    CalculateFoldedPreviewHeight(previewThreadCount, out float barHeight, out float offsetStep);

                    // Add threads with preview visibility
                    int frameIndex = groupsIndex[previewFrameIndex].frameIndex;
                    int arrayStart = groupsIndex[previewFrameIndex].group.arrayIndex;

                    for (int t = 0; t < previewThreadCount; ++t)
                    {
                        ulong threadId = frames[frameIndex].data.threads[arrayStart + t].threadId;

                        m_threadOffsets[threadId] = new ThreadPosition
                        {
                            visibility = ThreadVisibility.Preview,
                            depth = 1,
                            offset = threadOffset,
                            isFolded = true,
                            animationProgress = 0.0f,
                            previewHeight = barHeight,
                        };

                        threadOffset += offsetStep;
                    }
                }
                else if (isSingleThreadGroup)
                {
                    // For single-thread groups without preview, still reserve 1.0f space
                    // to ensure proper separator positioning for the next group
                    threadOffset += 1.0f;
                }
                continue;
            }

            // Check if the groups has the same hash
            int hash = groupsIndex[0].group.groupNamesHash;
            int maxThreadCount = groupsIndex[0].group.arrayEnd - groupsIndex[0].group.arrayIndex;
            int groupWithMostThreads = 0;

            for (int i = 1; i < groupsIndex.Count; ++i)
            {
                int threadCount = groupsIndex[i].group.arrayEnd - groupsIndex[i].group.arrayIndex;

                if (threadCount > maxThreadCount)
                {
                    maxThreadCount = threadCount;
                    groupWithMostThreads = i;
                }

                if (groupsIndex[i].group.groupNamesHash != hash)
                {
                    hash = -1;
                    break;
                }
            }

            if (hash != -1)
            {
                for (int t = 0; t < maxThreadCount; ++t)
                {
                    int maxDepth = 0;

                    int frameIndex = groupsIndex[groupWithMostThreads].frameIndex;
                    int arrayStart = groupsIndex[groupWithMostThreads].group.arrayIndex;
                    ulong threadId = frames[frameIndex].data.threads[arrayStart + t].threadId;

                    for (int i = 0; i < groupsIndex.Count; ++i)
                    {
                        frameIndex = groupsIndex[i].frameIndex;
                        arrayStart = groupsIndex[i].group.arrayIndex;

                        int depth = frames[frameIndex].data.threads[arrayStart + t].maxDepth;
                        maxDepth = math.max(maxDepth, depth);
                    }

                    // Get or preserve existing fold state and animation progress
                    var (isFolded, animationProgress) = GetThreadFoldState(threadId, group.name, preservedStates);

                    // Get previous visible depth (animated value) or default to full depth
                    float prevVisibleDepth = preservedStates.TryGetValue(threadId, out var existing)
                        ? existing.visibleDepth
                        : 0;
                    float effectiveMaxDepth = prevVisibleDepth > 0 ? prevVisibleDepth : (float)maxDepth;

                    // Lerp between folded (1) and expanded (effectiveMaxDepth) based on animation progress
                    float lerpedDepth = math.lerp(1.0f, effectiveMaxDepth, animationProgress);

                    m_threadOffsets[threadId] = new ThreadPosition
                    {
                        visibility = ThreadVisibility.Visible,
                        depth = maxDepth,
                        offset = threadOffset,
                        isFolded = isFolded,
                        animationProgress = animationProgress,
                        visibleDepth = prevVisibleDepth > 0 ? prevVisibleDepth : (float)maxDepth,
                    };

                    threadOffset += lerpedDepth;
                }
            }
            else
            {
                // If we have uneven number of threads between the frames we take the following actions
                // 1. Create a dictionary over all threads in all groups and while doing that calculate
                //    the max depth for each thread.
                // 2. Create a new list with all threads and sort by name.
                // 3. Loop over all entries and create the thread offsets lookup

                Dictionary<ulong, ThreadTempInfo> threads = new Dictionary<ulong, ThreadTempInfo>(maxThreadCount);
                List<ThreadTempInfo> sortData = new List<ThreadTempInfo>(maxThreadCount);

                foreach (var groups in groupsIndex)
                {
                    int arrayStart = groups.group.arrayIndex;
                    int threadCount = groups.group.arrayEnd - arrayStart;
                    int frameIndex = groups.frameIndex;

                    for (int i = 0; i < threadCount; ++i)
                    {
                        ThreadTempInfo buildInfo;
                        ThreadInfo threadInfo = frames[frameIndex].data.threads[arrayStart + i];

                        if (threads.TryGetValue(threadInfo.threadId, out buildInfo))
                        {
                            buildInfo.depth = Math.Max(buildInfo.depth, threadInfo.maxDepth);
                            threads[threadInfo.threadId] = buildInfo;
                        }
                        else
                        {
                            threads[threadInfo.threadId] = new ThreadTempInfo
                            {
                                name = threadInfo.name.ToString(),
                                depth = threadInfo.maxDepth,
                                threadId = threadInfo.threadId
                            };
                        }
                    }
                }

                // 2. Create a new list and sort
                foreach (var threadInfo in threads)
                    sortData.Add(threadInfo.Value);

                sortData.Sort(delegate (ThreadTempInfo x, ThreadTempInfo y)
                {
                    return EditorUtility.NaturalCompare(x.name, y.name);
                });

                // 3. Loop over all entries and create the thread offsets lookup
                foreach (var threadInfo in sortData)
                {
                    // Get or preserve existing fold state and animation progress
                    var (isFolded, animationProgress) = GetThreadFoldState(threadInfo.threadId, group.name, preservedStates);

                    // Get previous visible depth (animated value) or default to full depth
                    float prevVisibleDepth = preservedStates.TryGetValue(threadInfo.threadId, out var existing)
                        ? existing.visibleDepth
                        : 0;
                    float effectiveMaxDepth = prevVisibleDepth > 0 ? prevVisibleDepth : (float)threadInfo.depth;

                    // Lerp between folded (1) and expanded (effectiveMaxDepth) based on animation progress
                    float lerpedDepth = math.lerp(1.0f, effectiveMaxDepth, animationProgress);

                    m_threadOffsets[threadInfo.threadId] = new ThreadPosition
                    {
                        visibility = ThreadVisibility.Visible,
                        depth = threadInfo.depth,
                        offset = threadOffset,
                        isFolded = isFolded,
                        animationProgress = animationProgress,
                        visibleDepth = prevVisibleDepth > 0 ? prevVisibleDepth : (float)threadInfo.depth,
                    };

                    threadOffset += lerpedDepth;
                }
            }
        }

        preservedStates.Dispose();

        return threadOffset; // Return total accumulated height
    }

    void UpdateZoom()
    {
        var parentStyle = m_timeline.resolvedStyle;
        float width = parentStyle.width;
        float height = parentStyle.height;

        // This is a workaround due to the fact that width can be nan/0.0 for a frame. We don't want to update
        // the zoom with these value as it causes problem futher on as the zoom area will be invalid.
        if (float.IsNaN(width) || width == 0.0 || float.IsNaN(height) || height == 0.0)
            return;

        Rect position = new Rect(0.0f, 0.0f, width, height);

        m_zoomArea.rect = position;
        m_zoomArea.SetShownVRange(m_zoomArea.shownArea.y, m_zoomArea.shownArea.y + m_zoomArea.drawRect.height);

        float localToGlobalScaleX = (position.width / m_zoomArea.shownArea.width);
        float localToGlobalScaleY = m_settings.barSize;
        float shownAreaX = m_zoomArea.shownArea.x;
        float lXoffset = -shownAreaX * localToGlobalScaleX;
        float timeOffset = 0.0f;
        float offset = lXoffset + timeOffset * localToGlobalScaleX;

        float3 t = new float3(offset, m_zoomArea.temp_y, -1.0f);
        float3 s = new float3(localToGlobalScaleX, localToGlobalScaleY, 1.0f);
        float4x4 mat = Unity.Mathematics.float4x4.TRS(t, Unity.Mathematics.quaternion.identity, s);

        m_settings.mat = mat;
        m_settings.windowRect = position;

        // Update tickhandler used for calculating the tick rendering area
        m_tickHandler.SetRanges(m_zoomArea.shownArea.xMin, m_zoomArea.shownArea.xMax, m_zoomArea.drawRect.xMin, m_zoomArea.drawRect.xMax);
        m_tickHandler.SetTickStrengths(kTickRulerDistMin, kTickRulerDistFull, true);
    }

    void SetVertices(MeshGenerationContext mgc, PrimitiveRenderer renderer)
    {
        if (renderer.vertices.Length == 0)
            return;

        var verts = renderer.vertices.AsArray();
        var indices = renderer.indices.AsArray();

        int vertexStart = 0;
        int indexStart = 0;

        foreach (var range in renderer.drawRanges)
        {
            vertexStart = range.vertexStart;
            indexStart = range.indexStart;

            int vertexCount = range.vertexCount;
            int indexCount = range.indexCount;

            var vertArray = new NativeSlice<Vertex>(verts, vertexStart, vertexCount);
            var indArray = new NativeSlice<ushort>(indices, indexStart, indexCount);

            MeshWriteData meshWriteData = mgc.Allocate(vertArray.Length, indArray.Length, null);

            meshWriteData.SetAllVertices(vertArray);
            meshWriteData.SetAllIndices(indArray);

            // when we fall out we want start to to be at the end of the range above meaning start + count
            vertexStart += vertexCount;
            indexStart += indexCount;
        }

        var vertexArray = new NativeSlice<Vertex>(verts, vertexStart, renderer.vertices.Length - vertexStart);
        var indexArray = new NativeSlice<ushort>(indices, indexStart, renderer.indices.Length - indexStart);

        MeshWriteData mwd = mgc.Allocate(vertexArray.Length, indexArray.Length, null);

        mwd.SetAllVertices(vertexArray);
        mwd.SetAllIndices(indexArray);
    }
    void RenderDependencies(MeshGenerationContext mgc, int frameIndex, float timeOffset)
    {
        FrameData data;

        if (!m_frameCache.GetFrame(frameIndex, out data))
            return;

        var jobSelection = m_jobSelection;

        // Make sure the selected job matches the filter that has been set. This has to happen before the
        // renderer is constructed, it owns persistent allocations that the early return would leak on every
        // repaint while a filtered-out job stays selected.
        if (m_filter.UseFilter && !m_filter.FilterIds.Contains(jobSelection.markerId))
            return;

        var renderer = new PrimitiveRenderer(data.events.Length);

        // Find if the Jobs group is folded
        bool isJobsGroupFolded = false;
        for (int i = 0; i < m_threadGroupOrder.Length; i++)
        {
            if (m_threadGroupOrder[i].name == "Job")
            {
                isJobsGroupFolded = m_threadGroupOrder[i].isFolded;
                break;
            }
        }

        var job = new GenerateDepenedicesMeshJob
        {
            m_renderer = renderer,
            m_frameIndex = frameIndex,
            m_settings = m_settings,
            m_threadOffsets = m_threadOffsets,
            m_threads = data.threads,
            m_events = data.events,
            m_selectedEvent = jobSelection,
            m_dependencyTable = data.dependencyTable,
            m_scheduledJobs = data.scheduledJobs,
            m_jobEventList = data.jobEventIndexList,
            m_handleIndexLookup = data.handleIndexLookup,
            m_eventHandleLookup = data.eventHandleLookup,
            m_handleToScheduleIndex = data.handleToScheduleIndex,
            m_dependencyJobs = m_dependencyInfo.dependencyJobs,
            m_depedantJobs = m_dependencyInfo.dependantJobs,
            m_timeOffset = timeOffset,
            m_startCompleteInfo = m_dependencyInfo.startComplete,
            m_jobFlows = data.jobFlows,
            m_isJobsGroupFolded = isJobsGroupFolded,
        };
        job.Schedule().Complete();

        SetVertices(mgc, renderer);

        renderer.Dispose();
    }

    GenerateMeshContext ScheduleMeshJob(int index, in FrameData data, FrameIndex frameIndex)
    {
        var renderer = m_primitiveRenderers[index];
        //renderer.UpdateSize(data.events.Length);
        //m_primitiveRenderers[index] = renderer;

        // Create per-job output hashmap for visible depth tracking
        var visibleDepthOutput = new NativeHashMap<ulong, int>(data.threads.Length, Allocator.TempJob);

        var job = new GenerateMeshJob
        {
            m_renderer = renderer,
            m_settings = m_settings,
            m_threads = data.threads,
            m_threadOffsets = m_threadOffsets,
            m_events = data.events,
            m_mouseState = m_mouseData,
            m_markerColors = JobsProfilerSettings.IsColorBlindMode ? data.catColorsColorBlind : data.catColors,
            m_jobSelection = m_jobSelectionJobs[index],
            m_frameIndex = frameIndex,
            m_eventHandleLookup = data.eventHandleLookup,
            m_idFilters = m_filter.FilterIds,
            m_useFilter = m_filter.UseFilter,
            m_jobFlows = data.jobFlows,
            m_backgroundColor = BackgroundColor,
            m_selectionBlendColor = JobsProfilerSettings.SelectionBlendColor,
            m_deselectionBlendColor = JobsProfilerSettings.DeselectionBlendColor,
            m_visibleDepthOutput = visibleDepthOutput,
        };

        return new GenerateMeshContext
        {
            renderer = renderer,
            jobHandle = job.Schedule(),
            visibleDepthOutput = visibleDepthOutput,
        };
    }

    enum FrameVisibility
    {
        Hidden,
        Partial,
        Fully,
    }

    /// <summary>
    /// Calculates the visibility of a frame. This is useful as most of the time a user will focus on a single frame and thus the other
    /// frames will be hidden. Using this we don't even schedule any text/render jobs for them which removes a bunch of overhead as these jobs
    /// would only cull already hidden events. Partial/Fully allows us to run different jobs that doesn't do any partial culling or not.
    /// </summary>
    /// <param name="frame"></param>
    /// <returns></returns>

    FrameVisibility CalcFrameVisibility(FrameIndex frame, Rect rect)
    {
        float start = frame.time;
        float end = start + frame.frameTime;

        float startPixel = m_zoomArea.TimeToPixel(start, rect);
        float endPixel = m_zoomArea.TimeToPixel(end, rect);

        float frameWidth = resolvedStyle.width;

        if (startPixel > 0.0f && endPixel < frameWidth)
            return FrameVisibility.Fully;
        else if (startPixel > frameWidth)
            return FrameVisibility.Hidden;
        else if (endPixel <= 0.0f)
            return FrameVisibility.Hidden;
        else
            return FrameVisibility.Partial;
    }

    void ScheduleJobs()
    {
        var frames = GetActiveFrameRange();

        if (!frames.IsCreated || frames.Length == 0)
        {
            m_textRenderer.HideLables();
            return;
        }

        m_totalThreadHeight = CalculateThreadOffsets(frames);

        m_activeMeshGenerators.Clear();

        // Update the job selection job data before scheduling any jobs
        var t = m_jobSelection;
        t.updatedSelection = false;

        NativeArray<JobHandle> textHandles = new NativeArray<JobHandle>(frames.Length, Allocator.Temp);

        for (int i = m_jobSelectionJobs.Count; i < frames.Length; ++i)
            m_jobSelectionJobs.Add(new NativeArray<JobSelection>(1, Allocator.Persistent));

        for (int i = 0; i < m_jobSelectionJobs.Count; ++i)
        {
            var jobTemp = m_jobSelectionJobs[i];
            jobTemp[0] = t;
        }

        Rect rect = m_zoomArea.drawRect;
        rect.x = rect.y = 0.0f;

        float screenStartY = m_zoomArea.temp_y;
        float screenEndY = screenStartY + resolvedStyle.height;

        JobHandle textHandle = new JobHandle();

        m_textRenderer.PreUpdate();

        var infos = new NativeArray<FrameIndex>(frames.Length, Allocator.TempJob);

        for (int i = 0; i < frames.Length; ++i)
        {
            var frameDataIndex = frames[i];
            var frameVisibility = CalcFrameVisibility(frameDataIndex.index, rect);

            // If the frame is fully hidden we don't schedule any jobs for it
            if (frameVisibility == FrameVisibility.Hidden)
                continue;

            infos[i] = frameDataIndex.index;

            // Calculate the threads here
            textHandle = m_textRenderer.ScheduleJob(
                (byte)i,
                m_settings,
                m_threadOffsets,
                m_filter.FilterIds,
                frameDataIndex,
                m_jobSelection,
                textHandle);

            var meshContext = ScheduleMeshJob(i, frameDataIndex.data, frameDataIndex.index);
            m_activeMeshGenerators.Add(meshContext);
        }

        m_textRenderer.PostUpdate(textHandle, infos, m_frameCache, BackgroundColor, JobsProfilerSettings.BarTextColor);

        frames.Dispose();
        infos.Dispose();
    }
    void WaitPreviousFrameJobs()
    {
        // If the UI decide to not generate any vertices it means that we need to wait for the jobs to finish and dispose the data here
        // Only collect visible depths if there are jobs to process (otherwise OnGenerateVisualContent already handled them)
        if (m_activeMeshGenerators.Length > 0)
        {
            foreach (var meshContext in m_activeMeshGenerators)
                meshContext.jobHandle.Complete();

            CollectVisibleDepths();
            m_activeMeshGenerators.Clear();
        }
    }

    /// <summary>
    /// Collects visible depth data from all completed mesh jobs and merges them.
    /// Takes the max visible depth for each thread across all frame jobs.
    /// </summary>
    void CollectVisibleDepths()
    {
        m_collectedVisibleDepths.Clear();

        foreach (var ctx in m_activeMeshGenerators)
        {
            foreach (var kvp in ctx.visibleDepthOutput)
            {
                if (m_collectedVisibleDepths.TryGetValue(kvp.Key, out int existing))
                {
                    if (kvp.Value > existing)
                        m_collectedVisibleDepths[kvp.Key] = kvp.Value;
                }
                else
                {
                    m_collectedVisibleDepths.TryAdd(kvp.Key, kvp.Value);
                }
            }
            ctx.visibleDepthOutput.Dispose();
        }
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        m_threads.DrawSeparators(mgc, resolvedStyle.width, resolvedStyle.height);

        foreach (var meshContext in m_activeMeshGenerators)
        {
            meshContext.jobHandle.Complete();
            SetVertices(mgc, meshContext.renderer);
        }

        CollectVisibleDepths();
        m_activeMeshGenerators.Clear();

        NativeList<FrameDataIndex> frames = GetActiveFrameRange();

        if (!frames.IsCreated)
            return;

        var t = m_jobSelection;

        foreach (var js in m_jobSelectionJobs)
        {
            var temp = js[0];
            t = temp;

            if (temp.updatedSelection || temp.updatedFocus)
                break;
        }

        // If the non-job selection state has been updated (such as a selection in the stats view) we use that instead of the job one.
        if (m_jobSelection.updatedFocus)
        {
            t = m_jobSelection;
            t.updatedFocus = false;
        }

        if (t.state == JobSelection.State.Selected)
        {
            foreach (var frameInfo in frames)
            {
                if (frameInfo.index.frameCacheIndex == t.frameIndex)
                {
                    RenderDependencies(mgc, frameInfo.index.frameCacheIndex, frameInfo.index.time);
                    break;
                }
            }
        }

        // If mouse was down to do a selection, but we didn't find any we clicked out side
        // and need to change the selection state back to Regular from JobSelected
        if (m_mouseData.isLeftDown == 1 && m_mouseData.isAltKeyDown == 0 && !t.updatedSelection)
        {
            switch (t.dragState)
            {
                case JobSelection.State.Default:
                    {
                        float startTime = m_zoomArea.PixelToTime(m_mouseData.pos.x, m_zoomArea.rect);
                        m_dragRange.startTime = startTime;
                        m_dragRange.endTime = startTime;
                        t.dragState = JobSelection.State.Drag;
                        break;
                    }

                case JobSelection.State.Drag:
                    {
                        m_dragRange.endTime = m_zoomArea.PixelToTime(m_mouseData.pos.x, m_zoomArea.rect);
                        m_dragRange.show = true;
                        break;
                    }
            }
        }
        else
        {
            switch (t.dragState)
            {
                case JobSelection.State.Drag:
                    {
                        t.dragState = JobSelection.State.EndDrag;
                        break;
                    }
                case JobSelection.State.EndDrag:
                    {
                        t.dragState = JobSelection.State.Default;
                        break;
                    }
            }

            // If no selection was made, we have ended the dragging and we clicked outside of anything
            // we should reset the selection state to default
            if (Math.Abs(m_dragRange.startTime - m_dragRange.endTime) < 0.5f &&
                t.state == JobSelection.State.Selected &&
                t.dragState == JobSelection.State.EndDrag)
            {
                t.state = JobSelection.State.Default;
            }
        }

        m_jobSelection = t;
        frames.Dispose();

        DrawGrid(mgc);
        DrawSelectionRange(mgc);
    }

    private void DrawGrid(MeshGenerationContext mgc)
    {
        Color tickColor = Color.white;
        tickColor.a = 0.1f;
        const float kTickRulerFatThreshold = 0.5f;     // size of ruler tick marks at which they begin getting fatter

        Rect rect = m_zoomArea.drawRect;
        rect.x = rect.y = 0.0f;

        int lineCount = 0;

        // TODO: Optimize
        for (int l = 0; l < m_tickHandler.tickLevels; l++)
        {
            var strength = m_tickHandler.GetStrengthOfLevel(l) * .9f;
            if (strength > kTickRulerFatThreshold)
            {
                var ticks = m_tickHandler.GetTicksAtLevel(l, true);
                lineCount += ticks.Length;
            }
        }

        if (lineCount == 0)
            return;

        var vertices = new NativeArray<Vertex>(lineCount * 4, Allocator.Temp);
        var indices = new NativeArray<ushort>(lineCount * 6, Allocator.Temp);

        int vertexPos = 0;
        int indexOffset = 0;

        Vertex v0 = new Vertex();
        Vertex v1 = new Vertex();
        Vertex v2 = new Vertex();
        Vertex v3 = new Vertex();

        v0.tint = tickColor;
        v1.tint = tickColor;
        v2.tint = tickColor;
        v3.tint = tickColor;

        // Draw tick markers of various sizes
        for (int l = 0; l < m_tickHandler.tickLevels; l++)
        {
            var strength = m_tickHandler.GetStrengthOfLevel(l) * .9f;
            if (strength > kTickRulerFatThreshold)
            {
                var ticks = m_tickHandler.GetTicksAtLevel(l, true);
                for (int i = 0; i < ticks.Length; i++)
                {
                    // Draw line
                    var time = ticks[i];
                    var x = m_zoomArea.TimeToPixel(time, rect);

                    float2 pos = new float2(x - 0.5f, 0.0f);
                    float2 size = pos + new float2(1.0f, rect.height);

                    v0.position.x = pos.x;
                    v0.position.y = pos.y;

                    v1.position.x = size.x;
                    v1.position.y = pos.y;

                    v2.position.x = size.x;
                    v2.position.y = size.y;

                    v3.position.x = pos.x;
                    v3.position.y = size.y;

                    // generate quad
                    vertices[vertexPos + 0] = v0;
                    vertices[vertexPos + 1] = v1;
                    vertices[vertexPos + 2] = v2;
                    vertices[vertexPos + 3] = v3;

                    indices[indexOffset + 0] = (ushort)(vertexPos + 0);
                    indices[indexOffset + 1] = (ushort)(vertexPos + 1);
                    indices[indexOffset + 2] = (ushort)(vertexPos + 2);

                    indices[indexOffset + 3] = (ushort)(vertexPos + 2);
                    indices[indexOffset + 4] = (ushort)(vertexPos + 3);
                    indices[indexOffset + 5] = (ushort)(vertexPos + 0);

                    vertexPos += 4;
                    indexOffset += 6;
                }
            }
        }

        MeshWriteData mwd = mgc.Allocate(lineCount * 4, lineCount * 6, null);

        mwd.SetAllVertices(vertices);
        mwd.SetAllIndices(indices);

        vertices.Dispose();
        indices.Dispose();
    }

    static private void DrawBox(Color32 color, float x, float y, float width, float height, MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;
        painter.strokeColor = color;
        painter.fillColor = color;
        painter.BeginPath();
        painter.MoveTo(new Vector2(x, y));
        painter.LineTo(new Vector2(x + width, y));
        painter.LineTo(new Vector2(x + width, y + height));
        painter.LineTo(new Vector2(x, y + height));
        painter.Fill();
    }

    private void DrawSelectionRange(MeshGenerationContext mgc)
    {
        if (!m_dragRange.show)
            return;

        float x0 = m_dragRange.startTime;
        float x1 = m_dragRange.endTime;

        // Sort so we know which value is the start and which is the end
        if (x0 > x1)
        {
            float temp = x0;
            x0 = x1;
            x1 = temp;
        }

        float xStart = m_zoomArea.TimeToPixel(x0, m_zoomArea.drawRect);
        float xEnd = m_zoomArea.TimeToPixel(x1, m_zoomArea.drawRect);

        float size = xEnd - xStart;

        // Only render if it's bigger than 1 pixel
        if (size < 1.0f)
            return;

        Color32 color = new Color32(127, 127, 127, 100);

        DrawBox(color, xStart, 0.0f, size, m_zoomArea.drawRect.height, mgc);
    }

    /// <summary>
    /// This will focus on a specific element in the timeline
    /// </summary>
    private void FocusOnElement(int frameIndex, int eventId, bool zoomArea, bool fullFrame, bool hover, TransitionMode transMode)
    {
        FrameData frameData;
        ThreadPosition threadPos;

        if (frameIndex < 0 || (!fullFrame && eventId < 0))
            return;

        if (!m_frameCache.GetFrame(frameIndex, out frameData))
            return;

        float startTime = 0.0f;
        float duration;
        threadPos.offset = 0;

        if (fullFrame)
        {
            startTime = 0.0f;
            duration = frameData.info[0].frameTime;
            threadPos.offset = -(int)(m_zoomArea.temp_y / m_settings.barSize);
        }
        else
        {
            ProfilingEvent temp = frameData.events[eventId];

            // The way we need to calculate it is if the frame is higher we need to go from the current frame
            // to the end frame, otherwise from the start frame to the current frame.
            if (frameIndex != m_currentFrameIndex)
            {
                // The way we need to calculate it is if the frame is higher we need to go from the current frame
                // to the end frame, otherwise from the start frame to the current frame.
                if (frameIndex > m_currentFrameIndex)
                {
                    for (int i = m_currentFrameIndex; i < frameIndex; ++i)
                    {
                        FrameData fd;
                        if (m_frameCache.GetFrame(i, out fd))
                            startTime += fd.info[0].frameTime;
                    }
                }
                else
                {
                    for (int i = m_currentFrameIndex - 1; i >= frameIndex; --i)
                    {
                        FrameData fd;
                        if (m_frameCache.GetFrame(i, out fd))
                            startTime -= fd.info[0].frameTime;
                    }
                }
            }

            // adjust for marker start time
            startTime += temp.startTime;
            duration = temp.time;
            ProfilingEvent profEvent = frameData.events[eventId];
            m_threadOffsets.TryGetValue(frameData.threads[profEvent.threadIndex].threadId, out threadPos);
        }

        if (transMode == TransitionMode.Fixed)
            m_posTransition.SetFixedTarget(startTime, duration, threadPos.offset);
        else
            m_posTransition.SetTarget(startTime, duration, threadPos.offset);

        /*
        if (zoomArea)
            m_zoomArea.SetShownHRangeInsideMargins(startTime - duration * 0.2f, startTime + duration * 1.2f);
        else
        {
            // center the selected time
            startTime += duration * 0.5f;
            // take current zoom width in both directions
            float offset = m_zoomArea.shownAreaInsideMargins.width * 0.5f;
            m_zoomArea.SetShownHRangeInsideMargins(startTime - offset, startTime + offset);
        }

        if (fullFrame)
            return;

        ProfilingEvent profEvent = frameData.events[eventId];

        // Calculate the y position of the thread
        if (m_threadOffsets.TryGetValue(frameData.threads[profEvent.threadIndex].threadId, out threadPos))
        {
            float height = m_timeline.resolvedStyle.height;
            m_zoomArea.temp_y = Mathf.Min(-(threadPos.offset * m_settings.barSize - height / 2.0f), 0.0f);
            m_verticalScroller.value = -m_zoomArea.temp_y;
        }
        */

        if (!hover)
            m_stats.SelectRowByMarkerId(frameData.events[eventId].markerId);
    }

    internal void FocusOnPosition(float startTime, float duration, float threadOffset, bool zoomArea)
    {
        if (zoomArea)
            m_zoomArea.SetShownHRangeInsideMargins(startTime - duration * 0.2f, startTime + duration * 1.2f);
        else
        {
            // center the selected time
            startTime += duration * 0.5f;
            // take current zoom width in both directions
            float offset = m_zoomArea.shownAreaInsideMargins.width * 0.5f;
            m_zoomArea.SetShownHRangeInsideMargins(startTime - offset, startTime + offset);
        }

        float height = m_timeline.resolvedStyle.height;
        m_zoomArea.temp_y = Mathf.Min(-(threadOffset * m_settings.barSize - height / 2.0f), 0.0f);
    }

    private void OnWheelEvent(WheelEvent evt)
    {
        m_zoomArea.ScrollWheelZoom(evt.localMousePosition);
    }

    private void OnPointerDownEvent(PointerDownEvent evt)
    {
        m_mouseData.pos.x = evt.localPosition.x;
        m_mouseData.pos.y = evt.localPosition.y;
        m_mouseData.isLeftDown = evt.button == 0 ? 1 : 0;
        m_mouseData.isRightDown = evt.button == 1 ? 1 : 0;
        m_mouseData.isAltKeyDown = evt.altKey == true ? 1 : 0;

        m_zoomArea.UpdatePointerDown(evt);
    }
    private void OnPointerUpEvent(PointerUpEvent evt)
    {
        m_mouseData.pos.x = evt.localPosition.x;
        m_mouseData.pos.y = evt.localPosition.y;
        m_mouseData.isAltKeyDown = evt.altKey == true ? 1 : 0;
        m_mouseData.isLeftDown = 0;
        m_mouseData.isRightDown = 0;

        m_zoomArea.UpdatePointerUpEvent(evt);
    }
    private void OnKeyDownEvent(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.F)
        {
            bool resetZoom = m_jobSelection.state == JobSelection.State.Default;
            FocusOnElement(m_jobSelection.frameIndex, m_jobSelection.eventIndex, true, resetZoom, false, TransitionMode.Fixed);
        }
        else if (evt.keyCode == KeyCode.A)
        {
            FocusOnElement(m_nextFrame, 0, true, true, false, TransitionMode.Fixed);
        }

        m_zoomArea.SetShownVRange(m_zoomArea.shownArea.y, m_zoomArea.shownArea.y + m_zoomArea.drawRect.height);
    }

    private void OnPointerMoveEvent(PointerMoveEvent evt)
    {
        m_mouseData.pos.x = evt.localPosition.x;
        m_mouseData.pos.y = evt.localPosition.y;
        m_mouseData.isAltKeyDown = evt.altKey == true ? 1 : 0;

        m_zoomArea.UpdatePointerMove(evt);
        m_zoomArea.UpdateScrollers(m_horizontalScroller, m_verticalScroller);

        // changing the scroller will update the view
        //m_verticalScroller.value = -m_zoomArea.temp_y;
    }
}
