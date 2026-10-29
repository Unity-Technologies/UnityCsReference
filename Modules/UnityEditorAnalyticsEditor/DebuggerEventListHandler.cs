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
        public long sequence { get; }
        public string eventName { get; }
        public string payloadJson { get; }
        public string sessionHeaderJson { get; }
        public AnalyticsEventFate fate { get; }
        public long timestampTicks { get; }

        public AnalyticsEventRecord(long sequence, string eventName, string payloadJson, string sessionHeaderJson,
            AnalyticsEventFate fate, long timestampTicks)
        {
            this.sequence = sequence;
            this.eventName = eventName;
            this.payloadJson = payloadJson;
            this.sessionHeaderJson = sessionHeaderJson;
            this.fate = fate;
            this.timestampTicks = timestampTicks;
        }
    }

    class AnalyticsRecordReader
    {
        public long sequence;
        public long headerId;

        public readonly Dictionary<long, string> sessionHeaders = new Dictionary<long, string>();

        public void Reset(long resumeFromSequence)
        {
            sequence = resumeFromSequence;
            headerId = 0;
            sessionHeaders.Clear();
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

        const string k_SessionHeaderRow = "h";

        static readonly char[] k_FieldSeparator = { '\t' };

        internal static int ReadRecords(List<AnalyticsEventRecord> into, AnalyticsRecordReader reader,
            out int discardedBeforeRead)
        {
            if (into == null)
                throw new ArgumentNullException(nameof(into));
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            discardedBeforeRead = 0;

            string raw = EditorAnalytics.ReadDebuggerRecordsSince(reader.sequence, reader.headerId);
            if (string.IsNullOrEmpty(raw))
                return 0;

            int added = 0;
            string[] rows = raw.Split('\n');
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].Length == 0)
                    continue;

                string[] parts = rows[i].Split(k_FieldSeparator, 6);

                if (parts[0] == k_SessionHeaderRow)
                {
                    if (parts.Length < 3 || !long.TryParse(parts[1], out long headerId))
                        continue;

                    reader.sessionHeaders[headerId] = parts[2];
                    if (headerId > reader.headerId)
                        reader.headerId = headerId;
                    continue;
                }

                if (parts.Length < 6)
                    continue;

                if (!long.TryParse(parts[0], out long sequence))
                    continue;

                if (sequence > reader.sequence + 1)
                    discardedBeforeRead += (int)(sequence - reader.sequence - 1);

                reader.sequence = sequence;

                int.TryParse(parts[1], out int fate);
                long.TryParse(parts[2], out long timestampMs);
                long.TryParse(parts[3], out long recordHeaderId);

                reader.sessionHeaders.TryGetValue(recordHeaderId, out string sessionHeader);

                into.Add(new AnalyticsEventRecord(sequence, parts[4], parts[5], sessionHeader,
                    (AnalyticsEventFate)fate, DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime.Ticks));
                added++;
            }

            return added;
        }

        internal static void ClearRecords()
        {
            EditorAnalytics.ClearDebuggerRecords();
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
