// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using Unity.Collections;
using System.Text;
using System;
using UnityEngine.UIElements.Experimental;

using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.JobsProfiling;

internal class JobsList
{
    struct JobListData
    {
        internal string name;
        internal float time;
        internal int markerId;
        internal int eventIndex;
        internal int frameIndex;
    }

    const int kRowShift = 16;
    const int kRowMask = (1 << kRowShift) - 1;
    ClickLinkEvent m_clickOrHoverEvent = ClickLinkEvent.CreateEmpty();
    JobSelection m_jobSelection = new JobSelection();

    MultiColumnListView m_view;
    List<JobListData> m_data = new List<JobListData>();

    [NoAutoStaticsCleanup]
    static string[] m_columnNames =
    {
        "Name",
        "Time",
    };

    void ColumnData(VisualElement element, int row)
    {
        Label label = element as Label;
        int columnIndex = (int)element.userData;

        if (columnIndex == 0)
        {
            label.text = String.Format("<link=\"{0}\"><color={1}>{2}</color></link>",
                                                (row << kRowShift) + columnIndex, JobsProfilerSettings.LinkColorHex, m_data[row].name);
        }
        else if (columnIndex == 1)
        {
            label.text = String.Format("<link=\"{0}\"><color={1}>{2:0.000} ms</color></link>",
                                      (row << kRowShift) + columnIndex, JobsProfilerSettings.LinkColorHex, m_data[row].time);
        }
    }

    internal JobsList(VisualElement root, StyleSheet styleSheet, string serializeName)
    {
        int i = 0;

        var columns = new Columns();

        foreach (var name in m_columnNames)
        {
            var c = new Column();
            c.name = name;
            c.title = name;
            var index = i++;
            c.makeCell = () =>
            {
                var label = new Label();
                label.RegisterCallback<PointerUpLinkTagEvent>(LinkUpClicked);
                label.RegisterCallback<PointerOverLinkTagEvent>(HyperlinkOnPointerOver);
                label.RegisterCallback<PointerOutLinkTagEvent>(HyperlinkOnPointerOut);
                label.styleSheets.Add(styleSheet);
                label.style.paddingLeft = 6;
                label.userData = index;
                return label;
            };
            c.bindCell = ColumnData;
            c.stretchable = true;
            columns.Add(c);
        }

        m_view = new MultiColumnListView(columns);
        m_view.viewDataKey = serializeName;
        m_view.itemsSource = m_data;
        m_view.style.flexGrow = 1;
        m_view.styleSheets.Add(styleSheet);
        m_view.sortingMode = ColumnSortingMode.Custom;
        m_view.columnSortingChanged += SortingChanged;
        m_view.AddToClassList("jobs-profiler-info-panel-border");
        m_view.style.borderTopWidth = 1;
        m_view.style.borderLeftWidth = 1;
        m_view.style.borderRightWidth = 1;
        m_view.style.borderBottomWidth = 1;

        root.Add(m_view);
    }

    void SortingChanged()
    {
        foreach (var v in m_view.sortedColumns)
        {
            if (v.columnName == "Name")
                if (v.direction == SortDirection.Ascending)
                    m_data.Sort((a, b) => (a.name.CompareTo(b.name)));
                else
                    m_data.Sort((a, b) => (b.name.CompareTo(a.name)));
            else if (v.columnName == "Time")
                if (v.direction == SortDirection.Ascending)
                    m_data.Sort((a, b) => (a.time.CompareTo(b.time)));
                else
                    m_data.Sort((a, b) => (b.time.CompareTo(a.time)));
        }

        m_view.RefreshItems();
    }

    internal void UpdateData(in NativeList<DependJobInfo> jobs, FrameCache frameCache, JobSelection jobSelection)
    {
        m_data.Clear();
        m_jobSelection = jobSelection;

        for (int i = 0; i < jobs.Length; i++)
        {
            DependJobInfo info = jobs[i];
            if (info.eventIndex == -1)
                continue;

            FrameData frame;

            if (!frameCache.GetFrame(info.frameIndex, out frame))
                continue;

            int markerId = frame.events[info.eventIndex].markerId;
            float time = frame.events[(int)info.eventIndex].time;

            var text = frameCache.GetEventStringForFrame(info.frameIndex, info.eventIndex);

            var data = new JobListData
            {
                name = text,
                time = time,
                markerId = markerId,
                eventIndex = info.eventIndex,
                frameIndex = info.frameIndex,
            };

            m_data.Add(data);
        }

        SortingChanged();
    }

