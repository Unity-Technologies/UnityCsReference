// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEditor.JobsProfiling;

internal struct EventRef : IEquatable<EventRef>
{
    internal int frameCacheIndex;
    internal int eventIndex;

    internal long key => ((long)frameCacheIndex << 32) | (uint)eventIndex;

    public bool Equals(EventRef other)
    {
        return frameCacheIndex == other.frameCacheIndex && eventIndex == other.eventIndex;
    }

    public override bool Equals(object obj)
    {
        return obj is EventRef other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(frameCacheIndex, eventIndex);
    }

    public override string ToString()
    {
        return "frame " + frameCacheIndex + " event " + eventIndex;
    }
}

internal struct Strider : IComparable<Strider>
{
    internal EventRef source;
    internal ulong threadId;
    internal FixedString128Bytes threadName;
    internal ProfilingEvent profEvent;
    internal ulong handle;

    public int CompareTo(Strider other)
    {
        int result = threadId.CompareTo(other.threadId);

        if (result == 0)
            result = profEvent.level.CompareTo(other.profEvent.level);

        if (result == 0)
            result = profEvent.startTime.CompareTo(other.profEvent.startTime);

        return result;
    }
}

/// <summary>
/// Links the piece of a frame to the piece that continues it in the next frame. Frames are processed in order so a
/// continuation of a continuation resolves to the first head.
/// </summary>
[BurstCompile]
internal struct StitchFramePairJob : IJob
{
    [ReadOnly] internal NativeList<ProfilingEvent> m_previousEvents;
    [ReadOnly] internal NativeArray<ThreadInfo> m_previousThreads;
    [ReadOnly] internal NativeList<ProfilingEvent> m_currentEvents;
    [ReadOnly] internal NativeArray<ThreadInfo> m_currentThreads;
    [ReadOnly] internal NativeHashMap<int, double> m_frameStartTimes;
    internal int m_previousFrame;
    internal int m_currentFrame;
    internal NativeHashMap<long, EventRef> m_continuationToHead;
    internal NativeHashMap<long, float> m_headEndTime;

    public void Execute()
    {
        var continuing = new NativeList<int>(16, Allocator.Temp);
        var starting = new NativeList<int>(16, Allocator.Temp);

        for (int p = 0; p < m_previousThreads.Length; ++p)
        {
            var previousThread = m_previousThreads[p];

            int currentThreadIndex = FindThread(previousThread.threadId);

            if (currentThreadIndex < 0)
                continue;

            var currentThread = m_currentThreads[currentThreadIndex];
            continuing.Clear();
            starting.Clear();

            for (int i = previousThread.eventStart; i < previousThread.eventEnd; ++i)
            {
                if ((m_previousEvents[i].flags & (ushort)ProfilingEventFlags.ContinuesInNextFrame) != 0)
                    continuing.Add(i);
            }

            for (int i = currentThread.eventStart; i < currentThread.eventEnd; ++i)
            {
                if ((m_currentEvents[i].flags & (ushort)ProfilingEventFlags.StartedInPreviousFrame) != 0)
                    starting.Add(i);
            }

            int pairs = math.min(continuing.Length, starting.Length);

            for (int i = 0; i < pairs; ++i)
                Link(continuing[i], starting[i]);
        }
    }

    int FindThread(ulong threadId)
    {
        for (int i = 0; i < m_currentThreads.Length; ++i)
        {
            if (m_currentThreads[i].threadId == threadId)
                return i;
        }

        return -1;
    }

    void Link(int previousEventIndex, int currentEventIndex)
    {
        var previousRef = new EventRef { frameCacheIndex = m_previousFrame, eventIndex = previousEventIndex };

        if (!m_continuationToHead.TryGetValue(previousRef.key, out EventRef head))
            head = previousRef;

        var currentRef = new EventRef { frameCacheIndex = m_currentFrame, eventIndex = currentEventIndex };
        m_continuationToHead[currentRef.key] = head;

        var continuation = m_currentEvents[currentEventIndex];
        double offset = m_frameStartTimes[m_currentFrame] - m_frameStartTimes[head.frameCacheIndex];
        m_headEndTime[head.key] = (float)offset + continuation.startTime + continuation.time;
    }
}

