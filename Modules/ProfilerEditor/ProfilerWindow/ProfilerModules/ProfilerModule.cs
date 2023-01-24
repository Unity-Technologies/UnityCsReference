// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    [Serializable]
    public abstract partial class ProfilerModule
    {
        // The identifier must be serialized as it is used by the ProfilerWindow to identify a deserialized module prior to calling Initialize() on it.
        [SerializeField] string m_Identifier;

        ProfilerModuleViewController m_DetailsViewController;

        protected ProfilerModule(ProfilerCounterDescriptor[] chartCounters, ProfilerModuleChartType defaultChartType = ProfilerModuleChartType.Line, string[] autoEnabledCategoryNames = null)
        {
            ChartCounters = chartCounters;
            ChartType = defaultChartType;

            // By default, the module's auto-enabled categories are those used by the chart.
            if (autoEnabledCategoryNames == null || autoEnabledCategoryNames.Length == 0)
                autoEnabledCategoryNames = UniqueCategoryNamesInCounters(chartCounters);
            AutoEnabledCategoryNames = autoEnabledCategoryNames;
        }

        public string DisplayName { get; private set; }

        internal string Identifier => m_Identifier;

        // Must be non-serialized to prevent the value being overridden after it is set in the constructor during deserialization.
        [field: NonSerialized] internal ProfilerCounterDescriptor[] ChartCounters { get; private set; }

        protected ProfilerWindow ProfilerWindow { get; private set; }

        private protected string IconPath { get; private set; }

        // Must be non-serialized to prevent the value being overridden after it is set in the constructor during deserialization.
        [field: NonSerialized] ProfilerModuleChartType ChartType { get; }

        // Must be non-serialized to prevent the value being overridden after it is set in the constructor during deserialization.
        [field: NonSerialized] string[] AutoEnabledCategoryNames { get; set; }

        public virtual ProfilerModuleViewController CreateDetailsViewController()
        {
            return new StandardDetailsViewController(ProfilerWindow, ChartCounters);
        }

        internal void Initialize(InitializationArgs args)
        {
            m_Identifier = args.Identifier;
            DisplayName = args.DisplayName;
            IconPath = args.IconPath;
            ProfilerWindow = args.ProfilerWindow;

            // Give legacy modules a chance to setup their counters after construction.
            LegacyModuleInitialize();

            // Verify a module is valid once initialized. A module could become invalid across script reload, such as if a user changes the list of counters for an existing module to an invalid value.
            // Because ProfilerModule derived types use the parameterless constructor, we validate the module after construction here so we don't throw exceptions during deserialization.
            AssertIsValid();
        }

        internal virtual void LegacyModuleInitialize() {}

        internal void AssertIsValid()
        {
            if (string.IsNullOrEmpty(Identifier))
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid identifier.");

            if (string.IsNullOrEmpty(DisplayName))
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid name.");

            if (string.IsNullOrEmpty(IconPath))
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid icon path.");

            if (ProfilerWindow == null)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid Profiler window reference.");

            if (ChartCounters == null || ChartCounters.Length == 0)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' cannot have no chart counters.");

            if (ChartCounters.Length > ProfilerChart.k_MaximumSeriesCount)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' cannot have more than {ProfilerChart.k_MaximumSeriesCount} chart counters.");
        }

        internal VisualElement CreateDetailsView()
        {
            OnSelected();

            if (m_DetailsViewController != null)
                throw new InvalidOperationException($"A new details view was requested for the module '{DisplayName}' but the previous one has not been destroyed.");

            m_DetailsViewController = CreateDetailsViewController();
            if (m_DetailsViewController == null)
                throw new InvalidOperationException($"A new details view controller was requested for the module '{DisplayName}' but none was provided.");

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

        string[] UniqueCategoryNamesInCounters(ProfilerCounterDescriptor[] counters)
        {
            var categories = new HashSet<string>();
            if (counters != null)
            {
                foreach (var counter in counters)
                {
                    categories.Add(counter.CategoryName);
                }
            }

            var uniqueCategoryNames = new string[categories.Count];
            categories.CopyTo(uniqueCategoryNames);

            return uniqueCategoryNames;
        }

        internal readonly struct InitializationArgs
        {
            public InitializationArgs(string identifier, string name, string iconPath, ProfilerWindow profilerWindow)
            {
                Identifier = identifier;
                DisplayName = name;
                IconPath = iconPath;
                ProfilerWindow = profilerWindow;
            }

            public string Identifier { get; }

            public string DisplayName { get; }

            public string IconPath { get; }

            public ProfilerWindow ProfilerWindow { get; }
        }

        internal class LocalizationResource : ProfilerModuleMetadataAttribute.IResource
        {
            string ProfilerModuleMetadataAttribute.IResource.GetLocalizedString(string key)
            {
                return LocalizationDatabase.GetLocalizedString(key);
            }
        }
    }

    // All legacy elements of the module that have been copied over from ProfilerModuleBase and may not be applicable going forward. Over time we can rework this.
    public abstract partial class ProfilerModule
    {
        internal const int k_UndefinedOrderIndex = int.MaxValue;

        const string k_ProfilerModuleActiveStatePreferenceKeyFormat = "ProfilerModule.{0}.Active";
        const string k_ProfilerModuleOrderIndexPreferenceKeyFormat = "ProfilerModule.{0}.OrderIndex";
        const int k_NoFrameIndex = int.MinValue; // We cannot use -1 as the Profiler uses a frame index of -1 to signify 'no data'.

        private protected ProfilerChart m_Chart;

        [NonSerialized] bool m_Active = false;
        int m_LastUpdatedFrameIndex = k_NoFrameIndex;

        internal virtual ProfilerArea area => unchecked((ProfilerArea)Profiler.invalidProfilerArea);

        internal bool active
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

                if (active == false && Chart != null)
                {
                    Chart.Close();
                }
            }
        }

        internal int orderIndex
        {
            get => EditorPrefs.GetInt(orderIndexPreferenceKey, defaultOrderIndex);
            set => EditorPrefs.SetInt(orderIndexPreferenceKey, value);
        }

        // Used by ProfilerEditorTests.
        internal ProfilerChart Chart => m_Chart;

        private protected virtual string activeStatePreferenceKey => string.Format(k_ProfilerModuleActiveStatePreferenceKeyFormat, Identifier);

        private protected string orderIndexPreferenceKey => string.Format(k_ProfilerModuleOrderIndexPreferenceKeyFormat, Identifier);

        // Use this when iterating over arrays of history length. This + iterationIndex < 0 means no data for this frame, for anything else, this is the same as ProfilerDriver.firstFrame.
        private protected int firstFrameIndexWithHistoryOffset => ProfilerDriver.lastFrameIndex + 1 - ProfilerUserSettings.frameCount;

        // Legacy modules override this to maintain the user's existing preferences, which used the ProfilerArea in the key.
        private protected virtual string legacyPreferenceKey => null;

        // Legacy modules override this to specify their default order in the menu.
        private protected virtual int defaultOrderIndex => k_UndefinedOrderIndex;

        // For ProfilerModuleBase to access private property.
        private protected string[] GetAutoEnabledCategoryNames => AutoEnabledCategoryNames;

        internal virtual void OnEnable()
        {
            BuildChartIfNecessary();
            active = ReadActiveState();
        }

        internal virtual void OnDisable()
        {
            SaveViewSettings();
        }

        internal float GetMinimumChartHeight()
        {
            return m_Chart.GetMinimumHeight();
        }

        internal int DrawChartView(Rect chartRect, int currentFrame, bool isSelected, int lastVisibleFrameIndex)
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

        internal virtual void Update()
        {
            using (Markers.updateModule.Auto())
            {
                UpdateChart();
                m_LastUpdatedFrameIndex = ProfilerDriver.lastFrameIndex;
            }
        }

        internal virtual void Rebuild()
        {
            RebuildChart();
        }

        internal void OnLostFocus()
        {
            m_Chart.OnLostFocus();
        }

        internal virtual void Clear()
        {
            m_LastUpdatedFrameIndex = k_NoFrameIndex;
            m_Chart?.ResetChartState();
        }

        internal virtual void OnNativePlatformSupportModuleChanged() {}

        internal virtual void SaveViewSettings() {}

        internal void ToggleActive()
        {
            active = !active;
        }

        internal void ResetToDefaultPreferences()
        {
            DeleteAllPreferences();
            active = ReadActiveState();
            Rebuild();
        }

        internal void DeleteAllPreferences()
        {
            DeleteActiveState();
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);
            m_Chart.DeleteSettings();
        }

        // Used by ProfilerModuleBase, which cannot provide counters during construction.
        internal void InternalSetChartCounters(ProfilerCounterDescriptor[] chartCounters)
        {
            ChartCounters = chartCounters;
        }

        // Used by ProfilerModuleBase, which cannot provide counters during construction and therefore cannot provide auto-enabled category names either.
        internal void InternalSetAutoEnabledCategoryNames(string[] autoEnabledCategoryNames)
        {
            AutoEnabledCategoryNames = autoEnabledCategoryNames;
        }

        private protected virtual void OnSelected() {}

        private protected virtual void OnDeselected() {}

        private protected virtual void ApplyActiveState()
        {
            ProfilerWindow.SetCategoriesInUse(AutoEnabledCategoryNames, active);
        }

        private protected virtual bool ReadActiveState()
        {
            return EditorPrefs.GetBool(activeStatePreferenceKey, true);
        }

        private protected virtual void SaveActiveState()
        {
            EditorPrefs.SetBool(activeStatePreferenceKey, active);
        }

        private protected virtual void DeleteActiveState()
        {
            EditorPrefs.DeleteKey(activeStatePreferenceKey);
        }

        private protected virtual ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            m_Chart = new ProfilerChart(area, ChartType, defaultChartScale, chartMaximumScaleInterpolationValue, ChartCounters.Length, Identifier, DisplayName, IconPath);
            return m_Chart;
        }

        private protected virtual void UpdateChartOverlay(int firstEmptyFrame, int firstFrame, int frameCount) {}

        private protected virtual void DrawChartOverlay(Rect chartRect) {}

        private protected void RebuildChart()
        {
            var forceRebuild = true;
            BuildChartIfNecessary(forceRebuild);
        }

        private protected void SetName(string name)
        {
            DisplayName = name;
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

        void InitializeChart()
        {
            var isStackedFillChartType = (ChartType == ProfilerModuleChartType.StackedTimeArea);
            var chartScale = (isStackedFillChartType) ? 0.001f : 1f;
            var chartMaximumScaleInterpolationValue = (isStackedFillChartType) ? -1f : 0f;
            m_Chart = InstantiateChart(chartScale, chartMaximumScaleInterpolationValue);
            m_Chart.ConfigureChartSeries(ProfilerUserSettings.frameCount, ChartCounters);
            ConfigureChartSelectionCallbacks();
        }

        void ConfigureChartSelectionCallbacks()
        {
            m_Chart.selected += OnChartSelected;
            m_Chart.closed += OnChartClosed;
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

        void OnChartSelected(Chart chart)
        {
            ProfilerWindow.selectedModule = this;
        }

        void OnChartClosed(Chart chart)
        {
            ProfilerWindow.CloseModule(this);
        }

        static class Markers
        {
            public static readonly ProfilerMarker updateModule = new ProfilerMarker("ProfilerModule.Update");
            public static readonly ProfilerMarker drawChartView = new ProfilerMarker("ProfilerModule.DrawChartView");
        }
    }
}
