// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal.Profiling;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [Serializable]
    internal class ProfilerTimelineGUI : ProfilerFrameDataViewBase
    {
        const float kTextFadeStartWidth = 50.0f;
        const float kTextFadeOutWidth = 20.0f;
        const float kLineHeight = 16.0f;
        const float kGroupHeight = kLineHeight + 4.0f;

        static readonly float[] k_TickModulos = { 0.001f, 0.005f, 0.01f, 0.05f, 0.1f, 0.5f, 1, 5, 10, 50, 100, 500, 1000, 5000, 10000, 30000, 60000 };
        const string k_TickFormatMilliseconds = "{0}ms";
        const string k_TickFormatSeconds = "{0}s";
        const int k_TickLabelSeparation = 60;

        private float animationTime = 1.0f;
        private double lastScrollUpdate = 0.0f;

        internal class ThreadInfo
        {
            public float height;
            public float desiredWeight;
            public float weight;
            public int threadIndex;
            public string name;
            public bool alive;
        }


        internal class GroupInfo
        {
            public bool expanded;
            public string name;
            public float height;
            public List<ThreadInfo> threads;
        }

        private List<GroupInfo> groups = null;

        internal class Styles
        {
            public GUIStyle background = "OL Box";
            public GUIStyle tooltip = "AnimationEventTooltip";
            public GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
            public GUIStyle bar = "ProfilerTimelineBar";
            public GUIStyle leftPane = "ProfilerTimelineLeftPane";
            public GUIStyle rightPane = "ProfilerRightPane";
            public GUIStyle foldout = "ProfilerTimelineFoldout";
            public GUIStyle profilerGraphBackground = new GUIStyle("ProfilerScrollviewBackground");
            public GUIStyle timelineTick = "AnimationTimelineTick";
            public GUIStyle rectangleToolSelection = "RectangleToolSelection";
            public Color frameDelimiterColor = Color.white.RGBMultiplied(0.4f);
            Color m_RangeSelectionColorLight = new Color32(255, 255, 255, 90);
            Color m_RangeSelectionColorDark = new Color32(200, 200, 200, 40);
            public Color rangeSelectionColor => EditorGUIUtility.isProSkin ? m_RangeSelectionColorDark : m_RangeSelectionColorLight;
            Color m_OutOfRangeColorLight = new Color32(160, 160, 160, 127);
            Color m_OutOfRangeColorDark = new Color32(40, 40, 40, 127);
            public Color outOfRangeColor => EditorGUIUtility.isProSkin ? m_OutOfRangeColorDark : m_OutOfRangeColorLight;

            internal Styles()
            {
                bar.normal.background = bar.hover.background = bar.active.background = EditorGUIUtility.whiteTexture;
                bar.normal.textColor = bar.hover.textColor = bar.active.textColor = Color.black;
                profilerGraphBackground.overflow.left = -((int)Chart.kSideWidth - 1);
                leftPane.padding.left = 15;
            }
        }

        private static Styles ms_Styles;
        private static Styles styles
        {
            get { return ms_Styles ?? (ms_Styles = new Styles()); }
        }

        private class EntryInfo
        {
            public int frameId = -1;
            public int threadId = -1;
            public int nativeIndex = -1; // Uniquely identifies the sample for the thread and frame.
            public float relativeYPos = 0.0f;
            public float time = 0.0f;
            public float duration = 0.0f;
            public string name = string.Empty;

            public bool IsValid() { return this.name.Length > 0; }
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

        private class SelectedEntryInfo : EntryInfo
        {
            public int instanceId = -1;
            public string metaData = string.Empty;

            public float totalDuration = -1.0f;
            public int instanceCount = -1;
            public string callstackInfo = string.Empty;

            public override void Reset()
            {
                base.Reset();

                this.instanceId = -1;
                this.metaData = string.Empty;

                this.totalDuration = -1.0f;
                this.instanceCount = -1;
                this.callstackInfo = string.Empty;
            }
        }

        [NonSerialized]
        private ZoomableArea m_TimeArea;
        private TickHandler m_HTicks;
        private IProfilerWindowController m_Window;
        private SelectedEntryInfo m_SelectedEntry = new SelectedEntryInfo();
        private float m_SelectedThreadY = 0.0f;

        private string m_LocalizedString_Total;
        private string m_LocalizedString_Instances;

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

        public ProfilerTimelineGUI(IProfilerWindowController window)
        {
            m_Window = window;
            // Configure default groups
            groups = new List<GroupInfo>(new GroupInfo[]
            {
                new GroupInfo() { name = "", height = 0, expanded = true, threads = new List<ThreadInfo>() },
                new GroupInfo() { name = "Unity Job System", height = kGroupHeight, expanded = SessionState.GetBool("Unity Job System", false), threads = new List<ThreadInfo>() },
                new GroupInfo() { name = "Loading", height = kGroupHeight, expanded = SessionState.GetBool("Loading", false), threads = new List<ThreadInfo>() },
            });

            m_LocalizedString_Total = LocalizationDatabase.GetLocalizedString("Total");
            m_LocalizedString_Instances = LocalizationDatabase.GetLocalizedString("Instances");

            m_HTicks = new TickHandler();
            m_HTicks.SetTickModulos(k_TickModulos);
        }

        private void CalculateBars(ref ProfilerFrameDataIterator iter, Rect r, int frameIndex, float time)
        {
            float visibleThreadCount = 0;
            iter.SetRoot(frameIndex, 0);
            int threadCount = iter.GetThreadCount(frameIndex);
            for (int i = 0; i < threadCount; ++i)
            {
                iter.SetRoot(frameIndex, i);
                string groupname = iter.GetGroupName();
                GroupInfo group = groups.Find(g => g.name == groupname);
                if (group == null)
                {
                    group = new GroupInfo();
                    group.name = groupname;
                    group.height = kGroupHeight;
                    group.expanded = false;
                    group.threads = new List<ThreadInfo>();
                    groups.Add(group);
                }
                var threads = group.threads;

                ThreadInfo thread = threads.Find(t => t.threadIndex == i);
                if (thread == null)
                {
                    thread = new ThreadInfo();
                    thread.name = iter.GetThreadName();
                    thread.height = 0;
                    thread.weight = thread.desiredWeight = group.expanded ? 1 : 0;
                    thread.threadIndex = i;
                    group.threads.Add(thread);
                }
                thread.alive = true;

                if (thread.weight != thread.desiredWeight)
                    thread.weight = thread.desiredWeight * time + (1 - thread.desiredWeight) * (1 - time);

                visibleThreadCount += thread.weight;
            }

            var groupheight = 0.0f;
            foreach (var group in groups)
            {
                if (group.threads.Count > 1)
                    groupheight += !group.expanded ? group.height : Math.Max(group.height, group.threads.Count * 2.0f);
            }
            var remaining = r.height - groupheight;
            var heightPerThread = remaining / (visibleThreadCount + 1); // main thread gets 2 times the space

            foreach (var group in groups)
            {
                foreach (var thread in group.threads)
                {
                    thread.height = heightPerThread * thread.weight;
                }
            }

            groups[0].expanded = true;
            groups[0].height = 0;
            if (groups[0].threads.Count > 0)
            {
                groups[0].threads[0].height = 2 * heightPerThread;
            }
        }

        private void UpdateAnimatedFoldout()
        {
            double deltaTime = EditorApplication.timeSinceStartup - lastScrollUpdate;
            animationTime = Math.Min(1.0f, animationTime + (float)deltaTime);
            m_Window.Repaint();
            if (animationTime == 1.0f)
                EditorApplication.update -= UpdateAnimatedFoldout;
        }

        private bool DrawBar(Rect r, float y, float height, string name, bool group, bool expanded, bool indent)
        {
            Rect leftRect = new Rect(r.x - Chart.kSideWidth, y, Chart.kSideWidth, height);
            Rect rightRect = new Rect(r.x, y, r.width, height);
            if (Event.current.type == EventType.Repaint)
            {
                styles.rightPane.Draw(rightRect, false, false, false, false);
                const float shrinkHeight = 25;
                bool shrinkName = height < shrinkHeight;
                GUIContent content = GUIContent.Temp(name);
                if (shrinkName)
                    styles.leftPane.padding.top -= (int)(shrinkHeight - height) / 2;
                if (indent)
                    styles.leftPane.padding.left += 10;
                styles.leftPane.Draw(leftRect, content, false, false, false, false);
                if (indent)
                    styles.leftPane.padding.left -= 10;
                if (shrinkName)
                    styles.leftPane.padding.top += (int)(shrinkHeight - height) / 2;
            }
            if (group)
            {
                leftRect.width -= 1.0f; // text should not draw ontop of right border
                leftRect.xMin += 1.0f; // shift toggle arrow right
                return GUI.Toggle(leftRect, expanded, GUIContent.none, styles.foldout);
            }
            return false;
        }

        private void DrawBars(Rect r, int frameIndex)
        {
            bool hasThreadinfoToDraw = false;
            foreach (var group in groups)
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
            foreach (var groupInfo in groups)
            {
                bool mainGroup = groupInfo.name == "";
                if (!mainGroup)
                {
                    var height = groupInfo.height;
                    var expandedState = groupInfo.expanded;
                    groupInfo.expanded = DrawBar(r, y, height, groupInfo.name, true, expandedState, false);

                    if (groupInfo.expanded != expandedState)
                    {
                        SessionState.SetBool(groupInfo.name, groupInfo.expanded);
                        animationTime = 0.0f;
                        lastScrollUpdate = EditorApplication.timeSinceStartup;
                        EditorApplication.update += UpdateAnimatedFoldout;
                        foreach (var threadInfo in groupInfo.threads)
                        {
                            threadInfo.desiredWeight = groupInfo.expanded ? 1.0f : 0.0f;
                        }
                    }
                    y += height;
                }

                foreach (var threadInfo in groupInfo.threads)
                {
                    var height = threadInfo.height;
                    if (height != 0)
                        DrawBar(r, y, height, threadInfo.name, false, true, !mainGroup);
                    y += height;
                }
            }
        }

        void DoNativeProfilerTimeline(Rect r, int frameIndex, int threadIndex, float timeOffset, bool ghost)
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
                localRect.y = 0;

                if (Event.current.type == EventType.Repaint)
                {
                    DrawNativeProfilerTimeline(localRect, frameIndex, threadIndex, timeOffset, ghost);
                }
                else if (Event.current.type == EventType.MouseDown && !ghost) // Ghosts are not clickable
                {
                    HandleNativeProfilerTimelineInput(localRect, frameIndex, threadIndex, timeOffset, topMargin);
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
            drawArgs.shownAreaRect = m_TimeArea.shownArea;
            drawArgs.selectedEntryIndex = hasSelection ? m_SelectedEntry.nativeIndex : -1;
            drawArgs.mousedOverEntryIndex = -1;

            NativeProfilerTimeline.Draw(ref drawArgs);
        }

        void HandleNativeProfilerTimelineInput(Rect threadRect, int frameIndex, int threadIndex, float timeOffset, float topMargin)
        {
            // Only let this thread view change mouse state if it contained the mouse pos
            bool inThreadRect = threadRect.Contains(Event.current.mousePosition);
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

                    m_Window.SetSelectedPropertyPath(instanceInfoArgs.out_Path);

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
                }

                Event.current.Use();
                UpdateSelectedObject(singleClick, doubleClick);
            }
            else
            {
                // click on empty space de-selects
                if (doSelect)
                {
                    ClearSelection();
                    Event.current.Use();
                }
            }
        }

        private void UpdateSelectedObject(bool singleClick, bool doubleClick)
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

        private void ClearSelection()
        {
            m_Window.ClearSelectedPropertyPath();

            m_SelectedEntry.Reset();
            m_RangeSelection.active = false;
        }

        private void PerformFrameSelected(float frameMS)
        {
            float t;
            float dt;
            if (m_RangeSelection.active)
            {
                t = m_RangeSelection.startTime;
                dt = m_RangeSelection.duration;
            }
            else
            {
                t = m_SelectedEntry.time;
                dt = m_SelectedEntry.duration;
                if (m_SelectedEntry.instanceId < 0 || dt <= 0.0f)
                {
                    t = 0.0f;
                    dt = frameMS;
                }
            }

            m_TimeArea.SetShownHRangeInsideMargins(t - dt * 0.2f, t + dt * 1.2f);
        }

        private void HandleFrameSelected(float frameMS)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
            {
                if (evt.commandName == EventCommandNames.FrameSelected)
                {
                    bool execute = evt.type == EventType.ExecuteCommand;
                    if (execute)
                        PerformFrameSelected(frameMS);
                    evt.Use();
                }
            }
        }

        void DoProfilerFrame(int frameIndex, Rect fullRect, bool ghost, int threadCount, float offset)
        {
            var iter = new ProfilerFrameDataIterator();
            int myThreadCount = iter.GetThreadCount(frameIndex);
            if (ghost && myThreadCount != threadCount)
                return;

            iter.SetRoot(frameIndex, 0);
            if (!ghost)
            {
                HandleFrameSelected(iter.frameTimeMS);
            }

            float y = fullRect.y;
            foreach (var groupInfo in groups)
            {
                Rect r = fullRect;
                var expanded = groupInfo.expanded;
                if (expanded)
                    y += groupInfo.height;

                var yStart = y;
                var groupThreadCount = groupInfo.threads.Count;
                foreach (var threadInfo in groupInfo.threads)
                {
                    iter.SetRoot(frameIndex, threadInfo.threadIndex);
                    r.y = y;
                    r.height = expanded ? threadInfo.height : Math.Max(groupInfo.height / groupThreadCount - 1, 2);

                    DoNativeProfilerTimeline(r, frameIndex, threadInfo.threadIndex, offset, ghost);

                    // Save the y pos of the selected thread each time we draw, since it can change
                    bool containsSelected = m_SelectedEntry.IsValid() && (m_SelectedEntry.frameId == frameIndex) && (m_SelectedEntry.threadId == threadInfo.threadIndex);
                    if (containsSelected)
                        m_SelectedThreadY = y;

                    y += r.height;
                }
                if (!expanded)
                    y = yStart + Math.Max(groupInfo.height, groupThreadCount * 2);
            }
        }

        void DoSelectionTooltip(int frameIndex, Rect fullRect)
        {
            // Draw selected tooltip
            if (!m_SelectedEntry.IsValid() || m_SelectedEntry.frameId != frameIndex)
                return;

            string durationString = string.Format(m_SelectedEntry.duration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.duration);

            System.Text.StringBuilder text = new System.Text.StringBuilder();
            text.Append(string.Format("{0}\n{1}", m_SelectedEntry.name, durationString));

            // Show total duration if more than one instance
            if (m_SelectedEntry.instanceCount > 1)
            {
                string totalDurationString = string.Format(m_SelectedEntry.totalDuration >= 1.0 ? "{0:f2}ms" : "{0:f3}ms", m_SelectedEntry.totalDuration);
                text.Append(string.Format("\n{0}: {1} ({2} {3})", m_LocalizedString_Total, totalDurationString, m_SelectedEntry.instanceCount, m_LocalizedString_Instances));
            }

            if (m_SelectedEntry.metaData.Length > 0)
            {
                text.Append(string.Format("\n{0}", m_SelectedEntry.metaData));
            }

            if (m_SelectedEntry.callstackInfo.Length > 0)
            {
                text.Append(string.Format("\n{0}", m_SelectedEntry.callstackInfo));
            }

            float selectedY = fullRect.y + m_SelectedThreadY + m_SelectedEntry.relativeYPos;
            float x = m_TimeArea.TimeToPixel(m_SelectedEntry.time + m_SelectedEntry.duration * 0.5f, fullRect);
            ShowLargeTooltip(new Vector2(x, selectedY), fullRect, text.ToString());
        }

        public void MarkDeadOrClearThread()
        {
            foreach (var group in groups)
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

            GL.End();

            GUI.EndClip();
        }

        public void DoTimeRulerGUI(Rect rect, float frameTime)
        {
            if (m_TimeArea == null || Event.current.type != EventType.Repaint)
                return;

            GUI.BeginClip(rect);
            rect.x = rect.y = 0;

            GUI.Box(rect, GUIContent.none, EditorStyles.toolbarButton);

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
                    var ticks = m_HTicks.GetTicksAtLevel(l, true);
                    for (int i = 0; i < ticks.Length; i++)
                    {
                        // Draw line
                        var time = ticks[i];
                        var x = m_TimeArea.TimeToPixel(time, rect);
                        var height = rect.height * Mathf.Min(1, strength) * TimeArea.kTickRulerHeightMax;
                        var color = new Color(1, 1, 1, strength / TimeArea.kTickRulerFatThreshold) * baseColor;
                        TimeArea.DrawVerticalLineFast(x, rect.height - height + 0.5f, rect.height - 0.5f, color);
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
                float labelpos = Mathf.Floor(m_TimeArea.TimeToPixel(time, rect));
                string label = FormatTickLabel(time, labelLevel);
                GUI.Label(new Rect(labelpos + 3, -3, labelWidth, 20), label, styles.timelineTick);
            }

            // Outside current frame coloring
            DrawOutOfRangeOverlay(rect, frameTime);

            // Range selection coloring
            DrawRangeSelectionOverlay(rect);

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
            return string.Format(format, time.ToString("N" + Mathf.Max(0, -log10)));
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
            var labelText = string.Format(k_TickFormatMilliseconds, m_RangeSelection.duration.ToString("N3"));
            Chart.DoLabel(startPixel + (endPixel - startPixel) / 2, rect.yMin + 3, labelText, -0.5f);
        }

        public void DoGUI(FrameDataView frameDataView, float width, float ypos, float height)
        {
            if (frameDataView == null || !frameDataView.IsValid())
            {
                GUILayout.Label(BaseStyles.noData, BaseStyles.label);
                return;
            }

            Rect fullRect = new Rect(0, ypos - 1, width, height + 1);
            float sideWidth = Chart.kSideWidth - 1;

            if (Event.current.type == EventType.Repaint)
            {
                styles.profilerGraphBackground.Draw(fullRect, false, false, false, false);
                // The bar in the lower left side that fills the space next to the horizontal scrollbar.
                EditorStyles.toolbar.Draw(new Rect(0, ypos + height - 15, sideWidth, 15), false, false, false, false);
            }

            bool initializing = false;
            if (m_TimeArea == null)
            {
                initializing = true;
                m_TimeArea = new ZoomableArea();
                m_TimeArea.hRangeLocked = false;
                m_TimeArea.vRangeLocked = true;
                m_TimeArea.hSlider = true;
                m_TimeArea.vSlider = false;
                m_TimeArea.scaleWithWindow = true;
                m_TimeArea.rect = new Rect(fullRect.x + sideWidth - 1, fullRect.y, fullRect.width - sideWidth, fullRect.height);
                m_TimeArea.margin = 10;
            }

            if (initializing)
            {
                NativeProfilerTimeline_InitializeArgs args = new NativeProfilerTimeline_InitializeArgs();
                args.Reset();
                args.ghostAlpha = 0.3f;
                args.nonSelectedAlpha = 0.75f;
                args.guiStyle = styles.bar.m_Ptr;
                args.lineHeight = kLineHeight;
                args.textFadeOutWidth = kTextFadeOutWidth;
                args.textFadeStartWidth = kTextFadeStartWidth;

                NativeProfilerTimeline.Initialize(ref args);
            }

            var iter = new ProfilerFrameDataIterator();
            int threadCount = iter.GetThreadCount(frameDataView.frameIndex);
            iter.SetRoot(frameDataView.frameIndex, 0);
            m_TimeArea.hBaseRangeMin = 0;
            m_TimeArea.hBaseRangeMax = iter.frameTimeMS;
            if (initializing)
                PerformFrameSelected(iter.frameTimeMS);

            m_TimeArea.rect = new Rect(fullRect.x + sideWidth, fullRect.y, fullRect.width - sideWidth, fullRect.height);
            m_TimeArea.BeginViewGUI();
            m_TimeArea.EndViewGUI();

            fullRect = m_TimeArea.drawRect;

            DrawGrid(fullRect, iter.frameTimeMS);

            MarkDeadOrClearThread();
            CalculateBars(ref iter, fullRect, frameDataView.frameIndex, animationTime);
            DrawBars(fullRect, frameDataView.frameIndex);

            DoRangeSelection(m_TimeArea.drawRect);

            GUI.BeginClip(m_TimeArea.drawRect);
            fullRect.x = 0;
            fullRect.y = 0;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = false;
            // Walk backwards to find how many previous frames we need to show.
            int maxContextFramesToShow = m_Window.IsRecording() ? 1 : 3;
            int numContextFramesToShow = maxContextFramesToShow;
            int currentFrame = frameDataView.frameIndex;
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
            while (currentFrame != -1 && currentFrame != frameDataView.frameIndex)
            {
                iter.SetRoot(currentFrame, 0);
                DoProfilerFrame(currentFrame, fullRect, true, threadCount, currentTime);
                currentTime += iter.frameTimeMS;
                currentFrame = ProfilerDriver.GetNextFrameIndex(currentFrame);
            }

            // Draw next frames
            numContextFramesToShow = maxContextFramesToShow;
            currentFrame = frameDataView.frameIndex;
            currentTime = 0;
            while (currentTime < m_TimeArea.shownArea.x + m_TimeArea.shownArea.width && numContextFramesToShow >= 0)
            {
                if (frameDataView.frameIndex != currentFrame)
                    DoProfilerFrame(currentFrame, fullRect, true, threadCount, currentTime);
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
            DoProfilerFrame(frameDataView.frameIndex, fullRect, false, threadCount, 0);

            GUI.EndClip();

            // Draw tooltips on top of clip to be able to extend outside of timeline area
            DoSelectionTooltip(frameDataView.frameIndex, m_TimeArea.drawRect);
        }

        void DoRangeSelection(Rect rect)
        {
            var controlID = EditorGUIUtility.GetControlID(RangeSelectionInfo.controlIDHint, FocusType.Passive);
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (EditorGUIUtility.hotControl == 0 && evt.button == 0 && rect.Contains(evt.mousePosition))
                    {
                        var delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                        delay.mouseDownPosition = evt.mousePosition;
                        m_RangeSelection.mouseDown = true;
                        m_RangeSelection.active = false;
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
                    }
                    break;

                case EventType.MouseUp:
                    if (EditorGUIUtility.hotControl == controlID && evt.button == 0)
                    {
                        EditorGUIUtility.hotControl = 0;
                        m_RangeSelection.mouseDown = false;
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

        internal void DrawToolbar(FrameDataView frameDataView)
        {
            const float sidebarWidth = Chart.kSideWidth - 1;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(sidebarWidth));
            DrawViewTypePopup(ProfilerViewType.Timeline);
            EditorGUILayout.EndHorizontal();

            var height = EditorStyles.toolbar.CalcHeight(GUIContent.none, 0f);
            var timeRulerRect = EditorGUILayout.GetControlRect(false, height, GUIStyle.none, GUILayout.ExpandWidth(true));
            var iter = new ProfilerFrameDataIterator();
            iter.SetRoot(frameDataView.frameIndex, 0);
            var frameTime = iter.frameTimeMS;
            DoTimeRulerGUI(timeRulerRect, frameTime);
        }
    }
}