[BurstCompile]
internal struct CollectWindowHandlesJob : IJob
{
    [ReadOnly] internal NativeHashMap<int, ulong> m_handles;
    internal NativeHashSet<ulong> m_output;

    public void Execute()
    {
        foreach (var pair in m_handles)
            m_output.Add(pair.Value);
    }
}

/// <summary>
/// Collects the stitched heads that live in one frame, with times relative to the start of the first frame in the window.
/// </summary>
[BurstCompile]
internal struct GatherStridersJob : IJob
{
    [ReadOnly] internal NativeList<ProfilingEvent> m_events;
    [ReadOnly] internal NativeArray<ThreadInfo> m_threads;
    [ReadOnly] internal NativeHashMap<int, ulong> m_handles;
    [ReadOnly] internal NativeHashMap<long, float> m_headEndTime;
    internal int m_frame;
    internal double m_frameStart;
    internal double m_windowStart;
    internal NativeList<Strider> m_output;

    public void Execute()
    {
        foreach (var pair in m_headEndTime)
        {
            if ((int)(pair.Key >> 32) != m_frame)
                continue;

            int eventIndex = (int)(pair.Key & 0xFFFFFFFF);
            var profEvent = m_events[eventIndex];
            var thread = m_threads[profEvent.threadIndex];
            m_handles.TryGetValue(eventIndex, out ulong handle);

            profEvent.time = pair.Value - profEvent.startTime;
            profEvent.startTime += (float)(m_frameStart - m_windowStart);

            m_output.Add(new Strider
            {
                source = new EventRef { frameCacheIndex = m_frame, eventIndex = eventIndex },
                threadId = thread.threadId,
                threadName = thread.name,
                profEvent = profEvent,
                handle = handle,
            });
        }
    }
}

[BurstCompile]
internal struct BuildStriderFrameJob : IJob
{
    [ReadOnly] internal NativeList<Strider> m_striders;
    internal NativeList<ProfilingEvent> m_events;
    internal NativeArray<ThreadInfo> m_threads;
    internal NativeHashMap<int, ulong> m_handles;
    internal NativeList<EventRef> m_sources;

    public void Execute()
    {
        var sorted = new NativeArray<Strider>(m_striders.AsArray(), Allocator.Temp);
        sorted.Sort();

        int threadCount = 0;

        for (int i = 0; i < sorted.Length; ++i)
        {
            var strider = sorted[i];

            if (threadCount == 0 || m_threads[threadCount - 1].threadId != strider.threadId)
            {
                if (threadCount > 0)
                {
                    var finished = m_threads[threadCount - 1];
                    finished.eventEnd = i;
                    m_threads[threadCount - 1] = finished;
                }

                m_threads[threadCount++] = new ThreadInfo { name = strider.threadName, threadId = strider.threadId, eventStart = i };
            }

            var thread = m_threads[threadCount - 1];
            thread.maxDepth = math.max(thread.maxDepth, strider.profEvent.level + 1);
            m_threads[threadCount - 1] = thread;

            var profEvent = strider.profEvent;
            profEvent.threadIndex = (ushort)(threadCount - 1);
            profEvent.parentIndex = -1;
            m_events.Add(profEvent);
            m_sources.Add(strider.source);

            if (strider.handle != 0)
                m_handles[i] = strider.handle;
        }

        if (threadCount > 0)
        {
            var last = m_threads[threadCount - 1];
            last.eventEnd = sorted.Length;
            m_threads[threadCount - 1] = last;
        }
    }
}

/// <summary>
/// The native profiler splits a sample that is open at a thread's frame boundary into one piece per frame.
/// This links the pieces back together across a window of consecutive cached frames: the first piece is the
/// head, and every following piece is a continuation that resolves to it. All the work is done in jobs. Anything
/// that reads the results has to depend on <see cref="handle"/>.
/// </summary>
internal class StitchedEvents : IDisposable
{
    NativeHashMap<long, EventRef> m_continuationToHead;
    NativeHashMap<long, float> m_headEndTime;
    NativeHashMap<int, double> m_frameStartTimes;
    NativeHashSet<ulong> m_windowHandles;
    NativeList<Strider> m_striders;
    FrameData m_striderData;
    NativeList<EventRef> m_striderSources;
    NativeList<EventRef> m_noSources;
    NativeHashMap<long, EventRef> m_noContinuations;
    NativeHashMap<long, float> m_noHeadEndTimes;
    Dictionary<int, FrameData> m_frames;
    JobHandle m_handle;

