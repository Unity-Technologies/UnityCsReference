// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEngine.UIElements.Experimental
{
    struct EventDebuggerLogCall : IDisposable
    {
        private readonly Delegate m_Callback;
        private readonly EventBase m_Event;
        private readonly long m_Start;
        private readonly bool m_IsPropagationStopped;
        private readonly bool m_IsImmediatePropagationStopped;
        private readonly bool m_IsDefaultPrevented;
        public EventDebuggerLogCall(Delegate callback, EventBase evt)
        {
            m_Callback = callback;
            m_Event = evt;

            m_Start = (long)(Time.realtimeSinceStartup * 1000.0f);
            m_IsPropagationStopped = evt.isPropagationStopped;
            m_IsImmediatePropagationStopped = evt.isImmediatePropagationStopped;
            m_IsDefaultPrevented = evt.isDefaultPrevented;
        }

        public void Dispose()
        {
            if (m_Event != null && m_Event.log)
            {
                IPanel panel = (m_Event.target as VisualElement)?.panel;
                IEventHandler capture = panel?.GetCapturingElement(PointerId.mousePointerId);

                m_Event.eventLogger.LogCall(GetCallbackHashCode(), GetCallbackName(), m_Event,
                    m_IsPropagationStopped != m_Event.isPropagationStopped,
                    m_IsImmediatePropagationStopped != m_Event.isImmediatePropagationStopped,
                    m_IsDefaultPrevented != m_Event.isDefaultPrevented,
                    (long)(Time.realtimeSinceStartup * 1000.0f) - m_Start, capture);
            }
        }

        private string GetCallbackName()
        {
            if (m_Callback == null)
            {
                return "No callback";
            }

            if (m_Callback.Target != null)
            {
                return m_Callback.Target.GetType().FullName + "." + m_Callback.Method.Name;
            }

            if (m_Callback.Method.DeclaringType != null)
            {
                return m_Callback.Method.DeclaringType.FullName + "." + m_Callback.Method.Name;
            }

            return m_Callback.Method.Name;
        }

        private int GetCallbackHashCode()
        {
            return m_Callback?.GetHashCode() ?? 0;
        }

    }

    struct EventDebuggerLogIMGUICall : IDisposable
    {
        private readonly EventBase m_Event;
        private readonly long m_Start;
        public EventDebuggerLogIMGUICall(EventBase evt)
        {
            m_Event = evt;
            m_Start = (long)(Time.realtimeSinceStartup * 1000.0f);
        }

        public void Dispose()
        {
            if (m_Event != null && m_Event.log)
            {
                IPanel panel = (m_Event.target as VisualElement)?.panel;
                IEventHandler capture = panel?.GetCapturingElement(PointerId.mousePointerId);
                m_Event.eventLogger.LogIMGUICall(m_Event,
                    (long)(Time.realtimeSinceStartup * 1000.0f) - m_Start, capture);
            }
        }
    }

    struct EventDebuggerLogExecuteDefaultAction : IDisposable
    {
        private readonly EventBase m_Event;
        private readonly long m_Start;
        public EventDebuggerLogExecuteDefaultAction(EventBase evt)
        {
            m_Event = evt;
            m_Start = (long)(Time.realtimeSinceStartup * 1000.0f);
        }

        public void Dispose()
        {
            if (m_Event != null && m_Event.log)
            {
                IPanel panel = (m_Event.target as VisualElement)?.panel;
                IEventHandler capture = panel?.GetCapturingElement(PointerId.mousePointerId);
                m_Event.eventLogger.LogExecuteDefaultAction(m_Event, m_Event.propagationPhase,
                    (long)(Time.realtimeSinceStartup * 1000.0f) - m_Start, capture);
            }
        }
    }

    class EventDebugger
    {
        public IPanel panel
        {
            get { return panelDebug?.panel; }
            set
            {
                /* Ignore in editor */
            }
        }

        IPanelDebug m_PanelDebug;
        public IPanelDebug panelDebug
        {
            get { return m_PanelDebug; }
            set
            {
                m_PanelDebug = value;
                if (m_PanelDebug != null)
                {
                    if (!m_EventTypeProcessedCount.ContainsKey(panel))
                        m_EventTypeProcessedCount.Add(panel, new Dictionary<long, int>());
                }
            }
        }

        public bool isReplaying { get; private set; }
        public float playbackSpeed { get; set; } = 1.0f;
        public bool isPlaybackPaused { get; set; }

        public void UpdateModificationCount()
        {
            if (panel == null)
                return;

            if (!m_ModificationCount.TryGetValue(panel, out var count))
            {
                count = 0;
            }

            count++;
            m_ModificationCount[panel] = count;
        }

        public void BeginProcessEvent(EventBase evt, IEventHandler mouseCapture)
        {
            AddBeginProcessEvent(evt, mouseCapture);
            UpdateModificationCount();
        }

        public void EndProcessEvent(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            AddEndProcessEvent(evt, duration, mouseCapture);
            UpdateModificationCount();
        }

        public void LogCall(int cbHashCode, string cbName, EventBase evt, bool propagationHasStopped, bool immediatePropagationHasStopped, bool defaultHasBeenPrevented, long duration, IEventHandler mouseCapture)
        {
            AddCallObject(cbHashCode, cbName, evt, propagationHasStopped, immediatePropagationHasStopped, defaultHasBeenPrevented, duration, mouseCapture);
            UpdateModificationCount();
        }

        public void LogIMGUICall(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            AddIMGUICall(evt, duration, mouseCapture);
            UpdateModificationCount();
        }

        public void LogExecuteDefaultAction(EventBase evt, PropagationPhase phase, long duration, IEventHandler mouseCapture)
        {
            AddExecuteDefaultAction(evt, phase, duration, mouseCapture);
            UpdateModificationCount();
        }

        public static void LogPropagationPaths(EventBase evt, PropagationPaths paths)
        {
            if (evt.log)
            {
                evt.eventLogger.LogPropagationPathsInternal(evt, paths);
            }
        }

        void LogPropagationPathsInternal(EventBase evt, PropagationPaths paths)
        {
            var pathsCopy = paths == null ? new PropagationPaths() : new PropagationPaths(paths);
            AddPropagationPaths(evt, pathsCopy);
            UpdateModificationCount();
        }

        public List<EventDebuggerCallTrace> GetCalls(IPanel panel, EventDebuggerEventRecord evt = null)
        {
            if (!m_EventCalledObjects.TryGetValue(panel, out var list))
            {
                return null;
            }

            if ((evt != null) && (list != null))
            {
                List<EventDebuggerCallTrace> filteredList = new List<EventDebuggerCallTrace>();
                foreach (var callObject in list)
                {
                    if (callObject.eventBase.eventId == evt.eventId)
                    {
                        filteredList.Add(callObject);
                    }
                }

                list = filteredList;
            }

            return list;
        }

        public List<EventDebuggerDefaultActionTrace> GetDefaultActions(IPanel panel, EventDebuggerEventRecord evt = null)
        {
            if (!m_EventDefaultActionObjects.TryGetValue(panel, out var list))
            {
                return null;
            }

            if ((evt != null) && (list != null))
            {
                List<EventDebuggerDefaultActionTrace> filteredList = new List<EventDebuggerDefaultActionTrace>();
                foreach (var defaultActionObject in list)
                {
                    if (defaultActionObject.eventBase.eventId == evt.eventId)
                    {
                        filteredList.Add(defaultActionObject);
                    }
                }

                list = filteredList;
            }

            return list;
        }

        public List<EventDebuggerPathTrace> GetPropagationPaths(IPanel panel, EventDebuggerEventRecord evt = null)
        {
            if (!m_EventPathObjects.TryGetValue(panel, out var list))
            {
                return null;
            }

            if ((evt != null) && (list != null))
            {
                List<EventDebuggerPathTrace> filteredList = new List<EventDebuggerPathTrace>();
                foreach (var pathObject in list)
                {
                    if (pathObject.eventBase.eventId == evt.eventId)
                    {
                        filteredList.Add(pathObject);
                    }
                }

                list = filteredList;
            }

            return list;
        }

        public List<EventDebuggerTrace> GetBeginEndProcessedEvents(IPanel panel, EventDebuggerEventRecord evt = null)
        {
            if (!m_EventProcessedEvents.TryGetValue(panel, out var list))
            {
                return null;
            }

            if ((evt != null) && (list != null))
            {
                List<EventDebuggerTrace> filteredList = new List<EventDebuggerTrace>();
                foreach (var defaultActionObject in list)
                {
                    if (defaultActionObject.eventBase.eventId == evt.eventId)
                    {
                        filteredList.Add(defaultActionObject);
                    }
                }

                list = filteredList;
            }

            return list;
        }

        public long GetModificationCount(IPanel panel)
        {
            if (panel == null)
                return -1;

            if (!m_ModificationCount.TryGetValue(panel, out var modificationCount))
            {
                modificationCount = -1;
            }

            return modificationCount;
        }

        public void ClearLogs()
        {
            UpdateModificationCount();

            if (panel == null)
            {
                m_EventCalledObjects.Clear();
                m_EventDefaultActionObjects.Clear();
                m_EventPathObjects.Clear();
                m_EventProcessedEvents.Clear();
                m_StackOfProcessedEvent.Clear();
                m_EventTypeProcessedCount.Clear();
                return;
            }

            m_EventCalledObjects.Remove(panel);
            m_EventDefaultActionObjects.Remove(panel);
            m_EventPathObjects.Remove(panel);
            m_EventProcessedEvents.Remove(panel);
            m_StackOfProcessedEvent.Remove(panel);

            if (m_EventTypeProcessedCount.TryGetValue(panel, out var eventTypeProcessedForPanel))
                eventTypeProcessedForPanel.Clear();
        }

        public void SaveReplaySessionFromSelection(string path, List<EventDebuggerEventRecord> eventList)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var recordSave = new EventDebuggerRecordList() { eventList = eventList };
            var json = JsonUtility.ToJson(recordSave);
            File.WriteAllText(path, json);
            Debug.Log($"Saved under: {path}");
        }

        public EventDebuggerRecordList LoadReplaySession(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var fileContent = File.ReadAllText(path);
            return JsonUtility.FromJson<EventDebuggerRecordList>(fileContent);
        }

        public IEnumerator ReplayEvents(IEnumerable<EventDebuggerEventRecord> eventBases, Action<int, int> refreshList)
        {
            if (eventBases == null)
                yield break;

            isReplaying = true;
            var doReplay = DoReplayEvents(eventBases, refreshList);
            while (doReplay.MoveNext())
            {
                yield return null;
            }
        }

        public void StopPlayback()
        {
            isReplaying = false;
            isPlaybackPaused = false;
        }

        private IEnumerator DoReplayEvents(IEnumerable<EventDebuggerEventRecord> eventBases, Action<int, int> refreshList)
        {
            var sortedEvents = eventBases.OrderBy(e => e.timestamp).ToList();
            var sortedEventsCount = sortedEvents.Count;

            IEnumerator AwaitForNextEvent(int currentIndex)
            {
                if (currentIndex == sortedEvents.Count - 1)
                    yield break;

                var deltaTimestampMs = sortedEvents[currentIndex + 1].timestamp - sortedEvents[currentIndex].timestamp;

                var timeMs = 0.0f;
                while (timeMs < deltaTimestampMs)
                {
                    if (isPlaybackPaused)
                    {
                        yield return null;
                    }
                    else
                    {
                        var time = Panel.TimeSinceStartupMs();
                        yield return null;
                        var delta = Panel.TimeSinceStartupMs() - time;
                        timeMs += delta * playbackSpeed;
                    }
                }
            }

            for (var i = 0; i < sortedEventsCount; i++)
            {
                if (!isReplaying)
                    break;

                var eventBase = sortedEvents[i];
                var newEvent = new Event
                {
                    button = eventBase.button,
                    clickCount = eventBase.clickCount,
                    modifiers = eventBase.modifiers,
                    mousePosition = eventBase.mousePosition,
                };

                if (eventBase.eventTypeId == MouseMoveEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseMove;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseMove), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == MouseDownEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseDown;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseDown), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == MouseUpEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseUp;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseUp), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == ContextClickEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.ContextClick;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.ContextClick), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == MouseEnterWindowEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseEnterWindow;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseEnterWindow), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == MouseLeaveWindowEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseLeaveWindow;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseLeaveWindow), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == PointerMoveEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseMove;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseMove), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == PointerDownEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseDown;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseDown), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == PointerUpEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.MouseUp;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.MouseUp), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == WheelEvent.TypeId() && eventBase.hasUnderlyingPhysicalEvent)
                {
                    newEvent.type = EventType.ScrollWheel;
                    newEvent.delta = eventBase.delta;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.ScrollWheel), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == KeyDownEvent.TypeId())
                {
                    newEvent.type = EventType.KeyDown;
                    newEvent.character = eventBase.character;
                    newEvent.keyCode = eventBase.keyCode;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.KeyDown), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == KeyUpEvent.TypeId())
                {
                    newEvent.type = EventType.KeyUp;
                    newEvent.character = eventBase.character;
                    newEvent.keyCode = eventBase.keyCode;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.KeyUp), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == NavigationMoveEvent.TypeId())
                {
                    panel.dispatcher.Dispatch(NavigationMoveEvent.GetPooled(eventBase.navigationDirection, eventBase.deviceType, eventBase.modifiers), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == NavigationSubmitEvent.TypeId())
                {
                    panel.dispatcher.Dispatch(NavigationSubmitEvent.GetPooled(eventBase.deviceType, eventBase.modifiers), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == NavigationCancelEvent.TypeId())
                {
                    panel.dispatcher.Dispatch(NavigationCancelEvent.GetPooled(eventBase.deviceType, eventBase.modifiers), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == DragUpdatedEvent.TypeId())
                {
                    newEvent.type = EventType.DragUpdated;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.DragUpdated), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == DragPerformEvent.TypeId())
                {
                    newEvent.type = EventType.DragPerform;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.DragPerform), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == DragExitedEvent.TypeId())
                {
                    newEvent.type = EventType.DragExited;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.DragExited), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == ValidateCommandEvent.TypeId())
                {
                    newEvent.type = EventType.ValidateCommand;
                    newEvent.commandName = eventBase.commandName;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.ValidateCommand), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    newEvent.type = EventType.ExecuteCommand;
                    newEvent.commandName = eventBase.commandName;
                    panel.dispatcher.Dispatch(UIElementsUtility.CreateEvent(newEvent, EventType.ExecuteCommand), panel,
                        DispatchMode.Default);
                }
                else if (eventBase.eventTypeId == IMGUIEvent.TypeId())
                {
                    Debug.Log("Skipped IMGUI event (" + eventBase.eventBaseName + "): " + eventBase);
                    var awaitSkipped = AwaitForNextEvent(i);
                    while (awaitSkipped.MoveNext()) yield return null;
                    continue;
                }
                else
                {
                    Debug.Log("Skipped event (" + eventBase.eventBaseName + "): " + eventBase);
                    var awaitSkipped = AwaitForNextEvent(i);
                    while (awaitSkipped.MoveNext()) yield return null;
                    continue;
                }

                refreshList?.Invoke(i, sortedEventsCount);

                Debug.Log($"Replayed event {eventBase.eventId.ToString()} ({eventBase.eventBaseName}): {newEvent}");
                var await = AwaitForNextEvent(i);
                while (await.MoveNext()) yield return null;
            }

            isReplaying = false;
        }

        internal struct HistogramRecord
        {
            public long count;
            public long duration;
        }

        public Dictionary<string, HistogramRecord> ComputeHistogram(List<EventDebuggerEventRecord> eventBases)
        {
            if (panel == null || !m_EventProcessedEvents.TryGetValue(panel, out var list))
                return null;

            if (list == null)
                return null;

            Dictionary<string, HistogramRecord> histogram = new Dictionary<string, HistogramRecord>();
            foreach (var callObject in list)
            {
                if (eventBases == null || eventBases.Count == 0 || eventBases.Contains(callObject.eventBase))
                {
                    var key = callObject.eventBase.eventBaseName;
                    var totalDuration = callObject.duration;
                    long totalCount = 1;
                    if (histogram.TryGetValue(key, out var currentHistogramRecord))
                    {
                        totalDuration += currentHistogramRecord.duration;
                        totalCount += currentHistogramRecord.count;
                    }

                    histogram[key] = new HistogramRecord { count = totalCount, duration = totalDuration };
                }
            }

            return histogram;
        }

        // Call Object
        Dictionary<IPanel, List<EventDebuggerCallTrace>> m_EventCalledObjects;
        Dictionary<IPanel, List<EventDebuggerDefaultActionTrace>> m_EventDefaultActionObjects;
        Dictionary<IPanel, List<EventDebuggerPathTrace>> m_EventPathObjects;
        Dictionary<IPanel, List<EventDebuggerTrace>> m_EventProcessedEvents;
        Dictionary<IPanel, Stack<EventDebuggerTrace>> m_StackOfProcessedEvent;
        Dictionary<IPanel, Dictionary<long, int>> m_EventTypeProcessedCount;

        public Dictionary<long, int> eventTypeProcessedCount => m_EventTypeProcessedCount.TryGetValue(panel, out var eventTypeProcessedCountForPanel) ? eventTypeProcessedCountForPanel : null;

        readonly Dictionary<IPanel, long> m_ModificationCount;
        readonly bool m_Log;

        public bool suspended { get; set; }

        // Methods
        public EventDebugger()
        {
            m_EventCalledObjects = new Dictionary<IPanel, List<EventDebuggerCallTrace>>();
            m_EventDefaultActionObjects = new Dictionary<IPanel, List<EventDebuggerDefaultActionTrace>>();
            m_EventPathObjects = new Dictionary<IPanel, List<EventDebuggerPathTrace>>();
            m_StackOfProcessedEvent = new Dictionary<IPanel, Stack<EventDebuggerTrace>>();
            m_EventProcessedEvents = new Dictionary<IPanel, List<EventDebuggerTrace>>();
            m_EventTypeProcessedCount = new Dictionary<IPanel, Dictionary<long, int>>();
            m_ModificationCount = new Dictionary<IPanel, long>();
            m_Log = true;
        }

        void AddCallObject(int cbHashCode, string cbName, EventBase evt, bool propagationHasStopped, bool immediatePropagationHasStopped, bool defaultHasBeenPrevented, long duration, IEventHandler mouseCapture)
        {
            if (suspended)
                return;

            if (m_Log)
            {
                var callObject = new EventDebuggerCallTrace(panel, evt, cbHashCode, cbName, propagationHasStopped, immediatePropagationHasStopped, defaultHasBeenPrevented, duration, mouseCapture);

                if (!m_EventCalledObjects.TryGetValue(panel, out var list))
                {
                    list = new List<EventDebuggerCallTrace>();
                    m_EventCalledObjects.Add(panel, list);
                }

                list.Add(callObject);
            }
        }

        void AddExecuteDefaultAction(EventBase evt, PropagationPhase phase, long duration, IEventHandler mouseCapture)
        {
            if (suspended)
                return;

            if (m_Log)
            {
                var defaultActionObject = new EventDebuggerDefaultActionTrace(panel, evt, phase, duration, mouseCapture);

                if (!m_EventDefaultActionObjects.TryGetValue(panel, out var list))
                {
                    list = new List<EventDebuggerDefaultActionTrace>();
                    m_EventDefaultActionObjects.Add(panel, list);
                }

                list.Add(defaultActionObject);
            }
        }

        void AddPropagationPaths(EventBase evt, PropagationPaths paths)
        {
            if (suspended)
                return;

            if (m_Log)
            {
                var pathObject = new EventDebuggerPathTrace(panel, evt, paths);

                if (!m_EventPathObjects.TryGetValue(panel, out var list))
                {
                    list = new List<EventDebuggerPathTrace>();
                    m_EventPathObjects.Add(panel, list);
                }

                list.Add(pathObject);
            }
        }

        void AddIMGUICall(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            if (suspended)
                return;

            if (m_Log)
            {
                var callObject = new EventDebuggerCallTrace(panel, evt, 0, "OnGUI", false, false, false, duration, mouseCapture);

                if (!m_EventCalledObjects.TryGetValue(panel, out var list))
                {
                    list = new List<EventDebuggerCallTrace>();
                    m_EventCalledObjects.Add(panel, list);
                }

                list.Add(callObject);
            }
        }

        void AddBeginProcessEvent(EventBase evt, IEventHandler mouseCapture)
        {
            if (suspended)
                return;

            var dbgObject = new EventDebuggerTrace(panel, evt, -1, mouseCapture);

            if (!m_StackOfProcessedEvent.TryGetValue(panel, out var stack))
            {
                stack = new Stack<EventDebuggerTrace>();
                m_StackOfProcessedEvent.Add(panel, stack);
            }

            if (!m_EventProcessedEvents.TryGetValue(panel, out var list))
            {
                list = new List<EventDebuggerTrace>();
                m_EventProcessedEvents.Add(panel, list);
            }

            list.Add(dbgObject);
            stack.Push(dbgObject);

            if (!m_EventTypeProcessedCount.TryGetValue(panel, out var eventTypeProcessedCountForPanel))
                return;

            if (!eventTypeProcessedCountForPanel.TryGetValue(dbgObject.eventBase.eventTypeId, out var count))
                count = 0;

            eventTypeProcessedCountForPanel[dbgObject.eventBase.eventTypeId] = count + 1;
        }

        void AddEndProcessEvent(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            if (suspended)
                return;

            bool evtHandled = false;
            if (m_StackOfProcessedEvent.TryGetValue(panel, out var stack))
            {
                if (stack.Count > 0)
                {
                    var dbgObject = stack.Peek();
                    if (dbgObject.eventBase.eventId == evt.eventId)
                    {
                        stack.Pop();
                        dbgObject.duration = duration;

                        // Update the target if it was unknown in AddBeginProcessEvent.
                        if (dbgObject.eventBase.target == null)
                        {
                            dbgObject.eventBase.target = evt.target;
                        }

                        evtHandled = true;
                    }
                }
            }

            if (!evtHandled)
            {
                var dbgObject = new EventDebuggerTrace(panel, evt, duration, mouseCapture);
                if (!m_EventProcessedEvents.TryGetValue(panel, out var list))
                {
                    list = new List<EventDebuggerTrace>();
                    m_EventProcessedEvents.Add(panel, list);
                }

                list.Add(dbgObject);

                if (!m_EventTypeProcessedCount.TryGetValue(panel, out var eventTypeProcessedForPanel))
                    return;

                if (!eventTypeProcessedForPanel.TryGetValue(dbgObject.eventBase.eventTypeId, out var count))
                    count = 0;

                eventTypeProcessedForPanel[dbgObject.eventBase.eventTypeId] = count + 1;
            }
        }

        public static string GetObjectDisplayName(object obj, bool withHashCode = true)
        {
            if (obj == null) return String.Empty;

            var type = obj.GetType();
            var objectName = GetTypeDisplayName(type);
            if (obj is VisualElement)
            {
                VisualElement ve = obj as VisualElement;
                if (!String.IsNullOrEmpty(ve.name))
                {
                    objectName += "#" + ve.name;
                }
            }

            if (withHashCode)
            {
                objectName += " (" + obj.GetHashCode().ToString("x8") + ")";
            }

            return objectName;
        }

        public static string GetTypeDisplayName(Type type)
        {
            return type.IsGenericType ? $"{type.Name.TrimEnd('`', '1')}<{type.GetGenericArguments()[0].Name}>" : type.Name;
        }
    }
}
