// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    // A bundle of component safety handles implicitly referenced by an EntityQuery. This allows a query to
    // register its component dependencies when the query is used by a job.
    [NativeContainer]
    internal struct EntityQuerySafetyHandles
    {
        // All jobs that read a query's records (IJobChunk and the jobs behind the async EntityQuery methods) have an
        // EntityManager safety handle to ensure that BeforeStructuralChange throws an error if they are still running
        // (haven't been synced). A structural change can free the records of a disposed query.
        internal AtomicSafetyHandle m_Safety0;
        // Enableable components from query used to schedule job. To add more handles here, you must also increase
        // EntityQueryManager.MAX_ENABLEABLE_COMPONENTS_PER_QUERY.
        internal AtomicSafetyHandle m_SafetyEnableable1;
        internal AtomicSafetyHandle m_SafetyEnableable2;
        internal AtomicSafetyHandle m_SafetyEnableable3;
        internal AtomicSafetyHandle m_SafetyEnableable4;
        internal AtomicSafetyHandle m_SafetyEnableable5;
        internal AtomicSafetyHandle m_SafetyEnableable6;
        internal AtomicSafetyHandle m_SafetyEnableable7;
        internal AtomicSafetyHandle m_SafetyEnableable8;
        internal int m_SafetyReadOnlyCount;
        internal int m_SafetyReadWriteCount;

        internal unsafe EntityQuerySafetyHandles(EntityQueryImpl* queryImpl)
        {
            this = default; // workaround for CS0171 error (all fields must be fully assigned before control is returned)
            var queryData = queryImpl->_QueryData;
            m_Safety0 = queryImpl->SafetyHandles->GetEntityManagerSafetyHandle();
            m_SafetyReadOnlyCount = 1 + queryData->EnableableComponentTypeIndexCount; // +1 for EntityManager handle
            m_SafetyReadWriteCount = 0;
            fixed (AtomicSafetyHandle* pEnableableHandles = &m_SafetyEnableable1)
            {
                for (int i = 0; i < EntityQueryManager.MAX_ENABLEABLE_COMPONENTS_PER_QUERY; ++i)
                {
                    if (i < queryData->EnableableComponentTypeIndexCount)
                    {
                        pEnableableHandles[i] =
                            queryImpl->SafetyHandles->GetSafetyHandle(queryData->EnableableComponentTypeIndices[i],
                                true);
                        AtomicSafetyHandle.SetExclusiveWeak(ref pEnableableHandles[i], true);
                    }
                }
            }
        }
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct EntityQueryManager
    {
        [NoAutoStaticsCleanup] // EntitiesInitialization sets it before any world is created.
        internal static int EntityQueryDataChunkAllocatorBudgetInBytes = EntityComponentStore.kDefaultAllocatorBudgetInBytes;

        private ComponentDependencyManager*    m_DependencyManager;
        private BlockAllocator                 m_EntityQueryDataChunkAllocator;
        private UnsafePtrList<EntityQueryData> m_EntityQueryDatas;

        internal int EntityQueryDataAllocatorUsedBytes => m_EntityQueryDataChunkAllocator.AllocatedBytes;
        internal int EntityQueryDataAllocatorBudgetBytes => m_EntityQueryDataChunkAllocator.BudgetInBytes;
        internal const int MAX_ENABLEABLE_COMPONENTS_PER_QUERY = 8;

        private UntypedUnsafeParallelHashMap           m_EntityQueryDataCacheUntyped;
        internal int                           m_EntityQueryMasksAllocated;
        private TypeIndex m_disabledTypeIndex;
        private TypeIndex m_prefabTypeIndex;
        private TypeIndex m_systemInstanceTypeIndex;
        private TypeIndex m_chunkHeaderTypeIndex;

        private UnsafeList<QueryCacheEntry> m_QueryCacheEntries;
        private UntypedUnsafeParallelHashMap m_QueryCacheEntriesByHashUntyped;
        private UnsafeList<QueryCombinedEntry> m_QueryCombinedEntries;
        private MemoryLabel m_CacheLabel;
        private MemoryLabel m_CacheRecordsLabel;
        private byte m_HasUnusedCacheEntries; // set when an entry's last query goes away

        internal QueryCacheEntry* GetCacheEntry(int index)
        {
            if (index >= 0 && index < m_QueryCacheEntries.Length && m_QueryCacheEntries.Ptr[index].DescriptionBlock != null)
                return m_QueryCacheEntries.Ptr + index;
            return null;
        }

        internal int QueryCacheEntryCount
        {
            get
            {
                var count = 0;
                for (int e = 0; e < m_QueryCacheEntries.Length; ++e)
                {
                    if (m_QueryCacheEntries.Ptr[e].DescriptionBlock != null)
                        ++count;
                }
                for (int e = 0; e < m_QueryCombinedEntries.Length; ++e)
                {
                    if (m_QueryCombinedEntries.Ptr[e].Entries != null)
                        ++count;
                }
                return count;
            }
        }

        internal QueryMemoryStats GetMemoryStats(EntityComponentStore* ecs)
        {
            var stats = new QueryMemoryStats
            {
                ArchetypeCount = ecs->m_Archetypes.Length,
                QueryCount = m_EntityQueryDatas.Length,
                BudgetBytes = m_EntityQueryDataChunkAllocator.AllocatedBytes,
                ArchetypeSlotBytes = (long)ecs->m_Archetypes.Capacity * sizeof(Archetype*) +
                    (long)ecs->m_ArchetypeCreationLog.Capacity * sizeof(ArchetypeRef),
            };

            for (var i = 0; i < ecs->m_Archetypes.Length; ++i)
            {
                var archetype = ecs->m_Archetypes.Ptr[i];
                if (archetype->EntityCount == 0)
                    stats.EmptyArchetypeCount++;
            }

            stats.CacheBytes = (long)m_QueryCacheEntries.Capacity * sizeof(QueryCacheEntry) +
                (long)m_QueryCombinedEntries.Capacity * sizeof(QueryCombinedEntry);
            for (var i = 0; i < m_QueryCacheEntries.Length; ++i)
            {
                var entry = m_QueryCacheEntries.Ptr + i;
                if (entry->DescriptionBlock == null)
                    continue;
                stats.CacheEntryCount++;
                stats.RecordCount += entry->Records.Length;
                stats.CacheBytes += sizeof(QueryDescription*) + entry->Description->SizeInBytes +
                    math.max(entry->Description->TypeCount, 1) + (long)entry->RequiredTypeCount * sizeof(ComponentType) +
                    GetRecordBytes(entry->Records) + (long)entry->RunningTotals.Capacity * sizeof(int);
            }
            for (var i = 0; i < m_QueryCombinedEntries.Length; ++i)
            {
                var entry = m_QueryCombinedEntries.Ptr + i;
                if (entry->Entries == null)
                    continue;
                stats.CombinedEntryCount++;
                stats.RecordCount += entry->Records.Length;
                stats.CacheBytes += (long)entry->EntryCount * sizeof(int) + (long)entry->RequiredTypeCount * sizeof(ComponentType) +
                    GetRecordBytes(entry->Records) + (long)entry->RunningTotals.Capacity * sizeof(int);
            }
            return stats;
        }

        static long GetRecordBytes(in UnsafeMatchingArchetypePtrList records)
        {
            var bytes = records.AllocatedBytes;
            for (var i = 0; i < records.Length; ++i)
                bytes += records.Ptr[i]->AllocationSize;
            return bytes;
        }

        internal QueryCombinedEntry* GetCombinedEntry(int index)
        {
            if (index >= 0 && index < m_QueryCombinedEntries.Length && m_QueryCombinedEntries.Ptr[index].Entries != null)
                return m_QueryCombinedEntries.Ptr + index;
            return null;
        }

        // Finds the world's cache entry for this description, or creates it with records for every existing archetype that matches.
        int GetOrCreateCacheEntry(QueryDescription* description, EntityComponentStore* ecs)
        {
            ref var entriesByHash = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<uint, int>>(ref m_QueryCacheEntriesByHashUntyped);
            if (entriesByHash.TryGetFirstValue(description->TypesHash, out var index, out var iterator))
            {
                do
                {
                    var existing = m_QueryCacheEntries.Ptr[index].Description;
                    if (existing->SizeInBytes == description->SizeInBytes &&
                        UnsafeUtility.MemCmp(existing, description, description->SizeInBytes) == 0)
                        return index;
                }
                while (entriesByHash.TryGetNextValue(out index, ref iterator));
            }

            var entry = new QueryCacheEntry();
            entry.DescriptionBlock = QueryDescription.AllocateCopy(description);
            var copy = entry.Description;

            var readOnly = (byte*)Memory.Unmanaged.Allocate(math.max(copy->TypeCount, 1), 1, m_CacheLabel);
            UnsafeUtility.MemSet(readOnly, (byte)ComponentType.AccessMode.ReadOnly, copy->TypeCount);
            ulong allBloomFilterMask = 0;
            for (int i = 0; i < copy->AllCount; ++i)
                allBloomFilterMask |= TypeManager.GetTypeInfo(copy->All[i]).BloomFilterMask;
            entry.ArchetypeQuery = new ArchetypeQuery
            {
                All = copy->All, AllAccessMode = readOnly, AllCount = copy->AllCount, AllBloomFilterMask = allBloomFilterMask,
                Any = copy->Any, AnyAccessMode = readOnly, AnyCount = copy->AnyCount,
                None = copy->None, NoneAccessMode = readOnly, NoneCount = copy->NoneCount,
                Disabled = copy->Disabled, DisabledAccessMode = readOnly, DisabledCount = copy->DisabledCount,
                Absent = copy->Absent, AbsentAccessMode = readOnly, AbsentCount = copy->AbsentCount,
                Present = copy->Present, PresentAccessMode = readOnly, PresentCount = copy->PresentCount,
                Options = copy->Options,
            };

            var scratchSize = 2 * (copy->TypeCount + 1) * sizeof(ComponentType) + 64;
            var scratchBuffer = stackalloc byte[scratchSize];
            var scratchAllocator = new UnsafeScratchAllocator(scratchBuffer, scratchSize);
            var requiredTypes = CalculateRequiredComponentsFromQuery(ref scratchAllocator, &entry.ArchetypeQuery, 1, out entry.RequiredTypeCount);
            entry.RequiredTypes = (ComponentType*)CacheAllocate<ComponentType>(entry.RequiredTypeCount, requiredTypes);

            entry.Records = new UnsafeMatchingArchetypePtrList(ecs);
            for (int slot = 0; slot < ecs->m_Archetypes.Length; ++slot)
            {
                var archetype = ecs->m_Archetypes.Ptr[slot];
                if (archetype != null && IsMatchingArchetype(archetype, &entry.ArchetypeQuery))
                {
                    CheckRecordInCreationOrder(entry.Records, archetype);
                    entry.Records.Add(CreateRecord(archetype, ref entry.ArchetypeQuery, entry.RequiredTypes, entry.RequiredTypeCount));
                }
            }
            entry.FirstUnseenVersion = ecs->m_NextArchetypeVersion;
            entry.RunningTotals = new UnsafeList<int>(0, Allocator.Persistent);

            for (index = 0; index < m_QueryCacheEntries.Length; ++index)
            {
                if (m_QueryCacheEntries.Ptr[index].DescriptionBlock == null)
                    break;
            }
            if (index == m_QueryCacheEntries.Length)
                m_QueryCacheEntries.Add(entry);
            else
                m_QueryCacheEntries.Ptr[index] = entry;
            entriesByHash.Add(description->TypesHash, index);
            return index;
        }

        void* CacheAllocate<T>(int count, void* source) where T : unmanaged
        {
            var bytes = count * sizeof(T);
            if (bytes == 0)
                return null;
            var pointer = Memory.Unmanaged.Allocate(bytes, UnsafeUtility.AlignOf<T>(), m_CacheLabel);
            UnsafeUtility.MemCpy(pointer, source, bytes);
            return pointer;
        }

        // Builds the record of an archetype that matches archetypeQuery, with positions keyed by requiredComponents.
        MatchingArchetype* CreateRecord(Archetype* archetype, ref ArchetypeQuery archetypeQuery, ComponentType* requiredComponents, int requiredComponentsCount)
        {
            var match = MatchingArchetype.Create(m_CacheRecordsLabel, archetype, archetypeQuery, requiredComponentsCount);
            return FillRecord(match, ref archetypeQuery, requiredComponents, requiredComponentsCount);
        }

        // Adds records for the archetypes in the creation log. Runs at the end of every structural change that created archetypes.
        internal void CatchUpQueryCache(EntityComponentStore* ecs)
        {
            var log = ecs->m_ArchetypeCreationLog;
            for (int e = 0; e < m_QueryCacheEntries.Length; ++e)
            {
                var entry = m_QueryCacheEntries.Ptr + e;
                if (entry->DescriptionBlock == null)
                    continue;
                for (int i = 0; i < log.Length; ++i)
                {
                    // An entry created during this structural change has already seen the archetypes created before it.
                    if (log.Ptr[i].Version < entry->FirstUnseenVersion)
                        continue;

                    var archetype = ecs->GetArchetype(log.Ptr[i]);
                    if (archetype != null && IsMatchingArchetype(archetype, &entry->ArchetypeQuery))
                    {
                        CheckRecordInCreationOrder(entry->Records, archetype);
                        entry->Records.Add(CreateRecord(archetype, ref entry->ArchetypeQuery, entry->RequiredTypes, entry->RequiredTypeCount));
                        if (entry->Mask.IsCreated())
                            archetype->SetMask(entry->Mask);
                    }
                }
                entry->FirstUnseenVersion = ecs->m_NextArchetypeVersion;
            }

            for (int e = 0; e < m_QueryCombinedEntries.Length; ++e)
            {
                var combined = m_QueryCombinedEntries.Ptr + e;
                if (combined->Entries == null)
                    continue;
                for (int i = 0; i < log.Length; ++i)
                {
                    if (log.Ptr[i].Version < combined->FirstUnseenVersion)
                        continue;

                    var archetype = ecs->GetArchetype(log.Ptr[i]);
                    if (archetype != null)
                        AddCombinedRecordIfMatching(combined, archetype);
                }
                combined->FirstUnseenVersion = ecs->m_NextArchetypeVersion;
            }
        }

        // Finds the world's combined entry for these description entries and combined types, or creates it with records
        // for every existing archetype that matches. Few queries have several descriptions, so a linear search is enough.
        int GetOrCreateCombinedEntry(int* entries, int entryCount, ComponentType* requiredComponents, int requiredComponentCount, EntityComponentStore* ecs)
        {
            // Records only depend on which combined types are excluded, so the other access modes don't take part in the key.
            var requiredTypes = stackalloc ComponentType[requiredComponentCount];
            for (int i = 0; i < requiredComponentCount; ++i)
            {
                requiredTypes[i] = requiredComponents[i];
                if (requiredTypes[i].AccessModeType != ComponentType.AccessMode.Exclude)
                    requiredTypes[i].AccessModeType = ComponentType.AccessMode.ReadOnly;
            }

            for (int e = 0; e < m_QueryCombinedEntries.Length; ++e)
            {
                var existing = m_QueryCombinedEntries.Ptr + e;
                if (existing->Entries != null && existing->EntryCount == entryCount && existing->RequiredTypeCount == requiredComponentCount &&
                    UnsafeUtility.MemCmp(existing->Entries, entries, sizeof(int) * entryCount) == 0 &&
                    UnsafeUtility.MemCmp(existing->RequiredTypes, requiredTypes, sizeof(ComponentType) * requiredComponentCount) == 0)
                    return e;
            }

            var combined = new QueryCombinedEntry
            {
                Entries = (int*)CacheAllocate<int>(entryCount, entries),
                EntryCount = entryCount,
                RequiredTypes = (ComponentType*)CacheAllocate<ComponentType>(requiredComponentCount, requiredTypes),
                RequiredTypeCount = requiredComponentCount,
                Records = new UnsafeMatchingArchetypePtrList(ecs),
                RunningTotals = new UnsafeList<int>(0, Allocator.Persistent),
            };
            for (int slot = 0; slot < ecs->m_Archetypes.Length; ++slot)
            {
                var archetype = ecs->m_Archetypes.Ptr[slot];
                if (archetype != null)
                    AddCombinedRecordIfMatching(&combined, archetype);
            }
            combined.FirstUnseenVersion = ecs->m_NextArchetypeVersion;
            for (int i = 0; i < entryCount; ++i)
                m_QueryCacheEntries.Ptr[entries[i]].QueryCount++;

            int index;
            for (index = 0; index < m_QueryCombinedEntries.Length; ++index)
            {
                if (m_QueryCombinedEntries.Ptr[index].Entries == null)
                    break;
            }
            if (index == m_QueryCombinedEntries.Length)
                m_QueryCombinedEntries.Add(combined);
            else
                m_QueryCombinedEntries.Ptr[index] = combined;
            return index;
        }

        void AddCombinedRecordIfMatching(QueryCombinedEntry* combined, Archetype* archetype)
        {
            for (int i = 0; i < combined->EntryCount; ++i)
            {
                var entry = m_QueryCacheEntries.Ptr + combined->Entries[i];
                if (IsMatchingArchetype(archetype, &entry->ArchetypeQuery))
                {
                    CheckRecordInCreationOrder(combined->Records, archetype);
                    combined->Records.Add(CreateRecord(archetype, ref entry->ArchetypeQuery, combined->RequiredTypes, combined->RequiredTypeCount));
                    if (combined->Mask.IsCreated())
                        archetype->SetMask(combined->Mask);
                    return;
                }
            }
        }

        internal int* CreateCacheEntries(QueryDescription** descriptions, int descriptionCount, ComponentType* requiredComponents, int requiredComponentCount, EntityComponentStore* ecs)
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Query.EntityQuery", Allocator.Persistent);
            var slotCount = descriptionCount > 1 ? descriptionCount + 1 : descriptionCount;
            var cacheEntries = (int*)Memory.Unmanaged.Allocate(sizeof(int) * slotCount, 4, memoryLabel);
            for (int i = 0; i < descriptionCount; ++i)
            {
                cacheEntries[i] = GetOrCreateCacheEntry(descriptions[i], ecs);
                m_QueryCacheEntries.Ptr[cacheEntries[i]].QueryCount++;
            }
            if (descriptionCount > 1)
            {
                cacheEntries[descriptionCount] = GetOrCreateCombinedEntry(cacheEntries, descriptionCount, requiredComponents, requiredComponentCount, ecs);
                m_QueryCombinedEntries.Ptr[cacheEntries[descriptionCount]].QueryCount++;
            }
            return cacheEntries;
        }

        // Takes a reference to the cache entry of the builder's only description, for a native EntityQuery
        internal int AcquireCacheEntry(EntityQueryBuilder builder, EntityComponentStore* ecs)
        {
            builder.FinalizeQueryInternal();
            var buffer = stackalloc byte[2048];
            var scratchAllocator = new UnsafeScratchAllocator(buffer, 2048);
            var archetypeQuery = CreateArchetypeQueries(ref scratchAllocator, builder);
            var descriptions = QueryDescription.Allocate(archetypeQuery, 1);
            var index = GetOrCreateCacheEntry(descriptions[0], ecs);
            m_QueryCacheEntries.Ptr[index].QueryCount++;
            QueryDescription.Free(descriptions);
            return index;
        }

        internal void ReleaseCacheEntry(int index)
        {
            if (--m_QueryCacheEntries.Ptr[index].QueryCount == 0)
                m_HasUnusedCacheEntries = 1;
        }

        // The entries stay until the next structural change, so a query recreated before then finds them again.
        internal void ReleaseCacheEntries(int* cacheEntries, int descriptionCount)
        {
            for (int i = 0; i < descriptionCount; ++i)
            {
                if (--m_QueryCacheEntries.Ptr[cacheEntries[i]].QueryCount == 0)
                    m_HasUnusedCacheEntries = 1;
            }
            if (descriptionCount > 1 && --m_QueryCombinedEntries.Ptr[cacheEntries[descriptionCount]].QueryCount == 0)
                m_HasUnusedCacheEntries = 1;

            var memoryLabel = Memory.CreateLabel("Entities", "Query.EntityQuery", Allocator.Persistent);
            Memory.Unmanaged.Free(cacheEntries, memoryLabel);
        }

        // Frees the entries no query uses. Only call it when no job can be reading records, such as at the start of a
        // structural change. Entries with a mask stay, since nothing tells when the mask is no longer used.
        internal void FreeUnusedCacheEntries()
        {
            if (m_HasUnusedCacheEntries == 0)
                return;
            m_HasUnusedCacheEntries = 0;

            // Combined entries first, so the description entries they release are freed in the same pass.
            for (int e = 0; e < m_QueryCombinedEntries.Length; ++e)
            {
                var combined = m_QueryCombinedEntries.Ptr + e;
                if (combined->Entries == null || combined->QueryCount != 0 || combined->Mask.IsCreated())
                    continue;
                for (int i = 0; i < combined->EntryCount; ++i)
                    m_QueryCacheEntries.Ptr[combined->Entries[i]].QueryCount--;
                FreeCombinedEntry(combined);
            }

            for (int e = 0; e < m_QueryCacheEntries.Length; ++e)
            {
                var entry = m_QueryCacheEntries.Ptr + e;
                if (entry->DescriptionBlock == null || entry->QueryCount != 0 || entry->Mask.IsCreated())
                    continue;
                ref var entriesByHash = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<uint, int>>(ref m_QueryCacheEntriesByHashUntyped);
                entriesByHash.Remove(entry->Description->TypesHash, e);
                FreeCacheEntry(entry);
            }
        }

        void FreeRecords(UnsafeMatchingArchetypePtrList records)
        {
            for (int i = 0; i < records.Length; ++i)
                Memory.Unmanaged.Free(records.Ptr[i], m_CacheRecordsLabel);
            records.Dispose();
        }

        void FreeCacheEntry(QueryCacheEntry* entry)
        {
            FreeRecords(entry->Records);
            entry->RunningTotals.Dispose();
            Memory.Unmanaged.Free(entry->ArchetypeQuery.AllAccessMode, m_CacheLabel);
            Memory.Unmanaged.Free(entry->RequiredTypes, m_CacheLabel);
            QueryDescription.Free(entry->DescriptionBlock);
            *entry = default;
        }

        void FreeCombinedEntry(QueryCombinedEntry* combined)
        {
            FreeRecords(combined->Records);
            combined->RunningTotals.Dispose();
            Memory.Unmanaged.Free(combined->Entries, m_CacheLabel);
            Memory.Unmanaged.Free(combined->RequiredTypes, m_CacheLabel);
            *combined = default;
        }



        public static void Create(EntityQueryManager* queryManager, ComponentDependencyManager* dependencyManager)
        {
            queryManager->m_DependencyManager = dependencyManager;
            queryManager->m_EntityQueryDataChunkAllocator = new BlockAllocator(AllocatorManager.Persistent, EntityQueryDataChunkAllocatorBudgetInBytes);
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref queryManager->m_EntityQueryDataCacheUntyped);
            entityQueryCache = new UnsafeParallelMultiHashMap<int, int>(1024, Allocator.Persistent);
            queryManager->m_EntityQueryDatas = new UnsafePtrList<EntityQueryData>(0, Allocator.Persistent);
            queryManager->m_EntityQueryMasksAllocated = 0;
            queryManager->m_disabledTypeIndex = TypeManager.GetTypeIndex<Disabled>();
            queryManager->m_prefabTypeIndex = TypeManager.GetTypeIndex<Prefab>();
            queryManager->m_systemInstanceTypeIndex = TypeManager.GetTypeIndex<SystemInstance>();
            queryManager->m_chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();
            queryManager->m_QueryCacheEntries = new UnsafeList<QueryCacheEntry>(0, Allocator.Persistent);
            ref var entriesByHash = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<uint, int>>(ref queryManager->m_QueryCacheEntriesByHashUntyped);
            entriesByHash = new UnsafeParallelMultiHashMap<uint, int>(256, Allocator.Persistent);
            queryManager->m_QueryCombinedEntries = new UnsafeList<QueryCombinedEntry>(0, Allocator.Persistent);
            queryManager->m_CacheLabel = Memory.CreateLabel("Entities", "Query.Cache", Allocator.Persistent);
            queryManager->m_CacheRecordsLabel = Memory.CreateLabel("Entities", "Query.CacheRecords", Allocator.Persistent);
            queryManager->m_HasUnusedCacheEntries = 0;

        }

        public static void Destroy(EntityQueryManager* manager)
        {
            manager->Dispose();
        }

        void Dispose()
        {
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref m_EntityQueryDataCacheUntyped);
            entityQueryCache.Dispose();
            m_EntityQueryDatas.Dispose();
            for (int e = 0; e < m_QueryCacheEntries.Length; ++e)
            {
                if (m_QueryCacheEntries.Ptr[e].DescriptionBlock != null)
                    FreeCacheEntry(m_QueryCacheEntries.Ptr + e);
            }
            m_QueryCacheEntries.Dispose();
            ref var entriesByHash = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<uint, int>>(ref m_QueryCacheEntriesByHashUntyped);
            entriesByHash.Dispose();
            for (int e = 0; e < m_QueryCombinedEntries.Length; ++e)
            {
                if (m_QueryCombinedEntries.Ptr[e].Entries != null)
                    FreeCombinedEntry(m_QueryCombinedEntries.Ptr + e);
            }
            m_QueryCombinedEntries.Dispose();
            //@TODO: Need to wait for all job handles to be completed..
            m_EntityQueryDataChunkAllocator.Dispose();
        }

        ArchetypeQuery* CreateArchetypeQueries(ref UnsafeScratchAllocator unsafeScratchAllocator, ComponentType* requiredTypes, int count)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);

            for (int i = 0; i != count; i++)
            {
                if (requiredTypes[i].AccessModeType == ComponentType.AccessMode.Exclude)
                {
                    var noneType = ComponentType.ReadOnly(requiredTypes[i].TypeIndex);
                    builder.WithNone(&noneType, 1);
                }
                else
                {
                    builder.WithAll(&requiredTypes[i], 1);
                }
            }

            builder.FinalizeQueryInternal();

            var result = CreateArchetypeQueries(ref unsafeScratchAllocator, builder);

            builder.Dispose();

            return result;
        }

        internal void ConstructTypeArray(ref UnsafeScratchAllocator unsafeScratchAllocator, UnsafeList<ComponentType> types, out TypeIndex* outTypes, out byte* outAccessModes, out int outLength)
        {
            if (types.Length == 0)
            {
                outTypes = null;
                outAccessModes = null;
                outLength = 0;
            }
            else
            {
                outLength = types.Length;
                outTypes = (TypeIndex*)unsafeScratchAllocator.Allocate<TypeIndex>(types.Length);
                outAccessModes = (byte*)unsafeScratchAllocator.Allocate<byte>(types.Length);

                var sortedTypes = stackalloc ComponentType[types.Length];
                for (var i = 0; i < types.Length; ++i)
                    SortingUtilities.InsertSorted(sortedTypes, i, types[i]);

                for (int i = 0; i != types.Length; i++)
                {
                    outTypes[i] = sortedTypes[i].TypeIndex;
                    outAccessModes[i] = (byte)sortedTypes[i].AccessModeType;
                }
            }
        }

