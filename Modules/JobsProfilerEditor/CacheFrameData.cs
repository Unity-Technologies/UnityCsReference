// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

/// <summary>
/// Unity Jobs Profiler Data Caching System
///
/// This file implements the caching system for Unity's Jobs Profiler, which transforms
/// raw profiler data into an optimized format for visualization and analysis. It consists of:
///
/// 1. Data Structures:
///    - ProfilingEvent: Represents a single profiled event with timing, hierarchy, and metadata
///    - FrameData: Contains all events, threads, jobs, and their relationships for a frame
///    - ThreadInfo/ThreadGroup: Organizes and manages thread visualization
///
/// 2. Caching Pipeline:
///    - CacheFrameJob: Processes raw profiler data, extracting events and job information
///    - CacheFixupJob: Reorganizes events by hierarchy level for efficient rendering
///    - FrameCache: Manages frame caching using background jobs for responsive UI
///
/// 3. Async Data Caching:
///    - Tracks job scheduling, dependencies, and execution across threads
///    - Manages the relationship between JobHandles and profiling events
///    - Preserves job flow state transitions (scheduled, waited on, completed)
///
/// The caching is performed asynchronously using Unity's job system to maintain UI responsiveness
/// while processing potentially large amounts of profiling data.
/// </summary>

//#define LOGGING
//#define CACHE_LOGGING

using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Jobs;
using UnityEditorInternal;
using UnityEditor.Profiling;
using Unity.Mathematics;
using System;
using System.Text;
using UnityEditor;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;

namespace UnityEditor.JobsProfiling;

/// <summary>
/// This data is gathered as cache from RawFrameView data for faster access during rendering
/// and analysis. Each ProfilingEvent represents a single profiled event in the frame.
/// </summary>
internal struct ProfilingEvent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilingEvent"/> struct.
    /// </summary>
    /// <param name="startTime">Start time of the event in milliseconds from the beginning of the frame.</param>
    /// <param name="time">Duration of the event in milliseconds.</param>
    /// <param name="threadIndex">Index of the thread (0 is typically the main thread, others are job threads).</param>
    /// <param name="level">Hierarchical depth in the callstack where the event occurred.</param>
    /// <param name="categoryId">Category identifier used for determining event color and grouping.</param>
    /// <param name="markerId">Unique identifier for the marker name used for this event.</param>
    /// <param name="parentIndex">Index to the parent event in the hierarchical callstack.</param>
    internal ProfilingEvent(float startTime, float time, ushort threadIndex, ushort level, ushort categoryId, int markerId, int parentIndex)
    {
        this.startTime = startTime;
        this.time = time;
        this.markerId = markerId;
        this.threadIndex = threadIndex;
        this.level = level;
        this.categoryId = categoryId;
        this.parentIndex = parentIndex;
        this.pad0 = 0;
    }

    /// <summary>
    /// Start time of the event in milliseconds from the beginning of the frame.
    /// </summary>
    internal float startTime;
    /// <summary>
    /// Duration of the event in milliseconds.
    /// </summary>
    internal float time;
    /// <summary>
    /// Unique identifier for the marker name used for this event.
    /// </summary>
    internal int markerId;
    /// <summary>
    /// Index to the parent event in the hierarchical callstack.
    /// </summary>
    internal int parentIndex;
    /// <summary>
    /// Index of the thread (0 is typically the main thread, others are job threads).
    /// </summary>
    internal ushort threadIndex;
    /// <summary>
    /// Hierarchical depth in the callstack where the event occurred.
    /// </summary>
    internal ushort level;
    /// <summary>
    /// Category identifier used for determining event color and grouping.
    /// </summary>
    internal ushort categoryId;
    /// <summary>
    /// Alignment padding for struct memory layout.
    /// </summary>
    internal ushort pad0;
}

/// <summary>
/// Info for the frame such as start time, index, etc.
/// </summary>
internal struct FrameInfo
{
    /// <summary>
    /// Start time of the frame.
    /// </summary>
    internal double startTime;
    /// <summary>
    /// How long the frame is.
    /// </summary>
    internal float frameTime;
    /// <summary>
    /// Index of the frame.
    /// </summary>
    internal int frameIndex;
}

/// <summary>
/// Used to determine how a ThreadGroup or individual thread is shown.
/// </summary>
internal enum ThreadVisibility
{
    /// <summary>
    /// No visibility at all.
    /// </summary>
    Hidden,
    /// <summary>
    /// Preview with one/small line of rendering per thread.
    /// </summary>
    Preview,
    /// <summary>
    /// Full visibility of all the threads.
    /// </summary>
    Visible,
}

/// <summary>
/// Groups threads for organizational and display purposes.
/// </summary>
internal struct ThreadGroup
{
    /// <summary>
    /// Name of the thread group.
    /// </summary>
    internal FixedString128Bytes name;
    /// <summary>
    /// Offset into Threads array.
    /// </summary>
    internal int arrayIndex;
    /// <summary>
    /// End of the range.
    /// </summary>
    internal int arrayEnd;
    /// <summary>
    /// Hash of all the names in the group.
    /// </summary>
    internal int groupNamesHash;
}

/// <summary>
/// Information about individual threads active in the frame.
/// </summary>
internal struct ThreadInfo
{
    /// <summary>
    /// Name of the thread.
    /// </summary>
    internal FixedString128Bytes name;
    /// <summary>
    /// Persistent thread-id.
    /// </summary>
    internal ulong threadId;
    /// <summary>
    /// Max levels of events.
    /// </summary>
    internal int maxDepth;
    /// <summary>
    /// Index into the event stream.
    /// </summary>
    internal int eventStart;
    /// <summary>
    /// End of the event stream.
    /// </summary>
    internal int eventEnd;
}

/// <summary>
/// Holds info on how a specific thread should be displayed in the ThreadView.
/// </summary>
internal struct ThreadPosition
{
    /// <summary>
    /// How the thread is shown.
    /// </summary>
    internal ThreadVisibility visibility;
    /// <summary>
    /// How many levels of samples are being shown.
    /// </summary>
    internal int depth;
    /// <summary>
    /// Offset of the thread being displayed.
    /// </summary>
    internal float offset;
    /// <summary>
    /// Whether this thread is folded/collapsed.
    /// </summary>
    internal bool isFolded;
    /// <summary>
    /// Animation progress: 0.0 = fully folded, 1.0 = fully expanded.
    /// </summary>
    internal float animationProgress;
    /// <summary>
    /// Height multiplier for folded group preview rendering (0.0-1.0).
    /// </summary>
    internal float previewHeight;
    /// <summary>
    /// Animated visible depth - smoothly transitions towards the actual visible depth.
    /// </summary>
    internal float visibleDepth;
}

