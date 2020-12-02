// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Profiling;

using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class NetworkingMessagesProfilerModule : ProfilerModuleBase
    {
        static public Action<Rect, IProfilerWindowController> DrawDetailsViewOverride = null;
        static public Func<List<ProfilerCounterData>> GetCustomChartCounters = null;

        const string k_IconName = "Profiler.NetworkMessages";
        const int k_DefaultOrderIndex = 8;
        static readonly string k_UnLocalizedName = "Network Messages";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        public NetworkingMessagesProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow,  k_UnLocalizedName, k_Name, k_IconName)
        {
            InitCounterOverride();
        }

        private void InitCounterOverride()
        {
            if (GetCustomChartCounters != null)
            {
                var chartCounters = GetCustomChartCounters.Invoke();
                SetCounters(chartCounters, chartCounters);
            }
        }

        public override ProfilerArea area => ProfilerArea.NetworkMessages;
        public override bool usesCounters => false;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartNetworkMessages";

        protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.m_SharedScale = true;
            return chart;
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            if (DrawDetailsViewOverride != null)
                DrawDetailsViewOverride.Invoke(position, m_ProfilerWindow);
            else
                DrawDetailsViewText(position);
        }
    }
}
