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
        ProfilerTimelineGUI m_TimelineGUI;

        const string k_ViewTypeSettingsKey = "Profiler.CPUProfilerModule.ViewType";
        protected override string ViewTypeSettingsKey => k_ViewTypeSettingsKey;
        protected override ProfilerViewType DefaultViewTypeSetting => ProfilerViewType.Timeline;

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);

            m_TimelineGUI = new ProfilerTimelineGUI(m_ProfilerWindow);
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
                m_TimelineGUI.DoGUI(m_ProfilerWindow.GetActiveVisibleFrameIndex(), position);
            }
            else
            {
                base.DrawView(position);
            }
        }

        HierarchyFrameDataView GetTimelineFrameDataView()
        {
            return m_ProfilerWindow.GetFrameDataView(
                m_FrameDataHierarchyView.threadName,
                HierarchyFrameDataView.ViewModes.Default | m_TimelineGUI.GetFilteringMode(),
                m_FrameDataHierarchyView.sortedProfilerColumn,
                m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }
    }
}
