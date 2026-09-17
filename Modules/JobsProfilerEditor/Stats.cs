// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.Jobs;
using Unity.Burst;
using System;
using UnityEngine.UIElements.Experimental;

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.JobsProfiling;

struct MinMaxAvg
{
    /// Avarage value for the type
    internal double avg;
    /// Minimum value for the type
    internal float min;
    /// Maximum value for the type
    internal float max;
    /// Which frame the minium value occured on
    internal int minFrame;
    /// Which event in the frame of the minimum value
    internal int minEvent;
    /// Which frame the maximum value occured on.
    internal int maxFrame;
    /// Which event in the frame of the maximum value.
    internal int maxEvent;
    /// Total times this event occured
    internal int count;

    internal void Reset()
    {
        min = float.MaxValue;
        max = float.MinValue;
        minFrame = 0;
        maxFrame = 0;
        minEvent = 0;
        maxEvent = 0;
        avg = 0.0f;
        count = 0;
    }

    internal void Update(float value, int evt)
    {
        if (value < min)
        {
            min = value;
            minEvent = evt;
        }

        if (value > max)
        {
            max = value;
            maxEvent = evt;
        }

        avg += value;
        count++;
    }

    internal void UpdateFrame(int frame)
    {
        minFrame = frame;
        maxFrame = frame;
    }

    internal void UpdateWithFrame(in MinMaxAvg v)
    {
        if (v.min < min)
        {
            min = v.min;
            minFrame = v.minFrame;
            minEvent = v.minEvent;
        }

        if (v.max > max)
        {
            max = v.max;
            maxFrame = v.maxFrame;
            maxEvent = v.maxEvent;
        }

        avg += v.avg;
        count += v.count;
    }
}

struct OutputStatsData
{
    /// Duration for the job
    internal MinMaxAvg time;
    /// Time it took to schedule the job
    internal MinMaxAvg startTime;
    /// Time spent waiting for the job to be finished
    internal MinMaxAvg waitTime;
    /// Total number of times the job was executed
    internal int totalCount;
    // Internal data not displayed in the UI
    internal int jobId;

    internal void Reset()
    {
        time.Reset();
        startTime.Reset();
        waitTime.Reset();
        totalCount = 0;
    }
}

class DataValue
{
    internal enum Type
    {
        String,
        Float,
        Int,
    }

    internal Type dataType;
    internal string stringData;
    internal float floatData;
    internal int intData;
    internal bool isLink;
    internal int frameIndex;
    internal int eventIndex;
}


class ManagedStatsData
{
    /// Duration for the job
    internal DataValue[] values = null;
    /// Id for the job (usually the markerId from the ProfilingDriver)
    internal int jobId;
}

/// <summary>
/// Generates stats for a single frame
/// </summary>
[BurstCompile()]
struct GenerateFrameStats : IJob
{
    [ReadOnly]
    internal FrameData m_frame;

    internal NativeList<OutputStatsData> m_stats;

    bool GetFlowTime(ulong jobHandle, JobFlowState state, ref float outValue)
    {
        for (int i = 0, count = m_frame.jobFlows.Length; i < count; ++i)
        {
            var flow = m_frame.jobFlows[i];

            if (flow.handle.ToUlong() == jobHandle && flow.state == state)
            {
                outValue =
                    m_frame.events[flow.eventIndex].startTime +
                    m_frame.events[flow.eventIndex].time;
                return true;
            }
        }

        return false;
    }
    public void Execute()
    {
        // Maps a marker id to a index in the stats array
        NativeHashMap<int, int> makerIdIndex = new NativeHashMap<int, int>(32, Allocator.Temp);

        m_stats.Clear();

        foreach (var v in m_frame.eventHandleLookup)
        {
            int index;
            int i = v.Key;

            ProfilingEvent e = m_frame.events[i];

            if (!makerIdIndex.TryGetValue(e.markerId, out index))
            {
                index = m_stats.Length;
                makerIdIndex.Add(e.markerId, index);
                var statsData = new OutputStatsData();
                statsData.Reset();
                statsData.jobId = e.markerId;
                m_stats.Add(statsData);
            }

            OutputStatsData data = m_stats[index];

            float startTime = e.startTime;
            float time = e.time;

            data.time.Update(time, i);

            float scheduleTime = 0.0f;
            float waitedOnTime = 0.0f;
            ulong jobHandle = v.Value;

            if (GetFlowTime(jobHandle, JobFlowState.BeginSchedule, ref scheduleTime))
            {
                data.startTime.Update(startTime - scheduleTime, i);
            }

            if (GetFlowTime(jobHandle, JobFlowState.WaitedOn, ref waitedOnTime))
            {
                float waitTime = waitedOnTime - (startTime + time);
                data.waitTime.Update(waitTime, i);
            }

            data.totalCount++;

            m_stats[index] = data;
        }

        int frameIndex = m_frame.info[0].frameIndex;

        for (int i = 0, count = m_stats.Length; i < count; ++i)
        {
            OutputStatsData data = m_stats[i];
            data.time.UpdateFrame(frameIndex);
            data.startTime.UpdateFrame(frameIndex);
            data.waitTime.UpdateFrame(frameIndex);
            m_stats[i] = data;
        }

        makerIdIndex.Dispose();
    }
}

