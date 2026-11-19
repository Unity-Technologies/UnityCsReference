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



internal struct ScheduleRef
{
    internal int frame;
    internal int scheduleIndex;
}

internal unsafe struct FlowFrameView
{
    internal int frame;
    internal float timeOffset;
    internal int threadBase;
    internal ProfilingEvent* events;
    internal ThreadInfo* threads;
    internal int threadCount;
    internal ScheduledJobInfo* scheduledJobs;
    internal int scheduledJobCount;
    internal InternalJobHandle* dependencyTable;
    internal JobHandleEventIndex* jobEvents;
    internal int jobEventCount;
    internal JobFlow* jobFlows;
    internal int jobFlowCount;

    internal static FlowFrameView Create(in FrameDataIndex frame, int threadBase)
    {
        var data = frame.data;

        return new FlowFrameView
        {
            frame = frame.index.frameCacheIndex,
            timeOffset = frame.index.time,
            threadBase = threadBase,
            events = (ProfilingEvent*)data.events.GetUnsafeReadOnlyPtr(),
            threads = (ThreadInfo*)data.threads.GetUnsafeReadOnlyPtr(),
            threadCount = data.threads.Length,
            scheduledJobs = (ScheduledJobInfo*)data.scheduledJobs.GetUnsafeReadOnlyPtr(),
            scheduledJobCount = data.scheduledJobs.Length,
            dependencyTable = (InternalJobHandle*)data.dependencyTable.GetUnsafeReadOnlyPtr(),
            jobEvents = (JobHandleEventIndex*)data.jobEventIndexList.GetUnsafeReadOnlyPtr(),
            jobEventCount = data.jobEventIndexList.Length,
            jobFlows = (JobFlow*)data.jobFlows.GetUnsafeReadOnlyPtr(),
            jobFlowCount = data.jobFlows.Length,
        };
    }
}

/// <summary>
/// Lookup tables built on-demand for fast dependency traversal.
/// </summary>
struct DependencyLookupTables : IDisposable
{
    /// <summary>Maps job handle to events (supports parallel jobs with multiple events per handle)</summary>
    internal NativeParallelMultiHashMap<ulong, EventRef> handleToEvents;
    /// <summary>Maps job handle to handles that depend on it (reverse dependency lookup)</summary>
    internal NativeParallelMultiHashMap<ulong, ulong> handleToDependentHandles;
    internal NativeHashMap<ulong, ScheduleRef> handleToSchedule;
    internal NativeHashMap<long, ulong> eventToHandle;

    public void Dispose()
    {
        if (handleToEvents.IsCreated)
            handleToEvents.Dispose();
        if (handleToDependentHandles.IsCreated)
            handleToDependentHandles.Dispose();
        if (handleToSchedule.IsCreated)
            handleToSchedule.Dispose();
        if (eventToHandle.IsCreated)
            eventToHandle.Dispose();
    }
}

