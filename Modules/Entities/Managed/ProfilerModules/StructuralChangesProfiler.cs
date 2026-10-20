// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
    // The recording half of the Structural Changes profiler module, which the entity store calls on every structural
    // change. The package's StructuralChangesProfilerReporting turns what it records into profiler counters and frame
    // metadata, and tells it which profiler category to record for.
    static partial class StructuralChangesProfiler
    {
        internal const string k_CategoryName = "Entities Structural Changes";
        internal const string k_CreateEntityCounterName = "Create Entity";
        internal const string k_DestroyEntityCounterName = "Destroy Entity";
        internal const string k_AddComponentCounterName = "Add Component";
        internal const string k_RemoveComponentCounterName = "Remove Component";
        internal const string k_SetSharedComponentCounterName = "Set Shared Component";
        const string k_Guid = "7e866afa654f4469aef462540c0192fa";

        sealed class SharedInitialized { internal static readonly SharedStatic<bool> Ref = SharedStatic<bool>.GetOrCreate<bool>(); }
        sealed class SharedCategory { internal static readonly SharedStatic<ushort> Ref = SharedStatic<ushort>.GetOrCreate<SharedCategory>(); }
        sealed class SharedElapsed { internal static readonly SharedStatic<ElapsedTotals> Ref = SharedStatic<ElapsedTotals>.GetOrCreate<SharedElapsed>(); }

        // Live, like the profiler's own state: changes made in the same frame the profiler is turned on are recorded.
        public static bool Enabled => SharedCategory.Ref.Data != 0 && ProfilerClock.IsCategoryRecording(SharedCategory.Ref.Data);
        public static Guid Guid => new Guid(k_Guid);

        static ref bool s_Initialized => ref SharedInitialized.Ref.Data;

        // Nanoseconds spent in each type of structural change since the reporting side last read them.
        internal struct ElapsedTotals
        {
            public long CreateEntity;
            public long DestroyEntity;
            public long AddComponent;
            public long RemoveComponent;
            public long SetSharedComponent;
        }

        internal static ref ElapsedTotals Elapsed => ref SharedElapsed.Ref.Data;

        // The reporting side's profiler category; 0 until it initializes, which keeps recording off.
        internal static void SetCategory(ushort categoryId) => SharedCategory.Ref.Data = categoryId;

        internal delegate void FlushChangesFn(ref UnsafeList<StructuralChangeData> changes);

        // Set by the package's StructuralChangesProfilerReporting; sends a recorder's changes to the profiler.
        [AutoStaticsCleanupOnCodeReload] internal static FlushChangesFn s_FlushChanges;

        public static void Initialize()
        {
            if (s_Initialized)
                return;

            Elapsed = default;
            s_Initialized = true;
        }

        public static void Shutdown()
        {
            if (!s_Initialized)
                return;

            SetCategory(0);
            s_Initialized = false;
        }
    }
}
