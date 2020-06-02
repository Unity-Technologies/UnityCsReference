// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal.Profiling;
using Object = UnityEngine.Object;
using UnityEditor.AnimatedValues;
using Unity.Profiling;
using System.Globalization;

namespace UnityEditorInternal
{
    [Serializable]
    internal class ProfilerTimelineGUI : ProfilerFrameDataViewBase
    {
        const float k_TextFadeStartWidth = 50.0f;
        const float k_TextFadeOutWidth = 20.0f;
        const float k_LineHeight = 16.0f;
        const float k_ExtraHeightPerThread = 4f;
        const float k_FullThreadLineHeight = k_LineHeight + 0.55f;
        const float k_GroupHeight = k_LineHeight + 4f;
        const float k_ThreadMinHeightCollapsed = 2.0f;
        const float k_ThreadSplitterHandleSize = 6f;

        static readonly float[] k_TickModulos = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 30000, 60000 };
        const string k_TickFormatMilliseconds = "{0}ms";
        const string k_TickFormatSeconds = "{0}s";
        const int k_TickLabelSeparation = 60;

        internal class ThreadInfo : IComparable<ThreadInfo>
        {
            public float height = 0;
            public float linesToDisplay = 2f;
            public int threadIndex;
            public string name;
            public bool alive;
            public int maxDepth;

            List<FlowEventData> m_ActiveFlowEvents = new List<FlowEventData>();

            public ThreadInfo(string name, int threadIndex, int maxDepth, int linesToDisplay)
            {
                this.name = name;
                this.threadIndex = threadIndex;
                this.linesToDisplay = linesToDisplay;
                this.maxDepth = Mathf.Max(1, maxDepth);
            }

            public List<FlowEventData> ActiveFlowEvents
            {
                get
                {
                    return m_ActiveFlowEvents;
                }
            }

            public int CompareTo(ThreadInfo other)
            {
                if (this == other)
                    return 0;
                return EditorUtility.NaturalCompare(name, other.name);
            }

            public struct FlowEventData
            {
                public RawFrameDataView.FlowEvent flowEvent;
                public int frameIndex;

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
            public int defaultLineCountPerThread = k_DefaultLineCountPerThread;

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
            }
        }

        List<GroupInfo> m_Groups = null;
        List<int> m_SamplesWithDrawnFlowEventsPerThreadCache = new List<int>();

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

            public string localizedStringTotal = L10n.Tr("Total");
            public string localizedStringInstances = L10n.Tr("Instances");

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
            public int frameId = -1;
            public int threadId = -1;
            public int nativeIndex = -1; // Uniquely identifies the sample for the thread and frame.
            public float relativeYPos = 0.0f;
            public float time = 0.0f;
            public float duration = 0.0f;
            public string name = string.Empty;

            public bool IsValid()
            {
                return this.name.Length > 0;
            }

            public bool Equals(int frameId, int threadId, int nativeIndex)
            {
                return frameId == this.frameId && threadId == this.threadId && nativeIndex == this.nativeIndex;
            }

