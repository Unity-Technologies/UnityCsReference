// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.AdaptivePerformance;
namespace UnityEditor.AdaptivePerformance.Editor
{
    [Serializable]
    [ProfilerModuleMetadata("Adaptive Performance")]
    internal class AdaptivePerformanceProfilerModule : ProfilerModule
    {
        [NoAutoStaticsCleanup] // fixed compile-time list of profiler counter descriptors
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("CPU frametime", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("GPU frametime", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("CPU performance level", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("GPU performance level", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("Frametime", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("Temperature Level", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("Temperature Trend", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("CPU Utilization", ProfilerCategory.Scripts),
            new ProfilerCounterDescriptor("GPU Utilization", ProfilerCategory.Scripts),
        };

        // Specify a list of Profiler category names, which should be auto-enabled when the module is active.
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
            AdaptivePerformanceProfilerStats.AdaptivePerformanceProfilerCategory.Name,
        };

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new AdaptivePerformanceDetailsViewController(ProfilerWindow);
        }

        public AdaptivePerformanceProfilerModule() : base(k_ChartCounters, ProfilerModuleChartType.Line, k_AutoEnabledCategoryNames) {}
    }
}
