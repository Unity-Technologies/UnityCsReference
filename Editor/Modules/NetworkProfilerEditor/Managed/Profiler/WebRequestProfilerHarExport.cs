// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Networking
{
    internal struct WebRequestProfilerHarOptions
    {
        public bool includeSensitiveData;
        public DateTime anchorUtc;
        public ulong anchorFrameStartNs;
        public string creatorVersion;
    }

    // HTTP Archive 1.2
    internal static class WebRequestProfilerHarExport
    {
        internal const string k_FileExtension = "har";

        const string k_CreatorName = "Unity Web Requests Profiler";
        const string k_DateFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        const int k_MainThreadIndex = 0;
        const long k_NanosecondsPerTick = 100;
        const long k_TicksPerMicrosecond = 10;
        const string k_PageId = "page_1";
        const string k_EncodedBodyNotCapturedComment = "Body not captured; the decoded size is unknown because the response is content-encoded.";
        const string k_HeadersTruncatedComment = "Header capture hit its per-request limit; headers beyond it were not recorded.";

        internal static string DefaultFileName(DateTime localNow)
        {
            return "WebRequests-" + localNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "." + k_FileExtension;
        }

        internal static void ExportToFile(string path, IReadOnlyList<WebRequestProfilerRecord> records,
            bool includeSensitiveData)
        {
            var options = new WebRequestProfilerHarOptions
            {
                includeSensitiveData = includeSensitiveData,
                anchorUtc = DateTime.UtcNow,
                anchorFrameStartNs = ReadNewestFrameEndNs(records),
                creatorVersion = Application.unityVersion,
            };

            File.WriteAllText(path, ToJson(records, options), new UTF8Encoding(false));
        }

        static ulong ReadNewestFrameEndNs(IReadOnlyList<WebRequestProfilerRecord> records)
        {
            var lastFrame = ProfilerDriver.lastFrameIndex;
            if (lastFrame >= 0)
            {
                using (var frameData = ProfilerDriver.GetRawFrameDataView(lastFrame, k_MainThreadIndex))
                {
                    if (frameData != null && frameData.valid)
                        return frameData.frameStartTimeNs + frameData.frameTimeNs;
                }
            }

            ulong newest = 0;
            foreach (var record in records)
                newest = Math.Max(newest, record.lastSeenFrameStartNs);
            return newest;
        }

        internal static string ToJson(IReadOnlyList<WebRequestProfilerRecord> records,
            WebRequestProfilerHarOptions options)
        {
            var ordered = new List<WebRequestProfilerRecord>(records);
            ordered.Sort((a, b) => a.captureIndex.CompareTo(b.captureIndex));

            using (var stream = new MemoryStream())
            {
                var writerOptions = new JsonWriterOptions
                {
                    Indented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                using (var writer = new Utf8JsonWriter(stream, writerOptions))
                {
                    writer.WriteStartObject();
                    writer.WriteStartObject("log");
                    writer.WriteString("version", "1.2");

                    writer.WriteStartObject("creator");
                    writer.WriteString("name", k_CreatorName);
                    writer.WriteString("version", options.creatorVersion ?? string.Empty);
                    writer.WriteEndObject();

                    var comment = BuildLogComment(ordered, options.includeSensitiveData);
                    if (comment.Length > 0)
                        writer.WriteString("comment", comment);

                    // Optional in HAR 1.2, but Firefox refuses a file without it.
                    writer.WriteStartArray("pages");
                    writer.WriteStartObject();
                    writer.WriteString("startedDateTime", ordered.Count > 0
                        ? FormatStartedDateTime(EarliestStart(ordered, options), options)
                        : FormatDateTime(ToUtc(options.anchorUtc)));
                    writer.WriteString("id", k_PageId);
                    writer.WriteString("title", k_CreatorName);
                    writer.WriteStartObject("pageTimings");
                    writer.WriteNumber("onContentLoad", -1);
                    writer.WriteNumber("onLoad", -1);
                    writer.WriteEndObject();
                    writer.WriteEndObject();
                    writer.WriteEndArray();

                    writer.WriteStartArray("entries");
                    foreach (var record in ordered)
                        WriteEntry(writer, record, options);
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        static string BuildLogComment(List<WebRequestProfilerRecord> records, bool includeSensitiveData)
        {
            var comment = string.Empty;
            if (records.Exists(record => record.startUnixUsec == 0))
                comment = "This capture has no wall-clock start times, so startedDateTime is anchored at export time: "
                    + "spacing between requests is as captured, the absolute time is approximate.";
            if (!includeSensitiveData)
                comment += (comment.Length > 0 ? " " : string.Empty) + "Credential headers were removed.";
            return comment;
        }

        static WebRequestProfilerRecord EarliestStart(List<WebRequestProfilerRecord> records,
            WebRequestProfilerHarOptions options)
        {
            var earliest = records[0];
            foreach (var record in records)
            {
                if (StartedTicks(record, options) < StartedTicks(earliest, options))
                    earliest = record;
            }
            return earliest;
        }

        static DateTime ToUtc(DateTime time)
        {
            return time.Kind == DateTimeKind.Local ? time.ToUniversalTime() : time;
        }

        static string FormatDateTime(DateTime utc)
        {
            return utc.ToString(k_DateFormat, CultureInfo.InvariantCulture);
        }

        static void WriteEntry(Utf8JsonWriter writer, WebRequestProfilerRecord record,
            WebRequestProfilerHarOptions options)
        {
            var timeMs = record.durationNs / 1e6;

            writer.WriteStartObject();
            writer.WriteString("pageref", k_PageId);
            writer.WriteString("startedDateTime", FormatStartedDateTime(record, options));
            writer.WriteNumber("time", timeMs);
            WriteRequest(writer, record, options.includeSensitiveData);
            WriteResponse(writer, record, options.includeSensitiveData);
            writer.WriteStartObject("cache");
            writer.WriteEndObject();
            WriteTimings(writer, record, timeMs);

            writer.WriteStartObject("_unity");
            writer.WriteNumber("requestId", record.requestId);
            writer.WriteString("source", record.source.ToString());
            writer.WriteString("state", StateName(record.state));
            writer.WriteNumber("captureIndex", record.captureIndex);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        static string StateName(WebRequestProfilerState state)
        {
            switch (state)
            {
                case WebRequestProfilerState.Completed:
                    return "completed";
                case WebRequestProfilerState.Failed:
                    return "failed";
                default:
                    return "inFlight";
            }
        }

        static string FormatStartedDateTime(WebRequestProfilerRecord record, WebRequestProfilerHarOptions options)
        {
            return FormatDateTime(new DateTime(StartedTicks(record, options), DateTimeKind.Utc));
        }

        static long StartedTicks(WebRequestProfilerRecord record, WebRequestProfilerHarOptions options)
        {
            long startedTicks;
            if (record.startUnixUsec != 0)
            {
                startedTicks = DateTime.UnixEpoch.Ticks + (long)record.startUnixUsec * k_TicksPerMicrosecond;
            }
            else
            {
                startedTicks = ToUtc(options.anchorUtc).Ticks;
                if (record.lastSeenFrameStartNs != 0)
                {
                    var requestStartNs = record.lastSeenFrameStartNs > record.durationNs
                        ? record.lastSeenFrameStartNs - record.durationNs
                        : 0ul;
                    var agoNs = (long)options.anchorFrameStartNs - (long)requestStartNs;
                    startedTicks -= agoNs / k_NanosecondsPerTick;
                }
            }

            return Math.Max(0, Math.Min(startedTicks, DateTime.MaxValue.Ticks));
        }

        static void WriteRequest(Utf8JsonWriter writer, WebRequestProfilerRecord record, bool includeSensitiveData)
        {
            writer.WriteStartObject("request");
            writer.WriteString("method", record.method);
            writer.WriteString("url", record.url);
            writer.WriteString("httpVersion", string.Empty);
            writer.WriteStartArray("cookies");
            writer.WriteEndArray();

            var hasWire = record.headers != null
                && record.headers.Exists(header => header.kind == WebRequestProfilerHeaderKind.RequestWire);
            var headersTruncated = WriteHeaders(writer, record,
                hasWire ? WebRequestProfilerHeaderKind.RequestWire : WebRequestProfilerHeaderKind.Request,
                includeSensitiveData);

            writer.WriteStartArray("queryString");
            foreach (var parameter in WebRequestProfilerUrl.ParseQueryParameters(record.url))
            {
                writer.WriteStartObject();
                writer.WriteString("name", parameter.Key);
                writer.WriteString("value", parameter.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            var body = record.requestBody;
            if (body.captured && body.totalLength > 0)
            {
                writer.WriteStartObject("postData");
                writer.WriteString("mimeType", body.contentType ?? string.Empty);
                if (!body.IsBinary && body.text != null)
                    writer.WriteString("text", body.text);
                WriteBodyComment(writer, body);
                writer.WriteEndObject();
            }

            writer.WriteNumber("headersSize", -1);
            writer.WriteNumber("bodySize",
                WebRequestProfilerFormatting.HasMeasuredUpload(record) ? (long)record.bytesUploaded : -1L);
            if (headersTruncated)
                writer.WriteString("comment", k_HeadersTruncatedComment);
            writer.WriteEndObject();
        }

        static void WriteResponse(Utf8JsonWriter writer, WebRequestProfilerRecord record, bool includeSensitiveData)
        {
            var body = record.responseBody;
            var downloadMeasured = WebRequestProfilerFormatting.HasMeasuredDownload(record);

            writer.WriteStartObject("response");
            writer.WriteNumber("status", record.state == WebRequestProfilerState.InFlight ? 0 : record.statusCode);
            writer.WriteString("statusText", string.Empty);
            writer.WriteString("httpVersion", string.Empty);
            writer.WriteStartArray("cookies");
            writer.WriteEndArray();
            var headersTruncated = WriteHeaders(writer, record, WebRequestProfilerHeaderKind.Response, includeSensitiveData);

            var encoded = !body.captured && IsContentEncoded(record);

            writer.WriteStartObject("content");
            if (body.captured)
                writer.WriteNumber("size", body.totalLength);
            else
                writer.WriteNumber("size", downloadMeasured && !encoded ? record.bytesDownloaded : 0ul);

            var mimeType = !string.IsNullOrEmpty(record.contentType) ? record.contentType : body.contentType;
            writer.WriteString("mimeType", mimeType ?? string.Empty);

            if (body.captured)
            {
                if (!body.IsBinary && body.text != null)
                    writer.WriteString("text", body.text);
                WriteBodyComment(writer, body);
            }
            else
                writer.WriteString("comment", encoded ? k_EncodedBodyNotCapturedComment : "Body not captured");
            writer.WriteEndObject();

            writer.WriteString("redirectURL", RedirectUrl(record));
            writer.WriteNumber("headersSize", -1);
            writer.WriteNumber("bodySize", downloadMeasured ? (long)record.bytesDownloaded : -1L);
            if (headersTruncated)
                writer.WriteString("comment", k_HeadersTruncatedComment);
            writer.WriteEndObject();
        }

        static bool IsContentEncoded(WebRequestProfilerRecord record)
        {
            if (record.headers == null)
                return false;

            var encoding = record.headers.Find(header => header.kind == WebRequestProfilerHeaderKind.Response
                && string.Equals(header.name, "Content-Encoding", StringComparison.OrdinalIgnoreCase));
            return !string.IsNullOrEmpty(encoding.value)
                && !string.Equals(encoding.value.Trim(), "identity", StringComparison.OrdinalIgnoreCase);
        }

        static string RedirectUrl(WebRequestProfilerRecord record)
        {
            if (record.statusCode < 300 || record.statusCode >= 400 || record.headers == null)
                return string.Empty;

            var location = record.headers.Find(header => header.kind == WebRequestProfilerHeaderKind.Response
                && string.Equals(header.name, "Location", StringComparison.OrdinalIgnoreCase));
            return location.value ?? string.Empty;
        }

        static void WriteBodyComment(Utf8JsonWriter writer, WebRequestProfilerBody body)
        {
            var comments = new List<string>();
            if (body.IsBinary)
                comments.Add("Binary body, content not included");
            if (body.IsTruncated)
                comments.Add($"Truncated: {body.capturedLength} of {body.totalLength} bytes captured");
            if (comments.Count > 0)
                writer.WriteString("comment", string.Join("; ", comments));
        }

        static bool WriteHeaders(Utf8JsonWriter writer, WebRequestProfilerRecord record,
            WebRequestProfilerHeaderKind kind, bool includeSensitiveData)
        {
            var truncated = false;
            writer.WriteStartArray("headers");
            if (record.headers != null)
            {
                foreach (var header in record.headers)
                {
                    if (header.kind != kind)
                        continue;

                    truncated |= header.truncated;
                    if (header.isCredential && !includeSensitiveData)
                        continue;

                    writer.WriteStartObject();
                    writer.WriteString("name", header.name);
                    writer.WriteString("value", header.value);
                    writer.WriteEndObject();
                }
            }
            writer.WriteEndArray();
            return truncated;
        }

        static void WriteTimings(Utf8JsonWriter writer, WebRequestProfilerRecord record, double timeMs)
        {
            double blocked = -1, dns = -1, connect = -1, ssl = -1, send = 0, wait = timeMs, receive = 0;
            string comment = null;

            if (record.hasTimings)
            {
                var t = record.timings;
                var spans = WebRequestDetailsViewController.ComputePhaseSpans(
                    new[]
                    {
                        t.queuedUsec, t.nameLookupUsec, t.connectUsec, t.tlsHandshakeUsec,
                        t.preTransferUsec, t.startTransferUsec, t.totalUsec,
                    },
                    t.availableMarks);

                blocked = PhaseMs(spans[0], -1);
                dns = PhaseMs(spans[1], -1);
                ssl = PhaseMs(spans[3], -1);
                send = PhaseMs(spans[4], 0);
                wait = PhaseMs(spans[5], 0);
                receive = PhaseMs(spans[6], 0);

                // HAR's connect includes the TLS handshake.
                var initialConnect = PhaseMs(spans[2], -1);
                connect = initialConnect < 0 && ssl < 0 ? -1 : Math.Max(initialConnect, 0) + Math.Max(ssl, 0);

                // The marks cover only the final transfer; durationNs also spans queueing and earlier redirect legs.
                var unaccountedMs = timeMs - t.totalUsec / 1000.0;
                if (unaccountedMs > 0)
                {
                    blocked = Math.Max(blocked, 0) + unaccountedMs;
                    comment = string.Format(CultureInfo.InvariantCulture,
                        "blocked includes {0:0.###} ms before the final transfer started: queueing ahead of the transport and any earlier redirect legs.",
                        unaccountedMs);
                }
            }

            writer.WriteStartObject("timings");
            writer.WriteNumber("blocked", blocked);
            writer.WriteNumber("dns", dns);
            writer.WriteNumber("connect", connect);
            writer.WriteNumber("ssl", ssl);
            writer.WriteNumber("send", send);
            writer.WriteNumber("wait", wait);
            writer.WriteNumber("receive", receive);
            if (comment != null)
                writer.WriteString("comment", comment);
            writer.WriteEndObject();
        }

        static double PhaseMs(WebRequestDetailsViewController.PhaseSpan span, double unmeasured)
        {
            return span.unavailable || span.skipped ? unmeasured : span.elapsedUsec / 1000.0;
        }
    }
}
