// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/// This file contains code for drawing job "flows" in the timeline. This includes
/// dependencies between jobs, wait on jobs, and jobs that are scheduled, etc.

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking.PlayerConnection;

using System.Runtime.InteropServices;
using System.Threading;
using UnityEditorInternal;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Profiling;
using static Unity.Mathematics.math;
using System.Text;
using UnityEngine.TextCore.Text;
using UnityEngine.TextCore.LowLevel;
using System.Globalization;
using UnityEngine.Events;

namespace UnityEditor.JobsProfiling;


/// <summary>
/// Lookup tables built on-demand for fast dependency traversal.
/// </summary>
struct DependencyLookupTables : IDisposable
{
    /// <summary>Maps job handle to event indices (supports parallel jobs with multiple events per handle)</summary>
    internal NativeParallelMultiHashMap<ulong, int> handleToEventIndices;
    /// <summary>Maps job handle to handles that depend on it (reverse dependency lookup)</summary>
    internal NativeParallelMultiHashMap<ulong, ulong> handleToDependentHandles;

    public void Dispose()
    {
        if (handleToEventIndices.IsCreated)
            handleToEventIndices.Dispose();
        if (handleToDependentHandles.IsCreated)
            handleToDependentHandles.Dispose();
    }
}

[BurstCompile()]
struct GenerateDepenedicesMeshJob : IJob
{
    const float k_LineWidth = 2.0f;
    const float k_ArrowWidth = 5.0f;

    [WriteOnly]
    internal NativeArray<StartCompleteInfo> m_startCompleteInfo;

    [ReadOnly]
    internal TimelineSettings m_settings;

    /// Used for selecting in which order the Thread groups are drawn.
    [ReadOnly]
    internal NativeHashMap<ulong, ThreadPosition> m_threadOffsets;

    /// Threads beloning the the groups above
    [ReadOnly]
    internal NativeArray<ThreadInfo> m_threads;

    [ReadOnly]
    internal NativeList<ProfilingEvent> m_events;

    [ReadOnly]
    internal NativeHashMap<ulong, int> m_handleIndexLookup;

    [ReadOnly]
    internal NativeHashMap<int, ulong> m_eventHandleLookup;

    [ReadOnly]
    internal NativeList<InternalJobHandle> m_dependencyTable;

    /// List of jobs being scheduled
    [ReadOnly]
    internal NativeList<ScheduledJobInfo> m_scheduledJobs;

    /// List of all the jobhandles/event indices in the frame
    [ReadOnly]
    internal NativeList<JobHandleEventIndex> m_jobEventList;

    [ReadOnly]
    internal NativeList<JobFlow> m_jobFlows;

    // Optimized lookup table for schedule index
    [ReadOnly]
    internal NativeHashMap<ulong, int> m_handleToScheduleIndex;

    [ReadOnly]
    internal float m_timeOffset;

    [ReadOnly]
    internal int m_frameIndex;

    // This data is updated with the seleceted event in a frame
    [ReadOnly]
    internal JobSelection m_selectedEvent;

    /// Whether the Jobs group is folded (used to determine preview mode for dependency lines)
    [ReadOnly]
    internal bool m_isJobsGroupFolded;

    internal NativeList<DependJobInfo> m_dependencyJobs;
    internal NativeList<DependJobInfo> m_depedantJobs;

    internal DependJobInfo m_dependencyJob;
    internal DependJobInfo m_depedantJob;

    struct Rect
    {
        internal float2 x0y0;
        internal float2 x1y1;
    }

    internal PrimitiveRenderer m_renderer;

    /// <summary>
    /// This is used when drawing lines to determine where a line starts
    /// </summary>
    enum EventSide
    {
        Default,
        Left,
        Right,
    }

    /// <summary>
    /// This describes where a line should be placed on an event
    /// </summary>
    enum LineLocation
    {
        /// Line will be placed at the top of the event
        Top,
        /// Line will be placed at the bottom of the event
        Bottom,
        /// Draw Event in the middle of the lines (not recommended)
        Middle,
    }

