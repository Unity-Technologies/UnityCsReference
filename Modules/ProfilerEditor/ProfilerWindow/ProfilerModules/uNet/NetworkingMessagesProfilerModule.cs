// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.Profiling.Editor;

using UnityEditor;
using UnityEditor.Profiling;

using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Network Messages", typeof(LocalizationResource), IconPath = "Profiler.NetworkMessages")]
    internal class NetworkingMessagesProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 8;

        static List<ProfilerCounterData> s_CounterData = new List<ProfilerCounterData>();

        public NetworkingMessagesProfilerModule() : base()
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

        internal override ProfilerArea area => ProfilerArea.NetworkMessages;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartNetworkMessages";

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
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
                NetworkingMessagesProfilerOverrides.drawDetailsViewOverride.Invoke(position, ProfilerWindow.GetActiveVisibleFrameIndex());
            else
                DrawDetailsViewText(position);
        }
    }
}
