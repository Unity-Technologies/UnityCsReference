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

        ChartViewController m_ChartViewController;
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

        protected internal ProfilerWindow ProfilerWindow { get; private set; }

        internal IProfilerPersistentSettingsService SettingsService { get; private set; }

        internal ChartViewController ChartViewController => m_ChartViewController;

        private protected string Tooltip { get; private set; }
        private protected string IconPath { get; private set; }

        // Must be non-serialized to prevent the value being overridden after it is set in the constructor during deserialization.
        [field: NonSerialized] private protected ProfilerModuleChartType ChartType { get; }

        // Must be non-serialized to prevent the value being overridden after it is set in the constructor during deserialization.
        [field: NonSerialized] string[] AutoEnabledCategoryNames { get; set; }

        internal virtual ChartModelBuilder CreateChartModelBuilder()
        {
            var builder = new ChartModelBuilder(SettingsService, ChartType, ChartCounters.Length, Identifier, DisplayName, Tooltip, IconPath);
            builder.SetArea(area);
            builder.ConfigureChartSeries(ProfilerUserSettings.frameCount, ChartCounters);
            return builder;
        }

        internal virtual ChartViewController CreateChartViewController()
        {
            var controller = new ChartViewController(this, ChartType, m_ChartModelBuilder.Model)
            {
                ModuleSelected = () => ProfilerWindow.selectedModule = this,
                CountersEnabledStateChanged = m_ChartModelBuilder.OnCountersEnableChange,
                CountersOrderChanged = m_ChartModelBuilder.OnCountersOrderChange,
                SelectedFrameChanged = (x) => ProfilerWindow.SetCurrentFrame(x)
            };
            return controller;
        }

        public virtual ProfilerModuleViewController CreateDetailsViewController()
        {
            return new StandardDetailsViewController(ProfilerWindow, ChartCounters);
        }

        internal static bool IsValidDisplayName(string displayName)
        {
            return !string.IsNullOrWhiteSpace(displayName);
        }

        internal void Initialize(InitializationArgs args)
        {
            m_Identifier = args.Identifier;
            DisplayName = args.DisplayName;
            Tooltip = args.Tooltip;
            IconPath = args.IconPath;
            ProfilerWindow = args.ProfilerWindow;
            SettingsService = args.SettingsService;

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

            if (!IsValidDisplayName(DisplayName))
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid name.");

            if (string.IsNullOrEmpty(IconPath))
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid icon path.");

            if (ProfilerWindow == null)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' has an invalid Profiler window reference.");

            if (ChartCounters == null || ChartCounters.Length == 0)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' cannot have no chart counters.");

            if (ChartCounters.Length > ChartModelBuilder.k_MaximumSeriesCount)
                throw new InvalidOperationException($"The Profiler module '{DisplayName}' cannot have more than {ChartModelBuilder.k_MaximumSeriesCount} chart counters.");
        }

        internal VisualElement CreateChartView()
        {
            if (m_ChartModelBuilder == null)
                m_ChartModelBuilder = CreateChartModelBuilder();

            if (m_ChartViewController == null)
            {
                m_ChartViewController = CreateChartViewController();

                // Update state outside of create chain, as it forces View
                // to load and prevents overrides from initializing correctly
                m_ChartViewController.SetSelected(ProfilerWindow.selectedModule == this);
                m_ChartViewController.SetActiveState(active);
                m_ChartViewController.NotifySelectedFrameIndexChanged(ProfilerWindow.selectedFrameIndex);
            }

            if (m_ChartViewController == null)
                throw new InvalidOperationException($"A new chart view controller was requested for the module '{DisplayName}' but none was provided.");

            return m_ChartViewController.View;
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
            public InitializationArgs(string identifier, string name, string tooltip, string iconPath, ProfilerWindow profilerWindow, IProfilerPersistentSettingsService settingsService)
            {
                Identifier = identifier;
                DisplayName = name;
                Tooltip = tooltip;
                IconPath = iconPath;
                ProfilerWindow = profilerWindow;
                SettingsService = settingsService;
            }

            public string Identifier { get; }

            public string DisplayName { get; }

            public string Tooltip { get; }

            public string IconPath { get; }

            public ProfilerWindow ProfilerWindow { get; }

            public IProfilerPersistentSettingsService SettingsService { get; }
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
        internal const ProfilerArea k_InvalidProfilerArea = unchecked((ProfilerArea)Profiler.invalidProfilerArea);

        const string k_ProfilerModuleActiveStatePreferenceKeyFormat = "ProfilerModule.{0}.Active";
        const string k_ProfilerModulePinnedStatePreferenceKeyFormat = "ProfilerModule.{0}.Pinned";
        const string k_ProfilerModuleOrderIndexPreferenceKeyFormat = "ProfilerModule.{0}.OrderIndex";
        const int k_NoFrameIndex = int.MinValue; // We cannot use -1 as the Profiler uses a frame index of -1 to signify 'no data'.

        ChartModelBuilder m_ChartModelBuilder;

        [NonSerialized] bool m_Active = false;
        [NonSerialized] bool m_Pinned = false;
        int m_LastUpdatedFrameIndex = k_NoFrameIndex;

        internal virtual ProfilerArea area => k_InvalidProfilerArea;

        internal bool active
        {
            get => m_Active;
            set
            {
                if (value == active)
                    return;

                m_Active = value;
                ApplyActiveState();
                SaveActiveState();

                // Place/close at the setter (not inside ApplyActiveState) so subclasses that
                // override ApplyActiveState without calling base (e.g. CPUProfilerModule) still
                // get re-parented when their active state changes.
                if (active)
                    ProfilerWindow.PlaceChartViewInContainer(this);
                else
                    ProfilerWindow.CloseModule(this);
            }
        }

        internal bool pinned
        {
            get => m_Pinned;
            set
            {
                if (value == pinned)
                    return;

                m_Pinned = value;
                SavePinnedState();
                ProfilerWindow.OnModulePinnedStateChanged(this);
            }
        }

        internal void SetPinnedStateWithoutCallback(bool value)
        {
            m_Pinned = value;
            SavePinnedState();
        }

        internal int orderIndex
        {
            get => EditorPrefs.GetInt(orderIndexPreferenceKey, defaultOrderIndex);
            set => EditorPrefs.SetInt(orderIndexPreferenceKey, value);
        }

        // Used by ProfilerEditorTests.
        internal ChartModelBuilder ChartModelBuilder => m_ChartModelBuilder;

        // Some modules might expose a warning message
        internal string WarningMsg { get; private protected set; }

        private protected virtual string activeStatePreferenceKey => string.Format(k_ProfilerModuleActiveStatePreferenceKeyFormat, Identifier);

        private protected virtual string pinnedStatePreferenceKey => string.Format(k_ProfilerModulePinnedStatePreferenceKeyFormat, Identifier);

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
            // Read pinned state before active. The active-setter calls PlaceChartViewInContainer,
            // which keys off m_Pinned; setting active first would place the chart in the wrong
            // container until CreateGUI's reconcile pass runs.
            m_Pinned = ReadPinnedState();
            SavePinnedState();
            active = ReadActiveState();
            SaveActiveState();
            ProfilerWindow.SelectedFrameIndexChanged += SelectedFrameIndexChanged;
        }

        internal virtual void OnDisable()
        {
            ProfilerWindow.SelectedFrameIndexChanged -= SelectedFrameIndexChanged;
            SaveViewSettings();
        }

        internal virtual void Update()
        {
            if (!active || m_ChartModelBuilder == null)
                return;

            using (Markers.updateModule.Auto())
            {
                m_ChartModelBuilder.Update(ProfilerWindow.selectedFrameIndex);

                int frameCount = ProfilerUserSettings.frameCount;
                int firstEmptyFrame = firstFrameIndexWithHistoryOffset;
                int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);
                m_ChartModelBuilder.UpdateData(firstEmptyFrame, firstFrame, frameCount);
                m_ChartModelBuilder.UpdateOverlayData(firstEmptyFrame);
                m_ChartModelBuilder.UpdateScaleValuesIfNecessary(firstEmptyFrame, firstFrame, frameCount);
                m_ChartModelBuilder.UpdateSelectedData(ProfilerWindow.selectedFrameIndex);

                m_ChartViewController.Update();

                m_LastUpdatedFrameIndex = ProfilerDriver.lastFrameIndex;
            }
        }

        internal virtual void Rebuild()
        {
            if (m_ChartViewController == null)
                return;

            // Keep current parent and dispose
            var parent = m_ChartViewController.View.parent;
            m_ChartViewController.Dispose();
            m_ChartViewController = null;

            m_ChartModelBuilder = null;

            // Re-create and add
            parent.Add(CreateChartView());
            Update();

            ProfilerWindow.UpdateVisualTreeModulesOrder();
        }

        internal virtual void Clear()
        {
            m_LastUpdatedFrameIndex = k_NoFrameIndex;
            m_ChartModelBuilder?.ResetChartState();
            m_ChartViewController.Clear();
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
            // Rebuild keeps the existing parent, so without re-syncing m_Pinned here a module
            // that was pinned before reset stays in the pinned container even though the pref
            // was just cleared. Going through the setter fires OnModulePinnedStateChanged which
            // re-parents via PlaceChartViewInContainer.
            pinned = ReadPinnedState();
            Rebuild();
        }

        internal void DeleteAllPreferences()
        {
            DeleteActiveState();
            DeletePinnedState();
            EditorPrefs.DeleteKey(orderIndexPreferenceKey);
            m_ChartModelBuilder.DeleteSettings();
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

        private protected virtual void OnSelected()
        {
            m_ChartViewController.SetSelected(true);
        }

        private protected virtual void OnDeselected()
        {
            m_ChartViewController.SetSelected(false);
        }

        private protected virtual void ApplyActiveState()
        {
            // Nb! CPU Module overrides this function w/o calling base class —
            // re-parenting lives in the `active` property setter, not here, so
            // overrides that skip the base call still get the placement.
            ProfilerWindow.SetCategoriesInUse(AutoEnabledCategoryNames, active);
            // We need to update as in de-activated state model become out-of-sync
            Update();

            if (m_ChartViewController == null)
                return;

            m_ChartViewController.SetActiveState(active);
        }

        private protected virtual bool ReadActiveState()
        {
            return EditorPrefs.GetBool(activeStatePreferenceKey, true);
        }

        // Sibling-access forwarder for ProfilerWindow's pre-OnEnable parent-placement pass.
        // Kept narrow so the virtual itself stays a derived-class-only extension point.
        internal bool GetActiveStateForWindow() => ReadActiveState();

        private protected virtual void SaveActiveState()
        {
            EditorPrefs.SetBool(activeStatePreferenceKey, active);
        }

        private protected virtual void DeleteActiveState()
        {
            EditorPrefs.DeleteKey(activeStatePreferenceKey);
        }

        private protected virtual bool ReadPinnedState()
        {
            return EditorPrefs.GetBool(pinnedStatePreferenceKey, false);
        }

        internal bool GetPinnedStateForWindow() => ReadPinnedState();

        private protected virtual void SavePinnedState()
        {
            EditorPrefs.SetBool(pinnedStatePreferenceKey, pinned);
        }

        private protected virtual void DeletePinnedState()
        {
            EditorPrefs.DeleteKey(pinnedStatePreferenceKey);
        }

        private protected void SetName(string name)
        {
            DisplayName = name;
        }

        void SelectedFrameIndexChanged(long selectedFrameIndex)
        {
            m_ChartModelBuilder.UpdateSelectedData(selectedFrameIndex);
            m_ChartViewController.NotifySelectedFrameIndexChanged(selectedFrameIndex);
        }

        static class Markers
        {
            public static readonly ProfilerMarker updateModule = new ProfilerMarker("ProfilerModule.Update");
            public static readonly ProfilerMarker drawChartView = new ProfilerMarker("ProfilerModule.DrawChartView");
        }
    }
}