            public virtual void Reset()
            {
                this.frameId = -1;
                this.threadId = -1;
                this.nativeIndex = -1;
                this.relativeYPos = 0.0f;
                this.time = 0.0f;
                this.duration = 0.0f;
                this.name = string.Empty;
            }
        }

        class SelectedEntryInfo : EntryInfo
        {
            // The associated GameObjects instance ID. Negative means Native Object, Positive means Managed Object, 0 means not set (as in, no object associated)
            public int instanceId = 0;
            public string metaData = string.Empty;

            public float totalDuration = -1.0f;
            public int instanceCount = -1;
            public string callstackInfo = string.Empty;

            List<RawFrameDataView.FlowEvent> m_FlowEvents = new List<RawFrameDataView.FlowEvent>();

            public List<RawFrameDataView.FlowEvent> FlowEvents
            {
                get
                {
                    return m_FlowEvents;
                }
            }

            public override void Reset()
            {
                base.Reset();

                this.instanceId = 0;
                this.metaData = string.Empty;

                this.totalDuration = -1.0f;
                this.instanceCount = -1;
                this.callstackInfo = string.Empty;

                FlowEvents.Clear();
            }
        }

        float scrollOffsetY
        {
            get { return -m_TimeArea.shownArea.y * m_TimeArea.scale.y; }
        }

        [NonSerialized]
        ZoomableArea m_TimeArea;
        TickHandler m_HTicks;
        SelectedEntryInfo m_SelectedEntry = new SelectedEntryInfo();
        float m_SelectedThreadY = 0.0f;
        float m_SelectedThreadYRange = 0.0f;
        ThreadInfo m_SelectedThread = null;
        int m_LastSelectedFrameID = -1;
        float m_LastHeightForAllBars = -1;
        float m_LastFullRectHeight = -1;
        float m_MaxLinesToDisplayForTheCurrentlyModifiedSplitter = -1;

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

        public override void OnEnable(CPUorGPUProfilerModule cpuOrGpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            base.OnEnable(cpuOrGpuModule, profilerWindow, isGpuView);
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

        void UpdateGroupAndThreadInfo(ProfilerFrameDataIterator iter, int frameIndex)
        {
            iter.SetRoot(frameIndex, 0);
            int threadCount = iter.GetThreadCount(frameIndex);
            for (int i = 0; i < threadCount; ++i)
            {
                iter.SetRoot(frameIndex, i);
                string groupname = iter.GetGroupName();
                GroupInfo group = m_Groups.Find(g => g.name == groupname);
                if (group == null)
                {
                    group = new GroupInfo(groupname, RepaintProfilerWindow);
                    m_Groups.Add(group);
                }

                var threads = group.threads;

                ThreadInfo thread = threads.Find(t => t.threadIndex == i);
                if (thread == null)
                {
                    // ProfilerFrameDataIterator.maxDepth includes the thread sample which is not getting displayed, so we store it at -1 for all intents and purposes
                    thread = new ThreadInfo(iter.GetThreadName(), i, iter.maxDepth - 1, group.defaultLineCountPerThread);

                    // the main thread gets double the size
                    if (i == 0)
                        thread.linesToDisplay *= 2;

                    group.threads.Add(thread);
                }
                else if (m_LastSelectedFrameID != frameIndex)
                {
                    thread.maxDepth = iter.maxDepth;
                }

                thread.alive = true;
            }

            foreach (var group in m_Groups)
            {
                group.threads.Sort();
            }

            m_LastSelectedFrameID = frameIndex;
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

        bool DrawBar(Rect r, float y, float height, string name, bool group, bool expanded, bool indent)
        {
            Rect leftRect = new Rect(r.x - Chart.kSideWidth, y, Chart.kSideWidth, height);
            Rect rightRect = new Rect(r.x, y, r.width, height);
            if (Event.current.type == EventType.Repaint)
            {
                styles.rightPane.Draw(rightRect, false, false, false, false);
                GUIContent content = GUIContent.Temp(name);
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
                    var newExpandedState = DrawBar(r, y, height, groupInfo.name, true, expandedState, false);

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
                        DrawBar(r, y, height, threadInfo.name, false, true, !mainGroup);
                    y += height;
                }
            }
        }

        void DoNativeProfilerTimeline(Rect r, int frameIndex, int maxContextFramesToShow, int threadIndex, float timeOffset, bool ghost, float scaleForThreadHeight)
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
                else if (Event.current.type == EventType.MouseDown && !ghost) // Ghosts are not clickable
                {
                    HandleNativeProfilerTimelineInput(localRect, frameIndex, maxContextFramesToShow, threadIndex, timeOffset, topMargin, scaleForThreadHeight);
                }
            }
            GUI.EndGroup();
        }

        void DrawNativeProfilerTimeline(Rect threadRect, int frameIndex, int threadIndex, float timeOffset, bool ghost)
        {
            bool hasSelection = m_SelectedEntry.threadId == threadIndex && m_SelectedEntry.frameId == frameIndex;

            NativeProfilerTimeline_DrawArgs drawArgs = new NativeProfilerTimeline_DrawArgs();
            drawArgs.Reset();
            drawArgs.frameIndex = frameIndex;
            drawArgs.threadIndex = threadIndex;
            drawArgs.timeOffset = timeOffset;
            drawArgs.threadRect = threadRect;

            // cull text that would otherwise draw over the bottom scrollbar
            drawArgs.threadRect.yMax = Mathf.Min(drawArgs.threadRect.yMax, m_TimeArea.shownArea.height - m_TimeArea.hSliderHeight);
            drawArgs.shownAreaRect = m_TimeArea.shownArea;
            drawArgs.selectedEntryIndex = hasSelection ? m_SelectedEntry.nativeIndex : -1;
            drawArgs.mousedOverEntryIndex = -1;

            NativeProfilerTimeline.Draw(ref drawArgs);
        }

        void HandleNativeProfilerTimelineInput(Rect threadRect, int frameIndex, int maxContextFramesToShow, int threadIndex, float timeOffset, float topMargin, float scaleForThreadHeight)
        {
            // Only let this thread view change mouse state if it contained the mouse pos
            Rect clippedRect = threadRect;
            clippedRect.y = 0;
            bool inThreadRect = clippedRect.Contains(Event.current.mousePosition);
            if (!inThreadRect)
                return;

            bool singleClick = Event.current.clickCount == 1 && Event.current.type == EventType.MouseDown;
            bool doubleClick = Event.current.clickCount == 2 && Event.current.type == EventType.MouseDown;

            bool doSelect = (singleClick || doubleClick) && Event.current.button == 0;
            if (!doSelect)
                return;

            NativeProfilerTimeline_GetEntryAtPositionArgs posArgs = new NativeProfilerTimeline_GetEntryAtPositionArgs();
            posArgs.Reset();
            posArgs.frameIndex = frameIndex;
            posArgs.threadIndex = threadIndex;
            posArgs.timeOffset = timeOffset;
            posArgs.threadRect = threadRect;
            posArgs.threadRect.height *= scaleForThreadHeight;
            posArgs.shownAreaRect = m_TimeArea.shownArea;
            posArgs.position = Event.current.mousePosition;

            NativeProfilerTimeline.GetEntryAtPosition(ref posArgs);

            int mouseOverIndex = posArgs.out_EntryIndex;
            if (mouseOverIndex != -1)
            {
                bool selectedChanged = !m_SelectedEntry.Equals(frameIndex, threadIndex, mouseOverIndex);
                if (selectedChanged)
                {
                    // Read out timing info
                    NativeProfilerTimeline_GetEntryTimingInfoArgs timingInfoArgs = new NativeProfilerTimeline_GetEntryTimingInfoArgs();
                    timingInfoArgs.Reset();
                    timingInfoArgs.frameIndex = frameIndex;
                    timingInfoArgs.threadIndex = threadIndex;
                    timingInfoArgs.entryIndex = mouseOverIndex;
                    timingInfoArgs.calculateFrameData = true;
                    NativeProfilerTimeline.GetEntryTimingInfo(ref timingInfoArgs);

                    // Read out instance info for selection
                    NativeProfilerTimeline_GetEntryInstanceInfoArgs instanceInfoArgs = new NativeProfilerTimeline_GetEntryInstanceInfoArgs();
                    instanceInfoArgs.Reset();
                    instanceInfoArgs.frameIndex = frameIndex;
                    instanceInfoArgs.threadIndex = threadIndex;
                    instanceInfoArgs.entryIndex = mouseOverIndex;
                    NativeProfilerTimeline.GetEntryInstanceInfo(ref instanceInfoArgs);

                    m_ProfilerWindow.SetSelectedPropertyPath(instanceInfoArgs.out_Path);

                    // Set selected entry info
                    m_SelectedEntry.Reset();
                    m_SelectedEntry.frameId = frameIndex;
                    m_SelectedEntry.threadId = threadIndex;
                    m_SelectedEntry.nativeIndex = mouseOverIndex;
                    m_SelectedEntry.instanceId = instanceInfoArgs.out_Id;
                    m_SelectedEntry.time = timingInfoArgs.out_LocalStartTime;
                    m_SelectedEntry.duration = timingInfoArgs.out_Duration;
                    m_SelectedEntry.totalDuration = timingInfoArgs.out_TotalDurationForFrame;
                    m_SelectedEntry.instanceCount = timingInfoArgs.out_InstanceCountForFrame;
                    m_SelectedEntry.relativeYPos = posArgs.out_EntryYMaxPos + topMargin;
                    m_SelectedEntry.name = posArgs.out_EntryName;
                    m_SelectedEntry.callstackInfo = instanceInfoArgs.out_CallstackInfo;
                    m_SelectedEntry.metaData = instanceInfoArgs.out_MetaData;

                    if ((cpuModule.ViewOptions & CPUorGPUProfilerModule.ProfilerViewFilteringOptions.ShowExecutionFlow) != 0)
                    {
                        using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                        {
                            // posArgs.out_EntryIndex is a MeshCache index which differs from sample index by 1 as root is not included into MeshCache.
                            frameData.GetSampleFlowEvents(mouseOverIndex + 1, m_SelectedEntry.FlowEvents);
                            UpdateActiveFlowEventsForAllThreadsInAllVisibleFrames(frameIndex, maxContextFramesToShow, m_SelectedEntry.FlowEvents);
                        }
                    }
                }

                Event.current.Use();
                UpdateSelectedObject(singleClick, doubleClick);

                m_CurrentlyProcessedInputs |= ProcessedInputs.MouseDown | ProcessedInputs.FrameSelection;
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
        }

        void ClearSelection()
        {
            m_ProfilerWindow.ClearSelectedPropertyPath();

            m_SelectedEntry.Reset();
            m_RangeSelection.active = false;
        }

        void PerformFrameAll(float frameMS)
        {
            PerformFrameSelected(frameMS, false, true);
        }

        void PerformFrameSelected(float frameMS, bool verticallyFrameSelected = true, bool hFrameAll = false)
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
                if (m_SelectedEntry.instanceCount <= 0 || dt <= 0.0f)
                {
                    t = 0.0f;
                    dt = frameMS;
                }
            }

            m_TimeArea.SetShownHRangeInsideMargins(t - dt * 0.2f, t + dt * 1.2f);

            if (m_SelectedEntry.instanceCount >= 0 && verticallyFrameSelected)
            {
                if (m_SelectedEntry.relativeYPos > m_SelectedThread.height)
                {
                    ThreadInfo selectedThread = null;
                    foreach (var group in m_Groups)
                    {
                        foreach (var thread in group.threads)
                        {
                            if (thread.threadIndex == m_SelectedEntry.threadId)
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
                        selectedThread.linesToDisplay = CalculateLineCount(m_SelectedEntry.relativeYPos + k_LineHeight);
                        RepaintProfilerWindow();
                    }
                }

                float yMin = m_TimeArea.shownArea.y;
                float yMax = yMin + m_TimeArea.shownArea.height;

                float yMinPosition = m_SelectedThreadYRange + m_SelectedEntry.relativeYPos - k_LineHeight;
                float yMaxPosition = m_SelectedThreadYRange + m_SelectedEntry.relativeYPos;


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

        void DoProfilerFrame(int frameIndex, int maxContextFramesToShow, Rect fullRect, bool ghost, int threadCount, float offset, float scaleForThreadHeight)
        {
            using (var iter = new ProfilerFrameDataIterator())
            {
                int myThreadCount = iter.GetThreadCount(frameIndex);
                if (ghost && myThreadCount != threadCount)
                    return;

                iter.SetRoot(frameIndex, 0);

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

                        var tr = r;
                        tr.y -= fullRect.y;
                        if (tr.yMin < m_TimeArea.shownArea.yMax && tr.yMax > m_TimeArea.shownArea.yMin)
                        {
                            iter.SetRoot(frameIndex, threadInfo.threadIndex);
                            DoNativeProfilerTimeline(r, frameIndex, maxContextFramesToShow, threadInfo.threadIndex, offset, ghost, scaleForThreadHeight);
                        }

                        // Save the y pos and height of the selected thread each time we draw, since it can change
                        bool containsSelected = m_SelectedEntry.IsValid() && (m_SelectedEntry.frameId == frameIndex) && (m_SelectedEntry.threadId == threadInfo.threadIndex);
                        if (containsSelected)
                        {
                            m_SelectedThreadY = y;
                            m_SelectedThreadYRange = rangeY;
                            m_SelectedThread = threadInfo;
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
        }

        void DoFlowEvents(int currentFrameIndex, int firstDrawnFrameIndex, int lastDrawnFrameIndex, Rect fullRect, float scaleForThreadHeight)
        {
            if ((cpuModule.ViewOptions & CPUorGPUProfilerModule.ProfilerViewFilteringOptions.ShowExecutionFlow) == 0)
            {
                return;
            }

            bool hasSelectedSampleWithFlowEvents = (m_SelectedEntry.FlowEvents.Count > 0) && SelectedSampleIsVisible(firstDrawnFrameIndex, lastDrawnFrameIndex);
            FlowLinesDrawer activeFlowLinesDrawer = (hasSelectedSampleWithFlowEvents) ? new FlowLinesDrawer() : null;
            ForEachThreadInEachGroup(fullRect, scaleForThreadHeight, (groupInfo, threadInfo, threadRect) =>
            {
                var groupIsExpanded = groupInfo.expanded.value;
                DrawIndicatorsForAllFlowEventsInFrameOnThread(currentFrameIndex, threadInfo, threadRect, fullRect, groupIsExpanded, hasSelectedSampleWithFlowEvents);

                if (hasSelectedSampleWithFlowEvents)
                {
                    ProcessActiveFlowEventsOnThread(ref activeFlowLinesDrawer, threadInfo, threadRect, fullRect, currentFrameIndex, groupIsExpanded);
                }
            });

            activeFlowLinesDrawer?.Draw();
        }

        void DrawIndicatorsForAllFlowEventsInFrameOnThread(int currentFrameIndex, ThreadInfo threadInfo, Rect threadRect, Rect fullRect, bool groupIsExpanded, bool hasSelectedEntryWithFlowEvents)
        {
            m_SamplesWithDrawnFlowEventsPerThreadCache.Clear();

            using (var frameData = ProfilerDriver.GetRawFrameDataView(currentFrameIndex, threadInfo.threadIndex))
            {
                frameData.GetFlowEvents(m_CachedThreadFlowEvents);

                var localViewport = new Rect(Vector2.zero, fullRect.size);
                var indicatorMode = (hasSelectedEntryWithFlowEvents) ? FlowIndicatorDrawer.IndicatorAppearanceMode.Inactive : FlowIndicatorDrawer.IndicatorAppearanceMode.NoActiveEvents;
                foreach (var flowEvent in m_CachedThreadFlowEvents)
                {
                    if (flowEvent.ParentSampleIndex != 0)
                    {
                        // A sample can have multiple begin flow events. Check an indicator hasn't already been drawn for this sample on this thread.
                        if (!SampleAlreadyHasFlowEventType(flowEvent.ParentSampleIndex, flowEvent.FlowEventType))
                        {
                            var sampleRect = RectForSampleOnFrameInThread(flowEvent.ParentSampleIndex, currentFrameIndex, threadInfo, threadRect, 0f, m_TimeArea.shownArea, (groupIsExpanded == false));
                            if (sampleRect.Overlaps(localViewport))
                            {
                                FlowIndicatorDrawer.DrawFlowIndicatorForFlowEvent(flowEvent, sampleRect, indicatorMode);

                                // A sample can only have multiple Begin flow events so only use the cache for begin events.
                                if (flowEvent.FlowEventType == ProfilerFlowEventType.Begin)
                                {
                                    m_SamplesWithDrawnFlowEventsPerThreadCache.Add(flowEvent.ParentSampleIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        void ProcessActiveFlowEventsOnThread(ref FlowLinesDrawer flowLinesDrawer, ThreadInfo threadInfo, Rect threadRect, Rect fullRect, int currentFrameIndex, bool groupIsExpanded)
        {
            foreach (var flowEventData in threadInfo.ActiveFlowEvents)
            {
                var flowEvent = flowEventData.flowEvent;
                var flowEventFrameIndex = flowEventData.frameIndex;
                var timeOffset = ProfilerFrameTimingUtility.TimeOffsetBetweenFrames(currentFrameIndex, flowEventData.frameIndex);
                var sampleRect = RectForSampleOnFrameInThread(flowEvent.ParentSampleIndex, flowEventFrameIndex, threadInfo, threadRect, timeOffset, m_TimeArea.shownArea, (groupIsExpanded == false));

                // A flow event can have no parent sample if it is not enclosed within a PROFILER_AUTO scope.
                if (flowEventData.hasParentSampleIndex)
                {
                    // Add flow events to the 'flow lines drawer' for drawing later.
                    bool isSelectedSample = false;
                    if (!flowLinesDrawer.hasSelectedEventPosition)
                    {
                        isSelectedSample = (m_SelectedEntry.threadId == threadInfo.threadIndex) && (m_SelectedEntry.frameId == flowEventFrameIndex) && m_SelectedEntry.FlowEvents.Contains(flowEvent);
                    }
                    flowLinesDrawer.AddFlowEvent(flowEventData, threadInfo.threadIndex, sampleRect, isSelectedSample);

                    // Draw indicator for this active flow event.
                    var localViewport = new Rect(Vector2.zero, fullRect.size);
                    if (sampleRect.Overlaps(localViewport))
                    {
                        FlowIndicatorDrawer.DrawFlowIndicatorForFlowEvent(flowEvent, sampleRect, FlowIndicatorDrawer.IndicatorAppearanceMode.Active);
                    }
                }
            }
        }

        bool SampleAlreadyHasFlowEventType(int sampleIndex, ProfilerFlowEventType flowEventType)
        {
            // A sample can only have multiple Begin flow events.
            if (flowEventType != ProfilerFlowEventType.Begin)
            {
                return false;
            }

            bool sampleAlreadyHasFlowEventType = false;
            foreach (int parentSampleId in m_SamplesWithDrawnFlowEventsPerThreadCache)
            {
                if (sampleIndex == parentSampleId)
                {
                    sampleAlreadyHasFlowEventType = true;
                    break;
                }
            }

            return sampleAlreadyHasFlowEventType;
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
            // The unscaled y value equating to the timeline Y range value.
            float rangeY = 0;
            foreach (var groupInfo in m_Groups)
            {
                var threadRect = fullRect;
                var groupIsExpanded = groupInfo.expanded.value;
                if (groupIsExpanded && groupInfo.threads.Count > 0)
                {
                    y += groupInfo.height;
                    rangeY += groupInfo.height;
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
                    rangeY += threadRect.height;
                    notExpandedLeftOverY -= threadRect.height;
                }

                // Align next thread with the next group
                if (notExpandedLeftOverY > 0)
                {
                    y += notExpandedLeftOverY;
                    rangeY += notExpandedLeftOverY;
                }
            }
        }

        Rect RectForSampleOnFrameInThread(int sampleIndex, int frameIndex, ThreadInfo threadInfo, Rect threadRect, float timeOffset, Rect shownAreaRect, bool isGroupCollapsed)
        {
            var positionInfoArgs = RawPositionInfoForSample(sampleIndex, frameIndex, threadInfo.threadIndex, threadRect, timeOffset, shownAreaRect);
            CorrectRawPositionInfo(isGroupCollapsed, threadInfo, threadRect, ref positionInfoArgs);

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
        void CorrectRawPositionInfo(bool isGroupCollapsed, ThreadInfo threadInfo, Rect threadRect, ref NativeProfilerTimeline_GetEntryPositionInfoArgs positionInfo)
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

        void DoSelectionTooltip(int frameIndex, Rect fullRect)
        {
            // Draw selected tooltip
            if (!m_SelectedEntry.IsValid() || m_SelectedEntry.frameId != frameIndex)
                return;

            string durationString = UnityString.Format(m_SelectedEntry.duration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.duration);

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.Append(UnityString.Format("{0}\n{1}", m_SelectedEntry.name, durationString));

            // Show total duration if more than one instance
            if (m_SelectedEntry.instanceCount > 1)
            {
                string totalDurationString = UnityString.Format(m_SelectedEntry.totalDuration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.totalDuration);
                text.Append(string.Format("\n{0}: {1} ({2} {3})", styles.localizedStringTotal, totalDurationString, m_SelectedEntry.instanceCount, styles.localizedStringInstances));
            }

            if (m_SelectedEntry.metaData.Length > 0)
            {
                text.Append(string.Format("\n{0}", m_SelectedEntry.metaData));
            }

            if (m_SelectedEntry.callstackInfo.Length > 0)
            {
                text.Append(string.Format("\n{0}", m_SelectedEntry.callstackInfo));
            }

            float selectedThreadYOffset = fullRect.y + m_SelectedThreadY;
            float selectedY = selectedThreadYOffset + m_SelectedEntry.relativeYPos;
            float maxYPosition = Mathf.Min(fullRect.yMax, selectedThreadYOffset + m_SelectedThread.height);

            // calculate how much of the line height is visible (needed for calculating the offset of the tooltip when flipping)
            float selectedLineHeightVisible = Mathf.Clamp(maxYPosition - (selectedY - k_LineHeight), 0, k_LineHeight);

            // keep the popup within the drawing area and thread rect
            selectedY = Mathf.Clamp(selectedY, fullRect.y, maxYPosition);

            float x = m_TimeArea.TimeToPixel(m_SelectedEntry.time + m_SelectedEntry.duration * 0.5f, fullRect);
            ShowLargeTooltip(new Vector2(x, selectedY), fullRect, text.ToString(), selectedLineHeightVisible);
        }

        public void MarkDeadOrClearThread()
        {
            foreach (var group in m_Groups)
            {
                for (int i = group.threads.Count - 1; i >= 0; i--)
                {
                    if (group.threads[i].alive)
                        group.threads[i].alive = false;
                    else
                        group.threads.RemoveAt(i);
                }
            }
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
                    UpdateGroupAndThreadInfo(iter, frameIndex);
                    MarkDeadOrClearThread();

                    HandleFrameSelected(iter.frameTimeMS);

                    // update time area to new bounds
                    float combinedHeaderHeight, combinedThreadHeight;
                    float heightForAllBars = CalculateHeightForAllBars(fullRect, out combinedHeaderHeight, out combinedThreadHeight);
                    float emptySpaceBelowBars = k_LineHeight * 3f;

                    // if needed, take up more empty space below, to fill up the ZoomableArea
                    emptySpaceBelowBars = Mathf.Max(emptySpaceBelowBars, timeAreaRect.height - m_TimeArea.hSliderHeight - heightForAllBars);

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
                    if (initializing)
                        PerformFrameSelected(iter.frameTimeMS);
                    DoTimeRulerGUI(timeRulerRect, sideWidth, iter.frameTimeMS);
                    DoTimeArea();

                    Rect fullThreadsRect = new Rect(fullRect.x, fullRect.y + timeRulerRect.height, fullRect.width - m_TimeArea.vSliderWidth, fullRect.height - timeRulerRect.height - m_TimeArea.hSliderHeight);

                    Rect fullThreadsRectWithoutSidebar = fullThreadsRect;
                    fullThreadsRectWithoutSidebar.x += sideWidth;
                    fullThreadsRectWithoutSidebar.width -= sideWidth;

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
                    int maxContextFramesToShow = m_ProfilerWindow.IsRecording() ? 1 : 3;
                    int numContextFramesToShow = maxContextFramesToShow;
                    int currentFrame = frameIndex;
                    float currentTime = 0;
                    do
                    {
                        int prevFrame = ProfilerDriver.GetPreviousFrameIndex(currentFrame);
                        if (prevFrame == -1)
                            break;
                        iter.SetRoot(prevFrame, 0);
                        currentTime -= iter.frameTimeMS;
                        currentFrame = prevFrame;
                        --numContextFramesToShow;
                    }
                    while (currentTime > m_TimeArea.shownArea.x && numContextFramesToShow > 0);

                    // Draw previous frames
                    int firstDrawnFrame = currentFrame;
                    while (currentFrame != -1 && currentFrame != frameIndex)
                    {
                        iter.SetRoot(currentFrame, 0);
                        DoProfilerFrame(currentFrame, maxContextFramesToShow, shownBarsUIRect, true, threadCount, currentTime, scaleForThreadHeight);
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
                            DoProfilerFrame(currentFrame, maxContextFramesToShow, shownBarsUIRect, true, threadCount, currentTime, scaleForThreadHeight);
                            lastDrawnFrame = currentFrame;
                        }
                        iter.SetRoot(currentFrame, 0);
                        currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
                        if (currentFrame == -1)
                            break;
                        currentTime += iter.frameTimeMS;
                        --numContextFramesToShow;
                    }

                    GUI.enabled = oldEnabled;

                    // Draw center frame last to get on top
                    threadCount = 0;
                    currentTime = 0;
                    DoProfilerFrame(frameIndex, maxContextFramesToShow, shownBarsUIRect, false, threadCount, currentTime, scaleForThreadHeight);
                    DoFlowEvents(frameIndex, firstDrawnFrame, lastDrawnFrame, shownBarsUIRect, scaleForThreadHeight);

                    GUI.EndClip();

                    // Draw Foldout Buttons on top of natively drawn bars
                    DoThreadSplitters(fullThreadsRect, fullThreadsRectWithoutSidebar, frameIndex, ThreadSplitterCommand.HandleThreadSplitterFoldoutButtons);

                    // Draw tooltips on top of clip to be able to extend outside of timeline area
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

            args.showFullScriptingMethodNames = ((cpuModule.ViewOptions & CPUorGPUProfilerModule.ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0) ? 1 : 0;

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

        void UpdateActiveFlowEventsForAllThreadsInAllVisibleFrames(int frameIndex, int maximumNumberOfContextFramesToShow, List<RawFrameDataView.FlowEvent> activeFlowEvents)
        {
            if (activeFlowEvents.Count == 0)
            {
                return;
            }

            int firstContextFrame = IndexOfFirstContextFrame(frameIndex, maximumNumberOfContextFramesToShow);
            int lastContextFrame = frameIndex + maximumNumberOfContextFramesToShow;
            int currentFrame = firstContextFrame;
            while ((currentFrame != -1) && (currentFrame <= lastContextFrame))
            {
                bool clearActiveFlowEvents = (currentFrame == firstContextFrame);
                UpdateActiveFlowEventsForAllThreadsAtFrame(currentFrame, clearActiveFlowEvents, activeFlowEvents);

                currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
            }
        }

        int IndexOfFirstContextFrame(int currentFrame, int maximumNumberOfContextFramesToShow)
        {
            int firstVisibleContextFrameIndex = currentFrame;
            do
            {
                int previousFrame = ProfilerDriver.GetPreviousFrameIndex(currentFrame);
                if (previousFrame == -1)
                {
                    break;
                }

                firstVisibleContextFrameIndex = previousFrame;
                maximumNumberOfContextFramesToShow--;
            }
            while (maximumNumberOfContextFramesToShow > 0);

            return firstVisibleContextFrameIndex;
        }

        void UpdateActiveFlowEventsForAllThreadsAtFrame(int frameIndex, bool clearThreadActiveFlowEvents, List<RawFrameDataView.FlowEvent> activeFlowEvents)
        {
            foreach (var groupInfo in m_Groups)
            {
                foreach (var threadInfo in groupInfo.threads)
                {
                    if (clearThreadActiveFlowEvents)
                    {
                        threadInfo.ActiveFlowEvents.Clear();
                    }

                    using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadInfo.threadIndex))
                    {
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
                                    frameIndex = frameIndex
                                };
                                threadInfo.ActiveFlowEvents.Add(flowEventData);
                            }
                        }
                    }
                }
            }
        }
    }
}
