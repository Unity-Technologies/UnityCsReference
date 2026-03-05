// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating two TopMarkersModel objects representing the top markers in a range, which are:
    //  - Top X markers by exclusive time across the range.
    //  - Top X markers by GC allocation across the range.
    // This is achieved by building the top markers in each frame and merging the results. The builder also
    // allows a user to obtain an additional TopMarkersModel representing the top markers in the longest frame
    // (by exclusive time) as soon as it is computed, via the BuildTopMarkersForLongestFrame property.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class TopRangeMarkersModelBuilder : TopMarkersModelBuilder
    {
        readonly Range m_FrameRange;
        readonly int m_LongestFrameIndex;
        readonly TaskCompletionSource<TopMarkersModel> m_BuildTopMarkersForLongestFrameCompletion;

        public TopRangeMarkersModelBuilder(
            IProfilerCaptureDataService dataService,
            uint numberOfTopMarkers,
            Range frameRange,
            int longestFrameIndex) : base(dataService, numberOfTopMarkers)
        {
            if (frameRange.Equals(Range.All))
            {
                // The current profiler API to obtain these can only be called on the main thread.
                // Therefore to work around this limitation, we access and cache what we need on
                // the main thread prior to dispatching the builder to background threads.
                // Naturally this means the builder must be constructed on the main thread.
                // Ideally we wouldn't have to do this if we could access Profiler API from a
                // background thread.
                frameRange = BuilderUtility.RangeForAllProfilerData(dataService);
            }

            m_FrameRange = frameRange;
            m_LongestFrameIndex = longestFrameIndex;
            m_BuildTopMarkersForLongestFrameCompletion = new();
        }

        // Await this task to get results for the longest frame as soon as it's built.
        public Task<TopMarkersModel> BuildTopMarkersForLongestFrame
        {
            get
            {
                return m_BuildTopMarkersForLongestFrameCompletion.Task;
            }
        }

        public async Task<Result> BuildAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                return TopMarkersInRangeByExclusiveTimeAndGCAllocation(cancellationToken);
            });
        }

        async Task<Result> TopMarkersInRangeByExclusiveTimeAndGCAllocation(CancellationToken cancellationToken)
        {
            var frameRange = m_FrameRange;
            var dataService = m_DataService;
            var numberOfTopMarkers = m_NumberOfTopMarkers;
            var longestFrameIndex = m_LongestFrameIndex;
            var tasks = new List<Task<Dictionary<int, CombinedMarkerData>>>();

            // Begin asynchronous tasks to compute each frame's combined marker data in parallel.
            var firstFrameIndex = frameRange.Start.Value;
            var frameCount = frameRange.End.Value - frameRange.Start.Value;
            Task<Dictionary<int, CombinedMarkerData>> buildCombinedMarkersInLongestFrame = null;
            for (int i = 0; i < frameCount; ++i)
            {
                var frameIndex = firstFrameIndex + i;
                var buildCombinedMarkersInFrame = Task.Run(() =>
                {
                    var combinedMarkersInFrame = ComputeCombinedExclusiveTimeAndGCAllocationForAllMarkersInFrame(
                        frameIndex,
                        cancellationToken);
                    return combinedMarkersInFrame;
                });
                tasks.Add(buildCombinedMarkersInFrame);

                // Store which task is building the longest frame so we can return those results separately.
                if (frameIndex == longestFrameIndex)
                    buildCombinedMarkersInLongestFrame = buildCombinedMarkersInFrame;
            }

            // Process each frame's results as its task completes.
            var topMarkersInRangeByExclusiveTime = new TopMarkersCollection(numberOfTopMarkers);
            var topMarkersInRangeByGCAllocation = new TopMarkersCollection(numberOfTopMarkers);
            while (tasks.Count > 0)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);

                // Check for cancellation after each builder completes.
                cancellationToken.ThrowIfCancellationRequested();

                // Add the combined markers for the frame to both top marker collections for the
                // range, if necessary, to build up the top X markers in the range. Multiple marker
                // instances across frames will not be combined; only multiple marker instances
                // within a frame are combined.
                var combinedMarkersInFrame = completedTask.Result;
                AddCombinedMarkersIntoCollectionsIfNecessary(
                    combinedMarkersInFrame,
                    ref topMarkersInRangeByExclusiveTime,
                    ref topMarkersInRangeByGCAllocation,
                    cancellationToken);

                // Report results for the longest frame separately.
                if (completedTask == buildCombinedMarkersInLongestFrame)
                {
                    var longestFrameResult = BuildTopMarkersModelsWithCombinedMarkers(
                        combinedMarkersInFrame,
                        dataService,
                        cancellationToken);
                    m_BuildTopMarkersForLongestFrameCompletion.SetResult(longestFrameResult.TopMarkersByExclusiveTime);
                }
            }

            // Build the two top marker models (exclusive time and GC allocation) from
            // the two final top marker collections.
            return BuildTopMarkersModelsFromCollections(
                dataService,
                topMarkersInRangeByExclusiveTime,
                topMarkersInRangeByGCAllocation);
        }
    }
}
