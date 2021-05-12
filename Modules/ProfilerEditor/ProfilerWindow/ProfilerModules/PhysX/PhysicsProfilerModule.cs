// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Physics", typeof(LocalizationResource), IconPath = "Profiler.Physics")]
    internal class PhysicsProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 6;

        internal override ProfilerArea area => ProfilerArea.Physics;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartPhysics";

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
