// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewModel : IDisposable
    {
        public BottlenecksChartViewModel(
            int numberOfDataSeries,
            int dataSeriesCapacity,
            Color[] colors,
            Color invalidColor,
            float bottleneckThreshold)
        {
            NumberOfDataSeries = numberOfDataSeries;
            DataSeriesCapacity = dataSeriesCapacity;

            DataValueBuffers = new NativeArray<float>[numberOfDataSeries];
            for (var i = 0; i < DataValueBuffers.Length; i++)
                DataValueBuffers[i] = new NativeArray<float>(dataSeriesCapacity, Allocator.Persistent);

            Colors = colors;
            InvalidColor = invalidColor;
            BottleneckThreshold = bottleneckThreshold;
        }

        // The number of data series on the chart.
        public int NumberOfDataSeries { get; }

        // The capacity of each data series on the chart.
        public int DataSeriesCapacity { get; }

        // The buffers of all data values for each data series.
        public NativeArray<float>[] DataValueBuffers { get; }

        // The colors for each data series.
        public Color[] Colors { get; }

        // The color for invalid data values in all data series.
        public Color InvalidColor { get; }

        // The value at which the data values are identified as a 'bottleneck'.
        public float BottleneckThreshold { get; set; }

        // The frame index of the first element in the data buffers.
        public int FirstFrameIndex { get; set; }

        public void Dispose()
        {
            for (var i = 0; i < DataValueBuffers.Length; i++)
                DataValueBuffers[i].Dispose();
        }
    }
}
