// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Profiling.Memory.Experimental
{
    [Flags]
    public enum CaptureFlags : uint
    {
        ManagedObjects        = 1 << 0,
        NativeObjects         = 1 << 1,
        NativeAllocations     = 1 << 2,
        NativeAllocationSites = 1 << 3,
        NativeStackTraces     = 1 << 4,
    }

    public class MetaData
    {
        public string    content;
        public string    platform;
        public Texture2D screenshot;
    }

    [NativeHeader("Modules/ProfilerEditor/Public/EditorProfilerConnection.h")]
    public sealed class MemoryProfiler
    {
        private static event Action<string, bool> snapshotFinished;
        public static event Action<MetaData>     createMetaData;

        [StaticAccessor("EditorProfilerConnection::Get()", StaticAccessorType.Dot)]
        [NativeMethod("TakeMemorySnapshot")]
        [NativeConditional("ENABLE_PLAYERCONNECTION")]
        private static extern void TakeSnapshotInternal(string path, uint captureFlag);

        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            if (snapshotFinished != null)
            {
                Debug.LogWarning("Canceling taking the snapshot. There is already ongoing capture.");
                finishCallback(path, false);
            }
            else
            {
                snapshotFinished += finishCallback;
                TakeSnapshotInternal(path, (uint)captureFlags);
            }
        }

        public static void TakeTempSnapshot(Action<string, bool>  finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            string[] s = Application.dataPath.Split('/');
            string projectName = s[s.Length - 2];
            string path = Application.temporaryCachePath + "/" + projectName + ".snap";
            TakeSnapshot(path, finishCallback, captureFlags);
        }

        [RequiredByNativeCode]
        static byte[] PrepareMetadata()
        {
            if (createMetaData == null)
            {
                return new byte[0];
            }

            MetaData data = new MetaData();
            createMetaData(data);

            if (data.content == null) data.content = "";
            if (data.platform == null) data.platform = "";

            int contentLength = sizeof(char) * data.content.Length;
            int platformLength = sizeof(char) * data.platform.Length;

            int metaDataSize = contentLength + platformLength + sizeof(int) * 3 /*ScreenshotDataSize + content.Length + data.platform.Length*/;

            byte[] screenshotRaw = null;

            if (data.screenshot != null)
            {
                screenshotRaw = data.screenshot.GetRawTextureData();
                metaDataSize += screenshotRaw.Length + sizeof(int) * 3 /*width, height, format*/;
            }

            byte[] metaDataBytes = new byte[metaDataSize];
            // encoded as
            //   content_data_length
            //   content_data
            //   platform_data_length
            //   platform_data
            //   screenshot_data_length
            //     [opt: screenshot_data ]
            //     [opt: screenshot_width ]
            //     [opt: screenshot_height ]
            //     [opt: screenshot_format ]

            int offset = 0;
            offset = WriteIntToByteArray(metaDataBytes, offset, data.content.Length);
            offset = WriteStringToByteArray(metaDataBytes, offset, data.content);

            offset = WriteIntToByteArray(metaDataBytes, offset, data.platform.Length);
            offset = WriteStringToByteArray(metaDataBytes, offset, data.platform);

            if (data.screenshot != null)
            {
                offset = WriteIntToByteArray(metaDataBytes, offset, screenshotRaw.Length);
                Array.Copy(screenshotRaw, 0, metaDataBytes, offset, screenshotRaw.Length);
                offset += screenshotRaw.Length;

                offset = WriteIntToByteArray(metaDataBytes, offset, data.screenshot.width);
                offset = WriteIntToByteArray(metaDataBytes, offset, data.screenshot.height);
                offset = WriteIntToByteArray(metaDataBytes, offset, (int)data.screenshot.format);
            }
            else
            {
                offset = WriteIntToByteArray(metaDataBytes, offset, 0);
            }

            Assertions.Assert.AreEqual(metaDataBytes.Length, offset);

            return metaDataBytes;
        }

        internal static int WriteIntToByteArray(byte[] array, int offset, int value)
        {
            unsafe
            {
                byte* pi = (byte*)&value;
                array[offset++] = pi[0];
                array[offset++] = pi[1];
                array[offset++] = pi[2];
                array[offset++] = pi[3];
            }

            return offset;
        }

        internal static int WriteStringToByteArray(byte[] array, int offset, string value)
        {
            if (value.Length != 0)
            {
                unsafe
                {
                    fixed(char* p = value)
                    {
                        char* begin = p;
                        char* end = p + value.Length;

                        while (begin != end)
                        {
                            for (int i = 0; i < sizeof(char); ++i)
                            {
                                array[offset++] = ((byte*)begin)[i];
                            }

                            begin++;
                        }
                    }
                }
            }

            return offset;
        }

        [RequiredByNativeCode]
        static void FinalizeSnapshot(string path, bool result)
        {
            if (snapshotFinished != null)
            {
                var onSnapshotFinished = snapshotFinished;

                snapshotFinished = null;

                onSnapshotFinished(path, result);
            }
        }
    }
}
