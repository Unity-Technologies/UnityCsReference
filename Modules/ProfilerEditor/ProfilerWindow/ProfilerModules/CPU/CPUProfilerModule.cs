// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class CPUProfilerModule : CPUorGPUProfilerModule
    {
        [SerializeField]
        ProfilerTimelineGUI m_TimelineGUI;

        const string k_SettingsKeyPrefix = "Profiler.CPUProfilerModule.";
        protected override string SettingsKeyPrefix => k_SettingsKeyPrefix;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Timeline;

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);

            m_TimelineGUI = new ProfilerTimelineGUI();
            m_TimelineGUI.OnEnable(this, profilerWindow, false);
            m_TimelineGUI.viewTypeChanged += CPUOrGPUViewTypeChanged;
        }

        public override void DrawToolbar(Rect position)
        {
            if (m_TimelineGUI != null && m_ViewType == ProfilerViewType.Timeline)
            {
                base.DrawToolbar(position);
            }
            else
            {
                base.DrawToolbar(position);
            }
        }

        public override void DrawView(Rect position)
        {
            if (m_TimelineGUI != null && m_ViewType == ProfilerViewType.Timeline)
            {
                m_TimelineGUI.DoGUI(m_ProfilerWindow.GetActiveVisibleFrameIndex(), position, fetchData, ref updateViewLive);
            }
            else
            {
                base.DrawView(position);
            }
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
    }
}