[BurstCompile()]
unsafe struct GenerateDepenedicesMeshJob : IJob
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

    /// The frames in the window. Events are referenced by frame and index in that frame
    [ReadOnly]
    internal NativeArray<FlowFrameView> m_views;

    [ReadOnly]
    internal NativeHashMap<long, EventRef> m_continuations;

    [ReadOnly]
    internal NativeHashMap<long, float> m_headEndTimes;

    // This data is updated with the seleceted event in a frame
    [ReadOnly]
    internal JobSelection m_selectedEvent;

    internal NativeList<DependJobInfo> m_dependencyJobs;
    internal NativeList<DependJobInfo> m_depedantJobs;

    internal DependJobInfo m_dependencyJob;
    internal DependJobInfo m_depedantJob;

    struct Rect
    {
        internal float2 x0y0;
        internal float2 x1y1;
        internal bool isPreview;
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

    int FindSlot(int frame)
    {
        for (int i = 0; i < m_views.Length; ++i)
        {
            if (m_views[i].frame == frame)
                return i;
        }

        return -1;
    }

    ProfilingEvent GetEvent(EventRef eventRef)
    {
        return m_views[FindSlot(eventRef.frameCacheIndex)].events[eventRef.eventIndex];
    }

    /// <summary>
    /// Calculates the Rect for an event
    /// </summary>
    Rect GetRectForEvent(in NativeArray<float> threadOffsets, EventRef eventRef)
    {
        FlowFrameView view = m_views[FindSlot(eventRef.frameCacheIndex)];
        ProfilingEvent profEvent = view.events[eventRef.eventIndex];

        float threadOffset = threadOffsets[view.threadBase + profEvent.threadIndex];
        float previewHeight = 1.0f;
        float level = profEvent.level;
        bool isPreview = false;
        if (m_threadOffsets.TryGetValue(view.threads[profEvent.threadIndex].threadId, out ThreadPosition threadPos))
        {
            float visibleDepth = math.lerp(1.0f, threadPos.depth, threadPos.animationProgress);
            level = math.clamp(level, 0.0f, math.max(visibleDepth - 1.0f, 0.0f));

            isPreview = threadPos.visibility == ThreadVisibility.Preview;
            if (isPreview)
            {
                previewHeight = math.min(threadPos.previewHeight, 1.0f);
                if (previewHeight < 1.0f)
                    level = 0.0f;
            }
        }

        float time = profEvent.time;

        if (m_headEndTimes.TryGetValue(eventRef.key, out float endTime))
            time = endTime - profEvent.startTime;

        float3 posLocal = new float3(profEvent.startTime + view.timeOffset, threadOffset + level, 0.0f);
        float3 sizeLocal = new float3(time, m_settings.invYBarSize * previewHeight, 0.0f);
        float3 cornerLocal = posLocal + sizeLocal;

        float3 pos = transform(m_settings.mat, posLocal);
        float3 corner = transform(m_settings.mat, cornerLocal);

        return new Rect
        {
            x0y0 = new float2(pos.x, pos.y),
            x1y1 = new float2(corner.x, corner.y),
            isPreview = isPreview,
        };
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
    void GetEventsWithHandleAsDependency(NativeList<EventRef> output, ulong jobHandle, in DependencyLookupTables tables)
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

    NativeList<EventRef> GatherDependencyJobsFromHandle(ulong jobHandle, in DependencyLookupTables tables)
    {
        if (!tables.handleToSchedule.TryGetValue(jobHandle, out ScheduleRef schedule))
            return new NativeList<EventRef>(0, Allocator.Temp);

        return GatherDependencyJobs(schedule, tables);
    }

    /// <summary>
    /// Collect all the jobs that has a dependency on the job with the given schedule.
    /// Uses pre-built multi-hashmap for O(d*k) lookup where d is dependency count
    /// and k is events per handle.
    /// </summary>
    NativeList<EventRef> GatherDependencyJobs(ScheduleRef schedule, in DependencyLookupTables tables)
    {
        FlowFrameView view = m_views[FindSlot(schedule.frame)];
        ScheduledJobInfo info = view.scheduledJobs[schedule.scheduleIndex];

        int dependencyCount = info.dependencyCount;
        int dependencyTableOffset = info.dependencyTableIndex;

        var events = new NativeList<EventRef>(16, Allocator.Temp);

        for (int i = 0; i < dependencyCount; i++)
        {
            ulong handle = view.dependencyTable[dependencyTableOffset + i].ToUlong();

            // Use pre-built hashmap for O(k) lookup instead of O(n)
            if (tables.handleToEvents.TryGetFirstValue(handle, out EventRef eventRef, out var iterator))
            {
                do
                {
                    events.Add(eventRef);
                }
                while (tables.handleToEvents.TryGetNextValue(out eventRef, ref iterator));
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
    void DrawDependencyJobs(in NativeArray<float> threadOffsets, NativeList<EventRef> events, EventRef eventRef)
    {
        if (!events.IsEmpty && m_settings.showDependsOn)
            DrawLineBetweenEvents(JobsProfilerColors.GetDependencyColor(m_settings.colorBlindMode), EventSide.Default, LineLocation.Bottom, threadOffsets, eventRef, events);
    }

    /// <summary>
    /// Draw connections to the jobs that depends on the selected job (right side in the timeline)
    /// </summary>
    void DrawDependantJobs(in NativeArray<float> threadOffsets, NativeList<EventRef> events, EventRef eventRef)
    {
        if (!events.IsEmpty && m_settings.showDependantOn)
            DrawLineBetweenEvents(JobsProfilerColors.GetDependencyColor(m_settings.colorBlindMode), EventSide.Default, LineLocation.Bottom, threadOffsets, eventRef, events);
    }

    static float GetBottomOffset(in Rect rect)
    {
        return rect.isPreview ? 0.0f : rect.x1y1.y - rect.x0y0.y;
    }

    void DrawLineBetweenEvents(
        Color32 color,
        EventSide eventSide,
        LineLocation lineLocation,
        in NativeArray<float> threadOffsets,
        EventRef startEvent,
        NativeList<EventRef> targetEvents)
    {
        Rect source = GetRectForEvent(threadOffsets, startEvent);

        float eventsMaxX = float.MinValue;
        float eventsMinX = float.MaxValue;

        float eventsMaxY = float.MinValue;
        float eventsMinY = float.MaxValue;
        float lineMaxY = float.MinValue;
        float lineMinY = float.MaxValue;

        NativeArray<Rect> rects = new NativeArray<Rect>(targetEvents.Length, Allocator.Temp);

        // In preview mode, use small fixed offset
        // In normal mode, use source bar height to draw at bottom
        float offset = 0.0f;
        if (lineLocation == LineLocation.Bottom)
            offset = GetBottomOffset(source);

        // Get the rects for the events and also keep track of the max x and min x value of these
        for (int i = 0; i < targetEvents.Length; ++i)
        {
            Rect rect = GetRectForEvent(threadOffsets, targetEvents[i]);

            eventsMinX = Math.Min(eventsMinX, rect.x0y0.x);
            eventsMinY = Math.Min(eventsMinY, rect.x0y0.y);

            eventsMaxY = Math.Max(eventsMaxY, rect.x0y0.y);

            float lineY = rect.x0y0.y + (lineLocation == LineLocation.Bottom ? GetBottomOffset(rect) : 0.0f);
            lineMinY = Math.Min(lineMinY, lineY);
            lineMaxY = Math.Max(lineMaxY, lineY);
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
            float2 startLine = new float2(sourceX, lineMinY);
            float2 endLine = new float2(sourceX, lineMaxY);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
        }
        else if (sourceY < eventsMaxY)
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, lineMaxY);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            bool pointsDown = (endLine.y > startLine.y) == eventIsOnLeftSide;
            m_renderer.DrawArrow(startLine, color, pointsDown ? PrimitiveRenderer.ArrowDirection.Down : PrimitiveRenderer.ArrowDirection.Up);
        }
        // It's really bad to compare == with floats and we should do an approx comparison instead
        else if (sourceY == eventsMinY && sourceY == eventsMaxY)
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, lineMinY);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            m_renderer.DrawArrow(startLine, color, PrimitiveRenderer.ArrowDirection.Right);
        }
        else
        {
            float2 startLine = new float2(sourceX, sourceY + offset);
            float2 endLine = new float2(sourceX, lineMinY);
            m_renderer.DrawVerticalLine(startLine, endLine, k_LineWidth, color);
            bool pointsDown = (endLine.y > startLine.y) == eventIsOnLeftSide;
            m_renderer.DrawArrow(startLine, color, pointsDown ? PrimitiveRenderer.ArrowDirection.Down : PrimitiveRenderer.ArrowDirection.Up);
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

                float y = rect.x0y0.y + (lineLocation == LineLocation.Bottom ? GetBottomOffset(rect) : 0.0f);

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

                float y = rect.x0y0.y + (lineLocation == LineLocation.Bottom ? GetBottomOffset(rect) : 0.0f);

                float2 endPos = new float2(sourceX, y);
                float2 startPos = new float2(rect.x1y1.x, y);

                m_renderer.DrawHorizontalLine(startPos, endPos, k_LineWidth, color);
                m_renderer.DrawArrow(startPos, color, PrimitiveRenderer.ArrowDirection.Right);
            }
        }

        rects.Dispose();
    }


    void BuildOutputDependencies(NativeList<DependJobInfo> output, in NativeList<EventRef> events)
    {
        output.Clear();

        for (int i = 0; i < events.Length; ++i)
        {
            var info = new DependJobInfo
            {
                jobHandle = new InternalJobHandle(0),
                frameIndex = events[i].frameCacheIndex,
                eventIndex = events[i].eventIndex,
            };

            output.Add(info);
        }
    }

    /// <summary>
    /// Get a list of all events with a specific JobHandle. Uses pre-built multi-hashmap
    /// for O(k) lookup where k is the number of events (handles parallel jobs).
    /// </summary>
    void GetEventsWithJobHandle(NativeList<EventRef> output, InternalJobHandle handle, in DependencyLookupTables tables)
    {
        ulong jobId = handle.ToUlong();

        if (tables.handleToEvents.TryGetFirstValue(jobId, out EventRef eventRef, out var iterator))
        {
            do
            {
                output.Add(eventRef);
            }
            while (tables.handleToEvents.TryGetNextValue(out eventRef, ref iterator));
        }
    }

    void DrawEventsByInfo(JobFlow info, int frame, in NativeArray<float> tempOffsets, int index, ref StartCompleteInfo startCompleteInfo, ref int waitedOnIndex, in DependencyLookupTables tables)
    {
        NativeList<EventRef> events = new NativeList<EventRef>(16, Allocator.Temp);
        var flowEvent = new EventRef { frameCacheIndex = frame, eventIndex = info.eventIndex };

        if (m_continuations.TryGetValue(flowEvent.key, out EventRef flowHead))
            flowEvent = flowHead;

        GetEventsWithJobHandle(events, info.handle, tables);

        Color32 color = info.getColor(m_settings.colorBlindMode);

        switch (info.state)
        {
            case JobFlowState.WaitedOn:
            {
                if (m_settings.showCompletedByWait)
                    DrawLineBetweenEvents(color, EventSide.Right, LineLocation.Bottom, tempOffsets, flowEvent, events);

                startCompleteInfo.completeFrame = flowEvent.frameCacheIndex;
                startCompleteInfo.completeEventIndex = flowEvent.eventIndex;
                startCompleteInfo.completeEventParentIndex = GetEvent(flowEvent).parentIndex;

                waitedOnIndex = index;
                break;
            }

            case JobFlowState.BeginSchedule:
            {
                if (m_settings.showScheduledBy)
                    DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Top, tempOffsets, flowEvent, events);

                if (startCompleteInfo.startFrame == -1)
                {
                    startCompleteInfo.startFrame = flowEvent.frameCacheIndex;
                    startCompleteInfo.startEventIndex = flowEvent.eventIndex;
                    startCompleteInfo.startEventParentIndex = GetEvent(flowEvent).parentIndex;
                }

                break;
            }

            case JobFlowState.CompletedNoWait:
            {
                // If previous event was that we waited for completion we shouldn't draw something for complete no wait
                if (waitedOnIndex != index - 1)
                {
                    if (m_settings.showCompletedByNoWait)
                        DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Bottom, tempOffsets, flowEvent, events);

                    startCompleteInfo.completeFrame = flowEvent.frameCacheIndex;
                    startCompleteInfo.completeEventIndex = flowEvent.eventIndex;
                    startCompleteInfo.completeEventParentIndex = GetEvent(flowEvent).parentIndex;
                }

                break;
            }

            default:
            {
                DrawLineBetweenEvents(color, EventSide.Left, LineLocation.Bottom, tempOffsets, flowEvent, events);
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

    void RenderDependantJobsRecursive(NativeHashSet<long> visitedEvents, in NativeArray<float> tempOffsets, NativeList<EventRef> dependantEvents, ulong jobHandle, EventRef selectedEvent, int level, in DependencyLookupTables tables)
    {
        DrawDependantJobs(tempOffsets, dependantEvents, selectedEvent);

        for (int i = 0; i < dependantEvents.Length; ++i)
        {
            ulong outHandle;
            EventRef currentEvent = dependantEvents[i];

            // As we can have several events with the same jobId (for parallell jobs) we keep track of the events we have visited already
            if (!visitedEvents.Contains(currentEvent.key))
            {
                if (tables.eventToHandle.TryGetValue(currentEvent.key, out outHandle))
                {
                    NativeList<EventRef> events = new NativeList<EventRef>(16, Allocator.Temp);
                    GetEventsWithHandleAsDependency(events, outHandle, tables);
                    RenderDependantJobsRecursive(visitedEvents, tempOffsets, events, outHandle, currentEvent, level + 1, tables);
                    events.Dispose();
                }

                visitedEvents.Add(currentEvent.key);
            }
        }
    }

    void RenderDependencyJobsRecursive(NativeHashSet<long> visitedEvents, in NativeArray<float> tempOffsets, NativeList<EventRef> dependantEvents, ulong jobHandle, EventRef selectedEvent, int level, in DependencyLookupTables tables)
    {
        DrawDependencyJobs(tempOffsets, dependantEvents, selectedEvent);

        for (int i = 0; i < dependantEvents.Length; ++i)
        {
            ulong outHandle;
            EventRef currentEvent = dependantEvents[i];

            // As we can have several events with the same jobId (for parallell jobs) we keep track of the events we have visited already
            if (!visitedEvents.Contains(currentEvent.key))
            {
                if (tables.eventToHandle.TryGetValue(currentEvent.key, out outHandle))
                {
                    NativeList<EventRef> events = GatherDependencyJobsFromHandle(outHandle, tables);
                    RenderDependencyJobsRecursive(visitedEvents, tempOffsets, events, outHandle, currentEvent, level + 1, tables);
                    events.Dispose();
                }

                visitedEvents.Add(currentEvent.key);
            }
        }
    }

    DependencyLookupTables BuildLookupTables()
    {
        var tables = new DependencyLookupTables();

        int jobEventCount = 0;
        int scheduledJobCount = 0;
        int totalDeps = 0;

        for (int slot = 0; slot < m_views.Length; ++slot)
        {
            FlowFrameView view = m_views[slot];
            jobEventCount += view.jobEventCount;
            scheduledJobCount += view.scheduledJobCount;

            for (int i = 0; i < view.scheduledJobCount; i++)
                totalDeps += view.scheduledJobs[i].dependencyCount;
        }

        tables.handleToEvents = new NativeParallelMultiHashMap<ulong, EventRef>(math.max(jobEventCount, 1), Allocator.Temp);
        tables.eventToHandle = new NativeHashMap<long, ulong>(math.max(jobEventCount, 1), Allocator.Temp);
        tables.handleToSchedule = new NativeHashMap<ulong, ScheduleRef>(math.max(scheduledJobCount, 1), Allocator.Temp);
        tables.handleToDependentHandles = new NativeParallelMultiHashMap<ulong, ulong>(math.max(totalDeps, 1), Allocator.Temp);

        for (int slot = 0; slot < m_views.Length; ++slot)
        {
            FlowFrameView view = m_views[slot];

            for (int i = 0; i < view.jobEventCount; ++i)
            {
                var eventRef = new EventRef { frameCacheIndex = view.frame, eventIndex = view.jobEvents[i].eventIndex };

                if (m_continuations.ContainsKey(eventRef.key))
                    continue;

                ulong handle = view.jobEvents[i].handle.ToUlong();
                tables.handleToEvents.Add(handle, eventRef);
                tables.eventToHandle.TryAdd(eventRef.key, handle);
            }

            for (int i = 0; i < view.scheduledJobCount; i++)
            {
                ScheduledJobInfo job = view.scheduledJobs[i];
                ulong jobHandle = job.handle.ToUlong();
                tables.handleToSchedule.TryAdd(jobHandle, new ScheduleRef { frame = view.frame, scheduleIndex = i });

                for (int d = 0; d < job.dependencyCount; d++)
                {
                    ulong depHandle = view.dependencyTable[job.dependencyTableIndex + d].ToUlong();
                    tables.handleToDependentHandles.Add(depHandle, jobHandle);
                }
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

        int threadCount = 0;

        for (int i = 0; i < m_views.Length; ++i)
            threadCount += m_views[i].threadCount;

        NativeArray<float> tempOffsets = new NativeArray<float>(threadCount, Allocator.Temp);

        for (int slot = 0; slot < m_views.Length; ++slot)
        {
            FlowFrameView view = m_views[slot];

            for (int i = 0; i < view.threadCount; ++i)
            {
                ThreadPosition threadPos;

                if (m_threadOffsets.TryGetValue(view.threads[i].threadId, out threadPos))
                {
                    tempOffsets[view.threadBase + i] = (float)threadPos.offset;
                }
                else
                {
                    // Large number outside of the screen
                    tempOffsets[view.threadBase + i] = 1024;
                }
            }
        }

        ulong jobHandle = m_selectedEvent.jobHandle.ToUlong();

        StartCompleteInfo startCompleteInfo = StartCompleteInfo.CreateInvalid();

        var selectedEvent = new EventRef { frameCacheIndex = m_selectedEvent.frameIndex, eventIndex = m_selectedEvent.eventIndex };

        if (m_continuations.TryGetValue(selectedEvent.key, out EventRef head))
            selectedEvent = head;

        int selectedSlot = FindSlot(selectedEvent.frameCacheIndex);

        if (selectedSlot == -1)
        {
            m_startCompleteInfo[0] = startCompleteInfo;
            lookupTables.Dispose();
            return;
        }

        int waitedOnIndex = -1;

        // If we don't have a jobHandle selected it may be that we have a proper flow event still. This can happen if we have
        // selected an event where have started a job (like on the main thread) but it doesn't actually have a jobHandle
        if (jobHandle == 0)
        {
            FlowFrameView selectedView = m_views[selectedSlot];

            // If we didn't have a jobHandle it may be that we have selected an event where we are waiting for something to
            // start / complete wait so we loop over and check if we find any event in the list
            for (int index = 0, count = selectedView.jobFlowCount; index < count; ++index)
            {
                JobFlow info = selectedView.jobFlows[index];

                if (info.eventIndex != selectedEvent.eventIndex)
                    continue;

                DrawEventsByInfo(info, selectedView.frame, tempOffsets, index, ref startCompleteInfo, ref waitedOnIndex, lookupTables);

                break;
            }

            m_dependencyJobs.Clear();
            m_depedantJobs.Clear();
        }
        else
        {
            if (!lookupTables.handleToSchedule.TryGetValue(jobHandle, out ScheduleRef schedule))
            {
                m_startCompleteInfo[0] = startCompleteInfo;
                lookupTables.Dispose();
                return;
            }

            for (int slot = 0; slot < m_views.Length; ++slot)
            {
                FlowFrameView view = m_views[slot];
                waitedOnIndex = -1;

                for (int index = 0, count = view.jobFlowCount; index < count; ++index)
                {
                    JobFlow info = view.jobFlows[index];

                    if (info.handle.ToUlong() != jobHandle)
                        continue;

                    DrawEventsByInfo(info, view.frame, tempOffsets, index, ref startCompleteInfo, ref waitedOnIndex, lookupTables);
                }
            }

            var dependantEvents = new NativeList<EventRef>(16, Allocator.Temp);
            var dependencyEvents = GatherDependencyJobs(schedule, lookupTables);

            GetEventsWithHandleAsDependency(dependantEvents, jobHandle, lookupTables);

            // Only output these lists if we aren't hovering over the event
            if (!m_selectedEvent.hover)
            {
                BuildOutputDependencies(m_dependencyJobs, dependencyEvents);
                BuildOutputDependencies(m_depedantJobs, dependantEvents);
            }

            if (m_settings.showFullDependencyChain)
            {
                var visitedEvents = new NativeHashSet<long>(64, Allocator.Temp);

                if (m_settings.showDependantOn)
                {
                    RenderDependantJobsRecursive(visitedEvents, tempOffsets, dependantEvents, jobHandle, selectedEvent, 0, lookupTables);
                    visitedEvents.Clear();
                }

                if (m_settings.showDependsOn)
                    RenderDependencyJobsRecursive(visitedEvents, tempOffsets, dependencyEvents, jobHandle, selectedEvent, 0, lookupTables);

                visitedEvents.Dispose();
            }
            else
            {
                DrawDependencyJobs(tempOffsets, dependencyEvents, selectedEvent);
                DrawDependantJobs(tempOffsets, dependantEvents, selectedEvent);
            }

            dependantEvents.Dispose();
        }

        m_startCompleteInfo[0] = startCompleteInfo;

        tempOffsets.Dispose();
        lookupTables.Dispose();
    }
}

