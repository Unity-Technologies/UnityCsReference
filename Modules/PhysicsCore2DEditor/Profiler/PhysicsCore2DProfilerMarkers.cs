// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.U2D.PhysicsCore2D.Profiler
{
    static class PhysicsCore2DProfilerMarkers
    {
        // Counter names
        public const string k_WorldCountCounterName = "Total Worlds PhyCore2D";
        public const string k_BodyCountCounterName = "Total Bodies PhyCore2D";
        public const string k_ShapeCountCounterName = "Total Shapes PhyCore2D";
        public const string k_ContactCountCounterName = "Total Contacts PhyCore2D";
        public const string k_JointCountCounterName = "Total Joints PhyCore2D";
        public const string k_IslandCountCounterName = "Total Islands PhyCore2D";
        public const string k_StackUsedCounterName = "Total Stack Used PhyCore2D";
        public const string k_MemoryUsedCounterName = "Total Memory Used PhyCore2D";
        public const string k_TaskCountCounterName = "Total Tasks PhyCore2D";
        public const string k_BroadphaseHeightCounterName = "Broadphase Tree Height PhyCore2D";
        public const string k_StaticBroadphaseHeightCounterName = "Static Broadphase Tree Height PhyCore2D";

        [NoAutoStaticsCleanup]
        public static readonly Guid k_PhysicsCore2DProfilerProjectId = new Guid("4844E8B7-EB08-4D4B-B3E2-9B50F84AC562");
        // Metadata tags, which must stay in step with PhysicsCore2DProfiler.cpp on the native side.
        public const int k_PhysicsCore2DFrameDataTag = 0;
        public const int k_PhysicsCore2DWorldNamesTag = 1;

        // The markers the native module registers, read back from the profiler itself rather than listed
        // here. A list kept by hand drifts as markers are added and removed natively: a name that is no
        // longer registered resolves to nothing, and a marker missing from the list never appears at all.
        public static string[] markerNames => s_MarkerNames ??= BuildMarkerNames();

        // Every registered marker carrying the module's name prefix, sorted so the order does not depend on
        // the order the markers happened to register in. Counters are left out, since the view sums sample
        // times and a counter has none.
        static string[] BuildMarkerNames()
        {
            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);

            var names = new List<string>();

            foreach (var handle in handles)
            {
                var description = ProfilerRecorderHandle.GetDescription(handle);
                if ((description.Flags & MarkerFlags.Counter) != 0)
                    continue;

                if (description.Name.StartsWith(k_MarkerNamePrefix, StringComparison.Ordinal))
                    names.Add(description.Name);
            }

            names.Sort(StringComparer.Ordinal);
            return names.ToArray();
        }

        #region Internal

        const string k_MarkerNamePrefix = "PhysicsCore2D.";

        [NoAutoStaticsCleanup]
        static string[] s_MarkerNames;

        #endregion
    }
}
