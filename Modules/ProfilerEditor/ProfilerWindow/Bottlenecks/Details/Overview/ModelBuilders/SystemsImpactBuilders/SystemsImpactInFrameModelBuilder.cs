// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using static Unity.Profiling.Editor.UI.SystemsImpactModel;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a SystemsImpactModel containing the top systems in a frame.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class SystemsImpactInFrameModelBuilder : SystemsImpactModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly int m_FrameIndex;

        public SystemsImpactInFrameModelBuilder(
            IProfilerCaptureDataService dataService,
            int frameIndex)
        {
            m_DataService = dataService;
            m_FrameIndex = frameIndex;
        }

        public async Task<SystemsImpactModel> BuildAsync(CancellationToken cancellationToken)
        {
            var dataService = m_DataService;
            var frameIndex = m_FrameIndex;
            var systemsCount = k_CpuLegacyStatisticNames.Length;

            var task = Task.Run(() =>
            {
                // Check for cancellation.
                cancellationToken.ThrowIfCancellationRequested();

                var systemImpacts = new SystemImpact[systemsCount];

                const int k_MainThreadIndex = 0;
                using var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex);
                if (!mainThreadData.valid)
                    return default;

                for (var i = 0; i < systemsCount; ++i)
                {
                    // Check for cancellation.
                    cancellationToken.ThrowIfCancellationRequested();

                    var legacyStatisticName = k_CpuLegacyStatisticNames[i];
                    var legacyStatisticValue = GetLegacyStatisticValueAsFloat(mainThreadData, legacyStatisticName);
                    systemImpacts[i] = new SystemImpact(
                        k_CpuLegacyStatisticNames[i],
                        k_CpuLegacyStatisticColors[i],
                        k_CpuLegacyStatisticColorBlindColors[i],
                        Convert.ToUInt64(legacyStatisticValue));
                }

                return BuildModelFromSystemImpacts(new Range(frameIndex, frameIndex), systemImpacts);
            }, cancellationToken);

            return await task;
        }
    }
}
