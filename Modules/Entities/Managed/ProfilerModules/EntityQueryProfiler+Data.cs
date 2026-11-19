// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections;

namespace Unity.Entities
{
    partial class EntityQueryProfiler
    {
        // Frame metadata tags. WorldName and SystemName carry the package's EntitiesProfiler.WorldData and SystemData.
        public enum DataTag
        {
            QueryCall,
            QueryCreation,
            ArchetypeMatching,
            QueryDescription,
            WorldName,
            SystemName,
        }

        // A query is identified by its world and an id that is unique within that world. StartTimestamp is in
        // ProfilerClock ticks, and orders a world's calls, creations and archetype matching passes. SampleId is the
        // metadata of the profiler sample that wraps the record (see GetSampleMarkerName), 0 when there is none.
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct QueryCallData
        {
            public readonly SystemHandle ExecutingSystem;
            public readonly ulong WorldSequenceNumber;
            public readonly long StartTimestamp;
            public readonly long ElapsedNanoseconds;

            // Time the call spent waiting for running jobs to complete, which ElapsedNanoseconds includes.
            public readonly long JobWaitNanoseconds;
            public readonly int QueryId;
            public readonly Operation Operation;
            public readonly int SampleId;

            public QueryCallData(in SystemHandle executingSystem, ulong worldSequenceNumber, int queryId, Operation operation,
                long startTimestamp, long elapsed, long jobWaitElapsed, int sampleId)
            {
                SampleId = sampleId;
                ExecutingSystem = executingSystem;
                WorldSequenceNumber = worldSequenceNumber;
                QueryId = queryId;
                Operation = operation;
                StartTimestamp = startTimestamp;
                ElapsedNanoseconds = elapsed;
                JobWaitNanoseconds = jobWaitElapsed;
            }
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct QueryCreationData
        {
            public readonly SystemHandle ExecutingSystem;
            public readonly ulong WorldSequenceNumber;
            public readonly long StartTimestamp;
            public readonly long ElapsedNanoseconds;
            public readonly int QueryId;

            // 1 when the query data was built, 0 when an existing one with the same description was reused.
            public readonly int Built;
            public readonly int SampleId;

            public QueryCreationData(in SystemHandle executingSystem, ulong worldSequenceNumber, int queryId, long startTimestamp,
                long elapsed, bool built, int sampleId)
            {
                SampleId = sampleId;
                ExecutingSystem = executingSystem;
                WorldSequenceNumber = worldSequenceNumber;
                QueryId = queryId;
                StartTimestamp = startTimestamp;
                ElapsedNanoseconds = elapsed;
                Built = built ? 1 : 0;
            }
        }

        // One pass that tests new archetypes against every query of a world.
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct ArchetypeMatchingData
        {
            public readonly SystemHandle ExecutingSystem;
            public readonly ulong WorldSequenceNumber;
            public readonly long StartTimestamp;
            public readonly long ElapsedNanoseconds;
            public readonly int PassId;
            public readonly int ArchetypeCount;
            public readonly int QueryCount;
            public readonly int SampleId;

            public ArchetypeMatchingData(in SystemHandle executingSystem, ulong worldSequenceNumber, int passId, long startTimestamp,
                long elapsed, int archetypeCount, int queryCount, int sampleId)
            {
                SampleId = sampleId;
                ExecutingSystem = executingSystem;
                WorldSequenceNumber = worldSequenceNumber;
                PassId = passId;
                StartTimestamp = startTimestamp;
                ElapsedNanoseconds = elapsed;
                ArchetypeCount = archetypeCount;
                QueryCount = queryCount;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct QueryDescriptionData
        {
            public readonly ulong WorldSequenceNumber;
            public readonly int QueryId;
            readonly FixedString512Bytes m_Description;

            [ExcludeFromBurstCompatTesting("Returns managed string")]
            public string Description => m_Description.ToString();

            [ExcludeFromBurstCompatTesting("Takes managed string")]
            public QueryDescriptionData(ulong worldSequenceNumber, int queryId, string description)
            {
                WorldSequenceNumber = worldSequenceNumber;
                QueryId = queryId;
                m_Description = default;
                m_Description.CopyFromTruncated(description);
            }
        }
    }
}
