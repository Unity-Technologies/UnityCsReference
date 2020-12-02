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
    internal class GlobalIlluminationProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.GlobalIllumination";
        const int k_DefaultOrderIndex = 12;
        static readonly string k_UnLocalizedName = "Global Illumination";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        public GlobalIlluminationProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName, Chart.ChartType.StackedFill) {}

        public override ProfilerArea area => ProfilerArea.GlobalIllumination;
        public override bool usesCounters => false;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartGlobalIllumination";

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
