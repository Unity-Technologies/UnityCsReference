// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("UI Details (Canvas)", typeof(LocalizationResource), IconPath = "Profiler.UICanvasDetails")]
    internal class UIDetailsProfilerModule : UIProfilerModule
    {
        const int k_DefaultOrderIndex = 13;
        const ProfilerModuleChartType k_ChartType = ProfilerModuleChartType.Line;
        UISystemProfilerModelBuilder m_UIModelBuilder;
        UISystemProfilerChartViewController m_UIChartView;

        public UIDetailsProfilerModule() : base(k_ChartType) { }

        internal override ProfilerArea area => ProfilerArea.UIDetails;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartUIDetails";

        internal override ChartModelBuilder CreateChartModelBuilder()
        {
            m_UIModelBuilder = new UISystemProfilerModelBuilder(SettingsService, k_ChartType, ChartCounters.Length, Identifier, DisplayName, Tooltip, IconPath);
            m_UIModelBuilder.SetArea(area);
            m_UIModelBuilder.ConfigureChartSeries(ProfilerUserSettings.frameCount, ChartCounters);
            return m_UIModelBuilder;
        }

        internal override ChartViewController CreateChartViewController()
        {
            var controller = base.CreateChartViewController();
            controller.OnViewLoaded = () =>
            {
                m_UIChartView = new UISystemProfilerChartViewController(this, ChartViewController.Chart, m_UIModelBuilder.UIModel);
            };
            return controller;
        }

        internal override void Update()
        {
            base.Update();

            if (active)
                m_UIChartView?.Update();
        }
    }
}
