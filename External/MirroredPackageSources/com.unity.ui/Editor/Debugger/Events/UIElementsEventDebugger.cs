using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
            m_DebuggerImpl.ClearLogs();
            m_DebuggerImpl.OnDisable();
            m_DebuggerImpl = null;
        }
    }

    [Serializable]
    class UIElementsEventsDebuggerImpl : PanelDebugger
    {
        const string k_EventsContainerName = "eventsHistogramContainer";
        const string k_EventsLabelName = "eventsHistogramEntry";
        const string k_EventsDurationName = "eventsHistogramDuration";
        const string k_EventsDurationLabelName = "eventsHistogramDurationLabel";
        const string k_EventsDurationLengthName = "eventsHistogramDurationLength";
        const int k_DefaultMaxLogLines = 5000;

        Label m_EventPropagationPaths;
        Label m_EventBaseInfo;
        ListView m_EventsLog;
        ListView m_EventRegistrationsListView;
        ScrollView m_EventCallbacksScrollView;
        EventLog m_Log;
        int m_StartIndex;

        ScrollView m_EventsHistogramScrollView;

        long m_ModificationCount;
        [SerializeField]
        bool m_AutoScroll;
        [SerializeField]
        bool m_MaxLogLines;
        [SerializeField]
        int m_MaxLogLineCount;
        [SerializeField]
        bool m_DisplayHistogram;
        [SerializeField]
        bool m_DisplayRegisteredEventCallbacks;

        EventTypeSearchField m_EventTypeFilter;
        ToolbarSearchField m_CallbackTypeFilter;
        Label m_LogCountLabel;
        Label m_SelectionCountLabel;
        Toggle m_HistogramToggle;
        Toggle m_RegisteredEventCallbacksToggle;
        List<EventLogLine> m_SelectedEvents;
        IntegerField m_MaxLogLinesField;
        ToolbarMenu m_SettingsMenu;
        ToolbarToggle m_SuspendListeningToggle;
        List<IRegisteredCallbackLine> m_RegisteredEventCallbacksDataSource = new List<IRegisteredCallbackLine>();

        ToolbarToggle m_TogglePlayback;
        ToolbarButton m_DecreasePlaybackSpeedButton;
        TextField m_PlaybackSpeedTextField;
        ToolbarButton m_IncreasePlaybackSpeedButton;
        ToolbarButton m_SaveReplayButton;
        ToolbarButton m_LoadReplayButton;
        ToolbarButton m_StartPlaybackButton;
        ToolbarButton m_StopPlaybackButton;
        Label m_PlaybackLabel;

        Dictionary<ulong, long> m_EventTimestampDictionary = new Dictionary<ulong, long>();
        VisualElement rootVisualElement;

        HighlightOverlayPainter m_HighlightOverlay;
        RepaintOverlayPainter m_RepaintOverlay;

        readonly EventDebugger m_Debugger = new EventDebugger();

        void DisplayHistogram(ScrollView scrollView)
        {
            if (scrollView == null)
                return;

            // Clear the ScrollView
            scrollView.Clear();

            if (!m_DisplayHistogram)
                return;

            if (panel == null)
            {
                m_HistogramToggle.text = "Histogram - No Panel Selected";
            }
            else
            {
                m_HistogramToggle.text = "Histogram";

                var histogramValue = m_Debugger.ComputeHistogram(m_SelectedEvents?.Select(x => x.eventBase).ToList() ??
                    m_Log.lines.Select(x => x.eventBase).ToList());
                if (histogramValue == null)
                    return;

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

                var eventsHistogramTitleContainer = rootVisualElement.MandatoryQ("eventsHistogramTitleContainer");
                var eventsHistogramTotal = eventsHistogramTitleContainer.MandatoryQ<Label>("eventsHistogramTotal");
                eventsHistogramTotal.text = $"{histogramValue.Count} elements";
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

        void InitializeRegisteredCallbacksBinding()
        {
            m_EventRegistrationsListView.itemHeight = 18;
            m_EventRegistrationsListView.makeItem += () =>
            {
                var lineContainer = new VisualElement { pickingMode = PickingMode.Position };
                lineContainer.RegisterCallback<ClickEvent>(OnCallbackLineClick);
                lineContainer.RegisterCallback<MouseOverEvent>(OnCallbackMouseOver);

                // Title items
                var titleLine = new Label { pickingMode = PickingMode.Ignore };
                titleLine.AddToClassList("callback-list-element");
                titleLine.AddToClassList("visual-element");
                lineContainer.Add(titleLine);

                // Callback items
                var callbackLine = new Label { pickingMode = PickingMode.Ignore };
                callbackLine.AddToClassList("callback-list-element");
                callbackLine.AddToClassList("event-type");
                lineContainer.Add(callbackLine);

                // Code line items
                var codeLineContainer = new VisualElement();
                codeLineContainer.AddToClassList("code-line-container");
                var line = new CodeLine { pickingMode = PickingMode.Ignore };
                line.AddToClassList("callback-list-element");
                line.AddToClassList("callback");
                codeLineContainer.Add(line);
                var openSourceFileButton = new Button();
                openSourceFileButton.AddToClassList("open-source-file-button");
                openSourceFileButton.clickable.clicked += line.GotoCode;
                openSourceFileButton.tooltip = $"Click to go to event registration point in code:\n{line}";
                codeLineContainer.Add(openSourceFileButton);
                lineContainer.Add(codeLineContainer);

                return lineContainer;
            };
            m_EventRegistrationsListView.bindItem += (element, i) =>
            {
                var titleLine = element[0] as Label;
                var callbackLine = element[1] as Label;
                var codeLineContainer = element[2];
                var codeLine = codeLineContainer?.Q<CodeLine>();

                if (titleLine == null || callbackLine == null || codeLine == null)
                    return;

                var data = m_RegisteredEventCallbacksDataSource[i];
                titleLine.style.display = data.type == LineType.Title ? DisplayStyle.Flex : DisplayStyle.None;
                callbackLine.style.display = data.type == LineType.Callback ? DisplayStyle.Flex : DisplayStyle.None;
                codeLineContainer.style.display = data.type == LineType.CodeLine ? DisplayStyle.Flex : DisplayStyle.None;

                element.userData = data.callbackHandler;

                titleLine.text = data.text;
                callbackLine.text = data.text;

                if (data.type == LineType.CodeLine)
                {
                    if (!(data is CodeLineInfo codeLineData))
                        return;

                    codeLine.Init(codeLineData.text, codeLineData.fileName, codeLineData.lineNumber, codeLineData.lineHashCode);
                    codeLine.RemoveFromClassList("highlighted");

                    if (codeLineData.highlighted)
                    {
                        codeLine.AddToClassList("highlighted");
                    }
                }
            };
        }

        long GetTypeId(Type type)
        {
            var getTypeId = type.GetMethod("TypeId", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (getTypeId == null)
                return -1;

            return (long)getTypeId.Invoke(null, null);
        }

        void DisplayRegisteredEventCallbacks()
        {
            var listView = m_EventRegistrationsListView;
            var filter = m_CallbackTypeFilter.value;
            if (panel == null || listView == null)
                return;

            m_RegisteredEventCallbacksDataSource.Clear();

            if (!m_DisplayRegisteredEventCallbacks || !GlobalCallbackRegistry.IsEventDebuggerConnected)
            {
                listView.Refresh();
                return;
            }

            bool IsFilteredOut(Type type)
            {
                var id = GetTypeId(type);
                return m_EventTypeFilter.State.TryGetValue(id, out var isEnabled) && !isEnabled;
            }

            GlobalCallbackRegistry.CleanListeners(panel);

            var listeners = GlobalCallbackRegistry.s_Listeners.ToList();
            var nbListeners = 0;
            var nbCallbacks = 0;
            foreach (var eventRegistrationListener in listeners)
            {
                var key = eventRegistrationListener.Key as VisualElement; // VE that sends events
                if (key?.panel == null)
                    continue;

                var vePanel = key.panel;
                if (vePanel != panel)
                    continue;

                var text = EventDebugger.GetObjectDisplayName(key);

                if (!string.IsNullOrEmpty(filter) && !text.ToLower().Contains(filter.ToLower()))
                    continue;

                var events = eventRegistrationListener.Value;
                if (events.All(e => IsFilteredOut(e.Key)))
                    continue;

                m_RegisteredEventCallbacksDataSource.Add(new TitleInfo(text, key));

                foreach (var evt in events)
                {
                    var evtType = evt.Key;
                    text = EventDebugger.GetTypeDisplayName(evtType);

                    if (IsFilteredOut(evtType))
                        continue;

                    m_RegisteredEventCallbacksDataSource.Add(new CallbackInfo(text, key));

                    var evtCallbacks = evt.Value;
                    foreach (var evtCallback in evtCallbacks)
                    {
                        m_RegisteredEventCallbacksDataSource.Add(new CodeLineInfo(evtCallback.name, key, evtCallback.fileName, evtCallback.lineNumber, evtCallback.hashCode));
                        nbCallbacks++;
                    }
                }

                nbListeners++;
            }

            listView.itemsSource = m_RegisteredEventCallbacksDataSource;

            var nbEvents = m_EventTypeFilter.State.Count(s => s.Key > 0);
            var nbFilteredEvents = m_EventTypeFilter.State.Count(s => s.Key > 0 && s.Value);
            var eventsRegistrationTitleContainer = rootVisualElement.MandatoryQ("eventsRegistrationTitleContainer");
            var eventsRegistrationTotals = eventsRegistrationTitleContainer.MandatoryQ<Label>("eventsRegistrationTotals");
            eventsRegistrationTotals.text =
                $"{nbListeners} listener{(nbListeners > 1 ? "s" : "")}, {nbCallbacks} callback{(nbCallbacks > 1 ? "s" : "")}" +
                (nbFilteredEvents < nbEvents ? $" (filter: {nbFilteredEvents} event{(nbFilteredEvents > 1 ? "s" : "")})" : string.Empty);
        }

        void OnCallbackMouseOver(MouseOverEvent evt)
        {
            m_HighlightOverlay?.ClearOverlay();

            var element = evt.currentTarget as VisualElement;
            var highlightElement = element.userData as VisualElement;
            HighlightElement(highlightElement, true, false);
        }

        void OnCallbackLineClick(ClickEvent evt)
        {
            var element = evt.currentTarget as VisualElement;
            var highlightElement = element.userData as VisualElement;
            HighlightElement(highlightElement, true);
        }

        public void Initialize(EditorWindow debuggerWindow, VisualElement root)
        {
            rootVisualElement = root;

            VisualTreeAsset template = EditorGUIUtility.Load("UIPackageResources/UXML/UIElementsDebugger/UIElementsEventsDebugger.uxml") as VisualTreeAsset;
            if (template != null)
                template.CloneTree(rootVisualElement);

            var toolbar = rootVisualElement.MandatoryQ<Toolbar>("toolbar");
            m_Toolbar = toolbar;

            base.Initialize(debuggerWindow);

            rootVisualElement.AddStyleSheetPath("UIPackageResources/StyleSheets/UIElementsDebugger/UIElementsEventsDebugger.uss");

            var eventsDebugger = rootVisualElement.MandatoryQ("eventsDebugger");
            eventsDebugger.StretchToParentSize();

            m_EventCallbacksScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventCallbacksScrollView");

            m_EventTypeFilter = toolbar.MandatoryQ<EventTypeSearchField>("filter-event-type");
            m_EventTypeFilter.RegisterCallback<ChangeEvent<string>>(OnFilterChange);
            m_SuspendListeningToggle = rootVisualElement.MandatoryQ<ToolbarToggle>("suspend");
            m_SuspendListeningToggle.RegisterValueChangedCallback(SuspendListening);
            var refreshButton = rootVisualElement.MandatoryQ<ToolbarButton>("refresh");
            refreshButton.clickable.clicked += Refresh;
            var clearLogsButton = rootVisualElement.MandatoryQ<ToolbarButton>("clear-logs");
            clearLogsButton.clickable.clicked += ClearLogs;

            var eventReplayToolbar = rootVisualElement.MandatoryQ<Toolbar>("eventReplayToolbar");
            m_DecreasePlaybackSpeedButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("decrease-playback-speed");
            m_DecreasePlaybackSpeedButton.clickable.clicked += DecreasePlaybackSpeed;
            m_IncreasePlaybackSpeedButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("increase-playback-speed");
            m_IncreasePlaybackSpeedButton.clickable.clicked += IncreasePlaybackSpeed;
            m_PlaybackSpeedTextField = eventReplayToolbar.MandatoryQ<TextField>("playback-speed");
            m_PlaybackSpeedTextField.RegisterValueChangedCallback(UpdatePlaybackSpeed);
            m_SaveReplayButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("save-replay");
            m_SaveReplayButton.clickable.clicked += SaveReplaySessionFromSelection;
            m_LoadReplayButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("load-replay");
            m_LoadReplayButton.clickable.clicked += LoadReplaySession;
            m_TogglePlayback = eventReplayToolbar.MandatoryQ<ToolbarToggle>("pause-resume-playback");
            m_TogglePlayback.RegisterValueChangedCallback(TogglePlayback);
            m_PlaybackLabel = eventReplayToolbar.MandatoryQ<Label>("replay-selected-events");
            m_PlaybackLabel.text = "";
            m_StartPlaybackButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("start-playback");
            m_StartPlaybackButton.clickable.clicked += OnReplayStart;
            m_StopPlaybackButton = eventReplayToolbar.MandatoryQ<ToolbarButton>("stop-playback");
            m_StopPlaybackButton.clickable.clicked += OnReplayCompleted;
            UpdatePlaybackButtons();

            var infoContainer = rootVisualElement.MandatoryQ("eventInfoContainer");
            m_LogCountLabel = infoContainer.MandatoryQ<Label>("log-count");
            m_SelectionCountLabel = infoContainer.MandatoryQ<Label>("selection-count");

            m_MaxLogLinesField = infoContainer.MandatoryQ<IntegerField>("maxLogLinesField");
            m_MaxLogLinesField.RegisterValueChangedCallback(e =>
            {
                // Minimum 1 line if max log lines is enabled
                m_MaxLogLineCount = Math.Max(1, e.newValue);
                m_MaxLogLinesField.value = m_MaxLogLineCount;
                DoMaxLogLines();
            });

            m_SettingsMenu = infoContainer.Q<ToolbarMenu>("settings-menu");
            SetupSettingsMenu();

            m_EventPropagationPaths = (Label)rootVisualElement.MandatoryQ("eventPropagationPaths");
            m_EventBaseInfo = (Label)rootVisualElement.MandatoryQ("eventbaseInfo");

            m_EventsLog = (ListView)rootVisualElement.MandatoryQ("eventsLog");
            m_EventsLog.focusable = true;
            m_EventsLog.selectionType = SelectionType.Multiple;
            m_EventsLog.onSelectionChange += OnEventsLogSelectionChanged;
            m_EventsLog.RegisterCallback<FocusOutEvent>(OnListFocusedOut, TrickleDown.TrickleDown);

            m_HistogramToggle = rootVisualElement.MandatoryQ<Toggle>("eventsHistogramTitle");
            m_HistogramToggle.RegisterValueChangedCallback((e) =>
            {
                m_DisplayHistogram = e.newValue;
                DisplayHistogram(m_EventsHistogramScrollView);
            });

            m_Log = new EventLog();

            m_ModificationCount = 0;

            var eventCallbacksScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventCallbacksScrollView");
            eventCallbacksScrollView.StretchToParentSize();

            var eventPropagationPathsScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventPropagationPathsScrollView");
            eventPropagationPathsScrollView.StretchToParentSize();

            var eventBaseInfoScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventbaseInfoScrollView");
            eventBaseInfoScrollView.StretchToParentSize();

            m_RegisteredEventCallbacksToggle = rootVisualElement.MandatoryQ<Toggle>("eventsRegistrationTitle");
            m_RegisteredEventCallbacksToggle.RegisterValueChangedCallback((e) =>
            {
                m_DisplayRegisteredEventCallbacks = e.newValue;
                DisplayRegisteredEventCallbacks();
            });

            m_CallbackTypeFilter = rootVisualElement.MandatoryQ<ToolbarSearchField>("filter-registered-callback");
            m_CallbackTypeFilter.RegisterCallback<ChangeEvent<string>>(OnRegisteredCallbackFilterChange);
            m_CallbackTypeFilter.tooltip = "Type in element name, type or id to filter callbacks.";

            m_EventRegistrationsListView = rootVisualElement.MandatoryQ<ListView>("eventsRegistrationsListView");
            InitializeRegisteredCallbacksBinding();
            DisplayRegisteredEventCallbacks();
            m_EventRegistrationsListView.StretchToParentSize();

            m_EventsHistogramScrollView = (ScrollView)rootVisualElement.MandatoryQ("eventsHistogramScrollView");
            m_EventsHistogramScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_EventsHistogramScrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            DisplayHistogram(m_EventsHistogramScrollView);
            m_EventsHistogramScrollView.StretchToParentSize();

            m_AutoScroll = true;
            m_MaxLogLines = false;
            m_MaxLogLineCount = k_DefaultMaxLogLines;
            m_MaxLogLinesField.value = m_MaxLogLineCount;
            m_DisplayHistogram = true;
            m_HistogramToggle.value = m_DisplayHistogram;
            m_DisplayRegisteredEventCallbacks = true;
            m_RegisteredEventCallbacksToggle.value = m_DisplayRegisteredEventCallbacks;

            m_HighlightOverlay = new HighlightOverlayPainter();
            m_RepaintOverlay = new RepaintOverlayPainter();

            DoMaxLogLines();

            GlobalCallbackRegistry.IsEventDebuggerConnected = true;

            EditorApplication.update += EditorUpdate;
        }

        void SuspendListening(ChangeEvent<bool> evt)
        {
            m_Debugger.suspended = evt.newValue;
            m_SuspendListeningToggle.text = m_Debugger.suspended ? "Suspended" : "Suspend";
            Refresh();
        }

        void SetupSettingsMenu()
        {
            m_SettingsMenu.menu.AppendAction(
                "Autoscroll",
                a =>
                {
                    m_AutoScroll = !m_AutoScroll;
                    DoAutoScroll();
                },
                a => m_AutoScroll ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            m_SettingsMenu.menu.AppendAction(
                "Max Log Lines",
                a =>
                {
                    m_MaxLogLines = !m_MaxLogLines;
                    DoMaxLogLines();
                },
                a => m_MaxLogLines ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        void DoMaxLogLines()
        {
            m_MaxLogLinesField.SetEnabled(m_MaxLogLines);
            m_MaxLogLineCount = m_MaxLogLinesField.value;
            BuildEventsLog();
        }

        public new void OnDisable()
        {
            base.OnDisable();

            EditorApplication.update -= EditorUpdate;
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

        void HighlightElement(VisualElement ve, bool isFirst, bool showRepaint = true)
        {
            var visible = ve.resolvedStyle.visibility == Visibility.Visible && ve.resolvedStyle.opacity > Mathf.Epsilon;
            if (visible)
            {
                if (isFirst)
                {
                    m_HighlightOverlay?.AddOverlay(ve, OverlayContent.Content);
                    if (m_Debugger.panelDebug?.debugContainer != null && showRepaint)
                        m_RepaintOverlay?.AddOverlay(ve, m_Debugger.panelDebug.debugContainer);
                }
                else
                {
                    m_HighlightOverlay?.AddOverlay(ve, OverlayContent.Content, 0.1f);
                }
            }

            SelectPanelToDebug(panel);

            m_Debugger.panelDebug?.MarkDirtyRepaint();
            m_Debugger.panelDebug?.MarkDebugContainerDirtyRepaint();
        }

        float[] m_PlaybackSpeeds = { 0.1f, 0.2f, 0.5f, 1.0f, 2.0f, 5.0f, 10f };

        void DecreasePlaybackSpeed()
        {
            var i = m_PlaybackSpeeds.Length - 1;
            for (; i >= 0; i--)
            {
                if (m_PlaybackSpeeds[i] < m_Debugger.playbackSpeed)
                {
                    UpdatePlaybackSpeed(m_PlaybackSpeeds[i]);
                    break;
                }
            }
        }

        void IncreasePlaybackSpeed()
        {
            var i = 0;
            for (; i < m_PlaybackSpeeds.Length; i++)
            {
                if (m_PlaybackSpeeds[i] > m_Debugger.playbackSpeed)
                {
                    UpdatePlaybackSpeed(m_PlaybackSpeeds[i]);
                    break;
                }
            }
        }

        IEnumerator _replayEnumerator;

        void OnReplayStart()
        {
            if (m_SelectedEvents == null)
                return;

            ReplayEvents(m_SelectedEvents.Select(x => x.eventBase));
        }

        void ReplayEvents(IEnumerable<EventDebuggerEventRecord> events)
        {
            if (!m_Debugger.isReplaying)
            {
                _replayEnumerator = m_Debugger.ReplayEvents(events, RefreshFromReplay);

                if (_replayEnumerator == null || !_replayEnumerator.MoveNext())
                    return;

                UpdatePlaybackButtons();
            }
        }

        void EditorUpdate()
        {
            var overlayPanel = m_Debugger.panelDebug?.debuggerOverlayPanel as Panel;
            overlayPanel?.UpdateAnimations();

            if (_replayEnumerator != null && !_replayEnumerator.MoveNext())
            {
                OnReplayCompleted();
                _replayEnumerator = null;
            }
        }

        void TogglePlayback(ChangeEvent<bool> evt)
        {
            if (!m_Debugger.isReplaying)
                return;

            m_Debugger.isPlaybackPaused = evt.newValue;
            if (m_Debugger.isPlaybackPaused)
                m_PlaybackLabel.text = m_PlaybackLabel.text.Replace("Replaying", "Paused");
            else
                m_PlaybackLabel.text = m_PlaybackLabel.text.Replace("Paused", "Replaying");
        }

        void RefreshFromReplay(int i, int count)
        {
            m_PlaybackLabel.text = $"{(m_Debugger.isPlaybackPaused ? "Paused" : "Replaying")} {i}/{count}...";
            Refresh();
        }

        void UpdatePlaybackSpeed(ChangeEvent<string> changeEvent)
        {
            if (!float.TryParse(changeEvent.newValue, out float result))
                return;

            UpdatePlaybackSpeed(result);
        }

        void UpdatePlaybackSpeed(float playbackSpeed)
        {
            var slowest = m_PlaybackSpeeds[0];
            var fastest = m_PlaybackSpeeds[m_PlaybackSpeeds.Length - 1];
            var newValue = Mathf.Max(slowest, Mathf.Min(fastest, playbackSpeed));

            m_Debugger.playbackSpeed = newValue;
            m_PlaybackSpeedTextField.SetValueWithoutNotify(m_Debugger.playbackSpeed.ToString("F1"));
            m_DecreasePlaybackSpeedButton.SetEnabled(newValue > slowest);
            m_IncreasePlaybackSpeedButton.SetEnabled(newValue < fastest);
        }

        void SaveReplaySessionFromSelection()
        {
            var path = EditorUtility.SaveFilePanel("Save Replay File", Application.dataPath, "ReplayData.json", "json");
            m_Debugger.SaveReplaySessionFromSelection(path, m_SelectedEvents.Select(x => x.eventBase).ToList());
        }

        void LoadReplaySession()
        {
            var path = EditorUtility.OpenFilePanel("Select Replay File", "", "json");
            var savedRecord = m_Debugger.LoadReplaySession(path);
            if (savedRecord == null)
                return;

            ReplayEvents(savedRecord.eventList);
        }

        void HighlightCodeLine(int hashcode)
        {
            var matchingIndex = -1;
            for (var i = 0; i < m_RegisteredEventCallbacksDataSource.Count; i++)
            {
                var data = m_RegisteredEventCallbacksDataSource[i];
                if (data.type == LineType.CodeLine && data is CodeLineInfo codeLineData)
                {
                    var matchesHashcode = codeLineData.lineHashCode == hashcode;
                    if (matchesHashcode)
                    {
                        matchingIndex = i;
                        codeLineData.highlighted = true;
                    }
                }
            }

            if (matchingIndex >= 0)
            {
                m_EventRegistrationsListView.Refresh();
                m_EventRegistrationsListView.ScrollToItem(matchingIndex);
            }
        }

        void UpdateEventsLog()
        {
            if (m_Log == null)
                return;

            m_Log.Clear();

            var activeEventTypes = new List<long>();

            foreach (var s in m_EventTypeFilter.State)
            {
                if (s.Value)
                {
                    activeEventTypes.Add(s.Key);
                }
            }

            bool allActive = activeEventTypes.Count == m_EventTypeFilter.State.Count;
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
            ClearOverlays();
        }

        void OnListFocusedOut(FocusOutEvent evt)
        {
            ClearOverlays();
        }

        void OnEventsLogSelectionChanged(IEnumerable<object> obj)
        {
            if (m_SelectedEvents == null)
                m_SelectedEvents = new List<EventLogLine>();
            m_SelectedEvents.Clear();
            ClearOverlays();

            if (obj != null)
            {
                foreach (EventLogLine listItem in obj)
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
            UpdatePlaybackButtons();

            if (m_SelectedEvents.Count == 1)
            {
                UpdateEventCallbacks(eventBase);
                UpdateEventPropagationPaths(eventBase);
                UpdateEventBaseInfo(eventBase, focused, capture);
            }
            else
            {
                ClearEventCallbacks();
                ClearEventPropagationPaths();
                ClearEventBaseInfo();
            }

            DisplayHistogram(m_EventsHistogramScrollView);
        }

        void ClearEventBaseInfo()
        {
            m_EventBaseInfo.text = "";
        }

        void UpdateEventBaseInfo(EventDebuggerEventRecord eventBase, IEventHandler focused, IEventHandler capture)
        {
            ClearEventBaseInfo();

            if (eventBase == null)
                return;

            m_EventBaseInfo.text += "Focused element: " + EventDebugger.GetObjectDisplayName(focused) + "\n";
            m_EventBaseInfo.text += "Capture element: " + EventDebugger.GetObjectDisplayName(capture) + "\n";

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
                m_EventBaseInfo.text += "Mouse position: " + eventBase.mousePosition + "\n";
                m_EventBaseInfo.text += "Modifiers: " + eventBase.modifiers + "\n";
            }

            if (eventBase.eventTypeId == KeyDownEvent.TypeId() ||
                eventBase.eventTypeId == KeyUpEvent.TypeId())
            {
                m_EventBaseInfo.text += "Modifiers: " + eventBase.modifiers + "\n";
            }

            if (eventBase.eventTypeId == MouseDownEvent.TypeId() ||
                eventBase.eventTypeId == MouseUpEvent.TypeId() ||
                eventBase.eventTypeId == PointerDownEvent.TypeId() ||
                eventBase.eventTypeId == PointerUpEvent.TypeId() ||
                eventBase.eventTypeId == DragUpdatedEvent.TypeId() ||
                eventBase.eventTypeId == DragPerformEvent.TypeId() ||
                eventBase.eventTypeId == DragExitedEvent.TypeId())
            {
                m_EventBaseInfo.text += "Button: " + (eventBase.button == 0 ? "Left" : eventBase.button == 1 ? "Middle" : "Right") + "\n";
                m_EventBaseInfo.text += "Click count: " + eventBase.clickCount + "\n";
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
                m_EventBaseInfo.text += "Pressed buttons: " + eventBase.pressedButtons + "\n";
            }

            if (eventBase.eventTypeId == WheelEvent.TypeId())
            {
                m_EventBaseInfo.text += "Mouse delta: " + eventBase.delta + "\n";
            }

            if (eventBase.eventTypeId == KeyDownEvent.TypeId() ||
                eventBase.eventTypeId == KeyUpEvent.TypeId())
            {
                if (char.IsControl(eventBase.character))
                {
                    m_EventBaseInfo.text += "Character: \\" + (byte)(eventBase.character) + "\n";
                }
                else
                {
                    m_EventBaseInfo.text += "Character: " + eventBase.character + "\n";
                }

                m_EventBaseInfo.text += "Key code: " + eventBase.keyCode + "\n";
            }

            if (eventBase.eventTypeId == ValidateCommandEvent.TypeId() ||
                eventBase.eventTypeId == ExecuteCommandEvent.TypeId())
            {
                m_EventBaseInfo.text += "Command: " + eventBase.commandName + "\n";
            }
        }

        void OnFilterChange(ChangeEvent<string> e)
        {
            if (e.newValue != null)
                return;

            m_Debugger.UpdateModificationCount();
            Refresh();
            BuildEventsLog();
            DisplayRegisteredEventCallbacks();
        }

        void OnRegisteredCallbackFilterChange(ChangeEvent<string> e)
        {
            DisplayRegisteredEventCallbacks();
        }

        void ClearEventCallbacks()
        {
            foreach (var data in m_RegisteredEventCallbacksDataSource)
            {
                if (data.type == LineType.CodeLine && data is CodeLineInfo codeLineData)
                {
                    codeLineData.highlighted = false;
                }
            }

            m_EventRegistrationsListView.Refresh();
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

                    Label timeStamp = new Label();
                    timeStamp.AddToClassList("timestamp");
                    Label handler = new Label();
                    handler.AddToClassList("handler");
                    Label phaseDurationContainer = new Label { name = "phaseDurationContainer" };
                    Label phase = new Label();
                    phase.AddToClassList("phase");
                    Label duration = new Label();
                    duration.AddToClassList("duration");

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
                    duration.text = callback.duration / 1000f + "ms";

                    container.Add(timeStamp);
                    container.Add(handler);
                    phaseDurationContainer.Add(phase);
                    phaseDurationContainer.Add(duration);
                    container.Add(phaseDurationContainer);

                    m_EventCallbacksScrollView.Add(container);

                    var hash = callback.callbackHashCode;
                    HighlightCodeLine(hash);
                }
            }

            var defaultActions = m_Debugger.GetDefaultActions(panel, eventBase);
            if (defaultActions == null)
                return;

            foreach (EventDebuggerDefaultActionTrace defaultAction in defaultActions)
            {
                VisualElement container = new VisualElement { name = "line-container" };

                Label timeStamp = new Label();
                timeStamp.AddToClassList("timestamp");
                Label handler = new Label();
                handler.AddToClassList("handler");
                Label phaseDurationContainer = new Label { name = "phaseDurationContainer" };
                Label phase = new Label();
                phase.AddToClassList("phase");
                Label duration = new Label();
                duration.AddToClassList("duration");

                timeStamp.AddToClassList("log-line-item");
                handler.AddToClassList("log-line-item");
                phaseDurationContainer.AddToClassList("log-line-item");

                timeStamp.text = "[" + eventBase.TimestampString() + "]";
                handler.text = defaultAction.targetName + "." +
                    (defaultAction.phase == PropagationPhase.AtTarget
                        ? "ExecuteDefaultActionAtTarget"
                        : "ExecuteDefaultAction");

                duration.text = defaultAction.duration / 1000f + "ms";

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
                    var i = 0;
                    foreach (var t in targets)
                    {
                        var targetName = t.name;
                        if (string.IsNullOrEmpty(targetName))
                            targetName = t.GetType().Name;
                        m_EventPropagationPaths.text += "    " + targetName + "\n";

                        HighlightElement(t, i++ == 0);
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
            if (m_MaxLogLines)
            {
                m_StartIndex = Math.Max(0, m_Log.lines.Count - m_MaxLogLineCount);
                m_EventsLog.itemsSource = m_Log.lines.Skip(m_StartIndex).Take(m_MaxLogLineCount).ToList();
            }
            else
            {
                m_StartIndex = 0;
                m_EventsLog.itemsSource = ToList();
            }

            m_EventsLog.itemHeight = 15;
            m_EventsLog.bindItem = (target, index) =>
            {
                var line = m_Log.lines[index + m_StartIndex];

                // Add text
                VisualElement lineText = target.MandatoryQ<VisualElement>("log-line");
                if (lineText[0] is Label theLabel)
                {
                    theLabel.text = line.timestamp;
                    theLabel = lineText[1] as Label;
                    if (theLabel != null)
                        theLabel.text = line.eventName;
                    theLabel = lineText[2] as Label;
                    if (theLabel != null)
                        theLabel.text = line.target;
                }
            };
            m_EventsLog.makeItem = () =>
            {
                VisualElement container = new VisualElement { name = "log-line" };
                Label timeStamp = new Label();
                timeStamp.AddToClassList("timestamp");
                Label eventLabel = new Label();
                eventLabel.AddToClassList("event");
                Label target = new Label();
                target.AddToClassList("target");

                timeStamp.AddToClassList("log-line-item");
                eventLabel.AddToClassList("log-line-item");
                target.AddToClassList("log-line-item");

                container.Add(timeStamp);
                container.Add(eventLabel);
                container.Add(target);

                return container;
            };

            m_EventsLog.Refresh();
            DoAutoScroll();
        }

        void DoAutoScroll()
        {
            if (m_AutoScroll)
                rootVisualElement.schedule.Execute(() => m_EventsLog.ScrollToItem(-1));
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
            OnReplayCompleted();
        }

        void ClearOverlays()
        {
            m_HighlightOverlay?.ClearOverlay();
            m_RepaintOverlay?.ClearOverlay();
            m_Debugger.panelDebug?.MarkDirtyRepaint();
            m_Debugger.panelDebug?.MarkDebugContainerDirtyRepaint();
        }

        void OnReplayCompleted()
        {
            m_Debugger.StopPlayback();

            if (m_TogglePlayback != null)
                m_TogglePlayback.value = false;
            if (m_PlaybackLabel != null)
                m_PlaybackLabel.text = "";
            UpdatePlaybackButtons();
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType changeTypeFlag) {}

        protected override void OnSelectPanelDebug(IPanelDebug selectedPanelDebug)
        {
            if (selectedPanelDebug == m_Debugger.panelDebug)
                return;

            if (selectedPanelDebug != null)
            {
                if (m_Debugger.panelDebug?.debugContainer != null)
                    m_Debugger.panelDebug.debugContainer.generateVisualContent -= OnGenerateVisualContent;

                m_Debugger.panelDebug = selectedPanelDebug;
                m_Debugger.panel = selectedPanelDebug.panel;

                if (m_Debugger.panelDebug.debugContainer != null)
                    m_Debugger.panelDebug.debugContainer.generateVisualContent += OnGenerateVisualContent;
            }

            m_EventTypeFilter.SetEventLog(m_Debugger.eventTypeProcessedCount);

            DisplayRegisteredEventCallbacks();
            Refresh();

            var erTitle = rootVisualElement.MandatoryQ<Toggle>("eventsRegistrationTitle");
            const string prefix = "Registered Event Callbacks";
            erTitle.text = panel != null ? prefix + " in " + ((Panel)panel).name : prefix + " [No Panel Selected]";
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
            UpdatePlaybackButtons();
            DisplayHistogram(m_EventsHistogramScrollView);
        }

        void OnGenerateVisualContent(MeshGenerationContext context)
        {
            m_HighlightOverlay?.Draw(context);
            m_RepaintOverlay?.Draw(context);
        }

        void UpdateLogCount()
        {
            if (m_LogCountLabel == null || m_Log?.lines == null)
                return;

            m_LogCountLabel.text = m_Log.lines.Count + " event" + (m_Log.lines.Count > 1 ? "s" : "");
        }

        void UpdateSelectionCount()
        {
            if (m_SelectionCountLabel == null)
                return;

            m_SelectionCountLabel.text =
                "(" + (m_SelectedEvents != null ? m_SelectedEvents.Count.ToString() : "0") + " selected)";
        }

        void UpdatePlaybackButtons()
        {
            var anySelected = m_SelectedEvents != null && m_SelectedEvents.Any();
            m_TogglePlayback?.SetEnabled(m_Debugger.isReplaying);
            m_StopPlaybackButton?.SetEnabled(m_Debugger.isReplaying);
            m_SaveReplayButton?.SetEnabled(anySelected);
            m_StartPlaybackButton?.SetEnabled(anySelected && !m_Debugger.isReplaying);
        }
    }
}