/// <summary>
/// Combine output from one job to another
/// </summary>
[BurstCompile()]
struct CombineStats : IJob
{
    /// All the frames to combine
    [ReadOnly]
    internal NativeList<OutputStatsData> m_input;

    /// Combined output
    internal NativeList<OutputStatsData> m_output;

    public void Execute()
    {
        foreach (var input in m_input)
        {
            int index = -1;

            for (int i = 0, count = m_output.Length; i < count; ++i)
            {
                if (m_output[i].jobId == input.jobId)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                index = m_output.Length;
                var t = new OutputStatsData();
                t.Reset();
                t.jobId = input.jobId;
                m_output.Add(t);
            }

            OutputStatsData outputData = m_output[index];

            outputData.time.UpdateWithFrame(input.time);
            outputData.startTime.UpdateWithFrame(input.startTime);
            outputData.waitTime.UpdateWithFrame(input.waitTime);
            outputData.totalCount += input.totalCount;

            m_output[index] = outputData;
        }
    }
}

internal struct ClickLinkEvent
{
    // This is set to true if we only want to select an event temporary
    internal bool hover;
    internal int jobId;
    internal int frameIndex;
    internal int eventIndex;

    /// Frame 0 and event 0 are both valid, so -1 is the "no such event" sentinel
    internal static ClickLinkEvent CreateEmpty()
    {
        return new ClickLinkEvent
        {
            hover = false,
            jobId = 0,
            frameIndex = -1,
            eventIndex = -1,
        };
    }
};

internal class Stats : VisualElement, IDisposable
{
    FrameCache m_frameCache;
    [SerializeField] MultiColumnListView m_statsList;
    List<ManagedStatsData> m_stats;
    JobHandle m_jobHandle = new JobHandle();
    Hash128 m_hash = new Hash128();
    NativeList<OutputStatsData> m_jobOutputStats;
    List<NativeList<OutputStatsData>> m_tempClearLists;
    Filter m_filter;
    int m_selectedFrame = 0;
    State m_state = State.Idle;
    StyleSheet m_labelStyleSheet;
    ClickLinkEvent m_clickEvent = ClickLinkEvent.CreateEmpty();

    int m_lastCount = 0;

    const int kRowShift = 8;
    const int kRowMask = (1 << kRowShift) - 1;

    enum State
    {
        Idle,
        WaitingForResult,
        RetryFrame,
    };

    [NoAutoStaticsCleanup]
    static string[] m_columnNames =
    {
        "Job Name",
        "Total Count",
        "Minimum Time",
        "Average Time",
        "Maximum Time",
        "Minimum Start Time",
        "Average Start Time",
        "Maximum Start Time",
        "Minimum Wait Time",
        "Average Wait Time",
        "Maximum Wait Time",
    };

    internal StyleSheet LabelStyles { get { return m_labelStyleSheet; } }

