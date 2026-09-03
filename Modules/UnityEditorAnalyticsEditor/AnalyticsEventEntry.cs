// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.EditorAnalyticsDebugger
{
    class AnalyticsEventEntry
    {
        string m_Summary;

        public string eventName { get; }
        public string payloadJson { get; }
        public AnalyticsEventFate fate { get; }
        public DateTime timestampUtc { get; }

        public AnalyticsEventEntry(string eventName, string payloadJson, AnalyticsEventFate fate, DateTime timestampUtc)
        {
            this.eventName = eventName;
            this.payloadJson = payloadJson;
            this.fate = fate;
            this.timestampUtc = timestampUtc;
        }

        public string summary
        {
            get
            {
                if (m_Summary == null)
                    m_Summary = BuildSummary();
                return m_Summary;
            }
        }

        string BuildSummary()
        {
            string localTime = timestampUtc.ToLocalTime().ToString("HH:mm:ss.fff");
            string name = string.IsNullOrEmpty(eventName) ? "<unnamed>" : eventName;
            return localTime + "  " + name;
        }

        public bool Matches(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return true;

            if (eventName != null && eventName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return payloadJson != null && payloadJson.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
