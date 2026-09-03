// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Analytics;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    enum AnalyticsEventFate
    {
        Recorded = 0,
        AnalyticsDisabled = 1,
        RateLimited = 2,

        Unknown = 3,
    }

    struct AnalyticsEventRecord
    {
        public string eventName { get; }
        public string payloadJson { get; }
        public AnalyticsEventFate fate { get; }
        public long timestampTicks { get; }

        public AnalyticsEventRecord(string eventName, string payloadJson, AnalyticsEventFate fate, long timestampTicks)
        {
            this.eventName = eventName;
            this.payloadJson = payloadJson;
            this.fate = fate;
            this.timestampTicks = timestampTicks;
        }
    }

    public partial class DebuggerEventListHandler
    {
        public List<string> items = new List<string>();
        // items is written to from a worker thread but read from a main thread, so lock for all accesses for thread safety
        private readonly object itemsLock = new object();
        [AutoStaticsCleanupOnCodeReload] // singleton holds a List<string> buffer and lock; must recreate on reload
        internal static DebuggerEventListHandler handler = new DebuggerEventListHandler();
        [AutoStaticsCleanupOnCodeReload] // holds user-registered analytic-sent handlers (internal window)
        static public event Action<Analytic> analyticSent; // Used only by the internal window

        public Analytic[] csharp_items = null;

        public static void AddCSharpAnalytic(Analytic analytic)
        {
            analyticSent?.Invoke(analytic);
        }

        internal static int DrainRecords(List<AnalyticsEventRecord> into, out int droppedSinceLastDrain)
        {
            if (into == null)
                throw new ArgumentNullException(nameof(into));

            droppedSinceLastDrain = 0;

            string raw = EditorAnalytics.DrainDebuggerRecords();
            if (string.IsNullOrEmpty(raw))
                return 0;

            int added = 0;
            string[] rows = raw.Split('\n');
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Length == 0)
                    continue;

                string[] parts = rows[i].Split('\t');
                if (parts.Length < 5)
                    continue;

                if (int.TryParse(parts[2], out int dropped) && dropped > 0)
                    droppedSinceLastDrain += dropped;

                if (parts[3].Length == 0 && parts[4].Length == 0)
                    continue;

                int.TryParse(parts[0], out int fate);
                long.TryParse(parts[1], out long timestampMs);

                into.Add(new AnalyticsEventRecord(parts[3], parts[4], (AnalyticsEventFate)fate,
                    DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime.Ticks));
                added++;
            }

            return added;
        }

        internal static void ClearRecords()
        {
            var discard = new List<AnalyticsEventRecord>();
            DrainRecords(discard, out _);
        }

        [Obsolete("The dispatch-time debugger feed was removed in 6000.7 and this no longer returns events.")]
        public static void AddAnalytic(String analytic)
        {
            handler.AddAnalyticInternal(analytic);
        }

        const int k_MaxBufferedPayloads = 1024;

        private void AddAnalyticInternal(String analytic)
        {
            lock (itemsLock)
            {
                if (items.Count >= k_MaxBufferedPayloads)
                    items.RemoveAt(0);
                items.Add(analytic);
            }
        }

        private void ClearList()
        {
            lock (itemsLock)
            {
                items.Clear();
            }
        }

        [Obsolete("The dispatch-time debugger feed was removed in 6000.7 and this no longer returns events.")]
        public static void ClearEventList()
        {
            handler.ClearList();
        }

        [Obsolete("The dispatch-time debugger feed was removed in 6000.7 and this no longer returns events.")]
        public static List<string> fetchEventList()
        {
            List<string> eventList = null;
            lock (handler.itemsLock)
            {
                eventList = handler.items;
                handler.items = new List<string>();
            }
            return eventList;
        }

        [Obsolete("The dispatch-time debugger feed was removed in 6000.7 and this no longer returns events.")]
        public static void fetchEventList(Action<string> processEventListItems)
        {
            List<string> pending;
            lock (handler.itemsLock)
            {
                pending = handler.items;
                handler.items = new List<string>();
            }

            for (int i = 0; i < pending.Count; i++)
                processEventListItems(pending[i]);
        }
    }
}
