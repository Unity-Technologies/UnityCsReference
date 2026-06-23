// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEngine;

namespace UnityEditor.UIElements
{
    [Serializable]
    [ProfilerModuleMetadata("UI Toolkit Details", IconPath = "Profiler.UIToolkitDetails", Tooltip = "Shows detailed UI Toolkit panel component information and metrics.")]
    internal class UIToolkitDetailProfilerModule : ProfilerModule
    {
        const int k_DefaultOrderIndex = 10; // after UI Toolkit
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;

        // Specify a list of Profiler category names, which should be auto-enabled when the module is active.
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
            ProfilerCategory.UIToolkit.Name,
            ProfilerCategory.Render.Name,
        };

        // Render metrics
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("Batches Count", "Total number of batches rendered.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Draw Calls Count", "Total number of draw calls.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Vertices Count", "Total number of vertices.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Indices Count", "Total number of indices.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Hierarchy Version Changes", "Total number of hierarchy version changes since previous tick, summed across panels.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Repaint Version Changes", "Total number of version changes since previous repaint, summed across panels.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("VisualElement Count", "Total number of visual elements, summed across panels.", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("Event Count", "Total number of events dispatched (pointer, keyboard, navigation, and others), summed across panels.", ProfilerCategory.UIToolkit),
        };

        public UIToolkitDetailProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.Line, autoEnabledCategoryNames: k_AutoEnabledCategoryNames) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new UIToolkitDetailsProfilerModuleDetailsView(ProfilerWindow);
        }
    }
}
