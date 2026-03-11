// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewModel : IDisposable
    {
        const int k_CurrentVersion = 2;
        const string k_HighlightsFileHeader = "HIGHLIGHTS_INFO";
        public const string k_HighlightFileExtension = ".highlights";

        public BottlenecksChartViewModel(
            int numberOfDataSeries,
            int dataSeriesCapacity,
            float bottleneckThreshold)
        {
            HighlightsFileVersion = -1;
            NumberOfDataSeries = numberOfDataSeries;
            DataSeriesCapacity = dataSeriesCapacity;

            DataValueBuffers = new NativeArray<float>[numberOfDataSeries];
            PercentOverThreshold = new float[numberOfDataSeries];
            BottleneckThreshold = bottleneckThreshold;

            // Don't bother allocating for zero data.
            if (dataSeriesCapacity < 1)
                return;

            for (var i = 0; i < DataValueBuffers.Length; i++)
            {
                PercentOverThreshold[i] = 0f;
                DataValueBuffers[i] = new NativeArray<float>(dataSeriesCapacity, Allocator.Persistent);
            }
        }

        public int HighlightsFileVersion { get; private set; }

        // The number of data series on the chart.
        public int NumberOfDataSeries { get; private set; }

        // The capacity of each data series on the chart.
        public int DataSeriesCapacity { get; private set; }

        // Capacity used for thumbnails in the captures list.
        // This is the amount of frames with actual data.
        public int DataSeriesCapacityThumbnail { get; private set; }

        // The buffers of all data values for each data series.
        public NativeArray<float>[] DataValueBuffers { get; }

        // How many frames are over the BottleneckThreshold?
        public float[] PercentOverThreshold { get; private set; }

        // The colors for each data series.
        public static readonly Color[] Colors = new[]
        {
            new Color(0.929f, 0.337f, 0.337f), // #ED5656
            new Color(0.929f, 0.906f, 0.337f), // #EDE756
        };

        static readonly Color k_InvalidColorPro = new Color(0.078f, 0.078f, 0.078f);
        static readonly Color k_InvalidColorNonPro = new Color(0.247f, 0.247f, 0.247f);

        // The color for invalid data values in all data series.
        static Color s_InvalidColor;
        public static Color InvalidColor
        {
            get
            {
                if (s_InvalidColor == Color.clear)
                    s_InvalidColor = EditorGUIUtility.isProSkin ? k_InvalidColorPro : k_InvalidColorNonPro;

                return s_InvalidColor;
            }
        }

        // The value at which the data values are identified as a 'bottleneck'.
        public float BottleneckThreshold { get; set; }

        // The frame index of the first element in the data buffers.
        public int FirstFrameIndex { get; set; }

        // The associated file on disk, if applicable
        string CachedFilePath { get; set; } = string.Empty;

        // Session metadata from the capture
        public int CaptureMetaDataVersion { get; private set; } = -1;

        public uint RuntimeSessionId { get; private set; }

        public RuntimePlatform Platform { get; private set; } = (RuntimePlatform)(-1);

        public GraphicsDeviceType GraphicsDeviceType { get; private set; } = (GraphicsDeviceType)(-1);

        public ulong TotalPhysicalMemory { get; private set; }

        public ulong TotalGraphicsMemory { get; private set; }

        public ScriptingImplementation ScriptingBackend { get; private set; } = (ScriptingImplementation)(-1);

        public double TimeSinceStartup { get; private set; }

        public long FrameCountSinceStartup { get; private set; }

        public string UnityVersion { get; private set; } = string.Empty;

        public string ProductName { get; private set; } = string.Empty;

        public DateTime DateTimeOfRecording { get; private set; }

        long m_TimeOfRecordingNative;
        public long TimeOfRecordingNative
        {
            get => m_TimeOfRecordingNative;
            private set
            {
                m_TimeOfRecordingNative = value;
                DateTimeOfRecording = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(value).ToLocalTime();
            }
        }

        public string DeviceModel { get; private set; } = string.Empty;

        public string DeviceSystemVersion { get; private set; } = string.Empty;


        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            foreach (NativeArray<float> dataBuffer in DataValueBuffers)
                dataBuffer.Dispose();

            IsDisposed = true;
        }

        void UpdateHighlightsInfoFromMetadata()
        {
            using (var frameData = ProfilerDriver.GetRawFrameDataView(ProfilerDriver.lastFrameIndex, 0))
            {
                // Return early if it looks like we don't have any session metadata.
                CaptureMetaDataVersion = -1;
                if (1 > frameData.GetSessionMetaDataCount(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)ProfilingSessionMetaDataEntry.Version))
                    return;

                // Due to a previous issue with attempting to write metadata before it was available,
                // we need to individually check existence of these entries.
                // "Version" was already checked, so set that directly - others go via helper functions.

                CaptureMetaDataVersion = frameData.GetProfilingSessionMetaData<int>(ProfilingSessionMetaDataEntry.Version);
                RuntimeSessionId = frameData.GetProfilingSessionMetaDataLatest<uint>(ProfilingSessionMetaDataEntry.RuntimeSessionId) ?? 0;
                Platform = (RuntimePlatform)(frameData.GetProfilingSessionMetaDataLatest<int>(ProfilingSessionMetaDataEntry.RuntimePlatform) ?? -1);
                GraphicsDeviceType = (GraphicsDeviceType)(frameData.GetProfilingSessionMetaDataLatest<int>(ProfilingSessionMetaDataEntry.GraphicsDeviceType) ?? -1);
                TotalPhysicalMemory = frameData.GetProfilingSessionMetaDataLatest<ulong>(ProfilingSessionMetaDataEntry.TotalPhysicalMemory) ?? 0;
                TotalGraphicsMemory = frameData.GetProfilingSessionMetaDataLatest<ulong>(ProfilingSessionMetaDataEntry.TotalGraphicsMemory) ?? 0;
                ScriptingBackend = (ScriptingImplementation)(frameData.GetProfilingSessionMetaDataLatest<int>(ProfilingSessionMetaDataEntry.ScriptingBackend) ?? -1);
                TimeSinceStartup = frameData.GetProfilingSessionMetaDataLatest<double>(ProfilingSessionMetaDataEntry.TimeSinceStartup) ?? 0f;
                FrameCountSinceStartup = frameData.GetProfilingSessionMetaDataLatest<long>(ProfilingSessionMetaDataEntry.FrameCountSinceStartup) ?? 0;
                UnityVersion = frameData.GetProfilingSessionMetaDataStringLatest(ProfilingSessionMetaDataEntry.UnityVersion) ?? string.Empty;
                ProductName = frameData.GetProfilingSessionMetaDataStringLatest(ProfilingSessionMetaDataEntry.ProductName) ?? string.Empty;

                if (CaptureMetaDataVersion <= 1)
                    return;

                // For these newer entries, they should always be there if the version is new enough...
                // but check them, just in case.

                TimeOfRecordingNative = frameData.GetProfilingSessionMetaDataLatest<long>(ProfilingSessionMetaDataEntry.DateTimeOfRecording) ?? 0;
                DeviceModel = frameData.GetProfilingSessionMetaDataStringLatest(ProfilingSessionMetaDataEntry.DeviceModel) ?? string.Empty;
                DeviceSystemVersion = frameData.GetProfilingSessionMetaDataStringLatest(ProfilingSessionMetaDataEntry.DeviceSystemVersion) ?? string.Empty;;
            }
        }

        public bool ToFile(string path, int numFramesSaved)
        {
            path = Path.ChangeExtension(path, k_HighlightFileExtension);
            if (File.Exists(path))
            {
                // Update existing files if they exist.
                // Make sure it's actually a highlights file first.
                try
                {
                    using var fileStream = File.Open(path, FileMode.Open);
                    using (var bReader = new BinaryReader(fileStream))
                    {
                        if (bReader.ReadString() != k_HighlightsFileHeader)
                            throw new Exception("Invalid highlights file header");

                        // Find out the old version we're replacing.
                        HighlightsFileVersion = bReader.ReadInt32();

                        // Skip if we wouldn't be updating the existing file.
                        if (HighlightsFileVersion >= k_CurrentVersion)
                            return false;
                    }
                    File.Delete(path);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write highlights data to file \"{path}\": {e.Message}");
                    return false;
                }
            }

            // Query the profiler data for its (most recent) session metadata.
            // Right now we don't care about supporting multiple sessions' data being in one stream.
            UpdateHighlightsInfoFromMetadata();

            try
            {
                using var fileStream = File.Open(path, FileMode.Create);
                using (var bWriter = new BinaryWriter(fileStream))
                {
                    bWriter.Write(k_HighlightsFileHeader);
                    bWriter.Write(k_CurrentVersion);
                    bWriter.Write(NumberOfDataSeries);
                    // In case we care about this value in future, write out
                    // the "full" capacity the user had at the time.
                    bWriter.Write(DataSeriesCapacity);
                    // Don't save more frames than are available. This can happen if more than the minimum number of
                    // frames are recorded in the profiler, but then the frame count setting is reduced to below that:
                    // The profiler driver keeps the frames in memory, but the highlights bar doesn't.
                    if (numFramesSaved > DataSeriesCapacity)
                        numFramesSaved = DataSeriesCapacity;
                    var firstElement = DataSeriesCapacity - numFramesSaved;
                    DataSeriesCapacityThumbnail = numFramesSaved;
                    bWriter.Write(DataSeriesCapacityThumbnail);
                    bWriter.Write(BottleneckThreshold);
                    bWriter.Write(FirstFrameIndex);

                    for (var i = 0; i < NumberOfDataSeries; ++i)
                    {
                        int overCount = 0;
                        // Don't write out empty data, start at first actual frame
                        for (var j = firstElement; j < DataSeriesCapacity; ++j)
                        {
                            bWriter.Write(DataValueBuffers[i][j]);
                            if (DataValueBuffers[i][j] > BottleneckThreshold)
                                ++overCount;
                        }
                        PercentOverThreshold[i] = 100 * (overCount / (float)(DataSeriesCapacity - firstElement));
                        bWriter.Write(PercentOverThreshold[i]);
                    }

                    bWriter.Write(CaptureMetaDataVersion);
                    bWriter.Write(RuntimeSessionId);
                    bWriter.Write((int)Platform);
                    bWriter.Write((int)GraphicsDeviceType);
                    bWriter.Write(TotalPhysicalMemory);
                    bWriter.Write(TotalGraphicsMemory);
                    bWriter.Write((int)ScriptingBackend);
                    bWriter.Write(TimeSinceStartup);
                    bWriter.Write(FrameCountSinceStartup);
                    bWriter.Write(UnityVersion);
                    bWriter.Write(ProductName);
                    bWriter.Write(TimeOfRecordingNative);
                    bWriter.Write(DeviceModel);
                    bWriter.Write(DeviceSystemVersion);
                }
                CachedFilePath = path;

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write highlights data to file \"{path}\": {e.Message}");
            }

            return false;
        }

        public bool UpdateFromFile(string path)
        {
            path = Path.ChangeExtension(path, k_HighlightFileExtension);
            if (!File.Exists(path))
                return false;

            try
            {
                using var fileStream = File.Open(path, FileMode.Open);
                using (var bReader = new BinaryReader(fileStream))
                {
                    if (bReader.ReadString() != k_HighlightsFileHeader)
                        throw new Exception("Invalid highlights file header");

                    HighlightsFileVersion = bReader.ReadInt32();
                    if (HighlightsFileVersion < 1)
                        throw new Exception("Invalid highlights file header version");

                    NumberOfDataSeries = bReader.ReadInt32();
                    DataSeriesCapacity = bReader.ReadInt32();
                    DataSeriesCapacityThumbnail = bReader.ReadInt32();
                    BottleneckThreshold = bReader.ReadSingle();
                    FirstFrameIndex = bReader.ReadInt32();

                    for (var i = 0; i < NumberOfDataSeries; ++i)
                    {
                        DataValueBuffers[i].Dispose();
                        DataValueBuffers[i] = new NativeArray<float>(DataSeriesCapacityThumbnail, Allocator.Persistent);
                        for (var j = 0; j < DataSeriesCapacityThumbnail; ++j)
                            DataValueBuffers[i][j] = bReader.ReadSingle();
                        PercentOverThreshold[i] = bReader.ReadSingle();
                    }

                    if (HighlightsFileVersion > 1)
                    {
                        CaptureMetaDataVersion = bReader.ReadInt32();
                        RuntimeSessionId = bReader.ReadUInt32();
                        Platform = (RuntimePlatform)bReader.ReadInt32();
                        GraphicsDeviceType = (GraphicsDeviceType)bReader.ReadInt32();
                        TotalPhysicalMemory = bReader.ReadUInt64();
                        TotalGraphicsMemory = bReader.ReadUInt64();
                        ScriptingBackend = (ScriptingImplementation)bReader.ReadInt32();
                        TimeSinceStartup = bReader.ReadDouble();
                        FrameCountSinceStartup = bReader.ReadInt64();
                        UnityVersion = bReader.ReadString();
                        ProductName = bReader.ReadString();
                        TimeOfRecordingNative = bReader.ReadInt64();
                        DeviceModel = bReader.ReadString();
                        DeviceSystemVersion = bReader.ReadString();
                    }
                }
                CachedFilePath = path;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read highlights data from file \"{path}\": {e.Message}");
                return false;
            }

            return true;
        }

        // Return value: Whether BottleneckThreshold has changed or not
        public bool ChangeFPSTarget(int fpsValue)
        {
            var newThreshold = 1e9f / Math.Clamp(fpsValue,
                ProfilerUserSettings.k_MinimumTargetFramesPerSecond,
                ProfilerUserSettings.k_MaximumTargetFramesPerSecond);

            // return if not changed
            if (Mathf.Approximately(BottleneckThreshold, newThreshold))
                return false;

            BottleneckThreshold = newThreshold;

            if (!File.Exists(CachedFilePath))
                return false;

            try
            {
                using var fileStreamRead = File.Open(CachedFilePath, FileMode.Open, FileAccess.Read);
                using (var bReader = new BinaryReader(fileStreamRead))
                {
                    if (bReader.ReadString() != k_HighlightsFileHeader)
                        throw new Exception("Invalid highlights file header");

                    if (bReader.ReadInt32() < 1)
                        throw new Exception("Invalid highlights file header version");
                }

                using var fileStreamWrite = File.Open(CachedFilePath, FileMode.Open, FileAccess.Write);
                using (var bWriter = new BinaryWriter(fileStreamWrite))
                {
                    bWriter.Seek(32, SeekOrigin.Begin);
                    bWriter.Write(BottleneckThreshold);

                    for (var i = 0; i < NumberOfDataSeries; ++i)
                    {
                        int overCount = 0;
                        for (var j = 0; j < DataSeriesCapacityThumbnail; ++j)
                        {
                            if (DataValueBuffers[i][j] > BottleneckThreshold)
                                ++overCount;
                        }

                        PercentOverThreshold[i] = 100 * (overCount / (float)DataSeriesCapacityThumbnail);
                        // Percentage is at the end of each set of frame data:
                        // First 40 bytes are everything else we write at the start of the file.
                        var offSetDataSeries = (i + 1) * sizeof(float) * DataSeriesCapacityThumbnail;
                        var offsetPercentage = i * sizeof(float);
                        bWriter.Seek(40 + offSetDataSeries + offsetPercentage, SeekOrigin.Begin);
                        bWriter.Write(PercentOverThreshold[i]);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to update highlights file \"{CachedFilePath}\": {e.Message}");
                return false;
            }
            return true;
        }
    }
}
