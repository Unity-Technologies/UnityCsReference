// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Networking
{
    // Reads the web request rows, headers, per-phase timings and string blob a frame's profiler
    // metadata carries. Kept apart from the view so the designed UI Toolkit view can reuse it
    // unchanged.
    static class WebRequestProfilerFrameReader
    {
        // Metadata is emitted from ProfilerManager::StartNewFrame, which runs on the main thread, so
        // only thread 0 carries any of it.
        const int k_MainThreadIndex = 0;

        public static List<WebRequestProfilerRow> ReadFrame(long frameIndex, out byte[] strings)
        {
            return ReadFrame(frameIndex, out strings, out _, out _, out _, out _);
        }

        public static List<WebRequestProfilerRow> ReadFrame(long frameIndex, out byte[] strings,
            out List<WebRequestProfilerHeaderRow> headers, out List<WebRequestProfilerTimingRow> timings)
        {
            return ReadFrame(frameIndex, out strings, out headers, out timings, out _, out _);
        }

        public static List<WebRequestProfilerRow> ReadFrame(long frameIndex, out byte[] strings,
            out List<WebRequestProfilerHeaderRow> headers, out List<WebRequestProfilerTimingRow> timings,
            out List<WebRequestProfilerBodyRow> bodies, out byte[] bodyBytes)
        {
            strings = Array.Empty<byte>();
            headers = null;
            timings = null;
            bodies = null;
            bodyBytes = Array.Empty<byte>();

            // ProfilerWindow hands out a long, ProfilerDriver indexes frames with an int.
            if (frameIndex < 0 || frameIndex > int.MaxValue)
                return null;

            using (var frameData = ProfilerDriver.GetRawFrameDataView((int)frameIndex, k_MainThreadIndex))
            {
                if (frameData == null || !frameData.valid)
                    return null;

                var guid = WebRequestProfilerData.MetadataGuid;

                var rows = ReadChunks<WebRequestProfilerRow>(frameData, guid, WebRequestProfilerData.TagRequests);
                if (rows == null)
                    return null;

                // Both are absent on most frames: they are recorded when a request finishes, and the
                // timings only when its transport can measure the phases.
                headers = ReadChunks<WebRequestProfilerHeaderRow>(frameData, guid, WebRequestProfilerData.TagHeaders);

                if (headers == null)
                    headers = ReadChunks<WebRequestProfilerHeaderRow>(frameData, guid, WebRequestProfilerData.TagHeadersLegacy);
                timings = ReadChunks<WebRequestProfilerTimingRow>(frameData, guid, WebRequestProfilerData.TagTimings);

                // Absent on every frame of every capture where nobody switched body capture on.
                bodies = ReadChunks<WebRequestProfilerBodyRow>(frameData, guid, WebRequestProfilerData.TagBodies);
                if (bodies != null)
                    bodyBytes = ReadBlob(frameData, guid, WebRequestProfilerData.TagBodyBytes, "body");

                strings = ReadBlob(frameData, guid, WebRequestProfilerData.TagStrings, "string");

                return rows;
            }
        }

        // Null when the tag is absent, which the caller distinguishes from an empty list. In practice
        // native emits one chunk per tag per frame, so this iterates only to avoid depending on that.
        static List<T> ReadChunks<T>(RawFrameDataView frameData, Guid guid, int tag) where T : struct
        {
            var chunkCount = frameData.GetFrameMetaDataCount(guid, tag);
            if (chunkCount <= 0)
                return null;

            var items = new List<T>();
            for (var chunkIndex = 0; chunkIndex < chunkCount; ++chunkIndex)
            {
                using (var chunk = frameData.GetFrameMetaData<T>(guid, tag, chunkIndex))
                {
                    for (var i = 0; i < chunk.Length; ++i)
                        items.Add(chunk[i]);
                }
            }

            return items;
        }

        // Every offset indexes one blob, so a second chunk makes all of them ambiguous: concatenating
        // would silently misattribute. Both blobs are pinned the same way, hence one reader.
        static byte[] ReadBlob(RawFrameDataView frameData, Guid guid, int tag, string blobName)
        {
            var chunkCount = frameData.GetFrameMetaDataCount(guid, tag);
            if (chunkCount <= 0)
                return Array.Empty<byte>();

            if (chunkCount > 1)
            {
                Debug.LogError($"Web request profiler metadata holds {chunkCount} {blobName} blobs in one frame, but row offsets index a single blob. This frame cannot be read.");
                return Array.Empty<byte>();
            }

            using (var blob = frameData.GetFrameMetaData<byte>(guid, tag, 0))
            {
                return blob.ToArray();
            }
        }

        // Decoded by what the body declares: a BOM, then the charset parameter, then an XML declaration's
        // encoding, then UTF-8. Replacement fallback everywhere so a mis-declared body cannot throw into the view.
        public static string DecodeBody(byte[] bodyBytes, uint offset, uint length, string contentType)
        {
            if (bodyBytes == null || length == 0)
                return string.Empty;

            if ((long)offset + length > bodyBytes.Length)
                return string.Empty;

            var start = (int)offset;
            var count = (int)length;

            var bomEncoding = EncodingFromBom(bodyBytes, start, count, out var bomLength);
            if (bomEncoding != null)
                return bomEncoding.GetString(bodyBytes, start + bomLength, count - bomLength);

            return EncodingFor(contentType, bodyBytes, start, count).GetString(bodyBytes, start, count);
        }

        static readonly byte[] k_Utf8Bom = { 0xEF, 0xBB, 0xBF };
        static readonly byte[] k_Utf32LittleEndianBom = { 0xFF, 0xFE, 0x00, 0x00 };
        static readonly byte[] k_Utf32BigEndianBom = { 0x00, 0x00, 0xFE, 0xFF };
        static readonly byte[] k_Utf16LittleEndianBom = { 0xFF, 0xFE };
        static readonly byte[] k_Utf16BigEndianBom = { 0xFE, 0xFF };

        [NoAutoStaticsCleanup]
        static readonly (byte[] bom, Encoding encoding)[] k_BomEncodings =
        {
            (k_Utf8Bom, Encoding.UTF8),
            (k_Utf32LittleEndianBom, new UTF32Encoding(false, false, false)),
            (k_Utf32BigEndianBom, new UTF32Encoding(true, false, false)),
            (k_Utf16LittleEndianBom, new UnicodeEncoding(false, false, false)),
            (k_Utf16BigEndianBom, new UnicodeEncoding(true, false, false)),
        };

        static Encoding EncodingFromBom(byte[] bytes, int start, int count, out int bomLength)
        {
            var body = bytes.AsSpan(start, count);
            foreach (var (bom, encoding) in k_BomEncodings)
            {
                if (body.StartsWith(bom))
                {
                    bomLength = bom.Length;
                    return encoding;
                }
            }

            bomLength = 0;
            return null;
        }

        static Encoding EncodingFor(string contentType, byte[] bytes, int start, int count)
        {
            var charsetEncoding = EncodingByName(CharsetOf(contentType));
            if (charsetEncoding != null)
                return charsetEncoding;

            if (IsXmlContentType(contentType))
            {
                var declaredEncoding = EncodingByName(XmlDeclarationEncoding(bytes, start, count));
                if (declaredEncoding != null)
                    return declaredEncoding;
            }

            return Encoding.UTF8;
        }

        static Encoding EncodingByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (string.Equals(name, "utf-8", StringComparison.OrdinalIgnoreCase))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(name, EncoderFallback.ReplacementFallback,
                    DecoderFallback.ReplacementFallback);
            }
            catch (ArgumentException)
            {
                // A charset this runtime does not carry, or one the peer invented. Fall through.
                return null;
            }
        }

        static bool IsXmlContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var end = contentType.IndexOf(';');
            var mediaType = (end < 0 ? contentType : contentType.Substring(0, end)).Trim();
            return mediaType.EndsWith("/xml", StringComparison.OrdinalIgnoreCase)
                || mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
        }

        const int k_XmlDeclarationScanLimit = 256;

        static string XmlDeclarationEncoding(byte[] bytes, int start, int count)
        {
            var scan = Math.Min(count, k_XmlDeclarationScanLimit);
            var builder = new StringBuilder(scan);
            for (var i = 0; i < scan; ++i)
            {
                if (bytes[start + i] >= 0x80)
                    break;

                builder.Append((char)bytes[start + i]);
            }

            var text = builder.ToString();
            if (!text.StartsWith("<?xml", StringComparison.Ordinal) || text.Length < 6 || !char.IsWhiteSpace(text[5]))
                return null;

            var close = text.IndexOf("?>", StringComparison.Ordinal);
            if (close < 0)
                return null;

            var at = text.IndexOf("encoding", 5, close - 5, StringComparison.Ordinal);
            if (at < 0)
                return null;

            var cursor = at + "encoding".Length;
            while (cursor < close && char.IsWhiteSpace(text[cursor]))
                ++cursor;

            if (cursor >= close || text[cursor] != '=')
                return null;

            ++cursor;
            while (cursor < close && char.IsWhiteSpace(text[cursor]))
                ++cursor;

            if (cursor >= close || (text[cursor] != '"' && text[cursor] != '\''))
                return null;

            var valueEnd = text.IndexOf(text[cursor], cursor + 1, close - cursor - 1);
            return valueEnd < 0 ? null : text.Substring(cursor + 1, valueEnd - cursor - 1).Trim();
        }

        // The charset parameter, or null. Walked rather than searched: "charset=" is legal text inside
        // another parameter's quoted value, and a quoted value can carry the ';' that separates them.
        static string CharsetOf(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return null;

            // Before the first ';' is the type, which carries no parameters.
            var i = contentType.IndexOf(';');
            if (i < 0)
                return null;

            while (i < contentType.Length)
            {
                ++i;

                var nameStart = i;
                while (i < contentType.Length && contentType[i] != '=' && contentType[i] != ';')
                    ++i;

                // Malformed - a parameter with no value - but not worth refusing over.
                if (i >= contentType.Length || contentType[i] == ';')
                    continue;

                var name = contentType.Substring(nameStart, i - nameStart).Trim();
                ++i;

                string value;
                if (i < contentType.Length && contentType[i] == '"')
                {
                    ++i;
                    var quotedStart = i;
                    while (i < contentType.Length && contentType[i] != '"')
                        ++i;

                    value = contentType.Substring(quotedStart, i - quotedStart);

                    // Past the closing quote to the parameter's end.
                    while (i < contentType.Length && contentType[i] != ';')
                        ++i;
                }
                else
                {
                    var valueStart = i;
                    while (i < contentType.Length && contentType[i] != ';')
                        ++i;

                    value = contentType.Substring(valueStart, i - valueStart).Trim();
                }

                if (string.Equals(name, "charset", StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            return null;
        }

        public static string Slice(byte[] strings, uint offset, uint length)
        {
            if (strings == null || length == 0)
                return string.Empty;

            // Guards against a truncated or mismatched blob rather than throwing inside the view.
            if ((long)offset + length > strings.Length)
                return string.Empty;

            return Encoding.UTF8.GetString(strings, (int)offset, (int)length);
        }
    }
}
