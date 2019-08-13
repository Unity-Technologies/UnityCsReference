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
    internal abstract class CPUorGPUProfilerModule : ProfilerModuleBase
    {
        [SerializeField]
        protected ProfilerViewType m_ViewType = ProfilerViewType.Timeline;

        [SerializeField]
        protected ProfilerFrameDataHierarchyView m_FrameDataHierarchyView;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal ProfilerFrameDataHierarchyView FrameDataHierarchyView => m_FrameDataHierarchyView;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests
        internal ProfilerViewType ViewType
        {
            get { return m_ViewType; }
            set { m_ViewType = value; }
        }

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);
            if (m_FrameDataHierarchyView == null)
                m_FrameDataHierarchyView = new ProfilerFrameDataHierarchyView();
            m_FrameDataHierarchyView.gpuView = false;
            m_FrameDataHierarchyView.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_FrameDataHierarchyView.selectionChanged += CPUOrGPUViewSelectionChanged;
            m_ProfilerWindow.selectionChanged += m_FrameDataHierarchyView.SetSelectionFromLegacyPropertyPath;
        }

        public override void DrawToolbar(Rect position)
        {
            // Hierarchy view still needs to be broken apart into Toolbar and View.
        }

        public override void DrawView(Rect position)
        {
            m_FrameDataHierarchyView.DoGUI(GetFrameDataView());
        }

        HierarchyFrameDataView GetFrameDataView()
        {
            var viewMode = HierarchyFrameDataView.ViewModes.Default;
            if (m_ViewType == ProfilerViewType.Hierarchy)
                viewMode |= HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            return m_ProfilerWindow.GetFrameDataView(m_FrameDataHierarchyView.threadName, viewMode | m_FrameDataHierarchyView.GetFilteringMode(), m_FrameDataHierarchyView.sortedProfilerColumn, m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }

        void CPUOrGPUViewSelectionChanged(int id)
        {
            var frameDataView = GetFrameDataView();
            if (frameDataView == null || !frameDataView.valid)
                return;

            m_ProfilerWindow.SetSelectedPropertyPath(frameDataView.GetItemPath(id));
        }

        protected void CPUOrGPUViewTypeChanged(ProfilerViewType viewtype)
        {
            if (m_ViewType == viewtype)
                return;

            m_ViewType = viewtype;
        }

        public override void Clear()
        {
            base.Clear();
            m_FrameDataHierarchyView.Clear();
        }
    }
}