    internal void Clear()
    {
        m_data.Clear();
        m_view.RefreshItems();
    }

    void UpdateSelection(int markerId, int eventIndex, int frameIndex, bool hover)
    {
        m_clickOrHoverEvent.jobId = markerId;
        m_clickOrHoverEvent.frameIndex = frameIndex;
        m_clickOrHoverEvent.eventIndex = eventIndex;
        m_clickOrHoverEvent.hover = hover;
    }

    void HoverOrClick(int linkID, bool hover)
    {
        var row = linkID >> kRowShift;
        JobListData data = m_data[row];
        UpdateSelection(data.markerId, data.eventIndex, data.frameIndex, hover);
    }

    internal void HyperlinkOnPointerOver(PointerOverLinkTagEvent evt)
    {
        var linkID = int.Parse(evt.linkID);
        HoverOrClick(linkID, true);

        var label = evt.currentTarget as Label;
        label.AddToClassList("link-cursor");
        // Add underline tags around the link content
        string text = label.text;
        int start = text.IndexOf('>', text.IndexOf("<color=")) + 1;
        int end = text.IndexOf("</color>");
        if (start > 0 && end > start)
        {
            label.text = text.Insert(end, "</u>").Insert(start, "<u>");
        }
    }
    internal void HyperlinkOnPointerOut(PointerOutLinkTagEvent evt)
    {
        UpdateSelection(
            m_jobSelection.markerId,
            m_jobSelection.eventIndex,
            m_jobSelection.frameIndex,
            false);

        var label = evt.target as Label;
        label.RemoveFromClassList("link-cursor");
        // Remove underline tags
        label.text = label.text.Replace("<u>", "").Replace("</u>", "");
    }

    void LinkUpClicked(PointerUpLinkTagEvent evt)
    {
        var linkID = int.Parse(evt.linkID);
        HoverOrClick(linkID, false);
    }

    internal ClickLinkEvent ClickOrHoverEvent { get { return m_clickOrHoverEvent; } }

    internal void ClearClickOrHoverEvent()
    {
        m_clickOrHoverEvent = ClickLinkEvent.CreateEmpty();
    }
}

internal class JobsInfoPanel
{
    struct HistoryEntry
    {
        internal int frameIndex;
        internal int eventIndex;
        internal int tabIndex;
    }

    const int kMaxHistory = 100;

    Label m_jobName;
    Label m_jobTime;
    Label m_scheduledBy;
    Label m_completedBy;

    Tab m_tabDependsOn;
    Tab m_tabDependencies;
    Label m_dependsOnCount;
    Label m_dependingCount;

    JobsList m_dependsOn;
    JobsList m_dependantOn;

    VisualElement m_infoPanelData;

    Button m_backButton;
    Button m_forwardButton;
    TabView m_tabView;

    List<HistoryEntry> m_history = new List<HistoryEntry>();
    int m_historyIndex = -1;
    bool m_hasPendingHistoryNav;
    ClickLinkEvent m_historyNavEvent;
    int m_pendingHistoryTab = -1;

