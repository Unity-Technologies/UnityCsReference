// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    internal unsafe partial struct EntityComponentStore
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------

        internal bool IsComponentEnabled(Entity entity, TypeIndex typeIndex)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            var typeOffset = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);

            return IsComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk, typeOffset);
        }

        internal bool IsComponentEnabled(Entity entity, TypeIndex typeIndex, ref LookupCache typeLookupCache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
                typeLookupCache.Update(archetype, typeIndex);

            return IsComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk, typeLookupCache.IndexInArchetype);
        }

        internal bool IsComponentEnabled(ChunkIndex chunk, int indexInChunk, int typeIndexInArchetype)
        {
            var archetype = GetArchetype(chunk);

            // the bitmask size is always 128 bits, so make sure we're not indexing outside the chunk's capacity.
            var capacity = archetype->ChunkCapacity;
            if (Hint.Unlikely(indexInChunk < 0 || indexInChunk >= capacity))
                throw new ArgumentException($"indexInChunk {indexInChunk} is outside the valid range [0..{capacity - 1}]");

            var isComponentEnabled = ChunkDataUtility.GetEnabledRefRO(chunk, archetype, typeIndexInArchetype);
            return isComponentEnabled.IsSet(indexInChunk);
        }

        internal void SetComponentEnabled(Entity entity, TypeIndex typeIndex, bool value)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            var typeOffset = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);

            SetComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk, typeOffset, value, GlobalSystemVersion);
        }

        internal void SetComponentEnabled(Entity entity, TypeIndex typeIndex, bool value, ref LookupCache typeLookupCache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
                typeLookupCache.Update(archetype, typeIndex);

            SetComponentEnabled(entityInChunk.Chunk, entityInChunk.IndexInChunk, typeLookupCache.IndexInArchetype, value, GlobalSystemVersion);
        }

        internal void SetComponentEnabled(ChunkIndex chunk, int indexInChunk, int typeIndexInArchetype, bool value, uint globalSystemVersion)
        {
            var archetype = GetArchetype(chunk);
            var capacity = archetype->ChunkCapacity;

            // the bit array size is padded up to 64 bits, so we validate we're not indexing outside the valid data.
            if (Hint.Unlikely(indexInChunk < 0 || indexInChunk >= capacity))
                throw new ArgumentException($"indexInChunk {indexInChunk} is outside the valid range [0..{capacity - 1}]");

            var bits = ChunkDataUtility.GetEnabledRefRW(chunk, archetype, typeIndexInArchetype, globalSystemVersion, out var ptrChunkDisabledCount);
            var numStridesIntoBits = (indexInChunk / 64);
            var pBits = bits.Ptr + numStridesIntoBits;
            var indexInPBits = indexInChunk - (numStridesIntoBits * 64);
            var mask = 1L << indexInPBits;

            var oldBits = (long)*pBits;
            var newBits = 0L;
            var expectedOldBits = 0L;

            do
            {
                newBits = math.select(oldBits & ~mask, oldBits | mask, value);
                expectedOldBits = oldBits;
                oldBits = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<long>(pBits), newBits, expectedOldBits);
            } while (expectedOldBits != oldBits);

            if (oldBits == newBits)
                return;

            // do we need increment or decrement?
            var adjustment = math.select(1, -1, value);
            Interlocked.Add(ref UnsafeUtility.AsRef<int>(ptrChunkDisabledCount), adjustment);
        }

        /// <summary>
        /// Get a pointer to the component type index enabled bits of the chunk the entity is part of.
        /// Also returns a pointer to the chunk's disable counter.
        /// Will update the chunk version
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeIndex"></param>
        /// <param name="typeLookupCache"></param>
        /// <param name="globalSystemVersion"></param>
        /// <param name="indexInBitField"></param>
        /// <param name="ptrChunkDisabledCount"></param>
        /// <returns></returns>
        public ulong* GetEnabledRawRW(Entity entity, TypeIndex typeIndex, ref LookupCache typeLookupCache, uint globalSystemVersion, out int indexInBitField, out int* ptrChunkDisabledCount)
        {
            AssertEntityHasComponent(entity, typeIndex, ref typeLookupCache);
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            indexInBitField = entityInChunk.IndexInChunk;
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
                typeLookupCache.Update(archetype, typeIndex);
            return ChunkDataUtility.GetEnabledRefRW(entityInChunk.Chunk, archetype, typeLookupCache.IndexInArchetype, globalSystemVersion, out ptrChunkDisabledCount).Ptr;
        }

        /// <summary>
        /// Get a pointer to the component type index enabled bits of the chunk the entity is part of.
        /// Also returns a pointer to the chunk's disable counter
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeIndex"></param>
        /// <param name="typeLookupCache"></param>
        /// <param name="indexInBitField"></param>
        /// <param name="ptrChunkDisabledCount"></param>
        /// <returns></returns>
        public ulong* GetEnabledRawRO(Entity entity, TypeIndex typeIndex, ref LookupCache typeLookupCache, out int indexInBitField, out int* ptrChunkDisabledCount)
        {
            AssertEntityHasComponent(entity, typeIndex, ref typeLookupCache);
            var entityInChunk = GetEntityInChunk(entity);
            var archetype = GetArchetype(entityInChunk.Chunk);
            indexInBitField = entityInChunk.IndexInChunk;
            if (Hint.Unlikely(archetype != typeLookupCache.Archetype))
                typeLookupCache.Update(archetype, typeIndex);
            int memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeLookupCache.IndexInArchetype];
            ptrChunkDisabledCount = archetype->Chunks.GetPointerToChunkDisabledCountForType(memoryOrderIndexInArchetype, entityInChunk.Chunk.ListIndex);
            return ChunkDataUtility.GetEnabledRefRO(entityInChunk.Chunk, archetype, typeLookupCache.IndexInArchetype).Ptr;
        }

        //                              | ChangeVersion | OrderVersion |
        // -----------------------------|---------------|--------------|
        // Remove(EntityBatchInChunk)   | NO            | YES          |

        // Write Component to Chunk     | YES           | NO           |
        // Remove Component In-Place    | NO            | NO           |
        // Add Entities in Chunk        | YES           | YES          |
        // Add Component In-Place       | YES           | NO           |
        // Move Chunk World             | YES           | YES          |
        //
        // ChangeVersion : e.g. Should I update LocalToWorld from Translation?
        // OrderVersion : e.g. Should I re-allocate a lookaside cache based on chunk data?


        internal void AllocateEntities(Archetype* arch, ChunkIndex chunk, int baseIndex, int count, Entity* outputEntities)
        {
            Assert.AreEqual(arch->Offsets[0], 0);
            Assert.AreEqual(arch->SizeOfs[0], sizeof(Entity));

            var entityInChunkStart = (Entity*)chunk.Buffer + baseIndex;

            s_entityStore.Data.AllocateEntitiesDirect(entityInChunkStart, count, chunk, baseIndex);

            if (outputEntities != null)
            {
                UnsafeUtility.MemCpy(outputEntities, entityInChunkStart, count * sizeof(Entity));
            }
        }

        internal void DeallocateDataEntitiesInChunk(ChunkIndex chunk, Archetype* archetype, int indexInChunk, int batchCount)
        {
            DeallocateBuffers(chunk, indexInChunk, batchCount);
            DeallocateCompanionGameObjects(chunk, indexInChunk, batchCount);

            var entities = (Entity*)chunk.Buffer + indexInChunk;

            {
                s_entityStore.Data.DeallocateEntities(entities, batchCount);
            }

            EntityNameStorage.RemoveEntitiesFromNameMap(entities, batchCount);
            m_NameStoreAccess.RemoveEntitiesFromEntitiesWithNames(entities, batchCount);


            // Compute the number of things that need to moved and patched.
            int patchCount = Math.Min(batchCount, chunk.Count - indexInChunk - batchCount);

            ChunkDataUtility.RemoveFromEnabledBitsHierarchicalData(chunk, archetype, indexInChunk, batchCount);
            if (0 == patchCount)
            {
                // if we're not patching, we still need to clear the padding bits for the entities we destroyed
                ChunkDataUtility.ClearPaddingBits(chunk, archetype, indexInChunk, batchCount);
                return;
            }

            // updates indexInChunk to point to where the components will be moved to
            //Assert.IsTrue(chunk->archetype->sizeOfs[0] == sizeof(Entity) && chunk->archetype->offsets[0] == 0);
            var movedEntities = (Entity*)chunk.Buffer + (chunk.Count - patchCount);
            for (var i = 0; i != patchCount; i++)
            {
                var entityInChunk = GetEntityInChunk(movedEntities[i]);
                entityInChunk.IndexInChunk = indexInChunk + i;
                SetEntityInChunk(movedEntities[i], entityInChunk);
            }

            // Move component data from the end to where we deleted components
            var startIndex = chunk.Count - patchCount;

            fixed (EntityComponentStore* store = &this)
            {
                ChunkDataUtility.Copy(store, chunk, startIndex, chunk, indexInChunk, patchCount);
            }

            ChunkDataUtility.CloneEnabledBits(chunk, archetype, startIndex, chunk, archetype, indexInChunk, patchCount);
            var clearStartIndex = chunk.Count - batchCount;
            ChunkDataUtility.ClearPaddingBits(chunk, archetype, clearStartIndex, batchCount);
        }

        void DeallocateBuffers(ChunkIndex chunk, int indexInChunk, int batchCount)
        {
            var archetype = GetArchetype(chunk);
            var chunkBuffer = chunk.Buffer;
            var bufferComponentsEnd = archetype->BufferComponentsEnd;

            for (var ti = archetype->FirstBufferComponent; ti < bufferComponentsEnd; ++ti)
            {
                Assert.IsTrue(archetype->Types[ti].IsBuffer);
                var basePtr = chunkBuffer + archetype->Offsets[ti];
                var stride = archetype->SizeOfs[ti];

                for (int i = 0; i < batchCount; ++i)
                {
                    byte* bufferPtr = basePtr + stride * (indexInChunk + i);
                    BufferHeader.Destroy((BufferHeader*)bufferPtr);
                }
            }
        }
    }
}
