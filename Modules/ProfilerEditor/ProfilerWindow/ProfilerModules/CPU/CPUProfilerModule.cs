// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class CPUProfilerModule : CPUorGPUProfilerModule
    {
        const string k_SettingsKeyPrefix = "Profiler.CPUProfilerModule.";
        const string k_IconName = "Profiler.CPU";
        const int k_DefaultOrderIndex = 0;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("CPU Usage");

        [SerializeField]
        ProfilerTimelineGUI m_TimelineGUI;

        public CPUProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.CPU;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartCPU";
        protected override string SettingsKeyPrefix => k_SettingsKeyPrefix;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Timeline;

        public override void OnEnable()
        {
            base.OnEnable();

            m_TimelineGUI = new ProfilerTimelineGUI();
            m_TimelineGUI.OnEnable(this, m_ProfilerWindow, false);
            m_TimelineGUI.viewTypeChanged += CPUOrGPUViewTypeChanged;
        }

        public override void DrawDetailsView(Rect position)
        {
            if (m_TimelineGUI != null && m_ViewType == ProfilerViewType.Timeline)
            {
                if (Event.current.isKey)
                    ProfilerWindowAnalytics.RecordViewKeyboardEvent(ProfilerWindowAnalytics.profilerCPUModuleTimeline);
                if (Event.current.isMouse && position.Contains(Event.current.mousePosition))
                    ProfilerWindowAnalytics.RecordViewMouseEvent(ProfilerWindowAnalytics.profilerCPUModuleTimeline);
                m_TimelineGUI.DoGUI(m_ProfilerWindow.GetActiveVisibleFrameIndex(), position, fetchData, ref updateViewLive);
            }
            else
            {
                if (Event.current.isKey)
                    ProfilerWindowAnalytics.RecordViewKeyboardEvent(ProfilerWindowAnalytics.profilerCPUModuleHierarchy);
                if (Event.current.isMouse && position.Contains(Event.current.mousePosition))
                    ProfilerWindowAnalytics.RecordViewMouseEvent(ProfilerWindowAnalytics.profilerCPUModuleHierarchy);
                base.DrawDetailsView(position);
            }
        }

        public override void Rebuild()
        {
            base.Rebuild();
            m_TimelineGUI.ReInitialize();
        }

        protected override HierarchyFrameDataView.ViewModes GetFilteringMode()
        {
            return (((int)ViewOptions & (int)ProfilerViewFilteringOptions.CollapseEditorBoundarySamples) != 0) ? HierarchyFrameDataView.ViewModes.HideEditorOnlySamples : HierarchyFrameDataView.ViewModes.Default;
        }

        protected override void ToggleOption(ProfilerViewFilteringOptions option)
        {
            base.ToggleOption(option);
            m_TimelineGUI?.Clear();
        }

        protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.SetOnSeriesToggleCallback(OnChartSeriesToggled);
            return chart;
        }

        protected override void ApplyActiveState()
        {
            // Opening/closing CPU chart should not set the CPU area as that would set Profiler.enabled.
        }

        void OnChartSeriesToggled(bool wasToggled)
        {
            if (wasToggled)
            {
                int firstEmptyFrame = firstFrameIndexWithHistoryOffset;
                int firstFrame = Mathf.Max(ProfilerDriver.firstFrameIndex, firstEmptyFrame);
                int frameCount = ProfilerUserSettings.frameCount;
                m_Chart.ComputeChartScaleValue(firstEmptyFrame, firstFrame, frameCount);
            }
        }

        protected override void UpdateChartOverlay(int firstEmptyFrame, int firstFrame, int frameCount)
        {
            base.UpdateChartOverlay(firstEmptyFrame, firstFrame, frameCount);

            string selectedName = ProfilerDriver.selectedPropertyPath;
            var selectedModule = m_ProfilerWindow.SelectedModule;
            bool hasCPUOverlay = (selectedName != string.Empty) && this.Equals(selectedModule);
            if (hasCPUOverlay)
            {
                m_Chart.UpdateOverlayData(firstEmptyFrame);
            }
            else
            {
                m_Chart.m_Data.hasOverlay = false;
            }
        }
    }
}
