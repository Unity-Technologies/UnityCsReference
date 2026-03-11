// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a BoxPlotModel of all frame times in a range of profiler frames. Frame times are stored in nanoseconds.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class FrameTimesBoxPlotModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public FrameTimesBoxPlotModelBuilder(
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
                const int k_MainThreadIndex = 0;

                var dataService = m_DataService;
                var firstFrameIndex = m_FrameRange.Start.Value;
                var frameCount = m_FrameRange.End.Value - m_FrameRange.Start.Value;
                var frameDurations = new (ulong, int)[frameCount];
                for (var i = 0; i < frameCount; ++i)
                {
                    var frameIndex = firstFrameIndex + i;
                    using (var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex))
                        frameDurations[i] = (mainThreadData.frameTimeNs, frameIndex);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                Array.Sort(frameDurations, (a, b) => { return a.Item1.CompareTo(b.Item1); });

                return BoxPlotModelExtensions.BoxPlotModelFromSortedData(frameDurations, true);
            });

            return await task;
        }
    }
}
