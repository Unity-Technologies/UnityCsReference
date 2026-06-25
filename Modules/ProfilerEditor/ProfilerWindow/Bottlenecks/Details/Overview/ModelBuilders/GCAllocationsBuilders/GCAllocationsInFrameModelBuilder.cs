// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a FrameGCAllocationsModel summarizing all GC Allocations in a given frame.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class GCAllocationsInFrameModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public GCAllocationsInFrameModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task<FrameGCAllocationsModel> BuildAsync(CancellationToken cancellationToken)
        {
            var result = await Task.Run(() => BuildFrameGCAllocationsModel(cancellationToken), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        FrameGCAllocationsModel BuildFrameGCAllocationsModel(CancellationToken cancellationToken)
        {
            const string k_GCAllocMarkerName = "GC.Alloc";

            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;

            var gcAllocTotalCount = 0UL;
            var gcAllocTotalSize = 0L;
            var gcAllocMarkerId = FrameDataView.invalidMarkerId;
            for (var threadIndex = 0; ; ++threadIndex)
            {
                using var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex);

                // Profiler API has no way to obtain thread count in a frame.
                // This is the only way to know we have processed all threads.
                if (threadData.valid == false)
                    break;

                if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                {
                    gcAllocMarkerId = threadData.GetMarkerId(k_GCAllocMarkerName);
                    if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                    {
                        // There were no GC.Alloc markers in the entire capture.
                        return FrameGCAllocationsModel.Empty;
                    }
                }

                int sampleCount = threadData.sampleCount;
                for (var sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
                {
                    var markerId = threadData.GetSampleMarkerId(sampleIndex);
                    if (markerId == gcAllocMarkerId)
                    {
                        gcAllocTotalCount++;
                        if (threadData.GetSampleMetadataCount(sampleIndex) > 0)
                            gcAllocTotalSize += threadData.GetSampleMetadataAsLong(sampleIndex, 0);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            if (gcAllocTotalCount == 0)
            {
                // There were no GC.Alloc markers in the frame.
                return FrameGCAllocationsModel.Empty;
            }

            return new FrameGCAllocationsModel(frameIndex, gcAllocTotalCount, gcAllocTotalSize);
        }
    }
}
