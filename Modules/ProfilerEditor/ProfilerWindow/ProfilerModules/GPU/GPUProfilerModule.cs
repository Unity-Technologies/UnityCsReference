// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class GPUProfilerModule : CPUorGPUProfilerModule
    {
        const string k_SettingsKeyPrefix = "Profiler.GPUProfilerModule.";
        protected override string SettingsKeyPrefix => k_SettingsKeyPrefix;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Hierarchy;

        static readonly string k_GpuProfilingDisabled = L10n.Tr("GPU Profiling was not enabled so no data was gathered.");
        static readonly string k_GpuProfilingEnabled = L10n.Tr("GPU Profiling was enabled but no data was gathered.");
        static readonly string k_GpuProfilingSupported = L10n.Tr("The Graphics API, driver, and device supported GPU Profiling.");

        static readonly string k_GpuProfilingNotSupportedWithEditorProfiling = L10n.Tr("GPU Profiling is currently not supported when profiling the Editor, try switching to Playmode.");
        static readonly string k_GpuProfilingNotSupportedWithLegacyGfxJobs = L10n.Tr("GPU Profiling is currently not supported when using Graphics Jobs.");
        static readonly string k_GpuProfilingNotSupportedWithNativeGfxJobs = L10n.Tr("GPU Profiling is currently not supported when using Graphics Jobs.");
        static readonly string k_GpuProfilingNotSupportedByDevice = L10n.Tr("GPU Profiling is currently not supported by this device.");
        static readonly string k_GpuProfilingNotSupportedByGraphicsAPI = L10n.Tr("GPU Profiling is currently not supported by the used graphics API.");
        static readonly string k_GpuProfilingNotSupportedDueToFrameTimingStatsAndDisjointTimerQuery = L10n.Tr("GPU Profiling is currently not supported on this device when PlayerSettings.enableFrameTimingStats is enabled.");
        static readonly string k_GpuProfilingNotSupportedWithVulkan = L10n.Tr("GPU Profiling is currently not supported when using Vulkan.");
        static readonly string k_GpuProfilingNotSupportedWithMetal = L10n.Tr("GPU Profiling is currently not supported when using Metal.");

        static readonly Dictionary<GpuProfilingStatisticsAvailabilityStates, string> s_StatisticsAvailabilityStateReason
            = new Dictionary<GpuProfilingStatisticsAvailabilityStates, string>()
            {
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithEditorProfiling , k_GpuProfilingNotSupportedWithEditorProfiling},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithLegacyGfxJobs , k_GpuProfilingNotSupportedWithLegacyGfxJobs},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithNativeGfxJobs , k_GpuProfilingNotSupportedWithLegacyGfxJobs},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedByDevice , k_GpuProfilingNotSupportedByDevice},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedByGraphicsAPI , k_GpuProfilingNotSupportedByGraphicsAPI},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedDueToFrameTimingStatsAndDisjointTimerQuery , k_GpuProfilingNotSupportedDueToFrameTimingStatsAndDisjointTimerQuery},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithVulkan , k_GpuProfilingNotSupportedWithVulkan},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithMetal , k_GpuProfilingNotSupportedWithMetal},
            };

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);
            m_FrameDataHierarchyView.OnEnable(this, profilerWindow, true);
            m_FrameDataHierarchyView.dataAvailabilityMessage = null;
            if (m_ViewType == ProfilerViewType.Timeline)
                m_ViewType = ProfilerViewType.Hierarchy;
        }

        public override void DrawView(Rect position)
        {
            int selectedFrameIndex = m_ProfilerWindow.GetActiveVisibleFrameIndex();
            if (selectedFrameIndex >= ProfilerDriver.firstFrameIndex && selectedFrameIndex <= ProfilerDriver.lastFrameIndex)
            {
                GpuProfilingStatisticsAvailabilityStates state = (GpuProfilingStatisticsAvailabilityStates)ProfilerDriver.GetGpuStatisticsAvailabilityState(selectedFrameIndex);

                if ((state & GpuProfilingStatisticsAvailabilityStates.Enabled) == 0)
                    m_FrameDataHierarchyView.dataAvailabilityMessage = k_GpuProfilingDisabled;
                else if ((state & GpuProfilingStatisticsAvailabilityStates.Gathered) == 0)
                    m_FrameDataHierarchyView.dataAvailabilityMessage = GetStatisticsAvailabilityStateReason((int)state);
                else
                    m_FrameDataHierarchyView.dataAvailabilityMessage = null;
            }
            else
                m_FrameDataHierarchyView.dataAvailabilityMessage = null;
            base.DrawView(position);
        }

        public static string GetStatisticsAvailabilityStateReason(int statisticsAvailabilityState)
        {
            GpuProfilingStatisticsAvailabilityStates state = (GpuProfilingStatisticsAvailabilityStates)statisticsAvailabilityState;

            if ((state & GpuProfilingStatisticsAvailabilityStates.Enabled) == 0)
                return null;

            if (!s_StatisticsAvailabilityStateReason.ContainsKey(state))
            {
                string combinedReason = "";
                for (int i = 0; i < sizeof(GpuProfilingStatisticsAvailabilityStates) * 8; i++)
                {
                    if ((statisticsAvailabilityState >> i & 1) != 0)
                    {
                        GpuProfilingStatisticsAvailabilityStates currentBit = (GpuProfilingStatisticsAvailabilityStates)(1 << i);
                        if (currentBit == GpuProfilingStatisticsAvailabilityStates.NotSupportedByGraphicsAPI
                            && ((state & GpuProfilingStatisticsAvailabilityStates.NotSupportedWithMetal) != 0
                                || (state & GpuProfilingStatisticsAvailabilityStates.NotSupportedWithVulkan) != 0
                            )
                        )
                            continue; // no need to war about the general case, when a more specific reason was given.
                        if (s_StatisticsAvailabilityStateReason.ContainsKey(currentBit))
                        {
                            if (string.IsNullOrEmpty(combinedReason))
                                combinedReason = s_StatisticsAvailabilityStateReason[currentBit];
                            else
                                combinedReason += '\n' + s_StatisticsAvailabilityStateReason[currentBit];
                        }
                    }
                }
                s_StatisticsAvailabilityStateReason[state] = combinedReason;
            }
            return s_StatisticsAvailabilityStateReason[state];
        }
    }
}