    internal JobsInfoPanel(in VisualElement root, StyleSheet labelStyleSheet)
    {
        m_jobName = root.Query<Label>("job_name").First();

        m_scheduledBy = root.Query<Label>("scheduled_by").First();
        m_completedBy = root.Query<Label>("completed_by").First();
        m_jobTime = root.Query<Label>("job_time").First();
        m_infoPanelData = root.Query<VisualElement>("jobs_info_data");

        m_tabDependsOn   = root.Q<Tab>("tab_depends_on");
        m_tabDependencies = root.Q<Tab>("tab_dependencies");
        m_dependsOnCount  = root.Q<Label>("depends_on_count");
        m_dependingCount  = root.Q<Label>("depending_count");

        m_tabView = root.Q<TabView>("jobs_info_tabs");

        m_backButton = root.Q<Button>("nav_back");
        m_forwardButton = root.Q<Button>("nav_forward");
        m_backButton.clicked += OnBackClicked;
        m_forwardButton.clicked += OnForwardClicked;
        UpdateNavButtonStates();

        m_scheduledBy.RegisterCallback<PointerOverLinkTagEvent>(Stats.HyperlinkOnPointerOver);
        m_scheduledBy.RegisterCallback<PointerOutLinkTagEvent>(Stats.HyperlinkOnPointerOut);

        m_completedBy.RegisterCallback<PointerOverLinkTagEvent>(Stats.HyperlinkOnPointerOver);
        m_completedBy.RegisterCallback<PointerOutLinkTagEvent>(Stats.HyperlinkOnPointerOut);

        var dependsOnArea = root.Query<VisualElement>("depends_on_list_area").First();
        var dependantOnArea = root.Query<VisualElement>("dependant_on_list_area").First();

        m_dependsOn = new JobsList(dependsOnArea, labelStyleSheet, "JobsProfiler.jobsDependsOn");
        m_dependantOn = new JobsList(dependantOnArea, labelStyleSheet, "JobsProfiler.jobsDependantOn");
    }

    internal void Update(
        in NativeList<DependJobInfo> dependsOnJobs,
        in NativeList<DependJobInfo> dependantOnJobs,
        JobSelection jobSelection,
        FrameCache frameCache)
    {
        m_dependsOn.UpdateData(dependsOnJobs, frameCache, jobSelection);
        m_dependantOn.UpdateData(dependantOnJobs, frameCache, jobSelection);

        int n = dependsOnJobs.Length;
        int m = dependantOnJobs.Length;
        m_tabDependsOn.label    = n > 0 ? $"Depends on ({n})"   : "Depends on";
        m_tabDependencies.label = m > 0 ? $"Dependencies ({m})" : "Dependencies";
        m_dependsOnCount.text   = $"\u2192 {n} (depends on)";
        m_dependingCount.text   = $"\u2192 {m} (depending)";
    }

    internal ClickLinkEvent GetClickOrHoverEvent()
    {
        ClickLinkEvent e0 = m_dependsOn.ClickOrHoverEvent;

        if (e0.jobId != 0)
        {
            m_dependsOn.ClearClickOrHoverEvent();
            return e0;
        }

        ClickLinkEvent e1 = m_dependantOn.ClickOrHoverEvent;

        if (e1.jobId != 0)
        {
            m_dependantOn.ClearClickOrHoverEvent();
            return e1;
        }

        return ClickLinkEvent.CreateEmpty();
    }

    internal void SetDependencyInfoVisible(bool visible)
    {
        m_infoPanelData.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        // The nav buttons live above the (now hidden) panel data, so hide them too
        // to avoid navigating while the content is not shown.
        m_backButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        m_forwardButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (visible)
            UpdateNavButtonStates();
    }

    void OnBackClicked()
    {
        if (m_historyIndex <= 0)
            return;

        // Capture the current tab before leaving
        var cur = m_history[m_historyIndex];
        m_history[m_historyIndex] = new HistoryEntry { frameIndex = cur.frameIndex, eventIndex = cur.eventIndex, tabIndex = m_tabView.selectedTabIndex };

        m_historyIndex--;
        HistoryEntry entry = m_history[m_historyIndex];
        m_historyNavEvent = new ClickLinkEvent { jobId = 1, frameIndex = entry.frameIndex, eventIndex = entry.eventIndex, hover = false };
        m_pendingHistoryTab = entry.tabIndex;
        m_hasPendingHistoryNav = true;
        UpdateNavButtonStates();
    }

