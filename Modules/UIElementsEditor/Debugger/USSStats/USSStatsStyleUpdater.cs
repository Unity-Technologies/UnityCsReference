// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements.Experimental.USSStats
{
    class USSStatsStyleUpdater : VisualTreeStyleUpdater<VisualTreeStyleUpdaterTraversal<USSStatsStyleProfiler>, USSStatsStyleProfiler>
    {
        public static List<StyleSheetProfilingResult> ProfileStyles(BaseVisualElementPanel elementPanel)
        {
            var updater = new USSStatsStyleUpdater();
            var previousUpdater = elementPanel.GetUpdater(VisualTreeUpdatePhase.Styles);

            elementPanel.SetUpdater(updater, VisualTreeUpdatePhase.Styles);
            updater.DirtyStyleSheets();

            ref USSStatsStyleProfiler profiler = ref StyleProfilerStorage<USSStatsStyleProfiler>.InstanceByRef;
            profiler.Initialize(elementPanel.visualTree);

            try
            {
                updater.ApplyStyles();

                var result = profiler.GetResults();

                return result;
            }
            finally
            {
                profiler.Clear();
                elementPanel.SetUpdater(previousUpdater, VisualTreeUpdatePhase.Styles);
            }
        }
    }
}

