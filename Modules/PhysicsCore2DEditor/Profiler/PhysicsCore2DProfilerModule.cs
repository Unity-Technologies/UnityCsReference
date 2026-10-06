// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    [Serializable]
    [ProfilerModuleMetadata("Physics Core 2D", IconPath = "Profiler.Physics2D")]
    class PhysicsCore2DProfilerModule :ProfilerModule
    {

        [NoAutoStaticsCleanup]
        static readonly ProfilerCounterDescriptor[] k_Counters = new ProfilerCounterDescriptor[]
        {

            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_WorldCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Worlds"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_BodyCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Bodies"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_ShapeCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Shapes"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_ContactCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Contacts"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_JointCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Joints"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_IslandCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Islands"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_StackUsedCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Stack Used"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_MemoryUsedCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Memory Used"),
            new ProfilerCounterDescriptor(PhysicsCore2DProfilerMarkers.k_TaskCountCounterName, ProfilerCategory.PhysicsCore2D, displayName:"Total Tasks"),
        };

        public PhysicsCore2DProfilerModule()
            : base(k_Counters, ProfilerModuleChartType.Line) { }

        public override ProfilerModuleViewController CreateDetailsViewController()
        {
            return new UnityEditor.U2D.PhysicsCore2D.Profiler.PhysicsCore2DProfilerViewController(ProfilerWindow);
        }
    }
}
