// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    // The world's matches for one query description, shared by every query in the world with that description.
    // Records are in archetype creation order, and their positions are keyed by RequiredTypes.
    internal unsafe struct QueryCacheEntry
    {
        public QueryDescription** DescriptionBlock;
        public ArchetypeQuery ArchetypeQuery; // points into the description; access modes are all ReadOnly
        public ComponentType* RequiredTypes;
        public int RequiredTypeCount;
        public UnsafeMatchingArchetypePtrList Records;
        public uint FirstUnseenVersion; // archetypes from this version on still have to be checked at the next catch-up
        public EntityQueryMask Mask; // created when a query first asks for it, then set on every new record's archetype
        public UnsafeList<int> RunningTotals; // see QueryChunks.RunningTotals; refreshed when a parallel job is scheduled
        public int QueryCount; // live queries using this entry, plus one for each combined entry that uses it

        public QueryDescription* Description => DescriptionBlock[0];
    }

    // The world's matches for a query with several descriptions. Each matching archetype gets one record, built from
    // the first description it matches, with positions keyed by RequiredTypes: the types every description requires.
    internal unsafe struct QueryCombinedEntry
    {
        public int* Entries; // the cache entry of each description, in description order
        public int EntryCount;
        public ComponentType* RequiredTypes; // access modes are all ReadOnly, except Exclude for enableable None types
        public int RequiredTypeCount;
        public UnsafeMatchingArchetypePtrList Records;
        public uint FirstUnseenVersion;
        public EntityQueryMask Mask;
        public UnsafeList<int> RunningTotals;
        public int QueryCount;
    }
}
