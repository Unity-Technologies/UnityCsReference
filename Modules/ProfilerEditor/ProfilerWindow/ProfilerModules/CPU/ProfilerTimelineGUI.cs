// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [Serializable]
    internal class ProfilerTimelineGUI : ProfilerFrameDataViewBase
    {
        const float k_TextFadeStartWidth = 50.0f;
        const float k_TextFadeOutWidth = 20.0f;
        const float k_LineHeight = 16.0f;
        const float k_DefaultEmptySpaceBelowBars = k_LineHeight * 3f;
        const float k_ExtraHeightPerThread = 4f;
        const float k_FullThreadLineHeight = k_LineHeight + 0.55f;
        const float k_GroupHeight = k_LineHeight + 4f;
        const float k_ThreadMinHeightCollapsed = 2.0f;
        const float k_ThreadSplitterHandleSize = 6f;

        const int k_MaxNeighborFrames = 3;
        const int k_MaxDisplayFrames = 1 + 2 * k_MaxNeighborFrames;

        static readonly float[] k_TickModulos = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 30000, 60000 };
        const string k_TickFormatMilliseconds = "{0}ms";
        const string k_TickFormatSeconds = "{0}s";
        const int k_TickLabelSeparation = 60;

        internal class ThreadInfo : IComparable<ThreadInfo>
        {
            public float height = 0;
            public float linesToDisplay = 2f;
            public ulong threadId;
            public int threadIndex => threadIndices[k_MaxNeighborFrames];
            public int[] threadIndices = new int[k_MaxDisplayFrames];
            public int maxDepth;
            string m_Name;

            public string name
            {
                get => m_Name;
                set
                {
                    m_Name = value; m_Content = null;
                }
            }

            [NonSerialized]
            GUIContent m_Content;

            public ThreadInfo(string name, ulong threadId, int threadIndex, int maxDepth, int linesToDisplay)
            {
                this.m_Name = name;
                this.threadId = threadId;
                for (var i = 0; i < k_MaxDisplayFrames; ++i)
                    threadIndices[i] = FrameDataView.invalidThreadIndex;
                this.threadIndices[k_MaxNeighborFrames] = threadIndex;
                this.linesToDisplay = linesToDisplay;
                this.maxDepth = Mathf.Max(1, maxDepth);
            }

            public void Reset()
            {
                for (var j = 0; j < k_MaxDisplayFrames; ++j)
                    threadIndices[j] = FrameDataView.invalidThreadIndex;
                maxDepth = -1;
                ActiveFlowEvents?.Clear();
            }

            public void Update(string name, int threadIndex, int maxDepth)
            {
                if (this.name != name)
                    this.name = name;
                this.threadIndices[k_MaxNeighborFrames] = threadIndex;
                this.maxDepth = Mathf.Max(1, maxDepth);
            }

            public List<FlowEventData> ActiveFlowEvents { get; private set; } = null;

            public void AddFlowEvent(FlowEventData d)
            {
                // Most threads don't have flow events - there are no reasons to allocate memory for those.
                if (ActiveFlowEvents == null)
                    ActiveFlowEvents = new List<FlowEventData>();
                ActiveFlowEvents.Add(d);
            }

            public GUIContent DisplayName(bool indent)
            {
                if (m_Content == null)
                    m_Content = CreateDisplayNameForThreadOrGroup(name, indent);

                return m_Content;
            }

            public int CompareTo(ThreadInfo other)
            {
                if (this == other)
                    return 0;
                var results = EditorUtility.NaturalCompare(name, other.name);
                if (results != 0)
                    return results;
                if (threadIndex != other.threadIndex)
                    return threadIndex < other.threadIndex ? -1 : 1;
                return threadId.CompareTo(other.threadId);
            }

            public struct FlowEventData
            {
                public RawFrameDataView.FlowEvent flowEvent;
                public int frameIndex;
                public int threadIndex;

                public bool hasParentSampleIndex
                {
                    get
                    {
                        return (flowEvent.ParentSampleIndex > 0);
                    }
                }
            }
        }

        internal class GroupInfo
        {
            const int k_DefaultLineCountPerThread = 2;

            public AnimBool expanded;
            public string name;
            public float height;
            public List<ThreadInfo> threads;
            public Dictionary<ulong, ThreadInfo> threadIdMap;
            public int defaultLineCountPerThread = k_DefaultLineCountPerThread;

            [NonSerialized]
            GUIContent m_Content;

            public GroupInfo(string name, UnityEngine.Events.UnityAction foldoutStateChangedCallback)
                : this(name, foldoutStateChangedCallback, false) {}

            public GroupInfo(string name, UnityEngine.Events.UnityAction foldoutStateChangedCallback, bool expanded, int defaultLineCountPerThread = k_DefaultLineCountPerThread, float height = k_GroupHeight)
            {
                this.name = name;
                this.height = height;
                this.defaultLineCountPerThread = defaultLineCountPerThread;
                this.expanded = new AnimBool(expanded);

                if (foldoutStateChangedCallback != null)
                    this.expanded.valueChanged.AddListener(foldoutStateChangedCallback);

                threads = new List<ThreadInfo>();
                threadIdMap = new Dictionary<ulong, ThreadInfo>();
            }

            public void Clear()
            {
                height = 0;
                threads.Clear();
                threadIdMap.Clear();
            }

            public GUIContent DisplayName
            {
                get
                {
                    if (m_Content == null)
                        m_Content = CreateDisplayNameForThreadOrGroup(name, false);

                    return m_Content;
                }
            }
        }

        [NonSerialized]
        List<GroupInfo> m_Groups = null;
        [NonSerialized]
        HashSet<DrawnFlowIndicatorCacheValue> m_DrawnFlowIndicatorsCache = new HashSet<DrawnFlowIndicatorCacheValue>(new DrawnFlowIndicatorCacheValueComparer());

        // Not localizable strings - should match group names in native code.
        const string k_MainGroupName = "";
        const string k_JobSystemGroupName = "Job";
        const string k_LoadingGroupName = "Loading";
        const string k_ScriptingThreadsGroupName = "Scripting Threads";
        const string k_BackgroundJobSystemGroupName = "Background Job";
        const string k_ProfilerThreadsGroupName = "Profiler";
        const string k_OtherThreadsGroupName = "Other Threads";

        internal class Styles
        {
            public GUIStyle background = "OL Box";
            public GUIStyle tooltip = "AnimationEventTooltip";
            public GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
            public GUIStyle bar = "ProfilerTimelineBar";
            public GUIStyle leftPane = "ProfilerTimelineLeftPane";
            public GUIStyle rightPane = "ProfilerRightPane";
            public GUIStyle foldout = "ProfilerTimelineFoldout";
            public GUIStyle profilerGraphBackground = "ProfilerGraphBackground";
            public GUIStyle timelineTick = "AnimationTimelineTick";
            public GUIStyle rectangleToolSelection = "RectangleToolSelection";
            public GUIStyle timeAreaToolbar = "TimeAreaToolbar";
            public GUIStyle digDownArrow = "ProfilerTimelineDigDownArrow";
            public GUIStyle rollUpArrow = "ProfilerTimelineRollUpArrow";
            public GUIStyle bottomShadow = "BottomShadowInwards";

            public string localizedStringTotalAcrossFrames = L10n.Tr("\n{0} total over {1} frames on thread '{2}'");
            public string localizedStringTotalAcumulatedTime = L10n.Tr("\n\nCurrent frame accumulated time:");
            public string localizedStringTotalInThread = L10n.Tr("\n{0} for {1} instances on thread '{2}'");
            public string localizedStringTotalInFrame = L10n.Tr("\n{0} for {1} instances over {2} threads");

            public Color frameDelimiterColor = Color.white.RGBMultiplied(0.4f);
            Color m_RangeSelectionColorLight = new Color32(255, 255, 255, 90);
            Color m_RangeSelectionColorDark = new Color32(200, 200, 200, 40);
            public Color rangeSelectionColor => EditorGUIUtility.isProSkin ? m_RangeSelectionColorDark : m_RangeSelectionColorLight;
            Color m_OutOfRangeColorLight = new Color32(160, 160, 160, 127);
            Color m_OutOfRangeColorDark = new Color32(40, 40, 40, 127);
            public Color outOfRangeColor => EditorGUIUtility.isProSkin ? m_OutOfRangeColorDark : m_OutOfRangeColorLight;
        }

        static Styles ms_Styles;

        static Styles styles
        {
            get { return ms_Styles ?? (ms_Styles = new Styles()); }
        }

        class EntryInfo
        {
            public int frameId = FrameDataView.invalidOrCurrentFrameIndex;
            public int threadIndex = FrameDataView.invalidThreadIndex;
            public int nativeIndex = TimelineIndexHelper.invalidNativeTimelineEntryIndex; // Uniquely identifies the sample for the thread and frame.
            public float relativeYPos = 0.0f;
            public float time = 0.0f;
            public float duration = 0.0f;
            public string name = string.Empty;

            public bool IsValid()
            {
                return this.name.Length > 0;
            }

            public bool Equals(int frameId, int threadIndex, int nativeIndex)
            {
                return frameId == this.frameId && threadIndex == this.threadIndex && nativeIndex == this.nativeIndex;
            }

            public virtual void Reset()
            {
                this.frameId = FrameDataView.invalidOrCurrentFrameIndex;
                this.threadIndex = FrameDataView.invalidThreadIndex;
                this.nativeIndex = TimelineIndexHelper.invalidNativeTimelineEntryIndex;
                this.relativeYPos = 0.0f;
                this.time = 0.0f;
                this.duration = 0.0f;
                this.name = string.Empty;
            }
        }

        class SelectedEntryInfo : EntryInfo
        {
            public const int invalidInstancIdCount = -1;
            public const float invalidDuration = -1.0f;
            // The associated GameObjects instance ID. Negative means Native Object, Positive means Managed Object, 0 means not set (as in, no object associated)
            public int instanceId = 0;
            public string metaData = string.Empty;

            public float totalDurationForThread = invalidDuration;
            public int instanceCountForThread = invalidInstancIdCount;
            public float totalDurationForFrame = invalidDuration;
            public int instanceCountForFrame = invalidInstancIdCount;
            public int threadCount = 0;
            public bool hasCallstack = false;

            public string nonProxyName = null;
            public ReadOnlyCollection<string> sampleStack = null;
            public int nonProxyDepthDifference = 0;

            public GUIContent cachedSelectionTooltipContent = null;

            public float downwardsZoomableAreaSpaceNeeded = 0;

            public List<RawFrameDataView.FlowEvent> FlowEvents { get; } = new List<RawFrameDataView.FlowEvent>();

            public override void Reset()
            {
                base.Reset();

                this.instanceId = 0;
                this.metaData = string.Empty;

                this.totalDurationForThread = invalidDuration;
                this.instanceCountForThread = invalidInstancIdCount;
                this.totalDurationForFrame = invalidDuration;
                this.instanceCountForFrame = invalidInstancIdCount;
                this.threadCount = 0;
                this.hasCallstack = false;
                this.nonProxyName = null;
                this.sampleStack = null;
                this.cachedSelectionTooltipContent = null;
                this.nonProxyDepthDifference = 0;
                this.downwardsZoomableAreaSpaceNeeded = 0;

                FlowEvents.Clear();
            }
        }

        struct TimelineIndexHelper
        {
            int m_SampleIndex;
            // m_NativeTimelineEntryIndex is m_SampleIndex -1 because the root sample is not considered as an entry in the Native timeline code
            int m_NativeTimelineEntryIndex;
            public const int invalidNativeTimelineEntryIndex = -1;

            public static TimelineIndexHelper invalidIndex => new TimelineIndexHelper() { m_NativeTimelineEntryIndex = invalidNativeTimelineEntryIndex, m_SampleIndex = RawFrameDataView.invalidSampleIndex };

            public int sampleIndex
            {
                get { return m_SampleIndex; }
                set
                {
                    m_SampleIndex = value;
                    m_NativeTimelineEntryIndex = value > 0 ? value - 1 : RawFrameDataView.invalidSampleIndex;
                }
            }
            public int nativeTimelineEntryIndex
            {
                get { return m_NativeTimelineEntryIndex; }
                set
                {
                    m_NativeTimelineEntryIndex = value;
                    m_SampleIndex = value >= 0 ? value + 1 : invalidNativeTimelineEntryIndex;
                }
            }
            public bool valid => m_NativeTimelineEntryIndex >= 0 && m_SampleIndex >= 0;
        }

        // a local cache of the marker Id path, which is modified in frames other than the one originally selected, in case the marker ids changed
        // changing m_SelectionPendingTransfer.markerIdPath instead of this local one would potentially corrupt the markerIdPath in the original frame
        // that would lead to confusion where it is assumed to be valid.
        List<int> m_LocalSelectedItemMarkerIdPath = new List<int>();
        ProfilerTimeSampleSelection m_SelectionPendingTransfer = null;
        int m_ThreadIndexOfSelectionPendingTransfer = FrameDataView.invalidThreadIndex;
        bool m_FrameSelectionVerticallyAfterTransfer = false;
        bool m_Scheduled_FrameSelectionVertically = false;

        public event Action<ProfilerTimeSampleSelection> selectionChanged;

        struct RawSampleIterationInfo { public int partOfThePath; public int lastSampleIndexInScope; }

        static RawSampleIterationInfo[] s_SkippedScopesCache = new RawSampleIterationInfo[1024];
        static int[] s_LastSampleInScopeOfThePathCache = new int[1024];

        static List<int> s_SampleIndexPathCache = new List<int>(1024);
        [MethodImpl(256 /*MethodImplOptions.AggressiveInlining*/)]
        static List<int> GetCachedSampleIndexPath(int requiredCapacity)
        {
            if (s_SampleIndexPathCache.Capacity < requiredCapacity)
                s_SampleIndexPathCache.Capacity = requiredCapacity;
            s_SampleIndexPathCache.Clear();
            return s_SampleIndexPathCache;
        }

        [NonSerialized]
        protected IProfilerSampleNameProvider m_ProfilerSampleNameProvider;

        float scrollOffsetY
        {
            get { return -m_TimeArea.shownArea.y * m_TimeArea.scale.y; }
        }

        int maxContextFramesToShow => m_ProfilerWindow.ProfilerWindowOverheadIsAffectingProfilingRecordingData() ? 1 : k_MaxNeighborFrames;

        [NonSerialized]
        ZoomableArea m_TimeArea;
        TickHandler m_HTicks;
        SelectedEntryInfo m_SelectedEntry = new SelectedEntryInfo();
        float m_SelectedThreadY = 0.0f;
        float m_SelectedThreadYRange = 0.0f;
        ThreadInfo m_SelectedThread = null;
        float m_LastHeightForAllBars = -1;
        float m_LastFullRectHeight = -1;
        float m_MaxLinesToDisplayForTheCurrentlyModifiedSplitter = -1;

        [NonSerialized]
        int m_LastSelectedFrameIndex = FrameDataView.invalidThreadIndex;
        [NonSerialized]
        int m_LastMaxContextFramesToShow = -1;
        [NonSerialized]
        List<RawFrameDataView.FlowEvent> m_CachedThreadFlowEvents = new List<RawFrameDataView.FlowEvent>();

        [Flags]
        enum ProcessedInputs
        {
            MouseDown = 1 << 0,
            PanningOrZooming = 1 << 1,
            SplitterMoving = 1 << 2,
            FrameSelection = 1 << 3,
            RangeSelection = 1 << 4,
        }

        ProcessedInputs m_LastRepaintProcessedInputs;
        ProcessedInputs m_CurrentlyProcessedInputs;

        enum HandleThreadSplitterFoldoutButtonsCommand
        {
            OnlyHandleInput,
            OnlyDraw,
        }

        enum ThreadSplitterCommand
        {
            HandleThreadSplitter,
            HandleThreadSplitterFoldoutButtons,
        }

        struct RangeSelectionInfo
        {
            public static readonly int controlIDHint = "RangeSelection".GetHashCode();
            public bool active;
            public bool mouseDown;
            public float mouseDownTime;
            public float startTime;
            public float endTime;
            public float duration => endTime - startTime;
        }

        RangeSelectionInfo m_RangeSelection = new RangeSelectionInfo();

        static readonly ProfilerMarker m_DoGUIMarker = new ProfilerMarker(nameof(ProfilerTimelineGUI) + ".DoGUI");

        public ProfilerTimelineGUI()
        {
            // Configure default groups
            m_Groups = new List<GroupInfo>(new GroupInfo[]
            {
                new GroupInfo(k_MainGroupName, RepaintProfilerWindow, true, 3, 0),
                new GroupInfo(k_JobSystemGroupName, RepaintProfilerWindow),
                new GroupInfo(k_LoadingGroupName, RepaintProfilerWindow),
                new GroupInfo(k_ScriptingThreadsGroupName, RepaintProfilerWindow),
                new GroupInfo(k_BackgroundJobSystemGroupName, RepaintProfilerWindow),
                new GroupInfo(k_ProfilerThreadsGroupName, RepaintProfilerWindow),
                new GroupInfo(k_OtherThreadsGroupName, RepaintProfilerWindow),
            });

            m_HTicks = new TickHandler();
            m_HTicks.SetTickModulos(k_TickModulos);
        }

        public override void OnEnable(CPUOrGPUProfilerModule cpuOrGpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            base.OnEnable(cpuOrGpuModule, profilerWindow, isGpuView);
            m_ProfilerSampleNameProvider = cpuOrGpuModule;
            if (m_Groups != null)
            {
                for (int i = 0; i < m_Groups.Count; i++)
                {
                    m_Groups[i].expanded.value = SessionState.GetBool($"Profiler.Timeline.GroupExpanded.{m_Groups[i].name}", false);
                }
            }
        }

        void RepaintProfilerWindow()
        {
            m_ProfilerWindow?.Repaint();
        }

        static GUIContent CreateDisplayNameForThreadOrGroup(string name, bool indent)
        {
            var content = GUIContent.Temp(name, name);

            const int indentSize = 10;
            bool stripped = false;
            if ((styles.leftPane.CalcSize(content).x + (indent ? indentSize : 0)) > Chart.kSideWidth)
            {
                stripped = true;
                content.text += "...";
                while ((styles.leftPane.CalcSize(content).x + (indent ? indentSize : 0)) > Chart.kSideWidth)
                {
                    content.text = content.text.Remove(content.text.Length - 4, 1);
                }
            }

            var result = new GUIContent();
            result.text = content.text;
            if (stripped)
                result.tooltip = name;

            return result;
        }

        GroupInfo GetOrCreateGroupByName(string name)
        {
            var group = m_Groups.Find(g => g.name == name);
            if (group == null)
            {
                group = new GroupInfo(name, RepaintProfilerWindow);
                m_Groups.Add(group);
            }

            return group;
        }

        void AddNeighboringActiveThreads(int currentFrameIndex, int displayFrameOffset)
        {
            var frameIndex = currentFrameIndex + displayFrameOffset;
            GroupInfo lastGroupLookupValue = null;
            for (int threadIndex = 0;; ++threadIndex)
            {
                using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                {
                    if (frameData == null || !frameData.valid)
                        break;

                    var threadId = frameData.threadId;
                    // If threadId is unavailable we can't match threads in different frames
                    if (threadId == 0)
                        break;

                    var threadGroupName = frameData.threadGroupName;
                    GroupInfo group;
                    if (lastGroupLookupValue != null && lastGroupLookupValue.name == threadGroupName)
                        group = lastGroupLookupValue;
                    else
                        group = lastGroupLookupValue = GetOrCreateGroupByName(threadGroupName);

                    var maxDepth = frameData.maxDepth - 1;
                    if (group.threadIdMap.TryGetValue(threadId, out var threadInfo))
                    {
                        threadInfo.threadIndices[k_MaxNeighborFrames + displayFrameOffset] = threadIndex;
                        threadInfo.maxDepth = Mathf.Max(threadInfo.maxDepth, maxDepth);
                    }
                    else
                    {
                        // frameData.maxDepth includes the thread sample which is not getting displayed, so we store it at -1 for all intents and purposes
                        threadInfo = new ThreadInfo(frameData.threadName, threadId, FrameDataView.invalidThreadIndex, maxDepth, group.defaultLineCountPerThread);
                        threadInfo.threadIndices[k_MaxNeighborFrames + displayFrameOffset] = threadIndex;
                        group.threads.Add(threadInfo);
                        group.threadIdMap.Add(threadId, threadInfo);
                    }
                }
            }
        }

        void UpdateGroupAndThreadInfo(int frameIndex)
        {
            // Only update groups cache when we change frame index or neighbor frames count.
            if (m_LastSelectedFrameIndex == frameIndex && m_LastMaxContextFramesToShow == maxContextFramesToShow)
                return;

            m_LastSelectedFrameIndex = frameIndex;
            m_LastMaxContextFramesToShow = maxContextFramesToShow;

            // Mark threads terminated by nullifying name.
            // This helps to reuse the ThreadInfo object in most cases when switching frames and eliminate allocations.
            // Besides caching we also preserve linesToDisplay information.
            foreach (var group in m_Groups)
            {
                for (var i = 0; i < group.threads.Count; ++i)
                    group.threads[i].Reset();
            }

            GroupInfo lastGroupLookupValue = null;

            var canUseThreadIdForThreadAlignment = true;
            for (int threadIndex = 0;; ++threadIndex)
            {
                using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                {
                    if (frameData == null || !frameData.valid)
                        break;

                    var threadGroupName = frameData.threadGroupName;
                    var threadId = frameData.threadId;

                    GroupInfo group;
                    // Micro optimization by caching last accessed group - that guarantees hits for jobs and scripting threads.
                    if (lastGroupLookupValue != null && lastGroupLookupValue.name == threadGroupName)
                        group = lastGroupLookupValue;
                    else
                        group = lastGroupLookupValue = GetOrCreateGroupByName(threadGroupName);

                    if (threadId != 0)
                    {
                        var maxDepth = frameData.maxDepth - 1;
                        if (group.threadIdMap.TryGetValue(threadId, out var thread))
                        {
                            // Reuse existing ThreadInfo object, but update its name, index and dempth.
                            thread.Update(frameData.threadName, threadIndex, maxDepth);
                        }
                        else
                        {
                            // frameData.maxDepth includes the thread sample which is not getting displayed, so we store it at -1 for all intents and purposes
                            thread = new ThreadInfo(frameData.threadName, threadId, threadIndex, maxDepth, group.defaultLineCountPerThread);

                            // the main thread gets double the size
                            if (threadIndex == 0)
                                thread.linesToDisplay *= 2;

                            // Add a new thread
                            group.threads.Add(thread);
                            group.threadIdMap.Add(threadId, thread);
                        }
                    }
                    else
                    {
                        // Old data compatibility path where we don't have threadId.
                        canUseThreadIdForThreadAlignment = false;

                        var threads = group.threads;
                        ThreadInfo thread = threads.Find(t => t.threadIndex == threadIndex);
                        if (thread == null)
                        {
                            // frameData.maxDepth includes the thread sample which is not getting displayed, so we store it at -1 for all intents and purposes
                            thread = new ThreadInfo(frameData.threadName, threadId, threadIndex, frameData.maxDepth - 1, group.defaultLineCountPerThread);
                            for (var i = 0; i < k_MaxDisplayFrames; ++i)
                                thread.threadIndices[i] = threadIndex;

                            // the main thread gets double the size
                            if (threadIndex == 0)
                                thread.linesToDisplay *= 2;

                            group.threads.Add(thread);
                        }
                        else
                        {
                            thread.name = frameData.threadName;
                            thread.maxDepth = frameData.maxDepth - 1;
                        }
                    }
                }
            }

            // If this is a new capture with threadId support we scan neighbor frames to ensure we have a consistent view of threads
            // across all visible frames.
            if (canUseThreadIdForThreadAlignment)
            {
                for (var i = -1; i >= -m_LastMaxContextFramesToShow && frameIndex - i >= ProfilerDriver.firstFrameIndex; --i)
                {
                    if (!ProfilerDriver.GetFramesBelongToSameSession(frameIndex + i, frameIndex))
                        break;
                    AddNeighboringActiveThreads(frameIndex, i);
                }

                for (var i = 1; i <= m_LastMaxContextFramesToShow && frameIndex + i <= ProfilerDriver.lastFrameIndex; ++i)
                {
                    if (!ProfilerDriver.GetFramesBelongToSameSession(frameIndex + i, frameIndex))
                        break;
                    AddNeighboringActiveThreads(frameIndex, i);
                }
            }

            // Sort threads by name
            foreach (var group in m_Groups)
            {
                // and cleanup those that are no longer present in the view.
                for (var i = 0; i < group.threads.Count;)
                {
                    if (group.threads[i].maxDepth == -1)
                    {
                        group.threadIdMap.Remove(group.threads[i].threadId);
                        group.threads.RemoveAt(i);
                    }
                    else
                        ++i;
                }
                group.threads.Sort();
            }
        }

        float CalculateHeightForAllBars(Rect fullRect, out float combinedHeaderHeight, out float combinedThreadHeight)
        {
            combinedHeaderHeight = 0f;
            combinedThreadHeight = 0f;

            for (int i = 0; i < m_Groups.Count; i++)
            {
                var group = m_Groups[i];
                bool mainGroup = group.name == k_MainGroupName;
                if (mainGroup)
                {
                    // main group has no height of it's own and is always expanded
                    group.height = 0;
                    group.expanded.value = true;
                }
                else
                {
                    group.height = group.expanded.value ? k_GroupHeight : Math.Max(group.height, group.threads.Count * k_ThreadMinHeightCollapsed);
                    // Ensure minimum height is k_GroupHeight.
                    if (group.height < k_GroupHeight)
                        group.height = k_GroupHeight;
                }

                combinedHeaderHeight += group.height;

                foreach (var thread in group.threads)
                {
                    int lines = Mathf.RoundToInt(thread.linesToDisplay);
                    thread.height = CalculateThreadHeight(lines) * group.expanded.faded;
                    combinedThreadHeight += thread.height;
                }
            }

            return combinedHeaderHeight + combinedThreadHeight;
        }

        bool DrawBar(Rect r, float y, float height, GUIContent content, bool group, bool expanded, bool indent)
        {
            Rect leftRect = new Rect(r.x - Chart.kSideWidth, y, Chart.kSideWidth, height);
            Rect rightRect = new Rect(r.x, y, r.width, height);
            if (Event.current.type == EventType.Repaint)
            {
                styles.rightPane.Draw(rightRect, false, false, false, false);
                if (indent)
                    styles.leftPane.padding.left += 10;
                styles.leftPane.Draw(leftRect, content, false, false, false, false);
                if (indent)
                    styles.leftPane.padding.left -= 10;
            }

            if (group)
            {
                leftRect.width -= 1.0f; // text should not draw ontop of right border
                leftRect.xMin += 3.0f; // shift toggle arrow right
                leftRect.yMin += 1.0f;
                return GUI.Toggle(leftRect, expanded, GUIContent.none, styles.foldout);
            }

            return false;
        }

        void DrawBars(Rect r, float scaleForThreadHeight)
        {
            bool hasThreadinfoToDraw = false;
            foreach (var group in m_Groups)
            {
                foreach (var thread in group.threads)
                {
                    if (thread != null)
                    {
                        hasThreadinfoToDraw = true;
                        break;
                    }
                }

                if (hasThreadinfoToDraw)
                    break;
            }

            if (!hasThreadinfoToDraw)
                return; // nothing to draw

            float y = r.y;
            foreach (var groupInfo in m_Groups)
            {
                bool mainGroup = groupInfo.name == k_MainGroupName;
                if (!mainGroup)
                {
                    var height = groupInfo.height;
                    var expandedState = groupInfo.expanded.target;
                    var newExpandedState = DrawBar(r, y, height, groupInfo.DisplayName, true, expandedState, false);

                    if (newExpandedState != expandedState)
                    {
                        SessionState.SetBool($"Profiler.Timeline.GroupExpanded.{groupInfo.name}", newExpandedState);
                        groupInfo.expanded.value = newExpandedState;
                    }

                    y += height;
                }

                foreach (var threadInfo in groupInfo.threads)
                {
                    var height = threadInfo.height * scaleForThreadHeight;
                    if (height != 0)
                        DrawBar(r, y, height, threadInfo.DisplayName(!mainGroup), false, true, !mainGroup);
                    y += height;
                }
            }
        }

        void DoNativeProfilerTimeline(Rect r, int frameIndex, int threadIndex, float timeOffset, bool ghost, float scaleForThreadHeight)
        {
            // Add some margins to each thread view.
            Rect clipRect = r;
            float topMargin = Math.Min(clipRect.height * 0.25f, 1); // Reduce margin when drawing thin timelines (to more easily get an overview over collapsed threadgroups)
            float bottomMargin = topMargin + 1;
            clipRect.y += topMargin;
            clipRect.height -= bottomMargin;

            GUI.BeginGroup(clipRect);
            {
                Rect localRect = clipRect;
                localRect.x = 0;

                if (Event.current.type == EventType.Repaint)
                {
                    DrawNativeProfilerTimeline(localRect, frameIndex, threadIndex, timeOffset, ghost);
                }
                else
                {
                    var controlId = GUIUtility.GetControlID(BaseStyles.timelineTimeAreaControlId, FocusType.Passive, localRect);
                    if (!ghost && (Event.current.GetTypeForControl(controlId) == EventType.MouseDown ||  // Ghosts are not clickable (or can contain an active selection)
                                                                                                         // Selection of samples is handled in HandleNativeProfilerTimelineInput so it needs to get called if there is a selection to transfer
                                   (m_SelectionPendingTransfer != null && threadIndex == m_ThreadIndexOfSelectionPendingTransfer)))
                    {
                        HandleNativeProfilerTimelineInput(localRect, frameIndex, threadIndex, timeOffset, topMargin, scaleForThreadHeight, controlId);
                    }
                }
            }
            GUI.EndGroup();
        }

        void DrawNativeProfilerTimeline(Rect threadRect, int frameIndex, int threadIndex, float timeOffset, bool ghost)
        {
            bool hasSelection = m_SelectedEntry.threadIndex == threadIndex && m_SelectedEntry.frameId == frameIndex;

            NativeProfilerTimeline_DrawArgs drawArgs = new NativeProfilerTimeline_DrawArgs();
            drawArgs.Reset();
            drawArgs.frameIndex = frameIndex;
            drawArgs.threadIndex = threadIndex;
            drawArgs.timeOffset = timeOffset;
            drawArgs.threadRect = threadRect;

            // cull text that would otherwise draw over the bottom scrollbar
            drawArgs.threadRect.yMax = Mathf.Min(drawArgs.threadRect.yMax, m_TimeArea.shownArea.height - m_TimeArea.hSliderHeight);
            drawArgs.shownAreaRect = m_TimeArea.shownArea;
            drawArgs.selectedEntryIndex = hasSelection ? m_SelectedEntry.nativeIndex : TimelineIndexHelper.invalidNativeTimelineEntryIndex;
            drawArgs.mousedOverEntryIndex = TimelineIndexHelper.invalidNativeTimelineEntryIndex;

            NativeProfilerTimeline.Draw(ref drawArgs);
        }

        static readonly ProfilerMarker k_TransferSelectionMarker = new ProfilerMarker($"{nameof(ProfilerTimelineGUI)} Transfer Selection");
        static readonly ProfilerMarker k_TransferSelectionAcrossFramesMarker = new ProfilerMarker($"{nameof(ProfilerTimelineGUI)} Transfer Selection Between Frames");

        void HandleNativeProfilerTimelineInput(Rect threadRect, int frameIndex, int threadIndex, float timeOffset, float topMargin, float scaleForThreadHeight, int controlId)
        {
            // Only let this thread view change mouse state if it contained the mouse pos
            Rect clippedRect = threadRect;
            clippedRect.y = 0;

            var eventType = Event.current.GetTypeForControl(controlId);
            bool inThreadRect = clippedRect.Contains(Event.current.mousePosition);
            if (!inThreadRect && !(m_SelectionPendingTransfer != null && threadIndex == m_ThreadIndexOfSelectionPendingTransfer))
                return;

            bool singleClick = Event.current.clickCount == 1 && eventType == EventType.MouseDown;
            bool doubleClick = Event.current.clickCount == 2 && eventType == EventType.MouseDown;

            bool doSelect = (singleClick || doubleClick) && Event.current.button == 0 || (m_SelectionPendingTransfer != null && threadIndex == m_ThreadIndexOfSelectionPendingTransfer);
            if (!doSelect)
                return;

            using (var frameData = new RawFrameDataView(frameIndex, threadIndex))
            {
                TimelineIndexHelper indexHelper = TimelineIndexHelper.invalidIndex;
                float relativeYPosition = 0;
                string name = null;
                bool fireSelectionChanged = false;
                string nonProxySampleName = null;
                ReadOnlyCollection<string> markerNamePath = null;
                var nonProxySampleDepthDifference = 0;
                if (m_SelectionPendingTransfer != null)
                {
                    using (k_TransferSelectionMarker.Auto())
                    {
                        if (m_SelectionPendingTransfer.markerPathDepth <= 0)
                            return;

                        markerNamePath = m_SelectionPendingTransfer.markerNamePath;
                        var markerIdPath = m_SelectionPendingTransfer.markerIdPath;

                        indexHelper.sampleIndex = m_SelectionPendingTransfer.rawSampleIndex;
                        fireSelectionChanged = false;
                        name = m_SelectionPendingTransfer.sampleDisplayName;

                        var markerPathLength = markerIdPath.Count;
                        // initial assumption is that the depth is the full marker path. The depth will be revised if it is a Proxy Selection
                        var depth = markerPathLength;

                        // A quick sanity check on the validity of going with the raw index
                        var rawSampleIndexIsValid = m_SelectionPendingTransfer.frameIndexIsSafe &&
                            frameData.frameIndex == m_SelectionPendingTransfer.safeFrameIndex &&
                            m_SelectionPendingTransfer.rawSampleIndex < frameData.sampleCount &&
                            frameData.GetSampleMarkerId(m_SelectionPendingTransfer.rawSampleIndex) == markerIdPath[markerPathLength - 1];

                        if (!rawSampleIndexIsValid)
                        {
                            using (k_TransferSelectionAcrossFramesMarker.Auto())
                            {
                                if (m_LocalSelectedItemMarkerIdPath == null)
                                    m_LocalSelectedItemMarkerIdPath = new List<int>(markerPathLength);
                                else if (m_LocalSelectedItemMarkerIdPath.Capacity < markerPathLength)
                                    m_LocalSelectedItemMarkerIdPath.Capacity = markerPathLength;

                                m_LocalSelectedItemMarkerIdPath.Clear();
                                for (int i = 0; i < markerPathLength; i++)
                                {
                                    // update the marker Ids, they can't be trusted since they originated on another frame
                                    m_LocalSelectedItemMarkerIdPath.Add(frameData.GetMarkerId(markerNamePath[i]));
                                }
                                var longestMatchingPath = new List<int>(markerPathLength);

                                // The selection was made in a different frame so the raw sample ID is worthless here
                                // instead the selection needs to be transfered by finding the first sample with the same marker path.
                                if (markerPathLength > 0)
                                {
                                    indexHelper.sampleIndex = FindFirstSampleThroughMarkerPath(
                                        frameData, m_ProfilerSampleNameProvider,
                                        m_LocalSelectedItemMarkerIdPath, markerPathLength, ref name,
                                        longestMatchingPath: longestMatchingPath);
                                }
                                if (!indexHelper.valid && longestMatchingPath.Count > 0)
                                {
                                    // use the longest matching path for a "proxy" selection, i.e. select the sample that is closest to what was selected in the other frame
                                    indexHelper.sampleIndex = longestMatchingPath[longestMatchingPath.Count - 1];
                                    if (indexHelper.valid)
                                    {
                                        // it's likely not named the same
                                        nonProxySampleName = name;
                                        depth = longestMatchingPath.Count;
                                        nonProxySampleDepthDifference = depth - markerPathLength;
                                        name = null;
                                    }
                                }
                                if (!indexHelper.valid)
                                {
                                    m_SelectionPendingTransfer = null;
                                    m_LocalSelectedItemMarkerIdPath.Clear();
                                    m_ThreadIndexOfSelectionPendingTransfer = FrameDataView.invalidThreadIndex;
                                    m_FrameSelectionVerticallyAfterTransfer = false;
                                    m_Scheduled_FrameSelectionVertically = false;
                                    m_SelectedEntry.Reset();
                                    return;
                                }
                            }
                        }
                        else if (m_SelectionPendingTransfer.markerIdPath != null)
                            depth = m_SelectionPendingTransfer.markerPathDepth;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = frameData.GetSampleName(indexHelper.sampleIndex);
                        }
                        var requiredThreadHeight = CalculateThreadHeight(depth);
                        var requiredThreadRect = threadRect;
                        if (requiredThreadRect.height < requiredThreadHeight)
                            requiredThreadRect.height = requiredThreadHeight;
                        var entryPosArgs = new NativeProfilerTimeline_GetEntryPositionInfoArgs();
                        entryPosArgs.frameIndex = frameData.frameIndex;
                        entryPosArgs.threadIndex = m_ThreadIndexOfSelectionPendingTransfer;
                        entryPosArgs.sampleIndex = indexHelper.sampleIndex;
                        entryPosArgs.threadRect = requiredThreadRect;
                        entryPosArgs.timeOffset = timeOffset;
                        entryPosArgs.shownAreaRect = m_TimeArea.shownArea;
                        NativeProfilerTimeline.GetEntryPositionInfo(ref entryPosArgs);
                        relativeYPosition = entryPosArgs.out_Position.y + entryPosArgs.out_Size.y + topMargin;
                        m_SelectionPendingTransfer = null;
                        m_LocalSelectedItemMarkerIdPath.Clear();
                        m_ThreadIndexOfSelectionPendingTransfer = FrameDataView.invalidThreadIndex;
                        m_Scheduled_FrameSelectionVertically = m_FrameSelectionVerticallyAfterTransfer;
                        m_FrameSelectionVerticallyAfterTransfer = false;
                    }
                }
                else
                {
                    NativeProfilerTimeline_GetEntryAtPositionArgs posArgs = new NativeProfilerTimeline_GetEntryAtPositionArgs();
                    posArgs.Reset();
                    posArgs.frameIndex = frameData.frameIndex;
                    posArgs.threadIndex = frameData.threadIndex;
                    posArgs.timeOffset = timeOffset;
                    posArgs.threadRect = threadRect;
                    posArgs.threadRect.height *= scaleForThreadHeight;
                    posArgs.shownAreaRect = m_TimeArea.shownArea;
                    posArgs.position = Event.current.mousePosition;
                    NativeProfilerTimeline.GetEntryAtPosition(ref posArgs);

                    indexHelper.nativeTimelineEntryIndex = posArgs.out_EntryIndex;
                    relativeYPosition = posArgs.out_EntryYMaxPos + topMargin;
                    name = posArgs.out_EntryName;
                    fireSelectionChanged = true;
                }


                if (indexHelper.valid)
                {
                    bool selectedChanged = !m_SelectedEntry.Equals(frameData.frameIndex, frameData.threadIndex, indexHelper.nativeTimelineEntryIndex);
                    if (selectedChanged)
                    {
                        // Read out timing info
                        NativeProfilerTimeline_GetEntryTimingInfoArgs timingInfoArgs = new NativeProfilerTimeline_GetEntryTimingInfoArgs();
                        timingInfoArgs.Reset();
                        timingInfoArgs.frameIndex = frameData.frameIndex;
                        timingInfoArgs.threadIndex = frameData.threadIndex;
                        timingInfoArgs.entryIndex = indexHelper.nativeTimelineEntryIndex;
                        timingInfoArgs.calculateFrameData = true;
                        NativeProfilerTimeline.GetEntryTimingInfo(ref timingInfoArgs);

                        // Read out instance info for selection
                        NativeProfilerTimeline_GetEntryInstanceInfoArgs instanceInfoArgs = new NativeProfilerTimeline_GetEntryInstanceInfoArgs();
                        instanceInfoArgs.Reset();
                        instanceInfoArgs.frameIndex = frameData.frameIndex;
                        instanceInfoArgs.threadIndex = frameData.threadIndex;
                        instanceInfoArgs.entryIndex = indexHelper.nativeTimelineEntryIndex;
                        NativeProfilerTimeline.GetEntryInstanceInfo(ref instanceInfoArgs);

                        if (fireSelectionChanged)
                        {
                            var selection = new ProfilerTimeSampleSelection(frameData.frameIndex, frameData.threadGroupName, frameData.threadName, frameData.threadId, indexHelper.sampleIndex, name);
                            selection.GenerateMarkerNamePath(frameData, new List<int>(instanceInfoArgs.out_PathMarkerIds), instanceInfoArgs.out_Path);
                            selectionChanged(selection);
                            markerNamePath = selection.markerNamePath;
                        }
                        // Set selected entry info
                        m_SelectedEntry.Reset();
                        m_SelectedEntry.frameId = frameData.frameIndex;
                        m_SelectedEntry.threadIndex = frameData.threadIndex;
                        m_SelectedEntry.nativeIndex = indexHelper.nativeTimelineEntryIndex;
                        m_SelectedEntry.instanceId = instanceInfoArgs.out_Id;
                        m_SelectedEntry.time = timingInfoArgs.out_LocalStartTime;
                        m_SelectedEntry.duration = timingInfoArgs.out_Duration;
                        m_SelectedEntry.totalDurationForThread = timingInfoArgs.out_TotalDurationForThread;
                        m_SelectedEntry.instanceCountForThread = timingInfoArgs.out_InstanceCountForThread;
                        m_SelectedEntry.totalDurationForFrame = timingInfoArgs.out_TotalDurationForFrame;
                        m_SelectedEntry.instanceCountForFrame = timingInfoArgs.out_InstanceCountForFrame;
                        m_SelectedEntry.threadCount = timingInfoArgs.out_ThreadCountForFrame;
                        m_SelectedEntry.relativeYPos = relativeYPosition;
                        m_SelectedEntry.name = name;
                        m_SelectedEntry.hasCallstack = instanceInfoArgs.out_CallstackInfo != null && instanceInfoArgs.out_CallstackInfo.Length > 0;
                        m_SelectedEntry.metaData = instanceInfoArgs.out_MetaData;
                        m_SelectedEntry.sampleStack = markerNamePath;

                        if (nonProxySampleName != null)
                        {
                            m_SelectedEntry.nonProxyName = nonProxySampleName;
                            m_SelectedEntry.nonProxyDepthDifference = nonProxySampleDepthDifference;
                        }

                        if ((cpuModule.ViewOptions & CPUOrGPUProfilerModule.ProfilerViewFilteringOptions.ShowExecutionFlow) != 0)
                        {
                            // posArgs.out_EntryIndex is a MeshCache index which differs from sample index by 1 as root is not included into MeshCache.
                            frameData.GetSampleFlowEvents(indexHelper.sampleIndex, m_SelectedEntry.FlowEvents);
                            UpdateActiveFlowEventsForAllThreadsInAllVisibleFrames(frameData.frameIndex, m_SelectedEntry.FlowEvents);
                        }
                    }
                    if (eventType == EventType.MouseDown)
                    {
                        Event.current.Use();
                        UpdateSelectedObject(singleClick, doubleClick);

                        m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.FrameSelection;
                    }
                }
                else
                {
                    // click on empty space de-selects
                    if (doSelect)
                    {
                        ClearSelection();
                        Event.current.Use();

                        m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.FrameSelection;
                    }
                }
            }
        }

        /// <summary>
        /// Use this method to find the first matching Raw Sample Index (returned value).
        /// If <paramref name="longestMatchingPath"/> is not null, the search will also look for an approximate fit to the search criteria
        /// and fill this list with the path of Raw Sample Indices leading up to and including the searched sample or its approximation.
        /// </summary>
        /// <param name="iterator">
        /// A <see cref="RawFrameDataView"/> to use for searching, already pointing at the right frame and thread.</param>
        /// <param name="profilerSampleNameProvider">
        /// This sample name provider will be used to fill <paramref name="outName"/> with the name of the found sample.</param>
        /// <param name="markerIdPathToMatch">
        /// If provided, this method search for a sample that fits this marker Id path.
        /// If null, all paths will be explored in the search for a sample at a depth that fits the provided <paramref name="pathLength"/> or the <paramref name="specificRawSampleIndexToFind"/>.</param>
        /// <param name="pathLength">
        /// How deep should the search go?
        /// Any depth layers above the <paramref name="pathLength"/> - 1 threshold will be skipped.
        /// Provide iterator.maxDepth + 1 if you want to search through all samples regardless of depth.
        /// This value can be lower than the length of <paramref name="markerIdPathToMatch"/>, which means only a part of the path will be searched for.
        /// <param name="outName">
        /// If null or empty and if the searched sample was found, this name will be filled by the name of that sample as formatted by <paramref name="profilerSampleNameProvider"/>.</param>
        /// <param name="longestMatchingPath">
        /// If not null, aproximated fits to the search parameters will be explored.
        /// If there is a direct match found for the searched sample, this will contain the raw sample indices path leading up to and including the found sample.
        /// If there is then no direct match found for the searched sample, i.e. -1 was returned,
        /// this raw sample index path will lead to the sample that has the longest contiguous match to the provided <paramref name="markerIdPathToMatch"/>
        /// and within the scope of that contiguously matching path, the deepest sample that could be found while skipping some scopes in the <paramref name="markerIdPathToMatch"/>.</param>
        /// <returns>The raw sample index of the found sample or -1 if no direct fit was found.</returns>
        public static int FindFirstSampleThroughMarkerPath(
            RawFrameDataView iterator, IProfilerSampleNameProvider profilerSampleNameProvider,
            IList<int> markerIdPathToMatch, int pathLength, ref string outName,
            List<int> longestMatchingPath = null)
        {
            var sampleIndexPath = GetCachedSampleIndexPath(pathLength);

            return FindNextSampleThroughMarkerPath(
                iterator, profilerSampleNameProvider,
                markerIdPathToMatch, pathLength, ref outName, ref sampleIndexPath,
                longestMatchingPath: longestMatchingPath);
        }

        /// <summary>
        /// This searches for the MarkerId path leading up to a specific rawSampleIndex.
        /// </summary>
        /// <param name="iterator">
        /// A <see cref="RawFrameDataView"/> to use for searching, already pointing at the right frame and thread.</param>
        /// <param name="profilerSampleNameProvider">
        /// This sample name provider will be used to fill <paramref name="outName"/> with the name of the found sample.</param>
        /// <param name="rawSampleIndex">The sample to find the path to.</param>
        /// <param name="outName">
        /// If null or empty and if the searched sample was found, this name will be filled by the name of that sample as formatted by <paramref name="profilerSampleNameProvider"/>.</param>
        /// <param name="markerIdPath">Expected to be empty, will be filled with the marker id path for the sample, if that sample was found.</param>
        /// <returns>The <paramref name="rawSampleIndex"/> if found, otherwise -1.</returns>
        public static int GetItemMarkerIdPath(
            RawFrameDataView iterator,  IProfilerSampleNameProvider profilerSampleNameProvider,
            int rawSampleIndex, ref string outName, ref List<int> markerIdPath)
        {
            var unreachableDepth = iterator.maxDepth + 1;
            var sampleIndexPath = GetCachedSampleIndexPath(unreachableDepth);

            var sampleIdx = FindNextSampleThroughMarkerPath(
                iterator, profilerSampleNameProvider,
                markerIdPathToMatch: null, unreachableDepth, ref outName, ref sampleIndexPath,
                specificRawSampleIndexToFind: rawSampleIndex);

            if (sampleIdx != RawFrameDataView.invalidSampleIndex)
            {
                for (int i = 0; i < sampleIndexPath.Count; i++)
                {
                    markerIdPath.Add(iterator.GetSampleMarkerId(sampleIndexPath[i]));
                }
            }
            return sampleIdx;
        }

        /// <summary>
        /// Use this method to find the Raw Sample Index (returned value) and the path of Raw Sample Indices (<paramref name="sampleIndexPath"/>) leading up to a sample fitting the search criteria.
        /// If <paramref name="longestMatchingPath"/>, the search will also look for approximate fits to the search criteria and fill the path for that approximation, or the found sample, into that list.
        /// </summary>
        /// <param name="iterator">
        /// A <see cref="RawFrameDataView"/> to use for searching, already pointing at the right frame and thread.</param>
        /// <param name="profilerSampleNameProvider">
        /// This sample name provider will be used to fill <paramref name="outName"/> with the name of the found sample.</param>
        /// <param name="markerIdPathToMatch">
        /// If provided, this method search for a sample that fits this marker Id path.
        /// If null, all paths will be explored in the search for a sample at a depth that fits the provided <paramref name="pathLength"/> or the <paramref name="specificRawSampleIndexToFind"/>.</param>
        /// <param name="pathLength">
        /// How deep should the search go?
        /// Any depth layers above the <paramref name="pathLength"/> - 1 threshold will be skipped.
        /// Provide iterator.maxDepth + 1 if you want to search through all samples regardless of depth.
        /// This value can be lower than the length of <paramref name="markerIdPathToMatch"/>, which means only a part of the path will be searched for.
        /// <param name="outName">
        /// If null or empty and if the searched sample was found, this name will be filled by the name of that sample as formatted by <paramref name="profilerSampleNameProvider"/>.</param>
        /// <param name="sampleIndexPath">
        /// If a sample was found, i.e. the returned value wasn't -1, this list will contain the raw sample index path leading up to the found sample, but won't be including it.
        /// If this list isn't provided in empty form, the search will continue after the sample stack scope of the last sample in this path.</param>
        /// <param name="longestMatchingPath">
        /// If not null, aproximated fits to the search parameters will be explored.
        /// If there is a direct match found for the searched sample, this will contain the raw sample indices path leading up to and including the found sample, i.e. essentially a copy of <paramref name="sampleIndexPath"/>.
        /// If there is then no direct match found for the searched sample, i.e. -1 was returned,
        /// this raw sample index path will lead to the sample that has the longest contiguous match to the provided <paramref name="markerIdPathToMatch"/>
        /// and within the scope of that contiguously matching path, the deepest sample that could be found while skipping some scopes in the <paramref name="markerIdPathToMatch"/>.</param>
        /// <param name="specificRawSampleIndexToFind">
        /// When not provided as -1, the search will try to find the path to this specific sample, or return -1 if it failed to find it.</param>
        /// <param name="sampleIdFitsMarkerPathIndex">
        /// If provided additionally to <paramref name="markerIdPathToMatch"/>, this delegate will be queried for samples
        /// that do not fit <paramref name="markerIdPathToMatch"/> but might otherwise still fit, indicated by this Func returning true.</param>
        /// <returns>The raw sample index of the found sample or -1 if no direct fit was found. </returns>
        public static int FindNextSampleThroughMarkerPath(
            RawFrameDataView iterator, IProfilerSampleNameProvider profilerSampleNameProvider,
            IList<int> markerIdPathToMatch, int pathLength, ref string outName, ref List<int> sampleIndexPath,
            List<int> longestMatchingPath = null, int specificRawSampleIndexToFind = RawFrameDataView.invalidSampleIndex, Func<int, int, RawFrameDataView, bool> sampleIdFitsMarkerPathIndex = null)
        {
            var partOfThePath = sampleIndexPath.Count > 0 ? sampleIndexPath.Count - 1 : 0;
            var sampleIndex = partOfThePath == 0 ?
                /*skip the root sample*/ 1 :
                /*skip the last scope*/ sampleIndexPath[partOfThePath] + iterator.GetSampleChildrenCountRecursive(sampleIndexPath[partOfThePath]) + 1;
            bool foundSample = false;
            if (sampleIndexPath.Capacity < pathLength + 1)
                sampleIndexPath.Capacity = pathLength + 1;
            if (s_LastSampleInScopeOfThePathCache.Length < sampleIndexPath.Capacity)
                s_LastSampleInScopeOfThePathCache = new int[sampleIndexPath.Capacity];
            var lastSampleInScopeOfThePath = s_LastSampleInScopeOfThePathCache;
            var lastSampleInScopeOfThePathCount = 0;
            var lastSampleInScope = partOfThePath == 0 ? iterator.sampleCount - 1 : sampleIndex + iterator.GetSampleChildrenCountRecursive(sampleIndex);
            var allowProxySelection = longestMatchingPath != null;
            Debug.Assert(!allowProxySelection || longestMatchingPath.Count <= 0, $"{nameof(longestMatchingPath)} should be empty");
            int longestContiguousMarkerPathMatch = 0;
            int currentlyLongestContiguousMarkerPathMatch = 0;

            if (allowProxySelection && s_SkippedScopesCache.Length < sampleIndexPath.Capacity)
            {
                s_SkippedScopesCache = new RawSampleIterationInfo[sampleIndexPath.Capacity];
            }

            var skippedScopes = s_SkippedScopesCache;
            var skippedScopesCount = 0;
            while (sampleIndex <= lastSampleInScope && partOfThePath < pathLength && (specificRawSampleIndexToFind <= 0 || sampleIndex <= specificRawSampleIndexToFind))
            {
                if (markerIdPathToMatch == null ||
                    markerIdPathToMatch[partOfThePath + skippedScopesCount] == iterator.GetSampleMarkerId(sampleIndex) ||
                    (sampleIdFitsMarkerPathIndex != null && sampleIdFitsMarkerPathIndex(sampleIndex, partOfThePath + skippedScopesCount, iterator)))
                {
                    if ((specificRawSampleIndexToFind >= 0 && sampleIndex == specificRawSampleIndexToFind) ||
                        (specificRawSampleIndexToFind < 0 && partOfThePath == pathLength - 1))
                    {
                        foundSample = true;
                        break;
                    }
                    sampleIndexPath.Add(sampleIndex);
                    lastSampleInScopeOfThePath[lastSampleInScopeOfThePathCount++] = sampleIndex + iterator.GetSampleChildrenCountRecursive(sampleIndex);
                    ++sampleIndex;
                    ++partOfThePath;
                    if (skippedScopesCount <= 0)
                        currentlyLongestContiguousMarkerPathMatch = partOfThePath;

                    if (partOfThePath + skippedScopesCount >= pathLength)
                    {
                        if (longestMatchingPath != null && longestContiguousMarkerPathMatch <= currentlyLongestContiguousMarkerPathMatch && longestMatchingPath.Count < sampleIndexPath.Count)
                        {
                            // store the longest matching path. this will be used as a proxy selection fallback.
                            longestMatchingPath.Clear();
                            longestMatchingPath.AddRange(sampleIndexPath);
                            longestContiguousMarkerPathMatch = currentlyLongestContiguousMarkerPathMatch;
                        }
                        if (skippedScopesCount > 0)
                        {
                            //skip the current scope
                            sampleIndex = lastSampleInScopeOfThePath[--lastSampleInScopeOfThePathCount] + 1;
                            sampleIndexPath.RemoveAt(--partOfThePath); // same as sampleIndexPath.Count - 1;
                        }
                        else
                            break;
                    }
                }
                else if (allowProxySelection && partOfThePath + skippedScopesCount < pathLength - 1 && longestContiguousMarkerPathMatch <= currentlyLongestContiguousMarkerPathMatch)
                {
                    //skip this part of the path and continue checking the current sample against the next marker in the path
                    skippedScopes[skippedScopesCount++] = new RawSampleIterationInfo { partOfThePath = partOfThePath, lastSampleIndexInScope = sampleIndex + iterator.GetSampleChildrenCountRecursive(sampleIndex) };
                }
                else
                {
                    // move past this sample and skip all children.
                    sampleIndex += 1 + iterator.GetSampleChildrenCountRecursive(sampleIndex);
                }

                // if part of the path has already been "Stepped into", check if iterating means we've stepped out of current scope
                // No need to check partOfThePath == 0 because that scope is checked in the encompassing while
                while (lastSampleInScopeOfThePathCount > 0 && sampleIndex > lastSampleInScopeOfThePath[lastSampleInScopeOfThePathCount - 1] ||
                       allowProxySelection && skippedScopesCount > 0 && sampleIndex > skippedScopes[skippedScopesCount - 1].lastSampleIndexInScope)
                {
                    // we've stepped out of the current scope, unwind.

                    if (skippedScopesCount > 0 && skippedScopes[skippedScopesCount - 1].partOfThePath >= partOfThePath)
                    {
                        // if there are skippedScopes belonging to the current part of the path, unskip these first
                        sampleIndex = skippedScopes[--skippedScopesCount].lastSampleIndexInScope + 1;
                    }
                    else
                    {
                        if (longestMatchingPath != null && longestContiguousMarkerPathMatch <= currentlyLongestContiguousMarkerPathMatch && longestMatchingPath.Count < sampleIndexPath.Count)
                        {
                            // store the longest matching path. this will be used as a proxy selection fallback
                            longestMatchingPath.Clear();
                            longestMatchingPath.AddRange(sampleIndexPath);
                            longestContiguousMarkerPathMatch = currentlyLongestContiguousMarkerPathMatch;
                        }
                        sampleIndexPath.RemoveAt(--partOfThePath); // same as sampleIndexPath.Count - 1;
                        if (skippedScopesCount <= 0)
                            currentlyLongestContiguousMarkerPathMatch = partOfThePath;
                        sampleIndex = lastSampleInScopeOfThePath[--lastSampleInScopeOfThePathCount] + 1;
                    }
                }
            }
            if (foundSample)
            {
                if (string.IsNullOrEmpty(outName))
                {
                    outName = profilerSampleNameProvider.GetItemName(iterator, sampleIndex);
                }
                sampleIndexPath.Add(sampleIndex);
                if (longestMatchingPath != null)
                {
                    // The longest matching path is the full one
                    longestMatchingPath.Clear();
                    longestMatchingPath.AddRange(sampleIndexPath);
                }
                return sampleIndex;
            }
            return RawFrameDataView.invalidSampleIndex;
        }

        public void SetSelection(ProfilerTimeSampleSelection selection, int threadIndexInCurrentFrame, bool frameVertically)
        {
            m_SelectionPendingTransfer = selection;
            m_LocalSelectedItemMarkerIdPath.Clear();
            m_LocalSelectedItemMarkerIdPath.AddRange(selection.markerIdPath);
            m_ThreadIndexOfSelectionPendingTransfer = threadIndexInCurrentFrame;
            m_FrameSelectionVerticallyAfterTransfer = frameVertically;
            m_Scheduled_FrameSelectionVertically = false;
        }

        // Used for testing
        internal void GetSelectedSampleIdsForCurrentFrameAndView(ref List<int> ids)
        {
            if (m_SelectedEntry.IsValid())
            {
                // the native index is one lower than the raw index, because the thread root sample is not counted
                ids.Add(m_SelectedEntry.nativeIndex + 1);
            }
        }

        void UpdateSelectedObject(bool singleClick, bool doubleClick)
        {
            var obj = EditorUtility.InstanceIDToObject(m_SelectedEntry.instanceId);
            if (obj is Component)
                obj = ((Component)obj).gameObject;

            if (obj != null)
            {
                if (singleClick)
                {
                    EditorGUIUtility.PingObject(obj.GetInstanceID());
                }
                else if (doubleClick)
                {
                    var selection = new List<Object>();
                    selection.Add(obj);
                    Selection.objects = selection.ToArray();
                }
            }
        }

        public override void Clear()
        {
            InitializeNativeTimeline();
            m_LastSelectedFrameIndex = -1;
            for (int i = 0; i < m_Groups.Count; i++)
                m_Groups[i].Clear();
        }

        public void ClearSelection()
        {
            // in case the Selection is cleared in the same frame it was set in, drop the pending transfer
            m_SelectionPendingTransfer = null;
            m_LocalSelectedItemMarkerIdPath.Clear();
            m_ThreadIndexOfSelectionPendingTransfer = FrameDataView.invalidThreadIndex;
            m_FrameSelectionVerticallyAfterTransfer = false;
            m_Scheduled_FrameSelectionVertically = false;
            if (m_SelectedEntry.IsValid())
            {
                m_SelectedEntry.Reset();
                cpuModule.ClearSelection();
            }
            m_RangeSelection.active = false;
        }

        internal void FrameThread(int threadIndex)
        {
            PerformFrameSelected(0, false, false, true, threadIndex);
        }

        void PerformFrameAll(float frameMS)
        {
            PerformFrameSelected(frameMS, false, true);
        }

        void PerformFrameSelected(float frameMS, bool verticallyFrameSelected = true, bool hFrameAll = false, bool keepHorizontalZoomLevel = false, int verticallyFrameThreadIndex = FrameDataView.invalidThreadIndex)
        {
            float t;
            float dt;

            if (hFrameAll)
            {
                t = 0.0f;
                dt = frameMS;
            }
            else if (m_RangeSelection.active)
            {
                t = m_RangeSelection.startTime;
                dt = m_RangeSelection.duration;
            }
            else
            {
                t = m_SelectedEntry.time;
                dt = m_SelectedEntry.duration;
                if (m_SelectedEntry.instanceCountForFrame <= 0)
                {
                    t = 0.0f;
                    dt = frameMS;
                }
            }
            if (keepHorizontalZoomLevel)
            {
                // center the selected time
                t += dt * 0.5f;
                // take current zoom width in both directions
                dt = m_TimeArea.shownAreaInsideMargins.width * 0.5f;
                m_TimeArea.SetShownHRangeInsideMargins(t - dt, t + dt);
            }
            else
            {
                m_TimeArea.SetShownHRangeInsideMargins(t - dt * 0.2f, t + dt * 1.2f);
            }

            float yMinPosition = -1;
            float yMaxPosition = -1;
            if (verticallyFrameThreadIndex != FrameDataView.invalidThreadIndex)
            {
                // this overrides selection framing
                verticallyFrameSelected = false;
                ThreadInfo focusedThread = null;
                float yOffsetFromTop = 0;
                foreach (var group in m_Groups)
                {
                    foreach (var thread in group.threads)
                    {
                        if (thread.threadIndex == verticallyFrameThreadIndex)
                        {
                            focusedThread = thread;
                            break;
                        }
                        yOffsetFromTop += thread.height;
                    }

                    if (focusedThread != null)
                        break;
                }
                yMinPosition = yOffsetFromTop;
                yMaxPosition = yOffsetFromTop;
            }

            // [Case 1248631] The Analyzer may set m_SelectedEntry via reflection whilst m_SelectedThread is not assigned until later in DoProfilerFrame. Therefore it's possible we get here with a null m_SelectedThread.
            if (m_SelectedEntry.instanceCountForFrame >= 0 && verticallyFrameSelected && m_SelectedThread != null)
            {
                if (m_SelectedEntry.relativeYPos > m_SelectedThread.height)
                {
                    ThreadInfo selectedThread = null;
                    foreach (var group in m_Groups)
                    {
                        foreach (var thread in group.threads)
                        {
                            if (thread.threadIndex == m_SelectedEntry.threadIndex)
                            {
                                selectedThread = thread;
                                break;
                            }
                        }

                        if (selectedThread != null)
                            break;
                    }

                    if (selectedThread != null)
                    {
                        selectedThread.linesToDisplay = CalculateLineCount(m_SelectedEntry.relativeYPos + k_LineHeight * 2);
                        RepaintProfilerWindow();
                    }
                }

                yMinPosition = m_SelectedThreadYRange + m_SelectedEntry.relativeYPos - k_LineHeight;
                yMaxPosition = m_SelectedThreadYRange + m_SelectedEntry.relativeYPos;
            }

            if (yMinPosition >= 0 && yMaxPosition >= 0)
            {
                float yMin = m_TimeArea.shownArea.y;
                float yMax = yMin + m_TimeArea.shownArea.height;

                if (yMinPosition < m_TimeArea.shownAreaInsideMargins.yMin)
                {
                    yMin = yMinPosition;
                    yMax = Mathf.Min(yMin + m_TimeArea.shownArea.height, m_TimeArea.vBaseRangeMax);
                }

                if (yMaxPosition > m_TimeArea.shownAreaInsideMargins.yMax - m_TimeArea.hSliderHeight)
                {
                    yMax = yMaxPosition + m_TimeArea.hSliderHeight;
                    yMin = Mathf.Max(yMax - m_TimeArea.shownArea.height, m_TimeArea.vBaseRangeMin);
                }

                m_TimeArea.SetShownVRangeInsideMargins(yMin, yMax);
            }
        }

        void HandleFrameSelected(float frameMS)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
            {
                if (evt.commandName == EventCommandNames.FrameSelected)
                {
                    bool execute = evt.type == EventType.ExecuteCommand;
                    if (execute)
                        PerformFrameSelected(frameMS, true);
                    evt.Use();

                    m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.FrameSelection;
                }
            }

            if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.A)
            {
                PerformFrameAll(frameMS);
                evt.Use();
            }
        }

        void DoProfilerFrame(int currentFrameIndex, int currentFrameIndexOffset, Rect fullRect, bool ghost, float offset, float scaleForThreadHeight)
        {
            var frameIndex = currentFrameIndex + currentFrameIndexOffset;
            float y = fullRect.y;

            // the unscaled y value equating to the timeline Y range value
            float rangeY = 0;
            foreach (var groupInfo in m_Groups)
            {
                Rect r = fullRect;
                var expanded = groupInfo.expanded.value;
                if (expanded && groupInfo.threads.Count > 0)
                {
                    y += groupInfo.height;
                    rangeY += groupInfo.height;
                }

                // When group is not expanded its header still occupies at least groupInfo.height
                var notExpandedLeftOverY = groupInfo.height;

                var groupThreadCount = groupInfo.threads.Count;
                foreach (var threadInfo in groupInfo.threads)
                {
                    r.y = y;
                    r.height = expanded ? threadInfo.height * scaleForThreadHeight : Math.Max(groupInfo.height / groupThreadCount, k_ThreadMinHeightCollapsed);

                    var threadIndex = threadInfo.threadIndices[k_MaxNeighborFrames + currentFrameIndexOffset];
                    // Draw only threads that belong to the current frame
                    if (threadIndex != FrameDataView.invalidThreadIndex)
                    {
                        var tr = r;
                        tr.y -= fullRect.y;
                        if ((tr.yMin < m_TimeArea.shownArea.yMax && tr.yMax > m_TimeArea.shownArea.yMin)
                            // if there is a pending selection to be transfered to this thread, do process it.
                            || (m_SelectionPendingTransfer != null && m_ThreadIndexOfSelectionPendingTransfer == threadIndex))
                        {
                            DoNativeProfilerTimeline(r, frameIndex, threadIndex, offset, ghost, scaleForThreadHeight);
                        }

                        // Save the y pos and height of the selected thread each time we draw, since it can change
                        bool containsSelected = m_SelectedEntry.IsValid() && (m_SelectedEntry.frameId == frameIndex) && (m_SelectedEntry.threadIndex == threadIndex);
                        if (containsSelected)
                        {
                            m_SelectedThreadY = y;
                            m_SelectedThreadYRange = rangeY;
                            m_SelectedThread = threadInfo;
                        }
                    }

                    y += r.height;
                    rangeY += r.height;
                    notExpandedLeftOverY -= r.height;
                }

                // Align next thread with the next group
                if (notExpandedLeftOverY > 0)
                {
                    y += notExpandedLeftOverY;
                    rangeY += notExpandedLeftOverY;
                }
            }
        }

        void DoFlowEvents(int currentFrameIndex, int firstDrawnFrameIndex, int lastDrawnFrameIndex, Rect fullRect, float scaleForThreadHeight)
        {
            // Do nothing when flow visualization is disabled.
            if ((cpuModule.ViewOptions & CPUOrGPUProfilerModule.ProfilerViewFilteringOptions.ShowExecutionFlow) == 0)
                return;

            // Only redraw on repaint event.
            if (m_TimeArea == null || Event.current.type != EventType.Repaint)
                return;

            // TODO: Only update cache on frame change
            m_DrawnFlowIndicatorsCache.Clear();

            bool hasSelectedSampleWithFlowEvents = (m_SelectedEntry.FlowEvents.Count > 0) && SelectedSampleIsVisible(firstDrawnFrameIndex, lastDrawnFrameIndex);
            FlowLinesDrawer activeFlowLinesDrawer = (hasSelectedSampleWithFlowEvents) ? new FlowLinesDrawer() : null;
            ForEachThreadInEachGroup(fullRect, scaleForThreadHeight, (groupInfo, threadInfo, threadRect) =>
            {
                var groupIsExpanded = groupInfo.expanded.value;
                if (hasSelectedSampleWithFlowEvents)
                {
                    ProcessActiveFlowEventsOnThread(ref activeFlowLinesDrawer, threadInfo, threadRect, fullRect, currentFrameIndex, groupIsExpanded);
                }
                else
                {
                    DrawIndicatorsForAllFlowEventsInFrameOnThread(currentFrameIndex, threadInfo, threadRect, fullRect, groupIsExpanded, hasSelectedSampleWithFlowEvents);
                }
            });

            activeFlowLinesDrawer?.Draw();
        }

        void DrawIndicatorsForAllFlowEventsInFrameOnThread(int currentFrameIndex, ThreadInfo threadInfo, Rect threadRect, Rect fullRect, bool groupIsExpanded, bool hasSelectedEntryWithFlowEvents)
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(currentFrameIndex, threadInfo.threadIndex))
            {
                if (!frameData.valid)
                    return;

                frameData.GetFlowEvents(m_CachedThreadFlowEvents);

                var localViewport = new Rect(Vector2.zero, fullRect.size);
                foreach (var flowEvent in m_CachedThreadFlowEvents)
                {
                    if (flowEvent.ParentSampleIndex != 0)
                    {
                        // A sample can have multiple flow events. Check an indicator hasn't already been drawn for this sample on this thread.
                        var indicatorCacheValue = new DrawnFlowIndicatorCacheValue()
                        {
                            threadId = threadInfo.threadIndex,
                            markerId = flowEvent.ParentSampleIndex,
                            flowEventType = flowEvent.FlowEventType
                        };
                        if (!m_DrawnFlowIndicatorsCache.Contains(indicatorCacheValue))
                        {
                            var sampleRect = RectForSampleOnFrameInThread(flowEvent.ParentSampleIndex, currentFrameIndex, threadInfo.threadIndex, threadRect, 0f, m_TimeArea.shownArea, (groupIsExpanded == false));
                            if (sampleRect.Overlaps(localViewport))
                            {
                                FlowIndicatorDrawer.DrawFlowIndicatorForFlowEvent(flowEvent, sampleRect);
                                m_DrawnFlowIndicatorsCache.Add(indicatorCacheValue);
                            }
                        }
                    }
                }
            }
        }

        void ProcessActiveFlowEventsOnThread(ref FlowLinesDrawer flowLinesDrawer, ThreadInfo threadInfo, Rect threadRect, Rect fullRect, int currentFrameIndex, bool groupIsExpanded)
        {
            if (threadInfo.ActiveFlowEvents == null)
                return;

            foreach (var flowEventData in threadInfo.ActiveFlowEvents)
            {
                var flowEvent = flowEventData.flowEvent;
                var flowEventFrameIndex = flowEventData.frameIndex;
                var flowEventThreadIndex = flowEventData.threadIndex;
                var timeOffset = ProfilerFrameTimingUtility.TimeOffsetBetweenFrames(currentFrameIndex, flowEventData.frameIndex);
                var sampleRect = RectForSampleOnFrameInThread(flowEvent.ParentSampleIndex, flowEventFrameIndex, flowEventThreadIndex, threadRect, timeOffset, m_TimeArea.shownArea, (groupIsExpanded == false));

                // A flow event can have no parent sample if it is not enclosed within a PROFILER_AUTO scope.
                if (flowEventData.hasParentSampleIndex)
                {
                    // Add flow events to the 'flow lines drawer' for drawing later.
                    bool isSelectedSample = false;
                    if (!flowLinesDrawer.hasSelectedEvent)
                    {
                        isSelectedSample = (m_SelectedEntry.threadIndex == threadInfo.threadIndex) && (m_SelectedEntry.frameId == flowEventFrameIndex) && m_SelectedEntry.FlowEvents.Contains(flowEvent);
                    }
                    flowLinesDrawer.AddFlowEvent(flowEventData, sampleRect, isSelectedSample);

                    // A sample can have multiple flow events. Check an indicator hasn't already been drawn for this sample on this thread.
                    var indicatorCacheValue = new DrawnFlowIndicatorCacheValue()
                    {
                        threadId = threadInfo.threadIndex,
                        markerId = flowEvent.ParentSampleIndex,
                        flowEventType = flowEvent.FlowEventType
                    };
                    if (!m_DrawnFlowIndicatorsCache.Contains(indicatorCacheValue))
                    {
                        // Draw indicator for this active flow event.
                        var localViewport = new Rect(Vector2.zero, fullRect.size);
                        if (sampleRect.Overlaps(localViewport))
                        {
                            FlowIndicatorDrawer.DrawFlowIndicatorForFlowEvent(flowEvent, sampleRect);
                            m_DrawnFlowIndicatorsCache.Add(indicatorCacheValue);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterate over each group's threads and execute the provided <paramref name="action"/> for each thread. Passes the thread's rect to the provided action, calculated by summing all previous thread rects.
        /// </summary>
        /// <param name="fullRect"></param>
        /// <param name="scaleForThreadHeight"></param>
        /// <param name="action"></param>
        void ForEachThreadInEachGroup(Rect fullRect, float scaleForThreadHeight, Action<GroupInfo, ThreadInfo, Rect> action)
        {
            float y = fullRect.y;
            foreach (var groupInfo in m_Groups)
            {
                var threadRect = fullRect;
                var groupIsExpanded = groupInfo.expanded.value;
                if (groupIsExpanded && groupInfo.threads.Count > 0)
                {
                    y += groupInfo.height;
                }

                // When group is not expanded its header still occupies at least groupInfo.height.
                var notExpandedLeftOverY = groupInfo.height;
                var groupThreadCount = groupInfo.threads.Count;
                foreach (var threadInfo in groupInfo.threads)
                {
                    threadRect.y = y;
                    threadRect.height = groupIsExpanded ? threadInfo.height * scaleForThreadHeight : Math.Max(groupInfo.height / groupThreadCount, k_ThreadMinHeightCollapsed);

                    action(groupInfo, threadInfo, threadRect);

                    y += threadRect.height;
                    notExpandedLeftOverY -= threadRect.height;
                }

                // Align next thread with the next group
                if (notExpandedLeftOverY > 0)
                {
                    y += notExpandedLeftOverY;
                }
            }
        }

        Rect RectForSampleOnFrameInThread(int sampleIndex, int frameIndex, int threadIndex, Rect threadRect, float timeOffset, Rect shownAreaRect, bool isGroupCollapsed)
        {
            var positionInfoArgs = RawPositionInfoForSample(sampleIndex, frameIndex, threadIndex, threadRect, timeOffset, shownAreaRect);
            CorrectRawPositionInfo(isGroupCollapsed, threadRect, ref positionInfoArgs);

            var sampleRect = new Rect(new Vector2(positionInfoArgs.out_Position.x, positionInfoArgs.out_Position.y), positionInfoArgs.out_Size);
            return sampleRect;
        }

        NativeProfilerTimeline_GetEntryPositionInfoArgs RawPositionInfoForSample(int sampleIndex, int frameIndex, int threadIndex, Rect threadRect, float timeOffset, Rect shownAreaRect)
        {
            var positionInfoArgs = new NativeProfilerTimeline_GetEntryPositionInfoArgs();
            positionInfoArgs.Reset();
            positionInfoArgs.frameIndex = frameIndex;
            positionInfoArgs.threadIndex = threadIndex;
            positionInfoArgs.sampleIndex = sampleIndex;
            positionInfoArgs.timeOffset = timeOffset;
            positionInfoArgs.threadRect = threadRect;
            positionInfoArgs.shownAreaRect = shownAreaRect;

            NativeProfilerTimeline.GetEntryPositionInfo(ref positionInfoArgs);

            return positionInfoArgs;
        }

        // NativeProfilerTimeline.GetEntryPositionInfo appears to return rects relative to the thread rect. It also appears to not account for collapsed groups and threads.
        static void CorrectRawPositionInfo(bool isGroupCollapsed, Rect threadRect, ref NativeProfilerTimeline_GetEntryPositionInfoArgs positionInfo)
        {
            // Offset the vertical position by the thread rect's vertical position.
            positionInfo.out_Position.y += threadRect.y;

            // If the group is collapsed, adjust the height to the thread rect's height.
            if (isGroupCollapsed)
            {
                positionInfo.out_Size.y = threadRect.height;
            }

            // If the rect is below the bottom of the thread rect (i.e. the thread collapsed), clamp the rect to the bottom of the thread rect.
            var positionInfoMaxY = positionInfo.out_Position.y + positionInfo.out_Size.y;
            if (positionInfoMaxY > threadRect.yMax)
            {
                positionInfo.out_Position.y = threadRect.yMax;
                positionInfo.out_Size.y = 0f;
            }
        }

        bool SelectedSampleIsVisible(int firstDrawnFrameIndex, int lastDrawnFrameIndex)
        {
            return ((m_SelectedEntry.frameId >= firstDrawnFrameIndex) && (m_SelectedEntry.frameId <= lastDrawnFrameIndex));
        }

        static RawFrameDataView GetFrameDataForThreadId(int frameIndex, int threadIndex, ulong threadId)
        {
            RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
            // Check for valid data
            if (!frameData.valid)
                return null;

            // If threadId matches for the same index we found our thread.
            // (When new thread starts it might change the order of threads)
            if (frameData.threadId == threadId)
                return frameData;

            // Overwise do a scan across all threads matching threadId.
            frameData.Dispose();

            // Skip main thread which is always 0
            for (var i = 1;; ++i)
            {
                // Skip already inspected thread
                if (threadIndex == i)
                    continue;

                frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, i);
                // Check is this is the last thread.
                if (!frameData.valid)
                    return null;

                // Check if we found correct thread.
                if (frameData.threadId == threadId)
                    return frameData;

                // Continue lookup and dispose nonmatching thread.
                frameData.Dispose();
            }
        }

        static void GetSampleTimeInNeighboringFrames(bool previousFrames, int sampleIndex, int markerId, int frameIndex, int threadIndex, ulong threadId, List<int> slicedSampleIndexList, ref ulong totalAsyncDurationNs, ref int totalAsyncFramesCount)
        {
            var sampleDepth = slicedSampleIndexList.IndexOf(sampleIndex);
            if (sampleDepth == -1)
                return;

            var startFrame = previousFrames ? frameIndex - 1 : frameIndex + 1;
            var frameDirection = previousFrames ? -1 : 1;
            for (var i = startFrame;; i += frameDirection)
            {
                // Filter out frames which are beyond visible range
                if (i < ProfilerDriver.firstFrameIndex || i > ProfilerDriver.lastFrameIndex)
                    break;

                var startedPreviousFrame = false;
                var timeNs = 0UL;
                using (var frameData = GetFrameDataForThreadId(i, threadIndex, threadId))
                {
                    // Thread not found
                    if (frameData == null)
                        return;

                    var sampleCount = frameData.sampleCount;
                    // Check for root only sample
                    if (sampleCount == 1)
                        return;

                    // Sliced samples must have matching hierarchies on both sides of frame slice.
                    // It is expensive to reconstruct hierarchy back from the raw event data, thus we store it in the frame data.
                    // The list represents the sample hierarchy active at the frame boundary.
                    if (previousFrames)
                        frameData.GetSamplesContinuedInNextFrame(slicedSampleIndexList);
                    else
                        frameData.GetSamplesStartedInPreviousFrame(slicedSampleIndexList);
                    if (sampleDepth >= slicedSampleIndexList.Count)
                        return;

                    var otherFrameSampleIndex = slicedSampleIndexList[sampleDepth];
                    // Verify marker is the same
                    if (frameData.GetSampleMarkerId(otherFrameSampleIndex) != markerId)
                        return;

                    // Sample found - catch the duration
                    timeNs = frameData.GetSampleTimeNs(otherFrameSampleIndex);

                    // Check out if sample started even earlier
                    if (previousFrames)
                        frameData.GetSamplesStartedInPreviousFrame(slicedSampleIndexList);
                    else
                        frameData.GetSamplesContinuedInNextFrame(slicedSampleIndexList);
                    startedPreviousFrame = slicedSampleIndexList.Contains(otherFrameSampleIndex);
                }

                if (timeNs != 0)
                {
                    totalAsyncDurationNs += timeNs;
                    totalAsyncFramesCount++;
                }
                if (!startedPreviousFrame)
                    break;
            }
        }

        static readonly List<int> s_CachedSamplesStartedInPreviousFrame = new List<int>();
        static readonly List<int> s_CachedSamplesContinuedInNextFrame = new List<int>();

        internal static void CalculateTotalAsyncDuration(int selectedSampleIndex, int frameIndex, int selectedThreadIndex, out string selectedThreadName, out ulong totalAsyncDurationNs, out int totalAsyncFramesCount)
        {
            ulong selectedThreadId;
            int selectedMarkerId;
            using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, selectedThreadIndex))
            {
                selectedThreadId = frameData.threadId;
                selectedThreadName = frameData.threadName;
                selectedMarkerId = frameData.GetSampleMarkerId(selectedSampleIndex);
                frameData.GetSamplesStartedInPreviousFrame(s_CachedSamplesStartedInPreviousFrame);
                frameData.GetSamplesContinuedInNextFrame(s_CachedSamplesContinuedInNextFrame);
                totalAsyncDurationNs = frameData.GetSampleTimeNs(selectedSampleIndex);
            }
            totalAsyncFramesCount = 1;

            GetSampleTimeInNeighboringFrames(true, selectedSampleIndex, selectedMarkerId, frameIndex, selectedThreadIndex, selectedThreadId, s_CachedSamplesStartedInPreviousFrame, ref totalAsyncDurationNs, ref totalAsyncFramesCount);
            GetSampleTimeInNeighboringFrames(false, selectedSampleIndex, selectedMarkerId, frameIndex, selectedThreadIndex, selectedThreadId, s_CachedSamplesContinuedInNextFrame, ref totalAsyncDurationNs, ref totalAsyncFramesCount);
        }

        void DoSelectionTooltip(int frameIndex, Rect fullRect)
        {
            // Draw selected tooltip
            if (!m_SelectedEntry.IsValid() || m_SelectedEntry.frameId != frameIndex)
                return;

            bool hasCallStack = m_SelectedEntry.hasCallstack;

            if (callStackNeedsRegeneration || m_SelectedEntry.cachedSelectionTooltipContent == null)
            {
                string durationString = UnityString.Format(m_SelectedEntry.duration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.duration);

                System.Text.StringBuilder text = new System.Text.StringBuilder();
                if (m_SelectedEntry.nonProxyName != null)
                {
                    var diff = Math.Abs(m_SelectedEntry.nonProxyDepthDifference);
                    text.AppendFormat(
                        BaseStyles.proxySampleMessage,
                        m_SelectedEntry.nonProxyName, diff,
                        diff == 1 ? BaseStyles.proxySampleMessageScopeSingular : BaseStyles.proxySampleMessageScopePlural);
                    text.Append(BaseStyles.proxySampleMessagePart2TimelineView);
                }
                text.Append(UnityString.Format("{0}\n{1}", m_SelectedEntry.name, durationString));

                // Calculate total time of the sample across visible frames
                var selectedThreadIndex = m_SelectedEntry.threadIndex;
                int selectedSampleIndex = m_SelectedEntry.nativeIndex + 1;
                string selectedThreadName;
                ulong totalAsyncDurationNs;
                int totalAsyncFramesCount;

                // Check if sample is sliced and started in previous frames
                CalculateTotalAsyncDuration(selectedSampleIndex, frameIndex, selectedThreadIndex, out selectedThreadName, out totalAsyncDurationNs, out totalAsyncFramesCount);

                // Add total time to the tooltip
                if (totalAsyncFramesCount > 1)
                {
                    var totalAsyncDuration = totalAsyncDurationNs * 1e-6f;
                    var totalAsyncDurationString = UnityString.Format(totalAsyncDuration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", totalAsyncDuration);
                    text.Append(string.Format(styles.localizedStringTotalAcrossFrames, totalAsyncDurationString, totalAsyncFramesCount, selectedThreadName));
                }

                // Show total duration if more than one instance
                if (m_SelectedEntry.instanceCountForThread > 1 || m_SelectedEntry.instanceCountForFrame > 1)
                {
                    text.Append(styles.localizedStringTotalAcumulatedTime);

                    if (m_SelectedEntry.instanceCountForThread > 1)
                    {
                        string totalDurationForThreadString = UnityString.Format(m_SelectedEntry.totalDurationForThread >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.totalDurationForThread);
                        text.Append(string.Format(styles.localizedStringTotalInThread, totalDurationForThreadString, m_SelectedEntry.instanceCountForThread, selectedThreadName));
                    }

                    if (m_SelectedEntry.instanceCountForFrame > m_SelectedEntry.instanceCountForThread)
                    {
                        string totalDurationForFrameString = UnityString.Format(m_SelectedEntry.totalDurationForFrame >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.totalDurationForFrame);
                        text.Append(string.Format(styles.localizedStringTotalInFrame, totalDurationForFrameString, m_SelectedEntry.instanceCountForFrame, m_SelectedEntry.threadCount));
                    }
                }

                if (m_SelectedEntry.metaData.Length > 0)
                {
                    text.Append(string.Format("\n{0}", m_SelectedEntry.metaData));
                }

                if (hasCallStack)
                {
                    using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, m_SelectedEntry.threadIndex))
                    {
                        var callStack = new List<ulong>();
                        frameData.GetSampleCallstack(m_SelectedEntry.nativeIndex + 1, callStack);
                        CompileCallStack(text, callStack, frameData);
                    }
                }
                m_SelectedEntry.cachedSelectionTooltipContent = new GUIContent(text.ToString());
            }

            float selectedThreadYOffset = fullRect.y + m_SelectedThreadY;
            float selectedY = selectedThreadYOffset + m_SelectedEntry.relativeYPos;
            float maxYPosition = Mathf.Max(Mathf.Min(fullRect.yMax, selectedThreadYOffset + m_SelectedThread.height), fullRect.y);

            // calculate how much of the line height is visible (needed for calculating the offset of the tooltip when flipping)
            float selectedLineHeightVisible = Mathf.Clamp(maxYPosition - (selectedY - k_LineHeight), 0, k_LineHeight);

            // keep the popup within the drawing area and thread rect
            selectedY = Mathf.Clamp(selectedY, fullRect.y, maxYPosition);

            float x = m_TimeArea.TimeToPixel(m_SelectedEntry.time + m_SelectedEntry.duration * 0.5f, fullRect);
            ShowLargeTooltip(new Vector2(x, selectedY), fullRect, m_SelectedEntry.cachedSelectionTooltipContent, m_SelectedEntry.sampleStack, selectedLineHeightVisible,
                frameIndex, m_SelectedEntry.threadIndex, hasCallStack, ref m_SelectedEntry.downwardsZoomableAreaSpaceNeeded, m_SelectedEntry.nonProxyDepthDifference != 0 ? m_SelectedEntry.nativeIndex + 1 : RawFrameDataView.invalidSampleIndex);
        }

        void PrepareTicks()
        {
            m_HTicks.SetRanges(m_TimeArea.shownArea.xMin, m_TimeArea.shownArea.xMax, m_TimeArea.drawRect.xMin, m_TimeArea.drawRect.xMax);
            m_HTicks.SetTickStrengths(TimeArea.kTickRulerDistMin, TimeArea.kTickRulerDistFull, true);
        }

        void DrawGrid(Rect rect, float frameTime)
        {
            if (m_TimeArea == null || Event.current.type != EventType.Repaint)
                return;

            GUI.BeginClip(rect);
            rect.x = rect.y = 0;

            Color tickColor = styles.timelineTick.normal.textColor;
            tickColor.a = 0.1f;

            HandleUtility.ApplyWireMaterial();
            if (Application.platform == RuntimePlatform.WindowsEditor)
                GL.Begin(GL.QUADS);
            else
                GL.Begin(GL.LINES);

            PrepareTicks();

            // Draw tick markers of various sizes
            for (int l = 0; l < m_HTicks.tickLevels; l++)
            {
                var strength = m_HTicks.GetStrengthOfLevel(l) * .9f;
                if (strength > TimeArea.kTickRulerFatThreshold)
                {
                    var ticks = m_HTicks.GetTicksAtLevel(l, true);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        // Draw line
                        var time = ticks[i];
                        var x = m_TimeArea.TimeToPixel(time, rect);
                        TimeArea.DrawVerticalLineFast(x, 0, rect.height, tickColor);
                    }
                }
            }

            // Draw frame start and end delimiters
            TimeArea.DrawVerticalLineFast(m_TimeArea.TimeToPixel(0, rect), 0, rect.height, styles.frameDelimiterColor);
            TimeArea.DrawVerticalLineFast(m_TimeArea.TimeToPixel(frameTime, rect), 0, rect.height, styles.frameDelimiterColor);

            GL.End();

            GUI.EndClip();
        }

        void DoTimeRulerGUI(Rect timeRulerRect, float sideWidth, float frameTime)
        {
            // m_TimeArea shouldn't ever be null when this method is called, but just to make sure in case the call to this changes.
            if (Event.current.type != EventType.Repaint || m_TimeArea == null)
                return;

            Rect sidebarLeftOfTimeRulerRect = new Rect(timeRulerRect.x - sideWidth, timeRulerRect.y, sideWidth, k_LineHeight);
            timeRulerRect.width -= m_TimeArea.vSliderWidth;
            Rect spaceRightOftimeRulerRect = new Rect(timeRulerRect.xMax, timeRulerRect.y, m_TimeArea.vSliderWidth, timeRulerRect.height);

            styles.leftPane.Draw(sidebarLeftOfTimeRulerRect, GUIContent.none, false, false, false, false);

            styles.leftPane.Draw(spaceRightOftimeRulerRect, GUIContent.none, false, false, false, false);

            GUI.BeginClip(timeRulerRect);
            timeRulerRect.x = timeRulerRect.y = 0;

            GUI.Box(timeRulerRect, GUIContent.none, styles.leftPane);

            var baseColor = styles.timelineTick.normal.textColor;
            baseColor.a *= 0.75f;

            PrepareTicks();

            // Tick lines
            if (Event.current.type == EventType.Repaint)
            {
                HandleUtility.ApplyWireMaterial();
                if (Application.platform == RuntimePlatform.WindowsEditor)
                    GL.Begin(GL.QUADS);
                else
                    GL.Begin(GL.LINES);

                for (int l = 0; l < m_HTicks.tickLevels; l++)
                {
                    var strength = m_HTicks.GetStrengthOfLevel(l) * .8f;
                    if (strength < 0.1f)
                        continue;
                    var ticks = m_HTicks.GetTicksAtLevel(l, true);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        // Draw line
                        var time = ticks[i];
                        var x = m_TimeArea.TimeToPixel(time, timeRulerRect);
                        var height = timeRulerRect.height * Mathf.Min(1, strength) * TimeArea.kTickRulerHeightMax;
                        var color = new Color(1, 1, 1, strength / TimeArea.kTickRulerFatThreshold) * baseColor;
                        TimeArea.DrawVerticalLineFast(x, timeRulerRect.height - height + 0.5f, timeRulerRect.height - 0.5f, color);
                    }
                }

                GL.End();
            }

            // Tick labels
            var labelWidth = k_TickLabelSeparation;
            int labelLevel = m_HTicks.GetLevelWithMinSeparation(labelWidth);
            float[] labelTicks = m_HTicks.GetTicksAtLevel(labelLevel, false);
            for (int i = 0; i < labelTicks.Length; i++)
            {
                var time = labelTicks[i];
                float labelpos = Mathf.Floor(m_TimeArea.TimeToPixel(time, timeRulerRect));
                string label = FormatTickLabel(time, labelLevel);
                GUI.Label(new Rect(labelpos + 3, -3, labelWidth, 20), label, styles.timelineTick);
            }

            // Outside current frame coloring
            DrawOutOfRangeOverlay(timeRulerRect, frameTime);

            // Range selection coloring
            DrawRangeSelectionOverlay(timeRulerRect);

            GUI.EndClip();
        }

        string FormatTickLabel(float time, int level)
        {
            string format = k_TickFormatMilliseconds;
            var period = m_HTicks.GetPeriodOfLevel(level);
            var log10 = Mathf.FloorToInt(Mathf.Log10(period));
            if (log10 >= 3)
            {
                time /= 1000;
                format = k_TickFormatSeconds;
            }

            return UnityString.Format(format, time.ToString("N" + Mathf.Max(0, -log10), CultureInfo.InvariantCulture.NumberFormat));
        }

        void DrawOutOfRangeOverlay(Rect rect, float frameTime)
        {
            var color = styles.outOfRangeColor;
            var lineColor = styles.frameDelimiterColor;

            var frameStartPixel = m_TimeArea.TimeToPixel(0f, rect);
            var frameEndPixel = m_TimeArea.TimeToPixel(frameTime, rect);

            // Rect shaded shape drawn before selected frame
            if (frameStartPixel > rect.xMin)
            {
                var startRect = Rect.MinMaxRect(rect.xMin, rect.yMin, Mathf.Min(frameStartPixel, rect.xMax), rect.yMax);
                EditorGUI.DrawRect(startRect, color);
                TimeArea.DrawVerticalLine(startRect.xMax, startRect.yMin, startRect.yMax, lineColor);
            }

            // Rect shaded shape drawn after selected frame
            if (frameEndPixel < rect.xMax)
            {
                var endRect = Rect.MinMaxRect(Mathf.Max(frameEndPixel, rect.xMin), rect.yMin, rect.xMax, rect.yMax);
                EditorGUI.DrawRect(endRect, color);
                TimeArea.DrawVerticalLine(endRect.xMin, endRect.yMin, endRect.yMax, lineColor);
            }
        }

        void DrawRangeSelectionOverlay(Rect rect)
        {
            if (!m_RangeSelection.active)
                return;

            var startPixel = m_TimeArea.TimeToPixel(m_RangeSelection.startTime, rect);
            var endPixel = m_TimeArea.TimeToPixel(m_RangeSelection.endTime, rect);
            if (startPixel > rect.xMax || endPixel < rect.xMin)
                return;

            var selectionRect = Rect.MinMaxRect(Mathf.Max(rect.xMin, startPixel), rect.yMin, Mathf.Min(rect.xMax, endPixel), rect.yMax);
            EditorGUI.DrawRect(selectionRect, styles.rangeSelectionColor);

            // Duration label
            var labelText = UnityString.Format(k_TickFormatMilliseconds, m_RangeSelection.duration.ToString("N3", CultureInfo.InvariantCulture.NumberFormat));
            Chart.DoLabel(startPixel + (endPixel - startPixel) / 2, rect.yMin + 3, labelText, -0.5f);
        }

        void DoTimeArea()
        {
            int previousHotControl = GUIUtility.hotControl;

            // draw the time area
            m_TimeArea.BeginViewGUI();
            m_TimeArea.EndViewGUI();

            if (previousHotControl != GUIUtility.hotControl && GUIUtility.hotControl != 0)
                m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.PanningOrZooming;
        }

        void DoThreadSplitters(Rect fullThreadsRect, Rect fullThreadsRectWithoutSidebar, int frame, ThreadSplitterCommand command)
        {
            float headerHeight = 0;
            float threadHeight = 0;
            ThreadInfo lastThreadInLastExpandedGroup = null;
            GroupInfo lastExpandedGroup = null;
            foreach (var group in m_Groups)
            {
                headerHeight += group.height;

                switch (command)
                {
                    case ThreadSplitterCommand.HandleThreadSplitter:
                        // Another Splitter for the last thread of the previous group on the bottom line of this groups header
                        // the first group is always main and has no header so that case can be ignored
                        if (lastThreadInLastExpandedGroup != null && group.height > 0f)
                            HandleThreadSplitter(lastExpandedGroup, lastThreadInLastExpandedGroup, fullThreadsRect, headerHeight + threadHeight - lastThreadInLastExpandedGroup.height);
                        break;
                    case ThreadSplitterCommand.HandleThreadSplitterFoldoutButtons:
                    // nothing to do here
                    default:
                        break;
                }

                ThreadInfo lastThread = null;
                foreach (var thread in group.threads)
                {
                    if (group.expanded.value)
                    {
                        switch (command)
                        {
                            case ThreadSplitterCommand.HandleThreadSplitter:
                                HandleThreadSplitter(group, thread, fullThreadsRect, headerHeight + threadHeight);
                                HandleThreadSplitterFoldoutButtons(group, thread, fullThreadsRectWithoutSidebar, headerHeight + threadHeight, HandleThreadSplitterFoldoutButtonsCommand.OnlyHandleInput);
                                break;
                            case ThreadSplitterCommand.HandleThreadSplitterFoldoutButtons:
                                HandleThreadSplitterFoldoutButtons(group, thread, fullThreadsRectWithoutSidebar, headerHeight + threadHeight, HandleThreadSplitterFoldoutButtonsCommand.OnlyDraw);
                                break;
                            default:
                                break;
                        }
                    }

                    threadHeight += thread.height;
                    lastThread = thread;
                }

                lastThreadInLastExpandedGroup = group.expanded.value ? lastThread : lastThreadInLastExpandedGroup;
                lastExpandedGroup = group.expanded.value ? group : lastExpandedGroup;
            }
        }

        float CalculateMaxYPositionForThread(float lineCount, float yPositionOfFirstThread, float yOffsetForThisThread)
        {
            float threadHeight = CalculateThreadHeight(lineCount);
            threadHeight *= m_TimeArea.scale.y;
            yOffsetForThisThread *= m_TimeArea.scale.y;
            return yPositionOfFirstThread + scrollOffsetY + yOffsetForThisThread + threadHeight;
        }

        float CalculateThreadHeight(float lineCount)
        {
            // add a bit of extra height through kExtraHeightPerThread to give a sneak peek at the next line
            return lineCount * k_FullThreadLineHeight + k_ExtraHeightPerThread;
        }

        int CalculateLineCount(float threadHeight)
        {
            return Mathf.RoundToInt((threadHeight - k_ExtraHeightPerThread) / k_FullThreadLineHeight);
        }

        static bool CheckForExclusiveSplitterInput(ProcessedInputs inputs)
        {
            // check if there is a MouseDown event happening that is just moving a splitter and doing nothing else
            return inputs > 0 && (inputs & ProcessedInputs.SplitterMoving) > 0 && (inputs & ProcessedInputs.MouseDown) > 0 && inputs - (inputs & ProcessedInputs.SplitterMoving) - (inputs & ProcessedInputs.MouseDown) == 0;
        }

        void HandleThreadSplitter(GroupInfo group, ThreadInfo thread, Rect fullThreadsRect, float yOffsetForThisThread)
        {
            Rect splitterRect = new Rect(
                fullThreadsRect.x + 1,
                CalculateMaxYPositionForThread(thread.linesToDisplay, fullThreadsRect.y, yOffsetForThisThread) - (k_ThreadSplitterHandleSize / 2f),
                fullThreadsRect.width - 2,
                k_ThreadSplitterHandleSize);

            // Handle the mouse cursor look
            if (Event.current.type == EventType.Repaint)
            {
                // we're in Repaint so m_CurrentlyProcessedInputs is already filled with all the inputs since the last repaint
                if (CheckForExclusiveSplitterInput(m_CurrentlyProcessedInputs))
                {
                    // while dragging the splitter, the cursor should be MouseCursor.SplitResizeUpDown regardless of where the mouse is in the view
                    EditorGUIUtility.AddCursorRect(fullThreadsRect, MouseCursor.SplitResizeUpDown);
                }
                else if (fullThreadsRect.Contains(splitterRect.center) && ((m_CurrentlyProcessedInputs & ProcessedInputs.MouseDown) == 0 || (m_CurrentlyProcessedInputs - ProcessedInputs.MouseDown) > 0))
                {
                    //unless a splitter is getting dragged, only change the cursor if this splitter would be in view and no other input is occurring
                    EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.SplitResizeUpDown);
                }
            }

            // double clicking the splitter line resizes to fit
            if (splitterRect.Contains(Event.current.mousePosition) && Event.current.clickCount == 2 && Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                thread.linesToDisplay = thread.maxDepth;
                Event.current.Use();
            }

            int previousHotControl = GUIUtility.hotControl;

            float deltaY = EditorGUI.MouseDeltaReader(splitterRect, true).y;


            if (previousHotControl != GUIUtility.hotControl)
            {
                // the delta reader changed the hot control ...
                if (GUIUtility.hotControl != 0)
                {
                    // ... and took it
                    m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.SplitterMoving;
                    int defaultLineCountForThisThread = group.defaultLineCountPerThread * (group.name.Equals(k_MainGroupName) && thread.threadIndex == 0 ? 2 : 1);
                    m_MaxLinesToDisplayForTheCurrentlyModifiedSplitter = Mathf.Max(thread.linesToDisplay, Mathf.Max(thread.maxDepth, defaultLineCountForThisThread));
                }
                else
                {
                    // ... and released it
                    // so reset the lines to the nearest integer
                    thread.linesToDisplay = Mathf.Clamp(Mathf.RoundToInt(thread.linesToDisplay), 1, m_MaxLinesToDisplayForTheCurrentlyModifiedSplitter);
                    m_CurrentlyProcessedInputs -= m_CurrentlyProcessedInputs & (ProcessedInputs.MouseDown | ProcessedInputs.SplitterMoving);
                }
            }
            else if (CheckForExclusiveSplitterInput(m_LastRepaintProcessedInputs) && GUIUtility.hotControl != 0)
            {
                // continuous dragging
                m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.SplitterMoving;
            }

            if (deltaY != 0f)
            {
                thread.linesToDisplay = Mathf.Clamp((thread.linesToDisplay * k_FullThreadLineHeight + deltaY) / k_FullThreadLineHeight, 1, m_MaxLinesToDisplayForTheCurrentlyModifiedSplitter);
            }
        }

        void HandleThreadSplitterFoldoutButtons(GroupInfo group, ThreadInfo thread, Rect fullThreadsRectWithoutSidebar, float yOffsetForThisThread, HandleThreadSplitterFoldoutButtonsCommand command)
        {
            int roundedLineCount = Mathf.RoundToInt(thread.linesToDisplay);
            int defaultLineCountForThisThread = group.defaultLineCountPerThread * (group.name.Equals(k_MainGroupName) && thread.threadIndex == 0 ? 2 : 1);

            // only show the button if clicking it would change anything at all
            if (thread.maxDepth > defaultLineCountForThisThread || thread.maxDepth > roundedLineCount)
            {
                GUI.BeginClip(fullThreadsRectWithoutSidebar);
                fullThreadsRectWithoutSidebar.x = 0;
                fullThreadsRectWithoutSidebar.y = 0;

                bool expandOnButtonClick = true;
                GUIStyle expandCollapseButtonStyle = styles.digDownArrow;
                if (roundedLineCount >= thread.maxDepth)
                {
                    expandCollapseButtonStyle = styles.rollUpArrow;
                    expandOnButtonClick = false;
                }

                float threadYMax = CalculateMaxYPositionForThread(roundedLineCount, fullThreadsRectWithoutSidebar.y, yOffsetForThisThread);
                Vector2 expandCollapsButtonSize = expandCollapseButtonStyle.CalcSize(GUIContent.none);
                Rect expandCollapseButtonRect = new Rect(
                    fullThreadsRectWithoutSidebar.x + fullThreadsRectWithoutSidebar.width / 2 - expandCollapsButtonSize.x / 2,
                    threadYMax - expandCollapsButtonSize.y,
                    expandCollapsButtonSize.x,
                    expandCollapsButtonSize.y);

                // only do the button if it is visible
                if (GUIClip.visibleRect.Overlaps(expandCollapseButtonRect))
                {
                    switch (command)
                    {
                        case HandleThreadSplitterFoldoutButtonsCommand.OnlyHandleInput:
                            if (GUI.Button(expandCollapseButtonRect, GUIContent.none, GUIStyle.none))
                            {
                                // Expand or collapse button expands to show all or collapses to default line height
                                if (expandOnButtonClick)
                                {
                                    thread.linesToDisplay = thread.maxDepth;
                                }
                                else
                                {
                                    thread.linesToDisplay = group.defaultLineCountPerThread * (group.name.Equals(k_MainGroupName) && thread.threadIndex == 0 ? 2 : 1);
                                }

                                Event.current.Use();

                                // the height just changed dramatically, enforce the new boundaries
                                m_TimeArea.EnforceScaleAndRange();
                            }

                            break;
                        case HandleThreadSplitterFoldoutButtonsCommand.OnlyDraw:
                            if (Event.current.type == EventType.Repaint && expandOnButtonClick)
                            {
                                float height = styles.bottomShadow.CalcHeight(GUIContent.none, fullThreadsRectWithoutSidebar.width);
                                styles.bottomShadow.Draw(new Rect(fullThreadsRectWithoutSidebar.x, threadYMax - height, fullThreadsRectWithoutSidebar.width, height), GUIContent.none, 0);
                            }

                            if (Event.current.type == EventType.Repaint)
                            {
                                expandCollapseButtonStyle.Draw(expandCollapseButtonRect, GUIContent.none,
                                    expandCollapseButtonRect.Contains(Event.current.mousePosition), false, false, false);
                            }

                            break;
                        default:
                            break;
                    }
                }

                GUI.EndClip();
            }
        }

        public void DoGUI(int frameIndex, Rect position, bool fetchData, ref bool updateViewLive)
        {
            using (var iter = fetchData ? new ProfilerFrameDataIterator() : null)
            {
                int threadCount = fetchData ? iter.GetThreadCount(frameIndex) : 0;
                iter?.SetRoot(frameIndex, 0);

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

                DrawToolbar(iter, ref updateViewLive);

                EditorGUILayout.EndHorizontal();

                using (m_DoGUIMarker.Auto())
                {
                    if (!string.IsNullOrEmpty(dataAvailabilityMessage))
                    {
                        GUILayout.Label(dataAvailabilityMessage, BaseStyles.label);
                        return;
                    }
                    else if (!fetchData && !updateViewLive)
                    {
                        GUILayout.Label(BaseStyles.liveUpdateMessage, BaseStyles.label);
                        return;
                    }
                    if (threadCount == 0)
                    {
                        GUILayout.Label(BaseStyles.noData, BaseStyles.label);
                        return;
                    }

                    position.yMin -= 1; // Workaround: Adjust the y position as a temporary fix to a 1px vertical offset that need to be investigated.
                    Rect fullRect = position;
                    float sideWidth = Chart.kSideWidth;

                    Rect timeRulerRect = new Rect(fullRect.x + sideWidth, fullRect.y, fullRect.width - sideWidth, k_LineHeight);

                    Rect timeAreaRect = new Rect(fullRect.x + sideWidth, fullRect.y + timeRulerRect.height, fullRect.width - sideWidth, fullRect.height - timeRulerRect.height);

                    bool initializing = false;
                    if (m_TimeArea == null)
                    {
                        initializing = true;
                        m_TimeArea = new ZoomableArea();
                        m_TimeArea.hRangeLocked = false;
                        m_TimeArea.vRangeLocked = false;
                        m_TimeArea.hSlider = true;
                        m_TimeArea.vSlider = true;
                        m_TimeArea.vAllowExceedBaseRangeMax = false;
                        m_TimeArea.vAllowExceedBaseRangeMin = false;
                        m_TimeArea.hBaseRangeMin = 0;
                        m_TimeArea.vBaseRangeMin = 0;
                        m_TimeArea.vScaleMax = 1f;
                        m_TimeArea.vScaleMin = 1f;
                        m_TimeArea.scaleWithWindow = true;
                        m_TimeArea.margin = 10;
                        m_TimeArea.topmargin = 0;
                        m_TimeArea.bottommargin = 0;
                        m_TimeArea.upDirection = ZoomableArea.YDirection.Negative;
                        m_TimeArea.vZoomLockedByDefault = true;
                    }

                    m_TimeArea.rect = timeAreaRect;

                    Rect bottomLeftFillRect = new Rect(0, position.yMax - m_TimeArea.vSliderWidth, sideWidth - 1, m_TimeArea.vSliderWidth);

                    if (Event.current.type == EventType.Repaint)
                    {
                        styles.profilerGraphBackground.Draw(fullRect, false, false, false, false);
                    }

                    if (initializing)
                    {
                        InitializeNativeTimeline();
                    }

                    // Prepare group and Thread Info
                    UpdateGroupAndThreadInfo(frameIndex);

                    HandleFrameSelected(iter.frameTimeMS);

                    // update time area to new bounds
                    float combinedHeaderHeight, combinedThreadHeight;
                    float heightForAllBars = CalculateHeightForAllBars(fullRect, out combinedHeaderHeight, out combinedThreadHeight);

                    // if needed, take up more empty space below, to fill up the ZoomableArea
                    float emptySpaceBelowBars = Mathf.Max(k_DefaultEmptySpaceBelowBars, timeAreaRect.height - m_TimeArea.hSliderHeight - heightForAllBars);
                    if (m_SelectedEntry.downwardsZoomableAreaSpaceNeeded > 0)
                        emptySpaceBelowBars = Mathf.Max(m_SelectedThreadYRange + m_SelectedEntry.relativeYPos + m_SelectedEntry.downwardsZoomableAreaSpaceNeeded - heightForAllBars, emptySpaceBelowBars);

                    heightForAllBars += emptySpaceBelowBars;

                    m_TimeArea.hBaseRangeMax = iter.frameTimeMS;
                    m_TimeArea.vBaseRangeMax = heightForAllBars;

                    if (Mathf.Abs(heightForAllBars - m_LastHeightForAllBars) >= 0.5f || Mathf.Abs(fullRect.height - m_LastFullRectHeight) >= 0.5f)
                    {
                        m_LastHeightForAllBars = heightForAllBars;
                        m_LastFullRectHeight = fullRect.height;

                        // set V range to enforce scale of 1 and to shift the shown area up in case the drawn area shrunk down
                        m_TimeArea.SetShownVRange(m_TimeArea.shownArea.y, m_TimeArea.shownArea.y + m_TimeArea.drawRect.height);
                    }

                    // frame the selection if needed and before drawing the time area
                    if (m_Scheduled_FrameSelectionVertically)
                    {
                        // keep current zoom level but frame vertically
                        PerformFrameSelected(iter.frameTimeMS, verticallyFrameSelected: true, keepHorizontalZoomLevel: true);
                        m_Scheduled_FrameSelectionVertically = false;
                        //RepaintProfilerWindow();
                    }
                    else if (initializing)
                        PerformFrameSelected(iter.frameTimeMS);
                    // DoTimeArea needs to happen before DoTimeRulerGUI due to excess control ids being generated in repaint that breaks repeatbuttons
                    DoTimeArea();
                    DoTimeRulerGUI(timeRulerRect, sideWidth, iter.frameTimeMS);


                    Rect fullThreadsRect = new Rect(fullRect.x, fullRect.y + timeRulerRect.height, fullRect.width - m_TimeArea.vSliderWidth, fullRect.height - timeRulerRect.height - m_TimeArea.hSliderHeight);

                    Rect fullThreadsRectWithoutSidebar = fullThreadsRect;
                    fullThreadsRectWithoutSidebar.x += sideWidth;
                    fullThreadsRectWithoutSidebar.width -= sideWidth;

                    Rect sideRect = new Rect(fullThreadsRect.x, fullThreadsRect.y, sideWidth, fullThreadsRect.height);
                    if (sideRect.Contains(Event.current.mousePosition) && Event.current.isScrollWheel)
                    {
                        m_TimeArea.SetTransform(new Vector2(m_TimeArea.m_Translation.x, m_TimeArea.m_Translation.y - (Event.current.delta.y * 4)), m_TimeArea.m_Scale);
                        m_ProfilerWindow.Repaint();
                    }

                    // Layout and handle input for tooltips here so it can grab input before threadspillters and selection, draw it at the end of DoGUI to paint on top
                    if (Event.current.type != EventType.Repaint)
                        DoSelectionTooltip(frameIndex, m_TimeArea.drawRect);

                    // The splitters need to be handled after the time area so that they don't interfere with the input for panning/scrolling the ZoomableArea
                    DoThreadSplitters(fullThreadsRect, fullThreadsRectWithoutSidebar, frameIndex, ThreadSplitterCommand.HandleThreadSplitter);

                    Rect barsUIRect = m_TimeArea.drawRect;

                    DrawGrid(barsUIRect, iter.frameTimeMS);

                    Rect barsAndSidebarUIRect = new Rect(barsUIRect.x - sideWidth, barsUIRect.y, barsUIRect.width + sideWidth, barsUIRect.height);

                    if (Event.current.type == EventType.Repaint)
                    {
                        Rect leftSideRect = new Rect(0, position.y, sideWidth, position.height);

                        styles.leftPane.Draw(leftSideRect, false, false, false, false);

                        // The bar in the lower left side that fills the space next to the horizontal scrollbar.
                        EditorStyles.toolbar.Draw(bottomLeftFillRect, false, false, false, false);
                    }

                    GUI.BeginClip(barsAndSidebarUIRect);

                    Rect shownBarsUIRect = barsUIRect;
                    shownBarsUIRect.y = scrollOffsetY;

                    // since the scale is not applied to the group headers, there would be some height unaccounted for
                    // this calculation applies that height to the threads via scaleForThreadHeight
                    float heightUnaccountedForDueToNotScalingHeaders = m_TimeArea.scale.y * combinedHeaderHeight - combinedHeaderHeight;
                    float scaleForThreadHeight = (combinedThreadHeight * m_TimeArea.scale.y + heightUnaccountedForDueToNotScalingHeaders) / combinedThreadHeight;

                    DrawBars(shownBarsUIRect, scaleForThreadHeight);
                    GUI.EndClip();

                    DoRangeSelection(barsUIRect);

                    GUI.BeginClip(barsUIRect);
                    shownBarsUIRect.x = 0;

                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = false;

                    // Walk backwards to find how many previous frames we need to show.
                    int numContextFramesToShow = maxContextFramesToShow;
                    int currentFrame = frameIndex;
                    float currentTime = 0;
                    do
                    {
                        int prevFrame = ProfilerDriver.GetPreviousFrameIndex(currentFrame);
                        if (prevFrame == FrameDataView.invalidOrCurrentFrameIndex || !ProfilerDriver.GetFramesBelongToSameSession(currentFrame, prevFrame))
                            break;
                        iter.SetRoot(prevFrame, 0);
                        currentTime -= iter.frameTimeMS;
                        currentFrame = prevFrame;
                        --numContextFramesToShow;
                    }
                    while (currentTime > m_TimeArea.shownArea.x && numContextFramesToShow > 0);

                    // Draw previous frames
                    int firstDrawnFrame = currentFrame;
                    while (currentFrame != FrameDataView.invalidOrCurrentFrameIndex && currentFrame != frameIndex)
                    {
                        iter.SetRoot(currentFrame, 0);
                        DoProfilerFrame(frameIndex, currentFrame - frameIndex, shownBarsUIRect, true, currentTime, scaleForThreadHeight);
                        currentTime += iter.frameTimeMS;
                        currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
                    }

                    // Draw next frames
                    numContextFramesToShow = maxContextFramesToShow;
                    currentFrame = frameIndex;
                    currentTime = 0;
                    int lastDrawnFrame = currentFrame;
                    while (currentTime < m_TimeArea.shownArea.x + m_TimeArea.shownArea.width && numContextFramesToShow >= 0)
                    {
                        if (frameIndex != currentFrame)
                        {
                            DoProfilerFrame(frameIndex, currentFrame - frameIndex, shownBarsUIRect, true, currentTime, scaleForThreadHeight);
                            lastDrawnFrame = currentFrame;
                        }
                        iter.SetRoot(currentFrame, 0);
                        var prevFrame = currentFrame;
                        currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
                        if (currentFrame == FrameDataView.invalidOrCurrentFrameIndex || !ProfilerDriver.GetFramesBelongToSameSession(currentFrame, prevFrame))
                            break;
                        currentTime += iter.frameTimeMS;
                        --numContextFramesToShow;
                    }

                    GUI.enabled = oldEnabled;

                    // Draw center frame last to get on top
                    threadCount = 0;
                    currentTime = 0;
                    DoProfilerFrame(frameIndex, 0, shownBarsUIRect, false, currentTime, scaleForThreadHeight);
                    DoFlowEvents(frameIndex, firstDrawnFrame, lastDrawnFrame, shownBarsUIRect, scaleForThreadHeight);

                    GUI.EndClip();

                    // Draw Foldout Buttons on top of natively drawn bars
                    DoThreadSplitters(fullThreadsRect, fullThreadsRectWithoutSidebar, frameIndex, ThreadSplitterCommand.HandleThreadSplitterFoldoutButtons);

                    // Draw tooltips on top of clip to be able to extend outside of timeline area
                    if (Event.current.type == EventType.Repaint)
                        DoSelectionTooltip(frameIndex, m_TimeArea.drawRect);

                    if (Event.current.type == EventType.Repaint)
                    {
                        // Reset all flags once Repaint finished on this view
                        m_LastRepaintProcessedInputs = m_CurrentlyProcessedInputs;
                        m_CurrentlyProcessedInputs = 0;
                    }
                }
            }
        }

        public void ReInitialize()
        {
            InitializeNativeTimeline();
        }

        void InitializeNativeTimeline()
        {
            var args = new NativeProfilerTimeline_InitializeArgs();
            args.Reset();
            args.ghostAlpha = 0.3f;
            args.nonSelectedAlpha = 0.75f;
            args.guiStyle = styles.bar.m_Ptr;
            args.lineHeight = k_LineHeight;
            args.textFadeOutWidth = k_TextFadeOutWidth;
            args.textFadeStartWidth = k_TextFadeStartWidth;

            var timelineColors = ProfilerColors.timelineColors;
            args.profilerColorDescriptors = new ProfilerColorDescriptor[timelineColors.Length];

            for (int i = 0; i < timelineColors.Length; ++i)
            {
                args.profilerColorDescriptors[i] = new ProfilerColorDescriptor(timelineColors[i]);
            }

            args.showFullScriptingMethodNames = ((cpuModule.ViewOptions & CPUOrGPUProfilerModule.ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0) ? 1 : 0;

            NativeProfilerTimeline.Initialize(ref args);
        }

        void DoRangeSelection(Rect rect)
        {
            var controlID = EditorGUIUtility.GetControlID(RangeSelectionInfo.controlIDHint, FocusType.Passive);
            var evt = Event.current;

            switch (evt.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (EditorGUIUtility.hotControl == 0 && evt.button == 0 && rect.Contains(evt.mousePosition))
                    {
                        var delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                        delay.mouseDownPosition = evt.mousePosition;
                        m_RangeSelection.mouseDown = true;
                        m_RangeSelection.active = false;
                        m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.RangeSelection;
                    }

                    break;

                case EventType.MouseDrag:
                    if (EditorGUIUtility.hotControl == 0 && m_RangeSelection.mouseDown)
                    {
                        var delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                        if (delay.CanStartDrag())
                        {
                            EditorGUIUtility.hotControl = controlID;
                            m_RangeSelection.mouseDownTime = m_TimeArea.PixelToTime(delay.mouseDownPosition.x, rect);
                            m_RangeSelection.startTime = m_RangeSelection.endTime = m_RangeSelection.mouseDownTime;
                            ClearSelection();
                            m_RangeSelection.active = true;
                            evt.Use();
                            m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.RangeSelection;
                        }
                    }
                    else if (EditorGUIUtility.hotControl == controlID)
                    {
                        var cursorTime = m_TimeArea.PixelToTime(evt.mousePosition.x, rect);
                        if (cursorTime < m_RangeSelection.mouseDownTime)
                        {
                            m_RangeSelection.startTime = cursorTime;
                            m_RangeSelection.endTime = m_RangeSelection.mouseDownTime;
                        }
                        else
                        {
                            m_RangeSelection.startTime = m_RangeSelection.mouseDownTime;
                            m_RangeSelection.endTime = cursorTime;
                        }

                        evt.Use();
                        m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.RangeSelection;
                    }

                    break;

                case EventType.MouseUp:
                    if (EditorGUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        EditorGUIUtility.hotControl = 0;
                        m_RangeSelection.mouseDown = false;

                        m_CurrentlyProcessedInputs -= (m_CurrentlyProcessedInputs & ProcessedInputs.MouseDown) | (m_CurrentlyProcessedInputs & ProcessedInputs.RangeSelection);
                        evt.Use();
                    }

                    break;

                case EventType.Repaint:
                    if (m_RangeSelection.active)
                    {
                        var startPixel = m_TimeArea.TimeToPixel(m_RangeSelection.startTime, rect);
                        var endPixel = m_TimeArea.TimeToPixel(m_RangeSelection.endTime, rect);
                        if (startPixel > rect.xMax || endPixel < rect.xMin)
                            break;

                        var selectionRect = Rect.MinMaxRect(Mathf.Max(rect.xMin, startPixel), rect.yMin, Mathf.Min(rect.xMax, endPixel), rect.yMax);
                        styles.rectangleToolSelection.Draw(selectionRect, false, false, false, false);
                    }

                    break;
            }
        }

        internal void DrawToolbar(ProfilerFrameDataIterator frameDataIterator, ref bool updateViewLive)
        {
            DrawViewTypePopup(ProfilerViewType.Timeline);
            DrawLiveUpdateToggle(ref updateViewLive);

            GUILayout.FlexibleSpace();

            if (frameDataIterator != null)
                DrawCPUGPUTime(frameDataIterator.frameTimeMS, frameDataIterator.frameGpuTimeMS);

            GUILayout.FlexibleSpace();

            cpuModule?.DrawOptionsMenuPopup();
        }

        void UpdateActiveFlowEventsForAllThreadsInAllVisibleFrames(int frameIndex, List<RawFrameDataView.FlowEvent> activeFlowEvents)
        {
            if (activeFlowEvents?.Count == 0)
                return;

            int firstContextFrame = IndexOfFirstContextFrame(frameIndex, maxContextFramesToShow);
            int lastContextFrame = frameIndex + maxContextFramesToShow;
            int currentFrame = firstContextFrame;
            while ((currentFrame != FrameDataView.invalidOrCurrentFrameIndex) && (currentFrame <= lastContextFrame))
            {
                bool clearActiveFlowEvents = (currentFrame == firstContextFrame);
                UpdateActiveFlowEventsForAllThreadsAtFrame(frameIndex, currentFrame - frameIndex, clearActiveFlowEvents, activeFlowEvents);

                currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
            }
        }

        int IndexOfFirstContextFrame(int currentFrame, int maximumNumberOfContextFramesToShow)
        {
            int firstVisibleContextFrameIndex = currentFrame;
            do
            {
                int previousFrame = ProfilerDriver.GetPreviousFrameIndex(currentFrame);
                if (previousFrame == FrameDataView.invalidOrCurrentFrameIndex)
                {
                    break;
                }

                firstVisibleContextFrameIndex = previousFrame;
                maximumNumberOfContextFramesToShow--;
            }
            while (maximumNumberOfContextFramesToShow > 0);

            return firstVisibleContextFrameIndex;
        }

        void UpdateActiveFlowEventsForAllThreadsAtFrame(int currentFrameIndex, int currentFrameIndexOffset, bool clearThreadActiveFlowEvents, List<RawFrameDataView.FlowEvent> activeFlowEvents)
        {
            var frameIndex = currentFrameIndex + currentFrameIndexOffset;
            foreach (var groupInfo in m_Groups)
            {
                foreach (var threadInfo in groupInfo.threads)
                {
                    if (clearThreadActiveFlowEvents)
                    {
                        threadInfo.ActiveFlowEvents?.Clear();
                    }

                    var threadIndex = threadInfo.threadIndices[k_MaxNeighborFrames + currentFrameIndexOffset];
                    using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                    {
                        // In case we're crossing a boundary between data (f.e. captured and loaded, or captured from different devices)
                        // Some threads present in the first part might not be present in the second part
                        // As we use m_Groups from the first part, GetRawFrameDataView returns invalid object for the second
                        if ((frameData == null) || !frameData.valid)
                            continue;

                        frameData.GetFlowEvents(m_CachedThreadFlowEvents);
                        foreach (var threadFlowEvent in m_CachedThreadFlowEvents)
                        {
                            bool existsInActiveFlowEvents = false;
                            foreach (var activeFlowEvent in activeFlowEvents)
                            {
                                if (activeFlowEvent.FlowId == threadFlowEvent.FlowId)
                                {
                                    existsInActiveFlowEvents = true;
                                    break;
                                }
                            }

                            if (existsInActiveFlowEvents)
                            {
                                var flowEventData = new ThreadInfo.FlowEventData
                                {
                                    flowEvent = threadFlowEvent,
                                    frameIndex = frameIndex,
                                    threadIndex = threadIndex
                                };
                                threadInfo.AddFlowEvent(flowEventData);
                            }
                        }
                    }
                }
            }
        }

        struct DrawnFlowIndicatorCacheValue
        {
            public int threadId;
            public int markerId;
            public ProfilerFlowEventType flowEventType;
        }

        class DrawnFlowIndicatorCacheValueComparer : EqualityComparer<DrawnFlowIndicatorCacheValue>
        {
            public override bool Equals(DrawnFlowIndicatorCacheValue x, DrawnFlowIndicatorCacheValue y)
            {
                return x.threadId.Equals(y.threadId) && x.markerId.Equals(y.markerId) && x.flowEventType.Equals(y.flowEventType);
            }

            public override int GetHashCode(DrawnFlowIndicatorCacheValue obj)
            {
                return base.GetHashCode();
            }
        }
    }
}
