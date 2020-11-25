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
    internal class PhysicsProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.Physics";
        const int k_DefaultOrderIndex = 6;
        static readonly string k_UnLocalizedName = "Physics";
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString(k_UnLocalizedName);

        public PhysicsProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_UnLocalizedName, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.Physics;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartPhysics";

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