/// <summary>
/// This is used to track JobHandle and which event it belongs to in the FrameData.events array.
/// </summary>
internal struct JobHandleEventIndex
{
    /// <summary>
    /// JobHandle.
    /// </summary>
    internal InternalJobHandle handle;
    /// <summary>
    /// Event index into the FrameData.events array.
    /// </summary>
    internal int eventIndex;
}

/// <summary>
/// Caches all data for a frame including all threads, jobs, events, and their relationships.
/// This structure contains all the data needed to render and analyze a jobs profiler frame.
/// </summary>
internal struct FrameData
{
    /// <summary>Bit shift value for encoding string storage type in the stringIndex hash map</summary>
    internal const int k_StringIndexShift = 20;
    /// <summary>Bit flag to indicate a string is stored in the strings512 array</summary>
    internal const int k_StringIndexSet = 1 << k_StringIndexShift;
    /// <summary>Bit mask to extract the actual index within a string array</summary>
    internal const int k_StringIndexMask = k_StringIndexSet - 1;

    /// <summary>Basic timing and identification information for the frame</summary>
    internal NativeArray<FrameInfo> info;
    /// <summary>Collection of all profiling events that occurred during the frame</summary>
    internal NativeList<ProfilingEvent> events;
    /// <summary>Color mapping for different event categories</summary>
    internal NativeList<Color32> catColors;
    /// <summary>Color-blind safe color mapping for different event categories</summary>
    internal NativeList<Color32> catColorsColorBlind;
    /// <summary>Thread groupings for organizational and display purposes</summary>
    internal NativeList<ThreadGroup> threadGroups;
    /// <summary>Information about individual threads active in the frame</summary>
    internal NativeArray<ThreadInfo> threads;
    /// <summary>Lookup table to find thread info by thread ID</summary>
    internal NativeHashMap<ulong, int> threadsLookup;
    /// <summary>
    /// Maps marker IDs to string indices. The high bit (k_StringIndexSet) indicates
    /// whether the string is stored in strings128 or strings512.
    /// </summary>
    internal NativeHashMap<int, int> stringIndex;
    /// <summary>Storage for strings up to 128 bytes in length</summary>
    internal NativeList<FixedString128Bytes> strings128;
    /// <summary>Storage for longer strings up to 512 bytes in length</summary>
    internal NativeList<FixedString512Bytes> strings512;
    /// <summary>Links between job handles and their corresponding events</summary>
    internal NativeList<JobHandleEventIndex> jobEventIndexList;
    /// <summary>Maps job handle values to event indices for quick job event lookup</summary>
    internal NativeHashMap<ulong, int> handleIndexLookup;
    /// <summary>Maps event indices to job handle values</summary>
    internal NativeHashMap<int, ulong> eventHandleLookup;
    /// <summary>Maps job handle to schedule index for O(1) lookup</summary>
    internal NativeHashMap<ulong, int> handleToScheduleIndex;
    /// <summary>Detailed information about jobs scheduled in this frame</summary>
    internal NativeList<ScheduledJobInfo> scheduledJobs;
    /// <summary>Collection of all job dependencies referenced in the frame. ScheduledJobInfo has an index into this table</summary>
    internal NativeList<InternalJobHandle> dependencyTable;
    /// <summary>Tracks job state transitions (scheduled, waited on, completed)</summary>
    internal NativeList<JobFlow> jobFlows;
}

/// <summary>
/// Used for all RawDataViewFrames that we have to cache as we can't pass it to the job
/// without using the GCHandle as they are managed types
/// </summary>
struct RawDataViewHandles
{
    // GCHandles for RawDataView
    internal NativeList<GCHandle> handles;
    /// Data generated by the job
    internal FrameData frameData;
    /// Handle for the job in-flight
    internal JobHandle jobHandle;
    // Handle for the caching job
    internal int frameIndex;
}

/// <summary>
/// State when gathering data
/// </summary>
enum State
{
    Default,
    BeginJob,
    EndJob,
}

/// <summary>
/// JobHandle and the starting time of it
/// </summary>
struct HandleTime
{
    internal InternalJobHandle handle;
    internal float time;
}

/// <summary>
/// Processes RawFrameDataView objects from the Unity Profiler API and extracts job and profiling
/// information into the optimized FrameData structure.
/// </summary>
/// <remarks>
/// This job handles parsing the profiler data hierarchy, extracting event information, and
/// processing metadata about jobs.
/// </remarks>
struct CacheFrameJob : IJob
{
    /// <summary> This is the ssize of the Jobs Profiler metadata header </summary>
    const int k_JobsHeaderSize = 4;

    /// <summary> Raw profiler frames from the ProfilerDriver </summary>
    [ReadOnly]
    internal NativeList<GCHandle> m_rawProfilerFrames;

    /// <summary> The current frame index we are processing </summary>
    [ReadOnly]
    internal int m_frameIndex;

    /// <summary> </summary>Output data
    internal FrameData m_output;

    /// <summary> Used for tracking how we are reading data from the stream </summary>
    internal State m_state;

    /// <summary>
    /// Used when fetching different event such as BeginJob to be assigned to next coming event.
    /// This has to be a stack because we can have jobs that schedules jobs.
    /// </summary>
    internal class StringIndex : IComparable<StringIndex>
    {
        internal string name;
        internal string group;
        internal int index;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        int IComparable<StringIndex>.CompareTo(StringIndex other)
        {
            return EditorUtility.NaturalCompare(this.name, other.name);
        }
    }

    /// <summary>
    /// Adds a string to the appropriate string storage collection based on its length.
    /// Uses strings128 for shorter strings and strings512 for longer strings, with an index bit
    /// to differentiate between the two collections.
    /// <remarks>
    /// Doing it this wait instead of using the GetMarkers() API is much faster
    /// </remarks>
    /// </summary>
    /// <param name="output">The FrameData structure to add the string to</param>
    /// <param name="name">The string to add</param>
    /// <param name="markerId">Marker ID associated with this string</param>
    void AddString(ref FrameData output, string name, int markerId)
    {
        // FixedString capacities are UTF-8 byte counts, so the encoded length decides which storage fits.
        // Testing name.Length (UTF-16 code units) lets a non-ASCII name past the check, and the implicit
        // string conversion then throws. CopyFromTruncated does the fit test and the conversion in one step,
        // truncating on a rune boundary rather than throwing when the name is too long for either storage.
        var name128 = new FixedString128Bytes();

        if (name128.CopyFromTruncated(name) == CopyError.None)
        {
            int index = output.strings128.Length;
            output.strings128.Add(name128);
            output.stringIndex.TryAdd(markerId, index);
        }
        else
        {
            var name512 = new FixedString512Bytes();
            name512.CopyFromTruncated(name);

            int index = output.strings512.Length;
            output.strings512.Add(name512);
            output.stringIndex.TryAdd(markerId, index | FrameData.k_StringIndexSet);
        }
    }

