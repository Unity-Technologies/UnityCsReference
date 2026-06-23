// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Profiling;
using L10n = UnityEditor.L10n;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Rendering", typeof(LocalizationResource), IconPath = "Profiler.Rendering")]
    internal class RenderingProfilerModule : ProfilerModuleBase
    {
        internal static class Styles
        {
            public static readonly GUIContent frameDebugger = EditorGUIUtility.TrTextContent("Open Frame Debugger", "Frame Debugger for current game view");
            public static readonly GUIContent noFrameDebugger = EditorGUIUtility.TrTextContent("Frame Debugger", "Open Frame Debugger (Current frame needs to be selected)");
        }

        internal enum ChartMode { Default, PipelineTiming, Coverage, InstanceCounts }

        static readonly string[] k_ChartModeNames =
        {
            L10n.Tr("Rendering"),
            L10n.Tr("GRD Pipeline"),
            L10n.Tr("GRD Coverage"),
            L10n.Tr("GRD Instances"),
        };

        const int k_DefaultOrderIndex = 2;
        static readonly string k_RenderCountersCategoryName = ProfilerCategory.Render.Name;
        static readonly ProfilerCounterData[] k_DefaultRenderAreaCounterNames =
        {
            new ProfilerCounterData() { m_Name = "Batches Count", m_Category = k_RenderCountersCategoryName },
            new ProfilerCounterData() { m_Name = "SetPass Calls Count", m_Category = k_RenderCountersCategoryName },
            new ProfilerCounterData() { m_Name = "Triangles Count", m_Category = k_RenderCountersCategoryName },
            new ProfilerCounterData() { m_Name = "Vertices Count", m_Category = k_RenderCountersCategoryName },
        };

        static readonly ProfilerCounterData[] k_GRDPipelineTimingCounters =
        {
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_DataCollection, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_BatchBuilding, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_CpuToGpuUpload, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_CullingSchedule, m_Category = GRDCounterNames.k_CategoryName },
        };

        static readonly ProfilerCounterData[] k_GRDCoverageCounters =
        {
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_GRDRenderers, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_ExcludedRenderers, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_NonRenderingRenderers, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_InactiveRenderers, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_CoveragePercent, m_Category = GRDCounterNames.k_CategoryName },
        };

        static readonly ProfilerCounterData[] k_GRDInstanceCounters =
        {
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_TotalInstances, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_VisibleInstances, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_DisabledRendererCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_LayerCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_FrustumCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_OcclusionCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_GpuOcclusionCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_LODGroupCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_SmallMeshCulled, m_Category = GRDCounterNames.k_CategoryName },
            new ProfilerCounterData() { m_Name = GRDCounterNames.k_OtherCulled, m_Category = GRDCounterNames.k_CategoryName },
        };

        [SerializeField] ChartMode m_ChartMode = ChartMode.Default;
        // Null = "no counters applied yet". Distinguishes a fresh module load from a no-op switch.
        ChartMode? m_AppliedChartMode;

        // GRD chart modes use a custom ProfilerCategory, so the chart must fetch counter values
        // by (category, name) instead of by ProfilerArea. Returning the invalid area sentinel
        // for those modes makes ChartModelBuilder skip the area-based query and fall back to the
        // per-counter category path.
        internal override ProfilerArea area =>
            m_ChartMode == ChartMode.Default ? ProfilerArea.Rendering : k_InvalidProfilerArea;
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartRendering";

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new RenderingDetailsViewController(ProfilerWindow, this);
        }

        internal override void LegacyModuleInitialize()
        {
            base.LegacyModuleInitialize();
            // Honor the serialized chart mode on module load. Without this, a non-Default mode
            // saved across sessions would render with Default counters until the user re-picked it.
            ApplyChartMode(m_ChartMode);
        }

        public override void DrawToolbar(Rect position)
        {
            if (UnityEditor.MPE.ProcessService.level != UnityEditor.MPE.ProcessLevel.Main)
                return;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Frame Debugger button
            if (GUILayout.Button(GUI.enabled
                ? Styles.frameDebugger
                : Styles.noFrameDebugger, EditorStyles.toolbarButtonLeft))
            {
                FrameDebuggerWindow.OpenWindowAndToggleEnabled();
            }

            GUILayout.FlexibleSpace();

            // Chart mode dropdown
            int currentMode = (int)m_ChartMode;
            int newMode = EditorGUILayout.Popup(currentMode, k_ChartModeNames, EditorStyles.toolbarDropDown, GUILayout.Width(120));
            if (newMode != currentMode)
                SwitchChartMode((ChartMode)newMode);

            EditorGUILayout.EndHorizontal();
        }

        // DrawDetailsView intentionally not overridden — CreateDetailsViewController above returns
        // a UIElements-based controller, so the base IMGUI path is never invoked.

        internal void SwitchChartMode(ChartMode mode)
        {
            if (m_AppliedChartMode == mode)
                return;
            m_ChartMode = mode;
            ApplyChartMode(mode);
        }

        void ApplyChartMode(ChartMode mode)
        {
            m_AppliedChartMode = mode;
            var counters = mode switch
            {
                ChartMode.PipelineTiming => new List<ProfilerCounterData>(k_GRDPipelineTimingCounters),
                ChartMode.Coverage => new List<ProfilerCounterData>(k_GRDCoverageCounters),
                ChartMode.InstanceCounts => new List<ProfilerCounterData>(k_GRDInstanceCounters),
                _ => new List<ProfilerCounterData>(k_DefaultRenderAreaCounterNames),
            };
            SetCounters(counters, CollectAllGRDCounters());
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            return new List<ProfilerCounterData>(k_DefaultRenderAreaCounterNames);
        }

        // Returned as detail counters in every chart mode so the "GPU Resident Drawer" category
        // stays auto-enabled while the module is active. Without this, switching to a GRD chart
        // mode would only show data for frames captured AFTER the switch — past frames would be
        // empty because the category wasn't being recorded.
        protected override List<ProfilerCounterData> CollectDefaultDetailCounters()
        {
            return CollectAllGRDCounters();
        }

        static List<ProfilerCounterData> CollectAllGRDCounters()
        {
            var list = new List<ProfilerCounterData>(
                k_GRDPipelineTimingCounters.Length + k_GRDCoverageCounters.Length + k_GRDInstanceCounters.Length);
            list.AddRange(k_GRDPipelineTimingCounters);
            list.AddRange(k_GRDCoverageCounters);
            list.AddRange(k_GRDInstanceCounters);
            return list;
        }
    }
}
