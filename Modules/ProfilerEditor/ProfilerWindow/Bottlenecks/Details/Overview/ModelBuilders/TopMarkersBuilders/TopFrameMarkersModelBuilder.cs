// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating two TopMarkersModel objects representing the top markers in a frame, which are:
    //  - Top X markers by exclusive time.
    //  - Top X markers by GC allocation.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class TopFrameMarkersModelBuilder : TopMarkersModelBuilder
    {
        readonly int m_FrameIndex;

        public TopFrameMarkersModelBuilder(
            IProfilerCaptureDataService dataService,
            uint numberOfTopMarkers,
            int frameIndex) : base(dataService, numberOfTopMarkers)
        {
            m_FrameIndex = frameIndex;
        }

        public async Task<Result> BuildAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() => TopMarkersInFrameByExclusiveTimeAndGCAllocation(cancellationToken), cancellationToken);
        }

        Result TopMarkersInFrameByExclusiveTimeAndGCAllocation(CancellationToken cancellationToken)
        {
            var frameIndex = m_FrameIndex;
            var dataService = m_DataService;

            var combinedMarkers = ComputeCombinedExclusiveTimeAndGCAllocationForAllMarkersInFrame(
                frameIndex,
                cancellationToken);

            return BuildTopMarkersModelsWithCombinedMarkers(combinedMarkers, dataService, cancellationToken);
        }
    }
}
