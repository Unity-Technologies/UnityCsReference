// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.EditorAnalyticsDebugger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "The base EditorWindow constructor schedules a delayed call; this window is recreated on code reload")]
    class AnalyticsDebuggerWindow : EditorWindow
    {
        const string k_RecordPrefKey = "AnalyticsDebugger.RecordEvents";
        const string k_GroupPrefKey = "AnalyticsDebugger.GroupByEvent";
        const string k_ResourcePath = "EditorAnalyticsDebugger/analytics-debugger";
        const double k_BannerRefreshIntervalSeconds = 1;

        readonly AnalyticsEventLog m_Log = new AnalyticsEventLog();
        readonly List<AnalyticsEventRecord> m_RecordBuffer = new List<AnalyticsEventRecord>();
        readonly List<AnalyticsEventEntry> m_EntryBuffer = new List<AnalyticsEventEntry>();
        readonly HashSet<int> m_ExpandedGroupIds = new HashSet<int>();

        bool m_RecordEvents;
        bool m_PendingEnableNativeRecording;
        double m_NextBannerRefresh;
        AnalyticsEventEntry m_SelectedEntry;

        [SerializeField] bool m_HasBeenEnabledBefore;

        TreeView m_TreeView;
        Label m_NoDataMessage;
        Label m_StatusLine;
        HelpBox m_AnalyticsDisabledBanner;
        HelpBox m_DroppedBanner;
        TextField m_DetailText;
        Button m_CopyDetail;

        [MenuItem("Window/Internal/Analytics Debugger", false, 3000, true)]
        public static void Init()
        {
            GetWindow<AnalyticsDebuggerWindow>(false, "Analytics Debugger");
        }

        public void OnEnable()
        {
            bool newlyOpened = !m_HasBeenEnabledBefore;
            m_HasBeenEnabledBefore = true;

            LoadAssets();
            ResolveElements();
            RestorePreferences(newlyOpened);
            RegisterCallbacks();
            RefreshTree();
        }

        public void OnDisable()
        {
            EditorPrefs.SetBool(k_RecordPrefKey, m_RecordEvents);
            EditorPrefs.SetBool(k_GroupPrefKey, m_Log.groupByEvent);
        }

        void LoadAssets()
        {
            var tree = EditorGUIUtility.Load(k_ResourcePath + ".uxml") as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.Load(k_ResourcePath + ".uss") as StyleSheet;

            rootVisualElement.Clear();
            if (tree == null)
            {
                rootVisualElement.Add(new Label("Analytics Debugger UI resources could not be loaded."));
                return;
            }

            tree.CloneTree(rootVisualElement);
            if (stylesheet != null)
                rootVisualElement.styleSheets.Add(stylesheet);
        }

        void ResolveElements()
        {
            m_TreeView = rootVisualElement.Q<TreeView>("filteredListEvents");
            m_NoDataMessage = rootVisualElement.Q<Label>("no-data-message");
            m_StatusLine = rootVisualElement.Q<Label>("status-line");
            m_AnalyticsDisabledBanner = rootVisualElement.Q<HelpBox>("analytics-disabled-banner");
            m_DroppedBanner = rootVisualElement.Q<HelpBox>("dropped-banner");
            m_DetailText = rootVisualElement.Q<TextField>("prettified-event");
            m_CopyDetail = rootVisualElement.Q<Button>("copy-pretty");
        }

        void RestorePreferences(bool newlyOpened)
        {
            m_RecordEvents = newlyOpened || EditorPrefs.GetBool(k_RecordPrefKey, true);
            m_Log.groupByEvent = EditorPrefs.GetBool(k_GroupPrefKey, false);

            m_PendingEnableNativeRecording = m_RecordEvents;

            rootVisualElement.Q<Toggle>("record-toggle")?.SetValueWithoutNotify(m_RecordEvents);
            rootVisualElement.Q<Toggle>("group-toggle")?.SetValueWithoutNotify(m_Log.groupByEvent);
        }

        void RegisterCallbacks()
        {
            if (m_TreeView != null)
            {
                m_TreeView.makeItem = MakeRow;
                m_TreeView.bindItem = BindRow;
                m_TreeView.selectionChanged += OnSelectionChanged;
            }

            rootVisualElement.Q<ToolbarSearchField>("search")?.RegisterValueChangedCallback(evt =>
            {
                m_Log.filter = evt.newValue;
                RefreshTree();
            });

            rootVisualElement.Q<Toggle>("record-toggle")?.RegisterValueChangedCallback(evt => SetRecording(evt.newValue));

            rootVisualElement.Q<Toggle>("group-toggle")?.RegisterValueChangedCallback(evt =>
            {
                m_Log.groupByEvent = evt.newValue;
                RefreshTree();
            });

            var clear = rootVisualElement.Q<Button>("clear-data");
            if (clear != null)
                clear.clicked += ConfirmAndClearDataFeed;

            var copySelected = rootVisualElement.Q<Button>("copy-selected");
            if (copySelected != null)
                copySelected.clicked += CopySelected;

            var export = rootVisualElement.Q<Button>("export");
            if (export != null)
                export.clicked += ExportVisibleEvents;

            if (m_CopyDetail != null)
                m_CopyDetail.clicked += CopyDetail;
        }

        void SetRecording(bool value)
        {
            m_RecordEvents = value;
            EditorAnalytics.recordEventsEnabled = value;
            EditorPrefs.SetBool(k_RecordPrefKey, value);
            RefreshBanners();
        }

        public void Update()
        {
            if (m_PendingEnableNativeRecording)
            {
                m_PendingEnableNativeRecording = false;
                EditorAnalytics.recordEventsEnabled = true;
            }

            if (EditorApplication.timeSinceStartup >= m_NextBannerRefresh)
            {
                m_NextBannerRefresh = EditorApplication.timeSinceStartup + k_BannerRefreshIntervalSeconds;
                RefreshBanners();
            }

            if (!m_RecordEvents)
                return;

            m_RecordBuffer.Clear();
            DebuggerEventListHandler.DrainRecords(m_RecordBuffer, out int dropped);
            m_Log.NoteDropped(dropped);

            if (m_RecordBuffer.Count == 0)
            {
                if (dropped > 0)
                    RefreshBanners();
                return;
            }

            m_EntryBuffer.Clear();
            for (int i = 0; i < m_RecordBuffer.Count; i++)
            {
                AnalyticsEventRecord record = m_RecordBuffer[i];
                m_EntryBuffer.Add(new AnalyticsEventEntry(record.eventName, record.payloadJson, record.fate,
                    new DateTime(record.timestampTicks, DateTimeKind.Utc)));
            }

            if (m_Log.Add(m_EntryBuffer))
                RefreshTree();
        }

        VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("event-row");

            var icon = new Image { name = "icon" };
            icon.AddToClassList("event-icon");
            row.Add(icon);

            var text = new Label { name = "text" };
            text.AddToClassList("event-text");
            row.Add(text);

            return row;
        }

        void BindRow(VisualElement element, int index)
        {
            DisplayRow row = m_TreeView.GetItemDataForIndex<DisplayRow>(index);

            var icon = element.Q<Image>("icon");
            var text = element.Q<Label>("text");

            bool isGroupHeader = row.groupKey != null;

            icon.image = isGroupHeader ? null : FateIcon(row.entry);
            icon.tooltip = isGroupHeader ? "" : FateTooltip(row.entry);

            text.text = row.text;
        }

        static Texture FateIcon(AnalyticsEventEntry entry)
        {
            if (entry == null)
                return null;

            switch (entry.fate)
            {
                case AnalyticsEventFate.Recorded:
                    return EditorGUIUtility.LoadIcon("EditorUI/True");
                case AnalyticsEventFate.RateLimited:
                    return EditorGUIUtility.LoadIcon("EditorUI/Pending");
                case AnalyticsEventFate.AnalyticsDisabled:
                    return EditorGUIUtility.LoadIcon("EditorUI/False");
                default:
                    return EditorGUIUtility.LoadIcon("console.warnicon.sml");
            }
        }

        static string FateTooltip(AnalyticsEventEntry entry)
        {
            if (entry == null)
                return "";

            switch (entry.fate)
            {
                case AnalyticsEventFate.Recorded:
                    return "Recorded and queued for sending. Delivery is not confirmed here.";
                case AnalyticsEventFate.RateLimited:
                    return "Not sent: the event's maxEventsPerHour limit was reached.";
                case AnalyticsEventFate.AnalyticsDisabled:
                    return "Not sent: editor analytics are disabled.";
                default:
                    return "This editor reported an outcome the debugger does not recognize.";
            }
        }

        void RefreshTree()
        {
            if (m_TreeView != null)
                ReplaceTreeItems();

            bool empty = m_Log.visibleEventCount == 0;
            if (m_NoDataMessage != null)
                m_NoDataMessage.style.display = empty ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_TreeView != null)
                m_TreeView.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;

            if (m_SelectedEntry != null && !m_Log.Contains(m_SelectedEntry))
                ClearSelection();

            RefreshStatusLine();
        }

        void ReplaceTreeItems()
        {
            m_ExpandedGroupIds.Clear();

            if (m_TreeView.viewController != null)
            {
                foreach (int id in m_TreeView.GetRootIds())
                {
                    if (m_TreeView.IsExpanded(id))
                        m_ExpandedGroupIds.Add(id);
                }
            }

            m_TreeView.SetRootItems(m_Log.rootItems);

            if (m_ExpandedGroupIds.Count > 0)
            {
                foreach (int id in m_TreeView.GetRootIds())
                {
                    if (m_ExpandedGroupIds.Contains(id))
                        m_TreeView.ExpandItem(id, false, false);
                }
            }

            m_TreeView.Rebuild();
        }

        void RefreshStatusLine()
        {
            if (m_StatusLine == null)
                return;

            var text = new StringBuilder();
            if (m_Log.groupByEvent)
                text.Append(m_Log.visibleGroupCount).Append(" event types");
            else
                text.Append(m_Log.visibleEventCount).Append(" events");

            if (!m_Log.groupByEvent && m_Log.visibleEventCount != m_Log.retainedEventCount)
                text.Append(" (of ").Append(m_Log.retainedEventCount).Append(')');

            if (m_Log.evictedEventCount > 0)
                text.Append("  ·  ").Append(m_Log.evictedEventCount).Append(" older events discarded");

            m_StatusLine.text = text.ToString();
        }

        void RefreshBanners()
        {
            SetBanner(m_AnalyticsDisabledBanner, !EditorAnalytics.enabled,
                "Editor analytics are disabled, so no events are being sent. Events recorded while disabled are " +
                "still listed here and marked as not sent, so you can see what would have been collected.");

            SetBanner(m_DroppedBanner, m_Log.droppedEventCount > 0,
                $"{m_Log.droppedEventCount} events were discarded before the debugger could read them because they " +
                "arrived faster than the window drained them.");
        }

        static void SetBanner(HelpBox banner, bool visible, string message)
        {
            if (banner == null)
                return;

            banner.text = message;
            banner.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnSelectionChanged(IEnumerable<object> selection)
        {
            AnalyticsEventEntry first = null;

            if (selection != null)
            {
                foreach (object item in selection)
                {
                    if (item is DisplayRow row)
                    {
                        first = row.entry;
                        break;
                    }
                }
            }

            if (first == null)
            {
                ClearSelection();
                return;
            }

            m_SelectedEntry = first;
            if (m_DetailText != null)
            {
                m_DetailText.style.display = DisplayStyle.Flex;
                m_DetailText.value = Prettify(first.payloadJson);
            }
            if (m_CopyDetail != null)
                m_CopyDetail.style.display = DisplayStyle.Flex;
        }

        void ClearSelection()
        {
            m_SelectedEntry = null;
            m_TreeView?.ClearSelection();

            if (m_DetailText != null)
            {
                m_DetailText.value = "";
                m_DetailText.style.display = DisplayStyle.None;
            }
            if (m_CopyDetail != null)
                m_CopyDetail.style.display = DisplayStyle.None;
        }

        void ConfirmAndClearDataFeed()
        {
            if (m_Log.retainedEventCount == 0)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Clear captured events?",
                "This discards every captured event, including any hidden by the current search filter. " +
                "This cannot be undone.",
                "Clear",
                "Cancel");

            if (confirmed)
                ClearDataFeed();
        }

        public void ClearDataFeed()
        {
            m_Log.Clear();
            DebuggerEventListHandler.ClearRecords();
            ClearSelection();
            RefreshTree();
        }

        void CopySelected()
        {
            if (m_TreeView?.selectedItems == null)
                return;

            var text = new StringBuilder();
            foreach (object item in m_TreeView.selectedItems)
            {
                if (item is DisplayRow row && row.entry != null)
                    text.AppendLine(row.entry.payloadJson);
            }

            if (text.Length > 0)
                GUIUtility.systemCopyBuffer = text.ToString();
        }

        void CopyDetail()
        {
            if (m_SelectedEntry != null)
                GUIUtility.systemCopyBuffer = Prettify(m_SelectedEntry.payloadJson);
        }

        void ExportVisibleEvents()
        {
            if (m_Log.visibleEventCount == 0)
            {
                EditorUtility.DisplayDialog("Export events", "There are no events to export.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export analytics events", "", "analytics-events.json", "json");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                File.WriteAllText(path, BuildExportText());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Analytics Debugger could not write {path}: {exception.Message}");
            }
        }

        internal string BuildExportText()
        {
            var events = new List<AnalyticsEventEntry>();
            m_Log.GetVisibleEvents(events);

            var text = new StringBuilder();
            text.Append("[\n");
            for (int i = 0; i < events.Count; i++)
            {
                text.Append("  ").Append(events[i].payloadJson);
                if (i < events.Count - 1)
                    text.Append(',');
                text.Append('\n');
            }
            text.Append("]\n");
            return text.ToString();
        }

        internal static string Prettify(string json)
        {
            if (string.IsNullOrEmpty(json))
                return "";

            var result = new StringBuilder(json.Length * 2);
            int indent = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (escaped)
                {
                    result.Append(c);
                    escaped = false;
                    continue;
                }

                if (inString)
                {
                    result.Append(c);
                    if (c == '\\')
                        escaped = true;
                    else if (c == '"')
                        inString = false;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inString = true;
                        result.Append(c);
                        break;

                    case '{':
                    case '[':
                        result.Append(c);
                        AppendIndentedNewLine(result, ++indent);
                        break;

                    case '}':
                    case ']':
                        AppendIndentedNewLine(result, --indent);
                        result.Append(c);
                        break;

                    case ',':
                        result.Append(c);
                        AppendIndentedNewLine(result, indent);
                        break;

                    case ':':
                        result.Append(": ");
                        break;

                    default:
                        if (!char.IsWhiteSpace(c))
                            result.Append(c);
                        break;
                }
            }

            return result.ToString();
        }

        static void AppendIndentedNewLine(StringBuilder text, int indent)
        {
            text.Append('\n');
            for (int i = 0; i < indent; i++)
                text.Append("    ");
        }

        internal AnalyticsEventLog log
        {
            get { return m_Log; }
        }

        internal void SetPollingForTesting(bool polling)
        {
            m_RecordEvents = polling;
        }
    }
}
