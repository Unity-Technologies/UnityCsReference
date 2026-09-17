// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.EngineDiagnostics;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.ManagedCapture.Internal
{
    internal static partial class ManagedCaptureHookEngine
    {
        [NoAutoStaticsCleanup] internal static Action<int, string, bool> LogEventOverride { get; set; }

        // Off by default: payloads carry ad revenue and identifiers.
        [NoAutoStaticsCleanup] static bool s_Verbose;

        [ThreadStatic, NoAutoStaticsCleanup] static EventRecord s_Record;

        // 0 = no event open, 1 = one open, >1 = nested opens that were counted but not started.
        [ThreadStatic, NoAutoStaticsCleanup] static int s_OpenDepth;

        public static bool VerboseLogging
        {
            get { return s_Verbose; }
            set { s_Verbose = value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsGateOpen() => LogEventOverride != null || EngineDiagnostics.IsInitialized || s_Verbose;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsSuppressed(int eventType)
            => !IsGateOpen() || EngineDiagnostics.IsEventTypeBlocked(eventType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsEventOpen() => s_OpenDepth == 1;

        enum FieldKind : byte
        {
            String,
            Double,
            Long,
            Bool,
        }

        // Fields rather than properties: the drain thread walks these per event.
        struct FieldEntry
        {
            public string Name;
            public FieldKind Kind;
            public string StringValue;
            public double DoubleValue;
            public long LongValue;
            public bool BoolValue;
        }

        sealed class EventRecord
        {
            public int EventType;
            public string EventName;
            public string Mediator;
            public string SourceMethod;
            public string CorrelationField;
            public string CorrelationId;
            public readonly List<FieldEntry> Fields = new List<FieldEntry>();

            // Set for breadcrumbs; other events serialize from Fields instead.
            public string PreSerializedBody;
        }

        // Caller must pair BeginEvent with one EndEvent (in a finally); past this many unclosed
        // nested opens, treat depth as stale rather than counting forever.
        internal const int k_MaxNestedDepth = 32;

        // A suppressed class takes no part in correlation: it neither mints, reuses nor releases an id.
        // eventType is the config-authored number for this (mediator, l1Event) pair — the same value
        // crosses into native as the wire type (unity.managed_event.type) and appears in the payload
        // as "mediation_class". Producer-agnostic: any IL-injected SDK's config supplies its own number.
        public static void BeginEvent(int eventType, string l1Event, string mediator, string sourceMethod)
        {
            // A nested open is only counted, so its Field and EndEvent calls cannot add to or close the
            // event already in progress.
            if (s_OpenDepth > 0)
            {
                if (s_OpenDepth < k_MaxNestedDepth)
                {
                    s_OpenDepth++;
                    return;
                }

                ReturnRecord(s_Record);
                s_Record = null;
                EventCorrelation.Clear();
                s_OpenDepth = 0;
            }
            if (IsSuppressed(eventType))
                return;

            s_OpenDepth = 1;
            EventCorrelation.Clear();
            var record = RentRecord();
            record.EventType = eventType;
            record.EventName = l1Event;
            record.Mediator = mediator;
            record.SourceMethod = sourceMethod;
            s_Record = record;
        }

        public static void Field(string name, string value)
        {
            if (!IsEventOpen())
                return;
            if (value == null)
                return;
            s_Record.Fields.Add(new FieldEntry { Name = name, Kind = FieldKind.String, StringValue = value });
        }

        public static void Field(string name, double value)
        {
            if (!IsEventOpen())
                return;

            // NaN and infinity have no JSON representation.
            if (double.IsNaN(value) || double.IsInfinity(value))
                return;
            s_Record.Fields.Add(new FieldEntry { Name = name, Kind = FieldKind.Double, DoubleValue = value });
        }

        public static void Field(string name, long value)
        {
            if (!IsEventOpen())
                return;
            s_Record.Fields.Add(new FieldEntry { Name = name, Kind = FieldKind.Long, LongValue = value });
        }

        public static void Field(string name, int value) => Field(name, (long)value);

        public static void Field(string name, bool value)
        {
            if (!IsEventOpen())
                return;
            s_Record.Fields.Add(new FieldEntry { Name = name, Kind = FieldKind.Bool, BoolValue = value });
        }

        public static void EndEvent()
        {
            if (s_OpenDepth == 0)
                return;

            // Closes a nested open that was never started.
            if (s_OpenDepth > 1)
            {
                s_OpenDepth--;
                return;
            }
            s_OpenDepth = 0;

            var record = s_Record;
            s_Record = null;

            if (EventCorrelation.TryResolve(out var correlationField, out var correlationId))
            {
                record.CorrelationField = correlationField;
                record.CorrelationId = correlationId;
            }

            // Don't pin the SDK instance in a thread-static past the event.
            EventCorrelation.Clear();

            DispatchRecord(record, LogEventOverride);
        }

        public static void SetCorrelation(object key, string field, int role)
        {
            if (!IsEventOpen())
                return;
            EventCorrelation.Set(key, field, role);
        }

        // Resolves raw through a "k1=v1;k2=v2" spec, returning it unchanged when no key matches.
        public static string MapValue(string raw, string mapSpec)
        {
            if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(mapSpec))
                return raw;

            int i = 0;
            int specLength = mapSpec.Length;
            while (i < specLength)
            {
                int equalsIndex = mapSpec.IndexOf('=', i);
                if (equalsIndex < 0)
                    break;

                int separatorIndex = mapSpec.IndexOf(';', equalsIndex + 1);
                int valueEnd = separatorIndex < 0 ? specLength : separatorIndex;
                int keyLength = equalsIndex - i;
                if (keyLength == raw.Length && string.CompareOrdinal(mapSpec, i, raw, 0, keyLength) == 0)
                    return mapSpec.Substring(equalsIndex + 1, valueEnd - (equalsIndex + 1));

                i = separatorIndex < 0 ? specLength : separatorIndex + 1;
            }
            return raw;
        }
    }
}
