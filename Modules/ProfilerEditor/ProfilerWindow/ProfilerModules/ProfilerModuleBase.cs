// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using System.Globalization;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal abstract class ProfilerModuleBase : ProfilerModule
    {
        const string k_NoCategory = "NoCategory";

        protected List<ProfilerCounterData> m_LegacyChartCounters;
        protected List<ProfilerCounterData> m_LegacyDetailCounters;
        [SerializeField] protected Vector2 m_PaneScroll;

        // We cannot provide counters in the constructor for legacy modules. All concrete module types now use a parameterless constructor that invokes this base constructor, causing this to be called in serialization. Access to ProfilerDriver.GetGraphStatisticsPropertiesForArea is banned during (de)serialization, which is used by these modules to construct their counter lists. Therefore, we initialize with a null list and provide it later in Initialize().
        protected ProfilerModuleBase(ProfilerModuleChartType defaultChartType = ProfilerModuleChartType.Line) : base(null, defaultChartType) {}

        internal override void LegacyModuleInitialize()
        {
            base.LegacyModuleInitialize();

            // Construct legacy counter lists.
            m_LegacyChartCounters = CollectDefaultChartCounters();
            if (m_LegacyChartCounters.Count > maximumNumberOfChartCounters)
            {
                throw new ArgumentException($"Chart counters cannot contain more than {maximumNumberOfChartCounters} counters.");
            }
            m_LegacyDetailCounters = CollectDefaultDetailCounters();

            InternalUpdateBaseCountersAndAutoEnabledCategoryNames();
        }

        public ReadOnlyCollection<ProfilerCounterData> chartCounters => m_LegacyChartCounters.AsReadOnly();
        public ReadOnlyCollection<ProfilerCounterData> detailCounters => m_LegacyDetailCounters.AsReadOnly();

        // Modules that use legacy stats override `usesCounters` to use the legacy GetGraphStatisticsPropertiesForArea functionality instead of Profiler Counters.
        public virtual bool usesCounters => true;

        private protected override string activeStatePreferenceKey
        {
            get
            {
                if (!string.IsNullOrEmpty(legacyPreferenceKey))
                {
                    // Use the legacy preference key to maintain user settings on built-in modules.
                    return legacyPreferenceKey;
                }

                return base.activeStatePreferenceKey;
            }
        }

        int maximumNumberOfChartCounters
        {
            get => ProfilerChart.k_MaximumSeriesCount;
        }

        public virtual void DrawToolbar(Rect position) {}

        public virtual void DrawDetailsView(Rect position) {}

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new LegacyDetailsViewController(ProfilerWindow, this);
        }

        public void SetCounters(List<ProfilerCounterData> chartCounters, List<ProfilerCounterData> detailCounters)
        {
            if (chartCounters.Count > maximumNumberOfChartCounters)
            {
                throw new ArgumentException($"Chart counters cannot contain more than {maximumNumberOfChartCounters} counters.");
            }

            if (active)
            {
                // Decrement existing areas prior to updating counters.
                var previousAreas = GetProfilerAreas();
                ProfilerWindow.SetAreasInUse(previousAreas, false);
            }

            // Capture each chart counter's enabled state prior to assignment.
            Dictionary<ProfilerCounterData, bool> counterEnabledStates = null;
            if (m_Chart != null)
            {
                counterEnabledStates = new Dictionary<ProfilerCounterData, bool>();
                for (int i = 0; i < m_LegacyChartCounters.Count; ++i)
                {
                    var counter = m_LegacyChartCounters[i];
                    var enabled = m_Chart.m_Series[i].enabled;
                    counterEnabledStates.Add(counter, enabled);
                }
            }

            m_LegacyChartCounters = chartCounters;
            m_LegacyDetailCounters = detailCounters;
            InternalUpdateBaseCountersAndAutoEnabledCategoryNames();
            if (Chart != null)
                RebuildChart();

            // Restore each chart counter's enabled state after assignment.
            if (counterEnabledStates != null && counterEnabledStates.Count > 0)
            {
                for (int i = 0; i < m_LegacyChartCounters.Count; ++i)
                {
                    var counter = m_LegacyChartCounters[i];

                    bool enabled;
                    if (!counterEnabledStates.TryGetValue(counter, out enabled))
                    {
                        // A new counter is enabled by default.
                        enabled = true;
                    }

                    m_Chart.m_Series[i].enabled = enabled;
                }
            }

            if (active)
            {
                // Increment new areas after updating counters.
                var areas = GetProfilerAreas();
                ProfilerWindow.SetAreasInUse(areas, true);
            }
        }

        /// <summary>
        /// Override this method to customize the text displayed in the module's details view.
        /// </summary>
        protected virtual string GetDetailsViewText()
        {
            string detailsViewText = (usesCounters) ? ConstructTextSummaryFromDetailCounters() : ProfilerDriver.GetOverviewText(area, ProfilerWindow.GetActiveVisibleFrameIndex());
            return detailsViewText;
        }

        /// <summary>
        /// Override this method to provide the module's default chart counters. Modules that define a ProfilerArea, and therefore use legacy stats instead of counters, do not need to override this method.
        /// </summary>
        protected virtual List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var chartCounters = new List<ProfilerCounterData>();
            if (!usesCounters)
            {
                var legacyStats = ProfilerDriver.GetGraphStatisticsPropertiesForArea(area);
                foreach (var legacyStatName in legacyStats)
                {
                    var counter = new ProfilerCounterData()
                    {
                        m_Category = k_NoCategory,
                        m_Name = legacyStatName,
                    };
                    chartCounters.Add(counter);
                }
            }

            return chartCounters;
        }

        /// <summary>
        /// Override this method to provide the module's default detail counters.
        /// </summary>
        protected virtual List<ProfilerCounterData> CollectDefaultDetailCounters()
        {
            return new List<ProfilerCounterData>();
        }

        protected void DrawEmptyToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawDetailsViewText(Rect position)
        {
            string activeText = GetDetailsViewText();
            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            GUILayout.EndScrollView();
        }

        protected static long GetCounterValue(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return -1;

            return frameData.GetCounterValueAsLong(id);
        }

        protected static string GetCounterValueAsBytes(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return "N/A";

            return EditorUtility.FormatBytes(frameData.GetCounterValueAsLong(id));
        }

        protected static string GetCounterValueAsNumber(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return "N/A";

            return FormatNumber(frameData.GetCounterValueAsLong(id));
        }

        protected static string FormatNumber(long num)
        {
            if (num < 1000)
                return num.ToString(CultureInfo.InvariantCulture.NumberFormat);
            if (num < 1000000)
                return (num * 0.001).ToString("f1", CultureInfo.InvariantCulture.NumberFormat) + "k";
            return (num * 0.000001).ToString("f1", CultureInfo.InvariantCulture.NumberFormat) + "M";
        }

        string ConstructTextSummaryFromDetailCounters()
        {
            string detailsViewText = null;
            using (var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (rawFrameDataView.valid)
                {
                    var stringBuilder = new System.Text.StringBuilder();
                    foreach (var counterData in m_LegacyDetailCounters)
                    {
                        var category = counterData.m_Category;
                        var counter = counterData.m_Name;
                        var counterValue = ProfilerDriver.GetFormattedCounterValue(rawFrameDataView.frameIndex, category, counter);
                        stringBuilder.AppendLine($"{counter}: {counterValue}");
                    }

                    detailsViewText = stringBuilder.ToString();
                }
            }

            return detailsViewText;
        }

        public void SetNameAndUpdateAllPreferences(string name)
        {
            EditorPrefs.DeleteKey(activeStatePreferenceKey);
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);

            SetName(name);

            SaveActiveState();
            orderIndex = orderIndex;
            m_Chart.SetName(DisplayName, legacyPreferenceKey);
        }

        // ProfilerModule (base class) does not publicly support changing the chart's counters - it expects it to remain constant. We can do this internally from ProfilerModuleBase as long as we also update the auto-enabled category names.
        void InternalUpdateBaseCountersAndAutoEnabledCategoryNames()
        {
            // Pass legacy counter lists to base module.
            InternalSetChartCounters(ProfilerCounterDataUtility.ConvertFromLegacyCounterDatas(m_LegacyChartCounters));

            // Construct auto-enabled category names from chart and detail counters.
            var categories = new HashSet<string>();
            foreach (var chartCounter in chartCounters)
            {
                var categoryName = chartCounter.m_Category;
                categories.Add(categoryName);
            }

            foreach (var detailCounter in detailCounters)
            {
                var categoryName = detailCounter.m_Category;
                categories.Add(categoryName);
            }

            var autoEnabledCategoryNames = new string[categories.Count];
            categories.CopyTo(autoEnabledCategoryNames);

            // Pass auto-enabled category names to base module.
            InternalSetAutoEnabledCategoryNames(autoEnabledCategoryNames);
        }
    }
}
