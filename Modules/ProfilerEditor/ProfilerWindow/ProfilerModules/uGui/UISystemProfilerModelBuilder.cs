// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using Unity.Profiling.Editor;

namespace UnityEditor
{
    /// <summary>
    /// Model builder specialization for UGUI UI profiler.
    /// 
    /// It builds extra mini-model for UI events and adds extra
    /// track to chart's series. This extra track is only used
    /// for legend item visualization and series activation.
    /// 
    /// UI model is consumed by UI extra controller, which adds
    /// events on top of the normal chart view.
    /// </summary>
    internal class UISystemProfilerModelBuilder : ChartModelBuilder
    {
        // Fake series, needed for chart's legend view
        ChartSeriesViewData m_EventsSeries;
        UISystemProfilerModel m_UIModel;

        public UISystemProfilerModelBuilder(IProfilerPersistentSettingsService settingsService, ProfilerModuleChartType type, int seriesCount, string name, string localizedName, string tooltip, string iconName)
            : base(settingsService, type, seriesCount + 1, name, localizedName, tooltip, iconName)
        {
            m_UIModel = new UISystemProfilerModel();
        }

        public UISystemProfilerModel UIModel => m_UIModel;

        public override void ConfigureChartSeries(int historySize, ProfilerCounterDescriptor[] counters)
        {
            base.ConfigureChartSeries(historySize, counters);

            Model.series[counters.Length] = m_EventsSeries = new ChartSeriesViewData("Events", string.Empty, string.Empty, historySize, ProfilerColors.chartAreaColors[(uint)counters.Length % ProfilerColors.chartAreaColors.Length]);
        }

        public override void UpdateData(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            base.UpdateData(firstEmptyFrame, firstFrame, frameCount);

            m_UIModel.Events = null;
            m_UIModel.MarkerNames = null;
            int count = ProfilerDriver.GetUISystemEventMarkersCount(firstFrame, frameCount);
            if (count == 0)
                return;

            m_UIModel.ShowEvents = m_EventsSeries?.enabled ?? false;
            m_UIModel.EventsColor = m_EventsSeries.color;
            m_UIModel.Events = new EventMarker[count];
            m_UIModel.MarkerNames = new string[count];
            m_UIModel.DomainSize = Model.GetDataDomainLength();
            m_UIModel.DomainOffset = Model.chartDomainOffset;
            ProfilerDriver.GetUISystemEventMarkersBatch(firstFrame, frameCount, m_UIModel.Events, m_UIModel.MarkerNames);
        }

        public override void OnCountersEnableChange()
        {
            base.OnCountersEnableChange();
            if (m_EventsSeries != null)
                m_UIModel.ShowEvents = m_EventsSeries.enabled;
        }
    }
}
