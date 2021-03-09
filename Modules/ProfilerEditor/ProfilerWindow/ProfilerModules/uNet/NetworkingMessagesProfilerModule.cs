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
        const string k_IconName = "Profiler.NetworkMessages";
        const int k_DefaultOrderIndex = 8;
        static readonly string k_UnLocalizedName = "Network Messages";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        static List<ProfilerCounterData> s_CounterData = new List<ProfilerCounterData>();

        public NetworkingMessagesProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow,  k_UnLocalizedName, k_Name, k_IconName)
        {
            InitCounterOverride();
        }

        private void InitCounterOverride()
        {
            if (NetworkingMessagesProfilerOverrides.getCustomChartCounters != null)
            {
                s_CounterData.Clear();

                var chartCounters = NetworkingMessagesProfilerOverrides.getCustomChartCounters.Invoke();
                if (chartCounters != null)
                {
                    // If the Capcity is the same value a re-alloc will not happen
                    s_CounterData.Capacity = chartCounters.Count;
                    chartCounters.ToProfilerCounter(s_CounterData);
                }

                SetCounters(s_CounterData, s_CounterData);
            }
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            if (NetworkingMessagesProfilerOverrides.getCustomChartCounters != null)
            {
                s_CounterData.Clear();

                var chartCounters = NetworkingMessagesProfilerOverrides.getCustomChartCounters.Invoke();
                if (chartCounters != null)
                {
                    // If the Capcity is the same value a re-alloc will not happen
                    s_CounterData.Capacity = chartCounters.Count;
                    chartCounters.ToProfilerCounter(s_CounterData);
                }

                return s_CounterData;
            }

            return base.CollectDefaultChartCounters();
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
            if (NetworkingMessagesProfilerOverrides.drawDetailsViewOverride != null)
                NetworkingMessagesProfilerOverrides.drawDetailsViewOverride.Invoke(position, m_ProfilerWindow.GetActiveVisibleFrameIndex());
            else
                DrawDetailsViewText(position);
        }
    }
}
