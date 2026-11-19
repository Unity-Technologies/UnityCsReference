// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    // The chunks a query matches, ignoring filters: every chunk of every record's archetype, in record order.
    // Position N in that sequence is the chunk's flat number. The chunks are read from the archetypes.
    [NoAlias]
    internal unsafe struct QueryChunks
    {
        [NoAlias, NativeDisableUnsafePtrRestriction] public MatchingArchetype** Records;
        public int RecordCount;
        [NoAlias, NativeDisableUnsafePtrRestriction] public EntityComponentStore* EntityComponentStore;
        // For each record, the flat number of its first chunk, plus the total at the end. Only set when
        // the cursor has to jump to arbitrary flat numbers (parallel jobs).
        [NoAlias, NativeDisableUnsafePtrRestriction] public int* RunningTotals;

        internal QueryChunks(UnsafeMatchingArchetypePtrList records)
        {
            Records = records.Ptr;
            RecordCount = records.Length;
            EntityComponentStore = records.entityComponentStore;
            RunningTotals = null;
        }

        // Uses a cache entry's running totals, brought up to date first. Jobs scheduled earlier may still be reading
        // them, so only values that changed are written: they change only when chunks or records are added or
        // removed, and the structural change that does that completes every job first.
        internal QueryChunks(UnsafeMatchingArchetypePtrList records, UnsafeList<int>* runningTotals)
        {
            Records = records.Ptr;
            RecordCount = records.Length;
            EntityComponentStore = records.entityComponentStore;
            RunningTotals = null;
            if (runningTotals->Length != RecordCount + 1)
                runningTotals->Resize(RecordCount + 1);
            UpdateRunningTotals(runningTotals->Ptr);
        }

        internal void UpdateRunningTotals(int* runningTotals)
        {
            var total = 0;
            for (int i = 0; i < RecordCount; ++i)
            {
                if (runningTotals[i] != total)
                    runningTotals[i] = total;
                total += Records[i]->Archetype->Chunks.Count;
            }
            if (runningTotals[RecordCount] != total)
                runningTotals[RecordCount] = total;
            RunningTotals = runningTotals;
        }

        // The number of chunks. Walks every record unless the running totals are set, so only callers that need
        // it pay for it.
        public int CalculateLength()
        {
            if (RunningTotals != null)
                return RunningTotals[RecordCount];
            var length = 0;
            for (int i = 0; i < RecordCount; ++i)
                length += Records[i]->Archetype->Chunks.Count;
            return length;
        }

        public QueryChunkCursor GetCursor() => new QueryChunkCursor(this);
    }

    // Walks QueryChunks one chunk at a time. Starts before the first chunk: call MoveNext before reading.
    [NoAlias]
    internal unsafe struct QueryChunkCursor
    {
        public int Index; // flat number of the current chunk
        public int RecordIndex;
        public int ChunkIndexInArchetype;
        [NoAlias] MatchingArchetype** m_Records;
        int m_RecordCount;
        [NoAlias] int* m_RunningTotals;
        [NoAlias] ArchetypeChunkData* m_ArchetypeChunks;
        int m_ArchetypeChunkCount;

        internal QueryChunkCursor(in QueryChunks chunks)
        {
            m_Records = chunks.Records;
            m_RecordCount = chunks.RecordCount;
            m_RunningTotals = chunks.RunningTotals;
            Index = -1;
            RecordIndex = -1;
            ChunkIndexInArchetype = 0;
            m_ArchetypeChunks = null;
            m_ArchetypeChunkCount = 0;
        }

        public MatchingArchetype* Record => m_Records[RecordIndex];
        public ChunkIndex Chunk => (*m_ArchetypeChunks)[ChunkIndexInArchetype];

        public bool MoveNext()
        {
            ++Index;
            ++ChunkIndexInArchetype;
            while (ChunkIndexInArchetype >= m_ArchetypeChunkCount)
            {
                if (RecordIndex + 1 >= m_RecordCount)
                {
                    RecordIndex = m_RecordCount;
                    m_ArchetypeChunkCount = 0;
                    return false;
                }
                ++RecordIndex;
                m_ArchetypeChunks = &m_Records[RecordIndex]->Archetype->Chunks;
                m_ArchetypeChunkCount = m_ArchetypeChunks->Count;
                ChunkIndexInArchetype = 0;
            }
            return true;
        }

        // Positions the cursor so that the next MoveNext lands on flat number index.
        public void SeekBefore(int index)
        {
            if (index == Index + 1)
                return;

            if (m_RunningTotals == null)
            {
                Index = -1;
                RecordIndex = -1;
                ChunkIndexInArchetype = 0;
                m_ArchetypeChunkCount = 0;
                while (Index + 1 < index && MoveNext())
                {
                }
                return;
            }

            // The last record whose first chunk is at or before index. Records without chunks share their
            // running total with the next record, so this always lands on a record that has chunks.
            int low = 0, high = m_RecordCount - 1, record = -1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (m_RunningTotals[mid] <= index)
                {
                    record = mid;
                    low = mid + 1;
                }
                else
                    high = mid - 1;
            }

            Index = index - 1;
            if (record < 0 || index >= m_RunningTotals[m_RecordCount])
            {
                RecordIndex = m_RecordCount;
                m_ArchetypeChunkCount = 0;
                ChunkIndexInArchetype = 0;
                return;
            }
            RecordIndex = record;
            m_ArchetypeChunks = &m_Records[record]->Archetype->Chunks;
            m_ArchetypeChunkCount = m_ArchetypeChunks->Count;
            ChunkIndexInArchetype = index - m_RunningTotals[record] - 1;
        }

        public void PrefetchNext()
        {
            if (Hint.Likely(ChunkIndexInArchetype + 1 < m_ArchetypeChunkCount))
                Unity.Burst.Intrinsics.Common.Prefetch(&EntityComponentStore.PerChunkArray.ChunkData[(*m_ArchetypeChunks)[ChunkIndexInArchetype + 1]], Unity.Burst.Intrinsics.Common.ReadWrite.Read);
        }
    }
}
