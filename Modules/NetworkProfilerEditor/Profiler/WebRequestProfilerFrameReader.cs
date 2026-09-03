// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Networking
{
    // Reads the web request rows and the string blob a frame's profiler metadata carries. Kept apart
    // from the view so the designed UI Toolkit view can reuse it unchanged.
    static class WebRequestProfilerFrameReader
    {
        // Metadata is emitted from ProfilerManager::StartNewFrame, which runs on the main thread, so
        // only thread 0 carries any of it.
        const int k_MainThreadIndex = 0;

        public static List<WebRequestProfilerRow> ReadFrame(long frameIndex, out byte[] strings)
        {
            strings = Array.Empty<byte>();

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

                strings = ReadStringBlob(frameData, guid);

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

        // Every row offset indexes one blob, so a second chunk would make all of them ambiguous. That
        // constraint binds the emitter - concatenating here would silently misattribute strings - so an
        // extra chunk is reported rather than reconciled.
        static byte[] ReadStringBlob(RawFrameDataView frameData, Guid guid)
        {
            var chunkCount = frameData.GetFrameMetaDataCount(guid, WebRequestProfilerData.TagStrings);
            if (chunkCount <= 0)
                return Array.Empty<byte>();

            if (chunkCount > 1)
            {
                Debug.LogError($"Web request profiler metadata holds {chunkCount} string blobs in one frame, but row offsets index a single blob. This frame cannot be read.");
                return Array.Empty<byte>();
            }

            using (var blob = frameData.GetFrameMetaData<byte>(guid, WebRequestProfilerData.TagStrings, 0))
            {
                return blob.ToArray();
            }
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
