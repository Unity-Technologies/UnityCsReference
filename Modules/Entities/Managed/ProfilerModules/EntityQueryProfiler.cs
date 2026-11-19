// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
    // The module's side of the Entity Queries profiler module. Queries report what they do from their main-thread entry
    // points, query creation and archetype matching; the package's EntityQueryProfilerRecording records it through the
    // function it registers here, so the recording code only ships in builds where the profiler can run. The package's
    // EntityQueryProfilerReporting turns the records into profiler counters and frame metadata, and tells this which
    // profiler category to record for.
    static unsafe partial class EntityQueryProfiler
    {
        internal const string k_CategoryName = "Entities Queries";
        internal const string k_QueryCallsCounterName = "Query Calls";
        internal const string k_JobWaitsCounterName = "Job Waits";
        internal const string k_QueryCreationCounterName = "Query Creation";
        internal const string k_ArchetypeMatchingCounterName = "Archetype Matching";
        const string k_Guid = "fb571fb9734244b1831b517cb54ebc5f";

        sealed class SharedCategory { internal static readonly SharedStatic<ushort> Ref = SharedStatic<ushort>.GetOrCreate<SharedCategory>(); }
        sealed class SharedRecordFunction { internal static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<SharedRecordFunction>(); }

        // Live, like the profiler's own state: calls made in the same frame the profiler is turned on are recorded.
        public static bool Enabled => SharedCategory.Ref.Data != 0 && SharedRecordFunction.Ref.Data != IntPtr.Zero &&
            ProfilerClock.IsCategoryRecording(SharedCategory.Ref.Data);
        public static Guid Guid => new Guid(k_Guid);

        // Recorders are not thread-safe, so work done from jobs, such as in an ExclusiveEntityTransaction, is skipped.
        internal static bool ShouldRecord => Enabled && !JobsUtility.IsExecutingJob;

        // Records are wrapped in a profiler sample named after what they record, which places them in the CPU module and
        // gives the details view the samples they ran inside. The sample's one metadata value is the record's SampleId.
        internal static string GetSampleMarkerName(Operation operation) => "EntityQuery." + operation;
        internal const string k_CreationSampleMarkerName = "EntityQuery.CreateQuery";
        internal const string k_ArchetypeMatchingSampleMarkerName = "EntityQuery.MatchArchetypes";

        // The reporting side's profiler category; 0 until it initializes, which keeps recording off.
        internal static void SetCategory(ushort categoryId) => SharedCategory.Ref.Data = categoryId;

        internal enum EventKind
        {
            BeginCall,
            EndCall,
            BeginJobWait,
            EndJobWait,
            BeginCreation,
            MarkCreationBuilt,
            EndCreation,
            BeginArchetypeMatching,
            EndArchetypeMatching,
        }

        // One thing that happened, for the store's recorder. Each kind uses only some of the fields. The timestamps are
        // taken here, outside the call to the recorder, so the time that call takes isn't part of what gets recorded.
        internal struct Event
        {
            public EventKind Kind;
            public Operation Operation;
            public int QueryId;
            public int ArchetypeCount;
            public int QueryCount;
            public long StartTimestamp;
            public long EndTimestamp;
        }

        // Records an event in the store's recorder. Returns 1 from BeginJobWait when the wait counts, and 0 otherwise.
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int RecordFn(EntityComponentStore* store, Event* recordedEvent);

        // Set by the package's EntityQueryProfilerRecording when its code loads.
        internal static ref IntPtr RecordFunction => ref SharedRecordFunction.Ref.Data;

        internal delegate void StoreDisposedFn(EntityComponentStore* store);

        // Set by the package's EntityQueryProfilerReporting; sends a disposed store's records to the profiler and frees
        // its recorder.
        [AutoStaticsCleanupOnCodeReload] internal static StoreDisposedFn s_StoreDisposed;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Record(EntityComponentStore* store, Event recordedEvent)
        {
            var function = (delegate* unmanaged[Cdecl]<EntityComponentStore*, Event*, int>)SharedRecordFunction.Ref.Data;
            return function != null ? function(store, &recordedEvent) : 0;
        }

        // Begin functions return the start timestamp, taken after the recorder is called; End functions take the end
        // timestamp before calling it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long BeginCall(EntityComponentStore* store, Operation operation, int queryId)
        {
            Record(store, new Event { Kind = EventKind.BeginCall, Operation = operation, QueryId = queryId });
            return ProfilerClock.GetTimestamp();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void EndCall(EntityComponentStore* store, long startTimestamp) =>
            Record(store, new Event { Kind = EventKind.EndCall, StartTimestamp = startTimestamp, EndTimestamp = ProfilerClock.GetTimestamp() });

        // Returns 0 when the wait doesn't count, because it isn't inside a recorded call.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long BeginJobWait(EntityComponentStore* store) =>
            Record(store, new Event { Kind = EventKind.BeginJobWait }) != 0 ? ProfilerClock.GetTimestamp() : 0;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void EndJobWait(EntityComponentStore* store, long startTimestamp) =>
            Record(store, new Event { Kind = EventKind.EndJobWait, StartTimestamp = startTimestamp, EndTimestamp = ProfilerClock.GetTimestamp() });

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long BeginCreation(EntityComponentStore* store)
        {
            Record(store, new Event { Kind = EventKind.BeginCreation });
            return ProfilerClock.GetTimestamp();
        }

        // Called by the query implementation when the creation builds new query data instead of reusing one.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MarkCreationBuilt(EntityComponentStore* store) =>
            Record(store, new Event { Kind = EventKind.MarkCreationBuilt });

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EndCreation(EntityComponentStore* store, int queryId, long startTimestamp) =>
            Record(store, new Event { Kind = EventKind.EndCreation, QueryId = queryId, StartTimestamp = startTimestamp, EndTimestamp = ProfilerClock.GetTimestamp() });

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long BeginArchetypeMatching(EntityComponentStore* store)
        {
            Record(store, new Event { Kind = EventKind.BeginArchetypeMatching });
            return ProfilerClock.GetTimestamp();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EndArchetypeMatching(EntityComponentStore* store, int archetypeCount, int queryCount, long startTimestamp) =>
            Record(store, new Event { Kind = EventKind.EndArchetypeMatching, ArchetypeCount = archetypeCount, QueryCount = queryCount,
                StartTimestamp = startTimestamp, EndTimestamp = ProfilerClock.GetTimestamp() });

        [ExcludeFromBurstCompatTesting("Calls a managed delegate")]
        internal static void StoreDisposed(EntityComponentStore* store) => s_StoreDisposed?.Invoke(store);
    }
}
