// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    [BurstCompile]
    internal unsafe partial struct EntityComponentStore
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckInternalConsistency()
        {
            Assert.IsTrue(CompanionChangesTracker.Empty, "Found unapplied companion changes");
            var usedSequenceNumbers = new UnsafeParallelHashSet<ulong>(512, Allocator.Temp);

            EntityComponentStore* selfPtr;
            fixed(EntityComponentStore* self = &this) { selfPtr = self; }  // This is safe - we're allocated on the native heap

            // Iterate by archetype
            var entityCountByArchetype = 0;
            for (var i = 0; i < m_Archetypes.Length; ++i)
            {
                var archetype = m_Archetypes.Ptr[i];
                Assert.AreEqual((IntPtr)selfPtr, (IntPtr)archetype->EntityComponentStore, "archetype refers to the wrong EntityComponentStore");

                for (int indexInArchetype = 0; indexInArchetype < archetype->TypesCount; ++indexInArchetype)
                {
                    int memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[indexInArchetype];
                    Assert.AreEqual(indexInArchetype, archetype->TypeMemoryOrderIndexToIndexInArchetype[memoryOrderIndexInArchetype], $"archetype component types do not match expected memory order");
                }

                var countInArchetype = 0;
                var capacity = archetype->ChunkCapacity;
                for (var j = 0; j < archetype->Chunks.Count; ++j)
                {
                    var chunk = archetype->Chunks[j];
                    Assert.IsTrue(selfPtr->GetArchetype(chunk) == archetype, "chunk belongs to incorrect archetype");
                    var chunkCount = chunk.Count;
                    Assert.IsTrue(capacity >= chunkCount, "chunk entity count exceeds chunk capacity");
                    Assert.AreEqual(chunkCount, archetype->Chunks.GetChunkEntityCount(j), "cached chunk entity count in archetype does not match actual count");
                    Assert.AreNotEqual(0, chunkCount, "found chunk with entity count of 0; this should never happen");

                    var chunkEntities = (Entity*)chunk.Buffer;
                    AssertEntitiesExist(chunkEntities, chunkCount);

                    if (chunk.SequenceNumber == 0)
                        throw new ArgumentException("Sequence number must not be 0");
                    if (!usedSequenceNumbers.Add(chunk.SequenceNumber))
                        throw new ArgumentException("Sequence number must be unique");

                    if (chunkCount < capacity)
                    {
                        if (archetype->NumSharedComponents == 0)
                        {
                            Assert.IsTrue(chunk.ListWithEmptySlotsIndex >= 0 && chunk.ListWithEmptySlotsIndex < archetype->ChunksWithEmptySlots.Length, "chunk with empty slots is not tracked correctly");
                            Assert.IsTrue(chunk == archetype->ChunksWithEmptySlots[chunk.ListWithEmptySlotsIndex]);
                        }
                        else
                        {
                            Assert.IsTrue(archetype->FreeChunksBySharedComponents.Contains(chunk), "chunk with empty slots and shared components is not tracked correctly");
                        }
                    }

                    countInArchetype += chunkCount;

                    if (archetype->HasChunkHeader) // Chunk entities with chunk components are not supported
                    {
                        Assert.IsFalse(archetype->HasChunkComponents, "chunks with a chunk header should not have chunk components");
                    }

                    var metaChunkEntity = chunk.MetaChunkEntity;
                    Assert.AreEqual(archetype->HasChunkComponents, metaChunkEntity != Entity.Null, $"expected hasChunkComponents={archetype->HasChunkComponents}, actual={!archetype->HasChunkComponents}");
                    if (metaChunkEntity != Entity.Null)
                    {
                        var chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();
                        AssertEntityExists(metaChunkEntity);
                        AssertEntityHasComponent(metaChunkEntity, chunkHeaderTypeIndex);
                        var chunkHeader =
                            *(ChunkHeader*)GetComponentDataWithTypeRO(metaChunkEntity,
                                chunkHeaderTypeIndex);
                        Assert.IsTrue(chunk == chunkHeader.ArchetypeChunk.m_Chunk, "chunk's metaChunkEntity chunk != chunk's metaChunk");
                        var metaChunk = GetChunk(metaChunkEntity);
                        Assert.IsTrue(selfPtr->GetArchetype(metaChunk) == selfPtr->GetArchetype(chunk)->MetaChunkArchetype, "chunk's metaChunk archetype doesn't match cached value");
                        Assert.IsTrue(chunkHeader.ArchetypeChunk.m_EntityComponentStore == selfPtr, "metaChunkEntity's ArchetypeChunk has incorrect EntityComponentStore");
                    }
                }

                Assert.AreEqual(countInArchetype, archetype->EntityCount, "archetype's cached entity count is incorrect");

                AssertPaddingBitsAreZeroForArchetype(archetype);
                AssertEnabledBitsHierarchyIsCorrectForArchetype(archetype);

                entityCountByArchetype += countInArchetype;
            }

            usedSequenceNumbers.Dispose();

            s_entityStore.Data.IntegrityCheck();

            // Enabling this fails SerializeEntitiesWorksWithBlobAssetReferences.
            // There is some special entity 0 usage in the serialization code.

            // @TODO: Review with simon looks like a potential leak?
            //Assert.AreEqual(entityCountByEntities, entityCountByFreeList);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertAllEntitiesCopied(EntityComponentStore* lhs, EntityComponentStore* rhs)
        {
            // TODO - not clear if this check still makes sense with global entities
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ValidateEntity(Entity entity)
        {
            var maximum = EntityStore.MaximumTheoreticalAmountOfEntities;
            if ((uint)entity.Index >= (uint)maximum)
                throw new ArgumentException(
                    "An Entity index is higher than the valid range. This means the entity.Index got corrupted or incorrectly assigned and it may not be used.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ValidateChunk(ChunkIndex chunk)
        {
            if (chunk == ChunkIndex.Null)
                throw new InvalidOperationException(
                    "Entity was created via EntityCommandBuffer.CreateEntity/Instantiate but the EntityCommandBuffer has not yet been played back. Call EntityCommandBuffer.Playback() before reading or writing components on this entity.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void AssertArchetypeComponents(ComponentTypeInArchetype* types, int count)
        {
            if (count < 1)
                throw new ArgumentException($"Invalid component count");

            // NOTE: LookUpCache / ComponentLookup uses short for the IndexInArchetype cache
            if (count >= short.MaxValue)
                throw new ArgumentException($"Archetypes can have a maximum of {short.MaxValue} components.");

            if (types[0].TypeIndex == TypeIndex.Null)
                throw new ArgumentException($"Component type may not be null");
            if (types[0].TypeIndex != m_EntityType)
                throw new ArgumentException($"The Entity ID must always be the first component");

            for (var i = 1; i < count; i++)
            {
                if (types[i - 1].TypeIndex == types[i].TypeIndex)
                    throw new ArgumentException(
                        $"It is not allowed to have two components of the same type on the same entity. ({types[i - 1]} and {types[i]})");

                var SizeInChunk = GetTypeInfo(types[i].TypeIndex).SizeInChunk;
                if (SizeInChunk > ushort.MaxValue)
                    throw new ArgumentException($"IComponentData {types[i]} is too large. SizeOf may not be larger than {ushort.MaxValue}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanCreateArchetype(ComponentType* componentTypes, int componentTypeCount)
        {
            var entityTypeInfo = GetTypeInfo(m_EntityType);
            var archetypeInstanceSize = GetComponentArraySize(entityTypeInfo.SizeInChunk, 1);
            for (int i = 0; i < componentTypeCount; i++)
            {
                var componentTypeInfo = GetTypeInfo(componentTypes[i].TypeIndex);
                var componentInstanceSize = GetComponentArraySize(componentTypeInfo.SizeInChunk, 1);
                archetypeInstanceSize += componentInstanceSize;
            }
            var chunkDataSize = Chunk.kChunkBufferSize;
            if (archetypeInstanceSize > chunkDataSize)
                throw new ArgumentException($"Archetype too large to fit in chunk. Instance size {archetypeInstanceSize} bytes.  Maximum chunk size {chunkDataSize}. Components in new archetype: {AggregateNewArchetypesComponentTypes(componentTypes, componentTypeCount)}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AssertEntityExists(Entity entity)
        {
            AssertEntitiesExist(&entity, 1);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertEntitiesExist(Entity* entities, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = entities + i;

                ValidateEntity(*entity);

                var exists = s_entityStore.Data.Exists(*entity);
                if (!exists)
                    throw new ArgumentException(
                        "An EntityManager command is operating on an invalid entity. This usually means that the Entity has already been destroyed or was never created." +
                        " This can happen when a subscene is opened or closed between when a EntityCommandBuffer has been recorded and played back. This can invalidate recorded Entities.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertValidEntities(Entity* entities, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = entities + i;

                ValidateEntity(*entity);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertEntityHasComponent(Entity entity, ComponentType componentType)
        {
            if (HasComponent(entity, componentType, out var entityExists))
                return;

            if (!entityExists)
            {
                // Distinguish "pre-allocated via ECB but not yet played back" (globally alive,
                // null chunk) from "truly doesn't exist". ValidateChunk throws the ECB message.
                if (s_entityStore.Data.Exists(entity))
                    ValidateChunk(GetChunk(entity));
                throw new ArgumentException("The entity does not exist.");
            }

            throw new ArgumentException($"A component with type:{componentType} has not been added to the entity.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertEntityHasComponent(NativeArray<Entity> entities, ComponentType componentType)
        {
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (HasComponent(entity, componentType, out var entityExists))
                    continue;

                if (!entityExists)
                {
                    if (s_entityStore.Data.Exists(entity))
                        ValidateChunk(GetChunk(entity));
                    throw new ArgumentException("The entity does not exist.");
                }

                throw new ArgumentException($"A component with type:{componentType} has not been added to the entity.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertEntityHasComponent(Entity entity, TypeIndex componentTypeIndex)
        {
            AssertEntityHasComponent(entity, ComponentType.FromTypeIndex(componentTypeIndex));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertEntityHasComponent(Entity entity, TypeIndex componentTypeIndex, ref LookupCache typeLookupCache)
        {
            if (Hint.Likely(HasComponent(entity, componentTypeIndex, ref typeLookupCache, out var entityExists)))
                return;

            if (Hint.Unlikely(!entityExists))
                throw new ArgumentException("The entity does not exist.");

            throw new ArgumentException($"A component with type:{componentTypeIndex} has not been added to the entity.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertNonEmptyArchetypesHaveComponent(UnsafeMatchingArchetypePtrList archetypeList, ComponentType componentType)
        {
            int archetypeCount = archetypeList.Length;
            var ptrs = archetypeList.Ptr;
            TypeIndex typeIndex = componentType.TypeIndex;
            for (int i = 0; i < archetypeCount; i++)
            {
                if (Hint.Unlikely(ptrs[i]->Archetype->EntityCount > 0 && ChunkDataUtility.GetIndexInTypeArray(ptrs[i]->Archetype, typeIndex) == -1))
                    throw new ArgumentException($"Archetype does not have component {componentType}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertComponentIsUnmanaged(TypeIndex componentTypeIndex)
        {
            if (componentTypeIndex.IsManagedTypeInternal)
                throw new ArgumentException($"Component type is managed. Can not get pointer to data");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(Archetype* archetype, ComponentType componentType)
        {
            if (componentType == m_EntityComponentType)
                throw new ArgumentException("Cannot add Entity as a component.");

            if (componentType.IsSharedComponent && (archetype->NumSharedComponents == kMaxSharedComponentCount))
                throw new InvalidOperationException($"Cannot add more than {kMaxSharedComponentCount} SharedComponent's to a single Archetype. Attempting to add '{componentType}'. Archetype already contains types ({AggregateArchetypeComponentTypes(archetype)}).");

            var componentTypeInfo = GetTypeInfo(componentType.TypeIndex);
            var componentInstanceSize = GetComponentArraySize(componentTypeInfo.SizeInChunk, 1);
            if (componentType.IsChunkComponent)
            {
                // If the component to add is a chunk component, we need to check against the target entity's MetaChunk archetype.
                // The MetaChunkArchetype may not even exist yet, if this is the first chunk component being added; if so, for the purposes of this
                // "component too large", check, treat the MetaChunkArchetype instance size as zero.
                var archetypeInstanceSize = (archetype->MetaChunkArchetype == null) ? 0 : archetype->MetaChunkArchetype->InstanceSizeWithOverhead;
                if (archetypeInstanceSize + componentInstanceSize > Chunk.kChunkBufferSize)
                    throw new InvalidOperationException($"Meta-entity archetype component data is too large. Previous meta-entity archetype size per instance {archetypeInstanceSize} bytes. Attempting to add chunk component '{componentType}' with size {componentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}.");
            }
            else
            {
                var archetypeInstanceSize = archetype->InstanceSizeWithOverhead;
                if (archetypeInstanceSize + componentInstanceSize > Chunk.kChunkBufferSize)
                    throw new InvalidOperationException($"Entity archetype component data is too large. Previous archetype size per instance {archetypeInstanceSize} bytes. Attempting to add component '{componentType}' with size {componentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponents(Archetype* archetype, in ComponentTypeSet componentTypeSet)
        {
            int totalComponentInstanceSize = 0;
            int totalChunkComponentInstanceSize = 0;
            for (int i = 0; i < componentTypeSet.Length; i++)
            {
                var type = componentTypeSet.GetComponentType(i);

                if (type == m_EntityComponentType)
                    throw new ArgumentException("Cannot add Entity as a component.");

                var componentTypeInfo = GetTypeInfo(type.TypeIndex);
                var componentInstanceSize = GetComponentArraySize(componentTypeInfo.SizeInChunk, 1);
                if (type.IsChunkComponent)
                    totalChunkComponentInstanceSize += componentInstanceSize;
                else
                    totalComponentInstanceSize += componentInstanceSize;
            }

            int numSharedComponents = componentTypeSet.m_masks.SharedComponents;
            if (numSharedComponents + archetype->NumSharedComponents > kMaxSharedComponentCount)
                throw new InvalidOperationException($"Cannot add more than {kMaxSharedComponentCount} SharedComponent's to a single Archetype. Attempting to add types {AggregateComponentTypes(componentTypeSet)}. Archetype already contains types ({AggregateArchetypeComponentTypes(archetype)}).");

            if (totalChunkComponentInstanceSize > 0)
            {
                // If we're adding any chunk components, we need to check against the target entity's MetaChunk archetype.
                // The MetaChunkArchetype may not even exist yet, if this is the first chunk component being added; if so, for the purposes of this
                // "component too large", check, treat the MetaChunkArchetype instance size as zero.
                var archetypeInstanceSize = (archetype->MetaChunkArchetype == null) ? 0 : archetype->MetaChunkArchetype->InstanceSizeWithOverhead;
                if (archetypeInstanceSize + totalChunkComponentInstanceSize > Chunk.kChunkBufferSize)
                    throw new InvalidOperationException($"Meta-entity archetype component data is too large. Previous meta-entity archetype size per instance {archetypeInstanceSize} bytes. Attempting to add multiple chunk components '{AggregateComponentTypes(componentTypeSet)}' with chunk component size of {totalChunkComponentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}.");
            }
            if (totalComponentInstanceSize > 0)
            {
                var archetypeInstanceSize = archetype->InstanceSizeWithOverhead;
                if (archetypeInstanceSize + totalComponentInstanceSize > Chunk.kChunkBufferSize)
                    throw new InvalidOperationException($"Entity archetype component data is too large. Previous archetype size per instance {archetypeInstanceSize} bytes. Attempting to add multiple components ({AggregateComponentTypes(componentTypeSet)}) with a combined size {totalComponentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}. Archetype already contains types ({AggregateArchetypeComponentTypes(archetype)}).");
            }
        }

        static string AggregateComponentTypes(in ComponentTypeSet componentTypeSet)
        {
            var allTypes = $"{componentTypeSet.Length}: ";
            for (var i = 0; i < componentTypeSet.Length; i++)
            {
                var type = componentTypeSet.GetComponentType(i);
                allTypes += $"'{type}'";
                if (i < componentTypeSet.Length - 1) allTypes += ", ";
            }
            return allTypes;
        }

        static string AggregateArchetypeComponentTypes(Archetype* archetype)
        {
            var numTypes = archetype->TypesCount;
            var allTypes = $"{numTypes}: ";
            for (var i = 0; i < numTypes; i++)
            {
                allTypes += $"'{TypeManager.GetType(archetype->Types[i].TypeIndex)}'";
                if (i < numTypes - 1) allTypes += ", ";
            }
            return allTypes;
        }

        static string AggregateNewArchetypesComponentTypes(ComponentType* componentTypes, int componentTypeCount)
        {
            var allTypes = $"{componentTypeCount}: ";
            for (var i = 0; i < componentTypeCount; i++)
            {
                var type = componentTypes[i].TypeIndex;
                allTypes += $"'{TypeManager.GetType(type)}'";
                if (i < componentTypeCount - 1) allTypes += ", ";
            }
            return allTypes;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(Entity entity, ComponentType componentType)
        {
            if (!Exists(entity))
                throw new InvalidOperationException("The entity does not exist.");
            AssertCanAddComponent(GetArchetype(entity), componentType);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(Entity entity, TypeIndex componentType)
        {
            AssertCanAddComponent(entity, ComponentType.FromTypeIndex(componentType));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(EntityQueryImpl* queryImpl, ComponentType componentType)
        {
            fixed (EntityComponentStore* pThis = &this)
            {
                if (Hint.Unlikely((ulong)queryImpl->_Access->EntityComponentStore != (ulong)pThis))
                    throw new InvalidOperationException("Provided query belongs to a different World.");
            }
            var archetypeList = queryImpl->_QueryData->MatchingArchetypes;
            int archetypeCount = archetypeList.Length;
            var ptrs = archetypeList.Ptr;
            for (int i = 0; i < archetypeCount; i++)
                AssertCanAddComponent(ptrs[i]->Archetype, componentType);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponents(UnsafeMatchingArchetypePtrList archetypeList, in ComponentTypeSet componentTypeSet)
        {
            int newShared = 0;
            int totalComponentInstanceSize = 0;
            int totalChunkComponentInstanceSize = 0;
            for (int i = 0; i < componentTypeSet.Length; i++)
            {
                var componentType = componentTypeSet.GetComponentType(i);
                if (componentType == m_EntityComponentType)
                    throw new ArgumentException("Cannot add Entity as a component.");
                if (componentType.IsSharedComponent)
                    newShared++;
                var componentInstanceSize = GetComponentArraySize(GetTypeInfo(componentType.TypeIndex).SizeInChunk, 1);
                if (componentType.IsChunkComponent)
                    totalChunkComponentInstanceSize += componentInstanceSize;
                else
                    totalComponentInstanceSize += componentInstanceSize;
            }

            int archetypeCount = archetypeList.Length;
            var ptrs = archetypeList.Ptr;
            for (int i = 0; i < archetypeCount; i++)
            {
                var archetype = ptrs[i]->Archetype;
                if ((archetype->NumSharedComponents + newShared) > kMaxSharedComponentCount)
                    throw new InvalidOperationException($"Cannot add more than {kMaxSharedComponentCount} SharedComponent's to a single Archetype. Attempting to add types {AggregateComponentTypes(componentTypeSet)}. Archetype already contains types ({AggregateArchetypeComponentTypes(archetype)}).");
                if (totalChunkComponentInstanceSize > 0)
                {
                    // If we're adding any chunk components, we need to check against the target entity's MetaChunk archetype.
                    // The MetaChunkArchetype may not even exist yet, if this is the first chunk component being added; if so, for the purposes of this
                    // "component too large", check, treat the MetaChunkArchetype instance size as zero.
                    var archetypeInstanceSize = (archetype->MetaChunkArchetype == null) ? 0 : archetype->MetaChunkArchetype->InstanceSizeWithOverhead;
                    if (archetypeInstanceSize + totalChunkComponentInstanceSize > Chunk.kChunkBufferSize)
                        throw new InvalidOperationException($"Meta-entity archetype component data is too large. Previous meta-entity archetype size per instance {archetypeInstanceSize} bytes. Attempting to add multiple chunk components '{AggregateComponentTypes(componentTypeSet)}' with chunk component size of {totalChunkComponentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}.");
                }
                if (totalComponentInstanceSize > 0)
                {
                    var archetypeInstanceSize = archetype->InstanceSizeWithOverhead;
                    if (archetypeInstanceSize + totalComponentInstanceSize > Chunk.kChunkBufferSize)
                        throw new InvalidOperationException($"Entity archetype component data is too large. Previous archetype size per instance {archetypeInstanceSize} bytes. Attempting to add multiple components ({AggregateComponentTypes(componentTypeSet)}) with a combined size {totalComponentInstanceSize} bytes. Maximum chunk size {Chunk.kChunkBufferSize}. Archetype already contains types ({AggregateArchetypeComponentTypes(archetype)}).");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponents(Entity entity, in ComponentTypeSet typeSet)
        {
            if (!Exists(entity))
                throw new InvalidOperationException("The entity does not exist.");

            AssertCanAddComponents(GetArchetype(entity), typeSet);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponents(NativeArray<Entity> entities, in ComponentTypeSet typeSet)
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                AssertCanAddComponents(entity, typeSet);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(NativeArray<Entity> entities, ComponentType type)
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                var entity = entities[i];
                AssertCanAddComponent(entity, type);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanAddComponent(NativeList<EntityBatchInChunk> batches, ComponentType componentType)
        {
            Archetype* archetype = null;
            for (int i = 0; i < batches.Length; i++)
            {
                var nextArchetype = GetArchetype(batches[i].Chunk);
                if (nextArchetype != archetype)
                {
                    if (archetype != null)
                        AssertCanAddComponent(nextArchetype, componentType);
                    archetype = nextArchetype;
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanRemoveComponent(ComponentType componentType)
        {
            if (componentType == m_EntityComponentType)
                throw new ArgumentException("Cannot remove Entity as a component. Use DestroyEntity if you want to delete Entity and all associated components.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanRemoveComponents(in ComponentTypeSet typeSet)
        {
            for (int i = 0; i < typeSet.Length; ++i)
                AssertCanRemoveComponent(ComponentType.FromTypeIndex(typeSet.GetTypeIndex(i)));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void AssertWillDestroyAllInLinkedEntityGroup(UnsafeList<ChunkAndEnabledMask> chunksToProcess,
            ref BufferTypeHandle<LinkedEntityGroup> linkedGroupTypeHandle, bool queryHasEnableableComponents)
        {
            var sortedChunksToProcess = new UnsafeList<ChunkAndEnabledMask>(chunksToProcess.Length, Allocator.Temp);
            sortedChunksToProcess.CopyFrom(chunksToProcess);
            sortedChunksToProcess.Sort();
            var sortedChunkPtrs = new UnsafeList<ChunkIndex>(sortedChunksToProcess.Length, Allocator.Temp);
            for (int i = 0, chunkCount = sortedChunksToProcess.Length; i < chunkCount; ++i)
            {
                sortedChunkPtrs.AddNoResize(sortedChunksToProcess[i].Chunk);
            }

            fixed (EntityComponentStore* pThis = &this)
            {
                for (int i = 0, chunkCount = chunksToProcess.Length; i < chunkCount; ++i)
                {
                    var archetypeChunk = new ArchetypeChunk
                        { m_Chunk = chunksToProcess[i].Chunk, m_EntityComponentStore = pThis };
                    if (!archetypeChunk.Has(ref linkedGroupTypeHandle))
                        continue;

                    ChunkEntityEnumerator enumerator = new ChunkEntityEnumerator(chunksToProcess[i].UseEnabledMask == 0 ? false : true, chunksToProcess[i].EnabledMask, chunksToProcess[i].ChunkEntityCount);
                    Entity* chunkEntities = (Entity*)chunksToProcess[i].Chunk.Buffer;
                    var chunkLegBuffers = archetypeChunk.GetBufferAccessorRO(ref linkedGroupTypeHandle);
                    while (enumerator.NextEntityIndex(out int b))
                    {
                        var buffer = chunkLegBuffers[b];
                        var legEntities = (Entity*)buffer.GetUnsafeReadOnlyPtr();
                        for (int e = 0, entityCount = buffer.Length; e < entityCount; e++)
                        {
                            var referencedEntity = legEntities[e];
                            if (!Exists(referencedEntity))
                                continue; // destroyed entities in the LinkedEntityGroup are fine
                            var referencedEntityArchetype = GetArchetype(referencedEntity);
                            if (referencedEntityArchetype->CleanupResidueArchetype == referencedEntityArchetype)
                                continue; // destroyed entities awaiting cleanup are fine
                            var referencedEntityInChunk = GetEntityInChunk(referencedEntity);
                            int sortedChunkIndex = sortedChunkPtrs.BinarySearch(referencedEntityInChunk.Chunk);
                            if (Hint.Likely(sortedChunkIndex >= 0))
                            {
                                // referencedEntity's chunk matches the query.
                                if (queryHasEnableableComponents && sortedChunksToProcess[sortedChunkIndex].UseEnabledMask != 0)
                                {
                                    // TODO: we only care about the lowest bit of the result, so computing the high 64 bits of the result here is wasted work.
                                    v128 shiftedMask = EnabledBitUtility.ShiftRight(sortedChunksToProcess[sortedChunkIndex].EnabledMask,
                                        referencedEntityInChunk.IndexInChunk);
                                    if (Hint.Unlikely((shiftedMask.ULong0 & 0x1) == 0))
                                    {
                                        // referencedChunk matches the query, but referencedEntity is not enabled in this query & will not be deleted
                                        ThrowDestroyEntityError(chunkEntities[b], referencedEntity);
                                    }
                                }
                            }
                            else
                            {
                                // referencedChunk isn't in chunksToProcess at all, and won't be destroyed.
                                ThrowDestroyEntityError(chunkEntities[b], referencedEntity);
                            }
                        }
                    }
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void ThrowDestroyEntityError(Entity errorEntity, Entity errorReferencedEntity)
        {
            ThrowDestroyEntityErrorFancy(errorEntity, errorReferencedEntity);
            throw new ArgumentException($"DestroyEntity(EntityQuery query) is destroying an entity which contains a LinkedEntityGroup with an entity not included in the query. If you want to destroy entities using a query all linked entities must be contained in the query.. For more detail, disable Burst compilation.");
        }

        [BurstDiscard]
        private void ThrowDestroyEntityErrorFancy(Entity errorEntity, Entity errorReferencedEntity)
        {
            string errorEntityName = Exists(errorEntity) ? GetName(errorEntity) : "(Deleted)";
            string errorReferencedEntityName = Exists(errorReferencedEntity) ? GetName(errorReferencedEntity) : "(Deleted)";
            throw new ArgumentException($"DestroyEntity(EntityQuery query) is destroying entity {errorEntity} '{errorEntityName}' which contains a LinkedEntityGroup and the entity {errorReferencedEntity} '{errorReferencedEntityName}' in that group is not included in the query. If you want to destroy entities using a query all linked entities must be contained in the query..");
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertArchetypeDoesNotRemoveCleanupComponents(Archetype* src, Archetype* dst)
        {
            int o = 0;
            int n = 0;

            for (; o < src->TypesCount && n < dst->TypesCount;)
            {
                var srcType = src->Types[o].TypeIndex;
                var dstType = dst->Types[n].TypeIndex;
                if (srcType == dstType)
                {
                    o++;
                    n++;
                }
                else if (dstType > srcType)
                {
                    if (src->Types[o].IsCleanupComponent)
                        throw new System.ArgumentException(
                            $"Cleanup components may not be removed via SetArchetype: {src->Types[o]}");
                    o++;
                }
                else
                    n++;
            }

            for (; o < src->TypesCount; o++)
            {
                if (src->Types[o].IsCleanupComponent)
                    throw new System.ArgumentException($"Cleanup components may not be removed via SetArchetype: {src->Types[o]}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckCanAddChunkComponent(NativeArray<ArchetypeChunk> chunkArray, ComponentType componentType, ref bool result)
        {
            var chunks = (ArchetypeChunk*)chunkArray.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < chunkArray.Length; ++i)
            {
                var chunk = chunks[i].m_Chunk;
                var archetype = chunks[i].Archetype.Archetype;
                if (ChunkDataUtility.GetIndexInTypeArray(archetype, componentType.TypeIndex) != -1)
                {
                    result = false;
                    return;
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanInstantiateEntities(Entity srcEntity)
        {
            if (HasComponent(srcEntity, m_LinkedGroupType, out var entityExists))
            {
                var header = (BufferHeader*)GetComponentDataWithTypeRO(srcEntity, m_LinkedGroupType);
                var entityPtr = (Entity*)BufferHeader.GetElementPointer(header);
                var entityCount = header->Length;

                if (entityCount == 0 || entityPtr[0] != srcEntity)
                    throw new ArgumentException("LinkedEntityGroup[0] must always be the Entity itself.");

                for (int i = 0; i < entityCount; i++)
                {
                    if (!Exists(entityPtr[i]))
                        throw new ArgumentException(
                            $"The srcEntity's LinkedEntityGroup references an entity that is invalid. (Entity at index {i} on the LinkedEntityGroup.)");

                    var archetype = GetArchetype(entityPtr[i]);
                    if (archetype->InstantiateArchetype == null)
                        throw new ArgumentException(
                            $"The srcEntity's LinkedEntityGroup references an entity that has already been destroyed. (Entity at index {i} on the LinkedEntityGroup. Only cleanup components are left on the entity)");
                }
            }
            else
            {
                if (!entityExists)
                    throw new ArgumentException("srcEntity is not a valid entity");

                var srcArchetype = GetArchetype(srcEntity);
                if (srcArchetype->InstantiateArchetype == null)
                    throw new ArgumentException(
                        "srcEntity is not instantiable because it has already been destroyed. (Only cleanup components are left on it)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertCanInstantiateEntities(Entity* srcEntity, int entityCount, bool removePrefab)
        {
            for (int i = 0; i < entityCount; i++)
            {
                if (!Exists(srcEntity[i]))
                    throw new ArgumentException(
                        $"The srcEntity[{i}] references an entity that is invalid.");

                var archetype = GetArchetype(srcEntity[i]);
                if ((removePrefab ? archetype->InstantiateArchetype : archetype->CopyArchetype) == null)
                    throw new ArgumentException(
                        $"The srcEntity[{i}] references an entity that has already been destroyed. (Only cleanup components are left on the entity)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertValidEntityQuery(EntityQuery query, EntityComponentStore* store)
        {
            var e = query._GetImpl()->_Access->EntityComponentStore;
            if (e != store)
            {
                AssertValidEntityQuery(e, store);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertValidEntityQuery(EntityComponentStore* queryStore, EntityComponentStore* store)
        {
            if (queryStore != store)
            {
                if (queryStore ==  null)
                    throw new System.InvalidOperationException("The EntityQuery has been disposed and can no longer be used.");
                else
                    throw new System.InvalidOperationException("EntityQuery is associated with a different world");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertValidArchetype(EntityComponentStore* queryStore, EntityArchetype archetype)
        {
            if (archetype.Archetype == null || archetype.Archetype->EntityComponentStore != queryStore)
            {
                if (archetype.Valid)
                    throw new System.ArgumentException("The EntityArchetype has not been allocated");
                else
                    throw new System.ArgumentException("The EntityArchetype was not created by this EntityManager");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertPaddingBitsAreZeroForArchetype(Archetype* archetype)
        {
            for (int chunkIndex = 0; chunkIndex < archetype->Chunks.Count; ++chunkIndex)
            {
                var chunk = archetype->Chunks[chunkIndex];

                for (int enableableTypeIndex = 0; enableableTypeIndex < archetype->EnableableTypesCount; ++enableableTypeIndex)
                {
                    var typeIndexInArchetype = archetype->EnableableTypeIndexInArchetype[enableableTypeIndex];
                    int memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeIndexInArchetype];

                    v128 mask = *archetype->Chunks.GetComponentEnabledMaskArrayForTypeInChunk(memoryOrderIndexInArchetype, chunkIndex);
                    var paddingBits = EnabledBitUtility.ShiftRight(mask, chunk.Count);
                    int enabledCount = EnabledBitUtility.countbits(paddingBits);
                    if (enabledCount != 0)
                        Assert.AreEqual(0, enabledCount,
                        $"enabled bits padding check failed: chunk={chunkIndex} memoryOrderIndex={memoryOrderIndexInArchetype} mask={mask.ULong1:X16}:{mask.ULong0:X16} entityCount={chunk.Count} archetype={*archetype}");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public static void AssertEnabledBitsHierarchyIsCorrectForArchetype(Archetype* archetype)
        {
            for (int chunkIndex = 0; chunkIndex < archetype->Chunks.Count; ++chunkIndex)
            {
                var chunk = archetype->Chunks[chunkIndex];

                for (int enableableTypeIndex = 0; enableableTypeIndex < archetype->EnableableTypesCount; ++enableableTypeIndex)
                {
                    var typeIndexInArchetype = archetype->EnableableTypeIndexInArchetype[enableableTypeIndex];
                    int memoryOrderIndexInArchetype = archetype->TypeIndexInArchetypeToMemoryOrderIndex[typeIndexInArchetype];
                    v128 mask = *archetype->Chunks.GetComponentEnabledMaskArrayForTypeInChunk(memoryOrderIndexInArchetype, chunkIndex);
                    int storedDisabledCount = archetype->Chunks.GetChunkDisabledCountForType(memoryOrderIndexInArchetype, chunkIndex);
                    int actualDisabledCount = chunk.Count - EnabledBitUtility.countbits(mask);
                    if (actualDisabledCount != storedDisabledCount)
                        Assert.AreEqual(actualDisabledCount, storedDisabledCount,
                        $"enabled bits hierarchical mismatch: chunk={chunkIndex} memoryOrderIndex={memoryOrderIndexInArchetype} mask={mask.ULong1:X16}:{mask.ULong0:X16} entityCount={chunk.Count} storedCount={storedDisabledCount} archetype={*archetype}");
                }
            }
        }

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void AssertNotZeroSizedComponent(TypeIndex typeIndex)
        {
            if (Hint.Unlikely(typeIndex.IsZeroSized))
                throw new System.ArgumentException(
                    $"This operation can not be called with a zero sized component ({typeIndex}).");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal static void AssertComponentSizeMatches(TypeIndex typeIndex, int size)
        {
            int sizeInChunk = TypeManager.GetTypeInfo(typeIndex).SizeInChunk;
            if (Hint.Unlikely(sizeInChunk != size))
                throw new System.ArgumentException(
                    $"Provided data size={size} for type {typeIndex} does not match expected SizeInChunk={sizeInChunk}. (This should never happen; please use \"Help -> Report A Bug\" to inform the DOTS team)");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal static void AssertComponentEnableable(TypeIndex typeIndex)
        {
            if (Hint.Unlikely(!typeIndex.IsEnableable))
                throw new System.ArgumentException(
                    $"Component Enabled Bits APIs (SetComponentEnabled, IsComponentEnabled, etc) can not be called with a component type ({typeIndex}) that does not implement IEnableableComponent");
        }
    }
}