    /// <summary>
    /// Calculates the Rect for an event
    /// </summary>
    Rect GetRectForEvent(in NativeArray<float> threadOffsets, int eventIndex)
    {
        ProfilingEvent profEvent = m_events[eventIndex];

        float threadOffset = threadOffsets[profEvent.threadIndex];

        float3 posLocal = new float3(profEvent.startTime + m_timeOffset, threadOffset + profEvent.level, 0.0f);
        float3 sizeLocal = new float3(profEvent.time, m_settings.invYBarSize, 0.0f);
        float3 cornerLocal = posLocal + sizeLocal;

        float3 pos = transform(m_settings.mat, posLocal);
        float3 corner = transform(m_settings.mat, cornerLocal);

        return new Rect
        {
            x0y0 = new float2(pos.x, pos.y),
            x1y1 = new float2(corner.x, corner.y),
        };
    }


    /// <summary>
    /// Find the index of the schedeling information for a job. O(1) hashmap lookup.
    /// </summary>
    int FindJobScheduleIndex(ulong jobHandle)
    {
        if (m_handleToScheduleIndex.TryGetValue(jobHandle, out int index))
            return index;
        return -1;
    }

    /// <summary>
    /// Gathers events that has jobHandle as dependency. Uses pre-built reverse dependency lookup.
    ///
    ///   |______________ 2
    ///   |______________ 3
    ///   |
    ///   |1
    ///
    ///  If 1 is sent in 2 and 3 will be collected
    ///
    /// </summary>
    void GetEventsWithHandleAsDependency(NativeList<int> output, ulong jobHandle, in DependencyLookupTables tables)
    {
        output.Clear();

        // Use the pre-built reverse dependency lookup
        if (tables.handleToDependentHandles.TryGetFirstValue(jobHandle, out ulong dependentHandle, out var iterator))
        {
            do
            {
                // Get all events for this dependent handle
                GetEventsWithJobHandle(output, new InternalJobHandle(dependentHandle), tables);
            }
            while (tables.handleToDependentHandles.TryGetNextValue(out dependentHandle, ref iterator));
        }
    }

    InternalJobHandle GetHandleFromEventIndex(int index)
    {
        if (index == -1)
            return new InternalJobHandle();

        // Use pre-built hashmap for O(1) lookup
        if (m_eventHandleLookup.TryGetValue(index, out ulong handle))
            return new InternalJobHandle(handle);

        return new InternalJobHandle();
    }

    NativeList<int> GatherDepedencyJobsFromHandle(ulong jobHandle, in DependencyLookupTables tables)
    {
        int scheduleIndex = FindJobScheduleIndex(jobHandle);

        if (scheduleIndex == -1)
            return new NativeList<int>(0, Allocator.Temp);

        return GatherDepedencyJobs(scheduleIndex, tables);
    }

    /// <summary>
    /// Collect all the jobs that has a dependency on the job with the given index.
    /// Uses pre-built multi-hashmap for O(d*k) lookup where d is dependency count
    /// and k is events per handle.
    /// </summary>
    NativeList<int> GatherDepedencyJobs(int jobInfoIndex, in DependencyLookupTables tables)
    {
        ScheduledJobInfo info = m_scheduledJobs[jobInfoIndex];

        int dependencyCount = info.dependencyCount;
        int dependencyTableOffset = info.dependencyTableIndex;

        var events = new NativeList<int>(m_jobEventList.Length, Allocator.Temp);

        for (int i = 0; i < dependencyCount; i++)
        {
            ulong handle = m_dependencyTable[dependencyTableOffset + i].ToUlong();

            // Use pre-built hashmap for O(k) lookup instead of O(n)
            if (tables.handleToEventIndices.TryGetFirstValue(handle, out int eventIndex, out var iterator))
            {
                do
                {
                    events.Add(eventIndex);
                }
                while (tables.handleToEventIndices.TryGetNextValue(out eventIndex, ref iterator));
            }
        }

        return events;
    }

    internal struct OffsetIndex
    {
        internal float offset;
        internal int index;
        internal int level;
    }

    internal struct SortOffsetIndex : IComparer<OffsetIndex>
    {
        public int Compare(OffsetIndex a, OffsetIndex b)
        {
            if (a.offset < b.offset)
                return -1;

            return 1;
        }
    }

