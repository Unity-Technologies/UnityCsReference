// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Composite builder for creating all models required for the range summary view.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class RangeSummaryModelBuilder
    {
        const uint k_NumberOfTopMarkers = 5;

        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public RangeSummaryModelBuilder(
            IProfilerCaptureDataService dataService,
            Range frameRange)
        {
            m_DataService = dataService;
            m_FrameRange = frameRange;
        }

        public async Task BuildAsync(
            CancellationToken cancellationToken,
            Action<RangeBottlenecksModel> OnRangeBottlenecksBuildCompleted,
            Action<SystemsImpactModel> OnSystemsImpactBuildCompleted,
            Action<BoxPlotModel> OnFrameTimesBuildCompleted,
            Action<TopMarkersModel> OnTopFrameMarkersBuildCompleted,
            Action<TopMarkersModel> OnTopRangeMarkersBuildCompleted,
            Action<GCAllocationsModel> OnGCAllocationsBuildCompleted,
            Action<TopMarkersModel> OnTopGCMarkersBuildCompleted,
            Action<BoxPlotModel> OnGCCollectBuildCompleted)
        {
            var dataService = m_DataService;
            var frameRange = m_FrameRange;

            // Verify the frame range is valid. When the profiler drops the currently
            // selected frame from its circular history buffer, it doesn't change the
            // selection. Therefore we need to test if the frame range is still valid.
            BuilderUtility.ThrowIfFrameRangeIsOutOfBounds(frameRange, dataService);

            var rangeBottlenecksBuilder = new RangeBottlenecksModelBuilder(dataService, frameRange);
            var buildRangeBottlenecks = rangeBottlenecksBuilder.BuildAsync(cancellationToken);

            var systemsImpactBuilder = new SystemsImpactInRangeModelBuilder(dataService, frameRange);
            var buildSystemsImpact = systemsImpactBuilder.BuildAsync(cancellationToken);

            var frameTimesBuilder = new FrameTimesBoxPlotModelBuilder(dataService, frameRange);
            var buildFrameTimes = frameTimesBuilder.BuildAsync(cancellationToken);

            var gcAllocationsBuilder = new GCAllocationsInRangeModelBuilder(dataService, frameRange);
            var buildGCAllocations = gcAllocationsBuilder.BuildAsync(cancellationToken);

            var gcCollectBuilder = new GCCollectBoxPlotModelBuilder(dataService, frameRange);
            var buildGCCollect = gcCollectBuilder.BuildAsync(cancellationToken);

            var tasks = new List<Task>()
            {
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildRangeBottlenecks,
                    cancellationToken,
                    OnRangeBottlenecksBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildSystemsImpact,
                    cancellationToken,
                    OnSystemsImpactBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildFrameTimes,
                    cancellationToken,
                    OnFrameTimesBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildGCAllocations,
                    cancellationToken,
                    OnGCAllocationsBuildCompleted),
                AwaitTaskThenCheckCancellationThenInvokeCompletion(
                    buildGCCollect,
                    cancellationToken,
                    OnGCCollectBuildCompleted)
            };
            await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();

            // Build top markers in range.
            var numberOfTopMarkers = k_NumberOfTopMarkers;
            var longestFrameIndex = buildFrameTimes.Result.Maximum.FrameIndex;
            var topRangeMarkersBuilder = new TopRangeMarkersModelBuilder(
                dataService,
                numberOfTopMarkers,
                frameRange,
                longestFrameIndex);
            var buildTopRangeMarkers = topRangeMarkersBuilder.BuildAsync(cancellationToken);

            // Await the top markers for the longest frame and invoke callback.
            var buildTopMarkersForLongestFrame = topRangeMarkersBuilder.BuildTopMarkersForLongestFrame;
            var topMarkersForLongestFrame = await buildTopMarkersForLongestFrame;
            OnTopFrameMarkersBuildCompleted.Invoke(topMarkersForLongestFrame);

            // Await the top markers for the full range and invoke callbacks.
            var topRangeMarkers = await buildTopRangeMarkers;
            OnTopRangeMarkersBuildCompleted.Invoke(topRangeMarkers.TopMarkersByExclusiveTime);
            OnTopGCMarkersBuildCompleted.Invoke(topRangeMarkers.TopMarkersByGCAllocation);
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
