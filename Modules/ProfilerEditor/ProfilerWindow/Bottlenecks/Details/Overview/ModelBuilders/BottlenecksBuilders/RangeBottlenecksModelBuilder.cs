// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a RangeBottlenecksModel.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class RangeBottlenecksModelBuilder : BottlenecksModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public RangeBottlenecksModelBuilder(
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

        public async Task<RangeBottlenecksModel> BuildAsync(CancellationToken cancellationToken)
        {
            var dataService = m_DataService;
            var frameCount = m_FrameRange.End.Value - m_FrameRange.Start.Value;
            var firstFrameIndex = m_FrameRange.Start.Value;
            var task = Task.Run(() =>
            {
                // Collect counter values for each frame.
                var cpuDurationsNs = new ulong[frameCount];
                var gpuDurationsNs = new ulong[frameCount];
                for (var i = 0; i < frameCount; ++i)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var frameIndex = firstFrameIndex + i;
                    var cpuGpuDurationsNs = GetCpuGpuDurationsNsForFrame(dataService, frameIndex);
                    cpuDurationsNs[i] = Convert.ToUInt64(cpuGpuDurationsNs.Item1);
                    gpuDurationsNs[i] = Convert.ToUInt64(cpuGpuDurationsNs.Item2);
                }

                return new RangeBottlenecksModel(
                    cpuDurationsNs,
                    gpuDurationsNs);
            }, cancellationToken);

            return await task;
        }
    }
}
