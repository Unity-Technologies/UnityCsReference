// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewModelBuilder
    {
        const int k_NumberOfDataSeries = 2;
        const int k_FrameTimingManagerFixedDelay = 4;

        static readonly ProfilerCounterDescriptor[] k_CpuCounterDescriptors =
        {
            new("CPU Main Thread Active Time", ProfilerCategory.Scripts),
            new("CPU Render Thread Active Time", ProfilerCategory.Scripts),
        };
        static readonly ProfilerCounterDescriptor k_GpuFrameDurationCounterDescriptor = new("GPU Frame Time", ProfilerCategory.Render);

        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly UInt64 m_TargetFrameDurationNs;

        public BottlenecksChartViewModelBuilder(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            UInt64 targetFrameDurationNs)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_TargetFrameDurationNs = targetFrameDurationNs;
        }

        // Builds a new BottlenecksChartViewModel.
        public BottlenecksChartViewModel Build()
        {
            var dataSeriesCapacity = m_SettingsService.MaximumFrameCount;
            var colors = new[]
            {
                new Color(0.929f, 0.337f, 0.337f), // #ED5656
                new Color(0.929f, 0.906f, 0.337f), // #EDE756
            };
            var invalidColor = (EditorGUIUtility.isProSkin) ?
                new Color(0.078f, 0.078f, 0.078f) :
                new Color(0.247f, 0.247f, 0.247f);

            var model = new BottlenecksChartViewModel(
                k_NumberOfDataSeries,
                dataSeriesCapacity,
                colors,
                invalidColor,
                m_TargetFrameDurationNs);

            // Update the model with the latest capture data.
            UpdateModel(ref model);

            return model;
        }

        // Update an existing BottlenecksChartViewModel
        // TODO This can be improved to only fetch just the range of frames we don't already have in the buffers.
        public void UpdateModel(ref BottlenecksChartViewModel model)
        {
            // If there is no data available, clear the buffers and return early.
            if (m_DataService.FrameCount == 0)
            {
                for (var i = 0; i < model.NumberOfDataSeries; i++)
                {
                    var buffer = model.DataValueBuffers[i];
                    for (var j = 0; j < buffer.Length; j++)
                    {
                        buffer[j] = -1f;
                    }
                }

                model.FirstFrameIndex = m_DataService.FirstFrameIndex;

                return;
            }

            // We fill data value buffers to the end of the buffer - i.e. we offset the start so that the last
            // value will always be at the end of the buffer. This is to maintain the chart behaviours whereby
            // they always display the maximum frame count ('max history length'), even if there isn't data for
            // all frames, and they fill this space visually from the right-hand side.
            var firstDataValueIndex = model.DataSeriesCapacity - m_DataService.FrameCount;
            var firstFrameIndex = m_DataService.FirstFrameIndex;

            // To get CPU durations, we fetch both 'main thread duration' and 'render thread duration' from Frame Timing Manager and take whichever is longer.
            // To save on memory, we use the GPU durations buffer temporarily to hold the render thread durations.
            for (var i = 0; i < k_CpuCounterDescriptors.Length; i++)
            {
                var buffer = model.DataValueBuffers[i];
                var bufferRegion = buffer.AsSpan().Slice(firstDataValueIndex, m_DataService.FrameCount);

                var counterDescriptor = k_CpuCounterDescriptors[i];
                m_DataService.GetCounterValues(
                    counterDescriptor.CategoryName,
                    counterDescriptor.Name,
                    firstFrameIndex,
                    bufferRegion,
                    out _);
            }

            // Merge the 'main thread' and 'render thread' durations into one 'CPU duration' buffer by taking whichever is longer.
            var cpuMainThreadFrameDurations = model.DataValueBuffers[0];
            var cpuRenderThreadFrameDurations = model.DataValueBuffers[1];
            var cpuFrameDurations = model.DataValueBuffers[0];
            for (var i = firstDataValueIndex; i < model.DataSeriesCapacity; i++)
            {
                var cpuMainThreadFrameDuration = cpuMainThreadFrameDurations[i];
                var cpuRenderThreadFrameDuration = cpuRenderThreadFrameDurations[i];
                cpuFrameDurations[i] = Mathf.Max(cpuMainThreadFrameDuration, cpuRenderThreadFrameDuration);
            }

            // Fetch GPU frame durations.
            var frameTimingManagerFirstFrameIndex = firstFrameIndex + k_FrameTimingManagerFixedDelay;
            var gpuFrameDurationsBuffer = model.DataValueBuffers[1];
            var gpuFrameDurationsBufferRegion = gpuFrameDurationsBuffer.AsSpan().Slice(firstDataValueIndex, m_DataService.FrameCount);
            m_DataService.GetCounterValues(
                k_GpuFrameDurationCounterDescriptor.CategoryName,
                k_GpuFrameDurationCounterDescriptor.Name,
                frameTimingManagerFirstFrameIndex,
                gpuFrameDurationsBufferRegion,
                out _);

            // Update the first frame index to reflect the latest data stored in the buffers.
            var lastFrameIndex = (m_DataService.FirstFrameIndex + m_DataService.FrameCount) - 1;
            var firstFrameIndexInDataBuffers = lastFrameIndex - model.DataSeriesCapacity + 1;
            model.FirstFrameIndex = firstFrameIndexInDataBuffers;
        }
    }
}
