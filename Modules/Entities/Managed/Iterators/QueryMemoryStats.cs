// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    // A world's archetype and query counts, and the memory its queries use, for the Entities Memory profiler module.
    // Bytes are what the lists have allocated (their capacity), not only what they use. Hash maps are not counted.
    internal struct QueryMemoryStats
    {
        public int ArchetypeCount;
        public int EmptyArchetypeCount;
        public int QueryCount; // distinct queries, not EntityQuery handles
        public int CacheEntryCount;
        public int CombinedEntryCount;
        public int RecordCount; // query/archetype matches
        public long BudgetBytes; // query budget blocks in use: query data and type arrays
        public long CacheBytes; // world query cache: entries, descriptions, records and running totals
        public long ArchetypeSlotBytes; // archetype slot table and creation log

        public long TotalBytes => BudgetBytes + CacheBytes + ArchetypeSlotBytes;
    }
}
