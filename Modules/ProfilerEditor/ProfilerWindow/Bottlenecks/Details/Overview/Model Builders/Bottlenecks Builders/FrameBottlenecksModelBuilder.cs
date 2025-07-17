// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a FrameBottlenecksModel.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class FrameBottlenecksModelBuilder : BottlenecksModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public FrameBottlenecksModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task<FrameBottlenecksModel> BuildAsync(CancellationToken cancellationToken)
        {
            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;

            var task = Task.Run(() =>
            {
                var cpuGpuDurationsNs = GetCpuGpuDurationsNsForFrame(dataService, frameIndex);

                // Check for cancellation.
                cancellationToken.ThrowIfCancellationRequested();

                return new FrameBottlenecksModel(
                    cpuGpuDurationsNs.Item1,
                    cpuGpuDurationsNs.Item2);
            });

            return await task;
        }
    }
}
