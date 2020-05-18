using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class UIElementsEventsDebugger : EditorWindow
    {
        [SerializeField]
        UIElementsEventsDebuggerImpl m_DebuggerImpl;

        [MenuItem("Window/UI Toolkit/Events Debugger", false, 3010, true)]
        public static void ShowUIElementsEventDebugger()
        {
            var window = EditorWindow.GetWindow<UIElementsEventsDebugger>();
            window.minSize = new Vector2(640, 480);
            window.titleContent = EditorGUIUtility.TrTextContent("UI Toolkit Event Debugger");
            window.m_DebuggerImpl.ClearLogs();
        }

        void OnEnable()
        {
            if (m_DebuggerImpl == null)
                m_DebuggerImpl = new UIElementsEventsDebuggerImpl();

            m_DebuggerImpl.Initialize(this, rootVisualElement);
        }

        void OnDisable()
        {
            m_DebuggerImpl.OnDisable();
        }
    }

    [Serializable]
    class UIElementsEventsDebuggerImpl : PanelDebugger
    {
        Label m_EventPropagationPaths;
        Label m_EventbaseInfo;
        ListView m_EventsLog;
        ScrollView m_EventRegistrationsScrollView;
        ScrollView m_EventCallbacksScrollView;
        EventLog m_Log;

        ScrollView m_EventsHistogramScrollView;
        ScrollView m_EventTimelineScrollView;

        long m_ModificationCount;
        bool m_AutoScroll;

        EventTypeSelectField m_EventTypeFilter;
        Label m_TimelineLegend;
        Label m_LogCountLabel;
        Label m_SelectionCountLabel;
        Label m_HistogramTitle;
        List<EventLogLine> m_SelectedEvents;

        Button m_ReplaySelectedEventsButton;

        Dictionary<ulong, long> m_EventTimestampDictionary = new Dictionary<ulong, long>();

        const string k_EventsContainerName = "eventsHistogramContainer";
        const string k_EventsLabelName = "eventsHistogramEntry";
        const string k_EventsDurationName = "eventsHistogramDuration";
        const string k_EventsDurationLabelName = "eventsHistogramDurationLabel";
        const string k_EventsDurationLengthName = "eventsHistogramDurationLength";

        VisualElement m_LegendContainer;

        VisualElement rootVisualElement;

        private readonly EventDebugger m_Debugger = new EventDebugger();

        void DisplayHistogram(ScrollView scrollView)
        {
            // Clear the scrollview
            scrollView.Clear();

            if (panel == null)
            {
                m_HistogramTitle.text = "Histogram - No Panel Selected";
            }
            else
            {
                Dictionary<string, long> histogramValue;
                histogramValue = m_Debugger.ComputeHistogram(m_SelectedEvents?.Select(x => x.eventBase).ToList() ?? m_Log.lines.Select(x => x.eventBase).ToList());
                if (histogramValue == null)
                    return;

                m_HistogramTitle.text = "Histogram - Element Count : " + histogramValue.Count;

                var childrenList = scrollView.Children().ToList();
                foreach (var child in childrenList)
                    child.RemoveFromHierarchy();

                long maxDuration = 0;
                foreach (var key in histogramValue.Keys)
                {
                    if (maxDuration < histogramValue[key])
                        maxDuration = histogramValue[key];
                }

                foreach (var key in histogramValue.Keys)
                    AddHistogramEntry(scrollView, key, histogramValue[key], histogramValue[key] / (float)maxDuration);
            }
        }

        static void AddHistogramEntry(VisualElement root, string name, long duration, float percent)
        {
            var container = new VisualElement() { name = k_EventsContainerName };
            var labelName = new Label(name) { name = k_EventsLabelName };
            var durationGraph = new VisualElement() { name = k_EventsDurationName };
            float durationLength = duration / 1000.0f;
            var labelNameDuration = new Label(name) { name = k_EventsDurationLabelName, text = durationLength + "ms" };
            var durationGraphLength = new VisualElement() { name = k_EventsDurationLengthName };
            durationGraphLength.style.position = Position.Absolute;
            durationGraph.Add(durationGraphLength);
            durationGraph.Add(labelNameDuration);

            container.style.flexDirection = FlexDirection.Row;
            container.Add(labelName);
            container.Add(durationGraph);
            root.Add(container);

            durationGraphLength.style.top = 1.0f;
            durationGraphLength.style.left = 0.0f;
            durationGraphLength.style.height = 18.0f;
            durationGraphLength.style.width = 300.0f * percent;
        }

        void DisplayTimeline(ScrollView scrollView)
        {
            // Clear the scrollview
            scrollView.Clear();

            if (panel == null)
            {
                DisplayEmptyTimelinePanel(scrollView);
            }
            else
            {
                var calls = m_Debugger.GetBeginEndProcessedEvents(panel);
                if ((calls == null) || (calls.Count == 0))
                {
                    DisplayEmptyTimelinePanel(scrollView);
                    return;
                }

                long maxDuration = 0;
                foreach (var entry in calls)
                {
                    if (maxDuration < entry.duration)
                    {
                        maxDuration = entry.duration;
                    }
                }

                float currentTop = 5.0f;
                float lastPixel = 0;
                float maxHeight = 75.0f;
                foreach (var dbgObject in calls)
                {
                    var entry = new VisualElement();
                    entry.style.position = Position.Absolute;
                    entry.style.backgroundColor = m_EventTypeFilter.m_Color.ContainsKey(dbgObject.eventBase.eventBaseName)
                        ? m_EventTypeFilter.m_Color[dbgObject.eventBase.eventBaseName]
                        : Color.black;
                    double percent = dbgObject.duration / (double)maxDuration;
                    float topPosition = currentTop + (float)((1 - percent) * 100);
                    entry.style.top = (topPosition > (maxHeight - 5)) ? (maxHeight - 5) : topPosition;
                    entry.style.height = (maxHeight - topPosition) < 5 ? 5 : (maxHeight - topPosition);
                    entry.style.width = 5;
                    entry.style.left = lastPixel;
                    entry.tooltip = dbgObject.eventBase.eventBaseName + " (" + dbgObject.duration / 1000 + " ms)";
                    scrollView.Add(entry);
                    lastPixel += 7;
                }

                scrollView.contentContainer.style.width = (lastPixel < 100) ? 100 : lastPixel;

                if (m_AutoScroll)
                    scrollView.scrollOffset = new Vector2(lastPixel, 0);
            }
        }

        void DisplayEmptyTimelinePanel(ScrollView scrollView)
        {
            Label line = new Label("Timeline - No Panel Selected");
            scrollView.Add(line);
        }

        void DisplayEvents(ScrollView scrollView)
        {
            scrollView.Clear();

            if (panel == null)
                return;

            foreach (var eventRegistrationListener in GlobalCallbackRegistry.s_Listeners)
            {
                VisualElement key = eventRegistrationListener.Key as VisualElement; // VE that sends events
                if (key?.panel == null)
                    continue;

                var vePanel = key.panel;
                if (vePanel != panel)
                    continue;

                string text = EventDebugger.GetObjectDisplayName(key);

                {
                    Label line = new Label(text);
                    line.AddToClassList("callback-list-element");
                    line.AddToClassList("visual-element");
                    scrollView.Add(line);
                }

                var events = eventRegistrationListener.Value;
                foreach (var evt in events)
                {
                    var evtType = evt.Key;
                    text = evtType.Name + " callbacks:";

                    {
                        Label line = new Label(text);
                        line.AddToClassList("callback-list-element");
                        line.AddToClassList("event-type");
                        scrollView.Add(line);
                    }

                    var evtCallbacks = evt.Value;
                    foreach (var evtCallback in evtCallbacks)
                    {
                        {
                            CodeLine line = new CodeLine(evtCallback.name, evtCallback.fileName, evtCallback.lineNumber, evtCallback.hashCode);
                            line.AddToClassList("callback-list-element");
                            line.AddToClassList("callback");
                            scrollView.Add(line);
                        }
                    }
                }
            }
        }

        public void Initialize(EditorWindow debuggerWindow, VisualElement root)
        {
            rootVisualElement = root;

            VisualTreeAsset template = EditorGUIUtility.Load("UIPackageResources/UXML/UIElementsDebugger/UIElementsEventsDebugger.uxml") as VisualTreeAsset;
            template.CloneTree(rootVisualElement);

            var toolbar = rootVisualElement.MandatoryQ("toolbar");
            m_Toolbar = toolbar;

            base.Initialize(debuggerWindow);

            rootVisualElement.AddStyleSheetPath("UIPackageResources/StyleSheets/UIElementsDebugger/UIElementsEventsDebugger.uss");

            var eventsDebugger = rootVisualElement.MandatoryQ("eventsDebugger");
            eventsDebugger.StretchToParentSize();

            m_EventCallbacksScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventCallbacksScrollView");

            m_EventTypeFilter = toolbar.MandatoryQ<EventTypeSelectField>("filter-event-type");
            m_EventTypeFilter.RegisterCallback<ChangeEvent<ulong>>(OnFilterChange);
            var refreshButton = toolbar.MandatoryQ<Button>("refresh");
            refreshButton.clickable.clicked += Refresh;
            var clearLogsButton = toolbar.MandatoryQ<Button>("clear-logs");
            clearLogsButton.clickable.clicked += () => { ClearLogs(); };
            m_ReplaySelectedEventsButton = toolbar.MandatoryQ<Button>("replay-selected-events");
            m_ReplaySelectedEventsButton.clickable.clicked += ReplaySelectedEvents;
            UpdateReplaySelectedEventsButton();

            var infoContainer = rootVisualElement.MandatoryQ("eventInfoContainer");
            m_LogCountLabel = infoContainer.MandatoryQ<Label>("log-count");
            m_SelectionCountLabel = infoContainer.MandatoryQ<Label>("selection-count");
            var autoScrollToggle = infoContainer.MandatoryQ<Toggle>("autoscroll");
            autoScrollToggle.value = m_AutoScroll;
            autoScrollToggle.RegisterValueChangedCallback((e) => { m_AutoScroll = e.newValue; });

            m_EventPropagationPaths = (Label)rootVisualElement.MandatoryQ("eventPropagationPaths");
            m_EventbaseInfo = (Label)rootVisualElement.MandatoryQ("eventbaseInfo");

            m_EventsLog = (ListView)rootVisualElement.MandatoryQ("eventsLog");
            m_EventsLog.focusable = true;
            m_EventsLog.selectionType = SelectionType.Multiple;
            m_EventsLog.onSelectionChange += OnEventsLogSelectionChanged;

            m_HistogramTitle = (Label)rootVisualElement.MandatoryQ("eventsHistogramTitle");

            m_Log = new EventLog();

            m_ModificationCount = 0;
            m_AutoScroll = true;

            var eventCallbacksScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventCallbacksScrollView");
            eventCallbacksScrollView.StretchToParentSize();

            var eventPropagationPathsScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventPropagationPathsScrollView");
            eventPropagationPathsScrollView.StretchToParentSize();

            var eventbaseInfoScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventbaseInfoScrollView");
            eventbaseInfoScrollView.StretchToParentSize();

            m_EventRegistrationsScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventsRegistrationsScrollView");
            DisplayEvents(m_EventRegistrationsScrollView);
            m_EventRegistrationsScrollView.StretchToParentSize();


            m_EventsHistogramScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventsHistogramScrollView");
            DisplayHistogram(m_EventsHistogramScrollView);
            m_EventsHistogramScrollView.StretchToParentSize();


            m_EventTimelineScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventTimelineScrollView");
            DisplayTimeline(m_EventTimelineScrollView);
            m_EventTimelineScrollView.SetScrollViewMode(ScrollViewMode.Horizontal);
            m_EventTimelineScrollView.StretchToParentSize();

            m_TimelineLegend = (Label)rootVisualElement.MandatoryQ("eventTimelineTitleLegend");
            m_TimelineLegend.RegisterCallback<MouseEnterEvent>(ShowLegend);
            m_TimelineLegend.RegisterCallback<MouseLeaveEvent>(HideLegend);
            CreateLegendContainer();

            BuildEventsLog();

            GlobalCallbackRegistry.IsEventDebuggerConnected = true;
        }

        public new void OnDisable()
        {
            base.OnDisable();
            GlobalCallbackRegistry.IsEventDebuggerConnected = false;
        }

        public override bool InterceptEvent(EventBase evt)
        {
            evt.eventLogger = m_Debugger;
            m_EventTimestampDictionary[evt.eventId] = (long)(Time.realtimeSinceStartup * 1000.0f);
            IEventHandler capture = panel?.GetCapturingElement(PointerId.mousePointerId);
            m_Debugger.BeginProcessEvent(evt, capture);
            return false;
        }

        public override void PostProcessEvent(EventBase evt)
        {
            if (evt.log)
            {
                var now = (long)(Time.realtimeSinceStartup * 1000.0f);
                var start = m_EventTimestampDictionary[evt.eventId];
                m_EventTimestampDictionary.Remove(evt.eventId);
                IEventHandler capture = panel?.GetCapturingElement(PointerId.mousePointerId);
                m_Debugger.EndProcessEvent(evt, now - start, capture);
                evt.eventLogger = null;
            }
        }

        void CreateLegendContainer()
        {
            m_LegendContainer = new VisualElement() { name = "eventTimelineLegendContainer" };
            rootVisualElement.Add(m_LegendContainer);
            m_LegendContainer.visible = false;

            var title = new Label("Legend") { name = "eventTimelineLegendTitle" };
            m_LegendContainer.Add(title);
            var listOfEventForColor = m_EventTypeFilter.m_Color.Keys;
            foreach (var eventName in listOfEventForColor)
            {
                var container = new VisualElement() { name = "eventTimelineLegendEntry" };

                var colorCode = new VisualElement() { name = "eventTimelineLegendEntryColor" };
                colorCode.style.backgroundColor = m_EventTypeFilter.m_Color[eventName];

                var newEntry = new Label(eventName) { name = "eventTimelineLegendEntryName" };

                container.Add(colorCode);
                container.Add(newEntry);
                m_LegendContainer.Add(container);
            }
        }

        void ShowLegend(MouseEnterEvent evt)
        {
            if (m_LegendContainer == null)
            {
                CreateLegendContainer();
            }

            m_LegendContainer.style.left = evt.mousePosition.x + 5;
            m_LegendContainer.style.top = evt.mousePosition.y - m_LegendContainer.layout.height - 5;
            m_LegendContainer.visible = true;
        }

        void HideLegend(MouseLeaveEvent evt)
        {
            m_LegendContainer.visible = false;
        }

        void ReplaySelectedEvents()
        {
            if (m_SelectedEvents == null)
                return;
            m_Debugger.ReplayEvents(m_SelectedEvents.Select(x => x.eventBase).ToList());
        }

        void HighlightCodeline(int hashcode)
        {
            foreach (var codeLine in m_EventRegistrationsScrollView.Children().OfType<CodeLine>())
            {
                if (codeLine.hashCode == hashcode)
                {
                    codeLine.AddToClassList("highlighted");
                    m_EventRegistrationsScrollView.ScrollTo(codeLine);
                }
                else
                {
                    codeLine.RemoveFromClassList("highlighted");
                }
            }
        }

        void UpdateEventsLog()
        {
            if (m_Log == null)
                return;

            m_Log.Clear();

            List<long> activeEventTypes = new List<long>();

            foreach (var s in m_EventTypeFilter.m_State)
            {
                if (s.Value)
                {
                    activeEventTypes.Add(s.Key);
                }
            }

            bool allActive = activeEventTypes.Count == m_EventTypeFilter.m_State.Count;
            bool allInactive = activeEventTypes.Count == 0;

            if (panel == null)
            {
                m_EventsLog.itemsSource = ToList();
                m_EventsLog.Refresh();
                return;
            }

            var calls = m_Debugger.GetBeginEndProcessedEvents(panel);
            if (calls == null)
            {
                m_EventsLog.itemsSource = ToList();
                m_EventsLog.Refresh();
                return;
            }

            if (!allInactive)
            {
                for (var lineIndex = 0; lineIndex < calls.Count; lineIndex++)
                {
                    var eventBase = calls[lineIndex].eventBase;
                    if (allActive || activeEventTypes.Contains(eventBase.eventTypeId))
                    {
                        var eventDateTimeStr = eventBase.TimestampString() + " #" + eventBase.eventId;
                        string handler = eventBase.eventBaseName;
                        string targetName = (eventBase.target != null
                            ? EventDebugger.GetObjectDisplayName(eventBase.target)
                            : "<null>");
                        var line = new EventLogLine(lineIndex + 1, "[" + eventDateTimeStr + "]", handler, targetName, eventBase);
                        m_Log.AddLine(line);
                    }
                }
            }

            UpdateLogCount();
            BuildEventsLog();
        }

        void OnEventsLogSelectionChanged(object obj)
        {
            if (m_SelectedEvents == null)
                m_SelectedEvents = new List<EventLogLine>();
            m_SelectedEvents.Clear();

            var list = obj as List<object>;
            if (list != null)
            {
                foreach (EventLogLine listItem in list)
                {
                    if (listItem != null)
                        m_SelectedEvents.Add(listItem);
                }
            }

            EventDebuggerEventRecord eventBase = null;
            IEventHandler focused = null;
            IEventHandler capture = null;
            if (m_SelectedEvents.Any())
            {
                var line = m_SelectedEvents[0];
                var calls = m_Debugger.GetBeginEndProcessedEvents(panel);
                eventBase = line != null ? calls ? [line.lineNumber - 1].eventBase : null;
                focused = line != null ? calls ? [line.lineNumber - 1].focusedElement : null;
                capture = line != null ? calls ? [line.lineNumber - 1].mouseCapture : null;
            }

            UpdateSelectionCount();
            UpdateReplaySelectedEventsButton();

            if (m_SelectedEvents.Count == 1)
            {
                UpdateEventCallbacks(eventBase);
                UpdateEventPropagationPaths(eventBase);
                UpdateEventbaseInfo(eventBase, focused, capture);
            }
            else
            {
                ClearEventCallbacks();
                ClearEventPropagationPaths();
                ClearEventbaseInfo();
            }

            DisplayHistogram(m_EventsHistogramScrollView);

            // Not working :         DisplayTimeline(m_EventTimelineScrollView, m_SelectedVisualTree.panel);
        }

        void ClearEventbaseInfo()
        {
            m_EventbaseInfo.text = "";
        }

        void UpdateEventbaseInfo(EventDebuggerEventRecord eventBase, IEventHandler focused, IEventHandler capture)
        {
            ClearEventbaseInfo();

            if (eventBase == null)
                return;

            m_EventbaseInfo.text += "Focused element: " + EventDebugger.GetObjectDisplayName(focused) + "\n";
            m_EventbaseInfo.text += "Capture element: " + EventDebugger.GetObjectDisplayName(capture) + "\n";

            if (eventBase.eventTypeId == MouseMoveEvent.TypeId() ||
                eventBase.eventTypeId == MouseOverEvent.TypeId() ||
                eventBase.eventTypeId == MouseOutEvent.TypeId() ||
                eventBase.eventTypeId == MouseDownEvent.TypeId() ||
                eventBase.eventTypeId == MouseUpEvent.TypeId() ||
                eventBase.eventTypeId == MouseEnterEvent.TypeId() ||
                eventBase.eventTypeId == MouseLeaveEvent.TypeId() ||
                eventBase.eventTypeId == DragEnterEvent.TypeId() ||
                eventBase.eventTypeId == DragLeaveEvent.TypeId() ||
                eventBase.eventTypeId == DragUpdatedEvent.TypeId() ||
                eventBase.eventTypeId == DragPerformEvent.TypeId() ||
                eventBase.eventTypeId == DragExitedEvent.TypeId() ||
                eventBase.eventTypeId == ContextClickEvent.TypeId() ||
                eventBase.eventTypeId == PointerMoveEvent.TypeId() ||
                eventBase.eventTypeId == PointerOverEvent.TypeId() ||
                eventBase.eventTypeId == PointerOutEvent.TypeId() ||
                eventBase.eventTypeId == PointerDownEvent.TypeId() ||
                eventBase.eventTypeId == PointerUpEvent.TypeId() ||
                eventBase.eventTypeId == PointerCancelEvent.TypeId() ||
                eventBase.eventTypeId == PointerStationaryEvent.TypeId() ||
                eventBase.eventTypeId == PointerEnterEvent.TypeId() ||
                eventBase.eventTypeId == PointerLeaveEvent.TypeId())
            {
                m_EventbaseInfo.text += "Mouse position: " + eventBase.mousePosition + "\n";
                m_EventbaseInfo.text += "Modifiers: " + eventBase.modifiers + "\n";
            }

            if (eventBase.eventTypeId == KeyDownEvent.TypeId() ||
                eventBase.eventTypeId == KeyUpEvent.TypeId())
            {
                m_EventbaseInfo.text += "Modifiers: " + eventBase.modifiers + "\n";
            }

            if (eventBase.eventTypeId == MouseDownEvent.TypeId() ||
                eventBase.eventTypeId == MouseUpEvent.TypeId() ||
                eventBase.eventTypeId == PointerDownEvent.TypeId() ||
                eventBase.eventTypeId == PointerUpEvent.TypeId() ||
                eventBase.eventTypeId == DragUpdatedEvent.TypeId() ||
                eventBase.eventTypeId == DragPerformEvent.TypeId() ||
                eventBase.eventTypeId == DragExitedEvent.TypeId())
            {
                m_EventbaseInfo.text += "Button: " + (eventBase.button == 0 ? "Left" : eventBase.button == 1 ? "Middle" : "Right") + "\n";
                m_EventbaseInfo.text += "Click count: " + eventBase.clickCount + "\n";
            }

            if (eventBase.eventTypeId == MouseMoveEvent.TypeId() ||
                eventBase.eventTypeId == MouseOverEvent.TypeId() ||
                eventBase.eventTypeId == MouseOutEvent.TypeId() ||
                eventBase.eventTypeId == MouseDownEvent.TypeId() ||
                eventBase.eventTypeId == MouseUpEvent.TypeId() ||
                eventBase.eventTypeId == MouseEnterEvent.TypeId() ||
                eventBase.eventTypeId == MouseLeaveEvent.TypeId() ||
                eventBase.eventTypeId == DragEnterEvent.TypeId() ||
                eventBase.eventTypeId == DragLeaveEvent.TypeId() ||
                eventBase.eventTypeId == DragUpdatedEvent.TypeId() ||
                eventBase.eventTypeId == DragPerformEvent.TypeId() ||
                eventBase.eventTypeId == DragExitedEvent.TypeId() ||
                eventBase.eventTypeId == ContextClickEvent.TypeId() ||
                eventBase.eventTypeId == WheelEvent.TypeId() ||
                eventBase.eventTypeId == PointerMoveEvent.TypeId() ||
                eventBase.eventTypeId == PointerOverEvent.TypeId() ||
                eventBase.eventTypeId == PointerOutEvent.TypeId() ||
                eventBase.eventTypeId == PointerDownEvent.TypeId() ||
                eventBase.eventTypeId == PointerUpEvent.TypeId() ||
                eventBase.eventTypeId == PointerCancelEvent.TypeId() ||
                eventBase.eventTypeId == PointerStationaryEvent.TypeId() ||
                eventBase.eventTypeId == PointerEnterEvent.TypeId() ||
                eventBase.eventTypeId == PointerLeaveEvent.TypeId())
            {
                m_EventbaseInfo.text += "Pressed buttons: " + eventBase.pressedButtons + "\n";
            }

            if (eventBase.eventTypeId == WheelEvent.TypeId())
            {
                m_EventbaseInfo.text += "Mouse delta: " + eventBase.delta + "\n";
            }

            if (eventBase.eventTypeId == KeyDownEvent.TypeId() ||
                eventBase.eventTypeId == KeyUpEvent.TypeId())
            {
                if (char.IsControl(eventBase.character))
                {
                    m_EventbaseInfo.text += "Character: \\" + (byte)(eventBase.character) + "\n";
                }
                else
                {
                    m_EventbaseInfo.text += "Character: " + eventBase.character + "\n";
                }

                m_EventbaseInfo.text += "Key code: " + eventBase.keyCode + "\n";
            }

            if (eventBase.eventTypeId == ValidateCommandEvent.TypeId() ||
                eventBase.eventTypeId == ExecuteCommandEvent.TypeId())
            {
                m_EventbaseInfo.text += "Command: " + eventBase.commandName + "\n";
            }
        }

        void OnFilterChange(ChangeEvent<ulong> e)
        {
            m_Debugger.UpdateModificationCount();
            Refresh();
            BuildEventsLog();
        }

        void ClearEventCallbacks()
        {
            foreach (var codeLine in m_EventRegistrationsScrollView.Children().OfType<CodeLine>())
            {
                codeLine.RemoveFromClassList("highlighted");
            }

            m_EventCallbacksScrollView.Clear();
        }

        void UpdateEventCallbacks(EventDebuggerEventRecord eventBase)
        {
            ClearEventCallbacks();

            if (eventBase == null)
                return;

            var callbacks = m_Debugger.GetCalls(panel, eventBase);
            if (callbacks != null)
            {
                foreach (EventDebuggerCallTrace callback in callbacks)
                {
                    VisualElement container = new VisualElement { name = "line-container" };

                    Label timeStamp = new Label { name = "timestamp" };
                    Label handler = new Label { name = "handler" };
                    Label phaseDurationContainer = new Label { name = "phaseDurationContainer" };
                    Label phase = new Label { name = "phase" };
                    Label duration = new Label { name = "duration" };

                    timeStamp.AddToClassList("log-line-item");
                    handler.AddToClassList("log-line-item");
                    phaseDurationContainer.AddToClassList("log-line-item");

                    timeStamp.text = "[" + eventBase.TimestampString() + "]";
                    handler.text = callback.callbackName;
                    if (callback.immediatePropagationHasStopped)
                        handler.text += " Immediately Stopped Propagation";
                    else if (callback.propagationHasStopped)
                        handler.text += " Stopped Propagation";
                    if (callback.defaultHasBeenPrevented)
                        handler.text += " (Default Prevented)";

                    phase.text = callback.eventBase.propagationPhase.ToString();
                    duration.text = "Duration: " + callback.duration / 1000f + "ms";

                    container.Add(timeStamp);
                    container.Add(handler);
                    phaseDurationContainer.Add(phase);
                    phaseDurationContainer.Add(duration);
                    container.Add(phaseDurationContainer);

                    m_EventCallbacksScrollView.Add(container);

                    var hash = callback.callbackHashCode;
                    HighlightCodeline(hash);
                }
            }

            var defaultActions = m_Debugger.GetDefaultActions(panel, eventBase);
            if (defaultActions == null)
                return;

            foreach (EventDebuggerDefaultActionTrace defaultAction in defaultActions)
            {
                VisualElement container = new VisualElement { name = "line-container" };

                Label timeStamp = new Label { name = "timestamp" };
                Label handler = new Label { name = "handler" };
                Label phaseDurationContainer = new Label { name = "phaseDurationContainer" };
                Label phase = new Label { name = "phase" };
                Label duration = new Label { name = "duration" };

                timeStamp.AddToClassList("log-line-item");
                handler.AddToClassList("log-line-item");
                phaseDurationContainer.AddToClassList("log-line-item");

                timeStamp.text = "[" + eventBase.TimestampString() + "]";
                handler.text = defaultAction.targetName + "." +
                    (defaultAction.phase == PropagationPhase.AtTarget
                        ? "ExecuteDefaultActionAtTarget"
                        : "ExecuteDefaultAction");

                duration.text = "Duration: " + defaultAction.duration / 1000f + "ms";

                container.Add(timeStamp);
                container.Add(handler);
                phaseDurationContainer.Add(phase);
                phaseDurationContainer.Add(duration);
                container.Add(phaseDurationContainer);

                m_EventCallbacksScrollView.Add(container);
            }
        }

        void ClearEventPropagationPaths()
        {
            m_EventPropagationPaths.text = "";
        }

        void UpdateEventPropagationPaths(EventDebuggerEventRecord eventBase)
        {
            ClearEventPropagationPaths();

            if (eventBase == null)
                return;

            var propagationPaths = m_Debugger.GetPropagationPaths(panel, eventBase);
            if (propagationPaths == null)
                return;

            foreach (EventDebuggerPathTrace propagationPath in propagationPaths)
            {
                if (propagationPath?.paths == null)
                    continue;

                m_EventPropagationPaths.text += "Trickle Down Path:\n";
                var pathsTrickleDownPath = propagationPath.paths?.trickleDownPath;
                if (pathsTrickleDownPath != null && pathsTrickleDownPath.Any())
                {
                    foreach (var trickleDownPathElement in pathsTrickleDownPath)
                    {
                        var trickleDownPathName = trickleDownPathElement.name;
                        if (string.IsNullOrEmpty(trickleDownPathName))
                            trickleDownPathName = trickleDownPathElement.GetType().Name;
                        m_EventPropagationPaths.text += "    " + trickleDownPathName + "\n";
                    }
                }
                else
                {
                    m_EventPropagationPaths.text += "    <empty>\n";
                }

                m_EventPropagationPaths.text += "Target list:\n";
                var targets = propagationPath.paths.targetElements;
                if (targets != null && targets.Any())
                {
                    foreach (var t in targets)
                    {
                        var targetName = t.name;
                        if (string.IsNullOrEmpty(targetName))
                            targetName = t.GetType().Name;
                        m_EventPropagationPaths.text += "    " + targetName + "\n";
                    }
                }
                else
                {
                    m_EventPropagationPaths.text += "    <empty>\n";
                }

                m_EventPropagationPaths.text += "Bubble Up Path:\n";
                var pathsBubblePath = propagationPath.paths.bubbleUpPath;
                if (pathsBubblePath != null && pathsBubblePath.Any())
                {
                    foreach (var bubblePathElement in pathsBubblePath)
                    {
                        var bubblePathName = bubblePathElement.name;
                        if (string.IsNullOrEmpty(bubblePathName))
                            bubblePathName = bubblePathElement.GetType().Name;
                        m_EventPropagationPaths.text += "    " + bubblePathName + "\n";
                    }
                }
                else
                {
                    m_EventPropagationPaths.text += "    <empty>\n";
                }
            }
        }

        void BuildEventsLog()
        {
            m_EventsLog.itemsSource = ToList();
            m_EventsLog.itemHeight = 15;
            m_EventsLog.bindItem = (target, index) =>
            {
                var line = m_Log.lines[index];

                // Add text
                VisualElement lineText = target.MandatoryQ<VisualElement>("log-line");
                Label theLabel;
                theLabel = lineText[0] as Label;
                theLabel.text = line.timestamp;
                theLabel = lineText[1] as Label;
                theLabel.text = line.eventName;
                theLabel = lineText[2] as Label;
                theLabel.text = line.target;
            };
            m_EventsLog.makeItem = () =>
            {
                VisualElement container = new VisualElement { name = "log-line" };
                Label timeStamp = new Label { name = "timestamp" };
                Label eventLabel = new Label { name = "event" };
                Label target = new Label { name = "target" };

                timeStamp.AddToClassList("log-line-item");
                eventLabel.AddToClassList("log-line-item");
                target.AddToClassList("log-line-item");

                container.Add(timeStamp);
                container.Add(eventLabel);
                container.Add(target);

                return container;
            };
            m_EventsLog.Refresh();

            if (m_AutoScroll)
                m_EventsLog.ScrollToItem(-1);
        }

        IList ToList()
        {
            return m_Log.lines.ToList();
        }

        public void ClearLogs()
        {
            m_Debugger.ClearLogs();
            m_SelectedEvents?.Clear();
            Refresh();
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag)
        {
        }

        protected override void OnSelectPanelDebug(IPanelDebug pdbg)
        {
            if (pdbg != null)
            {
                m_Debugger.panel = pdbg.panel;
            }

            DisplayEvents(m_EventRegistrationsScrollView);
            DisplayHistogram(m_EventsHistogramScrollView);
            DisplayTimeline(m_EventTimelineScrollView);

            var erTitle = rootVisualElement.MandatoryQ<Label>("eventsRegistrationTitle");
            const string prefix = "Registered Event Callbacks";
            erTitle.text = panel != null ? prefix + " in " + ((Panel)panel).name : prefix + " [No Panel Selected]";

            Refresh();
        }

        public override void Refresh()
        {
            var eventDebuggerModificationCount = m_Debugger.GetModificationCount(panel);
            if (eventDebuggerModificationCount == m_ModificationCount)
                return;
            m_ModificationCount = eventDebuggerModificationCount;

            UpdateEventsLog();
            UpdateLogCount();
            UpdateSelectionCount();
            UpdateReplaySelectedEventsButton();
            DisplayHistogram(m_EventsHistogramScrollView);
            DisplayTimeline(m_EventTimelineScrollView);
        }

        void UpdateLogCount()
        {
            m_LogCountLabel.text = m_Log.lines.Count + " event" + (m_Log.lines.Count > 1 ? "s" : "");
        }

        void UpdateSelectionCount()
        {
            m_SelectionCountLabel.text =
                "(" + (m_SelectedEvents != null ? m_SelectedEvents.Count.ToString() : "0") + " selected)";
        }

        void UpdateReplaySelectedEventsButton()
        {
            m_ReplaySelectedEventsButton.SetEnabled(m_SelectedEvents != null && m_SelectedEvents.Any());
        }
    }
}
