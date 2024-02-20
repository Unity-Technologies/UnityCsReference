// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Profiling.Memory
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

    public class MemorySnapshotMetadata
    {
        public string    Description { get; set; }
        // Not part of the public API for now, but Memory Profiler Package may choose to use and expose this
        // via unsafe code, the MetaDataInjector and extension methods.
        internal byte[]  Data { get; set; }
    }

    [NativeHeader("Runtime/Profiler/Runtime/MemorySnapshotManager.h")]
    public static class MemoryProfiler
    {
        private static event Action<string, bool> m_SnapshotFinished;
        private static event Action<string, bool, DebugScreenCapture> m_SaveScreenshotToDisk;

        public static event Action<MemorySnapshotMetadata>     CreatingMetadata;

        static bool isCompiling = false;
        internal static void StartedCompilationCallback(object msg)
        {
            isCompiling = true;
        }

        internal static void FinishedCompilationCallback(object msg)
        {
            isCompiling = false;
        }


        [StaticAccessor("profiling::memory::GetMemorySnapshotManager()", StaticAccessorType.Dot)]
        [NativeMethod("StartOperation")]
        [NativeConditional("ENABLE_PROFILER")]
        private static extern void StartOperation(uint captureFlag, bool requestScreenshot, string path, bool isRemote);

        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            TakeSnapshot(path, finishCallback, null, captureFlags);
        }

        public static void TakeSnapshot(string path, Action<string, bool> finishCallback, Action<string, bool, DebugScreenCapture> screenshotCallback, CaptureFlags captureFlags = CaptureFlags.NativeObjects | CaptureFlags.ManagedObjects)
        {
            if (isCompiling)
            {
                Debug.LogError("Canceling snapshot, there is a compilation in progress.");
                return;
            }

            if (m_SnapshotFinished != null)
            {
                Debug.LogWarning("Canceling snapshot, there is another snapshot in progress.");
                finishCallback(path, false);
            }
            else
            {
                m_SnapshotFinished += finishCallback;
                m_SaveScreenshotToDisk += screenshotCallback;
                StartOperation((uint)captureFlags, m_SaveScreenshotToDisk != null, path, false);
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
            if (CreatingMetadata == null)
            {
                return new byte[0];
            }

            MemorySnapshotMetadata data = new MemorySnapshotMetadata();
            data.Description = string.Empty;
            CreatingMetadata(data);

            if (data.Description == null) data.Description = "";

            int contentLength = sizeof(char) * data.Description.Length;
            int dataLength = (data.Data == null ? 0 : data.Data.Length);

            int metaDataSize = contentLength + dataLength + sizeof(int) * 3 /*data.Description.Length + data.Data.Length*/;

            byte[] metaDataBytes = new byte[metaDataSize];
            // encoded as
            //   description_data_length
            //   description_data
            //   bytearraydata_data_length
            //   bytearraydata_data

            int offset = 0;
            offset = WriteIntToByteArray(metaDataBytes, offset, data.Description.Length);
            offset = WriteStringToByteArray(metaDataBytes, offset, data.Description);

            offset = WriteIntToByteArray(metaDataBytes, offset, dataLength);
            unsafe
            {
                fixed(byte* src = data.Data, dst = metaDataBytes)
                {
                    var start = dst + offset;
                    UnsafeUtility.MemCpy(start, src, dataLength);
                }
            }

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
            if (m_SnapshotFinished != null)
            {
                var onSnapshotFinished = m_SnapshotFinished;

                m_SnapshotFinished = null;

                onSnapshotFinished(path, result);
            }
        }

        [RequiredByNativeCode]
        static void SaveScreenshotToDisk(string path, bool result, IntPtr pixelsPtr, int pixelsCount, TextureFormat format, int width, int height)
        {
            if (m_SaveScreenshotToDisk != null)
            {
                var saveScreenshotToDisk = m_SaveScreenshotToDisk;
                m_SaveScreenshotToDisk = null;
                DebugScreenCapture debugScreenCapture = default(DebugScreenCapture);

                if (result)
                {
                    unsafe
                    {
                        var nonOwningNativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(pixelsPtr.ToPointer(), pixelsCount, Allocator.Persistent);
                        debugScreenCapture.RawImageDataReference = nonOwningNativeArray;
                    }

                    debugScreenCapture.Height = height;
                    debugScreenCapture.Width = width;
                    debugScreenCapture.ImageFormat = format;
                }

                saveScreenshotToDisk(path, result, debugScreenCapture);
            }
        }
    }
}
