// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Screenshots", IconPath = "Profiler.Video")]
    internal class ScreenshotsProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 20;

        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;

        // ProfilerModule.AssertIsValid requires at least one chart counter. The screenshots module
        // renders a custom timeline (ScreenshotsChartViewControllerWrapper) rather than a counter
        // chart, so the data behind this counter is never displayed. The empty category is the key:
        // ProfilerCategoryActivator.RetainCategory short-circuits on empty/null, so no native
        // category is auto-enabled when the module activates.
        protected override List<ProfilerCounterData> CollectDefaultChartCounters() =>
            new List<ProfilerCounterData> { new ProfilerCounterData { m_Category = string.Empty, m_Name = "ScreenshotsModulePlaceholder" } };

        // Screenshots module is pinned by default
        private protected override bool ReadPinnedState()
        {
            return EditorPrefs.GetBool(pinnedStatePreferenceKey, true);
        }

        // Persists the centred frame across module-switch teardowns of ScreenshotsTimelineViewController
        // so scroll position survives the round-trip. Lives on the per-window module instance (not a
        // process-wide static) so multiple open ProfilerWindow instances don't clobber each other.
        internal int LastCentredScreenshotFrameIndex { get; set; } = -1;

        internal override ChartViewController CreateChartViewController()
        {
            // Reuse the ChartModel already created (and re-anchored every frame) by the base
            // ProfilerModule update path rather than building a second one. Sharing it keeps the
            // chart's frame-selection slider on the same per-frame cadence as every other module, so
            // it stays aligned during live recording instead of only updating on discrete
            // frame-changed events. CreateChartView() assigns ChartModelBuilder before this runs.
            var wrapper = new ScreenshotsChartViewControllerWrapper(this, ProfilerWindow, ChartModelBuilder.Model);
            return wrapper;
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            var controller = new ScreenshotsModuleViewController(ProfilerWindow, this);
            return controller;
        }
    }
}