    internal Stats(VisualElement parent, FrameCache frameCache, Filter filter)
    {
        m_stats = new List<ManagedStatsData>(32);
        m_jobOutputStats = new NativeList<OutputStatsData>(32, Allocator.Persistent);
        m_tempClearLists = new List<NativeList<OutputStatsData>>(16);
        m_filter = filter;

        m_labelStyleSheet = JobsProfilerResources.LoadStyleSheet(JobsProfilerResources.StatsUss);

        var columns = new Columns();
        int i = 0;

        foreach (var name in m_columnNames)
        {
            var c = new Column();
            c.name = name;
            c.title = name;
            var index = i++;
            c.makeCell = () =>
            {
                var label = new Label();
                label.RegisterCallback<PointerUpLinkTagEvent>(LinkUpClicked);
                label.RegisterCallback<PointerOverLinkTagEvent>(HyperlinkOnPointerOver);
                label.RegisterCallback<PointerOutLinkTagEvent>(HyperlinkOnPointerOut);
                label.styleSheets.Add(m_labelStyleSheet);
                label.style.paddingLeft = 6;
                label.userData = index;
                return label;
            };
            c.bindCell = ColumnData;
            c.minWidth = 100;
            c.resizable = true;
            c.stretchable = true;
            columns.Add(c);
        }

        m_statsList = new MultiColumnListView(columns);
        m_statsList.viewDataKey = "JobsProfiler.Stats";
        m_statsList.itemsSource = m_stats;
        m_statsList.styleSheets.Add(m_labelStyleSheet);
        m_statsList.sortingMode = ColumnSortingMode.Custom;
        m_statsList.columnSortingChanged += SortingChanged;

        Show();

        styleSheets.Add(m_labelStyleSheet);
        AddToClassList("jobs-profiler-main-background");
        parent.Add(this);

        m_frameCache = frameCache;
    }

    void ColumnData(VisualElement element, int row)
    {
        Label label = element as Label;
        int columnIndex = (int)element.userData;
        DataValue dataValue = m_stats[row].values[columnIndex];

        switch (dataValue.dataType)
        {
            case DataValue.Type.String:
                {
                    if (dataValue.isLink)
                        label.text = String.Format("<link=\"{0}\"><color={1}>{2}</color></link>",
                                                            (row << kRowShift) + columnIndex, JobsProfilerSettings.LinkColorHex, dataValue.stringData);
                    else
                        label.text = dataValue.stringData;

                    break;
                }
            case DataValue.Type.Int:
                {
                    if (dataValue.isLink)
                        label.text = String.Format("<link=\"{0}\"><color={1}>{2}</color></link>",
                                                            (row << kRowShift) + columnIndex, JobsProfilerSettings.LinkColorHex, dataValue.intData);
                    else
                        label.text = dataValue.intData.ToString();

                    break;
                }
            case DataValue.Type.Float:
                {
                    if (dataValue.floatData == float.MaxValue || dataValue.floatData == float.MinValue)
                    {
                        label.text = "N/A";
                        label.SetEnabled(false);
                    }
                    else if (dataValue.isLink)
                    {
                        label.text = String.Format("<link=\"{0}\"><color={1}>{2:0.000} ms</color></link>",
                                                  (row << kRowShift) + columnIndex, JobsProfilerSettings.LinkColorHex, dataValue.floatData);
                        label.SetEnabled(true);
                    }
                    else
                    {
                        label.text = String.Format("{0:0.000} ms", dataValue.floatData);
                        label.SetEnabled(true);
                    }

                    break;
                }
        }
    }

    static internal void HyperlinkOnPointerOver(PointerOverLinkTagEvent evt)
    {
        var label = evt.currentTarget as Label;
        label.AddToClassList("link-cursor");
        // Add underline tags around the link content
        string text = label.text;
        int start = text.IndexOf('>', text.IndexOf("<color=")) + 1;
        int end = text.IndexOf("</color>");
        if (start > 0 && end > start)
        {
            label.text = text.Insert(end, "</u>").Insert(start, "<u>");
        }
    }
    static internal void HyperlinkOnPointerOut(PointerOutLinkTagEvent evt)
    {
        var label = evt.target as Label;
        label.RemoveFromClassList("link-cursor");
        // Remove underline tags
        label.text = label.text.Replace("<u>", "").Replace("</u>", "");
    }

    void LinkUpClicked(PointerUpLinkTagEvent evt)
    {
        var linkID = int.Parse(evt.linkID);
        var row = linkID >> kRowShift;
        var column = (linkID & kRowMask);
        DataValue v = m_stats[row].values[column];
        m_clickEvent.hover = false;
        m_clickEvent.frameIndex = v.frameIndex;
        m_clickEvent.eventIndex = v.eventIndex;
        m_clickEvent.jobId = m_stats[row].jobId;
    }

