// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Networking
{
    [Serializable]
    // IconPath is a placeholder until the designed icon exists; the asset belongs in this module's
    // editor-resources package alongside the UXML and USS.
    [ProfilerModuleMetadata("Web Requests", IconPath = "Profiler.NetworkOperations",
        Tooltip = "Shows how many UnityWebRequest transfers are in flight, and how many have started, completed or failed.")]
    internal class WebRequestProfilerModule : ProfilerModule
    {
        // This module's own category, not a built-in one. Both constructors go through
        // GetOrCreateCategory, and WebRequestProfilerCounters.cpp creates the same name natively, so the
        // colour has to match on both sides - Other is the palette index of the native kOlive.
        [NoAutoStaticsCleanup]
        static readonly ProfilerCategory k_Category = new ProfilerCategory("Web Requests", ProfilerCategoryColor.Other);

        // Counter names must match those registered natively in
        // Modules/UnityWebRequest/Profiler/WebRequestProfilerCounters.cpp.
        [NoAutoStaticsCleanup] // fixed compile-time list of profiler counter descriptors
        static readonly ProfilerCounterDescriptor[] k_ChartCounters = new ProfilerCounterDescriptor[]
        {
            new ProfilerCounterDescriptor("Active Requests", k_Category),
            new ProfilerCounterDescriptor("Requests Started", k_Category),
            new ProfilerCounterDescriptor("Requests Completed", k_Category),
            new ProfilerCounterDescriptor("Requests Failed", k_Category),
        };

        // Categories to auto-enable while this module is active, so the counters are actually captured.
        [NoAutoStaticsCleanup]
        static readonly string[] k_AutoEnabledCategoryNames = new string[]
        {
            k_Category.Name,
        };

        public WebRequestProfilerModule()
            : base(k_ChartCounters, ProfilerModuleChartType.Line, k_AutoEnabledCategoryNames)
        {
        }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new WebRequestDetailsViewController(ProfilerWindow);
        }
    }
}