    StitchedEvents(int frameCount)
    {
        m_continuationToHead = new NativeHashMap<long, EventRef>(math.max(frameCount * 16, 16), Allocator.Persistent);
        m_headEndTime = new NativeHashMap<long, float>(math.max(frameCount * 16, 16), Allocator.Persistent);
        m_frameStartTimes = new NativeHashMap<int, double>(math.max(frameCount, 1), Allocator.Persistent);
        m_windowHandles = new NativeHashSet<ulong>(math.max(frameCount * 16, 16), Allocator.Persistent);
        m_striders = new NativeList<Strider>(16, Allocator.Persistent);
        m_striderSources = new NativeList<EventRef>(16, Allocator.Persistent);
        m_noSources = new NativeList<EventRef>(1, Allocator.Persistent);
        m_noContinuations = new NativeHashMap<long, EventRef>(1, Allocator.Persistent);
        m_noHeadEndTimes = new NativeHashMap<long, float>(1, Allocator.Persistent);
        m_frames = new Dictionary<int, FrameData>(frameCount);
    }

    internal JobHandle handle => m_handle;

    internal NativeHashMap<long, EventRef> continuations => m_continuationToHead;

    internal NativeHashMap<long, float> headEndTimes => m_headEndTime;

    internal NativeHashSet<ulong> windowHandles => m_windowHandles;

    /// <summary>
    /// The stitched events as a frame of their own. Event start times are relative to the start of the first frame in the window.
    /// </summary>
    internal FrameData striderData => m_striderData;

    /// <summary>
    /// For each event in striderData the head event it was created from.
    /// </summary>
    internal NativeList<EventRef> striderSources => m_striderSources;

    internal NativeList<EventRef> noSources => m_noSources;

    internal NativeHashMap<long, EventRef> noContinuations => m_noContinuations;

    internal NativeHashMap<long, float> noHeadEndTimes => m_noHeadEndTimes;

    internal static StitchedEvents Build(IReadOnlyList<FrameData> frames)
    {
        var stitched = Schedule(frames, default);
        stitched.m_handle.Complete();
        return stitched;
    }

    internal static StitchedEvents Schedule(IReadOnlyList<FrameData> frames, JobHandle dependency)
    {
        var stitched = new StitchedEvents(frames.Count);
        int threadBound = 0;

        for (int i = 0; i < frames.Count; ++i)
        {
            int frameIndex = frames[i].info[0].frameIndex;
            stitched.m_frames[frameIndex] = frames[i];
            stitched.m_frameStartTimes[frameIndex] = frames[i].info[0].startTime;
            threadBound += frames[i].threads.Length;
        }

        var handle = dependency;

        for (int i = 0; i < frames.Count; ++i)
        {
            handle = new CollectWindowHandlesJob
            {
                m_handles = frames[i].eventHandleLookup,
                m_output = stitched.m_windowHandles,
            }.Schedule(handle);
        }

        for (int i = 1; i < frames.Count; ++i)
        {
            var previous = frames[i - 1];
            var current = frames[i];

            if (current.info[0].frameIndex != previous.info[0].frameIndex + 1)
                continue;

            handle = new StitchFramePairJob
            {
                m_previousEvents = previous.events,
                m_previousThreads = previous.threads,
                m_currentEvents = current.events,
                m_currentThreads = current.threads,
                m_frameStartTimes = stitched.m_frameStartTimes,
                m_previousFrame = previous.info[0].frameIndex,
                m_currentFrame = current.info[0].frameIndex,
                m_continuationToHead = stitched.m_continuationToHead,
                m_headEndTime = stitched.m_headEndTime,
            }.Schedule(handle);
        }

        if (frames.Count > 0)
            handle = stitched.ScheduleStriderFrame(frames, threadBound, handle);

        stitched.m_handle = handle;
        return stitched;
    }

