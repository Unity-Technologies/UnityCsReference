// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    // Computes the first entity index of each chunk.
    [BurstCompile]
    struct FilteredChunkIndexJob : IJob
    {
        [ReadOnly] public EntityQuerySafetyHandles Safety;
        public QueryChunks Chunks;
        public EntityQueryFilter Filter;
        [NoAlias] public NativeArray<int> OutFilteredChunkIndices;
        public int QueryIncludesEnableableComponents;

        public void Execute()
        {
            ChunkIterationUtility.CalculateFilteredChunkIndexArray(Chunks, ref Filter, QueryIncludesEnableableComponents, ref OutFilteredChunkIndices);
        }
    }

    // Computes the first entity index of each chunk.
    [BurstCompile]
    struct ChunkBaseEntityIndexJob : IJob
    {
        [ReadOnly] public EntityQuerySafetyHandles Safety;
        public QueryChunks Chunks;
        public EntityQueryFilter Filter;
        [NoAlias] public NativeArray<int> OutChunkBaseEntityIndices;
        public int QueryIncludesEnableableComponents;

        public void Execute()
        {
            ChunkIterationUtility.CalculateBaseEntityIndexArray(Chunks, ref Filter, QueryIncludesEnableableComponents, ref OutChunkBaseEntityIndices);
        }
    }

    // encapsulates an unsafe list whose type isn't known at compile time
    [NativeContainer]
    unsafe struct TypelessUnsafeList
    {
        public AtomicSafetyHandle m_Safety;
        [NativeDisableUnsafePtrRestriction] public byte* Ptr;
        [NativeDisableUnsafePtrRestriction] public int* Length;
        public int Capacity;
    }

    [BurstCompile]
    internal unsafe struct GatherChunksJob : IJob
    {
        [ReadOnly] public EntityQuerySafetyHandles Safety;
        public QueryChunks Chunks;
        public EntityQueryFilter Filter;
        public int QueryContainsEnableableComponents;
        public TypelessUnsafeList OutFilteredChunksList;

        public void Execute()
        {
            var filteredChunkCount = 0;
            var requiresFilter = Filter.RequiresMatchesFilter;
            var hasEnableableComponents = QueryContainsEnableableComponents == 1;
            int cachedChunkCount = Chunks.CalculateLength();
            int currentMatchingArchetypeIndex = -1;
            MatchingArchetype* currentMatchingArchetype = null;
            var currentMatchingArchetypeState = default(ChunkIterationUtility.EnabledMaskMatchingArchetypeState);
            int* currentArchetypeChunkEntityCountsPtr = null;
            ArchetypeChunk* outChunks = (ArchetypeChunk*)OutFilteredChunksList.Ptr;
            var ecs = Chunks.EntityComponentStore;
            if (cachedChunkCount > OutFilteredChunksList.Capacity)
            {
                // ERROR: this means there were more elements to copy than we were expecting, and we're about to
                // write off the end of the output list.
                throw new InvalidOperationException($"Internal error: detected buffer overrun in {nameof(GatherChunksJob)}");
            }
            // Fast path if no filtering at all is required
            if (!requiresFilter && !hasEnableableComponents)
            {
                var cursor = Chunks.GetCursor();
                while (cursor.MoveNext())
                {
                    outChunks[cursor.Index] = new ArchetypeChunk(cursor.Chunk, ecs);
                }
                filteredChunkCount = cachedChunkCount;
            }
            else if (hasEnableableComponents)
            {
                // per-entity + per-chunk filtering
                var cursor = Chunks.GetCursor();
                while (cursor.MoveNext())
                {
                    if (Hint.Unlikely(cursor.RecordIndex != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = cursor.RecordIndex;
                        currentMatchingArchetype = cursor.Record;
                        var currentArchetype = currentMatchingArchetype->Archetype;
                        currentArchetypeChunkEntityCountsPtr = currentArchetype->Chunks.GetChunkEntityCountArray();
                        currentMatchingArchetypeState =
                            new ChunkIterationUtility.EnabledMaskMatchingArchetypeState(currentMatchingArchetype);
                    }
                    int chunkIndexInArchetype = cursor.ChunkIndexInArchetype;
                    if (requiresFilter && !currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref Filter))
                        continue;
                    int chunkEntityCount = currentArchetypeChunkEntityCountsPtr[chunkIndexInArchetype];
                    ChunkIterationUtility.GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, currentMatchingArchetypeState,
                        out var chunkEnabledMask);
                    if (chunkEnabledMask.ULong0 == 0 && chunkEnabledMask.ULong1 == 0)
                        continue;
                    outChunks[filteredChunkCount++] = new ArchetypeChunk(cursor.Chunk, ecs);
                }
            }
            else
            {
                // chunk filtering only
                var cursor = Chunks.GetCursor();
                while (cursor.MoveNext())
                {
                    if (Hint.Unlikely(cursor.RecordIndex != currentMatchingArchetypeIndex))
                    {
                        currentMatchingArchetypeIndex = cursor.RecordIndex;
                        currentMatchingArchetype = cursor.Record;
                    }
                    int chunkIndexInArchetype = cursor.ChunkIndexInArchetype;
                    if (!currentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref Filter))
                        continue;
                    outChunks[filteredChunkCount++] = new ArchetypeChunk(cursor.Chunk, ecs);
                }
            }
            *OutFilteredChunksList.Length = filteredChunkCount;
        }
    }

    [BurstCompile]
    [Obsolete("Only used by obsolete code paths. (RemovedAfter Entities 1.0)")]
    unsafe struct GatherEntitiesToArrayJob : IJobChunk
    {
        [NativeDisableUnsafePtrRestriction] public byte* Entities;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            int baseEntityIndexInQuery = ChunkBaseEntityIndices[unfilteredChunkIndex];
            Entity* destinationPtr = (Entity*)Entities + baseEntityIndexInQuery;
            Entity* sourcePtr = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                int numCopied = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, firstIndexToCheck: rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(destinationPtr+numCopied, sourcePtr+rangeStart, rangeCount * sizeof(Entity));
                    numCopied += rangeCount;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(destinationPtr, sourcePtr, chunk.Count * sizeof(Entity));
            }
        }
    }

    [BurstCompile]
    unsafe struct GatherEntitiesJob : IJobChunk
    {
        [NativeDisableParallelForRestriction] public TypelessUnsafeList OutputList;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            int baseEntityIndexInQuery= ChunkBaseEntityIndices[unfilteredChunkIndex];
            Entity* dstEntities = (Entity*)OutputList.Ptr + baseEntityIndexInQuery;
            Entity* srcEntities = chunk.GetEntityDataPtrRO(EntityTypeHandle);
            int chunkEntityCount = chunk.Count;
            int copyCount = useEnabledMask ? EnabledBitUtility.countbits(chunkEnabledMask) : chunkEntityCount;
            Entity* dstEnd = (Entity*)OutputList.Ptr + OutputList.Capacity;
            if (dstEntities + copyCount > dstEnd)
            {
                // ERROR: this means there were more entities to copy than we were expecting, and we're about to
                // write off the end of the output list.
                throw new InvalidOperationException($"Internal error: detected buffer overrun in {nameof(GatherEntitiesJob)}");
            }
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                int numCopied = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(dstEntities+numCopied, srcEntities+rangeStart, rangeCount * sizeof(Entity));
                    numCopied += rangeCount;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dstEntities, srcEntities, chunk.Count * sizeof(Entity));
            }
            Interlocked.Add(ref *(OutputList.Length), copyCount);
        }
    }

    [Obsolete("Use GatherComponentDataJob instead. (RemovedAfter Entities 1.0)")]
    [BurstCompile]
    unsafe struct GatherComponentDataToArrayJob : IJobChunk
    {
        [NativeDisableUnsafePtrRestriction] public byte* ComponentData;
        public TypeIndex TypeIndex;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var archetype = chunk.Archetype.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeIndex);
            int typeOffset = archetype->Offsets[indexInTypeArray];
            ushort typeSize = archetype->SizeOfs[indexInTypeArray];

            int baseEntityIndexInQuery = ChunkBaseEntityIndices[unfilteredChunkIndex];
            byte* srcBytes = chunk.m_Chunk.Buffer + typeOffset;
            byte* dstBytes = ComponentData + (baseEntityIndexInQuery * typeSize);
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, firstIndexToCheck: rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(dstBytes, srcBytes+rangeStart*typeSize, rangeCount*typeSize);
                    dstBytes += rangeCount * typeSize;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dstBytes, srcBytes, chunk.Count * typeSize);
            }
        }
    }

    [BurstCompile]
    unsafe struct GatherComponentDataJob : IJobChunk
    {
        [NativeDisableParallelForRestriction] public TypelessUnsafeList OutputList;
        [ReadOnly] public DynamicComponentTypeHandle TypeHandle;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            var archetype = chunk.Archetype.Archetype;
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeHandle.m_TypeIndex);
            var typeSize = archetype->SizeOfs[indexInTypeArray];

            int baseEntityIndexInQuery = ChunkBaseEntityIndices[unfilteredChunkIndex];
            byte* dstBytes = OutputList.Ptr + (baseEntityIndexInQuery * typeSize);
            byte* srcBytes = ChunkDataUtility.GetComponentDataWithTypeRO(chunk.m_Chunk, archetype, 0, TypeHandle.m_TypeIndex);
            int chunkEntityCount = chunk.Count;
            int copyCount = useEnabledMask ? EnabledBitUtility.countbits(chunkEnabledMask) : chunkEntityCount;
            var dstEnd = OutputList.Ptr + (OutputList.Capacity * typeSize);
            if (dstBytes + copyCount * typeSize > dstEnd)
            {
                // ERROR: this means there were more entities to copy than we were expecting, and we're about to
                // write off the end of the output list.
                throw new InvalidOperationException(
                    $"Internal error: detected buffer overrun in {nameof(GatherComponentDataJob)}");
            }
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(dstBytes, srcBytes + rangeStart * typeSize, rangeCount * typeSize);
                    dstBytes += rangeCount * typeSize;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dstBytes, srcBytes, chunk.Count * typeSize);
            }
            Interlocked.Add(ref *(OutputList.Length), copyCount);
        }
    }

    [Obsolete("Use CopyComponentListToChunksJob instead. (RemovedAfter Entities 1.0)")]
    [BurstCompile]
    unsafe struct CopyComponentArrayToChunksJob : IJobChunk
    {
        [NativeDisableUnsafePtrRestriction] public byte* ComponentData;
        public TypeIndex TypeIndex;
        public uint GlobalSystemVersion;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var archetype = chunk.Archetype.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeIndex);
            ushort typeSize = archetype->SizeOfs[indexInTypeArray];

            int baseEntityIndexInQuery = ChunkBaseEntityIndices[unfilteredChunkIndex];
            var dstBytes = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, archetype, 0, TypeIndex, GlobalSystemVersion);
            var srcBytes = ComponentData + (baseEntityIndexInQuery * typeSize);
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, firstIndexToCheck: rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(dstBytes+rangeStart*typeSize, srcBytes, rangeCount*typeSize);
                    srcBytes += rangeCount * typeSize;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dstBytes, srcBytes, chunk.Count * typeSize);
            }
        }
    }

    [BurstCompile]
    unsafe struct CopyComponentListToChunksJob : IJobChunk
    {
        [ReadOnly,NativeDisableParallelForRestriction] public TypelessUnsafeList InputList;
        public DynamicComponentTypeHandle TypeHandle;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<int> ChunkBaseEntityIndices;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var archetype = chunk.Archetype.Archetype;
            int indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, TypeHandle.m_TypeIndex);
            ushort typeSize = archetype->SizeOfs[indexInTypeArray];

            int baseEntityIndexInQuery = ChunkBaseEntityIndices[unfilteredChunkIndex];
            byte* dstBytes = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, archetype, 0, TypeHandle.m_TypeIndex, TypeHandle.GlobalSystemVersion);
            byte* srcBytes = InputList.Ptr + (baseEntityIndexInQuery * typeSize);
            int chunkEntityCount = chunk.Count;
            int copyCount = useEnabledMask ? EnabledBitUtility.countbits(chunkEnabledMask) : chunkEntityCount;
            var srcEnd = InputList.Ptr + (*InputList.Length * typeSize);
            if (srcBytes + copyCount*typeSize > srcEnd)
            {
                // ERROR: this means there were more entities to copy than we were expecting, and we're about to
                // read off the end of the input list.
                throw new InvalidOperationException($"Internal error: detected buffer overrun in {nameof(CopyComponentListToChunksJob)}");
            }
            if (useEnabledMask)
            {
                int rangeEnd = 0;
                while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, rangeEnd, out int rangeStart, out rangeEnd))
                {
                    int rangeCount = rangeEnd - rangeStart;
                    UnsafeUtility.MemCpy(dstBytes+rangeStart*typeSize, srcBytes, rangeCount*typeSize);
                    srcBytes += rangeCount * typeSize;
                }
            }
            else
            {
                UnsafeUtility.MemCpy(dstBytes, srcBytes, chunk.Count * typeSize);
            }
        }
    }
}