    // TODO: Better way to do this
    void SortingChanged()
    {
        if (m_stats.Count == 0)
            return;

        foreach (var v in m_statsList.sortedColumns)
        {
            // The API doesn't set columnIndex so we have to loop to find it :(
            int i = 0;

            foreach (var n in m_columnNames)
            {
                if (n == v.columnName)
                    break;

                ++i;
            }

            if (i >= m_columnNames.Length)
                continue;

            switch (m_stats[0].values[i].dataType)
            {
                case DataValue.Type.String:
                    {
                        var index = i;
                        if (v.direction == SortDirection.Ascending)
                            m_stats.Sort((a, b) => (a.values[index].stringData.CompareTo(b.values[index].stringData)));
                        else
                            m_stats.Sort((a, b) => (b.values[index].stringData.CompareTo(a.values[index].stringData)));

                        break;
                    }
                case DataValue.Type.Int:
                    {
                        var index = i;
                        if (v.direction == SortDirection.Ascending)
                            m_stats.Sort((a, b) => (a.values[index].intData.CompareTo(b.values[index].intData)));
                        else
                            m_stats.Sort((a, b) => (b.values[index].intData.CompareTo(a.values[index].intData)));

                        break;
                    }
                case DataValue.Type.Float:
                    {
                        var index = i;
                        if (v.direction == SortDirection.Ascending)
                            m_stats.Sort((a, b) => (a.values[index].floatData.CompareTo(b.values[index].floatData)));
                        else
                            m_stats.Sort((a, b) => (b.values[index].floatData.CompareTo(a.values[index].floatData)));

                        break;
                    }
            }
        }

        m_statsList.RefreshItems();
    }

    public void Dispose()
    {
        m_jobHandle.Complete();

        // The per-frame TempJob lists are normally released by UpdateStats once it has consumed the
        // combined output, but that never runs if the view is closed while a stats job is pending, so
        // release them here as well.
        foreach (var v in m_tempClearLists)
            v.Dispose();

        m_tempClearLists.Clear();

        if (m_jobOutputStats.IsCreated)
            m_jobOutputStats.Dispose();
    }

    internal void ClearData()
    {
        m_jobHandle.Complete();
        m_jobOutputStats.Clear();
        UpdateStats();
    }

    JobHandle SetupGenerateFrameStatsJob(in FrameData frameData)
    {
        var frameJob = new GenerateFrameStats
        {
            m_frame = frameData,
            m_stats = new NativeList<OutputStatsData>(1024, Allocator.TempJob),
        };

        m_tempClearLists.Add(frameJob.m_stats);

        return frameJob.Schedule();
    }

    /// <summary>
    /// Set up the jobs for calculating the stats for some frames. 0 indicates that all frames should be used.
    /// </summary>
    void CalculateStats(int frameIndex)
    {
        m_stats.Clear();
        m_jobOutputStats.Clear();

        var cachedFrames = m_frameCache.Frames;

        if (frameIndex == 0 && cachedFrames.Count == 0)
            return;

        FrameData frameData;

        // If we get here and the user just selected a frame it may not be in the cache yet
        // So we set the state that we should retrying getting the frame data next update
        if (!cachedFrames.TryGetValue(frameIndex, out frameData))
        {
            m_state = State.RetryFrame;
            return;
        }

        m_jobHandle = SetupGenerateFrameStatsJob(frameData);

        foreach (var v in m_tempClearLists)
        {
            var cjob = new CombineStats
            {
                m_input = v,
                m_output = m_jobOutputStats,
            };

            m_jobHandle = cjob.Schedule(m_jobHandle);
        }

        m_state = State.WaitingForResult;
    }

