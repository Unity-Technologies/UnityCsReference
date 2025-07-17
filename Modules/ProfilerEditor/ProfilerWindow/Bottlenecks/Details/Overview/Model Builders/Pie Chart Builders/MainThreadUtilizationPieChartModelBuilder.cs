// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a PieChartModel of main thread utilization in  a single profiler frame.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class MainThreadUtilizationPieChartModelBuilder
    {
        static readonly Color k_ActiveColor = new(0.851f, 0.851f, 0.851f);
        static readonly Color k_WaitingColor = new(0.929f, 0.333f, 0.341f);

        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public MainThreadUtilizationPieChartModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task<PieChartModel> BuildAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(BuildMainThreadUtilizationPieChartModel);
            await task;

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        PieChartModel BuildMainThreadUtilizationPieChartModel()
        {
            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;

            var cpuMainThreadDurationNs = 0UL;
            var cpuMainThreadActiveDurationNs = 0UL;

            const int k_MainThreadIndex = 0;
            using (var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex))
            {
                if (mainThreadData.valid == false)
                    throw new ArgumentException("Invalid Profiler data.");

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
            return new PieChartModel(new PieChartModel.Segment[]
            {
                new(k_ActiveColor, k_ActiveName, Convert.ToUInt16(activePercentage)),
                new(k_WaitingColor, k_WaitingName, Convert.ToUInt16(waitingPercentage)),
            });
        }
    }
}
