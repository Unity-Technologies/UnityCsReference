// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
namespace UnityEditor.UIElements
{
    [Serializable]
    [ProfilerModuleMetadata("UI Toolkit", IconPath = "Profiler.UI")]
    internal class UIToolkitProfilerModule : ProfilerModule
    {
        // Specify a list of Profiler category names, which should be auto-enabled when the module is active.
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
            ProfilerCategory.UIToolkit.Name,
        };

        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("UpdatePanels", ProfilerCategory.UIToolkit),
            new ProfilerCounterDescriptor("RenderPanels", ProfilerCategory.UIToolkit),
        };

        public UIToolkitProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.StackedTimeArea, autoEnabledCategoryNames: k_AutoEnabledCategoryNames) { }
    }
}
