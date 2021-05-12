// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEngine;
using UnityEditor.Profiling;
using UnityEditor;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Virtual Texturing", typeof(LocalizationResource), IconPath = "Profiler.VirtualTexturing")]
    internal class VirtualTexturingProfilerModule : ProfilerModuleBase
    {
        [SerializeReference]
        VirtualTexturingProfilerView m_VTProfilerView;

        const int k_DefaultOrderIndex = 13;
        static readonly string k_VTCountersCategoryName = ProfilerCategory.VirtualTexturing.Name;

        static readonly string[] k_VirtualTexturingCounterNames =
        {
            "Required Tiles",
            "Max Cache Mip Bias",
            "Max Cache Demand",
            "Missing Streaming Tiles",
            "Missing Disk Data"
        };

        internal override ProfilerArea area => ProfilerArea.VirtualTexturing;
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;

        internal override void OnEnable()
        {
            base.OnEnable();
            if (m_VTProfilerView == null)
            {
                m_VTProfilerView = new VirtualTexturingProfilerView();
            }
        }

        public override void DrawToolbar(Rect position)
        {
        }

        public override void DrawDetailsView(Rect position)
        {
            m_VTProfilerView?.DrawUIPane(ProfilerWindow);
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
