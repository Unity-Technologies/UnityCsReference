// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using System.Globalization;

// TODO When migrating to ProfilerModuleController, ensure its namespace becomes Unity.Profiling.Editor.
namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal abstract class ProfilerModuleBase
    {
        static class Markers
        {
            public static readonly ProfilerMarker updateModule = new ProfilerMarker("ProfilerModule.Update");
            public static readonly ProfilerMarker drawChartView = new ProfilerMarker("ProfilerModule.DrawChartView");
        }

        const string k_ProfilerModuleActiveStatePreferenceKeyFormat = "ProfilerModule.{0}.Active";
        const string k_ProfilerModuleOrderIndexPreferenceKeyFormat = "ProfilerModule.{0}.OrderIndex";
        const string k_NoCategory = "NoCategory";
        const int k_InvalidIndex = -1;

        [SerializeReference] protected IProfilerWindowController m_ProfilerWindow;

        [SerializeField] protected string m_Name;
        [SerializeField] protected string m_LocalizedName;
        [SerializeField] protected string m_IconName;

        protected ProfilerChart m_Chart;
        [SerializeField] protected List<ProfilerCounterData> m_ChartCounters;
        [SerializeField] protected List<ProfilerCounterData> m_DetailCounters;
        [SerializeField] protected Chart.ChartType m_ChartType;

        [SerializeField] protected Vector2 m_PaneScroll;

        ProfilerModuleViewController m_DetailsViewController;

        // We cannot use -1 as the Profiler uses a frame index of -1 to signify 'no data'.
        const int k_NoFrameIndex = int.MinValue;
        int m_LastUpdatedFrameIndex = k_NoFrameIndex;

        protected ProfilerModuleBase(IProfilerWindowController profilerWindow, string name, string localizedName, string iconName, Chart.ChartType chartType = Chart.ChartType.Line)
        {
            m_ProfilerWindow = profilerWindow;
            m_Name = name;
            if (string.IsNullOrEmpty(localizedName))
                localizedName = name;
            m_LocalizedName = localizedName;
            m_IconName = iconName;
            m_ChartType = chartType;

            m_ChartCounters = CollectDefaultChartCounters();
            if (m_ChartCounters.Count > maximumNumberOfChartCounters)
            {
                throw new ArgumentException($"Chart counters cannot contain more than {maximumNumberOfChartCounters} counters.");
            }
            m_DetailCounters = CollectDefaultDetailCounters();
        }

        public virtual ProfilerArea area => unchecked((ProfilerArea)Profiler.invalidProfilerArea);
        public ReadOnlyCollection<ProfilerCounterData> chartCounters => m_ChartCounters.AsReadOnly();
        public ProfilerChart chart => m_Chart;
        public ReadOnlyCollection<ProfilerCounterData> detailCounters => m_DetailCounters.AsReadOnly();
        // TODO This property's default value will be changed to a view controller drawing a UIToolkit-based list of counters as part of the Extensibility API.
        public virtual ProfilerModuleViewController DetailsViewController => new LegacyDetailsViewController(ProfilerWindow, this);

        [NonSerialized]
        bool m_Active = false;
        public bool active
        {
            get => m_Active;
            set
            {
                if (value == active)
                {
                    return;
                }

                m_Active = value;
                ApplyActiveState();
                SaveActiveState();

                if (active == false)
                {
                    m_Chart.Close();
                }
            }
        }

        public string name
        {
            get => m_Name;
            set
            {
                SetNameAndUpdateAllPreferences(value);
            }
        }
        public string localizedName
        {
            get => m_LocalizedName;
        }
        public int orderIndex
        {
            get => EditorPrefs.GetInt(orderIndexPreferenceKey, defaultOrderIndex);
            set => EditorPrefs.SetInt(orderIndexPreferenceKey, value);
        }

        // TODO This property must be made public as part of Extensibility work.
        internal ProfilerWindow ProfilerWindow => EditorWindow.GetWindowDontShow<ProfilerWindow>(); // TODO Future Extensibility PR will just return the direct profiler window reference here.

        // Modules that use legacy stats override `usesCounters` to use the legacy GetGraphStatisticsPropertiesForArea functionality instead of Profiler Counters.
        public virtual bool usesCounters => true;

        protected virtual int defaultOrderIndex => k_InvalidIndex;
        // Built-in modules override this to maintain the user's existing preferences, which used the ProfilerArea in the key.
        protected virtual string legacyPreferenceKey => null;

        // Use this when iterating over arrays of history length. This + iterationIndex < 0 means no data for this frame, for anything else, this is the same as ProfilerDriver.firstFrame.
        protected int firstFrameIndexWithHistoryOffset => ProfilerDriver.lastFrameIndex + 1 - ProfilerUserSettings.frameCount;
        protected string activeStatePreferenceKey
        {
            get
            {
                if (!string.IsNullOrEmpty(legacyPreferenceKey))
                {
                    // Use the legacy preference key to maintain user settings on built-in modules.
                    return legacyPreferenceKey;
                }

                return string.Format(k_ProfilerModuleActiveStatePreferenceKeyFormat, m_Name);
            }
        }

        string orderIndexPreferenceKey => string.Format(k_ProfilerModuleOrderIndexPreferenceKeyFormat, name);
        int maximumNumberOfChartCounters
        {
            get => ProfilerChart.k_MaximumSeriesCount;
        }

        public virtual void OnEnable()
        {
            BuildChartIfNecessary();

            active = ReadActiveState();
        }

        public virtual void OnDisable()
        {
            SaveViewSettings();
        }

        public virtual void OnLostFocus()
        {
            m_Chart.OnLostFocus();
        }

        public virtual void Update()
        {
            using (Markers.updateModule.Auto())
            {
                UpdateChart();
                m_LastUpdatedFrameIndex = ProfilerDriver.lastFrameIndex;
            }
        }

        public virtual void SaveViewSettings() {}
        public virtual void OnClosed() {} // TODO Nobody uses this. Remove it?

        public virtual void Clear()
        {
            m_LastUpdatedFrameIndex = k_NoFrameIndex;
            m_Chart?.ResetChartState();
        }

        public virtual void OnNativePlatformSupportModuleChanged() {}

        public virtual void Rebuild()
        {
            RebuildChart();
        }

        internal VisualElement CreateDetailsView()
        {
            OnSelected();

            if (m_DetailsViewController != null)
                throw new InvalidOperationException($"A new details view was requested for the module '{m_Name}' but the previous one has not been destroyed.");

            m_DetailsViewController = DetailsViewController;
            if (m_DetailsViewController == null)
                throw new InvalidOperationException($"A new details view controller was requested for the module '{m_Name}' but none was provided.");

            return m_DetailsViewController.View;
        }

        internal void CloseDetailsView()
        {
            OnDeselected();

            if (m_DetailsViewController != null)
            {
                m_DetailsViewController.Dispose();
                m_DetailsViewController = null;
            }
        }

        public virtual void DrawToolbar(Rect position) {}

        public virtual void DrawDetailsView(Rect position) {}

        public float GetMinimumChartHeight()
        {
            return m_Chart.GetMinimumHeight();
        }

        public int DrawChartView(Rect chartRect, int currentFrame, bool isSelected, int lastVisibleFrameIndex)
        {
            using (Markers.drawChartView.Auto())
            {
                // Only update modules if repainting and the visible range has changed.
                var visibleRangeHasChanged = (m_LastUpdatedFrameIndex != lastVisibleFrameIndex);
                if (Event.current.type == EventType.Repaint && visibleRangeHasChanged)
                {
                    Update();
                }

                currentFrame = m_Chart.DoChartGUI(chartRect, currentFrame, isSelected);
                if (isSelected)
                    DrawChartOverlay(m_Chart.lastChartRect);
                return currentFrame;
            }
        }

        public virtual void DrawChartOverlay(Rect chartRect) {}

        public void ToggleActive()
        {
            active = !active;
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
                m_ProfilerWindow.SetAreasInUse(previousAreas, false);
            }

            // Capture each chart counter's enabled state prior to assignment.
            Dictionary<ProfilerCounterData, bool> counterEnabledStates = null;
            if (m_Chart != null)
            {
                counterEnabledStates = new Dictionary<ProfilerCounterData, bool>();
                for (int i = 0; i < m_ChartCounters.Count; ++i)
                {
                    var counter = m_ChartCounters[i];
                    var enabled = m_Chart.m_Series[i].enabled;
                    counterEnabledStates.Add(counter, enabled);
                }
            }

            m_ChartCounters = chartCounters;
            m_DetailCounters = detailCounters;
            RebuildChart();

            // Restore each chart counter's enabled state after assignment.
            if (counterEnabledStates != null && counterEnabledStates.Count > 0)
            {
                for (int i = 0; i < m_ChartCounters.Count; ++i)
                {
                    var counter = m_ChartCounters[i];

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
                m_ProfilerWindow.SetAreasInUse(areas, true);
            }
        }

        public void ResetOrderIndexToDefault()
        {
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);
        }

        public void DeleteAllPreferences()
        {
            EditorPrefs.DeleteKey(activeStatePreferenceKey);
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);
            m_Chart.DeleteSettings();
        }

        protected virtual void OnSelected() {}

        protected virtual void OnDeselected() {}

        /// <summary>
        /// Override this method to customize the text displayed in the module's details view.
        /// </summary>
        protected virtual string GetDetailsViewText()
        {
            string detailsViewText = (usesCounters) ? ConstructTextSummaryFromDetailCounters() : ProfilerDriver.GetOverviewText(area, m_ProfilerWindow.GetActiveVisibleFrameIndex());
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

        /// <summary>
        /// Override this method to instantiate the module's chart. For example, this is currently used by the UIDetailsProfilerModule to instantiate a custom ProfilerChart type.
        /// </summary>
        protected virtual ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            m_Chart = new ProfilerChart(area, m_ChartType, defaultChartScale, chartMaximumScaleInterpolationValue, m_ChartCounters.Count, name, localizedName, m_IconName);
            return m_Chart;
        }

        protected virtual void UpdateChartOverlay(int firstEmptyFrame, int firstFrame, int frameCount) {}

        protected virtual void ApplyActiveState()
        {
            var areas = GetProfilerAreas();
            m_ProfilerWindow.SetAreasInUse(areas, active);
        }

        protected virtual bool ReadActiveState()
        {
            return EditorPrefs.GetBool(activeStatePreferenceKey, true);
        }

        protected virtual void SaveActiveState()
        {
            EditorPrefs.SetBool(activeStatePreferenceKey, active);
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

        void InitializeChart()
        {
            var isStackedFillChartType = (m_ChartType == Chart.ChartType.StackedFill);
            var chartScale = (isStackedFillChartType) ? 0.001f : 1f;
            var chartMaximumScaleInterpolationValue = (isStackedFillChartType) ? -1f : 0f;
            // DynamicProfilerModules rely on serialization to store their counter lists.
            if (!(this is DynamicProfilerModule))
            {
                // Legacy modules rely on getting the counter lists from native.
                // Counter modules rely on CollectDefaultChartCounters overloads and lists of counters.
                // So to give these a chance to update what was serialized to layout file, stomp these serialized lists with fresh ones here
                // otherwise, e.g. a legacy module that no longer reports stat x or no longer charts it, would still try to query it based on the outdated serialized list.
                m_ChartCounters = CollectDefaultChartCounters();
                m_DetailCounters = CollectDefaultDetailCounters();
            }
            m_Chart = InstantiateChart(chartScale, chartMaximumScaleInterpolationValue);
            m_Chart.ConfigureChartSeries(ProfilerUserSettings.frameCount, m_ChartCounters);
            ConfigureChartSelectionCallbacks();
        }

        void ConfigureChartSelectionCallbacks()
        {
            m_Chart.selected += OnChartSelected;
            m_Chart.closed += OnChartClosed;
        }

        void OnChartSelected(Chart chart)
        {
            m_ProfilerWindow.selectedModule = this;
        }

        void OnChartClosed(Chart chart)
        {
            m_ProfilerWindow.CloseModule(this);
        }

        void UpdateChart()
        {
            BuildChartIfNecessary();
            int frameCount = ProfilerUserSettings.frameCount;
            int firstEmptyFrame = firstFrameIndexWithHistoryOffset;
            int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);
            m_Chart.UpdateData(firstEmptyFrame, firstFrame, frameCount);
            UpdateChartOverlay(firstEmptyFrame, firstFrame, frameCount);
            m_Chart.UpdateScaleValuesIfNecessary(firstEmptyFrame, firstFrame, frameCount);
        }

        void RebuildChart()
        {
            var forceRebuild = true;
            BuildChartIfNecessary(forceRebuild);
        }

        void BuildChartIfNecessary(bool forceRebuild = false)
        {
            if (forceRebuild || m_Chart == null)
            {
                InitializeChart();
                UpdateChart();
            }

            m_Chart.LoadAndBindSettings(legacyPreferenceKey);
        }

        string ConstructTextSummaryFromDetailCounters()
        {
            string detailsViewText = null;
            using (var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(m_ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (rawFrameDataView.valid)
                {
                    var stringBuilder = new System.Text.StringBuilder();
                    foreach (var counterData in m_DetailCounters)
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

        HashSet<ProfilerArea> GetProfilerAreas()
        {
            var areas = new HashSet<ProfilerArea>();
            if (usesCounters)
            {
                foreach (var counterData in chartCounters)
                {
                    AddCounterToAreas(counterData, areas);
                }

                foreach (var counterData in detailCounters)
                {
                    AddCounterToAreas(counterData, areas);
                }
            }
            else
            {
                areas.Add(area);
            }

            return areas;
        }

        void AddCounterToAreas(ProfilerCounterData counter, HashSet<ProfilerArea> areas)
        {
            var categoryName = counter.m_Category;
            var categoryAreas = ProfilerAreaReferenceCounterUtility.ProfilerCategoryNameToAreas(categoryName);
            foreach (var area in categoryAreas)
            {
                areas.Add(area);
            }
        }

        void SetNameAndUpdateAllPreferences(string name)
        {
            EditorPrefs.DeleteKey(activeStatePreferenceKey);
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);

            m_Name = name;
            m_LocalizedName = name;

            SaveActiveState();
            orderIndex = orderIndex;
            m_Chart.SetName(localizedName, legacyPreferenceKey);
        }
    }
}
