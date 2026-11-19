// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Networking
{
    // Turns the raw counters a request record carries into the strings the details view shows.
    static class WebRequestProfilerFormatting
    {
        const ulong k_Kilobyte = 1024;
        const ulong k_Megabyte = k_Kilobyte * 1024;
        const ulong k_Gigabyte = k_Megabyte * 1024;
        const ulong k_Terabyte = k_Gigabyte * 1024;

        // EditorUtility.FormatBytes is the house helper, but it is long-typed and spells its units KB/MB.
        public static string FormatBytes(ulong bytes)
        {
            if (bytes < k_Kilobyte)
                return $"{bytes} B";
            if (bytes < k_Megabyte)
                return $"{bytes / (float)k_Kilobyte:0.#} kB";
            if (bytes < k_Gigabyte)
                return $"{bytes / (float)k_Megabyte:0.##} MB";
            if (bytes < k_Terabyte)
                return $"{bytes / (float)k_Gigabyte:0.##} GB";

            return $"{bytes / (float)k_Terabyte:0.##} TB";
        }

        // Every noun the summary bar counts takes a trailing s, so English's regular plural is enough.
        public static string FormatCount(int count, string singularNoun)
        {
            return count == 1 ? $"{count} {singularNoun}" : $"{count} {singularNoun}s";
        }

        // An em dash rather than "0 B", which would be a measurement, and rather than the hyphen the
        // other columns use for "not known yet".
        public const string NotMeasured = "—";

        public const string NotMeasuredTooltip =
            "Not measured: the transport that ran this request does not count transferred bytes.";

        // Three renderings, because a zero has two meanings: a number when the transport counted,
        // "0 B" when it counted nothing crossing the wire, NotMeasured when nothing counted at all.
        public static string FormatTransferSize(ulong bytes, bool measured)
        {
            return measured ? FormatBytes(bytes) : NotMeasured;
        }

        public const string PartialTotalTooltip =
            "A lower bound: some of these requests ran on a transport that does not count transferred bytes.";

        // Rows need not agree on whether their bytes were measurable, and unmeasured ones contribute
        // nothing - so a partial sum is a floor and says so, rather than an exact-looking wrong number.
        public static string FormatTransferTotal(ulong bytes, bool anyMeasured, bool anyUnmeasured)
        {
            if (!anyMeasured)
                return NotMeasured;

            return anyUnmeasured ? $"≥ {FormatBytes(bytes)}" : FormatBytes(bytes);
        }

        // Per direction: the Web transport counts what it downloads and has no upload counter.
        public static bool HasMeasuredDownload(WebRequestProfilerRecord record)
        {
            return (record.measured & WebRequestProfilerMeasurement.Download) != 0;
        }

        public static bool HasMeasuredUpload(WebRequestProfilerRecord record)
        {
            return (record.measured & WebRequestProfilerMeasurement.Upload) != 0;
        }

        public static string FormatDownloadSize(WebRequestProfilerRecord record)
        {
            return FormatTransferSize(record.bytesDownloaded, HasMeasuredDownload(record));
        }

        public static string FormatUploadSize(WebRequestProfilerRecord record)
        {
            return FormatTransferSize(record.bytesUploaded, HasMeasuredUpload(record));
        }

        // Nanoseconds in, because a local request finishes well under a millisecond and would otherwise
        // be shown as 0.
        public static string FormatDuration(ulong nanoseconds)
        {
            if (nanoseconds < 1_000)
                return $"{nanoseconds} ns";
            if (nanoseconds < 1_000_000)
                return $"{nanoseconds / 1_000f:0.##} µs";
            if (nanoseconds < 1_000_000_000)
                return $"{nanoseconds / 1_000_000f:0.##} ms";

            return $"{nanoseconds / 1_000_000_000f:0.##} s";
        }

        // Column form: narrow enough that the state has to be carried by a glyph rather than a word.
        public static string FormatStatusCode(WebRequestProfilerRecord record)
        {
            if (record.state == WebRequestProfilerState.InFlight)
                return "...";

            return record.statusCode != 0 ? record.statusCode.ToString() : "-";
        }

        // The managed API the request came through. Native cannot tell them apart, so an untagged row
        // reads as UnityWebRequest.
        public static string FormatSource(WebRequestProfilerRecord record)
        {
            return record.source == WebRequestProfilerSource.HttpClient
                ? ".NET HttpClient"
                : "UnityWebRequest";
        }

        // Inspector form.
        public static string FormatStatus(WebRequestProfilerRecord record)
        {
            switch (record.state)
            {
                case WebRequestProfilerState.InFlight:
                    return "In flight";
                case WebRequestProfilerState.Failed:
                    return record.statusCode != 0 ? $"{WithReason(record.statusCode)} (failed)" : "Failed";
                default:
                    return record.statusCode != 0 ? WithReason(record.statusCode) : "Completed";
            }
        }

        static string WithReason(ushort statusCode)
        {
            var reason = ReasonPhrase(statusCode);
            return reason == null ? statusCode.ToString() : $"{statusCode} {reason}";
        }

        // HTTP/2 carries no reason phrase and UnityWebRequest does not expose HTTP/1's, so it comes from RFC 9110.
        internal static string ReasonPhrase(ushort statusCode)
        {
            switch (statusCode)
            {
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";
                case 103: return "Early Hints";
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 208: return "Already Reported";
                case 226: return "IM Used";
                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 308: return "Permanent Redirect";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Content Too Large";
                case 414: return "URI Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 421: return "Misdirected Request";
                case 422: return "Unprocessable Content";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 425: return "Too Early";
                case 426: return "Upgrade Required";
                case 428: return "Precondition Required";
                case 429: return "Too Many Requests";
                case 431: return "Request Header Fields Too Large";
                case 451: return "Unavailable For Legal Reasons";
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "HTTP Version Not Supported";
                case 506: return "Variant Also Negotiates";
                case 507: return "Insufficient Storage";
                case 508: return "Loop Detected";
                case 510: return "Not Extended";
                case 511: return "Network Authentication Required";
                default: return null;
            }
        }
    }
}