    void OnForwardClicked()
    {
        if (m_historyIndex >= m_history.Count - 1)
            return;

        // Capture the current tab before leaving
        var cur = m_history[m_historyIndex];
        m_history[m_historyIndex] = new HistoryEntry { frameIndex = cur.frameIndex, eventIndex = cur.eventIndex, tabIndex = m_tabView.selectedTabIndex };

        m_historyIndex++;
        HistoryEntry entry = m_history[m_historyIndex];
        m_historyNavEvent = new ClickLinkEvent { jobId = 1, frameIndex = entry.frameIndex, eventIndex = entry.eventIndex, hover = false };
        m_pendingHistoryTab = entry.tabIndex;
        m_hasPendingHistoryNav = true;
        UpdateNavButtonStates();
    }

    void UpdateNavButtonStates()
    {
        m_backButton.SetEnabled(m_historyIndex > 0);
        m_forwardButton.SetEnabled(m_historyIndex < m_history.Count - 1);
    }

    internal void PushHistory(int frameIndex, int eventIndex)
    {
        // Deduplicate consecutive identical entries
        if (m_historyIndex >= 0)
        {
            HistoryEntry current = m_history[m_historyIndex];
            if (current.frameIndex == frameIndex && current.eventIndex == eventIndex)
                return;

            // Capture the active tab for the entry we're leaving
            m_history[m_historyIndex] = new HistoryEntry { frameIndex = current.frameIndex, eventIndex = current.eventIndex, tabIndex = m_tabView.selectedTabIndex };
        }

        // Discard forward history
        int removeStart = m_historyIndex + 1;
        if (removeStart < m_history.Count)
            m_history.RemoveRange(removeStart, m_history.Count - removeStart);

        // Cap at max size by dropping the oldest entry
        if (m_history.Count >= kMaxHistory)
            m_history.RemoveAt(0);
        else
            m_historyIndex++;

        m_history.Add(new HistoryEntry { frameIndex = frameIndex, eventIndex = eventIndex, tabIndex = m_tabView.selectedTabIndex });
        UpdateNavButtonStates();
    }

    internal ClickLinkEvent GetHistoryNavigation()
    {
        if (!m_hasPendingHistoryNav)
            return ClickLinkEvent.CreateEmpty();
        m_hasPendingHistoryNav = false;
        return m_historyNavEvent;
    }

    internal void RestoreHistoryTab()
    {
        if (m_pendingHistoryTab >= 0)
        {
            m_tabView.selectedTabIndex = m_pendingHistoryTab;
            m_pendingHistoryTab = -1;
        }
    }

    internal void ClearHistory()
    {
        m_history.Clear();
        m_historyIndex = -1;
        m_hasPendingHistoryNav = false;
        m_pendingHistoryTab = -1;
        UpdateNavButtonStates();
    }

    internal void Activate()
    {
        m_scheduledBy.SetEnabled(true);
        m_completedBy.SetEnabled(true);
        m_jobTime.SetEnabled(true);
        m_jobName.SetEnabled(true);
        m_infoPanelData.visible = true;
    }

    internal void Deactivate()
    {
        m_jobName.text = "No event selected.";
        m_jobTime.text = "";
        m_scheduledBy.text = "N/A";
        m_completedBy.text = "N/A";
        m_infoPanelData.visible = false;
        m_jobTime.SetEnabled(false);
        m_jobName.SetEnabled(false);
        ClearLists();
    }

    internal void ClearLists()
    {
        m_scheduledBy.SetEnabled(false);
        m_completedBy.SetEnabled(false);
        m_dependsOn.Clear();
        m_dependantOn.Clear();
        m_tabDependsOn.label    = "Depends on";
        m_tabDependencies.label = "Dependencies";
        m_dependsOnCount.text   = "\u2192 0 (depends on)";
        m_dependingCount.text   = "\u2192 0 (depending)";
    }

    internal void ClearData()
    {
        m_jobName.text = string.Empty;
        m_scheduledBy.text = string.Empty;
        m_completedBy.text = string.Empty;
    }

    internal Label JobName { get { return m_jobName; } }
    internal Label JobTime { get { return m_jobTime; } }

    internal Label CompletedBy { get { return m_completedBy; } }
    internal Label ScheduledBy { get { return m_scheduledBy; } }

    internal JobsList DependsOn { get { return m_dependsOn; } }
    internal JobsList DependantOn { get { return m_dependantOn; } }
}
