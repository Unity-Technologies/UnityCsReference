// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class GlobalIlluminationProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.GlobalIllumination";
        const int k_DefaultOrderIndex = 12;
        static readonly string k_UnLocalizedName = "Realtime GI";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);
        static readonly string k_RealtimeGINotSupported = LocalizationDatabase.GetLocalizedString("Realtime GI was not supported.");
        static readonly string k_RealtimeGINotEnabled = LocalizationDatabase.GetLocalizedString("Realtime GI was not enabled.");

        public GlobalIlluminationProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName, Chart.ChartType.StackedFill) {}

        public override ProfilerArea area => ProfilerArea.GlobalIllumination;
        public override bool usesCounters => false;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartGlobalIllumination";


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

        protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
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
            var selectedFrameIndex = (int)m_ProfilerWindow.selectedFrameIndex;
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
