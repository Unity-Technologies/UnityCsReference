// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.EditorAnalyticsDebugger
{
    class DisplayRow
    {
        public string text;
        public AnalyticsEventEntry entry;

        public string groupKey;
    }

    class AnalyticsEventLog
    {
        public const int k_MaxRetainedEvents = 10000;

        readonly List<AnalyticsEventEntry> m_Events = new List<AnalyticsEventEntry>();
        List<TreeViewItemData<DisplayRow>> m_RootItems = new List<TreeViewItemData<DisplayRow>>();

        readonly Dictionary<string, int> m_GroupIds = new Dictionary<string, int>();
        int m_NextGroupId = -1;

        string m_Filter = "";
        bool m_GroupByEvent;
        int m_EvictedEventCount;
        int m_VisibleEventCount;
        long m_NewestSequence;

        public IList<TreeViewItemData<DisplayRow>> rootItems
        {
            get { return m_RootItems; }
        }

        public int retainedEventCount
        {
            get { return m_Events.Count; }
        }

        public int visibleEventCount
        {
            get { return m_VisibleEventCount; }
        }

        public int visibleGroupCount
        {
            get { return m_GroupByEvent ? m_RootItems.Count : 0; }
        }

        public int evictedEventCount
        {
            get { return m_EvictedEventCount; }
        }

        public long newestSequence
        {
            get { return m_NewestSequence; }
        }

        public bool groupByEvent
        {
            get { return m_GroupByEvent; }
            set
            {
                if (m_GroupByEvent == value)
                    return;
                m_GroupByEvent = value;
                Rebuild();
            }
        }

        public string filter
        {
            get { return m_Filter; }
            set
            {
                string newFilter = value ?? "";
                if (m_Filter == newFilter)
                    return;
                m_Filter = newFilter;
                Rebuild();
            }
        }

        public bool Add(List<AnalyticsEventEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return false;

            int visibleBefore = m_VisibleEventCount;
            int evictedBefore = m_EvictedEventCount;

            m_Events.AddRange(entries);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].sequence > m_NewestSequence)
                    m_NewestSequence = entries[i].sequence;
            }

            if (m_Events.Count > k_MaxRetainedEvents)
            {
                int excess = m_Events.Count - k_MaxRetainedEvents;
                m_Events.RemoveRange(0, excess);
                m_EvictedEventCount += excess;
            }

            Rebuild();

            return m_VisibleEventCount != visibleBefore
                || m_EvictedEventCount != evictedBefore
                || m_GroupByEvent;
        }

        public void NoteDiscarded(int count)
        {
            if (count > 0)
                m_EvictedEventCount += count;
        }

        public bool Contains(AnalyticsEventEntry entry)
        {
            return entry != null && m_Events.Contains(entry);
        }

        public void Clear()
        {
            m_Events.Clear();
            m_RootItems = new List<TreeViewItemData<DisplayRow>>();
            m_GroupIds.Clear();
            m_NextGroupId = -1;
            m_EvictedEventCount = 0;
            m_VisibleEventCount = 0;
            m_NewestSequence = 0;
        }

        public void GetVisibleEvents(List<AnalyticsEventEntry> results)
        {
            if (results == null)
                return;

            for (int i = 0; i < m_Events.Count; i++)
            {
                if (m_Events[i].Matches(m_Filter))
                    results.Add(m_Events[i]);
            }
        }

        public int FindRowId(long sequence)
        {
            for (int i = 0; i < m_RootItems.Count; i++)
            {
                TreeViewItemData<DisplayRow> item = m_RootItems[i];
                if (item.data.groupKey == null)
                {
                    if (item.data.entry != null && item.data.entry.sequence == sequence)
                        return item.id;
                    continue;
                }

                foreach (TreeViewItemData<DisplayRow> child in item.children)
                {
                    if (child.data.entry != null && child.data.entry.sequence == sequence)
                        return child.id;
                }
            }

            return -1;
        }

        int GroupId(string key)
        {
            if (!m_GroupIds.TryGetValue(key, out int id))
            {
                id = m_NextGroupId--;
                m_GroupIds[key] = id;
            }
            return id;
        }

        void Rebuild()
        {
            m_RootItems = new List<TreeViewItemData<DisplayRow>>();
            m_VisibleEventCount = 0;

            if (m_GroupByEvent)
                BuildGrouped();
            else
                BuildFlat();
        }

        void BuildFlat()
        {
            for (int i = 0; i < m_Events.Count; i++)
            {
                AnalyticsEventEntry entry = m_Events[i];
                if (!entry.Matches(m_Filter))
                    continue;

                m_RootItems.Add(new TreeViewItemData<DisplayRow>(m_VisibleEventCount, Row(entry)));
                m_VisibleEventCount++;
            }
        }

        void BuildGrouped()
        {
            var members = new Dictionary<string, List<AnalyticsEventEntry>>();
            var order = new List<string>();

            for (int i = 0; i < m_Events.Count; i++)
            {
                AnalyticsEventEntry entry = m_Events[i];
                if (!entry.Matches(m_Filter))
                    continue;

                string key = string.IsNullOrEmpty(entry.eventName) ? "<unnamed>" : entry.eventName;
                if (!members.TryGetValue(key, out List<AnalyticsEventEntry> list))
                {
                    list = new List<AnalyticsEventEntry>();
                    members[key] = list;
                    order.Add(key);
                }
                list.Add(entry);
            }

            for (int i = 0; i < order.Count; i++)
            {
                string key = order[i];
                List<AnalyticsEventEntry> list = members[key];
                AnalyticsEventEntry mostRecent = list[list.Count - 1];

                var children = new List<TreeViewItemData<DisplayRow>>(list.Count);
                for (int j = 0; j < list.Count; j++)
                {
                    children.Add(new TreeViewItemData<DisplayRow>(m_VisibleEventCount, Row(list[j])));
                    m_VisibleEventCount++;
                }

                m_RootItems.Add(new TreeViewItemData<DisplayRow>(GroupId(key), new DisplayRow
                {
                    text = $"{list.Count,5}x  {key}",
                    entry = mostRecent,
                    groupKey = key,
                }, children));
            }
        }

        static DisplayRow Row(AnalyticsEventEntry entry)
        {
            return new DisplayRow
            {
                text = entry.summary,
                entry = entry,
            };
        }
    }
}
