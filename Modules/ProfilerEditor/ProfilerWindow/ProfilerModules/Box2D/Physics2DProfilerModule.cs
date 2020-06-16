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
    internal class Physics2DProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.Physics2D";
        const int k_DefaultOrderIndex = 7;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("Physics (2D)");

        public Physics2DProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.Physics2D;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartPhysics2D";

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
