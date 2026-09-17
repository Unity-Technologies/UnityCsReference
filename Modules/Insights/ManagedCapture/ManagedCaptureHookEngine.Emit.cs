// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Unity.EngineDiagnostics;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.ManagedCapture.Internal
{
    internal static partial class ManagedCaptureHookEngine
    {
        // Envelope fields: shared by every event, written flat regardless of which event object is
        // open. Anything else lands nested under the event's own key.
        [NoAutoStaticsCleanup] static readonly HashSet<string> s_EnvelopeFieldNames = new HashSet<string>(new[]
        {
            "mediation_class", "mediator", "source_method", "ad_impression_id",
            "ad_unit_id", "ad_format", "ad_network", "placement",
            "creative_id", "instance_name", "auction_id",
        });

        const int k_EventBufferCapacity = 512;
        const int k_MaxPooledRecords = 64;
        const int k_MaxQueuedRecords = 256;
        const int k_MaxPooledFields = 32;
        const int k_MaxRetainedBufferChars = 8 * 1024;

        [NoAutoStaticsCleanup] static readonly ConcurrentQueue<EventRecord> s_Queue = new ConcurrentQueue<EventRecord>();
        [NoAutoStaticsCleanup] static readonly ConcurrentQueue<EventRecord> s_RecordPool = new ConcurrentQueue<EventRecord>();
        [NoAutoStaticsCleanup] static readonly WaitCallback s_DrainCallback = DrainQueue;
        [NoAutoStaticsCleanup] static int s_DrainScheduled;
        [NoAutoStaticsCleanup] static int s_PooledCount;
        [NoAutoStaticsCleanup] static int s_QueuedCount;
        [NoAutoStaticsCleanup] static int s_DroppedCount;

        [ThreadStatic, NoAutoStaticsCleanup] static StringBuilder s_Json;
        [ThreadStatic, NoAutoStaticsCleanup] static char[] s_Chars;

        internal static int DroppedEventCount => Volatile.Read(ref s_DroppedCount);

        // Test-only: drives Enqueue/DrainQueue deterministically instead of racing the ThreadPool.
        internal static int QueuedCountForTests => Volatile.Read(ref s_QueuedCount);
        internal static void DrainQueueForTests() => DrainQueue(null);

        static StringBuilder JsonBuffer() => RentBuffer(ref s_Json, k_EventBufferCapacity);

        // Shared by JsonBuffer and BreadcrumbBuffer (Breadcrumbs.cs).
        static StringBuilder RentBuffer(ref StringBuilder field, int initialCapacity)
        {
            var builder = field;
            if (builder == null || builder.Capacity > k_MaxRetainedBufferChars)
                return field = new StringBuilder(initialCapacity);
            return builder;
        }

        static EventRecord RentRecord()
        {
            if (s_RecordPool.TryDequeue(out var record))
            {
                Interlocked.Decrement(ref s_PooledCount);
                return record;
            }
            return new EventRecord();
        }

        static void ReturnRecord(EventRecord record)
        {
            record.Fields.Clear();
            record.EventName = null;
            record.Mediator = null;
            record.SourceMethod = null;
            record.CorrelationField = null;
            record.CorrelationId = null;
            record.PreSerializedBody = null;

            // Clear() keeps the backing array.
            if (record.Fields.Capacity > k_MaxPooledFields)
                record.Fields.Capacity = k_MaxPooledFields;

            // Check-then-increment, not a CAS loop: concurrent returns can overshoot the cap by
            // roughly the number of racing threads, which is an acceptable bound on a soft limit.
            if (Volatile.Read(ref s_PooledCount) >= k_MaxPooledRecords)
                return;

            Interlocked.Increment(ref s_PooledCount);
            s_RecordPool.Enqueue(record);
        }

        static void Enqueue(EventRecord record)
        {
            // Same soft-limit tradeoff as the pool cap in ReturnRecord above.
            if (Volatile.Read(ref s_QueuedCount) >= k_MaxQueuedRecords)
            {
                if (Interlocked.Increment(ref s_DroppedCount) == 1)
                    ReportDroppedEvents();
                ReturnRecord(record);
                return;
            }

            Interlocked.Increment(ref s_QueuedCount);
            s_Queue.Enqueue(record);

            if (Interlocked.CompareExchange(ref s_DrainScheduled, 1, 0) != 0)
                return;

            try
            {
                ThreadPool.UnsafeQueueUserWorkItem(s_DrainCallback, null);
            }
            catch (Exception exception)
            {
                // Leaving the flag set would stop every later event from ever being scheduled.
                Volatile.Write(ref s_DrainScheduled, 0);
                ReportHookFailure(exception);
            }
        }

        static void DrainQueue(object state)
        {
            // An unhandled exception on a pool thread terminates the process under CoreCLR.
            try
            {
                do
                {
                    while (s_Queue.TryDequeue(out var record))
                    {
                        Interlocked.Decrement(ref s_QueuedCount);
                        try
                        {
                            EmitRecord(record, null);
                        }
                        catch (Exception exception)
                        {
                            ReportHookFailure(exception);
                        }
                        finally
                        {
                            ReturnRecord(record);
                        }
                    }
                    // Backlog cleared: reset so a later overflow warns again instead of only once ever.
                    Volatile.Write(ref s_DroppedCount, 0);
                    Volatile.Write(ref s_DrainScheduled, 0);
                }
                while (!s_Queue.IsEmpty && Interlocked.CompareExchange(ref s_DrainScheduled, 1, 0) == 0);
            }
            catch (Exception exception)
            {
                Volatile.Write(ref s_DrainScheduled, 0);
                ReportHookFailure(exception);
            }
        }

        static void EmitRecord(EventRecord record, Action<int, string, bool> overrideAction)
        {
            var builder = JsonBuffer();
            Serialize(builder, record);
            Emit(record.EventType, builder, overrideAction);
        }

        // Shared by EndEvent and EmitBreadcrumb: an override delivers synchronously, otherwise the
        // record goes through the bounded queue.
        static void DispatchRecord(EventRecord record, Action<int, string, bool> overrideAction)
        {
            if (overrideAction != null)
            {
                EmitRecord(record, overrideAction);
                ReturnRecord(record);
            }
            else
                Enqueue(record);
        }

        static void Serialize(StringBuilder builder, EventRecord record)
        {
            if (record.PreSerializedBody != null)
            {
                builder.Clear();
                builder.Append(record.PreSerializedBody);
                return;
            }

            builder.Clear();
            builder.Append('{');
            EventJsonWriter.AppendInteger(builder, "mediation_class", record.EventType);
            EventJsonWriter.AppendString(builder, "mediator", string.IsNullOrEmpty(record.Mediator) ? null : record.Mediator);
            EventJsonWriter.AppendString(builder, "source_method", record.SourceMethod);

            var fields = record.Fields;
            for (int i = 0; i < fields.Count; i++)
            {
                if (s_EnvelopeFieldNames.Contains(fields[i].Name))
                    AppendField(builder, fields[i]);
            }

            if (record.CorrelationField != null && record.CorrelationId != null)
                EventJsonWriter.AppendString(builder, record.CorrelationField, record.CorrelationId);

            // The populated event object is the discriminator: no separate event-name field.
            if (!string.IsNullOrEmpty(record.EventName))
            {
                EventJsonWriter.AppendKey(builder, record.EventName);
                builder.Append('{');
                for (int i = 0; i < fields.Count; i++)
                {
                    if (!s_EnvelopeFieldNames.Contains(fields[i].Name))
                        AppendField(builder, fields[i]);
                }
                builder.Append('}');
            }

            builder.Append('}');
        }

        static void AppendField(StringBuilder builder, FieldEntry field)
        {
            switch (field.Kind)
            {
                case FieldKind.String:
                    EventJsonWriter.AppendString(builder, field.Name, field.StringValue);
                    break;

                case FieldKind.Double:
                    EventJsonWriter.AppendKey(builder, field.Name);
                    builder.Append(field.DoubleValue.ToString("R", CultureInfo.InvariantCulture));
                    break;

                case FieldKind.Long:
                    EventJsonWriter.AppendInteger(builder, field.Name, field.LongValue);
                    break;

                case FieldKind.Bool:
                    EventJsonWriter.AppendKey(builder, field.Name);
                    builder.Append(field.BoolValue ? "true" : "false");
                    break;
            }
        }

        // Takes the builder so the production path hands LogEventDeferred its characters directly.
        // eventType is the same config-authored number written into the payload as "mediation_class" —
        // one number, used both as the wire tag and in the body, not two.
        static void Emit(int eventType, StringBuilder builder, Action<int, string, bool> overrideAction)
        {
            // Settable at runtime, so two reads could disagree.
            bool verbose = s_Verbose;
            var json = verbose || overrideAction != null ? builder.ToString() : null;

            if (verbose)
                Debug.Log($"[ManagedCapture] eventType={eventType} {json}");

            // A throw here would propagate into the SDK's own callback.
            try
            {
                if (overrideAction != null)
                    overrideAction(eventType, json, false);
                else
                    EngineDiagnostics.LogEventDeferred(eventType, Chars(builder), immediate: false);
            }
            catch (Exception exception)
            {
                ReportHookFailure(exception);
            }
        }

        static ReadOnlySpan<char> Chars(StringBuilder builder)
        {
            int length = builder.Length;
            var buffer = s_Chars;
            if (buffer == null || buffer.Length < length)
            {
                buffer = new char[Math.Max(length, k_EventBufferCapacity)];
                if (buffer.Length <= k_MaxRetainedBufferChars)
                    s_Chars = buffer;
            }
            builder.CopyTo(0, buffer, 0, length);
            return new ReadOnlySpan<char>(buffer, 0, length);
        }

        static void ReportDroppedEvents()
            => SafeLogWarning($"[ManagedCapture] event queue reached {k_MaxQueuedRecords}; dropping events until it drains");

        static void ReportHookFailure(Exception exception)
            => SafeLogWarning($"[ManagedCapture] hook suppressed an exception: {exception.Message}");

        // Also used by ManagedCaptureHooks.WrapAction.
        internal static void SafeLogWarning(string message)
        {
            try
            {
                Debug.LogWarning(message);
            }
            catch
            {
                // Nothing more we can safely do on the reporting path.
            }
        }
    }
}
