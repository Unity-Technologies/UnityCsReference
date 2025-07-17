// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a FrameGCCollectModel for a given frame index.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class FrameGCCollectModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public FrameGCCollectModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task<FrameGCCollectModel> BuildAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                return BuildFrameGCCollectModel(cancellationToken);
            });
            await task;

            return await task;
        }

        FrameGCCollectModel BuildFrameGCCollectModel(CancellationToken cancellationToken)
        {
            const string k_GCCollectMarkerName = "GC.Collect";

            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;

            var gcCollectTotalTimeNs = 0UL;
            var gcCollectMarkerId = FrameDataView.invalidMarkerId;
            for (var threadIndex = 0; ; ++threadIndex)
            {
                using var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex);

                // Profiler API has no way to obtain thread count in a frame.
                // This is the only way to know we have processed all threads.
                if (threadData.valid == false)
                    break;

                if (gcCollectMarkerId == FrameDataView.invalidMarkerId)
                {
                    gcCollectMarkerId = threadData.GetMarkerId(k_GCCollectMarkerName);
                    if (gcCollectMarkerId == FrameDataView.invalidMarkerId)
                    {
                        // There were no GC.Collect markers in the entire capture.
                        break;
                    }
                }

                // Spin through all of the thread's markers, accumulating time spent in GC.Collect.
                int sampleCount = threadData.sampleCount;
                for (var sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
                {
                    var markerId = threadData.GetSampleMarkerId(sampleIndex);
                    if (markerId == gcCollectMarkerId)
                    {
                        gcCollectTotalTimeNs += threadData.GetSampleTimeNs(sampleIndex);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return new FrameGCCollectModel(gcCollectTotalTimeNs);
        }
    }
}