#pragma warning disable 0618 // Write groups are obsolete. They will be removed in a future release.
        void IncludeDependentWriteGroups(ComponentType type, ref UnsafeList<ComponentType> explicitList)
        {
            if (type.AccessModeType != ComponentType.AccessMode.ReadOnly)
                return;

            var typeInfo = TypeManager.GetTypeInfo(type.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;
            for (int i = 0; i < writeGroupCount; i++)
            {
                var excludedComponentType = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                if (explicitList.Contains(excludedComponentType))
                    continue;

                explicitList.Add(excludedComponentType);
                IncludeDependentWriteGroups(excludedComponentType, ref explicitList);
            }
        }

        private static ComponentType GetWriteGroupReadOnlyComponentType(TypeIndex* writeGroupTypes, int i)
        {
            // Need to get "Clean" TypeIndex from Type. Since TypeInfo.TypeIndex is not actually the index of the
            // type. (It includes other flags.) What is stored in WriteGroups is the actual index of the type.
            ref readonly var excludedType = ref TypeManager.GetTypeInfo(writeGroupTypes[i]);
            var excludedComponentType = ComponentType.ReadOnly(excludedType.TypeIndex);
            return excludedComponentType;
        }

        void ExcludeWriteGroups(ComponentType type, ref UnsafeList<ComponentType> noneList, UnsafeList<ComponentType> explicitList)
        {
            if (type.AccessModeType == ComponentType.AccessMode.ReadOnly)
                return;

            var typeInfo = TypeManager.GetTypeInfo(type.TypeIndex);
            var writeGroups = TypeManager.GetWriteGroups(typeInfo);
            var writeGroupCount = typeInfo.WriteGroupCount;
            for (int i = 0; i < writeGroupCount; i++)
            {
                var excludedComponentType = GetWriteGroupReadOnlyComponentType(writeGroups, i);
                if (noneList.Contains(excludedComponentType))
                    continue;
                if (explicitList.Contains(excludedComponentType))
                    continue;

                noneList.Add(excludedComponentType);
            }
        }
#pragma warning restore 0618

        // Plan to unmanaged EntityQueryManager
        // [X] Introduce EntityQueryBuilder and test it
        // [X] Change internals to take an EntityQueryBuilder as an in parameter
        // [X] Validate queryData in CreateArchetypeQueries
        // [x] Everyone calling this needs to convert managed stuff into unmanaged EQDBuilder
        // [x] Public overloads of APIs to offer an EQDBuilder option
        // [ ] Deprecate EntityQueryDesc
        ArchetypeQuery* CreateArchetypeQueries(ref UnsafeScratchAllocator unsafeScratchAllocator, in EntityQueryBuilder queryBuilder)
        {
            var types = queryBuilder._builderDataPtr->_typeData;
            var queryData = queryBuilder._builderDataPtr->_indexData;
            var outQuery = (ArchetypeQuery*)unsafeScratchAllocator.Allocate(sizeof(ArchetypeQuery) * queryData.Length, UnsafeUtility.AlignOf<ArchetypeQuery>());

            // we need to build out new lists of component types to mutate them for WriteGroups
            var allTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var anyTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var noneTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var disabledTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var absentTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var presentTypes = new UnsafeList<ComponentType>(32, Allocator.Temp);
            var entityTypeIndex = TypeManager.GetTypeIndex<Entity>();

            for (int q = 0; q != queryData.Length; q++)
            {
                outQuery[q].Options = queryData[q].Options;
                {
                    var typesAll = queryData[q].All;
                    for (int i = typesAll.Index; i < typesAll.Index + typesAll.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        allTypes.Add(types[i]);
                        if (types[i].TypeIndex == m_disabledTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeDisabledEntities;
                        else if (types[i].TypeIndex == m_prefabTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludePrefab;
                        else if (types[i].TypeIndex == m_chunkHeaderTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeMetaChunks;
                        else if (types[i].TypeIndex == m_systemInstanceTypeIndex)
                            outQuery[q].Options |= EntityQueryOptions.IncludeSystems;
                    }
                }

                {
                    var typesAny = queryData[q].Any;
                    for (int i = typesAny.Index; i < typesAny.Index + typesAny.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        anyTypes.Add(types[i]);
                    }
                }

                {
                    var typesNone = queryData[q].None;
                    for (int i = typesNone.Index; i < typesNone.Index + typesNone.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        var type = types[i];
                        // Can not use Assert.AreEqual here because it uses the (object, object) overload which
                        // boxes the enums being compared, and that can not be burst compiled.
                        Assert.IsTrue(ComponentType.AccessMode.ReadOnly == type.AccessModeType, "EntityQueryBuilder.None must convert ComponentType.AccessMode to ReadOnly");
                        noneTypes.Add(types[i]);
                    }
                }

                {
                    var typesDisabled = queryData[q].Disabled;
                    for (int i = typesDisabled.Index; i < typesDisabled.Index + typesDisabled.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        disabledTypes.Add(types[i]);
                    }
                }

                {
                    var typesAbsent = queryData[q].Absent;
                    for (int i = typesAbsent.Index; i < typesAbsent.Index + typesAbsent.Count; i++)
                    {
                        if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                        {
                            throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                        }
                        var type = types[i];
                        // Can not use Assert.AreEqual here because it uses the (object, object) overload which
                        // boxes the enums being compared, and that can not be burst compiled.
                        Assert.IsTrue(ComponentType.AccessMode.ReadOnly == type.AccessModeType, "EntityQueryBuilder.Absent must convert ComponentType.AccessMode to ReadOnly");
                        absentTypes.Add(types[i]);
                    }

                    {
                        var typesPresent = queryData[q].Present;
                        for (int i = typesPresent.Index; i < typesPresent.Index + typesPresent.Count; i++)
                        {
                            if (Hint.Unlikely(types[i].TypeIndex == entityTypeIndex))
                            {
                                throw new ArgumentException("Entity is not allowed in list of component types for EntityQuery");
                            }
                            presentTypes.Add(types[i]);
                        }
                    }
                }


                // Validate the queryBuilder has components declared in a consistent way
                EntityQueryBuilder.Validate(allTypes, anyTypes, noneTypes, disabledTypes, absentTypes, presentTypes);

#pragma warning disable 0618 // Write groups are obsolete. They will be removed in a future release.
                var isFilterWriteGroup = (queryData[q].Options & EntityQueryOptions.FilterWriteGroup) != 0;
#pragma warning restore 0618
                if (isFilterWriteGroup)
                {
                    // Each ReadOnly<type> in any or all
                    //   if has WriteGroup types,
                    //   - Recursively add to any (if not explicitly mentioned)
                    var explicitList = new UnsafeList<ComponentType>(allTypes.Length + anyTypes.Length +
                                                                     noneTypes.Length + disabledTypes.Length +
                                                                     absentTypes.Length + presentTypes.Length + 16, Allocator.Temp);
                    explicitList.AddRange(allTypes);
                    explicitList.AddRange(anyTypes);
                    explicitList.AddRange(noneTypes);
                    explicitList.AddRange(disabledTypes);
                    explicitList.AddRange(absentTypes);
                    explicitList.AddRange(presentTypes);

                    for (int i = 0; i < anyTypes.Length; i++)
                        IncludeDependentWriteGroups(anyTypes[i], ref explicitList);
                    for (int i = 0; i < allTypes.Length; i++)
                        IncludeDependentWriteGroups(allTypes[i], ref explicitList);
                    for (int i = 0; i < disabledTypes.Length; i++)
                        IncludeDependentWriteGroups(disabledTypes[i], ref explicitList);
                    for (int i = 0; i < presentTypes.Length; i++)
                        IncludeDependentWriteGroups(presentTypes[i], ref explicitList);

                    // Each ReadWrite<type> in any or all
                    //   if has WriteGroup types,
                    //     Add to none (if not exist in any or all or none)

                    for (int i = 0; i < anyTypes.Length; i++)
                        ExcludeWriteGroups(anyTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < allTypes.Length; i++)
                        ExcludeWriteGroups(allTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < disabledTypes.Length; i++)
                        ExcludeWriteGroups(disabledTypes[i], ref noneTypes, explicitList);
                    for (int i = 0; i < presentTypes.Length; i++)
                        ExcludeWriteGroups(presentTypes[i], ref noneTypes, explicitList);
                    explicitList.Dispose();
                }

                ConstructTypeArray(ref unsafeScratchAllocator, noneTypes, out outQuery[q].None,
                    out outQuery[q].NoneAccessMode, out outQuery[q].NoneCount);
                ConstructTypeArray(ref unsafeScratchAllocator, allTypes, out outQuery[q].All,
                    out outQuery[q].AllAccessMode, out outQuery[q].AllCount);
                ConstructTypeArray(ref unsafeScratchAllocator, anyTypes, out outQuery[q].Any,
                    out outQuery[q].AnyAccessMode, out outQuery[q].AnyCount);
                ConstructTypeArray(ref unsafeScratchAllocator, disabledTypes, out outQuery[q].Disabled,
                    out outQuery[q].DisabledAccessMode, out outQuery[q].DisabledCount);
                ConstructTypeArray(ref unsafeScratchAllocator, absentTypes, out outQuery[q].Absent,
                    out outQuery[q].AbsentAccessMode, out outQuery[q].AbsentCount);
                ConstructTypeArray(ref unsafeScratchAllocator, presentTypes, out outQuery[q].Present,
                    out outQuery[q].PresentAccessMode, out outQuery[q].PresentCount);

                ulong allBloomFilterMask = 0;
                for (int i = 0; i < outQuery[q].AllCount; ++i)
                    allBloomFilterMask |= TypeManager.GetTypeInfo(outQuery[q].All[i]).BloomFilterMask;
                outQuery[q].AllBloomFilterMask = allBloomFilterMask;

                allTypes.Clear();
                anyTypes.Clear();
                noneTypes.Clear();
                disabledTypes.Clear();
                absentTypes.Clear();
                presentTypes.Clear();
            }

            allTypes.Dispose();
            anyTypes.Dispose();
            noneTypes.Dispose();
            disabledTypes.Dispose();
            absentTypes.Dispose();
            presentTypes.Dispose();
            return outQuery;
        }

        internal static bool CompareQueryArray(in EntityQueryBuilder builder, EntityQueryBuilder.ComponentIndexArray arr, TypeIndex* typeArray, byte* accessModeArray, int typeArrayCount)
        {
            int arrCount = arr.Count;
            if (typeArrayCount != arrCount)
                return false;

            var sortedTypes = stackalloc ComponentType[arrCount];
            var types = builder._builderDataPtr->_typeData;
            for (var i = 0; i < arrCount; ++i)
            {
                SortingUtilities.InsertSorted(sortedTypes, i, types[arr.Index + i]);
            }

            for (var i = 0; i < arrCount; ++i)
            {
                if (typeArray[i] != sortedTypes[i].TypeIndex || accessModeArray[i] != (byte)sortedTypes[i].AccessModeType)
                    return false;
            }

            return true;
        }


        public static bool CompareQuery(in EntityQueryBuilder queryBuilder, EntityQueryData* queryData)
        {
            queryBuilder.FinalizeQueryInternal();
            var indexData = queryBuilder._builderDataPtr->_indexData;
            int count = indexData.Length;
            if (queryData->ArchetypeQueryCount != count)
                return false;

            for (int i = 0; i != count; i++)
            {
                ref var archetypeQuery = ref queryData->ArchetypeQueries[i];
                var q = indexData[i];

                if (!CompareQueryArray(queryBuilder, q.All, archetypeQuery.All, archetypeQuery.AllAccessMode, archetypeQuery.AllCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.None, archetypeQuery.None, archetypeQuery.NoneAccessMode, archetypeQuery.NoneCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Any, archetypeQuery.Any, archetypeQuery.AnyAccessMode, archetypeQuery.AnyCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Disabled, archetypeQuery.Disabled, archetypeQuery.DisabledAccessMode, archetypeQuery.DisabledCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Absent, archetypeQuery.Absent, archetypeQuery.AbsentAccessMode, archetypeQuery.AbsentCount))
                    return false;
                if (!CompareQueryArray(queryBuilder, q.Present, archetypeQuery.Present, archetypeQuery.PresentAccessMode, archetypeQuery.PresentCount))
                    return false;
                if (q.Options != archetypeQuery.Options)
                    return false;
            }

            return true;
        }

        private int IntersectSortedComponentIndexArrays(ComponentType* arrayA, int arrayACount, ComponentType* arrayB, int arrayBCount, ComponentType* outArray)
        {
            var intersectionCount = 0;

            var i = 0;
            var j = 0;
            while (i < arrayACount && j < arrayBCount)
            {
                if (arrayA[i] < arrayB[j])
                    i++;
                else if (arrayB[j] < arrayA[i])
                    j++;
                else
                {
                    outArray[intersectionCount] = arrayB[j];
                    intersectionCount++;
                    i++;
                    j++;
                }
            }

            return intersectionCount;
        }

        // Calculates the intersection of "All", "Disabled", and "Present" arrays from the provided ArchetypeQuery objects.
        // "None" types are also considered, if the type is an enableable component; see DOTS-9730.
        // The intersection is returned as a new array of ComponentType objects, which must be freed by the caller.
        // The Entity component is always required (and will be returned as the first element of the output array), so the
        // return value will never be null (unless the allocator is out of memory).
        private ComponentType* CalculateRequiredComponentsFromQuery(ref UnsafeScratchAllocator allocator, ArchetypeQuery* queries, int queryCount, out int outRequiredComponentsCount)
        {
            // Populate and sort a combined array of required component types and their access modes from the first ArchetypeQuery
            var maxIntersectionCount = queries[0].AllCount + queries[0].DisabledCount + queries[0].PresentCount + queries[0].NoneCount;
            // The first required component is always Entity.
            var outRequiredComponents = (ComponentType*)allocator.Allocate<ComponentType>(maxIntersectionCount+1);
            outRequiredComponents[0] = ComponentType.ReadWrite<Entity>();
            // If Entity is the only required component at this point, we're done; no need to look at any further ArchetypeQueries.
            if (maxIntersectionCount == 0)
            {
                outRequiredComponentsCount = 1;
                return outRequiredComponents;
            }
            var intersectionComponents = outRequiredComponents + 1;
            int currentIntersectionCount = 0;
            for (int j = 0; j < queries[0].AllCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].All[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].AllAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].DisabledCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].Disabled[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].DisabledAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].PresentCount; ++j)
            {
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].Present[j],
                    AccessModeType = (ComponentType.AccessMode)queries[0].PresentAccessMode[j],
                };
            }
            for (int j = 0; j < queries[0].NoneCount; ++j)
            {
                // If T is an enableable component used in WithNone, we conservatively add a T as a
                // required excluded (??) component. This does not mean T is required to be present in matching
                // archetypes (see the branch handling Exclude in AddArchetypeIfMatching), but it does ensure that
                // it's added as a read dependency on any systems that use this query.
                if (!TypeManager.IsEnableable(queries[0].None[j]))
                    continue;
                intersectionComponents[currentIntersectionCount++] = new ComponentType
                {
                    TypeIndex = queries[0].None[j],
                    AccessModeType = ComponentType.AccessMode.Exclude,
                };
            }
            NativeSortExtension.Sort(intersectionComponents, currentIntersectionCount);

            // For each additional ArchetypeQuery, create the same sorted array of component types, and reduce the
            // original array to the intersection of these two arrays.
            int maxQueryRequiredCount = 0;
            for (int i = 1; i < queryCount; ++i)
            {
                maxQueryRequiredCount = math.max(maxQueryRequiredCount,
                    queries[i].AllCount + queries[i].DisabledCount + queries[i].PresentCount + queries[i].NoneCount);
            }
            var queryRequiredTypes = (ComponentType*)allocator.Allocate<ComponentType>(maxQueryRequiredCount);
            for (int i = 1; i < queryCount; ++i)
            {
                int queryRequiredCount = 0;
                for (int j = 0; j < queries[i].AllCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].All[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].AllAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[i].DisabledCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].Disabled[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].DisabledAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[i].PresentCount; ++j)
                {
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[i].Present[j],
                        AccessModeType = (ComponentType.AccessMode)queries[i].PresentAccessMode[j],
                    };
                }
                for (int j = 0; j < queries[0].NoneCount; ++j)
                {
                    // If T is an enableable component used in WithNone, we conservatively add a T as a
                    // required excluded (??) component. This does not mean T is required to be present in matching
                    // archetypes (see the branch handling Exclude in AddArchetypeIfMatching), but it does ensure that
                    // it's added as a read dependency on any systems that use this query.
                    if (!TypeManager.IsEnableable(queries[0].None[j]))
                        continue;
                    queryRequiredTypes[queryRequiredCount++] = new ComponentType
                    {
                        TypeIndex = queries[0].None[j],
                        AccessModeType = ComponentType.AccessMode.Exclude,
                    };
                }
                NativeSortExtension.Sort(queryRequiredTypes, queryRequiredCount);
                currentIntersectionCount = IntersectSortedComponentIndexArrays(intersectionComponents, currentIntersectionCount,
                    queryRequiredTypes, queryRequiredCount, intersectionComponents);
            }
            outRequiredComponentsCount = currentIntersectionCount + 1; // again, the +1 is for the Entity type at outRequiredComponents[0]
            return outRequiredComponents;
        }

        [ExcludeFromBurstCompatTesting("Takes managed array")]
        internal static void ConvertToEntityQueryBuilder(ref EntityQueryBuilder builder, EntityQueryDesc[] queryDesc)
        {
            for (int q = 0; q != queryDesc.Length; q++)
            {
                ref var desc = ref queryDesc[q];
                fixed (ComponentType* allTypes = desc.All)
                {
                    builder.WithAll(allTypes, desc.All.Length);
                }
                fixed (ComponentType* anyTypes = desc.Any)
                {
                    builder.WithAny(anyTypes, desc.Any.Length);
                }
                fixed (ComponentType* noneTypes = desc.None)
                {
                    builder.WithNone(noneTypes, desc.None.Length);
                }
                fixed (ComponentType* disabledTypes = desc.Disabled)
                {
                    builder.WithDisabled(disabledTypes, desc.Disabled.Length);
                }
                fixed (ComponentType* absentTypes = desc.Absent)
                {
                    builder.WithAbsent(absentTypes, desc.Absent.Length);
                }
                fixed (ComponentType* presentTypes = desc.Present)
                {
                    builder.WithPresent(presentTypes, desc.Present.Length);
                }

                builder.WithOptions(desc.Options);
                builder.FinalizeQueryInternal();
            }
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access, EntityQueryBuilder query)
        {
            int createdQueryId = -1;
            using var creationScope = new EntityQueryProfiler.CreationScope(access->EntityComponentStore, &createdQueryId);
            query.FinalizeQueryInternal();

            var buffer = stackalloc byte[2048];
            var scratchAllocator = new UnsafeScratchAllocator(buffer, 2048);
            var archetypeQuery = CreateArchetypeQueries(ref scratchAllocator, query);

            var indexData = query._builderDataPtr->_indexData;
            var outRequiredComponents = CalculateRequiredComponentsFromQuery(ref scratchAllocator, archetypeQuery, indexData.Length, out var outRequiredComponentsCount);
            var result = CreateEntityQuery(access, archetypeQuery, indexData.Length, outRequiredComponents, outRequiredComponentsCount);
            createdQueryId = result.__impl->_QueryData->QueryDataIndex;
            return result;
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access, ComponentType* inRequiredComponents, int inRequiredComponentsCount)
        {
            int createdQueryId = -1;
            using var creationScope = new EntityQueryProfiler.CreationScope(access->EntityComponentStore, &createdQueryId);
            var buffer = stackalloc byte[1024];
            var scratchAllocator = new UnsafeScratchAllocator(buffer, 1024);
            var archetypeQueries = CreateArchetypeQueries(ref scratchAllocator, inRequiredComponents, inRequiredComponentsCount);
            var outRequiredComponents = CalculateRequiredComponentsFromQuery(ref scratchAllocator, archetypeQueries, 1, out var outRequiredComponentsCount);
            var result = CreateEntityQuery(access, archetypeQueries, 1, outRequiredComponents, outRequiredComponentsCount);
            createdQueryId = result.__impl->_QueryData->QueryDataIndex;
            return result;
        }

        bool Matches(EntityQueryData* queryData, ArchetypeQuery* archetypeQueries, int archetypeQueryCount,
            ComponentType* requiredComponents, int requiredComponentsCount)
        {
            if (requiredComponentsCount != queryData->RequiredComponentsCount)
                return false;
            if (archetypeQueryCount != queryData->ArchetypeQueryCount)
                return false;
            if (requiredComponentsCount > 0 && UnsafeUtility.MemCmp(requiredComponents, queryData->RequiredComponents, sizeof(ComponentType) * requiredComponentsCount) != 0)
                return false;
            for (var i = 0; i < archetypeQueryCount; ++i)
                if (!archetypeQueries[i].Equals(queryData->ArchetypeQueries[i]))
                    return false;
            return true;
        }

        void* ChunkAllocate<T>(int count = 1, void *source = null) where T : struct
        {
            var bytes = count * UnsafeUtility.SizeOf<T>();
            if (bytes == 0)
                return null;
            var pointer = m_EntityQueryDataChunkAllocator.Allocate(bytes, UnsafeUtility.AlignOf<T>());
            if (source != null)
                UnsafeUtility.MemCpy(pointer, source, bytes);
            return pointer;
        }

        public EntityQuery CreateEntityQuery(EntityDataAccess* access,
            ArchetypeQuery* archetypeQueries,
            int archetypeQueryCount,
            ComponentType* requiredComponents,
            int requiredComponentCount)
        {
            //@TODO: Validate that required types is subset of archetype filters all...

            int hash = (int)math.hash(requiredComponents, requiredComponentCount * sizeof(ComponentType));
            for (var i = 0; i < archetypeQueryCount; ++i)
                hash = hash * 397 ^ archetypeQueries[i].GetHashCode();
            EntityQueryData* queryData = null;
            ref var entityQueryCache = ref UnsafeUtility.As<UntypedUnsafeParallelHashMap, UnsafeParallelMultiHashMap<int, int>>(ref m_EntityQueryDataCacheUntyped);

            if (entityQueryCache.TryGetFirstValue(hash, out var entityQueryIndex, out var iterator))
            {
                do
                {
                    var possibleMatch = m_EntityQueryDatas.Ptr[entityQueryIndex];
                    if (Matches(possibleMatch, archetypeQueries, archetypeQueryCount, requiredComponents, requiredComponentCount))
                    {
                        queryData = possibleMatch;
                        break;
                    }
                }
                while (entityQueryCache.TryGetNextValue(out entityQueryIndex, ref iterator));
            }

            if (queryData == null)
            {
                if (EntityQueryProfiler.ShouldRecord)
                    EntityQueryProfiler.MarkCreationBuilt(access->EntityComponentStore);
                // Validate input archetype queries
                bool queryIgnoresEnabledBits =
                    (archetypeQueries[0].Options & EntityQueryOptions.IgnoreComponentEnabledState) != 0;
                for (int iAQ = 1; iAQ < archetypeQueryCount; ++iAQ)
                {
                    bool hasIgnoreEnabledBitsFlag = (archetypeQueries[iAQ].Options & EntityQueryOptions.IgnoreComponentEnabledState) != 0;
                    if (hasIgnoreEnabledBitsFlag != queryIgnoresEnabledBits)
                        throw new ArgumentException(
                            $"All EntityQueryOptions passed to CreateEntityQuery() must have the same value for the IgnoreComponentEnabledState flag");
                }

                // count & identify enableable component types
                int totalComponentCount = 0;
                for (int iAQ = 0; iAQ < archetypeQueryCount; ++iAQ)
                {
                    totalComponentCount += archetypeQueries[iAQ].AllCount + archetypeQueries[iAQ].AnyCount +
                                           archetypeQueries[iAQ].NoneCount + archetypeQueries[iAQ].DisabledCount +
                                           archetypeQueries[iAQ].AbsentCount + archetypeQueries[iAQ].PresentCount;
                }

                var allEnableableTypeIndices = new NativeList<TypeIndex>(totalComponentCount, Allocator.Temp);
                var enableableTypesInAny = new NativeList<ComponentType>(totalComponentCount, Allocator.Temp);
                for (int iAQ = 0; iAQ < archetypeQueryCount; ++iAQ)
                {
                    ref ArchetypeQuery aq = ref archetypeQueries[iAQ];
                    for (int i = 0; i < aq.AllCount; ++i)
                    {
                        if (aq.All[i].IsEnableable)
                            allEnableableTypeIndices.Add(aq.All[i]);
                    }
                    for (int i = 0; i < aq.NoneCount; ++i)
                    {
                        if (aq.None[i].IsEnableable)
                            allEnableableTypeIndices.Add(aq.None[i]);
                    }
                    for (int i = 0; i < aq.AnyCount; ++i)
                    {
                        if (aq.Any[i].IsEnableable)
                        {
                            allEnableableTypeIndices.Add(aq.Any[i]);
                            // We need a list of enableable types in all the query's Any lists below.
                            // Enableable types in the Any list are not in requiredComponents (by definition
                            // they are not required to be present), but evaluating the query implicitly
                            // requires reading their enabled bits if they are present. So, the query must
                            // have a read dependency on these types to avoid safety errors.
                            enableableTypesInAny.Add(ComponentType.ReadOnly(aq.Any[i]));
                        }
                    }
                    for (int i = 0; i < aq.DisabledCount; ++i)
                    {
                        // All "Disabled" types must be enableable. We'll enforce that elsewhere.
                        if (!aq.Disabled[i].IsEnableable)
                            throw new InvalidOperationException($"Component type {aq.Disabled[i]} passed with WithDisabled() is not an enableable component");
                        allEnableableTypeIndices.Add(aq.Disabled[i]);
                    }
                    // absent components are never present in a matching archetype, so it doesn't matter if they're
                    // enableable or not.
                    // present components are always present in a matching archetype, so it doesn't matter if they're
                    // enableable or not.
                }
                // eliminate duplicate type indices
                if (allEnableableTypeIndices.Length > 0)
                {
                    allEnableableTypeIndices.Sort();
                    int lastUniqueIndex = 0;
                    for (int i = 1; i < allEnableableTypeIndices.Length; ++i)
                    {
                        if (allEnableableTypeIndices[i] != allEnableableTypeIndices[lastUniqueIndex])
                        {
                            allEnableableTypeIndices[++lastUniqueIndex] = allEnableableTypeIndices[i];
                        }
                    }
                    allEnableableTypeIndices.Length = lastUniqueIndex + 1;
                    // This limit matches the number of safety handles for enableable types we store in job structs.
                    if (allEnableableTypeIndices.Length > MAX_ENABLEABLE_COMPONENTS_PER_QUERY)
                        throw new ArgumentException(
                            "EntityQuery objects may not reference more than 8 enableable components");
                }
                if (enableableTypesInAny.Length > 0)
                {
                    enableableTypesInAny.Sort();
                    int lastUniqueIndex = 0;
                    for (int i = 1; i < enableableTypesInAny.Length; ++i) {
                        if (enableableTypesInAny[i] != enableableTypesInAny[lastUniqueIndex]) {
                            enableableTypesInAny[++lastUniqueIndex] = enableableTypesInAny[i];
                        }
                    }
                    enableableTypesInAny.Length = lastUniqueIndex + 1;
                }
                // Allocate and populate query data
                queryData = (EntityQueryData*)ChunkAllocate<EntityQueryData>();
                queryData->RequiredComponentsCount = requiredComponentCount;
                queryData->RequiredComponents = (ComponentType*)ChunkAllocate<ComponentType>(requiredComponentCount, requiredComponents);
                queryData->EnableableComponentTypeIndexCount = queryIgnoresEnabledBits ? 0 : allEnableableTypeIndices.Length;
                queryData->EnableableComponentTypeIndices = (TypeIndex*)ChunkAllocate<TypeIndex>(allEnableableTypeIndices.Length, allEnableableTypeIndices.GetUnsafeReadOnlyPtr());
                queryData->HasEnableableComponents = (allEnableableTypeIndices.Length > 0 && !queryIgnoresEnabledBits) ? (byte)1 : (byte)0;

                bool canMatchGameObjects = false;
                for (var i = 0; i < archetypeQueryCount; ++i)
                    canMatchGameObjects |= CanMatchGameObjects(archetypeQueries[i]);
                InitializeReaderWriter(queryData, requiredComponents, requiredComponentCount,
                    enableableTypesInAny.GetUnsafeReadOnlyPtr<ComponentType>(), enableableTypesInAny.Length, canMatchGameObjects);
                queryData->ArchetypeQueryCount = archetypeQueryCount;
                queryData->ArchetypeQueries = (ArchetypeQuery*)ChunkAllocate<ArchetypeQuery>(archetypeQueryCount, archetypeQueries);
                for (var i = 0; i < archetypeQueryCount; ++i)
                {
                    queryData->ArchetypeQueries[i].All = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AllCount, archetypeQueries[i].All);
                    queryData->ArchetypeQueries[i].Any = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AnyCount, archetypeQueries[i].Any);
                    queryData->ArchetypeQueries[i].None = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].NoneCount, archetypeQueries[i].None);
                    queryData->ArchetypeQueries[i].Disabled = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].DisabledCount, archetypeQueries[i].Disabled);
                    queryData->ArchetypeQueries[i].Absent = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].AbsentCount, archetypeQueries[i].Absent);
                    queryData->ArchetypeQueries[i].Present = (TypeIndex*)ChunkAllocate<TypeIndex>(queryData->ArchetypeQueries[i].PresentCount, archetypeQueries[i].Present);
                    queryData->ArchetypeQueries[i].AllAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AllCount, archetypeQueries[i].AllAccessMode);
                    queryData->ArchetypeQueries[i].AnyAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AnyCount, archetypeQueries[i].AnyAccessMode);
                    queryData->ArchetypeQueries[i].NoneAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].NoneCount, archetypeQueries[i].NoneAccessMode);
                    queryData->ArchetypeQueries[i].DisabledAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].DisabledCount, archetypeQueries[i].DisabledAccessMode);
                    queryData->ArchetypeQueries[i].AbsentAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].AbsentCount, archetypeQueries[i].AbsentAccessMode);
                    queryData->ArchetypeQueries[i].PresentAccessMode = (byte*)ChunkAllocate<byte>(queryData->ArchetypeQueries[i].PresentCount, archetypeQueries[i].PresentAccessMode);
                }

                queryData->QueryDataIndex = m_EntityQueryDatas.Length;
                entityQueryCache.Add(hash, m_EntityQueryDatas.Length);
                m_EntityQueryDatas.Add(queryData);
            }

            var descriptions = QueryDescription.Allocate(archetypeQueries, archetypeQueryCount);
            var cacheEntries = CreateCacheEntries(descriptions, archetypeQueryCount, requiredComponents, requiredComponentCount, access->EntityComponentStore);
            return EntityQuery.Construct(queryData, access, descriptions, archetypeQueryCount, cacheEntries);
        }

        internal static bool CanMatchGameObjects(in ArchetypeQuery archetypeQuery)
        {
            var isGameObjectTypeIndex = TypeManager.GetTypeIndex<GameObjectComponentStoreBridge.IsGameObject>();
            for (var i = 0; i < archetypeQuery.NoneCount; ++i)
            {
                if (archetypeQuery.None[i] == isGameObjectTypeIndex)
                    return false;
            }
            for (var i = 0; i < archetypeQuery.AbsentCount; ++i)
            {
                if (archetypeQuery.Absent[i] == isGameObjectTypeIndex)
                    return false;
            }
            return true;
        }

        void InitializeReaderWriter(EntityQueryData* queryData, ComponentType* requiredTypes, int requiredCount,
            ComponentType* implicitReadTypes, int implicitReadCount, bool canMatchGameObjects)
        {
            Assert.IsTrue(requiredCount > 0);
            Assert.IsTrue(requiredTypes[0] == ComponentType.ReadWrite<Entity>());

            queryData->ReaderTypesCount = 0;
            queryData->WriterTypesCount = 0;

            // A query that accesses TransformRef and can match GameObject entities can reach GameObject transforms. It also depends 
            // on EntityJobsOnGameObjectTransforms and NativeJobsOnGameObjectTransforms
            var gameObjectTransformAccess = ComponentType.AccessMode.Exclude;
            var transformRefTypeIndex = TypeManager.GetTypeIndex<TransformRef>();

            for (var i = 1; i != requiredCount; i++)
            {
                if (canMatchGameObjects && requiredTypes[i].TypeIndex == transformRefTypeIndex)
                    gameObjectTransformAccess = requiredTypes[i].AccessModeType;

                if (requiredTypes[i].IsZeroSized && !requiredTypes[i].IsEnableable)
                    continue;

                switch (requiredTypes[i].AccessModeType)
                {
                    case ComponentType.AccessMode.ReadOnly:
                    case ComponentType.AccessMode.Exclude: // we must include required excluded types as readers in case they're enableable; see DOTS-9730
                        queryData->ReaderTypesCount++;
                        break;
                    case ComponentType.AccessMode.ReadWrite:
                        queryData->WriterTypesCount++;
                        break;
                }
            }
            queryData->ReaderTypesCount += implicitReadCount;
            if (gameObjectTransformAccess == ComponentType.AccessMode.ReadOnly)
                queryData->ReaderTypesCount += 2;
            else if (gameObjectTransformAccess == ComponentType.AccessMode.ReadWrite)
                queryData->WriterTypesCount += 2;

            queryData->ReaderTypes = (TypeIndex*)m_EntityQueryDataChunkAllocator.Allocate(sizeof(TypeIndex) * queryData->ReaderTypesCount, 4);
            queryData->WriterTypes = (TypeIndex*)m_EntityQueryDataChunkAllocator.Allocate(sizeof(TypeIndex) * queryData->WriterTypesCount, 4);

            var curReader = 0;
            var curWriter = 0;
            for (var i = 1; i != requiredCount; i++)
            {
                if (requiredTypes[i].IsZeroSized && !requiredTypes[i].IsEnableable)
                    continue;

                switch (requiredTypes[i].AccessModeType)
                {
                    case ComponentType.AccessMode.ReadOnly:
                    case ComponentType.AccessMode.Exclude: // we must include required excluded types as readers in case they're enableable; see DOTS-9730
                        queryData->ReaderTypes[curReader++] = requiredTypes[i].TypeIndex;
                        break;
                    case ComponentType.AccessMode.ReadWrite:
                        queryData->WriterTypes[curWriter++] = requiredTypes[i].TypeIndex;
                        break;
                }
            }
            for (var i = 0; i < implicitReadCount; i++)
            {
                queryData->ReaderTypes[curReader++] = implicitReadTypes[i].TypeIndex;
            }
            if (gameObjectTransformAccess == ComponentType.AccessMode.ReadOnly)
            {
                queryData->ReaderTypes[curReader++] = TypeManager.GetTypeIndex<EntityJobsOnGameObjectTransforms>();
                queryData->ReaderTypes[curReader++] = TypeManager.GetTypeIndex<NativeJobsOnGameObjectTransforms>();
            }
            else if (gameObjectTransformAccess == ComponentType.AccessMode.ReadWrite)
            {
                queryData->WriterTypes[curWriter++] = TypeManager.GetTypeIndex<EntityJobsOnGameObjectTransforms>();
                queryData->WriterTypes[curWriter++] = TypeManager.GetTypeIndex<NativeJobsOnGameObjectTransforms>();
            }
        }

        [ExcludeFromBurstCompatTesting("Returns managed string")]
        internal string GetQueryDescriptionForProfiler(int queryDataIndex)
        {
            if (queryDataIndex < 0 || queryDataIndex >= m_EntityQueryDatas.Length)
                return string.Empty;

            var queryData = m_EntityQueryDatas.Ptr[queryDataIndex];
            var description = new StringBuilder();
            for (var i = 0; i < queryData->ArchetypeQueryCount; ++i)
            {
                ref var archetypeQuery = ref queryData->ArchetypeQueries[i];
                if (i > 0)
                    description.Append(" | ");

                var start = description.Length;
                AppendTypesForProfiler(description, "All", archetypeQuery.All, archetypeQuery.AllAccessMode, archetypeQuery.AllCount);
                AppendTypesForProfiler(description, "Any", archetypeQuery.Any, archetypeQuery.AnyAccessMode, archetypeQuery.AnyCount);
                AppendTypesForProfiler(description, "None", archetypeQuery.None, null, archetypeQuery.NoneCount);
                AppendTypesForProfiler(description, "Disabled", archetypeQuery.Disabled, archetypeQuery.DisabledAccessMode, archetypeQuery.DisabledCount);
                AppendTypesForProfiler(description, "Absent", archetypeQuery.Absent, null, archetypeQuery.AbsentCount);
                AppendTypesForProfiler(description, "Present", archetypeQuery.Present, archetypeQuery.PresentAccessMode, archetypeQuery.PresentCount);
                if (description.Length == start)
                    description.Append("<Universal>");
                if (archetypeQuery.Options != EntityQueryOptions.Default)
                    description.Append(" Options(").Append(archetypeQuery.Options).Append(')');
            }
            return description.ToString();
        }

        [ExcludeFromBurstCompatTesting("Takes managed StringBuilder")]
        static void AppendTypesForProfiler(StringBuilder description, string label, TypeIndex* types, byte* accessModes, int count)
        {
            if (count == 0)
                return;

            if (description.Length > 0 && description[description.Length - 1] != ' ')
                description.Append(' ');
            description.Append(label).Append('(');
            for (var i = 0; i < count; ++i)
            {
                if (i > 0)
                    description.Append(", ");

                // Drop the namespace, but not the dots inside generic arguments.
                var typeName = TypeManager.GetTypeInfo(types[i]).DebugTypeName.ToString();
                var nameEnd = 0;
                while (nameEnd < typeName.Length && typeName[nameEnd] != '<' && typeName[nameEnd] != '[' && typeName[nameEnd] != '`')
                    ++nameEnd;
                var nameStart = nameEnd > 0 ? typeName.LastIndexOf('.', nameEnd - 1) + 1 : 0;
                description.Append(typeName, nameStart, typeName.Length - nameStart);
                if (accessModes != null && accessModes[i] == (byte)ComponentType.AccessMode.ReadOnly)
                    description.Append(" [RO]");
            }
            description.Append(')');
        }

        // Builds the MatchingArchetype record of an archetype that matches archetypeQuery, with positions keyed by requiredComponents.
        MatchingArchetype* FillRecord(MatchingArchetype* match, ref ArchetypeQuery archetypeQuery, ComponentType* requiredComponents, int requiredComponentsCount)
        {
            var archetype = match->Archetype;
            var typeIndexInArchetypeArray = match->IndexInArchetype;

            var typeComponentIndex = 0;
            for (var component = 0; component < requiredComponentsCount; ++component)
            {
                if (requiredComponents[component].AccessModeType != ComponentType.AccessMode.Exclude)
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, requiredComponents[component].TypeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    typeIndexInArchetypeArray[component] = typeComponentIndex;
                }
                else
                {
                    typeIndexInArchetypeArray[component] = -1;
                }
            }

            var enableableAllCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.AllCount; ++i)
            {
                var typeIndex = archetypeQuery.All[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    match->EnableableTypeMemoryOrderInArchetype_All[enableableAllCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeComponentIndex];
                }
            }

            var enableableNoneCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.NoneCount; ++i)
            {
                var typeIndex = archetypeQuery.None[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    var currentTypeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    if (currentTypeComponentIndex != -1) // we skip storing "None" component types for matching archetypes that do not contain the "None" type (there are no bits to check)
                    {
                        match->EnableableTypeMemoryOrderInArchetype_None[enableableNoneCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[currentTypeComponentIndex];
                        typeComponentIndex = currentTypeComponentIndex;
                    }
                }
            }

            // If the archetype contains any of the required non-enableable Any types, then we know every entity in this
            // archetype definitely meets this query's Any constraint; we don't need to perform any per-entity checks
            // on any enableable components in the Any list. We've already set the MatchingArchetype's Any count to zero in this
            // case, but we also need to skip storing these types into the EnableableTypeMemoryOrderInArchetype_Any array.
            if (match->EnableableComponentsCount_Any > 0)
            {
                var enableableAnyCount = 0;
                typeComponentIndex = 0;
                for (var i = 0; i < archetypeQuery.AnyCount; ++i)
                {
                    var typeIndex = archetypeQuery.Any[i];
                    if (TypeManager.IsEnableable(typeIndex))
                    {
                        var currentTypeComponentIndex =
                            ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                        // The archetype might not contain *all* the Any types (by definition; this is the whole point of Any).
                        // Skip storing the missing types.
                        if (currentTypeComponentIndex != -1)
                        {
                            match->EnableableTypeMemoryOrderInArchetype_Any[enableableAnyCount++] =
                                archetype->TypeIndexInArchetypeToMemoryOrderIndex[currentTypeComponentIndex];
                            typeComponentIndex = currentTypeComponentIndex;
                        }
                    }
                }
            }

            var enableableDisabledCount = 0;
            typeComponentIndex = 0;
            for (var i = 0; i < archetypeQuery.DisabledCount; ++i)
            {
                var typeIndex = archetypeQuery.Disabled[i];
                if (TypeManager.IsEnableable(typeIndex))
                {
                    typeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                    Assert.AreNotEqual(-1, typeComponentIndex);
                    match->EnableableTypeMemoryOrderInArchetype_Disabled[enableableDisabledCount++] = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeComponentIndex];
                }
            }

            // Absent types aren't handled because they're not present in the archetype at all, by definition.
            // Present types aren't handled because we don't care if they're enabled or not, by definition.

            return match;
        }

        static bool IsMatchingArchetype(Archetype* archetype, EntityQueryData* query)
        {
            for (int i = 0; i != query->ArchetypeQueryCount; i++)
            {
                if (IsMatchingArchetype(archetype, query->ArchetypeQueries + i))
                {
                    return true;
                }
            }

            return false;
        }

        static bool IsMatchingArchetype(Archetype* archetype, ArchetypeQuery* query)
        {
            ulong archetypeBloomFilterMask = archetype->BloomFilterMask;
            // Bloom-filter-based early out, if the required components can't possibly be present in this archetype.
            if (Hint.Likely((query->AllBloomFilterMask & archetypeBloomFilterMask) != query->AllBloomFilterMask))
                return false;

            // Early-out at the archetype level
            var options = query->Options;
            var includeDisabledEntities = (options & EntityQueryOptions.IncludeDisabledEntities) != 0;
            var includePrefab = (options & EntityQueryOptions.IncludePrefab) != 0;
            var includeSystems = (options & EntityQueryOptions.IncludeSystems) != 0;
            var includeChunkHeader = (options & EntityQueryOptions.IncludeMetaChunks) != 0;
            if (archetype->Disabled && !includeDisabledEntities)
                return false;
            if (archetype->Prefab && !includePrefab)
                return false;
            if (archetype->HasSystemInstanceComponents && !includeSystems)
                return false;
            if (archetype->HasChunkHeader && !includeChunkHeader)
                return false;

            var archetypeTypes = archetype->Types;
            var archetypeTypesCount = archetype->TypesCount;
            // More early-outs: if we require more components than are in the archetype, it obviously doesn't match
            if (archetypeTypesCount < query->AllCount ||
                archetypeTypesCount < query->DisabledCount ||
                archetypeTypesCount < query->PresentCount)
            {
                return false;
            }

            return
                TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->All, query->AllCount)
                && TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->Disabled, query->DisabledCount)
                && TestMatchingArchetypeRequiredComponent(archetypeTypes, archetypeTypesCount, query->Present, query->PresentCount)
                && TestMatchingArchetypeOptional(archetypeTypes, archetypeTypesCount, query->Any, query->AnyCount)
                && TestMatchingArchetypeExcludedComponent(archetypeTypes, archetypeTypesCount, query->None, query->NoneCount,
                    ignoreEnableableTypes:true)
                && TestMatchingArchetypeExcludedComponent(archetypeTypes, archetypeTypesCount, query->Absent, query->AbsentCount,
                    ignoreEnableableTypes:false);
        }

        static bool TestMatchingArchetypeRequiredComponent(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount)
        {
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                {
                    if (++iNextQueryType == queryTypesCount)
                        return true; // Ran out of query types; all required types were found.
                    nextQueryType = queryTypes[iNextQueryType];
                }
                else if (componentTypeIndex > nextQueryType)
                    return false; // A required type was not found
            }
            // Ran out of archetype types & didn't find all required types
            return false;
        }

        static bool TestMatchingArchetypeOptional(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount)
        {
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                while(componentTypeIndex > nextQueryType)
                {
                    if (++iNextQueryType == queryTypesCount)
                        return false; // Ran out of optional types and didn't find at least one of them
                    nextQueryType = queryTypes[iNextQueryType];
                }
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                    return true; // found at least one optional type
            }
            // Ran out of archetype types & didn't find at least one optional type
            return false;
        }

        static bool TestMatchingArchetypeExcludedComponent(ComponentTypeInArchetype* archetypeTypes,
            int archetypeTypesCount, TypeIndex* queryTypes, int queryTypesCount, bool ignoreEnableableTypes)
        {
            TypeIndex* filteredQueryTypes = stackalloc TypeIndex[queryTypesCount];
            if (ignoreEnableableTypes)
            {
                int filteredQueryTypesCount = 0;
                for (int i = 0; i < queryTypesCount; ++i)
                {
                    if (!TypeManager.IsEnableable(queryTypes[i]))
                        filteredQueryTypes[filteredQueryTypesCount++] = queryTypes[i];
                }
                queryTypes = filteredQueryTypes;
                queryTypesCount = filteredQueryTypesCount;
            }
            if (queryTypesCount == 0)
                return true; // no types to search for
            int iNextQueryType = 0;
            TypeIndex nextQueryType = queryTypes[iNextQueryType];
            for (var i = 0; i < archetypeTypesCount; i++)
            {
                var componentTypeIndex = archetypeTypes[i].TypeIndex;
                while (componentTypeIndex > nextQueryType)
                {
                    if (++iNextQueryType == queryTypesCount)
                        return true; // Ran out of query types. No excluded types were found
                    nextQueryType = queryTypes[iNextQueryType];
                }
                if (Hint.Unlikely(componentTypeIndex == nextQueryType))
                    return false; // An excluded type was found in archetype
            }
            // Ran out of archetype types & didn't find any excluded types
            return true;
        }

        // Records are in archetype creation order, so a binary search on the creation number finds an archetype's record.
        public static int FindRecordIndex(in UnsafeMatchingArchetypePtrList records, Archetype* archetype)
        {
            var ptrs = records.Ptr;
            var version = archetype->Version;
            int low = 0;
            int high = records.Length - 1;
            while (low <= high)
            {
                int middle = (low + high) >> 1;
                var middleArchetype = ptrs[middle]->Archetype;
                if (middleArchetype->Version < version)
                    low = middle + 1;
                else if (middleArchetype->Version > version)
                    high = middle - 1;
                else
                    return middleArchetype == archetype ? middle : -1;
            }

            return -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        static void CheckRecordInCreationOrder(in UnsafeMatchingArchetypePtrList records, Archetype* archetype)
        {
            if (records.Length > 0 && records.Ptr[records.Length - 1]->Archetype->Version >= archetype->Version)
                throw new InvalidOperationException("Query cache records must stay in archetype creation order");
        }

        public static int FindMatchingArchetypeIndexForArchetype(ref UnsafeMatchingArchetypePtrList matchingArchetypes,
            Archetype* archetype)
        {
            int archetypeCount = matchingArchetypes.Length;
            var ptrs = matchingArchetypes.Ptr;
            for (int i = 0; i < archetypeCount; ++i)
            {
                if (archetype == ptrs[i]->Archetype)
                    return i;
            }

            return -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowIfEntityQueryMasksIsGreaterThanLimit(int entityQueryMasksAllocated)
        {
            if (entityQueryMasksAllocated >= 1024)
                throw new Exception("You have reached the limit of 1024 unique EntityQueryMasks, and cannot generate any more.");
        }

        EntityQueryMask AllocateEntityQueryMask(UnsafeMatchingArchetypePtrList records, EntityComponentStore* ecStore)
        {
            ThrowIfEntityQueryMasksIsGreaterThanLimit(m_EntityQueryMasksAllocated);

            var mask = new EntityQueryMask(
                (byte)(m_EntityQueryMasksAllocated / 8),
                (byte)(1 << (m_EntityQueryMasksAllocated % 8)),
                ecStore);

            m_EntityQueryMasksAllocated++;

            int archetypeCount = records.Length;
            var ptrs = records.Ptr;
            for (var i = 0; i < archetypeCount; ++i)
            {
                ptrs[i]->Archetype->QueryMaskArray[mask.Index] |= mask.Mask;
            }

            return mask;
        }

        // The mask belongs to the query's cache entry, so every query with the same entry shares it. An entry with a
        // mask is never freed.
        public EntityQueryMask GetEntityQueryMask(int* cacheEntries, int descriptionCount, EntityComponentStore* ecStore)
        {
            if (descriptionCount == 1)
            {
                var entry = GetCacheEntry(cacheEntries[0]);
                if (!entry->Mask.IsCreated())
                    entry->Mask = AllocateEntityQueryMask(entry->Records, ecStore);
                return entry->Mask;
            }

            var combined = GetCombinedEntry(cacheEntries[descriptionCount]);
            if (!combined->Mask.IsCreated())
                combined->Mask = AllocateEntityQueryMask(combined->Records, ecStore);
            return combined->Mask;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct MatchingArchetype
    {
        public Archetype* Archetype;
        public int RequiredComponentCount;
        public int EnableableComponentsCount_All;
        public int EnableableComponentsCount_None;
        public int EnableableComponentsCount_Any;
        public int EnableableComponentsCount_Disabled; // TODO(DOTS-7809): the None and Disabled lists can be combined here, as they're treated identically in all cases.
        // No need to count enableable absent components, since they're not in the archetype (by definition)
        // No need to count enableable present components, since we don't care if they're enabled or not (by definition)

        public fixed int IndexInArchetype[1];

        public int* EnableableTypeMemoryOrderInArchetype_All
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_None
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_Any
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All + EnableableComponentsCount_None;
                }
            }
        }
        public int* EnableableTypeMemoryOrderInArchetype_Disabled
        {
            get
            {
                fixed (int* basePtr = IndexInArchetype)
                {
                    return basePtr + RequiredComponentCount + EnableableComponentsCount_All + EnableableComponentsCount_None + EnableableComponentsCount_Any;
                }
            }
        }
        public static int CalculateMatchingArchetypeEnableableTypeIntersectionCount(Archetype* archetype, TypeIndex* queryComponents, int queryComponentCount)
        {
            var intersectionCount = 0;
            var typeComponentIndex = 0;
            for (int i = 0; i < queryComponentCount; ++i)
            {
                var typeIndex = queryComponents[i];
                if (!TypeManager.IsEnableable(typeIndex))
                    continue;
                var currentTypeComponentIndex = ChunkDataUtility.GetNextIndexInTypeArray(archetype, typeIndex, typeComponentIndex);
                if (currentTypeComponentIndex >= 0)
                {
                    typeComponentIndex = currentTypeComponentIndex;
                    intersectionCount++;
                }
            }

            return intersectionCount;
        }

        public static MatchingArchetype* Create(ref BlockAllocator allocator, Archetype* archetype, in ArchetypeQuery archetypeQuery,
            int queryRequiredComponentCount)
        {
            var header = CreateHeader(archetype, archetypeQuery, queryRequiredComponentCount);
            return CopyHeader((MatchingArchetype*)allocator.Allocate(header.AllocationSize, 8), header);
        }

        public static MatchingArchetype* Create(MemoryLabel label, Archetype* archetype, in ArchetypeQuery archetypeQuery,
            int queryRequiredComponentCount)
        {
            var header = CreateHeader(archetype, archetypeQuery, queryRequiredComponentCount);
            return CopyHeader((MatchingArchetype*)Memory.Unmanaged.Allocate(header.AllocationSize, 8, label), header);
        }

        // The arrays after the header aren't part of it, so only its fields are copied.
        static MatchingArchetype* CopyHeader(MatchingArchetype* match, in MatchingArchetype header)
        {
            match->Archetype = header.Archetype;
            match->RequiredComponentCount = header.RequiredComponentCount;
            match->EnableableComponentsCount_All = header.EnableableComponentsCount_All;
            match->EnableableComponentsCount_None = header.EnableableComponentsCount_None;
            match->EnableableComponentsCount_Any = header.EnableableComponentsCount_Any;
            match->EnableableComponentsCount_Disabled = header.EnableableComponentsCount_Disabled;
            return match;
        }

        static MatchingArchetype CreateHeader(Archetype* archetype, in ArchetypeQuery archetypeQuery, int queryRequiredComponentCount)
        {
            // If the archetype matches any of the non-enableable Any types in this archetypeQuery, we don't need to do
            // any per-entity checks on the enableable Any types. Force the count to zero in this case.
            //
            // This is not just an optimization; it's necessary for correctness. Otherwise, an entity with components
            // T1 (non-enableable) and T2 (enableable) would fail to match a query with WithAny<T1,T2> if T2 is disabled.
            // (This was a bug for the first several versions of Entities 1.x -- see ECSB-809).
            //
            // Both Archetype.Types[] and ArchetypeQuery.Any[] are assumed to be sorted by TypeIndex, so we can
            // implement this as a single loop instead of N^2.
            bool archetypeMatchesNonEnableableAnyTypes = false;
            int iA = 0, iAQ = 0;
            while (iA < archetype->TypesCount && iAQ < archetypeQuery.AnyCount)
            {
                TypeIndex typeAQ = archetypeQuery.Any[iAQ];
                if (typeAQ.IsEnableable)
                {
                    iAQ++;
                    continue;
                }
                TypeIndex typeA = archetype->Types[iA].TypeIndex;
                if (typeAQ < typeA)
                    iAQ++;
                else if (typeA < typeAQ)
                    iA++;
                else // non-enableable typeA == typeAQ
                {
                    archetypeMatchesNonEnableableAnyTypes = true;
                    break;
                }
            }

            var enableableAllCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.All, archetypeQuery.AllCount);
            var enableableNoneCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.None, archetypeQuery.NoneCount);
            var enableableAnyCount = archetypeMatchesNonEnableableAnyTypes
                ? 0 : CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.Any, archetypeQuery.AnyCount);
            var enableableDisabledCount = CalculateMatchingArchetypeEnableableTypeIntersectionCount(archetype, archetypeQuery.Disabled, archetypeQuery.DisabledCount);
            return new MatchingArchetype
            {
                Archetype = archetype,
                RequiredComponentCount = queryRequiredComponentCount,
                EnableableComponentsCount_All = enableableAllCount,
                EnableableComponentsCount_None = enableableNoneCount,
                EnableableComponentsCount_Any = enableableAnyCount,
                EnableableComponentsCount_Disabled = enableableDisabledCount,
            };
        }

        internal int AllocationSize => GetAllocationSize(RequiredComponentCount,
            EnableableComponentsCount_All + EnableableComponentsCount_None + EnableableComponentsCount_Any + EnableableComponentsCount_Disabled);

        internal static int GetAllocationSize(int requiredComponentsCount, int enableableComponentCount)
        {
            return sizeof(MatchingArchetype) +
                   sizeof(int) * (requiredComponentsCount-1) +
                   sizeof(int) * enableableComponentCount;
        }

        public bool ChunkMatchesFilter(int chunkIndex, ref EntityQueryFilter filter)
        {
            var chunks = Archetype->Chunks;

            // Must match ALL shared component data
            for (int i = 0; i < filter.Shared.Count; ++i)
            {
                var indexInEntityQuery = filter.Shared.IndexInEntityQuery[i];
                var sharedComponentIndex = filter.Shared.SharedComponentIndex[i];
                var componentIndexInChunk = IndexInArchetype[indexInEntityQuery] - Archetype->FirstSharedComponent;
                var sharedComponents = chunks.GetSharedComponentValueArrayForType(componentIndexInChunk);

                // if we don't have a match, we can early out
                if (sharedComponents[chunkIndex] != sharedComponentIndex)
                    return false;
            }

            if (filter.Changed.Count == 0 && !filter.UseOrderFiltering)
                return true;

            var orderVersionFilterPassed = filter.UseOrderFiltering && ChangeVersionUtility.DidChange(chunks.GetOrderVersion(chunkIndex), filter.RequiredChangeVersion);

            // Must have AT LEAST ONE type have changed
            var changedVersionFilterPassed = false;
            for (int i = 0; i < filter.Changed.Count; ++i)
            {
                var indexInEntityQuery = filter.Changed.IndexInEntityQuery[i];
                var componentIndexInChunk = IndexInArchetype[indexInEntityQuery];
                var changeVersions = chunks.GetChangeVersionArrayForType(componentIndexInChunk);

                var requiredVersion = filter.RequiredChangeVersion;

                changedVersionFilterPassed |= ChangeVersionUtility.DidChange(changeVersions[chunkIndex], requiredVersion);
            }

            return changedVersionFilterPassed || orderVersionFilterPassed;
        }
    }

    [DebuggerTypeProxy(typeof(UnsafeMatchingArchetypePtrListDebugView))]
    unsafe struct UnsafeMatchingArchetypePtrList
    {
        [NativeDisableUnsafePtrRestriction]
        private UnsafeList<IntPtr>* ListData;

        // Allocated with the list and never moved, so the native EntityQuery keeps it
        public UnsafeList<IntPtr>* List => ListData;

        public MatchingArchetype** Ptr { get => (MatchingArchetype**)ListData->Ptr; }
        public int Length { get => ListData->Length; }

        public long AllocatedBytes { get => sizeof(UnsafeList<IntPtr>) + (long)ListData->Capacity * sizeof(IntPtr); }

        public void Dispose() { UnsafeList<IntPtr>.Destroy(ListData); }
        public void Add(void* t) { ListData->Add((IntPtr)t); }

        [NativeDisableUnsafePtrRestriction]
        public EntityComponentStore* entityComponentStore;

        public UnsafeMatchingArchetypePtrList(EntityComponentStore* entityComponentStore)
        {
            ListData = UnsafeList<IntPtr>.Create(0, Allocator.Persistent);
            this.entityComponentStore = entityComponentStore;
        }
    }

    [GenerateTestsForBurstCompatibility]
    unsafe struct ArchetypeQuery : IEquatable<ArchetypeQuery>
    {
        // The TypeIndex arrays in this struct are sorted by TypeIndex inside CreateArchetypeQueries().
        public TypeIndex*   Any;
        public byte*        AnyAccessMode;
        public int          AnyCount;

        public TypeIndex*   All;
        public byte*        AllAccessMode;
        public int          AllCount;
        public ulong        AllBloomFilterMask;

        public TypeIndex*   None;
        public byte*        NoneAccessMode;
        public int          NoneCount;

        public TypeIndex*   Disabled;
        public byte*        DisabledAccessMode;
        public int          DisabledCount;

        public TypeIndex*   Absent;
        public byte*        AbsentAccessMode;
        public int          AbsentCount;

        public TypeIndex*   Present;
        public byte*        PresentAccessMode;
        public int          PresentCount;

        public EntityQueryOptions  Options;

        public bool Equals(ArchetypeQuery other)
        {
            if (AnyCount != other.AnyCount)
                return false;
            if (AllCount != other.AllCount)
                return false;
            if (NoneCount != other.NoneCount)
                return false;
            if (DisabledCount != other.DisabledCount)
                return false;
            if (AbsentCount != other.AbsentCount)
                return false;
            if (PresentCount != other.PresentCount)
                return false;
            if (AnyCount > 0 && UnsafeUtility.MemCmp(Any, other.Any, sizeof(int) * AnyCount) != 0 &&
                UnsafeUtility.MemCmp(AnyAccessMode, other.AnyAccessMode, sizeof(byte) * AnyCount) != 0)
                return false;
            if (AllCount > 0 && UnsafeUtility.MemCmp(All, other.All, sizeof(int) * AllCount) != 0 &&
                UnsafeUtility.MemCmp(AllAccessMode, other.AllAccessMode, sizeof(byte) * AllCount) != 0)
                return false;
            if (NoneCount > 0 && UnsafeUtility.MemCmp(None, other.None, sizeof(int) * NoneCount) != 0 &&
                UnsafeUtility.MemCmp(NoneAccessMode, other.NoneAccessMode, sizeof(byte) * NoneCount) != 0)
                return false;
            if (DisabledCount > 0 && UnsafeUtility.MemCmp(Disabled, other.Disabled, sizeof(int) * DisabledCount) != 0 &&
                UnsafeUtility.MemCmp(DisabledAccessMode, other.DisabledAccessMode, sizeof(byte) * DisabledCount) != 0)
                return false;
            if (AbsentCount > 0 && UnsafeUtility.MemCmp(Absent, other.Absent, sizeof(int) * AbsentCount) != 0 &&
                UnsafeUtility.MemCmp(AbsentAccessMode, other.AbsentAccessMode, sizeof(byte) * AbsentCount) != 0)
                return false;
            if (PresentCount > 0 && UnsafeUtility.MemCmp(Present, other.Present, sizeof(int) * PresentCount) != 0 &&
                UnsafeUtility.MemCmp(PresentAccessMode, other.PresentAccessMode, sizeof(byte) * PresentCount) != 0)
                return false;
            if (Options != other.Options)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = AnyCount + 1;
                hashCode = 397 * hashCode ^ (AllCount + 1);
                hashCode = 397 * hashCode ^ (NoneCount + 1);
                hashCode = 397 * hashCode ^ (DisabledCount + 1);
                hashCode = 397 * hashCode ^ (AbsentCount + 1);
                hashCode = 397 * hashCode ^ (PresentCount + 1);
                hashCode = 397 * hashCode ^ (int)Options;
                hashCode = (int)math.hash(Any, sizeof(int) * AnyCount, (uint)hashCode);
                hashCode = (int)math.hash(All, sizeof(int) * AllCount, (uint)hashCode);
                hashCode = (int)math.hash(None, sizeof(int) * NoneCount, (uint)hashCode);
                hashCode = (int)math.hash(Disabled, sizeof(int) * DisabledCount, (uint)hashCode);
                hashCode = (int)math.hash(Absent, sizeof(int) * AbsentCount, (uint)hashCode);
                hashCode = (int)math.hash(Present, sizeof(int) * PresentCount, (uint)hashCode);
                hashCode = (int)math.hash(AnyAccessMode, sizeof(byte) * AnyCount, (uint)hashCode);
                hashCode = (int)math.hash(AllAccessMode, sizeof(byte) * AllCount, (uint)hashCode);
                hashCode = (int)math.hash(NoneAccessMode, sizeof(byte) * NoneCount, (uint)hashCode);
                hashCode = (int)math.hash(DisabledAccessMode, sizeof(byte) * DisabledCount, (uint)hashCode);
                hashCode = (int)math.hash(AbsentAccessMode, sizeof(byte) * AbsentCount, (uint)hashCode);
                hashCode = (int)math.hash(PresentAccessMode, sizeof(byte) * PresentCount, (uint)hashCode);
                return hashCode;
            }
        }
    }

    unsafe struct EntityQueryData
    {
        //@TODO: better name or remove entirely...
        public ComponentType*       RequiredComponents;
        public int                  RequiredComponentsCount;

        public TypeIndex*           ReaderTypes;
        public int                  ReaderTypesCount;

        public TypeIndex*           WriterTypes;
        public int                  WriterTypesCount;

        public TypeIndex*           EnableableComponentTypeIndices;
        public int                  EnableableComponentTypeIndexCount; // number of elements in EnableableComponentTypeIndices

        public ArchetypeQuery*      ArchetypeQueries;
        public int                  ArchetypeQueryCount;

        public byte HasEnableableComponents; // 0 = no, 1 = yes

        // Index in the EntityQueryManager's list; the profiler uses it to identify the query within its world.
        public int QueryDataIndex;
    }
}
