// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a BoxPlotModel of all 'GC Collect' marker times
    // in a range of profiler frames. Marker times are stored in nanoseconds.
    // Note: The constructor must be called on the main thread due to the
    // imitations of the Profiler API.
    class GCCollectBoxPlotModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public GCCollectBoxPlotModelBuilder(
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

        public async Task<BoxPlotModel> BuildAsync(CancellationToken cancellationToken)
        {
            var task = Task.Run(() =>
            {
                return BoxPlotModelWithGCCollectTimes(cancellationToken);
            });
            await task;

            cancellationToken.ThrowIfCancellationRequested();

            return await task;
        }

        BoxPlotModel BoxPlotModelWithGCCollectTimes(CancellationToken cancellationToken)
        {
            const string k_GCCollectMarkerName = "GC.Collect";

            var dataService = m_DataService;
            var firstFrameIndex = m_FrameRange.Start.Value;
            var frameCount = m_FrameRange.End.Value - m_FrameRange.Start.Value;
            var gcCollectMarkerId = FrameDataView.invalidMarkerId;
            var gcCollectTimes = new List<(ulong, int)>();
            for (var i = 0; i < frameCount; ++i)
            {
                var frameGCCollectTimeNs = 0UL;
                var frameIndex = firstFrameIndex + i;
                for (var threadIndex = 0; ; ++threadIndex)
                {
                    using var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex);

                    // Profiler API has no way to obtain thread count in a frame.
                    // This is the only way to know we have processed all threads.
                    if (threadData.valid == false)
                        break;

                    // Obtain the marker ID on the first iteration.
                    if (gcCollectMarkerId == FrameDataView.invalidMarkerId)
                    {
                        gcCollectMarkerId = threadData.GetMarkerId(k_GCCollectMarkerName);
                        if (gcCollectMarkerId == FrameDataView.invalidMarkerId)
                        {
                            // There were no GC.Collect markers in the entire capture.
                            return BoxPlotModel.Empty;
                        }
                    }

                    // Spin through all of the thread's markers, accumulating time spent in GC.Collect.
                    int sampleCount = threadData.sampleCount;
                    for (var sampleIndex = 0; sampleIndex < sampleCount; ++sampleIndex)
                    {
                        var markerId = threadData.GetSampleMarkerId(sampleIndex);
                        if (markerId == gcCollectMarkerId)
                        {
                            var gcCollectTimeNs = threadData.GetSampleTimeNs(sampleIndex);
                            frameGCCollectTimeNs += gcCollectTimeNs;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (frameGCCollectTimeNs != 0UL)
                    gcCollectTimes.Add((frameGCCollectTimeNs, frameIndex));
            }

            if (gcCollectTimes.Count == 0)
            {
                // There were no GC.Collect markers in the range.
                return BoxPlotModel.Empty;
            }

            var gcCollectTimesArray = gcCollectTimes.ToArray();
            Array.Sort(gcCollectTimesArray, (a, b) => { return a.Item1.CompareTo(b.Item1); });

            return BoxPlotModelExtensions.BoxPlotModelFromSortedData(gcCollectTimesArray);
        }
    }
}