    /// <summary>
    /// Processes job schedule metadata to extract information about job dependencies and scheduling details.
    /// </summary>
    /// <param name="output">The FrameData structure to add the metadata to</param>
    /// <param name="inputFrameData">Source profiler data</param>
    /// <param name="metadata">Raw metadata about the scheduled job</param>
    /// <remarks>
    /// This method parses job scheduling metadata to extract information about job handles,
    /// dependencies, count, and grain size, storing this information in the FrameData structure
    /// for further analysis and visualization.
    /// </remarks>
    void AddScheduleJobMetadata(ref FrameData output, in RawFrameDataView inputFrameData, in Span<uint> metadata)
    {
        int eventIndex = output.events.Length;
        InternalJobHandle handle = new InternalJobHandle { index = metadata[0], generation = metadata[1] };

        int mi = 2;


        uint count = metadata[mi++];
        uint grainSize = metadata[mi++];
        uint dependencyCount = metadata[mi++];
        mi++; // skip padding

        int depCount = 0;
        int tableStartIndex = output.dependencyTable.Length;

        for (int i = 0; i < dependencyCount * 2; i += 2)
        {
            var depHandle = new InternalJobHandle { index = metadata[mi + i + 0], generation = metadata[mi + i + 1] };

            if (!depHandle.IsValid())
                continue;


            depCount++;
            output.dependencyTable.Add(depHandle);
        }

        output.scheduledJobs.Add(new ScheduledJobInfo
        {
            handle = handle,
            count = count,
            grainSize = grainSize,
            eventIndex = eventIndex,
            dependencyCount = depCount,
            dependencyTableIndex = tableStartIndex,
        });

        output.jobFlows.Add(new JobFlow
        {
            handle = handle,
            eventIndex = eventIndex,
            state = JobFlowState.BeginSchedule,
        });
    }

    InternalJobHandle GetJobHandle(in Span<uint> metadata)
    {
        return new InternalJobHandle { index = metadata[0], generation = metadata[1] };
    }

    /// <summary>
    /// Records job state transitions (begin schedule, completed, waited on) in the job flow tracking system.
    /// </summary>
    /// <param name="output">The FrameData structure to add the flow metadata to</param>
    /// <param name="metadata">Raw metadata about the job state</param>
    /// <param name="state">The job state transition being recorded</param>
    void AddJobFlowMetadata(ref FrameData output, in Span<uint> metadata, JobFlowState state)
    {
        int eventIndex = output.events.Length;
        InternalJobHandle handle = GetJobHandle(metadata);


        output.jobFlows.Add(new JobFlow
        {
            handle = handle,
            eventIndex = eventIndex,
            state = state,
        });
    }

    /// <summary>
    /// Processes metadata for a profiling event and updates the job tracking state.
    /// </summary>
    /// <param name="output">The output FrameData structure</param>
    /// <param name="handleStack">Stack of active job handles being processed</param>
    /// <param name="inputFrameData">Source profiler data</param>
    /// <param name="metadata">Metadata for the current event</param>
    /// <param name="startTime">Start time of the related event</param>
    /// <returns>Updated state for the job processing</returns>
    /// <remarks>
    /// This method is the core of job metadata processing. It analyzes the metadata type
    /// and performs the appropriate actions:
    /// - For job scheduling events, it records scheduling info
    /// - For job begin/end events, it tracks job execution timing
    /// - For job completion events, it records job flow state
    /// </remarks>
    State AddMetadataToCache(ref FrameData output, ref Stack<HandleTime> handleStack, in RawFrameDataView inputFrameData, in Span<uint> metadata, float startTime)
    {
        uint metadataType = metadata[2];

        State state = State.Default;

        switch ((MetadataType)metadataType)
        {
            case MetadataType.AllocateJob:
            case MetadataType.CombineDependencies:
            case MetadataType.ScheduleJob:
            {
                AddScheduleJobMetadata(ref output, inputFrameData, metadata.Slice(k_JobsHeaderSize));
                break;
            }

            case MetadataType.WaitOnJob:
            {
                AddJobFlowMetadata(ref output, metadata.Slice(k_JobsHeaderSize), JobFlowState.WaitedOn);
                break;
            }

            case MetadataType.WaitForCompleted:
            {
                AddJobFlowMetadata(ref output, metadata.Slice(k_JobsHeaderSize), JobFlowState.CompletedNoWait);
                break;
            }

            // Marker for when a job begins (not an actual timer)
            case MetadataType.BeginPostExecute:
            case MetadataType.BeginJob:
            {
                InternalJobHandle handle = GetJobHandle(metadata.Slice(k_JobsHeaderSize));
                handleStack.Push(new HandleTime
                {
                    handle = handle,
                    time = startTime,
                });

                state = State.BeginJob;
                break;
            }

            case MetadataType.EndPostExecute:
            case MetadataType.EndJob:
            {
                InternalJobHandle handle = GetJobHandle(metadata.Slice(k_JobsHeaderSize));
                state = State.EndJob;
                break;
            }

        default:
            {
                InternalJobHandle handle = GetJobHandle(metadata.Slice(k_JobsHeaderSize));
                break;
            }
        }

        return state;
    }

    bool AddCacheSample(ref FrameData output, ref Stack<HandleTime> handleStack, in RawFrameDataView inputFrameData, ushort threadIndex, int index, int level, int parent)
    {
        var markerId = inputFrameData.GetSampleMarkerId(index);
        var name = inputFrameData.GetSampleName(index);

        // Skip the "Main Thread" marker that Unity adds at the root level
        if (level == 0 && name == "Main Thread")
            return false;

        if (!output.stringIndex.ContainsKey(markerId))
        {
            // TODO: Remove this hack. It requires a change to the profiler backend to fix.
            if (name == "EndJob")
                name = "NativeJob";

            if (name == null)
                return false;

            AddString(ref output, name, markerId);
        }
        else if (markerId == 0)
        {
            // Workaround for older captures where the same name for 0 can be null
            if (name == null)
                return false;
        }

        var startTime = (float)(inputFrameData.GetSampleStartTimeMs(index) - inputFrameData.frameStartTimeMs);
        var time = inputFrameData.GetSampleTimeMs(index);
        var catId = inputFrameData.GetSampleCategoryIndex(index);
        var metadataCount = inputFrameData.GetSampleMetadataCount(index);
        var metadataState = State.Default;


        if (metadataCount == 1)
        {
            var metadata = inputFrameData.GetSampleMetadataAsSpan<uint>(index, 0);

            if (metadata.Length > k_JobsHeaderSize)
            {
                uint id = metadata[0];
                uint version = metadata[1];

                // Validate magic header "JOBS" and version(s) that is supported
                if (id == 0x4A4F4253 && version == 1)
                {
                    metadataState = AddMetadataToCache(ref output, ref handleStack, inputFrameData, metadata, startTime);
                }
            }
        }

        // If new state is begin job we shouldn't add any data and the next events should be looked if data should be added or not
        if (metadataState == State.BeginJob)
        {
            m_state = metadataState;
            return true;
        }

        // If previous state is beginjob and we don't have any metadata for next event we should assign
        // the job handle info to this event and reset the state
        if (m_state == State.BeginJob && metadataState == State.Default)
        {
            HandleTime handleTime = handleStack.Pop();
            var currentIndex = output.events.Length;


            /// List of all jobHandle/EventIndex pairs for this frame
            output.jobEventIndexList.Add(new JobHandleEventIndex
            {
                handle = handleTime.handle,
                eventIndex = currentIndex,
            });

            output.events.AddNoResize(new ProfilingEvent(startTime, time, threadIndex, (ushort)level, catId, markerId, parent));

            m_state = State.Default;
        }
        // If have begin/end we should generate a new state here
        else if (m_state == State.BeginJob && metadataState == State.EndJob)
        {
            HandleTime handleTime = handleStack.Pop();
            var currentIndex = output.events.Length;


            /// List of all jobHandle/EventIndex pairs for this frame
            output.jobEventIndexList.Add(new JobHandleEventIndex
            {
                handle = handleTime.handle,
                eventIndex = currentIndex,
            });

            // TODO: We should validate matching being job/end job pairs
            output.events.AddNoResize(new ProfilingEvent(handleTime.time, startTime - handleTime.time, threadIndex, (ushort)level, catId, markerId, parent));

            m_state = State.Default;
        }
        else
        {
            if (metadataState != State.EndJob)
            {
                output.events.AddNoResize(new ProfilingEvent(startTime, time, threadIndex, (ushort)level, catId, markerId, parent));
            }
        }

        return true;
    }

