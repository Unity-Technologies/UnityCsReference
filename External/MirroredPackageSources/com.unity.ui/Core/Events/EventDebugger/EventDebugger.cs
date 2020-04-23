using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
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
        public IPanel panel { get; set; }

        public void UpdateModificationCount()
        {
            if (panel == null)
                return;

            long count = 0;
            if (m_ModificationCount.ContainsKey(panel))
            {
                count = m_ModificationCount[panel];
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
            List<EventDebuggerCallTrace> list = null;
            if (m_EventCalledObjects.ContainsKey(panel))
            {
                list = m_EventCalledObjects[panel];
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
            List<EventDebuggerDefaultActionTrace> list = null;
            if (m_EventDefaultActionObjects.ContainsKey(panel))
            {
                list = m_EventDefaultActionObjects[panel];
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
            List<EventDebuggerPathTrace> list = null;
            if (m_EventPathObjects.ContainsKey(panel))
            {
                list = m_EventPathObjects[panel];
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
            List<EventDebuggerTrace> list = null;
            if (m_EventProcessedEvents.ContainsKey(panel))
            {
                list = m_EventProcessedEvents[panel];
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
            long modifCount = -1;
            if (panel != null && m_ModificationCount.ContainsKey(panel))
            {
                modifCount = m_ModificationCount[panel];
            }
            return modifCount;
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

                return;
            }

            m_EventCalledObjects.Remove(panel);
            m_EventDefaultActionObjects.Remove(panel);
            m_EventPathObjects.Remove(panel);
            m_EventProcessedEvents.Remove(panel);
            m_StackOfProcessedEvent.Remove(panel);
        }

        public void ReplayEvents(List<EventDebuggerEventRecord> eventBases)
        {
            if (eventBases == null)
                return;

            foreach (var eventBase in eventBases)
            {
                Event newEvent = new Event
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
                    continue;
                }
                else
                {
                    Debug.Log("Skipped event (" + eventBase.eventBaseName + "): " + eventBase);
                    continue;
                }

                Debug.Log("Replayed event (" + eventBase.eventBaseName + "): " + newEvent);
            }
        }

        public Dictionary<string, long> ComputeHistogram(List<EventDebuggerEventRecord> eventBases)
        {
            if (panel == null || !m_EventProcessedEvents.ContainsKey(panel))
                return null;

            var list = m_EventProcessedEvents[panel];
            if (list == null)
                return null;

            Dictionary<string, long> histogram = new Dictionary<string, long>();
            foreach (var callObject in list)
            {
                if (eventBases == null || eventBases.Count == 0 || eventBases.Contains(callObject.eventBase))
                {
                    var key = callObject.eventBase.eventBaseName;
                    long totalDuration = callObject.duration;
                    if (histogram.ContainsKey(key))
                    {
                        var currentDuration = histogram[key];
                        totalDuration = totalDuration + currentDuration;
                    }

                    histogram[key] = totalDuration;
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

        readonly Dictionary<IPanel, long> m_ModificationCount;
        readonly bool m_Log;

        // Methods
        public EventDebugger()
        {
            m_EventCalledObjects = new Dictionary<IPanel, List<EventDebuggerCallTrace>>();
            m_EventDefaultActionObjects = new Dictionary<IPanel, List<EventDebuggerDefaultActionTrace>>();
            m_EventPathObjects = new Dictionary<IPanel, List<EventDebuggerPathTrace>>();
            m_StackOfProcessedEvent = new Dictionary<IPanel, Stack<EventDebuggerTrace>>();
            m_EventProcessedEvents = new Dictionary<IPanel, List<EventDebuggerTrace>>();
            m_ModificationCount = new Dictionary<IPanel, long>();
            m_Log = true;
        }

        void AddCallObject(int cbHashCode, string cbName, EventBase evt, bool propagationHasStopped, bool immediatePropagationHasStopped, bool defaultHasBeenPrevented, long duration, IEventHandler mouseCapture)
        {
            if (m_Log)
            {
                var callObject = new EventDebuggerCallTrace(panel, evt, cbHashCode, cbName, propagationHasStopped, immediatePropagationHasStopped, defaultHasBeenPrevented, duration, mouseCapture);

                List<EventDebuggerCallTrace> list;
                if (m_EventCalledObjects.ContainsKey(panel))
                {
                    list = m_EventCalledObjects[panel];
                }
                else
                {
                    list = new List<EventDebuggerCallTrace>();
                    m_EventCalledObjects.Add(panel, list);
                }
                list.Add(callObject);
            }
        }

        void AddExecuteDefaultAction(EventBase evt, PropagationPhase phase, long duration, IEventHandler mouseCapture)
        {
            if (m_Log)
            {
                var defaultActionObject = new EventDebuggerDefaultActionTrace(panel, evt, phase, duration, mouseCapture);
                List<EventDebuggerDefaultActionTrace> list;

                if (m_EventDefaultActionObjects.ContainsKey(panel))
                {
                    list = m_EventDefaultActionObjects[panel];
                }
                else
                {
                    list = new List<EventDebuggerDefaultActionTrace>();
                    m_EventDefaultActionObjects.Add(panel, list);
                }
                list.Add(defaultActionObject);
            }
        }

        void AddPropagationPaths(EventBase evt, PropagationPaths paths)
        {
            if (m_Log)
            {
                var pathObject = new EventDebuggerPathTrace(panel, evt, paths);

                List<EventDebuggerPathTrace> list;
                if (m_EventPathObjects.ContainsKey(panel))
                {
                    list = m_EventPathObjects[panel];
                }
                else
                {
                    list = new List<EventDebuggerPathTrace>();
                    m_EventPathObjects.Add(panel, list);
                }
                list.Add(pathObject);
            }
        }

        void AddIMGUICall(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            if (m_Log)
            {
                var callObject = new EventDebuggerCallTrace(panel, evt, 0, "OnGUI", false, false, false, duration, mouseCapture);
                List<EventDebuggerCallTrace> list;

                if (m_EventCalledObjects.ContainsKey(panel))
                {
                    list = m_EventCalledObjects[panel];
                }
                else
                {
                    list = new List<EventDebuggerCallTrace>();
                    m_EventCalledObjects.Add(panel, list);
                }
                list.Add(callObject);
            }
        }

        void AddBeginProcessEvent(EventBase evt, IEventHandler mouseCapture)
        {
            var dbgObject = new EventDebuggerTrace(panel, evt, -1, mouseCapture);
            Stack<EventDebuggerTrace> stack;
            if (m_StackOfProcessedEvent.ContainsKey(panel))
            {
                stack = m_StackOfProcessedEvent[panel];
            }
            else
            {
                stack = new Stack<EventDebuggerTrace>();
                m_StackOfProcessedEvent.Add(panel, stack);
            }

            List<EventDebuggerTrace> list;
            if (m_EventProcessedEvents.ContainsKey(panel))
            {
                list = m_EventProcessedEvents[panel];
            }
            else
            {
                list = new List<EventDebuggerTrace>();
                m_EventProcessedEvents.Add(panel, list);
            }
            list.Add(dbgObject);
            stack.Push(dbgObject);
        }

        void AddEndProcessEvent(EventBase evt, long duration, IEventHandler mouseCapture)
        {
            bool evtHandled = false;
            if (m_StackOfProcessedEvent.ContainsKey(panel))
            {
                var stack = m_StackOfProcessedEvent[panel];
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
                List<EventDebuggerTrace> list;
                if (m_EventProcessedEvents.ContainsKey(panel))
                {
                    list = m_EventProcessedEvents[panel];
                }
                else
                {
                    list = new List<EventDebuggerTrace>();
                    m_EventProcessedEvents.Add(panel, list);
                }

                list.Add(dbgObject);
            }
        }

        public static string GetObjectDisplayName(object obj, bool withHashCode = true)
        {
            if (obj == null) return String.Empty;

            string objectName = obj.GetType().Name;
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
    }
}
