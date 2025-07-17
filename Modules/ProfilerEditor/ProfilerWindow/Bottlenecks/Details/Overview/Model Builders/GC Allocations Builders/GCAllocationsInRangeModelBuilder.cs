// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a GCAllocationsModel summarizing all GC Allocations over a range of frames.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class GCAllocationsInRangeModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public GCAllocationsInRangeModelBuilder(
            IProfilerCaptureDataService dataService,
            Range frameRange)
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

            m_DataService = dataService;
            m_FrameRange = frameRange;
        }

        public async Task<GCAllocationsModel> BuildAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                return BuildGCAllocationsModel(cancellationToken);
            });
            await task;

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        GCAllocationsModel BuildGCAllocationsModel(CancellationToken cancellationToken)
        {
            const string k_GCAllocMarkerName = "GC.Alloc";

            var dataService = m_DataService;
            var firstFrameIndex = m_FrameRange.Start.Value;
            var frameCount = m_FrameRange.End.Value - m_FrameRange.Start.Value;
            var gcAllocMarkerId = FrameDataView.invalidMarkerId;

            var gcAllocTotalCalls = 0UL;
            var gcAllocMaximumCallsInFrame = 0UL;
            var gcAllocMaximumSizeInFrame = 0UL;
            var frameIndexOfMaximumCalls = 0;
            var frameIndexOfMaximumSize = 0;
            for (var i = 0; i < frameCount; ++i)
            {
                var gcAllocCallsInFrame = 0UL;
                var gcAllocSizeInFrame = 0UL;

                var frameIndex = firstFrameIndex + i;
                for (var threadIndex = 0; ; ++threadIndex)
                {
                    using var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex);

                    // Profiler API has no way to obtain thread count in a frame.
                    // This is the only way to know we have processed all threads.
                    if (threadData.valid == false)
                        break;

                    // Obtain the marker ID on the first iteration.
                    if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                    {
                        gcAllocMarkerId = threadData.GetMarkerId(k_GCAllocMarkerName);
                        if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                        {
                            // There were no GC.Alloc markers in the entire capture.
                            return GCAllocationsModel.Empty;
                        }
                    }

                    int sampleCount = threadData.sampleCount;
                    for (var sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
                    {
                        var markerId = threadData.GetSampleMarkerId(sampleIndex);
                        if (markerId == gcAllocMarkerId)
                        {
                            gcAllocCallsInFrame++;
                            if (threadData.GetSampleMetadataCount(sampleIndex) > 0)
                                gcAllocSizeInFrame += Convert.ToUInt64(threadData.GetSampleMetadataAsLong(sampleIndex, 0));
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                gcAllocTotalCalls += gcAllocCallsInFrame;
                if (gcAllocCallsInFrame > gcAllocMaximumCallsInFrame)
                {
                    gcAllocMaximumCallsInFrame = gcAllocCallsInFrame;
                    frameIndexOfMaximumCalls = frameIndex;
                }

                if (gcAllocSizeInFrame > gcAllocMaximumSizeInFrame)
                {
                    gcAllocMaximumSizeInFrame = gcAllocSizeInFrame;
                    frameIndexOfMaximumSize = frameIndex;
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            if (gcAllocTotalCalls == 0)
            {
                // There were no GC.Alloc markers in the range.
                return GCAllocationsModel.Empty;
            }

            return new GCAllocationsModel(
                gcAllocTotalCalls,
                gcAllocMaximumCallsInFrame,
                gcAllocMaximumSizeInFrame,
                frameIndexOfMaximumCalls,
                frameIndexOfMaximumSize);
        }
    }
}
