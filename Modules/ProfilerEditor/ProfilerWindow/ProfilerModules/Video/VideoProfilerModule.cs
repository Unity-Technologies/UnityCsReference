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
    internal class VideoProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.Video";
        const int k_DefaultOrderIndex = 5;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("Video");

        public VideoProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName) {}

        public override ProfilerArea area => ProfilerArea.Video;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartVideo";

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