    internal bool IsContinuation(int frameCacheIndex, int eventIndex)
    {
        m_handle.Complete();
        return m_continuationToHead.ContainsKey(new EventRef { frameCacheIndex = frameCacheIndex, eventIndex = eventIndex }.key);
    }

    internal bool TryGetHead(int frameCacheIndex, int eventIndex, out EventRef head)
    {
        m_handle.Complete();
        return m_continuationToHead.TryGetValue(new EventRef { frameCacheIndex = frameCacheIndex, eventIndex = eventIndex }.key, out head);
    }

    /// <summary>
    /// End time of the event in milliseconds from the start of its own frame. For a stitched head this is the
    /// end of its last continuation.
    /// </summary>
    internal float GetEndTime(int frameCacheIndex, int eventIndex)
    {
        m_handle.Complete();
        var key = new EventRef { frameCacheIndex = frameCacheIndex, eventIndex = eventIndex }.key;

        if (m_headEndTime.TryGetValue(key, out var endTime))
            return endTime;

        var profEvent = m_frames[frameCacheIndex].events[eventIndex];
        return profEvent.startTime + profEvent.time;
    }

    public void Dispose()
    {
        m_handle.Complete();
        m_continuationToHead.Dispose();
        m_headEndTime.Dispose();
        m_frameStartTimes.Dispose();
        m_windowHandles.Dispose();
        m_striders.Dispose();
        m_striderSources.Dispose();
        m_noSources.Dispose();
        m_noContinuations.Dispose();
        m_noHeadEndTimes.Dispose();

        m_striderData.info.Dispose();
        m_striderData.events.Dispose();
        m_striderData.threads.Dispose();
        m_striderData.eventHandleLookup.Dispose();
        m_striderData.jobFlows.Dispose();
        m_striderData.catColors.Dispose();
        m_striderData.catColorsColorBlind.Dispose();
    }

    static NativeList<Color32> MergeColors(IReadOnlyList<FrameData> frames, bool colorBlind)
    {
        var merged = new NativeList<Color32>(32, Allocator.Persistent);

        for (int i = 0; i < frames.Count; ++i)
        {
            var colors = colorBlind ? frames[i].catColorsColorBlind : frames[i].catColors;

            if (!colors.IsCreated)
                continue;

            if (colors.Length > merged.Length)
                merged.Resize(colors.Length, NativeArrayOptions.ClearMemory);

            for (int c = 0; c < colors.Length; ++c)
            {
                if (colors[c].a != 0 || merged[c].a == 0)
                    merged[c] = colors[c];
            }
        }

        return merged;
    }

    JobHandle ScheduleStriderFrame(IReadOnlyList<FrameData> frames, int threadBound, JobHandle dependency)
    {
        double windowStart = frames[0].info[0].startTime;
        var handle = dependency;

        for (int i = 0; i < frames.Count; ++i)
        {
            handle = new GatherStridersJob
            {
                m_events = frames[i].events,
                m_threads = frames[i].threads,
                m_handles = frames[i].eventHandleLookup,
                m_headEndTime = m_headEndTime,
                m_frame = frames[i].info[0].frameIndex,
                m_frameStart = frames[i].info[0].startTime,
                m_windowStart = windowStart,
                m_output = m_striders,
            }.Schedule(handle);
        }

        var info = new NativeArray<FrameInfo>(1, Allocator.Persistent);
        info[0] = new FrameInfo { startTime = windowStart, frameIndex = -1 };

        m_striderData = new FrameData
        {
            info = info,
            events = new NativeList<ProfilingEvent>(16, Allocator.Persistent),
            threads = new NativeArray<ThreadInfo>(math.max(threadBound, 1), Allocator.Persistent),
            eventHandleLookup = new NativeHashMap<int, ulong>(16, Allocator.Persistent),
            jobFlows = new NativeList<JobFlow>(1, Allocator.Persistent),
            catColors = MergeColors(frames, false),
            catColorsColorBlind = MergeColors(frames, true),
        };

        return new BuildStriderFrameJob
        {
            m_striders = m_striders,
            m_events = m_striderData.events,
            m_threads = m_striderData.threads,
            m_handles = m_striderData.eventHandleLookup,
            m_sources = m_striderSources,
        }.Schedule(handle);
    }
}