    /// <summary>
    /// Traverses all samples in the input frame data recursively, generating cache data for each sample.
    /// </summary>
    int GenerateCacheDataRecursive(ref FrameData outputData, ref Stack<HandleTime> handleStack, in RawFrameDataView inputFrameData, ushort threadIndex, int index, int level)
    {
        int parent = outputData.events.Length;

        // This check is a hack because there are cases when the first sample doesn't have any name and that
        // would mean that we get an extra empty "level" with not data as output. With this check we adjust
        // so the next level (in the loop below) will start at the "correct" level.
        if (!AddCacheSample(ref outputData, ref handleStack, inputFrameData, threadIndex, index, level, 0) && index == 0)
            level = -1;

        var count = (ushort)inputFrameData.GetSampleChildrenCount(index++);

        for (int i = 0; i < count; ++i)
        {
            if (inputFrameData.GetSampleChildrenCount(index) > 0)
            {
                index = GenerateCacheDataRecursive(ref outputData, ref handleStack, inputFrameData, threadIndex, index, level + 1);
            }
            else
            {
                AddCacheSample(ref outputData, ref handleStack, inputFrameData, threadIndex, index, level + 1, parent);
                index++;
            }
        }

        return index;
    }

    /// <summary>
    /// Main execution method for the caching job that processes raw profiler frame data into an optimized format.
    /// </summary>
    /// <remarks>
    /// This method performs several key operations:
    /// 1. Initializes the caching state and data structures
    /// 2. Processes profiler categories and their colors
    /// 3. Organizes threads into logical groups and ensures consistent ordering
    /// 4. Traverses the profiling hierarchy to extract events, timing, and job metadata
    /// 5. Creates connections between jobs, events, and their dependencies
    ///
    /// The method uses a recursive approach to traverse the profiler sample hierarchy while
    /// maintaining the parent-child relationships between events and processing job metadata.
    /// This processed data is then made available to the job profiler for efficient rendering
    /// and analysis without needing to repeatedly parse the raw profiler data.
    /// </remarks>
    public void Execute()
    {
        m_state = State.Default;
        Stack<HandleTime> handleStack = new Stack<HandleTime>(32);


        float frameTime = 0.0f;
        var output = m_output;

        // Fetch data from the main-thread first
        // TODO: We may need to revise this in case it differs between threads
        var mainThread = (RawFrameDataView)m_rawProfilerFrames[0].Target;

        List<ProfilerCategoryInfo> allCategories = new List<ProfilerCategoryInfo>(128);
        mainThread.GetAllCategories(allCategories);

        // Both lists are indexed by category id, and ids aren't guaranteed to be dense, so they have to be
        // sized from the highest id rather than from the category count.
        int colorCount = 0;
        for (int catId = 0, count = allCategories.Count; catId < count; ++catId)
            colorCount = math.max(colorCount, allCategories[catId].id + 1);

        output.catColors.Resize(colorCount, NativeArrayOptions.ClearMemory);
        output.catColorsColorBlind.Resize(colorCount, NativeArrayOptions.ClearMemory);

        for (int catId = 0, count = allCategories.Count; catId < count; ++catId)
        {
            int id = allCategories[catId].id;
            if (id >= output.catColors.Length)
                continue;

            output.catColors[id] = allCategories[catId].color;

            // Map each category's color to its color-blind safe equivalent
            Color defaultColor = allCategories[catId].color;
            output.catColorsColorBlind[id] = JobsProfilerSettings.GetColorBlindSafeColor(defaultColor) ?? defaultColor;
        }

        int threadCount = m_rawProfilerFrames.Length;

        // As the threads can come in any order we will sort them there so they
        // are consistent for each frame
        List<StringIndex> groupThreadList = new List<StringIndex>(threadCount);
        StringBuilder sb = new StringBuilder(512);

        for (int i = 0; i < threadCount; ++i)
        {
            var frameData = (RawFrameDataView)m_rawProfilerFrames[i].Target;
            var groupName = frameData.threadGroupName;

            // Main Thread and Render Thread has no groups so we put them into own groups instead
            if (groupName == "")
            {
                if (frameData.threadName == "Main Thread")
                    groupName = "Main Thread";
                else if (frameData.threadName == "Render Thread")
                    groupName = "Render Thread";
            }

            sb.AppendFormat("{0}:{1}", groupName, frameData.threadName);
            groupThreadList.Add(new StringIndex
            {
                name = sb.ToString(),
                group = groupName,
                index = i,
            });

            sb.Clear();
        }

        groupThreadList.Sort();

        string currentGroupName = groupThreadList[0].group;
        int startThreadIndex = 0;
        int currentThreadIndex = 0;

        // We use this to hash the group names so we can compare them later when we construct the offsets for the threads.
        int groupNamesHash = 0;

        for (int i = 0; i < groupThreadList.Count; ++i)
        {
            string groupName = groupThreadList[i].group;
            string threadName = groupThreadList[i].name;
            int orgThreadIndex = groupThreadList[i].index;

            if (groupName != currentGroupName)
            {
                output.threadGroups.Add(new ThreadGroup
                {
                    name = currentGroupName,
                    arrayIndex = startThreadIndex,
                    arrayEnd = currentThreadIndex,
                    groupNamesHash = groupNamesHash,
                });

                groupNamesHash = 0;
                startThreadIndex = currentThreadIndex;
                currentGroupName = groupName;
            }

            groupNamesHash = HashCode.Combine(groupNamesHash, threadName.GetHashCode());

            var frameData = (RawFrameDataView)m_rawProfilerFrames[orgThreadIndex].Target;

            frameTime = math.max(frameData.frameTimeMs, frameTime);

            int eventsStart = output.events.Length;

            // The state machine and the handle stack are per-thread. A thread whose last sample is a BeginJob
            // metadata marker (a job scheduled right at the frame boundary, with its body landing in the next
            // frame) would otherwise carry State.BeginJob and a stale handle into the next thread, which then
            // associates its first ordinary sample with the wrong job.
            m_state = State.Default;
            handleStack.Clear();

            GenerateCacheDataRecursive(ref output, ref handleStack, frameData, (ushort)(i), 0, 0);

            int depth = frameData.maxDepth;

            // If first sample is null we need to adjust the depth
            if (frameData.sampleCount > 0 && frameData.GetSampleName(0) == null)
                depth = math.max(frameData.maxDepth - 1, 1);

            ulong threadId = frameData.threadId;

            // ThreadId can be zero in older profile captures so we use threadIndex here instead.
            // This isn't ideal, but better than nothing.
            if (threadId == 0)
                threadId = (ulong)frameData.threadIndex;

            output.threads[i] = new ThreadInfo
            {
                name = new FixedString128Bytes(frameData.threadName),
                threadId = threadId,
                maxDepth = depth,
                eventStart = eventsStart,
                eventEnd = output.events.Length,
            };

            output.threadsLookup.TryAdd(threadId, i);

            currentThreadIndex++;
        }

        output.threadGroups.Add(new ThreadGroup
        {
            name = currentGroupName,
            arrayIndex = startThreadIndex,
            arrayEnd = currentThreadIndex,
            groupNamesHash = groupNamesHash,
        });


        output.info[0] = new FrameInfo
        {
            startTime = mainThread.frameStartTimeMs,
            frameTime = frameTime,
            frameIndex = m_frameIndex,
        };

    }
}

