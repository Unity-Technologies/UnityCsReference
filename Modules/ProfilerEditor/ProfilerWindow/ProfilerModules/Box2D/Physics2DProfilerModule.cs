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
    [ProfilerModuleMetadata("Physics (2D)", typeof(LocalizationResource), IconPath = "Profiler.Physics2D")]
    internal class Physics2DProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 7;

        internal override ProfilerArea area => ProfilerArea.Physics2D;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartPhysics2D";

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
