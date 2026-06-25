// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Accessibility;
using UnityEngine;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a PieChartModel of main thread utilization in  a single profiler frame.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class MainThreadUtilizationPieChartModelBuilder
    {
        // Indices for the pie chart segments, kept dedicated to Active/Waiting rather than reusing
        // the CPU/GPU bottleneck indices (Waiting is not GPU time). Passed via Segment.DataSeriesIndex
        // so the view controller can re-resolve segment colours when the colour-blind setting changes.
        public const int ActiveDataSeriesIndex = 0;
        public const int WaitingDataSeriesIndex = 1;

        // Active stays neutral (light gray) regardless of accessibility setting. Waiting uses a
        // warning-style colour with a dedicated colour-blind-safe variant.
        static readonly Color k_ActiveColor = new(0.851f, 0.851f, 0.851f);
        static readonly Color k_WaitingColor = new(0.929f, 0.337f, 0.337f);
        static readonly Color k_WaitingColorBlindSafeColor = new(0.863f, 0.149f, 0.494f);

        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public MainThreadUtilizationPieChartModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        // Resolves a segment index to its current display colour. Reads the colour-blind setting,
        // so callers must invoke this on the main thread.
        public static Color GetColorForDataSeries(int dataSeriesIndex)
        {
            switch (dataSeriesIndex)
            {
                case ActiveDataSeriesIndex:
                    return k_ActiveColor;
                case WaitingDataSeriesIndex:
                    return (UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default)
                        ? k_WaitingColor
                        : k_WaitingColorBlindSafeColor;
                default:
                    Debug.LogWarning($"{nameof(MainThreadUtilizationPieChartModelBuilder)}.{nameof(GetColorForDataSeries)}: invalid data series index {dataSeriesIndex}.");
                    return Color.magenta;
            }
        }

        public async Task<PieChartModel> BuildAsync(CancellationToken cancellationToken)
        {
            var result = await Task.Run(() => BuildMainThreadUtilizationPieChartModel(cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        PieChartModel BuildMainThreadUtilizationPieChartModel(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;

            var cpuMainThreadDurationNs = 0UL;
            var cpuMainThreadActiveDurationNs = 0UL;

            const int k_MainThreadIndex = 0;
            using (var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex))
            {
                if (mainThreadData.valid == false)
                    return default;

                cpuMainThreadDurationNs = mainThreadData.frameTimeNs;

                // When the frame hasn't completed, the main thread's duration is zero.
                if (cpuMainThreadDurationNs == 0UL)
                    return default;

                const string k_MainThreadActiveTimeCounterName = "CPU Main Thread Active Time";
                var markerId = mainThreadData.GetMarkerId(k_MainThreadActiveTimeCounterName);
                if (markerId == FrameDataView.invalidMarkerId)
                    return default;

                var value = mainThreadData.GetCounterValueAsLong(markerId);
                cpuMainThreadActiveDurationNs = Convert.ToUInt64(value);
            }

            var activePercentage = ((float)cpuMainThreadActiveDurationNs / cpuMainThreadDurationNs) * 100f;
            var waitingPercentage = 100f - activePercentage;

            const string k_ActiveName = "Active";
            const string k_WaitingName = "Waiting";
            // The colours below are placeholders: PieChartViewController re-resolves them on the
            // main thread via the supplied colour resolver before rendering, which lets the chart
            // pick up the current colour-blind setting. Using the static constants directly here
            // (instead of GetColorForDataSeries) keeps this method safe to run on a background
            // thread, since it avoids touching UserAccessiblitySettings off the main thread.
            return new PieChartModel(new PieChartModel.Segment[]
            {
                new(k_ActiveColor, k_ActiveName, Convert.ToUInt16(activePercentage), ActiveDataSeriesIndex),
                new(k_WaitingColor, k_WaitingName, Convert.ToUInt16(waitingPercentage), WaitingDataSeriesIndex),
            });
        }
    }
}
