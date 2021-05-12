// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("UI Details", typeof(LocalizationResource), IconPath = "Profiler.UIDetails")]
    internal class UIDetailsProfilerModule : UIProfilerModule
    {
        const int k_DefaultOrderIndex = 11;
        const ProfilerModuleChartType k_ChartType = ProfilerModuleChartType.Line;

        public UIDetailsProfilerModule() : base(k_ChartType) {}

        internal override ProfilerArea area => ProfilerArea.UIDetails;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartUIDetails";

        UISystemProfilerChart UISystemProfilerChart => m_Chart as UISystemProfilerChart;

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            // [Coverity Defect 53724] Intentionally not calling the base class here to instantiate a custom chart type.
            m_Chart = new UISystemProfilerChart(k_ChartType, defaultChartScale, chartMaximumScaleInterpolationValue, m_LegacyChartCounters.Count, DisplayName, DisplayName, IconPath);
            return m_Chart;
        }
    }
}