/// <summary>
/// Reorganizes profiling events to be grouped by hierarchy level rather than by execution order.
/// </summary>
/// <param name="allEvents">The complete list of profiling events</param>
/// <param name="threads">Information about threads in the frame</param>
/// <param name="scheduledJobs">List of scheduled jobs to be updated</param>
/// <param name="jobFlows">List of job flows to be updated</param>
/// <param name="jobEventIndexList">List mapping job handles to events</param>
/// <remarks>
/// This algorithm reorganizes events to make them contiguous by hierarchical level.
/// The original data layout typical of profiler samples is like:
///
///  0 4 5 (level 0)
///   1 3  (level 1)
///    2   (level 2)
///
/// The algorithm reorganizes this to:
///
///  0 1 2  (level 0)
///   3 4   (level 1)
///    5    (level 2)
///
/// This allows for:
/// 1. More efficient rendering by processing events at the same level together
/// 2. Easier culling of events at the same level
/// 3. Better event merging for small adjacent events
///
/// The method works in several phases:
/// 1. Count events at each level across all threads
/// 2. Calculate new position offsets for each level
/// 3. Build an indirection table mapping old indices to new ones
/// 4. Update all references to event indices in related data structures
/// 5. Copy events to their new positions using the indirection table
/// </remarks>
[BurstCompile(DisableSafetyChecks = true)]
internal struct CacheFixupJob : IJob
{
    internal NativeList<ProfilingEvent> m_allEvents;
    /// <summary> Info about the jobs that are being scheduled this frame with handle, dependecies, etc </summary>
    internal NativeList<ScheduledJobInfo> m_scheduledJobs;
    /// <summary> Jobs that are being started, waited on, etc </summary>
    internal NativeList<JobFlow> m_jobFlows;
    /// <summary> List of all jobHandle/EventIndex pairs for this frame</summary>
    internal NativeList<JobHandleEventIndex> m_jobEventIndexList;
    /// <summary> Lookup from JobHandle to event id </summary>
    internal NativeHashMap<ulong, int> m_handleIndexLookup;
    /// <summary> Lookup from eventId to JobHandle </summary>
    internal NativeHashMap<int, ulong> m_eventHandleLookup;
    /// <summary> Maps job handle to schedule index for O(1) lookup </summary>
    internal NativeHashMap<ulong, int> m_handleToScheduleIndex;

    [ReadOnly]
    internal NativeArray<ThreadInfo> m_threads;