    /// <summary>
    /// Draw connections to the jobs that depends on the selected job (left side)
    /// </summary>
    void DrawDependencyJobs(in NativeArray<float> threadOffsets, NativeList<int> events, int eventIndex, bool isPreviewMode)
    {
        if (!events.IsEmpty && m_settings.showDependsOn)
            DrawLineBetweenEvents(JobsProfilerColors.GetDependencyColor(m_settings.colorBlindMode), EventSide.Default, LineLocation.Bottom, threadOffsets, eventIndex, events, isPreviewMode);
    }

    /// <summary>
    /// Draw connections to the jobs that depends on the selected job (right side in the timeline)
    /// </summary>
    void DrawDependantJobs(in NativeArray<float> threadOffsets, NativeList<int> events, int eventIndex, bool isPreviewMode)
    {
        if (!events.IsEmpty && m_settings.showDependantOn)
            DrawLineBetweenEvents(JobsProfilerColors.GetDependencyColor(m_settings.colorBlindMode), EventSide.Default, LineLocation.Bottom, threadOffsets, eventIndex, events, isPreviewMode);
    }

    void DrawLineBetweenEvents(
        Color32 color,
        EventSide eventSide,
        LineLocation lineLocation,
        in NativeArray<float> threadOffsets,
        int startEvent,
        NativeList<int> targetEvents,
        bool isPreviewMode)
    {
        Rect source = GetRectForEvent(threadOffsets, startEvent);

        float eventsMaxX = float.MinValue;
        float eventsMinX = float.MaxValue;

        float eventsMaxY = float.MinValue;
        float eventsMinY = float.MaxValue;

        NativeArray<Rect> rects = new NativeArray<Rect>(targetEvents.Length, Allocator.Temp);

        // In preview mode, use small fixed offset
        // In normal mode, use source bar height to draw at bottom
        float offset = 0.0f;
        if (lineLocation == LineLocation.Bottom)
            offset = isPreviewMode ? 0.0f : (source.x1y1.y - source.x0y0.y);

        // Get the rects for the events and also keep track of the max x and min x value of these
        for (int i = 0; i < targetEvents.Length; ++i)
        {
            Rect rect = GetRectForEvent(threadOffsets, targetEvents[i]);

            eventsMinX = Math.Min(eventsMinX, rect.x0y0.x);
            eventsMinY = Math.Min(eventsMinY, rect.x0y0.y);

            eventsMaxY = Math.Max(eventsMaxY, rect.x0y0.y);
            eventsMaxX = Math.Max(eventsMaxX, rect.x0y0.x);

            rects[i] = rect;
        }

        // if source x is smaller that the smallest event x it means that the start event
        // is on the left side of the event and we need to draw something like this in the parallel case
        //
        //  _startEvent__
        //               |___ event0
        //               |_______ event1
        //               |____ event2
        float sourceX;

        bool eventIsOnLeftSide = source.x1y1.x < eventsMinX;

        if (eventIsOnLeftSide)
        {
            sourceX = source.x1y1.x;
        }
        else
        {
            if (eventSide == EventSide.Right)
                sourceX = source.x1y1.x;
            else
                sourceX = source.x0y0.x;
        }

        float sourceY = source.x0y0.y;

        if (sourceY > eventsMinY && sourceY <= eventsMaxY)
        {
            float2 startLine = new float2(sourceX, eventsMinY + offset);
            float2 endLine = new float2(sourceX, eventsMaxY + offset);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
        }
        else if (sourceY < eventsMaxY)
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, eventsMaxY + offset);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            if (eventIsOnLeftSide)
                m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Down);
            else
                m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Up);
        }
        // It's really bad to compare == with floats and we should do an approx comparison instead
        else if (sourceY == eventsMinY && sourceY == eventsMaxY)
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, eventsMinY + offset);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Right);
        }
        else
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, eventsMinY + offset);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            if (eventIsOnLeftSide)
                m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Down);
            else
                m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Up);
        }

        // If we have a case where the line looks like this or is in the middle of the events
        // we need to change drawing of the arrow accordingly
        //               |___ event0
        //               |_______ event1
        //               |____ event2
        //  _startEvent__|

        if (eventIsOnLeftSide)
        {
            for (int i = 0; i < targetEvents.Length; ++i)
            {
                Rect rect = rects[i];

                float y = rect.x0y0.y + offset;

                float2 startPos = new float2(sourceX, y);
                float2 endPos = new float2(rect.x0y0.x, y);

                endPos.x -= k_ArrowWidth;

                m_renderer.DrawHorizontalLine(startPos, endPos, k_LineWidth, color);
                m_renderer.DrawArrow(endPos, color, PrimitiveRenderer.ArrowDirection.Right);
            }
        }
        else
        {
            // if we are here it means we have this case
            //  event0 _______|
            //      event1 ___|__ startEvent
            //   event2 ______|

            for (int i = 0; i < targetEvents.Length; ++i)
            {
                Rect rect = rects[i];

                float y = rect.x0y0.y + offset;

                float2 endPos = new float2(sourceX, y);
                float2 startPos = new float2(rect.x1y1.x, y);

                m_renderer.DrawHorizontalLine(startPos, endPos, k_LineWidth, color);
                m_renderer.DrawArrow(startPos, color, PrimitiveRenderer.ArrowDirection.Right);
            }
        }

        rects.Dispose();
    }

    void BuildOutputDependencies(NativeList<DependJobInfo> output, in NativeList<int> events)
    {
        output.Clear();

        for (int i = 0; i < events.Length; ++i)
        {
            int eventIndex = events[i];
            var info = new DependJobInfo
            {
                jobHandle = new InternalJobHandle(0),
                frameIndex = m_frameIndex,
                eventIndex = eventIndex,
            };

            output.Add(info);
        }
    }

    /// <summary>
    /// Get a list of all events with a specific JobHandle. Uses pre-built multi-hashmap
    /// for O(k) lookup where k is the number of events (handles parallel jobs).
    /// </summary>
    void GetEventsWithJobHandle(NativeList<int> output, InternalJobHandle handle, in DependencyLookupTables tables)
    {
        ulong jobId = handle.ToUlong();

        if (tables.handleToEventIndices.TryGetFirstValue(jobId, out int eventIndex, out var iterator))
        {
            do
            {
                output.AddNoResize(eventIndex);
            }
            while (tables.handleToEventIndices.TryGetNextValue(out eventIndex, ref iterator));
        }
    }

    void DrawEventsByInfo(JobFlow info, in NativeArray<float> tempOffsets, int index, ref StartCompleteInfo startCompleteInfo, ref int waitedOnIndex, in DependencyLookupTables tables, bool isPreviewMode)
    {
        NativeList<int> events = new NativeList<int>(m_jobEventList.Length, Allocator.Temp);

        GetEventsWithJobHandle(events, info.handle, tables);

        Color32 color = info.getColor(m_settings.colorBlindMode);

        switch (info.state)
        {
            case JobFlowState.WaitedOn:
            {
                if (m_settings.showCompletedByWait)
                    DrawLineBetweenEvents(color, EventSide.Right, LineLocation.Bottom, tempOffsets, info.eventIndex, events, isPreviewMode);

                startCompleteInfo.completeFrame = m_frameIndex;
                startCompleteInfo.completeEventIndex = info.eventIndex;
                startCompleteInfo.completeEventParentIndex = m_events[info.eventIndex].parentIndex;

                waitedOnIndex = index;
                break;
            }

            case JobFlowState.BeginSchedule:
            {
                if (m_settings.showScheduledBy)
                    DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Top, tempOffsets, info.eventIndex, events, isPreviewMode);

                startCompleteInfo.startFrame = m_frameIndex;
                startCompleteInfo.startEventIndex = info.eventIndex;
                startCompleteInfo.startEventParentIndex = m_events[info.eventIndex].parentIndex;

                break;
            }

            case JobFlowState.CompletedNoWait:
            {
                // If previous event was that we waited for completion we shouldn't draw something for complete no wait
                if (waitedOnIndex != index - 1)
                {
                    if (m_settings.showCompletedByNoWait)
                        DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Bottom, tempOffsets, info.eventIndex, events, isPreviewMode);

                    startCompleteInfo.completeFrame = m_frameIndex;
                    startCompleteInfo.completeEventIndex = info.eventIndex;
                    startCompleteInfo.completeEventParentIndex = m_events[info.eventIndex].parentIndex;
                }

                break;
            }

            default:
            {
                DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Bottom, tempOffsets, info.eventIndex, events, isPreviewMode);
                break;
            }
        }

        events.Dispose();
    }

    private DependJobInfo DefaultDepInfo()
    {
        return new DependJobInfo
        {
            jobHandle = new InternalJobHandle(0),
            frameIndex = -1,
            eventIndex = -1,
        };
    }

    void RenderDependantJobsRecursive(NativeHashSet<int> visitedEvents, in NativeArray<float> tempOffsets, NativeList<int> dependantEvents, ulong jobHandle, int selectedEvent, int level, in DependencyLookupTables tables, bool isPreviewMode)
    {
        DrawDependantJobs(tempOffsets, dependantEvents, selectedEvent, isPreviewMode);

        for (int i = 0; i < dependantEvents.Length; ++i)
        {
            ulong outHandle;
            int currentEvent = dependantEvents[i];

            // As we can have several events with the same jobId (for parallell jobs) we keep track of the events we have visited already
            if (!visitedEvents.Contains(currentEvent))
            {
                if (m_eventHandleLookup.TryGetValue(currentEvent, out outHandle))
                {
                    NativeList<int> events = new NativeList<int>(m_jobEventList.Length, Allocator.Temp);
                    GetEventsWithHandleAsDependency(events, outHandle, tables);
                    RenderDependantJobsRecursive(visitedEvents, tempOffsets, events, outHandle, currentEvent, level + 1, tables, isPreviewMode);
                    events.Dispose();
                }

                visitedEvents.Add(currentEvent);
            }
        }
    }

    void RenderDependencyJobsRecursive(NativeHashSet<int> visitedEvents, in NativeArray<float> tempOffsets, NativeList<int> dependantEvents, ulong jobHandle, int selectedEvent, int level, in DependencyLookupTables tables, bool isPreviewMode)
    {
        DrawDependencyJobs(tempOffsets, dependantEvents, selectedEvent, isPreviewMode);

        for (int i = 0; i < dependantEvents.Length; ++i)
        {
            ulong outHandle;
            int currentEvent = dependantEvents[i];

            // As we can have several events with the same jobId (for parallell jobs) we keep track of the events we have visited already
            if (!visitedEvents.Contains(currentEvent))
            {
                if (m_eventHandleLookup.TryGetValue(currentEvent, out outHandle))
                {
                    NativeList<int> events = GatherDepedencyJobsFromHandle(outHandle, tables);
                    RenderDependencyJobsRecursive(visitedEvents, tempOffsets, events, outHandle, currentEvent, level + 1, tables, isPreviewMode);
                    events.Dispose();
                }

                visitedEvents.Add(currentEvent);
            }
        }
    }

    DependencyLookupTables BuildLookupTables()
    {
        var tables = new DependencyLookupTables();

        // Build handle -> event indices lookup (for parallel jobs with multiple events)
        tables.handleToEventIndices = new NativeParallelMultiHashMap<ulong, int>(m_jobEventList.Length, Allocator.Temp);
        foreach (var jobEvent in m_jobEventList)
        {
            tables.handleToEventIndices.Add(jobEvent.handle.ToUlong(), jobEvent.eventIndex);
        }

        // Build reverse dependency lookup: handle -> handles that depend on it
        int totalDeps = 0;
        for (int i = 0; i < m_scheduledJobs.Length; i++)
            totalDeps += m_scheduledJobs[i].dependencyCount;

        tables.handleToDependentHandles = new NativeParallelMultiHashMap<ulong, ulong>(math.max(totalDeps, 1), Allocator.Temp);
        for (int i = 0; i < m_scheduledJobs.Length; i++)
        {
            ScheduledJobInfo job = m_scheduledJobs[i];
            ulong jobHandle = job.handle.ToUlong();
            int depCount = job.dependencyCount;
            int depOffset = job.dependencyTableIndex;
            for (int d = 0; d < depCount; d++)
            {
                ulong depHandle = m_dependencyTable[depOffset + d].ToUlong();
                tables.handleToDependentHandles.Add(depHandle, jobHandle);
            }
        }

        return tables;
    }

    public void Execute()
    {
        if (m_selectedEvent.state != JobSelection.State.Selected)
            return;

        // Build lookup tables once at start for O(1) lookups during rendering
        var lookupTables = BuildLookupTables();

        NativeArray<float> tempOffsets = new NativeArray<float>(m_threads.Length, Allocator.Temp);
        bool isPreviewMode = m_isJobsGroupFolded;

        int waitedOnIndex = -1;

        for (int i = 0; i < m_threads.Length; ++i)
        {
            ThreadPosition threadPos;

            if (m_threadOffsets.TryGetValue(m_threads[i].threadId, out threadPos))
            {
                tempOffsets[i] = (float)threadPos.offset;
            }
            else
            {
                // Large number outside of the screen
                tempOffsets[i] = 1024;
            }
        }

        ulong jobHandle = m_selectedEvent.jobHandle.ToUlong();

        StartCompleteInfo startCompleteInfo = StartCompleteInfo.CreateInvalid();

        // If we don't have a jobHandle selected it may be that we have a proper flow event still. This can happen if we have
        // selected an event where have started a job (like on the main thread) but it doesn't actually have a jobHandle
        if (jobHandle == 0)
        {
            int selectedEventIndex = m_selectedEvent.eventIndex;

            // If we didn't have a jobHandle it may be that we have selected an event where we are waiting for something to
            // start / complete wait so we loop over and check if we find any event in the list
            for (int index = 0, count = m_jobFlows.Length; index < count; ++index)
            {
                JobFlow info = m_jobFlows[index];

                if (info.eventIndex != selectedEventIndex)
                    continue;

                DrawEventsByInfo(info, tempOffsets, index, ref startCompleteInfo, ref waitedOnIndex, lookupTables, isPreviewMode);

                break;
            }

            m_dependencyJobs.Clear();
            m_depedantJobs.Clear();
        }
        else
        {
            // TODO: We likely need to search over multiple frames here to support jobs across frames
            int jobScheduleIndex = FindJobScheduleIndex(jobHandle);

            if (jobScheduleIndex == -1)
            {
                m_startCompleteInfo[0] = startCompleteInfo;
                lookupTables.Dispose();
                return;
            }

            for (int index = 0, count = m_jobFlows.Length; index < count; ++index)
            {
                JobFlow info = m_jobFlows[index];

                if (info.handle.ToUlong() != jobHandle)
                    continue;

                DrawEventsByInfo(info, tempOffsets, index, ref startCompleteInfo, ref waitedOnIndex, lookupTables, isPreviewMode);
            }

            var dependantEvents = new NativeList<int>(m_jobEventList.Length, Allocator.Temp);
            var dependencyEvents = GatherDepedencyJobs(jobScheduleIndex, lookupTables);

            GetEventsWithHandleAsDependency(dependantEvents, jobHandle, lookupTables);

            // Only output these lists if it's for the current selected frame and we aren't hovering over the event
            if (m_frameIndex == m_selectedEvent.frameIndex && !m_selectedEvent.hover)
            {
                BuildOutputDependencies(m_dependencyJobs, dependencyEvents);
                BuildOutputDependencies(m_depedantJobs, dependantEvents);
            }

            if (m_settings.showFullDependencyChain)
            {
                var visitedEvents = new NativeHashSet<int>(m_events.Length, Allocator.Temp);

                if (m_settings.showDependantOn)
                {
                    RenderDependantJobsRecursive(visitedEvents, tempOffsets, dependantEvents, jobHandle, m_selectedEvent.eventIndex, 0, lookupTables, isPreviewMode);
                    visitedEvents.Clear();
                }

                if (m_settings.showDependsOn)
                    RenderDependencyJobsRecursive(visitedEvents, tempOffsets, dependencyEvents, jobHandle, m_selectedEvent.eventIndex, 0, lookupTables, isPreviewMode);

                visitedEvents.Dispose();
            }
            else
            {
                DrawDependencyJobs(tempOffsets, dependencyEvents, m_selectedEvent.eventIndex, isPreviewMode);
                DrawDependantJobs(tempOffsets, dependantEvents, m_selectedEvent.eventIndex, isPreviewMode);
            }

            dependantEvents.Dispose();
        }

        m_startCompleteInfo[0] = startCompleteInfo;

        tempOffsets.Dispose();
        lookupTables.Dispose();
    }
}

