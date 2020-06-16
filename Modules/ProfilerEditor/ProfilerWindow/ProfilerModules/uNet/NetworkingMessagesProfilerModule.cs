// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class NetworkingMessagesProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.NetworkMessages";
        const int k_DefaultOrderIndex = 8;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("Network Messages");

        public NetworkingMessagesProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.NetworkMessages;

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
            DrawDetailsViewText(position);
        }
    }
}
