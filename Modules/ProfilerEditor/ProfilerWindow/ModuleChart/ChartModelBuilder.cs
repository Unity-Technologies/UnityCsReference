// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Profiling.Editor
{
    internal class ChartModelBuilder
    {
        public const int k_MaximumSeriesCount = 10;
        public const float k_ChartMinClamp = 110.0f;
        public const float k_ChartMaxClamp = 70000.0f;

        const string k_ProfilerChartSettingsPreferenceKeyFormat = "ProfilerChart.{0}.ChartSettings";
        static readonly ProfilerMarker k_UpdateData = new($"{nameof(ChartModelBuilder)}.UpdateData");

        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerModuleChartType m_ChartType;
        readonly ChartModel m_Model;
        readonly ChartSeriesViewData[] m_Series;
        readonly float m_DataScale;

        // Preferences settings key
        string m_ModelKey;
        // Model profiling area, only defined for legacy modules
        // Is null for modules using counters
        ProfilerArea? m_Area;
        // Interpolation value for scale animation
        float m_MaximumScaleInterpolationValue;

        string ChartSettingsPreferenceKey => string.Format(k_ProfilerChartSettingsPreferenceKeyFormat, m_ModelKey);

        public ChartModelBuilder(IProfilerPersistentSettingsService settingsService, ProfilerModuleChartType chartType, int seriesCount, string name, string localizedName, string iconName)
        {
            Debug.Assert(seriesCount <= k_MaximumSeriesCount);

            m_SettingsService = settingsService;
            m_ChartType = chartType;

            m_ModelKey = name;
            m_Area = null;

            var isStackedTimeAreaChartType = m_ChartType == ProfilerModuleChartType.StackedTimeArea;
            m_DataScale = (isStackedTimeAreaChartType) ? 0.001f : 1f;
            m_MaximumScaleInterpolationValue = (isStackedTimeAreaChartType) ? -1f : 0f;

            var localizedTooltipFormat = LocalizationDatabase.GetLocalizedString("A chart showing performance counters related to '{0}'.");
            m_Model = new ChartModel
            {
                Tooltip = string.Format(localizedTooltipFormat, localizedName),
                Header = localizedName,
                HeaderIconName = iconName
            };
            m_Series = new ChartSeriesViewData[seriesCount];

            ShowGrid = (chartType == ProfilerModuleChartType.StackedTimeArea);
        }

        public ChartModel Model => m_Model;

        public bool HasOverlay { get; set; }
        public bool ShowGrid { get; set; }

        public void SetArea(ProfilerArea area)
        {
            m_Area = area != ProfilerModule.k_InvalidProfilerArea ? area : null;
        }

        public void SetName(string name)
        {
            m_ModelKey = name;
            m_Model.Header = name;

            SetChartSettingsNameAndUpdateAllPreferences(ChartSettingsPreferenceKey);
        }

        public void Update(long selectedFrameIndex)
        {
            LoadAndBindSettings();

            int frameCount = ProfilerUserSettings.frameCount;
            int firstEmptyFrame = ProfilerDriver.lastFrameIndex + 1 - ProfilerUserSettings.frameCount;
            int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);
            UpdateData(firstEmptyFrame, firstFrame, frameCount);
            UpdateOverlayData(firstEmptyFrame);
            UpdateScaleValuesIfNecessary(firstEmptyFrame, firstFrame, frameCount);
            UpdateSelectedData(selectedFrameIndex);
        }

        public void LoadAndBindSettings()
        {
            // Use the legacy preference key to maintain user settings on built-in modules.
            LoadChartsSettings(m_Model);
        }

        public void DeleteSettings()
        {
            m_SettingsService.ChartCountersOrder(ChartSettingsPreferenceKey).Delete();
        }

        public virtual void UpdateData(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            using var _ = k_UpdateData.Auto();

            // Clear the data available buffer prior to iterating over chart series.
            if (m_Series.Length > 0)
                m_Model.ClearDataAvailableBuffer();

            // Iterate over all chart series pulling counter data.
            float totalMaxValue = 1;
            for (int i = 0, count = m_Series.Length; i < count; ++i)
            {
                float maxValue;
                if (m_Area == null)
                {
                    // Retrieve counter values by category name & counter name.
                    ProfilerDriver.GetCounterValuesWithAvailabilityBatchByCategory(m_Series[i].category, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, m_Model.dataAvailable, out maxValue);
                }
                else
                {
                    // Retrieve counter values by area & counter name.
                    ProfilerDriver.GetCounterValuesWithAvailabilityBatch(m_Area.Value, m_Series[i].name, firstEmptyFrame, 1.0f, m_Series[i].yValues, m_Model.dataAvailable, out maxValue);
                }

                m_Series[i].yScale = m_DataScale;
                maxValue *= m_DataScale;

                // Minimum size so we don't generate nans during drawing
                maxValue = Mathf.Max(maxValue, 0.0001F);

                if (maxValue > totalMaxValue)
                    totalMaxValue = maxValue;

                if (m_ChartType == ProfilerModuleChartType.Line)
                {
                    // Scale line charts so they never hit the top. Scale them slightly differently for each line
                    // so that in "no stuff changing" case they will not end up being exactly the same.
                    maxValue *= (1.05f + i * 0.05f);
                    m_Series[i].rangeAxis = new Vector2(0f, maxValue);
                }
            }

            m_Model.Assign(m_Series, firstEmptyFrame, firstFrame);
        }

        public void UpdateSelectedData(long selectedFrame)
        {
            var domain = m_Model.GetDataDomain();
            var domainSpan = (int)(domain.y - domain.x);

            string[] labels = new string[m_Series.Length];
            for (int s = 0; s < m_Series.Length; s++)
            {
                string name = m_Model.hasOverlay ?
                    "Selected" + m_Series[s].name :
                    m_Series[s].name;

                if (m_Area == null)
                {
                    labels[s] = ProfilerDriver.GetFormattedCounterValue((int)selectedFrame, m_Series[s].category, name);
                }
                else
                {
                    labels[s] = ProfilerDriver.GetFormattedCounterValue((int)selectedFrame, m_Area.Value, name);
                }
            }
            m_Model.AssignSelectedLabels(labels);
        }

        public void UpdateOverlayData(int firstEmptyFrame)
        {
            m_Model.hasOverlay = HasOverlay;
            if (!HasOverlay)
                return;

            int numCharts = m_Model.numSeries;
            for (int i = 0; i < numCharts; ++i)
            {
                var chart = m_Model.series[i];
                var length = chart.yValues.Length;
                if (m_Model.overlays[i] == null || m_Model.overlays[i].yValues.Length != length)
                {
                    m_Model.overlays[i] = new ChartSeriesViewData(chart.name, chart.category, length, chart.color);
                }
                ProfilerDriver.GetCounterValuesBatch(ProfilerArea.CPU, string.Format("Selected{0}", chart.name), firstEmptyFrame, 1.0f, m_Model.overlays[i].yValues, out float maxValue);
                m_Model.overlays[i].yScale = m_DataScale;
            }
        }

        public void UpdateScaleValuesIfNecessary(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            if (m_ChartType.IsStackedChartType())
            {
                ComputeChartScaleValue(firstEmptyFrame, firstFrame, frameCount);
            }
        }

        public void ComputeChartScaleValue(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            float timeMax = 0.0f;
            float timeMaxExcludeFirst = 0.0f;
            // TODO: optimze this. it's not terrible but not cache friendly either and the yValues are the once needed to be accessed more often than the series
            // This takes up som 1 ms in deep profiling with 300 frames and likely scales badly with more
            for (int k = 0; k < frameCount; k++)
            {
                float timeNow = 0.0F;
                for (int j = 0; j < m_Series.Length; j++)
                {
                    var series = m_Series[j];

                    if (series.enabled)
                        timeNow += series.yValues[k];
                }
                if (timeNow > timeMax)
                    timeMax = timeNow;
                if (timeNow > timeMaxExcludeFirst && k + firstEmptyFrame >= firstFrame + 1)
                    timeMaxExcludeFirst = timeNow;
            }
            if (timeMaxExcludeFirst != 0.0f)
                timeMax = timeMaxExcludeFirst;

            if (m_ChartType == ProfilerModuleChartType.StackedTimeArea)
                timeMax = Mathf.Clamp(timeMax * m_DataScale, k_ChartMinClamp, k_ChartMaxClamp);

            // Do not apply the new scale immediately, but gradually go towards it
            if (m_MaximumScaleInterpolationValue > 0.0f)
                timeMax = Mathf.Lerp(m_MaximumScaleInterpolationValue, timeMax, 0.4f);
            m_MaximumScaleInterpolationValue = timeMax;

            for (int k = 0; k < m_Model.numSeries; ++k)
                m_Model.series[k].rangeAxis = new Vector2(0f, timeMax);
            m_Model.UpdateChartGrid(timeMax, ShowGrid);
        }

        public virtual void ConfigureChartSeries(int historySize, ProfilerCounterDescriptor[] counters)
        {
            var chartAreaColors = ProfilerColors.chartAreaColors;
            for (int i = 0; i < counters.Length; ++i)
            {
                var counter = counters[i];
                var category = counter.CategoryName;
                m_Series[i] = new ChartSeriesViewData(counter.Name, category, historySize, chartAreaColors[i % chartAreaColors.Length]);
            }

            // Allocate dataAvailable array for chart.
            m_Model.dataAvailable = new int[historySize];
            m_Model.Assign(m_Series, 0, -1);
        }

        public void ResetChartState()
        {
            m_MaximumScaleInterpolationValue = 0;
        }

        void LoadChartsSettings(ChartModel cdata)
        {
            var str = m_SettingsService.ChartCountersOrder(ChartSettingsPreferenceKey).Get();
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    var values = str.Split(',');
                    if (values.Length == cdata.numSeries)
                    {
                        for (var i = 0; i < cdata.numSeries; i++)
                            cdata.order[i] = int.Parse(values[i]);
                    }
                }
                catch (FormatException) { }
            }

            str = m_SettingsService.ChartCountersVisible(ChartSettingsPreferenceKey).Get();
            if (!string.IsNullOrEmpty(str))
            {
                for (var i = 0; i < cdata.numSeries; i++)
                {
                    if (i < str.Length && str[i] == '0')
                        cdata.series[i].enabled = false;
                }
            }
        }

        void SetChartSettingsNameAndUpdateAllPreferences(string name)
        {
            m_SettingsService.ChartCountersOrder(ChartSettingsPreferenceKey).Rename(name);
            m_SettingsService.ChartCountersVisible(ChartSettingsPreferenceKey).Rename(name);
        }

        public void OnCountersOrderChange()
        {
            var str = string.Empty;
            for (var i = 0; i < m_Model.numSeries; i++)
            {
                if (str.Length != 0)
                    str += ",";
                str += m_Model.order[i];
            }

            m_SettingsService.ChartCountersOrder(ChartSettingsPreferenceKey).Set(str);
        }

        public virtual void OnCountersEnableChange()
        {
            var str = string.Empty;
            for (var i = 0; i < m_Model.numSeries; i++)
                str += m_Model.series[i].enabled ? '1' : '0';

            m_SettingsService.ChartCountersVisible(ChartSettingsPreferenceKey).Set(str);
        }
    }
}
