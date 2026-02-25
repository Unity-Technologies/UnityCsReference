// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using static Unity.Profiling.Editor.UI.SystemsImpactModel;

namespace Unity.Profiling.Editor.UI
{
    // Builder for creating a SystemsImpactModel containing the top systems across a range, measured by their mean time.
    // Note: The constructor must be called on the main thread due to the limitations of the Profiler API.
    class SystemsImpactInRangeModelBuilder : SystemsImpactModelBuilder
    {
        readonly IProfilerCaptureDataService m_DataService;
        readonly Range m_FrameRange;

        public SystemsImpactInRangeModelBuilder(IProfilerCaptureDataService dataService, Range frameRange)
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

        public async Task<SystemsImpactModel> BuildAsync(
            CancellationToken cancellationToken)
        {
            var dataService = m_DataService;
            var frameCount = m_FrameRange.End.Value - m_FrameRange.Start.Value;
            var firstFrameIndex = m_FrameRange.Start.Value;
            var systemsCount = k_CpuLegacyStatisticNames.Length;

            var task = Task.Run(() =>
            {
                // Calculate sum of each system value across range.
                var systemValuesAccumulated = new long[systemsCount];
                for (var i = 0; i < frameCount; ++i)
                {
                    const int k_MainThreadIndex = 0;
                    var frameIndex = firstFrameIndex + i;
                    using var mainThreadData = dataService.GetRawFrameDataView(frameIndex, k_MainThreadIndex);
                    if (mainThreadData.valid == false)
                        break;

                    for (var j = 0; j < systemsCount; ++j)
                    {
                        var legacyStatisticName = k_CpuLegacyStatisticNames[j];
                        var legacyStatisticValue = GetLegacyStatisticValueAsFloat(mainThreadData, legacyStatisticName);
                        systemValuesAccumulated[j] += Convert.ToInt64(legacyStatisticValue);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Calculate mean value of each system over the range.
                var systemImpacts = new SystemImpact[systemsCount];
                for (var i = 0; i < systemsCount; ++i)
                {
                    var sum = systemValuesAccumulated[i];
                    var meanSystemValueNs = Convert.ToUInt64(sum / frameCount);
                    systemImpacts[i] = new SystemImpact(
                        k_CpuLegacyStatisticNames[i],
                        k_CpuLegacyStatisticColors[i],
                        k_CpuLegacyStatisticColorBlindColors[i],
                        meanSystemValueNs);
                }

                // Check for cancellation.
                cancellationToken.ThrowIfCancellationRequested();

                return BuildModelFromSystemImpacts(m_FrameRange, systemImpacts);
            });

            return await task;
        }
    }
}
