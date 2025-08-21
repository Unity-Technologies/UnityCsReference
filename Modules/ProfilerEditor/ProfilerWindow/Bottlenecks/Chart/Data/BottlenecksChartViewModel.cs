// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewModel : IDisposable
    {
        const int k_CurrentVersion = 1;
        const string k_HighlightsFileHeader = "HIGHLIGHTS_INFO";
        public const string k_HighlightFileExtension = ".highlights";

        public BottlenecksChartViewModel(
            int numberOfDataSeries,
            int dataSeriesCapacity,
            float bottleneckThreshold)
        {
            NumberOfDataSeries = numberOfDataSeries;
            DataSeriesCapacity = dataSeriesCapacity;

            DataValueBuffers = new NativeArray<float>[numberOfDataSeries];
            PercentOverThreshold = new float[numberOfDataSeries];
            for (var i = 0; i < DataValueBuffers.Length; i++)
            {
                PercentOverThreshold[i] = 0f;
                DataValueBuffers[i] = new NativeArray<float>(dataSeriesCapacity, Allocator.Persistent);
            }

            InvalidColor = EditorGUIUtility.isProSkin ? k_InvalidColorPro : k_InvalidColorNonPro;
            BottleneckThreshold = bottleneckThreshold;
        }

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
        public static Color InvalidColor { get; private set; }

        // The value at which the data values are identified as a 'bottleneck'.
        public float BottleneckThreshold { get; set; }

        // The frame index of the first element in the data buffers.
        public int FirstFrameIndex { get; set; }

        // The associated file on disk, if applicable
        string CachedFilePath { get; set; }

        public void Dispose()
        {
            foreach (NativeArray<float> dataBuffer in DataValueBuffers)
                dataBuffer.Dispose();
        }

        public void ToFile(string path, int numFramesSaved)
        {
            path = Path.ChangeExtension(path, k_HighlightFileExtension);
            if (File.Exists(path))
                return;

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
                }
                CachedFilePath = path;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write highlights data to file \"{path}\": {e.Message}");
            }
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

                    if (bReader.ReadInt32() < k_CurrentVersion)
                        return false;

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
                return true;

            try
            {
                using var fileStreamRead = File.Open(CachedFilePath, FileMode.Open, FileAccess.Read);
                using (var bReader = new BinaryReader(fileStreamRead))
                {
                    if (bReader.ReadString() != k_HighlightsFileHeader)
                        throw new Exception("Invalid highlights file header");

                    if (bReader.ReadInt32() < k_CurrentVersion)
                        return true;
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
            }
            return true;
        }
    }
}
