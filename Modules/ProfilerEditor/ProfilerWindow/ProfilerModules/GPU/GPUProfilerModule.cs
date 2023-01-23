// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    // TODO: refactor: rename to GpuProfilerModule
    // together with CPUOrGPUProfilerModule and CpuProfilerModule
    // in a PR that doesn't affect performance so that the sample names can be fixed as well without loosing comparability in Performance tests.
    [ProfilerModuleMetadata("GPU Usage", typeof(LocalizationResource), IconPath = "Profiler.GPU")]
    internal class GPUProfilerModule : CPUOrGPUProfilerModule
    {
        const string k_SettingsKeyPrefix = "Profiler.GPUProfilerModule.";
        protected override string SettingsKeyPrefix => k_SettingsKeyPrefix;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Hierarchy;

        static readonly string k_GpuProfilingDisabled = L10n.Tr("GPU Profiling was not enabled so no data was gathered.");

        static readonly string k_GpuProfilingNotSupportedWithEditorProfilingBefore2021_2 = L10n.Tr("GPU Profiling was not supported when profiling the Editor before 2021.2.");
        static readonly string k_GpuProfilingNotSupportedWithLegacyGfxJobs = L10n.Tr("GPU Profiling is currently not supported when using Graphics Jobs.");
        static readonly string k_GpuProfilingNotSupportedWithNativeGfxJobs = L10n.Tr("GPU Profiling is currently not supported when using Graphics Jobs.");
        static readonly string k_GpuProfilingNotSupportedByDevice = L10n.Tr("GPU Profiling is currently not supported by this device.");
        static readonly string k_GpuProfilingNotSupportedByGraphicsAPI = L10n.Tr("GPU Profiling is currently not supported by the used graphics API.");
        static readonly string k_GpuProfilingNotSupportedDueToFrameTimingStatsAndDisjointTimerQuery = L10n.Tr("GPU Profiling is currently not supported on this device when PlayerSettings.enableFrameTimingStats is enabled. (<a playersettingslink=\"Project/Player\" playersettingssearchstring=\"Frame Timing Stats\">Click here to edit</a>)");
        static readonly string k_GpuProfilingNotSupportedWithVulkan = L10n.Tr("GPU Profiling is currently not supported when using Vulkan.");
        static readonly string k_GpuProfilingNotSupportedWithMetal = L10n.Tr("GPU Profiling is currently not supported when using Metal.");
        static readonly string k_GpuProfilingNotSupportedWithOpenGLGPURecorders = L10n.Tr("GPU Profiling is currently not supported in OpenGL when PlayerSettings.\nenableOpenGLProfilerGPURecorders is enabled. (<a playersettingslink=\"Project/Player\" playersettingssearchstring=\"OpenGL: Profiler GPU Recorders\">Click here to edit</a>)");

        static readonly Dictionary<GpuProfilingStatisticsAvailabilityStates, string> s_StatisticsAvailabilityStateReason
            = new Dictionary<GpuProfilingStatisticsAvailabilityStates, string>()
            {
#pragma warning disable CS0618 // Type or member is obsolete
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithEditorProfiling , k_GpuProfilingNotSupportedWithEditorProfilingBefore2021_2},
#pragma warning restore CS0618 // Type or member is obsolete
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithLegacyGfxJobs , k_GpuProfilingNotSupportedWithLegacyGfxJobs},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithNativeGfxJobs , k_GpuProfilingNotSupportedWithNativeGfxJobs},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedByDevice , k_GpuProfilingNotSupportedByDevice},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedByGraphicsAPI , k_GpuProfilingNotSupportedByGraphicsAPI},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedDueToFrameTimingStatsAndDisjointTimerQuery , k_GpuProfilingNotSupportedDueToFrameTimingStatsAndDisjointTimerQuery},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithVulkan , k_GpuProfilingNotSupportedWithVulkan},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithMetal , k_GpuProfilingNotSupportedWithMetal},
            {GpuProfilingStatisticsAvailabilityStates.NotSupportedWithOpenGLGPURecorders , k_GpuProfilingNotSupportedWithOpenGLGPURecorders},
            };

        const int k_DefaultOrderIndex = 1;
        // ProfilerWindow exposes this as a const value via ProfilerWindow.gpuModuleName, so we need to define it as const.
        internal const string k_Identifier = "UnityEditorInternal.Profiling.GPUProfilerModule, UnityEditor.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";

        internal override ProfilerArea area => ProfilerArea.GPU;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartGPU";

        internal override ProfilerViewType ViewType
        {
            set
            {
                if (value == ProfilerViewType.Timeline)
                    throw new ArgumentException($"{DisplayName} does not implement a {nameof(ProfilerViewType.Timeline)} view.");
                CPUOrGPUViewTypeChanged(value);
            }
        }

        static string GetStatisticsAvailabilityStateReason(int statisticsAvailabilityState)
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
                                combinedReason += "\n\n" + s_StatisticsAvailabilityStateReason[currentBit];
                        }
                    }
                }
                s_StatisticsAvailabilityStateReason[state] = combinedReason;
            }
            return s_StatisticsAvailabilityStateReason[state];
        }

        internal override void OnEnable()
        {
            base.OnEnable();
            m_FrameDataHierarchyView.OnEnable(this, ProfilerWindow, true);
            m_FrameDataHierarchyView.dataAvailabilityMessage = null;
            if (m_ViewType == ProfilerViewType.Timeline)
                m_ViewType = ProfilerViewType.Hierarchy;
        }

        public override void DrawDetailsView(Rect position)
        {
            var selectedFrameIndex = (int)ProfilerWindow.selectedFrameIndex;
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
            base.DrawDetailsView(position);
        }

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.statisticsAvailabilityMessage = GetStatisticsAvailabilityStateReason;
            return chart;
        }

        private protected override bool ReadActiveState()
        {
            return SessionState.GetBool(activeStatePreferenceKey, false);
        }

        private protected override void SaveActiveState()
        {
            SessionState.SetBool(activeStatePreferenceKey, active);
        }

        private protected override void DeleteActiveState()
        {
            SessionState.EraseBool(activeStatePreferenceKey);
        }
    }
}