    /// <summary>
    /// Reorders profiling events so that events at the same hierarchy level are stored contiguously,
    /// rather than in the original execution order. This enables more efficient rendering, culling,
    /// and merging of events at the same level.
    /// </summary>
    /// <param name="allEvents">The list of all profiling events to be reordered.</param>
    /// <param name="threads">The array of thread information for the frame.</param>
    /// <param name="scheduledJobs">The list of scheduled jobs to update with new event indices.</param>
    /// <param name="jobFlows">The list of job flow records to update with new event indices.</param>
    /// <param name="jobEventIndexList">The list mapping job handles to event indices, to update with new indices.</param>
    static internal void OrderLevels(
        NativeList<ProfilingEvent> allEvents,
        NativeArray<ThreadInfo> threads,
        NativeList<ScheduledJobInfo> scheduledJobs,
        NativeList<JobFlow> jobFlows,
        NativeList<JobHandleEventIndex> jobEventIndexList)
    {
        // Create an array that support 16k callstack depth
        const int kCallDepth = 16 * 1024;

        var levelCounts = new NativeArray<int>(kCallDepth, Allocator.Temp);
        var levelOffsets = new NativeArray<int>(kCallDepth, Allocator.Temp);
        var writeOffsets = new NativeArray<int>(kCallDepth, Allocator.Temp);
        var indirectionTable = new NativeArray<int>(allEvents.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        int levels = 0;

        foreach (var thread in threads)
        {
            // Count the number events per level
            for (int eventIndex = thread.eventStart; eventIndex < thread.eventEnd; ++eventIndex)
            {
                ProfilingEvent profEvent = allEvents[eventIndex];
                levelCounts[profEvent.level] = levelCounts[profEvent.level] + 1;
            }

            int offset = 0;

            // Calculate offsets based on the above data where shuffled data needs to be placed
            for (int i = 0; i < kCallDepth; ++i)
            {
                if (levelCounts[i] == 0)
                    break;

                writeOffsets[i] = 0;
                levelOffsets[i] = thread.eventStart + offset;
                offset += levelCounts[i];
                levels++;
            }

            // Build the indirection table. That is required as we need to patch up some other offsets also
            for (int eventIndex = thread.eventStart; eventIndex < thread.eventEnd; ++eventIndex)
            {
                ProfilingEvent profEvent = allEvents[eventIndex];
                int writeOffset = writeOffsets[profEvent.level];
                int eventOffset = levelOffsets[profEvent.level] + writeOffset;
                indirectionTable[eventIndex] = eventOffset;
                writeOffsets[profEvent.level] = writeOffset + 1;
            }

            for (int i = 0; i < levels; ++i)
            {
                levelCounts[i] = 0;
                writeOffsets[i] = 0;
            }
        }

        // Go over all the data and change the eventIndex to the new one
        for (int i = 0; i < scheduledJobs.Length; ++i)
        {
            ScheduledJobInfo t = scheduledJobs[i];
            t.eventIndex = indirectionTable[t.eventIndex];
            scheduledJobs[i] = t;
        }

        for (int i = 0; i < jobFlows.Length; ++i)
        {
            JobFlow t = jobFlows[i];
            t.eventIndex = indirectionTable[t.eventIndex];
            jobFlows[i] = t;
        }

        for (int i = 0; i < jobEventIndexList.Length; ++i)
        {
            JobHandleEventIndex t = jobEventIndexList[i];
            t.eventIndex = indirectionTable[t.eventIndex];
            jobEventIndexList[i] = t;
        }

        // Take a copy of all events as we will read from this when moving the events around
        var tempEvents = new NativeArray<ProfilingEvent>(allEvents.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        tempEvents.CopyFrom(allEvents.AsArray());

        // And finally copy all the new data into the new locations
        for (int i = 0; i < allEvents.Length; ++i)
        {
            ProfilingEvent e = tempEvents[i];
            e.parentIndex = indirectionTable[e.parentIndex];
            int newTarget = indirectionTable[i];
            allEvents[newTarget] = e;
        }

        levelCounts.Dispose();
        levelOffsets.Dispose();
        writeOffsets.Dispose();
        tempEvents.Dispose();
        indirectionTable.Dispose();
    }

    public void Execute()
    {
        OrderLevels(m_allEvents, m_threads, m_scheduledJobs, m_jobFlows, m_jobEventIndexList);

        // Build handle to event index lookups (moved from CacheFrameJob, gets Bursted here)
        foreach (var info in m_jobEventIndexList)
        {
            ulong jobHandleId = info.handle.ToUlong();
            m_eventHandleLookup.TryAdd(info.eventIndex, jobHandleId);
            m_handleIndexLookup.TryAdd(jobHandleId, info.eventIndex);
        }

        // Build handle to schedule index lookup for O(1) FindJobScheduleIndex
        for (int i = 0; i < m_scheduledJobs.Length; i++)
        {
            ulong handleId = m_scheduledJobs[i].handle.ToUlong();
            m_handleToScheduleIndex.TryAdd(handleId, i);
        }
    }
}

/// <summary>
/// FrameCache manages the caching of profiler frame data for efficient access and rendering.
/// </summary>
/// <remarks>
/// The caching strategy works in two parts:
/// 1. Batch processing of frames using background jobs up to the system's job thread capacity
/// 2. Priority processing for frames specifically requested by user interaction
///
/// The class manages the lifecycle of cached frames:
/// - Requesting frames to be cached
/// - Processing cached frame data when jobs complete
/// - Maintaining a record of which frames are cached or in the process of being cached
/// - Providing access to cached frame data for display and analysis
///
/// This approach allows us to not wait on the main thread while the caching is being done.
/// </remarks>
internal class FrameCache : IDisposable
{
    /// <summary> All the frames that has been cached </summary>
    Dictionary<int, FrameData> m_frames;
    /// <summary> Ranges of frames that needs to be cached </summary>
    NativeList<Range> m_ranges;
    /// <summary> The GCHandles for RawDataView that are inflight for caching </summary>
    NativeArray<RawDataViewHandles> m_inflightData;
    /// <summary> Range of frames that has been cached </summary>
    NativeList<Range> m_cachedRanges;
    /// <summary> Range that is currently being cached </summary>
    Range m_activeRange;
    /// <summary> Range that is being processed </summary>
    Range m_updatingRange;
    /// <summary> Number of items in flight </summary>
    int m_inflightCount;
    /// <summary> Used for caching frames ranges </summary>
    struct Range
    {
        internal int start;
        internal int end;
    }

    enum State
    {
        Idle,
        Caching,
    }

    internal enum WaitOnJobs
    {
        Yes,
        No,
    }

    State m_state = State.Idle;

    /// <summary>
    /// Retrieves cached frame data for a specific frame if available.
    /// </summary>
    /// <param name="frameIndex">The index of the frame to retrieve</param>
    /// <param name="output">When successful, contains the cached frame data</param>
    /// <returns>True if the frame was found in the cache, false otherwise</returns>
    internal bool GetFrame(int frameIndex, out FrameData output)
    {
        if (!m_frames.TryGetValue(frameIndex, out output))
            return false;

        return true;
    }

    internal int GetNumberOfFrames()
    {
        return m_frames.Count;
    }

    internal Dictionary<int, FrameData> Frames
    {
        get { return m_frames; }
    }

    /// <summary>
    /// Retrieves the string associated with a marker ID for a specific frame.
    /// </summary>
    /// <param name="index">Frame index</param>
    /// <param name="markerId">The marker ID to look up</param>
    /// <returns>The string associated with the marker ID, or "Unknown" if not found</returns>
    internal string GetStringForFrame(int index, int markerId)
    {
        int stringIndex = 0;
        FrameData frame;

        if (!GetFrame(index, out frame))
            return "Unknown";

        if (!frame.stringIndex.TryGetValue(markerId, out stringIndex))
            return "Unknown";

        // In order to not use string directly in jobs we split the storage between string128/512 and have 1
        // bit that indicaces which one we need to fetch from. If the bit is 0 we grab from string128 otherwise string512
        if ((stringIndex >> FrameData.k_StringIndexShift) == 1)
            return frame.strings512[stringIndex & FrameData.k_StringIndexMask].ToString();
        else
            return frame.strings128[stringIndex].ToString();
    }

    /// <summary>
    /// Loop over all frames and find the string associated with a marker ID.
    /// </summary>
    /// <param name="markerId">The marker ID to look up</param>
    /// <returns>The string associated with the marker ID, or "Unknown" if not found</returns>
    internal string FindString(int markerId)
    {
        foreach (var frame in m_frames)
        {
            int stringIndex = 0;

            if (!frame.Value.stringIndex.TryGetValue(markerId, out stringIndex))
                continue;

            // In order to not use string directly in jobs we split the storage between string128/512 and have 1
            // bit that indicaces which one we need to fetch from. If the bit is 0 we grab from string128 otherwise string512
            if ((stringIndex >> FrameData.k_StringIndexShift) == 1)
                return frame.Value.strings512[stringIndex & FrameData.k_StringIndexMask].ToString();
            else
                return frame.Value.strings128[stringIndex].ToString();
        }

        return "Unknown";
    }

    /// <summary>
    /// Get the job handle for a specific frame and event ID.
    /// </summary>
    /// <param name="frameIndex"></param>
    /// <param name="eventId"></param>
    /// <returns>The JobHandle otherwise it return an empty JobHandle</returns>
    internal InternalJobHandle GetJobHandleForFrameEvent(int frameIndex, int eventId)
    {
        ulong handle;
        FrameData frame;

        if (!GetFrame(frameIndex, out frame))
            return new InternalJobHandle();

        if (frame.eventHandleLookup.TryGetValue(eventId, out handle))
            return new InternalJobHandle(handle);

        return new InternalJobHandle();
    }

    /// <summary>
    /// Gets the string associated with a specific event ID for a given frame.
    /// </summary>
    /// <param name="index">The frame index to query.</param>
    /// <param name="eventId">The event ID within the frame.</param>
    /// <returns>The string associated with the event ID, or "Unknown" if not found.</returns>
    internal string GetEventStringForFrame(int index, int eventId)
    {
        int stringIndex = 0;
        FrameData frame;

        if (!GetFrame(index, out frame))
            return "Unknown";

        int markerId = frame.events[eventId].markerId;

        if (!frame.stringIndex.TryGetValue(markerId, out stringIndex))
            return "Unknown";

        // In order to not use string directly in jobs we split the storage between string128/512 and have 1
        // bit that indicaces which one we need to fetch from. If the bit is 0 we grab from string128 otherwise string512
        if ((stringIndex >> FrameData.k_StringIndexShift) == 1)
            return frame.strings512[stringIndex & FrameData.k_StringIndexMask].ToString();
        else
            return frame.strings128[stringIndex].ToString();
    }

    /// <summary>
    /// Get the time associated with a specific event ID for a given frame.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="eventId"></param>
    /// <returns> Returns the time associated with the event ID, or 0.0f if not found</returns>
    internal float GetTimeForEvent(int index, int eventId)
    {
        FrameData frame;

        if (!GetFrame(index, out frame))
            return 0.0f;

        return frame.events[eventId].time;
    }

    internal FrameCache()
    {
        int maxJobThreadCount = JobsUtility.JobWorkerMaximumCount;
        int maxPerFrameCacheCount = Math.Max(maxJobThreadCount - 2, 1);
        m_frames = new Dictionary<int, FrameData>(10000);
        m_cachedRanges = new NativeList<Range>(16, Allocator.Persistent);
        m_ranges = new NativeList<Range>(16, Allocator.Persistent);
        m_inflightData = new NativeArray<RawDataViewHandles>(maxPerFrameCacheCount, Allocator.Persistent);
        m_activeRange = new Range { start = -1, end = -1 };
        m_updatingRange = m_activeRange;
    }

    /// <summary>
    /// Updates the caching system, processing completed jobs and starting new ones.
    /// This should be called regularly from the main thread.
    /// </summary>
    internal void Update()
    {
        UpdateCaching();
        UpdateInflightJobs(WaitOnJobs.No);
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        m_disposed = true;

        // The caching jobs write directly into the FrameData allocations owned by m_inflightData, so every
        // in-flight job has to be completed and reaped before any of it is freed. ClearCache does this as its
        // first step, but drain explicitly here too: it is idempotent, and it keeps the ordering requirement
        // visible at the point where the containers are actually released.
        UpdateInflightJobs(WaitOnJobs.Yes);
        Debug.Assert(m_inflightCount == 0, "FrameCache still has caching jobs in flight while being disposed.");

        ClearCache();

        m_cachedRanges.Dispose();
        m_ranges.Dispose();
        m_inflightData.Dispose();
    }

    bool m_disposed;

    /// Wait and fixing the previous caching jobs
    internal void UpdateInflightJobs(WaitOnJobs waitOnJobs)
    {
        int count = m_inflightCount;

        for (int i = 0; i < count;)
        {
            var data = m_inflightData[i];
            var jobHandle = data.jobHandle;

            if ((jobHandle.IsCompleted || waitOnJobs == WaitOnJobs.Yes) && (data.frameIndex != 0))
            {
                jobHandle.Complete();

                FrameData frameData = data.frameData;
                m_frames.Add(frameData.info[0].frameIndex, frameData);

                ReleaseFrameDataHandles(data.handles);


                // swap remove
                m_inflightData[i] = m_inflightData[count - 1];
                count--;
            }
            else
            {
                i++;
            }
        }

        m_inflightCount = count;

        if (count == 0 && m_state == State.Caching)
        {
            m_cachedRanges.Add(m_activeRange);

            // If we have any ranges left we start caching them
            if (m_ranges.Length > 0)
            {
                m_activeRange = m_ranges[0];
                m_updatingRange = m_activeRange;
                m_ranges.RemoveAtSwapBack(0);
                m_state = State.Caching;
            }
            else
            {
                m_state = State.Idle;
            }
        }
    }

    /// <summary>
    /// Disposes the RawFrameDataView each GCHandle points at, frees the handles and releases the list.
    /// </summary>
    /// <remarks>
    /// The GCHandles only keep the views alive for the duration of the caching job. The views themselves are
    /// IDisposable wrappers over native frame data, so they have to be disposed explicitly or the native data
    /// stays alive until the finalizers run.
    /// </remarks>
    static void ReleaseFrameDataHandles(NativeList<GCHandle> handles)
    {
        if (!handles.IsCreated)
            return;

        foreach (var t in handles)
        {
            (t.Target as IDisposable)?.Dispose();
            t.Free();
        }

        handles.Dispose();
    }

    /// <summary>
    /// Calculates a shallow hash of the cache based on the frame indices only
    /// </summary>
    internal Hash128 CalculateShallowHash()
    {
        var hash = new Hash128();

        foreach (var f in m_frames)
            hash.Append(f.Key);

        return hash;
    }

    /// <summary>
    /// Requests a range of frames to be cached.
    /// </summary>
    /// <param name="start">The first frame index to cache</param>
    /// <param name="end">The last frame index to cache (exclusive)</param>
    /// <remarks>
    /// This method starts the caching process for a range of frames. If another
    /// range is already being cached, this range is queued for later processing.
    /// Duplicate range requests are ignored. Caching is always asynchronous; call
    /// <see cref="UpdateInflightJobs"/> with <see cref="WaitOnJobs.Yes"/> to force completion.
    /// </remarks>
    internal void CacheRange(int start, int end)
    {
        // Check that we are not already caching this range or that it is already cached
        if (m_activeRange.start == start && m_activeRange.end == end)
            return;

        foreach (var r in m_cachedRanges)
        {
            if (r.start == start && r.end == end)
                return;
        }

        if (m_state == State.Caching)
        {
            // If we are already caching we just add it to the list of ranges to cache
            m_ranges.Add(new Range { start = start, end = end });
            return;
        }
        else
        {
            // Switch over to caching state
            m_state = State.Caching;
            m_activeRange = new Range { start = start, end = end };
            m_updatingRange = m_activeRange;

        }

        UpdateCaching();
    }

    /// <summary>
    /// Creates and schedules jobs to cache frames within the current range.
    /// </summary>
    /// <remarks>
    /// This method creates caching jobs for frames that haven't been cached yet,
    /// limited by the available job slots. It handles the creation of FrameData
    /// structures and scheduling of both the initial caching job and the follow-up
    /// fixup job that reorganizes the data.
    /// </remarks>
    void UpdateCaching()
    {
        int inflightIndex = m_inflightCount;
        int maxNewJobs = m_inflightData.Length - m_inflightCount;
        int jobsLeft = m_updatingRange.end - m_updatingRange.start;
        int jobCount = Math.Min(jobsLeft, maxNewJobs);
        int start = m_updatingRange.start;



        for (int i = start; i < start + jobCount; ++i)
        {
            // if we already cached this frame we skip it
            if (m_frames.ContainsKey(i))
                continue;

            (int eventsCount, NativeList<GCHandle> threadIndices) = CountEventsInFrame(i);

            // If there are no events in this frame we just skip it. A frame can still have valid thread views
            // without any samples, so the views and their handles have to be released here or they leak.
            if (eventsCount == 0)
            {
                ReleaseFrameDataHandles(threadIndices);
                continue;
            }

            var frameData = new FrameData
            {
                info = new NativeArray<FrameInfo>(1, Allocator.Persistent),
                events = new NativeList<ProfilingEvent>(eventsCount, Allocator.Persistent),
                threadGroups = new NativeList<ThreadGroup>(16, Allocator.Persistent),
                threadsLookup = new NativeHashMap<ulong, int>(200, Allocator.Persistent),
                threads = new NativeArray<ThreadInfo>(threadIndices.Length, Allocator.Persistent),
                catColors = new NativeList<Color32>(32, Allocator.Persistent),
                catColorsColorBlind = new NativeList<Color32>(32, Allocator.Persistent),
                scheduledJobs = new NativeList<ScheduledJobInfo>(128, Allocator.Persistent),
                stringIndex = new NativeHashMap<int, int>(128, Allocator.Persistent),
                strings128 = new NativeList<FixedString128Bytes>(64, Allocator.Persistent),
                strings512 = new NativeList<FixedString512Bytes>(1, Allocator.Persistent),
                dependencyTable = new NativeList<InternalJobHandle>(512, Allocator.Persistent),
                jobEventIndexList = new NativeList<JobHandleEventIndex>(128, Allocator.Persistent),
                handleIndexLookup = new NativeHashMap<ulong, int>(128, Allocator.Persistent),
                eventHandleLookup = new NativeHashMap<int, ulong>(128, Allocator.Persistent),
                handleToScheduleIndex = new NativeHashMap<ulong, int>(128, Allocator.Persistent),
                jobFlows = new NativeList<JobFlow>(8, Allocator.Persistent),
            };

            var cacheJob = new CacheFrameJob
            {
                m_output = frameData,
                m_rawProfilerFrames = threadIndices,
                m_frameIndex = i,
            };

            var cacheFixupJob = new CacheFixupJob
            {
                m_allEvents = frameData.events,
                m_scheduledJobs = frameData.scheduledJobs,
                m_jobFlows = frameData.jobFlows,
                m_jobEventIndexList = frameData.jobEventIndexList,
                m_handleIndexLookup = frameData.handleIndexLookup,
                m_eventHandleLookup = frameData.eventHandleLookup,
                m_handleToScheduleIndex = frameData.handleToScheduleIndex,
                m_threads = frameData.threads,
            };

            JobHandle handle = cacheJob.Schedule();
            JobHandle jobHandle = cacheFixupJob.Schedule(handle);

            m_inflightData[inflightIndex] = new RawDataViewHandles
            {
                handles = threadIndices,
                frameData = frameData,
                jobHandle = jobHandle,
                frameIndex = i + 1,
            };

            inflightIndex++;
        }

        m_updatingRange.start += jobCount;
        m_inflightCount = inflightIndex;
    }

    internal void ClearCache()
    {
        UpdateInflightJobs(WaitOnJobs.Yes);

        foreach (var cache in m_frames)
        {
            cache.Value.info.Dispose();
            cache.Value.threadGroups.Dispose();
            cache.Value.threads.Dispose();
            cache.Value.events.Dispose();
            cache.Value.catColors.Dispose();
            cache.Value.catColorsColorBlind.Dispose();
            cache.Value.stringIndex.Dispose();
            cache.Value.strings128.Dispose();
            cache.Value.strings512.Dispose();
            cache.Value.handleIndexLookup.Dispose();
            cache.Value.eventHandleLookup.Dispose();
            cache.Value.handleToScheduleIndex.Dispose();
            cache.Value.threadsLookup.Dispose();
            cache.Value.jobEventIndexList.Dispose();
            cache.Value.scheduledJobs.Dispose();
            cache.Value.dependencyTable.Dispose();
            cache.Value.jobFlows.Dispose();
        }

        m_frames.Clear();
        m_ranges.Clear();
        m_cachedRanges.Clear();

        m_activeRange = new Range { start = -1, end = -1 };
        m_updatingRange = m_activeRange;
        m_state = State.Idle;
    }

    /// <summary>
    /// Counts the number of profiling samples in a frame and creates GCHandles for
    /// the raw profiler data that can be used safely inside jobs.
    /// </summary>
    /// <param name="frame">Frame index to analyze</param>
    /// <returns>A tuple with the total event count and a list of GCHandles for each thread's data</returns>
    /// <remarks>
    /// This method is necessary because RawFrameDataView objects are managed types
    /// that need to be pinned before they can be accessed from unmanaged code in jobs.
    /// </remarks>
    (int, NativeList<GCHandle>) CountEventsInFrame(int frame)
    {
        int count = 0;

        NativeList<GCHandle> threadIndices = new NativeList<GCHandle>(256, Allocator.Persistent);

        for (int threadIndex = 0; ; ++threadIndex)
        {
            var frameData = ProfilerDriver.GetRawFrameDataView(frame, threadIndex);

            if (frameData == null)
                break;

            if (!frameData.valid)
            {
                frameData.Dispose();
                break;
            }

            // Normal, not Pinned: RawFrameDataView is a non-blittable managed class, which CoreCLR refuses to
            // pin. Nothing reads the pinned address anyway, the job only ever uses GCHandle.Target.
            threadIndices.Add(GCHandle.Alloc(frameData, GCHandleType.Normal));
            count += frameData.sampleCount;
        }

        return (count, threadIndices);
    }
}
