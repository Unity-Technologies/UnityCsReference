// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [Serializable]
    internal class ProfilerTimelineGUI
    {
        //static int s_TimelineHash = "ProfilerTimeline".GetHashCode();

        const float kSmallWidth = 7.0f;
        const float kTextFadeStartWidth = 50.0f;
        const float kTextFadeOutWidth = 20.0f;
        const float kTextLongWidth = 200.0f;
        const float kLineHeight = 16.0f;
        const float kGroupHeight = kLineHeight + 4.0f;

        private float animationTime = 1.0f;
        private double lastScrollUpdate = 0.0f;

        internal class ThreadInfo
        {
            public float height;
            public float desiredWeight;
            public float weight;
            public int threadIndex;
            public string name;
        }

        internal class GroupInfo
        {
            public bool expanded;
            public string name;
            public float height;
            public List<ThreadInfo> threads;
        }
        List<GroupInfo> groups;

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
        private IProfilerWindowController m_Window;
        private SelectedEntryInfo m_SelectedEntry = new SelectedEntryInfo();
        private float m_SelectedThreadY = 0.0f;

        private string m_LocalizedString_Total;
        private string m_LocalizedString_Instances;

        public ProfilerTimelineGUI(IProfilerWindowController window)
        {
            m_Window = window;

            // Configure default groups
            groups = new List<GroupInfo>(new GroupInfo[]
            {
                new GroupInfo() { name = "", height = kGroupHeight, expanded = true, threads = new List<ThreadInfo>() },
                new GroupInfo() { name = "Unity Job System", height = kGroupHeight, expanded = true, threads = new List<ThreadInfo>() },
                new GroupInfo() { name = "Loading", height = kGroupHeight, expanded = false, threads = new List<ThreadInfo>() },
            });

            m_LocalizedString_Total = LocalizationDatabase.GetLocalizedString("Total");
            m_LocalizedString_Instances = LocalizationDatabase.GetLocalizedString("Instances");
        }

        private void CalculateBars(Rect r, int frameIndex, float time)
        {
            var iter = new ProfilerFrameDataIterator();
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
                if (thread.weight != thread.desiredWeight)
                    thread.weight = thread.desiredWeight * time + (1 - thread.desiredWeight) * (1 - time);
                visibleThreadCount += thread.weight;
            }

            int groupCount = groups.Count((group) => group.threads.Count > 1);
            var groupheight = kGroupHeight * groupCount;
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
            groups[0].threads[0].height = 2 * heightPerThread;
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

        private void DrawGrid(Rect r, int threadCount, float frameTime)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            float x;
            float kDT = 16.66667f;
            if (frameTime > 1000.0f)
                kDT = 100.0f;

            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.LINES);
            GL.Color(new Color(1, 1, 1, 0.2f));
            // vertical 16.6ms apart
            float t = kDT;
            for (; t <= frameTime; t += kDT)
            {
                x = m_TimeArea.TimeToPixel(t, r);
                GL.Vertex3(x, r.y, 0.0f);
                GL.Vertex3(x, r.y + r.height, 0.0f);
            }
            // vertical: frame boundaries
            GL.Color(new Color(1, 1, 1, 0.8f));
            x = m_TimeArea.TimeToPixel(0.0f, r);
            GL.Vertex3(x, r.y, 0.0f);
            GL.Vertex3(x, r.y + r.height, 0.0f);
            x = m_TimeArea.TimeToPixel(frameTime, r);
            GL.Vertex3(x, r.y, 0.0f);
            GL.Vertex3(x, r.y + r.height, 0.0f);

            GL.End();

            // time labels
            GUI.color = new Color(1, 1, 1, 0.4f);
            t = 0.0f;
            for (; t <= frameTime; t += kDT)
            {
                x = m_TimeArea.TimeToPixel(t, r);
                // Don't show FPS for every ms marker along the x axis.
                Chart.DoLabel(x + 2, r.yMax - 12, string.Format("{0:f1}ms", t), 0);
            }

            GUI.color = new Color(1, 1, 1, 1.0f);
            t = frameTime;
            {
                x = m_TimeArea.TimeToPixel(t, r);
                // Show FPS for last ms marker that shows the actual ms for this frame.
                Chart.DoLabel(x + 2, r.yMax - 12, string.Format("{0:f1}ms ({1:f0}FPS)", t, 1000.0f / t), 0);
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
        }

        private void PerformFrameSelected(float frameMS)
        {
            float t = m_SelectedEntry.time;
            float dt = m_SelectedEntry.duration;
            if (m_SelectedEntry.instanceId < 0 || dt <= 0.0f)
            {
                t = 0.0f;
                dt = frameMS;
            }
            m_TimeArea.SetShownHRangeInsideMargins(t - dt * 0.2f, t + dt * 1.2f);
        }

        private void HandleFrameSelected(float frameMS)
        {
            Event evt = Event.current;
            if (evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
            {
                if (evt.commandName == "FrameSelected")
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
                DrawGrid(fullRect, threadCount, iter.frameTimeMS);
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
                    y = yStart + groupInfo.height;
            }
        }

        void DoSelectionTooltip(int frameIndex, Rect fullRect)
        {
            // Draw selected tooltip
            if (m_SelectedEntry.IsValid() && m_SelectedEntry.frameId == frameIndex)
            {
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

                GUIContent textC = new GUIContent(text.ToString());
                GUIStyle style = styles.tooltip;
                Vector2 size = style.CalcSize(textC);

                float x = m_TimeArea.TimeToPixel(m_SelectedEntry.time + m_SelectedEntry.duration * 0.5f, fullRect);

                // Arrow of tooltip
                Rect arrowRect = new Rect(x - 32, selectedY, 64, 6);

                // Label box
                Rect rect = new Rect(x, selectedY + 6, size.x, size.y);

                // Ensure it doesn't go too far right
                if (rect.xMax > fullRect.xMax + 16)
                    rect.x = fullRect.xMax - rect.width + 16;
                if (arrowRect.xMax > fullRect.xMax + 20)
                    arrowRect.x = fullRect.xMax - arrowRect.width + 20;

                // Adjust left to we can always see giant (STL) names.
                if (rect.xMin < fullRect.xMin + 30)
                    rect.x = fullRect.xMin + 30;
                if (arrowRect.xMin < fullRect.xMin - 20)
                    arrowRect.x = fullRect.xMin - 20;

                // Flip tooltip if too close to bottom (but do not flip if flipping would mean the tooltip is too high up)
                float flipRectAdjust = (kLineHeight + rect.height + 2 * arrowRect.height);
                bool flipped = (selectedY + size.y + 6 > fullRect.yMax) && (rect.y - flipRectAdjust > 0);
                if (flipped)
                {
                    rect.y -= flipRectAdjust;
                    arrowRect.y -= (kLineHeight + 2 * arrowRect.height);
                }

                // Draw small arrow
                GUI.BeginClip(arrowRect);
                Matrix4x4 oldMatrix = GUI.matrix;
                if (flipped)
                    GUIUtility.ScaleAroundPivot(new Vector2(1.0f, -1.0f), new Vector2(arrowRect.width * 0.5f, arrowRect.height));
                GUI.Label(new Rect(0, 0, arrowRect.width, arrowRect.height), GUIContent.none, styles.tooltipArrow);
                GUI.matrix = oldMatrix;
                GUI.EndClip();

                // Draw tooltip
                GUI.Label(rect, textC, style);
            }
        }

        public void DoGUI(int frameIndex, float width, float ypos, float height)
        {
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
                args.profilerColors = ProfilerColors.currentColors;
                args.allocationSampleColor = ProfilerColors.allocationSample;
                args.internalSampleColor = ProfilerColors.internalSample;
                args.ghostAlpha = 0.3f;
                args.nonSelectedAlpha = 0.75f;
                args.guiStyle = styles.bar.m_Ptr;
                args.lineHeight = kLineHeight;
                args.textFadeOutWidth = kTextFadeOutWidth;
                args.textFadeStartWidth = kTextFadeStartWidth;

                NativeProfilerTimeline.Initialize(ref args);
            }

            var iterCurrent = new ProfilerFrameDataIterator();
            iterCurrent.SetRoot(frameIndex, 0);
            m_TimeArea.hBaseRangeMin = 0;
            m_TimeArea.hBaseRangeMax = iterCurrent.frameTimeMS;
            if (initializing)
                PerformFrameSelected(iterCurrent.frameTimeMS);

            m_TimeArea.rect = new Rect(fullRect.x + sideWidth, fullRect.y, fullRect.width - sideWidth, fullRect.height);
            m_TimeArea.BeginViewGUI();
            m_TimeArea.EndViewGUI();

            fullRect = m_TimeArea.drawRect;
            CalculateBars(fullRect, frameIndex, animationTime);
            DrawBars(fullRect, frameIndex);

            GUI.BeginClip(m_TimeArea.drawRect);
            fullRect.x = 0;
            fullRect.y = 0;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = false;

            var iter = new ProfilerFrameDataIterator();
            int threadCount = iter.GetThreadCount(frameIndex);

            int prevFrame = ProfilerDriver.GetPreviousFrameIndex(frameIndex);
            if (prevFrame != -1)
            {
                iter.SetRoot(prevFrame, 0);
                DoProfilerFrame(prevFrame, fullRect, true, threadCount, -iter.frameTimeMS);
            }
            int nextFrame = ProfilerDriver.GetNextFrameIndex(frameIndex);
            if (nextFrame != -1)
            {
                iter.SetRoot(frameIndex, 0);
                DoProfilerFrame(nextFrame, fullRect, true, threadCount, iter.frameTimeMS);
            }

            GUI.enabled = oldEnabled;

            // Draw center frame last to get on top
            threadCount = 0;
            DoProfilerFrame(frameIndex, fullRect, false, threadCount, 0);

            GUI.EndClip();

            // Draw tooltips on top of clip to be able to extend outside of timeline area
            DoSelectionTooltip(frameIndex, m_TimeArea.drawRect);
        }
    }
}
