// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace UnityEditor.AnimationWindowBuiltin
{
    [System.Serializable]
    internal class AnimationEventTimeLine
    {
        internal static class Styles
        {
            public static GUIContent textAddEvent = EditorGUIUtility.TrTextContent("Add Animation Event");
            public static GUIContent textDeleteEvents = EditorGUIUtility.TrTextContent("Delete Animation Events");
            public static GUIContent textDeleteEvent = EditorGUIUtility.TrTextContent("Delete Animation Event");
            public static GUIContent textCopyEvents = EditorGUIUtility.TrTextContent("Copy Animation Events");
            public static GUIContent textPasteEvents = EditorGUIUtility.TrTextContent("Paste Animation Events");

            public static GUIContent eventMarker = EditorGUIUtility.IconContent("Animation.LargeEventMarker");
            public static GUIContent eventMarkerMultiOverlay = EditorGUIUtility.IconContent("Animation.LargeEventMarker.MultiOverlay");
        }

        // Event clustering data structure for grouping events at the same time
        struct EventCluster
        {
            public int firstIndex;
            public int lastIndex;
            public int index;
            public int indexCount;

            public EventCluster()
            {
                firstIndex = -1;
                lastIndex = -1;
                index = -1;
                indexCount = 0;
            }
        }

        [System.NonSerialized]
        private AnimationEvent[] m_EventsAtMouseDown;
        [System.NonSerialized]
        private float[] m_EventTimes;
        [System.NonSerialized]
        bool m_IsDragging = false;

        // Updated to Timeline's Signal marker size (9x16) for better visibility
        // Larger markers remain visible even when playhead overlaps them (fixes UUM-138402)
        private static readonly Vector2 k_EventMarkerSize = new Vector2(9, 16);

        private static readonly Vector2 k_TooltipOffset = new Vector2(-30f, 0f);

        private bool m_DirtyTooltip = false;
        private int m_HoverEvent = -1;
        private string m_InstantTooltipText = null;
        private Vector2 m_InstantTooltipPoint = Vector2.zero;
        private bool m_HasSelectedEvents;

        public string tooltipText => m_InstantTooltipText;
        public Vector2 tooltipPosition => m_InstantTooltipPoint;

        public AnimationEventTimeLine(EditorWindow owner)
        {
        }

        public class EventComparer : IComparer<AnimationEvent>
        {
            public int Compare(AnimationEvent x, AnimationEvent y)
            {
                float timeX = x.time;
                float timeY = y.time;
                if (timeX != timeY)
                    return ((int)Mathf.Sign(timeX - timeY));

                int valueX = x.GetHashCode();
                int valueY = y.GetHashCode();
                return valueX - valueY;
            }
        }

        // Build clusters of events at the same time (based on Timeline's MarkersLayer.cs)
        PooledObject<Dictionary<int, EventCluster>> BuildClusters(AnimationEvent[] events, float frameRate, out Dictionary<int, EventCluster> clusters)
        {
            var pooledClusters = DictionaryPool<int, EventCluster>.Get(out clusters);
            using var pooledAccumulator = ListPool<(int frame, int index)>.Get(out var accumulator);

            // Events should already be sorted by time from GetAnimationEvents
            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                var frame = AnimationKeyTime.Time(evt.time, frameRate).frame;

                // Check if this event is at a different frame than accumulated events
                if (accumulator.Count > 0)
                {
                    var lastFrame = accumulator[^1].frame;

                    // Use frame-based comparison for precise frame alignment
                    // Events snap to frames in Animation Window
                    if (frame != lastFrame)
                    {
                        ProcessAccumulator(accumulator, clusters);
                    }
                }

                accumulator.Add((frame, i));
            }

            ProcessAccumulator(accumulator, clusters);

            return pooledClusters;
        }

        void ProcessAccumulator(List<(int frame, int index)> accumulator, Dictionary<int, EventCluster> clusters)
        {
            if (accumulator.Count == 0) return;

            var cluster = new EventCluster
            {
                firstIndex = accumulator[0].index,
                lastIndex = accumulator[^1].index,
                index = accumulator[0].index,
                indexCount = accumulator.Count
            };
            clusters[accumulator[0].frame] = cluster;
            accumulator.Clear();
        }

        // Cycle to the next event in a cluster (Phase 3)
        void CycleCluster(int clusterFrame, in EventCluster cluster, AnimationEvent[] events, GameObject animated, AnimationClip clip)
        {
            if (cluster.indexCount < 2)
                return;

            if (cluster.index < 0 || cluster.index >= events.Length)
                return;

            // Cycle back at first index if at the last index of the cluster.
            if (cluster.index == cluster.lastIndex)
            {
                EditEvent(animated, clip, cluster.firstIndex);
                return;
            }

            // Cycle to next event in the cluster
            for (int i = cluster.index + 1; i < cluster.lastIndex; ++i)
            {
                var frame = AnimationKeyTime.Time(events[i].time, clip.frameRate).frame;
                if (frame == clusterFrame)
                {
                    EditEvent(animated, clip, i);
                    return;
                }
            }

            // If no event was found in cycle, fall back to last index
            EditEvent(animated, clip, cluster.lastIndex);
        }

        void UpdateInstantTooltip(Rect rect, float frameRate, GameObject root, AnimationEvent[] events, Dictionary<int, EventCluster> clusters, Rect[] hitRects)
        {
            if (m_HoverEvent >= 0 && m_HoverEvent < events.Length)
            {
                var animationEvent = events[m_HoverEvent];
                var frame = AnimationKeyTime.Time(animationEvent.time, frameRate).frame;

                // Find which cluster this event belongs to
                if (clusters.TryGetValue(frame, out var hoveredCluster) &&
                    hoveredCluster.indexCount > 1)
                {
                    // Show cluster count in tooltip
                    m_InstantTooltipText = $"Multiple events ({hoveredCluster.indexCount})";
                }
                else
                {
                    m_InstantTooltipText = AnimationEventWrapperInspector.FormatEvent(root, animationEvent);
                }

                m_InstantTooltipPoint = new Vector2(hitRects[m_HoverEvent].xMin + (int)(hitRects[m_HoverEvent].width / 2) + rect.x + k_TooltipOffset.x, rect.yMax + k_TooltipOffset.y);
            }
        }

        private struct EventLineContextMenuObject
        {
            public GameObject m_Animated;
            public AnimationClip m_Clip;
            public float m_Time;
            public int m_Index;
            public bool[] m_Selected;

            public EventLineContextMenuObject(GameObject animated, AnimationClip clip, float time, int index, bool[] selected)
            {
                m_Animated = animated;
                m_Clip = clip;
                m_Time = time;
                m_Index = index;
                m_Selected = selected;
            }
        }

        internal bool HasSelectedEvents => m_HasSelectedEvents;

        public static void AddEvent(float time, GameObject gameObject, AnimationClip animationClip)
        {
            AnimationEventWrapper awEvent = AnimationEventWrapper.CreateAndEdit(gameObject, animationClip, time);
            Selection.activeObject = awEvent;
        }

        public void EditEvents(GameObject gameObject, AnimationClip clip, bool[] selectedIndices)
        {
            List<AnimationEventWrapper> awEvents = new List<AnimationEventWrapper>();

            for (int index = 0; index < selectedIndices.Length; ++index)
            {
                if (selectedIndices[index])
                    awEvents.Add(AnimationEventWrapper.Edit(gameObject, clip, index));
            }

            if (awEvents.Count > 0)
            {
                Selection.objects = awEvents.ToArray();
            }
            else
            {
                ClearSelection();
            }
        }

        public void EditEvent(GameObject gameObject, AnimationClip clip, int index)
        {
            AnimationEventWrapper awEvent = AnimationEventWrapper.Edit(gameObject, clip, index);
            Selection.activeObject = awEvent;
        }

        public void ClearSelection()
        {
            // Do not unecessarily clear selection.  Only clear if selection already is animation window event.
            if (Selection.activeObject is AnimationEventWrapper)
                Selection.activeObject = null;
        }

        public void DeleteEvents(AnimationClip clip, bool[] deleteIndices)
        {
            bool deletedAny = false;

            List<AnimationEvent> eventList = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
            for (int i = eventList.Count - 1; i >= 0; i--)
            {
                if (deleteIndices[i])
                {
                    eventList.RemoveAt(i);
                    deletedAny = true;
                }
            }

            if (deletedAny)
            {
                Undo.RegisterCompleteObjectUndo(clip, "Delete Event");

                AnimationUtility.SetAnimationEvents(clip, eventList.ToArray());
                Selection.objects = Array.Empty<AnimationEventWrapper>();

                m_DirtyTooltip = true;
            }
        }

        void CopyEvents(AnimationClip clip, bool[] selected, int explicitIndex = -1)
        {
            var allEvents = new List<AnimationEvent>(AnimationUtility.GetAnimationEvents(clip));
            AnimationEventsClipboard.CopyEvents(allEvents, selected, explicitIndex);

            // Animation keyframes right now do not go through regular clipboard machinery,
            // so when copying Events, make sure Keyframes are cleared from the clipboard, or things
            // get confusing.
            AnimationWindowState.ClearKeyframeClipboard();
        }

        internal bool CanPaste() => AnimationEventsClipboard.CanPaste();

        internal void PasteEvents(GameObject animated, IAnimationWindowClip clipInterface, float time)
        {
            // This is hardcoded to work with animation clips.
            // Events are not edited like this in Motion.
            var clip = clipInterface as AnimationWindowBuiltin.AnimationWindowClip;
            if (clip == null)
                return;

            PasteEvents(animated, clip.animationClip, time);
        }

        void PasteEvents(GameObject animated, AnimationClip clip, float time)
        {
            var oldEvents = AnimationUtility.GetAnimationEvents(clip);
            var newEvents = AnimationEventsClipboard.AddPastedEvents(oldEvents, time, out var selected);
            if (newEvents == null)
                return;

            Undo.RegisterCompleteObjectUndo(clip, "Paste Events");
            EditEvents(animated, clip, selected);
            AnimationUtility.SetAnimationEvents(clip, newEvents);
            m_DirtyTooltip = true;
        }

        public void EventLineGUI(Rect rect, AnimationWindowState state)
        {
            //  We only display and manipulate animation events from the main
            //  game object in selection.  If we ever want to update to handle
            //  a multiple selection, a single timeline might not be sufficient...

            // TODO. This is hardcoded to work with animation clips.
            var clip = state.selection.clip as AnimationWindowBuiltin.AnimationWindowClip;
            if (clip == null)
                return;

            AnimationClip animationClip = clip.animationClip;
            GameObject animated = state.activeRootGameObject;

            GUI.BeginGroup(rect);
            Color backupCol = GUI.color;

            Rect eventLineRect = new Rect(0, 0, rect.width, rect.height);

            float mousePosTime = Mathf.Max(Mathf.RoundToInt(state.PixelToTime(Event.current.mousePosition.x, rect) * state.frameRate) / state.frameRate, 0.0f);

            // Draw events
            if (animationClip != null)
            {
                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(animationClip);

                // Build clusters for events at the same time
                using var pooledObjects = BuildClusters(events, state.frameRate, out var clusters);

                // Calculate rects for clusters
                // Map from original event index to cluster rect
                Rect[] drawRects = new Rect[events.Length];

                for (int eventIndex = 0; eventIndex < events.Length; ++eventIndex)
                {
                    var animationEvent = events[eventIndex];
                    var frame = AnimationKeyTime.Time(animationEvent.time, state.frameRate).frame;

                    // Important to take floor of positions of GUI stuff to get pixel correct alignment
                    float keypos = Mathf.Floor(state.FrameToPixel(frame, rect));

                    drawRects[eventIndex] = new Rect(
                        keypos - k_EventMarkerSize.x / 2 + 1,
                        0,  // Align to top of timeline
                        k_EventMarkerSize.x,
                        k_EventMarkerSize.y);
                }

                // Store tooltip info (with cluster awareness)
                if (m_DirtyTooltip)
                {
                    UpdateInstantTooltip(rect, state.frameRate, animated, events, clusters, drawRects);
                    m_DirtyTooltip = false;
                }

                bool[] selectedEvents = new bool[events.Length];
                m_HasSelectedEvents = false;

                var selectedEventWrappers = Selection.GetFiltered<AnimationEventWrapper>(SelectionMode.Unfiltered);
                foreach (AnimationEventWrapper eventWrapper in selectedEventWrappers)
                {
                    if (eventWrapper.eventIndex >= 0 && eventWrapper.eventIndex < selectedEvents.Length)
                    {
                        selectedEvents[eventWrapper.eventIndex] = true;
                        m_HasSelectedEvents = true;

                        // To make sure top most event in a cluster always remains selected whenever
                        // any event in that cluster is selected. Only performed during repaint
                        // to avoid changing selection.
                        if (Event.current.type == EventType.Repaint)
                        {
                            var frame = AnimationKeyTime.Time(events[eventWrapper.eventIndex].time, state.frameRate).frame;
                            if (clusters.TryGetValue(frame, out var cluster))
                            {
                                selectedEvents[cluster.lastIndex] = true;
                            }
                        }
                    }
                }

                Vector2 offset = Vector2.zero;
                int clickedIndex;
                float startSelection, endSelection;

                HighLevelEvent hEvent = EditorGUIExt.MultiSelection(
                    rect,
                    drawRects,
                    Styles.eventMarker,
                    drawRects,
                    ref selectedEvents,
                    null,
                    out clickedIndex,
                    out offset,
                    out startSelection,
                    out endSelection,
                    GUIStyle.none
                );

                // Draw "+" overlay for clusters with multiple events
                foreach (var (_, cluster) in clusters)
                {
                    if (cluster.indexCount > 1)
                    {
                        int firstIndex = cluster.firstIndex;
                        Rect clusterRect = drawRects[firstIndex];

                        GUI.DrawTexture(clusterRect, Styles.eventMarkerMultiOverlay.image, ScaleMode.ScaleToFit);
                    }
                }

                if (hEvent != HighLevelEvent.None)
                {
                    switch (hEvent)
                    {
                        case HighLevelEvent.BeginDrag:
                            m_EventsAtMouseDown = events;
                            m_EventTimes = new float[events.Length];
                            for (int i = 0; i < events.Length; i++)
                                m_EventTimes[i] = events[i].time;
                            m_IsDragging = true;
                            break;
                        case HighLevelEvent.EndDrag:
                            m_IsDragging = false;
                            break;
                        case HighLevelEvent.SelectionChanged:
                            state.ClearKeySelections();

                            // Check if this is a click on a cluster for cycling behavior
                            if (clickedIndex >= 0 && clickedIndex < events.Length)
                            {
                                var animationEvent = events[clickedIndex];
                                var frame = AnimationKeyTime.Time(animationEvent.time, state.frameRate).frame;

                                // Find last selected cluster.
                                var lastSelectedCluster = new EventCluster();
                                if (selectedEventWrappers.Length == 1)
                                {
                                    AnimationEventWrapper awe = selectedEventWrappers[0];
                                    if (awe.eventIndex >= 0 || awe.eventIndex < events.Length)
                                    {
                                        var selectedEventFrame = AnimationKeyTime.Time(events[awe.eventIndex].time, state.frameRate).frame;
                                        if (clusters.TryGetValue(selectedEventFrame, out lastSelectedCluster))
                                            lastSelectedCluster.index = awe.eventIndex;
                                    }
                                }

                                // Check if clicking on the same cluster
                                bool isSameClusterClick =
                                    clusters.TryGetValue(frame, out var clickedCluster) &&
                                    lastSelectedCluster.firstIndex == clickedCluster.firstIndex;

                                if (isSameClusterClick && lastSelectedCluster.indexCount > 1)
                                {
                                    // Cycle to next event in cluster
                                    CycleCluster(frame, lastSelectedCluster, events, animated, animationClip);
                                }
                                else
                                {
                                    // Regular selection
                                    EditEvents(animated, animationClip, selectedEvents);
                                }
                            }
                            else
                            {
                                EditEvents(animated, animationClip, selectedEvents);
                            }
                            break;
                        case HighLevelEvent.Delete:
                            DeleteEvents(animationClip, selectedEvents);
                            break;
                        case HighLevelEvent.Copy:
                            CopyEvents(animationClip, selectedEvents);
                            break;
                        case HighLevelEvent.Paste:
                            PasteEvents(animated, animationClip, state.currentTime);
                            break;

                        case HighLevelEvent.DoubleClick:

                            if (clickedIndex != -1)
                                EditEvents(animated, animationClip, selectedEvents);
                            else
                                EventLineContextMenuAdd(new EventLineContextMenuObject(animated, animationClip, mousePosTime, -1, selectedEvents));
                            break;
                        case HighLevelEvent.Drag:
                            for (int i = events.Length - 1; i >= 0; i--)
                            {
                                if (selectedEvents[i])
                                {
                                    AnimationEvent evt = m_EventsAtMouseDown[i];
                                    evt.time = m_EventTimes[i] + offset.x * state.PixelDeltaToTime(rect);
                                    evt.time = Mathf.Max(0.0F, evt.time);
                                    evt.time = Mathf.RoundToInt(evt.time * clip.frameRate) / clip.frameRate;
                                }
                            }
                            int[] order = new int[selectedEvents.Length];
                            for (int i = 0; i < order.Length; i++)
                            {
                                order[i] = i;
                            }
                            System.Array.Sort(m_EventsAtMouseDown, order, new EventComparer());
                            bool[] selectedOld = (bool[])selectedEvents.Clone();
                            float[] timesOld = (float[])m_EventTimes.Clone();
                            for (int i = 0; i < order.Length; i++)
                            {
                                selectedEvents[i] = selectedOld[order[i]];
                                m_EventTimes[i] = timesOld[order[i]];
                            }

                            // Update selection to reflect new order.
                            EditEvents(animated, animationClip, selectedEvents);

                            Undo.RegisterCompleteObjectUndo(animationClip, "Move Event");
                            AnimationUtility.SetAnimationEvents(animationClip, m_EventsAtMouseDown);
                            m_DirtyTooltip = true;
                            break;
                        case HighLevelEvent.ContextClick:
                            CreateContextMenu(animated, animationClip, events[clickedIndex].time, clickedIndex, selectedEvents);

                            // Mouse may move while context menu is open - make sure instant tooltip is handled
                            m_InstantTooltipText = null;
                            m_DirtyTooltip = true;
                            state.Repaint();
                            break;
                    }
                }

                if (m_IsDragging)
                {
                    m_HoverEvent = -1;
                    m_InstantTooltipText = "";
                }
                else
                    CheckRectsOnMouseMove(rect, state.frameRate, animated, events, clusters, drawRects);

                // Bring up menu when context-clicking on an empty timeline area (context-clicking on events is handled above)
                if (Event.current.type == EventType.ContextClick && eventLineRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    CreateContextMenu(animated, animationClip, mousePosTime, -1, selectedEvents);
                }
            }

            GUI.color = backupCol;
            GUI.EndGroup();
        }

        void CreateContextMenu(GameObject animatedGo, AnimationClip clip, float time, int eventIndex, bool[] selectedEvents)
        {
            GenericMenu menu = new GenericMenu();
            var contextData = new EventLineContextMenuObject(animatedGo, clip, time, eventIndex, selectedEvents);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedCount = selectedEvents.Count(selected => selected);
#pragma warning restore UA2001

            menu.AddItem(Styles.textAddEvent, false, EventLineContextMenuAdd, contextData);
            if (selectedCount > 0 || eventIndex != -1)
            {
                menu.AddItem(selectedCount > 1 ? Styles.textDeleteEvents : Styles.textDeleteEvent, false, EventLineContextMenuDelete, contextData);
                menu.AddItem(Styles.textCopyEvents, false, EventLineContextMenuCopy, contextData);
            }
            else
            {
                menu.AddDisabledItem(Styles.textDeleteEvents);
                menu.AddDisabledItem(Styles.textCopyEvents);
            }
            if (AnimationEventsClipboard.CanPaste())
                menu.AddItem(Styles.textPasteEvents, false, EventLineContextMenuPaste, contextData);
            else
                menu.AddDisabledItem(Styles.textPasteEvents);
            menu.ShowAsContext();
        }

        public void DrawInstantTooltip(Rect position)
        {
            if (!string.IsNullOrEmpty(m_InstantTooltipText))
            {
                // Draw body of tooltip
                GUIStyle style = (GUIStyle)"AnimationEventTooltip";

                // TODO: Move to editor_resources
                style.contentOffset = new Vector2(0f, 0f);
                style.overflow = new RectOffset(10, 10, 0, 0);

                Vector2 size = style.CalcSize(new GUIContent(m_InstantTooltipText));
                Rect rect = new Rect(m_InstantTooltipPoint.x - size.x * .5f, m_InstantTooltipPoint.y + 24, size.x, size.y);

                // Right align tooltip rect if it would otherwise exceed the bounds of the window
                if (rect.xMax > position.width)
                    rect.x = position.width - rect.width;

                GUI.Label(rect, m_InstantTooltipText, style);

                // Draw arrow of tooltip
                rect = new Rect(m_InstantTooltipPoint.x - 33, m_InstantTooltipPoint.y, 7, 25);
                GUI.Label(rect, "", "AnimationEventTooltipArrow");
            }
        }

        public void EventLineContextMenuAdd(object obj)
        {
            EventLineContextMenuObject eventObj = (EventLineContextMenuObject)obj;
            AddEvent(eventObj.m_Time, eventObj.m_Animated, eventObj.m_Clip);
        }

        public void EventLineContextMenuEdit(object obj)
        {
            EventLineContextMenuObject eventObj = (EventLineContextMenuObject)obj;

            if (Array.Exists(eventObj.m_Selected, selected => selected))
            {
                EditEvents(eventObj.m_Animated, eventObj.m_Clip, eventObj.m_Selected);
            }
            else if (eventObj.m_Index >= 0)
            {
                EditEvent(eventObj.m_Animated, eventObj.m_Clip, eventObj.m_Index);
            }
        }

        public void EventLineContextMenuDelete(object obj)
        {
            EventLineContextMenuObject eventObj = (EventLineContextMenuObject)obj;
            AnimationClip clip = eventObj.m_Clip;
            if (clip == null)
                return;

            int clickedIndex = eventObj.m_Index;

            // If a selection already exists, delete selection instead of clicked index
            if (Array.Exists(eventObj.m_Selected, selected => selected))
            {
                DeleteEvents(clip, eventObj.m_Selected);
            }
            // Else, only delete the clicked animation event
            else if (clickedIndex >= 0)
            {
                bool[] deleteIndices = new bool[eventObj.m_Selected.Length];
                deleteIndices[clickedIndex] = true;
                DeleteEvents(clip, deleteIndices);
            }
        }

        void EventLineContextMenuCopy(object obj)
        {
            var ctx = (EventLineContextMenuObject)obj;
            var clip = ctx.m_Clip;
            if (clip != null)
                CopyEvents(clip, ctx.m_Selected, ctx.m_Index);
        }

        void EventLineContextMenuPaste(object obj)
        {
            var ctx = (EventLineContextMenuObject)obj;
            AnimationClip clip = ctx.m_Clip;
            if (clip != null)
                PasteEvents(ctx.m_Animated, clip, ctx.m_Time);
        }

        void CheckRectsOnMouseMove(Rect eventLineRect, float frameRate, GameObject root, AnimationEvent[] events, Dictionary<int, EventCluster> clusters, Rect[] hitRects)
        {
            Vector2 mouse = Event.current.mousePosition;
            bool hasFound = false;

            if (events.Length == hitRects.Length)
            {
                for (int i = hitRects.Length - 1; i >= 0; i--)
                {
                    if (hitRects[i].Contains(mouse))
                    {
                        hasFound = true;
                        if (m_HoverEvent != i)
                        {
                            m_HoverEvent = i;
                            UpdateInstantTooltip(eventLineRect, frameRate, root, events, clusters, hitRects);
                            m_DirtyTooltip = true;
                        }

                        break;
                    }
                }
            }
            if (!hasFound)
            {
                m_HoverEvent = -1;
                m_InstantTooltipText = "";
            }
        }
    }
} // namespace
