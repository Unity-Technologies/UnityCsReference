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

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Asset Loading", typeof(LocalizationResource), IconPath = "Profiler.AssetLoading")]
    internal class AssetLoadingProfilerModule : ProfilerModuleBase
    {
        AssetLoadingProfilerView m_AssetLoadingProfilerView;

        static readonly string[] k_FileIOChartCounterNames =
        {
            "Other Reads",
            "Texture Reads",
            "Virtual Texture Reads",
            "Mesh Reads",
            "Audio Reads",
            "Scripting Reads",
            "Entities Reads"
        };

        static readonly string k_FileIOCountersCategoryName = ProfilerCategory.Loading.Name;

        public AssetLoadingProfilerModule() : base(ProfilerModuleChartType.StackedArea) {}

        private protected override int defaultOrderIndex => 15;

        internal override void OnEnable()
        {
            base.OnEnable();
            if (m_AssetLoadingProfilerView == null)
            {
                m_AssetLoadingProfilerView = new AssetLoadingProfilerView();
            }
        }

        private protected override bool ReadActiveState()
        {
            return EditorPrefs.GetBool(activeStatePreferenceKey, false);
        }

        private protected override void SaveActiveState()
        {
            EditorPrefs.SetBool(activeStatePreferenceKey, active);
        }

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            var localizedTooltipFormat = LocalizationDatabase.GetLocalizedString("A chart showing performance counters related to '{0}'. These only include bytes read through the AsyncReadManager.");
            chart.Tooltip = string.Format(localizedTooltipFormat, DisplayName);
            return chart;
        }

        public override void DrawToolbar(Rect position)
        {
            m_AssetLoadingProfilerView.DrawToolbar(position);
        }

        public override void DrawDetailsView(Rect position)
        {
            m_AssetLoadingProfilerView?.DrawUIPane(ProfilerWindow);
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var chartCounters = new List<ProfilerCounterData>(k_FileIOChartCounterNames.Length);
            foreach (var counterName in k_FileIOChartCounterNames)
            {
                chartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = counterName,
                    m_Category = k_FileIOCountersCategoryName,
                });
            }

            return chartCounters;
        }
    }
}
