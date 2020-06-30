// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEngine.Profiling;
using UnityEditor;
namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class VirtualTexturingProfilerModule : ProfilerModuleBase
    {
        [SerializeField]
        VirtualTexturingProfilerView m_VTProfilerView;

        const string k_IconName = "Profiler.VirtualTexturing";
        const int k_DefaultOrderIndex = 13;
        static readonly string k_Name = "Virtual Texturing Profiler";
        const string k_VTCountersCategoryName = "VirtualTexturing";

        static readonly string[] k_VirtualTexturingCounterNames =
        {
            "Required Tiles",
            "Max Cache Mip Bias",
            "Max Cache Demand",
            "Missing Streaming Tiles",
            "Missing Disk Data"
        };

        public VirtualTexturingProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName, Chart.ChartType.Line) {}

        public override ProfilerArea area => ProfilerArea.VirtualTexturing;
        protected override int defaultOrderIndex => k_DefaultOrderIndex;

        public override void OnEnable()
        {
            if (m_VTProfilerView == null)
            {
                m_VTProfilerView = new VirtualTexturingProfilerView();
            }
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            m_VTProfilerView?.DrawUIPane(m_ProfilerWindow);
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var chartCounters = new List<ProfilerCounterData>(k_VirtualTexturingCounterNames.Length);
            foreach (var counterName in k_VirtualTexturingCounterNames)
            {
                chartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = counterName,
                    m_Category = k_VTCountersCategoryName,
                });
            }

            return chartCounters;
        }
    }
}