    int FillMinMaxAvg(ref DataValue[] values, int index, MinMaxAvg stats, int jobId)
    {
        values[index + 0] = new DataValue();
        values[index + 1] = new DataValue();
        values[index + 2] = new DataValue();

        values[index + 0].dataType = DataValue.Type.Float;
        values[index + 0].floatData = stats.min;
        values[index + 0].frameIndex = stats.minFrame;
        values[index + 0].eventIndex = stats.minEvent;
        values[index + 0].isLink = true;

        float avg = float.MaxValue;

        if (stats.count > 0)
            avg = (float)(stats.avg / stats.count);

        values[index + 1].dataType = DataValue.Type.Float;
        values[index + 1].floatData = avg;
        values[index + 1].frameIndex = 0;
        values[index + 1].eventIndex = -1;
        values[index + 1].isLink = false;

        values[index + 2].dataType = DataValue.Type.Float;
        values[index + 2].floatData = stats.max;
        values[index + 2].frameIndex = stats.maxFrame;
        values[index + 2].eventIndex = stats.maxEvent;
        values[index + 2].isLink = true;

        return index + 3;
    }

    void UpdateStats()
    {
        m_stats.Clear();
        var filters = m_filter.FilterIds;
        var useFilters = m_filter.UseFilter;

        foreach (var stats in m_jobOutputStats)
        {
            // If we have a filter enable we need to make sure to filter the events
            if (useFilters && !filters.Contains(stats.jobId))
                continue;

            // TODO: Fixed hard-coded size
            DataValue[] values = new DataValue[16];
            values[0] = new DataValue();
            values[0].dataType = DataValue.Type.String;
            values[0].stringData = m_frameCache.FindString(stats.jobId);
            values[0].isLink = false;
            values[0].eventIndex = -1;

            values[1] = new DataValue();
            values[1].dataType = DataValue.Type.Int;
            values[1].intData = stats.totalCount;

            int index = 2;

            index = FillMinMaxAvg(ref values, index, stats.time, stats.jobId);
            index = FillMinMaxAvg(ref values, index, stats.startTime, stats.jobId);
            FillMinMaxAvg(ref values, index, stats.waitTime, stats.jobId);

            m_stats.Add(new ManagedStatsData
            {
                values = values,
                jobId = stats.jobId,
            });
        }

        foreach (var v in m_tempClearLists)
            v.Dispose();

        m_tempClearLists.Clear();

        // If the number of stats has changed, we need to rebuild the list. If count has been
        // zero twice we don't rebuild
        if ((m_lastCount != m_jobOutputStats.Length) || m_filter.HasChanged)
        {
            m_statsList.RefreshItems();
        }

        m_lastCount = m_jobOutputStats.Length;

        m_state = State.Idle;
    }

    internal void Update()
    {
        // If we failed to get the frame data (like it may haven't been cached yet) we will retry
        if (m_state == State.RetryFrame)
        {
            CalculateStats(m_selectedFrame);
        }
        // if the selectedFrame is 0, we want to calculate stats for all frames.
        // We calculate a shallow hash for the frames that are in the frame cache
        // and if it differes (like when new frames has been added), we need to recalculat the stats.
        else if (m_selectedFrame == 0)
        {
            Hash128 frameCacheHash = m_frameCache.CalculateShallowHash();
            if (frameCacheHash != m_hash && m_state == State.Idle)
            {
                CalculateStats(m_selectedFrame);
                m_hash = frameCacheHash;
            }
        }

        if (m_state == State.WaitingForResult || m_filter.HasChanged)
        {
            if (m_jobHandle.IsCompleted)
            {
                m_jobHandle.Complete();

                UpdateStats();
                SortingChanged();
            }
        }
    }
    internal void Show()
    {
        Add(m_statsList);
    }
    internal void Hide()
    {
        Remove(m_statsList);
    }

    internal void SelectFrame(int index)
    {
        // If we have selected the frame or are waiting for result we can't update the selection
        if (m_selectedFrame == index || m_state == State.WaitingForResult)
            return;

        CalculateStats(index);

        m_selectedFrame = index;
        m_statsList.RefreshItems();
    }

    internal void SelectRowByMarkerId(int id)
    {
        for (int i = 0, count = m_stats.Count; i < count; ++i)
        {
            if (m_stats[i].jobId == id)
            {
                m_statsList.ScrollToItem(i);
                m_statsList.selectedIndex = i;
                return;
            }
        }
    }

    internal ClickLinkEvent GetClickLinkEvent()
    {
        ClickLinkEvent evt = m_clickEvent;
        m_clickEvent = ClickLinkEvent.CreateEmpty();
        return evt;
    }
}

