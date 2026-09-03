// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Networking
{
    // Turns the raw counters a frame row carries into the strings the details view shows.
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

        // Nothing captures byte counts yet, so every row carries 0 and "0 B" would read as a request
        // that transferred nothing rather than one whose size is unknown. Revisit when the transport
        // supplies real counts: a genuinely empty response should then read "0 B" again.
        public static string FormatTransferSize(ulong bytes)
        {
            return bytes != 0 ? FormatBytes(bytes) : "-";
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
        public static string FormatStatusCode(WebRequestProfilerRow row)
        {
            if ((WebRequestProfilerState)row.state == WebRequestProfilerState.InFlight)
                return "...";

            return row.statusCode != 0 ? row.statusCode.ToString() : "-";
        }

        // The managed API the request came through. Native cannot tell them apart, so an untagged row
        // reads as UnityWebRequest.
        public static string FormatSource(WebRequestProfilerRow row)
        {
            return (WebRequestProfilerSource)row.source == WebRequestProfilerSource.HttpClient
                ? ".NET HttpClient"
                : "UnityWebRequest";
        }

        // Inspector form.
        public static string FormatStatus(WebRequestProfilerRow row)
        {
            switch ((WebRequestProfilerState)row.state)
            {
                case WebRequestProfilerState.InFlight:
                    return "In flight";
                case WebRequestProfilerState.Failed:
                    return row.statusCode != 0 ? $"{row.statusCode} (failed)" : "Failed";
                default:
                    return row.statusCode != 0 ? row.statusCode.ToString() : "Completed";
            }
        }
    }
}
