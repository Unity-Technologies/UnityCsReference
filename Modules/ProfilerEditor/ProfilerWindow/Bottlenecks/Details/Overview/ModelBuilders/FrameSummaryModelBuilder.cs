// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Composite builder for creating all models required for the frame summary page.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class FrameSummaryModelBuilder
    {
        const uint k_NumberOfTopMarkers = 5;

        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public FrameSummaryModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task BuildAsync(
            CancellationToken cancellationToken,
            Action<PieChartModel> OnMainThreadUtilizationCompleted,
            Action<SystemsImpactModel> OnSystemsImpactBuildCompleted,
            Action<FrameBottlenecksModel> OnFrameBottlenecksBuildCompleted,
            Action<TopMarkersModel> OnTopFrameMarkersBuildCompleted,
            Action<FrameGCAllocationsModel> OnFrameGCAllocationsBuildCompleted,
            Action<TopMarkersModel> OnTopGCMarkersBuildCompleted,
            Action<FrameGCCollectModel> OnFrameGCCollectBuildCompleted)
        {
            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;
            var numberOfTopMarkers = k_NumberOfTopMarkers;

            // Verify the frame index is valid. When the profiler drops the currently
            // selected frame from its circular history buffer, it doesn't change the
            // selection. Therefore we need to test if the frame index is still valid.
            BuilderUtility.ThrowIfFrameIndexIsOutOfBounds(frameIndex, dataService);

            var mainThreadUtilizationBuilder = new MainThreadUtilizationPieChartModelBuilder(dataService, frameIndex);
            var buildMainThreadUtilization = mainThreadUtilizationBuilder.BuildAsync(cancellationToken);

            var systemsImpactBuilder = new SystemsImpactInFrameModelBuilder(dataService, frameIndex);
            var buildSystemsImpact = systemsImpactBuilder.BuildAsync(cancellationToken);

            var frameBottlenecksBuilder = new FrameBottlenecksModelBuilder(dataService, frameIndex);
            var buildFrameBottlenecks = frameBottlenecksBuilder.BuildAsync(cancellationToken);

            var topFrameMarkersBuilder = new TopFrameMarkersModelBuilder(
                dataService,
                numberOfTopMarkers,
                frameIndex);
            var buildTopFrameMarkers = topFrameMarkersBuilder.BuildAsync(cancellationToken);

            var gcAllocationsBuilder = new GCAllocationsInFrameModelBuilder(dataService, frameIndex);
            var buildFrameGCAllocations = gcAllocationsBuilder.BuildAsync(cancellationToken);

            var gcCollectBuilder = new FrameGCCollectModelBuilder(dataService, frameIndex);
            var buildFrameGCCollect = gcCollectBuilder.BuildAsync(cancellationToken);

            var tasks = new List<Task>()
            {
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildMainThreadUtilization,
                    cancellationToken,
                    OnMainThreadUtilizationCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildSystemsImpact,
                    cancellationToken,
                    OnSystemsImpactBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildFrameBottlenecks,
                    cancellationToken,
                    OnFrameBottlenecksBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildTopFrameMarkers,
                    cancellationToken,
                    (result) =>
                    {
                        OnTopFrameMarkersBuildCompleted?.Invoke(result.TopMarkersByExclusiveTime);
                        OnTopGCMarkersBuildCompleted?.Invoke(result.TopMarkersByGCAllocation);
                    }),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildFrameGCAllocations,
                    cancellationToken,
                    OnFrameGCAllocationsBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildFrameGCCollect,
                    cancellationToken,
                    OnFrameGCCollectBuildCompleted)
            };
            await Task.WhenAll(tasks);
        }

        async Task AwaitTaskThenCheckCancellationThenInvokeCompletion<T>(
            Task<T> task,
            CancellationToken cancellationToken,
            Action<T> completionHandler)
        {
            T result = await task;
            cancellationToken.ThrowIfCancellationRequested();
            completionHandler.Invoke(result);
        }
    }
}
