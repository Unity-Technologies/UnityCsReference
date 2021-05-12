// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Realtime GI", typeof(LocalizationResource), IconPath = "Profiler.GlobalIllumination")]
    internal class GlobalIlluminationProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 12;
        static readonly string k_RealtimeGINotSupported = LocalizationDatabase.GetLocalizedString("Realtime GI was not supported.");
        static readonly string k_RealtimeGINotEnabled = LocalizationDatabase.GetLocalizedString("Realtime GI was not enabled.");

        public GlobalIlluminationProfilerModule() : base(ProfilerModuleChartType.StackedTimeArea) {}

        internal override ProfilerArea area => ProfilerArea.GlobalIllumination;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartGlobalIllumination";

        static string GetStatisticsAvailabilityStateReason(int statisticsAvailabilityState)
        {
            var state = (GlobalIlluminationProfilingStatisticsAvailabilityStates)statisticsAvailabilityState;

            if (state == 0)
                return null;

            if (!state.HasFlag(GlobalIlluminationProfilingStatisticsAvailabilityStates.GISupported))
                return k_RealtimeGINotSupported;

            if (!state.HasFlag(GlobalIlluminationProfilingStatisticsAvailabilityStates.GIEnabled))
                return k_RealtimeGINotEnabled;

            return null;
        }

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.statisticsAvailabilityMessage = GetStatisticsAvailabilityStateReason;
            return chart;
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            var selectedFrameIndex = (int)ProfilerWindow.selectedFrameIndex;
            if (selectedFrameIndex >= ProfilerDriver.firstFrameIndex && selectedFrameIndex <= ProfilerDriver.lastFrameIndex)
            {
                var state = (GlobalIlluminationProfilingStatisticsAvailabilityStates)ProfilerDriver.GetStatisticsAvailabilityState(ProfilerArea.GlobalIllumination, selectedFrameIndex);
                if (state.HasFlag(GlobalIlluminationProfilingStatisticsAvailabilityStates.DataGathered))
                {
                    DrawDetailsViewText(position);
                }
                else
                {
                    GUILayout.Label(GetStatisticsAvailabilityStateReason((int)state));
                }
            }
        }
    }
}
