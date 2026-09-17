// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Networking
{
    // One captured header, with its text already resolved out of its own frame's string blob.
    internal readonly struct WebRequestProfilerHeader
    {
        public readonly WebRequestProfilerHeaderKind kind;
        public readonly string name;
        public readonly string value;

        public WebRequestProfilerHeader(WebRequestProfilerHeaderKind kind, string name, string value)
        {
            this.kind = kind;
            this.name = name;
            this.value = value;
        }
    }

    // One captured body, its bytes already resolved out of its own frame's blob. `captured` separates
    // a request that carried no body from one whose body was not kept.
    internal readonly struct WebRequestProfilerBody
    {
        public readonly string text;
        public readonly ulong totalLength;
        // Bytes kept, which is what the cap counts - text.Length is UTF-16 code units.
        public readonly uint capturedLength;
        public readonly WebRequestProfilerBodyFlags flags;
        public readonly bool captured;

        public WebRequestProfilerBody(string text, ulong totalLength, uint capturedLength,
            WebRequestProfilerBodyFlags flags)
        {
            this.text = text;
            this.totalLength = totalLength;
            this.capturedLength = capturedLength;
            this.flags = flags;
            captured = true;
        }

        public bool IsTruncated => (flags & WebRequestProfilerBodyFlags.Truncated) != 0;
        public bool IsBinary => (flags & WebRequestProfilerBodyFlags.Binary) != 0;
    }

    // One request, merged from every frame it appeared in.
    //
    // A class, not a struct: the list view holds these by reference and each ingested frame mutates
    // them in place.
    internal sealed class WebRequestProfilerRecord
    {
        public ulong requestId;
        public string url = string.Empty;
        public string method = string.Empty;
        public string contentType = string.Empty;
        public ulong bytesDownloaded;
        public ulong bytesUploaded;
        public ulong durationNs;
        public ushort statusCode;
        public WebRequestProfilerState state;
        public WebRequestProfilerSource source;

        // Which of the two byte counts above are real. None of them is the normal case for a transport
        // that cannot count bytes at all, and it reads differently from a request that transferred
        // none: see WebRequestProfilerFormatting.FormatTransferSize.
        public WebRequestProfilerMeasurement measured;

        // Position in the capture, assigned once when the record is created and never revised. The
        // list's default order, and what the Order column sorts on - a display index would renumber
        // itself under every other sort, which is the one thing this column exists not to do.
        public int captureIndex;
        // Null until the frame the request finished on is read: headers are recorded at that point, so
        // an in-flight request genuinely has none rather than an empty set.
        public List<WebRequestProfilerHeader> headers;

        // Only a finished request from a transport that measures the phases has any, so absent is the
        // normal case and is distinct from every phase having measured zero.
        public bool hasTimings;
        public WebRequestProfilerTimingRow timings;

        // Whether this request's bodies could be captured at all, carried from its first frame. Tells
        // "not yet, still running" from "never, capture was off when it started".
        public WebRequestProfilerBodyCapture bodyCapture;

        // Default (captured false) unless capture was on. The tabs name the switch when it was off.
        public WebRequestProfilerBody requestBody;
        public WebRequestProfilerBody responseBody;

        // -1 until seen. completedFrame is what the view jumps to, and may name a frame that has since
        // been evicted from the ring buffer.
        public long lastSeenFrame = -1;
        public long completedFrame = -1;
    }

    // Accumulates every request across the frames held in the profiler's buffer.
    //
    // Native emits a request once per frame it is alive in and then drops it, so its final status and
    // content type exist only in the frame it completed in. Reading a single frame therefore shows an
    // arbitrary slice - mostly in-flight rows, and almost never a completion. This merges frames as
    // they are read so the view can show the whole capture.
    //
    // Strings cannot be deferred: a row's offsets index the string blob of its own frame, so they are
    // resolved here while that blob is still the one in hand.
    internal sealed class WebRequestProfilerCaptureLog
    {
        readonly Dictionary<ulong, WebRequestProfilerRecord> m_ById = new Dictionary<ulong, WebRequestProfilerRecord>();

        // Insertion ordered, so requests are listed in the order they started.
        readonly List<WebRequestProfilerRecord> m_Records = new List<WebRequestProfilerRecord>();

        long m_IngestedThrough = -1;

        public List<WebRequestProfilerRecord> Records => m_Records;

        public void Clear()
        {
            m_ById.Clear();
            m_Records.Clear();
            m_IngestedThrough = -1;
        }

        // Ingests every frame in range not already read. Returns true when a record was added or
        // changed, so the caller can skip refreshing an unchanged list.
        public bool Sync(long firstFrameIndex, long lastFrameIndex)
        {
            // Frames renumbered below what is ingested belong to another session, whose ids would
            // collide. A replacement capture at least as long is invisible here, so the owner also
            // has to Clear on ProfilerDriver.profileCleared.
            if (lastFrameIndex < m_IngestedThrough)
                Clear();

            if (lastFrameIndex < 0)
                return false;

            var from = m_IngestedThrough >= 0 ? m_IngestedThrough + 1 : firstFrameIndex;

            // Frames already evicted can never be read, so never wait for them.
            if (from < firstFrameIndex)
                from = firstFrameIndex;

            var changed = false;
            for (var frame = from; frame <= lastFrameIndex; ++frame)
                changed |= Ingest(frame);

            m_IngestedThrough = lastFrameIndex;
            return changed;
        }

        bool Ingest(long frameIndex)
        {
            var rows = WebRequestProfilerFrameReader.ReadFrame(frameIndex, out var strings,
                out var headers, out var timings, out var bodies, out var bodyBytes);
            return Merge(frameIndex, rows, strings, headers, timings, bodies, bodyBytes);
        }

        internal bool Merge(long frameIndex, List<WebRequestProfilerRow> rows, byte[] strings)
        {
            return Merge(frameIndex, rows, strings, null, null, null, null);
        }

        internal bool Merge(long frameIndex, List<WebRequestProfilerRow> rows, byte[] strings,
            List<WebRequestProfilerHeaderRow> headers, List<WebRequestProfilerTimingRow> timings)
        {
            return Merge(frameIndex, rows, strings, headers, timings, null, null);
        }

        // Split from Ingest so the merge rules can be tested without a live capture to read frames from.
        internal bool Merge(long frameIndex, List<WebRequestProfilerRow> rows, byte[] strings,
            List<WebRequestProfilerHeaderRow> headers, List<WebRequestProfilerTimingRow> timings,
            List<WebRequestProfilerBodyRow> bodies, byte[] bodyBytes)
        {
            if (rows == null || rows.Count == 0)
                return false;

            foreach (var row in rows)
            {
                if (!m_ById.TryGetValue(row.requestId, out var record))
                {
                    record = new WebRequestProfilerRecord
                    {
                        requestId = row.requestId,
                        captureIndex = m_Records.Count,
                    };
                    m_ById.Add(row.requestId, record);
                    m_Records.Add(record);
                }

                // Scrubbing backwards ingests older frames after newer ones, and a request's later
                // frames hold its newer state, so a record only ever moves forwards.
                if (frameIndex < record.lastSeenFrame)
                    continue;

                record.lastSeenFrame = frameIndex;
                record.url = WebRequestProfilerFrameReader.Slice(strings, row.urlOffset, row.urlLength);
                record.method = WebRequestProfilerFrameReader.Slice(strings, row.methodOffset, row.methodLength);
                record.bytesDownloaded = row.bytesDownloaded;
                record.bytesUploaded = row.bytesUploaded;
                record.durationNs = row.durationNs;
                record.statusCode = row.statusCode;
                record.state = (WebRequestProfilerState)row.state;
                record.source = (WebRequestProfilerSource)row.source;
                record.measured = (WebRequestProfilerMeasurement)row.measured;
                record.bodyCapture = (WebRequestProfilerBodyCapture)row.bodyCapture;

                // Only the response carries one, so an in-flight row must not blank what a completion
                // already established.
                var contentType = WebRequestProfilerFrameReader.Slice(strings, row.contentTypeOffset, row.contentTypeLength);
                if (!string.IsNullOrEmpty(contentType))
                    record.contentType = contentType;

                // Headers and timings are recorded as a request finishes, and only read off a frame that
                // shows it as finished. Native writes them under a different lock acquisition from the
                // one that marks the request complete, so a frame snapshot taken between the two emits
                // an in-flight row that already carries some of its headers. Ignoring them until the row
                // says finished makes that window unobservable, and costs nothing on the normal path
                // because that is the only frame carrying them anyway.
                if (record.state != WebRequestProfilerState.InFlight)
                {
                    // Set and never cleared: re-reading any other frame would otherwise drop what the
                    // completion frame established. Headers replace rather than append, so re-reading
                    // the same frame cannot list one twice.
                    var rowHeaders = ResolveHeaders(headers, strings, row.requestId);
                    if (rowHeaders != null)
                        record.headers = rowHeaders;
                    else if (headers != null)
                        // The headers tag is emitted on every snapshot, so its presence here means they
                        // were recorded and none carrying this id means the request genuinely had none.
                        // An empty list says that, where null still means nothing has reported yet.
                        record.headers ??= new List<WebRequestProfilerHeader>();

                    if (TryFindTimings(timings, row.requestId, out var timingRow))
                    {
                        record.timings = timingRow;
                        record.hasTimings = true;
                    }

                    // Set and never cleared, as headers are: recorded on the finishing frame only.
                    if (TryResolveBody(bodies, bodyBytes, strings, row.requestId,
                        WebRequestProfilerBodyKind.Request, out var requestBody))
                        record.requestBody = requestBody;

                    if (TryResolveBody(bodies, bodyBytes, strings, row.requestId,
                        WebRequestProfilerBodyKind.Response, out var responseBody))
                        record.responseBody = responseBody;

                    record.completedFrame = frameIndex;
                }
            }

            return true;
        }

        // Null when this frame carried no headers for the request, which the caller reads as "leave
        // whatever is already there" - it decides separately whether that means none were captured or
        // none have arrived yet. Text is resolved here because a header's offsets index the string blob
        // of its own frame, and that blob is gone once the next frame is read.
        static List<WebRequestProfilerHeader> ResolveHeaders(List<WebRequestProfilerHeaderRow> headers,
            byte[] strings, ulong requestId)
        {
            if (headers == null)
                return null;

            List<WebRequestProfilerHeader> resolved = null;
            foreach (var header in headers)
            {
                if (header.requestId != requestId)
                    continue;

                resolved ??= new List<WebRequestProfilerHeader>();
                resolved.Add(new WebRequestProfilerHeader(
                    (WebRequestProfilerHeaderKind)header.kind,
                    WebRequestProfilerFrameReader.Slice(strings, header.nameOffset, header.nameLength),
                    WebRequestProfilerFrameReader.Slice(strings, header.valueOffset, header.valueLength)));
            }

            return resolved;
        }

        // Scanned like the timings. Text is resolved here because a body's offsets index its own
        // frame's blob, which is gone once the next frame is read.
        static bool TryResolveBody(List<WebRequestProfilerBodyRow> bodies, byte[] bodyBytes, byte[] strings,
            ulong requestId, WebRequestProfilerBodyKind kind, out WebRequestProfilerBody body)
        {
            body = default;
            if (bodies == null)
                return false;

            foreach (var candidate in bodies)
            {
                if (candidate.requestId != requestId || (WebRequestProfilerBodyKind)candidate.kind != kind)
                    continue;

                var contentType = WebRequestProfilerFrameReader.Slice(strings,
                    candidate.contentTypeOffset, candidate.contentTypeLength);

                body = new WebRequestProfilerBody(
                    WebRequestProfilerFrameReader.DecodeBody(bodyBytes, candidate.bodyOffset,
                        candidate.bodyLength, contentType),
                    candidate.totalLength,
                    candidate.bodyLength,
                    (WebRequestProfilerBodyFlags)candidate.flags);
                return true;
            }

            return false;
        }

        // Scanned rather than indexed: a frame carries timings only for the requests that finished on
        // it, so the list is empty on almost every frame and never more than a handful long.
        static bool TryFindTimings(List<WebRequestProfilerTimingRow> timings, ulong requestId,
            out WebRequestProfilerTimingRow found)
        {
            found = default;
            if (timings == null)
                return false;

            foreach (var candidate in timings)
            {
                if (candidate.requestId != requestId)
                    continue;

                found = candidate;
                return true;
            }

            return false;
        }
    }
}
